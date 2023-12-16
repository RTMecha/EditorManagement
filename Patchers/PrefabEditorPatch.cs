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
        static PrefabEditor Instance { get => PrefabEditor.inst; set => PrefabEditor.inst = value; }

        [HarmonyPatch("Awake")]
        [HarmonyPrefix]
        static bool AwakePrefix(PrefabEditor __instance)
        {
            if (Instance == null)
                Instance = __instance;
            else if (Instance != __instance)
                Destroy(__instance.gameObject);

            PrefabEditorManager.Init(__instance);

            return false;
        }

        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        static bool StartPrefix()
        {
            Instance.StartCoroutine(RTEditor.inst.LoadPrefabs(Instance));
            Instance.OffsetLine = Instance.OffsetLinePrefab.Duplicate(EditorManager.inst.timeline.transform, "offset line");

            //Debug.Log($"{Instance.className}Creating prefab types...");
            //var transform = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/PrefabDialog/data/type/types").transform;

            //var list = new List<GameObject>();
            //for (int i = 0; i < transform.childCount; i++)
            //{
            //    var tf = transform.Find($"col_{i}");
            //    if (tf)
            //        list.Add(tf.gameObject);
            //}

            //foreach (var go in list)
            //    Destroy(go);

            //Debug.Log($"{Instance.className}Duplicating...");
            //for (int i = 0; i < 20; i++)
            //    transform.GetChild(0).gameObject.Duplicate(transform, $"col_{i}", i + 1);

            Debug.Log($"{Instance.className}Setting dialogs...");
            Instance.dialog = EditorManager.inst.GetDialog("Prefab Editor").Dialog;
            Instance.externalPrefabDialog = EditorManager.inst.GetDialog("Prefab Popup").Dialog.Find("external prefabs");
            Instance.internalPrefabDialog = EditorManager.inst.GetDialog("Prefab Popup").Dialog.Find("internal prefabs");
            Instance.externalSearch = Instance.externalPrefabDialog.Find("search-box/search").GetComponent<InputField>();
            Instance.internalSearch = Instance.internalPrefabDialog.Find("search-box/search").GetComponent<InputField>();
            Instance.externalContent = Instance.externalPrefabDialog.Find("mask/content");
            Instance.internalContent = Instance.internalPrefabDialog.Find("mask/content");

            Debug.Log($"{Instance.className}Setting search...");
            Instance.gridSearch = Instance.dialog.Find("data/selection/search-box/search").GetComponent<InputField>();
            Instance.gridContent = Instance.dialog.Find("data/selection/mask/content");

            Debug.Log($"{Instance.className}Destroying VerticalLayoutGroup...");
            if (RTExtensions.TryFind("Editor Systems/Editor GUI/sizer/main/EditorDialogs/PrefabDialog/data/type/types", out GameObject gm) && gm.TryGetComponent(out VerticalLayoutGroup component))
            {
                Destroy(component);
            }

            Debug.Log($"{Instance.className}Setting left / right...");
            PrefabEditorManager.inst.prefabSelectorLeft = EditorManager.inst.GetDialog("Prefab Selector").Dialog.Find("data/left");
            PrefabEditorManager.inst.prefabSelectorRight = EditorManager.inst.GetDialog("Prefab Selector").Dialog.Find("data/right");
            
            var eventDialogTMP = EditorManager.inst.GetDialog("Event Editor").Dialog.Find("data/right");

            Debug.Log($"{Instance.className}Setting Inputs to reuse...");
            var singleInput = eventDialogTMP.Find("move/position/x").gameObject;
            var vector2Input = eventDialogTMP.Find("move/position").gameObject;
            var labelTemp = eventDialogTMP.Find("move").transform.GetChild(8).gameObject;

            Debug.Log($"{Instance.className}Deleting unneeded...");
            DestroyImmediate(PrefabEditorManager.inst.prefabSelectorLeft.GetChild(4).gameObject);
            DestroyImmediate(PrefabEditorManager.inst.prefabSelectorLeft.GetChild(4).gameObject);

            Debug.Log($"{Instance.className}Setting Generators...");
            Action<Transform, string, string> labelGenerator = delegate (Transform parent, string name, string x)
            {
                var label = labelTemp.Duplicate(parent, $"{name.ToLower()} label");
                label.transform.GetChild(0).GetComponent<Text>().text = x;
                Destroy(label.transform.GetChild(1).gameObject);
            };

            Action<Transform, string, string, string> labelGenerator2 = delegate (Transform parent, string name, string x, string y)
            {
                var label = labelTemp.Duplicate(parent, $"{name.ToLower()} label");
                label.transform.GetChild(0).GetComponent<Text>().text = x;
                label.transform.GetChild(1).GetComponent<Text>().text = y;
            };

            Debug.Log($"{Instance.className}Setting InputFields...");
            // Time
            labelGenerator(PrefabEditorManager.inst.prefabSelectorLeft, "time", "Time");

            singleInput.Duplicate(PrefabEditorManager.inst.prefabSelectorLeft, "time");

            // Position
            labelGenerator2(PrefabEditorManager.inst.prefabSelectorLeft, "pos", "Position X Offset", "Position Y Offset");

            vector2Input.Duplicate(PrefabEditorManager.inst.prefabSelectorLeft, "position");

            // Scale
            labelGenerator2(PrefabEditorManager.inst.prefabSelectorLeft, "sca", "Scale X Offset", "Scale Y Offset");

            vector2Input.Duplicate(PrefabEditorManager.inst.prefabSelectorLeft, "scale");

            // Rotation
            labelGenerator(PrefabEditorManager.inst.prefabSelectorLeft, "rot", "Rotation Offset");

            var rot = vector2Input.Duplicate(PrefabEditorManager.inst.prefabSelectorLeft, "rotation");
            Destroy(rot.transform.GetChild(1).gameObject);

            // Repeat Count
            labelGenerator(PrefabEditorManager.inst.prefabSelectorLeft, "repeat count", "Repeat Count");

            singleInput.Duplicate(PrefabEditorManager.inst.prefabSelectorLeft, "repeat count");

            // Repeat Offset Time
            labelGenerator(PrefabEditorManager.inst.prefabSelectorLeft, "repeat offset time", "Repeat Offset Time");

            singleInput.Duplicate(PrefabEditorManager.inst.prefabSelectorLeft, "repeat offset time");

            // Layers
            singleInput.Duplicate(PrefabEditorManager.inst.prefabSelectorLeft.Find("editor"), "layers", 0);

            // Name
            labelGenerator(PrefabEditorManager.inst.prefabSelectorRight, "name", "Name");

            var prefabName = RTEditor.inst.defaultIF.Duplicate(PrefabEditorManager.inst.prefabSelectorRight, "name");

            prefabName.GetComponentAndPerformAction(delegate (InputField inputField)
            {
                inputField.characterValidation = InputField.CharacterValidation.None;
                inputField.contentType = InputField.ContentType.Standard;
                inputField.characterLimit = 0;
                PrefabEditorManager.inst.nameIF = inputField;
            });

            // Type
            labelGenerator(PrefabEditorManager.inst.prefabSelectorRight, "type", "Type");

            var type = singleInput.Duplicate(PrefabEditorManager.inst.prefabSelectorRight, "type");

            PrefabEditorManager.inst.typeImage = type.transform.GetChild(0).GetComponent<Image>();
            type.GetComponentAndPerformAction(delegate (InputField inputField)
            {
                inputField.characterValidation = InputField.CharacterValidation.None;
                inputField.contentType = InputField.ContentType.Standard;
                PrefabEditorManager.inst.typeIF = inputField;
            });

            // Save Prefab
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

            Action<string, string, Action<Text, string>> countGenerator = delegate (string name, string count, Action<Text, string> text)
            {
                var rotLabel = labelTemp.Duplicate(PrefabEditorManager.inst.prefabSelectorRight, name);

                Destroy(rotLabel.transform.GetChild(1).gameObject);

                text(rotLabel.transform.GetChild(0).GetComponent<Text>(), count);
            };

            // Object Count
            countGenerator("count label", "Object Count: 0", delegate (Text text, string count)
            {
                PrefabEditorManager.inst.objectCount = text;
                PrefabEditorManager.inst.objectCount.text = count;
            });

            // Prefab Object Count
            countGenerator("count label", "Prefab Object Count: 0", delegate (Text text, string count)
            {
                PrefabEditorManager.inst.prefabObjectCount = text;
                PrefabEditorManager.inst.prefabObjectCount.text = count;
            });

            // Prefab Object Timeline Count
            countGenerator("count label", "Prefab Object (Timeline) Count: 0", delegate (Text text, string count)
            {
                PrefabEditorManager.inst.prefabObjectTimelineCount = text;
                PrefabEditorManager.inst.prefabObjectTimelineCount.text = count;
            });

            // Object Editor list

            var prefabEditorData = EditorManager.inst.GetDialog("Prefab Editor").Dialog.Find("data");

            var prefabType = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject/event")
                .Duplicate(prefabEditorData.Find("type"), "Show Type Editor");

            Destroy(prefabEditorData.Find("type/types").gameObject);

            ((RectTransform)prefabType.transform).sizeDelta = new Vector2(132f, 34f);
            prefabType.transform.Find("Text").GetComponent<Text>().text = "Open Prefab Type Editor";
            var prefabTypeButton = prefabType.GetComponent<Button>();
            prefabTypeButton.onClick.ClearAll();
            prefabTypeButton.onClick.AddListener(delegate ()
            {
                PrefabEditorManager.inst.OpenPrefabTypePopup();
            });

            ((RectTransform)prefabEditorData.Find("spacer")).sizeDelta = new Vector2(749f, 32f);
            ((RectTransform)prefabEditorData.Find("type")).sizeDelta = new Vector2(749f,  48f);

            var descriptionGO = prefabEditorData.Find("name").gameObject.Duplicate(prefabEditorData, "description", 4);
            ((RectTransform)descriptionGO.transform).sizeDelta = new Vector2(749f, 108f);
            descriptionGO.transform.Find("title").GetComponent<Text>().text = "Desc";

            EditorManager.inst.GetDialog("Prefab Editor").Dialog.Find("data/selection").gameObject.SetActive(true);
            ((RectTransform)EditorManager.inst.GetDialog("Prefab Editor").Dialog.Find("data/selection").transform).sizeDelta = new Vector2(749f, 300f);
            var search = EditorManager.inst.GetDialog("Prefab Editor").Dialog.Find("data/selection/search-box/search").GetComponent<InputField>();
            search.onValueChanged.ClearAll();
            search.onValueChanged.AddListener(delegate (string _val)
            {
                PrefabEditorManager.inst.ReloadSelectionContent();
            });
            var selectionGroup = EditorManager.inst.GetDialog("Prefab Editor").Dialog.Find("data/selection/mask/content").GetComponent<GridLayoutGroup>();
            selectionGroup.cellSize = new Vector2(172.5f, 32f);
            selectionGroup.constraintCount = 4;

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
                if (ObjectEditor.inst.SelectedObjects.Count <= 0)
                    num = 0f;
                else
                    num = ObjectEditor.inst.SelectedObjects.Min(x => x.Time);

                if (!Instance.OffsetLine.activeSelf && ObjectEditor.inst.SelectedObjects.Count > 0)
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

        [HarmonyPatch("CreateNewPrefab")]
        [HarmonyPrefix]
        static bool CreateNewPrefabPrefix()
        {
            PrefabEditorManager.inst.CreateNewPrefab();
            return false;
        }

        [HarmonyPatch("SavePrefab")]
        [HarmonyPrefix]
        static bool SavePrefabPrefix(BasePrefab __0)
        {
            PrefabEditorManager.inst.SavePrefab((Prefab)__0);
            return false;
        }

        [HarmonyPatch("DeleteExternalPrefab")]
        [HarmonyPrefix]
        static bool DeleteExternalPrefabPrefix(int __0)
        {
            PrefabEditorManager.inst.DeleteExternalPrefab(__0);
            return false;
        }
        
        [HarmonyPatch("DeleteInternalPrefab")]
        [HarmonyPrefix]
        static bool DeleteInternalPrefabPrefix(int __0)
        {
            PrefabEditorManager.inst.DeleteInternalPrefab(__0);
            return false;
        }
        
        [HarmonyPatch("ExpandCurrentPrefab")]
        [HarmonyPrefix]
        static bool ExpandCurrentPrefabPrefix()
        {
            PrefabEditorManager.inst.ExpandCurrentPrefab();
            return false;
        }
        
        [HarmonyPatch("CollapseCurrentPrefab")]
        [HarmonyPrefix]
        static bool CollapseCurrentPrefabPrefix()
        {
            PrefabEditorManager.inst.CollapseCurrentPrefab();
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
                    x.spacing = RTEditor.GetEditorProperty("Prefab Internal Spacing").GetConfigEntry<Vector2>().Value;
                    x.cellSize = RTEditor.GetEditorProperty("Prefab Internal Cell Size").GetConfigEntry<Vector2>().Value;
                    x.constraint = RTEditor.GetEditorProperty("Prefab Internal Constraint Mode").GetConfigEntry<GridLayoutGroup.Constraint>().Value;
                    x.constraintCount = RTEditor.GetEditorProperty("Prefab Internal Constraint").GetConfigEntry<int>().Value;
                    x.startAxis = RTEditor.GetEditorProperty("Prefab Internal Start Axis").GetConfigEntry<GridLayoutGroup.Axis>().Value;
                });

                internalPrefab.GetComponentAndPerformAction(delegate (RectTransform x)
                {
                    x.anchoredPosition = RTEditor.GetEditorProperty("Prefab Internal Popup Pos").GetConfigEntry<Vector2>().Value;
                    x.sizeDelta = RTEditor.GetEditorProperty("Prefab Internal Popup Size").GetConfigEntry<Vector2>().Value;
                });

                internalPrefab.GetComponent<ScrollRect>().horizontal = RTEditor.GetEditorProperty("Prefab Internal Horizontal Scroll").GetConfigEntry<bool>().Value;
            }

            //External Config
            {
                var externalPrefab = PrefabEditor.inst.externalPrefabDialog.gameObject;

                externalPrefab.transform.Find("mask/content").GetComponentAndPerformAction(delegate (GridLayoutGroup x)
                {
                    x.spacing = RTEditor.GetEditorProperty("Prefab External Spacing").GetConfigEntry<Vector2>().Value;
                    x.cellSize = RTEditor.GetEditorProperty("Prefab External Cell Size").GetConfigEntry<Vector2>().Value;
                    x.constraint = RTEditor.GetEditorProperty("Prefab External Constraint Mode").GetConfigEntry<GridLayoutGroup.Constraint>().Value;
                    x.constraintCount = RTEditor.GetEditorProperty("Prefab External Constraint").GetConfigEntry<int>().Value;
                    x.startAxis = RTEditor.GetEditorProperty("Prefab External Start Axis").GetConfigEntry<GridLayoutGroup.Axis>().Value;

                });

                var exPMCGridLay = externalPrefab.transform.Find("mask/content").GetComponent<GridLayoutGroup>();

                externalPrefab.GetComponentAndPerformAction(delegate (RectTransform x)
                {
                    x.anchoredPosition = RTEditor.GetEditorProperty("Prefab External Popup Pos").GetConfigEntry<Vector2>().Value;
                    x.sizeDelta = RTEditor.GetEditorProperty("Prefab External Popup Size").GetConfigEntry<Vector2>().Value;
                });

                externalPrefab.GetComponent<ScrollRect>().horizontal = RTEditor.GetEditorProperty("Prefab External Horizontal Scroll").GetConfigEntry<bool>().Value;
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

        [HarmonyPatch("OpenPrefabDialog")]
        [HarmonyPrefix]
        static bool OpenPrefabDialogPrefix(PrefabEditor __instance)
        {
            #region Original Code

            EditorManager.inst.ClearDialogs(Array.Empty<EditorManager.EditorDialog.DialogType>());

            bool isPrefab = ObjectEditor.inst.CurrentSelection != null && ObjectEditor.inst.CurrentSelection.Data != null && ObjectEditor.inst.CurrentSelection.IsPrefabObject;
            if (!isPrefab)
            {
                EditorManager.inst.ShowDialog("Object Editor", false);
                return false;
            }

            EditorManager.inst.ShowDialog("Prefab Selector");

            var currentPrefab = ObjectEditor.inst.CurrentSelection.GetData<PrefabObject>();
            var prefab = currentPrefab.GetPrefab();

            var right = EditorManager.inst.GetDialog("Prefab Selector").Dialog.Find("data/right");

            right.Find("time/time").GetComponentAndPerformAction(delegate (InputField inputField)
            {
                inputField.NewValueChangedListener(prefab.Offset.ToString(), delegate (string _val)
                {
                    if (isPrefab && float.TryParse(_val, out float offset))
                    {
                        prefab.Offset = offset;
                        int num = 0;
                        foreach (var prefabObject in DataManager.inst.gameData.prefabObjects)
                        {
                            if (prefabObject.editorData.layer == EditorManager.inst.layer && prefabObject.prefabID == currentPrefab.prefabID)
                            {
                                ObjectEditor.inst.RenderTimelineObject(new TimelineObject((PrefabObject)prefabObject));
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
                TriggerHelper.IncreaseDecreaseButtons(inputField, t: right.transform.Find("time"));
                TriggerHelper.AddEventTriggerParams(right.Find("time").gameObject, TriggerHelper.ScrollDelta(inputField));
            });

            var prefabSelectorLeft = EditorManager.inst.GetDialog("Prefab Selector").Dialog.Find("data/left");

            prefabSelectorLeft.Find("editor/layer").gameObject.SetActive(false);
            prefabSelectorLeft.Find("editor/bin").gameObject.SetActive(false);
            prefabSelectorLeft.GetChild(2).GetChild(1).gameObject.SetActive(false);

            #endregion

            #region My Code

            {
                prefabSelectorLeft.Find("time").GetComponentAndPerformAction(delegate (InputField inputField)
                {
                    inputField.NewValueChangedListener(currentPrefab.StartTime.ToString(), delegate (string _val)
                    {
                        if (float.TryParse(_val, out float n))
                        {
                            n = Mathf.Clamp(n, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                            currentPrefab.StartTime = n;
                            Updater.UpdatePrefab(currentPrefab);
                            ObjectEditor.inst.RenderTimelineObject(new TimelineObject(currentPrefab));
                        }
                        else
                            EditorManager.inst.DisplayNotification("Text is not correct format!", 1f, EditorManager.NotificationType.Error);
                    });

                    TriggerHelper.IncreaseDecreaseButtons(inputField);
                    TriggerHelper.AddEventTriggerParams(inputField.gameObject, TriggerHelper.ScrollDelta(inputField));
                });

                //Layer
                {
                    int currentLayer = currentPrefab.editorData.layer;

                    prefabSelectorLeft.Find("editor/layers").GetComponentAndPerformAction(delegate (InputField inputField)
                    {
                        inputField.transform.GetChild(0).GetComponent<Image>().color = RTEditor.GetLayerColor(currentPrefab.editorData.layer);
                        inputField.NewValueChangedListener((currentPrefab.editorData.layer + 1).ToString(), delegate (string _val)
                        {
                            if (int.TryParse(_val, out int n))
                            {
                                currentLayer = currentPrefab.editorData.layer;
                                int a = n - 1;
                                if (a < 0)
                                {
                                    inputField.text = "1";
                                }

                                currentPrefab.editorData.layer = RTEditor.GetLayer(a);
                                inputField.transform.GetChild(0).GetComponent<Image>().color = RTEditor.GetLayerColor(RTEditor.GetLayer(a));
                                ObjectEditor.inst.RenderTimelineObject(new TimelineObject(currentPrefab));
                            }
                            else
                                EditorManager.inst.DisplayNotification("Text is not correct format!", 1f, EditorManager.NotificationType.Error);
                        });

                        TriggerHelper.IncreaseDecreaseButtons(inputField);
                        TriggerHelper.AddEventTriggerParams(inputField.gameObject, TriggerHelper.ScrollDeltaInt(inputField, min: 1, max: int.MaxValue));
                    });
                }

                for (int i = 0; i < 3; i++)
                {
                    int index = i;

                    string[] types = new string[]
                    {
                        "position",
                        "scale",
                        "rotation"
                    };

                    string type = types[index];
                    string inx = "/x";
                    string iny = "/y";

                    var currentKeyframe = currentPrefab.events[index];

                    prefabSelectorLeft.Find(type + inx).GetComponentAndPerformAction(delegate (InputField inputField)
                    {
                        inputField.onValueChanged.ClearAll();
                        inputField.text = currentKeyframe.eventValues[0].ToString();
                        inputField.onValueChanged.AddListener(delegate (string _val)
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                currentKeyframe.eventValues[0] = num;
                                Updater.UpdatePrefab(currentPrefab);
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(inputField);

                        if (index != 2)
                        {
                            prefabSelectorLeft.Find(type + iny).GetComponentAndPerformAction(delegate (InputField inputField2)
                            {
                                inputField2.onValueChanged.ClearAll();
                                inputField2.text = currentKeyframe.eventValues[1].ToString();
                                inputField2.onValueChanged.AddListener(delegate (string _val)
                                {
                                    if (float.TryParse(_val, out float num))
                                    {
                                        currentKeyframe.eventValues[1] = num;
                                        Updater.UpdatePrefab(currentPrefab);
                                    }
                                });

                                TriggerHelper.IncreaseDecreaseButtons(inputField2);
                                TriggerHelper.AddEventTriggerParams(inputField2.gameObject,
                                    TriggerHelper.ScrollDelta(inputField2, multi: true),
                                    TriggerHelper.ScrollDeltaVector2(inputField, inputField2, 0.1f, 10f));

                                TriggerHelper.AddEventTriggerParams(inputField.gameObject,
                                    TriggerHelper.ScrollDelta(inputField, multi: true),
                                    TriggerHelper.ScrollDeltaVector2(inputField, inputField2, 0.1f, 10f));
                            });
                        }
                        else
                            TriggerHelper.AddEventTriggerParams(inputField.gameObject, TriggerHelper.ScrollDelta(inputField, 15f, 3f));
                    });
                }

                var prefabCount = prefabSelectorLeft.Find("repeat count").GetComponent<InputField>();
                var prefabOffsetTime = prefabSelectorLeft.Find("repeat offset time").GetComponent<InputField>();

                prefabSelectorLeft.Find("repeat count").GetComponentAndPerformAction(delegate (InputField inputField)
                {
                    inputField.NewValueChangedListener(Mathf.Clamp(currentPrefab.RepeatCount, 0, 1000).ToString(), delegate (string _val)
                    {
                        if (int.TryParse(_val, out int num))
                        {
                            num = Mathf.Clamp(num, 0, 1000);
                            currentPrefab.RepeatCount = num;
                            Updater.UpdatePrefab(currentPrefab);
                        }
                    });

                    TriggerHelper.IncreaseDecreaseButtonsInt(inputField, max: 1000);
                    TriggerHelper.AddEventTriggerParams(inputField.gameObject, TriggerHelper.ScrollDeltaInt(inputField, max: 1000));
                });
                
                prefabSelectorLeft.Find("repeat offset time").GetComponentAndPerformAction(delegate (InputField inputField)
                {
                    inputField.NewValueChangedListener(Mathf.Clamp(currentPrefab.RepeatOffsetTime, 0f, 60f).ToString(), delegate (string _val)
                    {
                        if (float.TryParse(_val, out float num))
                        {
                            num = Mathf.Clamp(num, 0f, 60f);
                            currentPrefab.RepeatOffsetTime = num;
                            Updater.UpdatePrefab(currentPrefab);
                        }
                    });

                    TriggerHelper.IncreaseDecreaseButtons(inputField, max: 60f);
                    TriggerHelper.AddEventTriggerParams(inputField.gameObject, TriggerHelper.ScrollDelta(inputField, max: 60f));
                });

                //Global Settings
                {
                    PrefabEditorManager.inst.nameIF.onValueChanged.RemoveAllListeners();
                    PrefabEditorManager.inst.nameIF.text = prefab.Name;
                    PrefabEditorManager.inst.nameIF.onValueChanged.AddListener(delegate (string _val)
                    {
                        prefab.Name = _val;
                        foreach (var prefabObject in DataManager.inst.gameData.prefabObjects)
                        {
                            if (prefabObject.prefabID == prefab.ID)
                            {
                                ObjectEditor.inst.RenderTimelineObject(new TimelineObject(prefabObject));
                            }
                        }
                    });

                    SearchPrefabType(DataManager.inst.PrefabTypes[prefab.Type].Name, prefab);
                    PrefabEditorManager.inst.typeImage.color = DataManager.inst.PrefabTypes[prefab.Type].Color;
                    PrefabEditorManager.inst.typeImage.transform.Find("Text").GetComponent<Text>().color = RTHelpers.InvertColorHue(RTHelpers.InvertColorValue(DataManager.inst.PrefabTypes[prefab.Type].Color));

                    PrefabEditorManager.inst.currentPrefabType = prefab.Type;

                    var entry = new EventTrigger.Entry();
                    entry.eventID = EventTriggerType.Scroll;
                    entry.callback.AddListener(delegate (BaseEventData eventData)
                    {
                        PointerEventData pointerEventData = (PointerEventData)eventData;

                        int add = pointerEventData.scrollDelta.y < 0f ? prefab.Type - 1 : pointerEventData.scrollDelta.y > 0f ? prefab.Type + 1 : 0;

                        int num = Mathf.Clamp(add, 0, DataManager.inst.PrefabTypes.Count - 1);

                        prefab.Type = num;
                        SearchPrefabType(DataManager.inst.PrefabTypes[prefab.Type].Name, prefab);
                        PrefabEditorManager.inst.typeImage.color = DataManager.inst.PrefabTypes[prefab.Type].Color;
                        PrefabEditorManager.inst.currentPrefabType = num;

                        foreach (var prefabObject in DataManager.inst.gameData.prefabObjects)
                        {
                            if (prefabObject.prefabID == prefab.ID)
                            {
                                ObjectEditor.inst.RenderTimelineObject(new TimelineObject(prefabObject));
                            }
                        }
                    });

                    TriggerHelper.AddEventTrigger(PrefabEditorManager.inst.typeIF.gameObject, new List<EventTrigger.Entry> { entry });

                    var leftButton = PrefabEditorManager.inst.typeIF.transform.Find("<").GetComponent<Button>();
                    var rightButton = PrefabEditorManager.inst.typeIF.transform.Find(">").GetComponent<Button>();

                    leftButton.onClick.ClearAll();
                    leftButton.onClick.AddListener(delegate ()
                    {
                        int num = Mathf.Clamp(prefab.Type - 1, 0, DataManager.inst.PrefabTypes.Count - 1);

                        prefab.Type = num;
                        SearchPrefabType(DataManager.inst.PrefabTypes[prefab.Type].Name, prefab);
                        PrefabEditorManager.inst.typeImage.color = DataManager.inst.PrefabTypes[prefab.Type].Color;
                        PrefabEditorManager.inst.typeImage.transform.Find("Text").GetComponent<Text>().color = RTHelpers.InvertColorHue(RTHelpers.InvertColorValue(DataManager.inst.PrefabTypes[prefab.Type].Color));
                        PrefabEditorManager.inst.currentPrefabType = num;

                        foreach (var prefabObject in DataManager.inst.gameData.prefabObjects)
                        {
                            if (prefabObject.prefabID == prefab.ID)
                            {
                                ObjectEditor.inst.RenderTimelineObject(new TimelineObject(prefabObject));
                            }
                        }
                    });

                    rightButton.onClick.ClearAll();
                    rightButton.onClick.AddListener(delegate ()
                    {
                        int num = Mathf.Clamp(prefab.Type + 1, 0, DataManager.inst.PrefabTypes.Count - 1);

                        prefab.Type = num;
                        SearchPrefabType(DataManager.inst.PrefabTypes[prefab.Type].Name, prefab);
                        PrefabEditorManager.inst.typeImage.color = DataManager.inst.PrefabTypes[prefab.Type].Color;
                        PrefabEditorManager.inst.typeImage.transform.Find("Text").GetComponent<Text>().color = RTHelpers.InvertColorHue(RTHelpers.InvertColorValue(DataManager.inst.PrefabTypes[prefab.Type].Color));
                        PrefabEditorManager.inst.currentPrefabType = num;
                        foreach (var prefabObject in DataManager.inst.gameData.prefabObjects)
                        {
                            if (prefabObject.prefabID == prefab.ID)
                            {
                                ObjectEditor.inst.RenderTimelineObject(new TimelineObject(prefabObject));
                            }
                        }
                    });

                    var savePrefab = PrefabEditorManager.inst.prefabSelectorRight.Find("save prefab").GetComponent<Button>();
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

                                if (__instance.externalContent != null)
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

                    PrefabEditorManager.inst.objectCount.text = "Object Count: " + prefab.objects.Count.ToString();
                    PrefabEditorManager.inst.prefabObjectCount.text = "Prefab Object Count: " + prefab.prefabObjects.Count;
                    PrefabEditorManager.inst.prefabObjectTimelineCount.text = "Prefab Object (Timeline) Count: " + DataManager.inst.gameData.prefabObjects.FindAll(x => x.prefabID == prefab.ID).Count;
                }
            }
            if (!isPrefab)
                EditorManager.inst.DisplayNotification("Cannot edit non-prefab!", 1f, EditorManager.NotificationType.Error);

            #endregion

            return false;
        }

        static void SearchPrefabType(string t, BasePrefab prefab)
        {
            PrefabEditorManager.inst.typeIF.onValueChanged.RemoveAllListeners();
            PrefabEditorManager.inst.typeIF.text = t;
            PrefabEditorManager.inst.typeIF.onValueChanged.AddListener(delegate (string _val)
            {
                if (DataManager.inst.PrefabTypes.Find(x => x.Name.ToLower() == _val.ToLower()) != null)
                {
                    prefab.Type = DataManager.inst.PrefabTypes.FindIndex(x => x.Name.ToLower() == _val.ToLower());
                    PrefabEditorManager.inst.typeImage.color = DataManager.inst.PrefabTypes[prefab.Type].Color;
                    PrefabEditorManager.inst.typeImage.transform.Find("Text").GetComponent<Text>().color = RTHelpers.InvertColorHue(RTHelpers.InvertColorValue(DataManager.inst.PrefabTypes[prefab.Type].Color));
                    PrefabEditorManager.inst.currentPrefabType = prefab.Type;
                    foreach (var prefabObject in DataManager.inst.gameData.prefabObjects)
                    {
                        if (prefabObject.prefabID == prefab.ID)
                        {
                            ObjectEditor.inst.RenderTimelineObject(new TimelineObject(prefabObject));
                        }
                    }
                }
            });
        }

        [HarmonyPatch("OpenDialog")]
        [HarmonyPrefix]
        static bool PrefabLayout()
        {
            PrefabEditorManager.inst.OpenDialog();

            return false;
        }

        [HarmonyPatch("ImportPrefabIntoLevel")]
        [HarmonyPrefix]
        static bool ImportPrefabIntoLevel(PrefabEditor __instance, BasePrefab __0)
        {
            Debug.LogFormat("{0}Adding Prefab: [{1}]", EditorPlugin.className, __0.Name);

            var tmpPrefab = Prefab.DeepCopy((Prefab)__0);
            int num = DataManager.inst.gameData.prefabs.FindAll(x => Regex.Replace(x.Name, "( +\\[\\d+])", string.Empty) == tmpPrefab.Name).Count();
            if (num > 0)
                tmpPrefab.Name = $"{tmpPrefab.Name}[{num}]";

            DataManager.inst.gameData.prefabs.Add(tmpPrefab);
            __instance.ReloadInternalPrefabsInPopup();

            return false;
        }

        [HarmonyPatch("AddPrefabObjectToLevel")]
        [HarmonyPrefix]
        static bool AddPrefabObjectToLevel(BasePrefab __0)
        {
            PrefabEditorManager.inst.AddPrefabObjectToLevel(__0);
            return false;
        }
    }
}
