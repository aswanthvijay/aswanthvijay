using Runeheir.Movement;
using Runeheir.Visuals;
using Runeheir.World;
using UnityEngine;

namespace Runeheir.WorldBuilding
{
    /// <summary>
    /// Scenery from primitives (placeholder art until the Blender models land): one builder per <see cref="PropKind"/>,
    /// tinted per <see cref="MapTheme"/>. Blocking props get one simple collider matching <see cref="PropKinds.Radius"/>
    /// and a <see cref="NavBlocker"/>, so the runtime NavMesh carves them out; decals get no collider at all.
    /// </summary>
    public static class PropFactory
    {
        private const float Outline = 1.5f;

        private static readonly Color Bark = new Color(0.36f, 0.25f, 0.16f);
        private static readonly Color BirchBark = new Color(0.90f, 0.88f, 0.82f);
        private static readonly Color Pine = new Color(0.17f, 0.36f, 0.22f);
        private static readonly Color Leaf = new Color(0.42f, 0.62f, 0.26f);
        private static readonly Color AutumnGold = new Color(0.91f, 0.67f, 0.22f);
        private static readonly Color AutumnRed = new Color(0.82f, 0.38f, 0.16f);
        private static readonly Color Snow = new Color(0.93f, 0.96f, 1f);
        private static readonly Color Stone = new Color(0.55f, 0.56f, 0.58f);
        private static readonly Color DarkStone = new Color(0.27f, 0.29f, 0.33f);
        private static readonly Color Wood = new Color(0.55f, 0.40f, 0.25f);
        private static readonly Color DarkWood = new Color(0.34f, 0.24f, 0.16f);
        private static readonly Color Thatch = new Color(0.78f, 0.64f, 0.36f);
        private static readonly Color RoofRed = new Color(0.55f, 0.22f, 0.16f);
        private static readonly Color Cloth = new Color(0.86f, 0.82f, 0.72f);
        private static readonly Color Iron = new Color(0.22f, 0.23f, 0.26f);
        private static readonly Color Bone = new Color(0.90f, 0.87f, 0.78f);
        private static readonly Color RuneGlow = new Color(0.45f, 0.85f, 1f);
        private static readonly Color Flame = new Color(1f, 0.55f, 0.15f);
        private static readonly Color SpiritFlame = new Color(0.5f, 0.75f, 1f);
        private static readonly Color Ice = new Color(0.68f, 0.85f, 0.97f);
        private static readonly Color Sandstone = new Color(0.80f, 0.66f, 0.45f);
        private static readonly Color Basalt = new Color(0.20f, 0.17f, 0.17f);
        private static readonly Color Lava = new Color(1f, 0.42f, 0.08f);
        private static readonly Color Gold = new Color(0.92f, 0.72f, 0.25f);

        /// <summary>Builds the prop under <paramref name="parent"/> at its placement.</summary>
        public static GameObject Build(PropPlacement prop, MapTheme theme, Transform parent)
        {
            var root = new GameObject("Prop_" + prop.Kind);
            var t = root.transform;
            t.SetParent(parent, false);
            t.SetPositionAndRotation(new Vector3(prop.At.X, 0f, prop.At.Z), Quaternion.Euler(0f, prop.Yaw, 0f));
            t.localScale = Vector3.one * Mathf.Max(0.1f, prop.Scale);
            int seed = Mathf.Abs(Mathf.RoundToInt(prop.At.X * 7.31f + prop.At.Z * 13.17f));

            switch (prop.Kind)
            {
                case PropKind.Tree: Tree(t, theme, seed); break;
                case PropKind.BirchTree: BirchTree(t, theme, seed); break;
                case PropKind.DeadTree: DeadTree(t, theme); break;
                case PropKind.Rock: Rock(t, theme, seed); break;
                case PropKind.Runestone: Runestone(t, theme); break;
                case PropKind.Windmill: Windmill(t); break;
                case PropKind.Campfire: Campfire(t); break;
                case PropKind.House: House(t, theme, seed); break;
                case PropKind.MeadHall: MeadHall(t, theme); break;
                case PropKind.Stall: Stall(t, theme, seed); break;
                case PropKind.Well: Well(t, theme); break;
                case PropKind.Banner: Banner(t, theme, seed); break;
                case PropKind.Torch: Torch(t); break;
                case PropKind.Brazier: Brazier(t, theme); break;
                case PropKind.Pillar: Pillar(t, theme); break;
                case PropKind.Coffin: Coffin(t, theme); break;
                case PropKind.Bones: Bones(t); break;
                case PropKind.Crystal: Crystal(t, seed); break;
                case PropKind.IceSpike: IceSpike(t, seed); break;
                case PropKind.Stalagmite: Stalagmite(t, theme); break;
                case PropKind.Wreck: Wreck(t); break;
                case PropKind.Log: Log(t, theme); break;
                case PropKind.Mushroom: Mushroom(t, seed); break;
                case PropKind.Palisade: Palisade(t, prop.Length > 0f ? prop.Length : 8f); break;
                case PropKind.Statue: Statue(t, theme); break;
                case PropKind.Anvil: Anvil(t); break;
                case PropKind.Chains: Chains(t); break;
                case PropKind.GiantSkull: GiantSkull(t, theme); break;
                case PropKind.Tent: Tent(t, seed); break;
                case PropKind.Pool: Pool(t, theme); break;
                case PropKind.ForgeHut: ForgeHut(t); break;
                case PropKind.GuildHall: GuildHall(t); break;
                case PropKind.Cactus: Cactus(t, seed); break;
                case PropKind.PalmTree: PalmTree(t, seed); break;
                case PropKind.JungleTree: JungleTree(t, seed); break;
                case PropKind.Fern: Kit(t, "fern_0", theme, seed, () => Fern(t, seed)); break;
                case PropKind.LavaPool: LavaPool(t, seed); break;
                case PropKind.Obelisk: Obelisk(t, theme); break;
                case PropKind.Urn: Urn(t, seed); break;
                case PropKind.Throne: Throne(t); break;
                case PropKind.Bush: Kit(t, "bush_" + seed % 2, theme, seed, () => Part(PrimitiveType.Sphere, t, "Bush", new Vector3(0f, 0.5f, 0f), new Vector3(1.4f, 1f, 1.4f), Leaf)); break;
                case PropKind.Flowers: Kit(t, "flowers_0", theme, seed, () => Mushroom(t, seed)); break;
                case PropKind.Stump: Kit(t, "stump_0", theme, seed, () => Part(PrimitiveType.Cylinder, t, "Stump", new Vector3(0f, 0.25f, 0f), new Vector3(0.8f, 0.25f, 0.8f), Bark)); break;
                case PropKind.Hearth: Hearth(t, prop.Length > 0f ? prop.Length : 6f); break;
            }

            if (PropKinds.Blocks(prop.Kind))
            {
                AddCollider(t, prop.Kind, prop.Length > 0f ? prop.Length : 8f);
                root.AddComponent<NavBlocker>();
            }

            return root;
        }

        // ---------------------------------------------------------------- collider
        private static void AddCollider(Transform root, PropKind kind, float length)
        {
            float radius = PropKinds.Radius(kind);
            var go = new GameObject("Collider");
            go.transform.SetParent(root, false);
            float height = kind == PropKind.MeadHall || kind == PropKind.GuildHall || kind == PropKind.House || kind == PropKind.ForgeHut ? 6f : 3f;
            if (kind == PropKind.Palisade)
            {
                var box = go.AddComponent<BoxCollider>();
                box.size = new Vector3(length, 2.5f, radius * 2f);
                box.center = new Vector3(0f, 1.25f, 0f);
                return;
            }

            if (kind == PropKind.House || kind == PropKind.MeadHall || kind == PropKind.GuildHall || kind == PropKind.ForgeHut || kind == PropKind.Stall
                || kind == PropKind.Coffin || kind == PropKind.Anvil)
            {
                var box = go.AddComponent<BoxCollider>();
                box.size = new Vector3(radius * 1.8f, height, radius * 1.4f);
                box.center = new Vector3(0f, height * 0.5f, 0f);
                return;
            }

            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.radius = radius;
            capsule.height = Mathf.Max(height, radius * 2f);
            capsule.center = new Vector3(0f, capsule.height * 0.5f, 0f);
        }

        // ---------------------------------------------------------------- nature
        private static bool Snowy(MapTheme theme)
        {
            return theme == MapTheme.Tundra || theme == MapTheme.Isle;
        }

        private static void Tree(Transform t, MapTheme theme, int seed)
        {
            // Broadleafs on green land, firs on the cold coasts and in the snow.
            bool broadleaf = theme == MapTheme.Meadow || theme == MapTheme.Town || theme == MapTheme.BirchForest || theme == MapTheme.Jungle
                             || theme == MapTheme.Coast || theme == MapTheme.Arena || theme == MapTheme.Castle;
            string model = broadleaf && seed % 5 != 0 ? "tree_round_" + seed % 3 : "tree_pine_" + seed % 2;
            if (EnvironmentKit.Spawn(model, t, theme, seed) != null)
            {
                return;
            }

            Part(PrimitiveType.Cylinder, t, "Trunk", new Vector3(0f, 1.2f, 0f), new Vector3(0.38f, 1.2f, 0.38f), Bark);
            Color needles = theme == MapTheme.Town ? new Color(0.62f, 0.55f, 0.18f) : theme == MapTheme.Fjord ? Pine * 0.85f : Pine;
            if (theme == MapTheme.Meadow && seed % 3 == 0)
            {
                Part(PrimitiveType.Sphere, t, "Crown", new Vector3(0f, 3.2f, 0f), new Vector3(2.8f, 2.4f, 2.8f), Leaf);
                Part(PrimitiveType.Sphere, t, "Crown2", new Vector3(0.6f, 2.7f, 0.3f), new Vector3(1.7f, 1.5f, 1.7f), Leaf * 0.9f);
                return;
            }

            needles.a = 1f;
            Part(PrimitiveType.Sphere, t, "Pine1", new Vector3(0f, 2.5f, 0f), new Vector3(2.8f, 1.5f, 2.8f), needles);
            Part(PrimitiveType.Sphere, t, "Pine2", new Vector3(0f, 3.5f, 0f), new Vector3(2.1f, 1.3f, 2.1f), needles * 1.1f);
            Part(PrimitiveType.Sphere, t, "Pine3", new Vector3(0f, 4.4f, 0f), new Vector3(1.2f, 1.1f, 1.2f), needles * 1.2f);
            if (Snowy(theme))
            {
                Part(PrimitiveType.Sphere, t, "Snow1", new Vector3(0f, 2.85f, 0f), new Vector3(2.4f, 0.6f, 2.4f), Snow);
                Part(PrimitiveType.Sphere, t, "Snow2", new Vector3(0f, 4.8f, 0f), new Vector3(1f, 0.5f, 1f), Snow);
            }
        }

        private static void BirchTree(Transform t, MapTheme theme, int seed)
        {
            if (EnvironmentKit.Spawn("tree_birch_" + seed % 2, t, theme, seed) != null)
            {
                return;
            }

            Part(PrimitiveType.Cylinder, t, "Trunk", new Vector3(0f, 1.5f, 0f), new Vector3(0.32f, 1.5f, 0.32f), BirchBark);
            Part(PrimitiveType.Cube, t, "Mark", new Vector3(0f, 1.1f, 0.15f), new Vector3(0.2f, 0.08f, 0.05f), new Color(0.15f, 0.13f, 0.12f), outline: 0f);
            Color leaves = seed % 3 == 0 ? AutumnRed : seed % 3 == 1 ? AutumnGold : new Color(0.85f, 0.75f, 0.28f);
            if (theme == MapTheme.Meadow || theme == MapTheme.Town)
            {
                leaves = new Color(0.55f, 0.72f, 0.28f);
            }

            Part(PrimitiveType.Sphere, t, "Canopy", new Vector3(0f, 3.7f, 0f), new Vector3(2.5f, 2.3f, 2.5f), leaves);
            Part(PrimitiveType.Sphere, t, "Canopy2", new Vector3(0.6f, 3.1f, 0.4f), new Vector3(1.5f, 1.4f, 1.5f), leaves * 0.92f);
        }

        private static void DeadTree(Transform t, MapTheme theme)
        {
            int seed = Mathf.Abs(Mathf.RoundToInt(t.position.x * 3.1f + t.position.z * 1.7f));
            if (EnvironmentKit.Spawn("tree_dead_" + seed % 2, t, theme, seed) != null)
            {
                return;
            }

            Color bark = Snowy(theme) ? new Color(0.42f, 0.40f, 0.40f) : theme == MapTheme.Desert ? new Color(0.70f, 0.64f, 0.56f) : DarkWood;
            Part(PrimitiveType.Cylinder, t, "Trunk", new Vector3(0f, 1.4f, 0f), new Vector3(0.34f, 1.4f, 0.34f), bark);
            Part(PrimitiveType.Cylinder, t, "BranchL", new Vector3(-0.45f, 2.4f, 0f), new Vector3(0.12f, 0.6f, 0.12f), bark, new Vector3(0f, 0f, 40f));
            Part(PrimitiveType.Cylinder, t, "BranchR", new Vector3(0.4f, 2.1f, 0.1f), new Vector3(0.1f, 0.5f, 0.1f), bark, new Vector3(10f, 0f, -45f));
            if (Snowy(theme))
            {
                Part(PrimitiveType.Sphere, t, "SnowCap", new Vector3(0f, 2.85f, 0f), new Vector3(0.42f, 0.18f, 0.42f), Snow);
            }
        }

        private static void Rock(Transform t, MapTheme theme, int seed)
        {
            if (EnvironmentKit.Spawn("rock_" + seed % 3, t, theme, seed) != null)
            {
                return;
            }

            Color color = theme == MapTheme.Fjord ? new Color(0.42f, 0.46f, 0.50f) : Snowy(theme) ? new Color(0.66f, 0.71f, 0.76f)
                : theme == MapTheme.Desert || theme == MapTheme.Coast || theme == MapTheme.SandTomb ? Sandstone * 0.9f
                : theme == MapTheme.Volcano ? Basalt * 1.3f : Stone;
            float tint = 0.88f + (seed % 5) * 0.04f;
            Part(PrimitiveType.Sphere, t, "Rock", new Vector3(0f, 0.45f, 0f), new Vector3(2.3f, 1.2f, 1.9f), color * tint, new Vector3(0f, seed % 90, 0f));
            Part(PrimitiveType.Sphere, t, "Chip", new Vector3(0.7f, 0.3f, 0.5f), new Vector3(0.9f, 0.6f, 0.8f), color * (tint * 0.92f));
            if (Snowy(theme))
            {
                Part(PrimitiveType.Sphere, t, "Snow", new Vector3(0f, 0.95f, 0f), new Vector3(1.6f, 0.35f, 1.3f), Snow);
            }
        }

        private static void Runestone(Transform t, MapTheme theme)
        {
            Color stone = Snowy(theme) ? new Color(0.58f, 0.62f, 0.68f) : Stone * 0.85f;
            var slab = Part(PrimitiveType.Cube, t, "Slab", new Vector3(0f, 1.2f, 0f), new Vector3(0.9f, 2.6f, 0.4f), stone, new Vector3(0f, 0f, 3f));
            GlowPart(PrimitiveType.Cube, slab, "Glyphs", new Vector3(0f, 0.05f, 0.52f), new Vector3(0.35f, 0.75f, 0.05f), RuneGlow, RuneGlow * 1.6f);
        }

        private static void Mushroom(Transform t, int seed)
        {
            Color cap = seed % 2 == 0 ? new Color(0.78f, 0.22f, 0.18f) : new Color(0.62f, 0.45f, 0.75f);
            Part(PrimitiveType.Cylinder, t, "Stem", new Vector3(0f, 0.25f, 0f), new Vector3(0.18f, 0.25f, 0.18f), Cloth, outline: 0.8f);
            Part(PrimitiveType.Sphere, t, "Cap", new Vector3(0f, 0.55f, 0f), new Vector3(0.6f, 0.3f, 0.6f), cap, outline: 0.8f);
        }

        private static void Log(Transform t, MapTheme theme)
        {
            Color bark = Snowy(theme) ? new Color(0.45f, 0.40f, 0.36f) : Bark;
            Part(PrimitiveType.Cylinder, t, "Log", new Vector3(0f, 0.35f, 0f), new Vector3(0.7f, 1.5f, 0.7f), bark, new Vector3(0f, 0f, 90f));
            Part(PrimitiveType.Cylinder, t, "Ring", new Vector3(1.5f, 0.35f, 0f), new Vector3(0.6f, 0.02f, 0.6f), Wood * 1.3f, new Vector3(0f, 0f, 90f), 0f);
        }

        private static void IceSpike(Transform t, int seed)
        {
            float tilt = (seed % 7) - 3f;
            Part(PrimitiveType.Capsule, t, "Spike", new Vector3(0f, 1.2f, 0f), new Vector3(0.9f, 1.4f, 0.9f), Ice, new Vector3(tilt * 3f, 0f, tilt * 2f));
            Part(PrimitiveType.Capsule, t, "Shard", new Vector3(0.5f, 0.6f, 0.2f), new Vector3(0.45f, 0.7f, 0.45f), Ice * 1.05f, new Vector3(0f, 0f, -25f));
        }

        private static void Stalagmite(Transform t, MapTheme theme)
        {
            Color color = theme == MapTheme.IceCavern ? new Color(0.72f, 0.80f, 0.88f) : theme == MapTheme.Volcano ? Basalt * 1.4f : DarkStone * 1.2f;
            Part(PrimitiveType.Capsule, t, "Base", new Vector3(0f, 0.9f, 0f), new Vector3(1.2f, 1.1f, 1.2f), color);
            Part(PrimitiveType.Capsule, t, "Tip", new Vector3(0f, 2.1f, 0f), new Vector3(0.55f, 0.9f, 0.55f), color * 1.08f);
            if (theme == MapTheme.Volcano)
            {
                // Cracks of glowing rock down the side.
                GlowPart(PrimitiveType.Cube, t, "Seam", new Vector3(0f, 1.1f, 0.58f), new Vector3(0.1f, 1.2f, 0.05f), Lava, Lava * 1.4f, new Vector3(0f, 0f, 8f));
            }
        }

        private static void Crystal(Transform t, int seed)
        {
            Color glow = new Color(0.45f, 0.8f, 1f);
            GlowPart(PrimitiveType.Capsule, t, "Crystal", new Vector3(0f, 1f, 0f), new Vector3(0.6f, 1.2f, 0.6f), glow, glow * 0.9f, new Vector3(8f, 0f, 6f));
            GlowPart(PrimitiveType.Capsule, t, "Shard", new Vector3(0.45f, 0.55f, 0.1f), new Vector3(0.35f, 0.6f, 0.35f), glow, glow * 0.7f, new Vector3(0f, 0f, -30f));
            if (seed % 2 == 0)
            {
                PointLight(t, new Vector3(0f, 1.4f, 0f), glow, 5f, 1.2f);
            }
        }

        // ---------------------------------------------------------------- fire and light
        private static void Campfire(Transform t)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 2f / 8f;
                Part(PrimitiveType.Sphere, t, "Stone", new Vector3(Mathf.Cos(angle) * 0.75f, 0.12f, Mathf.Sin(angle) * 0.75f), new Vector3(0.35f, 0.25f, 0.35f), Stone, outline: 0.8f);
            }

            foreach (float yaw in new[] { 30f, 90f, 150f })
            {
                Part(PrimitiveType.Cylinder, t, "Log", new Vector3(0f, 0.15f, 0f), new Vector3(0.16f, 0.5f, 0.16f), Bark, new Vector3(80f, yaw, 0f));
            }

            Fire(t, new Vector3(0f, 0.12f, 0f), 1.15f, false);
        }

        private static void Brazier(Transform t, MapTheme theme)
        {
            bool spirit = theme == MapTheme.Crypt;
            Part(PrimitiveType.Cylinder, t, "Stand", new Vector3(0f, 0.55f, 0f), new Vector3(0.16f, 0.55f, 0.16f), Iron);
            Part(PrimitiveType.Cylinder, t, "Bowl", new Vector3(0f, 1.15f, 0f), new Vector3(0.75f, 0.12f, 0.75f), Iron * 1.2f);
            Fire(t, new Vector3(0f, 1.18f, 0f), 1.05f, spirit);
        }

        private static void Torch(Transform t)
        {
            Part(PrimitiveType.Cylinder, t, "Pole", new Vector3(0f, 0.9f, 0f), new Vector3(0.08f, 0.9f, 0.08f), DarkWood, outline: 0.8f);
            Fire(t, new Vector3(0f, 1.8f, 0f), 0.5f, false);
        }

        // ---------------------------------------------------------------- town
        private static void Windmill(Transform t)
        {
            Part(PrimitiveType.Cylinder, t, "Tower", new Vector3(0f, 4f, 0f), new Vector3(3.2f, 4f, 3.2f), Cloth);
            Part(PrimitiveType.Cylinder, t, "Roof", new Vector3(0f, 8.4f, 0f), new Vector3(3.6f, 0.5f, 3.6f), RoofRed);
            Part(PrimitiveType.Sphere, t, "Cap", new Vector3(0f, 8.9f, 0f), new Vector3(2.6f, 1.6f, 2.6f), RoofRed);
            var hub = new GameObject("Sails").transform;
            hub.SetParent(t, false);
            hub.localPosition = new Vector3(0f, 7f, 1.9f);
            hub.gameObject.AddComponent<Spinner>().DegreesPerSecond = new Vector3(0f, 0f, 25f);
            for (int i = 0; i < 4; i++)
            {
                var blade = Part(PrimitiveType.Cube, hub, "Blade", Vector3.zero, new Vector3(0.9f, 5.5f, 0.08f), Wood);
                blade.localRotation = Quaternion.Euler(0f, 0f, i * 90f);
                blade.localPosition = blade.localRotation * new Vector3(0f, 2.9f, 0f);
            }
        }

        private static void House(Transform t, MapTheme theme, int seed)
        {
            if (EnvironmentKit.Spawn("house_" + seed % 2, t, theme, seed) != null)
            {
                return;
            }

            Color walls = seed % 2 == 0 ? Wood : new Color(0.62f, 0.50f, 0.36f);
            Part(PrimitiveType.Cube, t, "Walls", new Vector3(0f, 1.6f, 0f), new Vector3(7f, 3.2f, 5.4f), walls);
            Roof(t, new Vector3(0f, 3.2f, 0f), 7.6f, 6f, 2.4f, seed % 3 == 0 ? RoofRed : Thatch);
            Part(PrimitiveType.Cube, t, "Door", new Vector3(0f, 1f, 2.72f), new Vector3(1.2f, 2f, 0.1f), DarkWood, outline: 0f);
            GlowPart(PrimitiveType.Cube, t, "Window", new Vector3(2.2f, 1.9f, 2.72f), new Vector3(0.9f, 0.7f, 0.06f), new Color(1f, 0.8f, 0.45f), new Color(0.9f, 0.6f, 0.25f));
        }

        private static void MeadHall(Transform t, MapTheme theme)
        {
            if (EnvironmentKit.Spawn("meadhall_0", t, theme, Mathf.Abs(Mathf.RoundToInt(t.position.x * 3.1f + t.position.z * 1.7f))) != null)
            {
                return;
            }

            Part(PrimitiveType.Cube, t, "Walls", new Vector3(0f, 2.4f, 0f), new Vector3(16f, 4.8f, 11f), DarkWood * 1.15f);
            Part(PrimitiveType.Cube, t, "Base", new Vector3(0f, 0.3f, 0f), new Vector3(16.6f, 0.6f, 11.6f), Stone);
            Roof(t, new Vector3(0f, 4.8f, 0f), 17f, 12.4f, 4.6f, Thatch * 0.9f);
            foreach (float x in new[] { -8.6f, 8.6f })
            {
                var head = Part(PrimitiveType.Capsule, t, "DragonHead", new Vector3(x, 9.2f, 0f), new Vector3(0.5f, 1.1f, 0.5f), DarkWood, new Vector3(0f, 0f, x > 0f ? -35f : 35f));
                Part(PrimitiveType.Sphere, head, "Eye", new Vector3(0f, 0.7f, 0.5f), Vector3.one * 0.25f, new Color(0.9f, 0.7f, 0.2f), outline: 0f);
            }

            Part(PrimitiveType.Cube, t, "Doors", new Vector3(0f, 1.6f, -5.55f), new Vector3(2.6f, 3.2f, 0.12f), new Color(0.3f, 0.18f, 0.1f), outline: 0f);
            GlowPart(PrimitiveType.Cube, t, "Shield1", new Vector3(-4f, 3.2f, -5.55f), new Vector3(1.2f, 1.2f, 0.08f), new Color(0.75f, 0.2f, 0.15f), new Color(0.25f, 0.05f, 0.03f));
            GlowPart(PrimitiveType.Cube, t, "Shield2", new Vector3(4f, 3.2f, -5.55f), new Vector3(1.2f, 1.2f, 0.08f), new Color(0.85f, 0.7f, 0.2f), new Color(0.25f, 0.2f, 0.05f));
        }

        private static void GuildHall(Transform t)
        {
            Part(PrimitiveType.Cube, t, "Walls", new Vector3(0f, 2.2f, 0f), new Vector3(10.5f, 4.4f, 8f), Stone * 0.95f);
            Roof(t, new Vector3(0f, 4.4f, 0f), 11.2f, 8.8f, 3.2f, new Color(0.25f, 0.32f, 0.5f));
            Part(PrimitiveType.Cylinder, t, "Tower", new Vector3(4.2f, 3.5f, 3f), new Vector3(2.2f, 3.5f, 2.2f), Stone);
            Part(PrimitiveType.Capsule, t, "Spire", new Vector3(4.2f, 7.6f, 3f), new Vector3(2.2f, 1.6f, 2.2f), new Color(0.25f, 0.32f, 0.5f));
            for (int i = -1; i <= 1; i++)
            {
                Part(PrimitiveType.Cube, t, "Banner", new Vector3(i * 2.8f, 2.6f, -4.05f), new Vector3(1f, 2.2f, 0.06f),
                    i == 0 ? new Color(0.56f, 0.27f, 0.68f) : new Color(0.75f, 0.2f, 0.15f), outline: 0f);
            }
        }

        private static void ForgeHut(Transform t)
        {
            Part(PrimitiveType.Cube, t, "Walls", new Vector3(0f, 1.6f, 0f), new Vector3(7f, 3.2f, 5.6f), Stone * 0.8f);
            Roof(t, new Vector3(0f, 3.2f, 0f), 7.6f, 6.2f, 2f, DarkWood);
            Part(PrimitiveType.Cylinder, t, "Chimney", new Vector3(2.4f, 4.6f, 1.2f), new Vector3(1f, 1.8f, 1f), DarkStone);
            GlowPart(PrimitiveType.Cube, t, "ForgeMouth", new Vector3(-1.5f, 1f, 2.82f), new Vector3(1.8f, 1.2f, 0.06f), Flame, new Color(1.6f, 0.55f, 0.1f));
            PointLight(t, new Vector3(-1.5f, 1.2f, 3.6f), new Color(1f, 0.5f, 0.15f), 8f, 2.4f);
        }

        private static void Stall(Transform t, MapTheme theme, int seed)
        {
            if (EnvironmentKit.Spawn("stall_0", t, theme, seed) != null)
            {
                return;
            }

            Color stripe = seed % 2 == 0 ? new Color(0.75f, 0.2f, 0.15f) : new Color(0.2f, 0.4f, 0.7f);
            Part(PrimitiveType.Cube, t, "Counter", new Vector3(0f, 0.5f, 0f), new Vector3(2.8f, 1f, 1.2f), Wood);
            foreach (var x in new[] { -1.3f, 1.3f })
            {
                Part(PrimitiveType.Cylinder, t, "Post", new Vector3(x, 1.2f, 0.5f), new Vector3(0.1f, 1.2f, 0.1f), DarkWood, outline: 0.8f);
            }

            for (int i = 0; i < 4; i++)
            {
                Part(PrimitiveType.Cube, t, "Awning", new Vector3(-1.05f + i * 0.7f, 2.45f, 0.2f), new Vector3(0.7f, 0.06f, 1.6f),
                    i % 2 == 0 ? stripe : Cloth, new Vector3(-12f, 0f, 0f), 0.8f);
            }

            Part(PrimitiveType.Sphere, t, "Goods", new Vector3(-0.6f, 1.15f, 0f), new Vector3(0.5f, 0.3f, 0.5f), new Color(0.85f, 0.6f, 0.25f), outline: 0.8f);
        }

        private static void Well(Transform t, MapTheme theme)
        {
            if (EnvironmentKit.Spawn("well_0", t, theme, Mathf.Abs(Mathf.RoundToInt(t.position.x * 3.1f + t.position.z * 1.7f))) != null)
            {
                return;
            }

            Part(PrimitiveType.Cylinder, t, "Ring", new Vector3(0f, 0.45f, 0f), new Vector3(2f, 0.45f, 2f), Stone);
            Part(PrimitiveType.Cylinder, t, "Water", new Vector3(0f, 0.86f, 0f), new Vector3(1.6f, 0.02f, 1.6f), new Color(0.2f, 0.35f, 0.55f), outline: 0f);
            foreach (var x in new[] { -0.85f, 0.85f })
            {
                Part(PrimitiveType.Cylinder, t, "Post", new Vector3(x, 1.4f, 0f), new Vector3(0.1f, 0.6f, 0.1f), DarkWood, outline: 0.8f);
            }

            Roof(t, new Vector3(0f, 2f, 0f), 2.2f, 1.6f, 0.7f, RoofRed);
        }

        private static void Banner(Transform t, MapTheme theme, int seed)
        {
            // A hall's banners mix the red of war with deep blue.
            Color cloth = theme == MapTheme.Arena ? new Color(0.2f, 0.2f, 0.25f)
                : theme == MapTheme.Castle && seed % 2 == 0 ? new Color(0.16f, 0.24f, 0.48f)
                : new Color(0.72f, 0.18f, 0.14f);
            Part(PrimitiveType.Cylinder, t, "Pole", new Vector3(0f, 1.8f, 0f), new Vector3(0.1f, 1.8f, 0.1f), DarkWood, outline: 0.8f);
            Part(PrimitiveType.Cube, t, "Cloth", new Vector3(0f, 2.6f, 0.06f), new Vector3(0.9f, 1.6f, 0.04f), cloth, outline: 0.8f);
            Part(PrimitiveType.Cube, t, "Trim", new Vector3(0f, 2f, 0.09f), new Vector3(0.5f, 0.5f, 0.02f), new Color(0.9f, 0.75f, 0.3f), new Vector3(0f, 0f, 45f), 0f);
        }

        private static void Statue(Transform t, MapTheme theme)
        {
            if (EnvironmentKit.Spawn("statue_0", t, theme, 4) != null)
            {
                return;
            }

            Color stone = Snowy(theme) ? new Color(0.62f, 0.66f, 0.72f) : Stone;
            Part(PrimitiveType.Cube, t, "Plinth", new Vector3(0f, 0.5f, 0f), new Vector3(2.4f, 1f, 2.4f), stone * 0.85f);
            Part(PrimitiveType.Capsule, t, "Robe", new Vector3(0f, 2.3f, 0f), new Vector3(1.1f, 1.4f, 0.9f), stone);
            Part(PrimitiveType.Sphere, t, "Head", new Vector3(0f, 3.9f, 0f), Vector3.one * 0.6f, stone * 1.05f);
            Part(PrimitiveType.Cylinder, t, "Spear", new Vector3(0.75f, 2.8f, 0f), new Vector3(0.08f, 1.9f, 0.08f), stone * 0.9f);
            GlowPart(PrimitiveType.Sphere, t, "Eye", new Vector3(0f, 3.95f, 0.26f), new Vector3(0.18f, 0.1f, 0.06f), RuneGlow, RuneGlow * 1.4f);
        }

        private static void Anvil(Transform t)
        {
            Part(PrimitiveType.Cube, t, "Block", new Vector3(0f, 0.3f, 0f), new Vector3(0.6f, 0.6f, 0.5f), DarkWood);
            Part(PrimitiveType.Cube, t, "Anvil", new Vector3(0f, 0.75f, 0f), new Vector3(0.9f, 0.3f, 0.4f), Iron);
            Part(PrimitiveType.Capsule, t, "Horn", new Vector3(0.6f, 0.78f, 0f), new Vector3(0.22f, 0.25f, 0.22f), Iron, new Vector3(0f, 0f, 90f));
        }

        private static void Tent(Transform t, int seed)
        {
            if (EnvironmentKit.Spawn("tent_0", t, MapTheme.Meadow, seed) != null)
            {
                return;
            }

            Color canvas = seed % 2 == 0 ? Cloth : new Color(0.62f, 0.52f, 0.38f);
            Roof(t, Vector3.zero, 3.4f, 3.6f, 2.2f, canvas);
            Part(PrimitiveType.Cylinder, t, "Pole", new Vector3(0f, 1.2f, 1.75f), new Vector3(0.08f, 1.2f, 0.08f), DarkWood, outline: 0.8f);
        }

        private static void Palisade(Transform t, float length)
        {
            int stakes = Mathf.Max(2, Mathf.RoundToInt(length / 0.55f));
            for (int i = 0; i < stakes; i++)
            {
                float x = -length * 0.5f + (i + 0.5f) * length / stakes;
                float height = 2.2f + (i % 3) * 0.15f;
                Part(PrimitiveType.Cylinder, t, "Stake", new Vector3(x, height * 0.5f, 0f), new Vector3(0.45f, height * 0.5f, 0.45f), Wood * (0.9f + (i % 2) * 0.1f), outline: 1f);
                Part(PrimitiveType.Capsule, t, "Tip", new Vector3(x, height, 0f), new Vector3(0.3f, 0.3f, 0.3f), Wood * 0.85f, outline: 1f);
            }

            Part(PrimitiveType.Cube, t, "Rail", new Vector3(0f, 1.4f, 0.25f), new Vector3(length, 0.15f, 0.08f), DarkWood, outline: 0.8f);
        }

        // ---------------------------------------------------------------- crypt, cave, ruin
        private static void Pillar(Transform t, MapTheme theme)
        {
            Color stone = theme == MapTheme.Castle ? new Color(0.40f, 0.28f, 0.19f)
                : theme == MapTheme.Crypt || theme == MapTheme.Arena ? DarkStone * 1.25f
                : theme == MapTheme.SandTomb || theme == MapTheme.Desert ? Sandstone
                : theme == MapTheme.Jungle ? new Color(0.48f, 0.52f, 0.44f) : Stone;
            Part(PrimitiveType.Cube, t, "Foot", new Vector3(0f, 0.2f, 0f), new Vector3(1.5f, 0.4f, 1.5f), stone * 0.9f);
            Part(PrimitiveType.Cylinder, t, "Shaft", new Vector3(0f, 2f, 0f), new Vector3(1.1f, 1.8f, 1.1f), stone);
            Part(PrimitiveType.Cube, t, "Capital", new Vector3(0f, 3.9f, 0f), new Vector3(1.5f, 0.35f, 1.5f), stone * 0.9f);
        }

        private static void Coffin(Transform t, MapTheme theme)
        {
            bool tomb = theme == MapTheme.SandTomb;
            Color stone = tomb ? Sandstone * 0.9f : DarkStone * 1.3f;
            Color rune = tomb ? Gold : SpiritFlame;
            Part(PrimitiveType.Cube, t, "Box", new Vector3(0f, 0.4f, 0f), new Vector3(1.1f, 0.8f, 2.1f), stone);
            Part(PrimitiveType.Cube, t, "Lid", new Vector3(0.1f, 0.86f, 0.05f), new Vector3(1.15f, 0.14f, 2.15f), stone * 1.15f, new Vector3(0f, 6f, 2f));
            GlowPart(PrimitiveType.Cube, t, "Rune", new Vector3(0.1f, 0.94f, 0.05f), new Vector3(0.12f, 0.02f, 1.2f), rune, rune * 0.8f);
        }

        private static void Bones(Transform t)
        {
            Part(PrimitiveType.Sphere, t, "Skull", new Vector3(0f, 0.15f, 0f), new Vector3(0.32f, 0.28f, 0.34f), Bone, outline: 0.8f);
            Part(PrimitiveType.Capsule, t, "Bone1", new Vector3(0.45f, 0.06f, 0.2f), new Vector3(0.1f, 0.35f, 0.1f), Bone, new Vector3(90f, 30f, 0f), 0.8f);
            Part(PrimitiveType.Capsule, t, "Bone2", new Vector3(-0.35f, 0.06f, -0.3f), new Vector3(0.09f, 0.3f, 0.09f), Bone, new Vector3(90f, -50f, 0f), 0.8f);
        }

        private static void Wreck(Transform t)
        {
            Part(PrimitiveType.Capsule, t, "Hull", new Vector3(0f, 0.6f, 0f), new Vector3(2.2f, 2.8f, 1.6f), DarkWood, new Vector3(90f, 0f, 18f));
            Part(PrimitiveType.Cylinder, t, "Mast", new Vector3(0.4f, 1.8f, 0.6f), new Vector3(0.18f, 1.6f, 0.18f), Wood, new Vector3(25f, 0f, -15f));
            Part(PrimitiveType.Cube, t, "Sail", new Vector3(0.9f, 2.4f, 0.9f), new Vector3(1.4f, 1.2f, 0.04f), new Color(0.75f, 0.7f, 0.6f), new Vector3(20f, 10f, -15f), 0.8f);
            Part(PrimitiveType.Capsule, t, "Prow", new Vector3(0f, 1.6f, 2.6f), new Vector3(0.35f, 0.7f, 0.35f), DarkWood, new Vector3(-30f, 0f, 0f));
        }

        private static void Chains(Transform t)
        {
            for (int i = 0; i < 6; i++)
            {
                Part(PrimitiveType.Capsule, t, "Link", new Vector3(0f, 0.08f, 0.4f + i * 0.5f), new Vector3(0.14f, 0.28f, 0.14f), Iron * 1.4f,
                    new Vector3(90f, 0f, i % 2 == 0 ? 0f : 90f), 0.6f);
            }

            Part(PrimitiveType.Cylinder, t, "Stake", new Vector3(0f, 0.35f, 3.4f), new Vector3(0.2f, 0.35f, 0.2f), Iron, outline: 0.8f);
        }

        private static void GiantSkull(Transform t, MapTheme theme)
        {
            Color bone = Snowy(theme) ? Bone * 1.02f : Bone;
            Part(PrimitiveType.Sphere, t, "Cranium", new Vector3(0f, 1.6f, 0f), new Vector3(3.6f, 3f, 3.8f), bone);
            Part(PrimitiveType.Cube, t, "Jaw", new Vector3(0f, 0.45f, 1.1f), new Vector3(2.4f, 0.8f, 1.8f), bone * 0.95f, new Vector3(12f, 0f, 0f));
            Part(PrimitiveType.Sphere, t, "EyeL", new Vector3(-0.75f, 1.9f, 1.55f), new Vector3(0.8f, 0.9f, 0.6f), new Color(0.08f, 0.08f, 0.1f), outline: 0f);
            Part(PrimitiveType.Sphere, t, "EyeR", new Vector3(0.75f, 1.9f, 1.55f), new Vector3(0.8f, 0.9f, 0.6f), new Color(0.08f, 0.08f, 0.1f), outline: 0f);
            Part(PrimitiveType.Capsule, t, "Horn", new Vector3(1.6f, 3f, -0.3f), new Vector3(0.5f, 1.2f, 0.5f), bone * 0.9f, new Vector3(0f, 0f, -40f));
        }

        private static void Pool(Transform t, MapTheme theme)
        {
            Color water = theme == MapTheme.IceCavern ? new Color(0.25f, 0.55f, 0.75f) : theme == MapTheme.Desert ? new Color(0.20f, 0.58f, 0.62f)
                : new Color(0.12f, 0.2f, 0.32f);
            Part(PrimitiveType.Cylinder, t, "Water", new Vector3(0f, 0.02f, 0f), new Vector3(3f, 0.01f, 2.4f), water, outline: 0f);
        }

        // ---------------------------------------------------------------- Phase 8 themes
        private static void Cactus(Transform t, int seed)
        {
            Color green = new Color(0.36f, 0.55f, 0.30f) * (0.9f + (seed % 3) * 0.06f);
            Part(PrimitiveType.Capsule, t, "Stem", new Vector3(0f, 1.3f, 0f), new Vector3(0.6f, 1.3f, 0.6f), green);
            Part(PrimitiveType.Capsule, t, "ArmL", new Vector3(-0.55f, 1.5f, 0f), new Vector3(0.32f, 0.5f, 0.32f), green * 0.95f);
            Part(PrimitiveType.Capsule, t, "ArmLUp", new Vector3(-0.7f, 2f, 0f), new Vector3(0.3f, 0.45f, 0.3f), green * 0.95f);
            if (seed % 2 == 0)
            {
                Part(PrimitiveType.Capsule, t, "ArmR", new Vector3(0.5f, 1.1f, 0f), new Vector3(0.3f, 0.45f, 0.3f), green * 0.92f);
                Part(PrimitiveType.Capsule, t, "ArmRUp", new Vector3(0.65f, 1.5f, 0f), new Vector3(0.28f, 0.4f, 0.28f), green * 0.92f);
            }

            if (seed % 3 == 0)
            {
                Part(PrimitiveType.Sphere, t, "Flower", new Vector3(0f, 2.65f, 0f), new Vector3(0.3f, 0.18f, 0.3f), new Color(0.95f, 0.45f, 0.55f), outline: 0.8f);
            }
        }

        private static void PalmTree(Transform t, int seed)
        {
            Color trunk = new Color(0.55f, 0.43f, 0.30f);
            float lean = (seed % 5 - 2) * 4f;
            var stem = new GameObject("Stem").transform;
            stem.SetParent(t, false);
            stem.localRotation = Quaternion.Euler(lean, 0f, lean * 0.6f);
            for (int i = 0; i < 5; i++)
            {
                Part(PrimitiveType.Cylinder, stem, "Ring", new Vector3(0f, 0.45f + i * 0.85f, 0f), new Vector3(0.32f - i * 0.02f, 0.45f, 0.32f - i * 0.02f), trunk * (i % 2 == 0 ? 1f : 0.9f));
            }

            // Fronds arch up from the crown and droop at the tips.
            Color frond = new Color(0.30f, 0.56f, 0.24f);
            for (int i = 0; i < 7; i++)
            {
                var arm = new GameObject("Frond").transform;
                arm.SetParent(stem, false);
                arm.localPosition = new Vector3(0f, 4.3f, 0f);
                arm.localRotation = Quaternion.Euler(0f, i * (360f / 7f) + seed % 30, 0f);
                Color shade = frond * (i % 2 == 0 ? 1f : 0.88f);
                Part(PrimitiveType.Capsule, arm, "Rise", new Vector3(0f, 0.25f, 0.55f), new Vector3(0.28f, 0.6f, 0.07f), shade, new Vector3(65f, 0f, 0f), 1f);
                Part(PrimitiveType.Capsule, arm, "Droop", new Vector3(0f, 0.2f, 1.6f), new Vector3(0.24f, 0.6f, 0.06f), shade * 0.92f, new Vector3(120f, 0f, 0f), 1f);
            }

            Part(PrimitiveType.Sphere, stem, "Coconuts", new Vector3(0f, 4.15f, 0f), new Vector3(0.5f, 0.4f, 0.5f), new Color(0.38f, 0.28f, 0.16f), outline: 0.8f);
        }

        private static void JungleTree(Transform t, int seed)
        {
            Color bark = new Color(0.40f, 0.30f, 0.20f);
            Part(PrimitiveType.Cylinder, t, "Trunk", new Vector3(0f, 2.2f, 0f), new Vector3(0.6f, 2.2f, 0.6f), bark);
            Part(PrimitiveType.Cylinder, t, "Root1", new Vector3(0.45f, 0.35f, 0f), new Vector3(0.22f, 0.5f, 0.22f), bark * 0.9f, new Vector3(0f, 0f, -35f));
            Part(PrimitiveType.Cylinder, t, "Root2", new Vector3(-0.35f, 0.35f, 0.3f), new Vector3(0.2f, 0.45f, 0.2f), bark * 0.9f, new Vector3(30f, 0f, 30f));
            Color leaves = new Color(0.16f, 0.42f, 0.18f) * (0.9f + (seed % 3) * 0.08f);
            Part(PrimitiveType.Sphere, t, "Canopy", new Vector3(0f, 4.8f, 0f), new Vector3(4.2f, 2f, 4.2f), leaves);
            Part(PrimitiveType.Sphere, t, "Canopy2", new Vector3(1f, 4.2f, 0.6f), new Vector3(2.6f, 1.5f, 2.6f), leaves * 1.1f);
            Part(PrimitiveType.Sphere, t, "Canopy3", new Vector3(-0.9f, 4.4f, -0.7f), new Vector3(2.4f, 1.4f, 2.4f), leaves * 0.92f);
            if (seed % 3 == 0)
            {
                Part(PrimitiveType.Cylinder, t, "Vine", new Vector3(0.9f, 2.6f, 0.9f), new Vector3(0.06f, 1.6f, 0.06f), leaves * 1.2f, outline: 0f);
            }
        }

        private static void Fern(Transform t, int seed)
        {
            Color green = new Color(0.26f, 0.52f, 0.22f) * (0.9f + (seed % 4) * 0.05f);
            for (int i = 0; i < 5; i++)
            {
                float yaw = i * 72f + seed % 40;
                var leaf = Part(PrimitiveType.Capsule, t, "Leaf", Vector3.zero, new Vector3(0.22f, 0.55f, 0.06f), green, default, 0f);
                leaf.localRotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(55f, 0f, 0f);
                leaf.localPosition = Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0.3f, 0.35f);
            }
        }

        private static void LavaPool(Transform t, int seed)
        {
            Part(PrimitiveType.Cylinder, t, "Crust", new Vector3(0f, 0.02f, 0f), new Vector3(3.4f, 0.02f, 2.8f), Basalt * 0.8f, outline: 0f);
            GlowPart(PrimitiveType.Cylinder, t, "Lava", new Vector3(0f, 0.04f, 0f), new Vector3(2.8f, 0.02f, 2.2f), Lava, Lava * 1.8f);
            if (seed % 2 == 0)
            {
                PointLight(t, new Vector3(0f, 1f, 0f), new Color(1f, 0.45f, 0.15f), 7f, 1.8f);
            }
        }

        private static void Obelisk(Transform t, MapTheme theme)
        {
            bool volcano = theme == MapTheme.Volcano;
            Color stone = volcano ? Basalt * 1.5f : theme == MapTheme.Jungle ? new Color(0.48f, 0.52f, 0.44f) : Sandstone;
            Part(PrimitiveType.Cube, t, "Base", new Vector3(0f, 0.25f, 0f), new Vector3(1.6f, 0.5f, 1.6f), stone * 0.85f);
            Part(PrimitiveType.Cube, t, "Shaft", new Vector3(0f, 2.3f, 0f), new Vector3(0.9f, 3.6f, 0.9f), stone);
            Part(PrimitiveType.Cube, t, "Tip", new Vector3(0f, 4.3f, 0f), new Vector3(0.55f, 0.55f, 0.55f), volcano ? Basalt : Gold, new Vector3(45f, 0f, 45f));
            Color glow = volcano ? Lava : RuneGlow;
            GlowPart(PrimitiveType.Cube, t, "Glyphs", new Vector3(0f, 2.4f, 0.46f), new Vector3(0.3f, 1.8f, 0.04f), glow, glow * 1.3f);
        }

        private static void Urn(Transform t, int seed)
        {
            Color clay = seed % 2 == 0 ? new Color(0.68f, 0.40f, 0.24f) : Sandstone * 0.95f;
            Part(PrimitiveType.Sphere, t, "Body", new Vector3(0f, 0.55f, 0f), new Vector3(0.8f, 0.95f, 0.8f), clay);
            Part(PrimitiveType.Cylinder, t, "Neck", new Vector3(0f, 1.1f, 0f), new Vector3(0.38f, 0.14f, 0.38f), clay * 0.9f);
            Part(PrimitiveType.Cylinder, t, "Band", new Vector3(0f, 0.65f, 0f), new Vector3(0.82f, 0.05f, 0.82f), Gold, outline: 0f);
        }

        // ---------------------------------------------------------------- Phase 8 castles
        /// <summary>The jarl's high seat on a two-step dais: carved posts topped with gold, a fur over the seat.</summary>
        private static void Throne(Transform t)
        {
            Part(PrimitiveType.Cube, t, "Dais", new Vector3(0f, 0.15f, 0f), new Vector3(4.2f, 0.3f, 3f), Stone * 0.85f);
            Part(PrimitiveType.Cube, t, "Step", new Vector3(0f, 0.42f, -0.2f), new Vector3(3f, 0.25f, 2.2f), Stone * 0.95f);
            Part(PrimitiveType.Cube, t, "Seat", new Vector3(0f, 0.95f, -0.2f), new Vector3(1.5f, 0.8f, 1.1f), DarkWood);
            Part(PrimitiveType.Cube, t, "Back", new Vector3(0f, 2f, -0.7f), new Vector3(1.5f, 2.2f, 0.25f), DarkWood);
            Part(PrimitiveType.Cube, t, "Fur", new Vector3(0f, 1.4f, -0.15f), new Vector3(1.3f, 0.12f, 1.15f), new Color(0.82f, 0.80f, 0.76f));
            foreach (float x in new[] { -0.85f, 0.85f })
            {
                Part(PrimitiveType.Cylinder, t, "Post", new Vector3(x, 1.7f, -0.7f), new Vector3(0.28f, 1.3f, 0.28f), DarkWood * 1.2f);
                Part(PrimitiveType.Sphere, t, "Knob", new Vector3(x, 3.1f, -0.7f), Vector3.one * 0.38f, Gold);
                Part(PrimitiveType.Cube, t, "Arm", new Vector3(x, 1.5f, -0.2f), new Vector3(0.22f, 0.18f, 1.1f), DarkWood * 1.2f);
            }

            GlowPart(PrimitiveType.Cube, t, "Rune", new Vector3(0f, 2.4f, -0.56f), new Vector3(0.12f, 0.9f, 0.02f), Gold, Gold * 1.2f);
        }

        /// <summary>The long fire down the middle of a hall: a stone-lined trough of embers along local X.</summary>
        private static void Hearth(Transform t, float length)
        {
            foreach (float z in new[] { -0.6f, 0.6f })
            {
                Part(PrimitiveType.Cube, t, "Curb", new Vector3(0f, 0.18f, z), new Vector3(length, 0.36f, 0.35f), Stone * 0.8f);
            }

            Part(PrimitiveType.Cube, t, "CurbEnd", new Vector3(-length * 0.5f, 0.18f, 0f), new Vector3(0.35f, 0.36f, 1.55f), Stone * 0.8f);
            Part(PrimitiveType.Cube, t, "CurbEnd", new Vector3(length * 0.5f, 0.18f, 0f), new Vector3(0.35f, 0.36f, 1.55f), Stone * 0.8f);
            GlowPart(PrimitiveType.Cube, t, "Embers", new Vector3(0f, 0.12f, 0f), new Vector3(length - 0.4f, 0.1f, 0.85f), new Color(0.9f, 0.32f, 0.08f), new Color(1.2f, 0.35f, 0.05f));
            int flames = Mathf.Max(2, Mathf.RoundToInt(length / 2f));
            for (int i = 0; i < flames; i++)
            {
                float x = (i + 0.5f) / flames * (length - 1f) - (length - 1f) * 0.5f;
                Part(PrimitiveType.Cube, t, "Log", new Vector3(x, 0.3f, 0f), new Vector3(1.1f, 0.18f, 0.18f), DarkWood, new Vector3(0f, i % 2 == 0 ? 25f : -25f, 0f));
                Fire(t, new Vector3(x, 0.32f, 0f), 0.85f, false);
            }

            PointLight(t, new Vector3(-length * 0.25f, 1.6f, 0f), new Color(1f, 0.6f, 0.25f), 9f, 2.2f);
            PointLight(t, new Vector3(length * 0.25f, 1.6f, 0f), new Color(1f, 0.6f, 0.25f), 9f, 2.2f);
        }

        // ---------------------------------------------------------------- helpers
        /// <summary>A kit model, or the primitive stand-in when the model isn't there.</summary>
        private static void Kit(Transform t, string model, MapTheme theme, int seed, System.Action fallback)
        {
            if (EnvironmentKit.Spawn(model, t, theme, seed) == null)
            {
                fallback();
            }
        }

        /// <summary>A gabled roof: two slabs meeting at the ridge, running along local X.</summary>
        private static void Roof(Transform t, Vector3 eaves, float length, float depth, float rise, Color color)
        {
            float half = depth * 0.5f;
            float slope = Mathf.Sqrt(half * half + rise * rise);
            float angle = Mathf.Atan2(rise, half) * Mathf.Rad2Deg;
            Part(PrimitiveType.Cube, t, "RoofN", eaves + new Vector3(0f, rise * 0.5f, half * 0.5f), new Vector3(length, 0.25f, slope), color, new Vector3(angle, 0f, 0f));
            Part(PrimitiveType.Cube, t, "RoofS", eaves + new Vector3(0f, rise * 0.5f, -half * 0.5f), new Vector3(length, 0.25f, slope), color, new Vector3(-angle, 0f, 0f));
        }

        /// <summary>
        /// Art Pass 3: a stylized fire: a wide red-orange base, an orange body, a yellow top and a pale tip, with two side
        /// tongues, all self-lit (the bloom makes them glow), flickering with its light. Spirit fire burns blue.
        /// </summary>
        private static void Fire(Transform t, Vector3 at, float size, bool spirit)
        {
            var pivot = new GameObject("Fire").transform;
            pivot.SetParent(t, false);
            pivot.localPosition = at;
            pivot.localScale = Vector3.one * size;
            Color deep = spirit ? new Color(0.2f, 0.32f, 0.95f) : new Color(0.95f, 0.22f, 0.04f);
            Color body = spirit ? new Color(0.35f, 0.58f, 1f) : new Color(1f, 0.42f, 0.07f);
            Color top = spirit ? new Color(0.6f, 0.82f, 1f) : new Color(1f, 0.66f, 0.18f);
            Color tip = spirit ? new Color(0.88f, 0.96f, 1f) : new Color(1f, 0.88f, 0.55f);
            GlowPart(PrimitiveType.Sphere, pivot, "Base", new Vector3(0f, 0.18f, 0f), new Vector3(0.62f, 0.42f, 0.62f), deep, deep * 0.55f);
            GlowPart(PrimitiveType.Sphere, pivot, "Body", new Vector3(0f, 0.45f, 0f), new Vector3(0.46f, 0.52f, 0.46f), body, body * 0.6f);
            GlowPart(PrimitiveType.Sphere, pivot, "Top", new Vector3(0.02f, 0.76f, 0f), new Vector3(0.3f, 0.46f, 0.3f), top, top * 0.7f);
            GlowPart(PrimitiveType.Sphere, pivot, "Tip", new Vector3(0.04f, 1.03f, 0f), new Vector3(0.14f, 0.3f, 0.14f), tip, tip * 0.85f);
            foreach (float side in new[] { -1f, 1f })
            {
                var tongue = GlowPart(PrimitiveType.Sphere, pivot, "Tongue", new Vector3(side * 0.2f, 0.55f, 0.05f), new Vector3(0.18f, 0.36f, 0.18f), body,
                    body * 0.6f);
                tongue.localRotation = Quaternion.Euler(0f, 0f, side * -25f);
            }

            var light = new GameObject("Light").AddComponent<Light>();
            light.transform.SetParent(pivot, false);
            light.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            light.type = LightType.Point;
            light.color = spirit ? new Color(0.5f, 0.7f, 1f) : new Color(1f, 0.6f, 0.25f);
            light.range = 7f * Mathf.Max(0.6f, size);
            light.intensity = 2f * Mathf.Max(0.6f, size);
            light.shadows = LightShadows.None;
            pivot.gameObject.AddComponent<Visuals.FlameFlicker>().Bind(light);
        }

        private static void PointLight(Transform parent, Vector3 localPosition, Color color, float range, float intensity)
        {
            var light = new GameObject("Light").AddComponent<Light>();
            light.transform.SetParent(parent, false);
            light.transform.localPosition = localPosition;
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
        }

        private static Transform Part(PrimitiveType type, Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color color,
            Vector3 euler = default, float outline = Outline)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            // Immediately: the NavMesh is baked in this same frame, and a deferred Destroy would still be collected.
            Object.DestroyImmediate(go.GetComponent<Collider>());
            var t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = localPosition;
            t.localRotation = Quaternion.Euler(euler);
            t.localScale = localScale;
            color.a = 1f;
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = RuntimeMaterials.Lit(color, outline);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return t;
        }

        private static Transform GlowPart(PrimitiveType type, Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color color, Color emission,
            Vector3 euler = default)
        {
            var t = Part(type, parent, name, localPosition, localScale, color, euler, 0f);
            var renderer = t.GetComponent<Renderer>();
            renderer.sharedMaterial = RuntimeMaterials.Glow(color, emission);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return t;
        }
    }
}
