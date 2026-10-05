using System;

namespace Runeheir.Combat
{
    /// <summary>Injectable randomness so combat math is deterministic in tests (and server-authoritative later).</summary>
    public interface IRandomSource
    {
        /// <summary>Uniform value in [0, 1).</summary>
        double NextDouble();
    }

    public sealed class SystemRandomSource : IRandomSource
    {
        private readonly Random _random;

        public SystemRandomSource(int? seed = null)
        {
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public static SystemRandomSource Shared { get; } = new SystemRandomSource();

        public double NextDouble()
        {
            return _random.NextDouble();
        }
    }

    public static class RandomSourceExtensions
    {
        /// <summary>Uniform integer in [min, max].</summary>
        public static int Range(this IRandomSource random, int minInclusive, int maxInclusive)
        {
            if (maxInclusive <= minInclusive)
            {
                return minInclusive;
            }

            int value = minInclusive + (int)(random.NextDouble() * (maxInclusive - minInclusive + 1));
            return Math.Min(value, maxInclusive);
        }

        /// <summary>True with <paramref name="percent"/>% probability.</summary>
        public static bool Chance(this IRandomSource random, double percent)
        {
            return random.NextDouble() * 100.0 < percent;
        }
    }
}
