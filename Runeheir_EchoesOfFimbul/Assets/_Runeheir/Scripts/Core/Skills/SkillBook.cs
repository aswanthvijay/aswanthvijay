using System;
using System.Collections.Generic;
using Runeheir.Characters;
using Runeheir.Combat;
using Runeheir.Jobs;
using Runeheir.Stats;

namespace Runeheir.Skills
{
    /// <summary>
    /// Ragnarok-style skill tree rules over a <see cref="CharacterRecord"/>: one skill point per job level,
    /// spent one level at a time on skills of your job line whose prerequisites are met. Points carry over
    /// between jobs. Passive skills feed <see cref="PassiveModifiers"/> into the stat engine.
    /// </summary>
    public sealed class SkillBook
    {
        /// <summary>Save format of <see cref="CharacterRecord.Skills"/> (2: Phase 7 roster).</summary>
        public const int CurrentDataVersion = 2;

        /// <summary>Initiates need Basic Training at this level before their first job change (Ragnarok Basic Skill 9).</summary>
        public const string BasicTrainingId = "basic_training";

        public const int BasicTrainingForJobChange = 9;

        private readonly CharacterRecord _record;

        public SkillBook(CharacterRecord record)
        {
            _record = record ?? throw new ArgumentNullException(nameof(record));
            SanitizeSkills(record);
        }

        /// <summary>A level was learned, reset or set.</summary>
        public event Action Changed;

        public IReadOnlyList<LearnedSkill> Learned => _record.Skills;

        public int Points => _record.SkillPoints;

        public int GetLevel(string id)
        {
            return LevelIn(_record, id);
        }

        /// <summary>The learned level of <paramref name="id"/>, or 0 if it isn't learned or the current job can't use it.</summary>
        public int UsableLevel(string id)
        {
            return SkillCatalog.CanUse(_record.Job, id) ? GetLevel(id) : 0;
        }

        public static int LevelIn(CharacterRecord record, string id)
        {
            if (record?.Skills == null || string.IsNullOrEmpty(id))
            {
                return 0;
            }

            foreach (var skill in record.Skills)
            {
                if (skill != null && skill.Id == id)
                {
                    return skill.Level;
                }
            }

            return 0;
        }

        public bool CanLearn(string id, out string reason)
        {
            var skill = SkillCatalog.Get(id);
            if (skill == null || skill.Hidden)
            {
                reason = "Unknown skill.";
                return false;
            }

            if (!JobDatabase.IsSelfOrAncestor(skill.Job, _record.Job))
            {
                reason = $"{skill.Name} belongs to the {JobDatabase.Get(skill.Job).Name} line.";
                return false;
            }

            int level = GetLevel(id);
            if (level >= skill.MaxLevel)
            {
                reason = $"{skill.Name} is already at its maximum level ({skill.MaxLevel}).";
                return false;
            }

            if (_record.SkillPoints <= 0)
            {
                reason = "No skill points left. You earn one per Job Level.";
                return false;
            }

            foreach (var requirement in skill.Requires)
            {
                if (GetLevel(requirement.SkillId) < requirement.Level)
                {
                    var needed = SkillCatalog.Get(requirement.SkillId);
                    reason = $"Requires {needed?.Name ?? requirement.SkillId} Lv {requirement.Level}.";
                    return false;
                }
            }

            reason = null;
            return true;
        }

        public bool TryLearn(string id, out string reason)
        {
            if (!CanLearn(id, out reason))
            {
                return false;
            }

            SetLevelInternal(id, GetLevel(id) + 1);
            _record.SkillPoints--;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Skill reset: every learned level goes back into the pool (granted skills stay at Lv 1). Returns points refunded.</summary>
        public int ResetAll()
        {
            int refunded = 0;
            for (int i = _record.Skills.Count - 1; i >= 0; i--)
            {
                var learned = _record.Skills[i];
                var skill = SkillCatalog.Get(learned.Id);
                int keep = skill != null && skill.Granted ? 1 : 0;
                refunded += Math.Max(0, learned.Level - keep);
                if (keep > 0)
                {
                    learned.Level = keep;
                }
                else
                {
                    _record.Skills.RemoveAt(i);
                }
            }

            _record.SkillPoints += refunded;
            if (refunded > 0)
            {
                Changed?.Invoke();
            }

            return refunded;
        }

        /// <summary>The record's skills changed outside the book (rebirth): tell the windows and the stats.</summary>
        public void NotifyChanged()
        {
            Changed?.Invoke();
        }

        /// <summary>GM/debug: sets a level directly (0 forgets it). Skill points are not touched.</summary>
        public void SetLevel(string id, int level)
        {
            var skill = SkillCatalog.Get(id);
            if (skill == null)
            {
                return;
            }

            SetLevelInternal(id, Math.Max(skill.Granted ? 1 : 0, Math.Min(skill.MaxLevel, level)));
            Changed?.Invoke();
        }

        /// <summary>GM/debug: sets the unspent skill points.</summary>
        public void SetPoints(int points)
        {
            _record.SkillPoints = Math.Max(0, points);
            Changed?.Invoke();
        }

        /// <summary>GM/debug: every skill of the current job line at max level, for free.</summary>
        public void LearnEverything()
        {
            foreach (var skill in SkillCatalog.ForJob(_record.Job))
            {
                SetLevelInternal(skill.Id, skill.MaxLevel);
            }

            Changed?.Invoke();
        }

        /// <summary>Bonuses from learned passive skills that the current job and weapon allow.</summary>
        public StatModifiers PassiveModifiers(WeaponType weapon)
        {
            var total = StatModifiers.Empty();
            foreach (var learned in _record.Skills)
            {
                var skill = SkillCatalog.Get(learned.Id);
                if (skill == null || !skill.Passive || skill.PassivePerLevel == null || learned.Level <= 0)
                {
                    continue;
                }

                if (!JobDatabase.IsSelfOrAncestor(skill.Job, _record.Job) || !WeaponMasks.Allows(skill.PassiveWeapons, weapon))
                {
                    continue;
                }

                total.AddScaled(skill.PassivePerLevel, learned.Level);
            }

            return total;
        }

        /// <summary>Learned passives with an on-hit proc the current job and weapon allow, with their levels.</summary>
        public List<KeyValuePair<SkillDefinition, int>> PassiveProcs(WeaponType weapon)
        {
            var procs = new List<KeyValuePair<SkillDefinition, int>>();
            foreach (var learned in _record.Skills)
            {
                var skill = SkillCatalog.Get(learned.Id);
                if (skill?.Proc != null && skill.Passive && learned.Level > 0
                    && JobDatabase.IsSelfOrAncestor(skill.Job, _record.Job)
                    && WeaponMasks.Allows(skill.PassiveWeapons, weapon))
                {
                    procs.Add(new KeyValuePair<SkillDefinition, int>(skill, learned.Level));
                }
            }

            return procs;
        }

        /// <summary>
        /// Repairs the skill list (unknown ids, bad levels, duplicates), adds granted skills, and migrates
        /// saves from before skill levels existed. Skills the character's job can't use (the roster moved them to
        /// another job, Phase 7) are forgotten and their points refunded. Called by <see cref="CharacterRecord.Sanitize"/>.
        /// </summary>
        public static void SanitizeSkills(CharacterRecord record)
        {
            var skills = record.Skills ?? new List<LearnedSkill>();
            var seen = new HashSet<string>();
            int refunded = 0;
            bool knownJob = JobDatabase.Exists(record.Job);
            for (int i = 0; i < skills.Count; i++)
            {
                var learned = skills[i];
                var skill = learned != null ? SkillCatalog.Get(learned.Id) : null;
                if (skill == null || skill.Hidden || learned.Level <= 0 || !seen.Add(learned.Id))
                {
                    skills.RemoveAt(i--);
                    continue;
                }

                learned.Level = Math.Min(learned.Level, skill.MaxLevel);
                if (knownJob && !skill.Granted && !JobDatabase.IsSelfOrAncestor(skill.Job, record.Job))
                {
                    refunded += learned.Level;
                    seen.Remove(learned.Id);
                    skills.RemoveAt(i--);
                }
            }

            record.SkillPoints = Math.Max(0, record.SkillPoints) + refunded;

            foreach (var skill in SkillCatalog.All)
            {
                if (skill.Granted && seen.Add(skill.Id))
                {
                    skills.Add(new LearnedSkill(skill.Id, 1));
                }
            }

            record.Skills = skills;
            record.SkillDataVersion = CurrentDataVersion;
        }

        private void SetLevelInternal(string id, int level)
        {
            for (int i = 0; i < _record.Skills.Count; i++)
            {
                if (_record.Skills[i].Id == id)
                {
                    if (level <= 0)
                    {
                        _record.Skills.RemoveAt(i);
                    }
                    else
                    {
                        _record.Skills[i].Level = level;
                    }

                    return;
                }
            }

            if (level > 0)
            {
                _record.Skills.Add(new LearnedSkill(id, level));
            }
        }
    }
}
