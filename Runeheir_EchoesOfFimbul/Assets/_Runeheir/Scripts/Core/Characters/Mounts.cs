using System;
using Runeheir.Combat;
using Runeheir.Jobs;
using Runeheir.Skills;

namespace Runeheir.Characters
{
    /// <summary>
    /// Phase 8 mounts. Anyone can ride a fjord horse with its reins (Ragnarok's universal mounts: faster, but you get down
    /// to fight). The Berserker line rides Sleipnir's Foal and the Guardian line Gullinbursti the golden boar (Ragnarok's
    /// Peco Peco and Grand Peco): faster, with more carrying room, and an ASPD penalty Cavalry Mastery wears away.
    /// </summary>
    public static class MountRules
    {
        public const string Horse = "horse";
        public const string Warhorse = "warhorse";
        public const string Boar = "boar";

        public const string ReinsItemId = "fjord_horse_reins";

        /// <summary>The buff each mount gives while ridden (speed, weight, the cavalry ASPD penalty).</summary>
        public static string BuffFor(string mount)
        {
            switch (mount)
            {
                case Horse: return SkillBuffs.MountHorse;
                case Warhorse: return SkillBuffs.MountWarhorse;
                case Boar: return SkillBuffs.MountBoar;
                default: return null;
            }
        }

        public static string NameOf(string mount)
        {
            switch (mount)
            {
                case Horse: return "fjord horse";
                case Warhorse: return "Sleipnir's Foal";
                case Boar: return "Gullinbursti";
                default: return "mount";
            }
        }

        /// <summary>The fjord horse is for travel: riding it, you can't fight until you get down.</summary>
        public static bool BlocksCombat(string mount) => mount == Horse;

        /// <summary>The class mount and the mastery that lifts its ASPD penalty, for a job's line (null when it has none).</summary>
        public static (string Mount, string RideSkill, string MasterySkill) ClassMount(JobId job)
        {
            var line = JobDatabase.Exists(job) ? JobDatabase.Get(job) : null;
            if (line == null)
            {
                return (null, null, null);
            }

            if (job == JobId.Berserker || job == JobId.Einherjar)
            {
                return (Warhorse, "horsemanship", "cavalry_mastery");
            }

            if (job == JobId.Guardian || job == JobId.Valkyrie)
            {
                return (Boar, "boar_riding", "boar_mastery");
            }

            return (null, null, null);
        }

        /// <summary>A saved mount that the character can still ride (a lost job or skill puts them back on foot).</summary>
        public static bool StillValid(CharacterRecord record, string mount)
        {
            if (string.IsNullOrEmpty(mount) || record == null)
            {
                return false;
            }

            if (mount == Horse)
            {
                return true;
            }

            var (classMount, ride, _) = ClassMount(record.Job);
            return mount == classMount && ride != null && LearnedLevel(record, ride) > 0;
        }

        public static int LearnedLevel(CharacterRecord record, string skillId)
        {
            if (record?.Skills == null)
            {
                return 0;
            }

            foreach (var skill in record.Skills)
            {
                if (skill != null && string.Equals(skill.Id, skillId, StringComparison.OrdinalIgnoreCase))
                {
                    return skill.Level;
                }
            }

            return 0;
        }
    }
}
