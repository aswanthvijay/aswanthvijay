using System.Collections;
using System.Linq;
using NUnit.Framework;
using Runeheir.Characters;
using Runeheir.Combat;
using Runeheir.Field;
using Runeheir.Items;
using Runeheir.Monsters;
using Runeheir.Movement;
using Runeheir.Player;
using Runeheir.Session;
using Runeheir.Visuals;
using Runeheir.World;
using Runeheir.WorldBuilding;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Runeheir.Tests
{
    /// <summary>Phase 5 play-mode smoke tests: generated maps, their NPCs and portals, boss fights and monster skills.</summary>
    public sealed class WorldSmokeTests
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

            WorldState.Bosses.RespawnScale = 1f;
            WorldState.Bosses.ResetAll();
            yield return null;
        }

        private static IEnumerator LoadMap(string mapId)
        {
            new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
            var bootstrap = new GameObject("WorldBootstrap").AddComponent<FieldBootstrap>();
            bootstrap.GeneratesMap = true;
            bootstrap.MapId = mapId;
            yield return null;
            yield return null;
        }

        private static PlayerCharacter ToughPlayer()
        {
            var player = PlayerCharacter.Local;
            player.Progression.SetBaseLevel(255);
            player.Progression.SetAllStats(200);
            player.Heal(player.MaxHp, showNumber: false);
            return player;
        }

        [UnityTest]
        public IEnumerator Vigrid_BuildsWithItsNpcsPortalsAndMinimap()
        {
            yield return LoadMap(MapCatalog.VigridHaven);
            var map = MapCatalog.Get(MapCatalog.VigridHaven);
            var player = PlayerCharacter.Local;
            Assert.IsNotNull(player, "the bootstrap spawned the player");
            Assert.IsTrue(player.GetComponent<NavMeshAgent>().isOnNavMesh, "on the generated NavMesh");
            Assert.AreEqual(MapCatalog.VigridHaven, FieldContext.MapId);
            Assert.IsNotNull(FieldContext.Layout);
            Assert.AreEqual(map.Npcs.Count, NpcActor.All.Count, "every Vigrid NPC is placed");
            Assert.AreEqual(map.Portals.Count, WarpPortal.All.Count, "south gate and hall door");
            Assert.IsNotNull(GameObject.Find("Minimap"), "minimap built from the layout");
            Assert.IsNotNull(Object.FindFirstObjectByType<Light>(), "the theme's sun");

            // No branches in the capital.
            player.Inventory.Add(ItemCatalog.DeadBranch, 1);
            Assert.IsFalse(player.UseItem(ItemCatalog.DeadBranch), "branches are for the Hall of Branches");
            Assert.AreEqual(1, player.Inventory.Count(ItemCatalog.DeadBranch), "the branch is kept");

            // Every NPC's menu builds (merchants by shop, Brokk, the courier's save/teleport, Sigrun).
            foreach (var npc in NpcActor.All.ToList())
            {
                npc.Interact();
                yield return null;
            }

            // Saving with the courier changes where you revive.
            player.Record.SaveMapId = MapCatalog.WhisperwoodPlains;
            player.Record.SaveMapId = MapCatalog.VigridHaven;
            Assert.AreEqual(MapCatalog.VigridHaven, player.Record.SaveMapId);
        }

        [UnityTest]
        public IEnumerator Lyngvi_Fenrir_PhasesSummonsInterruptsMvpAndTomb()
        {
            yield return LoadMap(MapCatalog.Lyngvi);
            var player = ToughPlayer();
            var lair = Object.FindFirstObjectByType<BossSpawner>();
            Assert.IsNotNull(lair, "Lyngvi has Fenrir's lair");
            var fenrir = lair.Boss;
            Assert.IsNotNull(fenrir, "Fenrir is up when nobody has slain him");
            Assert.IsTrue(fenrir.Definition.IsMvp);

            // Fight him up close: from the save point (40 m away) he'd give up the chase, leash home and reset.
            Assert.IsTrue(NavMesh.SamplePosition(lair.transform.position + new Vector3(0f, 0f, -6f), out NavMeshHit near, 4f, NavMesh.AllAreas));
            player.GetComponent<NavMotor>().Warp(near.position);
            yield return null;

            // Phase 1 at 70%: enraged.
            fenrir.ReceiveDamage(DamageResult.Fixed(Mathf.CeilToInt(fenrir.MaxHp * 0.35f)), player, physicalMelee: false);
            Assert.AreEqual(1, fenrir.Phase);
            Assert.IsTrue(fenrir.Buffs.Has(MonsterBuffs.Enraged));

            // Summons: Sköll answers the call.
            Assert.IsTrue(fenrir.ForceSkill("call_skoll", player));
            yield return new WaitForSeconds(2f);
            Assert.AreEqual(1, fenrir.Minions.Count(m => m != null && !m.IsDead));
            Assert.AreEqual(1, fenrir.Phase, "still in phase 1: he didn't leash and reset");

            // A telegraphed breath, cut short by a stagger.
            Assert.IsTrue(fenrir.ForceSkill("moon_eater_breath", player));
            Assert.IsTrue(fenrir.IsCasting);
            Assert.IsNotNull(Object.FindFirstObjectByType<SkillTelegraph>(), "the landing circle shows");
            fenrir.ApplyStatus(StatusEffect.Stagger, 1f);
            Assert.IsFalse(fenrir.IsCasting, "staggering a caster interrupts it");
            yield return null;

            // The kill: MVP rewards, a tombstone, the summons fade.
            int before = player.Inventory.Count("gleipnir_thread");
            fenrir.ReceiveDamage(DamageResult.Fixed(fenrir.Hp), player, physicalMelee: false);
            Assert.IsTrue(fenrir.IsDead);
            Assert.AreEqual(player.DisplayName, fenrir.MvpName);
            Assert.Greater(player.Inventory.Count("gleipnir_thread"), before, "5x drop rate makes the first MVP drop certain");
            yield return null;
            yield return null;
            Assert.IsNull(lair.Boss);
            Assert.IsFalse(WorldState.Bosses.Status(MapCatalog.Lyngvi, "fenrir").Alive);
            Assert.IsNotNull(lair.transform.Find("Tomb_fenrir"), "a tombstone marks the lair");
            Assert.IsFalse(Object.FindObjectsByType<Monster>(FindObjectsSortMode.None).Any(m => m.Definition.Id == "skoll" && !m.IsDead),
                "Fenrir's wolves vanish with him");

            // The timer runs out (GM reset): he's back.
            WorldState.Bosses.ResetAll();
            yield return new WaitForSeconds(1.5f);
            Assert.IsNotNull(lair.Boss, "Fenrir returns when his timer is up");
            Assert.IsNull(lair.transform.Find("Tomb_fenrir"));
        }

        [UnityTest]
        public IEnumerator HallOfBranches_TelegraphedBlasts_WeaponBreakAndRepair_AndBranches()
        {
            yield return LoadMap(MapCatalog.HallOfBranches);
            var player = ToughPlayer();

            // A Jotun Brawler's Earthshaker: a circle around it, then the blast.
            var spot = player.Position + new Vector3(2.5f, 0f, 0f);
            NavMesh.SamplePosition(spot, out NavMeshHit hit, 3f, NavMesh.AllAreas);
            var brawler = EntityFactory.CreateMonster(MonsterCatalog.Get("jotun_brawler"), hit.position);
            yield return null;
            int hp = player.Hp;
            Assert.IsTrue(brawler.ForceSkill("earthshaker", player));
            Assert.IsNotNull(Object.FindFirstObjectByType<SkillTelegraph>());
            yield return new WaitForSeconds(2f);
            Assert.IsFalse(brawler.IsCasting);
            Assert.Less(player.Hp, hp, "standing in the circle hurts");
            Object.Destroy(brawler.gameObject);

            // Weapon break and Brokk's repair.
            Assert.IsTrue(player.Gear.HasWeapon);
            Assert.IsTrue(player.TryBreakWeapon(100f));
            Assert.IsFalse(player.Gear.HasWeapon, "a broken weapon does nothing");
            player.Record.Zeny = 1000000;
            Assert.IsTrue(RepairRules.TryRepairAll(player.Record, out _, out _));
            player.Equipment.NotifyChanged();
            Assert.IsTrue(player.Gear.HasWeapon);

            // Branches work here.
            player.Inventory.Add(ItemCatalog.DeadBranch, 1);
            Assert.IsTrue(player.UseItem(ItemCatalog.DeadBranch));
            yield return null;
            Assert.IsTrue(Object.FindObjectsByType<Monster>(FindObjectsSortMode.None).Any(m => !m.IsDead), "something crawled out");
        }
    }
}
