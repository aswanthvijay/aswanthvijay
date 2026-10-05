using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Runeheir.UI
{
    /// <summary>Accepts a <see cref="DragPayload"/> (hotkey slots).</summary>
    public sealed class UIDropTarget : MonoBehaviour, IDropHandler
    {
        public Action<DragPayload> Dropped;

        public void OnDrop(PointerEventData eventData)
        {
            // Only accept drops from an active drag source (window title-bar drags carry no payload).
            var payload = DragDrop.Current;
            if (payload == null || eventData.pointerDrag == null || eventData.pointerDrag.GetComponent<UIDragSource>() == null)
            {
                return;
            }

            DragDrop.Accepted = true;
            Dropped?.Invoke(payload);
        }
    }
}
