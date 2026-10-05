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
                {
                    int hits = Mathf.Max(1, skill.Hits);
                    bool anyLanded = false;
                    for (int hit = 0; hit < hits; hit++)
                    {
                        if (!IsValid(caster, target))
                        {
                            yield break;
                        }

                        bool lastHit = hit == hits - 1;
                        anyLanded |= Strike(caster, skill, target, lastHit);

                        // Phantom Barrage's stun (GDD: "guaranteed stun") lands with the barrage as a whole:
                        // a miss on the 8th hit alone must not cancel it.
                        if (lastHit && anyLanded && skill.Status == StatusEffect.Stun)
                        {
                            TryApplyStatus(skill, target);
                        }

                        if (skill.HitInterval > 0f)
                        {
                            yield return new WaitForSeconds(skill.HitInterval);
                        }
                    }

                    break;
                }

                case SkillEffect.PhysicalAreaAroundSelf:
                {
                    GroundRing.SpawnPulse(caster.Position, new Color(1f, 0.55f, 0.3f, 1f), 0.5f, skill.Radius, 0.35f, 0.15f);
                    var launched = new List<CombatEntity>();
                    for (int wave = 0; wave < Mathf.Max(1, skill.Hits); wave++)
                    {
                        if (caster == null || caster.IsDead)
                        {
                            yield break;
                        }

                        CollectHostiles(caster, caster.Position, skill.Radius);
                        bool lastWave = wave == Mathf.Max(1, skill.Hits) - 1;
                        foreach (var enemy in Targets)
                        {
                            if (Strike(caster, skill, enemy, lastWave) && lastWave)
                            {
                                if (skill.Status == StatusEffect.Stun)
                                {
                                    TryApplyStatus(skill, enemy);
                                }

                                if (skill.Knockback > 0f)
                                {
                                    launched.Add(enemy);
                                }
                            }
                        }

                        if (skill.HitInterval > 0f)
                        {
                            yield return new WaitForSeconds(skill.HitInterval);
                        }
                    }

                    if (launched.Count > 0)
                    {
                        // Vortex Cleave (GDD: "Bowling Bash"): launched enemies crash into the ones where they land.
                        if (skill.HitInterval < KnockbackSettleSeconds)
                        {
                            yield return new WaitForSeconds(KnockbackSettleSeconds - Mathf.Max(0f, skill.HitInterval));
                        }

                        ChainImpacts(caster, skill, launched);
                    }

                    break;
                }

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
                    // A dash is a disengage: drop the auto-attack target so the player doesn't run straight back.
                    var autoAttacker = caster.GetComponent<AutoAttacker>();
                    if (autoAttacker != null)
                    {
                        autoAttacker.Disengage();
                    }

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
                    attacker.NeverMiss = true;
                    var result = DamageCalculator.Physical(attacker, target.BuildDefenderProfile(), 100f * (8f + spent / 10f), false, SystemRandomSource.Shared);
                    if (!result.IsMiss && result.ElementMultiplier > 0f)
                    {
                        result.Amount += 1750; // flat bonus on top; element immunity still holds
                    }

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

        private const float KnockbackSettleSeconds = 0.2f;
        private const float ChainImpactRadius = 1.2f;
        private const float ChainImpactPowerScale = 0.5f;

        /// <summary>Each launched enemy that lands among others hits them, and itself, once at half power.</summary>
        private static void ChainImpacts(PlayerCharacter caster, SkillDefinition skill, List<CombatEntity> launched)
        {
            var impacted = new List<CombatEntity>();
            foreach (var body in launched)
            {
                if (!IsValid(caster, body))
                {
                    continue;
                }

                CollectHostiles(caster, body.Position, body.Radius + ChainImpactRadius);
                impacted.Clear();
                foreach (var other in Targets)
                {
                    if (other != body)
                    {
                        impacted.Add(other);
                    }
                }

                if (impacted.Count == 0)
                {
                    continue;
                }

                GroundRing.SpawnPulse(body.Position, new Color(1f, 0.55f, 0.3f, 1f), 0.2f, body.Radius + ChainImpactRadius, 0.25f, 0.1f);
                Strike(caster, skill, body, isLastHit: false, ChainImpactPowerScale);
                foreach (var other in impacted)
                {
                    Strike(caster, skill, other, isLastHit: false, ChainImpactPowerScale);
                }
            }
        }

        /// <returns>True when the hit connected and the target survived it.</returns>
        private static bool Strike(PlayerCharacter caster, SkillDefinition skill, CombatEntity target, bool isLastHit, float powerScale = 1f)
        {
            if (!IsValid(caster, target))
            {
                return false;
            }

            var attacker = caster.BuildAttackerProfile();
            attacker.ForceCritical = false;
            if (!skill.UseWeaponElement)
            {
                attacker.AttackElement = skill.Element;
            }

            var defender = target.BuildDefenderProfile();
            DamageResult result = skill.IsMagic
                ? DamageCalculator.Magical(attacker, defender, skill.Power * powerScale, skill.Element, SystemRandomSource.Shared)
                : DamageCalculator.Physical(attacker, defender, skill.Power * powerScale, false, SystemRandomSource.Shared);

            bool melee = !skill.IsMagic && skill.Range <= 2f;
            target.ReceiveDamage(result, caster, physicalMelee: melee);
            if (result.IsMiss || target.IsDead)
            {
                return false;
            }

            // Freeze rolls on every wave that connects (Glacial Tempest); stun is applied by the caller
            // once per cast (Phantom Barrage).
            if (skill.Status == StatusEffect.Freeze)
            {
                TryApplyStatus(skill, target);
            }

            if (isLastHit && skill.Knockback > 0f)
            {
                target.Knockback(target.Position - caster.Position, skill.Knockback);
            }

            return true;
        }

        private static void TryApplyStatus(SkillDefinition skill, CombatEntity target)
        {
            if (target == null || target.IsDead || !(Random.value * 100f < skill.StatusChance))
            {
                return;
            }

            target.ApplyStatus(skill.Status, skill.StatusDuration);
            WorldFeedback.Announce(target, skill.Status == StatusEffect.Stun ? "Stunned" : "Frozen", new Color(0.7f, 0.9f, 1f));
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
