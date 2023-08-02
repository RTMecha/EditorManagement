using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

using EditorManagement.Functions.Editors;
using RTFunctions.Functions;

namespace EditorManagement.Functions.Components
{
    public class DraggableObject : MonoBehaviour
    {
        private bool dragging;
        public Vector3 startMousePos;
        public Vector3 startPos;
        public Vector3 startSca;
        private MeshRenderer meshRenderer;
        public Dictionary<string, GameObject> children = new Dictionary<string, GameObject>();
        public float scaleOffset = 1f;
        public int type;

        public Vector3 dragObjectPosition = new Vector3(0f, 0f, -90f);

        private void Start()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            children.Add("X Left", transform.Find("X>").gameObject);
            children.Add("X Right", transform.Find("X<").gameObject);
            children.Add("Y Up", transform.Find("YU").gameObject);
            children.Add("Y Down", transform.Find("YD").gameObject);
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
                type = 0;
                SetDraggable();
            }
        }

        public void SetDraggable()
        {
            startMousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f);
            var beatmapObject = ObjEditor.inst.currentObjectSelection.GetObjectData();
            startPos = new Vector3(beatmapObject.events[0][kfs[0]].eventValues[0], beatmapObject.events[0][kfs[0]].eventValues[1], -90f);
            startSca = new Vector3(beatmapObject.events[1][kfs[1]].eventValues[0], beatmapObject.events[1][kfs[1]].eventValues[1], 1f);
            if (kfs[type] == 0)
            {
                AudioManager.inst.CurrentAudioSource.time = beatmapObject.events[type][kfs[type]].eventTime + beatmapObject.StartTime + 0.001f;
            }
            else
            {
                AudioManager.inst.CurrentAudioSource.time = beatmapObject.events[type][kfs[type]].eventTime + beatmapObject.StartTime;
            }
            Debug.Log("Start Position: " + startPos);
            Debug.Log("Start Scale: " + startSca);
            Debug.Log("Start Mouse Position: " + startMousePos);
        }

        private void Test()
        {
            var beatmapObject = ObjEditor.inst.currentObjectSelection.GetObjectData();
            for (int i = 0; i < 4; i++)
            {
                if (beatmapObject.events[i].Find((DataManager.GameData.EventKeyframe x) => x.eventTime >= AudioManager.inst.CurrentAudioSource.time - beatmapObject.StartTime) != null)
                {
                    var nextKFE = beatmapObject.events[i].Find((DataManager.GameData.EventKeyframe x) => x.eventTime >= AudioManager.inst.CurrentAudioSource.time - beatmapObject.StartTime);
                    var nextKF = beatmapObject.events[i].IndexOf(nextKFE);
                    var prevKF = nextKF - 1;

                    if (nextKF == 0)
                    {
                        prevKF = 0;
                    }
                    else
                    {
                        var v1 = new Vector2(beatmapObject.events[i][prevKF].eventTime, 0f);
                        var v2 = new Vector2(beatmapObject.events[i][nextKF].eventTime, 0f);

                        float dis = Vector2.Distance(v1, v2);
                        float time = AudioManager.inst.CurrentAudioSource.time - beatmapObject.StartTime;

                        bool prevClose = time > dis + beatmapObject.events[i][prevKF].eventTime / 2f;
                        bool nextClose = time < beatmapObject.events[i][nextKF].eventTime - dis / 2f;

                        if (!prevClose)
                        {
                            kfs[i] = prevKF;
                        }
                        if (!nextClose)
                        {
                            kfs[i] = nextKF;
                        }
                    }
                }
                else
                {
                    kfs[i] = 0;
                }
            }
        }

        public List<int> kfs = new List<int>
        {
            0,
            0,
            0,
            0
        };

        public void GetPosition()
        {
            if (ObjEditor.inst.currentObjectSelection.IsObject() && ObjEditor.inst.currentObjectSelection.GetObjectData() != null && !string.IsNullOrEmpty(ObjEditor.inst.currentObjectSelection.ID) && EditorManager.inst.hasLoadedLevel && !RTEditor.ienumRunning)
            {
                dragObjectPosition = new Vector3(0f, 0f, depth);
                foreach (var obj in ObjEditor.inst.currentObjectSelection.GetObjectData().GetParentChain())
                {
                    dragObjectPosition += new Vector3(obj.events[0][obj.ClosestKeyframe(0)].eventValues[0], obj.events[0][obj.ClosestKeyframe(0)].eventValues[1], 0f);
                }
            }
        }

        private void Update()
        {
            if (ObjEditor.inst.selectedObjects.Count == 1 && ObjEditor.inst.currentObjectSelection != null && !string.IsNullOrEmpty(ObjEditor.inst.currentObjectSelection.ID) && ObjEditor.inst.currentObjectSelection.IsObject() && ObjEditor.inst.currentObjectSelection.GetObjectData() != null)
            {
                Test();

                //Position
                {
                    meshRenderer.enabled = true;
                    if (transform.position != dragObjectPosition)
                    {
                        transform.position = dragObjectPosition;
                    }

                    if (dragging == true)
                    {
                        Vector3 vector = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f);
                        if (Input.GetKey(KeyCode.LeftShift))
                        {
                            if (Mathf.Abs(startMousePos.x - vector.x) > Mathf.Abs(startMousePos.y - vector.y) && ObjEditor.inst.currentObjectSelection.GetObjectData().events[0][kfs[0]].eventValues[0] != (int)vector.x)
                            {
                                float x = startMousePos.x - vector.x;
                                float y = startMousePos.y - vector.y;
                                float fullX = startPos.x + -x / EventManager.inst.camZoom * 1.23f;
                                var beatmapObject = ObjEditor.inst.currentObjectSelection.GetObjectData();

                                if (beatmapObject.events[0][kfs[0]].eventValues[0] != (int)startPos.x + -x || beatmapObject.events[0][kfs[0]].eventValues[1] != (int)startPos.y + -y)
                                {
                                    if (ObjEditor.inst.keyframeSelections[0].Index == kfs[0] && ObjEditor.inst.KeyframeDialogs[0].activeSelf && ((EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Timeline) && EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Object)) || !EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Prefab)))
                                    {
                                        var xif = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/position/position/x").GetComponent<InputField>();

                                        if (Input.GetKey(KeyCode.LeftControl))
                                        {
                                            xif.text = ((int)fullX).ToString();
                                        }
                                        else
                                        {
                                            xif.text = fullX.ToString();
                                        }
                                    }
                                    else
                                    {
                                        if (Input.GetKey(KeyCode.LeftControl))
                                        {
                                            if (ObjEditor.inst.currentObjectSelection.GetObjectData().events[0][kfs[0]].eventValues[0] != (int)fullX)
                                            {
                                                ObjEditor.inst.currentObjectSelection.GetObjectData().events[0][kfs[0]].eventValues[0] = (int)fullX;
                                                ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                                            }
                                        }
                                        else
                                        {
                                            if (ObjEditor.inst.currentObjectSelection.GetObjectData().events[0][kfs[0]].eventValues[0] != fullX)
                                            {
                                                ObjEditor.inst.currentObjectSelection.GetObjectData().events[0][kfs[0]].eventValues[0] = fullX;
                                                ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                                            }
                                        }
                                    }
                                }
                            }
                            if (Mathf.Abs(startMousePos.x - vector.x) < Mathf.Abs(startMousePos.y - vector.y) && ObjEditor.inst.currentObjectSelection.GetObjectData().events[0][kfs[0]].eventValues[1] != (int)vector.y)
                            {
                                float x = startMousePos.x - vector.x;
                                float y = startMousePos.y - vector.y;
                                float fullY = startPos.y + -y / EventManager.inst.camZoom * 1.23f;
                                var beatmapObject = ObjEditor.inst.currentObjectSelection.GetObjectData();

                                if (beatmapObject.events[0][kfs[0]].eventValues[0] != (int)startPos.x + -x || beatmapObject.events[0][kfs[0]].eventValues[1] != (int)startPos.y + -y)
                                {
                                    if (ObjEditor.inst.keyframeSelections[0].Index == kfs[0] && ObjEditor.inst.KeyframeDialogs[0].activeSelf && ((EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Timeline) && EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Object)) || !EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Prefab)))
                                    {
                                        var yif = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/position/position/y").GetComponent<InputField>();

                                        if (Input.GetKey(KeyCode.LeftControl))
                                        {
                                            yif.text = ((int)fullY).ToString();
                                        }
                                        else
                                        {
                                            yif.text = fullY.ToString();
                                        }
                                    }
                                    else
                                    {
                                        if (Input.GetKey(KeyCode.LeftControl))
                                        {
                                            if (ObjEditor.inst.currentObjectSelection.GetObjectData().events[0][kfs[0]].eventValues[1] != (int)fullY)
                                            {
                                                ObjEditor.inst.currentObjectSelection.GetObjectData().events[0][kfs[0]].eventValues[1] = (int)fullY;
                                                ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                                            }
                                        }
                                        else
                                        {
                                            if (ObjEditor.inst.currentObjectSelection.GetObjectData().events[0][kfs[0]].eventValues[1] != fullY)
                                            {
                                                ObjEditor.inst.currentObjectSelection.GetObjectData().events[0][kfs[0]].eventValues[1] = fullY;
                                                ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            float x = startMousePos.x - vector.x;
                            float y = startMousePos.y - vector.y;
                            float fullX = startPos.x + -x / EventManager.inst.camZoom * 1.23f;
                            float fullY = startPos.y + -y / EventManager.inst.camZoom * 1.23f;
                            var beatmapObject = ObjEditor.inst.currentObjectSelection.GetObjectData();

                            if (beatmapObject.events[0][kfs[0]].eventValues[0] != (int)startPos.x + -x || beatmapObject.events[0][kfs[0]].eventValues[1] != (int)startPos.y + -y)
                            {
                                if (ObjEditor.inst.keyframeSelections[0].Index == kfs[0] && ObjEditor.inst.KeyframeDialogs[0].activeSelf && ((EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Timeline) && EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Object)) || !EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Prefab)))
                                {
                                    var xif = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/position/position/x").GetComponent<InputField>();
                                    var yif = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/position/position/y").GetComponent<InputField>();

                                    if (Input.GetKey(KeyCode.LeftControl))
                                    {
                                        xif.text = ((int)fullX).ToString();
                                        yif.text = ((int)fullY).ToString();
                                    }
                                    else
                                    {
                                        xif.text = fullX.ToString();
                                        yif.text = fullY.ToString();
                                    }
                                }
                                else
                                {
                                    if (Input.GetKey(KeyCode.LeftControl))
                                    {
                                        if (ObjEditor.inst.currentObjectSelection.GetObjectData().events[0][kfs[0]].eventValues[0] != (int)fullX || ObjEditor.inst.currentObjectSelection.GetObjectData().events[0][kfs[0]].eventValues[1] != (int)fullY)
                                        {
                                            ObjEditor.inst.currentObjectSelection.GetObjectData().events[0][kfs[0]].eventValues[0] = (int)fullX;
                                            ObjEditor.inst.currentObjectSelection.GetObjectData().events[0][kfs[0]].eventValues[1] = (int)fullY;
                                            ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                                        }
                                    }
                                    else
                                    {
                                        if (ObjEditor.inst.currentObjectSelection.GetObjectData().events[0][kfs[0]].eventValues[0] != fullX || ObjEditor.inst.currentObjectSelection.GetObjectData().events[0][kfs[0]].eventValues[1] != fullY)
                                        {
                                            Debug.Log("X: " + fullX + " Y: " + fullY);
                                            ObjEditor.inst.currentObjectSelection.GetObjectData().events[0][kfs[0]].eventValues[0] = fullX;
                                            ObjEditor.inst.currentObjectSelection.GetObjectData().events[0][kfs[0]].eventValues[1] = fullY;
                                            ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                //Scale
                {
                    Vector3 vector = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f);
                    float x = startMousePos.x - vector.x;
                    float y = startMousePos.y - vector.y;
                    float fullX = startSca.x + -x / EventManager.inst.camZoom * am;
                    float fullY = startSca.y + -y / EventManager.inst.camZoom * am;
                    float fullNX = startSca.x + x / EventManager.inst.camZoom * am;
                    float fullNY = startSca.y + y / EventManager.inst.camZoom * am;
                    var beatmapObject = ObjEditor.inst.currentObjectSelection.GetObjectData();

                    children["X Left"].GetComponent<MeshRenderer>().enabled = true;
                    children["X Right"].GetComponent<MeshRenderer>().enabled = true;
                    children["Y Up"].GetComponent<MeshRenderer>().enabled = true;
                    children["Y Down"].GetComponent<MeshRenderer>().enabled = true;

                    float posX = scaleOffset + ObjEditor.inst.currentObjectSelection.GetObjectData().events[1][kfs[1]].eventValues[0] / 2.3f;
                    children["X Left"].transform.localPosition = new Vector3(posX, 0f, 0f);
                    children["X Right"].transform.localPosition = new Vector3(-posX, 0f, 0f);

                    float posY = scaleOffset + ObjEditor.inst.currentObjectSelection.GetObjectData().events[1][kfs[1]].eventValues[1] / 2.3f;
                    children["Y Up"].transform.localPosition = new Vector3(0f, posY, 0f);
                    children["Y Down"].transform.localPosition = new Vector3(0f, -posY, 0f);

                    if (children["X Left"].GetComponent<DraggableChildren>().dragging)
                    {
                        if (beatmapObject.events[1][kfs[1]].eventValues[0] != (int)startSca.x + -x)
                        {
                            if (ObjEditor.inst.keyframeSelections[0].Index == kfs[1] && ObjEditor.inst.KeyframeDialogs[1].activeSelf && ((EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Timeline) && EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Object)) || !EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Prefab)))
                            {
                                var xif = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/scale/scale/x").GetComponent<InputField>();

                                if (Input.GetKey(KeyCode.LeftControl))
                                {
                                    xif.text = ((int)fullX).ToString();
                                }
                                else
                                {
                                    xif.text = fullX.ToString();
                                }
                            }
                            else
                            {
                                if (Input.GetKey(KeyCode.LeftControl))
                                {
                                    if (ObjEditor.inst.currentObjectSelection.GetObjectData().events[1][kfs[1]].eventValues[0] != (int)fullX)
                                    {
                                        ObjEditor.inst.currentObjectSelection.GetObjectData().events[1][kfs[1]].eventValues[0] = (int)fullX;
                                        ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                                    }
                                }
                                else
                                {
                                    if (ObjEditor.inst.currentObjectSelection.GetObjectData().events[1][kfs[1]].eventValues[0] != fullX)
                                    {
                                        ObjEditor.inst.currentObjectSelection.GetObjectData().events[1][kfs[1]].eventValues[0] = fullX;
                                        ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                                    }
                                }
                            }
                        }
                    }

                    if (children["X Right"].GetComponent<DraggableChildren>().dragging)
                    {
                        if (beatmapObject.events[1][kfs[1]].eventValues[0] != (int)startSca.x + -x)
                        {
                            if (ObjEditor.inst.keyframeSelections[0].Index == kfs[1] && ObjEditor.inst.KeyframeDialogs[1].activeSelf && ((EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Timeline) && EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Object)) || !EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Prefab)))
                            {
                                var xif = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/scale/scale/x").GetComponent<InputField>();

                                if (Input.GetKey(KeyCode.LeftControl))
                                {
                                    xif.text = ((int)fullNX).ToString();
                                }
                                else
                                {
                                    xif.text = fullNX.ToString();
                                }
                            }
                            else
                            {
                                if (Input.GetKey(KeyCode.LeftControl))
                                {
                                    if (ObjEditor.inst.currentObjectSelection.GetObjectData().events[1][kfs[1]].eventValues[0] != (int)fullNX)
                                    {
                                        ObjEditor.inst.currentObjectSelection.GetObjectData().events[1][kfs[1]].eventValues[0] = (int)fullNX;
                                        ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                                    }
                                }
                                else
                                {
                                    if (ObjEditor.inst.currentObjectSelection.GetObjectData().events[1][kfs[1]].eventValues[0] != fullNX)
                                    {
                                        ObjEditor.inst.currentObjectSelection.GetObjectData().events[1][kfs[1]].eventValues[0] = fullNX;
                                        ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                                    }
                                }
                            }
                        }
                    }

                    if (children["Y Up"].GetComponent<DraggableChildren>().dragging)
                    {
                        if (beatmapObject.events[1][kfs[1]].eventValues[1] != (int)startSca.y + -y)
                        {
                            if (ObjEditor.inst.keyframeSelections[0].Index == kfs[1] && ObjEditor.inst.KeyframeDialogs[1].activeSelf && ((EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Timeline) && EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Object)) || !EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Prefab)))
                            {
                                var xif = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/scale/scale/y").GetComponent<InputField>();

                                if (Input.GetKey(KeyCode.LeftControl))
                                {
                                    xif.text = ((int)fullY).ToString();
                                }
                                else
                                {
                                    xif.text = fullY.ToString();
                                }
                            }
                            else
                            {
                                if (Input.GetKey(KeyCode.LeftControl))
                                {
                                    if (beatmapObject.events[1][kfs[1]].eventValues[1] != (int)fullY)
                                    {
                                        beatmapObject.events[1][kfs[1]].eventValues[1] = (int)fullY;
                                        ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                                    }
                                }
                                else
                                {
                                    if (beatmapObject.events[1][kfs[1]].eventValues[1] != fullY)
                                    {
                                        beatmapObject.events[1][kfs[1]].eventValues[1] = fullY;
                                        ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                                    }
                                }
                            }
                        }
                    }

                    if (children["Y Down"].GetComponent<DraggableChildren>().dragging)
                    {
                        if (beatmapObject.events[1][kfs[1]].eventValues[1] != (int)startSca.y + -y)
                        {
                            if (ObjEditor.inst.keyframeSelections[0].Index == kfs[1] && ObjEditor.inst.KeyframeDialogs[1].activeSelf && ((EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Timeline) && EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Object)) || !EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Prefab)))
                            {
                                var xif = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/scale/scale/y").GetComponent<InputField>();

                                if (Input.GetKey(KeyCode.LeftControl))
                                {
                                    xif.text = ((int)fullNY).ToString();
                                }
                                else
                                {
                                    xif.text = fullNY.ToString();
                                }
                            }
                            else
                            {
                                if (Input.GetKey(KeyCode.LeftControl))
                                {
                                    if (beatmapObject.events[1][kfs[1]].eventValues[1] != (int)fullNY)
                                    {
                                        beatmapObject.events[1][kfs[1]].eventValues[1] = (int)fullNY;
                                        ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                                    }
                                }
                                else
                                {
                                    if (beatmapObject.events[1][kfs[1]].eventValues[1] != fullNY)
                                    {
                                        beatmapObject.events[1][kfs[1]].eventValues[1] = fullNY;
                                        ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                                    }
                                }
                            }
                        }
                    }

                    if (Input.GetKey(KeyCode.LeftShift) && (children["X Left"].GetComponent<DraggableChildren>().dragging || children["Y Up"].GetComponent<DraggableChildren>().dragging))
                    {
                        if (beatmapObject.events[1][kfs[1]].eventValues[0] != (int)startSca.x + -x && beatmapObject.events[1][kfs[1]].eventValues[1] != (int)startSca.y + -y)
                        {
                            if (ObjEditor.inst.keyframeSelections[0].Index == kfs[1] && ObjEditor.inst.KeyframeDialogs[1].activeSelf && ((EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Timeline) && EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Object)) || !EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Prefab)))
                            {
                                var xif = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/scale/scale/x").GetComponent<InputField>();
                                var yif = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/scale/scale/y").GetComponent<InputField>();

                                if (Input.GetKey(KeyCode.LeftControl))
                                {
                                    xif.text = ((int)fullX).ToString();
                                    yif.text = ((int)fullY).ToString();
                                }
                                else
                                {
                                    xif.text = fullX.ToString();
                                    yif.text = fullY.ToString();
                                }
                            }
                            else
                            {
                                if (Input.GetKey(KeyCode.LeftControl))
                                {
                                    if (beatmapObject.events[1][kfs[1]].eventValues[0] != (int)fullX || beatmapObject.events[1][kfs[1]].eventValues[1] != (int)fullY)
                                    {
                                        beatmapObject.events[1][kfs[1]].eventValues[0] = (int)fullX;
                                        beatmapObject.events[1][kfs[1]].eventValues[1] = (int)fullY;
                                        ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                                    }
                                }
                                else
                                {
                                    if (beatmapObject.events[1][kfs[1]].eventValues[0] != fullX || beatmapObject.events[1][kfs[1]].eventValues[1] != fullY)
                                    {
                                        beatmapObject.events[1][kfs[1]].eventValues[0] = fullX;
                                        beatmapObject.events[1][kfs[1]].eventValues[1] = fullY;
                                        ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                                    }
                                }
                            }
                        }
                    }

                    if (Input.GetKey(KeyCode.LeftShift) && (children["X Right"].GetComponent<DraggableChildren>().dragging || children["Y Down"].GetComponent<DraggableChildren>().dragging))
                    {
                        if (beatmapObject.events[1][kfs[1]].eventValues[0] != (int)startSca.x + -x && beatmapObject.events[1][kfs[1]].eventValues[1] != (int)startSca.y + -y)
                        {
                            if (ObjEditor.inst.keyframeSelections[0].Index == kfs[1] && ObjEditor.inst.KeyframeDialogs[1].activeSelf && ((EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Timeline) && EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Object)) || !EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Prefab)))
                            {
                                var xif = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/scale/scale/x").GetComponent<InputField>();
                                var yif = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/scale/scale/y").GetComponent<InputField>();

                                if (Input.GetKey(KeyCode.LeftControl))
                                {
                                    xif.text = ((int)fullNX).ToString();
                                    yif.text = ((int)fullNY).ToString();
                                }
                                else
                                {
                                    xif.text = fullNX.ToString();
                                    yif.text = fullNY.ToString();
                                }
                            }
                            else
                            {
                                if (Input.GetKey(KeyCode.LeftControl))
                                {
                                    if (beatmapObject.events[1][kfs[1]].eventValues[0] != (int)fullNX || beatmapObject.events[1][kfs[1]].eventValues[1] != (int)fullNY)
                                    {
                                        beatmapObject.events[1][kfs[1]].eventValues[0] = (int)fullNX;
                                        beatmapObject.events[1][kfs[1]].eventValues[1] = (int)fullNY;
                                        ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                                    }
                                }
                                else
                                {
                                    if (beatmapObject.events[1][kfs[1]].eventValues[0] != fullNX || beatmapObject.events[1][kfs[1]].eventValues[1] != fullNY)
                                    {
                                        beatmapObject.events[1][kfs[1]].eventValues[0] = fullNX;
                                        beatmapObject.events[1][kfs[1]].eventValues[1] = fullNY;
                                        ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void SetActive(bool active)
        {
            enabled = active;
            gameObject.GetComponent<PolygonCollider2D>().enabled = active;
            gameObject.GetComponent<MeshRenderer>().enabled = active;
        }

        public float am = 2.35f;

        public float depth = -9.8f;
    }
}
