using System;
using System.Collections.Generic;
using Runeheir.Combat;
using Runeheir.Jobs;
using Runeheir.Online;
using Runeheir.Player;
using Runeheir.Session;
using Runeheir.Social;
using UnityEngine;
using UnityEngine.UI;

namespace Runeheir.UI
{
    /// <summary>
    /// Phase 6 social HUD: the player menu (click another player), invitation prompts, and the party (Z), guild (G),
    /// trade and street stall (V) windows. Offline the windows just say they need a realm.
    /// </summary>
    public sealed class SocialHud
    {
        private readonly HudController _hud;
        private readonly PlayerCharacter _player;
        private readonly UIWindow _menu;
        private readonly Text _menuTitle;
        private readonly List<Button> _menuButtons = new List<Button>();
        private readonly UIWindow _prompt;
        private readonly Text _promptText;
        private Action _promptYes;
        private Action _promptNo;
        private string _promptKey;
        private SocialState _state;

        public SocialHud(HudController hud, PlayerCharacter player)
        {
            _hud = hud;
            _player = player;
            Party = new PartyWindow(hud, this);
            Guild = new GuildWindow(hud, this);
            Trade = new TradeWindow(hud, player, this);
            VendSetup = new VendingSetupWindow(hud, player, this);
            Stall = new StallWindow(hud, player, this);

            _menu = UIWindow.Create(hud.Canvas.transform, "Player", 820f, 300f, 260f, 330f);
            _menu.CenterOnShow = true;
            _menuTitle = UIFactory.CreateText(_menu.Content, string.Empty, 15, UITheme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            _menuTitle.rectTransform.SetRect(0f, 0f, 240f, 40f);
            _menu.Hide();

            _prompt = UIWindow.Create(hud.Canvas.transform, "Request", 760f, 260f, 420f, 180f, closable: false);
            _prompt.CenterOnShow = true;
            _promptText = UIFactory.CreateText(_prompt.Content, string.Empty, 15, UITheme.Text, TextAnchor.MiddleCenter);
            _promptText.rectTransform.SetRect(0f, 0f, 400f, 80f);
            var yes = UIFactory.CreateButton(_prompt.Content, "Accept", () => AnswerPrompt(true), 16);
            yes.GetComponent<RectTransform>().SetRect(60f, 92f, 130f, 38f);
            var no = UIFactory.CreateButton(_prompt.Content, "Decline", () => AnswerPrompt(false), 16);
            no.GetComponent<RectTransform>().SetRect(210f, 92f, 130f, 38f);
            _prompt.Hide();

            RemotePlayer.Clicked += OnPlayerClicked;
        }

        public PartyWindow Party { get; }

        public GuildWindow Guild { get; }

        public TradeWindow Trade { get; }

        public VendingSetupWindow VendSetup { get; }

        public StallWindow Stall { get; }

        /// <summary>The realm's social service, or null offline.</summary>
        public static ISocialService Service => OnlineSession.Current?.Social;

        public static SocialState State => Service?.State;

        /// <summary>Every social window, for Escape handling.</summary>
        public IEnumerable<UIWindow> Windows
        {
            get
            {
                yield return Party.Window;
                yield return Guild.Window;
                yield return Trade.Window;
                yield return VendSetup.Window;
                yield return Stall.Window;
                yield return _menu;
            }
        }

        public void Tick()
        {
            var state = State;
            if (state != _state)
            {
                if (_state != null)
                {
                    _state.Changed -= OnStateChanged;
                }

                _state = state;
                if (_state != null)
                {
                    _state.Changed += OnStateChanged;
                    OnStateChanged();
                }
            }
        }

        public void Dispose()
        {
            RemotePlayer.Clicked -= OnPlayerClicked;
            if (_state != null)
            {
                _state.Changed -= OnStateChanged;
            }

            Trade.Dispose();
            VendSetup.Dispose();
        }

        /// <summary>One line telling the player a feature needs an online realm (null when online).</summary>
        public static bool RequireOnline()
        {
            if (Service != null)
            {
                return true;
            }

            ChatLog.Error("Parties, guilds, trades and stalls need an online realm: Host or Join one from the realm screen.");
            return false;
        }

        private void OnStateChanged()
        {
            var state = _state;
            if (state == null)
            {
                return;
            }

            Party.Refresh();
            Guild.Refresh();
            Trade.Sync();
            VendSetup.Refresh();
            Stall.Sync();

            // One question at a time: trade requests, then party, then guild invitations.
            if (!string.IsNullOrEmpty(state.TradeRequestFrom))
            {
                Ask("trade:" + state.TradeRequestFrom, $"{state.TradeRequestFrom} wants to trade with you.",
                    () => Service?.TradeRespond(true), () => Service?.TradeRespond(false));
            }
            else if (!string.IsNullOrEmpty(state.PartyInviteFrom))
            {
                Ask("party:" + state.PartyInviteId, $"{state.PartyInviteFrom} invites you to the party\n<b>{state.PartyInviteName}</b>.",
                    () => Service?.PartyRespond(true), () => Service?.PartyRespond(false));
            }
            else if (!string.IsNullOrEmpty(state.GuildInviteFrom))
            {
                Ask("guild:" + state.GuildInviteId, $"{state.GuildInviteFrom} invites you to the guild\n<b>{state.GuildInviteName}</b>.",
                    () => Service?.GuildRespond(true), () => Service?.GuildRespond(false));
            }
            else if (_prompt.IsOpen)
            {
                _promptKey = null;
                _prompt.Hide();
            }
        }

        private void Ask(string key, string text, Action yes, Action no)
        {
            if (_promptKey == key && _prompt.IsOpen)
            {
                return;
            }

            _promptKey = key;
            _promptText.text = text;
            _promptYes = yes;
            _promptNo = no;
            _prompt.Show();
        }

        private void AnswerPrompt(bool accept)
        {
            var action = accept ? _promptYes : _promptNo;
            _promptYes = _promptNo = null;
            _promptKey = null;
            _prompt.Hide();
            action?.Invoke();
        }

        // ------------------------------------------------------------ player menu
        private void OnPlayerClicked(RemotePlayer other)
        {
            if (other == null || _player == null || _player.IsDead || Service == null)
            {
                return;
            }

            // Ragnarok: clicking a vendor opens their shop.
            if (other.IsVending)
            {
                Service.VendBrowse(other.DisplayName);
                return;
            }

            foreach (var button in _menuButtons)
            {
                UnityEngine.Object.Destroy(button.gameObject);
            }

            _menuButtons.Clear();
            string name = other.DisplayName;
            _menuTitle.text = $"{name}\n<size=12><color=#9AA8BC>Lv {other.Level} {JobDatabase.Get(other.Job).Name}</color></size>";
            var state = State;
            bool leader = state?.Party != null && state.Party.IsLeader(_player.DisplayName);
            var myRank = state?.Guild?.Member(_player.DisplayName);
            bool officer = myRank != null && GuildRules.CanManage(myRank.Rank);
            AddMenuButton("Trade", () => Service?.TradeRequest(name));
            AddMenuButton(state?.Party == null ? "Invite to party (make one first)" : "Invite to party", () =>
            {
                if (State?.Party == null)
                {
                    Party.Window.Show();
                    ChatLog.System("Name your party first, then invite.");
                    return;
                }

                Service?.PartyInvite(name);
            }, state?.Party == null || leader);
            AddMenuButton("Invite to guild", () => Service?.GuildInvite(name), officer);
            AddMenuButton("Whisper", () => _hud.StartWhisper(name));
            AddMenuButton("Close", null);
            _menu.Show();
        }

        private void AddMenuButton(string label, Action action, bool enabled = true)
        {
            var button = UIFactory.CreateButton(_menu.Content, label, () =>
            {
                _menu.Hide();
                action?.Invoke();
            }, 15);
            button.GetComponent<RectTransform>().SetRect(10f, 46f + _menuButtons.Count * 44f, 220f, 38f);
            button.interactable = enabled;
            _menuButtons.Add(button);
        }
    }
}
