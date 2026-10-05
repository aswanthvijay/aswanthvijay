using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Runeheir.UI
{
    /// <summary>Ragnarok-style draggable window: title bar, close button, content area, click-to-front.</summary>
    public sealed class UIWindow : MonoBehaviour, IPointerDownHandler
    {
        private const float TitleHeight = 26f;

        public event Action VisibilityChanged;

        public RectTransform Rect { get; private set; }

        public RectTransform Content { get; private set; }

        public Text Title { get; private set; }

        public bool IsOpen => gameObject.activeSelf;

        public static UIWindow Create(Transform parent, string title, float x, float y, float width, float height, bool closable = true)
        {
            var frame = UIFactory.CreateFramedPanel(parent, "Window_" + title, UITheme.WindowBg);
            frame.rectTransform.SetRect(x, y, width, height);
            var window = frame.gameObject.AddComponent<UIWindow>();
            window.Rect = frame.rectTransform;

            var bar = UIFactory.CreatePanel(frame.transform, "TitleBar", UITheme.TitleBar);
            bar.rectTransform.anchorMin = new Vector2(0f, 1f);
            bar.rectTransform.anchorMax = new Vector2(1f, 1f);
            bar.rectTransform.pivot = new Vector2(0.5f, 1f);
            bar.rectTransform.anchoredPosition = new Vector2(0f, -3f);
            bar.rectTransform.sizeDelta = new Vector2(-6f, TitleHeight);
            bar.gameObject.AddComponent<UIDragHandle>().Target = frame.rectTransform;

            window.Title = UIFactory.CreateText(bar.transform, title, 15, UITheme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            window.Title.rectTransform.Stretch(10f, 0f, 30f, 0f);
            UIFactory.AddShadow(window.Title, 1f);

            if (closable)
            {
                var close = UIFactory.CreateButton(bar.transform, "x", window.Hide, 14);
                close.GetComponent<RectTransform>().Anchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-3f, 0f), new Vector2(22f, 20f));
            }

            window.Content = UIFactory.CreateRect("Content", frame.transform);
            window.Content.Stretch(10f, TitleHeight + 10f, 10f, 10f);
            return window;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            VisibilityChanged?.Invoke();
        }

        public void Hide()
        {
            if (!gameObject.activeSelf)
            {
                return;
            }

            gameObject.SetActive(false);
            VisibilityChanged?.Invoke();
        }

        public void Toggle()
        {
            if (IsOpen)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            transform.SetAsLastSibling();
        }
    }
}
