using Mirror;
using Runeheir.Combat;
using Runeheir.Monsters;
using Runeheir.Online;
using Runeheir.Skills;
using Runeheir.Visuals;
using UnityEngine;

namespace Runeheir.Net
{
    /// <summary>
    /// A monster on the network. The realm runs the real one (its brain, skills and HP); every player's game shows a mirror
    /// that copies its HP, phase, buffs and statuses, plays its swings and casts, and sends the players' hits, statuses and
    /// knockbacks back to the realm.
    /// </summary>
    public sealed class NetMonster : NetworkBehaviour, IRemoteEntity, IMonsterObserver
    {
        [SyncVar] private int _hp = 1;
        [SyncVar] private int _maxHp = 1;
        [SyncVar] private bool _dead;
        [SyncVar] private int _phase;
        [SyncVar] private string _auras = string.Empty;

        private Monster _monster;
        private AutoAttacker _attacker;
        private string _appliedAuras;
        private string _auraSignature;
        private float _nextAuraCheck;
        private bool _watching;

        public Monster Monster => _monster;

        private void Awake()
        {
            _monster = GetComponent<Monster>();
            _attacker = GetComponent<AutoAttacker>();
        }

        public override void OnStartServer()
        {
            if (_monster == null || _watching)
            {
                return;
            }

            _watching = true;
            _monster.Observer = this;
            _monster.Damaged += OnDamaged;
            _monster.Died += OnDied;
            _monster.StatusLanded += OnStatusLanded;
            if (_attacker != null)
            {
                _attacker.Swung += OnSwung;
            }

            CopyState(force: true);
        }

        public override void OnStartClient()
        {
            if (isServer || _monster == null)
            {
                return; // the host shows the real monster
            }

            _monster.MakeMirror(this);
            ApplyMirror();
        }

        private void OnDestroy()
        {
            if (!_watching || _monster == null)
            {
                return;
            }

            _monster.Damaged -= OnDamaged;
            _monster.Died -= OnDied;
            _monster.StatusLanded -= OnStatusLanded;
            if (_attacker != null)
            {
                _attacker.Swung -= OnSwung;
            }
        }

        private void Update()
        {
            if (_monster == null)
            {
                return;
            }

            if (isServer)
            {
                CopyState(force: false);
            }
            else if (isClient)
            {
                ApplyMirror();
            }
        }

        // ------------------------------------------------------------ realm: report the real monster
        private void CopyState(bool force)
        {
            _hp = _monster.Hp;
            _maxHp = _monster.MaxHp;
            _dead = _monster.IsDead;
            _phase = _monster.Phase;
            float now = Time.unscaledTime;
            if (force || now >= _nextAuraCheck)
            {
                _nextAuraCheck = now + 0.25f;
                string signature = NetPlayer.AuraSignature(_monster);
                if (signature != _auraSignature)
                {
                    _auraSignature = signature;
                    _auras = AuraCodec.Encode(_monster.Buffs, _monster.Statuses, Time.timeAsDouble);
                }
            }
        }

        private void OnDamaged(DamageResult result, CombatEntity attacker)
        {
            RpcHit(result.Amount, NetWire.Flags(result), result.ElementMultiplier, result.Absorbed, NetWire.IdOf(attacker));
        }

        private void OnDied(CombatEntity killer)
        {
            _dead = true;
            _hp = 0;
            RpcDied(NetWire.IdOf(killer));
        }

        private void OnStatusLanded(StatusEffect status)
        {
            RpcStatus((byte)status);
        }

        private void OnSwung(CombatEntity target)
        {
            RpcSwing(NetWire.IdOf(target));
        }

        public void CastBegan(MonsterSkill skill, CombatEntity target, Vector3 point)
        {
            if (skill != null)
            {
                RpcCastBegan(skill.Id, NetWire.IdOf(target), point);
            }
        }

        public void CastEnded(MonsterSkill skill, CombatEntity target, Vector3 point, bool resolved)
        {
            RpcCastEnded(skill?.Id ?? string.Empty, NetWire.IdOf(target), point, resolved);
        }

        public void PhaseChanged(int phase)
        {
            _phase = phase;
        }

        // ------------------------------------------------------------ players' games: show the mirror
        private void ApplyMirror()
        {
            if (!_monster.IsMirror)
            {
                return;
            }

            if (_dead)
            {
                _monster.MirrorDeath(null);
                return;
            }

            _monster.MirrorVitals(_hp, _maxHp, 0, 1);
            _monster.MirrorPhase(_phase);
            if (_auras != _appliedAuras)
            {
                _appliedAuras = _auras;
                AuraCodec.Apply(_auras, _monster.Buffs, _monster.Statuses, Time.timeAsDouble);
            }
        }

        [ClientRpc]
        private void RpcHit(int amount, byte flags, float elementMultiplier, int absorbed, uint attacker)
        {
            if (!isServer && _monster != null)
            {
                _monster.MirrorHit(NetWire.Result(amount, flags, elementMultiplier, absorbed), NetWire.Entity(attacker));
            }
        }

        [ClientRpc]
        private void RpcDied(uint killer)
        {
            if (!isServer && _monster != null)
            {
                _monster.MirrorDeath(NetWire.Entity(killer));
            }
        }

        [ClientRpc]
        private void RpcStatus(byte status)
        {
            if (!isServer && _monster != null && status > 0 && status < CombatEnumCounts.StatusEffects)
            {
                _monster.MirrorStatus((StatusEffect)status);
            }
        }

        [ClientRpc]
        private void RpcSwing(uint target)
        {
            if (!isServer && _monster != null)
            {
                _monster.MirrorSwing(NetWire.Entity(target));
            }
        }

        [ClientRpc]
        private void RpcCastBegan(string skillId, uint target, Vector3 point)
        {
            if (!isServer && _monster != null)
            {
                _monster.MirrorCast(_monster.Definition?.Skill(skillId), NetWire.Entity(target), point);
            }
        }

        [ClientRpc]
        private void RpcCastEnded(string skillId, uint target, Vector3 point, bool resolved)
        {
            if (!isServer && _monster != null)
            {
                _monster.MirrorCastEnd(_monster.Definition?.Skill(skillId), NetWire.Entity(target), point, resolved);
            }
        }

        // ------------------------------------------------------------ IRemoteEntity: a player's game → the realm
        public void RelayDamage(DamageResult result, CombatEntity attacker, bool physicalMelee, float poiseDamage)
        {
            CmdDamage(result.Amount, NetWire.Flags(result), result.ElementMultiplier, physicalMelee, poiseDamage);
        }

        public void RelayStatus(StatusEffect status, float chancePercent, float seconds, bool roll, bool ignoreImmunity)
        {
            CmdStatus((byte)status, chancePercent, seconds, roll, ignoreImmunity);
        }

        public void RelayKnockback(Vector3 direction, float distance)
        {
            CmdKnockback(direction, distance);
        }

        public void RelayHeal(int amount, bool sp, bool showNumber)
        {
            // Players don't heal monsters.
        }

        public void RelayCleanse()
        {
            // Players don't cleanse monsters.
        }

        public void RelayPoise(float amount)
        {
            CmdPoise(amount);
        }

        public void RelayBuff(BuffDefinition buff, int level, float duration, int charges, int stackLimit)
        {
            if (buff != null)
            {
                CmdBuff(buff.Id, level, duration, charges, stackLimit);
            }
        }

        public void RelayProvoke(CombatEntity provoker)
        {
            CmdProvoke();
        }

        public void RelaySteal(int skillLevel, int dex)
        {
            CmdSteal(skillLevel, dex);
        }

        public void RelayResurrect(float hpPercent)
        {
            // Monsters aren't called back from Hel.
        }

        // ------------------------------------------------------------ realm side of those requests
        [Command(requiresAuthority = false)]
        private void CmdDamage(int amount, byte flags, float elementMultiplier, bool physicalMelee, float poiseDamage,
            NetworkConnectionToClient sender = null)
        {
            if (TryAttacker(sender, out var attacker))
            {
                _monster.ReceiveDamage(NetWire.Result(amount, flags, elementMultiplier), attacker, physicalMelee,
                    NetWire.Clamp(poiseDamage, 0f, 10000f));
            }
        }

        [Command(requiresAuthority = false)]
        private void CmdStatus(byte status, float chancePercent, float seconds, bool roll, bool ignoreImmunity,
            NetworkConnectionToClient sender = null)
        {
            if (status == 0 || status >= CombatEnumCounts.StatusEffects || !TryAttacker(sender, out _))
            {
                return;
            }

            seconds = NetWire.Clamp(seconds, 0f, 120f);
            if (roll)
            {
                _monster.TryApplyStatus((StatusEffect)status, NetWire.Clamp(chancePercent, 0f, 100f), seconds);
            }
            else
            {
                // Ignoring immunity is a GM tool: only on realms that allow @commands.
                bool gm = ignoreImmunity && RealmServer.Instance != null && RealmServer.Instance.Config.AllowGmCommands;
                _monster.ApplyStatus((StatusEffect)status, seconds, gm);
            }
        }

        [Command(requiresAuthority = false)]
        private void CmdKnockback(Vector3 direction, float distance, NetworkConnectionToClient sender = null)
        {
            if (NetWire.Finite(direction) && TryAttacker(sender, out _))
            {
                _monster.Knockback(direction, NetWire.Clamp(distance, 0f, 10f));
            }
        }

        [Command(requiresAuthority = false)]
        private void CmdPoise(float amount, NetworkConnectionToClient sender = null)
        {
            if (TryAttacker(sender, out _))
            {
                _monster.ApplyPoiseDamage(NetWire.Clamp(amount, 0f, 100000f));
            }
        }

        [Command(requiresAuthority = false)]
        private void CmdBuff(string buffId, int level, float duration, int charges, int stackLimit, NetworkConnectionToClient sender = null)
        {
            var buff = BuffCatalog.Get(buffId);
            if (buff != null && TryAttacker(sender, out _))
            {
                _monster.ApplyBuff(buff, Mathf.Clamp(level, 1, 20), NetWire.Clamp(duration, 0f, 600f), Mathf.Clamp(charges, 0, 100),
                    Mathf.Clamp(stackLimit, 0, 20));
            }
        }

        [Command(requiresAuthority = false)]
        private void CmdProvoke(NetworkConnectionToClient sender = null)
        {
            if (TryAttacker(sender, out var provoker))
            {
                _monster.Provoke(provoker);
            }
        }

        [Command(requiresAuthority = false)]
        private void CmdSteal(int skillLevel, int dex, NetworkConnectionToClient sender = null)
        {
            if (!TryAttacker(sender, out _) || sender.identity == null)
            {
                return;
            }

            var thief = sender.identity.GetComponent<NetPlayer>();
            string itemId = _monster.TrySteal(Mathf.Clamp(skillLevel, 1, 10), Mathf.Clamp(dex, 1, 999), out string reason);
            if (itemId != null)
            {
                _monster.MarkStolenFrom();
                WorldFeedback.Announce(_monster, "Stolen!", new Color(1f, 0.85f, 0.3f));
            }

            thief?.TargetStolen(itemId ?? string.Empty, reason ?? string.Empty);
        }

        /// <summary>The sender's character, if it may act on this monster (in the world, on this map, close enough).</summary>
        private bool TryAttacker(NetworkConnectionToClient sender, out PlayerEntity attacker)
        {
            attacker = null;
            if (_monster == null || _monster.IsDead || RealmServer.Instance == null)
            {
                return false;
            }

            return RealmServer.Instance.TryGetActor(sender, transform.position, out attacker);
        }
    }
}
