using Runeheir.Combat;
using Runeheir.Jobs;
using Runeheir.Stats;

namespace Runeheir.Skills
{
    /// <summary>Mystic → Sorcerer → Archmage, Mystic → Sage → Chronomancer.</summary>
    public static partial class SkillCatalog
    {
        private static void RegisterMysticLine()
        {
            // ---------------------------------------------------------------- Mystic: three bolts, one per element
            RegisterBolt("muspel_bolt", "Muspel Bolt", "MB", "#E67E22", Element.Fire, "Muspelheim fire");
            RegisterBolt("frost_spike", "Frost Spike", "FS", "#5DADE2", Element.Water, "Niflheim ice");
            RegisterBolt("thunder_rune", "Thunder Rune", "TR", "#F4D03F", Element.Wind, "Thor's lightning");
            Register(new SkillDefinition
            {
                Id = "soul_lance", Name = "Soul Lance", Job = JobId.Mystic, MaxLevel = 10,
                Description = "Ghostly lances: one more every two levels. Ghost element, quick to cast.",
                IconLabel = "SL", IconColorHex = "#D7BDE2",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Magic, Motion = SkillMotion.Cast, Projectile = true,
                Element = Element.Ghost, UseWeaponElement = false,
                Range = 9f, Power = 100f, Hits = T(1, 1, 2, 2, 3, 3, 4, 4, 5, 5), HitInterval = 0.12f,
                CastTime = 0.5f, SpCost = L(7f, 2f), AfterCastDelay = 0.8f, PoiseMultiplier = 0.6f,
            });
            Register(new SkillDefinition
            {
                Id = "frost_lock", Name = "Frost Lock", Job = JobId.Mystic, MaxLevel = 10, Requires = Req("frost_spike", 1),
                Description = "Encase an enemy in ice (Frozen: can't act; blunt weapons deal triple damage to it).",
                IconLabel = "FL", IconColorHex = "#AED6F1",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Magic, Motion = SkillMotion.Cast, Projectile = true,
                Element = Element.Water, UseWeaponElement = false,
                Range = 9f, Power = L(110f, 10f), CastTime = 0.8f, SpCost = L(25f, -1f), AfterCastDelay = 1.5f,
                Status = StatusEffect.Freeze, StatusChance = L(38f, 3f), StatusDuration = L(3f, 0.5f),
            });
            Register(new SkillDefinition
            {
                Id = "mana_focus", Name = "Mana Focus", Job = JobId.Mystic, MaxLevel = 10, Passive = true,
                Description = "+3 SP regen per level.",
                IconLabel = "MF", IconColorHex = "#5499C7",
                PassivePerLevel = new StatModifiers { SpRegenFlat = 3 },
            });

            // ---------------------------------------------------------------- Sorcerer
            Register(new SkillDefinition
            {
                Id = "muspel_rain", Name = "Muspel Rain", Job = JobId.Sorcerer, MaxLevel = 10, Requires = Req("muspel_bolt", 5, "thunder_rune", 1),
                Description = "Burning meteors fall on an area, wave after wave. Can stun.",
                IconLabel = "MR", IconColorHex = "#DC7633",
                Target = SkillTarget.Ground, Damage = SkillDamage.Magic, Area = SkillArea.AtGround, Motion = SkillMotion.Cast,
                Element = Element.Fire, UseWeaponElement = false,
                Range = 9f, Radius = 2.5f, Power = 125f, Hits = T(2, 2, 3, 3, 4, 4, 5, 5, 6, 6), HitInterval = 0.4f,
                CastTime = L(5f, 0.4f), SpCost = L(20f, 4f), AfterCastDelay = 2f,
                Status = StatusEffect.Stun, StatusChance = L(3f, 3f), StatusDuration = 3f,
            });
            Register(new SkillDefinition
            {
                Id = "storm_lance", Name = "Storm Lance", Job = JobId.Sorcerer, MaxLevel = 10, Requires = Req("thunder_rune", 1),
                Description = "A ball of lightning that hits many times and blasts the target back.",
                IconLabel = "STL", IconColorHex = "#5DADE2",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Magic, Motion = SkillMotion.Cast, Projectile = true,
                Element = Element.Wind, UseWeaponElement = false,
                Range = 9f, Power = 100f, Hits = L(3f, 1f), HitInterval = 0.08f, Knockback = L(0.5f, 0.15f),
                CastTime = L(2f, 0.4f), SpCost = L(20f, 3f), AfterCastDelay = 0.5f, PoiseMultiplier = 0.5f,
            });
            Register(new SkillDefinition
            {
                Id = "frost_nova", Name = "Frost Nova", Job = JobId.Sorcerer, MaxLevel = 10, Requires = Req("frost_lock", 3),
                Description = "A ring of frost around you that can freeze everything it touches.",
                IconLabel = "FN", IconColorHex = "#85C1E9",
                Target = SkillTarget.Self, Damage = SkillDamage.Magic, Area = SkillArea.AroundSelf, Motion = SkillMotion.Cast,
                Element = Element.Water, UseWeaponElement = false,
                Radius = 2.5f, Power = L(66f, 7f), CastTime = L(6f, -0.2f), SpCost = L(45f, -1f), AfterCastDelay = 1f,
                Status = StatusEffect.Freeze, StatusChance = L(38f, 3f), StatusDuration = L(3f, 0.3f),
            });
            Register(new SkillDefinition
            {
                Id = "muspel_wall", Name = "Muspel Wall", Job = JobId.Sorcerer, MaxLevel = 10, Requires = Req("muspel_bolt", 4),
                Description = "A wall of fire on the ground that burns and repels enemies that touch it.",
                IconLabel = "MW", IconColorHex = "#E74C3C",
                Target = SkillTarget.Ground, Damage = SkillDamage.Magic, Special = SkillSpecial.Zone, Motion = SkillMotion.Cast,
                Element = Element.Fire, UseWeaponElement = false,
                Range = 6f, Radius = 1.3f, Power = 50f, Knockback = 1f,
                ZoneDuration = L(5f, 1f), ZoneTick = 0.5f, CastTime = L(2f, -0.15f), SpCost = 40,
            });
            Register(new SkillDefinition
            {
                Id = "earthen_spikes", Name = "Earthen Spikes", Job = JobId.Sorcerer, MaxLevel = 5,
                Description = "Stone spikes erupt in an area, one wave per level.",
                IconLabel = "ES", IconColorHex = "#A04000",
                Target = SkillTarget.Ground, Damage = SkillDamage.Magic, Area = SkillArea.AtGround, Motion = SkillMotion.Cast,
                Element = Element.Earth, UseWeaponElement = false,
                Range = 9f, Radius = 2.2f, Power = 125f, Hits = L(1f, 1f), HitInterval = 0.2f, CastTime = 1f, SpCost = 28,
            });
            Register(new SkillDefinition
            {
                Id = "arcane_mind", Name = "Arcane Mind", Job = JobId.Sorcerer, MaxLevel = 10, Passive = true,
                Description = "+1% magic damage and 1% faster casting per level.",
                IconLabel = "AM", IconColorHex = "#2C3E7A",
                PassivePerLevel = new StatModifiers { MagicDamagePercent = 1f, CastTimePercent = -1f },
            });

            // ---------------------------------------------------------------- Archmage (GDD signatures)
            Register(new SkillDefinition
            {
                Id = "glacial_tempest", Name = "Glacial Tempest", Job = JobId.Archmage, MaxLevel = 10, Requires = Req("frost_nova", 3, "storm_lance", 1),
                Description = "Multi-wave blizzard (5 waves). Each wave can freeze enemies brittle: frozen foes take triple damage from blunt weapons.",
                IconLabel = "GT", IconColorHex = "#5DADE2",
                Target = SkillTarget.Ground, Damage = SkillDamage.Magic, Area = SkillArea.AtGround, Motion = SkillMotion.Cast,
                Element = Element.Water, UseWeaponElement = false,
                Range = 10f, Radius = 4.5f, Power = L(110f, 10f), Hits = 5, HitInterval = 0.5f,
                CastTime = 6f, SpCost = L(51f, 3f), AfterCastDelay = 1.5f,
                Status = StatusEffect.Freeze, StatusChance = L(26f, 1f), StatusDuration = 6f,
            });
            Register(new SkillDefinition
            {
                Id = "runic_aegis", Name = "Runic Aegis", Job = JobId.Archmage, MaxLevel = 10, Requires = Req("earthen_spikes", 3),
                Description = "A runic dome on you or an ally that blocks one physical melee strike per level (10 at Lv 10).",
                IconLabel = "AEG", IconColorHex = "#2E86C1",
                Target = SkillTarget.Friend, BuffId = BuffCatalog.RunicAegis, Motion = SkillMotion.Cast,
                Range = 9f, BuffCharges = L(1f, 1f), CastTime = 1.5f, SpCost = L(30.5f, 0.5f), AfterCastDelay = 0.5f,
            });
            Register(new SkillDefinition
            {
                Id = "rune_amplify", Name = "Rune Amplify", Job = JobId.Archmage, MaxLevel = 10, Requires = Req("arcane_mind", 5),
                Description = "Your next damaging spell deals +5% damage per level.",
                IconLabel = "AMP", IconColorHex = "#A569BD",
                Target = SkillTarget.Self, BuffId = SkillBuffs.RuneAmplify, Motion = SkillMotion.Buff,
                CastTime = 0.7f, SpCost = L(14f, 2f),
            });
            Register(new SkillDefinition
            {
                Id = "heavens_wrath", Name = "Heaven's Wrath", Job = JobId.Archmage, MaxLevel = 10, Requires = Req("storm_lance", 5, "earthen_spikes", 3),
                Description = "Thunder from the sky strikes an area in four waves. Can blind.",
                IconLabel = "HW", IconColorHex = "#F7DC6F",
                Target = SkillTarget.Ground, Damage = SkillDamage.Magic, Area = SkillArea.AtGround, Motion = SkillMotion.Cast,
                Element = Element.Wind, UseWeaponElement = false,
                Range = 10f, Radius = 3.5f, Power = L(100f, 20f), Hits = 4, HitInterval = 0.4f,
                CastTime = L(10f, -0.5f), SpCost = L(60f, 4f), AfterCastDelay = 2f,
                Status = StatusEffect.Blind, StatusChance = L(4f, 4f), StatusDuration = 6f,
            });

            // ---------------------------------------------------------------- Sage
            Register(new SkillDefinition
            {
                Id = "rune_study", Name = "Rune Study", Job = JobId.Sage, MaxLevel = 10, Passive = true,
                Description = "+2 MATK and +1% Max SP per level.",
                IconLabel = "RST", IconColorHex = "#4A6FA5",
                PassivePerLevel = new StatModifiers { Matk = 2, MaxSpPercent = 1f },
            });
            Register(new SkillDefinition
            {
                Id = "free_cast", Name = "Free Cast", Job = JobId.Sage, MaxLevel = 10, Passive = true,
                Description = "Walk while casting at 7.5% of your speed per level (75% at Lv 10).",
                IconLabel = "FRC", IconColorHex = "#5DADE2",
                PassivePerLevel = new StatModifiers { CastMoveSpeedPercent = 7.5f },
            });
            Register(new SkillDefinition
            {
                Id = "auto_rune", Name = "Auto Rune", Job = JobId.Sage, MaxLevel = 10, Requires = Req("rune_study", 3),
                Description = "While active, your basic hits can cast a learned bolt (Muspel Bolt, Frost Spike or Thunder Rune) for free.",
                IconLabel = "ARN", IconColorHex = "#2E4053",
                Target = SkillTarget.Self, BuffId = SkillBuffs.AutoRune, Motion = SkillMotion.Buff,
                BuffDuration = L(120f, 30f), SpCost = L(35f, 3f),
            });
            Register(new SkillDefinition
            {
                Id = "bog_of_niflheim", Name = "Bog of Niflheim", Job = JobId.Sage, MaxLevel = 5,
                Description = "Turn the ground into a freezing bog: enemies in it lose AGI and DEX (monsters: FLEE and HIT) and move at half speed.",
                IconLabel = "BOG", IconColorHex = "#566573",
                Target = SkillTarget.Ground, Special = SkillSpecial.Zone, Motion = SkillMotion.Cast,
                Range = 9f, Radius = 2.2f, SpCost = L(5f, 5f),
                ZoneDuration = L(5f, 5f), ZoneTick = 0.5f, DebuffId = SkillBuffs.Bogged, DebuffDuration = 1.5f,
            });
            Register(new SkillDefinition
            {
                Id = "mystic_volcano", Name = "Mystic Volcano", Job = JobId.Sage, MaxLevel = 5, Requires = Req("rune_study", 5),
                Description = "Allies standing in the area gain ATK and magic damage.",
                IconLabel = "VOL", IconColorHex = "#CB4335",
                Target = SkillTarget.Ground, Special = SkillSpecial.Zone, Motion = SkillMotion.Cast, ZoneAffectsAllies = true,
                Range = 9f, Radius = 3f, CastTime = 2f, SpCost = L(48f, -2f),
                ZoneDuration = L(60f, 30f), ZoneTick = 1f, BuffId = SkillBuffs.VolcanoAura, BuffDuration = 2f,
            });

            // ---------------------------------------------------------------- Chronomancer
            Register(new SkillDefinition
            {
                Id = "stasis_field", Name = "Stasis Field", Job = JobId.Chronomancer, MaxLevel = 5, Requires = Req("bog_of_niflheim", 3),
                Description = "Stop time in an area: enemies there can be frozen in place (stunned).",
                IconLabel = "STF", IconColorHex = "#BB8FCE",
                Target = SkillTarget.Ground, Area = SkillArea.AtGround, Motion = SkillMotion.Cast,
                Range = 9f, Radius = 3f, CastTime = 2f, Cooldown = 10f, SpCost = 50,
                Status = StatusEffect.Stun, StatusChance = L(40f, 10f), StatusDuration = L(2f, 0.5f),
            });
            Register(new SkillDefinition
            {
                Id = "haste_rune", Name = "Haste Rune", Job = JobId.Chronomancer, MaxLevel = 10, Requires = Req("free_cast", 5),
                Description = "Quicken yourself or an ally: +2% ASPD and 2% faster casting per level.",
                IconLabel = "HST", IconColorHex = "#F5B041",
                Target = SkillTarget.Friend, BuffId = SkillBuffs.HasteRune, Motion = SkillMotion.Cast,
                Range = 9f, BuffDuration = L(60f, 12f), CastTime = 1f, SpCost = L(40f, 2f),
            });
            Register(new SkillDefinition
            {
                Id = "slow_time", Name = "Slow Time", Job = JobId.Chronomancer, MaxLevel = 5, Requires = Req("stasis_field", 1),
                Description = "An area where time drags: enemies in it move and attack slower.",
                IconLabel = "SLT", IconColorHex = "#7D3C98",
                Target = SkillTarget.Ground, Special = SkillSpecial.Zone, Motion = SkillMotion.Cast,
                Range = 9f, Radius = 3f, CastTime = 1.5f, SpCost = 35,
                ZoneDuration = L(8f, 2f), ZoneTick = 0.5f, DebuffId = SkillBuffs.TimeSlowed, DebuffDuration = 1.5f,
            });
            Register(new SkillDefinition
            {
                Id = "chrono_lance", Name = "Chrono Lance", Job = JobId.Chronomancer, MaxLevel = 10, Requires = Req("soul_lance", 5),
                Description = "Lances from a moment ago and a moment ahead strike at once. Ghost element.",
                IconLabel = "CHL", IconColorHex = "#5B2C6F",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Magic, Motion = SkillMotion.Cast, Projectile = true,
                Element = Element.Ghost, UseWeaponElement = false,
                Range = 9f, Power = 130f, Hits = T(2, 2, 3, 3, 4, 4, 5, 5, 6, 6), HitInterval = 0.1f,
                CastTime = L(1.5f, 0.2f), SpCost = L(20f, 3f), AfterCastDelay = 1f, PoiseMultiplier = 0.6f,
            });
            Register(new SkillDefinition
            {
                Id = "temporal_mend", Name = "Temporal Mend", Job = JobId.Chronomancer, MaxLevel = 5, Requires = Req("haste_rune", 3),
                Description = "Rewind wounds: allies in the area heal every second.",
                IconLabel = "TMD", IconColorHex = "#82E0AA",
                Target = SkillTarget.Ground, Special = SkillSpecial.Zone, Motion = SkillMotion.Cast, ZoneAffectsAllies = true,
                Range = 9f, Radius = 2.5f, CastTime = 2f, SpCost = L(30f, 6f),
                ZoneDuration = L(4f, 2f), ZoneTick = 1f, FlatHeal = L(60f, 40f),
            });
        }

        private static void RegisterBolt(string id, string name, string icon, string color, Element element, string flavor)
        {
            Register(new SkillDefinition
            {
                Id = id, Name = name, Job = JobId.Mystic, MaxLevel = 10,
                Description = $"Bolts of {flavor}: one bolt (100% MATK) per level.",
                IconLabel = icon, IconColorHex = color,
                Target = SkillTarget.Enemy, Damage = SkillDamage.Magic, Motion = SkillMotion.Cast, Projectile = true,
                Element = element, UseWeaponElement = false,
                Range = 9f, Power = 100f, Hits = L(1f, 1f), HitInterval = 0.12f,
                CastTime = L(0.7f, 0.7f), SpCost = L(12f, 2f), AfterCastDelay = L(0.8f, 0.2f), PoiseMultiplier = 0.6f,
            });
        }
    }
}
