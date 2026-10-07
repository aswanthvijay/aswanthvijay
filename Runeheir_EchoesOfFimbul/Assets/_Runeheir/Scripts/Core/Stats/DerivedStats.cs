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

        /// <summary>Percent off NPC shop prices (Haggle), at most <see cref="Items.TradeRules.MaxPricePercent"/>.</summary>
        public float BuyDiscountPercent;

        /// <summary>Percent more from NPC shops when selling (Silver Tongue), at most <see cref="Items.TradeRules.MaxPricePercent"/>.</summary>
        public float SellBonusPercent;
        public int HpRegenPerTick;
        public int SpRegenPerTick;

        public float PhysicalDamagePercent;
        public float MagicDamagePercent;
        public bool ItemsLocked;
        public bool UninterruptibleCasting;
        public bool HyperArmor;

        /// <summary>Poise pool (GDD VIT "stagger / poise resilience").</summary>
        public float MaxPoise;

        /// <summary>Never staggered: Endure, Holdfast, or Rage of Thor's hyper-armor.</summary>
        public bool StaggerImmune;

        public float PoiseDamagePercent;

        /// <summary>Percent damage taken, negative = less. Never below -90.</summary>
        public float DamageTakenPercent;

        /// <summary>Percent chance to block a physical melee hit outright.</summary>
        public float BlockChance;

        /// <summary>Walk speed while casting, percent of normal (0 = rooted while casting).</summary>
        public float CastMoveSpeedPercent;

        /// <summary>What resists statuses aimed at this character.</summary>
        public Combat.StatusResistances StatusResistances;

        public float CritDamagePercent;
        public float LifeStealPercent;
        public float SpStealPercent;
        public int SpDrainOnHit;

        /// <summary>Percent skill cooldown change (negative = shorter). Never below -90.</summary>
        public float CooldownPercent;

        public float ReflectMeleePercent;
        public float ReflectMagicPercent;
        public float DefBypassPercent;
        public float MdefBypassPercent;

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

            float defScale = Math.Max(0f, 1f + mods.DefPercent / 100f);
            float mdefScale = Math.Max(0f, 1f + mods.MdefPercent / 100f);
            d.Def = (int)(Math.Max(0, mods.Def) * defScale);
            d.SoftDef = (int)(StatFormulas.SoftDef(total.Vit) * defScale);
            d.Mdef = (int)(Math.Max(0, mods.Mdef) * mdefScale);
            d.SoftMdef = (int)(StatFormulas.SoftMdef(total.Int) * mdefScale);
            d.Hit = Math.Max(0, (int)((StatFormulas.Hit(level, total.Dex) + mods.Hit) * (1f + mods.HitPercent / 100f)));
            d.Flee = Math.Max(0, (int)((StatFormulas.Flee(level, total.Agi) + mods.Flee) * (1f + mods.FleePercent / 100f)));
            d.Crit = (StatFormulas.CritChance(total.Luk) + mods.Crit) * Math.Max(0f, 1f + mods.CritPercent / 100f);

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
            d.AttackRange = WeaponRules.AttackRange(weapon.Type) + Math.Max(0f, mods.AttackRange);
            d.WeightCapacity = StatFormulas.WeightCapacity(total.Str) + Math.Max(0, mods.WeightCapacity);
            d.BuyDiscountPercent = Math.Max(0f, Math.Min(Items.TradeRules.MaxPricePercent, mods.BuyDiscountPercent));
            d.SellBonusPercent = Math.Max(0f, Math.Min(Items.TradeRules.MaxPricePercent, mods.SellBonusPercent));
            d.HpRegenPerTick = StatFormulas.HpRegenPerTick(d.MaxHp, total.Vit) + Math.Max(0, mods.HpRegenFlat);
            d.SpRegenPerTick = StatFormulas.SpRegenPerTick(d.MaxSp, total.Int) + Math.Max(0, mods.SpRegenFlat);

            d.PhysicalDamagePercent = mods.PhysicalDamagePercent;
            d.MagicDamagePercent = mods.MagicDamagePercent;
            d.ItemsLocked = mods.ItemsLocked;
            d.UninterruptibleCasting = mods.UninterruptibleCasting;
            d.HyperArmor = mods.HyperArmor;
            d.MaxPoise = Combat.PoiseRules.PlayerMaxPoise(total.Vit, level) * Math.Max(0.1f, 1f + mods.MaxPoisePercent / 100f);
            d.StaggerImmune = mods.StaggerImmune || mods.HyperArmor;
            d.PoiseDamagePercent = mods.PoiseDamagePercent;
            d.DamageTakenPercent = Math.Max(-90f, mods.DamageTakenPercent);
            d.BlockChance = StatFormulas.Clamp(mods.BlockChance, 0f, 95f);
            d.CastMoveSpeedPercent = StatFormulas.Clamp(mods.CastMoveSpeedPercent, 0f, 100f);
            d.CritDamagePercent = mods.CritDamagePercent;
            d.LifeStealPercent = Math.Max(0f, mods.LifeStealPercent);
            d.SpStealPercent = Math.Max(0f, mods.SpStealPercent);
            d.SpDrainOnHit = Math.Max(0, mods.SpDrainOnHit);
            d.CooldownPercent = Math.Max(-90f, mods.CooldownPercent);
            d.ReflectMeleePercent = StatFormulas.Clamp(mods.ReflectMeleePercent, 0f, 100f);
            d.ReflectMagicPercent = StatFormulas.Clamp(mods.ReflectMagicPercent, 0f, 100f);
            d.DefBypassPercent = StatFormulas.Clamp(mods.DefBypassPercent, 0f, 100f);
            d.MdefBypassPercent = StatFormulas.Clamp(mods.MdefBypassPercent, 0f, 100f);
            d.StatusResistances = new Combat.StatusResistances
            {
                Vit = total.Vit,
                Int = total.Int,
                Luk = total.Luk,
                Agi = total.Agi,
                Mdef = d.Mdef,
            };
            return d;
        }
    }
}
