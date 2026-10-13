using Runeheir.Mercenaries;
using Runeheir.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Runeheir.UI
{
    /// <summary>
    /// R: the companions window. The mercenary (Ragnarok's Ctrl+R): who's under contract, their HP, the time left, kills,
    /// Dismiss. The homunculus (Ragnarok's Alt+R): its kind, level, HP and EXP, and Call / Rest.
    /// </summary>
    public sealed class MercenaryWindow
    {
        private readonly PlayerCharacter _player;
        private readonly Text _info;
        private readonly Image _hp;
        private readonly Image _time;
        private readonly Button _dismiss;
        private readonly Text _homunculusInfo;
        private readonly Image _homunculusHp;
        private readonly Image _homunculusExp;
        private readonly Button _homunculusCall;
        private float _nextRefresh;
        private bool _wasHired;

        public MercenaryWindow(HudController hud, PlayerCharacter player)
        {
            _player = player;
            Window = UIWindow.Create(hud.Canvas.transform, "Companions", 40f, 160f, 300f, 400f);
            _info = UIFactory.CreateText(Window.Content, string.Empty, 13, UITheme.Text, TextAnchor.UpperLeft);
            _info.rectTransform.SetRect(0f, 0f, 276f, 92f);
            _hp = Bar("HP", 96f, UITheme.Hp);
            _time = Bar("Contract", 124f, new Color(0.85f, 0.7f, 0.35f));
            _dismiss = UIFactory.CreateButton(Window.Content, "Dismiss", () => _player.DismissMercenary(), 15);
            _dismiss.GetComponent<RectTransform>().SetRect(0f, 156f, 276f, 34f);

            _homunculusInfo = UIFactory.CreateText(Window.Content, string.Empty, 13, UITheme.Text, TextAnchor.UpperLeft);
            _homunculusInfo.rectTransform.SetRect(0f, 200f, 276f, 62f);
            _homunculusHp = Bar("HP", 264f, UITheme.Hp);
            _homunculusExp = Bar("EXP", 290f, new Color(0.45f, 0.65f, 0.95f));
            _homunculusCall = UIFactory.CreateButton(Window.Content, "Call / Rest", () => _player.WeaveOrCallHomunculus(), 15);
            _homunculusCall.GetComponent<RectTransform>().SetRect(0f, 320f, 276f, 34f);
            Window.VisibilityChanged += Refresh;
            Window.Hide();
        }

        public UIWindow Window { get; }

        public void Toggle()
        {
            if (Window.IsOpen)
            {
                Window.Hide();
            }
            else
            {
                Window.Show();
                Refresh();
            }
        }

        public void Tick()
        {
            // A fresh contract opens the window once, so the new hire is easy to see.
            bool hired = MercenaryCatalog.Active(_player.Record) != null;
            if (hired && !_wasHired && !Window.IsOpen)
            {
                Window.Show();
            }

            _wasHired = hired;
            if (Window.IsOpen && Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + 0.25f;
                Refresh();
            }
        }

        private Image Bar(string label, float y, Color color)
        {
            var text = UIFactory.CreateText(Window.Content, label, 12, UITheme.TextDim, TextAnchor.MiddleLeft);
            text.rectTransform.SetRect(0f, y, 70f, 22f);
            var back = UIFactory.CreatePanel(Window.Content, label + "Back", new Color(0f, 0f, 0f, 0.45f));
            back.rectTransform.SetRect(72f, y + 4f, 204f, 14f);
            var fill = UIFactory.CreatePanel(back.transform, label + "Fill", color);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;
            return fill;
        }

        private void Refresh()
        {
            if (!Window.IsOpen)
            {
                return;
            }

            RefreshHomunculus();

            var contract = MercenaryCatalog.Active(_player.Record);
            var mercenary = contract != null ? MercenaryCatalog.Get(contract.MercenaryId) : null;
            _dismiss.gameObject.SetActive(mercenary != null);
            if (mercenary == null)
            {
                _info.text = "<color=#9AA8BC>No one is under contract.\n\nThrand at Vigrid Haven's Hirð Hall sells contract scrolls: " +
                             "bowmen, spearmen and huscarls, grades 1 to 10. Use one to hire a mercenary for 30 minutes.</color>";
                SetBar(_hp, 0f);
                SetBar(_time, 0f);
                return;
            }

            var body = _player.Mercenary;
            var monster = mercenary.Monster;
            string role = mercenary.Role != null ? $"{mercenary.Role}, grade {mercenary.Grade}" : "Monster contract";
            string hp = body != null ? $"{body.Hp:N0} / {body.MaxHp:N0}" : "waiting for you";
            _info.text = $"<b><color=#EBC466>{monster.Name}</color></b>   <color=#9AA8BC>Lv {monster.Level}</color>\n" +
                         $"<color=#9AA8BC>{role}</color>\n" +
                         $"HP {hp}   Kills {contract.Kills}\n" +
                         $"Time left {MercenaryCatalog.TimeLeftText(contract)}";
            SetBar(_hp, body != null && body.MaxHp > 0 ? body.Hp / (float)body.MaxHp : 0f);
            SetBar(_time, contract.SecondsLeft / MercenaryCatalog.ContractSeconds);
        }

        private void RefreshHomunculus()
        {
            var state = Homunculi.HomunculusRules.Of(_player.Record);
            _homunculusCall.gameObject.SetActive(state != null && state.RestSeconds <= 0f);
            if (state == null)
            {
                _homunculusInfo.text = "<b>Homunculus</b>\n<color=#9AA8BC>A Brewmaster weaves one from an Embryo with Weave Life.</color>";
                SetBar(_homunculusHp, 0f);
                SetBar(_homunculusExp, 0f);
                return;
            }

            var kind = Homunculi.HomunculusRules.Get(state.Kind);
            var body = _player.HomunculusBody;
            string status = state.RestSeconds > 0f ? $"<color=#E74C3C>fallen · {Mathf.CeilToInt(state.RestSeconds)} s</color>"
                : body != null ? $"HP {body.Hp:N0} / {body.MaxHp:N0}" : "<color=#9AA8BC>resting</color>";
            _homunculusInfo.text = $"<b><color=#82E0AA>{state.Name}</color></b> {kind.Title}   <color=#9AA8BC>Lv {state.Level}</color>\n{status}";
            SetBar(_homunculusHp, body != null && body.MaxHp > 0 ? body.Hp / (float)body.MaxHp : 0f);
            SetBar(_homunculusExp, state.Exp / (float)Mathf.Max(1, Homunculi.HomunculusRules.ExpToNext(state.Level)));
        }

        private static void SetBar(Image fill, float fraction)
        {
            fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
        }
    }
}
