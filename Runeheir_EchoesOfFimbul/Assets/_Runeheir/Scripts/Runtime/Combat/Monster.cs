using System.Collections.Generic;
using Runeheir.Characters;
using Runeheir.Items;
using Runeheir.Monsters;
using Runeheir.Movement;
using Runeheir.Player;
using Runeheir.Session;
using Runeheir.Visuals;
using UnityEngine;

namespace Runeheir.Combat
{
    /// <summary>
    /// Phase 2 monster brain: wander near the spawn, aggro (aggressive types) or retaliate (passive types),
    /// chase and auto-attack, leash home, die → EXP split by damage share + autoloot drops.
    /// Full AI pipeline (skills, MVP phases) is Phase 5.
    /// </summary>
    [RequireComponent(typeof(NavMotor))]
    [RequireComponent(typeof(AutoAttacker))]
    public sealed class Monster : CombatEntity
    {
        private const float ThinkInterval = 0.25f;
        private const float WanderRadius = 6f;
        private const float DpsReportIdleSeconds = 2.5f;

        private readonly Dictionary<PlayerCharacter, long> _damageByPlayer = new Dictionary<PlayerCharacter, long>();

        private NavMotor _motor;
        private AutoAttacker _attacker;
        private CharacterAnimationBridge _animation;
        private Vector3 _home;
        private CombatEntity _aggroTarget;
        private bool _returningHome;
        private float _nextThinkAt;
        private float _nextWanderAt;

        // Training dummy DPS meter.
        private float _dpsSessionStart = -1f;
        private float _lastHitAt;
        private long _dpsTotal;
        private long _dpsFirstHit;
        private int _dpsHits;

        public MonsterDefinition Definition { get; private set; }

        public MonsterSpawner Spawner { get; set; }

        public override Faction Faction => Faction.Monster;

        public override string DisplayName => Definition != null ? Definition.Name : name;

        public override int Level => Definition?.Level ?? 1;

        public override float AttackRange => Definition?.AttackRange ?? 0.9f;

        public override float AttackInterval => Definition?.AttackInterval ?? 1.5f;

        public override bool CanBeKnockedBack => Definition != null && !Definition.Stationary;

        public void Initialize(MonsterDefinition definition, Vector3 home)
        {
            Definition = definition;
            _home = home;
            bodyRadius = 0.4f * Mathf.Max(0.5f, definition.Scale);
            bodyHeight = 1.6f * definition.Scale;
            SetVitals(definition.MaxHp, definition.MaxHp, 0, 1);
            _motor.BaseMoveSpeed = definition.MoveSpeed;
            _nextWanderAt = Time.time + Random.Range(1f, 4f);
        }

        public override AttackerProfile BuildAttackerProfile()
        {
            int min = Definition.AtkMin;
            int max = Mathf.Max(min, Definition.AtkMax);
            int mid = (min + max) / 2;
            return new AttackerProfile
            {
                StatusAtk = 0,
                WeaponAtk = mid,
                WeaponVariance = mid > 0 ? (max - min) / (float)(max + min) : 0f,
                Weapon = WeaponType.Unarmed,
                AttackElement = Element.Neutral,
                Hit = Definition.Hit,
                CritChance = 0f,
                MatkMin = min,
                MatkMax = max,
            };
        }

        public override DefenderProfile BuildDefenderProfile()
        {
            return new DefenderProfile
            {
                Def = Definition.Def,
                SoftDef = Definition.Level / 4,
                Mdef = Definition.Mdef,
                SoftMdef = Definition.Level / 4,
                Flee = Definition.Flee,
                Element = Definition.Element,
                Race = Definition.Race,
                Size = Definition.Size,
                BluntDamageTakenMultiplier = BluntDamageTakenMultiplier,
            };
        }

        private void Awake()
        {
            _motor = GetComponent<NavMotor>();
            _attacker = GetComponent<AutoAttacker>();
            _animation = GetComponent<CharacterAnimationBridge>();
        }

        protected override void Update()
        {
            base.Update();
            if (Definition == null || IsDead)
            {
                return;
            }

            UpdateDpsMeter();

            if (IsIncapacitated)
            {
                _motor.Stop();
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
                    Heal(MaxHp, showNumber: false);
                }

                return;
            }

            if (_aggroTarget == null && Definition.Aggressive && !Definition.Passive)
            {
                _aggroTarget = FindNearestPlayer(Definition.AggroRange);
            }

            if (_aggroTarget != null && !Definition.Passive)
            {
                _attacker.Engage(_aggroTarget);
                return;
            }

            if (!Definition.Stationary && Time.time >= _nextWanderAt)
            {
                Vector2 offset = Random.insideUnitCircle * WanderRadius;
                _motor.MoveTo(_home + new Vector3(offset.x, 0f, offset.y));
                _nextWanderAt = Time.time + Random.Range(3f, 8f);
            }
        }

        private bool ShouldDropTarget(CombatEntity target)
        {
            if (target == null || target.IsDead || !target.isActiveAndEnabled)
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
                if (entity is PlayerCharacter player && !player.IsDead)
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

        protected override void OnDamaged(DamageResult result, CombatEntity attacker)
        {
            if (_animation != null && result.Amount > 0)
            {
                _animation.PlayHit();
            }

            if (attacker is PlayerCharacter player && result.Amount > 0)
            {
                _damageByPlayer.TryGetValue(player, out long dealt);
                _damageByPlayer[player] = dealt + result.Amount;
            }

            if (Definition.Immortal)
            {
                TrackDps(result);
            }

            // Passive monsters still fight back when hit (Ragnarok behaviour); dummies never do.
            if (attacker != null && !Definition.Passive && _aggroTarget == null && !attacker.IsDead)
            {
                _aggroTarget = attacker;
                _returningHome = false;
                _nextThinkAt = 0f;
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
            _attacker.Disengage();
            _motor.Stop();

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

            AwardRewards(killer as PlayerCharacter);
            if (Spawner != null)
            {
                Spawner.NotifyDeath(this);
            }

            Destroy(gameObject, 2.5f);
        }

        private void AwardRewards(PlayerCharacter killer)
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
                long baseExp = (long)System.Math.Round(Definition.BaseExp * rates.BaseExp * share);
                long jobExp = (long)System.Math.Round(Definition.JobExp * rates.JobExp * share);
                pair.Key.GrantExperience(baseExp, jobExp);
            }

            if (killer == null || killer.IsDead)
            {
                return;
            }

            foreach (var drop in Definition.Drops)
            {
                float chance = Mathf.Min(100f, drop.ChancePercent * rates.Drop);
                if (chance >= 100f || Random.value * 100f < chance) // Random.value includes 1.0
                {
                    var item = ItemCatalog.Get(drop.ItemId);
                    if (item != null && killer.Inventory.Add(item.Id, 1) > 0)
                    {
                        ChatLog.Loot($"You got {item.Name} (1).");
                    }
                }
            }
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
