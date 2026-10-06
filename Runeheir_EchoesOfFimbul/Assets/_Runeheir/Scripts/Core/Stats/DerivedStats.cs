using System;
using Runeheir.Combat;

namespace Runeheir.Stats
{
    /// <summary>
    /// Everything the game derives from Base Level + STR..LUK + modifiers + weapon.
    /// Recomputed whenever stats, level, buffs or weapon change. This is what the status window shows.
    /// </summary>
    public sealed class DerivedStats
    {
        /// <summary>Base + bonus stats.</summary>
        public BaseStats Total;

        /// <summary>Bonus portion of each stat (shown as "+N" in the status window).</summary>
        public BaseStats Bonus;

        public int MaxHp;
        public int MaxSp;

        public int StatusAtk;
        public int WeaponAtk;
        public int MatkMin;
        public int MatkMax;

        public int Def;
        public int SoftDef;
        public int Mdef;
        public int SoftMdef;
        public int Hit;
        public int Flee;
        public float Crit;

        public float Aspd;
        public float AttackPlayRate;
        public float AttackInterval;
        public float SwingDuration;
        public float AttacksPerSecond;

        public float CastTimeMultiplier;
        public float MoveSpeedMultiplier;
        public float AttackRange;
        public int WeightCapacity;
        public int HpRegenPerTick;
        public int SpRegenPerTick;

        public float PhysicalDamagePercent;
        public float MagicDamagePercent;
        public bool ItemsLocked;
        public bool UninterruptibleCasting;
        public bool HyperArmor;

        public static DerivedStats Compute(int baseLevel, BaseStats baseStats, StatModifiers modifiers, WeaponProfile weapon)
        {
            if (baseStats == null)
            {
                throw new ArgumentNullException(nameof(baseStats));
            }

            var mods = modifiers ?? StatModifiers.Empty();
            int level = StatFormulas.Clamp(baseLevel, 1, StatFormulas.MaxBaseLevel);

            var total = new BaseStats();
            var bonus = new BaseStats();
            foreach (var stat in StatTypes.All)
            {
                int extra = mods.GetStat(stat);
                bonus[stat] = extra;
                total[stat] = Math.Max(0, baseStats[stat] + extra);
            }

            var d = new DerivedStats { Total = total, Bonus = bonus };

            double maxHp = StatFormulas.MaxHp(level, total.Vit) + mods.MaxHp;
            maxHp *= 1.0 + mods.MaxHpPercent / 100.0;
            maxHp *= mods.MaxHpMultiplier;
            d.MaxHp = Math.Max(1, (int)Math.Round(maxHp));

            double maxSp = StatFormulas.MaxSp(level, total.Int) + mods.MaxSp;
            maxSp *= 1.0 + mods.MaxSpPercent / 100.0;
            d.MaxSp = Math.Max(1, (int)Math.Round(maxSp));

            d.StatusAtk = StatFormulas.StatusAtk(total.Str) + mods.Atk;
            d.WeaponAtk = Math.Max(0, weapon.Atk) + WeaponRules.RefineAtkBonus(weapon.Level, weapon.Refine);
            d.MatkMin = StatFormulas.MatkMin(total.Int) + mods.Matk;
            d.MatkMax = Math.Max(d.MatkMin, StatFormulas.MatkMax(total.Int) + mods.Matk);

            d.Def = Math.Max(0, mods.Def);
            d.SoftDef = StatFormulas.SoftDef(total.Vit);
            d.Mdef = Math.Max(0, mods.Mdef);
            d.SoftMdef = StatFormulas.SoftMdef(total.Int);
            d.Hit = StatFormulas.Hit(level, total.Dex) + mods.Hit;
            d.Flee = StatFormulas.Flee(level, total.Agi) + mods.Flee;
            d.Crit = StatFormulas.CritChance(total.Luk) + mods.Crit;

            d.Aspd = StatFormulas.Aspd(
                WeaponRules.BaseAspd(weapon.Type),
                total.Agi,
                total.Dex,
                mods.AspdFlat,
                mods.AspdPercent,
                mods.AspdOverride);
            d.AttackPlayRate = StatFormulas.AttackPlayRate(d.Aspd);
            d.SwingDuration = StatFormulas.SwingDuration(d.Aspd);
            d.AttackInterval = StatFormulas.AttackInterval(d.Aspd, mods.CancelAttackRecovery);
            d.AttacksPerSecond = 1f / d.AttackInterval;

            d.CastTimeMultiplier = StatFormulas.CastTimeMultiplier(total.Dex) * Math.Max(0f, 1f + mods.CastTimePercent / 100f);
            d.MoveSpeedMultiplier = Math.Max(0.1f, 1f + mods.MoveSpeedPercent / 100f);
            d.AttackRange = WeaponRules.AttackRange(weapon.Type);
            d.WeightCapacity = StatFormulas.WeightCapacity(total.Str);
            d.HpRegenPerTick = StatFormulas.HpRegenPerTick(d.MaxHp, total.Vit);
            d.SpRegenPerTick = StatFormulas.SpRegenPerTick(d.MaxSp, total.Int);

            d.PhysicalDamagePercent = mods.PhysicalDamagePercent;
            d.MagicDamagePercent = mods.MagicDamagePercent;
            d.ItemsLocked = mods.ItemsLocked;
            d.UninterruptibleCasting = mods.UninterruptibleCasting;
            d.HyperArmor = mods.HyperArmor;
            return d;
        }
    }
}
