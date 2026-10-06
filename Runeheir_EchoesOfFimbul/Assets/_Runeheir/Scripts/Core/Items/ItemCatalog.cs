using System;
using System.Collections.Generic;
using Runeheir.Combat;

namespace Runeheir.Items
{
    /// <summary>
    /// Every item: consumables, GDD §7 runestones and crafting runes, loot, the 87 GDD §5 equipment pieces
    /// (ItemCatalog.Weapons.cs / ItemCatalog.Gear.cs) and the 35 GDD §6 Soul Cards (ItemCatalog.Cards.cs).
    /// </summary>
    public static partial class ItemCatalog
    {
        // ---- consumables
        public const string LingonberryTonic = "lingonberry_tonic";
        public const string HoneyMead = "honey_mead";
        public const string AetherSap = "aether_sap";
        public const string RuneUruz = "rune_uruz";
        public const string RuneTiwaz = "rune_tiwaz";
        public const string RuneSowilo = "rune_sowilo";
        public const string RuneThurisaz = "rune_thurisaz";
        public const string RuneIsa = "rune_isa";
        public const string RuneHagalaz = "rune_hagalaz";
        public const string RavenFeather = "raven_feather";
        public const string WindRuneShard = "wind_rune_shard";
        public const string DeadBranch = "dead_branch";
        public const string BloodBranch = "blood_branch";

        // ---- refine ores and catalytic runes (GDD §7)
        public const string BogIron = "bog_iron";
        public const string DwarvenSteel = "dwarven_steel";
        public const string Starmetal = "starmetal";
        public const string Skystone = "skystone";
        public const string RuneOfPreservation = "rune_of_preservation";
        public const string RuneOfExtraction = "rune_of_extraction";

        private static readonly Dictionary<string, ItemDefinition> ById = new Dictionary<string, ItemDefinition>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<ItemDefinition> Ordered = new List<ItemDefinition>();

        static ItemCatalog()
        {
            RegisterConsumables();
            RegisterMaterials();
            RegisterLoot();
            RegisterWeapons();
            RegisterGear();
            RegisterCards();
        }

        public static IReadOnlyList<ItemDefinition> All => Ordered;

        public static ItemDefinition Get(string id)
        {
            return !string.IsNullOrEmpty(id) && ById.TryGetValue(id, out var item) ? item : null;
        }

        private static ItemDefinition Register(ItemDefinition item)
        {
            ById[item.Id] = item;
            Ordered.Add(item);
            return item;
        }

        private static void RegisterConsumables()
        {
            Register(new ItemDefinition
            {
                Id = LingonberryTonic, Name = "Lingonberry Tonic", Kind = ItemKind.Consumable, Weight = 7, Price = 50,
                Description = "Restores 45~65 HP.", IconLabel = "LT", IconColorHex = "#C0392B",
                HealHpMin = 45, HealHpMax = 65,
            });
            Register(new ItemDefinition
            {
                Id = HoneyMead, Name = "Honey Mead", Kind = ItemKind.Consumable, Weight = 15, Price = 450,
                Description = "Restores 325~405 HP.", IconLabel = "HM", IconColorHex = "#F5B041",
                HealHpMin = 325, HealHpMax = 405,
            });
            Register(new ItemDefinition
            {
                Id = AetherSap, Name = "Aether Sap Vial", Kind = ItemKind.Consumable, Weight = 15, Price = 300,
                Description = "Runic sap of Yggdrasil. Restores 40~60 SP.", IconLabel = "AS", IconColorHex = "#2E86C1",
                HealSpMin = 40, HealSpMax = 60,
            });
            Register(new ItemDefinition
            {
                Id = RuneUruz, Name = "Uruz Runestone", Kind = ItemKind.Consumable, Weight = 5, Price = 3000,
                Description = "Restores 30% Max HP and grants +25 STR for 60s.", IconLabel = "URZ", IconColorHex = "#A04000",
                HealHpPercent = 30f, BuffId = BuffCatalog.UruzMight,
            });
            Register(new ItemDefinition
            {
                Id = RuneTiwaz, Name = "Tiwaz Runestone", Kind = ItemKind.Consumable, Weight = 5, Price = 3000,
                Description = "Your next 3 attacks are guaranteed criticals.", IconLabel = "TIW", IconColorHex = "#D4AC0D",
                BuffId = BuffCatalog.TiwazPrecision,
            });
            Register(new ItemDefinition
            {
                Id = RuneSowilo, Name = "Sowilo Runestone", Kind = ItemKind.Consumable, Weight = 5, Price = 3000,
                Description = "Cleanses every status and debuff and grants 10s immunity to statuses and stagger.", IconLabel = "SOW", IconColorHex = "#F7DC6F",
                BuffId = BuffCatalog.SowiloWard, Special = ItemSpecialEffect.Cleanse,
            });
            Register(new ItemDefinition
            {
                Id = RuneThurisaz, Name = "Thurisaz Runestone", Kind = ItemKind.Consumable, Weight = 5, Price = 3000,
                Description = "Triples the poise damage you deal for 30s: heavy blows stagger far sooner.", IconLabel = "THU", IconColorHex = "#CB4335",
                BuffId = BuffCatalog.ThurisazFury,
            });
            Register(new ItemDefinition
            {
                Id = RuneIsa, Name = "Isa Runestone", Kind = ItemKind.Consumable, Weight = 5, Price = 4000,
                Description = "A 5,000 HP glacial shield for 30s. Melee attackers that strike it are frozen.", IconLabel = "ISA", IconColorHex = "#85C1E9",
                BuffId = BuffCatalog.IsaShield,
            });
            Register(new ItemDefinition
            {
                Id = RuneHagalaz, Name = "Hagalaz Runestone", Kind = ItemKind.Consumable, Weight = 5, Price = 3000,
                Description = "For 30s, 25% of magic damage you take rebounds on the caster.", IconLabel = "HAG", IconColorHex = "#AF7AC5",
                BuffId = BuffCatalog.HagalazRebound,
            });
            Register(new ItemDefinition
            {
                Id = RavenFeather, Name = "Raven Feather", Kind = ItemKind.Consumable, Weight = 5, Price = 300,
                Description = "A feather of Huginn. Returns you to your save point.", IconLabel = "RF", IconColorHex = "#34495E",
                Special = ItemSpecialEffect.ReturnToSavePoint,
            });
            Register(new ItemDefinition
            {
                Id = WindRuneShard, Name = "Wind Rune Shard", Kind = ItemKind.Consumable, Weight = 5, Price = 60,
                Description = "Teleports you to a random spot on this map.", IconLabel = "WR", IconColorHex = "#76D7C4",
                Special = ItemSpecialEffect.RandomTeleport,
            });
            Register(new ItemDefinition
            {
                Id = DeadBranch, Name = "Dead Branch", Kind = ItemKind.Consumable, Weight = 5, Price = 5000,
                Description = "A dead twig of Yggdrasil. Breaking it summons a random monster. (Best cracked in the Hall of Branches.)", IconLabel = "DB", IconColorHex = "#6E2C00",
                Special = ItemSpecialEffect.SummonMonster,
            });
            Register(new ItemDefinition
            {
                Id = BloodBranch, Name = "Blood Branch", Kind = ItemKind.Consumable, Weight = 10, Price = 100000,
                Description = "A branch weeping red sap. Breaking it summons a powerful boss.", IconLabel = "BB", IconColorHex = "#922B21",
                Special = ItemSpecialEffect.SummonBoss,
            });
        }

        private static void RegisterMaterials()
        {
            Material(BogIron, "Bog Iron", "Refines Lv 1 weapons at the Dwarven Forge.", "FE", "#7B7D7D", 50);
            Material(DwarvenSteel, "Dwarven Steel", "Refines Lv 2 weapons at the Dwarven Forge.", "DS", "#95A5A6", 200);
            Material(Starmetal, "Starmetal", "Sky-fallen metal. Refines Lv 3 and Lv 4 weapons.", "SM", "#5DADE2", 5000);
            Material(Skystone, "Skystone", "Refines armor, shields, garments, footgear and headgear.", "SK", "#AED6F1", 5000);
            Material(RuneOfPreservation, "Rune of Preservation", "Use it while refining: a failed refine lowers the item by 1 instead of shattering it.", "RoP", "#F5B041", 50000);
            Material(RuneOfExtraction, "Rune of Extraction", "Safely pulls every Soul Card out of a piece of equipment, keeping both.", "RoE", "#A569BD", 150000);
            foreach (var glyph in RunewordRules.GlyphNames)
            {
                Material(glyph.Key, $"Glyph of {glyph.Value}", $"An Elder Futhark glyph ({glyph.Value}) for carving into a weapon's fuller. Two glyphs form a Runeword.",
                    "g" + glyph.Value.Substring(0, 2).ToUpperInvariant(), "#D4AC0D", 8000);
            }
        }

        private static void RegisterLoot()
        {
            Loot("spore_cap", "Spore Cap", "A spongy rune-spore cap.", 12);
            Loot("beetle_shell", "Beetle Shell", "A hard green shell.", 30);
            Loot("toxic_gland", "Toxic Gland", "Handle with gloves.", 60);
            Loot("imp_horn", "Imp Horn", "A small twisted horn.", 120);
            Loot("grazer_hide", "Grazer Hide", "Thick, warm hide.", 180);
            Loot("sprite_leaf", "Sprite Leaf", "It still hums with wind.", 300);
            Loot("wolf_pelt", "Wolf Pelt", "Coarse grey fur.", 1200);
            Loot("frost_fang", "Frost Fang", "A fang that never warms.", 4000);
            Loot("jotun_tooth", "Jotun Tooth", "Bigger than your fist.", 15000);
        }

        private static void Material(string id, string name, string description, string icon, string color, int price)
        {
            Register(new ItemDefinition
            {
                Id = id, Name = name, Description = description, Kind = ItemKind.Material, Weight = 1,
                IconLabel = icon, IconColorHex = color, Price = price,
            });
        }

        private static void Loot(string id, string name, string description, int price)
        {
            Register(new ItemDefinition
            {
                Id = id, Name = name, Description = description + " Sells to merchants.", Kind = ItemKind.Etc, Weight = 1,
                IconLabel = name.Substring(0, 2).ToUpperInvariant(), IconColorHex = "#A9927D", Price = price,
            });
        }
    }
}
