using System;
using Runeheir.Characters;
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

        /// <summary>Arrows, thunder-rod shot, shuriken (Phase 8): worn in the ammunition slot as a stack, spent one per shot.</summary>
        Ammo = 5,
    }

    /// <summary>One item (and how many) a box gives, or one weighted entry of a random box.</summary>
    public sealed class ItemGrant
    {
        public string ItemId;
        public int Amount = 1;

        /// <summary>Random boxes: relative weight of this entry.</summary>
        public int Weight = 1;

        public ItemGrant(string itemId, int amount = 1, int weight = 1)
        {
            ItemId = itemId;
            Amount = amount;
            Weight = weight;
        }
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

        /// <summary>Phase 8 pets: a taming item, used on the monster it tames (see Pets.PetCatalog).</summary>
        TamePet = 6,

        /// <summary>Phase 8 pets: a pet's egg; using it hatches the pet.</summary>
        HatchPet = 7,

        /// <summary>Phase 8 pets: an accessory, worn by the pet it's made for.</summary>
        PetArmor = 8,

        /// <summary>Phase 8 mercenaries: a contract scroll; using it hires <see cref="ItemDefinition.MercenaryId"/>.</summary>
        HireMercenary = 9,

        /// <summary>Phase 8 mounts: the fjord horse's reins. Using them mounts or dismounts; they're never used up.</summary>
        Mount = 10,
    }

    /// <summary>What an item is for, beyond its kind (Phase 8: the XileRetro database has ammunition, pet eggs, costumes...).</summary>
    public enum ItemCategory
    {
        Standard = 0,
        Ammo = 1,
        PetEgg = 2,
        PetArmor = 3,
        Costume = 4,
        CashShop = 5,
        Quest = 6,
    }

    /// <summary>A bonus a piece of gear gains from refine level <see cref="MinRefine"/> on.</summary>
    public sealed class RefineBonus
    {
        public int MinRefine;
        public EquipEffect Bonus;
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

        /// <summary>The ammunition slot (Phase 8).</summary>
        Ammo = 1 << 9,

        /// <summary>Phase 8.4 costume slots: worn over the gear, they change how you look and nothing else.</summary>
        CostumeUpper = 1 << 10,
        CostumeMid = 1 << 11,
        CostumeLower = 1 << 12,
        CostumeGarment = 1 << 13,

        Head = HeadUpper | HeadMid | HeadLower,
        CostumeHead = CostumeUpper | CostumeMid | CostumeLower,
        Costume = CostumeHead | CostumeGarment,
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

        /// <summary>Phase 8: arrows, shot and shuriken.</summary>
        Ammo = 10,

        /// <summary>Phase 8.4: the costume positions (looks only).</summary>
        CostumeUpper = 11,
        CostumeMid = 12,
        CostumeLower = 13,
        CostumeGarment = 14,
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
        Ammo = 8,

        /// <summary>Phase 8.4: worn in the costume slots, for looks only.</summary>
        Costume = 9,
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

        /// <summary>Percent of Max SP restored (Ragnarok's percentheal).</summary>
        public float HealSpPercent;

        /// <summary>Statuses it cures (Green Potion: Poison, Silence, Blind).</summary>
        public StatusEffect[] Cures;

        /// <summary>Boxes: every one of these goes into the bag.</summary>
        public ItemGrant[] GivesItems;

        /// <summary>Random boxes: one of these, by weight.</summary>
        public ItemGrant[] RandomGives;

        /// <summary>Zeny pouches: a random amount in this range.</summary>
        public int GivesZenyMin;

        public int GivesZenyMax;

        /// <summary>Scrolls: casts this skill for free at <see cref="CastSkillLevel"/> (support skills on you, attacks at your target).</summary>
        public string CastSkillId;

        public int CastSkillLevel = 1;

        /// <summary>An imported usable whose effect Runeheir can't do yet: using it does nothing and it isn't consumed.</summary>
        public bool NotUsableYet;

        /// <summary>Phase 8: the mercenary a <see cref="ItemSpecialEffect.HireMercenary"/> scroll hires.</summary>
        public string MercenaryId;

        /// <summary>Ammunition: the weapon type it's shot from (Bow, ThunderRod, Huuma).</summary>
        public WeaponType AmmoFor;

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

        // ------------------------------------------------------------ the XileRetro database (Phase 8)
        /// <summary>The item's id in the XileRetro / rAthena database it was adapted from (0 for Runeheir's own items).</summary>
        public int RoId;

        public ItemCategory Category;

        /// <summary>When set, only these jobs can use it (Ragnarok's per-item job lists); null = the usual tier/line rules.</summary>
        public JobId[] AllowedJobs;

        /// <summary>Only reborn characters (and expanded jobs) can wear it (Ragnarok's "Upper" class items).</summary>
        public bool RebornOnly;

        /// <summary>Bonuses added once per refine level (+2 ATK per refine...).</summary>
        public EquipEffect PerRefine;

        /// <summary>
        /// The highest refine level <see cref="PerRefine"/> counts (0 = no limit). Ragnarok's scripts stop at +10, so imported
        /// items keep their +10 bonus from +11 to +20 while Runeheir's own refine ATK/DEF keeps growing.
        /// </summary>
        public int PerRefineLimit;

        /// <summary>Only this gender can wear it (Ragnarok's gendered costumes and dresses); null = anyone.</summary>
        public Gender? RequiredGender;

        /// <summary>Bonuses from a refine level on (at +9: +10% ASPD...).</summary>
        public RefineBonus[] RefineBonuses;

        /// <summary>
        /// Phase 8.4: the look template this piece is drawn with ("cat_ears", "wings", "aura", "wig:3"...); null = picked from
        /// its id. Costumes use it, so a thousand costumes share a few dozen models in their own colors.
        /// </summary>
        public string LookAs;

        /// <summary>Phase 8.4: goes in the costume slots (looks only; no stats, refines or cards).</summary>
        public bool IsCostume => (Slots & EquipSlot.Costume) != 0;

        public bool IsUsable => Kind == ItemKind.Consumable;

        public bool IsEquipment => Kind == ItemKind.Equipment;

        public bool IsAmmo => Kind == ItemKind.Ammo;

        /// <summary>Goes on the paperdoll: equipment, or a stack of ammunition.</summary>
        public bool IsWearable => IsEquipment || IsAmmo;

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
                if (IsAmmo) return EquipKind.Ammo;
                if (!IsEquipment) return EquipKind.None;
                if (IsCostume) return EquipKind.Costume;
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
                case EquipKind.Ammo: return "Ammunition";
                case EquipKind.Costume: return "Costume";
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
                case EquipPosition.Ammo: return "Ammunition";
                case EquipPosition.CostumeUpper: return "Costume Upper";
                case EquipPosition.CostumeMid: return "Costume Mid";
                case EquipPosition.CostumeLower: return "Costume Lower";
                case EquipPosition.CostumeGarment: return "Costume Garment";
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
                case EquipPosition.Ammo: return EquipSlot.Ammo;
                case EquipPosition.CostumeUpper: return EquipSlot.CostumeUpper;
                case EquipPosition.CostumeMid: return EquipSlot.CostumeMid;
                case EquipPosition.CostumeLower: return EquipSlot.CostumeLower;
                case EquipPosition.CostumeGarment: return EquipSlot.CostumeGarment;
                default: return EquipSlot.Accessory;
            }
        }
    }
}
