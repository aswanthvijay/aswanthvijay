using System;

namespace Runeheir.Stats
{
    /// <summary>The six primary attributes (Ragnarok-style STR..LUK).</summary>
    public enum StatType
    {
        Str = 0,
        Agi = 1,
        Vit = 2,
        Int = 3,
        Dex = 4,
        Luk = 5,
    }

    public static class StatTypes
    {
        public const int Count = 6;

        public static readonly StatType[] All =
        {
            StatType.Str, StatType.Agi, StatType.Vit, StatType.Int, StatType.Dex, StatType.Luk,
        };

        public static string Label(StatType stat)
        {
            switch (stat)
            {
                case StatType.Str: return "STR";
                case StatType.Agi: return "AGI";
                case StatType.Vit: return "VIT";
                case StatType.Int: return "INT";
                case StatType.Dex: return "DEX";
                case StatType.Luk: return "LUK";
                default: throw new ArgumentOutOfRangeException(nameof(stat), stat, null);
            }
        }

        /// <summary>Parses "str", "STR", "Str" etc.</summary>
        public static bool TryParse(string text, out StatType stat)
        {
            stat = StatType.Str;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            foreach (var candidate in All)
            {
                if (string.Equals(Label(candidate), text.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    stat = candidate;
                    return true;
                }
            }

            return false;
        }
    }
}
