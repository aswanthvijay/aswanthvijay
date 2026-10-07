using System;
using System.Collections.Generic;
using Mirror;
using Runeheir.Accounts;
using Runeheir.Characters;
using Runeheir.Items;
using UnityEngine;

namespace Runeheir.Net
{
    // Every message is a flat struct of strings and numbers; anything richer travels as JSON. That keeps the wire format
    // easy to read and keeps Mirror's code generation to the simplest types.

    public enum AccountOp : byte
    {
        Register = 1,
        Login = 2,
        Logout = 3,
        Servers = 4,
        Characters = 5,
        Create = 6,
        Delete = 7,
        Save = 8,
        Storage = 9,
        SaveWithStorage = 10,
    }

    /// <summary>Client → realm: an account or character-server request (answered by <see cref="AccountReply"/>).</summary>
    public struct AccountRequest : NetworkMessage
    {
        public int Id;
        public AccountOp Op;
        public int Protocol;
        public string Text1;
        public string Text2;
        public int Slot;
        public string Json;
        public string Json2;
    }

    public struct AccountReply : NetworkMessage
    {
        public int Id;
        public bool Ok;
        public string Error;
        public string Value;
        public string Json;

        /// <summary>Realm settings sent with a successful login.</summary>
        public bool AllowGm;

        public string RealmName;
    }

    /// <summary>Client → realm: our map is built, put the character on it.</summary>
    public struct EnterMapRequest : NetworkMessage
    {
        public string MapId;
        public Vector3 Position;
        public int Slot;
        public string RecordJson;
    }

    /// <summary>Client → realm: travel to another map (the character record comes along, saved first).</summary>
    public struct WarpRequest : NetworkMessage
    {
        public string MapId;
        public string PortalId;
        public int Slot;
        public string RecordJson;
    }

    /// <summary>Realm → client: the warp was accepted (load the map) or refused.</summary>
    public struct WarpReply : NetworkMessage
    {
        public bool Ok;
        public string Error;
        public string MapId;
    }

    /// <summary>Client → realm: back to character select.</summary>
    public struct LeaveWorldRequest : NetworkMessage
    {
    }

    /// <summary>Realm → client: out of the world (character select can load).</summary>
    public struct LeftWorld : NetworkMessage
    {
    }

    /// <summary>Client → realm: a Dead or Blood Branch was cracked here.</summary>
    public struct BranchRequest : NetworkMessage
    {
        public bool Boss;
        public Vector3 Position;
    }

    public struct ChatSend : NetworkMessage
    {
        public byte Channel;
        public string Target;
        public string Text;
    }

    public struct ChatDeliver : NetworkMessage
    {
        public byte Channel;
        public string From;
        public string Text;

        /// <summary>Our own whisper, echoed back as "(To X)".</summary>
        public bool Outgoing;
    }

    /// <summary>Client → realm: a party, guild, trade or stall action (see <see cref="SocialOp"/>).</summary>
    public struct SocialRequest : NetworkMessage
    {
        public SocialOp Op;
        public string Text;
        public long Number;
        public int Index;
        public string Json;
    }

    /// <summary>Realm → client: social state and the steps of trades and stall sales.</summary>
    public struct SocialUpdate : NetworkMessage
    {
        public SocialEvent Kind;
        public string Text;
        public string Text2;
        public long Number;
        public int Index;
        public string Json;
        public string Json2;
    }

    public enum SocialOp : byte
    {
        PartyCreate = 1,
        PartyInvite = 2,
        PartyRespond = 3,
        PartyLeave = 4,
        PartyKick = 5,
        PartyLeader = 6,
        PartyShare = 7,

        GuildCreate = 20,
        GuildInvite = 21,
        GuildRespond = 22,
        GuildLeave = 23,
        GuildExpel = 24,
        GuildRank = 25,
        GuildHandOver = 26,
        GuildNotice = 27,
        GuildDisband = 28,

        TradeRequest = 40,
        TradeRespond = 41,
        TradeAdd = 42,
        TradeZeny = 43,
        TradeLock = 44,
        TradeConfirm = 45,
        TradeCancel = 46,

        /// <summary>The trade's "give" step went through on our side (Number 1) or failed (0).</summary>
        TradeGave = 47,

        VendOpen = 60,
        VendClose = 61,
        VendBrowse = 62,
        VendBuy = 63,

        /// <summary>The seller handed over a sale (Index = sale id, Number 1/0).</summary>
        VendDelivered = 64,
    }

    public enum SocialEvent : byte
    {
        Message = 1,
        Error = 2,

        Party = 10,
        PartyInvite = 11,
        Guild = 20,
        GuildInvite = 21,

        /// <summary>Zeny back (a guild that couldn't be founded, a sale that fell through).</summary>
        Refund = 22,

        TradeAsk = 40,
        TradeState = 41,
        TradeClosed = 42,

        /// <summary>Both confirmed: give your side (Json = your offer).</summary>
        TradeGive = 43,

        /// <summary>Take this (the partner's side, or your own back after a failed trade).</summary>
        TradeReceive = 44,

        StallOpened = 60,
        StallClosed = 61,
        StallView = 62,

        /// <summary>Seller: hand over these goods (Index = sale id, Json = goods, Number = price).</summary>
        VendDeliver = 63,

        /// <summary>Buyer: your goods arrived (Json = goods).</summary>
        VendReceived = 64,

        /// <summary>Seller: a sale went through (Text = buyer, Json = goods, Number = zeny).</summary>
        VendSold = 65,
    }

    [Serializable]
    internal sealed class CharacterListJson
    {
        public List<CharacterRecord> Items = new List<CharacterRecord>();
    }

    [Serializable]
    internal sealed class ItemListJson
    {
        public List<ItemStack> Items = new List<ItemStack>();
    }

    [Serializable]
    internal sealed class ServerListJson
    {
        public List<ServerInfo> Items = new List<ServerInfo>();
    }

    [Serializable]
    internal sealed class CreateRequestJson
    {
        public string Name;
        public int Gender;
        public int HairStyle;
        public int HairColor;
        public int Race;
    }

    /// <summary>One side of a trade as it travels (JsonUtility needs a wrapper class).</summary>
    [Serializable]
    internal sealed class TradeStateJson
    {
        public string Partner;
        public Social.TradeOffer Mine = new Social.TradeOffer();
        public Social.TradeOffer Theirs = new Social.TradeOffer();
    }

    internal static class Json
    {
        public static string Write(object value)
        {
            return value == null ? string.Empty : JsonUtility.ToJson(value);
        }

        /// <summary>Parses <paramref name="text"/>; null when it's empty or broken (never throws).</summary>
        public static T Read<T>(string text) where T : class
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<T>(text);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
