using Runeheir.Combat;
using Runeheir.Skills;
using Runeheir.Visuals;
using UnityEngine;

namespace Runeheir.Player
{
    /// <summary>
    /// Odin's ravens (Phase 8, Ragnarok's falcon): Huginn perches on a Ranger's or Deadeye's shoulder, swoops at the target
    /// for Raven Strike and Raven Assault, and now and then strikes on his own during auto-attacks (see
    /// <see cref="PlayerCharacter"/>: a chance of LUK ÷ 3 %, like Ragnarok's auto Blitz Beat).
    /// </summary>
    public sealed class RavenCompanion : MonoBehaviour
    {
        private const float SwoopSeconds = 0.7f;
        private static readonly Vector3 Perch = new Vector3(-0.24f, 1.62f, -0.04f);

        private Transform _owner;
        private Transform _wingL;
        private Transform _wingR;
        private Vector3 _swoopTarget;
        private float _swoopStart = -10f;

        /// <summary>Keeps a raven on <paramref name="owner"/>'s shoulder when <paramref name="wanted"/>, or takes it away.</summary>
        public static RavenCompanion Sync(RavenCompanion current, Transform owner, bool wanted)
        {
            if (!wanted || owner == null)
            {
                if (current != null)
                {
                    Destroy(current.gameObject);
                }

                return null;
            }

            if (current != null)
            {
                return current;
            }

            var go = new GameObject("Raven [Huginn]");
            var raven = go.AddComponent<RavenCompanion>();
            raven._owner = owner;
            raven.Build();
            go.transform.position = owner.TransformPoint(Perch);
            return raven;
        }

        /// <summary>Huginn dives at <paramref name="target"/> and comes back.</summary>
        public void Swoop(Vector3 target)
        {
            _swoopTarget = target + Vector3.up * 0.9f;
            _swoopStart = Time.time;
        }

        private void Build()
        {
            var black = new Color(0.08f, 0.08f, 0.11f);
            Part(PrimitiveType.Sphere, transform, "Body", Vector3.zero, new Vector3(0.14f, 0.13f, 0.22f), black);
            Part(PrimitiveType.Sphere, transform, "Head", new Vector3(0f, 0.07f, 0.11f), Vector3.one * 0.09f, black);
            Part(PrimitiveType.Cube, transform, "Beak", new Vector3(0f, 0.06f, 0.17f), new Vector3(0.025f, 0.025f, 0.07f), new Color(0.25f, 0.22f, 0.2f));
            Part(PrimitiveType.Cube, transform, "Tail", new Vector3(0f, -0.02f, -0.15f), new Vector3(0.08f, 0.015f, 0.1f), black);
            _wingL = new GameObject("WingL").transform;
            _wingL.SetParent(transform, false);
            _wingL.localPosition = new Vector3(-0.06f, 0.02f, 0f);
            Part(PrimitiveType.Cube, _wingL, "Wing", new Vector3(-0.08f, 0f, -0.02f), new Vector3(0.16f, 0.015f, 0.14f), black);
            _wingR = new GameObject("WingR").transform;
            _wingR.SetParent(transform, false);
            _wingR.localPosition = new Vector3(0.06f, 0.02f, 0f);
            Part(PrimitiveType.Cube, _wingR, "Wing", new Vector3(0.08f, 0f, -0.02f), new Vector3(0.16f, 0.015f, 0.14f), black);
        }

        private static void Part(PrimitiveType type, Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = RuntimeMaterials.Lit(color, 0.8f);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private void LateUpdate()
        {
            if (_owner == null)
            {
                Destroy(gameObject);
                return;
            }

            var perch = _owner.TransformPoint(Perch);
            float t = (Time.time - _swoopStart) / SwoopSeconds;
            float flap;
            if (t >= 0f && t < 1f)
            {
                // Out to the target and back on an arc.
                float leg = t < 0.5f ? t * 2f : (1f - t) * 2f;
                var position = Vector3.Lerp(perch, _swoopTarget, Mathf.SmoothStep(0f, 1f, leg)) + Vector3.up * Mathf.Sin(leg * Mathf.PI) * 0.6f;
                var heading = (t < 0.5f ? _swoopTarget - perch : perch - _swoopTarget);
                heading.y = 0f;
                transform.position = position;
                if (heading.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(heading.normalized, Vector3.up);
                }

                flap = Mathf.Sin(Time.time * 40f) * 50f;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, perch, Mathf.Clamp01(Time.deltaTime * 20f));
                transform.rotation = Quaternion.Slerp(transform.rotation, _owner.rotation, Time.deltaTime * 10f);
                flap = Mathf.Sin(Time.time * 2f) * 4f;
            }

            _wingL.localRotation = Quaternion.Euler(0f, 0f, flap);
            _wingR.localRotation = Quaternion.Euler(0f, 0f, -flap);
        }
    }
}
