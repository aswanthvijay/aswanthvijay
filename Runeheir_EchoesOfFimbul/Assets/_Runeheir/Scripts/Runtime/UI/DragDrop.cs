using Runeheir.Hotkeys;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Runeheir.UI
{
    /// <summary>What is being dragged: a skill or item reference, optionally from a hotkey slot.</summary>
    public sealed class DragPayload
    {
        public HotkeySlot Slot;

        /// <summary>Flat hotkey index when dragged out of the hotkey bar, else -1.</summary>
        public int SourceHotkeyIndex = -1;

        public string Glyph;
        public Color Color;
    }

    /// <summary>Single active drag (mouse only) with a ghost icon following the cursor.</summary>
    public static class DragDrop
    {
        private static RectTransform s_ghost;

        public static DragPayload Current { get; private set; }

        /// <summary>Set by a drop target when it accepted the payload.</summary>
        public static bool Accepted { get; set; }

        public static void Begin(DragPayload payload, PointerEventData eventData, Canvas canvas)
        {
            End();
            Current = payload;
            Accepted = false;

            var icon = UIFactory.CreateIcon(canvas.transform, payload.Glyph, payload.Color);
            s_ghost = icon.rectTransform;
            s_ghost.sizeDelta = new Vector2(44f, 44f);
            var group = icon.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.alpha = 0.85f;
            s_ghost.SetAsLastSibling();
            Move(eventData);
        }

        public static void Move(PointerEventData eventData)
        {
            if (s_ghost != null)
            {
                s_ghost.position = eventData.position;
            }
        }

        public static void End()
        {
            if (s_ghost != null)
            {
                Object.Destroy(s_ghost.gameObject);
                s_ghost = null;
            }

            Current = null;
        }
    }
}
