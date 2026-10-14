using System;
using System.Collections.Generic;
using System.IO;
using Runeheir.Characters;
using Runeheir.Items;
using Runeheir.Mercenaries;
using Runeheir.Visuals;
using Runeheir.World;
using Runeheir.WorldBuilding;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Runeheir.EditorTools
{
    /// <summary>
    /// Renders generated maps the way the game builds them (WorldBuilder: ground, props, light and fog), from the game
    /// camera's angle and from higher up: Runeheir ▸ Art ▸ Render Map Previews, or from the command line
    /// (<c>-executeMethod Runeheir.EditorTools.MapPreview.RenderFromCommandLine</c>, without -nographics). One map per
    /// Phase 8 theme, with the Hirð's mercenaries standing at the camp. Writes Builds/Previews/map_*.png (not committed).
    /// </summary>
    public static class MapPreview
    {
        public const string OutputFolder = "Builds/Previews";

        private static readonly string[] Maps =
        {
            MapCatalog.WhisperwoodPlains, "xr_moc_fild01", "xr_ayo_fild01", "xr_cmd_fild01", "xr_thor_v01", "xr_moc_pryd01", MapCatalog.VigridHaven,
            "castle_bilskirnir",
        };

        [MenuItem("Runeheir/Art/Render Map Previews", priority = 51)]
        public static void RenderMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var files = Render();
            EditorUtility.RevealInFinder(files[0]);
        }

        public static void RenderFromCommandLine()
        {
            try
            {
                foreach (string file in Render())
                {
                    Debug.Log("[Runeheir] Map preview written: " + file);
                }

                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("Runeheir/Art/Render Showcase", priority = 52)]
        public static void ShowcaseMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var files = Showcase();
            EditorUtility.RevealInFinder(files[0]);
        }

        public static void ShowcaseFromCommandLine()
        {
            try
            {
                foreach (string file in Showcase())
                {
                    Debug.Log("[Runeheir] Showcase written: " + file);
                }

                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// A staged shot of the "Option B" concept with today's art: a snowy Norse village at dusk on the Jotun Steppe, its
        /// longhouses and mead hall roofed in snow, braziers and a rune arch glowing, snow-laden firs, and a blond hero in a
        /// blue cape mid-swing with a greatsword (the slash arc is staged) against frost wolves and a purple mushroom
        /// creature. Builds/Previews/showcase_game.png and showcase_wide.png.
        /// </summary>
        public static List<string> Showcase()
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputFolder));
            Directory.CreateDirectory(folder);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            RenderSettings.sun = sun;
            var camera = new GameObject("ShowcaseCamera").AddComponent<Camera>();
            var map = MapCatalog.Get(MapCatalog.JotunSteppe);
            var built = WorldBuilder.Build(map, camera, Vector3.zero, applyAtmosphere: true);
            var c = built.SavePoint;

            // Dusk: a low, rosy sun and a cold sky, so the fires and runes carry the scene.
            sun.intensity = built.Theme.SunIntensity * 0.7f;
            sun.color = new Color(1f, 0.8f, 0.68f);
            sun.transform.rotation = Quaternion.Euler(30f, -50f, 0f);
            var dusk = built.Theme;
            dusk.AmbientSky = new Color(0.5f, 0.58f, 0.78f);
            dusk.AmbientEquator = new Color(0.42f, 0.46f, 0.6f);
            AmbientProbe(dusk);
            RenderSettings.fogColor = new Color(0.62f, 0.7f, 0.84f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = RenderSettings.fogColor;
            Visuals.PostEffects.Apply(camera, sun.color, 0.8f);

            // Clear the generated scenery around the stage.
            var props = built.Root.Find("Props");
            var clear = new List<GameObject>();
            foreach (Transform prop in props)
            {
                var d = prop.position - c;
                d.y = 0f;
                if (d.magnitude < 24f)
                {
                    clear.Add(prop.gameObject);
                }
            }

            foreach (var go in clear)
            {
                UnityEngine.Object.DestroyImmediate(go);
            }

            var stage = new GameObject("Stage").transform;
            void Place(PropKind kind, float x, float z, float yaw = 0f, float scale = 1f, MapTheme theme = MapTheme.Tundra)
            {
                PropFactory.Build(new PropPlacement { Kind = kind, At = new GroundPoint(c.x + x, c.z + z), Yaw = yaw, Scale = scale }, theme, stage);
            }

            // The village behind the fight.
            Place(PropKind.MeadHall, 1f, 22f, 45f);
            Place(PropKind.House, -9f, 10f, 225f);
            Place(PropKind.House, 10f, 8f, 200f, 0.95f);
            Place(PropKind.Stall, -3.5f, 9f, 215f);
            Place(PropKind.Well, 4.5f, 13f);
            foreach (var (x, z) in new[] { (-2.8f, -1.4f), (3.9f, -0.8f), (-4.8f, 6.5f), (5.2f, 5.6f), (-1.5f, 15f), (5.5f, 16.5f) })
            {
                Place(PropKind.Brazier, x, z);
            }

            Place(PropKind.Banner, -5.8f, 3.6f, 45f, 1f, MapTheme.Castle);
            Place(PropKind.Banner, 6.2f, 2.8f, 45f, 1f, MapTheme.Castle);
            Place(PropKind.Banner, -0.2f, 11.2f, 45f, 1f, MapTheme.Castle);
            foreach (var (x, z, s) in new[] { (-12f, 3f, 1.2f), (-10.5f, -5f, 1f), (11f, -4f, 1.1f), (13f, 4f, 1.25f), (-6f, 17f, 1.15f), (11f, 16f, 1.2f),
                         (-14f, 12f, 1.3f), (15f, 12f, 1.1f), (-8f, -9f, 0.9f), (7f, -9f, 1f) })
            {
                Place(PropKind.Tree, x, z, x * 17f, s);
            }

            Place(PropKind.Rock, -5f, -4.5f, 30f, 0.7f);
            Place(PropKind.Rock, 7.5f, -3f, 110f, 0.55f);
            Place(PropKind.Bush, -7.5f, 1f, 0f, 1f);

            // The rune arch: two carved stones and a glowing lintel, turned to face the camera.
            var arch = new GameObject("RuneArch").transform;
            arch.SetPositionAndRotation(c + new Vector3(-7f, 0f, 0.5f), Quaternion.Euler(0f, 45f, 0f));
            foreach (float side in new[] { -1.5f, 1.5f })
            {
                var stone = new GameObject("Pillar").transform;
                stone.SetParent(arch, false);
                stone.localPosition = new Vector3(side, 0f, 0f);
                PropFactory.Build(new PropPlacement { Kind = PropKind.Runestone, At = new GroundPoint(stone.position.x, stone.position.z), Yaw = 45f, Scale = 1.25f },
                    MapTheme.Tundra, arch);
                UnityEngine.Object.DestroyImmediate(stone.gameObject);
            }

            var lintel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.DestroyImmediate(lintel.GetComponent<Collider>());
            lintel.transform.SetParent(arch, false);
            lintel.transform.localPosition = new Vector3(0f, 3.35f, 0f);
            lintel.transform.localScale = new Vector3(4.4f, 0.55f, 0.65f);
            lintel.GetComponent<Renderer>().sharedMaterial = RuntimeMaterials.Matte(new Color(0.6f, 0.65f, 0.72f), 1.2f);
            var glyphs = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.DestroyImmediate(glyphs.GetComponent<Collider>());
            glyphs.transform.SetParent(arch, false);
            glyphs.transform.localPosition = new Vector3(0f, 3.35f, -0.34f);
            glyphs.transform.localScale = new Vector3(3.2f, 0.16f, 0.04f);
            glyphs.GetComponent<Renderer>().sharedMaterial = RuntimeMaterials.Glow(new Color(0.45f, 0.85f, 1f), new Color(0.6f, 1.1f, 1.4f));

            // The hero: blond and spiky-haired, in a blue cape, a greatsword just past the strike.
            var hero = new GameObject("Hero").transform;
            hero.SetPositionAndRotation(c, Quaternion.Euler(0f, 42f, 0f));
            var look = new AvatarLook
            {
                Job = Jobs.JobId.Valkyrie,
                Gender = Gender.Male,
                HairStyle = 1,
                Hair = AvatarLook.HairPalette[2],
                Skin = AvatarLook.DefaultSkin,
                Outfit = RuntimeMaterials.Hex(Jobs.JobDatabase.Get(Jobs.JobId.Valkyrie).ColorHex),
                Weapon = Combat.WeaponType.TwoHandSword,
            };
            var avatar = PlaceholderAvatar.CreateHumanoid(hero, look);
            avatar.Rig?.Animate(new RigMotion { Time = 0.8f, Combat = 1f, AttackT = 0.58f, AttackIndex = 1, SkillT = -1f });
            SlashArc(hero);

            // What it's fighting.
            void Foe(Monsters.MonsterDefinition definition, float x, float z)
            {
                var holder = new GameObject(definition.Id).transform;
                var at = c + new Vector3(x, 0f, z);
                var toHero = c - at;
                holder.SetPositionAndRotation(at, Quaternion.LookRotation(new Vector3(toHero.x, 0f, toHero.z)));
                PlaceholderAvatar.CreateMonster(holder, definition);
            }

            var wolf = new Monsters.MonsterDefinition { Id = "showcase_frost_wolf", Name = "Frost Wolf", Shape = Monsters.MonsterShape.Quadruped, ColorHex = "#D8E6F2", Scale = 1.15f };
            Foe(wolf, 2.7f, 1.5f);
            Foe(wolf, 1.3f, 3.2f);
            Foe(new Monsters.MonsterDefinition { Id = "showcase_spore", Name = "Spore", Shape = Monsters.MonsterShape.Blob, ColorHex = "#8E5BC2", Scale = 1.15f }, -1.9f, 3.5f);

            return new List<string>
            {
                Shoot(camera, folder, "showcase_game.png", c + new Vector3(0.6f, 0f, 2.6f), 15f, 36f),
                Shoot(camera, folder, "showcase_wide.png", c + new Vector3(0f, 0f, 7f), 31f, 42f),
            };
        }

        /// <summary>A staged greatsword slash: a tapered glowing crescent sweeping across the hero's front.</summary>
        private static void SlashArc(Transform hero)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            const int steps = 24;
            var tilt = Quaternion.Euler(0f, 0f, -28f);
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                float angle = Mathf.Lerp(-75f, 115f, t) * Mathf.Deg2Rad;
                var dir = tilt * new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                float width = 0.7f * Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.7f));
                var center = new Vector3(0f, 1.25f, 0f);
                vertices.Add(center + dir * 1.35f);
                vertices.Add(center + dir * (1.35f + width));
                if (i > 0)
                {
                    int a = (i - 1) * 2;
                    triangles.AddRange(new[] { a, a + 1, a + 3, a, a + 3, a + 2, a + 3, a + 1, a, a + 2, a + 3, a });
                }
            }

            var mesh = new Mesh { name = "SlashArc" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            var go = new GameObject("SlashArc");
            go.transform.SetParent(hero, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = RuntimeMaterials.Glow(new Color(0.3f, 0.78f, 1f), new Color(0.22f, 0.75f, 1.25f));
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        public static List<string> Render()
        {
            var files = new List<string>();
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputFolder));
            Directory.CreateDirectory(folder);
            files.Add(IconSheet(folder));
            foreach (string id in Maps)
            {
                var map = MapCatalog.Get(id);
                if (map == null)
                {
                    continue;
                }

                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var sun = new GameObject("Sun").AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.shadows = LightShadows.Soft;
                RenderSettings.sun = sun;
                var camera = new GameObject("PreviewCamera").AddComponent<Camera>();
                var built = WorldBuilder.Build(map, camera, Vector3.zero, applyAtmosphere: true);
                AmbientProbe(built.Theme);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = built.Theme.Fog;

                var focus = built.SavePoint;
                if (map.Kind == MapKind.Castle)
                {
                    // The Jarlstone and its hoards where the hall spawns them (save point at z -10, stone at 14, hoards at 21).
                    var stone = new GameObject("Jarlstone").transform;
                    stone.position = focus + new Vector3(0f, 0f, 24f);
                    PlaceholderAvatar.CreateMonster(stone, Monsters.MonsterCatalog.Get(Social.SiegeRules.JarlstoneId));
                    for (int i = -1; i <= 1; i++)
                    {
                        var hoard = new GameObject("Hoard").transform;
                        hoard.SetPositionAndRotation(focus + new Vector3(i * 2.5f, 0f, 31f), Quaternion.Euler(0f, 180f, 0f));
                        PlaceholderAvatar.CreateMonster(hoard, Monsters.MonsterCatalog.Get(Social.SiegeRules.HoardId));
                    }

                    files.Add(Shoot(camera, folder, "castle_jarlstone.png", focus + new Vector3(0f, 0f, 26f), 14f, 30f));
                }
                else
                {
                    Mercenaries(focus + new Vector3(4f, 0f, -2f));
                }

                string name = $"map_{map.Theme.ToString().ToLowerInvariant()}_{id}";
                files.Add(Shoot(camera, folder, name + "_game.png", focus, 30f, 45f));
                files.Add(Shoot(camera, folder, name + "_wide.png", focus, 80f, 55f));
                if (id == MapCatalog.VigridHaven)
                {
                    var stage = focus + new Vector3(-6f, 0f, 6f);
                    Costumes(stage);
                    files.Add(Shoot(camera, folder, "costumes_lineup.png", stage + new Vector3(3.5f, 0f, -1.5f), 13f, 30f));
                }
            }

            files.AddRange(Bestiary(folder));
            files.Add(KitLineup(folder));
            return files;
        }

        /// <summary>
        /// Art Pass 3: every model in Resources/Environment, painted for the meadow (front row) and the tundra (back row),
        /// in the game shader: Builds/Previews/environment_kit.png.
        /// </summary>
        private static string KitLineup(string folder)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var theme = WorldTheme.For(MapTheme.Meadow);
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.color = theme.SunColor;
            sun.intensity = theme.SunIntensity;
            sun.transform.rotation = Quaternion.Euler(theme.SunEuler);
            RenderSettings.sun = sun;
            AmbientProbe(theme);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = Vector3.one * 12f;
            ground.GetComponent<Renderer>().sharedMaterial = RuntimeMaterials.Lit(theme.Ground, 0f);
            var camera = new GameObject("PreviewCamera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = theme.Fog;
            Visuals.PostEffects.Apply(camera, theme.SunColor, 0.35f);

            var models = new List<string>();
            foreach (var prefab in Resources.LoadAll<GameObject>(EnvironmentKit.Folder.TrimEnd('/')))
            {
                models.Add(prefab.name);
            }

            models.Sort(string.CompareOrdinal);
            var across = new Vector3(1f, 0f, -1f).normalized;
            var away = new Vector3(1f, 0f, 1f).normalized;
            for (int i = 0; i < models.Count; i++)
            {
                for (int row = 0; row < 2; row++)
                {
                    var holder = new GameObject(models[i]).transform;
                    holder.position = across * ((i - (models.Count - 1) * 0.5f) * 3.2f) + away * (row * 7f);
                    holder.rotation = Quaternion.Euler(0f, 200f, 0f);
                    EnvironmentKit.Spawn(models[i], holder, row == 0 ? MapTheme.Meadow : MapTheme.Tundra, i);
                }
            }

            return Shoot(camera, folder, "environment_kit.png", away * 3f, 46f, 34f);
        }

        /// <summary>
        /// Phase 8: one monster of every body and variant (the first ordinary monster drawn with it), on a plain meadow,
        /// eighteen to a picture: Builds/Previews/bestiary_&lt;n&gt;.png.
        /// </summary>
        private static List<string> Bestiary(string folder)
        {
            var picks = new List<Monsters.MonsterDefinition>();
            var seen = new HashSet<string>();
            foreach (bool bosses in new[] { false, true })
            {
                foreach (var monster in Monsters.MonsterCatalog.All)
                {
                    if (monster.IsBoss != bosses || monster.SiegeObject || monster.Shape == Monsters.MonsterShape.Dummy)
                    {
                        continue;
                    }

                    if (seen.Add(monster.Shape + "/" + monster.Variant))
                    {
                        picks.Add(monster);
                    }
                }
            }

            picks.Sort((a, b) => a.Shape != b.Shape ? a.Shape.CompareTo(b.Shape) : string.CompareOrdinal(a.Variant, b.Variant));
            var files = new List<string>();
            var theme = WorldTheme.For(MapTheme.Meadow);
            for (int page = 0; page * 18 < picks.Count; page++)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var sun = new GameObject("Sun").AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.shadows = LightShadows.Soft;
                sun.color = theme.SunColor;
                sun.intensity = theme.SunIntensity;
                sun.transform.rotation = Quaternion.Euler(theme.SunEuler);
                RenderSettings.sun = sun;
                AmbientProbe(theme);
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.transform.localScale = Vector3.one * 12f;
                ground.GetComponent<Renderer>().sharedMaterial = RuntimeMaterials.Lit(theme.Ground);
                var camera = new GameObject("PreviewCamera").AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = theme.Fog;

                // Rows run across the camera's view (it looks along +X+Z), each monster facing it.
                var across = new Vector3(1f, 0f, -1f).normalized;
                var away = new Vector3(1f, 0f, 1f).normalized;
                int count = Math.Min(18, picks.Count - page * 18);
                for (int i = 0; i < count; i++)
                {
                    var monster = picks[page * 18 + i];
                    var holder = new GameObject(monster.Name).transform;
                    holder.SetPositionAndRotation(across * ((i % 9 - 4f) * 2.5f) + away * ((i / 9) * 3.2f), Quaternion.Euler(0f, 225f, 0f));
                    PlaceholderAvatar.CreateMonster(holder, monster);
                }

                files.Add(Shoot(camera, folder, $"bestiary_{page + 1}.png", away * 1.6f, 25f, 30f));
            }

            return files;
        }

        /// <summary>Phase 8.4: one character per costume look template, wearing the first costume drawn with it.</summary>
        private static void Costumes(Vector3 at)
        {
            var seen = new HashSet<string>();
            var picks = new List<ItemDefinition>();
            foreach (var item in ItemCatalog.All)
            {
                if (!item.IsCostume || item.LookAs == null)
                {
                    continue;
                }

                string template = item.LookAs.StartsWith("wig", StringComparison.Ordinal) ? "wig" : item.LookAs;
                if (seen.Add(template))
                {
                    picks.Add(item);
                }
            }

            for (int i = 0; i < picks.Count; i++)
            {
                var record = CharacterFactory.Create(new CharacterCreateRequest { Name = "Model", Gender = i % 2 == 0 ? Gender.Female : Gender.Male }, 0, 0);
                record.EquipmentDataVersion = EquipmentSet.CurrentDataVersion;
                record.Equipment = new ItemStack[EquipmentSet.Positions];
                var set = new EquipmentSet(record, new Inventory(record.Inventory));
                record.Equipment[(int)set.TargetPosition(picks[i])] = ItemStack.NewInstance(picks[i]);
                var root = new GameObject("Costume " + picks[i].Name).transform;
                root.position = at + new Vector3((i % 8) * 1.1f, 0f, (i / 8) * -1.6f);
                root.rotation = Quaternion.Euler(0f, 200f, 0f);
                PlaceholderAvatar.CreateHumanoid(root, AvatarLook.FromRecord(record));
            }
        }

        /// <summary>Every item pictogram (UI.ItemIcons) on its plate, tinted as the game tints it: Builds/Previews/icons_sheet.png.</summary>
        private static string IconSheet(string folder)
        {
            var shapes = (UI.IconShape[])Enum.GetValues(typeof(UI.IconShape));
            const int cell = 96;
            const int columns = 8;
            int rows = (shapes.Length + columns - 1) / columns;
            var sheet = new Texture2D(columns * cell, rows * cell, TextureFormat.RGBA32, false);
            var background = new Color(0.12f, 0.13f, 0.16f);
            var fill = new Color[sheet.width * sheet.height];
            for (int i = 0; i < fill.Length; i++)
            {
                fill[i] = background;
            }

            sheet.SetPixels(fill);
            Color[] tints = { new Color(0.85f, 0.35f, 0.3f), new Color(0.35f, 0.6f, 0.9f), new Color(0.9f, 0.75f, 0.3f), new Color(0.4f, 0.8f, 0.5f) };
            for (int s = 0; s < shapes.Length; s++)
            {
                var pixels = UI.ItemIcons.Render(UI.Shapes.Of(shapes[s]));
                var tint = tints[s % tints.Length];
                var plate = Color.Lerp(tint, new Color(0.08f, 0.09f, 0.12f), 0.62f);
                int ox = (s % columns) * cell + 16;
                int oy = (rows - 1 - s / columns) * cell + 16;
                for (int y = 0; y < 64; y++)
                {
                    for (int x = 0; x < 64; x++)
                    {
                        Color p = pixels[y * 64 + x];
                        var ink = Color.Lerp(tint, Color.white, 0.25f) * p;
                        sheet.SetPixel(ox + x, oy + y, Color.Lerp(plate, new Color(ink.r, ink.g, ink.b, 1f), p.a));
                    }
                }
            }

            sheet.Apply();
            string path = Path.Combine(folder, "icons_sheet.png");
            File.WriteAllBytes(path, sheet.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(sheet);
            return path;
        }

        /// <summary>One of each Hirð role (grade 1) and the Valkyrie contract, lined up by the camp.</summary>
        private static void Mercenaries(Vector3 at)
        {
            var hired = new List<MercenaryDefinition>();
            foreach (var mercenary in MercenaryCatalog.All)
            {
                if ((mercenary.Grade == 1 && mercenary.Role != null) || (mercenary.Role == null && mercenary.Name.StartsWith("Valkyrie", StringComparison.Ordinal)
                                                                            && !hired.Exists(m => m.Name == mercenary.Name)))
                {
                    hired.Add(mercenary);
                }
            }

            for (int i = 0; i < hired.Count; i++)
            {
                var root = new GameObject("Mercenary " + hired[i].Name).transform;
                root.position = at + new Vector3(i * 1.4f, 0f, i * -0.6f);
                root.rotation = Quaternion.Euler(0f, 225f, 0f);
                PlaceholderAvatar.CreateMonster(root, hired[i].Monster);
            }
        }

        /// <summary>A new scene has no baked ambient probe: build the theme's trilight one by hand.</summary>
        private static void AmbientProbe(WorldTheme theme)
        {
            var probe = new SphericalHarmonicsL2();
            probe.AddAmbientLight(theme.AmbientEquator);
            probe.AddDirectionalLight(Vector3.up, theme.AmbientSky - theme.AmbientEquator, 1f);
            probe.AddDirectionalLight(Vector3.down, theme.AmbientGround - theme.AmbientEquator, 1f);
            RenderSettings.ambientProbe = probe;
        }

        private static string Shoot(Camera camera, string folder, string fileName, Vector3 focus, float distance, float pitch)
        {
            const int width = 1920;
            const int height = 1080;
            var rotation = Quaternion.Euler(pitch, 45f, 0f);
            camera.transform.SetPositionAndRotation(focus + Vector3.up + rotation * new Vector3(0f, 0f, -distance), rotation);
            camera.fieldOfView = 30f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 400f;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            camera.targetTexture = target;
            GrassField.DrawAll(camera);
            camera.Render();

            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            RenderTexture.active = previous;

            string path = Path.Combine(folder, fileName);
            File.WriteAllBytes(path, image.EncodeToPNG());
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(target);
            return path;
        }
    }
}
