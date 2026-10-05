using System;
using System.Collections.Generic;
using Runeheir.Combat;
using Runeheir.Movement;
using Runeheir.Session;
using Runeheir.Skills;
using Runeheir.Visuals;
using UnityEngine;

namespace Runeheir.Player
{
    /// <summary>
    /// Ragnarok-style skill use: press a hotkey → (target cursor) → walk into range → cast bar
    /// (variable cast time × DEX multiplier, 150 DEX = instant) → SP cost → effect → after-cast delay
    /// and per-skill cooldown. Damage interrupts the cast unless the caster is uninterruptible.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerCharacter))]
    public sealed class SkillCaster : MonoBehaviour
    {
        [Tooltip("Pressing an Enemy/Friend skill while hovering a valid target casts immediately (no extra click).")]
        [SerializeField] private bool quickCastOnHover = true;

        [Tooltip("Ground skills (Glacial Tempest, Aether Snap) cast at the cursor immediately instead of showing the AoE ring first.")]
        [SerializeField] private bool quickCastGround;

        [SerializeField, Min(0.05f)] private float approachRepathInterval = 0.2f;

        private readonly Dictionary<string, float> _cooldownEnds = new Dictionary<string, float>();
        private readonly Dictionary<string, float> _cooldownTotals = new Dictionary<string, float>();

        private PlayerCharacter _owner;
        private NavMotor _motor;
        private AutoAttacker _attacker;
        private CharacterAnimationBridge _animation;

        private float _globalDelayEnds;
        private float _globalDelayTotal;

        private SkillDefinition _approachSkill;
        private CombatEntity _approachTarget;
        private Vector3 _approachPoint;
        private float _nextApproachRepath;

        private SkillDefinition _castingSkill;
        private CombatEntity _castTarget;
        private Vector3 _castPoint;
        private float _castStarted;
        private float _castEnds;

        /// <summary>Targeting started/ended, cast started/ended, cooldowns changed.</summary>
        public event Action StateChanged;

        public SkillDefinition TargetingSkill { get; private set; }

        public bool IsTargeting => TargetingSkill != null;

        public bool IsCasting => _castingSkill != null;

        public SkillDefinition CastingSkill => _castingSkill;

        public float CastProgress => IsCasting ? Mathf.InverseLerp(_castStarted, _castEnds, Time.time) : 0f;

        public float CastRemaining => IsCasting ? Mathf.Max(0f, _castEnds - Time.time) : 0f;

        /// <summary>Hotkey pressed (or skill double-clicked in the skill window).</summary>
        public void RequestSkill(string skillId, CombatEntity hovered, Vector3? hoveredGround)
        {
            var skill = SkillCatalog.Get(skillId);
            if (skill == null)
            {
                return;
            }

            if (!CanStart(skill, out string reason))
            {
                if (reason != null)
                {
                    ChatLog.Error(reason);
                }

                return;
            }

            switch (skill.Target)
            {
                case SkillTarget.Self:
                    BeginOrApproach(skill, _owner, _owner.Position);
                    break;

                case SkillTarget.Enemy:
                    if (quickCastOnHover && hovered != null && _owner.IsHostileTo(hovered) && !hovered.IsDead)
                    {
                        BeginOrApproach(skill, hovered, hovered.Position);
                    }
                    else
                    {
                        EnterTargeting(skill);
                    }

                    break;

                case SkillTarget.Friend:
                    if (quickCastOnHover && hovered != null && IsFriendly(hovered))
                    {
                        BeginOrApproach(skill, hovered, hovered.Position);
                    }
                    else
                    {
                        EnterTargeting(skill);
                    }

                    break;

                case SkillTarget.Ground:
                    if (quickCastGround && hoveredGround.HasValue)
                    {
                        BeginOrApproach(skill, null, hoveredGround.Value);
                    }
                    else
                    {
                        EnterTargeting(skill);
                    }

                    break;
            }
        }

        /// <summary>Left-click on an entity while the target cursor is up.</summary>
        public void ConfirmTarget(CombatEntity target)
        {
            var skill = TargetingSkill;
            if (skill == null || target == null || target.IsDead)
            {
                return;
            }

            bool valid = skill.Target == SkillTarget.Enemy ? _owner.IsHostileTo(target) : skill.Target == SkillTarget.Friend && IsFriendly(target);
            if (!valid)
            {
                ChatLog.Error(skill.Target == SkillTarget.Enemy ? "Invalid target: choose an enemy." : "Invalid target: choose yourself or an ally.");
                return;
            }

            ExitTargeting();
            BeginOrApproach(skill, target, target.Position);
        }

        /// <summary>Left-click on the ground while the target cursor is up.</summary>
        public void ConfirmGround(Vector3 point)
        {
            var skill = TargetingSkill;
            if (skill == null || skill.Target != SkillTarget.Ground)
            {
                return;
            }

            ExitTargeting();
            BeginOrApproach(skill, null, point);
        }

        public void CancelTargeting()
        {
            ExitTargeting();
        }

        public void CancelAll()
        {
            ExitTargeting();
            _approachSkill = null;
            _approachTarget = null;
            if (IsCasting)
            {
                EndCast();
            }
        }

        /// <summary>Called by <see cref="PlayerCharacter"/> when it takes damage.</summary>
        public void InterruptByDamage()
        {
            if (IsCasting && !_owner.Stats.UninterruptibleCasting)
            {
                ChatLog.Error($"{_castingSkill.Name} was interrupted!");
                EndCast();
            }
        }

        /// <summary>Remaining/total cooldown for hotkey overlays (max of the skill's own and the after-cast delay).</summary>
        public float GetCooldown(string skillId, out float total)
        {
            float now = Time.time;
            float remaining = Mathf.Max(0f, _globalDelayEnds - now);
            total = _globalDelayTotal;
            if (skillId != null && _cooldownEnds.TryGetValue(skillId, out float ends) && ends - now > remaining)
            {
                remaining = ends - now;
                total = _cooldownTotals[skillId];
            }

            return remaining;
        }

        private bool IsFriendly(CombatEntity entity)
        {
            return entity != null && !entity.IsDead && (entity == _owner || entity.Faction == _owner.Faction);
        }

        private bool CanStart(SkillDefinition skill, out string reason)
        {
            reason = null;
            if (_owner.IsDead)
            {
                return false;
            }

            if (!SkillCatalog.CanUse(_owner.Record.Job, skill.Id))
            {
                reason = $"{_owner.Job.Name}s cannot use {skill.Name}.";
                return false;
            }

            if (_owner.IsIncapacitated)
            {
                reason = "You can't act right now.";
                return false;
            }

            if (IsCasting)
            {
                return false;
            }

            if (Time.time < _globalDelayEnds)
            {
                // Ragnarok silently ignores skills during after-cast delay.
                return false;
            }

            if (_cooldownEnds.TryGetValue(skill.Id, out float ends) && Time.time < ends)
            {
                reason = $"{skill.Name} is not ready ({ends - Time.time:0.0}s).";
                return false;
            }

            if (_owner.Sp < skill.SpCost)
            {
                reason = "Not enough SP.";
                return false;
            }

            return true;
        }

        private void EnterTargeting(SkillDefinition skill)
        {
            // A new cursor replaces any walk-into-range still pending from an earlier skill.
            _approachSkill = null;
            _approachTarget = null;
            TargetingSkill = skill;
            StateChanged?.Invoke();
        }

        private void ExitTargeting()
        {
            if (TargetingSkill == null)
            {
                return;
            }

            TargetingSkill = null;
            StateChanged?.Invoke();
        }

        private float DistanceTo(SkillDefinition skill, CombatEntity target, Vector3 point)
        {
            if (skill.Target == SkillTarget.Self || target == _owner)
            {
                return 0f;
            }

            return target != null ? _owner.EdgeDistanceTo(target) : _owner.EdgeDistanceTo(point);
        }

        private void BeginOrApproach(SkillDefinition skill, CombatEntity target, Vector3 point)
        {
            // Whatever starts now (quick-cast, self-cast, confirmed cursor) replaces an open target cursor.
            ExitTargeting();

            // Dashes go as far as they can instead of walking into range first.
            if (skill.Effect == SkillEffect.Dash || DistanceTo(skill, target, point) <= skill.Range)
            {
                BeginCast(skill, target, point);
                return;
            }

            _approachSkill = skill;
            _approachTarget = target;
            _approachPoint = point;
            _nextApproachRepath = 0f;
        }

        private void BeginCast(SkillDefinition skill, CombatEntity target, Vector3 point)
        {
            _approachSkill = null;
            _approachTarget = null;
            if (!CanStart(skill, out string reason))
            {
                if (reason != null)
                {
                    ChatLog.Error(reason);
                }

                return;
            }

            _motor.Stop();
            if (target != null && target != _owner)
            {
                _motor.FaceTowards(target.Position, instant: true);
            }
            else if (skill.Target == SkillTarget.Ground)
            {
                _motor.FaceTowards(point, instant: true);
            }

            float castTime = skill.CastTime * _owner.Stats.CastTimeMultiplier;
            if (castTime <= 0.01f)
            {
                Execute(skill, target, point);
                return;
            }

            _castingSkill = skill;
            _castTarget = target;
            _castPoint = point;
            _castStarted = Time.time;
            _castEnds = Time.time + castTime;
            if (_animation != null)
            {
                _animation.SetCasting(true);
            }

            if (skill.Target == SkillTarget.Ground)
            {
                GroundRing.SpawnPulse(point, new Color(0.5f, 0.85f, 1f, 0.9f), skill.Radius, skill.Radius, castTime, 0.08f);
            }

            StateChanged?.Invoke();
        }

        private void EndCast()
        {
            _castingSkill = null;
            _castTarget = null;
            if (_animation != null)
            {
                _animation.SetCasting(false);
            }

            StateChanged?.Invoke();
        }

        private void Execute(SkillDefinition skill, CombatEntity target, Vector3 point)
        {
            if (target != null && target != _owner && target.IsDead)
            {
                return;
            }

            if (!_owner.TrySpendSp(skill.SpCost))
            {
                ChatLog.Error("Not enough SP.");
                return;
            }

            float now = Time.time;
            _globalDelayTotal = skill.AfterCastDelay;
            _globalDelayEnds = now + skill.AfterCastDelay;
            if (skill.Cooldown > 0f)
            {
                _cooldownEnds[skill.Id] = now + skill.Cooldown;
                _cooldownTotals[skill.Id] = skill.Cooldown;
            }

            WorldFeedback.Announce(_owner, skill.Name + "!!", new Color(1f, 0.93f, 0.6f));
            if (_animation != null && (skill.Effect == SkillEffect.PhysicalStrike || skill.Effect == SkillEffect.PhysicalAreaAroundSelf || skill.Effect == SkillEffect.FistOfOdin))
            {
                _animation.PlayAttack(_owner.AttackPlayRate, Mathf.Min(0.35f, _owner.SwingDuration));
            }

            CombatEntity effectTarget = target != null ? target : skill.Target == SkillTarget.Self ? _owner : null;
            StartCoroutine(SkillEffects.Run(_owner, skill, effectTarget, point));
            StateChanged?.Invoke();
        }

        private void Awake()
        {
            _owner = GetComponent<PlayerCharacter>();
            _motor = GetComponent<NavMotor>();
            _attacker = GetComponent<AutoAttacker>();
            _animation = GetComponent<CharacterAnimationBridge>();
        }

        private void Update()
        {
            if (_owner.Record == null)
            {
                return;
            }

            if (_owner.IsDead)
            {
                if (IsCasting || IsTargeting || _approachSkill != null)
                {
                    CancelAll();
                }

                return;
            }

            UpdateApproach();
            UpdateCast();
        }

        private void UpdateApproach()
        {
            if (_approachSkill == null || _owner.IsIncapacitated)
            {
                return;
            }

            if (_approachTarget != null && _approachTarget.IsDead)
            {
                _approachSkill = null;
                _approachTarget = null;
                return;
            }

            Vector3 point = _approachTarget != null ? _approachTarget.Position : _approachPoint;
            if (DistanceTo(_approachSkill, _approachTarget, point) <= _approachSkill.Range)
            {
                BeginCast(_approachSkill, _approachTarget, point);
                return;
            }

            if (Time.time >= _nextApproachRepath)
            {
                _attacker.Disengage();
                _motor.MoveTo(point);
                _nextApproachRepath = Time.time + approachRepathInterval;
            }
        }

        private void UpdateCast()
        {
            if (!IsCasting)
            {
                return;
            }

            if (_owner.IsIncapacitated)
            {
                ChatLog.Error($"{_castingSkill.Name} was interrupted!");
                EndCast();
                return;
            }

            if (_castTarget != null && _castTarget != _owner && _castTarget.IsDead)
            {
                EndCast();
                return;
            }

            if (Time.time >= _castEnds)
            {
                var skill = _castingSkill;
                var target = _castTarget;
                var point = _castTarget != null ? _castTarget.Position : _castPoint;
                EndCast();
                Execute(skill, target, point);
            }
        }

        /// <summary>A movement click cancels a pending "walk into range" (not an in-progress cast).</summary>
        public void CancelApproach()
        {
            _approachSkill = null;
            _approachTarget = null;
        }
    }
}
