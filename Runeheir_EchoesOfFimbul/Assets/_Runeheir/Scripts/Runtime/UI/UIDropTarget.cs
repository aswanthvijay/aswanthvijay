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
            var payload = DragDrop.Current;
            if (payload == null)
            {
                return;
            }

            DragDrop.Accepted = true;
            Dropped?.Invoke(payload);
        }
    }
}
