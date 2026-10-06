using System;
using System.Collections.Generic;

namespace Runeheir.Items
{
    /// <summary>
    /// One inventory entry: a stack of a stackable item, or one piece of equipment with its own refine level,
    /// compounded cards and etched glyphs. Plain public fields so JsonUtility (and later Mirror) can serialize it.
    /// Empty sockets/grooves are empty strings.
    /// </summary>
    [Serializable]
    public sealed class ItemStack
    {
        public string ItemId;
        public int Amount;

        /// <summary>+0..+20 (equipment only).</summary>
        public int Refine;

        /// <summary>Soul Cards, one entry per socket ("" = empty socket).</summary>
        public string[] Cards = Array.Empty<string>();

        /// <summary>Weapons: the two fuller grooves carved with Elder Futhark glyphs ("" = empty).</summary>
        public string[] Glyphs = Array.Empty<string>();

        /// <summary>Weapons: smashed by a monster skill. Useless (you fight bare-handed) until Brokk repairs it.</summary>
        public bool Broken;

        public ItemStack()
        {
        }

        public ItemStack(string itemId, int amount)
        {
            ItemId = itemId;
            Amount = amount;
        }

        public ItemDefinition Definition => ItemCatalog.Get(ItemId);

        public bool IsEmpty => string.IsNullOrEmpty(ItemId) || Amount <= 0;

        public int CardCount
        {
            get
            {
                int count = 0;
                foreach (string card in Cards ?? Array.Empty<string>())
                {
                    if (!string.IsNullOrEmpty(card))
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int FreeSockets => (Cards?.Length ?? 0) - CardCount;

        /// <summary>A fresh piece of equipment: +0, empty sockets, empty grooves on weapons.</summary>
        public static ItemStack NewInstance(ItemDefinition definition)
        {
            var stack = new ItemStack(definition.Id, 1);
            stack.Cards = EmptySlots(definition.Sockets);
            stack.Glyphs = EmptySlots(definition.IsWeapon ? RunewordRules.Grooves : 0);
            return stack;
        }

        public ItemStack Clone()
        {
            return new ItemStack(ItemId, Amount)
            {
                Refine = Refine,
                Cards = (string[])(Cards ?? Array.Empty<string>()).Clone(),
                Glyphs = (string[])(Glyphs ?? Array.Empty<string>()).Clone(),
                Broken = Broken,
            };
        }

        /// <summary>"+7 Iron Claymore [1/3]" — refine, name, cards used / sockets ("(Broken)" when smashed).</summary>
        public string DisplayName
        {
            get
            {
                var definition = Definition;
                if (definition == null)
                {
                    return ItemId;
                }

                string refine = Refine > 0 ? $"+{Refine} " : string.Empty;
                string sockets = definition.Sockets > 0 ? $" [{CardCount}/{definition.Sockets}]" : string.Empty;
                return refine + definition.Name + sockets + (Broken ? " (Broken)" : string.Empty);
            }
        }

        /// <summary>Repairs an entry from an old or hand-edited save (socket/groove sizes, refine range).</summary>
        public void Sanitize()
        {
            var definition = Definition;
            Cards = Resize(Cards, definition != null && definition.IsEquipment ? definition.Sockets : 0);
            Glyphs = Resize(Glyphs, definition != null && definition.IsWeapon ? RunewordRules.Grooves : 0);
            for (int i = 0; i < Cards.Length; i++)
            {
                var card = ItemCatalog.Get(Cards[i]);
                if (card == null || !card.IsCard)
                {
                    Cards[i] = string.Empty;
                }
            }

            for (int i = 0; i < Glyphs.Length; i++)
            {
                if (!RunewordRules.IsGlyph(Glyphs[i]))
                {
                    Glyphs[i] = string.Empty;
                }
            }

            Refine = definition != null && definition.IsEquipment && definition.Refinable ? Math.Max(0, Math.Min(RefineRules.MaxRefine, Refine)) : 0;
            Broken &= definition != null && definition.IsWeapon;
        }

        private static string[] EmptySlots(int count)
        {
            var slots = new string[Math.Max(0, count)];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = string.Empty;
            }

            return slots;
        }

        private static string[] Resize(string[] slots, int count)
        {
            var resized = EmptySlots(count);
            if (slots != null)
            {
                for (int i = 0; i < Math.Min(count, slots.Length); i++)
                {
                    resized[i] = slots[i] ?? string.Empty;
                }
            }

            return resized;
        }
    }

    /// <summary>
    /// The character's bag, operating on the saved list in <c>CharacterRecord</c>. Stackable items share one entry;
    /// equipment gets one entry per piece.
    /// </summary>
    public sealed class Inventory
    {
        public const int MaxStack = 30000;

        /// <summary>Distinct entries the bag can hold (each piece of equipment is one).</summary>
        public const int MaxEntries = 200;

        private readonly List<ItemStack> _stacks;

        public Inventory(List<ItemStack> backingList)
        {
            _stacks = backingList ?? throw new ArgumentNullException(nameof(backingList));
        }

        public event Action Changed;

        public IReadOnlyList<ItemStack> Stacks => _stacks;

        /// <summary>Total amount of <paramref name="itemId"/> (each piece of equipment counts 1).</summary>
        public int Count(string itemId)
        {
            int total = 0;
            foreach (var stack in _stacks)
            {
                if (Same(stack.ItemId, itemId))
                {
                    total += stack.Amount;
                }
            }

            return total;
        }

        public bool Has(string itemId, int amount = 1)
        {
            return Count(itemId) >= amount;
        }

        public bool Contains(ItemStack entry)
        {
            return entry != null && _stacks.Contains(entry);
        }

        /// <summary>
        /// Adds items; returns how many were actually added (stacks cap at <see cref="MaxStack"/>, the bag at
        /// <see cref="MaxEntries"/> entries). Equipment arrives as fresh +0 pieces.
        /// </summary>
        public int Add(string itemId, int amount)
        {
            var definition = ItemCatalog.Get(itemId);
            if (amount <= 0 || definition == null)
            {
                return 0;
            }

            int added;
            if (!definition.IsStackable)
            {
                added = 0;
                while (added < amount && _stacks.Count < MaxEntries)
                {
                    _stacks.Add(ItemStack.NewInstance(definition));
                    added++;
                }
            }
            else
            {
                var stack = FindStack(itemId);
                if (stack == null)
                {
                    if (_stacks.Count >= MaxEntries)
                    {
                        return 0;
                    }

                    stack = new ItemStack(definition.Id, 0);
                    _stacks.Add(stack);
                }

                // Never negative: an over-cap stack from an old or hand-edited save must not lose items.
                added = Math.Max(0, Math.Min(amount, MaxStack - stack.Amount));
                stack.Amount += added;
                if (stack.Amount == 0)
                {
                    _stacks.Remove(stack);
                }
            }

            if (added > 0)
            {
                Changed?.Invoke();
            }

            return added;
        }

        /// <summary>Puts an existing entry in the bag (unequip, storage, extracted cards). Stackables merge. Returns false when full.</summary>
        public bool AddEntry(ItemStack entry)
        {
            var definition = entry?.Definition;
            if (definition == null || entry.Amount <= 0)
            {
                return false;
            }

            if (definition.IsStackable)
            {
                // All or nothing: a partial merge would leave the caller unsure what moved.
                var existing = FindFirst(entry.ItemId);
                bool fits = existing != null ? existing.Amount + (long)entry.Amount <= MaxStack : _stacks.Count < MaxEntries && entry.Amount <= MaxStack;
                return fits && Add(entry.ItemId, entry.Amount) == entry.Amount;
            }

            if (_stacks.Count >= MaxEntries)
            {
                return false;
            }

            entry.Amount = 1;
            _stacks.Add(entry);
            Changed?.Invoke();
            return true;
        }

        /// <summary>Takes one specific entry out of the bag (equip, sell, store, refine break).</summary>
        public bool RemoveEntry(ItemStack entry)
        {
            if (entry == null || !_stacks.Remove(entry))
            {
                return false;
            }

            Changed?.Invoke();
            return true;
        }

        /// <summary>Removes <paramref name="amount"/> of a stackable item, or that many plain (+0, uncarded) pieces of equipment.</summary>
        public bool TryRemove(string itemId, int amount = 1)
        {
            if (amount <= 0 || Count(itemId) < amount)
            {
                return false;
            }

            var definition = ItemCatalog.Get(itemId);
            if (definition != null && !definition.IsStackable)
            {
                var pieces = _stacks.FindAll(s => Same(s.ItemId, itemId));
                pieces.Sort((a, b) => (a.Refine + a.CardCount).CompareTo(b.Refine + b.CardCount));
                for (int i = 0; i < amount; i++)
                {
                    _stacks.Remove(pieces[i]);
                }
            }
            else
            {
                var stack = FindStack(itemId);
                stack.Amount -= amount;
                if (stack.Amount <= 0)
                {
                    _stacks.Remove(stack);
                }
            }

            Changed?.Invoke();
            return true;
        }

        /// <summary>First entry holding <paramref name="itemId"/> (hotkeys use it to equip a piece).</summary>
        public ItemStack FindFirst(string itemId)
        {
            foreach (var stack in _stacks)
            {
                if (Same(stack.ItemId, itemId))
                {
                    return stack;
                }
            }

            return null;
        }

        /// <summary>Call after changing an entry in place (refine, compound, etch) so the UI refreshes.</summary>
        public void NotifyChanged()
        {
            Changed?.Invoke();
        }

        public int TotalWeight()
        {
            int total = 0;
            foreach (var stack in _stacks)
            {
                var item = stack.Definition;
                if (item != null)
                {
                    total += item.Weight * stack.Amount;
                }
            }

            return total;
        }

        private ItemStack FindStack(string itemId)
        {
            foreach (var stack in _stacks)
            {
                if (Same(stack.ItemId, itemId))
                {
                    return stack;
                }
            }

            return null;
        }

        private static bool Same(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }
}
