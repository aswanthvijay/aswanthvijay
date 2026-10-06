using System.Collections.Generic;
using UnityEngine;

namespace Runeheir.UI
{
    /// <summary>A name floating over a world object that isn't a combatant or an NPC: warp portals, boss tombstones.</summary>
    public sealed class WorldLabel : MonoBehaviour
    {
        private static readonly List<WorldLabel> Registry = new List<WorldLabel>();

        public static IReadOnlyList<WorldLabel> All => Registry;

        /// <summary>Rich text shown over the object; change it at any time.</summary>
        public string Text;

        public Color Color = new Color(0.75f, 0.9f, 1f);

        public int FontSize = 15;

        /// <summary>Meters above the object's pivot.</summary>
        public float Height = 2f;

        public static WorldLabel Attach(GameObject target, string text, Color color, float height, int fontSize = 15)
        {
            var label = target.AddComponent<WorldLabel>();
            label.Text = text;
            label.Color = color;
            label.Height = height;
            label.FontSize = fontSize;
            return label;
        }

        private void OnEnable()
        {
            Registry.Add(this);
        }

        private void OnDisable()
        {
            Registry.Remove(this);
        }
    }
}
