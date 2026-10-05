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

        /// <summary>Called when the drag ends; the argument is true if a drop target accepted it.</summary>
        public Action<bool> Ended;

        public void OnBeginDrag(PointerEventData eventData)
        {
            var payload = PayloadProvider?.Invoke();
            var canvas = GetComponentInParent<Canvas>();
            if (payload == null || canvas == null || eventData.button != PointerEventData.InputButton.Left)
            {
                eventData.pointerDrag = null;
                return;
            }

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
            if (DragDrop.Current == null)
            {
                return;
            }

            bool accepted = DragDrop.Accepted;
            DragDrop.End();
            Ended?.Invoke(accepted);
        }
    }
}
