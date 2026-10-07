using System;
using Runeheir.Stats;

namespace Runeheir.Characters
{
    /// <summary>
    /// EXP needed per level. Generated from power curves so design can retune with two numbers.
    /// Base: 20 x L^2.65 (L99→100 ≈ 3.9M, L254→255 ≈ 47M).
    /// Job:  15 x TierMultiplier x L^2.5.
    /// Combined with <see cref="ServerRates"/> to get the "high-rate" feel.
    /// </summary>
    public static class ExperienceTable
    {
        public const double BaseCoefficient = 20.0;
        public const double BaseExponent = 2.65;
        public const double JobCoefficient = 15.0;
        public const double JobExponent = 2.5;

        /// <summary>Job EXP multiplier per tier (Initiate, 1st, 2nd, transcendent).</summary>
        public static readonly double[] JobTierMultiplier = { 1.0, 4.0, 10.0, 25.0 };

        private static readonly long[] BaseTable = BuildBaseTable();
        private static readonly long[][] JobTables = BuildJobTables();

        /// <summary>EXP needed to go from <paramref name="level"/> to level + 1. 0 at the cap.</summary>
        public static long BaseExpToNext(int level)
        {
            if (level < 1 || level >= StatFormulas.MaxBaseLevel)
            {
                return 0;
            }

            return BaseTable[level];
        }

        public static long JobExpToNext(int jobLevel, int tier, int maxJobLevel)
        {
            if (jobLevel < 1 || jobLevel >= maxJobLevel)
            {
                return 0;
            }

            int clampedTier = Math.Max(0, Math.Min(JobTables.Length - 1, tier));
            var table = JobTables[clampedTier];
            return jobLevel < table.Length ? table[jobLevel] : 0;
        }

        private static long[] BuildBaseTable()
        {
            var table = new long[StatFormulas.MaxBaseLevel + 1];
            for (int level = 1; level < StatFormulas.MaxBaseLevel; level++)
            {
                table[level] = (long)Math.Ceiling(BaseCoefficient * Math.Pow(level, BaseExponent));
            }

            return table;
        }

        private static long[][] BuildJobTables()
        {
            var tables = new long[JobTierMultiplier.Length][];
            for (int tier = 0; tier < tables.Length; tier++)
            {
                var table = new long[StatFormulas.MaxJobLevel + 1];
                for (int level = 1; level < StatFormulas.MaxJobLevel; level++)
                {
                    table[level] = (long)Math.Ceiling(JobCoefficient * JobTierMultiplier[tier] * Math.Pow(level, JobExponent));
                }

                tables[tier] = table;
            }

            return tables;
        }
    }

    /// <summary>Server multipliers (XileRO-style high rate). Set by the field bootstrap / server config.</summary>
    public sealed class ServerRates
    {
        public float BaseExp = 50f;
        public float JobExp = 50f;
        public float Drop = 5f;

        /// <summary>Soul Cards keep the GDD §6 rates (0.5–1%, MVPs 0.01%) unless a server raises this.</summary>
        public float CardDrop = 1f;

        public static ServerRates Current { get; set; } = new ServerRates();
    }
}
