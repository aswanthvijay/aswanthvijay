using System.Collections;
using System.IO;
using NUnit.Framework;
using Runeheir.Accounts;
using Runeheir.Characters;
using Runeheir.Combat;
using Runeheir.Field;
using Runeheir.FrontEnd;
using Runeheir.Items;
using Runeheir.Jobs;
using Runeheir.Monsters;
using Runeheir.Movement;
using Runeheir.Player;
using Runeheir.Session;
using Runeheir.Stats;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Runeheir.Tests
{
    /// <summary>
    /// Play-mode smoke tests that drive the real runtime in Unity (Test Runner ▸ PlayMode, or CI).
    /// Unity fails a test on any logged error/exception, so these also catch crashes in Update loops
    /// (HUD, world UI, AI, skills) while the scenarios run.
    /// </summary>
    public sealed class PrototypeSmokeTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.Destroy(root);
            }

            if (GameSession.Exists)
            {
                Object.Destroy(GameSession.Instance.gameObject);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Field_ClickToMove_Attack_Skills_Hotkeys_DeathAndRespawn()
        {
            BuildMiniField();
            yield return null;
            yield return null;

            var player = PlayerCharacter.Local;
            Assert.IsNotNull(player, "FieldBootstrap spawned the player");
            Assert.IsTrue(player.GetComponent<NavMeshAgent>().isOnNavMesh, "player is on the NavMesh");

            // --- NavMesh click-to-move
            var motor = player.GetComponent<NavMotor>();
            Vector3 start = player.Position;
            Assert.IsTrue(motor.MoveTo(start + new Vector3(8f, 0f, 0f)), "MoveTo accepted");
            yield return new WaitForSeconds(2.5f);
            Assert.Greater(Vector3.Distance(start, player.Position), 5f, "player walked");

            // --- Click-to-attack on a training dummy
            var dummy = EntityFactory.CreateMonster(MonsterCatalog.Get("training_dummy"), player.Position + new Vector3(3f, 0f, 0f));
            int hits = 0;
            System.Action<CombatEntity, DamageResult, CombatEntity> counter = (target, result, attacker) =>
            {
                if (target == dummy && attacker == player)
                {
                    hits++;
                }
            };
            CombatEntity.AnyDamaged += counter;
            player.GetComponent<AutoAttacker>().Engage(dummy);
            yield return new WaitForSeconds(4f);
            CombatEntity.AnyDamaged -= counter;
            Assert.GreaterOrEqual(hits, 2, "auto-attack loop keeps swinging");
            Assert.IsFalse(dummy.IsDead, "training dummy never dies");

            // --- Skills: Two-Hand Surge adds +7 ASPD (stat engine -> buffs -> derived stats)
            player.Progression.ForceChangeJob(JobId.Einherjar);
            float aspdBefore = player.Aspd;
            player.GetComponent<SkillCaster>().RequestSkill("two_hand_surge", null, null);
            yield return null;
            Assert.IsTrue(player.Buffs.Has(BuffCatalog.TwoHandSurge), "buff applied");
            Assert.AreEqual(Mathf.Min(StatFormulas.MaxAspd, aspdBefore + 7f), player.Aspd, 0.01f);

            // --- Hotkeys: F2 holds Lingonberry Tonic on a new character
            int tonics = player.Inventory.Count(ItemCatalog.LingonberryTonic);
            player.GetComponent<HotkeyController>().Activate(1);
            Assert.AreEqual(tonics - 1, player.Inventory.Count(ItemCatalog.LingonberryTonic), "hotkey used the item");

            // --- Level up through the progression engine
            player.Progression.SetBaseLevel(99);
            Assert.AreEqual(99, player.Record.BaseLevel);
            Assert.AreEqual(StatFormulas.MaxHp(99, player.Stats.Total.Vit), player.MaxHp);

            // --- Death and respawn
            player.ReceiveDamage(DamageResult.Fixed(10000000), dummy, physicalMelee: false);
            Assert.IsTrue(player.IsDead);
            yield return new WaitForSeconds(0.5f);
            player.RespawnAtSavePoint();
            Assert.IsFalse(player.IsDead);
            yield return new WaitForSeconds(0.5f);
        }

        [UnityTest]
        public IEnumerator Monsters_SpawnWanderAggroAndDie()
        {
            BuildMiniField();
            yield return null;
            yield return null;

            var player = PlayerCharacter.Local;
            player.Progression.SetBaseLevel(150);
            player.Progression.SetAllStats(150);

            var imp = EntityFactory.CreateMonster(MonsterCatalog.Get("forest_imp"), player.Position + new Vector3(4f, 0f, 2f));
            yield return new WaitForSeconds(1.5f);
            Assert.AreEqual(player, imp.GetComponent<AutoAttacker>().Target, "aggressive monster engages the player");

            long expBefore = player.Record.BaseExp + player.Record.BaseLevel * 1000000L;
            player.GetComponent<AutoAttacker>().Engage(imp);
            float timeout = Time.time + 15f;
            while (!imp.IsDead && Time.time < timeout)
            {
                yield return null;
            }

            Assert.IsTrue(imp.IsDead, "level 150 character kills a Forest Imp");
            Assert.Greater(player.Record.BaseExp + player.Record.BaseLevel * 1000000L, expBefore, "EXP awarded");
            yield return new WaitForSeconds(0.5f);
        }

        [UnityTest]
        public IEnumerator FrontEnd_Builds_And_AccountFlowWorks()
        {
            string dbPath = Path.Combine(Application.temporaryCachePath, "runeheir_test_accounts.json");
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }

            var session = GameSession.Instance;
            session.Accounts = new LocalAccountService(dbPath);
            new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>().cullingMask = 0;
            var frontEnd = new GameObject("FrontEnd").AddComponent<FrontEndController>();
            yield return null;
            yield return null;

            var register = session.Accounts.RegisterAsync("smoketest", "valhalla123");
            yield return new WaitUntil(() => register.IsCompleted);
            Assert.IsTrue(register.Result.Success, register.Result.Error);

            var login = session.Accounts.LoginAsync("smoketest", "valhalla123");
            yield return new WaitUntil(() => login.IsCompleted);
            Assert.IsTrue(login.Result.Success, login.Result.Error);
            session.SetLoggedIn(login.Result.Value);

            var created = session.Accounts.CreateCharacterAsync("smoketest", 0, new CharacterCreateRequest { Name = "Smoke Test", HairStyle = 3, HairColor = 4 });
            yield return new WaitUntil(() => created.IsCompleted);
            Assert.IsTrue(created.Result.Success, created.Result.Error);

            frontEnd.ShowServerSelect();
            yield return null;
            frontEnd.ShowCharacterSelect(0);
            yield return null;
            yield return null;
            frontEnd.ShowCharacterCreate(1);
            yield return null;
            frontEnd.ShowLogin();
            yield return new WaitForSeconds(0.5f);

            Assert.IsTrue(File.Exists(dbPath), "account database written");
            Assert.IsFalse(File.ReadAllText(dbPath).Contains("valhalla123"), "password never stored in plain text");
            File.Delete(dbPath);
        }

        private static void BuildMiniField()
        {
            new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
            new GameObject("Sun").AddComponent<Light>().type = LightType.Directional;

            var environment = new GameObject("Environment");
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.SetParent(environment.transform, false);
            ground.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(60f, 1f, 60f);
            environment.AddComponent<RuntimeNavMeshBaker>();

            new GameObject("FieldBootstrap").AddComponent<FieldBootstrap>();
        }
    }
}
