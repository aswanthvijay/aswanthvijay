namespace Runeheir.Combat
{
    /// <summary>
    /// Attack element (row) vs. defender element (column), level-1 defender, in percent.
    /// Mirrors the classic pre-renewal table. Negative values (Ragnarok "heals the target")
    /// are treated as 0 damage by <see cref="DamageCalculator"/>.
    /// </summary>
    public static class ElementTable
    {
        // Columns: Neutral, Water, Earth, Fire, Wind, Poison, Holy, Shadow, Ghost, Undead
        private static readonly int[,] Percent =
        {
            /* Neutral */ { 100, 100, 100, 100, 100, 100, 100, 100,  25, 100 },
            /* Water   */ { 100,  25, 100, 150,  50, 100,  75, 100, 100, 100 },
            /* Earth   */ { 100, 100,  25,  50, 150, 100,  75, 100, 100, 100 },
            /* Fire    */ { 100,  50, 150,  25, 100, 100,  75, 100, 100, 125 },
            /* Wind    */ { 100, 150,  50, 100,  25, 100,  75, 100, 100, 100 },
            /* Poison  */ { 100, 100, 125, 125, 125,   0,  75,  50, 100, -25 },
            /* Holy    */ { 100, 100, 100, 100, 100, 100,   0, 125, 100, 150 },
            /* Shadow  */ { 100, 100, 100, 100, 100,  50, 125,   0, 100, -25 },
            /* Ghost   */ {  25, 100, 100, 100, 100, 100,  75,  75, 125, 100 },
            /* Undead  */ { 100, 100, 100, 100, 100,  50, 100,   0, 100,   0 },
        };

        public static int GetPercent(Element attack, Element defend)
        {
            return Percent[(int)attack, (int)defend];
        }

        public static float Multiplier(Element attack, Element defend)
        {
            return GetPercent(attack, defend) / 100f;
        }
    }
}
