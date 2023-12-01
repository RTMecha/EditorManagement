using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using HarmonyLib;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using SimpleJSON;
using LSFunctions;

using EditorManagement.Functions;
using EditorManagement.Functions.Editors;
using EditorManagement.Functions.Helpers;

using RTFunctions.Functions;
using RTFunctions.Functions.Data;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Optimization;
using RTFunctions.Patchers;

using BasePrefab = DataManager.GameData.Prefab;
using BaseBeatmapObject = DataManager.GameData.BeatmapObject;
using BasePrefabObject = DataManager.GameData.PrefabObject;

namespace EditorManagement.Patchers
{
    [HarmonyPatch(typeof(PrefabEditor))]
    public class PrefabEditorPatch : MonoBehaviour
    {
        static PrefabEditor Instance => PrefabEditor.inst;

        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        static bool StartPrefix()
        {
            //__instance.StartCoroutine(RTEditor.LoadExternalPrefabs(__instance));
            Instance.OffsetLine = Instantiate(Instance.OffsetLinePrefab);
            Instance.OffsetLine.name = "offset line";
            Instance.OffsetLine.transform.SetParent(EditorManager.inst.timeline.transform);
            Instance.OffsetLine.transform.localScale = Vector3.one;

            Debug.Log($"{EditorPlugin.className}Creating prefab types...");
            var transform = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/PrefabDialog/data/type/types").transform;

            var list = new List<GameObject>();
            for (int i = 0; i < transform.childCount; i++)
            {
                var tf = transform.Find($"col_{i}");
                if (tf && i != 0)
                    list.Add(tf.gameObject);
            }

            foreach (var go in list)
                Destroy(go);

            for (int i = 0; i < 20; i++)
            {
                var color = Instantiate(transform.GetChild(0).gameObject);
                color.transform.SetParent(transform);
                color.transform.SetSiblingIndex(i + 1);
                color.transform.localScale = Vector3.one;
                color.name = $"col_{i}";
            }

            Instance.externalPrefabDialog = EditorManager.inst.GetDialog("Prefab Popup").Dialog.Find("external prefabs");
            Instance.internalPrefabDialog = EditorManager.inst.GetDialog("Prefab Popup").Dialog.Find("internal prefabs");
            Instance.externalSearch = Instance.externalPrefabDialog.Find("search-box/search").GetComponent<InputField>();
            Instance.internalSearch = Instance.internalPrefabDialog.Find("search-box/search").GetComponent<InputField>();
            Instance.externalContent = Instance.externalPrefabDialog.Find("mask/content");
            Instance.internalContent = Instance.internalPrefabDialog.Find("mask/content");

            Instance.gridSearch = PrefabEditor.inst.dialog.Find("data/selection/search-box/search").GetComponent<InputField>();
            Instance.gridContent = PrefabEditor.inst.dialog.Find("data/selection/mask/content");

            if (RTExtensions.TryFind("Editor Systems/Editor GUI/sizer/main/EditorDialogs/PrefabDialog/data/type/types", out GameObject gm) && gm.TryGetComponent(out VerticalLayoutGroup component))
            {
                Destroy(component);
            }

            PrefabEditorManager.inst.prefabSelectorLeft = EditorManager.inst.GetDialog("Prefab Selector").Dialog.Find("data/left");
            PrefabEditorManager.inst.prefabSelectorRight = EditorManager.inst.GetDialog("Prefab Selector").Dialog.Find("data/right");

            var eventDialogTMP = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right");

            var singleInput = eventDialogTMP.transform.Find("move/position/x");
            var vector2Input = eventDialogTMP.transform.Find("move/position");
            var labelTemp = eventDialogTMP.transform.Find("move").transform.GetChild(8).gameObject;

            List<GameObject> toDelete = new List<GameObject>
            {
                PrefabEditorManager.inst.prefabSelectorLeft.GetChild(4).gameObject,
                PrefabEditorManager.inst.prefabSelectorLeft.GetChild(5).gameObject
            };

            foreach (var obj in toDelete)
            {
                Destroy(obj);
            }

            // Time
            {
                var timeLabel = Instantiate(labelTemp);
                timeLabel.transform.SetParent(PrefabEditorManager.inst.prefabSelectorLeft);
                timeLabel.transform.localScale = Vector3.one;
                timeLabel.name = "time label";
                timeLabel.transform.GetChild(0).GetComponent<Text>().text = "Time";
                Destroy(timeLabel.transform.GetChild(1).gameObject);

                var time = Instantiate(singleInput);
                time.transform.SetParent(PrefabEditorManager.inst.prefabSelectorLeft);
                time.transform.localScale = Vector3.one;
                time.name = "time";
            }

            // Position
            {
                var posLabel = Instantiate(labelTemp);
                posLabel.transform.SetParent(PrefabEditorManager.inst.prefabSelectorLeft);
                posLabel.transform.localScale = Vector3.one;
                posLabel.name = "pos label";
                posLabel.transform.GetChild(0).GetComponent<Text>().text = "Position X Offset";
                posLabel.transform.GetChild(1).GetComponent<Text>().text = "Position Y Offset";

                var pos = Instantiate(vector2Input);
                pos.transform.SetParent(PrefabEditorManager.inst.prefabSelectorLeft);
                pos.transform.localScale = Vector3.one;
                pos.name = "position";
            }

            // Scale
            {
                var scaLabel = Instantiate(labelTemp);
                scaLabel.transform.SetParent(PrefabEditorManager.inst.prefabSelectorLeft);
                scaLabel.transform.localScale = Vector3.one;
                scaLabel.name = "sca label";
                scaLabel.transform.GetChild(0).GetComponent<Text>().text = "Scale X Offset";
                scaLabel.transform.GetChild(1).GetComponent<Text>().text = "Scale Y Offset";

                var sca = Instantiate(vector2Input);
                sca.transform.SetParent(PrefabEditorManager.inst.prefabSelectorLeft);
                sca.transform.localScale = Vector3.one;
                sca.name = "scale";
            }

            // Rotation
            {
                var rotLabel = Instantiate(labelTemp);
                rotLabel.transform.SetParent(PrefabEditorManager.inst.prefabSelectorLeft);
                rotLabel.transform.localScale = Vector3.one;
                rotLabel.name = "rot label";
                rotLabel.transform.GetChild(0).GetComponent<Text>().text = "Rotation Offset";
                Destroy(rotLabel.transform.GetChild(1).gameObject);

                var rot = Instantiate(singleInput);
                rot.transform.SetParent(PrefabEditorManager.inst.prefabSelectorLeft);
                rot.transform.localScale = Vector3.one;
                rot.name = "rotation";
            }

            // Repeat Count
            {
                var rotLabel = Instantiate(labelTemp);
                rotLabel.transform.SetParent(PrefabEditorManager.inst.prefabSelectorLeft);
                rotLabel.transform.localScale = Vector3.one;
                rotLabel.name = "repeat count label";
                rotLabel.transform.GetChild(0).GetComponent<Text>().text = "Repeat Count";
                Destroy(rotLabel.transform.GetChild(1).gameObject);

                var rot = Instantiate(singleInput);
                rot.transform.SetParent(PrefabEditorManager.inst.prefabSelectorLeft);
                rot.transform.localScale = Vector3.one;
                rot.name = "repeat count";
            }

            // Repeat Offset Time
            {
                var rotLabel = Instantiate(labelTemp);
                rotLabel.transform.SetParent(PrefabEditorManager.inst.prefabSelectorLeft);
                rotLabel.transform.localScale = Vector3.one;
                rotLabel.name = "repeat offset time label";
                rotLabel.transform.GetChild(0).GetComponent<Text>().text = "Repeat Offset Time";
                Destroy(rotLabel.transform.GetChild(1).gameObject);

                var rot = Instantiate(singleInput);
                rot.transform.SetParent(PrefabEditorManager.inst.prefabSelectorLeft);
                rot.transform.localScale = Vector3.one;
                rot.name = "repeat offset time";
            }

            // Layers
            {
                var layers = Instantiate(singleInput);
                layers.transform.SetParent(PrefabEditorManager.inst.prefabSelectorLeft.Find("editor"));
                layers.transform.localScale = Vector3.one;
                layers.transform.SetSiblingIndex(0);
                layers.name = "layers";
            }

            // Name
            {
                var rotLabel = Instantiate(labelTemp);
                rotLabel.transform.SetParent(PrefabEditorManager.inst.prefabSelectorRight);
                rotLabel.transform.localScale = Vector3.one;
                rotLabel.name = "name label";
                rotLabel.transform.GetChild(0).GetComponent<Text>().text = "Name";
                Destroy(rotLabel.transform.GetChild(1).gameObject);

                var prefabName = Instantiate(RTEditor.defaultIF);
                prefabName.transform.SetParent(PrefabEditorManager.inst.prefabSelectorRight);
                prefabName.transform.localScale = Vector3.one;
                prefabName.name = "name";

                PrefabEditorManager.inst.nameIF = prefabName.GetComponent<InputField>();

                PrefabEditorManager.inst.nameIF.characterValidation = InputField.CharacterValidation.None;
                PrefabEditorManager.inst.nameIF.contentType = InputField.ContentType.Standard;
                PrefabEditorManager.inst.nameIF.characterLimit = 0;
            }

            // Type
            {
                var rotLabel = Instantiate(labelTemp);
                rotLabel.transform.SetParent(PrefabEditorManager.inst.prefabSelectorRight);
                rotLabel.transform.localScale = Vector3.one;
                rotLabel.name = "type label";
                rotLabel.transform.GetChild(0).GetComponent<Text>().text = "Type";
                Destroy(rotLabel.transform.GetChild(1).gameObject);

                var type = Instantiate(singleInput);
                type.transform.SetParent(PrefabEditorManager.inst.prefabSelectorRight);
                type.transform.localScale = Vector3.one;

                type.name = "type";

                PrefabEditorManager.inst.typeIF = type.GetComponent<InputField>();
                PrefabEditorManager.inst.typeImage = type.transform.GetChild(0).GetComponent<Image>();

                PrefabEditorManager.inst.typeIF.characterValidation = InputField.CharacterValidation.None;
                PrefabEditorManager.inst.typeIF.contentType = InputField.ContentType.Standard;
            }

            // Save Prefab
            {
                var label = Instantiate(PrefabEditorManager.inst.prefabSelectorLeft.GetChild(0).gameObject);
                label.transform.SetParent(PrefabEditorManager.inst.prefabSelectorRight);
                label.transform.localScale = Vector3.one;
                label.name = "save prefab label";
                label.transform.GetChild(0).GetComponent<Text>().text = "Apply all changes to external prefabs";

                var savePrefab = Instantiate(PrefabEditorManager.inst.prefabSelectorLeft.GetChild(1).gameObject);
                savePrefab.transform.SetParent(PrefabEditorManager.inst.prefabSelectorRight);
                savePrefab.transform.localScale = Vector3.one;
                savePrefab.name = "save prefab";
                savePrefab.transform.GetChild(0).GetComponent<Text>().text = "Save Prefab";
            }

            // Object Count
            {
                var rotLabel = Instantiate(labelTemp);
                rotLabel.transform.SetParent(PrefabEditorManager.inst.prefabSelectorRight);
                rotLabel.transform.localScale = Vector3.one;
                rotLabel.name = "count label";

                PrefabEditorManager.inst.objectCount = rotLabel.transform.GetChild(0).GetComponent<Text>();

                PrefabEditorManager.inst.objectCount.text = "Object Count: 0";
                Destroy(rotLabel.transform.GetChild(1).gameObject);
            }

            // Prefab Object Count
            {
                var rotLabel = Instantiate(labelTemp);
                rotLabel.transform.SetParent(PrefabEditorManager.inst.prefabSelectorRight);
                rotLabel.transform.localScale = Vector3.one;
                rotLabel.name = "count label";

                PrefabEditorManager.inst.prefabObjectCount = rotLabel.transform.GetChild(0).GetComponent<Text>();

                PrefabEditorManager.inst.prefabObjectCount.text = "Prefab Object Count: 0";
                Destroy(rotLabel.transform.GetChild(1).gameObject);
            }

            // Prefab Object Timeline Count
            {
                var rotLabel = Instantiate(labelTemp);
                rotLabel.transform.SetParent(PrefabEditorManager.inst.prefabSelectorRight);
                rotLabel.transform.localScale = Vector3.one;
                rotLabel.name = "count label";

                PrefabEditorManager.inst.prefabObjectTimelineCount = rotLabel.transform.GetChild(0).GetComponent<Text>();

                PrefabEditorManager.inst.prefabObjectTimelineCount.text = "Prefab Object (Timeline) Count: 0";
                Destroy(rotLabel.transform.GetChild(1).gameObject);
            }

            // Object Editor list

            return false;
        }

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static bool UpdatePrefix()
        {
            // Replace this with KeybindManager system.
            //if (InputDataManager.inst.editorActions.SpawnPrefab.WasPressed && !EditorManager.inst.IsUsingInputField() && EditorManager.inst.hasLoadedLevel)
            //{
            //    Instance.AddPrefabObjectToLevel(Instance.currentPrefab);
            //}
            if (EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Prefab) && EditorManager.inst.currentDialog.Name == "Prefab Editor")
            {
                float num;
                if (ObjectEditor.inst.SelectedBeatmapObjects.Count <= 0)
                    num = 0f;
                else
                    num = ObjectEditor.inst.SelectedBeatmapObjects.Min(x => x.Time);

                if (ObjectEditor.inst.SelectedPrefabObjects.Count <= 0)
                    num = 0f;
                else
                    num = ObjectEditor.inst.SelectedPrefabObjects.Min(x => x.Time);

                if (!Instance.OffsetLine.activeSelf && ObjectEditor.inst.SelectedBeatmapObjects.Count > 0)
                {
                    Instance.OffsetLine.transform.SetAsLastSibling();
                    Instance.OffsetLine.SetActive(true);
                }
                ((RectTransform)Instance.OffsetLine.transform).anchoredPosition = new Vector2(Instance.posCalc(num - Instance.NewPrefabOffset), 0f);
            }
            if (((!EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Prefab) && EditorManager.inst.currentDialog.Name != "Prefab Editor") || ObjectEditor.inst.SelectedBeatmapObjects.Count <= 0) && Instance.OffsetLine.activeSelf)
            {
                Instance.OffsetLine.SetActive(false);
            }
            return false;
        }

        static bool CreateNewPrefabPrefix()
        {
            PrefabEditorManager.inst.CreateNewPrefab();
            return false;
        }

        static bool SavePrefabPrefix(BasePrefab __0)
        {
            PrefabEditorManager.inst.SavePrefab(__0);
            return false;
        }

        [HarmonyPatch("ExpandCurrentPrefab")]
        [HarmonyPrefix]
        static bool ExpandCurrentPrefabPrefix()
        {
            PrefabEditorManager.inst.ExpandCurrentPrefab();
            return false;
        }

        [HarmonyPatch("ReloadExternalPrefabsInPopup")]
        [HarmonyPostfix]
        static void SetPopupSizesPostfix()
        {
            //Internal Config
            {
                var internalPrefab = PrefabEditor.inst.internalPrefabDialog.gameObject;

                internalPrefab.transform.Find("mask/content").GetComponentAndPerformAction(delegate (GridLayoutGroup x)
                {
                    x.spacing = ConfigEntries.PrefabINCellSpacing.Value;
                    x.cellSize = ConfigEntries.PrefabINCellSize.Value;
                    x.constraint = ConfigEntries.PrefabINConstraint.Value;
                    x.constraintCount = ConfigEntries.PrefabINConstraintColumns.Value;
                    x.startAxis = ConfigEntries.PrefabINAxis.Value;
                });

                internalPrefab.GetComponentAndPerformAction(delegate (RectTransform x)
                {
                    x.anchoredPosition = ConfigEntries.PrefabINANCH.Value;
                    x.sizeDelta = ConfigEntries.PrefabINSD.Value;
                });

                internalPrefab.GetComponent<ScrollRect>().horizontal = ConfigEntries.PrefabINHScroll.Value;
            }

            //External Config
            {
                var externalPrefab = PrefabEditor.inst.externalPrefabDialog.gameObject;

                externalPrefab.transform.Find("mask/content").GetComponentAndPerformAction(delegate (GridLayoutGroup x)
                {
                    x.spacing = ConfigEntries.PrefabEXCellSpacing.Value;
                    x.cellSize = ConfigEntries.PrefabEXCellSize.Value;
                    x.constraint = ConfigEntries.PrefabEXConstraint.Value;
                    x.constraintCount = ConfigEntries.PrefabEXConstraintColumns.Value;
                    x.startAxis = ConfigEntries.PrefabEXAxis.Value;

                });

                var exPMCGridLay = externalPrefab.transform.Find("mask/content").GetComponent<GridLayoutGroup>();

                externalPrefab.GetComponentAndPerformAction(delegate (RectTransform x)
                {
                    x.anchoredPosition = ConfigEntries.PrefabEXANCH.Value;
                    x.sizeDelta = ConfigEntries.PrefabEXSD.Value;
                });

                externalPrefab.GetComponent<ScrollRect>().horizontal = ConfigEntries.PrefabEXHScroll.Value;
            }
        }

        [HarmonyPatch("ReloadExternalPrefabsInPopup")]
        [HarmonyPrefix]
        static bool ReloadExternalPrefabsInPopupPatch(bool __0)
        {
            if (Instance.externalPrefabDialog == null || Instance.externalSearch == null || Instance.externalContent == null)
            {
                Debug.LogErrorFormat("External Prefabs Error: \n{0}\n{1}\n{2}", Instance.externalPrefabDialog, Instance.externalSearch, Instance.externalContent);
            }
            Debug.Log("Loading External Prefabs Popup");
            RTEditor.inst.StartCoroutine(PrefabEditorManager.inst.ExternalPrefabFiles(__0));
            return false;
        }

        [HarmonyPatch("ReloadInternalPrefabsInPopup")]
        [HarmonyPrefix]
        static bool ReloadInternalPrefabsInPopupPatch(bool __0)
        {
            if (Instance.internalPrefabDialog == null || Instance.internalSearch == null || Instance.internalContent == null)
            {
                Debug.LogErrorFormat("Internal Prefabs Error: \n{0}\n{1}\n{2}", Instance.internalPrefabDialog, Instance.internalSearch, Instance.internalContent);
            }
            Debug.Log("Loading Internal Prefabs Popup");
            RTEditor.inst.StartCoroutine(PrefabEditorManager.inst.InternalPrefabs(__0));
            return false;
        }

        [HarmonyPatch("LoadExternalPrefabs")]
        [HarmonyPrefix]
        static bool LoadExternalPrefabsPrefix(PrefabEditor __instance, ref IEnumerator __result)
        {
            __result = RTEditor.inst.LoadPrefabs(__instance);
            return false;
        }

        [HarmonyPatch("SavePrefab")]
        [HarmonyPrefix]
        static bool SavePrefab(PrefabEditor __instance, BasePrefab _prefab)
        {
            EditorManager.inst.DisplayNotification(string.Format("Saving Prefab to System [{0}]!", _prefab.Name), 2f, EditorManager.NotificationType.Warning, false);
            Debug.LogFormat("{0}Saving Prefab to File System!", EditorPlugin.className);
            __instance.LoadedPrefabs.Add(_prefab);
            __instance.LoadedPrefabsFiles.Add(RTFile.ApplicationDirectory + RTEditor.prefabListSlash + _prefab.Name.ToLower().Replace(" ", "_") + ".lsp");
            var jsonnode = DataManager.inst.GeneratePrefabJSON(_prefab);

            FileManager.inst.SaveJSONFile(RTEditor.prefabListPath, _prefab.Name.ToLower().Replace(" ", "_") + ".lsp", jsonnode.ToString());
            EditorManager.inst.DisplayNotification(string.Format("Saved prefab [{0}]!", _prefab.Name), 2f, EditorManager.NotificationType.Success);
            return false;
        }

        [HarmonyPatch("OpenPrefabDialog")]
        [HarmonyPrefix]
        static bool SetPrefabValues(PrefabEditor __instance)
        {
            #region Original Code

            if (ObjectEditor.inst.CurrentPrefabObjectSelection == null || ObjectEditor.inst.CurrentPrefabObjectSelection.Data == null)
            {
                EditorManager.inst.ClearDialogs(Array.Empty<EditorManager.EditorDialog.DialogType>());
                EditorManager.inst.ShowDialog("Object Editor", false);
                return false;
            }

            bool isPrefab = ObjectEditor.inst.CurrentPrefabObjectSelection != null && ObjectEditor.inst.CurrentPrefabObjectSelection.Data != null;
            EditorManager.inst.ClearDialogs(Array.Empty<EditorManager.EditorDialog.DialogType>());
            EditorManager.inst.ShowDialog("Prefab Selector");
            var right = EditorManager.inst.GetDialog("Prefab Selector").Dialog.Find("data/right");

            var timeOffset = right.Find("time/time").GetComponent<InputField>();
            timeOffset.onValueChanged.RemoveAllListeners();
            timeOffset.text = ObjectEditor.inst.CurrentPrefabObjectSelection.Data.GetPrefab().Offset.ToString();
            timeOffset.onValueChanged.AddListener(delegate (string _value)
            {
                if (isPrefab)
                {
                    string text;
                    float offset = DataManager.inst.ParseFloat(_value, out text);
                    EditorManager.inst.GetDialog("Prefab Selector").Dialog.Find("data/right/time/time").GetComponent<InputField>().text = text;

                    ObjectEditor.inst.CurrentPrefabObjectSelection.Data.GetPrefab().Offset = offset;
                    int num = 0;
                    foreach (var prefabObject in DataManager.inst.gameData.prefabObjects)
                    {
                        if (prefabObject.editorData.Layer == EditorManager.inst.layer && prefabObject.prefabID == ObjectEditor.inst.CurrentPrefabObjectSelection.Data.prefabID)
                        {
                            ObjectEditor.RenderTimelineObject(new TimelineObject<PrefabObject>((PrefabObject)prefabObject));
                            Updater.UpdatePrefab(prefabObject);
                        }
                        num++;
                    }
                }
                else
                {
                    EditorManager.inst.DisplayNotification("Can't edit non-prefab!", 2f, EditorManager.NotificationType.Error, false);
                }
            });

            TriggerHelper.IncreaseDecreaseButtons(timeOffset, t: right.transform.Find("time"));
            
            var timeTrigger = right.Find("time").GetComponent<EventTrigger>();
            timeTrigger.triggers.Clear();
            timeTrigger.triggers.Add(TriggerHelper.ScrollDelta(timeOffset, 0.1f, 10f));

            var prefabSelectorLeft = EditorManager.inst.GetDialog("Prefab Selector").Dialog.Find("data/left");

            prefabSelectorLeft.Find("editor/layer").gameObject.SetActive(false);
            prefabSelectorLeft.Find("editor/bin").gameObject.SetActive(false);
            prefabSelectorLeft.GetChild(2).GetChild(1).gameObject.SetActive(false);

            #endregion

            #region My Code

            if (isPrefab)
            {
                var currentPrefab = ObjectEditor.inst.CurrentPrefabObjectSelection.Data;
                var prefab = ObjectEditor.inst.CurrentPrefabObjectSelection.Data.GetPrefab();

                var time = prefabSelectorLeft.Find("time").GetComponent<InputField>();
                time.onValueChanged.RemoveAllListeners();
                time.text = currentPrefab.StartTime.ToString();
                time.onValueChanged.AddListener(delegate (string _val)
                {
                    if (float.TryParse(_val, out float n))
                    {
                        n = Mathf.Clamp(n, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                        currentPrefab.StartTime = n;
                        ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                        ObjEditor.inst.RenderTimelineObject(ObjEditor.inst.currentObjectSelection);
                    }
                    else
                    {
                        EditorManager.inst.DisplayNotification("Text is not correct format!", 1f, EditorManager.NotificationType.Error);
                    }
                });

                TriggerHelper.IncreaseDecreaseButtons(time);

                TriggerHelper.AddEventTrigger(time.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(time) });

                //Layer
                {
                    int currentLayer = currentPrefab.editorData.Layer;
                    var layer = prefabSelectorLeft.Find("editor/layers").GetComponent<InputField>();
                    layer.onValueChanged.RemoveAllListeners();
                    layer.text = (currentPrefab.editorData.Layer + 1).ToString();
                    layer.transform.GetChild(0).GetComponent<Image>().color = RTEditor.GetLayerColor(currentLayer);
                    layer.onValueChanged.AddListener(delegate (string _val)
                    {
                        if (int.TryParse(_val, out int n))
                        {
                            currentLayer = currentPrefab.editorData.Layer;
                            int a = n - 1;
                            if (a < 0)
                            {
                                layer.text = "1";
                            }

                            currentPrefab.editorData.Layer = RTEditor.GetLayer(a);
                            layer.transform.GetChild(0).GetComponent<Image>().color = RTEditor.GetLayerColor(RTEditor.GetLayer(a));
                            ObjEditor.inst.RenderTimelineObject(ObjEditor.inst.currentObjectSelection);
                        }
                        else
                        {
                            EditorManager.inst.DisplayNotification("Text is not correct format!", 1f, EditorManager.NotificationType.Error);
                        }
                    });

                    var layerLeft = layer.transform.Find("<").GetComponent<Button>();
                    var layerRight = layer.transform.Find(">").GetComponent<Button>();

                    layerLeft.onClick.RemoveAllListeners();
                    layerLeft.onClick.AddListener(delegate ()
                    {
                        layer.text = (currentPrefab.editorData.Layer - (int)ConfigEntries.EventMoveModify.Value).ToString();
                    });

                    layerRight.onClick.RemoveAllListeners();
                    layerRight.onClick.AddListener(delegate ()
                    {
                        layer.text = (currentPrefab.editorData.Layer + (int)ConfigEntries.EventMoveModify.Value).ToString();
                    });

                    if (!layer.gameObject.GetComponent<EventTrigger>())
                    {
                        layer.gameObject.AddComponent<EventTrigger>();
                    }

                    var layerET = layer.gameObject.GetComponent<EventTrigger>();

                    layerET.triggers.Clear();
                    layerET.triggers.Add(Triggers.ScrollDeltaInt(layer, 1, false, new List<int> { 1, int.MaxValue }));
                }

                for (int i = 0; i < 3; i++)
                {
                    string type = "";
                    string inx = "/x";
                    string iny = "/y";
                    switch (i)
                    {
                        case 0:
                            {
                                type = "position";
                                break;
                            }
                        case 1:
                            {
                                type = "scale";
                                break;
                            }
                        case 2:
                            {
                                type = "rotation";
                                inx = "";
                                break;
                            }
                    }

                    var currentKeyframe = currentPrefab.events[i];

                    //X
                    var prefabPosX = prefabSelectorLeft.Find(type + inx).GetComponent<InputField>();

                    prefabPosX.onValueChanged.RemoveAllListeners();
                    prefabPosX.text = currentKeyframe.eventValues[0].ToString("f2");
                    prefabPosX.onValueChanged.AddListener(delegate (string val)
                    {
                        float num = float.Parse(val);
                        currentKeyframe.eventValues[0] = num;
                        ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                    });

                    var posXLeft = prefabPosX.transform.Find("<").GetComponent<Button>();
                    var posXRight = prefabPosX.transform.Find(">").GetComponent<Button>();

                    posXLeft.onClick.RemoveAllListeners();
                    posXLeft.onClick.AddListener(delegate ()
                    {
                        prefabPosX.text = (currentKeyframe.eventValues[0] - ConfigEntries.EventMoveModify.Value).ToString();
                    });

                    posXRight.onClick.RemoveAllListeners();
                    posXRight.onClick.AddListener(delegate ()
                    {
                        prefabPosX.text = (currentKeyframe.eventValues[0] + ConfigEntries.EventMoveModify.Value).ToString();
                    });

                    if (!prefabPosX.gameObject.GetComponent<EventTrigger>())
                    {
                        prefabPosX.gameObject.AddComponent<EventTrigger>();
                    }

                    var posXET = prefabPosX.gameObject.GetComponent<EventTrigger>();

                    if (i != 2)
                    {
                        //Y
                        var prefabPosY = prefabSelectorLeft.Find(type + iny).GetComponent<InputField>();

                        prefabPosY.onValueChanged.RemoveAllListeners();
                        prefabPosY.text = currentKeyframe.eventValues[1].ToString("f2");
                        prefabPosY.onValueChanged.AddListener(delegate (string val)
                        {
                            float num = float.Parse(val);
                            currentKeyframe.eventValues[1] = num;
                            ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                        });

                        var posYLeft = prefabPosY.transform.Find("<").GetComponent<Button>();
                        var posYRight = prefabPosY.transform.Find(">").GetComponent<Button>();

                        posYLeft.onClick.RemoveAllListeners();
                        posYLeft.onClick.AddListener(delegate ()
                        {
                            prefabPosY.text = (currentKeyframe.eventValues[1] - ConfigEntries.EventMoveModify.Value).ToString();
                        });

                        posYRight.onClick.RemoveAllListeners();
                        posYRight.onClick.AddListener(delegate ()
                        {
                            prefabPosY.text = (currentKeyframe.eventValues[1] + ConfigEntries.EventMoveModify.Value).ToString();
                        });

                        if (!prefabPosY.gameObject.GetComponent<EventTrigger>())
                        {
                            prefabPosY.gameObject.AddComponent<EventTrigger>();
                        }

                        var posYET = prefabPosY.gameObject.GetComponent<EventTrigger>();

                        posXET.triggers.Clear();
                        posXET.triggers.Add(Triggers.ScrollDelta(prefabPosX, 0.1f, 10f, true));
                        posXET.triggers.Add(Triggers.ScrollDeltaVector2(prefabPosX, prefabPosY, 0.1f, 10f));

                        posYET.triggers.Clear();
                        posYET.triggers.Add(Triggers.ScrollDelta(prefabPosY, 0.1f, 10f, true));
                        posYET.triggers.Add(Triggers.ScrollDeltaVector2(prefabPosX, prefabPosY, 0.1f, 10f));
                    }
                    else
                    {
                        posXET.triggers.Clear();
                        posXET.triggers.Add(Triggers.ScrollDelta(prefabPosX, 15f, 3f));
                    }
                }

                var prefabCount = prefabSelectorLeft.Find("repeat count").GetComponent<InputField>();
                var prefabOffsetTime = prefabSelectorLeft.Find("repeat offset time").GetComponent<InputField>();

                prefabCount.onValueChanged.ClearAll();
                prefabCount.text = Mathf.Clamp(currentPrefab.RepeatCount, 0, 1000).ToString();
                prefabCount.onValueChanged.AddListener(delegate (string _val)
                {
                    if (int.TryParse(_val, out int num))
                    {
                        num = Mathf.Clamp(num, 0, 1000);
                        currentPrefab.RepeatCount = num;
                        ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                    }
                });

                prefabOffsetTime.onValueChanged.ClearAll();
                prefabOffsetTime.text = Mathf.Clamp(currentPrefab.RepeatOffsetTime, 0f, 60f).ToString();
                prefabOffsetTime.onValueChanged.AddListener(delegate (string _val)
                {
                    if (float.TryParse(_val, out float num))
                    {
                        num = Mathf.Clamp(num, 0f, 60f);
                        currentPrefab.RepeatOffsetTime = num;
                        ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                    }
                });

                Triggers.AddEventTrigger(prefabCount.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDeltaInt(prefabCount, 1, clamp: new List<int> { 0, 1000 }) });
                Triggers.AddEventTrigger(prefabOffsetTime.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(prefabOffsetTime, 0.1f, 10f, clamp: new List<float> { 0f, 60f }) });

                Triggers.IncreaseDecreaseButtonsInt(prefabCount, 1, clamp: new List<int> { 0, 1000 });
                Triggers.IncreaseDecreaseButtons(prefabOffsetTime, 1f, 10f, clamp: new List<float> { 0f, 60f });

                //Global Settings
                {
                    nameIF.onValueChanged.RemoveAllListeners();
                    nameIF.text = prefab.Name;
                    nameIF.onValueChanged.AddListener(delegate (string _val)
                    {
                        prefab.Name = _val;
                        foreach (var prefabObject in DataManager.inst.gameData.prefabObjects)
                        {
                            if (prefabObject.prefabID == prefab.ID)
                            {
                                var objectSelection = new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Prefab, prefabObject.ID);
                                ObjEditor.inst.RenderTimelineObject(objectSelection);
                            }
                        }
                    });

                    SearchPrefabType(DataManager.inst.PrefabTypes[prefab.Type].Name, prefab);
                    typeImage.color = DataManager.inst.PrefabTypes[prefab.Type].Color;
                    typeImage.transform.Find("Text").GetComponent<Text>().color = Triggers.InvertColorHue(Triggers.InvertColorValue(DataManager.inst.PrefabTypes[prefab.Type].Color));

                    currentPrefabType = prefab.Type;

                    var entry = new EventTrigger.Entry();
                    entry.eventID = EventTriggerType.Scroll;
                    entry.callback.AddListener(delegate (BaseEventData eventData)
                    {
                        PointerEventData pointerEventData = (PointerEventData)eventData;

                        if (pointerEventData.scrollDelta.y < 0f)
                        {
                            int num = Mathf.Clamp(prefab.Type - 1, 0, DataManager.inst.PrefabTypes.Count - 1);

                            prefab.Type = num;
                            SearchPrefabType(DataManager.inst.PrefabTypes[prefab.Type].Name, prefab);
                            typeImage.color = DataManager.inst.PrefabTypes[prefab.Type].Color;
                            currentPrefabType = num;
                            foreach (var prefabObject in DataManager.inst.gameData.prefabObjects)
                            {
                                if (prefabObject.prefabID == prefab.ID)
                                {
                                    var objectSelection = new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Prefab, prefabObject.ID);
                                    ObjEditor.inst.RenderTimelineObject(objectSelection);
                                }
                            }
                            return;
                        }
                        if (pointerEventData.scrollDelta.y > 0f)
                        {
                            int num = Mathf.Clamp(prefab.Type + 1, 0, DataManager.inst.PrefabTypes.Count - 1);

                            prefab.Type = num;
                            SearchPrefabType(DataManager.inst.PrefabTypes[prefab.Type].Name, prefab);
                            typeImage.color = DataManager.inst.PrefabTypes[num].Color;
                            currentPrefabType = num;
                            foreach (var prefabObject in DataManager.inst.gameData.prefabObjects)
                            {
                                if (prefabObject.prefabID == prefab.ID)
                                {
                                    var objectSelection = new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Prefab, prefabObject.ID);
                                    ObjEditor.inst.RenderTimelineObject(objectSelection);
                                }
                            }
                        }
                    });

                    Triggers.AddEventTrigger(typeIF.gameObject, new List<EventTrigger.Entry> { entry });

                    var leftButton = typeIF.transform.Find("<").GetComponent<Button>();
                    var rightButton = typeIF.transform.Find(">").GetComponent<Button>();

                    leftButton.onClick.ClearAll();
                    leftButton.onClick.AddListener(delegate ()
                    {
                        int num = Mathf.Clamp(prefab.Type - 1, 0, DataManager.inst.PrefabTypes.Count - 1);

                        prefab.Type = num;
                        SearchPrefabType(DataManager.inst.PrefabTypes[prefab.Type].Name, prefab);
                        typeImage.color = DataManager.inst.PrefabTypes[prefab.Type].Color;
                        typeImage.transform.Find("Text").GetComponent<Text>().color = Triggers.InvertColorHue(Triggers.InvertColorValue(DataManager.inst.PrefabTypes[prefab.Type].Color));
                        currentPrefabType = num;
                        foreach (var prefabObject in DataManager.inst.gameData.prefabObjects)
                        {
                            if (prefabObject.prefabID == prefab.ID)
                            {
                                var objectSelection = new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Prefab, prefabObject.ID);
                                ObjEditor.inst.RenderTimelineObject(objectSelection);
                            }
                        }
                    });

                    rightButton.onClick.ClearAll();
                    rightButton.onClick.AddListener(delegate ()
                    {
                        int num = Mathf.Clamp(prefab.Type + 1, 0, DataManager.inst.PrefabTypes.Count - 1);

                        prefab.Type = num;
                        SearchPrefabType(DataManager.inst.PrefabTypes[prefab.Type].Name, prefab);
                        typeImage.color = DataManager.inst.PrefabTypes[prefab.Type].Color;
                        typeImage.transform.Find("Text").GetComponent<Text>().color = Triggers.InvertColorHue(Triggers.InvertColorValue(DataManager.inst.PrefabTypes[prefab.Type].Color));
                        currentPrefabType = num;
                        foreach (var prefabObject in DataManager.inst.gameData.prefabObjects)
                        {
                            if (prefabObject.prefabID == prefab.ID)
                            {
                                var objectSelection = new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Prefab, prefabObject.ID);
                                ObjEditor.inst.RenderTimelineObject(objectSelection);
                            }
                        }
                    });

                    var savePrefab = prefabSelectorRight.Find("save prefab").GetComponent<Button>();
                    savePrefab.onClick.ClearAll();
                    savePrefab.onClick.AddListener(delegate ()
                    {
                        if (__instance.LoadedPrefabs.Find(x => x.Name == prefab.Name) != null)
                        {
                            var externalPrefab = __instance.LoadedPrefabs.Find(x => x.Name == prefab.Name);
                            var index = __instance.LoadedPrefabs.FindIndex(x => x.Name == prefab.Name);

                            Debug.LogFormat("{0}External Prefab: {1}", EditorPlugin.className, externalPrefab.Name);
                            Debug.LogFormat("{0}External Prefab Index: {1}", EditorPlugin.className, index);

                            if (index >= 0)
                            {
                                FileManager.inst.DeleteFileRaw(__instance.LoadedPrefabsFiles[index]);
                                Debug.LogFormat("{0}Deleted File", EditorPlugin.className);
                                __instance.LoadedPrefabs.RemoveAt(index);
                                Debug.LogFormat("{0}Removed Prefab", EditorPlugin.className);
                                __instance.LoadedPrefabsFiles.RemoveAt(index);
                                Debug.LogFormat("{0}Removed Prefab File", EditorPlugin.className);

                                __instance.SavePrefab(prefab);
                                Debug.LogFormat("{0}Saved Prefab", EditorPlugin.className);

                                if (externalContent != null)
                                    __instance.ReloadExternalPrefabsInPopup();

                                EditorManager.inst.DisplayNotification("Applied all changes to external prefab!", 2f, EditorManager.NotificationType.Success);
                            }
                        }
                        else
                        {
                            __instance.SavePrefab(prefab);
                            EditorManager.inst.DisplayNotification("External Prefab with same name does not exist!", 2f, EditorManager.NotificationType.Error);
                        }
                    });

                    objectCount.text = "Object Count: " + prefab.objects.Count.ToString();
                    prefabObjectCount.text = "Prefab Object Count: " + prefab.prefabObjects.Count;
                    prefabObjectTimelineCount.text = "Prefab Object (Timeline) Count: " + DataManager.inst.gameData.prefabObjects.FindAll(x => x.prefabID == prefab.ID).Count;
                }
            }
            if (!isPrefab)
            {
                EditorManager.inst.DisplayNotification("Cannot edit non-prefab!", 1f, EditorManager.NotificationType.Error);
            }

            #endregion
            return false;
        }

        static void SearchPrefabType(string t, BasePrefab prefab)
        {
            typeIF.onValueChanged.RemoveAllListeners();
            typeIF.text = t;
            typeIF.onValueChanged.AddListener(delegate (string _val)
            {
                if (DataManager.inst.PrefabTypes.Find(x => x.Name.ToLower() == _val.ToLower()) != null)
                {
                    prefab.Type = DataManager.inst.PrefabTypes.FindIndex(x => x.Name.ToLower() == _val.ToLower());
                    typeImage.color = DataManager.inst.PrefabTypes[prefab.Type].Color;
                    typeImage.transform.Find("Text").GetComponent<Text>().color = Triggers.InvertColorHue(Triggers.InvertColorValue(DataManager.inst.PrefabTypes[prefab.Type].Color));
                    currentPrefabType = prefab.Type;
                    foreach (var prefabObject in DataManager.inst.gameData.prefabObjects)
                    {
                        if (prefabObject.prefabID == prefab.ID)
                        {
                            var objectSelection = new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Prefab, prefabObject.ID);
                            ObjEditor.inst.RenderTimelineObject(objectSelection);
                        }
                    }
                }
            });
        }

        [HarmonyPatch("OpenDialog")]
        [HarmonyPostfix]
        static void PrefabLayout()
        {
            if (GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/PrefabDialog/data/type/types").GetComponent<VerticalLayoutGroup>())
            {
                Destroy(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/PrefabDialog/data/type/types").GetComponent<VerticalLayoutGroup>());
            }

            if (!GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/PrefabDialog/data/type/types").GetComponent<GridLayoutGroup>())
            {
                Debug.Log("Adding Prefab Grid Layout Component.");
                GridLayoutGroup prefabLay = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/PrefabDialog/data/type/types").AddComponent<GridLayoutGroup>();
                prefabLay.cellSize = new Vector2(280f, 30f);
                prefabLay.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                prefabLay.constraintCount = 2;
                prefabLay.spacing = new Vector2(8f, 8f);
                prefabLay.startAxis = GridLayoutGroup.Axis.Horizontal;
            }
        }

        [HarmonyPatch("ImportPrefabIntoLevel")]
        [HarmonyPrefix]
        static bool ImportPrefabIntoLevel(PrefabEditor __instance, DataManager.GameData.Prefab _prefab)
        {
            Debug.LogFormat("{0}Adding Prefab: [{1}]", EditorPlugin.className, _prefab.Name);

            var tmpPrefab = Prefab.DeepCopy((Prefab)_prefab);
            int num = DataManager.inst.gameData.prefabs.FindAll(x => Regex.Replace(x.Name, "( +\\[\\d+])", string.Empty) == tmpPrefab.Name).Count();
            if (num > 0)
                tmpPrefab.Name = $"{tmpPrefab.Name}[{num}]";

            DataManager.inst.gameData.prefabs.Add(tmpPrefab);
            __instance.ReloadInternalPrefabsInPopup();

            return false;
        }

        //[HarmonyPatch("AddPrefabObjectToLevel")]
        //[HarmonyPrefix]
        static bool AddPrefabObjectToLevel(DataManager.GameData.Prefab __0)
        {
            RTEditor.AddPrefabObjectToLevel(__0);
            return false;
        }
    }
}
