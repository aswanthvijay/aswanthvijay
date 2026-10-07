using System;
using System.Collections.Generic;
using Runeheir.Characters;
using Runeheir.Items;
using Runeheir.World;

namespace Runeheir.Social
{
    /// <summary>One line of a street stall: the goods (their amount is what's left) and the price of one.</summary>
    [Serializable]
    public sealed class VendingEntry
    {
        public ItemStack Item;
        public long Price;
    }

    /// <summary>An open-air street stall (GDD §8): a title and up to <see cref="VendingRules.MaxEntries"/> priced lines.</summary>
    [Serializable]
    public sealed class VendingStall
    {
        public string Owner;
        public string Title;
        public List<VendingEntry> Entries = new List<VendingEntry>();

        /// <summary>Zeny taken since the stall opened.</summary>
        public long Earned;

        public bool IsSoldOut => Entries == null || Entries.Count == 0;

        public VendingStall Clone()
        {
            var copy = new VendingStall { Owner = Owner, Title = Title, Earned = Earned };
            foreach (var entry in Entries ?? new List<VendingEntry>())
            {
                if (entry?.Item != null)
                {
                    copy.Entries.Add(new VendingEntry { Item = entry.Item.Clone(), Price = entry.Price });
                }
            }

            return copy;
        }
    }

    /// <summary>A line the seller picked in the setup window: a bag entry, how many of it, and the price of one.</summary>
    public readonly struct VendingRequest
    {
        public VendingRequest(ItemStack entry, int amount, long price)
        {
            Entry = entry;
            Amount = amount;
            Price = price;
        }

        public ItemStack Entry { get; }

        public int Amount { get; }

        public long Price { get; }
    }

    /// <summary>
    /// Street vending: a character with a Pushcart opens a stall in Vigrid Haven. The goods move from the bag into the
    /// cart hold (saved with the character, so nothing is lost if the game closes); buyers pay first, the seller's
    /// client hands the goods over, and whatever is left goes back to the bag when the stall closes.
    /// </summary>
    public static class VendingRules
    {
        public const int MaxEntries = 12;
        public const int MaxTitleLength = 36;
        public const long MinPrice = 1;
        public const long MaxPrice = 1_000_000_000L;

        /// <summary>Stalls keep at least this far from each other, NPCs and portals (meters).</summary>
        public const float MinSpacing = 2.5f;

        public const string DefaultTitle = "Bargains!";

        /// <summary>Street vending is allowed in towns (Vigrid Haven).</summary>
        public static bool MapAllowsVending(MapDefinition map)
        {
            return map != null && map.IsTown;
        }

        public static bool CanOpen(CharacterRecord record, MapDefinition map, out string error)
        {
            if (record == null)
            {
                error = "No character.";
                return false;
            }

            if (!record.HasPushcart)
            {
                error = "You need a Pushcart to vend: rent one from the cart merchant in Vigrid Haven.";
                return false;
            }

            if (!MapAllowsVending(map))
            {
                error = "Stalls can only be set up in Vigrid Haven.";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>Trims a stall title and strips rich-text; an empty title becomes <see cref="DefaultTitle"/>.</summary>
        public static string CleanTitle(string raw)
        {
            string title = ChatRules.Sanitize(raw, MaxTitleLength);
            return string.IsNullOrEmpty(title) ? DefaultTitle : title;
        }

        /// <summary>
        /// Opens a stall from the chosen bag lines: checks every line, then moves the goods from the bag into the cart
        /// hold. Nothing changes unless the whole stall is valid.
        /// </summary>
        public static bool TryOpen(CharacterRecord record, Inventory inventory, MapDefinition map, string title,
            IReadOnlyList<VendingRequest> goods, out VendingStall stall, out string error)
        {
            stall = null;
            if (!CanOpen(record, map, out error))
            {
                return false;
            }

            if (inventory == null || goods == null || goods.Count == 0)
            {
                error = "Put something on the stall first.";
                return false;
            }

            if (goods.Count > MaxEntries)
            {
                error = $"A stall holds at most {MaxEntries} lines.";
                return false;
            }

            var entries = new List<VendingEntry>();
            var seenPieces = new HashSet<ItemStack>();
            var seenStackables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in goods)
            {
                var definition = line.Entry?.Definition;
                if (definition == null || !inventory.Contains(line.Entry))
                {
                    error = "That item isn't in your bag.";
                    return false;
                }

                if (definition.IsStackable ? !seenStackables.Add(definition.Id) : !seenPieces.Add(line.Entry))
                {
                    error = $"{definition.Name} is on the stall twice.";
                    return false;
                }

                int available = definition.IsStackable ? inventory.Count(definition.Id) : 1;
                if (line.Amount <= 0 || line.Amount > available)
                {
                    error = $"You don't have {line.Amount:N0} {definition.Name}.";
                    return false;
                }

                if (line.Price < MinPrice || line.Price > MaxPrice)
                {
                    error = $"Prices run from {MinPrice:N0} to {MaxPrice:N0} zeny.";
                    return false;
                }

                entries.Add(new VendingEntry { Item = ItemTransfer.Copy(line.Entry, line.Amount), Price = line.Price });
            }

            var items = entries.ConvertAll(e => e.Item);
            if (!ItemTransfer.TryRemoveAll(inventory, items))
            {
                error = "Your bag changed. Try again.";
                return false;
            }

            ItemTransfer.AddToCart(record, items);
            inventory.NotifyChanged();
            stall = new VendingStall { Owner = record.Name, Title = CleanTitle(title), Entries = entries };
            error = null;
            return true;
        }

        /// <summary>Cost of <paramref name="amount"/> of line <paramref name="index"/>, checked against the buyer's zeny.</summary>
        public static bool CanBuy(VendingStall stall, int index, int amount, long buyerZeny, out long cost, out string error)
        {
            cost = 0;
            var entry = Line(stall, index);
            if (entry == null)
            {
                error = "That's no longer for sale.";
                return false;
            }

            var definition = entry.Item.Definition;
            int max = definition != null && definition.IsStackable ? entry.Item.Amount : Math.Min(1, entry.Item.Amount);
            if (amount <= 0 || amount > max)
            {
                error = max > 0 ? $"Only {max:N0} left." : "Sold out.";
                return false;
            }

            if (entry.Price > MaxPrice || entry.Price < MinPrice)
            {
                error = "That price isn't valid.";
                return false;
            }

            cost = entry.Price * amount;
            if (buyerZeny < cost)
            {
                error = $"You need {cost:N0} zeny.";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Server side: takes <paramref name="amount"/> of line <paramref name="index"/> off the stall for a buyer, if the
        /// line still sells the same goods at <paramref name="unitPrice"/> (so a price changed mid-click can't be charged).
        /// </summary>
        public static bool TryTake(VendingStall stall, int index, ItemStack expected, int amount, long unitPrice, out ItemStack sold, out long cost,
            out string error)
        {
            sold = null;
            if (!CanBuy(stall, index, amount, long.MaxValue, out cost, out error))
            {
                return false;
            }

            var entry = stall.Entries[index];
            if (entry.Price != unitPrice || (expected != null && !ItemTransfer.Matches(entry.Item, expected)))
            {
                error = "The stall changed. Look again.";
                return false;
            }

            sold = ItemTransfer.Copy(entry.Item, amount);
            entry.Item.Amount -= amount;
            if (entry.Item.Amount <= 0)
            {
                stall.Entries.RemoveAt(index);
            }

            stall.Earned += cost;
            error = null;
            return true;
        }

        /// <summary>Puts goods back on a stall after a sale fell through.</summary>
        public static void PutBack(VendingStall stall, ItemStack goods, long unitPrice)
        {
            if (stall == null || goods?.Definition == null || goods.Amount <= 0)
            {
                return;
            }

            stall.Earned = Math.Max(0, stall.Earned - unitPrice * goods.Amount);
            var line = stall.Entries.Find(e => e.Price == unitPrice && ItemTransfer.Matches(e.Item, goods));
            if (line != null && goods.Definition.IsStackable)
            {
                line.Item.Amount += goods.Amount;
                return;
            }

            stall.Entries.Add(new VendingEntry { Item = ItemTransfer.Copy(goods, goods.Amount), Price = unitPrice });
        }

        /// <summary>
        /// Seller side: hands <paramref name="goods"/> over from the cart hold and takes the money. False when the cart no
        /// longer holds them (the buyer is refunded).
        /// </summary>
        public static bool TryDeliver(CharacterRecord record, ItemStack goods, long cost, out string error)
        {
            if (record?.Cart == null || goods?.Definition == null || goods.Amount <= 0)
            {
                error = "Nothing to hand over.";
                return false;
            }

            int have = 0;
            foreach (var stack in record.Cart)
            {
                if (ItemTransfer.Matches(stack, goods))
                {
                    have += stack.Amount;
                }
            }

            if (have < goods.Amount)
            {
                error = $"Your cart no longer holds {goods.DisplayName}.";
                return false;
            }

            if (record.Zeny + cost > ItemTransfer.MaxZeny)
            {
                error = $"You can't hold more than {ItemTransfer.MaxZeny:N0} zeny.";
                return false;
            }

            int remaining = goods.Amount;
            for (int i = 0; i < record.Cart.Count && remaining > 0; i++)
            {
                var stack = record.Cart[i];
                if (!ItemTransfer.Matches(stack, goods))
                {
                    continue;
                }

                int take = Math.Min(stack.Amount, remaining);
                stack.Amount -= take;
                remaining -= take;
                if (stack.Amount <= 0)
                {
                    record.Cart.RemoveAt(i--);
                }
            }

            record.Zeny += Math.Max(0, cost);
            error = null;
            return true;
        }

        /// <summary>
        /// Buyer side: the goods arrive (the zeny was paid when the order went out). What doesn't fit in the bag waits in
        /// the Pushcart hold; returns how many entries went there.
        /// </summary>
        public static int Receive(CharacterRecord record, Inventory inventory, ItemStack goods)
        {
            if (record == null || inventory == null || goods == null)
            {
                return 0;
            }

            var leftovers = ItemTransfer.AddAll(inventory, new[] { goods });
            ItemTransfer.AddToCart(record, leftovers);
            return leftovers.Count;
        }

        /// <summary>One line of the stall, or null.</summary>
        public static VendingEntry Line(VendingStall stall, int index)
        {
            return stall?.Entries != null && index >= 0 && index < stall.Entries.Count && stall.Entries[index]?.Item?.Definition != null
                ? stall.Entries[index]
                : null;
        }

        /// <summary>Checks a stall listing that came over the network (title, lines, prices) and cleans it up.</summary>
        public static bool Validate(VendingStall stall, out string error)
        {
            if (stall == null || stall.Entries == null || stall.Entries.Count == 0)
            {
                error = "The stall is empty.";
                return false;
            }

            if (stall.Entries.Count > MaxEntries)
            {
                error = $"A stall holds at most {MaxEntries} lines.";
                return false;
            }

            foreach (var entry in stall.Entries)
            {
                if (entry?.Item?.Definition == null || entry.Item.Amount <= 0 || entry.Price < MinPrice || entry.Price > MaxPrice)
                {
                    error = "A line on the stall isn't valid.";
                    return false;
                }

                entry.Item = ItemTransfer.Copy(entry.Item, entry.Item.Amount);
            }

            stall.Title = CleanTitle(stall.Title);
            stall.Earned = 0;
            error = null;
            return true;
        }
    }
}
