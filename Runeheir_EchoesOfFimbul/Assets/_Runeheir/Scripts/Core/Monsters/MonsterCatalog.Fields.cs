using Runeheir.Combat;
using Runeheir.Items;

namespace Runeheir.Monsters
{
    /// <summary>The four leveling fields: Whisperwood Plains, Whispering Woods, Howling Fjord and Jotun Steppe.</summary>
    public static partial class MonsterCatalog
    {
        // ================================================================ Whisperwood Plains (Lv 1–60)
        private static void RegisterPlains()
        {
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
                Id = "field_beetle", Name = "Field Beetle", Level = 7, MaxHp = 175, AtkMin = 18, AtkMax = 22, Def = 12, Mdef = 2,
                Hit = 23, Flee = 10, Element = Element.Earth, Race = Race.Insect, Size = Size.Small,
                BaseExp = 40, JobExp = 32, MoveSpeed = 2.2f,
                Skills = { SelfBuff("harden", "Harden", MonsterBuffs.Harden, 50f, 20f, 30f) },
                Drops =
                {
                    Drop(ItemCatalog.LingonberryTonic, 30f), Drop(ItemCatalog.RavenFeather, 5f), Drop("beetle_shell", 55f), Drop(ItemCatalog.BogIron, 8f),
                    Drop("cotton_tunic", 1.5f), Drop("slotted_dark_sunglasses", 0.1f), Drop("field_beetle_card", 1f),
                },
                Shape = MonsterShape.Quadruped, ColorHex = "#4E5D3A", Scale = 0.6f,
            });
            Register(new MonsterDefinition
            {
                Id = "toxic_spore", Name = "Toxic Spore", Level = 13, MaxHp = 375, AtkMin = 33, AtkMax = 40, Def = 3,
                Hit = 35, Flee = 19, Element = Element.Poison, Race = Race.Plant, Size = Size.Small,
                BaseExp = 95, JobExp = 75, MoveSpeed = 2f,
                Skills =
                {
                    new MonsterSkill
                    {
                        Id = "spore_burst", Name = "Spore Burst", Kind = MonsterSkillKind.Area, CenteredOnSelf = true, Radius = 2.5f, CastTime = 1f,
                        Percent = 80f, Element = Element.Poison, Status = StatusEffect.Poison, StatusChance = 30f, StatusSeconds = 6f,
                        Cooldown = 10f, ChancePercent = 25f,
                    },
                },
                Drops =
                {
                    Drop(ItemCatalog.AetherSap, 10f), Drop("toxic_gland", 50f), Drop("spore_cap", 20f), Drop("bandana", 1f), Drop("rune_monocle", 0.2f),
                    Drop("toxic_spore_card", 1f),
                },
                Shape = MonsterShape.Blob, ColorHex = "#7D3C98", Scale = 0.75f,
            });
            Register(new MonsterDefinition
            {
                Id = "forest_imp", Name = "Forest Imp", Level = 22, MaxHp = 800, AtkMin = 65, AtkMax = 80, Def = 5, Mdef = 5,
                Hit = 55, Flee = 47, Element = Element.Shadow, Race = Race.Demon, Size = Size.Small,
                BaseExp = 225, JobExp = 180, Aggressive = true, MoveSpeed = 3.6f, AttackInterval = 1.1f,
                Skills =
                {
                    new MonsterSkill
                    {
                        Id = "blink", Name = "Blink", Kind = MonsterSkillKind.Teleport, Range = 3f, BelowHpPercent = 30f, Cooldown = 20f, ChancePercent = 50f,
                        Shout = "Hee hee!",
                    },
                    new MonsterSkill
                    {
                        Id = "imp_bolt", Name = "Imp Bolt", Kind = MonsterSkillKind.Bolt, Range = 7f, MinRange = 1.5f, CastTime = 0.8f, Magical = true,
                        Element = Element.Shadow, Percent = 120f, Cooldown = 8f, ChancePercent = 35f,
                    },
                },
                Drops =
                {
                    Drop(ItemCatalog.WindRuneShard, 15f), Drop(ItemCatalog.RuneTiwaz, 2f), Drop("imp_horn", 45f), Drop(ItemCatalog.DeadBranch, 3f), Drop("clip_ring", 0.5f),
                    Drop("fang_mask", 0.3f), Drop("raven_hood", 0.3f), Drop("forest_imp_card", 0.75f),
                },
                Shape = MonsterShape.Biped, ColorHex = "#6E2C00", Scale = 0.75f,
            });
            Register(new MonsterDefinition
            {
                Id = "horned_grazer", Name = "Horned Grazer", Level = 34, MaxHp = 2140, AtkMin = 120, AtkMax = 145, Def = 10, Mdef = 4,
                Hit = 80, Flee = 55, Element = Element.Earth, Race = Race.Beast, Size = Size.Medium,
                BaseExp = 600, JobExp = 480, MoveSpeed = 2.6f, AttackInterval = 1.5f,
                Skills =
                {
                    new MonsterSkill
                    {
                        Id = "horn_charge", Name = "Horn Charge", Kind = MonsterSkillKind.Leap, MinRange = 3f, Range = 9f, Radius = 1.5f, CastTime = 0.6f,
                        Percent = 150f, Knockback = 2f, Status = StatusEffect.Stun, StatusChance = 20f, StatusSeconds = 2f, Cooldown = 12f, ChancePercent = 40f,
                    },
                },
                Drops =
                {
                    Drop(ItemCatalog.HoneyMead, 8f), Drop("grazer_hide", 50f), Drop(ItemCatalog.BogIron, 10f), Drop("leather_jerkin", 1f), Drop("fur_cap", 1f),
                    Drop("antler_crown", 0.2f), Drop("fur_boots", 0.3f), Drop("horned_grazer_card", 0.75f),
                },
                Shape = MonsterShape.Quadruped, ColorHex = "#8D6E63", Scale = 1.1f,
            });
            Register(new MonsterDefinition
            {
                Id = "wood_sprite", Name = "Wood Sprite", Level = 48, MaxHp = 3510, AtkMin = 195, AtkMax = 240, Def = 10, Mdef = 20,
                Hit = 100, Flee = 75, Element = Element.Wind, Race = Race.Plant, Size = Size.Small,
                BaseExp = 1360, JobExp = 1090, Aggressive = true, MoveSpeed = 3.4f,
                Skills =
                {
                    new MonsterSkill
                    {
                        Id = "sap_mending", Name = "Sap Mending", Kind = MonsterSkillKind.Heal, HealPercent = 15f, BelowHpPercent = 50f, CastTime = 1f,
                        Cooldown = 20f, ChancePercent = 40f,
                    },
                    new MonsterSkill
                    {
                        Id = "gale_leaf", Name = "Gale Leaf", Kind = MonsterSkillKind.Bolt, Range = 7f, CastTime = 1f, Magical = true, Element = Element.Wind,
                        Percent = 130f, Cooldown = 6f, ChancePercent = 35f,
                    },
                },
                Drops =
                {
                    Drop(ItemCatalog.RuneUruz, 3f), Drop(ItemCatalog.AetherSap, 12f), Drop("sprite_leaf", 45f), Drop(ItemCatalog.DwarvenSteel, 4f),
                    Drop("runed_oak_wand", 1f), Drop("rune_tome_staff", 0.3f), Drop("rune_circlet", 0.2f), Drop("rune_muffler", 0.3f),
                    Drop("archmage_wizard_hat", 0.02f), Drop("wood_sprite_card", 0.5f),
                },
                Shape = MonsterShape.Flyer, ColorHex = "#82E0AA", Scale = 0.7f,
            });
        }

        // ================================================================ Whispering Woods (Lv 61–120) and Howling Fjord (Lv 121–180)
        private static void RegisterWoodsAndFjord()
        {
            Register(new MonsterDefinition
            {
                Id = "wild_boar", Name = "Wild Boar", Level = 64, MaxHp = 8600, AtkMin = 320, AtkMax = 395, Def = 29, Mdef = 8,
                Hit = 135, Flee = 110, Element = Element.Earth, Race = Race.Beast, Size = Size.Medium,
                BaseExp = 3190, JobExp = 2550, Assist = true, MoveSpeed = 3.2f, AttackInterval = 1.3f,
                Skills =
                {
                    SelfBuff("frenzy", "Frenzy", MonsterBuffs.Frenzied, 40f, 30f, 50f),
                    new MonsterSkill
                    {
                        Id = "tusk_charge", Name = "Tusk Charge", Kind = MonsterSkillKind.Leap, MinRange = 3f, Range = 10f, Radius = 1.5f, CastTime = 0.6f,
                        Percent = 160f, Knockback = 3f, Cooldown = 10f, ChancePercent = 40f,
                    },
                },
                Drops =
                {
                    Drop("boar_tusk", 50f), Drop(ItemCatalog.HoneyMead, 10f), Drop(ItemCatalog.BogIron, 12f), Drop(ItemCatalog.DwarvenSteel, 5f),
                    Drop("iron_mouthguard", 0.3f), Drop("wolfskin_mantle", 0.2f), Drop("fur_boots", 0.4f), Drop("wild_boar_card", 0.75f),
                },
                Shape = MonsterShape.Quadruped, ColorHex = "#6E4B3A", Scale = 1.05f,
            });
            Register(new MonsterDefinition
            {
                Id = "dire_wolf", Name = "Dire Wolf", Level = 70, MaxHp = 9000, AtkMin = 380, AtkMax = 460, Def = 35, Mdef = 15,
                Hit = 150, Flee = 120, Element = Element.Earth, Race = Race.Beast, Size = Size.Medium,
                BaseExp = 4200, JobExp = 3300, Aggressive = true, Assist = true, MoveSpeed = 4.2f, AttackInterval = 1.1f,
                Skills =
                {
                    PackHowl(),
                    new MonsterSkill
                    {
                        Id = "rending_bite", Name = "Rending Bite", Kind = MonsterSkillKind.Strike, Percent = 180f, Status = StatusEffect.Bleeding,
                        StatusChance = 30f, StatusSeconds = 6f, Cooldown = 8f, ChancePercent = 35f,
                    },
                },
                Drops =
                {
                    Drop(ItemCatalog.RuneTiwaz, 5f), Drop(ItemCatalog.HoneyMead, 20f), Drop("wolf_pelt", 50f), Drop(ItemCatalog.DwarvenSteel, 8f), Drop(ItemCatalog.Skystone, 2f),
                    Drop("wolf_hood", 0.8f), Drop("wolf_claws", 0.5f), Drop("wolfskin_mantle", 0.4f), Drop("fang_mask", 0.2f), Drop("dire_wolf_card", 0.5f),
                },
                Shape = MonsterShape.Quadruped, ColorHex = "#5D6D7E", Scale = 1.2f,
            });
            Register(new MonsterDefinition
            {
                Id = "forest_outlaw", Name = "Forest Outlaw", Level = 86, MaxHp = 14300, AtkMin = 530, AtkMax = 650, Def = 43, Mdef = 12,
                Hit = 175, Flee = 140, Element = Element.Neutral, Race = Race.DemiHuman, Size = Size.Medium,
                BaseExp = 6100, JobExp = 4870, Aggressive = true, Assist = true, MoveSpeed = 3.4f, AttackRange = 7f, AttackInterval = 1.5f,
                Skills =
                {
                    new MonsterSkill
                    {
                        Id = "blinding_shot", Name = "Blinding Shot", Kind = MonsterSkillKind.Bolt, Range = 9f, CastTime = 0.6f, Percent = 150f,
                        Status = StatusEffect.Blind, StatusChance = 40f, StatusSeconds = 5f, Cooldown = 10f, ChancePercent = 35f,
                    },
                    new MonsterSkill
                    {
                        Id = "arrow_volley", Name = "Arrow Volley", Kind = MonsterSkillKind.Area, Range = 9f, Radius = 3f, CastTime = 1.2f, Percent = 140f,
                        Cooldown = 12f, ChancePercent = 30f, Shout = "Loose!",
                    },
                },
                Drops =
                {
                    Drop("stolen_coin_pouch", 40f), Drop(ItemCatalog.HoneyMead, 10f), Drop(ItemCatalog.RuneTiwaz, 3f), Drop("yew_longbow", 0.3f),
                    Drop("runed_blindfold", 0.2f), Drop("jamadhar", 0.15f), Drop("eyepatch", 0.5f), Drop("forest_outlaw_card", 0.75f),
                },
                Shape = MonsterShape.Humanoid, ColorHex = "#556B2F", LookWeapon = WeaponType.Bow, LookHead = "bandana", LookGarment = "traveler_cloak",
                SkinHex = "#E0B48A",
            });
            Register(new MonsterDefinition
            {
                Id = "fjord_harpy", Name = "Fjord Harpy", Level = 124, MaxHp = 36300, AtkMin = 1030, AtkMax = 1260, Def = 68, Mdef = 30,
                Hit = 250, Flee = 280, Element = Element.Wind, Race = Race.Demon, Size = Size.Medium,
                BaseExp = 13400, JobExp = 10700, Aggressive = true, MoveSpeed = 4f, AttackInterval = 1.2f,
                Skills =
                {
                    new MonsterSkill
                    {
                        Id = "gust", Name = "Gust", Kind = MonsterSkillKind.Area, CenteredOnSelf = true, Radius = 3.5f, CastTime = 0.8f, Magical = true,
                        Element = Element.Wind, Percent = 120f, Knockback = 3f, Cooldown = 12f, ChancePercent = 30f,
                    },
                    Dive(170f),
                },
                Drops =
                {
                    Drop("harpy_feather", 50f), Drop(ItemCatalog.HoneyMead, 12f), Drop(ItemCatalog.Skystone, 2f), Drop("rune_muffler", 0.3f),
                    Drop("swift_boots", 0.1f), Drop("fjord_harpy_card", 0.75f),
                },
                Shape = MonsterShape.Flyer, ColorHex = "#A9CCE3", Scale = 0.95f,
            });
            Register(new MonsterDefinition
            {
                Id = "frost_wolf", Name = "Frost Wolf", Level = 140, MaxHp = 60000, AtkMin = 1300, AtkMax = 1600, Def = 80, Mdef = 40,
                Hit = 280, Flee = 230, Element = Element.Water, Race = Race.Beast, Size = Size.Medium,
                BaseExp = 18000, JobExp = 14000, Aggressive = true, Assist = true, MoveSpeed = 4.4f, AttackInterval = 1f,
                Skills =
                {
                    PackHowl(),
                    new MonsterSkill
                    {
                        Id = "frost_bite", Name = "Frost Bite", Kind = MonsterSkillKind.Strike, Percent = 180f, Element = Element.Water,
                        Status = StatusEffect.Freeze, StatusChance = 15f, StatusSeconds = 2f, Cooldown = 10f, ChancePercent = 35f,
                    },
                },
                Drops =
                {
                    Drop(ItemCatalog.RuneSowilo, 5f), Drop(ItemCatalog.HoneyMead, 30f), Drop("frost_fang", 40f), Drop(ItemCatalog.Starmetal, 3f), Drop(ItemCatalog.Skystone, 4f),
                    Drop("frost_stiletto", 0.4f), Drop("frost_goggles", 0.3f), Drop("frost_crown", 0.03f), Drop(RunewordRules.Isa, 1f), Drop("frost_wolf_card", 0.5f),
                },
                Shape = MonsterShape.Quadruped, ColorHex = "#D6EAF8", Scale = 1.3f,
            });
            Register(new MonsterDefinition
            {
                Id = "runic_berserker", Name = "Runic Berserker", Level = 152, MaxHp = 79200, AtkMin = 1820, AtkMax = 2220, Def = 71, Mdef = 20,
                Hit = 305, Flee = 240, Element = Element.Fire, Race = Race.DemiHuman, Size = Size.Medium,
                BaseExp = 21700, JobExp = 17400, Aggressive = true, Assist = true, MoveSpeed = 3.6f, AttackRange = 1.2f, AttackInterval = 1.3f,
                Skills =
                {
                    SelfBuff("berserk", "Berserk", MonsterBuffs.Berserk, 50f, 40f, 60f, "BLOOD FOR ODIN!"),
                    new MonsterSkill
                    {
                        Id = "leaping_cleave", Name = "Leaping Cleave", Kind = MonsterSkillKind.Leap, MinRange = 4f, Range = 9f, Radius = 2.5f, CastTime = 0.8f,
                        Percent = 200f, Cooldown = 14f, ChancePercent = 30f,
                    },
                    new MonsterSkill
                    {
                        Id = "axe_rend", Name = "Axe Rend", Kind = MonsterSkillKind.Strike, Percent = 220f, BreakWeaponPercent = 2f, Cooldown = 10f,
                        ChancePercent = 35f,
                    },
                },
                Drops =
                {
                    Drop("berserker_braid", 50f), Drop(ItemCatalog.HoneyMead, 15f), Drop(ItemCatalog.RuneUruz, 4f), Drop("flamberge", 0.15f),
                    Drop("beard_of_odin", 0.08f), Drop("grand_horned_viking_crest", 0.03f), Drop("runic_full_plate", 0.03f), Drop("runic_berserker_card", 0.5f),
                },
                Shape = MonsterShape.Humanoid, ColorHex = "#922B21", LookWeapon = WeaponType.TwoHandSword, LookHead = "iron_helm", LookLower = "braided_beard",
                LookGarment = "bear_pelt", SkinHex = "#E8B896",
            });
            Register(new MonsterDefinition
            {
                Id = "sea_drake", Name = "Sea Drake", Level = 166, MaxHp = 139000, AtkMin = 1940, AtkMax = 2370, Def = 99, Mdef = 45,
                Hit = 330, Flee = 255, Element = Element.Water, Race = Race.Dragon, Size = Size.Large,
                BaseExp = 26600, JobExp = 21300, Aggressive = true, MoveSpeed = 3.2f, AttackRange = 1.4f, AttackInterval = 1.5f,
                Skills =
                {
                    new MonsterSkill
                    {
                        Id = "tail_sweep", Name = "Tail Sweep", Kind = MonsterSkillKind.Area, CenteredOnSelf = true, Radius = 3f, CastTime = 0.6f,
                        Percent = 160f, Knockback = 3f, Cooldown = 9f, ChancePercent = 35f,
                    },
                    new MonsterSkill
                    {
                        Id = "tidal_breath", Name = "Tidal Breath", Kind = MonsterSkillKind.Area, Range = 8f, Radius = 3.5f, CastTime = 1.5f, Magical = true,
                        Element = Element.Water, Percent = 180f, Cooldown = 12f, ChancePercent = 35f,
                    },
                },
                Drops =
                {
                    Drop("drake_scale", 50f), Drop(ItemCatalog.HoneyMead, 15f), Drop(ItemCatalog.Starmetal, 3f), Drop("rune_lance", 0.2f),
                    Drop("dragon_flame_wings", 0.01f), Drop("sea_drake_card", 0.5f),
                },
                Shape = MonsterShape.Serpent, ColorHex = "#1F618D", Scale = 1.5f,
            });
        }

        // ================================================================ Jotun Steppe (Lv 181–255)
        private static void RegisterSteppe()
        {
            Register(new MonsterDefinition
            {
                Id = "ice_golem", Name = "Ice Golem", Level = 196, MaxHp = 282000, AtkMin = 3160, AtkMax = 3860, Def = 188, Mdef = 50,
                Hit = 390, Flee = 170, Element = Element.Water, Race = Race.Formless, Size = Size.Large,
                BaseExp = 39800, JobExp = 31800, MoveSpeed = 2.2f, AttackRange = 1.3f, AttackInterval = 1.8f,
                Skills =
                {
                    SelfBuff("ice_armor", "Ice Armor", MonsterBuffs.IceArmor, 60f, 40f),
                    new MonsterSkill
                    {
                        Id = "glacial_slam", Name = "Glacial Slam", Kind = MonsterSkillKind.Area, CenteredOnSelf = true, Radius = 3.5f, CastTime = 1.4f,
                        Percent = 200f, Element = Element.Water, Status = StatusEffect.Freeze, StatusChance = 25f, StatusSeconds = 2.5f,
                        Cooldown = 12f, ChancePercent = 35f,
                    },
                },
                Drops =
                {
                    Drop("ice_core", 50f), Drop(ItemCatalog.HoneyMead, 15f), Drop(ItemCatalog.Skystone, 4f), Drop(ItemCatalog.Starmetal, 4f), Drop(ItemCatalog.RuneIsa, 3f),
                    Drop("frost_crown", 0.08f), Drop("runic_full_plate", 0.08f), Drop("fimbul_frost_wings", 0.02f), Drop("ice_golem_card", 0.5f),
                },
                Shape = MonsterShape.Golem, ColorHex = "#AED6F1", Scale = 1.6f,
            });
            Register(new MonsterDefinition
            {
                Id = "snow_harpy", Name = "Snow Harpy", Level = 222, MaxHp = 309000, AtkMin = 3880, AtkMax = 4750, Def = 152, Mdef = 60,
                Hit = 445, Flee = 385, Element = Element.Water, Race = Race.Demon, Size = Size.Medium,
                BaseExp = 54800, JobExp = 43800, Aggressive = true, MoveSpeed = 4.2f, AttackInterval = 1.2f,
                Skills =
                {
                    new MonsterSkill
                    {
                        Id = "blizzard_gale", Name = "Blizzard Gale", Kind = MonsterSkillKind.Area, Range = 9f, Radius = 4f, CastTime = 1.6f, Magical = true,
                        Element = Element.Water, Percent = 170f, Status = StatusEffect.Freeze, StatusChance = 20f, StatusSeconds = 2f,
                        Cooldown = 14f, ChancePercent = 30f,
                    },
                    Dive(180f, 10f, 30f),
                    new MonsterSkill
                    {
                        Id = "frost_feather", Name = "Frost Feather", Kind = MonsterSkillKind.Bolt, Range = 8f, CastTime = 0.8f, Magical = true,
                        Element = Element.Water, Percent = 150f, Cooldown = 6f, ChancePercent = 35f,
                    },
                },
                Drops =
                {
                    Drop("snow_plume", 50f), Drop(ItemCatalog.HoneyMead, 20f), Drop(ItemCatalog.Skystone, 5f), Drop("swift_boots", 0.15f),
                    Drop("raven_longbow", 0.02f), Drop("valkyrian_feather_wings", 0.02f), Drop("snow_harpy_card", 0.5f),
                },
                Shape = MonsterShape.Flyer, ColorHex = "#EBF5FB", Scale = 1f,
            });
            Register(new MonsterDefinition
            {
                Id = "jotun_brawler", Name = "Jotun Brawler", Level = 230, MaxHp = 400000, AtkMin = 4200, AtkMax = 5200, Def = 160, Mdef = 60,
                Hit = 460, Flee = 300, Element = Element.Water, Race = Race.DemiHuman, Size = Size.Large,
                BaseExp = 60000, JobExp = 50000, Aggressive = true, MoveSpeed = 3.2f, AttackRange = 1.6f, AttackInterval = 1.6f,
                Skills =
                {
                    SelfBuff("giants_rage", "Giant's Rage", MonsterBuffs.Berserk, 40f, 45f, 60f, "JOTUNHEIM REMEMBERS!"),
                    new MonsterSkill
                    {
                        Id = "earthshaker", Name = "Earthshaker", Kind = MonsterSkillKind.Area, CenteredOnSelf = true, Radius = 4f, CastTime = 1.6f,
                        Percent = 220f, Knockback = 4f, Cooldown = 16f, ChancePercent = 30f,
                    },
                    new MonsterSkill
                    {
                        Id = "bone_crusher", Name = "Bone Crusher", Kind = MonsterSkillKind.Strike, Percent = 250f, BreakWeaponPercent = 3f,
                        Status = StatusEffect.Stun, StatusChance = 20f, StatusSeconds = 2f, Cooldown = 12f, ChancePercent = 35f,
                    },
                },
                Drops =
                {
                    Drop(ItemCatalog.RuneUruz, 10f), Drop(ItemCatalog.HoneyMead, 40f), Drop("jotun_tooth", 40f), Drop(ItemCatalog.Starmetal, 6f), Drop(ItemCatalog.Skystone, 6f),
                    Drop(RunewordRules.Thurisaz, 2f), Drop("plated_greaves", 0.5f), Drop("fist_of_odin_knuckles", 0.02f), Drop("muspel_flamberge", 0.03f),
                    Drop(ItemCatalog.BloodBranch, 0.5f), Drop("jotun_brawler_card", 0.5f),
                },
                Shape = MonsterShape.Humanoid, ColorHex = "#5D6D7E", LookLower = "braided_beard", LookGarment = "bear_pelt", SkinHex = "#A9CCE3", Scale = 2.2f,
            });
            Register(new MonsterDefinition
            {
                Id = "frost_wyrm", Name = "Frost Wyrm", Level = 244, MaxHp = 605000, AtkMin = 4860, AtkMax = 5900, Def = 171, Mdef = 90,
                Hit = 480, Flee = 310, Element = Element.Water, Race = Race.Dragon, Size = Size.Large,
                BaseExp = 70700, JobExp = 56600, Aggressive = true, MoveSpeed = 3.4f, AttackRange = 1.6f, AttackInterval = 1.6f,
                Skills =
                {
                    SelfBuff("glacial_hide", "Glacial Hide", MonsterBuffs.IceArmor, 50f, 40f),
                    new MonsterSkill
                    {
                        Id = "wing_buffet", Name = "Wing Buffet", Kind = MonsterSkillKind.Area, CenteredOnSelf = true, Radius = 4f, CastTime = 0.8f,
                        Percent = 160f, Knockback = 4f, Cooldown = 10f, ChancePercent = 30f,
                    },
                    new MonsterSkill
                    {
                        Id = "frost_breath", Name = "Frost Breath", Kind = MonsterSkillKind.Area, Range = 9f, Radius = 4.5f, CastTime = 1.8f, Magical = true,
                        Element = Element.Water, Percent = 220f, Status = StatusEffect.Frostbite, StatusChance = 50f, StatusSeconds = 5f,
                        Cooldown = 12f, ChancePercent = 35f,
                    },
                },
                Drops =
                {
                    Drop("wyrm_heart", 40f), Drop(ItemCatalog.HoneyMead, 20f), Drop(ItemCatalog.Starmetal, 6f), Drop(ItemCatalog.Skystone, 6f), Drop(ItemCatalog.RuneIsa, 3f),
                    Drop("fimbul_frost_wings", 0.03f), Drop("yggdrasil_staff", 0.03f), Drop("dragon_flame_wings", 0.02f), Drop("frost_wyrm_card", 0.5f),
                },
                Shape = MonsterShape.Serpent, ColorHex = "#D4E6F1", Scale = 1.9f,
            });
        }
    }
}
