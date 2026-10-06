using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Runeheir.Characters;
using Runeheir.Monsters;
using Runeheir.World;

namespace Runeheir.Tests
{
    /// <summary>Phase 5 world: the 14 maps, their portal graph, and the generated layouts.</summary>
    public sealed class WorldTests
    {
        private static readonly Dictionary<string, MapLayout> Layouts = new Dictionary<string, MapLayout>();

        private static MapLayout Layout(MapDefinition map)
        {
            if (!Layouts.TryGetValue(map.Id, out var layout))
            {
                layout = MapLayoutGenerator.Generate(map);
                Layouts[map.Id] = layout;
            }

            return layout;
        }

        [Test]
        public void Catalog_Has14MapsInOneScene()
        {
            Assert.AreEqual(14, MapCatalog.All.Count, "GDD §2: 14 maps");
            Assert.AreEqual(1, MapCatalog.All.Count(m => m.Kind == MapKind.Town));
            Assert.AreEqual(4, MapCatalog.All.Count(m => m.Kind == MapKind.Field), "four leveling fields");
            Assert.AreEqual(7, MapCatalog.All.Count(m => m.Kind == MapKind.Dungeon), "Catacombs B1–B4 and Caverns 1–3");
            Assert.AreEqual(1, MapCatalog.All.Count(m => m.Kind == MapKind.Arena), "the Hall of Branches");
            Assert.AreEqual(1, MapCatalog.All.Count(m => m.Kind == MapKind.Lair), "Lyngvi");
            Assert.IsTrue(MapCatalog.All.All(m => m.SceneName == MapCatalog.WorldScene));
            Assert.AreEqual(14, MapCatalog.All.Select(m => m.Seed).Distinct().Count(), "every map has its own seed");
            Assert.IsTrue(MapCatalog.Get(MapCatalog.StartingMapId).IsTown);
        }

        [Test]
        public void Portals_ComeInPairs()
        {
            foreach (var map in MapCatalog.All)
            {
                Assert.IsNotEmpty(map.Portals, $"{map.Id} has no way out");
                foreach (var portal in map.Portals)
                {
                    var target = MapCatalog.Get(portal.TargetMap);
                    Assert.IsNotNull(target, $"{map.Id}.{portal.Id} leads to unknown {portal.TargetMap}");
                    var back = target.Portal(portal.TargetPortal);
                    Assert.IsNotNull(back, $"{map.Id}.{portal.Id}: {target.Id} has no portal {portal.TargetPortal}");
                    Assert.AreEqual(map.Id, back.TargetMap, $"{target.Id}.{back.Id} must lead back to {map.Id}");
                    Assert.AreEqual(portal.Id, back.TargetPortal, $"{target.Id}.{back.Id} must arrive at {map.Id}.{portal.Id}");
                    Assert.IsFalse(string.IsNullOrEmpty(portal.Label), $"{map.Id}.{portal.Id} needs a label");
                }

                Assert.AreEqual(map.Portals.Count, map.Portals.Select(p => p.Id).Distinct().Count(), $"{map.Id} portal ids are unique");
            }
        }

        [Test]
        public void EveryMap_IsReachableFromVigrid()
        {
            var seen = new HashSet<string> { MapCatalog.VigridHaven };
            var queue = new Queue<string>(seen);
            while (queue.Count > 0)
            {
                foreach (var portal in MapCatalog.Get(queue.Dequeue()).Portals)
                {
                    if (seen.Add(portal.TargetMap))
                    {
                        queue.Enqueue(portal.TargetMap);
                    }
                }
            }

            CollectionAssert.AreEquivalent(MapCatalog.All.Select(m => m.Id), seen);
        }

        [Test]
        public void Spawns_AndBosses_UseRealMonsters()
        {
            foreach (var map in MapCatalog.All)
            {
                foreach (var spawn in map.Spawns)
                {
                    var monster = MonsterCatalog.Get(spawn.MonsterId);
                    Assert.IsNotNull(monster, $"{map.Id} spawns unknown {spawn.MonsterId}");
                    Assert.IsFalse(monster.IsBoss || monster.SummonOnly, $"{map.Id}: {monster.Id} is not a field spawn");
                    Assert.Greater(spawn.Count, 0);
                    if (map.MaxLevel > 0 && !monster.Immortal)
                    {
                        Assert.LessOrEqual(monster.Level, map.MaxLevel + 5, $"{monster.Id} is too strong for {map.Name} ({map.LevelLabel})");
                    }
                }

                foreach (var boss in map.Bosses)
                {
                    var monster = MonsterCatalog.Get(boss.MonsterId);
                    Assert.IsNotNull(monster, $"{map.Id} boss {boss.MonsterId}");
                    Assert.IsTrue(monster.IsBoss, $"{monster.Id} must be a mini-boss or MVP");
                    Assert.AreEqual(monster.IsMvp ? 60f : 120f, boss.RespawnMinutes, $"{monster.Id}: GDD MVP 1 h, mini-boss 2 h");
                }

                Assert.IsTrue(map.IsTown || map.Kind == MapKind.Arena || map.Spawns.Count > 0, $"{map.Id} has no monsters");
                Assert.IsTrue(!map.IsTown && map.Kind != MapKind.Arena || map.Spawns.Count == 0, $"{map.Id} is a safe zone");
            }
        }

        [Test]
        public void EveryCardMonster_LivesSomewhere()
        {
            var placed = new HashSet<string>(MapCatalog.All.SelectMany(m => m.Spawns.Select(s => s.MonsterId).Concat(m.Bosses.Select(b => b.MonsterId))));
            foreach (var monster in MonsterCatalog.All.Where(m => !m.Immortal && !m.SummonOnly))
            {
                Assert.IsTrue(placed.Contains(monster.Id), $"{monster.Name} isn't on any map");
            }

            Assert.AreEqual(7, MapCatalog.All.Sum(m => m.Bosses.Count), "4 mini-bosses + 3 MVPs");
        }

        [Test]
        public void Layouts_AreDeterministic()
        {
            foreach (var map in MapCatalog.All)
            {
                var a = MapLayoutGenerator.Generate(map);
                var b = MapLayoutGenerator.Generate(map);
                CollectionAssert.AreEqual(a.Walkable, b.Walkable, map.Id);
                Assert.AreEqual(a.Props.Count, b.Props.Count, map.Id);
                Assert.AreEqual(a.SavePoint.ToString(), b.SavePoint.ToString(), map.Id);
            }
        }

        [Test]
        public void Layouts_ConnectEveryKeyPoint()
        {
            foreach (var map in MapCatalog.All)
            {
                var layout = Layout(map);
                Assert.IsTrue(layout.IsWalkable(layout.SavePoint), $"{map.Id} save point");
                var reached = layout.Reachable(layout.SavePoint);

                void Check(GroundPoint point, string what)
                {
                    Assert.IsTrue(layout.CellOf(point, out int x, out int z) && reached[z * layout.Width + x], $"{map.Id}: {what} at {point} can't be reached from the save point");
                }

                foreach (var portal in layout.Portals)
                {
                    Check(portal.At, "portal " + portal.Portal.Id);
                    Check(portal.Arrival, "arrival of " + portal.Portal.Id);

                    // Arriving at the save point (death, teleports, @warp) must not drop you into a warp.
                    Assert.Greater(portal.At.DistanceTo(layout.SavePoint), 6f, $"{map.Id}: portal {portal.Portal.Id} is on top of the save point");
                    Assert.Greater(portal.At.DistanceTo(portal.Arrival), 3f, $"{map.Id}: arriving through {portal.Portal.Id} lands in the portal");
                }

                layout.Npcs.ForEach(n => Check(n.At, n.Npc.Name));
                layout.Bosses.ForEach(b => Check(b.At, b.Boss.MonsterId));
                layout.Spawns.ForEach(s => Check(s.Center, s.Spawn.MonsterId));
                Assert.AreEqual(map.Portals.Count, layout.Portals.Count);
                Assert.AreEqual(map.Spawns.Count, layout.Spawns.Count);
                Assert.AreEqual(map.Bosses.Count, layout.Bosses.Count);
                Assert.AreEqual(map.Npcs.Count, layout.Npcs.Count);
                Assert.Greater(layout.WalkableCount() * layout.CellSize * layout.CellSize, 1500f, $"{map.Id} is too small to play in");
            }
        }

        [Test]
        public void Layouts_KeepScenery_OffKeyPoints_AndOnTheGround()
        {
            foreach (var map in MapCatalog.All)
            {
                var layout = Layout(map);
                var keys = new List<(GroundPoint at, string what)> { (layout.SavePoint, "save point") };
                keys.AddRange(layout.Portals.SelectMany(p => new[] { (p.At, "portal " + p.Portal.Id), (p.Arrival, "arrival " + p.Portal.Id) }));
                keys.AddRange(layout.Npcs.Select(n => (n.At, n.Npc.Name)));
                keys.AddRange(layout.Bosses.Select(b => (b.At, b.Boss.MonsterId)));
                foreach (var prop in layout.Props.Where(p => PropKinds.Blocks(p.Kind)))
                {
                    foreach (var (at, what) in keys)
                    {
                        Assert.Greater(PropKinds.EdgeDistance(prop, at), 1f, $"{map.Id}: a {prop.Kind} at {prop.At} blocks the {what} at {at}");
                    }
                }

                foreach (var npc in layout.Npcs)
                {
                    foreach (var other in layout.Npcs.Where(o => o != npc))
                    {
                        Assert.Greater(npc.At.DistanceTo(other.At), 2f, $"{map.Id}: {npc.Npc.Name} and {other.Npc.Name} overlap");
                    }
                }
            }
        }

        [Test]
        public void Dungeons_PutTheBossDeepAndTheStairsAtTheEntrance()
        {
            foreach (var map in MapCatalog.All.Where(m => m.IsDungeon))
            {
                var layout = Layout(map);
                var up = layout.Portals.First(p => p.Portal.Id == "up");
                Assert.Less(up.At.DistanceTo(layout.SavePoint), 20f, $"{map.Id}: you arrive by the stairs up");
                foreach (var boss in layout.Bosses)
                {
                    Assert.Greater(boss.At.DistanceTo(up.At), 40f, $"{map.Id}: the boss chamber is deep inside");
                }

                var down = layout.Portals.FirstOrDefault(p => p.Portal.Id == "down");
                if (down != null)
                {
                    Assert.Greater(down.At.DistanceTo(up.At), 40f, $"{map.Id}: the stairs down are deep inside");
                }

                Assert.IsNotEmpty(layout.Walls, $"{map.Id} has walls");
            }
        }

        [Test]
        public void Towns_AreSafe_AndHaveTheirNpcs()
        {
            var vigrid = MapCatalog.Get(MapCatalog.VigridHaven);
            Assert.IsFalse(vigrid.AllowBranches, "no branches in the capital");
            Assert.IsFalse(vigrid.AllowRandomTeleport);
            foreach (var kind in new[] { NpcKind.Merchant, NpcKind.Forge, NpcKind.Storage, NpcKind.JobMaster })
            {
                Assert.IsTrue(vigrid.Npcs.Any(n => n.Kind == kind), $"Vigrid needs a {kind}");
            }

            Assert.IsTrue(vigrid.Npcs.Count(n => n.Kind == NpcKind.Merchant) >= 2, "Ásta and the Armory");
            var hall = MapCatalog.Get(MapCatalog.HallOfBranches);
            Assert.IsTrue(hall.AllowBranches);
            foreach (var field in MapCatalog.All.Where(m => m.Kind == MapKind.Field))
            {
                Assert.IsTrue(field.Npcs.Any(n => n.Kind == NpcKind.Storage), $"{field.Name} needs a Norn Courier camp");
            }

            foreach (var destination in MapCatalog.TeleportDestinations)
            {
                Assert.IsNotNull(MapCatalog.Get(destination.MapId), destination.MapId);
                Assert.Greater(destination.Zeny, 0);
            }
        }

        [Test]
        public void NewCharacters_StartInVigrid_AndOldSavesStayPut()
        {
            var record = CharacterFactory.Create(new CharacterCreateRequest { Name = "World Tester" }, 0, 0);
            Assert.AreEqual(MapCatalog.VigridHaven, record.MapId);
            Assert.AreEqual(MapCatalog.VigridHaven, record.SaveMapId);

            var old = record.Clone();
            old.MapId = MapCatalog.WhisperwoodPlains;
            old.SaveMapId = null;
            old.HasSavedPosition = true;
            old.Sanitize();
            Assert.AreEqual(MapCatalog.WhisperwoodPlains, old.MapId, "a Phase 4 save on the plains stays there");
            Assert.IsTrue(old.HasSavedPosition);
            Assert.AreEqual(MapCatalog.StartingMapId, old.SaveMapId, "and revives in Vigrid until it saves elsewhere");

            old.MapId = "rh_field_gone";
            old.Sanitize();
            Assert.AreEqual(MapCatalog.StartingMapId, old.MapId);
            Assert.IsFalse(old.HasSavedPosition);
        }
    }
}
