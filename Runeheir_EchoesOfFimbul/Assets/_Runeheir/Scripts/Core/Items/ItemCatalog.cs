using System;
using System.Collections.Generic;
using Runeheir.Combat;

namespace Runeheir.Items
{
    public enum ItemKind
    {
        Consumable = 0,
        Etc = 1,
    }

    public enum ItemSpecialEffect
    {
        None = 0,

        /// <summary>Butterfly Wing equivalent: warp to the save point.</summary>
        ReturnToSavePoint = 1,

        /// <summary>Fly Wing equivalent: random teleport on the current map.</summary>
        RandomTeleport = 2,

        /// <summary>Sowilo: cleanse stun/freeze and grant CC immunity.</summary>
        Cleanse = 3,
    }

    public sealed class ItemDefinition
    {
        public string Id;
        public string Name;
        public string Description;
        public ItemKind Kind;
        public int Weight;
        public string IconLabel;
        public string IconColorHex = "#7F8C8D";

        public int HealHpMin;
        public int HealHpMax;
        public float HealHpPercent;
        public int HealSpMin;
        public int HealSpMax;

        public string BuffId;
        public ItemSpecialEffect Special;

        public bool IsUsable => Kind == ItemKind.Consumable;
    }

    /// <summary>Phase 2 consumables (Norse-flavored potions + GDD §7 combat runestones).</summary>
    public static class ItemCatalog
    {
        public const string LingonberryTonic = "lingonberry_tonic";
        public const string HoneyMead = "honey_mead";
        public const string AetherSap = "aether_sap";
        public const string RuneUruz = "rune_uruz";
        public const string RuneTiwaz = "rune_tiwaz";
        public const string RuneSowilo = "rune_sowilo";
        public const string RavenFeather = "raven_feather";
        public const string WindRuneShard = "wind_rune_shard";

        private static readonly Dictionary<string, ItemDefinition> ById = new Dictionary<string, ItemDefinition>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<ItemDefinition> Ordered = new List<ItemDefinition>();

        static ItemCatalog()
        {
            Register(new ItemDefinition
            {
                Id = LingonberryTonic, Name = "Lingonberry Tonic", Kind = ItemKind.Consumable, Weight = 7,
                Description = "Restores 45~65 HP.", IconLabel = "LT", IconColorHex = "#C0392B",
                HealHpMin = 45, HealHpMax = 65,
            });
            Register(new ItemDefinition
            {
                Id = HoneyMead, Name = "Honey Mead", Kind = ItemKind.Consumable, Weight = 15,
                Description = "Restores 325~405 HP.", IconLabel = "HM", IconColorHex = "#F5B041",
                HealHpMin = 325, HealHpMax = 405,
            });
            Register(new ItemDefinition
            {
                Id = AetherSap, Name = "Aether Sap Vial", Kind = ItemKind.Consumable, Weight = 15,
                Description = "Runic sap of Yggdrasil. Restores 40~60 SP.", IconLabel = "AS", IconColorHex = "#2E86C1",
                HealSpMin = 40, HealSpMax = 60,
            });
            Register(new ItemDefinition
            {
                Id = RuneUruz, Name = "Uruz Runestone", Kind = ItemKind.Consumable, Weight = 5,
                Description = "Restores 30% Max HP and grants +25 STR for 60s.", IconLabel = "URZ", IconColorHex = "#A04000",
                HealHpPercent = 30f, BuffId = BuffCatalog.UruzMight,
            });
            Register(new ItemDefinition
            {
                Id = RuneTiwaz, Name = "Tiwaz Runestone", Kind = ItemKind.Consumable, Weight = 5,
                Description = "Your next 3 attacks are guaranteed criticals.", IconLabel = "TIW", IconColorHex = "#D4AC0D",
                BuffId = BuffCatalog.TiwazPrecision,
            });
            Register(new ItemDefinition
            {
                Id = RuneSowilo, Name = "Sowilo Runestone", Kind = ItemKind.Consumable, Weight = 5,
                Description = "Cleanses stun/freeze and grants 10s CC immunity.", IconLabel = "SOW", IconColorHex = "#F7DC6F",
                BuffId = BuffCatalog.SowiloWard, Special = ItemSpecialEffect.Cleanse,
            });
            Register(new ItemDefinition
            {
                Id = RavenFeather, Name = "Raven Feather", Kind = ItemKind.Consumable, Weight = 5,
                Description = "A feather of Huginn. Returns you to your save point.", IconLabel = "RF", IconColorHex = "#34495E",
                Special = ItemSpecialEffect.ReturnToSavePoint,
            });
            Register(new ItemDefinition
            {
                Id = WindRuneShard, Name = "Wind Rune Shard", Kind = ItemKind.Consumable, Weight = 5,
                Description = "Teleports you to a random spot on this map.", IconLabel = "WR", IconColorHex = "#76D7C4",
                Special = ItemSpecialEffect.RandomTeleport,
            });
        }

        public static IReadOnlyList<ItemDefinition> All => Ordered;

        public static ItemDefinition Get(string id)
        {
            return id != null && ById.TryGetValue(id, out var item) ? item : null;
        }

        private static void Register(ItemDefinition item)
        {
            ById[item.Id] = item;
            Ordered.Add(item);
        }
    }

    [Serializable]
    public sealed class ItemStack
    {
        public string ItemId;
        public int Amount;

        public ItemStack()
        {
        }

        public ItemStack(string itemId, int amount)
        {
            ItemId = itemId;
            Amount = amount;
        }
    }

    /// <summary>Stack-based inventory operating on the saved list in <c>CharacterRecord</c>.</summary>
    public sealed class Inventory
    {
        public const int MaxStack = 30000;

        private readonly List<ItemStack> _stacks;

        public Inventory(List<ItemStack> backingList)
        {
            _stacks = backingList ?? throw new ArgumentNullException(nameof(backingList));
        }

        public event Action Changed;

        public IReadOnlyList<ItemStack> Stacks => _stacks;

        public int Count(string itemId)
        {
            var stack = Find(itemId);
            return stack?.Amount ?? 0;
        }

        public bool Has(string itemId, int amount = 1)
        {
            return Count(itemId) >= amount;
        }

        /// <summary>Adds items; returns how many were actually added (capped at <see cref="MaxStack"/>).</summary>
        public int Add(string itemId, int amount)
        {
            if (amount <= 0 || ItemCatalog.Get(itemId) == null)
            {
                return 0;
            }

            var stack = Find(itemId);
            if (stack == null)
            {
                stack = new ItemStack(ItemCatalog.Get(itemId).Id, 0);
                _stacks.Add(stack);
            }

            // Never negative: an over-cap stack from an old or hand-edited save must not lose items.
            int added = Math.Max(0, Math.Min(amount, MaxStack - stack.Amount));
            stack.Amount += added;
            if (added > 0)
            {
                Changed?.Invoke();
            }

            return added;
        }

        public bool TryRemove(string itemId, int amount = 1)
        {
            var stack = Find(itemId);
            if (stack == null || amount <= 0 || stack.Amount < amount)
            {
                return false;
            }

            stack.Amount -= amount;
            if (stack.Amount == 0)
            {
                _stacks.Remove(stack);
            }

            Changed?.Invoke();
            return true;
        }

        public int TotalWeight()
        {
            int total = 0;
            foreach (var stack in _stacks)
            {
                var item = ItemCatalog.Get(stack.ItemId);
                if (item != null)
                {
                    total += item.Weight * stack.Amount;
                }
            }

            return total;
        }

        private ItemStack Find(string itemId)
        {
            foreach (var stack in _stacks)
            {
                if (string.Equals(stack.ItemId, itemId, StringComparison.OrdinalIgnoreCase))
                {
                    return stack;
                }
            }

            return null;
        }
    }
}
