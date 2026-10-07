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
            TestScene.Clear();

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

            // --- Skills: unlearned skills are refused; Two-Hand Surge Lv 10 adds +7 ASPD (skill tree -> buffs -> derived stats)
            player.Progression.ForceChangeJob(JobId.Einherjar);
            var caster = player.GetComponent<SkillCaster>();
            caster.RequestSkill("two_hand_surge", null, null);
            yield return null;
            Assert.IsFalse(player.Buffs.Has(BuffCatalog.TwoHandSurge), "not learned yet");

            player.SkillBook.SetLevel("two_hand_surge", 10);
            float aspdBefore = player.Aspd;
            caster.RequestSkill("two_hand_surge", null, null);
            yield return null;
            Assert.IsTrue(player.Buffs.Has(BuffCatalog.TwoHandSurge), "buff applied");
            Assert.AreEqual(Mathf.Min(StatFormulas.MaxAspd, aspdBefore + 7f), player.Aspd, 0.01f);

            // --- Passives feed the stat engine; poise and statuses reach the player
            int atkBefore = player.Stats.StatusAtk;
            player.SkillBook.SetLevel("sword_mastery", 10);
            Assert.AreEqual(atkBefore + 40, player.Stats.StatusAtk, "Sword Mastery Lv 10 with a greatsword");
            Assert.IsTrue(player.ApplyStatus(StatusEffect.Silence, 1f));
            Assert.IsFalse(player.CanUseSkills, "silenced");
            player.Cleanse();
            Assert.IsTrue(player.CanUseSkills);

            // --- Hotkeys: F2 holds Lingonberry Tonic on a new character
            int tonics = player.Inventory.Count(ItemCatalog.LingonberryTonic);
            player.GetComponent<HotkeyController>().Activate(1);
            Assert.AreEqual(tonics - 1, player.Inventory.Count(ItemCatalog.LingonberryTonic), "hotkey used the item");

            // --- Level up through the progression engine
            player.Progression.SetBaseLevel(99);
            Assert.AreEqual(99, player.Record.BaseLevel);
            Assert.IsTrue(player.Record.Reborn, "Einherjar is a transcendent job: only the reborn reach it");
            Assert.AreEqual(Mathf.RoundToInt(StatFormulas.MaxHp(99, player.Stats.Total.Vit) * (1f + RebirthRules.HpSpBonusPercent / 100f)), player.MaxHp,
                "reborn: +25% Max HP");

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
            DeleteDatabaseFiles(dbPath);

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
            DeleteDatabaseFiles(dbPath);
        }

        /// <summary>The database plus its .tmp/.bak siblings, which LocalAccountService would recover from.</summary>
        private static void DeleteDatabaseFiles(string dbPath)
        {
            foreach (string file in Directory.GetFiles(Path.GetDirectoryName(dbPath), Path.GetFileName(dbPath) + "*"))
            {
                File.Delete(file);
            }
        }

        [UnityTest]
        public IEnumerator Gear_Cards_Npcs_Branches_AndStorage()
        {
            BuildMiniField();
            yield return null;
            yield return null;

            var player = PlayerCharacter.Local;
            Assert.AreEqual(3, NpcActor.All.Count, "shop, forge and storage NPCs stand by the save point");
            Assert.AreEqual(ItemCatalog.RustySeax, player.Equipment.Get(EquipPosition.Weapon).ItemId, "new characters hold the Rusty Seax");
            Assert.AreEqual(WeaponType.Dagger, player.Weapon.Type);

            // --- Wear the tunic (DEF up, model rebuilt), compound a card into it while worn (Max HP +10%)
            int def = player.Stats.Def;
            Assert.IsTrue(player.Equip(player.Inventory.FindFirst("cotton_tunic")));
            Assert.Greater(player.Stats.Def, def);
            var tunic = player.Equipment.Get(EquipPosition.Armor);
            player.Inventory.Add("field_beetle_card", 1);
            int maxHp = player.MaxHp;
            Assert.IsTrue(player.WorkOnPiece(tunic, () => CardRules.TryCompound(player.Inventory, "field_beetle_card", tunic, out _)));
            Assert.AreSame(tunic, player.Equipment.Get(EquipPosition.Armor), "the worn piece went back on");
            Assert.Greater(player.MaxHp, maxHp, "Field Beetle Card: +10% Max HP");

            // --- On-hit card: Jormungandr's Brood frostbites the target on every melee hit
            var seax = player.Equipment.Get(EquipPosition.Weapon);
            seax.Cards[0] = "jormungandrs_brood_card";
            player.Equipment.NotifyChanged();
            var dummy = EntityFactory.CreateMonster(MonsterCatalog.Get("training_dummy"), player.Position + new Vector3(2.5f, 0f, 0f));
            player.GetComponent<AutoAttacker>().Engage(dummy);
            float timeout = Time.time + 5f;
            while (!dummy.Statuses.Has(StatusEffect.Frostbite) && Time.time < timeout)
            {
                yield return null;
            }

            Assert.IsTrue(dummy.Statuses.Has(StatusEffect.Frostbite), "gear on-hit procs reach the target");
            player.GetComponent<AutoAttacker>().Disengage();

            // --- Talk to every NPC (dialogs build without errors), then walk away (windows close)
            foreach (var npc in NpcActor.All)
            {
                npc.Interact();
                yield return null;
            }

            player.GetComponent<NavMotor>().Warp(player.Position + new Vector3(15f, 0f, -15f));
            yield return null;

            // --- Dead Branch summons a monster that hunts the reader
            int monsters = Object.FindObjectsByType<Monster>(FindObjectsSortMode.None).Length;
            player.Inventory.Add(ItemCatalog.DeadBranch, 1);
            Assert.IsTrue(player.UseItem(ItemCatalog.DeadBranch));
            Assert.AreEqual(monsters + 1, Object.FindObjectsByType<Monster>(FindObjectsSortMode.None).Length);

            // --- Storage (temporary character: an unsaved box) keeps refine through a round trip
            var storageTask = GameSession.Instance.LoadStorage();
            while (!storageTask.IsCompleted)
            {
                yield return null;
            }

            var storage = storageTask.Result;
            var sandals = player.Inventory.FindFirst("sandals");
            sandals.Refine = 4;
            Assert.IsTrue(StorageRules.TryDeposit(player.Inventory, storage, sandals, 1, out _));
            Assert.IsFalse(player.Inventory.Has("sandals"));
            Assert.IsTrue(StorageRules.TryWithdraw(storage, player.Inventory, storage[0], 1, player.Stats.WeightCapacity, player.CurrentWeight, out _));
            Assert.AreEqual(4, player.Inventory.FindFirst("sandals").Refine);
            yield return new WaitForSeconds(0.3f);
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
