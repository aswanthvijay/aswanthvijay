using System.Text;
using Runeheir.Controls;
using Runeheir.Field;
using Runeheir.Player;
using Runeheir.Session;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Runeheir.UI
{
    /// <summary>Bottom-left chat/log box. Enter focuses the input; lines starting with @ are GM commands.</summary>
    public sealed class ChatWindow
    {
        private const int VisibleLines = 9;

        private readonly PlayerCharacter _player;
        private readonly Text _log;
        private readonly InputField _input;
        private readonly StringBuilder _builder = new StringBuilder();

        public ChatWindow(HudController hud, PlayerCharacter player)
        {
            _player = player;
            var frame = UIFactory.CreateFramedPanel(hud.Canvas.transform, "Chat", new Color(0.04f, 0.06f, 0.09f, 0.78f));
            frame.rectTransform.Anchor(Vector2.zero, Vector2.zero, new Vector2(12f, 12f), new Vector2(580f, 230f));

            // Newest line sits at the bottom; older lines overflow upward and are clipped by the viewport.
            var viewport = UIFactory.CreateRect("Viewport", frame.transform);
            viewport.Stretch(10f, 8f, 10f, 44f);
            viewport.gameObject.AddComponent<RectMask2D>();
            _log = UIFactory.CreateText(viewport, string.Empty, 15, UITheme.Text, TextAnchor.LowerLeft);
            _log.rectTransform.Stretch();
            _log.verticalOverflow = VerticalWrapMode.Overflow;
            UIFactory.AddShadow(_log, 1f);

            _input = UIFactory.CreateInputField(frame.transform, "Press Enter to chat · @help for commands", characterLimit: 200, fontSize: 15);
            _input.GetComponent<RectTransform>().Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(560f, 30f));
            _input.onEndEdit.AddListener(OnEndEdit);

            ChatLog.LineAdded += OnLine;
            Render();
        }

        /// <summary>Frame in which a message was submitted (so the same Enter press doesn't reopen chat).</summary>
        public int LastSubmitFrame { get; private set; } = -1;

        public bool IsFocused => _input.isFocused;

        public void Dispose()
        {
            ChatLog.LineAdded -= OnLine;
        }

        public void Focus()
        {
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(_input.gameObject);
            }

            _input.ActivateInputField();
        }

        public void Blur()
        {
            _input.DeactivateInputField();
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == _input.gameObject)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private void OnEndEdit(string text)
        {
            if (!GameInput.KeyDown(GameKey.Enter))
            {
                return;
            }

            LastSubmitFrame = Time.frameCount;
            _input.text = string.Empty;
            Blur();

            text = text?.Trim();
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (text.StartsWith("@"))
            {
                ChatLog.Add(text, ChatKind.Normal);
                GmCommands.Execute(text, _player);
            }
            else
            {
                // Local echo until Mirror chat channels exist (Phase 6). Escape rich text from players.
                ChatLog.Add($"{_player.DisplayName} : {text.Replace("<", "‹").Replace(">", "›")}");
            }
        }

        private void OnLine(ChatLine line)
        {
            Render();
        }

        private void Render()
        {
            var lines = ChatLog.Lines;
            int start = Mathf.Max(0, lines.Count - VisibleLines);
            _builder.Clear();
            for (int i = start; i < lines.Count; i++)
            {
                if (_builder.Length > 0)
                {
                    _builder.Append('\n');
                }

                _builder.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(UITheme.ChatColor(lines[i].Kind))).Append('>')
                    .Append(lines[i].Text).Append("</color>");
            }

            _log.text = _builder.ToString();
        }
    }
}
