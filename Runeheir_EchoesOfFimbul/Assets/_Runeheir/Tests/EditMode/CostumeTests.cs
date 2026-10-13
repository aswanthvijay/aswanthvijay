using System.Linq;
using NUnit.Framework;
using Runeheir.Characters;
using Runeheir.Items;
using Runeheir.Jobs;
using Runeheir.Monsters;
using Runeheir.Pets;
using Runeheir.World;

namespace Runeheir.Tests
{
    /// <summary>Phase 8.4: XileRetro's costumes and its own gear, cards and pets.</summary>
    public sealed class CostumeTests
    {
        private static CharacterRecord Hero()
        {
            var record = CharacterFactory.Create(new CharacterCreateRequest { Name = "Costume Tester" }, 0, 0);
            record.Job = JobId.Warrior;
            record.BaseLevel = 99;
            return record;
        }

        private static ItemDefinition Costume(EquipSlot slot)
        {
            return ItemCatalog.All.First(i => i.IsCostume && i.Slots == slot);
        }

        [Test]
        public void Catalog_HasHundredsOfCostumes_EachWithALook()
        {
            var costumes = ItemCatalog.All.Where(i => i.IsCostume).ToList();
            Assert.Greater(costumes.Count, 400);
            foreach (var costume in costumes)
            {
                Assert.IsFalse(string.IsNullOrEmpty(costume.LookAs), costume.Name);
                Assert.IsFalse(string.IsNullOrEmpty(costume.ViewColorHex), costume.Name);
                Assert.IsFalse(costume.Refinable, costume.Name);
                Assert.AreEqual(EquipKind.Costume, costume.EquipKind, costume.Name);
                Assert.AreEqual(ItemCategory.Costume, costume.Category, costume.Name);
            }

            Assert.AreEqual(costumes.Count, costumes.Select(c => c.Name).Distinct().Count(), "every costume has its own name");
            Assert.IsTrue(costumes.Any(c => c.LookAs.StartsWith("aura")));
            Assert.IsTrue(costumes.Any(c => c.LookAs.StartsWith("wings")));
            Assert.IsTrue(costumes.Any(c => c.LookAs.StartsWith("wig:")));
        }

        [Test]
        public void TheTailors_SellEveryCostume()
        {
            var sold = ShopCatalog.Get(ShopCatalog.TailorHead).ItemIds.Concat(ShopCatalog.Get(ShopCatalog.TailorBack).ItemIds).ToList();
            var costumes = ItemCatalog.All.Where(i => i.IsCostume && i.RoId != 0).Select(i => i.Id).ToList();
            CollectionAssert.AreEquivalent(costumes, sold);
            var vigrid = MapCatalog.Get(MapCatalog.VigridHaven);
            Assert.IsTrue(vigrid.Npcs.Any(n => n.ShopId == ShopCatalog.TailorHead));
            Assert.IsTrue(vigrid.Npcs.Any(n => n.ShopId == ShopCatalog.TailorBack));
        }

        [Test]
        public void Costumes_GoInTheCostumeSlots_OverTheGear_AndAddNoStats()
        {
            var record = Hero();
            var bag = new Inventory(record.Inventory);
            var set = new EquipmentSet(record, bag);
            var before = EquipmentStats.Compute(record);

            var hat = Costume(EquipSlot.CostumeUpper);
            Assert.AreEqual(EquipPosition.CostumeUpper, set.TargetPosition(hat));
            Assert.AreEqual(EquipPosition.CostumeMid, set.TargetPosition(Costume(EquipSlot.CostumeMid)));
            Assert.AreEqual(EquipPosition.CostumeLower, set.TargetPosition(Costume(EquipSlot.CostumeLower)));
            Assert.AreEqual(EquipPosition.CostumeGarment, set.TargetPosition(Costume(EquipSlot.CostumeGarment)));
            Assert.IsTrue(EquipmentSet.CanWear(record, hat, out string reason), reason);

            record.Equipment[(int)EquipPosition.CostumeUpper] = ItemStack.NewInstance(hat);
            var after = EquipmentStats.Compute(record);
            Assert.AreEqual(before.Modifiers.Atk, after.Modifiers.Atk);
            Assert.AreEqual(before.Modifiers.Def, after.Modifiers.Def);
            Assert.IsNull(set.Covering(EquipPosition.HeadUpper), "the costume doesn't take the real helm's place");
        }

        [Test]
        public void Costumes_SurviveTheSaveRepair()
        {
            var record = Hero();
            var wings = ItemCatalog.All.First(i => i.IsCostume && i.LookAs.StartsWith("wings") && i.Slots == EquipSlot.CostumeGarment);
            record.Equipment = new ItemStack[EquipmentSet.Positions];
            record.Equipment[(int)EquipPosition.CostumeGarment] = ItemStack.NewInstance(wings);
            EquipmentSet.SanitizeEquipment(record);
            Assert.AreEqual(wings.Id, record.Equipment[(int)EquipPosition.CostumeGarment]?.ItemId);

            // A costume saved in a gear slot goes back to the bag.
            record.Equipment[(int)EquipPosition.CostumeGarment] = null;
            record.Equipment[(int)EquipPosition.Garment] = ItemStack.NewInstance(wings);
            EquipmentSet.SanitizeEquipment(record);
            Assert.IsNull(record.Equipment[(int)EquipPosition.Garment]);
            Assert.IsTrue(new Inventory(record.Inventory).Has(wings.Id));
        }

        [Test]
        public void LookAlikes_ResolveToTheCostumeThatLooksTheSame()
        {
            int aliased = 0;
            foreach (var costume in ItemCatalog.All.Where(i => i.IsCostume).Take(50))
            {
                Assert.AreSame(costume, ItemCatalog.GetXile(costume.RoId));
                aliased++;
            }

            Assert.Greater(aliased, 0);
        }

        // ------------------------------------------------------------ XileRetro's own gear, cards and pets
        [Test]
        public void CustomGear_IsImported_WithItsBonusesAndANewName()
        {
            var custom = ItemCatalog.All.Where(i => i.RoId != 0 && i.IsEquipment && !i.IsCostume
                                                    && (i.Name.StartsWith("Godly ") || i.Name.StartsWith("Uber ") || i.Name.StartsWith("Elite "))).ToList();
            Assert.Greater(custom.Count, 50);
            Assert.Greater(custom.Count(i => i.Effect != null), custom.Count / 2, "most custom pieces keep their stats");
            Assert.IsTrue(custom.Any(i => i.IsWeapon));
        }

        [Test]
        public void CustomGearAndCards_DropFromMonsters()
        {
            var customIds = ItemCatalog.All.Where(i => i.RoId != 0 && (i.Name.StartsWith("Godly ") || i.Name.StartsWith("Uber "))).Select(i => i.Id).ToHashSet();
            var dropped = MonsterCatalog.All.SelectMany(m => m.Drops.Concat(m.MvpDrops)).Select(d => d.ItemId).ToHashSet();
            Assert.Greater(customIds.Count(dropped.Contains), customIds.Count / 2, "the custom pieces come from bosses");
        }

        [Test]
        public void CustomPets_HatchFromTheirEggs()
        {
            var custom = PetCatalog.All.Where(p => string.IsNullOrEmpty(p.TameItemId)).ToList();
            Assert.Greater(custom.Count, 10, "XileRetro's own pets (no taming item: their eggs drop)");
            foreach (var pet in custom)
            {
                Assert.AreEqual(ItemSpecialEffect.HatchPet, ItemCatalog.Get(pet.EggItemId)?.Special, pet.EggItemId);
                Assert.IsNotNull(MonsterCatalog.Get(pet.MonsterId), pet.MonsterId);
                Assert.IsTrue(MonsterCatalog.Get(pet.MonsterId).Drops.Any(d => d.ItemId == pet.EggItemId), $"{pet.EggItemId} drops from its monster");
            }
        }
    }
}
