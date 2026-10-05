using System;
using Runeheir.Combat;
using UnityEngine;

namespace Runeheir.Visuals
{
    /// <summary>
    /// Fire-and-forget overhead messages ("Base Level Up!", "Bash!!", "Blocked"). The world UI layer
    /// listens and draws them, so gameplay code never references UI types.
    /// </summary>
    public static class WorldFeedback
    {
        /// <summary>(entity, text, color).</summary>
        public static event Action<CombatEntity, string, Color> Announced;

        public static void Announce(CombatEntity entity, string text, Color color)
        {
            if (entity != null && !string.IsNullOrEmpty(text))
            {
                Announced?.Invoke(entity, text, color);
            }
        }
    }
}
