using System;
using Runeheir.Characters;
using Runeheir.Combat;
using Runeheir.Field;
using Runeheir.Hotkeys;
using Runeheir.Items;
using Runeheir.Jobs;
using Runeheir.Monsters;
using Runeheir.Movement;
using Runeheir.Session;
using Runeheir.Skills;
using Runeheir.Stats;
using Runeheir.Visuals;
using Runeheir.WorldBuilding;
using UnityEngine;
using UnityEngine.AI;

namespace Runeheir.Player
{
    /// <summary>
    /// The playable character: binds the Base 255 / Job 120 stat engine (<see cref="CharacterProgression"/>,
    /// <see cref="DerivedStats"/>) to HP/SP, regen, EXP, items, equipment, buffs and death/respawn.
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
        private WeaponProfile _weapon = WeaponProfile.BareHands;

        /// <summary>The local player (Phase 6 will have many PlayerCharacters, one of them local).</summary>
        public static PlayerCharacter Local { get; private set; }

        public CharacterRecord Record { get; private set; }

        public CharacterProgression Progression { get; private set; }

        public Inventory Inventory { get; private set; }

        /// <summary>The 10-slot paperdoll (GDD §5).</summary>
        public EquipmentSet Equipment { get; private set; }

        /// <summary>What the worn gear, its cards and runeword add up to (recomputed with the stats).</summary>
        public EquipmentStats Gear { get; private set; } = new EquipmentStats();

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

        protected override float ReflectMeleePercent => Stats?.ReflectMeleePercent ?? 0f;

        protected override float ReflectMagicPercent => Stats?.ReflectMagicPercent ?? 0f;

        /// <summary>The equipped weapon with its refine and runeword element; bare hands when none is equipped.</summary>
        public WeaponProfile Weapon => _weapon;

        /// <summary>Bag plus worn gear.</summary>
        public int CurrentWeight => Inventory.TotalWeight() + Equipment.TotalWeight();

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
            Equipment = new EquipmentSet(record, Inventory);
            Hotkeys = new HotkeyLayout(record.Hotkeys);
            SkillBook = new SkillBook(record);

            Progression.StatsChanged += Recalculate;
            Progression.BaseLevelUp += OnBaseLevelUp;
            Progression.JobLevelUp += OnJobLevelUp;
            Progression.JobChanged += OnJobChanged;
            Buffs.Changed += Recalculate;
            Statuses.Changed += Recalculate;
            SkillBook.Changed += Recalculate;
            Equipment.Changed += OnEquipmentChanged;

            Recalculate();
            SetVitals(record.Hp < 0 ? MaxHp : record.Hp, MaxHp, record.Sp < 0 ? MaxSp : record.Sp, MaxSp);
            if (Hp <= 0)
            {
                SetVitals(MaxHp / 2, MaxHp, Sp, MaxSp);
            }

            Local = this;
        }

        /// <summary>Stat engine input: gear + cards + buffs + statuses + learned passives (for the current job and weapon).</summary>
        public void Recalculate()
        {
            Gear = EquipmentStats.Compute(Record);
            _weapon = Gear.HasWeapon ? Gear.Weapon : WeaponProfile.BareHands;

            var modifiers = StatModifiers.Empty();
            modifiers.Add(Gear.Modifiers);
            modifiers.Add(Buffs.Aggregate);
            modifiers.Add(Statuses.Aggregate);
            if (SkillBook != null)
            {
                modifiers.Add(SkillBook.PassiveModifiers(Weapon.Type));
            }

            Stats = DerivedStats.Compute(Record.BaseLevel, Record.Stats, modifiers, Weapon);
            var resist = Stats.StatusResistances;
            resist.ImmunityMask = Gear.ImmunityMask;
            resist.ExtraResist = Gear.ExtraResist;
            Stats.StatusResistances = resist;
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
                DefBypassPercent = Stats.DefBypassPercent,
                MdefBypassPercent = Stats.MdefBypassPercent,
                CritDamagePercent = Stats.CritDamagePercent,
                Race = Race.DemiHuman,
                Bonuses = Gear.Bonuses,
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
                Element = Gear.ArmorElement,
                Race = Race.DemiHuman,
                Size = Size.Medium,
                BluntDamageTakenMultiplier = BluntDamageTakenMultiplier,
                Resist = Gear.Bonuses,
            };
        }

        public override DamageResult RollBasicAttack(CombatEntity target)
        {
            bool forcedCrit = Buffs.HasCharge(BuffTraits.CriticalCharges);

            // Attacking reveals you; out of Shadow Veil that first attack is a guaranteed critical backstab.
            bool ambush = false;
            if (IsHidden)
            {
                Buffs.BreakStealth(out ambush);
            }

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

        /// <summary>Card leech and on-hit cards, then passive and buff procs: Storm Fists, Keen Edge, Auto Rune.</summary>
        public override void OnBasicAttackLanded(CombatEntity target, DamageResult result)
        {
            OnPhysicalHitLanded(target, result);
            if (target == null || target.IsDead || IsDead)
            {
                return;
            }

            TriggerGearProcs(target, melee: !IsRangedAttacker);
            if (_caster == null || target.IsDead)
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

        /// <summary>Crypt Bat, Corrupted Einherjar and Abyssal Leech: heal and restore SP from physical hits that connect.</summary>
        public void OnPhysicalHitLanded(CombatEntity target, DamageResult applied)
        {
            if (IsDead || Stats == null || applied.Amount <= 0 || applied.IsDamageOverTime)
            {
                return;
            }

            if (Stats.LifeStealPercent > 0f)
            {
                Heal(Mathf.Max(1, Mathf.RoundToInt(applied.Amount * Stats.LifeStealPercent / 100f)), showNumber: false);
            }

            int sp = Stats.SpStealPercent > 0f ? Mathf.Max(1, Mathf.RoundToInt(applied.Amount * Stats.SpStealPercent / 100f)) : 0;
            if (Stats.SpDrainOnHit > 0)
            {
                // Monsters have no SP pool in Phase 4: the leech pulls its full amount; players (Phase 6) lose what you gain.
                sp += Stats.SpDrainOnHit;
                if (target != null && target.MaxSp > 1)
                {
                    target.DrainSp(Stats.SpDrainOnHit);
                }
            }

            RestoreSp(sp, showNumber: false);
        }

        /// <summary>Chance-on-hit effects from cards and runewords (Toxic Spore, Fenrir, Hel's Executioner, Jormungandr's Brood...).</summary>
        private void TriggerGearProcs(CombatEntity target, bool melee)
        {
            foreach (var effect in Gear.OnHit)
            {
                if (target.IsDead || (effect.MeleeOnly && !melee))
                {
                    continue;
                }

                switch (effect.Kind)
                {
                    case OnHitKind.Status when effect.Guaranteed:
                        // "Unblockable": no resistance roll, but status-immune bosses still shrug it off.
                        if (SystemRandomSource.Shared.Chance(effect.ChancePercent))
                        {
                            target.ApplyStatus(effect.Status, effect.Duration);
                        }

                        break;
                    case OnHitKind.Status:
                        target.TryApplyStatus(effect.Status, effect.ChancePercent, effect.Duration);
                        break;
                    case OnHitKind.SelfBuff:
                        var buff = BuffCatalog.Get(effect.BuffId);
                        if (buff != null && !Buffs.Has(buff.Id) && SystemRandomSource.Shared.Chance(effect.ChancePercent))
                        {
                            Buffs.Apply(buff, Time.timeAsDouble);
                            WorldFeedback.Announce(this, buff.Name + "!", new Color(1f, 0.55f, 0.3f));
                        }

                        break;
                    case OnHitKind.PoiseBreak:
                        if (SystemRandomSource.Shared.Chance(effect.ChancePercent) && !target.IsStaggered)
                        {
                            WorldFeedback.Announce(target, "Mortal Stagger!", new Color(1f, 0.35f, 0.3f));
                            target.ApplyPoiseDamage(target.Poise.Max + 1f);
                        }

                        break;
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
            if (item != null && item.IsEquipment)
            {
                // Hotkeyed or double-clicked gear: wear the first matching piece in the bag.
                return Equip(Inventory.FindFirst(item.Id));
            }

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

        /// <summary>Wears a piece from the bag; whatever it displaces goes back into the bag.</summary>
        public bool Equip(ItemStack entry)
        {
            if (entry == null || IsDead)
            {
                return false;
            }

            if (!Equipment.TryEquip(entry, out string reason))
            {
                ChatLog.Error(reason);
                return false;
            }

            return true;
        }

        public bool Unequip(EquipPosition position)
        {
            if (!Equipment.TryUnequip(position, out string reason))
            {
                ChatLog.Error(reason);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Forge and card work happen on pieces in the bag (Core rules). A worn piece is taken off for the work and put back
        /// on if it survived (a shattered piece doesn't). Returns the work's result, or default when it couldn't be taken off.
        /// </summary>
        public T WorkOnPiece<T>(ItemStack entry, Func<T> work)
        {
            EquipPosition? worn = null;
            foreach (var pair in Equipment.Worn())
            {
                if (pair.Value == entry)
                {
                    worn = pair.Key;
                }
            }

            int hp = Hp;
            int sp = Sp;
            if (worn.HasValue && !Equipment.TryUnequip(worn.Value, out string reason))
            {
                ChatLog.Error(reason);
                return default;
            }

            try
            {
                return work();
            }
            finally
            {
                if (worn.HasValue && Inventory.Contains(entry))
                {
                    Equipment.TryEquip(entry, out _);

                    // Taking +Max HP/SP gear off clamped HP/SP; putting it back must not leave them lower.
                    if (!IsDead)
                    {
                        SetVitals(Mathf.Min(hp, MaxHp), MaxHp, Mathf.Min(sp, MaxSp), MaxSp);
                    }
                }
            }
        }

        public bool IsWorn(ItemStack entry)
        {
            foreach (var pair in Equipment.Worn())
            {
                if (pair.Value == entry)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnEquipmentChanged()
        {
            Recalculate();
            RebuildAvatar();
        }

        private void RebuildAvatar()
        {
            var avatar = GetComponentInChildren<PlaceholderAvatar>();
            if (avatar == null)
            {
                return;
            }

            avatar.RebuildHumanoid(AvatarLook.FromRecord(Record));
            if (_animation != null)
            {
                _animation.RefreshHidden(); // the rebuilt parts need the stealth look too
            }
        }

        /// <summary>
        /// A monster skill rolled to smash the worn weapon (Ancient Golem Card protects it). A broken weapon stays in your
        /// hands but does nothing until Brokk repairs it.
        /// </summary>
        public bool TryBreakWeapon(float chancePercent)
        {
            var broken = WeaponBreakRules.TryBreak(Record, chancePercent, SystemRandomSource.Shared);
            if (broken == null)
            {
                return false;
            }

            Equipment.NotifyChanged();
            WorldFeedback.Announce(this, "Weapon Broken!", new Color(1f, 0.35f, 0.3f));
            ChatLog.Error($"Your {broken.Definition.Name} broke! Brokk at Vigrid Haven's forge can repair it.");
            return true;
        }

        /// <summary>Dead Branch / Blood Branch: a monster bursts out next to you, already hunting you.</summary>
        private bool SummonFromBranch(bool boss)
        {
            if (!FieldContext.AllowsBranches)
            {
                ChatLog.Error("Branches can't be cracked inside Vigrid Haven. Take them to the Hall of Branches.");
                return false;
            }

            var definition = MonsterCatalog.PickForBranch(boss, SystemRandomSource.Shared);
            Vector2 offset = UnityEngine.Random.insideUnitCircle.normalized * 2.5f;
            Vector3 point = Position + new Vector3(offset.x, 0f, offset.y);
            if (definition == null || !NavMesh.SamplePosition(point, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                ChatLog.Error("The branch crumbles: nothing can grow here.");
                return false;
            }

            var monster = EntityFactory.CreateMonster(definition, hit.position, UnityEngine.Random.Range(0f, 360f));
            monster.Provoke(this);
            GroundRing.SpawnPulse(hit.position, boss ? new Color(0.9f, 0.15f, 0.2f, 1f) : new Color(0.55f, 0.4f, 0.25f, 1f), 0.3f, 2.5f, 0.7f);
            ChatLog.Notice($"The branch splinters and a {definition.Name} (Lv {definition.Level}) bursts out!");
            return true;
        }

        private bool ApplyItemEffect(ItemDefinition item)
        {
            switch (item.Special)
            {
                case ItemSpecialEffect.SummonMonster:
                    return SummonFromBranch(boss: false);
                case ItemSpecialEffect.SummonBoss:
                    return SummonFromBranch(boss: true);
                case ItemSpecialEffect.ReturnToSavePoint:
                    WorldTravel.ToSavePoint(this, "You return to your save point.");
                    return true;
                case ItemSpecialEffect.RandomTeleport:
                    if (!FieldContext.AllowsRandomTeleport)
                    {
                        ChatLog.Error("The wind rune won't carry you here.");
                        return false;
                    }

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
            WorldTravel.ToSavePoint(this, "You have been revived at your save point.");
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
            // Die() cleared the stealth buff; Update stops running while dead, so drop the silhouette here.
            if (_animation != null)
            {
                _animation.SetHidden(false);
            }

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

            // Mid-warp the record already points at the destination map; this map's position no longer applies.
            if (WorldTravel.InTransit)
            {
                return;
            }

            Record.MapId = FieldContext.MapId ?? Record.MapId;

            // Leaving while dead (generated world): you wake at your save point, on your save map.
            if (IsDead && FieldContext.Layout != null && World.MapCatalog.Get(Record.SaveMapId) != null)
            {
                Record.MapId = Record.SaveMapId;
            }

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
            ChatLog.Notice($"{DisplayName} is now a {Job.Name}!");

            // Gear the new job can't use goes to the bag; the guild hands over the job's own weapon.
            int removed = Equipment.RemoveUnwearable();
            if (removed > 0)
            {
                ChatLog.System($"{removed} piece(s) of gear went back to your bag: a {Job.Name} can't use them.");
            }

            var gift = Equipment.GiftJobWeapon();
            if (gift != null)
            {
                ChatLog.Loot($"Job change gift: {gift.DisplayName}{(Equipment.Get(EquipPosition.Weapon) == gift ? " (equipped)" : " (in your bag)")}.");
            }

            Recalculate();
            RebuildAvatar();
            SaveNow();
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
            if (Equipment != null)
            {
                Equipment.Changed -= OnEquipmentChanged;
            }

            if (SkillBook != null)
            {
                SkillBook.Changed -= Recalculate;
            }
        }
    }
}
