using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mirror;
using NUnit.Framework;
using Runeheir.Characters;
using Runeheir.Combat;
using Runeheir.Field;
using Runeheir.Items;
using Runeheir.Monsters;
using Runeheir.Net;
using Runeheir.Online;
using Runeheir.Player;
using Runeheir.Session;
using Runeheir.Social;
using Runeheir.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Runeheir.Tests
{
    /// <summary>
    /// Phase 6 play-mode smoke test: host a realm in this process (Mirror host mode), register and log in over the
    /// network account service, enter Vigrid Haven through the realm, and use what the realm runs: monsters, rewards,
    /// chat, a party and a street stall.
    /// </summary>
    public sealed class NetworkSmokeTests
    {
        private const ushort Port = 7791;
        private string _data;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _data = Path.Combine(Application.temporaryCachePath, "realm_test_" + Guid.NewGuid().ToString("N"));
            RealmLauncher.DataDirectoryOverride = _data;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            OnlineSession.Launcher?.Shutdown();
            RealmLauncher.DataDirectoryOverride = null;
            yield return null;
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                UnityEngine.Object.Destroy(root);
            }

            if (GameSession.Exists)
            {
                UnityEngine.Object.Destroy(GameSession.Instance.gameObject);
            }

            yield return null;
            try
            {
                Directory.Delete(_data, true);
            }
            catch (Exception)
            {
                // A file still locked on Windows: the temp folder is cleaned up later anyway.
            }
        }

        private static IEnumerator Await(Task task, float seconds = 15f)
        {
            float until = Time.realtimeSinceStartup + seconds;
            yield return new WaitUntil(() => task.IsCompleted || Time.realtimeSinceStartup > until);
            Assert.IsTrue(task.IsCompleted, "timed out");
            if (task.IsFaulted)
            {
                throw task.Exception;
            }
        }

        private static IEnumerator Until(Func<bool> condition, string what, float seconds = 10f)
        {
            float until = Time.realtimeSinceStartup + seconds;
            yield return new WaitUntil(() => condition() || Time.realtimeSinceStartup > until);
            Assert.IsTrue(condition(), "timed out waiting for " + what);
        }

        [UnityTest]
        public IEnumerator Host_LogsIn_EntersVigrid_AndPlaysOnTheRealm()
        {
            // --- Host a realm and connect to it.
            bool? hosted = null;
            string hostError = null;
            OnlineSession.Launcher.Host(Port, (ok, error) =>
            {
                hosted = ok;
                hostError = error;
            });
            yield return Until(() => hosted.HasValue, "the realm to start");
            Assert.IsTrue(hosted.Value, hostError);
            Assert.IsNotNull(OnlineSession.Current, "connected sessions are online");
            Assert.IsTrue(OnlineSession.Current.IsServer, "a host runs the realm");
            Assert.IsNotNull(RealmServer.Instance);
            Assert.IsTrue(File.Exists(Path.Combine(_data, RealmServer.ConfigFile)), "realm.json written with defaults");

            // --- Register, log in and create a character through the realm's account service.
            var accounts = OnlineSession.Launcher.Accounts;
            Assert.IsNotNull(accounts);
            Assert.IsFalse(accounts.IsOffline);
            var register = accounts.RegisterAsync("nettester", "valhalla123");
            yield return Await(register);
            Assert.IsTrue(register.Result.Success, register.Result.Error);
            var login = accounts.LoginAsync("nettester", "valhalla123");
            yield return Await(login);
            Assert.IsTrue(login.Result.Success, login.Result.Error);
            var servers = accounts.GetServersAsync();
            yield return Await(servers);
            Assert.AreEqual(RealmServer.Instance.Config.Name, servers.Result[0].Name);
            var created = accounts.CreateCharacterAsync(login.Result.Value, 0, new CharacterCreateRequest { Name = "Net Tester" });
            yield return Await(created);
            Assert.IsTrue(created.Result.Success, created.Result.Error);

            var session = GameSession.Instance;
            session.Accounts = accounts;
            session.SetLoggedIn(login.Result.Value);
            session.SelectCharacter(created.Result.Value);

            // --- Enter Vigrid Haven: the host shows the realm's own copy of the map and the realm places the character.
            new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
            var bootstrap = new GameObject("WorldBootstrap").AddComponent<FieldBootstrap>();
            bootstrap.GeneratesMap = true;
            yield return Until(() => PlayerCharacter.Local != null, "the realm to spawn our character");
            var player = PlayerCharacter.Local;
            var identity = player.GetComponent<NetworkIdentity>();
            Assert.IsNotNull(identity, "the character is a networked object");
            Assert.IsTrue(identity.isLocalPlayer);
            Assert.IsNotNull(player.GetComponent<NetPlayer>());
            Assert.AreEqual(MapCatalog.VigridHaven, FieldContext.MapId);
            Assert.AreSame(RealmServer.Instance.EnsureMap(MapCatalog.VigridHaven), bootstrap.World, "the host shows the realm's map");
            var origin = WorldGrid.Origin(MapCatalog.VigridHaven);
            Assert.AreEqual(new Vector3(origin.X, 0f, origin.Z), FieldContext.Origin);
            yield return Until(() => GameObject.Find("Minimap") != null, "the HUD");

            // --- A monster made on the realm is networked, and its kill pays the player.
            var definition = MonsterCatalog.Get("rune_spore");
            var monster = EntityFactory.CreateMonster(definition, player.Position + new Vector3(2f, 0f, 0f));
            yield return null;
            Assert.IsNotNull(monster.GetComponent<NetMonster>());
            Assert.AreNotEqual(0u, monster.GetComponent<NetworkIdentity>().netId, "spawned on the realm");
            long exp = player.Record.BaseExp;
            int level = player.Record.BaseLevel;
            monster.ReceiveDamage(DamageResult.Fixed(monster.MaxHp * 10), player, physicalMelee: true);
            Assert.IsTrue(monster.IsDead);
            Assert.IsTrue(player.Record.BaseExp > exp || player.Record.BaseLevel > level, "EXP from the realm's kill");

            // --- Chat goes through the realm and comes back to everyone on the map.
            OnlineSession.Current.SendChat(ChatRules.Parse("hello realm"));
            yield return Until(() => ChatLog.Lines.Any(l => l.Text.Contains("Net Tester: hello realm")), "the map chat echo");

            // --- A party, made through the realm.
            var social = OnlineSession.Current.Social;
            social.PartyCreate("Test Raiders");
            yield return Until(() => social.State.Party != null, "the party");
            Assert.AreEqual("Test Raiders", social.State.Party.Name);
            Assert.IsTrue(social.State.Party.IsLeader("Net Tester"));
            social.PartySetShare(ExpShareMode.EvenShare);
            yield return Until(() => social.State.Party.ExpShare == ExpShareMode.EvenShare, "Even Share");

            // --- A street stall: the goods go to the cart hold while it's open and come back when it closes.
            player.Record.Zeny = 10000;
            player.Record.BaseLevel = Mathf.Max(player.Record.BaseLevel, PushcartRules.MinBaseLevel);
            Assert.IsTrue(PushcartRules.TryRent(player.Record, out string rentMessage), rentMessage);
            player.Recalculate();
            player.Inventory.Add(ItemCatalog.LingonberryTonic, 20);
            int tonics = player.Inventory.Count(ItemCatalog.LingonberryTonic);
            var vigrid = MapCatalog.Get(MapCatalog.VigridHaven);
            player.TeleportTo(FieldContext.SavePoint + new Vector3(4f, 0f, 6f), null); // clear of NPCs and portals
            yield return null;
            Assert.IsTrue(VendingRules.TryOpen(player.Record, player.Inventory, vigrid, "Smoke Test Wares",
                new[] { new VendingRequest(player.Inventory.FindFirst(ItemCatalog.LingonberryTonic), 5, 25) }, out var stall, out string vendError), vendError);
            social.VendOpen(stall);
            yield return Until(() => social.State.MyStall != null, "the stall to open");
            Assert.AreEqual(tonics - 5, player.Inventory.Count(ItemCatalog.LingonberryTonic));
            social.VendClose();
            yield return Until(() => social.State.MyStall == null, "the stall to close");
            Assert.AreEqual(tonics, player.Inventory.Count(ItemCatalog.LingonberryTonic), "unsold goods are back in the bag");

            // --- Leaving the realm puts the game back offline.
            OnlineSession.Launcher.Shutdown();
            yield return null;
            Assert.IsNull(OnlineSession.Current);
            Assert.IsNull(RealmServer.Instance);
        }
    }
}
