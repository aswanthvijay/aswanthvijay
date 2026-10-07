using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Runeheir.Combat
{
    /// <summary>
    /// A player's defences as their client sends them to the realm server (Phase 6), so the server's monsters hit
    /// that player exactly as hard as they would offline. Plain fields for JSON.
    /// </summary>
    [Serializable]
    public sealed class CombatSnapshot
    {
        public int Def;
        public int SoftDef;
        public int Mdef;
        public int SoftMdef;
        public int Flee;
        public int Element;
        public int Race;
        public int Size;
        public float BluntMultiplier = 1f;

        /// <summary>% damage taken from each attacker race / attack element (empty = none).</summary>
        public float[] TakenFromRace = Array.Empty<float>();

        public float[] TakenFromElement = Array.Empty<float>();

        public static CombatSnapshot From(in DefenderProfile defender)
        {
            var snapshot = new CombatSnapshot
            {
                Def = defender.Def,
                SoftDef = defender.SoftDef,
                Mdef = defender.Mdef,
                SoftMdef = defender.SoftMdef,
                Flee = defender.Flee,
                Element = (int)defender.Element,
                Race = (int)defender.Race,
                Size = (int)defender.Size,
                BluntMultiplier = defender.BluntDamageTakenMultiplier,
            };

            if (defender.Resist != null)
            {
                snapshot.TakenFromRace = (float[])defender.Resist.TakenFromRace.Clone();
                snapshot.TakenFromElement = (float[])defender.Resist.TakenFromElement.Clone();
            }

            return snapshot;
        }

        public DefenderProfile ToDefender()
        {
            var profile = new DefenderProfile
            {
                Def = Math.Max(0, Def),
                SoftDef = Math.Max(0, SoftDef),
                Mdef = Math.Max(0, Mdef),
                SoftMdef = Math.Max(0, SoftMdef),
                Flee = Math.Max(0, Flee),
                Element = Clamp<Element>(Element, CombatEnumCounts.Elements),
                Race = Clamp<Race>(Race, CombatEnumCounts.Races),
                Size = Clamp<Size>(Size, CombatEnumCounts.Sizes),
                BluntDamageTakenMultiplier = float.IsNaN(BluntMultiplier) || BluntMultiplier <= 0f ? 1f : Math.Min(10f, BluntMultiplier),
            };

            bool hasRace = TakenFromRace != null && TakenFromRace.Length > 0;
            bool hasElement = TakenFromElement != null && TakenFromElement.Length > 0;
            if (hasRace || hasElement)
            {
                var resist = new DamageBonuses();
                Copy(TakenFromRace, resist.TakenFromRace);
                Copy(TakenFromElement, resist.TakenFromElement);
                profile.Resist = resist;
            }

            return profile;
        }

        private static void Copy(float[] from, float[] to)
        {
            if (from == null)
            {
                return;
            }

            for (int i = 0; i < Math.Min(from.Length, to.Length); i++)
            {
                to[i] = float.IsNaN(from[i]) ? 0f : Math.Max(-100f, Math.Min(100f, from[i]));
            }
        }

        private static T Clamp<T>(int value, int count) where T : struct
        {
            return (T)(object)Math.Max(0, Math.Min(count - 1, value));
        }
    }

    /// <summary>One buff or status as it travels between peers (an "aura").</summary>
    public struct AuraEntry
    {
        /// <summary>A status (true) or a buff/debuff (false).</summary>
        public bool IsStatus;

        public StatusEffect Status;
        public string BuffId;
        public float Remaining;
        public int Level;
        public int Stacks;
    }

    /// <summary>
    /// Text form of an entity's buffs and statuses (Phase 6): the realm sends a monster's to every client, a client sends
    /// its character's to the realm, so slows, freezes, stealth and debuffs look and work the same on every screen.
    /// Format: <c>s3:4.5;bprovoke_debuff:9.8:5:1</c> (statuses by number; buffs by id, with level and stacks).
    /// </summary>
    public static class AuraCodec
    {
        public const int MaxEntries = 48;

        public static string Encode(BuffContainer buffs, StatusContainer statuses, double now)
        {
            var builder = new StringBuilder();
            int count = 0;
            if (statuses != null)
            {
                foreach (var status in statuses.Active)
                {
                    if (count++ >= MaxEntries)
                    {
                        break;
                    }

                    Separator(builder);
                    builder.Append('s').Append((int)status.Status).Append(':').Append(Seconds(status.Remaining(now)));
                }
            }

            if (buffs != null)
            {
                foreach (var buff in buffs.Active)
                {
                    if (buff.Definition == null || count++ >= MaxEntries)
                    {
                        continue;
                    }

                    Separator(builder);
                    builder.Append('b').Append(buff.Definition.Id).Append(':').Append(Seconds(buff.Remaining(now)))
                        .Append(':').Append(buff.Level).Append(':').Append(buff.Stacks);
                }
            }

            return builder.ToString();
        }

        public static List<AuraEntry> Decode(string text)
        {
            var entries = new List<AuraEntry>();
            if (string.IsNullOrEmpty(text))
            {
                return entries;
            }

            foreach (string part in text.Split(';'))
            {
                if (part.Length < 3 || entries.Count >= MaxEntries)
                {
                    continue;
                }

                string[] fields = part.Substring(1).Split(':');
                if (part[0] == 's' && fields.Length >= 2 && int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int status)
                    && status > 0 && status < CombatEnumCounts.StatusEffects && TryFloat(fields[1], out float remaining))
                {
                    entries.Add(new AuraEntry { IsStatus = true, Status = (StatusEffect)status, Remaining = remaining });
                }
                else if (part[0] == 'b' && fields.Length >= 4 && fields[0].Length > 0 && TryFloat(fields[1], out remaining)
                         && int.TryParse(fields[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int level)
                         && int.TryParse(fields[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int stacks))
                {
                    entries.Add(new AuraEntry
                    {
                        BuffId = fields[0],
                        Remaining = remaining,
                        Level = Math.Max(1, Math.Min(99, level)),
                        Stacks = Math.Max(1, Math.Min(99, stacks)),
                    });
                }
            }

            return entries;
        }

        /// <summary>
        /// Replaces <paramref name="buffs"/> and <paramref name="statuses"/> with the decoded auras (a mirror of another
        /// peer's entity). Unknown buff ids are skipped.
        /// </summary>
        public static void Apply(string text, BuffContainer buffs, StatusContainer statuses, double now)
        {
            var entries = Decode(text);
            statuses?.Clear();
            buffs?.Clear();
            foreach (var entry in entries)
            {
                if (entry.IsStatus)
                {
                    statuses?.Apply(entry.Status, Math.Max(0.05f, entry.Remaining), now);
                    continue;
                }

                var definition = BuffCatalog.Get(entry.BuffId);
                if (definition == null || buffs == null)
                {
                    continue;
                }

                for (int i = 0; i < entry.Stacks; i++)
                {
                    buffs.Apply(definition, now, entry.Level, Math.Max(0.05f, entry.Remaining), 0, entry.Stacks);
                }
            }
        }

        private static void Separator(StringBuilder builder)
        {
            if (builder.Length > 0)
            {
                builder.Append(';');
            }
        }

        private static string Seconds(double seconds)
        {
            return Math.Min(99999.0, Math.Max(0.0, seconds)).ToString("0.#", CultureInfo.InvariantCulture);
        }

        private static bool TryFloat(string text, out float value)
        {
            bool ok = float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && !float.IsNaN(value) && !float.IsInfinity(value);
            value = ok ? Math.Max(0f, Math.Min(99999f, value)) : 0f;
            return ok;
        }
    }
}
