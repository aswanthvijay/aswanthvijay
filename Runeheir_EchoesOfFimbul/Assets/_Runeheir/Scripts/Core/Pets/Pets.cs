using System;
using System.Collections.Generic;
using Runeheir.Characters;
using Runeheir.Combat;
using Runeheir.Items;

namespace Runeheir.Pets
{
    /// <summary>How a pet feels about you (Ragnarok's intimacy bands).</summary>
    public enum PetIntimacy
    {
        Awkward = 0,
        Shy = 1,
        Neutral = 2,
        Cordial = 3,
        Loyal = 4,
    }

    /// <summary>A tameable monster: what tames it, the egg it becomes, what it eats, its armor and its Loyal bonus.</summary>
    public sealed class PetDefinition
    {
        public string EggItemId;
        public string MonsterId;
        public string TameItemId;
        public string FoodItemId;
        public string ArmorItemId;

        /// <summary>Base taming chance in percent against a monster at full HP.</summary>
        public float CaptureRate = 10f;

        /// <summary>Hunger a meal fills (×10 points of 100) and intimacy it gains while the pet is hungry.</summary>
        public int Fullness = 3;

        public int IntimacyFed = 50;

        /// <summary>The bonus the pet gives its owner once Loyal.</summary>
        public EquipEffect LoyalBonus;

        public string LoyalText;
    }

    /// <summary>One of a character's pets (stored on the character while it's out of its egg, or remembered for its egg).</summary>
    [Serializable]
    public sealed class PetState
    {
        public string EggItemId;
        public string Name;

        /// <summary>0–1000. 250 when first hatched; Loyal from 900.</summary>
        public int Intimacy = 250;

        /// <summary>0–100. 0 = starving (it loses intimacy), above 75 = full (feeding then annoys it).</summary>
        public int Hunger = 50;

        public string ArmorItemId;
    }

    /// <summary>Ragnarok's pet rules: taming, hatching, hunger, feeding, intimacy and the Loyal bonus.</summary>
    public static partial class PetCatalog
    {
        public const int MaxIntimacy = 1000;
        public const int LoyalAt = 900;
        public const int StartIntimacy = 250;

        /// <summary>Hunger falls by one point this often (Ragnarok: about a point a minute).</summary>
        public const float HungerTickSeconds = 60f;

        private static readonly Dictionary<string, PetDefinition> ByEgg = new Dictionary<string, PetDefinition>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, List<PetDefinition>> ByTameItem = new Dictionary<string, List<PetDefinition>>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<PetDefinition> Ordered = new List<PetDefinition>();

        static PetCatalog()
        {
            RegisterXilePets();
        }

        public static IReadOnlyList<PetDefinition> All => Ordered;

        public static PetDefinition ForEgg(string eggItemId)
        {
            return eggItemId != null && ByEgg.TryGetValue(eggItemId, out var pet) ? pet : null;
        }

        /// <summary>The pet a taming item tames from <paramref name="monsterId"/>, or null.</summary>
        public static PetDefinition ForTaming(string tameItemId, string monsterId)
        {
            if (tameItemId == null || !ByTameItem.TryGetValue(tameItemId, out var pets))
            {
                return null;
            }

            foreach (var pet in pets)
            {
                if (string.Equals(pet.MonsterId, monsterId, StringComparison.OrdinalIgnoreCase))
                {
                    return pet;
                }
            }

            return null;
        }

        public static bool IsTamingItem(string itemId)
        {
            return itemId != null && ByTameItem.ContainsKey(itemId);
        }

        private static void Register(PetDefinition pet)
        {
            ByEgg[pet.EggItemId] = pet;
            Ordered.Add(pet);
            if (!string.IsNullOrEmpty(pet.TameItemId))
            {
                if (!ByTameItem.TryGetValue(pet.TameItemId, out var list))
                {
                    ByTameItem[pet.TameItemId] = list = new List<PetDefinition>();
                }

                list.Add(pet);
            }
        }

        // ---------------------------------------------------------------- rules
        public static PetIntimacy IntimacyOf(int intimacy)
        {
            if (intimacy >= LoyalAt) return PetIntimacy.Loyal;
            if (intimacy >= 750) return PetIntimacy.Cordial;
            if (intimacy >= 250) return PetIntimacy.Neutral;
            if (intimacy >= 100) return PetIntimacy.Shy;
            return PetIntimacy.Awkward;
        }

        /// <summary>
        /// Taming chance in percent: the pet's capture rate, better the weaker the monster is (up to double at 1 HP) and a
        /// little better with LUK, never above 95%.
        /// </summary>
        public static float TameChance(PetDefinition pet, float monsterHpFraction, int luk)
        {
            float weakness = 2f - Math.Max(0f, Math.Min(1f, monsterHpFraction));
            return Math.Min(95f, pet.CaptureRate * weakness * (1f + luk / 500f));
        }

        public static PetState Active(CharacterRecord record)
        {
            if (record?.Pets == null || string.IsNullOrEmpty(record.ActivePetEgg))
            {
                return null;
            }

            foreach (var state in record.Pets)
            {
                if (state != null && string.Equals(state.EggItemId, record.ActivePetEgg, StringComparison.OrdinalIgnoreCase))
                {
                    return state;
                }
            }

            return null;
        }

        /// <summary>Hatches an egg from the bag: the egg is used up and the pet (new or remembered) comes out.</summary>
        public static bool Hatch(CharacterRecord record, Inventory bag, string eggItemId, out string message, bool consumeEgg = true)
        {
            var pet = ForEgg(eggItemId);
            if (pet == null || !bag.Has(eggItemId))
            {
                message = "That isn't a pet's egg.";
                return false;
            }

            if (Active(record) != null)
            {
                message = "Return your pet to its egg first.";
                return false;
            }

            if (consumeEgg)
            {
                bag.TryRemove(eggItemId);
            }

            record.Pets = record.Pets ?? new List<PetState>();
            var state = record.Pets.Find(p => p != null && string.Equals(p.EggItemId, eggItemId, StringComparison.OrdinalIgnoreCase));
            if (state == null)
            {
                state = new PetState { EggItemId = eggItemId, Name = Monsters.MonsterCatalog.Get(pet.MonsterId)?.Name ?? "Pet" };
                record.Pets.Add(state);
            }

            record.ActivePetEgg = eggItemId;
            message = $"{state.Name} hatches!";
            return true;
        }

        /// <summary>The active pet goes back into its egg (the egg returns to the bag; the pet is remembered).</summary>
        public static bool ReturnToEgg(CharacterRecord record, Inventory bag, out string message)
        {
            var state = Active(record);
            if (state == null)
            {
                message = "You have no pet out.";
                return false;
            }

            if (bag.Add(state.EggItemId, 1) < 1)
            {
                message = "Your bag is full.";
                return false;
            }

            record.ActivePetEgg = null;
            message = $"{state.Name} curls up in its egg.";
            return true;
        }

        /// <summary>Feeds the active pet its food from the bag. Feeding a full pet annoys it.</summary>
        public static bool Feed(CharacterRecord record, Inventory bag, out string message)
        {
            var state = Active(record);
            var pet = state != null ? ForEgg(state.EggItemId) : null;
            if (pet == null)
            {
                message = "You have no pet out.";
                return false;
            }

            if (string.IsNullOrEmpty(pet.FoodItemId) || !bag.TryRemove(pet.FoodItemId))
            {
                message = $"{state.Name} only eats {ItemCatalog.Get(pet.FoodItemId)?.Name ?? "its own food"}.";
                return false;
            }

            if (state.Hunger > 75)
            {
                state.Intimacy = Math.Max(0, state.Intimacy - 20);
                message = $"{state.Name} is too full and sulks.";
            }
            else
            {
                state.Intimacy = Math.Min(MaxIntimacy, state.Intimacy + (state.Hunger <= 25 ? pet.IntimacyFed : pet.IntimacyFed / 2));
                message = $"{state.Name} eats happily.";
            }

            state.Hunger = Math.Min(100, state.Hunger + pet.Fullness * 10);
            return true;
        }

        /// <summary>One hunger tick: the pet gets hungrier; a starving pet loses intimacy (and runs away at 0).</summary>
        public static bool Starve(CharacterRecord record, out string message)
        {
            message = null;
            var state = Active(record);
            if (state == null)
            {
                return false;
            }

            state.Hunger = Math.Max(0, state.Hunger - 1);
            if (state.Hunger > 0)
            {
                return false;
            }

            state.Intimacy = Math.Max(0, state.Intimacy - 20);
            if (state.Intimacy > 0)
            {
                message = $"{state.Name} is starving!";
                return false;
            }

            record.Pets.Remove(state);
            record.ActivePetEgg = null;
            message = $"{state.Name} ran away.";
            return true;
        }

        /// <summary>Puts an accessory on the active pet (only the one made for it). Ragnarok's accessories are for looks.</summary>
        public static bool WearArmor(CharacterRecord record, string itemId, out string message)
        {
            var state = Active(record);
            var pet = state != null ? ForEgg(state.EggItemId) : null;
            if (pet == null)
            {
                message = "Hatch the pet this is made for first.";
                return false;
            }

            if (!string.Equals(pet.ArmorItemId, itemId, StringComparison.OrdinalIgnoreCase))
            {
                message = $"That doesn't fit {state.Name}.";
                return false;
            }

            if (!string.IsNullOrEmpty(state.ArmorItemId))
            {
                message = $"{state.Name} is already wearing it.";
                return false;
            }

            state.ArmorItemId = pet.ArmorItemId;
            message = $"{state.Name} wears its {ItemCatalog.Get(itemId)?.Name ?? "accessory"} proudly.";
            return true;
        }

        /// <summary>Takes the active pet's accessory off, back into the bag.</summary>
        public static bool TakeOffArmor(CharacterRecord record, Inventory bag, out string message)
        {
            var state = Active(record);
            if (state == null || string.IsNullOrEmpty(state.ArmorItemId))
            {
                message = "Your pet isn't wearing anything.";
                return false;
            }

            if (bag.Add(state.ArmorItemId, 1) < 1)
            {
                message = "Your bag is full.";
                return false;
            }

            message = $"You take the {ItemCatalog.Get(state.ArmorItemId)?.Name ?? "accessory"} off {state.Name}.";
            state.ArmorItemId = null;
            return true;
        }

        /// <summary>The pet an accessory is made for, or null.</summary>
        public static PetDefinition ForArmor(string itemId)
        {
            foreach (var pet in Ordered)
            {
                if (string.Equals(pet.ArmorItemId, itemId, StringComparison.OrdinalIgnoreCase))
                {
                    return pet;
                }
            }

            return null;
        }

        /// <summary>The bonus the active pet gives, if it's Loyal (added to the gear's bonuses).</summary>
        public static EquipEffect LoyalBonus(CharacterRecord record)
        {
            var state = Active(record);
            if (state == null || IntimacyOf(state.Intimacy) != PetIntimacy.Loyal)
            {
                return null;
            }

            return ForEgg(state.EggItemId)?.LoyalBonus;
        }
    }
}
