using System;
using System.Collections.Generic;

namespace Runeheir.Social
{
    /// <summary>One of the five halls fought over in the War of the Realms.</summary>
    public sealed class CastleDefinition
    {
        public string MapId;
        public string Name;
        public string Lore;
    }

    /// <summary>Which guild holds a castle, and since when.</summary>
    [Serializable]
    public sealed class CastleOwner
    {
        public string CastleId;
        public string Guild;
        public long SinceUnixMs;
    }

    /// <summary>The realm's castle owners, saved next to its guilds.</summary>
    [Serializable]
    public sealed class SiegeBook
    {
        public List<CastleOwner> Owners = new List<CastleOwner>();
    }

    /// <summary>
    /// The War of the Realms (Phase 8, Ragnarok's War of Emperium). At the realm's siege hours, guilds storm five halls on
    /// Iðavöllr. A hall falls when its Jarlstone (the Emperium) breaks: only members of a guild that doesn't hold it can
    /// strike it, and only while the war is on. The guild that breaks it takes the hall; everyone else in it is sent home.
    /// The holders, and only they, can open the Castle Hoards that gather in their hall.
    /// </summary>
    public static class SiegeRules
    {
        public const string JarlstoneId = "jarlstone";
        public const string HoardId = "castle_hoard";
        public const string FieldsMapId = "idavollr";

        public static readonly IReadOnlyList<CastleDefinition> Castles = new[]
        {
            new CastleDefinition { MapId = "castle_bilskirnir", Name = "Bilskirnir", Lore = "Thor's hall of five hundred and forty rooms, under lightning." },
            new CastleDefinition { MapId = "castle_breidablik", Name = "Breiðablik", Lore = "Baldr's hall, where nothing unclean can stand." },
            new CastleDefinition { MapId = "castle_gladsheimr", Name = "Glaðsheimr", Lore = "Odin's bright home, roofed with gold." },
            new CastleDefinition { MapId = "castle_sessrumnir", Name = "Sessrúmnir", Lore = "Freyja's many-seated hall on Fólkvangr." },
            new CastleDefinition { MapId = "castle_sokkvabekkr", Name = "Sökkvabekkr", Lore = "Sága's hall, where cool waves wash over it." },
        };

        public static CastleDefinition Castle(string mapId)
        {
            foreach (var castle in Castles)
            {
                if (string.Equals(castle.MapId, mapId, StringComparison.OrdinalIgnoreCase))
                {
                    return castle;
                }
            }

            return null;
        }

        /// <summary>Only a guild that doesn't hold the hall can strike its Jarlstone, and only while the war is on.</summary>
        public static bool MayStrikeJarlstone(string attackerGuild, string ownerGuild, bool siegeOn)
        {
            return siegeOn && !string.IsNullOrEmpty(attackerGuild) && !string.Equals(attackerGuild, ownerGuild, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Only the hall's own guild opens its hoards.</summary>
        public static bool MayOpenHoard(string guild, string ownerGuild)
        {
            return !string.IsNullOrEmpty(guild) && string.Equals(guild, ownerGuild, StringComparison.OrdinalIgnoreCase);
        }

        public static string OwnerOf(SiegeBook book, string castleId)
        {
            if (book?.Owners == null)
            {
                return null;
            }

            foreach (var owner in book.Owners)
            {
                if (owner != null && string.Equals(owner.CastleId, castleId, StringComparison.OrdinalIgnoreCase))
                {
                    return owner.Guild;
                }
            }

            return null;
        }

        /// <summary>The breaking guild takes the hall.</summary>
        public static void Capture(SiegeBook book, string castleId, string guild, long nowUnixMs)
        {
            book.Owners = book.Owners ?? new List<CastleOwner>();
            book.Owners.RemoveAll(o => o == null || string.Equals(o.CastleId, castleId, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(guild))
            {
                book.Owners.Add(new CastleOwner { CastleId = castleId, Guild = guild, SinceUnixMs = nowUnixMs });
            }
        }

        /// <summary>A disbanded guild loses its halls.</summary>
        public static void ForgetGuild(SiegeBook book, string guild)
        {
            book?.Owners?.RemoveAll(o => o == null || string.Equals(o.Guild, guild, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Drops owners of halls that no longer exist (an edited file, a renamed hall).</summary>
        public static void Sanitize(SiegeBook book)
        {
            book.Owners = book.Owners ?? new List<CastleOwner>();
            book.Owners.RemoveAll(o => o == null || Castle(o.CastleId) == null || string.IsNullOrEmpty(o.Guild));
        }

        /// <summary>Is the war on at <paramref name="local"/> (the realm's local time), by the realm's schedule?</summary>
        public static bool IsSiegeTime(RealmConfig config, DateTime local)
        {
            if (config == null || config.SiegeMinutes <= 0 || string.IsNullOrWhiteSpace(config.SiegeDays))
            {
                return false;
            }

            // A war that started yesterday evening may still be on after midnight.
            foreach (int daysBack in new[] { 0, 1 })
            {
                var day = local.Date.AddDays(-daysBack);
                if (!OnDay(config.SiegeDays, day.DayOfWeek))
                {
                    continue;
                }

                var start = day.AddHours(config.SiegeStartHour);
                if (local >= start && local < start.AddMinutes(config.SiegeMinutes))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>When the next war starts after <paramref name="local"/> (null when the realm has none).</summary>
        public static DateTime? NextSiege(RealmConfig config, DateTime local)
        {
            if (config == null || config.SiegeMinutes <= 0 || string.IsNullOrWhiteSpace(config.SiegeDays))
            {
                return null;
            }

            for (int days = 0; days <= 7; days++)
            {
                var day = local.Date.AddDays(days);
                var start = day.AddHours(config.SiegeStartHour);
                if (OnDay(config.SiegeDays, day.DayOfWeek) && start > local)
                {
                    return start;
                }
            }

            return null;
        }

        /// <summary>"Wed,Sat" / "daily" / "Mon Tue" ...</summary>
        public static bool OnDay(string days, DayOfWeek day)
        {
            string text = (days ?? string.Empty).ToLowerInvariant();
            return text.Contains("daily") || text.Contains("every") || text.Contains(day.ToString().Substring(0, 3).ToLowerInvariant());
        }
    }
}
