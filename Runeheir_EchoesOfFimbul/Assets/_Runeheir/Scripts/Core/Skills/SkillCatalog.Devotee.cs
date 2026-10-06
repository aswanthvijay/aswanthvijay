using Runeheir.Combat;
using Runeheir.Jobs;
using Runeheir.Stats;

namespace Runeheir.Skills
{
    /// <summary>Devotee → Paladin → Templar, Devotee → Monk → Champion.</summary>
    public static partial class SkillCatalog
    {
        public const string StormFistsCombo = "storm_fists_combo";

        private static void RegisterDevoteeLine()
        {
            // ---------------------------------------------------------------- Devotee
            Register(new SkillDefinition
            {
                Id = "eirs_blessing", Name = "Eir's Blessing", Job = JobId.Devotee, MaxLevel = 10,
                Description = "Heal yourself or an ally (Ragnarok Heal formula at this level).",
                IconLabel = "HL", IconColorHex = "#58D68D",
                Target = SkillTarget.Friend, Special = SkillSpecial.Heal, Motion = SkillMotion.Cast,
                Range = 9f, HealLevel = L(1f, 1f), SpCost = L(13f, 3f), AfterCastDelay = 1f,
            });
            Register(new SkillDefinition
            {
                Id = "divine_shelter", Name = "Divine Shelter", Job = JobId.Devotee, MaxLevel = 10, Passive = true,
                Description = "Take 1% less damage per level.",
                IconLabel = "DSH", IconColorHex = "#F9E79F",
                PassivePerLevel = new StatModifiers { DamageTakenPercent = -1f },
            });
            Register(new SkillDefinition
            {
                Id = "odins_blessing", Name = "Odin's Blessing", Job = JobId.Devotee, MaxLevel = 10, Requires = Req("divine_shelter", 5),
                Description = "+1 STR, INT and DEX per level for you or an ally.",
                IconLabel = "BLS", IconColorHex = "#F4D03F",
                Target = SkillTarget.Friend, BuffId = SkillBuffs.OdinsBlessing, Motion = SkillMotion.Cast,
                Range = 9f, BuffDuration = L(60f, 20f), SpCost = L(28f, 4f),
            });
            Register(new SkillDefinition
            {
                Id = "swift_wind", Name = "Swift Wind", Job = JobId.Devotee, MaxLevel = 10, Requires = Req("eirs_blessing", 3),
                Description = "+3 AGI (+1 per level) and +25% movement speed for you or an ally.",
                IconLabel = "INC", IconColorHex = "#76D7C4",
                Target = SkillTarget.Friend, BuffId = SkillBuffs.SwiftWind, Motion = SkillMotion.Cast,
                Range = 9f, BuffDuration = L(60f, 20f), SpCost = L(18f, 3f),
            });
            Register(new SkillDefinition
            {
                Id = "holy_light", Name = "Holy Light", Job = JobId.Devotee, MaxLevel = 5,
                Description = "A beam of holy light. +50% damage to Undead and Demons.",
                IconLabel = "HLT", IconColorHex = "#FCF3CF",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Magic, Motion = SkillMotion.Cast, Projectile = true,
                Element = Element.Holy, UseWeaponElement = false, BonusVsUndeadPercent = 50f,
                Range = 9f, Power = L(125f, 25f), CastTime = 1.5f, SpCost = 15,
            });
            Register(new SkillDefinition
            {
                Id = "purify", Name = "Purify", Job = JobId.Devotee, Requires = Req("eirs_blessing", 2),
                Description = "Cure yourself or an ally of every negative status and debuff.",
                IconLabel = "CUR", IconColorHex = "#D5F5E3",
                Target = SkillTarget.Friend, Special = SkillSpecial.Cleanse, Motion = SkillMotion.Cast,
                Range = 9f, SpCost = 15,
            });

            // ---------------------------------------------------------------- Paladin
            Register(new SkillDefinition
            {
                Id = "faith", Name = "Faith", Job = JobId.Paladin, MaxLevel = 10, Passive = true,
                Description = "+200 Max HP per level.",
                IconLabel = "FTH", IconColorHex = "#F7DC6F",
                PassivePerLevel = new StatModifiers { MaxHp = 200 },
            });
            Register(new SkillDefinition
            {
                Id = "sacred_cross", Name = "Sacred Cross", Job = JobId.Paladin, MaxLevel = 10, Requires = Req("faith", 5),
                Description = "Two holy cross-shaped strikes. Can blind.",
                IconLabel = "SCR", IconColorHex = "#D4AC0D",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Swing,
                Element = Element.Holy, UseWeaponElement = false, BonusVsUndeadPercent = 25f,
                Range = 1.3f, Power = L(135f, 35f), Hits = 2, HitInterval = 0.12f, SpCost = L(11f, 1f),
                Status = StatusEffect.Blind, StatusChance = L(3f, 3f), StatusDuration = 6f,
            });
            Register(new SkillDefinition
            {
                Id = "radiant_cross", Name = "Radiant Cross", Job = JobId.Paladin, MaxLevel = 10, Requires = Req("sacred_cross", 6),
                Description = "Sacrifice 20% of your HP: a great holy cross erupts around you, three times. +50% vs Undead and Demons.",
                IconLabel = "GCR", IconColorHex = "#FDEBD0",
                Target = SkillTarget.Self, Damage = SkillDamage.Magic, Area = SkillArea.AroundSelf, Motion = SkillMotion.Cast,
                Element = Element.Holy, UseWeaponElement = false, BonusVsUndeadPercent = 50f,
                Radius = 2.5f, Power = L(100f, 20f), Hits = 3, HitInterval = 0.25f, HpCostPercent = 20f,
                CastTime = 2f, SpCost = L(37f, 3f), AfterCastDelay = 1.5f,
                Status = StatusEffect.Blind, StatusChance = 10f, StatusDuration = 5f,
            });
            Register(new SkillDefinition
            {
                Id = "valor_aura", Name = "Valor Aura", Job = JobId.Paladin, MaxLevel = 10, Requires = Req("faith", 3),
                Description = "+5 ATK and 1% less damage taken per level.",
                IconLabel = "VAL", IconColorHex = "#E59866",
                Target = SkillTarget.Self, BuffId = SkillBuffs.ValorAura, Motion = SkillMotion.Buff,
                BuffDuration = 180f, SpCost = L(30f, 3f),
            });
            Register(new SkillDefinition
            {
                Id = "judgment", Name = "Judgment", Job = JobId.Paladin, MaxLevel = 5, Requires = Req("sacred_cross", 3),
                Description = "Bring the mace down on your target and everyone beside it. Often stuns.",
                IconLabel = "JDG", IconColorHex = "#B7950B", Weapons = WeaponMask.Mace,
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Area = SkillArea.AroundTarget, Motion = SkillMotion.Swing,
                Range = 1.3f, Radius = 1.8f, Power = L(200f, 40f), SpCost = 20, Cooldown = 3f, PoiseMultiplier = 1.8f,
                Status = StatusEffect.Stun, StatusChance = L(20f, 10f), StatusDuration = 2.5f,
            });

            // ---------------------------------------------------------------- Templar
            Register(new SkillDefinition
            {
                Id = "sanctuary", Name = "Sanctuary", Job = JobId.Templar, MaxLevel = 10, Requires = Req("eirs_blessing", 3),
                Description = "Consecrate the ground: allies standing in it heal every second.",
                IconLabel = "SNC", IconColorHex = "#ABEBC6",
                Target = SkillTarget.Ground, Special = SkillSpecial.Zone, Motion = SkillMotion.Cast, ZoneAffectsAllies = true,
                Range = 9f, Radius = 2.5f, CastTime = L(5f, -0.2f), SpCost = L(15f, 3f),
                ZoneDuration = L(4f, 1f), ZoneTick = 1f, FlatHeal = L(80f, 60f),
            });
            Register(new SkillDefinition
            {
                Id = "holy_judgment", Name = "Holy Judgment", Job = JobId.Templar, MaxLevel = 10, Requires = Req("holy_light", 3),
                Description = "Waves of holy fire purge an area. Double damage to Undead and Demons.",
                IconLabel = "HJD", IconColorHex = "#FEF9E7",
                Target = SkillTarget.Ground, Damage = SkillDamage.Magic, Area = SkillArea.AtGround, Motion = SkillMotion.Cast,
                Element = Element.Holy, UseWeaponElement = false, BonusVsUndeadPercent = 100f,
                Range = 9f, Radius = 3f, Power = 100f, Hits = T(2, 2, 3, 3, 4, 4, 5, 5, 6, 6), HitInterval = 0.6f,
                CastTime = L(8f, -0.4f), SpCost = L(40f, 2f), AfterCastDelay = 3f,
            });
            Register(new SkillDefinition
            {
                Id = "divine_bulwark", Name = "Divine Bulwark", Job = JobId.Templar, MaxLevel = 5, Requires = Req("divine_shelter", 5),
                Description = "You or an ally take 10% less damage per level (half at Lv 5).",
                IconLabel = "ASM", IconColorHex = "#FAD7A0",
                Target = SkillTarget.Friend, BuffId = SkillBuffs.DivineBulwark, Motion = SkillMotion.Cast,
                Range = 9f, BuffDuration = L(20f, 20f), CastTime = 1f, SpCost = L(20f, 10f),
            });
            Register(new SkillDefinition
            {
                Id = "hammer_of_tyr", Name = "Hammer of Tyr", Job = JobId.Templar, MaxLevel = 5, Requires = Req("judgment", 3),
                Description = "Smash the ground: damage and a strong chance to stun everything in the area.",
                IconLabel = "HOT", IconColorHex = "#F5CBA7", Weapons = WeaponMask.Mace,
                Target = SkillTarget.Ground, Damage = SkillDamage.Physical, Area = SkillArea.AtGround, Motion = SkillMotion.Swing,
                Range = 3f, Radius = L(1.5f, 0.25f), Power = 120f, SpCost = 10, PoiseMultiplier = 2f,
                Status = StatusEffect.Stun, StatusChance = L(30f, 10f), StatusDuration = 3f,
            });

            // ---------------------------------------------------------------- Monk
            Register(new SkillDefinition
            {
                Id = "iron_fist", Name = "Iron Fist", Job = JobId.Monk, MaxLevel = 10, Passive = true,
                Description = "+3 ATK per level with knuckles or bare hands.",
                IconLabel = "IFS", IconColorHex = "#B9770E",
                PassivePerLevel = new StatModifiers { Atk = 3 }, PassiveWeapons = WeaponMask.Knuckle | WeaponMask.Unarmed,
            });
            Register(new SkillDefinition
            {
                Id = "storm_fists", Name = "Storm Fists", Job = JobId.Monk, MaxLevel = 10, Passive = true,
                Description = "Basic hits can burst into a three-hit combo (higher levels: stronger combo, slightly lower chance).",
                IconLabel = "SFS", IconColorHex = "#CA6F1E",
                PassiveWeapons = WeaponMask.Knuckle | WeaponMask.Unarmed,
                Proc = new ProcDefinition { SkillIds = new[] { StormFistsCombo }, Chance = L(29f, -1f), ProcLevel = L(1f, 1f) },
            });
            Register(new SkillDefinition
            {
                Id = StormFistsCombo, Name = "Storm Fists", Job = JobId.Monk, Hidden = true, MaxLevel = 10,
                IconLabel = "SFS", IconColorHex = "#CA6F1E",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Punch,
                Range = 2f, Power = L(40f, 7f), Hits = 3, HitInterval = 0.07f,
            });
            Register(new SkillDefinition
            {
                Id = "spirit_call", Name = "Spirit Call", Job = JobId.Monk, MaxLevel = 5,
                Description = "Summon one Spirit Sphere (+3 ATK each), up to one per level. Spheres fuel Occult Strike, Spirit Barrage and Thunder Palm.",
                IconLabel = "SPH", IconColorHex = "#F8C471",
                Target = SkillTarget.Self, BuffId = SkillBuffs.SpiritSpheres, Motion = SkillMotion.Buff,
                CastTime = 1f, SpCost = 8, AfterCastDelay = 0.5f,
            });
            Register(new SkillDefinition
            {
                Id = "occult_strike", Name = "Occult Strike", Job = JobId.Monk, MaxLevel = 5, Requires = Req("iron_fist", 5),
                Description = "A palm strike through armour: ignores DEF and never misses. Uses 1 Spirit Sphere.",
                IconLabel = "OCS", IconColorHex = "#AF601A",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Punch, IgnoreDefense = true, NeverMiss = true,
                Range = 1.2f, Power = L(175f, 75f), SphereCost = 1, SpCost = L(10f, 4f), PoiseMultiplier = 2f,
            });
            Register(new SkillDefinition
            {
                Id = "spirit_barrage", Name = "Spirit Barrage", Job = JobId.Monk, MaxLevel = 5, Requires = Req("spirit_call", 3),
                Description = "Hurl your Spirit Spheres: one hit per sphere, up to the skill level.",
                IconLabel = "FOF", IconColorHex = "#F5B041",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Special = SkillSpecial.SpiritRelease, Motion = SkillMotion.Shoot,
                Projectile = true, Range = 7f, Power = L(150f, 50f), HitInterval = 0.1f, CastTime = 1f, SpCost = 10,
            });
            Register(new SkillDefinition
            {
                Id = "diamond_skin", Name = "Diamond Skin", Job = JobId.Monk, MaxLevel = 5, Requires = Req("occult_strike", 3),
                Description = "Harden your body: huge DEF (+100 per level) and MDEF, but -25% movement speed and ASPD. Uses 3 Spirit Spheres.",
                IconLabel = "DMS", IconColorHex = "#D5D8DC",
                Target = SkillTarget.Self, BuffId = SkillBuffs.DiamondSkin, Motion = SkillMotion.Buff,
                BuffDuration = L(30f, 30f), SphereCost = 3, SpCost = 60, Cooldown = 10f,
            });

            // ---------------------------------------------------------------- Champion (GDD signatures)
            Register(new SkillDefinition
            {
                Id = "fist_of_odin", Name = "Fist of Odin", Job = JobId.Champion, MaxLevel = 5, Requires = Req("occult_strike", 3),
                Description = "Drains ALL SP into one lethal, unmissable punch: ATK x (8 + SP/10) plus a flat bonus (+1750 at Lv 5).",
                IconLabel = "FOO", IconColorHex = "#F39C12",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Special = SkillSpecial.FistOfOdin, Motion = SkillMotion.Punch, NeverMiss = true,
                Range = 1.2f, FlatDamage = L(1150f, 150f), SpCost = 10, CastTime = L(4f, -0.75f), Cooldown = 5f, AfterCastDelay = 2f, PoiseMultiplier = 5f,
            });
            Register(new SkillDefinition
            {
                Id = "aether_snap", Name = "Aether Snap", Job = JobId.Champion, MaxLevel = 5, Requires = Req("spirit_call", 2),
                Description = "Instant-transmission dash (8 m at Lv 5).",
                IconLabel = "AS", IconColorHex = "#48C9B0",
                Target = SkillTarget.Ground, Special = SkillSpecial.Dash, Motion = SkillMotion.Leap,
                Range = L(4f, 1f), SpCost = L(14f, -0.5f), Cooldown = L(2.5f, -0.25f), AfterCastDelay = 0.2f,
            });
            Register(new SkillDefinition
            {
                Id = "thunder_palm", Name = "Thunder Palm", Job = JobId.Champion, MaxLevel = 10, Requires = Req("storm_fists", 5),
                Description = "A four-strike palm combo. Uses 1 Spirit Sphere.",
                IconLabel = "THP", IconColorHex = "#F7DC6F", Weapons = WeaponMask.Knuckle | WeaponMask.Unarmed,
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Punch,
                Range = 1.2f, Power = L(80f, 15f), Hits = 4, HitInterval = 0.08f, SphereCost = 1, SpCost = L(11f, 1f),
            });
            Register(new SkillDefinition
            {
                Id = "zen_breath", Name = "Zen Breath", Job = JobId.Champion, MaxLevel = 5, Requires = Req("spirit_call", 5),
                Description = "Meditate: +10 SP regen and +2% Max SP per level.",
                IconLabel = "ZEN", IconColorHex = "#A3E4D7",
                Target = SkillTarget.Self, BuffId = SkillBuffs.ZenBreath, Motion = SkillMotion.Buff,
                BuffDuration = 60f, SpCost = 20, Cooldown = 30f,
            });
        }
    }
}
