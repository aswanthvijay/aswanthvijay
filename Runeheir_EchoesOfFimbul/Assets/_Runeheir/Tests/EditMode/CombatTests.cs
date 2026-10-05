using System.Collections.Generic;
using NUnit.Framework;
using Runeheir.Combat;
using Runeheir.Skills;
using Runeheir.Stats;

namespace Runeheir.Tests
{
    /// <summary>Feeds fixed values so random rolls are predictable.</summary>
    internal sealed class SequenceRandom : IRandomSource
    {
        private readonly Queue<double> _values;

        public SequenceRandom(params double[] values)
        {
            _values = new Queue<double>(values);
        }

        public double NextDouble()
        {
            return _values.Count > 0 ? _values.Dequeue() : 0.5;
        }
    }

    public sealed class CombatTests
    {
        private static AttackerProfile Attacker(WeaponType weapon = WeaponType.OneHandSword)
        {
            return new AttackerProfile
            {
                StatusAtk = 100,
                WeaponAtk = 100,
                WeaponVariance = 0f,
                Weapon = weapon,
                AttackElement = Element.Neutral,
                Hit = 100,
                CritChance = 0f,
                MatkMin = 200,
                MatkMax = 200,
            };
        }

        private static DefenderProfile Defender(Size size = Size.Medium, Element element = Element.Neutral)
        {
            return new DefenderProfile { Element = element, Size = size, Race = Race.Beast };
        }

        [Test]
        public void HitChance_Is80PlusHitMinusFlee_Clamped()
        {
            Assert.AreEqual(80f, DamageCalculator.HitChance(50, 50));
            Assert.AreEqual(95f, DamageCalculator.HitChance(500, 0));
            Assert.AreEqual(5f, DamageCalculator.HitChance(0, 500));
        }

        [Test]
        public void Physical_NoDefense_IsStatusPlusWeaponAtk()
        {
            // rolls: crit (skipped, canCrit false), hit roll 0.0 = hit, variance roll
            var result = DamageCalculator.Physical(Attacker(), Defender(), 100f, false, new SequenceRandom(0.0, 0.5));
            Assert.IsFalse(result.IsMiss);
            Assert.AreEqual(200, result.Amount);
        }

        [Test]
        public void Physical_SkillPercentScalesDamage()
        {
            var result = DamageCalculator.Physical(Attacker(), Defender(), 300f, false, new SequenceRandom(0.0, 0.5));
            Assert.AreEqual(600, result.Amount);
        }

        [Test]
        public void Physical_MissesWhenRollAboveHitChance()
        {
            var attacker = Attacker();
            attacker.Hit = 0;
            var defender = Defender();
            defender.Flee = 1000;

            var result = DamageCalculator.Physical(attacker, defender, 100f, true, new SequenceRandom(0.99, 0.5));
            Assert.IsTrue(result.IsMiss);
            Assert.AreEqual(0, result.Amount);
        }

        [Test]
        public void NeverMiss_SkipsTheHitRoll()
        {
            // Fist of Odin: even 0 HIT vs 1000 FLEE with a roll that would miss must connect.
            var attacker = Attacker();
            attacker.Hit = 0;
            attacker.NeverMiss = true;
            var defender = Defender();
            defender.Flee = 1000;

            var result = DamageCalculator.Physical(attacker, defender, 100f, false, new SequenceRandom(0.99, 0.99));
            Assert.IsFalse(result.IsMiss);
            Assert.AreEqual(200, result.Amount);
        }

        [Test]
        public void Critical_Deals140PercentAndIgnoresDefense()
        {
            var attacker = Attacker();
            attacker.ForceCritical = true;
            var defender = Defender();
            defender.Def = 500;
            defender.SoftDef = 100;
            defender.Flee = 1000;

            var result = DamageCalculator.Physical(attacker, defender, 100f, true, new SequenceRandom(0.5));
            Assert.IsTrue(result.IsCritical);
            Assert.AreEqual(280, result.Amount);
        }

        [Test]
        public void Defense_ReducesDamageWithDiminishingReturns()
        {
            Assert.AreEqual(1f, DamageCalculator.HardDefReduction(0f), 1e-6f);
            float at100 = DamageCalculator.HardDefReduction(100f);
            float at400 = DamageCalculator.HardDefReduction(400f);
            Assert.Less(at100, 1f);
            Assert.Less(at400, at100);
            Assert.Greater(at400, 0.1f);
        }

        [Test]
        public void SizeModifier_DaggerVsLargeIsHalfWeaponAtk()
        {
            var result = DamageCalculator.Physical(Attacker(WeaponType.Dagger), Defender(Size.Large), 100f, false, new SequenceRandom(0.0, 0.5));
            Assert.AreEqual(150, result.Amount);
        }

        [Test]
        public void Element_GhostTakesQuarterFromNeutral_PoisonImmuneToPoison()
        {
            var vsGhost = DamageCalculator.Physical(Attacker(), Defender(element: Element.Ghost), 100f, false, new SequenceRandom(0.0, 0.5));
            Assert.AreEqual(50, vsGhost.Amount);

            var poison = Attacker();
            poison.AttackElement = Element.Poison;
            var vsPoison = DamageCalculator.Physical(poison, Defender(element: Element.Poison), 100f, false, new SequenceRandom(0.0, 0.5));
            Assert.AreEqual(0, vsPoison.Amount);
        }

        [Test]
        public void MiasmaWeapon_QuadruplesPhysicalDamage()
        {
            var attacker = Attacker();
            attacker.PhysicalDamagePercent = BuffCatalog.Get(BuffCatalog.MiasmaWeapon).Modifiers.PhysicalDamagePercent;
            var result = DamageCalculator.Physical(attacker, Defender(), 100f, false, new SequenceRandom(0.0, 0.5));
            Assert.AreEqual(800, result.Amount);
        }

        [Test]
        public void FrozenTarget_TakesBonusBluntDamage()
        {
            var defender = Defender();
            defender.BluntDamageTakenMultiplier = StatFormulas.FrozenBluntDamageMultiplier;

            var mace = DamageCalculator.Physical(Attacker(WeaponType.Mace), defender, 100f, false, new SequenceRandom(0.0, 0.5));
            var sword = DamageCalculator.Physical(Attacker(WeaponType.OneHandSword), defender, 100f, false, new SequenceRandom(0.0, 0.5));

            Assert.AreEqual(600, mace.Amount);
            Assert.AreEqual(200, sword.Amount);
        }

        [Test]
        public void Magical_UsesElementTable()
        {
            var vsEarth = DamageCalculator.Magical(Attacker(), Defender(element: Element.Earth), 100f, Element.Fire, new SequenceRandom(0.5));
            Assert.AreEqual(300, vsEarth.Amount, "fire vs earth = 150%");
        }

        [Test]
        public void Buffs_ExpireAndAggregate()
        {
            var buffs = new BuffContainer();
            buffs.Apply(BuffCatalog.Get(BuffCatalog.TwoHandSurge), now: 0);
            Assert.AreEqual(7f, buffs.Aggregate.AspdFlat);
            Assert.IsTrue(buffs.Aggregate.CancelAttackRecovery);

            buffs.Tick(59.9);
            Assert.IsTrue(buffs.Has(BuffCatalog.TwoHandSurge));
            buffs.Tick(60.0);
            Assert.IsFalse(buffs.Has(BuffCatalog.TwoHandSurge));
            Assert.AreEqual(0f, buffs.Aggregate.AspdFlat);
        }

        [Test]
        public void Buffs_ChargesAreConsumed()
        {
            var buffs = new BuffContainer();
            buffs.Apply(BuffCatalog.Get(BuffCatalog.TiwazPrecision), now: 0);

            Assert.IsTrue(buffs.TryConsumeCharge(BuffTraits.CriticalCharges));
            Assert.IsTrue(buffs.TryConsumeCharge(BuffTraits.CriticalCharges));
            Assert.IsTrue(buffs.TryConsumeCharge(BuffTraits.CriticalCharges));
            Assert.IsFalse(buffs.TryConsumeCharge(BuffTraits.CriticalCharges));
            Assert.IsFalse(buffs.Has(BuffCatalog.TiwazPrecision));
        }

        [Test]
        public void Skills_JobLinesInheritAncestorSkills()
        {
            var einherjar = SkillCatalog.ForJob(Jobs.JobId.Einherjar);
            Assert.IsTrue(einherjar.Exists(s => s.Id == "bash"));
            Assert.IsTrue(einherjar.Exists(s => s.Id == "vortex_cleave"));
            Assert.IsTrue(einherjar.Exists(s => s.Id == "first_aid"));
            Assert.IsFalse(einherjar.Exists(s => s.Id == "glacial_tempest"));
            Assert.IsFalse(SkillCatalog.CanUse(Jobs.JobId.Initiate, "bash"));
        }
    }
}
