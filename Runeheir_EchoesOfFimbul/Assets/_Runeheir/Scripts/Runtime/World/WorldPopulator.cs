using Runeheir.Combat;
using Runeheir.Field;
using UnityEngine;

namespace Runeheir.WorldBuilding
{
    /// <summary>Places everything that lives on a built map: warp portals, NPCs, monster spawners and boss spawners.</summary>
    public static class WorldPopulator
    {
        public static Transform Populate(BuiltWorld world)
        {
            var root = new GameObject("Population").transform;
            var layout = world.Layout;
            foreach (var portal in layout.Portals)
            {
                WarpPortal.Create(portal.Portal, WorldBuilder.ToWorld(portal.At), root);
            }

            foreach (var npc in layout.Npcs)
            {
                var at = WorldBuilder.ToWorld(npc.At);
                // Town NPCs face the plaza; camp couriers face the campfire and the save point.
                var face = world.Map.IsTown ? Vector3.zero : world.SavePoint;
                if ((face - at).sqrMagnitude < 1f)
                {
                    face = at + Vector3.back;
                }

                NpcActor.Spawn(npc.Npc, at, face);
            }

            foreach (var spawn in layout.Spawns)
            {
                var go = new GameObject($"Spawn_{spawn.Spawn.MonsterId}");
                go.transform.SetParent(root, false);
                go.transform.position = WorldBuilder.ToWorld(spawn.Center);
                var spawner = go.AddComponent<MonsterSpawner>();
                spawner.MonsterId = spawn.Spawn.MonsterId;
                spawner.Count = spawn.Spawn.Count;
                spawner.Radius = spawn.Radius;
            }

            foreach (var boss in layout.Bosses)
            {
                BossSpawner.Create(world.Map, boss.Boss, WorldBuilder.ToWorld(boss.At), root);
            }

            return root;
        }
    }
}
