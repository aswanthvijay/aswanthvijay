using System.Collections.Generic;
using Runeheir.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace Runeheir.WorldBuilding
{
    /// <summary>
    /// Art Pass 3: grass on a map's open ground. Tufts are planted once (seeded by the map, so every player sees the same
    /// meadow) on grassy cells, thick in some patches and thin in others, never on paths, paving or snow, and never
    /// inside a rock or trunk. They're drawn GPU-instanced in 16 m chunks, only the chunks near the camera, through
    /// Runeheir/Grass (wind sway, the ground's own patches and light). Nothing here collides or blocks.
    /// </summary>
    public sealed class GrassField : MonoBehaviour
    {
        public const string ShaderName = "Runeheir/Grass";

        private const float ChunkSize = 16f;
        private const float DrawDistance = 60f;
        private const int MaxPerBatch = 1023;

        private static readonly List<GrassField> Active = new List<GrassField>();
        private static Mesh s_tuft;

        private readonly List<Chunk> _chunks = new List<Chunk>();
        private Material _material;

        private sealed class Chunk
        {
            public Bounds Bounds;
            public readonly List<Matrix4x4> Matrices = new List<Matrix4x4>();
        }

        /// <summary>How thick grass grows on each theme (0 = none).</summary>
        public static float DensityFor(MapTheme theme)
        {
            switch (theme)
            {
                case MapTheme.Meadow: return 1f;
                case MapTheme.Jungle: return 1f;
                case MapTheme.BirchForest: return 0.7f;
                case MapTheme.Fjord: return 0.6f;
                case MapTheme.Town: return 0.35f;
                case MapTheme.Coast: return 0.25f;
                default: return 0f;
            }
        }

        /// <summary>Plants grass for a built map (call once its root sits at its final spot; the layout is still local).</summary>
        public static GrassField Plant(MapLayout layout, MapTheme theme, WorldTheme colors, Transform root)
        {
            float density = DensityFor(theme);
            var shader = density > 0f ? Shader.Find(ShaderName) : null;
            if (shader == null || !shader.isSupported || !Visuals.RuntimeMaterials.ToonAvailable)
            {
                return null;
            }

            var field = root.gameObject.AddComponent<GrassField>();
            field._material = new Material(shader) { name = "RH_Grass", enableInstancing = true };
            var baseColor = Color.Lerp(colors.Ground, colors.GroundAlt, 0.5f) * 0.98f;
            baseColor.a = 1f;
            var tip = Color.Lerp(colors.GroundAlt, new Color(0.95f, 0.98f, 0.7f), 0.3f);
            tip.a = 1f;
            field._material.SetColor("_BaseColor", baseColor);
            field._material.SetColor("_TipColor", tip);

            var blockers = BlockerGrid(layout);
            var random = new System.Random(layout.Map.Seed * 31 + 7);
            float seed = layout.Map.Seed % 997 * 0.37f;
            float extent = layout.Width * layout.CellSize;
            int chunksX = Mathf.CeilToInt(extent / ChunkSize);
            var chunks = new Dictionary<int, Chunk>();
            const float step = 0.85f;
            for (float z = layout.OriginZ; z < layout.OriginZ + layout.Height * layout.CellSize; z += step)
            {
                for (float x = layout.OriginX; x < layout.OriginX + extent; x += step)
                {
                    float px = x + (float)random.NextDouble() * step;
                    float pz = z + (float)random.NextDouble() * step;
                    int cx = Mathf.FloorToInt((px - layout.OriginX) / layout.CellSize);
                    int cz = Mathf.FloorToInt((pz - layout.OriginZ) / layout.CellSize);
                    if (!layout.IsWalkable(cx, cz) || layout.TagAt(cx, cz) != CellTag.Ground)
                    {
                        continue;
                    }

                    float clump = Mathf.PerlinNoise(px * 0.07f + seed, pz * 0.07f - seed);
                    float keep = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.28f, 0.66f, clump)) * density;
                    if ((float)random.NextDouble() > keep || Blocked(blockers, layout, px, pz))
                    {
                        continue;
                    }

                    var world = root.TransformPoint(new Vector3(px, 0f, pz));
                    float size = Mathf.Lerp(0.55f, 1.1f, (float)random.NextDouble()) * Mathf.Lerp(0.85f, 1.15f, clump);
                    var matrix = Matrix4x4.TRS(world, Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f), new Vector3(size, size, size));
                    int key = Mathf.FloorToInt((px - layout.OriginX) / ChunkSize) + Mathf.FloorToInt((pz - layout.OriginZ) / ChunkSize) * chunksX;
                    if (!chunks.TryGetValue(key, out var chunk))
                    {
                        chunk = new Chunk { Bounds = new Bounds(world, Vector3.zero) };
                        chunks[key] = chunk;
                    }

                    chunk.Matrices.Add(matrix);
                    chunk.Bounds.Encapsulate(new Bounds(world + Vector3.up * 0.4f, new Vector3(1.2f, 1f, 1.2f)));
                }
            }

            field._chunks.AddRange(chunks.Values);
            Active.RemoveAll(f => f == null);
            Active.Add(field);
            return field;
        }

        public int TuftCount
        {
            get
            {
                int total = 0;
                foreach (var chunk in _chunks)
                {
                    total += chunk.Matrices.Count;
                }

                return total;
            }
        }

        private void OnDestroy()
        {
            Active.Remove(this);
            if (_material != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_material);
                }
                else
                {
                    DestroyImmediate(_material);
                }
            }
        }

        private void Update()
        {
            Draw(Camera.main);
        }

        /// <summary>Queues every field's nearby grass for <paramref name="camera"/> (the editor's preview renders call this).</summary>
        public static void DrawAll(Camera camera)
        {
            foreach (var field in Active)
            {
                field.Draw(camera);
            }
        }

        private void Draw(Camera camera)
        {
            if (camera == null || _material == null || _chunks.Count == 0)
            {
                return;
            }

            var mesh = Tuft();
            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            var focus = Focus(camera);
            var parameters = new RenderParams(_material)
            {
                camera = Application.isPlaying ? null : camera,
                shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = true,
                layer = gameObject.layer,
            };

            foreach (var chunk in _chunks)
            {
                if ((chunk.Bounds.center - focus).sqrMagnitude > DrawDistance * DrawDistance || !GeometryUtility.TestPlanesAABB(planes, chunk.Bounds))
                {
                    continue;
                }

                parameters.worldBounds = chunk.Bounds;
                for (int start = 0; start < chunk.Matrices.Count; start += MaxPerBatch)
                {
                    int count = Mathf.Min(MaxPerBatch, chunk.Matrices.Count - start);
                    Graphics.RenderMeshInstanced(parameters, mesh, 0, chunk.Matrices, count, start);
                }
            }
        }

        /// <summary>Where the camera looks at the ground.</summary>
        private static Vector3 Focus(Camera camera)
        {
            var ray = new Ray(camera.transform.position, camera.transform.forward);
            var ground = new Plane(Vector3.up, Vector3.zero);
            return ground.Raycast(ray, out float distance) ? ray.GetPoint(Mathf.Min(distance, 200f)) : camera.transform.position;
        }

        // ------------------------------------------------------------ where grass can't grow
        private static Dictionary<int, List<PropPlacement>> BlockerGrid(MapLayout layout)
        {
            var grid = new Dictionary<int, List<PropPlacement>>();
            foreach (var prop in layout.Props)
            {
                if (!PropKinds.Blocks(prop.Kind))
                {
                    continue;
                }

                int cx = Mathf.FloorToInt((prop.At.X - layout.OriginX) / layout.CellSize);
                int cz = Mathf.FloorToInt((prop.At.Z - layout.OriginZ) / layout.CellSize);
                int key = cx + cz * 4096;
                if (!grid.TryGetValue(key, out var list))
                {
                    grid[key] = list = new List<PropPlacement>();
                }

                list.Add(prop);
            }

            return grid;
        }

        private static bool Blocked(Dictionary<int, List<PropPlacement>> grid, MapLayout layout, float x, float z)
        {
            int cx = Mathf.FloorToInt((x - layout.OriginX) / layout.CellSize);
            int cz = Mathf.FloorToInt((z - layout.OriginZ) / layout.CellSize);
            var point = new GroundPoint(x, z);
            for (int dz = -2; dz <= 2; dz++)
            {
                for (int dx = -2; dx <= 2; dx++)
                {
                    if (!grid.TryGetValue(cx + dx + (cz + dz) * 4096, out var list))
                    {
                        continue;
                    }

                    foreach (var prop in list)
                    {
                        if (PropKinds.EdgeDistance(prop, point) < 0.2f)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        // ------------------------------------------------------------ the tuft
        /// <summary>Nine blades fanning out from one root, leaning and curving; vertex colors carry tip (r), shade (g) and sway (a).</summary>
        private static Mesh Tuft()
        {
            if (s_tuft != null)
            {
                return s_tuft;
            }

            var random = new System.Random(4242);
            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            for (int blade = 0; blade < 9; blade++)
            {
                float angle = blade * 40f + (float)random.NextDouble() * 25f;
                var dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                var side = Vector3.Cross(Vector3.up, dir).normalized;
                float height = Mathf.Lerp(0.32f, 0.62f, (float)random.NextDouble());
                float lean = Mathf.Lerp(0.08f, 0.3f, (float)random.NextDouble());
                float width = Mathf.Lerp(0.035f, 0.055f, (float)random.NextDouble());
                float shade = Mathf.Lerp(0.55f, 1f, (float)random.NextDouble());
                var root = dir * (float)random.NextDouble() * 0.12f;
                int start = vertices.Count;
                const int segments = 3;
                for (int i = 0; i <= segments; i++)
                {
                    float t = i / (float)segments;
                    var p = root + Vector3.up * (height * t) + dir * (lean * t * t);
                    float w = width * (1f - t * 0.92f);
                    vertices.Add(p - side * w);
                    vertices.Add(p + side * w);
                    var c = new Color(t, shade, 0f, t * t);
                    colors.Add(c);
                    colors.Add(c);
                }

                for (int i = 0; i < segments; i++)
                {
                    int a = start + i * 2;
                    triangles.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 });
                }
            }

            s_tuft = new Mesh { name = "GrassTuft" };
            s_tuft.SetVertices(vertices);
            s_tuft.SetColors(colors);
            s_tuft.SetTriangles(triangles, 0);
            s_tuft.RecalculateNormals();
            s_tuft.RecalculateBounds();
            return s_tuft;
        }
    }
}
