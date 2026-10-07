using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Runeheir.Characters;
using Runeheir.Combat;
using Runeheir.Items;
using Runeheir.Jobs;
using Runeheir.Social;
using Runeheir.Stats;
using Runeheir.World;

namespace Runeheir.Tests
{
    /// <summary>Phase 6 social rules: parties and Even Share, trades, the Pushcart, street stalls, guilds and chat.</summary>
    public sealed class SocialTests
    {
        private static CharacterRecord NewRecord(string name, int baseLevel = 50, long zeny = 100000)
        {
            var record = CharacterFactory.Create(new CharacterCreateRequest { Name = name }, 0, 0);
            record.BaseLevel = baseLevel;
            record.Zeny = zeny;
            record.Inventory.Clear();
            return record;
        }

        private static PartyMember Member(string name, int level, string map = MapCatalog.VigridHaven)
        {
            return new PartyMember { Name = name, BaseLevel = level, Job = JobId.Warrior, MapId = map, Online = true, Hp = 100, MaxHp = 100 };
        }

        private static ItemStack Piece(Inventory bag, string id, int refine = 0)
        {
            Assert.AreEqual(1, bag.Add(id, 1), id);
            var piece = bag.Stacks.Last(s => s.ItemId == id);
            piece.Refine = refine;
            return piece;
        }

        // ------------------------------------------------------------ Pushcart (GDD §8: +8,000 weight)
        [Test]
        public void Pushcart_AddsEightThousandWeight_AndCostsZeny()
        {
            var record = NewRecord("Cart Hauler", 20, 5000);
            var without = DerivedStats.Compute(record.BaseLevel, record.Stats, PushcartRules.Modifiers(record), WeaponProfile.BareHands);

            Assert.IsTrue(PushcartRules.TryRent(record, out string message), message);
            Assert.IsTrue(record.HasPushcart);
            Assert.AreEqual(5000 - PushcartRules.RentalFee, record.Zeny);
            var with = DerivedStats.Compute(record.BaseLevel, record.Stats, PushcartRules.Modifiers(record), WeaponProfile.BareHands);
            Assert.AreEqual(without.WeightCapacity + 8000, with.WeightCapacity);

            Assert.IsFalse(PushcartRules.TryRent(record, out _), "one cart is enough");
            var poor = NewRecord("Poor Hauler", 20, 10);
            Assert.IsFalse(PushcartRules.CanRent(poor, out _));
            var young = NewRecord("Young Hauler", 3);
            Assert.IsFalse(PushcartRules.CanRent(young, out _));
        }

        // ------------------------------------------------------------ parties and Even Share (GDD §8: 30-level gap)
        [Test]
        public void Party_EvenShareNeedsLevelsWithinThirty()
        {
            var book = new PartyBook();
            var party = book.Create(Member("Astrid", 60), "Shieldmaidens").Value;
            Assert.IsTrue(book.Invite("Astrid", "Bjorn").Success);
            Assert.IsTrue(book.Accept(Member("Bjorn", 90), party.Id, out _).Success);
            Assert.IsTrue(book.SetExpShare("Astrid", ExpShareMode.EvenShare).Success, "gap of exactly 30 is allowed");

            Assert.IsTrue(book.Invite("Astrid", "Cnut").Success);
            Assert.IsTrue(book.Accept(Member("Cnut", 91), party.Id, out bool dropped).Success);
            Assert.IsTrue(dropped, "a member 31 levels apart turns Even Share off");
            Assert.AreEqual(ExpShareMode.EachTakes, party.ExpShare);
            Assert.IsFalse(book.SetExpShare("Astrid", ExpShareMode.EvenShare).Success);

            Assert.IsNotNull(book.Leave("Cnut"));
            Assert.IsTrue(book.SetExpShare("Astrid", ExpShareMode.EvenShare).Success);
            book.UpdateMember("Bjorn", 95, JobId.Warrior, MapCatalog.VigridHaven, true, 1, 1, out dropped);
            Assert.IsTrue(dropped, "levelling past the gap turns it off too");
        }

        [Test]
        public void Party_SharesEvenlyWithBonus_OnlyWithMembersOnTheMap()
        {
            Assert.AreEqual(1000, PartyRules.ShareEach(1000, 1));
            Assert.AreEqual(550, PartyRules.ShareEach(1000, 2), "+10% pool for the second member, split two ways");
            Assert.AreEqual(400, PartyRules.ShareEach(1000, 3), "+20% pool, three ways");
            Assert.AreEqual(1, PartyRules.ShareEach(1, 12), "a kill worth anything gives at least 1");
            Assert.AreEqual(0, PartyRules.ShareEach(0, 3));

            var book = new PartyBook();
            var party = book.Create(Member("Astrid", 60), "Raiders").Value;
            foreach (var (name, map) in new[] { ("Bjorn", MapCatalog.VigridHaven), ("Cnut", MapCatalog.WhisperwoodPlains), ("Dagny", MapCatalog.VigridHaven) })
            {
                book.Invite("Astrid", name);
                book.Accept(Member(name, 70, map), party.Id, out _);
            }

            Assert.IsEmpty(book.Sharers("Astrid", MapCatalog.VigridHaven, null), "Each Takes: nobody shares");
            book.SetExpShare("Astrid", ExpShareMode.EvenShare);
            party.Member("Dagny").Online = false;
            var sharers = book.Sharers("Bjorn", MapCatalog.VigridHaven, m => m.Name != "Astrid" || m.Hp > 0);
            CollectionAssert.AreEquivalent(new[] { "Astrid", "Bjorn" }, sharers.Select(m => m.Name), "same map, online");
        }

        [Test]
        public void Party_LeaderRules_InvitesAndDisband()
        {
            var book = new PartyBook();
            var party = book.Create(Member("Astrid", 60), "Wolfpack").Value;
            Assert.IsFalse(book.Create(Member("Bjorn", 60), "wolfpack").Success, "names are unique");
            Assert.IsFalse(book.Create(Member("Astrid", 60), "Second").Success, "one party at a time");

            book.Invite("Astrid", "Bjorn");
            Assert.IsFalse(book.Accept(Member("Bjorn", 60), party.Id + 1, out _).Success, "only the party that invited");
            book.Invite("Astrid", "Bjorn");
            Assert.IsTrue(book.Accept(Member("Bjorn", 60), party.Id, out _).Success);
            Assert.IsFalse(book.Invite("Bjorn", "Cnut").Success, "members don't invite");
            Assert.IsFalse(book.Kick("Bjorn", "Astrid").Success);
            Assert.IsTrue(book.MakeLeader("Astrid", "Bjorn").Success);
            Assert.AreEqual("Bjorn", party.Leader);

            for (int i = 0; i < PartyRules.MaxMembers - 2; i++)
            {
                string name = "Filler " + i;
                Assert.IsTrue(book.Invite("Bjorn", name).Success, name);
                Assert.IsTrue(book.Accept(Member(name, 60), party.Id, out _).Success, name);
            }

            Assert.AreEqual(PartyRules.MaxMembers, party.Members.Count);
            Assert.IsFalse(book.Invite("Bjorn", "Thirteenth").Success, "12 members at most");

            book.Leave("Bjorn");
            Assert.AreNotEqual("Bjorn", party.Leader, "the lead passes on");
            foreach (var member in party.Members.ToList())
            {
                book.Leave(member.Name);
            }

            Assert.IsNull(book.Get(party.Id), "the last one out disbands it");
            Assert.IsNull(book.PartyOf("Astrid"));
        }

        // ------------------------------------------------------------ player trade
        [Test]
        public void Trade_LocksThenConfirms_AndSwapsExactPieces()
        {
            var session = new TradeSession("Astrid", "Bjorn");
            var astrid = NewRecord("Astrid", 50, 1000);
            var bjorn = NewRecord("Bjorn", 50, 0);
            var astridBag = new Inventory(astrid.Inventory);
            var bjornBag = new Inventory(bjorn.Inventory);
            var plain = Piece(astridBag, "iron_claymore");
            var refined = Piece(astridBag, "iron_claymore", 7);
            astridBag.Add(ItemCatalog.LingonberryTonic, 20);

            Assert.IsTrue(session.TryAddItem("Astrid", refined, out _));
            Assert.IsTrue(session.TryAddItem("Astrid", new ItemStack(ItemCatalog.LingonberryTonic, 5), out _));
            Assert.IsTrue(session.TryAddItem("Astrid", new ItemStack(ItemCatalog.LingonberryTonic, 3), out _), "stackables merge");
            Assert.AreEqual(2, session.OfferA.Items.Count);
            Assert.IsTrue(session.TrySetZeny("Astrid", 400, out _));
            Assert.IsFalse(session.TryConfirm("Astrid", out _), "both must lock first");
            Assert.IsTrue(session.TryLock("Astrid", out _));
            Assert.IsFalse(session.TrySetZeny("Astrid", 1, out _), "a locked offer can't change");
            Assert.IsTrue(session.TryLock("Bjorn", out _));
            Assert.IsTrue(session.TryConfirm("Astrid", out _));
            Assert.IsTrue(session.TryConfirm("Bjorn", out _));
            Assert.IsTrue(session.BothConfirmed);

            int capacity = 100000;
            Assert.IsTrue(PlayerTradeRules.CanExchange(astrid, astridBag, capacity, astridBag.TotalWeight(), session.OfferA, session.OfferB, out string error), error);
            Assert.IsTrue(PlayerTradeRules.CanExchange(bjorn, bjornBag, capacity, bjornBag.TotalWeight(), session.OfferB, session.OfferA, out error), error);
            var given = session.OfferA.Clone();
            Assert.IsTrue(PlayerTradeRules.TryGive(astrid, astridBag, given, out error), error);
            Assert.AreEqual(0, PlayerTradeRules.Receive(bjorn, bjornBag, given));

            Assert.IsTrue(astridBag.Contains(plain), "the +0 claymore stays: the +7 one was offered");
            Assert.IsFalse(astridBag.Contains(refined));
            Assert.AreEqual(12, astridBag.Count(ItemCatalog.LingonberryTonic));
            Assert.AreEqual(600, astrid.Zeny);
            Assert.AreEqual(400, bjorn.Zeny);
            Assert.AreEqual(7, bjornBag.FindFirst("iron_claymore").Refine);
            Assert.AreEqual(8, bjornBag.Count(ItemCatalog.LingonberryTonic));
        }

        [Test]
        public void Trade_RefusesWhatYouCantGiveOrCarry()
        {
            var astrid = NewRecord("Astrid", 50, 100);
            var bag = new Inventory(astrid.Inventory);
            var give = new TradeOffer { Zeny = 500 };
            Assert.IsFalse(PlayerTradeRules.CanExchange(astrid, bag, 100000, 0, give, new TradeOffer(), out _), "not enough zeny");

            give = new TradeOffer { Items = { new ItemStack(ItemCatalog.LingonberryTonic, 1) } };
            Assert.IsFalse(PlayerTradeRules.CanExchange(astrid, bag, 100000, 0, give, new TradeOffer(), out _), "nothing to give");
            Assert.IsFalse(PlayerTradeRules.TryGive(astrid, bag, give, out _));

            var heavy = new TradeOffer { Items = { ItemStack.NewInstance(ItemCatalog.Get("iron_claymore")) } };
            Assert.IsFalse(PlayerTradeRules.CanExchange(astrid, bag, 10, 0, new TradeOffer(), heavy, out _), "too heavy");

            var rich = new TradeOffer { Zeny = ItemTransfer.MaxZeny };
            Assert.IsFalse(PlayerTradeRules.CanExchange(astrid, bag, 100000, 0, new TradeOffer(), rich, out _), "zeny cap");

            var session = new TradeSession("Astrid", "Bjorn");
            for (int i = 0; i < TradeSession.MaxItems; i++)
            {
                Assert.IsTrue(session.TryAddItem("Astrid", ItemStack.NewInstance(ItemCatalog.Get("iron_claymore")), out _));
            }

            Assert.IsFalse(session.TryAddItem("Astrid", ItemStack.NewInstance(ItemCatalog.Get("iron_claymore")), out _), "10 items at most");
            Assert.IsFalse(session.TryAddItem("Cnut", new ItemStack(ItemCatalog.LingonberryTonic, 1), out _), "outsiders can't add");
        }

        [Test]
        public void Trade_OverflowWaitsInTheCartHold()
        {
            var bjorn = NewRecord("Bjorn");
            var bag = new Inventory(bjorn.Inventory);
            for (int i = 0; i < Inventory.MaxEntries; i++)
            {
                bjorn.Inventory.Add(ItemStack.NewInstance(ItemCatalog.Get("iron_claymore")));
            }

            var gift = new TradeOffer { Items = { new ItemStack(ItemCatalog.LingonberryTonic, 5) } };
            Assert.AreEqual(1, PlayerTradeRules.Receive(bjorn, bag, gift));
            Assert.AreEqual(5, bjorn.Cart.Single().Amount);

            bjorn.Inventory.RemoveAt(0);
            Assert.AreEqual(1, ItemTransfer.ReturnCartToBag(bjorn, bag, 1000000));
            Assert.IsEmpty(bjorn.Cart);
            Assert.AreEqual(5, bag.Count(ItemCatalog.LingonberryTonic));
        }

        // ------------------------------------------------------------ street vending (GDD §8)
        [Test]
        public void Vending_NeedsACartAndVigrid_AndMovesGoodsToTheCart()
        {
            var seller = NewRecord("Seller");
            var bag = new Inventory(seller.Inventory);
            bag.Add(ItemCatalog.LingonberryTonic, 30);
            var claymore = Piece(bag, "iron_claymore", 4);
            var vigrid = MapCatalog.Get(MapCatalog.VigridHaven);
            var tonics = bag.FindFirst(ItemCatalog.LingonberryTonic);
            var goods = new[] { new VendingRequest(tonics, 10, 50), new VendingRequest(claymore, 1, 90000) };

            Assert.IsFalse(VendingRules.TryOpen(seller, bag, vigrid, "Shop", goods, out _, out _), "no Pushcart");
            seller.HasPushcart = true;
            Assert.IsFalse(VendingRules.TryOpen(seller, bag, MapCatalog.Get(MapCatalog.WhisperwoodPlains), "Shop", goods, out _, out _), "fields refuse");
            Assert.IsFalse(VendingRules.TryOpen(seller, bag, vigrid, "Shop", new[] { new VendingRequest(tonics, 31, 50) }, out _, out _), "more than you have");
            Assert.IsFalse(VendingRules.TryOpen(seller, bag, vigrid, "Shop", new[] { new VendingRequest(tonics, 1, 0) }, out _, out _), "free isn't a price");
            Assert.IsFalse(VendingRules.TryOpen(seller, bag, vigrid, "Shop", new[] { goods[0], goods[0] }, out _, out _), "no duplicates");

            Assert.IsTrue(VendingRules.TryOpen(seller, bag, vigrid, "<b>Cheap</b>   tonics", goods, out var stall, out string error), error);
            Assert.AreEqual("‹b›Cheap‹/b› tonics", stall.Title, "rich text is defused");
            Assert.AreEqual(20, bag.Count(ItemCatalog.LingonberryTonic));
            Assert.IsFalse(bag.Contains(claymore));
            Assert.AreEqual(2, seller.Cart.Count, "the goods wait in the cart");

            var tooMany = new List<VendingRequest>();
            for (int i = 0; i < VendingRules.MaxEntries + 1; i++)
            {
                tooMany.Add(new VendingRequest(Piece(bag, "iron_claymore"), 1, 10));
            }

            Assert.IsFalse(VendingRules.TryOpen(seller, bag, vigrid, "Big", tooMany, out _, out _), "12 lines at most");
        }

        [Test]
        public void Vending_BuyTakesDeliversAndReceives()
        {
            var seller = NewRecord("Seller", 50, 0);
            seller.HasPushcart = true;
            var bag = new Inventory(seller.Inventory);
            bag.Add(ItemCatalog.LingonberryTonic, 30);
            var vigrid = MapCatalog.Get(MapCatalog.VigridHaven);
            Assert.IsTrue(VendingRules.TryOpen(seller, bag, vigrid, null, new[] { new VendingRequest(bag.FindFirst(ItemCatalog.LingonberryTonic), 10, 50) },
                out var stall, out _));
            Assert.AreEqual(VendingRules.DefaultTitle, stall.Title);

            var server = stall.Clone();
            Assert.IsTrue(VendingRules.Validate(server, out _));
            var buyer = NewRecord("Buyer", 50, 1000);
            var buyerBag = new Inventory(buyer.Inventory);

            Assert.IsFalse(VendingRules.CanBuy(server, 0, 11, buyer.Zeny, out _, out _), "only 10 on sale");
            Assert.IsFalse(VendingRules.CanBuy(server, 0, 4, 100, out _, out _), "can't afford");
            Assert.IsTrue(VendingRules.CanBuy(server, 0, 4, buyer.Zeny, out long cost, out _));
            Assert.AreEqual(200, cost);
            Assert.IsFalse(VendingRules.TryTake(server, 0, server.Entries[0].Item, 4, 49, out _, out _, out _), "price changed");
            Assert.IsTrue(VendingRules.TryTake(server, 0, server.Entries[0].Item, 4, 50, out var sold, out cost, out _));
            Assert.AreEqual(6, server.Entries[0].Item.Amount);

            buyer.Zeny -= cost;
            Assert.IsTrue(VendingRules.TryDeliver(seller, sold, cost, out string error), error);
            Assert.AreEqual(200, seller.Zeny);
            Assert.AreEqual(6, seller.Cart.Single().Amount);
            Assert.AreEqual(0, VendingRules.Receive(buyer, buyerBag, sold));
            Assert.AreEqual(4, buyerBag.Count(ItemCatalog.LingonberryTonic));
            Assert.AreEqual(800, buyer.Zeny);

            Assert.IsFalse(VendingRules.TryDeliver(seller, new ItemStack(ItemCatalog.LingonberryTonic, 7), 350, out _), "the cart only holds 6");
            VendingRules.PutBack(server, sold, 50);
            Assert.AreEqual(10, server.Entries[0].Item.Amount, "a failed sale goes back on the stall");

            // The cart's goods stay while the stall is open, then come home.
            Assert.AreEqual(0, ItemTransfer.ReturnCartToBag(seller, bag, 100000, 0, stall));
            Assert.AreEqual(1, ItemTransfer.ReturnCartToBag(seller, bag, 100000));
            Assert.AreEqual(26, bag.Count(ItemCatalog.LingonberryTonic));
            Assert.IsEmpty(seller.Cart);
        }

        [Test]
        public void CharacterRecord_KeepsTheCartThroughCloneAndSanitize()
        {
            var record = NewRecord("Cart Keeper");
            record.HasPushcart = true;
            record.Cart.Add(new ItemStack(ItemCatalog.LingonberryTonic, 3));
            record.Cart.Add(new ItemStack("no_such_item", 3));
            record.Cart.Add(null);
            var copy = record.Clone();
            copy.Sanitize();
            Assert.IsTrue(copy.HasPushcart);
            Assert.AreEqual(1, copy.Cart.Count);
            copy.Cart[0].Amount = 1;
            Assert.AreEqual(3, record.Cart[0].Amount, "clone is deep");
        }

        // ------------------------------------------------------------ guilds
        [Test]
        public void Guild_FoundInviteRanksAndPersistence()
        {
            GuildDatabase saved = null;
            int writes = 0;
            var book = new GuildBook(new GuildDatabase(), db =>
            {
                saved = db;
                writes++;
            });

            Assert.IsFalse(book.Create("Astrid", 10, JobId.Warrior, "Valhalla Bound").Success, "founders need level 30");
            Assert.IsFalse(book.Create("Astrid", 40, JobId.Warrior, "V!").Success);
            var guild = book.Create("Astrid", 40, JobId.Warrior, "Valhalla Bound").Value;
            Assert.IsFalse(book.Create("Bjorn", 40, JobId.Warrior, "valhalla bound").Success, "names are unique");

            Assert.IsTrue(book.Invite("Astrid", "Bjorn").Success);
            Assert.IsTrue(book.Accept("Bjorn", 20, JobId.Scout, guild.Id).Success);
            Assert.IsFalse(book.Invite("Bjorn", "Cnut").Success, "members don't invite");
            Assert.IsTrue(book.SetRank("Astrid", "Bjorn", GuildRank.Officer).Success);
            Assert.IsTrue(book.Invite("Bjorn", "Cnut").Success, "officers do");
            Assert.IsTrue(book.Accept("Cnut", 20, JobId.Mystic, guild.Id).Success);
            Assert.IsFalse(book.Expel("Cnut", "Bjorn").Success);
            Assert.IsTrue(book.Expel("Bjorn", "Cnut").Success);
            Assert.IsTrue(book.SetNotice("Bjorn", "Raid at <dusk>").Success);
            Assert.AreEqual("Raid at ‹dusk›", guild.Notice);

            Assert.IsFalse(book.Leave("Astrid").Success, "the leader hands over first");
            Assert.IsTrue(book.HandOver("Astrid", "Bjorn").Success);
            Assert.IsTrue(book.Leave("Astrid").Success);
            Assert.AreEqual(GuildRank.Leader, guild.Member("Bjorn").Rank);
            Assert.Greater(writes, 5);

            var reloaded = new GuildBook(saved, null);
            Assert.AreEqual("Valhalla Bound", reloaded.GuildOf("Bjorn").Name);
            Assert.IsTrue(reloaded.Create("Astrid", 40, JobId.Warrior, "Second Guild").Success, "ids continue past the saved ones");
            Assert.AreNotEqual(guild.Id, reloaded.GuildOf("Astrid").Id);

            Assert.IsTrue(reloaded.Disband("Bjorn").Success);
            Assert.IsNull(reloaded.GuildOf("Bjorn"));
        }

        [Test]
        public void Guild_FailedWriteRollsBack()
        {
            bool fail = false;
            var book = new GuildBook(new GuildDatabase(), _ =>
            {
                if (fail)
                {
                    throw new System.IO.IOException("disk full");
                }
            });
            var guild = book.Create("Astrid", 40, JobId.Warrior, "Rollback").Value;
            book.Invite("Astrid", "Bjorn");
            fail = true;
            Assert.IsFalse(book.Accept("Bjorn", 30, JobId.Scout, guild.Id).Success);
            Assert.AreEqual(1, guild.Members.Count);
            Assert.IsFalse(book.Create("Cnut", 40, JobId.Warrior, "Never Saved").Success);
            Assert.AreEqual(1, book.All.Count);
        }

        // ------------------------------------------------------------ chat
        [Test]
        public void Chat_ParsesChannelsAndWhispers()
        {
            Assert.AreEqual(ChatChannel.Local, ChatRules.Parse("hello there").Channel);
            Assert.AreEqual(ChatChannel.Party, ChatRules.Parse("%pull left").Channel);
            Assert.AreEqual("pull left", ChatRules.Parse("/p pull left").Text);
            Assert.AreEqual(ChatChannel.Guild, ChatRules.Parse("$meeting").Channel);
            Assert.AreEqual(ChatChannel.Shout, ChatRules.Parse("/sh selling cards").Channel);
            Assert.IsTrue(ChatRules.Parse("@warp vigrid_haven").IsCommand);
            Assert.IsTrue(ChatRules.Parse("/dance").IsCommand, "unknown slash commands aren't sent as chat");

            var whisper = ChatRules.Parse("/w Bjorn meet me at the forge");
            Assert.AreEqual(ChatChannel.Whisper, whisper.Channel);
            Assert.AreEqual("Bjorn", whisper.Target);
            Assert.AreEqual("meet me at the forge", whisper.Text);
            whisper = ChatRules.Parse("/w \"Ivar the Bold\" hi");
            Assert.AreEqual("Ivar the Bold", whisper.Target);
            Assert.AreEqual("hi", whisper.Text);
            Assert.AreEqual("Cnut", ChatRules.Parse("/r thanks", "Cnut").Target);

            Assert.AreEqual("‹color=red›x‹/color› y", ChatRules.Sanitize("<color=red>x</color>\n\t y"));
            Assert.AreEqual(ChatRules.MaxLength, ChatRules.Sanitize(new string('a', 500)).Length);
            Assert.AreEqual("[Party] Astrid: go", ChatRules.Format(ChatChannel.Party, "Astrid", "go"));
        }

        [Test]
        public void Chat_FloodGateLimitsBurstsAndShouts()
        {
            var gate = new ChatFloodGate();
            for (int i = 0; i < ChatRules.FloodLimit; i++)
            {
                Assert.IsTrue(gate.Allow(ChatChannel.Local, i * 0.1, out _));
            }

            Assert.IsFalse(gate.Allow(ChatChannel.Local, 0.7, out _));
            Assert.IsTrue(gate.Allow(ChatChannel.Shout, 10, out _));
            Assert.IsFalse(gate.Allow(ChatChannel.Shout, 15, out _), "one shout per 10 s");
            Assert.IsTrue(gate.Allow(ChatChannel.Shout, 21, out _));
        }

        // ------------------------------------------------------------ realm settings
        [Test]
        public void Realm_ParsesAddressesAndClampsSettings()
        {
            Assert.IsTrue(RealmConfig.TryParseAddress("192.168.1.20", out string host, out ushort port, out _));
            Assert.AreEqual(("192.168.1.20", RealmConfig.DefaultPort), (host, port));
            Assert.IsTrue(RealmConfig.TryParseAddress("realm.example:9000", out host, out port, out _));
            Assert.AreEqual(("realm.example", (ushort)9000), (host, port));
            Assert.IsTrue(RealmConfig.TryParseAddress("[::1]:7000", out host, out port, out _));
            Assert.AreEqual(("::1", (ushort)7000), (host, port));
            Assert.IsTrue(RealmConfig.TryParseAddress("fe80::1", out host, out _, out _));
            Assert.AreEqual("fe80::1", host);
            Assert.IsFalse(RealmConfig.TryParseAddress("host:99999", out _, out _, out _));
            Assert.IsFalse(RealmConfig.TryParseAddress("  ", out _, out _, out _));

            var config = new RealmConfig { Name = "", MaxPlayers = 0, BaseExpRate = float.NaN, Port = 0, DropRate = 1e9f };
            config.Sanitize();
            Assert.AreEqual("Runeheir Alpha", config.Name);
            Assert.AreEqual(1, config.MaxPlayers);
            Assert.AreEqual(0f, config.BaseExpRate);
            Assert.AreEqual(RealmConfig.DefaultPort, config.Port);
            Assert.AreEqual(100f, config.DropRate);
        }

        [Test]
        public void Vigrid_HasThePushcartMerchant()
        {
            var vigrid = MapCatalog.Get(MapCatalog.VigridHaven);
            Assert.IsTrue(VendingRules.MapAllowsVending(vigrid));
            Assert.IsTrue(vigrid.Npcs.Any(n => n.Kind == NpcKind.CartMerchant));
            Assert.IsFalse(MapCatalog.All.Where(m => m.Id != MapCatalog.VigridHaven).Any(VendingRules.MapAllowsVending), "only Vigrid vends");
        }
    }
}
