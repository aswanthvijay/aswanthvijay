using System;
using Runeheir.Characters;

namespace Runeheir.Social
{
    /// <summary>
    /// A realm server's settings (Phase 6), read from <c>realm.json</c> next to the server's data. Every value is clamped
    /// on load, so a hand-edited file can't break the server.
    /// </summary>
    [Serializable]
    public sealed class RealmConfig
    {
        /// <summary>Bumped whenever client and server stop understanding each other; mismatches are refused at login.</summary>
        public const int ProtocolVersion = 1;

        public const ushort DefaultPort = 7777;
        public const int MaxNameLength = 32;

        public string Name = "Runeheir Alpha";
        public string Motd = "Welcome to the Runeheir alpha realm! Party up with %text, trade by clicking players, vend in Vigrid Haven.";
        public ushort Port = DefaultPort;
        public int MaxPlayers = 64;

        /// <summary>Lets players use the @ commands (handy for an alpha; turn off for a public server).</summary>
        public bool AllowGmCommands = true;

        public bool AllowRegistration = true;
        public float BaseExpRate = 50f;
        public float JobExpRate = 50f;
        public float DropRate = 5f;
        public float CardDropRate = 1f;

        /// <summary>Seconds between the server's own saves of guilds and boss timers.</summary>
        public int SaveSeconds = 120;

        public void Sanitize()
        {
            Name = ChatRules.Sanitize(string.IsNullOrWhiteSpace(Name) ? "Runeheir Alpha" : Name, MaxNameLength);
            Motd = ChatRules.Sanitize(Motd ?? string.Empty, 200);
            Port = Port == 0 ? DefaultPort : Port;
            MaxPlayers = Math.Max(1, Math.Min(500, MaxPlayers));
            BaseExpRate = Clamp(BaseExpRate, 0f, 1000f);
            JobExpRate = Clamp(JobExpRate, 0f, 1000f);
            DropRate = Clamp(DropRate, 0f, 100f);
            CardDropRate = Clamp(CardDropRate, 0f, 100f);
            SaveSeconds = Math.Max(10, Math.Min(3600, SaveSeconds));
        }

        public ServerRates Rates()
        {
            return new ServerRates { BaseExp = BaseExpRate, JobExp = JobExpRate, Drop = DropRate, CardDrop = CardDropRate };
        }

        /// <summary>Reads "host", "host:port" or "[ipv6]:port"; false (with a reason) when it can't.</summary>
        public static bool TryParseAddress(string raw, out string host, out ushort port, out string error)
        {
            host = null;
            port = DefaultPort;
            string text = (raw ?? string.Empty).Trim();
            if (text.Length == 0)
            {
                error = "Enter the realm's address (for example 192.168.1.20 or myrealm.example:7777).";
                return false;
            }

            string portText = null;
            if (text[0] == '[')
            {
                int close = text.IndexOf(']');
                if (close < 2)
                {
                    error = "That address isn't valid.";
                    return false;
                }

                host = text.Substring(1, close - 1);
                if (close + 1 < text.Length)
                {
                    if (text[close + 1] != ':')
                    {
                        error = "That address isn't valid.";
                        return false;
                    }

                    portText = text.Substring(close + 2);
                }
            }
            else
            {
                int colon = text.LastIndexOf(':');
                if (colon >= 0 && text.IndexOf(':') == colon)
                {
                    host = text.Substring(0, colon);
                    portText = text.Substring(colon + 1);
                }
                else
                {
                    host = text; // a bare IPv6 address has several colons and no port
                }
            }

            if (string.IsNullOrWhiteSpace(host) || host.IndexOf(' ') >= 0)
            {
                error = "That address isn't valid.";
                return false;
            }

            if (portText != null && (!ushort.TryParse(portText, out port) || port == 0))
            {
                error = "The port must be a number from 1 to 65535.";
                return false;
            }

            error = null;
            return true;
        }

        private static float Clamp(float value, float min, float max)
        {
            return float.IsNaN(value) ? min : Math.Max(min, Math.Min(max, value));
        }
    }
}
