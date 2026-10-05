using Runeheir.Hotkeys;
using Runeheir.Items;
using Runeheir.Skills;
using Runeheir.Visuals;
using UnityEngine;

namespace Runeheir.UI
{
    /// <summary>Glyph/color/tooltip text for skills and items until real icon art exists.</summary>
    public static class HudIcons
    {
        public static string Glyph(HotkeySlot slot)
        {
            switch (slot.Kind)
            {
                case HotkeyKind.Skill: return SkillCatalog.Get(slot.Id)?.IconLabel ?? "?";
                case HotkeyKind.Item: return ItemCatalog.Get(slot.Id)?.IconLabel ?? "?";
                default: return string.Empty;
            }
        }

        public static Color Tint(HotkeySlot slot)
        {
            switch (slot.Kind)
            {
                case HotkeyKind.Skill: return RuntimeMaterials.Hex(SkillCatalog.Get(slot.Id)?.IconColorHex);
                case HotkeyKind.Item: return RuntimeMaterials.Hex(ItemCatalog.Get(slot.Id)?.IconColorHex);
                default: return UITheme.SlotBg;
            }
        }

        public const string InventoryUseHint = "Double-click or right-click to use";
        public const string HotkeyUseHint = "Click or F-key to use · right-click to clear · drag off to unbind";

        /// <summary>Hotkey-bar tooltip; <paramref name="castMultiplier"/> is the player's DEX cast multiplier.</summary>
        public static string Tooltip(HotkeySlot slot, float castMultiplier)
        {
            switch (slot.Kind)
            {
                case HotkeyKind.Skill:
                {
                    string text = SkillTooltip(SkillCatalog.Get(slot.Id), castMultiplier);
                    return text == null ? null : $"{text}\n<color=#9AA8BC>{HotkeyUseHint}</color>";
                }

                case HotkeyKind.Item: return ItemTooltip(ItemCatalog.Get(slot.Id), HotkeyUseHint);
                default: return null;
            }
        }

        public static string SkillTooltip(SkillDefinition skill, float castMultiplier)
        {
            if (skill == null)
            {
                return null;
            }

            string cast = skill.CastTime > 0f
                ? $"Cast {skill.CastTime * castMultiplier:0.0#}s (base {skill.CastTime:0.0}s)"
                : "Instant";
            string cooldown = skill.Cooldown > 0f ? $" · Cooldown {skill.Cooldown:0.#}s" : string.Empty;
            return $"<b><color=#EBC466>{skill.Name}</color></b>\n{skill.Description}\n<color=#9AA8BC>SP {skill.SpCost} · {cast}{cooldown} · {TargetLabel(skill.Target)}</color>";
        }

        public static string ItemTooltip(ItemDefinition item, string useHint = InventoryUseHint)
        {
            return item == null
                ? null
                : $"<b><color=#EBC466>{item.Name}</color></b>\n{item.Description}\n<color=#9AA8BC>Weight {item.Weight} · {useHint}</color>";
        }

        public static string TargetLabel(SkillTarget target)
        {
            switch (target)
            {
                case SkillTarget.Self: return "Self";
                case SkillTarget.Enemy: return "Target enemy";
                case SkillTarget.Friend: return "Target ally/self";
                default: return "Target ground";
            }
        }
    }
}
