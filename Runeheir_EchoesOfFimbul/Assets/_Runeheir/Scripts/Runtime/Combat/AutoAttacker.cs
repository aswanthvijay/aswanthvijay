using System;
using Runeheir.Movement;
using Runeheir.Stats;
using Runeheir.Visuals;
using UnityEngine;

namespace Runeheir.Combat
{
    /// <summary>
    /// Ragnarok/XileRO click-to-attack loop shared by players and monsters:
    /// chase the target until in range → stop → face → swing → damage lands on the impact frame →
    /// wait out the ASPD-derived interval → swing again (continuous, like /noctrl) until told otherwise.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMotor))]
    public sealed class AutoAttacker : MonoBehaviour
    {
        [SerializeField, Min(0.05f)] private float repathInterval = 0.2f;

        [Tooltip("Extra reach allowed when the hit lands, so a target stepping away mid-swing still gets hit.")]
        [SerializeField, Min(0f)] private float rangeTolerance = 0.6f;

        private CombatEntity _owner;
        private NavMotor _motor;
        private CharacterAnimationBridge _animation;
        private Func<bool> _isBusy;

        private float _nextSwingAt;
        private float _swingEndsAt;
        private float _impactAt;
        private float _nextRepathAt;
        private bool _impactPending;
        private CombatEntity _impactTarget;

        public event Action<CombatEntity> TargetChanged;

        public CombatEntity Target { get; private set; }

        /// <summary>Keep attacking after each hit (XileRO default). False = one hit per click.</summary>
        public bool Continuous { get; set; } = true;

        public bool IsSwinging => Time.time < _swingEndsAt;

        /// <summary>Pauses the loop while something else (casting) owns the character.</summary>
        public void SetBusyCheck(Func<bool> isBusy)
        {
            _isBusy = isBusy;
        }

        public void Engage(CombatEntity target, bool continuous = true)
        {
            if (target == null || target.IsDead || !_owner.IsHostileTo(target))
            {
                return;
            }

            Continuous = continuous;
            _nextRepathAt = 0f;
            if (Target != target)
            {
                Target = target;
                TargetChanged?.Invoke(target);
            }
        }

        public void Disengage()
        {
            _impactPending = false;
            _impactTarget = null;
            if (Target == null)
            {
                return;
            }

            Target = null;
            TargetChanged?.Invoke(null);
        }

        private void Awake()
        {
            _owner = GetComponent<CombatEntity>();
            _motor = GetComponent<NavMotor>();
            _animation = GetComponent<CharacterAnimationBridge>();
        }

        private void Update()
        {
            if (_impactPending && Time.time >= _impactAt)
            {
                ResolveImpact();
            }

            if (Target == null)
            {
                return;
            }

            if (_owner.IsDead || Target.IsDead || !Target.isActiveAndEnabled)
            {
                Disengage();
                return;
            }

            if (_owner.IsIncapacitated || (_isBusy != null && _isBusy()) || IsSwinging)
            {
                return;
            }

            if (_owner.EdgeDistanceTo(Target) > _owner.AttackRange)
            {
                if (Time.time >= _nextRepathAt)
                {
                    _motor.MoveTo(Target.Position);
                    _nextRepathAt = Time.time + repathInterval;
                }

                return;
            }

            _motor.Stop();
            _motor.FaceTowards(Target.Position);
            if (Time.time >= _nextSwingAt)
            {
                BeginSwing();
            }
        }

        private void BeginSwing()
        {
            float swing = Mathf.Max(0.05f, _owner.SwingDuration);
            float interval = Mathf.Max(swing, _owner.AttackInterval);
            float now = Time.time;

            // While attacking continuously, start from the scheduled time when it fell inside the last frame,
            // so frame overshoot never accumulates and the real attack rate matches the ASPD table at any FPS.
            float start = _nextSwingAt > 0f && now - _nextSwingAt <= Time.deltaTime ? _nextSwingAt : now;

            _swingEndsAt = start + swing;
            _impactAt = start + swing * StatFormulas.ImpactFrameFraction;
            _nextSwingAt = start + interval;
            _impactPending = true;
            _impactTarget = Target;

            if (_animation != null)
            {
                _animation.PlayAttack(_owner.AttackPlayRate, swing);
            }
        }

        private void ResolveImpact()
        {
            _impactPending = false;
            var target = _impactTarget;
            _impactTarget = null;
            // A stun/freeze that lands mid-swing cancels the hit.
            if (target == null || target.IsDead || _owner.IsDead || _owner.IsIncapacitated)
            {
                return;
            }

            if (_owner.EdgeDistanceTo(target) > _owner.AttackRange + rangeTolerance)
            {
                return;
            }

            var result = _owner.RollBasicAttack(target);
            target.ReceiveDamage(result, _owner, physicalMelee: !_owner.IsRangedAttacker);

            if (!Continuous && Target == target)
            {
                Disengage();
            }
        }
    }
}
