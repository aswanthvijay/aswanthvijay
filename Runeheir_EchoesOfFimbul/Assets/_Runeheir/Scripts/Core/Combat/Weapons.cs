using System;

namespace Runeheir.Combat
{
    public enum WeaponType
    {
        Unarmed = 0,
        Dagger = 1,
        OneHandSword = 2,
        TwoHandSword = 3,
        Spear = 4,
        Mace = 5,
        Staff = 6,
        Bow = 7,
        Knuckle = 8,
        Katar = 9,

        // Phase 7: the full roster's weapons.
        Axe = 10,
        TwoHandAxe = 11,

        /// <summary>Skald lyres and harps.</summary>
        Instrument = 12,

        /// <summary>Seidkona lashes.</summary>
        Whip = 13,

        /// <summary>Rune tomes (Sage and Gothi lines).</summary>
        Book = 14,

        /// <summary>Thunderer firearms: rods that throw lightning-driven shot.</summary>
        ThunderRod = 15,

        /// <summary>Nightraider great shuriken.</summary>
        Huuma = 16,

        /// <summary>Freyja's Kin staves.</summary>
        CatStaff = 17,
    }

    /// <summary>A set of weapon types (skill requirements, weapon-specific passives). None = any weapon.</summary>
    [Flags]
    public enum WeaponMask
    {
        None = 0,
        Unarmed = 1 << (int)WeaponType.Unarmed,
        Dagger = 1 << (int)WeaponType.Dagger,
        OneHandSword = 1 << (int)WeaponType.OneHandSword,
        TwoHandSword = 1 << (int)WeaponType.TwoHandSword,
        Spear = 1 << (int)WeaponType.Spear,
        Mace = 1 << (int)WeaponType.Mace,
        Staff = 1 << (int)WeaponType.Staff,
        Bow = 1 << (int)WeaponType.Bow,
        Knuckle = 1 << (int)WeaponType.Knuckle,
        Katar = 1 << (int)WeaponType.Katar,
        Axe = 1 << (int)WeaponType.Axe,
        TwoHandAxe = 1 << (int)WeaponType.TwoHandAxe,
        Instrument = 1 << (int)WeaponType.Instrument,
        Whip = 1 << (int)WeaponType.Whip,
        Book = 1 << (int)WeaponType.Book,
        ThunderRod = 1 << (int)WeaponType.ThunderRod,
        Huuma = 1 << (int)WeaponType.Huuma,
        CatStaff = 1 << (int)WeaponType.CatStaff,

        Swords = OneHandSword | TwoHandSword,
        Axes = Axe | TwoHandAxe,
        Blades = Dagger | Katar | OneHandSword,
        AnyMelee = Unarmed | Dagger | OneHandSword | TwoHandSword | Spear | Mace | Staff | Knuckle | Katar | Axe | TwoHandAxe | Instrument | Whip
                   | Book | Huuma | CatStaff,
        Ranged = Bow | ThunderRod,
    }

    public static class WeaponMasks
    {
        public static WeaponMask Of(WeaponType type)
        {
            return (WeaponMask)(1 << (int)type);
        }

        /// <summary>True when <paramref name="mask"/> is None (any weapon) or contains <paramref name="type"/>.</summary>
        public static bool Allows(WeaponMask mask, WeaponType type)
        {
            return mask == WeaponMask.None || (mask & Of(type)) != 0;
        }

        public static string Describe(WeaponMask mask)
        {
            if (mask == WeaponMask.None)
            {
                return "any weapon";
            }

            var names = new System.Collections.Generic.List<string>();
            foreach (WeaponType type in Enum.GetValues(typeof(WeaponType)))
            {
                if ((mask & Of(type)) != 0)
                {
                    names.Add(WeaponRules.Label(type));
                }
            }

            return string.Join(" / ", names);
        }
    }

    /// <summary>The weapon a character attacks with. Equipment/refining arrives in Phase 4.</summary>
    [Serializable]
    public struct WeaponProfile
    {
        public string Name;
        public WeaponType Type;
        public int Atk;

        /// <summary>Weapon level 1..4 (GDD weapon tiers).</summary>
        public int Level;

        /// <summary>+0..+20 refine.</summary>
        public int Refine;

        public Element Element;

        public WeaponProfile(string name, WeaponType type, int atk, int level, int refine = 0, Element element = Element.Neutral)
        {
            Name = name;
            Type = type;
            Atk = atk;
            Level = level;
            Refine = refine;
            Element = element;
        }

        public static WeaponProfile BareHands => new WeaponProfile("Bare Hands", WeaponType.Unarmed, 0, 1);
    }

    /// <summary>Per-weapon-type tables: base ASPD, reach, size penalties, refine ATK.</summary>
    public static class WeaponRules
    {
        /// <summary>Base ASPD before AGI/DEX. Kept inside the GDD 150–197 band.</summary>
        public static float BaseAspd(WeaponType type)
        {
            switch (type)
            {
                case WeaponType.Unarmed: return 156f;
                case WeaponType.Dagger: return 154f;
                case WeaponType.Katar: return 153f;
                case WeaponType.Knuckle: return 153f;
                case WeaponType.OneHandSword: return 152f;
                case WeaponType.Mace: return 151f;
                case WeaponType.Bow: return 151f;
                case WeaponType.Spear: return 150f;
                case WeaponType.TwoHandSword: return 150f;
                case WeaponType.Staff: return 150f;
                case WeaponType.ThunderRod: return 153f;
                case WeaponType.Instrument:
                case WeaponType.Whip:
                case WeaponType.Book:
                case WeaponType.CatStaff:
                    return 152f;
                case WeaponType.Axe: return 151f;
                case WeaponType.TwoHandAxe:
                case WeaponType.Huuma:
                    return 150f;
                default: return 150f;
            }
        }

        public static string Label(WeaponType type)
        {
            switch (type)
            {
                case WeaponType.Unarmed: return "Unarmed";
                case WeaponType.Dagger: return "Dagger";
                case WeaponType.OneHandSword: return "Sword";
                case WeaponType.TwoHandSword: return "Greatsword";
                case WeaponType.Spear: return "Spear";
                case WeaponType.Mace: return "Mace";
                case WeaponType.Staff: return "Staff";
                case WeaponType.Bow: return "Bow";
                case WeaponType.Knuckle: return "Knuckles";
                case WeaponType.Katar: return "Katar";
                case WeaponType.Axe: return "Axe";
                case WeaponType.TwoHandAxe: return "Great Axe";
                case WeaponType.Instrument: return "Instrument";
                case WeaponType.Whip: return "Whip";
                case WeaponType.Book: return "Rune Tome";
                case WeaponType.ThunderRod: return "Thunder-Rod";
                case WeaponType.Huuma: return "Huuma Shuriken";
                case WeaponType.CatStaff: return "Cat Staff";
                default: return type.ToString();
            }
        }

        /// <summary>Poise damage of one basic hit (heavier weapons stagger sooner). See <see cref="PoiseRules"/>.</summary>
        public static float PoiseDamage(WeaponType type)
        {
            switch (type)
            {
                case WeaponType.TwoHandAxe: return 24f;
                case WeaponType.TwoHandSword: return 22f;
                case WeaponType.Axe: return 16f;
                case WeaponType.Huuma: return 14f;
                case WeaponType.Book: return 10f;
                case WeaponType.ThunderRod: return 7f;
                case WeaponType.Mace: return 18f;
                case WeaponType.Spear: return 16f;
                case WeaponType.Knuckle: return 14f;
                case WeaponType.OneHandSword: return 12f;
                case WeaponType.Katar: return 10f;
                case WeaponType.Bow: return 6f;
                default: return 8f;
            }
        }

        /// <summary>Edge-to-edge reach in meters (body radii are added by the attacker logic).</summary>
        public static float AttackRange(WeaponType type)
        {
            switch (type)
            {
                case WeaponType.Bow: return 9f;
                case WeaponType.ThunderRod: return 8f;
                case WeaponType.Spear: return 2.2f;
                case WeaponType.Instrument:
                case WeaponType.Whip:
                    return 2f;
                case WeaponType.Huuma: return 1.6f;
                case WeaponType.TwoHandSword:
                case WeaponType.TwoHandAxe:
                    return 1.3f;
                case WeaponType.OneHandSword:
                case WeaponType.Mace:
                case WeaponType.Staff:
                case WeaponType.Axe:
                case WeaponType.Book:
                case WeaponType.CatStaff:
                    return 1.1f;
                default:
                    return 0.9f;
            }
        }

        public static bool IsRanged(WeaponType type)
        {
            return type == WeaponType.Bow || type == WeaponType.ThunderRod;
        }

        /// <summary>Blunt weapons get the frozen-target bonus (GDD Glacial Tempest).</summary>
        public static bool IsBlunt(WeaponType type)
        {
            return type == WeaponType.Unarmed || type == WeaponType.Mace || type == WeaponType.Staff || type == WeaponType.Knuckle
                   || type == WeaponType.Book || type == WeaponType.CatStaff;
        }

        /// <summary>Classic size modifier table (percent of weapon ATK applied).</summary>
        public static int SizeModifierPercent(WeaponType type, Size size)
        {
            switch (type)
            {
                case WeaponType.Dagger: return Pick(size, 100, 75, 50);
                case WeaponType.OneHandSword: return Pick(size, 75, 100, 75);
                case WeaponType.TwoHandSword: return Pick(size, 75, 75, 100);
                case WeaponType.Spear: return Pick(size, 75, 75, 100);
                case WeaponType.Mace: return Pick(size, 75, 100, 100);
                case WeaponType.Bow: return Pick(size, 100, 100, 75);
                case WeaponType.Knuckle: return Pick(size, 100, 75, 50);
                case WeaponType.Katar: return Pick(size, 75, 100, 75);
                case WeaponType.Axe:
                case WeaponType.TwoHandAxe:
                    return Pick(size, 50, 75, 100);
                case WeaponType.Instrument: return Pick(size, 75, 100, 75);
                case WeaponType.Whip: return Pick(size, 75, 100, 50);
                case WeaponType.Book: return Pick(size, 100, 100, 50);
                case WeaponType.Huuma: return Pick(size, 75, 75, 100);
                default: return 100;
            }
        }

        /// <summary>Flat ATK per refine by weapon level (Lv1 +2, Lv2 +3, Lv3 +5, Lv4 +7).</summary>
        public static int RefineAtkBonus(int weaponLevel, int refine)
        {
            int perRefine;
            switch (Math.Max(1, Math.Min(4, weaponLevel)))
            {
                case 1: perRefine = 2; break;
                case 2: perRefine = 3; break;
                case 3: perRefine = 5; break;
                default: perRefine = 7; break;
            }

            return perRefine * Math.Max(0, refine);
        }

        private static int Pick(Size size, int small, int medium, int large)
        {
            switch (size)
            {
                case Size.Small: return small;
                case Size.Medium: return medium;
                default: return large;
            }
        }
    }
}
