using Runeheir.Combat;
using Runeheir.Stats;

namespace Runeheir.Items
{
    /// <summary>
    /// GDD §5: 28 base weapons in the 10 GDD families (Claymores, Flamberges, Katars, Daggers, Oak Wands, Yggdrasil Staves,
    /// Knuckles, Maces, Longbows, Lances), tiers 1–4 with up to 4 sockets, plus the Phase 7 roster's families (axes,
    /// instruments, whips, rune tomes, thunder-rods, huuma shuriken, cat staves). Each job's starter weapon is one of them.
    /// </summary>
    public static partial class ItemCatalog
    {
        public const string RustySeax = "rusty_seax";
        public const string IronClaymore = "iron_claymore";
        public const string Seax = "seax";
        public const string OakWand = "oak_wand";
        public const string IronMace = "iron_mace";

        private static void RegisterWeapons()
        {
            // ---- Claymores (two-handed swords)
            Weapon("iron_claymore", "Iron Claymore", WeaponType.TwoHandSword, 2, 100, 3, 1, 200, 4000, "A plain but honest greatsword.");
            Weapon("steel_claymore", "Steel Claymore", WeaponType.TwoHandSword, 3, 150, 2, 40, 250, 30000, "Dwarf-forged steel, balanced for wide swings.");
            Weapon("einherjar_greatsword", "Einherjar Greatsword", WeaponType.TwoHandSword, 4, 230, 1, 1, 280, 300000,
                "Carried out of Valhalla by the chosen dead.", minTier: 3);

            // ---- Flamberges
            Weapon("flamberge", "Flamberge", WeaponType.TwoHandSword, 3, 160, 2, 1, 240, 60000, "A wavy blade that tears as it cuts.", minTier: 2);
            Weapon("muspel_flamberge", "Muspel Flamberge", WeaponType.TwoHandSword, 4, 215, 1, 120, 260, 250000,
                "Forged in Muspelheim's fire. Fire element.", element: Element.Fire,
                effect: new EquipEffect { Modifiers = new StatModifiers().SetStat(StatType.Str, 2) });

            // ---- Katars
            Weapon("jamadhar", "Jamadhar", WeaponType.Katar, 3, 120, 2, 1, 120, 50000, "Punch-blades for close killing. +5 CRIT.", minTier: 2,
                effect: new EquipEffect { Modifiers = new StatModifiers { Crit = 5f } });
            Weapon("shadow_katar", "Shadow Katar", WeaponType.Katar, 4, 190, 1, 1, 130, 280000, "Drinks the light around it. Shadow element, +10 CRIT.",
                element: Element.Shadow, minTier: 3, effect: new EquipEffect { Modifiers = new StatModifiers { Crit = 10f } });

            // ---- Daggers
            Weapon(RustySeax, "Rusty Seax", WeaponType.Dagger, 1, 17, 3, 1, 40, 100, "Every Initiate's first blade.");
            Weapon(Seax, "Seax", WeaponType.Dagger, 2, 64, 2, 1, 60, 2400, "A sharp single-edged knife.");
            Weapon("frost_stiletto", "Frost Stiletto", WeaponType.Dagger, 3, 105, 1, 55, 60, 45000, "Rimed with Niflheim frost. Water element.", element: Element.Water);

            // ---- Oak Wands (one-handed staves)
            Weapon(OakWand, "Oak Wand", WeaponType.Staff, 1, 25, 3, 1, 40, 500, "A runed oak twig. +15 MATK, +1 INT.",
                effect: new EquipEffect { Modifiers = new StatModifiers { Matk = 15 }.SetStat(StatType.Int, 1) });
            Weapon("runed_oak_wand", "Runed Oak Wand", WeaponType.Staff, 2, 40, 2, 24, 50, 6000, "+40 MATK, +2 INT.",
                effect: new EquipEffect { Modifiers = new StatModifiers { Matk = 40 }.SetStat(StatType.Int, 2) });

            // ---- Yggdrasil Staves (two-handed)
            Weapon("runed_staff", "Runed Staff", WeaponType.Staff, 2, 45, 2, 1, 120, 12000, "+60 MATK, +3 INT.", twoHanded: true, minTier: 2,
                effect: new EquipEffect { Modifiers = new StatModifiers { Matk = 60 }.SetStat(StatType.Int, 3) });
            Weapon("rune_tome_staff", "Rune Tome Staff", WeaponType.Staff, 2, 50, 2, 1, 120, 12000, "A staff bound with a book of runes. +55 MATK, +5% Max SP.",
                twoHanded: true, minTier: 2, effect: new EquipEffect { Modifiers = new StatModifiers { Matk = 55, MaxSpPercent = 5f } });
            Weapon("norn_staff", "Norn Staff", WeaponType.Staff, 4, 85, 1, 1, 130, 260000, "Cut from the Norns' well-side ash. +140 MATK, 5% faster casting.",
                twoHanded: true, minTier: 3, effect: new EquipEffect { Modifiers = new StatModifiers { Matk = 140, CastTimePercent = -5f } });
            Weapon("yggdrasil_staff", "Yggdrasil Staff", WeaponType.Staff, 4, 80, 1, 1, 130, 260000, "A living branch of the World Tree. +160 MATK, +5 INT.",
                twoHanded: true, minTier: 3, effect: new EquipEffect { Modifiers = new StatModifiers { Matk = 160 }.SetStat(StatType.Int, 5) });

            // ---- Knuckles
            Weapon("wolf_claws", "Wolf Claws", WeaponType.Knuckle, 2, 70, 2, 20, 80, 9000, "Iron claws strapped over the knuckles.");
            Weapon("iron_knuckles", "Iron Knuckles", WeaponType.Knuckle, 3, 110, 2, 1, 90, 40000, "Heavy studded knuckles.", minTier: 2);
            Weapon("fist_of_odin_knuckles", "Fist of Odin Knuckles", WeaponType.Knuckle, 4, 180, 1, 1, 100, 280000, "Gold-banded knuckles. +3 STR.",
                minTier: 3, effect: new EquipEffect { Modifiers = new StatModifiers().SetStat(StatType.Str, 3) });

            // ---- Maces
            Weapon(IronMace, "Iron Mace", WeaponType.Mace, 2, 75, 2, 1, 100, 3500, "A flanged iron mace.");
            Weapon("holy_mace", "Holy Mace", WeaponType.Mace, 3, 130, 1, 1, 120, 55000, "Blessed in Vigrid Haven. Holy element, +10% vs Undead.",
                element: Element.Holy, minTier: 2, effect: new EquipEffect().Vs(Race.Undead, 10f));
            Weapon("templar_mace", "Hallowed Mace", WeaponType.Mace, 4, 200, 1, 1, 140, 280000, "The High Gothi's mace. Holy element, +15% vs Undead and Demons.",
                element: Element.Holy, minTier: 3, effect: new EquipEffect().Vs(Race.Undead, 15f).Vs(Race.Demon, 15f));

            // ---- Longbows (two-handed)
            Weapon("hunters_bow", "Hunter's Bow", WeaponType.Bow, 1, 40, 3, 1, 60, 1500, "A short hunting bow.", twoHanded: true);
            Weapon("yew_longbow", "Yew Longbow", WeaponType.Bow, 3, 110, 2, 1, 90, 45000, "A tall yew bow. +2 DEX.", twoHanded: true, minTier: 2,
                effect: new EquipEffect { Modifiers = new StatModifiers().SetStat(StatType.Dex, 2) });
            Weapon("raven_longbow", "Raven Longbow", WeaponType.Bow, 4, 180, 1, 1, 100, 280000, "Fletched with raven feathers. +4 DEX, +10 HIT.",
                twoHanded: true, minTier: 3, effect: new EquipEffect { Modifiers = new StatModifiers { Hit = 10 }.SetStat(StatType.Dex, 4) });

            // ---- Lances (two-handed spears)
            Weapon("iron_spear", "Iron Spear", WeaponType.Spear, 1, 60, 3, 1, 150, 1800, "A long iron-tipped spear.", twoHanded: true);
            Weapon("rune_lance", "Rune Lance", WeaponType.Spear, 3, 150, 2, 1, 200, 50000, "Runes run the length of its shaft.", twoHanded: true, minTier: 2);
            Weapon("valkyrian_lance", "Valkyrian Lance", WeaponType.Spear, 4, 220, 1, 1, 220, 300000, "A Valkyrie's spear. +3 VIT, +5% Max HP.",
                twoHanded: true, minTier: 3, effect: new EquipEffect { Modifiers = new StatModifiers { MaxHpPercent = 5f }.SetStat(StatType.Vit, 3) });

            RegisterRosterWeapons();
        }

        /// <summary>Phase 7: the weapon families of the full roster, and the new jobs' gift weapons.</summary>
        private static void RegisterRosterWeapons()
        {
            // ---- Axes (Warrior, Trader lines and Initiates)
            Weapon("woodcutters_axe", "Woodcutter's Axe", WeaponType.Axe, 1, 38, 3, 1, 80, 500, "Splits firewood and skulls alike.");
            Weapon("bearded_axe", "Bearded Axe", WeaponType.Axe, 2, 82, 2, 18, 110, 5200, "The long-hooked axe of the raiders.");
            Weapon("dwarven_axe", "Dwarven Axe", WeaponType.Axe, 3, 135, 2, 1, 150, 48000, "Svartalfheim steel. +2 STR.", minTier: 2,
                effect: new EquipEffect { Modifiers = new StatModifiers().SetStat(StatType.Str, 2) });
            Weapon("herbwife_sickle", "Herbwife's Sickle", WeaponType.Axe, 3, 120, 2, 1, 110, 46000, "For cutting herbs, and whatever eats them. +2 DEX.",
                minTier: 2, effect: new EquipEffect { Modifiers = new StatModifiers().SetStat(StatType.Dex, 2) });
            Weapon("idunn_sickle", "Iðunn's Golden Sickle", WeaponType.Axe, 4, 185, 1, 1, 120, 270000,
                "Harvests the apples of youth. +3 VIT, +5% Max HP.", minTier: 3,
                effect: new EquipEffect { Modifiers = new StatModifiers { MaxHpPercent = 5f }.SetStat(StatType.Vit, 3) });
            Weapon("dane_axe", "Dane Axe", WeaponType.TwoHandAxe, 3, 160, 2, 40, 240, 34000, "A broad two-handed war axe.");
            Weapon("eitri_great_axe", "Eitri's Great Axe", WeaponType.TwoHandAxe, 4, 235, 1, 1, 280, 300000,
                "Forged by the dwarf who made Mjölnir. +3 STR, +3 DEX.", minTier: 3,
                effect: new EquipEffect { Modifiers = new StatModifiers().SetStat(StatType.Str, 3).SetStat(StatType.Dex, 3) });

            // ---- Instruments (Skald and Thul)
            Weapon("willow_lyre", "Willow Lyre", WeaponType.Instrument, 2, 60, 3, 20, 60, 6000, "A six-string lyre. +1 DEX.",
                effect: new EquipEffect { Modifiers = new StatModifiers().SetStat(StatType.Dex, 1) });
            Weapon("tagelharpa", "Tagelharpa", WeaponType.Instrument, 3, 115, 2, 1, 80, 48000, "A bowed horsehair lyre of the north. +2 DEX.",
                minTier: 2, effect: new EquipEffect { Modifiers = new StatModifiers().SetStat(StatType.Dex, 2) });
            Weapon("bragis_harp", "Bragi's Harp", WeaponType.Instrument, 4, 180, 1, 1, 90, 280000,
                "The god of poetry's harp. +3 DEX, +3 INT.", minTier: 3,
                effect: new EquipEffect { Modifiers = new StatModifiers().SetStat(StatType.Dex, 3).SetStat(StatType.Int, 3) });

            // ---- Whips (Seidkona and Völva)
            Weapon("leather_lash", "Leather Lash", WeaponType.Whip, 2, 58, 3, 20, 50, 6000, "Braided elk hide. +1 AGI.",
                effect: new EquipEffect { Modifiers = new StatModifiers().SetStat(StatType.Agi, 1) });
            Weapon("seidr_lash", "Seidr Lash", WeaponType.Whip, 3, 115, 2, 1, 60, 48000, "Knotted with spell-cords. +2 DEX.", minTier: 2,
                effect: new EquipEffect { Modifiers = new StatModifiers().SetStat(StatType.Dex, 2) });
            Weapon("serpent_lash", "Serpent Lash", WeaponType.Whip, 4, 180, 1, 1, 70, 280000, "Scaled like Jörmungandr. +3 AGI, +3 DEX.",
                minTier: 3, effect: new EquipEffect { Modifiers = new StatModifiers().SetStat(StatType.Agi, 3).SetStat(StatType.Dex, 3) });

            // ---- Rune tomes (Sage and Gothi lines)
            Weapon("rune_primer", "Rune Primer", WeaponType.Book, 2, 55, 3, 20, 60, 6000, "Futhark drills for the young. +25 MATK.",
                effect: new EquipEffect { Modifiers = new StatModifiers { Matk = 25 } });
            Weapon("eddic_codex", "Eddic Codex", WeaponType.Book, 3, 110, 2, 1, 80, 50000, "The lays of gods and heroes. +50 MATK, +2 INT.", minTier: 2,
                effect: new EquipEffect { Modifiers = new StatModifiers { Matk = 50 }.SetStat(StatType.Int, 2) });
            Weapon("book_of_mimir", "Book of Mímir", WeaponType.Book, 4, 170, 1, 1, 90, 280000, "Wisdom from Mímir's well. +100 MATK, +4 INT.",
                minTier: 3, effect: new EquipEffect { Modifiers = new StatModifiers { Matk = 100 }.SetStat(StatType.Int, 4) });

            // ---- Thunder-rods (Thunderer)
            Weapon("spark_rod", "Spark Rod", WeaponType.ThunderRod, 1, 42, 3, 1, 60, 1200, "An iron rod that spits sparks and shot.", twoHanded: true);
            Weapon("thunder_carbine", "Thunder Carbine", WeaponType.ThunderRod, 2, 88, 2, 24, 90, 9500, "A longer rod with a heavier crack.", twoHanded: true);
            Weapon("storm_rod", "Storm Rod", WeaponType.ThunderRod, 3, 140, 2, 50, 120, 52000, "Wind element: the shot rides a gale.",
                twoHanded: true, element: Element.Wind);
            Weapon("thors_wrath", "Thor's Wrath", WeaponType.ThunderRod, 4, 215, 1, 1, 140, 300000, "Thunder made iron. Wind element, +3 DEX, +10 HIT.",
                twoHanded: true, element: Element.Wind, minTier: 3,
                effect: new EquipEffect { Modifiers = new StatModifiers { Hit = 10 }.SetStat(StatType.Dex, 3) });

            // ---- Huuma shuriken (Nightraider)
            Weapon("iron_huuma", "Iron Huuma", WeaponType.Huuma, 2, 92, 2, 1, 150, 8000, "A great folding shuriken.", twoHanded: true);
            Weapon("frost_huuma", "Frost Huuma", WeaponType.Huuma, 3, 150, 2, 50, 160, 48000, "Rimed with Niflheim frost. Water element.",
                twoHanded: true, element: Element.Water);
            Weapon("fenrir_huuma", "Fenrir's Fang", WeaponType.Huuma, 4, 225, 1, 1, 170, 300000, "Shaped from the wolf's shed tooth. +3 AGI, +5 CRIT.",
                twoHanded: true, minTier: 3, effect: new EquipEffect { Modifiers = new StatModifiers { Crit = 5f }.SetStat(StatType.Agi, 3) });

            // ---- Cat staves (Freyja's Kin)
            Weapon("bygul_staff", "Bygul's Staff", WeaponType.CatStaff, 1, 30, 3, 1, 40, 600, "Named for one of Freyja's cats. +15 MATK.",
                effect: new EquipEffect { Modifiers = new StatModifiers { Matk = 15 } });
            Weapon("trjegul_staff", "Trjegul's Staff", WeaponType.CatStaff, 2, 60, 2, 30, 50, 9000, "Named for the other. +45 MATK, +2 INT.",
                effect: new EquipEffect { Modifiers = new StatModifiers { Matk = 45 }.SetStat(StatType.Int, 2) });
            Weapon("brisingamen_staff", "Brísingamen Staff", WeaponType.CatStaff, 4, 120, 1, 1, 60, 300000,
                "Set with a shard of Freyja's necklace. +150 MATK, +5 INT.", minTier: 3,
                effect: new EquipEffect { Modifiers = new StatModifiers { Matk = 150 }.SetStat(StatType.Int, 5) });

            // ---- Gift weapons of the new jobs
            Weapon("cutpurse_dagger", "Cutpurse Dagger", WeaponType.Dagger, 3, 112, 2, 1, 60, 48000, "Thin enough to slit a purse string. +5 FLEE.",
                minTier: 2, effect: new EquipEffect { Modifiers = new StatModifiers { Flee = 5 } });
            Weapon("lokis_sting", "Loki's Sting", WeaponType.Dagger, 4, 175, 1, 1, 70, 280000, "The trickster's own knife. +3 AGI, +5 CRIT.",
                minTier: 3, effect: new EquipEffect { Modifiers = new StatModifiers { Crit = 5f }.SetStat(StatType.Agi, 3) });
            Weapon("fylgja_wand", "Fylgja Wand", WeaponType.Staff, 3, 60, 2, 1, 50, 45000, "Carved with the shapes of guardian spirits. +80 MATK.",
                minTier: 2, effect: new EquipEffect { Modifiers = new StatModifiers { Matk = 80 } });
        }

        private static void Weapon(string id, string name, WeaponType type, int level, int atk, int sockets, int equipLevel, int weight, int price,
            string description, bool? twoHanded = null, Element element = Element.Neutral, int minTier = 0, EquipEffect effect = null)
        {
            bool bothHands = twoHanded ?? (type == WeaponType.TwoHandSword || type == WeaponType.TwoHandAxe || type == WeaponType.Katar
                                           || type == WeaponType.Bow || type == WeaponType.Spear || type == WeaponType.ThunderRod
                                           || type == WeaponType.Huuma);
            Register(new ItemDefinition
            {
                Id = id, Name = name, Description = description, Kind = ItemKind.Equipment,
                Slots = bothHands ? EquipSlot.TwoHanded : EquipSlot.Weapon,
                WeaponType = type, WeaponLevel = level, Atk = atk, Element = element, Sockets = sockets,
                EquipLevel = equipLevel, MinTier = minTier, Weight = weight, Price = price,
                IconLabel = WeaponIcon(type), IconColorHex = "#B2BABB", Effect = effect,
            });
        }

        private static string WeaponIcon(WeaponType type)
        {
            switch (type)
            {
                case WeaponType.TwoHandSword: return "2HS";
                case WeaponType.Katar: return "KTR";
                case WeaponType.Dagger: return "DGR";
                case WeaponType.Staff: return "STF";
                case WeaponType.Knuckle: return "KNK";
                case WeaponType.Mace: return "MCE";
                case WeaponType.Bow: return "BOW";
                case WeaponType.Spear: return "LNC";
                case WeaponType.Axe: return "AXE";
                case WeaponType.TwoHandAxe: return "2HA";
                case WeaponType.Instrument: return "LYR";
                case WeaponType.Whip: return "WHP";
                case WeaponType.Book: return "TOM";
                case WeaponType.ThunderRod: return "ROD";
                case WeaponType.Huuma: return "HMA";
                case WeaponType.CatStaff: return "CAT";
                default: return "WPN";
            }
        }
    }
}
