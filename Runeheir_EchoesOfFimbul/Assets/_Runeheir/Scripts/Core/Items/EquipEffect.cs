using System;
using System.Collections.Generic;
using Runeheir.Combat;
using Runeheir.Stats;

namespace Runeheir.Items
{
    public enum OnHitKind
    {
        /// <summary>Inflict a status on the target (Toxic Spore: poison).</summary>
        Status = 0,

        /// <summary>Buff yourself (Fenrir: Berserk Wolf Form).</summary>
        SelfBuff = 1,

        /// <summary>Break the target's poise outright (Hel's Executioner: Mortal Stagger).</summary>
        PoiseBreak = 2,
    }

    /// <summary>A chance-on-hit effect from a card or runeword. Triggers on basic attacks that land.</summary>
    public sealed class OnHitEffect
    {
        public OnHitKind Kind;
        public StatusEffect Status;
        public float ChancePercent;
        public float Duration;
        public string BuffId;

        /// <summary>Ignores the target's resistance (Jormungandr's Brood frostbite: "unblockable").</summary>
        public bool Guaranteed;

        /// <summary>Only melee hits trigger it (most cards); false = arrows too.</summary>
        public bool MeleeOnly = true;
    }

    /// <summary>When an <see cref="AutoCast"/> fires (Ragnarok's bAutoSpell, bAutoSpellWhenHit, bAutoSpellOnSkill, autobonus).</summary>
    public enum AutoCastTrigger
    {
        /// <summary>A basic attack lands.</summary>
        OnAttack = 0,

        /// <summary>You take damage from an enemy.</summary>
        WhenHit = 1,

        /// <summary>You cast <see cref="AutoCast.TriggerSkillId"/>.</summary>
        OnSkill = 2,
    }

    /// <summary>
    /// A chance to cast a skill for free (offensive skills at the enemy, support skills on yourself), or to gain a timed
    /// buff (<see cref="BuffId"/>), when <see cref="Trigger"/> happens. At most one fires per trigger.
    /// </summary>
    public sealed class AutoCast
    {
        public AutoCastTrigger Trigger;
        public string SkillId;

        /// <summary>The level to cast at; 0 = the level you have learned, at least 1.</summary>
        public int Level;

        public string BuffId;
        public float ChancePercent;
        public string TriggerSkillId;

        public AutoCast Scaled(float times)
        {
            return new AutoCast
            {
                Trigger = Trigger, SkillId = SkillId, Level = Level, BuffId = BuffId, TriggerSkillId = TriggerSkillId,
                ChancePercent = ChancePercent * times,
            };
        }
    }

    /// <summary>Extra bonus while a base stat reaches a threshold (Jotun Brawler: base STR 180+).</summary>
    public sealed class EquipCondition
    {
        public StatType Stat;
        public int MinBaseValue;
        public EquipEffect Bonus;
    }

    /// <summary>
    /// Everything a piece of gear, a Soul Card or a runeword can grant. Stat-like bonuses go in <see cref="Modifiers"/>;
    /// race/size/element tables, on-hit procs, element changes and immunities live here.
    /// Fluent helpers keep the catalogs readable: <c>new EquipEffect { Modifiers = ... }.Vs(Race.Beast, 20)</c>.
    /// </summary>
    public sealed class EquipEffect
    {
        public StatModifiers Modifiers = StatModifiers.Empty();

        /// <summary>+% physical damage dealt by target race / size / element (cards multiply across categories).</summary>
        public readonly float[] VsRace = new float[CombatEnumCounts.Races];

        public readonly float[] VsSize = new float[CombatEnumCounts.Sizes];
        public readonly float[] VsElement = new float[CombatEnumCounts.Elements];

        /// <summary>% damage taken from attacker race / attack element (negative = less).</summary>
        public readonly float[] TakenFromRace = new float[CombatEnumCounts.Races];

        public readonly float[] TakenFromElement = new float[CombatEnumCounts.Elements];

        public readonly List<OnHitEffect> OnHit = new List<OnHitEffect>();

        /// <summary>Armor cards that change your body's element (Frost Wolf: Water, Hel's Vanguard: Ghost).</summary>
        public Element? ArmorElement;

        /// <summary>Runewords that change the weapon's element (Blade of Dawn: Holy).</summary>
        public Element? WeaponElement;

        public readonly List<StatusEffect> Immunities = new List<StatusEffect>();

        /// <summary>Extra resistance against one status, in percent (Cave Crawler: +10% vs Stone Curse).</summary>
        public StatusEffect ResistStatus;

        public float ResistPercent;

        /// <summary>The weapon can't break (Ancient Golem).</summary>
        public bool Unbreakable;

        /// <summary>Flat ATK per Base Level (Megingjard: 0.5 = +1 ATK every 2 levels).</summary>
        public float AtkPerBaseLevel;

        public EquipCondition Condition;

        // ------------------------------------------------------------ Ragnarok bonuses from the XileRetro database (Phase 8)
        /// <summary>% damage taken from an attacker size (bSubSize).</summary>
        public readonly float[] TakenFromSize = new float[CombatEnumCounts.Sizes];

        /// <summary>+% physical damage against bosses / normal monsters (bAddClass).</summary>
        public float VsBoss;

        public float VsNormal;

        /// <summary>+crit chance, % DEF and % MDEF ignored, +% magic damage against a race (bCriticalAddRace, bIgnoreDefRaceRate,
        /// bIgnoreMdefRaceRate, bMagicAddRace), +% magic damage against an element (bMagicAddEle).</summary>
        public readonly float[] CritVsRace = new float[CombatEnumCounts.Races];

        public readonly float[] DefBypassVsRace = new float[CombatEnumCounts.Races];
        public readonly float[] MdefBypassVsRace = new float[CombatEnumCounts.Races];
        public readonly float[] MagicVsRace = new float[CombatEnumCounts.Races];
        public readonly float[] MagicVsElement = new float[CombatEnumCounts.Elements];

        /// <summary>Extra resistance against several statuses, in percent (bResEff).</summary>
        public readonly float[] StatusResist = new float[CombatEnumCounts.StatusEffects];

        /// <summary>Bonuses Runeheir doesn't have yet (EXP or drop bonuses...), described for the tooltip.</summary>
        public readonly List<string> Inactive = new List<string>();

        /// <summary>Free skill casts and timed buffs on attack, when hit or on a skill (Ragnarok's auto-spells and autobonus).</summary>
        public readonly List<AutoCast> AutoCasts = new List<AutoCast>();

        /// <summary>Skills the wearer can use at this level whatever their job (Ragnarok's <c>skill</c> on gear).</summary>
        public readonly Dictionary<string, int> GrantedSkills = new Dictionary<string, int>();

        /// <summary>+% damage of one skill (bSkillAtk).</summary>
        public readonly Dictionary<string, float> SkillDamage = new Dictionary<string, float>();

        public EquipEffect AutoCastOnAttack(string skillId, int level, float chancePercent)
        {
            AutoCasts.Add(new AutoCast { Trigger = AutoCastTrigger.OnAttack, SkillId = skillId, Level = level, ChancePercent = chancePercent });
            return this;
        }

        public EquipEffect AutoCastWhenHit(string skillId, int level, float chancePercent)
        {
            AutoCasts.Add(new AutoCast { Trigger = AutoCastTrigger.WhenHit, SkillId = skillId, Level = level, ChancePercent = chancePercent });
            return this;
        }

        public EquipEffect AutoCastOnSkill(string triggerSkillId, string skillId, int level, float chancePercent)
        {
            AutoCasts.Add(new AutoCast
            {
                Trigger = AutoCastTrigger.OnSkill, TriggerSkillId = triggerSkillId, SkillId = skillId, Level = level, ChancePercent = chancePercent,
            });
            return this;
        }

        public EquipEffect AutoBuff(AutoCastTrigger trigger, string buffId, float chancePercent, string triggerSkillId = null)
        {
            AutoCasts.Add(new AutoCast { Trigger = trigger, BuffId = buffId, ChancePercent = chancePercent, TriggerSkillId = triggerSkillId });
            return this;
        }

        public EquipEffect Grant(string skillId, int level)
        {
            GrantedSkills[skillId] = Math.Max(level, GrantedSkills.TryGetValue(skillId, out int had) ? had : 0);
            return this;
        }

        public EquipEffect SkillBonus(string skillId, float percent)
        {
            SkillDamage[skillId] = percent + (SkillDamage.TryGetValue(skillId, out float had) ? had : 0f);
            return this;
        }

        public EquipEffect Vs(Race race, float percent)
        {
            VsRace[(int)race] += percent;
            return this;
        }

        public EquipEffect Vs(Size size, float percent)
        {
            VsSize[(int)size] += percent;
            return this;
        }

        public EquipEffect Vs(Element element, float percent)
        {
            VsElement[(int)element] += percent;
            return this;
        }

        public EquipEffect From(Race race, float percent)
        {
            TakenFromRace[(int)race] += percent;
            return this;
        }

        public EquipEffect From(Element element, float percent)
        {
            TakenFromElement[(int)element] += percent;
            return this;
        }

        public EquipEffect From(Size size, float percent)
        {
            TakenFromSize[(int)size] += percent;
            return this;
        }

        public EquipEffect VsBosses(float percent)
        {
            VsBoss += percent;
            return this;
        }

        public EquipEffect VsNormals(float percent)
        {
            VsNormal += percent;
            return this;
        }

        public EquipEffect CritVs(Race race, float chance)
        {
            CritVsRace[(int)race] += chance;
            return this;
        }

        public EquipEffect IgnoreDef(Race race, float percent)
        {
            DefBypassVsRace[(int)race] += percent;
            return this;
        }

        public EquipEffect IgnoreMdef(Race race, float percent)
        {
            MdefBypassVsRace[(int)race] += percent;
            return this;
        }

        public EquipEffect MagicVs(Race race, float percent)
        {
            MagicVsRace[(int)race] += percent;
            return this;
        }

        public EquipEffect MagicVs(Element element, float percent)
        {
            MagicVsElement[(int)element] += percent;
            return this;
        }

        public EquipEffect Resist(StatusEffect status, float percent)
        {
            StatusResist[(int)status] += percent;
            return this;
        }

        /// <summary>A bonus Runeheir can't apply yet; shown in the tooltip so the item still reads as the original.</summary>
        public EquipEffect Note(string text)
        {
            Inactive.Add(text);
            return this;
        }

        public EquipEffect Immune(params StatusEffect[] statuses)
        {
            Immunities.AddRange(statuses);
            return this;
        }

        public EquipEffect Proc(OnHitEffect effect)
        {
            OnHit.Add(effect);
            return this;
        }
    }
}
