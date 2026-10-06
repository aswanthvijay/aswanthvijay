using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Runeheir.UI
{
    /// <summary>Click / double-click / right-click / hover callbacks for any UI graphic.</summary>
    public sealed class UIPointerHandler : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public Action LeftClick;
        public Action DoubleClick;
        public Action RightClick;
        public Action PointerEnter;
        public Action PointerExit;

        private const float DoubleClickSeconds = 0.3f;

        private float _lastLeftClick = -1f;
        private bool _hovered;

        public void OnPointerClick(PointerEventData eventData)
        {
            // A drag released over its own source is not a click (InputSystemUIInputModule sends a click there).
            if (eventData.dragging)
            {
                return;
            }

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                // Tracked per handler, not with eventData.clickCount: inside a UIWindow every slot shares the
                // window as pointerPress, so clickCount would pair quick clicks on two different slots.
                float now = Time.unscaledTime;
                if (DoubleClick != null && _lastLeftClick >= 0f && now - _lastLeftClick <= DoubleClickSeconds)
                {
                    _lastLeftClick = -1f;
                    DoubleClick();
                }
                else
                {
                    _lastLeftClick = now;
                    LeftClick?.Invoke();
                }
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                RightClick?.Invoke();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            PointerEnter?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            PointerExit?.Invoke();
        }

        // Destroyed or hidden while hovered (window closed, buff icons rebuilt): end the hover. Only the hovered
        // handler may do this, or every buff-tray rebuild would hide the tooltip of whatever the pointer is on.
        private void OnDisable()
        {
            if (_hovered)
            {
                _hovered = false;
                PointerExit?.Invoke();
            }
        }
    }
}
