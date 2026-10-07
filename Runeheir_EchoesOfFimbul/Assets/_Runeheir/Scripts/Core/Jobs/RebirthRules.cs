using Runeheir.Characters;
using Runeheir.Items;
using Runeheir.Skills;
using Runeheir.Stats;

namespace Runeheir.Jobs
{
    /// <summary>
    /// Ragnarok rebirth (Phase 7), performed by the Norns at Urðr's Well in Vigrid Haven. A second job at Base Level 99
    /// (Job Level 50+) pays the fee and starts over as a High Initiate at Base 1 / Job 1, with 100 starting status
    /// points instead of 48, a fresh skill tree and +25% Max HP and SP for good. Its path is remembered: a reborn
    /// Berserker becomes High Initiate → High Warrior → Einherjar, and nothing else. Reborn characters level to
    /// Base 255 / Job 120; everyone else stops at Base 99. Expanded jobs never rebirth and level to 255 directly.
    /// </summary>
    public static class RebirthRules
    {
        /// <summary>Base level cap before rebirth (Ragnarok's 99).</summary>
        public const int NormalBaseLevelCap = 99;

        /// <summary>Job level the second job needs before rebirth.</summary>
        public const int MinJobLevel = 50;

        /// <summary>The Norns' fee (Ragnarok's 1,285,000 zeny).</summary>
        public const long Fee = 1_285_000;

        /// <summary>Extra starting status points for reborn characters (100 instead of 48).</summary>
        public const int BonusStatPoints = 52;

        /// <summary>Reborn characters' lasting bonus to Max HP and Max SP.</summary>
        public const float HpSpBonusPercent = 25f;

        /// <summary>How high this character's base level can go: 255 when reborn or on an expanded job, else 99.</summary>
        public static int BaseLevelCap(CharacterRecord record)
        {
            if (record == null)
            {
                return StatFormulas.MaxBaseLevel;
            }

            var job = JobDatabase.Exists(record.Job) ? JobDatabase.Get(record.Job) : null;
            return record.Reborn || (job != null && job.IsExpanded) ? StatFormulas.MaxBaseLevel : NormalBaseLevelCap;
        }

        /// <summary>Starting status points (Base Level 1) for this character.</summary>
        public static int StartingStatPoints(CharacterRecord record)
        {
            return StatFormulas.StartingStatPoints + (record != null && record.Reborn ? BonusStatPoints : 0);
        }

        /// <summary>The reborn +25% Max HP / Max SP as stat modifiers (empty for everyone else).</summary>
        public static StatModifiers Modifiers(CharacterRecord record)
        {
            var modifiers = StatModifiers.Empty();
            if (record != null && record.Reborn)
            {
                modifiers.MaxHpPercent = HpSpBonusPercent;
                modifiers.MaxSpPercent = HpSpBonusPercent;
            }

            return modifiers;
        }

        public static bool CanRebirth(CharacterRecord record, out string reason)
        {
            if (record == null || !JobDatabase.Exists(record.Job))
            {
                reason = "No character.";
                return false;
            }

            var job = JobDatabase.Get(record.Job);
            if (record.Reborn)
            {
                reason = "You have already been reborn once. The Norns weave each thread only twice.";
                return false;
            }

            if (job.IsExpanded)
            {
                reason = $"{job.Name}s walk their own path: they are never reborn.";
                return false;
            }

            if (job.Family != JobFamily.Normal || job.Tier != 2)
            {
                reason = "Only second jobs can be reborn (Berserker, Gothi, Skald...).";
                return false;
            }

            if (record.BaseLevel < NormalBaseLevelCap)
            {
                reason = $"Rebirth needs Base Level {NormalBaseLevelCap}.";
                return false;
            }

            if (record.JobLevel < MinJobLevel)
            {
                reason = $"Rebirth needs Job Level {MinJobLevel}.";
                return false;
            }

            if (record.Zeny < Fee)
            {
                reason = $"The Norns ask {Fee:N0} zeny for a new thread.";
                return false;
            }

            reason = null;
            return true;
        }

        /// <summary>
        /// Rebirth: pays the fee, remembers the second job, and turns the character into a High Initiate at Base 1 /
        /// Job 1 with fresh stats (100 points to spend) and skills (granted skills stay). Worn gear goes back to the bag;
        /// items, zeny, storage and the Pushcart are kept.
        /// </summary>
        public static bool TryRebirth(CharacterRecord record, out string reason)
        {
            if (!CanRebirth(record, out reason))
            {
                return false;
            }

            var previous = JobDatabase.Get(record.Job);
            record.Zeny -= Fee;
            record.RebirthPath = previous.Id;
            record.Reborn = true;
            record.Job = JobId.Initiate;
            record.BaseLevel = 1;
            record.JobLevel = 1;
            record.BaseExp = 0;
            record.JobExp = 0;
            record.Stats.SetAll(StatFormulas.MinStat);
            record.StatPoints = StartingStatPoints(record);

            // A fresh thread: every learned skill is forgotten and the points are earned again with the new job
            // levels (more of them: transcendent jobs reach Job Level 120).
            record.Skills.Clear();
            record.SkillPoints = 0;
            SkillBook.SanitizeSkills(record);

            UnequipAll(record);
            record.Hp = -1;
            record.Sp = -1;

            var next = JobDatabase.TranscendentOf(previous.Id);
            reason = $"Reborn as a High Initiate. Your path: High {JobDatabase.Get(previous.Line).Name}, then {next?.Name ?? "?"}.";
            return true;
        }

        /// <summary>
        /// Save repairs for the roster (Phase 7): characters already on a transcendent job (the old Ascended tier) count as
        /// reborn along their own path; a reborn record with a broken path is mended where it can be.
        /// </summary>
        public static void Sanitize(CharacterRecord record)
        {
            var job = JobDatabase.Get(record.Job);
            if (job.IsTranscendent)
            {
                record.Reborn = true;
                record.RebirthPath = job.Parent;
                return;
            }

            if (!record.Reborn)
            {
                record.RebirthPath = JobId.Initiate;
                return;
            }

            bool validPath = JobDatabase.Exists(record.RebirthPath) && JobDatabase.TranscendentOf(record.RebirthPath) != null;
            if (job.IsExpanded || job.Tier == 2 || !validPath || (job.Tier == 1 && JobDatabase.Get(record.RebirthPath).Line != job.Id))
            {
                // Not a state rebirth can produce (hand-edited or an older build): treat as never reborn.
                record.Reborn = false;
                record.RebirthPath = JobId.Initiate;
            }
        }

        private static void UnequipAll(CharacterRecord record)
        {
            var equipment = EquipmentSet.Normalize(record.Equipment);
            for (int i = 0; i < equipment.Length; i++)
            {
                var worn = equipment[i];
                if (worn == null)
                {
                    continue;
                }

                // Two-handed pieces sit in two positions as the same stack: move it once.
                if (!record.Inventory.Contains(worn))
                {
                    record.Inventory.Add(worn);
                }

                equipment[i] = null;
            }

            record.Equipment = equipment;
        }
    }
}
