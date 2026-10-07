using System;
using System.Collections.Generic;
using Runeheir.Characters;

namespace Runeheir.Items
{
    public sealed class ShopDefinition
    {
        public string Id;
        public string Name;
        public string Greeting;
        public string[] ItemIds = Array.Empty<string>();
    }

    /// <summary>NPC shops. Phase 4 places their keepers by the save point; Phase 5 moves them into Vigrid Haven.</summary>
    public static class ShopCatalog
    {
        public const string GeneralStore = "general_store";
        public const string ForgeSupplies = "forge_supplies";
        public const string Armory = "vigrid_armory";
        public const string BranchWarden = "branch_warden";

        private static readonly Dictionary<string, ShopDefinition> ById = new Dictionary<string, ShopDefinition>
        {
            {
                GeneralStore, new ShopDefinition
                {
                    Id = GeneralStore,
                    Name = "Ásta's Trading Post",
                    Greeting = "Tonics, feathers and honest gear. I'll buy your loot for half price.",
                    ItemIds = new[]
                    {
                        ItemCatalog.LingonberryTonic, ItemCatalog.HoneyMead, ItemCatalog.AetherSap, ItemCatalog.RavenFeather,
                        ItemCatalog.WindRuneShard, ItemCatalog.DeadBranch,
                        "cotton_tunic", "leather_jerkin", "buckler", "traveler_cloak", "sandals", "leather_boots", "bandana", "fur_cap",
                        "clip_ring", "hunters_bow", "iron_spear", "oak_wand", ItemCatalog.RustySeax, ItemCatalog.Seax, ItemCatalog.IronMace,
                        "woodcutters_axe", "spark_rod", "bygul_staff",
                    },
                }
            },
            {
                ForgeSupplies, new ShopDefinition
                {
                    Id = ForgeSupplies,
                    Name = "Brokk's Dwarven Forge",
                    Greeting = "Ores, runes and glyphs. Bring me steel and I'll make it sing.",
                    ItemIds = new[]
                    {
                        ItemCatalog.BogIron, ItemCatalog.DwarvenSteel, ItemCatalog.Starmetal, ItemCatalog.Skystone,
                        ItemCatalog.RuneOfPreservation, ItemCatalog.RuneOfExtraction,
                        RunewordRules.Sowilo, RunewordRules.Tiwaz, RunewordRules.Isa, RunewordRules.Hagalaz, RunewordRules.Thurisaz, RunewordRules.Uruz,
                        ItemCatalog.RuneThurisaz, ItemCatalog.RuneIsa, ItemCatalog.RuneHagalaz,
                    },
                }
            },
            {
                Armory, new ShopDefinition
                {
                    Id = Armory,
                    Name = "Vigrid Armory",
                    Greeting = "Mail, greatswords and shields for the road north. Rare steel you'll have to win from the wilds.",
                    ItemIds = new[]
                    {
                        "iron_claymore", "steel_claymore", "chainmail", "mystic_robe", "kite_shield", "round_viking_shield", "bear_pelt",
                        "bearded_axe", "willow_lyre", "leather_lash", "rune_primer", "thunder_carbine", "iron_huuma", "trjegul_staff",
                        "feathered_beret", "iron_helm", "eyepatch", "viking_pipe", "braided_beard",
                        "rune_ring", "wolf_tooth_necklace", "raven_brooch", "ward_amulet",
                    },
                }
            },
            {
                BranchWarden, new ShopDefinition
                {
                    Id = BranchWarden,
                    Name = "Hall of Branches Supply",
                    Greeting = "Branches and something to drink before whatever climbs out of them.",
                    ItemIds = new[] { ItemCatalog.DeadBranch, ItemCatalog.LingonberryTonic, ItemCatalog.HoneyMead, ItemCatalog.AetherSap, ItemCatalog.RuneSowilo },
                }
            },
        };

        public static ShopDefinition Get(string id)
        {
            return id != null && ById.TryGetValue(id, out var shop) ? shop : null;
        }

        public static IEnumerable<ShopDefinition> All => ById.Values;
    }

    /// <summary>
    /// Buying and selling for zeny. Merchants pay half the shop price; equipped items can't be sold. A Trader's Haggle takes up
    /// to 24% off the shop price and Silver Tongue gets up to 24% more when selling (Ragnarok's Discount and Overcharge).
    /// </summary>
    public static class TradeRules
    {
        /// <summary>Ragnarok's cap for Discount and Overcharge.</summary>
        public const float MaxPricePercent = 24f;

        /// <summary>What a shop charges for one <paramref name="item"/> with <paramref name="discountPercent"/>% off (at least 1 zeny).</summary>
        public static long BuyPrice(ItemDefinition item, float discountPercent = 0f)
        {
            if (item == null)
            {
                return 0;
            }

            float off = Math.Max(0f, Math.Min(MaxPricePercent, discountPercent));
            return Math.Max(1L, (long)Math.Floor(item.Price * (1.0 - off / 100.0)));
        }

        /// <summary>What a shop pays for one <paramref name="item"/> with <paramref name="bonusPercent"/>% more (0 for worthless items).</summary>
        public static long SellPrice(ItemDefinition item, float bonusPercent = 0f)
        {
            if (item == null || item.SellPrice <= 0)
            {
                return 0;
            }

            float more = Math.Max(0f, Math.Min(MaxPricePercent, bonusPercent));
            return (long)Math.Floor(item.SellPrice * (1.0 + more / 100.0));
        }

        public static bool TryBuy(CharacterRecord record, Inventory inventory, ShopDefinition shop, string itemId, int amount,
            int weightCapacity, int currentWeight, out string message, float discountPercent = 0f)
        {
            var item = ItemCatalog.Get(itemId);
            if (shop == null || item == null || Array.IndexOf(shop.ItemIds, item.Id) < 0)
            {
                message = "That isn't for sale here.";
                return false;
            }

            if (amount <= 0)
            {
                message = "Choose how many to buy.";
                return false;
            }

            long each = BuyPrice(item, discountPercent);
            long cost = each * amount;
            if (record.Zeny < cost)
            {
                message = $"You need {cost:N0} zeny.";
                return false;
            }

            if (currentWeight + (long)item.Weight * amount > weightCapacity)
            {
                message = "You can't carry that much weight.";
                return false;
            }

            int added = inventory.Add(item.Id, amount);
            if (added <= 0)
            {
                message = "Your bag is full.";
                return false;
            }

            record.Zeny -= each * added;
            inventory.NotifyChanged(); // zeny changed after the bag did: refresh the zeny shown
            message = $"Bought {item.Name} x{added} for {each * added:N0} zeny.";
            return true;
        }

        public static bool TrySell(CharacterRecord record, Inventory inventory, ItemStack entry, int amount, out string message, float bonusPercent = 0f)
        {
            var item = entry?.Definition;
            if (item == null || !inventory.Contains(entry))
            {
                message = "That isn't in your bag.";
                return false;
            }

            amount = item.IsStackable ? Math.Min(amount, entry.Amount) : 1;
            if (amount <= 0)
            {
                message = "Choose how many to sell.";
                return false;
            }

            bool removed = item.IsStackable ? inventory.TryRemove(item.Id, amount) : inventory.RemoveEntry(entry);
            if (!removed)
            {
                message = "You don't have that many.";
                return false;
            }

            long earned = SellPrice(item, bonusPercent) * amount;
            record.Zeny += earned;
            inventory.NotifyChanged();
            message = $"Sold {entry.DisplayName} x{amount} for {earned:N0} zeny.";
            return true;
        }
    }

    /// <summary>
    /// Norn Courier storage: one shared chest per account (GDD §2 "the Norns Courier"). Items move between the bag and the
    /// chest; refine, cards and glyphs travel with equipment.
    /// </summary>
    public static class StorageRules
    {
        public const int MaxEntries = 300;

        public static bool TryDeposit(Inventory inventory, List<ItemStack> storage, ItemStack entry, int amount, out string message)
        {
            var item = entry?.Definition;
            if (item == null || !inventory.Contains(entry))
            {
                message = "That isn't in your bag.";
                return false;
            }

            amount = item.IsStackable ? Math.Min(amount, entry.Amount) : 1;
            if (amount <= 0)
            {
                message = "Choose how many to store.";
                return false;
            }

            var existing = item.IsStackable ? storage.Find(s => s.ItemId == item.Id) : null;
            if (existing == null && storage.Count >= MaxEntries)
            {
                message = "Storage is full.";
                return false;
            }

            if (existing != null && existing.Amount + amount > Inventory.MaxStack)
            {
                message = "Storage can't hold more of that.";
                return false;
            }

            if (item.IsStackable)
            {
                if (!inventory.TryRemove(item.Id, amount))
                {
                    message = "You don't have that many.";
                    return false;
                }

                if (existing != null)
                {
                    existing.Amount += amount;
                }
                else
                {
                    storage.Add(new ItemStack(item.Id, amount));
                }
            }
            else
            {
                inventory.RemoveEntry(entry);
                storage.Add(entry);
            }

            message = $"Stored {entry.DisplayName} x{amount}.";
            return true;
        }

        public static bool TryWithdraw(List<ItemStack> storage, Inventory inventory, ItemStack entry, int amount, int weightCapacity, int currentWeight,
            out string message)
        {
            var item = entry?.Definition;
            if (item == null || !storage.Contains(entry))
            {
                message = "That isn't in storage.";
                return false;
            }

            amount = item.IsStackable ? Math.Min(amount, entry.Amount) : 1;
            if (amount <= 0)
            {
                message = "Choose how many to take out.";
                return false;
            }

            if (currentWeight + (long)item.Weight * amount > weightCapacity)
            {
                message = "You can't carry that much weight.";
                return false;
            }

            if (item.IsStackable)
            {
                int added = inventory.Add(item.Id, amount);
                if (added <= 0)
                {
                    message = "Your bag is full.";
                    return false;
                }

                entry.Amount -= added;
                if (entry.Amount <= 0)
                {
                    storage.Remove(entry);
                }

                amount = added;
            }
            else
            {
                if (!inventory.AddEntry(entry))
                {
                    message = "Your bag is full.";
                    return false;
                }

                storage.Remove(entry);
            }

            message = $"Took {item.Name} x{amount} out of storage.";
            return true;
        }
    }
}
