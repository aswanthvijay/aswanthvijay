using System.Collections.Generic;
using Runeheir.Player;
using Runeheir.Visuals;
using UnityEngine;
using UnityEngine.UI;

namespace Runeheir.UI
{
    /// <summary>Active buffs (top-right) with remaining time and charges, Ragnarok status-icon style.</summary>
    public sealed class BuffTray
    {
        private const float Size = 40f;

        private readonly PlayerCharacter _player;
        private readonly RectTransform _root;
        private readonly List<Entry> _entries = new List<Entry>();

        public BuffTray(HudController hud, PlayerCharacter player)
        {
            _player = player;
            _root = UIFactory.CreateRect("Buffs", hud.Canvas.transform);
            _root.Anchor(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -14f), new Vector2(400f, 70f));
            player.Buffs.Changed += Rebuild;
            Rebuild();
        }

        public void Dispose()
        {
            _player.Buffs.Changed -= Rebuild;
        }

        public void Tick()
        {
            double now = Time.timeAsDouble;
            foreach (var entry in _entries)
            {
                double remaining = entry.Buff.Remaining(now);
                string charges = entry.Buff.ChargesLeft > 0 ? $" ×{entry.Buff.ChargesLeft}" : string.Empty;
                entry.Timer.text = (remaining >= 60 ? $"{remaining / 60:0}m" : $"{remaining:0}s") + charges;
            }
        }

        private void Rebuild()
        {
            foreach (var entry in _entries)
            {
                Object.Destroy(entry.Root);
            }

            _entries.Clear();
            var active = _player.Buffs.Active;
            for (int i = 0; i < active.Count; i++)
            {
                var buff = active[i];
                var definition = buff.Definition;
                var icon = UIFactory.CreateIcon(_root, definition.IconLabel, RuntimeMaterials.Hex(definition.IconColorHex), 12);
                icon.rectTransform.Anchor(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-i * (Size + 8f), 0f), new Vector2(Size, Size));
                icon.raycastTarget = true;
                var pointer = icon.gameObject.AddComponent<UIPointerHandler>();
                pointer.PointerEnter = () => UITooltip.Show($"<b><color=#EBC466>{definition.Name}</color></b>\n{definition.Description}");
                pointer.PointerExit = UITooltip.Hide;

                var timer = UIFactory.CreateText(icon.transform, string.Empty, 12, Color.white, TextAnchor.UpperCenter, FontStyle.Bold);
                timer.rectTransform.Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(70f, 18f));
                UIFactory.AddOutline(timer, Color.black, 1f);
                _entries.Add(new Entry { Buff = buff, Root = icon.gameObject, Timer = timer });
            }

            Tick();
        }

        private sealed class Entry
        {
            public Combat.ActiveBuff Buff;
            public GameObject Root;
            public Text Timer;
        }
    }
}
