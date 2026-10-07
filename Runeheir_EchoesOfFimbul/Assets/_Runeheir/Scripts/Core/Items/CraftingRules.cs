using System;
using System.Collections.Generic;
using Runeheir.Combat;
using Runeheir.Skills;
using Runeheir.Stats;

namespace Runeheir.Items
{
    /// <summary>One material a recipe uses up.</summary>
    public readonly struct CraftMaterial
    {
        public CraftMaterial(string itemId, int amount)
        {
            ItemId = itemId;
            Amount = amount;
        }

        public string ItemId { get; }

        public int Amount { get; }

        public ItemDefinition Definition => ItemCatalog.Get(ItemId);
    }

    /// <summary>Something a Runesmith can forge or a Brewmaster can brew.</summary>
    public sealed class CraftRecipe
    {
        public CraftKind Kind;
        public string ProductId;
        public int ProductAmount = 1;

        /// <summary>Rune Forging / Brewing level needed (for forging, the weapon level).</summary>
        public int SkillLevel = 1;

        /// <summary>Percentage points off the success chance.</summary>
        public int Difficulty;

        public IReadOnlyList<CraftMaterial> Materials = Array.Empty<CraftMaterial>();

        public ItemDefinition Product => ItemCatalog.Get(ProductId);
    }

    public enum CraftOutcome
    {
        /// <summary>Nothing happened: missing skill, materials or bag room.</summary>
        NotAllowed = 0,
        Success = 1,

        /// <summary>The attempt failed and the materials are gone (Ragnarok).</summary>
        Failure = 2,
    }

    /// <summary>
    /// Phase 7 crafting, Ragnarok's Smithing and Pharmacy. Rune Forging (Runesmith, Forgelord) makes the bladed and blunt
    /// weapons of weapon level 1 to 3 (one Rune Forging level per weapon level) from bog iron, dwarven steel and starmetal plus a
    /// little of the right loot. Brewing (Brewmaster, Lifeweaver) makes tonics, sap, mead, feathers and runestones from herbs
    /// and monster parts. A failed attempt uses up the materials. Engine-free so the rules are tested in Core.
    /// </summary>
    public static class CraftingRules
    {
        public const string ForgeSkill = "rune_forging";
        public const string ForgeResearch = "weaponry_research";
        public const string BrewSkill = "brewing";
        public const string BrewResearch = "potion_research";

        public const float MinChance = 5f;
        public const float MaxChance = 95f;

        /// <summary>The weapon types a forge can make (Ragnarok smiths never made bows, staves, books or instruments).</summary>
        private static readonly Dictionary<WeaponType, string> ForgeLoot = new Dictionary<WeaponType, string>
        {
            { WeaponType.Dagger, "wolf_pelt" },
            { WeaponType.TwoHandSword, "grazer_hide" },
            { WeaponType.Spear, "grazer_hide" },
            { WeaponType.Mace, "beetle_shell" },
            { WeaponType.Knuckle, "wolf_pelt" },
            { WeaponType.Katar, "crawler_carapace" },
            { WeaponType.Axe, "boar_tusk" },
            { WeaponType.TwoHandAxe, "boar_tusk" },
        };

        private static readonly List<CraftRecipe> Forge = new List<CraftRecipe>();
        private static readonly List<CraftRecipe> Brew = new List<CraftRecipe>();

        static CraftingRules()
        {
            foreach (var item in ItemCatalog.All)
            {
                if (item.IsWeapon && item.WeaponLevel >= 1 && item.WeaponLevel <= 3 && item.Price >= 500 && ForgeLoot.TryGetValue(item.WeaponType, out string loot))
                {
                    Forge.Add(ForgeRecipe(item, loot));
                }
            }

            Forge.Sort((a, b) => a.SkillLevel != b.SkillLevel ? a.SkillLevel.CompareTo(b.SkillLevel)
                : string.Compare(a.Product.Name, b.Product.Name, StringComparison.Ordinal));

            BrewRecipe(ItemCatalog.LingonberryTonic, 3, 1, 0, ("spore_cap", 2));
            BrewRecipe(ItemCatalog.WindRuneShard, 2, 2, 5, ("sprite_leaf", 1));
            BrewRecipe(ItemCatalog.AetherSap, 1, 3, 10, ("sprite_leaf", 1), ("spore_cap", 1));
            BrewRecipe(ItemCatalog.RavenFeather, 1, 4, 10, ("harpy_feather", 1), ("bat_wing", 1));
            BrewRecipe(ItemCatalog.HoneyMead, 1, 5, 15, ("imp_horn", 1), ("spore_cap", 2));
            BrewRecipe(ItemCatalog.RuneSowilo, 1, 7, 30, ("harpy_feather", 1), ("sprite_leaf", 2));
            BrewRecipe(ItemCatalog.RuneUruz, 1, 9, 35, ("boar_tusk", 2), ("imp_horn", 1));
            BrewRecipe(ItemCatalog.RuneIsa, 1, 10, 40, ("ice_core", 1), ("sprite_leaf", 1));
        }

        public static IReadOnlyList<CraftRecipe> For(CraftKind kind)
        {
            switch (kind)
            {
                case CraftKind.Forge:
                    return Forge;
                case CraftKind.Brew:
                    return Brew;
                default:
                    return Array.Empty<CraftRecipe>();
            }
        }

        public static CraftRecipe Get(CraftKind kind, string productId)
        {
            foreach (var recipe in For(kind))
            {
                if (string.Equals(recipe.ProductId, productId, StringComparison.OrdinalIgnoreCase))
                {
                    return recipe;
                }
            }

            return null;
        }

        /// <summary>The crafting skill for <paramref name="kind"/> (Rune Forging or Brewing).</summary>
        public static string SkillFor(CraftKind kind)
        {
            return kind == CraftKind.Forge ? ForgeSkill : kind == CraftKind.Brew ? BrewSkill : null;
        }

        /// <summary>The passive that improves <paramref name="kind"/> (Weaponry Research or Potion Research).</summary>
        public static string ResearchFor(CraftKind kind)
        {
            return kind == CraftKind.Forge ? ForgeResearch : kind == CraftKind.Brew ? BrewResearch : null;
        }

        /// <summary>
        /// Chance in percent (5 to 95). Forging: 45 + DEX/5 + LUK/10 + Job Lv/5 + 2 per Weaponry Research level, +10 per Rune
        /// Forging level above the weapon's, -15 per weapon level above 1. Brewing: 30 + 3 per Brewing level + 2 per Potion
        /// Research level + Job Lv × 0.3 + (INT + DEX + LUK)/10, minus the recipe's difficulty.
        /// </summary>
        public static float SuccessChance(CraftRecipe recipe, int skillLevel, int researchLevel, int jobLevel, BaseStats stats)
        {
            if (recipe == null || stats == null)
            {
                return 0f;
            }

            float chance;
            if (recipe.Kind == CraftKind.Forge)
            {
                chance = 45f + stats.Dex * 0.2f + stats.Luk * 0.1f + jobLevel * 0.2f + researchLevel * 2f
                         + (skillLevel - recipe.SkillLevel) * 10f - recipe.Difficulty;
            }
            else
            {
                chance = 30f + skillLevel * 3f + researchLevel * 2f + jobLevel * 0.3f + (stats.Int + stats.Dex + stats.Luk) * 0.1f
                         - recipe.Difficulty;
            }

            return Math.Max(MinChance, Math.Min(MaxChance, chance));
        }

        /// <summary>Whether <paramref name="recipe"/> can be tried now; the reason says what's missing.</summary>
        public static bool CanCraft(CraftRecipe recipe, int skillLevel, Inventory inventory, out string reason)
        {
            reason = null;
            if (recipe == null || recipe.Product == null || inventory == null)
            {
                reason = "Nothing to make.";
                return false;
            }

            if (skillLevel < recipe.SkillLevel)
            {
                reason = recipe.Kind == CraftKind.Forge
                    ? $"Forging a level {recipe.SkillLevel} weapon needs Rune Forging Lv {recipe.SkillLevel}."
                    : $"{recipe.Product.Name} needs Brewing Lv {recipe.SkillLevel}.";
                return false;
            }

            foreach (var material in recipe.Materials)
            {
                int have = inventory.Count(material.ItemId);
                if (have < material.Amount)
                {
                    reason = $"Not enough {material.Definition?.Name ?? material.ItemId} ({have}/{material.Amount}).";
                    return false;
                }
            }

            if (!HasRoom(inventory, recipe))
            {
                reason = "Your bag is full.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// One attempt: on success the product goes in the bag; either way the materials are used up. The caller pays the
        /// skill's SP first.
        /// </summary>
        public static CraftOutcome TryCraft(CraftRecipe recipe, int skillLevel, int researchLevel, int jobLevel, BaseStats stats,
            Inventory inventory, IRandomSource random, out string message)
        {
            if (!CanCraft(recipe, skillLevel, inventory, out message))
            {
                return CraftOutcome.NotAllowed;
            }

            float chance = SuccessChance(recipe, skillLevel, researchLevel, jobLevel, stats);
            foreach (var material in recipe.Materials)
            {
                inventory.TryRemove(material.ItemId, material.Amount);
            }

            var product = recipe.Product;
            if (random.NextDouble() * 100.0 >= chance)
            {
                message = recipe.Kind == CraftKind.Forge
                    ? $"The {product.Name} cracked in the quench. The materials are lost."
                    : $"The {product.Name} curdled in the pot. The materials are lost.";
                return CraftOutcome.Failure;
            }

            int added = inventory.Add(recipe.ProductId, recipe.ProductAmount);
            string amount = recipe.ProductAmount > 1 ? $"{added} × " : string.Empty;
            message = recipe.Kind == CraftKind.Forge ? $"You forged a {product.Name}!" : $"You brewed {amount}{product.Name}!";
            return CraftOutcome.Success;
        }

        private static bool HasRoom(Inventory inventory, CraftRecipe recipe)
        {
            var product = recipe.Product;
            if (product.IsStackable && inventory.FindFirst(recipe.ProductId) != null)
            {
                return inventory.Count(recipe.ProductId) + recipe.ProductAmount <= Inventory.MaxStack;
            }

            // Materials used up to the last one free a slot, so count those too.
            int freed = 0;
            foreach (var material in recipe.Materials)
            {
                if (material.Definition != null && material.Definition.IsStackable && inventory.Count(material.ItemId) == material.Amount)
                {
                    freed++;
                }
            }

            int slots = product.IsStackable ? 1 : recipe.ProductAmount;
            return inventory.Stacks.Count - freed + slots <= Inventory.MaxEntries;
        }

        private static CraftRecipe ForgeRecipe(ItemDefinition weapon, string loot)
        {
            var materials = new List<CraftMaterial>();
            switch (weapon.WeaponLevel)
            {
                case 1:
                    materials.Add(new CraftMaterial(ItemCatalog.BogIron, 4));
                    break;
                case 2:
                    materials.Add(new CraftMaterial(ItemCatalog.DwarvenSteel, 2));
                    materials.Add(new CraftMaterial(ItemCatalog.BogIron, 4));
                    break;
                default:
                    materials.Add(new CraftMaterial(ItemCatalog.Starmetal, 1));
                    materials.Add(new CraftMaterial(ItemCatalog.DwarvenSteel, 4));
                    break;
            }

            materials.Add(new CraftMaterial(loot, weapon.WeaponLevel));
            return new CraftRecipe
            {
                Kind = CraftKind.Forge,
                ProductId = weapon.Id,
                SkillLevel = weapon.WeaponLevel,
                Difficulty = (weapon.WeaponLevel - 1) * 15,
                Materials = materials,
            };
        }

        private static void BrewRecipe(string productId, int amount, int brewingLevel, int difficulty, params (string id, int amount)[] materials)
        {
            var list = new List<CraftMaterial>();
            foreach (var (id, count) in materials)
            {
                list.Add(new CraftMaterial(id, count));
            }

            Brew.Add(new CraftRecipe
            {
                Kind = CraftKind.Brew,
                ProductId = productId,
                ProductAmount = amount,
                SkillLevel = brewingLevel,
                Difficulty = difficulty,
                Materials = list,
            });
        }
    }
}
