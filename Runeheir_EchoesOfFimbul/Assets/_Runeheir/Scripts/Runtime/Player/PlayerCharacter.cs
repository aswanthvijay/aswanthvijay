using System;
using Runeheir.Characters;
using Runeheir.Combat;
using Runeheir.Field;
using Runeheir.Hotkeys;
using Runeheir.Items;
using Runeheir.Jobs;
using Runeheir.Movement;
using Runeheir.Session;
using Runeheir.Skills;
using Runeheir.Stats;
using Runeheir.Visuals;
using UnityEngine;

namespace Runeheir.Player
{
    /// <summary>
    /// The playable character: binds the Base 255 / Job 120 stat engine (<see cref="CharacterProgression"/>,
    /// <see cref="DerivedStats"/>) to HP/SP, regen, EXP, items, buffs and death/respawn.
    /// </summary>
    [RequireComponent(typeof(NavMotor))]
    public sealed class PlayerCharacter : CombatEntity
    {
        [Tooltip("Ragnarok death penalty: % of the current level's EXP lost on death.")]
        [SerializeField, Range(0f, 10f)] private float deathExpPenaltyPercent = 1f;

        private NavMotor _motor;
        private AutoAttacker _attacker;
        private SkillCaster _caster;
        private CharacterAnimationBridge _animation;
        private float _nextHpRegenAt;
        private float _nextSpRegenAt;

        /// <summary>The local player (Phase 6 will have many PlayerCharacters, one of them local).</summary>
        public static PlayerCharacter Local { get; private set; }

        public CharacterRecord Record { get; private set; }

        public CharacterProgression Progression { get; private set; }

        public Inventory Inventory { get; private set; }

        public HotkeyLayout Hotkeys { get; private set; }

        /// <summary>Learned skills and skill points (Phase 3 skill tree).</summary>
        public SkillBook SkillBook { get; private set; }

        public DerivedStats Stats { get; private set; }

        public JobInfo Job => JobDatabase.Get(Record.Job);

        /// <summary>Rage of Thor's hyper-armor ignores knockback.</summary>
        public override bool CanBeKnockedBack => Stats == null || !Stats.HyperArmor;

        public override bool StaggerImmune => Stats != null && Stats.StaggerImmune;

        public override StatusResistances StatusResistances => Stats?.StatusResistances ?? default;

        /// <summary>Weapon poise (greatswords stagger fastest), raised by Thurisaz-style bonuses.</summary>
        public override float BasicPoiseDamage => WeaponRules.PoiseDamage(Weapon.Type) * (1f + (Stats?.PoiseDamagePercent ?? 0f) / 100f);

        protected override float DamageTakenPercent => Stats?.DamageTakenPercent ?? 0f;

        protected override float BlockChance => Stats?.BlockChance ?? 0f;

        public WeaponProfile Weapon => Job.StarterWeapon;

        /// <summary>Derived stats were recomputed (level, stat points, buffs, job).</summary>
        public event Action StatsRecalculated;

        public override Faction Faction => Faction.Player;

        public override string DisplayName => Record != null ? Record.Name : name;

        public override int Level => Record?.BaseLevel ?? 1;

        public override float AttackRange => Stats?.AttackRange ?? 1f;

        public override float AttackInterval => Stats?.AttackInterval ?? 1f;

        public override float Aspd => Stats?.Aspd ?? StatFormulas.MinAspd;

        public override float AttackPlayRate => Stats?.AttackPlayRate ?? 1f;

        public override float SwingDuration => Stats?.SwingDuration ?? StatFormulas.BaseSwingSeconds;

        public override bool IsRangedAttacker => WeaponRules.IsRanged(Weapon.Type);

        public void Initialize(CharacterRecord record)
        {
            Record = record ?? throw new ArgumentNullException(nameof(record));
            Record.Sanitize();
            Progression = new CharacterProgression(record);
            Inventory = new Inventory(record.Inventory);
            Hotkeys = new HotkeyLayout(record.Hotkeys);
            SkillBook = new SkillBook(record);

            Progression.StatsChanged += Recalculate;
            Progression.BaseLevelUp += OnBaseLevelUp;
            Progression.JobLevelUp += OnJobLevelUp;
            Progression.JobChanged += OnJobChanged;
            Buffs.Changed += Recalculate;
            Statuses.Changed += Recalculate;
            SkillBook.Changed += Recalculate;

            Recalculate();
            SetVitals(record.Hp < 0 ? MaxHp : record.Hp, MaxHp, record.Sp < 0 ? MaxSp : record.Sp, MaxSp);
            if (Hp <= 0)
            {
                SetVitals(MaxHp / 2, MaxHp, Sp, MaxSp);
            }

            Local = this;
        }

        /// <summary>Stat engine input: buffs + statuses + learned passives (for the current job and weapon).</summary>
        public void Recalculate()
        {
            var modifiers = StatModifiers.Empty();
            modifiers.Add(Buffs.Aggregate);
            modifiers.Add(Statuses.Aggregate);
            if (SkillBook != null)
            {
                modifiers.Add(SkillBook.PassiveModifiers(Weapon.Type));
            }

            Stats = DerivedStats.Compute(Record.BaseLevel, Record.Stats, modifiers, Weapon);
            SetVitals(Mathf.Min(Hp, Stats.MaxHp), Stats.MaxHp, Mathf.Min(Sp, Stats.MaxSp), Stats.MaxSp);
            Poise.SetMax(Stats.MaxPoise);
            ApplyMoveSpeed();
            StatsRecalculated?.Invoke();
        }

        private bool _castSpeedApplied;

        /// <summary>Normal speed, or the Free Cast fraction while the cast bar fills.</summary>
        private void ApplyMoveSpeed()
        {
            if (_motor == null || Stats == null)
            {
                return;
            }

            _castSpeedApplied = _caster != null && _caster.IsCasting && Stats.CastMoveSpeedPercent > 0f;
            _motor.SpeedMultiplier = _castSpeedApplied
                ? Stats.MoveSpeedMultiplier * Stats.CastMoveSpeedPercent / 100f
                : Stats.MoveSpeedMultiplier;
        }

        public override AttackerProfile BuildAttackerProfile()
        {
            return new AttackerProfile
            {
                StatusAtk = Stats.StatusAtk,
                WeaponAtk = Stats.WeaponAtk,
                WeaponVariance = 0.05f,
                Weapon = Weapon.Type,
                AttackElement = Weapon.Element,
                Hit = Stats.Hit,
                CritChance = Stats.Crit,
                ForceCritical = Buffs.HasCharge(BuffTraits.CriticalCharges),
                MatkMin = Stats.MatkMin,
                MatkMax = Stats.MatkMax,
                PhysicalDamagePercent = Stats.PhysicalDamagePercent,
                MagicDamagePercent = Stats.MagicDamagePercent,
            };
        }

        public override DefenderProfile BuildDefenderProfile()
        {
            return new DefenderProfile
            {
                Def = Stats.Def,
                SoftDef = Stats.SoftDef,
                Mdef = Stats.Mdef,
                SoftMdef = Stats.SoftMdef,
                Flee = Stats.Flee,
                Element = Element.Neutral,
                Race = Race.DemiHuman,
                Size = Size.Medium,
                BluntDamageTakenMultiplier = BluntDamageTakenMultiplier,
            };
        }

        public override DamageResult RollBasicAttack(CombatEntity target)
        {
            bool forcedCrit = Buffs.HasCharge(BuffTraits.CriticalCharges);

            // Attacking reveals you; out of Shadow Veil that first attack is a guaranteed critical backstab.
            var stealth = IsHidden ? Buffs.BreakStealth() : null;
            bool ambush = stealth != null && stealth.Definition.Has(BuffTraits.AmbushCritical);

            var attacker = BuildAttackerProfile();
            attacker.ForceCritical |= ambush;
            var result = DamageCalculator.Physical(attacker, target.BuildDefenderProfile(), 100f, true, SystemRandomSource.Shared);
            if (forcedCrit)
            {
                Buffs.TryConsumeCharge(BuffTraits.CriticalCharges);
            }

            if (ambush)
            {
                WorldFeedback.Announce(this, "Ambush!", new Color(0.75f, 0.6f, 1f));
            }

            return result;
        }

        /// <summary>Passive and buff procs: Storm Fists, Keen Edge, Auto Rune.</summary>
        public override void OnBasicAttackLanded(CombatEntity target, DamageResult result)
        {
            if (_caster == null || target == null || target.IsDead)
            {
                return;
            }

            foreach (var pair in SkillBook.PassiveProcs(Weapon.Type))
            {
                if (TryProc(pair.Key.Proc, pair.Value, target))
                {
                    return; // at most one proc per hit
                }
            }

            foreach (var buff in Buffs.Active)
            {
                if (buff.Definition.Proc != null && TryProc(buff.Definition.Proc, buff.Level, target))
                {
                    return;
                }
            }
        }

        private bool TryProc(ProcDefinition proc, int ownerLevel, CombatEntity target)
        {
            if (!SystemRandomSource.Shared.Chance(proc.Chance.At(ownerLevel)))
            {
                return false;
            }

            var candidates = new System.Collections.Generic.List<SkillDefinition>();
            foreach (string id in proc.SkillIds)
            {
                var skill = SkillCatalog.Get(id);
                if (skill != null && (!proc.OnlyLearned || SkillBook.UsableLevel(id) > 0))
                {
                    candidates.Add(skill);
                }
            }

            if (candidates.Count == 0)
            {
                return false;
            }

            var chosen = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            int level = proc.ProcLevel.AtInt(ownerLevel);
            if (proc.OnlyLearned)
            {
                level = Mathf.Min(level, SkillBook.UsableLevel(chosen.Id));
            }

            _caster.CastProc(chosen, Mathf.Max(1, level), target);
            return true;
        }

        // ------------------------------------------------------------ EXP
        /// <summary>Rates are already applied by the caller (monster kill).</summary>
        public void GrantExperience(long baseExp, long jobExp)
        {
            if (IsDead)
            {
                return;
            }

            Progression.GainExperience(baseExp, jobExp);
        }

        // ------------------------------------------------------------ items
        public bool UseItem(string itemId)
        {
            var item = ItemCatalog.Get(itemId);
            if (item == null || !item.IsUsable)
            {
                return false;
            }

            if (IsDead)
            {
                return false;
            }

            if (Stats.ItemsLocked)
            {
                ChatLog.Error("You can't use items during Rage of Thor.");
                return false;
            }

            if (!Inventory.Has(item.Id))
            {
                ChatLog.Error($"You have no {item.Name} left.");
                return false;
            }

            if (!ApplyItemEffect(item))
            {
                return false;
            }

            Inventory.TryRemove(item.Id);
            return true;
        }

        private bool ApplyItemEffect(ItemDefinition item)
        {
            switch (item.Special)
            {
                case ItemSpecialEffect.ReturnToSavePoint:
                    TeleportTo(FieldContext.SavePoint, "You return to your save point.");
                    return true;
                case ItemSpecialEffect.RandomTeleport:
                    if (!FieldContext.TryGetRandomPoint(out var point))
                    {
                        ChatLog.Error("The rune fizzles: nowhere to go.");
                        return false;
                    }

                    TeleportTo(point, null);
                    return true;
                case ItemSpecialEffect.Cleanse:
                    Cleanse();
                    break;
            }

            int hp = 0;
            if (item.HealHpMax > 0)
            {
                hp += UnityEngine.Random.Range(item.HealHpMin, item.HealHpMax + 1);
            }

            if (item.HealHpPercent > 0f)
            {
                hp += Mathf.RoundToInt(MaxHp * item.HealHpPercent / 100f);
            }

            Heal(hp);
            if (item.HealSpMax > 0)
            {
                RestoreSp(UnityEngine.Random.Range(item.HealSpMin, item.HealSpMax + 1));
            }

            var buff = BuffCatalog.Get(item.BuffId);
            if (buff != null)
            {
                Buffs.Apply(buff, Time.timeAsDouble);
            }

            return true;
        }

        public void TeleportTo(Vector3 point, string message)
        {
            _attacker.Disengage();
            _caster.CancelAll();
            if (_motor.Warp(point))
            {
                GroundRing.SpawnPulse(transform.position, new Color(0.6f, 0.9f, 1f, 1f), 0.2f, 1.6f, 0.5f);
                if (!string.IsNullOrEmpty(message))
                {
                    ChatLog.System(message);
                }
            }
        }

        // ------------------------------------------------------------ death / respawn
        public void RespawnAtSavePoint()
        {
            if (!IsDead)
            {
                return;
            }

            Revive(Mathf.Max(1, MaxHp / 2), Sp);
            if (_animation != null)
            {
                _animation.SetDead(false);
            }

            _motor.SetLocked(false);
            _motor.Warp(FieldContext.SavePoint);
            ChatLog.System("You have been revived at your save point.");
        }

        protected override void OnDamaged(DamageResult result, CombatEntity attacker)
        {
            if (result.Amount <= 0 || result.IsDamageOverTime)
            {
                return;
            }

            // Rage of Thor hyper-armor: no flinch (casting is already uninterruptible through the same buff).
            if (_animation != null && !Stats.HyperArmor)
            {
                _animation.PlayHit();
            }

            _caster.InterruptByDamage();
        }

        /// <summary>Stuns, freezes, sleep, stone, stagger and silence cancel a cast even through hyper-armor's no-flinch.</summary>
        protected override void OnStatusApplied(StatusEffect effect)
        {
            var info = StatusRules.Get(effect);
            if (info == null || _caster == null)
            {
                return;
            }

            if (info.Has(StatusFlags.Incapacitates) || info.Has(StatusFlags.BlocksSkills))
            {
                _caster.InterruptHard(info.Name.ToLowerInvariant());
            }

            if (effect == StatusEffect.Stagger)
            {
                WorldFeedback.Announce(this, "Staggered!", new Color(1f, 0.6f, 0.4f));
                if (_animation != null)
                {
                    _animation.PlayStagger();
                }
            }
        }

        protected override void OnDied(CombatEntity killer)
        {
            _attacker.Disengage();
            _caster.CancelAll();
            _motor.Stop();
            if (_animation != null)
            {
                _animation.SetDead(true);
            }

            long lost = Progression.ApplyDeathPenalty(deathExpPenaltyPercent);
            ChatLog.Error(lost > 0
                ? $"You have been defeated by {killer?.DisplayName ?? "the frost"}. Lost {lost:N0} Base EXP."
                : $"You have been defeated by {killer?.DisplayName ?? "the frost"}.");
        }

        // ------------------------------------------------------------ persistence
        /// <summary>Copies live state (HP/SP/position) into the record before saving.</summary>
        public void WriteBackToRecord()
        {
            Record.Hp = IsDead ? Mathf.Max(1, MaxHp / 2) : Hp;
            Record.Sp = Sp;
            Record.MapId = FieldContext.MapId ?? Record.MapId;
            Record.HasSavedPosition = !IsDead;
            Vector3 position = IsDead ? FieldContext.SavePoint : transform.position;
            Record.PosX = position.x;
            Record.PosY = position.y;
            Record.PosZ = position.z;
        }

        // ------------------------------------------------------------ level-ups
        private void OnBaseLevelUp(int level)
        {
            Recalculate();
            if (!IsDead)
            {
                // A level-up refills HP/SP, but never revives (e.g. @blvl typed while dead).
                SetVitals(MaxHp, MaxHp, MaxSp, MaxSp);
            }

            WorldFeedback.Announce(this, "Base Level Up!", new Color(1f, 0.85f, 0.3f));
            GroundRing.SpawnPulse(transform.position, new Color(1f, 0.85f, 0.3f, 1f), 0.3f, 2.2f, 0.8f, 0.12f);
            ChatLog.Notice($"Congratulations! {DisplayName} reached Base Level {level}. ({Record.StatPoints} status points)");
            SaveNow();
        }

        /// <summary>Writes live state into the record and asks the account service to persist it.</summary>
        public System.Threading.Tasks.Task<Accounts.OpResult> SaveNow()
        {
            WriteBackToRecord();
            if (GameSession.Instance.ActiveCharacter != Record)
            {
                return System.Threading.Tasks.Task.FromResult(Accounts.OpResult.Fail("This character is not the active one."));
            }

            return GameSession.Instance.SaveActiveCharacter();
        }

        private void OnJobLevelUp(int level)
        {
            WorldFeedback.Announce(this, "Job Level Up!", new Color(0.45f, 0.9f, 1f));
            ChatLog.Notice($"{DisplayName} reached Job Level {level}.");
        }

        private void OnJobChanged()
        {
            Recalculate();
            var avatar = GetComponentInChildren<PlaceholderAvatar>();
            if (avatar != null)
            {
                avatar.RebuildHumanoid(AvatarLook.FromRecord(Record));
            }

            ChatLog.Notice($"{DisplayName} is now a {Job.Name}!");
        }

        // ------------------------------------------------------------ Unity
        private void Awake()
        {
            _motor = GetComponent<NavMotor>();
            _attacker = GetComponent<AutoAttacker>();
            _caster = GetComponent<SkillCaster>();
            _animation = GetComponent<CharacterAnimationBridge>();
            bodyRadius = 0.4f;
            bodyHeight = 1.8f;
        }

        private void Start()
        {
            if (_attacker != null && _caster != null)
            {
                _attacker.SetBusyCheck(() => _caster.IsCasting);
            }
        }

        protected override void Update()
        {
            base.Update();
            if (Record == null || IsDead)
            {
                return;
            }

            bool casting = _caster != null && _caster.IsCasting;
            _motor.SetLocked(IsIncapacitated || !CanMove || (casting && !_caster.CanMoveWhileCasting));
            if (_castSpeedApplied != (casting && Stats.CastMoveSpeedPercent > 0f))
            {
                ApplyMoveSpeed();
            }

            if (_animation != null)
            {
                _animation.SetHidden(IsHidden);
            }

            // Poison and bleeding stop natural regeneration.
            float now = Time.time;
            if (now >= _nextHpRegenAt)
            {
                _nextHpRegenAt = now + StatFormulas.HpRegenIntervalSeconds;
                if (Hp < MaxHp && !Statuses.BlocksRegen)
                {
                    Heal(Stats.HpRegenPerTick, showNumber: false);
                }
            }

            if (now >= _nextSpRegenAt)
            {
                _nextSpRegenAt = now + StatFormulas.SpRegenIntervalSeconds;
                if (Sp < MaxSp && !Statuses.BlocksRegen)
                {
                    RestoreSp(Stats.SpRegenPerTick, showNumber: false);
                }
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Local == this)
            {
                Local = null;
            }

            if (Progression != null)
            {
                Progression.StatsChanged -= Recalculate;
                Progression.BaseLevelUp -= OnBaseLevelUp;
                Progression.JobLevelUp -= OnJobLevelUp;
                Progression.JobChanged -= OnJobChanged;
            }

            Buffs.Changed -= Recalculate;
            Statuses.Changed -= Recalculate;
            if (SkillBook != null)
            {
                SkillBook.Changed -= Recalculate;
            }
        }
    }
}
