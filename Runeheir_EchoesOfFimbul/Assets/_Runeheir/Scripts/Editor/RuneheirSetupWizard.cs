using System.Collections.Generic;
using System.IO;
using Runeheir.Accounts;
using Runeheir.Cameras;
using Runeheir.Field;
using Runeheir.FrontEnd;
using Runeheir.Stats;
using Runeheir.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Runeheir.EditorTools
{
    /// <summary>
    /// Menu: Runeheir ▸ Setup ▸ Build Prototype Scenes.
    /// Generates RH_Login (login / server / character select) and RH_World (the isometric camera and the bootstrap that
    /// builds whichever of the 14 maps the character is on at load time) and registers both in Build Settings. Safe to
    /// re-run; it asks before overwriting. The Phase 2–4 hand-built field scene is retired (and removed) by this step.
    /// </summary>
    public static class RuneheirSetupWizard
    {
        public const string RootFolder = "Assets/_Runeheir";
        public const string ScenesFolder = RootFolder + "/Scenes";
        public const string LoginScenePath = ScenesFolder + "/RH_Login.unity";
        public const string WorldScenePath = ScenesFolder + "/" + MapCatalog.WorldScene + ".unity";

        /// <summary>The Phase 2–4 scene that RH_World replaces.</summary>
        public const string LegacyFieldScenePath = ScenesFolder + "/RH_Field_WhisperwoodPlains.unity";

        [MenuItem("Runeheir/Setup/Build Prototype Scenes", priority = 0)]
        public static void BuildPrototypeScenes()
        {
            BuildScenesAndOpen(LoginScenePath);
        }

        // Scene creation and opening throw in Play mode, so these menu items are disabled there.
        [MenuItem("Runeheir/Setup/Build Prototype Scenes", true)]
        [MenuItem("Runeheir/Setup/Open Login Scene", true)]
        [MenuItem("Runeheir/Setup/Open World Scene", true)]
        public static bool CanEditScenes()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private static void BuildScenesAndOpen(string sceneToOpen)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            bool exists = File.Exists(LoginScenePath) || File.Exists(WorldScenePath);
            if (exists && !EditorUtility.DisplayDialog("Runeheir", "Prototype scenes already exist. Rebuild and overwrite them?", "Rebuild", "Cancel"))
            {
                return;
            }

            try
            {
                GenerateScenes();
            }
            catch (IOException exception)
            {
                EditorUtility.DisplayDialog("Runeheir", exception.Message, "OK");
                return;
            }

            EditorSceneManager.OpenScene(sceneToOpen);
            EditorUtility.DisplayDialog(
                "Runeheir",
                "Created RH_Login and RH_World and added them to Build Settings.\n\n" +
                "• Press Play in RH_Login: register, create a character, Start. New heroes begin in Vigrid Haven.\n" +
                "• Or open RH_World and press Play for a temporary test character (pick its start map on the WorldBootstrap).\n\n" +
                "In game: left-click to move/attack/talk, F1–F10 hotkeys, A/S/E/Q windows, Ctrl+Tab minimap, Enter + @help (@warp, @bosses).",
                "Play!");
        }

        /// <summary>
        /// Non-interactive scene generation, used by the menu, the CI build (<see cref="RuneheirBuild"/>)
        /// and the editor tests. Returns the login and world scene paths.
        /// </summary>
        public static (string login, string world) GenerateScenes(string scenesFolder = ScenesFolder, bool registerInBuildSettings = true)
        {
            EnsureFolder(scenesFolder);
            string login = scenesFolder + "/RH_Login.unity";
            string world = scenesFolder + "/" + MapCatalog.WorldScene + ".unity";
            BuildLoginScene(login);
            BuildWorldScene(world);

            string legacy = scenesFolder + "/RH_Field_WhisperwoodPlains.unity";
            if (File.Exists(legacy))
            {
                AssetDatabase.DeleteAsset(legacy);
            }

            if (registerInBuildSettings)
            {
                RegisterScenesInBuildSettings(login, world, legacy);
            }

            AssetDatabase.SaveAssets();
            return (login, world);
        }

        [MenuItem("Runeheir/Setup/Open Login Scene", priority = 20)]
        public static void OpenLogin()
        {
            OpenScene(LoginScenePath);
        }

        [MenuItem("Runeheir/Setup/Open World Scene", priority = 21)]
        public static void OpenWorld()
        {
            OpenScene(WorldScenePath);
        }

        [MenuItem("Runeheir/Debug/Reveal Local Account Database", priority = 40)]
        public static void RevealDatabase()
        {
            string path = Path.Combine(Application.persistentDataPath, LocalAccountService.FileName);
            string existing = System.Array.Find(new[] { path, path + ".tmp", path + ".bak" }, File.Exists);
            if (existing != null)
            {
                EditorUtility.RevealInFinder(existing);
            }
            else
            {
                EditorUtility.DisplayDialog("Runeheir", $"No local account database yet.\nIt will be created at:\n{path}", "OK");
            }
        }

        [MenuItem("Runeheir/Debug/Delete Local Account Database", priority = 41)]
        public static void DeleteDatabase()
        {
            string path = Path.Combine(Application.persistentDataPath, LocalAccountService.FileName);
            string[] files = Directory.Exists(Application.persistentDataPath)
                ? Directory.GetFiles(Application.persistentDataPath, LocalAccountService.FileName + "*")
                : new string[0];
            if (files.Length == 0)
            {
                EditorUtility.DisplayDialog("Runeheir", "There is no local account database to delete.", "OK");
                return;
            }

            if (EditorUtility.DisplayDialog("Runeheir", $"Delete ALL local accounts and characters?\n{path}\n(plus its .bak/.tmp copies)", "Delete", "Cancel"))
            {
                foreach (string file in files)
                {
                    File.Delete(file);
                }

                Debug.Log($"[Runeheir] Deleted {files.Length} account file(s) in {Application.persistentDataPath}");
            }
        }

        // A running game holds the accounts in memory and would write them straight back.
        [MenuItem("Runeheir/Debug/Delete Local Account Database", true)]
        public static bool CanDeleteDatabase()
        {
            return !EditorApplication.isPlaying;
        }

        [MenuItem("Runeheir/Debug/Log ASPD Table", priority = 60)]
        public static void LogAspdTable()
        {
            var builder = new System.Text.StringBuilder("ASPD → attack play rate / interval (GDD: 150–197 → 1.0x–3.0x)\n");
            for (int aspd = (int)StatFormulas.MinAspd; aspd <= (int)StatFormulas.MaxAspd; aspd++)
            {
                if (aspd % 5 != 0 && aspd != (int)StatFormulas.MaxAspd)
                {
                    continue;
                }

                float interval = StatFormulas.AttackInterval(aspd);
                builder.AppendLine($"ASPD {aspd}: x{StatFormulas.AttackPlayRate(aspd):0.00} anim, swing {StatFormulas.SwingDuration(aspd):0.000}s, interval {interval:0.000}s = {1f / interval:0.00} hits/s");
            }

            Debug.Log(builder.ToString());
        }

        // ================================================================ login scene
        private static void BuildLoginScene(string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.03f, 0.05f, 0.1f);
            camera.cullingMask = 0; // the UI is a screen-space overlay; the preview stage has its own camera
            cameraGo.AddComponent<AudioListener>();

            CreateSun(new Vector3(40f, -30f, 0f), new Color(1f, 0.95f, 0.88f), 1.1f);
            new GameObject("FrontEnd").AddComponent<FrontEndController>();

            if (!EditorSceneManager.SaveScene(scene, path))
            {
                throw new IOException($"[Runeheir] Could not save {path} (read-only or locked?).");
            }
        }

        // ================================================================ world scene
        /// <summary>
        /// The one scene every map loads into: a camera with the isometric rig, a sun, and the bootstrap that generates the
        /// character's map (ground, props, NavMesh, NPCs, portals, monsters) at load time. Played directly, it starts a
        /// temporary character in Vigrid Haven.
        /// </summary>
        private static void BuildWorldScene(string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateSun(new Vector3(50f, -35f, 0f), new Color(1f, 0.93f, 0.80f), 1.3f);

            var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraGo.AddComponent<Camera>();
            camera.fieldOfView = 30f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 250f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            cameraGo.AddComponent<AudioListener>();
            var rotation = Quaternion.Euler(45f, 45f, 0f);
            cameraGo.transform.SetPositionAndRotation(Vector3.up - rotation * Vector3.forward * 18f, rotation);
            var rig = cameraGo.AddComponent<IsometricCameraRig>();

            var bootstrap = new GameObject("WorldBootstrap").AddComponent<FieldBootstrap>();
            var settings = new SerializedObject(bootstrap);
            settings.FindProperty("generateMap").boolValue = true;
            settings.FindProperty("mapId").stringValue = MapCatalog.StartingMapId;
            settings.FindProperty("cameraRig").objectReferenceValue = rig;
            settings.ApplyModifiedPropertiesWithoutUndo();

            if (!EditorSceneManager.SaveScene(scene, path))
            {
                throw new IOException($"[Runeheir] Could not save {path} (read-only or locked?).");
            }
        }

        // ================================================================ helpers
        private static void CreateSun(Vector3 euler, Color color, float intensity)
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = color;
            sun.intensity = intensity;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(euler);
            RenderSettings.sun = sun;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static void RegisterScenesInBuildSettings(string loginPath, string worldPath, string retiredPath)
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(loginPath, true),
                new EditorBuildSettingsScene(worldPath, true),
            };
            foreach (var existing in EditorBuildSettings.scenes)
            {
                if (existing.path != loginPath && existing.path != worldPath && existing.path != retiredPath)
                {
                    scenes.Add(existing);
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void OpenScene(string path)
        {
            if (!File.Exists(path))
            {
                if (EditorUtility.DisplayDialog("Runeheir", "The prototype scenes don't exist yet. Build them now?", "Build", "Cancel"))
                {
                    BuildScenesAndOpen(path);
                }

                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(path);
            }
        }
    }
}
