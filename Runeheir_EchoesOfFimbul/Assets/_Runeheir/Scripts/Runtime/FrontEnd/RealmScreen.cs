using Runeheir.Accounts;
using Runeheir.Controls;
using Runeheir.Online;
using Runeheir.Social;
using Runeheir.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Runeheir.FrontEnd
{
    /// <summary>
    /// Phase 6 first screen: play offline (accounts on this PC), host a realm on this PC for friends, or join someone's
    /// realm by address. The login screen then talks to whichever was chosen.
    /// </summary>
    public sealed class RealmScreen : FrontEndScreen
    {
        private const string AddressKey = "runeheir.realm_address";
        private const string PortKey = "runeheir.realm_port";
        private const float Width = 620f;

        private readonly InputField _port;
        private readonly InputField _address;
        private readonly Button _host;
        private readonly Button _join;
        private readonly Text _status;

        public RealmScreen(FrontEndController context) : base(context, "RealmScreen")
        {
            var panel = UIFactory.CreateFramedPanel(Frame, "RealmPanel", UITheme.WindowBg);
            panel.rectTransform.SetRect((1920f - Width) / 2f, 360f, Width, 520f);
            var content = UIFactory.CreateRect("Content", panel.transform);
            content.Stretch(24f, 18f, 24f, 18f);
            float inner = Width - 48f;

            CreateHeading(content, "Choose a Realm", 0f, inner);

            // Offline.
            Section(content, "Offline", "Single-player. Accounts and characters stay on this PC.", 46f, inner);
            var offline = UIFactory.CreateButton(content, "Play Offline", PlayOffline, 18);
            offline.GetComponent<RectTransform>().SetRect(inner - 200f, 52f, 200f, 40f);

            // Host.
            Section(content, "Host a Realm", "Run a realm on this PC; friends join with your address. Characters live here.", 126f, inner);
            Label(content, "Port", 176f);
            _port = UIFactory.CreateInputField(content, RealmConfig.DefaultPort.ToString(), characterLimit: 5, fontSize: 16);
            _port.contentType = InputField.ContentType.IntegerNumber;
            _port.GetComponent<RectTransform>().SetRect(90f, 172f, 120f, 34f);
            _host = UIFactory.CreateButton(content, "Host", Host, 18);
            _host.GetComponent<RectTransform>().SetRect(inner - 200f, 168f, 200f, 40f);

            // Join.
            Section(content, "Join a Realm", "Connect to a friend's realm or a dedicated server (address or address:port).", 236f, inner);
            Label(content, "Address", 286f);
            _address = UIFactory.CreateInputField(content, "192.168.1.20 or realm.example:7777", characterLimit: 120, fontSize: 16);
            _address.GetComponent<RectTransform>().SetRect(90f, 282f, inner - 310f, 34f);
            _join = UIFactory.CreateButton(content, "Join", Join, 18);
            _join.GetComponent<RectTransform>().SetRect(inner - 200f, 278f, 200f, 40f);

            _status = CreateStatus(content, 0f, 350f, inner);
            _status.rectTransform.SetRect(0f, 340f, inner, 52f);
            _status.horizontalOverflow = HorizontalWrapMode.Wrap;

            var exit = UIFactory.CreateButton(content, "Exit", Session.GameSession.QuitGame, 16);
            exit.GetComponent<RectTransform>().SetRect(inner / 2f - 70f, 420f, 140f, 36f);
        }

        public override void HandleKeys()
        {
            if (Busy)
            {
                return;
            }

            if (GameInput.KeyDown(GameKey.Enter))
            {
                if (_address.isFocused)
                {
                    Join();
                }
                else if (_port.isFocused)
                {
                    Host();
                }
                else
                {
                    PlayOffline();
                }
            }
        }

        protected override void OnShow()
        {
            _address.text = PlayerPrefs.GetString(AddressKey, string.Empty);
            _port.text = PlayerPrefs.GetInt(PortKey, RealmConfig.DefaultPort).ToString();
            bool available = OnlineSession.Launcher != null;
            _host.interactable = available;
            _join.interactable = available;
            string notice = OnlineSession.Launcher?.TakeNotice();
            if (!string.IsNullOrEmpty(notice))
            {
                SetStatus(_status, notice, error: true);
                return;
            }

            SetStatus(_status, available
                ? "Offline for solo play, or Host / Join to play together (Mirror networking, Phase 6 alpha)."
                : "This build has no networking: offline only.", error: !available);
        }

        private void PlayOffline()
        {
            if (Busy)
            {
                return;
            }

            OnlineSession.Launcher?.Shutdown();
            if (!(Context.Session.Accounts is LocalAccountService))
            {
                Context.Session.Accounts = null; // back to the accounts on this PC
            }

            Context.ShowLogin();
        }

        private void Host()
        {
            var launcher = OnlineSession.Launcher;
            if (Busy || launcher == null)
            {
                return;
            }

            if (!ushort.TryParse(_port.text.Trim(), out ushort port) || port == 0)
            {
                SetStatus(_status, "The port must be a number from 1 to 65535.", error: true);
                return;
            }

            PlayerPrefs.SetInt(PortKey, port);
            PlayerPrefs.Save();
            Begin($"Starting your realm on port {port}...");
            int visit = Visit;
            launcher.Host(port, (ok, error) => Finish(visit, ok, error));
        }

        private void Join()
        {
            var launcher = OnlineSession.Launcher;
            if (Busy || launcher == null)
            {
                return;
            }

            if (!RealmConfig.TryParseAddress(_address.text, out string host, out ushort port, out string error))
            {
                SetStatus(_status, error, error: true);
                return;
            }

            PlayerPrefs.SetString(AddressKey, _address.text.Trim());
            PlayerPrefs.Save();
            Begin($"Connecting to {host}:{port}...");
            int visit = Visit;
            launcher.Join(host, port, (ok, message) => Finish(visit, ok, message));
        }

        private void Begin(string message)
        {
            Busy = true;
            SetStatus(_status, message);
        }

        private void Finish(int visit, bool ok, string error)
        {
            if (!IsCurrent(visit))
            {
                return;
            }

            Busy = false;
            var launcher = OnlineSession.Launcher;
            if (!ok || launcher == null || launcher.Accounts == null)
            {
                SetStatus(_status, string.IsNullOrEmpty(error) ? "Could not reach the realm." : error, error: true);
                return;
            }

            Context.Session.Accounts = launcher.Accounts;
            Context.ShowLogin();
        }

        private static void Section(Transform parent, string title, string description, float y, float width)
        {
            var heading = UIFactory.CreateText(parent, title, 18, UITheme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            heading.rectTransform.SetRect(0f, y, width - 220f, 24f);
            var text = UIFactory.CreateText(parent, description, 13, UITheme.TextDim, TextAnchor.UpperLeft);
            text.rectTransform.SetRect(0f, y + 24f, width - 220f, 36f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        private static void Label(Transform parent, string text, float y)
        {
            var label = UIFactory.CreateText(parent, text, 15, UITheme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            label.rectTransform.SetRect(0f, y, 86f, 28f);
        }
    }
}
