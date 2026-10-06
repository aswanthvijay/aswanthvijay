using System;
using UnityEngine;
using UnityEngine.UI;

namespace Runeheir.UI
{
    /// <summary>One shared Yes/No dialog ("Sell +7 Iron Claymore?", "Refine past the safe limit?").</summary>
    public sealed class ConfirmDialog
    {
        private readonly Text _message;
        private readonly Button _yes;
        private Action _onYes;

        public ConfirmDialog(HudController hud)
        {
            Window = UIWindow.Create(hud.Canvas.transform, "Confirm", 760f, 400f, 420f, 190f);
            Window.CenterOnShow = true;
            _message = UIFactory.CreateText(Window.Content, string.Empty, 15, UITheme.Text, TextAnchor.MiddleCenter);
            _message.rectTransform.SetRect(0f, 0f, 400f, 92f);
            _yes = UIFactory.CreateButton(Window.Content, "Yes", Accept, 16);
            _yes.GetComponent<RectTransform>().SetRect(60f, 104f, 130f, 38f);
            var no = UIFactory.CreateButton(Window.Content, "No", Window.Hide, 16);
            no.GetComponent<RectTransform>().SetRect(210f, 104f, 130f, 38f);
            Window.Hide();
        }

        public UIWindow Window { get; }

        public void Ask(string message, Action onYes, string yesLabel = "Yes")
        {
            _message.text = message;
            _onYes = onYes;
            UIFactory.SetButtonLabel(_yes, yesLabel);
            Window.Show();
        }

        private void Accept()
        {
            var action = _onYes;
            _onYes = null;
            Window.Hide();
            action?.Invoke();
        }
    }
}
