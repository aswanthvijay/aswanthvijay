using System.Collections.Generic;
using Runeheir.Combat;
using Runeheir.Skills;
using Runeheir.Visuals;
using UnityEngine;

namespace Runeheir.Player
{
    /// <summary>
    /// A skill left on the ground (Venom Dust, Muspel Wall, Sanctuary, Ankle Snare, Bog of Niflheim...).
    /// Every <see cref="SkillDefinition.ZoneTick"/> seconds it repeats the skill's effect on everyone inside:
    /// damage, status, debuff, buff or heal. Traps fire once on the first enemy that steps in.
    /// </summary>
    public sealed class GroundZone : MonoBehaviour
    {
        /// <summary>Most zones of one skill a caster can have at once; the oldest disappears first.</summary>
        public const int MaxPerSkill = 3;

        private static readonly List<GroundZone> Active = new List<GroundZone>();

        private SkillCast _cast;
        private float _radius;
        private float _endsAt;
        private float _nextTickAt;
        private GroundRing _ring;
        private GroundRing _innerRing;

        public SkillDefinition Skill => _cast?.Skill;

        public static GroundZone Spawn(SkillCast cast)
        {
            int own = 0;
            for (int i = Active.Count - 1; i >= 0; i--)
            {
                var zone = Active[i];
                if (zone != null && zone._cast.Caster == cast.Caster && zone._cast.Skill == cast.Skill && ++own >= MaxPerSkill)
                {
                    zone.Expire();
                }
            }

            var go = new GameObject("Zone_" + cast.Skill.Id);
            go.transform.position = cast.Point;
            var created = go.AddComponent<GroundZone>();
            created.Initialize(cast);
            return created;
        }

        private void Initialize(SkillCast cast)
        {
            _cast = cast;
            _radius = Mathf.Max(0.3f, cast.Skill.Radius.At(cast.Level));
            _endsAt = Time.time + cast.Skill.ZoneDuration.At(cast.Level);
            _nextTickAt = Time.time;

            Color color = RuntimeMaterials.Hex(cast.Skill.IconColorHex);
            _ring = GroundRing.Create("ZoneRing", color, _radius, cast.Skill.ZoneTrap ? 0.05f : 0.1f);
            _ring.transform.SetParent(transform, true);
            _ring.ShowAt(transform.position);
            _ring.SetSpin(cast.Skill.ZoneTrap ? 0f : 25f);
            if (!cast.Skill.ZoneTrap)
            {
                _innerRing = GroundRing.Create("ZoneInner", new Color(color.r, color.g, color.b, 0.45f), _radius * 0.55f, 0.06f);
                _innerRing.transform.SetParent(transform, true);
                _innerRing.ShowAt(transform.position);
                _innerRing.SetSpin(-40f);
            }

            Active.Add(this);
        }

        private void Update()
        {
            var caster = _cast?.Caster;
            if (caster == null || caster.IsDead || Time.time >= _endsAt)
            {
                Expire();
                return;
            }

            // Traps watch every frame (a fast monster can cross one between 1 s ticks).
            if (!_cast.Skill.ZoneTrap && Time.time < _nextTickAt)
            {
                return;
            }

            _nextTickAt = Time.time + Mathf.Max(0.1f, _cast.Skill.ZoneTick);
            if (_cast.Skill.ZoneAffectsAllies)
            {
                TickAllies(caster);
            }
            else
            {
                TickEnemies(caster);
            }
        }

        private void TickEnemies(PlayerCharacter caster)
        {
            var skill = _cast.Skill;
            var inside = SkillEffects.CollectHostiles(caster, transform.position, _radius);
            if (inside.Count == 0)
            {
                return;
            }

            if (skill.ZoneTrap && skill.Area != SkillArea.AtGround)
            {
                // A trap springs on the first enemy to step in (the nearest one if several arrive together). Area traps
                // (Muspel Mine, Thorn of Sleep) burst over everyone inside instead.
                CombatEntity first = inside[0];
                foreach (var enemy in inside)
                {
                    if (CombatEntity.HorizontalDistance(enemy.Position, transform.position) < CombatEntity.HorizontalDistance(first.Position, transform.position))
                    {
                        first = enemy;
                    }
                }

                inside.Clear();
                inside.Add(first);
            }

            foreach (var enemy in inside)
            {
                if (skill.Damage != SkillDamage.None)
                {
                    SkillEffects.Strike(_cast, enemy, isLastHit: false);
                }
                else
                {
                    SkillEffects.TryStatus(_cast, enemy);
                    SkillEffects.ApplyDebuff(_cast, enemy);
                }

                float knockback = skill.Knockback.At(_cast.Level);
                if (knockback > 0f && enemy != null && !enemy.IsDead)
                {
                    enemy.Knockback(enemy.Position - transform.position, knockback);
                }
            }

            if (skill.ZoneTrap)
            {
                GroundRing.SpawnPulse(transform.position, RuntimeMaterials.Hex(skill.IconColorHex), 0.2f, _radius * 1.6f, 0.4f, 0.12f);
                Expire();
            }
        }

        private void TickAllies(PlayerCharacter caster)
        {
            var skill = _cast.Skill;
            foreach (var entity in CombatEntity.All)
            {
                if (entity.IsDead || entity.Faction != caster.Faction
                    || CombatEntity.HorizontalDistance(entity.Position, transform.position) > _radius + entity.Radius)
                {
                    continue;
                }

                if (skill.BuffId != null)
                {
                    SkillEffects.ApplyBuff(_cast, entity, skill.BuffDuration.At(_cast.Level));
                }

                int heal = Mathf.RoundToInt(skill.FlatHeal.At(_cast.Level));
                if (heal > 0 && entity.Hp < entity.MaxHp)
                {
                    entity.Heal(heal);
                }
            }
        }

        private void Expire()
        {
            Active.Remove(this);
            if (this != null)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            Active.Remove(this);
        }
    }
}
