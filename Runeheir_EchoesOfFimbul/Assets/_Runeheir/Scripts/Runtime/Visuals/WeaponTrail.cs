using System.Collections.Generic;
using UnityEngine;

namespace Runeheir.Visuals
{
    /// <summary>
    /// The arc a blade leaves through the air (the concept art's glowing slash): a ribbon between two points on the weapon,
    /// sampled every frame while <see cref="Emitting"/> and fading out over <see cref="Lifetime"/> seconds.
    /// Lives at the scene root so it stays where the swing happened.
    /// </summary>
    public sealed class WeaponTrail : MonoBehaviour
    {
        public const float Lifetime = 0.16f;
        private const int MaxSamples = 32;

        private readonly List<(Vector3 Base, Vector3 Tip, float Time)> _samples = new List<(Vector3, Vector3, float)>();
        private Transform _weapon;
        private Vector3 _baseLocal;
        private Vector3 _tipLocal;
        private Color _color;
        private Mesh _mesh;
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Color> _colors = new List<Color>();
        private readonly List<int> _triangles = new List<int>();

        public bool Emitting { get; set; }

        public static WeaponTrail Create(Transform weapon, Vector3 baseLocal, Vector3 tipLocal, Color color)
        {
            var go = new GameObject("WeaponTrail");
            var trail = go.AddComponent<WeaponTrail>();
            trail._weapon = weapon;
            trail._baseLocal = baseLocal;
            trail._tipLocal = tipLocal;
            trail._color = color;
            trail._mesh = new Mesh { name = "WeaponTrail" };
            trail._mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = trail._mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = RuntimeMaterials.Unlit;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return trail;
        }

        private void LateUpdate()
        {
            if (_weapon == null)
            {
                RiggedBody.Discard(gameObject);
                return;
            }

            float now = Time.time;
            if (Emitting && _weapon.gameObject.activeInHierarchy)
            {
                _samples.Add((_weapon.TransformPoint(_baseLocal), _weapon.TransformPoint(_tipLocal), now));
                if (_samples.Count > MaxSamples)
                {
                    _samples.RemoveAt(0);
                }
            }

            while (_samples.Count > 0 && now - _samples[0].Time > Lifetime)
            {
                _samples.RemoveAt(0);
            }

            Rebuild(now);
        }

        private void Rebuild(float now)
        {
            _mesh.Clear();
            if (_samples.Count < 2)
            {
                return;
            }

            _vertices.Clear();
            _colors.Clear();
            _triangles.Clear();
            for (int i = 0; i < _samples.Count; i++)
            {
                var (b, t, time) = _samples[i];
                float fade = 1f - Mathf.Clamp01((now - time) / Lifetime);
                var tip = _color;
                tip.a *= fade;
                var root = _color;
                root.a *= fade * 0.15f;
                _vertices.Add(b);
                _vertices.Add(t);
                _colors.Add(root);
                _colors.Add(tip);
                if (i > 0)
                {
                    int v = i * 2;
                    _triangles.Add(v - 2);
                    _triangles.Add(v - 1);
                    _triangles.Add(v + 1);
                    _triangles.Add(v - 2);
                    _triangles.Add(v + 1);
                    _triangles.Add(v);
                }
            }

            _mesh.SetVertices(_vertices);
            _mesh.SetColors(_colors);
            _mesh.SetTriangles(_triangles, 0);
            _mesh.RecalculateBounds();
        }

        private void OnDestroy()
        {
            if (_mesh != null)
            {
                RiggedBody.Discard(_mesh);
            }
        }
    }
}
