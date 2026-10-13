using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Runeheir.Characters;
using Runeheir.Combat;
using Runeheir.Items;
using Runeheir.Jobs;
using Runeheir.Stats;

namespace Runeheir.Tests
{
    /// <summary>Phase 8: the XileRetro item database, imported under Norse names (Tools/ItemImport).</summary>
    public sealed class XileDatabaseTests
    {
        private static CharacterRecord Hero(JobId job, int baseLevel = 150, bool reborn = false)
        {
            var record = CharacterFactory.Create(new CharacterCreateRequest { Name = "Xile Tester" }, 0, 0);
            record.Job = job;
            record.BaseLevel = baseLevel;
            record.Reborn = reborn;
            return record;
        }

        private static EquipmentStats Wielding(int roId, int refine, Action<CharacterRecord> setup = null)
        {
            var record = Hero(JobId.Warrior);
            setup?.Invoke(record);
            var item = ItemCatalog.GetXile(roId);
            var entry = ItemStack.NewInstance(item);
            entry.Refine = refine;
            // EquipmentSet normalizes (replaces) the record's array, so find the position before writing into it.
            var position = new EquipmentSet(record, new Inventory(record.Inventory)).TargetPosition(item);
            record.Equipment[(int)position] = entry;
            return EquipmentStats.Compute(record);
        }

        private static IEnumerable<ItemDefinition> Imported => ItemCatalog.All.Where(i => i.RoId != 0);

        // ------------------------------------------------------------ the catalog
        [Test]
        public void Import_HasXileRetrosWholeEquipmentList()
        {
            Assert.GreaterOrEqual(Imported.Count(i => i.IsWeapon), 600, "weapons");
            Assert.GreaterOrEqual(Imported.Count(i => i.IsEquipment && !i.IsWeapon), 1000, "armor, headgear, garments, shoes, accessories");

            foreach (var item in Imported)
            {
                Assert.AreEqual("xr" + item.RoId, item.Id, item.Name);
                Assert.IsFalse(string.IsNullOrWhiteSpace(item.Name), item.Id);
                Assert.IsFalse(string.IsNullOrWhiteSpace(item.Description), item.Id);
                Assert.LessOrEqual(item.Sockets, 4, item.Id);
                Assert.GreaterOrEqual(item.Weight, 0, item.Id);
                if (item.IsWeapon)
                {
                    Assert.That(item.WeaponLevel, Is.InRange(1, 4), item.Id);
                }
            }

            var names = ItemCatalog.All.GroupBy(i => i.Name).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            Assert.IsEmpty(names, "every item name is unique, imported or not");
        }

        [Test]
        public void Import_RenamesUnderNorseMyth()
        {
            Assert.AreEqual("Hofud", ItemCatalog.GetXile(1137).Name, "Excalibur becomes Heimdall's sword");
            Assert.AreEqual("Mistilteinn", ItemCatalog.GetXile(1138).Name);
            Assert.AreEqual("Ullr's Bow", ItemCatalog.GetXile(1720).Name);

            string[] ragnarokOnly = { "Poring", "Prontera", "Geffen", "Morroc", "Payon", "Novice", "Swordman", "Kafra", "Santa", "Christmas",
                                      "Excalibur", "Masamune", "Muramasa", "Valentine", "kRO", "Baphomet", "Deviruchi", "Ifrit" };
            foreach (var item in Imported)
            {
                foreach (string word in ragnarokOnly)
                {
                    StringAssert.DoesNotContain(word, item.Name, $"{item.Id} keeps a Ragnarok name");
                }
            }
        }

        [Test]
        public void Aliases_LetRuneheirsOwnVersionsStandIn()
        {
            Assert.AreSame(ItemCatalog.Get("valkyrian_armor"), ItemCatalog.GetXile(2357));
            Assert.AreSame(ItemCatalog.Get("megingjard"), ItemCatalog.GetXile(2629));
            Assert.AreSame(ItemCatalog.Get("valkyrian_boots"), ItemCatalog.GetXile(2421));
            Assert.AreEqual(ItemCatalog.All.Count, ItemCatalog.All.Distinct().Count(), "aliases aren't listed twice");
        }

        [Test]
        public void Dealers_SellRagnaroksTownStock()
        {
            var smith = ShopCatalog.Get(ShopCatalog.WeaponSmith);
            var armourer = ShopCatalog.Get(ShopCatalog.Armourer);
            Assert.GreaterOrEqual(smith.ItemIds.Length, 60);
            Assert.GreaterOrEqual(armourer.ItemIds.Length, 30);
            Assert.IsTrue(smith.ItemIds.All(id => ItemCatalog.Get(id)?.IsWeapon == true), "weapons only");
            Assert.IsTrue(armourer.ItemIds.All(id => ItemCatalog.Get(id) is { IsEquipment: true, IsWeapon: false }), "armor only");
            CollectionAssert.Contains(smith.ItemIds, "xr1201", "the Knife");
            Assert.IsTrue(smith.ItemIds.Concat(armourer.ItemIds).All(id => ItemCatalog.Get(id).Price > 0));
        }

        // ------------------------------------------------------------ who can wear it
        [Test]
        public void JobsFor_MapsRagnaroksClassesOntoRuneheirsJobs()
        {
            Assert.IsNull(ItemCatalog.JobsFor(RoClass.All), "everyone");
            CollectionAssert.AreEquivalent(new[] { JobId.Berserker, JobId.Einherjar }, ItemCatalog.JobsFor(RoClass.Knight));
            CollectionAssert.AreEquivalent(new[] { JobId.Berserker }, ItemCatalog.JobsFor(RoClass.Knight, RoTier.Normal));
            CollectionAssert.AreEquivalent(new[] { JobId.Einherjar }, ItemCatalog.JobsFor(RoClass.Knight, RoTier.Upper));
            CollectionAssert.AreEquivalent(new[] { JobId.Skald, JobId.Seidkona, JobId.Thul, JobId.Volva }, ItemCatalog.JobsFor(RoClass.BardDancer));
            CollectionAssert.AreEquivalent(new[] { JobId.Warrior }, ItemCatalog.JobsFor(RoClass.Swordman, RoTier.Upper), "first jobs keep one id when reborn");

            // Every Runeheir job is some Ragnarok class.
            var covered = new HashSet<JobId>(ItemCatalog.JobsFor(RoClass.All, RoTier.Normal)
                .Concat(ItemCatalog.JobsFor(RoClass.All, RoTier.Upper)));
            foreach (var job in JobDatabase.All)
            {
                Assert.IsTrue(covered.Contains(job.Id), job.Name);
            }
        }

        [Test]
        public void JobLists_AndRebornOnlyGear_AreEnforced()
        {
            var bow = ItemCatalog.GetXile(1701);
            Assert.IsTrue(EquipmentSet.CanWear(Hero(JobId.Huntsman), bow, out string reason), reason);
            Assert.IsFalse(EquipmentSet.CanWear(Hero(JobId.Mystic), bow, out reason));

            var kronos = ItemCatalog.GetXile(2004);
            Assert.IsTrue(kronos.RebornOnly);
            Assert.IsTrue(EquipmentSet.CanWear(Hero(JobId.Archmage, reborn: true), kronos, out reason), reason);
            Assert.IsFalse(EquipmentSet.CanWear(Hero(JobId.Sorcerer), kronos, out reason), "Ragnarok's Upper classes only");
            Assert.IsFalse(EquipmentSet.CanWear(Hero(JobId.Mystic), kronos, out reason), "a first job must be reborn");
            Assert.IsTrue(EquipmentSet.CanWear(Hero(JobId.Mystic, reborn: true), kronos, out reason), reason);
        }

        // ------------------------------------------------------------ scripts
        [Test]
        public void RefineFormulas_BecomePerRefineAndThresholdBonuses()
        {
            // Glorious Rapier: INT +(refine - 5), skills cost 10% less SP from +6, INT +5 more from +9.
            Assert.AreEqual(-5, Wielding(13417, 0).Modifiers.GetStat(StatType.Int));
            Assert.AreEqual(0f, Wielding(13417, 5).Modifiers.SpCostPercent);
            Assert.AreEqual(-10f, Wielding(13417, 6).Modifiers.SpCostPercent);
            Assert.AreEqual(9, Wielding(13417, 9).Modifiers.GetStat(StatType.Int));
            Assert.AreEqual(10, Wielding(13417, 10).Modifiers.GetStat(StatType.Int));
            Assert.AreEqual(10, Wielding(13417, 15).Modifiers.GetStat(StatType.Int), "Ragnarok scripts stop at +10");

            // Kronos: Max HP +300 + 25 per refine, INT +3 + refine/2.
            Assert.AreEqual(300 + 250, Wielding(2004, 10).Modifiers.MaxHp);
            Assert.AreEqual(300 + 250, Wielding(2004, 20).Modifiers.MaxHp);
            Assert.AreEqual(3 + 3, Wielding(2004, 7).Modifiers.GetStat(StatType.Int));
        }

        [Test]
        public void BaseStatConditions_ComeFromTheScript()
        {
            // Doom Slayer: ATK +340 with base STR 95 or more.
            Assert.AreEqual(0, Wielding(1371, 0, r => r.Stats[StatType.Str] = 94).Modifiers.Atk);
            Assert.AreEqual(340, Wielding(1371, 0, r => r.Stats[StatType.Str] = 95).Modifiers.Atk);
        }

        [Test]
        public void UnsupportedBonuses_AreListedNotDropped()
        {
            Assert.IsTrue(Imported.Any(i => i.Effect != null && i.Effect.Inactive.Count > 0), "drop bonuses and the like are noted for the tooltip");
            Assert.IsTrue(Imported.Count(i => i.Effect == null || i.Effect.Inactive.Count == 0) * 100 / Imported.Count() >= 80,
                "most imported gear works in full");
        }

        [Test]
        public void AutoSpellsAndAutoBonuses_BecomeAutoCasts()
        {
            // Kronos: a small chance on attack of MATK +12% (and dearer skills) for 5 s, as a generated buff.
            var kronos = ItemCatalog.GetXile(2004).Effect.AutoCasts.Single();
            Assert.AreEqual(AutoCastTrigger.OnAttack, kronos.Trigger);
            var buff = BuffCatalog.Get(kronos.BuffId);
            Assert.IsNotNull(buff);
            Assert.AreEqual(5f, buff.Duration);
            Assert.AreEqual(12f, buff.Modifiers.MagicDamagePercent);

            // Every auto-cast names a real Runeheir skill or buff.
            foreach (var item in Imported.Where(i => i.Effect != null))
            {
                foreach (var auto in item.Effect.AutoCasts)
                {
                    Assert.IsTrue(auto.BuffId != null ? BuffCatalog.Get(auto.BuffId) != null : Skills.SkillCatalog.Get(auto.SkillId) != null, item.Id);
                    Assert.Greater(auto.ChancePercent, 0f, item.Id);
                }

                foreach (var grant in item.Effect.GrantedSkills)
                {
                    Assert.IsNotNull(Skills.SkillCatalog.Get(grant.Key), item.Id);
                }
            }
        }

        [Test]
        public void GrantedSkills_AreUsableWhileWorn()
        {
            var record = Hero(JobId.Warrior);
            var book = new Skills.SkillBook(record);
            Assert.AreEqual(0, book.UsableLevel("eirs_blessing"));
            book.SetGranted(new Dictionary<string, int> { { "eirs_blessing", 3 } });
            Assert.AreEqual(3, book.UsableLevel("eirs_blessing"), "a Warrior can heal while the gear is on");
        }

        [Test]
        public void PerfectDodge_TurnsBasicAttacksIntoMisses()
        {
            var attacker = new AttackerProfile { StatusAtk = 100, WeaponAtk = 100, Weapon = WeaponType.OneHandSword, Hit = 500, ForceCritical = true };
            var dodger = new DefenderProfile { PerfectDodge = 50f };
            Assert.IsTrue(DamageCalculator.BasicAttack(attacker, dodger, new SequenceRandom(0.1, 0.5, 0.5)).IsMiss, "even a critical");
            Assert.IsFalse(DamageCalculator.BasicAttack(attacker, dodger, new SequenceRandom(0.9, 0.5, 0.5)).IsMiss);
            Assert.AreEqual(25f, DerivedStats.Compute(1, new BaseStats(), new StatModifiers { PerfectDodge = 25f }, WeaponProfile.BareHands).PerfectDodge);
        }

        // ------------------------------------------------------------ goods (Phase 8.3)
        [Test]
        public void Goods_CoverPotionsFoodBoxesLootAndAmmo()
        {
            Assert.GreaterOrEqual(Imported.Count(i => i.Kind == ItemKind.Consumable), 1000, "potions, food, boxes, scrolls, cash items");
            Assert.GreaterOrEqual(Imported.Count(i => i.Kind == ItemKind.Etc), 1200, "loot");
            Assert.GreaterOrEqual(Imported.Count(i => i.IsAmmo), 40, "arrows, shot, shuriken");

            Assert.AreSame(ItemCatalog.Get(ItemCatalog.LingonberryTonic), ItemCatalog.GetXile(501), "the Red Potion is the Lingonberry Tonic");
            Assert.AreSame(ItemCatalog.Get(ItemCatalog.Starmetal), ItemCatalog.GetXile(984), "Oridecon is Starmetal");

            var berry = ItemCatalog.GetXile(607);
            Assert.AreEqual(100f, berry.HealHpPercent);
            Assert.AreEqual(100f, berry.HealSpPercent);
            CollectionAssert.AreEquivalent(new[] { StatusEffect.Poison, StatusEffect.Silence, StatusEffect.Blind }, ItemCatalog.GetXile(506).Cures);

            var manual = BuffCatalog.Get(ItemCatalog.GetXile(12208).BuffId);
            Assert.AreEqual(50f, manual.Modifiers.ExpPercent, "Battle Manual");
            Assert.AreEqual(1800f, manual.Duration);

            var envelope = ItemCatalog.GetXile(668);
            Assert.AreEqual(1000, envelope.GivesZenyMin);
            Assert.AreEqual(10000, envelope.GivesZenyMax);
            Assert.Greater(ItemCatalog.GetXile(603).RandomGives.Length, 100, "Old Blue Box");
        }

        [Test]
        public void Boxes_OnlyHoldRealItems()
        {
            foreach (var box in Imported.Where(i => i.GivesItems != null || i.RandomGives != null))
            {
                foreach (var grant in (box.GivesItems ?? Array.Empty<ItemGrant>()).Concat(box.RandomGives ?? Array.Empty<ItemGrant>()))
                {
                    Assert.IsNotNull(ItemCatalog.Get(grant.ItemId), $"{box.Id} holds {grant.ItemId}");
                    Assert.Greater(grant.Amount, 0, box.Id);
                }
            }
        }

        [Test]
        public void Ammunition_RidesTheMatchingWeapon_AndIsSpent()
        {
            var record = Hero(JobId.Huntsman);
            var bag = new Inventory(record.Inventory);
            var set = new EquipmentSet(record, bag);
            bag.Add("xr1702", 1); // a bow
            bag.Add("xr1750", 100); // arrows
            Assert.IsTrue(set.TryEquip(bag.FindFirst("xr1750"), out string reason), reason);
            Assert.AreEqual(100, set.Get(EquipPosition.Ammo).Amount, "the whole stack goes on");
            Assert.IsFalse(EquipmentStats.Compute(record).AmmoInUse, "no bow, no arrows");

            Assert.IsTrue(set.TryEquip(bag.FindFirst("xr1702"), out reason), reason);
            var stats = EquipmentStats.Compute(record);
            Assert.IsTrue(stats.AmmoInUse);
            Assert.AreEqual(25, stats.Modifiers.Atk, "Arrow ATK 25");

            Assert.IsTrue(set.SpendAmmo());
            Assert.AreEqual(99, set.Get(EquipPosition.Ammo).Amount);

            bag.Add("xr1750", 10);
            Assert.IsTrue(set.TryEquip(bag.FindFirst("xr1750"), out reason), reason);
            Assert.AreEqual(109, set.Get(EquipPosition.Ammo).Amount, "more of the same tops the stack up");
            Assert.IsFalse(bag.Has("xr1750"));
        }

        [Test]
        public void OldSaves_GainTheAmmunitionAndCostumeSlots()
        {
            var record = Hero(JobId.Warrior);
            record.Equipment = new ItemStack[10];
            record.EquipmentDataVersion = 1;
            EquipmentSet.SanitizeEquipment(record);
            Assert.AreEqual(15, record.Equipment.Length);
            Assert.AreEqual(EquipmentSet.Positions, record.Equipment.Length);
            Assert.AreEqual(EquipmentSet.CurrentDataVersion, record.EquipmentDataVersion);
        }

        // ------------------------------------------------------------ the new damage bonuses
        [Test]
        public void DamageBonuses_CoverBossesRacesAndSizes()
        {
            var bonuses = new DamageBonuses { VsBoss = 25f, VsNormal = 10f };
            Assert.AreEqual(1.25f, bonuses.Multiplier(new DefenderProfile { IsBoss = true }), 1e-4f);
            Assert.AreEqual(1.10f, bonuses.Multiplier(new DefenderProfile()), 1e-4f);

            bonuses.MagicVsRace[(int)Race.Undead] = 20f;
            bonuses.MagicVsElement[(int)Element.Fire] = 10f;
            Assert.AreEqual(1.2f * 1.1f, bonuses.MagicMultiplier(new DefenderProfile { Race = Race.Undead, Element = Element.Fire }), 1e-4f);

            bonuses.TakenFromSize[(int)Size.Large] = -20f;
            Assert.AreEqual(0.8f, bonuses.TakenMultiplier(Race.Beast, Element.Neutral, Size.Large), 1e-4f);
            Assert.AreEqual(1f, bonuses.TakenMultiplier(Race.Beast, Element.Neutral, Size.Small), 1e-4f);
        }

        [Test]
        public void RaceCritAndDefBypass_ApplyToPhysicalHits()
        {
            var bonuses = new DamageBonuses();
            bonuses.CritVsRace[(int)Race.Beast] = 100f;
            bonuses.DefBypassVsRace[(int)Race.Demon] = 100f;
            var attacker = new AttackerProfile { StatusAtk = 100, WeaponAtk = 100, Weapon = WeaponType.OneHandSword, Hit = 500, Bonuses = bonuses };

            var crit = DamageCalculator.Physical(attacker, new DefenderProfile { Race = Race.Beast }, 100f, true, new SequenceRandom(0.5, 0.5));
            Assert.IsTrue(crit.IsCritical, "+100 CRIT against Beasts");

            var armored = new DefenderProfile { Race = Race.Demon, Def = 2000 };
            var open = new DefenderProfile { Race = Race.Demon };
            int bypassed = DamageCalculator.Physical(attacker, armored, 100f, false, new SequenceRandom(0.0, 0.5)).Amount;
            Assert.AreEqual(DamageCalculator.Physical(attacker, open, 100f, false, new SequenceRandom(0.0, 0.5)).Amount, bypassed, "DEF ignored vs Demons");
        }

        [Test]
        public void SpCostAndHealPower_FromGear()
        {
            var modifiers = new StatModifiers { SpCostPercent = -200f, HealPowerPercent = 10f, HpRegenPercent = 50f };
            var derived = DerivedStats.Compute(50, new BaseStats(), modifiers, WeaponProfile.BareHands);
            Assert.AreEqual(-90f, derived.SpCostPercent, "SP cost floors at 10%");
            Assert.AreEqual(10f, derived.HealPowerPercent);
        }
    }
}
