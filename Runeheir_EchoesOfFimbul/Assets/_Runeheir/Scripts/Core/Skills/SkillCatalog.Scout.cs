using Runeheir.Combat;
using Runeheir.Jobs;
using Runeheir.Stats;

namespace Runeheir.Skills
{
    /// <summary>
    /// Scout → Assassin → Shadow Walker; and the bow skills of Huntsman → Ranger → Deadeye (Phase 7 made the Huntsman, Ragnarok's
    /// Archer, the Ranger's first job). The Outlaw and the rest of the Huntsman line live in SkillCatalog.Roster and .Huntsman.
    /// </summary>
    public static partial class SkillCatalog
    {
        public const string KeenEdgeStrike = "keen_edge_strike";

        private static void RegisterScoutLine()
        {
            // ---------------------------------------------------------------- Scout
            Register(new SkillDefinition
            {
                Id = "twin_fang", Name = "Twin Fang", Job = JobId.Scout, MaxLevel = 10,
                Description = "Two quick stabs.",
                IconLabel = "TF", IconColorHex = "#1E8449",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Thrust,
                Range = 1.2f, Power = L(110f, 10f), Hits = 2, HitInterval = 0.12f, SpCost = 10,
            });
            Register(new SkillDefinition
            {
                Id = "envenom", Name = "Envenom", Job = JobId.Scout, MaxLevel = 10,
                Description = "A poisoned strike that can poison the target (Poison: lose HP over time, -25% DEF).",
                IconLabel = "ENV", IconColorHex = "#7D3C98",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Thrust,
                Element = Element.Poison, UseWeaponElement = false,
                Range = 1.2f, Power = L(100f, 15f), SpCost = 12,
                Status = StatusEffect.Poison, StatusChance = L(14f, 4f), StatusDuration = 10f,
            });
            Register(new SkillDefinition
            {
                Id = "dust_kick", Name = "Dust Kick", Job = JobId.Scout, MaxLevel = 5,
                Description = "Kick dirt in an enemy's eyes. Can blind (Blind: -25% HIT and FLEE).",
                IconLabel = "DST", IconColorHex = "#A04000",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Swing,
                Element = Element.Earth, UseWeaponElement = false,
                Range = 1.5f, Power = 125f, SpCost = 9,
                Status = StatusEffect.Blind, StatusChance = L(20f, 5f), StatusDuration = 8f,
            });
            Register(new SkillDefinition
            {
                Id = "pilfer", Name = "Pilfer", Job = JobId.Scout, MaxLevel = 10,
                Description = "Try to steal one of a monster's drops (once per monster). Better with DEX and against weaker monsters.",
                IconLabel = "PLF", IconColorHex = "#B7950B",
                Target = SkillTarget.Enemy, Special = SkillSpecial.Steal, Motion = SkillMotion.Thrust,
                Range = 1.2f, SpCost = 10, AfterCastDelay = 0.5f,
            });
            Register(new SkillDefinition
            {
                Id = "evasion_drills", Name = "Evasion Drills", Job = JobId.Scout, MaxLevel = 10, Passive = true,
                Description = "+3 FLEE per level.",
                IconLabel = "EVD", IconColorHex = "#52BE80",
                PassivePerLevel = new StatModifiers { Flee = 3 },
            });
            Register(new SkillDefinition
            {
                Id = "keen_edge", Name = "Keen Edge", Job = JobId.Scout, MaxLevel = 10, Passive = true,
                Description = "With daggers, each basic hit has a 5% chance per level to strike again.",
                IconLabel = "KEE", IconColorHex = "#28B463",
                PassiveWeapons = WeaponMask.Dagger,
                Proc = new ProcDefinition { SkillIds = new[] { KeenEdgeStrike }, Chance = L(5f, 5f) },
            });
            Register(new SkillDefinition
            {
                Id = KeenEdgeStrike, Name = "Keen Edge", Job = JobId.Scout, Hidden = true,
                IconLabel = "KEE", IconColorHex = "#28B463",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Power = 100f, CanCrit = true, Range = 2f,
            });

            // ---------------------------------------------------------------- Assassin
            Register(new SkillDefinition
            {
                Id = "katar_mastery", Name = "Katar Mastery", Job = JobId.Assassin, MaxLevel = 10, Passive = true,
                Description = "+3 ATK and +1 CRIT per level with katars.",
                IconLabel = "KTM", IconColorHex = "#1E3D2F",
                PassivePerLevel = new StatModifiers { Atk = 3, Crit = 1f }, PassiveWeapons = WeaponMask.Katar,
            });
            Register(new SkillDefinition
            {
                Id = "shadow_cloak", Name = "Shadow Cloak", Job = JobId.Assassin, MaxLevel = 10, Requires = Req("evasion_drills", 3),
                Description = "Vanish: monsters lose track of you and can't target you. You move slowly (less so at higher levels). Attacking or using a skill reveals you.",
                IconLabel = "SCL", IconColorHex = "#212F3D",
                Target = SkillTarget.Self, BuffId = SkillBuffs.ShadowCloak, Motion = SkillMotion.Buff,
                BuffDuration = L(10f, 2f), SpCost = 15, Cooldown = 1f,
            });
            Register(new SkillDefinition
            {
                Id = "venom_dust", Name = "Venom Dust", Job = JobId.Assassin, MaxLevel = 10, Requires = Req("envenom", 5),
                Description = "Leave a cloud of venom on the ground that poisons enemies standing in it.",
                IconLabel = "VDU", IconColorHex = "#6C3483",
                Target = SkillTarget.Ground, Special = SkillSpecial.Zone, Motion = SkillMotion.Cast,
                Range = 4f, Radius = 1.6f, CastTime = 0.5f, SpCost = 20,
                ZoneDuration = L(5f, 5f), ZoneTick = 1f,
                Status = StatusEffect.Poison, StatusChance = 100f, StatusDuration = L(10f, 2f),
            });
            Register(new SkillDefinition
            {
                Id = "underfang", Name = "Underfang", Job = JobId.Assassin, MaxLevel = 5, Requires = Req("shadow_cloak", 2),
                Description = "Strike from the shadows at everything around your target. Out of Shadow Veil it is a critical ambush.",
                IconLabel = "UFG", IconColorHex = "#17202A", Weapons = WeaponMask.Katar, CanCrit = true,
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Area = SkillArea.AroundTarget, Motion = SkillMotion.Thrust,
                Range = 2f, Radius = 1.5f, Power = L(250f, 50f), SpCost = 3,
            });
            Register(new SkillDefinition
            {
                Id = "lacerate", Name = "Lacerate", Job = JobId.Assassin, MaxLevel = 10, Requires = Req("katar_mastery", 3),
                Description = "Two tearing cuts that can cause Bleeding (HP loss over time, no natural regen).",
                IconLabel = "LAC", IconColorHex = "#922B21", Weapons = WeaponMask.Katar | WeaponMask.Dagger,
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Swing,
                Range = 1.2f, Power = L(100f, 15f), Hits = 2, HitInterval = 0.1f, SpCost = 12,
                Status = StatusEffect.Bleeding, StatusChance = L(5f, 3f), StatusDuration = 10f,
            });

            // ---------------------------------------------------------------- Shadow Walker (GDD signatures)
            Register(new SkillDefinition
            {
                Id = "phantom_barrage", Name = "Phantom Barrage", Job = JobId.ShadowWalker, MaxLevel = 10, Requires = Req("katar_mastery", 4),
                Description = "Rapid 8-hit strike with a guaranteed stun.",
                IconLabel = "PB", IconColorHex = "#5B2C6F", Weapons = WeaponMask.Katar,
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Thrust,
                Range = 1.2f, Power = L(65f, 5f), Hits = 8, HitInterval = 0.06f, SpCost = L(16f, 1f), AfterCastDelay = 1.2f,
                Status = StatusEffect.Stun, StatusChance = 100f, StatusDuration = 2f, GuaranteedStatus = true,
            });
            Register(new SkillDefinition
            {
                Id = "miasma_weapon", Name = "Miasma Weapon", Job = JobId.ShadowWalker, MaxLevel = 5, Requires = Req("venom_dust", 3),
                Description = "Poison your blades for 40 s: physical damage x2 at Lv 1, +50% per level (x4 at Lv 5).",
                IconLabel = "MIA", IconColorHex = "#6C3483",
                Target = SkillTarget.Self, BuffId = BuffCatalog.MiasmaWeapon, Motion = SkillMotion.Buff,
                SpCost = L(40f, 5f), Cooldown = 60f, AfterCastDelay = 0.5f,
            });
            Register(new SkillDefinition
            {
                Id = "shadow_veil", Name = "Shadow Veil", Job = JobId.ShadowWalker, MaxLevel = 10, Requires = Req("shadow_cloak", 5),
                Description = "Conceal yourself in darkness. Your first attack out of the veil is a guaranteed critical backstab.",
                IconLabel = "SHV", IconColorHex = "#1B2631",
                Target = SkillTarget.Self, BuffId = SkillBuffs.ShadowVeil, Motion = SkillMotion.Buff,
                BuffDuration = L(15f, 3f), SpCost = 25, Cooldown = 3f,
            });
            Register(new SkillDefinition
            {
                Id = "shadow_fang", Name = "Shadow Fang", Job = JobId.ShadowWalker, MaxLevel = 10, Requires = Req("phantom_barrage", 3),
                Description = "Hurl a blade of shadow at a distant enemy. Never misses.",
                IconLabel = "SFG", IconColorHex = "#4A235A",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Shoot, Projectile = true, NeverMiss = true,
                Range = 7f, Power = L(150f, 30f), CastTime = 0.5f, SpCost = L(15f, 2f), AfterCastDelay = 0.8f,
            });
            Register(new SkillDefinition
            {
                Id = "lethal_precision", Name = "Lethal Precision", Job = JobId.ShadowWalker, MaxLevel = 10, Passive = true,
                Description = "+1 CRIT per level with katars and daggers.",
                IconLabel = "LTP", IconColorHex = "#943126",
                PassivePerLevel = new StatModifiers { Crit = 1f }, PassiveWeapons = WeaponMask.Katar | WeaponMask.Dagger,
            });

            // ---------------------------------------------------------------- Huntsman (bow basics) and Ranger
            Register(new SkillDefinition
            {
                Id = "double_strafe", Name = "Double Strafe", Job = JobId.Huntsman, MaxLevel = 10,
                Description = "Loose two arrows at once.",
                IconLabel = "DS", IconColorHex = "#4A7A3A", Weapons = WeaponMask.Bow,
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Shoot, Projectile = true,
                Range = 9f, Power = L(100f, 10f), Hits = 2, HitInterval = 0.1f, SpCost = 12,
            });
            Register(new SkillDefinition
            {
                Id = "arrow_shower", Name = "Arrow Shower", Job = JobId.Huntsman, MaxLevel = 10, Requires = Req("double_strafe", 5),
                Description = "Rain arrows on an area, pushing enemies back.",
                IconLabel = "ASH", IconColorHex = "#58D68D", Weapons = WeaponMask.Bow,
                Target = SkillTarget.Ground, Damage = SkillDamage.Physical, Area = SkillArea.AtGround, Motion = SkillMotion.Shoot,
                Range = 9f, Radius = 1.8f, Power = L(80f, 5f), Knockback = 1.5f, SpCost = 15,
            });
            Register(new SkillDefinition
            {
                Id = "ankle_snare", Name = "Ankle Snare", Job = JobId.Ranger, MaxLevel = 5,
                Description = "Set a trap. The first enemy to step on it is snared in place (it can still attack).",
                IconLabel = "SNR", IconColorHex = "#6E2C00",
                Target = SkillTarget.Ground, Special = SkillSpecial.Zone, Motion = SkillMotion.Cast,
                Range = 4f, Radius = 1f, SpCost = 12, ZoneDuration = 60f, ZoneTrap = true,
                Status = StatusEffect.Root, StatusChance = 100f, StatusDuration = L(3f, 2f), GuaranteedStatus = true,
            });
            Register(new SkillDefinition
            {
                Id = "raven_strike", Name = "Raven Strike", Job = JobId.Ranger, MaxLevel = 5, Requires = Req("double_strafe", 3),
                Description = "Huginn dives at the target: one hit per level that ignores FLEE.",
                IconLabel = "RVS", IconColorHex = "#212F3D",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Magic, Motion = SkillMotion.Cast, Projectile = true,
                Range = 9f, Power = 80f, Hits = L(1f, 1f), HitInterval = 0.12f, CastTime = 1f, SpCost = L(10f, 3f),
            });
            Register(new SkillDefinition
            {
                Id = "owls_eye", Name = "Owl's Eye", Job = JobId.Huntsman, MaxLevel = 10, Passive = true,
                Description = "+1 DEX per level.",
                IconLabel = "OWL", IconColorHex = "#A9CCE3",
                PassivePerLevel = new StatModifiers().SetStat(StatType.Dex, 1),
            });
            Register(new SkillDefinition
            {
                Id = "vultures_eye", Name = "Vulture's Eye", Job = JobId.Huntsman, MaxLevel = 10, Passive = true,
                Description = "+1 HIT and +0.25 m bow range per level.",
                IconLabel = "VUL", IconColorHex = "#7FB3D5",
                PassivePerLevel = new StatModifiers { Hit = 1, AttackRange = 0.25f }, PassiveWeapons = WeaponMask.Bow,
            });

            // ---------------------------------------------------------------- Deadeye
            Register(new SkillDefinition
            {
                Id = "sharp_shot", Name = "Sharp Shot", Job = JobId.Deadeye, MaxLevel = 10, Requires = Req("double_strafe", 5, "vultures_eye", 5),
                Description = "A piercing arrow that hits every enemy in a line and can critically hit.",
                IconLabel = "SSH", IconColorHex = "#1D4D2B", Weapons = WeaponMask.Bow,
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Area = SkillArea.Line, Motion = SkillMotion.Shoot, CanCrit = true,
                Range = 10f, Power = L(150f, 50f), CastTime = 1.5f, SpCost = L(17f, 1f), AfterCastDelay = 1f,
            });
            Register(new SkillDefinition
            {
                Id = "raven_assault", Name = "Raven Assault", Job = JobId.Deadeye, MaxLevel = 5, Requires = Req("raven_strike", 5),
                Description = "Huginn and Muninn strike together: one huge hit that never misses.",
                IconLabel = "RVA", IconColorHex = "#17202A",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Cast, Projectile = true, NeverMiss = true,
                Range = 10f, Power = L(400f, 100f), CastTime = 1f, Cooldown = 5f, SpCost = L(30f, 4f),
            });
            Register(new SkillDefinition
            {
                Id = "true_sight", Name = "True Sight", Job = JobId.Deadeye, MaxLevel = 10,
                Description = "+5 all stats, +3 HIT and +1 CRIT per level.",
                IconLabel = "TRS", IconColorHex = "#F4D03F",
                Target = SkillTarget.Self, BuffId = SkillBuffs.TrueSight, Motion = SkillMotion.Buff,
                BuffDuration = L(60f, 15f), SpCost = L(20f, 2f),
            });
            Register(new SkillDefinition
            {
                Id = "gale_step", Name = "Gale Step", Job = JobId.Deadeye, MaxLevel = 10,
                Description = "Wind at your heels (or an ally's): +2% movement speed and +1 FLEE per level.",
                IconLabel = "GLS", IconColorHex = "#76D7C4",
                Target = SkillTarget.Friend, BuffId = SkillBuffs.GaleStep, Motion = SkillMotion.Cast,
                Range = 9f, BuffDuration = L(130f, 20f), CastTime = L(7f, -0.5f), SpCost = L(46f, 2f),
            });
        }
    }
}
