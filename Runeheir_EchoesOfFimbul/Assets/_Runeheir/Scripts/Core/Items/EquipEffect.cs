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
