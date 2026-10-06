using Runeheir.Combat;
using Runeheir.Items;

namespace Runeheir.Monsters
{
    /// <summary>The Catacombs of Helheim (B1–B4) and the Sunken Fjord Caverns (1–3).</summary>
    public static partial class MonsterCatalog
    {
        // ================================================================ Catacombs of Helheim
        private static void RegisterCatacombs()
        {
            Register(new MonsterDefinition
            {
                Id = "crypt_bat", Name = "Crypt Bat", Level = 94, MaxHp = 13900, AtkMin = 610, AtkMax = 750, Def = 40, Mdef = 20,
                Hit = 190, Flee = 200, Element = Element.Shadow, Race = Race.Beast, Size = Size.Small,
                BaseExp = 7200, JobExp = 5700, Aggressive = true, MoveSpeed = 4.2f, AttackInterval = 1f,
                Skills =
                {
                    new MonsterSkill
                    {
                        Id = "blood_drain", Name = "Blood Drain", Kind = MonsterSkillKind.Strike, Percent = 140f, LeechPercent = 50f, Cooldown = 8f,
                        ChancePercent = 40f,
                    },
                },
                Drops =
                {
                    Drop("bat_wing", 50f), Drop(ItemCatalog.AetherSap, 15f), Drop(ItemCatalog.RavenFeather, 5f), Drop("slotted_dark_sunglasses", 0.3f),
                    Drop("fang_mask", 0.3f), Drop("crypt_bat_card", 0.75f),
                },
                Shape = MonsterShape.Flyer, ColorHex = "#4A235A", Scale = 0.7f,
            });
            Register(new MonsterDefinition
            {
                Id = "draugr_footman", Name = "Draugr Footman", Level = 104, MaxHp = 22700, AtkMin = 730, AtkMax = 890, Def = 79, Mdef = 20,
                Hit = 210, Flee = 165, Element = Element.Undead, Race = Race.Undead, Size = Size.Medium,
                BaseExp = 8800, JobExp = 7000, Aggressive = true, Assist = true, MoveSpeed = 2.8f, AttackInterval = 1.4f,
                Skills =
                {
                    SelfBuff("shield_wall", "Shield Wall", MonsterBuffs.ShieldWall, 50f, 30f, 40f),
                    new MonsterSkill
                    {
                        Id = "shield_bash", Name = "Shield Bash", Kind = MonsterSkillKind.Strike, Percent = 130f, Status = StatusEffect.Stun, StatusChance = 25f,
                        StatusSeconds = 2f, Cooldown = 10f, ChancePercent = 35f,
                    },
                },
                Drops =
                {
                    Drop("draugr_bone", 50f), Drop(ItemCatalog.HoneyMead, 10f), Drop(ItemCatalog.DwarvenSteel, 6f), Drop("iron_helm", 0.4f),
                    Drop("round_viking_shield", 0.4f), Drop("chainmail", 0.3f), Drop("mirror_shield", 0.08f), Drop("draugr_footman_card", 0.75f),
                },
                Shape = MonsterShape.Humanoid, ColorHex = "#5F6A6A", LookWeapon = WeaponType.OneHandSword, LookHead = "iron_helm",
                LookShield = "round_viking_shield", SkinHex = "#9FB3A8",
            });
            Register(new MonsterDefinition
            {
                Id = "ghoul", Name = "Ghoul", Level = 128, MaxHp = 58200, AtkMin = 1090, AtkMax = 1340, Def = 71, Mdef = 25,
                Hit = 255, Flee = 150, Element = Element.Undead, Race = Race.Undead, Size = Size.Medium,
                BaseExp = 14500, JobExp = 11600, Aggressive = true, MoveSpeed = 2.4f, AttackInterval = 1.5f,
                Skills =
                {
                    new MonsterSkill
                    {
                        Id = "feast", Name = "Feast", Kind = MonsterSkillKind.Heal, HealPercent = 10f, BelowHpPercent = 50f, CastTime = 1.2f,
                        Cooldown = 25f, ChancePercent = 40f,
                    },
                    new MonsterSkill
                    {
                        Id = "rotting_claw", Name = "Rotting Claw", Kind = MonsterSkillKind.Strike, Percent = 150f, Status = StatusEffect.Poison,
                        StatusChance = 40f, StatusSeconds = 8f, Cooldown = 8f, ChancePercent = 35f,
                    },
                },
                Drops =
                {
                    Drop("rotten_bandage", 50f), Drop(ItemCatalog.HoneyMead, 12f), Drop(ItemCatalog.DwarvenSteel, 8f), Drop("saint_robe", 0.3f),
                    Drop("holy_mace", 0.05f), Drop("ghoul_card", 0.75f),
                },
                Shape = MonsterShape.Humanoid, ColorHex = "#3E4A3D", SkinHex = "#7D8C6B",
            });
            Register(new MonsterDefinition
            {
                Id = "crypt_wraith", Name = "Crypt Wraith", Level = 154, MaxHp = 74500, AtkMin = 1630, AtkMax = 1990, Def = 60, Mdef = 40,
                Hit = 310, Flee = 295, Element = Element.Ghost, Race = Race.Demon, Size = Size.Medium,
                BaseExp = 22400, JobExp = 17900, Aggressive = true, MoveSpeed = 3.4f, AttackInterval = 1.3f,
                Skills =
                {
                    new MonsterSkill
                    {
                        Id = "phase_shift", Name = "Phase Shift", Kind = MonsterSkillKind.Teleport, BehindTarget = true, Range = 8f, Cooldown = 15f,
                        ChancePercent = 30f,
                    },
                    new MonsterSkill
                    {
                        Id = "soul_drain", Name = "Soul Drain", Kind = MonsterSkillKind.Bolt, Range = 7f, CastTime = 1.2f, Magical = true, Element = Element.Ghost,
                        Percent = 150f, LeechPercent = 30f, Cooldown = 8f, ChancePercent = 35f,
                    },
                },
                Drops =
                {
                    Drop("wraith_shroud", 50f), Drop(ItemCatalog.AetherSap, 20f), Drop("runed_staff", 0.3f), Drop("archmage_wizard_hat", 0.05f),
                    Drop("demon_nether_wings", 0.01f), Drop("crypt_wraith_card", 0.5f),
                },
                Shape = MonsterShape.Wraith, ColorHex = "#7FB3D5", Scale = 1.1f,
            });
            Register(new MonsterDefinition
            {
                Id = "banshee", Name = "Banshee", Level = 182, MaxHp = 132000, AtkMin = 2400, AtkMax = 2940, Def = 80, Mdef = 60,
                Hit = 365, Flee = 270, Element = Element.Shadow, Race = Race.Undead, Size = Size.Medium,
                BaseExp = 32900, JobExp = 26300, Aggressive = true, MoveSpeed = 3.2f, AttackRange = 6f, AttackInterval = 1.6f,
                Skills =
                {
                    new MonsterSkill
                    {
                        Id = "wail", Name = "Wail", Kind = MonsterSkillKind.Area, CenteredOnSelf = true, Radius = 6f, CastTime = 1.5f, Magical = true,
                        Element = Element.Shadow, Percent = 130f, Status = StatusEffect.Silence, StatusChance = 40f, StatusSeconds = 5f,
                        Cooldown = 16f, ChancePercent = 35f, Shout = "Aaaaiiieee!",
                    },
                    new MonsterSkill
                    {
                        Id = "shadow_bolt", Name = "Shadow Bolt", Kind = MonsterSkillKind.Bolt, Range = 8f, CastTime = 1.2f, Magical = true,
                        Element = Element.Shadow, Percent = 180f, Cooldown = 6f, ChancePercent = 40f,
                    },
                },
                Drops =
                {
                    Drop("spectral_lace", 50f), Drop(ItemCatalog.AetherSap, 25f), Drop("rune_circlet", 0.4f), Drop("archmage_wizard_hat", 0.08f),
                    Drop("norn_staff", 0.03f), Drop("banshee_card", 0.5f),
                },
                Shape = MonsterShape.Wraith, ColorHex = "#D2B4DE", Scale = 1f,
            });
            Register(new MonsterDefinition
            {
                Id = "corrupted_einherjar", Name = "Corrupted Einherjar", Level = 198, MaxHp = 211000, AtkMin = 2950, AtkMax = 3600, Def = 127, Mdef = 50,
                Hit = 395, Flee = 280, Element = Element.Shadow, Race = Race.DemiHuman, Size = Size.Medium,
                BaseExp = 40900, JobExp = 32700, Aggressive = true, MoveSpeed = 3.4f, AttackRange = 1.3f, AttackInterval = 1.4f,
                Skills =
                {
                    SelfBuff("fallen_valor", "Fallen Valor", MonsterBuffs.WarCry, 50f, 40f, 50f, "Valhalla... forsook us!"),
                    new MonsterSkill
                    {
                        Id = "valkyries_fall", Name = "Valkyrie's Fall", Kind = MonsterSkillKind.Leap, MinRange = 4f, Range = 10f, Radius = 3f, CastTime = 0.9f,
                        Percent = 200f, Cooldown = 15f, ChancePercent = 30f,
                    },
                    new MonsterSkill
                    {
                        Id = "sunder_strike", Name = "Sunder Strike", Kind = MonsterSkillKind.Strike, Percent = 230f, BreakWeaponPercent = 3f, Cooldown = 12f,
                        ChancePercent = 35f,
                    },
                },
                Drops =
                {
                    Drop("tarnished_valknut", 50f), Drop(ItemCatalog.HoneyMead, 15f), Drop(ItemCatalog.Skystone, 4f), Drop("einherjar_greatsword", 0.04f),
                    Drop("valkyrie_winged_helm", 0.08f), Drop("holy_mace", 0.15f), Drop("templar_mace", 0.02f), Drop("corrupted_einherjar_card", 0.5f),
                },
                Shape = MonsterShape.Humanoid, ColorHex = "#4A4A4A", LookWeapon = WeaponType.TwoHandSword, LookHead = "valkyrie_winged_helm",
                LookGarment = "valkyrian_manteau", SkinHex = "#9EA7B0",
            });
            Register(new MonsterDefinition
            {
                Id = "frozen_revenant", Name = "Frozen Revenant", Level = 228, MaxHp = 385000, AtkMin = 4140, AtkMax = 5100, Def = 158, Mdef = 50,
                Hit = 455, Flee = 300, Element = Element.Undead, Race = Race.Undead, Size = Size.Medium,
                BaseExp = 58700, JobExp = 46900, Aggressive = true, MoveSpeed = 3f, AttackInterval = 1.5f,
                Skills =
                {
                    new MonsterSkill
                    {
                        Id = "frozen_grasp", Name = "Frozen Grasp", Kind = MonsterSkillKind.Area, Range = 8f, Radius = 3f, CastTime = 1.2f, Magical = true,
                        Element = Element.Water, Percent = 150f, Status = StatusEffect.Root, StatusChance = 50f, StatusSeconds = 3f,
                        Cooldown = 14f, ChancePercent = 30f,
                    },
                    new MonsterSkill
                    {
                        Id = "grave_frost", Name = "Grave Frost", Kind = MonsterSkillKind.Bolt, Range = 8f, CastTime = 1.3f, Magical = true, Element = Element.Water,
                        Percent = 200f, Status = StatusEffect.Freeze, StatusChance = 25f, StatusSeconds = 2.5f, Cooldown = 8f, ChancePercent = 40f,
                    },
                },
                Drops =
                {
                    Drop("grave_frost", 50f), Drop(ItemCatalog.HoneyMead, 20f), Drop(ItemCatalog.Starmetal, 5f), Drop("frost_crown", 0.08f),
                    Drop("templar_mace", 0.03f), Drop("frozen_revenant_card", 0.5f),
                },
                Shape = MonsterShape.Humanoid, ColorHex = "#2E4053", LookWeapon = WeaponType.Staff, LookHead = "frost_crown", SkinHex = "#D6EAF8",
            });
            Register(new MonsterDefinition
            {
                Id = "hels_executioner", Name = "Hel's Executioner", Level = 240, MaxHp = 473000, AtkMin = 4680, AtkMax = 5700, Def = 168, Mdef = 60,
                Hit = 475, Flee = 300, Element = Element.Shadow, Race = Race.Undead, Size = Size.Large,
                BaseExp = 67600, JobExp = 54000, Aggressive = true, MoveSpeed = 3.2f, AttackRange = 1.6f, AttackInterval = 1.7f,
                Skills =
                {
                    SelfBuff("bloodlust", "Bloodlust", MonsterBuffs.Berserk, 40f, 45f, 60f),
                    new MonsterSkill
                    {
                        Id = "hels_chains", Name = "Hel's Chains", Kind = MonsterSkillKind.Bolt, Range = 8f, MinRange = 2f, CastTime = 0.8f, Percent = 150f,
                        Status = StatusEffect.Root, StatusChance = 60f, StatusSeconds = 3f, Cooldown = 12f, ChancePercent = 30f,
                    },
                    new MonsterSkill
                    {
                        Id = "execution", Name = "Execution", Kind = MonsterSkillKind.Strike, CastTime = 1.2f, Percent = 300f, BreakWeaponPercent = 3f,
                        PoiseDamage = 999f, Cooldown = 14f, ChancePercent = 35f, Shout = "Kneel!",
                    },
                },
                Drops =
                {
                    Drop("hel_brand", 50f), Drop(ItemCatalog.HoneyMead, 20f), Drop(ItemCatalog.Starmetal, 6f), Drop("muspel_flamberge", 0.04f),
                    Drop("einherjar_greatsword", 0.04f), Drop("helm_of_awe", 0.01f), Drop("hels_executioner_card", 0.5f),
                },
                Shape = MonsterShape.Humanoid, ColorHex = "#1B2631", LookWeapon = WeaponType.TwoHandSword, LookHead = "raven_hood", SkinHex = "#7F8C8D",
                Scale = 1.5f,
            });
        }

        // ================================================================ Sunken Fjord Caverns
        private static void RegisterCaverns()
        {
            Register(new MonsterDefinition
            {
                Id = "cave_crawler", Name = "Cave Crawler", Level = 136, MaxHp = 54600, AtkMin = 1230, AtkMax = 1510, Def = 154, Mdef = 20,
                Hit = 270, Flee = 200, Element = Element.Earth, Race = Race.Insect, Size = Size.Small,
                BaseExp = 16800, JobExp = 13400, Aggressive = true, MoveSpeed = 3f, AttackInterval = 1.4f,
                Skills =
                {
                    new MonsterSkill
                    {
                        Id = "burrow", Name = "Burrow", Kind = MonsterSkillKind.Teleport, Range = 3f, BelowHpPercent = 30f, Cooldown = 25f, ChancePercent = 50f,
                    },
                    new MonsterSkill
                    {
                        Id = "petrifying_spit", Name = "Petrifying Spit", Kind = MonsterSkillKind.Bolt, Range = 7f, CastTime = 1f, Magical = true,
                        Element = Element.Earth, Percent = 120f, Status = StatusEffect.StoneCurse, StatusChance = 25f, StatusSeconds = 4f,
                        Cooldown = 12f, ChancePercent = 35f,
                    },
                },
                Drops =
                {
                    Drop("crawler_carapace", 50f), Drop(ItemCatalog.HoneyMead, 12f), Drop(ItemCatalog.Starmetal, 2f), Drop("frost_goggles", 0.2f),
                    Drop("iron_knuckles", 0.2f), Drop("cave_crawler_card", 0.75f),
                },
                Shape = MonsterShape.Quadruped, ColorHex = "#7E5109", Scale = 0.8f,
            });
            Register(new MonsterDefinition
            {
                Id = "naga_scout", Name = "Naga Scout", Level = 158, MaxHp = 90200, AtkMin = 1730, AtkMax = 2110, Def = 93, Mdef = 35,
                Hit = 315, Flee = 250, Element = Element.Water, Race = Race.Fish, Size = Size.Medium,
                BaseExp = 23700, JobExp = 19000, Aggressive = true, Assist = true, MoveSpeed = 3.6f, AttackRange = 1.4f, AttackInterval = 1.3f,
                Skills =
                {
                    new MonsterSkill
                    {
                        Id = "coil", Name = "Coil", Kind = MonsterSkillKind.Strike, Percent = 150f, Status = StatusEffect.Root, StatusChance = 30f,
                        StatusSeconds = 3f, Cooldown = 12f, ChancePercent = 30f,
                    },
                    new MonsterSkill
                    {
                        Id = "venom_javelin", Name = "Venom Javelin", Kind = MonsterSkillKind.Bolt, Range = 9f, MinRange = 2f, CastTime = 0.8f,
                        Element = Element.Poison, Percent = 160f, Status = StatusEffect.Poison, StatusChance = 40f, StatusSeconds = 8f,
                        Cooldown = 8f, ChancePercent = 40f,
                    },
                },
                Drops =
                {
                    Drop("naga_scale", 50f), Drop(ItemCatalog.HoneyMead, 15f), Drop(ItemCatalog.Starmetal, 3f), Drop("rune_lance", 0.2f),
                    Drop("mirror_shield", 0.1f), Drop("ward_amulet", 0.3f), Drop("naga_scout_card", 0.5f),
                },
                Shape = MonsterShape.Serpent, ColorHex = "#48C9B0", Scale = 0.95f, LookWeapon = WeaponType.Spear,
            });
            Register(new MonsterDefinition
            {
                Id = "abyssal_leech", Name = "Abyssal Leech", Level = 226, MaxHp = 334000, AtkMin = 4050, AtkMax = 4960, Def = 156, Mdef = 70,
                Hit = 450, Flee = 300, Element = Element.Poison, Race = Race.Fish, Size = Size.Small,
                BaseExp = 57400, JobExp = 45900, Aggressive = true, MoveSpeed = 3.2f, AttackInterval = 1.2f,
                Skills =
                {
                    new MonsterSkill
                    {
                        Id = "leech_bite", Name = "Leech Bite", Kind = MonsterSkillKind.Strike, Percent = 160f, LeechPercent = 60f, Cooldown = 8f,
                        ChancePercent = 40f,
                    },
                    new MonsterSkill
                    {
                        Id = "ichor_spray", Name = "Ichor Spray", Kind = MonsterSkillKind.Area, Range = 7f, Radius = 3f, CastTime = 1f, Magical = true,
                        Element = Element.Poison, Percent = 150f, Status = StatusEffect.Poison, StatusChance = 50f, StatusSeconds = 8f,
                        Cooldown = 12f, ChancePercent = 30f,
                    },
                },
                Drops =
                {
                    Drop("leech_ichor", 50f), Drop(ItemCatalog.AetherSap, 25f), Drop(ItemCatalog.Skystone, 5f), Drop("shadow_katar", 0.03f),
                    Drop("demon_nether_wings", 0.02f), Drop("abyssal_leech_card", 0.5f),
                },
                Shape = MonsterShape.Serpent, ColorHex = "#1C2833", Scale = 0.9f,
            });
        }
    }
}
