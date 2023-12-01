using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using SimpleJSON;
using LSFunctions;

using EditorManagement.Functions.Helpers;
using EditorManagement.Functions.Editors;

using RTFunctions.Functions;
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

        public InputField externalSearch;
        public InputField internalSearch;
        public string externalSearchStr;
        public string internalSearchStr;
        public Transform externalContent;
        public Transform internalContent;
        public Transform externalPrefabDialog;
        public Transform internalPrefabDialog;

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

        #endregion

        public static void Init(PrefabEditor prefabEditor) => prefabEditor?.gameObject?.AddComponent<PrefabEditorManager>();

        void Awake()
        {
            inst = this;
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
                ObjectEditor.inst.SelectedBeatmapObjects.Select(x => x.Data).ToList(),
                ObjectEditor.inst.SelectedPrefabObjects.Select(x => x.Data).ToList());

            PrefabEditor.inst.SavePrefab(prefab);
            PrefabEditor.inst.OpenPopup();
            ObjEditor.inst.OpenDialog();
        }

        public void SavePrefab(BasePrefab __0)
        {
            EditorManager.inst.DisplayNotification(string.Format("Saving Prefab to System [{0}]!", __0.Name), 2f, EditorManager.NotificationType.Warning);
            Debug.Log($"{PrefabEditor.inst.className}Saving Prefab to File System!");

            PrefabEditor.inst.LoadedPrefabs.Add(__0);
            PrefabEditor.inst.LoadedPrefabsFiles.Add($"{RTFile.ApplicationDirectory}{RTEditor.prefabListSlash}{__0.Name.ToLower().Replace(" ", "_")}.lsp");
            JSONNode jn;
            if (__0 is Prefab)
                jn = ((Prefab)__0).ToJSON();
            else
                jn = DataManager.inst.GeneratePrefabJSON(__0);

            FileManager.inst.SaveJSONFile(RTEditor.prefabListPath, $"{__0.Name.ToLower().Replace(" ", "_")}.lsp", jn.ToString());
            EditorManager.inst.DisplayNotification($"Saved prefab [{__0.Name}]!", 2f, EditorManager.NotificationType.Success);
        }

        public void DeleteExternalPrefabPrefix(int __0)
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
            ObjectEditor.RenderTimelineObjects();

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

        // Reimplement this in either EditorManagement 2.0.0 or 2.1.0
        public void ReloadSelectionContent()
        {
            LSHelpers.DeleteChildren(PrefabEditor.inst.gridContent, false);
            int num = 0;
            foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
            {
                if (beatmapObject.name.ToLower().Contains(PrefabEditor.inst.gridSearch.text.ToLower()) || string.IsNullOrEmpty(PrefabEditor.inst.gridSearch.text.ToLower()))
                {
                    var tmpGridObj = Instantiate(PrefabEditor.inst.selectionPrefab, Vector3.zero, Quaternion.identity);
                    tmpGridObj.name = "grid";
                    tmpGridObj.transform.SetParent(PrefabEditor.inst.gridContent);
                    tmpGridObj.transform.localScale = Vector3.one;
                    tmpGridObj.transform.Find("text").GetComponent<Text>().text = beatmapObject.name;
                    int tmpIndex = num;

                    if (RTEditor.inst.timelineBeatmapObjects.ContainsKey(beatmapObject.id))
                    {
                        var timelineObject = RTEditor.inst.timelineBeatmapObjects[beatmapObject.id];
                        var toggle = tmpGridObj.GetComponent<Toggle>();
                        toggle.onValueChanged.RemoveAllListeners();
                        toggle.isOn = timelineObject.selected;
                        toggle.onValueChanged.AddListener(delegate (bool _value)
                        {
                            timelineObject.selected = _value;
                        });
                    }
                }
                num++;
            }
        }

        // Redo this
        public void OpenPrefabDialog()
        {
            if (ObjectEditor.inst.CurrentPrefabObjectSelection == null || ObjectEditor.inst.CurrentPrefabObjectSelection.Data == null)
            {
                EditorManager.inst.ClearDialogs(Array.Empty<EditorManager.EditorDialog.DialogType>());
                EditorManager.inst.ShowDialog("Object Editor", false);
                return;
            }
            EditorManager.inst.ClearDialogs(Array.Empty<EditorManager.EditorDialog.DialogType>());
            EditorManager.inst.ShowDialog("Prefab Selector");
            var transform = EditorManager.inst.GetDialog("Prefab Selector").Dialog.Find("data/right");

            transform.Find("time").GetComponent<EventTrigger>().triggers.Clear();

            //var entry = new EventTrigger.Entry();
            //entry.eventID = EventTriggerType.Scroll;
            //entry.callback.AddListener(delegate (BaseEventData eventData)
            //{
            //    var pointerEventData = (PointerEventData)eventData;
            //    if (InputDataManager.inst.editorActions.MultiSelect.IsPressed)
            //    {
            //        if (pointerEventData.scrollDelta.y < 0f)
            //            PrefabEditor.inst.AddPrefabOffset(-1f);
            //        if (pointerEventData.scrollDelta.y > 0f)
            //            PrefabEditor.inst.AddPrefabOffset(1f);
            //    }
            //    else
            //    {
            //        if (pointerEventData.scrollDelta.y < 0f)
            //            PrefabEditor.inst.AddPrefabOffset(-0.1f);
            //        if (pointerEventData.scrollDelta.y > 0f)
            //            PrefabEditor.inst.AddPrefabOffset(0.1f);
            //    }
            //});

            //transform.Find("time").GetComponent<EventTrigger>().triggers.Add(entry);

            transform.Find("time/time").GetComponent<InputField>().onValueChanged.RemoveAllListeners();
            transform.Find("time/time").GetComponent<InputField>().text = ObjEditor.inst.currentObjectSelection.GetPrefabData().Offset.ToString();
            transform.Find("time/time").GetComponent<InputField>().onValueChanged.AddListener(delegate (string _value)
            {
                PrefabEditor.inst.SetPrefabOffset(_value);
            });
            transform = EditorManager.inst.GetDialog("Prefab Selector").Dialog.Find("data/left");
            foreach (object obj in transform.Find("editor/layer").transform)
            {
                ((Transform)obj).GetComponent<Toggle>().interactable = false;
            }
            //transform.Find("editor/layer").GetChild(ObjEditor.inst.currentObjectSelection.GetPrefabObjectData().editorData.Layer).GetComponent<Toggle>().isOn = true;
            foreach (object obj2 in transform.transform.Find("editor/layer").transform)
            {
                ((Transform)obj2).GetComponent<Toggle>().interactable = true;
            }
            transform.Find("editor/bin").GetComponent<Slider>().interactable = false;
            transform.Find("editor/bin").GetComponent<Slider>().value = (float)ObjEditor.inst.currentObjectSelection.GetPrefabObjectData().editorData.Bin;
            transform.Find("editor/bin").GetComponent<Slider>().interactable = true;
        }

        public void OpenDialog()
        {
            EditorManager.inst.ClearDialogs(Array.Empty<EditorManager.EditorDialog.DialogType>());
            EditorManager.inst.ShowDialog("Prefab Editor");
            PrefabEditor.inst.dialog = EditorManager.inst.GetDialog("Prefab Editor").Dialog;
            InputField component = PrefabEditor.inst.dialog.Find("data/name/input").GetComponent<InputField>();
            component.onValueChanged.RemoveAllListeners();
            component.onValueChanged.AddListener(delegate (string _value)
            {
                PrefabEditor.inst.NewPrefabName = _value;
            });
            PrefabEditor.inst.dialog.Find("data/offset/slider").GetComponent<Slider>().onValueChanged.RemoveAllListeners();
            PrefabEditor.inst.dialog.Find("data/offset/slider").GetComponent<Slider>().onValueChanged.AddListener(delegate (float _value)
            {
                PrefabEditor.inst.NewPrefabOffset = Mathf.Round(_value * 100f) / 100f;
                PrefabEditor.inst.dialog.Find("data/offset/input").GetComponent<InputField>().text = PrefabEditor.inst.NewPrefabOffset.ToString();
            });
            PrefabEditor.inst.dialog.Find("data/offset/input").GetComponent<InputField>().onValueChanged.RemoveAllListeners();
            PrefabEditor.inst.dialog.Find("data/offset/input").GetComponent<InputField>().onValueChanged.AddListener(delegate (string _value)
            {
                PrefabEditor.inst.NewPrefabOffset = float.Parse(_value);
                PrefabEditor.inst.dialog.Find("data/offset/slider").GetComponent<Slider>().value = PrefabEditor.inst.NewPrefabOffset;
            });
            PrefabEditor.inst.dialog.Find("data/offset/<").GetComponent<Button>().onClick.RemoveAllListeners();
            PrefabEditor.inst.dialog.Find("data/offset/<").GetComponent<Button>().onClick.AddListener(delegate ()
            {
                PrefabEditor.inst.NewPrefabOffset += 0.1f;
                PrefabEditor.inst.dialog.Find("data/offset/input").GetComponent<InputField>().text = PrefabEditor.inst.NewPrefabOffset.ToString();
                PrefabEditor.inst.dialog.Find("data/offset/slider").GetComponent<Slider>().value = PrefabEditor.inst.NewPrefabOffset;
            });
            PrefabEditor.inst.dialog.Find("data/offset/>").GetComponent<Button>().onClick.RemoveAllListeners();
            PrefabEditor.inst.dialog.Find("data/offset/>").GetComponent<Button>().onClick.AddListener(delegate ()
            {
                PrefabEditor.inst.NewPrefabOffset -= 0.1f;
                PrefabEditor.inst.dialog.Find("data/offset/input").GetComponent<InputField>().text = PrefabEditor.inst.NewPrefabOffset.ToString();
                PrefabEditor.inst.dialog.Find("data/offset/slider").GetComponent<Slider>().value = PrefabEditor.inst.NewPrefabOffset;
            });
            PrefabEditor.inst.dialog.Find("data/type/types/col_" + PrefabEditor.inst.NewPrefabType).GetComponent<Toggle>().isOn = true;
            for (int i = 0; i < DataManager.inst.PrefabTypes.Count; i++)
            {
                int index = i;
                PrefabEditor.inst.dialog.Find("data/type/types/col_" + i + "/text").GetComponent<Text>().text = DataManager.inst.PrefabTypes[i].Name;
                PrefabEditor.inst.dialog.Find("data/type/types/col_" + i + "/Background").GetComponent<Image>().color = DataManager.inst.PrefabTypes[i].Color;
                PrefabEditor.inst.dialog.Find("data/type/types/col_" + i).GetComponent<Toggle>().onValueChanged.RemoveAllListeners();
                PrefabEditor.inst.dialog.Find("data/type/types/col_" + i).GetComponent<Toggle>().onValueChanged.AddListener(delegate (bool val)
                {
                    int tmpIndex = index;
                    PrefabEditor.inst.NewPrefabType = tmpIndex;
                });
            }
            PrefabEditor.inst.dialog.Find("data/type/types/<").GetComponent<Button>().onClick.RemoveAllListeners();
            PrefabEditor.inst.dialog.Find("data/type/types/<").GetComponent<Button>().onClick.AddListener(delegate ()
            {
                PrefabEditor.inst.NewPrefabType--;
                PrefabEditor.inst.NewPrefabType = Mathf.Clamp(PrefabEditor.inst.NewPrefabType, 0, 9);
                PrefabEditor.inst.dialog.Find("data/type/types/col_" + PrefabEditor.inst.NewPrefabType).GetComponent<Toggle>().isOn = true;
            });
            PrefabEditor.inst.dialog.Find("data/type/types/>").GetComponent<Button>().onClick.RemoveAllListeners();
            PrefabEditor.inst.dialog.Find("data/type/types/>").GetComponent<Button>().onClick.AddListener(delegate ()
            {
                PrefabEditor.inst.NewPrefabType++;
                PrefabEditor.inst.NewPrefabType = Mathf.Clamp(PrefabEditor.inst.NewPrefabType, 0, 9);
                PrefabEditor.inst.dialog.Find("data/type/types/col_" + PrefabEditor.inst.NewPrefabType).GetComponent<Toggle>().isOn = true;
            });
        }

        public void UpdateCurrentPrefab(BasePrefab __0)
        {
            PrefabEditor.inst.currentPrefab = __0;

            bool prefabExists = PrefabEditor.inst.currentPrefab != null;

            PrefabEditor.inst.internalPrefabDialog.Find("select_prefab/selected_prefab").GetComponent<Text>().text = (!prefabExists ? "-Select Prefab-" : "<color=#669e37>-Prefab-</color>") + "\n" + (!prefabExists ? "n/a" : PrefabEditor.inst.currentPrefab.Name);
        }

        public IEnumerator InternalPrefabs(bool _toggle = false)
        {
            // Here we add the Example prefab provided to you.
            if (!DataManager.inst.gameData.prefabs.Exists(x => x.ID == "toYoutoYoutoYou") && RTEditor.GetEditorProperty("Prefab Example Template").GetConfigEntry<bool>().Value)
                DataManager.inst.gameData.prefabs.Add(ExamplePrefab.PAExampleM);

            yield return new WaitForSeconds(0.03f);

            LSHelpers.DeleteChildren(internalContent);
            var gameObject = PrefabEditor.inst.CreatePrefab.Duplicate(internalContent, "add new prefab");
            gameObject.GetComponentInChildren<Text>().text = "New Internal Prefab";

            // Currently not reimplemented
            //var hover = gameObject.AddComponent<HoverUI>();
            //hover.animateSca = true;
            //hover.animatePos = false;
            //hover.size = ConfigEntries.PrefabButtonHoverSize.Value;

            gameObject.GetComponentAndPerformAction(delegate (Button x)
            {
                x.NewOnClickListener(delegate ()
                {
                    PrefabEditor.inst.OpenDialog();
                    createInternal = true;
                });
            });

            var list = new List<Coroutine>();

            int num = 0;
            foreach (var prefab in DataManager.inst.gameData.prefabs)
            {
                if (ContainsName(prefab, PrefabDialog.Internal))
                    list.Add(StartCoroutine(CreatePrefabButton(prefab, num, PrefabDialog.Internal, _toggle)));
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
            yield return new WaitForSeconds(0.03f);

            LSHelpers.DeleteChildren(externalContent, false);
            var gameObject = PrefabEditor.inst.CreatePrefab.Duplicate(externalContent, "add new prefab");
            gameObject.GetComponentInChildren<Text>().text = "New External Prefab";

            //var hover = gameObject.AddComponent<HoverUI>();
            //hover.animateSca = true;
            //hover.animatePos = false;
            //hover.size = ConfigEntries.PrefabButtonHoverSize.Value;

            gameObject.GetComponentAndPerformAction(delegate (Button x)
            {
                x.NewOnClickListener(delegate ()
                {
                    PrefabEditor.inst.OpenDialog();
                    createInternal = false;
                });
            });

            var list = new List<Coroutine>();

            int num = 0;
            foreach (var prefab in PrefabEditor.inst.LoadedPrefabs)
            {
                if (ContainsName(prefab, PrefabDialog.External))
                {
                    list.Add(StartCoroutine(CreatePrefabButton(prefab, num, PrefabDialog.External, _toggle)));
                }
                num++;
            }

            yield return StartCoroutine(LSHelpers.WaitForMultipleCoroutines(list, delegate ()
            {
                //foreach (object obj in externalContent)
                //    ((Transform)obj).localScale = Vector3.one;
            }));

            yield break;
        }

        public IEnumerator CreatePrefabButton(BasePrefab _p, int _num, PrefabDialog _d, bool _toggle = false)
        {
            var gameObject = Instantiate(PrefabEditor.inst.AddPrefab, Vector3.zero, Quaternion.identity);
            var tf = gameObject.transform;

            //var hover = gameObject.AddComponent<HoverUI>();
            //hover.animateSca = true;
            //hover.animatePos = false;
            //hover.size = ConfigEntries.PrefabButtonHoverSize.Value;

            var name = tf.Find("name").GetComponent<Text>();
            var typeName = tf.Find("type-name").GetComponent<Text>();
            var color = tf.Find("category").GetComponent<Image>();
            var deleteRT = tf.Find("delete").GetComponent<RectTransform>();
            var addPrefabObject = gameObject.GetComponent<Button>();
            var delete = tf.Find("delete").GetComponent<Button>();

            name.text = _p.Name;
            _p.Type = Mathf.Clamp(_p.Type, 0, DataManager.inst.PrefabTypes.Count - 1);
            typeName.text = DataManager.inst.PrefabTypes[_p.Type].Name;
            color.color = DataManager.inst.PrefabTypes[_p.Type].Color;

            TooltipHelper.AddTooltip(gameObject, "<#" + LSColors.ColorToHex(color.color) + ">" + _p.Name + "</color>", "O: " + _p.Offset + "<br>T: " + typeName.text + "<br>Count: " + _p.objects.Count);

            addPrefabObject.onClick.RemoveAllListeners();
            delete.onClick.RemoveAllListeners();

            bool isExternal = _d == PrefabDialog.External;

            name.horizontalOverflow = isExternal ? ConfigEntries.PrefabEXNameHOverflow.Value : ConfigEntries.PrefabINNameHOverflow.Value;
            name.verticalOverflow = isExternal ? ConfigEntries.PrefabEXNameVOverflow.Value : ConfigEntries.PrefabINNameVOverflow.Value;
            name.fontSize = isExternal ? ConfigEntries.PrefabEXNameFontSize.Value : ConfigEntries.PrefabINNameFontSize.Value;
            typeName.horizontalOverflow = isExternal ? ConfigEntries.PrefabEXTypeHOverflow.Value : ConfigEntries.PrefabINTypeHOverflow.Value;
            typeName.verticalOverflow = isExternal ? ConfigEntries.PrefabEXTypeVOverflow.Value : ConfigEntries.PrefabINTypeVOverflow.Value;
            typeName.fontSize = isExternal ? ConfigEntries.PrefabEXTypeFontSize.Value : ConfigEntries.PrefabINTypeFontSize.Value;


            deleteRT.anchoredPosition = isExternal ? ConfigEntries.PrefabEXLDeletePos.Value : ConfigEntries.PrefabINLDeletePos.Value;
            deleteRT.sizeDelta = isExternal ? ConfigEntries.PrefabEXLDeleteSca.Value : ConfigEntries.PrefabINLDeleteSca.Value;
            gameObject.transform.SetParent(isExternal ? externalContent : internalContent);

            tf.localScale = Vector3.one;

            if (!isExternal)
            {
                delete.onClick.AddListener(delegate ()
                {
                    EditorManager.inst.ShowDialog("Warning Popup");
                    RTEditor.inst.RefreshWarningPopup("Are you sure you want to delete this prefab? (This is permanent!)", delegate ()
                    {
                        PrefabEditor.inst.DeleteInternalPrefab(_num);
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
                        AddPrefabObjectToLevel(_p);
                        EditorManager.inst.ClearDialogs(new EditorManager.EditorDialog.DialogType[1]);
                        return;
                    }
                    UpdateCurrentPrefab(_p);
                    PrefabEditor.inst.ReloadInternalPrefabsInPopup(false);
                });
            }
            if (isExternal)
            {
                delete.onClick.AddListener(delegate ()
                {
                    EditorManager.inst.ShowDialog("Warning Popup");
                    RTEditor.inst.RefreshWarningPopup("Are you sure you want to delete this prefab? (This is permanent!)", delegate ()
                    {
                        PrefabEditor.inst.DeleteExternalPrefab(_num);
                        EditorManager.inst.HideDialog("Warning Popup");
                    }, delegate ()
                    {
                        EditorManager.inst.HideDialog("Warning Popup");
                    });
                });
                addPrefabObject.onClick.AddListener(delegate ()
                {
                    ImportPrefabIntoLevel(_p);
                });
            }

            yield break;
        }

        public bool ContainsName(BasePrefab _p, PrefabDialog _d)
        {
            string str = _d == PrefabDialog.External ? externalSearchStr.ToLower() : internalSearchStr.ToLower();
            return _p.Name.ToLower().Contains(str) || DataManager.inst.PrefabTypes[_p.Type].Name.ToLower().Contains(str) || string.IsNullOrEmpty(str);
        }

        public void CollapseCurrentPrefab()
        {
            if (ObjectEditor.inst.CurrentSelection.prefabInstanceID != "")
            {
                var editorData = ObjectEditor.inst.CurrentSelection.editorData;
                string prefabInstanceID = ObjectEditor.inst.CurrentSelection.prefabInstanceID;
                float startTime = DataManager.inst.gameData.beatmapObjects.Find(x => x.prefabInstanceID == prefabInstanceID).StartTime;

                var prefab = DataManager.inst.gameData.prefabs.Find(x => x.ID == ObjectEditor.inst.CurrentSelection.prefabID);

                var prefabObject = new PrefabObject(prefab.ID, startTime);
                prefabObject.editorData.Bin = editorData.Bin;
                prefabObject.editorData.Layer = editorData.Layer;
                var prefab2 = new Prefab(prefab.Name, prefab.Type, prefab.Offset, DataManager.inst.gameData.beatmapObjects.FindAll(x => x.prefabInstanceID == prefabInstanceID).Select(x => (BeatmapObject)x).ToList(), new List<PrefabObject>());

                prefab2.ID = prefab.ID;
                int index = DataManager.inst.gameData.prefabs.FindIndex(x => x.ID == ObjectEditor.inst.CurrentSelection.prefabID);
                DataManager.inst.gameData.prefabs[index] = prefab2;
                var list = DataManager.inst.gameData.beatmapObjects.FindAll(x => x.prefabInstanceID == prefabInstanceID);
                foreach (var beatmapObject in list)
                {
                    if (RTEditor.inst.timelineBeatmapObjects.ContainsKey(beatmapObject.id))
                        Destroy(RTEditor.inst.timelineBeatmapObjects[beatmapObject.id].GameObject);
                    RTEditor.inst.timelineBeatmapObjects.Remove(beatmapObject.id);
                }

                DataManager.inst.gameData.beatmapObjects.RemoveAll(x => x.prefabInstanceID == prefabInstanceID);
                DataManager.inst.gameData.prefabObjects.Add(prefabObject);

                Updater.UpdatePrefab(prefabObject);

                //ObjectManager.inst.updateObjects(new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Prefab, prefabObject.ID), true);
                ObjectEditor.RenderTimelineObjects();
                if (RTEditor.inst.timelinePrefabObjects.ContainsKey(prefabObject.ID))
                    ObjectEditor.inst.SetCurrentObject(RTEditor.inst.timelinePrefabObjects[prefabObject.ID]);

                EditorManager.inst.DisplayNotification("Replaced all instances of Prefab!", 2f, EditorManager.NotificationType.Success);
            }
            else
                EditorManager.inst.DisplayNotification("Can't collapse non-object!", 2f, EditorManager.NotificationType.Error);
        }

        public void SetPrefabLayer(float _value)
        {
            if (ObjectEditor.inst.CurrentPrefabObjectSelection && ObjectEditor.inst.CurrentPrefabObjectSelection.Data != null)
            {
                ObjectEditor.inst.CurrentPrefabObjectSelection.Data.editorData.Layer = (int)_value;
                ObjectEditor.RenderTimelineObject(ObjectEditor.inst.CurrentPrefabObjectSelection);
            }
            else
                EditorManager.inst.DisplayNotification("Can't edit non-prefab!", 2f, EditorManager.NotificationType.Error);
        }

        public void SetPrefabBin(float _value)
        {
            if (ObjectEditor.inst.CurrentPrefabObjectSelection && ObjectEditor.inst.CurrentPrefabObjectSelection.Data != null)
            {
                ObjectEditor.inst.CurrentPrefabObjectSelection.Data.editorData.Bin = (int)_value;
                ObjectEditor.RenderTimelineObject(ObjectEditor.inst.CurrentPrefabObjectSelection);
            }
            else
                EditorManager.inst.DisplayNotification("Can't edit non-prefab!", 2f, EditorManager.NotificationType.Error);
        }

        public void SetPrefabOffset(string _value)
        {
            if (ObjectEditor.inst.CurrentPrefabObjectSelection && ObjectEditor.inst.CurrentPrefabObjectSelection.Data != null)
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
                        ObjectEditor.RenderTimelineObject(RTEditor.inst.timelinePrefabObjects[prefabObject.ID]);
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
            if (ObjectEditor.inst.CurrentPrefabObjectSelection && ObjectEditor.inst.CurrentPrefabObjectSelection.Data != null)
            {
                float num = ObjectEditor.inst.CurrentPrefabObjectSelection.Data.GetPrefab().Offset + _value;
                EditorManager.inst.GetDialog("Prefab Selector").Dialog.Find("data/right").Find("time/time").GetComponent<InputField>().text = num.ToString();
            }
            else
                EditorManager.inst.DisplayNotification("Can't edit non-prefab!", 2f, EditorManager.NotificationType.Error, false);
        }

        public void ExpandCurrentPrefab()
        {
            if (ObjectEditor.inst.CurrentPrefabObjectSelection && ObjectEditor.inst.CurrentPrefabObjectSelection.Data != null)
            {
                var prefabObject = ObjectEditor.inst.CurrentPrefabObjectSelection.Data;
                string id = prefabObject.ID;
                ObjectManager.inst.AddExpandedPrefabToLevel(prefabObject);
                //ObjectManager.inst.terminateObject(ObjEditor.inst.currentObjectSelection);

                //foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects.FindAll(x => x.prefabInstanceID == id))
                //    Updater.UpdateProcessor(beatmapObject, reinsert: false);

                Updater.UpdatePrefab(prefabObject, false);

                Destroy(RTEditor.inst.timelinePrefabObjects[id].GameObject);
                RTEditor.inst.timelinePrefabObjects.Remove(id);

                DataManager.inst.gameData.prefabObjects.RemoveAll(x => x.ID == id);
                DataManager.inst.gameData.beatmapObjects.RemoveAll(x => x.prefabInstanceID == id && x.fromPrefab);
                ObjectEditor.inst.DeselectAllObjects();

                ObjectEditor.RenderTimelineObjects();

                foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects.FindAll(x => x.prefabInstanceID == id))
                    ObjectEditor.inst.AddSelectedObject(new TimelineObject<BeatmapObject>((BeatmapObject)beatmapObject));
            }
            else
                EditorManager.inst.DisplayNotification("Can't expand non-prefab!", 2f, EditorManager.NotificationType.Error);
        }

        public void ImportPrefabIntoLevel(BasePrefab _prefab)
        {
            Debug.Log($"{PrefabEditor.inst.className}Adding Prefab: [{_prefab.Name}]");
            var tmpPrefab = Prefab.DeepCopy((Prefab)_prefab, true);
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
            prefabObject.editorData.Layer = EditorManager.inst.layer;
            DataManager.inst.gameData.prefabObjects.Add(prefabObject);

            Updater.UpdatePrefab(prefabObject);

            ObjectEditor.RenderTimelineObject(new TimelineObject<PrefabObject>(prefabObject));
        }
    }
}
