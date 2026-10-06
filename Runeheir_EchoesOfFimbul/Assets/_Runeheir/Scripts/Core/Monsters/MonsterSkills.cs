using System;
using System.Collections.Generic;
using Runeheir.Combat;

namespace Runeheir.Monsters
{
    /// <summary>What a monster skill does (GDD Phase 5 monster AI pipeline).</summary>
    public enum MonsterSkillKind
    {
        /// <summary>A heavy melee blow on the target. With a cast time, stepping out of reach before it lands dodges it.</summary>
        Strike = 0,

        /// <summary>A spell or shot at one target in range; it can't be dodged once released.</summary>
        Bolt = 1,

        /// <summary>A blast on a circle the players can see coming: at the target's feet when the cast starts, or around the caster.</summary>
        Area = 2,

        /// <summary>Buffs the caster (and allies within <see cref="MonsterSkill.Radius"/> when set).</summary>
        Buff = 3,

        /// <summary>Heals the caster (and allies within <see cref="MonsterSkill.Radius"/> when set).</summary>
        Heal = 4,

        /// <summary>Calls <see cref="MonsterSkill.SummonCount"/> of <see cref="MonsterSkill.SummonId"/>; never while that many are still alive.</summary>
        Summon = 5,

        /// <summary>Jumps onto the target and blasts the landing circle.</summary>
        Leap = 6,

        /// <summary>Blinks behind the target, or away to somewhere nearby.</summary>
        Teleport = 7,
    }

    public sealed class MonsterSkill
    {
        public string Id;
        public string Name;
        public MonsterSkillKind Kind;

        /// <summary>Seconds of casting before it goes off (cast bar; a stagger interrupts it). 0 = instant.</summary>
        public float CastTime;

        public float Cooldown = 10f;

        /// <summary>Chance to use it on a think where it's ready and usable.</summary>
        public float ChancePercent = 100f;

        /// <summary>Reach to the target's edge (Strike: the monster's own attack range when 0).</summary>
        public float Range;

        /// <summary>Leap and long-range Bolts: only when the target is at least this far.</summary>
        public float MinRange;

        /// <summary>Area and Leap: blast radius. Buff and Heal: allies in this radius share it.</summary>
        public float Radius;

        /// <summary>Area: centered on the caster instead of the target.</summary>
        public bool CenteredOnSelf;

        /// <summary>Damage in percent of ATK (physical) or MATK (magical).</summary>
        public float Percent = 100f;

        public bool Magical;
        public Element Element = Element.Neutral;
        public StatusEffect Status;
        public float StatusChance;
        public float StatusSeconds;
        public float Knockback;

        /// <summary>Chance per hit to break the target's weapon (Ancient Golem Card prevents it; Brokk repairs).</summary>
        public float BreakWeaponPercent;

        /// <summary>Strike, Bolt: heals the caster for this percent of the damage dealt.</summary>
        public float LeechPercent;

        /// <summary>Extra poise damage on top of the hit (Execution's Mortal Stagger).</summary>
        public float PoiseDamage;

        /// <summary>Heal: percent of max HP.</summary>
        public float HealPercent;

        /// <summary>Buff: id from the BuffCatalog (monster buffs are in <see cref="MonsterBuffs"/>).</summary>
        public string BuffId;

        public string SummonId;
        public int SummonCount;

        /// <summary>Teleport: appear behind the target (otherwise somewhere 6–12 m away).</summary>
        public bool BehindTarget;

        /// <summary>Only at or below this HP percent.</summary>
        public float BelowHpPercent = 100f;

        /// <summary>Bosses: only from this phase on (0 = from the start).</summary>
        public int MinPhase;

        /// <summary>Line shouted over its head when the cast starts.</summary>
        public string Shout;

        public bool IsDamaging => Kind == MonsterSkillKind.Strike || Kind == MonsterSkillKind.Bolt || Kind == MonsterSkillKind.Area || Kind == MonsterSkillKind.Leap;

        /// <summary>Area and Leap show a ground circle while casting.</summary>
        public bool HasTelegraph => (Kind == MonsterSkillKind.Area || Kind == MonsterSkillKind.Leap) && Radius > 0f;
    }

    /// <summary>A boss enters this phase when its HP falls to <see cref="BelowHpPercent"/> (and leaves it only by resetting).</summary>
    public sealed class BossPhase
    {
        public float BelowHpPercent;
        public string Shout;

        /// <summary>Buff it keeps for the rest of the fight.</summary>
        public string BuffId;

        public string SummonId;
        public int SummonCount;
    }

    /// <summary>What the AI knows about its situation when picking a skill.</summary>
    public struct MonsterSkillContext
    {
        public float HpPercent;
        public int Phase;
        public bool HasTarget;

        /// <summary>Distance between the bodies' edges (meters).</summary>
        public float TargetDistance;

        public float AttackRange;

        /// <summary>Living minions from this skill's <see cref="MonsterSkill.SummonId"/>.</summary>
        public Func<string, int> SummonsAlive;

        /// <summary>True when the caster already has this buff.</summary>
        public Func<string, bool> HasBuff;
    }

    /// <summary>Pure skill-selection rules (the runtime brain adds timers, movement and visuals).</summary>
    public static class MonsterSkillRules
    {
        /// <summary>Seconds between any two skills from the same monster.</summary>
        public const float GlobalCooldown = 1.5f;

        /// <summary>How far a monster can be from its target and still choose a Strike (it walks the rest).</summary>
        public const float StrikeSlack = 0.5f;

        /// <summary>Phase index for this HP: 0 at full HP, +1 for every phase threshold already crossed.</summary>
        public static int PhaseFor(MonsterDefinition definition, float hpPercent)
        {
            int phase = 0;
            foreach (var p in definition.Phases)
            {
                if (hpPercent <= p.BelowHpPercent)
                {
                    phase++;
                }
            }

            return phase;
        }

        /// <summary>True when the skill's conditions hold right now (cooldowns are the caller's).</summary>
        public static bool IsUsable(MonsterSkill skill, in MonsterSkillContext context)
        {
            if (skill == null || context.HpPercent > skill.BelowHpPercent || context.Phase < skill.MinPhase)
            {
                return false;
            }

            switch (skill.Kind)
            {
                case MonsterSkillKind.Strike:
                    return context.HasTarget && context.TargetDistance <= (skill.Range > 0f ? skill.Range : context.AttackRange) + StrikeSlack;
                case MonsterSkillKind.Bolt:
                case MonsterSkillKind.Leap:
                    return context.HasTarget && context.TargetDistance <= skill.Range && context.TargetDistance >= skill.MinRange;
                case MonsterSkillKind.Area:
                    return context.HasTarget && context.TargetDistance <= (skill.CenteredOnSelf ? Math.Max(0.5f, skill.Radius - 0.5f) : skill.Range);
                case MonsterSkillKind.Buff:
                    return context.HasTarget && !string.IsNullOrEmpty(skill.BuffId) && (context.HasBuff == null || !context.HasBuff(skill.BuffId));
                case MonsterSkillKind.Heal:
                    return skill.HealPercent > 0f && context.HpPercent < 100f;
                case MonsterSkillKind.Summon:
                    return context.HasTarget && skill.SummonCount > 0 && !string.IsNullOrEmpty(skill.SummonId)
                           && (context.SummonsAlive == null || context.SummonsAlive(skill.SummonId) < skill.SummonCount);
                case MonsterSkillKind.Teleport:
                    return context.HasTarget && context.TargetDistance <= Math.Max(skill.Range, 1f);
                default:
                    return false;
            }
        }

        /// <summary>
        /// The skill to use now, in priority order: the first usable, ready skill whose chance roll passes. Null = basic attacks.
        /// </summary>
        public static MonsterSkill Choose(IReadOnlyList<MonsterSkill> skills, in MonsterSkillContext context, Func<MonsterSkill, bool> isReady, IRandomSource random)
        {
            if (skills == null)
            {
                return null;
            }

            foreach (var skill in skills)
            {
                if ((isReady == null || isReady(skill)) && IsUsable(skill, context) && random.Chance(skill.ChancePercent))
                {
                    return skill;
                }
            }

            return null;
        }
    }
}
