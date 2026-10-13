using System.Linq;
using NUnit.Framework;
using Runeheir.Characters;
using Runeheir.Combat;
using Runeheir.Items;
using Runeheir.Mercenaries;
using Runeheir.Monsters;
using Runeheir.World;

namespace Runeheir.Tests
{
    /// <summary>Phase 8: Ragnarok's mercenaries (mercenary_db.yml) as the Hirð's hired swords.</summary>
    public sealed class MercenaryTests
    {
        private static CharacterRecord Hirer(int baseLevel)
        {
            var record = CharacterFactory.Create(new CharacterCreateRequest { Name = "Hirer" }, 0, 0);
            record.BaseLevel = baseLevel;
            return record;
        }

        private static MercenaryDefinition Hird(string role, int grade)
        {
            return MercenaryCatalog.All.Single(m => m.Role == role && m.Grade == grade);
        }

        [Test]
        public void Hird_HasTenGradesOfEachRole_GettingStronger()
        {
            foreach (var role in new[] { "Bowman", "Spearman", "Huscarl" })
            {
                var grades = MercenaryCatalog.All.Where(m => m.Role == role).OrderBy(m => m.Grade).ToList();
                CollectionAssert.AreEqual(Enumerable.Range(1, 10), grades.Select(m => m.Grade), role);
                for (int i = 1; i < grades.Count; i++)
                {
                    Assert.GreaterOrEqual(grades[i].Monster.Level, grades[i - 1].Monster.Level, $"{role} {i + 1}");
                    Assert.GreaterOrEqual(grades[i].MinBaseLevel, grades[i - 1].MinBaseLevel, $"{role} {i + 1}");
                    Assert.Greater(grades[i].Monster.MaxHp, grades[i - 1].Monster.MaxHp / 2, $"{role} {i + 1}");
                }

                Assert.AreEqual(MonsterShape.Humanoid, grades[0].Monster.Shape);
            }

            Assert.AreEqual(WeaponType.Bow, Hird("Bowman", 1).Monster.LookWeapon);
            Assert.IsTrue(Hird("Bowman", 1).Monster.IsRanged, "bowmen shoot from range");
            Assert.AreEqual(WeaponType.Spear, Hird("Spearman", 1).Monster.LookWeapon);
        }

        [Test]
        public void Mercenaries_AreNotWildMonsters_AndUseOnlyAllySafeSkills()
        {
            foreach (var mercenary in MercenaryCatalog.All)
            {
                var monster = mercenary.Monster;
                Assert.IsNull(MonsterCatalog.Get(monster.Id), $"{monster.Name} never spawns in the wild");
                Assert.IsTrue(monster.SummonOnly, monster.Name);
                Assert.LessOrEqual(mercenary.MinBaseLevel, monster.Level, monster.Name);
                Assert.AreEqual(monster.Skills.Count, monster.Skills.Select(s => s.Id).Distinct().Count(), $"{monster.Name}: skill ids");
                foreach (var skill in monster.Skills)
                {
                    Assert.AreNotEqual(MonsterSkillKind.Summon, skill.Kind, monster.Name);
                    Assert.AreNotEqual(MonsterSkillKind.Teleport, skill.Kind, monster.Name);
                    if (skill.Kind == MonsterSkillKind.Buff)
                    {
                        Assert.IsNotNull(BuffCatalog.Get(skill.BuffId), $"{monster.Name}: {skill.Name} buff {skill.BuffId}");
                    }
                }
            }
        }

        [Test]
        public void Scrolls_HireTheirMercenary_AndTheHirdHallSellsTheThirty()
        {
            var scrolls = ItemCatalog.All.Where(i => i.Special == ItemSpecialEffect.HireMercenary).ToList();
            Assert.GreaterOrEqual(scrolls.Count, 30);
            foreach (var scroll in scrolls)
            {
                Assert.IsNotNull(MercenaryCatalog.Get(scroll.MercenaryId), scroll.Name);
                Assert.IsFalse(scroll.NotUsableYet, scroll.Name);
            }

            var shop = ShopCatalog.Get(ShopCatalog.HirdHall);
            Assert.AreEqual(30, shop.ItemIds.Length);
            var first = ItemCatalog.Get(shop.ItemIds[0]);
            Assert.AreEqual(Hird("Bowman", 1).Id, first.MercenaryId, "the shelf starts with the first bowman");
            Assert.Less(first.Price, ItemCatalog.Get(shop.ItemIds[9]).Price, "a grade 10 contract costs more than a grade 1");

            var vigrid = MapCatalog.Get(MapCatalog.VigridHaven);
            Assert.IsTrue(vigrid.Npcs.Any(n => n.ShopId == ShopCatalog.HirdHall), "Thrand keeps the Hirð Hall in Vigrid Haven");
        }

        [Test]
        public void Hire_NeedsTheLevel_AndOneContractAtATime()
        {
            var bowman = Hird("Bowman", 1);
            var huscarl = Hird("Huscarl", 1);
            var record = Hirer(bowman.MinBaseLevel - 1);
            Assert.IsFalse(MercenaryCatalog.Hire(record, bowman.Id, out _), "too green");
            Assert.IsNull(MercenaryCatalog.Active(record));

            record.BaseLevel = 255;
            Assert.IsTrue(MercenaryCatalog.Hire(record, bowman.Id, out _));
            Assert.AreEqual(bowman.Id, MercenaryCatalog.Active(record).MercenaryId);
            Assert.AreEqual(MercenaryCatalog.ContractSeconds, MercenaryCatalog.Active(record).SecondsLeft);

            Assert.IsTrue(MercenaryCatalog.Hire(record, huscarl.Id, out string message));
            StringAssert.Contains("contract ends", message);
            Assert.AreEqual(huscarl.Id, MercenaryCatalog.Active(record).MercenaryId, "the new hire replaces the old");
        }

        [Test]
        public void Contract_RunsOut()
        {
            var record = Hirer(255);
            MercenaryCatalog.Hire(record, Hird("Spearman", 3).Id, out _);
            Assert.IsFalse(MercenaryCatalog.Tick(record, MercenaryCatalog.ContractSeconds - 1f));
            Assert.AreEqual("0:01", MercenaryCatalog.TimeLeftText(record.Mercenary));
            Assert.IsTrue(MercenaryCatalog.Tick(record, 2f), "time's up");
            Assert.IsNull(MercenaryCatalog.Active(record));
            Assert.IsFalse(MercenaryCatalog.Tick(record, 1f), "nothing left to end");
        }

        [Test]
        public void Contract_SurvivesTheSaveCopy_AndSanitize()
        {
            var record = Hirer(255);
            MercenaryCatalog.Hire(record, Hird("Huscarl", 5).Id, out _);
            record.Mercenary.Hp = 1234;
            record.Mercenary.Kills = 7;
            record.Mercenary.SecondsLeft = 99999f;

            var copy = record.Clone();
            copy.Sanitize();
            Assert.AreEqual(Hird("Huscarl", 5).Id, copy.Mercenary.MercenaryId);
            Assert.AreEqual(1234, copy.Mercenary.Hp);
            Assert.AreEqual(7, copy.Mercenary.Kills);
            Assert.AreEqual(MercenaryCatalog.ContractSeconds, copy.Mercenary.SecondsLeft, "a contract never lasts longer than 30 minutes");
            Assert.AreNotSame(record.Mercenary, copy.Mercenary);

            copy.Mercenary.MercenaryId = "nobody";
            copy.Sanitize();
            Assert.IsNull(copy.Mercenary, "an unknown mercenary's contract is dropped");
        }

        [Test]
        public void MendingSkills_LookAfterTheMaster()
        {
            var mend = new MonsterSkill { Id = "mend", Kind = MonsterSkillKind.Heal, HealPercent = 10f, BelowHpPercent = 60f };
            var healthy = new MonsterSkillContext { HpPercent = 100f, AllyHpPercent = 100f };
            var masterHurt = new MonsterSkillContext { HpPercent = 100f, AllyHpPercent = 40f };
            var wild = new MonsterSkillContext { HpPercent = 100f };
            Assert.IsFalse(MonsterSkillRules.IsUsable(mend, healthy));
            Assert.IsTrue(MonsterSkillRules.IsUsable(mend, masterHurt), "its master is hurt");
            Assert.IsFalse(MonsterSkillRules.IsUsable(mend, wild), "a wild monster only mends itself");
        }
    }
}
