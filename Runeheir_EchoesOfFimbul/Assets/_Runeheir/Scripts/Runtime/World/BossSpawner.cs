using System;
using Runeheir.Combat;
using Runeheir.Field;
using Runeheir.Monsters;
using Runeheir.Session;
using Runeheir.UI;
using Runeheir.Visuals;
using Runeheir.World;
using UnityEngine;
using UnityEngine.AI;

namespace Runeheir.WorldBuilding
{
    /// <summary>
    /// A map's mini-boss or MVP lair. The boss is up unless its timer in <see cref="WorldState.Bosses"/> says otherwise
    /// (mini-bosses come back about 2 hours after they die, MVPs about 1 hour); while it's away a tombstone marks the
    /// spot with who slew it and when.
    /// </summary>
    public sealed class BossSpawner : MonoBehaviour
    {
        private const float CheckInterval = 1f;

        private MapDefinition _map;
        private BossSpawn _spawn;
        private MonsterDefinition _definition;
        private Monster _boss;
        private GameObject _tomb;
        private float _nextCheck;

        public Monster Boss => _boss;

        public BossSpawn Spawn => _spawn;

        public static BossSpawner Create(MapDefinition map, BossSpawn spawn, Vector3 position, Transform parent)
        {
            var go = new GameObject("Boss_" + spawn.MonsterId);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var spawner = go.AddComponent<BossSpawner>();
            spawner._map = map;
            spawner._spawn = spawn;
            spawner._definition = MonsterCatalog.Get(spawn.MonsterId);
            return spawner;
        }

        private void Start()
        {
            if (_definition == null)
            {
                Debug.LogWarning($"[Runeheir] Unknown boss '{_spawn?.MonsterId}' on {_map?.Id}.");
                enabled = false;
                return;
            }

            if (WorldState.Bosses.IsDue(_map.Id, _definition.Id, WorldState.Now))
            {
                SpawnBoss(arrival: true);
            }
            else
            {
                ShowTomb(WorldState.Bosses.Status(_map.Id, _definition.Id));
            }
        }

        private void Update()
        {
            if (_boss != null || Time.time < _nextCheck)
            {
                return;
            }

            _nextCheck = Time.time + CheckInterval;
            if (WorldState.Bosses.IsDue(_map.Id, _definition.Id, WorldState.Now))
            {
                SpawnBoss(arrival: false);
            }
        }

        private void SpawnBoss(bool arrival)
        {
            Vector3 point = transform.position;
            if (NavMesh.SamplePosition(point, out NavMeshHit hit, 6f, NavMesh.AllAreas))
            {
                point = hit.position;
            }

            ClearTomb();
            _boss = EntityFactory.CreateMonster(_definition, point, 180f);
            _boss.Died += OnBossDied;
            WorldState.Bosses.MarkSpawned(_map.Id, _definition.Id);
            string rank = _definition.IsMvp ? "MVP" : "Mini-boss";
            if (arrival)
            {
                ChatLog.Notice($"⚔ {rank} {_definition.Name} (Lv {_definition.Level}) dwells on this map.");
            }
            else
            {
                ChatLog.Notice($"⚔ {_definition.Name} has returned to {_map.Name}!");
                GroundRing.SpawnPulse(point, _definition.IsMvp ? new Color(1f, 0.8f, 0.25f, 1f) : new Color(0.85f, 0.85f, 0.95f, 1f), 0.5f, 6f, 1.2f, 0.15f);
            }
        }

        /// <summary>Raised with the killer (CombatEntity.Died passes the killer, not the victim).</summary>
        private void OnBossDied(CombatEntity killer)
        {
            if (_boss != null)
            {
                _boss.Died -= OnBossDied;
            }

            string mvp = _boss != null ? _boss.MvpName : null;
            var status = WorldState.Bosses.RecordKill(_map.Id, _spawn, mvp, WorldState.Now, SystemRandomSource.Shared);
            _boss = null;
            ChatLog.Notice($"{_definition.Name} has fallen{(string.IsNullOrEmpty(mvp) ? string.Empty : $" to {mvp}")}. " +
                           $"It will rise again in about {BossTracker.FormatDuration(_spawn.RespawnMinutes * 60.0 * WorldState.Bosses.RespawnScale)}.");
            ShowTomb(status);
        }

        private void ShowTomb(BossStatus status)
        {
            ClearTomb();
            if (status == null || status.Alive)
            {
                return;
            }

            _tomb = new GameObject("Tomb_" + _definition.Id);
            _tomb.transform.SetParent(transform, false);
            _tomb.transform.position = transform.position;
            var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = "Slab";
            slab.transform.SetParent(_tomb.transform, false);
            slab.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            slab.transform.localScale = new Vector3(1f, 1.5f, 0.35f);
            slab.GetComponent<Renderer>().sharedMaterial = RuntimeMaterials.Lit(new Color(0.42f, 0.42f, 0.46f), 1.5f);
            var rune = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rune.name = "Rune";
            Destroy(rune.GetComponent<Collider>());
            rune.transform.SetParent(slab.transform, false);
            rune.transform.localPosition = new Vector3(0f, 0.1f, 0.52f);
            rune.transform.localScale = new Vector3(0.35f, 0.5f, 0.05f);
            var glow = _definition.IsMvp ? new Color(1f, 0.8f, 0.3f) : new Color(0.75f, 0.85f, 1f);
            rune.GetComponent<Renderer>().sharedMaterial = RuntimeMaterials.Glow(glow, glow * 1.3f);

            var died = DateTime.Now.AddSeconds(-(WorldState.Now - status.DiedAt));
            string killer = string.IsNullOrEmpty(status.KilledBy) ? "unknown hands" : status.KilledBy;
            WorldLabel.Attach(_tomb, $"{_definition.Name}'s Tomb\n<size=11>Slain by {killer} at {died:HH:mm}</size>",
                _definition.IsMvp ? new Color(1f, 0.85f, 0.45f) : new Color(0.85f, 0.9f, 1f), 2.1f, 14);
        }

        private void ClearTomb()
        {
            if (_tomb != null)
            {
                Destroy(_tomb);
                _tomb = null;
            }
        }
    }
}
