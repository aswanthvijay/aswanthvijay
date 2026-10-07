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

        /// <summary>Percent HIT / FLEE (Blind -25).</summary>
        public float HitPercent;

        public float FleePercent;

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

        /// <summary>Rage of Thor "hyper-armor": no flinch, no knockback, never staggered.</summary>
        public bool HyperArmor;

        /// <summary>Percent hard and soft DEF (Provoke and Poison lower it).</summary>
        public float DefPercent;

        public float MdefPercent;

        /// <summary>Flat HP/SP per regen tick (Iron Constitution, Mana Focus).</summary>
        public int HpRegenFlat;

        public int SpRegenFlat;

        /// <summary>Extra basic-attack reach in meters (Vulture Eye).</summary>
        public float AttackRange;

        /// <summary>Percent damage taken; negative = less (Freyja's Shield, Divine Bulwark).</summary>
        public float DamageTakenPercent;

        /// <summary>Percent chance to block a physical melee hit outright (Guardian's Oath).</summary>
        public float BlockChance;

        /// <summary>+% poise damage dealt (Thurisaz runestone +200 = x3).</summary>
        public float PoiseDamagePercent;

        public float MaxPoisePercent;

        /// <summary>Can't be staggered (Endure). Hyper-armor implies it.</summary>
        public bool StaggerImmune;

        /// <summary>Walk at this percent of normal speed while casting (Free Cast). 0 = rooted while casting.</summary>
        public float CastMoveSpeedPercent;

        /// <summary>Percent of CRIT chance (Fenrir's Wolf Form doubles it: +100).</summary>
        public float CritPercent;

        /// <summary>+% critical damage on top of the 140% (Dire Wolf Card +20).</summary>
        public float CritDamagePercent;

        /// <summary>Percent of physical damage dealt healed as HP / restored as SP (Crypt Bat, Corrupted Einherjar).</summary>
        public float LifeStealPercent;

        public float SpStealPercent;

        /// <summary>SP drained from the target on each hit (Abyssal Leech).</summary>
        public int SpDrainOnHit;

        /// <summary>Percent skill cooldown (Naga Queen -10).</summary>
        public float CooldownPercent;

        /// <summary>Percent of physical melee damage taken reflected to the attacker (Draugr Warlord).</summary>
        public float ReflectMeleePercent;

        /// <summary>Percent of magic damage taken reflected to the caster (Hagalaz).</summary>
        public float ReflectMagicPercent;

        /// <summary>Percent of the target's DEF / MDEF ignored (Jormungandr's Brood 40, Frost Wyrm 10 MDEF).</summary>
        public float DefBypassPercent;

        public float MdefBypassPercent;

        /// <summary>Extra weight capacity (the Merchant Pushcart: +8,000).</summary>
        public int WeightCapacity;

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
            AddScaled(other, 1f);
        }

        /// <summary>
        /// Adds <paramref name="other"/> <paramref name="times"/> times over (per skill level / per stack).
        /// Flat values and percents scale; a multiplier m scales as 1 + (m - 1) x times; overrides and flags
        /// apply once when <paramref name="times"/> &gt; 0.
        /// </summary>
        public void AddScaled(StatModifiers other, float times)
        {
            if (other == null || times == 0f)
            {
                return;
            }

            EnsureStatArray();
            for (int i = 0; i < StatTypes.Count; i++)
            {
                FlatStats[i] += Scale(other.GetStat((StatType)i), times);
            }

            Atk += Scale(other.Atk, times);
            Matk += Scale(other.Matk, times);
            Def += Scale(other.Def, times);
            Mdef += Scale(other.Mdef, times);
            Hit += Scale(other.Hit, times);
            Flee += Scale(other.Flee, times);
            Crit += other.Crit * times;
            HitPercent += other.HitPercent * times;
            FleePercent += other.FleePercent * times;
            MaxHp += Scale(other.MaxHp, times);
            MaxSp += Scale(other.MaxSp, times);
            MaxHpPercent += other.MaxHpPercent * times;
            MaxSpPercent += other.MaxSpPercent * times;
            MaxHpMultiplier *= 1f + (other.MaxHpMultiplier - 1f) * times;
            AspdFlat += other.AspdFlat * times;
            AspdPercent += other.AspdPercent * times;
            CastTimePercent += other.CastTimePercent * times;
            MoveSpeedPercent += other.MoveSpeedPercent * times;
            PhysicalDamagePercent += other.PhysicalDamagePercent * times;
            MagicDamagePercent += other.MagicDamagePercent * times;
            DefPercent += other.DefPercent * times;
            MdefPercent += other.MdefPercent * times;
            HpRegenFlat += Scale(other.HpRegenFlat, times);
            SpRegenFlat += Scale(other.SpRegenFlat, times);
            WeightCapacity += Scale(other.WeightCapacity, times);
            AttackRange += other.AttackRange * times;
            DamageTakenPercent += other.DamageTakenPercent * times;
            BlockChance += other.BlockChance * times;
            PoiseDamagePercent += other.PoiseDamagePercent * times;
            MaxPoisePercent += other.MaxPoisePercent * times;
            CastMoveSpeedPercent += other.CastMoveSpeedPercent * times;
            CritPercent += other.CritPercent * times;
            CritDamagePercent += other.CritDamagePercent * times;
            LifeStealPercent += other.LifeStealPercent * times;
            SpStealPercent += other.SpStealPercent * times;
            SpDrainOnHit += Scale(other.SpDrainOnHit, times);
            CooldownPercent += other.CooldownPercent * times;
            ReflectMeleePercent += other.ReflectMeleePercent * times;
            ReflectMagicPercent += other.ReflectMagicPercent * times;
            DefBypassPercent += other.DefBypassPercent * times;
            MdefBypassPercent += other.MdefBypassPercent * times;
            if (times > 0f)
            {
                AspdOverride = Math.Max(AspdOverride, other.AspdOverride);
                CancelAttackRecovery |= other.CancelAttackRecovery;
                ItemsLocked |= other.ItemsLocked;
                UninterruptibleCasting |= other.UninterruptibleCasting;
                HyperArmor |= other.HyperArmor;
                StaggerImmune |= other.StaggerImmune;
            }
        }

        public void Clear()
        {
            FlatStats = new int[StatTypes.Count];
            Atk = Matk = Def = Mdef = Hit = Flee = 0;
            Crit = HitPercent = FleePercent = 0f;
            MaxHp = MaxSp = 0;
            MaxHpPercent = MaxSpPercent = 0f;
            MaxHpMultiplier = 1f;
            AspdFlat = AspdPercent = AspdOverride = 0f;
            CastTimePercent = MoveSpeedPercent = 0f;
            PhysicalDamagePercent = MagicDamagePercent = 0f;
            CancelAttackRecovery = ItemsLocked = UninterruptibleCasting = HyperArmor = StaggerImmune = false;
            DefPercent = MdefPercent = 0f;
            HpRegenFlat = SpRegenFlat = 0;
            AttackRange = DamageTakenPercent = BlockChance = PoiseDamagePercent = MaxPoisePercent = CastMoveSpeedPercent = 0f;
            CritPercent = CritDamagePercent = LifeStealPercent = SpStealPercent = CooldownPercent = 0f;
            ReflectMeleePercent = ReflectMagicPercent = DefBypassPercent = MdefBypassPercent = 0f;
            SpDrainOnHit = 0;
        }

        public StatModifiers Clone()
        {
            var copy = new StatModifiers();
            copy.Add(this);
            return copy;
        }

        private static int Scale(int value, float times)
        {
            return (int)Math.Round(value * times, MidpointRounding.AwayFromZero);
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
