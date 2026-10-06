using System;
using System.Collections.Generic;
using Runeheir.Characters;
using Runeheir.Combat;
using Runeheir.Stats;

namespace Runeheir.Items
{
    public sealed class RunewordDefinition
    {
        public string Id;
        public string Name;
        public string Description;
        public string GlyphA;
        public string GlyphB;
        public EquipEffect Effect;

        public bool Matches(string a, string b)
        {
            return (a == GlyphA && b == GlyphB) || (a == GlyphB && b == GlyphA);
        }
    }

    /// <summary>
    /// GDD §7 Runic Fuller Etching: every weapon has two fuller grooves. Carve an Elder Futhark glyph into each at the
    /// Dwarven Forge; the right pair forms a Runeword (Blade of Dawn, Glacial Shroud, Giant's Cleave).
    /// Carving over a groove replaces (and destroys) the glyph that was there.
    /// </summary>
    public static class RunewordRules
    {
        public const int Grooves = 2;
        public const int EtchZeny = 10000;

        public const string Sowilo = "glyph_sowilo";
        public const string Tiwaz = "glyph_tiwaz";
        public const string Isa = "glyph_isa";
        public const string Hagalaz = "glyph_hagalaz";
        public const string Thurisaz = "glyph_thurisaz";
        public const string Uruz = "glyph_uruz";

        /// <summary>Glyph item id → rune name.</summary>
        public static readonly IReadOnlyList<KeyValuePair<string, string>> GlyphNames = new[]
        {
            new KeyValuePair<string, string>(Sowilo, "Sowilo"),
            new KeyValuePair<string, string>(Tiwaz, "Tiwaz"),
            new KeyValuePair<string, string>(Isa, "Isa"),
            new KeyValuePair<string, string>(Hagalaz, "Hagalaz"),
            new KeyValuePair<string, string>(Thurisaz, "Thurisaz"),
            new KeyValuePair<string, string>(Uruz, "Uruz"),
        };

        public static readonly IReadOnlyList<RunewordDefinition> Runewords = new[]
        {
            new RunewordDefinition
            {
                Id = "blade_of_dawn", Name = "Blade of Dawn", GlyphA = Sowilo, GlyphB = Tiwaz,
                Description = "Sowilo + Tiwaz: the weapon turns Holy; +15% damage to Undead and Demons, +5 CRIT.",
                Effect = new EquipEffect { WeaponElement = Element.Holy, Modifiers = new StatModifiers { Crit = 5f } }.Vs(Race.Undead, 15f).Vs(Race.Demon, 15f),
            },
            new RunewordDefinition
            {
                Id = "glacial_shroud", Name = "Glacial Shroud", GlyphA = Isa, GlyphB = Hagalaz,
                Description = "Isa + Hagalaz: the weapon turns Water; 5% chance on melee hit to Freeze for 3s; take 10% less Fire damage.",
                Effect = new EquipEffect { WeaponElement = Element.Water }
                    .Proc(new OnHitEffect { Status = StatusEffect.Freeze, ChancePercent = 5f, Duration = 3f })
                    .From(Element.Fire, -10f),
            },
            new RunewordDefinition
            {
                Id = "giants_cleave", Name = "Giant's Cleave", GlyphA = Thurisaz, GlyphB = Uruz,
                Description = "Thurisaz + Uruz: +20% damage to Large monsters, +50% poise damage, +5 STR.",
                Effect = new EquipEffect { Modifiers = new StatModifiers { PoiseDamagePercent = 50f }.SetStat(StatType.Str, 5) }.Vs(Size.Large, 20f),
            },
        };

        public static bool IsGlyph(string itemId)
        {
            foreach (var glyph in GlyphNames)
            {
                if (string.Equals(glyph.Key, itemId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The Runeword the weapon's two grooves spell, or null.</summary>
        public static RunewordDefinition ActiveRuneword(ItemStack weapon)
        {
            if (weapon?.Glyphs == null || weapon.Glyphs.Length < Grooves)
            {
                return null;
            }

            foreach (var runeword in Runewords)
            {
                if (runeword.Matches(weapon.Glyphs[0], weapon.Glyphs[1]))
                {
                    return runeword;
                }
            }

            return null;
        }

        /// <summary>Carves <paramref name="glyphId"/> into groove <paramref name="groove"/> of a weapon in the bag (costs the glyph and <see cref="EtchZeny"/>).</summary>
        public static bool TryEtch(CharacterRecord record, Inventory inventory, ItemStack weapon, int groove, string glyphId, out string message)
        {
            var definition = weapon?.Definition;
            if (definition == null || !definition.IsWeapon || !inventory.Contains(weapon))
            {
                message = "Choose a weapon from your bag (unequip it first).";
                return false;
            }

            if (groove < 0 || groove >= Grooves)
            {
                message = "That weapon has two grooves.";
                return false;
            }

            if (!IsGlyph(glyphId) || !inventory.Has(glyphId))
            {
                message = "You need that glyph.";
                return false;
            }

            if (record.Zeny < EtchZeny)
            {
                message = $"Etching costs {EtchZeny:N0} zeny.";
                return false;
            }

            weapon.Sanitize();
            inventory.TryRemove(glyphId);
            record.Zeny -= EtchZeny;
            weapon.Glyphs[groove] = ItemCatalog.Get(glyphId).Id;
            inventory.NotifyChanged();
            var runeword = ActiveRuneword(weapon);
            message = runeword != null
                ? $"The glyphs flare: {runeword.Name}! {runeword.Description}"
                : $"{ItemCatalog.Get(glyphId).Name} carved into groove {groove + 1}.";
            return true;
        }
    }
}
