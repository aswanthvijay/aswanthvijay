using Runeheir.Controls;
using UnityEngine;
using UnityEngine.UI;

namespace Runeheir.UI
{
    /// <summary>One tooltip per canvas that follows the mouse (skill/item descriptions).</summary>
    public sealed class UITooltip : MonoBehaviour
    {
        private const float Width = 300f;

        private static UITooltip s_instance;

        private RectTransform _rect;
        private Text _text;
        private Canvas _canvas;

        public static void Create(Canvas canvas)
        {
            var panel = UIFactory.CreateFramedPanel(canvas.transform, "Tooltip", new Color(0.05f, 0.07f, 0.1f, 0.97f));
            panel.raycastTarget = false;
            foreach (var graphic in panel.GetComponentsInChildren<Graphic>())
            {
                graphic.raycastTarget = false;
            }

            var tooltip = panel.gameObject.AddComponent<UITooltip>();
            tooltip._canvas = canvas;
            tooltip._rect = panel.rectTransform;
            tooltip._rect.pivot = new Vector2(0f, 1f);
            tooltip._rect.anchorMin = tooltip._rect.anchorMax = new Vector2(0f, 0f);
            tooltip._rect.sizeDelta = new Vector2(Width, 60f);
            tooltip._text = UIFactory.CreateText(panel.transform, string.Empty, 15, UITheme.Text, TextAnchor.UpperLeft);
            tooltip._text.verticalOverflow = VerticalWrapMode.Overflow;
            tooltip._text.rectTransform.Stretch(10f, 8f, 10f, 8f);
            panel.gameObject.SetActive(false);
            s_instance = tooltip;
        }

        public static void Show(string content)
        {
            if (s_instance == null || string.IsNullOrEmpty(content))
            {
                return;
            }

            s_instance._text.text = content;
            s_instance.gameObject.SetActive(true);
            s_instance.transform.SetAsLastSibling();
            s_instance.Resize();
            s_instance.Follow();
        }

        public static void Hide()
        {
            if (s_instance != null)
            {
                s_instance.gameObject.SetActive(false);
            }
        }

        private void Resize()
        {
            Canvas.ForceUpdateCanvases();
            float height = _text.preferredHeight + 18f;
            _rect.sizeDelta = new Vector2(Width, height);
        }

        private void LateUpdate()
        {
            Follow();
        }

        private void Follow()
        {
            float scale = _canvas != null ? _canvas.scaleFactor : 1f;
            Vector2 pointer = GameInput.PointerPosition;
            Vector2 size = _rect.sizeDelta * scale;
            float x = Mathf.Min(pointer.x + 18f, Screen.width - size.x - 4f);
            float y = pointer.y - 18f;
            if (y - size.y < 0f)
            {
                y = pointer.y + size.y + 12f;
            }

            _rect.position = new Vector2(x, y);
        }

        private void OnDestroy()
        {
            if (s_instance == this)
            {
                s_instance = null;
            }
        }
    }
}
