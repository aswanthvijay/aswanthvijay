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
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.SceneManagement;
using UnityEngine;

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
        public void SceneWizard_GeneratesPlayableLoginAndFieldScenes()
        {
            var (login, field) = RuneheirSetupWizard.GenerateScenes(TempRoot + "/Scenes", TempRoot + "/Materials", registerInBuildSettings: false);
            Assert.IsTrue(File.Exists(login), login);
            Assert.IsTrue(File.Exists(field), field);

            EditorSceneManager.OpenScene(field, OpenSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<FieldBootstrap>();
            Assert.IsNotNull(bootstrap, "FieldBootstrap");
            var settings = new SerializedObject(bootstrap);
            Assert.IsNotNull(settings.FindProperty("savePoint").objectReferenceValue, "save point wired");
            Assert.IsNotNull(settings.FindProperty("cameraRig").objectReferenceValue, "camera rig wired");
            Assert.IsNotNull(Object.FindFirstObjectByType<RuntimeNavMeshBaker>(), "NavMesh baker");
            Assert.IsNotNull(Camera.main, "main camera tagged");

            var spawners = Object.FindObjectsByType<MonsterSpawner>(FindObjectsSortMode.None);
            Assert.GreaterOrEqual(spawners.Length, 9, "6 monster spawners + 3 training dummies");
            foreach (var spawner in spawners)
            {
                Assert.IsNotNull(MonsterCatalog.Get(spawner.MonsterId), spawner.MonsterId);
            }

            Assert.Greater(Object.FindObjectsByType<NavBlocker>(FindObjectsSortMode.None).Length, 20, "props carve the NavMesh");

            EditorSceneManager.OpenScene(login, OpenSceneMode.Single);
            Assert.IsNotNull(Object.FindFirstObjectByType<FrontEndController>(), "FrontEndController");
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
