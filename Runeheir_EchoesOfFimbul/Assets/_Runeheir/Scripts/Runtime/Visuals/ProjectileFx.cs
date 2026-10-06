using System;
using Runeheir.Combat;
using UnityEngine;

namespace Runeheir.Visuals
{
    /// <summary>
    /// A flying arrow or bolt (bow attacks, Muspel Bolt, Double Strafe...). It homes on the target's chest and
    /// calls back on arrival, so damage lands when the projectile hits, not when it is fired. Drawn with a
    /// vertex-colored line streak plus a short trail, like the ground rings.
    /// </summary>
    public sealed class ProjectileFx : MonoBehaviour
    {
        public const float ArrowSpeed = 24f;
        public const float BoltSpeed = 16f;
        private const float MaxLifetime = 3f;

        public static readonly Color ArrowColor = new Color(0.96f, 0.9f, 0.74f, 1f);

        private CombatEntity _target;
        private Action _onArrive;
        private Vector3 _aimPoint;
        private float _speed;
        private float _length;
        private float _age;
        private LineRenderer _streak;

        /// <summary>Fires from <paramref name="from"/> at <paramref name="target"/>; <paramref name="onArrive"/> runs on impact.</summary>
        public static void Launch(CombatEntity from, CombatEntity target, Color color, float speed, Action onArrive, bool arrow = false, float width = 0.07f)
        {
            if (from == null || target == null)
            {
                onArrive?.Invoke();
                return;
            }

            Vector3 start = from.Position + Vector3.up * (from.Height * 0.6f) + from.transform.forward * Mathf.Max(0.3f, from.Radius);
            var go = new GameObject(arrow ? "Arrow" : "Bolt");
            go.transform.position = start;
            go.AddComponent<ProjectileFx>().Initialize(target, color, speed, onArrive, arrow, width);
        }

        private void Initialize(CombatEntity target, Color color, float speed, Action onArrive, bool arrow, float width)
        {
            _target = target;
            _onArrive = onArrive;
            _speed = Mathf.Max(1f, speed);
            _length = arrow ? 0.6f : 0.3f;
            _aimPoint = AimPoint(target);

            _streak = gameObject.AddComponent<LineRenderer>();
            _streak.useWorldSpace = true;
            _streak.positionCount = 2;
            _streak.widthMultiplier = arrow ? width * 0.6f : width * 1.6f;
            _streak.sharedMaterial = RuntimeMaterials.Unlit;
            _streak.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _streak.receiveShadows = false;
            _streak.startColor = new Color(color.r, color.g, color.b, 0.15f);
            _streak.endColor = color;
            _streak.SetPosition(0, transform.position);
            _streak.SetPosition(1, transform.position);

            if (!arrow)
            {
                var trail = gameObject.AddComponent<TrailRenderer>();
                trail.time = 0.2f;
                trail.minVertexDistance = 0.05f;
                trail.widthMultiplier = width * 2.2f;
                trail.sharedMaterial = RuntimeMaterials.Unlit;
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                trail.receiveShadows = false;
                var gradient = new Gradient();
                gradient.SetKeys(
                    new[] { new GradientColorKey(color, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
                trail.colorGradient = gradient;
            }
        }

        private static Vector3 AimPoint(CombatEntity target)
        {
            return target.Position + Vector3.up * (target.Height * 0.55f);
        }

        private void Update()
        {
            if (_target != null && _target.isActiveAndEnabled)
            {
                _aimPoint = AimPoint(_target);
            }

            _age += Time.deltaTime;
            Vector3 position = transform.position;
            Vector3 toTarget = _aimPoint - position;
            float step = _speed * Time.deltaTime;
            if (toTarget.magnitude <= step + 0.05f || _age >= MaxLifetime)
            {
                Arrive();
                return;
            }

            Vector3 direction = toTarget.normalized;
            position += direction * step;
            transform.position = position;
            _streak.SetPosition(0, position - direction * _length);
            _streak.SetPosition(1, position);
        }

        private void Arrive()
        {
            var callback = _onArrive;
            _onArrive = null;
            Destroy(gameObject);
            callback?.Invoke();
        }
    }
}
