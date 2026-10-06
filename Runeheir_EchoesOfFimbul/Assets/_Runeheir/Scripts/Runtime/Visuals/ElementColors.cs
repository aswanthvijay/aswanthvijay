using Runeheir.Combat;
using UnityEngine;

namespace Runeheir.Visuals
{
    /// <summary>One color per element for bolts, telegraphs and skill call-outs.</summary>
    public static class ElementColors
    {
        public static Color Of(Element element)
        {
            switch (element)
            {
                case Element.Water: return new Color(0.45f, 0.78f, 1f);
                case Element.Earth: return new Color(0.78f, 0.6f, 0.35f);
                case Element.Fire: return new Color(1f, 0.5f, 0.15f);
                case Element.Wind: return new Color(0.55f, 0.95f, 0.6f);
                case Element.Poison: return new Color(0.62f, 0.85f, 0.25f);
                case Element.Holy: return new Color(1f, 0.92f, 0.55f);
                case Element.Shadow: return new Color(0.62f, 0.38f, 0.92f);
                case Element.Ghost: return new Color(0.78f, 0.86f, 1f);
                case Element.Undead: return new Color(0.45f, 0.7f, 0.5f);
                default: return new Color(1f, 0.42f, 0.32f);
            }
        }
    }
}
