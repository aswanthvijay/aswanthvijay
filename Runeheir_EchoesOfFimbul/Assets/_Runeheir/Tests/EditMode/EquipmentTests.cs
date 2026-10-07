using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Runeheir.Accounts;
using Runeheir.Characters;
using Runeheir.Combat;
using Runeheir.Items;
using Runeheir.Jobs;
using Runeheir.Monsters;
using Runeheir.Skills;
using Runeheir.Stats;

namespace Runeheir.Tests
{
    public sealed class EquipmentTests
    {
        private static CharacterRecord NewRecord(JobId job = JobId.Initiate, int baseLevel = 1)
        {
            var record = CharacterFactory.Create(new CharacterCreateRequest { Name = "Gear Tester" }, 0, 0);
            record.Job = job;
            record.BaseLevel = baseLevel;
            record.Zeny = 10000000;
            return record;
        }

        private static ItemStack Give(Inventory bag, string id)
        {
            Assert.AreEqual(1, bag.Add(id, 1), id);
            return bag.Stacks.Last(s => s.ItemId == id);
        }

        // ------------------------------------------------------------ catalog (GDD §5, §6)
        [Test]
        public void Catalog_Has87BaseItemsAnd35Cards()
        {
            var equipment = ItemCatalog.All.Where(i => i.IsEquipment).ToList();
            Assert.AreEqual(87 + 29, equipment.Count, "GDD §5: 87 base items, plus the Phase 7 roster's 29 weapons");
            Assert.AreEqual(28 + 29, equipment.Count(i => i.IsWeapon), "28 GDD weapons + 29 roster weapons");
            Assert.AreEqual(24, equipment.Count(i => i.EquipKind == EquipKind.Headgear), "24 headgears");
            Assert.AreEqual(4, equipment.Count(i => i.Name.EndsWith("Wings")), "4 animated wings");
            Assert.AreEqual(35, ItemCatalog.All.Count(i => i.IsCard), "GDD §6: 35 Soul Cards");

            foreach (var item in equipment)
            {
                Assert.LessOrEqual(item.Sockets, item.IsWeapon ? 4 : 1, item.Id);
                Assert.Greater(item.Price, 0, item.Id);
                if (item.IsWeapon)
                {
                    Assert.That(item.WeaponLevel, Is.InRange(1, 4), item.Id);
                    Assert.Greater(item.Atk, 0, item.Id);
                }
            }

            foreach (var card in ItemCatalog.All.Where(i => i.IsCard))
            {
                Assert.AreNotEqual(EquipKind.None, card.CardTarget, card.Id);
                Assert.IsNotNull(card.Effect, card.Id);
            }

            foreach (string glyph in RunewordRules.GlyphNames.Select(g => g.Key))
            {
                Assert.IsNotNull(ItemCatalog.Get(glyph), glyph);
            }

            foreach (var shop in ShopCatalog.All)
            {
                Assert.IsTrue(shop.ItemIds.All(id => ItemCatalog.Get(id) != null && ItemCatalog.Get(id).Price > 0), shop.Id);
            }
        }

        [Test]
        public void EveryJob_CanWieldItsStarterWeapon()
        {
            foreach (var job in JobDatabase.All)
            {
                if (job.StarterWeaponId == null)
                {
                    Assert.AreEqual(WeaponMask.Unarmed, job.AllowedWeapons, $"{job.Name} has no gift weapon, so it fights bare-handed");
                    continue;
                }

                var weapon = ItemCatalog.Get(job.StarterWeaponId);
                Assert.IsNotNull(weapon, job.Name);
                Assert.IsTrue(weapon.IsWeapon, job.Name);
                var record = NewRecord(job.Id);
                Assert.IsTrue(EquipmentSet.CanWear(record, weapon, out string reason), $"{job.Name}: {reason}");
            }
        }

        // ------------------------------------------------------------ equipping
        [Test]
        public void NewCharacter_WearsTheRustySeax()
        {
            var record = NewRecord();
            Assert.AreEqual(ItemCatalog.RustySeax, record.Equipment[(int)EquipPosition.Weapon].ItemId);
            Assert.AreEqual(EquipmentSet.CurrentDataVersion, record.EquipmentDataVersion);
            Assert.IsTrue(new Inventory(record.Inventory).Has("cotton_tunic"));
            Assert.AreEqual(WeaponType.Dagger, EquipmentStats.Compute(record).Weapon.Type);
        }

        [Test]
        public void TwoHandedWeaponsAndShieldsDisplaceEachOther()
        {
            var record = NewRecord(JobId.Warrior);
            var bag = new Inventory(record.Inventory);
            var set = new EquipmentSet(record, bag);

            var buckler = Give(bag, "buckler");
            Assert.IsTrue(set.TryEquip(buckler, out string reason), reason);
            Assert.IsNotNull(set.Get(EquipPosition.Shield));
            Assert.IsNotNull(set.Get(EquipPosition.Weapon), "a one-handed dagger and a shield together");

            var claymore = Give(bag, "iron_claymore");
            Assert.IsTrue(set.TryEquip(claymore, out reason), reason);
            Assert.IsNull(set.Get(EquipPosition.Shield), "the greatsword needs both hands");
            Assert.AreSame(claymore, set.Covering(EquipPosition.Shield));
            Assert.IsTrue(bag.Contains(buckler));
            Assert.IsTrue(bag.Has(ItemCatalog.RustySeax), "the old weapon went back to the bag");

            Assert.IsTrue(set.TryEquip(buckler, out reason), reason);
            Assert.IsNull(set.Get(EquipPosition.Weapon), "equipping a shield drops the two-handed weapon");
        }

        [Test]
        public void HeadgearAndAccessorySlots()
        {
            var record = NewRecord(JobId.Warrior, 99);
            var bag = new Inventory(record.Inventory);
            var set = new EquipmentSet(record, bag);

            Assert.IsTrue(set.TryEquip(Give(bag, "feathered_beret"), out _));
            Assert.IsTrue(set.TryEquip(Give(bag, "eyepatch"), out _));
            Assert.IsTrue(set.TryEquip(Give(bag, "viking_pipe"), out _));
            Assert.IsTrue(set.TryEquip(Give(bag, "iron_helm"), out _), "full helm covers upper and mid");
            Assert.AreEqual("iron_helm", set.Get(EquipPosition.HeadUpper).ItemId);
            Assert.IsNull(set.Get(EquipPosition.HeadMid));
            Assert.AreEqual("viking_pipe", set.Get(EquipPosition.HeadLower).ItemId, "lower slot untouched");

            var ringA = Give(bag, "rune_ring");
            var ringB = Give(bag, "raven_brooch");
            var ringC = Give(bag, "ward_amulet");
            Assert.IsTrue(set.TryEquip(ringA, out _));
            Assert.IsTrue(set.TryEquip(ringB, out _));
            Assert.AreSame(ringA, set.Get(EquipPosition.Accessory1));
            Assert.AreSame(ringB, set.Get(EquipPosition.Accessory2));
            Assert.IsTrue(set.TryEquip(ringC, out _));
            Assert.AreSame(ringC, set.Get(EquipPosition.Accessory1), "a third accessory replaces the first");
            Assert.IsTrue(bag.Contains(ringA));

            Assert.IsTrue(set.TryUnequip(EquipPosition.Accessory2, out _));
            Assert.IsTrue(bag.Contains(ringB));
        }

        [Test]
        public void WearRules_LevelJobTierAndLine()
        {
            var initiate = NewRecord(JobId.Initiate, 1);
            Assert.IsFalse(EquipmentSet.CanWear(initiate, ItemCatalog.Get("chainmail"), out string reason));
            StringAssert.Contains("Base Level 40", reason);
            Assert.IsFalse(EquipmentSet.CanWear(initiate, ItemCatalog.Get("iron_claymore"), out reason), "Initiates can't wield greatswords");
            Assert.IsFalse(EquipmentSet.CanWear(NewRecord(JobId.Berserker, 120), ItemCatalog.Get("valkyrian_armor"), out reason));
            StringAssert.Contains("transcendent", reason);
            Assert.IsTrue(EquipmentSet.CanWear(NewRecord(JobId.Einherjar, 120), ItemCatalog.Get("valkyrian_armor"), out _));
            Assert.IsFalse(EquipmentSet.CanWear(NewRecord(JobId.Warrior, 99), ItemCatalog.Get("archmage_wizard_hat"), out _), "Mystic line only");
            Assert.IsTrue(EquipmentSet.CanWear(NewRecord(JobId.Archmage, 99), ItemCatalog.Get("archmage_wizard_hat"), out _));
        }

        // ------------------------------------------------------------ stats from gear
        [Test]
        public void EquipmentStats_AddDefRefineCardsAndConditions()
        {
            var record = NewRecord(JobId.Einherjar, 200);
            var bag = new Inventory(record.Inventory);
            var set = new EquipmentSet(record, bag);

            var plate = Give(bag, "runic_full_plate");
            plate.Refine = 5;
            plate.Cards[0] = "jotun_brawler_card";
            Assert.IsTrue(set.TryEquip(plate, out string reason), reason);
            var greatsword = Give(bag, "einherjar_greatsword");
            greatsword.Refine = 7;
            greatsword.Cards[0] = "forest_outlaw_card";
            Assert.IsTrue(set.TryEquip(greatsword, out reason), reason);

            var stats = EquipmentStats.Compute(record);
            Assert.AreEqual(120 + 5 * RefineRules.DefPerRefine, stats.Modifiers.Def);
            Assert.AreEqual(5f, stats.Modifiers.MaxHpPercent, 0.001f);
            Assert.AreEqual(10, stats.Modifiers.GetStat(StatType.Str), "Jotun Brawler: +10 STR");
            Assert.AreEqual(0, stats.Modifiers.Atk, "STR 1 < 180: no conditional bonus");
            Assert.AreEqual(7, stats.Weapon.Refine);
            Assert.AreEqual(230 + WeaponRules.RefineAtkBonus(4, 7), DerivedStats.Compute(200, record.Stats, stats.Modifiers, stats.Weapon).WeaponAtk);
            Assert.AreEqual(20f, stats.Bonuses.VsRace[(int)Race.DemiHuman]);

            record.Stats[StatType.Str] = 180;
            stats = EquipmentStats.Compute(record);
            Assert.AreEqual(20, stats.Modifiers.Atk, "base STR 180: +20 ATK");
            Assert.AreEqual(5f, stats.Modifiers.PhysicalDamagePercent, 0.001f);
        }

        [Test]
        public void ArmorCards_ChangeElementAndGrantImmunity()
        {
            var record = NewRecord(JobId.Warrior, 99);
            var bag = new Inventory(record.Inventory);
            var set = new EquipmentSet(record, bag);
            var tunic = bag.FindFirst("cotton_tunic");
            tunic.Cards[0] = "frost_wolf_card";
            Assert.IsTrue(set.TryEquip(tunic, out _));

            var stats = EquipmentStats.Compute(record);
            Assert.AreEqual(Element.Water, stats.ArmorElement);
            var resist = new StatusResistances { ImmunityMask = stats.ImmunityMask };
            Assert.IsTrue(resist.IsImmuneTo(StatusEffect.Freeze));
            Assert.AreEqual(0f, StatusRules.EffectiveChance(StatusEffect.Freeze, 100f, resist));
            Assert.Greater(StatusRules.EffectiveChance(StatusEffect.Stun, 100f, resist), 0f);
        }

        [Test]
        public void Relics_MegingjardScalesWithLevel_MjolnirMaxesAspd()
        {
            var record = NewRecord(JobId.Einherjar, 200);
            var bag = new Inventory(record.Inventory);
            var set = new EquipmentSet(record, bag);
            Assert.IsTrue(set.TryEquip(Give(bag, "megingjard"), out _));
            Assert.IsTrue(set.TryEquip(Give(bag, "mjolnir"), out _));
            var stats = EquipmentStats.Compute(record);
            Assert.AreEqual(100, stats.Modifiers.Atk, "+1 ATK per 2 Base Levels");
            Assert.AreEqual(40, stats.Modifiers.GetStat(StatType.Str));
            var derived = DerivedStats.Compute(200, record.Stats, stats.Modifiers, stats.Weapon);
            Assert.AreEqual(StatFormulas.MaxAspd, derived.Aspd);
        }

        // ------------------------------------------------------------ refining
        [Test]
        public void Refine_SafeStepsAlwaysWork_ThenFailureShatters()
        {
            var record = NewRecord(JobId.Warrior);
            var bag = new Inventory(record.Inventory);
            var tunic = bag.FindFirst("cotton_tunic");
            tunic.Cards[0] = "field_beetle_card";
            bag.Add(ItemCatalog.Skystone, 10);

            for (int i = 0; i < RefineRules.SafeLimit(tunic.Definition); i++)
            {
                Assert.AreEqual(RefineOutcome.Success, RefineRules.TryRefine(record, bag, tunic, false, new SequenceRandom(0.999), out _));
            }

            Assert.AreEqual(4, tunic.Refine);
            Assert.AreEqual(6, bag.Count(ItemCatalog.Skystone), "one ore per attempt");
            Assert.AreEqual(60f, RefineRules.SuccessChance(tunic.Definition, 5));

            Assert.AreEqual(RefineOutcome.Shattered, RefineRules.TryRefine(record, bag, tunic, false, new SequenceRandom(0.99), out string message));
            Assert.IsFalse(bag.Contains(tunic), "shattered, cards and all");
            StringAssert.Contains("cards", message);
        }

        [Test]
        public void Refine_PreservationTurnsFailureIntoMinusOne()
        {
            var record = NewRecord(JobId.Warrior);
            var bag = new Inventory(record.Inventory);
            var sandals = bag.FindFirst("sandals");
            sandals.Refine = 6;
            bag.Add(ItemCatalog.Skystone, 5);
            bag.Add(ItemCatalog.RuneOfPreservation, 1);
            long zeny = record.Zeny;

            Assert.AreEqual(RefineOutcome.Downgraded, RefineRules.TryRefine(record, bag, sandals, true, new SequenceRandom(0.99), out _));
            Assert.AreEqual(5, sandals.Refine);
            Assert.AreEqual(0, bag.Count(ItemCatalog.RuneOfPreservation), "the rune is spent");
            Assert.AreEqual(zeny - RefineRules.ZenyCost(sandals.Definition), record.Zeny);

            Assert.AreEqual(RefineOutcome.NotAllowed, RefineRules.TryRefine(record, bag, sandals, true, new SequenceRandom(0.0), out string reason));
            StringAssert.Contains("Preservation", reason);

            var ring = Give(bag, "clip_ring");
            Assert.IsFalse(RefineRules.CanRefine(record, bag, ring, false, out reason), "accessories can't be refined");
            Assert.AreEqual(ItemCatalog.BogIron, RefineRules.MaterialFor(ItemCatalog.Get(ItemCatalog.RustySeax)));
            Assert.AreEqual(ItemCatalog.Starmetal, RefineRules.MaterialFor(ItemCatalog.Get("einherjar_greatsword")));
            Assert.AreEqual(7, RefineRules.SafeLimit(ItemCatalog.Get(ItemCatalog.RustySeax)));
        }

        // ------------------------------------------------------------ cards and runewords
        [Test]
        public void Cards_CompoundIntoMatchingFreeSockets_AndExtract()
        {
            var record = NewRecord(JobId.Warrior);
            var bag = new Inventory(record.Inventory);
            var bow = Give(bag, "hunters_bow");
            bag.Add("sea_drake_card", 2);
            bag.Add("field_beetle_card", 1);

            Assert.IsFalse(CardRules.TryCompound(bag, "field_beetle_card", bow, out string message));
            StringAssert.Contains("Armor", message);
            Assert.IsTrue(CardRules.TryCompound(bag, "sea_drake_card", bow, out message), message);
            Assert.IsTrue(CardRules.TryCompound(bag, "sea_drake_card", bow, out message), message);
            Assert.AreEqual(2, bow.CardCount);
            Assert.AreEqual(0, bag.Count("sea_drake_card"));
            Assert.AreEqual("Hunter's Bow [2/3]", bow.DisplayName);

            Assert.IsFalse(CardRules.TryExtract(bag, bow, out message), "needs a Rune of Extraction");
            bag.Add(ItemCatalog.RuneOfExtraction, 1);
            Assert.IsTrue(CardRules.TryExtract(bag, bow, out message), message);
            Assert.AreEqual(2, bag.Count("sea_drake_card"));
            Assert.AreEqual(0, bow.CardCount);
            Assert.IsTrue(bag.Contains(bow));
        }

        [Test]
        public void Runewords_FormFromTwoGlyphs()
        {
            var record = NewRecord(JobId.Warrior);
            var bag = new Inventory(record.Inventory);
            var claymore = Give(bag, "iron_claymore");
            bag.Add(RunewordRules.Sowilo, 1);
            bag.Add(RunewordRules.Tiwaz, 1);
            long zeny = record.Zeny;

            Assert.IsTrue(RunewordRules.TryEtch(record, bag, claymore, 0, RunewordRules.Tiwaz, out _));
            Assert.IsNull(RunewordRules.ActiveRuneword(claymore));
            Assert.IsTrue(RunewordRules.TryEtch(record, bag, claymore, 1, RunewordRules.Sowilo, out string message));
            Assert.AreEqual("blade_of_dawn", RunewordRules.ActiveRuneword(claymore).Id, "order doesn't matter");
            StringAssert.Contains("Blade of Dawn", message);
            Assert.AreEqual(zeny - 2 * RunewordRules.EtchZeny, record.Zeny);

            var set = new EquipmentSet(record, bag);
            Assert.IsTrue(set.TryEquip(claymore, out _));
            var stats = EquipmentStats.Compute(record);
            Assert.AreEqual(Element.Holy, stats.Weapon.Element);
            Assert.AreEqual(15f, stats.Bonuses.VsRace[(int)Race.Undead]);
        }

        // ------------------------------------------------------------ trade and storage
        [Test]
        public void Trade_BuyAndSellForZeny()
        {
            var record = NewRecord();
            record.Zeny = 1000;
            var bag = new Inventory(record.Inventory);
            var shop = ShopCatalog.Get(ShopCatalog.GeneralStore);

            Assert.IsTrue(TradeRules.TryBuy(record, bag, shop, ItemCatalog.HoneyMead, 2, 10000, 0, out _));
            Assert.AreEqual(100, record.Zeny);
            Assert.IsFalse(TradeRules.TryBuy(record, bag, shop, ItemCatalog.HoneyMead, 1, 10000, 0, out string message));
            StringAssert.Contains("450", message);
            Assert.IsFalse(TradeRules.TryBuy(record, bag, shop, ItemCatalog.Starmetal, 1, 10000, 0, out _), "not sold here");
            Assert.IsFalse(TradeRules.TryBuy(record, bag, shop, ItemCatalog.LingonberryTonic, 1, 100, 100, out message), "too heavy");

            Assert.IsTrue(TradeRules.TrySell(record, bag, bag.FindFirst(ItemCatalog.HoneyMead), 5, out _));
            Assert.AreEqual(100 + 2 * 225, record.Zeny, "sold both at half price");
            Assert.IsTrue(TradeRules.TrySell(record, bag, bag.FindFirst("sandals"), 1, out _));
            Assert.IsFalse(bag.Has("sandals"));
        }

        [Test]
        public void Storage_MovesItemsBothWays()
        {
            var record = NewRecord();
            var bag = new Inventory(record.Inventory);
            var storage = new List<ItemStack>();
            var tunic = bag.FindFirst("cotton_tunic");
            tunic.Refine = 3;

            Assert.IsTrue(StorageRules.TryDeposit(bag, storage, bag.FindFirst(ItemCatalog.LingonberryTonic), 10, out _));
            Assert.IsTrue(StorageRules.TryDeposit(bag, storage, tunic, 1, out _));
            Assert.AreEqual(20, bag.Count(ItemCatalog.LingonberryTonic));
            Assert.AreEqual(2, storage.Count);

            Assert.IsTrue(StorageRules.TryWithdraw(storage, bag, storage[1], 1, 10000, 0, out _));
            Assert.AreEqual(3, bag.FindFirst("cotton_tunic").Refine, "refine travels with the item");
            Assert.IsTrue(StorageRules.TryWithdraw(storage, bag, storage[0], 4, 10000, 0, out _));
            Assert.AreEqual(6, storage[0].Amount);
        }

        [Test]
        public void AccountStore_SavesCharacterAndStorageTogether_AndRollsBack()
        {
            bool fail = false;
            var store = new AccountStore(new AccountDatabase(), _ =>
            {
                if (fail)
                {
                    throw new System.IO.IOException("disk full");
                }
            }, () => 1000);
            store.Register("courier_user", "valhalla123");
            var record = store.CreateCharacter("courier_user", 0, new CharacterCreateRequest { Name = "Storage Hero" }).Value;
            var bag = new Inventory(record.Inventory);
            var storage = store.GetStorage("courier_user").Value;
            Assert.IsTrue(StorageRules.TryDeposit(bag, storage, bag.FindFirst("cotton_tunic"), 1, out _));
            Assert.IsTrue(store.SaveCharacterAndStorage("courier_user", record, storage).Success);
            Assert.AreEqual(1, store.GetStorage("courier_user").Value.Count);
            Assert.IsFalse(new Inventory(store.GetCharacters("courier_user")[0].Inventory).Has("cotton_tunic"));

            fail = true;
            var tunic = storage[0];
            Assert.IsTrue(StorageRules.TryWithdraw(storage, bag, tunic, 1, 10000, 0, out _));
            Assert.IsFalse(store.SaveCharacterAndStorage("courier_user", record, storage).Success);
            Assert.AreEqual(1, store.GetStorage("courier_user").Value.Count, "rolled back: still in storage");
            Assert.IsFalse(new Inventory(store.GetCharacters("courier_user")[0].Inventory).Has("cotton_tunic"), "and not duplicated in the bag");
        }

        // ------------------------------------------------------------ saves
        [Test]
        public void Sanitize_MigratesOldSavesAndRepairsEquipment()
        {
            var old = new CharacterRecord { Name = "Phase Three", Job = JobId.Einherjar, BaseLevel = 150 };
            old.Inventory.Add(new ItemStack("buckler", 3));
            old.Equipment = new[] { new ItemStack("sandals", 1) }; // footgear stored in the upper-head slot
            old.Sanitize();

            Assert.AreEqual("einherjar_greatsword", old.Equipment[(int)EquipPosition.Weapon].ItemId, "Phase 2/3 characters get their job weapon");
            Assert.AreEqual(3, old.Inventory.Count(s => s.ItemId == "buckler"), "equipment never stacks");
            Assert.IsTrue(old.Inventory.Any(s => s.ItemId == "sandals"), "misplaced piece goes back to the bag");
            Assert.IsNull(old.Equipment[(int)EquipPosition.HeadUpper]);
            Assert.AreEqual(EquipmentSet.Positions, old.Equipment.Length);

            var copy = old.Clone();
            copy.Equipment[(int)EquipPosition.Weapon].Refine = 9;
            Assert.AreEqual(0, old.Equipment[(int)EquipPosition.Weapon].Refine, "clone is deep");
        }

        // ------------------------------------------------------------ combat math added for cards
        [Test]
        public void Damage_DefenderCardsAndCritDamage()
        {
            var attacker = new AttackerProfile
            {
                StatusAtk = 100, WeaponAtk = 100, Weapon = WeaponType.OneHandSword, Hit = 1000, Race = Race.DemiHuman,
            };
            var defender = new DefenderProfile { Size = Size.Medium, Race = Race.DemiHuman };
            var plain = DamageCalculator.Physical(attacker, defender, 100f, false, new SequenceRandom(0.0, 0.5));

            defender.Resist = new DamageBonuses();
            defender.Resist.TakenFromRace[(int)Race.DemiHuman] = -30f;
            var shielded = DamageCalculator.Physical(attacker, defender, 100f, false, new SequenceRandom(0.0, 0.5));
            Assert.AreEqual((int)System.Math.Round(plain.Amount * 0.7), shielded.Amount, "Draugr Footman: -30% from Demi-Humans");

            attacker.ForceCritical = true;
            defender.Resist = null;
            var crit = DamageCalculator.Physical(attacker, defender, 100f, true, new SequenceRandom(0.5));
            attacker.CritDamagePercent = 20f;
            var bigCrit = DamageCalculator.Physical(attacker, defender, 100f, true, new SequenceRandom(0.5));
            Assert.AreEqual((int)System.Math.Round(crit.Amount * 1.2), bigCrit.Amount, "Dire Wolf: +20% crit damage");
        }

        [Test]
        public void JobChange_GiftsAndEquipsTheNewJobWeaponWhenTheOldOneCantBeUsed()
        {
            var record = NewRecord(JobId.Initiate, 20);
            var bag = new Inventory(record.Inventory);
            var set = new EquipmentSet(record, bag);
            var mace = Give(bag, ItemCatalog.IronMace);
            Assert.IsTrue(set.TryEquip(mace, out string reason), reason);

            record.Job = JobId.Mystic; // Mystics can't swing maces
            Assert.AreEqual(1, set.RemoveUnwearable());
            var gift = set.GiftJobWeapon();
            Assert.AreSame(gift, set.Get(EquipPosition.Weapon));
            Assert.AreEqual(JobDatabase.Get(JobId.Mystic).StarterWeaponId, gift.ItemId);
            Assert.IsTrue(bag.Contains(mace), "the mace went back to the bag");

            record.Job = JobId.Sage;
            var second = set.GiftJobWeapon();
            Assert.AreSame(second, set.Get(EquipPosition.Weapon), "a plain starter weapon is swapped for the new job's");
            Assert.IsTrue(bag.Contains(gift));

            second.Refine = 1;
            record.Job = JobId.Chronomancer;
            var third = set.GiftJobWeapon();
            Assert.IsTrue(bag.Contains(third), "a weapon you refined stays in your hands; the gift goes to the bag");
            Assert.AreSame(second, set.Get(EquipPosition.Weapon));
        }

        [Test]
        public void MonsterDrops_AreRealItems_AndEveryFieldMonsterCanDropItsCard()
        {
            foreach (var monster in MonsterCatalog.All)
            {
                foreach (var drop in monster.Drops)
                {
                    Assert.IsNotNull(ItemCatalog.Get(drop.ItemId), $"{monster.Id} drops unknown {drop.ItemId}");
                    if (ItemCatalog.Get(drop.ItemId).IsCard)
                    {
                        Assert.That(drop.ChancePercent, Is.InRange(0.01f, 1f), $"{drop.ItemId}: GDD card rates are 0.5–1%");
                    }
                }

                if (!monster.Immortal && !monster.SummonOnly)
                {
                    Assert.IsTrue(monster.Drops.Any(d => d.ItemId == monster.Id + "_card"), $"{monster.Id} doesn't drop its own card");
                }
            }
        }

        [Test]
        public void Pilfer_NeverStealsCards()
        {
            var drops = new[] { new DropEntry("dire_wolf_card", 1000f), new DropEntry("wolf_pelt", 0.001f) };
            for (int i = 0; i < 20; i++)
            {
                Assert.AreEqual("wolf_pelt", StealRules.PickItem(drops, new SequenceRandom(i / 20.0)));
            }

            Assert.IsNull(StealRules.PickItem(new[] { new DropEntry("dire_wolf_card", 1f) }, new SequenceRandom(0.5)));
        }

        [Test]
        public void Branches_SummonNormalMonstersOrBosses()
        {
            for (int i = 0; i < 20; i++)
            {
                var dead = MonsterCatalog.PickForBranch(false, new SequenceRandom(i / 20.0));
                Assert.IsNotNull(dead);
                Assert.AreEqual(MonsterRank.Normal, dead.Rank, "Dead Branch: normal monsters only");
                Assert.IsFalse(dead.Immortal || dead.SummonOnly, dead.Id);
                var blood = MonsterCatalog.PickForBranch(true, new SequenceRandom(i / 20.0));
                Assert.IsTrue(blood.IsBoss, "Blood Branch: a mini-boss or MVP");
            }
        }

        [Test]
        public void RefineCostCheck_WorksOnWornPieces()
        {
            var record = NewRecord(JobId.Warrior);
            var bag = new Inventory(record.Inventory);
            var weapon = record.Equipment[(int)EquipPosition.Weapon];
            Assert.IsFalse(RefineRules.CanRefine(record, bag, weapon, false, out _), "worn: not in the bag");
            Assert.IsFalse(RefineRules.CheckCosts(record, bag, weapon, false, out string reason));
            StringAssert.Contains("Bog Iron", reason);
            bag.Add(ItemCatalog.BogIron, 1);
            Assert.IsTrue(RefineRules.CheckCosts(record, bag, weapon, false, out reason), reason);
        }

        [Test]
        public void StatusResistGear_StacksPerStatus()
        {
            var record = NewRecord(JobId.Warrior, 99);
            var bag = new Inventory(record.Inventory);
            var set = new EquipmentSet(record, bag);
            var goggles = ItemCatalog.All.First(i => i.IsEquipment && i.Effect != null && i.Effect.ResistStatus == StatusEffect.Freeze);
            var guard = ItemCatalog.All.First(i => i.IsEquipment && i.Effect != null && i.Effect.ResistStatus == StatusEffect.Poison);
            Assert.IsTrue(set.TryEquip(Give(bag, goggles.Id), out string reason), reason);
            Assert.IsTrue(set.TryEquip(Give(bag, guard.Id), out reason), reason);

            var stats = EquipmentStats.Compute(record);
            var resist = new StatusResistances { ExtraResist = stats.ExtraResist };
            Assert.AreEqual(goggles.Effect.ResistPercent, StatusRules.ResistPercent(StatusEffect.Freeze, resist), 0.001f);
            Assert.AreEqual(guard.Effect.ResistPercent, StatusRules.ResistPercent(StatusEffect.Poison, resist), 0.001f);
            Assert.AreEqual(0f, StatusRules.ResistPercent(StatusEffect.Stun, resist), 0.001f);
        }

        [Test]
        public void FullBag_NeverDestroysGear()
        {
            var record = NewRecord(JobId.Initiate, 20);
            var bag = new Inventory(record.Inventory);
            var set = new EquipmentSet(record, bag);
            var seax = set.Get(EquipPosition.Weapon);
            seax.Refine = 7;
            while (bag.Stacks.Count < Inventory.MaxEntries)
            {
                bag.Add("sandals", 1);
            }

            // Devotees can't use daggers: the +7 Seax comes off into the over-full bag and the gift still arrives.
            record.Job = JobId.Devotee;
            Assert.AreEqual(1, set.RemoveUnwearable());
            Assert.IsTrue(bag.Contains(seax));
            var gift = set.GiftJobWeapon();
            Assert.AreSame(gift, set.Get(EquipPosition.Weapon));

            // A save repair with a full bag keeps misplaced gear too.
            var hat = ItemStack.NewInstance(ItemCatalog.Get("feathered_beret"));
            record.Equipment[(int)EquipPosition.Footgear] = hat;
            record.Sanitize();
            Assert.IsTrue(record.Inventory.Contains(hat), "moved to the bag, not deleted");
            Assert.IsTrue(record.Inventory.Any(s => s.ItemId == ItemCatalog.RustySeax && s.Refine == 7));
        }

        [Test]
        public void Storage_RejectsZeroOrNegativeAmounts_AndBagMergesAllOrNothing()
        {
            var record = NewRecord();
            var bag = new Inventory(record.Inventory);
            var storage = new List<ItemStack>();
            var tonics = bag.FindFirst(ItemCatalog.LingonberryTonic);
            int before = tonics.Amount;
            Assert.IsFalse(StorageRules.TryDeposit(bag, storage, tonics, -5, out _));
            Assert.IsFalse(StorageRules.TryDeposit(bag, storage, tonics, 0, out _));
            Assert.AreEqual(0, storage.Count);
            Assert.AreEqual(before, bag.Count(ItemCatalog.LingonberryTonic));

            tonics.Amount = Inventory.MaxStack - 3;
            Assert.IsFalse(bag.AddEntry(new ItemStack(ItemCatalog.LingonberryTonic, 10)), "would overflow the stack");
            Assert.AreEqual(Inventory.MaxStack - 3, bag.Count(ItemCatalog.LingonberryTonic), "nothing was added");
            Assert.IsTrue(bag.AddEntry(new ItemStack(ItemCatalog.LingonberryTonic, 3)));
        }

        [Test]
        public void IsaShield_AbsorbsDamageThenBreaks()
        {
            var buffs = new BuffContainer();
            buffs.Apply(BuffCatalog.Get(BuffCatalog.IsaShield), now: 0);
            Assert.AreEqual(0, buffs.AbsorbDamage(3000));
            Assert.AreEqual(1000, buffs.AbsorbDamage(3000), "2,000 shield left, 1,000 gets through");
            Assert.IsFalse(buffs.Has(BuffCatalog.IsaShield));
        }
    }
}
