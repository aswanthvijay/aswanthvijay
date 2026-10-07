namespace Runeheir.Combat
{
    /// <summary>GDD §8 Combat Matrix: the 10 elements.</summary>
    public enum Element
    {
        Neutral = 0,
        Water = 1,
        Earth = 2,
        Fire = 3,
        Wind = 4,
        Poison = 5,
        Holy = 6,
        Shadow = 7,
        Ghost = 8,
        Undead = 9,
    }

    public enum Race
    {
        Formless = 0,
        Undead = 1,
        Beast = 2,
        Plant = 3,
        Insect = 4,
        Fish = 5,
        Demon = 6,
        DemiHuman = 7,
        Angel = 8,
        Dragon = 9,
    }

    public enum Size
    {
        Small = 0,
        Medium = 1,
        Large = 2,
    }

    public enum Faction
    {
        Player = 0,
        Monster = 1,
        Neutral = 2,
    }

    public static class CombatEnumCounts
    {
        public const int Elements = 10;
        public const int Races = 10;
        public const int Sizes = 3;

        /// <summary>Values of <see cref="StatusEffect"/>, None included.</summary>
        public const int StatusEffects = 13;
    }
}
