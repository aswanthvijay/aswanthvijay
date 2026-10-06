using System;
using System.Collections.Generic;
using Runeheir.Combat;
using Runeheir.Items;

namespace Runeheir.Monsters
{
    /// <summary>
    /// MVP rewards (Ragnarok style): the player who dealt the most damage is the MVP. They get the bonus MVP EXP and one
    /// roll down the MVP drop list (the first entry that succeeds goes straight into their bag); the normal drops and the
    /// shared EXP work as for any monster.
    /// </summary>
    public static class MvpRules
    {
        /// <summary>The top damage dealer (the first one listed wins a tie). Default when nobody dealt damage.</summary>
        public static TKey PickMvp<TKey>(IEnumerable<KeyValuePair<TKey, long>> damageDealt)
        {
            TKey best = default;
            long bestDamage = -1;
            foreach (var pair in damageDealt)
            {
                if (pair.Key != null && pair.Value > bestDamage)
                {
                    best = pair.Key;
                    bestDamage = pair.Value;
                }
            }

            return best;
        }

        /// <summary>Rolls the MVP drops in order at the server item rate; returns the first that succeeds (or null).</summary>
        public static string RollMvpDrop(MonsterDefinition definition, float itemRate, IRandomSource random)
        {
            if (definition == null)
            {
                return null;
            }

            foreach (var drop in definition.MvpDrops)
            {
                var item = ItemCatalog.Get(drop.ItemId);
                if (item == null)
                {
                    continue;
                }

                float chance = Math.Min(100f, drop.ChancePercent * Math.Max(0f, itemRate));
                if (chance >= 100f || random.NextDouble() * 100.0 < chance)
                {
                    return drop.ItemId;
                }
            }

            return null;
        }
    }
}
