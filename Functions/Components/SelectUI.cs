using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.EventSystems;

using LSFunctions;

namespace EditorManagement.Functions.Components
{
    public class SelectUI : MonoBehaviour, IEventSystemHandler, IPointerDownHandler, IPointerUpHandler
    {
        private bool dragging;
        private Vector3 startMousePos;
        private Vector3 startPos;
        public Vector3 ogPos;
        public Transform target;
        public float scale = 1.05f;


        public void OnPointerUp(PointerEventData eventData)
        {
            if (ConfigEntries.DragUI.Value == true)
            {
                AudioManager.inst.PlaySound("blip");
                target.localScale = new Vector3(1f, 1f, 1f);
                dragging = false;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (ConfigEntries.DragUI.Value == true)
            {
                if (!Input.GetMouseButtonDown(2))
                {
                    AudioManager.inst.PlaySound("Click");
                    target.localScale = new Vector3(scale, scale, 1f);
                    dragging = true;
                    startMousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f);
                    startPos = target.position;
                }
                else
                {
                    AudioManager.inst.PlaySound("Click");
                    target.position = ogPos;
                    startMousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f);
                    startPos = target.position;
                }
            }
        }

        private void Update()
        {
            if (dragging && ConfigEntries.DragUI.Value == true)
            {
                Vector3 vector = new Vector3(Input.mousePosition.x, Input.mousePosition.y, target.localPosition.z);
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    if (Mathf.Abs(startMousePos.x - vector.x) > Mathf.Abs(startMousePos.y - vector.y))
                        target.position = new Vector3(vector.x, target.position.y);
                    if (Mathf.Abs(startMousePos.x - vector.x) < Mathf.Abs(startMousePos.y - vector.y))
                        target.position = new Vector3(target.position.x, vector.y);
                }
                else
                {
                    float x = startMousePos.x - vector.x;
                    float y = startMousePos.y - vector.y;
                    target.position = new Vector3(startPos.x + -x, startPos.y + -y);
                }
            }

            //Start pos = Vector2(100f, 50f)
        }
    }
}
