using System.Collections.Generic;
using Runeheir.Combat;
using Runeheir.Player;
using Runeheir.UI;
using Runeheir.Visuals;
using Runeheir.World;
using UnityEngine;

namespace Runeheir.WorldBuilding
{
    /// <summary>
    /// A Ragnarok-style warp: a swirling ring on the ground. Step into it and you travel to the linked map, arriving next
    /// to its partner portal. Dungeon stairs glow blue, roads between maps glow rose.
    /// </summary>
    public sealed class WarpPortal : MonoBehaviour
    {
        public const float TriggerRadius = 1.3f;

        private static readonly List<WarpPortal> Registry = new List<WarpPortal>();

        /// <summary>Walking into it only counts after the player has been outside it (no bouncing back on arrival).</summary>
        private bool _armed;

        public static IReadOnlyList<WarpPortal> All => Registry;

        public MapPortal Portal { get; private set; }

        public Vector3 Position => transform.position;

        public static WarpPortal Create(MapPortal portal, Vector3 position, Transform parent)
        {
            var go = new GameObject("Warp_" + portal.Id);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var warp = go.AddComponent<WarpPortal>();
            warp.Portal = portal;
            warp.BuildVisuals();
            return warp;
        }

        private bool IsStairs => Portal.Id == "up" || Portal.Id == "down";

        private void BuildVisuals()
        {
            Color color = IsStairs ? new Color(0.45f, 0.75f, 1f, 1f) : new Color(1f, 0.45f, 0.62f, 1f);
            var outer = GroundRing.Create("WarpRing", color, 1.35f, 0.12f);
            outer.transform.SetParent(transform, false);
            outer.ShowAt(transform.position + Vector3.up * 0.04f);
            outer.SetSpin(70f);
            var inner = GroundRing.Create("WarpRingInner", new Color(color.r, color.g, color.b, 0.7f), 0.8f, 0.08f);
            inner.transform.SetParent(transform, false);
            inner.ShowAt(transform.position + Vector3.up * 0.05f);
            inner.SetSpin(-110f);

            var column = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            column.name = "LightColumn";
            Object.DestroyImmediate(column.GetComponent<Collider>());
            column.transform.SetParent(transform, false);
            column.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            column.transform.localScale = new Vector3(2.2f, 1.6f, 2.2f);
            var renderer = column.GetComponent<Renderer>();
            renderer.sharedMaterial = RuntimeMaterials.Translucent(new Color(color.r, color.g, color.b, 0.16f));
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var light = new GameObject("Light").AddComponent<Light>();
            light.transform.SetParent(transform, false);
            light.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            light.type = LightType.Point;
            light.color = color;
            light.range = 6f;
            light.intensity = 1.6f;
            light.shadows = LightShadows.None;

            var target = MapCatalog.Get(Portal.TargetMap);
            string level = target != null && target.MinLevel > 0 ? $"\n<size=11>{target.LevelLabel}</size>" : string.Empty;
            WorldLabel.Attach(gameObject, $"{Portal.Label}{level}", IsStairs ? new Color(0.7f, 0.88f, 1f) : new Color(1f, 0.78f, 0.86f), 3.3f, 14);
        }

        private void Update()
        {
            var player = PlayerCharacter.Local;
            if (player == null || player.IsDead || WorldTravel.InTransit)
            {
                return;
            }

            float distance = CombatEntity.HorizontalDistance(player.Position, transform.position);
            if (!_armed)
            {
                _armed = distance > TriggerRadius + 0.8f;
                return;
            }

            if (distance <= TriggerRadius)
            {
                var target = MapCatalog.Get(Portal.TargetMap);
                WorldTravel.Warp(player, Portal.TargetMap, Portal.TargetPortal, target != null ? $"You travel to {target.Name}." : null);
            }
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
