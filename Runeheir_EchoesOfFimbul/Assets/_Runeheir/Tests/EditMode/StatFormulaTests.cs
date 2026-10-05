using NUnit.Framework;
using Runeheir.Combat;
using Runeheir.Stats;

namespace Runeheir.Tests
{
    public sealed class StatFormulaTests
    {
        [TestCase(1, 1)]
        [TestCase(9, 9)]
        [TestCase(10, 11)]
        [TestCase(100, 200)]
        [TestCase(255, 880)]
        public void StatusAtk_IsStrPlusStrOverTenSquared(int str, int expected)
        {
            Assert.AreEqual(expected, StatFormulas.StatusAtk(str));
        }

        [Test]
        public void MaxHpAndSp_FollowGdd()
        {
            Assert.AreEqual(1 * 120 + 1 * 250, StatFormulas.MaxHp(1, 1));
            Assert.AreEqual(255 * 120 + 255 * 250, StatFormulas.MaxHp(255, 255));
            Assert.AreEqual(255 * 25 + 255 * 50, StatFormulas.MaxSp(255, 255));
        }

        [TestCase(0, 1f)]
        [TestCase(75, 0.5f)]
        [TestCase(149, 1f / 150f)]
        [TestCase(150, 0f)]
        [TestCase(255, 0f)]
        public void CastTime_150DexIsInstant(int dex, float expected)
        {
            Assert.AreEqual(expected, StatFormulas.CastTimeMultiplier(dex), 1e-5f);
        }

        [Test]
        public void CritChance_IsLukTimesPoint35PlusOne()
        {
            Assert.AreEqual(1.35f, StatFormulas.CritChance(1), 1e-4f);
            Assert.AreEqual(36f, StatFormulas.CritChance(100), 1e-4f);
        }

        [Test]
        public void PlayRate_Maps150To1xAnd197To3x()
        {
            Assert.AreEqual(1f, StatFormulas.AttackPlayRate(150f), 1e-5f);
            Assert.AreEqual(2f, StatFormulas.AttackPlayRate(173.5f), 1e-5f);
            Assert.AreEqual(3f, StatFormulas.AttackPlayRate(197f), 1e-5f);
            Assert.AreEqual(1f, StatFormulas.AttackPlayRate(120f), 1e-5f, "clamped below");
            Assert.AreEqual(3f, StatFormulas.AttackPlayRate(250f), 1e-5f, "clamped above");
        }

        [Test]
        public void AttackInterval_OneHitPerSecondAtFloor_FiveAtCap()
        {
            Assert.AreEqual(1.0f, StatFormulas.AttackInterval(150f), 1e-5f);
            Assert.AreEqual(0.2f, StatFormulas.AttackInterval(197f), 1e-5f);
            Assert.Less(StatFormulas.AttackInterval(180f, cancelRecovery: true), StatFormulas.AttackInterval(180f));
        }

        [Test]
        public void AttackInterval_DecreasesMonotonically()
        {
            float previous = float.MaxValue;
            for (float aspd = 150f; aspd <= 197f; aspd += 0.5f)
            {
                float interval = StatFormulas.AttackInterval(aspd);
                Assert.LessOrEqual(interval, previous, $"ASPD {aspd}");
                previous = interval;
            }
        }

        [Test]
        public void Aspd_IsClampedTo150And197()
        {
            Assert.GreaterOrEqual(StatFormulas.Aspd(150f, 1, 1), StatFormulas.MinAspd);
            Assert.AreEqual(197f, StatFormulas.Aspd(150f, 255, 0), 1e-5f);
            Assert.AreEqual(197f, StatFormulas.Aspd(150f, 255, 255, flatBonus: 50f), 1e-5f);
        }

        [Test]
        public void Aspd_FlatBonusAndOverride()
        {
            float baseAspd = StatFormulas.Aspd(150f, 50, 30);
            Assert.AreEqual(baseAspd + 7f, StatFormulas.Aspd(150f, 50, 30, flatBonus: 7f), 1e-4f, "Two-Hand Surge");
            Assert.AreEqual(195f, StatFormulas.Aspd(150f, 1, 1, overrideAspd: 195f), 1e-5f, "Rage of Thor");
        }

        [Test]
        public void Aspd_GrowsWithAgi()
        {
            float previous = 0f;
            for (int agi = 1; agi <= 255; agi += 10)
            {
                float aspd = StatFormulas.Aspd(WeaponRules.BaseAspd(WeaponType.TwoHandSword), agi, 1);
                Assert.GreaterOrEqual(aspd, previous);
                previous = aspd;
            }
        }

        [Test]
        public void StatRaiseCost_PreRenewalCurve()
        {
            Assert.AreEqual(2, StatFormulas.StatRaiseCost(1));
            Assert.AreEqual(2, StatFormulas.StatRaiseCost(10));
            Assert.AreEqual(3, StatFormulas.StatRaiseCost(11));
            Assert.AreEqual(27, StatFormulas.StatRaiseCost(254));
            Assert.AreEqual(0, StatFormulas.TotalCostToReach(1));
            Assert.AreEqual(20, StatFormulas.TotalCostToReach(11));
            Assert.AreEqual(23, StatFormulas.TotalCostToReach(12));
        }

        [Test]
        public void StatPoints_StartAt48AndGrowPerLevel()
        {
            Assert.AreEqual(48, StatFormulas.TotalStatPointsAtLevel(1));
            Assert.AreEqual(51, StatFormulas.TotalStatPointsAtLevel(2));
            Assert.AreEqual(64, StatFormulas.TotalStatPointsAtLevel(6));
            Assert.AreEqual(StatFormulas.TotalStatPointsAtLevel(255), StatFormulas.TotalStatPointsAtLevel(400), "capped at 255");
        }

        [Test]
        public void MaxLevelBudget_OneMaxedStatPlusA200_ButNotTwoMaxed()
        {
            // Build tension by design: at Base 255 you can run 255 STR + 200 AGI with points left
            // for VIT/DEX, but you cannot max two stats.
            int budget = StatFormulas.TotalStatPointsAtLevel(255);
            Assert.GreaterOrEqual(budget, StatFormulas.TotalCostToReach(255) + StatFormulas.TotalCostToReach(200));
            Assert.Less(budget, StatFormulas.TotalCostToReach(255) * 2);
        }

        [Test]
        public void DerivedStats_UseTotalsIncludingBonuses()
        {
            var stats = new BaseStats { Str = 100, Agi = 120, Vit = 80, Int = 1, Dex = 140, Luk = 30 };
            var mods = new StatModifiers().SetStat(StatType.Dex, 10);
            var weapon = new WeaponProfile("Test Claymore", WeaponType.TwoHandSword, 150, 3, refine: 10);

            var derived = DerivedStats.Compute(200, stats, mods, weapon);

            Assert.AreEqual(150, derived.Total.Dex);
            Assert.AreEqual(0f, derived.CastTimeMultiplier, 1e-6f, "140 + 10 DEX = instant cast");
            Assert.AreEqual(StatFormulas.MaxHp(200, 80), derived.MaxHp);
            Assert.AreEqual(200, derived.StatusAtk);
            Assert.AreEqual(150 + 50, derived.WeaponAtk, "Lv3 weapon +10 = +50 ATK");
            Assert.That(derived.Aspd, Is.InRange(StatFormulas.MinAspd, StatFormulas.MaxAspd));
            Assert.AreEqual(StatFormulas.AttackPlayRate(derived.Aspd), derived.AttackPlayRate, 1e-5f);
        }

        [Test]
        public void DerivedStats_RageOfThorTriplesHpAndLocksAspd()
        {
            var stats = new BaseStats { Vit = 50, Agi = 10 };
            var rage = BuffCatalog.Get(BuffCatalog.RageOfThor).Modifiers;
            var weapon = new WeaponProfile("Claymore", WeaponType.TwoHandSword, 100, 2);

            var normal = DerivedStats.Compute(150, stats, null, weapon);
            var raging = DerivedStats.Compute(150, stats, rage, weapon);

            Assert.AreEqual(normal.MaxHp * 3, raging.MaxHp);
            Assert.AreEqual(195f, raging.Aspd, 1e-5f);
            Assert.IsTrue(raging.ItemsLocked);
        }
    }
}
