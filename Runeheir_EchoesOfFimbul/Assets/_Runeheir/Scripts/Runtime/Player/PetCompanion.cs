using Runeheir.Items;
using Runeheir.Monsters;
using Runeheir.Visuals;
using UnityEngine;
using UnityEngine.AI;

namespace Runeheir.Player
{
    /// <summary>
    /// A hatched pet (Phase 8): its monster's look at pet size, trotting at its owner's heel, wearing its accessory when it
    /// has one. It doesn't fight; it keeps you company, gets hungry, and once Loyal grants its bonus (see
    /// <see cref="Pets.PetCatalog"/>). Other players' pets follow their mirrors (the pet rides in the look code).
    /// </summary>
    public sealed class PetCompanion : MonoBehaviour
    {
        private const float Size = 0.6f;
        private Transform _owner;
        private PlaceholderAvatar _avatar;
        private float _bob;

        public string EggItemId { get; private set; }

        public string ArmorItemId { get; private set; }

        /// <summary>
        /// Keeps <paramref name="current"/> in step with the pet that should be out: removes it when none is, rebuilds it
        /// when the pet or its accessory changed. Returns the companion to keep (null when no pet is out).
        /// </summary>
        public static PetCompanion Sync(PetCompanion current, Transform owner, string eggItemId, string armorItemId, string name)
        {
            var pet = Pets.PetCatalog.ForEgg(eggItemId);
            var look = pet != null ? MonsterCatalog.Get(pet.MonsterId) : null;
            if (current != null && (look == null || current.EggItemId != eggItemId || current.ArmorItemId != armorItemId))
            {
                Destroy(current.gameObject);
                current = null;
            }

            if (current == null && look != null && owner != null)
            {
                current = Spawn(owner, look, eggItemId, armorItemId, string.IsNullOrEmpty(name) ? look.Name : name);
            }

            return current;
        }

        private static PetCompanion Spawn(Transform owner, MonsterDefinition look, string eggItemId, string armorItemId, string name)
        {
            var go = new GameObject($"Pet [{name}]");
            go.transform.position = owner.position + owner.right * 1.2f;
            var visual = new GameObject("Visual").transform;
            visual.SetParent(go.transform, false);
            visual.localScale = Vector3.one * Size / Mathf.Max(0.6f, look.Scale);
            var pet = go.AddComponent<PetCompanion>();
            pet._owner = owner;
            pet.EggItemId = eggItemId;
            pet.ArmorItemId = armorItemId;
            pet._avatar = PlaceholderAvatar.CreateMonster(visual, look);
            if (!string.IsNullOrEmpty(armorItemId))
            {
                AddAccessory(go.transform, ItemCatalog.Get(armorItemId));
            }

            return pet;
        }

        /// <summary>A little cap in the accessory's color, perched on top of the pet.</summary>
        private static void AddAccessory(Transform pet, ItemDefinition item)
        {
            var bounds = new Bounds(pet.position + Vector3.up * 0.3f, Vector3.one * 0.3f);
            foreach (var renderer in pet.GetComponentsInChildren<Renderer>())
            {
                bounds.Encapsulate(renderer.bounds);
            }

            var cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cap.name = "Accessory";
            Destroy(cap.GetComponent<Collider>());
            cap.transform.SetParent(pet, false);
            cap.transform.position = new Vector3(pet.position.x, bounds.max.y, pet.position.z);
            float width = Mathf.Clamp(Mathf.Min(bounds.size.x, bounds.size.z) * 0.45f, 0.12f, 0.4f);
            cap.transform.localScale = new Vector3(width, width * 0.45f, width);
            var color = item != null ? RuntimeMaterials.Hex(item.IconColorHex) : new Color(0.84f, 0.74f, 0.89f);
            cap.GetComponent<Renderer>().sharedMaterial = RuntimeMaterials.Lit(color);
        }

        private void Update()
        {
            if (_owner == null)
            {
                Destroy(gameObject);
                return;
            }

            // At the owner's heel, a little to the right; too far behind (a warp) and it simply appears there.
            var owner = _owner;
            var heel = owner.position - owner.forward * 1.4f + owner.right * 0.9f;
            var position = transform.position;
            var flat = heel - position;
            flat.y = 0f;
            float distance = flat.magnitude;
            if (distance > 20f)
            {
                position = heel;
                distance = 0f;
            }

            float speed = distance > 1.1f ? Mathf.Clamp(distance * 2.5f, 1f, 9f) : 0f;
            if (speed > 0f)
            {
                position += flat.normalized * Mathf.Min(distance, speed * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat.normalized, Vector3.up), Time.deltaTime * 8f);
            }
            else
            {
                var toOwner = owner.position - position;
                toOwner.y = 0f;
                if (toOwner.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toOwner.normalized, Vector3.up), Time.deltaTime * 3f);
                }
            }

            if (NavMesh.SamplePosition(position, out var hit, 3f, NavMesh.AllAreas))
            {
                position.y = hit.position.y;
            }

            _bob += Time.deltaTime * (speed > 0f ? 9f : 2f);
            transform.position = position + Vector3.up * (speed > 0f ? Mathf.Abs(Mathf.Sin(_bob)) * 0.08f : Mathf.Sin(_bob) * 0.02f);
            if (_avatar != null)
            {
                _avatar.SetLocomotion(speed);
            }
        }
    }
}
