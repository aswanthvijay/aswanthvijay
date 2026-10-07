using Runeheir.Combat;
using Runeheir.Jobs;
using Runeheir.Stats;

namespace Runeheir.Items
{
    /// <summary>
    /// GDD §5 armor side of the 87 base items: 24 headgears, 4 animated wings, the named defensive gear (Viking and
    /// Valkyrian sets), everyday armor/shields/garments/footgear/accessories, and the three God Relics.
    /// </summary>
    public static partial class ItemCatalog
    {
        private static void RegisterGear()
        {
            // ---------------------------------------------------------------- 24 headgears
            // Upper
            Head("valkyrie_winged_helm", "Valkyrie Winged Helm", EquipSlot.HeadUpper, 18, 5, 1, 60, 120000, "#ECF0F1",
                "Silver wings sweep back from the temples. +2 AGI, +1 VIT.", Stats(StatType.Agi, 2, StatType.Vit, 1));
            Head("grand_horned_viking_crest", "Grand Horned Viking Crest", EquipSlot.HeadUpper, 25, 0, 1, 70, 150000, "#D5DBDB",
                "Two great horns. +3 STR, +3% Max HP.", new EquipEffect { Modifiers = new StatModifiers { MaxHpPercent = 3f }.SetStat(StatType.Str, 3) });
            Head("archmage_wizard_hat", "Archmage Wizard Hat", EquipSlot.HeadUpper, 8, 15, 1, 50, 140000, "#1B2A6B",
                "Mystic line only. +3 INT, +3% magic damage.", new EquipEffect { Modifiers = new StatModifiers { MagicDamagePercent = 3f }.SetStat(StatType.Int, 3) },
                line: JobId.Mystic);
            Head("feathered_beret", "Feathered Beret", EquipSlot.HeadUpper, 6, 0, 1, 30, 40000, "#C0392B",
                "A jaunty beret. +2 DEX, +5 HIT.", new EquipEffect { Modifiers = new StatModifiers { Hit = 5 }.SetStat(StatType.Dex, 2) });
            Head("iron_helm", "Iron Helm", EquipSlot.HeadUpper | EquipSlot.HeadMid, 22, 0, 1, 40, 25000, "#7B7D7D",
                "A full nasal helm covering upper and mid head.", null);
            Head("wolf_hood", "Wolf Hood", EquipSlot.HeadUpper, 10, 0, 0, 25, 9000, "#5D6D7E",
                "A wolf's head worn as a hood. +2 AGI, +3 FLEE.", new EquipEffect { Modifiers = new StatModifiers { Flee = 3 }.SetStat(StatType.Agi, 2) });
            Head("raven_hood", "Raven Hood", EquipSlot.HeadUpper, 8, 2, 0, 20, 8000, "#17202A",
                "Black feathers. +1 INT, +1 DEX.", Stats(StatType.Int, 1, StatType.Dex, 1));
            Head("antler_crown", "Antler Crown", EquipSlot.HeadUpper, 12, 0, 1, 40, 30000, "#A0522D",
                "Shed antlers bound in gold. +2 VIT, +200 Max HP.", new EquipEffect { Modifiers = new StatModifiers { MaxHp = 200 }.SetStat(StatType.Vit, 2) });
            Head("fur_cap", "Fur Cap", EquipSlot.HeadUpper, 7, 0, 0, 1, 1200, "#A9927D",
                "Warm against Fimbulwinter. Take 5% less Water damage.", new EquipEffect().From(Element.Water, -5f));
            Head("bandana", "Bandana", EquipSlot.HeadUpper, 3, 0, 0, 1, 400, "#922B21", "A strip of red cloth.", null);
            Head("rune_circlet", "Rune Circlet", EquipSlot.HeadUpper, 6, 6, 1, 30, 35000, "#F4D03F",
                "A thin band of runes. +2 INT.", Stats(StatType.Int, 2));
            Head("flower_crown", "Flower Crown", EquipSlot.HeadUpper, 2, 3, 0, 1, 800, "#F1948A", "Meadow flowers. +2 LUK.", Stats(StatType.Luk, 2));
            Head("frost_crown", "Frost Crown", EquipSlot.HeadUpper, 15, 10, 1, 90, 160000, "#AED6F1",
                "Ice that never melts. Take 15% less Water damage.", new EquipEffect().From(Element.Water, -15f));
            Head("helm_of_awe", "Helm of Awe", EquipSlot.HeadUpper | EquipSlot.HeadMid, 35, 8, 1, 150, 500000, "#B7950B",
                "Ægishjálmur. +2 all stats, +30% poise.", new EquipEffect { Modifiers = new StatModifiers { MaxPoisePercent = 30f }.AddAllStats(2) });

            // Mid
            Head("slotted_dark_sunglasses", "Slotted Dark Sunglasses", EquipSlot.HeadMid, 1, 0, 1, 10, 20000, "#1C2833",
                "Immune to Blind.", new EquipEffect().Immune(StatusEffect.Blind));
            Head("runed_blindfold", "Runed Blindfold", EquipSlot.HeadMid, 2, 5, 0, 30, 30000, "#5B2C6F",
                "See with the inner eye. +2 INT, 3% faster casting.", new EquipEffect { Modifiers = new StatModifiers { CastTimePercent = -3f }.SetStat(StatType.Int, 2) });
            Head("eyepatch", "Eyepatch", EquipSlot.HeadMid, 1, 0, 0, 1, 900, "#212F3D", "+3 HIT, +2 CRIT.", new EquipEffect { Modifiers = new StatModifiers { Hit = 3, Crit = 2f } });
            Head("rune_monocle", "Rune Monocle", EquipSlot.HeadMid, 1, 0, 0, 20, 7000, "#D4AC0D", "+2 DEX.", Stats(StatType.Dex, 2));
            Head("frost_goggles", "Frost Goggles", EquipSlot.HeadMid, 3, 0, 0, 40, 15000, "#85C1E9",
                "Take 5% less Water damage; +20% resistance to Freeze.",
                new EquipEffect { ResistStatus = StatusEffect.Freeze, ResistPercent = 20f }.From(Element.Water, -5f));

            // Lower
            Head("viking_pipe", "Viking Pipe", EquipSlot.HeadLower, 1, 0, 0, 1, 1500, "#6E2C00", "+1 VIT, +2 SP regen.", new EquipEffect { Modifiers = new StatModifiers { SpRegenFlat = 2 }.SetStat(StatType.Vit, 1) });
            Head("fang_mask", "Fang Mask", EquipSlot.HeadLower, 3, 0, 0, 20, 9000, "#F2F3F4", "A wolf's jaw. +1 STR, +5 ATK.", new EquipEffect { Modifiers = new StatModifiers { Atk = 5 }.SetStat(StatType.Str, 1) });
            Head("beard_of_odin", "Beard of Odin", EquipSlot.HeadLower, 2, 3, 0, 60, 90000, "#D5D8DC", "Wisdom grows on the chin. +2 INT, +5% Max SP.",
                new EquipEffect { Modifiers = new StatModifiers { MaxSpPercent = 5f }.SetStat(StatType.Int, 2) });
            Head("iron_mouthguard", "Iron Mouthguard", EquipSlot.HeadLower, 4, 0, 0, 30, 12000, "#7B7D7D", "+1 VIT, +20% resistance to Poison.",
                new EquipEffect { Modifiers = new StatModifiers().SetStat(StatType.Vit, 1), ResistStatus = StatusEffect.Poison, ResistPercent = 20f });
            Head("braided_beard", "Braided Beard", EquipSlot.HeadLower, 2, 0, 0, 10, 4000, "#A04000", "+1 STR, +1 VIT.", Stats(StatType.Str, 1, StatType.Vit, 1));

            // ---------------------------------------------------------------- 4 animated wings (garments)
            Wear("valkyrian_feather_wings", "Valkyrian Feather Wings", EquipSlot.Garment, 18, 5, 1, 90, 30, 300000, "#FDFEFE",
                "White wings that beat softly. +3 AGI, +5% movement speed.", new EquipEffect { Modifiers = new StatModifiers { MoveSpeedPercent = 5f }.SetStat(StatType.Agi, 3) });
            Wear("demon_nether_wings", "Demon Nether Wings", EquipSlot.Garment, 18, 5, 1, 90, 30, 300000, "#4A235A",
                "Leathery wings of Niflheim. +3 STR, take 10% less Holy damage.", new EquipEffect { Modifiers = new StatModifiers().SetStat(StatType.Str, 3) }.From(Element.Holy, -10f));
            Wear("fimbul_frost_wings", "Fimbul Frost Wings", EquipSlot.Garment, 18, 8, 1, 90, 30, 300000, "#AED6F1",
                "Wings of the endless winter. +3 INT, take 10% less Water damage.", new EquipEffect { Modifiers = new StatModifiers().SetStat(StatType.Int, 3) }.From(Element.Water, -10f));
            Wear("dragon_flame_wings", "Dragon Flame Wings", EquipSlot.Garment, 18, 5, 1, 90, 30, 300000, "#E74C3C",
                "Ember-edged wings. +3 VIT, take 10% less Fire damage.", new EquipEffect { Modifiers = new StatModifiers().SetStat(StatType.Vit, 3) }.From(Element.Fire, -10f));

            // ---------------------------------------------------------------- GDD defensive gear
            Wear("round_viking_shield", "Round Viking Shield", EquipSlot.Shield, 30, 0, 1, 20, 100, 15000, "#A04000", "Painted linden boards. +1 VIT.", Stats(StatType.Vit, 1));
            Wear("valkyrian_shield", "Valkyrian Shield", EquipSlot.Shield, 80, 10, 1, 99, 120, 300000, "#F7F9F9",
                "Transcendent and expanded jobs only. Take 15% less Fire, Water, Shadow and Undead damage.",
                new EquipEffect().From(Element.Fire, -15f).From(Element.Water, -15f).From(Element.Shadow, -15f).From(Element.Undead, -15f), minTier: 3);
            Wear("runic_full_plate", "Runic Full Plate", EquipSlot.Armor, 120, 0, 1, 70, 300, 150000, "#7F8C8D", "Plate etched with warding runes. +5% Max HP.",
                new EquipEffect { Modifiers = new StatModifiers { MaxHpPercent = 5f } });
            Wear("valkyrian_armor", "Valkyrian Armor", EquipSlot.Armor, 150, 15, 1, 99, 250, 350000, "#F4F6F7", "Transcendent and expanded jobs only. +5% Max HP and Max SP.",
                new EquipEffect { Modifiers = new StatModifiers { MaxHpPercent = 5f, MaxSpPercent = 5f } }, minTier: 3);
            Wear("wolfskin_mantle", "Wolfskin Mantle", EquipSlot.Garment, 15, 0, 1, 30, 40, 20000, "#5D6D7E", "Take 5% less Neutral damage.", new EquipEffect().From(Element.Neutral, -5f));
            Wear("valkyrian_manteau", "Valkyrian Manteau", EquipSlot.Garment, 35, 5, 1, 99, 40, 300000, "#FBFCFC", "Transcendent and expanded jobs only. +10 FLEE, take 10% less Neutral damage.",
                new EquipEffect { Modifiers = new StatModifiers { Flee = 10 } }.From(Element.Neutral, -10f), minTier: 3);
            Wear("plated_greaves", "Plated Greaves", EquipSlot.Footgear, 30, 0, 1, 60, 120, 60000, "#839192", "+5% Max HP.", new EquipEffect { Modifiers = new StatModifiers { MaxHpPercent = 5f } });
            Wear("valkyrian_boots", "Valkyrian Boots", EquipSlot.Footgear, 40, 0, 1, 99, 60, 300000, "#F8F9F9", "Transcendent and expanded jobs only. +10% movement speed, +5% Max HP.",
                new EquipEffect { Modifiers = new StatModifiers { MoveSpeedPercent = 10f, MaxHpPercent = 5f } }, minTier: 3);

            // ---------------------------------------------------------------- everyday armor
            Wear("cotton_tunic", "Cotton Tunic", EquipSlot.Armor, 10, 0, 1, 1, 10, 500, "#D5D8DC", "Simple homespun cloth.", null);
            Wear("leather_jerkin", "Leather Jerkin", EquipSlot.Armor, 25, 0, 1, 15, 80, 4000, "#A04000", "Boiled leather.", null);
            Wear("chainmail", "Chainmail", EquipSlot.Armor, 60, 0, 1, 40, 220, 30000, "#909497", "Riveted iron rings.", null);
            Wear("mystic_robe", "Mystic Robe", EquipSlot.Armor, 30, 15, 1, 30, 40, 25000, "#2E4053", "Woven with runic thread. +2 INT.", Stats(StatType.Int, 2));
            Wear("saint_robe", "Saint's Robe", EquipSlot.Armor, 45, 10, 1, 45, 60, 40000, "#FDFEFE", "Devotee line only. Take 10% less Shadow damage.",
                new EquipEffect().From(Element.Shadow, -10f), line: JobId.Devotee);

            Wear("buckler", "Buckler", EquipSlot.Shield, 15, 0, 1, 1, 60, 2000, "#A04000", "A small round shield.", null);
            Wear("kite_shield", "Kite Shield", EquipSlot.Shield, 50, 0, 1, 50, 160, 45000, "#7B7D7D", "A tall teardrop shield.", null);
            Wear("mirror_shield", "Mirror Shield", EquipSlot.Shield, 40, 15, 1, 60, 120, 70000, "#D6EAF8", "Polished silver. Reflects 5% of magic damage.",
                new EquipEffect { Modifiers = new StatModifiers { ReflectMagicPercent = 5f } });

            Wear("traveler_cloak", "Traveler's Cloak", EquipSlot.Garment, 8, 0, 1, 1, 20, 800, "#784212", "Road-worn wool.", null);
            Wear("bear_pelt", "Bear Pelt", EquipSlot.Garment, 20, 0, 1, 40, 60, 25000, "#6E2C00", "+1 VIT.", Stats(StatType.Vit, 1));
            Wear("rune_muffler", "Rune Muffler", EquipSlot.Garment, 12, 5, 1, 25, 25, 15000, "#B03A2E", "Take 3% less Neutral damage.", new EquipEffect().From(Element.Neutral, -3f));

            Wear("sandals", "Sandals", EquipSlot.Footgear, 6, 0, 1, 1, 20, 400, "#A04000", "Leather straps.", null);
            Wear("leather_boots", "Leather Boots", EquipSlot.Footgear, 12, 0, 1, 15, 40, 3500, "#6E2C00", "Sturdy boots.", null);
            Wear("fur_boots", "Fur Boots", EquipSlot.Footgear, 22, 0, 1, 40, 50, 20000, "#A9927D", "+3% Max HP.", new EquipEffect { Modifiers = new StatModifiers { MaxHpPercent = 3f } });
            Wear("swift_boots", "Swift Boots", EquipSlot.Footgear, 18, 0, 1, 70, 40, 90000, "#48C9B0", "+1 AGI, +5% movement speed.",
                new EquipEffect { Modifiers = new StatModifiers { MoveSpeedPercent = 5f }.SetStat(StatType.Agi, 1) });

            // ---------------------------------------------------------------- accessories (never refined)
            Wear("clip_ring", "Clip Ring", EquipSlot.Accessory, 0, 0, 1, 1, 10, 3000, "#BDC3C7", "A plain ring with a socket.", null);
            Wear("rune_ring", "Rune Ring", EquipSlot.Accessory, 0, 0, 0, 20, 10, 15000, "#F4D03F", "+2 INT.", Stats(StatType.Int, 2));
            Wear("wolf_tooth_necklace", "Wolf Tooth Necklace", EquipSlot.Accessory, 0, 0, 0, 20, 10, 15000, "#F2F3F4", "+2 STR.", Stats(StatType.Str, 2));
            Wear("raven_brooch", "Raven Brooch", EquipSlot.Accessory, 0, 0, 0, 20, 10, 15000, "#17202A", "+2 AGI.", Stats(StatType.Agi, 2));
            Wear("ward_amulet", "Ward Amulet", EquipSlot.Accessory, 0, 0, 0, 20, 10, 15000, "#58D68D", "+2 VIT.", Stats(StatType.Vit, 2));

            // ---------------------------------------------------------------- God Relics (GDD §5)
            Wear("megingjard", "Megingjard", EquipSlot.Accessory, 0, 0, 0, 150, 50, 5000000, "#B7950B",
                "Thor's belt of strength. +40 STR; +1 ATK per 2 Base Levels.",
                new EquipEffect { Modifiers = new StatModifiers().SetStat(StatType.Str, 40), AtkPerBaseLevel = 0.5f });
            Wear("brisingamen", "Brisingamen", EquipSlot.Accessory, 0, 0, 0, 150, 20, 5000000, "#F5B041",
                "Freyja's necklace. +10 all stats, +20% Max SP.", new EquipEffect { Modifiers = new StatModifiers { MaxSpPercent = 20f }.AddAllStats(10) });
            Wear("mjolnir", "Mjolnir", EquipSlot.Accessory, 0, 0, 0, 150, 100, 5000000, "#85929E",
                "Thor's hammer as a talisman. +50 DEX; ASPD permanently at the 197 maximum.",
                new EquipEffect { Modifiers = new StatModifiers { AspdOverride = StatFormulas.MaxAspd }.SetStat(StatType.Dex, 50) });
        }

        private static void Head(string id, string name, EquipSlot slots, int def, int mdef, int sockets, int equipLevel, int price, string color,
            string description, EquipEffect effect, JobId? line = null)
        {
            // Only upper headgear refines (Ragnarok rule); mid/lower pieces are cosmetic-light.
            Wear(id, name, slots, def, mdef, sockets, equipLevel, 20, price, color, description, effect, line: line,
                refinable: (slots & EquipSlot.HeadUpper) != 0);
        }

        private static void Wear(string id, string name, EquipSlot slots, int def, int mdef, int sockets, int equipLevel, int weight, int price, string color,
            string description, EquipEffect effect, int minTier = 0, JobId? line = null, bool? refinable = null)
        {
            Register(new ItemDefinition
            {
                Id = id, Name = name, Description = description, Kind = ItemKind.Equipment, Slots = slots,
                Def = def, Mdef = mdef, Sockets = sockets, EquipLevel = equipLevel, Weight = weight, Price = price,
                MinTier = minTier, RequiredLine = line, Effect = effect, ViewColorHex = color,
                Refinable = refinable ?? (slots & EquipSlot.Accessory) == 0,
                IconLabel = GearIcon(slots), IconColorHex = color,
            });
        }

        private static EquipEffect Stats(StatType a, int amountA, StatType? b = null, int amountB = 0)
        {
            var modifiers = new StatModifiers().SetStat(a, amountA);
            if (b.HasValue)
            {
                modifiers.SetStat(b.Value, amountB);
            }

            return new EquipEffect { Modifiers = modifiers };
        }

        private static string GearIcon(EquipSlot slots)
        {
            if ((slots & EquipSlot.Head) != 0) return (slots & EquipSlot.HeadUpper) != 0 ? "HAT" : (slots & EquipSlot.HeadMid) != 0 ? "EYE" : "MTH";
            if ((slots & EquipSlot.Armor) != 0) return "ARM";
            if ((slots & EquipSlot.Shield) != 0) return "SHD";
            if ((slots & EquipSlot.Garment) != 0) return "CLK";
            if ((slots & EquipSlot.Footgear) != 0) return "BTS";
            return "ACC";
        }
    }
}
