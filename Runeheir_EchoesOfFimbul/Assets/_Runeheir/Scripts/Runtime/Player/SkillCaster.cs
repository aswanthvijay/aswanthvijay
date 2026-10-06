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
    /// and per-skill cooldown. Skills fire at the learned level (or the level stored on the hotkey).
    /// Damage interrupts the cast unless the caster is uninterruptible; a stagger always does.
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

        /// <summary>Extra reach allowed when a cast completes, so a target shuffling half a step doesn't fizzle it.</summary>
        private const float CastRangeTolerance = 1.5f;

        private readonly Dictionary<string, float> _cooldownEnds = new Dictionary<string, float>();
        private readonly Dictionary<string, float> _cooldownTotals = new Dictionary<string, float>();

        private PlayerCharacter _owner;
        private NavMotor _motor;
        private AutoAttacker _attacker;
        private CharacterAnimationBridge _animation;

        private float _globalDelayEnds;
        private float _globalDelayTotal;

        private SkillDefinition _approachSkill;
        private int _approachLevel;
        private CombatEntity _approachTarget;
        private Vector3 _approachPoint;
        private float _nextApproachRepath;

        private SkillDefinition _castingSkill;
        private int _castingLevel;
        private CombatEntity _castTarget;
        private Vector3 _castPoint;
        private float _castStarted;
        private float _castEnds;

        /// <summary>Targeting started/ended, cast started/ended, cooldowns changed.</summary>
        public event Action StateChanged;

        public SkillDefinition TargetingSkill { get; private set; }

        /// <summary>The level the target cursor's skill will be cast at.</summary>
        public int TargetingLevel { get; private set; }

        public bool IsTargeting => TargetingSkill != null;

        public bool IsCasting => _castingSkill != null;

        public SkillDefinition CastingSkill => _castingSkill;

        public int CastingLevel => _castingLevel;

        public float CastProgress => IsCasting ? Mathf.InverseLerp(_castStarted, _castEnds, Time.time) : 0f;

        public float CastRemaining => IsCasting ? Mathf.Max(0f, _castEnds - Time.time) : 0f;

        /// <summary>Free Cast: the caster may walk while the cast bar fills.</summary>
        public bool CanMoveWhileCasting => _owner.Stats != null && _owner.Stats.CastMoveSpeedPercent > 0f;

        /// <summary>Hotkey pressed (or skill double-clicked in the skill window). <paramref name="requestedLevel"/> 0 = highest learned.</summary>
        public void RequestSkill(string skillId, CombatEntity hovered, Vector3? hoveredGround, int requestedLevel = 0)
        {
            var skill = SkillCatalog.Get(skillId);
            if (skill == null)
            {
                return;
            }

            int level = ResolveLevel(skill, requestedLevel);
            if (!CanStart(skill, level, out string reason))
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
                    BeginOrApproach(skill, level, _owner, _owner.Position);
                    break;

                case SkillTarget.Enemy:
                    if (quickCastOnHover && IsValidEnemy(hovered))
                    {
                        BeginOrApproach(skill, level, hovered, hovered.Position);
                    }
                    else
                    {
                        EnterTargeting(skill, level);
                    }

                    break;

                case SkillTarget.Friend:
                    if (quickCastOnHover && hovered != null && IsFriendly(hovered))
                    {
                        BeginOrApproach(skill, level, hovered, hovered.Position);
                    }
                    else
                    {
                        EnterTargeting(skill, level);
                    }

                    break;

                case SkillTarget.Ground:
                    if (quickCastGround && hoveredGround.HasValue)
                    {
                        BeginOrApproach(skill, level, null, hoveredGround.Value);
                    }
                    else
                    {
                        EnterTargeting(skill, level);
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

            bool valid = skill.Target == SkillTarget.Enemy ? IsValidEnemy(target) : skill.Target == SkillTarget.Friend && IsFriendly(target);
            if (!valid)
            {
                ChatLog.Error(skill.Target == SkillTarget.Enemy ? "Invalid target: choose an enemy." : "Invalid target: choose yourself or an ally.");
                return;
            }

            int level = TargetingLevel;
            ExitTargeting();
            BeginOrApproach(skill, level, target, target.Position);
        }

        /// <summary>Left-click on the ground while the target cursor is up.</summary>
        public void ConfirmGround(Vector3 point)
        {
            var skill = TargetingSkill;
            if (skill == null || skill.Target != SkillTarget.Ground)
            {
                return;
            }

            int level = TargetingLevel;
            ExitTargeting();
            BeginOrApproach(skill, level, null, point);
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

        /// <summary>Stagger, stun, freeze, sleep, stone, silence: the cast is lost even when uninterruptible by damage.</summary>
        public void InterruptHard(string why)
        {
            if (IsCasting)
            {
                ChatLog.Error($"{_castingSkill.Name} was interrupted ({why})!");
                EndCast();
            }
        }

        /// <summary>
        /// A free auto-cast from a proc (Storm Fists, Keen Edge, Auto Rune): no SP, cast time, delay or cooldown,
        /// and it never interrupts what the player is doing.
        /// </summary>
        public void CastProc(SkillDefinition skill, int level, CombatEntity target)
        {
            if (skill == null || _owner.IsDead || target == null || target.IsDead)
            {
                return;
            }

            var cast = new SkillCast
            {
                Caster = _owner,
                Skill = skill,
                Level = skill.ClampLevel(level),
                Target = target,
                Point = target.Position,
                IsProc = true,
            };
            WorldFeedback.Announce(_owner, skill.Name + "!", new Color(1f, 0.8f, 0.45f));
            if (_animation != null && skill.Motion != SkillMotion.None)
            {
                _animation.PlaySkill(skill.Motion, _owner.AttackPlayRate, Mathf.Min(0.3f, _owner.SwingDuration));
            }

            StartCoroutine(SkillEffects.Run(cast));
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

        /// <summary>The level a hotkey press fires at: the stored level, capped by what is learned (0 = not usable).</summary>
        public int ResolveLevel(SkillDefinition skill, int requestedLevel)
        {
            int learned = _owner.SkillBook.UsableLevel(skill.Id);
            return requestedLevel > 0 ? Mathf.Min(learned, requestedLevel) : learned;
        }

        private bool IsValidEnemy(CombatEntity entity)
        {
            return entity != null && !entity.IsDead && _owner.IsHostileTo(entity) && _owner.CanSee(entity);
        }

        private bool IsFriendly(CombatEntity entity)
        {
            return entity != null && !entity.IsDead && (entity == _owner || entity.Faction == _owner.Faction);
        }

        private bool CanStart(SkillDefinition skill, int level, out string reason)
        {
            reason = null;
            if (_owner.IsDead)
            {
                return false;
            }

            if (skill.Passive)
            {
                reason = $"{skill.Name} is a passive skill: it works on its own once learned.";
                return false;
            }

            if (!SkillCatalog.CanUse(_owner.Record.Job, skill.Id))
            {
                reason = $"{_owner.Job.Name}s cannot use {skill.Name}.";
                return false;
            }

            if (level <= 0)
            {
                reason = $"You haven't learned {skill.Name}. Open the Skill window (Alt+S) to spend skill points.";
                return false;
            }

            if (!WeaponMasks.Allows(skill.Weapons, _owner.Weapon.Type))
            {
                reason = $"{skill.Name} needs a {WeaponMasks.Describe(skill.Weapons)}.";
                return false;
            }

            if (_owner.IsIncapacitated)
            {
                reason = "You can't act right now.";
                return false;
            }

            if (!_owner.CanUseSkills)
            {
                reason = "You are silenced.";
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

            if (_owner.Sp < skill.SpCost.AtInt(level))
            {
                reason = "Not enough SP.";
                return false;
            }

            int spheresNeeded = skill.Special == SkillSpecial.SpiritRelease ? Mathf.Max(1, skill.SphereCost) : skill.SphereCost;
            if (spheresNeeded > 0 && _owner.Buffs.StacksOf(SkillBuffs.SpiritSpheres) < spheresNeeded)
            {
                reason = $"{skill.Name} needs {spheresNeeded} Spirit Sphere{(spheresNeeded > 1 ? "s" : string.Empty)} (Spirit Call).";
                return false;
            }

            float hpCost = skill.HpCostPercent.At(level);
            if (hpCost > 0f && _owner.Hp <= 1)
            {
                reason = "Not enough HP.";
                return false;
            }

            return true;
        }

        private void EnterTargeting(SkillDefinition skill, int level)
        {
            // A new cursor replaces any walk-into-range still pending from an earlier skill.
            _approachSkill = null;
            _approachTarget = null;
            TargetingSkill = skill;
            TargetingLevel = level;
            StateChanged?.Invoke();
        }

        private void ExitTargeting()
        {
            if (TargetingSkill == null)
            {
                return;
            }

            TargetingSkill = null;
            TargetingLevel = 0;
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

        private void BeginOrApproach(SkillDefinition skill, int level, CombatEntity target, Vector3 point)
        {
            // Whatever starts now (quick-cast, self-cast, confirmed cursor) replaces an open target cursor.
            ExitTargeting();

            // Dashes go as far as they can instead of walking into range first.
            if (skill.Special == SkillSpecial.Dash || DistanceTo(skill, target, point) <= skill.Range.At(level))
            {
                BeginCast(skill, level, target, point);
                return;
            }

            _approachSkill = skill;
            _approachLevel = level;
            _approachTarget = target;
            _approachPoint = point;
            _nextApproachRepath = 0f;
        }

        private void BeginCast(SkillDefinition skill, int level, CombatEntity target, Vector3 point)
        {
            _approachSkill = null;
            _approachTarget = null;
            if (!CanStart(skill, level, out string reason))
            {
                if (reason != null)
                {
                    ChatLog.Error(reason);
                }

                return;
            }

            if (!CanMoveWhileCasting)
            {
                _motor.Stop();
            }

            if (target != null && target != _owner)
            {
                _motor.FaceTowards(target.Position, instant: true);
            }
            else if (skill.Target == SkillTarget.Ground)
            {
                _motor.FaceTowards(point, instant: true);
            }

            float castTime = skill.CastTime.At(level) * _owner.Stats.CastTimeMultiplier;
            if (castTime <= 0.01f)
            {
                Execute(skill, level, target, point);
                return;
            }

            _castingSkill = skill;
            _castingLevel = level;
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
                float radius = Mathf.Max(0.6f, skill.Radius.At(level));
                GroundRing.SpawnPulse(point, new Color(0.5f, 0.85f, 1f, 0.9f), radius, radius, castTime, 0.08f);
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

        private void Execute(SkillDefinition skill, int level, CombatEntity target, Vector3 point)
        {
            if (target != null && target != _owner && target.IsDead)
            {
                return;
            }

            if (skill.SphereCost > 0 && _owner.Buffs.StacksOf(SkillBuffs.SpiritSpheres) < skill.SphereCost)
            {
                ChatLog.Error("Not enough Spirit Spheres.");
                return;
            }

            if (!_owner.TrySpendSp(skill.SpCost.AtInt(level)))
            {
                ChatLog.Error("Not enough SP.");
                return;
            }

            if (skill.SphereCost > 0)
            {
                _owner.Buffs.TakeStacks(SkillBuffs.SpiritSpheres, skill.SphereCost);
            }

            float hpCost = skill.HpCostPercent.At(level);
            if (hpCost > 0f)
            {
                _owner.PayHp(Mathf.RoundToInt(_owner.Hp * hpCost / 100f));
            }

            float now = Time.time;
            float delay = skill.AfterCastDelay.At(level);
            _globalDelayTotal = delay;
            _globalDelayEnds = now + delay;
            float cooldown = skill.Cooldown.At(level);
            if (cooldown > 0f)
            {
                _cooldownEnds[skill.Id] = now + cooldown;
                _cooldownTotals[skill.Id] = cooldown;
            }

            var cast = new SkillCast { Caster = _owner, Skill = skill, Level = level, Target = target, Point = point };

            // Using a skill reveals you (except recasting a stealth skill); Shadow Veil's ambush makes a skill that can crit, crit.
            bool keepsStealth = skill.BuffId != null && BuffCatalog.Get(skill.BuffId) is BuffDefinition buff && buff.Has(BuffTraits.Stealth);
            if (!keepsStealth && _owner.IsHidden)
            {
                _owner.Buffs.BreakStealth(out bool ambush);
                cast.Ambush = ambush;
            }

            // Rune Amplify boosts this one damaging spell, then ends.
            if (skill.IsMagic)
            {
                foreach (var active in _owner.Buffs.Active)
                {
                    if (active.Definition.Has(BuffTraits.ConsumedBySpell))
                    {
                        cast.BonusMagicPercent += active.Definition.Modifiers.MagicDamagePercent
                                                  + (active.Definition.ModifiersPerLevel?.MagicDamagePercent ?? 0f) * (active.Level - 1);
                    }
                }

                _owner.Buffs.RemoveWithTrait(BuffTraits.ConsumedBySpell);
            }

            string label = skill.MaxLevel > 1 ? $"{skill.Name} Lv {level}!!" : skill.Name + "!!";
            WorldFeedback.Announce(_owner, label, new Color(1f, 0.93f, 0.6f));
            if (_animation != null && skill.Motion != SkillMotion.None)
            {
                _animation.PlaySkill(skill.Motion, _owner.AttackPlayRate, Mathf.Min(0.35f, _owner.SwingDuration));
            }

            if (target == null && skill.Target == SkillTarget.Self)
            {
                cast.Target = _owner;
            }

            StartCoroutine(SkillEffects.Run(cast));
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

            if (_approachTarget != null && (_approachTarget.IsDead || !_owner.CanSee(_approachTarget)))
            {
                _approachSkill = null;
                _approachTarget = null;
                return;
            }

            Vector3 point = _approachTarget != null ? _approachTarget.Position : _approachPoint;
            if (DistanceTo(_approachSkill, _approachTarget, point) <= _approachSkill.Range.At(_approachLevel))
            {
                BeginCast(_approachSkill, _approachLevel, _approachTarget, point);
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
                InterruptHard(_owner.IsStaggered ? "staggered" : "can't act");
                return;
            }

            if (!_owner.CanUseSkills)
            {
                InterruptHard("silenced");
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
                int level = _castingLevel;
                var target = _castTarget;
                var point = _castTarget != null ? _castTarget.Position : _castPoint;
                EndCast();

                // The caster may have walked (Free Cast) or the target moved during the cast.
                if (skill.Special != SkillSpecial.Dash && DistanceTo(skill, target, point) > skill.Range.At(level) + CastRangeTolerance)
                {
                    ChatLog.Error($"{skill.Name} failed: the target is out of range.");
                    return;
                }

                Execute(skill, level, target, point);
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
