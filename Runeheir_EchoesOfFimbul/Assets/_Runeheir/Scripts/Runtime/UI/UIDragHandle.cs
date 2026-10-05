using UnityEngine;
using UnityEngine.EventSystems;

namespace Runeheir.UI
{
    /// <summary>Drag a window by its title bar; keeps a strip of it on screen.</summary>
    public sealed class UIDragHandle : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler
    {
        public RectTransform Target;

        private Canvas _canvas;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (Target != null)
            {
                Target.SetAsLastSibling();
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _canvas = GetComponentInParent<Canvas>();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (Target == null || _canvas == null)
            {
                return;
            }

            Vector2 position = Target.anchoredPosition + eventData.delta / _canvas.rootCanvas.scaleFactor;
            Rect bounds = ((RectTransform)_canvas.rootCanvas.transform).rect;
            Vector2 size = Target.rect.size;
            position.x = Mathf.Clamp(position.x, -size.x + 80f, bounds.width - 80f);
            position.y = Mathf.Clamp(position.y, -bounds.height + 30f, 0f);
            Target.anchoredPosition = position;
        }
    }
}
