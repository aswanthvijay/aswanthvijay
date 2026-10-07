using System.Linq;
using NUnit.Framework;
using Runeheir.Characters;
using Runeheir.Items;
using Runeheir.Jobs;
using Runeheir.Skills;
using Runeheir.Stats;

namespace Runeheir.Tests
{
    /// <summary>Phase 7: the full roster, Ragnarok rebirth, the expanded jobs and Freyja's Kin.</summary>
    public sealed class RosterTests
    {
        private static CharacterRecord NewRecord(JobId job = JobId.Initiate, CharacterRace race = CharacterRace.Human)
        {
            var record = CharacterFactory.Create(new CharacterCreateRequest { Name = "Roster Tester", Race = race }, 0, 0);
            if (race == CharacterRace.Human)
            {
                record.Job = job;
            }

            return record;
        }

        private static void Learn(CharacterRecord record, string skillId, int level)
        {
            record.Skills.RemoveAll(s => s.Id == skillId);
            record.Skills.Add(new LearnedSkill(skillId, level));
        }

        /// <summary>A second job at Base 99 / Job 50 with the fee in hand.</summary>
        private static CharacterRecord ReadyForRebirth(JobId secondJob)
        {
            var record = NewRecord(secondJob);
            record.BaseLevel = RebirthRules.NormalBaseLevelCap;
            record.JobLevel = RebirthRules.MinJobLevel;
            record.Zeny = RebirthRules.Fee + 1000;
            new CharacterProgression(record).RecalculateStatPoints();
            return record;
        }

        // ------------------------------------------------------------ the tree
        [Test]
        public void Roster_HasEveryRagnarokClass()
        {
            var jobs = JobDatabase.All.ToList();
            Assert.AreEqual(1 + 6 + 13 + 13 + 7, jobs.Count, "Initiate, 6 first, 13 second, 13 transcendent, 7 expanded");
            Assert.AreEqual(6, jobs.Count(j => j.Family == JobFamily.Normal && j.Tier == 1));
            Assert.AreEqual(13, jobs.Count(j => j.Family == JobFamily.Normal && j.Tier == 2));
            Assert.AreEqual(13, jobs.Count(j => j.IsTranscendent));
            Assert.AreEqual(7, jobs.Count(j => j.IsExpanded));
            Assert.AreEqual(jobs.Count, jobs.Select(j => j.Name).Distinct().Count(), "names are unique");
            Assert.AreEqual(jobs.Count, jobs.Select(j => j.RoName).Distinct().Count(), "one job per Ragnarok class");

            foreach (var second in jobs.Where(j => j.Family == JobFamily.Normal && j.Tier == 2))
            {
                Assert.AreEqual(1, JobDatabase.Get(second.Parent).Tier, $"{second.Name} follows a first job");
                var transcendent = JobDatabase.TranscendentOf(second.Id);
                Assert.IsNotNull(transcendent, $"{second.Name} has a transcendent job");
                Assert.AreEqual(second.Line, transcendent.Line);
                Assert.AreEqual(120, transcendent.MaxJobLevel);
            }

            Assert.AreEqual(JobId.Huntsman, JobDatabase.Get(JobId.Ranger).Parent, "Hunters follow Archers");
            Assert.AreEqual(JobId.Warrior, JobDatabase.Get(JobId.Guardian).Parent, "Crusaders follow Swordmen");
            CollectionAssert.AreEquivalent(new[] { JobId.Ranger, JobId.Skald, JobId.Seidkona },
                JobDatabase.SecondJobsOf(JobId.Huntsman).Select(j => j.Id));
            CollectionAssert.AreEquivalent(new[] { JobId.Runesmith, JobId.Brewmaster }, JobDatabase.SecondJobsOf(JobId.Trader).Select(j => j.Id));
        }

        [Test]
        public void TryParse_AcceptsNorseAndRagnarokNames()
        {
            Assert.IsTrue(JobDatabase.TryParse("lord knight", out var id));
            Assert.AreEqual(JobId.Einherjar, id);
            Assert.IsTrue(JobDatabase.TryParse("Paladin", out id));
            Assert.AreEqual(JobId.Valkyrie, id, "Paladin is Ragnarok's name for the Valkyrie");
            Assert.IsTrue(JobDatabase.TryParse("Völva", out id));
            Assert.AreEqual(JobId.Volva, id);
            Assert.IsTrue(JobDatabase.TryParse("gypsy", out id));
            Assert.AreEqual(JobId.Volva, id);
            Assert.IsTrue(JobDatabase.TryParse("Super Novice", out id));
            Assert.AreEqual(JobId.Wanderer, id);
            Assert.IsTrue(JobDatabase.TryParse("glima fighter", out id));
            Assert.AreEqual(JobId.GlimaFighter, id);
            Assert.IsTrue(JobDatabase.TryParse("High Gothi", out id));
            Assert.AreEqual(JobId.HighGothi, id);
            Assert.IsFalse(JobDatabase.TryParse("Dragon Knight", out _));
        }

        // ------------------------------------------------------------ the normal path
        [Test]
        public void JobChange_FollowsTheNormalPath()
        {
            var record = NewRecord();
            var progression = new CharacterProgression(record);
            record.JobLevel = 10;
            Learn(record, SkillBook.BasicTrainingId, SkillBook.BasicTrainingForJobChange);
            Assert.IsTrue(progression.TryChangeJob(JobId.Huntsman, out string reason), reason);

            record.JobLevel = 39;
            Assert.IsFalse(progression.TryChangeJob(JobId.Skald, out reason));
            StringAssert.Contains("Job Level 40", reason);
            record.JobLevel = 40;
            Assert.IsFalse(progression.TryChangeJob(JobId.Guardian, out _), "not a Huntsman job");
            Assert.IsTrue(progression.TryChangeJob(JobId.Skald, out reason), reason);

            record.JobLevel = 70;
            Assert.IsFalse(progression.TryChangeJob(JobId.Thul, out reason), "transcendent jobs need rebirth");
            StringAssert.Contains("Urðr's Well", reason);
        }

        [Test]
        public void BaseLevel_StopsAt99_UntilRebirth()
        {
            var record = NewRecord(JobId.Berserker);
            var progression = new CharacterProgression(record);
            progression.GainExperience(long.MaxValue / 4, 0);
            Assert.AreEqual(99, record.BaseLevel);
            Assert.IsTrue(progression.IsMaxBaseLevel);
        }

        // ------------------------------------------------------------ rebirth
        [Test]
        public void Rebirth_NeedsASecondJobAtBase99Job50AndTheFee()
        {
            Assert.IsFalse(RebirthRules.CanRebirth(NewRecord(JobId.Warrior), out _), "first jobs can't");
            var record = ReadyForRebirth(JobId.Berserker);
            Assert.IsTrue(RebirthRules.CanRebirth(record, out string reason), reason);

            record.JobLevel = 49;
            Assert.IsFalse(RebirthRules.CanRebirth(record, out reason));
            StringAssert.Contains("Job Level 50", reason);
            record.JobLevel = 50;
            record.BaseLevel = 98;
            Assert.IsFalse(RebirthRules.CanRebirth(record, out reason));
            StringAssert.Contains("Base Level 99", reason);
            record.BaseLevel = 99;
            record.Zeny = RebirthRules.Fee - 1;
            Assert.IsFalse(RebirthRules.CanRebirth(record, out reason));
            StringAssert.Contains("zeny", reason);

            var wanderer = ReadyForRebirth(JobId.Wanderer);
            Assert.IsFalse(RebirthRules.CanRebirth(wanderer, out reason), "expanded jobs never rebirth");
        }

        [Test]
        public void Rebirth_StartsOverAsAHighInitiate()
        {
            var record = ReadyForRebirth(JobId.Berserker);
            record.Stats.SetAll(60);
            Learn(record, "bash", 10);
            record.SkillPoints = 3;
            var claymore = ItemStack.NewInstance(ItemCatalog.Get("iron_claymore"));
            record.Equipment[(int)EquipPosition.Weapon] = claymore;
            long zeny = record.Zeny;
            var progression = new CharacterProgression(record);

            Assert.IsTrue(progression.TryRebirth(out string message), message);
            Assert.AreEqual(JobId.Initiate, record.Job);
            Assert.IsTrue(record.Reborn);
            Assert.AreEqual(JobId.Berserker, record.RebirthPath);
            Assert.AreEqual("High Initiate", JobDatabase.NameFor(record));
            Assert.AreEqual(1, record.BaseLevel);
            Assert.AreEqual(1, record.JobLevel);
            Assert.AreEqual(zeny - RebirthRules.Fee, record.Zeny);
            Assert.AreEqual(1, record.Stats[StatType.Str]);
            Assert.AreEqual(100, record.StatPoints, "Ragnarok's High Novice starts with 100 status points");
            Assert.AreEqual(0, record.SkillPoints, "skill points are earned again with the new job levels");
            Assert.AreEqual(0, SkillBook.LevelIn(record, "bash"));
            Assert.AreEqual(1, SkillBook.LevelIn(record, SkillCatalog.FirstAid), "granted skills stay");
            Assert.IsNull(record.Equipment[(int)EquipPosition.Weapon], "worn gear goes back to the bag");
            Assert.IsTrue(record.Inventory.Contains(claymore));
            StringAssert.Contains("Einherjar", message);
            Assert.AreEqual(25f, RebirthRules.Modifiers(record).MaxHpPercent);
            Assert.AreEqual(25f, RebirthRules.Modifiers(record).MaxSpPercent);
            Assert.AreEqual(255, RebirthRules.BaseLevelCap(record));

            Assert.IsFalse(RebirthRules.CanRebirth(record, out _), "only once");
        }

        [Test]
        public void Reborn_RetraceTheirFirstLifeIntoItsTranscendentJob()
        {
            var record = ReadyForRebirth(JobId.Skald);
            var progression = new CharacterProgression(record);
            Assert.IsTrue(progression.TryRebirth(out _));

            record.JobLevel = 10;
            Learn(record, SkillBook.BasicTrainingId, SkillBook.BasicTrainingForJobChange);
            CollectionAssert.AreEqual(new[] { JobId.Huntsman }, JobDatabase.NextJobs(record).Select(j => j.Id));
            Assert.IsFalse(progression.TryChangeJob(JobId.Warrior, out string reason));
            StringAssert.Contains("Skald", reason);
            Assert.IsFalse(progression.TryChangeJob(JobId.Thunderer, out _), "no expanded jobs after rebirth");
            Assert.IsTrue(progression.TryChangeJob(JobId.Huntsman, out reason), reason);
            Assert.AreEqual("High Huntsman", JobDatabase.NameFor(record));

            record.JobLevel = 40;
            Assert.IsFalse(progression.TryChangeJob(JobId.Skald, out _), "reborn characters skip the second jobs");
            Assert.IsFalse(progression.TryChangeJob(JobId.Volva, out _), "a Skald's path leads to the Thul");
            Assert.IsTrue(progression.TryChangeJob(JobId.Thul, out reason), reason);
            Assert.AreEqual(120, JobDatabase.MaxJobLevel(record.Job));
            Assert.IsTrue(JobDatabase.IsSelfOrAncestor(JobId.Skald, JobId.Thul), "a Thul keeps the Skald's songs");
        }

        // ------------------------------------------------------------ expanded jobs
        [Test]
        public void ExpandedJobs_FollowTheirOwnUnlocks()
        {
            var record = NewRecord();
            var progression = new CharacterProgression(record);
            record.JobLevel = 10;
            Learn(record, SkillBook.BasicTrainingId, SkillBook.BasicTrainingForJobChange);
            record.BaseLevel = 44;
            Assert.IsFalse(progression.TryChangeJob(JobId.Wanderer, out string reason));
            StringAssert.Contains("Base Level 45", reason);
            record.BaseLevel = 45;
            Assert.IsTrue(progression.TryChangeJob(JobId.Wanderer, out reason), reason);
            Assert.IsTrue(JobDatabase.IsSelfOrAncestor(JobId.Warrior, JobId.Wanderer), "Wanderers learn every first job's skills");
            Assert.IsTrue(JobDatabase.IsSelfOrAncestor(JobId.Mystic, JobId.Wanderer));
            Assert.IsFalse(JobDatabase.IsSelfOrAncestor(JobId.Berserker, JobId.Wanderer), "but no second job's");
            Assert.AreEqual(255, RebirthRules.BaseLevelCap(record));

            var glima = NewRecord(JobId.GlimaFighter);
            glima.JobLevel = 40;
            CollectionAssert.AreEquivalent(new[] { JobId.SolGuardian, JobId.FylgjaCaller }, JobDatabase.NextJobs(glima).Select(j => j.Id));
            Assert.IsTrue(JobDatabase.CanChangeJob(glima, JobId.SolGuardian, out reason), reason);

            Assert.IsFalse(JobDatabase.CanChangeJob(NewRecord(), JobId.FreyjasKin, out reason));
            StringAssert.Contains("chosen when a character is made", reason);
        }

        [Test]
        public void FreyjasKin_AreDoramFromCreation()
        {
            var record = NewRecord(race: CharacterRace.Doram);
            Assert.AreEqual(JobId.FreyjasKin, record.Job);
            Assert.AreEqual(CharacterRace.Doram, record.Race);
            Assert.AreEqual("bygul_staff", record.Equipment[(int)EquipPosition.Weapon]?.ItemId, "the gift staff is in hand");
            Assert.AreEqual(1, SkillBook.LevelIn(record, SkillCatalog.FirstAid));
            Assert.IsTrue(SkillCatalog.CanUse(record.Job, SkillCatalog.FirstAid), "granted skills belong to everyone");
            Assert.AreEqual(0, JobDatabase.NextJobs(record).Count, "Freyja's Kin never change job");

            // A hand-edited save can't make a Doram something else, or a human one of Freyja's Kin.
            record.Job = JobId.Warrior;
            record.Sanitize();
            Assert.AreEqual(CharacterRace.Human, record.Race);
        }

        // ------------------------------------------------------------ old saves
        [Test]
        public void OldSaves_AscendedBecomeReborn_AndMovedSkillsAreRefunded()
        {
            // Phase 2–6 Ascended characters were never "reborn": they are now, along their own path.
            var einherjar = NewRecord(JobId.Einherjar);
            einherjar.Reborn = false;
            einherjar.Sanitize();
            Assert.IsTrue(einherjar.Reborn);
            Assert.AreEqual(JobId.Berserker, einherjar.RebirthPath);
            Assert.AreEqual(255, RebirthRules.BaseLevelCap(einherjar));

            // The old Devotee "Paladin" (26) is the Gothi now.
            Assert.AreEqual("Gothi", JobDatabase.Get((JobId)26).Name);
            Assert.AreEqual("High Gothi", JobDatabase.Get((JobId)36).Name);

            // A Ranger is a Huntsman's job now: Scout skills it learned go back into the pool.
            var ranger = NewRecord(JobId.Ranger);
            Learn(ranger, "twin_fang", 5);
            ranger.SkillPoints = 2;
            ranger.Sanitize();
            Assert.AreEqual(0, SkillBook.LevelIn(ranger, "twin_fang"));
            Assert.AreEqual(7, ranger.SkillPoints);

            // A record claiming a rebirth no rebirth can produce is repaired.
            var odd = NewRecord(JobId.Berserker);
            odd.Reborn = true;
            odd.RebirthPath = JobId.Berserker;
            odd.Sanitize();
            Assert.IsFalse(odd.Reborn, "reborn characters never hold a second job");
        }

        [Test]
        public void EveryJobCanWearSomething_AndGiftWeaponsFitTheirJob()
        {
            foreach (var job in JobDatabase.All)
            {
                Assert.AreNotEqual(Combat.WeaponMask.None, job.AllowedWeapons, job.Name);
                Assert.IsTrue(Combat.WeaponMasks.Allows(job.AllowedWeapons, Combat.WeaponType.Unarmed), $"{job.Name} can always fight bare-handed");
                if (job.StarterWeaponId != null)
                {
                    var weapon = ItemCatalog.Get(job.StarterWeaponId);
                    Assert.IsNotNull(weapon, $"{job.Name}'s gift {job.StarterWeaponId}");
                    Assert.IsTrue(Combat.WeaponMasks.Allows(job.AllowedWeapons, weapon.WeaponType), $"{job.Name} can wield its gift");
                    Assert.LessOrEqual(weapon.MinTier, job.GearTier, $"{job.Name}'s gift isn't above its gear tier");
                }
            }
        }
    }
}
