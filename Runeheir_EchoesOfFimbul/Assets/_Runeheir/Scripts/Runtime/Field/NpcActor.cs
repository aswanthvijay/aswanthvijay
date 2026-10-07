using System;
using System.Collections.Generic;
using Runeheir.Characters;
using Runeheir.Combat;
using Runeheir.Items;
using Runeheir.Visuals;
using Runeheir.World;
using UnityEngine;
using UnityEngine.AI;

namespace Runeheir.Field
{
    /// <summary>
    /// A clickable NPC (GDD Phase 4 services, placed per map in Phase 5): merchants, Brokk's forge, the Norn Couriers and the
    /// guild's job master. Click it to walk over; in range the HUD opens its dialog. Built from primitives like the rest of
    /// the prototype, from the map's <see cref="MapNpc"/> data.
    /// </summary>
    public sealed class NpcActor : MonoBehaviour
    {
        /// <summary>Meters (edge to edge) within which an NPC can be talked to; walking further away closes its windows.</summary>
        public const float TalkRange = 2.2f;

        public const float LeaveRange = 7f;

        private static readonly List<NpcActor> Registry = new List<NpcActor>();

        public static IReadOnlyList<NpcActor> All => Registry;

        /// <summary>Raised when the local player reaches an NPC they clicked (the HUD opens its dialog).</summary>
        public static event Action<NpcActor> Interacted;

        public NpcKind Kind { get; private set; }

        public string DisplayName { get; private set; }

        public string Title { get; private set; }

        public string Greeting { get; private set; }

        /// <summary>Merchants: the shop they run.</summary>
        public string ShopId { get; private set; }

        public MapNpc Definition { get; private set; }

        public float Radius { get; private set; } = 0.45f;

        public float Height { get; private set; } = 1.9f;

        public Vector3 Position => transform.position;

        public void Interact()
        {
            Interacted?.Invoke(this);
        }

        /// <summary>
        /// Hand-built scenes (the PlayMode test field): Vigrid's shopkeeper, smith and courier in a half-circle a few meters
        /// from the save point. Generated maps place their own NPCs (<see cref="Spawn(MapNpc, Vector3, Vector3)"/>).
        /// </summary>
        public static void SpawnTownNpcs(Vector3 savePoint)
        {
            var vigrid = MapCatalog.Get(MapCatalog.VigridHaven);
            var offsets = new[] { new Vector3(-4.5f, 0f, 3.5f), new Vector3(0f, 0f, 5.5f), new Vector3(4.5f, 0f, 3.5f) };
            var kinds = new[] { NpcKind.Merchant, NpcKind.Forge, NpcKind.Storage };
            for (int i = 0; i < kinds.Length; i++)
            {
                var npc = vigrid.Npcs.Find(n => n.Kind == kinds[i]);
                if (npc != null)
                {
                    Spawn(npc, savePoint + offsets[i], savePoint);
                }
            }
        }

        public static NpcActor Spawn(MapNpc definition, Vector3 position, Vector3 faceTowards)
        {
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 4f, NavMesh.AllAreas))
            {
                position = hit.position;
            }

            var go = new GameObject("NPC_" + definition.Name);
            go.transform.position = position;
            Vector3 facing = faceTowards - position;
            facing.y = 0f;
            if (facing.sqrMagnitude > 0.01f)
            {
                go.transform.rotation = Quaternion.LookRotation(facing);
            }

            var npc = go.AddComponent<NpcActor>();
            npc.Configure(definition);
            return npc;
        }

        private void Configure(MapNpc definition)
        {
            Definition = definition;
            Kind = definition.Kind;
            DisplayName = definition.Name;
            Title = definition.Title;
            Greeting = definition.Greeting;
            ShopId = definition.ShopId;
            float scale = Mathf.Max(0.5f, definition.Scale);
            int hash = Mathf.Abs((definition.Name ?? string.Empty).GetHashCode());
            var look = Look(RuntimeMaterials.Hex(definition.OutfitHex), definition.Gender == 1 ? Gender.Female : Gender.Male, hash % 8, hash / 8 % 9,
                definition.Head, definition.Lower, definition.Shield, definition.Garment, Kind == NpcKind.Forge ? WeaponType.Mace : WeaponType.Unarmed);

            Height *= scale;
            var avatar = PlaceholderAvatar.CreateHumanoid(transform, look);
            avatar.transform.localScale = Vector3.one * scale;

            var collider = gameObject.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, Height * 0.5f, 0f);
            collider.height = Height;
            collider.radius = Radius;
            var obstacle = gameObject.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Capsule;
            obstacle.radius = Radius;
            obstacle.height = Height;
            obstacle.center = new Vector3(0f, Height * 0.5f, 0f);

            // A service marker on the ground, like a Kafra's carpet.
            GroundRing.Create("NpcRing_" + definition.Name, RingColor(Kind), 0.85f, 0.05f).ShowAt(transform.position);
        }

        private static Color RingColor(NpcKind kind)
        {
            switch (kind)
            {
                case NpcKind.Forge: return new Color(1f, 0.55f, 0.2f, 0.8f);
                case NpcKind.Storage: return new Color(0.45f, 0.7f, 1f, 0.8f);
                case NpcKind.JobMaster: return new Color(0.75f, 0.45f, 1f, 0.8f);
                case NpcKind.Norns: return new Color(0.9f, 0.85f, 1f, 0.8f);
                default: return new Color(0.95f, 0.8f, 0.35f, 0.8f);
            }
        }

        private static AvatarLook Look(Color outfit, Gender gender, int hairStyle, int hairColor, string head = null, string lower = null,
            string shield = null, string garment = null, WeaponType weapon = WeaponType.Unarmed)
        {
            return new AvatarLook
            {
                Outfit = outfit,
                Skin = AvatarLook.DefaultSkin,
                Hair = AvatarLook.HairPalette[hairColor % AvatarLook.HairPalette.Length],
                HairStyle = hairStyle,
                Gender = gender,
                Weapon = weapon,
                HeadUpper = ItemCatalog.Get(head)?.Id,
                HeadLower = ItemCatalog.Get(lower)?.Id,
                Shield = ItemCatalog.Get(shield)?.Id,
                Garment = ItemCatalog.Get(garment)?.Id,
            };
        }

        /// <summary>Edge distance on the ground plane from <paramref name="entity"/> to this NPC.</summary>
        public float EdgeDistanceTo(CombatEntity entity)
        {
            return Mathf.Max(0f, CombatEntity.HorizontalDistance(entity.Position, Position) - entity.Radius - Radius);
        }

        private void OnEnable()
        {
            Registry.Add(this);
        }

        private void OnDisable()
        {
            Registry.Remove(this);
        }
    }
}
