using System;
using System.Collections.Generic;
using Runeheir.Combat;
using Runeheir.Items;
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

        /// <summary>
        /// Picks one drop, weighted by the drop table's own chances. Soul Cards can never be stolen (Ragnarok rule).
        /// Null when nothing on the table can be stolen.
        /// </summary>
        public static string PickItem(IReadOnlyList<DropEntry> drops, IRandomSource random)
        {
            var stealable = new List<DropEntry>();
            foreach (var drop in drops ?? (IReadOnlyList<DropEntry>)Array.Empty<DropEntry>())
            {
                if (!IsUnstealable(drop.ItemId))
                {
                    stealable.Add(drop);
                }
            }

            if (stealable.Count == 0)
            {
                return null;
            }

            double total = 0;
            foreach (var drop in stealable)
            {
                total += Math.Max(0f, drop.ChancePercent);
            }

            if (total <= 0)
            {
                return stealable[0].ItemId;
            }

            double roll = random.NextDouble() * total;
            foreach (var drop in stealable)
            {
                roll -= Math.Max(0f, drop.ChancePercent);
                if (roll < 0)
                {
                    return drop.ItemId;
                }
            }

            return stealable[stealable.Count - 1].ItemId;
        }

        public static bool IsUnstealable(string itemId)
        {
            var item = ItemCatalog.Get(itemId);
            return item != null && item.IsCard;
        }
    }
}
