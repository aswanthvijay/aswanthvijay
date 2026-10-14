using UnityEngine;

namespace Runeheir.Visuals
{
    /// <summary>
    /// Art Pass 3: a fire that lives. The flame stretches and leans a little and its light breathes with it, each fire on
    /// its own rhythm (braziers, torches, campfires and hearths share this).
    /// </summary>
    public sealed class FlameFlicker : MonoBehaviour
    {
        private Vector3 _baseScale;
        private Light _light;
        private float _lightBase;
        private float _seed;
        private bool _ready;

        public void Bind(Light light)
        {
            _light = light;
        }

        private void Update()
        {
            if (!_ready)
            {
                _ready = true;
                _baseScale = transform.localScale;
                _seed = (transform.position.x * 13.7f + transform.position.z * 7.3f) % 100f;
                _lightBase = _light != null ? _light.intensity : 0f;
            }

            float t = Time.time * 7f + _seed;
            float noise = Mathf.PerlinNoise(t * 0.6f, _seed) - 0.5f;
            float stretch = 1f + 0.06f * Mathf.Sin(t * 1.9f) + 0.12f * noise;
            transform.localScale = new Vector3(_baseScale.x * (2f - stretch), _baseScale.y * stretch, _baseScale.z * (2f - stretch));
            transform.localRotation = Quaternion.Euler(noise * 8f, 0f, Mathf.Sin(t * 1.3f) * 4f);
            if (_light != null)
            {
                _light.intensity = _lightBase * (0.85f + 0.3f * (noise + 0.5f));
            }
        }
    }
}
