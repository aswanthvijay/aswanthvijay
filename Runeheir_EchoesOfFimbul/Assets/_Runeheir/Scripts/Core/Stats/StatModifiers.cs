using System;

namespace Runeheir.Stats
{
    /// <summary>
    /// Bonuses from buffs (and later equipment / soul cards / runes).
    /// Combine rules: flat values and percents add, multipliers multiply, overrides take the highest,
    /// flags OR together.
    /// </summary>
    [Serializable]
    public sealed class StatModifiers
    {
        /// <summary>Bonus STR..LUK, indexed by <see cref="StatType"/>.</summary>
        public int[] FlatStats = new int[StatTypes.Count];

        public int Atk;
        public int Matk;
        public int Def;
        public int Mdef;
        public int Hit;
        public int Flee;
        public float Crit;

        public int MaxHp;
        public int MaxSp;
        public float MaxHpPercent;
        public float MaxSpPercent;

        /// <summary>Multiplicative HP scale, e.g. Rage of Thor = 3.</summary>
        public float MaxHpMultiplier = 1f;

        /// <summary>Flat ASPD, e.g. Two-Hand Surge +7, Fenrir Card +10.</summary>
        public float AspdFlat;

        /// <summary>Percent ASPD (shortens remaining delay).</summary>
        public float AspdPercent;

        /// <summary>Fixed ASPD when &gt; 0 (Rage of Thor 195, Mjolnir 197). Highest wins.</summary>
        public float AspdOverride;

        /// <summary>+ slows casting (Naga Scout +15), - speeds it up.</summary>
        public float CastTimePercent;

        public float MoveSpeedPercent;

        /// <summary>Physical damage bonus in percent (Miasma Weapon = +300 → x4).</summary>
        public float PhysicalDamagePercent;

        public float MagicDamagePercent;

        /// <summary>Two-Hand Surge: removes attack recovery frames.</summary>
        public bool CancelAttackRecovery;

        /// <summary>Rage of Thor: cannot use items.</summary>
        public bool ItemsLocked;

        /// <summary>Casting cannot be interrupted by damage (Naga Scout / Phen).</summary>
        public bool UninterruptibleCasting;

        /// <summary>Rage of Thor "hyper-armor": no flinch, no knockback.</summary>
        public bool HyperArmor;

        public static StatModifiers Empty()
        {
            return new StatModifiers();
        }

        public int GetStat(StatType stat)
        {
            return FlatStats != null && FlatStats.Length > (int)stat ? FlatStats[(int)stat] : 0;
        }

        public StatModifiers SetStat(StatType stat, int value)
        {
            EnsureStatArray();
            FlatStats[(int)stat] = value;
            return this;
        }

        public StatModifiers AddAllStats(int value)
        {
            EnsureStatArray();
            for (int i = 0; i < StatTypes.Count; i++)
            {
                FlatStats[i] += value;
            }

            return this;
        }

        public void Add(StatModifiers other)
        {
            if (other == null)
            {
                return;
            }

            EnsureStatArray();
            for (int i = 0; i < StatTypes.Count; i++)
            {
                FlatStats[i] += other.GetStat((StatType)i);
            }

            Atk += other.Atk;
            Matk += other.Matk;
            Def += other.Def;
            Mdef += other.Mdef;
            Hit += other.Hit;
            Flee += other.Flee;
            Crit += other.Crit;
            MaxHp += other.MaxHp;
            MaxSp += other.MaxSp;
            MaxHpPercent += other.MaxHpPercent;
            MaxSpPercent += other.MaxSpPercent;
            MaxHpMultiplier *= other.MaxHpMultiplier;
            AspdFlat += other.AspdFlat;
            AspdPercent += other.AspdPercent;
            AspdOverride = Math.Max(AspdOverride, other.AspdOverride);
            CastTimePercent += other.CastTimePercent;
            MoveSpeedPercent += other.MoveSpeedPercent;
            PhysicalDamagePercent += other.PhysicalDamagePercent;
            MagicDamagePercent += other.MagicDamagePercent;
            CancelAttackRecovery |= other.CancelAttackRecovery;
            ItemsLocked |= other.ItemsLocked;
            UninterruptibleCasting |= other.UninterruptibleCasting;
            HyperArmor |= other.HyperArmor;
        }

        public void Clear()
        {
            var empty = new StatModifiers();
            FlatStats = new int[StatTypes.Count];
            Atk = empty.Atk;
            Matk = empty.Matk;
            Def = empty.Def;
            Mdef = empty.Mdef;
            Hit = empty.Hit;
            Flee = empty.Flee;
            Crit = empty.Crit;
            MaxHp = empty.MaxHp;
            MaxSp = empty.MaxSp;
            MaxHpPercent = empty.MaxHpPercent;
            MaxSpPercent = empty.MaxSpPercent;
            MaxHpMultiplier = empty.MaxHpMultiplier;
            AspdFlat = empty.AspdFlat;
            AspdPercent = empty.AspdPercent;
            AspdOverride = empty.AspdOverride;
            CastTimePercent = empty.CastTimePercent;
            MoveSpeedPercent = empty.MoveSpeedPercent;
            PhysicalDamagePercent = empty.PhysicalDamagePercent;
            MagicDamagePercent = empty.MagicDamagePercent;
            CancelAttackRecovery = empty.CancelAttackRecovery;
            ItemsLocked = empty.ItemsLocked;
            UninterruptibleCasting = empty.UninterruptibleCasting;
            HyperArmor = empty.HyperArmor;
        }

        private void EnsureStatArray()
        {
            if (FlatStats == null || FlatStats.Length != StatTypes.Count)
            {
                var resized = new int[StatTypes.Count];
                if (FlatStats != null)
                {
                    Array.Copy(FlatStats, resized, Math.Min(FlatStats.Length, resized.Length));
                }

                FlatStats = resized;
            }
        }
    }
}
