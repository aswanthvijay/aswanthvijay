using System.Collections.Generic;
using System.Text;
using Runeheir.Combat;
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

        /// <summary>Hotkey-bar tooltip at the player's learned level.</summary>
        public static string Tooltip(HotkeySlot slot, Player.PlayerCharacter player)
        {
            switch (slot.Kind)
            {
                case HotkeyKind.Skill:
                {
                    var skill = SkillCatalog.Get(slot.Id);
                    if (skill == null)
                    {
                        return null;
                    }

                    int level = player.SkillBook.UsableLevel(skill.Id);
                    string text = SkillTooltip(skill, level, player.Stats.CastTimeMultiplier, showNext: false);
                    return $"{text}\n<color=#9AA8BC>{HotkeyUseHint}</color>";
                }

                case HotkeyKind.Item: return ItemTooltip(ItemCatalog.Get(slot.Id), HotkeyUseHint);
                default: return null;
            }
        }

        /// <summary>
        /// Skill tooltip: description, the numbers at <paramref name="level"/> (or Lv 1 when unlearned), and the next
        /// level's numbers when <paramref name="showNext"/>. <paramref name="castMultiplier"/> is the DEX cast multiplier.
        /// </summary>
        public static string SkillTooltip(SkillDefinition skill, int level, float castMultiplier, bool showNext, string requirement = null)
        {
            if (skill == null)
            {
                return null;
            }

            var text = new StringBuilder();
            text.Append($"<b><color=#EBC466>{skill.Name}</color></b>");
            text.Append(skill.MaxLevel > 1 ? $"  <color=#9AA8BC>Lv {level}/{skill.MaxLevel}</color>" : level > 0 ? string.Empty : "  <color=#9AA8BC>not learned</color>");
            text.Append('\n').Append(skill.Description);

            int shown = Mathf.Max(1, level);
            text.Append($"\n<color=#C9D3E0>{(level > 0 ? $"Lv {shown}" : "At Lv 1")}: {LevelSummary(skill, shown, castMultiplier)}</color>");
            if (showNext && level > 0 && level < skill.MaxLevel)
            {
                text.Append($"\n<color=#9AA8BC>Next, Lv {level + 1}: {LevelSummary(skill, level + 1, castMultiplier)}</color>");
            }

            if (skill.Weapons != WeaponMask.None)
            {
                text.Append($"\n<color=#9AA8BC>Needs: {WeaponMasks.Describe(skill.Weapons)}</color>");
            }

            if (!string.IsNullOrEmpty(requirement))
            {
                text.Append($"\n<color=#E6735C>{requirement}</color>");
            }

            return text.ToString();
        }

        /// <summary>One line of a skill's numbers at a level: damage, area, status, buff, costs and timing.</summary>
        public static string LevelSummary(SkillDefinition skill, int level, float castMultiplier)
        {
            if (skill.Passive)
            {
                return skill.Proc != null ? $"{skill.Proc.Chance.At(level):0.#}% chance per basic hit" : "Passive bonus (see above)";
            }

            var parts = new List<string>();
            if (skill.Damage != SkillDamage.None && skill.Special != SkillSpecial.FistOfOdin)
            {
                string kind = skill.IsMagic ? "MATK" : "ATK";
                string hits = skill.Special == SkillSpecial.SpiritRelease
                    ? $" x{level} max"
                    : skill.Hits.AtInt(level) > 1 ? $" x{skill.Hits.AtInt(level)}" : string.Empty;
                string element = !skill.UseWeaponElement && skill.Element != Element.Neutral ? $" {skill.Element}" : string.Empty;
                parts.Add($"{skill.Power.At(level):0}% {kind}{hits}{element}");
            }

            if (skill.Special == SkillSpecial.FistOfOdin)
            {
                parts.Add($"ATK x (8 + SP/10) + {skill.FlatDamage.At(level):0}");
            }

            float radius = skill.Radius.At(level);
            if (radius > 0f)
            {
                parts.Add($"radius {radius:0.#} m");
            }

            if (skill.Special == SkillSpecial.Heal)
            {
                parts.Add(skill.FlatHeal.IsZero ? $"Heal Lv {skill.HealLevel.AtInt(level)}" : $"+{skill.FlatHeal.At(level):0} HP + Base Lv");
            }
            else if (skill.Special == SkillSpecial.Zone && !skill.FlatHeal.IsZero)
            {
                parts.Add($"+{skill.FlatHeal.At(level):0} HP/s");
            }

            if (skill.Special == SkillSpecial.Steal)
            {
                parts.Add($"{StealRules.Chance(level, 0, 0):0}% + (DEX - monster Lv)/2");
            }

            if (skill.Status != StatusEffect.None && skill.StatusChance.At(level) > 0f)
            {
                parts.Add($"{skill.StatusChance.At(level):0.#}% {StatusRules.Get(skill.Status).Name} {skill.StatusDuration.At(level):0.#}s");
            }

            float buffSeconds = skill.Special == SkillSpecial.Zone ? skill.ZoneDuration.At(level) : skill.BuffDuration.At(level);
            if (buffSeconds <= 0f && skill.BuffId != null)
            {
                buffSeconds = BuffCatalog.Get(skill.BuffId)?.Duration ?? 0f;
            }

            if (buffSeconds > 0f)
            {
                parts.Add($"lasts {buffSeconds:0.#}s");
            }

            if (!skill.BuffCharges.IsZero)
            {
                parts.Add($"{skill.BuffCharges.AtInt(level)} blocks");
            }

            if (skill.Target != SkillTarget.Self)
            {
                parts.Add($"range {skill.Range.At(level):0.#} m");
            }

            parts.Add($"SP {skill.SpCost.AtInt(level)}");
            if (skill.HpCostPercent.At(level) > 0f)
            {
                parts.Add($"{skill.HpCostPercent.At(level):0}% HP");
            }

            if (skill.SphereCost > 0)
            {
                parts.Add($"{skill.SphereCost} sphere{(skill.SphereCost > 1 ? "s" : string.Empty)}");
            }

            float cast = skill.CastTime.At(level);
            parts.Add(cast > 0f ? $"cast {cast * castMultiplier:0.0#}s" : "instant");
            float cooldown = skill.Cooldown.At(level);
            if (cooldown > 0f)
            {
                parts.Add($"cooldown {cooldown:0.#}s");
            }

            return string.Join(" · ", parts);
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
