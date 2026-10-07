using System;
using Runeheir.Combat;
using Runeheir.Jobs;

namespace Runeheir.Items
{
    public enum ItemKind
    {
        Consumable = 0,
        Etc = 1,
        Equipment = 2,

        /// <summary>Soul Card: compounds into a socket of matching equipment (GDD §6).</summary>
        Card = 3,

        /// <summary>Crafting material: refine ores, catalytic runes, etching glyphs (GDD §7).</summary>
        Material = 4,
    }

    public enum ItemSpecialEffect
    {
        None = 0,

        /// <summary>Butterfly Wing equivalent: warp to the save point.</summary>
        ReturnToSavePoint = 1,

        /// <summary>Fly Wing equivalent: random teleport on the current map.</summary>
        RandomTeleport = 2,

        /// <summary>Sowilo: cleanse every status and debuff.</summary>
        Cleanse = 3,

        /// <summary>Dead Branch: summons a random monster.</summary>
        SummonMonster = 4,

        /// <summary>Blood Branch: summons a random high-level boss.</summary>
        SummonBoss = 5,
    }

    /// <summary>Where an item can be worn (GDD §5 10-slot paperdoll). Two-handed weapons are Weapon | Shield.</summary>
    [Flags]
    public enum EquipSlot
    {
        None = 0,
        HeadUpper = 1 << 0,
        HeadMid = 1 << 1,
        HeadLower = 1 << 2,
        Armor = 1 << 3,
        Weapon = 1 << 4,
        Shield = 1 << 5,
        Garment = 1 << 6,
        Footgear = 1 << 7,
        Accessory = 1 << 8,

        Head = HeadUpper | HeadMid | HeadLower,
        TwoHanded = Weapon | Shield,
    }

    /// <summary>The 10 paperdoll positions, in <c>CharacterRecord.Equipment</c> order.</summary>
    public enum EquipPosition
    {
        HeadUpper = 0,
        HeadMid = 1,
        HeadLower = 2,
        Armor = 3,
        Weapon = 4,
        Shield = 5,
        Garment = 6,
        Footgear = 7,
        Accessory1 = 8,
        Accessory2 = 9,
    }

    /// <summary>Equipment family: which cards fit, how refining works.</summary>
    public enum EquipKind
    {
        None = 0,
        Weapon = 1,
        Armor = 2,
        Shield = 3,
        Garment = 4,
        Footgear = 5,
        Headgear = 6,
        Accessory = 7,
    }

    public sealed class ItemDefinition
    {
        public string Id;
        public string Name;
        public string Description;
        public ItemKind Kind;
        public int Weight;
        public string IconLabel;
        public string IconColorHex = "#7F8C8D";

        /// <summary>Shop price in zeny; merchants buy it back for half.</summary>
        public int Price;

        // ------------------------------------------------------------ consumables
        public int HealHpMin;
        public int HealHpMax;
        public float HealHpPercent;
        public int HealSpMin;
        public int HealSpMax;
        public string BuffId;
        public ItemSpecialEffect Special;

        // ------------------------------------------------------------ equipment
        public EquipSlot Slots;

        /// <summary>Minimum Base Level to wear it.</summary>
        public int EquipLevel = 1;

        /// <summary>Minimum job tier (compared with the job's GearTier; 3 = transcendent and expanded jobs, e.g. the Valkyrian set).</summary>
        public int MinTier;

        /// <summary>When set, only this first job's line can wear it (e.g. Archmage Wizard Hat → Mystic line).</summary>
        public JobId? RequiredLine;

        public WeaponType WeaponType;

        /// <summary>Weapon level 1–4 (GDD weapon tiers): refine ATK and refine success rates depend on it.</summary>
        public int WeaponLevel;

        public int Atk;

        /// <summary>Weapon element (attacks) or armor element (defense). Neutral by default.</summary>
        public Element Element;

        public int Def;
        public int Mdef;

        /// <summary>Card sockets (weapons up to 4).</summary>
        public int Sockets;

        public bool Refinable = true;

        /// <summary>Bonuses of the item itself (relics, headgear stats, staff MATK).</summary>
        public EquipEffect Effect;

        /// <summary>Placeholder-avatar color for headgear and wings.</summary>
        public string ViewColorHex;

        // ------------------------------------------------------------ cards
        /// <summary>Which equipment a card compounds into.</summary>
        public EquipKind CardTarget;

        public bool IsUsable => Kind == ItemKind.Consumable;

        public bool IsEquipment => Kind == ItemKind.Equipment;

        public bool IsCard => Kind == ItemKind.Card;

        /// <summary>Equipment never stacks: every piece carries its own refine, cards and etching.</summary>
        public bool IsStackable => Kind != ItemKind.Equipment;

        public bool IsWeapon => (Slots & EquipSlot.Weapon) != 0;

        public bool IsTwoHanded => (Slots & EquipSlot.TwoHanded) == EquipSlot.TwoHanded;

        public int SellPrice => Price / 2;

        public EquipKind EquipKind
        {
            get
            {
                if (!IsEquipment) return EquipKind.None;
                if ((Slots & EquipSlot.Weapon) != 0) return EquipKind.Weapon;
                if ((Slots & EquipSlot.Shield) != 0) return EquipKind.Shield;
                if ((Slots & EquipSlot.Armor) != 0) return EquipKind.Armor;
                if ((Slots & EquipSlot.Garment) != 0) return EquipKind.Garment;
                if ((Slots & EquipSlot.Footgear) != 0) return EquipKind.Footgear;
                if ((Slots & EquipSlot.Head) != 0) return EquipKind.Headgear;
                return (Slots & EquipSlot.Accessory) != 0 ? EquipKind.Accessory : EquipKind.None;
            }
        }

        public WeaponProfile ToWeaponProfile(int refine = 0)
        {
            return new WeaponProfile(Name, WeaponType, Atk, Math.Max(1, WeaponLevel), refine, Element);
        }
    }

    public static class EquipKinds
    {
        public static string Label(EquipKind kind)
        {
            switch (kind)
            {
                case EquipKind.Weapon: return "Weapon";
                case EquipKind.Armor: return "Armor";
                case EquipKind.Shield: return "Shield";
                case EquipKind.Garment: return "Garment";
                case EquipKind.Footgear: return "Footgear";
                case EquipKind.Headgear: return "Headgear";
                case EquipKind.Accessory: return "Accessory";
                default: return "—";
            }
        }

        public static string Label(EquipPosition position)
        {
            switch (position)
            {
                case EquipPosition.HeadUpper: return "Upper Headgear";
                case EquipPosition.HeadMid: return "Mid Headgear";
                case EquipPosition.HeadLower: return "Lower Headgear";
                case EquipPosition.Armor: return "Armor";
                case EquipPosition.Weapon: return "Weapon";
                case EquipPosition.Shield: return "Shield";
                case EquipPosition.Garment: return "Garment";
                case EquipPosition.Footgear: return "Footgear";
                case EquipPosition.Accessory1: return "Accessory 1";
                default: return "Accessory 2";
            }
        }

        public static EquipSlot SlotOf(EquipPosition position)
        {
            switch (position)
            {
                case EquipPosition.HeadUpper: return EquipSlot.HeadUpper;
                case EquipPosition.HeadMid: return EquipSlot.HeadMid;
                case EquipPosition.HeadLower: return EquipSlot.HeadLower;
                case EquipPosition.Armor: return EquipSlot.Armor;
                case EquipPosition.Weapon: return EquipSlot.Weapon;
                case EquipPosition.Shield: return EquipSlot.Shield;
                case EquipPosition.Garment: return EquipSlot.Garment;
                case EquipPosition.Footgear: return EquipSlot.Footgear;
                default: return EquipSlot.Accessory;
            }
        }
    }
}
