using System;
using System.Collections.Generic;
using Runeheir.Movement;
using Runeheir.Stats;
using UnityEngine;

namespace Runeheir.Combat
{
    /// <summary>
    /// Anything that has HP and can fight: players and monsters. Owns HP/SP, buffs, stun/freeze and
    /// damage intake. Damage math itself lives in the engine-free <see cref="DamageCalculator"/>, so it
    /// can move to a Mirror server unchanged in Phase 6.
    /// </summary>
    public abstract class CombatEntity : MonoBehaviour
    {
        private static readonly List<CombatEntity> Registry = new List<CombatEntity>();

        [SerializeField, Min(0.1f)] protected float bodyRadius = 0.45f;
        [SerializeField, Min(0.1f)] protected float bodyHeight = 1.8f;

        private float _stunnedUntil;
        private float _frozenUntil;

        /// <summary>(target, result, attacker) — floating damage numbers and logs listen here.</summary>
        public static event Action<CombatEntity, DamageResult, CombatEntity> AnyDamaged;

        /// <summary>(target, amount, isSp).</summary>
        public static event Action<CombatEntity, int, bool> AnyHealed;

        /// <summary>(victim, killer).</summary>
        public static event Action<CombatEntity, CombatEntity> AnyDied;

        public event Action<DamageResult, CombatEntity> Damaged;

        public event Action<CombatEntity> Died;

        /// <summary>HP, SP or their maximums changed.</summary>
        public event Action VitalsChanged;

        public static IReadOnlyList<CombatEntity> All => Registry;

        public BuffContainer Buffs { get; } = new BuffContainer();

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

        public bool IsStunned => Time.time < _stunnedUntil;

        public bool IsFrozen => Time.time < _frozenUntil;

        public bool IsIncapacitated => IsDead || IsStunned || IsFrozen;

        public virtual bool CanBeKnockedBack => true;

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

        public bool IsHostileTo(CombatEntity other)
        {
            return other != null && other != this && Faction != Faction.Neutral && other.Faction != Faction.Neutral && Faction != other.Faction;
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

        public void ReceiveDamage(DamageResult result, CombatEntity attacker, bool physicalMelee)
        {
            if (IsDead)
            {
                return;
            }

            if (physicalMelee && !result.IsMiss && Buffs.TryConsumeCharge(BuffTraits.MeleeBlockCharges))
            {
                result = DamageResult.Blocked();
            }

            if (result.Amount > 0)
            {
                Hp = Mathf.Max(0, Hp - result.Amount);
            }

            OnDamaged(result, attacker);
            Damaged?.Invoke(result, attacker);
            AnyDamaged?.Invoke(this, result, attacker);
            RaiseVitalsChanged();

            if (Hp <= 0 && result.Amount > 0)
            {
                OnHpDepleted(attacker);
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

        /// <summary>Empties SP and returns how much there was (Fist of Odin).</summary>
        public int DrainAllSp()
        {
            int drained = Sp;
            Sp = 0;
            RaiseVitalsChanged();
            return drained;
        }

        public void ApplyStatus(StatusEffect effect, float duration)
        {
            if (IsDead || duration <= 0f || Buffs.HasTrait(BuffTraits.CrowdControlImmune))
            {
                return;
            }

            switch (effect)
            {
                case StatusEffect.Stun:
                    _stunnedUntil = Mathf.Max(_stunnedUntil, Time.time + duration);
                    break;
                case StatusEffect.Freeze:
                    _frozenUntil = Mathf.Max(_frozenUntil, Time.time + duration);
                    break;
                default:
                    return;
            }

            OnStatusApplied(effect);
        }

        public void ClearCrowdControl()
        {
            _stunnedUntil = 0f;
            _frozenUntil = 0f;
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
            ClearCrowdControl();
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
        }

        protected virtual void OnDisable()
        {
            Registry.Remove(this);
        }

        protected virtual void Update()
        {
            Buffs.Tick(Time.timeAsDouble);
        }
    }
}
