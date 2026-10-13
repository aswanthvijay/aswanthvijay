using System.Linq;
using NUnit.Framework;
using Runeheir.Characters;
using Runeheir.Combat;
using Runeheir.Homunculi;
using Runeheir.Items;
using Runeheir.Jobs;
using Runeheir.Skills;

namespace Runeheir.Tests
{
    /// <summary>Phase 7's last pieces: the Skald and Seidkona duets, and the Brewmaster's homunculi.</summary>
    public sealed class CompanionTests
    {
        // ------------------------------------------------------------ duets
        [Test]
        public void Duets_ExistForBothSides_WithAShareKey()
        {
            var duets = SkillCatalog.All.Where(s => s.Special == SkillSpecial.Ensemble).ToList();
            Assert.AreEqual(16, duets.Count, "eight duets, each with a Skald half and a Seidkona half");
            foreach (var group in duets.GroupBy(s => s.EnsembleKey))
            {
                Assert.AreEqual(2, group.Count(), group.Key);
                Assert.IsTrue(group.Any(s => s.Job == JobId.Skald && (s.Weapons & WeaponMask.Instrument) != 0), group.Key);
                Assert.IsTrue(group.Any(s => s.Job == JobId.Seidkona && (s.Weapons & WeaponMask.Whip) != 0), group.Key);
                var half = group.First();
                Assert.IsTrue(half.BuffId != null || half.DebuffId != null || half.Status != StatusEffect.None, $"{group.Key} does something");
                if (half.BuffId != null)
                {
                    Assert.IsNotNull(BuffCatalog.Get(half.BuffId), half.BuffId);
                }

                if (half.DebuffId != null)
                {
                    Assert.IsNotNull(BuffCatalog.Get(half.DebuffId), half.DebuffId);
                }
            }

            Assert.AreEqual(-100f, BuffCatalog.Get(SkillBuffs.Ensemble).Modifiers.MoveSpeedPercent, "duet performers can't move");
        }

        [Test]
        public void Duets_PairASkaldWithASeidkona()
        {
            Assert.IsTrue(EnsembleRules.ArePartners(JobId.Skald, JobId.Seidkona));
            Assert.IsTrue(EnsembleRules.ArePartners(JobId.Volva, JobId.Thul));
            Assert.IsTrue(EnsembleRules.ArePartners(JobId.Seidkona, JobId.Thul));
            Assert.IsFalse(EnsembleRules.ArePartners(JobId.Skald, JobId.Thul), "two singers aren't a duet");
            Assert.IsFalse(EnsembleRules.ArePartners(JobId.Seidkona, JobId.Warrior));
            Assert.AreEqual("seidkona_lokis_veil", EnsembleRules.HalfFor(JobId.Volva, "lokis_veil").Id);
            Assert.AreEqual("skald_lokis_veil", EnsembleRules.HalfFor(JobId.Thul, "lokis_veil").Id);
            Assert.IsNull(EnsembleRules.HalfFor(JobId.Warrior, "lokis_veil"));
        }

        [Test]
        public void Duets_AreNotCopiedByLokisMimicry()
        {
            Assert.IsFalse(MimicryRules.CanCopy(SkillCatalog.Get("skald_mead_of_plenty")));
        }

        // ------------------------------------------------------------ homunculi
        private static CharacterRecord Brewmaster()
        {
            var record = CharacterFactory.Create(new CharacterCreateRequest { Name = "Weaver" }, 0, 0);
            record.Job = JobId.Brewmaster;
            record.BaseLevel = 80;
            return record;
        }

        [Test]
        public void WeaveLife_And_BreathOfIdunn_AreBrewmasterAndLifeweaverSkills()
        {
            Assert.AreEqual(JobId.Brewmaster, SkillCatalog.Get("weave_life").Job);
            Assert.AreEqual(SkillSpecial.Homunculus, SkillCatalog.Get("weave_life").Special);
            Assert.AreEqual(JobId.Lifeweaver, SkillCatalog.Get("breath_of_idunn").Job);
            Assert.AreEqual(SkillSpecial.HomunculusRevive, SkillCatalog.Get("breath_of_idunn").Special);
        }

        [Test]
        public void TheEmbryo_IsBrewed_FromWhatBrokkSells()
        {
            var recipe = CraftingRules.Get(CraftKind.Brew, HomunculusRules.EmbryoItemId);
            Assert.IsNotNull(recipe, "an Embryo recipe");
            var forge = ShopCatalog.Get(ShopCatalog.ForgeSupplies).ItemIds;
            foreach (var material in recipe.Materials)
            {
                Assert.IsNotNull(material.Definition, material.ItemId);
                Assert.Contains(material.ItemId, forge, $"Brokk sells {material.Definition.Name}");
            }
        }

        [Test]
        public void Weave_GivesOneOfFour_ThatGrowsWithItsLevel()
        {
            var kinds = Enumerable.Range(0, 4).Select(roll => HomunculusRules.Weave(Brewmaster(), roll).Kind).Distinct().Count();
            Assert.AreEqual(4, kinds, "Líf, Bjarki, Valfugl and Gandr");

            var record = Brewmaster();
            var state = HomunculusRules.Weave(record, 1);
            Assert.AreEqual(HomunculusKind.Bjarki, state.Kind);
            Assert.AreSame(state, HomunculusRules.Of(record));
            var young = HomunculusRules.Build(state);
            state.Level = 120;
            var grown = HomunculusRules.Build(state);
            Assert.Greater(grown.MaxHp, young.MaxHp * 3, "a level-120 Bjarki is far tougher");
            Assert.Greater(grown.AtkMax, young.AtkMax);
            Assert.Greater(grown.Skills.Count, young.Skills.Count, "it learns skills as it grows");
            Assert.IsTrue(grown.SummonOnly, "never a wild monster");
        }

        [Test]
        public void Kinds_HaveTheirBuilds()
        {
            var lif = HomunculusRules.Build(new HomunculusState { Kind = HomunculusKind.Lif, Name = "Líf", Level = 50 });
            var bjarki = HomunculusRules.Build(new HomunculusState { Kind = HomunculusKind.Bjarki, Name = "Bjarki", Level = 50 });
            var valfugl = HomunculusRules.Build(new HomunculusState { Kind = HomunculusKind.Valfugl, Name = "Valfugl", Level = 50 });
            Assert.Greater(bjarki.MaxHp, lif.MaxHp, "Bjarki tanks");
            Assert.Greater(valfugl.AtkMax, lif.AtkMax, "Valfugl strikes");
            Assert.IsTrue(lif.Skills.Any(s => s.Kind == Monsters.MonsterSkillKind.Heal), "Líf heals");
        }

        [Test]
        public void Homunculi_GainLevels_FromTheirShare()
        {
            var state = new HomunculusState { Kind = HomunculusKind.Gandr, Name = "Gandr", Level = 1 };
            Assert.Less(HomunculusRules.ExpToNext(1), HomunculusRules.ExpToNext(50));
            int gained = HomunculusRules.GainExp(state, HomunculusRules.ExpToNext(1) + HomunculusRules.ExpToNext(2));
            Assert.AreEqual(2, gained);
            Assert.AreEqual(3, state.Level);
            Assert.AreEqual(0, HomunculusRules.GainExp(state, 0));
        }

        [Test]
        public void Homunculus_SurvivesTheSaveCopy_AndSanitize()
        {
            var record = Brewmaster();
            var state = HomunculusRules.Weave(record, 2);
            state.Level = 9999;
            state.RestSeconds = 99999f;
            var copy = record.Clone();
            copy.Sanitize();
            Assert.AreEqual(HomunculusKind.Valfugl, copy.Homunculus.Kind);
            Assert.AreEqual(HomunculusRules.MaxLevel, copy.Homunculus.Level);
            Assert.AreEqual(HomunculusRules.RestAfterFalling, copy.Homunculus.RestSeconds);
            Assert.AreNotSame(record.Homunculus, copy.Homunculus);

            copy.Homunculus.Name = null;
            copy.Sanitize();
            Assert.IsNull(HomunculusRules.Of(copy), "an unnamed homunculus is none at all (old saves)");
        }
    }
}
