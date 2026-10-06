using System;
using System.Collections.Generic;
using Runeheir.Combat;
using Runeheir.Monsters;

namespace Runeheir.Skills
{
    /// <summary>Pilfer (Ragnarok Steal): one attempt succeeds at most once per monster.</summary>
    public static class StealRules
    {
        public const float MinChance = 1f;
        public const float MaxChance = 95f;

        /// <summary>Percent: 4 + 6 per skill level + (DEX - monster level) / 2, clamped to 1..95.</summary>
        public static float Chance(int skillLevel, int dex, int monsterLevel)
        {
            return Stats.StatFormulas.Clamp(4f + 6f * skillLevel + (dex - monsterLevel) / 2f, MinChance, MaxChance);
        }

        /// <summary>Picks one drop, weighted by the drop table's own chances. Null when the table is empty.</summary>
        public static string PickItem(IReadOnlyList<DropEntry> drops, IRandomSource random)
        {
            if (drops == null || drops.Count == 0)
            {
                return null;
            }

            double total = 0;
            foreach (var drop in drops)
            {
                total += Math.Max(0f, drop.ChancePercent);
            }

            if (total <= 0)
            {
                return drops[0].ItemId;
            }

            double roll = random.NextDouble() * total;
            foreach (var drop in drops)
            {
                roll -= Math.Max(0f, drop.ChancePercent);
                if (roll < 0)
                {
                    return drop.ItemId;
                }
            }

            return drops[drops.Count - 1].ItemId;
        }
    }
}
