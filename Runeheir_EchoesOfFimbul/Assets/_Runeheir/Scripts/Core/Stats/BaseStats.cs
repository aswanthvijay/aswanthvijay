using System;

namespace Runeheir.Stats
{
    /// <summary>
    /// Allocated (base) STR..LUK values. Serializable with JsonUtility and the Unity inspector.
    /// Bonuses from gear/buffs are NOT stored here; see <see cref="StatModifiers"/>.
    /// </summary>
    [Serializable]
    public sealed class BaseStats
    {
        public int Str = 1;
        public int Agi = 1;
        public int Vit = 1;
        public int Int = 1;
        public int Dex = 1;
        public int Luk = 1;

        public int this[StatType stat]
        {
            get
            {
                switch (stat)
                {
                    case StatType.Str: return Str;
                    case StatType.Agi: return Agi;
                    case StatType.Vit: return Vit;
                    case StatType.Int: return Int;
                    case StatType.Dex: return Dex;
                    case StatType.Luk: return Luk;
                    default: throw new ArgumentOutOfRangeException(nameof(stat), stat, null);
                }
            }
            set
            {
                switch (stat)
                {
                    case StatType.Str: Str = value; break;
                    case StatType.Agi: Agi = value; break;
                    case StatType.Vit: Vit = value; break;
                    case StatType.Int: Int = value; break;
                    case StatType.Dex: Dex = value; break;
                    case StatType.Luk: Luk = value; break;
                    default: throw new ArgumentOutOfRangeException(nameof(stat), stat, null);
                }
            }
        }

        public BaseStats Clone()
        {
            return (BaseStats)MemberwiseClone();
        }

        public void SetAll(int value)
        {
            foreach (var stat in StatTypes.All)
            {
                this[stat] = value;
            }
        }

        public override string ToString()
        {
            return $"STR {Str} / AGI {Agi} / VIT {Vit} / INT {Int} / DEX {Dex} / LUK {Luk}";
        }
    }
}
