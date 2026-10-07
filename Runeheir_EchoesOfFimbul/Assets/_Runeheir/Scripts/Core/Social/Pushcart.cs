using Runeheir.Characters;
using Runeheir.Stats;

namespace Runeheir.Social
{
    /// <summary>
    /// The Merchant Pushcart (GDD §8): rented once from the cart merchant in Vigrid Haven, it adds 8,000 to the
    /// character's weight capacity and lets them set up a street stall there.
    /// </summary>
    public static class PushcartRules
    {
        public const int WeightBonus = 8000;
        public const long RentalFee = 1500;
        public const int MinBaseLevel = 10;

        /// <summary>The cart's weight bonus as a stat modifier (empty without a cart).</summary>
        public static StatModifiers Modifiers(CharacterRecord record)
        {
            var modifiers = StatModifiers.Empty();
            if (record != null && record.HasPushcart)
            {
                modifiers.WeightCapacity = WeightBonus;
            }

            return modifiers;
        }

        public static bool CanRent(CharacterRecord record, out string error)
        {
            if (record == null)
            {
                error = "No character.";
                return false;
            }

            if (record.HasPushcart)
            {
                error = "You already have a Pushcart.";
                return false;
            }

            if (record.BaseLevel < MinBaseLevel)
            {
                error = $"Pushcarts are rented from base level {MinBaseLevel}.";
                return false;
            }

            if (record.Zeny < RentalFee)
            {
                error = $"A Pushcart costs {RentalFee:N0} zeny.";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>Pays the fee and hitches the cart.</summary>
        public static bool TryRent(CharacterRecord record, out string message)
        {
            if (!CanRent(record, out message))
            {
                return false;
            }

            record.Zeny -= RentalFee;
            record.HasPushcart = true;
            message = $"Pushcart rented for {RentalFee:N0} zeny: +{WeightBonus:N0} weight capacity, and you can open a stall in Vigrid Haven.";
            return true;
        }
    }
}
