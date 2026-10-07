using System.Text;
using Runeheir.Controls;
using Runeheir.Field;
using Runeheir.Online;
using Runeheir.Player;
using Runeheir.Session;
using Runeheir.Social;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Runeheir.UI
{
    /// <summary>Bottom-left chat/log box. Enter focuses the input; lines starting with @ are GM commands.</summary>
    public sealed class ChatWindow
    {
        private const int VisibleLines = 9;
        private const float ChatWidth = 440f;

        private readonly PlayerCharacter _player;
        private readonly Text _log;
        private readonly InputField _input;
        private readonly StringBuilder _builder = new StringBuilder();
        private int _caretToEndFrames;

        public ChatWindow(HudController hud, PlayerCharacter player)
        {
            _player = player;
            var frame = UIFactory.CreateFramedPanel(hud.Canvas.transform, "Chat", new Color(0.04f, 0.06f, 0.09f, 0.78f));
            // 440 wide keeps the chat clear of the centered hotkey bar down to 5:4 screens (bar's left edge ≈ 470).
            frame.rectTransform.Anchor(Vector2.zero, Vector2.zero, new Vector2(12f, 12f), new Vector2(ChatWidth, 230f));

            // Newest line sits at the bottom; older lines overflow upward and are clipped by the viewport.
            var viewport = UIFactory.CreateRect("Viewport", frame.transform);
            viewport.Stretch(10f, 8f, 10f, 44f);
            viewport.gameObject.AddComponent<RectMask2D>();
            _log = UIFactory.CreateText(viewport, string.Empty, 15, UITheme.Text, TextAnchor.LowerLeft);
            _log.rectTransform.Stretch();
            _log.verticalOverflow = VerticalWrapMode.Overflow;
            UIFactory.AddShadow(_log, 1f);

            _input = UIFactory.CreateInputField(frame.transform, "Press Enter to chat · @help for commands", characterLimit: 200, fontSize: 15);
            _input.GetComponent<RectTransform>().Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(ChatWidth - 20f, 30f));
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

        /// <summary>Opens the chat box with <paramref name="text"/> typed in (a whisper to someone you clicked).</summary>
        public void Prefill(string text)
        {
            Focus();
            _input.text = text ?? string.Empty;
            _caretToEndFrames = 2; // focusing selects everything a frame later: move the caret after that
        }

        public void Tick()
        {
            if (_caretToEndFrames > 0 && --_caretToEndFrames == 0 && _input.isFocused)
            {
                _input.MoveTextEnd(false);
            }
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

            // The log renders rich text: escape everything a player typed, commands included.
            string safe = text.Replace("<", "‹").Replace(">", "›");
            if (text.StartsWith("@"))
            {
                ChatLog.Add(safe, ChatKind.Normal);
                GmCommands.Execute(text, _player);
            }
            else if (OnlineSession.Current != null)
            {
                Send(OnlineSession.Current, text);
            }
            else
            {
                // Offline there's nobody to hear it: a local echo.
                ChatLog.Add($"{_player.DisplayName} : {safe}");
            }
        }

        /// <summary>Online chat (Phase 6): map, %party, $guild, /sh shout, /w whisper, /r reply. The realm echoes it back.</summary>
        private static void Send(IOnlineSession online, string text)
        {
            var input = ChatRules.Parse(text, online.Social?.State.LastWhisperFrom);
            if (input.IsCommand)
            {
                ChatLog.Error("Unknown chat command. Try %party, $guild, /sh shout, /w Name message, /r reply.");
                return;
            }

            if (input.IsEmpty)
            {
                return;
            }

            if (input.Channel == ChatChannel.Whisper && string.IsNullOrEmpty(input.Target))
            {
                ChatLog.Error("Whisper whom? /w Name message (use quotes for names with spaces).");
                return;
            }

            online.SendChat(input);
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
