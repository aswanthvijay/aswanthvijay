using System;
using System.Collections.Generic;
using Runeheir.Accounts;
using Runeheir.Controls;
using Runeheir.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Runeheir.FrontEnd
{
    /// <summary>Realm list shown after login (Ragnarok "select server").</summary>
    public sealed class ServerSelectScreen : FrontEndScreen
    {
        private const float Width = 560f;

        private readonly RectTransform _list;
        private readonly Text _status;
        private readonly List<GameObject> _entries = new List<GameObject>();
        private IReadOnlyList<ServerInfo> _servers = Array.Empty<ServerInfo>();

        public ServerSelectScreen(FrontEndController context) : base(context, "ServerSelectScreen")
        {
            var panel = UIFactory.CreateFramedPanel(Frame, "ServerPanel", UITheme.WindowBg);
            panel.rectTransform.SetRect((1920f - Width) / 2f, 400f, Width, 400f);
            var content = UIFactory.CreateRect("Content", panel.transform);
            content.Stretch(24f, 18f, 24f, 18f);

            CreateHeading(content, "Select Realm", 0f, Width - 48f);
            _list = UIFactory.CreateRect("List", content);
            _list.SetRect(0f, 50f, Width - 48f, 220f);
            _status = CreateStatus(content, 0f, 274f, Width - 48f);

            var back = UIFactory.CreateButton(content, "Back", OnBack, 16);
            back.GetComponent<RectTransform>().SetRect((Width - 48f) / 2f - 80f, 310f, 160f, 38f);
        }

        public override void HandleKeys()
        {
            if (GameInput.KeyDown(GameKey.Escape))
            {
                OnBack();
                return;
            }

            if (GameInput.KeyDown(GameKey.Enter))
            {
                foreach (var server in _servers)
                {
                    if (server.Online)
                    {
                        Choose(server);
                        return;
                    }
                }
            }
        }

        protected override async void OnShow()
        {
            foreach (var entry in _entries)
            {
                UnityEngine.Object.Destroy(entry);
            }

            _entries.Clear();
            SetStatus(_status, "Fetching realms...");
            int visit = Visit;
            Busy = true;
            try
            {
                _servers = await Context.Session.Accounts.GetServersAsync();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (IsCurrent(visit))
                {
                    Busy = false;
                    SetStatus(_status, "Could not load realms.", error: true);
                }

                return;
            }

            // Left and came back before the reply arrived: that visit builds its own list.
            if (!IsCurrent(visit))
            {
                return;
            }

            Busy = false;

            for (int i = 0; i < _servers.Count; i++)
            {
                _entries.Add(CreateEntry(_servers[i], i * 100f));
            }

            SetStatus(_status, $"Logged in as {Context.Session.Username}. Choose a realm.");
        }

        private GameObject CreateEntry(ServerInfo server, float y)
        {
            var button = UIFactory.CreateButton(_list, string.Empty, () => Choose(server), 16);
            button.GetComponent<RectTransform>().SetRect(0f, y, Width - 48f, 88f);
            button.interactable = server.Online;
            UIFactory.SetButtonLabel(button, string.Empty);

            var name = UIFactory.CreateText(button.transform, server.Name, 22, server.Online ? UITheme.Gold : UITheme.TextDim, TextAnchor.UpperLeft, FontStyle.Bold);
            name.rectTransform.Stretch(16f, 10f, 16f, 40f);
            var description = UIFactory.CreateText(button.transform, server.Description, 14, UITheme.TextDim, TextAnchor.LowerLeft);
            description.rectTransform.Stretch(16f, 40f, 140f, 12f);
            var population = UIFactory.CreateText(button.transform, server.Online ? $"{server.Population} characters" : "Offline",
                14, server.Online ? UITheme.Success : UITheme.Error, TextAnchor.MiddleRight, FontStyle.Bold);
            population.rectTransform.Stretch(16f, 10f, 16f, 10f);
            return button.gameObject;
        }

        private void Choose(ServerInfo server)
        {
            if (!server.Online)
            {
                return;
            }

            Context.Session.Server = server;
            Context.ShowCharacterSelect();
        }

        private void OnBack()
        {
            Context.Session.Logout();
            Context.ShowLogin();
        }
    }
}
