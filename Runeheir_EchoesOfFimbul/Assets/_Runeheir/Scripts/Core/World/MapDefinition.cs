using System;
using System.Collections.Generic;

namespace Runeheir.World
{
    public enum MapKind
    {
        Town = 0,
        Field = 1,
        Dungeon = 2,
        Arena = 3,

        /// <summary>An MVP's own map (Lyngvi).</summary>
        Lair = 4,
    }

    /// <summary>How a map looks and which layout generator builds it.</summary>
    public enum MapTheme
    {
        Town = 0,
        Arena = 1,
        Meadow = 2,
        BirchForest = 3,
        Fjord = 4,
        Tundra = 5,
        Crypt = 6,
        IceCavern = 7,
        Isle = 8,
    }

    /// <summary>Town services. Each NPC's menu comes from its kind (shops from <see cref="MapNpc.ShopId"/>).</summary>
    public enum NpcKind
    {
        /// <summary>A shop: buy from its list, sell anything.</summary>
        Merchant = 0,

        /// <summary>Brokk's Dwarven Forge: refine, etch, extract, repair, supplies.</summary>
        Forge = 1,

        /// <summary>Norn Courier: storage, save point, teleport.</summary>
        Storage = 2,

        /// <summary>Guild master: job changes.</summary>
        JobMaster = 3,

        /// <summary>Phase 6: rents the Merchant Pushcart (+8,000 weight, street vending) and explains the market.</summary>
        CartMerchant = 4,
    }

    /// <summary>A group of one monster type kept alive around a point (Ragnarok spawn).</summary>
    public sealed class MapSpawn
    {
        public string MonsterId;
        public int Count;

        /// <summary>World position (fields). Ignored in dungeons, where the generator spreads groups over the rooms.</summary>
        public float X;

        public float Z;
        public float Radius = 8f;
    }

    /// <summary>A mini-boss or MVP with a long respawn timer. In dungeons it lives in the last room.</summary>
    public sealed class BossSpawn
    {
        public string MonsterId;
        public float X;
        public float Z;

        /// <summary>Minutes from death to respawn (mini-bosses 120, MVPs 60), plus up to <see cref="VarianceMinutes"/>.</summary>
        public float RespawnMinutes = 60f;

        public float VarianceMinutes = 10f;
    }

    /// <summary>A warp to another map. Arriving puts you a few steps inside the target portal.</summary>
    public sealed class MapPortal
    {
        public string Id;
        public string Label;
        public float X;
        public float Z;
        public string TargetMap;
        public string TargetPortal;

        /// <summary>Dungeons: put this portal in the entrance room (false = the deepest room).</summary>
        public bool AtEntrance = true;
    }

    public sealed class MapNpc
    {
        public NpcKind Kind;
        public string Name;
        public string Title;
        public string Greeting;

        /// <summary>For <see cref="NpcKind.Merchant"/>: the shop it runs (<c>ShopCatalog</c>).</summary>
        public string ShopId;

        public float X;
        public float Z;

        /// <summary>Look: outfit color and gear ids for the placeholder model.</summary>
        public string OutfitHex = "#8E9AAF";

        public int Gender;
        public string Head;
        public string Lower;
        public string Shield;
        public string Garment;
        public float Scale = 1f;
    }

    /// <summary>
    /// GDD §2: one of the 14 maps of Midgard. Everything the world needs to build and populate it: theme, size, level
    /// range, monster spawns, bosses, portals, NPCs and the save/revive point.
    /// </summary>
    public sealed class MapDefinition
    {
        public string Id;
        public string Name;
        public string Description;

        /// <summary>Generated maps all load the world scene; a hand-made scene can replace one by name.</summary>
        public string SceneName = MapCatalog.WorldScene;

        public MapKind Kind;
        public MapTheme Theme;
        public int MinLevel;
        public int MaxLevel;

        /// <summary>Dungeon floor (1-based), 0 elsewhere.</summary>
        public int Floor;

        /// <summary>Side length of the square map in meters.</summary>
        public float Size = 160f;

        public int Seed = 1;

        /// <summary>Where you appear when you come without a portal (and where you revive if you saved here).</summary>
        public float SaveX;

        public float SaveZ;

        /// <summary>Dead/Blood Branches work here (not in towns).</summary>
        public bool AllowBranches = true;

        /// <summary>Wind Rune Shards work here.</summary>
        public bool AllowRandomTeleport = true;

        public readonly List<MapSpawn> Spawns = new List<MapSpawn>();
        public readonly List<BossSpawn> Bosses = new List<BossSpawn>();
        public readonly List<MapPortal> Portals = new List<MapPortal>();
        public readonly List<MapNpc> Npcs = new List<MapNpc>();

        public bool IsTown => Kind == MapKind.Town;

        public bool IsDungeon => Kind == MapKind.Dungeon;

        public string LevelLabel => MinLevel > 0 ? $"Lv {MinLevel}–{MaxLevel}" : "Safe zone";

        public MapPortal Portal(string id)
        {
            return Portals.Find(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
        }
    }
}
