using System.Linq;
using NUnit.Framework;
using Runeheir.Characters;
using Runeheir.Combat;
using Runeheir.Items;
using Runeheir.Jobs;
using Runeheir.Skills;
using Runeheir.Stats;
using Runeheir.World;

namespace Runeheir.Tests
{
    /// <summary>Phase 8 mounts: the fjord horse for anyone, Sleipnir's Foal and Gullinbursti for the knightly lines.</summary>
    public sealed class MountTests
    {
        private static CharacterRecord Rider(JobId job, params (string id, int level)[] skills)
        {
            var record = CharacterFactory.Create(new CharacterCreateRequest { Name = "Rider" }, 0, 0);
            record.Job = job;
            record.BaseLevel = 99;
            foreach (var (id, level) in skills)
            {
                record.Skills.Add(new LearnedSkill(id, level));
            }

            return record;
        }

        private static StatModifiers RidingAt(string buffId, int level)
        {
            var buffs = new BuffContainer();
            buffs.Apply(BuffCatalog.Get(buffId), 0, level, 100f);
            return buffs.Aggregate;
        }

        [Test]
        public void KnightlyLines_HaveTheirOwnMounts()
        {
            Assert.AreEqual(MountRules.Warhorse, MountRules.ClassMount(JobId.Berserker).Mount);
            Assert.AreEqual(MountRules.Warhorse, MountRules.ClassMount(JobId.Einherjar).Mount);
            Assert.AreEqual(MountRules.Boar, MountRules.ClassMount(JobId.Guardian).Mount);
            Assert.AreEqual(MountRules.Boar, MountRules.ClassMount(JobId.Valkyrie).Mount);
            Assert.IsNull(MountRules.ClassMount(JobId.Mystic).Mount);
            foreach (var job in new[] { JobId.Berserker, JobId.Guardian })
            {
                var (_, ride, mastery) = MountRules.ClassMount(job);
                Assert.AreEqual(SkillSpecial.Mount, SkillCatalog.Get(ride).Special, ride);
                Assert.AreEqual(job, SkillCatalog.Get(ride).Job, ride);
                Assert.IsTrue(SkillCatalog.Get(mastery).Passive, mastery);
            }
        }

        [Test]
        public void Riding_IsFaster_AndCavalryMasteryLiftsTheAspdPenalty()
        {
            var horse = RidingAt(SkillBuffs.MountHorse, 1);
            Assert.AreEqual(25f, horse.MoveSpeedPercent);
            Assert.AreEqual(0f, horse.AspdPercent);
            Assert.IsTrue(MountRules.BlocksCombat(MountRules.Horse), "the fjord horse isn't for fighting");
            Assert.IsFalse(MountRules.BlocksCombat(MountRules.Warhorse));

            var green = RidingAt(SkillBuffs.MountWarhorse, 1);
            Assert.AreEqual(36f, green.MoveSpeedPercent);
            Assert.AreEqual(1000, green.WeightCapacity);
            Assert.AreEqual(-50f, green.AspdPercent);
            Assert.AreEqual(0f, RidingAt(SkillBuffs.MountWarhorse, 6).AspdPercent, 1e-4f, "Cavalry Mastery 5: no penalty");
        }

        [Test]
        public void SavedMounts_NeedTheSkill_ButTheHorseIsForAnyone()
        {
            var berserker = Rider(JobId.Berserker, ("horsemanship", 1));
            Assert.IsTrue(MountRules.StillValid(berserker, MountRules.Warhorse));
            Assert.IsFalse(MountRules.StillValid(berserker, MountRules.Boar), "not the Guardians' boar");
            Assert.IsTrue(MountRules.StillValid(Rider(JobId.Mystic), MountRules.Horse));
            Assert.IsFalse(MountRules.StillValid(Rider(JobId.Berserker), MountRules.Warhorse), "unlearned");

            berserker.Mount = MountRules.Warhorse;
            var copy = berserker.Clone();
            copy.Sanitize();
            Assert.AreEqual(MountRules.Warhorse, copy.Mount);
            copy.Job = JobId.Mystic;
            copy.Sanitize();
            Assert.IsNull(copy.Mount, "a Mystic can't ride Sleipnir's Foal");
        }

        [Test]
        public void TheReins_AreSoldAtHestrsStable_AndNeverUsedUp()
        {
            var reins = ItemCatalog.Get(MountRules.ReinsItemId);
            Assert.AreEqual(ItemSpecialEffect.Mount, reins.Special);
            CollectionAssert.Contains(ShopCatalog.Get(ShopCatalog.Stable).ItemIds, reins.Id);
            Assert.IsTrue(MapCatalog.Get(MapCatalog.VigridHaven).Npcs.Any(n => n.ShopId == ShopCatalog.Stable));
        }
    }
}
