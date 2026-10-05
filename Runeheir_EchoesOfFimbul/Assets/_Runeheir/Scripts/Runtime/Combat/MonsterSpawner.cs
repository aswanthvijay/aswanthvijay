using System.Collections;
using System.Collections.Generic;
using Runeheir.Field;
using Runeheir.Monsters;
using UnityEngine;
using UnityEngine.AI;

namespace Runeheir.Combat
{
    /// <summary>Keeps <see cref="count"/> monsters of one type alive around this point (Ragnarok-style spawn).</summary>
    public sealed class MonsterSpawner : MonoBehaviour
    {
        [Tooltip("Id from MonsterCatalog, e.g. rune_spore, forest_imp, training_dummy.")]
        [SerializeField] private string monsterId = "rune_spore";
        [SerializeField, Min(1)] private int count = 5;
        [SerializeField, Min(0f)] private float radius = 8f;
        [Tooltip("Seconds before a dead monster respawns. Negative = use the monster's default.")]
        [SerializeField] private float respawnSecondsOverride = -1f;

        private readonly List<Monster> _alive = new List<Monster>();

        public string MonsterId
        {
            get => monsterId;
            set => monsterId = value;
        }

        public int Count
        {
            get => count;
            set => count = Mathf.Max(1, value);
        }

        public float Radius
        {
            get => radius;
            set => radius = Mathf.Max(0f, value);
        }

        public void NotifyDeath(Monster monster)
        {
            _alive.Remove(monster);
            float delay = respawnSecondsOverride >= 0f ? respawnSecondsOverride : monster.Definition.RespawnSeconds;
            StartCoroutine(RespawnAfter(delay));
        }

        private void Start()
        {
            var definition = MonsterCatalog.Get(monsterId);
            if (definition == null)
            {
                Debug.LogWarning($"[Runeheir] MonsterSpawner '{name}': unknown monster id '{monsterId}'.", this);
                enabled = false;
                return;
            }

            for (int i = 0; i < count; i++)
            {
                SpawnOne();
            }
        }

        private IEnumerator RespawnAfter(float seconds)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, seconds));
            if (isActiveAndEnabled && _alive.Count < count)
            {
                SpawnOne();
            }
        }

        private void SpawnOne()
        {
            var definition = MonsterCatalog.Get(monsterId);
            Vector2 offset = radius > 0f ? Random.insideUnitCircle * radius : Vector2.zero;
            Vector3 point = transform.position + new Vector3(offset.x, 0f, offset.y);
            if (!NavMesh.SamplePosition(point, out NavMeshHit hit, Mathf.Max(2f, radius), NavMesh.AllAreas))
            {
                Debug.LogWarning($"[Runeheir] MonsterSpawner '{name}': no NavMesh near {point}.", this);
                return;
            }

            var monster = EntityFactory.CreateMonster(definition, hit.position, Random.Range(0f, 360f));
            monster.Spawner = this;
            _alive.Add(monster);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.35f, 0.25f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.3f, radius));
        }
    }
}
