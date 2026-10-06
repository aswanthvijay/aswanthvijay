using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Runeheir.Characters;
using Runeheir.Combat;
using Runeheir.Items;
using Runeheir.Jobs;
using Runeheir.Monsters;
using Runeheir.World;

namespace Runeheir.Tests
{
    /// <summary>Phase 5 bestiary: the 35 card monsters, their skills and phases, MVP rewards, boss timers and weapon breaking.</summary>
    public sealed class BestiaryTests
    {
        private static IEnumerable<MonsterDefinition> Real => MonsterCatalog.All.Where(m => !m.Immortal);

        // ------------------------------------------------------------ catalog
        [Test]
        public void Bestiary_Has35CardMonsters_4MiniBosses_And3Mvps()
        {
            var carded = MonsterCatalog.All.Where(m => !m.Immortal && !m.SummonOnly).ToList();
            Assert.AreEqual(35, carded.Count, "GDD §6: one monster per Soul Card");
            Assert.AreEqual(4, carded.Count(m => m.Rank == MonsterRank.MiniBoss));
            Assert.AreEqual(3, carded.Count(m => m.Rank == MonsterRank.Mvp));
            var cards = ItemCatalog.All.Where(i => i.IsCard).Select(i => i.Id).ToList();
            CollectionAssert.AreEquivalent(cards, carded.Select(m => m.Id + "_card"), "every card has its monster and every monster its card");
            foreach (var mvp in carded.Where(m => m.IsMvp))
            {
                Assert.AreEqual(0.01f, mvp.Drops.Single(d => d.ItemId == mvp.Id + "_card").ChancePercent, 1e-5f, "GDD: MVP cards drop at 0.01%");
            }
        }

        [Test]
        public void Stats_ClimbWithLevel()
        {
            var normals = Real.Where(m => m.Rank == MonsterRank.Normal && !m.SummonOnly).OrderBy(m => m.Level).ToList();
            for (int i = 1; i < normals.Count; i++)
            {
                var weaker = normals[i - 1];
                var stronger = normals[i];
                if (stronger.Level - weaker.Level >= 10)
                {
                    Assert.Greater(stronger.MaxHp, weaker.MaxHp * 0.9f, $"{stronger.Id} vs {weaker.Id} HP");
                    Assert.Greater(stronger.AtkMax, weaker.AtkMax, $"{stronger.Id} vs {weaker.Id} ATK");
                    Assert.Greater(stronger.BaseExp, weaker.BaseExp, $"{stronger.Id} vs {weaker.Id} EXP");
                }
            }

            foreach (var monster in Real)
            {
                Assert.LessOrEqual(monster.AtkMin, monster.AtkMax, monster.Id);
                Assert.Greater(monster.MaxHp, 0, monster.Id);
                Assert.That(monster.Level, Is.InRange(1, 255), monster.Id);
                if (monster.IsBoss)
                {
                    var peers = Real.Where(m => !m.IsBoss && System.Math.Abs(m.Level - monster.Level) <= 30).ToList();
                    Assert.IsTrue(peers.All(p => monster.MaxHp > p.MaxHp * 4), $"{monster.Id} must be far tougher than monsters of its level");
                }
            }
        }

        [Test]
        public void Skills_AreWellFormed()
        {
            foreach (var monster in Real)
            {
                Assert.AreEqual(monster.Skills.Count, monster.Skills.Select(s => s.Id).Distinct().Count(), $"{monster.Id} skill ids");
                foreach (var skill in monster.Skills)
                {
                    string name = $"{monster.Id}.{skill.Id}";
                    Assert.IsFalse(string.IsNullOrEmpty(skill.Name), name);
                    Assert.Greater(skill.Cooldown, 0f, name);
                    Assert.That(skill.ChancePercent, Is.InRange(1f, 100f), name);
                    Assert.LessOrEqual(skill.MinPhase, monster.Phases.Count, $"{name} needs a phase that exists");
                    switch (skill.Kind)
                    {
                        case MonsterSkillKind.Bolt:
                            Assert.Greater(skill.Range, skill.MinRange, name);
                            break;
                        case MonsterSkillKind.Leap:
                            Assert.Greater(skill.Range, skill.MinRange, name);
                            Assert.Greater(skill.Radius, 0f, $"{name}: a leap blasts where it lands");
                            break;
                        case MonsterSkillKind.Area:
                            Assert.Greater(skill.Radius, 0f, name);
                            Assert.IsTrue(skill.CenteredOnSelf || skill.Range > 0f, $"{name} needs a range");
                            Assert.Greater(skill.CastTime, 0f, $"{name}: areas are telegraphed");
                            break;
                        case MonsterSkillKind.Buff:
                            Assert.IsNotNull(BuffCatalog.Get(skill.BuffId), $"{name} buff {skill.BuffId}");
                            break;
                        case MonsterSkillKind.Heal:
                            Assert.Greater(skill.HealPercent, 0f, name);
                            Assert.Less(skill.BelowHpPercent, 100f, $"{name}: heals only when hurt");
                            break;
                        case MonsterSkillKind.Summon:
                            var minion = MonsterCatalog.Get(skill.SummonId);
                            Assert.IsNotNull(minion, $"{name} summons unknown {skill.SummonId}");
                            Assert.IsFalse(minion.IsBoss, $"{name} can't summon bosses");
                            Assert.That(skill.SummonCount, Is.InRange(1, 4), name);
                            break;
                    }

                    if (skill.Status != StatusEffect.None)
                    {
                        Assert.Greater(skill.StatusChance, 0f, name);
                        Assert.Greater(skill.StatusSeconds, 0f, name);
                    }
                }
            }
        }

        [Test]
        public void Bosses_HavePhasesSummonsAndTheirRewards()
        {
            foreach (var boss in Real.Where(m => m.IsBoss))
            {
                Assert.IsNotEmpty(boss.Phases, $"{boss.Id} has phases");
                for (int i = 1; i < boss.Phases.Count; i++)
                {
                    Assert.Less(boss.Phases[i].BelowHpPercent, boss.Phases[i - 1].BelowHpPercent, $"{boss.Id} phases go down in HP");
                }

                foreach (var phase in boss.Phases)
                {
                    Assert.IsNotNull(BuffCatalog.Get(phase.BuffId), $"{boss.Id} phase buff");
                    Assert.IsFalse(string.IsNullOrEmpty(phase.Shout), $"{boss.Id} announces its phase");
                }

                Assert.IsTrue(boss.Skills.Any(s => s.Kind == MonsterSkillKind.Summon), $"{boss.Id} calls for help");
                Assert.IsTrue(boss.Skills.Any(s => s.HasTelegraph), $"{boss.Id} has an attack you can see coming");
                Assert.IsTrue(boss.Aggressive);
                Assert.AreEqual(0f, boss.RespawnSeconds, "bosses use the map timer");
                if (boss.IsMvp)
                {
                    Assert.AreEqual(2, boss.Phases.Count, $"{boss.Id}: MVPs have two phases");
                    Assert.Greater(boss.MvpExp, 0, boss.Id);
                    Assert.IsNotEmpty(boss.MvpDrops, boss.Id);
                    Assert.IsTrue(boss.MvpDrops.All(d => ItemCatalog.Get(d.ItemId) != null), boss.Id);
                }
                else
                {
                    Assert.IsEmpty(boss.MvpDrops, $"{boss.Id}: only MVPs give MVP drops");
                }
            }

            var godly = new[] { "megingjard", "brisingamen", "mjolnir" };
            foreach (string id in godly)
            {
                Assert.IsTrue(MonsterCatalog.All.Any(m => m.IsMvp && m.MvpDrops.Any(d => d.ItemId == id)), $"{id} comes from an MVP");
            }
        }

        [Test]
        public void EveryItem_CanBeBoughtOrLooted()
        {
            var sold = new HashSet<string>(ShopCatalog.All.SelectMany(s => s.ItemIds));
            var dropped = new HashSet<string>(MonsterCatalog.All.SelectMany(m => m.Drops.Concat(m.MvpDrops)).Select(d => d.ItemId));
            foreach (var item in ItemCatalog.All)
            {
                Assert.IsTrue(sold.Contains(item.Id) || dropped.Contains(item.Id), $"{item.Name} can't be obtained");
            }

            foreach (var monster in MonsterCatalog.All)
            {
                foreach (var drop in monster.Drops.Concat(monster.MvpDrops))
                {
                    Assert.IsNotNull(ItemCatalog.Get(drop.ItemId), $"{monster.Id} drops unknown {drop.ItemId}");
                    Assert.That(drop.ChancePercent, Is.InRange(0.001f, 100f), $"{monster.Id} {drop.ItemId}");
                }
            }
        }

        [Test]
        public void StatusImmunities_FollowRankAndElement()
        {
            var fenrir = MonsterCatalog.Get("fenrir").Resistances;
            Assert.IsTrue(fenrir.IsImmuneTo(StatusEffect.Stun) && fenrir.IsImmuneTo(StatusEffect.Freeze) && fenrir.IsImmuneTo(StatusEffect.Silence));
            Assert.IsFalse(fenrir.IsImmuneTo(StatusEffect.Poison), "bosses can still be poisoned");
            Assert.IsFalse(fenrir.IsImmuneTo(StatusEffect.Stagger), "and staggered, through their big poise");
            Assert.Greater(MonsterCatalog.Get("fenrir").MaxPoise, PoiseRules.MonsterMaxPoise(Size.Large, 255) * 4f);

            var ghoul = MonsterCatalog.Get("ghoul").Resistances;
            Assert.IsTrue(ghoul.IsImmuneTo(StatusEffect.Freeze) && ghoul.IsImmuneTo(StatusEffect.StoneCurse), "Undead element");
            Assert.IsFalse(ghoul.IsImmuneTo(StatusEffect.Stun));
            Assert.IsTrue(MonsterCatalog.Get("frost_wolf").Resistances.IsImmuneTo(StatusEffect.Freeze), "Water element");
            Assert.AreEqual(0, MonsterCatalog.Get("forest_imp").Resistances.ImmunityMask);
        }

        // ------------------------------------------------------------ skill selection
        private static MonsterSkillContext Context(float hp = 100f, int phase = 0, float distance = 0.5f, float range = 1f,
            int alive = 0, bool hasBuff = false)
        {
            return new MonsterSkillContext
            {
                HpPercent = hp, Phase = phase, HasTarget = true, TargetDistance = distance, AttackRange = range,
                SummonsAlive = _ => alive, HasBuff = _ => hasBuff,
            };
        }

        [Test]
        public void Phases_FollowHp()
        {
            var fenrir = MonsterCatalog.Get("fenrir");
            Assert.AreEqual(0, MonsterSkillRules.PhaseFor(fenrir, 100f));
            Assert.AreEqual(0, MonsterSkillRules.PhaseFor(fenrir, 70.1f));
            Assert.AreEqual(1, MonsterSkillRules.PhaseFor(fenrir, 70f));
            Assert.AreEqual(1, MonsterSkillRules.PhaseFor(fenrir, 36f));
            Assert.AreEqual(2, MonsterSkillRules.PhaseFor(fenrir, 10f));
            Assert.AreEqual(0, MonsterSkillRules.PhaseFor(MonsterCatalog.Get("forest_imp"), 1f), "normal monsters have no phases");
        }

        [Test]
        public void SkillConditions_RangeHpPhaseSummonsAndBuffs()
        {
            var wolf = MonsterCatalog.Get("elder_direwolf");
            var bite = wolf.Skill("rending_bite");
            Assert.IsTrue(MonsterSkillRules.IsUsable(bite, Context(distance: 1f, range: 1.4f)));
            Assert.IsFalse(MonsterSkillRules.IsUsable(bite, Context(distance: 3f, range: 1.4f)), "a strike needs reach");

            var pounce = wolf.Skill("savage_pounce");
            Assert.IsFalse(MonsterSkillRules.IsUsable(pounce, Context(distance: 1f)), "no leaping onto someone at your feet");
            Assert.IsTrue(MonsterSkillRules.IsUsable(pounce, Context(distance: 8f)));
            Assert.IsFalse(MonsterSkillRules.IsUsable(pounce, Context(distance: 20f)));

            var howl = wolf.Skill("alpha_howl");
            Assert.IsTrue(MonsterSkillRules.IsUsable(howl, Context(alive: 2)));
            Assert.IsFalse(MonsterSkillRules.IsUsable(howl, Context(alive: 3)), "not while the pack is still up");

            var packHowl = wolf.Skill("pack_howl");
            Assert.IsFalse(MonsterSkillRules.IsUsable(packHowl, Context(hasBuff: true)), "no recasting a buff it has");

            var heal = MonsterCatalog.Get("wood_sprite").Skill("sap_mending");
            Assert.IsFalse(MonsterSkillRules.IsUsable(heal, Context(hp: 80f)));
            Assert.IsTrue(MonsterSkillRules.IsUsable(heal, Context(hp: 40f)));

            var howlOfRagnarok = MonsterCatalog.Get("fenrir").Skill("ragnarok_howl");
            Assert.IsFalse(MonsterSkillRules.IsUsable(howlOfRagnarok, Context(phase: 0, distance: 2f)), "phase 1 skill");
            Assert.IsTrue(MonsterSkillRules.IsUsable(howlOfRagnarok, Context(phase: 1, distance: 2f)));
            Assert.IsFalse(MonsterSkillRules.IsUsable(howlOfRagnarok, Context(phase: 1, distance: 9f)), "a self-centered blast needs the target inside it");
        }

        [Test]
        public void Choose_TakesTheFirstReadySkillThatPassesItsRoll()
        {
            var wolf = MonsterCatalog.Get("elder_direwolf");
            var context = Context(distance: 1f, range: 1.4f);
            // Alpha Howl (60%) is first: a roll of 0.5 passes it.
            Assert.AreEqual("alpha_howl", MonsterSkillRules.Choose(wolf.Skills, context, _ => true, new SequenceRandom(0.5))?.Id);
            // A failed roll moves on: Pack Howl (40%) fails at 0.5 too, the bite (40%) passes at 0.1.
            Assert.AreEqual("rending_bite", MonsterSkillRules.Choose(wolf.Skills, context, _ => true, new SequenceRandom(0.9, 0.9, 0.1))?.Id);
            // Cooldowns: nothing ready means basic attacks.
            Assert.IsNull(MonsterSkillRules.Choose(wolf.Skills, context, _ => false, new SequenceRandom(0.0)));
        }

        // ------------------------------------------------------------ MVP rewards and timers
        [Test]
        public void Mvp_IsTheTopDamageDealer_AndGetsTheFirstMvpDropThatHits()
        {
            var damage = new Dictionary<string, long> { { "Astrid", 4000 }, { "Bjorn", 9000 }, { "Freya", 9000 } };
            Assert.AreEqual("Bjorn", MvpRules.PickMvp(damage));
            Assert.IsNull(MvpRules.PickMvp(new Dictionary<string, long>()));

            var fenrir = MonsterCatalog.Get("fenrir");
            Assert.AreEqual("gleipnir_thread", MvpRules.RollMvpDrop(fenrir, 1f, new SequenceRandom(0.1)));
            Assert.AreEqual("valkyrian_manteau", MvpRules.RollMvpDrop(fenrir, 1f, new SequenceRandom(0.9, 0.2)));
            Assert.AreEqual("megingjard", MvpRules.RollMvpDrop(fenrir, 1f, new SequenceRandom(0.9, 0.9, 0.01)));
            Assert.IsNull(MvpRules.RollMvpDrop(fenrir, 1f, new SequenceRandom(0.9, 0.9, 0.9)));
            Assert.AreEqual("gleipnir_thread", MvpRules.RollMvpDrop(fenrir, 2f, new SequenceRandom(0.99)), "a 2x rate makes 50% certain");
        }

        [Test]
        public void BossTimers_RespawnInTheirWindow()
        {
            var tracker = new BossTracker();
            var spawn = MapCatalog.Get(MapCatalog.Lyngvi).Bosses.Single();
            Assert.IsTrue(tracker.IsDue(MapCatalog.Lyngvi, "fenrir", 0), "a boss that never died is up");

            var status = tracker.RecordKill(MapCatalog.Lyngvi, spawn, "Bjorn", 1000, new SequenceRandom(0.5));
            Assert.IsFalse(status.Alive);
            Assert.AreEqual("Bjorn", status.KilledBy);
            Assert.AreEqual(1000 + 60 * 60, status.RespawnAt, 1e-6, "60 minutes at the middle of the window");
            Assert.IsFalse(tracker.IsDue(MapCatalog.Lyngvi, "fenrir", 1000 + 3599));
            Assert.IsTrue(tracker.IsDue(MapCatalog.Lyngvi, "fenrir", 1000 + 3600));
            Assert.AreEqual(1800, tracker.SecondsUntilDue(MapCatalog.Lyngvi, "fenrir", 1000 + 1800), 1e-6);

            var early = tracker.RecordKill(MapCatalog.Lyngvi, spawn, "Bjorn", 0, new SequenceRandom(0.0));
            Assert.AreEqual((60 - 10) * 60, early.RespawnAt, 1e-6, "the window opens 10 minutes early");
            var late = tracker.RecordKill(MapCatalog.Lyngvi, spawn, "Bjorn", 0, new SequenceRandom(0.999999));
            Assert.AreEqual((60 + 10) * 60, late.RespawnAt, 1, "and closes 10 minutes late");

            tracker.RespawnScale = 0.01f;
            Assert.AreEqual(36, tracker.RecordKill(MapCatalog.Lyngvi, spawn, null, 0, new SequenceRandom(0.5)).RespawnAt, 1e-3, "GM time scale");
            tracker.ResetAll();
            Assert.IsTrue(tracker.IsDue(MapCatalog.Lyngvi, "fenrir", 0));
            tracker.MarkSpawned(MapCatalog.Lyngvi, "fenrir");
            Assert.IsTrue(tracker.Status(MapCatalog.Lyngvi, "fenrir").Alive);

            Assert.AreEqual("1h 05m", BossTracker.FormatDuration(3900));
            Assert.AreEqual("12m 30s", BossTracker.FormatDuration(750));
            Assert.AreEqual("45s", BossTracker.FormatDuration(44.2));
        }

        // ------------------------------------------------------------ weapon break and repair
        private static CharacterRecord Warrior()
        {
            var record = CharacterFactory.Create(new CharacterCreateRequest { Name = "Breaker" }, 0, 0);
            record.Job = JobId.Warrior;
            record.BaseLevel = 99;
            record.Zeny = 100000;
            var set = new EquipmentSet(record, new Inventory(record.Inventory));
            var bag = new Inventory(record.Inventory);
            bag.Add("steel_claymore", 1);
            Assert.IsTrue(set.TryEquip(record.Inventory.Last(s => s.ItemId == "steel_claymore"), out string reason), reason);
            return record;
        }

        [Test]
        public void WeaponBreak_DisarmsUntilBrokkRepairs()
        {
            var record = Warrior();
            var weapon = record.Equipment[(int)EquipPosition.Weapon];
            int atk = EquipmentStats.Compute(record).Weapon.Atk;
            Assert.Greater(atk, 0);

            Assert.IsNull(WeaponBreakRules.TryBreak(record, 3f, new SequenceRandom(0.5)), "the roll failed");
            Assert.AreSame(weapon, WeaponBreakRules.TryBreak(record, 3f, new SequenceRandom(0.01)));
            Assert.IsTrue(weapon.Broken);
            Assert.IsTrue(weapon.DisplayName.EndsWith("(Broken)"));
            var stats = EquipmentStats.Compute(record);
            Assert.IsFalse(stats.HasWeapon, "a broken weapon does nothing");
            Assert.AreEqual(WeaponProfile.BareHands.Atk, stats.Weapon.Atk);
            Assert.IsNull(WeaponBreakRules.TryBreak(record, 100f, new SequenceRandom(0.0)), "already broken");
            Assert.IsTrue(weapon.Clone().Broken, "copies (storage, trade) stay broken");
            Assert.IsFalse(RefineRules.CheckCosts(record, new Inventory(record.Inventory), weapon, false, out string refuse));
            StringAssert.Contains("repaired", refuse);

            int cost = RepairRules.TotalCost(record);
            Assert.AreEqual(9000, cost, "1,000 x weapon level 3 squared");
            record.Zeny = cost - 1;
            Assert.IsFalse(RepairRules.TryRepairAll(record, out _, out string message));
            StringAssert.Contains("9,000", message.Replace(".", ","));
            record.Zeny = cost;
            Assert.IsTrue(RepairRules.TryRepairAll(record, out int repaired, out _));
            Assert.AreEqual(1, repaired);
            Assert.AreEqual(0, record.Zeny);
            Assert.IsFalse(weapon.Broken);
            Assert.AreEqual(atk, EquipmentStats.Compute(record).Weapon.Atk);
            Assert.IsFalse(RepairRules.TryRepairAll(record, out _, out _), "nothing left to fix");
        }

        [Test]
        public void AncientGolemCard_MakesTheWeaponUnbreakable()
        {
            var record = Warrior();
            var weapon = record.Equipment[(int)EquipPosition.Weapon];
            weapon.Cards = new[] { "ancient_golem_card" }.Concat(weapon.Cards.Skip(1)).ToArray();
            Assert.IsTrue(WeaponBreakRules.IsUnbreakable(record));
            Assert.IsNull(WeaponBreakRules.TryBreak(record, 100f, new SequenceRandom(0.0)));
            Assert.IsFalse(weapon.Broken);
        }

        [Test]
        public void BrokenFlag_OnlyStaysOnWeapons()
        {
            var tunic = ItemStack.NewInstance(ItemCatalog.Get("cotton_tunic"));
            tunic.Broken = true;
            tunic.Sanitize();
            Assert.IsFalse(tunic.Broken);
            var seax = ItemStack.NewInstance(ItemCatalog.Get(ItemCatalog.Seax));
            seax.Broken = true;
            seax.Sanitize();
            Assert.IsTrue(seax.Broken);
        }
    }
}
