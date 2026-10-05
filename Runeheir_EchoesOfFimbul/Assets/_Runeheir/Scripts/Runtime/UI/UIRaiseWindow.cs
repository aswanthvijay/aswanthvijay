using UnityEngine;
using UnityEngine.EventSystems;

namespace Runeheir.UI
{
    /// <summary>
    /// Brings the parent <see cref="UIWindow"/> to the front when a button or toggle inside it is pressed.
    /// uGUI stops pointer-down at the first handler (the Selectable), so the window's own handler never runs;
    /// handlers on the same GameObject all receive the event, so this sits next to the Selectable.
    /// </summary>
    public sealed class UIRaiseWindow : MonoBehaviour, IPointerDownHandler
    {
        public void OnPointerDown(PointerEventData eventData)
        {
            var window = GetComponentInParent<UIWindow>();
            if (window != null)
            {
                window.transform.SetAsLastSibling();
            }
        }
    }
}
