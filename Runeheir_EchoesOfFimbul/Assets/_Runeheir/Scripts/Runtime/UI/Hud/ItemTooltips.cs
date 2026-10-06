using System.Text;
using Runeheir.Combat;
using Runeheir.Items;
using Runeheir.Jobs;
using Runeheir.Player;

namespace Runeheir.UI
{
    /// <summary>Rich tooltips for item instances: refine, weapon level, sockets and their cards, glyphs, runeword, requirements.</summary>
    public static class ItemTooltips
    {
        public static string For(ItemStack entry, string hint = null)
        {
            var item = entry?.Definition;
            if (item == null)
            {
                return null;
            }

            var text = new StringBuilder();
            text.Append($"<b><color=#EBC466>{entry.DisplayName}</color></b>");
            if (entry.Amount > 1)
            {
                text.Append($"  <color=#9AA8BC>x{entry.Amount:N0}</color>");
            }

            if (item.IsEquipment)
            {
                AppendEquipment(text, entry, item);
            }
            else
            {
                text.Append('\n').Append(item.Description);
            }

            text.Append($"\n<color=#9AA8BC>Weight {item.Weight} · Sells for {item.SellPrice:N0} z");
            if (!string.IsNullOrEmpty(hint))
            {
                text.Append(" · ").Append(hint);
            }

            return text.Append("</color>").ToString();
        }

        private static void AppendEquipment(StringBuilder text, ItemStack entry, ItemDefinition item)
        {
            text.Append($"\n<color=#9AA8BC>{EquipKinds.Label(item.EquipKind)}");
            if (item.IsWeapon)
            {
                text.Append($" · {WeaponRules.Label(item.WeaponType)} · Weapon Lv {item.WeaponLevel}{(item.IsTwoHanded ? " · two-handed" : string.Empty)}");
            }
            else
            {
                text.Append($" · {SlotsLabel(item.Slots)}");
            }

            text.Append("</color>");
            if (item.IsWeapon)
            {
                int refineAtk = WeaponRules.RefineAtkBonus(item.WeaponLevel, entry.Refine);
                text.Append($"\nATK {item.Atk}{(refineAtk > 0 ? $" <color=#7DCEA0>+{refineAtk}</color>" : string.Empty)}");
                if (item.Element != Element.Neutral)
                {
                    text.Append($" · {item.Element} element");
                }
            }
            else if (item.Def > 0 || item.Mdef > 0 || entry.Refine > 0)
            {
                int refineDef = item.Refinable ? entry.Refine * RefineRules.DefPerRefine : 0;
                text.Append($"\nDEF {item.Def}{(refineDef > 0 ? $" <color=#7DCEA0>+{refineDef}</color>" : string.Empty)} · MDEF {item.Mdef}");
            }

            text.Append('\n').Append(item.Description);

            if (item.Sockets > 0)
            {
                text.Append($"\n<color=#C39BD3>Sockets {entry.CardCount}/{item.Sockets}:</color>");
                foreach (string cardId in entry.Cards ?? System.Array.Empty<string>())
                {
                    var card = ItemCatalog.Get(cardId);
                    text.Append(card != null ? $"\n  ◆ {card.Name}: {StripTarget(card.Description)}" : "\n  ◇ <color=#9AA8BC>empty</color>");
                }
            }

            if (item.IsWeapon)
            {
                var runeword = RunewordRules.ActiveRuneword(entry);
                string groove1 = GlyphName(entry, 0);
                string groove2 = GlyphName(entry, 1);
                if (groove1 != null || groove2 != null)
                {
                    text.Append($"\n<color=#85C1E9>Runic Fuller: {groove1 ?? "—"} · {groove2 ?? "—"}</color>");
                }

                if (runeword != null)
                {
                    text.Append($"\n<color=#F7DC6F>Runeword {runeword.Name}: {runeword.Description}</color>");
                }
            }

            AppendRequirements(text, item);
        }

        private static void AppendRequirements(StringBuilder text, ItemDefinition item)
        {
            var player = PlayerCharacter.Local;
            text.Append("\n<color=#9AA8BC>");
            text.Append(item.EquipLevel > 1 ? $"Base Lv {item.EquipLevel}+" : "Any level");
            if (item.RequiredLine.HasValue)
            {
                text.Append($" · {JobDatabase.Get(item.RequiredLine.Value).Name} line");
            }

            if (item.MinTier >= 3)
            {
                text.Append(" · Ascended jobs");
            }
            else if (item.MinTier > 0)
            {
                text.Append($" · tier {item.MinTier}+ jobs");
            }

            text.Append(item.Refinable ? $" · refinable to +{RefineRules.MaxRefine}" : " · can't be refined");
            text.Append("</color>");
            if (player != null && !EquipmentSet.CanWear(player.Record, item, out string reason))
            {
                text.Append($"\n<color=#FF786B>{reason}</color>");
            }
        }

        private static string GlyphName(ItemStack entry, int groove)
        {
            var glyphs = entry.Glyphs;
            if (glyphs == null || groove >= glyphs.Length || string.IsNullOrEmpty(glyphs[groove]))
            {
                return null;
            }

            return ItemCatalog.Get(glyphs[groove])?.Name;
        }

        private static string SlotsLabel(EquipSlot slots)
        {
            if ((slots & EquipSlot.Head) != 0)
            {
                var parts = new System.Collections.Generic.List<string>();
                if ((slots & EquipSlot.HeadUpper) != 0) parts.Add("Upper");
                if ((slots & EquipSlot.HeadMid) != 0) parts.Add("Mid");
                if ((slots & EquipSlot.HeadLower) != 0) parts.Add("Lower");
                return string.Join(" + ", parts) + " head";
            }

            return slots == EquipSlot.Accessory ? "Accessory slot" : "Body";
        }

        /// <summary>Card descriptions start with their target ("[Weapon] ..."); inside an item that is noise.</summary>
        private static string StripTarget(string description)
        {
            int close = description.IndexOf("] ", System.StringComparison.Ordinal);
            return description.StartsWith("[") && close > 0 ? description.Substring(close + 2) : description;
        }
    }
}
