using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Runeheir.Characters;
using Runeheir.Items;
using Runeheir.Jobs;
using Runeheir.Skills;
using Runeheir.Social;
using Runeheir.Stats;

namespace Runeheir.Tests
{
    /// <summary>Phase 9, the realm guard: honest play is always kept; impossible characters and conjured wealth are refused.</summary>
    public sealed class GuardTests
    {
        private static CharacterRecord Fresh()
        {
            var record = CharacterFactory.Create(new CharacterCreateRequest { Name = "Hrafna" }, 0, 0);
            record.CreatedUnixMs = 1700000000000L;
            return record;
        }

        private static GuardVerdict Audit(CharacterRecord before, CharacterRecord after, GuardLedger ledger = null, bool economy = true,
            List<ItemStack> storageBefore = null, List<ItemStack> storageAfter = null)
        {
            return RealmGuard.Audit(before, storageBefore, after, storageAfter, ledger ?? new GuardLedger(), new GuardOptions { Economy = economy });
        }

        private static ItemDefinition DropOnlyCard()
        {
            return ItemCatalog.All.First(i => i.IsCard && !RealmGuard.IsClientMade(i.Id) && !RealmGuard.IsBoxed(i.Id));
        }

        [Test]
        public void HonestPlay_IsKept()
        {
            var before = Fresh();
            var after = before.Clone();
            var ledger = new GuardLedger();
            ledger.AddExp(5000, 3000, 0);
            new CharacterProgression(after).GainExperience(5000, 3000);
            var progression = new CharacterProgression(after);
            Assert.IsTrue(progression.TryRaiseStat(StatType.Str));
            var card = DropOnlyCard();
            ledger.AddItemIn(card.Id, 1, 0);
            new Inventory(after.Inventory).Add(card.Id, 1);

            var verdict = Audit(before, after, ledger);
            Assert.IsFalse(verdict.Refused, verdict.Summary);
        }

        [Test]
        public void ImpossibleStatsAndSkills_AreRefused()
        {
            var before = Fresh();
            var strong = before.Clone();
            strong.Stats.Str = 99;
            Assert.IsTrue(Audit(before, strong).Refused, "Str 99 at Base Level 1");
            Assert.IsFalse(Audit(before, strong, economy: false).Refused, "a realm with @commands lets GMs set stats");

            var skilled = before.Clone();
            skilled.Skills.Add(new LearnedSkill("bash", 10));
            var verdict = Audit(before, skilled);
            Assert.IsTrue(verdict.Refused, "an Initiate with Bash 10");
            StringAssert.Contains("another line", verdict.Summary);

            var points = before.Clone();
            points.SkillPoints = 500;
            Assert.IsTrue(Audit(before, points).Refused, "500 skill points at Job Level 1");
        }

        [Test]
        public void BrokenItems_AreRefused()
        {
            var before = Fresh();
            var unknown = before.Clone();
            unknown.Inventory.Add(new ItemStack("not_a_real_item", 1));
            Assert.IsTrue(Audit(before, unknown, economy: false).Refused);

            var overRefined = before.Clone();
            var sword = ItemCatalog.All.First(i => i.IsWeapon && i.Refinable && i.Sockets == 1 && RealmGuard.IsClientMade(i.Id));
            var piece = ItemStack.NewInstance(sword);
            piece.Refine = 25;
            overRefined.Inventory.Add(piece);
            Assert.IsTrue(Audit(before, overRefined, economy: false).Refused, "+25");

            var overCarded = before.Clone();
            var socketed = ItemStack.NewInstance(sword);
            var card = DropOnlyCard();
            socketed.Cards = new[] { card.Id, card.Id, card.Id };
            overCarded.Inventory.Add(socketed);
            Assert.IsTrue(Audit(before, overCarded, economy: false).Refused, "three cards in one socket");
        }

        [Test]
        public void ExpThatTheRealmNeverAwarded_IsRefused()
        {
            var before = Fresh();
            var after = before.Clone();
            new CharacterProgression(after).GainExperience(200000, 0);
            Assert.IsTrue(Audit(before, after).Refused, "levels from nowhere");

            var ledger = new GuardLedger();
            ledger.AddExp(60000, 0, 0);
            Assert.IsFalse(Audit(before, after, ledger).Refused, "a 4x bonus covers it");
            Assert.IsFalse(Audit(before, after, economy: false).Refused, "a realm with @commands lets GMs level people");
        }

        [Test]
        public void JobsFollowTheirPaths()
        {
            var before = Fresh();
            before.JobLevel = 10;
            var warrior = before.Clone();
            warrior.Job = JobId.Warrior;
            warrior.JobLevel = 1;
            Assert.IsFalse(Audit(before, warrior).Refused, Audit(before, warrior).Summary);

            var lordKnight = before.Clone();
            lordKnight.Job = JobId.Einherjar;
            lordKnight.JobLevel = 1;
            Assert.IsTrue(Audit(before, lordKnight).Refused, "an Initiate can't become an Einherjar");
        }

        [Test]
        public void ConjuredItems_AreRefused_ButShopGoodsAndOpenedChestsAreFine()
        {
            var before = Fresh();
            var card = DropOnlyCard();
            var spawned = before.Clone();
            new Inventory(spawned.Inventory).Add(card.Id, 3);
            var verdict = Audit(before, spawned);
            Assert.IsTrue(verdict.Refused);
            StringAssert.Contains("never handed over", verdict.Summary);

            var bought = before.Clone();
            new Inventory(bought.Inventory).Add(ItemCatalog.LingonberryTonic, 50);
            Assert.IsFalse(Audit(before, bought).Refused, "shop goods are the player's own business");

            var chest = ItemCatalog.FindByName("Elite Hoard Chest");
            Assume.That(chest != null && chest.RandomGives != null && chest.RandomGives.Length > 0);
            var prize = ItemCatalog.Get(chest.RandomGives[0].ItemId);
            var holding = before.Clone();
            new Inventory(holding.Inventory).Add(chest.Id, 1);
            var opened = before.Clone();
            if (prize.IsStackable)
            {
                new Inventory(opened.Inventory).Add(prize.Id, 1);
            }
            else
            {
                opened.Inventory.Add(ItemStack.NewInstance(prize));
            }

            Assert.IsFalse(Audit(holding, opened).Refused, "the chest is gone and its prize is in the bag");
            Assert.IsTrue(Audit(before, opened).Refused, "the same prize with no chest to open");
        }

        [Test]
        public void KeepingWhatYouTraded_IsRefused()
        {
            var before = Fresh();
            var card = DropOnlyCard();
            new Inventory(before.Inventory).Add(card.Id, 1);
            var ledger = new GuardLedger();
            ledger.AddItemOut(card.Id, 1, 0);
            var kept = before.Clone();
            var verdict = Audit(before, kept, ledger);
            Assert.IsTrue(verdict.Refused);
            StringAssert.Contains("after handing it over", verdict.Summary);

            var gave = before.Clone();
            gave.Inventory.RemoveAll(s => s.ItemId == card.Id);
            Assert.IsFalse(Audit(before, gave, ledger).Refused);
        }

        [Test]
        public void Zeny_MustBeExplained()
        {
            var before = Fresh();
            var rich = before.Clone();
            rich.Zeny += 1000000;
            Assert.IsTrue(Audit(before, rich).Refused, "a million from nowhere");

            var ledger = new GuardLedger();
            ledger.AddZenyIn(1000000, 0);
            Assert.IsFalse(Audit(before, rich, ledger).Refused, "a stall's takings");

            var seller = before.Clone();
            var sold = seller.Clone();
            var entry = sold.Inventory.First(s => s.Definition.SellPrice > 0);
            Assert.IsTrue(TradeRules.TrySell(sold, new Inventory(sold.Inventory), entry, entry.Amount, out _, TradeRules.MaxPricePercent));
            Assert.IsFalse(Audit(seller, sold).Refused, "selling to a shop at the best price");
        }

        [Test]
        public void Storage_MovesAreFree_ButStorageCantConjure()
        {
            var before = Fresh();
            var card = DropOnlyCard();
            new Inventory(before.Inventory).Add(card.Id, 2);
            var storage = new List<ItemStack>();
            var after = before.Clone();
            after.Inventory.RemoveAll(s => s.ItemId == card.Id);
            var newStorage = new List<ItemStack> { new ItemStack(card.Id, 2) };
            Assert.IsFalse(Audit(before, after, storageBefore: storage, storageAfter: newStorage).Refused, "a deposit");

            var duped = new List<ItemStack> { new ItemStack(card.Id, 4) };
            Assert.IsTrue(Audit(before, after, storageBefore: storage, storageAfter: duped).Refused, "two more than went in");
        }

        [Test]
        public void Settle_KeepsCreditsInFlight_AndUsesEachOnce()
        {
            var before = Fresh();
            var card = DropOnlyCard();
            var ledger = new GuardLedger();
            ledger.AddItemIn(card.Id, 1, 0);

            // The save left before the drop arrived: the credit waits for the next one.
            var early = before.Clone();
            Assert.IsFalse(Audit(before, early, ledger).Refused);
            RealmGuard.Settle(ledger, before, null, early, null, 1);
            Assert.AreEqual(1, ledger.In(card.Id));

            var arrived = early.Clone();
            new Inventory(arrived.Inventory).Add(card.Id, 1);
            Assert.IsFalse(Audit(early, arrived, ledger).Refused);
            RealmGuard.Settle(ledger, early, null, arrived, null, 2);
            Assert.AreEqual(0, ledger.In(card.Id), "used up");

            var again = arrived.Clone();
            new Inventory(again.Inventory).Add(card.Id, 1);
            Assert.IsTrue(Audit(arrived, again, ledger).Refused, "one drop can't be claimed twice");
        }

        [Test]
        public void UnclaimedCredits_LapseAfterOneMoreSave()
        {
            var before = Fresh();
            var card = DropOnlyCard();
            var ledger = new GuardLedger();
            ledger.AddItemIn(card.Id, 1, 0);
            RealmGuard.Settle(ledger, before, null, before.Clone(), null, 1);
            Assert.AreEqual(1, ledger.In(card.Id), "waits through one save");
            RealmGuard.Settle(ledger, before, null, before.Clone(), null, 2);
            Assert.AreEqual(0, ledger.In(card.Id), "a drop the bag never took isn't banked for later");
        }

        [Test]
        public void DamageCeiling_IsGenerous_ButStopsForgedHits()
        {
            var weak = Fresh();
            int ceiling = RealmGuard.DamageCeiling(weak);
            Assert.Greater(ceiling, 50000, "never trims a real hit");
            Assert.Less(ceiling, 10000000, "a forged million-point hit is cut down");

            var strong = Fresh();
            strong.BaseLevel = 99;
            strong.Stats.Str = 99;
            Assert.Greater(RealmGuard.DamageCeiling(strong), ceiling, "stronger characters may hit harder");
        }

        [Test]
        public void SkillBudget_FollowsTheJobPath()
        {
            var record = Fresh();
            record.Job = JobId.Berserker;
            record.JobLevel = 10;
            int expected = StatFormulas.SkillPointsAtJobLevel(10) + StatFormulas.SkillPointsAtJobLevel(JobDatabase.MaxJobLevel(JobId.Warrior))
                           + StatFormulas.SkillPointsAtJobLevel(JobDatabase.MaxJobLevel(JobId.Initiate));
            Assert.AreEqual(expected, RealmGuard.SkillPointBudget(record));
        }
    }
}
