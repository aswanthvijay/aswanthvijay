using System;
using System.Collections.Generic;
using Runeheir.Characters;
using Runeheir.Items;

namespace Runeheir.Social
{
    /// <summary>
    /// Moving items between characters (player trades, street stalls) without losing or duplicating any: everything is
    /// checked before the bag changes. Stackables are matched by item; equipment by the exact piece (refine, cards,
    /// glyphs, broken).
    /// </summary>
    public static class ItemTransfer
    {
        /// <summary>Most zeny one character can hold (trades and stalls refuse to go past it).</summary>
        public const long MaxZeny = 2_000_000_000L;

        /// <summary>True when <paramref name="a"/> and <paramref name="b"/> are the same kind of item (amounts aside).</summary>
        public static bool Matches(ItemStack a, ItemStack b)
        {
            if (a == null || b == null || !string.Equals(a.ItemId, b.ItemId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var definition = a.Definition;
            if (definition == null || definition.IsStackable)
            {
                return true;
            }

            return a.Refine == b.Refine && a.Broken == b.Broken && SameSlots(a.Cards, b.Cards) && SameSlots(a.Glyphs, b.Glyphs);
        }

        /// <summary>A clean copy for sending or listing (unknown items and empty stacks give null).</summary>
        public static ItemStack Copy(ItemStack stack, int amount)
        {
            var definition = stack?.Definition;
            if (definition == null || amount <= 0)
            {
                return null;
            }

            var copy = stack.Clone();
            copy.Sanitize();
            copy.Amount = definition.IsStackable ? Math.Min(amount, Inventory.MaxStack) : 1;
            return copy;
        }

        /// <summary>Drops unknown items and empty stacks, sanitizes the rest and caps amounts (equipment is always 1).</summary>
        public static List<ItemStack> Clean(IEnumerable<ItemStack> items)
        {
            var clean = new List<ItemStack>();
            if (items == null)
            {
                return clean;
            }

            foreach (var item in items)
            {
                var copy = Copy(item, item?.Amount ?? 0);
                if (copy != null)
                {
                    clean.Add(copy);
                }
            }

            return clean;
        }

        public static long WeightOf(IEnumerable<ItemStack> items)
        {
            long total = 0;
            if (items == null)
            {
                return 0;
            }

            foreach (var item in items)
            {
                var definition = item?.Definition;
                if (definition != null)
                {
                    total += (long)definition.Weight * Math.Max(0, item.Amount);
                }
            }

            return total;
        }

        /// <summary>True when the bag holds everything in <paramref name="items"/> (each piece of equipment once).</summary>
        public static bool HasAll(Inventory inventory, IReadOnlyList<ItemStack> items, out string error)
        {
            error = null;
            if (inventory == null)
            {
                error = "No bag.";
                return false;
            }

            if (items == null || items.Count == 0)
            {
                return true;
            }

            var counts = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            var used = new HashSet<ItemStack>();
            foreach (var item in items)
            {
                var definition = item?.Definition;
                if (definition == null || item.Amount <= 0)
                {
                    error = "That item can't be traded.";
                    return false;
                }

                if (definition.IsStackable)
                {
                    counts.TryGetValue(definition.Id, out long sum);
                    counts[definition.Id] = sum + item.Amount;
                    continue;
                }

                var piece = FindPiece(inventory, item, used);
                if (piece == null)
                {
                    error = $"You no longer have {item.DisplayName}.";
                    return false;
                }

                used.Add(piece);
            }

            foreach (var pair in counts)
            {
                if (inventory.Count(pair.Key) < pair.Value)
                {
                    error = $"You don't have {pair.Value:N0} {ItemCatalog.Get(pair.Key)?.Name ?? pair.Key}.";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// True when the bag still fits within its entry limit and <paramref name="capacity"/> after giving
        /// <paramref name="give"/> away and taking <paramref name="receive"/>.
        /// </summary>
        public static bool CanReceive(Inventory inventory, int capacity, int currentWeight, IReadOnlyList<ItemStack> give,
            IReadOnlyList<ItemStack> receive, out string error)
        {
            error = null;
            if (inventory == null)
            {
                error = "No bag.";
                return false;
            }

            give = give ?? Array.Empty<ItemStack>();
            receive = receive ?? Array.Empty<ItemStack>();

            long weight = currentWeight - WeightOf(give) + WeightOf(receive);
            if (WeightOf(receive) > 0 && weight > capacity)
            {
                error = "You can't carry that much weight.";
                return false;
            }

            // Simulate the bag's entries: stackables by id, equipment one entry per piece.
            var stackCounts = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            int equipmentEntries = 0;
            foreach (var stack in inventory.Stacks)
            {
                var definition = stack?.Definition;
                if (definition == null)
                {
                    continue;
                }

                if (definition.IsStackable)
                {
                    stackCounts.TryGetValue(definition.Id, out long sum);
                    stackCounts[definition.Id] = sum + stack.Amount;
                }
                else
                {
                    equipmentEntries++;
                }
            }

            foreach (var item in give)
            {
                var definition = item?.Definition;
                if (definition == null)
                {
                    continue;
                }

                if (definition.IsStackable)
                {
                    stackCounts.TryGetValue(definition.Id, out long sum);
                    stackCounts[definition.Id] = Math.Max(0, sum - item.Amount);
                }
                else
                {
                    equipmentEntries--;
                }
            }

            foreach (var item in receive)
            {
                var definition = item?.Definition;
                if (definition == null || item.Amount <= 0)
                {
                    continue;
                }

                if (definition.IsStackable)
                {
                    stackCounts.TryGetValue(definition.Id, out long sum);
                    if (sum + item.Amount > Inventory.MaxStack)
                    {
                        error = $"You can't carry more than {Inventory.MaxStack:N0} {definition.Name}.";
                        return false;
                    }

                    stackCounts[definition.Id] = sum + item.Amount;
                }
                else
                {
                    equipmentEntries++;
                }
            }

            int entries = equipmentEntries;
            foreach (var pair in stackCounts)
            {
                if (pair.Value > 0)
                {
                    entries++;
                }
            }

            if (entries > Inventory.MaxEntries)
            {
                error = "Your bag is full.";
                return false;
            }

            return true;
        }

        /// <summary>Takes <paramref name="items"/> out of the bag. Call <see cref="HasAll"/> first: nothing is removed unless all are there.</summary>
        public static bool TryRemoveAll(Inventory inventory, IReadOnlyList<ItemStack> items)
        {
            if (!HasAll(inventory, items, out _))
            {
                return false;
            }

            if (items == null)
            {
                return true;
            }

            var used = new HashSet<ItemStack>();
            foreach (var item in items)
            {
                var definition = item.Definition;
                if (definition.IsStackable)
                {
                    inventory.TryRemove(definition.Id, item.Amount);
                    continue;
                }

                var piece = FindPiece(inventory, item, used);
                used.Add(piece);
                inventory.RemoveEntry(piece);
            }

            return true;
        }

        /// <summary>Puts copies of <paramref name="items"/> in the bag; returns what didn't fit (empty when all did).</summary>
        public static List<ItemStack> AddAll(Inventory inventory, IEnumerable<ItemStack> items)
        {
            var leftovers = new List<ItemStack>();
            if (items == null)
            {
                return leftovers;
            }

            foreach (var item in items)
            {
                var copy = Copy(item, item?.Amount ?? 0);
                if (copy == null)
                {
                    continue;
                }

                if (copy.Definition.IsStackable)
                {
                    int added = inventory.Add(copy.ItemId, copy.Amount);
                    if (added < copy.Amount)
                    {
                        copy.Amount -= added;
                        leftovers.Add(copy);
                    }
                }
                else if (!inventory.AddEntry(copy))
                {
                    leftovers.Add(copy);
                }
            }

            return leftovers;
        }

        /// <summary>
        /// Puts items in the character's Pushcart hold (goods waiting there until the bag has room). Stackables merge.
        /// </summary>
        public static void AddToCart(CharacterRecord record, IEnumerable<ItemStack> items)
        {
            if (record == null || items == null)
            {
                return;
            }

            record.Cart = record.Cart ?? new List<ItemStack>();
            foreach (var item in items)
            {
                var copy = Copy(item, item?.Amount ?? 0);
                if (copy == null)
                {
                    continue;
                }

                var existing = copy.Definition.IsStackable ? record.Cart.Find(c => Matches(c, copy)) : null;
                if (existing != null)
                {
                    existing.Amount = (int)Math.Min(int.MaxValue, (long)existing.Amount + copy.Amount);
                }
                else
                {
                    record.Cart.Add(copy);
                }
            }
        }

        /// <summary>
        /// Moves what fits from the cart hold back into the bag (after a stall closes, or when a trade had too much to
        /// carry). Goods on an open stall stay put: pass the stall so they're skipped. <paramref name="wornWeight"/> is the
        /// weight of equipment being worn (it counts against <paramref name="capacity"/> too). Returns how many entries moved.
        /// </summary>
        public static int ReturnCartToBag(CharacterRecord record, Inventory inventory, int capacity, int wornWeight = 0, VendingStall openStall = null)
        {
            if (record?.Cart == null || inventory == null || record.Cart.Count == 0)
            {
                return 0;
            }

            int moved = 0;
            var reserved = new List<ItemStack>();
            if (openStall != null)
            {
                foreach (var entry in openStall.Entries)
                {
                    if (entry?.Item != null)
                    {
                        reserved.Add(entry.Item.Clone());
                    }
                }
            }

            for (int i = 0; i < record.Cart.Count; i++)
            {
                var stack = record.Cart[i];
                var definition = stack?.Definition;
                if (definition == null || stack.Amount <= 0)
                {
                    record.Cart.RemoveAt(i--);
                    continue;
                }

                int free = stack.Amount - Reserve(reserved, stack);
                if (free <= 0)
                {
                    continue;
                }

                int room = definition.Weight > 0 ? Math.Max(0, (capacity - wornWeight - inventory.TotalWeight()) / definition.Weight) : free;
                int amount = Math.Min(free, room);
                if (amount <= 0)
                {
                    continue;
                }

                int added;
                if (definition.IsStackable)
                {
                    added = inventory.Add(definition.Id, amount);
                }
                else
                {
                    var piece = stack.Clone();
                    piece.Amount = 1;
                    added = inventory.AddEntry(piece) ? 1 : 0;
                }

                if (added <= 0)
                {
                    continue;
                }

                moved++;
                stack.Amount -= added;
                if (stack.Amount <= 0)
                {
                    record.Cart.RemoveAt(i--);
                }
            }

            return moved;
        }

        /// <summary>Takes up to <paramref name="stack"/>'s amount out of the reserved list; returns how much of it is reserved.</summary>
        private static int Reserve(List<ItemStack> reserved, ItemStack stack)
        {
            int total = 0;
            for (int i = 0; i < reserved.Count && total < stack.Amount; i++)
            {
                if (!Matches(reserved[i], stack))
                {
                    continue;
                }

                int take = Math.Min(reserved[i].Amount, stack.Amount - total);
                total += take;
                reserved[i].Amount -= take;
                if (reserved[i].Amount <= 0)
                {
                    reserved.RemoveAt(i--);
                }
            }

            return total;
        }

        private static ItemStack FindPiece(Inventory inventory, ItemStack wanted, HashSet<ItemStack> used)
        {
            foreach (var stack in inventory.Stacks)
            {
                if (!used.Contains(stack) && Matches(stack, wanted))
                {
                    return stack;
                }
            }

            return null;
        }

        private static bool SameSlots(string[] a, string[] b)
        {
            int length = Math.Max(a?.Length ?? 0, b?.Length ?? 0);
            for (int i = 0; i < length; i++)
            {
                string x = a != null && i < a.Length ? a[i] ?? string.Empty : string.Empty;
                string y = b != null && i < b.Length ? b[i] ?? string.Empty : string.Empty;
                if (!string.Equals(x, y, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
