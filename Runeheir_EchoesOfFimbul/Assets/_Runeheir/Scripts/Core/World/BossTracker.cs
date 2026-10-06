using System;
using System.Collections.Generic;
using Runeheir.Combat;

namespace Runeheir.World
{
    /// <summary>One map boss's timer: alive, or dead until <see cref="RespawnAt"/>.</summary>
    public sealed class BossStatus
    {
        public string MapId;
        public string MonsterId;
        public bool Alive = true;

        /// <summary>World-clock seconds of the last kill and of the earliest respawn.</summary>
        public double DiedAt;

        public double RespawnAt;

        /// <summary>The MVP (top damage dealer) of the last kill.</summary>
        public string KilledBy;

        public int Kills;
    }

    /// <summary>
    /// GDD §6 boss timers: mini-bosses return about 2 hours after they die and MVPs about 1 hour, each ± a few minutes so
    /// nobody can camp the exact second. Time is passed in (seconds on any steady clock), so the same rules run on the
    /// Phase 6 map server; for now the runtime keeps one tracker in memory for the play session.
    /// </summary>
    public sealed class BossTracker
    {
        private readonly Dictionary<string, BossStatus> _byKey = new Dictionary<string, BossStatus>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Multiplies every respawn time (GM testing: 0.01 turns an hour into 36 seconds).</summary>
        public float RespawnScale = 1f;

        public IEnumerable<BossStatus> All => _byKey.Values;

        /// <summary>Null when the boss has never died (it's up).</summary>
        public BossStatus Status(string mapId, string monsterId)
        {
            return _byKey.TryGetValue(Key(mapId, monsterId), out var status) ? status : null;
        }

        /// <summary>True when the boss should be on its map now (never killed, alive, or its timer has run out).</summary>
        public bool IsDue(string mapId, string monsterId, double now)
        {
            var status = Status(mapId, monsterId);
            return status == null || status.Alive || now >= status.RespawnAt;
        }

        /// <summary>Seconds until it's due (0 when it is).</summary>
        public double SecondsUntilDue(string mapId, string monsterId, double now)
        {
            var status = Status(mapId, monsterId);
            return status == null || status.Alive ? 0 : Math.Max(0, status.RespawnAt - now);
        }

        public void MarkSpawned(string mapId, string monsterId)
        {
            var status = GetOrAdd(mapId, monsterId);
            status.Alive = true;
        }

        /// <summary>Starts the respawn timer: <see cref="BossSpawn.RespawnMinutes"/> ± <see cref="BossSpawn.VarianceMinutes"/>.</summary>
        public BossStatus RecordKill(string mapId, BossSpawn spawn, string killedBy, double now, IRandomSource random)
        {
            if (spawn == null)
            {
                throw new ArgumentNullException(nameof(spawn));
            }

            var status = GetOrAdd(mapId, spawn.MonsterId);
            double variance = (random.NextDouble() * 2.0 - 1.0) * spawn.VarianceMinutes;
            double minutes = Math.Max(0.1, spawn.RespawnMinutes + variance) * Math.Max(0f, RespawnScale);
            status.Alive = false;
            status.DiedAt = now;
            status.RespawnAt = now + minutes * 60.0;
            status.KilledBy = killedBy;
            status.Kills++;
            return status;
        }

        /// <summary>Every boss comes back at once (GM).</summary>
        public void ResetAll()
        {
            foreach (var status in _byKey.Values)
            {
                status.RespawnAt = 0;
            }
        }

        /// <summary>"1h 05m", "12m 30s", "45s".</summary>
        public static string FormatDuration(double seconds)
        {
            var span = TimeSpan.FromSeconds(Math.Max(0, Math.Ceiling(seconds)));
            if (span.TotalHours >= 1)
            {
                return $"{(int)span.TotalHours}h {span.Minutes:00}m";
            }

            return span.TotalMinutes >= 1 ? $"{span.Minutes}m {span.Seconds:00}s" : $"{span.Seconds}s";
        }

        private BossStatus GetOrAdd(string mapId, string monsterId)
        {
            string key = Key(mapId, monsterId);
            if (!_byKey.TryGetValue(key, out var status))
            {
                status = new BossStatus { MapId = mapId, MonsterId = monsterId };
                _byKey[key] = status;
            }

            return status;
        }

        private static string Key(string mapId, string monsterId)
        {
            return mapId + "|" + monsterId;
        }
    }
}
