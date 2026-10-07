using Runeheir.Jobs;
using Runeheir.Social;
using Runeheir.World;
using UnityEngine;
using UnityEngine.UI;

namespace Runeheir.UI
{
    /// <summary>
    /// Party window (Z): create a party, invite by name, see members' level, map and HP, hand over the lead, expel, and
    /// switch EXP between Each Takes and Even Share (members within 30 levels, GDD §8).
    /// </summary>
    public sealed class PartyWindow
    {
        private readonly Text _header;
        private readonly InputField _name;
        private readonly Button _create;
        private readonly Button _invite;
        private readonly Button _share;
        private readonly Button _leader;
        private readonly Button _kick;
        private readonly Button _leave;
        private readonly UIScrollList _list;
        private string _selected;

        public PartyWindow(HudController hud, SocialHud social)
        {
            Window = UIWindow.Create(hud.Canvas.transform, "Party", 470f, 120f, 420f, 520f);
            _header = UIFactory.CreateText(Window.Content, string.Empty, 14, UITheme.Text, TextAnchor.UpperLeft);
            _header.rectTransform.SetRect(0f, 0f, 396f, 40f);
            _list = new UIScrollList(Window.Content, 0f, 44f, 396f, 270f) { EmptyText = "Not in a party." };

            _name = UIFactory.CreateInputField(Window.Content, "party name / player name", characterLimit: 24, fontSize: 14);
            _name.GetComponent<RectTransform>().SetRect(0f, 322f, 396f, 30f);
            _create = Button("Create party", 0f, 358f, () => SocialHud.Service?.PartyCreate(_name.text));
            _invite = Button("Invite", 200f, 358f, () => SocialHud.Service?.PartyInvite(_name.text.Trim()));
            _share = Button("EXP: Each Takes", 0f, 400f, ToggleShare);
            _leader = Button("Make leader", 200f, 400f, () => WithSelected(n => SocialHud.Service?.PartyMakeLeader(n)));
            _kick = Button("Expel", 0f, 442f, () => WithSelected(n => SocialHud.Service?.PartyKick(n)));
            _leave = Button("Leave party", 200f, 442f, () => SocialHud.Service?.PartyLeave());
            Window.VisibilityChanged += Refresh;
            Window.Hide();
        }

        public UIWindow Window { get; }

        public void Toggle()
        {
            if (Window.IsOpen || SocialHud.RequireOnline())
            {
                Window.Toggle();
            }
        }

        public void Refresh()
        {
            if (!Window.IsOpen)
            {
                return;
            }

            var party = SocialHud.State?.Party;
            string me = Player.PlayerCharacter.Local != null ? Player.PlayerCharacter.Local.DisplayName : string.Empty;
            bool leader = party != null && party.IsLeader(me);
            _create.gameObject.SetActive(party == null);
            _invite.gameObject.SetActive(party != null);
            _invite.interactable = leader;
            _share.interactable = leader;
            _leader.interactable = leader && _selected != null;
            _kick.interactable = leader && _selected != null && _selected != me;
            _leave.interactable = party != null;
            _share.gameObject.SetActive(party != null);
            UIFactory.SetButtonLabel(_share, party != null && party.ExpShare == ExpShareMode.EvenShare ? "EXP: Even Share" : "EXP: Each Takes");

            _list.Clear();
            if (party == null)
            {
                _selected = null;
                _header.text = "Type a name and press <b>Create party</b>. Then invite players by name, or click a player and pick Invite.";
                return;
            }

            int gap = PartyRules.LevelGap(party.Members.ConvertAll(m => m.BaseLevel));
            _header.text = $"<b><color=#EBC466>{party.Name}</color></b>  ·  {party.Members.Count}/{PartyRules.MaxMembers}  ·  level gap {gap}" +
                           (gap > PartyRules.EvenShareLevelGap ? " <color=#FF8A80>(too wide for Even Share)</color>" : string.Empty);
            foreach (var member in party.Members)
            {
                string name = member.Name;
                var map = MapCatalog.Get(member.MapId);
                string hp = member.Online && member.MaxHp > 0 ? $"{Mathf.RoundToInt(100f * member.Hp / member.MaxHp)}% HP" : string.Empty;
                string detail = member.Online
                    ? $"Lv {member.BaseLevel} {JobDatabase.Get(member.Job).Name} · {map?.Name ?? "travelling"} · {hp}"
                    : $"Lv {member.BaseLevel} {JobDatabase.Get(member.Job).Name} · offline";
                _list.Add(party.IsLeader(name) ? "★" : "·", member.Online ? UITheme.Gold : UITheme.TextDim, name, detail,
                    () =>
                    {
                        _selected = name;
                        Refresh();
                    }, selected: name == _selected, dimmed: !member.Online);
            }
        }

        private void ToggleShare()
        {
            var party = SocialHud.State?.Party;
            if (party != null)
            {
                SocialHud.Service?.PartySetShare(party.ExpShare == ExpShareMode.EvenShare ? ExpShareMode.EachTakes : ExpShareMode.EvenShare);
            }
        }

        private void WithSelected(System.Action<string> action)
        {
            if (_selected != null)
            {
                action(_selected);
            }
        }

        private Button Button(string label, float x, float y, UnityEngine.Events.UnityAction onClick)
        {
            var button = UIFactory.CreateButton(Window.Content, label, onClick, 14);
            button.GetComponent<RectTransform>().SetRect(x, y, 196f, 36f);
            return button;
        }
    }

    /// <summary>
    /// Guild window (G): found a guild (base level 30, 50,000 zeny), invite, the member list with ranks and who's online,
    /// promote, expel, hand over the lead, the notice board, leave and disband. Guild chat is $text.
    /// </summary>
    public sealed class GuildWindow
    {
        private readonly Text _header;
        private readonly InputField _name;
        private readonly Button _found;
        private readonly Button _invite;
        private readonly Button _notice;
        private readonly Button _rank;
        private readonly Button _expel;
        private readonly Button _handOver;
        private readonly Button _leave;
        private readonly Button _disband;
        private readonly UIScrollList _list;
        private readonly HudController _hud;
        private string _selected;

        public GuildWindow(HudController hud, SocialHud social)
        {
            _hud = hud;
            Window = UIWindow.Create(hud.Canvas.transform, "Guild", 900f, 120f, 440f, 580f);
            _header = UIFactory.CreateText(Window.Content, string.Empty, 14, UITheme.Text, TextAnchor.UpperLeft);
            _header.rectTransform.SetRect(0f, 0f, 416f, 66f);
            _header.horizontalOverflow = HorizontalWrapMode.Wrap;
            _list = new UIScrollList(Window.Content, 0f, 70f, 416f, 270f) { EmptyText = "Not in a guild." };

            _name = UIFactory.CreateInputField(Window.Content, "guild name / player name / notice", characterLimit: GuildRules.MaxNoticeLength, fontSize: 14);
            _name.GetComponent<RectTransform>().SetRect(0f, 348f, 416f, 30f);
            _found = Button($"Found guild ({GuildRules.FoundingFee:N0} z)", 0f, 384f, () => SocialHud.Service?.GuildCreate(_name.text));
            _invite = Button("Invite", 0f, 384f, () => SocialHud.Service?.GuildInvite(_name.text.Trim()));
            _notice = Button("Set notice", 210f, 384f, () => SocialHud.Service?.GuildSetNotice(_name.text));
            _rank = Button("Promote / demote", 0f, 426f, ToggleRank);
            _expel = Button("Expel", 210f, 426f, () => WithSelected(n => SocialHud.Service?.GuildExpel(n)));
            _handOver = Button("Hand over lead", 0f, 468f, () => WithSelected(n => _hud.Confirm($"Make {n} the guild leader?", () => SocialHud.Service?.GuildHandOver(n))));
            _leave = Button("Leave guild", 210f, 468f, () => _hud.Confirm("Leave the guild?", () => SocialHud.Service?.GuildLeave()));
            _disband = Button("Disband guild", 105f, 510f, () => _hud.Confirm("Disband the guild for everyone?", () => SocialHud.Service?.GuildDisband()));
            Window.VisibilityChanged += Refresh;
            Window.Hide();
        }

        public UIWindow Window { get; }

        public void Toggle()
        {
            if (Window.IsOpen || SocialHud.RequireOnline())
            {
                Window.Toggle();
            }
        }

        public void Refresh()
        {
            if (!Window.IsOpen)
            {
                return;
            }

            var guild = SocialHud.State?.Guild;
            string me = Player.PlayerCharacter.Local != null ? Player.PlayerCharacter.Local.DisplayName : string.Empty;
            var mine = guild?.Member(me);
            bool manage = mine != null && GuildRules.CanManage(mine.Rank);
            bool leader = mine != null && mine.Rank == GuildRank.Leader;
            _found.gameObject.SetActive(guild == null);
            foreach (var button in new[] { _invite, _notice, _rank, _expel, _handOver, _leave, _disband })
            {
                button.gameObject.SetActive(guild != null);
            }

            _invite.interactable = manage;
            _notice.interactable = manage;
            _rank.interactable = leader && _selected != null && _selected != me;
            _expel.interactable = manage && _selected != null && _selected != me;
            _handOver.interactable = leader && _selected != null && _selected != me;
            _disband.interactable = leader;

            _list.Clear();
            if (guild == null)
            {
                _selected = null;
                _header.text = $"Found a guild from base level {GuildRules.MinFounderLevel} for {GuildRules.FoundingFee:N0} zeny: type its name and press Found. " +
                               "Or wait for an officer's invitation.";
                return;
            }

            int online = guild.Members.FindAll(m => m.Online).Count;
            _header.text = $"<b><color=#9FE6A0>{guild.Name}</color></b>  ·  {online}/{guild.Members.Count} online  ·  max {GuildRules.MaxMembers}\n" +
                           (string.IsNullOrEmpty(guild.Notice) ? "<color=#9AA8BC>No notice.</color>" : $"<i>{guild.Notice}</i>");
            foreach (var member in guild.Members)
            {
                string name = member.Name;
                string rank = member.Rank == GuildRank.Leader ? "Leader" : member.Rank == GuildRank.Officer ? "Officer" : "Member";
                _list.Add(member.Rank == GuildRank.Leader ? "♛" : member.Rank == GuildRank.Officer ? "◆" : "·",
                    member.Online ? new Color(0.62f, 1f, 0.62f) : UITheme.TextDim, name,
                    $"{rank} · Lv {member.BaseLevel} {JobDatabase.Get(member.Job).Name}{(member.Online ? string.Empty : " · offline")}",
                    () =>
                    {
                        _selected = name;
                        Refresh();
                    }, selected: name == _selected, dimmed: !member.Online);
            }
        }

        private void ToggleRank()
        {
            var member = SocialHud.State?.Guild?.Member(_selected);
            if (member != null)
            {
                SocialHud.Service?.GuildSetRank(member.Name, member.Rank == GuildRank.Officer ? GuildRank.Member : GuildRank.Officer);
            }
        }

        private void WithSelected(System.Action<string> action)
        {
            if (_selected != null)
            {
                action(_selected);
            }
        }

        private Button Button(string label, float x, float y, UnityEngine.Events.UnityAction onClick)
        {
            var button = UIFactory.CreateButton(Window.Content, label, onClick, 14);
            button.GetComponent<RectTransform>().SetRect(x, y, 206f, 36f);
            return button;
        }
    }
}
