using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Runeheir.Characters;
using Runeheir.Combat;
using Runeheir.Items;
using Runeheir.Jobs;
using Runeheir.Monsters;
using Runeheir.Skills;
using Runeheir.Stats;
using Runeheir.World;

namespace Runeheir.Tests
{
    /// <summary>Phase 7: Rune Forging and Brewing, the Trader's Haggle and Silver Tongue, and the Norns at Urðr's Well.</summary>
    public sealed class CraftingTests
    {
        private static BaseStats Stats(int dex = 50, int luk = 30, int intel = 30)
        {
            return new BaseStats { Dex = dex, Luk = luk, Int = intel };
        }

        private static Inventory WithMaterials(CraftRecipe recipe, int times = 1)
        {
            var inventory = new Inventory(new List<ItemStack>());
            foreach (var material in recipe.Materials)
            {
                inventory.Add(material.ItemId, material.Amount * times);
            }

            return inventory;
        }

        [Test]
        public void Forge_MakesBladedAndBluntWeaponsOfLevelOneToThree()
        {
            var forge = CraftingRules.For(CraftKind.Forge);
            Assert.GreaterOrEqual(forge.Count, 12);
            var forgeable = new[]
            {
                WeaponType.Dagger, WeaponType.TwoHandSword, WeaponType.Spear, WeaponType.Mace, WeaponType.Knuckle, WeaponType.Katar,
                WeaponType.Axe, WeaponType.TwoHandAxe,
            };
            foreach (var recipe in forge)
            {
                var product = recipe.Product;
                Assert.IsNotNull(product, recipe.ProductId);
                Assert.IsTrue(product.IsWeapon, product.Id);
                Assert.That(product.WeaponLevel, Is.InRange(1, 3), product.Id);
                Assert.Contains(product.WeaponType, forgeable, product.Id);
                Assert.AreEqual(product.WeaponLevel, recipe.SkillLevel, "one Rune Forging level per weapon level");
            }

            Assert.IsNotNull(CraftingRules.Get(CraftKind.Forge, "dane_axe"));
            Assert.IsNotNull(CraftingRules.Get(CraftKind.Forge, "iron_claymore"));
            Assert.IsNull(CraftingRules.Get(CraftKind.Forge, "hunters_bow"), "smiths don't make bows");
            Assert.IsNull(CraftingRules.Get(CraftKind.Forge, "eitri_great_axe"), "legendary (level 4) weapons can't be forged");
            Assert.IsNull(CraftingRules.Get(CraftKind.Forge, "rusty_seax"));
            Assert.AreEqual(8, CraftingRules.For(CraftKind.Brew).Count);
        }

        [Test]
        public void EveryCraftingMaterial_CanBeBoughtOrLooted()
        {
            var sold = new HashSet<string>(ShopCatalog.All.SelectMany(s => s.ItemIds));
            var dropped = new HashSet<string>(MonsterCatalog.All.SelectMany(m => m.Drops).Select(d => d.ItemId));
            foreach (var kind in new[] { CraftKind.Forge, CraftKind.Brew })
            {
                foreach (var recipe in CraftingRules.For(kind))
                {
                    Assert.IsNotNull(recipe.Product, recipe.ProductId);
                    Assert.Greater(recipe.ProductAmount, 0);
                    foreach (var material in recipe.Materials)
                    {
                        Assert.IsNotNull(material.Definition, $"{recipe.ProductId} uses unknown {material.ItemId}");
                        Assert.Greater(material.Amount, 0);
                        Assert.IsTrue(sold.Contains(material.ItemId) || dropped.Contains(material.ItemId),
                            $"{material.ItemId} (for {recipe.ProductId}) is neither sold nor dropped");
                    }
                }
            }
        }

        [Test]
        public void SuccessChance_FollowsSkillResearchStatsAndDifficulty()
        {
            var level1 = CraftingRules.Get(CraftKind.Forge, "iron_spear");
            var level3 = CraftingRules.Get(CraftKind.Forge, "dane_axe");
            Assert.AreEqual(1, level1.SkillLevel);
            Assert.AreEqual(3, level3.SkillLevel);

            float easy = CraftingRules.SuccessChance(level1, 3, 0, 1, Stats(dex: 1, luk: 1));
            float hard = CraftingRules.SuccessChance(level3, 3, 0, 1, Stats(dex: 1, luk: 1));
            Assert.Greater(easy, hard, "higher weapon levels are harder");
            Assert.Greater(CraftingRules.SuccessChance(level3, 3, 10, 50, Stats(dex: 90, luk: 60)), hard, "research, job level, DEX and LUK help");
            Assert.AreEqual(CraftingRules.MaxChance, CraftingRules.SuccessChance(level1, 3, 10, 50, Stats(dex: 99, luk: 99)));
            Assert.AreEqual(CraftingRules.MinChance, CraftingRules.SuccessChance(level3, 1, 0, 1, Stats(dex: 1, luk: 1)));

            var tonic = CraftingRules.Get(CraftKind.Brew, ItemCatalog.LingonberryTonic);
            var isa = CraftingRules.Get(CraftKind.Brew, ItemCatalog.RuneIsa);
            Assert.Greater(CraftingRules.SuccessChance(tonic, 10, 10, 40, Stats()), CraftingRules.SuccessChance(isa, 10, 10, 40, Stats()));
            Assert.Greater(CraftingRules.SuccessChance(isa, 10, 10, 40, Stats()), CraftingRules.SuccessChance(isa, 10, 0, 40, Stats()));
        }

        [Test]
        public void TryCraft_UsesTheMaterials_AndMakesTheProductOnSuccess()
        {
            var recipe = CraftingRules.Get(CraftKind.Forge, "bearded_axe");
            var inventory = WithMaterials(recipe);
            var outcome = CraftingRules.TryCraft(recipe, 2, 5, 40, Stats(), inventory, new SequenceRandom(0.0), out string message);
            Assert.AreEqual(CraftOutcome.Success, outcome, message);
            Assert.AreEqual(1, inventory.Count("bearded_axe"));
            foreach (var material in recipe.Materials)
            {
                Assert.AreEqual(0, inventory.Count(material.ItemId), material.ItemId);
            }

            var brew = CraftingRules.Get(CraftKind.Brew, ItemCatalog.LingonberryTonic);
            var bag = WithMaterials(brew);
            Assert.AreEqual(CraftOutcome.Success, CraftingRules.TryCraft(brew, 1, 0, 1, Stats(), bag, new SequenceRandom(0.0), out _));
            Assert.AreEqual(brew.ProductAmount, bag.Count(ItemCatalog.LingonberryTonic));
        }

        [Test]
        public void TryCraft_FailureLosesTheMaterials()
        {
            var recipe = CraftingRules.Get(CraftKind.Forge, "dane_axe");
            var inventory = WithMaterials(recipe, times: 2);
            var outcome = CraftingRules.TryCraft(recipe, 3, 0, 1, Stats(), inventory, new SequenceRandom(0.999), out string message);
            Assert.AreEqual(CraftOutcome.Failure, outcome);
            StringAssert.Contains("lost", message);
            Assert.AreEqual(0, inventory.Count("dane_axe"));
            foreach (var material in recipe.Materials)
            {
                Assert.AreEqual(material.Amount, inventory.Count(material.ItemId), "one attempt's worth is gone");
            }
        }

        [Test]
        public void CanCraft_NeedsTheSkillLevelTheMaterialsAndRoom()
        {
            var recipe = CraftingRules.Get(CraftKind.Forge, "dane_axe");
            var inventory = WithMaterials(recipe);
            Assert.IsFalse(CraftingRules.CanCraft(recipe, 2, inventory, out string reason));
            StringAssert.Contains("Rune Forging Lv 3", reason);
            Assert.AreEqual(CraftOutcome.NotAllowed, CraftingRules.TryCraft(recipe, 2, 0, 1, Stats(), inventory, new SequenceRandom(0.0), out _));
            Assert.AreEqual(recipe.Materials[0].Amount, inventory.Count(recipe.Materials[0].ItemId), "a refused attempt keeps the materials");

            inventory.TryRemove(ItemCatalog.Starmetal);
            Assert.IsFalse(CraftingRules.CanCraft(recipe, 3, inventory, out reason));
            StringAssert.Contains("Starmetal", reason);

            // A bag at its entry limit has no room for a new weapon (the materials stay as partial stacks).
            var full = WithMaterials(recipe, times: 2);
            int filler = 0;
            foreach (var item in ItemCatalog.All.Where(i => i.IsEquipment))
            {
                if (full.Stacks.Count >= Inventory.MaxEntries)
                {
                    break;
                }

                filler += full.Add(item.Id, 1);
            }

            while (full.Stacks.Count < Inventory.MaxEntries)
            {
                full.Add("rusty_seax", 1);
            }

            Assert.Greater(filler, 0);
            Assert.IsFalse(CraftingRules.CanCraft(recipe, 3, full, out reason));
            StringAssert.Contains("bag is full", reason);
        }

        [Test]
        public void Haggle_AndSilverTongue_ChangeShopPrices_WithoutArbitrage()
        {
            var mead = ItemCatalog.Get(ItemCatalog.HoneyMead);
            Assert.AreEqual(mead.Price, TradeRules.BuyPrice(mead));
            Assert.AreEqual((long)(mead.Price * 0.76), TradeRules.BuyPrice(mead, 24f));
            Assert.AreEqual(TradeRules.BuyPrice(mead, 24f), TradeRules.BuyPrice(mead, 80f), "capped at 24%");
            Assert.AreEqual((long)(mead.SellPrice * 1.24), TradeRules.SellPrice(mead, 24f));

            foreach (var shop in ShopCatalog.All)
            {
                foreach (string id in shop.ItemIds)
                {
                    var item = ItemCatalog.Get(id);
                    Assert.GreaterOrEqual(TradeRules.BuyPrice(item, TradeRules.MaxPricePercent), TradeRules.SellPrice(item, TradeRules.MaxPricePercent),
                        $"{id} can be bought and resold at a profit");
                }
            }

            var record = CharacterFactory.Create(new CharacterCreateRequest { Name = "Haggler" }, 0, 0);
            record.Zeny = 10000;
            var inventory = new Inventory(record.Inventory);
            var store = ShopCatalog.Get(ShopCatalog.GeneralStore);
            Assert.IsTrue(TradeRules.TryBuy(record, inventory, store, ItemCatalog.HoneyMead, 2, 99999, 0, out string message, 24f), message);
            Assert.AreEqual(10000 - 2 * TradeRules.BuyPrice(mead, 24f), record.Zeny);
            var stack = inventory.FindFirst(ItemCatalog.HoneyMead);
            long before = record.Zeny;
            Assert.IsTrue(TradeRules.TrySell(record, inventory, stack, 2, out message, 24f), message);
            Assert.AreEqual(before + 2 * TradeRules.SellPrice(mead, 24f), record.Zeny);
        }

        [Test]
        public void HaggleTen_GivesTheFullDiscountInDerivedStats()
        {
            var record = CharacterFactory.Create(new CharacterCreateRequest { Name = "Trader Ten" }, 0, 0);
            record.Job = JobId.Trader;
            record.Skills.Add(new LearnedSkill("haggle", 10));
            record.Skills.Add(new LearnedSkill("silver_tongue", 10));
            var book = new SkillBook(record);
            var derived = DerivedStats.Compute(record.BaseLevel, record.Stats, book.PassiveModifiers(WeaponType.Axe), WeaponProfile.BareHands);
            Assert.AreEqual(24f, derived.BuyDiscountPercent, 0.01f);
            Assert.AreEqual(24f, derived.SellBonusPercent, 0.01f);
        }

        [Test]
        public void TheNorns_WaitAtUrdrsWellInVigrid()
        {
            var vigrid = MapCatalog.Get(MapCatalog.VigridHaven);
            var norns = vigrid.Npcs.SingleOrDefault(n => n.Kind == NpcKind.Norns);
            Assert.IsNotNull(norns, "Vigrid needs the rebirth NPC");
            Assert.IsFalse(vigrid.Npcs.Any(n => n != norns && System.Math.Abs(n.X - norns.X) < 4f && System.Math.Abs(n.Z - norns.Z) < 4f),
                "the Norns stand apart from the other NPCs");
            Assert.AreEqual("a Skald", JobDatabase.WithArticle("Skald"));
            Assert.AreEqual("an Einherjar", JobDatabase.WithArticle("Einherjar"));
            Assert.AreEqual("an Initiate", JobDatabase.WithArticle("Initiate"));
        }
    }
}
