using System;

namespace Runeheir.Stats
{
    /// <summary>
    /// Every combat/progression number from GDD §4 lives here so design has a single place to tune.
    /// Pure functions with no Unity dependency; covered by the EditMode tests in Tests/EditMode.
    /// Integer divisions are intentional (Ragnarok-style floor math).
    /// </summary>
    public static class StatFormulas
    {
        // ---------------------------------------------------------------- caps
        public const int MaxBaseLevel = 255;
        public const int MaxJobLevel = 120;
        public const int MinStat = 1;
        public const int MaxStat = 255;

        /// <summary>Renewal-style start: every stat at 1 plus 48 free points.</summary>
        public const int StartingStatPoints = 48;

        // ---------------------------------------------------------------- ASPD (GDD: 150–197, play rate 1.0x–3.0x)
        public const float MinAspd = 150f;
        public const float MaxAspd = 197f;
        public const float MinAttackPlayRate = 1f;
        public const float MaxAttackPlayRate = 3f;

        /// <summary>Length of the attack swing at 1.0x play rate (ASPD 150).</summary>
        public const float BaseSwingSeconds = 0.6f;

        /// <summary>Recovery after the swing at ASPD 150. Shrinks linearly to 0 at ASPD 197.</summary>
        public const float BaseRecoverySeconds = 0.4f;

        /// <summary>Fraction of the swing at which the hit lands (the "impact frame").</summary>
        public const float ImpactFrameFraction = 0.5f;

        // ---------------------------------------------------------------- DEX / LUK
        /// <summary>GDD: 150 DEX = 0.0 s variable cast time.</summary>
        public const int InstantCastDex = 150;

        /// <summary>GDD: crits deal 140% and bypass physical DEF.</summary>
        public const float CriticalDamageMultiplier = 1.4f;

        /// <summary>
        /// GDD (Glacial Tempest): "frozen foes take 300% bonus physical blunt damage".
        /// Interpreted as 300% total (x3). Change to 4 if design means +300% on top.
        /// </summary>
        public const float FrozenBluntDamageMultiplier = 3f;

        // ---------------------------------------------------------------- regen
        public const float HpRegenIntervalSeconds = 6f;
        public const float SpRegenIntervalSeconds = 8f;

        // ================================================================ STR
        /// <summary>GDD: Physical ATK bonus = STR + (STR / 10)^2.</summary>
        public static int StatusAtk(int str)
        {
            int bonus = str / 10;
            return str + bonus * bonus;
        }

        /// <summary>Ragnarok weight capacity: 2000 + STR x 30.</summary>
        public static int WeightCapacity(int str)
        {
            return 2000 + str * 30;
        }

        // ================================================================ VIT / INT
        /// <summary>GDD: Max HP = (BaseLevel x 120) + (VIT x 250).</summary>
        public static int MaxHp(int baseLevel, int vit)
        {
            return baseLevel * 120 + vit * 250;
        }

        /// <summary>GDD: Max SP = (BaseLevel x 25) + (INT x 50).</summary>
        public static int MaxSp(int baseLevel, int intel)
        {
            return baseLevel * 25 + intel * 50;
        }

        /// <summary>Pre-renewal MATK range: INT + (INT/7)^2 .. INT + (INT/5)^2.</summary>
        public static int MatkMin(int intel)
        {
            int bonus = intel / 7;
            return intel + bonus * bonus;
        }

        public static int MatkMax(int intel)
        {
            int bonus = intel / 5;
            return intel + bonus * bonus;
        }

        public static int SoftDef(int vit)
        {
            return vit / 2;
        }

        public static int SoftMdef(int intel)
        {
            return intel / 2;
        }

        public static int HpRegenPerTick(int maxHp, int vit)
        {
            return Math.Max(1, maxHp / 200) + vit / 5;
        }

        public static int SpRegenPerTick(int maxSp, int intel)
        {
            return 1 + maxSp / 100 + intel / 6;
        }

        // ================================================================ DEX / AGI / LUK
        /// <summary>GDD: variable cast multiplier = 1.0 - (DEX / 150). 150+ DEX = instant cast.</summary>
        public static float CastTimeMultiplier(int dex)
        {
            return Math.Max(0f, 1f - dex / (float)InstantCastDex);
        }

        /// <summary>GDD: critical chance (%) = (LUK x 0.35) + 1.0.</summary>
        public static float CritChance(int luk)
        {
            return luk * 0.35f + 1f;
        }

        public static int Hit(int baseLevel, int dex)
        {
            return baseLevel + dex;
        }

        public static int Flee(int baseLevel, int agi)
        {
            return baseLevel + agi;
        }

        // ================================================================ ASPD
        /// <summary>
        /// Raw ASPD before bonuses. AGI drives it (GDD), DEX contributes a little.
        /// The square root gives diminishing returns so 255 AGI lands just past the 197 cap.
        /// </summary>
        public static float AspdFromStats(float weaponBaseAspd, int agi, int dex)
        {
            return weaponBaseAspd + (float)Math.Sqrt(Math.Max(0, agi) * 9.9987 + Math.Max(0, dex) * 0.1922);
        }

        /// <summary>
        /// Final ASPD clamped to [150, 197].
        /// <paramref name="flatBonus"/>: e.g. Two-Hand Surge +7, Fenrir Card +10.
        /// <paramref name="percentBonus"/>: shortens the remaining delay to 200 (potion-style).
        /// <paramref name="overrideAspd"/>: fixed value when &gt; 0 (Rage of Thor = 195, Mjolnir = 197).
        /// </summary>
        public static float Aspd(
            float weaponBaseAspd,
            int agi,
            int dex,
            float flatBonus = 0f,
            float percentBonus = 0f,
            float overrideAspd = 0f)
        {
            if (overrideAspd > 0f)
            {
                return Clamp(overrideAspd, MinAspd, MaxAspd);
            }

            float raw = AspdFromStats(weaponBaseAspd, agi, dex) + flatBonus;
            if (percentBonus != 0f)
            {
                raw = 200f - (200f - raw) * (1f - percentBonus / 100f);
            }

            return Clamp(raw, MinAspd, MaxAspd);
        }

        /// <summary>0 at ASPD 150, 1 at ASPD 197.</summary>
        public static float AspdProgress(float aspd)
        {
            return Clamp((aspd - MinAspd) / (MaxAspd - MinAspd), 0f, 1f);
        }

        /// <summary>GDD: ASPD 150..197 maps linearly to animation play rate 1.0x..3.0x.</summary>
        public static float AttackPlayRate(float aspd)
        {
            return MinAttackPlayRate + (MaxAttackPlayRate - MinAttackPlayRate) * AspdProgress(aspd);
        }

        /// <summary>Seconds the swing animation takes at this ASPD.</summary>
        public static float SwingDuration(float aspd)
        {
            return BaseSwingSeconds / AttackPlayRate(aspd);
        }

        /// <summary>
        /// Full attack cycle (swing + recovery) in seconds.
        /// ASPD 150 = 1.00 s (1 hit/s), 180 ≈ 0.41 s, 197 = 0.20 s (5 hits/s).
        /// <paramref name="cancelRecovery"/> = Two-Hand Surge "recovery canceling".
        /// </summary>
        public static float AttackInterval(float aspd, bool cancelRecovery = false)
        {
            float recovery = cancelRecovery ? 0f : BaseRecoverySeconds * (1f - AspdProgress(aspd));
            return SwingDuration(aspd) + recovery;
        }

        // ================================================================ stat & skill points
        /// <summary>Points needed to raise a stat from <paramref name="currentValue"/> to +1 (pre-renewal curve).</summary>
        public static int StatRaiseCost(int currentValue)
        {
            return (Math.Max(1, currentValue) - 1) / 10 + 2;
        }

        /// <summary>Total points spent to raise a stat from 1 to <paramref name="value"/>.</summary>
        public static int TotalCostToReach(int value)
        {
            int total = 0;
            for (int current = MinStat; current < value; current++)
            {
                total += StatRaiseCost(current);
            }

            return total;
        }

        public static int SpentStatPoints(BaseStats stats)
        {
            int total = 0;
            foreach (var stat in StatTypes.All)
            {
                total += TotalCostToReach(stats[stat]);
            }

            return total;
        }

        /// <summary>Status points granted when reaching <paramref name="newBaseLevel"/> (rAthena: 3 + floor((L-1)/5)).</summary>
        public static int StatPointsGainedAtLevel(int newBaseLevel)
        {
            return newBaseLevel <= 1 ? 0 : 3 + (newBaseLevel - 1) / 5;
        }

        /// <summary>Total status points a character has earned by <paramref name="baseLevel"/> (including the 48 starters).</summary>
        public static int TotalStatPointsAtLevel(int baseLevel)
        {
            int total = StartingStatPoints;
            int capped = Math.Min(baseLevel, MaxBaseLevel);
            for (int level = 2; level <= capped; level++)
            {
                total += StatPointsGainedAtLevel(level);
            }

            return total;
        }

        /// <summary>One skill point per job level gained.</summary>
        public static int SkillPointsAtJobLevel(int jobLevel)
        {
            return Math.Max(0, jobLevel - 1);
        }

        // ================================================================ healing
        /// <summary>Ragnarok Heal: floor((BaseLv + INT) / 8) x (4 + 8 x SkillLv).</summary>
        public static int HealAmount(int baseLevel, int intel, int skillLevel)
        {
            return (baseLevel + intel) / 8 * (4 + 8 * skillLevel);
        }

        // ================================================================ helpers
        public static float Clamp(float value, float min, float max)
        {
            return value < min ? min : value > max ? max : value;
        }

        public static int Clamp(int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
}
