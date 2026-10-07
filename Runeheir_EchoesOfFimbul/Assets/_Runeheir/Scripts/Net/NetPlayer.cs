using Mirror;
using Runeheir.Combat;
using Runeheir.Field;
using Runeheir.Items;
using Runeheir.Jobs;
using Runeheir.Online;
using Runeheir.Player;
using Runeheir.Session;
using Runeheir.Skills;
using Runeheir.Social;
using Runeheir.Visuals;
using UnityEngine;

namespace Runeheir.Net
{
    /// <summary>
    /// A player's character on the network. The owner's game runs the character (stats, items, skills, movement) and
    /// reports its state here; the realm and other players see a <see cref="RemotePlayer"/> mirror driven by it. Hits,
    /// heals, buffs and rewards aimed at a mirror travel back to the owner through this component.
    /// </summary>
    public sealed class NetPlayer : NetworkBehaviour, IRemotePlayerLink
    {
        // Owner → realm → everyone (syncDirection is ClientToServer, set where the object is built).
        [SyncVar] private string _name = string.Empty;
        [SyncVar] private int _level = 1;
        [SyncVar] private int _job;
        [SyncVar] private string _look = string.Empty;
        [SyncVar] private int _hp = 1;
        [SyncVar] private int _maxHp = 1;
        [SyncVar] private int _sp;
        [SyncVar] private int _maxSp = 1;
        [SyncVar] private bool _dead;
        [SyncVar] private bool _casting;
        [SyncVar] private string _auras = string.Empty;
        [SyncVar] private string _stall = string.Empty;
        [SyncVar] private string _guild = string.Empty;

        private PlayerCharacter _player;
        private RemotePlayer _remote;
        private AutoAttacker _attacker;
        private CharacterAnimationBridge _animation;
        private SkillCaster _caster;
        private bool _subscribed;
        private string _appliedAuras;
        private bool _appliedCasting;
        private string _auraSignature;
        private string _sentDefences;
        private float _nextAuraCheck;
        private float _nextLookCheck;
        private float _nextDefenceCheck;

        /// <summary>The realm's session for this player (realm side only).</summary>
        internal RealmSession Session { get; set; }

        public string CharacterName => _name;

        public int Level => _level;

        public JobId Job => (JobId)_job;

        public int Hp => _hp;

        public int MaxHp => _maxHp;

        public bool IsDown => _dead;

        public string StallTitle => _stall;

        /// <summary>The character as this machine sees it: our own full one, or a mirror.</summary>
        public PlayerEntity Entity => _player != null ? (PlayerEntity)_player : _remote;

        private void Awake()
        {
            _player = GetComponent<PlayerCharacter>();
            _remote = GetComponent<RemotePlayer>();
            _attacker = GetComponent<AutoAttacker>();
            _animation = GetComponent<CharacterAnimationBridge>();
        }

        /// <summary>Realm side, before the spawn: what everyone sees until the owner's first report.</summary>
        internal void ServerSeed(Characters.CharacterRecord record)
        {
            _name = record.Name;
            _level = record.BaseLevel;
            _job = (int)record.Job;
            _look = AvatarLook.Code(record);
            _remote?.MirrorIdentity(record.Name, record.BaseLevel, record.Job, string.Empty, string.Empty);
            _remote?.MirrorLook(_look);
        }

        public override void OnStartServer()
        {
            BindMirror();
        }

        public override void OnStartClient()
        {
            BindMirror();
        }

        public override void OnStartLocalPlayer()
        {
            if (_player == null)
            {
                return;
            }

            Subscribe();
            ReportState(force: true);
            FieldBootstrap.Active?.AttachPlayer(_player);
        }

        public override void OnStopLocalPlayer()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void BindMirror()
        {
            if (_remote != null && _remote.Remote == null)
            {
                _remote.Bind(this);
                ApplyMirror();
            }
        }

        private void Update()
        {
            if (_player != null && isLocalPlayer)
            {
                ReportState(force: false);
            }
            else if (_remote != null)
            {
                ApplyMirror();
            }
        }

        // ------------------------------------------------------------ owner: report our character
        private void ReportState(bool force)
        {
            var record = _player.Record;
            if (record == null)
            {
                return;
            }

            _name = record.Name;
            _level = record.BaseLevel;
            _job = (int)record.Job;
            _hp = _player.Hp;
            _maxHp = _player.MaxHp;
            _sp = _player.Sp;
            _maxSp = _player.MaxSp;
            _dead = _player.IsDead;

            var social = OnlineSession.Current?.Social?.State;
            _stall = social?.MyStall != null ? social.MyStall.Title ?? string.Empty : string.Empty;
            _guild = social?.Guild != null ? social.Guild.Name ?? string.Empty : string.Empty;

            float now = Time.unscaledTime;
            if (force || now >= _nextLookCheck)
            {
                _nextLookCheck = now + 0.5f;
                _look = AvatarLook.Code(record);
            }

            if (force || now >= _nextAuraCheck)
            {
                _nextAuraCheck = now + 0.25f;
                string signature = AuraSignature(_player);
                if (signature != _auraSignature)
                {
                    _auraSignature = signature;
                    _auras = AuraCodec.Encode(_player.Buffs, _player.Statuses, Time.timeAsDouble);
                }
            }

            if ((force || now >= _nextDefenceCheck) && _player.Stats != null)
            {
                _nextDefenceCheck = now + 0.5f;
                string defences = JsonUtility.ToJson(CombatSnapshot.From(_player.BuildDefenderProfile()));
                if (defences != _sentDefences)
                {
                    _sentDefences = defences;
                    CmdDefences(defences);
                }
            }
        }

        /// <summary>Changes when a buff or status is added, refreshed or removed (not as time ticks down).</summary>
        internal static string AuraSignature(CombatEntity entity)
        {
            var builder = new System.Text.StringBuilder();
            foreach (var status in entity.Statuses.Active)
            {
                builder.Append((int)status.Status).Append('@').Append(status.ExpiresAt.ToString("0.0")).Append(';');
            }

            foreach (var buff in entity.Buffs.Active)
            {
                builder.Append(buff.Definition?.Id).Append('@').Append(buff.ExpiresAt.ToString("0.0")).Append('x').Append(buff.Stacks)
                    .Append('l').Append(buff.Level).Append(';');
            }

            return builder.ToString();
        }

        private void Subscribe()
        {
            if (_subscribed)
            {
                return;
            }

            _subscribed = true;
            if (_attacker != null)
            {
                _attacker.Swung += OnSwung;
            }

            if (_animation != null)
            {
                _animation.SkillMotionPlayed += OnSkillMotion;
                _animation.CastingChanged += OnCastingChanged;
            }

            _caster = GetComponent<SkillCaster>();
            if (_caster != null)
            {
                _caster.SkillUsed += OnSkillUsed;
            }

            _player.Damaged += OnDamaged;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _subscribed = false;
            if (_attacker != null)
            {
                _attacker.Swung -= OnSwung;
            }

            if (_animation != null)
            {
                _animation.SkillMotionPlayed -= OnSkillMotion;
                _animation.CastingChanged -= OnCastingChanged;
            }

            if (_caster != null)
            {
                _caster.SkillUsed -= OnSkillUsed;
            }

            if (_player != null)
            {
                _player.Damaged -= OnDamaged;
            }
        }

        private void OnSwung(CombatEntity target)
        {
            if (NetworkClient.ready)
            {
                CmdSwing(NetWire.IdOf(target), _player.AttackPlayRate, _player.SwingDuration);
            }
        }

        private void OnSkillMotion(SkillMotion motion)
        {
            if (NetworkClient.ready)
            {
                CmdSkillMotion((byte)motion);
            }
        }

        private void OnSkillUsed(SkillDefinition skill, int level)
        {
            if (NetworkClient.ready && skill != null)
            {
                CmdSkillUsed(skill.Id, (byte)Mathf.Clamp(level, 1, 255));
            }
        }

        private void OnCastingChanged(bool casting)
        {
            _casting = casting;
        }

        private void OnDamaged(DamageResult result, CombatEntity attacker)
        {
            if (NetworkClient.ready && !result.IsDamageOverTime)
            {
                CmdShowHit(result.Amount, NetWire.Flags(result), NetWire.IdOf(attacker));
            }
        }

        // ------------------------------------------------------------ mirrors: show someone else's character
        private void ApplyMirror()
        {
            if (_remote == null)
            {
                return;
            }

            _remote.MirrorIdentity(_name, _level, (JobId)_job, _guild, _stall);
            _remote.MirrorLook(_look);
            _remote.MirrorState(_hp, _maxHp, _sp, _maxSp, _dead);
            if (_auras != _appliedAuras)
            {
                _appliedAuras = _auras;
                AuraCodec.Apply(_auras, _remote.Buffs, _remote.Statuses, Time.timeAsDouble);
            }

            if (_casting != _appliedCasting)
            {
                _appliedCasting = _casting;
                _remote.MirrorCasting(_casting);
            }
        }

        // ------------------------------------------------------------ owner → realm
        [Command]
        private void CmdDefences(string json)
        {
            if (_remote != null && json != null && json.Length < 4096)
            {
                _remote.MirrorDefences(Json.Read<CombatSnapshot>(json));
            }
        }

        [Command]
        private void CmdSwing(uint target, float playRate, float swingSeconds)
        {
            RpcSwing(target, NetWire.Clamp(playRate, 0.1f, 5f), NetWire.Clamp(swingSeconds, 0.05f, 3f));
        }

        [Command]
        private void CmdSkillMotion(byte motion)
        {
            RpcSkillMotion(motion);
        }

        [Command]
        private void CmdSkillUsed(string skillId, byte level)
        {
            // Only real skills go out (Loki's Mimicry on other machines copies from it).
            if (SkillCatalog.Get(skillId) != null && level > 0)
            {
                RpcSkillUsed(skillId, level);
            }
        }

        [Command]
        private void CmdShowHit(int amount, byte flags, uint attacker)
        {
            RpcShowHit(amount, flags, attacker);
        }

        [ClientRpc(includeOwner = false)]
        private void RpcSwing(uint target, float playRate, float swingSeconds)
        {
            _remote?.MirrorSwing(NetWire.Entity(target), playRate, swingSeconds);
        }

        [ClientRpc(includeOwner = false)]
        private void RpcSkillUsed(string skillId, byte level)
        {
            SkillCaster.RaiseSkillUsed(_remote, SkillCatalog.Get(skillId), level);
        }

        [ClientRpc(includeOwner = false)]
        private void RpcSkillMotion(byte motion)
        {
            _remote?.MirrorSkillMotion((SkillMotion)motion);
        }

        [ClientRpc(includeOwner = false)]
        private void RpcShowHit(int amount, byte flags, uint attacker)
        {
            _remote?.MirrorHit(NetWire.Result(amount, flags, 1f), NetWire.Entity(attacker));
        }

        // ------------------------------------------------------------ IRemotePlayerLink
        // On the realm, this object mirrors a remote player: everything goes to their game (TargetRpc).
        // On another player's game, it mirrors a third player: friendly effects go to the realm first (Command).

        public void RelayDamage(DamageResult result, CombatEntity attacker, bool physicalMelee, float poiseDamage)
        {
            if (isServer)
            {
                TargetHit(result.Amount, NetWire.Flags(result), result.ElementMultiplier, physicalMelee, poiseDamage, NetWire.IdOf(attacker));
            }
        }

        public void RelayStatus(StatusEffect status, float chancePercent, float seconds, bool roll, bool ignoreImmunity)
        {
            if (isServer)
            {
                TargetStatus((byte)status, chancePercent, seconds, roll, ignoreImmunity);
            }
        }

        public void RelayKnockback(Vector3 direction, float distance)
        {
            if (isServer)
            {
                TargetKnockback(direction, distance);
            }
        }

        public void RelayHeal(int amount, bool sp, bool showNumber)
        {
            if (isServer)
            {
                TargetHeal(amount, sp, showNumber);
            }
            else
            {
                CmdHealOther(amount, sp, showNumber);
            }
        }

        public void RelayCleanse()
        {
            if (isServer)
            {
                TargetCleanse();
            }
            else
            {
                CmdCleanseOther();
            }
        }

        public void RelayPoise(float amount)
        {
            if (isServer)
            {
                TargetPoise(amount);
            }
        }

        public void RelayBuff(BuffDefinition buff, int level, float duration, int charges, int stackLimit)
        {
            if (buff == null)
            {
                return;
            }

            if (isServer)
            {
                TargetBuff(buff.Id, level, duration, charges, stackLimit);
            }
            else
            {
                CmdBuffOther(buff.Id, level, duration, charges, stackLimit);
            }
        }

        public void RelayProvoke(CombatEntity provoker)
        {
            // Players can't be provoked.
        }

        public void RelaySteal(int skillLevel, int dex)
        {
            // Players can't be robbed.
        }

        public void RelayResurrect(float hpPercent)
        {
            hpPercent = NetWire.Clamp(hpPercent, 1f, 100f);
            if (isServer)
            {
                TargetResurrect(hpPercent);
            }
            else
            {
                CmdResurrectOther(hpPercent);
            }
        }

        public void RelayExperience(long baseExp, long jobExp)
        {
            if (isServer)
            {
                TargetExperience(baseExp, jobExp);
            }
        }

        public void RelayLoot(ItemDefinition item, float baseChance, bool mvpReward)
        {
            if (isServer && item != null)
            {
                TargetLoot(item.Id, baseChance, mvpReward);
            }
        }

        public void RelayBreakWeapon(float chancePercent)
        {
            if (isServer)
            {
                TargetBreakWeapon(chancePercent);
            }
        }

        public void RelayMvp(string monsterName, long bonusExp)
        {
            if (isServer)
            {
                TargetMvp(monsterName ?? string.Empty, bonusExp);
            }
        }

        // ------------------------------------------------------------ another player → realm (friendly effects)
        [Command(requiresAuthority = false)]
        private void CmdHealOther(int amount, bool sp, bool showNumber, NetworkConnectionToClient sender = null)
        {
            if (RealmServer.Instance != null && RealmServer.Instance.MayAffect(sender, this) && amount > 0)
            {
                amount = Mathf.Min(amount, NetWire.MaxHit);
                if (sp)
                {
                    Entity?.RestoreSp(amount, showNumber);
                }
                else
                {
                    Entity?.Heal(amount, showNumber);
                }
            }
        }

        [Command(requiresAuthority = false)]
        private void CmdResurrectOther(float hpPercent, NetworkConnectionToClient sender = null)
        {
            if (RealmServer.Instance != null && RealmServer.Instance.MayAffect(sender, this))
            {
                Entity?.Resurrect(NetWire.Clamp(hpPercent, 1f, 100f));
            }
        }

        [Command(requiresAuthority = false)]
        private void CmdCleanseOther(NetworkConnectionToClient sender = null)
        {
            if (RealmServer.Instance != null && RealmServer.Instance.MayAffect(sender, this))
            {
                Entity?.Cleanse();
            }
        }

        [Command(requiresAuthority = false)]
        private void CmdBuffOther(string buffId, int level, float duration, int charges, int stackLimit, NetworkConnectionToClient sender = null)
        {
            var buff = BuffCatalog.Get(buffId);
            if (buff != null && !buff.IsDebuff && RealmServer.Instance != null && RealmServer.Instance.MayAffect(sender, this))
            {
                Entity?.ApplyBuff(buff, Mathf.Clamp(level, 1, 20), NetWire.Clamp(duration, 0f, 3600f), Mathf.Clamp(charges, 0, 100),
                    Mathf.Clamp(stackLimit, 0, 20));
            }
        }

        // ------------------------------------------------------------ realm → owner
        [TargetRpc]
        private void TargetHit(int amount, byte flags, float elementMultiplier, bool physicalMelee, float poiseDamage, uint attacker)
        {
            _player?.ReceiveDamage(NetWire.Result(amount, flags, elementMultiplier), NetWire.Entity(attacker), physicalMelee,
                NetWire.Clamp(poiseDamage, 0f, 10000f));
        }

        [TargetRpc]
        private void TargetStatus(byte status, float chancePercent, float seconds, bool roll, bool ignoreImmunity)
        {
            if (_player == null || status == 0 || status >= CombatEnumCounts.StatusEffects)
            {
                return;
            }

            if (roll)
            {
                _player.TryApplyStatus((StatusEffect)status, chancePercent, seconds);
            }
            else
            {
                _player.ApplyStatus((StatusEffect)status, seconds, ignoreImmunity);
            }
        }

        [TargetRpc]
        private void TargetKnockback(Vector3 direction, float distance)
        {
            if (NetWire.Finite(direction))
            {
                _player?.Knockback(direction, NetWire.Clamp(distance, 0f, 10f));
            }
        }

        [TargetRpc]
        private void TargetHeal(int amount, bool sp, bool showNumber)
        {
            if (sp)
            {
                _player?.RestoreSp(amount, showNumber);
            }
            else
            {
                _player?.Heal(amount, showNumber);
            }
        }

        [TargetRpc]
        private void TargetResurrect(float hpPercent)
        {
            _player?.Resurrect(NetWire.Clamp(hpPercent, 1f, 100f));
        }

        [TargetRpc]
        private void TargetCleanse()
        {
            _player?.Cleanse();
        }

        [TargetRpc]
        private void TargetPoise(float amount)
        {
            _player?.ApplyPoiseDamage(amount);
        }

        [TargetRpc]
        private void TargetBuff(string buffId, int level, float duration, int charges, int stackLimit)
        {
            var buff = BuffCatalog.Get(buffId);
            if (buff != null)
            {
                _player?.ApplyBuff(buff, level, duration, charges, stackLimit);
            }
        }

        [TargetRpc]
        private void TargetExperience(long baseExp, long jobExp)
        {
            _player?.GrantExperience(baseExp, jobExp);
        }

        [TargetRpc]
        private void TargetLoot(string itemId, float baseChance, bool mvpReward)
        {
            var item = ItemCatalog.Get(itemId);
            if (item != null)
            {
                _player?.ReceiveLoot(item, baseChance, mvpReward);
            }
        }

        [TargetRpc]
        private void TargetBreakWeapon(float chancePercent)
        {
            _player?.TryBreakWeapon(chancePercent);
        }

        [TargetRpc]
        private void TargetMvp(string monsterName, long bonusExp)
        {
            _player?.ReceiveMvp(monsterName, bonusExp);
        }

        /// <summary>Pilfer's answer from the realm: the item (into the bag, or the cart hold when full) or why it failed.</summary>
        [TargetRpc]
        internal void TargetStolen(string itemId, string reason)
        {
            if (_player == null)
            {
                return;
            }

            var item = ItemCatalog.Get(itemId);
            if (item == null)
            {
                if (!string.IsNullOrEmpty(reason) && reason != "Steal failed.")
                {
                    ChatLog.Error(reason);
                }
                else
                {
                    ChatLog.System("Steal failed.");
                }

                return;
            }

            if (_player.Inventory.Add(item.Id, 1) <= 0)
            {
                ItemTransfer.AddToCart(_player.Record, new[] { new ItemStack(item.Id, 1) });
                ChatLog.Loot($"You stole {item.Name} (1); your bag is full, so it waits in your Pushcart hold.");
                return;
            }

            ChatLog.Loot($"You stole {item.Name} (1).");
        }
    }
}
