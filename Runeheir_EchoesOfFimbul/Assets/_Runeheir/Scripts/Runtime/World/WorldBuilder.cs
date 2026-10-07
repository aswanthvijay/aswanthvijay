using System.Collections.Generic;
using Runeheir.Movement;
using Runeheir.Visuals;
using Runeheir.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace Runeheir.WorldBuilding
{
    /// <summary>Colors, light and fog of one <see cref="MapTheme"/>.</summary>
    public sealed class WorldTheme
    {
        public Color Ground;
        public Color GroundAlt;
        public Color Path;
        public Color Paving;
        public Color Ice;
        public Color Snow;

        /// <summary>Ground beyond the map's edge (open maps).</summary>
        public Color Outer;

        /// <summary>Water under the gaps (fjord inlets, the sea around Lyngvi). Null = no water plane.</summary>
        public Color? Water;

        /// <summary>Solid rock between dungeon rooms (drawn as flat tops at wall height) and the arena stands.</summary>
        public bool Enclosed;

        public Color RockTop;
        public Color Wall;
        public float WallHeight = 2.4f;

        public Vector3 SunEuler = new Vector3(50f, -35f, 0f);
        public Color SunColor = new Color(1f, 0.93f, 0.80f);
        public float SunIntensity = 1.3f;
        public Color AmbientSky = new Color(0.62f, 0.70f, 0.82f);
        public Color AmbientEquator = new Color(0.55f, 0.58f, 0.52f);
        public Color AmbientGround = new Color(0.30f, 0.28f, 0.24f);
        public Color Fog = new Color(0.78f, 0.84f, 0.90f);
        public float FogStart = 45f;
        public float FogEnd = 140f;

        public Color ColorOf(CellTag tag, bool alt)
        {
            switch (tag)
            {
                case CellTag.Path: return Path;
                case CellTag.Paving: return Paving;
                case CellTag.Ice: return Ice;
                case CellTag.Snow: return alt ? Snow * 0.96f : Snow;
                default: return alt ? GroundAlt : Ground;
            }
        }

        public static WorldTheme For(MapTheme theme)
        {
            var t = new WorldTheme();
            t.Path = new Color(0.56f, 0.45f, 0.31f);
            t.Paving = new Color(0.64f, 0.62f, 0.58f);
            t.Ice = new Color(0.72f, 0.87f, 0.96f);
            t.Snow = new Color(0.90f, 0.93f, 0.97f);
            switch (theme)
            {
                case MapTheme.Town:
                    t.Ground = new Color(0.47f, 0.63f, 0.30f);
                    t.GroundAlt = new Color(0.43f, 0.59f, 0.28f);
                    t.SunColor = new Color(1f, 0.88f, 0.68f);
                    t.SunIntensity = 1.35f;
                    t.Fog = new Color(0.86f, 0.82f, 0.72f);
                    break;
                case MapTheme.Meadow:
                    t.Ground = new Color(0.42f, 0.63f, 0.30f);
                    t.GroundAlt = new Color(0.36f, 0.56f, 0.26f);
                    break;
                case MapTheme.BirchForest:
                    t.Ground = new Color(0.55f, 0.52f, 0.27f);
                    t.GroundAlt = new Color(0.62f, 0.47f, 0.24f);
                    t.Path = new Color(0.47f, 0.36f, 0.24f);
                    t.SunColor = new Color(1f, 0.82f, 0.58f);
                    t.SunEuler = new Vector3(38f, -50f, 0f);
                    t.AmbientSky = new Color(0.70f, 0.62f, 0.52f);
                    t.Fog = new Color(0.82f, 0.70f, 0.52f);
                    t.FogStart = 30f;
                    t.FogEnd = 110f;
                    break;
                case MapTheme.Fjord:
                    t.Ground = new Color(0.36f, 0.45f, 0.37f);
                    t.GroundAlt = new Color(0.40f, 0.44f, 0.40f);
                    t.Path = new Color(0.50f, 0.48f, 0.44f);
                    t.Water = new Color(0.16f, 0.31f, 0.42f);
                    t.SunColor = new Color(0.85f, 0.90f, 1f);
                    t.SunIntensity = 1.1f;
                    t.AmbientSky = new Color(0.56f, 0.64f, 0.74f);
                    t.Fog = new Color(0.62f, 0.70f, 0.78f);
                    t.FogStart = 30f;
                    t.FogEnd = 115f;
                    break;
                case MapTheme.Tundra:
                    t.Ground = new Color(0.78f, 0.82f, 0.86f);
                    t.GroundAlt = new Color(0.74f, 0.79f, 0.84f);
                    t.Path = new Color(0.60f, 0.61f, 0.64f);
                    t.SunColor = new Color(0.90f, 0.94f, 1f);
                    t.SunIntensity = 1.15f;
                    t.SunEuler = new Vector3(32f, -20f, 0f);
                    t.AmbientSky = new Color(0.72f, 0.80f, 0.90f);
                    t.AmbientGround = new Color(0.55f, 0.58f, 0.62f);
                    t.Fog = new Color(0.86f, 0.90f, 0.95f);
                    t.FogStart = 25f;
                    t.FogEnd = 100f;
                    break;
                case MapTheme.Isle:
                    t.Ground = new Color(0.86f, 0.90f, 0.95f);
                    t.GroundAlt = new Color(0.82f, 0.87f, 0.93f);
                    t.Path = new Color(0.45f, 0.55f, 0.72f);
                    t.Water = new Color(0.09f, 0.17f, 0.28f);
                    t.SunColor = new Color(0.75f, 0.80f, 1f);
                    t.SunIntensity = 0.95f;
                    t.SunEuler = new Vector3(28f, 140f, 0f);
                    t.AmbientSky = new Color(0.45f, 0.50f, 0.68f);
                    t.AmbientEquator = new Color(0.38f, 0.40f, 0.50f);
                    t.Fog = new Color(0.30f, 0.34f, 0.48f);
                    t.FogStart = 30f;
                    t.FogEnd = 95f;
                    break;
                case MapTheme.Arena:
                    t.Ground = new Color(0.62f, 0.56f, 0.44f);
                    t.GroundAlt = t.Ground;
                    t.Paving = new Color(0.58f, 0.52f, 0.42f);
                    t.Enclosed = true;
                    t.RockTop = new Color(0.20f, 0.19f, 0.20f);
                    t.Wall = new Color(0.40f, 0.36f, 0.33f);
                    t.WallHeight = 2.8f;
                    t.SunColor = new Color(1f, 0.78f, 0.55f);
                    t.SunIntensity = 1f;
                    t.AmbientSky = new Color(0.50f, 0.42f, 0.36f);
                    t.Fog = new Color(0.22f, 0.16f, 0.14f);
                    t.FogStart = 35f;
                    t.FogEnd = 90f;
                    break;
                case MapTheme.Crypt:
                    t.Ground = new Color(0.27f, 0.28f, 0.33f);
                    t.GroundAlt = new Color(0.25f, 0.26f, 0.31f);
                    t.Path = new Color(0.22f, 0.23f, 0.28f);
                    t.Enclosed = true;
                    t.RockTop = new Color(0.07f, 0.07f, 0.10f);
                    t.Wall = new Color(0.28f, 0.29f, 0.36f);
                    t.SunColor = new Color(0.55f, 0.65f, 0.95f);
                    t.SunIntensity = 0.45f;
                    t.AmbientSky = new Color(0.30f, 0.34f, 0.48f);
                    t.AmbientEquator = new Color(0.22f, 0.24f, 0.32f);
                    t.AmbientGround = new Color(0.12f, 0.12f, 0.16f);
                    t.Fog = new Color(0.04f, 0.05f, 0.09f);
                    t.FogStart = 18f;
                    t.FogEnd = 60f;
                    break;
                case MapTheme.IceCavern:
                    t.Ground = new Color(0.52f, 0.62f, 0.72f);
                    t.GroundAlt = new Color(0.48f, 0.58f, 0.70f);
                    t.Path = new Color(0.44f, 0.52f, 0.62f);
                    t.Enclosed = true;
                    t.RockTop = new Color(0.10f, 0.16f, 0.24f);
                    t.Wall = new Color(0.42f, 0.56f, 0.72f);
                    t.SunColor = new Color(0.65f, 0.80f, 1f);
                    t.SunIntensity = 0.55f;
                    t.AmbientSky = new Color(0.40f, 0.52f, 0.68f);
                    t.AmbientEquator = new Color(0.30f, 0.40f, 0.52f);
                    t.AmbientGround = new Color(0.15f, 0.20f, 0.28f);
                    t.Fog = new Color(0.06f, 0.10f, 0.18f);
                    t.FogStart = 20f;
                    t.FogEnd = 65f;
                    break;
                default:
                    t.Ground = new Color(0.42f, 0.63f, 0.30f);
                    t.GroundAlt = new Color(0.36f, 0.56f, 0.26f);
                    break;
            }

            t.Outer = t.Ground * 0.94f;
            t.Outer.a = 1f;
            return t;
        }
    }

    /// <summary>The scene objects built for one map.</summary>
    public sealed class BuiltWorld
    {
        public MapDefinition Map;
        public MapLayout Layout;
        public WorldTheme Theme;
        public Transform Root;
        public Vector3 SavePoint;
        public Bounds Bounds;

        /// <summary>The map's spot in the world (zero offline).</summary>
        public Vector3 Origin;

        /// <summary>Public so the realm's map copies can be lit by whoever looks at them.</summary>
        public void ApplyAtmosphere(Camera camera)
        {
            WorldBuilder.ApplyAtmosphere(Theme, camera);
        }
    }

    /// <summary>
    /// Draws a <see cref="MapLayout"/> (Phase 5: every map is generated into the one RH_World scene): ground meshes per
    /// cell type, water or dungeon rock and walls, the props, the light and fog of the theme, and then the NavMesh.
    /// </summary>
    public static class WorldBuilder
    {
        public static BuiltWorld Build(MapDefinition map, Camera camera)
        {
            return Build(map, camera, Vector3.zero, applyAtmosphere: true);
        }

        /// <summary>
        /// Builds <paramref name="map"/> with its center at <paramref name="origin"/> (online realms give every map its own
        /// spot; see <see cref="WorldGrid"/>). A realm server building maps for other players skips the atmosphere, which
        /// belongs to whoever is looking.
        /// </summary>
        public static BuiltWorld Build(MapDefinition map, Camera camera, Vector3 origin, bool applyAtmosphere)
        {
            var layout = MapLayoutGenerator.Generate(map);
            var theme = WorldTheme.For(map.Theme);
            if (applyAtmosphere)
            {
                ApplyAtmosphere(theme, camera);
            }

            var root = new GameObject($"Environment [{map.Name}]").transform;
            float extent = layout.Width * layout.CellSize;
            var baker = root.gameObject.AddComponent<RuntimeNavMeshBaker>();
            baker.BoundsSize = new Vector3(extent + 10f, 30f, extent + 10f);

            BuildGround(layout, theme, root);
            if (theme.Enclosed)
            {
                BuildRock(layout, theme, root);
                BuildWalls(layout, theme, root);
            }
            else
            {
                BuildOuterGround(layout, theme, root);
                BuildBanks(layout, theme, root);
            }

            if (theme.Water.HasValue)
            {
                Quad(root, "Water", new Vector3(0f, -0.45f, 0f), extent + 160f, RuntimeMaterials.Lit(theme.Water.Value, 0f));
            }

            var props = new GameObject("Props").transform;
            props.SetParent(root, false);
            foreach (var prop in layout.Props)
            {
                PropFactory.Build(prop, map.Theme, props);
            }

            // Everything above was drawn around the world origin: move it (and the layout's points) to the map's spot.
            origin.y = 0f;
            root.position = origin;
            layout.Translate(origin.x, origin.z);
            Physics.SyncTransforms();
            baker.EnsureBaked();

            return new BuiltWorld
            {
                Map = map,
                Layout = layout,
                Theme = theme,
                Root = root,
                Origin = origin,
                SavePoint = ToWorld(layout.SavePoint),
                Bounds = new Bounds(origin, new Vector3(extent - layout.CellSize, 20f, extent - layout.CellSize)),
            };
        }

        public static Vector3 ToWorld(GroundPoint point)
        {
            return new Vector3(point.X, 0f, point.Z);
        }

        public static void ApplyAtmosphere(WorldTheme theme, Camera camera)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = theme.AmbientSky;
            RenderSettings.ambientEquatorColor = theme.AmbientEquator;
            RenderSettings.ambientGroundColor = theme.AmbientGround;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = theme.Fog;
            RenderSettings.fogStartDistance = theme.FogStart;
            RenderSettings.fogEndDistance = theme.FogEnd;

            var sun = RenderSettings.sun;
            if (sun == null)
            {
                foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                {
                    if (light.type == LightType.Directional)
                    {
                        sun = light;
                        break;
                    }
                }
            }

            if (sun == null)
            {
                sun = new GameObject("Sun").AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.shadows = LightShadows.Soft;
                RenderSettings.sun = sun;
            }

            sun.color = theme.SunColor;
            sun.intensity = theme.SunIntensity;
            sun.transform.rotation = Quaternion.Euler(theme.SunEuler);

            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = theme.Fog;
            }
        }

        // ---------------------------------------------------------------- ground
        private static void BuildGround(MapLayout layout, WorldTheme theme, Transform root)
        {
            // One submesh per (tag, shade): cells of a kind share a material, and a noise field gives meadows and
            // snowfields some patches without per-cell materials.
            var groups = new Dictionary<Color32, List<int>>();
            var order = new List<Color32>();
            for (int z = 0; z < layout.Height; z++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    if (!layout.IsWalkable(x, z))
                    {
                        continue;
                    }

                    bool alt = Mathf.PerlinNoise(x * 0.23f + layout.Map.Seed % 97, z * 0.23f) > 0.55f;
                    Color32 color = theme.ColorOf(layout.TagAt(x, z), alt);
                    if (!groups.TryGetValue(color, out var cells))
                    {
                        cells = new List<int>();
                        groups[color] = cells;
                        order.Add(color);
                    }

                    cells.Add(z * layout.Width + x);
                }
            }

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var mesh = new Mesh { name = "Ground", indexFormat = IndexFormat.UInt32 };
            var submeshes = new List<int[]>();
            var materials = new List<Material>();
            float s = layout.CellSize;
            foreach (var color in order)
            {
                var triangles = new List<int>();
                foreach (int index in groups[color])
                {
                    int x = index % layout.Width;
                    int z = index / layout.Width;
                    float x0 = layout.OriginX + x * s;
                    float z0 = layout.OriginZ + z * s;
                    int v = vertices.Count;
                    vertices.Add(new Vector3(x0, 0f, z0));
                    vertices.Add(new Vector3(x0, 0f, z0 + s));
                    vertices.Add(new Vector3(x0 + s, 0f, z0 + s));
                    vertices.Add(new Vector3(x0 + s, 0f, z0));
                    for (int i = 0; i < 4; i++)
                    {
                        normals.Add(Vector3.up);
                    }

                    triangles.AddRange(new[] { v, v + 1, v + 2, v, v + 2, v + 3 });
                }

                submeshes.Add(triangles.ToArray());
                materials.Add(RuntimeMaterials.Lit(color, 0f));
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.subMeshCount = submeshes.Count;
            for (int i = 0; i < submeshes.Count; i++)
            {
                mesh.SetTriangles(submeshes[i], i);
            }

            mesh.RecalculateBounds();
            var go = new GameObject("Ground");
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials.ToArray();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        /// <summary>Open maps: ground continues past the edge (no collider) so the camera never looks into the void.</summary>
        private static void BuildOuterGround(MapLayout layout, WorldTheme theme, Transform root)
        {
            if (theme.Water.HasValue && layout.Map.Theme == MapTheme.Isle)
            {
                return; // Lyngvi is an island in the open sea
            }

            float half = layout.Width * layout.CellSize * 0.5f;
            const float skirt = 90f;
            var material = RuntimeMaterials.Lit(theme.Outer, 0f);
            float size = half * 2f + skirt * 2f;
            Strip(root, "OuterN", new Vector3(0f, -0.02f, half + skirt * 0.5f), new Vector2(size, skirt), material);
            Strip(root, "OuterS", new Vector3(0f, -0.02f, -half - skirt * 0.5f), new Vector2(size, skirt), material);
            Strip(root, "OuterE", new Vector3(half + skirt * 0.5f, -0.02f, 0f), new Vector2(skirt, half * 2f), material);
            Strip(root, "OuterW", new Vector3(-half - skirt * 0.5f, -0.02f, 0f), new Vector2(skirt, half * 2f), material);
        }

        /// <summary>Earth banks where walkable ground meets water.</summary>
        private static void BuildBanks(MapLayout layout, WorldTheme theme, Transform root)
        {
            if (!theme.Water.HasValue)
            {
                return;
            }

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            float s = layout.CellSize;
            const float depth = 0.6f;
            for (int z = 0; z < layout.Height; z++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    if (!layout.IsWalkable(x, z))
                    {
                        continue;
                    }

                    float x0 = layout.OriginX + x * s;
                    float z0 = layout.OriginZ + z * s;
                    if (!layout.IsWalkable(x + 1, z))
                    {
                        Face(vertices, triangles, new Vector3(x0 + s, 0f, z0), new Vector3(x0 + s, 0f, z0 + s), depth);
                    }

                    if (!layout.IsWalkable(x - 1, z))
                    {
                        Face(vertices, triangles, new Vector3(x0, 0f, z0 + s), new Vector3(x0, 0f, z0), depth);
                    }

                    if (!layout.IsWalkable(x, z + 1))
                    {
                        Face(vertices, triangles, new Vector3(x0 + s, 0f, z0 + s), new Vector3(x0, 0f, z0 + s), depth);
                    }

                    if (!layout.IsWalkable(x, z - 1))
                    {
                        Face(vertices, triangles, new Vector3(x0, 0f, z0), new Vector3(x0 + s, 0f, z0), depth);
                    }
                }
            }

            var bank = theme.Ground * 0.55f;
            bank.a = 1f;
            MeshObject(root, "Banks", vertices, triangles, RuntimeMaterials.Lit(bank, 0f));
        }

        // ---------------------------------------------------------------- dungeons and the arena
        private static void BuildRock(MapLayout layout, WorldTheme theme, Transform root)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            float s = layout.CellSize;
            float h = theme.WallHeight;
            for (int z = -2; z < layout.Height + 2; z++)
            {
                for (int x = -2; x < layout.Width + 2; x++)
                {
                    if (layout.IsWalkable(x, z))
                    {
                        continue;
                    }

                    float x0 = layout.OriginX + x * s;
                    float z0 = layout.OriginZ + z * s;
                    int v = vertices.Count;
                    vertices.Add(new Vector3(x0, h, z0));
                    vertices.Add(new Vector3(x0, h, z0 + s));
                    vertices.Add(new Vector3(x0 + s, h, z0 + s));
                    vertices.Add(new Vector3(x0 + s, h, z0));
                    triangles.AddRange(new[] { v, v + 1, v + 2, v, v + 2, v + 3 });
                }
            }

            MeshObject(root, "Rock", vertices, triangles, RuntimeMaterials.Lit(theme.RockTop, 0f));

            // Beyond the grid (and its two-cell margin): a ring of the same rock so the camera never sees the sky.
            float half = layout.Width * s * 0.5f + 2f * s;
            const float skirt = 90f;
            var material = RuntimeMaterials.Lit(theme.RockTop, 0f);
            float size = half * 2f + skirt * 2f;
            Strip(root, "AbyssN", new Vector3(0f, h, half + skirt * 0.5f), new Vector2(size, skirt), material);
            Strip(root, "AbyssS", new Vector3(0f, h, -half - skirt * 0.5f), new Vector2(size, skirt), material);
            Strip(root, "AbyssE", new Vector3(half + skirt * 0.5f, h, 0f), new Vector2(skirt, half * 2f), material);
            Strip(root, "AbyssW", new Vector3(-half - skirt * 0.5f, h, 0f), new Vector2(skirt, half * 2f), material);
        }

        private static void BuildWalls(MapLayout layout, WorldTheme theme, Transform root)
        {
            var walls = new GameObject("Walls").transform;
            walls.SetParent(root, false);
            walls.gameObject.AddComponent<NavBlocker>();
            var material = RuntimeMaterials.Lit(theme.Wall, 1f);
            foreach (var wall in layout.Walls)
            {
                var from = ToWorld(wall.From);
                var to = ToWorld(wall.To);
                float length = Vector3.Distance(from, to);
                if (length < 0.01f)
                {
                    continue;
                }

                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "Wall";
                go.transform.SetParent(walls, false);
                go.transform.position = (from + to) * 0.5f + Vector3.up * (theme.WallHeight * 0.5f);
                go.transform.rotation = Quaternion.LookRotation(to - from);
                go.transform.localScale = new Vector3(0.5f, theme.WallHeight, length + 0.5f);
                go.GetComponent<Renderer>().sharedMaterial = material;
            }
        }

        // ---------------------------------------------------------------- mesh helpers
        private static void Face(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, float depth)
        {
            int v = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(b + Vector3.down * depth);
            vertices.Add(a + Vector3.down * depth);
            triangles.AddRange(new[] { v, v + 1, v + 2, v, v + 2, v + 3 });
        }

        private static void MeshObject(Transform root, string name, List<Vector3> vertices, List<int> triangles, Material material)
        {
            if (vertices.Count == 0)
            {
                return;
            }

            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        private static void Quad(Transform root, string name, Vector3 center, float size, Material material)
        {
            Strip(root, name, center, new Vector2(size, size), material);
        }

        private static void Strip(Transform root, string name, Vector3 center, Vector2 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(root, false);
            go.transform.position = center;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }
    }
}
