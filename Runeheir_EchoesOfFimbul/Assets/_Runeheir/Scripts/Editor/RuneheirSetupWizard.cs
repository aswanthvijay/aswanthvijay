using System.Collections.Generic;
using System.IO;
using Runeheir.Accounts;
using Runeheir.Cameras;
using Runeheir.Combat;
using Runeheir.Field;
using Runeheir.FrontEnd;
using Runeheir.Movement;
using Runeheir.Stats;
using Runeheir.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Runeheir.EditorTools
{
    /// <summary>
    /// Menu: Runeheir ▸ Setup ▸ Build Prototype Scenes.
    /// Generates RH_Login (login / server / character select) and RH_Field_WhisperwoodPlains
    /// (isometric camera, runtime-baked NavMesh, monster spawners, training dummies) and registers
    /// both in Build Settings. Safe to re-run; it asks before overwriting.
    /// </summary>
    public static class RuneheirSetupWizard
    {
        public const string RootFolder = "Assets/_Runeheir";
        public const string ScenesFolder = RootFolder + "/Scenes";
        public const string MaterialsFolder = RootFolder + "/Generated/Materials";
        public const string LoginScenePath = ScenesFolder + "/RH_Login.unity";
        public const string FieldScenePath = ScenesFolder + "/RH_Field_WhisperwoodPlains.unity";

        private const float GroundSize = 140f;

        /// <summary>Ink outline (pixels at 1080p) for props; ground decals get none.</summary>
        private const float PropOutline = 1.5f;

        private static readonly Color Grass = new Color(0.42f, 0.63f, 0.30f);
        private static readonly Color GrassDark = new Color(0.33f, 0.53f, 0.25f);
        private static readonly Color Dirt = new Color(0.56f, 0.45f, 0.31f);
        private static readonly Color Stone = new Color(0.55f, 0.56f, 0.58f);
        private static readonly Color Bark = new Color(0.36f, 0.25f, 0.16f);
        private static readonly Color Birch = new Color(0.90f, 0.88f, 0.82f);
        private static readonly Color PineGreen = new Color(0.17f, 0.36f, 0.22f);
        private static readonly Color AutumnGold = new Color(0.91f, 0.67f, 0.22f);
        private static readonly Color RuneGlow = new Color(0.45f, 0.85f, 1f);
        private static readonly Color Wood = new Color(0.55f, 0.40f, 0.25f);
        private static readonly Color Cloth = new Color(0.86f, 0.82f, 0.72f);

        private static readonly Dictionary<string, Material> MaterialCache = new Dictionary<string, Material>();
        private static string s_materialsFolder = MaterialsFolder;

        [MenuItem("Runeheir/Setup/Build Prototype Scenes", priority = 0)]
        public static void BuildPrototypeScenes()
        {
            BuildScenesAndOpen(LoginScenePath);
        }

        // Scene creation and opening throw in Play mode, so these menu items are disabled there.
        [MenuItem("Runeheir/Setup/Build Prototype Scenes", true)]
        [MenuItem("Runeheir/Setup/Open Login Scene", true)]
        [MenuItem("Runeheir/Setup/Open Field Scene", true)]
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

            bool exists = File.Exists(LoginScenePath) || File.Exists(FieldScenePath);
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
                "Created RH_Login and RH_Field_WhisperwoodPlains and added them to Build Settings.\n\n" +
                "• Press Play in RH_Login: register, create a character, Start.\n" +
                "• Or open the field scene and press Play for a temporary test character.\n\n" +
                "In game: left-click to move/attack, F1–F10 hotkeys, A/S/E windows, Enter + @help for test commands.",
                "Play!");
        }

        /// <summary>
        /// Non-interactive scene generation, used by the menu, the CI build (<see cref="RuneheirBuild"/>)
        /// and the editor tests. Returns the login and field scene paths.
        /// </summary>
        public static (string login, string field) GenerateScenes(
            string scenesFolder = ScenesFolder,
            string materialsFolder = MaterialsFolder,
            bool registerInBuildSettings = true)
        {
            MaterialCache.Clear();
            s_materialsFolder = materialsFolder;
            EnsureFolder(scenesFolder);
            EnsureFolder(materialsFolder);

            string login = scenesFolder + "/RH_Login.unity";
            string field = scenesFolder + "/RH_Field_WhisperwoodPlains.unity";
            BuildLoginScene(login);
            BuildFieldScene(field);
            if (registerInBuildSettings)
            {
                RegisterScenesInBuildSettings(login, field);
            }

            AssetDatabase.SaveAssets();
            return (login, field);
        }

        [MenuItem("Runeheir/Setup/Open Login Scene", priority = 20)]
        public static void OpenLogin()
        {
            OpenScene(LoginScenePath);
        }

        [MenuItem("Runeheir/Setup/Open Field Scene", priority = 21)]
        public static void OpenField()
        {
            OpenScene(FieldScenePath);
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

        // ================================================================ field scene
        private static void BuildFieldScene(string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var random = new System.Random(1337);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.70f, 0.82f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.58f, 0.52f);
            RenderSettings.ambientGroundColor = new Color(0.30f, 0.28f, 0.24f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.78f, 0.84f, 0.90f);
            RenderSettings.fogStartDistance = 45f;
            RenderSettings.fogEndDistance = 140f;

            CreateSun(new Vector3(50f, -35f, 0f), new Color(1f, 0.93f, 0.80f), 1.3f);

            // Camera with the GDD isometric rig.
            var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraGo.AddComponent<Camera>();
            camera.fieldOfView = 30f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 250f;
            camera.clearFlags = CameraClearFlags.Skybox;
            cameraGo.AddComponent<AudioListener>();
            var rotation = Quaternion.Euler(45f, 45f, 0f);
            cameraGo.transform.SetPositionAndRotation(Vector3.up - rotation * Vector3.forward * 18f, rotation);
            var rig = cameraGo.AddComponent<IsometricCameraRig>();

            // Environment: everything with a collider under here becomes NavMesh at load time.
            var environment = new GameObject("Environment");
            var baker = environment.AddComponent<RuntimeNavMeshBaker>();
            var bakerSettings = new SerializedObject(baker);
            bakerSettings.FindProperty("boundsSize").vector3Value = new Vector3(GroundSize + 10f, 40f, GroundSize + 10f);
            bakerSettings.ApplyModifiedPropertiesWithoutUndo();

            var ground = Block(environment.transform, "Ground", PrimitiveType.Cube, new Vector3(0f, -0.5f, 0f), new Vector3(GroundSize, 1f, GroundSize), Grass, keepCollider: true, outline: 0f);
            SetStatic(ground);

            var decor = new GameObject("Decor").transform;
            decor.SetParent(environment.transform, false);
            Block(decor, "PathNS", PrimitiveType.Cube, new Vector3(0f, 0.01f, 0f), new Vector3(4.5f, 0.02f, GroundSize - 6f), Dirt, keepCollider: false, outline: 0f);
            Block(decor, "PathEW", PrimitiveType.Cube, new Vector3(0f, 0.012f, 0f), new Vector3(GroundSize - 6f, 0.02f, 4.5f), Dirt, keepCollider: false, outline: 0f);
            for (int i = 0; i < 26; i++)
            {
                var position = RandomPoint(random, GroundSize * 0.45f);
                float size = Range(random, 4f, 11f);
                // The two colors sit at different heights (tops 0.013 / 0.010, below the paths) so overlapping
                // patches never z-fight; same-color overlaps share a material and don't show.
                bool dark = i % 2 == 0;
                Block(decor, "GrassPatch", PrimitiveType.Cylinder, new Vector3(position.x, dark ? 0.008f : 0.005f, position.z), new Vector3(size, 0.005f, size * Range(random, 0.6f, 1f)),
                    dark ? GrassDark : new Color(0.48f, 0.68f, 0.32f), keepCollider: false, outline: 0f);
            }

            var spawns = new (string id, Vector3 position, int count, float radius)[]
            {
                ("rune_spore", new Vector3(18f, 0f, 16f), 8, 8f),
                ("field_beetle", new Vector3(-20f, 0f, 18f), 6, 8f),
                ("toxic_spore", new Vector3(28f, 0f, -20f), 5, 7f),
                ("horned_grazer", new Vector3(-26f, 0f, -24f), 5, 9f),
                ("forest_imp", new Vector3(42f, 0f, 34f), 5, 8f),
                ("wood_sprite", new Vector3(-46f, 0f, -44f), 4, 8f),
            };

            var avoid = new List<(Vector3 center, float radius)> { (Vector3.zero, 14f) };
            foreach (var spawn in spawns)
            {
                avoid.Add((spawn.position, spawn.radius + 3f));
            }

            var props = new GameObject("Props").transform;
            props.SetParent(environment.transform, false);
            PlaceTrees(props, random, avoid);
            PlaceRocks(props, random, avoid);
            PlaceRunestones(props, random, avoid);
            Windmill(props, new Vector3(-42f, 0f, 40f), 25f);
            Windmill(props, new Vector3(50f, 0f, -46f), -40f);

            // Save point with a campfire.
            var savePoint = new GameObject("SavePoint").transform;
            savePoint.position = new Vector3(0f, 0f, -3f);
            Campfire(decor, new Vector3(3f, 0f, -6f));

            // Spawners.
            var spawnRoot = new GameObject("Spawns").transform;
            foreach (var spawn in spawns)
            {
                Spawner(spawnRoot, spawn.id, spawn.position, spawn.count, spawn.radius);
            }

            Spawner(spawnRoot, "training_dummy", new Vector3(8f, 0f, 4f), 1, 0f);
            Spawner(spawnRoot, "training_dummy", new Vector3(10.5f, 0f, 1f), 1, 0f);
            Spawner(spawnRoot, "training_dummy", new Vector3(11f, 0f, -2.5f), 1, 0f);

            // Bootstrap.
            var bootstrapGo = new GameObject("FieldBootstrap");
            var bootstrap = bootstrapGo.AddComponent<FieldBootstrap>();
            var settings = new SerializedObject(bootstrap);
            settings.FindProperty("savePoint").objectReferenceValue = savePoint;
            settings.FindProperty("cameraRig").objectReferenceValue = rig;
            settings.FindProperty("mapSize").vector3Value = new Vector3(GroundSize - 10f, 20f, GroundSize - 10f);
            settings.ApplyModifiedPropertiesWithoutUndo();

            if (!EditorSceneManager.SaveScene(scene, path))
            {
                throw new IOException($"[Runeheir] Could not save {path} (read-only or locked?).");
            }
        }

        private static void PlaceTrees(Transform parent, System.Random random, List<(Vector3 center, float radius)> avoid)
        {
            int placed = 0;
            for (int attempt = 0; attempt < 400 && placed < 55; attempt++)
            {
                var position = RandomPoint(random, GroundSize * 0.47f);
                if (!IsFree(position, avoid, 2.5f))
                {
                    continue;
                }

                avoid.Add((position, 2.5f));
                placed++;
                var tree = new GameObject("Tree").transform;
                tree.SetParent(parent, false);
                tree.position = position;
                tree.rotation = Quaternion.Euler(0f, Range(random, 0f, 360f), 0f);
                float scale = Range(random, 0.85f, 1.35f);
                tree.localScale = Vector3.one * scale;

                bool birch = random.NextDouble() < 0.45;
                var trunk = Block(tree, "Trunk", PrimitiveType.Cylinder, new Vector3(0f, 1.5f, 0f), new Vector3(0.38f, 1.5f, 0.38f), birch ? Birch : Bark, keepCollider: true);
                trunk.AddComponent<NavBlocker>();
                SetStatic(trunk);

                if (birch)
                {
                    Color leaves = random.NextDouble() < 0.6 ? AutumnGold : new Color(0.55f, 0.72f, 0.28f);
                    Block(tree, "Canopy", PrimitiveType.Sphere, new Vector3(0f, 3.7f, 0f), new Vector3(2.6f, 2.4f, 2.6f), leaves, keepCollider: false);
                    Block(tree, "Canopy2", PrimitiveType.Sphere, new Vector3(0.6f, 3.1f, 0.4f), new Vector3(1.6f, 1.5f, 1.6f), leaves * 0.92f, keepCollider: false);
                }
                else
                {
                    Block(tree, "Pine1", PrimitiveType.Sphere, new Vector3(0f, 2.7f, 0f), new Vector3(2.8f, 1.5f, 2.8f), PineGreen, keepCollider: false);
                    Block(tree, "Pine2", PrimitiveType.Sphere, new Vector3(0f, 3.7f, 0f), new Vector3(2.1f, 1.3f, 2.1f), PineGreen * 1.1f, keepCollider: false);
                    Block(tree, "Pine3", PrimitiveType.Sphere, new Vector3(0f, 4.6f, 0f), new Vector3(1.2f, 1.1f, 1.2f), PineGreen * 1.2f, keepCollider: false);
                }
            }
        }

        private static void PlaceRocks(Transform parent, System.Random random, List<(Vector3 center, float radius)> avoid)
        {
            int placed = 0;
            for (int attempt = 0; attempt < 200 && placed < 22; attempt++)
            {
                var position = RandomPoint(random, GroundSize * 0.47f);
                if (!IsFree(position, avoid, 2f))
                {
                    continue;
                }

                avoid.Add((position, 2f));
                placed++;
                var rock = Block(parent, "Rock", PrimitiveType.Sphere, position + Vector3.up * 0.25f,
                    new Vector3(Range(random, 1.4f, 3f), Range(random, 0.8f, 1.5f), Range(random, 1.2f, 2.6f)), Stone * Range(random, 0.85f, 1.1f), keepCollider: false);
                rock.transform.rotation = Quaternion.Euler(0f, Range(random, 0f, 360f), 0f);

                // A SphereCollider ignores non-uniform scale (it uses the largest axis), so it would be far bigger
                // than the squashed rock. A convex MeshCollider follows the visible shape for clicks and the NavMesh.
                var rockCollider = rock.AddComponent<MeshCollider>();
                rockCollider.sharedMesh = rock.GetComponent<MeshFilter>().sharedMesh;
                rockCollider.convex = true;
                rock.AddComponent<NavBlocker>();
                SetStatic(rock);
            }
        }

        private static void PlaceRunestones(Transform parent, System.Random random, List<(Vector3 center, float radius)> avoid)
        {
            int placed = 0;
            for (int attempt = 0; attempt < 120 && placed < 8; attempt++)
            {
                var position = RandomPoint(random, GroundSize * 0.4f);
                if (!IsFree(position, avoid, 2f))
                {
                    continue;
                }

                avoid.Add((position, 2f));
                placed++;
                var stone = Block(parent, "Runestone", PrimitiveType.Cube, position + Vector3.up * 1.2f, new Vector3(0.9f, 2.6f, 0.4f), Stone * 0.85f, keepCollider: true);
                stone.transform.rotation = Quaternion.Euler(Range(random, -4f, 4f), Range(random, 0f, 360f), Range(random, -4f, 4f));
                stone.AddComponent<NavBlocker>();
                SetStatic(stone);
                Block(stone.transform, "Glyphs", PrimitiveType.Cube, new Vector3(0f, 0.05f, 0.52f), new Vector3(0.35f, 0.75f, 0.05f), RuneGlow, keepCollider: false, outline: 0f, emission: RuneGlow * 1.6f);
            }
        }

        private static void Windmill(Transform parent, Vector3 position, float yaw)
        {
            var root = new GameObject("Windmill").transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));

            var tower = Block(root, "Tower", PrimitiveType.Cylinder, new Vector3(0f, 4f, 0f), new Vector3(3.2f, 4f, 3.2f), Cloth, keepCollider: true);
            tower.AddComponent<NavBlocker>();
            SetStatic(tower);
            Block(root, "Roof", PrimitiveType.Cylinder, new Vector3(0f, 8.4f, 0f), new Vector3(3.6f, 0.5f, 3.6f), new Color(0.5f, 0.22f, 0.16f), keepCollider: false);
            Block(root, "Cap", PrimitiveType.Sphere, new Vector3(0f, 8.9f, 0f), new Vector3(2.6f, 1.6f, 2.6f), new Color(0.5f, 0.22f, 0.16f), keepCollider: false);

            var hub = new GameObject("Sails").transform;
            hub.SetParent(root, false);
            hub.localPosition = new Vector3(0f, 7f, 1.9f);
            hub.gameObject.AddComponent<Spinner>().DegreesPerSecond = new Vector3(0f, 0f, 25f);
            for (int i = 0; i < 4; i++)
            {
                var blade = Block(hub, "Blade", PrimitiveType.Cube, Vector3.zero, new Vector3(0.9f, 5.5f, 0.08f), Wood, keepCollider: false).transform;
                blade.localRotation = Quaternion.Euler(0f, 0f, i * 90f);
                blade.localPosition = blade.localRotation * new Vector3(0f, 2.9f, 0f);
            }
        }

        private static void Campfire(Transform parent, Vector3 position)
        {
            var root = new GameObject("Campfire").transform;
            root.SetParent(parent, false);
            root.position = position;
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 2f / 8f;
                Block(root, "Stone", PrimitiveType.Sphere, new Vector3(Mathf.Cos(angle) * 0.75f, 0.12f, Mathf.Sin(angle) * 0.75f), new Vector3(0.35f, 0.25f, 0.35f), Stone, keepCollider: false);
            }

            Block(root, "Logs", PrimitiveType.Cylinder, new Vector3(0f, 0.15f, 0f), new Vector3(0.18f, 0.5f, 0.18f), Bark, keepCollider: false).transform.localRotation = Quaternion.Euler(80f, 30f, 0f);
            Block(root, "Flame", PrimitiveType.Sphere, new Vector3(0f, 0.45f, 0f), new Vector3(0.45f, 0.7f, 0.45f), new Color(1f, 0.55f, 0.15f), keepCollider: false, outline: 0f, emission: new Color(1.4f, 0.6f, 0.15f));
            var light = new GameObject("FireLight").AddComponent<Light>();
            light.transform.SetParent(root, false);
            light.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            light.type = LightType.Point;
            light.color = new Color(1f, 0.6f, 0.25f);
            light.range = 9f;
            light.intensity = 2.5f;
        }

        private static void Spawner(Transform parent, string monsterId, Vector3 position, int count, float radius)
        {
            var go = new GameObject($"Spawn_{monsterId}");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var spawner = go.AddComponent<MonsterSpawner>();
            spawner.MonsterId = monsterId;
            spawner.Count = count;
            spawner.Radius = radius;
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

        private static GameObject Block(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color, bool keepCollider,
            float outline = PropOutline, Color? emission = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            if (!keepCollider)
            {
                Object.DestroyImmediate(go.GetComponent<Collider>());
            }

            go.GetComponent<Renderer>().sharedMaterial = MaterialFor(color, outline, emission);
            return go;
        }

        private static void SetStatic(GameObject go)
        {
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        }

        /// <summary>
        /// Toon material asset (GDD Phase 2 Step 5) when the Runeheir/Toon shader and URP are available,
        /// otherwise URP Lit / Standard. One asset per color + outline + emission combination.
        /// </summary>
        private static Material MaterialFor(Color color, float outline, Color? emission)
        {
            color.a = 1f;
            string key = $"{ColorUtility.ToHtmlStringRGB(color)}_o{Mathf.RoundToInt(outline * 10f)}" +
                         (emission.HasValue ? "_e" + ColorUtility.ToHtmlStringRGB(emission.Value) : string.Empty);
            if (MaterialCache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var shader = PreferredShader(out bool toon);
            string path = $"{s_materialsFolder}/M_{key}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            material.color = color;
            if (toon)
            {
                material.SetFloat("_OutlineWidth", outline);
                material.SetColor("_EmissionColor", emission ?? Color.black);
            }
            else
            {
                if (material.HasProperty("_Smoothness"))
                {
                    material.SetFloat("_Smoothness", 0.12f);
                }

                if (material.HasProperty("_Glossiness"))
                {
                    material.SetFloat("_Glossiness", 0.12f);
                }

                if (emission.HasValue && material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", emission.Value);
                }
            }

            EditorUtility.SetDirty(material);
            MaterialCache[key] = material;
            return material;
        }

        private static Shader PreferredShader(out bool toon)
        {
            var pipeline = GraphicsSettings.currentRenderPipeline;
            bool urp = pipeline != null && pipeline.GetType().Name.Contains("Universal");
            var toonShader = urp ? Shader.Find(RuntimeMaterials.ToonShaderName) : null;
            toon = toonShader != null && toonShader.isSupported;
            if (toon)
            {
                return toonShader;
            }

            var lit = Shader.Find("Universal Render Pipeline/Lit");
            return urp && lit != null ? lit : Shader.Find("Standard");
        }

        private static Vector3 RandomPoint(System.Random random, float halfExtent)
        {
            return new Vector3(Range(random, -halfExtent, halfExtent), 0f, Range(random, -halfExtent, halfExtent));
        }

        private static bool IsFree(Vector3 position, List<(Vector3 center, float radius)> avoid, float radius)
        {
            if (Mathf.Abs(position.x) < 4f || Mathf.Abs(position.z) < 4f)
            {
                return false; // keep the dirt paths clear
            }

            foreach (var (center, otherRadius) in avoid)
            {
                if (Vector3.Distance(position, center) < radius + otherRadius)
                {
                    return false;
                }
            }

            return true;
        }

        private static float Range(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
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

        private static void RegisterScenesInBuildSettings(string loginPath, string fieldPath)
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(loginPath, true),
                new EditorBuildSettingsScene(fieldPath, true),
            };
            foreach (var existing in EditorBuildSettings.scenes)
            {
                if (existing.path != loginPath && existing.path != fieldPath)
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
