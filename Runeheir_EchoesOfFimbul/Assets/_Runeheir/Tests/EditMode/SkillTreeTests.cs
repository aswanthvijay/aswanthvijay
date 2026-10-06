using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Runeheir.Characters;
using Runeheir.Combat;
using Runeheir.Jobs;
using Runeheir.Monsters;
using Runeheir.Skills;
using Runeheir.Stats;

namespace Runeheir.Tests
{
    public sealed class SkillTreeTests
    {
        private static CharacterRecord NewRecord(JobId job, int skillPoints)
        {
            var record = CharacterFactory.Create(new CharacterCreateRequest { Name = "Tree Tester" }, 0, 0);
            record.Job = job;
            record.SkillPoints = skillPoints;
            return record;
        }

        // ------------------------------------------------------------ catalog integrity
        [Test]
        public void Catalog_EveryJobHasASkillTree()
        {
            foreach (var job in JobDatabase.All)
            {
                var own = SkillCatalog.OwnedBy(job.Id);
                Assert.GreaterOrEqual(own.Count, job.Tier == 0 ? 2 : 4, $"{job.Name} needs its own skills");
                Assert.IsTrue(own.Any(s => !s.Passive), $"{job.Name} needs at least one active skill");
            }

            Assert.GreaterOrEqual(SkillCatalog.All.Count(s => !s.Hidden), 100, "Phase 3: full skill lists for all 21 jobs");
        }

        [Test]
        public void Catalog_ReferencesAreValid()
        {
            var ids = new HashSet<string>();
            foreach (var skill in SkillCatalog.All)
            {
                Assert.IsTrue(ids.Add(skill.Id), $"duplicate id {skill.Id}");
                Assert.IsFalse(string.IsNullOrEmpty(skill.Name), skill.Id);
                Assert.IsFalse(string.IsNullOrEmpty(skill.IconLabel), skill.Id);
                Assert.GreaterOrEqual(skill.MaxLevel, 1, skill.Id);
                Assert.IsTrue(JobDatabase.Exists(skill.Job), skill.Id);
                if (!skill.Hidden)
                {
                    Assert.IsFalse(string.IsNullOrEmpty(skill.Description), skill.Id);
                }

                foreach (var requirement in skill.Requires)
                {
                    var needed = SkillCatalog.Get(requirement.SkillId);
                    Assert.IsNotNull(needed, $"{skill.Id} requires unknown {requirement.SkillId}");
                    Assert.IsFalse(needed.Hidden, skill.Id);
                    Assert.IsTrue(JobDatabase.IsSelfOrAncestor(needed.Job, skill.Job), $"{skill.Id} requires {needed.Id} from another job line");
                    Assert.LessOrEqual(requirement.Level, needed.MaxLevel, skill.Id);
                }

                if (skill.BuffId != null)
                {
                    Assert.IsNotNull(BuffCatalog.Get(skill.BuffId), $"{skill.Id} buff {skill.BuffId}");
                }

                if (skill.DebuffId != null)
                {
                    Assert.IsNotNull(BuffCatalog.Get(skill.DebuffId), $"{skill.Id} debuff {skill.DebuffId}");
                    Assert.IsTrue(BuffCatalog.Get(skill.DebuffId).IsDebuff, $"{skill.DebuffId} should be marked as a debuff");
                }

                if (skill.Proc != null)
                {
                    foreach (var proc in skill.Proc.SkillIds)
                    {
                        Assert.IsNotNull(SkillCatalog.Get(proc), $"{skill.Id} procs unknown {proc}");
                    }
                }
            }

            foreach (var buff in BuffCatalog.All)
            {
                if (buff.Proc != null)
                {
                    Assert.IsTrue(buff.Proc.SkillIds.All(id => SkillCatalog.Get(id) != null), buff.Id);
                }
            }
        }

        [Test]
        public void Catalog_NumbersAreSaneAtEveryLevel()
        {
            foreach (var skill in SkillCatalog.All)
            {
                if (skill.Passive)
                {
                    Assert.IsTrue(skill.PassivePerLevel != null || skill.Proc != null, $"{skill.Id}: a passive needs bonuses or a proc");
                    continue;
                }

                for (int level = 1; level <= skill.MaxLevel; level++)
                {
                    string at = $"{skill.Id} Lv {level}";
                    Assert.GreaterOrEqual(skill.SpCost.At(level), 0f, at);
                    Assert.GreaterOrEqual(skill.CastTime.At(level), 0f, at);
                    Assert.GreaterOrEqual(skill.Cooldown.At(level), 0f, at);
                    Assert.GreaterOrEqual(skill.AfterCastDelay.At(level), 0f, at);
                    Assert.GreaterOrEqual(skill.StatusChance.At(level), 0f, at);
                    Assert.LessOrEqual(skill.StatusChance.At(level), 100f, at);
                    if (skill.Damage != SkillDamage.None)
                    {
                        Assert.Greater(skill.Power.At(level), 0f, at);
                        Assert.GreaterOrEqual(skill.Hits.AtInt(level), 1, at);
                    }

                    if (skill.Target != SkillTarget.Self)
                    {
                        Assert.Greater(skill.Range.At(level), 0f, at);
                    }

                    if (skill.Area != SkillArea.Single && skill.Area != SkillArea.Line || skill.Special == SkillSpecial.Zone)
                    {
                        Assert.Greater(skill.Radius.At(level), 0f, $"{at}: area skills need a radius");
                    }

                    if (skill.Special == SkillSpecial.Zone)
                    {
                        Assert.Greater(skill.ZoneDuration.At(level), 0f, at);
                        Assert.AreEqual(SkillTarget.Ground, skill.Target, at);
                    }
                }
            }
        }

        [Test]
        public void EveryJob_CanLearnItsWholeTree()
        {
            foreach (var job in JobDatabase.All)
            {
                var record = NewRecord(job.Id, 10000);
                var book = new SkillBook(record);
                bool progress = true;
                while (progress)
                {
                    progress = false;
                    foreach (var skill in SkillCatalog.ForJob(job.Id))
                    {
                        while (book.TryLearn(skill.Id, out _))
                        {
                            progress = true;
                        }
                    }
                }

                foreach (var skill in SkillCatalog.ForJob(job.Id))
                {
                    Assert.AreEqual(skill.MaxLevel, book.GetLevel(skill.Id), $"{job.Name} can't max {skill.Name}: prerequisites unreachable");
                }
            }
        }

        // ------------------------------------------------------------ learning rules
        [Test]
        public void NewCharacter_KnowsFirstAidForFree()
        {
            var record = NewRecord(JobId.Initiate, 0);
            Assert.AreEqual(1, SkillBook.LevelIn(record, SkillCatalog.FirstAid));
            Assert.AreEqual(SkillBook.CurrentDataVersion, record.SkillDataVersion);
        }

        [Test]
        public void Learn_SpendsPointsAndChecksPrerequisites()
        {
            var record = NewRecord(JobId.Warrior, 6);
            var book = new SkillBook(record);
            int changes = 0;
            book.Changed += () => changes++;

            Assert.IsFalse(book.CanLearn("magnum_break", out string reason));
            StringAssert.Contains("Bash Lv 5", reason);

            for (int i = 0; i < 5; i++)
            {
                Assert.IsTrue(book.TryLearn("bash", out reason), reason);
            }

            Assert.IsTrue(book.TryLearn("magnum_break", out reason), reason);
            Assert.AreEqual(0, record.SkillPoints);
            Assert.AreEqual(6, changes);
            Assert.IsFalse(book.CanLearn("bash", out reason), "no points left");
            StringAssert.Contains("No skill points", reason);
        }

        [Test]
        public void Learn_RespectsJobLineAndMaxLevel()
        {
            var book = new SkillBook(NewRecord(JobId.Mystic, 50));
            Assert.IsFalse(book.CanLearn("bash", out _), "a Mystic can't learn Warrior skills");
            Assert.IsFalse(book.CanLearn("glacial_tempest", out _), "not until Archmage");
            Assert.IsFalse(book.CanLearn(SkillCatalog.StormFistsCombo, out _), "hidden skills are not learnable");

            for (int i = 0; i < 10; i++)
            {
                Assert.IsTrue(book.TryLearn("muspel_bolt", out _));
            }

            Assert.IsFalse(book.CanLearn("muspel_bolt", out string reason));
            StringAssert.Contains("maximum", reason);
        }

        [Test]
        public void Reset_RefundsEverythingButGrantedSkills()
        {
            var record = NewRecord(JobId.Warrior, 10);
            var book = new SkillBook(record);
            for (int i = 0; i < 7; i++)
            {
                book.TryLearn("bash", out _);
            }

            book.TryLearn("sword_mastery", out _);
            Assert.AreEqual(2, record.SkillPoints);
            Assert.AreEqual(8, book.ResetAll());
            Assert.AreEqual(10, record.SkillPoints);
            Assert.AreEqual(0, book.GetLevel("bash"));
            Assert.AreEqual(1, book.GetLevel(SkillCatalog.FirstAid), "First Aid stays");
        }

        [Test]
        public void UsableLevel_RequiresTheJobLine()
        {
            var record = NewRecord(JobId.Warrior, 0);
            var book = new SkillBook(record);
            book.SetLevel("bash", 10);
            Assert.AreEqual(10, book.UsableLevel("bash"));

            record.Job = JobId.Mystic; // GM job change to another line
            Assert.AreEqual(0, book.UsableLevel("bash"));
            Assert.AreEqual(10, book.GetLevel("bash"), "still learned; usable again on a Warrior");
        }

        [Test]
        public void Sanitize_RepairsSkillLists()
        {
            var record = NewRecord(JobId.Warrior, 0);
            record.SkillDataVersion = 0;
            record.Skills = new List<LearnedSkill>
            {
                new LearnedSkill("bash", 99),
                new LearnedSkill("bash", 3),
                new LearnedSkill("not_a_skill", 1),
                new LearnedSkill(SkillCatalog.StormFistsCombo, 1),
                new LearnedSkill("provoke", 0),
                null,
            };

            record.Sanitize();
            Assert.AreEqual(10, SkillBook.LevelIn(record, "bash"), "clamped to max, duplicate dropped");
            Assert.AreEqual(1, SkillBook.LevelIn(record, SkillCatalog.FirstAid), "granted skills restored");
            Assert.AreEqual(2, record.Skills.Count);
            Assert.AreEqual(SkillBook.CurrentDataVersion, record.SkillDataVersion);

            record.Skills = null;
            record.Sanitize();
            Assert.AreEqual(1, record.Skills.Count);
        }

        [Test]
        public void Clone_CopiesSkillsDeeply()
        {
            var record = NewRecord(JobId.Warrior, 0);
            new SkillBook(record).SetLevel("bash", 4);
            var copy = record.Clone();
            new SkillBook(record).SetLevel("bash", 9);
            Assert.AreEqual(4, SkillBook.LevelIn(copy, "bash"));
        }

        [Test]
        public void Passives_ApplyOnlyWithTheRightWeaponAndJob()
        {
            var record = NewRecord(JobId.Warrior, 0);
            var book = new SkillBook(record);
            book.SetLevel("sword_mastery", 10);
            book.SetLevel("iron_constitution", 4);

            var withSword = book.PassiveModifiers(WeaponType.TwoHandSword);
            Assert.AreEqual(40, withSword.Atk);
            Assert.AreEqual(12, withSword.HpRegenFlat);
            Assert.AreEqual(2f, withSword.MaxHpPercent, 0.001f);

            Assert.AreEqual(0, book.PassiveModifiers(WeaponType.Spear).Atk, "Sword Mastery needs a sword");

            record.Job = JobId.Scout;
            Assert.AreEqual(0, book.PassiveModifiers(WeaponType.TwoHandSword).Atk, "other job line");

            record.Job = JobId.Scout;
            book.SetLevel("keen_edge", 3);
            var procs = book.PassiveProcs(WeaponType.Dagger);
            Assert.AreEqual(1, procs.Count);
            Assert.AreEqual(15f, procs[0].Key.Proc.Chance.At(procs[0].Value));
            Assert.AreEqual(0, book.PassiveProcs(WeaponType.Bow).Count);
        }

        [Test]
        public void DerivedStats_IncludePassivesAndNewModifiers()
        {
            var stats = new BaseStats();
            stats.SetAll(50);
            var weapon = new WeaponProfile("Test", WeaponType.Bow, 100, 1);
            var plain = DerivedStats.Compute(50, stats, null, weapon);

            var mods = new StatModifiers { AttackRange = 2.5f, HpRegenFlat = 7, HitPercent = -25f, DefPercent = -50f, MaxPoisePercent = 50f, HyperArmor = true };
            var modded = DerivedStats.Compute(50, stats, mods, weapon);

            Assert.AreEqual(plain.AttackRange + 2.5f, modded.AttackRange, 0.001f);
            Assert.AreEqual(plain.HpRegenPerTick + 7, modded.HpRegenPerTick);
            Assert.AreEqual((int)(plain.Hit * 0.75f), modded.Hit);
            Assert.AreEqual(plain.SoftDef / 2, modded.SoftDef);
            Assert.AreEqual(plain.MaxPoise * 1.5f, modded.MaxPoise, 0.01f);
            Assert.IsTrue(modded.StaggerImmune, "hyper-armor can't be staggered");
            Assert.AreEqual(PoiseRules.PlayerMaxPoise(50, 50), plain.MaxPoise, 0.001f);
        }

        // ------------------------------------------------------------ level values, buffs
        [Test]
        public void LevelValue_LinearAndTables()
        {
            var linear = new LevelValue(130f, 30f);
            Assert.AreEqual(130f, linear.At(1));
            Assert.AreEqual(400f, linear.At(10));
            Assert.AreEqual(130f, linear.At(0), "levels below 1 read as Lv 1");

            var table = LevelValue.Table(8, 8, 15);
            Assert.AreEqual(8f, table.At(2));
            Assert.AreEqual(15f, table.At(3));
            Assert.AreEqual(15f, table.At(9), "past the end uses the last value");

            LevelValue constant = 2.5f;
            Assert.AreEqual(2.5f, constant.At(7));
            Assert.AreEqual(3, constant.AtInt(1), "rounds half away from zero");
            Assert.IsTrue(default(LevelValue).IsZero);
        }

        [Test]
        public void Buffs_ScaleByLevelAndStack()
        {
            var buffs = new BuffContainer();
            buffs.Apply(BuffCatalog.Get(SkillBuffs.OdinsBlessing), now: 0, level: 10, duration: 5f);
            Assert.AreEqual(10, buffs.Aggregate.GetStat(StatType.Str));
            Assert.AreEqual(5.0, buffs.Find(SkillBuffs.OdinsBlessing).ExpiresAt, 0.001);

            var spheres = BuffCatalog.Get(SkillBuffs.SpiritSpheres);
            for (int i = 0; i < 9; i++)
            {
                buffs.Apply(spheres, now: 0, stackLimit: 3);
            }

            Assert.AreEqual(3, buffs.StacksOf(SkillBuffs.SpiritSpheres), "Spirit Call Lv 3 holds 3 spheres");
            buffs.Apply(spheres, now: 0, stackLimit: 1);
            Assert.AreEqual(3, buffs.StacksOf(SkillBuffs.SpiritSpheres), "a Lv 1 recast never destroys spheres");
            Assert.AreEqual(9, buffs.Aggregate.Atk);
            Assert.AreEqual(2, buffs.TakeStacks(SkillBuffs.SpiritSpheres, 2));
            Assert.AreEqual(1, buffs.TakeStacks(SkillBuffs.SpiritSpheres, 5));
            Assert.IsFalse(buffs.Has(SkillBuffs.SpiritSpheres));
        }

        [Test]
        public void Buffs_StealthBreaksAndDebuffsCleanse()
        {
            var buffs = new BuffContainer();
            buffs.Apply(BuffCatalog.Get(SkillBuffs.ShadowCloak), now: 0);
            buffs.Apply(BuffCatalog.Get(SkillBuffs.ShadowVeil), now: 0);
            buffs.Apply(BuffCatalog.Get(SkillBuffs.Provoked), now: 0, level: 10);
            Assert.AreEqual(-55f, buffs.Aggregate.DefPercent, 0.001f, "Provoke Lv 10: -55% DEF");

            Assert.IsTrue(buffs.BreakStealth(out bool ambush), "attacking ends Cloak and Veil together");
            Assert.IsTrue(ambush, "Shadow Veil's ambush critical");
            Assert.IsFalse(buffs.HasTrait(BuffTraits.Stealth));
            Assert.IsFalse(buffs.BreakStealth(out ambush));
            Assert.IsFalse(ambush);

            Assert.AreEqual(1, buffs.RemoveWhere(b => b.Definition.IsDebuff));
            Assert.AreEqual(0, buffs.Active.Count);
        }

        [Test]
        public void RuneAmplify_IsHeldForTheNextSpell_NotAStandingBonus()
        {
            var buffs = new BuffContainer();
            buffs.Apply(BuffCatalog.Get(SkillBuffs.RuneAmplify), now: 0, level: 10);
            Assert.AreEqual(0f, buffs.Aggregate.MagicDamagePercent, "procs, zones and other spells don't get it");
            Assert.IsTrue(buffs.HasTrait(BuffTraits.ConsumedBySpell));
            Assert.AreEqual(1, buffs.RemoveWithTrait(BuffTraits.ConsumedBySpell));
        }

        [Test]
        public void StatModifiers_AddScaled()
        {
            var total = new StatModifiers();
            total.AddScaled(new StatModifiers { Atk = 4, Crit = 0.5f, MaxHpMultiplier = 3f, StaggerImmune = true }, 3);
            Assert.AreEqual(12, total.Atk);
            Assert.AreEqual(1.5f, total.Crit, 0.001f);
            Assert.AreEqual(7f, total.MaxHpMultiplier, 0.001f);
            Assert.IsTrue(total.StaggerImmune);

            var none = new StatModifiers();
            none.AddScaled(new StatModifiers { Atk = 4, StaggerImmune = true }, 0);
            Assert.AreEqual(0, none.Atk);
            Assert.IsFalse(none.StaggerImmune);
        }

        // ------------------------------------------------------------ steal
        [Test]
        public void Steal_ChanceAndWeightedPick()
        {
            Assert.AreEqual(64f, StealRules.Chance(10, 20, 20));
            Assert.AreEqual(StealRules.MaxChance, StealRules.Chance(10, 255, 1));
            Assert.AreEqual(StealRules.MinChance, StealRules.Chance(1, 1, 255));

            var drops = new List<DropEntry> { new DropEntry("a", 30f), new DropEntry("b", 10f) };
            Assert.AreEqual("a", StealRules.PickItem(drops, new SequenceRandom(0.5)));
            Assert.AreEqual("b", StealRules.PickItem(drops, new SequenceRandom(0.9)));
            Assert.IsNull(StealRules.PickItem(new List<DropEntry>(), new SequenceRandom(0.5)));
        }
    }
}
