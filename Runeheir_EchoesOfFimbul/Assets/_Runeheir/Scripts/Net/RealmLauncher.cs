using System;
using System.IO;
using kcp2k;
using Mirror;
using Runeheir.Accounts;
using Runeheir.Online;
using Runeheir.Session;
using Runeheir.Social;
using UnityEngine;

namespace Runeheir.Net
{
    /// <summary>
    /// Mirror's NetworkManager for Runeheir: no prefabs, scenes or automatic player objects (the realm places characters
    /// itself when a map asks), KCP over UDP, and a <see cref="RealmServer"/> whenever this process serves.
    /// </summary>
    public sealed class RealmNetworkManager : NetworkManager
    {
        internal RealmServer Realm { get; private set; }

        internal string DataDirectory { get; set; }

        public override void OnStartServer()
        {
            base.OnStartServer();
            Realm = new RealmServer(DataDirectory);
            Realm.Start();
        }

        public override void OnStopServer()
        {
            Realm?.Stop();
            Realm = null;
            base.OnStopServer();
        }

        public override void OnServerConnect(NetworkConnectionToClient conn)
        {
            base.OnServerConnect(conn);
            if (Realm != null && NetworkServer.connections.Count > Realm.Config.MaxPlayers && !(conn is LocalConnectionToClient))
            {
                conn.Disconnect(); // full
                return;
            }

            Realm?.OnConnected(conn);
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            Realm?.OnDisconnected(conn);
            base.OnServerDisconnect(conn);
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            // Characters enter through EnterMapRequest, once their map is up.
        }

        public override void OnClientConnect()
        {
            // No automatic Ready/AddPlayer: the realm spawns the character when the map asks for it.
            RealmLauncher.Instance?.ClientConnected();
        }

        public override void OnClientDisconnect()
        {
            RealmLauncher.Instance?.ClientDisconnected();
        }

        public override void Update()
        {
            base.Update();
            Realm?.Update();
        }
    }

    /// <summary>
    /// Starts and stops realms for the front end (<see cref="IRealmLauncher"/>): host one on this PC, join one by address,
    /// or run one headless (<see cref="DedicatedServer"/>).
    /// </summary>
    public sealed class RealmLauncher : IRealmLauncher
    {
        private RealmNetworkManager _manager;
        private NetworkAccountService _accounts;
        private RealmClient _client;
        private Action<bool, string> _pending;
        private string _target;
        private string _notice;
        private bool _shuttingDown;

        public static RealmLauncher Instance { get; private set; }

        public IAccountService Accounts => _client != null ? _accounts : null;

        public bool IsConnected => NetworkClient.isConnected;

        public bool IsDedicatedServer { get; private set; }

        /// <summary>Default home for a realm's files: accounts, guilds and realm.json.</summary>
        public static string DefaultDataDirectory => DataDirectoryOverride ?? Path.Combine(Application.persistentDataPath, "realm");

        /// <summary>Tests point hosted realms at a scratch folder.</summary>
        public static string DataDirectoryOverride { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Register()
        {
            Instance = new RealmLauncher();
            OnlineSession.Launcher = Instance;
            OnlineSession.Current = null;
        }

        public void Host(ushort port, Action<bool, string> done)
        {
            Shutdown();
            var manager = Manager(port, DefaultDataDirectory);
            _pending = done;
            _target = $"your realm on port {port}";
            try
            {
                manager.StartHost();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Finish(false, $"Couldn't host on port {port}: {exception.Message}");
                Shutdown();
            }
        }

        public void Join(string address, ushort port, Action<bool, string> done)
        {
            Shutdown();
            var manager = Manager(port, DefaultDataDirectory);
            manager.networkAddress = address;
            _pending = done;
            _target = $"{address}:{port}";
            try
            {
                manager.StartClient();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Finish(false, $"Couldn't connect to {_target}: {exception.Message}");
                Shutdown();
            }
        }

        /// <summary>A headless realm: no player, no menus, just the world and its connections.</summary>
        public void StartDedicated(string dataDirectory, ushort portOverride)
        {
            IsDedicatedServer = true;
            Directory.CreateDirectory(dataDirectory);
            var config = ReadConfig(dataDirectory);
            ushort port = portOverride != 0 ? portOverride : config.Port;
            var manager = Manager(port, dataDirectory);
            manager.maxConnections = Mathf.Max(1, config.MaxPlayers + 4);
            manager.StartServer();
            Debug.Log($"[Runeheir realm] Dedicated server listening on UDP {port}. Data: {dataDirectory}");
        }

        public void Shutdown()
        {
            if (_shuttingDown)
            {
                return;
            }

            _shuttingDown = true;
            try
            {
                if (_manager != null)
                {
                    if (NetworkServer.active && NetworkClient.isConnected)
                    {
                        _manager.StopHost();
                    }
                    else if (NetworkServer.active)
                    {
                        _manager.StopServer();
                    }
                    else if (NetworkClient.active)
                    {
                        _manager.StopClient();
                    }
                }
            }
            finally
            {
                DropClient();
                _pending = null;
                _shuttingDown = false;
            }
        }

        public string TakeNotice()
        {
            string notice = _notice;
            _notice = null;
            return notice;
        }

        internal void ClientConnected()
        {
            DropClient();
            _accounts = new NetworkAccountService();
            _client = new RealmClient(_accounts);
            _client.Start();
            OnlineSession.Current = _client;
            Finish(true, null);
        }

        internal void ClientDisconnected()
        {
            bool wasPlaying = _client != null && _pending == null;
            DropClient();
            if (_pending != null)
            {
                Finish(false, $"Couldn't reach {_target}. Check the address, the port and the host's firewall (UDP).");
                return;
            }

            if (wasPlaying && !_shuttingDown)
            {
                // Lost the realm mid-game: back to the realm screen, saying why.
                _notice = "Disconnected from the realm.";
                GameSession.Instance.Logout();
                SceneFlow.LoadLogin();
            }
        }

        private void DropClient()
        {
            _client?.Stop();
            _client = null;
            _accounts = null;
            OnlineSession.Current = null;
        }

        private void Finish(bool ok, string error)
        {
            var callback = _pending;
            _pending = null;
            callback?.Invoke(ok, error);
        }

        private RealmNetworkManager Manager(ushort port, string dataDirectory)
        {
            if (_manager == null)
            {
                var go = new GameObject("[Realm Network]");
                go.SetActive(false);
                var transport = go.AddComponent<KcpTransport>();
                go.AddComponent<MapInterestManagement>();
                _manager = go.AddComponent<RealmNetworkManager>();
                _manager.transport = transport;
                _manager.autoCreatePlayer = false;
                _manager.dontDestroyOnLoad = true;
                _manager.runInBackground = true;
                _manager.sendRate = 30;
                _manager.maxConnections = 128;
                go.SetActive(true);
            }

            if (_manager.transport is KcpTransport kcp)
            {
                kcp.Port = port;
            }

            _manager.DataDirectory = dataDirectory;
            return _manager;
        }

        private static RealmConfig ReadConfig(string directory)
        {
            string path = Path.Combine(directory, RealmServer.ConfigFile);
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
                Debug.LogWarning($"[Runeheir realm] {path}: {exception.Message}");
            }

            config = config ?? new RealmConfig();
            config.Sanitize();
            return config;
        }
    }

    /// <summary>
    /// Runs the game as a headless realm server: build for Linux/Windows "Dedicated Server" (Runeheir ▸ Build ▸ Realm
    /// Server), or start any build with <c>-server</c>. Options: <c>-realmData &lt;folder&gt;</c> for its files,
    /// <c>-port &lt;number&gt;</c> to override realm.json.
    /// </summary>
    public static class DedicatedServer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            string[] args = Environment.GetCommandLineArgs();
            bool requested = Array.IndexOf(args, "-server") >= 0;
#if UNITY_SERVER
            requested = true;
#endif
            if (!requested || RealmLauncher.Instance == null || RealmLauncher.Instance.IsDedicatedServer)
            {
                return;
            }

            string data = Argument(args, "-realmData") ?? RealmLauncher.DefaultDataDirectory;
            ushort.TryParse(Argument(args, "-port"), out ushort port);
            Application.targetFrameRate = 30;
            RealmLauncher.Instance.StartDedicated(data, port);
        }

        private static string Argument(string[] args, string name)
        {
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
