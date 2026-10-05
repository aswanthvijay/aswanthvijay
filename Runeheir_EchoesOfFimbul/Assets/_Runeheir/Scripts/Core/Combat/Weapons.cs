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
                default: return 150f;
            }
        }

        /// <summary>Edge-to-edge reach in meters (body radii are added by the attacker logic).</summary>
        public static float AttackRange(WeaponType type)
        {
            switch (type)
            {
                case WeaponType.Bow: return 9f;
                case WeaponType.Spear: return 2.2f;
                case WeaponType.TwoHandSword: return 1.3f;
                case WeaponType.OneHandSword:
                case WeaponType.Mace:
                case WeaponType.Staff:
                    return 1.1f;
                default:
                    return 0.9f;
            }
        }

        public static bool IsRanged(WeaponType type)
        {
            return type == WeaponType.Bow;
        }

        /// <summary>Blunt weapons get the frozen-target bonus (GDD Glacial Tempest).</summary>
        public static bool IsBlunt(WeaponType type)
        {
            return type == WeaponType.Unarmed || type == WeaponType.Mace || type == WeaponType.Staff || type == WeaponType.Knuckle;
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
