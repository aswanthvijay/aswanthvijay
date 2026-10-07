using System;
using System.Collections.Generic;
using Runeheir.Characters;
using Runeheir.Combat;

namespace Runeheir.Jobs
{
    /// <summary>
    /// Every job (Phase 7: the full Ragnarok / XileRO roster under Norse names). The numbers are saved, so existing
    /// values never change: Paladin (26) became the Gothi and Templar (36) the High Gothi.
    /// </summary>
    public enum JobId
    {
        Initiate = 0,

        // First jobs
        Warrior = 10,
        Scout = 11,
        Mystic = 12,
        Devotee = 13,
        Huntsman = 14,
        Trader = 15,

        // Second jobs
        Berserker = 20,
        Guardian = 21,
        Assassin = 22,
        Ranger = 23,
        Sorcerer = 24,
        Sage = 25,
        Gothi = 26,
        Monk = 27,
        Skald = 40,
        Seidkona = 41,
        Runesmith = 42,
        Brewmaster = 43,
        Outlaw = 44,

        // Transcendent jobs (after rebirth)
        Einherjar = 30,
        Valkyrie = 31,
        ShadowWalker = 32,
        Deadeye = 33,
        Archmage = 34,
        Chronomancer = 35,
        HighGothi = 36,
        Champion = 37,
        Thul = 50,
        Volva = 51,
        Forgelord = 52,
        Lifeweaver = 53,
        Vargr = 54,

        // Expanded jobs (no rebirth)
        Wanderer = 60,
        GlimaFighter = 61,
        SolGuardian = 62,
        FylgjaCaller = 63,
        Thunderer = 64,
        Nightraider = 65,
        FreyjasKin = 66,
    }

    /// <summary>Normal jobs go Initiate → 1st → 2nd → rebirth → High → transcendent; expanded jobs have their own paths and no rebirth.</summary>
    public enum JobFamily
    {
        Normal = 0,
        Expanded = 1,
    }

    public sealed class JobInfo
    {
        public JobId Id;
        public string Name;

        /// <summary>The Ragnarok class it stands for (Knight, Lord Knight...).</summary>
        public string RoName;

        /// <summary>One line about the job, for the job master and the guides.</summary>
        public string Description;

        /// <summary>0 = Initiate, 1 = first job, 2 = second job, 3 = transcendent. Expanded jobs sit at 1 or 2.</summary>
        public int Tier;

        public JobFamily Family;

        public JobId Parent;
        public bool HasParent;

        /// <summary>Jobs whose skills this one also learns besides its parents (the Wanderer learns every first job's).</summary>
        public JobId[] ExtraAncestors = Array.Empty<JobId>();

        public int MaxJobLevel;

        /// <summary>Job level needed to advance out of this job (int.MaxValue when it's the end of its path).</summary>
        public int JobChangeLevel = int.MaxValue;

        /// <summary>Base level needed to take this job (the Wanderer needs 45).</summary>
        public int MinBaseLevel;

        /// <summary>Picked when the character is made (Freyja's Kin, the Doram people), never changed into.</summary>
        public bool ChosenAtCreation;

        /// <summary>Which gear tier this job counts as (transcendent gear says 3).</summary>
        public int GearTier;

        /// <summary>The weapon the guild gives on reaching this job (an item in <c>ItemCatalog</c>), or null for none.</summary>
        public string StarterWeaponId;

        /// <summary>Weapon types this job can wield.</summary>
        public WeaponMask AllowedWeapons;

        /// <summary>The starter weapon as a +0 weapon profile.</summary>
        public WeaponProfile StarterWeapon => Items.ItemCatalog.Get(StarterWeaponId)?.ToWeaponProfile() ?? WeaponProfile.BareHands;

        /// <summary>The first job of this line (Warrior, Huntsman...), or the job itself for Initiates and line starters.</summary>
        public JobId Line;

        /// <summary>Placeholder outfit color for the capsule avatar.</summary>
        public string ColorHex;

        public bool IsTranscendent => Family == JobFamily.Normal && Tier == 3;

        public bool IsExpanded => Family == JobFamily.Expanded;
    }

    public static class JobDatabase
    {
        /// <summary>Default max job level per tier of the normal path (expanded jobs set their own).</summary>
        public static readonly int[] MaxJobLevelByTier = { 10, 50, 70, 120 };

        /// <summary>Default job level needed before advancing out of each tier of the normal path.</summary>
        public static readonly int[] JobChangeLevelByTier = { 10, 40, int.MaxValue, int.MaxValue };

        private static readonly Dictionary<JobId, JobInfo> Jobs = new Dictionary<JobId, JobInfo>();
        private static readonly List<JobInfo> Ordered = new List<JobInfo>();

        static JobDatabase()
        {
            // Weapon permissions (Ragnarok): what each job can wield.
            var initiate = WeaponMask.Unarmed | WeaponMask.Dagger | WeaponMask.OneHandSword | WeaponMask.Axe | WeaponMask.Staff | WeaponMask.Mace;
            var warrior = WeaponMask.Unarmed | WeaponMask.Dagger | WeaponMask.Swords | WeaponMask.Spear | WeaponMask.Axes | WeaponMask.Mace;
            var scout = WeaponMask.Unarmed | WeaponMask.Dagger | WeaponMask.OneHandSword | WeaponMask.Bow;
            var assassin = WeaponMask.Unarmed | WeaponMask.Dagger | WeaponMask.OneHandSword | WeaponMask.Katar;
            var mystic = WeaponMask.Unarmed | WeaponMask.Dagger | WeaponMask.Staff;
            var sage = mystic | WeaponMask.Book;
            var huntsman = WeaponMask.Unarmed | WeaponMask.Dagger | WeaponMask.Bow;
            var devotee = WeaponMask.Unarmed | WeaponMask.Mace | WeaponMask.Staff;
            var gothi = devotee | WeaponMask.Book;
            var monk = devotee | WeaponMask.Knuckle;
            var trader = WeaponMask.Unarmed | WeaponMask.Dagger | WeaponMask.OneHandSword | WeaponMask.Axes | WeaponMask.Mace;

            Add(JobId.Initiate, "Initiate", "Novice", 0, null, "rusty_seax", initiate, "#9C8D74",
                "Everyone begins here. Learn Basic Training, then choose a path at Job Level 10.");

            // ---- First jobs
            Add(JobId.Warrior, "Warrior", "Swordman", 1, JobId.Initiate, "iron_claymore", warrior, "#8E3B2E",
                "Sword and shield of the shield-wall: high HP, Bash and Magnum Break.");
            Add(JobId.Mystic, "Mystic", "Mage", 1, JobId.Initiate, "oak_wand", mystic, "#3B4F8C",
                "A student of rune-magic: bolts of fire, frost and lightning.");
            Add(JobId.Huntsman, "Huntsman", "Archer", 1, JobId.Initiate, "hunters_bow", huntsman, "#5E7D3A",
                "Bow-hunter of the northern woods: Double Strafe, Arrow Shower, a keen eye.");
            Add(JobId.Devotee, "Devotee", "Acolyte", 1, JobId.Initiate, "iron_mace", devotee, "#C9A227",
                "Servant of the Aesir: heals, blessings and holy light.");
            Add(JobId.Trader, "Trader", "Merchant", 1, JobId.Initiate, "woodcutters_axe", trader, "#A0642D",
                "A market-town trader: better prices, a cart, and a zeny-backed axe swing.");
            Add(JobId.Scout, "Scout", "Thief", 1, JobId.Initiate, "seax", scout, "#3E6B48",
                "Quick hands and quicker feet: double strikes, poison, stealing.");

            // ---- Second jobs
            Add(JobId.Berserker, "Berserker", "Knight", 2, JobId.Warrior, "flamberge", warrior, "#7B241C",
                "Two-handed fury: frenzied strikes that shake the battlefield.");
            Add(JobId.Guardian, "Guardian", "Crusader", 2, JobId.Warrior, "rune_lance", warrior, "#5D6D7E",
                "Holy shield-bearer: guards allies, smites with the Sacred Cross.");
            Add(JobId.Sorcerer, "Sorcerer", "Wizard", 2, JobId.Mystic, "runed_staff", mystic, "#2C3E7A",
                "Master of destructive seidr: storms of fire, ice and lightning.");
            Add(JobId.Sage, "Sage", "Sage", 2, JobId.Mystic, "rune_tome_staff", sage, "#4A6FA5",
                "Scholar of runes: free casting, auto-runes, ground magic.");
            Add(JobId.Ranger, "Ranger", "Hunter", 2, JobId.Huntsman, "yew_longbow", huntsman, "#4A7A3A",
                "Trapper with a hunting raven: snares, mines and Raven Strike.");
            Add(JobId.Skald, "Skald", "Bard", 2, JobId.Huntsman, "tagelharpa", huntsman | WeaponMask.Instrument, "#8C5A2B",
                "Court poet of the jarls: songs that strengthen the whole warband.");
            Add(JobId.Seidkona, "Seidkona", "Dancer", 2, JobId.Huntsman, "seidr_lash", huntsman | WeaponMask.Whip, "#8E44AD",
                "Seidr-woman whose dances bend fate against her foes.");
            Add(JobId.Gothi, "Gothi", "Priest", 2, JobId.Devotee, "holy_mace", gothi, "#D4AC0D",
                "Temple priest: Sanctuary, resurrection, banishing the dead.");
            Add(JobId.Monk, "Monk", "Monk", 2, JobId.Devotee, "iron_knuckles", monk, "#B9770E",
                "Fighting ascetic: spirit spheres, combos, Occult Strike.");
            Add(JobId.Runesmith, "Runesmith", "Blacksmith", 2, JobId.Trader, "dwarven_axe", trader, "#6E2C00",
                "Dwarf-taught smith: forges runed weapons and hammers foes flat.");
            Add(JobId.Brewmaster, "Brewmaster", "Alchemist", 2, JobId.Trader, "herbwife_sickle", trader, "#117A65",
                "Brewer of potions and bombs, grower of man-eating plants.");
            Add(JobId.Assassin, "Assassin", "Assassin", 2, JobId.Scout, "jamadhar", assassin, "#1E3D2F",
                "Katar killer of the shadows: cloaking, poison, Sonic Blow.");
            Add(JobId.Outlaw, "Outlaw", "Rogue", 2, JobId.Scout, "cutpurse_dagger", scout, "#5B4636",
                "Loki's own: strips foes, copies skills, snatches zeny.");

            // ---- Transcendent jobs (rebirth at Urðr's Well; Parent is the second job they continue)
            Add(JobId.Einherjar, "Einherjar", "Lord Knight", 3, JobId.Berserker, "einherjar_greatsword", warrior, "#922B21",
                "Chosen of Valhalla: Vortex Cleave, Rage of Thor, Spiral Pierce.");
            Add(JobId.Valkyrie, "Valkyrie", "Paladin", 3, JobId.Guardian, "valkyrian_lance", warrior, "#AEB6BF",
                "Odin's shield-maiden: Valkyrie's Descent, Gloria, Shield Chain.");
            Add(JobId.Archmage, "Archmage", "High Wizard", 3, JobId.Sorcerer, "yggdrasil_staff", mystic, "#1B2A6B",
                "Rune-lord of the nine realms: Glacial Tempest, Runic Aegis.");
            Add(JobId.Chronomancer, "Chronomancer", "Professor", 3, JobId.Sage, "norn_staff", sage, "#5B2C6F",
                "Scholar of the Norns' threads: stasis, haste, slowed time.");
            Add(JobId.Deadeye, "Deadeye", "Sniper", 3, JobId.Ranger, "raven_longbow", huntsman, "#1D4D2B",
                "Huginn's eye: Sharp Shot, Raven Assault, True Sight.");
            Add(JobId.Thul, "Thul", "Minstrel", 3, JobId.Skald, "bragis_harp", huntsman | WeaponMask.Instrument, "#A04000",
                "Bragi's chanter: songs of the gods and sound that shatters.");
            Add(JobId.Volva, "Völva", "Gypsy", 3, JobId.Seidkona, "serpent_lash", huntsman | WeaponMask.Whip, "#6C3483",
                "Far-seeing seeress: dances that unravel fate itself.");
            Add(JobId.HighGothi, "High Gothi", "High Priest", 3, JobId.Gothi, "templar_mace", gothi, "#F4D03F",
                "High priest of the hof: Meditatio, Assumptio, Holy Judgment.");
            Add(JobId.Champion, "Champion", "Champion", 3, JobId.Monk, "fist_of_odin_knuckles", monk, "#CA6F1E",
                "Odin's fist: the five-sphere combo ending in Fist of Odin.");
            Add(JobId.Forgelord, "Forgelord", "Mastersmith", 3, JobId.Runesmith, "eitri_great_axe", trader, "#4D2600",
                "Heir of Brokkr and Eitri: Cart Termination and masterwork arms.");
            Add(JobId.Lifeweaver, "Lifeweaver", "Biochemist", 3, JobId.Brewmaster, "idunn_sickle", trader, "#0E6655",
                "Keeper of Iðunn's lore: Acid Demonstration, plant cultivation.");
            Add(JobId.ShadowWalker, "Shadow Walker", "Assassin Cross", 3, JobId.Assassin, "shadow_katar", assassin, "#17202A",
                "Death from the dark: Phantom Barrage, Miasma Weapon, Shadow Veil.");
            Add(JobId.Vargr, "Vargr", "Stalker", 3, JobId.Outlaw, "lokis_sting", scout, "#3B2F2F",
                "The wolf-outlaw: Full Strip, Chase Walk, Preserve.");

            // ---- Expanded jobs (Ragnarok's special classes; no rebirth, base level 255 on their own)
            Add(JobId.Wanderer, "Wanderer", "Super Novice", 2, JobId.Initiate, "seax", initiate, "#B7950B",
                "An Initiate who never chose: learns every first job's skills. Needs Base Level 45.",
                JobFamily.Expanded, maxJobLevel: 99, gearTier: 3, minBaseLevel: 45,
                extraAncestors: new[] { JobId.Warrior, JobId.Mystic, JobId.Huntsman, JobId.Devotee, JobId.Trader, JobId.Scout });
            Add(JobId.GlimaFighter, "Glíma Fighter", "Taekwon", 1, JobId.Initiate, null, WeaponMask.Unarmed, "#C0392B",
                "Norse wrestler-kicker: stances, kicks and tumbling. Fights bare-handed.",
                JobFamily.Expanded, maxJobLevel: 50, gearTier: 1, jobChangeLevel: 40);
            Add(JobId.SolGuardian, "Sól Guardian", "Star Gladiator", 2, JobId.GlimaFighter, null, WeaponMask.Unarmed, "#E67E22",
                "Sworn to Sól, Máni and the stars: celestial kicks and auras.",
                JobFamily.Expanded, maxJobLevel: 70, gearTier: 3);
            Add(JobId.FylgjaCaller, "Fylgja Caller", "Soul Linker", 2, JobId.GlimaFighter, "fylgja_wand",
                WeaponMask.Unarmed | WeaponMask.Dagger | WeaponMask.Staff, "#2E86C1",
                "Calls the fylgjur, guardian spirits, to bind with allies.",
                JobFamily.Expanded, maxJobLevel: 70, gearTier: 3);
            Add(JobId.Thunderer, "Thunderer", "Gunslinger", 1, JobId.Initiate, "spark_rod", WeaponMask.Unarmed | WeaponMask.ThunderRod, "#7F8C8D",
                "Wields Thor's lightning through iron thunder-rods; flips Thor's coins.",
                JobFamily.Expanded, maxJobLevel: 70, gearTier: 3);
            Add(JobId.Nightraider, "Nightraider", "Ninja", 1, JobId.Initiate, "iron_huuma", WeaponMask.Unarmed | WeaponMask.Dagger | WeaponMask.Huuma,
                "#212F3D", "Raider from the long night: thrown blades, shadow steps, rune-fire.",
                JobFamily.Expanded, maxJobLevel: 70, gearTier: 3);
            Add(JobId.FreyjasKin, "Freyja's Kin", "Doram (Summoner)", 1, null, "bygul_staff", WeaponMask.Unarmed | WeaponMask.CatStaff, "#E59866",
                "Cat-folk of Freyja's chariot (Bygul and Trjegul's kin). Chosen when a character is made.",
                JobFamily.Expanded, maxJobLevel: 70, gearTier: 3, chosenAtCreation: true);

            foreach (var job in Ordered)
            {
                var first = job;
                while (first.Tier > 1 && first.HasParent)
                {
                    first = Jobs[first.Parent];
                }

                job.Line = first.Id;
            }
        }

        /// <summary>Every job, in roster order (Initiate, first jobs, second, transcendent, expanded).</summary>
        public static IEnumerable<JobInfo> All => Ordered;

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

        /// <summary>The job's name as shown, with "High" for reborn Initiates and first jobs (High Warrior).</summary>
        public static string NameFor(JobId id, bool reborn)
        {
            var job = Get(id);
            return reborn && job.Family == JobFamily.Normal && job.Tier <= 1 ? "High " + job.Name : job.Name;
        }

        public static string NameFor(CharacterRecord record)
        {
            return record == null ? string.Empty : NameFor(record.Job, record.Reborn);
        }

        /// <summary>
        /// True if <paramref name="job"/> learns <paramref name="ancestor"/>'s skills: it is that job, one of its earlier
        /// jobs, or (the Wanderer) one of its extra ancestors.
        /// </summary>
        public static bool IsSelfOrAncestor(JobId ancestor, JobId job)
        {
            var current = Get(job);
            foreach (var extra in current.ExtraAncestors)
            {
                if (extra == ancestor)
                {
                    return true;
                }
            }

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

        /// <summary>Jobs that list <paramref name="id"/> as their parent (the transcendent job of a second job included).</summary>
        public static List<JobInfo> ChildrenOf(JobId id)
        {
            var children = new List<JobInfo>();
            foreach (var job in Ordered)
            {
                if (job.HasParent && job.Parent == id)
                {
                    children.Add(job);
                }
            }

            return children;
        }

        /// <summary>The second jobs of a first job's line (Huntsman: Ranger, Skald, Seidkona).</summary>
        public static List<JobInfo> SecondJobsOf(JobId firstJob)
        {
            var result = new List<JobInfo>();
            foreach (var job in Ordered)
            {
                if (job.Family == JobFamily.Normal && job.Tier == 2 && job.Parent == firstJob)
                {
                    result.Add(job);
                }
            }

            return result;
        }

        /// <summary>The transcendent job that continues a second job (Berserker → Einherjar), or null.</summary>
        public static JobInfo TranscendentOf(JobId secondJob)
        {
            foreach (var job in Ordered)
            {
                if (job.IsTranscendent && job.Parent == secondJob)
                {
                    return job;
                }
            }

            return null;
        }

        /// <summary>The jobs this character could change into next (requirements aside): what the job master offers.</summary>
        public static List<JobInfo> NextJobs(CharacterRecord record)
        {
            var result = new List<JobInfo>();
            if (record == null || !Exists(record.Job))
            {
                return result;
            }

            var current = Get(record.Job);
            if (record.Reborn)
            {
                if (current.Id == JobId.Initiate && Exists(record.RebirthPath))
                {
                    result.Add(Get(Get(record.RebirthPath).Line));
                }
                else if (current.Family == JobFamily.Normal && current.Tier == 1)
                {
                    var next = TranscendentOf(record.RebirthPath);
                    if (next != null && next.Line == current.Id)
                    {
                        result.Add(next);
                    }
                }

                return result;
            }

            foreach (var job in ChildrenOf(current.Id))
            {
                if (!job.IsTranscendent && !job.ChosenAtCreation)
                {
                    result.Add(job);
                }
            }

            return result;
        }

        /// <summary>
        /// The job change rule (Ragnarok): the next step of your own path at the job level the current job asks for.
        /// Reborn characters retrace their first life: High Initiate → High first job → the transcendent job of the
        /// second job they had. Expanded jobs can't be taken after rebirth. Basic Training is checked by
        /// <see cref="CharacterProgression.TryChangeJob"/>.
        /// </summary>
        public static bool CanChangeJob(CharacterRecord record, JobId target, out string reason)
        {
            if (record == null || !Exists(record.Job) || !Exists(target))
            {
                reason = "Unknown job.";
                return false;
            }

            var current = Get(record.Job);
            var next = Get(target);
            if (next.ChosenAtCreation)
            {
                reason = $"{next.Name} is a people chosen when a character is made, not a job to change into.";
                return false;
            }

            if (!NextJobs(record).Exists(j => j.Id == target))
            {
                reason = PathReason(record, current, next);
                return false;
            }

            if (record.JobLevel < current.JobChangeLevel)
            {
                reason = $"Requires Job Level {current.JobChangeLevel} as {NameFor(record)}.";
                return false;
            }

            if (record.BaseLevel < next.MinBaseLevel)
            {
                reason = $"{next.Name} requires Base Level {next.MinBaseLevel}.";
                return false;
            }

            reason = null;
            return true;
        }

        private static string PathReason(CharacterRecord record, JobInfo current, JobInfo next)
        {
            if (next.IsTranscendent && !record.Reborn)
            {
                return $"{next.Name} is a transcendent job: be reborn at Urðr's Well first (a second job at Base Level 99).";
            }

            if (record.Reborn)
            {
                if (next.IsExpanded)
                {
                    return $"{next.Name} can't be taken after rebirth.";
                }

                var path = Exists(record.RebirthPath) ? Get(record.RebirthPath) : null;
                var transcendent = path != null ? TranscendentOf(path.Id) : null;
                if (path != null && transcendent != null)
                {
                    return $"Your first life was a {path.Name}: your path is High {Get(path.Line).Name}, then {transcendent.Name}.";
                }
            }

            return $"{next.Name} is not the next step after {NameFor(record)}.";
        }

        // RO class names and short forms, for @job and the GDD's abbreviations.
        private static readonly Dictionary<string, JobId> Aliases = new Dictionary<string, JobId>
        {
            { "chrono", JobId.Chronomancer },
            { "shadow", JobId.ShadowWalker },
            { "novice", JobId.Initiate },
            { "swordman", JobId.Warrior },
            { "swordsman", JobId.Warrior },
            { "mage", JobId.Mystic },
            { "archer", JobId.Huntsman },
            { "acolyte", JobId.Devotee },
            { "merchant", JobId.Trader },
            { "thief", JobId.Scout },
            { "knight", JobId.Berserker },
            { "crusader", JobId.Guardian },
            { "wizard", JobId.Sorcerer },
            { "hunter", JobId.Ranger },
            { "bard", JobId.Skald },
            { "dancer", JobId.Seidkona },
            { "priest", JobId.Gothi },
            { "blacksmith", JobId.Runesmith },
            { "alchemist", JobId.Brewmaster },
            { "rogue", JobId.Outlaw },
            { "lordknight", JobId.Einherjar },
            { "paladin", JobId.Valkyrie },
            { "highwizard", JobId.Archmage },
            { "professor", JobId.Chronomancer },
            { "sniper", JobId.Deadeye },
            { "minstrel", JobId.Thul },
            { "clown", JobId.Thul },
            { "gypsy", JobId.Volva },
            { "volva", JobId.Volva },
            { "highpriest", JobId.HighGothi },
            { "templar", JobId.HighGothi },
            { "mastersmith", JobId.Forgelord },
            { "whitesmith", JobId.Forgelord },
            { "biochemist", JobId.Lifeweaver },
            { "creator", JobId.Lifeweaver },
            { "assassincross", JobId.ShadowWalker },
            { "stalker", JobId.Vargr },
            { "supernovice", JobId.Wanderer },
            { "taekwon", JobId.GlimaFighter },
            { "glima", JobId.GlimaFighter },
            { "stargladiator", JobId.SolGuardian },
            { "sol", JobId.SolGuardian },
            { "soullinker", JobId.FylgjaCaller },
            { "fylgja", JobId.FylgjaCaller },
            { "gunslinger", JobId.Thunderer },
            { "ninja", JobId.Nightraider },
            { "doram", JobId.FreyjasKin },
            { "summoner", JobId.FreyjasKin },
            { "freyja", JobId.FreyjasKin },
        };

        /// <summary>Accepts "einherjar", "Shadow Walker", "shadow_walker", Ragnarok names ("lord knight", "gypsy") and short forms.</summary>
        public static bool TryParse(string text, out JobId id)
        {
            id = JobId.Initiate;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string wanted = Normalize(text);
            foreach (var job in Ordered)
            {
                if (Normalize(job.Name) == wanted || Normalize(job.Id.ToString()) == wanted)
                {
                    id = job.Id;
                    return true;
                }
            }

            return Aliases.TryGetValue(wanted, out id);
        }

        private static string Normalize(string value)
        {
            var chars = new List<char>(value.Length);
            foreach (char c in value)
            {
                if (char.IsLetterOrDigit(c))
                {
                    // Ö, Í and Ó fold to their plain letters so "volva" finds the Völva and "glima" the Glíma Fighter.
                    char lower = char.ToLowerInvariant(c);
                    switch (lower)
                    {
                        case 'ö': lower = 'o'; break;
                        case 'í': lower = 'i'; break;
                        case 'ó': lower = 'o'; break;
                        case 'á': lower = 'a'; break;
                        case 'ð': lower = 'd'; break;
                    }

                    chars.Add(lower);
                }
            }

            return new string(chars.ToArray());
        }

        private static void Add(JobId id, string name, string roName, int tier, JobId? parent, string weaponId, WeaponMask weapons, string color,
            string description, JobFamily family = JobFamily.Normal, int maxJobLevel = 0, int gearTier = -1, int minBaseLevel = 0,
            int jobChangeLevel = 0, bool chosenAtCreation = false, JobId[] extraAncestors = null)
        {
            var job = new JobInfo
            {
                Id = id,
                Name = name,
                RoName = roName,
                Description = description,
                Tier = tier,
                Family = family,
                Parent = parent ?? JobId.Initiate,
                HasParent = parent.HasValue,
                ExtraAncestors = extraAncestors ?? Array.Empty<JobId>(),
                MaxJobLevel = maxJobLevel > 0 ? maxJobLevel : MaxJobLevelByTier[tier],
                JobChangeLevel = jobChangeLevel > 0 ? jobChangeLevel : family == JobFamily.Normal ? JobChangeLevelByTier[tier] : int.MaxValue,
                MinBaseLevel = minBaseLevel,
                ChosenAtCreation = chosenAtCreation,
                GearTier = gearTier >= 0 ? gearTier : tier,
                StarterWeaponId = weaponId,
                AllowedWeapons = weapons,
                ColorHex = color,
            };

            Jobs[id] = job;
            Ordered.Add(job);
        }
    }
}
