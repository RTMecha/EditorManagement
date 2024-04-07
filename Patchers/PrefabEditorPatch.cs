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
using RTFunctions.Functions.Components;
using RTFunctions.Functions.Data;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Optimization;
using RTFunctions.Patchers;

using BasePrefab = DataManager.GameData.Prefab;
using BaseBeatmapObject = DataManager.GameData.BeatmapObject;
using BasePrefabObject = DataManager.GameData.PrefabObject;
using EditorManagement.Functions.Components;

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
            {
                Destroy(__instance.gameObject);
                return false;
            }

            Debug.Log($"{__instance.className}" +
                $"---------------------------------------------------------------------\n" +
                $"---------------------------- INITIALIZED ----------------------------\n" +
                $"---------------------------------------------------------------------\n");

            // Prefab Type Icon
            {
                var gameObject = PrefabEditor.inst.AddPrefab.Duplicate(__instance.transform, PrefabEditor.inst.AddPrefab.name);

                var type = gameObject.transform.Find("category");
                type.GetComponent<LayoutElement>().minWidth = 32f;

                var b = new GameObject("type");
                b.transform.SetParent(type);
                b.transform.localScale = Vector3.one;

                var bRT = b.AddComponent<RectTransform>();
                bRT.anchoredPosition = Vector2.zero;
                bRT.sizeDelta = new Vector2(28f, 28f);

                var bImage = b.AddComponent<Image>();
                bImage.color = new Color(0f, 0f, 0f, 0.45f);

                var icon = new GameObject("type");
                icon.transform.SetParent(bRT);
                icon.transform.localScale = Vector3.one;

                var iconRT = icon.AddComponent<RectTransform>();
                iconRT.anchoredPosition = Vector2.zero;
                iconRT.sizeDelta = new Vector2(28f, 28f);

                icon.AddComponent<Image>();

                PrefabEditor.inst.AddPrefab = gameObject;
            }

            PrefabEditorManager.Init(__instance);

            return false;
        }

        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        static bool StartPrefix()
        {
            PrefabEditorManager.loadingPrefabTypes = true;
            Instance.StartCoroutine(RTEditor.inst.LoadPrefabs(Instance));
            Instance.OffsetLine = Instance.OffsetLinePrefab.Duplicate(EditorManager.inst.timeline.transform, "offset line");
            Instance.OffsetLine.transform.AsRT().pivot = Vector2.one;

            Instance.dialog = EditorManager.inst.GetDialog("Prefab Editor").Dialog;
            Instance.externalPrefabDialog = EditorManager.inst.GetDialog("Prefab Popup").Dialog.Find("external prefabs");
            Instance.internalPrefabDialog = EditorManager.inst.GetDialog("Prefab Popup").Dialog.Find("internal prefabs");
            Instance.externalSearch = Instance.externalPrefabDialog.Find("search-box/search").GetComponent<InputField>();
            Instance.internalSearch = Instance.internalPrefabDialog.Find("search-box/search").GetComponent<InputField>();
            Instance.externalContent = Instance.externalPrefabDialog.Find("mask/content");
            Instance.internalContent = Instance.internalPrefabDialog.Find("mask/content");

            var externalSelectGUI = Instance.externalPrefabDialog.gameObject.AddComponent<SelectGUI>();
            var internalSelectGUI = Instance.internalPrefabDialog.gameObject.AddComponent<SelectGUI>();
            externalSelectGUI.ogPos = Instance.externalPrefabDialog.position;
            internalSelectGUI.ogPos = Instance.internalPrefabDialog.position;
            externalSelectGUI.target = Instance.externalPrefabDialog;
            internalSelectGUI.target = Instance.internalPrefabDialog;

            Instance.internalPrefabDialog.Find("Panel/Text").GetComponent<Text>().text = "Internal Prefabs";

            Instance.gridSearch = Instance.dialog.Find("data/selection/search-box/search").GetComponent<InputField>();
            Instance.gridContent = Instance.dialog.Find("data/selection/mask/content");

            if (RTExtensions.TryFind("Editor Systems/Editor GUI/sizer/main/EditorDialogs/PrefabDialog/data/type/types", out GameObject gm) && gm.TryGetComponent(out VerticalLayoutGroup component))
            {
                Destroy(component);
            }

            PrefabEditorManager.inst.prefabSelectorLeft = EditorManager.inst.GetDialog("Prefab Selector").Dialog.Find("data/left");
            PrefabEditorManager.inst.prefabSelectorRight = EditorManager.inst.GetDialog("Prefab Selector").Dialog.Find("data/right");
            
            var eventDialogTMP = EditorManager.inst.GetDialog("Event Editor").Dialog.Find("data/right");

            var singleInput = eventDialogTMP.Find("move/position/x").gameObject;
            var vector2Input = eventDialogTMP.Find("move/position").gameObject;
            var labelTemp = eventDialogTMP.Find("move").transform.GetChild(8).gameObject;

            DestroyImmediate(PrefabEditorManager.inst.prefabSelectorLeft.GetChild(4).gameObject);
            DestroyImmediate(PrefabEditorManager.inst.prefabSelectorLeft.GetChild(4).gameObject);

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

            // AutoKill
            labelGenerator(PrefabEditorManager.inst.prefabSelectorLeft, "tod-dropdown", "Time of Death");

            var autoKillType = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/autokill/tod-dropdown")
                .Duplicate(PrefabEditorManager.inst.prefabSelectorLeft, "tod-dropdown", 14);
            autoKillType.GetComponent<Dropdown>().options = new List<Dropdown.OptionData>
            {
                new Dropdown.OptionData("Regular"),
                new Dropdown.OptionData("Start Offset"),
                new Dropdown.OptionData("Song Time"),
            };
            autoKillType.GetComponent<HideDropdownOptions>().DisabledOptions = new List<bool>
            {
                false,
                false,
                false,
            };

            singleInput.Duplicate(PrefabEditorManager.inst.prefabSelectorLeft, "akoffset");

            var setToCurrent = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/autokill/|").Duplicate(PrefabEditorManager.inst.prefabSelectorLeft.Find("akoffset"), "|");

            // Time
            labelGenerator(PrefabEditorManager.inst.prefabSelectorLeft, "time", "Time");

            singleInput.Duplicate(PrefabEditorManager.inst.prefabSelectorLeft, "time");

            var timeParent = PrefabEditorManager.inst.prefabSelectorLeft.Find("time");

            var locker = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/toggle/toggle").Duplicate(timeParent, "lock", 0);

            locker.transform.Find("Background/Checkmark").GetComponent<Image>().sprite = ObjEditor.inst.timelineObjectPrefabLock.transform.Find("lock (1)").GetComponent<Image>().sprite;

            var collapser = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/autokill/collapse").Duplicate(timeParent, "collapse", 1);

            // Position
            labelGenerator2(PrefabEditorManager.inst.prefabSelectorLeft, "pos", "Position X Offset", "Position Y Offset");

            var position = vector2Input.Duplicate(PrefabEditorManager.inst.prefabSelectorLeft, "position");
            var positionX = position.transform.Find("x").gameObject.AddComponent<InputFieldSwapper>();
            positionX.Init(position.transform.Find("x").GetComponent<InputField>());
            var positionY = position.transform.Find("y").gameObject.AddComponent<InputFieldSwapper>();
            positionY.Init(position.transform.Find("y").GetComponent<InputField>());

            // Scale
            labelGenerator2(PrefabEditorManager.inst.prefabSelectorLeft, "sca", "Scale X Offset", "Scale Y Offset");

            var scale = vector2Input.Duplicate(PrefabEditorManager.inst.prefabSelectorLeft, "scale");
            var scaleX = scale.transform.Find("x").gameObject.AddComponent<InputFieldSwapper>();
            scaleX.Init(scale.transform.Find("x").GetComponent<InputField>());
            var scaleY = scale.transform.Find("y").gameObject.AddComponent<InputFieldSwapper>();
            scaleY.Init(scale.transform.Find("y").GetComponent<InputField>());

            // Rotation
            labelGenerator(PrefabEditorManager.inst.prefabSelectorLeft, "rot", "Rotation Offset");

            var rot = vector2Input.Duplicate(PrefabEditorManager.inst.prefabSelectorLeft, "rotation");
            Destroy(rot.transform.GetChild(1).gameObject);
            var rotX = rot.transform.Find("x").gameObject.AddComponent<InputFieldSwapper>();
            rotX.Init(rot.transform.Find("x").GetComponent<InputField>());

            // Repeat
            labelGenerator2(PrefabEditorManager.inst.prefabSelectorLeft, "repeat", "Repeat Count", "Repeat Offset Time");

            vector2Input.Duplicate(PrefabEditorManager.inst.prefabSelectorLeft, "repeat");

            // Speed
            labelGenerator(PrefabEditorManager.inst.prefabSelectorLeft, "speed", "Speed");

            singleInput.Duplicate(PrefabEditorManager.inst.prefabSelectorLeft, "speed");

            // Layers
            singleInput.Duplicate(PrefabEditorManager.inst.prefabSelectorLeft.Find("editor"), "layers", 0);

            // Name
            labelGenerator(PrefabEditorManager.inst.prefabSelectorRight, "name", "Name");

            var prefabName = RTEditor.inst.defaultIF.Duplicate(PrefabEditorManager.inst.prefabSelectorRight, "name");
            prefabName.transform.localScale = Vector3.one;

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
            var applyToAllText = label.transform.GetChild(0).GetComponent<Text>();
            applyToAllText.fontSize = 19;
            applyToAllText.text = "Apply to an External Prefab";

            var savePrefab = Instantiate(PrefabEditorManager.inst.prefabSelectorLeft.GetChild(1).gameObject);
            savePrefab.transform.SetParent(PrefabEditorManager.inst.prefabSelectorRight);
            savePrefab.transform.localScale = Vector3.one;
            savePrefab.name = "save prefab";
            savePrefab.transform.GetChild(0).GetComponent<Text>().text = "Select Prefab";

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
                PrefabEditorManager.inst.OpenPrefabTypePopup(PrefabEditor.inst.NewPrefabType, delegate(int index)
                {
                    PrefabEditor.inst.NewPrefabType = index;
                    if (PrefabEditor.inst.dialog)
                        PrefabEditor.inst.dialog.Find("data/type/Show Type Editor").GetComponent<Image>().color =
                            DataManager.inst.PrefabTypes[Mathf.Clamp(PrefabEditor.inst.NewPrefabType, 0, DataManager.inst.PrefabTypes.Count - 1)].Color;
                });
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
            if (Instance.dialog && Instance.dialog.gameObject.activeSelf)
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
            if (((!Instance.dialog || !Instance.dialog.gameObject.activeSelf) || ObjectEditor.inst.SelectedBeatmapObjects.Count <= 0) && Instance.OffsetLine.activeSelf)
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
                var internalPrefab = PrefabEditor.inst.internalPrefabDialog;

                internalPrefab.Find("mask/content").GetComponentAndPerformAction(delegate (GridLayoutGroup x)
                {
                    x.spacing = EditorConfig.Instance.PrefabInternalSpacing.Value;
                    x.cellSize = EditorConfig.Instance.PrefabInternalCellSize.Value;
                    x.constraint = EditorConfig.Instance.PrefabInternalConstraintMode.Value;
                    x.constraintCount = EditorConfig.Instance.PrefabInternalConstraint.Value;
                    x.startAxis = EditorConfig.Instance.PrefabInternalStartAxis.Value;
                });

                internalPrefab.AsRT().anchoredPosition = EditorConfig.Instance.PrefabInternalPopupPos.Value;
                internalPrefab.AsRT().sizeDelta = EditorConfig.Instance.PrefabInternalPopupSize.Value;

                internalPrefab.GetComponent<ScrollRect>().horizontal = EditorConfig.Instance.PrefabInternalHorizontalScroll.Value;
            }

            //External Config
            {
                var externalPrefab = PrefabEditor.inst.externalPrefabDialog;

                externalPrefab.Find("mask/content").GetComponentAndPerformAction(delegate (GridLayoutGroup x)
                {
                    x.spacing = EditorConfig.Instance.PrefabExternalSpacing.Value;
                    x.cellSize = EditorConfig.Instance.PrefabExternalCellSize.Value;
                    x.constraint = EditorConfig.Instance.PrefabExternalConstraintMode.Value;
                    x.constraintCount = EditorConfig.Instance.PrefabExternalConstraint.Value;
                    x.startAxis = EditorConfig.Instance.PrefabExternalStartAxis.Value;
                });

                externalPrefab.AsRT().anchoredPosition = EditorConfig.Instance.PrefabExternalPopupPos.Value;
                externalPrefab.AsRT().sizeDelta = EditorConfig.Instance.PrefabExternalPopupSize.Value;

                externalPrefab.GetComponent<ScrollRect>().horizontal = EditorConfig.Instance.PrefabExternalHorizontalScroll.Value;
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
            EditorManager.inst.ClearDialogs();

            bool isPrefab = ObjectEditor.inst.CurrentSelection != null && ObjectEditor.inst.CurrentSelection.Data != null && ObjectEditor.inst.CurrentSelection.IsPrefabObject;
            if (!isPrefab)
            {
                Debug.LogError($"{__instance.className}Cannot select non-Prefab with this editor!");
                EditorManager.inst.ShowDialog("Object Editor", false);
                return false;
            }

            EditorManager.inst.ShowDialog("Prefab Selector");
            PrefabEditorManager.inst.RenderPrefabObjectDialog(ObjectEditor.inst.CurrentSelection.GetData<PrefabObject>(), __instance);

            return false;
        }

        [HarmonyPatch("OpenDialog")]
        [HarmonyPrefix]
        static bool OpenDialogPrefix()
        {
            PrefabEditorManager.inst.OpenDialog();

            return false;
        }
        
        [HarmonyPatch("OpenPopup")]
        [HarmonyPrefix]
        static bool OpenPopupPrefix()
        {
            PrefabEditorManager.inst.OpenPopup();

            return false;
        }

        [HarmonyPatch("ImportPrefabIntoLevel")]
        [HarmonyPrefix]
        static bool ImportPrefabIntoLevelPrefix(PrefabEditor __instance, BasePrefab __0)
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
