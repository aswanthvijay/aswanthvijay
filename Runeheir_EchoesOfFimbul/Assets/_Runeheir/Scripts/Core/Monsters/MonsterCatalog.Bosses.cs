using Runeheir.Combat;
using Runeheir.Items;

namespace Runeheir.Monsters
{
    /// <summary>The 2-hour field mini-bosses and the 1-hour MVP world bosses (the Holy Trinity), with Fenrir's two wolves.</summary>
    public static partial class MonsterCatalog
    {
        // ================================================================ mini-bosses
        private static void RegisterMiniBosses()
        {
            Register(new MonsterDefinition
            {
                Id = "elder_direwolf", Name = "Elder Direwolf", Rank = MonsterRank.MiniBoss, Level = 115, MaxHp = 450000, AtkMin = 1340, AtkMax = 1630,
                Def = 80, Mdef = 30, Hit = 260, Flee = 220, Element = Element.Earth, Race = Race.Beast, Size = Size.Large,
                BaseExp = 224000, JobExp = 180000, Aggressive = true, MoveSpeed = 4.5f, AttackRange = 1.4f, AttackInterval = 1.1f,
                Skills =
                {
                    Summon("alpha_howl", "Alpha Howl", "dire_wolf", 3, 45f, shout: "AWOOOOOO!"),
                    PackHowl(30f, 40f),
                    new MonsterSkill
                    {
                        Id = "savage_pounce", Name = "Savage Pounce", Kind = MonsterSkillKind.Leap, MinRange = 3f, Range = 12f, Radius = 2.5f, CastTime = 0.8f,
                        Percent = 220f, Status = StatusEffect.Stun, StatusChance = 30f, StatusSeconds = 2f, Cooldown = 10f, ChancePercent = 40f,
                    },
                    new MonsterSkill
                    {
                        Id = "rending_bite", Name = "Rending Bite", Kind = MonsterSkillKind.Strike, Percent = 200f, Status = StatusEffect.Bleeding,
                        StatusChance = 50f, StatusSeconds = 8f, Cooldown = 8f, ChancePercent = 40f,
                    },
                },
                Phases = { new BossPhase { BelowHpPercent = 50f, BuffId = MonsterBuffs.Enraged, Shout = "The Elder Direwolf's eyes burn red!" } },
                Drops =
                {
                    Drop("elder_wolf_mane", 60f), Drop(ItemCatalog.HoneyMead, 50f), Drop(ItemCatalog.DeadBranch, 10f), Drop("wolf_hood", 30f),
                    Drop("wolfskin_mantle", 20f), Drop("fang_mask", 20f), Drop("swift_boots", 3f), Drop("valkyrie_winged_helm", 2f),
                    Drop("raven_longbow", 0.5f), Drop("elder_direwolf_card", 0.5f),
                },
                Shape = MonsterShape.Quadruped, ColorHex = "#34495E", Scale = 2f,
            });
            Register(new MonsterDefinition
            {
                Id = "draugr_warlord", Name = "Draugr Warlord", Rank = MonsterRank.MiniBoss, Level = 178, MaxHp = 1350000, AtkMin = 3190, AtkMax = 3900,
                Def = 140, Mdef = 60, Hit = 380, Flee = 280, Element = Element.Undead, Race = Race.Undead, Size = Size.Large,
                BaseExp = 620000, JobExp = 500000, Aggressive = true, MoveSpeed = 3.2f, AttackRange = 1.6f, AttackInterval = 1.5f,
                Skills =
                {
                    Summon("raise_the_fallen", "Raise the Fallen", "draugr_footman", 3, 50f, shout: "Rise, my shieldmen!"),
                    SelfBuff("shield_wall", "Shield Wall", MonsterBuffs.ShieldWall, 50f, 30f),
                    new MonsterSkill
                    {
                        Id = "grave_roar", Name = "Grave Roar", Kind = MonsterSkillKind.Area, CenteredOnSelf = true, Radius = 7f, CastTime = 1f, Magical = true,
                        Element = Element.Shadow, Percent = 100f, Status = StatusEffect.Curse, StatusChance = 40f, StatusSeconds = 6f,
                        Cooldown = 20f, ChancePercent = 30f,
                    },
                    new MonsterSkill
                    {
                        Id = "warlords_cleave", Name = "Warlord's Cleave", Kind = MonsterSkillKind.Area, CenteredOnSelf = true, Radius = 4f, CastTime = 1.4f,
                        Percent = 240f, Knockback = 3f, Cooldown = 12f, ChancePercent = 40f,
                    },
                },
                Phases = { new BossPhase { BelowHpPercent = 40f, BuffId = MonsterBuffs.Enraged, Shout = "The Warlord's crest blazes with grave-light!" } },
                Drops =
                {
                    Drop("warlord_seal", 60f), Drop(ItemCatalog.HoneyMead, 50f), Drop(ItemCatalog.Starmetal, 30f), Drop("mirror_shield", 10f),
                    Drop("grand_horned_viking_crest", 5f), Drop("runic_full_plate", 5f), Drop("beard_of_odin", 5f), Drop("einherjar_greatsword", 2f),
                    Drop("valkyrian_shield", 1f), Drop("draugr_warlord_card", 0.5f),
                },
                Shape = MonsterShape.Humanoid, ColorHex = "#2C3E50", LookWeapon = WeaponType.OneHandSword, LookHead = "grand_horned_viking_crest",
                LookShield = "mirror_shield", LookGarment = "bear_pelt", SkinHex = "#95A5A6", Scale = 1.8f,
            });
            Register(new MonsterDefinition
            {
                Id = "naga_queen", Name = "Naga Queen", Rank = MonsterRank.MiniBoss, Level = 232, MaxHp = 2500000, AtkMin = 5600, AtkMax = 6860,
                Def = 200, Mdef = 120, Hit = 520, Flee = 330, Element = Element.Water, Race = Race.Fish, Size = Size.Large,
                BaseExp = 1230000, JobExp = 980000, Aggressive = true, MoveSpeed = 3.4f, AttackRange = 1.8f, AttackInterval = 1.4f,
                Skills =
                {
                    new MonsterSkill
                    {
                        Id = "serpents_blessing", Name = "Serpent's Blessing", Kind = MonsterSkillKind.Heal, HealPercent = 8f, BelowHpPercent = 50f, Radius = 10f,
                        CastTime = 2f, Cooldown = 30f, ChancePercent = 50f,
                    },
                    Summon("royal_guard", "Royal Guard", "naga_scout", 3, 45f, shout: "Guards! To your queen!"),
                    new MonsterSkill
                    {
                        Id = "tidal_wave", Name = "Tidal Wave", Kind = MonsterSkillKind.Area, CenteredOnSelf = true, Radius = 6f, CastTime = 2f, Magical = true,
                        Element = Element.Water, Percent = 220f, Knockback = 4f, Cooldown = 16f, ChancePercent = 35f,
                    },
                    new MonsterSkill
                    {
                        Id = "siren_song", Name = "Siren Song", Kind = MonsterSkillKind.Area, Range = 10f, Radius = 4f, CastTime = 1.5f, Magical = true,
                        Element = Element.Water, Percent = 120f, Status = StatusEffect.Sleep, StatusChance = 40f, StatusSeconds = 4f,
                        Cooldown = 18f, ChancePercent = 30f,
                    },
                    new MonsterSkill
                    {
                        Id = "hydro_lance", Name = "Hydro Lance", Kind = MonsterSkillKind.Bolt, Range = 10f, CastTime = 1.2f, Magical = true, Element = Element.Water,
                        Percent = 260f, Cooldown = 7f, ChancePercent = 35f,
                    },
                },
                Phases = { new BossPhase { BelowHpPercent = 50f, BuffId = MonsterBuffs.Enraged, Shout = "The Naga Queen's song turns to a scream!" } },
                Drops =
                {
                    Drop("naga_pearl", 60f), Drop(ItemCatalog.HoneyMead, 50f), Drop(ItemCatalog.Skystone, 30f), Drop("mirror_shield", 10f),
                    Drop("archmage_wizard_hat", 5f), Drop("valkyrian_lance", 3f), Drop("norn_staff", 3f), Drop("demon_nether_wings", 1f),
                    Drop("naga_queen_card", 0.5f),
                },
                Shape = MonsterShape.Serpent, ColorHex = "#16A085", Scale = 2.2f,
            });
            Register(new MonsterDefinition
            {
                Id = "ancient_golem", Name = "Ancient Golem", Rank = MonsterRank.MiniBoss, Level = 250, MaxHp = 3300000, AtkMin = 6700, AtkMax = 8190,
                Def = 300, Mdef = 80, Hit = 520, Flee = 250, Element = Element.Earth, Race = Race.Formless, Size = Size.Large,
                BaseExp = 1500000, JobExp = 1200000, Aggressive = true, MoveSpeed = 2.4f, AttackRange = 2f, AttackInterval = 1.9f,
                Skills =
                {
                    SelfBuff("stone_skin", "Stone Skin", MonsterBuffs.StoneSkin, 50f, 60f, 60f),
                    Summon("rock_guardians", "Rock Guardians", "ice_golem", 2, 60f, minPhase: 1),
                    new MonsterSkill
                    {
                        Id = "seismic_slam", Name = "Seismic Slam", Kind = MonsterSkillKind.Area, CenteredOnSelf = true, Radius = 5f, CastTime = 2f, Percent = 260f,
                        Status = StatusEffect.Stun, StatusChance = 30f, StatusSeconds = 2f, Cooldown = 14f, ChancePercent = 35f,
                    },
                    new MonsterSkill
                    {
                        Id = "boulder_toss", Name = "Boulder Toss", Kind = MonsterSkillKind.Area, Range = 12f, MinRange = 4f, Radius = 3.5f, CastTime = 1.6f,
                        Percent = 220f, Cooldown = 10f, ChancePercent = 35f,
                    },
                    new MonsterSkill
                    {
                        Id = "crushing_fist", Name = "Crushing Fist", Kind = MonsterSkillKind.Strike, CastTime = 0.8f, Percent = 300f, BreakWeaponPercent = 5f,
                        Cooldown = 10f, ChancePercent = 40f,
                    },
                },
                Phases =
                {
                    new BossPhase { BelowHpPercent = 60f, BuffId = MonsterBuffs.Enraged, Shout = "Runes flare across the Ancient Golem's body!" },
                    new BossPhase { BelowHpPercent = 25f, BuffId = MonsterBuffs.Unbound, Shout = "The Ancient Golem's core cracks open!" },
                },
                Drops =
                {
                    Drop("golem_core", 60f), Drop(ItemCatalog.Starmetal, 50f), Drop(ItemCatalog.Skystone, 50f), Drop("frost_crown", 10f),
                    Drop("fist_of_odin_knuckles", 3f), Drop("muspel_flamberge", 3f), Drop("valkyrian_armor", 2f), Drop("helm_of_awe", 1f),
                    Drop("ancient_golem_card", 0.5f),
                },
                Shape = MonsterShape.Golem, ColorHex = "#7F8C8D", Scale = 2.6f,
            });
        }

        // ================================================================ MVPs and summons
        private static void RegisterMvps()
        {
            Register(new MonsterDefinition
            {
                Id = "skoll", Name = "Sköll, Sun-Chaser", Level = 240, MaxHp = 600000, AtkMin = 4700, AtkMax = 5700, Def = 160, Mdef = 60,
                Hit = 480, Flee = 320, Element = Element.Fire, Race = Race.Beast, Size = Size.Large,
                BaseExp = 20000, JobExp = 16000, Aggressive = true, SummonOnly = true, MoveSpeed = 4.8f, AttackRange = 1.3f, AttackInterval = 1.1f,
                Skills =
                {
                    new MonsterSkill
                    {
                        Id = "sun_chaser_pounce", Name = "Sun-Chaser Pounce", Kind = MonsterSkillKind.Leap, MinRange = 3f, Range = 10f, Radius = 2f,
                        CastTime = 0.6f, Percent = 200f, Element = Element.Fire, Cooldown = 10f, ChancePercent = 40f,
                    },
                },
                Shape = MonsterShape.Quadruped, ColorHex = "#E67E22", Scale = 1.8f,
            });
            Register(new MonsterDefinition
            {
                Id = "hati", Name = "Hati, Moon-Hunter", Level = 240, MaxHp = 600000, AtkMin = 4700, AtkMax = 5700, Def = 160, Mdef = 60,
                Hit = 480, Flee = 320, Element = Element.Shadow, Race = Race.Beast, Size = Size.Large,
                BaseExp = 20000, JobExp = 16000, Aggressive = true, SummonOnly = true, MoveSpeed = 4.8f, AttackRange = 1.3f, AttackInterval = 1.1f,
                Skills =
                {
                    new MonsterSkill
                    {
                        Id = "moon_bite", Name = "Moon Bite", Kind = MonsterSkillKind.Strike, Percent = 220f, Element = Element.Shadow, Status = StatusEffect.Blind,
                        StatusChance = 40f, StatusSeconds = 4f, Cooldown = 9f, ChancePercent = 40f,
                    },
                },
                Shape = MonsterShape.Quadruped, ColorHex = "#212F3D", Scale = 1.8f,
            });

            Register(new MonsterDefinition
            {
                Id = "fenrir", Name = "Fenrir, the Bound Wolf", Rank = MonsterRank.Mvp, Level = 255, MaxHp = 8000000, AtkMin = 7200, AtkMax = 8800,
                Def = 260, Mdef = 120, Hit = 620, Flee = 420, Element = Element.Shadow, Race = Race.Beast, Size = Size.Large,
                BaseExp = 1800000, JobExp = 1500000, MvpExp = 600000, Aggressive = true, MoveSpeed = 5f, AttackRange = 2f, AttackInterval = 1f,
                Skills =
                {
                    Summon("call_skoll", "Call Sköll", "skoll", 1, 90f, 80f, 1, "Sköll! Hunt the sun!"),
                    Summon("call_hati", "Call Hati", "hati", 1, 90f, 80f, 1, "Hati! Devour the moon!"),
                    new MonsterSkill
                    {
                        Id = "ragnarok_howl", Name = "Ragnarök Howl", Kind = MonsterSkillKind.Area, CenteredOnSelf = true, Radius = 8f, CastTime = 2f, Magical = true,
                        Element = Element.Shadow, Percent = 200f, Status = StatusEffect.Stun, StatusChance = 30f, StatusSeconds = 2f, MinPhase = 1,
                        Cooldown = 20f, ChancePercent = 35f, Shout = "The end of all things begins with my howl!",
                    },
                    new MonsterSkill
                    {
                        Id = "lunar_step", Name = "Lunar Step", Kind = MonsterSkillKind.Teleport, BehindTarget = true, Range = 12f, MinPhase = 2, Cooldown = 15f,
                        ChancePercent = 30f,
                    },
                    new MonsterSkill
                    {
                        Id = "moon_eater_breath", Name = "Moon-Eater Breath", Kind = MonsterSkillKind.Area, Range = 10f, Radius = 5f, CastTime = 1.8f,
                        Magical = true, Element = Element.Shadow, Percent = 240f, Cooldown = 14f, ChancePercent = 35f,
                    },
                    new MonsterSkill
                    {
                        Id = "gleipnir_lunge", Name = "Gleipnir Lunge", Kind = MonsterSkillKind.Leap, MinRange = 3f, Range = 14f, Radius = 3f, CastTime = 0.9f,
                        Percent = 260f, Knockback = 3f, Cooldown = 10f, ChancePercent = 40f,
                    },
                    new MonsterSkill
                    {
                        Id = "devouring_bite", Name = "Devouring Bite", Kind = MonsterSkillKind.Strike, Percent = 350f, Status = StatusEffect.Bleeding,
                        StatusChance = 50f, StatusSeconds = 8f, BreakWeaponPercent = 2f, Cooldown = 9f, ChancePercent = 40f,
                    },
                },
                Phases =
                {
                    new BossPhase { BelowHpPercent = 70f, BuffId = MonsterBuffs.Enraged, Shout = "Gleipnir groans... the chains are slipping!" },
                    new BossPhase { BelowHpPercent = 35f, BuffId = MonsterBuffs.Unbound, Shout = "GLEIPNIR SNAPS! Fenrir is unbound!" },
                },
                MvpDrops = { Drop("gleipnir_thread", 50f), Drop("valkyrian_manteau", 30f), Drop("megingjard", 5f) },
                Drops =
                {
                    Drop(ItemCatalog.Starmetal, 60f), Drop(ItemCatalog.Skystone, 60f), Drop(ItemCatalog.RuneTiwaz, 50f), Drop("valkyrian_feather_wings", 5f),
                    Drop("fimbul_frost_wings", 5f), Drop("valkyrian_boots", 5f), Drop("raven_longbow", 5f), Drop("shadow_katar", 5f), Drop("fenrir_card", 0.01f),
                },
                Shape = MonsterShape.Quadruped, ColorHex = "#2C3E50", Scale = 3.2f,
            });
            Register(new MonsterDefinition
            {
                Id = "hels_vanguard", Name = "Hel's Vanguard", Rank = MonsterRank.Mvp, Level = 255, MaxHp = 7000000, AtkMin = 6800, AtkMax = 8300,
                Def = 340, Mdef = 160, Hit = 600, Flee = 330, Element = Element.Ghost, Race = Race.Undead, Size = Size.Large,
                BaseExp = 1700000, JobExp = 1400000, MvpExp = 550000, Aggressive = true, MoveSpeed = 3.2f, AttackRange = 2f, AttackInterval = 1.5f,
                Skills =
                {
                    new MonsterSkill
                    {
                        Id = "deaths_embrace", Name = "Death's Embrace", Kind = MonsterSkillKind.Heal, HealPercent = 5f, BelowHpPercent = 35f, MinPhase = 2,
                        CastTime = 2.5f, Cooldown = 45f, ChancePercent = 60f, Shout = "Hel, mend your champion!",
                    },
                    Summon("raise_the_dead", "Raise the Dead", "frozen_revenant", 2, 60f, minPhase: 1, shout: "Rise. Serve."),
                    new MonsterSkill
                    {
                        Id = "spectral_blink", Name = "Spectral Blink", Kind = MonsterSkillKind.Teleport, BehindTarget = true, Range = 12f, MinPhase = 1,
                        Cooldown = 20f, ChancePercent = 30f,
                    },
                    new MonsterSkill
                    {
                        Id = "niflheim_gate", Name = "Niflheim Gate", Kind = MonsterSkillKind.Area, CenteredOnSelf = true, Radius = 7f, CastTime = 2.2f,
                        Magical = true, Element = Element.Shadow, Percent = 260f, Status = StatusEffect.Curse, StatusChance = 40f, StatusSeconds = 8f,
                        Cooldown = 18f, ChancePercent = 35f,
                    },
                    new MonsterSkill
                    {
                        Id = "soul_chains", Name = "Soul Chains", Kind = MonsterSkillKind.Bolt, Range = 10f, MinRange = 2f, CastTime = 0.8f, Percent = 200f,
                        Status = StatusEffect.Root, StatusChance = 60f, StatusSeconds = 3f, Cooldown = 12f, ChancePercent = 35f,
                    },
                    new MonsterSkill
                    {
                        Id = "hels_judgment", Name = "Hel's Judgment", Kind = MonsterSkillKind.Strike, CastTime = 1f, Percent = 320f, BreakWeaponPercent = 3f,
                        Cooldown = 10f, ChancePercent = 40f, Shout = "Judgment!",
                    },
                },
                Phases =
                {
                    new BossPhase { BelowHpPercent = 70f, BuffId = MonsterBuffs.Enraged, Shout = "The Vanguard raises Hel's banner. The dead answer!" },
                    new BossPhase { BelowHpPercent = 35f, BuffId = MonsterBuffs.Unbound, Shout = "Niflheim's gate yawns open!" },
                },
                MvpDrops = { Drop("hel_soulfire", 50f), Drop("helm_of_awe", 10f), Drop("brisingamen", 5f) },
                Drops =
                {
                    Drop(ItemCatalog.Starmetal, 60f), Drop(ItemCatalog.Skystone, 60f), Drop("valkyrian_armor", 10f), Drop("valkyrian_shield", 10f),
                    Drop("demon_nether_wings", 5f), Drop("templar_mace", 5f), Drop("einherjar_greatsword", 5f), Drop("muspel_flamberge", 5f),
                    Drop("hels_vanguard_card", 0.01f),
                },
                Shape = MonsterShape.Humanoid, ColorHex = "#17202A", LookWeapon = WeaponType.TwoHandSword, LookHead = "valkyrie_winged_helm",
                LookGarment = "demon_nether_wings", SkinHex = "#5D6D7E", Scale = 2.6f,
            });
            Register(new MonsterDefinition
            {
                Id = "jormungandrs_brood", Name = "Jormungandr's Brood", Rank = MonsterRank.Mvp, Level = 255, MaxHp = 9000000, AtkMin = 7000, AtkMax = 8600,
                Def = 300, Mdef = 180, Hit = 620, Flee = 300, Element = Element.Water, Race = Race.Dragon, Size = Size.Large,
                BaseExp = 1900000, JobExp = 1600000, MvpExp = 650000, Aggressive = true, MoveSpeed = 3f, AttackRange = 2.2f, AttackInterval = 1.4f,
                Skills =
                {
                    new MonsterSkill
                    {
                        Id = "shed_skin", Name = "Shed Skin", Kind = MonsterSkillKind.Heal, HealPercent = 6f, BelowHpPercent = 35f, MinPhase = 2, CastTime = 2f,
                        Cooldown = 50f, ChancePercent = 60f,
                    },
                    Summon("abyssal_spawn", "Abyssal Spawn", "abyssal_leech", 3, 50f, minPhase: 1, shout: "The brood hungers..."),
                    new MonsterSkill
                    {
                        Id = "submerge", Name = "Submerge", Kind = MonsterSkillKind.Teleport, Range = 6f, BelowHpPercent = 70f, MinPhase = 1, Cooldown = 25f,
                        ChancePercent = 25f,
                    },
                    new MonsterSkill
                    {
                        Id = "frost_venom_storm", Name = "Frost-Venom Storm", Kind = MonsterSkillKind.Area, CenteredOnSelf = true, Radius = 9f, CastTime = 2.6f,
                        Magical = true, Element = Element.Water, Percent = 240f, Status = StatusEffect.Frostbite, StatusChance = 70f, StatusSeconds = 6f,
                        MinPhase = 2, Cooldown = 22f, ChancePercent = 40f, Shout = "Drown in the World Serpent's cold!",
                    },
                    new MonsterSkill
                    {
                        Id = "tidal_crush", Name = "Tidal Crush", Kind = MonsterSkillKind.Area, Range = 11f, Radius = 5f, CastTime = 2f, Magical = true,
                        Element = Element.Water, Percent = 280f, Knockback = 4f, Cooldown = 14f, ChancePercent = 35f,
                    },
                    new MonsterSkill
                    {
                        Id = "venom_spit", Name = "Venom Spit", Kind = MonsterSkillKind.Bolt, Range = 11f, MinRange = 2f, CastTime = 1f, Magical = true,
                        Element = Element.Poison, Percent = 220f, Status = StatusEffect.Poison, StatusChance = 60f, StatusSeconds = 10f,
                        Cooldown = 7f, ChancePercent = 35f,
                    },
                    new MonsterSkill
                    {
                        Id = "coil_constrict", Name = "Coil Constrict", Kind = MonsterSkillKind.Strike, Percent = 300f, Status = StatusEffect.Root,
                        StatusChance = 50f, StatusSeconds = 3f, Cooldown = 10f, ChancePercent = 35f,
                    },
                },
                Phases =
                {
                    new BossPhase { BelowHpPercent = 70f, BuffId = MonsterBuffs.Enraged, Shout = "The waters boil as the Brood thrashes!" },
                    new BossPhase { BelowHpPercent = 35f, BuffId = MonsterBuffs.Unbound, Shout = "The World Serpent's blood awakens!" },
                },
                MvpDrops = { Drop("world_serpent_scale", 50f), Drop("valkyrian_lance", 10f), Drop("mjolnir", 5f) },
                Drops =
                {
                    Drop(ItemCatalog.Starmetal, 60f), Drop(ItemCatalog.Skystone, 60f), Drop("dragon_flame_wings", 5f), Drop("fimbul_frost_wings", 5f),
                    Drop("norn_staff", 5f), Drop("yggdrasil_staff", 5f), Drop("fist_of_odin_knuckles", 5f), Drop("valkyrian_boots", 5f),
                    Drop("jormungandrs_brood_card", 0.01f),
                },
                Shape = MonsterShape.Serpent, ColorHex = "#117A65", Scale = 3f,
            });
        }
    }
}
