using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

namespace EditorManagement.Functions
{
    public class DraggableObject : MonoBehaviour
    {
        private bool dragging;
        private Vector3 startMousePos;
        private Vector3 startPos;

        private void OnMouseUp()
        {
            dragging = false;
        }
        private void OnMouseDown()
        {
            if (ObjEditor.inst.selectedObjects.Count == 1 && ObjEditor.inst.currentObjectSelection.IsObject() && ObjEditor.inst.currentObjectSelection.GetObjectData() != null)
            {
                dragging = true;
                startMousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f);
                var beatmapObject = ObjEditor.inst.currentObjectSelection.GetObjectData();
                startPos = new Vector3(beatmapObject.events[0][0].eventValues[0], beatmapObject.events[0][0].eventValues[1], -90f);
            }
        }

        private void Update()
        {
            if (ObjEditor.inst.selectedObjects.Count == 1 && ObjEditor.inst.currentObjectSelection != null && !string.IsNullOrEmpty(ObjEditor.inst.currentObjectSelection.ID) && ObjEditor.inst.currentObjectSelection.IsObject() && ObjEditor.inst.currentObjectSelection.GetObjectData() != null)
            {
                if (ObjectManager.inst.beatmapGameObjects.ContainsKey(ObjEditor.inst.currentObjectSelection.GetObjectData().id) && ObjectManager.inst.beatmapGameObjects[ObjEditor.inst.currentObjectSelection.GetObjectData().id].rend && transform.position != new Vector3(ObjEditor.inst.currentObjectSelection.GetObjectData().events[0][0].eventValues[0], ObjEditor.inst.currentObjectSelection.GetObjectData().events[0][0].eventValues[1]))
                {
                    transform.position = new Vector3(ObjEditor.inst.currentObjectSelection.GetObjectData().events[0][0].eventValues[0], ObjEditor.inst.currentObjectSelection.GetObjectData().events[0][0].eventValues[1], -90f);
                }

                if (dragging == true)
                {
                    Vector3 vector = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f);
                    if (Input.GetKey(KeyCode.LeftShift))
                    {
                        if (Mathf.Abs(startMousePos.x - vector.x) > Mathf.Abs(startMousePos.y - vector.y) && ObjEditor.inst.currentObjectSelection.GetObjectData().events[0][0].eventValues[0] != (int)vector.x)
                        {
                            ObjEditor.inst.currentObjectSelection.GetObjectData().events[0][0].eventValues[0] = (int)vector.x / EventManager.inst.camZoom * 1.23f;
                            ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                        }
                        if (Mathf.Abs(startMousePos.x - vector.x) < Mathf.Abs(startMousePos.y - vector.y) && ObjEditor.inst.currentObjectSelection.GetObjectData().events[0][0].eventValues[1] != (int)vector.y)
                        {
                            ObjEditor.inst.currentObjectSelection.GetObjectData().events[0][0].eventValues[1] = (int)vector.y / EventManager.inst.camZoom * 1.23f;
                            ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                        }
                    }
                    else
                    {
                        float x = startMousePos.x - vector.x;
                        float y = startMousePos.y - vector.y;
                        var beatmapObject = ObjEditor.inst.currentObjectSelection.GetObjectData();

                        if (beatmapObject.events[0][0].eventValues[0] != (int)startPos.x + -x || beatmapObject.events[0][0].eventValues[1] != (int)startPos.y + -y)
                        {
                            if ((EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Timeline) && EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Object)) || !EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Prefab))
                            {
                                var xif = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/position/position/x").GetComponent<InputField>();
                                var yif = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/position/position/y").GetComponent<InputField>();

                                int newx = (int)(startPos.x + -x / EventManager.inst.camZoom * 1.23f);
                                int newy = (int)(startPos.y + -y / EventManager.inst.camZoom * 1.23f);

                                xif.text = newx.ToString();
                                yif.text = newy.ToString();
                            }
                            else
                            {
                                ObjEditor.inst.currentObjectSelection.GetObjectData().events[0][0].eventValues[0] = (int)startPos.x + -x / EventManager.inst.camZoom * 1.23f;
                                ObjEditor.inst.currentObjectSelection.GetObjectData().events[0][0].eventValues[1] = (int)startPos.y + -y / EventManager.inst.camZoom * 1.23f;
                                ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                            }
                        }
                    }
                }
            }
        }
    }
}
