using System.Collections.Generic;
using Runeheir.Combat;
using Runeheir.Jobs;
using Runeheir.Stats;

namespace Runeheir.Skills
{
    /// <summary>
    /// Every skill of every job: the GDD's 21 jobs (§3) and, from Phase 7, Ragnarok's whole roster under Norse names, split
    /// by job line across the SkillCatalog.*.cs files.
    /// One skill point per job level; most skills go to Lv 10, signature skills to Lv 5 or 10.
    /// The GDD signature skills keep their Phase 2 numbers at max level.
    /// </summary>
    public static partial class SkillCatalog
    {
        public const string FirstAid = "first_aid";

        private static readonly Dictionary<string, SkillDefinition> ById = new Dictionary<string, SkillDefinition>();
        private static readonly List<SkillDefinition> Ordered = new List<SkillDefinition>();

        static SkillCatalog()
        {
            RegisterInitiate();
            RegisterWarriorLine();
            RegisterScoutLine();
            RegisterMysticLine();
            RegisterDevoteeLine();
            RegisterHuntsmanLine();
            RegisterTraderLine();
            RegisterRosterAdditions();
            RegisterExpandedJobs();
        }

        public static IReadOnlyList<SkillDefinition> All => Ordered;

        public static SkillDefinition Get(string id)
        {
            return id != null && ById.TryGetValue(id, out var skill) ? skill : null;
        }

        /// <summary>Skills a job can learn and use: its own plus every ancestor's (Einherjar also has Bash). No hidden ones.</summary>
        public static List<SkillDefinition> ForJob(JobId job)
        {
            var result = new List<SkillDefinition>();
            foreach (var skill in Ordered)
            {
                if (!skill.Hidden && (skill.Granted || JobDatabase.IsSelfOrAncestor(skill.Job, job)))
                {
                    result.Add(skill);
                }
            }

            return result;
        }

        /// <summary>Skills taught by exactly this job (one page of the skill tree).</summary>
        public static List<SkillDefinition> OwnedBy(JobId job)
        {
            var result = new List<SkillDefinition>();
            foreach (var skill in Ordered)
            {
                if (!skill.Hidden && skill.Job == job)
                {
                    result.Add(skill);
                }
            }

            return result;
        }

        /// <summary>
        /// True when <paramref name="job"/>'s line includes the skill (learning is checked by <see cref="SkillBook"/>).
        /// Granted skills (First Aid) belong to everyone, Freyja's Kin included.
        /// </summary>
        public static bool CanUse(JobId job, string skillId)
        {
            var skill = Get(skillId);
            return skill != null && (skill.Granted || JobDatabase.IsSelfOrAncestor(skill.Job, job));
        }

        private static void Register(SkillDefinition skill)
        {
            ById[skill.Id] = skill;
            Ordered.Add(skill);
        }

        /// <summary>Lv 1 value plus a step per level.</summary>
        private static LevelValue L(float atLevel1, float perLevel)
        {
            return new LevelValue(atLevel1, perLevel);
        }

        /// <summary>Explicit value per level.</summary>
        private static LevelValue T(params float[] values)
        {
            return LevelValue.Table(values);
        }

        private static SkillRequirement[] Req(string id, int level)
        {
            return new[] { new SkillRequirement(id, level) };
        }

        private static SkillRequirement[] Req(string id, int level, string id2, int level2)
        {
            return new[] { new SkillRequirement(id, level), new SkillRequirement(id2, level2) };
        }

        // ------------------------------------------------------------ Initiate
        private static void RegisterInitiate()
        {
            Register(new SkillDefinition
            {
                Id = SkillBook.BasicTrainingId, Name = "Basic Training", Job = JobId.Initiate, MaxLevel = 9, Passive = true,
                Description = "Survival basics: +1 HIT, +1 FLEE and +1 HP regen per level.",
                IconLabel = "BT", IconColorHex = "#9C8D74",
                PassivePerLevel = new StatModifiers { Hit = 1, Flee = 1, HpRegenFlat = 1 },
            });
            Register(new SkillDefinition
            {
                Id = FirstAid, Name = "First Aid", Job = JobId.Initiate, Granted = true,
                Description = "Bandage your wounds: heals 25 + Base Level HP.",
                IconLabel = "FA", IconColorHex = "#27AE60",
                Target = SkillTarget.Self, Special = SkillSpecial.Heal, Motion = SkillMotion.Buff,
                SpCost = 3, FlatHeal = 25, AfterCastDelay = 0.5f,
            });
        }
    }
}
