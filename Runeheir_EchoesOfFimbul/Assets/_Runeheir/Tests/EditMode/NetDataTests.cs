using System.Linq;
using NUnit.Framework;
using Runeheir.Combat;
using Runeheir.World;

namespace Runeheir.Tests
{
    /// <summary>Phase 6 data that crosses the network: map placement in a realm, defence snapshots and auras.</summary>
    public sealed class NetDataTests
    {
        [Test]
        public void WorldGrid_GivesEveryMapItsOwnSquare()
        {
            var squares = MapCatalog.All.Select(m => WorldGrid.Origin(m.Id)).ToList();
            Assert.AreEqual(squares.Count, squares.Distinct().Count(), "no two maps share an origin");
            foreach (var map in MapCatalog.All)
            {
                var origin = WorldGrid.Origin(map.Id);
                Assert.Less(map.Size, WorldGrid.Spacing * 0.5f, map.Id);
                Assert.AreEqual(map.Id, WorldGrid.MapAt(origin.X + map.Size * 0.4f, origin.Z - map.Size * 0.4f)?.Id, map.Id);
            }

            Assert.AreEqual(new GroundPoint(0f, 0f), WorldGrid.Origin("nowhere"));
        }

        [Test]
        public void MapLayout_TranslateMovesEverything()
        {
            var map = MapCatalog.Get(MapCatalog.WhisperwoodPlains);
            var home = MapLayoutGenerator.Generate(map);
            var moved = MapLayoutGenerator.Generate(map);
            moved.Translate(3000f, 1000f);

            Assert.AreEqual(home.OriginX + 3000f, moved.OriginX, 0.001f);
            Assert.AreEqual(home.SavePoint.X + 3000f, moved.SavePoint.X, 0.001f);
            Assert.AreEqual(home.Portals[0].Arrival.Z + 1000f, moved.Portals[0].Arrival.Z, 0.001f);
            Assert.AreEqual(home.Spawns[0].Center.X + 3000f, moved.Spawns[0].Center.X, 0.001f);
            Assert.AreEqual(home.Props[0].At.Z + 1000f, moved.Props[0].At.Z, 0.001f);
            Assert.IsTrue(moved.IsWalkable(moved.SavePoint), "the grid moved with the points");
            Assert.AreEqual(home.IsWalkable(home.Npcs.Count > 0 ? home.Npcs[0].At : home.SavePoint),
                moved.IsWalkable(moved.Npcs.Count > 0 ? moved.Npcs[0].At : moved.SavePoint));
        }

        [Test]
        public void CombatSnapshot_RoundTripsDefences()
        {
            var resist = new DamageBonuses();
            resist.TakenFromRace[(int)Race.Undead] = -30f;
            resist.TakenFromElement[(int)Element.Fire] = -20f;
            var defender = new DefenderProfile
            {
                Def = 40, SoftDef = 22, Mdef = 7, SoftMdef = 11, Flee = 130, Element = Element.Water, Race = Race.DemiHuman,
                Size = Size.Medium, BluntDamageTakenMultiplier = 1f, Resist = resist,
            };

            var copy = CombatSnapshot.From(defender).ToDefender();
            Assert.AreEqual((40, 22, 7, 11, 130), (copy.Def, copy.SoftDef, copy.Mdef, copy.SoftMdef, copy.Flee));
            Assert.AreEqual(Element.Water, copy.Element);
            Assert.AreEqual(-30f, copy.Resist.TakenFromRace[(int)Race.Undead]);
            Assert.AreEqual(-20f, copy.Resist.TakenFromElement[(int)Element.Fire]);

            var hostile = new CombatSnapshot { Def = -5, Element = 99, BluntMultiplier = float.NaN, TakenFromRace = new[] { 500f } };
            var clean = hostile.ToDefender();
            Assert.AreEqual(0, clean.Def);
            Assert.AreEqual(CombatEnumCounts.Elements - 1, (int)clean.Element);
            Assert.AreEqual(1f, clean.BluntDamageTakenMultiplier);
            Assert.AreEqual(100f, clean.Resist.TakenFromRace[0], "a forged snapshot can't go past the limits");
        }

        [Test]
        public void AuraCodec_MirrorsBuffsAndStatuses()
        {
            var buffs = new BuffContainer();
            var statuses = new StatusContainer();
            var buff = BuffCatalog.All.First(b => b.MaxStacks > 1);
            buffs.Apply(buff, 0, 3, 20f);
            buffs.Apply(buff, 0, 3, 20f);
            statuses.Apply(StatusEffect.Freeze, 4f, 0);

            string text = AuraCodec.Encode(buffs, statuses, 1.0);
            var mirrorBuffs = new BuffContainer();
            var mirrorStatuses = new StatusContainer();
            AuraCodec.Apply(text, mirrorBuffs, mirrorStatuses, 100.0);

            Assert.IsTrue(mirrorStatuses.Has(StatusEffect.Freeze));
            Assert.AreEqual(3.0, mirrorStatuses.Remaining(StatusEffect.Freeze, 100.0), 0.11);
            var mirrored = mirrorBuffs.Active.Single();
            Assert.AreEqual(buff.Id, mirrored.Definition.Id);
            Assert.AreEqual(3, mirrored.Level);
            Assert.AreEqual(2, mirrored.Stacks);
            Assert.AreEqual(19.0, mirrored.Remaining(100.0), 0.11);

            AuraCodec.Apply("bno_such_buff:5:1:1;s99:3;garbage;s2:abc", mirrorBuffs, mirrorStatuses, 0);
            Assert.IsEmpty(mirrorBuffs.Active, "unknown buffs and bad entries are skipped");
            Assert.IsEmpty(mirrorStatuses.Active);
            Assert.IsEmpty(AuraCodec.Decode(null));
        }
    }
}
