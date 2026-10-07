using Runeheir.Combat;
using Runeheir.Skills;
using Runeheir.Visuals;
using UnityEngine;

namespace Runeheir.Player
{
    /// <summary>
    /// Phase 7 songs and dances (<see cref="SkillSpecial.Performance"/>): a ring that walks with the Skald or Seidkona for the
    /// skill's <c>ZoneDuration</c>. Every <c>ZoneTick</c> it gives friendly players inside it the song's buff (refreshed, so it
    /// fades a few seconds after they step out), and gives foes inside it the dance's debuff and damage. The performer moves at
    /// half speed and can't start another performance until this one ends.
    /// </summary>
    public sealed class PerformanceAura : MonoBehaviour
    {
        private SkillCast _cast;
        private float _ends;
        private float _nextTick;
        private GroundRing _ring;

        /// <summary>The performance this character is giving, or null.</summary>
        public SkillDefinition Skill => _cast?.Skill;

        public static void Begin(SkillCast cast)
        {
            var caster = cast.Caster;
            if (caster == null || caster.IsDead)
            {
                return;
            }

            var aura = caster.GetComponent<PerformanceAura>();
            if (aura == null)
            {
                aura = caster.gameObject.AddComponent<PerformanceAura>();
            }

            aura.Play(cast);
        }

        /// <summary>Ends the performance now (death, a new map, or the time running out).</summary>
        public void Stop()
        {
            if (_cast?.Caster != null)
            {
                _cast.Caster.Buffs.Remove(SkillBuffs.Performing);
            }

            _cast = null;
            if (_ring != null)
            {
                Destroy(_ring.gameObject);
                _ring = null;
            }

            Destroy(this);
        }

        private void Play(SkillCast cast)
        {
            _cast = cast;
            var skill = cast.Skill;
            float seconds = Mathf.Max(1f, skill.ZoneDuration.At(cast.Level));
            _ends = Time.time + seconds;
            _nextTick = Time.time;

            var performing = BuffCatalog.Get(SkillBuffs.Performing);
            if (performing != null)
            {
                cast.Caster.ApplyBuff(performing, 1, seconds, 0, 0);
            }

            if (_ring == null)
            {
                _ring = GroundRing.Create("Performance", RuntimeMaterials.Hex(skill.IconColorHex), skill.Radius.At(cast.Level), 0.08f);
                _ring.Follow(cast.Caster.transform);
                _ring.SetSpin(skill.DebuffId != null || skill.Damage != SkillDamage.None ? -40f : 40f);
            }
            else
            {
                _ring.SetColor(RuntimeMaterials.Hex(skill.IconColorHex));
                _ring.SetRadius(skill.Radius.At(cast.Level));
            }
        }

        private void Update()
        {
            if (_cast == null)
            {
                Destroy(this);
                return;
            }

            var caster = _cast.Caster;
            if (caster == null || caster.IsDead || Time.time >= _ends)
            {
                Stop();
                return;
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
            Vector3 center = caster.Position;

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

            if (skill.DebuffId != null || skill.Damage != SkillDamage.None)
            {
                foreach (var foe in SkillEffects.CollectHostiles(caster, center, radius))
                {
                    if (skill.DebuffId != null)
                    {
                        SkillEffects.ApplyDebuff(_cast, foe);
                    }

                    if (skill.Damage != SkillDamage.None)
                    {
                        SkillEffects.Strike(_cast, foe, isLastHit: true);
                    }
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
