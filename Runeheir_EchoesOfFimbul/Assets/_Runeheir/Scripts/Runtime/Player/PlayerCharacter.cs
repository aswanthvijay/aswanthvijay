using System;
using Runeheir.Audio;
using Runeheir.Characters;
using Runeheir.Combat;
using Runeheir.Field;
using Runeheir.Hotkeys;
using Runeheir.Items;
using Runeheir.Jobs;
using Runeheir.Monsters;
using Runeheir.Movement;
using Runeheir.Online;
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
    public sealed class PlayerCharacter : PlayerEntity
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
            SkillCaster.AnySkillUsed += OnAnySkillUsed;

            Recalculate();
            SetVitals(record.Hp < 0 ? MaxHp : record.Hp, MaxHp, record.Sp < 0 ? MaxSp : record.Sp, MaxSp);
            if (Hp <= 0)
            {
                SetVitals(MaxHp / 2, MaxHp, Sp, MaxSp);
            }

            Local = this;
            RefreshPet();
            Monster.MercenaryFell -= OnMercenaryFell;
            Monster.MercenaryFell += OnMercenaryFell;
            RefreshMercenary();
            RefreshHomunculus();
            ApplyMount(rebuild: false);
        }

        /// <summary>Stat engine input: gear + cards + buffs + statuses + learned passives (for the current job and weapon).</summary>
        public void Recalculate()
        {
            _raven = RavenCompanion.Sync(_raven, transform, SkillBook != null && SkillBook.UsableLevel("raven_strike") > 0 && !IsDead);
            Gear = EquipmentStats.Compute(Record);
            _weapon = Gear.HasWeapon ? Gear.Weapon : WeaponProfile.BareHands;
            SkillBook?.SetGranted(Gear.GrantedSkills);

            var modifiers = StatModifiers.Empty();
            modifiers.Add(Gear.Modifiers);
            modifiers.Add(Social.PushcartRules.Modifiers(Record));
            modifiers.Add(RebirthRules.Modifiers(Record)); // reborn: +25% Max HP and SP
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
                PhysicalDamagePercent = Stats.PhysicalDamagePercent + (WeaponRules.IsRanged(Gear.Weapon.Type) ? Stats.LongRangePercent : 0f),
                MagicDamagePercent = Stats.MagicDamagePercent,
                DefBypassPercent = Stats.DefBypassPercent,
                MdefBypassPercent = Stats.MdefBypassPercent,
                CritDamagePercent = Stats.CritDamagePercent,
                Race = Race.DemiHuman,
                Size = Size.Medium,
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
                PerfectDodge = Stats.PerfectDodge,
                Element = Gear.ArmorElement,
                Race = Race.DemiHuman,
                Size = Size.Medium,
                BluntDamageTakenMultiplier = BluntDamageTakenMultiplier,
                Resist = Gear.Bonuses,
            };
        }

        public override DamageResult RollBasicAttack(CombatEntity target)
        {
            GetDownToFight();
            bool forcedCrit = Buffs.HasCharge(BuffTraits.CriticalCharges);

            // Attacking reveals you; out of Shadow Veil that first attack is a guaranteed critical backstab.
            bool ambush = false;
            if (IsHidden)
            {
                Buffs.BreakStealth(out ambush);
            }

            var attacker = BuildAttackerProfile();
            attacker.ForceCritical |= ambush;
            var result = DamageCalculator.BasicAttack(attacker, target.BuildDefenderProfile(), SystemRandomSource.Shared);
            GameAudio.Play(SwingSound(Weapon.Type), Position, 0.65f);
            if (result.IsMiss)
            {
                GameAudio.Play(Sfx.Miss, target.Position, 0.5f);
            }
            if (Gear.AmmoInUse)
            {
                Equipment.SpendAmmo(); // every shot spends one arrow, round or shuriken
            }
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
        /// <summary>The sound of a basic attack leaving the weapon (Phase 8 audio).</summary>
        private static Sfx SwingSound(WeaponType type)
        {
            switch (type)
            {
                case WeaponType.Bow: return Sfx.BowShot;
                case WeaponType.ThunderRod: return Sfx.ShotFired;
                case WeaponType.TwoHandSword:
                case WeaponType.TwoHandAxe:
                case WeaponType.Mace:
                case WeaponType.Spear: return Sfx.SwingHeavy;
                default: return Sfx.SwingLight;
            }
        }

        public override void OnBasicAttackLanded(CombatEntity target, DamageResult result)
        {
            if (target != null)
            {
                bool heavy = Weapon.Type == WeaponType.TwoHandSword || Weapon.Type == WeaponType.TwoHandAxe || Weapon.Type == WeaponType.Mace;
                GameAudio.Play(result.IsCritical ? Sfx.Critical : IsRangedAttacker ? Sfx.ArrowHit : heavy ? Sfx.HitHeavy : Sfx.Hit, target.Position, 0.8f);
            }

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

        /// <summary>
        /// Gear and card auto-casts (Phase 8, Ragnarok's auto-spells): a free cast of an offensive skill at the enemy or a
        /// support skill on yourself, or a timed buff. At most one per trigger.
        /// </summary>
        private void TriggerAutoCasts(AutoCastTrigger trigger, CombatEntity enemy, string usedSkillId)
        {
            if (IsDead || Gear == null || Gear.AutoCasts.Count == 0 || _caster == null)
            {
                return;
            }

            foreach (var auto in Gear.AutoCasts)
            {
                if (auto.Trigger != trigger || (trigger == AutoCastTrigger.OnSkill && auto.TriggerSkillId != usedSkillId)
                    || !SystemRandomSource.Shared.Chance(auto.ChancePercent))
                {
                    continue;
                }

                if (auto.BuffId != null)
                {
                    var buff = BuffCatalog.Get(auto.BuffId);
                    if (buff == null)
                    {
                        continue;
                    }

                    Buffs.Apply(buff, Time.timeAsDouble);
                    WorldFeedback.Announce(this, buff.Name + "!", new Color(1f, 0.75f, 0.35f));
                    return;
                }

                var skill = SkillCatalog.Get(auto.SkillId);
                if (skill == null)
                {
                    continue;
                }

                bool support = skill.Target == SkillTarget.Self || skill.Target == SkillTarget.Friend;
                var target = support ? this : enemy;
                if (target == null || target.IsDead)
                {
                    continue;
                }

                int level = auto.Level > 0 ? auto.Level : Mathf.Max(1, SkillBook.UsableLevel(skill.Id));
                _caster.CastProc(skill, level, target);
                return;
            }
        }

        /// <summary>Called by the skill caster after you cast a skill yourself: gear that auto-casts on that skill.</summary>
        public void TriggerSkillAutoCasts(string skillId, CombatEntity target)
        {
            TriggerAutoCasts(AutoCastTrigger.OnSkill, target, skillId);
        }

        /// <summary>Chance-on-hit effects from cards and runewords (Toxic Spore, Fenrir, Hel's Executioner, Jormungandr's Brood...).</summary>
        private void TriggerGearProcs(CombatEntity target, bool melee)
        {
            TriggerAutoCasts(AutoCastTrigger.OnAttack, target, null);
            TryHuginnStrike(target);
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
        public override void GrantExperience(long baseExp, long jobExp)
        {
            if (IsDead)
            {
                return;
            }

            if (Stats != null && Stats.ExpPercent != 0f)
            {
                float scale = 1f + Stats.ExpPercent / 100f; // battle manuals and the like
                baseExp = (long)(baseExp * scale);
                jobExp = (long)(jobExp * scale);
            }

            Progression.GainExperience(baseExp, jobExp);
            ShareWithHomunculus(baseExp);
        }

        // ------------------------------------------------------------ items
        public bool UseItem(string itemId)
        {
            var item = ItemCatalog.Get(itemId);
            if (item != null && item.IsWearable)
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
                GameAudio.Play(Sfx.UiError, null, 0.5f);
                return false;
            }

            GameAudio.Play(Sfx.Equip, null, 0.6f);
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
        public override bool TryBreakWeapon(float chancePercent)
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

        /// <summary>Autoloot: a drop from a kill goes straight to the bag (if it fits); MVP rewards and cards get a cheer.</summary>
        public override void ReceiveLoot(ItemDefinition item, float baseChance, bool mvpReward)
        {
            if (item != null)
            {
                GameAudio.Play(item.IsCard ? Sfx.Critical : Sfx.Pickup, null, 0.6f);
            }

            if (item == null)
            {
                return;
            }

            if (CurrentWeight + item.Weight > Stats.WeightCapacity)
            {
                ChatLog.Error($"You are carrying too much to pick up {item.Name}.");
                return;
            }

            if (Inventory.Add(item.Id, 1) <= 0)
            {
                ChatLog.Error($"Your bag is full: {item.Name} was left behind.");
                return;
            }

            if (mvpReward || item.IsCard)
            {
                WorldFeedback.Announce(this, item.Name + "!", new Color(1f, 0.82f, 0.25f));
            }

            ChatLog.Loot($"You got {item.Name} (1).");
        }

        /// <summary>
        /// Goods waiting in the Pushcart hold (a closed stall, a trade that didn't fit) go back to the bag while there's room.
        /// <paramref name="openStall"/>'s goods stay on the stall.
        /// </summary>
        public int ReturnCartGoods(Social.VendingStall openStall = null)
        {
            if (Record.Cart == null || Record.Cart.Count == 0)
            {
                return 0;
            }

            int moved = Social.ItemTransfer.ReturnCartToBag(Record, Inventory, Stats.WeightCapacity, Equipment.TotalWeight(), openStall);
            if (moved > 0)
            {
                Inventory.NotifyChanged();
                ChatLog.Loot(Record.Cart.Count == 0 ? "Everything in your Pushcart hold is back in your bag." : "Some goods moved from your Pushcart hold to your bag.");
            }

            return moved;
        }

        public override void ReceiveMvp(string monsterName, long bonusExp)
        {
            GameAudio.Play(Sfx.MvpFanfare);
            WorldFeedback.Announce(this, "MVP!", new Color(1f, 0.82f, 0.25f));
            GroundRing.SpawnPulse(Position, new Color(1f, 0.82f, 0.25f, 1f), 0.4f, 4f, 1.2f, 0.16f);
            if (bonusExp > 0)
            {
                ChatLog.Notice($"MVP bonus: {bonusExp:N0} Base EXP.");
                GrantExperience(bonusExp, 0);
            }
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

            if (OnlineSession.IsRemoteClient)
            {
                // The realm runs the monsters: it picks and summons one here.
                OnlineSession.Current.RequestBranch(boss, hit.position);
                GroundRing.SpawnPulse(hit.position, boss ? new Color(0.9f, 0.15f, 0.2f, 1f) : new Color(0.55f, 0.4f, 0.25f, 1f), 0.3f, 2.5f, 0.7f);
                ChatLog.Notice("The branch splinters...");
                return true;
            }

            var monster = EntityFactory.CreateMonster(definition, hit.position, UnityEngine.Random.Range(0f, 360f));
            monster.Provoke(this);
            GroundRing.SpawnPulse(hit.position, boss ? new Color(0.9f, 0.15f, 0.2f, 1f) : new Color(0.55f, 0.4f, 0.25f, 1f), 0.3f, 2.5f, 0.7f);
            ChatLog.Notice($"The branch splinters and a {definition.Name} (Lv {definition.Level}) bursts out!");
            return true;
        }

        private bool ApplyItemEffect(ItemDefinition item)
        {
            if (!item.NotUsableYet)
            {
                GameAudio.Play(item.HealHpMax > 0 || item.HealHpPercent > 0f || item.HealSpMax > 0 ? Sfx.Heal : !string.IsNullOrEmpty(item.BuffId) ? Sfx.Buff : Sfx.Pickup, null, 0.6f);
            }

            if (item.NotUsableYet)
            {
                ChatLog.Error($"{item.Name} can't be used in Runeheir yet.");
                return false;
            }

            if (!OpenBox(item) || !CastScroll(item))
            {
                return false;
            }

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
                case ItemSpecialEffect.HatchPet:
                    if (!Pets.PetCatalog.Hatch(Record, Inventory, item.Id, out string hatched, consumeEgg: false))
                    {
                        ChatLog.Error(hatched);
                        return false;
                    }

                    ChatLog.Notice(hatched);
                    RefreshPet();
                    return true;
                case ItemSpecialEffect.TamePet:
                    return TryTame(item);
                case ItemSpecialEffect.HireMercenary:
                    if (!Mercenaries.MercenaryCatalog.Hire(Record, item.MercenaryId, out string hired))
                    {
                        ChatLog.Error(hired);
                        return false;
                    }

                    ChatLog.Notice(hired);
                    if (_mercenary != null)
                    {
                        Destroy(_mercenary.gameObject);
                        _mercenary = null;
                    }

                    RefreshMercenary();
                    GameAudio.Play(Sfx.Buff);
                    return true;
                case ItemSpecialEffect.Mount:
                    ToggleMount(Characters.MountRules.Horse);
                    return false; // the reins are never used up
                case ItemSpecialEffect.PetArmor:
                    if (!Pets.PetCatalog.WearArmor(Record, item.Id, out string worn))
                    {
                        ChatLog.Error(worn);
                        return false;
                    }

                    ChatLog.Notice(worn);
                    RefreshPet();
                    return true;
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
            int sp = item.HealSpMax > 0 ? UnityEngine.Random.Range(item.HealSpMin, item.HealSpMax + 1) : 0;
            if (item.HealSpPercent > 0f)
            {
                sp += Mathf.RoundToInt(MaxSp * item.HealSpPercent / 100f);
            }

            if (sp > 0)
            {
                RestoreSp(sp);
            }

            if (item.Cures != null)
            {
                foreach (var status in item.Cures)
                {
                    Statuses.Remove(status);
                }
            }

            var buff = BuffCatalog.Get(item.BuffId);
            if (buff != null)
            {
                Buffs.Apply(buff, Time.timeAsDouble);
            }

            return true;
        }

        /// <summary>Boxes, random boxes and zeny pouches (Phase 8). False (nothing used) when the bag can't take the contents.</summary>
        private bool OpenBox(ItemDefinition item)
        {
            int entries = (item.GivesItems?.Length ?? 0) + (item.RandomGives != null && item.RandomGives.Length > 0 ? 1 : 0);
            if (entries > 0 && Inventory.Stacks.Count + entries > Inventory.MaxEntries)
            {
                ChatLog.Error($"Your bag is too full to open the {item.Name}.");
                return false;
            }

            if (item.GivesItems != null)
            {
                foreach (var grant in item.GivesItems)
                {
                    GiveFromBox(grant);
                }
            }

            if (item.RandomGives != null && item.RandomGives.Length > 0)
            {
                int total = 0;
                foreach (var grant in item.RandomGives)
                {
                    total += Mathf.Max(1, grant.Weight);
                }

                int roll = UnityEngine.Random.Range(0, total);
                foreach (var grant in item.RandomGives)
                {
                    roll -= Mathf.Max(1, grant.Weight);
                    if (roll < 0)
                    {
                        GiveFromBox(grant);
                        break;
                    }
                }
            }

            if (item.GivesZenyMax > 0)
            {
                int zeny = UnityEngine.Random.Range(item.GivesZenyMin, item.GivesZenyMax + 1);
                Record.Zeny += zeny;
                ChatLog.Loot($"You found {zeny:N0} zeny.");
            }

            return true;
        }

        private void GiveFromBox(ItemGrant grant)
        {
            var definition = ItemCatalog.Get(grant.ItemId);
            if (definition == null)
            {
                return;
            }

            int added = Inventory.Add(definition.Id, Mathf.Max(1, grant.Amount));
            if (added > 0)
            {
                ChatLog.Loot($"You got {definition.Name}{(added > 1 ? $" x{added}" : string.Empty)}.");
            }
        }

        /// <summary>Scrolls: a free cast of their skill, support skills on you and attacks at your current target.</summary>
        private bool CastScroll(ItemDefinition item)
        {
            if (string.IsNullOrEmpty(item.CastSkillId))
            {
                return true;
            }

            var skill = SkillCatalog.Get(item.CastSkillId);
            if (skill == null || _caster == null)
            {
                return false;
            }

            bool support = skill.Target == SkillTarget.Self || skill.Target == SkillTarget.Friend;
            CombatEntity target = support ? this : _attacker != null ? _attacker.Target : null;
            if (target == null || target.IsDead)
            {
                ChatLog.Error($"Attack something first, then read the {item.Name}.");
                return false;
            }

            _caster.CastProc(skill, item.CastSkillLevel, target);
            return true;
        }

        // ------------------------------------------------------------ pets (Phase 8)
        private PetCompanion _pet;
        private float _petHungerTimer;

        /// <summary>Taming: use the item while fighting the monster it tames. Weaker monsters are easier; the item is used either way.</summary>
        private bool TryTame(ItemDefinition item)
        {
            var target = _attacker != null ? _attacker.Target as Monster : null;
            var pet = target != null ? Pets.PetCatalog.ForTaming(item.Id, target.Definition.Id) : null;
            if (pet == null)
            {
                ChatLog.Error($"Use the {item.Name} while you fight the creature it tames.");
                return false;
            }

            float chance = Pets.PetCatalog.TameChance(pet, target.Hp / (float)Mathf.Max(1, target.MaxHp), Stats.Total[StatType.Luk]);
            if (!SystemRandomSource.Shared.Chance(chance))
            {
                ChatLog.Notice($"The {target.Definition.Name} shrugs off the {item.Name}. ({chance:0.#}% chance)");
                return true;
            }

            target.Despawn();
            Inventory.Add(pet.EggItemId, 1);
            GameAudio.Play(Sfx.Heal);
            ChatLog.Loot($"You tamed the {target.Definition.Name}! Its egg is in your bag.");
            return true;
        }

        public void FeedPet()
        {
            if (Pets.PetCatalog.Feed(Record, Inventory, out string message))
            {
                ChatLog.Notice(message);
                Recalculate(); // Loyal brings the pet's bonus
                GameAudio.Play(Sfx.Pickup);
            }
            else
            {
                ChatLog.Error(message);
            }
        }

        public void ReturnPet()
        {
            if (Pets.PetCatalog.ReturnToEgg(Record, Inventory, out string message))
            {
                ChatLog.Notice(message);
                RefreshPet();
            }
            else
            {
                ChatLog.Error(message);
            }
        }

        // ------------------------------------------------------------ mercenaries (Phase 8)
        private Monster _mercenary;

        /// <summary>The mercenary fighting at this character's side, or null.</summary>
        public Monster Mercenary => _mercenary;

        /// <summary>Brings the hired mercenary in (after a hire, a map change or a respawn), wounds and kills as they were.</summary>
        public void RefreshMercenary()
        {
            var contract = Mercenaries.MercenaryCatalog.Active(Record);
            var definition = contract != null ? Mercenaries.MercenaryCatalog.Get(contract.MercenaryId)?.Monster : null;
            if (_mercenary != null && (definition == null || _mercenary.Definition != definition || _mercenary.IsDead))
            {
                Destroy(_mercenary.gameObject);
                _mercenary = null;
            }

            if (_mercenary != null || definition == null || IsDead)
            {
                return;
            }

            var spot = Position - transform.forward * 1.6f - transform.right * 1.2f;
            if (!UnityEngine.AI.NavMesh.SamplePosition(spot, out var hit, 4f, UnityEngine.AI.NavMesh.AllAreas)
                && !UnityEngine.AI.NavMesh.SamplePosition(Position, out hit, 4f, UnityEngine.AI.NavMesh.AllAreas))
            {
                return;
            }

            _mercenary = EntityFactory.CreateMercenary(definition, hit.position, this);
            _mercenary.Kills = contract.Kills;
            if (contract.Hp > 0)
            {
                _mercenary.RestoreHp(contract.Hp);
            }
        }

        /// <summary>Ends the contract early (the mercenary bows out).</summary>
        public void DismissMercenary()
        {
            var contract = Mercenaries.MercenaryCatalog.Active(Record);
            if (contract == null)
            {
                ChatLog.Error("You have no mercenary.");
                return;
            }

            ChatLog.Notice($"{Mercenaries.MercenaryCatalog.Get(contract.MercenaryId).Name} bows and leaves your service.");
            Mercenaries.MercenaryCatalog.End(Record);
            RefreshMercenary();
        }

        /// <summary>
        /// The contract's clock runs while you play. While you're down, your mercenary waits out of sight (it comes back when
        /// you do); its wounds and kills are written to the contract so a map change keeps them.
        /// </summary>
        private void TickMercenary()
        {
            var contract = Mercenaries.MercenaryCatalog.Active(Record);
            if (contract == null)
            {
                if (_mercenary != null)
                {
                    RefreshMercenary();
                }

                return;
            }

            string name = Mercenaries.MercenaryCatalog.Get(contract.MercenaryId).Name;
            if (Mercenaries.MercenaryCatalog.Tick(Record, Time.deltaTime))
            {
                ChatLog.Notice($"{name}'s contract has run out. They salute you and go.");
                RefreshMercenary();
                return;
            }

            if (_mercenary != null && !_mercenary.IsDead)
            {
                contract.Hp = _mercenary.Hp;
                contract.Kills = _mercenary.Kills;
            }

            if (IsDead && _mercenary != null)
            {
                Destroy(_mercenary.gameObject);
                _mercenary = null;
            }
            else if (!IsDead && _mercenary == null && Time.frameCount % 30 == 0)
            {
                RefreshMercenary();
            }
        }

        private void OnMercenaryFell(Monster mercenary)
        {
            if (mercenary != null && mercenary == _homunculus)
            {
                OnHomunculusFell();
                return;
            }

            if (mercenary == null || mercenary != _mercenary)
            {
                return;
            }

            ChatLog.Error($"{mercenary.DisplayName} has fallen. The contract ends.");
            Mercenaries.MercenaryCatalog.End(Record);
            _mercenary = null;
        }

        // ------------------------------------------------------------ Odin's ravens (Phase 8)
        private RavenCompanion _raven;

        /// <summary>Huginn on a Ranger's shoulder: he swoops for Raven Strike and Raven Assault.</summary>
        public RavenCompanion Raven => _raven;

        /// <summary>Ragnarok's auto Blitz Beat: on each auto-attack, a LUK ÷ 3 % chance that Huginn strikes on his own.</summary>
        private void TryHuginnStrike(CombatEntity target)
        {
            if (_raven == null || target == null || target.IsDead || _caster == null)
            {
                return;
            }

            int level = SkillBook.UsableLevel("raven_strike");
            float chance = Stats.Total[StatType.Luk] / 3f;
            if (level <= 0 || UnityEngine.Random.value * 100f >= chance)
            {
                return;
            }

            _raven.Swoop(target.Position);
            _caster.CastProc(SkillCatalog.Get("raven_strike"), level, target);
        }

        // ------------------------------------------------------------ mounts (Phase 8)
        /// <summary>What this character is riding, or null.</summary>
        public string Mount => Record?.Mount;

        /// <summary>Horsemanship / Boar Riding: calls the line's mount, or gets down.</summary>
        public void ToggleClassMount()
        {
            var (mount, ride, _) = Characters.MountRules.ClassMount(Record.Job);
            if (mount == null || SkillBook.UsableLevel(ride) <= 0)
            {
                ChatLog.Error("Your guild doesn't teach riding.");
                return;
            }

            ToggleMount(mount);
        }

        /// <summary>Mounts <paramref name="mount"/>, or gets down when already riding it (or anything).</summary>
        public void ToggleMount(string mount)
        {
            if (IsDead)
            {
                return;
            }

            if (!string.IsNullOrEmpty(Record.Mount))
            {
                string old = Record.Mount;
                Record.Mount = null;
                ApplyMount(rebuild: true);
                ChatLog.Notice($"You get down from your {Characters.MountRules.NameOf(old)}.");
                if (old == mount)
                {
                    return;
                }
            }

            Record.Mount = mount;
            ApplyMount(rebuild: true);
            GameAudio.Play(Sfx.Buff, Position, 0.5f);
            ChatLog.Notice($"You ride {(mount == Characters.MountRules.Horse ? "your fjord horse" : Characters.MountRules.NameOf(mount))}.");
        }

        /// <summary>Off the fjord horse before a fight (it isn't trained for battle).</summary>
        public void GetDownToFight()
        {
            if (Characters.MountRules.BlocksCombat(Record?.Mount))
            {
                Record.Mount = null;
                ApplyMount(rebuild: true);
                ChatLog.Notice("You get down from your fjord horse to fight.");
            }
        }

        /// <summary>The riding buff (its level is the cavalry mastery + 1) and the mount drawn under the rider.</summary>
        private void ApplyMount(bool rebuild)
        {
            foreach (string id in new[] { SkillBuffs.MountHorse, SkillBuffs.MountWarhorse, SkillBuffs.MountBoar })
            {
                if (id != Characters.MountRules.BuffFor(Record.Mount))
                {
                    Buffs.Remove(id);
                }
            }

            var buff = BuffCatalog.Get(Characters.MountRules.BuffFor(Record.Mount));
            if (buff != null && Buffs.StacksOf(buff.Id) == 0)
            {
                var (_, _, mastery) = Characters.MountRules.ClassMount(Record.Job);
                int level = 1 + (mastery != null ? Mathf.Min(5, SkillBook.UsableLevel(mastery)) : 0);
                ApplyBuff(buff, level, 1e7f, 0, 0);
            }

            if (rebuild)
            {
                RebuildAvatar();
            }
        }

        private void TickMount()
        {
            // Death clears buffs; a rider who comes back is still mounted.
            if (!IsDead && !string.IsNullOrEmpty(Record.Mount) && Buffs.StacksOf(Characters.MountRules.BuffFor(Record.Mount)) == 0
                && Time.frameCount % 20 == 5)
            {
                ApplyMount(rebuild: false);
            }
        }

        // ------------------------------------------------------------ homunculi (Phase 8)
        private Monster _homunculus;

        /// <summary>The homunculus at this character's side, or null.</summary>
        public Monster HomunculusBody => _homunculus;

        public bool CanWeaveOrCallHomunculus(out string reason)
        {
            var state = Homunculi.HomunculusRules.Of(Record);
            if (state == null && !Inventory.Has(Homunculi.HomunculusRules.EmbryoItemId))
            {
                reason = "Weave Life needs an Embryo: brew one from a Seed of Life, Morning Dew of Yggdrasil, a Glass Tube and a Medicine Bowl.";
                return false;
            }

            if (state != null && state.RestSeconds > 0f)
            {
                reason = $"{state.Name} is still recovering ({Mathf.CeilToInt(state.RestSeconds)} s). A Lifeweaver's Breath of Iðunn wakes it at once.";
                return false;
            }

            reason = null;
            return true;
        }

        /// <summary>Weave Life: weaves a homunculus from an Embryo the first time, then calls it out or sends it to rest.</summary>
        public void WeaveOrCallHomunculus()
        {
            if (!CanWeaveOrCallHomunculus(out string reason))
            {
                ChatLog.Error(reason);
                return;
            }

            var state = Homunculi.HomunculusRules.Of(Record);
            if (state == null)
            {
                Inventory.TryRemove(Homunculi.HomunculusRules.EmbryoItemId);
                state = Homunculi.HomunculusRules.Weave(Record, UnityEngine.Random.Range(0, 4000));
                var kind = Homunculi.HomunculusRules.Get(state.Kind);
                ChatLog.Loot($"The Embryo quickens: {kind.Name} {kind.Title} is born! {kind.Description}");
                GameAudio.Play(Sfx.LevelUp);
            }
            else
            {
                state.Out = !state.Out;
                ChatLog.Notice(state.Out ? $"{state.Name} comes to your side." : $"{state.Name} goes to rest.");
            }

            RefreshHomunculus();
        }

        /// <summary>Breath of Iðunn: the fallen homunculus wakes now with <paramref name="hpPercent"/>% of its HP.</summary>
        public void ReviveHomunculus(float hpPercent)
        {
            var state = Homunculi.HomunculusRules.Of(Record);
            if (state == null || state.RestSeconds <= 0f)
            {
                return;
            }

            state.RestSeconds = 0f;
            state.Out = true;
            var definition = Homunculi.HomunculusRules.Build(state);
            state.Hp = Mathf.Max(1, Mathf.RoundToInt(definition.MaxHp * Mathf.Clamp(hpPercent, 1f, 100f) / 100f));
            ChatLog.Notice($"{state.Name} stirs and rises.");
            RefreshHomunculus();
        }

        /// <summary>Shows the homunculus when it's out and able (after a map change, a respawn, a level up).</summary>
        public void RefreshHomunculus()
        {
            var state = Homunculi.HomunculusRules.Of(Record);
            bool wanted = state != null && state.Out && state.RestSeconds <= 0f && !IsDead;
            if (_homunculus != null && (!wanted || _homunculus.IsDead || _homunculus.Level != state.Level))
            {
                Destroy(_homunculus.gameObject);
                _homunculus = null;
            }

            if (!wanted || _homunculus != null)
            {
                return;
            }

            var spot = Position - transform.forward * 1.4f + transform.right * 1.3f;
            if (!UnityEngine.AI.NavMesh.SamplePosition(spot, out var hit, 4f, UnityEngine.AI.NavMesh.AllAreas)
                && !UnityEngine.AI.NavMesh.SamplePosition(Position, out hit, 4f, UnityEngine.AI.NavMesh.AllAreas))
            {
                return;
            }

            _homunculus = EntityFactory.CreateMercenary(Homunculi.HomunculusRules.Build(state), hit.position, this);
            _homunculus.name = $"Homunculus [{state.Name}]";
            if (state.Hp > 0)
            {
                _homunculus.RestoreHp(state.Hp);
            }
        }

        private void TickHomunculus()
        {
            var state = Homunculi.HomunculusRules.Of(Record);
            if (state == null)
            {
                return;
            }

            if (state.RestSeconds > 0f)
            {
                state.RestSeconds = Mathf.Max(0f, state.RestSeconds - Time.deltaTime);
                if (state.RestSeconds <= 0f)
                {
                    ChatLog.Notice($"{state.Name} has recovered. Use Weave Life to call it.");
                    state.Out = false;
                }

                return;
            }

            if (_homunculus != null && !_homunculus.IsDead)
            {
                state.Hp = _homunculus.Hp;
            }

            if (IsDead && _homunculus != null)
            {
                Destroy(_homunculus.gameObject);
                _homunculus = null;
            }
            else if (!IsDead && state.Out && _homunculus == null && Time.frameCount % 30 == 15)
            {
                RefreshHomunculus();
            }
        }

        private void OnHomunculusFell()
        {
            var state = Homunculi.HomunculusRules.Of(Record);
            _homunculus = null;
            if (state == null)
            {
                return;
            }

            state.Hp = 0;
            state.RestSeconds = Homunculi.HomunculusRules.RestAfterFalling;
            ChatLog.Error($"{state.Name} has fallen. It will recover in {Homunculi.HomunculusRules.RestAfterFalling / 60f:0} minutes.");
        }

        /// <summary>The homunculus out at your side takes a share of the base EXP you earn, and grows.</summary>
        private void ShareWithHomunculus(long baseExp)
        {
            var state = Homunculi.HomunculusRules.Of(Record);
            if (state == null || _homunculus == null || _homunculus.IsDead || baseExp <= 0)
            {
                return;
            }

            int gained = Homunculi.HomunculusRules.GainExp(state, Math.Max(1L, (long)(baseExp * Homunculi.HomunculusRules.ExpShare)));
            if (gained <= 0)
            {
                return;
            }

            ChatLog.Loot($"{state.Name} grows to level {state.Level}!");
            state.Hp = 0; // a level up heals it fully
            RefreshHomunculus();
        }

        public void TakeOffPetArmor()
        {
            if (Pets.PetCatalog.TakeOffArmor(Record, Inventory, out string message))
            {
                ChatLog.Notice(message);
                RefreshPet();
            }
            else
            {
                ChatLog.Error(message);
            }
        }

        /// <summary>Shows the active pet (or removes the companion when none is out) and refreshes the Loyal bonus.</summary>
        public void RefreshPet()
        {
            var state = Pets.PetCatalog.Active(Record);
            _pet = PetCompanion.Sync(_pet, transform, state?.EggItemId, state?.ArmorItemId, state?.Name);
            Recalculate();
        }

        private void TickPet()
        {
            if (Pets.PetCatalog.Active(Record) == null)
            {
                return;
            }

            _petHungerTimer += Time.deltaTime;
            if (_petHungerTimer < Pets.PetCatalog.HungerTickSeconds)
            {
                return;
            }

            _petHungerTimer = 0f;
            bool ranAway = Pets.PetCatalog.Starve(Record, out string message);
            if (!string.IsNullOrEmpty(message))
            {
                ChatLog.Error(message);
            }

            if (ranAway)
            {
                RefreshPet();
            }
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
            GameAudio.Play(Sfx.Revive);
            if (_animation != null)
            {
                _animation.SetDead(false);
            }

            _motor.SetLocked(false);
            WorldTravel.ToSavePoint(this, "You have been revived at your save point.");
        }

        /// <summary>Called back from Hel by a Gothi: up where you fell, no trip to the save point.</summary>
        protected override void OnResurrected(float hpPercent)
        {
            Revive(Mathf.Max(1, Mathf.RoundToInt(MaxHp * hpPercent / 100f)), Sp);
            if (_animation != null)
            {
                _animation.SetDead(false);
            }

            _motor.SetLocked(false);
            GroundRing.SpawnPulse(transform.position, new Color(1f, 0.97f, 0.8f, 1f), 0.2f, 2f, 0.7f);
            ChatLog.System("You were called back from Hel's road.");
        }

        protected override void OnDamaged(DamageResult result, CombatEntity attacker)
        {
            if (result.Amount <= 0 || result.IsDamageOverTime)
            {
                return;
            }

            GameAudio.Play(Sfx.Hit, Position, 0.45f, 0.85f);

            // Rage of Thor hyper-armor: no flinch (casting is already uninterruptible through the same buff).
            if (_animation != null && !Stats.HyperArmor)
            {
                _animation.PlayHit();
            }

            _caster.InterruptByDamage();
            if (attacker != null && !attacker.IsDead && attacker != this)
            {
                TriggerAutoCasts(AutoCastTrigger.WhenHit, attacker, null);
            }
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
                GameAudio.Play(Sfx.Stagger, Position, 0.8f);
                WorldFeedback.Announce(this, "Staggered!", new Color(1f, 0.6f, 0.4f));
                if (_animation != null)
                {
                    _animation.PlayStagger();
                }
            }
        }

        protected override void OnDied(CombatEntity killer)
        {
            GameAudio.Play(Sfx.Death);

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

            // Saved map-local: online realms place maps at their own spot in the world (FieldContext.Origin).
            Vector3 position = (IsDead ? FieldContext.SavePoint : transform.position) - FieldContext.Origin;
            Record.PosX = position.x;
            Record.PosY = position.y;
            Record.PosZ = position.z;
        }

        // ------------------------------------------------------------ level-ups
        private void OnBaseLevelUp(int level)
        {
            GameAudio.Play(Sfx.LevelUp);
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
            GameAudio.Play(Sfx.JobLevelUp);
            WorldFeedback.Announce(this, "Job Level Up!", new Color(0.45f, 0.9f, 1f));
            ChatLog.Notice($"{DisplayName} reached Job Level {level}.");
        }

        private void OnJobChanged()
        {
            ChatLog.Notice($"{DisplayName} is now {JobDatabase.WithArticle(JobDatabase.NameFor(Record))}!");

            // Gear the new job can't use goes to the bag; the guild hands over the job's own weapon.
            int removed = Equipment.RemoveUnwearable();
            if (removed > 0)
            {
                ChatLog.System($"{removed} piece(s) of gear went back to your bag: {JobDatabase.WithArticle(Job.Name)} can't use them.");
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

        /// <summary>Loki's Mimicry: an ally nearby used a skill this Outlaw can copy.</summary>
        private void OnAnySkillUsed(CombatEntity caster, SkillDefinition skill, int level)
        {
            if (caster == this || !(caster is PlayerEntity) || caster.Faction != Faction || IsDead || Record == null
                || HorizontalDistance(caster.Position, Position) > MimicryRules.CopyRange)
            {
                return;
            }

            if (MimicryRules.TryCopy(Record, skill, level, Buffs.Has(SkillBuffs.PreserveCopy), out int copied))
            {
                ChatLog.Notice($"Loki's Mimicry copies {skill.Name} (Lv {copied}) from {caster.DisplayName}. Find it in your Skill window.");
                SkillBook.NotifyChanged();
            }
        }

        /// <summary>
        /// The Norns' rebirth (Phase 7): back to High Initiate at Base and Job Lv 1 with 100 status points, skills cleared, gear in
        /// the bag. Saves at once.
        /// </summary>
        public bool TryRebirth(out string reason)
        {
            if (IsDead)
            {
                reason = "The dead can't be reborn.";
                return false;
            }

            if (!Progression.TryRebirth(out reason))
            {
                return false;
            }

            // The rules emptied the record's skills and gear directly; the job change above already recalculated and saved.
            SkillBook.NotifyChanged();
            Equipment.NotifyChanged();
            Inventory.NotifyChanged();
            Recalculate();
            SetVitals(MaxHp, MaxHp, MaxSp, MaxSp);
            if (_animation != null)
            {
                _animation.PlaySkill(SkillMotion.Buff, 1f, 0.8f);
            }

            GroundRing.SpawnPulse(transform.position, new Color(0.85f, 0.75f, 1f, 1f), 0.3f, 4f, 1.2f);
            ChatLog.Notice($"The Norns weave {DisplayName} a new thread. Walk it again, {JobDatabase.NameFor(Record)}.");
            SaveNow();
            return true;
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
            if (Record != null)
            {
                TickMercenary();
                TickHomunculus();
                TickMount();
            }

            if (Record == null || IsDead)
            {
                return;
            }

            TickPet();
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
            SkillCaster.AnySkillUsed -= OnAnySkillUsed;
            Monster.MercenaryFell -= OnMercenaryFell;
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
