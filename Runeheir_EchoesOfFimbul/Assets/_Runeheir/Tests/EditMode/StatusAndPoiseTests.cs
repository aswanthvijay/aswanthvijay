using System.Collections.Generic;
using NUnit.Framework;
using Runeheir.Combat;

namespace Runeheir.Tests
{
    public sealed class StatusAndPoiseTests
    {
        [Test]
        public void Statuses_ApplyExtendAndExpire()
        {
            var statuses = new StatusContainer();
            int changes = 0;
            statuses.Changed += () => changes++;

            Assert.IsTrue(statuses.Apply(StatusEffect.Stun, 2f, now: 0));
            Assert.IsTrue(statuses.IsIncapacitated);
            Assert.IsTrue(statuses.BlocksSkills && statuses.BlocksMovement);

            statuses.Apply(StatusEffect.Stun, 1f, now: 0.5);
            Assert.AreEqual(1.5, statuses.Remaining(StatusEffect.Stun, 0.5), 0.0001, "a shorter reapply never cuts it short");

            statuses.Tick(1.99);
            Assert.IsTrue(statuses.Has(StatusEffect.Stun));
            statuses.Tick(2.0);
            Assert.IsFalse(statuses.IsIncapacitated);
            Assert.AreEqual(3, changes);
            Assert.IsFalse(statuses.Apply(StatusEffect.None, 5f, 0));
        }

        [Test]
        public void Statuses_FlagsAndModifiers()
        {
            var statuses = new StatusContainer();
            statuses.Apply(StatusEffect.Silence, 5f, 0);
            Assert.IsTrue(statuses.BlocksSkills);
            Assert.IsFalse(statuses.BlocksMovement);
            Assert.IsFalse(statuses.IsIncapacitated);

            statuses.Apply(StatusEffect.Root, 5f, 0);
            Assert.IsTrue(statuses.BlocksMovement);

            statuses.Apply(StatusEffect.Frostbite, 5f, 0);
            Assert.AreEqual(-50f, statuses.Aggregate.MoveSpeedPercent, "GDD: halves movement speed and ASPD");
            Assert.AreEqual(-50f, statuses.Aggregate.AspdPercent);

            statuses.Apply(StatusEffect.Blind, 5f, 0);
            Assert.AreEqual(-25f, statuses.Aggregate.HitPercent);

            statuses.Clear();
            Assert.AreEqual(0f, statuses.Aggregate.MoveSpeedPercent);
        }

        [Test]
        public void Statuses_DamageOverTimeTicks()
        {
            var statuses = new StatusContainer();
            statuses.Apply(StatusEffect.Poison, 3f, now: 0);
            Assert.IsTrue(statuses.BlocksRegen);

            var dots = new List<float>();
            statuses.Tick(0.5, dots);
            Assert.AreEqual(0, dots.Count);
            statuses.Tick(2.0, dots);
            Assert.AreEqual(2, dots.Count, "ticks at 1 s and 2 s");
            Assert.AreEqual(1.5f, dots[0]);
            statuses.Tick(10.0, dots);
            Assert.AreEqual(3, dots.Count, "the 3 s tick still counts, then it expires");
            Assert.IsFalse(statuses.Has(StatusEffect.Poison));
        }

        [Test]
        public void Statuses_SleepAndStoneBreakOnDamage_FreezeDoesNot()
        {
            var statuses = new StatusContainer();
            statuses.Apply(StatusEffect.Sleep, 10f, 0);
            statuses.Apply(StatusEffect.StoneCurse, 10f, 0);
            statuses.Apply(StatusEffect.Freeze, 10f, 0);
            statuses.BreakOnDamage();
            Assert.IsFalse(statuses.Has(StatusEffect.Sleep));
            Assert.IsFalse(statuses.Has(StatusEffect.StoneCurse));
            Assert.IsTrue(statuses.Has(StatusEffect.Freeze), "GDD combo: frozen foes stay frozen for the blunt follow-up");
        }

        [Test]
        public void StatusResistance_ScalesWithTheRightStat()
        {
            var tough = new StatusResistances { Vit = 100 };
            Assert.AreEqual(50f, StatusRules.ResistPercent(StatusEffect.Stun, tough));
            Assert.AreEqual(25f, StatusRules.EffectiveChance(StatusEffect.Stun, 50f, tough), 0.001f);
            Assert.AreEqual(7.5f, StatusRules.EffectiveDuration(StatusEffect.Stun, 10f, tough), 0.001f);
            Assert.AreEqual(0f, StatusRules.ResistPercent(StatusEffect.Sleep, tough), "sleep is resisted by INT");

            var maxed = new StatusResistances { Vit = 255, Int = 255, Luk = 255, Agi = 255, Mdef = 100 };
            Assert.AreEqual(StatusRules.MaxResistPercent, StatusRules.ResistPercent(StatusEffect.Stun, maxed));
            Assert.AreEqual(0f, StatusRules.ResistPercent(StatusEffect.Frostbite, maxed), "GDD: frostbite is unblockable");

            Assert.IsTrue(StatusRules.Roll(StatusEffect.Stun, 100f, default, new SequenceRandom(0.9999)), "100% vs no resist is guaranteed");
            Assert.IsFalse(StatusRules.Roll(StatusEffect.Stun, 100f, new StatusResistances { Immune = true }, new SequenceRandom(0.0)));
        }

        [Test]
        public void Poise_BreaksAtZeroThenGivesImmunity()
        {
            var poise = new PoiseMeter(60f);
            Assert.IsFalse(poise.Apply(25f, now: 0, canStagger: true));
            Assert.IsFalse(poise.Apply(25f, now: 0.5, canStagger: true));
            Assert.AreEqual(10f, poise.Current, 0.001f);

            Assert.IsTrue(poise.Apply(25f, now: 1, canStagger: true), "third heavy hit staggers");
            Assert.AreEqual(60f, poise.Current, "poise refills after a stagger");
            Assert.IsTrue(poise.IsImmune(1 + PoiseRules.StaggerSeconds + PoiseRules.ImmunitySeconds - 0.01));
            Assert.IsFalse(poise.Apply(100f, now: 2, canStagger: true), "no stun-lock");
            Assert.IsTrue(poise.Apply(100f, now: 1 + PoiseRules.StaggerSeconds + PoiseRules.ImmunitySeconds, canStagger: true));
        }

        [Test]
        public void Poise_RecoversAfterAPause_AndHyperArmorNeverBreaks()
        {
            var poise = new PoiseMeter(50f);
            poise.Apply(40f, now: 0, canStagger: true);
            poise.Tick(PoiseRules.RecoverDelaySeconds - 0.1);
            Assert.AreEqual(10f, poise.Current, 0.001f);
            poise.Tick(PoiseRules.RecoverDelaySeconds);
            Assert.AreEqual(50f, poise.Current);

            var armored = new PoiseMeter(50f);
            for (int i = 0; i < 10; i++)
            {
                Assert.IsFalse(armored.Apply(40f, now: i * 0.1, canStagger: false));
            }

            Assert.GreaterOrEqual(armored.Current, 1f);

            armored.SetMax(100f);
            Assert.AreEqual(100f, armored.Max);
            Assert.AreEqual(2f, armored.Current, 0.001f, "keeps the fraction");
        }

        [Test]
        public void PoiseRules_VitAndWeaponsMatter()
        {
            Assert.Greater(PoiseRules.PlayerMaxPoise(100, 99), PoiseRules.PlayerMaxPoise(1, 99));
            Assert.Greater(PoiseRules.MonsterMaxPoise(Size.Large, 50), PoiseRules.MonsterMaxPoise(Size.Small, 50));
            Assert.Greater(WeaponRules.PoiseDamage(WeaponType.TwoHandSword), WeaponRules.PoiseDamage(WeaponType.Dagger));
            Assert.Greater(PoiseRules.MonsterPoiseDamage(Size.Large, 100), PoiseRules.MonsterPoiseDamage(Size.Medium, 100));
        }
    }
}
