using System;

namespace Runeheir.Combat
{
    /// <summary>
    /// GDD §4 VIT "Stagger / Poise resilience", §7 Thurisaz "triples poise stagger damage", §3 Rage of Thor
    /// "uninterruptible hyper-armor". Every hit chips poise; at zero the target is staggered (a short
    /// incapacitate that cancels its swing or cast), then gets its poise back and a moment of immunity.
    /// </summary>
    public static class PoiseRules
    {
        public const float StaggerSeconds = 0.7f;

        /// <summary>After a stagger the target can't be staggered again for this long (no stun-lock).</summary>
        public const float ImmunitySeconds = 2f;

        /// <summary>Poise refills completely after this long without taking poise damage.</summary>
        public const float RecoverDelaySeconds = 2.5f;

        /// <summary>Base poise damage of one hit of a spell (scaled by the skill's poise multiplier).</summary>
        public const float MagicPoiseDamage = 10f;

        public const float CriticalPoiseMultiplier = 1.5f;

        /// <summary>Players: 40 + VIT x 0.6 + Base Level x 0.2 (VIT 100, Lv 99 ≈ 120).</summary>
        public static float PlayerMaxPoise(int vit, int baseLevel)
        {
            return 40f + Math.Max(0, vit) * 0.6f + Math.Max(1, baseLevel) * 0.2f;
        }

        /// <summary>Monsters: by size (Small 30, Medium 60, Large 120) + Level / 2.</summary>
        public static float MonsterMaxPoise(Size size, int level)
        {
            float bySize = size == Size.Small ? 30f : size == Size.Medium ? 60f : 120f;
            return bySize + Math.Max(1, level) * 0.5f;
        }

        /// <summary>A monster's basic hit: 10 + Level / 10, x1.5 for Large monsters.</summary>
        public static float MonsterPoiseDamage(Size size, int level)
        {
            float damage = 10f + Math.Max(1, level) / 10f;
            return size == Size.Large ? damage * 1.5f : damage;
        }
    }

    /// <summary>One combatant's poise. Time is passed in (seconds) so it runs on a server unchanged.</summary>
    public sealed class PoiseMeter
    {
        private double _lastDamageAt = double.NegativeInfinity;
        private double _immuneUntil = double.NegativeInfinity;

        public PoiseMeter(float max)
        {
            Max = Math.Max(1f, max);
            Current = Max;
        }

        public float Max { get; private set; }

        public float Current { get; private set; }

        public float Fraction => Current / Max;

        /// <summary>Changes the pool size (VIT/level/buffs changed) keeping the current fraction.</summary>
        public void SetMax(float max)
        {
            float fraction = Fraction;
            Max = Math.Max(1f, max);
            Current = Max * fraction;
        }

        public bool IsImmune(double now)
        {
            return now < _immuneUntil;
        }

        /// <summary>
        /// Applies poise damage. Returns true when this hit breaks poise (the caller staggers the target).
        /// <paramref name="canStagger"/> = false (hyper-armor, Endure) still records the hit but never breaks.
        /// </summary>
        public bool Apply(float amount, double now, bool canStagger)
        {
            if (amount <= 0f || IsImmune(now))
            {
                return false;
            }

            _lastDamageAt = now;
            Current = Math.Max(0f, Current - amount);
            if (Current > 0f || !canStagger)
            {
                if (!canStagger)
                {
                    Current = Math.Max(Current, 1f);
                }

                return false;
            }

            Current = Max;
            _immuneUntil = now + PoiseRules.StaggerSeconds + PoiseRules.ImmunitySeconds;
            return true;
        }

        /// <summary>Refills poise once the target has gone <see cref="PoiseRules.RecoverDelaySeconds"/> without poise damage.</summary>
        public void Tick(double now)
        {
            if (Current < Max && now - _lastDamageAt >= PoiseRules.RecoverDelaySeconds)
            {
                Current = Max;
            }
        }

        public void Reset()
        {
            Current = Max;
            _lastDamageAt = double.NegativeInfinity;
            _immuneUntil = double.NegativeInfinity;
        }
    }
}
