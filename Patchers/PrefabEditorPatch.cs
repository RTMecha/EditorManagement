using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using SimpleJSON;
using LSFunctions;

using EditorManagement.Functions.Editors;

using RTFunctions.Functions;
using RTFunctions.Functions.Data;
using RTFunctions.Functions.Optimization;
using RTFunctions.Patchers;

using BasePrefab = DataManager.GameData.Prefab;
using BaseBeatmapObject = DataManager.GameData.BeatmapObject;
using BasePrefabObject = DataManager.GameData.PrefabObject;

namespace EditorManagement.Patchers
{
    public class PrefabEditorPatch : MonoBehaviour
    {
        static PrefabEditor Instance => PrefabEditor.inst;

        public static void Init()
        {

        }

        static void AwakePostfix()
        {

        }

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

        static bool CreateNewPrefab()
        {
            PrefabEditorManager.inst.CreateNewPrefab();
            return false;
        }

        static bool SavePrefab(BasePrefab __0)
        {
            PrefabEditorManager.inst.SavePrefab(__0);
            return false;
        }
    }
}
