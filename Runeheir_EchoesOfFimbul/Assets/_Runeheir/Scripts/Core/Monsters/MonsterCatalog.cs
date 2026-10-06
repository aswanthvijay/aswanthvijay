using System;
using System.Collections.Generic;
using Runeheir.Combat;
using Runeheir.Items;

namespace Runeheir.Monsters
{
    public enum MonsterShape
    {
        Blob = 0,
        Biped = 1,
        Quadruped = 2,
        Flyer = 3,
        Dummy = 4,
    }

    public sealed class DropEntry
    {
        public string ItemId;

        /// <summary>Base chance in percent before the server drop rate.</summary>
        public float ChancePercent;

        public DropEntry(string itemId, float chancePercent)
        {
            ItemId = itemId;
            ChancePercent = chancePercent;
        }
    }

    public sealed class MonsterDefinition
    {
        public string Id;
        public string Name;
        public int Level;
        public int MaxHp;
        public int AtkMin;
        public int AtkMax;
        public int Def;
        public int Mdef;
        public int Hit;
        public int Flee;
        public Element Element;
        public Race Race;
        public Size Size;
        public long BaseExp;
        public long JobExp;

        /// <summary>Attacks players on sight (otherwise only retaliates).</summary>
        public bool Aggressive;

        /// <summary>Never moves (training dummy).</summary>
        public bool Stationary;

        /// <summary>Never attacks.</summary>
        public bool Passive;

        /// <summary>Refills HP instead of dying (training dummy).</summary>
        public bool Immortal;

        public float MoveSpeed = 3f;
        public float AttackRange = 0.9f;

        /// <summary>Seconds between attacks.</summary>
        public float AttackInterval = 1.4f;

        public float AggroRange = 8f;

        /// <summary>Gives up and walks home past this distance from its spawn.</summary>
        public float LeashRange = 25f;

        public float RespawnSeconds = 8f;
        public List<DropEntry> Drops = new List<DropEntry>();

        public MonsterShape Shape;
        public string ColorHex = "#999999";
        public float Scale = 1f;
    }

    /// <summary>Phase 2 bestiary: GDD Field 1 (Whisperwood Plains) plus a few higher-level test targets.</summary>
    public static class MonsterCatalog
    {
        private static readonly Dictionary<string, MonsterDefinition> ById = new Dictionary<string, MonsterDefinition>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<MonsterDefinition> Ordered = new List<MonsterDefinition>();

        static MonsterCatalog()
        {
            Register(new MonsterDefinition
            {
                Id = "training_dummy", Name = "Training Dummy", Level = 1, MaxHp = 1000000,
                Element = Element.Neutral, Race = Race.Formless, Size = Size.Medium,
                Stationary = true, Passive = true, Immortal = true, RespawnSeconds = 0f,
                Shape = MonsterShape.Dummy, ColorHex = "#A67C52", Scale = 1.1f,
            });
            Register(new MonsterDefinition
            {
                Id = "rune_spore", Name = "Rune Spore", Level = 3, MaxHp = 70, AtkMin = 8, AtkMax = 12,
                Hit = 15, Flee = 5, Element = Element.Earth, Race = Race.Plant, Size = Size.Small,
                BaseExp = 15, JobExp = 12, MoveSpeed = 2f, AttackInterval = 1.6f,
                Drops = { Drop(ItemCatalog.LingonberryTonic, 40f), Drop("spore_cap", 55f), Drop("sandals", 1f), Drop("flower_crown", 0.3f), Drop("rune_spore_card", 1f) },
                Shape = MonsterShape.Blob, ColorHex = "#C9A66B", Scale = 0.7f,
            });
            Register(new MonsterDefinition
            {
                Id = "field_beetle", Name = "Field Beetle", Level = 6, MaxHp = 150, AtkMin = 14, AtkMax = 20, Def = 10,
                Hit = 20, Flee = 8, Element = Element.Earth, Race = Race.Insect, Size = Size.Small,
                BaseExp = 35, JobExp = 28, MoveSpeed = 2.2f,
                Drops = { Drop(ItemCatalog.LingonberryTonic, 30f), Drop(ItemCatalog.RavenFeather, 5f), Drop("beetle_shell", 55f), Drop(ItemCatalog.BogIron, 8f), Drop("cotton_tunic", 1.5f), Drop("field_beetle_card", 1f) },
                Shape = MonsterShape.Quadruped, ColorHex = "#4E5D3A", Scale = 0.6f,
            });
            Register(new MonsterDefinition
            {
                Id = "toxic_spore", Name = "Toxic Spore", Level = 10, MaxHp = 260, AtkMin = 22, AtkMax = 30, Def = 2,
                Hit = 28, Flee = 14, Element = Element.Poison, Race = Race.Plant, Size = Size.Small,
                BaseExp = 60, JobExp = 45, MoveSpeed = 2f,
                Drops = { Drop(ItemCatalog.AetherSap, 10f), Drop("toxic_gland", 50f), Drop("spore_cap", 20f), Drop("bandana", 1f), Drop("toxic_spore_card", 1f) },
                Shape = MonsterShape.Blob, ColorHex = "#7D3C98", Scale = 0.75f,
            });
            Register(new MonsterDefinition
            {
                Id = "forest_imp", Name = "Forest Imp", Level = 14, MaxHp = 380, AtkMin = 35, AtkMax = 48, Def = 4,
                Hit = 40, Flee = 30, Element = Element.Shadow, Race = Race.Demon, Size = Size.Small,
                BaseExp = 110, JobExp = 90, Aggressive = true, MoveSpeed = 3.6f, AttackInterval = 1.1f,
                Drops = { Drop(ItemCatalog.WindRuneShard, 15f), Drop(ItemCatalog.RuneTiwaz, 2f), Drop("imp_horn", 45f), Drop(ItemCatalog.DeadBranch, 3f), Drop("clip_ring", 0.5f), Drop("forest_imp_card", 0.75f) },
                Shape = MonsterShape.Biped, ColorHex = "#6E2C00", Scale = 0.75f,
            });
            Register(new MonsterDefinition
            {
                Id = "horned_grazer", Name = "Horned Grazer", Level = 18, MaxHp = 620, AtkMin = 50, AtkMax = 66, Def = 8,
                Hit = 45, Flee = 22, Element = Element.Earth, Race = Race.Beast, Size = Size.Medium,
                BaseExp = 160, JobExp = 130, MoveSpeed = 2.6f, AttackInterval = 1.5f,
                Drops = { Drop(ItemCatalog.HoneyMead, 8f), Drop("grazer_hide", 50f), Drop(ItemCatalog.BogIron, 10f), Drop("leather_jerkin", 1f), Drop("fur_cap", 1f), Drop("horned_grazer_card", 0.75f) },
                Shape = MonsterShape.Quadruped, ColorHex = "#8D6E63", Scale = 1.1f,
            });
            Register(new MonsterDefinition
            {
                Id = "wood_sprite", Name = "Wood Sprite", Level = 24, MaxHp = 900, AtkMin = 70, AtkMax = 95, Def = 6, Mdef = 20,
                Hit = 60, Flee = 40, Element = Element.Wind, Race = Race.Plant, Size = Size.Small,
                BaseExp = 260, JobExp = 210, Aggressive = true, MoveSpeed = 3.4f,
                Drops = { Drop(ItemCatalog.RuneUruz, 3f), Drop(ItemCatalog.AetherSap, 12f), Drop("sprite_leaf", 45f), Drop(ItemCatalog.DwarvenSteel, 4f), Drop("runed_oak_wand", 1f), Drop("wood_sprite_card", 0.5f) },
                Shape = MonsterShape.Flyer, ColorHex = "#82E0AA", Scale = 0.7f,
            });

            // Higher-level targets for testing with @monster (Fields 2–4 arrive in Phase 5).
            Register(new MonsterDefinition
            {
                Id = "dire_wolf", Name = "Dire Wolf", Level = 70, MaxHp = 9000, AtkMin = 380, AtkMax = 460, Def = 35, Mdef = 15,
                Hit = 150, Flee = 120, Element = Element.Earth, Race = Race.Beast, Size = Size.Medium,
                BaseExp = 4200, JobExp = 3300, Aggressive = true, MoveSpeed = 4.2f, AttackInterval = 1.1f,
                Drops = { Drop(ItemCatalog.RuneTiwaz, 5f), Drop(ItemCatalog.HoneyMead, 20f), Drop("wolf_pelt", 50f), Drop(ItemCatalog.DwarvenSteel, 8f), Drop(ItemCatalog.Skystone, 2f), Drop("wolf_hood", 0.8f), Drop("wolf_claws", 0.5f), Drop("dire_wolf_card", 0.5f) },
                Shape = MonsterShape.Quadruped, ColorHex = "#5D6D7E", Scale = 1.2f,
            });
            Register(new MonsterDefinition
            {
                Id = "frost_wolf", Name = "Frost Wolf", Level = 140, MaxHp = 60000, AtkMin = 1300, AtkMax = 1600, Def = 80, Mdef = 40,
                Hit = 280, Flee = 230, Element = Element.Water, Race = Race.Beast, Size = Size.Medium,
                BaseExp = 18000, JobExp = 14000, Aggressive = true, MoveSpeed = 4.4f, AttackInterval = 1f,
                Drops = { Drop(ItemCatalog.RuneSowilo, 5f), Drop(ItemCatalog.HoneyMead, 30f), Drop("frost_fang", 40f), Drop(ItemCatalog.Starmetal, 3f), Drop(ItemCatalog.Skystone, 4f), Drop("frost_stiletto", 0.4f), Drop(RunewordRules.Isa, 1f), Drop("frost_wolf_card", 0.5f) },
                Shape = MonsterShape.Quadruped, ColorHex = "#D6EAF8", Scale = 1.3f,
            });
            Register(new MonsterDefinition
            {
                Id = "jotun_brawler", Name = "Jotun Brawler", Level = 230, MaxHp = 400000, AtkMin = 4200, AtkMax = 5200, Def = 160, Mdef = 60,
                Hit = 460, Flee = 300, Element = Element.Water, Race = Race.DemiHuman, Size = Size.Large,
                BaseExp = 60000, JobExp = 50000, Aggressive = true, MoveSpeed = 3.2f, AttackRange = 1.6f, AttackInterval = 1.6f,
                Drops = { Drop(ItemCatalog.RuneUruz, 10f), Drop(ItemCatalog.HoneyMead, 40f), Drop("jotun_tooth", 40f), Drop(ItemCatalog.Starmetal, 6f), Drop(ItemCatalog.Skystone, 6f), Drop(RunewordRules.Thurisaz, 2f), Drop("plated_greaves", 0.5f), Drop(ItemCatalog.BloodBranch, 0.5f), Drop("jotun_brawler_card", 0.5f) },
                Shape = MonsterShape.Biped, ColorHex = "#85C1E9", Scale = 2.2f,
            });
        }

        public static IReadOnlyList<MonsterDefinition> All => Ordered;

        /// <summary>Blood Branch pulls from this level up (the strongest tier until Phase 5 adds the MVPs).</summary>
        public const int BloodBranchMinLevel = 70;

        /// <summary>Dead Branch: a random field monster below <see cref="BloodBranchMinLevel"/>. Blood Branch: one at or above it.</summary>
        public static MonsterDefinition PickForBranch(bool boss, IRandomSource random)
        {
            var pool = new List<MonsterDefinition>();
            foreach (var monster in Ordered)
            {
                if (!monster.Immortal && !monster.Stationary && (monster.Level >= BloodBranchMinLevel) == boss)
                {
                    pool.Add(monster);
                }
            }

            return pool.Count == 0 ? null : pool[Math.Min(pool.Count - 1, (int)(random.NextDouble() * pool.Count))];
        }

        private static DropEntry Drop(string itemId, float chancePercent)
        {
            return new DropEntry(itemId, chancePercent);
        }

        public static MonsterDefinition Get(string id)
        {
            return id != null && ById.TryGetValue(id, out var monster) ? monster : null;
        }

        private static void Register(MonsterDefinition monster)
        {
            ById[monster.Id] = monster;
            Ordered.Add(monster);
        }
    }
}
