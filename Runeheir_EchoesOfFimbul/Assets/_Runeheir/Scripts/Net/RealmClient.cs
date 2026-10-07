using System;
using System.Collections.Generic;
using Mirror;
using Runeheir.Field;
using Runeheir.Items;
using Runeheir.Online;
using Runeheir.Player;
using Runeheir.Session;
using Runeheir.Social;
using Runeheir.World;
using Runeheir.WorldBuilding;
using UnityEngine;

namespace Runeheir.Net
{
    /// <summary>
    /// This game's side of a realm connection: the <see cref="IOnlineSession"/> the field, chat and travel use, and the
    /// <see cref="ISocialService"/> behind the party, guild, trade and stall windows. Steps that change our own character
    /// (handing over trade goods, delivering a stall sale, paying a guild fee) run here on the local character.
    /// </summary>
    internal sealed class RealmClient : IOnlineSession, ISocialService
    {
        private readonly NetworkAccountService _accounts;
        private bool _warping;

        public RealmClient(NetworkAccountService accounts)
        {
            _accounts = accounts;
        }

        public bool IsServer => NetworkServer.active;

        public string RealmName => IsServer && RealmServer.Instance != null ? RealmServer.Instance.Config.Name : _accounts.RealmName;

        public bool AllowGmCommands => IsServer && RealmServer.Instance != null ? RealmServer.Instance.Config.AllowGmCommands : _accounts.AllowGmCommands;

        public ISocialService Social => this;

        public SocialState State { get; } = new SocialState();

        private static PlayerCharacter Me => PlayerCharacter.Local;

        public void Start()
        {
            _accounts.Attach();
            NetworkClient.RegisterHandler<WarpReply>(OnWarpReply);
            NetworkClient.RegisterHandler<LeftWorld>(_ => { });
            NetworkClient.RegisterHandler<ChatDeliver>(OnChat);
            NetworkClient.RegisterHandler<SocialUpdate>(OnSocial);
            NetSpawning.RegisterClientHandlers();
        }

        public void Stop()
        {
            _accounts.Detach();
            NetworkClient.UnregisterHandler<WarpReply>();
            NetworkClient.UnregisterHandler<LeftWorld>();
            NetworkClient.UnregisterHandler<ChatDeliver>();
            NetworkClient.UnregisterHandler<SocialUpdate>();
            NetSpawning.UnregisterClientHandlers();
            State.Clear();
        }

        // ================================================================ IOnlineSession
        public BuiltWorld HostedWorld(string mapId)
        {
            return IsServer && RealmServer.Instance != null ? RealmServer.Instance.EnsureMap(mapId) : null;
        }

        public void EnterMap(string mapId, Vector3 spawnPoint)
        {
            var record = GameSession.Instance.ActiveCharacter;
            if (record == null || !NetworkClient.isConnected)
            {
                return;
            }

            _warping = false;
            State.Trade = null;
            State.MyStall = null;
            State.Browsing = null;
            State.NotifyChanged();
            if (!NetworkClient.ready)
            {
                NetworkClient.Ready();
            }

            NetworkClient.Send(new EnterMapRequest
            {
                MapId = mapId,
                Position = spawnPoint,
                Slot = record.Slot,
                RecordJson = JsonUtility.ToJson(record),
            });
        }

        public bool Warp(PlayerCharacter player, string mapId, string arrivalPortalId, string message)
        {
            if (!NetworkClient.isConnected || player == null)
            {
                return false;
            }

            _warping = true;
            if (!string.IsNullOrEmpty(message))
            {
                ChatLog.System(message);
            }

            NetworkClient.Send(new WarpRequest
            {
                MapId = mapId,
                PortalId = arrivalPortalId ?? string.Empty,
                Slot = player.Record.Slot,
                RecordJson = JsonUtility.ToJson(player.Record),
            });
            return true;
        }

        public void RequestBranch(bool boss, Vector3 point)
        {
            if (NetworkClient.isConnected)
            {
                NetworkClient.Send(new BranchRequest { Boss = boss, Position = point });
            }
        }

        public void SendChat(ChatInput input)
        {
            if (NetworkClient.isConnected)
            {
                NetworkClient.Send(new ChatSend { Channel = (byte)input.Channel, Target = input.Target ?? string.Empty, Text = input.Text });
            }
        }

        public void LeaveWorld()
        {
            State.Trade = null;
            State.MyStall = null;
            State.Browsing = null;
            State.NotifyChanged();
            if (NetworkClient.isConnected)
            {
                NetworkClient.Send(new LeaveWorldRequest());
            }
        }

        public void Disconnect()
        {
            OnlineSession.Launcher?.Shutdown();
        }

        private void OnWarpReply(WarpReply reply)
        {
            if (_warping)
            {
                _warping = false;
                if (!reply.Ok)
                {
                    WorldTravel.AbortWarp(reply.Error ?? "The realm refused the warp.");
                    return;
                }

                var map = MapCatalog.Get(reply.MapId);
                SceneFlow.Load(map != null ? map.SceneName : MapCatalog.WorldScene);
                return;
            }

            if (!reply.Ok)
            {
                // Entering the map itself failed: back to character select with the reason.
                ChatLog.Error("The realm couldn't place your character: " + reply.Error);
                GameSession.Instance.ReturnToCharacterSelect();
            }
        }

        // ================================================================ chat
        private void OnChat(ChatDeliver message)
        {
            var channel = (ChatChannel)message.Channel;
            string from = ChatRules.Sanitize(message.From, 40);
            string text = ChatRules.Sanitize(message.Text, 240);
            switch (channel)
            {
                case ChatChannel.System:
                    ChatLog.Notice(text);
                    break;
                case ChatChannel.Party:
                    ChatLog.Add(ChatRules.Format(channel, from, text), ChatKind.Party);
                    break;
                case ChatChannel.Guild:
                    ChatLog.Add(ChatRules.Format(channel, from, text), ChatKind.Guild);
                    break;
                case ChatChannel.Shout:
                    ChatLog.Add(ChatRules.Format(channel, from, text), ChatKind.Shout);
                    break;
                case ChatChannel.Whisper:
                    if (!message.Outgoing)
                    {
                        State.LastWhisperFrom = from;
                    }

                    ChatLog.Add(ChatRules.Format(channel, from, text, message.Outgoing), ChatKind.Whisper);
                    break;
                default:
                    ChatLog.Add(ChatRules.Format(channel, from, text));
                    break;
            }
        }

        // ================================================================ realm → us
        private void OnSocial(SocialUpdate update)
        {
            switch (update.Kind)
            {
                case SocialEvent.Message:
                    ChatLog.System(update.Text);
                    break;
                case SocialEvent.Error:
                    ChatLog.Error(update.Text);
                    break;

                case SocialEvent.Party:
                    State.Party = Json.Read<PartyInfo>(update.Json);
                    break;
                case SocialEvent.PartyInvite:
                    State.PartyInviteFrom = update.Text;
                    State.PartyInviteName = update.Text2;
                    State.PartyInviteId = update.Index;
                    ChatLog.System($"{update.Text} invites you to the party {update.Text2}.");
                    break;
                case SocialEvent.Guild:
                    State.Guild = Json.Read<GuildRecord>(update.Json);
                    break;
                case SocialEvent.GuildInvite:
                    State.GuildInviteFrom = update.Text;
                    State.GuildInviteName = update.Text2;
                    State.GuildInviteId = update.Index;
                    ChatLog.System($"{update.Text} invites you to the guild {update.Text2}.");
                    break;
                case SocialEvent.Refund:
                    Refund(update.Number, update.Text);
                    break;

                case SocialEvent.TradeAsk:
                    State.TradeRequestFrom = update.Text;
                    ChatLog.System($"{update.Text} wants to trade with you.");
                    break;
                case SocialEvent.TradeState:
                {
                    var state = Json.Read<TradeStateJson>(update.Json);
                    if (state != null)
                    {
                        if (State.Trade == null)
                        {
                            ChatLog.System($"Trading with {state.Partner}. Put items in, press OK, then Trade.");
                        }

                        State.Trade = new TradeView { Partner = state.Partner, Mine = state.Mine ?? new TradeOffer(), Theirs = state.Theirs ?? new TradeOffer() };
                    }

                    break;
                }

                case SocialEvent.TradeClosed:
                    State.Trade = null;
                    ChatLog.System(update.Text);
                    break;
                case SocialEvent.TradeGive:
                    GiveTradeSide(Json.Read<TradeOffer>(update.Json));
                    break;
                case SocialEvent.TradeReceive:
                    ReceiveTradeSide(Json.Read<TradeOffer>(update.Json), update.Text);
                    break;

                case SocialEvent.StallOpened:
                    State.MyStall = Json.Read<VendingStall>(update.Json);
                    ChatLog.System($"Your stall \"{State.MyStall?.Title}\" is open. Buyers click you to browse; sales pay straight into your purse.");
                    break;
                case SocialEvent.StallClosed:
                    State.MyStall = null;
                    ChatLog.System(update.Text);
                    if (Me != null)
                    {
                        Me.ReturnCartGoods();
                        Me.SaveNow();
                    }

                    break;
                case SocialEvent.StallView:
                {
                    var stall = Json.Read<VendingStall>(update.Json);
                    if (stall != null)
                    {
                        stall.Owner = update.Text;
                    }

                    State.Browsing = stall;
                    break;
                }

                case SocialEvent.VendDeliver:
                    Deliver(update.Index, Json.Read<ItemStack>(update.Json), update.Number, update.Text);
                    break;
                case SocialEvent.VendReceived:
                    ReceivePurchase(Json.Read<ItemStack>(update.Json), update.Text, update.Number);
                    break;
                case SocialEvent.VendSold:
                {
                    var goods = Json.Read<ItemStack>(update.Json);
                    ChatLog.Loot($"Sold {goods?.DisplayName} x{goods?.Amount} to {update.Text} for {update.Number:N0} zeny.");
                    if (State.MyStall != null && goods != null)
                    {
                        VendingRules.TryTake(State.MyStall, State.MyStall.Entries.FindIndex(e => ItemTransfer.Matches(e.Item, goods)), goods, goods.Amount,
                            State.MyStall.Entries.Find(e => ItemTransfer.Matches(e.Item, goods))?.Price ?? 0, out _, out _, out _);
                    }

                    break;
                }
            }

            State.NotifyChanged();
        }

        private void Refund(long zeny, string reason)
        {
            if (Me != null && zeny > 0)
            {
                Me.Record.Zeny = Math.Min(ItemTransfer.MaxZeny, Me.Record.Zeny + zeny);
                Me.Inventory.NotifyChanged();
                Me.SaveNow();
            }

            if (!string.IsNullOrEmpty(reason))
            {
                ChatLog.Error(zeny > 0 ? $"{reason} ({zeny:N0} zeny returned.)" : reason);
            }
        }

        private void GiveTradeSide(TradeOffer give)
        {
            var me = Me;
            string error = null;
            bool gave = me != null && give != null
                        && PlayerTradeRules.CanExchange(me.Record, me.Inventory, me.Stats.WeightCapacity, me.CurrentWeight, give, State.Trade?.Theirs, out error)
                        && PlayerTradeRules.TryGive(me.Record, me.Inventory, give, out error);
            if (gave)
            {
                me.SaveNow();
            }

            NetworkClient.Send(new SocialRequest { Op = SocialOp.TradeGave, Number = gave ? 1 : 0, Text = error ?? string.Empty });
        }

        private static void ReceiveTradeSide(TradeOffer offer, string from)
        {
            var me = Me;
            if (me == null || offer == null)
            {
                return;
            }

            int toCart = PlayerTradeRules.Receive(me.Record, me.Inventory, offer);
            if (string.IsNullOrEmpty(from))
            {
                ChatLog.System("Your side of the trade came back to you.");
            }
            else
            {
                ChatLog.Loot($"Trade with {from} complete.");
            }

            if (toCart > 0)
            {
                ChatLog.System("Your bag was full: the rest waits in your Pushcart hold and comes out when there's room.");
            }

            me.SaveNow();
        }

        private static void Deliver(int saleId, ItemStack goods, long cost, string buyer)
        {
            var me = Me;
            string error = "You aren't here.";
            bool ok = me != null && VendingRules.TryDeliver(me.Record, goods, cost, out error);
            if (ok)
            {
                me.Inventory.NotifyChanged();
                me.SaveNow();
            }

            NetworkClient.Send(new SocialRequest { Op = SocialOp.VendDelivered, Index = saleId, Number = ok ? 1 : 0, Text = error ?? string.Empty });
        }

        private static void ReceivePurchase(ItemStack goods, string seller, long cost)
        {
            var me = Me;
            if (me == null || goods == null)
            {
                return;
            }

            int toCart = VendingRules.Receive(me.Record, me.Inventory, goods);
            me.Inventory.NotifyChanged();
            ChatLog.Loot($"Bought {goods.DisplayName} x{goods.Amount} from {seller} for {cost:N0} zeny.");
            if (toCart > 0)
            {
                ChatLog.System("Your bag was full: it waits in your Pushcart hold.");
            }

            me.SaveNow();
        }

        // ================================================================ ISocialService: us → realm
        private static void Send(SocialOp op, string text = null, long number = 0, int index = 0, string json = null)
        {
            if (NetworkClient.isConnected)
            {
                NetworkClient.Send(new SocialRequest { Op = op, Text = text ?? string.Empty, Number = number, Index = index, Json = json ?? string.Empty });
            }
        }

        public void PartyCreate(string name)
        {
            if (PartyRules.ValidateName(name, out string clean, out string error))
            {
                Send(SocialOp.PartyCreate, clean);
            }
            else
            {
                ChatLog.Error(error);
            }
        }

        public void PartyInvite(string name)
        {
            Send(SocialOp.PartyInvite, name);
        }

        public void PartyRespond(bool accept)
        {
            Send(SocialOp.PartyRespond, number: accept ? 1 : 0, index: State.PartyInviteId);
            State.PartyInviteFrom = State.PartyInviteName = null;
            State.PartyInviteId = 0;
            State.NotifyChanged();
        }

        public void PartyLeave()
        {
            Send(SocialOp.PartyLeave);
        }

        public void PartyKick(string name)
        {
            Send(SocialOp.PartyKick, name);
        }

        public void PartyMakeLeader(string name)
        {
            Send(SocialOp.PartyLeader, name);
        }

        public void PartySetShare(ExpShareMode mode)
        {
            Send(SocialOp.PartyShare, number: (int)mode);
        }

        public void GuildCreate(string name)
        {
            var me = Me;
            if (me == null)
            {
                return;
            }

            if (!GuildRules.ValidateName(name, out string clean, out string error))
            {
                ChatLog.Error(error);
                return;
            }

            if (me.Record.BaseLevel < GuildRules.MinFounderLevel)
            {
                ChatLog.Error($"Founding a guild needs base level {GuildRules.MinFounderLevel}.");
                return;
            }

            if (me.Record.Zeny < GuildRules.FoundingFee)
            {
                ChatLog.Error($"Founding a guild costs {GuildRules.FoundingFee:N0} zeny.");
                return;
            }

            // Paid up front; the realm gives it back if the name is taken.
            me.Record.Zeny -= GuildRules.FoundingFee;
            me.Inventory.NotifyChanged();
            me.SaveNow();
            Send(SocialOp.GuildCreate, clean);
        }

        public void GuildInvite(string name)
        {
            Send(SocialOp.GuildInvite, name);
        }

        public void GuildRespond(bool accept)
        {
            Send(SocialOp.GuildRespond, number: accept ? 1 : 0, index: State.GuildInviteId);
            State.GuildInviteFrom = State.GuildInviteName = null;
            State.GuildInviteId = 0;
            State.NotifyChanged();
        }

        public void GuildLeave()
        {
            Send(SocialOp.GuildLeave);
        }

        public void GuildExpel(string name)
        {
            Send(SocialOp.GuildExpel, name);
        }

        public void GuildSetRank(string name, GuildRank rank)
        {
            Send(SocialOp.GuildRank, name, rank == GuildRank.Officer ? 1 : 0);
        }

        public void GuildHandOver(string name)
        {
            Send(SocialOp.GuildHandOver, name);
        }

        public void GuildSetNotice(string notice)
        {
            Send(SocialOp.GuildNotice, ChatRules.Sanitize(notice, GuildRules.MaxNoticeLength));
        }

        public void GuildDisband()
        {
            Send(SocialOp.GuildDisband);
        }

        public void TradeRequest(string name)
        {
            if (State.MyStall != null)
            {
                ChatLog.Error("Close your stall before trading.");
                return;
            }

            Send(SocialOp.TradeRequest, name);
        }

        public void TradeRespond(bool accept)
        {
            Send(SocialOp.TradeRespond, number: accept ? 1 : 0);
            State.TradeRequestFrom = null;
            State.NotifyChanged();
        }

        public void TradeAddItem(ItemStack entry, int amount)
        {
            var me = Me;
            var trade = State.Trade;
            if (me == null || trade == null || entry == null || !me.Inventory.Contains(entry))
            {
                return;
            }

            if (trade.Mine.Locked)
            {
                ChatLog.Error("Your offer is locked.");
                return;
            }

            var copy = ItemTransfer.Copy(entry, Math.Max(1, amount));
            if (copy == null)
            {
                ChatLog.Error("That can't be traded.");
                return;
            }

            var planned = new List<ItemStack>(trade.Mine.Items) { copy };
            if (!ItemTransfer.HasAll(me.Inventory, planned, out string error))
            {
                ChatLog.Error(error);
                return;
            }

            Send(SocialOp.TradeAdd, json: JsonUtility.ToJson(copy));
        }

        public void TradeSetZeny(long zeny)
        {
            var me = Me;
            if (me == null || zeny < 0 || zeny > me.Record.Zeny)
            {
                ChatLog.Error("You don't have that much zeny.");
                return;
            }

            Send(SocialOp.TradeZeny, number: zeny);
        }

        public void TradeLock()
        {
            Send(SocialOp.TradeLock);
        }

        public void TradeConfirm()
        {
            var me = Me;
            var trade = State.Trade;
            if (me != null && trade != null
                && !PlayerTradeRules.CanExchange(me.Record, me.Inventory, me.Stats.WeightCapacity, me.CurrentWeight, trade.Mine, trade.Theirs, out string error))
            {
                ChatLog.Error(error);
                return;
            }

            Send(SocialOp.TradeConfirm);
        }

        public void TradeCancel()
        {
            Send(SocialOp.TradeCancel);
        }

        public void VendOpen(VendingStall stall)
        {
            if (stall == null)
            {
                return;
            }

            Send(SocialOp.VendOpen, json: Json.Write(stall));
        }

        public void VendClose()
        {
            Send(SocialOp.VendClose);
        }

        public void VendBrowse(string owner)
        {
            Send(SocialOp.VendBrowse, owner);
        }

        public void VendBuy(string owner, int index, int amount)
        {
            var me = Me;
            var stall = State.Browsing;
            var line = VendingRules.Line(stall, index);
            if (me == null || line == null)
            {
                return;
            }

            if (!VendingRules.CanBuy(stall, index, amount, me.Record.Zeny, out long cost, out string error))
            {
                ChatLog.Error(error);
                return;
            }

            var goods = ItemTransfer.Copy(line.Item, amount);
            if (!ItemTransfer.CanReceive(me.Inventory, me.Stats.WeightCapacity, me.CurrentWeight, null, new[] { goods }, out error))
            {
                ChatLog.Error(error);
                return;
            }

            // Paid now; the realm gives it back if the sale falls through.
            me.Record.Zeny -= cost;
            me.Inventory.NotifyChanged();
            Send(SocialOp.VendBuy, owner, amount, index, Json.Write(new VendBuyJson { Item = line.Item, UnitPrice = line.Price }));
        }
    }
}
