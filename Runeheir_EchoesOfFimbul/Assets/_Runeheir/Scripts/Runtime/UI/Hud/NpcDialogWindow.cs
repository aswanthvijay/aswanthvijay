using System;
using System.Collections.Generic;
using Runeheir.Field;
using UnityEngine;
using UnityEngine.UI;

namespace Runeheir.UI
{
    /// <summary>Ragnarok-style NPC talk box: the NPC's line and a menu of what it can do for you.</summary>
    public sealed class NpcDialogWindow
    {
        private const float ButtonHeight = 36f;

        private readonly Text _greeting;
        private readonly List<GameObject> _buttons = new List<GameObject>();

        public NpcDialogWindow(HudController hud)
        {
            Window = UIWindow.Create(hud.Canvas.transform, "NPC", 720f, 560f, 480f, 330f);
            _greeting = UIFactory.CreateText(Window.Content, string.Empty, 15, UITheme.Text, TextAnchor.UpperLeft);
            _greeting.rectTransform.SetRect(4f, 0f, 452f, 84f);
            Window.Hide();
        }

        public UIWindow Window { get; }

        public void Open(NpcActor npc, IReadOnlyList<KeyValuePair<string, Action>> options)
        {
            Window.Title.text = $"{npc.DisplayName} · {npc.Title}";
            _greeting.text = $"<color=#AED6F1>[{npc.DisplayName}]</color>\n{npc.Greeting}";
            foreach (var button in _buttons)
            {
                UnityEngine.Object.Destroy(button);
            }

            _buttons.Clear();
            for (int i = 0; i < options.Count; i++)
            {
                var option = options[i];
                var button = UIFactory.CreateButton(Window.Content, option.Key, () =>
                {
                    Window.Hide();
                    option.Value?.Invoke();
                }, 15);
                button.GetComponent<RectTransform>().SetRect(40f, 90f + i * (ButtonHeight + 6f), 376f, ButtonHeight);
                _buttons.Add(button.gameObject);
            }

            Window.Rect.sizeDelta = new Vector2(480f, 90f + options.Count * (ButtonHeight + 6f) + 48f);
            Window.Show();
        }
    }
}
