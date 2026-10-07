using System;
using System.Collections.Generic;

namespace Runeheir.World
{
    /// <summary>A Norn Courier teleport destination: you arrive at the map's save point.</summary>
    public sealed class TeleportDestination
    {
        public string MapId;
        public int Zeny;
    }

    /// <summary>
    /// GDD §2: the 14 maps of Midgard under the Fimbulwinter. Vigrid Haven is the hub; the Whisperwood Plains, Whispering
    /// Woods, Howling Fjord and Jotun Steppe climb from Lv 1 to 255; the Catacombs of Helheim (4 floors) and the Sunken
    /// Fjord Caverns (3 floors) end in MVP chambers; the Hall of Branches is the arena for Dead and Blood Branches; and
    /// Lyngvi, past the Jotun Steppe, is where Fenrir lies bound.
    /// </summary>
    public static class MapCatalog
    {
        /// <summary>The one scene every generated map loads (the map is built at load time).</summary>
        public const string WorldScene = "RH_World";

        public const string VigridHaven = "vigrid_haven";
        public const string HallOfBranches = "hall_of_branches";
        public const string WhisperwoodPlains = "whisperwood_plains";
        public const string WhisperingWoods = "whispering_woods";
        public const string HowlingFjord = "howling_fjord";
        public const string JotunSteppe = "jotun_steppe";
        public const string Lyngvi = "lyngvi";

        /// <summary>New characters start (and everyone revives, until they save elsewhere) in the capital.</summary>
        public const string StartingMapId = VigridHaven;

        private static readonly Dictionary<string, MapDefinition> ById = new Dictionary<string, MapDefinition>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<MapDefinition> Ordered = new List<MapDefinition>();

        public static readonly IReadOnlyList<TeleportDestination> TeleportDestinations = new[]
        {
            new TeleportDestination { MapId = VigridHaven, Zeny = 600 },
            new TeleportDestination { MapId = WhisperwoodPlains, Zeny = 500 },
            new TeleportDestination { MapId = WhisperingWoods, Zeny = 1500 },
            new TeleportDestination { MapId = HowlingFjord, Zeny = 3000 },
            new TeleportDestination { MapId = JotunSteppe, Zeny = 5000 },
            new TeleportDestination { MapId = Helheim(1), Zeny = 2500 },
            new TeleportDestination { MapId = Sunken(1), Zeny = 4000 },
        };

        static MapCatalog()
        {
            Town();
            Fields();
            Catacombs();
            Caverns();
        }

        public static IReadOnlyList<MapDefinition> All => Ordered;

        public static MapDefinition Get(string id)
        {
            return id != null && ById.TryGetValue(id, out var map) ? map : null;
        }

        public static string Helheim(int floor)
        {
            return "helheim_b" + floor;
        }

        public static string Sunken(int floor)
        {
            return "sunken_caverns_" + floor;
        }

        // ================================================================ Vigrid Haven and the Hall of Branches
        private static void Town()
        {
            var vigrid = Add(new MapDefinition
            {
                Id = VigridHaven, Name = "Vigrid Haven", Kind = MapKind.Town, Theme = MapTheme.Town, Size = 120f, Seed = 11,
                SaveX = 0f, SaveZ = -12f, AllowBranches = false, AllowRandomTeleport = false,
                Description = "The fortified mead-hall capital under golden pines: forge, couriers, guilds and the market.",
            });
            vigrid.Portals.Add(new MapPortal { Id = "south_gate", Label = "Whisperwood Plains", X = 0f, Z = -57f, TargetMap = WhisperwoodPlains, TargetPortal = "north_road" });
            vigrid.Portals.Add(new MapPortal { Id = "hall_door", Label = "Hall of Branches", X = -57f, Z = 0f, TargetMap = HallOfBranches, TargetPortal = "exit" });
            vigrid.Npcs.Add(new MapNpc
            {
                Kind = NpcKind.Merchant, ShopId = Items.ShopCatalog.GeneralStore, Name = "Ásta", Title = "Trading Post", X = -14f, Z = -17f,
                Greeting = "Welcome to Vigrid, traveller! Mead, tonics and branches. And I'll buy whatever the wolves left you.",
                OutfitHex = "#C7782E", Gender = 1, Head = "feathered_beret", Garment = "traveler_cloak",
            });
            vigrid.Npcs.Add(new MapNpc
            {
                Kind = NpcKind.Merchant, ShopId = Items.ShopCatalog.Armory, Name = "Hrafn", Title = "Vigrid Armory", X = 14f, Z = -18f,
                Greeting = "Steel for the road ahead. Greatswords, mail, shields and trinkets, all honestly forged.",
                OutfitHex = "#4D5656", Head = "iron_helm", Lower = "braided_beard", Shield = "round_viking_shield",
            });
            vigrid.Npcs.Add(new MapNpc
            {
                Kind = NpcKind.Forge, Name = "Brokk", Title = "Grand Dwarven Forge", X = 26f, Z = 4f,
                Greeting = "Hrmph. Bring ore and zeny and I'll hammer your gear harder. Past the safe line, Starmetal sings, or it shatters.",
                OutfitHex = "#6B4D38", Head = "grand_horned_viking_crest", Lower = "braided_beard", Shield = "round_viking_shield", Scale = 0.82f,
            });
            vigrid.Npcs.Add(new MapNpc
            {
                Kind = NpcKind.Storage, Name = "Verdandi's Courier", Title = "Norn Courier", X = 7f, Z = -34f,
                Greeting = "The Norns keep what you leave with me, carry you where you need to go, and remember where you last stood.",
                OutfitHex = "#33426B", Gender = 1, Head = "raven_hood", Garment = "valkyrian_feather_wings",
            });
            vigrid.Npcs.Add(new MapNpc
            {
                Kind = NpcKind.JobMaster, Name = "Sigrun", Title = "Guild Hall", X = -26f, Z = 18f,
                Greeting = "Every hero starts as an Initiate. Prove yourself and the guilds will teach you a path.",
                OutfitHex = "#8E44AD", Gender = 1, Head = "valkyrie_winged_helm", Garment = "valkyrian_manteau",
            });
            vigrid.Npcs.Add(new MapNpc
            {
                Kind = NpcKind.CartMerchant, Name = "Gunnar", Title = "Pushcart Rental", X = -8f, Z = -34f,
                Greeting = "A sturdy Pushcart carries 8,000 more weight, and with one you can set up a stall anywhere in Vigrid's streets.",
                OutfitHex = "#A04000", Head = "fur_cap", Lower = "braided_beard", Garment = "bear_pelt",
            });
            vigrid.Npcs.Add(new MapNpc
            {
                Kind = NpcKind.Norns, Name = "Urðr", Title = "Urðr's Well · Rebirth", X = 26f, Z = 26f,
                Greeting = "Your first thread is spun to its end. Bring it to me whole, and my sisters and I will weave it again, brighter.",
                OutfitHex = "#5B2C6F", Gender = 1, Head = "raven_hood", Garment = "valkyrian_feather_wings",
            });

            var hall = Add(new MapDefinition
            {
                Id = HallOfBranches, Name = "Hall of Branches", Kind = MapKind.Arena, Theme = MapTheme.Arena, Size = 60f, Seed = 12,
                SaveX = 0f, SaveZ = -16f, AllowRandomTeleport = false,
                Description = "A walled arena under the mead hall, where Dead and Blood Branches are cracked away from the market.",
            });
            hall.Portals.Add(new MapPortal { Id = "exit", Label = "Vigrid Haven", X = 0f, Z = -23f, TargetMap = VigridHaven, TargetPortal = "hall_door" });
            hall.Npcs.Add(new MapNpc
            {
                Kind = NpcKind.Merchant, ShopId = Items.ShopCatalog.BranchWarden, Name = "Ylva", Title = "Branch Warden", X = -9f, Z = -17f,
                Greeting = "Crack your branches here, where the walls hold whatever crawls out. Dead Branches, potions, and a prayer.",
                OutfitHex = "#7D6608", Gender = 1, Head = "antler_crown", Garment = "bear_pelt",
            });
        }

        // ================================================================ the four fields and Lyngvi
        private static void Fields()
        {
            var plains = Add(new MapDefinition
            {
                Id = WhisperwoodPlains, Name = "Whisperwood Plains", Kind = MapKind.Field, Theme = MapTheme.Meadow, MinLevel = 1, MaxLevel = 60,
                Size = 170f, Seed = 1337, SaveX = 0f, SaveZ = -6f,
                Description = "Rolling green meadows and windmills south of Vigrid: Rune Spores, Forest Imps and Horned Grazers.",
            });
            plains.Portals.Add(new MapPortal { Id = "north_road", Label = "Vigrid Haven", X = 0f, Z = 82f, TargetMap = VigridHaven, TargetPortal = "south_gate" });
            plains.Portals.Add(new MapPortal { Id = "east_trail", Label = "Whispering Woods", X = 82f, Z = 10f, TargetMap = WhisperingWoods, TargetPortal = "west_trail" });
            plains.Npcs.Add(Courier(-7f, -12f));
            Spawn(plains, "rune_spore", 10, 18f, 18f, 9f);
            Spawn(plains, "field_beetle", 8, -24f, 20f, 9f);
            Spawn(plains, "toxic_spore", 8, 30f, -26f, 8f);
            Spawn(plains, "forest_imp", 7, 48f, 38f, 9f);
            Spawn(plains, "horned_grazer", 7, -32f, -32f, 10f);
            Spawn(plains, "wood_sprite", 6, -52f, -54f, 9f);
            Spawn(plains, "training_dummy", 1, 10f, 3f, 0f);
            Spawn(plains, "training_dummy", 1, 12.5f, 0f, 0f);
            Spawn(plains, "training_dummy", 1, 13f, -3.5f, 0f);

            var woods = Add(new MapDefinition
            {
                Id = WhisperingWoods, Name = "Whispering Woods", Kind = MapKind.Field, Theme = MapTheme.BirchForest, MinLevel = 61, MaxLevel = 120,
                Size = 170f, Seed = 2024, SaveX = -66f, SaveZ = 6f,
                Description = "Dense autumn birch forest with ancient Viking runestones. Dire Wolves hunt in packs; outlaws watch the roads.",
            });
            woods.Portals.Add(new MapPortal { Id = "west_trail", Label = "Whisperwood Plains", X = -82f, Z = 10f, TargetMap = WhisperwoodPlains, TargetPortal = "east_trail" });
            woods.Portals.Add(new MapPortal { Id = "north_trail", Label = "Howling Fjord", X = 10f, Z = 82f, TargetMap = HowlingFjord, TargetPortal = "south_trail" });
            woods.Portals.Add(new MapPortal { Id = "crypt_gate", Label = "Catacombs of Helheim", X = 62f, Z = -62f, TargetMap = Helheim(1), TargetPortal = "up" });
            woods.Npcs.Add(Courier(-62f, -2f));
            Spawn(woods, "wild_boar", 9, -30f, -24f, 10f);
            Spawn(woods, "dire_wolf", 8, 10f, 30f, 9f);
            Spawn(woods, "forest_outlaw", 7, 42f, -12f, 9f);
            Spawn(woods, "dire_wolf", 6, -24f, 52f, 9f);
            Spawn(woods, "wild_boar", 6, 34f, 52f, 9f);
            Spawn(woods, "forest_outlaw", 5, 10f, -48f, 8f);
            Boss(woods, "elder_direwolf", 24f, 40f, 120f);

            var fjord = Add(new MapDefinition
            {
                Id = HowlingFjord, Name = "Howling Fjord", Kind = MapKind.Field, Theme = MapTheme.Fjord, MinLevel = 121, MaxLevel = 180,
                Size = 180f, Seed = 4242, SaveX = 12f, SaveZ = -74f,
                Description = "Freezing rocky coastline cut by grey inlets. Frost Wolves, Sea Drakes and rogue Vikings.",
            });
            fjord.Portals.Add(new MapPortal { Id = "south_trail", Label = "Whispering Woods", X = 12f, Z = -87f, TargetMap = WhisperingWoods, TargetPortal = "north_trail" });
            fjord.Portals.Add(new MapPortal { Id = "pass", Label = "Jotun Steppe", X = 87f, Z = 40f, TargetMap = JotunSteppe, TargetPortal = "pass" });
            fjord.Portals.Add(new MapPortal { Id = "sea_cave", Label = "Sunken Fjord Caverns", X = -46f, Z = -60f, TargetMap = Sunken(1), TargetPortal = "up" });
            fjord.Npcs.Add(Courier(4f, -70f));
            Spawn(fjord, "fjord_harpy", 8, 32f, -40f, 10f);
            Spawn(fjord, "frost_wolf", 8, 52f, 18f, 9f);
            Spawn(fjord, "runic_berserker", 7, -8f, 30f, 9f);
            Spawn(fjord, "sea_drake", 6, -40f, -16f, 9f);
            Spawn(fjord, "fjord_harpy", 6, 60f, 64f, 9f);
            Spawn(fjord, "frost_wolf", 5, 20f, 66f, 8f);
            Boss(fjord, "draugr_warlord", 22f, 44f, 120f);

            var steppe = Add(new MapDefinition
            {
                Id = JotunSteppe, Name = "Jotun Steppe", Kind = MapKind.Field, Theme = MapTheme.Tundra, MinLevel = 181, MaxLevel = 255,
                Size = 190f, Seed = 9001, SaveX = -78f, SaveZ = 40f,
                Description = "High-altitude freezing tundra littered with giants' bones. Jotun Brawlers, Snow Harpies and Frost Wyrms.",
            });
            steppe.Portals.Add(new MapPortal { Id = "pass", Label = "Howling Fjord", X = -92f, Z = 40f, TargetMap = HowlingFjord, TargetPortal = "pass" });
            steppe.Portals.Add(new MapPortal { Id = "rune_bridge", Label = "Lyngvi", X = 90f, Z = 88f, TargetMap = Lyngvi, TargetPortal = "bridge" });
            steppe.Npcs.Add(Courier(-74f, 32f));
            Spawn(steppe, "ice_golem", 7, -42f, 10f, 10f);
            Spawn(steppe, "snow_harpy", 8, 0f, 52f, 10f);
            Spawn(steppe, "jotun_brawler", 7, 32f, -22f, 10f);
            Spawn(steppe, "frost_wyrm", 5, 60f, 40f, 10f);
            Spawn(steppe, "snow_harpy", 6, -22f, -52f, 9f);
            Spawn(steppe, "jotun_brawler", 5, 52f, -62f, 9f);
            Boss(steppe, "ancient_golem", 10f, 6f, 120f);

            var lyngvi = Add(new MapDefinition
            {
                Id = Lyngvi, Name = "Lyngvi, Isle of the Bound Wolf", Kind = MapKind.Lair, Theme = MapTheme.Isle, MinLevel = 240, MaxLevel = 255,
                Size = 120f, Seed = 777, SaveX = 0f, SaveZ = -18f,
                Description = "A lonely isle past the Jotun Steppe where the gods bound Fenrir with Gleipnir. The chains are cracking.",
            });
            lyngvi.Portals.Add(new MapPortal { Id = "bridge", Label = "Jotun Steppe", X = 0f, Z = -56f, TargetMap = JotunSteppe, TargetPortal = "rune_bridge" });
            Spawn(lyngvi, "frost_wyrm", 3, -20f, 6f, 7f);
            Spawn(lyngvi, "snow_harpy", 4, 20f, 6f, 7f);
            Boss(lyngvi, "fenrir", 0f, 22f, 60f);
        }

        // ================================================================ Catacombs of Helheim (B1–B4)
        private static void Catacombs()
        {
            string[][] spawns =
            {
                new[] { "crypt_bat", "draugr_footman", "crypt_bat", "draugr_footman" },
                new[] { "ghoul", "crypt_wraith", "ghoul", "crypt_bat" },
                new[] { "banshee", "corrupted_einherjar", "crypt_wraith", "banshee" },
                new[] { "frozen_revenant", "hels_executioner", "frozen_revenant", "hels_executioner" },
            };
            int[][] levels = { new[] { 90, 110 }, new[] { 120, 170 }, new[] { 175, 215 }, new[] { 220, 255 } };
            for (int floor = 1; floor <= 4; floor++)
            {
                var map = Add(new MapDefinition
                {
                    Id = Helheim(floor), Name = $"Catacombs of Helheim B{floor}", Kind = MapKind.Dungeon, Theme = MapTheme.Crypt,
                    MinLevel = levels[floor - 1][0], MaxLevel = levels[floor - 1][1], Floor = floor, Size = 120f, Seed = 600 + floor,
                    Description = floor == 4
                        ? "The lowest crypt, lit by spectral blue flames: the Chamber of Hel's Vanguard."
                        : "Deep crypts under the Whispering Woods, lit by spectral blue flames.",
                });
                map.Portals.Add(new MapPortal
                {
                    Id = "up", Label = floor == 1 ? "Whispering Woods" : $"Catacombs B{floor - 1}", AtEntrance = true,
                    TargetMap = floor == 1 ? WhisperingWoods : Helheim(floor - 1), TargetPortal = floor == 1 ? "crypt_gate" : "down",
                });
                if (floor < 4)
                {
                    map.Portals.Add(new MapPortal { Id = "down", Label = $"Catacombs B{floor + 1}", AtEntrance = false, TargetMap = Helheim(floor + 1), TargetPortal = "up" });
                }

                int count = 8;
                foreach (string id in spawns[floor - 1])
                {
                    map.Spawns.Add(new MapSpawn { MonsterId = id, Count = count, Radius = 8f });
                    count = Math.Max(5, count - 1);
                }

                if (floor == 4)
                {
                    map.Bosses.Add(new BossSpawn { MonsterId = "hels_vanguard", RespawnMinutes = 60f });
                }
            }
        }

        // ================================================================ Sunken Fjord Caverns (1–3)
        private static void Caverns()
        {
            string[][] spawns =
            {
                new[] { "cave_crawler", "naga_scout", "cave_crawler", "sea_drake" },
                new[] { "ice_golem", "abyssal_leech", "naga_scout", "abyssal_leech" },
                new[] { "abyssal_leech", "frost_wyrm", "ice_golem" },
            };
            int[][] levels = { new[] { 130, 175 }, new[] { 185, 235 }, new[] { 235, 255 } };
            for (int floor = 1; floor <= 3; floor++)
            {
                var map = Add(new MapDefinition
                {
                    Id = Sunken(floor), Name = $"Sunken Fjord Caverns {floor}", Kind = MapKind.Dungeon, Theme = MapTheme.IceCavern,
                    MinLevel = levels[floor - 1][0], MaxLevel = levels[floor - 1][1], Floor = floor, Size = 120f, Seed = 800 + floor,
                    Description = floor == 3
                        ? "A flooded grotto of blue ice: the lair of Jormungandr's Brood."
                        : "Crystal ice and flooded grottos under the Howling Fjord.",
                });
                map.Portals.Add(new MapPortal
                {
                    Id = "up", Label = floor == 1 ? "Howling Fjord" : $"Caverns {floor - 1}", AtEntrance = true,
                    TargetMap = floor == 1 ? HowlingFjord : Sunken(floor - 1), TargetPortal = floor == 1 ? "sea_cave" : "down",
                });
                if (floor < 3)
                {
                    map.Portals.Add(new MapPortal { Id = "down", Label = $"Caverns {floor + 1}", AtEntrance = false, TargetMap = Sunken(floor + 1), TargetPortal = "up" });
                }

                int count = 8;
                foreach (string id in spawns[floor - 1])
                {
                    map.Spawns.Add(new MapSpawn { MonsterId = id, Count = count, Radius = 8f });
                    count = Math.Max(5, count - 1);
                }

                if (floor == 2)
                {
                    map.Bosses.Add(new BossSpawn { MonsterId = "naga_queen", RespawnMinutes = 120f });
                }

                if (floor == 3)
                {
                    map.Bosses.Add(new BossSpawn { MonsterId = "jormungandrs_brood", RespawnMinutes = 60f });
                }
            }
        }

        // ================================================================ helpers
        private static MapDefinition Add(MapDefinition map)
        {
            ById[map.Id] = map;
            Ordered.Add(map);
            return map;
        }

        private static void Spawn(MapDefinition map, string monsterId, int count, float x, float z, float radius)
        {
            map.Spawns.Add(new MapSpawn { MonsterId = monsterId, Count = count, X = x, Z = z, Radius = radius });
        }

        private static void Boss(MapDefinition map, string monsterId, float x, float z, float respawnMinutes)
        {
            map.Bosses.Add(new BossSpawn { MonsterId = monsterId, X = x, Z = z, RespawnMinutes = respawnMinutes });
        }

        private static MapNpc Courier(float x, float z)
        {
            return new MapNpc
            {
                Kind = NpcKind.Storage, Name = "Norn Courier", Title = "Storage · Save · Teleport", X = x, Z = z,
                Greeting = "Storage, a save point, or a ride back to Vigrid? The Norns' couriers stand at every camp.",
                OutfitHex = "#33426B", Gender = 1, Head = "raven_hood", Garment = "traveler_cloak",
            };
        }
    }
}
