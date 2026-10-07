using System;
using System.Collections.Generic;
using Runeheir.Characters;
using Runeheir.Items;

namespace Runeheir.Social
{
    /// <summary>One side of a player trade: up to <see cref="TradeSession.MaxItems"/> items and some zeny.</summary>
    [Serializable]
    public sealed class TradeOffer
    {
        public List<ItemStack> Items = new List<ItemStack>();
        public long Zeny;

        /// <summary>"OK" pressed: the offer can't change any more.</summary>
        public bool Locked;

        /// <summary>"Trade" pressed (only once both sides are locked).</summary>
        public bool Confirmed;

        public TradeOffer Clone()
        {
            return new TradeOffer { Items = ItemTransfer.Clean(Items), Zeny = Zeny, Locked = Locked, Confirmed = Confirmed };
        }
    }

    /// <summary>
    /// A trade between two characters as the server keeps it (Ragnarok flow): both sides add items and zeny, both press
    /// OK to lock, then both press Trade. Each client then checks it can give its side and carry the other before the
    /// swap happens.
    /// </summary>
    public sealed class TradeSession
    {
        public const int MaxItems = 10;

        public TradeSession(string a, string b)
        {
            A = a;
            B = b;
        }

        public string A { get; }

        public string B { get; }

        public TradeOffer OfferA { get; } = new TradeOffer();

        public TradeOffer OfferB { get; } = new TradeOffer();

        public bool Cancelled { get; private set; }

        public bool BothLocked => OfferA.Locked && OfferB.Locked;

        public bool BothConfirmed => OfferA.Confirmed && OfferB.Confirmed;

        public bool Involves(string name)
        {
            return Same(name, A) || Same(name, B);
        }

        public string PartnerOf(string name)
        {
            return Same(name, A) ? B : Same(name, B) ? A : null;
        }

        public TradeOffer OfferOf(string name)
        {
            return Same(name, A) ? OfferA : Same(name, B) ? OfferB : null;
        }

        public TradeOffer PartnerOffer(string name)
        {
            return Same(name, A) ? OfferB : Same(name, B) ? OfferA : null;
        }

        public bool TryAddItem(string name, ItemStack item, out string error)
        {
            var offer = Editable(name, out error);
            if (offer == null)
            {
                return false;
            }

            var copy = ItemTransfer.Copy(item, item?.Amount ?? 0);
            if (copy == null)
            {
                error = "That item can't be traded.";
                return false;
            }

            var existing = copy.Definition.IsStackable ? offer.Items.Find(i => ItemTransfer.Matches(i, copy)) : null;
            if (existing != null)
            {
                existing.Amount = (int)Math.Min(Inventory.MaxStack, (long)existing.Amount + copy.Amount);
                return true;
            }

            if (offer.Items.Count >= MaxItems)
            {
                error = $"A trade holds at most {MaxItems} items.";
                return false;
            }

            offer.Items.Add(copy);
            return true;
        }

        public bool TrySetZeny(string name, long zeny, out string error)
        {
            var offer = Editable(name, out error);
            if (offer == null)
            {
                return false;
            }

            if (zeny < 0 || zeny > ItemTransfer.MaxZeny)
            {
                error = "That's not a valid amount of zeny.";
                return false;
            }

            offer.Zeny = zeny;
            return true;
        }

        /// <summary>"OK": this side's offer is final.</summary>
        public bool TryLock(string name, out string error)
        {
            var offer = Editable(name, out error);
            if (offer == null)
            {
                return false;
            }

            offer.Locked = true;
            return true;
        }

        /// <summary>"Trade": both offers must be locked first.</summary>
        public bool TryConfirm(string name, out string error)
        {
            var offer = OfferOf(name);
            if (offer == null || Cancelled)
            {
                error = "You aren't trading.";
                return false;
            }

            if (!BothLocked)
            {
                error = "Both sides have to press OK first.";
                return false;
            }

            offer.Confirmed = true;
            error = null;
            return true;
        }

        public void Cancel()
        {
            Cancelled = true;
        }

        private TradeOffer Editable(string name, out string error)
        {
            var offer = OfferOf(name);
            if (offer == null || Cancelled)
            {
                error = "You aren't trading.";
                return null;
            }

            if (offer.Locked)
            {
                error = "Your offer is locked.";
                return null;
            }

            error = null;
            return offer;
        }

        private static bool Same(string a, string b)
        {
            return a != null && string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>The swap itself, run by each client on its own character.</summary>
    public static class PlayerTradeRules
    {
        /// <summary>How close two characters must stand to trade (meters).</summary>
        public const float MaxDistance = 6f;

        /// <summary>
        /// True when this character can hand over <paramref name="give"/> and carry <paramref name="receive"/>
        /// (<paramref name="currentWeight"/> includes worn equipment).
        /// </summary>
        public static bool CanExchange(CharacterRecord record, Inventory inventory, int capacity, int currentWeight, TradeOffer give,
            TradeOffer receive, out string error)
        {
            give = give ?? new TradeOffer();
            receive = receive ?? new TradeOffer();
            if (record == null || inventory == null)
            {
                error = "No character.";
                return false;
            }

            if (give.Zeny < 0 || receive.Zeny < 0 || record.Zeny < give.Zeny)
            {
                error = "You don't have that much zeny.";
                return false;
            }

            if (record.Zeny - give.Zeny + receive.Zeny > ItemTransfer.MaxZeny)
            {
                error = $"You can't hold more than {ItemTransfer.MaxZeny:N0} zeny.";
                return false;
            }

            if (!ItemTransfer.HasAll(inventory, give.Items, out error))
            {
                return false;
            }

            return ItemTransfer.CanReceive(inventory, capacity, currentWeight, give.Items, ItemTransfer.Clean(receive.Items), out error);
        }

        /// <summary>Gives this side away (the first half of the swap); nothing changes unless all of it is there.</summary>
        public static bool TryGive(CharacterRecord record, Inventory inventory, TradeOffer give, out string error)
        {
            give = give ?? new TradeOffer();
            if (record == null || record.Zeny < give.Zeny || give.Zeny < 0)
            {
                error = "You don't have that much zeny.";
                return false;
            }

            if (!ItemTransfer.HasAll(inventory, give.Items, out error) || !ItemTransfer.TryRemoveAll(inventory, give.Items))
            {
                error = error ?? "Your items changed.";
                return false;
            }

            record.Zeny -= give.Zeny;
            inventory.NotifyChanged();
            return true;
        }

        /// <summary>
        /// Takes the other side (the second half). Whatever no longer fits in the bag waits in the Pushcart hold;
        /// returns how many entries went there.
        /// </summary>
        public static int Receive(CharacterRecord record, Inventory inventory, TradeOffer receive)
        {
            if (record == null || inventory == null || receive == null)
            {
                return 0;
            }

            record.Zeny = Math.Min(ItemTransfer.MaxZeny, record.Zeny + Math.Max(0, receive.Zeny));
            var leftovers = ItemTransfer.AddAll(inventory, receive.Items);
            ItemTransfer.AddToCart(record, leftovers);
            inventory.NotifyChanged();
            return leftovers.Count;
        }

        /// <summary>Puts a side back after a trade fell through between the two halves.</summary>
        public static int Refund(CharacterRecord record, Inventory inventory, TradeOffer given)
        {
            return Receive(record, inventory, given);
        }
    }
}
