using UnityEngine;

namespace Runeheir.Visuals
{
    /// <summary>Short-lived streaks for skills without art yet: piercing lines (Spear Stab, Sharp Shot) and weapon arcs.</summary>
    public sealed class SlashFx : MonoBehaviour
    {
        private LineRenderer _line;
        private Color _color;
        private float _started;
        private float _duration;
        private float _startWidth;

        /// <summary>A fading straight streak from <paramref name="from"/> towards <paramref name="towards"/>, <paramref name="length"/> long.</summary>
        public static void Line(Vector3 from, Vector3 towards, float length, Color color, float width = 0.35f, float duration = 0.3f)
        {
            Vector3 direction = towards - from;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            direction.Normalize();
            Vector3 start = from + Vector3.up * 0.9f;
            Create("SkillLine", color, width, duration, start, start + direction * length);
        }

        /// <summary>A fading horizontal arc around <paramref name="center"/> (sweeps and spins).</summary>
        public static void Arc(Vector3 center, Vector3 forward, float radius, float degrees, Color color, float width = 0.25f, float duration = 0.25f)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            const int segments = 20;
            var points = new Vector3[segments + 1];
            Quaternion facing = Quaternion.LookRotation(forward.normalized, Vector3.up);
            for (int i = 0; i <= segments; i++)
            {
                float angle = -degrees * 0.5f + degrees * i / segments;
                points[i] = center + Vector3.up * 0.9f + facing * (Quaternion.Euler(0f, angle, 0f) * Vector3.forward) * radius;
            }

            Create("SkillArc", color, width, duration, points);
        }

        private static void Create(string name, Color color, float width, float duration, params Vector3[] points)
        {
            var go = new GameObject(name);
            var fx = go.AddComponent<SlashFx>();
            fx._line = go.AddComponent<LineRenderer>();
            fx._line.useWorldSpace = true;
            fx._line.positionCount = points.Length;
            fx._line.SetPositions(points);
            fx._line.sharedMaterial = RuntimeMaterials.Unlit;
            fx._line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            fx._line.receiveShadows = false;
            fx._line.numCapVertices = 4;
            fx._line.widthMultiplier = width;
            fx._startWidth = width;
            fx._color = color;
            fx._started = Time.time;
            fx._duration = Mathf.Max(0.05f, duration);
            fx.Apply(0f);
        }

        private void Update()
        {
            float t = (Time.time - _started) / _duration;
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            Apply(t);
        }

        private void Apply(float t)
        {
            float alpha = 1f - t;
            _line.startColor = new Color(_color.r, _color.g, _color.b, alpha * 0.5f);
            _line.endColor = new Color(_color.r, _color.g, _color.b, alpha);
            _line.widthMultiplier = _startWidth * (1f - 0.5f * t);
        }
    }
}
