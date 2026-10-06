using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Runeheir.UI
{
    /// <summary>Makes a UI element draggable as a <see cref="DragPayload"/> (skills, items, hotkey slots).</summary>
    public sealed class UIDragSource : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        /// <summary>Return null to refuse the drag (e.g. empty slot).</summary>
        public Func<DragPayload> PayloadProvider;

        /// <summary>Called when the drag ends with the payload it started with; true if a drop target accepted it.</summary>
        public Action<DragPayload, bool> Ended;

        private DragPayload _payload;

        public void OnBeginDrag(PointerEventData eventData)
        {
            var payload = PayloadProvider?.Invoke();
            var canvas = GetComponentInParent<Canvas>();
            if (payload == null || canvas == null || eventData.button != PointerEventData.InputButton.Left)
            {
                eventData.pointerDrag = null;
                return;
            }

            _payload = payload;
            DragDrop.Begin(payload, eventData, canvas.rootCanvas);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (DragDrop.Current != null)
            {
                DragDrop.Move(eventData);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            var payload = _payload;
            _payload = null;
            if (DragDrop.Current == null || payload == null)
            {
                return;
            }

            // Released back over itself: the Input System UI module sends a click instead of a drop there, so
            // deliver the drop ourselves. A hotkey slot is reused for every page, so after F12 "itself" can be a
            // different slot (the legacy module drops there too); anything else just keeps its binding.
            var over = eventData.pointerCurrentRaycast.gameObject;
            bool overSelf = over != null && over.transform.IsChildOf(transform);
            if (overSelf && !DragDrop.Accepted)
            {
                var ownTarget = GetComponent<UIDropTarget>();
                if (ownTarget != null)
                {
                    ownTarget.OnDrop(eventData);
                }
            }

            bool accepted = DragDrop.Accepted || overSelf;
            DragDrop.End();
            Ended?.Invoke(payload, accepted);
        }

        // Closing the source's window (Esc/S/E) or rebuilding its rows mid-drag: uGUI never sends OnEndDrag to an
        // inactive object, so cancel here, keeping the original binding.
        private void OnDisable()
        {
            if (_payload != null && DragDrop.Current == _payload)
            {
                DragDrop.End();
            }

            _payload = null;
        }
    }
}
