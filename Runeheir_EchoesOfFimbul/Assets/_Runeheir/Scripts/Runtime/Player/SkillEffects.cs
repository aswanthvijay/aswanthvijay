using System.Collections;
using System.Collections.Generic;
using Runeheir.Cameras;
using Runeheir.Combat;
using Runeheir.Field;
using Runeheir.Movement;
using Runeheir.Session;
using Runeheir.Skills;
using Runeheir.Stats;
using Runeheir.Visuals;
using UnityEngine;

namespace Runeheir.Player
{
    /// <summary>
    /// What a skill does once its cast completes. Damage skills are driven by <see cref="SkillDefinition.Damage"/>,
    /// <see cref="SkillDefinition.Area"/> and the leveled numbers; <see cref="SkillDefinition.Special"/> adds
    /// heals, dashes, zones, steals and the GDD signature mechanics. Multi-hit skills run as coroutines.
    /// </summary>
    public static class SkillEffects
    {
        private const float KnockbackSettleSeconds = 0.2f;
        private const float ChainImpactRadius = 1.2f;
        private const float ChainImpactPowerScale = 0.5f;
        private const float LineWidth = 0.7f;
        private const float DashStrikeReach = 1.5f;

        /// <summary>A crafting skill (Rune Forging, Brewing) was used: the HUD opens its window.</summary>
        public static event System.Action<SkillCast> CraftRequested;

        public static IEnumerator Run(SkillCast cast)
        {
            var skill = cast.Skill;
            switch (skill.Special)
            {
                case SkillSpecial.Performance:
                    PerformanceAura.Begin(cast);
                    yield break;
                case SkillSpecial.Resurrect:
                    DoResurrect(cast);
                    yield break;
                case SkillSpecial.StealCoin:
                    DoStealCoin(cast);
                    yield break;
                case SkillSpecial.Craft:
                    CraftRequested?.Invoke(cast);
                    yield break;
                case SkillSpecial.Heal:
                    DoHeal(cast);
                    yield break;
                case SkillSpecial.Cleanse:
                    DoCleanse(cast);
                    yield break;
                case SkillSpecial.Steal:
                    DoSteal(cast);
                    yield break;
                case SkillSpecial.Provoke:
                    DoProvoke(cast);
                    yield break;
                case SkillSpecial.Zone:
                    GroundZone.Spawn(cast);
                    yield break;
                case SkillSpecial.FistOfOdin:
                    DoFistOfOdin(cast);
                    yield break;
                case SkillSpecial.Dash:
                    if (!DoDash(cast) || skill.Damage == SkillDamage.None)
                    {
                        yield break;
                    }

                    break;
                case SkillSpecial.SpiritRelease:
                    cast.HitsOverride = cast.Caster.Buffs.TakeStacks(skill.SphereResource, cast.Level);
                    if (cast.HitsOverride <= 0)
                    {
                        ChatLog.Error("You have no Spirit Spheres. (Spirit Call)");
                        yield break;
                    }

                    break;
            }

            if (skill.BuffId != null)
            {
                ApplyBuff(cast, skill.Target == SkillTarget.Friend && cast.Target != null ? cast.Target : cast.Caster);
            }

            if (skill.Damage != SkillDamage.None)
            {
                yield return DamageRoutine(cast);
            }
            else if (skill.Area != SkillArea.Single)
            {
                // Status-only areas (Stasis Field).
                PulseArea(cast);
                foreach (var enemy in CollectTargets(cast))
                {
                    TryStatus(cast, enemy);
                    ApplyDebuff(cast, enemy);
                }
            }
            else if (skill.Target == SkillTarget.Enemy && IsValid(cast.Caster, cast.Target))
            {
                TryStatus(cast, cast.Target);
                ApplyDebuff(cast, cast.Target);
            }
        }

        // ------------------------------------------------------------ damage
        private static IEnumerator DamageRoutine(SkillCast cast)
        {
            var skill = cast.Skill;
            int hits = cast.Hits;

            // One status roll per cast for multi-hit single-target skills (Phantom Barrage's "guaranteed stun"
            // lands with the barrage as a whole); per landed hit or wave otherwise (Glacial Tempest freezes per wave).
            bool statusOncePerCast = skill.Area == SkillArea.Single && hits > 1;
            var launched = skill.ChainImpacts ? new List<CombatEntity>() : null;
            var tracker = new HitTracker { Remaining = hits };

            for (int wave = 0; wave < hits; wave++)
            {
                if (cast.Caster == null || cast.Caster.IsDead)
                {
                    yield break;
                }

                bool lastWave = wave == hits - 1;
                PulseArea(cast);
                if (skill.Projectile && skill.Area == SkillArea.Single)
                {
                    FireProjectile(cast, lastWave, statusOncePerCast, tracker);
                }
                else
                {
                    foreach (var enemy in CollectTargets(cast))
                    {
                        bool landed = Strike(cast, enemy, lastWave, applyStatus: !statusOncePerCast);
                        tracker.AnyLanded |= landed;
                        if (landed && lastWave && launched != null && enemy.CanBeKnockedBack)
                        {
                            // Only bodies that actually fly can crash into others (not dummies, not hyper-armor).
                            launched.Add(enemy);
                        }
                    }

                    if (lastWave && statusOncePerCast && tracker.AnyLanded)
                    {
                        TryStatus(cast, cast.Target);
                    }
                }

                if (skill.HitInterval > 0f && !lastWave)
                {
                    yield return new WaitForSeconds(skill.HitInterval);
                }
            }

            if (launched != null && launched.Count > 0)
            {
                yield return new WaitForSeconds(KnockbackSettleSeconds);
                ChainImpacts(cast, launched);
            }
        }

        private sealed class HitTracker
        {
            public int Remaining;
            public bool AnyLanded;
        }

        private static void FireProjectile(SkillCast cast, bool lastWave, bool statusOncePerCast, HitTracker tracker)
        {
            var target = cast.Target;
            if (!IsValid(cast.Caster, target))
            {
                return;
            }

            ProjectileFx.Launch(cast.Caster, target, RuntimeMaterials.Hex(cast.Skill.IconColorHex), ProjectileFx.BoltSpeed, () =>
            {
                tracker.Remaining--;
                bool landed = Strike(cast, target, lastWave, applyStatus: !statusOncePerCast);
                tracker.AnyLanded |= landed;
                if (statusOncePerCast && tracker.Remaining <= 0 && tracker.AnyLanded)
                {
                    TryStatus(cast, target);
                }
            });
        }

        /// <summary>
        /// One hit of a skill on <paramref name="target"/>. Returns true when it connected and the target survived.
        /// Also used by <see cref="GroundZone"/> ticks.
        /// </summary>
        internal static bool Strike(SkillCast cast, CombatEntity target, bool isLastHit, bool applyStatus = true, float powerScale = 1f)
        {
            var caster = cast.Caster;
            if (!IsValid(caster, target))
            {
                return false;
            }

            var skill = cast.Skill;
            int level = cast.Level;
            var attacker = caster.BuildAttackerProfile();
            attacker.ForceCritical = cast.Ambush && skill.CanCrit;
            attacker.NeverMiss |= skill.NeverMiss;
            attacker.MagicDamagePercent += cast.BonusMagicPercent;
            if (!skill.UseWeaponElement)
            {
                attacker.AttackElement = skill.Element;
            }

            if (skill.IgnoreDefense)
            {
                attacker.DefBypassPercent = 100f;
            }

            var defender = target.BuildDefenderProfile();
            float power = skill.Power.At(level) * powerScale;
            if (skill.WeightPowerPerThousand > 0f && caster is PlayerCharacter hauler)
            {
                // Cart Charge, Brokkr's Cart Crush: the load behind the blow.
                power *= 1f + skill.WeightPowerPerThousand * hauler.CurrentWeight / 100000f;
            }
            if (skill.BonusVsUndeadPercent > 0f && (defender.Race == Race.Undead || defender.Race == Race.Demon || defender.Element == Element.Undead))
            {
                power *= 1f + skill.BonusVsUndeadPercent / 100f;
            }

            DamageResult result = skill.IsMagic
                ? DamageCalculator.Magical(attacker, defender, power, attacker.AttackElement, SystemRandomSource.Shared)
                : DamageCalculator.Physical(attacker, defender, power, skill.CanCrit, SystemRandomSource.Shared);

            float flat = skill.FlatDamage.At(level);
            if (flat > 0f && !result.IsMiss && result.ElementMultiplier > 0f)
            {
                result.Amount += Mathf.RoundToInt(flat * powerScale);
            }

            float poiseBase = skill.IsMagic
                ? PoiseRules.MagicPoiseDamage * (1f + caster.Stats.PoiseDamagePercent / 100f)
                : caster.BasicPoiseDamage;
            float poise = poiseBase * skill.PoiseMultiplier.At(level) * powerScale * (result.IsCritical ? PoiseRules.CriticalPoiseMultiplier : 1f);

            result = target.ReceiveDamage(result, caster, physicalMelee: skill.IsMelee(level), poise);
            if (!skill.IsMagic && !result.IsMiss && !result.IsBlocked)
            {
                caster.OnPhysicalHitLanded(target, result);
            }

            if (result.IsMiss || result.IsBlocked || target.IsDead)
            {
                return false;
            }

            if (applyStatus)
            {
                TryStatus(cast, target);
            }

            ApplyDebuff(cast, target);
            float knockback = skill.Knockback.At(level);
            if (isLastHit && knockback > 0f)
            {
                Vector3 origin = skill.Area == SkillArea.AtGround ? cast.Point : caster.Position;
                target.Knockback(target.Position - origin, knockback);
            }

            return true;
        }

        /// <summary>
        /// GDD Vortex Cleave: each launched enemy that lands among others crashes into them. The body and everyone it
        /// lands on take one hit at half power; every enemy takes at most one crash hit per cast, however many bodies
        /// land on it, so the damage does not grow with pack size.
        /// </summary>
        private static void ChainImpacts(SkillCast cast, List<CombatEntity> launched)
        {
            var crashed = new HashSet<CombatEntity>();
            foreach (var body in launched)
            {
                if (!IsValid(cast.Caster, body))
                {
                    continue;
                }

                var impacted = CollectHostiles(cast.Caster, body.Position, body.Radius + ChainImpactRadius);
                impacted.Remove(body);
                if (impacted.Count == 0)
                {
                    continue;
                }

                GroundRing.SpawnPulse(body.Position, new Color(1f, 0.55f, 0.3f, 1f), 0.2f, body.Radius + ChainImpactRadius, 0.25f, 0.1f);
                if (crashed.Add(body))
                {
                    Strike(cast, body, isLastHit: false, applyStatus: false, powerScale: ChainImpactPowerScale);
                }

                foreach (var other in impacted)
                {
                    if (crashed.Add(other))
                    {
                        Strike(cast, other, isLastHit: false, applyStatus: false, powerScale: ChainImpactPowerScale);
                    }
                }
            }
        }

        // ------------------------------------------------------------ specials
        private static void DoHeal(SkillCast cast)
        {
            var receiver = cast.Target != null ? cast.Target : cast.Caster;
            int amount = !cast.Skill.FlatHeal.IsZero
                ? Mathf.RoundToInt(cast.Skill.FlatHeal.At(cast.Level)) + cast.Caster.Level
                : StatFormulas.HealAmount(cast.Caster.Level, cast.Caster.Stats.Total.Int, cast.Skill.HealLevel.AtInt(cast.Level));
            receiver.Heal(amount);
            GroundRing.SpawnPulse(receiver.Position, new Color(0.45f, 1f, 0.55f, 1f), 0.2f, 1.2f, 0.5f);
        }

        private static void DoResurrect(SkillCast cast)
        {
            var fallen = cast.Target;
            if (fallen == null || !fallen.IsDead || !(fallen is PlayerEntity))
            {
                ChatLog.Error("Choose a fallen ally.");
                return;
            }

            // Ragnarok: 10% HP at Lv 1, then 30%, 50%, 80%.
            float[] share = { 10f, 30f, 50f, 80f };
            fallen.Resurrect(share[Mathf.Clamp(cast.Level, 1, share.Length) - 1]);
            GroundRing.SpawnPulse(fallen.Position, new Color(1f, 0.97f, 0.8f, 1f), 0.2f, 2f, 0.7f);
            WorldFeedback.Announce(fallen, "Return from Hel", new Color(1f, 0.97f, 0.8f));
        }

        /// <summary>Cut Purse: once per monster, DEX and LUK against its level, a handful of zeny that grows with its level.</summary>
        private static void DoStealCoin(SkillCast cast)
        {
            if (!(cast.Target is Monster monster) || !IsValid(cast.Caster, monster) || !(cast.Caster is PlayerCharacter thief))
            {
                return;
            }

            if (monster.CoinsTaken)
            {
                WorldFeedback.Announce(monster, "Empty purse", new Color(0.75f, 0.75f, 0.75f));
                return;
            }

            var stats = thief.Stats.Total;
            float chance = Mathf.Clamp(cast.Level * 4f + stats.Dex * 0.3f + stats.Luk * 0.2f - monster.Level * 0.25f, 5f, 95f);
            if (Random.value * 100f >= chance)
            {
                WorldFeedback.Announce(monster, "Missed the purse", new Color(0.75f, 0.75f, 0.75f));
                return;
            }

            monster.CoinsTaken = true;
            int zeny = Mathf.Max(1, Mathf.RoundToInt(monster.Level * Random.Range(8f, 16f) * (1f + cast.Level * 0.1f)));
            thief.Record.Zeny += zeny;
            thief.Inventory.NotifyChanged();
            WorldFeedback.Announce(monster, $"+{zeny:N0} z", new Color(1f, 0.85f, 0.3f));
            ChatLog.Loot($"You cut {zeny:N0} zeny from {monster.DisplayName}'s purse.");
        }

        private static void DoCleanse(SkillCast cast)
        {
            var receiver = cast.Target != null ? cast.Target : cast.Caster;
            receiver.Cleanse();
            GroundRing.SpawnPulse(receiver.Position, new Color(0.85f, 1f, 0.9f, 1f), 0.2f, 1.3f, 0.5f);
            WorldFeedback.Announce(receiver, "Purified", new Color(0.85f, 1f, 0.9f));
        }

        private static void DoSteal(SkillCast cast)
        {
            if (!(cast.Target is Monster monster) || !IsValid(cast.Caster, monster))
            {
                return;
            }

            if (monster.IsMirror)
            {
                // Online: the realm's monster is the one robbed; the loot (or the reason) comes back from the realm.
                monster.Remote.RelaySteal(cast.Level, cast.Caster.Stats.Total.Dex);
                return;
            }

            string itemId = monster.TrySteal(cast.Level, cast.Caster.Stats.Total.Dex, out string reason);
            var item = Items.ItemCatalog.Get(itemId);
            if (item != null && cast.Caster.Inventory.Add(item.Id, 1) > 0)
            {
                monster.MarkStolenFrom();
                WorldFeedback.Announce(monster, "Stolen!", new Color(1f, 0.85f, 0.3f));
                ChatLog.Loot($"You stole {item.Name} (1).");
            }
            else if (item != null)
            {
                ChatLog.Error($"You can't carry any more {item.Name}.");
            }
            else
            {
                WorldFeedback.Announce(monster, "Steal failed", new Color(0.75f, 0.75f, 0.75f));
                if (reason != null && reason != "Steal failed.")
                {
                    ChatLog.Error(reason);
                }
            }
        }

        private static void DoProvoke(SkillCast cast)
        {
            var targets = cast.Skill.Area == SkillArea.Single
                ? new List<CombatEntity> { cast.Target }
                : CollectTargets(cast);
            PulseArea(cast);
            foreach (var enemy in targets)
            {
                if (!IsValid(cast.Caster, enemy))
                {
                    continue;
                }

                ApplyDebuff(cast, enemy);
                if (enemy is Monster monster)
                {
                    monster.Provoke(cast.Caster);
                }

                WorldFeedback.Announce(enemy, "Provoked!", new Color(1f, 0.45f, 0.35f));
            }
        }

        /// <summary>Dashes (Aether Snap, Blood Rush, Valkyrie's Descent). Returns false when it didn't move.</summary>
        private static bool DoDash(SkillCast cast)
        {
            var caster = cast.Caster;
            var autoAttacker = caster.GetComponent<AutoAttacker>();
            var motor = caster.GetComponent<NavMotor>();
            if (motor == null)
            {
                return false;
            }

            Vector3 start = caster.Position;
            float range = cast.Skill.Range.At(cast.Level);
            Vector3 destination;
            if (cast.Skill.Target == SkillTarget.Enemy)
            {
                if (!IsValid(caster, cast.Target))
                {
                    return false;
                }

                // Charge: stop just short of the target, up to the skill's range.
                Vector3 toTarget = cast.Target.Position - start;
                toTarget.y = 0f;
                float stopShort = cast.Target.Radius + caster.Radius + 0.15f;
                float travel = Mathf.Clamp(toTarget.magnitude - stopShort, 0f, range);
                destination = start + toTarget.normalized * travel;
            }
            else
            {
                // A ground dash is a disengage (Aether Snap): drop the auto-attack target so you don't run straight back.
                if (cast.Skill.Damage == SkillDamage.None && autoAttacker != null)
                {
                    autoAttacker.Disengage();
                }

                Vector3 direction = cast.Point - start;
                direction.y = 0f;
                if (direction.magnitude > range)
                {
                    direction = direction.normalized * range;
                }

                destination = start + direction;
            }

            Vector3 end = motor.ClampToReachable(destination);
            GroundRing.SpawnPulse(start, new Color(0.3f, 1f, 0.9f, 1f), 0.6f, 0.1f, 0.25f);
            motor.Warp(end);
            Vector3 facing = cast.Skill.Target == SkillTarget.Enemy ? cast.Target.Position : start + (end - start) * 2f;
            motor.FaceTowards(facing, instant: true);
            GroundRing.SpawnPulse(caster.Position, new Color(0.3f, 1f, 0.9f, 1f), 0.1f, 1.2f, 0.35f);

            // A charge that came up short (target out of range) only closes the distance.
            return cast.Skill.Target != SkillTarget.Enemy || caster.EdgeDistanceTo(cast.Target) <= DashStrikeReach;
        }

        private static void DoFistOfOdin(SkillCast cast)
        {
            var caster = cast.Caster;
            var target = cast.Target;
            if (!IsValid(caster, target))
            {
                return;
            }

            int spent = caster.DrainAllSp() + cast.Skill.SpCost.AtInt(cast.Level);
            var attacker = caster.BuildAttackerProfile();
            attacker.ForceCritical = false;
            attacker.NeverMiss = true;
            var result = DamageCalculator.Physical(attacker, target.BuildDefenderProfile(), 100f * (8f + spent / 10f), false, SystemRandomSource.Shared);
            if (!result.IsMiss && result.ElementMultiplier > 0f)
            {
                result.Amount += Mathf.RoundToInt(cast.Skill.FlatDamage.At(cast.Level)); // flat bonus on top; element immunity still holds
            }

            float poise = caster.BasicPoiseDamage * cast.Skill.PoiseMultiplier.At(cast.Level);
            target.ReceiveDamage(result, caster, physicalMelee: true, poise);

            var rig = Object.FindFirstObjectByType<IsometricCameraRig>();
            if (rig != null)
            {
                rig.Shake(0.45f, 0.5f);
            }

            GroundRing.SpawnPulse(target.Position, new Color(1f, 0.75f, 0.2f, 1f), 0.4f, 3.5f, 0.5f, 0.2f);
        }

        // ------------------------------------------------------------ buffs, debuffs, statuses
        internal static void ApplyBuff(SkillCast cast, CombatEntity receiver, float durationOverride = 0f)
        {
            var buff = BuffCatalog.Get(cast.Skill.BuffId);
            if (buff == null || receiver == null || receiver.IsDead)
            {
                return;
            }

            float duration = durationOverride > 0f ? durationOverride : cast.Skill.BuffDuration.At(cast.Level);
            int charges = cast.Skill.BuffCharges.AtInt(cast.Level);
            int stackLimit = buff.MaxStacks > 1 ? cast.Level : 0;
            receiver.ApplyBuff(buff, cast.Level, duration, charges, stackLimit);
            if (buff.Modifiers.MaxHpMultiplier > 1f)
            {
                // Rage of Thor: the new HP pool starts full.
                receiver.Heal(receiver.MaxHp, showNumber: false);
            }

            if (cast.Skill.Special != SkillSpecial.Zone && cast.Skill.Special != SkillSpecial.Performance)
            {
                GroundRing.SpawnPulse(receiver.Position, RuntimeMaterials.Hex(buff.IconColorHex), 0.3f, 1.4f, 0.6f, 0.12f);
            }
        }

        internal static void ApplyDebuff(SkillCast cast, CombatEntity enemy)
        {
            var debuff = BuffCatalog.Get(cast.Skill.DebuffId);
            if (debuff == null || enemy == null || enemy.IsDead)
            {
                return;
            }

            // Strips and charms roll; most debuffs always land.
            float chance = cast.Skill.DebuffChance.At(cast.Level);
            if (chance > 0f && Random.value * 100f >= chance)
            {
                if (cast.Skill.Special != SkillSpecial.Performance)
                {
                    WorldFeedback.Announce(enemy, "Failed", new Color(0.75f, 0.75f, 0.75f));
                }

                return;
            }

            enemy.ApplyBuff(debuff, cast.Level, cast.Skill.DebuffDuration.At(cast.Level));
        }

        internal static void TryStatus(SkillCast cast, CombatEntity target)
        {
            var status = cast.Skill.Status;
            if (status == StatusEffect.None || target == null || target.IsDead)
            {
                return;
            }

            float chance = cast.Skill.StatusChance.At(cast.Level);
            float seconds = cast.Skill.StatusDuration.At(cast.Level);
            bool applied = cast.Skill.GuaranteedStatus && chance >= 100f
                ? target.ApplyStatus(status, seconds)
                : target.TryApplyStatus(status, chance, seconds);
            if (applied)
            {
                var info = StatusRules.Get(status);
                WorldFeedback.Announce(target, info.Name, RuntimeMaterials.Hex(info.ColorHex, Color.white) * 0.5f + Color.white * 0.5f);
            }
        }

        // ------------------------------------------------------------ targeting helpers
        internal static bool IsValid(CombatEntity caster, CombatEntity target)
        {
            return caster != null && !caster.IsDead && target != null && !target.IsDead && target.isActiveAndEnabled;
        }

        /// <summary>Who the skill hits this wave, by its <see cref="SkillArea"/>.</summary>
        internal static List<CombatEntity> CollectTargets(SkillCast cast)
        {
            var caster = cast.Caster;
            float radius = cast.Skill.Radius.At(cast.Level);
            switch (cast.Skill.Area)
            {
                case SkillArea.AroundSelf:
                    return CollectHostiles(caster, caster.Position, radius);
                case SkillArea.AroundTarget:
                    if (!IsValid(caster, cast.Target))
                    {
                        return new List<CombatEntity>();
                    }

                    return CollectHostiles(caster, cast.Target.Position, radius);
                case SkillArea.AtGround:
                    return CollectHostiles(caster, cast.Point, radius);
                case SkillArea.Line:
                    return CollectLine(cast);
                default:
                    return IsValid(caster, cast.Target) ? new List<CombatEntity> { cast.Target } : new List<CombatEntity>();
            }
        }

        internal static List<CombatEntity> CollectHostiles(CombatEntity caster, Vector3 center, float radius)
        {
            var result = new List<CombatEntity>();
            foreach (var entity in CombatEntity.All)
            {
                if (caster.IsHostileTo(entity) && !entity.IsDead && CombatEntity.HorizontalDistance(entity.Position, center) <= radius + entity.Radius)
                {
                    result.Add(entity);
                }
            }

            return result;
        }

        /// <summary>Everyone in a <see cref="LineWidth"/>-wide line from the caster towards the target or point, out to the skill's range.</summary>
        private static List<CombatEntity> CollectLine(SkillCast cast)
        {
            var caster = cast.Caster;
            Vector3 start = caster.Position;
            Vector3 aim = cast.Target != null ? cast.Target.Position : cast.Point;
            Vector3 direction = aim - start;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = caster.transform.forward;
            }

            direction.Normalize();
            float length = cast.Skill.Range.At(cast.Level) + caster.Radius + 0.5f;
            var result = new List<CombatEntity>();
            foreach (var entity in CombatEntity.All)
            {
                if (!caster.IsHostileTo(entity) || entity.IsDead)
                {
                    continue;
                }

                Vector3 offset = entity.Position - start;
                offset.y = 0f;
                float along = Vector3.Dot(offset, direction);
                if (along < -entity.Radius || along > length + entity.Radius)
                {
                    continue;
                }

                float across = (offset - direction * along).magnitude;
                if (across <= LineWidth * 0.5f + entity.Radius)
                {
                    result.Add(entity);
                }
            }

            return result;
        }

        private static void PulseArea(SkillCast cast)
        {
            var skill = cast.Skill;
            float radius = skill.Radius.At(cast.Level);
            Color color = RuntimeMaterials.Hex(skill.IconColorHex);
            switch (skill.Area)
            {
                case SkillArea.AroundSelf:
                    GroundRing.SpawnPulse(cast.Caster.Position, color, 0.5f, radius, 0.35f, 0.15f);
                    break;
                case SkillArea.AroundTarget:
                    if (cast.Target != null)
                    {
                        GroundRing.SpawnPulse(cast.Target.Position, color, 0.3f, radius, 0.3f, 0.12f);
                    }

                    break;
                case SkillArea.AtGround:
                    GroundRing.SpawnPulse(cast.Point, color, radius * 0.3f, radius, 0.45f, 0.12f);
                    break;
                case SkillArea.Line:
                    SlashFx.Line(cast.Caster.Position, cast.Target != null ? cast.Target.Position : cast.Point,
                        skill.Range.At(cast.Level) + cast.Caster.Radius, color);
                    break;
            }
        }
    }
}
