using System;
using System.IO;
using System.Text;
using Runeheir.Combat;
using Runeheir.Online;
using Runeheir.Session;
using Runeheir.Social;
using Runeheir.World;
using UnityEngine;

namespace Runeheir.Net
{
    /// <summary>
    /// Phase 8, the War of the Realms on a realm server: who holds the five halls (saved in <see cref="SiegesFile"/>), when
    /// the war is on (realm.json's schedule, or a GM's @siege), who may strike a Jarlstone or open a hoard (the realm's own
    /// guild records, never what a client claims), and what happens when a Jarlstone breaks: the breaker's guild takes the
    /// hall, the realm hears of it, and everyone else in the hall is sent home.
    /// </summary>
    public sealed partial class RealmServer
    {
        public const string SiegesFile = "realm_sieges.json";

        private SiegeBook _sieges = new SiegeBook();
        private bool? _siegeForced;
        private bool _siegeWasOn;

        /// <summary>Is the war on right now (the schedule, unless a GM started or stopped it)?</summary>
        public bool SiegeOn => _siegeForced ?? SiegeRules.IsSiegeTime(Config, DateTime.Now);

        private void StartSieges()
        {
            _sieges = LoadSieges();
            SiegeRules.Sanitize(_sieges);
            RealmHooks.GuildOf = entity => entity == null ? null : _guilds.GuildOf(entity.DisplayName)?.Name;
            RealmHooks.CastleOwner = mapId => SiegeRules.OwnerOf(_sieges, mapId);
            RealmHooks.SiegeOn = () => SiegeOn;
            RealmHooks.JarlstoneBroken = OnJarlstoneBroken;
            _siegeWasOn = SiegeOn;
        }

        private static void StopSieges()
        {
            RealmHooks.GuildOf = null;
            RealmHooks.CastleOwner = null;
            RealmHooks.SiegeOn = null;
            RealmHooks.JarlstoneBroken = null;
        }

        /// <summary>Once a second: the war's start and end are announced to the whole realm.</summary>
        private void TickSieges()
        {
            bool on = SiegeOn;
            if (on == _siegeWasOn)
            {
                return;
            }

            _siegeWasOn = on;
            Broadcast(on
                ? "⚔ The War of the Realms has begun! Take the road north of Vigrid Haven to Iðavöllr and break the Jarlstones."
                : "The War of the Realms is over. " + HoldersLine());
        }

        private void OnJarlstoneBroken(Monster stone, PlayerEntity breaker)
        {
            var map = WorldGrid.MapAt(stone.Position.x, stone.Position.z);
            var castle = SiegeRules.Castle(map?.Id);
            string guild = breaker != null ? _guilds.GuildOf(breaker.DisplayName)?.Name : null;
            if (castle == null || string.IsNullOrEmpty(guild))
            {
                return;
            }

            SiegeRules.Capture(_sieges, castle.MapId, guild, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            SaveSieges();
            Broadcast($"★ {breaker.DisplayName} of {guild} has broken the Jarlstone of {castle.Name}! The hall is theirs.");

            foreach (var session in _sessions.Values)
            {
                if (!session.InWorld || !string.Equals(session.MapId, castle.MapId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.Equals(_guilds.GuildOf(session.CharacterName)?.Name, guild, StringComparison.OrdinalIgnoreCase))
                {
                    Send(session, new SocialUpdate { Kind = SocialEvent.Expel, Text = $"{castle.Name} has fallen to {guild}. You're sent home." });
                }
            }
        }

        /// <summary>A disbanded guild's halls stand empty.</summary>
        private void ReleaseHalls(string guild)
        {
            if (!string.IsNullOrEmpty(guild) && _sieges.Owners.Exists(o => string.Equals(o.Guild, guild, StringComparison.OrdinalIgnoreCase)))
            {
                SiegeRules.ForgetGuild(_sieges, guild);
                SaveSieges();
            }
        }

        /// <summary>What a player is told on entering the world (and what @siege status says).</summary>
        private string SiegeStatus()
        {
            var text = new StringBuilder("War of the Realms: ");
            if (SiegeOn)
            {
                text.Append("the war is ON now! ");
            }
            else
            {
                var next = SiegeRules.NextSiege(Config, DateTime.Now);
                text.Append(next.HasValue ? $"next war {next.Value:ddd HH:mm} (realm time). " : "no wars are scheduled on this realm. ");
            }

            return text.Append(HoldersLine()).ToString();
        }

        private string HoldersLine()
        {
            var text = new StringBuilder("Holders: ");
            for (int i = 0; i < SiegeRules.Castles.Count; i++)
            {
                var castle = SiegeRules.Castles[i];
                text.Append(i > 0 ? " · " : string.Empty).Append(castle.Name).Append(" — ").Append(SiegeRules.OwnerOf(_sieges, castle.MapId) ?? "unclaimed");
            }

            return text.ToString();
        }

        /// <summary>@siege start | stop | auto | status (GM commands must be allowed on the realm).</summary>
        private void SiegeCommand(RealmSession session, string args)
        {
            string verb = (args ?? string.Empty).Trim().ToLowerInvariant();
            if (verb != "status" && verb.Length > 0 && !Config.AllowGmCommands)
            {
                Error(session, "GM commands are off on this realm.");
                return;
            }

            switch (verb)
            {
                case "start":
                    _siegeForced = true;
                    break;
                case "stop":
                    _siegeForced = false;
                    break;
                case "auto":
                    _siegeForced = null;
                    break;
            }

            TickSieges();
            Notify(session, SiegeStatus());
        }

        /// <summary>A line for every player in the world (the host's own screen too).</summary>
        private void Broadcast(string line)
        {
            ChatLog.System(line);
            foreach (var session in _sessions.Values)
            {
                if (session.InWorld && !session.IsLocal)
                {
                    session.Connection.Send(new ChatDeliver { Channel = (byte)ChatChannel.System, Text = line });
                }
            }
        }

        private SiegeBook LoadSieges()
        {
            string path = Path.Combine(_dataDirectory, SiegesFile);
            try
            {
                if (File.Exists(path))
                {
                    return JsonUtility.FromJson<SiegeBook>(File.ReadAllText(path)) ?? new SiegeBook();
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Runeheir realm] {path} couldn't be read ({exception.Message}).");
            }

            return new SiegeBook();
        }

        private void SaveSieges()
        {
            try
            {
                string path = Path.Combine(_dataDirectory, SiegesFile);
                string temp = path + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(_sieges, true));
                if (File.Exists(path))
                {
                    File.Replace(temp, path, path + ".bak");
                }
                else
                {
                    File.Move(temp, path);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Runeheir realm] castle owners couldn't be saved ({exception.Message}).");
            }
        }
    }
}
