using System;
using System.Collections.Generic;
using Runeheir.Movement;
using Runeheir.Stats;
using UnityEngine;

namespace Runeheir.Combat
{
    /// <summary>
    /// Anything that has HP and can fight: players and monsters. Owns HP/SP, buffs, statuses, poise and
    /// damage intake. Damage math itself lives in the engine-free <see cref="DamageCalculator"/>, and the
    /// status/poise rules in <see cref="StatusContainer"/> / <see cref="PoiseMeter"/>, so they can move to a
    /// Mirror server unchanged in Phase 6.
    /// </summary>
    public abstract class CombatEntity : MonoBehaviour
    {
        private static readonly List<CombatEntity> Registry = new List<CombatEntity>();

        /// <summary>How long Isa's shield freezes a melee attacker.</summary>
        private const float IsaFreezeSeconds = 3f;

        [SerializeField, Min(0.1f)] protected float bodyRadius = 0.45f;
        [SerializeField, Min(0.1f)] protected float bodyHeight = 1.8f;

        private readonly List<float> _dueDots = new List<float>();
        private readonly StatModifiers _activeModifiers = StatModifiers.Empty();
        private bool _modifiersDirty = true;
        private bool _watchingModifiers;

        /// <summary>(target, result, attacker) — floating damage numbers and logs listen here.</summary>
        public static event Action<CombatEntity, DamageResult, CombatEntity> AnyDamaged;

        /// <summary>(target, amount, isSp).</summary>
        public static event Action<CombatEntity, int, bool> AnyHealed;

        /// <summary>(victim, killer).</summary>
        public static event Action<CombatEntity, CombatEntity> AnyDied;

        /// <summary>(target, status) after a status lands (stagger included).</summary>
        public static event Action<CombatEntity, StatusEffect> AnyStatusApplied;

        public event Action<DamageResult, CombatEntity> Damaged;

        public event Action<CombatEntity> Died;

        /// <summary>HP, SP or their maximums changed.</summary>
        public event Action VitalsChanged;

        public static IReadOnlyList<CombatEntity> All => Registry;

        public BuffContainer Buffs { get; } = new BuffContainer();

        public StatusContainer Statuses { get; } = new StatusContainer();

        public PoiseMeter Poise { get; } = new PoiseMeter(60f);

        public abstract Faction Faction { get; }

        public abstract string DisplayName { get; }

        public abstract int Level { get; }

        public int Hp { get; protected set; }

        public int MaxHp { get; protected set; } = 1;

        public int Sp { get; protected set; }

        public int MaxSp { get; protected set; } = 1;

        public bool IsDead { get; private set; }

        public float Radius => bodyRadius;

        public float Height => bodyHeight;

        public Vector3 Position => transform.position;

        public bool IsStunned => Statuses.Has(StatusEffect.Stun);

        public bool IsFrozen => Statuses.Has(StatusEffect.Freeze);

        public bool IsStaggered => Statuses.Has(StatusEffect.Stagger);

        /// <summary>Can't move, attack or cast: dead, stunned, frozen, stone, asleep or staggered.</summary>
        public bool IsIncapacitated => IsDead || Statuses.IsIncapacitated;

        /// <summary>Snared (Ankle Snare) or incapacitated.</summary>
        public bool CanMove => !IsDead && !Statuses.BlocksMovement;

        /// <summary>Silenced or incapacitated.</summary>
        public bool CanUseSkills => !IsDead && !Statuses.BlocksSkills;

        /// <summary>Stealth (Shadow Cloak, Shadow Veil): monsters can't see or target this entity.</summary>
        public bool IsHidden => Buffs.HasTrait(BuffTraits.Stealth);

        public virtual bool CanBeKnockedBack => true;

        /// <summary>Hyper-armor, Endure, Holdfast: poise never breaks.</summary>
        public virtual bool StaggerImmune => false;

        /// <summary>The stats that resist statuses aimed at this entity.</summary>
        public virtual StatusResistances StatusResistances => default;

        /// <summary>Poise damage of one basic hit by this entity.</summary>
        public virtual float BasicPoiseDamage => 8f;

        // ------------------------------------------------------------ attack timing (ASPD for players)
        /// <summary>Edge-to-edge reach in meters.</summary>
        public abstract float AttackRange { get; }

        /// <summary>Seconds between basic attacks.</summary>
        public abstract float AttackInterval { get; }

        public virtual float Aspd => StatFormulas.MinAspd;

        public virtual float AttackPlayRate => 1f;

        public virtual float SwingDuration => Mathf.Min(StatFormulas.BaseSwingSeconds, AttackInterval * 0.6f);

        public virtual bool IsRangedAttacker => false;

        public abstract AttackerProfile BuildAttackerProfile();

        public abstract DefenderProfile BuildDefenderProfile();

        /// <summary>Percent damage taken (negative = less). Players read it from their stats.</summary>
        protected virtual float DamageTakenPercent => ActiveModifiers.DamageTakenPercent;

        /// <summary>Percent chance to block a physical melee hit.</summary>
        protected virtual float BlockChance => ActiveModifiers.BlockChance;

        /// <summary>Percent of physical melee damage sent back to the attacker (Draugr Warlord Card).</summary>
        protected virtual float ReflectMeleePercent => ActiveModifiers.ReflectMeleePercent;

        /// <summary>Percent of spell damage sent back to the caster (Hagalaz Rebound).</summary>
        protected virtual float ReflectMagicPercent => ActiveModifiers.ReflectMagicPercent;

        /// <summary>Buffs + statuses combined (cached). Monsters build their profiles from it.</summary>
        protected StatModifiers ActiveModifiers
        {
            get
            {
                WatchModifiers();
                if (_modifiersDirty)
                {
                    _activeModifiers.Clear();
                    _activeModifiers.Add(Buffs.Aggregate);
                    _activeModifiers.Add(Statuses.Aggregate);
                    _modifiersDirty = false;
                }

                return _activeModifiers;
            }
        }

        public bool IsHostileTo(CombatEntity other)
        {
            return other != null && other != this && Faction != Faction.Neutral && other.Faction != Faction.Neutral && Faction != other.Faction;
        }

        /// <summary>Hidden entities can only be seen by their own side.</summary>
        public bool CanSee(CombatEntity other)
        {
            return other != null && (!other.IsHidden || other.Faction == Faction);
        }

        /// <summary>Distance between the two bodies' edges, ignoring height.</summary>
        public float EdgeDistanceTo(CombatEntity other)
        {
            return Mathf.Max(0f, HorizontalDistance(Position, other.Position) - Radius - other.Radius);
        }

        public float EdgeDistanceTo(Vector3 point)
        {
            return Mathf.Max(0f, HorizontalDistance(Position, point) - Radius);
        }

        public static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        /// <summary>One basic attack roll against <paramref name="target"/>.</summary>
        public virtual DamageResult RollBasicAttack(CombatEntity target)
        {
            return DamageCalculator.Physical(BuildAttackerProfile(), target.BuildDefenderProfile(), 100f, true, SystemRandomSource.Shared);
        }

        /// <summary>A basic attack connected (procs: Storm Fists, Keen Edge, Auto Rune).</summary>
        public virtual void OnBasicAttackLanded(CombatEntity target, DamageResult result)
        {
        }

        /// <summary>
        /// Applies a hit. Melee hits can be blocked (Runic Aegis charges, Guardian's Oath chance); damage-taken
        /// modifiers scale it; absorb shields (Isa) soak it; reflect cards and buffs bounce part back; hits that deal
        /// damage wake sleepers, shatter stone and chip <paramref name="poiseDamage"/>.
        /// Returns the result as applied (blocked, scaled), so callers skip on-hit effects for blocked hits
        /// (a hit the shield soaked up completely counts as blocked).
        /// </summary>
        public DamageResult ReceiveDamage(DamageResult result, CombatEntity attacker, bool physicalMelee, float poiseDamage = 0f)
        {
            if (IsDead)
            {
                return DamageResult.Miss();
            }

            if (physicalMelee && !result.IsMiss && !result.IsBlocked)
            {
                if (Buffs.TryConsumeCharge(BuffTraits.MeleeBlockCharges)
                    || (BlockChance > 0f && SystemRandomSource.Shared.Chance(BlockChance)))
                {
                    result = DamageResult.Blocked();
                }
            }

            // Isa's glacial shield freezes whoever strikes it in melee, even on the blow that breaks it.
            if (physicalMelee && !result.IsMiss && attacker != null && attacker != this && !attacker.IsDead
                && Buffs.HasTrait(BuffTraits.FreezeMeleeAttackers))
            {
                attacker.TryApplyStatus(StatusEffect.Freeze, 100f, IsaFreezeSeconds);
            }

            if (result.Amount > 0 && !result.IsDamageOverTime)
            {
                float taken = DamageTakenPercent;
                if (taken != 0f)
                {
                    result.Amount = Mathf.Max(1, Mathf.RoundToInt(result.Amount * Mathf.Max(0.1f, 1f + taken / 100f)));
                }

                int through = Buffs.AbsorbDamage(result.Amount);
                result.Absorbed = result.Amount - through;
                result.Amount = through;
                if (through <= 0 && result.Absorbed > 0)
                {
                    result.IsBlocked = true;
                }
            }

            if (result.Amount > 0)
            {
                Hp = Mathf.Max(0, Hp - result.Amount);
            }

            OnDamaged(result, attacker);
            Damaged?.Invoke(result, attacker);
            AnyDamaged?.Invoke(this, result, attacker);
            RaiseVitalsChanged();

            // Death resolves before the reflect: a reflected kill (EXP, a level-up refill) must not save a lethal hit.
            if (Hp <= 0 && result.Amount > 0)
            {
                OnHpDepleted(attacker);
                Reflect(result, attacker, physicalMelee);
                return result;
            }

            Reflect(result, attacker, physicalMelee);

            if (result.Amount > 0 && !result.IsDamageOverTime)
            {
                Statuses.BreakOnDamage();
                ApplyPoiseDamage(poiseDamage);
            }

            return result;
        }

        /// <summary>Sends part of a melee hit or spell back at its source as fixed damage (which never reflects again).</summary>
        private void Reflect(DamageResult result, CombatEntity attacker, bool physicalMelee)
        {
            if (attacker == null || attacker == this || attacker.IsDead || result.Amount <= 0 || result.IsDamageOverTime)
            {
                return;
            }

            float percent = (physicalMelee ? ReflectMeleePercent : 0f) + (result.IsMagical ? ReflectMagicPercent : 0f);
            if (percent <= 0f)
            {
                return;
            }

            int amount = Mathf.Max(1, Mathf.RoundToInt(result.Amount * percent / 100f));
            attacker.ReceiveDamage(DamageResult.Fixed(amount), this, physicalMelee: false);
        }

        /// <summary>Chips poise; at zero the entity is staggered (unless immune).</summary>
        public void ApplyPoiseDamage(float amount)
        {
            if (IsDead || amount <= 0f || Buffs.HasTrait(BuffTraits.CrowdControlImmune)
                || StatusResistances.IsImmuneTo(StatusEffect.Stagger))
            {
                return;
            }

            if (Poise.Apply(amount, Time.timeAsDouble, !StaggerImmune))
            {
                ApplyStatus(StatusEffect.Stagger, PoiseRules.StaggerSeconds);
            }
        }

        public void Heal(int amount, bool showNumber = true)
        {
            if (IsDead || amount <= 0)
            {
                return;
            }

            int before = Hp;
            Hp = Mathf.Min(MaxHp, Hp + amount);
            if (showNumber)
            {
                AnyHealed?.Invoke(this, Hp - before, false);
            }

            RaiseVitalsChanged();
        }

        public void RestoreSp(int amount, bool showNumber = true)
        {
            if (IsDead || amount <= 0)
            {
                return;
            }

            int before = Sp;
            Sp = Mathf.Min(MaxSp, Sp + amount);
            if (showNumber)
            {
                AnyHealed?.Invoke(this, Sp - before, true);
            }

            RaiseVitalsChanged();
        }

        public bool TrySpendSp(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (Sp < amount)
            {
                return false;
            }

            Sp -= amount;
            RaiseVitalsChanged();
            return true;
        }

        /// <summary>Pays HP for a skill (Radiant Cross). Never kills: leaves at least 1 HP.</summary>
        public void PayHp(int amount)
        {
            if (IsDead || amount <= 0)
            {
                return;
            }

            Hp = Mathf.Max(1, Hp - amount);
            RaiseVitalsChanged();
        }

        /// <summary>Removes up to <paramref name="amount"/> SP (Abyssal Leech); returns how much was taken.</summary>
        public int DrainSp(int amount)
        {
            int drained = Mathf.Clamp(amount, 0, Sp);
            if (drained > 0)
            {
                Sp -= drained;
                RaiseVitalsChanged();
            }

            return drained;
        }

        /// <summary>Empties SP and returns how much there was (Fist of Odin).</summary>
        public int DrainAllSp()
        {
            int drained = Sp;
            Sp = 0;
            RaiseVitalsChanged();
            return drained;
        }

        /// <summary>Rolls the status against this entity's resistances; on success applies it for the resisted duration.</summary>
        public bool TryApplyStatus(StatusEffect status, float chancePercent, float seconds)
        {
            if (IsDead || status == StatusEffect.None)
            {
                return false;
            }

            var resist = StatusResistances;
            if (!StatusRules.Roll(status, chancePercent, resist, SystemRandomSource.Shared))
            {
                return false;
            }

            return ApplyStatus(status, StatusRules.EffectiveDuration(status, seconds, resist));
        }

        /// <summary>
        /// Applies a status with no chance roll (stagger, guaranteed stuns). Sowilo's ward always blocks it; status-immune
        /// targets (MVPs, or a card's immunity to that one status) block it unless <paramref name="ignoreImmunity"/> (GM commands).
        /// </summary>
        public bool ApplyStatus(StatusEffect status, float seconds, bool ignoreImmunity = false)
        {
            if (IsDead || seconds <= 0f || status == StatusEffect.None || Buffs.HasTrait(BuffTraits.CrowdControlImmune)
                || (!ignoreImmunity && StatusResistances.IsImmuneTo(status)))
            {
                return false;
            }

            if (!Statuses.Apply(status, seconds, Time.timeAsDouble))
            {
                return false;
            }

            OnStatusApplied(status);
            AnyStatusApplied?.Invoke(this, status);
            return true;
        }

        public void ClearCrowdControl()
        {
            Statuses.Clear();
        }

        /// <summary>Purify / Sowilo: removes every negative status and every debuff.</summary>
        public void Cleanse()
        {
            Statuses.Clear();
            Buffs.RemoveWhere(b => b.Definition.IsDebuff);
        }

        public void Knockback(Vector3 direction, float distance)
        {
            if (!CanBeKnockedBack || IsDead)
            {
                return;
            }

            var motor = GetComponent<NavMotor>();
            if (motor != null)
            {
                motor.Knockback(direction, distance);
            }
        }

        /// <summary>Frozen targets take extra blunt damage (GDD Glacial Tempest combo).</summary>
        protected float BluntDamageTakenMultiplier => IsFrozen ? StatFormulas.FrozenBluntDamageMultiplier : 1f;

        protected void SetVitals(int hp, int maxHp, int sp, int maxSp)
        {
            MaxHp = Mathf.Max(1, maxHp);
            MaxSp = Mathf.Max(1, maxSp);
            Hp = Mathf.Clamp(hp, 0, MaxHp);
            Sp = Mathf.Clamp(sp, 0, MaxSp);
            RaiseVitalsChanged();
        }

        protected void RaiseVitalsChanged()
        {
            VitalsChanged?.Invoke();
        }

        protected virtual void OnDamaged(DamageResult result, CombatEntity attacker)
        {
        }

        protected virtual void OnStatusApplied(StatusEffect effect)
        {
        }

        /// <summary>Default: die. Training dummies override this to refill instead.</summary>
        protected virtual void OnHpDepleted(CombatEntity killer)
        {
            Die(killer);
        }

        protected void Die(CombatEntity killer)
        {
            if (IsDead)
            {
                return;
            }

            IsDead = true;
            Hp = 0;
            Buffs.Clear();
            Statuses.Clear();
            Poise.Reset();
            OnDied(killer);
            Died?.Invoke(killer);
            AnyDied?.Invoke(this, killer);
            RaiseVitalsChanged();
        }

        protected virtual void OnDied(CombatEntity killer)
        {
        }

        protected void Revive(int hp, int sp)
        {
            IsDead = false;
            Hp = Mathf.Clamp(hp, 1, MaxHp);
            Sp = Mathf.Clamp(sp, 0, MaxSp);
            RaiseVitalsChanged();
        }

        protected virtual void OnEnable()
        {
            Registry.Add(this);

            // Subscribed up front so a debuff landing before anything reads the modifiers still reaches
            // OnModifiersChanged (a Bog of Niflheim slow on a monster that hasn't swung yet).
            WatchModifiers();
        }

        private void WatchModifiers()
        {
            if (_watchingModifiers)
            {
                return;
            }

            _watchingModifiers = true;
            Buffs.Changed += MarkModifiersDirty;
            Statuses.Changed += MarkModifiersDirty;
        }

        protected virtual void OnDisable()
        {
            Registry.Remove(this);
        }

        protected virtual void OnDestroy()
        {
            if (_watchingModifiers)
            {
                Buffs.Changed -= MarkModifiersDirty;
                Statuses.Changed -= MarkModifiersDirty;
            }
        }

        protected virtual void Update()
        {
            double now = Time.timeAsDouble;
            Buffs.Tick(now);
            _dueDots.Clear();
            Statuses.Tick(now, _dueDots);
            Poise.Tick(now);
            foreach (float percent in _dueDots)
            {
                TakeDamageOverTime(percent);
            }
        }

        /// <summary>Poison/bleeding: percent of max HP, never below 1 HP.</summary>
        private void TakeDamageOverTime(float percentOfMaxHp)
        {
            if (IsDead || Hp <= 1)
            {
                return;
            }

            int amount = Mathf.Min(Hp - 1, Mathf.Max(1, Mathf.RoundToInt(MaxHp * percentOfMaxHp / 100f)));
            var result = DamageResult.Fixed(amount);
            result.IsDamageOverTime = true;
            ReceiveDamage(result, null, physicalMelee: false);
        }

        private void MarkModifiersDirty()
        {
            _modifiersDirty = true;
            OnModifiersChanged();
        }

        /// <summary>Buffs or statuses changed (monsters re-apply move speed here).</summary>
        protected virtual void OnModifiersChanged()
        {
        }
    }
}
