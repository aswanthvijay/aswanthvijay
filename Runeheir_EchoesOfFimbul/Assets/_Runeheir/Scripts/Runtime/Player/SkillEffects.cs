using System.Collections;
using System.Collections.Generic;
using Runeheir.Cameras;
using Runeheir.Combat;
using Runeheir.Field;
using Runeheir.Movement;
using Runeheir.Skills;
using Runeheir.Stats;
using Runeheir.Visuals;
using UnityEngine;

namespace Runeheir.Player
{
    /// <summary>What each <see cref="SkillEffect"/> does once the cast completes. Multi-hit skills run as coroutines.</summary>
    public static class SkillEffects
    {
        private static readonly List<CombatEntity> Targets = new List<CombatEntity>();

        public static IEnumerator Run(PlayerCharacter caster, SkillDefinition skill, CombatEntity target, Vector3 point)
        {
            switch (skill.Effect)
            {
                case SkillEffect.PhysicalStrike:
                case SkillEffect.MagicStrike:
                    for (int hit = 0; hit < Mathf.Max(1, skill.Hits); hit++)
                    {
                        if (!IsValid(caster, target))
                        {
                            yield break;
                        }

                        Strike(caster, skill, target, isLastHit: hit == skill.Hits - 1);
                        if (skill.HitInterval > 0f)
                        {
                            yield return new WaitForSeconds(skill.HitInterval);
                        }
                    }

                    break;

                case SkillEffect.PhysicalAreaAroundSelf:
                    GroundRing.SpawnPulse(caster.Position, new Color(1f, 0.55f, 0.3f, 1f), 0.5f, skill.Radius, 0.35f, 0.15f);
                    for (int wave = 0; wave < Mathf.Max(1, skill.Hits); wave++)
                    {
                        if (caster == null || caster.IsDead)
                        {
                            yield break;
                        }

                        CollectHostiles(caster, caster.Position, skill.Radius);
                        foreach (var enemy in Targets)
                        {
                            Strike(caster, skill, enemy, isLastHit: wave == skill.Hits - 1);
                        }

                        if (skill.HitInterval > 0f)
                        {
                            yield return new WaitForSeconds(skill.HitInterval);
                        }
                    }

                    break;

                case SkillEffect.MagicAreaAtGround:
                    for (int wave = 0; wave < Mathf.Max(1, skill.Hits); wave++)
                    {
                        if (caster == null || caster.IsDead)
                        {
                            yield break;
                        }

                        GroundRing.SpawnPulse(point, new Color(0.65f, 0.9f, 1f, 1f), skill.Radius * 0.3f, skill.Radius, 0.45f, 0.12f);
                        CollectHostiles(caster, point, skill.Radius);
                        foreach (var enemy in Targets)
                        {
                            Strike(caster, skill, enemy, isLastHit: false);
                        }

                        if (skill.HitInterval > 0f)
                        {
                            yield return new WaitForSeconds(skill.HitInterval);
                        }
                    }

                    break;

                case SkillEffect.Heal:
                {
                    var receiver = target != null ? target : caster;
                    int amount = skill.FlatHeal > 0
                        ? skill.FlatHeal + caster.Level
                        : StatFormulas.HealAmount(caster.Level, caster.Stats.Total.Int, skill.HealLevel);
                    receiver.Heal(amount);
                    GroundRing.SpawnPulse(receiver.Position, new Color(0.45f, 1f, 0.55f, 1f), 0.2f, 1.2f, 0.5f);
                    break;
                }

                case SkillEffect.Buff:
                {
                    var receiver = target != null ? target : caster;
                    var buff = BuffCatalog.Get(skill.BuffId);
                    if (buff != null)
                    {
                        receiver.Buffs.Apply(buff, Time.timeAsDouble);
                        if (buff.Modifiers.MaxHpMultiplier > 1f)
                        {
                            // Rage of Thor: the new HP pool starts full.
                            receiver.Heal(receiver.MaxHp, showNumber: false);
                        }

                        GroundRing.SpawnPulse(receiver.Position, RuntimeMaterials.Hex(buff.IconColorHex), 0.3f, 1.4f, 0.6f, 0.12f);
                    }

                    break;
                }

                case SkillEffect.Dash:
                {
                    var motor = caster.GetComponent<NavMotor>();
                    Vector3 start = caster.Position;
                    Vector3 direction = point - start;
                    direction.y = 0f;
                    if (direction.magnitude > skill.Range)
                    {
                        direction = direction.normalized * skill.Range;
                    }

                    Vector3 end = motor.ClampToReachable(start + direction);
                    GroundRing.SpawnPulse(start, new Color(0.3f, 1f, 0.9f, 1f), 0.6f, 0.1f, 0.25f);
                    motor.Warp(end);
                    motor.FaceTowards(start + direction * 2f, instant: true);
                    GroundRing.SpawnPulse(caster.Position, new Color(0.3f, 1f, 0.9f, 1f), 0.1f, 1.2f, 0.35f);
                    break;
                }

                case SkillEffect.FistOfOdin:
                {
                    if (!IsValid(caster, target))
                    {
                        yield break;
                    }

                    int spent = caster.DrainAllSp() + skill.SpCost;
                    var attacker = caster.BuildAttackerProfile();
                    attacker.ForceCritical = false;
                    attacker.Hit = 100000; // never misses
                    var result = DamageCalculator.Physical(attacker, target.BuildDefenderProfile(), 100f * (8f + spent / 10f), false, SystemRandomSource.Shared);
                    result.Amount += 1750;
                    target.ReceiveDamage(result, caster, physicalMelee: true);

                    var rig = Object.FindFirstObjectByType<IsometricCameraRig>();
                    if (rig != null)
                    {
                        rig.Shake(0.45f, 0.5f);
                    }

                    GroundRing.SpawnPulse(target.Position, new Color(1f, 0.75f, 0.2f, 1f), 0.4f, 3.5f, 0.5f, 0.2f);
                    break;
                }
            }
        }

        private static void Strike(PlayerCharacter caster, SkillDefinition skill, CombatEntity target, bool isLastHit)
        {
            if (!IsValid(caster, target))
            {
                return;
            }

            var attacker = caster.BuildAttackerProfile();
            attacker.ForceCritical = false;
            if (!skill.UseWeaponElement)
            {
                attacker.AttackElement = skill.Element;
            }

            var defender = target.BuildDefenderProfile();
            DamageResult result = skill.IsMagic
                ? DamageCalculator.Magical(attacker, defender, skill.Power, skill.Element, SystemRandomSource.Shared)
                : DamageCalculator.Physical(attacker, defender, skill.Power, false, SystemRandomSource.Shared);

            bool melee = !skill.IsMagic && skill.Range <= 2f;
            target.ReceiveDamage(result, caster, physicalMelee: melee);
            if (result.IsMiss || target.IsDead)
            {
                return;
            }

            // Stun lands once on the final hit (Phantom Barrage); freeze rolls every wave (Glacial Tempest).
            bool rollStatus = skill.Status == StatusEffect.Freeze || (skill.Status == StatusEffect.Stun && isLastHit);
            if (rollStatus && Random.value * 100f < skill.StatusChance)
            {
                target.ApplyStatus(skill.Status, skill.StatusDuration);
                WorldFeedback.Announce(target, skill.Status == StatusEffect.Stun ? "Stunned" : "Frozen", new Color(0.7f, 0.9f, 1f));
            }

            if (isLastHit && skill.Knockback > 0f)
            {
                target.Knockback(target.Position - caster.Position, skill.Knockback);
            }
        }

        private static bool IsValid(PlayerCharacter caster, CombatEntity target)
        {
            return caster != null && !caster.IsDead && target != null && !target.IsDead && target.isActiveAndEnabled;
        }

        private static void CollectHostiles(CombatEntity caster, Vector3 center, float radius)
        {
            Targets.Clear();
            foreach (var entity in CombatEntity.All)
            {
                if (caster.IsHostileTo(entity) && !entity.IsDead && CombatEntity.HorizontalDistance(entity.Position, center) <= radius + entity.Radius)
                {
                    Targets.Add(entity);
                }
            }
        }
    }
}
