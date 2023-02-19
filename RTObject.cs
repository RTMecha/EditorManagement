using System;
using LSFunctions;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Reflection;

namespace EditorManagement
{
    public class RTObject : MonoBehaviour
    {
        public bool selected;

        public void OnMouseEnter()
        {
            selected = true;
        }

        public void OnMouseExit()
        {
            selected = false;
        }
    }
}
