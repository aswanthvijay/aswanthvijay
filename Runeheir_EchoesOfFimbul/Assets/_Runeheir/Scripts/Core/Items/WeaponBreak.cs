using System;
using System.Collections.Generic;
using Runeheir.Characters;
using Runeheir.Combat;

namespace Runeheir.Items
{
    /// <summary>
    /// Phase 5 weapon breaking: some monster skills (Axe Rend, Bone Crusher, Crushing Fist...) can smash the weapon in your
    /// hands. A broken weapon stays equipped but does nothing until Brokk repairs it. The Ancient Golem Card makes a weapon
    /// unbreakable.
    /// </summary>
    public static class WeaponBreakRules
    {
        /// <summary>
        /// Rolls <paramref name="chancePercent"/> against the worn weapon. Returns the weapon it broke, or null (no weapon,
        /// already broken, unbreakable, or the roll failed).
        /// </summary>
        public static ItemStack TryBreak(CharacterRecord record, float chancePercent, IRandomSource random)
        {
            var weapon = Worn(record);
            if (weapon == null || weapon.Broken || chancePercent <= 0f || IsUnbreakable(record))
            {
                return null;
            }

            if (!random.Chance(chancePercent))
            {
                return null;
            }

            weapon.Broken = true;
            return weapon;
        }

        /// <summary>True when a card or effect on the worn gear protects the weapon (Ancient Golem Card).</summary>
        public static bool IsUnbreakable(CharacterRecord record)
        {
            return EquipmentStats.Compute(record).WeaponUnbreakable;
        }

        private static ItemStack Worn(CharacterRecord record)
        {
            var equipment = record?.Equipment;
            int index = (int)EquipPosition.Weapon;
            var entry = equipment != null && index < equipment.Length ? equipment[index] : null;
            return entry != null && !entry.IsEmpty && entry.Definition != null && entry.Definition.IsWeapon ? entry : null;
        }
    }

    /// <summary>Brokk's repairs: every broken weapon you carry or wear, for zeny.</summary>
    public static class RepairRules
    {
        /// <summary>Zeny per weapon: 1,000 x weapon level squared (Lv 1: 1,000, Lv 4: 16,000), +10% per refine.</summary>
        public static int Cost(ItemStack stack)
        {
            var item = stack?.Definition;
            if (item == null || !item.IsWeapon || !stack.Broken)
            {
                return 0;
            }

            int level = Math.Max(1, item.WeaponLevel);
            return (int)Math.Round(1000.0 * level * level * (1.0 + 0.1 * stack.Refine));
        }

        /// <summary>Broken weapons in the bag and in the weapon slot.</summary>
        public static List<ItemStack> BrokenItems(CharacterRecord record)
        {
            var list = new List<ItemStack>();
            if (record == null)
            {
                return list;
            }

            foreach (var stack in record.Inventory ?? new List<ItemStack>())
            {
                if (stack != null && stack.Broken)
                {
                    list.Add(stack);
                }
            }

            foreach (var stack in record.Equipment ?? Array.Empty<ItemStack>())
            {
                if (stack != null && stack.Broken)
                {
                    list.Add(stack);
                }
            }

            return list;
        }

        public static int TotalCost(CharacterRecord record)
        {
            int total = 0;
            foreach (var stack in BrokenItems(record))
            {
                total += Cost(stack);
            }

            return total;
        }

        /// <summary>Repairs everything broken if the character can pay for all of it.</summary>
        public static bool TryRepairAll(CharacterRecord record, out int repaired, out string message)
        {
            repaired = 0;
            var broken = BrokenItems(record);
            if (broken.Count == 0)
            {
                message = "Nothing of yours is broken.";
                return false;
            }

            int cost = TotalCost(record);
            if (record.Zeny < cost)
            {
                message = $"Repairing {broken.Count} weapon{(broken.Count == 1 ? string.Empty : "s")} costs {cost:N0} zeny.";
                return false;
            }

            record.Zeny -= cost;
            foreach (var stack in broken)
            {
                stack.Broken = false;
            }

            repaired = broken.Count;
            message = $"Brokk repaired {repaired} weapon{(repaired == 1 ? string.Empty : "s")} for {cost:N0} zeny.";
            return true;
        }
    }
}
