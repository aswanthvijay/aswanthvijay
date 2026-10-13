using System;
using System.Linq;
using NUnit.Framework;
using Runeheir.Monsters;
using Runeheir.Social;
using Runeheir.World;

namespace Runeheir.Tests
{
    /// <summary>Phase 8, the War of the Realms: the schedule, who may strike a Jarlstone, captures and the five halls.</summary>
    public sealed class SiegeTests
    {
        // 2026-10-14 is a Wednesday.
        private static readonly DateTime Wednesday = new DateTime(2026, 10, 14);

        private static RealmConfig Schedule(string days = "Wed,Sat", int hour = 20, int minutes = 60)
        {
            return new RealmConfig { SiegeDays = days, SiegeStartHour = hour, SiegeMinutes = minutes };
        }

        [Test]
        public void TheWar_IsOnOnlyAtTheRealmsHours()
        {
            var config = Schedule();
            Assert.IsFalse(SiegeRules.IsSiegeTime(config, Wednesday.AddHours(19).AddMinutes(59)));
            Assert.IsTrue(SiegeRules.IsSiegeTime(config, Wednesday.AddHours(20)));
            Assert.IsTrue(SiegeRules.IsSiegeTime(config, Wednesday.AddHours(20).AddMinutes(59)));
            Assert.IsFalse(SiegeRules.IsSiegeTime(config, Wednesday.AddHours(21)));
            Assert.IsFalse(SiegeRules.IsSiegeTime(config, Wednesday.AddDays(1).AddHours(20)), "Thursday isn't a war day");
            Assert.IsTrue(SiegeRules.IsSiegeTime(config, Wednesday.AddDays(3).AddHours(20).AddMinutes(30)), "Saturday is");
        }

        [Test]
        public void ALateWar_RunsPastMidnight()
        {
            var config = Schedule("Wed", 23, 120);
            Assert.IsTrue(SiegeRules.IsSiegeTime(config, Wednesday.AddDays(1).AddMinutes(30)), "Wednesday's 23:00 war is still on at 00:30");
            Assert.IsFalse(SiegeRules.IsSiegeTime(config, Wednesday.AddDays(1).AddHours(1)));
        }

        [Test]
        public void ARealmWithoutWars_NeverStartsOne()
        {
            Assert.IsFalse(SiegeRules.IsSiegeTime(Schedule(minutes: 0), Wednesday.AddHours(20)));
            Assert.IsFalse(SiegeRules.IsSiegeTime(Schedule(days: " "), Wednesday.AddHours(20)));
            Assert.IsNull(SiegeRules.NextSiege(Schedule(minutes: 0), Wednesday));
            Assert.IsTrue(SiegeRules.IsSiegeTime(Schedule(days: "daily"), Wednesday.AddDays(1).AddHours(20)));
        }

        [Test]
        public void NextSiege_IsTheNextScheduledStart()
        {
            var config = Schedule();
            Assert.AreEqual(Wednesday.AddHours(20), SiegeRules.NextSiege(config, Wednesday.AddHours(9)));
            Assert.AreEqual(Wednesday.AddDays(3).AddHours(20), SiegeRules.NextSiege(config, Wednesday.AddHours(20).AddMinutes(1)), "then Saturday");
        }

        [Test]
        public void OnlyAnotherGuild_StrikesTheJarlstone_AndOnlyInWar()
        {
            Assert.IsTrue(SiegeRules.MayStrikeJarlstone("Ironwolves", null, true), "an unclaimed hall is open to any guild");
            Assert.IsTrue(SiegeRules.MayStrikeJarlstone("Ironwolves", "Ravenkin", true));
            Assert.IsFalse(SiegeRules.MayStrikeJarlstone("Ravenkin", "ravenkin", true), "the holders don't break their own stone");
            Assert.IsFalse(SiegeRules.MayStrikeJarlstone(null, "Ravenkin", true), "the guildless can't take a hall");
            Assert.IsFalse(SiegeRules.MayStrikeJarlstone("Ironwolves", "Ravenkin", false), "outside the war the stone won't yield");
        }

        [Test]
        public void OnlyTheHolders_OpenTheHoards()
        {
            Assert.IsTrue(SiegeRules.MayOpenHoard("Ravenkin", "RAVENKIN"));
            Assert.IsFalse(SiegeRules.MayOpenHoard("Ironwolves", "Ravenkin"));
            Assert.IsFalse(SiegeRules.MayOpenHoard(null, null), "nobody opens an unclaimed hall's hoards");
        }

        [Test]
        public void Capture_ReplacesTheHolder_AndDisbandingReleasesTheHalls()
        {
            var book = new SiegeBook();
            SiegeRules.Capture(book, "castle_bilskirnir", "Ravenkin", 1);
            SiegeRules.Capture(book, "castle_breidablik", "Ravenkin", 2);
            SiegeRules.Capture(book, "castle_bilskirnir", "Ironwolves", 3);

            Assert.AreEqual("Ironwolves", SiegeRules.OwnerOf(book, "castle_bilskirnir"));
            Assert.AreEqual("Ravenkin", SiegeRules.OwnerOf(book, "CASTLE_BREIDABLIK"));
            Assert.AreEqual(2, book.Owners.Count, "one holder per hall");

            SiegeRules.ForgetGuild(book, "ravenkin");
            Assert.IsNull(SiegeRules.OwnerOf(book, "castle_breidablik"));
            Assert.AreEqual("Ironwolves", SiegeRules.OwnerOf(book, "castle_bilskirnir"));
        }

        [Test]
        public void Sanitize_DropsHallsThatDontExist()
        {
            var book = new SiegeBook();
            book.Owners.Add(null);
            book.Owners.Add(new CastleOwner { CastleId = "castle_valhalla", Guild = "Ravenkin" });
            book.Owners.Add(new CastleOwner { CastleId = "castle_gladsheimr", Guild = "" });
            book.Owners.Add(new CastleOwner { CastleId = "castle_sessrumnir", Guild = "Ironwolves" });
            SiegeRules.Sanitize(book);

            Assert.AreEqual(1, book.Owners.Count);
            Assert.AreEqual("Ironwolves", SiegeRules.OwnerOf(book, "castle_sessrumnir"));
        }

        [Test]
        public void EveryHall_HasAJarlstone_Hoards_AndARoadFromIdavollr()
        {
            var fields = MapCatalog.Get(SiegeRules.FieldsMapId);
            Assert.IsNotNull(fields);
            Assert.AreEqual(5, SiegeRules.Castles.Count);
            foreach (var castle in SiegeRules.Castles)
            {
                var hall = MapCatalog.Get(castle.MapId);
                Assert.IsNotNull(hall, castle.MapId);
                Assert.AreEqual(MapKind.Castle, hall.Kind, castle.Name);
                Assert.AreEqual(1, hall.Spawns.Where(s => s.MonsterId == SiegeRules.JarlstoneId).Sum(s => s.Count), castle.Name + ": one Jarlstone");
                Assert.IsTrue(hall.Spawns.Any(s => s.MonsterId == SiegeRules.HoardId), castle.Name + ": hoards");
                Assert.IsTrue(fields.Portals.Any(p => p.TargetMap == castle.MapId), castle.Name + ": a road from Iðavöllr");
                Assert.IsTrue(hall.Portals.Any(p => p.TargetMap == SiegeRules.FieldsMapId), castle.Name + ": a gate back");
            }

            Assert.IsTrue(MapCatalog.Get(MapCatalog.VigridHaven).Portals.Any(p => p.TargetMap == SiegeRules.FieldsMapId), "the road north of Vigrid Haven");
        }

        [Test]
        public void TheJarlstone_AndHoards_AreSiegeObjects()
        {
            var stone = MonsterCatalog.Get(SiegeRules.JarlstoneId);
            var hoard = MonsterCatalog.Get(SiegeRules.HoardId);
            Assert.IsTrue(stone.SiegeObject && stone.Stationary && stone.Passive);
            Assert.IsTrue(hoard.SiegeObject && hoard.Stationary && hoard.Passive);
            Assert.IsTrue(hoard.Drops.Count > 0, "a hoard holds treasure chests");
        }
    }
}
