﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using SimpleJSON;
using LSFunctions;

using EditorManagement.Functions.Components;
using EditorManagement.Functions.Helpers;
using EditorManagement.Functions.Editors;

using RTFunctions.Functions;
using RTFunctions.Functions.Components;
using RTFunctions.Functions.Data;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Optimization;
using RTFunctions.Patchers;

using BasePrefab = DataManager.GameData.Prefab;
using BaseBeatmapObject = DataManager.GameData.BeatmapObject;
using BasePrefabObject = DataManager.GameData.PrefabObject;


namespace EditorManagement.Functions.Editors
{
    public class PrefabEditorManager : MonoBehaviour
    {
        public static PrefabEditorManager inst;

        #region Variables

        public string externalSearchStr;
        public string internalSearchStr;

        public Transform prefabSelectorRight;
        public Transform prefabSelectorLeft;

        public int currentPrefabType = 0;
        public InputField typeIF;
        public Image typeImage;
        public InputField nameIF;
        public Text objectCount;
        public Text prefabObjectCount;
        public Text prefabObjectTimelineCount;

        public bool createInternal;

        public bool selectingPrefab;

        public int currentTypeSelection;

        public GameObject prefabTypePrefab;
        public GameObject prefabTypeTogglePrefab;
        public Transform prefabTypeContent;

        public string NewPrefabDescription { get; set; }

        public List<PrefabPanel> PrefabPanels { get; set; } = new List<PrefabPanel>();

        public static bool ImportPrefabsDirectly { get; set; }

        #endregion

        public static void Init(PrefabEditor prefabEditor) => prefabEditor?.gameObject?.AddComponent<PrefabEditorManager>();

        void Awake()
        {
            inst = this;

        }

        void Start()
        {
            var transform = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/PrefabDialog/data/type/types").transform;

            var list = new List<GameObject>();
            for (int i = 1; i < transform.childCount; i++)
            {
                var tf = transform.Find($"col_{i}");
                if (tf)
                    list.Add(tf.gameObject);
            }

            foreach (var go in list)
                Destroy(go);

            prefabTypeTogglePrefab = transform.GetChild(0).gameObject;
            prefabTypeTogglePrefab.transform.SetParent(null);

            CreatePrefabTypesPopup();
            CreatePrefabExternalDialog();

            if (RTFile.DirectoryExists(RTFile.ApplicationDirectory + "beatmaps/prefabtypes/"))
                StartCoroutine(LoadPrefabTypes());

        }

        public void CreatePrefabTypesPopup()
        {
            var parent = EditorManager.inst.GetDialog("Quick Actions Popup").Dialog.parent;
            var gameObject = new GameObject("Prefab Types Popup");
            gameObject.transform.SetParent(parent);
            gameObject.transform.localScale = Vector3.one;

            var baseRT = gameObject.AddComponent<RectTransform>();
            var baseImage = gameObject.AddComponent<Image>();
            baseImage.color = new Color(0.12f, 0.12f, 0.12f);
            var baseSelectGUI = gameObject.AddComponent<SelectGUI>();

            baseRT.anchoredPosition = new Vector2(356f, 0f);
            baseRT.sizeDelta = new Vector2(400f, 600f);

            baseSelectGUI.target = baseRT;
            baseSelectGUI.OverrideDrag = true;

            var panel = EditorManager.inst.GetDialog("Save As Popup").Dialog.Find("New File Popup/Panel").gameObject.Duplicate(baseRT, "Panel");
            var panelRT = (RectTransform)panel.transform;
            panelRT.anchoredPosition = Vector2.zero;
            panelRT.sizeDelta = new Vector2(0f, 32f);

            panel.transform.Find("Text").GetComponent<Text>().text = "Prefab Type Editor / Selector";
            var closeButton = panel.transform.Find("x").GetComponent<Button>();
            closeButton.onClick.ClearAll();
            closeButton.onClick.AddListener(delegate ()
            {
                EditorManager.inst.HideDialog("Prefab Types Popup");
            });

            var scrollRect = new GameObject("ScrollRect");
            scrollRect.transform.SetParent(baseRT);
            scrollRect.transform.localScale = Vector3.one;
            var scrollRectRT = scrollRect.AddComponent<RectTransform>();
            scrollRectRT.anchoredPosition = new Vector2(0f, 0f);
            scrollRectRT.sizeDelta = new Vector2(400f, 600f);
            var scrollRectSR = scrollRect.AddComponent<ScrollRect>();

            var mask = new GameObject("Mask");
            mask.transform.SetParent(scrollRectRT);
            mask.transform.localScale = Vector3.one;
            var maskRT = mask.AddComponent<RectTransform>();
            maskRT.anchoredPosition = new Vector2(0f, 0f);
            maskRT.anchorMax = new Vector2(1f, 1f);
            maskRT.anchorMin = new Vector2(0f, 0f);
            maskRT.sizeDelta = new Vector2(0f, 0f);

            var maskImage = mask.AddComponent<Image>();
            var maskMask = mask.AddComponent<Mask>();
            maskMask.showMaskGraphic = false;

            var content = new GameObject("Content");
            content.transform.SetParent(maskRT);
            content.transform.localScale = Vector3.one;

            var contentRT = content.AddComponent<RectTransform>();
            contentRT.anchoredPosition = new Vector2(0f, -16f);
            contentRT.anchorMax = new Vector2(0f, 1f);
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.pivot = new Vector2(0f, 1f);
            contentRT.sizeDelta = new Vector2(400f, 104f);

            prefabTypeContent = contentRT;

            var contentSizeFitter = content.AddComponent<ContentSizeFitter>();
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.MinSize;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;

            var contentVLG = content.AddComponent<VerticalLayoutGroup>();
            contentVLG.childControlHeight = false;
            contentVLG.childForceExpandHeight = false;
            contentVLG.spacing = 4f;

            scrollRectSR.content = contentRT;

            // Prefab Type Prefab
            try
            {
                prefabTypePrefab = new GameObject("Prefab Type");
                prefabTypePrefab.transform.localScale = Vector3.one;
                var rectTransform = prefabTypePrefab.AddComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(400f, 32f);
                var image = prefabTypePrefab.AddComponent<Image>();
                image.color = new Color(0.2f, 0.2f, 0.2f);

                var horizontalLayoutGroup = prefabTypePrefab.AddComponent<HorizontalLayoutGroup>();
                horizontalLayoutGroup.childControlWidth = false;
                horizontalLayoutGroup.childForceExpandWidth = false;
                horizontalLayoutGroup.spacing = 4;

                var toggleType = prefabTypeTogglePrefab.Duplicate(rectTransform, "Toggle");
                toggleType.transform.localScale = Vector3.one;
                var toggleTypeRT = (RectTransform)toggleType.transform;
                toggleTypeRT.sizeDelta = new Vector2(32f, 32f);
                Destroy(toggleTypeRT.Find("text").gameObject);
                toggleTypeRT.Find("Background/Checkmark").GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);

                var toggleTog = toggleType.GetComponent<Toggle>();
                toggleTog.enabled = true;
                toggleTog.group = null;

                var icon = new GameObject("Icon");
                icon.transform.localScale = Vector3.one;
                icon.transform.SetParent(toggleTypeRT);
                icon.transform.localScale = Vector3.one;
                var iconRT = icon.AddComponent<RectTransform>();
                iconRT.anchoredPosition = Vector2.zero;
                iconRT.sizeDelta = new Vector2(32f, 32f);

                var iconImage = icon.AddComponent<Image>();

                //var spacer = new GameObject("Spacer");
                //spacer.transform.SetParent(rectTransform);
                //spacer.transform.localScale = Vector3.one;
                //var spacerRT = spacer.AddComponent<RectTransform>();
                //spacerRT.sizeDelta = new Vector2(32f, 32f);

                var nameGO = RTEditor.inst.defaultIF.Duplicate(rectTransform, "Name");
                nameGO.transform.localScale = Vector3.one;
                var nameRT = nameGO.GetComponent<RectTransform>();
                nameRT.sizeDelta = new Vector2(163f, 32f);

                var nameTextRT = (RectTransform)nameRT.Find("Text");
                nameTextRT.anchoredPosition = new Vector2(0f, 0f);
                nameTextRT.sizeDelta = new Vector2(0f, 0f);

                nameTextRT.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;

                var colorGO = RTEditor.inst.defaultIF.Duplicate(rectTransform, "Color");
                colorGO.transform.localScale = Vector3.one;
                var colorRT = colorGO.GetComponent<RectTransform>();
                colorRT.sizeDelta = new Vector2(163f, 32f);

                var colorTextRT = (RectTransform)colorRT.Find("Text");
                colorTextRT.anchoredPosition = new Vector2(0f, 0f);
                colorTextRT.sizeDelta = new Vector2(0f, 0f);

                colorTextRT.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;

                var delete = EditorManager.inst.GetDialog("Quick Actions Popup").Dialog.Find("Panel/x").gameObject.Duplicate(rectTransform, "Delete");
                delete.transform.localScale = Vector3.one;
                ((RectTransform)delete.transform).anchoredPosition = Vector2.zero;
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }

            EditorHelper.AddEditorPopup("Prefab Types Popup", gameObject);
        }

        public void SavePrefabTypes()
        {
            foreach (var prefabType in DataManager.inst.PrefabTypes.Select(x => x as PrefabType))
            {
                var jn = prefabType.ToJSON();
                var directory = RTFile.ApplicationDirectory + "beatmaps/prefabtypes/" + prefabType.Name;
                if (!RTFile.DirectoryExists(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllBytes(directory + "/icon.png", prefabType.Icon.texture.EncodeToPNG());
                RTFile.WriteToFile(directory + "/data.lsp", jn.ToString(3));
            }
        }

        public IEnumerator LoadPrefabTypes()
        {
            DataManager.inst.PrefabTypes.Clear();

            var directories = Directory.GetDirectories(RTFile.ApplicationDirectory + "beatmaps/prefabtypes");
            var list = new List<DataManager.PrefabType>();
            foreach (var folder in directories)
            {
                var fileName = Path.GetFileName(folder);
                var jn = JSON.Parse(RTFile.ReadFromFile(folder + "/data.lsp"));
                var prefabType = PrefabType.Parse(jn);

                StartCoroutine(EditorManager.inst.GetSprite(folder + "/icon.png", new EditorManager.SpriteLimits(), delegate (Sprite sprite)
                {
                    prefabType.Icon = sprite;
                }, delegate (string onError) { }));

                prefabType.Index = jn["index"].AsInt;

                list.Add(prefabType);
            }

            list = list.OrderBy(x => (x as PrefabType).Index).ToList();

            DataManager.inst.PrefabTypes.AddRange(list);

            yield break;
        }

        public void OpenPrefabTypePopup(Action<int> onSelect)
        {
            EditorManager.inst.ShowDialog("Prefab Types Popup");
            RenderPrefabTypesPopup(onSelect);
        }

        public void ReorderPrefabTypes()
        {
            int num = 0;
            foreach (var prefabType in DataManager.inst.PrefabTypes.Select(x => x as PrefabType))
            {
                prefabType.Index = num;
                num++;
            }
        }

        public void RenderPrefabTypesPopup(Action<int> onSelect)
        {
            LSHelpers.DeleteChildren(prefabTypeContent);

            var createPrefabType = PrefabEditor.inst.CreatePrefab.Duplicate(prefabTypeContent, "Create Prefab Type");
            ((RectTransform)createPrefabType.transform).sizeDelta = new Vector2(402f, 32f);
            createPrefabType.transform.Find("Text").GetComponent<Text>().text = "Create New Prefab Type";
            var createPrefabTypeButton = createPrefabType.GetComponent<Button>();
            createPrefabTypeButton.onClick.ClearAll();
            createPrefabTypeButton.onClick.AddListener(delegate ()
            {
                string name = "New Type";
                int n = 0;
                while (DataManager.inst.PrefabTypes.Has(x => x.Name == name))
                {
                    name = $"New Type [{n}]";
                    n++;
                }

                var prefabType = new PrefabType(name, LSColors.pink500);
                prefabType.Index = DataManager.inst.PrefabTypes.Count;
                prefabType.Icon = ((PrefabType)DataManager.inst.PrefabTypes[prefabType.Index - 1]).Icon;

                DataManager.inst.PrefabTypes.Add(prefabType);

                ReorderPrefabTypes();

                SavePrefabTypes();

                RenderPrefabTypesPopup(onSelect);
            });

            int num = 0;
            foreach (var prefabType in DataManager.inst.PrefabTypes.Select(x => x as PrefabType))
            {
                int index = num;
                var gameObject = prefabTypePrefab.Duplicate(prefabTypeContent, prefabType.Name);

                var toggle = gameObject.transform.Find("Toggle").GetComponent<Toggle>();
                toggle.onValueChanged.ClearAll();
                toggle.isOn = PrefabEditor.inst.NewPrefabType == index;
                toggle.onValueChanged.AddListener(delegate (bool _val)
                {
                    onSelect?.Invoke(index);
                    RenderPrefabTypesPopup(onSelect);
                });
                
                toggle.image.color = prefabType.Color;

                var icon = gameObject.transform.Find("Toggle/Icon").GetComponent<Image>();
                icon.sprite = prefabType.Icon;

                var inputField = gameObject.transform.Find("Name").GetComponent<InputField>();
                inputField.onValueChanged.ClearAll();
                inputField.characterValidation = InputField.CharacterValidation.None;
                inputField.characterLimit = 0;
                inputField.text = prefabType.Name;
                inputField.onValueChanged.AddListener(delegate (string _val)
                {
                    string oldName = DataManager.inst.PrefabTypes[index].Name;

                    string name = _val;
                    int n = 0;
                    while (DataManager.inst.PrefabTypes.Has(x => x.Name == name))
                    {
                        name = $"{_val}[{n}]";
                        n++;
                    }

                    DataManager.inst.PrefabTypes[index].Name = name;

                    if (RTFile.FileExists(RTFile.ApplicationDirectory + "beatmaps/prefabtypes/" + oldName))
                    {
                        File.Delete(RTFile.ApplicationDirectory + "beatmaps/prefabtypes/" + oldName);
                    }
                });
                inputField.onEndEdit.ClearAll();
                inputField.onEndEdit.AddListener(delegate (string _val)
                {
                    SavePrefabTypes();
                    RenderPrefabTypesPopup(onSelect);
                });

                var color = gameObject.transform.Find("Color").GetComponent<InputField>();
                color.onValueChanged.ClearAll();
                color.characterValidation = InputField.CharacterValidation.None;
                color.characterLimit = 0;
                color.text = RTHelpers.ColorToHex(prefabType.Color);
                color.onValueChanged.AddListener(delegate (string _val)
                {
                    prefabType.Color = _val.Length == 8 ? LSColors.HexToColorAlpha(_val) : _val.Length == 6 ? LSColors.HexToColor(_val) : LSColors.pink500;
                });
                color.onEndEdit.ClearAll();
                color.onEndEdit.AddListener(delegate (string _val)
                {
                    RenderPrefabTypesPopup(onSelect);
                    SavePrefabTypes();
                });

                var delete = gameObject.transform.Find("Delete").GetComponent<Button>();
                delete.onClick.ClearAll();
                delete.onClick.AddListener(delegate ()
                {
                    var path = RTFile.ApplicationDirectory + "beatmaps/prefabtypes/" + prefabType.Name;

                    if (RTFile.DirectoryExists(path))
                    {
                        foreach (var file in Directory.GetFiles(path))
                        {
                            File.Delete(file);
                        }

                        Directory.Delete(path);
                    }

                    DataManager.inst.PrefabTypes.RemoveAt(index);

                    int n = 0;
                    foreach (var pt in DataManager.inst.PrefabTypes.Select(x => x as PrefabType))
                    {
                        pt.Index = n;
                        n++;
                    }

                    ReorderPrefabTypes();

                    RenderPrefabTypesPopup(onSelect);
                    SavePrefabTypes();
                });

                num++;
            }
        }

        public Text externalDescription;
        public Button externalType;
        public Image extenalTypeImage;

        public Button importPrefab;
        public Button exportToVG;

        public void CreatePrefabExternalDialog()
        {
            var editorDialogObject = Instantiate(EditorManager.inst.GetDialog("Multi Keyframe Editor (Object)").Dialog.gameObject);
            var editorDialogTransform = editorDialogObject.transform;
            editorDialogObject.name = "PrefabExternalDialog";
            editorDialogObject.layer = 5;
            editorDialogTransform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs").transform);
            editorDialogTransform.localScale = Vector3.one;
            editorDialogTransform.position = new Vector3(1537.5f, 714.945f, 0f) * EditorManager.inst.ScreenScale;
            editorDialogTransform.AsRT().sizeDelta = new Vector2(0f, 32f);

            var editorDialogTitle = editorDialogTransform.GetChild(0);
            editorDialogTitle.GetComponent<Image>().color = LSColors.HexToColor("4C4C4C");
            var documentationTitle = editorDialogTitle.GetChild(0).GetComponent<Text>();
            documentationTitle.text = "- Prefab External View -";
            documentationTitle.color = new Color(0.9f, 0.9f, 0.9f, 1f);

            var editorDialogSpacer = editorDialogTransform.GetChild(1);
            editorDialogSpacer.AsRT().sizeDelta = new Vector2(765f, 54f);

            //Destroy(editorDialogTransform.GetChild(2).gameObject);

            //var scrollView = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View"));
            //var documentationContent = scrollView.transform.Find("Viewport/Content");
            //scrollView.transform.SetParent(editorDialogTransform);
            //scrollView.transform.localScale = Vector3.one;
            //scrollView.name = "Scroll View";

            //LSHelpers.DeleteChildren(documentationContent);

            //var scrollViewLE = scrollView.AddComponent<LayoutElement>();
            //scrollViewLE.ignoreLayout = true;

            //scrollView.transform.AsRT().anchoredPosition = new Vector2(392.5f, 320f);
            //scrollView.transform.AsRT().sizeDelta = new Vector2(735f, 638f);

            editorDialogTransform.GetChild(1).AsRT().sizeDelta = new Vector2(765f, 24f);

            //{
            //    var prefabEditorData = EditorManager.inst.GetDialog("Prefab Editor").Dialog.Find("data/type");

            //    var prefabType = prefabEditorData.gameObject.Duplicate(editorDialogTransform, "Show Type Editor");

            //    ((RectTransform)prefabType.transform).sizeDelta = new Vector2(132f, 34f);
            //    prefabType.transform.Find("Text").GetComponent<Text>().text = "Open Prefab Type Editor";
            //    externalType = prefabType.GetComponent<Button>();
            //    extenalTypeImage = prefabType.GetComponent<Image>();
            //}

            {
                var spacer = new GameObject("spacer2");
                spacer.transform.SetParent(editorDialogTransform);
                spacer.transform.localScale = Vector3.one;

                var spacerRT = spacer.AddComponent<RectTransform>();
                spacerRT.sizeDelta = new Vector2(765f, 24f);
            }

            var textBase1 = new GameObject("Text Base 1");
            textBase1.transform.SetParent(editorDialogTransform);
            textBase1.transform.localScale = Vector3.one;

            var textBase1RT = textBase1.AddComponent<RectTransform>();
            textBase1RT.sizeDelta = new Vector2(765f, 300f);
            
            var textBase2 = new GameObject("Text Base 2");
            textBase2.transform.SetParent(textBase1RT);
            textBase2.transform.localScale = Vector3.one;

            var textBase2RT = textBase2.AddComponent<RectTransform>();
            textBase2RT.sizeDelta = new Vector2(740f, 300f);
            var textBase2Image = textBase2.AddComponent<Image>();
            textBase2Image.color = new Color(1f, 1f, 1f, 0.07f);

            var text = editorDialogTransform.GetChild(2);
            text.SetParent(textBase2RT);
            text.localPosition = Vector3.zero;
            text.localScale = Vector3.one;
            text.AsRT().sizeDelta = new Vector2(725f, 290f);
            externalDescription = text.GetComponent<Text>();
            externalDescription.text = "No description";
            externalDescription.alignment = TextAnchor.UpperLeft;

            {
                var spacer = new GameObject("spacer3");
                spacer.transform.SetParent(editorDialogTransform);
                spacer.transform.localScale = Vector3.one;

                var spacerRT = spacer.AddComponent<RectTransform>();
                spacerRT.sizeDelta = new Vector2(765f, 260f);
            }

            var buttonsBase = new GameObject("buttons base");
            buttonsBase.transform.SetParent(editorDialogTransform);
            buttonsBase.transform.localScale = Vector3.one;

            var buttonsBaseRT = buttonsBase.AddComponent<RectTransform>();
            buttonsBaseRT.sizeDelta = new Vector2(765f, 0f);

            var buttons = new GameObject("buttons");
            buttons.transform.SetParent(buttonsBaseRT);
            buttons.transform.localScale = Vector3.one;

            var buttonsHLG = buttons.AddComponent<HorizontalLayoutGroup>();
            buttonsHLG.spacing = 60f;

            buttons.transform.AsRT().sizeDelta = new Vector2(600f, 32f);
            
            var tfv = ObjEditor.inst.ObjectView.transform;

            var importPrefab = tfv.Find("applyprefab").gameObject.Duplicate(buttons.transform);
            importPrefab.SetActive(true);
            importPrefab.name = "import";
            this.importPrefab = importPrefab.GetComponent<Button>();
            importPrefab.transform.GetChild(0).GetComponent<Text>().text = "Import Prefab";

            var exportToVG = tfv.Find("applyprefab").gameObject.Duplicate(buttons.transform);
            exportToVG.SetActive(true);
            exportToVG.name = "export";
            this.exportToVG = exportToVG.GetComponent<Button>();
            exportToVG.transform.GetChild(0).GetComponent<Text>().text = "Export to VG Format";

            EditorHelper.AddEditorDialog("Prefab External Dialog", editorDialogObject);
        }

        public void RenderPrefabExternalDialog(Prefab prefab)
        {
            //extenalTypeImage.color = prefab.Type < DataManager.inst.PrefabTypes.Count ? DataManager.inst.PrefabTypes[prefab.Type].Color : PrefabType.InvalidType.Color;
            //externalType.onClick.ClearAll();
            //externalType.onClick.AddListener(delegate ()
            //{
            //    OpenPrefabTypePopup(delegate (int index)
            //    {
            //        prefab.Type = index;
            //        extenalTypeImage.color = prefab.Type < DataManager.inst.PrefabTypes.Count ? DataManager.inst.PrefabTypes[prefab.Type].Color : PrefabType.InvalidType.Color;
            //    });
            //});

            importPrefab.onClick.ClearAll();
            importPrefab.onClick.AddListener(delegate ()
            {
                ImportPrefabIntoLevel(prefab);
            });

            exportToVG.onClick.ClearAll();
            exportToVG.onClick.AddListener(delegate ()
            {
                if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + "beatmaps/exports"))
                    Directory.CreateDirectory(RTFile.ApplicationDirectory + "beatmaps/exports");

                RTFile.WriteToFile($"{RTFile.ApplicationDirectory}beatmaps/exports/{prefab.Name.ToLower()}.vgp", prefab.ToJSONVG().ToString());

                EditorManager.inst.DisplayNotification($"Converted Prefab to VG format and saved to {prefab.Name.ToLower()}.vgp!", 4f, EditorManager.NotificationType.Success);
            });

            externalDescription.text = prefab.description;
        }

        public void CreateNewPrefab()
        {
            if (ObjectEditor.inst.SelectedBeatmapObjects.Count <= 0)
            {
                EditorManager.inst.DisplayNotification("Can't save prefab without any objects in it!", 2f, EditorManager.NotificationType.Error, false);
                return;
            }

            if (string.IsNullOrEmpty(PrefabEditor.inst.NewPrefabName))
            {
                EditorManager.inst.DisplayNotification("Can't save prefab without a name!", 2f, EditorManager.NotificationType.Error, false);
                return;
            }

            var prefab = new Prefab(
                PrefabEditor.inst.NewPrefabName,
                PrefabEditor.inst.NewPrefabType,
                PrefabEditor.inst.NewPrefabOffset,
                ObjectEditor.inst.SelectedBeatmapObjects.Select(x => x.GetData<BeatmapObject>()).ToList(),
                ObjectEditor.inst.SelectedPrefabObjects.Select(x => x.GetData<PrefabObject>()).ToList());

            prefab.description = NewPrefabDescription;

            if (createInternal)
                ImportPrefabIntoLevel(prefab);
            else
                SavePrefab(prefab);

            PrefabEditor.inst.OpenPopup();
            ObjEditor.inst.OpenDialog();
        }

        public void SavePrefab(Prefab prefab)
        {
            EditorManager.inst.DisplayNotification($"Saving Prefab to System [{prefab.Name}]!", 2f, EditorManager.NotificationType.Warning);
            Debug.Log($"{PrefabEditor.inst.className}Saving Prefab to File System!");

            prefab.objects.ForEach(x => x.prefabID = "");
            prefab.objects.ForEach(x => x.prefabInstanceID = "");
            PrefabEditor.inst.LoadedPrefabs.Add(prefab);
            PrefabEditor.inst.LoadedPrefabsFiles.Add($"{RTFile.ApplicationDirectory}{RTEditor.prefabListSlash}{prefab.Name.ToLower().Replace(" ", "_")}.lsp");

            FileManager.inst.SaveJSONFile(RTEditor.prefabListPath, $"{prefab.Name.ToLower().Replace(" ", "_")}.lsp", prefab.ToJSON().ToString());
            EditorManager.inst.DisplayNotification($"Saved prefab [{prefab.Name}]!", 2f, EditorManager.NotificationType.Success);
        }

        public void DeleteExternalPrefab(int __0)
        {
            if (RTFile.FileExists(PrefabEditor.inst.LoadedPrefabsFiles[__0]))
                FileManager.inst.DeleteFileRaw(PrefabEditor.inst.LoadedPrefabsFiles[__0]);

            PrefabEditor.inst.LoadedPrefabs.RemoveAt(__0);
            PrefabEditor.inst.LoadedPrefabsFiles.RemoveAt(__0);

            PrefabEditor.inst.ReloadExternalPrefabsInPopup(false);
        }

        public void DeleteInternalPrefab(int __0)
        {
            string id = DataManager.inst.gameData.prefabs[__0].ID;

            DataManager.inst.gameData.prefabs.RemoveAt(__0);
            DataManager.inst.gameData.prefabObjects.RemoveAll(x => x.prefabID == id);

            PrefabEditor.inst.ReloadInternalPrefabsInPopup();
            ObjectEditor.inst.RenderTimelineObjects();

            //DataManager.inst.gameData.beatmapObjects.Where(x => x.prefabID == id).ToList().ForEach(x => Updater.UpdateProcessor(x, reinsert: false));
            //DataManager.inst.gameData.beatmapObjects.RemoveAll(x => x.prefabID == id);

            ObjectManager.inst.updateObjects();
        }

        public void OpenPopup()
        {
            EditorManager.inst.ClearDialogs(new EditorManager.EditorDialog.DialogType[1]);
            EditorManager.inst.ShowDialog("Prefab Popup");
            PrefabEditor.inst.UpdateCurrentPrefab(PrefabEditor.inst.currentPrefab);

            var selectToggle = PrefabEditor.inst.internalPrefabDialog.Find("select_prefab/select_toggle").GetComponent<Button>();
            selectToggle.onClick.RemoveAllListeners();
            selectToggle.onClick.AddListener(delegate ()
            {
                PrefabEditor.inst.internalPrefabDialog.Find("select_prefab/selected_prefab").GetComponent<Text>().text = "<color=#669e37>Selecting</color>";
                PrefabEditor.inst.ReloadInternalPrefabsInPopup(true);
            });

            PrefabEditor.inst.externalSearch.onValueChanged.RemoveAllListeners();
            PrefabEditor.inst.externalSearch.onValueChanged.AddListener(delegate (string _value)
            {
                PrefabEditor.inst.externalSearchStr = _value;
                PrefabEditor.inst.ReloadExternalPrefabsInPopup();
            });

            PrefabEditor.inst.internalSearch.onValueChanged.RemoveAllListeners();
            PrefabEditor.inst.internalSearch.onValueChanged.AddListener(delegate (string _value)
            {
                PrefabEditor.inst.internalSearchStr = _value;
                PrefabEditor.inst.ReloadInternalPrefabsInPopup();
            });

            PrefabEditor.inst.ReloadExternalPrefabsInPopup();
            PrefabEditor.inst.ReloadInternalPrefabsInPopup();
        }

        public void ReloadSelectionContent()
        {
            LSHelpers.DeleteChildren(PrefabEditor.inst.gridContent, false);
            int num = 0;
            foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects.Where(x => !x.fromPrefab))
            {
                if (RTHelpers.SearchString(beatmapObject.name, PrefabEditor.inst.gridSearch.text))
                {
                    var tmpGridObj = PrefabEditor.inst.selectionPrefab.Duplicate(PrefabEditor.inst.gridContent, "grid");
                    tmpGridObj.transform.Find("text").GetComponent<Text>().text = beatmapObject.name;
                    int tmpIndex = num;

                    if (RTEditor.inst.timelineObjects.TryFind(x => x.ID == beatmapObject.id, out TimelineObject timelineObject))
                    {
                        tmpGridObj.GetComponentAndPerformAction(delegate (Toggle x)
                        {
                            x.NewValueChangedListener(timelineObject.selected, delegate (bool _val)
                            {
                                timelineObject.selected = _val;
                            });
                        });
                    }
                }
                num++;
            }
        }

        public void OpenDialog()
        {
            EditorManager.inst.ClearDialogs(Array.Empty<EditorManager.EditorDialog.DialogType>());
            EditorManager.inst.ShowDialog("Prefab Editor");
            PrefabEditor.inst.dialog = EditorManager.inst.GetDialog("Prefab Editor").Dialog;

            var component = PrefabEditor.inst.dialog.Find("data/name/input").GetComponent<InputField>();
            component.onValueChanged.RemoveAllListeners();
            component.onValueChanged.AddListener(delegate (string _value)
            {
                PrefabEditor.inst.NewPrefabName = _value;
            });

            var offsetSlider = PrefabEditor.inst.dialog.Find("data/offset/slider").GetComponent<Slider>();
            var offsetInput = PrefabEditor.inst.dialog.Find("data/offset/input").GetComponent<InputField>();

            bool setting = false;
            offsetSlider.onValueChanged.RemoveAllListeners();
            offsetSlider.onValueChanged.AddListener(delegate (float _value)
            {
                if (!setting)
                {
                    setting = true;
                    PrefabEditor.inst.NewPrefabOffset = Mathf.Round(_value * 100f) / 100f;
                    offsetInput.text = PrefabEditor.inst.NewPrefabOffset.ToString();
                }
                setting = false;
            });

            offsetInput.onValueChanged.RemoveAllListeners();
            offsetInput.characterLimit = 0;
            offsetInput.onValueChanged.AddListener(delegate (string _value)
            {
                if (!setting && float.TryParse(_value, out float num))
                {
                    setting = true;
                    PrefabEditor.inst.NewPrefabOffset = num;
                    offsetSlider.value = num;
                }
                setting = false;
            });

            TriggerHelper.AddEventTriggerParams(offsetInput.gameObject, TriggerHelper.ScrollDelta(offsetInput));

            TriggerHelper.IncreaseDecreaseButtons(offsetInput, t: PrefabEditor.inst.dialog.Find("data/offset"));
            PrefabEditor.inst.dialog.Find("data/type/Show Type Editor").GetComponent<Image>().color =
                DataManager.inst.PrefabTypes[Mathf.Clamp(PrefabEditor.inst.NewPrefabType, 0, DataManager.inst.PrefabTypes.Count - 1)].Color;

            var description = PrefabEditor.inst.dialog.Find("data/description/input").GetComponent<InputField>();
            description.onValueChanged.ClearAll();
            ((Text)description.placeholder).text = "Prefab Description";
            description.lineType = InputField.LineType.MultiLineNewline;
            description.characterLimit = 0;
            description.characterValidation = InputField.CharacterValidation.None;
            description.textComponent.alignment = TextAnchor.UpperLeft;
            NewPrefabDescription = string.IsNullOrEmpty(NewPrefabDescription) ? "What is your prefab like?" : NewPrefabDescription;
            description.text = NewPrefabDescription;
            description.onValueChanged.AddListener(delegate (string _val)
            {
                NewPrefabDescription = _val;
            });

            ReloadSelectionContent();

            ((RectTransform)PrefabEditor.inst.dialog.Find("data/type/Show Type Editor")).sizeDelta = new Vector2(260f, 34f);

            //PrefabEditor.inst.dialog.Find("data/offset/<").GetComponent<Button>().onClick.RemoveAllListeners();
            //PrefabEditor.inst.dialog.Find("data/offset/<").GetComponent<Button>().onClick.AddListener(delegate ()
            //{
            //    PrefabEditor.inst.NewPrefabOffset += 0.1f;
            //    PrefabEditor.inst.dialog.Find("data/offset/input").GetComponent<InputField>().text = PrefabEditor.inst.NewPrefabOffset.ToString();
            //    PrefabEditor.inst.dialog.Find("data/offset/slider").GetComponent<Slider>().value = PrefabEditor.inst.NewPrefabOffset;
            //});
            //PrefabEditor.inst.dialog.Find("data/offset/>").GetComponent<Button>().onClick.RemoveAllListeners();
            //PrefabEditor.inst.dialog.Find("data/offset/>").GetComponent<Button>().onClick.AddListener(delegate ()
            //{
            //    PrefabEditor.inst.NewPrefabOffset -= 0.1f;
            //    PrefabEditor.inst.dialog.Find("data/offset/input").GetComponent<InputField>().text = PrefabEditor.inst.NewPrefabOffset.ToString();
            //    PrefabEditor.inst.dialog.Find("data/offset/slider").GetComponent<Slider>().value = PrefabEditor.inst.NewPrefabOffset;
            //});
            //PrefabEditor.inst.dialog.Find("data/type/types/col_" + PrefabEditor.inst.NewPrefabType).GetComponent<Toggle>().isOn = true;
            //for (int i = 0; i < DataManager.inst.PrefabTypes.Count; i++)
            //{
            //    int index = i;
            //    PrefabEditor.inst.dialog.Find("data/type/types/col_" + i + "/text").GetComponent<Text>().text = DataManager.inst.PrefabTypes[i].Name;
            //    PrefabEditor.inst.dialog.Find("data/type/types/col_" + i + "/Background").GetComponent<Image>().color = DataManager.inst.PrefabTypes[i].Color;
            //    PrefabEditor.inst.dialog.Find("data/type/types/col_" + i).GetComponent<Toggle>().onValueChanged.RemoveAllListeners();
            //    PrefabEditor.inst.dialog.Find("data/type/types/col_" + i).GetComponent<Toggle>().onValueChanged.AddListener(delegate (bool val)
            //    {
            //        int tmpIndex = index;
            //        PrefabEditor.inst.NewPrefabType = tmpIndex;
            //    });
            //}
            //PrefabEditor.inst.dialog.Find("data/type/types/<").GetComponent<Button>().onClick.RemoveAllListeners();
            //PrefabEditor.inst.dialog.Find("data/type/types/<").GetComponent<Button>().onClick.AddListener(delegate ()
            //{
            //    PrefabEditor.inst.NewPrefabType--;
            //    PrefabEditor.inst.NewPrefabType = Mathf.Clamp(PrefabEditor.inst.NewPrefabType, 0, 9);
            //    PrefabEditor.inst.dialog.Find("data/type/types/col_" + PrefabEditor.inst.NewPrefabType).GetComponent<Toggle>().isOn = true;
            //});
            //PrefabEditor.inst.dialog.Find("data/type/types/>").GetComponent<Button>().onClick.RemoveAllListeners();
            //PrefabEditor.inst.dialog.Find("data/type/types/>").GetComponent<Button>().onClick.AddListener(delegate ()
            //{
            //    PrefabEditor.inst.NewPrefabType++;
            //    PrefabEditor.inst.NewPrefabType = Mathf.Clamp(PrefabEditor.inst.NewPrefabType, 0, 9);
            //    PrefabEditor.inst.dialog.Find("data/type/types/col_" + PrefabEditor.inst.NewPrefabType).GetComponent<Toggle>().isOn = true;
            //});
        }

        public void UpdateCurrentPrefab(BasePrefab __0)
        {
            PrefabEditor.inst.currentPrefab = __0;

            bool prefabExists = PrefabEditor.inst.currentPrefab != null;

            PrefabEditor.inst.internalPrefabDialog.Find("select_prefab/selected_prefab").GetComponent<Text>().text = (!prefabExists ? "-Select Prefab-" : "<color=#669e37>-Prefab-</color>") + "\n" + (!prefabExists ? "n/a" : PrefabEditor.inst.currentPrefab.Name);
        }

        public void RenderPrefabObjectDialog(PrefabObject prefabObject, PrefabEditor __instance)
        {
            #region Original Code

            var currentPrefab = prefabObject;
            var prefab = currentPrefab.GetPrefab();

            var right = EditorManager.inst.GetDialog("Prefab Selector").Dialog.Find("data/right");

            right.Find("time/time").GetComponentAndPerformAction(delegate (InputField inputField)
            {
                inputField.NewValueChangedListener(prefab.Offset.ToString(), delegate (string _val)
                {
                    if (float.TryParse(_val, out float offset))
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
                prefabSelectorLeft.Find("tod-dropdown").GetComponentAndPerformAction(delegate (Dropdown dropdown)
                {
                    dropdown.onValueChanged.ClearAll();
                    dropdown.value = (int)currentPrefab.autoKillType;
                    dropdown.onValueChanged.AddListener(delegate (int _val)
                    {
                        currentPrefab.autoKillType = (PrefabObject.AutoKillType)_val;
                        Updater.UpdatePrefab(currentPrefab, "autokill");
                    });
                });

                prefabSelectorLeft.Find("akoffset").GetComponentAndPerformAction(delegate (InputField inputField)
                {
                    inputField.onValueChanged.ClearAll();
                    inputField.characterValidation = InputField.CharacterValidation.None;
                    inputField.contentType = InputField.ContentType.Standard;
                    inputField.characterLimit = 0;
                    inputField.text = currentPrefab.autoKillOffset.ToString();
                    inputField.onValueChanged.AddListener(delegate (string _val)
                    {
                        if (float.TryParse(_val, out float num))
                        {
                            currentPrefab.autoKillOffset = num;
                            Updater.UpdatePrefab(currentPrefab, "autokill");
                        }
                    });

                    TriggerHelper.IncreaseDecreaseButtons(inputField);
                    TriggerHelper.AddEventTriggerParams(inputField.gameObject, TriggerHelper.ScrollDelta(inputField));
                });

                prefabSelectorLeft.Find("akoffset/|").GetComponentAndPerformAction(delegate (Button button)
                {
                    button.onClick.ClearAll();
                    button.onClick.AddListener(delegate ()
                    {
                        currentPrefab.autoKillOffset = currentPrefab.autoKillType == PrefabObject.AutoKillType.StartTimeOffset ? currentPrefab.StartTime + prefab.Offset :
                                                       currentPrefab.autoKillType == PrefabObject.AutoKillType.SongTime ? AudioManager.inst.CurrentAudioSource.time : -1f;
                    });
                });

                prefabSelectorLeft.Find("time").GetComponentAndPerformAction(delegate (InputField inputField)
                {
                    inputField.NewValueChangedListener(currentPrefab.StartTime.ToString(), delegate (string _val)
                    {
                        if (float.TryParse(_val, out float n))
                        {
                            n = Mathf.Clamp(n, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                            currentPrefab.StartTime = n;
                            Updater.UpdatePrefab(currentPrefab, "starttime");
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
                                Updater.UpdatePrefab(currentPrefab, "offset");
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
                                        Updater.UpdatePrefab(currentPrefab, "offset");
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

                prefabSelectorLeft.Find("repeat/x").GetComponentAndPerformAction(delegate (InputField inputField)
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

                prefabSelectorLeft.Find("repeat/y").GetComponentAndPerformAction(delegate (InputField inputField)
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

                prefabSelectorLeft.Find("speed").GetComponentAndPerformAction(delegate (InputField inputField)
                {
                    inputField.NewValueChangedListener(Mathf.Clamp(currentPrefab.speed, 0.1f, Updater.MaxFastSpeed).ToString(), delegate (string _val)
                    {
                        if (float.TryParse(_val, out float num))
                        {
                            num = Mathf.Clamp(num, 0.1f, Updater.MaxFastSpeed);
                            currentPrefab.speed = num;
                            Updater.UpdatePrefab(currentPrefab, "Speed");
                        }
                    });

                    TriggerHelper.IncreaseDecreaseButtons(inputField, min: 0.1f, max: Updater.MaxFastSpeed);
                    TriggerHelper.AddEventTriggerParams(inputField.gameObject, TriggerHelper.ScrollDelta(inputField, min: 0.1f, max: Updater.MaxFastSpeed));
                });

                //Global Settings
                {
                    nameIF.onValueChanged.ClearAll();
                    nameIF.text = prefab.Name;
                    nameIF.onValueChanged.AddListener(delegate (string _val)
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
                    typeImage.color = DataManager.inst.PrefabTypes[prefab.Type].Color;
                    typeImage.transform.Find("Text").GetComponent<Text>().color = RTHelpers.InvertColorHue(RTHelpers.InvertColorValue(DataManager.inst.PrefabTypes[prefab.Type].Color));

                    currentPrefabType = prefab.Type;

                    var entry = new EventTrigger.Entry();
                    entry.eventID = EventTriggerType.Scroll;
                    entry.callback.AddListener(delegate (BaseEventData eventData)
                    {
                        var pointerEventData = (PointerEventData)eventData;

                        int add = pointerEventData.scrollDelta.y < 0f ? prefab.Type - 1 : pointerEventData.scrollDelta.y > 0f ? prefab.Type + 1 : 0;

                        int num = Mathf.Clamp(add, 0, DataManager.inst.PrefabTypes.Count - 1);

                        prefab.Type = num;
                        SearchPrefabType(DataManager.inst.PrefabTypes[prefab.Type].Name, prefab);
                        typeImage.color = DataManager.inst.PrefabTypes[prefab.Type].Color;
                        currentPrefabType = num;

                        foreach (var prefabObject in DataManager.inst.gameData.prefabObjects)
                        {
                            if (prefabObject.prefabID == prefab.ID)
                            {
                                ObjectEditor.inst.RenderTimelineObject(new TimelineObject(prefabObject));
                            }
                        }
                    });

                    TriggerHelper.AddEventTrigger(typeIF.gameObject, new List<EventTrigger.Entry> { entry });

                    var leftButton = typeIF.transform.Find("<").GetComponent<Button>();
                    var rightButton = typeIF.transform.Find(">").GetComponent<Button>();

                    leftButton.onClick.ClearAll();
                    leftButton.onClick.AddListener(delegate ()
                    {
                        int num = Mathf.Clamp(prefab.Type - 1, 0, DataManager.inst.PrefabTypes.Count - 1);

                        prefab.Type = num;
                        SearchPrefabType(DataManager.inst.PrefabTypes[prefab.Type].Name, prefab);
                        typeImage.color = DataManager.inst.PrefabTypes[prefab.Type].Color;
                        typeImage.transform.Find("Text").GetComponent<Text>().color = RTHelpers.InvertColorHue(RTHelpers.InvertColorValue(DataManager.inst.PrefabTypes[prefab.Type].Color));
                        currentPrefabType = num;

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
                        typeImage.color = DataManager.inst.PrefabTypes[prefab.Type].Color;
                        typeImage.transform.Find("Text").GetComponent<Text>().color = RTHelpers.InvertColorHue(RTHelpers.InvertColorValue(DataManager.inst.PrefabTypes[prefab.Type].Color));
                        currentPrefabType = num;
                        foreach (var prefabObject in DataManager.inst.gameData.prefabObjects)
                        {
                            if (prefabObject.prefabID == prefab.ID)
                            {
                                ObjectEditor.inst.RenderTimelineObject(new TimelineObject(prefabObject));
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

                    objectCount.text = "Object Count: " + prefab.objects.Count.ToString();
                    prefabObjectCount.text = "Prefab Object Count: " + prefab.prefabObjects.Count;
                    prefabObjectTimelineCount.text = "Prefab Object (Imported) Count: " + DataManager.inst.gameData.prefabObjects.FindAll(x => x.prefabID == prefab.ID).Count;
                }
            }

            #endregion
        }

        public void SearchPrefabType(string t, BasePrefab prefab)
        {
            typeIF.onValueChanged.RemoveAllListeners();
            typeIF.text = t;
            typeIF.onValueChanged.AddListener(delegate (string _val)
            {
                if (DataManager.inst.PrefabTypes.Find(x => x.Name.ToLower() == _val.ToLower()) != null)
                {
                    prefab.Type = DataManager.inst.PrefabTypes.FindIndex(x => x.Name.ToLower() == _val.ToLower());
                    typeImage.color = DataManager.inst.PrefabTypes[prefab.Type].Color;
                    typeImage.transform.Find("Text").GetComponent<Text>().color = RTHelpers.InvertColorHue(RTHelpers.InvertColorValue(DataManager.inst.PrefabTypes[prefab.Type].Color));
                    currentPrefabType = prefab.Type;
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

        public IEnumerator InternalPrefabs(bool _toggle = false)
        {
            // Here we add the Example prefab provided to you.
            if (!DataManager.inst.gameData.prefabs.Exists(x => x.ID == "toYoutoYoutoYou") && RTEditor.GetEditorProperty("Prefab Example Template").GetConfigEntry<bool>().Value)
                DataManager.inst.gameData.prefabs.Add(Prefab.DeepCopy(ExamplePrefab.PAExampleM, false));

            yield return new WaitForSeconds(0.03f);

            LSHelpers.DeleteChildren(PrefabEditor.inst.internalContent);
            var gameObject = PrefabEditor.inst.CreatePrefab.Duplicate(PrefabEditor.inst.internalContent, "add new prefab");
            gameObject.GetComponentInChildren<Text>().text = "New Internal Prefab";

            var hover = gameObject.AddComponent<HoverUI>();
            hover.animateSca = true;
            hover.animatePos = false;
            hover.size = RTEditor.GetEditorProperty("Prefab Button Hover Size").GetConfigEntry<float>().Value;

            gameObject.GetComponentAndPerformAction(delegate (Button x)
            {
                x.NewOnClickListener(delegate ()
                {
                    PrefabEditor.inst.OpenDialog();
                    createInternal = true;
                });
            });

            var hoverSize = RTEditor.GetEditorProperty("Prefab Button Hover Size").GetConfigEntry<float>().Value;

            bool isExternal = false;

            var nameHorizontalOverflow = isExternal ?
                RTEditor.GetEditorProperty("Prefab External Name Horizontal Wrap").GetConfigEntry<HorizontalWrapMode>().Value :
                RTEditor.GetEditorProperty("Prefab Internal Name Horizontal Wrap").GetConfigEntry<HorizontalWrapMode>().Value;

            var nameVerticalOverflow = isExternal ?
                RTEditor.GetEditorProperty("Prefab External Name Vertical Wrap").GetConfigEntry<VerticalWrapMode>().Value :
                RTEditor.GetEditorProperty("Prefab Internal Name Vertical Wrap").GetConfigEntry<VerticalWrapMode>().Value;

            var nameFontSize = isExternal ?
                RTEditor.GetEditorProperty("Prefab External Name Font Size").GetConfigEntry<int>().Value :
                RTEditor.GetEditorProperty("Prefab Internal Name Font Size").GetConfigEntry<int>().Value;

            var typeHorizontalOverflow = isExternal ?
                RTEditor.GetEditorProperty("Prefab External Type Horizontal Wrap").GetConfigEntry<HorizontalWrapMode>().Value :
                RTEditor.GetEditorProperty("Prefab Internal Type Horizontal Wrap").GetConfigEntry<HorizontalWrapMode>().Value;

            var typeVerticalOverflow = isExternal ?
                RTEditor.GetEditorProperty("Prefab External Type Vertical Wrap").GetConfigEntry<VerticalWrapMode>().Value :
                RTEditor.GetEditorProperty("Prefab Internal Type Vertical Wrap").GetConfigEntry<VerticalWrapMode>().Value;

            var typeFontSize = isExternal ?
                RTEditor.GetEditorProperty("Prefab External Type Font Size").GetConfigEntry<int>().Value :
                RTEditor.GetEditorProperty("Prefab Internal Type Font Size").GetConfigEntry<int>().Value;

            var deleteAnchoredPosition = isExternal ?
                RTEditor.GetEditorProperty("Prefab External Delete Button Pos").GetConfigEntry<Vector2>().Value :
                RTEditor.GetEditorProperty("Prefab Internal Delete Button Pos").GetConfigEntry<Vector2>().Value;
            var deleteSizeDelta = isExternal ?
                RTEditor.GetEditorProperty("Prefab External Delete Button Sca").GetConfigEntry<Vector2>().Value :
                RTEditor.GetEditorProperty("Prefab Internal Delete Button Sca").GetConfigEntry<Vector2>().Value;

            var list = new List<Coroutine>();

            int num = 0;
            foreach (var prefab in DataManager.inst.gameData.prefabs)
            {
                if (ContainsName(prefab, PrefabDialog.Internal))
                    list.Add(StartCoroutine(CreatePrefabButton((Prefab)prefab, num, PrefabDialog.Internal, _toggle, hoverSize,
                        nameHorizontalOverflow, nameVerticalOverflow, nameFontSize,
                        typeHorizontalOverflow, typeVerticalOverflow, typeFontSize,
                        deleteAnchoredPosition, deleteSizeDelta)));
                num++;
            }

            yield return StartCoroutine(LSHelpers.WaitForMultipleCoroutines(list, delegate ()
            {
                //foreach (object obj in internalContent)
                //    ((Transform)obj).localScale = Vector3.one;
            }));

            yield break;
        }

        public IEnumerator ExternalPrefabFiles(bool _toggle = false)
        {
            foreach (var prefabPanel in PrefabPanels.Where(x => x.Dialog == PrefabDialog.External))
            {
                prefabPanel.SetActive(ContainsName(prefabPanel.Prefab, PrefabDialog.External));
            }

            //yield return new WaitForSeconds(0.03f);

            //LSHelpers.DeleteChildren(PrefabEditor.inst.externalContent);
            //var gameObject = PrefabEditor.inst.CreatePrefab.Duplicate(PrefabEditor.inst.externalContent, "add new prefab");
            //gameObject.GetComponentInChildren<Text>().text = "New External Prefab";

            //var hoverSize = RTEditor.GetEditorProperty("Prefab Button Hover Size").GetConfigEntry<float>().Value;

            //var hover = gameObject.AddComponent<HoverUI>();
            //hover.animateSca = true;
            //hover.animatePos = false;
            //hover.size = hoverSize;

            //gameObject.GetComponentAndPerformAction(delegate (Button x)
            //{
            //    x.NewOnClickListener(delegate ()
            //    {
            //        PrefabEditor.inst.OpenDialog();
            //        createInternal = false;
            //    });
            //});

            //bool isExternal = true;

            //var nameHorizontalOverflow = isExternal ?
            //    RTEditor.GetEditorProperty("Prefab External Name Horizontal Wrap").GetConfigEntry<HorizontalWrapMode>().Value :
            //    RTEditor.GetEditorProperty("Prefab Internal Name Horizontal Wrap").GetConfigEntry<HorizontalWrapMode>().Value;

            //var nameVerticalOverflow = isExternal ?
            //    RTEditor.GetEditorProperty("Prefab External Name Vertical Wrap").GetConfigEntry<VerticalWrapMode>().Value :
            //    RTEditor.GetEditorProperty("Prefab Internal Name Vertical Wrap").GetConfigEntry<VerticalWrapMode>().Value;

            //var nameFontSize = isExternal ?
            //    RTEditor.GetEditorProperty("Prefab External Name Font Size").GetConfigEntry<int>().Value :
            //    RTEditor.GetEditorProperty("Prefab Internal Name Font Size").GetConfigEntry<int>().Value;

            //var typeHorizontalOverflow = isExternal ?
            //    RTEditor.GetEditorProperty("Prefab External Type Horizontal Wrap").GetConfigEntry<HorizontalWrapMode>().Value :
            //    RTEditor.GetEditorProperty("Prefab Internal Type Horizontal Wrap").GetConfigEntry<HorizontalWrapMode>().Value;

            //var typeVerticalOverflow = isExternal ?
            //    RTEditor.GetEditorProperty("Prefab External Type Vertical Wrap").GetConfigEntry<VerticalWrapMode>().Value :
            //    RTEditor.GetEditorProperty("Prefab Internal Type Vertical Wrap").GetConfigEntry<VerticalWrapMode>().Value;

            //var typeFontSize = isExternal ?
            //    RTEditor.GetEditorProperty("Prefab External Type Font Size").GetConfigEntry<int>().Value :
            //    RTEditor.GetEditorProperty("Prefab Internal Type Font Size").GetConfigEntry<int>().Value;

            //var deleteAnchoredPosition = isExternal ?
            //    RTEditor.GetEditorProperty("Prefab External Delete Button Pos").GetConfigEntry<Vector2>().Value :
            //    RTEditor.GetEditorProperty("Prefab Internal Delete Button Pos").GetConfigEntry<Vector2>().Value;
            //var deleteSizeDelta = isExternal ?
            //    RTEditor.GetEditorProperty("Prefab External Delete Button Sca").GetConfigEntry<Vector2>().Value :
            //    RTEditor.GetEditorProperty("Prefab Internal Delete Button Sca").GetConfigEntry<Vector2>().Value;

            //var list = new List<Coroutine>();

            //int num = 0;
            //foreach (var prefab in PrefabEditor.inst.LoadedPrefabs)
            //{
            //    if (ContainsName(prefab, PrefabDialog.External))
            //    {
            //        list.Add(StartCoroutine(CreatePrefabButton((Prefab)prefab, num, PrefabDialog.External, _toggle, hoverSize,
            //            nameHorizontalOverflow, nameVerticalOverflow, nameFontSize,
            //            typeHorizontalOverflow, typeVerticalOverflow, typeFontSize,
            //            deleteAnchoredPosition, deleteSizeDelta)));
            //    }
            //    num++;
            //}

            //yield return StartCoroutine(LSHelpers.WaitForMultipleCoroutines(list, delegate ()
            //{
            //    //foreach (object obj in externalContent)
            //    //    ((Transform)obj).localScale = Vector3.one;
            //}));

            yield break;
        }

        public IEnumerator CreatePrefabButton(Prefab prefab, int index, PrefabDialog dialog, bool _toggle, float hoversize,
            HorizontalWrapMode nameHorizontalWrapMode, VerticalWrapMode nameVerticalWrapMode, int nameFontSize,
            HorizontalWrapMode typeHorizontalWrapMode, VerticalWrapMode typeVerticalWrapMode, int typeFontSize,
            Vector2 deleteAnchoredPosition, Vector2 deleteSizeDelta)
        {
            bool isExternal = dialog == PrefabDialog.External;
            var gameObject = PrefabEditor.inst.AddPrefab.Duplicate(isExternal ? PrefabEditor.inst.externalContent : PrefabEditor.inst.internalContent);
            var tf = gameObject.transform;

            var hover = gameObject.AddComponent<HoverUI>();
            hover.animateSca = true;
            hover.animatePos = false;
            hover.size = hoversize;

            //var hover = gameObject.AddComponent<HoverUI>();
            //hover.animateSca = true;
            //hover.animatePos = false;
            //hover.size = RTEditor.GetEditorProperty("Prefab Button Hover Size").GetConfigEntry<float>().Value;

            var name = tf.Find("name").GetComponent<Text>();
            var typeName = tf.Find("type-name").GetComponent<Text>();
            var color = tf.Find("category").GetComponent<Image>();
            var deleteRT = tf.Find("delete").GetComponent<RectTransform>();
            var addPrefabObject = gameObject.GetComponent<Button>();
            var delete = tf.Find("delete").GetComponent<Button>();

            PrefabPanels.Add(new PrefabPanel
            {
                GameObject = gameObject,
                Button = addPrefabObject,
                DeleteButton = delete,
                Dialog = dialog,
                Name = name,
                TypeText = typeName,
                TypeImage = color,
                Prefab = prefab
            });

            name.text = prefab.Name;
            //_p.Type = Mathf.Clamp(_p.Type, 0, DataManager.inst.PrefabTypes.Count - 1);

            var prefabType = prefab.Type < DataManager.inst.PrefabTypes.Count ? DataManager.inst.PrefabTypes[prefab.Type] : PrefabType.InvalidType;

            typeName.text = prefabType.Name;
            color.color = prefabType.Color;

            TooltipHelper.AddTooltip(gameObject, 
                "<#" + LSColors.ColorToHex(color.color) + ">" + prefab.Name + "</color>",
                "O: " + prefab.Offset +
                "<br>T: " + typeName.text +
                "<br>Count: " + prefab.objects.Count + 
                "<br>Description: " + prefab.description);

            addPrefabObject.onClick.RemoveAllListeners();
            delete.onClick.RemoveAllListeners();

            name.horizontalOverflow = nameHorizontalWrapMode;
            name.verticalOverflow = nameVerticalWrapMode;
            name.fontSize = nameFontSize;

            typeName.horizontalOverflow = typeHorizontalWrapMode;
            typeName.verticalOverflow = typeVerticalWrapMode;
            typeName.fontSize = typeFontSize;

            deleteRT.anchoredPosition = deleteAnchoredPosition;
            deleteRT.sizeDelta = deleteSizeDelta;

            // Gonna have to remove these customizations for now until I put the Configs back into their own static class.
            //name.horizontalOverflow = isExternal ?
            //    RTEditor.GetEditorProperty("Prefab External Name Horizontal Wrap").GetConfigEntry<HorizontalWrapMode>().Value :
            //    RTEditor.GetEditorProperty("Prefab Internal Name Horizontal Wrap").GetConfigEntry<HorizontalWrapMode>().Value;

            //name.verticalOverflow = isExternal ?
            //    RTEditor.GetEditorProperty("Prefab External Name Vertical Wrap").GetConfigEntry<VerticalWrapMode>().Value :
            //    RTEditor.GetEditorProperty("Prefab Internal Name Vertical Wrap").GetConfigEntry<VerticalWrapMode>().Value;

            //name.fontSize = isExternal ?
            //    RTEditor.GetEditorProperty("Prefab External Name Font Size").GetConfigEntry<int>().Value :
            //    RTEditor.GetEditorProperty("Prefab Internal Name Font Size").GetConfigEntry<int>().Value;

            //typeName.horizontalOverflow = isExternal ?
            //    RTEditor.GetEditorProperty("Prefab External Type Horizontal Wrap").GetConfigEntry<HorizontalWrapMode>().Value :
            //    RTEditor.GetEditorProperty("Prefab Internal Type Horizontal Wrap").GetConfigEntry<HorizontalWrapMode>().Value;

            //typeName.verticalOverflow = isExternal ?
            //    RTEditor.GetEditorProperty("Prefab External Type Vertical Wrap").GetConfigEntry<VerticalWrapMode>().Value :
            //    RTEditor.GetEditorProperty("Prefab Internal Type Vertical Wrap").GetConfigEntry<VerticalWrapMode>().Value;

            //typeName.fontSize = isExternal ?
            //    RTEditor.GetEditorProperty("Prefab External Type Font Size").GetConfigEntry<int>().Value :
            //    RTEditor.GetEditorProperty("Prefab Internal Type Font Size").GetConfigEntry<int>().Value;

            //deleteRT.anchoredPosition = isExternal ?
            //    RTEditor.GetEditorProperty("Prefab External Delete Button Pos").GetConfigEntry<Vector2>().Value :
            //    RTEditor.GetEditorProperty("Prefab Internal Delete Button Pos").GetConfigEntry<Vector2>().Value;
            //deleteRT.sizeDelta = isExternal ?
            //    RTEditor.GetEditorProperty("Prefab External Delete Button Sca").GetConfigEntry<Vector2>().Value :
            //    RTEditor.GetEditorProperty("Prefab Internal Delete Button Sca").GetConfigEntry<Vector2>().Value;

            if (!isExternal)
            {
                delete.onClick.AddListener(delegate ()
                {
                    EditorManager.inst.ShowDialog("Warning Popup");
                    RTEditor.inst.RefreshWarningPopup("Are you sure you want to delete this prefab? (This is permanent!)", delegate ()
                    {
                        PrefabEditor.inst.DeleteInternalPrefab(index);
                        EditorManager.inst.HideDialog("Warning Popup");
                    }, delegate ()
                    {
                        EditorManager.inst.HideDialog("Warning Popup");
                    });
                });
                addPrefabObject.onClick.AddListener(delegate ()
                {
                    if (!_toggle)
                    {
                        AddPrefabObjectToLevel(prefab);
                        EditorManager.inst.ClearDialogs(EditorManager.EditorDialog.DialogType.Popup);
                        return;
                    }
                    UpdateCurrentPrefab(prefab);
                    PrefabEditor.inst.ReloadInternalPrefabsInPopup(false);
                });
            }
            else
            {
                delete.onClick.AddListener(delegate ()
                {
                    EditorManager.inst.ShowDialog("Warning Popup");
                    RTEditor.inst.RefreshWarningPopup("Are you sure you want to delete this prefab? (This is permanent!)", delegate ()
                    {
                        PrefabEditor.inst.DeleteExternalPrefab(index);
                        EditorManager.inst.HideDialog("Warning Popup");
                    }, delegate ()
                    {
                        EditorManager.inst.HideDialog("Warning Popup");
                    });
                });
                addPrefabObject.onClick.AddListener(delegate ()
                {
                    if (!ImportPrefabsDirectly)
                    {
                        EditorManager.inst.ShowDialog("Prefab External Dialog");
                        RenderPrefabExternalDialog(prefab);
                    }
                    else
                        ImportPrefabIntoLevel(prefab);
                });
            }

            yield break;
        }

        public bool ContainsName(BasePrefab _p, PrefabDialog _d)
        {
            string str = _d == PrefabDialog.External ?
                string.IsNullOrEmpty(PrefabEditor.inst.externalSearchStr) ? "" : PrefabEditor.inst.externalSearchStr.ToLower() :
                string.IsNullOrEmpty(PrefabEditor.inst.internalSearchStr) ? "" : PrefabEditor.inst.internalSearchStr.ToLower();
            return string.IsNullOrEmpty(str) || _p.Name.ToLower().Contains(str) || (_p.Type < DataManager.inst.PrefabTypes.Count ? DataManager.inst.PrefabTypes[_p.Type] : PrefabType.InvalidType).Name.ToLower().Contains(str);
        }

        public void CollapseCurrentPrefab()
        {
            if (ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
            {
                var bm = ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>();

                if (!bm || bm.prefabInstanceID == "")
                    return;

                var editorData = bm.editorData;
                string prefabInstanceID = bm.prefabInstanceID;
                float startTime = DataManager.inst.gameData.beatmapObjects.Find(x => x.prefabInstanceID == prefabInstanceID).StartTime;

                var prefab = DataManager.inst.gameData.prefabs.Find(x => x.ID == bm.prefabID);

                var prefabObject = new PrefabObject(prefab.ID, startTime);
                prefabObject.editorData.Bin = editorData.Bin;
                prefabObject.editorData.layer = editorData.layer;
                var prefab2 = new Prefab(prefab.Name, prefab.Type, prefab.Offset, DataManager.inst.gameData.beatmapObjects.FindAll(x => x.prefabInstanceID == prefabInstanceID).Select(x => (BeatmapObject)x).ToList(), new List<PrefabObject>());

                prefab2.ID = prefab.ID;
                int index = DataManager.inst.gameData.prefabs.FindIndex(x => x.ID == bm.prefabID);
                DataManager.inst.gameData.prefabs[index] = prefab2;
                var list = RTEditor.inst.TimelineBeatmapObjects.FindAll(x => x.GetData<BeatmapObject>().prefabInstanceID == prefabInstanceID);
                foreach (var timelineObject in list)
                {
                    Destroy(timelineObject.GameObject);
                    var a = RTEditor.inst.timelineObjects.FindIndex(x => x.ID == timelineObject.ID);
                    if (a >= 0)
                        RTEditor.inst.timelineObjects.RemoveAt(a);
                }

                DataManager.inst.gameData.beatmapObjects.Where(x => x.prefabInstanceID == prefabInstanceID && !x.fromPrefab).ToList().ForEach(x => Updater.UpdateProcessor(x, reinsert: false));
                DataManager.inst.gameData.beatmapObjects.RemoveAll(x => x.prefabInstanceID == prefabInstanceID && !x.fromPrefab);
                DataManager.inst.gameData.prefabObjects.Add(prefabObject);

                Updater.AddPrefabToLevel(prefabObject);

                DataManager.inst.gameData.prefabObjects.Where(x => x.prefabID == prefab.ID).ToList().ForEach(x => Updater.UpdatePrefab(x));

                //Updater.UpdatePrefab(prefabObject);

                //ObjectManager.inst.updateObjects(new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Prefab, prefabObject.ID), true);
                //ObjectEditor.inst.RenderTimelineObjects();
                ObjectEditor.inst.SetCurrentObject(new TimelineObject(prefabObject));

                EditorManager.inst.DisplayNotification("Replaced all instances of Prefab!", 2f, EditorManager.NotificationType.Success);
            }
            else
                EditorManager.inst.DisplayNotification("Can't collapse non-object!", 2f, EditorManager.NotificationType.Error);
        }

        public void SetPrefabLayer(float _value)
        {
            if (ObjectEditor.inst.CurrentSelection && ObjectEditor.inst.CurrentSelection.Data != null && ObjectEditor.inst.CurrentSelection.IsPrefabObject)
            {
                ObjectEditor.inst.CurrentSelection.GetData<PrefabObject>().editorData.layer = (int)_value;
                ObjectEditor.inst.RenderTimelineObject(ObjectEditor.inst.CurrentSelection);
            }
            else
                EditorManager.inst.DisplayNotification("Can't edit non-prefab!", 2f, EditorManager.NotificationType.Error);
        }

        public void SetPrefabBin(float _value)
        {
            if (ObjectEditor.inst.CurrentSelection && ObjectEditor.inst.CurrentSelection.Data != null)
            {
                ObjectEditor.inst.CurrentSelection.Bin = (int)_value;
                ObjectEditor.inst.RenderTimelineObject(ObjectEditor.inst.CurrentSelection);
            }
            else
                EditorManager.inst.DisplayNotification("Can't edit non-prefab!", 2f, EditorManager.NotificationType.Error);
        }

        public void SetPrefabOffset(string _value)
        {
            if (ObjectEditor.inst.CurrentSelection && ObjectEditor.inst.CurrentSelection.Data != null)
            {
                string text;
                float offset = DataManager.inst.ParseFloat(_value, out text);
                EditorManager.inst.GetDialog("Prefab Selector").Dialog.Find("data/right/time/time").GetComponent<InputField>().text = text;
                ObjectEditor.inst.CurrentSelection.GetData<PrefabObject>().GetPrefab().Offset = offset;
                int num = 0;
                foreach (var prefabObject in DataManager.inst.gameData.prefabObjects)
                {
                    if (prefabObject.editorData.layer == EditorManager.inst.layer && prefabObject.prefabID == ObjectEditor.inst.CurrentSelection.GetData<PrefabObject>().prefabID)
                    {
                        ObjectEditor.inst.RenderTimelineObject(RTEditor.inst.TimelinePrefabObjects.Find(x => x.ID == prefabObject.ID));
                        Updater.UpdatePrefab(prefabObject);
                    }
                    num++;
                }
            }
            else
                EditorManager.inst.DisplayNotification("Can't edit non-prefab!", 2f, EditorManager.NotificationType.Error);
        }

        public void AddPrefabOffset(float _value)
        {
            if (ObjectEditor.inst.CurrentSelection && ObjectEditor.inst.CurrentSelection.Data != null)
            {
                float num = ObjectEditor.inst.CurrentSelection.GetData<PrefabObject>().GetPrefab().Offset + _value;
                EditorManager.inst.GetDialog("Prefab Selector").Dialog.Find("data/right").Find("time/time").GetComponent<InputField>().text = num.ToString();
            }
            else
                EditorManager.inst.DisplayNotification("Can't edit non-prefab!", 2f, EditorManager.NotificationType.Error, false);
        }

        public void ExpandCurrentPrefab()
        {
            if (ObjectEditor.inst.CurrentSelection.IsPrefabObject)
            {
                var prefabObject = ObjectEditor.inst.CurrentSelection.GetData<PrefabObject>();
                string id = prefabObject.ID;

                EditorManager.inst.ClearDialogs(Array.Empty<EditorManager.EditorDialog.DialogType>());

                StartCoroutine(AddExpandedPrefabToLevel(prefabObject));
                //ObjectManager.inst.terminateObject(ObjEditor.inst.currentObjectSelection);

                //foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects.FindAll(x => x.prefabInstanceID == id))
                //    Updater.UpdateProcessor(beatmapObject, reinsert: false);

                Updater.UpdatePrefab(prefabObject, false);

                RTEditor.inst.RemoveTimelineObject(RTEditor.inst.timelineObjects.Find(x => x.ID == id));

                DataManager.inst.gameData.prefabObjects.RemoveAll(x => x.ID == id);
                DataManager.inst.gameData.beatmapObjects.RemoveAll(x => x.prefabInstanceID == id && x.fromPrefab);
                ObjectEditor.inst.DeselectAllObjects();

                ObjectEditor.inst.RenderTimelineObjects();

                //foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects.FindAll(x => x.prefabInstanceID == id))
                //    ObjectEditor.inst.AddSelectedObject(new TimelineObject((BeatmapObject)beatmapObject));
            }
            else
                EditorManager.inst.DisplayNotification("Can't expand non-prefab!", 2f, EditorManager.NotificationType.Error);
        }

        public void ImportPrefabIntoLevel(BasePrefab _prefab)
        {
            Debug.Log($"{PrefabEditor.inst.className}Adding Prefab: [{_prefab.Name}]");
            var tmpPrefab = Prefab.DeepCopy((Prefab)_prefab);
            int num = DataManager.inst.gameData.prefabs.FindAll(x => Regex.Replace(x.Name, "( +\\[\\d+])", string.Empty) == tmpPrefab.Name).Count();
            if (num > 0)
                tmpPrefab.Name = $"{tmpPrefab.Name} [{num}]";

            DataManager.inst.gameData.prefabs.Add(tmpPrefab);
            PrefabEditor.inst.ReloadInternalPrefabsInPopup();
        }

        public void AddPrefabObjectToLevel(BasePrefab _prefab)
        {
            var prefabObject = new PrefabObject();
            prefabObject.ID = LSText.randomString(16);
            prefabObject.prefabID = _prefab.ID;
            prefabObject.StartTime = EditorManager.inst.CurrentAudioPos;
            prefabObject.editorData.layer = EditorManager.inst.layer;

            if (RTEditor.inst.layerType == RTEditor.LayerType.Events)
                RTEditor.inst.SetLayer(RTEditor.LayerType.Objects);

            for (int i = 0; i < prefabObject.events.Count; i++)
                prefabObject.events[i] = new EventKeyframe(prefabObject.events[i]);

            DataManager.inst.gameData.prefabObjects.Add(prefabObject);

            Updater.AddPrefabToLevel(prefabObject);
            //Updater.UpdatePrefab(prefabObject); < Idk why this isn't working

            ObjectEditor.inst.RenderTimelineObject(new TimelineObject(prefabObject));
        }

        public BeatmapObject Expand(BeatmapObject beatmapObject, Dictionary<string, string> ids, float audioTime,
            BasePrefab prefab, BasePrefabObject prefabObject)
        {
            var beatmapObjectCopy = BeatmapObject.DeepCopy(beatmapObject, false);

            if (ids.ContainsKey(beatmapObject.id))
                beatmapObjectCopy.id = ids[beatmapObject.id];

            if (ids.ContainsKey(beatmapObject.parent))
                beatmapObjectCopy.parent = ids[beatmapObject.parent];
            else if (DataManager.inst.gameData.beatmapObjects.FindIndex(x => x.id == beatmapObject.parent) == -1)
                beatmapObjectCopy.parent = "";

            beatmapObjectCopy.StartTime += prefabObject.StartTime + prefab.Offset;

            beatmapObjectCopy.prefabID = prefab.ID;
            beatmapObjectCopy.prefabInstanceID = prefabObject.ID;

            beatmapObjectCopy.fromPrefab = false;

            beatmapObjectCopy.editorData.layer = RTEditor.inst.Layer;

            return beatmapObjectCopy;
        }

        public IEnumerable<BeatmapObject> ExpandBeatmapObjects(Dictionary<string, string> ids, float audioTime, BasePrefab prefab, BasePrefabObject prefabObject)
        {
            foreach (var beatmapObject in prefab.objects)
            {
                yield return Expand((BeatmapObject)beatmapObject, ids, audioTime, prefab, prefabObject);
            }
        }

        public IEnumerator AddExpandedPrefabToLevel(PrefabObject prefabObject)
        {
            RTEditor.inst.ienumRunning = true;
            float delay = 0f;
            float audioTime = EditorManager.inst.CurrentAudioPos;

            var prefab = (Prefab)DataManager.inst.gameData.prefabs.Find(x => x.ID == prefabObject.prefabID);

            var ids = prefab.objects.ToDictionary(x => x.id, x => LSText.randomString(16));

            EditorManager.inst.ClearDialogs(Array.Empty<EditorManager.EditorDialog.DialogType>());

            //var beatmapObjects = ExpandBeatmapObjects(ids, audioTime, prefab, prefabObject);
            //DataManager.inst.gameData.beatmapObjects.AddRange(beatmapObjects);

            //foreach (var beatmapObject in beatmapObjects)
            //{
            //    Updater.UpdateProcessor(beatmapObject);
            //}

            //yield return StartCoroutine(ObjectEditor.inst.ToTimelineObjects(beatmapObjects, true));

            var expandedObjects = new List<BeatmapObject>();
            foreach (var beatmapObject in prefab.objects)
            {
                yield return new WaitForSeconds(delay);
                var beatmapObjectCopy = BeatmapObject.DeepCopy((BeatmapObject)beatmapObject, false);
                if (ids.ContainsKey(beatmapObject.id))
                    beatmapObjectCopy.id = ids[beatmapObject.id];
                if (ids.ContainsKey(beatmapObject.parent))
                    beatmapObjectCopy.parent = ids[beatmapObject.parent];
                else if (DataManager.inst.gameData.beatmapObjects.FindIndex(x => x.id == beatmapObject.parent) == -1)
                    beatmapObjectCopy.parent = "";

                beatmapObjectCopy.active = false;
                beatmapObjectCopy.fromPrefab = false;
                beatmapObjectCopy.prefabID = prefab.ID;
                beatmapObjectCopy.StartTime += prefabObject.StartTime + prefab.Offset;

                if (EditorManager.inst != null)
                {
                    beatmapObjectCopy.editorData.layer = prefabObject.editorData.layer;
                    beatmapObjectCopy.editorData.Bin = Mathf.Clamp(beatmapObjectCopy.editorData.Bin, 0, 14);
                }

                beatmapObjectCopy.prefabInstanceID = prefabObject.ID;
                DataManager.inst.gameData.beatmapObjects.Add(beatmapObjectCopy);
                expandedObjects.Add(beatmapObjectCopy);

                if (ObjectEditor.inst != null)
                {
                    var timelineObject = new TimelineObject(beatmapObjectCopy);
                    timelineObject.selected = true;
                    ObjectEditor.inst.CurrentSelection = timelineObject;

                    ObjectEditor.inst.RenderTimelineObject(timelineObject);
                }

                delay += 0.0001f;
            }

            foreach (var beatmapObject in expandedObjects)
            {
                Updater.UpdateProcessor(beatmapObject);
            }

            expandedObjects.Clear();
            expandedObjects = null;

            if (prefab.objects.Count > 1 || prefab.prefabObjects.Count > 1)
                EditorManager.inst.ShowDialog("Multi Object Editor", false);
            else if (ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
                ObjectEditor.inst.OpenDialog(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>());
            else if (ObjectEditor.inst.CurrentSelection.IsPrefabObject)
                PrefabEditor.inst.OpenPrefabDialog();

            EditorManager.inst.DisplayNotification("Expanded Prefab Object [" + prefabObject + "].", 1f, EditorManager.NotificationType.Success, false);
            RTEditor.inst.ienumRunning = false;
            yield break;
        }
    }
}
