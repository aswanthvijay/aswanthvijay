using System;
using System.Collections.Generic;
using Runeheir.Characters;
using Runeheir.Field;
using Runeheir.Items;
using Runeheir.Monsters;
using Runeheir.Movement;
using Runeheir.Online;
using Runeheir.Session;
using Runeheir.Skills;
using Runeheir.Visuals;
using UnityEngine;
using UnityEngine.AI;

namespace Runeheir.Combat
{
    /// <summary>
    /// Phase 5 monster brain. Wanders near its spawn, aggroes (aggressive kinds) or retaliates, calls its pack for help,
    /// chases and auto-attacks, and uses its skills (<see cref="MonsterSkillRules"/> picks them): casts show a bar and, for
    /// areas and leaps, a circle on the ground; a stagger interrupts them. Bosses change phase as their HP falls, summon
    /// their minions, and pay out MVP rewards. Leashes home past its range, healing and resetting. Dies → EXP split by
    /// damage share and autoloot drops.
    /// </summary>
    [RequireComponent(typeof(NavMotor))]
    [RequireComponent(typeof(AutoAttacker))]
    public sealed class Monster : CombatEntity
    {
        private const float ThinkInterval = 0.25f;
        private const float WanderRadius = 6f;
        private const float DpsReportIdleSeconds = 2.5f;
        private const float AssistRange = 10f;

        /// <summary>A cast Strike still lands if the target is within the monster's reach plus this (else it was dodged).</summary>
        private const float StrikeReachSlack = 1.2f;

        private readonly Dictionary<PlayerEntity, long> _damageByPlayer = new Dictionary<PlayerEntity, long>();
        private readonly Dictionary<string, float> _skillReadyAt = new Dictionary<string, float>();
        private readonly List<Monster> _minions = new List<Monster>();
        private readonly List<CombatEntity> _hitBuffer = new List<CombatEntity>();

        private NavMotor _motor;
        private AutoAttacker _attacker;
        private CharacterAnimationBridge _animation;
        private Vector3 _home;
        private CombatEntity _aggroTarget;
        private bool _returningHome;
        private float _nextThinkAt;
        private float _nextWanderAt;
        private bool _stolenFrom;

        // Skills and casting.
        private Func<string, int> _summonsAlive;
        private Func<string, bool> _hasBuff;
        private Func<MonsterSkill, bool> _isReady;
        private float _globalSkillReadyAt;
        private MonsterSkill _cast;
        private CombatEntity _castTarget;
        private Vector3 _castPoint;
        private float _castStart;
        private float _castEnd;
        private SkillTelegraph _telegraph;
        private int _phase;

        // Training dummy DPS meter.
        private float _dpsSessionStart = -1f;
        private float _lastHitAt;
        private long _dpsTotal;
        private long _dpsFirstHit;
        private int _dpsHits;

        public MonsterDefinition Definition { get; private set; }

        public MonsterSpawner Spawner { get; set; }

        /// <summary>The boss that summoned this one (minions vanish when it dies and stay near it).</summary>
        public Monster Leader { get; private set; }

        /// <summary>Bosses: the top damage dealer of the kill (set when it dies).</summary>
        public string MvpName { get; private set; }

        /// <summary>Boss phase: 0 at full HP, +1 per threshold crossed.</summary>
        public int Phase => _phase;

        public bool IsCasting => _cast != null;

        public MonsterSkill CastingSkill => _cast;

        /// <summary>0..1 through the current cast.</summary>
        public float CastProgress => _cast == null ? 0f : Mathf.Clamp01((Time.time - _castStart) / Mathf.Max(0.01f, _castEnd - _castStart));

        public float HpPercent => MaxHp > 0 ? Hp * 100f / MaxHp : 0f;

        public override Faction Faction => Faction.Monster;

        public override string DisplayName => Definition != null ? Definition.Name : name;

        public override int Level => Definition?.Level ?? 1;

        public override float AttackRange => Definition?.AttackRange ?? 0.9f;

        /// <summary>Base interval, slowed or sped up by ASPD% buffs/statuses (Frostbite halves the rate).</summary>
        public override float AttackInterval => (Definition?.AttackInterval ?? 1.5f) / Mathf.Max(0.1f, 1f + ActiveModifiers.AspdPercent / 100f);

        public override bool IsRangedAttacker => Definition != null && Definition.IsRanged;

        /// <summary>Bosses and dummies hold their ground.</summary>
        public override bool CanBeKnockedBack => Definition != null && !Definition.Stationary && !Definition.IsBoss;

        public override float BasicPoiseDamage => Definition != null ? PoiseRules.MonsterPoiseDamage(Definition.Size, Definition.Level) : 10f;

        /// <summary>Level / 2 in every resisting stat; bosses, Undead and Water monsters are immune to some statuses.</summary>
        public override StatusResistances StatusResistances => Definition?.Resistances ?? default;

        /// <summary>The player this monster is fighting (or null).</summary>
        public CombatEntity AggroTarget => _aggroTarget;

        public IReadOnlyList<Monster> Minions => _minions;

        /// <summary>The realm server watches its monsters through this (casts, announcements) to show them on every client.</summary>
        public IMonsterObserver Observer { get; set; }

        public void Initialize(MonsterDefinition definition, Vector3 home)
        {
            Definition = definition;
            _home = home;
            bodyRadius = 0.4f * Mathf.Max(0.5f, definition.Scale);
            bodyHeight = 1.6f * definition.Scale;
            SetVitals(definition.MaxHp, definition.MaxHp, 0, 1);
            Poise.SetMax(definition.MaxPoise);
            Poise.Reset();
            _motor.BaseMoveSpeed = definition.MoveSpeed;
            OnModifiersChanged();
            _nextWanderAt = Time.time + UnityEngine.Random.Range(1f, 4f);
            _globalSkillReadyAt = Time.time + UnityEngine.Random.Range(0.5f, 2f);
        }

        /// <summary>Marks this monster as <paramref name="leader"/>'s summon.</summary>
        public void SetLeader(Monster leader)
        {
            Leader = leader;
            if (leader != null)
            {
                leader._minions.Add(this);
            }
        }

        public override AttackerProfile BuildAttackerProfile()
        {
            var mods = ActiveModifiers;
            int min = Definition.AtkMin;
            int max = Mathf.Max(min, Definition.AtkMax);
            int mid = (min + max) / 2;
            return new AttackerProfile
            {
                StatusAtk = mods.Atk,
                WeaponAtk = mid,
                WeaponVariance = mid > 0 ? (max - min) / (float)(max + min) : 0f,
                Weapon = WeaponType.Unarmed,
                AttackElement = Definition.AttackElement,
                Hit = Scaled(Definition.Hit + mods.Hit, mods.HitPercent),
                CritChance = 0f,
                MatkMin = min,
                MatkMax = max,
                PhysicalDamagePercent = mods.PhysicalDamagePercent,
                MagicDamagePercent = mods.MagicDamagePercent,
                Race = Definition.Race, // Horned Grazer / Draugr Footman cards cut damage by attacker race
            };
        }

        /// <summary>Monster DEF/FLEE with debuffs applied (Provoke and Poison lower DEF, Blind lowers FLEE).</summary>
        public override DefenderProfile BuildDefenderProfile()
        {
            var mods = ActiveModifiers;
            return new DefenderProfile
            {
                Def = Scaled(Definition.Def + mods.Def, mods.DefPercent),
                SoftDef = Scaled(Definition.Level / 4, mods.DefPercent),
                Mdef = Scaled(Definition.Mdef + mods.Mdef, mods.MdefPercent),
                SoftMdef = Scaled(Definition.Level / 4, mods.MdefPercent),
                Flee = Scaled(Definition.Flee + mods.Flee, mods.FleePercent),
                Element = Definition.Element,
                Race = Definition.Race,
                Size = Definition.Size,
                BluntDamageTakenMultiplier = BluntDamageTakenMultiplier,
            };
        }

        /// <summary>Provoke / War Cry / a packmate's call: drop whatever it was doing and come after <paramref name="provoker"/>.</summary>
        public void Provoke(CombatEntity provoker)
        {
            if (Remote != null)
            {
                if (provoker != null && !IsDead)
                {
                    Remote.RelayProvoke(provoker);
                }

                return;
            }

            if (Definition == null || Definition.Passive || IsDead || provoker == null || provoker.IsDead)
            {
                return;
            }

            _aggroTarget = provoker;
            _returningHome = false;
            _nextThinkAt = 0f;
        }

        /// <summary>Starts one of its skills now, skipping the chance roll and cooldown (tests and GM tools).</summary>
        public bool ForceSkill(string skillId, CombatEntity target)
        {
            var skill = Definition?.Skill(skillId);
            if (skill == null || IsDead || IsCasting)
            {
                return false;
            }

            BeginCast(skill, target);
            return true;
        }

        /// <summary>
        /// Pilfer: one success per monster. Returns the stolen item id, or null (already robbed, no drops, or the roll failed;
        /// <paramref name="reason"/> says which).
        /// </summary>
        public string TrySteal(int skillLevel, int dex, out string reason)
        {
            if (_stolenFrom)
            {
                reason = "There is nothing left to steal.";
                return null;
            }

            if (Definition == null || Definition.Drops.Count == 0)
            {
                reason = "It has nothing to steal.";
                return null;
            }

            if (!SystemRandomSource.Shared.Chance(StealRules.Chance(skillLevel, dex, Definition.Level)))
            {
                reason = "Steal failed.";
                return null;
            }

            string itemId = StealRules.PickItem(Definition.Drops, SystemRandomSource.Shared);
            reason = itemId == null ? "It has nothing you can steal." : null;
            return itemId;
        }

        /// <summary>Called once the stolen item is actually in the thief's inventory: no second steal from this monster.</summary>
        /// <summary>An Outlaw already cut this monster's purse (Cut Purse works once per monster).</summary>
        public bool CoinsTaken { get; set; }

        public void MarkStolenFrom()
        {
            _stolenFrom = true;
        }

        protected override void OnModifiersChanged()
        {
            if (_motor != null)
            {
                _motor.SpeedMultiplier = Mathf.Max(0.1f, 1f + ActiveModifiers.MoveSpeedPercent / 100f);
            }
        }

        private static int Scaled(int value, float percent)
        {
            return Mathf.Max(0, Mathf.RoundToInt(value * Mathf.Max(0f, 1f + percent / 100f)));
        }

        private void Awake()
        {
            _motor = GetComponent<NavMotor>();
            _attacker = GetComponent<AutoAttacker>();
            _animation = GetComponent<CharacterAnimationBridge>();
            _summonsAlive = SummonsAlive;
            _hasBuff = id => Buffs.Has(id);
            _isReady = skill => !_skillReadyAt.TryGetValue(skill.Id, out float readyAt) || Time.time >= readyAt;
        }

        protected override void Update()
        {
            base.Update();
            if (Definition == null || IsDead)
            {
                return;
            }

            if (IsMirror)
            {
                UpdateMirrorCast();
                return;
            }

            UpdateDpsMeter();

            // A summon whose master is gone fades away with it.
            if (!ReferenceEquals(Leader, null) && (Leader == null || Leader.IsDead))
            {
                Vanish();
                return;
            }

            // Snared, incapacitated or casting: no walking (a snared monster still attacks whatever is in reach).
            _motor.SetLocked(!CanMove || IsCasting);
            if (IsIncapacitated)
            {
                return;
            }

            if (IsCasting)
            {
                UpdateCast();
                return;
            }

            if (Time.time < _nextThinkAt)
            {
                return;
            }

            _nextThinkAt = Time.time + ThinkInterval;
            Think();
        }

        private void Think()
        {
            if (!ReferenceEquals(Leader, null) && Leader != null)
            {
                _home = Leader.Position;
            }

            if (_aggroTarget != null && ShouldDropTarget(_aggroTarget))
            {
                _aggroTarget = null;
                _attacker.Disengage();
                _returningHome = !Definition.Stationary;
                if (_returningHome)
                {
                    _motor.MoveTo(_home);
                }
            }

            if (_returningHome)
            {
                if (HorizontalDistance(Position, _home) < 1.5f)
                {
                    _returningHome = false;
                    ResetFight();
                }
                else if (!_motor.IsMoving)
                {
                    // A snare or stun on the way home stopped the agent (and cleared its path): set off again.
                    _motor.MoveTo(_home);
                }

                return;
            }

            if (_aggroTarget == null && Definition.Aggressive && !Definition.Passive)
            {
                _aggroTarget = FindNearestPlayer(Definition.AggroRange);
            }

            if (!Definition.Passive && TryUseSkill(_aggroTarget))
            {
                return;
            }

            if (_aggroTarget != null && !Definition.Passive)
            {
                _attacker.Engage(_aggroTarget);
                return;
            }

            if (!Definition.Stationary && Time.time >= _nextWanderAt)
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle * WanderRadius;
                _motor.MoveTo(_home + new Vector3(offset.x, 0f, offset.y));
                _nextWanderAt = Time.time + UnityEngine.Random.Range(3f, 8f);
            }
        }

        /// <summary>Back home after a leash: full HP, phases and their buffs cleared (the fight starts over).</summary>
        private void ResetFight()
        {
            Heal(MaxHp, showNumber: false);
            if (_phase > 0)
            {
                _phase = 0;
                Buffs.RemoveWhere(b => b.Definition.Id == MonsterBuffs.Enraged || b.Definition.Id == MonsterBuffs.Unbound);
            }

            _damageByPlayer.Clear();
        }

        private bool ShouldDropTarget(CombatEntity target)
        {
            if (target == null || target.IsDead || !target.isActiveAndEnabled || !CanSee(target))
            {
                return true;
            }

            if (Definition.Stationary)
            {
                return false;
            }

            return HorizontalDistance(Position, _home) > Definition.LeashRange
                   || HorizontalDistance(Position, target.Position) > Mathf.Max(Definition.AggroRange * 2.5f, 15f);
        }

        private CombatEntity FindNearestPlayer(float range)
        {
            CombatEntity best = null;
            float bestDistance = range;
            foreach (var entity in All)
            {
                if (entity is PlayerEntity player && !player.IsDead && CanSee(player))
                {
                    float distance = HorizontalDistance(Position, player.Position);
                    if (distance <= bestDistance)
                    {
                        best = player;
                        bestDistance = distance;
                    }
                }
            }

            return best;
        }

        // ================================================================ skills
        private bool TryUseSkill(CombatEntity target)
        {
            if (Definition.Skills.Count == 0 || !CanUseSkills || Time.time < _globalSkillReadyAt)
            {
                return false;
            }

            var context = new MonsterSkillContext
            {
                HpPercent = HpPercent,
                Phase = _phase,
                HasTarget = target != null,
                TargetDistance = target != null ? EdgeDistanceTo(target) : float.MaxValue,
                AttackRange = AttackRange,
                SummonsAlive = _summonsAlive,
                HasBuff = _hasBuff,
            };
            var skill = MonsterSkillRules.Choose(Definition.Skills, context, _isReady, SystemRandomSource.Shared);
            if (skill == null)
            {
                return false;
            }

            BeginCast(skill, target);
            return true;
        }

        private int SummonsAlive(string monsterId)
        {
            _minions.RemoveAll(m => m == null || m.IsDead);
            int count = 0;
            foreach (var minion in _minions)
            {
                if (minion.Definition.Id == monsterId)
                {
                    count++;
                }
            }

            return count;
        }

        private void BeginCast(MonsterSkill skill, CombatEntity target)
        {
            _skillReadyAt[skill.Id] = Time.time + skill.Cooldown;
            _globalSkillReadyAt = Time.time + skill.CastTime + MonsterSkillRules.GlobalCooldown;
            _attacker.Disengage();
            _motor.Stop();
            if (target != null)
            {
                _motor.FaceTowards(target.Position, instant: true);
            }

            _cast = skill;
            _castTarget = target;
            _castStart = Time.time;
            _castEnd = Time.time + Mathf.Max(0f, skill.CastTime);
            _castPoint = skill.CenteredOnSelf || target == null ? Position : target.Position;

            Color color = ElementColors.Of(skill.Element);
            if (skill.HasTelegraph)
            {
                _telegraph = SkillTelegraph.Show(_castPoint, skill.Radius, Mathf.Max(0.2f, skill.CastTime), color);
            }

            WorldFeedback.Announce(this, skill.Name + "!", Color.Lerp(color, Color.white, 0.35f));
            if (!string.IsNullOrEmpty(skill.Shout))
            {
                Announce($"{Definition.Name}: {skill.Shout}");
            }

            Observer?.CastBegan(skill, target, _castPoint);

            if (skill.CastTime > 0f && _animation != null)
            {
                _animation.SetCasting(true);
            }

            if (skill.CastTime <= 0f)
            {
                ResolveCast();
            }
        }

        private void UpdateCast()
        {
            if (_castTarget != null && !_castTarget.IsDead && !_cast.CenteredOnSelf && _cast.Kind != MonsterSkillKind.Area && _cast.Kind != MonsterSkillKind.Leap)
            {
                _motor.FaceTowards(_castTarget.Position);
            }

            if (Time.time >= _castEnd)
            {
                ResolveCast();
            }
        }

        private void CancelCast(string reason)
        {
            if (_cast == null)
            {
                return;
            }

            var skill = _cast;
            _cast = null;
            _castTarget = null;
            CloseTelegraph();
            if (_animation != null)
            {
                _animation.SetCasting(false);
            }

            if (!string.IsNullOrEmpty(reason))
            {
                WorldFeedback.Announce(this, reason, new Color(1f, 0.85f, 0.4f));
            }

            Observer?.CastEnded(skill, null, Position, resolved: false);
        }

        private void CloseTelegraph()
        {
            if (_telegraph != null)
            {
                _telegraph.Close();
                _telegraph = null;
            }
        }

        private void ResolveCast()
        {
            var skill = _cast;
            var target = _castTarget;
            _cast = null;
            _castTarget = null;
            CloseTelegraph();
            if (_animation != null)
            {
                _animation.SetCasting(false);
            }

            if (skill == null || IsDead)
            {
                return;
            }

            Observer?.CastEnded(skill, target, _castPoint, resolved: true);
            bool targetValid = target != null && !target.IsDead && target.isActiveAndEnabled && CanSee(target);
            switch (skill.Kind)
            {
                case MonsterSkillKind.Strike:
                    PlayMotion(SkillMotion.Swing);
                    if (!targetValid)
                    {
                        break;
                    }

                    if (EdgeDistanceTo(target) > AttackRange + StrikeReachSlack)
                    {
                        WorldFeedback.Announce(target, "Dodged!", new Color(0.7f, 0.95f, 1f));
                        break;
                    }

                    HitTarget(skill, target, melee: !skill.Magical);
                    break;

                case MonsterSkillKind.Bolt:
                    PlayMotion(skill.Magical ? SkillMotion.Cast : SkillMotion.Shoot);
                    if (targetValid)
                    {
                        ProjectileFx.Launch(this, target, ElementColors.Of(skill.Element), ProjectileFx.BoltSpeed, () =>
                        {
                            if (this != null && !IsDead && target != null && !target.IsDead)
                            {
                                HitTarget(skill, target, melee: false);
                            }
                        }, arrow: !skill.Magical, width: skill.Magical ? 0.14f : 0.07f);
                    }

                    break;

                case MonsterSkillKind.Area:
                    PlayMotion(skill.CenteredOnSelf ? SkillMotion.Spin : SkillMotion.Cast);
                    Blast(skill, skill.CenteredOnSelf ? Position : _castPoint);
                    break;

                case MonsterSkillKind.Leap:
                    LeapTo(skill, _castPoint);
                    break;

                case MonsterSkillKind.Buff:
                    PlayMotion(SkillMotion.Buff);
                    ApplyBuffToAllies(skill);
                    break;

                case MonsterSkillKind.Heal:
                    PlayMotion(SkillMotion.Buff);
                    HealAllies(skill);
                    break;

                case MonsterSkillKind.Summon:
                    PlayMotion(SkillMotion.Buff);
                    Summon(skill.SummonId, skill.SummonCount, targetValid ? target : null);
                    break;

                case MonsterSkillKind.Teleport:
                    TeleportNear(skill, targetValid ? target : null);
                    break;
            }
        }

        private void PlayMotion(SkillMotion motion)
        {
            if (_animation != null)
            {
                _animation.PlaySkill(motion, 1f, 0.45f);
            }
        }

        /// <summary>One skill hit: damage (skills never miss; physical ones can still be blocked), then status, knockback, weapon break and leech.</summary>
        private void HitTarget(MonsterSkill skill, CombatEntity target, bool melee)
        {
            var profile = BuildAttackerProfile();
            profile.AttackElement = skill.Element;
            profile.NeverMiss = true;
            var defender = target.BuildDefenderProfile();
            var result = skill.Magical
                ? DamageCalculator.Magical(profile, defender, skill.Percent, skill.Element, SystemRandomSource.Shared)
                : DamageCalculator.Physical(profile, defender, skill.Percent, false, SystemRandomSource.Shared);
            result.IsMagical = skill.Magical;
            var applied = target.ReceiveDamage(result, this, physicalMelee: melee, BasicPoiseDamage * 1.5f + skill.PoiseDamage);
            if (applied.IsMiss || applied.IsBlocked || target.IsDead)
            {
                return;
            }

            if (skill.Status != StatusEffect.None)
            {
                target.TryApplyStatus(skill.Status, skill.StatusChance, skill.StatusSeconds);
            }

            if (skill.Knockback > 0f)
            {
                Vector3 away = target.Position - Position;
                away.y = 0f;
                target.Knockback(away.sqrMagnitude > 0.01f ? away.normalized : transform.forward, skill.Knockback);
            }

            if (skill.BreakWeaponPercent > 0f && target is PlayerEntity player)
            {
                player.TryBreakWeapon(skill.BreakWeaponPercent);
            }

            if (skill.LeechPercent > 0f && applied.Amount > 0)
            {
                Heal(Mathf.Max(1, Mathf.RoundToInt(applied.Amount * skill.LeechPercent / 100f)));
            }
        }

        private void Blast(MonsterSkill skill, Vector3 center)
        {
            GroundRing.SpawnPulse(center, ElementColors.Of(skill.Element), 0.3f, skill.Radius, 0.45f, 0.14f);
            _hitBuffer.Clear();
            foreach (var entity in All)
            {
                if (IsHostileTo(entity) && !entity.IsDead && HorizontalDistance(entity.Position, center) <= skill.Radius + entity.Radius)
                {
                    _hitBuffer.Add(entity);
                }
            }

            foreach (var entity in _hitBuffer)
            {
                if (IsDead)
                {
                    break;
                }

                HitTarget(skill, entity, melee: false);
            }
        }

        private void LeapTo(MonsterSkill skill, Vector3 landing)
        {
            Vector3 from = Position;
            Vector3 back = from - landing;
            back.y = 0f;
            Vector3 spot = landing + (back.sqrMagnitude > 0.01f ? back.normalized : Vector3.back) * (Radius + 0.5f);
            if (NavMesh.SamplePosition(spot, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                _motor.Warp(hit.position);
            }

            PlayMotion(SkillMotion.Leap);
            Blast(skill, landing);
        }

        private void TeleportNear(MonsterSkill skill, CombatEntity target)
        {
            Vector3 destination;
            if (skill.BehindTarget && target != null)
            {
                Vector3 through = target.Position - Position;
                through.y = 0f;
                destination = target.Position + (through.sqrMagnitude > 0.01f ? through.normalized : Vector3.forward) * (target.Radius + Radius + 0.6f);
            }
            else
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(6f, 12f);
                destination = Position + new Vector3(offset.x, 0f, offset.y);
            }

            if (!NavMesh.SamplePosition(destination, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                return;
            }

            Color color = new Color(0.65f, 0.5f, 1f, 1f);
            GroundRing.SpawnPulse(Position, color, 0.2f, 1.6f, 0.4f);
            _motor.Warp(hit.position);
            GroundRing.SpawnPulse(hit.position, color, 1.6f, 0.2f, 0.4f);
            if (target != null)
            {
                _motor.FaceTowards(target.Position, instant: true);
            }
        }

        private void ApplyBuffToAllies(MonsterSkill skill)
        {
            var buff = BuffCatalog.Get(skill.BuffId);
            if (buff == null)
            {
                return;
            }

            foreach (var ally in Allies(skill.Radius))
            {
                ally.Buffs.Apply(buff, Time.timeAsDouble);
                GroundRing.SpawnPulse(ally.Position, RuntimeMaterials.Hex(buff.IconColorHex), 0.3f, 1.4f, 0.5f);
            }
        }

        private void HealAllies(MonsterSkill skill)
        {
            foreach (var ally in Allies(skill.Radius))
            {
                ally.Heal(Mathf.Max(1, Mathf.RoundToInt(ally.MaxHp * skill.HealPercent / 100f)));
            }
        }

        /// <summary>This monster, plus every living monster within <paramref name="radius"/> when it's above zero.</summary>
        private List<Monster> Allies(float radius)
        {
            var allies = new List<Monster> { this };
            if (radius <= 0f)
            {
                return allies;
            }

            foreach (var entity in All)
            {
                if (entity is Monster other && other != this && !other.IsDead && other.Definition != null && !other.Definition.Immortal
                    && HorizontalDistance(other.Position, Position) <= radius)
                {
                    allies.Add(other);
                }
            }

            return allies;
        }

        private void Summon(string monsterId, int count, CombatEntity target)
        {
            var definition = MonsterCatalog.Get(monsterId);
            if (definition == null)
            {
                return;
            }

            int missing = count - SummonsAlive(monsterId);
            for (int i = 0; i < missing; i++)
            {
                float angle = (i + 0.5f) * Mathf.PI * 2f / Mathf.Max(1, missing);
                Vector3 point = Position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (Radius + 2.5f);
                if (!NavMesh.SamplePosition(point, out NavMeshHit hit, 4f, NavMesh.AllAreas))
                {
                    continue;
                }

                var minion = EntityFactory.CreateMonster(definition, hit.position, UnityEngine.Random.Range(0f, 360f));
                minion.SetLeader(this);
                GroundRing.SpawnPulse(hit.position, new Color(0.85f, 0.3f, 0.3f, 1f), 0.2f, 1.8f, 0.5f);
                if (target != null)
                {
                    minion.Provoke(target);
                }
            }
        }

        /// <summary>Gone without a trace (a summon whose master fell): no rewards.</summary>
        private void Vanish()
        {
            if (IsMirror)
            {
                return; // the realm removes it everywhere
            }

            CancelCast(null);
            GroundRing.SpawnPulse(Position, new Color(0.5f, 0.5f, 0.6f, 1f), 1.2f, 0.1f, 0.4f);
            Destroy(gameObject);
        }

        // ================================================================ mirrors (online play: the realm runs the real one)
        /// <summary>Turns this into a mirror of a monster the realm runs: no brain, moved by the network.</summary>
        public void MakeMirror(IRemoteEntity remote)
        {
            Remote = remote;
            _attacker.Disengage();
            _motor.MakePuppet();
        }

        /// <summary>The realm's monster started a skill: the bar, the ground circle and the name, here too.</summary>
        public void MirrorCast(MonsterSkill skill, CombatEntity target, Vector3 point)
        {
            if (skill == null || IsDead)
            {
                return;
            }

            CloseTelegraph();
            _cast = skill;
            _castTarget = target;
            _castStart = Time.time;
            _castEnd = Time.time + Mathf.Max(0f, skill.CastTime);
            _castPoint = point;
            Color color = ElementColors.Of(skill.Element);
            if (skill.HasTelegraph && skill.CastTime > 0f)
            {
                _telegraph = SkillTelegraph.Show(point, skill.Radius, Mathf.Max(0.2f, skill.CastTime), color);
            }

            if (target != null)
            {
                _motor.FaceTowards(target.Position, instant: true);
            }

            WorldFeedback.Announce(this, skill.Name + "!", Color.Lerp(color, Color.white, 0.35f));
            if (skill.CastTime > 0f && _animation != null)
            {
                _animation.SetCasting(true);
            }
        }

        /// <summary>The realm's cast finished (or was interrupted): the effect's look, without its gameplay.</summary>
        public void MirrorCastEnd(MonsterSkill skill, CombatEntity target, Vector3 point, bool resolved)
        {
            _cast = null;
            _castTarget = null;
            CloseTelegraph();
            if (_animation != null)
            {
                _animation.SetCasting(false);
            }

            if (!resolved || skill == null || IsDead)
            {
                return;
            }

            Color color = ElementColors.Of(skill.Element);
            switch (skill.Kind)
            {
                case MonsterSkillKind.Strike:
                    PlayMotion(SkillMotion.Swing);
                    break;
                case MonsterSkillKind.Bolt:
                    PlayMotion(skill.Magical ? SkillMotion.Cast : SkillMotion.Shoot);
                    if (target != null && !target.IsDead)
                    {
                        ProjectileFx.Launch(this, target, color, ProjectileFx.BoltSpeed, null, arrow: !skill.Magical, width: skill.Magical ? 0.14f : 0.07f);
                    }

                    break;
                case MonsterSkillKind.Area:
                    PlayMotion(skill.CenteredOnSelf ? SkillMotion.Spin : SkillMotion.Cast);
                    GroundRing.SpawnPulse(skill.CenteredOnSelf ? Position : point, color, 0.3f, skill.Radius, 0.45f, 0.14f);
                    break;
                case MonsterSkillKind.Leap:
                    PlayMotion(SkillMotion.Leap);
                    GroundRing.SpawnPulse(point, color, 0.3f, skill.Radius, 0.45f, 0.14f);
                    break;
                case MonsterSkillKind.Teleport:
                    GroundRing.SpawnPulse(Position, new Color(0.65f, 0.5f, 1f, 1f), 0.2f, 1.6f, 0.4f);
                    break;
                default:
                    PlayMotion(SkillMotion.Buff);
                    break;
            }
        }

        /// <summary>The realm's monster swung its basic attack.</summary>
        public void MirrorSwing(CombatEntity target)
        {
            if (target != null)
            {
                _motor.FaceTowards(target.Position, instant: true);
            }

            if (_animation != null)
            {
                _animation.PlayAttack(AttackPlayRate, SwingDuration);
            }

            if (IsRangedAttacker && target != null)
            {
                ProjectileFx.Launch(this, target, ProjectileFx.ArrowColor, ProjectileFx.ArrowSpeed, null, arrow: true);
            }
        }

        /// <summary>The boss crossed into another phase on the realm (its HP plate shows it).</summary>
        public void MirrorPhase(int phase)
        {
            _phase = Mathf.Max(0, phase);
        }

        private void UpdateMirrorCast()
        {
            if (_cast == null)
            {
                return;
            }

            if (_castTarget != null && !_castTarget.IsDead && !_cast.CenteredOnSelf && _cast.Kind != MonsterSkillKind.Area && _cast.Kind != MonsterSkillKind.Leap)
            {
                _motor.FaceTowards(_castTarget.Position);
            }

            // The end arrives from the realm; if it got lost, don't leave the bar up forever.
            if (Time.time > _castEnd + 1.5f)
            {
                MirrorCastEnd(_cast, null, _castPoint, resolved: false);
            }
        }

        protected override void OnMirroredHit(DamageResult result, CombatEntity attacker)
        {
            if (_animation != null && result.Amount > 0 && !result.IsDamageOverTime)
            {
                _animation.PlayHit();
            }
        }

        // ================================================================ phases
        private void CheckPhase()
        {
            int phase = MonsterSkillRules.PhaseFor(Definition, HpPercent);
            while (_phase < phase && _phase < Definition.Phases.Count)
            {
                var next = Definition.Phases[_phase];
                _phase++;
                EnterPhase(next);
            }
        }

        private void EnterPhase(BossPhase phase)
        {
            var buff = BuffCatalog.Get(phase.BuffId);
            if (buff != null)
            {
                Buffs.Apply(buff, Time.timeAsDouble, duration: MonsterBuffs.PhaseDuration);
            }

            if (!string.IsNullOrEmpty(phase.Shout))
            {
                Announce($"⚔ {phase.Shout}");
                WorldFeedback.Announce(this, phase.Shout, new Color(1f, 0.45f, 0.35f));
            }

            Observer?.PhaseChanged(_phase);

            GroundRing.SpawnPulse(Position, new Color(1f, 0.35f, 0.25f, 1f), Radius, Radius + 7f, 0.9f, 0.18f);
            if (!string.IsNullOrEmpty(phase.SummonId) && phase.SummonCount > 0)
            {
                Summon(phase.SummonId, phase.SummonCount, _aggroTarget);
            }
        }

        // ================================================================ damage, statuses, death
        protected override void OnDamaged(DamageResult result, CombatEntity attacker)
        {
            if (_animation != null && result.Amount > 0 && !result.IsDamageOverTime)
            {
                _animation.PlayHit();
            }

            if (attacker is PlayerEntity player && result.Amount > 0)
            {
                _damageByPlayer.TryGetValue(player, out long dealt);
                _damageByPlayer[player] = dealt + result.Amount;
            }

            if (Definition.Immortal)
            {
                TrackDps(result);
                return;
            }

            // Passive monsters still fight back when hit (Ragnarok behaviour); dummies never do.
            // A hidden attacker (Underfang from the shadows) can't be retaliated against.
            if (attacker != null && !Definition.Passive && !attacker.IsDead && CanSee(attacker))
            {
                if (_aggroTarget == null)
                {
                    _aggroTarget = attacker;
                    _returningHome = false;
                    _nextThinkAt = 0f;
                }

                if (result.Amount > 0 && attacker.Faction == Faction.Player)
                {
                    CallForHelp(attacker);
                }
            }

            // OnDamaged runs before the death check: a killing blow must not start a phase on the way down.
            if (Definition.Phases.Count > 0 && result.Amount > 0 && Hp > 0 && !IsDead)
            {
                CheckPhase();
            }
        }

        /// <summary>Packmates of the same kind (assist monsters) and this monster's own summons join the fight.</summary>
        private void CallForHelp(CombatEntity attacker)
        {
            foreach (var entity in All)
            {
                if (entity is Monster other && other != this && !other.IsDead && other.AggroTarget == null && other.Definition != null
                    && (other.Leader == this || (Definition.Assist && other.Definition.Id == Definition.Id))
                    && HorizontalDistance(other.Position, Position) <= AssistRange)
                {
                    other.Provoke(attacker);
                }
            }
        }

        protected override void OnStatusApplied(StatusEffect effect)
        {
            var info = StatusRules.Get(effect);
            if (IsCasting && (effect == StatusEffect.Stagger || (info != null && (info.Has(StatusFlags.Incapacitates) || info.Has(StatusFlags.BlocksSkills)))))
            {
                CancelCast("Interrupted!");
            }

            if (effect != StatusEffect.Stagger)
            {
                return;
            }

            WorldFeedback.Announce(this, "Staggered!", new Color(1f, 0.6f, 0.4f));
            if (_animation != null)
            {
                _animation.PlayStagger();
            }
        }

        protected override void OnHpDepleted(CombatEntity killer)
        {
            if (Definition.Immortal)
            {
                Heal(MaxHp, showNumber: false);
                return;
            }

            Die(killer);
        }

        protected override void OnDied(CombatEntity killer)
        {
            CancelCast(null);
            _attacker.Disengage();
            _motor.Stop();
            if (IsMirror)
            {
                // A mirror only looks dead: the realm hands out the rewards and removes the body.
                if (_animation != null)
                {
                    _animation.SetDead(true);
                }

                foreach (var collider in GetComponentsInChildren<Collider>())
                {
                    collider.enabled = false;
                }

                return;
            }

            // Leave the crowd simulation so the corpse no longer pushes or blocks other agents.
            if (_motor.Agent != null)
            {
                _motor.Agent.enabled = false;
            }

            if (_animation != null)
            {
                _animation.SetDead(true);
            }

            foreach (var collider in GetComponentsInChildren<Collider>())
            {
                collider.enabled = false;
            }

            AwardRewards(killer as PlayerEntity);
            if (Spawner != null)
            {
                Spawner.NotifyDeath(this);
            }

            Destroy(gameObject, 2.5f);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            CloseTelegraph();
        }

        private void AwardRewards(PlayerEntity killer)
        {
            long total = 0;
            foreach (var pair in _damageByPlayer)
            {
                total += pair.Value;
            }

            var rates = ServerRates.Current;
            foreach (var pair in _damageByPlayer)
            {
                if (pair.Key == null || pair.Key.IsDead)
                {
                    continue;
                }

                double share = total > 0 ? pair.Value / (double)total : 1.0;
                long baseExp = (long)Math.Round(Definition.BaseExp * rates.BaseExp * share);
                long jobExp = (long)Math.Round(Definition.JobExp * rates.JobExp * share);

                // Online, an Even Share party splits it between the members on this map (GDD §8).
                if (RealmHooks.ShareExperience == null || !RealmHooks.ShareExperience(pair.Key, baseExp, jobExp))
                {
                    pair.Key.GrantExperience(baseExp, jobExp);
                }
            }

            if (Definition.IsBoss)
            {
                var mvp = MvpRules.PickMvp(_damageByPlayer);
                MvpName = mvp != null ? mvp.DisplayName : killer?.DisplayName;
                if (Definition.IsMvp && mvp != null && !mvp.IsDead)
                {
                    AwardMvp(mvp, rates);
                }
            }

            if (killer == null || killer.IsDead)
            {
                return;
            }

            foreach (var drop in Definition.Drops)
            {
                var item = ItemCatalog.Get(drop.ItemId);
                if (item == null)
                {
                    continue;
                }

                // Server card rate applies to Soul Cards, the item rate to everything else.
                float rate = item.IsCard ? rates.CardDrop : rates.Drop;
                float chance = Mathf.Min(100f, drop.ChancePercent * rate);
                if (chance < 100f && UnityEngine.Random.value * 100f >= chance) // Random.value includes 1.0
                {
                    continue;
                }

                if (item.IsCard)
                {
                    Announce($"★ {killer.DisplayName} got a {item.Name}! ({drop.ChancePercent:0.##}% drop)");
                }

                killer.ReceiveLoot(item, drop.ChancePercent, mvpReward: false);
            }
        }

        private void AwardMvp(PlayerEntity mvp, ServerRates rates)
        {
            Announce($"★★ MVP! {mvp.DisplayName} is the Most Valuable Player of the fight against {Definition.Name}!");
            long bonus = (long)Math.Round(Definition.MvpExp * rates.BaseExp);
            mvp.ReceiveMvp(Definition.Name, bonus);

            string reward = MvpRules.RollMvpDrop(Definition, rates.Drop, SystemRandomSource.Shared);
            var item = ItemCatalog.Get(reward);
            if (item != null)
            {
                Announce($"★ MVP reward: {mvp.DisplayName} receives {item.Name}!");
                mvp.ReceiveLoot(item, 0f, mvpReward: true);
            }
        }

        /// <summary>A line for everyone nearby: this screen's chat, and (on a realm server) every player on the map.</summary>
        private void Announce(string line)
        {
            ChatLog.Notice(line);
            RealmHooks.MapNotice?.Invoke(this, line);
        }

        private void TrackDps(DamageResult result)
        {
            if (_dpsSessionStart < 0f)
            {
                _dpsSessionStart = Time.time;
                _dpsTotal = 0;
                _dpsHits = 0;
                _dpsFirstHit = result.Amount;
            }

            _lastHitAt = Time.time;
            _dpsTotal += result.Amount;
            _dpsHits++;
        }

        private void UpdateDpsMeter()
        {
            if (_dpsSessionStart < 0f || Time.time - _lastHitAt < DpsReportIdleSeconds)
            {
                return;
            }

            // N hits span N-1 intervals: measure from the first hit and leave its damage out of the rate.
            float seconds = _lastHitAt - _dpsSessionStart;
            if (_dpsHits < 2 || seconds < 0.05f)
            {
                ChatLog.Notice($"[{DisplayName}] {_dpsTotal:N0} damage ({_dpsHits} hits). Keep attacking for a DPS reading.");
            }
            else
            {
                ChatLog.Notice($"[{DisplayName}] {_dpsTotal:N0} damage in {seconds:0.0}s → {(_dpsTotal - _dpsFirstHit) / seconds:N0} DPS, " +
                               $"{(_dpsHits - 1) / seconds:0.00} hits/s ({_dpsHits} hits)");
            }

            _dpsSessionStart = -1f;
        }
    }
}
