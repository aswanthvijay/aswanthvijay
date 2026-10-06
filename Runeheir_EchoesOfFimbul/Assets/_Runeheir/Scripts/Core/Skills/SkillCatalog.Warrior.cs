using Runeheir.Combat;
using Runeheir.Jobs;
using Runeheir.Stats;

namespace Runeheir.Skills
{
    /// <summary>Warrior → Berserker → Einherjar, Warrior → Guardian → Valkyrie.</summary>
    public static partial class SkillCatalog
    {
        private static void RegisterWarriorLine()
        {
            // ---------------------------------------------------------------- Warrior
            Register(new SkillDefinition
            {
                Id = "bash", Name = "Bash", Job = JobId.Warrior, MaxLevel = 10,
                Description = "A heavy blow. From Lv 6 it can stun.",
                IconLabel = "BSH", IconColorHex = "#A93226",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Swing,
                Range = 1.4f, Power = L(130f, 30f), SpCost = T(8, 8, 8, 8, 8, 15, 15, 15, 15, 15), PoiseMultiplier = L(1.2f, 0.08f),
                Status = StatusEffect.Stun, StatusChance = T(0, 0, 0, 0, 0, 5, 10, 15, 20, 25), StatusDuration = 2f,
            });
            Register(new SkillDefinition
            {
                Id = "magnum_break", Name = "Magnum Break", Job = JobId.Warrior, MaxLevel = 10, Requires = Req("bash", 5),
                Description = "A fiery shockwave around you that knocks enemies back.",
                IconLabel = "MGB", IconColorHex = "#E74C3C",
                Target = SkillTarget.Self, Damage = SkillDamage.Physical, Area = SkillArea.AroundSelf, Motion = SkillMotion.Spin,
                Element = Element.Fire, UseWeaponElement = false,
                Power = L(120f, 20f), Radius = 2.5f, Knockback = 1.5f, SpCost = 30, AfterCastDelay = 1f,
            });
            Register(new SkillDefinition
            {
                Id = "provoke", Name = "Provoke", Job = JobId.Warrior, MaxLevel = 10,
                Description = "Enrage an enemy: it hits harder but its DEF drops, and it comes after you.",
                IconLabel = "PRV", IconColorHex = "#922B21",
                Target = SkillTarget.Enemy, Special = SkillSpecial.Provoke, Motion = SkillMotion.Buff,
                Range = 9f, SpCost = L(4f, 1f), DebuffId = SkillBuffs.Provoked, DebuffDuration = 30f, NeverMiss = true,
            });
            Register(new SkillDefinition
            {
                Id = "endure", Name = "Endure", Job = JobId.Warrior, MaxLevel = 10, Requires = Req("provoke", 5),
                Description = "Grit your teeth: you can't be staggered, and MDEF rises.",
                IconLabel = "END", IconColorHex = "#B03A2E",
                Target = SkillTarget.Self, BuffId = SkillBuffs.Endure, Motion = SkillMotion.Buff,
                BuffDuration = L(10f, 3f), SpCost = 10, Cooldown = 10f,
            });
            Register(new SkillDefinition
            {
                Id = "sword_mastery", Name = "Sword Mastery", Job = JobId.Warrior, MaxLevel = 10, Passive = true,
                Description = "+4 ATK per level with swords and greatswords.",
                IconLabel = "SWM", IconColorHex = "#7B241C",
                PassivePerLevel = new StatModifiers { Atk = 4 }, PassiveWeapons = WeaponMask.Swords,
            });
            Register(new SkillDefinition
            {
                Id = "iron_constitution", Name = "Iron Constitution", Job = JobId.Warrior, MaxLevel = 10, Passive = true,
                Description = "+3 HP regen and +0.5% Max HP per level.",
                IconLabel = "IRC", IconColorHex = "#CB4335",
                PassivePerLevel = new StatModifiers { HpRegenFlat = 3, MaxHpPercent = 0.5f },
            });

            // ---------------------------------------------------------------- Berserker
            Register(new SkillDefinition
            {
                Id = "battle_frenzy", Name = "Battle Frenzy", Job = JobId.Berserker, MaxLevel = 10, Requires = Req("sword_mastery", 1),
                Description = "Greatsword fury: +3% ASPD and +2 HIT per level.",
                IconLabel = "BFZ", IconColorHex = "#D35400", Weapons = WeaponMask.TwoHandSword,
                Target = SkillTarget.Self, BuffId = SkillBuffs.BattleFrenzy, Motion = SkillMotion.Buff,
                BuffDuration = L(30f, 30f), SpCost = L(14f, 4f),
            });
            Register(new SkillDefinition
            {
                Id = "cleaving_strike", Name = "Cleaving Strike", Job = JobId.Berserker, MaxLevel = 10, Requires = Req("bash", 5),
                Description = "A wide cut that also hits everyone next to your target.",
                IconLabel = "CLV", IconColorHex = "#A04000",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Area = SkillArea.AroundTarget, Motion = SkillMotion.Swing,
                Range = 1.4f, Radius = 1.5f, Power = L(150f, 25f), SpCost = L(10f, 1f), AfterCastDelay = 0.5f, PoiseMultiplier = 1.5f,
            });
            Register(new SkillDefinition
            {
                Id = "war_cry", Name = "War Cry", Job = JobId.Berserker, MaxLevel = 5, Requires = Req("provoke", 3),
                Description = "A terrifying roar: nearby enemies lose DEF and turn on you.",
                IconLabel = "WCY", IconColorHex = "#943126",
                Target = SkillTarget.Self, Area = SkillArea.AroundSelf, Special = SkillSpecial.Provoke, Motion = SkillMotion.Buff,
                Radius = 4f, DebuffId = SkillBuffs.Terrified, DebuffDuration = 20f, SpCost = 20, Cooldown = 8f, NeverMiss = true,
            });
            Register(new SkillDefinition
            {
                Id = "blood_rush", Name = "Blood Rush", Job = JobId.Berserker, MaxLevel = 5, Requires = Req("cleaving_strike", 3),
                Description = "Charge at an enemy and slam into it. Can stun.",
                IconLabel = "BRS", IconColorHex = "#78281F",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Special = SkillSpecial.Dash, Motion = SkillMotion.Leap,
                Range = L(5f, 1f), Power = L(200f, 50f), Knockback = 1f, SpCost = 18, Cooldown = 4f, PoiseMultiplier = 2f,
                Status = StatusEffect.Stun, StatusChance = L(10f, 5f), StatusDuration = 1.5f,
            });
            Register(new SkillDefinition
            {
                Id = "feral_resilience", Name = "Feral Resilience", Job = JobId.Berserker, MaxLevel = 10, Passive = true,
                Description = "+2% Max HP and +5% poise per level.",
                IconLabel = "FRL", IconColorHex = "#6E2C00",
                PassivePerLevel = new StatModifiers { MaxHpPercent = 2f, MaxPoisePercent = 5f },
            });
            Register(new SkillDefinition
            {
                Id = "reckless_edge", Name = "Reckless Edge", Job = JobId.Berserker, MaxLevel = 10, Passive = true,
                Description = "+1% physical damage and +1 CRIT per level with greatswords.",
                IconLabel = "RKE", IconColorHex = "#B9770E",
                PassivePerLevel = new StatModifiers { PhysicalDamagePercent = 1f, Crit = 1f }, PassiveWeapons = WeaponMask.TwoHandSword,
            });

            // ---------------------------------------------------------------- Einherjar (GDD signatures)
            Register(new SkillDefinition
            {
                Id = "vortex_cleave", Name = "Vortex Cleave", Job = JobId.Einherjar, MaxLevel = 10, Requires = Req("bash", 10, "magnum_break", 3),
                Description = "360° greatsword sweep: two hits that launch enemies. Each launched enemy that lands on others crashes into them (half power, at most once per enemy per cast).",
                IconLabel = "VC", IconColorHex = "#CB4335",
                Target = SkillTarget.Self, Damage = SkillDamage.Physical, Area = SkillArea.AroundSelf, Motion = SkillMotion.Spin,
                Radius = 3f, Power = L(130f, 30f), Hits = 2, HitInterval = 0.15f, Knockback = 2.5f, ChainImpacts = true,
                SpCost = L(11f, 1f), CastTime = 0.6f, AfterCastDelay = 0.6f, PoiseMultiplier = 1.5f,
            });
            Register(new SkillDefinition
            {
                Id = "two_hand_surge", Name = "Two-Hand Surge", Job = JobId.Einherjar, MaxLevel = 10, Requires = Req("battle_frenzy", 5),
                Description = "+0.7 ASPD per level (+7 at Lv 10) and attack recovery canceling.",
                IconLabel = "THS", IconColorHex = "#D35400", Weapons = WeaponMask.TwoHandSword,
                Target = SkillTarget.Self, BuffId = BuffCatalog.TwoHandSurge, Motion = SkillMotion.Buff,
                BuffDuration = L(33f, 3f), SpCost = L(10f, 0.5f),
            });
            Register(new SkillDefinition
            {
                Id = "rage_of_thor", Name = "Rage of Thor", Job = JobId.Einherjar, MaxLevel = 5, Requires = Req("two_hand_surge", 5),
                Description = "Max HP x3, ASPD 195, hyper-armor (no flinch, knockback or stagger); items locked. Lasts 10 s + 5 s per level.",
                IconLabel = "ROT", IconColorHex = "#C0392B",
                Target = SkillTarget.Self, BuffId = BuffCatalog.RageOfThor, Motion = SkillMotion.Buff,
                BuffDuration = L(10f, 5f), SpCost = 60, Cooldown = 120f, AfterCastDelay = 0.5f,
            });
            Register(new SkillDefinition
            {
                Id = "valhallan_might", Name = "Valhallan Might", Job = JobId.Einherjar, MaxLevel = 10, Passive = true,
                Description = "+2% physical damage per level.",
                IconLabel = "VHM", IconColorHex = "#F5B041",
                PassivePerLevel = new StatModifiers { PhysicalDamagePercent = 2f },
            });

            // ---------------------------------------------------------------- Guardian
            Register(new SkillDefinition
            {
                Id = "spear_mastery", Name = "Spear Mastery", Job = JobId.Guardian, MaxLevel = 10, Passive = true,
                Description = "+4 ATK per level with spears.",
                IconLabel = "SPM", IconColorHex = "#5D6D7E",
                PassivePerLevel = new StatModifiers { Atk = 4 }, PassiveWeapons = WeaponMask.Spear,
            });
            Register(new SkillDefinition
            {
                Id = "spear_stab", Name = "Spear Stab", Job = JobId.Guardian, MaxLevel = 10, Requires = Req("spear_mastery", 3),
                Description = "Thrust through every enemy in a line and push them back.",
                IconLabel = "SPS", IconColorHex = "#566573", Weapons = WeaponMask.Spear,
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Area = SkillArea.Line, Motion = SkillMotion.Thrust,
                Range = 4f, Power = L(120f, 20f), Knockback = 2f, SpCost = 9,
            });
            Register(new SkillDefinition
            {
                Id = "brandish_spear", Name = "Brandish Spear", Job = JobId.Guardian, MaxLevel = 10, Requires = Req("spear_stab", 3),
                Description = "Sweep the spear around your target, hitting everything near it.",
                IconLabel = "BRD", IconColorHex = "#717D7E", Weapons = WeaponMask.Spear,
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Area = SkillArea.AroundTarget, Motion = SkillMotion.Spin,
                Range = 2.2f, Radius = L(1.5f, 0.15f), Power = L(140f, 20f), CastTime = 0.7f, SpCost = 12, PoiseMultiplier = 1.3f,
            });
            Register(new SkillDefinition
            {
                Id = "guardians_oath", Name = "Guardian's Oath", Job = JobId.Guardian, MaxLevel = 10,
                Description = "Raise your guard: +3% chance per level to block physical melee hits, but walk 10% slower.",
                IconLabel = "GOA", IconColorHex = "#AAB7B8",
                Target = SkillTarget.Self, BuffId = SkillBuffs.GuardiansOath, Motion = SkillMotion.Buff,
                BuffDuration = 300f, SpCost = L(12f, 2f),
            });
            Register(new SkillDefinition
            {
                Id = "holdfast", Name = "Holdfast", Job = JobId.Guardian, MaxLevel = 5, Requires = Req("guardians_oath", 5),
                Description = "Plant your feet: take 4% less damage per level and can't be staggered, but walk 30% slower.",
                IconLabel = "HLD", IconColorHex = "#839192",
                Target = SkillTarget.Self, BuffId = SkillBuffs.Holdfast, Motion = SkillMotion.Buff,
                BuffDuration = 15f, SpCost = 25, Cooldown = 30f,
            });
            Register(new SkillDefinition
            {
                Id = "stalwart", Name = "Stalwart", Job = JobId.Guardian, MaxLevel = 10, Passive = true,
                Description = "+1% Max HP and +5% poise per level.",
                IconLabel = "STW", IconColorHex = "#4D5656",
                PassivePerLevel = new StatModifiers { MaxHpPercent = 1f, MaxPoisePercent = 5f },
            });

            // ---------------------------------------------------------------- Valkyrie
            Register(new SkillDefinition
            {
                Id = "valkyries_descent", Name = "Valkyrie's Descent", Job = JobId.Valkyrie, MaxLevel = 10, Requires = Req("brandish_spear", 5),
                Description = "Leap to a spot and crash down, hitting and knocking back everyone around you.",
                IconLabel = "VDS", IconColorHex = "#D6DBDF",
                Target = SkillTarget.Ground, Damage = SkillDamage.Physical, Area = SkillArea.AroundSelf, Special = SkillSpecial.Dash, Motion = SkillMotion.Leap,
                Range = L(5f, 0.5f), Radius = 2f, Power = L(150f, 30f), Knockback = 1.5f, SpCost = L(20f, 2f), Cooldown = 6f, PoiseMultiplier = 2f,
            });
            Register(new SkillDefinition
            {
                Id = "spiral_lance", Name = "Spiral Lance", Job = JobId.Valkyrie, MaxLevel = 10, Requires = Req("spear_stab", 5),
                Description = "Five spinning lance strikes on one enemy.",
                IconLabel = "SPL", IconColorHex = "#BFC9CA", Weapons = WeaponMask.Spear,
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Thrust,
                Range = 4f, Power = L(60f, 12f), Hits = 5, HitInterval = 0.08f, CastTime = L(1f, -0.05f), SpCost = L(18f, 2f), Cooldown = 1f,
            });
            Register(new SkillDefinition
            {
                Id = "freyjas_shield", Name = "Freyja's Shield", Job = JobId.Valkyrie, MaxLevel = 10, Requires = Req("guardians_oath", 5),
                Description = "Shield yourself or an ally: less damage taken and more poise.",
                IconLabel = "FRS", IconColorHex = "#F7DC6F",
                Target = SkillTarget.Friend, BuffId = SkillBuffs.FreyjasShield, Motion = SkillMotion.Cast,
                Range = 9f, BuffDuration = 120f, SpCost = L(30f, 2f),
            });
            Register(new SkillDefinition
            {
                Id = "valkyrie_grace", Name = "Valkyrie Grace", Job = JobId.Valkyrie, MaxLevel = 10, Passive = true,
                Description = "+1% Max HP, +5% poise and +1% movement speed per level.",
                IconLabel = "VGR", IconColorHex = "#EAEDED",
                PassivePerLevel = new StatModifiers { MaxHpPercent = 1f, MaxPoisePercent = 5f, MoveSpeedPercent = 1f },
            });
        }
    }
}
