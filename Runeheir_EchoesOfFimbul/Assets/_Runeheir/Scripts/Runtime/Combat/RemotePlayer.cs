using System;
using Runeheir.Items;
using Runeheir.Jobs;
using Runeheir.Movement;
using Runeheir.Online;
using Runeheir.Skills;
using Runeheir.Stats;
using Runeheir.Visuals;
using UnityEngine;

namespace Runeheir.Combat
{
    /// <summary>
    /// Online play (Phase 6): another player's character as this game sees it, or any player's character on the realm
    /// server. It moves, swings and shows its outfit as their game reports; hits, heals and buffs aimed at it, and
    /// monster rewards, go to their game through <see cref="IRemotePlayerLink"/>.
    /// </summary>
    [RequireComponent(typeof(NavMotor))]
    public sealed class RemotePlayer : PlayerEntity
    {
        private CharacterAnimationBridge _animation;
        private PlaceholderAvatar _avatar;
        private DefenderProfile _defender = new DefenderProfile { BluntDamageTakenMultiplier = 1f, Race = Race.DemiHuman, Size = Size.Medium };
        private string _name = "Adventurer";
        private int _level = 1;
        private string _lookCode;
        private float _attackRange = 1f;
        private bool _ranged;
        private UI.WorldLabel _stallLabel;

        /// <summary>Someone clicked another player (the HUD opens the player menu).</summary>
        public static event Action<RemotePlayer> Clicked;

        public IRemotePlayerLink Link => Remote as IRemotePlayerLink;

        public JobId Job { get; private set; }

        /// <summary>Their street stall's title, or empty when they aren't vending.</summary>
        public string StallTitle { get; private set; } = string.Empty;

        public bool IsVending => !string.IsNullOrEmpty(StallTitle);

        public string GuildName { get; private set; } = string.Empty;

        public override string DisplayName => _name;

        public override int Level => _level;

        public override float AttackRange => _attackRange;

        public override float AttackInterval => 1f;

        public override bool IsRangedAttacker => _ranged;

        /// <summary>Connects the mirror to the network (movement then comes from it too).</summary>
        public void Bind(IRemotePlayerLink link)
        {
            Remote = link;
            GetComponent<NavMotor>().MakePuppet();
        }

        public void MirrorIdentity(string characterName, int level, JobId job, string guildName, string stallTitle)
        {
            if (!string.IsNullOrEmpty(characterName) && characterName != _name)
            {
                _name = characterName;
                gameObject.name = $"Player [{characterName}]";
            }

            _level = Mathf.Max(1, level);
            Job = job;
            GuildName = guildName ?? string.Empty;
            stallTitle = stallTitle ?? string.Empty;
            if (stallTitle != StallTitle)
            {
                StallTitle = stallTitle;
                ShowStall();
            }
        }

        public void MirrorLook(string code)
        {
            if (code == _lookCode)
            {
                return;
            }

            _lookCode = code;
            var look = AvatarLook.FromCode(code);
            _ranged = WeaponRules.IsRanged(look.Weapon);
            _attackRange = WeaponRules.AttackRange(look.Weapon);
            if (_avatar == null)
            {
                _avatar = GetComponentInChildren<PlaceholderAvatar>();
            }

            if (_avatar != null)
            {
                _avatar.RebuildHumanoid(look);
                _animation?.RefreshHidden();
            }
        }

        /// <summary>Their defences, as their game computed them (monsters on the realm use these).</summary>
        public void MirrorDefences(CombatSnapshot snapshot)
        {
            if (snapshot != null)
            {
                _defender = snapshot.ToDefender();
            }
        }

        /// <summary>Their vitals and whether they're down.</summary>
        public void MirrorState(int hp, int maxHp, int sp, int maxSp, bool dead)
        {
            if (dead)
            {
                MirrorVitals(0, maxHp, sp, maxSp);
                MirrorDeath(null);
                return;
            }

            if (IsDead)
            {
                MirrorRevive(Mathf.Max(1, hp), sp);
                _animation?.SetDead(false);
            }

            MirrorVitals(hp, maxHp, sp, maxSp);
        }

        /// <summary>They swung at something (the animation, and an arrow for bows).</summary>
        public void MirrorSwing(CombatEntity target, float playRate, float swingSeconds)
        {
            if (target != null)
            {
                GetComponent<NavMotor>().FaceTowards(target.Position, instant: true);
            }

            _animation?.PlayAttack(Mathf.Max(0.1f, playRate), Mathf.Max(0.05f, swingSeconds));
            if (_ranged && target != null)
            {
                ProjectileFx.Launch(this, target, ProjectileFx.ArrowColor, ProjectileFx.ArrowSpeed, null, arrow: true);
            }
        }

        public void MirrorSkillMotion(SkillMotion motion)
        {
            _animation?.PlaySkill(motion, 1f, 0.45f);
        }

        public void MirrorCasting(bool casting)
        {
            _animation?.SetCasting(casting);
        }

        public override AttackerProfile BuildAttackerProfile()
        {
            // Never attacks from here: their own game swings for them.
            return new AttackerProfile { Weapon = WeaponType.Unarmed, Race = Race.DemiHuman };
        }

        public override DefenderProfile BuildDefenderProfile()
        {
            var profile = _defender;
            profile.BluntDamageTakenMultiplier = Mathf.Max(profile.BluntDamageTakenMultiplier, BluntDamageTakenMultiplier);
            return profile;
        }

        public override void GrantExperience(long baseExp, long jobExp)
        {
            Link?.RelayExperience(baseExp, jobExp);
        }

        public override bool TryBreakWeapon(float chancePercent)
        {
            Link?.RelayBreakWeapon(chancePercent);
            return false;
        }

        public override void ReceiveLoot(ItemDefinition item, float baseChance, bool mvpReward)
        {
            if (item != null)
            {
                Link?.RelayLoot(item, baseChance, mvpReward);
            }
        }

        public override void ReceiveMvp(string monsterName, long bonusExp)
        {
            Link?.RelayMvp(monsterName, bonusExp);
        }

        /// <summary>A vending player's shop sign floats over their head (Ragnarok's stall bubble).</summary>
        private void ShowStall()
        {
            if (!IsVending)
            {
                if (_stallLabel != null)
                {
                    Destroy(_stallLabel);
                    _stallLabel = null;
                }

                return;
            }

            string text = $"<b>[Shop]</b> {StallTitle}";
            if (_stallLabel == null)
            {
                _stallLabel = UI.WorldLabel.Attach(gameObject, text, new Color(1f, 0.85f, 0.4f), 2.7f, 14);
            }
            else
            {
                _stallLabel.Text = text;
            }
        }

        /// <summary>The HUD calls this when the local player clicks this character.</summary>
        public void NotifyClicked()
        {
            Clicked?.Invoke(this);
        }

        protected override void OnMirroredHit(DamageResult result, CombatEntity attacker)
        {
            if (result.Amount > 0 && !result.IsDamageOverTime)
            {
                _animation?.PlayHit();
            }
        }

        protected override void OnStatusApplied(StatusEffect effect)
        {
            if (effect == StatusEffect.Stagger)
            {
                _animation?.PlayStagger();
            }
        }

        protected override void OnDied(CombatEntity killer)
        {
            _animation?.SetDead(true);
        }

        private void Awake()
        {
            _animation = GetComponent<CharacterAnimationBridge>();
            bodyRadius = 0.4f;
            bodyHeight = 1.8f;
        }

        protected override void Update()
        {
            base.Update();
            if (_animation != null)
            {
                _animation.SetHidden(IsHidden);
            }
        }
    }
}
