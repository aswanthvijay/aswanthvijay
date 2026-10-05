using System;
using System.Collections.Generic;
using Runeheir.Combat;

namespace Runeheir.Jobs
{
    /// <summary>GDD §3 job evolution tree.</summary>
    public enum JobId
    {
        Initiate = 0,

        Warrior = 10,
        Scout = 11,
        Mystic = 12,
        Devotee = 13,

        Berserker = 20,
        Guardian = 21,
        Assassin = 22,
        Ranger = 23,
        Sorcerer = 24,
        Sage = 25,
        Paladin = 26,
        Monk = 27,

        Einherjar = 30,
        Valkyrie = 31,
        ShadowWalker = 32,
        Deadeye = 33,
        Archmage = 34,
        Chronomancer = 35,
        Templar = 36,
        Champion = 37,
    }

    public sealed class JobInfo
    {
        public JobId Id;
        public string Name;

        /// <summary>0 = Initiate, 1 = first job, 2 = second job, 3 = Ascended.</summary>
        public int Tier;

        public JobId Parent;
        public bool HasParent;
        public int MaxJobLevel;

        /// <summary>Phase 2 stand-in until the equipment system (Phase 4) exists.</summary>
        public WeaponProfile StarterWeapon;

        /// <summary>Placeholder outfit color for the capsule avatar.</summary>
        public string ColorHex;
    }

    public static class JobDatabase
    {
        /// <summary>
        /// Max job level per tier. GDD only fixes the Ascended cap (120); the others are
        /// Ragnarok-style defaults for design to tune.
        /// </summary>
        public static readonly int[] MaxJobLevelByTier = { 10, 50, 70, 120 };

        /// <summary>Job level needed before advancing out of each tier.</summary>
        public static readonly int[] JobChangeLevelByTier = { 10, 40, 70, int.MaxValue };

        private static readonly Dictionary<JobId, JobInfo> Jobs = new Dictionary<JobId, JobInfo>();

        static JobDatabase()
        {
            var knife = new WeaponProfile("Rusty Seax", WeaponType.Dagger, 17, 1);

            Add(JobId.Initiate, "Initiate", 0, null, knife, "#9C8D74");

            Add(JobId.Warrior, "Warrior", 1, JobId.Initiate, new WeaponProfile("Iron Claymore", WeaponType.TwoHandSword, 100, 2), "#8E3B2E");
            Add(JobId.Scout, "Scout", 1, JobId.Initiate, new WeaponProfile("Seax", WeaponType.Dagger, 64, 2), "#3E6B48");
            Add(JobId.Mystic, "Mystic", 1, JobId.Initiate, new WeaponProfile("Oak Wand", WeaponType.Staff, 25, 1), "#3B4F8C");
            Add(JobId.Devotee, "Devotee", 1, JobId.Initiate, new WeaponProfile("Iron Mace", WeaponType.Mace, 75, 2), "#C9A227");

            Add(JobId.Berserker, "Berserker", 2, JobId.Warrior, new WeaponProfile("Flamberge", WeaponType.TwoHandSword, 160, 3), "#7B241C");
            Add(JobId.Guardian, "Guardian", 2, JobId.Warrior, new WeaponProfile("Rune Lance", WeaponType.Spear, 150, 3), "#5D6D7E");
            Add(JobId.Assassin, "Assassin", 2, JobId.Scout, new WeaponProfile("Jamadhar", WeaponType.Katar, 120, 3), "#1E3D2F");
            Add(JobId.Ranger, "Ranger", 2, JobId.Scout, new WeaponProfile("Yew Longbow", WeaponType.Bow, 110, 3), "#4A7A3A");
            Add(JobId.Sorcerer, "Sorcerer", 2, JobId.Mystic, new WeaponProfile("Runed Staff", WeaponType.Staff, 45, 2), "#2C3E7A");
            Add(JobId.Sage, "Sage", 2, JobId.Mystic, new WeaponProfile("Rune Tome Staff", WeaponType.Staff, 50, 2), "#4A6FA5");
            Add(JobId.Paladin, "Paladin", 2, JobId.Devotee, new WeaponProfile("Holy Mace", WeaponType.Mace, 130, 3), "#D4AC0D");
            Add(JobId.Monk, "Monk", 2, JobId.Devotee, new WeaponProfile("Iron Knuckles", WeaponType.Knuckle, 110, 3), "#B9770E");

            Add(JobId.Einherjar, "Einherjar", 3, JobId.Berserker, new WeaponProfile("Einherjar Greatsword", WeaponType.TwoHandSword, 230, 4), "#922B21");
            Add(JobId.Valkyrie, "Valkyrie", 3, JobId.Guardian, new WeaponProfile("Valkyrian Lance", WeaponType.Spear, 220, 4), "#AEB6BF");
            Add(JobId.ShadowWalker, "Shadow Walker", 3, JobId.Assassin, new WeaponProfile("Shadow Katar", WeaponType.Katar, 190, 4), "#17202A");
            Add(JobId.Deadeye, "Deadeye", 3, JobId.Ranger, new WeaponProfile("Raven Longbow", WeaponType.Bow, 180, 4), "#1D4D2B");
            Add(JobId.Archmage, "Archmage", 3, JobId.Sorcerer, new WeaponProfile("Yggdrasil Staff", WeaponType.Staff, 80, 4), "#1B2A6B");
            Add(JobId.Chronomancer, "Chronomancer", 3, JobId.Sage, new WeaponProfile("Norn Staff", WeaponType.Staff, 85, 4), "#5B2C6F");
            Add(JobId.Templar, "Templar", 3, JobId.Paladin, new WeaponProfile("Templar Mace", WeaponType.Mace, 200, 4), "#F4D03F");
            Add(JobId.Champion, "Champion", 3, JobId.Monk, new WeaponProfile("Fist of Odin Knuckles", WeaponType.Knuckle, 180, 4), "#CA6F1E");
        }

        public static IEnumerable<JobInfo> All => Jobs.Values;

        public static JobInfo Get(JobId id)
        {
            if (!Jobs.TryGetValue(id, out var info))
            {
                throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown job");
            }

            return info;
        }

        public static int MaxJobLevel(JobId id)
        {
            return Get(id).MaxJobLevel;
        }

        /// <summary>True if <paramref name="ancestor"/> is <paramref name="job"/> or one of its earlier tiers.</summary>
        public static bool IsSelfOrAncestor(JobId ancestor, JobId job)
        {
            var current = Get(job);
            while (true)
            {
                if (current.Id == ancestor)
                {
                    return true;
                }

                if (!current.HasParent)
                {
                    return false;
                }

                current = Get(current.Parent);
            }
        }

        public static List<JobInfo> ChildrenOf(JobId id)
        {
            var children = new List<JobInfo>();
            foreach (var job in Jobs.Values)
            {
                if (job.HasParent && job.Parent == id)
                {
                    children.Add(job);
                }
            }

            return children;
        }

        /// <summary>Normal (non-GM) job change rule: next tier of your own branch, at the required job level.</summary>
        public static bool CanChangeJob(JobId current, int currentJobLevel, JobId target, out string reason)
        {
            var targetInfo = Get(target);
            var currentInfo = Get(current);
            if (!targetInfo.HasParent || targetInfo.Parent != current)
            {
                reason = $"{targetInfo.Name} is not the next step after {currentInfo.Name}.";
                return false;
            }

            int required = JobChangeLevelByTier[currentInfo.Tier];
            if (currentJobLevel < required)
            {
                reason = $"Requires Job Level {required} as {currentInfo.Name}.";
                return false;
            }

            reason = null;
            return true;
        }

        /// <summary>Accepts "einherjar", "Shadow Walker", "shadowwalker", "shadow_walker".</summary>
        public static bool TryParse(string text, out JobId id)
        {
            id = JobId.Initiate;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string wanted = Normalize(text);
            foreach (var job in Jobs.Values)
            {
                if (Normalize(job.Name) == wanted || Normalize(job.Id.ToString()) == wanted)
                {
                    id = job.Id;
                    return true;
                }
            }

            return false;
        }

        private static string Normalize(string value)
        {
            var chars = new List<char>(value.Length);
            foreach (char c in value)
            {
                if (char.IsLetterOrDigit(c))
                {
                    chars.Add(char.ToLowerInvariant(c));
                }
            }

            return new string(chars.ToArray());
        }

        private static void Add(JobId id, string name, int tier, JobId? parent, WeaponProfile weapon, string color)
        {
            Jobs[id] = new JobInfo
            {
                Id = id,
                Name = name,
                Tier = tier,
                Parent = parent ?? JobId.Initiate,
                HasParent = parent.HasValue,
                MaxJobLevel = MaxJobLevelByTier[tier],
                StarterWeapon = weapon,
                ColorHex = color,
            };
        }
    }
}
