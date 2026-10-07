using UnityEngine;

namespace Runeheir.Visuals
{
    /// <summary>
    /// The rune circle at a caster's feet while a spell is being cast (Ragnarok draws one under every chanting character):
    /// two rings turning against each other around a seven-pointed rune star, faded in and out with the cast pose.
    /// </summary>
    public sealed class CastCircle : MonoBehaviour
    {
        private const int StarPoints = 7;

        private GroundRing _outer;
        private GroundRing _inner;
        private LineRenderer _star;
        private Transform _follow;
        private Color _color;
        private float _alpha;

        public static CastCircle Create(Transform follow, Color color)
        {
            var go = new GameObject("CastCircle");
            var circle = go.AddComponent<CastCircle>();
            circle._follow = follow;
            circle._color = color;
            circle._outer = GroundRing.Create("CastRingOuter", color, 0.95f, 0.05f);
            circle._outer.transform.SetParent(go.transform, false);
            circle._outer.transform.localPosition = Vector3.up * 0.06f;
            circle._outer.SetSpin(35f);
            circle._inner = GroundRing.Create("CastRingInner", color, 0.72f, 0.025f);
            circle._inner.transform.SetParent(go.transform, false);
            circle._inner.transform.localPosition = Vector3.up * 0.062f;
            circle._inner.SetSpin(-55f);

            var star = new GameObject("CastStar");
            star.transform.SetParent(go.transform, false);
            star.transform.localPosition = Vector3.up * 0.065f;
            star.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            circle._star = star.AddComponent<LineRenderer>();
            circle._star.useWorldSpace = false;
            circle._star.loop = true;
            circle._star.alignment = LineAlignment.TransformZ;
            circle._star.widthMultiplier = 0.025f;
            circle._star.sharedMaterial = RuntimeMaterials.Unlit;
            circle._star.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            circle._star.receiveShadows = false;
            circle._star.positionCount = StarPoints;
            for (int i = 0; i < StarPoints; i++)
            {
                // a {7/3} heptagram: every third point of the circle
                float angle = i * 3 * Mathf.PI * 2f / StarPoints;
                circle._star.SetPosition(i, new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0f) * 0.7f);
            }

            circle.SetStrength(0f);
            return circle;
        }

        /// <summary>0 hides the circle, 1 shows it fully.</summary>
        public void SetStrength(float strength)
        {
            _alpha = Mathf.Clamp01(strength);
            bool visible = _alpha > 0.01f;
            if (gameObject.activeSelf != visible)
            {
                gameObject.SetActive(visible);
            }

            if (!visible)
            {
                return;
            }

            var c = _color;
            c.a *= _alpha;
            _outer.SetColor(c);
            _inner.SetColor(c);
            _star.startColor = c;
            _star.endColor = c;
            float scale = Mathf.Lerp(0.6f, 1f, _alpha);
            transform.localScale = new Vector3(scale, 1f, scale);
        }

        private void LateUpdate()
        {
            if (_follow == null)
            {
                RiggedBody.Discard(gameObject);
                return;
            }

            transform.position = _follow.position;
            _star.transform.localRotation = Quaternion.Euler(90f, 0f, Time.time * 20f);
        }
    }
}
