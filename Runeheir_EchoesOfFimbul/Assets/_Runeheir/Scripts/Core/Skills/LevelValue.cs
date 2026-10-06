using System;

namespace Runeheir.Skills
{
    /// <summary>
    /// A skill number that depends on the skill level: <see cref="Base"/> at Lv 1 plus <see cref="PerLevel"/> for each
    /// level above it, or an explicit per-level table. A plain number converts implicitly (same value at every level).
    /// </summary>
    public readonly struct LevelValue
    {
        public readonly float Base;
        public readonly float PerLevel;
        private readonly float[] _table;

        public LevelValue(float baseValue, float perLevel = 0f)
        {
            Base = baseValue;
            PerLevel = perLevel;
            _table = null;
        }

        private LevelValue(float[] table)
        {
            Base = table.Length > 0 ? table[0] : 0f;
            PerLevel = 0f;
            _table = table;
        }

        /// <summary>Explicit values for Lv 1, 2, 3...; levels past the end use the last value.</summary>
        public static LevelValue Table(params float[] values)
        {
            if (values == null || values.Length == 0)
            {
                throw new ArgumentException("A level table needs at least one value.", nameof(values));
            }

            return new LevelValue((float[])values.Clone());
        }

        public static implicit operator LevelValue(float value)
        {
            return new LevelValue(value);
        }

        public bool IsZero => _table == null ? Base == 0f && PerLevel == 0f : Array.TrueForAll(_table, v => v == 0f);

        public float At(int level)
        {
            level = Math.Max(1, level);
            if (_table != null)
            {
                return _table[Math.Min(level, _table.Length) - 1];
            }

            return Base + PerLevel * (level - 1);
        }

        public int AtInt(int level)
        {
            return (int)Math.Round(At(level), MidpointRounding.AwayFromZero);
        }
    }
}
