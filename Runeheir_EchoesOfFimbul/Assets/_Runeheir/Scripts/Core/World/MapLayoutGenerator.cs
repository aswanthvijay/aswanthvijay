using System;
using System.Collections.Generic;

namespace Runeheir.World
{
    /// <summary>
    /// Turns a <see cref="MapDefinition"/> into a <see cref="MapLayout"/>: deterministic for a given seed, so every player
    /// (and later the server) sees the same map. Fields are open ground with paths, water inlets, frozen lakes and scenery;
    /// dungeons are rooms joined by corridors inside walls; the town and the arena are laid out by hand.
    /// </summary>
    public static class MapLayoutGenerator
    {
        /// <summary>Keep scenery this far from portals, the save point, NPCs and bosses.</summary>
        private const float ClearRadius = 7f;

        /// <summary>Arrivals are this far inside the portal you come through.</summary>
        private const float ArrivalInset = 5f;

        public static MapLayout Generate(MapDefinition map)
        {
            if (map == null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            var random = new Random(map.Seed);
            switch (map.Theme)
            {
                case MapTheme.Town:
                    return Town(map, random);
                case MapTheme.Arena:
                    return Arena(map, random);
                case MapTheme.Castle:
                    return Castle(map);
                case MapTheme.Crypt:
                case MapTheme.IceCavern:
                case MapTheme.Volcano:
                case MapTheme.SandTomb:
                    return Dungeon(map, random);
                case MapTheme.Isle:
                    return Isle(map, random);
                default:
                    return Field(map, random);
            }
        }

        // ================================================================ shared
        private static MapLayout NewLayout(MapDefinition map, float cellSize)
        {
            int cells = Math.Max(4, (int)Math.Ceiling(map.Size / cellSize));
            return new MapLayout
            {
                Map = map,
                CellSize = cellSize,
                Width = cells,
                Height = cells,
                OriginX = -cells * cellSize * 0.5f,
                OriginZ = -cells * cellSize * 0.5f,
                Walkable = new bool[cells * cells],
                Tags = new CellTag[cells * cells],
            };
        }

        private static void Fill(MapLayout layout, CellTag tag)
        {
            for (int i = 0; i < layout.Walkable.Length; i++)
            {
                layout.Walkable[i] = true;
                layout.Tags[i] = tag;
            }
        }

        /// <summary>Resolves the save point, portals, NPCs, spawns and bosses that use absolute coordinates.</summary>
        private static void ResolveAbsolute(MapLayout layout)
        {
            var map = layout.Map;
            layout.SavePoint = layout.NearestWalkable(new GroundPoint(map.SaveX, map.SaveZ));
            foreach (var portal in map.Portals)
            {
                var at = layout.NearestWalkable(new GroundPoint(portal.X, portal.Z));
                layout.Portals.Add(new ResolvedPortal { Portal = portal, At = at, Arrival = Inset(layout, at, layout.SavePoint) });
            }

            foreach (var npc in map.Npcs)
            {
                layout.Npcs.Add(new ResolvedNpc { Npc = npc, At = layout.NearestWalkable(new GroundPoint(npc.X, npc.Z)) });
            }

            foreach (var spawn in map.Spawns)
            {
                layout.Spawns.Add(new ResolvedSpawn { Spawn = spawn, Center = layout.NearestWalkable(new GroundPoint(spawn.X, spawn.Z)), Radius = spawn.Radius });
            }

            foreach (var boss in map.Bosses)
            {
                layout.Bosses.Add(new ResolvedBoss { Boss = boss, At = layout.NearestWalkable(new GroundPoint(boss.X, boss.Z)) });
            }
        }

        /// <summary>A point <see cref="ArrivalInset"/> meters from <paramref name="from"/> toward <paramref name="towards"/>, on walkable ground.</summary>
        private static GroundPoint Inset(MapLayout layout, GroundPoint from, GroundPoint towards)
        {
            float distance = from.DistanceTo(towards);
            if (distance < 0.01f)
            {
                return layout.NearestWalkable(new GroundPoint(from.X, from.Z + ArrivalInset));
            }

            float t = Math.Min(1f, ArrivalInset / distance);
            var arrival = layout.NearestWalkable(new GroundPoint(from.X + (towards.X - from.X) * t, from.Z + (towards.Z - from.Z) * t));
            if (arrival.DistanceTo(from) >= MinArrivalGap)
            {
                return arrival;
            }

            // Water or a wall that way (a portal on a fjord shore): step off the portal in whichever direction is open,
            // nearest the save point first.
            var best = arrival;
            float bestScore = float.MaxValue;
            for (int i = 0; i < 16; i++)
            {
                double angle = i * Math.PI / 8;
                for (float reach = ArrivalInset; reach <= ArrivalInset * 2.5f; reach += 1f)
                {
                    var candidate = new GroundPoint(from.X + (float)Math.Cos(angle) * reach, from.Z + (float)Math.Sin(angle) * reach);
                    if (!layout.IsWalkable(candidate) || candidate.DistanceTo(from) < MinArrivalGap)
                    {
                        continue;
                    }

                    float score = candidate.DistanceTo(towards);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = candidate;
                    }

                    break;
                }
            }

            return best;
        }

        /// <summary>Arriving through a portal must not put you back on top of it.</summary>
        private const float MinArrivalGap = 3.5f;

        /// <summary>Tags an L-shaped two-cell-wide path between two points, making it walkable (paths bridge water).</summary>
        private static void CarvePath(MapLayout layout, GroundPoint a, GroundPoint b, CellTag tag, int width = 2)
        {
            layout.CellOf(a, out int ax, out int az);
            layout.CellOf(b, out int bx, out int bz);
            ax = Clamp(ax, 0, layout.Width - 1);
            az = Clamp(az, 0, layout.Height - 1);
            bx = Clamp(bx, 0, layout.Width - 1);
            bz = Clamp(bz, 0, layout.Height - 1);
            for (int x = Math.Min(ax, bx); x <= Math.Max(ax, bx); x++)
            {
                for (int w = 0; w < width; w++)
                {
                    layout.Set(x, az + w, true, tag);
                }
            }

            for (int z = Math.Min(az, bz); z <= Math.Max(az, bz); z++)
            {
                for (int w = 0; w < width; w++)
                {
                    layout.Set(bx + w, z, true, tag);
                }
            }
        }

        /// <summary>Drops walkable cells that can't be reached from the save point (islands left by water carving).</summary>
        private static void RemoveIslands(MapLayout layout)
        {
            var reached = layout.Reachable(layout.SavePoint);
            for (int i = 0; i < layout.Walkable.Length; i++)
            {
                if (layout.Walkable[i] && !reached[i])
                {
                    layout.Walkable[i] = false;
                }
            }
        }

        private static bool NearKeyPoint(MapLayout layout, GroundPoint point, float radius)
        {
            if (point.DistanceTo(layout.SavePoint) < radius)
            {
                return true;
            }

            foreach (var portal in layout.Portals)
            {
                if (point.DistanceTo(portal.At) < radius || point.DistanceTo(portal.Arrival) < radius)
                {
                    return true;
                }
            }

            foreach (var npc in layout.Npcs)
            {
                if (point.DistanceTo(npc.At) < radius)
                {
                    return true;
                }
            }

            foreach (var boss in layout.Bosses)
            {
                if (point.DistanceTo(boss.At) < radius * 1.5f)
                {
                    return true;
                }
            }

            foreach (var spawn in layout.Spawns)
            {
                if (point.DistanceTo(spawn.Center) < 2.5f)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Places a fixed prop at the first candidate spot on open ground that keeps 2 m clear of every key point.</summary>
        private static void CampProp(MapLayout layout, PropKind kind, float yaw, params GroundPoint[] candidates)
        {
            foreach (var at in candidates)
            {
                var prop = new PropPlacement { Kind = kind, At = at, Yaw = yaw };
                if (layout.IsWalkable(at) && !BlocksKeyPoint(layout, prop, 2f) && !TooCloseToProps(layout, at, PropKinds.Radius(kind) + 1f))
                {
                    layout.Props.Add(prop);
                    return;
                }
            }
        }

        private static bool BlocksKeyPoint(MapLayout layout, PropPlacement prop, float clearance)
        {
            bool Blocks(GroundPoint point) => PropKinds.EdgeDistance(prop, point) < clearance;
            if (Blocks(layout.SavePoint))
            {
                return true;
            }

            foreach (var portal in layout.Portals)
            {
                if (Blocks(portal.At) || Blocks(portal.Arrival))
                {
                    return true;
                }
            }

            foreach (var npc in layout.Npcs)
            {
                if (Blocks(npc.At))
                {
                    return true;
                }
            }

            foreach (var boss in layout.Bosses)
            {
                if (Blocks(boss.At))
                {
                    return true;
                }
            }

            return false;
        }

        private static void Prop(MapLayout layout, PropKind kind, GroundPoint at, float yaw = 0f, float scale = 1f, float length = 0f)
        {
            layout.Props.Add(new PropPlacement { Kind = kind, At = at, Yaw = yaw, Scale = scale, Length = length });
        }

        /// <summary>Scatters props over free walkable ground, away from paths and key points.</summary>
        private static void Scatter(MapLayout layout, Random random, int count, Func<Random, PropKind> pick, float minSpacing, float minScale = 0.8f,
            float maxScale = 1.3f, bool allowOnPaths = false)
        {
            int placed = 0;
            var spots = new List<GroundPoint>();
            for (int attempt = 0; attempt < count * 12 && placed < count; attempt++)
            {
                int x = random.Next(layout.Width);
                int z = random.Next(layout.Height);
                if (!layout.IsWalkable(x, z) || (!allowOnPaths && layout.TagAt(x, z) == CellTag.Path))
                {
                    continue;
                }

                var center = layout.CellCenter(x, z);
                var at = new GroundPoint(center.X + Range(random, -0.4f, 0.4f) * layout.CellSize, center.Z + Range(random, -0.4f, 0.4f) * layout.CellSize);
                if (NearKeyPoint(layout, at, ClearRadius) || TooClose(spots, at, minSpacing) || TooCloseToProps(layout, at, minSpacing * 0.6f))
                {
                    continue;
                }

                spots.Add(at);
                Prop(layout, pick(random), at, Range(random, 0f, 360f), Range(random, minScale, maxScale));
                placed++;
            }
        }

        private static bool TooClose(List<GroundPoint> points, GroundPoint at, float spacing)
        {
            foreach (var point in points)
            {
                if (point.DistanceTo(at) < spacing)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TooCloseToProps(MapLayout layout, GroundPoint at, float spacing)
        {
            foreach (var prop in layout.Props)
            {
                if (prop.At.DistanceTo(at) < spacing)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>A ring of scenery along the outer cells so a field's edge reads as forest or rock, not a cliff.</summary>
        private static void Border(MapLayout layout, Random random, Func<Random, PropKind> pick, float spacing)
        {
            float half = layout.Width * layout.CellSize * 0.5f - layout.CellSize * 0.6f;
            for (float t = -half; t <= half; t += spacing)
            {
                foreach (var at in new[]
                         {
                             new GroundPoint(t, -half), new GroundPoint(t, half), new GroundPoint(-half, t), new GroundPoint(half, t),
                         })
                {
                    var jittered = new GroundPoint(at.X + Range(random, -1.5f, 1.5f), at.Z + Range(random, -1.5f, 1.5f));
                    if (!layout.CellOf(jittered, out int x, out int z) || !layout.IsWalkable(x, z) || layout.TagAt(x, z) == CellTag.Path
                        || NearKeyPoint(layout, jittered, ClearRadius))
                    {
                        continue;
                    }

                    Prop(layout, pick(random), jittered, Range(random, 0f, 360f), Range(random, 1f, 1.5f));
                }
            }
        }

        // ================================================================ fields
        private static MapLayout Field(MapDefinition map, Random random)
        {
            var layout = NewLayout(map, 5f);
            Fill(layout, map.Theme == MapTheme.Tundra ? CellTag.Snow : CellTag.Ground);
            if (map.Theme == MapTheme.Fjord)
            {
                CarveInlets(layout, random);
                layout.HasWater = true;
            }

            if (map.Theme == MapTheme.Tundra)
            {
                FrozenLakes(layout, random);
            }

            ResolveAbsolute(layout);
            if (map.Theme == MapTheme.Coast)
            {
                // After the key points are placed, so the sea keeps clear of the camp, the roads' ends and the spawns.
                CarveCoast(layout, random);
                layout.HasWater = true;
            }

            foreach (var portal in layout.Portals)
            {
                CarvePath(layout, layout.SavePoint, portal.At, CellTag.Path);
            }

            RemoveIslands(layout);
            Respot(layout);

            // A camp at the save point: a fire and a tent, on whichever side leaves the courier and the roads clear.
            var save = layout.SavePoint;
            CampProp(layout, PropKind.Campfire, 0f,
                new GroundPoint(save.X + 3f, save.Z - 3f), new GroundPoint(save.X - 3f, save.Z - 3f), new GroundPoint(save.X + 3f, save.Z + 3f), new GroundPoint(save.X - 3f, save.Z + 3f));
            CampProp(layout, PropKind.Tent, 30f,
                new GroundPoint(save.X - 6f, save.Z - 5f), new GroundPoint(save.X + 6f, save.Z - 5f), new GroundPoint(save.X - 6f, save.Z + 5f), new GroundPoint(save.X + 6f, save.Z + 5f));

            switch (map.Theme)
            {
                case MapTheme.BirchForest:
                    Border(layout, random, r => r.Next(4) == 0 ? PropKind.Tree : PropKind.BirchTree, 5f);
                    Scatter(layout, random, 130, r => r.Next(10) < 7 ? PropKind.BirchTree : r.Next(2) == 0 ? PropKind.Tree : PropKind.Log, 5.5f);
                    Scatter(layout, random, 14, r => r.Next(2) == 0 ? PropKind.Runestone : PropKind.Mushroom, 8f);
                    Scatter(layout, random, 18, _ => PropKind.Rock, 7f, 0.6f, 1.4f);
                    break;
                case MapTheme.Fjord:
                    Border(layout, random, r => r.Next(3) == 0 ? PropKind.Tree : PropKind.Rock, 6f);
                    Scatter(layout, random, 45, r => r.Next(3) == 0 ? PropKind.Tree : PropKind.Rock, 8f, 0.8f, 2f);
                    Scatter(layout, random, 4, _ => PropKind.Wreck, 20f, 1f, 1.2f);
                    Scatter(layout, random, 8, _ => PropKind.Runestone, 14f);
                    break;
                case MapTheme.Desert:
                    Border(layout, random, r => r.Next(3) == 0 ? PropKind.Cactus : PropKind.Rock, 7f);
                    Oases(layout, random);
                    Scatter(layout, random, 70, r => r.Next(5) < 2 ? PropKind.Cactus : r.Next(3) == 0 ? PropKind.DeadTree : PropKind.Rock, 8f, 0.7f, 1.8f);
                    Scatter(layout, random, 10, r => r.Next(2) == 0 ? PropKind.Bones : PropKind.GiantSkull, 18f, 0.8f, 1.3f);
                    Scatter(layout, random, 6, _ => PropKind.Obelisk, 22f, 0.9f, 1.3f);
                    break;
                case MapTheme.Jungle:
                    Border(layout, random, r => r.Next(3) == 0 ? PropKind.PalmTree : PropKind.JungleTree, 5f);
                    Scatter(layout, random, 120, r => r.Next(10) < 5 ? PropKind.JungleTree : r.Next(2) == 0 ? PropKind.PalmTree : PropKind.Fern, 5.5f, 0.8f, 1.4f);
                    Scatter(layout, random, 30, r => r.Next(3) == 0 ? PropKind.Mushroom : PropKind.Fern, 6f, 0.8f, 1.5f);
                    Scatter(layout, random, 8, r => r.Next(2) == 0 ? PropKind.Pillar : PropKind.Obelisk, 18f, 0.8f, 1.2f);
                    break;
                case MapTheme.Coast:
                    Border(layout, random, r => r.Next(2) == 0 ? PropKind.PalmTree : PropKind.Rock, 6f);
                    Scatter(layout, random, 40, r => r.Next(5) < 3 ? PropKind.PalmTree : PropKind.Rock, 8f, 0.8f, 1.6f);
                    Scatter(layout, random, 4, _ => PropKind.Wreck, 20f, 1f, 1.2f);
                    Scatter(layout, random, 10, _ => PropKind.Fern, 10f);
                    break;
                case MapTheme.Tundra:
                    Border(layout, random, r => r.Next(2) == 0 ? PropKind.IceSpike : PropKind.Rock, 7f);
                    Scatter(layout, random, 40, r => r.Next(3) == 0 ? PropKind.DeadTree : r.Next(2) == 0 ? PropKind.IceSpike : PropKind.Rock, 9f, 0.8f, 2f);
                    Scatter(layout, random, 5, _ => PropKind.GiantSkull, 25f, 1f, 1.4f);
                    Scatter(layout, random, 8, _ => PropKind.Runestone, 14f);
                    break;
                default:
                    Border(layout, random, r => r.Next(5) == 0 ? PropKind.Rock : PropKind.Tree, 6f);
                    Scatter(layout, random, 55, r => r.Next(4) == 0 ? PropKind.Rock : PropKind.Tree, 8f);
                    Scatter(layout, random, 10, _ => PropKind.Runestone, 14f);
                    Scatter(layout, random, 2, _ => PropKind.Windmill, 40f, 1f, 1f);
                    break;
            }

            Undergrowth(layout, random, map.Theme);
            return layout;
        }

        /// <summary>
        /// Art Pass 3: what grows between the trees: bushes and stumps (which block, like small rocks) and clumps of
        /// wildflowers and ferns (which you walk through). Green lands get the most; snow, sand and ash the least.
        /// </summary>
        private static void Undergrowth(MapLayout layout, Random random, MapTheme theme)
        {
            float area = layout.Width * layout.CellSize * layout.Height * layout.CellSize / (160f * 160f);
            int Count(int perField) => Math.Max(1, (int)(perField * area));
            switch (theme)
            {
                case MapTheme.Desert:
                case MapTheme.Volcano:
                    return;
                case MapTheme.Tundra:
                    Scatter(layout, random, Count(18), r => r.Next(3) == 0 ? PropKind.Stump : PropKind.Bush, 9f, 0.7f, 1.2f);
                    return;
                case MapTheme.Fjord:
                    Scatter(layout, random, Count(35), r => r.Next(4) == 0 ? PropKind.Stump : PropKind.Bush, 7f, 0.7f, 1.3f);
                    Scatter(layout, random, Count(30), r => r.Next(2) == 0 ? PropKind.Fern : PropKind.Flowers, 6f, 0.8f, 1.3f);
                    return;
                default:
                    Scatter(layout, random, Count(55), r => r.Next(5) == 0 ? PropKind.Stump : PropKind.Bush, 6f, 0.7f, 1.4f);
                    Scatter(layout, random, Count(90), r => r.Next(3) == 0 ? PropKind.Fern : PropKind.Flowers, 4.5f, 0.8f, 1.4f);
                    return;
            }
        }

        /// <summary>
        /// Phase 8 coasts: open sea along the west and south edges, its shoreline ragged, a few coves cut inland. Water never
        /// covers a key point (the save point, portals, NPCs, spawns): those stay on headlands.
        /// </summary>
        private static void CarveCoast(MapLayout layout, Random random)
        {
            void Sea(int cx, int cz)
            {
                if (cx < 0 || cz < 0 || cx >= layout.Width || cz >= layout.Height || NearKeyPoint(layout, layout.CellCenter(cx, cz), 10f))
                {
                    return;
                }

                foreach (var spawn in layout.Spawns)
                {
                    if (layout.CellCenter(cx, cz).DistanceTo(spawn.Center) < 8f)
                    {
                        return;
                    }
                }

                layout.Set(cx, cz, false);
            }

            int west = Range(random, 2, 4);
            int south = Range(random, 2, 4);
            for (int z = 0; z < layout.Height; z++)
            {
                west = Clamp(west + random.Next(3) - 1, 1, 5);
                for (int x = 0; x < west; x++)
                {
                    Sea(x, z);
                }
            }

            for (int x = 0; x < layout.Width; x++)
            {
                south = Clamp(south + random.Next(3) - 1, 1, 5);
                for (int z = 0; z < south; z++)
                {
                    Sea(x, z);
                }
            }

            for (int cove = 0; cove < 3; cove++)
            {
                bool fromWest = cove % 2 == 0;
                int x = fromWest ? 3 : Range(random, layout.Width / 4, layout.Width * 3 / 4);
                int z = fromWest ? Range(random, layout.Height / 4, layout.Height * 3 / 4) : 3;
                int length = Range(random, layout.Width / 8, layout.Width / 5);
                for (int step = 0; step < length; step++)
                {
                    for (int w = -1; w <= 1; w++)
                    {
                        Sea(fromWest ? x : x + w, fromWest ? z + w : z);
                    }

                    if (fromWest)
                    {
                        x++;
                        z += random.Next(3) - 1;
                    }
                    else
                    {
                        z++;
                        x += random.Next(3) - 1;
                    }
                }
            }
        }

        /// <summary>Phase 8 deserts: a few oases (a pool ringed by palms) away from the roads and the camp.</summary>
        private static void Oases(MapLayout layout, Random random)
        {
            int placed = 0;
            for (int attempt = 0; attempt < 60 && placed < 3; attempt++)
            {
                int x = Range(random, layout.Width / 6, layout.Width * 5 / 6);
                int z = Range(random, layout.Height / 6, layout.Height * 5 / 6);
                var center = layout.CellCenter(x, z);
                if (!layout.IsWalkable(x, z) || layout.TagAt(x, z) == CellTag.Path || NearKeyPoint(layout, center, 16f))
                {
                    continue;
                }

                Prop(layout, PropKind.Pool, center, Range(random, 0f, 360f), Range(random, 1.2f, 1.8f));
                for (int i = 0; i < 4; i++)
                {
                    double angle = i * Math.PI / 2 + random.NextDouble() * 0.6;
                    var palm = new GroundPoint(center.X + (float)Math.Cos(angle) * 5.5f, center.Z + (float)Math.Sin(angle) * 5.5f);
                    if (layout.CellOf(palm, out int px, out int pz) && layout.IsWalkable(px, pz) && layout.TagAt(px, pz) != CellTag.Path)
                    {
                        Prop(layout, PropKind.PalmTree, palm, Range(random, 0f, 360f), Range(random, 0.9f, 1.2f));
                    }
                }

                placed++;
            }
        }

        /// <summary>Fjord inlets: crooked channels of water cut in from the west and north edges.</summary>
        private static void CarveInlets(MapLayout layout, Random random)
        {
            int inlets = 4;
            for (int i = 0; i < inlets; i++)
            {
                bool fromWest = i % 2 == 0;
                int x = fromWest ? 0 : Range(random, layout.Width / 5, layout.Width * 4 / 5);
                int z = fromWest ? Range(random, layout.Height / 5, layout.Height * 4 / 5) : layout.Height - 1;
                int length = Range(random, layout.Width / 4, layout.Width / 2);
                int width = Range(random, 2, 4);
                for (int step = 0; step < length; step++)
                {
                    for (int w = -width / 2; w <= width / 2; w++)
                    {
                        if (fromWest)
                        {
                            layout.Set(x, z + w, false);
                        }
                        else
                        {
                            layout.Set(x + w, z, false);
                        }
                    }

                    if (fromWest)
                    {
                        x++;
                        z += random.Next(3) - 1;
                    }
                    else
                    {
                        z--;
                        x += random.Next(3) - 1;
                    }

                    width = Clamp(width + random.Next(3) - 1, 1, 4);
                }
            }

            // The sea along the west edge.
            for (int z = 0; z < layout.Height; z++)
            {
                layout.Set(0, z, false);
            }
        }

        private static void FrozenLakes(MapLayout layout, Random random)
        {
            for (int lake = 0; lake < 3; lake++)
            {
                int cx = Range(random, layout.Width / 5, layout.Width * 4 / 5);
                int cz = Range(random, layout.Height / 5, layout.Height * 4 / 5);
                int radius = Range(random, 2, 4);
                for (int z = cz - radius; z <= cz + radius; z++)
                {
                    for (int x = cx - radius; x <= cx + radius; x++)
                    {
                        if ((x - cx) * (x - cx) + (z - cz) * (z - cz) <= radius * radius)
                        {
                            layout.SetTag(x, z, CellTag.Ice);
                        }
                    }
                }
            }
        }

        /// <summary>After islands were dropped, move anything that ended up on water back to land.</summary>
        private static void Respot(MapLayout layout)
        {
            foreach (var spawn in layout.Spawns)
            {
                spawn.Center = layout.NearestWalkable(spawn.Center);
            }

            foreach (var boss in layout.Bosses)
            {
                boss.At = layout.NearestWalkable(boss.At);
            }

            foreach (var npc in layout.Npcs)
            {
                npc.At = layout.NearestWalkable(npc.At);
            }

            foreach (var portal in layout.Portals)
            {
                portal.At = layout.NearestWalkable(portal.At);
                portal.Arrival = layout.NearestWalkable(portal.Arrival);
            }
        }

        // ================================================================ Lyngvi (MVP isle)
        private static MapLayout Isle(MapDefinition map, Random random)
        {
            var layout = NewLayout(map, 4f);
            layout.HasWater = true;
            const float islandZ = 10f;
            const float islandRadius = 34f;
            for (int z = 0; z < layout.Height; z++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    var center = layout.CellCenter(x, z);
                    float dx = center.X;
                    float dz = center.Z - islandZ;
                    if (dx * dx + dz * dz <= islandRadius * islandRadius)
                    {
                        layout.Set(x, z, true, CellTag.Snow);
                    }
                }
            }

            // The rune bridge: a straight causeway from each portal (out over the sea) to the island, carved before the
            // points are resolved so the portal stays at the far end instead of snapping onto the shore.
            foreach (var portal in map.Portals)
            {
                CarvePath(layout, new GroundPoint(portal.X, portal.Z), new GroundPoint(0f, islandZ - islandRadius + 4f), CellTag.Path);
            }

            ResolveAbsolute(layout);

            RemoveIslands(layout);
            Respot(layout);

            // Gleipnir: chains staked in a ring around where Fenrir lies bound, under the rock of the gods.
            var lair = layout.Bosses.Count > 0 ? layout.Bosses[0].At : new GroundPoint(0f, islandZ + 12f);
            Prop(layout, PropKind.Statue, new GroundPoint(lair.X, lair.Z + 11f), 180f, 1.8f);
            for (int i = 0; i < 6; i++)
            {
                double angle = i * Math.PI / 3;
                Prop(layout, PropKind.Chains, new GroundPoint(lair.X + (float)Math.Sin(angle) * 7f, lair.Z + (float)Math.Cos(angle) * 7f), (float)(angle * 180 / Math.PI), 1.2f);
            }

            Border(layout, random, r => r.Next(2) == 0 ? PropKind.IceSpike : PropKind.Rock, 6f);
            Scatter(layout, random, 18, r => r.Next(2) == 0 ? PropKind.IceSpike : PropKind.DeadTree, 8f, 0.8f, 1.8f);
            return layout;
        }

        // ================================================================ Vigrid Haven
        private static MapLayout Town(MapDefinition map, Random random)
        {
            var layout = NewLayout(map, 4f);
            Fill(layout, CellTag.Ground);
            ResolveAbsolute(layout);

            // The paved plaza and the roads out of it.
            for (int z = 0; z < layout.Height; z++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    var center = layout.CellCenter(x, z);
                    if (center.X * center.X + center.Z * center.Z <= 18f * 18f)
                    {
                        layout.SetTag(x, z, CellTag.Paving);
                    }
                }
            }

            foreach (var portal in layout.Portals)
            {
                CarvePath(layout, new GroundPoint(0f, 0f), portal.At, CellTag.Paving);
            }

            foreach (var npc in layout.Npcs)
            {
                CarvePath(layout, new GroundPoint(0f, 0f), npc.At, CellTag.Paving, 1);
            }

            float half = map.Size * 0.5f;

            // Buildings (blocking) around the plaza.
            Prop(layout, PropKind.MeadHall, new GroundPoint(0f, 38f), 0f, 1f);
            Prop(layout, PropKind.ForgeHut, new GroundPoint(34f, 12f), -90f, 1f);
            Prop(layout, PropKind.GuildHall, new GroundPoint(-34f, 22f), 90f, 1f);
            Prop(layout, PropKind.House, new GroundPoint(-36f, -26f), 20f, 1f);
            Prop(layout, PropKind.House, new GroundPoint(36f, -28f), -15f, 1f);
            Prop(layout, PropKind.House, new GroundPoint(26f, 36f), 0f, 0.9f);
            Prop(layout, PropKind.Statue, new GroundPoint(0f, 0f), 180f, 1.4f);
            Prop(layout, PropKind.Well, new GroundPoint(10f, -8f));
            Prop(layout, PropKind.Stall, new GroundPoint(-14f, -12f), 30f);
            Prop(layout, PropKind.Stall, new GroundPoint(14f, -13f), -30f);
            Prop(layout, PropKind.Stall, new GroundPoint(-25f, -9f), 40f);
            Prop(layout, PropKind.Stall, new GroundPoint(25f, -10f), -40f);
            Prop(layout, PropKind.Anvil, new GroundPoint(27f, 6f), 0f);

            // Braziers and banners ring the plaza.
            for (int i = 0; i < 8; i++)
            {
                double angle = (i + 0.5) * Math.PI / 4;
                var at = new GroundPoint((float)Math.Sin(angle) * 20f, (float)Math.Cos(angle) * 20f);
                Prop(layout, i % 2 == 0 ? PropKind.Brazier : PropKind.Banner, at, (float)(angle * 180 / Math.PI));
            }

            // Palisade around the town, with gaps where roads leave.
            float wall = half - 3f;
            for (float t = -wall; t < wall; t += 8f)
            {
                foreach (var (at, yaw) in new[]
                         {
                             (new GroundPoint(t + 4f, -wall), 0f), (new GroundPoint(t + 4f, wall), 0f),
                             (new GroundPoint(-wall, t + 4f), 90f), (new GroundPoint(wall, t + 4f), 90f),
                         })
                {
                    if (!NearKeyPoint(layout, at, 8f) && layout.CellOf(at, out int x, out int z) && layout.TagAt(x, z) != CellTag.Paving)
                    {
                        Prop(layout, PropKind.Palisade, at, yaw, 1f, 8f);
                    }
                }
            }

            // Golden pines inside the walls.
            Scatter(layout, random, 26, _ => PropKind.Tree, 7f, 0.9f, 1.3f);
            Scatter(layout, random, 10, _ => PropKind.Torch, 9f, 1f, 1f);
            return layout;
        }

        // ================================================================ Hall of Branches
        private static MapLayout Arena(MapDefinition map, Random random)
        {
            var layout = NewLayout(map, 3f);
            const float radius = 25f;
            for (int z = 0; z < layout.Height; z++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    var center = layout.CellCenter(x, z);
                    if (center.X * center.X + center.Z * center.Z <= radius * radius)
                    {
                        layout.Set(x, z, true, CellTag.Paving);
                    }
                }
            }

            ResolveAbsolute(layout);
            for (int i = 0; i < 12; i++)
            {
                double angle = i * Math.PI / 6;
                var at = new GroundPoint((float)Math.Sin(angle) * (radius - 2f), (float)Math.Cos(angle) * (radius - 2f));
                if (!NearKeyPoint(layout, at, 6f))
                {
                    Prop(layout, i % 2 == 0 ? PropKind.Pillar : PropKind.Brazier, at, (float)(angle * 180 / Math.PI));
                }
            }

            Prop(layout, PropKind.Runestone, new GroundPoint(0f, 0f), 0f, 1.5f);
            AddWalls(layout);
            return layout;
        }

        // ================================================================ War of the Realms halls
        /// <summary>
        /// A long Norse hall running north from its gate: a hearth down the middle, two rows of pillars, banners on the long
        /// walls, braziers either side of the Jarlstone's dais and the jarl's high seat at the far end, where the hoards gather.
        /// </summary>
        private static MapLayout Castle(MapDefinition map)
        {
            var layout = NewLayout(map, 2f);
            const float halfWidth = 15f;
            const float halfLength = 30f;
            for (int z = 0; z < layout.Height; z++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    var center = layout.CellCenter(x, z);
                    if (Math.Abs(center.X) <= halfWidth && Math.Abs(center.Z) <= halfLength)
                    {
                        layout.Set(x, z, true, CellTag.Paving);
                    }
                }
            }

            ResolveAbsolute(layout);
            Prop(layout, PropKind.Hearth, new GroundPoint(0f, 0f), 90f, 1f, 8f);
            for (float z = -18f; z <= 22f; z += 8f)
            {
                Prop(layout, PropKind.Pillar, new GroundPoint(-9f, z), 0f, 0.9f);
                Prop(layout, PropKind.Pillar, new GroundPoint(9f, z), 0f, 0.9f);
            }

            for (float z = -14f; z <= 18f; z += 8f)
            {
                Prop(layout, PropKind.Banner, new GroundPoint(-halfWidth + 0.6f, z), 90f);
                Prop(layout, PropKind.Banner, new GroundPoint(halfWidth - 0.6f, z), -90f);
            }

            Prop(layout, PropKind.Brazier, new GroundPoint(-3.5f, 14f));
            Prop(layout, PropKind.Brazier, new GroundPoint(3.5f, 14f));
            Prop(layout, PropKind.Throne, new GroundPoint(0f, halfLength - 3f), 180f);
            Prop(layout, PropKind.Statue, new GroundPoint(-11.5f, halfLength - 3f), 150f, 0.8f);
            Prop(layout, PropKind.Statue, new GroundPoint(11.5f, halfLength - 3f), 210f, 0.8f);
            Prop(layout, PropKind.Torch, new GroundPoint(-4f, -halfLength + 4f));
            Prop(layout, PropKind.Torch, new GroundPoint(4f, -halfLength + 4f));
            AddWalls(layout);
            return layout;
        }

        // ================================================================ dungeons
        private static MapLayout Dungeon(MapDefinition map, Random random)
        {
            var layout = NewLayout(map, 4f);
            layout.HasWater = map.Theme == MapTheme.IceCavern;
            int w = layout.Width;
            int h = layout.Height;
            bool bossFloor = map.Bosses.Count > 0;

            // The deepest room first (north), then the entrance (south), then the rest wherever they fit.
            int deepSize = bossFloor ? 8 : 6;
            var deep = new CellRect(Range(random, 2, w - deepSize - 2), h - deepSize - 2, deepSize, deepSize);
            var entrance = new CellRect(Range(random, 2, w - 7), 2, 5, 5);
            layout.Rooms.Add(entrance);
            int extra = Range(random, 5, 8);
            for (int attempt = 0; attempt < 400 && layout.Rooms.Count < extra + 1; attempt++)
            {
                int rw = Range(random, 4, 7);
                int rh = Range(random, 4, 7);
                var room = new CellRect(Range(random, 2, w - rw - 2), Range(random, 2, h - rh - 2), rw, rh);
                bool free = !room.Overlaps(deep, 2);
                foreach (var other in layout.Rooms)
                {
                    free &= !room.Overlaps(other, 2);
                }

                if (free)
                {
                    layout.Rooms.Add(room);
                }
            }

            layout.Rooms.Add(deep); // always last

            foreach (var room in layout.Rooms)
            {
                for (int z = room.Z; z < room.Z + room.H; z++)
                {
                    for (int x = room.X; x < room.X + room.W; x++)
                    {
                        layout.Set(x, z, true, CellTag.Ground);
                    }
                }
            }

            // Corridors: a minimum spanning tree over the rooms plus two loops.
            foreach (var (a, b) in Connections(layout.Rooms, random))
            {
                var ca = layout.CellCenter(layout.Rooms[a].CenterX, layout.Rooms[a].CenterZ);
                var cb = layout.CellCenter(layout.Rooms[b].CenterX, layout.Rooms[b].CenterZ);
                CarveCorridor(layout, ca, cb);
            }

            var entranceCenter = layout.CellCenter(entrance.CenterX, entrance.CenterZ);
            var deepCenter = layout.CellCenter(deep.CenterX, deep.CenterZ);
            layout.SavePoint = entranceCenter;

            int entranceSlot = 0;
            int deepSlot = 0;
            foreach (var portal in map.Portals)
            {
                // Portals sit against the room's far wall so the middle stays free for fights. Several ways out of the
                // entrance line up along its wall; several ways deeper go to the deep room first, then the other rooms.
                CellRect room;
                int slot = 0;
                if (portal.AtEntrance)
                {
                    room = entrance;
                    slot = entranceSlot++;
                }
                else
                {
                    int middle = layout.Rooms.Count - 2;
                    room = deepSlot == 0 || middle < 1 ? deep : layout.Rooms[1 + (middle - 1 - (deepSlot - 1) % middle + middle) % middle];
                    deepSlot++;
                }

                int offset = (slot % 2 == 0 ? 1 : -1) * ((slot + 1) / 2) * 2;
                int cx = Math.Max(room.X, Math.Min(room.X + room.W - 1, room.CenterX + offset));
                var at = layout.CellCenter(cx, portal.AtEntrance ? room.Z : room.Z + room.H - 1);
                var arrivalTowards = layout.CellCenter(room.CenterX, room.CenterZ);
                layout.Portals.Add(new ResolvedPortal { Portal = portal, At = at, Arrival = Inset(layout, at, arrivalTowards) });
            }

            foreach (var boss in map.Bosses)
            {
                layout.Bosses.Add(new ResolvedBoss { Boss = boss, At = deepCenter });
            }

            foreach (var npc in map.Npcs)
            {
                layout.Npcs.Add(new ResolvedNpc { Npc = npc, At = layout.NearestWalkable(new GroundPoint(entranceCenter.X - 6f, entranceCenter.Z)) });
            }

            // Spawn groups go round the rooms between the entrance and the deep room (the deep room too if it has no boss).
            int firstRoom = 1;
            int lastRoom = bossFloor ? layout.Rooms.Count - 2 : layout.Rooms.Count - 1;
            if (lastRoom < firstRoom)
            {
                lastRoom = layout.Rooms.Count - 1;
            }

            int roomCount = lastRoom - firstRoom + 1;
            for (int i = 0; i < map.Spawns.Count; i++)
            {
                var spawn = map.Spawns[i];
                var room = layout.Rooms[firstRoom + i % roomCount];
                float roomRadius = Math.Min(room.W, room.H) * layout.CellSize * 0.5f - 1f;
                layout.Spawns.Add(new ResolvedSpawn
                {
                    Spawn = spawn,
                    Center = layout.CellCenter(room.CenterX, room.CenterZ),
                    Radius = Math.Max(2f, Math.Min(spawn.Radius, roomRadius)),
                });
            }

            AddWalls(layout);
            DungeonProps(layout, random);
            return layout;
        }

        private static IEnumerable<(int a, int b)> Connections(List<CellRect> rooms, Random random)
        {
            int n = rooms.Count;
            var inTree = new bool[n];
            inTree[0] = true;
            var edges = new List<(int, int)>();
            for (int added = 1; added < n; added++)
            {
                int bestA = -1;
                int bestB = -1;
                int bestDistance = int.MaxValue;
                for (int a = 0; a < n; a++)
                {
                    if (!inTree[a])
                    {
                        continue;
                    }

                    for (int b = 0; b < n; b++)
                    {
                        if (inTree[b])
                        {
                            continue;
                        }

                        int distance = Math.Abs(rooms[a].CenterX - rooms[b].CenterX) + Math.Abs(rooms[a].CenterZ - rooms[b].CenterZ);
                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            bestA = a;
                            bestB = b;
                        }
                    }
                }

                inTree[bestB] = true;
                edges.Add((bestA, bestB));
            }

            for (int loop = 0; loop < 2 && n > 3; loop++)
            {
                int a = random.Next(n);
                int b = random.Next(n);
                if (a != b)
                {
                    edges.Add((a, b));
                }
            }

            return edges;
        }

        private static void CarveCorridor(MapLayout layout, GroundPoint a, GroundPoint b)
        {
            layout.CellOf(a, out int ax, out int az);
            layout.CellOf(b, out int bx, out int bz);
            for (int x = Math.Min(ax, bx); x <= Math.Max(ax, bx); x++)
            {
                for (int w = 0; w < 2; w++)
                {
                    if (!layout.IsWalkable(x, az + w))
                    {
                        layout.Set(x, az + w, true, CellTag.Path);
                    }
                }
            }

            for (int z = Math.Min(az, bz); z <= Math.Max(az, bz); z++)
            {
                for (int w = 0; w < 2; w++)
                {
                    if (!layout.IsWalkable(bx + w, z))
                    {
                        layout.Set(bx + w, z, true, CellTag.Path);
                    }
                }
            }
        }

        /// <summary>Walls on every edge between walkable and solid ground, merged into straight runs.</summary>
        private static void AddWalls(MapLayout layout)
        {
            float s = layout.CellSize;

            // Horizontal edges: between (x, z-1) and (x, z).
            for (int z = 0; z <= layout.Height; z++)
            {
                int runStart = -1;
                for (int x = 0; x <= layout.Width; x++)
                {
                    bool edge = x < layout.Width && layout.IsWalkable(x, z) != layout.IsWalkable(x, z - 1);
                    if (edge && runStart < 0)
                    {
                        runStart = x;
                    }
                    else if (!edge && runStart >= 0)
                    {
                        float wz = layout.OriginZ + z * s;
                        layout.Walls.Add(new WallSegment
                        {
                            From = new GroundPoint(layout.OriginX + runStart * s, wz),
                            To = new GroundPoint(layout.OriginX + x * s, wz),
                        });
                        runStart = -1;
                    }
                }
            }

            // Vertical edges: between (x-1, z) and (x, z).
            for (int x = 0; x <= layout.Width; x++)
            {
                int runStart = -1;
                for (int z = 0; z <= layout.Height; z++)
                {
                    bool edge = z < layout.Height && layout.IsWalkable(x, z) != layout.IsWalkable(x - 1, z);
                    if (edge && runStart < 0)
                    {
                        runStart = z;
                    }
                    else if (!edge && runStart >= 0)
                    {
                        float wx = layout.OriginX + x * s;
                        layout.Walls.Add(new WallSegment
                        {
                            From = new GroundPoint(wx, layout.OriginZ + runStart * s),
                            To = new GroundPoint(wx, layout.OriginZ + z * s),
                        });
                        runStart = -1;
                    }
                }
            }
        }

        private static void DungeonProps(MapLayout layout, Random random)
        {
            var theme = layout.Map.Theme;
            if (theme == MapTheme.Volcano || theme == MapTheme.SandTomb)
            {
                HotDungeonProps(layout, random, theme == MapTheme.Volcano);
                return;
            }

            bool crypt = theme == MapTheme.Crypt;
            float s = layout.CellSize;
            foreach (var room in layout.Rooms)
            {
                // Light in every corner: spectral braziers in the crypt, glowing crystals in the caverns.
                foreach (var (cx, cz) in new[]
                         {
                             (room.X, room.Z), (room.X + room.W - 1, room.Z), (room.X, room.Z + room.H - 1), (room.X + room.W - 1, room.Z + room.H - 1),
                         })
                {
                    var center = layout.CellCenter(cx, cz);
                    var inward = layout.CellCenter(room.CenterX, room.CenterZ);
                    var at = new GroundPoint(center.X + Math.Sign(inward.X - center.X) * -s * 0.25f, center.Z + Math.Sign(inward.Z - center.Z) * -s * 0.25f);
                    if (!NearKeyPoint(layout, at, 4f))
                    {
                        Prop(layout, crypt ? PropKind.Brazier : PropKind.Crystal, at, Range(random, 0f, 360f), crypt ? 1f : Range(random, 0.8f, 1.3f));
                    }
                }

                if (room.W >= 6 && room.H >= 6)
                {
                    foreach (var (px, pz) in new[] { (room.X + 1, room.Z + 1), (room.X + room.W - 2, room.Z + 1), (room.X + 1, room.Z + room.H - 2), (room.X + room.W - 2, room.Z + room.H - 2) })
                    {
                        var at = layout.CellCenter(px, pz);
                        if (!NearKeyPoint(layout, at, 5f))
                        {
                            Prop(layout, crypt ? PropKind.Pillar : PropKind.Stalagmite, at, 0f, 1f);
                        }
                    }
                }
            }

            Scatter(layout, random, 24, r => crypt ? (r.Next(2) == 0 ? PropKind.Coffin : PropKind.Bones) : (r.Next(2) == 0 ? PropKind.Pool : PropKind.Stalagmite), 6f, 0.8f, 1.2f);
        }

        /// <summary>
        /// Phase 8: the volcano's rooms glow with lava pools and fire-lit basalt; the sand tombs have braziers, sandstone
        /// pillars, sarcophagi, urns and the bones of the robbers who came before.
        /// </summary>
        private static void HotDungeonProps(MapLayout layout, Random random, bool volcano)
        {
            float s = layout.CellSize;
            foreach (var room in layout.Rooms)
            {
                foreach (var (cx, cz) in new[]
                         {
                             (room.X, room.Z), (room.X + room.W - 1, room.Z), (room.X, room.Z + room.H - 1), (room.X + room.W - 1, room.Z + room.H - 1),
                         })
                {
                    var center = layout.CellCenter(cx, cz);
                    var inward = layout.CellCenter(room.CenterX, room.CenterZ);
                    var at = new GroundPoint(center.X + Math.Sign(inward.X - center.X) * -s * 0.25f, center.Z + Math.Sign(inward.Z - center.Z) * -s * 0.25f);
                    if (!NearKeyPoint(layout, at, 4f))
                    {
                        Prop(layout, volcano ? PropKind.Stalagmite : PropKind.Brazier, at, Range(random, 0f, 360f), volcano ? Range(random, 0.8f, 1.3f) : 1f);
                    }
                }

                if (room.W >= 6 && room.H >= 6)
                {
                    foreach (var (px, pz) in new[] { (room.X + 1, room.Z + 1), (room.X + room.W - 2, room.Z + 1), (room.X + 1, room.Z + room.H - 2), (room.X + room.W - 2, room.Z + room.H - 2) })
                    {
                        var at = layout.CellCenter(px, pz);
                        if (!NearKeyPoint(layout, at, 5f))
                        {
                            Prop(layout, volcano ? PropKind.Obelisk : PropKind.Pillar, at, 0f, 1f);
                        }
                    }
                }
            }

            if (volcano)
            {
                Scatter(layout, random, 26, r => r.Next(3) < 2 ? PropKind.LavaPool : PropKind.Bones, 6f, 0.8f, 1.4f);
                Scatter(layout, random, 10, _ => PropKind.Stalagmite, 7f, 0.7f, 1.2f);
            }
            else
            {
                Scatter(layout, random, 24, r => r.Next(3) == 0 ? PropKind.Coffin : r.Next(2) == 0 ? PropKind.Urn : PropKind.Bones, 6f, 0.8f, 1.2f);
            }
        }

        // ================================================================ helpers
        private static int Range(Random random, int min, int maxExclusive)
        {
            return maxExclusive <= min ? min : random.Next(min, maxExclusive);
        }

        private static float Range(Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }

        private static int Clamp(int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
}
