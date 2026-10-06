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

        /// <summary>The weapon the guild gives on reaching this job (an item in <c>ItemCatalog</c>).</summary>
        public string StarterWeaponId;

        /// <summary>Weapon types this job can wield.</summary>
        public WeaponMask AllowedWeapons;

        /// <summary>The starter weapon as a +0 weapon profile.</summary>
        public WeaponProfile StarterWeapon => Items.ItemCatalog.Get(StarterWeaponId)?.ToWeaponProfile() ?? WeaponProfile.BareHands;

        /// <summary>The first job of this line (Warrior, Scout, Mystic, Devotee), or Initiate for Initiates.</summary>
        public JobId Line;

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
            // Weapon permissions by line (Ragnarok-style): what each job can wield.
            var initiate = WeaponMask.Unarmed | WeaponMask.Dagger | WeaponMask.OneHandSword | WeaponMask.Staff | WeaponMask.Mace;
            var warrior = WeaponMask.Unarmed | WeaponMask.Dagger | WeaponMask.Swords | WeaponMask.Spear | WeaponMask.Mace;
            var scout = WeaponMask.Unarmed | WeaponMask.Dagger | WeaponMask.OneHandSword | WeaponMask.Bow;
            var assassin = WeaponMask.Unarmed | WeaponMask.Dagger | WeaponMask.OneHandSword | WeaponMask.Katar;
            var ranger = WeaponMask.Unarmed | WeaponMask.Dagger | WeaponMask.Bow;
            var mystic = WeaponMask.Unarmed | WeaponMask.Dagger | WeaponMask.Staff;
            var devotee = WeaponMask.Unarmed | WeaponMask.Mace | WeaponMask.Staff;
            var paladin = devotee | WeaponMask.OneHandSword | WeaponMask.Spear;
            var monk = devotee | WeaponMask.Knuckle;

            Add(JobId.Initiate, "Initiate", 0, null, "rusty_seax", initiate, "#9C8D74");

            Add(JobId.Warrior, "Warrior", 1, JobId.Initiate, "iron_claymore", warrior, "#8E3B2E");
            Add(JobId.Scout, "Scout", 1, JobId.Initiate, "seax", scout, "#3E6B48");
            Add(JobId.Mystic, "Mystic", 1, JobId.Initiate, "oak_wand", mystic, "#3B4F8C");
            Add(JobId.Devotee, "Devotee", 1, JobId.Initiate, "iron_mace", devotee, "#C9A227");

            Add(JobId.Berserker, "Berserker", 2, JobId.Warrior, "flamberge", warrior, "#7B241C");
            Add(JobId.Guardian, "Guardian", 2, JobId.Warrior, "rune_lance", warrior, "#5D6D7E");
            Add(JobId.Assassin, "Assassin", 2, JobId.Scout, "jamadhar", assassin, "#1E3D2F");
            Add(JobId.Ranger, "Ranger", 2, JobId.Scout, "yew_longbow", ranger, "#4A7A3A");
            Add(JobId.Sorcerer, "Sorcerer", 2, JobId.Mystic, "runed_staff", mystic, "#2C3E7A");
            Add(JobId.Sage, "Sage", 2, JobId.Mystic, "rune_tome_staff", mystic, "#4A6FA5");
            Add(JobId.Paladin, "Paladin", 2, JobId.Devotee, "holy_mace", paladin, "#D4AC0D");
            Add(JobId.Monk, "Monk", 2, JobId.Devotee, "iron_knuckles", monk, "#B9770E");

            Add(JobId.Einherjar, "Einherjar", 3, JobId.Berserker, "einherjar_greatsword", warrior, "#922B21");
            Add(JobId.Valkyrie, "Valkyrie", 3, JobId.Guardian, "valkyrian_lance", warrior, "#AEB6BF");
            Add(JobId.ShadowWalker, "Shadow Walker", 3, JobId.Assassin, "shadow_katar", assassin, "#17202A");
            Add(JobId.Deadeye, "Deadeye", 3, JobId.Ranger, "raven_longbow", ranger, "#1D4D2B");
            Add(JobId.Archmage, "Archmage", 3, JobId.Sorcerer, "yggdrasil_staff", mystic, "#1B2A6B");
            Add(JobId.Chronomancer, "Chronomancer", 3, JobId.Sage, "norn_staff", mystic, "#5B2C6F");
            Add(JobId.Templar, "Templar", 3, JobId.Paladin, "templar_mace", paladin, "#F4D03F");
            Add(JobId.Champion, "Champion", 3, JobId.Monk, "fist_of_odin_knuckles", monk, "#CA6F1E");

            foreach (var job in Jobs.Values)
            {
                var first = job;
                while (first.Tier > 1)
                {
                    first = Jobs[first.Parent];
                }

                job.Line = first.Id;
            }
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

        public static bool Exists(JobId id)
        {
            return Jobs.ContainsKey(id);
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

        private static readonly Dictionary<string, JobId> Aliases = new Dictionary<string, JobId>
        {
            { "chrono", JobId.Chronomancer },
            { "shadow", JobId.ShadowWalker },
        };

        /// <summary>Accepts "einherjar", "Shadow Walker", "shadowwalker", "shadow_walker", and GDD short names ("chrono").</summary>
        public static bool TryParse(string text, out JobId id)
        {
            id = JobId.Initiate;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string wanted = Normalize(text);

            // Short names used in the GDD's job tree.
            if (Aliases.TryGetValue(wanted, out id))
            {
                return true;
            }

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

        private static void Add(JobId id, string name, int tier, JobId? parent, string weaponId, WeaponMask weapons, string color)
        {
            Jobs[id] = new JobInfo
            {
                Id = id,
                Name = name,
                Tier = tier,
                Parent = parent ?? JobId.Initiate,
                HasParent = parent.HasValue,
                MaxJobLevel = MaxJobLevelByTier[tier],
                StarterWeaponId = weaponId,
                AllowedWeapons = weapons,
                ColorHex = color,
            };
        }
    }
}
