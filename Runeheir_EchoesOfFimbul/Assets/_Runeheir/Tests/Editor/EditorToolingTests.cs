using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Runeheir.Combat;
using Runeheir.EditorTools;
using Runeheir.Field;
using Runeheir.FrontEnd;
using Runeheir.Monsters;
using Runeheir.Movement;
using Runeheir.Visuals;
using Runeheir.World;
using Runeheir.WorldBuilding;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Runeheir.Tests
{
    /// <summary>Runs inside the Unity Editor (Test Runner ▸ EditMode, or CI): scene generator + toon shader.</summary>
    public sealed class EditorToolingTests
    {
        private const string TempRoot = "Assets/_RuneheirTestTemp";

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(TempRoot);
        }

        [Test]
        public void SceneWizard_GeneratesLoginAndWorldScenes()
        {
            var (login, world) = RuneheirSetupWizard.GenerateScenes(TempRoot + "/Scenes", registerInBuildSettings: false);
            Assert.IsTrue(File.Exists(login), login);
            Assert.IsTrue(File.Exists(world), world);
            StringAssert.EndsWith(MapCatalog.WorldScene + ".unity", world);

            EditorSceneManager.OpenScene(world, OpenSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<FieldBootstrap>();
            Assert.IsNotNull(bootstrap, "WorldBootstrap");
            Assert.IsTrue(bootstrap.GeneratesMap, "RH_World builds the character's map at load time");
            Assert.AreEqual(MapCatalog.StartingMapId, bootstrap.MapId, "played directly, it starts in Vigrid Haven");
            var settings = new SerializedObject(bootstrap);
            Assert.IsNotNull(settings.FindProperty("cameraRig").objectReferenceValue, "camera rig wired");
            Assert.IsNotNull(Camera.main, "main camera tagged");
            Assert.IsNull(Object.FindFirstObjectByType<RuntimeNavMeshBaker>(), "no hand-built environment: each map brings its own");
            Assert.IsEmpty(Object.FindObjectsByType<MonsterSpawner>(FindObjectsSortMode.None), "spawners come from the map data");

            EditorSceneManager.OpenScene(login, OpenSceneMode.Single);
            Assert.IsNotNull(Object.FindFirstObjectByType<FrontEndController>(), "FrontEndController");
        }

        /// <summary>Every one of the 14 maps builds, bakes, and lets you walk from its save point to every portal, NPC and boss.</summary>
        [Test]
        public void WorldBuilder_BuildsEveryMap_WithAConnectedNavMesh()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            foreach (var map in MapCatalog.All)
            {
                NavMesh.RemoveAllNavMeshData();
                var world = WorldBuilder.Build(map, null);
                try
                {
                    Assert.Greater(Object.FindObjectsByType<NavBlocker>(FindObjectsSortMode.None).Length, 0, $"{map.Id}: scenery carves the NavMesh");
                    Assert.IsTrue(NavMesh.SamplePosition(world.SavePoint, out NavMeshHit save, 1.5f, NavMesh.AllAreas), $"{map.Id}: save point on the NavMesh");
                    var path = new NavMeshPath();
                    var targets = new List<(Vector3 point, string what)>();
                    targets.AddRange(world.Layout.Portals.Select(p => (WorldBuilder.ToWorld(p.At), "portal " + p.Portal.Id)));
                    targets.AddRange(world.Layout.Portals.Select(p => (WorldBuilder.ToWorld(p.Arrival), "arrival " + p.Portal.Id)));
                    targets.AddRange(world.Layout.Npcs.Select(n => (WorldBuilder.ToWorld(n.At), n.Npc.Name)));
                    targets.AddRange(world.Layout.Bosses.Select(b => (WorldBuilder.ToWorld(b.At), b.Boss.MonsterId)));
                    foreach (var (point, what) in targets)
                    {
                        Assert.IsTrue(NavMesh.SamplePosition(point, out NavMeshHit hit, 2.5f, NavMesh.AllAreas), $"{map.Id}: no NavMesh at the {what}");
                        Assert.IsTrue(NavMesh.CalculatePath(save.position, hit.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete,
                            $"{map.Id}: can't walk from the save point to the {what}");
                    }
                }
                finally
                {
                    Object.DestroyImmediate(world.Root.gameObject);
                }
            }

            NavMesh.RemoveAllNavMeshData();
        }

        [Test]
        public void AnimatorBuilder_WiresEveryBridgeParameterAndSkillMotion()
        {
            var controller = RuneheirAnimatorBuilder.Build(TempRoot + "/Animation");
            Assert.IsNotNull(controller);
            Assert.IsEmpty(RuneheirAnimatorBuilder.MissingParameters(controller));

            var states = controller.layers[0].stateMachine.states.Select(s => s.state).ToList();
            var attack = states.Single(s => s.name == "Attack");
            Assert.IsTrue(attack.speedParameterActive, "ASPD scales the attack clip");
            Assert.AreEqual(RuneheirAnimatorBuilder.AttackSpeed, attack.speedParameter);
            foreach (Runeheir.Skills.SkillMotion motion in System.Enum.GetValues(typeof(Runeheir.Skills.SkillMotion)))
            {
                if (motion != Runeheir.Skills.SkillMotion.None)
                {
                    Assert.IsTrue(states.Any(s => s.name == "Skill_" + motion), motion.ToString());
                }
            }

            Assert.IsTrue(states.Any(s => s.name == "Stagger") && states.Any(s => s.name == "Dead") && states.Any(s => s.name == "Cast"));
            Assert.AreEqual("Locomotion", controller.layers[0].stateMachine.defaultState.name);
            foreach (var transition in controller.layers[0].stateMachine.anyStateTransitions)
            {
                if (transition.destinationState.name != "Dead")
                {
                    Assert.IsTrue(transition.conditions.Any(c => c.parameter == RuneheirAnimatorBuilder.Dead && c.mode == UnityEditor.Animations.AnimatorConditionMode.IfNot),
                        $"{transition.destinationState.name} must not fire while dead");
                }
            }

            // Rebuilding keeps the asset (and its GUID) instead of making a new one.
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(controller));
            var rebuilt = RuneheirAnimatorBuilder.Build(TempRoot + "/Animation");
            Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(rebuilt)));
            Assert.AreEqual(states.Count, rebuilt.layers[0].stateMachine.states.Length, "no duplicate states after a rebuild");
        }

        [Test]
        public void ToonShader_ImportsWithoutErrors()
        {
            var shader = Shader.Find(RuntimeMaterials.ToonShaderName);
            Assert.IsNotNull(shader, "Runeheir/Toon shader not found (it should live in a Resources folder)");

            string[] errors = ShaderUtil.GetShaderMessages(shader)
                .Where(m => m.severity == ShaderCompilerMessageSeverity.Error)
                .Select(m => $"{m.message} ({m.file}:{m.line}, {m.platform})")
                .ToArray();
            Assert.IsEmpty(errors, string.Join("\n", errors));
            Assert.IsFalse(ShaderUtil.ShaderHasError(shader), "ShaderUtil reports errors for Runeheir/Toon");
        }
    }
}
