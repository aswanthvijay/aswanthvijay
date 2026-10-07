using NUnit.Framework;
using Runeheir.Characters;
using Runeheir.Jobs;
using Runeheir.Skills;
using Runeheir.Stats;

namespace Runeheir.Tests
{
    public sealed class ProgressionTests
    {
        private static CharacterProgression NewCharacter(JobId job = JobId.Initiate)
        {
            var record = CharacterFactory.Create(new CharacterCreateRequest { Name = "Test Hero" }, 0, 0);
            record.Job = job;
            RebirthRules.Sanitize(record); // a transcendent job is reborn, as when a save loads
            return new CharacterProgression(record);
        }

        [Test]
        public void ExperienceTable_IsPositiveIncreasingAndZeroAtCap()
        {
            long previous = 0;
            for (int level = 1; level < StatFormulas.MaxBaseLevel; level++)
            {
                long next = ExperienceTable.BaseExpToNext(level);
                Assert.Greater(next, previous, $"level {level}");
                previous = next;
            }

            Assert.AreEqual(0, ExperienceTable.BaseExpToNext(StatFormulas.MaxBaseLevel));
            Assert.AreEqual(0, ExperienceTable.JobExpToNext(120, 3, 120));
            Assert.Greater(ExperienceTable.JobExpToNext(40, 3, 120), ExperienceTable.JobExpToNext(40, 1, 50), "higher tiers need more job EXP");
            Assert.AreEqual(0, ExperienceTable.JobExpToNext(10, 0, 10), "Initiate caps at Job 10");
        }

        [Test]
        public void GainExperience_LevelsUpAndGrantsStatPoints()
        {
            var progression = NewCharacter();
            int levelsSeen = 0;
            progression.BaseLevelUp += _ => levelsSeen++;

            long toLevel10 = 0;
            for (int level = 1; level < 10; level++)
            {
                toLevel10 += ExperienceTable.BaseExpToNext(level);
            }

            var result = progression.GainExperience(toLevel10, 0);

            Assert.AreEqual(10, progression.Record.BaseLevel);
            Assert.AreEqual(9, result.BaseLevelsGained);
            Assert.AreEqual(1, levelsSeen, "one event per gain, not per level");
            Assert.AreEqual(StatFormulas.TotalStatPointsAtLevel(10), progression.Record.StatPoints);
            Assert.AreEqual(0, progression.Record.BaseExp);
        }

        [Test]
        public void GainExperience_StopsAtBase99_UntilReborn()
        {
            var progression = NewCharacter();
            progression.GainExperience(long.MaxValue / 4, 0);

            Assert.AreEqual(RebirthRules.NormalBaseLevelCap, progression.Record.BaseLevel);
            Assert.AreEqual(0, progression.Record.BaseExp);
            Assert.AreEqual(StatFormulas.TotalStatPointsAtLevel(99), progression.Record.StatPoints);
        }

        [Test]
        public void GainExperience_StopsAtBase255_WhenReborn()
        {
            var progression = NewCharacter(JobId.Einherjar);
            progression.GainExperience(long.MaxValue / 4, 0);

            Assert.AreEqual(StatFormulas.MaxBaseLevel, progression.Record.BaseLevel);
            Assert.AreEqual(0, progression.Record.BaseExp);
            Assert.AreEqual(StatFormulas.TotalStatPointsAtLevel(255) + RebirthRules.BonusStatPoints, progression.Record.StatPoints);
        }

        [Test]
        public void GainExperience_SaturatesInsteadOfOverflowing()
        {
            var progression = NewCharacter(JobId.Einherjar);
            progression.Record.BaseExp = 10;
            progression.Record.JobExp = 10;
            progression.GainExperience(long.MaxValue, long.MaxValue);

            Assert.AreEqual(StatFormulas.MaxBaseLevel, progression.Record.BaseLevel);
            Assert.AreEqual(StatFormulas.MaxJobLevel, progression.Record.JobLevel);
            Assert.GreaterOrEqual(progression.Record.BaseExp, 0);
            Assert.GreaterOrEqual(progression.Record.JobExp, 0);
        }

        [Test]
        public void NegativeSavedExp_IsRepaired_AndNeverSaturates()
        {
            var progression = NewCharacter();
            progression.Record.BaseExp = -1;
            progression.Record.JobExp = -5;
            progression.GainExperience(10, 3);
            Assert.AreEqual(1, progression.Record.BaseLevel, "a corrupt negative EXP must not jump to Base 255");
            Assert.AreEqual(1, progression.Record.JobLevel);

            var record = new CharacterRecord { Name = "Corrupt", BaseExp = -1, JobExp = -1 };
            record.Sanitize();
            Assert.AreEqual(0, record.BaseExp);
            Assert.AreEqual(0, record.JobExp);
        }

        [Test]
        public void SetJobLevel_KeepsSkillPointsFromEarlierJobs()
        {
            var progression = NewCharacter();
            progression.GainExperience(0, long.MaxValue / 4); // Initiate Job 10: 9 points
            new SkillBook(progression.Record).SetLevel(SkillBook.BasicTrainingId, 9); // GM-set: keeps the 9 points
            Assert.IsTrue(progression.TryChangeJob(JobId.Warrior, out _));
            progression.SetJobLevel(2);
            Assert.AreEqual(10, progression.Record.SkillPoints);
        }

        [Test]
        public void LevelUp_NeverGrantsPointsWhileStatsCostMoreThanTheLevelGives()
        {
            var progression = NewCharacter();
            progression.SetBaseLevel(99);
            progression.SetAllStats(99);
            progression.SetBaseLevel(1);
            Assert.AreEqual(0, progression.Record.StatPoints);

            progression.GainExperience(ExperienceTable.BaseExpToNext(1), 0);
            Assert.AreEqual(2, progression.Record.BaseLevel);
            Assert.AreEqual(0, progression.Record.StatPoints, "still in debt after one level");
        }

        [Test]
        public void JobLevel_CapsPerTier()
        {
            var initiate = NewCharacter();
            initiate.GainExperience(0, long.MaxValue / 4);
            Assert.AreEqual(10, initiate.Record.JobLevel, "Initiate caps at Job 10");

            var einherjar = NewCharacter(JobId.Einherjar);
            einherjar.GainExperience(0, long.MaxValue / 4);
            Assert.AreEqual(StatFormulas.MaxJobLevel, einherjar.Record.JobLevel, "Ascended caps at Job 120");
            Assert.AreEqual(119, einherjar.Record.SkillPoints);
        }

        [Test]
        public void RaiseStat_SpendsPointsAndRespectsCap()
        {
            var progression = NewCharacter();
            int before = progression.Record.StatPoints;

            Assert.IsTrue(progression.TryRaiseStat(StatType.Agi));
            Assert.AreEqual(2, progression.Record.Stats.Agi);
            Assert.AreEqual(before - 2, progression.Record.StatPoints);

            progression.SetStat(StatType.Agi, 255);
            Assert.IsFalse(progression.CanRaiseStat(StatType.Agi), "255 cap");
        }

        [Test]
        public void IncrementalLeveling_MatchesRecalculatedPoints()
        {
            var progression = NewCharacter();
            progression.TryRaiseStat(StatType.Str);
            progression.TryRaiseStat(StatType.Str);
            progression.GainExperience(50000000, 0);

            int incremental = progression.Record.StatPoints;
            progression.RecalculateStatPoints();
            Assert.AreEqual(incremental, progression.Record.StatPoints);
        }

        [Test]
        public void ResetStats_RefundsEverything()
        {
            var progression = NewCharacter();
            progression.SetBaseLevel(99);
            progression.SetAllStats(80);
            progression.ResetStats();

            Assert.AreEqual(1, progression.Record.Stats.Str);
            Assert.AreEqual(StatFormulas.TotalStatPointsAtLevel(99), progression.Record.StatPoints);
        }

        [Test]
        public void JobChange_RequiresBranchAndJobLevel()
        {
            var progression = NewCharacter();
            Assert.IsFalse(progression.TryChangeJob(JobId.Warrior, out _), "needs Job 10");

            progression.SetJobLevel(10);
            Assert.IsFalse(progression.TryChangeJob(JobId.Berserker, out _), "cannot skip a tier");
            Assert.IsFalse(progression.TryChangeJob(JobId.Warrior, out string reason), "needs Basic Training 9");
            StringAssert.Contains("Basic Training", reason);

            var book = new SkillBook(progression.Record);
            for (int i = 0; i < 9; i++)
            {
                Assert.IsTrue(book.TryLearn(SkillBook.BasicTrainingId, out string why), why);
            }

            Assert.AreEqual(0, progression.Record.SkillPoints, "Job 10 gives exactly the 9 points Basic Training needs");
            Assert.IsTrue(progression.TryChangeJob(JobId.Warrior, out _));
            Assert.AreEqual(1, progression.Record.JobLevel);
            Assert.AreEqual(50, progression.Job.MaxJobLevel);
        }

        [Test]
        public void JobTree_AncestorsAreInherited()
        {
            Assert.IsTrue(JobDatabase.IsSelfOrAncestor(JobId.Warrior, JobId.Einherjar));
            Assert.IsTrue(JobDatabase.IsSelfOrAncestor(JobId.Initiate, JobId.Champion));
            Assert.IsFalse(JobDatabase.IsSelfOrAncestor(JobId.Mystic, JobId.Einherjar));
            Assert.IsTrue(JobDatabase.TryParse("shadow walker", out var id));
            Assert.AreEqual(JobId.ShadowWalker, id);
            Assert.IsTrue(JobDatabase.TryParse("Chrono", out id), "GDD job-tree short name");
            Assert.AreEqual(JobId.Chronomancer, id);
            Assert.IsFalse(JobDatabase.TryParse("frost giant", out _), "not a job");
        }

        [Test]
        public void DeathPenalty_TakesOnePercentOfLevelButNeverBelowZero()
        {
            var progression = NewCharacter();
            progression.SetBaseLevel(50);
            long need = progression.BaseExpToNext;
            progression.Record.BaseExp = need / 2;

            long lost = progression.ApplyDeathPenalty(1f);

            Assert.AreEqual(need / 100, lost);
            progression.Record.BaseExp = 1;
            Assert.AreEqual(1, progression.ApplyDeathPenalty(1f));
            Assert.AreEqual(0, progression.Record.BaseExp);
        }
    }
}
