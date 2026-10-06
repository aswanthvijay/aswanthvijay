using System;
using Runeheir.Jobs;
using Runeheir.Stats;

namespace Runeheir.Characters
{
    public struct ExpGainResult
    {
        public long BaseExpGained;
        public long JobExpGained;
        public int BaseLevelsGained;
        public int JobLevelsGained;
    }

    /// <summary>
    /// The Base 255 / Job 120 progression engine: EXP, level-ups, status points, stat raising,
    /// job changes. Mutates a <see cref="CharacterRecord"/> in place and raises events for UI.
    /// </summary>
    public sealed class CharacterProgression
    {
        public CharacterProgression(CharacterRecord record)
        {
            Record = record ?? throw new ArgumentNullException(nameof(record));
        }

        /// <summary>Raised once per gain with the new base level.</summary>
        public event Action<int> BaseLevelUp;

        public event Action<int> JobLevelUp;

        /// <summary>Stats, stat points or levels changed: recompute derived stats.</summary>
        public event Action StatsChanged;

        public event Action ExpChanged;

        public event Action JobChanged;

        public CharacterRecord Record { get; }

        public JobInfo Job => JobDatabase.Get(Record.Job);

        public bool IsMaxBaseLevel => Record.BaseLevel >= StatFormulas.MaxBaseLevel;

        public bool IsMaxJobLevel => Record.JobLevel >= Job.MaxJobLevel;

        public long BaseExpToNext => ExperienceTable.BaseExpToNext(Record.BaseLevel);

        public long JobExpToNext => ExperienceTable.JobExpToNext(Record.JobLevel, Job.Tier, Job.MaxJobLevel);

        public float BaseExpPercent => Percent(Record.BaseExp, BaseExpToNext);

        public float JobExpPercent => Percent(Record.JobExp, JobExpToNext);

        // ------------------------------------------------------------ EXP
        public ExpGainResult GainExperience(long baseExp, long jobExp)
        {
            var result = new ExpGainResult();

            if (baseExp > 0 && !IsMaxBaseLevel)
            {
                result.BaseExpGained = baseExp;
                Record.BaseExp = SaturatingAdd(Record.BaseExp, baseExp);
                while (!IsMaxBaseLevel && Record.BaseExp >= BaseExpToNext)
                {
                    Record.BaseExp -= BaseExpToNext;
                    Record.BaseLevel++;
                    result.BaseLevelsGained++;
                }

                if (result.BaseLevelsGained > 0)
                {
                    // Same number as adding StatPointsGainedAtLevel per level for a normal character, but never
                    // hands out points to one whose stats already cost more than its level grants (GM edits).
                    RecalculateStatPoints();
                }

                if (IsMaxBaseLevel)
                {
                    Record.BaseExp = 0;
                }
            }

            if (jobExp > 0 && !IsMaxJobLevel)
            {
                result.JobExpGained = jobExp;
                Record.JobExp = SaturatingAdd(Record.JobExp, jobExp);
                while (!IsMaxJobLevel && Record.JobExp >= JobExpToNext)
                {
                    Record.JobExp -= JobExpToNext;
                    Record.JobLevel++;
                    Record.SkillPoints++;
                    result.JobLevelsGained++;
                }

                if (IsMaxJobLevel)
                {
                    Record.JobExp = 0;
                }
            }

            if (result.BaseLevelsGained > 0)
            {
                BaseLevelUp?.Invoke(Record.BaseLevel);
            }

            if (result.JobLevelsGained > 0)
            {
                JobLevelUp?.Invoke(Record.JobLevel);
            }

            if (result.BaseLevelsGained > 0 || result.JobLevelsGained > 0)
            {
                StatsChanged?.Invoke();
            }

            if (result.BaseExpGained > 0 || result.JobExpGained > 0)
            {
                ExpChanged?.Invoke();
            }

            return result;
        }

        /// <summary>Ragnarok death penalty: lose a percent of the EXP needed for the current level.</summary>
        public long ApplyDeathPenalty(float percentOfLevel)
        {
            if (IsMaxBaseLevel || percentOfLevel <= 0f)
            {
                return 0;
            }

            long loss = Math.Min(Record.BaseExp, (long)(BaseExpToNext * (percentOfLevel / 100.0)));
            Record.BaseExp -= loss;
            ExpChanged?.Invoke();
            return loss;
        }

        // ------------------------------------------------------------ stats
        public int GetRaiseCost(StatType stat)
        {
            return StatFormulas.StatRaiseCost(Record.Stats[stat]);
        }

        public bool CanRaiseStat(StatType stat)
        {
            return Record.Stats[stat] < StatFormulas.MaxStat && Record.StatPoints >= GetRaiseCost(stat);
        }

        public bool TryRaiseStat(StatType stat)
        {
            if (!CanRaiseStat(stat))
            {
                return false;
            }

            Record.StatPoints -= GetRaiseCost(stat);
            Record.Stats[stat]++;
            StatsChanged?.Invoke();
            return true;
        }

        /// <summary>GM/debug: set a stat directly; status points are recomputed.</summary>
        public void SetStat(StatType stat, int value)
        {
            Record.Stats[stat] = StatFormulas.Clamp(value, StatFormulas.MinStat, StatFormulas.MaxStat);
            RecalculateStatPoints();
            StatsChanged?.Invoke();
        }

        public void SetAllStats(int value)
        {
            Record.Stats.SetAll(StatFormulas.Clamp(value, StatFormulas.MinStat, StatFormulas.MaxStat));
            RecalculateStatPoints();
            StatsChanged?.Invoke();
        }

        /// <summary>Stat reset: everything back to 1, all points refunded.</summary>
        public void ResetStats()
        {
            Record.Stats.SetAll(StatFormulas.MinStat);
            RecalculateStatPoints();
            StatsChanged?.Invoke();
        }

        /// <summary>Points available = points earned by this level - points spent on current stats (never below 0).</summary>
        public void RecalculateStatPoints()
        {
            int available = StatFormulas.TotalStatPointsAtLevel(Record.BaseLevel) - StatFormulas.SpentStatPoints(Record.Stats);
            Record.StatPoints = Math.Max(0, available);
        }

        private static long SaturatingAdd(long current, long amount)
        {
            return amount > long.MaxValue - current ? long.MaxValue : current + amount;
        }

        // ------------------------------------------------------------ levels (GM/debug)
        public void SetBaseLevel(int level)
        {
            int previous = Record.BaseLevel;
            Record.BaseLevel = StatFormulas.Clamp(level, 1, StatFormulas.MaxBaseLevel);
            Record.BaseExp = 0;
            RecalculateStatPoints();
            if (Record.BaseLevel > previous)
            {
                BaseLevelUp?.Invoke(Record.BaseLevel);
            }

            StatsChanged?.Invoke();
            ExpChanged?.Invoke();
        }

        public void SetJobLevel(int level)
        {
            int previous = Record.JobLevel;
            Record.JobLevel = StatFormulas.Clamp(level, 1, Job.MaxJobLevel);
            Record.JobExp = 0;

            // Change by the level difference, like leveling does: points carried over from earlier jobs stay.
            Record.SkillPoints = Math.Max(0, Record.SkillPoints + (Record.JobLevel - previous));
            if (Record.JobLevel > previous)
            {
                JobLevelUp?.Invoke(Record.JobLevel);
            }

            StatsChanged?.Invoke();
            ExpChanged?.Invoke();
        }

        // ------------------------------------------------------------ jobs
        public bool TryChangeJob(JobId target, out string reason)
        {
            if (!JobDatabase.CanChangeJob(Record.Job, Record.JobLevel, target, out reason))
            {
                return false;
            }

            ForceChangeJob(target);
            return true;
        }

        /// <summary>GM/debug job change (no requirements). Job level resets to 1.</summary>
        public void ForceChangeJob(JobId target)
        {
            Record.Job = target;
            Record.JobLevel = 1;
            Record.JobExp = 0;
            JobChanged?.Invoke();
            StatsChanged?.Invoke();
            ExpChanged?.Invoke();
        }

        private static float Percent(long value, long max)
        {
            return max <= 0 ? 0f : (float)Math.Min(100.0, value * 100.0 / max);
        }
    }
}
