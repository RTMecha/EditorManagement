using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.EventSystems;

namespace EditorManagement.Functions.Components.Example
{
    public class ExampleClickable : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler
    {
        public Action<PointerEventData> onClick;
        public Action<PointerEventData> onDown;
        public Action<PointerEventData> onEnter;
        public Action<PointerEventData> onExit;
        public Action<PointerEventData> onUp;

        public void OnPointerClick(PointerEventData pointerEventData)
        {
            if (onClick != null) onClick(pointerEventData);
        }

        public void OnPointerDown(PointerEventData pointerEventData)
        {
            if (onDown != null) onDown(pointerEventData);
        }

        public void OnPointerEnter(PointerEventData pointerEventData)
        {
            if (onEnter != null) onEnter(pointerEventData);
        }

        public void OnPointerExit(PointerEventData pointerEventData)
        {
            if (onExit != null) onExit(pointerEventData);
        }

        public void OnPointerUp(PointerEventData pointerEventData)
        {
            if (onUp != null) onUp(pointerEventData);
        }
    }
}
