using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Runeheir.Accounts;
using Runeheir.Jobs;

namespace Runeheir.Social
{
    public enum GuildRank
    {
        Member = 0,
        Officer = 1,
        Leader = 2,
    }

    [Serializable]
    public sealed class GuildMember
    {
        public string Name;
        public GuildRank Rank;
        public int BaseLevel;
        public JobId Job;
        public long JoinedUnixMs;

        /// <summary>Not saved meaningfully: the server sets it while the character is logged in.</summary>
        public bool Online;
    }

    [Serializable]
    public sealed class GuildRecord
    {
        public int Id;
        public string Name;
        public string Notice = string.Empty;
        public long CreatedUnixMs;
        public List<GuildMember> Members = new List<GuildMember>();

        public GuildMember Member(string name)
        {
            return Members.Find(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        public GuildMember Leader => Members.Find(m => m.Rank == GuildRank.Leader);

        public GuildRecord Clone()
        {
            var copy = (GuildRecord)MemberwiseClone();
            copy.Members = Members.ConvertAll(m => new GuildMember
            {
                Name = m.Name, Rank = m.Rank, BaseLevel = m.BaseLevel, Job = m.Job, JoinedUnixMs = m.JoinedUnixMs, Online = m.Online,
            });
            return copy;
        }
    }

    /// <summary>Every guild on a server, saved as one JSON file next to the accounts.</summary>
    [Serializable]
    public sealed class GuildDatabase
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public int NextId = 1;
        public List<GuildRecord> Guilds = new List<GuildRecord>();
    }

    public static class GuildRules
    {
        public const int MaxMembers = 30;
        public const int MinNameLength = 3;
        public const int MaxNameLength = 24;
        public const int MaxNoticeLength = 120;

        /// <summary>Founding a guild costs this much zeny (paid by the founder's client before asking).</summary>
        public const long FoundingFee = 50_000;

        public const int MinFounderLevel = 30;

        private static readonly Regex NamePattern = new Regex("^[A-Za-z0-9 '\\-]+\\z");

        public static bool ValidateName(string raw, out string name, out string error)
        {
            name = (raw ?? string.Empty).Trim();
            while (name.Contains("  "))
            {
                name = name.Replace("  ", " ");
            }

            if (name.Length < MinNameLength || name.Length > MaxNameLength)
            {
                error = $"Guild names are {MinNameLength}-{MaxNameLength} characters.";
                return false;
            }

            if (!NamePattern.IsMatch(name))
            {
                error = "Guild names may use letters, numbers, spaces, ' and -.";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>Officers and the leader invite, expel members and write the notice.</summary>
        public static bool CanManage(GuildRank rank)
        {
            return rank >= GuildRank.Officer;
        }
    }

    /// <summary>
    /// Guilds on the server: found, invite, join, leave, expel, promote, hand over, disband and the notice. Changes are
    /// written through <c>persist</c> (a JSON file); a failed write is rolled back, like the account store.
    /// </summary>
    public sealed class GuildBook
    {
        private readonly GuildDatabase _db;
        private readonly Action<GuildDatabase> _persist;
        private readonly Func<long> _now;
        private readonly Dictionary<string, int> _invites = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public GuildBook(GuildDatabase database, Action<GuildDatabase> persist, Func<long> nowUnixMs = null)
        {
            _db = database ?? new GuildDatabase();
            _db.Guilds = _db.Guilds ?? new List<GuildRecord>();
            _db.Guilds.RemoveAll(g => g == null || g.Members == null || g.Members.Count == 0);
            foreach (var guild in _db.Guilds)
            {
                guild.Members.RemoveAll(m => m == null || string.IsNullOrEmpty(m.Name));
                foreach (var member in guild.Members)
                {
                    member.Online = false;
                }

                if (guild.Leader == null && guild.Members.Count > 0)
                {
                    guild.Members[0].Rank = GuildRank.Leader;
                }

                _db.NextId = Math.Max(_db.NextId, guild.Id + 1);
            }

            _persist = persist ?? (_ => { });
            _now = nowUnixMs ?? (() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        }

        public IReadOnlyList<GuildRecord> All => _db.Guilds;

        public GuildRecord Get(int id)
        {
            return _db.Guilds.Find(g => g.Id == id);
        }

        public GuildRecord GuildOf(string name)
        {
            return name == null ? null : _db.Guilds.Find(g => g.Member(name) != null);
        }

        public int PendingInvite(string name)
        {
            return name != null && _invites.TryGetValue(name, out int id) && Get(id) != null ? id : 0;
        }

        public OpResult<GuildRecord> Create(string founder, int baseLevel, JobId job, string guildName)
        {
            if (string.IsNullOrEmpty(founder))
            {
                return OpResult<GuildRecord>.Fail("No character.");
            }

            if (GuildOf(founder) != null)
            {
                return OpResult<GuildRecord>.Fail("Leave your guild first.");
            }

            if (baseLevel < GuildRules.MinFounderLevel)
            {
                return OpResult<GuildRecord>.Fail($"Founding a guild needs base level {GuildRules.MinFounderLevel}.");
            }

            if (!GuildRules.ValidateName(guildName, out string name, out string error))
            {
                return OpResult<GuildRecord>.Fail(error);
            }

            if (_db.Guilds.Exists(g => string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                return OpResult<GuildRecord>.Fail("That guild name is taken.");
            }

            var guild = new GuildRecord { Id = _db.NextId, Name = name, CreatedUnixMs = _now() };
            guild.Members.Add(new GuildMember { Name = founder, Rank = GuildRank.Leader, BaseLevel = baseLevel, Job = job, JoinedUnixMs = _now(), Online = true });
            _db.NextId++;
            _db.Guilds.Add(guild);
            if (!TryPersist(out error))
            {
                _db.Guilds.Remove(guild);
                _db.NextId--;
                return OpResult<GuildRecord>.Fail(error);
            }

            _invites.Remove(founder);
            return OpResult<GuildRecord>.Ok(guild);
        }

        public OpResult<GuildRecord> Invite(string inviter, string invitee)
        {
            var guild = GuildOf(inviter);
            var member = guild?.Member(inviter);
            if (member == null)
            {
                return OpResult<GuildRecord>.Fail("You aren't in a guild.");
            }

            if (!GuildRules.CanManage(member.Rank))
            {
                return OpResult<GuildRecord>.Fail("Only officers and the leader can invite.");
            }

            if (string.IsNullOrEmpty(invitee) || string.Equals(inviter, invitee, StringComparison.OrdinalIgnoreCase))
            {
                return OpResult<GuildRecord>.Fail("Invite whom?");
            }

            if (GuildOf(invitee) != null)
            {
                return OpResult<GuildRecord>.Fail($"{invitee} already belongs to a guild.");
            }

            if (guild.Members.Count >= GuildRules.MaxMembers)
            {
                return OpResult<GuildRecord>.Fail($"A guild holds at most {GuildRules.MaxMembers} members.");
            }

            _invites[invitee] = guild.Id;
            return OpResult<GuildRecord>.Ok(guild);
        }

        public void Decline(string name)
        {
            if (name != null)
            {
                _invites.Remove(name);
            }
        }

        public OpResult<GuildRecord> Accept(string name, int baseLevel, JobId job, int guildId)
        {
            if (string.IsNullOrEmpty(name) || !_invites.TryGetValue(name, out int invited) || invited != guildId)
            {
                return OpResult<GuildRecord>.Fail("That invitation has expired.");
            }

            _invites.Remove(name);
            var guild = Get(guildId);
            if (guild == null)
            {
                return OpResult<GuildRecord>.Fail("That guild has disbanded.");
            }

            if (GuildOf(name) != null)
            {
                return OpResult<GuildRecord>.Fail("Leave your guild first.");
            }

            if (guild.Members.Count >= GuildRules.MaxMembers)
            {
                return OpResult<GuildRecord>.Fail("That guild is full.");
            }

            var member = new GuildMember { Name = name, Rank = GuildRank.Member, BaseLevel = baseLevel, Job = job, JoinedUnixMs = _now(), Online = true };
            guild.Members.Add(member);
            if (!TryPersist(out string error))
            {
                guild.Members.Remove(member);
                return OpResult<GuildRecord>.Fail(error);
            }

            return OpResult<GuildRecord>.Ok(guild);
        }

        /// <summary>Members and officers leave; the leader must hand over the guild or disband it.</summary>
        public OpResult<GuildRecord> Leave(string name)
        {
            var guild = GuildOf(name);
            var member = guild?.Member(name);
            if (member == null)
            {
                return OpResult<GuildRecord>.Fail("You aren't in a guild.");
            }

            if (member.Rank == GuildRank.Leader && guild.Members.Count > 1)
            {
                return OpResult<GuildRecord>.Fail("Hand the guild to another member first (or disband it).");
            }

            if (guild.Members.Count == 1)
            {
                return Disband(name);
            }

            return Change(guild, () => guild.Members.Remove(member), () => guild.Members.Add(member));
        }

        /// <summary>Officers expel members; only the leader expels officers.</summary>
        public OpResult<GuildRecord> Expel(string by, string target)
        {
            var guild = GuildOf(by);
            var actor = guild?.Member(by);
            var victim = guild?.Member(target);
            if (actor == null || victim == null || actor == victim)
            {
                return OpResult<GuildRecord>.Fail($"{target} isn't in your guild.");
            }

            if (!GuildRules.CanManage(actor.Rank) || victim.Rank >= actor.Rank)
            {
                return OpResult<GuildRecord>.Fail("You can't expel them.");
            }

            return Change(guild, () => guild.Members.Remove(victim), () => guild.Members.Add(victim));
        }

        /// <summary>The leader makes a member an officer or an officer a member.</summary>
        public OpResult<GuildRecord> SetRank(string leader, string target, GuildRank rank)
        {
            var guild = GuildOf(leader);
            var actor = guild?.Member(leader);
            var member = guild?.Member(target);
            if (actor == null || actor.Rank != GuildRank.Leader)
            {
                return OpResult<GuildRecord>.Fail("Only the guild leader can change ranks.");
            }

            if (member == null || member == actor || rank == GuildRank.Leader)
            {
                return OpResult<GuildRecord>.Fail(rank == GuildRank.Leader ? "Use Hand over to pass the lead." : $"{target} isn't in your guild.");
            }

            var previous = member.Rank;
            return Change(guild, () => member.Rank = rank, () => member.Rank = previous);
        }

        public OpResult<GuildRecord> HandOver(string leader, string target)
        {
            var guild = GuildOf(leader);
            var actor = guild?.Member(leader);
            var member = guild?.Member(target);
            if (actor == null || actor.Rank != GuildRank.Leader)
            {
                return OpResult<GuildRecord>.Fail("Only the guild leader can hand it over.");
            }

            if (member == null || member == actor)
            {
                return OpResult<GuildRecord>.Fail($"{target} isn't in your guild.");
            }

            var previous = member.Rank;
            return Change(guild,
                () =>
                {
                    member.Rank = GuildRank.Leader;
                    actor.Rank = GuildRank.Officer;
                },
                () =>
                {
                    member.Rank = previous;
                    actor.Rank = GuildRank.Leader;
                });
        }

        public OpResult<GuildRecord> SetNotice(string by, string notice)
        {
            var guild = GuildOf(by);
            var actor = guild?.Member(by);
            if (actor == null || !GuildRules.CanManage(actor.Rank))
            {
                return OpResult<GuildRecord>.Fail("Only officers and the leader can write the notice.");
            }

            string previous = guild.Notice;
            string clean = ChatRules.Sanitize(notice, GuildRules.MaxNoticeLength);
            return Change(guild, () => guild.Notice = clean, () => guild.Notice = previous);
        }

        public OpResult<GuildRecord> Disband(string leader)
        {
            var guild = GuildOf(leader);
            var actor = guild?.Member(leader);
            if (actor == null || actor.Rank != GuildRank.Leader)
            {
                return OpResult<GuildRecord>.Fail("Only the guild leader can disband it.");
            }

            int index = _db.Guilds.IndexOf(guild);
            _db.Guilds.RemoveAt(index);
            if (!TryPersist(out string error))
            {
                _db.Guilds.Insert(index, guild);
                return OpResult<GuildRecord>.Fail(error);
            }

            var stale = new List<string>();
            foreach (var pair in _invites)
            {
                if (pair.Value == guild.Id)
                {
                    stale.Add(pair.Key);
                }
            }

            foreach (string name in stale)
            {
                _invites.Remove(name);
            }

            return OpResult<GuildRecord>.Ok(guild);
        }

        /// <summary>Level, job and online state shown in the member list (not saved until the next real change).</summary>
        public GuildRecord UpdateMember(string name, int baseLevel, JobId job, bool online)
        {
            var guild = GuildOf(name);
            var member = guild?.Member(name);
            if (member == null)
            {
                return null;
            }

            member.BaseLevel = baseLevel;
            member.Job = job;
            member.Online = online;
            return guild;
        }

        /// <summary>A character was deleted: leaves their guild, passing the lead on (or disbanding an empty guild).</summary>
        public GuildRecord Forget(string name)
        {
            _invites.Remove(name ?? string.Empty);
            var guild = GuildOf(name);
            var member = guild?.Member(name);
            if (member == null)
            {
                return null;
            }

            guild.Members.Remove(member);
            if (guild.Members.Count == 0)
            {
                _db.Guilds.Remove(guild);
                TryPersist(out _);
                return null;
            }

            if (member.Rank == GuildRank.Leader)
            {
                var next = guild.Members.Find(m => m.Rank == GuildRank.Officer) ?? guild.Members[0];
                next.Rank = GuildRank.Leader;
            }

            TryPersist(out _);
            return guild;
        }

        private OpResult<GuildRecord> Change(GuildRecord guild, Action apply, Action undo)
        {
            apply();
            if (!TryPersist(out string error))
            {
                undo();
                return OpResult<GuildRecord>.Fail(error);
            }

            return OpResult<GuildRecord>.Ok(guild);
        }

        private bool TryPersist(out string error)
        {
            try
            {
                _persist(_db);
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = "Could not save guild data: " + exception.Message;
                return false;
            }
        }
    }
}
