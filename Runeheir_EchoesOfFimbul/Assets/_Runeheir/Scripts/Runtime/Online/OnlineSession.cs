using System;
using Runeheir.Accounts;
using Runeheir.Combat;
using Runeheir.Items;
using Runeheir.Monsters;
using Runeheir.Player;
using Runeheir.Social;
using Runeheir.WorldBuilding;
using UnityEngine;

namespace Runeheir.Online
{
    /// <summary>
    /// Phase 6 seam between the game and the Mirror network layer (the Runeheir.Net assembly). Offline play leaves
    /// <see cref="Current"/> null and every system works exactly as before; online, the network layer fills it in.
    /// </summary>
    public static class OnlineSession
    {
        /// <summary>The realm this game is connected to (or hosting); null offline.</summary>
        public static IOnlineSession Current { get; set; }

        /// <summary>Starts or joins realms; registered by the network layer when it is in the build.</summary>
        public static IRealmLauncher Launcher { get; set; }

        public static bool IsOnline => Current != null;

        /// <summary>Connected to someone else's realm: the world (monsters, bosses) runs over there.</summary>
        public static bool IsRemoteClient => Current != null && !Current.IsServer;

        /// <summary>
        /// This process runs the realm's world: monsters, bosses and rewards are real here. True offline, for a host and
        /// for a dedicated server; false on a client of someone else's realm.
        /// </summary>
        public static bool RunsWorld => Current == null || Current.IsServer;
    }

    /// <summary>The connection to a realm, as the game sees it.</summary>
    public interface IOnlineSession
    {
        /// <summary>This process is the realm (host or dedicated server).</summary>
        bool IsServer { get; }

        string RealmName { get; }

        bool AllowGmCommands { get; }

        ISocialService Social { get; }

        /// <summary>A host's own copy of a map (built once, shared with the realm), or null on a remote client.</summary>
        BuiltWorld HostedWorld(string mapId);

        /// <summary>The map is up on this screen: ask the realm to put the character on it (the player object arrives later).</summary>
        void EnterMap(string mapId, Vector3 spawnPoint);

        /// <summary>Leaves for another map through the realm. False when the request can't be sent.</summary>
        bool Warp(PlayerCharacter player, string mapId, string arrivalPortalId, string message);

        /// <summary>A Dead or Blood Branch cracked here: the realm summons the monster.</summary>
        void RequestBranch(bool boss, Vector3 point);

        /// <summary>A chat line typed by the player (already parsed).</summary>
        void SendChat(ChatInput input);

        /// <summary>Back to character select: the realm takes the character out of the world (the login stays).</summary>
        void LeaveWorld();

        /// <summary>Leaves the realm (back to the realm screen).</summary>
        void Disconnect();
    }

    /// <summary>Front-end entry points: host a realm, join one, or stop.</summary>
    public interface IRealmLauncher
    {
        /// <summary>The account service of the realm we're connected to (null when not connected).</summary>
        IAccountService Accounts { get; }

        bool IsConnected { get; }

        /// <summary>This process is a headless realm server (no menus, no player of its own).</summary>
        bool IsDedicatedServer { get; }

        /// <summary>Why the last connection ended (shown once on the realm screen), or null.</summary>
        string TakeNotice();

        /// <summary>Hosts a realm on this PC and connects to it; <paramref name="done"/> gets (ok, error).</summary>
        void Host(ushort port, Action<bool, string> done);

        /// <summary>Connects to someone else's realm; <paramref name="done"/> gets (ok, error).</summary>
        void Join(string address, ushort port, Action<bool, string> done);

        /// <summary>Disconnects (and stops hosting).</summary>
        void Shutdown();
    }

    /// <summary>
    /// What the game asks of an entity it only mirrors (a monster the realm runs, another player): every change goes to the
    /// realm, which applies it to the real one and sends the result back.
    /// </summary>
    public interface IRemoteEntity
    {
        void RelayDamage(DamageResult result, CombatEntity attacker, bool physicalMelee, float poiseDamage);

        void RelayStatus(StatusEffect status, float chancePercent, float seconds, bool roll, bool ignoreImmunity);

        void RelayKnockback(Vector3 direction, float distance);

        void RelayHeal(int amount, bool sp, bool showNumber);

        void RelayCleanse();

        void RelayPoise(float amount);

        void RelayBuff(BuffDefinition buff, int level, float duration, int charges, int stackLimit);

        void RelayProvoke(CombatEntity provoker);

        /// <summary>Pilfer: the realm rolls it and sends the item (or the reason it failed) back.</summary>
        void RelaySteal(int skillLevel, int dex);
    }

    /// <summary>The realm side of another player's character: rewards and hits go to that player's own game.</summary>
    public interface IRemotePlayerLink : IRemoteEntity
    {
        void RelayExperience(long baseExp, long jobExp);

        void RelayLoot(ItemDefinition item, float baseChance, bool mvpReward);

        void RelayBreakWeapon(float chancePercent);

        void RelayMvp(string monsterName, long bonusExp);
    }

    /// <summary>The realm server watches a monster's skills through this, to show them on every player's screen.</summary>
    public interface IMonsterObserver
    {
        void CastBegan(MonsterSkill skill, CombatEntity target, Vector3 point);

        /// <summary>The cast went off (<paramref name="resolved"/>) or was interrupted.</summary>
        void CastEnded(MonsterSkill skill, CombatEntity target, Vector3 point, bool resolved);

        void PhaseChanged(int phase);
    }

    /// <summary>Parties, guilds, trades and stalls (Phase 6). The windows talk to this; the network layer implements it.</summary>
    public interface ISocialService
    {
        SocialState State { get; }

        void PartyCreate(string name);

        void PartyInvite(string name);

        void PartyRespond(bool accept);

        void PartyLeave();

        void PartyKick(string name);

        void PartyMakeLeader(string name);

        void PartySetShare(ExpShareMode mode);

        void GuildCreate(string name);

        void GuildInvite(string name);

        void GuildRespond(bool accept);

        void GuildLeave();

        void GuildExpel(string name);

        void GuildSetRank(string name, GuildRank rank);

        void GuildHandOver(string name);

        void GuildSetNotice(string notice);

        void GuildDisband();

        void TradeRequest(string name);

        void TradeRespond(bool accept);

        void TradeAddItem(ItemStack entry, int amount);

        void TradeSetZeny(long zeny);

        void TradeLock();

        void TradeConfirm();

        void TradeCancel();

        /// <summary>Opens a stall (the goods are already in the cart hold: see <see cref="VendingRules.TryOpen"/>).</summary>
        void VendOpen(VendingStall stall);

        void VendClose();

        void VendBrowse(string owner);

        void VendBuy(string owner, int index, int amount);
    }

    /// <summary>What the social windows show; the network layer updates it and raises <see cref="Changed"/>.</summary>
    public sealed class SocialState
    {
        public PartyInfo Party;
        public GuildRecord Guild;

        /// <summary>Someone invited us to their party: (inviter, party name).</summary>
        public string PartyInviteFrom;

        public string PartyInviteName;
        public int PartyInviteId;
        public string GuildInviteFrom;
        public string GuildInviteName;
        public int GuildInviteId;

        /// <summary>Someone wants to trade with us.</summary>
        public string TradeRequestFrom;

        /// <summary>The trade in progress (null when not trading).</summary>
        public TradeView Trade;

        /// <summary>Our open stall (null when not vending).</summary>
        public VendingStall MyStall;

        /// <summary>The stall we're looking at.</summary>
        public VendingStall Browsing;

        /// <summary>Who whispered us last (/r answers them).</summary>
        public string LastWhisperFrom;

        public event Action Changed;

        public void NotifyChanged()
        {
            Changed?.Invoke();
        }

        public void Clear()
        {
            Party = null;
            Guild = null;
            PartyInviteFrom = PartyInviteName = GuildInviteFrom = GuildInviteName = TradeRequestFrom = null;
            Trade = null;
            MyStall = null;
            Browsing = null;
            LastWhisperFrom = null;
            NotifyChanged();
        }
    }

    public sealed class TradeView
    {
        public string Partner;
        public TradeOffer Mine = new TradeOffer();
        public TradeOffer Theirs = new TradeOffer();
    }

    /// <summary>Things the realm's own systems announce or hook (filled in by the network layer on a realm server).</summary>
    public static class RealmHooks
    {
        /// <summary>
        /// A kill's EXP for one player: the realm may split it with their Even Share party. Return true when handled.
        /// </summary>
        public static Func<PlayerEntity, long, long, bool> ShareExperience;

        /// <summary>A line every player on the monster's map should see (boss shouts, MVP announcements).</summary>
        public static Action<CombatEntity, string> MapNotice;

        /// <summary>Monsters created on a realm server (summons, branches, spawners) are given to the network here.</summary>
        public static Action<GameObject> MonsterBuilding;

        public static Action<Monster> MonsterCreated;
    }
}
