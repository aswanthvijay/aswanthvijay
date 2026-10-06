using UnityEngine;

namespace Runeheir.Visuals
{
    /// <summary>
    /// The warning circle under a monster's area attack: a faint disc showing where it will land, filling up as the cast
    /// bar does, so players can step out before it goes off.
    /// </summary>
    public sealed class SkillTelegraph : MonoBehaviour
    {
        private Transform _fill;
        private GroundRing _edge;
        private float _radius;
        private float _start;
        private float _duration;

        public static SkillTelegraph Show(Vector3 center, float radius, float duration, Color color)
        {
            var go = new GameObject("Telegraph");
            go.transform.position = new Vector3(center.x, center.y + 0.03f, center.z);
            var telegraph = go.AddComponent<SkillTelegraph>();
            telegraph.Build(radius, duration, color);
            return telegraph;
        }

        public void Close()
        {
            if (_edge != null)
            {
                Destroy(_edge.gameObject);
            }

            Destroy(gameObject);
        }

        private void Build(float radius, float duration, Color color)
        {
            _radius = Mathf.Max(0.2f, radius);
            _start = Time.time;
            _duration = Mathf.Max(0.05f, duration);
            Disc("Area", _radius, new Color(color.r, color.g, color.b, 0.18f), 0f);
            _fill = Disc("Fill", 0.01f, new Color(color.r, color.g, color.b, 0.32f), 0.01f);
            _edge = GroundRing.Create("TelegraphEdge", new Color(color.r, color.g, color.b, 0.9f), _radius, 0.08f);
            _edge.ShowAt(transform.position + Vector3.up * 0.02f);
        }

        private Transform Disc(string name, float radius, Color color, float lift)
        {
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = name;
            Destroy(disc.GetComponent<Collider>());
            disc.transform.SetParent(transform, false);
            disc.transform.localPosition = new Vector3(0f, lift, 0f);
            disc.transform.localScale = new Vector3(radius * 2f, 0.005f, radius * 2f);
            var renderer = disc.GetComponent<Renderer>();
            renderer.sharedMaterial = RuntimeMaterials.Translucent(color);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return disc.transform;
        }

        private void Update()
        {
            float t = Mathf.Clamp01((Time.time - _start) / _duration);
            float r = Mathf.Max(0.01f, _radius * t);
            _fill.localScale = new Vector3(r * 2f, 0.005f, r * 2f);
            if (Time.time > _start + _duration + 3f)
            {
                Close(); // a cast that never resolved (its caster vanished)
            }
        }
    }
}
