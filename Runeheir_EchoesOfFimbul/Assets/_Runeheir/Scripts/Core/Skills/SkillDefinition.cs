using System;
using Runeheir.Combat;
using Runeheir.Jobs;
using Runeheir.Stats;

namespace Runeheir.Skills
{
    public enum SkillTarget
    {
        /// <summary>Fires on yourself immediately.</summary>
        Self = 0,

        /// <summary>Click a hostile target.</summary>
        Enemy = 1,

        /// <summary>Click yourself or a friendly target (Heal).</summary>
        Friend = 2,

        /// <summary>Click a spot on the ground.</summary>
        Ground = 3,
    }

    public enum SkillDamage
    {
        None = 0,
        Physical = 1,
        Magic = 2,
    }

    /// <summary>Who a damaging skill hits.</summary>
    public enum SkillArea
    {
        /// <summary>The clicked target only.</summary>
        Single = 0,

        /// <summary>Everyone within Radius of the caster.</summary>
        AroundSelf = 1,

        /// <summary>The clicked target and everyone within Radius of it (splash).</summary>
        AroundTarget = 2,

        /// <summary>Everyone within Radius of the clicked ground point.</summary>
        AtGround = 3,

        /// <summary>Everyone in a 0.7 m wide line from the caster to Range (pierces).</summary>
        Line = 4,
    }

    /// <summary>Behaviour beyond "deal damage / apply buff".</summary>
    public enum SkillSpecial
    {
        None = 0,

        /// <summary>Heals the target: <c>FlatHeal</c> + Base Level, or the Ragnarok Heal formula at <c>HealLevel</c>.</summary>
        Heal = 1,

        /// <summary>Moves the caster up to Range (ground) or next to the target (enemy), then deals the skill's damage there.</summary>
        Dash = 2,

        /// <summary>GDD Fist of Odin: drains all SP into ATK x (8 + SP/10) + 1750.</summary>
        FistOfOdin = 3,

        /// <summary>Rolls once per monster for one of its drops (DEX vs monster level).</summary>
        Steal = 4,

        /// <summary>Removes negative statuses and debuffs from the target.</summary>
        Cleanse = 5,

        /// <summary>Applies the debuff to the target and makes it attack the caster.</summary>
        Provoke = 6,

        /// <summary>Leaves an area on the ground that repeats the skill's effect every <c>ZoneTick</c> seconds.</summary>
        Zone = 7,

        /// <summary>Consumes Spirit Spheres: one hit per sphere, up to the skill level.</summary>
        SpiritRelease = 8,
    }

    /// <summary>Which animation the caster plays (the bridge maps it to an Animator state or placeholder motion).</summary>
    public enum SkillMotion
    {
        None = 0,
        Swing = 1,
        Thrust = 2,
        Spin = 3,
        Cast = 4,
        Shoot = 5,
        Punch = 6,
        Leap = 7,
        Buff = 8,
    }

    public readonly struct SkillRequirement
    {
        public readonly string SkillId;
        public readonly int Level;

        public SkillRequirement(string skillId, int level)
        {
            SkillId = skillId;
            Level = level;
        }
    }

    /// <summary>Auto-casts a skill on a basic-attack hit (Storm Fists, Auto Rune).</summary>
    public sealed class ProcDefinition
    {
        /// <summary>One of these is picked at random (only learned ones when <see cref="OnlyLearned"/>).</summary>
        public string[] SkillIds = Array.Empty<string>();

        /// <summary>Percent per basic-attack hit, by the level of the passive/buff that owns the proc.</summary>
        public LevelValue Chance;

        /// <summary>Level of the auto-cast skill, by the owner's level (capped by the learned level when OnlyLearned).</summary>
        public LevelValue ProcLevel = 1f;

        public bool OnlyLearned;
    }

    /// <summary>
    /// One skill: who learns it, what it costs and does at each level. Leveled numbers are <see cref="LevelValue"/>s
    /// read with <c>At(level)</c>, e.g. <c>skill.Power.At(5)</c>. Kept as code-defined data so it diffs and tests easily.
    /// </summary>
    public sealed class SkillDefinition
    {
        public string Id;
        public string Name;

        /// <summary>What the skill does in general; per-level numbers are shown by the tooltip.</summary>
        public string Description;

        public string IconLabel;
        public string IconColorHex = "#5D6D7E";

        /// <summary>The job that learns it; every later job in that branch inherits it.</summary>
        public JobId Job;

        public int MaxLevel = 1;

        /// <summary>Never activated: <see cref="PassivePerLevel"/> applies while learned (and the weapon fits).</summary>
        public bool Passive;

        /// <summary>Known at Lv 1 from character creation, free (First Aid).</summary>
        public bool Granted;

        /// <summary>Not shown in the skill tree and can't be learned (auto-cast effects such as Storm Fists' combo).</summary>
        public bool Hidden;

        public SkillRequirement[] Requires = Array.Empty<SkillRequirement>();

        /// <summary>Weapons the skill needs. None = any.</summary>
        public WeaponMask Weapons;

        // ------------------------------------------------------------ activation
        public SkillTarget Target;
        public SkillDamage Damage;
        public SkillArea Area;
        public SkillSpecial Special;
        public SkillMotion Motion;

        /// <summary>Visual: a projectile flies to the target and the damage lands on arrival.</summary>
        public bool Projectile;

        /// <summary>
        /// GDD Vortex Cleave: enemies knocked back by the last wave crash into enemies where they land
        /// (the body and everyone it lands on take one half-power hit, at most once each per cast).
        /// </summary>
        public bool ChainImpacts;

        /// <summary>Can roll critical hits like a basic attack (Sharp Shot, Shadow Veil ambush).</summary>
        public bool CanCrit;

        /// <summary>Skips the HIT vs FLEE roll.</summary>
        public bool NeverMiss;

        public Element Element = Element.Neutral;

        /// <summary>Use the weapon's element instead of <see cref="Element"/> (physical skills).</summary>
        public bool UseWeaponElement = true;

        /// <summary>Damage percent per hit (300 = 300% ATK/MATK).</summary>
        public LevelValue Power = 100f;

        public LevelValue Hits = 1f;

        /// <summary>Seconds between hits/waves (0 = all at once).</summary>
        public float HitInterval;

        public LevelValue SpCost;

        /// <summary>Percent of current HP paid on use (Radiant Cross).</summary>
        public LevelValue HpCostPercent;

        /// <summary>Spirit Spheres needed (and consumed) to use the skill.</summary>
        public int SphereCost;

        /// <summary>Base variable cast time in seconds, scaled by DEX (150 DEX = instant).</summary>
        public LevelValue CastTime;

        /// <summary>Global delay after the cast before any other skill.</summary>
        public LevelValue AfterCastDelay = 0.3f;

        public LevelValue Cooldown;

        /// <summary>Edge-to-edge cast range in meters (ignored for Self).</summary>
        public LevelValue Range = 1.2f;

        public LevelValue Radius;
        public LevelValue Knockback;

        /// <summary>Poise damage per hit relative to a basic hit (weapon poise, or 10 for spells).</summary>
        public LevelValue PoiseMultiplier = 1f;

        public StatusEffect Status;

        /// <summary>The status ignores the target's resistance (GDD Phantom Barrage: "guaranteed stun"). Sowilo still blocks it.</summary>
        public bool GuaranteedStatus;

        public LevelValue StatusChance;
        public LevelValue StatusDuration;

        /// <summary>Buff applied to the caster (Self) or the clicked friend (Friend).</summary>
        public string BuffId;

        /// <summary>Overrides the buff's duration when non-zero.</summary>
        public LevelValue BuffDuration;

        /// <summary>Overrides the buff's charges when non-zero (Runic Aegis: one block per level).</summary>
        public LevelValue BuffCharges;

        /// <summary>Debuff applied to each enemy the skill hits (or to the target of Provoke).</summary>
        public string DebuffId;

        public LevelValue DebuffDuration;

        /// <summary>Flat heal (First Aid); Base Level is added.</summary>
        public LevelValue FlatHeal;

        /// <summary>Ragnarok Heal level used by the heal formula (0 = use FlatHeal).</summary>
        public LevelValue HealLevel;

        /// <summary>Extra damage percent against Undead and Demon targets (holy skills).</summary>
        public float BonusVsUndeadPercent;

        /// <summary>Flat damage added to each landed hit (Fist of Odin's +1750).</summary>
        public LevelValue FlatDamage;

        /// <summary>Ignores the target's DEF (Occult Strike).</summary>
        public bool IgnoreDefense;

        // ------------------------------------------------------------ zones (Special = Zone)
        public LevelValue ZoneDuration;
        public float ZoneTick = 1f;

        /// <summary>Trap: fires once on the first enemy that walks in, then disappears.</summary>
        public bool ZoneTrap;

        /// <summary>The zone affects allies (Sanctuary) instead of enemies.</summary>
        public bool ZoneAffectsAllies;

        // ------------------------------------------------------------ passives
        /// <summary>Added once per learned level while the weapon fits <see cref="PassiveWeapons"/>.</summary>
        public StatModifiers PassivePerLevel;

        public WeaponMask PassiveWeapons;

        /// <summary>Passive auto-cast on basic-attack hits (Storm Fists).</summary>
        public ProcDefinition Proc;

        public bool IsMagic => Damage == SkillDamage.Magic;

        public bool IsPhysical => Damage == SkillDamage.Physical;

        /// <summary>Physical skills used up close count as melee (Runic Aegis blocks them).</summary>
        public bool IsMelee(int level)
        {
            return IsPhysical && Range.At(level) <= 2f && !Projectile;
        }

        public int ClampLevel(int level)
        {
            return Math.Max(1, Math.Min(MaxLevel, level));
        }
    }
}
