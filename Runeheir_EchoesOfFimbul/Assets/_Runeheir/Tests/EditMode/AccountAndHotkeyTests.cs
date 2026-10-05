using NUnit.Framework;
using Runeheir.Accounts;
using Runeheir.Characters;
using Runeheir.Hotkeys;
using Runeheir.Items;

namespace Runeheir.Tests
{
    public sealed class AccountAndHotkeyTests
    {
        private int _persistCount;

        private AccountStore NewStore()
        {
            _persistCount = 0;
            return new AccountStore(new AccountDatabase(), _ => _persistCount++, () => 1000);
        }

        private static CharacterCreateRequest Request(string name)
        {
            return new CharacterCreateRequest { Name = name, Gender = Gender.Female, HairStyle = 2, HairColor = 4 };
        }

        [Test]
        public void Register_ThenLogin_WithHashedPassword()
        {
            var store = NewStore();
            Assert.IsTrue(store.Register("odin_main", "valhalla123").Success);
            Assert.IsFalse(store.Register("ODIN_MAIN", "whatever123").Success, "usernames are case-insensitive");

            Assert.IsTrue(store.Login("odin_main", "valhalla123").Success);
            Assert.IsFalse(store.Login("odin_main", "wrong-pass").Success);
            Assert.IsFalse(store.Login("nobody", "valhalla123").Success);
            Assert.Greater(_persistCount, 0);
        }

        [Test]
        public void Register_RejectsBadInput()
        {
            var store = NewStore();
            Assert.IsFalse(store.Register("abc", "valhalla123").Success, "too short");
            Assert.IsFalse(store.Register("bad name", "valhalla123").Success, "space");
            Assert.IsFalse(store.Register("goodname", "123").Success, "short password");
        }

        [Test]
        public void PasswordHasher_NeverStoresPlainText()
        {
            PasswordHasher.Hash("valhalla123", out string hash, out string salt, out int iterations);
            Assert.AreNotEqual("valhalla123", hash);
            Assert.IsTrue(PasswordHasher.Verify("valhalla123", hash, salt, iterations));
            Assert.IsFalse(PasswordHasher.Verify("valhalla124", hash, salt, iterations));
        }

        [Test]
        public void CreateCharacter_EnforcesSlotsAndUniqueNames()
        {
            var store = NewStore();
            store.Register("player_one", "valhalla123");
            store.Register("player_two", "valhalla123");

            var created = store.CreateCharacter("player_one", 0, Request("Ragnar"));
            Assert.IsTrue(created.Success, created.Error);
            Assert.AreEqual(1, created.Value.BaseLevel);
            Assert.AreEqual(Jobs.JobId.Initiate, created.Value.Job);

            Assert.IsFalse(store.CreateCharacter("player_one", 0, Request("Lagertha")).Success, "slot taken");
            Assert.IsFalse(store.CreateCharacter("player_two", 0, Request("ragnar")).Success, "name is global");
            Assert.IsFalse(store.CreateCharacter("player_one", AccountRules.MaxCharacterSlots, Request("Bjorn")).Success);
            Assert.IsFalse(store.CreateCharacter("player_one", 1, Request("No@Way")).Success);
        }

        [Test]
        public void SaveCharacter_PersistsCopiesAndChecksOwnership()
        {
            var store = NewStore();
            store.Register("player_one", "valhalla123");
            store.Register("player_two", "valhalla123");
            var record = store.CreateCharacter("player_one", 3, Request("Ivar Boneless")).Value;

            record.BaseLevel = 42;
            Assert.AreEqual(1, store.GetCharacters("player_one")[0].BaseLevel, "callers hold copies");

            Assert.IsTrue(store.SaveCharacter("player_one", record).Success);
            Assert.AreEqual(42, store.GetCharacters("player_one")[0].BaseLevel);
            Assert.IsFalse(store.SaveCharacter("player_two", record).Success);
        }

        [Test]
        public void DeleteCharacter_RequiresTypedName()
        {
            var store = NewStore();
            store.Register("player_one", "valhalla123");
            store.CreateCharacter("player_one", 0, Request("Ragnar"));

            Assert.IsFalse(store.DeleteCharacter("player_one", 0, "ragnar").Success);
            Assert.IsTrue(store.DeleteCharacter("player_one", 0, "Ragnar").Success);
            Assert.AreEqual(0, store.GetCharacters("player_one").Count);
            Assert.IsFalse(store.IsNameTaken("Ragnar"));
        }

        [Test]
        public void FailedWrites_RollBack_AndLoginStillWorks()
        {
            bool diskFull = false;
            var store = new AccountStore(new AccountDatabase(), _ =>
            {
                if (diskFull)
                {
                    throw new System.IO.IOException("disk full");
                }
            }, () => 1000);

            diskFull = true;
            var failed = store.Register("thor_main", "valhalla123");
            Assert.IsFalse(failed.Success);
            StringAssert.Contains("disk full", failed.Error);
            diskFull = false;
            Assert.IsTrue(store.Register("thor_main", "valhalla123").Success, "a failed register leaves no ghost account");

            var record = store.CreateCharacter("thor_main", 0, Request("Mjolnir Bearer")).Value;
            diskFull = true;
            Assert.IsTrue(store.Login("thor_main", "valhalla123").Success, "last-login bookkeeping can't block a login");
            Assert.IsFalse(store.CreateCharacter("thor_main", 1, Request("Second One")).Success);
            Assert.IsFalse(store.IsNameTaken("Second One"));

            Assert.IsFalse(store.DeleteCharacter("thor_main", 0, "Mjolnir Bearer").Success);
            Assert.AreEqual(1, store.GetCharacters("thor_main").Count, "a failed delete keeps the character");

            record.BaseLevel = 50;
            Assert.IsFalse(store.SaveCharacter("thor_main", record).Success);
            Assert.AreEqual(1, store.GetCharacters("thor_main")[0].BaseLevel, "a failed save keeps the stored record");
        }

        [Test]
        public void Names_RejectTrailingNewline()
        {
            Assert.IsFalse(AccountRules.ValidateUsername("odin_main\n", out _), "no look-alike accounts");
            Assert.IsTrue(AccountRules.ValidateUsername("odin_main", out _));
            Assert.AreEqual("Ragnar", CharacterNames.Normalize("Ragnar\n"), "character names are trimmed before use");
        }

        [Test]
        public void UnknownJobInSave_IsRepaired_NotThrown()
        {
            var store = NewStore();
            store.Register("player_one", "valhalla123");
            var record = store.CreateCharacter("player_one", 0, Request("Future Hero")).Value;
            record.Job = (Runeheir.Jobs.JobId)999;
            store.SaveCharacter("player_one", record);

            var characters = store.GetCharacters("player_one");
            Assert.AreEqual(1, characters.Count);
            Assert.AreEqual(Runeheir.Jobs.JobId.Initiate, characters[0].Job);
        }

        [Test]
        public void NewCharacter_HasStarterHotkeysAndItems()
        {
            var record = CharacterFactory.Create(Request("Freydis"), 0, 0);
            var layout = new HotkeyLayout(record.Hotkeys);

            Assert.AreEqual(HotkeyKind.Skill, layout.Get(0, 0).Kind);
            Assert.AreEqual(ItemCatalog.LingonberryTonic, layout.Get(0, 1).Id);
            Assert.IsTrue(new Inventory(record.Inventory).Has(ItemCatalog.LingonberryTonic, 30));
        }

        [Test]
        public void HotkeyLayout_AssignSwapClearAndNormalize()
        {
            var layout = new HotkeyLayout(HotkeyLayout.CreateEmptyArray());
            int changes = 0;
            layout.Changed += () => changes++;

            layout.Assign(HotkeyLayout.ToIndex(0, 9), HotkeySlot.Skill("bash"));
            layout.Assign(HotkeyLayout.ToIndex(1, 0), HotkeySlot.Item(ItemCatalog.HoneyMead));
            layout.Swap(HotkeyLayout.ToIndex(0, 9), HotkeyLayout.ToIndex(1, 0));

            Assert.AreEqual(ItemCatalog.HoneyMead, layout.Get(0, 9).Id);
            Assert.AreEqual("bash", layout.Get(1, 0).Id);

            layout.Clear(HotkeyLayout.ToIndex(1, 0));
            Assert.IsTrue(layout.Get(1, 0).IsEmpty);
            Assert.AreEqual(4, changes);

            Assert.AreEqual(HotkeyLayout.TotalSlots, HotkeyLayout.Normalize(new HotkeySlot[3]).Length);
        }

        [Test]
        public void Inventory_StacksAndRemoves()
        {
            var inventory = new Inventory(new System.Collections.Generic.List<ItemStack>());
            Assert.AreEqual(5, inventory.Add(ItemCatalog.AetherSap, 5));
            Assert.AreEqual(3, inventory.Add(ItemCatalog.AetherSap, 3));
            Assert.AreEqual(1, inventory.Stacks.Count);
            Assert.AreEqual(0, inventory.Add("not_an_item", 1));

            Assert.IsTrue(inventory.TryRemove(ItemCatalog.AetherSap, 8));
            Assert.IsFalse(inventory.TryRemove(ItemCatalog.AetherSap));
            Assert.AreEqual(0, inventory.Stacks.Count);
        }
    }
}
