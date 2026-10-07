using System;
using Runeheir.Characters;
using Runeheir.Jobs;

namespace Runeheir.Skills
{
    /// <summary>
    /// Loki's Mimicry (Phase 7, Ragnarok's Plagiarism): an Outlaw or Vargr keeps a copy of the last skill a nearby ally used,
    /// usable up to their Loki's Mimicry level (and never above the level it was used at). One copy at a time; Preserve keeps
    /// the current one. Like Ragnarok, only first- and second-job active skills can be copied: no passives, songs, crafting,
    /// resurrection, transcendent or expanded-job skills, and nothing of the Outlaw's own line.
    /// </summary>
    public static class MimicryRules
    {
        public const string SkillId = "lokis_mimicry";
        public const string PreserveSkillId = "preserve";

        /// <summary>How close (meters) an ally must be for the Outlaw to copy their skill.</summary>
        public const float CopyRange = 14f;

        /// <summary>The Loki's Mimicry level this character can use (0 if not learned or not their line).</summary>
        public static int MimicryLevel(CharacterRecord record)
        {
            return record != null && SkillCatalog.CanUse(record.Job, SkillId) ? SkillBook.LevelIn(record, SkillId) : 0;
        }

        public static bool CanCopy(SkillDefinition skill)
        {
            if (skill == null || skill.Passive || skill.Granted)
            {
                return false;
            }

            switch (skill.Special)
            {
                case SkillSpecial.Performance:
                case SkillSpecial.Craft:
                case SkillSpecial.Resurrect:
                    return false;
            }

            var owner = JobDatabase.Exists(skill.Job) ? JobDatabase.Get(skill.Job) : null;
            if (owner == null || owner.IsTranscendent || owner.IsExpanded || owner.Tier < 1)
            {
                return false;
            }

            // The thief line's own tricks (and Mimicry itself) aren't copied.
            return !JobDatabase.IsSelfOrAncestor(skill.Job, JobId.Vargr);
        }

        /// <summary>
        /// Copies <paramref name="skill"/>, used at <paramref name="usedLevel"/>, if this character can: returns true when the
        /// copy changed. <paramref name="preserved"/> (the Preserve buff) keeps an existing copy.
        /// </summary>
        public static bool TryCopy(CharacterRecord record, SkillDefinition skill, int usedLevel, bool preserved, out int level)
        {
            level = 0;
            int mimicry = MimicryLevel(record);
            if (mimicry <= 0 || !CanCopy(skill) || usedLevel <= 0)
            {
                return false;
            }

            if (preserved && !string.IsNullOrEmpty(record.MimicSkillId))
            {
                return false;
            }

            // Our own skills are already ours.
            if (SkillCatalog.CanUse(record.Job, skill.Id))
            {
                return false;
            }

            level = Math.Min(Math.Min(mimicry, usedLevel), skill.MaxLevel);
            if (record.MimicSkillId == skill.Id && record.MimicSkillLevel == level)
            {
                return false;
            }

            record.MimicSkillId = skill.Id;
            record.MimicSkillLevel = level;
            return true;
        }

        /// <summary>The level the copy can be used at for <paramref name="skillId"/> (0 if it isn't the copied skill).</summary>
        public static int CopiedLevel(CharacterRecord record, string skillId)
        {
            if (record == null || string.IsNullOrEmpty(skillId) || record.MimicSkillId != skillId)
            {
                return 0;
            }

            return Math.Min(record.MimicSkillLevel, MimicryLevel(record));
        }

        /// <summary>The copied skill, if there is one and it can still be used.</summary>
        public static SkillDefinition Copied(CharacterRecord record)
        {
            var skill = SkillCatalog.Get(record?.MimicSkillId);
            return skill != null && CopiedLevel(record, skill.Id) > 0 ? skill : null;
        }

        /// <summary>Drops a copy that names an unknown or uncopyable skill (old or hand-edited saves).</summary>
        public static void Sanitize(CharacterRecord record)
        {
            var skill = SkillCatalog.Get(record.MimicSkillId);
            if (skill == null || !CanCopy(skill))
            {
                record.MimicSkillId = null;
                record.MimicSkillLevel = 0;
                return;
            }

            record.MimicSkillLevel = Math.Max(1, Math.Min(record.MimicSkillLevel, skill.MaxLevel));
        }
    }
}
