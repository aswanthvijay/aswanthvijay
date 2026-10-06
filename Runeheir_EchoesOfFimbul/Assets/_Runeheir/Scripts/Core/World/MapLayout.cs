using System;
using System.Collections.Generic;

namespace Runeheir.World
{
    /// <summary>A point on the ground plane (engine-free).</summary>
    public struct GroundPoint
    {
        public float X;
        public float Z;

        public GroundPoint(float x, float z)
        {
            X = x;
            Z = z;
        }

        public float DistanceTo(GroundPoint other)
        {
            float dx = X - other.X;
            float dz = Z - other.Z;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        public override string ToString()
        {
            return $"({X:0.#}, {Z:0.#})";
        }
    }

    /// <summary>What a walkable cell is made of (drawn in a different color).</summary>
    public enum CellTag : byte
    {
        Ground = 0,
        Path = 1,
        Paving = 2,
        Ice = 3,
        Snow = 4,
    }

    /// <summary>Scenery the builder knows how to draw. Most block movement; flat decals don't.</summary>
    public enum PropKind
    {
        Tree = 0,
        BirchTree = 1,
        DeadTree = 2,
        Rock = 3,
        Runestone = 4,
        Windmill = 5,
        Campfire = 6,
        House = 7,
        MeadHall = 8,
        Stall = 9,
        Well = 10,
        Banner = 11,
        Torch = 12,
        Brazier = 13,
        Pillar = 14,
        Coffin = 15,
        Bones = 16,
        Crystal = 17,
        IceSpike = 18,
        Stalagmite = 19,
        Wreck = 20,
        Log = 21,
        Mushroom = 22,
        Palisade = 23,
        Statue = 24,
        Anvil = 25,
        Chains = 26,
        GiantSkull = 27,
        Tent = 28,
        Pool = 29,
        ForgeHut = 30,
        GuildHall = 31,
    }

    public sealed class PropPlacement
    {
        public PropKind Kind;
        public GroundPoint At;
        public float Yaw;
        public float Scale = 1f;

        /// <summary>Long props (palisades, wrecks, logs): length in meters.</summary>
        public float Length;
    }

    /// <summary>A wall between a walkable and a solid cell, already merged into straight runs.</summary>
    public struct WallSegment
    {
        public GroundPoint From;
        public GroundPoint To;
    }

    public struct CellRect
    {
        public int X;
        public int Z;
        public int W;
        public int H;

        public CellRect(int x, int z, int w, int h)
        {
            X = x;
            Z = z;
            W = w;
            H = h;
        }

        public int CenterX => X + W / 2;

        public int CenterZ => Z + H / 2;

        public bool Overlaps(CellRect other, int margin)
        {
            return X - margin < other.X + other.W && other.X - margin < X + W && Z - margin < other.Z + other.H && other.Z - margin < Z + H;
        }
    }

    public sealed class ResolvedSpawn
    {
        public MapSpawn Spawn;
        public GroundPoint Center;
        public float Radius;
    }

    public sealed class ResolvedBoss
    {
        public BossSpawn Boss;
        public GroundPoint At;
    }

    public sealed class ResolvedPortal
    {
        public MapPortal Portal;
        public GroundPoint At;

        /// <summary>Where someone warping in through this portal appears (a few steps inside).</summary>
        public GroundPoint Arrival;
    }

    public sealed class ResolvedNpc
    {
        public MapNpc Npc;
        public GroundPoint At;
    }

    /// <summary>
    /// A map turned into geometry: a grid of walkable cells (with what they're made of), walls, props, and every spawn,
    /// boss, portal and NPC resolved to a point. Built by <see cref="MapLayoutGenerator"/>; drawn by the runtime map builder.
    /// </summary>
    public sealed class MapLayout
    {
        public MapDefinition Map;
        public float CellSize;
        public int Width;
        public int Height;

        /// <summary>World X/Z of cell (0, 0)'s corner. The map is centered on the origin.</summary>
        public float OriginX;

        public float OriginZ;
        public bool[] Walkable;
        public CellTag[] Tags;
        public readonly List<CellRect> Rooms = new List<CellRect>();
        public readonly List<WallSegment> Walls = new List<WallSegment>();
        public readonly List<PropPlacement> Props = new List<PropPlacement>();
        public readonly List<ResolvedSpawn> Spawns = new List<ResolvedSpawn>();
        public readonly List<ResolvedBoss> Bosses = new List<ResolvedBoss>();
        public readonly List<ResolvedPortal> Portals = new List<ResolvedPortal>();
        public readonly List<ResolvedNpc> Npcs = new List<ResolvedNpc>();
        public GroundPoint SavePoint;

        /// <summary>True when the map has water under its gaps (fjord, isle, caverns) rather than darkness or wall.</summary>
        public bool HasWater;

        public bool InBounds(int x, int z)
        {
            return x >= 0 && z >= 0 && x < Width && z < Height;
        }

        public bool IsWalkable(int x, int z)
        {
            return InBounds(x, z) && Walkable[z * Width + x];
        }

        public CellTag TagAt(int x, int z)
        {
            return InBounds(x, z) ? Tags[z * Width + x] : CellTag.Ground;
        }

        public void Set(int x, int z, bool walkable, CellTag tag = CellTag.Ground)
        {
            if (InBounds(x, z))
            {
                Walkable[z * Width + x] = walkable;
                Tags[z * Width + x] = tag;
            }
        }

        public void SetTag(int x, int z, CellTag tag)
        {
            if (IsWalkable(x, z))
            {
                Tags[z * Width + x] = tag;
            }
        }

        public GroundPoint CellCenter(int x, int z)
        {
            return new GroundPoint(OriginX + (x + 0.5f) * CellSize, OriginZ + (z + 0.5f) * CellSize);
        }

        public bool CellOf(GroundPoint point, out int x, out int z)
        {
            x = (int)Math.Floor((point.X - OriginX) / CellSize);
            z = (int)Math.Floor((point.Z - OriginZ) / CellSize);
            return InBounds(x, z);
        }

        public bool IsWalkable(GroundPoint point)
        {
            return CellOf(point, out int x, out int z) && IsWalkable(x, z);
        }

        public int WalkableCount()
        {
            int count = 0;
            foreach (bool walkable in Walkable)
            {
                if (walkable)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>Walkable cells reachable from <paramref name="start"/> (4-neighbour flood fill).</summary>
        public bool[] Reachable(GroundPoint start)
        {
            var reached = new bool[Width * Height];
            if (!CellOf(start, out int sx, out int sz) || !IsWalkable(sx, sz))
            {
                return reached;
            }

            var queue = new Queue<int>();
            queue.Enqueue(sz * Width + sx);
            reached[sz * Width + sx] = true;
            while (queue.Count > 0)
            {
                int index = queue.Dequeue();
                int x = index % Width;
                int z = index / Width;
                Visit(x + 1, z);
                Visit(x - 1, z);
                Visit(x, z + 1);
                Visit(x, z - 1);
            }

            return reached;

            void Visit(int x, int z)
            {
                if (IsWalkable(x, z) && !reached[z * Width + x])
                {
                    reached[z * Width + x] = true;
                    queue.Enqueue(z * Width + x);
                }
            }
        }

        /// <summary>The nearest walkable cell center to <paramref name="point"/> (itself if walkable).</summary>
        public GroundPoint NearestWalkable(GroundPoint point)
        {
            if (IsWalkable(point))
            {
                return point;
            }

            float best = float.MaxValue;
            var result = point;
            for (int z = 0; z < Height; z++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (!IsWalkable(x, z))
                    {
                        continue;
                    }

                    var center = CellCenter(x, z);
                    float distance = center.DistanceTo(point);
                    if (distance < best)
                    {
                        best = distance;
                        result = center;
                    }
                }
            }

            return result;
        }
    }
}
