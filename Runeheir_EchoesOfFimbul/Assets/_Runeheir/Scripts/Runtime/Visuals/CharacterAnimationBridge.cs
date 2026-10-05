using Runeheir.Movement;
using UnityEngine;

namespace Runeheir.Visuals
{
    /// <summary>
    /// The only thing gameplay code talks to for animation. Drives a real <see cref="Animator"/> when the
    /// model has one (parameters are optional; missing ones are skipped) and the procedural
    /// <see cref="PlaceholderAvatar"/> otherwise.
    ///
    /// Animator setup for real models (Phase 3):
    ///   Parameters: Speed (float), AttackSpeed (float), Attack (trigger), Casting (bool), Dead (bool), Hit (trigger)
    ///   On the Attack state: Speed = 1, Multiplier → tick "Parameter" and pick AttackSpeed.
    ///   That makes ASPD (via <see cref="AspdAnimationScaler"/>) scale only the attack clip, 1.0x–3.0x.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterAnimationBridge : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        [Header("Animator parameter names")]
        [SerializeField] private string speedParameter = "Speed";
        [SerializeField] private string attackSpeedParameter = "AttackSpeed";
        [SerializeField] private string attackTrigger = "Attack";
        [SerializeField] private string castingParameter = "Casting";
        [SerializeField] private string deadParameter = "Dead";
        [SerializeField] private string hitTrigger = "Hit";

        [Header("High-ASPD handling")]
        [Tooltip("Restart the attack state every swing so fast ASPD never waits for the previous clip to finish.")]
        [SerializeField] private bool restartAttackStateEachSwing = true;
        [SerializeField] private string attackStateName = "Attack";
        [SerializeField] private int attackLayer;

        private PlaceholderAvatar _placeholder;
        private NavMotor _motor;
        private int _speedHash;
        private int _attackSpeedHash;
        private int _attackHash;
        private int _castingHash;
        private int _deadHash;
        private int _hitHash;
        private int _attackStateHash;
        private bool _hasSpeed;
        private bool _hasAttackSpeed;
        private bool _hasAttack;
        private bool _hasCasting;
        private bool _hasDead;
        private bool _hasHit;
        private bool _hasAttackState;

        public float AttackPlayRate { get; private set; } = 1f;

        public Animator Animator => animator;

        /// <summary>Re-scan the children for an Animator / placeholder (call after swapping models).</summary>
        public void Resolve()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            _placeholder = GetComponentInChildren<PlaceholderAvatar>();
            _motor = GetComponent<NavMotor>();

            _hasSpeed = _hasAttackSpeed = _hasAttack = _hasCasting = _hasDead = _hasHit = _hasAttackState = false;
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            _speedHash = Animator.StringToHash(speedParameter);
            _attackSpeedHash = Animator.StringToHash(attackSpeedParameter);
            _attackHash = Animator.StringToHash(attackTrigger);
            _castingHash = Animator.StringToHash(castingParameter);
            _deadHash = Animator.StringToHash(deadParameter);
            _hitHash = Animator.StringToHash(hitTrigger);
            _attackStateHash = Animator.StringToHash(attackStateName);

            foreach (var parameter in animator.parameters)
            {
                if (parameter.nameHash == _speedHash && parameter.type == AnimatorControllerParameterType.Float) _hasSpeed = true;
                if (parameter.nameHash == _attackSpeedHash && parameter.type == AnimatorControllerParameterType.Float) _hasAttackSpeed = true;
                if (parameter.nameHash == _attackHash && parameter.type == AnimatorControllerParameterType.Trigger) _hasAttack = true;
                if (parameter.nameHash == _castingHash && parameter.type == AnimatorControllerParameterType.Bool) _hasCasting = true;
                if (parameter.nameHash == _deadHash && parameter.type == AnimatorControllerParameterType.Bool) _hasDead = true;
                if (parameter.nameHash == _hitHash && parameter.type == AnimatorControllerParameterType.Trigger) _hasHit = true;
            }

            _hasAttackState = animator.isInitialized && animator.HasState(attackLayer, _attackStateHash);
        }

        public void SetAttackPlayRate(float playRate)
        {
            AttackPlayRate = Mathf.Max(0.05f, playRate);
            if (_hasAttackSpeed)
            {
                animator.SetFloat(_attackSpeedHash, AttackPlayRate);
            }
        }

        /// <summary>One basic attack. <paramref name="swingSeconds"/> is already ASPD-scaled.</summary>
        public void PlayAttack(float playRate, float swingSeconds)
        {
            SetAttackPlayRate(playRate);
            if (animator != null)
            {
                if (restartAttackStateEachSwing && _hasAttackState)
                {
                    animator.Play(_attackStateHash, attackLayer, 0f);
                }
                else if (_hasAttack)
                {
                    animator.SetTrigger(_attackHash);
                }
            }

            if (_placeholder != null)
            {
                _placeholder.PlayAttack(swingSeconds);
            }
        }

        public void SetCasting(bool casting)
        {
            if (_hasCasting)
            {
                animator.SetBool(_castingHash, casting);
            }

            if (_placeholder != null)
            {
                _placeholder.SetCasting(casting);
            }
        }

        public void SetDead(bool dead)
        {
            if (_hasDead)
            {
                animator.SetBool(_deadHash, dead);
            }

            if (_placeholder != null)
            {
                _placeholder.SetDead(dead);
            }
        }

        public void PlayHit()
        {
            if (_hasHit)
            {
                animator.SetTrigger(_hitHash);
            }

            if (_placeholder != null)
            {
                _placeholder.PlayHit();
            }
        }

        private void Start()
        {
            Resolve();
        }

        private void Update()
        {
            float speed = _motor != null ? _motor.Velocity.magnitude : 0f;
            if (_hasSpeed)
            {
                animator.SetFloat(_speedHash, speed);
            }

            if (_placeholder != null)
            {
                _placeholder.SetLocomotion(speed);
            }
        }
    }
}
