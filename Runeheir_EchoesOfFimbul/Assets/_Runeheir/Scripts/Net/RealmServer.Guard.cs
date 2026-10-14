using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Mirror;
using Runeheir.Characters;
using Runeheir.Items;
using Runeheir.Online;
using Runeheir.Social;
using UnityEngine;

namespace Runeheir.Net
{
    /// <summary>
    /// Phase 9, the realm guard on a realm server. Every save from a player's game is checked against the realm's last
    /// good copy and the ledger of what the realm handed that character (<see cref="RealmGuard"/>); a refused save leaves
    /// the realm's copy as it was, and too many refusals send the player back to the login screen. Hits on the realm's
    /// monsters are capped at what the character could ever deal. Verdicts go to <see cref="GuardLogFile"/>. The host's
    /// own character is trusted, and a realm with <c>"Guard": false</c> in realm.json checks nothing.
    /// </summary>
    public sealed partial class RealmServer
    {
        public const string GuardLogFile = "realm_guard.log";

        private bool Guarded(RealmSession session)
        {
            return Config.Guard && session != null && !session.IsLocal;
        }

        private static double GuardClock => Time.unscaledTimeAsDouble;

        /// <summary>Checks a save before it's kept: null when it may be saved, or why the realm refuses it.</summary>
        private async Task<string> GuardSave(RealmSession session, CharacterRecord submitted, List<ItemStack> submittedStorage, string context)
        {
            if (!Guarded(session))
            {
                return null;
            }

            var previous = await LastGoodRecord(session, submitted);
            List<ItemStack> previousStorage = null;
            if (submittedStorage != null)
            {
                var stored = await _accounts.GetStorageAsync(session.Username);
                previousStorage = stored.Success && stored.Value != null ? stored.Value : new List<ItemStack>();
            }

            var verdict = RealmGuard.Audit(previous, previousStorage, submitted, submittedStorage, session.Ledger,
                new GuardOptions { Economy = !Config.AllowGmCommands });
            if (verdict.Refused || verdict.Warnings.Count > 0)
            {
                WriteGuardLog(session, submitted, context, verdict);
            }

            if (verdict.Refused)
            {
                session.Strikes++;
                session.LastVerdict = verdict.Summary;
                if (session.Strikes >= Config.GuardStrikes && session.KickAt < 0f)
                {
                    session.KickAt = Time.unscaledTime + 1f;
                    Notify(session, "The realm refused too many of your saves. You're being sent back to the login screen.");
                }

                return "The realm refused this save (" + verdict.Violations[0] + "). Your last good save is kept.";
            }

            RealmGuard.Settle(session.Ledger, previous, previousStorage, submitted, submittedStorage, GuardClock);
            session.DamageCeiling = RealmGuard.DamageCeiling(submitted);
            return null;
        }

        /// <summary>The realm's last good copy of the character: the one in play, or the one on disk.</summary>
        private async Task<CharacterRecord> LastGoodRecord(RealmSession session, CharacterRecord submitted)
        {
            if (session.Record != null && submitted != null && session.Record.Slot == submitted.Slot
                && string.Equals(session.Record.Name, submitted.Name, StringComparison.Ordinal))
            {
                return session.Record;
            }

            if (submitted == null)
            {
                return null;
            }

            foreach (var character in await _accounts.GetCharactersAsync(session.Username))
            {
                if (character != null && character.Slot == submitted.Slot)
                {
                    return character;
                }
            }

            return null;
        }

        /// <summary>A hit from <paramref name="sender"/>'s character on a realm monster, cut down to what it could ever deal.</summary>
        internal int GuardHit(NetworkConnectionToClient sender, int amount)
        {
            var session = sender != null ? Session(sender) : null;
            if (!Guarded(session) || amount <= 0)
            {
                return amount;
            }

            if (session.DamageCeiling <= 0)
            {
                session.DamageCeiling = RealmGuard.DamageCeiling(session.Record);
            }

            if (session.DamageCeiling <= 0 || amount <= session.DamageCeiling)
            {
                return amount;
            }

            if (GuardClock >= session.NextHitWarning)
            {
                session.NextHitWarning = GuardClock + 10.0;
                var verdict = new GuardVerdict();
                verdict.Warnings.Add($"a hit for {amount:N0} (the most {session.CharacterName} could deal is {session.DamageCeiling:N0}); cut down");
                WriteGuardLog(session, session.Record, "hit", verdict);
            }

            return session.DamageCeiling;
        }

        /// <summary>Once a second: players with too many refused saves are disconnected.</summary>
        private void TickGuard()
        {
            foreach (var session in new List<RealmSession>(_sessions.Values))
            {
                if (session.KickAt >= 0f && Time.unscaledTime >= session.KickAt && !session.Disconnected)
                {
                    session.KickAt = -1f;
                    session.Connection.Disconnect();
                }
            }
        }

        /// <summary>@guard (the host only): each player's refused saves and the last reason.</summary>
        private void GuardCommand(RealmSession session)
        {
            if (!session.IsLocal)
            {
                Error(session, "Only the realm's host can read the guard's log.");
                return;
            }

            var text = new StringBuilder(Config.Guard
                ? Config.AllowGmCommands ? "Realm guard: on (shape and points only; @commands are allowed). " : "Realm guard: on. "
                : "Realm guard: off. ");
            int shown = 0;
            foreach (var other in _sessions.Values)
            {
                if (other.Strikes > 0)
                {
                    text.Append(shown++ > 0 ? " · " : string.Empty).Append(other.CharacterName ?? other.Username).Append(": ")
                        .Append(other.Strikes).Append(" refused (").Append(other.LastVerdict).Append(')');
                }
            }

            Notify(session, shown == 0 ? text.Append("No refused saves.").ToString() : text.ToString());
        }

        private void WriteGuardLog(RealmSession session, CharacterRecord record, string context, GuardVerdict verdict)
        {
            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {session.Username}/{record?.Name ?? session.CharacterName} {context}: "
                          + (verdict.Refused ? "REFUSED " : "warning ") + verdict.Summary;
            Debug.LogWarning("[Runeheir guard] " + line);
            try
            {
                File.AppendAllText(Path.Combine(_dataDirectory, GuardLogFile), line + Environment.NewLine);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Runeheir realm] {GuardLogFile} couldn't be written ({exception.Message}).");
            }
        }

        // ------------------------------------------------------------ the ledger: what the realm hands over and takes back
        /// <summary>A trade offer handed to <paramref name="to"/> (the other side's goods, or their own back).</summary>
        private void HandOffer(RealmSession to, TradeOffer offer, string from)
        {
            if (to == null || offer == null)
            {
                return;
            }

            to.Ledger.AddGoods(offer.Items, true, GuardClock);
            to.Ledger.AddZenyIn(offer.Zeny, GuardClock);
            Send(to, new SocialUpdate { Kind = SocialEvent.TradeReceive, Json = Json.Write(offer), Text = from });
        }
    }
}
