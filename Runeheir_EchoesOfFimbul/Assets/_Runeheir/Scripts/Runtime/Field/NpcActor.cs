using System;
using System.Collections.Generic;
using Runeheir.Characters;
using Runeheir.Combat;
using Runeheir.Items;
using Runeheir.Visuals;
using UnityEngine;
using UnityEngine.AI;

namespace Runeheir.Field
{
    public enum NpcKind
    {
        /// <summary>Ásta's Trading Post: consumables, Dead Branches, starter gear; buys anything.</summary>
        Merchant = 0,

        /// <summary>Brokk's Dwarven Forge: ores and runes, refining, Runic Fuller etching, card extraction.</summary>
        Forge = 1,

        /// <summary>The Norn Courier: account-wide storage.</summary>
        Storage = 2,
    }

    /// <summary>
    /// A clickable town NPC (GDD Phase 4 services). Click it to walk over; in range the HUD opens its dialog.
    /// Built from primitives like the rest of the prototype; spawned next to the save point by <see cref="FieldBootstrap"/>.
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

        public float Radius { get; private set; } = 0.45f;

        public float Height { get; private set; } = 1.9f;

        public Vector3 Position => transform.position;

        public void Interact()
        {
            Interacted?.Invoke(this);
        }

        /// <summary>The three Phase 4 service NPCs in a half-circle a few meters from the save point.</summary>
        public static void SpawnTownNpcs(Vector3 savePoint)
        {
            Spawn(NpcKind.Merchant, savePoint + new Vector3(-4.5f, 0f, 3.5f), savePoint);
            Spawn(NpcKind.Forge, savePoint + new Vector3(0f, 0f, 5.5f), savePoint);
            Spawn(NpcKind.Storage, savePoint + new Vector3(4.5f, 0f, 3.5f), savePoint);
        }

        public static NpcActor Spawn(NpcKind kind, Vector3 position, Vector3 faceTowards)
        {
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 4f, NavMesh.AllAreas))
            {
                position = hit.position;
            }

            var go = new GameObject("NPC_" + kind);
            go.transform.position = position;
            Vector3 facing = faceTowards - position;
            facing.y = 0f;
            if (facing.sqrMagnitude > 0.01f)
            {
                go.transform.rotation = Quaternion.LookRotation(facing);
            }

            var npc = go.AddComponent<NpcActor>();
            npc.Configure(kind);
            return npc;
        }

        private void Configure(NpcKind kind)
        {
            Kind = kind;
            AvatarLook look;
            float scale = 1f;
            switch (kind)
            {
                case NpcKind.Merchant:
                    DisplayName = "Ásta";
                    Title = "Trading Post";
                    Greeting = "Welcome, traveller! Mead, tonics and steel. And I'll buy whatever the wolves left you.";
                    look = Look(new Color(0.78f, 0.47f, 0.18f), Gender.Female, 3, 2, head: "feathered_beret", garment: "traveler_cloak");
                    break;
                case NpcKind.Forge:
                    DisplayName = "Brokk";
                    Title = "Dwarven Forge";
                    Greeting = "Hrmph. Bring ore and zeny and I'll hammer your gear harder. Past the safe line, Starmetal sings, or it shatters.";
                    look = Look(new Color(0.42f, 0.30f, 0.22f), Gender.Male, 7, 3, head: "grand_horned_viking_crest", lower: "braided_beard", shield: "round_viking_shield", weapon: WeaponType.Mace);
                    scale = 0.82f;
                    break;
                default:
                    DisplayName = "Verdandi's Courier";
                    Title = "Norn Storage";
                    Greeting = "The Norns keep what you leave with me, for every hero on your account, across every age of the world.";
                    look = Look(new Color(0.20f, 0.26f, 0.42f), Gender.Female, 2, 4, head: "raven_hood", garment: "valkyrian_feather_wings");
                    break;
            }

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
            GroundRing.Create("NpcRing_" + kind, kind == NpcKind.Forge ? new Color(1f, 0.55f, 0.2f, 0.8f) : new Color(0.95f, 0.8f, 0.35f, 0.8f), 0.85f, 0.05f)
                .ShowAt(transform.position);
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
