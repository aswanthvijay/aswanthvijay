using Runeheir.Characters;

namespace Runeheir.Items
{
    /// <summary>
    /// GDD §6 card compounding: a Soul Card goes into a free socket of matching equipment in your bag, permanently.
    /// A Rune of Extraction (GDD §7) pulls every card back out, keeping both the cards and the equipment.
    /// </summary>
    public static class CardRules
    {
        public static bool CanCompound(Inventory inventory, string cardId, ItemStack target, out string reason)
        {
            var card = ItemCatalog.Get(cardId);
            var item = target?.Definition;
            if (card == null || !card.IsCard || !inventory.Has(card.Id))
            {
                reason = "You don't have that card.";
                return false;
            }

            if (item == null || !item.IsEquipment || !inventory.Contains(target))
            {
                reason = "Choose a piece of equipment from your bag (unequip it first).";
                return false;
            }

            if (item.EquipKind != card.CardTarget)
            {
                reason = $"{card.Name} only fits {EquipKinds.Label(card.CardTarget)} equipment.";
                return false;
            }

            target.Sanitize();
            if (target.FreeSockets <= 0)
            {
                reason = item.Sockets == 0 ? $"{item.Name} has no sockets." : $"Every socket of {item.Name} is full.";
                return false;
            }

            reason = null;
            return true;
        }

        public static bool TryCompound(Inventory inventory, string cardId, ItemStack target, out string message)
        {
            if (!CanCompound(inventory, cardId, target, out message))
            {
                return false;
            }

            var card = ItemCatalog.Get(cardId);
            for (int i = 0; i < target.Cards.Length; i++)
            {
                if (string.IsNullOrEmpty(target.Cards[i]))
                {
                    target.Cards[i] = card.Id;
                    break;
                }
            }

            inventory.TryRemove(card.Id);
            inventory.NotifyChanged();
            message = $"{card.Name} compounded into {target.DisplayName}.";
            return true;
        }

        /// <summary>Uses a Rune of Extraction: every card returns to the bag and the equipment keeps its refine.</summary>
        public static bool TryExtract(Inventory inventory, ItemStack target, out string message)
        {
            var item = target?.Definition;
            if (item == null || !inventory.Contains(target))
            {
                message = "Choose a piece of equipment from your bag (unequip it first).";
                return false;
            }

            target.Sanitize();
            if (target.CardCount == 0)
            {
                message = $"{item.Name} has no cards.";
                return false;
            }

            if (!inventory.Has(ItemCatalog.RuneOfExtraction))
            {
                message = "You need a Rune of Extraction.";
                return false;
            }

            if (inventory.Stacks.Count + target.CardCount > Inventory.MaxEntries)
            {
                message = "Your bag is too full to hold the cards.";
                return false;
            }

            inventory.TryRemove(ItemCatalog.RuneOfExtraction);
            int extracted = 0;
            for (int i = 0; i < target.Cards.Length; i++)
            {
                if (!string.IsNullOrEmpty(target.Cards[i]))
                {
                    inventory.Add(target.Cards[i], 1);
                    target.Cards[i] = string.Empty;
                    extracted++;
                }
            }

            inventory.NotifyChanged();
            message = $"The Rune of Extraction pulls {extracted} card{(extracted > 1 ? "s" : string.Empty)} out of {target.DisplayName}.";
            return true;
        }
    }
}
