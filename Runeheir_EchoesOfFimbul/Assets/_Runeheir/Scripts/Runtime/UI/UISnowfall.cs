using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Runeheir.UI
{
    /// <summary>Drifting UI snow for the Fimbulwinter login screen (render-pipeline independent).</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class UISnowfall : MonoBehaviour
    {
        [SerializeField, Min(1)] private int flakeCount = 90;
        [SerializeField] private Vector2 fallSpeed = new Vector2(25f, 70f);

        private readonly List<RectTransform> _flakes = new List<RectTransform>();
        private readonly List<Vector3> _motion = new List<Vector3>();
        private RectTransform _area;

        private void Start()
        {
            _area = (RectTransform)transform;
            for (int i = 0; i < flakeCount; i++)
            {
                var flake = UIFactory.CreatePanel(transform, "Flake", new Color(1f, 1f, 1f, Random.Range(0.25f, 0.8f)), rounded: true, blocksRaycasts: false);
                float size = Random.Range(3f, 8f);
                flake.rectTransform.anchorMin = flake.rectTransform.anchorMax = Vector2.zero;
                flake.rectTransform.sizeDelta = new Vector2(size, size);
                flake.rectTransform.anchoredPosition = new Vector2(Random.Range(0f, 1920f), Random.Range(0f, 1080f));
                _flakes.Add(flake.rectTransform);
                _motion.Add(new Vector3(Random.Range(fallSpeed.x, fallSpeed.y), Random.Range(0.3f, 1.2f), Random.Range(0f, 6.28f)));
            }
        }

        private void Update()
        {
            Rect area = _area.rect;
            for (int i = 0; i < _flakes.Count; i++)
            {
                var motion = _motion[i];
                var position = _flakes[i].anchoredPosition;
                position.y -= motion.x * Time.deltaTime;
                position.x += Mathf.Sin(Time.time * motion.y + motion.z) * 20f * Time.deltaTime;
                if (position.y < -10f)
                {
                    position.y = area.height + 10f;
                    position.x = Random.Range(0f, area.width);
                }

                _flakes[i].anchoredPosition = position;
            }
        }
    }
}
