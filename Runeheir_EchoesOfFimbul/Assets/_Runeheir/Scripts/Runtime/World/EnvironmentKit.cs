using System.Collections.Generic;
using Runeheir.Visuals;
using Runeheir.World;
using UnityEngine;

namespace Runeheir.WorldBuilding
{
    /// <summary>
    /// Art Pass 3: the Blender-built scenery in Resources/Environment (Tools/Blender/build_environment.py), placed and
    /// painted per map theme. The models only carry slot names (Bark, Leaf, Needle, Rock, Moss...); each theme says what
    /// color they are, so the same fir is dark green on the fjord, snow-laden on the tundra, and the same broadleaf turns
    /// gold in the birch woods. Without the models the scenery falls back to the primitive shapes.
    /// </summary>
    public static class EnvironmentKit
    {
        public const string Folder = "Environment/";

        private const float Outline = 1.2f;

        private static readonly Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();

        private static readonly Color Bark = new Color(0.36f, 0.25f, 0.17f);
        private static readonly Color Snow = new Color(0.93f, 0.96f, 1f);

        /// <summary>The model under <paramref name="parent"/>, painted for <paramref name="theme"/>, or null when it isn't in the build.</summary>
        public static GameObject Spawn(string model, Transform parent, MapTheme theme, int seed)
        {
            var prefab = Load(model);
            if (prefab == null)
            {
                return null;
            }

            var go = Object.Instantiate(prefab, parent, false);
            go.name = model;
            foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                var slots = renderer.sharedMaterials;
                var painted = new Material[slots.Length];
                for (int i = 0; i < slots.Length; i++)
                {
                    string slot = slots[i] != null ? slots[i].name.Replace(" (Instance)", string.Empty) : string.Empty;
                    var color = ColorOf(slot, theme, seed);
                    painted[i] = slot == "Window" || slot == "Runes" ? RuntimeMaterials.Glow(color, color * 1.2f) : RuntimeMaterials.Matte(color, ThinSheet(slot) ? 0f : Outline);
                }

                renderer.sharedMaterials = painted;
            }

            return go;
        }

        /// <summary>Slots drawn as thin sheets (canvas, petals, fronds): an inverted-hull ink line would show through them.</summary>
        private static bool ThinSheet(string slot)
        {
            switch (slot)
            {
                case "Petal":
                case "PetalCenter":
                case "Stem":
                case "Rope":
                case "TentShadow":
                case "Canvas":
                case "CanvasDark":
                case "Frond":
                case "FrondLight":
                    return true;
                default:
                    return false;
            }
        }

        public static bool Has(string model)
        {
            return Load(model) != null;
        }

        private static GameObject Load(string model)
        {
            // Reload after a scene change unloaded it (the cached reference then compares equal to null).
            if (!Prefabs.TryGetValue(model, out var prefab) || prefab == null)
            {
                prefab = Resources.Load<GameObject>(Folder + model);
                Prefabs[model] = prefab;
            }

            return prefab;
        }

        private static bool Snowy(MapTheme theme)
        {
            return theme == MapTheme.Tundra || theme == MapTheme.Isle || theme == MapTheme.IceCavern;
        }

        /// <summary>Each slot's color on a theme; <paramref name="seed"/> varies leaves, petals and stone a little per prop.</summary>
        public static Color ColorOf(string slot, MapTheme theme, int seed)
        {
            float jitter = 0.94f + (seed % 7) * 0.02f;
            switch (slot)
            {
                case "Bark":
                    return (Snowy(theme) ? new Color(0.40f, 0.36f, 0.34f) : theme == MapTheme.Desert ? new Color(0.62f, 0.52f, 0.42f) : Bark) * jitter;
                case "Wood":
                    return new Color(0.72f, 0.56f, 0.36f);
                case "BirchBark":
                    return new Color(0.92f, 0.90f, 0.84f);
                case "BirchMark":
                    return new Color(0.2f, 0.18f, 0.17f);
                case "Leaf":
                case "LeafLight":
                    return Leaves(slot == "LeafLight", theme, seed) * jitter;
                case "Frond":
                case "FrondLight":
                    return Leaves(slot == "FrondLight", theme, seed + 1) * jitter;
                case "Needle":
                    return (theme == MapTheme.Fjord ? new Color(0.16f, 0.34f, 0.26f) : Snowy(theme) ? new Color(0.2f, 0.33f, 0.3f) : new Color(0.17f, 0.38f, 0.22f)) * jitter;
                case "NeedleLight":
                    return Snowy(theme) ? Snow : (theme == MapTheme.Fjord ? new Color(0.24f, 0.45f, 0.34f) : new Color(0.26f, 0.5f, 0.3f)) * jitter;
                case "Rock":
                case "RockDark":
                    return Stone(theme) * (slot == "RockDark" ? 0.8f : 1f) * jitter;
                case "Moss":
                    return Snowy(theme) ? Snow : theme == MapTheme.Desert || theme == MapTheme.Volcano ? Stone(theme) * 0.9f : new Color(0.42f, 0.58f, 0.26f);
                case "Stem":
                    return new Color(0.32f, 0.52f, 0.24f);
                case "Petal":
                    return Petal(seed);
                case "PetalCenter":
                    return new Color(0.98f, 0.84f, 0.3f);
                case "Berry":
                    return new Color(0.78f, 0.16f, 0.2f);
                case "Statue":
                    return Stone(theme) * 1.12f;
                case "StatueDark":
                    return Stone(theme) * 0.86f;
                case "Runes":
                    return new Color(0.45f, 0.85f, 1f);
                case "Wall":
                    return new Color(0.56f, 0.4f, 0.26f) * jitter;
                case "WallDark":
                case "WoodDark":
                    return new Color(0.34f, 0.23f, 0.15f) * jitter;
                case "Roof":
                    return Snowy(theme) ? Snow : RoofColor(seed);
                case "RoofDark":
                    return new Color(0.3f, 0.22f, 0.15f);
                case "Door":
                    return new Color(0.3f, 0.19f, 0.12f);
                case "Window":
                    return new Color(1f, 0.8f, 0.45f);
                case "Stone":
                    return new Color(0.6f, 0.59f, 0.56f) * jitter;
                case "StoneDark":
                    return new Color(0.48f, 0.47f, 0.45f) * jitter;
                case "Gold":
                    return new Color(0.86f, 0.66f, 0.26f);
                case "ShieldA":
                    return new Color(0.68f, 0.18f, 0.15f);
                case "ShieldB":
                    return new Color(0.2f, 0.36f, 0.62f);
                case "Awning":
                    return seed % 2 == 0 ? new Color(0.72f, 0.2f, 0.16f) : new Color(0.2f, 0.38f, 0.66f);
                case "AwningStripe":
                    return new Color(0.92f, 0.88f, 0.78f);
                case "Crate":
                    return new Color(0.62f, 0.46f, 0.3f);
                case "Sack":
                    return new Color(0.78f, 0.7f, 0.52f);
                case "Goods":
                    return seed % 3 == 0 ? new Color(0.92f, 0.72f, 0.2f) : new Color(0.78f, 0.2f, 0.16f);
                case "Water":
                    return new Color(0.22f, 0.42f, 0.6f);
                case "Canvas":
                    return seed % 2 == 0 ? new Color(0.88f, 0.83f, 0.72f) : new Color(0.72f, 0.6f, 0.44f);
                case "CanvasDark":
                    return (seed % 2 == 0 ? new Color(0.88f, 0.83f, 0.72f) : new Color(0.72f, 0.6f, 0.44f)) * 0.82f;
                case "TentShadow":
                    return new Color(0.16f, 0.13f, 0.12f);
                case "Rope":
                    return new Color(0.62f, 0.55f, 0.42f);
                default:
                    return new Color(0.6f, 0.6f, 0.6f);
            }
        }

        private static Color Leaves(bool light, MapTheme theme, int seed)
        {
            switch (theme)
            {
                case MapTheme.BirchForest:
                    // Autumn: golds and russets, a few still green.
                    switch (seed % 3)
                    {
                        case 0: return light ? new Color(0.98f, 0.74f, 0.26f) : new Color(0.88f, 0.55f, 0.18f);
                        case 1: return light ? new Color(0.92f, 0.5f, 0.2f) : new Color(0.76f, 0.32f, 0.16f);
                        default: return light ? new Color(0.78f, 0.78f, 0.3f) : new Color(0.6f, 0.62f, 0.22f);
                    }

                case MapTheme.Jungle:
                    return light ? new Color(0.3f, 0.62f, 0.24f) : new Color(0.15f, 0.42f, 0.2f);
                case MapTheme.Fjord:
                    return light ? new Color(0.36f, 0.56f, 0.34f) : new Color(0.22f, 0.42f, 0.28f);
                case MapTheme.Tundra:
                case MapTheme.Isle:
                    return light ? Snow : new Color(0.32f, 0.44f, 0.4f);
                case MapTheme.Crypt:
                    return light ? new Color(0.4f, 0.44f, 0.36f) : new Color(0.28f, 0.32f, 0.27f);
                case MapTheme.Desert:
                case MapTheme.Coast:
                    return light ? new Color(0.62f, 0.7f, 0.32f) : new Color(0.42f, 0.55f, 0.24f);
                default:
                    return light ? new Color(0.56f, 0.76f, 0.3f) : new Color(0.34f, 0.6f, 0.24f);
            }
        }

        private static Color Stone(MapTheme theme)
        {
            switch (theme)
            {
                case MapTheme.Fjord: return new Color(0.46f, 0.5f, 0.55f);
                case MapTheme.Tundra:
                case MapTheme.Isle:
                case MapTheme.IceCavern: return new Color(0.62f, 0.67f, 0.73f);
                case MapTheme.Desert:
                case MapTheme.Coast:
                case MapTheme.SandTomb: return new Color(0.78f, 0.64f, 0.46f);
                case MapTheme.Volcano: return new Color(0.26f, 0.22f, 0.21f);
                case MapTheme.Crypt: return new Color(0.4f, 0.42f, 0.47f);
                default: return new Color(0.58f, 0.57f, 0.55f);
            }
        }

        /// <summary>Roofs: green turf, golden thatch or red shingles.</summary>
        private static Color RoofColor(int seed)
        {
            switch (seed % 3)
            {
                case 0: return new Color(0.4f, 0.55f, 0.27f);
                case 1: return new Color(0.78f, 0.63f, 0.35f);
                default: return new Color(0.58f, 0.25f, 0.18f);
            }
        }

        private static Color Petal(int seed)
        {
            switch (seed % 5)
            {
                case 0: return new Color(0.95f, 0.62f, 0.78f);
                case 1: return new Color(0.98f, 0.92f, 0.5f);
                case 2: return new Color(0.62f, 0.7f, 0.98f);
                case 3: return new Color(0.98f, 0.98f, 0.96f);
                default: return new Color(0.9f, 0.45f, 0.38f);
            }
        }
    }
}
