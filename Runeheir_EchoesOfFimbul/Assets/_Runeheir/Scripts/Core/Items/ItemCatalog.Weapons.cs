using Runeheir.Combat;
using Runeheir.Stats;

namespace Runeheir.Items
{
    /// <summary>
    /// GDD §5: 28 base weapons in the 10 GDD families (Claymores, Flamberges, Katars, Daggers, Oak Wands, Yggdrasil Staves,
    /// Knuckles, Maces, Longbows, Lances), tiers 1–4 with up to 4 sockets. Each job's starter weapon is one of them.
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
            Weapon("templar_mace", "Templar Mace", WeaponType.Mace, 4, 200, 1, 1, 140, 280000, "Holy element, +15% vs Undead and Demons.",
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
        }

        private static void Weapon(string id, string name, WeaponType type, int level, int atk, int sockets, int equipLevel, int weight, int price,
            string description, bool? twoHanded = null, Element element = Element.Neutral, int minTier = 0, EquipEffect effect = null)
        {
            bool bothHands = twoHanded ?? (type == WeaponType.TwoHandSword || type == WeaponType.Katar || type == WeaponType.Bow || type == WeaponType.Spear);
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
                default: return "WPN";
            }
        }
    }
}
