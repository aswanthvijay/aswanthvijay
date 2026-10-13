using System.Linq;
using NUnit.Framework;
using Runeheir.Characters;
using Runeheir.Items;
using Runeheir.Monsters;
using Runeheir.Pets;
using Runeheir.Stats;

namespace Runeheir.Tests
{
    /// <summary>Phase 8: Ragnarok's pets (pet_db.yml), tamed, hatched, fed and Loyal.</summary>
    public sealed class PetTests
    {
        // The Slimling (Poring): tamed with an Unripe Apple, eats Apple Juice, wears a Backpack, LUK +2 CRIT +1 when Loyal.
        private const string SlimlingEgg = "xr9001";

        private static CharacterRecord Owner(out Inventory bag)
        {
            var record = CharacterFactory.Create(new CharacterCreateRequest { Name = "Pet Tester" }, 0, 0);
            bag = new Inventory(record.Inventory);
            return record;
        }

        private static PetState Hatched(CharacterRecord record, Inventory bag)
        {
            bag.Add(SlimlingEgg, 1);
            Assert.IsTrue(PetCatalog.Hatch(record, bag, SlimlingEgg, out string message), message);
            return PetCatalog.Active(record);
        }

        [Test]
        public void Catalog_EveryPetPointsAtRealThings()
        {
            Assert.GreaterOrEqual(PetCatalog.All.Count, 50);
            foreach (var pet in PetCatalog.All)
            {
                Assert.IsNotNull(ItemCatalog.Get(pet.EggItemId), pet.EggItemId);
                Assert.AreEqual(ItemSpecialEffect.HatchPet, ItemCatalog.Get(pet.EggItemId).Special, pet.EggItemId);
                Assert.IsNotNull(MonsterCatalog.Get(pet.MonsterId), $"{pet.EggItemId}: monster {pet.MonsterId}");
                if (!string.IsNullOrEmpty(pet.FoodItemId))
                {
                    Assert.IsNotNull(ItemCatalog.Get(pet.FoodItemId), $"{pet.EggItemId}: food {pet.FoodItemId}");
                }

                if (!string.IsNullOrEmpty(pet.ArmorItemId))
                {
                    Assert.AreEqual(ItemSpecialEffect.PetArmor, ItemCatalog.Get(pet.ArmorItemId)?.Special, $"{pet.EggItemId}: armor {pet.ArmorItemId}");
                }

                if (!string.IsNullOrEmpty(pet.TameItemId))
                {
                    Assert.AreEqual(ItemSpecialEffect.TamePet, ItemCatalog.Get(pet.TameItemId)?.Special, $"{pet.EggItemId}: tamer {pet.TameItemId}");
                    Assert.AreSame(pet, PetCatalog.ForTaming(pet.TameItemId, pet.MonsterId));
                }
            }
        }

        [Test]
        public void TameChance_RisesAsTheMonsterWeakens_AndIsCapped()
        {
            var pet = PetCatalog.ForEgg(SlimlingEgg);
            float healthy = PetCatalog.TameChance(pet, 1f, 1);
            float hurt = PetCatalog.TameChance(pet, 0.1f, 1);
            Assert.Greater(hurt, healthy);
            Assert.LessOrEqual(PetCatalog.TameChance(pet, 0f, 999), 95f);
        }

        [Test]
        public void Hatch_RemembersThePet_AndOnlyOneIsOut()
        {
            var record = Owner(out var bag);
            var state = Hatched(record, bag);
            Assert.AreEqual(PetCatalog.StartIntimacy, state.Intimacy);
            Assert.IsFalse(bag.Has(SlimlingEgg), "the egg is used up");

            bag.Add("xr9002", 1);
            Assert.IsFalse(PetCatalog.Hatch(record, bag, "xr9002", out _), "one pet at a time");

            state.Intimacy = 600;
            Assert.IsTrue(PetCatalog.ReturnToEgg(record, bag, out _));
            Assert.IsTrue(bag.Has(SlimlingEgg), "back in its egg");
            Assert.IsNull(PetCatalog.Active(record));
            Assert.IsTrue(PetCatalog.Hatch(record, bag, SlimlingEgg, out _));
            Assert.AreEqual(600, PetCatalog.Active(record).Intimacy, "the same pet comes back");
        }

        [Test]
        public void Feeding_WhenHungry_RaisesIntimacy_WhenFull_Annoys()
        {
            var record = Owner(out var bag);
            var state = Hatched(record, bag);
            var pet = PetCatalog.ForEgg(SlimlingEgg);
            bag.Add(pet.FoodItemId, 5);

            state.Hunger = 20;
            Assert.IsTrue(PetCatalog.Feed(record, bag, out _));
            Assert.AreEqual(PetCatalog.StartIntimacy + pet.IntimacyFed, state.Intimacy);

            state.Hunger = 90;
            int before = state.Intimacy;
            Assert.IsTrue(PetCatalog.Feed(record, bag, out _));
            Assert.Less(state.Intimacy, before, "overfed");
        }

        [Test]
        public void Starving_DrainsIntimacy_UntilThePetRunsAway()
        {
            var record = Owner(out var bag);
            var state = Hatched(record, bag);
            state.Hunger = 1;
            state.Intimacy = 40;
            Assert.IsFalse(PetCatalog.Starve(record, out string starving), "the first empty tick only hurts");
            StringAssert.Contains("starving", starving);
            Assert.AreEqual(20, state.Intimacy);
            Assert.IsTrue(PetCatalog.Starve(record, out string message), "then it leaves");
            StringAssert.Contains("ran away", message);
            Assert.IsNull(PetCatalog.Active(record));
            Assert.IsEmpty(record.Pets);
        }

        [Test]
        public void LoyalPet_AddsItsBonusToTheOwner()
        {
            var record = Owner(out var bag);
            var state = Hatched(record, bag);
            int luk = EquipmentStats.Compute(record).Modifiers.GetStat(StatType.Luk);

            state.Intimacy = PetCatalog.LoyalAt - 1;
            Assert.AreEqual(luk, EquipmentStats.Compute(record).Modifiers.GetStat(StatType.Luk), "Cordial: no bonus yet");

            state.Intimacy = PetCatalog.LoyalAt;
            Assert.AreEqual(luk + 2, EquipmentStats.Compute(record).Modifiers.GetStat(StatType.Luk), "Loyal Slimling: LUK +2");
        }

        [Test]
        public void Accessory_FitsOnlyItsPet_AndComesBackOff()
        {
            var record = Owner(out var bag);
            var state = Hatched(record, bag);
            var pet = PetCatalog.ForEgg(SlimlingEgg);
            var wrong = PetCatalog.All.First(p => !string.IsNullOrEmpty(p.ArmorItemId) && p.ArmorItemId != pet.ArmorItemId).ArmorItemId;

            Assert.IsFalse(PetCatalog.WearArmor(record, wrong, out _));
            Assert.IsTrue(PetCatalog.WearArmor(record, pet.ArmorItemId, out _));
            Assert.AreEqual(pet.ArmorItemId, state.ArmorItemId);
            Assert.IsFalse(PetCatalog.WearArmor(record, pet.ArmorItemId, out _), "one at a time");

            Assert.IsTrue(PetCatalog.TakeOffArmor(record, bag, out _));
            Assert.IsNull(state.ArmorItemId);
            Assert.IsTrue(bag.Has(pet.ArmorItemId));
        }

        [Test]
        public void Pets_SurviveTheSaveCopy_AndSanitize()
        {
            var record = Owner(out var bag);
            var state = Hatched(record, bag);
            state.Intimacy = 950;
            state.ArmorItemId = PetCatalog.ForEgg(SlimlingEgg).ArmorItemId;
            record.Pets.Add(new PetState { EggItemId = "not-an-egg" });

            var copy = record.Clone();
            copy.Sanitize();
            Assert.AreEqual(1, copy.Pets.Count, "unknown eggs are dropped");
            Assert.AreEqual(950, copy.Pets[0].Intimacy);
            Assert.AreEqual(state.ArmorItemId, copy.Pets[0].ArmorItemId);
            Assert.AreEqual(SlimlingEgg, copy.ActivePetEgg);
        }
    }
}
