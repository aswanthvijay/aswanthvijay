using System;
using Runeheir.Characters;
using Runeheir.Combat;

namespace Runeheir.Items
{
    public enum RefineOutcome
    {
        NotAllowed = 0,
        Success = 1,

        /// <summary>Failed with a Rune of Preservation: the item loses one level instead of shattering.</summary>
        Downgraded = 2,

        /// <summary>Failed without protection: the item and its cards are destroyed.</summary>
        Shattered = 3,
    }

    /// <summary>
    /// Dwarven Forge refining, +1 to +20 (GDD §7). Up to the safe limit (Lv 1 weapons +7, Lv 2 +6, Lv 3 +5, Lv 4 +4,
    /// armor +4) it always works; past it each step can fail and shatter the item, unless a Rune of Preservation is
    /// used, which turns the failure into -1.
    /// </summary>
    public static class RefineRules
    {
        public const int MaxRefine = 20;

        /// <summary>Hard DEF per refine level on armor, shields, garments, footgear and upper headgear.</summary>
        public const int DefPerRefine = 3;

        /// <summary>Chance (%) of each step past the safe limit: first step 60%, then down to 5% by +20.</summary>
        private static readonly float[] RatesPastSafe = { 60, 40, 40, 20, 20, 15, 15, 12, 12, 10, 10, 8, 8, 6, 6, 5, 5 };

        public static int SafeLimit(ItemDefinition item)
        {
            if (item == null || !item.IsWeapon)
            {
                return 4;
            }

            switch (Math.Max(1, Math.Min(4, item.WeaponLevel)))
            {
                case 1: return 7;
                case 2: return 6;
                case 3: return 5;
                default: return 4;
            }
        }

        /// <summary>Success chance in percent of refining to <paramref name="targetLevel"/>.</summary>
        public static float SuccessChance(ItemDefinition item, int targetLevel)
        {
            int past = targetLevel - SafeLimit(item);
            return past <= 0 ? 100f : RatesPastSafe[Math.Min(past, RatesPastSafe.Length) - 1];
        }

        public static string MaterialFor(ItemDefinition item)
        {
            if (item == null || !item.IsWeapon)
            {
                return ItemCatalog.Skystone;
            }

            switch (Math.Max(1, Math.Min(4, item.WeaponLevel)))
            {
                case 1: return ItemCatalog.BogIron;
                case 2: return ItemCatalog.DwarvenSteel;
                default: return ItemCatalog.Starmetal;
            }
        }

        public static int ZenyCost(ItemDefinition item)
        {
            if (item == null || !item.IsWeapon)
            {
                return 2000;
            }

            switch (Math.Max(1, Math.Min(4, item.WeaponLevel)))
            {
                case 1: return 50;
                case 2: return 200;
                case 3: return 5000;
                default: return 20000;
            }
        }

        public static bool CanRefine(CharacterRecord record, Inventory inventory, ItemStack entry, bool usePreservation, out string reason)
        {
            var item = entry?.Definition;
            if (item == null || !item.IsEquipment || !inventory.Contains(entry))
            {
                reason = "Choose a piece of equipment from your bag (unequip it first).";
                return false;
            }

            if (!item.Refinable)
            {
                reason = $"{item.Name} can't be refined.";
                return false;
            }

            if (entry.Refine >= MaxRefine)
            {
                reason = $"{item.Name} is already +{MaxRefine}.";
                return false;
            }

            string material = MaterialFor(item);
            if (!inventory.Has(material))
            {
                reason = $"You need {ItemCatalog.Get(material).Name}.";
                return false;
            }

            if (record.Zeny < ZenyCost(item))
            {
                reason = $"Refining costs {ZenyCost(item):N0} zeny.";
                return false;
            }

            if (usePreservation && !inventory.Has(ItemCatalog.RuneOfPreservation))
            {
                reason = "You have no Rune of Preservation.";
                return false;
            }

            reason = null;
            return true;
        }

        /// <summary>
        /// One refine attempt. Consumes the ore, the zeny and (if used) the Rune of Preservation, whatever the outcome.
        /// A preservation rune is only spent on steps that can fail.
        /// </summary>
        public static RefineOutcome TryRefine(CharacterRecord record, Inventory inventory, ItemStack entry, bool usePreservation, IRandomSource random, out string message)
        {
            if (!CanRefine(record, inventory, entry, usePreservation, out message))
            {
                return RefineOutcome.NotAllowed;
            }

            var item = entry.Definition;
            int target = entry.Refine + 1;
            float chance = SuccessChance(item, target);
            bool risky = chance < 100f;

            inventory.TryRemove(MaterialFor(item));
            record.Zeny -= ZenyCost(item);
            bool protectedAttempt = usePreservation && risky && inventory.TryRemove(ItemCatalog.RuneOfPreservation);

            if (!risky || random.Chance(chance))
            {
                entry.Refine = target;
                inventory.NotifyChanged();
                message = $"Success! {entry.DisplayName}.";
                return RefineOutcome.Success;
            }

            if (protectedAttempt)
            {
                entry.Refine = Math.Max(0, entry.Refine - 1);
                inventory.NotifyChanged();
                message = $"The refine failed, but the Rune of Preservation held: {entry.DisplayName}.";
                return RefineOutcome.Downgraded;
            }

            inventory.RemoveEntry(entry);
            message = $"The refine failed and {item.Name} shattered{(entry.CardCount > 0 ? ", taking its cards with it" : string.Empty)}.";
            return RefineOutcome.Shattered;
        }
    }
}
