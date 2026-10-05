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

        /// <summary>Dialogs (menu, death) re-center every time they open, whatever the screen shape.</summary>
        public bool CenterOnShow { get; set; }

        private Vector2Int _screenSize;

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
            if (CenterOnShow)
            {
                CenterOnCanvas();
            }

            ClampToCanvas();
            VisibilityChanged?.Invoke();
        }

        /// <summary>
        /// Keeps the whole window on screen. Window positions are authored for 16:9 (1920x1080 reference); on
        /// 16:10, 4:3 or after a resize the canvas is narrower, so windows near the right edge are pulled in.
        /// </summary>
        public void ClampToCanvas()
        {
            if (!TryGetCanvasRect(out UnityEngine.Rect bounds))
            {
                return;
            }

            Vector2 size = Rect.rect.size;
            Vector2 position = Rect.anchoredPosition; // top-left anchored (see UILayout.SetRect)
            position.x = Mathf.Clamp(position.x, 0f, Mathf.Max(0f, bounds.width - size.x));
            position.y = Mathf.Clamp(position.y, -Mathf.Max(0f, bounds.height - size.y), 0f);
            Rect.anchoredPosition = position;
        }

        public void CenterOnCanvas()
        {
            if (TryGetCanvasRect(out UnityEngine.Rect bounds))
            {
                Vector2 size = Rect.rect.size;
                Rect.anchoredPosition = new Vector2((bounds.width - size.x) * 0.5f, -(bounds.height - size.y) * 0.5f);
            }
        }

        private bool TryGetCanvasRect(out UnityEngine.Rect bounds)
        {
            bounds = default;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null || Rect == null || Rect.anchorMin != new Vector2(0f, 1f) || Rect.anchorMax != new Vector2(0f, 1f))
            {
                return false;
            }

            bounds = ((RectTransform)canvas.rootCanvas.transform).rect;
            return bounds.width > 0f && bounds.height > 0f;
        }

        private void LateUpdate()
        {
            // Window resized or resolution changed while open: pull the window back on screen.
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (screen != _screenSize)
            {
                bool first = _screenSize == Vector2Int.zero;
                _screenSize = screen;
                if (!first)
                {
                    if (CenterOnShow)
                    {
                        CenterOnCanvas();
                    }

                    ClampToCanvas();
                }
            }
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
