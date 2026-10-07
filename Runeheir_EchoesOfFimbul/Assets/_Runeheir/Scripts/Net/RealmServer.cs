using System;
using System.Collections.Generic;
using System.IO;
using Mirror;
using Runeheir.Accounts;
using Runeheir.Characters;
using Runeheir.Combat;
using Runeheir.Field;
using Runeheir.Monsters;
using Runeheir.Online;
using Runeheir.Session;
using Runeheir.Social;
using Runeheir.Visuals;
using Runeheir.World;
using Runeheir.WorldBuilding;
using UnityEngine;

namespace Runeheir.Net
{
    /// <summary>One connection as the realm sees it: the account logged in on it and the character it plays.</summary>
    internal sealed class RealmSession
    {
        public NetworkConnectionToClient Connection;
        public string Username;
        public int Slot = -1;
        public string CharacterName;
        public NetPlayer Player;
        public string MapId;
        public CharacterRecord Record;
        public int LoginFailures;
        public bool Greeted;
        public readonly ChatFloodGate Flood = new ChatFloodGate();

        /// <summary>The trade this character is in (or asked for).</summary>
        public TradeSession Trade;

        public string TradeAskedBy;

        /// <summary>Their open street stall, as the realm lists it.</summary>
        public VendingStall Stall;

        /// <summary>Where the stall was set up: walking away from it closes it.</summary>
        public Vector3 StallSpot;

        public bool IsLocal => Connection is LocalConnectionToClient;

        /// <summary>The connection is going away: nothing more is sent to it.</summary>
        public bool Disconnected;

        public bool InWorld => Player != null && !string.IsNullOrEmpty(CharacterName);

        public PlayerEntity Entity => Player != null ? Player.Entity : null;
    }

    /// <summary>
    /// The realm (Phase 6): login and character server, map server and social hub in one process, either hosted by a
    /// player or run headless. It keeps accounts and guilds on disk, builds each map once players arrive, runs every
    /// monster, and passes chat, parties, trades and stall sales between players.
    /// </summary>
    public sealed partial class RealmServer
    {
        public const string ConfigFile = "realm.json";
        public const string AccountsFile = "realm_accounts.json";
        public const string GuildsFile = "realm_guilds.json";

        private readonly Dictionary<int, RealmSession> _sessions = new Dictionary<int, RealmSession>();
        private readonly Dictionary<string, BuiltWorld> _maps = new Dictionary<string, BuiltWorld>(StringComparer.OrdinalIgnoreCase);
        private readonly LocalAccountService _accounts;
        private readonly string _dataDirectory;
        private float _nextRefresh;

        public RealmServer(string dataDirectory)
        {
            _dataDirectory = dataDirectory;
            Directory.CreateDirectory(dataDirectory);
            Config = LoadConfig(dataDirectory);
            _accounts = new LocalAccountService(Path.Combine(dataDirectory, AccountsFile));
            _guilds = new GuildBook(LoadGuilds(), SaveGuilds);
        }

        /// <summary>The realm running in this process (null when none).</summary>
        public static RealmServer Instance { get; private set; }

        public RealmConfig Config { get; }

        public string DataDirectory => _dataDirectory;

        public int PlayersInWorld
        {
            get
            {
                int count = 0;
                foreach (var session in _sessions.Values)
                {
                    if (session.InWorld)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public void Start()
        {
            Instance = this;
            ServerRates.Current = Config.Rates();
            NetworkServer.RegisterHandler<AccountRequest>(OnAccountRequest);
            NetworkServer.RegisterHandler<EnterMapRequest>(OnEnterMap);
            NetworkServer.RegisterHandler<WarpRequest>(OnWarp);
            NetworkServer.RegisterHandler<LeaveWorldRequest>(OnLeaveWorld);
            NetworkServer.RegisterHandler<BranchRequest>(OnBranch);
            NetworkServer.RegisterHandler<ChatSend>(OnChat);
            NetworkServer.RegisterHandler<SocialRequest>(OnSocial);

            RealmHooks.ShareExperience = ShareExperience;
            RealmHooks.MapNotice = MapNotice;
            RealmHooks.MonsterBuilding = NetSpawning.AddMonsterComponents;
            RealmHooks.MonsterCreated = SpawnMonster;
            if (_accounts.StorageError != null)
            {
                Debug.LogError("[Runeheir realm] " + _accounts.StorageError);
            }

            Debug.Log($"[Runeheir realm] '{Config.Name}' is up. Data: {_dataDirectory}");
        }

        public void Stop()
        {
            NetworkServer.UnregisterHandler<AccountRequest>();
            NetworkServer.UnregisterHandler<EnterMapRequest>();
            NetworkServer.UnregisterHandler<WarpRequest>();
            NetworkServer.UnregisterHandler<LeaveWorldRequest>();
            NetworkServer.UnregisterHandler<BranchRequest>();
            NetworkServer.UnregisterHandler<ChatSend>();
            NetworkServer.UnregisterHandler<SocialRequest>();
            RealmHooks.ShareExperience = null;
            RealmHooks.MapNotice = null;
            RealmHooks.MonsterBuilding = null;
            RealmHooks.MonsterCreated = null;

            foreach (var world in _maps.Values)
            {
                if (world?.Root != null)
                {
                    UnityEngine.Object.Destroy(world.Root.gameObject);
                }
            }

            _maps.Clear();
            _sessions.Clear();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void OnConnected(NetworkConnectionToClient connection)
        {
            _sessions[connection.connectionId] = new RealmSession { Connection = connection };
        }

        public void OnDisconnected(NetworkConnectionToClient connection)
        {
            if (_sessions.TryGetValue(connection.connectionId, out var session))
            {
                session.Disconnected = true;
                RemoveFromWorld(session, disconnecting: true);
                _sessions.Remove(connection.connectionId);
            }
        }

        public void Update()
        {
            if (Time.unscaledTime < _nextRefresh)
            {
                return;
            }

            _nextRefresh = Time.unscaledTime + 1f;
            RefreshSocial();
        }

        // ================================================================ maps
        /// <summary>The realm's copy of a map, built (with its portals, NPCs, monsters and bosses) the first time it's needed.</summary>
        public BuiltWorld EnsureMap(string mapId)
        {
            var map = MapCatalog.Get(mapId);
            if (map == null)
            {
                return null;
            }

            if (_maps.TryGetValue(map.Id, out var world) && world?.Root != null)
            {
                return world;
            }

            var origin = WorldGrid.Origin(map.Id);
            world = WorldBuilder.Build(map, null, new Vector3(origin.X, 0f, origin.Z), applyAtmosphere: false);
            UnityEngine.Object.DontDestroyOnLoad(world.Root.gameObject); // a host's scene changes must not take the realm's maps along
            WorldPopulator.Populate(world, monsters: true);
            _maps[map.Id] = world;
            Debug.Log($"[Runeheir realm] Map up: {map.Name}");
            return world;
        }

        private void SpawnMonster(Monster monster)
        {
            if (monster == null || monster.Definition == null)
            {
                return;
            }

            UnityEngine.Object.DontDestroyOnLoad(monster.gameObject);
            NetworkServer.Spawn(monster.gameObject, NetSpawning.MonsterAsset(monster.Definition.Id));
        }

        // ================================================================ accounts (login + character server)
        private async void OnAccountRequest(NetworkConnectionToClient connection, AccountRequest request)
        {
            var session = Session(connection);
            if (session == null)
            {
                return;
            }

            var reply = new AccountReply { Id = request.Id };
            if (request.Protocol != RealmConfig.ProtocolVersion)
            {
                reply.Error = "This realm runs a different version of Runeheir. Update the game (or the realm) and try again.";
                connection.Send(reply);
                return;
            }

            try
            {
                switch (request.Op)
                {
                    case AccountOp.Register:
                    {
                        if (!Config.AllowRegistration)
                        {
                            reply.Error = "This realm isn't taking new accounts.";
                            break;
                        }

                        var result = await _accounts.RegisterAsync(request.Text1, request.Text2);
                        reply.Ok = result.Success;
                        reply.Error = result.Error;
                        reply.Value = result.Value;
                        break;
                    }

                    case AccountOp.Login:
                    {
                        var result = await _accounts.LoginAsync(request.Text1, request.Text2);
                        if (!Alive(session))
                        {
                            return;
                        }

                        if (!result.Success)
                        {
                            reply.Error = result.Error;
                            if (++session.LoginFailures >= 8 && !session.IsLocal)
                            {
                                connection.Send(reply);
                                connection.Disconnect(); // someone guessing passwords
                                return;
                            }

                            break;
                        }

                        // One login per account: an older session (a crashed game) is closed.
                        foreach (var other in new List<RealmSession>(_sessions.Values))
                        {
                            if (other != session && string.Equals(other.Username, result.Value, StringComparison.OrdinalIgnoreCase))
                            {
                                RemoveFromWorld(other, disconnecting: false);
                                other.Username = null;
                                if (!other.IsLocal)
                                {
                                    other.Connection.Disconnect();
                                }
                            }
                        }

                        RemoveFromWorld(session, disconnecting: false);
                        session.Username = result.Value;
                        session.LoginFailures = 0;
                        reply.Ok = true;
                        reply.Value = result.Value;
                        reply.RealmName = Config.Name;
                        reply.AllowGm = Config.AllowGmCommands;
                        break;
                    }

                    case AccountOp.Logout:
                        RemoveFromWorld(session, disconnecting: false);
                        session.Username = null;
                        reply.Ok = true;
                        break;

                    case AccountOp.Servers:
                    {
                        var list = new ServerListJson();
                        list.Items.Add(new ServerInfo
                        {
                            Id = "realm",
                            Name = Config.Name,
                            Description = string.IsNullOrEmpty(Config.Motd) ? "Online realm" : Config.Motd,
                            Online = true,
                            Population = PlayersInWorld,
                        });
                        reply.Ok = true;
                        reply.Json = Json.Write(list);
                        break;
                    }

                    case AccountOp.Characters:
                    {
                        if (!RequireLogin(session, ref reply))
                        {
                            break;
                        }

                        var characters = await _accounts.GetCharactersAsync(session.Username);
                        reply.Ok = true;
                        reply.Json = Json.Write(new CharacterListJson { Items = characters ?? new List<CharacterRecord>() });
                        break;
                    }

                    case AccountOp.Create:
                    {
                        if (!RequireLogin(session, ref reply))
                        {
                            break;
                        }

                        var create = Json.Read<CreateRequestJson>(request.Json);
                        if (create == null)
                        {
                            reply.Error = "Bad character data.";
                            break;
                        }

                        var result = await _accounts.CreateCharacterAsync(session.Username, request.Slot, new CharacterCreateRequest
                        {
                            Name = create.Name,
                            Gender = create.Gender == (int)Gender.Female ? Gender.Female : Gender.Male,
                            HairStyle = create.HairStyle,
                            HairColor = create.HairColor,
                        });
                        reply.Ok = result.Success;
                        reply.Error = result.Error;
                        reply.Json = result.Success ? Json.Write(result.Value) : null;
                        break;
                    }

                    case AccountOp.Delete:
                    {
                        if (!RequireLogin(session, ref reply))
                        {
                            break;
                        }

                        if (session.InWorld && session.Slot == request.Slot)
                        {
                            reply.Error = "That character is in the world right now.";
                            break;
                        }

                        string name = null;
                        foreach (var character in await _accounts.GetCharactersAsync(session.Username))
                        {
                            if (character.Slot == request.Slot)
                            {
                                name = character.Name;
                            }
                        }

                        var result = await _accounts.DeleteCharacterAsync(session.Username, request.Slot, request.Text1);
                        reply.Ok = result.Success;
                        reply.Error = result.Error;
                        if (result.Success && name != null)
                        {
                            ForgetCharacter(name);
                        }

                        break;
                    }

                    case AccountOp.Save:
                    {
                        if (!RequireLogin(session, ref reply))
                        {
                            break;
                        }

                        var record = Json.Read<CharacterRecord>(request.Json);
                        if (record == null)
                        {
                            reply.Error = "Bad character data.";
                            break;
                        }

                        var result = await _accounts.SaveCharacterAsync(session.Username, record);
                        reply.Ok = result.Success;
                        reply.Error = result.Error;
                        if (result.Success && Alive(session) && session.InWorld && string.Equals(record.Name, session.CharacterName, StringComparison.Ordinal))
                        {
                            session.Record = record;
                        }

                        break;
                    }

                    case AccountOp.Storage:
                    {
                        if (!RequireLogin(session, ref reply))
                        {
                            break;
                        }

                        var result = await _accounts.GetStorageAsync(session.Username);
                        reply.Ok = result.Success;
                        reply.Error = result.Error;
                        reply.Json = result.Success ? Json.Write(new ItemListJson { Items = result.Value }) : null;
                        break;
                    }

                    case AccountOp.SaveWithStorage:
                    {
                        if (!RequireLogin(session, ref reply))
                        {
                            break;
                        }

                        var record = Json.Read<CharacterRecord>(request.Json);
                        var storage = Json.Read<ItemListJson>(request.Json2);
                        if (record == null || storage == null)
                        {
                            reply.Error = "Bad character data.";
                            break;
                        }

                        var result = await _accounts.SaveCharacterAndStorageAsync(session.Username, record, storage.Items ?? new List<Items.ItemStack>());
                        reply.Ok = result.Success;
                        reply.Error = result.Error;
                        break;
                    }

                    default:
                        reply.Error = "Unknown request.";
                        break;
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                reply.Ok = false;
                reply.Error = "The realm hit an error: " + exception.Message;
            }

            if (Alive(session))
            {
                connection.Send(reply);
            }
        }

        private static bool RequireLogin(RealmSession session, ref AccountReply reply)
        {
            if (!string.IsNullOrEmpty(session.Username))
            {
                return true;
            }

            reply.Error = "Not logged in.";
            return false;
        }

        // ================================================================ entering, leaving and crossing maps
        private async void OnEnterMap(NetworkConnectionToClient connection, EnterMapRequest request)
        {
            var session = Session(connection);
            if (session == null || string.IsNullOrEmpty(session.Username))
            {
                connection.Send(new WarpReply { Ok = false, Error = "Log in first." });
                return;
            }

            var record = Json.Read<CharacterRecord>(request.RecordJson);
            if (record == null)
            {
                connection.Send(new WarpReply { Ok = false, Error = "Bad character data." });
                return;
            }

            record.Slot = request.Slot;
            var saved = await _accounts.SaveCharacterAsync(session.Username, record);
            if (!Alive(session))
            {
                return;
            }

            if (!saved.Success)
            {
                connection.Send(new WarpReply { Ok = false, Error = saved.Error });
                return;
            }

            // A character already in the world (the warp's old body, a second Enter) leaves first.
            RemoveFromWorld(session, disconnecting: false, keepReady: true);

            var map = MapCatalog.Get(request.MapId) ?? MapCatalog.Get(record.MapId) ?? MapCatalog.Get(MapCatalog.StartingMapId);
            var world = EnsureMap(map.Id);
            Vector3 position = request.Position;
            if (!NetWire.Finite(position) || WorldGrid.MapAt(position.x, position.z) != map)
            {
                position = world.SavePoint;
            }

            GameObject body;
            if (session.IsLocal)
            {
                // The host's own character: the full one, playing the host's live record.
                var field = FieldBootstrap.Active;
                var local = GameSession.Instance.ActiveCharacter;
                if (field == null || local == null)
                {
                    connection.Send(new WarpReply { Ok = false, Error = "No map is up." });
                    return;
                }

                body = field.CreatePlayerObject(local, position, NetSpawning.AddPlayerComponents).gameObject;
            }
            else
            {
                body = EntityFactory.CreateRemotePlayer(AvatarLook.Code(record), position, NetSpawning.AddPlayerComponents).gameObject;
            }

            var player = body.GetComponent<NetPlayer>();
            player.Session = session;
            player.ServerSeed(record);
            UnityEngine.Object.DontDestroyOnLoad(body);
            NetworkServer.AddPlayerForConnection(connection, body, NetSpawning.PlayerAsset);

            session.Player = player;
            session.CharacterName = record.Name;
            session.Slot = record.Slot;
            session.MapId = map.Id;
            session.Record = record;
            OnEnteredWorld(session);

            if (!session.Greeted)
            {
                session.Greeted = true;
                Notify(session, ChatRules.Sanitize($"[{Config.Name}] {Config.Motd}", 220));
            }
        }

        private async void OnWarp(NetworkConnectionToClient connection, WarpRequest request)
        {
            var session = Session(connection);
            var map = MapCatalog.Get(request.MapId);
            if (session == null || !session.InWorld || map == null)
            {
                connection.Send(new WarpReply { Ok = false, Error = map == null ? "There's no such map." : "You aren't in the world." });
                return;
            }

            var record = Json.Read<CharacterRecord>(request.RecordJson);
            if (record == null)
            {
                connection.Send(new WarpReply { Ok = false, Error = "Bad character data." });
                return;
            }

            record.Slot = session.Slot;
            record.MapId = map.Id;
            var saved = await _accounts.SaveCharacterAsync(session.Username, record);
            if (!Alive(session))
            {
                return;
            }

            if (!saved.Success)
            {
                connection.Send(new WarpReply { Ok = false, Error = saved.Error });
                return;
            }

            // Trades and stalls end at the map's edge. The old body stays until the new map asks for the character
            // (so nothing on either screen loses its character mid-frame); the world stops updating this connection now.
            EndTrade(session, "The trade was cancelled: they left the map.");
            CloseStall(session);
            session.Record = record;
            EnsureMap(map.Id); // a host's scene shows the realm's copy, so it must exist before the scene loads
            if (!session.IsLocal && connection.isReady)
            {
                NetworkServer.SetClientNotReady(connection);
            }

            connection.Send(new WarpReply { Ok = true, MapId = map.Id });
        }

        private void OnLeaveWorld(NetworkConnectionToClient connection, LeaveWorldRequest request)
        {
            var session = Session(connection);
            if (session != null)
            {
                RemoveFromWorld(session, disconnecting: false);
                connection.Send(new LeftWorld());
            }
        }

        /// <summary>Takes the character out of the world (warp, character select, logout or disconnect).</summary>
        private void RemoveFromWorld(RealmSession session, bool disconnecting, bool keepReady = false)
        {
            if (session == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(session.CharacterName))
            {
                EndTrade(session, "The trade was cancelled: they left.");
                CloseStall(session);
                OnLeftWorld(session);
            }

            var connection = session.Connection;
            if (!disconnecting && connection != null)
            {
                if (connection.identity != null)
                {
                    NetworkServer.RemovePlayerForConnection(connection, RemovePlayerOptions.Destroy);
                }

                if (!keepReady && connection.isReady)
                {
                    NetworkServer.SetClientNotReady(connection);
                }
            }

            session.Player = null;
            session.CharacterName = null;
            session.MapId = null;
        }

        private void OnBranch(NetworkConnectionToClient connection, BranchRequest request)
        {
            var session = Session(connection);
            var player = session?.Entity;
            var map = session != null ? MapCatalog.Get(session.MapId) : null;
            if (player == null || map == null || !map.AllowBranches || !NetWire.Finite(request.Position)
                || CombatEntity.HorizontalDistance(player.Position, request.Position) > 8f)
            {
                return;
            }

            var definition = MonsterCatalog.PickForBranch(request.Boss, SystemRandomSource.Shared);
            if (definition == null || !UnityEngine.AI.NavMesh.SamplePosition(request.Position, out var hit, 3f, UnityEngine.AI.NavMesh.AllAreas))
            {
                return;
            }

            var monster = EntityFactory.CreateMonster(definition, hit.position, UnityEngine.Random.Range(0f, 360f));
            monster.Provoke(player);
            MapNotice(player, $"{session.CharacterName}'s branch splinters and a {definition.Name} (Lv {definition.Level}) bursts out!");
        }

        // ================================================================ checks used by the networked objects
        /// <summary>
        /// The character of <paramref name="sender"/>, if it may act at <paramref name="target"/> (alive, in the world, on
        /// that map and within reach). Stops forged packets from hitting monsters across the world.
        /// </summary>
        internal bool TryGetActor(NetworkConnectionToClient sender, Vector3 target, out PlayerEntity actor)
        {
            actor = null;
            var session = sender != null ? Session(sender) : null;
            var entity = session?.Entity;
            if (entity == null || entity.IsDead)
            {
                return false;
            }

            var here = WorldGrid.MapAt(entity.Position.x, entity.Position.z);
            if (here == null || here != WorldGrid.MapAt(target.x, target.z)
                || CombatEntity.HorizontalDistance(entity.Position, target) > NetWire.MaxActionRange)
            {
                return false;
            }

            actor = entity;
            return true;
        }

        /// <summary>True when <paramref name="sender"/>'s character may heal or buff <paramref name="target"/>.</summary>
        internal bool MayAffect(NetworkConnectionToClient sender, NetPlayer target)
        {
            return target != null && TryGetActor(sender, target.transform.position, out _);
        }

        // ================================================================ chat
        private void OnChat(NetworkConnectionToClient connection, ChatSend message)
        {
            var session = Session(connection);
            if (session == null || !session.InWorld)
            {
                return;
            }

            var channel = (ChatChannel)message.Channel;
            string text = ChatRules.Sanitize(message.Text);
            if (string.IsNullOrEmpty(text) || channel == ChatChannel.System)
            {
                return;
            }

            if (!session.Flood.Allow(channel, Time.unscaledTimeAsDouble, out string flood))
            {
                Error(session, flood);
                return;
            }

            var deliver = new ChatDeliver { Channel = (byte)channel, From = session.CharacterName, Text = text };
            switch (channel)
            {
                case ChatChannel.Local:
                    foreach (var other in _sessions.Values)
                    {
                        if (other.InWorld && string.Equals(other.MapId, session.MapId, StringComparison.OrdinalIgnoreCase))
                        {
                            other.Connection.Send(deliver);
                        }
                    }

                    break;

                case ChatChannel.Shout:
                    foreach (var other in _sessions.Values)
                    {
                        if (other.InWorld)
                        {
                            other.Connection.Send(deliver);
                        }
                    }

                    break;

                case ChatChannel.Party:
                {
                    var party = _parties.PartyOf(session.CharacterName);
                    if (party == null)
                    {
                        Error(session, "You aren't in a party. Make one in the party window (Alt+Z).");
                        return;
                    }

                    foreach (var member in party.Members)
                    {
                        Find(member.Name)?.Connection.Send(deliver);
                    }

                    break;
                }

                case ChatChannel.Guild:
                {
                    var guild = _guilds.GuildOf(session.CharacterName);
                    if (guild == null)
                    {
                        Error(session, "You aren't in a guild.");
                        return;
                    }

                    foreach (var member in guild.Members)
                    {
                        Find(member.Name)?.Connection.Send(deliver);
                    }

                    break;
                }

                case ChatChannel.Whisper:
                {
                    var target = Find(message.Target);
                    if (target == null)
                    {
                        Error(session, $"{message.Target} isn't online.");
                        return;
                    }

                    target.Connection.Send(deliver);
                    connection.Send(new ChatDeliver { Channel = (byte)ChatChannel.Whisper, From = target.CharacterName, Text = text, Outgoing = true });
                    break;
                }
            }
        }

        /// <summary>Boss shouts, MVPs and card drops: every player on that monster's map (the host saw it already).</summary>
        private void MapNotice(CombatEntity source, string line)
        {
            if (source == null || string.IsNullOrEmpty(line))
            {
                return;
            }

            var map = WorldGrid.MapAt(source.Position.x, source.Position.z);
            foreach (var session in _sessions.Values)
            {
                if (session.InWorld && !session.IsLocal && map != null && string.Equals(session.MapId, map.Id, StringComparison.OrdinalIgnoreCase))
                {
                    session.Connection.Send(new ChatDeliver { Channel = (byte)ChatChannel.System, Text = line });
                }
            }
        }

        private static void Notify(RealmSession session, string text)
        {
            Send(session, new SocialUpdate { Kind = SocialEvent.Message, Text = text });
        }

        private static void Error(RealmSession session, string text)
        {
            Send(session, new SocialUpdate { Kind = SocialEvent.Error, Text = text });
        }

        private static void Send<T>(RealmSession session, T message) where T : struct, NetworkMessage
        {
            if (session?.Connection != null && !session.Disconnected)
            {
                session.Connection.Send(message);
            }
        }

        // ================================================================ sessions
        private RealmSession Session(NetworkConnectionToClient connection)
        {
            return connection != null && _sessions.TryGetValue(connection.connectionId, out var session) ? session : null;
        }

        /// <summary>The session playing <paramref name="characterName"/> in the world, or null.</summary>
        private RealmSession Find(string characterName)
        {
            if (string.IsNullOrEmpty(characterName))
            {
                return null;
            }

            foreach (var session in _sessions.Values)
            {
                if (session.InWorld && string.Equals(session.CharacterName, characterName, StringComparison.OrdinalIgnoreCase))
                {
                    return session;
                }
            }

            return null;
        }

        /// <summary>The connection is still there after an await.</summary>
        private bool Alive(RealmSession session)
        {
            return session?.Connection != null && _sessions.TryGetValue(session.Connection.connectionId, out var current) && current == session;
        }

        // ================================================================ files
        private static RealmConfig LoadConfig(string directory)
        {
            string path = Path.Combine(directory, ConfigFile);
            RealmConfig config = null;
            try
            {
                if (File.Exists(path))
                {
                    config = JsonUtility.FromJson<RealmConfig>(File.ReadAllText(path));
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Runeheir realm] {path} couldn't be read ({exception.Message}); using defaults.");
            }

            config = config ?? new RealmConfig();
            config.Sanitize();
            try
            {
                if (!File.Exists(path))
                {
                    File.WriteAllText(path, JsonUtility.ToJson(config, true)); // a starting point to edit
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Runeheir realm] Couldn't write {path}: {exception.Message}");
            }

            return config;
        }

        private GuildDatabase LoadGuilds()
        {
            string path = Path.Combine(_dataDirectory, GuildsFile);
            try
            {
                if (File.Exists(path))
                {
                    return JsonUtility.FromJson<GuildDatabase>(File.ReadAllText(path)) ?? new GuildDatabase();
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Runeheir realm] {path} couldn't be read ({exception.Message}).");
                File.Copy(path, path + ".corrupt-" + DateTime.UtcNow.Ticks, true);
            }

            return new GuildDatabase();
        }

        private void SaveGuilds(GuildDatabase database)
        {
            string path = Path.Combine(_dataDirectory, GuildsFile);
            string temp = path + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(database, true));
            if (File.Exists(path))
            {
                File.Replace(temp, path, path + ".bak");
            }
            else
            {
                File.Move(temp, path);
            }
        }
    }
}
