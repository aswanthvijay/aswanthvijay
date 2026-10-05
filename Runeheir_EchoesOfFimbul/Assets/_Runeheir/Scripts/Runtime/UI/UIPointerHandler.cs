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

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (eventData.clickCount >= 2 && DoubleClick != null)
                {
                    DoubleClick();
                }
                else
                {
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
            PointerEnter?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            PointerExit?.Invoke();
        }

        private void OnDisable()
        {
            PointerExit?.Invoke();
        }
    }
}
