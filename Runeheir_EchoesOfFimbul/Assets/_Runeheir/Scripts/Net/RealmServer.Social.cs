using System;
using System.Collections.Generic;
using Mirror;
using Runeheir.Combat;
using Runeheir.Field;
using Runeheir.Items;
using Runeheir.Jobs;
using Runeheir.Social;
using Runeheir.World;
using Runeheir.WorldBuilding;
using UnityEngine;

namespace Runeheir.Net
{
    /// <summary>A stall purchase between "the buyer paid" and "the seller handed it over".</summary>
    internal sealed class PendingSale
    {
        public RealmSession Buyer;
        public RealmSession Seller;
        public ItemStack Goods;
        public long UnitPrice;
        public long Cost;
    }

    /// <summary>Both traders' answers to "give your side".</summary>
    internal sealed class TradeCommit
    {
        public bool? GaveA;
        public bool? GaveB;
    }

    [Serializable]
    internal sealed class VendBuyJson
    {
        public ItemStack Item;
        public long UnitPrice;
    }

    public sealed partial class RealmServer
    {
        private readonly PartyBook _parties = new PartyBook();
        private readonly GuildBook _guilds;
        private readonly Dictionary<int, PendingSale> _sales = new Dictionary<int, PendingSale>();
        private readonly Dictionary<TradeSession, TradeCommit> _commits = new Dictionary<TradeSession, TradeCommit>();
        private readonly Dictionary<int, string> _partyJson = new Dictionary<int, string>();
        private int _nextSale = 1;

        // ================================================================ arriving and leaving
        private void OnEnteredWorld(RealmSession session)
        {
            var record = session.Record;
            var party = _parties.UpdateMember(session.CharacterName, record.BaseLevel, record.Job, session.MapId, true,
                Math.Max(0, record.Hp), Math.Max(1, record.Hp), out bool dropped);
            if (party != null)
            {
                PushParty(party);
                if (dropped)
                {
                    PartyNotice(party, "Even Share is off: the party's levels are now more than 30 apart.");
                }
            }
            else
            {
                SendParty(session, null);
            }

            var guild = _guilds.UpdateMember(session.CharacterName, record.BaseLevel, record.Job, true);
            if (guild != null)
            {
                PushGuild(guild);
            }
            else
            {
                SendGuild(session, null);
            }
        }

        private void OnLeftWorld(RealmSession session)
        {
            string name = session.CharacterName;
            var party = _parties.UpdateMember(name, session.Record?.BaseLevel ?? 1, session.Record?.Job ?? JobId.Initiate, null, false, 0, 1, out _);
            if (party != null)
            {
                PushParty(party);
            }

            var guild = _guilds.UpdateMember(name, session.Record?.BaseLevel ?? 1, session.Record?.Job ?? JobId.Initiate, false);
            if (guild != null)
            {
                PushGuild(guild);
            }
        }

        /// <summary>A character was deleted: out of its party and guild.</summary>
        private void ForgetCharacter(string name)
        {
            var party = _parties.Forget(name);
            if (party != null)
            {
                PushParty(party);
            }

            var guild = _guilds.Forget(name);
            if (guild != null)
            {
                PushGuild(guild);
            }
        }

        /// <summary>Vendors stay put (Ragnarok): a stall closes if its owner leaves the spot it was set up on.</summary>
        private const float StallDrift = 1.5f;

        /// <summary>Once a second: party members' HP, levels and maps; stalls left behind; trades that drifted apart.</summary>
        private void RefreshSocial()
        {
            foreach (var party in new List<PartyInfo>(_parties.All))
            {
                bool dropped = false;
                foreach (var member in party.Members)
                {
                    var session = Find(member.Name);
                    var player = session?.Player;
                    if (player == null)
                    {
                        continue;
                    }

                    _parties.UpdateMember(member.Name, player.Level, player.Job, session.MapId, true, player.Hp, player.MaxHp, out bool memberDropped);
                    dropped |= memberDropped;
                }

                if (dropped)
                {
                    PartyNotice(party, "Even Share is off: the party's levels are now more than 30 apart.");
                }

                string json = Json.Write(party);
                if (!_partyJson.TryGetValue(party.Id, out string last) || last != json)
                {
                    PushParty(party);
                }
            }

            foreach (var session in _sessions.Values)
            {
                if (session.Stall != null && session.Entity != null
                    && CombatEntity.HorizontalDistance(session.Entity.Position, session.StallSpot) > StallDrift)
                {
                    CloseStall(session, "Your stall closed: you left its spot.");
                }
            }

            foreach (var session in _sessions.Values)
            {
                var trade = session.Trade;
                if (trade == null || _commits.ContainsKey(trade) || !string.Equals(trade.A, session.CharacterName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var partner = Find(trade.B);
                if (partner?.Entity == null || session.Entity == null
                    || CombatEntity.HorizontalDistance(session.Entity.Position, partner.Entity.Position) > PlayerTradeRules.MaxDistance + 4f)
                {
                    EndTrade(session, "The trade was cancelled: you moved too far apart.");
                    break; // the collection was changed
                }
            }
        }

        // ================================================================ Even Share (GDD §8)
        private bool ShareExperience(PlayerEntity earner, long baseExp, long jobExp)
        {
            RealmSession session = null;
            foreach (var candidate in _sessions.Values)
            {
                if (candidate.Entity == earner)
                {
                    session = candidate;
                    break;
                }
            }

            if (session == null)
            {
                return false;
            }

            var sharers = _parties.Sharers(session.CharacterName, session.MapId, member =>
            {
                var entity = Find(member.Name)?.Entity;
                return entity != null && !entity.IsDead;
            });
            if (sharers.Count < 2)
            {
                return false;
            }

            long eachBase = PartyRules.ShareEach(baseExp, sharers.Count);
            long eachJob = PartyRules.ShareEach(jobExp, sharers.Count);
            foreach (var member in sharers)
            {
                Find(member.Name)?.Entity?.GrantExperience(eachBase, eachJob);
            }

            return true;
        }

        // ================================================================ requests
        private void OnSocial(NetworkConnectionToClient connection, SocialRequest request)
        {
            var session = Session(connection);
            if (session == null || !session.InWorld)
            {
                return;
            }

            try
            {
                switch (request.Op)
                {
                    case SocialOp.PartyCreate:
                        Result(session, _parties.Create(Member(session), request.Text), PushParty);
                        break;
                    case SocialOp.PartyInvite:
                        PartyInvite(session, request.Text);
                        break;
                    case SocialOp.PartyRespond:
                        PartyRespond(session, request.Number != 0, request.Index);
                        break;
                    case SocialOp.PartyLeave:
                        PartyLeave(session);
                        break;
                    case SocialOp.PartyKick:
                        PartyKick(session, request.Text);
                        break;
                    case SocialOp.PartyLeader:
                        Result(session, _parties.MakeLeader(session.CharacterName, request.Text), PushParty);
                        break;
                    case SocialOp.PartyShare:
                        Result(session, _parties.SetExpShare(session.CharacterName, request.Number == 1 ? ExpShareMode.EvenShare : ExpShareMode.EachTakes), PushParty);
                        break;

                    case SocialOp.GuildCreate:
                        GuildCreate(session, request.Text);
                        break;
                    case SocialOp.GuildInvite:
                        GuildInvite(session, request.Text);
                        break;
                    case SocialOp.GuildRespond:
                        GuildRespond(session, request.Number != 0, request.Index);
                        break;
                    case SocialOp.GuildLeave:
                        GuildLeave(session);
                        break;
                    case SocialOp.GuildExpel:
                        GuildExpel(session, request.Text);
                        break;
                    case SocialOp.GuildRank:
                        Result(session, _guilds.SetRank(session.CharacterName, request.Text, request.Number == 1 ? GuildRank.Officer : GuildRank.Member), PushGuild);
                        break;
                    case SocialOp.GuildHandOver:
                        Result(session, _guilds.HandOver(session.CharacterName, request.Text), PushGuild);
                        break;
                    case SocialOp.GuildNotice:
                        Result(session, _guilds.SetNotice(session.CharacterName, request.Text), PushGuild);
                        break;
                    case SocialOp.GuildDisband:
                        GuildDisband(session);
                        break;

                    case SocialOp.TradeRequest:
                        TradeRequest(session, request.Text);
                        break;
                    case SocialOp.TradeRespond:
                        TradeRespond(session, request.Number != 0);
                        break;
                    case SocialOp.TradeAdd:
                        TradeEdit(session, trade => trade.TryAddItem(session.CharacterName, Json.Read<ItemStack>(request.Json), out string error) ? null : error);
                        break;
                    case SocialOp.TradeZeny:
                        TradeEdit(session, trade => trade.TrySetZeny(session.CharacterName, request.Number, out string error) ? null : error);
                        break;
                    case SocialOp.TradeLock:
                        TradeEdit(session, trade => trade.TryLock(session.CharacterName, out string error) ? null : error);
                        break;
                    case SocialOp.TradeConfirm:
                        TradeConfirm(session);
                        break;
                    case SocialOp.TradeGave:
                        TradeGave(session, request.Number != 0, request.Text);
                        break;
                    case SocialOp.TradeCancel:
                        EndTrade(session, $"{session.CharacterName} cancelled the trade.");
                        break;

                    case SocialOp.VendOpen:
                        VendOpen(session, Json.Read<VendingStall>(request.Json));
                        break;
                    case SocialOp.VendClose:
                        CloseStall(session);
                        break;
                    case SocialOp.VendBrowse:
                        VendBrowse(session, request.Text);
                        break;
                    case SocialOp.VendBuy:
                        VendBuy(session, request.Text, request.Index, (int)Math.Max(0, Math.Min(int.MaxValue, request.Number)), Json.Read<VendBuyJson>(request.Json));
                        break;
                    case SocialOp.VendDelivered:
                        VendDelivered(session, request.Index, request.Number != 0, request.Text);
                        break;
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Error(session, "The realm couldn't do that: " + exception.Message);
            }
        }

        private static void Result<T>(RealmSession session, Accounts.OpResult<T> result, Action<T> onSuccess)
        {
            if (result.Success)
            {
                onSuccess(result.Value);
            }
            else
            {
                Error(session, result.Error);
            }
        }

        // ================================================================ parties
        private static PartyMember Member(RealmSession session)
        {
            var player = session.Player;
            return new PartyMember
            {
                Name = session.CharacterName,
                BaseLevel = player != null ? player.Level : session.Record?.BaseLevel ?? 1,
                Job = player != null ? player.Job : session.Record?.Job ?? JobId.Initiate,
                MapId = session.MapId,
                Online = true,
                Hp = player != null ? player.Hp : 1,
                MaxHp = player != null ? player.MaxHp : 1,
            };
        }

        private void PartyInvite(RealmSession session, string name)
        {
            var target = Find(name);
            if (target == null)
            {
                Error(session, $"{name} isn't online.");
                return;
            }

            var result = _parties.Invite(session.CharacterName, target.CharacterName);
            if (!result.Success)
            {
                Error(session, result.Error);
                return;
            }

            target.Connection.Send(new SocialUpdate
            {
                Kind = SocialEvent.PartyInvite, Text = session.CharacterName, Text2 = result.Value.Name, Index = result.Value.Id,
            });
            Notify(session, $"You invited {target.CharacterName} to the party.");
        }

        private void PartyRespond(RealmSession session, bool accept, int partyId)
        {
            if (!accept)
            {
                _parties.Decline(session.CharacterName);
                var party = _parties.Get(partyId);
                if (party != null)
                {
                    Notify(Find(party.Leader), $"{session.CharacterName} declined the party invitation.");
                }

                return;
            }

            var result = _parties.Accept(Member(session), partyId, out bool dropped);
            if (!result.Success)
            {
                Error(session, result.Error);
                return;
            }

            PushParty(result.Value);
            PartyNotice(result.Value, $"{session.CharacterName} joined the party.");
            if (dropped)
            {
                PartyNotice(result.Value, "Even Share is off: the party's levels are now more than 30 apart.");
            }
        }

        private void PartyLeave(RealmSession session)
        {
            var party = _parties.Leave(session.CharacterName);
            SendParty(session, null);
            Notify(session, "You left the party.");
            if (party != null)
            {
                PushParty(party);
                PartyNotice(party, $"{session.CharacterName} left the party.");
            }
        }

        private void PartyKick(RealmSession session, string name)
        {
            var result = _parties.Kick(session.CharacterName, name);
            if (!result.Success)
            {
                Error(session, result.Error);
                return;
            }

            var kicked = Find(name);
            SendParty(kicked, null);
            Notify(kicked, "You were expelled from the party.");
            PushParty(result.Value);
            PartyNotice(result.Value, $"{name} was expelled from the party.");
        }

        private void PushParty(PartyInfo party)
        {
            if (party == null)
            {
                return;
            }

            string json = Json.Write(party);
            _partyJson[party.Id] = json;
            foreach (var member in party.Members)
            {
                var session = Find(member.Name);
                Send(session, new SocialUpdate { Kind = SocialEvent.Party, Json = json });
            }
        }

        private static void SendParty(RealmSession session, PartyInfo party)
        {
            Send(session, new SocialUpdate { Kind = SocialEvent.Party, Json = party != null ? Json.Write(party) : string.Empty });
        }

        private void PartyNotice(PartyInfo party, string text)
        {
            foreach (var member in party.Members)
            {
                Notify(Find(member.Name), text);
            }
        }

        // ================================================================ guilds
        private void GuildCreate(RealmSession session, string name)
        {
            var player = session.Player;
            var result = _guilds.Create(session.CharacterName, player != null ? player.Level : 1, player != null ? player.Job : JobId.Initiate, name);
            if (!result.Success)
            {
                // The founder paid the fee before asking: give it back.
                session.Connection.Send(new SocialUpdate { Kind = SocialEvent.Refund, Number = GuildRules.FoundingFee, Text = result.Error });
                return;
            }

            PushGuild(result.Value);
            Notify(session, $"The guild {result.Value.Name} is founded. Invite members from the guild window (Alt+G).");
        }

        private void GuildInvite(RealmSession session, string name)
        {
            var target = Find(name);
            if (target == null)
            {
                Error(session, $"{name} isn't online.");
                return;
            }

            var result = _guilds.Invite(session.CharacterName, target.CharacterName);
            if (!result.Success)
            {
                Error(session, result.Error);
                return;
            }

            target.Connection.Send(new SocialUpdate
            {
                Kind = SocialEvent.GuildInvite, Text = session.CharacterName, Text2 = result.Value.Name, Index = result.Value.Id,
            });
            Notify(session, $"You invited {target.CharacterName} to the guild.");
        }

        private void GuildRespond(RealmSession session, bool accept, int guildId)
        {
            if (!accept)
            {
                _guilds.Decline(session.CharacterName);
                return;
            }

            var player = session.Player;
            var result = _guilds.Accept(session.CharacterName, player != null ? player.Level : 1, player != null ? player.Job : JobId.Initiate, guildId);
            if (!result.Success)
            {
                Error(session, result.Error);
                return;
            }

            PushGuild(result.Value);
            GuildNotice(result.Value, $"{session.CharacterName} joined the guild.");
        }

        private void GuildLeave(RealmSession session)
        {
            var guild = _guilds.GuildOf(session.CharacterName);
            var result = _guilds.Leave(session.CharacterName);
            if (!result.Success)
            {
                Error(session, result.Error);
                return;
            }

            SendGuild(session, null);
            Notify(session, "You left the guild.");
            if (guild != null && _guilds.Get(guild.Id) != null)
            {
                PushGuild(guild);
                GuildNotice(guild, $"{session.CharacterName} left the guild.");
            }
        }

        private void GuildExpel(RealmSession session, string name)
        {
            var result = _guilds.Expel(session.CharacterName, name);
            if (!result.Success)
            {
                Error(session, result.Error);
                return;
            }

            var expelled = Find(name);
            SendGuild(expelled, null);
            Notify(expelled, "You were expelled from the guild.");
            PushGuild(result.Value);
        }

        private void GuildDisband(RealmSession session)
        {
            var guild = _guilds.GuildOf(session.CharacterName);
            var result = _guilds.Disband(session.CharacterName);
            if (!result.Success)
            {
                Error(session, result.Error);
                return;
            }

            foreach (var member in guild.Members)
            {
                var other = Find(member.Name);
                SendGuild(other, null);
                Notify(other, $"The guild {guild.Name} has been disbanded.");
            }
        }

        private void PushGuild(GuildRecord guild)
        {
            if (guild == null)
            {
                return;
            }

            string json = Json.Write(guild);
            foreach (var member in guild.Members)
            {
                Send(Find(member.Name), new SocialUpdate { Kind = SocialEvent.Guild, Json = json });
            }
        }

        private static void SendGuild(RealmSession session, GuildRecord guild)
        {
            Send(session, new SocialUpdate { Kind = SocialEvent.Guild, Json = guild != null ? Json.Write(guild) : string.Empty });
        }

        private void GuildNotice(GuildRecord guild, string text)
        {
            foreach (var member in guild.Members)
            {
                Notify(Find(member.Name), text);
            }
        }

        // ================================================================ player trades
        private void TradeRequest(RealmSession session, string name)
        {
            var target = Find(name);
            if (target == null || target == session)
            {
                Error(session, target == null ? $"{name} isn't online." : "You can't trade with yourself.");
                return;
            }

            string problem = TradeProblem(session, target);
            if (problem != null)
            {
                Error(session, problem);
                return;
            }

            target.TradeAskedBy = session.CharacterName;
            target.Connection.Send(new SocialUpdate { Kind = SocialEvent.TradeAsk, Text = session.CharacterName });
            Notify(session, $"You asked {target.CharacterName} to trade.");
        }

        private void TradeRespond(RealmSession session, bool accept)
        {
            var asker = Find(session.TradeAskedBy);
            session.TradeAskedBy = null;
            if (asker == null)
            {
                Error(session, "They're no longer here.");
                return;
            }

            if (!accept)
            {
                Notify(asker, $"{session.CharacterName} declined to trade.");
                return;
            }

            string problem = TradeProblem(asker, session);
            if (problem != null)
            {
                Error(session, problem);
                Error(asker, problem);
                return;
            }

            var trade = new TradeSession(asker.CharacterName, session.CharacterName);
            asker.Trade = trade;
            session.Trade = trade;
            PushTrade(trade);
        }

        private static string TradeProblem(RealmSession a, RealmSession b)
        {
            if (a.Trade != null || b.Trade != null)
            {
                return "One of you is already trading.";
            }

            if (a.Stall != null || b.Stall != null)
            {
                return "Close the stall before trading.";
            }

            var ea = a.Entity;
            var eb = b.Entity;
            if (ea == null || eb == null || ea.IsDead || eb.IsDead || !string.Equals(a.MapId, b.MapId, StringComparison.OrdinalIgnoreCase)
                || CombatEntity.HorizontalDistance(ea.Position, eb.Position) > PlayerTradeRules.MaxDistance + 1f)
            {
                return "Stand next to each other to trade.";
            }

            return null;
        }

        private void TradeEdit(RealmSession session, Func<TradeSession, string> edit)
        {
            var trade = session.Trade;
            if (trade == null || _commits.ContainsKey(trade))
            {
                return;
            }

            string error = edit(trade);
            if (error != null)
            {
                Error(session, error);
                return;
            }

            PushTrade(trade);
        }

        private void TradeConfirm(RealmSession session)
        {
            var trade = session.Trade;
            if (trade == null || _commits.ContainsKey(trade))
            {
                return;
            }

            if (!trade.TryConfirm(session.CharacterName, out string error))
            {
                Error(session, error);
                return;
            }

            PushTrade(trade);
            if (!trade.BothConfirmed)
            {
                return;
            }

            // Both pressed Trade: each side gives its offer first, then receives the other's.
            _commits[trade] = new TradeCommit();
            foreach (string name in new[] { trade.A, trade.B })
            {
                Send(Find(name), new SocialUpdate { Kind = SocialEvent.TradeGive, Json = Json.Write(trade.OfferOf(name)) });
            }
        }

        private void TradeGave(RealmSession session, bool gave, string error)
        {
            var trade = session.Trade;
            if (trade == null || !_commits.TryGetValue(trade, out var commit))
            {
                return;
            }

            if (string.Equals(trade.A, session.CharacterName, StringComparison.OrdinalIgnoreCase))
            {
                commit.GaveA = gave;
            }
            else
            {
                commit.GaveB = gave;
            }

            if (!gave)
            {
                Error(session, string.IsNullOrEmpty(error) ? "You couldn't hand over your side." : ChatRules.Sanitize(error));
            }

            if (!commit.GaveA.HasValue || !commit.GaveB.HasValue)
            {
                return;
            }

            var a = Find(trade.A);
            var b = Find(trade.B);
            bool success = commit.GaveA.Value && commit.GaveB.Value;
            if (success)
            {
                Send(a, new SocialUpdate { Kind = SocialEvent.TradeReceive, Json = Json.Write(trade.OfferB), Text = trade.B });
                Send(b, new SocialUpdate { Kind = SocialEvent.TradeReceive, Json = Json.Write(trade.OfferA), Text = trade.A });
            }
            else
            {
                // Whoever already gave gets their side back.
                if (commit.GaveA.Value)
                {
                    Send(a, new SocialUpdate { Kind = SocialEvent.TradeReceive, Json = Json.Write(trade.OfferA), Text = string.Empty });
                }

                if (commit.GaveB.Value)
                {
                    Send(b, new SocialUpdate { Kind = SocialEvent.TradeReceive, Json = Json.Write(trade.OfferB), Text = string.Empty });
                }
            }

            _commits.Remove(trade);
            CloseTrade(trade, success ? "Trade complete." : "The trade fell through; nothing changed hands.");
        }

        /// <summary>Cancels <paramref name="session"/>'s trade (or pending request), refunding anyone who already gave.</summary>
        private void EndTrade(RealmSession session, string reason)
        {
            var trade = session?.Trade;
            if (trade == null)
            {
                return;
            }

            if (_commits.TryGetValue(trade, out var commit))
            {
                if (commit.GaveA == true)
                {
                    Send(Find(trade.A), new SocialUpdate { Kind = SocialEvent.TradeReceive, Json = Json.Write(trade.OfferA), Text = string.Empty });
                }

                if (commit.GaveB == true)
                {
                    Send(Find(trade.B), new SocialUpdate { Kind = SocialEvent.TradeReceive, Json = Json.Write(trade.OfferB), Text = string.Empty });
                }

                _commits.Remove(trade);
            }

            trade.Cancel();
            CloseTrade(trade, reason);
        }

        private void CloseTrade(TradeSession trade, string message)
        {
            foreach (var other in _sessions.Values)
            {
                if (other.Trade == trade)
                {
                    other.Trade = null;
                    Send(other, new SocialUpdate { Kind = SocialEvent.TradeClosed, Text = message });
                }
            }
        }

        private void PushTrade(TradeSession trade)
        {
            foreach (string name in new[] { trade.A, trade.B })
            {
                var session = Find(name);
                if (session == null)
                {
                    continue;
                }

                var state = new TradeStateJson { Partner = trade.PartnerOf(name), Mine = trade.OfferOf(name), Theirs = trade.PartnerOffer(name) };
                Send(session, new SocialUpdate { Kind = SocialEvent.TradeState, Json = Json.Write(state) });
            }
        }

        // ================================================================ street stalls (GDD §8)
        private void VendOpen(RealmSession session, VendingStall stall)
        {
            var map = MapCatalog.Get(session.MapId);
            var entity = session.Entity;
            string problem = null;
            if (stall == null || !VendingRules.Validate(stall, out problem))
            {
                problem = problem ?? "That stall isn't valid.";
            }
            else if (!VendingRules.MapAllowsVending(map))
            {
                problem = "Stalls can only be set up in Vigrid Haven.";
            }
            else if (session.Trade != null || session.Stall != null)
            {
                problem = "Finish what you're doing first.";
            }
            else if (entity == null || entity.IsDead)
            {
                problem = "You can't vend right now.";
            }
            else
            {
                problem = SpotProblem(session, entity.Position);
            }

            if (problem != null)
            {
                // The goods are already in the seller's cart hold: this tells their game to put them back.
                session.Connection.Send(new SocialUpdate { Kind = SocialEvent.StallClosed, Text = problem });
                return;
            }

            stall.Owner = session.CharacterName;
            session.Stall = stall;
            session.StallSpot = entity.Position;
            session.Connection.Send(new SocialUpdate { Kind = SocialEvent.StallOpened, Json = Json.Write(stall) });
        }

        /// <summary>Stalls keep their distance from each other, from NPCs and from warp portals.</summary>
        private string SpotProblem(RealmSession session, Vector3 at)
        {
            foreach (var other in _sessions.Values)
            {
                if (other != session && other.Stall != null && other.Entity != null && string.Equals(other.MapId, session.MapId, StringComparison.OrdinalIgnoreCase)
                    && CombatEntity.HorizontalDistance(other.Entity.Position, at) < VendingRules.MinSpacing)
                {
                    return $"Too close to {other.CharacterName}'s stall: step aside a little.";
                }
            }

            foreach (var npc in NpcActor.All)
            {
                if (npc != null && CombatEntity.HorizontalDistance(npc.transform.position, at) < VendingRules.MinSpacing + 1f)
                {
                    return $"Too close to {npc.DisplayName}: leave room for their customers.";
                }
            }

            foreach (var portal in WarpPortal.All)
            {
                if (portal != null && CombatEntity.HorizontalDistance(portal.Position, at) < VendingRules.MinSpacing + WarpPortal.TriggerRadius)
                {
                    return "Too close to the portal.";
                }
            }

            return null;
        }

        private void CloseStall(RealmSession session, string reason = "Your stall is closed.")
        {
            if (session?.Stall == null)
            {
                return;
            }

            session.Stall = null;
            foreach (var pair in new List<KeyValuePair<int, PendingSale>>(_sales))
            {
                if (pair.Value.Seller == session)
                {
                    _sales.Remove(pair.Key);
                    Refund(pair.Value.Buyer, pair.Value.Cost, "The stall closed before the sale went through. Your zeny is back.");
                }
            }

            Send(session, new SocialUpdate { Kind = SocialEvent.StallClosed, Text = reason });
        }

        private void VendBrowse(RealmSession session, string owner)
        {
            var seller = Find(owner);
            if (seller?.Stall == null)
            {
                Error(session, $"{owner} isn't vending.");
                return;
            }

            session.Connection.Send(new SocialUpdate { Kind = SocialEvent.StallView, Text = seller.CharacterName, Json = Json.Write(seller.Stall) });
        }

        private void VendBuy(RealmSession buyer, string owner, int index, int amount, VendBuyJson order)
        {
            var seller = Find(owner);
            long paid = order != null && amount > 0 ? Math.Max(0, order.UnitPrice) * amount : 0;
            if (seller?.Stall == null || seller == buyer || order == null || buyer.Entity == null || seller.Entity == null
                || !string.Equals(seller.MapId, buyer.MapId, StringComparison.OrdinalIgnoreCase)
                || CombatEntity.HorizontalDistance(buyer.Entity.Position, seller.Entity.Position) > 12f)
            {
                Refund(buyer, paid, seller?.Stall == null ? $"{owner} isn't vending any more." : "Walk up to the stall to buy.");
                return;
            }

            if (!VendingRules.TryTake(seller.Stall, index, order.Item, amount, order.UnitPrice, out var goods, out long cost, out string error))
            {
                Refund(buyer, paid, error);
                VendBrowse(buyer, owner); // show what's there now
                return;
            }

            int id = _nextSale++;
            _sales[id] = new PendingSale { Buyer = buyer, Seller = seller, Goods = goods, UnitPrice = order.UnitPrice, Cost = cost };
            seller.Connection.Send(new SocialUpdate
            {
                Kind = SocialEvent.VendDeliver, Index = id, Json = Json.Write(goods), Number = cost, Text = buyer.CharacterName,
            });
        }

        private void VendDelivered(RealmSession seller, int saleId, bool delivered, string error)
        {
            if (!_sales.TryGetValue(saleId, out var sale) || sale.Seller != seller)
            {
                return;
            }

            _sales.Remove(saleId);
            if (!delivered)
            {
                if (seller.Stall != null)
                {
                    VendingRules.PutBack(seller.Stall, sale.Goods, sale.UnitPrice);
                }

                Refund(sale.Buyer, sale.Cost, "The seller couldn't hand it over. Your zeny is back.");
                return;
            }

            if (sale.Buyer != null && !sale.Buyer.Disconnected)
            {
                sale.Buyer.Connection.Send(new SocialUpdate
                {
                    Kind = SocialEvent.VendReceived, Json = Json.Write(sale.Goods), Text = seller.CharacterName, Number = sale.Cost,
                });
            }
            else
            {
                Debug.LogWarning($"[Runeheir realm] {sale.Buyer?.CharacterName} left before receiving {sale.Goods?.DisplayName} from {seller.CharacterName}.");
            }

            seller.Connection.Send(new SocialUpdate
            {
                Kind = SocialEvent.VendSold, Text = sale.Buyer?.CharacterName, Json = Json.Write(sale.Goods), Number = sale.Cost,
            });
            if (seller.Stall != null && seller.Stall.IsSoldOut)
            {
                CloseStall(seller);
            }
        }

        private void Refund(RealmSession buyer, long zeny, string reason)
        {
            Send(buyer, new SocialUpdate { Kind = SocialEvent.Refund, Number = Math.Max(0, zeny), Text = reason });
        }
    }
}
