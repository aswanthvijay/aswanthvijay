using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Runeheir.Accounts;
using Runeheir.Jobs;

namespace Runeheir.Social
{
    public enum ExpShareMode
    {
        /// <summary>Whoever lands the kill (or did the most damage) keeps the EXP.</summary>
        EachTakes = 0,

        /// <summary>GDD §8: the kill's EXP is split evenly between members on the map, while levels stay within 30.</summary>
        EvenShare = 1,
    }

    [Serializable]
    public sealed class PartyMember
    {
        public string Name;
        public int BaseLevel;
        public JobId Job;
        public string MapId;
        public bool Online;
        public int Hp;
        public int MaxHp;
    }

    /// <summary>A party as its members see it (sent to clients whenever it changes).</summary>
    [Serializable]
    public sealed class PartyInfo
    {
        public int Id;
        public string Name;
        public string Leader;
        public ExpShareMode ExpShare;
        public List<PartyMember> Members = new List<PartyMember>();

        public PartyMember Member(string name)
        {
            return Members.Find(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        public bool IsLeader(string name)
        {
            return string.Equals(Leader, name, StringComparison.OrdinalIgnoreCase);
        }

        public PartyInfo Clone()
        {
            var copy = (PartyInfo)MemberwiseClone();
            copy.Members = Members.ConvertAll(m => new PartyMember
            {
                Name = m.Name, BaseLevel = m.BaseLevel, Job = m.Job, MapId = m.MapId, Online = m.Online, Hp = m.Hp, MaxHp = m.MaxHp,
            });
            return copy;
        }
    }

    public static class PartyRules
    {
        public const int MaxMembers = 12;

        /// <summary>GDD §8: Even Share works while the party's highest and lowest base levels are at most 30 apart.</summary>
        public const int EvenShareLevelGap = 30;

        /// <summary>Each extra member sharing a kill adds this much to the pool before it's split.</summary>
        public const int BonusPercentPerExtraMember = 10;

        public const int MaxNameLength = 24;

        private static readonly Regex NamePattern = new Regex("^[A-Za-z0-9 '\\-]+\\z");

        public static bool ValidateName(string raw, out string name, out string error)
        {
            name = (raw ?? string.Empty).Trim();
            while (name.Contains("  "))
            {
                name = name.Replace("  ", " ");
            }

            if (name.Length < 1 || name.Length > MaxNameLength)
            {
                error = $"Party names are 1-{MaxNameLength} characters.";
                return false;
            }

            if (!NamePattern.IsMatch(name))
            {
                error = "Party names may use letters, numbers, spaces, ' and -.";
                return false;
            }

            error = null;
            return true;
        }

        public static int LevelGap(IEnumerable<int> levels)
        {
            int min = int.MaxValue;
            int max = int.MinValue;
            foreach (int level in levels)
            {
                min = Math.Min(min, level);
                max = Math.Max(max, level);
            }

            return max >= min ? max - min : 0;
        }

        public static bool CanEvenShare(PartyInfo party)
        {
            return party != null && LevelGap(party.Members.ConvertAll(m => m.BaseLevel)) <= EvenShareLevelGap;
        }

        /// <summary>
        /// Splits a kill's EXP between <paramref name="sharers"/> members: the pool grows by
        /// <see cref="BonusPercentPerExtraMember"/>% per extra member, then everyone gets an equal part (at least 1 when
        /// the kill was worth anything).
        /// </summary>
        public static long ShareEach(long exp, int sharers)
        {
            if (exp <= 0)
            {
                return 0;
            }

            sharers = Math.Max(1, Math.Min(MaxMembers, sharers));
            double pool = exp * (100.0 + BonusPercentPerExtraMember * (sharers - 1)) / 100.0;
            return Math.Max(1, (long)Math.Floor(pool / sharers));
        }
    }

    /// <summary>
    /// Every party on the server: create, invite, join, leave, kick, hand over the lead, and the EXP mode. Member names are
    /// character names (unique on a server). Members who log out stay in their party.
    /// </summary>
    public sealed class PartyBook
    {
        private readonly Dictionary<int, PartyInfo> _parties = new Dictionary<int, PartyInfo>();
        private readonly Dictionary<string, int> _memberOf = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _invites = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private int _nextId = 1;

        public IEnumerable<PartyInfo> All => _parties.Values;

        public PartyInfo Get(int id)
        {
            return _parties.TryGetValue(id, out var party) ? party : null;
        }

        public PartyInfo PartyOf(string name)
        {
            return name != null && _memberOf.TryGetValue(name, out int id) ? Get(id) : null;
        }

        /// <summary>The party <paramref name="name"/> has been invited to (0 = none).</summary>
        public int PendingInvite(string name)
        {
            return name != null && _invites.TryGetValue(name, out int id) && _parties.ContainsKey(id) ? id : 0;
        }

        public OpResult<PartyInfo> Create(PartyMember leader, string partyName)
        {
            if (leader == null || string.IsNullOrEmpty(leader.Name))
            {
                return OpResult<PartyInfo>.Fail("No character.");
            }

            if (PartyOf(leader.Name) != null)
            {
                return OpResult<PartyInfo>.Fail("Leave your party first.");
            }

            if (!PartyRules.ValidateName(partyName, out string name, out string error))
            {
                return OpResult<PartyInfo>.Fail(error);
            }

            foreach (var other in _parties.Values)
            {
                if (string.Equals(other.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return OpResult<PartyInfo>.Fail("A party with that name already exists.");
                }
            }

            var party = new PartyInfo { Id = _nextId++, Name = name, Leader = leader.Name, ExpShare = ExpShareMode.EachTakes };
            party.Members.Add(leader);
            _parties[party.Id] = party;
            _memberOf[leader.Name] = party.Id;
            _invites.Remove(leader.Name);
            return OpResult<PartyInfo>.Ok(party);
        }

        /// <summary>Only the leader invites; the invitee answers with <see cref="Accept"/> or <see cref="Decline"/>.</summary>
        public OpResult<PartyInfo> Invite(string leaderName, string inviteeName)
        {
            var party = PartyOf(leaderName);
            if (party == null)
            {
                return OpResult<PartyInfo>.Fail("You aren't in a party.");
            }

            if (!party.IsLeader(leaderName))
            {
                return OpResult<PartyInfo>.Fail("Only the party leader can invite.");
            }

            if (string.IsNullOrEmpty(inviteeName) || string.Equals(leaderName, inviteeName, StringComparison.OrdinalIgnoreCase))
            {
                return OpResult<PartyInfo>.Fail("Invite whom?");
            }

            if (PartyOf(inviteeName) != null)
            {
                return OpResult<PartyInfo>.Fail($"{inviteeName} is already in a party.");
            }

            if (party.Members.Count >= PartyRules.MaxMembers)
            {
                return OpResult<PartyInfo>.Fail($"A party holds at most {PartyRules.MaxMembers} members.");
            }

            _invites[inviteeName] = party.Id;
            return OpResult<PartyInfo>.Ok(party);
        }

        public void Decline(string name)
        {
            if (name != null)
            {
                _invites.Remove(name);
            }
        }

        /// <summary>
        /// Joins the party <paramref name="member"/> was invited to. When the new level gap is over 30, Even Share turns
        /// off (<paramref name="evenShareDropped"/>).
        /// </summary>
        public OpResult<PartyInfo> Accept(PartyMember member, int partyId, out bool evenShareDropped)
        {
            evenShareDropped = false;
            if (member == null || string.IsNullOrEmpty(member.Name))
            {
                return OpResult<PartyInfo>.Fail("No character.");
            }

            if (!_invites.TryGetValue(member.Name, out int invited) || invited != partyId)
            {
                return OpResult<PartyInfo>.Fail("That invitation has expired.");
            }

            _invites.Remove(member.Name);
            var party = Get(partyId);
            if (party == null)
            {
                return OpResult<PartyInfo>.Fail("That party has disbanded.");
            }

            if (PartyOf(member.Name) != null)
            {
                return OpResult<PartyInfo>.Fail("Leave your party first.");
            }

            if (party.Members.Count >= PartyRules.MaxMembers)
            {
                return OpResult<PartyInfo>.Fail("That party is full.");
            }

            party.Members.Add(member);
            _memberOf[member.Name] = party.Id;
            evenShareDropped = EnforceShareRule(party);
            return OpResult<PartyInfo>.Ok(party);
        }

        /// <summary>
        /// Leaves the party. The lead passes to the next member (online first); the last one out disbands it. Returns the
        /// party as it is now (null once disbanded).
        /// </summary>
        public PartyInfo Leave(string name)
        {
            var party = PartyOf(name);
            if (party == null)
            {
                return null;
            }

            party.Members.RemoveAll(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));
            _memberOf.Remove(name);
            if (party.Members.Count == 0)
            {
                Disband(party);
                return null;
            }

            if (party.IsLeader(name))
            {
                var next = party.Members.Find(m => m.Online) ?? party.Members[0];
                party.Leader = next.Name;
            }

            return party;
        }

        public OpResult<PartyInfo> Kick(string leaderName, string target)
        {
            var party = PartyOf(leaderName);
            if (party == null || !party.IsLeader(leaderName))
            {
                return OpResult<PartyInfo>.Fail("Only the party leader can expel members.");
            }

            if (party.Member(target) == null || string.Equals(leaderName, target, StringComparison.OrdinalIgnoreCase))
            {
                return OpResult<PartyInfo>.Fail($"{target} isn't in your party.");
            }

            Leave(target);
            return OpResult<PartyInfo>.Ok(party);
        }

        public OpResult<PartyInfo> MakeLeader(string leaderName, string target)
        {
            var party = PartyOf(leaderName);
            if (party == null || !party.IsLeader(leaderName))
            {
                return OpResult<PartyInfo>.Fail("Only the party leader can hand over the lead.");
            }

            var member = party.Member(target);
            if (member == null)
            {
                return OpResult<PartyInfo>.Fail($"{target} isn't in your party.");
            }

            party.Leader = member.Name;
            return OpResult<PartyInfo>.Ok(party);
        }

        public OpResult<PartyInfo> SetExpShare(string leaderName, ExpShareMode mode)
        {
            var party = PartyOf(leaderName);
            if (party == null || !party.IsLeader(leaderName))
            {
                return OpResult<PartyInfo>.Fail("Only the party leader can change how EXP is shared.");
            }

            if (mode == ExpShareMode.EvenShare && !PartyRules.CanEvenShare(party))
            {
                return OpResult<PartyInfo>.Fail($"Even Share needs every member within {PartyRules.EvenShareLevelGap} base levels of each other.");
            }

            party.ExpShare = mode;
            return OpResult<PartyInfo>.Ok(party);
        }

        /// <summary>
        /// Updates what the party sees of a member (level, map, HP, online). Returns the party (null when not in one);
        /// <paramref name="evenShareDropped"/> is set when a level-up pushed the gap past 30.
        /// </summary>
        public PartyInfo UpdateMember(string name, int baseLevel, JobId job, string mapId, bool online, int hp, int maxHp, out bool evenShareDropped)
        {
            evenShareDropped = false;
            var party = PartyOf(name);
            var member = party?.Member(name);
            if (member == null)
            {
                return null;
            }

            member.BaseLevel = baseLevel;
            member.Job = job;
            member.MapId = mapId;
            member.Online = online;
            member.Hp = hp;
            member.MaxHp = maxHp;
            evenShareDropped = EnforceShareRule(party);
            return party;
        }

        /// <summary>A character was deleted: out of any party and invitation.</summary>
        public PartyInfo Forget(string name)
        {
            Decline(name);
            return Leave(name);
        }

        /// <summary>
        /// Who gets a share of a kill on <paramref name="mapId"/>: with Even Share, every online member on that map for
        /// whom <paramref name="canShare"/> holds (alive); otherwise nobody (the killer keeps it).
        /// </summary>
        public List<PartyMember> Sharers(string killerName, string mapId, Func<PartyMember, bool> canShare)
        {
            var sharers = new List<PartyMember>();
            var party = PartyOf(killerName);
            if (party == null || party.ExpShare != ExpShareMode.EvenShare)
            {
                return sharers;
            }

            foreach (var member in party.Members)
            {
                if (member.Online && string.Equals(member.MapId, mapId, StringComparison.OrdinalIgnoreCase) && (canShare == null || canShare(member)))
                {
                    sharers.Add(member);
                }
            }

            return sharers;
        }

        private bool EnforceShareRule(PartyInfo party)
        {
            if (party.ExpShare == ExpShareMode.EvenShare && !PartyRules.CanEvenShare(party))
            {
                party.ExpShare = ExpShareMode.EachTakes;
                return true;
            }

            return false;
        }

        private void Disband(PartyInfo party)
        {
            _parties.Remove(party.Id);
            foreach (var member in party.Members)
            {
                _memberOf.Remove(member.Name);
            }

            var stale = new List<string>();
            foreach (var pair in _invites)
            {
                if (pair.Value == party.Id)
                {
                    stale.Add(pair.Key);
                }
            }

            foreach (string name in stale)
            {
                _invites.Remove(name);
            }
        }
    }
}
