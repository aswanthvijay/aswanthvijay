using Runeheir.Combat;
using Runeheir.Jobs;
using Runeheir.Online;
using Runeheir.Session;
using Runeheir.Skills;
using Runeheir.Visuals;
using UnityEngine;

namespace Runeheir.Player
{
    /// <summary>
    /// A duet (<see cref="SkillSpecial.Ensemble"/>, Ragnarok's ensembles): the performer who starts it and the partner beside
    /// them are rooted (the Duet buff, which reaches the partner's own game online), and a circle between them gives allies
    /// the duet's buff and foes its debuff or status every tick. It ends when the time runs out, either performer falls, or
    /// they drift apart.
    /// </summary>
    public sealed class EnsembleAura : MonoBehaviour
    {
        private SkillCast _cast;
        private PlayerEntity _partner;
        private float _ends;
        private float _nextTick;
        private GroundRing _ring;

        /// <summary>A partner who can play <paramref name="skill"/> with <paramref name="performer"/> right now, or null.</summary>
        public static PlayerEntity FindPartner(PlayerCharacter performer, SkillDefinition skill)
        {
            if (performer == null || skill == null || string.IsNullOrEmpty(skill.EnsembleKey))
            {
                return null;
            }

            foreach (var entity in CombatEntity.All)
            {
                if (!(entity is PlayerEntity other) || other == performer || other.IsDead
                    || CombatEntity.HorizontalDistance(performer.Position, other.Position) > EnsembleRules.PartnerRange)
                {
                    continue;
                }

                var job = JobOf(other);
                if (!EnsembleRules.ArePartners(performer.Record.Job, job) || !SameParty(other)
                    || other.Buffs.StacksOf(SkillBuffs.Performing) > 0 || other.Buffs.StacksOf(SkillBuffs.Ensemble) > 0)
                {
                    continue;
                }

                // A partner in this game (offline, or tests) must know the duet; another player's game checks its own.
                var half = EnsembleRules.HalfFor(job, skill.EnsembleKey);
                if (other is PlayerCharacter local && (half == null || local.SkillBook.UsableLevel(half.Id) <= 0))
                {
                    continue;
                }

                return other;
            }

            return null;
        }

        public static void Begin(SkillCast cast)
        {
            var performer = cast.Caster as PlayerCharacter;
            var partner = FindPartner(performer, cast.Skill);
            if (performer == null || partner == null)
            {
                ChatLog.Error($"{cast.Skill.Name} needs your partner beside you.");
                return;
            }

            var aura = performer.GetComponent<EnsembleAura>() ?? performer.gameObject.AddComponent<EnsembleAura>();
            aura.Play(cast, partner);
            ChatLog.Notice($"You and {partner.DisplayName} begin {cast.Skill.Name}.");
        }

        private static JobId JobOf(PlayerEntity entity)
        {
            if (entity is PlayerCharacter local)
            {
                return local.Record.Job;
            }

            return entity is RemotePlayer remote ? remote.Job : JobId.Initiate;
        }

        /// <summary>Offline everyone nearby counts as your party; online the partner must be in it.</summary>
        private static bool SameParty(PlayerEntity other)
        {
            if (!OnlineSession.IsOnline)
            {
                return true;
            }

            var party = OnlineSession.Current?.Social?.State?.Party;
            return party != null && party.Member(other.DisplayName) != null;
        }

        private void Play(SkillCast cast, PlayerEntity partner)
        {
            _cast = cast;
            _partner = partner;
            float seconds = Mathf.Max(1f, cast.Skill.ZoneDuration.At(cast.Level));
            _ends = Time.time + seconds;
            _nextTick = Time.time;

            var duet = BuffCatalog.Get(SkillBuffs.Ensemble);
            if (duet != null)
            {
                cast.Caster.ApplyBuff(duet, 1, seconds, 0, 0);
                partner.ApplyBuff(duet, 1, seconds, 0, 0);
            }

            if (_ring == null)
            {
                _ring = GroundRing.Create("Duet", RuntimeMaterials.Hex(cast.Skill.IconColorHex), cast.Skill.Radius.At(cast.Level), 0.1f);
                _ring.SetSpin(cast.Skill.DebuffId != null || cast.Skill.Status != StatusEffect.None ? -30f : 30f);
            }

            _ring.transform.position = Center;
        }

        private Vector3 Center => _partner != null ? Vector3.Lerp(_cast.Caster.Position, _partner.Position, 0.5f) : _cast.Caster.Position;

        /// <summary>Ends the duet now: both performers are free to move again.</summary>
        public void Stop()
        {
            if (_cast?.Caster != null)
            {
                _cast.Caster.Buffs.Remove(SkillBuffs.Ensemble);
            }

            if (_partner != null && !_partner.IsDead)
            {
                _partner.Buffs.Remove(SkillBuffs.Ensemble);
            }

            _cast = null;
            _partner = null;
            Destroy(this);
        }

        private void Update()
        {
            if (_cast == null)
            {
                Destroy(this);
                return;
            }

            var caster = _cast.Caster;
            if (caster == null || caster.IsDead || _partner == null || _partner.IsDead || Time.time >= _ends
                || CombatEntity.HorizontalDistance(caster.Position, _partner.Position) > EnsembleRules.PartnerRange + 1.5f)
            {
                if (caster != null && !caster.IsDead && Time.time < _ends)
                {
                    ChatLog.Notice($"{_cast.Skill.Name} breaks off.");
                }

                Stop();
                return;
            }

            if (_ring != null)
            {
                _ring.transform.position = Center;
            }

            if (Time.time < _nextTick)
            {
                return;
            }

            _nextTick = Time.time + Mathf.Max(0.25f, _cast.Skill.ZoneTick);
            Tick();
        }

        private void Tick()
        {
            var caster = _cast.Caster;
            var skill = _cast.Skill;
            float radius = skill.Radius.At(_cast.Level);
            var center = Center;

            if (skill.BuffId != null)
            {
                float linger = Mathf.Max(skill.BuffDuration.At(_cast.Level), skill.ZoneTick + 1f);
                foreach (var ally in CombatEntity.All)
                {
                    if (ally is PlayerEntity && !ally.IsDead && ally.Faction == caster.Faction
                        && CombatEntity.HorizontalDistance(ally.Position, center) <= radius + ally.Radius)
                    {
                        SkillEffects.ApplyBuff(_cast, ally, linger);
                    }
                }
            }

            if (skill.DebuffId != null || skill.Status != StatusEffect.None)
            {
                foreach (var foe in SkillEffects.CollectHostiles(caster, center, radius))
                {
                    if (skill.DebuffId != null)
                    {
                        SkillEffects.ApplyDebuff(_cast, foe);
                    }

                    SkillEffects.TryStatus(_cast, foe);
                }
            }

            GroundRing.SpawnPulse(center, RuntimeMaterials.Hex(skill.IconColorHex), radius * 0.85f, radius, 0.6f, 0.06f);
        }

        private void OnDestroy()
        {
            if (_ring != null)
            {
                Destroy(_ring.gameObject);
            }
        }
    }
}
