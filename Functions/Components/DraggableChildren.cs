using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

namespace EditorManagement.Functions.Components
{
    public class DraggableChildren : MonoBehaviour
    {
        public bool dragging;
        private MeshRenderer meshRenderer;

        private void Start()
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }

        private void OnMouseUp()
        {
            dragging = false;
        }

        private void OnMouseDown()
        {
            if (ObjEditor.inst.selectedObjects.Count == 1 && ObjEditor.inst.currentObjectSelection.IsObject() && ObjEditor.inst.currentObjectSelection.GetObjectData() != null)
            {
                dragging = true;
                transform.parent.GetComponent<DraggableObject>().type = 1;
                transform.parent.GetComponent<DraggableObject>().SetDraggable();
            }
        }
    }
}
