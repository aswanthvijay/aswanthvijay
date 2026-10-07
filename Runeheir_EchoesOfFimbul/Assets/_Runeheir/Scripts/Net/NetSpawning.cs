using System.Collections.Generic;
using Mirror;
using Runeheir.Field;
using Runeheir.Monsters;
using Runeheir.Session;
using UnityEngine;

namespace Runeheir.Net
{
    /// <summary>
    /// Everything networked is built in code (no prefabs), so each kind of object has an asset id and a spawn handler that
    /// builds it the same way on every machine: players, and one id per monster kind. Network components are added while
    /// the object is still asleep, in the same order everywhere, so Mirror lines them up.
    /// </summary>
    internal static class NetSpawning
    {
        public const uint PlayerAsset = 0x52480001;

        private static readonly Dictionary<uint, MonsterDefinition> MonstersByAsset = new Dictionary<uint, MonsterDefinition>();
        private static readonly Dictionary<string, uint> AssetByMonster = new Dictionary<string, uint>();

        /// <summary>A stable id per monster kind (FNV-1a of its catalog id, top bit set so it never meets the player id).</summary>
        public static uint MonsterAsset(string monsterId)
        {
            Build();
            return monsterId != null && AssetByMonster.TryGetValue(monsterId, out uint asset) ? asset : 0;
        }

        public static void RegisterClientHandlers()
        {
            Build();
            NetworkClient.UnregisterSpawnHandler(PlayerAsset);
            NetworkClient.RegisterSpawnHandler(PlayerAsset, SpawnPlayer, Unspawn);
            foreach (var pair in MonstersByAsset)
            {
                var definition = pair.Value;
                NetworkClient.UnregisterSpawnHandler(pair.Key);
                NetworkClient.RegisterSpawnHandler(pair.Key, message => SpawnMonster(definition, message), Unspawn);
            }
        }

        public static void UnregisterClientHandlers()
        {
            NetworkClient.UnregisterSpawnHandler(PlayerAsset);
            foreach (uint asset in MonstersByAsset.Keys)
            {
                NetworkClient.UnregisterSpawnHandler(asset);
            }
        }

        /// <summary>A player's network parts: state (owner → realm → everyone) and movement (owner-driven).</summary>
        public static void AddPlayerComponents(GameObject go)
        {
            var player = go.AddComponent<NetPlayer>();
            player.syncDirection = SyncDirection.ClientToServer;
            player.syncInterval = 0.1f;
            var transform = go.AddComponent<NetworkTransformUnreliable>();
            transform.syncDirection = SyncDirection.ClientToServer;
            transform.syncInterval = 0.05f;
            transform.syncScale = false;
            go.AddComponent<NetworkIdentity>();
        }

        /// <summary>A monster's network parts: its state and movement, both run by the realm.</summary>
        public static void AddMonsterComponents(GameObject go)
        {
            var monster = go.AddComponent<NetMonster>();
            monster.syncDirection = SyncDirection.ServerToClient;
            monster.syncInterval = 0.1f;
            var transform = go.AddComponent<NetworkTransformUnreliable>();
            transform.syncDirection = SyncDirection.ServerToClient;
            transform.syncInterval = 0.1f;
            transform.syncScale = false;
            go.AddComponent<NetworkIdentity>();
        }

        private static GameObject SpawnPlayer(SpawnMessage message)
        {
            if (message.isLocalPlayer)
            {
                // Our own character: the full one, from the record this game is playing.
                var record = GameSession.Instance.ActiveCharacter;
                var field = FieldBootstrap.Active;
                if (record != null && field != null)
                {
                    return field.CreatePlayerObject(record, message.position, AddPlayerComponents).gameObject;
                }

                Debug.LogWarning("[Runeheir] The realm sent our character, but no map is up to take it.");
            }

            var remote = EntityFactory.CreateRemotePlayer(null, message.position, AddPlayerComponents);
            remote.transform.rotation = message.rotation;
            return remote.gameObject;
        }

        private static GameObject SpawnMonster(MonsterDefinition definition, SpawnMessage message)
        {
            var monster = EntityFactory.CreateMonster(definition, message.position, message.rotation.eulerAngles.y, mirror: true,
                beforeActivate: AddMonsterComponents);
            return monster.gameObject;
        }

        private static void Unspawn(GameObject spawned)
        {
            if (spawned != null)
            {
                Object.Destroy(spawned);
            }
        }

        private static void Build()
        {
            if (MonstersByAsset.Count > 0)
            {
                return;
            }

            foreach (var definition in MonsterCatalog.All)
            {
                uint asset = Fnv("monster:" + definition.Id) | 0x80000000u;
                while (MonstersByAsset.ContainsKey(asset) || asset == PlayerAsset)
                {
                    asset++; // never happens with today's catalog, but two ids must never share one
                }

                MonstersByAsset[asset] = definition;
                AssetByMonster[definition.Id] = asset;
            }
        }

        private static uint Fnv(string text)
        {
            uint hash = 2166136261u;
            foreach (char c in text)
            {
                hash ^= c;
                hash *= 16777619u;
            }

            return hash;
        }
    }

    /// <summary>
    /// Players see what's on their own map. Every networked thing belongs to the map whose square it stands in
    /// (<see cref="World.WorldGrid"/>), so a warp to another map is all it takes to change what you see.
    /// </summary>
    public sealed class MapInterestManagement : InterestManagement
    {
        private const float RebuildInterval = 1f;

        private double _nextRebuild;

        public override bool OnCheckObserver(NetworkIdentity identity, NetworkConnectionToClient newObserver)
        {
            var viewer = newObserver?.identity;
            return viewer != null && SameMap(identity, viewer);
        }

        public override void OnRebuildObservers(NetworkIdentity identity, HashSet<NetworkConnectionToClient> newObservers)
        {
            foreach (var connection in NetworkServer.connections.Values)
            {
                var viewer = connection?.identity;
                if (connection != null && connection.isReady && viewer != null && SameMap(identity, viewer))
                {
                    newObservers.Add(connection);
                }
            }
        }

        [ServerCallback]
        private void Update()
        {
            if (NetworkTime.localTime >= _nextRebuild)
            {
                _nextRebuild = NetworkTime.localTime + RebuildInterval;
                RebuildAll();
            }
        }

        private static bool SameMap(NetworkIdentity a, NetworkIdentity b)
        {
            var mapA = World.WorldGrid.MapAt(a.transform.position.x, a.transform.position.z);
            var mapB = World.WorldGrid.MapAt(b.transform.position.x, b.transform.position.z);
            return mapA != null && mapA == mapB;
        }
    }
}
