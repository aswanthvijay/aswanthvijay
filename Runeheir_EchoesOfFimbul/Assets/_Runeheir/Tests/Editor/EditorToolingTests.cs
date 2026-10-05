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
