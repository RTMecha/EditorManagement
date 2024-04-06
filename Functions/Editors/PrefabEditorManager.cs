using EditorManagement.Functions.Helpers;
using LSFunctions;
using RTFunctions.Functions;
using RTFunctions.Functions.Components;
using RTFunctions.Functions.Data;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;
using RTFunctions.Functions.Optimization;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BasePrefab = DataManager.GameData.Prefab;

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

        void Awake() => inst = this;

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
            prefabTypeTogglePrefab.transform.SetParent(transform);

            CreatePrefabTypesPopup();
            CreatePrefabExternalDialog();

            if (RTFile.DirectoryExists(RTFile.ApplicationDirectory + "beatmaps/prefabtypes/"))
                StartCoroutine(LoadPrefabTypes());

        }

        #region Prefab Objects

        public void UpdateModdedVisbility()
        {
            var prefabSelectorLeft = EditorManager.inst.GetDialog("Prefab Selector").Dialog.Find("data/left");

            if (!prefabSelectorLeft.gameObject.activeInHierarchy)
                return;

            prefabSelectorLeft.Find("tod-dropdown label").gameObject.SetActive(RTEditor.ShowModdedUI);
            prefabSelectorLeft.Find("tod-dropdown").gameObject.SetActive(RTEditor.ShowModdedUI);
            prefabSelectorLeft.Find("akoffset").gameObject.SetActive(RTEditor.ShowModdedUI);
            prefabSelectorLeft.Find("repeat label").gameObject.SetActive(RTEditor.ShowModdedUI);
            prefabSelectorLeft.Find("repeat").gameObject.SetActive(RTEditor.ShowModdedUI);
            prefabSelectorLeft.Find("speed label").gameObject.SetActive(RTEditor.ShowModdedUI);
            prefabSelectorLeft.Find("speed").gameObject.SetActive(RTEditor.ShowModdedUI);
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
                            }
                            Updater.UpdatePrefab(prefabObject, "Start Time");
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

            UpdateModdedVisbility();

            #region My Code

            if (RTEditor.ShowModdedUI)
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
                            if (currentPrefab.autoKillType != PrefabObject.AutoKillType.Regular)
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
            }

            prefabSelectorLeft.Find("time").GetComponentAndPerformAction(delegate (InputField inputField)
            {
                var locked = inputField.transform.Find("lock").GetComponent<Toggle>();
                locked.onValueChanged.ClearAll();
                locked.isOn = currentPrefab.editorData.locked;
                locked.onValueChanged.AddListener(delegate (bool _val)
                {
                    currentPrefab.editorData.locked = _val;
                    ObjectEditor.inst.RenderTimelineObject(new TimelineObject(currentPrefab));
                });

                var collapse = inputField.transform.Find("collapse").GetComponent<Toggle>();
                collapse.onValueChanged.ClearAll();
                collapse.isOn = currentPrefab.editorData.collapse;
                collapse.onValueChanged.AddListener(delegate (bool _val)
                {
                    currentPrefab.editorData.collapse = _val;
                    ObjectEditor.inst.RenderTimelineObject(new TimelineObject(currentPrefab));
                });

                inputField.NewValueChangedListener(currentPrefab.StartTime.ToString(), delegate (string _val)
                {
                    if (!currentPrefab.editorData.locked)
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
                    }
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

            if (RTEditor.ShowModdedUI)
            {
                prefabSelectorLeft.Find("repeat/x").GetComponentAndPerformAction(delegate (InputField inputField)
                {
                    inputField.characterValidation = InputField.CharacterValidation.Integer;
                    inputField.contentType = InputField.ContentType.Standard;
                    inputField.characterLimit = 5;
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
                    inputField.characterValidation = InputField.CharacterValidation.Decimal;
                    inputField.contentType = InputField.ContentType.Standard;
                    inputField.characterLimit = 0;
                    inputField.NewValueChangedListener(Mathf.Clamp(currentPrefab.RepeatOffsetTime, 0f, 60f).ToString(), delegate (string _val)
                    {
                        if (float.TryParse(_val, out float num))
                        {
                            num = Mathf.Clamp(num, 0f, 60f);
                            currentPrefab.RepeatOffsetTime = num;
                            Updater.UpdatePrefab(currentPrefab, "Start Time");
                        }
                    });

                    TriggerHelper.IncreaseDecreaseButtons(inputField, max: 60f);
                    TriggerHelper.AddEventTriggerParams(inputField.gameObject, TriggerHelper.ScrollDelta(inputField, max: 60f));
                });

                prefabSelectorLeft.Find("speed").GetComponentAndPerformAction(delegate (InputField inputField)
                {
                    inputField.characterValidation = InputField.CharacterValidation.Decimal;
                    inputField.contentType = InputField.ContentType.Standard;
                    inputField.characterLimit = 0;
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
            }

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
                prefabObjectTimelineCount.text = "Prefab Object (Timeline) Count: " + DataManager.inst.gameData.prefabObjects.FindAll(x => x.prefabID == prefab.ID).Count;
            }

            #endregion
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

                ObjectEditor.inst.SetCurrentObject(new TimelineObject(prefabObject));

                EditorManager.inst.DisplayNotification("Replaced all instances of Prefab!", 2f, EditorManager.NotificationType.Success);
            }
            else
                EditorManager.inst.DisplayNotification("Can't collapse non-object!", 2f, EditorManager.NotificationType.Error);
        }

        public void ExpandCurrentPrefab()
        {
            if (ObjectEditor.inst.CurrentSelection.IsPrefabObject)
            {
                var prefabObject = ObjectEditor.inst.CurrentSelection.GetData<PrefabObject>();
                string id = prefabObject.ID;

                EditorManager.inst.ClearDialogs();

                Debug.Log($"{PrefabEditor.inst.className}Expanding Prefab Object.");
                StartCoroutine(AddExpandedPrefabToLevel(prefabObject));

                Debug.Log($"{PrefabEditor.inst.className}Removing Prefab Object's spawned objects.");
                Updater.UpdatePrefab(prefabObject, false);

                RTEditor.inst.RemoveTimelineObject(RTEditor.inst.timelineObjects.Find(x => x.ID == id));

                DataManager.inst.gameData.prefabObjects.RemoveAll(x => x.ID == id);
                DataManager.inst.gameData.beatmapObjects.RemoveAll(x => x.prefabInstanceID == id && x.fromPrefab);
                ObjectEditor.inst.DeselectAllObjects();

                ObjectEditor.inst.RenderTimelineObjects();
            }
            else
                EditorManager.inst.DisplayNotification("Can't expand non-prefab!", 2f, EditorManager.NotificationType.Error);
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

            ObjectEditor.inst.RenderTimelineObject(new TimelineObject(prefabObject));
        }

        public IEnumerator AddExpandedPrefabToLevel(PrefabObject prefabObject)
        {
            RTEditor.inst.ienumRunning = true;
            float delay = 0f;
            float audioTime = EditorManager.inst.CurrentAudioPos;

            var prefab = (Prefab)DataManager.inst.gameData.prefabs.Find(x => x.ID == prefabObject.prefabID);

            var ids = prefab.objects.ToDictionary(x => x.id, x => LSText.randomString(16));

            EditorManager.inst.ClearDialogs();

            var expandedObjects = new List<BeatmapObject>();
            foreach (var beatmapObject in prefab.objects)
            {
                yield return new WaitForSeconds(delay);
                var beatmapObjectCopy = BeatmapObject.DeepCopy((BeatmapObject)beatmapObject, false);
                if (ids.ContainsKey(beatmapObject.id))
                    beatmapObjectCopy.id = ids[beatmapObject.id];
                if (ids.ContainsKey(beatmapObject.parent))
                    beatmapObjectCopy.parent = ids[beatmapObject.parent];
                else if (DataManager.inst.gameData.beatmapObjects.FindIndex(x => x.id == beatmapObject.parent) == -1 && beatmapObjectCopy.parent != "CAMERA_PARENT")
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

                if (!AssetManager.SpriteAssets.ContainsKey(beatmapObject.text) && prefab.SpriteAssets.ContainsKey(beatmapObject.text))
                {
                    AssetManager.SpriteAssets.Add(beatmapObject.text, prefab.SpriteAssets[beatmapObject.text]);
                }

                beatmapObjectCopy.prefabInstanceID = prefabObject.ID;
                DataManager.inst.gameData.beatmapObjects.Add(beatmapObjectCopy);
                if (Updater.levelProcessor && Updater.levelProcessor.converter != null && !Updater.levelProcessor.converter.beatmapObjects.ContainsKey(beatmapObjectCopy.id))
                    Updater.levelProcessor.converter.beatmapObjects.Add(beatmapObjectCopy.id, beatmapObjectCopy);

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

        #endregion

        #region Prefab Types

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

            var nameGO = RTEditor.inst.defaultIF.Duplicate(rectTransform, "Name");
            nameGO.transform.localScale = Vector3.one;
            var nameRT = nameGO.GetComponent<RectTransform>();
            nameRT.sizeDelta = new Vector2(132f, 32f);

            var nameTextRT = (RectTransform)nameRT.Find("Text");
            nameTextRT.anchoredPosition = new Vector2(0f, 0f);
            nameTextRT.sizeDelta = new Vector2(0f, 0f);

            nameTextRT.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;

            var colorGO = RTEditor.inst.defaultIF.Duplicate(rectTransform, "Color");
            colorGO.transform.localScale = Vector3.one;
            var colorRT = colorGO.GetComponent<RectTransform>();
            colorRT.sizeDelta = new Vector2(90f, 32f);

            var colorTextRT = (RectTransform)colorRT.Find("Text");
            colorTextRT.anchoredPosition = new Vector2(0f, 0f);
            colorTextRT.sizeDelta = new Vector2(0f, 0f);

            colorTextRT.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;

            var delete = EditorManager.inst.GetDialog("Quick Actions Popup").Dialog.Find("Panel/x").gameObject.Duplicate(rectTransform, "Delete");
            delete.transform.localScale = Vector3.one;
            ((RectTransform)delete.transform).anchoredPosition = Vector2.zero;

            var button = GameObject.Find("TimelineBar/GameObject/event");

            var setIcon = button.Duplicate(rectTransform, "Set Icon");
            var text = setIcon.transform.GetChild(0).GetComponent<Text>();
            text.text = "Set Icon";
            setIcon.transform.GetComponent<Image>().color = new Color(0.3922f, 0.7098f, 0.9647f, 1f);
            ((RectTransform)setIcon.transform).sizeDelta = new Vector2(95f, 32f);

            Destroy(setIcon.GetComponent<LayoutElement>());

            EditorHelper.AddEditorPopup("Prefab Types Popup", gameObject);

            EditorHelper.AddEditorDropdown("View Prefab Types", "", "View", RTEditor.inst.SearchSprite, delegate ()
            {
                OpenPrefabTypePopup(PrefabEditor.inst.NewPrefabType, delegate (int index)
                {
                    PrefabEditor.inst.NewPrefabType = index;
                    if (PrefabEditor.inst.dialog)
                        PrefabEditor.inst.dialog.Find("data/type/Show Type Editor").GetComponent<Image>().color =
                            DataManager.inst.PrefabTypes[Mathf.Clamp(PrefabEditor.inst.NewPrefabType, 0, DataManager.inst.PrefabTypes.Count - 1)].Color;
                });
            });
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

        public static bool loadingPrefabTypes = false;
        public IEnumerator LoadPrefabTypes()
        {
            loadingPrefabTypes = true;
            DataManager.inst.PrefabTypes.Clear();

            var directories = Directory.GetDirectories(RTFile.ApplicationDirectory + "beatmaps/prefabtypes");
            var list = new List<DataManager.PrefabType>();
            foreach (var folder in directories)
            {
                var fileName = Path.GetFileName(folder);
                var jn = JSON.Parse(RTFile.ReadFromFile(folder + "/data.lsp"));
                var prefabType = PrefabType.Parse(jn);

                if (RTFile.FileExists(folder + "/icon.png"))
                    prefabType.Icon = SpriteManager.LoadSprite(folder + "/icon.png");

                prefabType.Index = jn["index"].AsInt;

                list.Add(prefabType);
            }

            list = list.OrderBy(x => (x as PrefabType).Index).ToList();

            DataManager.inst.PrefabTypes.AddRange(list);

            loadingPrefabTypes = false;

            yield break;
        }

        public void OpenPrefabTypePopup(int current, Action<int> onSelect)
        {
            EditorManager.inst.ShowDialog("Prefab Types Popup");
            RenderPrefabTypesPopup(current, onSelect);
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

        public void RenderPrefabTypesPopup(int current, Action<int> onSelect)
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

                RenderPrefabTypesPopup(current, onSelect);
            });

            int num = 0;
            foreach (var prefabType in DataManager.inst.PrefabTypes.Select(x => x as PrefabType))
            {
                int index = num;
                var gameObject = prefabTypePrefab.Duplicate(prefabTypeContent, prefabType.Name);

                var toggle = gameObject.transform.Find("Toggle").GetComponent<Toggle>();
                toggle.onValueChanged.ClearAll();
                toggle.isOn = current == index;
                toggle.onValueChanged.AddListener(delegate (bool _val)
                {
                    onSelect?.Invoke(index);
                    RenderPrefabTypesPopup(index, onSelect);
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

                    if (RTFile.DirectoryExists(RTFile.ApplicationDirectory + "beatmaps/prefabtypes/" + oldName))
                    {
                        foreach (var file in Directory.GetFiles(RTFile.ApplicationDirectory + "beatmaps/prefabtypes/" + oldName, "*", SearchOption.AllDirectories))
                        {
                            File.Delete(file);
                        }
                        
                        foreach (var directory in Directory.GetDirectories(RTFile.ApplicationDirectory + "beatmaps/prefabtypes/" + oldName, "*", SearchOption.AllDirectories))
                        {
                            Directory.Delete(directory);
                        }

                        if (RTFile.DirectoryExists(RTFile.ApplicationDirectory + "beatmaps/prefabtypes/" + oldName))
                            Directory.Delete(RTFile.ApplicationDirectory + "beatmaps/prefabtypes/" + oldName);
                    }
                });
                inputField.onEndEdit.ClearAll();
                inputField.onEndEdit.AddListener(delegate (string _val)
                {
                    SavePrefabTypes();
                    RenderPrefabTypesPopup(index, onSelect);
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
                    RenderPrefabTypesPopup(index, onSelect);
                    SavePrefabTypes();
                });

                var delete = gameObject.transform.Find("Delete").GetComponent<Button>();
                delete.onClick.ClearAll();
                delete.onClick.AddListener(delegate ()
                {
                    var path = RTFile.ApplicationDirectory + "beatmaps/prefabtypes/" + prefabType.Name;

                    if (RTFile.DirectoryExists(path))
                    {
                        foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                        {
                            File.Delete(file);
                        }

                        foreach (var directory in Directory.GetDirectories(path, "*", SearchOption.AllDirectories))
                        {
                            Directory.Delete(directory);
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

                    RenderPrefabTypesPopup(index, onSelect);
                    SavePrefabTypes();
                });

                var setImage = gameObject.transform.Find("Set Icon").GetComponent<Button>();
                setImage.onClick.ClearAll();
                setImage.onClick.AddListener(delegate ()
                {
                    EditorManager.inst.ShowDialog("Browser Popup");
                    RTFileBrowser.inst.UpdateBrowser(Directory.GetCurrentDirectory(), new string[] { ".png" }, onSelectFile: delegate (string _val)
                    {
                        var copyTo = _val.Replace("\\", "/").Replace(Path.GetDirectoryName(_val).Replace("\\", "/"), RTFile.ApplicationDirectory + "beatmaps/prefabtypes/" + prefabType.Name).Replace(Path.GetFileName(_val), "icon.png");

                        File.Copy(_val, copyTo, RTFile.FileExists(copyTo));

                        prefabType.Icon = SpriteManager.LoadSprite(copyTo);
                        icon.sprite = prefabType.Icon;

                        EditorManager.inst.HideDialog("Browser Popup");
                    });
                });

                num++;
            }
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

        #endregion

        #region Prefabs
        
        public Button externalType;
        public Image extenalTypeImage;

        public Button importPrefab;
        public Button exportToVG;

        public InputField externalDescriptionField;

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

            var labelTypeBase = new GameObject("Type Label");
            labelTypeBase.transform.SetParent(editorDialogTransform);
            labelTypeBase.transform.localScale = Vector3.one;

            var labelTypeBaseRT = labelTypeBase.AddComponent<RectTransform>();
            labelTypeBaseRT.sizeDelta = new Vector2(765f, 32f);

            var labelType = editorDialogTransform.GetChild(2);
            labelType.SetParent(labelTypeBaseRT);
            labelType.localPosition = Vector3.zero;
            labelType.localScale = Vector3.one;
            labelType.AsRT().sizeDelta = new Vector2(725f, 32f);
            var labelTypeText = labelType.GetComponent<Text>();
            labelTypeText.text = "Type";
            labelTypeText.alignment = TextAnchor.UpperLeft;

            var prefabTypeBase = new GameObject("Prefab Type Base");
            prefabTypeBase.transform.SetParent(editorDialogTransform);
            prefabTypeBase.transform.localScale = Vector3.one;

            var prefabTypeBaseRT = prefabTypeBase.AddComponent<RectTransform>();
            prefabTypeBaseRT.sizeDelta = new Vector2(765f, 32f);

            var prefabEditorData = EditorManager.inst.GetDialog("Prefab Editor").Dialog.Find("data/type/Show Type Editor");

            var prefabType = prefabEditorData.gameObject.Duplicate(prefabTypeBaseRT, "Show Type Editor");

            prefabType.transform.AsRT().anchoredPosition = new Vector2(-250f, 0f);
            prefabType.transform.AsRT().anchorMax = new Vector2(0.5f, 0.5f);
            prefabType.transform.AsRT().anchorMin = new Vector2(0.5f, 0.5f);
            prefabType.transform.AsRT().pivot = new Vector2(0.5f, 0.5f);
            prefabType.transform.AsRT().sizeDelta = new Vector2(232f, 34f);
            prefabType.transform.Find("Text").GetComponent<Text>().text = "Open Prefab Type Editor";
            externalType = prefabType.GetComponent<Button>();
            extenalTypeImage = prefabType.GetComponent<Image>();

            // Spacer
            {
                var spacer = new GameObject("spacer2");
                spacer.transform.SetParent(editorDialogTransform);
                spacer.transform.localScale = Vector3.one;

                var spacerRT = spacer.AddComponent<RectTransform>();
                spacerRT.sizeDelta = new Vector2(765f, 24f);
            }

            var labelDescriptionBase = new GameObject("Description Label");
            labelDescriptionBase.transform.SetParent(editorDialogTransform);
            labelDescriptionBase.transform.localScale = Vector3.one;

            var labelDescriptionBaseRT = labelDescriptionBase.AddComponent<RectTransform>();
            labelDescriptionBaseRT.sizeDelta = new Vector2(765f, 32f);

            var labelDescription = labelType.gameObject.Duplicate(labelDescriptionBaseRT);
            labelDescription.transform.localPosition = Vector3.zero;
            labelDescription.transform.localScale = Vector3.one;
            labelDescription.transform.AsRT().sizeDelta = new Vector2(725f, 32f);
            var labelDescriptionText = labelDescription.GetComponent<Text>();
            labelDescriptionText.text = "Description";
            labelDescriptionText.alignment = TextAnchor.UpperLeft;

            var textBase1 = new GameObject("Text Base 1");
            textBase1.transform.SetParent(editorDialogTransform);
            textBase1.transform.localScale = Vector3.one;

            var textBase1RT = textBase1.AddComponent<RectTransform>();
            textBase1RT.sizeDelta = new Vector2(765f, 300f);
            
            var description = RTEditor.inst.defaultIF.Duplicate(textBase1RT);
            description.transform.AsRT().anchoredPosition = Vector2.zero;
            description.transform.AsRT().anchorMax = new Vector2(0.5f, 0.5f);
            description.transform.AsRT().anchorMin = new Vector2(0.5f, 0.5f);
            description.transform.AsRT().sizeDelta = new Vector2(740f, 300f);

            externalDescriptionField = description.GetComponent<InputField>();
            externalDescriptionField.lineType = InputField.LineType.MultiLineNewline;
            ((Text)externalDescriptionField.placeholder).text = "Set description...";
            ((Text)externalDescriptionField.placeholder).color = new Color(0.1961f, 0.1961f, 0.1961f, 0.5f);

            // Spacer
            {
                var spacer = new GameObject("spacer3");
                spacer.transform.SetParent(editorDialogTransform);
                spacer.transform.localScale = Vector3.one;

                var spacerRT = spacer.AddComponent<RectTransform>();
                spacerRT.sizeDelta = new Vector2(765f, 200f);
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
            exportToVG.transform.GetChild(0).GetComponent<Text>().text = "Convert to VG Format";

            EditorHelper.AddEditorDialog("Prefab External Dialog", editorDialogObject);
        }

        public void RenderPrefabExternalDialog(PrefabPanel prefabPanel)
        {
            var prefab = prefabPanel.Prefab;

            extenalTypeImage.color = prefab.Type < DataManager.inst.PrefabTypes.Count ? DataManager.inst.PrefabTypes[prefab.Type].Color : PrefabType.InvalidType.Color;
            externalType.onClick.ClearAll();
            externalType.onClick.AddListener(delegate ()
            {
                OpenPrefabTypePopup(prefab.Type, delegate (int index)
                {
                    prefab.Type = index;
                    var prefabType = prefab.Type < DataManager.inst.PrefabTypes.Count ? (PrefabType)DataManager.inst.PrefabTypes[prefab.Type] : PrefabType.InvalidType;
                    var color = prefabType.Color;
                    extenalTypeImage.color = color;

                    prefabPanel.TypeImage.color = color;
                    prefabPanel.TypeIcon.sprite = prefabType.Icon;
                    prefabPanel.TypeText.text = prefabType.Name;

                    if (!string.IsNullOrEmpty(prefab.filePath))
                        RTFile.WriteToFile(prefab.filePath, prefab.ToJSON().ToString());
                });
            });

            importPrefab.onClick.ClearAll();
            importPrefab.onClick.AddListener(delegate ()
            {
                ImportPrefabIntoLevel(prefab);
            });

            exportToVG.onClick.ClearAll();
            exportToVG.onClick.AddListener(delegate ()
            {
                var exportPath = EditorConfig.Instance.ConvertPrefabLSToVGExportPath.Value;

                if (string.IsNullOrEmpty(exportPath))
                {
                    if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + "beatmaps/exports"))
                        Directory.CreateDirectory(RTFile.ApplicationDirectory + "beatmaps/exports");
                    exportPath = RTFile.ApplicationDirectory + "beatmaps/exports/";
                }

                if (!string.IsNullOrEmpty(exportPath) && exportPath[exportPath.Length - 1] != '/')
                    exportPath += "/";

                if (!RTFile.DirectoryExists(Path.GetDirectoryName(exportPath)))
                {
                    EditorManager.inst.DisplayNotification("Directory does not exist.", 2f, EditorManager.NotificationType.Error);
                    return;
                }

                var vgjn = prefab.ToJSONVG();

                RTFile.WriteToFile($"{exportPath}{prefab.Name.ToLower()}.vgp", vgjn.ToString());

                EditorManager.inst.DisplayNotification($"Converted Prefab {prefab.Name.ToLower()}.lsp from LS format to VG format and saved to {prefab.Name.ToLower()}.vgp!", 4f, EditorManager.NotificationType.Success);
            });

            externalDescriptionField.onValueChanged.ClearAll();
            externalDescriptionField.onEndEdit.ClearAll();
            externalDescriptionField.text = prefab.description;
            externalDescriptionField.onValueChanged.AddListener(delegate (string _val)
            {
                prefab.description = _val;
            });
            externalDescriptionField.onEndEdit.AddListener(delegate (string _val)
            {
                if (!string.IsNullOrEmpty(prefab.filePath))
                    RTFile.WriteToFile(prefab.filePath, prefab.ToJSON().ToString());
            });
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

            foreach (var beatmapObject in prefab.objects)
            {
                if (!prefab.SpriteAssets.ContainsKey(beatmapObject.text) && AssetManager.SpriteAssets.ContainsKey(beatmapObject.text))
                {
                    prefab.SpriteAssets.Add(beatmapObject.text, AssetManager.SpriteAssets[beatmapObject.text]);
                }
            }

            if (createInternal)
                ImportPrefabIntoLevel(prefab);
            else
                SavePrefab(prefab);

            PrefabEditor.inst.OpenPopup();
            ObjEditor.inst.OpenDialog();
        }

        public void SavePrefab(Prefab prefab)
        {
            RTEditor.inst.canUpdatePrefabs = false;
            RTEditor.inst.PrefabWatcher.EnableRaisingEvents = false;

            EditorManager.inst.DisplayNotification($"Saving Prefab to System [{prefab.Name}]!", 2f, EditorManager.NotificationType.Warning);
            Debug.Log($"{PrefabEditor.inst.className}Saving Prefab to File System!");

            prefab.objects.ForEach(x => { x.prefabID = ""; x.prefabInstanceID = ""; });
            int count = PrefabEditor.inst.LoadedPrefabs.Count;
            var file = $"{RTFile.ApplicationDirectory}{RTEditor.prefabListSlash}{prefab.Name.ToLower().Replace(" ", "_")}.lsp";
            prefab.filePath = file;
            PrefabEditor.inst.LoadedPrefabs.Add(prefab);
            PrefabEditor.inst.LoadedPrefabsFiles.Add(file);

            var config = EditorConfig.Instance;

            if (config.UpdatePrefabListOnFilesChanged.Value)
                return;

            var hoverSize = config.PrefabButtonHoverSize.Value;

            var nameHorizontalOverflow = config.PrefabExternalNameHorizontalWrap.Value;

            var nameVerticalOverflow = config.PrefabExternalNameVerticalWrap.Value;

            var nameFontSize = config.PrefabExternalNameFontSize.Value;

            var typeHorizontalOverflow = config.PrefabExternalTypeHorizontalWrap.Value;

            var typeVerticalOverflow = config.PrefabExternalTypeVerticalWrap.Value;

            var typeFontSize = config.PrefabExternalTypeFontSize.Value;

            var deleteAnchoredPosition = config.PrefabExternalDeleteButtonPos.Value;
            var deleteSizeDelta = config.PrefabExternalDeleteButtonSca.Value;

            StartCoroutine(CreatePrefabButton(prefab, count, PrefabDialog.External, $"{RTFile.ApplicationDirectory}{RTEditor.prefabListSlash}{prefab.Name.ToLower().Replace(" ", "_")}.lsp",
                false, hoverSize, nameHorizontalOverflow, nameVerticalOverflow, nameFontSize,
                typeHorizontalOverflow, typeVerticalOverflow, typeFontSize, deleteAnchoredPosition, deleteSizeDelta));

            RTFile.WriteToFile(file, prefab.ToJSON().ToString());
            //FileManager.inst.SaveJSONFile(RTEditor.prefabListPath, $"{prefab.Name.ToLower().Replace(" ", "_")}.lsp", prefab.ToJSON().ToString());
            EditorManager.inst.DisplayNotification($"Saved prefab [{prefab.Name}]!", 2f, EditorManager.NotificationType.Success);

            if (RTFile.DirectoryExists(RTFile.ApplicationDirectory + RTEditor.prefabListPath))
            {
                RTEditor.inst.PrefabWatcher.Path = RTFile.ApplicationDirectory + RTEditor.prefabListPath;
                RTEditor.inst.PrefabWatcher.EnableRaisingEvents = true;
            }

            RTEditor.inst.canUpdatePrefabs = true;
        }

        public void DeleteExternalPrefab(PrefabPanel prefabPanel)
        {
            RTEditor.inst.canUpdatePrefabs = false;
            RTEditor.inst.PrefabWatcher.EnableRaisingEvents = false;

            if (RTFile.FileExists(prefabPanel.FilePath))
                FileManager.inst.DeleteFileRaw(prefabPanel.FilePath);

            PrefabEditor.inst.LoadedPrefabs.RemoveAt(prefabPanel.Index);
            PrefabEditor.inst.LoadedPrefabsFiles.RemoveAt(prefabPanel.Index);

            Destroy(prefabPanel.GameObject);
            PrefabPanels.RemoveAt(prefabPanel.Index);

            int num = 0;
            foreach (var p in PrefabPanels)
            {
                p.Index = num;
                num++;
            }

            if (RTFile.DirectoryExists(RTFile.ApplicationDirectory + RTEditor.prefabListPath))
            {
                RTEditor.inst.PrefabWatcher.Path = RTFile.ApplicationDirectory + RTEditor.prefabListPath;
                RTEditor.inst.PrefabWatcher.EnableRaisingEvents = true;
            }

            RTEditor.inst.canUpdatePrefabs = true;
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
            EditorManager.inst.ClearDialogs();
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
        }

        public void UpdateCurrentPrefab(BasePrefab __0)
        {
            PrefabEditor.inst.currentPrefab = __0;

            bool prefabExists = PrefabEditor.inst.currentPrefab != null;

            PrefabEditor.inst.internalPrefabDialog.Find("select_prefab/selected_prefab").GetComponent<Text>().text = (!prefabExists ? "-Select Prefab-" : "<color=#669e37>-Prefab-</color>") + "\n" + (!prefabExists ? "n/a" : PrefabEditor.inst.currentPrefab.Name);
        }

        public IEnumerator InternalPrefabs(bool _toggle = false)
        {
            var config = EditorConfig.Instance;

            // Here we add the Example prefab provided to you.
            if (!DataManager.inst.gameData.prefabs.Exists(x => x.ID == "toYoutoYoutoYou") && config.PrefabExampleTemplate.Value)
                DataManager.inst.gameData.prefabs.Add(Prefab.DeepCopy(ExamplePrefab.PAExampleM, false));

            yield return new WaitForSeconds(0.03f);

            LSHelpers.DeleteChildren(PrefabEditor.inst.internalContent);
            var gameObject = PrefabEditor.inst.CreatePrefab.Duplicate(PrefabEditor.inst.internalContent, "add new prefab");
            gameObject.GetComponentInChildren<Text>().text = "New Internal Prefab";

            var hoverSize = config.PrefabButtonHoverSize.Value;

            var hover = gameObject.AddComponent<HoverUI>();
            hover.animateSca = true;
            hover.animatePos = false;
            hover.size = hoverSize;

            gameObject.GetComponentAndPerformAction(delegate (Button x)
            {
                x.NewOnClickListener(delegate ()
                {
                    PrefabEditor.inst.OpenDialog();
                    createInternal = true;
                });
            });

            var nameHorizontalOverflow = config.PrefabInternalNameHorizontalWrap.Value;

            var nameVerticalOverflow = config.PrefabInternalNameVerticalWrap.Value;

            var nameFontSize = config.PrefabInternalNameFontSize.Value;

            var typeHorizontalOverflow = config.PrefabInternalTypeHorizontalWrap.Value;

            var typeVerticalOverflow = config.PrefabInternalTypeVerticalWrap.Value;

            var typeFontSize = config.PrefabInternalTypeFontSize.Value;

            var deleteAnchoredPosition = config.PrefabInternalDeleteButtonPos.Value;
            var deleteSizeDelta = config.PrefabInternalDeleteButtonSca.Value;

            var list = new List<Coroutine>();

            int num = 0;
            foreach (var prefab in DataManager.inst.gameData.prefabs)
            {
                if (ContainsName(prefab, PrefabDialog.Internal))
                    list.Add(StartCoroutine(CreatePrefabButton((Prefab)prefab, num, PrefabDialog.Internal, null, _toggle, hoverSize,
                        nameHorizontalOverflow, nameVerticalOverflow, nameFontSize,
                        typeHorizontalOverflow, typeVerticalOverflow, typeFontSize,
                        deleteAnchoredPosition, deleteSizeDelta)));
                num++;
            }

            //yield return StartCoroutine(LSHelpers.WaitForMultipleCoroutines(list, delegate ()
            //{
            //    //foreach (object obj in internalContent)
            //    //    ((Transform)obj).localScale = Vector3.one;
            //}));

            yield break;
        }

        public IEnumerator ExternalPrefabFiles(bool _toggle = false)
        {
            foreach (var prefabPanel in PrefabPanels.Where(x => x.Dialog == PrefabDialog.External))
            {
                prefabPanel.SetActive(ContainsName(prefabPanel.Prefab, PrefabDialog.External));
            }

            yield break;
        }

        public IEnumerator CreatePrefabButton(Prefab prefab, int index, PrefabDialog dialog, string file, bool _toggle, float hoversize,
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

            var name = tf.Find("name").GetComponent<Text>();
            var typeName = tf.Find("type-name").GetComponent<Text>();
            var color = tf.Find("category").GetComponent<Image>();
            var deleteRT = tf.Find("delete").GetComponent<RectTransform>();
            var addPrefabObject = gameObject.GetComponent<Button>();
            var delete = tf.Find("delete").GetComponent<Button>();
            var typeImage = tf.Find("category/type/type").GetComponent<Image>();

            name.text = prefab.Name;

            var prefabType = prefab.Type >= 0 && prefab.Type < DataManager.inst.PrefabTypes.Count ? (PrefabType)DataManager.inst.PrefabTypes[prefab.Type] : PrefabType.InvalidType;

            typeName.text = prefabType.Name;
            color.color = prefabType.Color;
            typeImage.sprite = prefabType.Icon;
            //typeImage.color = RTHelpers.InvertColorHue(RTHelpers.InvertColorValue(prefabType.Color));

            TooltipHelper.AddTooltip(gameObject,
                "<#" + LSColors.ColorToHex(color.color) + ">" + prefab.Name + "</color>",
                "O: " + prefab.Offset +
                "<br>T: " + typeName.text +
                "<br>Count: " + prefab.objects.Count +
                "<br>Description: " + prefab.description);

            addPrefabObject.onClick.ClearAll();
            delete.onClick.ClearAll();

            name.horizontalOverflow = nameHorizontalWrapMode;
            name.verticalOverflow = nameVerticalWrapMode;
            name.fontSize = nameFontSize;

            typeName.horizontalOverflow = typeHorizontalWrapMode;
            typeName.verticalOverflow = typeVerticalWrapMode;
            typeName.fontSize = typeFontSize;

            deleteRT.anchoredPosition = deleteAnchoredPosition;
            deleteRT.sizeDelta = deleteSizeDelta;

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
                var prefabPanel = new PrefabPanel
                {
                    GameObject = gameObject,
                    Button = addPrefabObject,
                    DeleteButton = delete,
                    Dialog = dialog,
                    Name = name,
                    TypeText = typeName,
                    TypeImage = color,
                    TypeIcon = typeImage,
                    Prefab = prefab,
                    Index = index,
                    FilePath = file
                };
                PrefabPanels.Add(prefabPanel);

                delete.onClick.AddListener(delegate ()
                {
                    EditorManager.inst.ShowDialog("Warning Popup");
                    RTEditor.inst.RefreshWarningPopup("Are you sure you want to delete this prefab? (This is permanent!)", delegate ()
                    {
                        DeleteExternalPrefab(prefabPanel);
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
                        RenderPrefabExternalDialog(prefabPanel);
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

        #endregion
    }
}
