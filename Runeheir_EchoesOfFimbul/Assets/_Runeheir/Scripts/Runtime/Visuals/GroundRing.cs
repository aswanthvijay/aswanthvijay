using UnityEngine;

namespace Runeheir.Visuals
{
    /// <summary>
    /// Flat circle drawn with a LineRenderer: click marker, target ring, AoE preview, skill pulses.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class GroundRing : MonoBehaviour
    {
        private const int Segments = 48;
        private const float GroundOffset = 0.06f;

        private LineRenderer _line;
        private Transform _follow;
        private float _radius = 0.5f;
        private Color _color = Color.white;
        private float _pulseStart;
        private float _pulseDuration;
        private float _pulseFromRadius;
        private float _pulseToRadius;
        private bool _destroyAfterPulse;
        private float _spin;

        public static GroundRing Create(string name, Color color, float radius, float width = 0.06f)
        {
            var go = new GameObject(name);
            var ring = go.AddComponent<GroundRing>();
            ring.Initialize(color, radius, width);
            return ring;
        }

        /// <summary>One-shot expanding/fading ring (skill impacts, level-up glow).</summary>
        public static void SpawnPulse(Vector3 position, Color color, float fromRadius, float toRadius, float duration, float width = 0.1f)
        {
            var ring = Create("RingPulse", color, fromRadius, width);
            ring.ShowAt(position);
            ring.Pulse(fromRadius, toRadius, duration, destroyAfter: true);
        }

        public void Initialize(Color color, float radius, float width)
        {
            _line = GetComponent<LineRenderer>();
            _line.useWorldSpace = false;
            _line.loop = true;
            _line.positionCount = Segments;
            _line.alignment = LineAlignment.TransformZ;
            _line.widthMultiplier = width;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;
            _line.sharedMaterial = RuntimeMaterials.Unlit;
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            SetColor(color);
            SetRadius(radius);
        }

        public void SetRadius(float radius)
        {
            _radius = Mathf.Max(0.01f, radius);
            for (int i = 0; i < Segments; i++)
            {
                float angle = i * Mathf.PI * 2f / Segments;
                _line.SetPosition(i, new Vector3(Mathf.Cos(angle) * _radius, Mathf.Sin(angle) * _radius, 0f));
            }
        }

        public void SetColor(Color color)
        {
            _color = color;
            _line.startColor = color;
            _line.endColor = color;
        }

        public void SetSpin(float degreesPerSecond)
        {
            _spin = degreesPerSecond;
        }

        public void ShowAt(Vector3 position)
        {
            _follow = null;
            transform.position = position + Vector3.up * GroundOffset;
            gameObject.SetActive(true);
        }

        public void Follow(Transform target)
        {
            _follow = target;
            gameObject.SetActive(target != null);
        }

        public void Hide()
        {
            _follow = null;
            gameObject.SetActive(false);
        }

        public void Pulse(float fromRadius, float toRadius, float duration, bool destroyAfter = false)
        {
            _pulseStart = Time.time;
            _pulseDuration = Mathf.Max(0.01f, duration);
            _pulseFromRadius = fromRadius;
            _pulseToRadius = toRadius;
            _destroyAfterPulse = destroyAfter;
        }

        private void LateUpdate()
        {
            if (_follow != null)
            {
                transform.position = _follow.position + Vector3.up * GroundOffset;
            }

            if (_spin != 0f)
            {
                transform.rotation = Quaternion.Euler(90f, 0f, Time.time * _spin);
            }

            if (_pulseDuration > 0f)
            {
                float t = (Time.time - _pulseStart) / _pulseDuration;
                if (t >= 1f)
                {
                    _pulseDuration = 0f;
                    if (_destroyAfterPulse)
                    {
                        Destroy(gameObject);
                    }
                    else
                    {
                        gameObject.SetActive(false);
                    }

                    return;
                }

                SetRadius(Mathf.Lerp(_pulseFromRadius, _pulseToRadius, t));
                var faded = _color;
                faded.a *= 1f - t;
                _line.startColor = faded;
                _line.endColor = faded;
            }
        }
    }
}
