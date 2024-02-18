using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

using HarmonyLib;
using BepInEx.Configuration;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

using SimpleJSON;
using Crosstales.FB;
using TMPro;
using LSFunctions;

using EditorManagement.Functions.Components;
using EditorManagement.Functions.Helpers;
using EditorManagement.Patchers;

using RTFunctions.Functions;
using RTFunctions.Functions.Animation;
using RTFunctions.Functions.Animation.Keyframe;
using RTFunctions.Functions.Components;
using RTFunctions.Functions.Data;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;
using RTFunctions.Functions.Optimization;

using BaseBeatmapObject = DataManager.GameData.BeatmapObject;
using BaseEventKeyframe = DataManager.GameData.EventKeyframe;
using BasePrefab = DataManager.GameData.Prefab;
using BasePrefabObject = DataManager.GameData.PrefabObject;
using BaseBackgroundObject = DataManager.GameData.BackgroundObject;

using ObjectType = DataManager.GameData.BeatmapObject.ObjectType;
using AutoKillType = DataManager.GameData.BeatmapObject.AutoKillType;

using ObjectSelection = ObjEditor.ObjectSelection;
using ObjectKeyframeSelection = ObjEditor.KeyframeSelection;
using EventKeyframeSelection = EventEditor.KeyframeSelection;

using MetadataWrapper = EditorManager.MetadataWrapper;

namespace EditorManagement.Functions.Editors
{
    public class RTEditor : MonoBehaviour
    {
        public static RTEditor inst;

        public float timeInEditorOffset;
        public static void Init(EditorManager editorManager) => editorManager?.gameObject?.AddComponent<RTEditor>();

        void Awake()
        {
            inst = this;

            timeOffset = Time.time;
            timeInEditorOffset = Time.time;

            try
            {
                PrefabWatcher = new FileSystemWatcher
                {
                    Path = RTFile.ApplicationDirectory + prefabListPath,
                    Filter = "*.lsp"
                };
                PrefabWatcher.Changed += OnPrefabPathChanged;
                PrefabWatcher.Created += OnPrefabPathChanged;
                PrefabWatcher.Deleted += OnPrefabPathChanged;
                PrefabWatcher.EnableRaisingEvents = true;

                ThemeWatcher = new FileSystemWatcher
                {
                    Path = RTFile.ApplicationDirectory + themeListPath,
                    Filter = "*.lst"
                };
                ThemeWatcher.Changed += OnThemePathChanged;
                ThemeWatcher.Created += OnThemePathChanged;
                ThemeWatcher.Deleted += OnThemePathChanged;
                ThemeWatcher.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }

            SetupNotificationValues();
            SetupTimelineBar();
            SetupTimelineTriggers();
            SetupSelectGUI();
            SetupCreateObjects();
            SetupDropdowns();
            SetupDoggo();
            SetupFileBrowser();
            SetupPaths();
            SetupTimelinePreview();
            SetupTimelineElements();
            SetupTimelineGrid();
            CreateObjectSearch();
            CreateWarningPopup();
            CreateREPLEditor();
            CreateMultiObjectEditor();
            CreatePropertiesWindow();
            CreateDocumentation();
            CreateDebug();
            CreateAutosavePopup();

            if (!RTFile.FileExists(EditorSettingsPath))
                CreateGlobalSettings();
            else
                LoadGlobalSettings();

            // Player Editor
            {
                var gameObject = new GameObject("PlayerEditorManager");
                gameObject.transform.SetParent(GameObject.Find("Editor Systems").transform);
                gameObject.AddComponent<CreativePlayersEditor>();
            }

            // Object Modifiers Editor
            {
                var gameObject = new GameObject("ObjectModifiersEditor");
                gameObject.transform.SetParent(GameObject.Find("Editor Systems").transform);
                gameObject.AddComponent<ObjectModifiersEditor>();
            }

            // Level Combiner
            {
                var gameObject = new GameObject("LevelCombiner");
                gameObject.transform.SetParent(GameObject.Find("Editor Systems").transform);
                gameObject.AddComponent<LevelCombiner>();
            }

            // Project Planner
            {
                var gameObject = new GameObject("ProjectPlanner");
                gameObject.transform.SetParent(GameObject.Find("Editor Systems").transform);
                gameObject.AddComponent<ProjectPlannerManager>();
            }

            mousePicker = new GameObject("picker");
            mousePicker.transform.SetParent(EditorManager.inst.GetDialog("Parent Selector").Dialog.parent.parent);
            mousePicker.transform.localScale = Vector3.one;
            mousePicker.layer = 5;
            mousePickerRT = mousePicker.AddComponent<RectTransform>();

            var img = new GameObject("image");
            img.transform.SetParent(mousePicker.transform);
            img.transform.localScale = Vector3.one;
            img.layer = 5;

            var imgRT = img.AddComponent<RectTransform>();
            imgRT.anchoredPosition = new Vector2(-930f, -520f);
            imgRT.sizeDelta = new Vector2(32f, 32f);

            var image = img.AddComponent<Image>();

            UIManager.GetImage(image, RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_dropper.png");

            doggoObject = GameObject.Find("Editor Systems/Editor GUI/sizer/main/Popups/File Info Popup/loading");
            doggoImage = doggoObject.GetComponent<Image>();
            timelineTime = EditorManager.inst.timelineTime.GetComponent<Text>();
            EditorPlugin.SetNotificationProperties();
            
            timelineSlider = EditorManager.inst.timelineSlider.GetComponent<Slider>();

            //EditorPlugin.SetTimelineColors();

            if (!ModCompatibility.sharedFunctions.ContainsKey("ParentPickerDisable"))
                ModCompatibility.sharedFunctions.Add("ParentPickerDisable", (Action)delegate ()
                {
                    parentPickerEnabled = false;
                });
            if (ModCompatibility.sharedFunctions.ContainsKey("ParentPickerDisable"))
                ModCompatibility.sharedFunctions["ParentPickerDisable"] = (Action)delegate ()
                {
                    parentPickerEnabled = false;
                };
        }

        void Update()
        {
            timeEditing = Time.time - timeOffset + savedTimeEditng;

            foreach (var timelineObject in timelineObjects)
            {
                if (timelineObject.Data != null && timelineObject.GameObject && timelineObject.Image)
                {
                    bool isCurrentLayer = timelineObject.Layer == Layer && layerType == LayerType.Objects;
                    timelineObject.GameObject.SetActive(isCurrentLayer);
                    if (isCurrentLayer)
                    {
                        timelineObject.Image.color = timelineObject.selected ? ObjEditor.inst.SelectedColor :
                            timelineObject.IsBeatmapObject && !string.IsNullOrEmpty(timelineObject.GetData<BeatmapObject>().prefabID) ? timelineObject.GetData<BeatmapObject>().GetPrefabTypeColor():
                            timelineObject.IsPrefabObject ? timelineObject.GetData<PrefabObject>().GetPrefabTypeColor() : ObjEditor.inst.NormalColor;
                    }
                }
            }

            if (ObjectEditor.inst && ObjectEditor.inst.CurrentSelection && ObjectEditor.inst.CurrentSelection.IsBeatmapObject && ObjectEditor.inst.CurrentSelection.InternalSelections.Count > 0)
                foreach (var timelineObject in ObjectEditor.inst.CurrentSelection.InternalSelections)
                {
                    if (timelineObject.Data != null && timelineObject.GameObject && timelineObject.Image)
                    {
                        timelineObject.GameObject.SetActive(true);

                        timelineObject.Image.color = timelineObject.selected ? ObjEditor.inst.SelectedColor : ObjEditor.inst.NormalColor;
                    }
                }

            foreach (var timelineObject in timelineKeyframes)
            {
                if (timelineObject.Data != null && timelineObject.GameObject && timelineObject.Image)
                {
                    int limit = timelineObject.Type / RTEventEditor.EventLimit;
                    bool isCurrentLayer = limit == Layer && layerType == LayerType.Events;

                    timelineObject.GameObject.SetActive(isCurrentLayer);

                    var color = EventEditor.inst.EventColors[timelineObject.Type % RTEventEditor.EventLimit];
                    color.a = 1f;

                    timelineObject.Image.color = timelineObject.selected ? EventEditor.inst.Selected : color;
                }
            }


            if (Input.GetMouseButtonDown(1))
                parentPickerEnabled = false;

            mousePicker?.SetActive(parentPickerEnabled);

            if (mousePicker != null && mousePickerRT != null && parentPickerEnabled)
            {
                float num = (float)Screen.width / 1920f;
                num = 1f / num;
                float x = mousePickerRT.sizeDelta.x;
                float y = mousePickerRT.sizeDelta.y;
                Vector3 zero = Vector3.zero;
                mousePickerRT.anchoredPosition = (Input.mousePosition + zero) * num;
            }

            if (selectingKey)
            {
                var key = KeybindManager.WatchKeyCode();

                if (key != KeyCode.None)
                {
                    selectingKey = false;

                    setKey?.Invoke(key);
                    onKeySet?.Invoke();
                }
            }

            if (GameManager.inst.timeline && timelinePreview)
                timelinePreview.gameObject.SetActive(GameManager.inst.timeline.activeSelf);

            if (GameManager.inst.gameState == GameManager.State.Playing && timelinePreview && AudioManager.inst.CurrentAudioSource.clip != null && GameManager.inst.timeline && GameManager.inst.timeline.activeSelf)
            {
                float num = AudioManager.inst.CurrentAudioSource.time * 400f / AudioManager.inst.CurrentAudioSource.clip.length;
                if (timelinePosition)
                {
                    timelinePosition.anchoredPosition = new Vector2(num, 0f);
                }

                timelinePreview.localPosition = GameManager.inst.timeline.transform.localPosition;
                timelinePreview.localScale = GameManager.inst.timeline.transform.localScale;
                timelinePreview.localRotation = GameManager.inst.timeline.transform.localRotation;

                for (int i = 0; i < checkpointImages.Count; i++)
                {
                    if (GameStorageManager.inst.checkpointImages.Count > i)
                        checkpointImages[i].color = GameStorageManager.inst.checkpointImages[i].color;
                }

                timelinePreviewPlayer.color = GameStorageManager.inst.timelinePlayer.color;
                timelinePreviewLeftCap.color = GameStorageManager.inst.timelineLeftCap.color;
                timelinePreviewRightCap.color = GameStorageManager.inst.timelineRightCap.color;
                timelinePreviewLine.color = GameStorageManager.inst.timelineLine.color;
            }

            if (!ModCompatibility.sharedFunctions.ContainsKey("ParentPickerActive"))
                ModCompatibility.sharedFunctions.Add("ParentPickerActive", parentPickerEnabled);
            if (ModCompatibility.sharedFunctions.ContainsKey("ParentPickerActive"))
                ModCompatibility.sharedFunctions["ParentPickerActive"] = parentPickerEnabled;
        }

        #region Variables

        public static bool RoundToNearest => GetEditorProperty("Round To Nearest").GetConfigEntry<bool>().Value;
        public static bool ShowModifiedColors => GetEditorProperty("Show Modified Colors").GetConfigEntry<bool>().Value;
        public static float BPMSnapDivisions => GetEditorProperty("BPM Snap Divisions").GetConfigEntry<float>().Value;
        public static bool BPMSnapKeyframes => GetEditorProperty("BPM Snaps Keyframes").GetConfigEntry<bool>().Value;

        public bool ienumRunning;
        public bool parentPickerEnabled = false;

        public Slider timelineSlider;

        public GameObject mousePicker;
        RectTransform mousePickerRT;

        public BeatmapObject objectToParent;

        public InputField layersIF;
        Image layersImage;
        public Image LayersImage
        {
            get
            {
                if (!layersImage)
                    layersImage = layersIF.GetComponent<Image>();
                return layersImage;
            }
        }

        public Toggle layerToggle;
        public Toggle LayerToggle
        {
            get
            {
                if (!layerToggle && layersIF.transform.parent.Find("6"))
                    layerToggle = layersIF.transform.parent.Find("6").GetComponent<Toggle>();
                return layerToggle;
            }
        }

        public GameObject timelineBar;

        public InputField pitchIF;
        public InputField timeIF;

        public GameObject defaultIF;

        public string objectSearchTerm = "";

        public GameObject replBase;
        public InputField replEditor;
        //public Text replText;

        public Transform titleBar;

        public Text fileInfoText;

        public GameObject doggoObject;
        public Image doggoImage;

        public Text timelineTime;

        public Image timelineSliderHandle;
        public Image timelineSliderRuler;
        public Image keyframeTimelineSliderHandle;
        public Image keyframeTimelineSliderRuler;

        Sprite searchSprite;
        public Sprite SearchSprite
        {
            get
            {
                if (!searchSprite)
                    searchSprite = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/parent/parent/image").GetComponent<Image>().sprite;
                return searchSprite;
            }
        }

        public string propertiesSearch;

        public static EditorProperty.EditorPropCategory currentCategory = EditorProperty.EditorPropCategory.General;

        public bool selectingKey = false;
        public Action onKeySet;
        public Action<KeyCode> setKey;

        public Transform timelinePreview;
        public RectTransform timelinePosition;

        public List<Document> documentations = new List<Document>();

        public bool canUpdateThemes = true;
        public bool canUpdatePrefabs = true;

        #endregion

        #region Settings

        public float timeOffset;
        public float timeEditing;
        public float savedTimeEditng;
        public int openAmount;

        public int levelFilter = 0;
        public bool levelAscend = true;

        public void SaveSettings()
        {
            var jn = JSON.Parse(RTFile.FileExists(GameManager.inst.basePath + "editor.lse") ? FileManager.inst.LoadJSONFileRaw(GameManager.inst.basePath + "editor.lse") : "{}");

            jn["timeline"]["tsc"] = EditorManager.inst.timelineScrollRectBar.value.ToString("f2");
            jn["timeline"]["z"] = EditorManager.inst.zoomFloat.ToString("f3");
            jn["timeline"]["l"] = EditorManager.inst.layer.ToString();
            jn["editor"]["t"] = timeEditing.ToString();
            jn["editor"]["a"] = openAmount.ToString();
            jn["sort"]["f"] = levelFilter.ToString();
            jn["sort"]["a"] = levelAscend.ToString();
            jn["misc"]["sn"] = SettingEditor.inst.SnapActive.ToString();
            jn["misc"]["t"] = AudioManager.inst.CurrentAudioSource.time;

            RTFile.WriteToFile(GameManager.inst.basePath + "editor.lse", jn.ToString(3));
        }

        public void LoadSettings()
        {
            if (!RTFile.FileExists(GameManager.inst.basePath + "editor.lse"))
            {
                savedTimeEditng = 0f;
                timeOffset = Time.time;
                return;
            }

            var jn = JSON.Parse(FileManager.inst.LoadJSONFileRaw(GameManager.inst.basePath + "editor.lse"));

            if (jn["timeline"] != null)
            {
                if (jn["timeline"]["z"] != null)
                    EditorManager.inst.zoomSlider.value = jn["timeline"]["z"].AsFloat;
                if (jn["timeline"]["tsc"] != null)
                    EditorManager.inst.timelineScrollRectBar.value = jn["timeline"]["tsc"].AsFloat;
                if (jn["timeline"]["l"] != null)
                    SetLayer(jn["timeline"]["l"].AsInt, false);
            }

            if (jn["editor"] != null)
            {
                savedTimeEditng = jn["editor"]["t"].AsFloat;
                openAmount = jn["editor"]["a"].AsInt + 1;
            }

            if (jn["sort"] != null)
            {
                levelFilter = jn["sort"]["f"].AsInt;
                levelAscend = jn["sort"]["a"].AsBool;
            }

            if (jn["misc"] != null)
            {
                if (jn["misc"]["sn"] != null)
                    SettingEditor.inst.SnapActive = jn["misc"]["sn"].AsBool;
                if (jn["misc"]["t"] != null && LevelLoadsSavedTime)
                    AudioManager.inst.SetMusicTime(jn["misc"]["t"].AsFloat);

                SettingEditor.inst.SnapBPM = DataManager.inst.metaData.song.BPM;
            }

            prevLayer = EditorManager.inst.layer;
            prevLayerType = layerType;
        }

        #endregion

        #region Notifications

        public List<string> notifications = new List<string>();

        public IEnumerator FixHelp(string _text, float _time)
        {
            EditorManager.inst.notification.transform.Find("info").gameObject.SetActive(true);
            EditorManager.inst.notification.transform.Find("info/text").GetComponent<TextMeshProUGUI>().text = _text;
            LayoutRebuilder.ForceRebuildLayoutImmediate(EditorManager.inst.notification.transform.Find("info/text").GetComponent<RectTransform>());
            LayoutRebuilder.ForceRebuildLayoutImmediate(EditorManager.inst.notification.transform.Find("info").GetComponent<RectTransform>());
            LayoutRebuilder.ForceRebuildLayoutImmediate(EditorManager.inst.notification.GetComponent<RectTransform>());
            yield return new WaitForSeconds(_time);
            EditorManager.inst.notification.transform.Find("info").gameObject.SetActive(EditorManager.inst.showHelp);
            if (EditorManager.inst.showHelp)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(EditorManager.inst.notification.transform.Find("info/text").GetComponent<RectTransform>());
                LayoutRebuilder.ForceRebuildLayoutImmediate(EditorManager.inst.notification.transform.Find("info").GetComponent<RectTransform>());
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(EditorManager.inst.notification.GetComponent<RectTransform>());
        }

        public IEnumerator DisplayDefaultNotification(string _text, float _time, EditorManager.NotificationType _type)
        {
            if (!GetEditorProperty("Debug").GetConfigEntry<bool>().Value)
            {
                Debug.LogFormat("{0}Notification:\nText: " + _text + "\nTime: " + _time + "\nType: " + _type, EditorPlugin.className);
            }
            if (notifications.Count < 20)
            {
                switch (_type)
                {
                    case EditorManager.NotificationType.Info:
                        {
                            GameObject gameObject = Instantiate(EditorManager.inst.notificationPrefabs[0], Vector3.zero, Quaternion.identity);
                            Destroy(gameObject, _time);
                            gameObject.transform.Find("text").GetComponent<TextMeshProUGUI>().text = _text;
                            gameObject.transform.SetParent(EditorManager.inst.notification.transform);
                            if (GetEditorProperty("Notification Direction").GetConfigEntry<Direction>().Value == Direction.Down)
                            {
                                gameObject.transform.SetAsFirstSibling();
                            }
                            gameObject.transform.localScale = Vector3.one;
                            break;
                        }
                    case EditorManager.NotificationType.Success:
                        {
                            GameObject gameObject1 = Instantiate(EditorManager.inst.notificationPrefabs[1], Vector3.zero, Quaternion.identity);
                            Destroy(gameObject1, _time);
                            gameObject1.transform.Find("text").GetComponent<Text>().text = _text;
                            gameObject1.transform.SetParent(EditorManager.inst.notification.transform);
                            if (GetEditorProperty("Notification Direction").GetConfigEntry<Direction>().Value == Direction.Down)
                            {
                                gameObject1.transform.SetAsFirstSibling();
                            }
                            gameObject1.transform.localScale = Vector3.one;
                            break;
                        }
                    case EditorManager.NotificationType.Error:
                        {
                            GameObject gameObject2 = Instantiate(EditorManager.inst.notificationPrefabs[2], Vector3.zero, Quaternion.identity);
                            Destroy(gameObject2, _time);
                            gameObject2.transform.Find("text").GetComponent<Text>().text = _text;
                            gameObject2.transform.SetParent(EditorManager.inst.notification.transform);
                            if (GetEditorProperty("Notification Direction").GetConfigEntry<Direction>().Value == Direction.Down)
                            {
                                gameObject2.transform.SetAsFirstSibling();
                            }
                            gameObject2.transform.localScale = Vector3.one;
                            break;
                        }
                    case EditorManager.NotificationType.Warning:
                        {
                            GameObject gameObject3 = Instantiate(EditorManager.inst.notificationPrefabs[3], Vector3.zero, Quaternion.identity);
                            Destroy(gameObject3, _time);
                            gameObject3.transform.Find("text").GetComponent<Text>().text = _text;
                            gameObject3.transform.SetParent(EditorManager.inst.notification.transform);
                            if (GetEditorProperty("Notification Direction").GetConfigEntry<Direction>().Value == Direction.Down)
                            {
                                gameObject3.transform.SetAsFirstSibling();
                            }
                            gameObject3.transform.localScale = Vector3.one;
                            break;
                        }
                }
            }

            yield break;
        }

        public void DisplayNotification(string _name, string _text, float _time, EditorManager.NotificationType _type)
        {
            inst.StartCoroutine(DisplayNotificationLoop(_name, _text, _time, _type));
        }

        public void DisplayCustomNotification(string _name, string _text, float _time, Color _base, Color _top, Color _icCol, string _title, Sprite _icon = null)
        {
            inst.StartCoroutine(DisplayCustomNotificationLoop(_name, _text, _time, _base, _top, _icCol, _title, _icon));
        }

        public IEnumerator DisplayNotificationLoop(string _name, string _text, float _time, EditorManager.NotificationType _type)
        {
            if (!GetEditorProperty("Debug").GetConfigEntry<bool>().Value)
            {
                Debug.Log("<color=#F6AC1A>Editor</color><color=#2FCBD6>Management</color>\nNotification: " + _name + "\nText: " + _text + "\nTime: " + _time + "\nType: " + _type);
            }
            if (!notifications.Contains(_name) && notifications.Count < 20 && GetEditorProperty("Notifications Display").GetConfigEntry<bool>().Value)
            {
                notifications.Add(_name);
                switch (_type)
                {
                    case EditorManager.NotificationType.Info:
                        {
                            GameObject gameObject = Instantiate(EditorManager.inst.notificationPrefabs[0], Vector3.zero, Quaternion.identity);
                            Destroy(gameObject, _time);
                            gameObject.transform.Find("text").GetComponent<TextMeshProUGUI>().text = _text;
                            gameObject.transform.SetParent(EditorManager.inst.notification.transform);
                            if (GetEditorProperty("Notification Direction").GetConfigEntry<Direction>().Value == Direction.Down)
                            {
                                gameObject.transform.SetAsFirstSibling();
                            }
                            gameObject.transform.localScale = Vector3.one;
                            break;
                        }
                    case EditorManager.NotificationType.Success:
                        {
                            GameObject gameObject1 = Instantiate(EditorManager.inst.notificationPrefabs[1], Vector3.zero, Quaternion.identity);
                            Destroy(gameObject1, _time);
                            gameObject1.transform.Find("text").GetComponent<Text>().text = _text;
                            gameObject1.transform.SetParent(EditorManager.inst.notification.transform);
                            if (GetEditorProperty("Notification Direction").GetConfigEntry<Direction>().Value == Direction.Down)
                            {
                                gameObject1.transform.SetAsFirstSibling();
                            }
                            gameObject1.transform.localScale = Vector3.one;
                            break;
                        }
                    case EditorManager.NotificationType.Error:
                        {
                            GameObject gameObject2 = Instantiate(EditorManager.inst.notificationPrefabs[2], Vector3.zero, Quaternion.identity);
                            Destroy(gameObject2, _time);
                            gameObject2.transform.Find("text").GetComponent<Text>().text = _text;
                            gameObject2.transform.SetParent(EditorManager.inst.notification.transform);
                            if (GetEditorProperty("Notification Direction").GetConfigEntry<Direction>().Value == Direction.Down)
                            {
                                gameObject2.transform.SetAsFirstSibling();
                            }
                            gameObject2.transform.localScale = Vector3.one;
                            break;
                        }
                    case EditorManager.NotificationType.Warning:
                        {
                            GameObject gameObject3 = Instantiate(EditorManager.inst.notificationPrefabs[3], Vector3.zero, Quaternion.identity);
                            Destroy(gameObject3, _time);
                            gameObject3.transform.Find("text").GetComponent<Text>().text = _text;
                            gameObject3.transform.SetParent(EditorManager.inst.notification.transform);
                            if (GetEditorProperty("Notification Direction").GetConfigEntry<Direction>().Value == Direction.Down)
                            {
                                gameObject3.transform.SetAsFirstSibling();
                            }
                            gameObject3.transform.localScale = Vector3.one;
                            break;
                        }
                }

                yield return new WaitForSeconds(_time);
                notifications.Remove(_name);
            }
            yield break;
        }

        public IEnumerator DisplayCustomNotificationLoop(string _name, string _text, float _time, Color _base, Color _top, Color _icCol, string _title, Sprite _icon = null)
        {
            if (!notifications.Contains(_name) && notifications.Count < 20 && GetEditorProperty("Notifications Display").GetConfigEntry<bool>().Value)
            {
                notifications.Add(_name);
                GameObject gameObject = Instantiate(EditorManager.inst.notificationPrefabs[0], Vector3.zero, Quaternion.identity);
                Destroy(gameObject, _time);
                gameObject.transform.Find("text").GetComponent<TextMeshProUGUI>().text = _text;
                gameObject.transform.SetParent(EditorManager.inst.notification.transform);
                if (GetEditorProperty("Notification Direction").GetConfigEntry<Direction>().Value == Direction.Down)
                {
                    gameObject.transform.SetAsFirstSibling();
                }
                gameObject.transform.localScale = Vector3.one;

                gameObject.GetComponent<Image>().color = _base;
                var bg = gameObject.transform.Find("bg");
                var img = bg.Find("Image").GetComponent<Image>();
                bg.Find("bg").GetComponent<Image>().color = _top;
                if (_icon != null)
                {
                    img.sprite = _icon;
                }

                img.color = _icCol;
                bg.Find("title").GetComponent<Text>().text = _title;

                yield return new WaitForSeconds(_time);
                notifications.Remove(_name);
            }

            yield break;
        }

        public void SetupNotificationValues()
        {
            var notifyRT = EditorManager.inst.notification.GetComponent<RectTransform>();
            var notifyGroup = EditorManager.inst.notification.GetComponent<VerticalLayoutGroup>();
            notifyRT.sizeDelta = new Vector2(GetEditorProperty("Notification Width").GetConfigEntry<float>().Value, 632f);
            EditorManager.inst.notification.transform.localScale =
                new Vector3(GetEditorProperty("Notification Size").GetConfigEntry<float>().Value, GetEditorProperty("Notification Size").GetConfigEntry<float>().Value, 1f);

            if (GetEditorProperty("Notification Direction").GetConfigEntry<Direction>().Value == Direction.Down)
            {
                notifyRT.anchoredPosition = new Vector2(8f, 408f);
                notifyGroup.childAlignment = TextAnchor.LowerLeft;
            }
            if (GetEditorProperty("Notification Direction").GetConfigEntry<Direction>().Value == Direction.Up)
            {
                notifyRT.anchoredPosition = new Vector2(8f, 410f);
                notifyGroup.childAlignment = TextAnchor.UpperLeft;
            }
        }

        #endregion

        #region Timeline

        Image timelineImage;
        public Image TimelineImage
        {
            get
            {
                if (!timelineImage)
                    timelineImage = EditorManager.inst.timeline.GetComponent<Image>();
                return timelineImage;
            }
        }

        Image timelineOverlayImage;
        public Image TimelineOverlayImage
        {
            get
            {
                if (!timelineOverlayImage)
                    timelineOverlayImage = EditorManager.inst.timelineWaveformOverlay.GetComponent<Image>();
                return timelineOverlayImage;
            }
        }

        public bool IsObjectDialog => EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Object);

        public bool IsTimeline => (EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Timeline) &&
            EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Object)) || EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Prefab);

        #endregion

        #region Timeline Objects

        public List<TimelineObject> timelineObjects = new List<TimelineObject>();
        public List<TimelineObject> timelineKeyframes = new List<TimelineObject>();
        //public List<TimelineObject> timelineBeatmapObjectKeyframes = new List<TimelineObject>();

        public List<TimelineObject> TimelineBeatmapObjects => timelineObjects.Where(x => x.IsBeatmapObject).ToList();
        public List<TimelineObject> TimelinePrefabObjects => timelineObjects.Where(x => x.IsPrefabObject).ToList();

        public void RemoveTimelineObject(TimelineObject timelineObject)
        {
            if (timelineObjects.Has(x => x.ID == timelineObject.ID))
            {
                int a = timelineObjects.FindIndex(x => x.ID == timelineObject.ID);
                timelineObject.selected = false;
                Destroy(timelineObject.GameObject);
                timelineObjects.RemoveAt(a);
            }
        }

        public static Sprite GetKeyframeIcon(DataManager.LSAnimation a, DataManager.LSAnimation b)
            => ObjEditor.inst.KeyframeSprites[a.Name.Contains("Out") && b.Name.Contains("In") ? 3 : a.Name.Contains("Out") ? 2 : b.Name.Contains("In") ? 1 : 0];

        #endregion

        #region Timeline Textures

        public static bool GenerateWaveform => GetEditorProperty("Waveform Generate").GetConfigEntry<bool>().Value;

        public static WaveformType WaveformMode => GetEditorProperty("Waveform Mode").GetConfigEntry<WaveformType>().Value;
        public static Color WaveformBGColor => GetEditorProperty("Waveform BG Color").GetConfigEntry<Color>().Value;
        public static Color WaveformTopColor => GetEditorProperty("Waveform Top Color").GetConfigEntry<Color>().Value;
        public static Color WaveformBottomColor => GetEditorProperty("Waveform Bottom Color").GetConfigEntry<Color>().Value;
        public static TextureFormat WaveformFormat => GetEditorProperty("Waveform Texture Format").GetConfigEntry<TextureFormat>().Value;

        public IEnumerator AssignTimelineTexture()
        {
            if (!EditorManager.inst.hasLoadedLevel && !EditorManager.inst.loading && !RTFile.FileExists($"{RTFile.ApplicationDirectory}settings/waveform-{WaveformMode.ToString().ToLower()}.png") ||
                !RTFile.FileExists(GameManager.inst.basePath + $"waveform-{WaveformMode.ToString().ToLower()}.png"))
            {
                int num = Mathf.Clamp((int)AudioManager.inst.CurrentAudioSource.clip.length * 48, 100, 15000);
                Texture2D waveform = null;

                if (WaveformMode == WaveformType.Legacy)
                    StartCoroutine(Legacy(AudioManager.inst.CurrentAudioSource.clip, num, 300, WaveformBGColor, WaveformTopColor, WaveformBottomColor, delegate (Texture2D _tex) { waveform = _tex; }));
                if (WaveformMode == WaveformType.Beta)
                    StartCoroutine(Beta(AudioManager.inst.CurrentAudioSource.clip, num, 300, WaveformBGColor, WaveformTopColor, delegate (Texture2D _tex) { waveform = _tex; }));
                if (WaveformMode == WaveformType.BetaFast)
                    StartCoroutine(BetaFast(AudioManager.inst.CurrentAudioSource.clip, 1f, num, 300, WaveformBGColor, WaveformTopColor, delegate (Texture2D _tex) { waveform = _tex; }));
                if (WaveformMode == WaveformType.LegacyFast)
                    StartCoroutine(LegacyFast(AudioManager.inst.CurrentAudioSource.clip, 1f, num, 300, WaveformBGColor, WaveformTopColor, WaveformBottomColor, delegate (Texture2D _tex) { waveform = _tex; }));

                while (waveform == null)
                    yield return null;

                var waveSprite = Sprite.Create(waveform, new Rect(0f, 0f, (float)num, 300f), new Vector2(0.5f, 0.5f), 100f);
                TimelineImage.sprite = waveSprite;
                TimelineOverlayImage.sprite = TimelineImage.sprite;
            }
            else
            {
                var waveSprite = SpriteManager.LoadSprite(!EditorManager.inst.hasLoadedLevel && !EditorManager.inst.loading ?
                    $"{RTFile.ApplicationDirectory}settings/waveform-{WaveformMode.ToString().ToLower()}.png" :
                    GameManager.inst.basePath + $"waveform-{WaveformMode.ToString().ToLower()}.png");
                TimelineImage.sprite = waveSprite;
                TimelineOverlayImage.sprite = TimelineImage.sprite;
            }

            TimelineImage.sprite.Save(!EditorManager.inst.hasLoadedLevel && !EditorManager.inst.loading ?
                    $"{RTFile.ApplicationDirectory}settings/waveform-{WaveformMode.ToString().ToLower()}.png" :
                    GameManager.inst.basePath + $"waveform-{WaveformMode.ToString().ToLower()}.png");

            SetTimelineGridSize();

            yield break;
        }

        public IEnumerator Beta(AudioClip clip, int textureWidth, int textureHeight, Color background, Color waveform, Action<Texture2D> action)
        {
            Debug.LogFormat("{0}Generating Beta Waveform", EditorPlugin.className);
            int num = 100;
            Texture2D texture2D = new Texture2D(textureWidth, textureHeight, WaveformFormat, false);
            Color[] array = new Color[texture2D.width * texture2D.height];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = background;
            }
            texture2D.SetPixels(array);
            num = clip.frequency / num;
            float[] array2 = new float[clip.samples * clip.channels];
            clip.GetData(array2, 0);
            float[] array3 = new float[array2.Length / num];
            for (int j = 0; j < array3.Length; j++)
            {
                array3[j] = 0f;
                for (int k = 0; k < num; k++)
                {
                    array3[j] += Mathf.Abs(array2[j * num + k]);
                }
                array3[j] /= (float)num;
            }
            for (int l = 0; l < array3.Length - 1; l++)
            {
                int num2 = 0;
                while ((float)num2 < (float)textureHeight * array3[l] + 1f)
                {
                    texture2D.SetPixel(textureWidth * l / array3.Length, (int)((float)textureHeight * (array3[l] + 1f) / 2f) - num2, waveform);
                    num2++;
                }
            }
            texture2D.wrapMode = TextureWrapMode.Clamp;
            texture2D.filterMode = FilterMode.Point;
            texture2D.Apply();
            action(texture2D);
            yield break;
        }

        public IEnumerator Legacy(AudioClip clip, int textureWidth, int textureHeight, Color background, Color _top, Color _bottom, Action<Texture2D> action)
        {
            Debug.LogFormat("{0}Generating Legacy Waveform", EditorPlugin.className);
            int num = 160;
            num = clip.frequency / num;
            Texture2D texture2D = new Texture2D(textureWidth, textureHeight, WaveformFormat, false);
            Color[] array = new Color[texture2D.width * texture2D.height];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = background;
            }
            texture2D.SetPixels(array);
            float[] array3 = new float[clip.samples];
            float[] array4 = new float[clip.samples];
            float[] array5 = new float[clip.samples * clip.channels];
            clip.GetData(array5, 0);
            if (clip.channels > 1)
            {
                array3 = array5.Where((float value, int index) => index % 2 != 0).ToArray();
                array4 = array5.Where((float value, int index) => index % 2 == 0).ToArray();
            }
            else
            {
                array3 = array5;
                array4 = array5;
            }
            float[] array6 = new float[array3.Length / num];
            for (int j = 0; j < array6.Length; j++)
            {
                array6[j] = 0f;
                for (int k = 0; k < num; k++)
                {
                    array6[j] += Mathf.Abs(array3[j * num + k]);
                }
                array6[j] /= (float)num;
                array6[j] *= 0.85f;
            }
            for (int l = 0; l < array6.Length - 1; l++)
            {
                int num2 = 0;
                while ((float)num2 < (float)textureHeight * array6[l])
                {
                    texture2D.SetPixel(textureWidth * l / array6.Length, (int)((float)textureHeight * array6[l]) - num2, _top);
                    num2++;
                }
            }
            array6 = new float[array4.Length / num];
            for (int m = 0; m < array6.Length; m++)
            {
                array6[m] = 0f;
                for (int n = 0; n < num; n++)
                {
                    array6[m] += Mathf.Abs(array4[m * num + n]);
                }
                array6[m] /= (float)num;
                array6[m] *= 0.85f;
            }
            for (int num3 = 0; num3 < array6.Length - 1; num3++)
            {
                int num4 = 0;
                while ((float)num4 < (float)textureHeight * array6[num3])
                {
                    int x = textureWidth * num3 / array6.Length;
                    int y = (int)array4[num3 * num + num4] - num4;
                    texture2D.SetPixel(x, y, texture2D.GetPixel(x, y) == _top ? MixColors(new List<Color> { _top, _bottom }) : _bottom);
                    num4++;
                }
            }
            texture2D.wrapMode = TextureWrapMode.Clamp;
            texture2D.filterMode = FilterMode.Point;
            texture2D.Apply();
            action(texture2D);
            yield break;
        }

        public IEnumerator BetaFast(AudioClip audio, float saturation, int width, int height, Color background, Color col, Action<Texture2D> action)
        {
            Debug.LogFormat("{0}Generating Beta Waveform (Fast)", EditorPlugin.className);
            Texture2D tex = new Texture2D(width, height, WaveformFormat, false);
            float[] samples = new float[audio.samples * audio.channels];
            float[] waveform = new float[width];
            audio.GetData(samples, 0);
            float packSize = ((float)samples.Length / (float)width);
            int s = 0;
            for (float i = 0; Mathf.RoundToInt(i) < samples.Length && s < waveform.Length; i += packSize)
            {
                waveform[s] = Mathf.Abs(samples[Mathf.RoundToInt(i)]);
                s++;
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    tex.SetPixel(x, y, background);
                }
            }

            for (int x = 0; x < waveform.Length; x++)
            {
                for (int y = 0; y <= waveform[x] * ((float)height * .75f); y++)
                {
                    tex.SetPixel(x, (height / 2) + y, col);
                    tex.SetPixel(x, (height / 2) - y, col);
                }
            }
            tex.Apply();

            action(tex);
            yield break;
        }

        public IEnumerator LegacyFast(AudioClip audio, float saturation, int width, int height, Color background, Color colTop, Color colBot, Action<Texture2D> action)
        {
            Debug.LogFormat("{0}Generating Legacy Waveform (Fast)", EditorPlugin.className);
            Texture2D tex = new Texture2D(width, height, WaveformFormat, false);
            
            float[] samples = new float[audio.samples * audio.channels];
            float[] waveform = new float[width];
            audio.GetData(samples, 0);
            float packSize = ((float)samples.Length / (float)width);
            int s = 0;
            for (float i = 0; Mathf.RoundToInt(i) < samples.Length && s < waveform.Length; i += packSize)
            {
                waveform[s] = Mathf.Abs(samples[Mathf.RoundToInt(i)]);
                s++;
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    tex.SetPixel(x, y, background);
                }
            }

            for (int x = 0; x < waveform.Length; x++)
            {
                for (int y = 0; y <= waveform[x] * ((float)height * .75f); y++)
                {
                    tex.SetPixel(x, height - y, colTop);

                    tex.SetPixel(x, y, tex.GetPixel(x, y) == colTop ? MixColors(new List<Color> { colTop, colBot }) : colBot);
                }
            }
            tex.Apply();

            action(tex);
            yield break;
        }

        public static Color MixColors(List<Color> colors)
        {
            var invertedColorSum = Color.black;
            foreach (var color in colors)
            {
                invertedColorSum += Color.white - color;
            }

            return Color.white - invertedColorSum / colors.Count;
        }

        public float timelineGridRenderMultiSizeCloser = 40f;
        public float timelineGridRenderMultiSizeClose = 20f;
        public float timelineGridUnrenderSize = 6f;
        public void SetTimelineGridSize()
        {
            if (!AudioManager.inst || !AudioManager.inst.CurrentAudioSource || !AudioManager.inst.CurrentAudioSource.clip)
            {
                if (timelineGridRenderer)
                    timelineGridRenderer.enabled = false;
                return;
            }

            var clipLength = AudioManager.inst.CurrentAudioSource.clip.length;

            float x = SettingEditor.inst.SnapBPM / 60f;

            var closer = timelineGridRenderMultiSizeCloser * x;
            var close = timelineGridRenderMultiSizeClose * x;
            var unrender = timelineGridUnrenderSize * x;

            var bpm = EditorManager.inst.Zoom > closer ? SettingEditor.inst.SnapBPM : EditorManager.inst.Zoom > close ? SettingEditor.inst.SnapBPM / 2f : SettingEditor.inst.SnapBPM / 4f;
            var snapDivisions = BPMSnapDivisions * 2f;
            if (timelineGridRenderer && EditorManager.inst.Zoom > unrender && GetEditorProperty("Timeline Grid Enabled").GetConfigEntry<bool>().Value)
            {
                timelineGridRenderer.enabled = false;
                timelineGridRenderer.gridCellSize.x = ((int)bpm / (int)snapDivisions) * (int)clipLength;
                timelineGridRenderer.gridSize.x = clipLength * bpm / (snapDivisions * 1.875f);
                timelineGridRenderer.enabled = true;
            }
            else if (timelineGridRenderer)
                timelineGridRenderer.enabled = false;
        }

        #endregion

        #region Paths

        public static string EditorSettingsPath => $"{RTFile.ApplicationDirectory}settings/editor.lss";

        public static string EditorPath
        {
            get => editorPath;
            set
            {
                editorPath = value;
                // Makes the editor path always in the beatmaps folder.
                editorListPath = $"beatmaps/{editorPath}";
                editorListSlash = $"beatmaps/{editorPath}/";

                if (ModCompatibility.sharedFunctions.ContainsKey("EditorPath"))
                    ModCompatibility.sharedFunctions["EditorPath"] = editorPath;
                else
                    ModCompatibility.sharedFunctions.Add("EditorPath", editorPath);
            }
        }
        static string editorPath = "editor";
        public static string editorListPath = "beatmaps/editor";
        public static string editorListSlash = "beatmaps/editor/";

        public static string ThemePath
        {
            get => themePath;
            set
            {
                themePath = value;
                // Makes the themes path always in the beatmaps folder.
                themeListPath = $"beatmaps/{themePath}";
                themeListSlash = $"beatmaps/{themePath}/";

                if (ModCompatibility.sharedFunctions.ContainsKey("ThemePath"))
                    ModCompatibility.sharedFunctions["ThemePath"] = themePath;
                else
                    ModCompatibility.sharedFunctions.Add("ThemePath", themePath);
            }
        }
        static string themePath = "themes";
        public static string themeListPath = "beatmaps/themes";
        public static string themeListSlash = "beatmaps/themes/";

        public static string PrefabPath
        {
            get => prefabPath;
            set
            {
                prefabPath = value;
                // Makes the prefabs path always in the beatmaps folder.
                prefabListPath = $"beatmaps/{prefabPath}";
                prefabListSlash = $"beatmaps/{prefabPath}/";

                if (ModCompatibility.sharedFunctions.ContainsKey("PrefabPath"))
                    ModCompatibility.sharedFunctions["PrefabPath"] = prefabPath;
                else
                    ModCompatibility.sharedFunctions.Add("PrefabPath", prefabPath);
            }
        }
        static string prefabPath = "prefabs";
        public static string prefabListPath = "beatmaps/prefabs";
        public static string prefabListSlash = "beatmaps/prefabs/";

        public void CreateGlobalSettings()
        {
            if (!RTFile.FileExists(EditorSettingsPath))
            {
                var jn = JSON.Parse("{}");

                EditorPath = "editor";
                jn["paths"]["editor"] = EditorPath;

                ThemePath = "themes";
                jn["paths"]["themes"] = ThemePath;

                PrefabPath = "prefabs";
                jn["paths"]["prefabs"] = PrefabPath;

                for (int i = 0; i < MarkerEditor.inst.markerColors.Count; i++)
                {
                    jn["marker_colors"][i] = LSColors.ColorToHex(MarkerEditor.inst.markerColors[i]);
                }

                EditorManager.inst.layerColors.RemoveAt(5);
                for (int i = 0; i < EditorManager.inst.layerColors.Count; i++)
                {
                    jn["layer_colors"][i] = LSColors.ColorToHex(EditorManager.inst.layerColors[i]);
                }

                RTFile.WriteToFile(EditorSettingsPath, jn.ToString(3));
            }
        }

        public void LoadGlobalSettings()
        {
            if (!RTFile.FileExists(EditorSettingsPath))
                return;

            var jn = JSON.Parse(RTFile.ReadFromFile(EditorSettingsPath));

            if (!string.IsNullOrEmpty(jn["paths"]["editor"]))
                EditorPath = jn["paths"]["editor"];
            if (!string.IsNullOrEmpty(jn["paths"]["themes"]))
                ThemePath = jn["paths"]["themes"];
            if (!string.IsNullOrEmpty(jn["paths"]["prefabs"]))
                PrefabPath = jn["paths"]["prefabs"];

            if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + editorListPath))
                Directory.CreateDirectory(RTFile.ApplicationDirectory + editorListPath);
            if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + themeListPath))
                Directory.CreateDirectory(RTFile.ApplicationDirectory + themeListPath);
            if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + prefabListPath))
                Directory.CreateDirectory(RTFile.ApplicationDirectory + prefabListPath);

            SetWatcherPaths();

            if (jn["marker_colors"] != null)
            {
                MarkerEditor.inst.markerColors.Clear();
                for (int i = 0; i < jn["marker_colors"].Count; i++)
                {
                    MarkerEditor.inst.markerColors.Add(LSColors.HexToColor(jn["marker_colors"][i]));
                }
            }
            else
            {
                for (int i = 0; i < MarkerEditor.inst.markerColors.Count; i++)
                {
                    jn["marker_colors"][i] = LSColors.ColorToHex(MarkerEditor.inst.markerColors[i]);
                }

                RTFile.WriteToFile(EditorSettingsPath, jn.ToString(3));
            }

            if (jn["layer_colors"] != null)
            {
                EditorManager.inst.layerColors.Clear();
                for (int i = 0; i < jn["layer_colors"].Count; i++)
                {
                    EditorManager.inst.layerColors.Add(LSColors.HexToColor(jn["layer_colors"][i]));
                }
            }
            else
            {
                for (int i = 0; i < EditorManager.inst.layerColors.Count; i++)
                {
                    jn["layer_colors"][i] = LSColors.ColorToHex(EditorManager.inst.layerColors[i]);
                }

                RTFile.WriteToFile(EditorSettingsPath, jn.ToString(3));
            }
        }

        public void SaveGlobalSettings()
        {
            var jn = JSON.Parse(RTFile.ReadFromFile(EditorSettingsPath));

            jn["paths"]["editor"] = EditorPath;
            jn["paths"]["themes"] = ThemePath;
            jn["paths"]["prefabs"] = PrefabPath;

            SetWatcherPaths();

            for (int i = 0; i < MarkerEditor.inst.markerColors.Count; i++)
            {
                jn["marker_colors"][i] = LSColors.ColorToHex(MarkerEditor.inst.markerColors[i]);
            }

            for (int i = 0; i < EditorManager.inst.layerColors.Count; i++)
            {
                jn["layer_colors"][i] = LSColors.ColorToHex(EditorManager.inst.layerColors[i]);
            }

            RTFile.WriteToFile(EditorSettingsPath, jn.ToString(3));
        }

        public void SetWatcherPaths()
        {
            PrefabWatcher.EnableRaisingEvents = false;
            if (RTFile.DirectoryExists(RTFile.ApplicationDirectory + prefabListPath))
            {
                PrefabWatcher.Path = RTFile.ApplicationDirectory + prefabListPath;
                PrefabWatcher.EnableRaisingEvents = true;
            }
            ThemeWatcher.EnableRaisingEvents = false;
            if (RTFile.DirectoryExists(RTFile.ApplicationDirectory + themeListPath))
            {
                ThemeWatcher.Path = RTFile.ApplicationDirectory + themeListPath;
                ThemeWatcher.EnableRaisingEvents = true;
            }
        }

        public void OnPrefabPathChanged(object sender, FileSystemEventArgs e)
        {
            if (canUpdatePrefabs && WatchPrefabFiles)
            {
                StartCoroutine(UpdatePrefabs());
            }
        }

        public void OnThemePathChanged(object sender, FileSystemEventArgs e)
        {
            if (canUpdateThemes && WatchThemeFiles)
            {
                StartCoroutine(LoadThemes(EventEditor.inst.dialogRight.GetChild(4).gameObject.activeInHierarchy));
            }
        }

        public static bool WatchPrefabFiles { get; set; } = GetEditorProperty("Update Prefab List on Files Changed").GetConfigEntry<bool>().Value;
        public static bool WatchThemeFiles { get; set; } = GetEditorProperty("Update Theme List on Files Changed").GetConfigEntry<bool>().Value;

        public FileSystemWatcher PrefabWatcher { get; set; }
        public FileSystemWatcher ThemeWatcher { get; set; }

        #endregion

        #region Objects

        public void Duplicate(bool _regen = true) => Copy(false, true, _regen);

        public void Copy(bool _cut = false, bool _dup = false, bool _regen = true)
        {
            if ((EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Timeline) && EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Background)) || EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Background))
            {
                BackgroundEditor.inst.CopyBackground();
                if (!_cut)
                {
                    EditorManager.inst.DisplayNotification("Copied Background Object", 1f, EditorManager.NotificationType.Success, false);
                }
                else
                {
                    BackgroundEditor.inst.DeleteBackground(BackgroundEditor.inst.currentObj);
                    EditorManager.inst.DisplayNotification("Cut Background Object", 1f, EditorManager.NotificationType.Success, false);
                }
                if (_dup)
                {
                    EditorManager.inst.Paste(0f);
                }
            }
            if ((EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Timeline) && EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Checkpoint)) || EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Checkpoint))
            {
                if (!_dup)
                {
                    CheckpointEditor.inst.CopyCheckpoint();
                    if (!_cut)
                    {
                        EditorManager.inst.DisplayNotification("Copied Checkpoint", 1f, EditorManager.NotificationType.Success, false);
                    }
                    else
                    {
                        BackgroundEditor.inst.DeleteBackground(BackgroundEditor.inst.currentObj);
                        EditorManager.inst.DisplayNotification("Cut Checkpoint", 1f, EditorManager.NotificationType.Success, false);
                    }
                }
                else
                {
                    EditorManager.inst.DisplayNotification("Can't Duplicate Checkpoint", 1f, EditorManager.NotificationType.Error, false);
                }
            }
            if (EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Object))
            {
                if (!_dup)
                {
                    ObjEditor.inst.CopyAllSelectedEvents();
                    if (!_cut)
                    {
                        EditorManager.inst.DisplayNotification("Copied Object Keyframe", 1f, EditorManager.NotificationType.Success, false);
                    }
                    else
                    {
                        StartCoroutine(ObjectEditor.inst.DeleteKeyframes());
                        EditorManager.inst.DisplayNotification("Cut Object Keyframe", 1f, EditorManager.NotificationType.Success, false);
                    }
                }
                else
                {
                    EditorManager.inst.DisplayNotification("Can't Duplicate Keyframe", 1f, EditorManager.NotificationType.Error, false);
                }
            }
            if ((EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Timeline) && EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Event)) || EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Event))
            {
                if (!_dup)
                {
                    EventEditor.inst.CopyAllSelectedEvents();
                    if (!_cut)
                    {
                        EditorManager.inst.DisplayNotification("Copied Event Keyframe", 1f, EditorManager.NotificationType.Success, false);
                    }
                    else
                    {
                        foreach (EventKeyframeSelection keyframeSelection2 in EventEditor.inst.copiedEventKeyframes.Keys)
                        {
                            EventEditor.inst.DeleteEvent(keyframeSelection2.Type, keyframeSelection2.Index);
                        }
                        EventEditor.inst.copiedEventKeyframes.Clear();
                        EditorManager.inst.DisplayNotification("Cut Event Keyframe", 1f, EditorManager.NotificationType.Success, false);
                    }
                }
                else
                {
                    EditorManager.inst.DisplayNotification("Can't Duplicate Keyframe", 1f, EditorManager.NotificationType.Error, false);
                }
            }
            if ((EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Timeline) && EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Object)) || EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Prefab))
            {
                var offsetTime = ObjectEditor.inst.SelectedObjects.Min(x => x.Time);

                ObjEditor.inst.CopyObject();
                if (!_cut)
                {
                    EditorManager.inst.DisplayNotification("Copied Beatmap Object", 1f, EditorManager.NotificationType.Success, false);
                }
                else
                {
                    StartCoroutine(ObjectEditor.inst.DeleteObjects());
                    EditorManager.inst.DisplayNotification("Cut Beatmap Object", 1f, EditorManager.NotificationType.Success, false);
                }
                if (_dup)
                {
                    Paste(offsetTime, _regen);
                }
            }
        }

        public void Paste(float _offsetTime = 0f, bool _regen = true)
        {
            if ((EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Timeline) && EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Object)) || (EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Timeline) && EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Prefab)) || (EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Timeline) && EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Prefab)))
            {
                PasteObject(_offsetTime, _regen);
            }
            if ((EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Timeline) && EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Event)) || EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Event))
            {
                RTEventEditor.inst.PasteEvents();
                EditorManager.inst.DisplayNotification("Pasted Event Object", 1f, EditorManager.NotificationType.Success);
            }
            if (EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Object))
            {
                ObjEditor.inst.PasteKeyframes();
                EditorManager.inst.DisplayNotification("Pasted Object Keyframe", 1f, EditorManager.NotificationType.Success);
            }
            if ((EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Timeline) && EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Checkpoint)) || EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Checkpoint))
            {
                CheckpointEditor.inst.PasteCheckpoint();
                EditorManager.inst.DisplayNotification("Pasted Checkpoint Object", 1f, EditorManager.NotificationType.Success);
            }
            if (EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Background))
            {
                BackgroundEditor.inst.PasteBackground();
                EditorManager.inst.DisplayNotification("Pasted Background Object", 1f, EditorManager.NotificationType.Success);
            }
        }

        public void PasteObject(float _offsetTime = 0f, bool _regen = true)
        {
            if (!ObjEditor.inst.hasCopiedObject || ObjEditor.inst.beatmapObjCopy == null || (ObjEditor.inst.beatmapObjCopy.prefabObjects.Count <= 0 && ObjEditor.inst.beatmapObjCopy.objects.Count <= 0))
            {
                EditorManager.inst.DisplayNotification("No copied object yet!", 1f, EditorManager.NotificationType.Error, false);
                return;
            }

            ObjectEditor.inst.DeselectAllObjects();
            EditorManager.inst.DisplayNotification("Pasting objects, please wait.", 1f, EditorManager.NotificationType.Success);

            Prefab pr = null;

            if (RTFile.FileExists(Application.persistentDataPath + "/copied_objects.lsp"))
            {
                JSONNode jn = JSON.Parse(FileManager.inst.LoadJSONFileRaw(Application.persistentDataPath + "/copied_objects.lsp"));

                pr = Prefab.Parse(jn);

                ObjEditor.inst.hasCopiedObject = true;
            }

            StartCoroutine(ObjectEditor.inst.AddPrefabExpandedToLevel(pr ?? ObjEditor.inst.beatmapObjCopy, true, _offsetTime, false, _regen));
        }

        public void Delete()
        {
            if (IsObjectDialog)
            {
                if (ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
                    if (ObjEditor.inst.currentKeyframe != 0)
                    {
                        var list = new List<TimelineObject>();
                        foreach (var timelineObject in ObjectEditor.inst.CurrentSelection.InternalSelections.Where(x => x.selected))
                            list.Add(timelineObject);
                        var beatmapObject = ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>();

                        EditorManager.inst.history.Add(new History.Command("Delete Keyframes", delegate ()
                        {
                            inst.StartCoroutine(ObjectEditor.inst.DeleteKeyframes());
                        }, delegate ()
                        {
                            ObjectEditor.inst.PasteKeyframes(beatmapObject, list, false);
                        }));

                        inst.StartCoroutine(ObjectEditor.inst.DeleteKeyframes());
                    }
                else
                    EditorManager.inst.DisplayNotification("Can't Delete First Keyframe.", 1f, EditorManager.NotificationType.Error);
                return;
            }
            if (IsTimeline)
            {
                if (DataManager.inst.gameData.beatmapObjects.Count > 1 && ObjectEditor.inst.SelectedObjectCount != DataManager.inst.gameData.beatmapObjects.Count)
                {
                    var list = new List<TimelineObject>();
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                        list.Add(timelineObject);

                    EditorManager.inst.ClearDialogs(EditorManager.EditorDialog.DialogType.Object, EditorManager.EditorDialog.DialogType.Prefab);

                    float startTime = 0f;

                    List<float> startTimeList = new List<float>();
                    foreach (var bm in list)
                        startTimeList.Add(bm.Time);

                    startTimeList = (from x in startTimeList
                                     orderby x ascending
                                     select x).ToList();

                    startTime = startTimeList[0];

                    var prefab = new Prefab("deleted objects", 0, startTime,
                        list.Where(x => x.IsBeatmapObject).Select(x => x.GetData<BeatmapObject>()).ToList(),
                        list.Where(x => x.IsPrefabObject).Select(x => x.GetData<PrefabObject>()).ToList());

                    EditorManager.inst.history.Add(new History.Command("Delete Objects", delegate ()
                    {
                        Delete();
                    }, delegate ()
                    {
                        ObjectEditor.inst.DeselectAllObjects();
                        StartCoroutine(ObjectEditor.inst.AddPrefabExpandedToLevel(prefab, true, 0f, true));
                    }));

                    StartCoroutine(ObjectEditor.inst.DeleteObjects());
                }
                else
                    EditorManager.inst.DisplayNotification("Can't Delete Only Beatmap Object", 1f, EditorManager.NotificationType.Error);
                return;
            }
            if (EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Event))
            {
                if (RTEventEditor.inst.SelectedKeyframes.Count > 0 && !RTEventEditor.inst.SelectedKeyframes.Has(x => x.Index == 0))
                {
                    EditorManager.inst.ClearDialogs(EditorManager.EditorDialog.DialogType.Event);

                    var list = new List<TimelineObject>();
                    foreach (var timelineObject in RTEventEditor.inst.SelectedKeyframes)
                        list.Add(timelineObject);

                    EditorManager.inst.history.Add(new History.Command("Delete Event Keyframes", delegate ()
                    {
                        StartCoroutine(RTEventEditor.inst.DeleteKeyframes(list));
                    }, delegate ()
                    {
                        RTEventEditor.inst.PasteEvents(list, false);
                    }));

                    StartCoroutine(RTEventEditor.inst.DeleteKeyframes());
                }
                else
                    EditorManager.inst.DisplayNotification("Can't delete first Event Keyframe.", 1f, EditorManager.NotificationType.Error);
                return;
            }
            if (EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Background))
            {
                BackgroundEditor.inst.DeleteBackground(BackgroundEditor.inst.currentObj);
                return;
            }
            if (EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Checkpoint))
            {
                if (CheckpointEditor.inst.currentObj != 0)
                {
                    CheckpointEditor.inst.DeleteCheckpoint(CheckpointEditor.inst.currentObj);
                    EditorManager.inst.DisplayNotification("Deleted Checkpoint.", 1f, EditorManager.NotificationType.Success);
                }
                else
                    EditorManager.inst.DisplayNotification("Can't delete first Checkpoint.", 1f, EditorManager.NotificationType.Error);
                return;
            }
        }

        #endregion

        #region Layers

        public int Layer
        {
            get => Mathf.Clamp(EditorManager.inst.layer, 0, int.MaxValue);
            set => EditorManager.inst.layer = Mathf.Clamp(value, 0, int.MaxValue);
        }

        int prevLayer;
        LayerType prevLayerType;
        public LayerType layerType;

        public enum LayerType
        {
            Objects,
            Events
        }

        public static int GetLayer(int _layer) => Mathf.Clamp(_layer, 0, int.MaxValue);

        public static string GetLayerString(int _layer) => (_layer + 1).ToString();

        public static Color GetLayerColor(int _layer) => _layer < EditorManager.inst.layerColors.Count ? EditorManager.inst.layerColors[_layer] : Color.white;

        public void SetLayer(LayerType layerType)
        {
            this.layerType = layerType;
            Layer = 0;
            SetLayer(Layer);
        }

        public void SetLayer(int layer, bool setHistory = true)
        {
            DataManager.inst.UpdateSettingInt("EditorLayer", layer);
            int oldLayer = Layer;

            Layer = layer;
            TimelineOverlayImage.color = GetLayerColor(layer);
            LayersImage.color = GetLayerColor(layer);

            layersIF.onValueChanged.RemoveAllListeners();
            layersIF.text = (layer + 1).ToString();
            layersIF.onValueChanged.AddListener(delegate (string _value)
            {
                if (int.TryParse(_value, out int num))
                    SetLayer(Mathf.Clamp(num - 1, 0, int.MaxValue));
            });

            if (LayerToggle)
            {
                LayerToggle.onValueChanged.ClearAll();
                LayerToggle.isOn = layerType == LayerType.Events;
                LayerToggle.onValueChanged.AddListener(delegate (bool _val)
                {
                    SetLayer(_val ? LayerType.Events : LayerType.Objects);
                });
            }

            RTEventEditor.inst.SetEventActive(layerType == LayerType.Events);

            if (prevLayer != layer || prevLayerType != layerType)
            {
                switch (layerType)
                {
                    case LayerType.Objects:
                        {
                            ObjectEditor.inst.RenderTimelineObjects();

                            if (CheckpointEditor.inst.checkpoints.Count > 0)
                            {
                                foreach (var obj2 in CheckpointEditor.inst.checkpoints)
                                    Destroy(obj2);

                                CheckpointEditor.inst.checkpoints.Clear();
                            }

                            CheckpointEditor.inst.CreateGhostCheckpoints();

                            LayerToggle?.SetIsOn(false);

                            break;
                        }
                    case LayerType.Events:
                        {
                            RTEventEditor.inst.RenderEventObjects();
                            CheckpointEditor.inst.CreateCheckpoints();

                            RTEventEditor.inst.RenderLayerBins();

                            LayerToggle?.SetIsOn(true);

                            break;
                        }
                }
            }

            prevLayerType = layerType;
            prevLayer = layer;

            int tmpLayer = Layer;
            if (setHistory)
            {
                EditorManager.inst.history.Add(new History.Command("Change Layer", delegate ()
                {
                    Debug.LogFormat("{0}Redone layer: {1}", EditorPlugin.className, tmpLayer);
                    SetLayer(tmpLayer, false);
                }, delegate ()
                {
                    Debug.LogFormat("{0}Undone layer: {1}", EditorPlugin.className, oldLayer);
                    SetLayer(oldLayer, false);
                }), false);
            }
        }

        #endregion

        #region Generate UI

        public void SetupTimelineBar()
        {
            var __instance = EditorManager.inst;

            timelineBar = GameObject.Find("TimelineBar/GameObject");

            for (int i = 1; i <= 5; i++)
                timelineBar.transform.Find(i.ToString()).gameObject.SetActive(false);

            Destroy(GameObject.Find("TimelineBar/GameObject/6").GetComponent<EventTrigger>());

            var eventToggle = GameObject.Find("TimelineBar/GameObject/6").GetComponent<Toggle>();
            eventToggle.onValueChanged.ClearAll();
            eventToggle.onValueChanged.AddListener(delegate (bool _val)
            {
                if (_val)
                    layerType = LayerType.Events;
                else
                    layerType = LayerType.Objects;
            });

            timelineBar.transform.GetChild(0).gameObject.name = "Time Default";

            var t = timelineBar.transform.Find("Time");
            defaultIF = t.gameObject;
            defaultIF.SetActive(true);
            t.SetParent(null);
            __instance.speedText.transform.parent.SetParent(null);

            if (defaultIF.TryGetComponent(out InputField frick))
            {
                frick.textComponent.fontSize = 19;
            }

            var timeObj = Instantiate(t.gameObject);
            {
                timeObj.transform.SetParent(timelineBar.transform);
                timeObj.transform.localScale = Vector3.one;
                timeObj.name = "Time Input";

                //timelineBar.transform.GetChild(0).gameObject.SetActive(true);
                timeIF = timeObj.GetComponent<InputField>();

                //Triggers.AddTooltip(timeObj, "Shows the exact current time of song.", "Type in the input field to go to a precise time in the level.");

                timeObj.transform.SetAsFirstSibling();
                timeObj.SetActive(true);
                ((Text)timeIF.placeholder).text = "Set time...";
                ((Text)timeIF.placeholder).alignment = TextAnchor.MiddleCenter;
                ((Text)timeIF.placeholder).fontSize = 16;
                ((Text)timeIF.placeholder).horizontalOverflow = HorizontalWrapMode.Overflow;
                timeIF.text = AudioManager.inst.CurrentAudioSource.time.ToString();
                timeIF.characterValidation = InputField.CharacterValidation.Decimal;

                timeIF.onValueChanged.AddListener(delegate (string _value)
                {
                    if (float.TryParse(_value, out float num))
                    {
                        AudioManager.inst.CurrentAudioSource.time = num;
                    }
                });

                TriggerHelper.AddEventTrigger(timeObj, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(timeIF) });
            }

            var layersObj = Instantiate(timeObj);
            {
                layersObj.transform.SetParent(timelineBar.transform);
                layersObj.name = "layers";
                layersObj.transform.SetSiblingIndex(8);
                layersObj.transform.localScale = Vector3.one;

                for (int i = 0; i < layersObj.transform.childCount; i++)
                {
                    layersObj.transform.GetChild(i).localScale = Vector3.one;
                }
                //layersObj.GetComponent<HoverTooltip>().tooltipLangauges.Add(Triggers.NewTooltip("Input any positive number to go to that editor layer.", "Layers will only show specific objects that are on that layer. Can be good to use for organizing levels.", new List<string> { "Middle Mouse Button" }));

                layersIF = layersObj.GetComponent<InputField>();
                layersObj.transform.Find("Text").gameObject.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;

                layersIF.text = GetLayerString(EditorManager.inst.layer);

                var layerImage = layersObj.GetComponent<Image>();

                layersIF.characterValidation = InputField.CharacterValidation.None;
                layersIF.contentType = InputField.ContentType.Standard;
                ((Text)layersIF.placeholder).text = "Set layer...";
                ((Text)layersIF.placeholder).alignment = TextAnchor.MiddleCenter;
                ((Text)layersIF.placeholder).fontSize = 16;
                ((Text)layersIF.placeholder).horizontalOverflow = HorizontalWrapMode.Overflow;
                layersIF.onValueChanged.RemoveAllListeners();
                layersIF.onValueChanged.AddListener(delegate (string _value)
                {
                    if (int.TryParse(_value, out int num))
                    {
                        SetLayer(Mathf.Clamp(num - 1, 0, int.MaxValue));
                    }
                });

                layerImage.color = GetLayerColor(EditorManager.inst.layer);

                TriggerHelper.AddEventTriggerParams(layersObj,
                    TriggerHelper.ScrollDeltaInt(layersIF, 1, 1, int.MaxValue), TriggerHelper.CreateEntry(EventTriggerType.PointerDown, delegate (BaseEventData eventData)
                    {
                        var pointerEventData = (PointerEventData)eventData;
                        if (pointerEventData.button == PointerEventData.InputButton.Middle)
                        {
                            EditorPlugin.ListObjectLayers();
                        }
                    }));
            }

            var pitchObj = Instantiate(timeObj);
            {
                pitchObj.transform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject").transform);
                pitchObj.transform.SetSiblingIndex(5);
                pitchObj.name = "pitch";
                pitchObj.transform.localScale = Vector3.one;

                pitchIF = pitchObj.GetComponent<InputField>();
                ((Text)pitchIF.placeholder).text = "Pitch";
                ((Text)pitchIF.placeholder).alignment = TextAnchor.MiddleCenter;
                ((Text)pitchIF.placeholder).fontSize = 16;
                ((Text)pitchIF.placeholder).horizontalOverflow = HorizontalWrapMode.Overflow;
                pitchIF.onValueChanged.RemoveAllListeners();
                pitchIF.onValueChanged.AddListener(delegate (string _val)
                {
                    if (float.TryParse(_val, out float num))
                    {
                        AudioManager.inst.SetPitch(num);
                    }
                    else
                    {
                        EditorManager.inst.DisplayNotification("Input is not correct format!", 1f, EditorManager.NotificationType.Error);
                    }
                });

                TriggerHelper.AddEventTrigger(pitchObj, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(pitchIF, 0.1f, 10f) });

                //Triggers.AddTooltip(pitchObj, "Change the pitch of the song", "", new List<string> { "Up / Down Arrow" }, clear: true);

                pitchObj.GetComponent<LayoutElement>().minWidth = 64f;
                pitchObj.transform.Find("Text").GetComponent<Text>().alignment = TextAnchor.MiddleCenter;

                pitchObj.AddComponent<InputFieldSwapper>();
            }
        }

        public bool isOverMainTimeline;
        public void SetupTimelineTriggers()
        {
            var isOver = new EventTrigger.Entry();
            isOver.eventID = EventTriggerType.PointerEnter;
            isOver.callback.AddListener(delegate (BaseEventData eventData)
            {
                isOverMainTimeline = true;
            });

            var isNotOver = new EventTrigger.Entry();
            isNotOver.eventID = EventTriggerType.PointerExit;
            isNotOver.callback.AddListener(delegate (BaseEventData eventData)
            {
                isOverMainTimeline = false;
            });

            var tltrig = EditorManager.inst.timeline.GetComponent<EventTrigger>();

            tltrig.triggers.Add(isOver);
            tltrig.triggers.Add(isNotOver);
            tltrig.triggers.Add(TriggerHelper.EndDragTrigger());

            if (DataManager.inst != null)
            {
                for (int i = 0; i < EventEditor.inst.EventHolders.transform.childCount - 1; i++)
                {
                    var et = EventEditor.inst.EventHolders.transform.GetChild(i).GetComponent<EventTrigger>();
                    et.triggers.Clear();
                    et.triggers.Add(isOver);
                    et.triggers.Add(isNotOver);
                    et.triggers.Add(TriggerHelper.StartDragTrigger());
                    et.triggers.Add(TriggerHelper.DragTrigger());
                    et.triggers.Add(TriggerHelper.EndDragTrigger());

                    int typeTmp = i;
                    var entry3 = new EventTrigger.Entry();
                    entry3.eventID = EventTriggerType.PointerDown;
                    entry3.callback.AddListener(delegate (BaseEventData eventData)
                    {
                        var layer = Layer + 1;
                        int max = RTEventEditor.EventLimit * layer;
                        int min = max - RTEventEditor.EventLimit;

                        Debug.Log($"{EditorPlugin.className}EventHolder: {typeTmp}\nMax: {max}\nMin: {min}\nCurrent Event: {min + typeTmp}");
                        if (((PointerEventData)eventData).button == PointerEventData.InputButton.Right)
                        {
                            if (RTEventEditor.EventTypes.Length > min + typeTmp && DataManager.inst.gameData.eventObjects.allEvents.Count > min + typeTmp)
                                RTEventEditor.inst.NewKeyframeFromTimeline(min + typeTmp);
                        }
                    });
                    et.triggers.Add(entry3);
                }
            }
        }

        public void SetupSelectGUI()
        {
            var __instance = EditorManager.inst;

            var openFilePopup = __instance.GetDialog("Open File Popup").Dialog;
            var newFilePopup = __instance.GetDialog("New File Popup").Dialog;
            var parentSelector = __instance.GetDialog("Parent Selector").Dialog;
            var saveAsPopup = __instance.GetDialog("Save As Popup").Dialog;
            var quickActionsPopup = __instance.GetDialog("Quick Actions Popup").Dialog;
            var prefabPopup = __instance.GetDialog("Prefab Popup").Dialog;

            var openFilePopupSelect = openFilePopup.gameObject.AddComponent<SelectGUI>();
            openFilePopupSelect.target = openFilePopup;
            openFilePopupSelect.ogPos = openFilePopup.position;

            var parentSelectorSelect = parentSelector.gameObject.AddComponent<SelectGUI>();
            parentSelectorSelect.target = parentSelector;
            parentSelectorSelect.ogPos = parentSelector.position;

            var saveAsPopupSelect = saveAsPopup.Find("New File Popup").gameObject.AddComponent<SelectGUI>();
            saveAsPopupSelect.target = saveAsPopup;
            saveAsPopupSelect.ogPos = saveAsPopup.position;

            var quickActionsPopupSelect = quickActionsPopup.gameObject.AddComponent<SelectGUI>();
            quickActionsPopupSelect.target = quickActionsPopup;
            quickActionsPopupSelect.ogPos = quickActionsPopup.position;
        }

        public void SetupCreateObjects()
        {
            var __instance = EditorManager.inst;

            var persistent = __instance.GetDialog("Object Options Popup").Dialog.Find("persistent").gameObject.GetComponent<Button>();
            EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("persistent/text").gameObject.GetComponent<Text>().text = "No Autokill";
            persistent.onClick.ClearAll();
            persistent.onClick.AddListener(delegate ()
            {
                ObjectEditor.inst.CreateNewNoAutokillObject();
            });

            var empty = __instance.GetDialog("Object Options Popup").Dialog.Find("empty").gameObject.GetComponent<Button>();
            empty.onClick.ClearAll();
            empty.onClick.AddListener(delegate ()
            {
                ObjectEditor.inst.CreateNewEmptyObject();
            });

            var decoration = __instance.GetDialog("Object Options Popup").Dialog.Find("decoration").gameObject.GetComponent<Button>();
            decoration.onClick.ClearAll();
            decoration.onClick.AddListener(delegate ()
            {
                ObjectEditor.inst.CreateNewDecorationObject();
            });

            var helper = __instance.GetDialog("Object Options Popup").Dialog.Find("helper").gameObject.GetComponent<Button>();
            helper.onClick.ClearAll();
            helper.onClick.AddListener(delegate ()
            {
                ObjectEditor.inst.CreateNewHelperObject();
            });

            var normal = __instance.GetDialog("Object Options Popup").Dialog.Find("normal").gameObject.GetComponent<Button>();
            normal.onClick.ClearAll();
            normal.onClick.AddListener(delegate ()
            {
                ObjectEditor.inst.CreateNewNormalObject();
            });

            var circle = __instance.GetDialog("Object Options Popup").Dialog.Find("shapes/circle").gameObject.GetComponent<Button>();
            circle.onClick.ClearAll();
            circle.onClick.AddListener(delegate ()
            {
                ObjectEditor.inst.CreateNewCircleObject();
            });

            var triangle = __instance.GetDialog("Object Options Popup").Dialog.Find("shapes/triangle").gameObject.GetComponent<Button>();
            triangle.onClick.ClearAll();
            triangle.onClick.AddListener(delegate ()
            {
                ObjectEditor.inst.CreateNewTriangleObject();
            });

            var text = __instance.GetDialog("Object Options Popup").Dialog.Find("shapes/text").gameObject.GetComponent<Button>();
            text.onClick.ClearAll();
            text.onClick.AddListener(delegate ()
            {
                ObjectEditor.inst.CreateNewTextObject();
            });

            var hexagon = __instance.GetDialog("Object Options Popup").Dialog.Find("shapes/hexagon").gameObject.GetComponent<Button>();
            hexagon.onClick.ClearAll();
            hexagon.onClick.AddListener(delegate ()
            {
                ObjectEditor.inst.CreateNewHexagonObject();
            });
        }

        public void SetupDropdowns()
        {
            titleBar = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar").transform;

            EditorManager.inst.GetDialog("Save As Popup").Dialog.Find("New File Popup/level-name").GetComponent<InputField>().characterValidation = InputField.CharacterValidation.None;
            GameObject.Find("Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/left/theme/name").GetComponent<InputField>().characterValidation = InputField.CharacterValidation.None;
            GameObject.Find("Editor GUI/sizer/main/EditorDialogs/PrefabDialog/data/name/input").GetComponent<InputField>().characterValidation = InputField.CharacterValidation.None;
            GameObject.Find("Editor GUI/sizer/main/Popups/New File Popup/Browser Popup").SetActive(true);

            if (ModCompatibility.ArcadiaCustomsInstalled)
                EditorHelper.AddEditorDropdown("Quit to Arcade", "", "File", titleBar.Find("File/File Dropdown/Quit to Main Menu/Image").GetComponent<Image>().sprite, delegate ()
                {
                    EditorManager.inst.ShowDialog("Warning Popup");
                    RefreshWarningPopup("Are you sure you want to quit to the arcade?", delegate ()
                    {
                        DG.Tweening.DOTween.Clear();
                        Updater.UpdateObjects(false);
                        DataManager.inst.gameData = null;
                        DataManager.inst.gameData = new DataManager.GameData();

                        ArcadeManager.inst.skippedLoad = false;
                        ArcadeManager.inst.forcedSkip = false;
                        DataManager.inst.UpdateSettingBool("IsArcade", true);

                        SceneManager.inst.LoadScene("Input Select");
                    }, delegate ()
                    {
                        EditorManager.inst.HideDialog("Warning Popup");
                    });
                }, 7);

            EditorHelper.AddEditorDropdown("Switch to Arcade Mode", "", "File", SpriteManager.LoadSprite(RTFile.ApplicationDirectory + RTFunctions.FunctionsPlugin.BepInExAssetsPath + "editor_gui_right_small.png"), delegate ()
            {
                if (EditorManager.inst.hasLoadedLevel)
                {
                    LevelManager.OnLevelEnd = delegate ()
                    {
                        DG.Tweening.DOTween.Clear();
                        DataManager.inst.gameData = null;
                        DataManager.inst.gameData = new GameData();
                        Updater.OnLevelEnd();
                        SceneManager.inst.LoadScene("Editor");
                    };
                    LevelManager.Load(GameManager.inst.basePath + "level.lsb", false);
                }
                else
                {
                    EditorManager.inst.DisplayNotification("Load a level before switching to Arcade Mode!", 2f, EditorManager.NotificationType.Error);
                }

            }, 7);
            
            EditorHelper.AddEditorDropdown("Open Level Browser", "", "File", titleBar.Find("File/File Dropdown/Open/Image").GetComponent<Image>().sprite, delegate ()
            {
                EditorManager.inst.ShowDialog("Browser Popup");
                RefreshFileBrowserLevels();
            }, 3);

            EditorHelper.AddEditorDropdown("Convert VG to LS", "", "File", SearchSprite, delegate ()
            {
                EditorManager.inst.ShowDialog("Browser Popup");
                RTFileBrowser.inst.UpdateBrowser(Directory.GetCurrentDirectory(), new string[] { ".lsp", ".vgp", "lst", ".vgt", ".lsb", ".vgd" }, onSelectFile: delegate (string _val)
                {
                    bool failed = false;
                    if (_val.Contains(".lsp"))
                    {
                        var file = RTFile.ApplicationDirectory + prefabListSlash + Path.GetFileName(_val);
                        File.Copy(_val, file, RTFile.FileExists(file));
                        EditorManager.inst.DisplayNotification($"Copied {Path.GetFileName(_val)} to prefab ({prefabListPath}) folder.", 2f, EditorManager.NotificationType.Success);
                    }
                    else if (_val.Contains(".vgp"))
                    {
                        try
                        {
                            var file = RTFile.ReadFromFile(_val);

                            var vgjn = JSON.Parse(file);

                            var prefab = Prefab.ParseVG(vgjn);

                            var jn = prefab.ToJSON();

                            if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + prefabListPath))
                                Directory.CreateDirectory(RTFile.ApplicationDirectory + prefabListPath);

                            string fileName = $"{prefab.Name.ToLower().Replace(" ", "_")}.lsp";
                            RTFile.WriteToFile(RTFile.ApplicationDirectory + prefabListSlash + fileName, jn.ToString());

                            file = null;
                            vgjn = null;
                            prefab = null;
                            jn = null;

                            EditorManager.inst.DisplayNotification($"Successfully converted {Path.GetFileName(_val)} to {fileName} and added it to your prefab ({prefabListPath}) folder.", 2f,
                                EditorManager.NotificationType.Success);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError(ex);
                            failed = true;
                        }
                    }
                    else if (_val.Contains(".lst"))
                    {
                        var file = RTFile.ApplicationDirectory + themeListSlash + Path.GetFileName(_val);
                        File.Copy(_val, file, RTFile.FileExists(file));
                        EditorManager.inst.DisplayNotification($"Copied {Path.GetFileName(_val)} to theme ({themeListPath}) folder.", 2f, EditorManager.NotificationType.Success);
                    }
                    else if (_val.Contains(".vgt"))
                    {
                        try
                        {
                            var file = RTFile.ReadFromFile(_val);

                            var vgjn = JSON.Parse(file);

                            var theme = BeatmapTheme.ParseVG(vgjn);

                            var jn = theme.ToJSON();

                            if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + themeListPath))
                                Directory.CreateDirectory(RTFile.ApplicationDirectory + themeListPath);

                            var fileName = $"{theme.name.ToLower().Replace(" ", "_")}.lst";
                            RTFile.WriteToFile(RTFile.ApplicationDirectory + themeListSlash + fileName, jn.ToString());

                            file = null;
                            vgjn = null;
                            theme = null;
                            jn = null;

                            EditorManager.inst.DisplayNotification($"Successfully converted {Path.GetFileName(_val)} to {fileName} and added it to your theme ({themeListPath}) folder.", 2f,
                                EditorManager.NotificationType.Success);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError(ex);
                            failed = true;
                        }
                    }
                    else if (_val.Replace("\\", "/").Contains("/level.lsb"))
                    {
                        EditorManager.inst.ShowDialog("Warning Popup");
                        RefreshWarningPopup("Warning! Selecting a level will copy all of its contents to your editor, are you sure you want to do this?", delegate ()
                        {
                            var path = _val.Replace("\\", "/").Replace("/level.lsb", "");

                            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);

                            foreach (var file in files)
                            {
                                var copyTo = file.Replace("\\", "/").Replace(Path.GetDirectoryName(path), RTFile.ApplicationDirectory + editorListPath);
                                File.Copy(file, copyTo, RTFile.FileExists(copyTo));
                            }

                            EditorManager.inst.DisplayNotification($"Copied {Path.GetFileName(path)} to level ({editorListPath}) folder.", 2f, EditorManager.NotificationType.Success);

                            EditorManager.inst.HideDialog("Warning Popup");
                        }, delegate ()
                        {
                            EditorManager.inst.HideDialog("Warning Popup");
                            EditorManager.inst.ShowDialog("Browser Popup");
                        });
                    }
                    else if (_val.Replace("\\", "/").Contains("/level.vgd"))
                    {
                        try
                        {
                            var path = _val.Replace("\\", "/").Replace("/level.vgd", "");

                            if (RTFile.FileExists(path + "/metadata.vgm") && RTFile.FileExists(path + "/audio.ogg") && RTFile.FileExists(path + "/cover.jpg"))
                            {
                                var copyTo = path.Replace(Path.GetDirectoryName(path).Replace("\\", "/"), RTFile.ApplicationDirectory + editorListPath);

                                if (!RTFile.DirectoryExists(copyTo))
                                    Directory.CreateDirectory(copyTo);

                                var metadataVGJSON = RTFile.ReadFromFile(path + "/metadata.vgm");

                                var metadataVGJN = JSON.Parse(metadataVGJSON);

                                var metadata = MetaData.ParseVG(metadataVGJN);

                                var metadataJN = metadata.ToJSON();

                                RTFile.WriteToFile(copyTo + "/metadata.lsb", metadataJN.ToString());

                                File.Copy(path + "/audio.ogg", copyTo + "/level.ogg", RTFile.FileExists(copyTo + "/level.ogg"));

                                File.Copy(path + "/cover.jpg", copyTo + "/level.jpg", RTFile.FileExists(copyTo + "/level.jpg"));

                                var levelVGJSON = RTFile.ReadFromFile(path + "/level.vgd");

                                var levelVGJN = JSON.Parse(levelVGJSON);

                                var level = GameData.ParseVG(levelVGJN, false);

                                StartCoroutine(ProjectData.Writer.SaveData(copyTo + "/level.lsb", level, delegate ()
                                {
                                    EditorManager.inst.DisplayNotification($"Successfully converted {Path.GetFileName(path)} to {Path.GetFileName(copyTo)} and added it to your level ({editorListPath}) folder.", 2f,
                                        EditorManager.NotificationType.Success);

                                    metadataVGJSON = null;
                                    metadataVGJN = null;
                                    metadata = null;
                                    metadataJN = null;
                                    levelVGJSON = null;
                                    levelVGJN = null;
                                    level = null;
                                }));
                            }
                            else
                            {
                                EditorManager.inst.DisplayNotification("Could not convert since some needed files are missing!", 2f, EditorManager.NotificationType.Error);
                                failed = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError(ex);
                            failed = true;
                        }
                    }
                    else if (_val.Replace("\\", "/").Contains("/autosave_") && _val.Contains(".vgd"))
                    {
                        EditorManager.inst.DisplayNotification("Cannot select autosave.", 2f, EditorManager.NotificationType.Warning);
                        failed = true;
                    }

                    if (!failed)
                        EditorManager.inst.HideDialog("Browser Popup");
                });
            }, 4);

            EditorHelper.AddEditorDropdown("Add File to Level Folder", "", "File", SearchSprite, delegate ()
            {
                EditorManager.inst.ShowDialog("Browser Popup");
                RTFileBrowser.inst.UpdateBrowser(Directory.GetCurrentDirectory(), new string[] { ".ogg", ".wav", ".png", ".jpg", ".mp4", ".mov" }, onSelectFile: delegate (string _val)
                {
                    if (_val.Contains(".mp4") || _val.Contains(".mov"))
                    {
                        var copyTo = _val.Replace("\\", "/").Replace((Path.GetDirectoryName(_val) + "/").Replace("\\", "/"), RTFile.BasePath).Replace(Path.GetFileName(_val),
                            _val.Contains(".mp4") ? "bg.mp4" : "bg.mov");
                        File.Copy(_val, copyTo, RTFile.FileExists(copyTo));

                        if (RTFile.FileExists(copyTo) && RTFunctions.FunctionsPlugin.EnableVideoBackground.Value)
                        {
                            RTVideoManager.inst.Play(copyTo, 1f);
                        }
                        else
                        {
                            RTVideoManager.inst.Stop();
                        }
                        return;
                    }

                    var destination = _val.Replace("\\", "/").Replace((Path.GetDirectoryName(_val) + "/").Replace("\\", "/"), RTFile.BasePath);
                    File.Copy(_val, destination, RTFile.FileExists(destination));
                });
            }, 5);

            if (ModCompatibility.mods.ContainsKey("ExampleCompanion"))
            {
                var exitToArcade = Instantiate(titleBar.Find("File/File Dropdown/Quit to Main Menu").gameObject);
                exitToArcade.name = "Get Example";
                exitToArcade.transform.SetParent(titleBar.Find("View/View Dropdown"));
                exitToArcade.transform.localScale = Vector3.one;
                exitToArcade.transform.SetSiblingIndex(4);
                exitToArcade.transform.GetChild(0).GetComponent<Text>().text = "Get Example";

                var ex = exitToArcade.GetComponent<Button>();
                ex.onClick.ClearAll();
                ex.onClick.AddListener(delegate ()
                {
                    if (ModCompatibility.mods["ExampleCompanion"].Methods.ContainsKey("InitExample"))
                        ModCompatibility.mods["ExampleCompanion"].Invoke("InitExample", new object[] { });
                });
            }

            titleBar.Find("Help/Help Dropdown/Join Discord/Text").GetComponent<Text>().text = "Modder's Discord";
            titleBar.Find("Help/Help Dropdown/Watch Tutorials/Text").GetComponent<Text>().text = "Watch PA History";
            titleBar.Find("Help/Help Dropdown/Community Guides").gameObject.SetActive(false);
            titleBar.Find("Help/Help Dropdown/Which songs can I use?").gameObject.SetActive(false);
            titleBar.Find("File/File Dropdown/Save As").gameObject.SetActive(true);
        }

        public void SetupDoggo()
        {
            var loading = new GameObject("loading");
            var fileInfoPopup = EditorManager.inst.GetDialog("File Info Popup").Dialog;

            fileInfoText = fileInfoPopup.Find("text").GetComponent<Text>();

            loading.transform.SetParent(fileInfoPopup);
            loading.layer = 5;
            loading.transform.localScale = Vector3.one;

            var rtLoading = loading.AddComponent<RectTransform>();
            loading.AddComponent<CanvasRenderer>();
            var iLoading = loading.AddComponent<Image>();
            var leLoading = loading.AddComponent<LayoutElement>();

            rtLoading.anchoredPosition = new Vector2(0f, -75f);
            rtLoading.sizeDelta = new Vector2(122f, 122f);
            iLoading.sprite = EditorManager.inst.loadingImage.sprite;
            leLoading.ignoreLayout = true;

            fileInfoPopup.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(500f, 280f);

            fileInfoPopup.gameObject.GetComponent<Image>().sprite = null;
        }

        public void SetupPaths()
        {
            var sortList = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/autokill/tod-dropdown")
                .Duplicate(EditorManager.inst.GetDialog("Open File Popup").Dialog);

            var sortListRT = sortList.GetComponent<RectTransform>();
            sortListRT.anchoredPosition = GetEditorProperty("Open Level Dropdown Position").GetConfigEntry<Vector2>().Value;
            var sortListTip = sortList.GetComponent<HoverTooltip>();
            {
                sortListTip.tooltipLangauges[0].desc = "Sort the order of your levels.";
                sortListTip.tooltipLangauges[0].hint = "<b>Cover</b> Sort by if level has a set cover. (Default)" +
                    "<br><b>Artist</b> Sort by song artist." +
                    "<br><b>Creator</b> Sort by level creator." +
                    "<br><b>Folder</b> Sort by level folder name." +
                    "<br><b>Title</b> Sort by song title." +
                    "<br><b>Difficulty</b> Sort by level difficulty." +
                    "<br><b>Date Edited</b> Sort by date edited / created.";
            }

            var sortListDD = sortList.GetComponent<Dropdown>();
            Destroy(sortList.GetComponent<HideDropdownOptions>());
            sortListDD.options.Clear();
            sortListDD.onValueChanged.RemoveAllListeners();

            sortListDD.options = new List<Dropdown.OptionData>
            {
                new Dropdown.OptionData("Cover"),
                new Dropdown.OptionData("Artist"),
                new Dropdown.OptionData("Creator"),
                new Dropdown.OptionData("Folder"),
                new Dropdown.OptionData("Title"),
                new Dropdown.OptionData("Difficulty"),
                new Dropdown.OptionData("Date Edited")
            };

            sortListDD.onValueChanged.AddListener(delegate (int _value)
            {
                levelFilter = _value;
                StartCoroutine(RefreshLevelList());
            });

            var checkDes = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/toggle")
                .Duplicate(EditorManager.inst.GetDialog("Open File Popup").Dialog);

            var checkDesRT = checkDes.GetComponent<RectTransform>();
            checkDesRT.anchoredPosition = GetEditorProperty("Open Level Toggle Position").GetConfigEntry<Vector2>().Value;

            checkDes.transform.Find("title").GetComponent<Text>().enabled = false;
            var titleRT = checkDes.transform.Find("title").GetComponent<RectTransform>();
            titleRT.sizeDelta = new Vector2(110f, 32f);

            var toggle = checkDes.transform.Find("toggle").GetComponent<Toggle>();
            toggle.isOn = true;
            toggle.onValueChanged.AddListener(delegate (bool _value)
            {
                levelAscend = _value;
                StartCoroutine(RefreshLevelList());
            });

            if (toggle.gameObject)
                TooltipHelper.AddTooltip(toggle.gameObject, new List<HoverTooltip.Tooltip> { sortListTip.tooltipLangauges[0] });

            CreateGlobalSettings();
            LoadGlobalSettings();

            // Editor Path
            {
                var editorPathGO = GameObject.Find("TimelineBar/GameObject/Time Input")
                    .Duplicate(EditorManager.inst.GetDialog("Open File Popup").Dialog, "editor path");
                ((RectTransform)editorPathGO.transform).anchoredPosition = GetEditorProperty("Open Level Editor Path Pos").GetConfigEntry<Vector2>().Value;
                ((RectTransform)editorPathGO.transform).sizeDelta = new Vector2(GetEditorProperty("Open Level Editor Path Length").GetConfigEntry<float>().Value, 34f);

                var levelListTip = editorPathGO.GetComponent<HoverTooltip>();
                if (!levelListTip)
                    levelListTip = editorPathGO.AddComponent<HoverTooltip>();

                var llTip = new HoverTooltip.Tooltip();

                llTip.desc = "Level list path";
                llTip.hint = "Input the path you want to load levels from within the beatmaps folder. For example: inputting \"editor\" into the input field will load levels from beatmaps/editor. You can also set it to sub-directories, like: \"editor/pa levels\" will take levels from \"beatmaps/editor/pa levels\".";

                levelListTip.tooltipLangauges.Add(llTip);

                var editorPathIF = editorPathGO.GetComponent<InputField>();
                editorPathIF.characterValidation = InputField.CharacterValidation.None;
                editorPathIF.text = EditorPath;

                editorPathIF.onValueChanged.RemoveAllListeners();
                editorPathIF.onValueChanged.AddListener(delegate (string _val)
                {
                    EditorPath = _val;
                });

                editorPathIF.onEndEdit.ClearAll();
                editorPathIF.onEndEdit.AddListener(delegate (string _val)
                {
                    if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + editorListPath))
                    {
                        Directory.CreateDirectory(RTFile.ApplicationDirectory + editorListPath);
                    }
                    SaveGlobalSettings();
                });

                var clickable = editorPathGO.AddComponent<Clickable>();
                clickable.onDown = delegate (PointerEventData pointerEventData)
                {
                    if (pointerEventData.button == PointerEventData.InputButton.Right)
                    {
                        EditorManager.inst.ShowDialog("Browser Popup");
                        RTFileBrowser.inst.UpdateBrowser(Directory.GetCurrentDirectory(), onSelectFolder: delegate (string _val)
                        {
                            if (_val.Replace("\\", "/").Contains(RTFile.ApplicationDirectory + "beatmaps/"))
                            {
                                editorPathIF.text = _val.Replace("\\", "/").Replace(RTFile.ApplicationDirectory.Replace("\\", "/") + "beatmaps/", "");
                                EditorManager.inst.DisplayNotification($"Set Editor path to {EditorPath}!", 2f, EditorManager.NotificationType.Success);
                                EditorManager.inst.HideDialog("Browser Popup");
                            }
                            else
                            {
                                EditorManager.inst.DisplayNotification($"Path does not contain the proper directory.", 2f, EditorManager.NotificationType.Warning);
                            }
                        });
                    }
                };

                var levelListReloader = GameObject.Find("TimelineBar/GameObject/play")
                    .Duplicate(EditorManager.inst.GetDialog("Open File Popup").Dialog, "reload");
                ((RectTransform)levelListReloader.transform).anchoredPosition = GetEditorProperty("Open Level List Refresh Position").GetConfigEntry<Vector2>().Value;
                ((RectTransform)levelListReloader.transform).sizeDelta = new Vector2(32f, 32f);

                var levelListRTip = levelListReloader.AddComponent<HoverTooltip>();
                var llRTip = new HoverTooltip.Tooltip();

                llRTip.desc = "Refresh level list";
                llRTip.hint = "Clicking this will reload the level list.";

                levelListRTip.tooltipLangauges.Add(llRTip);

                var levelListRButton = levelListReloader.GetComponent<Button>();
                levelListRButton.onClick.ClearAll();
                levelListRButton.onClick.AddListener(delegate ()
                {
                    EditorManager.inst.GetLevelList();
                });

                string jpgFileLocation = RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_refresh-white.png";

                if (RTFile.FileExists(jpgFileLocation))
                {
                    var spriteReloader = levelListReloader.GetComponent<Image>();

                    EditorManager.inst.StartCoroutine(EditorManager.inst.GetSprite(jpgFileLocation, new EditorManager.SpriteLimits(), delegate (Sprite cover)
                    {
                        spriteReloader.sprite = cover;
                    }, delegate (string errorFile)
                    {
                        spriteReloader.sprite = ArcadeManager.inst.defaultImage;
                    }));
                }
            }

            // Theme Path
            {
                var themePathSpacer = EditorManager.inst.GetDialog("Event Editor").Dialog.Find("data/right/theme").GetChild(2).gameObject
                    .Duplicate(EditorManager.inst.GetDialog("Event Editor").Dialog.Find("data/right/theme"), "themepathers", 8);

                var themePathGO = timeIF.gameObject.Duplicate(themePathSpacer.transform, "themes path");
                ((RectTransform)themePathGO.transform).anchoredPosition = new Vector2(150f, 0f);
                ((RectTransform)themePathGO.transform).sizeDelta = new Vector2(300f, 34f);

                var themePathTip = themePathGO.AddComponent<HoverTooltip>();
                var llTip = new HoverTooltip.Tooltip();

                llTip.desc = "Theme list path";
                llTip.hint = "Input the path you want to load themes from within the beatmaps folder. For example: inputting \"themes\" into the input field will load themes from beatmaps/themes. You can also set it to sub-directories, like: \"themes/pa colors\" will take levels from \"beatmaps/themes/pa colors\".";

                themePathTip.tooltipLangauges.Add(llTip);

                var themePathIF = themePathGO.GetComponent<InputField>();
                themePathIF.characterValidation = InputField.CharacterValidation.None;
                themePathIF.text = ThemePath;

                themePathIF.onValueChanged.RemoveAllListeners();
                themePathIF.onValueChanged.AddListener(delegate (string _val)
                {
                    ThemePath = _val;
                });

                themePathIF.onEndEdit.ClearAll();
                themePathIF.onEndEdit.AddListener(delegate (string _val)
                {
                    if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + themeListPath))
                    {
                        Directory.CreateDirectory(RTFile.ApplicationDirectory + themeListPath);
                    }
                    SaveGlobalSettings();
                });

                var clickable = themePathGO.AddComponent<Clickable>();
                clickable.onDown = delegate (PointerEventData pointerEventData)
                {
                    if (pointerEventData.button == PointerEventData.InputButton.Right)
                    {
                        EditorManager.inst.ShowDialog("Browser Popup");
                        RTFileBrowser.inst.UpdateBrowser(Directory.GetCurrentDirectory(), onSelectFolder: delegate (string _val)
                        {
                            if (_val.Replace("\\", "/").Contains(RTFile.ApplicationDirectory + "beatmaps/"))
                            {
                                themePathIF.text = _val.Replace("\\", "/").Replace(RTFile.ApplicationDirectory.Replace("\\", "/") + "beatmaps/", "");
                                EditorManager.inst.DisplayNotification($"Set Theme path to {ThemePath}!", 2f, EditorManager.NotificationType.Success);
                                EditorManager.inst.HideDialog("Browser Popup");
                            }
                            else
                            {
                                EditorManager.inst.DisplayNotification($"Path does not contain the proper directory.", 2f, EditorManager.NotificationType.Warning);
                            }
                        });
                    }
                };

                var themePathReloader = GameObject.Find("TimelineBar/GameObject/play").Duplicate(themePathSpacer.transform, "reload themes");
                ((RectTransform)themePathReloader.transform).anchoredPosition = new Vector2(310f, 35f);
                ((RectTransform)themePathReloader.transform).sizeDelta = new Vector2(32f, 32f);

                var levelListRTip = themePathReloader.AddComponent<HoverTooltip>();
                var llRTip = new HoverTooltip.Tooltip();

                llRTip.desc = "Refresh theme list";
                llRTip.hint = "Clicking this will reload the theme list.";

                levelListRTip.tooltipLangauges.Add(llRTip);

                var levelListRButton = themePathReloader.GetComponent<Button>();
                levelListRButton.onClick.ClearAll();
                levelListRButton.onClick.AddListener(delegate ()
                {
                    StartCoroutine(LoadThemes(true));
                    EventEditor.inst.RenderEventsDialog();
                });

                string jpgFileLocation = RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_refresh-white.png";

                if (RTFile.FileExists(jpgFileLocation))
                {
                    var spriteReloader = themePathReloader.GetComponent<Image>();

                    EditorManager.inst.StartCoroutine(EditorManager.inst.GetSprite(jpgFileLocation, new EditorManager.SpriteLimits(), delegate (Sprite cover)
                    {
                        spriteReloader.sprite = cover;
                    }, delegate (string errorFile)
                    {
                        spriteReloader.sprite = ArcadeManager.inst.defaultImage;
                    }));
                }
            }

            // Prefab Path
            {
                var prefabPathGO = timeIF.gameObject.Duplicate(EditorManager.inst.GetDialog("Prefab Popup").Dialog.Find("external prefabs"), "prefabs path");

                ((RectTransform)prefabPathGO.transform).anchoredPosition = GetEditorProperty("Prefab External Prefab Path Pos").GetConfigEntry<Vector2>().Value;
                ((RectTransform)prefabPathGO.transform).sizeDelta = new Vector2(GetEditorProperty("Prefab External Prefab Path Length").GetConfigEntry<float>().Value, 34f);

                var levelListTip = prefabPathGO.AddComponent<HoverTooltip>();
                var llTip = new HoverTooltip.Tooltip();

                llTip.desc = "Prefab list path";
                llTip.hint = "Input the path you want to load prefabs from within the beatmaps folder. For example: inputting \"prefabs\" into the input field will load levels from beatmaps/prefabs. You can also set it to sub-directories, like: \"prefabs/pa characters\" will take levels from \"beatmaps/prefabs/pa characters\".";

                levelListTip.tooltipLangauges.Add(llTip);

                var prefabPathIF = prefabPathGO.GetComponent<InputField>();
                prefabPathIF.characterValidation = InputField.CharacterValidation.None;
                prefabPathIF.text = PrefabPath;

                prefabPathIF.onValueChanged.RemoveAllListeners();
                prefabPathIF.onValueChanged.AddListener(delegate (string _val)
                {
                    PrefabPath = _val;
                });

                prefabPathIF.onEndEdit.ClearAll();
                prefabPathIF.onEndEdit.AddListener(delegate (string _val)
                {
                    if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + prefabListPath))
                    {
                        Directory.CreateDirectory(RTFile.ApplicationDirectory + prefabListPath);
                    }
                    SaveGlobalSettings();
                });

                var clickable = prefabPathGO.AddComponent<Clickable>();
                clickable.onDown = delegate (PointerEventData pointerEventData)
                {
                    if (pointerEventData.button == PointerEventData.InputButton.Right)
                    {
                        EditorManager.inst.ShowDialog("Browser Popup");
                        RTFileBrowser.inst.UpdateBrowser(Directory.GetCurrentDirectory(), onSelectFolder: delegate (string _val)
                        {
                            if (_val.Replace("\\", "/").Contains(RTFile.ApplicationDirectory + "beatmaps/"))
                            {
                                prefabPathIF.text = _val.Replace("\\", "/").Replace(RTFile.ApplicationDirectory.Replace("\\", "/") + "beatmaps/", "");
                                EditorManager.inst.DisplayNotification($"Set Prefab path to {PrefabPath}!", 2f, EditorManager.NotificationType.Success);
                                EditorManager.inst.HideDialog("Browser Popup");
                            }
                            else
                            {
                                EditorManager.inst.DisplayNotification($"Path does not contain the proper directory.", 2f, EditorManager.NotificationType.Warning);
                            }
                        });
                    }
                };

                var levelListReloader = GameObject.Find("TimelineBar/GameObject/play")
                    .Duplicate(EditorManager.inst.GetDialog("Prefab Popup").Dialog.Find("external prefabs"), "reload prefabs");
                ((RectTransform)levelListReloader.transform).anchoredPosition = GetEditorProperty("Prefab External Prefab Refresh Pos").GetConfigEntry<Vector2>().Value;
                ((RectTransform)levelListReloader.transform).sizeDelta = new Vector2(32f, 32f);

                var levelListRTip = levelListReloader.AddComponent<HoverTooltip>();
                var llRTip = new HoverTooltip.Tooltip();

                llRTip.desc = "Refresh prefab list";
                llRTip.hint = "Clicking this will reload the prefab list.";

                levelListRTip.tooltipLangauges.Add(llRTip);

                var levelListRButton = levelListReloader.GetComponent<Button>();
                levelListRButton.onClick.ClearAll();
                levelListRButton.onClick.AddListener(delegate ()
                {
                    StartCoroutine(UpdatePrefabs());
                });

                string jpgFileLocation = RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_refresh-white.png";

                if (RTFile.FileExists(jpgFileLocation))
                {
                    var spriteReloader = levelListReloader.GetComponent<Image>();

                    EditorManager.inst.StartCoroutine(EditorManager.inst.GetSprite(jpgFileLocation, new EditorManager.SpriteLimits(), delegate (Sprite cover)
                    {
                        spriteReloader.sprite = cover;
                    }, delegate (string errorFile)
                    {
                        spriteReloader.sprite = ArcadeManager.inst.defaultImage;
                    }));
                }
            }

            if (!ModCompatibility.sharedFunctions.ContainsKey("EditorOnLoadLevel"))
            {
                Action action = delegate () { };

                ModCompatibility.sharedFunctions.Add("EditorOnLoadLevel", action);
            }
            else
            {
                Action action = delegate () { };

                ModCompatibility.sharedFunctions["EditorOnLoadLevel"] = action;
            }
        }

        public void SetupFileBrowser()
        {
            var fileBrowser = EditorManager.inst.GetDialog("New File Popup").Dialog.Find("Browser Popup").gameObject.Duplicate(EditorManager.inst.GetDialog("New File Popup").Dialog.parent, "Browser Popup");
            fileBrowser.gameObject.SetActive(false);
            fileBrowser.transform.localPosition = Vector3.zero;
            ((RectTransform)fileBrowser.transform).anchoredPosition = Vector3.zero;
            ((RectTransform)fileBrowser.transform).anchorMax = new Vector2(0.5f, 0.5f);
            ((RectTransform)fileBrowser.transform).anchorMin = new Vector2(0.5f, 0.5f);
            ((RectTransform)fileBrowser.transform).pivot = new Vector2(0.5f, 0.5f);
            var close = fileBrowser.transform.Find("Panel/x").GetComponent<Button>();
            close.onClick.ClearAll();
            close.onClick.AddListener(delegate ()
            {
                EditorManager.inst.HideDialog("Browser Popup");
            });
            fileBrowser.transform.Find("GameObject").gameObject.SetActive(false);

            var selectGUI = fileBrowser.AddComponent<SelectGUI>();
            selectGUI.target = fileBrowser.transform;

            var rtfb = fileBrowser.AddComponent<RTFileBrowser>();
            var fileBrowserBase = fileBrowser.GetComponent<FileBrowserTest>();
            rtfb.viewport = fileBrowserBase.viewport;
            rtfb.backPrefab = fileBrowserBase.backPrefab;
            rtfb.folderPrefab = fileBrowserBase.folderPrefab;
            rtfb.folderBar = fileBrowserBase.folderBar;
            rtfb.oggFileInput = fileBrowserBase.oggFileInput;
            rtfb.filePrefab = fileBrowserBase.filePrefab;
            Destroy(fileBrowserBase);

            EditorHelper.AddEditorPopup("Browser Popup", fileBrowser);
        }

        public void SetupTimelinePreview()
        {
            GameManager.inst.playerGUI.transform.Find("Interface").gameObject.SetActive(false);
            var gui = GameManager.inst.playerGUI.Duplicate(EditorManager.inst.dialogs.parent);
            GameManager.inst.playerGUI.transform.Find("Interface").gameObject.SetActive(true);
            gui.transform.SetSiblingIndex(0);

            Destroy(gui.transform.Find("Health").gameObject);
            Destroy(gui.transform.Find("Interface").gameObject);

            gui.transform.localPosition = new Vector3(-382.5f, 184.05f, 0f);
            gui.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

            gui.SetActive(true);
            timelinePreview = gui.transform.Find("Timeline");
            timelinePosition = timelinePreview.Find("Base/position").GetComponent<RectTransform>();

            timelinePreviewPlayer = timelinePreview.Find("Base/position").GetComponent<Image>();
            timelinePreviewLeftCap = timelinePreview.Find("Base/Image").GetComponent<Image>();
            timelinePreviewRightCap = timelinePreview.Find("Base/Image 1").GetComponent<Image>();
            timelinePreviewLine = timelinePreview.Find("Base").GetComponent<Image>();
        }

        public void SetupTimelineElements()
        {
            Debug.Log($"{EditorPlugin.className}Setting Timeline Cursor Colors");
            try
            {
                if (RTExtensions.TryFind("Editor Systems/Editor GUI/sizer/main/whole-timeline/Slider_Parent/Slider/Handle Slide Area/Image/Handle", out GameObject gm) && gm.TryGetComponent(out Image image))
                {
                    timelineSliderHandle = image;
                    timelineSliderHandle.color = GetEditorProperty("Timeline Cursor Color").GetConfigEntry<Color>().Value;
                }
                else
                {
                    Debug.LogError($"{EditorPlugin.className}Whoooops you gotta put this CD up your-");
                }

                if (RTExtensions.TryFind("Editor Systems/Editor GUI/sizer/main/whole-timeline/Slider_Parent/Slider/Handle Slide Area/Image", out GameObject gm1) && gm1.TryGetComponent(out Image image1))
                {
                    timelineSliderRuler = image1;
                    timelineSliderRuler.color = GetEditorProperty("Timeline Cursor Color").GetConfigEntry<Color>().Value;
                }
                else
                {
                    Debug.LogError($"{EditorPlugin.className}Whoooops you gotta put this CD up your-");
                }

                if (RTExtensions.TryFind("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/timeline/Scroll View/Viewport/Content/time_slider/Handle Slide Area/Handle/Image", out GameObject gm2) && gm2.TryGetComponent(out Image image2))
                {
                    keyframeTimelineSliderHandle = image2;
                    keyframeTimelineSliderHandle.color = GetEditorProperty("Keyframe Cursor Color").GetConfigEntry<Color>().Value;
                }
                else
                {
                    Debug.LogError($"{EditorPlugin.className}Whoooops you gotta put this CD up your-");
                }

                if (RTExtensions.TryFind("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/timeline/Scroll View/Viewport/Content/time_slider/Handle Slide Area/Handle", out GameObject gm3) && gm3.TryGetComponent(out Image image3))
                {
                    keyframeTimelineSliderRuler = image3;
                    keyframeTimelineSliderRuler.color = GetEditorProperty("Keyframe Cursor Color").GetConfigEntry<Color>().Value;
                }
                else
                {
                    Debug.LogError($"{EditorPlugin.className}Whoooops you gotta put this CD up your-");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"{EditorPlugin.className}Error {ex}");
            }
        }

        public GridRenderer timelineGridRenderer;
        public void SetupTimelineGrid()
        {
            var grid = new GameObject("grid");
            grid.transform.SetParent(EditorManager.inst.timeline.transform);
            grid.transform.localScale = Vector3.one;
            grid.transform.SetSiblingIndex(0);

            var gridRT = grid.AddComponent<RectTransform>();

            grid.AddComponent<CanvasRenderer>();

            var gridLayout = grid.AddComponent<LayoutElement>();
            gridLayout.ignoreLayout = true;

            gridRT.anchoredPosition = Vector3.zero;
            gridRT.anchorMax = Vector3.one;
            gridRT.anchorMin = Vector3.zero;
            gridRT.pivot = Vector3.zero;
            gridRT.sizeDelta = Vector3.zero;

            timelineGridRenderer = grid.AddComponent<GridRenderer>();

            timelineGridRenderer.color = GetEditorProperty("Timeline Grid Color").GetConfigEntry<Color>().Value;
            timelineGridRenderer.thickness = GetEditorProperty("Timeline Grid Thickness").GetConfigEntry<float>().Value;

            timelineGridRenderer.enabled = GetEditorProperty("Timeline Grid Enabled").GetConfigEntry<bool>().Value;

            var gridCanvasGroup = grid.AddComponent<CanvasGroup>();
            gridCanvasGroup.blocksRaycasts = false;
            gridCanvasGroup.interactable = false;
        }

        public Image timelinePreviewPlayer;
        public Image timelinePreviewLine;
        public Image timelinePreviewLeftCap;
        public Image timelinePreviewRightCap;
        public List<Image> checkpointImages = new List<Image>();
        public void UpdateTimeline()
        {
            if (timelinePreview && AudioManager.inst.CurrentAudioSource.clip != null && DataManager.inst.gameData.beatmapData != null)
            {
                checkpointImages.Clear();
                LSHelpers.DeleteChildren(timelinePreview.Find("elements"), true);
                foreach (var checkpoint in DataManager.inst.gameData.beatmapData.checkpoints)
                {
                    if (checkpoint.time > 0.5f)
                    {
                        var gameObject = GameManager.inst.checkpointPrefab.Duplicate(timelinePreview.Find("elements"), $"Checkpoint [{checkpoint.name}] - [{checkpoint.time}]");
                        float num = checkpoint.time * 400f / AudioManager.inst.CurrentAudioSource.clip.length;
                        gameObject.transform.AsRT().anchoredPosition = new Vector2(num, 0f);
                        checkpointImages.Add(gameObject.GetComponent<Image>());
                    }
                }
            }
        }

        public void CreateObjectSearch()
        {
            var objectSearch = EditorManager.inst.GetDialog("Parent Selector").Dialog.gameObject
                .Duplicate(EditorManager.inst.GetDialog("Parent Selector").Dialog.GetParent(), "Object Search");
            objectSearch.transform.localPosition = Vector3.zero;

            var objectSearchRT = (RectTransform)objectSearch.transform;
            objectSearchRT.sizeDelta = new Vector2(600f, 450f);
            var objectSearchPanel = (RectTransform)objectSearch.transform.Find("Panel");
            objectSearchPanel.sizeDelta = new Vector2(632f, 32f);
            objectSearchPanel.transform.Find("Text").GetComponent<Text>().text = "Object Search";
            ((RectTransform)objectSearch.transform.Find("search-box")).sizeDelta = new Vector2(600f, 32f);
            objectSearch.transform.Find("mask/content").GetComponent<GridLayoutGroup>().cellSize = new Vector2(600f, 32f);

            var x = objectSearchPanel.transform.Find("x").GetComponent<Button>();
            x.onClick.RemoveAllListeners();
            x.onClick.AddListener(delegate ()
            {
                EditorManager.inst.HideDialog("Object Search Popup");
            });

            var searchBar = objectSearch.transform.Find("search-box/search").GetComponent<InputField>();
            searchBar.onValueChanged.ClearAll();
            searchBar.onValueChanged.AddListener(delegate (string _value)
            {
                objectSearchTerm = _value;
                RefreshObjectSearch(x => ObjectEditor.inst.SetCurrentObject(ObjectEditor.inst.GetTimelineObject(x), Input.GetKey(KeyCode.LeftControl)));
            });
            searchBar.transform.Find("Placeholder").GetComponent<Text>().text = "Search for object...";

            EditorHelper.AddEditorDropdown("Search Objects", "", "Edit", SearchSprite, delegate ()
            {
                EditorManager.inst.ShowDialog("Object Search Popup");
                RefreshObjectSearch(x => ObjectEditor.inst.SetCurrentObject(ObjectEditor.inst.GetTimelineObject(x), Input.GetKey(KeyCode.LeftControl)));
            });

            EditorHelper.AddEditorPopup("Object Search Popup", objectSearch);
        }

        public void CreateWarningPopup()
        {
            var warningPopup = EditorManager.inst.GetDialog("Save As Popup").Dialog.gameObject
                .Duplicate(EditorManager.inst.GetDialog("Save As Popup").Dialog.GetParent(), "Warning Popup");
            warningPopup.transform.localPosition = Vector3.zero;

            var main = warningPopup.transform.GetChild(0);

            var spacer1 = new GameObject
            {
                name = "spacerL",
                transform =
                {
                    parent = main,
                    localScale = Vector3.one
                }
            };
            var spacer1RT = spacer1.AddComponent<RectTransform>();
            spacer1.AddComponent<LayoutElement>();
            var horiz = spacer1.AddComponent<HorizontalLayoutGroup>();
            horiz.spacing = 22f;

            spacer1RT.sizeDelta = new Vector2(292f, 40f);

            var submit1 = main.Find("submit");
            submit1.SetParent(spacer1.transform);

            var submit2 = Instantiate(submit1);
            var submit2TF = submit2.transform;

            submit2TF.SetParent(spacer1.transform);
            submit2TF.localScale = Vector3.one;

            submit1.name = "submit1";
            submit2.name = "submit2";

            submit1.GetComponent<Image>().color = new Color(1f, 0.2137f, 0.2745f, 1f);
            submit2.GetComponent<Image>().color = new Color(0.302f, 0.7137f, 0.6745f, 1f);

            var submit1Button = submit1.GetComponent<Button>();
            var submit2Button = submit2.GetComponent<Button>();

            submit1Button.onClick.ClearAll();
            submit1Button.onClick.RemoveAllListeners();

            submit2Button.onClick.ClearAll();
            submit2Button.onClick.RemoveAllListeners();

            Destroy(main.Find("level-name").gameObject);

            var sizeFitter = main.GetComponent<ContentSizeFitter>();
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            var mainRT = main.GetComponent<RectTransform>();
            mainRT.sizeDelta = new Vector2(400f, 160f);

            main.Find("Level Name").GetComponent<RectTransform>().sizeDelta = new Vector2(292f, 64f);

            var close = main.Find("Panel/x").GetComponent<Button>();
            close.onClick.ClearAll();
            close.onClick.RemoveAllListeners();
            close.onClick.AddListener(delegate ()
            {
                EditorManager.inst.HideDialog("Warning Popup");
            });

            main.Find("Panel/Text").GetComponent<Text>().text = "Warning!";

            EditorHelper.AddEditorPopup("Warning Popup", warningPopup);
        }

        public void CreateREPLEditor()
        {
            var font = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("Panel/Text").GetComponent<Text>().font;

            replBase = new GameObject("REPL Editor");
            replBase.transform.SetParent(EditorManager.inst.GetDialog("Quick Actions Popup").Dialog.parent);
            replBase.transform.localScale = Vector3.one;
            var replRT = replBase.AddComponent<RectTransform>();

            replRT.anchoredPosition = Vector2.zero;

            var uiField = UIManager.GenerateUIInputField("REPL Editor", replBase.transform);

            replEditor = (InputField)uiField["InputField"];

            ((Image)uiField["Image"]).color = new Color(0.1132075f, 0.1132075f, 0.1132075f);

            replEditor.lineType = InputField.LineType.MultiLineNewline;
            replEditor.textComponent.color = new Color(0.9788679f, 0.9788679f, 0.9788679f, 1f);
            replEditor.textComponent.font = font;

            ((RectTransform)uiField["RectTransform"]).anchoredPosition = Vector2.zero;
            ((RectTransform)uiField["RectTransform"]).sizeDelta = new Vector2(1000f, 550f);

            var uiTop = UIManager.GenerateUIImage("Panel", replBase.transform);

            UIManager.SetRectTransform((RectTransform)uiTop["RectTransform"], new Vector2(0f, 291f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1000f, 32f));

            ((Image)uiTop["Image"]).color = new Color(0.1973585f, 0.1973585f, 0.1973585f);

            var close = Instantiate(EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("Panel/x").gameObject);
            close.transform.SetParent(((GameObject)uiTop["GameObject"]).transform);
            close.transform.localScale = Vector3.one;

            close.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            var closeButton = close.GetComponent<Button>();
            closeButton.onClick.ClearAll();
            closeButton.onClick.AddListener(delegate ()
            {
                EditorManager.inst.HideDialog("REPL Editor Popup");
                //StartCoroutine(ObjectEditor.RefreshObjectGUI(ObjectEditor.inst.CurrentSelection));
            });

            var uiTitle = UIManager.GenerateUIText("Title", ((GameObject)uiTop["GameObject"]).transform);
            UIManager.SetRectTransform(((RectTransform)uiTitle["RectTransform"]), new Vector2(-350f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(300f, 32f));
            ((Text)uiTitle["Text"]).text = "REPL Editor";
            ((Text)uiTitle["Text"]).alignment = TextAnchor.MiddleLeft;
            ((Text)uiTitle["Text"]).font = font;

            //var rtext = Instantiate(replEditor.textComponent.gameObject);
            //rtext.transform.SetParent(replEditor.transform);
            //rtext.transform.localScale = Vector3.one;

            //var rttext = rtext.GetComponent<RectTransform>();
            //rttext.anchoredPosition = new Vector2(2f, 0f);
            //rttext.sizeDelta = new Vector2(-12f, -8f);

            var selectUI = ((GameObject)uiTop["GameObject"]).AddComponent<SelectGUI>();
            selectUI.target = replBase.transform;

            //((RectTransform)replEditor.textComponent.transform).anchoredPosition = new Vector2(9999f, 9999f);
            replEditor.textComponent.color = new Color(0.9788679f, 0.9788679f, 0.9788679f, 1f);
            //replEditor.textComponent.GetComponent<CanvasRenderer>().cull = true;

            replEditor.customCaretColor = true;
            replEditor.caretColor = new Color(0.9788679f, 0.9788679f, 0.9788679f, 1f);

            //replText = rtext.GetComponent<Text>();

            var uiBottom = UIManager.GenerateUIImage("Panel", replBase.transform);

            UIManager.SetRectTransform((RectTransform)uiBottom["RectTransform"], new Vector2(0f, -291f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1000f, 32f));

            ((Image)uiBottom["Image"]).color = new Color(0.1973585f, 0.1973585f, 0.1973585f);

            var evaluator = UIManager.GenerateUIButton("Evaluate", ((GameObject)uiBottom["GameObject"]).transform);

            UIManager.SetRectTransform((RectTransform)evaluator["RectTransform"], new Vector2(400f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(200f, 32f));

            var button = (Button)evaluator["Button"];
            button.onClick.AddListener(delegate ()
            {
                RTCode.Evaluate(replEditor.text);
            });

            try
            {
                var text = AssetManager.inst.TextObject.Duplicate((RectTransform)evaluator["RectTransform"], "Text");
                ((RectTransform)text.transform).anchoredPosition = Vector2.zero;
                ((RectTransform)text.transform).sizeDelta = new Vector2(200f, 32f);
                var t = text.GetComponent<Text>();
                t.alignment = TextAnchor.MiddleCenter;
                t.text = "Evaluate";
            }
            catch
            {

            }

            replBase.SetActive(false);

            EditorHelper.AddEditorPopup("REPL Editor Popup", replBase);
        }

        public void CreateMultiObjectEditor()
        {
            var barButton = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/time").transform.GetChild(4).gameObject;

            var eventButton = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject/event");

            var bcol = new Color(0.3922f, 0.7098f, 0.9647f, 1f);

            var dataLeft = EditorManager.inst.GetDialog("Multi Object Editor").Dialog.Find("data/left");

            dataLeft.gameObject.SetActive(true);

            var scrollView = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View").Duplicate(dataLeft);

            var parent = scrollView.transform.Find("Viewport/Content");

            LSHelpers.DeleteChildren(parent);

            scrollView.GetComponent<RectTransform>().sizeDelta = new Vector2(383f, 690f);

            dataLeft.GetChild(1).gameObject.SetActive(true);

            dataLeft.GetChild(1).gameObject.name = "label layer";

            dataLeft.GetChild(3).gameObject.SetActive(true);

            dataLeft.GetChild(3).gameObject.name = "label depth";

            dataLeft.GetChild(1).SetParent(parent);

            dataLeft.GetChild(2).SetParent(parent);

            var textHolder = EditorManager.inst.GetDialog("Multi Object Editor").Dialog.Find("data/right/text holder/Text");
            var textHolderText = textHolder.GetComponent<Text>();
            textHolderText.text = textHolderText.text.Replace(
                "The current version of the editor doesn't support any editing functionality.",
                "On the left you'll see all the Multi Object Editor tools you'll need.");

            textHolderText.fontSize = 22;

            textHolder.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -125f);

            textHolder.GetComponent<RectTransform>().sizeDelta = new Vector2(-68f, 0f);

            var zoom = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/zoom/zoom");

            var labelL = parent.Find("label layer");
            labelL.SetParent(null);
            Destroy(parent.Find("label depth").gameObject);

            Action<string, string, bool, UnityAction, UnityAction, UnityAction> action
                = delegate (string name, string placeHolder, bool doMiddle, UnityAction leftButton, UnityAction middleButton, UnityAction rightButton)
            {
                var gameObject = zoom.Duplicate(parent, name);
                gameObject.transform.localScale = Vector3.one;
                gameObject.transform.GetChild(0).Find("input/Placeholder").GetComponent<Text>().text = placeHolder;

                ((RectTransform)gameObject.transform).sizeDelta = new Vector2(428f, 32f);

                if (gameObject.transform.GetChild(0).gameObject.TryGetComponent(out InputField inputField))
                {
                    inputField.text = "1";
                    //TriggerHelper.AddEventTrigger(gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDeltaInt(inputField, min: 0) });
                }

                var layerIF = gameObject.transform.GetChild(0).gameObject.GetComponent<InputField>();

                if (doMiddle)
                {
                    var multiLB = gameObject.transform.GetChild(0).Find("<").gameObject
                    .Duplicate(gameObject.transform.GetChild(0), "|", 2);
                    multiLB.GetComponent<Image>().sprite = barButton.GetComponent<Image>().sprite;

                    var multiLBB = multiLB.GetComponent<Button>();

                    multiLBB.onClick.RemoveAllListeners();
                    multiLBB.onClick.AddListener(middleButton);
                }

                var mlsLeft = gameObject.transform.GetChild(0).Find("<").GetComponent<Button>();
                mlsLeft.onClick.RemoveAllListeners();
                mlsLeft.onClick.AddListener(leftButton);

                var mlsRight = gameObject.transform.GetChild(0).Find(">").GetComponent<Button>();
                mlsRight.onClick.RemoveAllListeners();
                mlsRight.onClick.AddListener(rightButton);
            };

            Action<string> labelGenerator = delegate (string name)
            {
                var label = labelL.gameObject.Duplicate(parent, "label");
                label.transform.localScale = Vector3.one;
                label.transform.GetChild(0).gameObject.GetComponent<Text>().text = name;
            };

            Action<string, string, Transform, UnityAction> buttonGenerator = delegate (string name, string text, Transform parent, UnityAction unityAction)
            {
                var gameObject = eventButton.Duplicate(parent, name);
                gameObject.transform.localScale = Vector3.one;

                ((RectTransform)gameObject.transform).sizeDelta = new Vector2(404f, 32f);

                gameObject.transform.GetChild(0).GetComponent<Text>().text = text;
                gameObject.GetComponent<Image>().color = bcol;

                var button = gameObject.GetComponent<Button>();
                button.onClick.ClearAll();
                button.onClick.AddListener(unityAction);
            };

            // Layers
            {
                labelGenerator("Set Group Layer");

                action("layer", "Enter layer...", true, delegate ()
                {
                    if (parent.Find("layer") && parent.Find("layer").GetChild(0).gameObject.TryGetComponent(out InputField inputField))
                    {
                        if (int.TryParse(inputField.text, out int num))
                        {
                            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                            {
                                if (timelineObject.IsBeatmapObject)
                                    timelineObject.GetData<BeatmapObject>().editorData.layer = Mathf.Clamp(timelineObject.GetData<BeatmapObject>().editorData.layer - 1, 0, int.MaxValue);
                                if (timelineObject.IsPrefabObject)
                                    timelineObject.GetData<PrefabObject>().editorData.layer = Mathf.Clamp(timelineObject.GetData<PrefabObject>().editorData.layer - 1, 0, int.MaxValue);
                            }
                        }
                    }

                }, delegate ()
                {
                    if (parent.Find("layer") && parent.Find("layer").GetChild(0).gameObject.TryGetComponent(out InputField inputField))
                    {
                        //inputField.text = "1";
                        TriggerHelper.AddEventTrigger(parent.Find("layer").gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDeltaInt(inputField, min: 0) });

                        if (int.TryParse(inputField.text, out int num))
                        {
                            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                            {
                                if (timelineObject.IsBeatmapObject)
                                    timelineObject.GetData<BeatmapObject>().editorData.layer = Mathf.Clamp(num - 1, 0, int.MaxValue);
                                if (timelineObject.IsPrefabObject)
                                    timelineObject.GetData<PrefabObject>().editorData.layer = Mathf.Clamp(num - 1, 0, int.MaxValue);
                            }
                        }
                    }
                }, delegate ()
                {
                    if (parent.Find("layer") && parent.Find("layer").GetChild(0).gameObject.TryGetComponent(out InputField inputField))
                    {
                        if (int.TryParse(inputField.text, out int num))
                        {
                            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                            {
                                if (timelineObject.IsBeatmapObject)
                                    timelineObject.GetData<BeatmapObject>().editorData.layer = Mathf.Clamp(timelineObject.GetData<BeatmapObject>().editorData.layer + 1, 0, int.MaxValue);
                                if (timelineObject.IsPrefabObject)
                                    timelineObject.GetData<PrefabObject>().editorData.layer = Mathf.Clamp(timelineObject.GetData<PrefabObject>().editorData.layer + 1, 0, int.MaxValue);
                            }
                        }
                    }
                });
            }

            // Depth
            {
                labelGenerator("Set Group Depth");

                action("depth", "Enter depth...", true, delegate ()
                {
                    if (parent.Find("depth") && parent.Find("depth").GetChild(0).gameObject.TryGetComponent(out InputField inputField))
                    {
                        if (int.TryParse(inputField.text, out int num))
                        {
                            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                bm.Depth -= num;
                                Updater.UpdateProcessor(bm, "Depth");
                            }
                        }
                    }
                }, delegate ()
                {
                    if (parent.Find("depth") && parent.Find("depth").GetChild(0).gameObject.TryGetComponent(out InputField inputField))
                    {
                        //inputField.text = "15";
                        TriggerHelper.AddEventTrigger(parent.Find("depth").gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDeltaInt(inputField, min: 0) });

                        if (int.TryParse(inputField.text, out int num))
                        {
                            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                bm.Depth = num;
                                Updater.UpdateProcessor(bm, "Depth");
                            }
                        }
                    }
                }, delegate ()
                {
                    if (parent.Find("depth") && parent.Find("depth").GetChild(0).gameObject.TryGetComponent(out InputField inputField))
                    {
                        if (int.TryParse(inputField.text, out int num))
                        {
                            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                bm.Depth += num;
                                Updater.UpdateProcessor(bm, "Depth");
                            }
                        }
                    }
                });
            }

            // Song Time
            {
                labelGenerator("Set Song Time");

                action("time", "Enter time...", true, delegate ()
                {
                    if (parent.Find("time") && parent.Find("time").GetChild(0).gameObject.TryGetComponent(out InputField inputField)
                        && float.TryParse(inputField.text, out float num))
                    {
                        float first = ObjectEditor.inst.SelectedObjects.Min(x => x.Time);

                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                        {
                            timelineObject.Time = AudioManager.inst.CurrentAudioSource.time - first + timelineObject.Time + num;
                            if (timelineObject.IsBeatmapObject)
                                Updater.UpdateProcessor(timelineObject.GetData<BeatmapObject>(), "StartTime");
                            if (timelineObject.IsPrefabObject)
                                Updater.UpdatePrefab(timelineObject.GetData<PrefabObject>());

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                        }
                    }
                }, delegate ()
                {
                    if (parent.Find("time") && parent.Find("time").GetChild(0).gameObject.TryGetComponent(out InputField inputField))
                    {
                        TriggerHelper.AddEventTrigger(parent.Find("time").gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(inputField) });

                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                        {
                            timelineObject.Time = AudioManager.inst.CurrentAudioSource.time;
                            if (timelineObject.IsBeatmapObject)
                                Updater.UpdateProcessor(timelineObject.GetData<BeatmapObject>(), "StartTime");
                            if (timelineObject.IsPrefabObject)
                                Updater.UpdatePrefab(timelineObject.GetData<PrefabObject>());

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                        }
                    }
                }, delegate ()
                {
                    if (parent.Find("time") && parent.Find("time").GetChild(0).gameObject.TryGetComponent(out InputField inputField)
                        && float.TryParse(inputField.text, out float num))
                    {
                        float first = ObjectEditor.inst.SelectedObjects.Min(x => x.Time);

                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                        {
                            timelineObject.Time = AudioManager.inst.CurrentAudioSource.time - first + timelineObject.Time - num;
                            if (timelineObject.IsBeatmapObject)
                                Updater.UpdateProcessor(timelineObject.GetData<BeatmapObject>(), "StartTime");
                            if (timelineObject.IsPrefabObject)
                                Updater.UpdatePrefab(timelineObject.GetData<PrefabObject>());

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                        }
                    }
                });
            }

            // Autokill Offset
            {
                labelGenerator("Set Autokill Offset");

                action("autokill offset", "Enter autokill...", true, delegate ()
                {
                    if (parent.Find("autokill offset") && parent.Find("autokill offset").GetChild(0).gameObject.TryGetComponent(out InputField inputField)
                    && float.TryParse(inputField.text, out float num))
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                        {
                            if (timelineObject.IsBeatmapObject)
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                bm.autoKillOffset -= num;
                                Updater.UpdateProcessor(bm, "Autokill");
                            }

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                        }
                    }
                }, delegate ()
                {
                    if (parent.Find("autokill offset") && parent.Find("autokill offset").GetChild(0).gameObject.TryGetComponent(out InputField inputField)
                        && float.TryParse(inputField.text, out float num))
                    {
                        TriggerHelper.AddEventTrigger(parent.Find("autokill offset").gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(inputField) });

                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                        {
                            if (timelineObject.IsBeatmapObject)
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                bm.autoKillOffset = num;
                                Updater.UpdateProcessor(bm, "Autokill");
                            }

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                        }
                    }
                }, delegate ()
                {
                    if (parent.Find("autokill offset") && parent.Find("autokill offset").GetChild(0).gameObject.TryGetComponent(out InputField inputField)
                        && float.TryParse(inputField.text, out float num))
                    {
                        float first = ObjectEditor.inst.SelectedObjects.Min(x => x.Time);

                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                        {
                            if (timelineObject.IsBeatmapObject)
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                bm.autoKillOffset += num;
                                Updater.UpdateProcessor(bm, "Autokill");
                            }

                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                        }
                    }
                });
            }

            // Name
            {
                labelGenerator("Set Name");

                var multiNameSet = zoom.Duplicate(parent, "name");
                multiNameSet.transform.localScale = Vector3.one;

                multiNameSet.GetComponent<RectTransform>().sizeDelta = new Vector2(428f, 32f);

                var inputField = multiNameSet.transform.GetChild(0).GetComponent<InputField>();
                inputField.characterValidation = InputField.CharacterValidation.None;
                inputField.characterLimit = 0;
                inputField.text = "name";
                ((Text)inputField.placeholder).text = "Enter name...";

                var multiNB = multiNameSet.transform.GetChild(0).Find("<").gameObject;
                //multiNB.transform.SetParent(multiNameSet.transform.GetChild(0));
                //multiNB.transform.SetSiblingIndex(2);
                multiNB.name = "|";
                //multiNB.transform.localScale = Vector3.one;
                multiNB.GetComponent<Image>().sprite = barButton.GetComponent<Image>().sprite;

                var multiNBB = multiNB.GetComponent<Button>();
                multiNBB.onClick.RemoveAllListeners();
                multiNBB.onClick.AddListener(delegate ()
                {
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                    {
                        timelineObject.GetData<BeatmapObject>().name = inputField.text;
                        ObjectEditor.inst.RenderTimelineObject(timelineObject);
                    }
                });

                var mtnRight = multiNameSet.transform.GetChild(0).Find(">").GetComponent<Button>();

                string jpgFileLocation = RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/add.png";

                if (RTFile.FileExists(jpgFileLocation))
                {
                    Image spriteReloader = multiNameSet.transform.GetChild(0).Find(">").GetComponent<Image>();

                    EditorManager.inst.StartCoroutine(EditorManager.inst.GetSprite(jpgFileLocation, new EditorManager.SpriteLimits(), delegate (Sprite cover)
                    {
                        spriteReloader.sprite = cover;
                    }, delegate (string errorFile)
                    {
                        spriteReloader.sprite = ArcadeManager.inst.defaultImage;
                    }));
                }

                var mtnLeftLE = multiNameSet.transform.GetChild(0).Find(">").gameObject.AddComponent<LayoutElement>();
                mtnLeftLE.ignoreLayout = true;

                var mtnLeftRT = multiNameSet.transform.GetChild(0).Find(">").GetComponent<RectTransform>();
                mtnLeftRT.anchoredPosition = new Vector2(339f, 0f);
                mtnLeftRT.sizeDelta = new Vector2(32f, 32f);

                var mtnRightB = mtnRight.GetComponent<Button>();
                mtnRightB.onClick.RemoveAllListeners();
                mtnRightB.onClick.AddListener(delegate ()
                {
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                    {
                        timelineObject.GetData<BeatmapObject>().name += inputField.text;
                        ObjectEditor.inst.RenderTimelineObject(timelineObject);
                    }
                });
            }

            // Tags
            {
                labelGenerator("Add a Tag");

                var multiNameSet = zoom.Duplicate(parent, "name");
                multiNameSet.transform.localScale = Vector3.one;

                multiNameSet.GetComponent<RectTransform>().sizeDelta = new Vector2(428f, 32f);

                var inputField = multiNameSet.transform.GetChild(0).GetComponent<InputField>();
                inputField.characterValidation = InputField.CharacterValidation.None;
                inputField.characterLimit = 0;
                inputField.text = "name";
                ((Text)inputField.placeholder).text = "Enter tag...";

                Destroy(multiNameSet.transform.GetChild(0).Find("<").gameObject);
                //var multiNB = multiNameSet.transform.GetChild(0).Find("<").gameObject;
                //multiNB.name = "|";
                //multiNB.GetComponent<Image>().sprite = barButton.GetComponent<Image>().sprite;

                //var multiNBB = multiNB.GetComponent<Button>();
                //multiNBB.onClick.RemoveAllListeners();
                //multiNBB.onClick.AddListener(delegate ()
                //{
                //    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                //    {
                //        timelineObject.GetData<BeatmapObject>().name = inputField.text;
                //        ObjectEditor.inst.RenderTimelineObject(timelineObject);
                //    }
                //});

                var mtnRight = multiNameSet.transform.GetChild(0).Find(">").GetComponent<Button>();

                string jpgFileLocation = RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/add.png";

                if (RTFile.FileExists(jpgFileLocation))
                {
                    Image spriteReloader = multiNameSet.transform.GetChild(0).Find(">").GetComponent<Image>();

                    EditorManager.inst.StartCoroutine(EditorManager.inst.GetSprite(jpgFileLocation, new EditorManager.SpriteLimits(), delegate (Sprite cover)
                    {
                        spriteReloader.sprite = cover;
                    }, delegate (string errorFile)
                    {
                        spriteReloader.sprite = ArcadeManager.inst.defaultImage;
                    }));
                }

                var mtnLeftLE = multiNameSet.transform.GetChild(0).Find(">").gameObject.AddComponent<LayoutElement>();
                mtnLeftLE.ignoreLayout = true;

                var mtnLeftRT = multiNameSet.transform.GetChild(0).Find(">").GetComponent<RectTransform>();
                mtnLeftRT.anchoredPosition = new Vector2(339f, 0f);
                mtnLeftRT.sizeDelta = new Vector2(32f, 32f);

                var mtnRightB = mtnRight.GetComponent<Button>();
                mtnRightB.onClick.RemoveAllListeners();
                mtnRightB.onClick.AddListener(delegate ()
                {
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                    {
                        timelineObject.GetData<BeatmapObject>().tags.Add(inputField.text);
                    }
                });
            }

            // Clear Tags
            {
                labelGenerator("Clear all objects' tags");

                buttonGenerator("clear tags", "Clear Tags", parent, delegate ()
                {
                    foreach (var beatmapObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
                    {
                        beatmapObject.tags.Clear();
                    }
                });
            }

            // Song Time Autokill
            {
                labelGenerator("Set Song Time Autokill to Current");

                buttonGenerator("set autokill", "Set", parent, delegate ()
                {
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                    {
                        var bm = timelineObject.GetData<BeatmapObject>();
                        bm.autoKillType = AutoKillType.SongTime;
                        bm.autoKillOffset = AudioManager.inst.CurrentAudioSource.time;

                        ObjectEditor.inst.RenderTimelineObject(timelineObject);
                        Updater.UpdateProcessor(bm, "Autokill");
                    }
                });
            }
            
            // No Autokill
            {
                labelGenerator("Set to No Autokill");

                buttonGenerator("set no autokill", "Set", parent, delegate ()
                {
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                    {
                        var bm = timelineObject.GetData<BeatmapObject>();
                        bm.autoKillType = AutoKillType.OldStyleNoAutokill;

                        ObjectEditor.inst.RenderTimelineObject(timelineObject);
                        Updater.UpdateProcessor(bm, "Autokill");
                    }
                });
            }
            
            // Force Snap BPM
            {
                labelGenerator("Force Snap Start Time to BPM");

                buttonGenerator("set autokill", "Snap", parent, delegate ()
                {
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                    {
                        timelineObject.Time = SnapToBPM(timelineObject.Time);
                        if (timelineObject.IsBeatmapObject)
                            Updater.UpdateProcessor(timelineObject.GetData<BeatmapObject>(), "Start Time");
                        if (timelineObject.IsPrefabObject)
                            Updater.UpdatePrefab(timelineObject.GetData<PrefabObject>(), "Start Time");

                        ObjectEditor.inst.RenderTimelineObject(timelineObject);
                    }
                });
            }

            // Cycle Object Type
            {
                labelGenerator("Cycle Object Type");

                buttonGenerator("cycle obj type", "Cycle", parent, delegate ()
                {
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                    {
                        var bm = timelineObject.GetData<BeatmapObject>();
                        bm.objectType += 1;
                        if ((int)bm.objectType > 4)
                            bm.objectType = 0;

                        ObjectEditor.inst.RenderTimelineObject(timelineObject);
                        Updater.UpdateProcessor(bm, "ObjectType");
                    }
                });
            }

            // Lock Swap
            {
                labelGenerator("Swap each object's lock state");

                buttonGenerator("lock swap", "Swap Lock", parent, delegate ()
                {
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                    {
                        timelineObject.Locked = !timelineObject.Locked;

                        ObjectEditor.inst.RenderTimelineObject(timelineObject);
                    }
                });
            }

            // Lock Toggle
            {
                labelGenerator("Toggle all object's lock state");

                bool loggle = false;

                buttonGenerator("lock toggle", "Toggle Lock", parent, delegate ()
                {
                    loggle = !loggle;
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                    {
                        timelineObject.Locked = loggle;

                        ObjectEditor.inst.RenderTimelineObject(timelineObject);
                    }
                });
            }

            // Collapse Swap
            {
                labelGenerator("Swap each object's collapse state");

                buttonGenerator("collapse swap", "Swap Collapse", parent, delegate ()
                {
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                    {
                        timelineObject.Collapse = !timelineObject.Collapse;

                        ObjectEditor.inst.RenderTimelineObject(timelineObject);
                    }
                });
            }

            // Collapse Toggle
            {
                labelGenerator("Toggle all object's collapse state");

                bool coggle = false;

                buttonGenerator("collapse toggle", "Toggle Collapse", parent, delegate ()
                {
                    coggle = !coggle;
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                    {
                        timelineObject.Collapse = coggle;

                        ObjectEditor.inst.RenderTimelineObject(timelineObject);
                    }
                });
            }
            
            // Background Swap
            {
                labelGenerator("Swap each object's render type");

                buttonGenerator("render type swap", "Swap Render Type", parent, delegate ()
                {
                    foreach (var beatmapObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
                    {
                        beatmapObject.background = !beatmapObject.background;
                        Updater.UpdateProcessor(beatmapObject);
                    }
                });
            }

            // Background Toggle
            {
                labelGenerator("Toggle all object's render type");

                bool boggle = false;

                buttonGenerator("render type toggle", "Toggle Render Type", parent, delegate ()
                {
                    boggle = !boggle;
                    foreach (var beatmapObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
                    {
                        beatmapObject.background = boggle;
                        Updater.UpdateProcessor(beatmapObject);
                    }
                });
            }

            // Clear Animations
            {
                labelGenerator("Clear Animations");

                buttonGenerator("clear animations", "Clear", parent, delegate ()
                {
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                    {
                        if (timelineObject.IsBeatmapObject)
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();
                            foreach (var tkf in timelineObject.InternalSelections)
                            {
                                Destroy(tkf.GameObject);
                            }
                            timelineObject.InternalSelections.Clear();
                            for (int i = 0; i < bm.events.Count; i++)
                            {
                                bm.events[i] = bm.events[i].OrderBy(x => x.eventTime).ToList();
                                var firstKF = EventKeyframe.DeepCopy((EventKeyframe)bm.events[i][0], false);
                                bm.events[i].Clear();
                                bm.events[i].Add(firstKF);
                            }
                            if (ObjectEditor.inst.SelectedObjects.Count == 1)
                            {
                                ObjectEditor.inst.ResizeKeyframeTimeline(bm);
                                ObjectEditor.inst.RenderKeyframes(bm);
                            }

                            Updater.UpdateProcessor(bm, "Keyframes");
                            ObjectEditor.inst.RenderTimelineObject(timelineObject);
                        }
                    }
                });
            }

            // Clear Modifiers
            if (ModCompatibility.ObjectModifiersInstalled)
            {
                labelGenerator("Clear Modifiers");

                buttonGenerator("clear modifiers", "Clear", parent, delegate ()
                {
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                    {
                        if (timelineObject.IsBeatmapObject)
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();

                            bm.modifiers.Clear();

                            Updater.UpdateProcessor(bm);
                        }
                    }
                });
            }

            // Sync object selection
            {
                labelGenerator("Sync to specific object");

                var syncLayout = new GameObject("sync layout");
                syncLayout.transform.SetParent(parent);
                syncLayout.transform.localScale = Vector3.one;

                var multiSyncRT = syncLayout.AddComponent<RectTransform>();
                multiSyncRT.sizeDelta = new Vector2(390f, 160f);
                var multiSyncGLG = syncLayout.AddComponent<GridLayoutGroup>();
                multiSyncGLG.spacing = new Vector2(4f, 4f);
                multiSyncGLG.cellSize = new Vector2(61.6f, 49f);

                // Start Time
                {
                    buttonGenerator("start time", "ST", syncLayout.transform, delegate ()
                    {
                        EditorManager.inst.ShowDialog("Object Search Popup");
                        RefreshObjectSearch(delegate (BeatmapObject beatmapObject)
                        {
                            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                            {
                                timelineObject.Time = beatmapObject.StartTime;
                                ObjectEditor.inst.RenderTimelineObject(timelineObject);
                                Updater.UpdateProcessor(timelineObject.GetData<BeatmapObject>(), "StartTime");
                            }
                            EditorManager.inst.HideDialog("Object Search Popup");
                        });
                    });
                }

                // Name
                {
                    buttonGenerator("name", "N", syncLayout.transform, delegate ()
                    {
                        EditorManager.inst.ShowDialog("Object Search Popup");
                        RefreshObjectSearch(delegate (BeatmapObject beatmapObject)
                        {
                            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                            {
                                timelineObject.GetData<BeatmapObject>().name = beatmapObject.name;
                                ObjectEditor.inst.RenderTimelineObject(timelineObject);
                            }
                            EditorManager.inst.HideDialog("Object Search Popup");
                        });
                    });
                }

                // Object Type
                {
                    buttonGenerator("object type", "OT", syncLayout.transform, delegate ()
                    {
                        EditorManager.inst.ShowDialog("Object Search Popup");
                        RefreshObjectSearch(delegate (BeatmapObject beatmapObject)
                        {
                            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                            {
                                timelineObject.GetData<BeatmapObject>().objectType = beatmapObject.objectType;
                                ObjectEditor.inst.RenderTimelineObject(timelineObject);
                                Updater.UpdateProcessor(timelineObject.GetData<BeatmapObject>(), "ObjectType");
                            }
                            EditorManager.inst.HideDialog("Object Search Popup");
                        });
                    });
                }

                // Autokill Type
                {
                    buttonGenerator("autokill type", "AKT", syncLayout.transform, delegate ()
                    {
                        EditorManager.inst.ShowDialog("Object Search Popup");
                        RefreshObjectSearch(delegate (BeatmapObject beatmapObject)
                        {
                            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                            {
                                timelineObject.GetData<BeatmapObject>().autoKillType = beatmapObject.autoKillType;
                                ObjectEditor.inst.RenderTimelineObject(timelineObject);
                                Updater.UpdateProcessor(timelineObject.GetData<BeatmapObject>(), "AutoKill");
                            }
                            EditorManager.inst.HideDialog("Object Search Popup");
                        });
                    });
                }

                // Autokill Offset
                {
                    buttonGenerator("autokill offset", "AKO", syncLayout.transform, delegate ()
                    {
                        EditorManager.inst.ShowDialog("Object Search Popup");
                        RefreshObjectSearch(delegate (BeatmapObject beatmapObject)
                        {
                            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                            {
                                timelineObject.GetData<BeatmapObject>().autoKillOffset = beatmapObject.autoKillOffset;
                                ObjectEditor.inst.RenderTimelineObject(timelineObject);
                                Updater.UpdateProcessor(timelineObject.GetData<BeatmapObject>(), "AutoKill");
                            }
                            EditorManager.inst.HideDialog("Object Search Popup");
                        });
                    });
                }

                // Parent
                {
                    buttonGenerator("parent", "P", syncLayout.transform, delegate ()
                    {
                        EditorManager.inst.ShowDialog("Object Search Popup");
                        RefreshObjectSearch(delegate (BeatmapObject beatmapObject)
                        {
                            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                            {
                                timelineObject.GetData<BeatmapObject>().parent = beatmapObject.parent;
                                Updater.UpdateProcessor(timelineObject.GetData<BeatmapObject>(), "Parent");
                            }
                            EditorManager.inst.HideDialog("Object Search Popup");
                        });
                    });
                }

                // Parent Type
                {
                    buttonGenerator("parent type", "PT", syncLayout.transform, delegate ()
                    {
                        EditorManager.inst.ShowDialog("Object Search Popup");
                        RefreshObjectSearch(delegate (BeatmapObject beatmapObject)
                        {
                            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                            {
                                timelineObject.GetData<BeatmapObject>().parentType = beatmapObject.parentType;
                                Updater.UpdateProcessor(timelineObject.GetData<BeatmapObject>(), "ParentType");
                            }
                            EditorManager.inst.HideDialog("Object Search Popup");
                        });
                    });
                }

                // Parent Offset
                {
                    buttonGenerator("parent offset", "PO", syncLayout.transform, delegate ()
                    {
                        EditorManager.inst.ShowDialog("Object Search Popup");
                        RefreshObjectSearch(delegate (BeatmapObject beatmapObject)
                        {
                            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                            {
                                timelineObject.GetData<BeatmapObject>().parentOffsets = beatmapObject.parentOffsets.Clone();
                                Updater.UpdateProcessor(timelineObject.GetData<BeatmapObject>(), "ParentOffset");
                            }
                            EditorManager.inst.HideDialog("Object Search Popup");
                        });
                    });
                }

                // Origin
                {
                    buttonGenerator("origin", "O", syncLayout.transform, delegate ()
                    {
                        EditorManager.inst.ShowDialog("Object Search Popup");
                        RefreshObjectSearch(delegate (BeatmapObject beatmapObject)
                        {
                            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                            {
                                timelineObject.GetData<BeatmapObject>().origin = beatmapObject.origin;
                                Updater.UpdateProcessor(timelineObject.GetData<BeatmapObject>(), "Origin");
                            }
                            EditorManager.inst.HideDialog("Object Search Popup");
                        });
                    });
                }

                // Shape
                {
                    buttonGenerator("shape", "S", syncLayout.transform, delegate ()
                    {
                        EditorManager.inst.ShowDialog("Object Search Popup");
                        RefreshObjectSearch(delegate (BeatmapObject beatmapObject)
                        {
                            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                            {
                                timelineObject.GetData<BeatmapObject>().shape = beatmapObject.shape;
                                timelineObject.GetData<BeatmapObject>().shapeOption = beatmapObject.shapeOption;
                                Updater.UpdateProcessor(timelineObject.GetData<BeatmapObject>(), "Shape");
                            }
                            EditorManager.inst.HideDialog("Object Search Popup");
                        });
                    });
                }

                // Text
                {
                    buttonGenerator("text", "T", syncLayout.transform, delegate ()
                    {
                        EditorManager.inst.ShowDialog("Object Search Popup");
                        RefreshObjectSearch(delegate (BeatmapObject beatmapObject)
                        {
                            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                            {
                                timelineObject.GetData<BeatmapObject>().text = beatmapObject.text;
                                Updater.UpdateProcessor(timelineObject.GetData<BeatmapObject>(), "Text");
                            }
                            EditorManager.inst.HideDialog("Object Search Popup");
                        });
                    });
                }

                // Depth
                {
                    buttonGenerator("depth", "D", syncLayout.transform, delegate ()
                    {
                        EditorManager.inst.ShowDialog("Object Search Popup");
                        RefreshObjectSearch(delegate (BeatmapObject beatmapObject)
                        {
                            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                            {
                                timelineObject.GetData<BeatmapObject>().Depth = beatmapObject.Depth;
                                Updater.UpdateProcessor(timelineObject.GetData<BeatmapObject>(), "Depth");
                            }
                            EditorManager.inst.HideDialog("Object Search Popup");
                        });
                    });
                }

                // Keyframes
                {
                    buttonGenerator("keyframes", "KF", syncLayout.transform, delegate ()
                    {
                        EditorManager.inst.ShowDialog("Object Search Popup");
                        RefreshObjectSearch(delegate (BeatmapObject beatmapObject)
                        {
                            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();

                                for (int i = 0; i < bm.events.Count; i++)
                                    bm.events[i] = beatmapObject.events[i].Clone();

                                Updater.UpdateProcessor(bm, "Keyframes");
                            }
                            EditorManager.inst.HideDialog("Object Search Popup");
                        });
                    });
                }

                // Modifiers
                if (ModCompatibility.ObjectModifiersInstalled)
                {
                    buttonGenerator("keyframes", "MOD", syncLayout.transform, delegate ()
                    {
                        EditorManager.inst.ShowDialog("Object Search Popup");
                        RefreshObjectSearch(delegate (BeatmapObject beatmapObject)
                        {
                            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();

                                bm.modifiers.AddRange(beatmapObject.modifiers.Select(x => BeatmapObject.Modifier.DeepCopy(x, bm)));

                                Updater.UpdateProcessor(bm);
                            }
                            EditorManager.inst.HideDialog("Object Search Popup");
                        });
                    });
                }
            }

            // Replace Name
            {
                labelGenerator("Replace Name");

                var replaceName = new GameObject("replace name");
                replaceName.transform.SetParent(parent);
                replaceName.transform.localScale = Vector3.one;

                var multiSyncRT = replaceName.AddComponent<RectTransform>();
                multiSyncRT.sizeDelta = new Vector2(390f, 32f);
                var multiSyncGLG = replaceName.AddComponent<GridLayoutGroup>();
                multiSyncGLG.spacing = new Vector2(8f, 8f);
                multiSyncGLG.cellSize = new Vector2(124f, 32f);

                var oldName = GameObject.Find("TimelineBar/GameObject/Time Input").Duplicate(multiSyncRT, "old name");

                Destroy(oldName.GetComponent<EventTrigger>());
                var oldNameIF = oldName.GetComponent<InputField>();
                oldNameIF.characterValidation = InputField.CharacterValidation.None;
                oldNameIF.textComponent.alignment = TextAnchor.MiddleLeft;
                oldNameIF.textComponent.fontSize = 16;
                oldNameIF.text = "Old Name";
                ((Text)oldNameIF.placeholder).text = "Enter old name...";
                ((Text)oldNameIF.placeholder).alignment = TextAnchor.MiddleLeft;
                ((Text)oldNameIF.placeholder).fontSize = 16;
                ((Text)oldNameIF.placeholder).color = new Color(0f, 0f, 0f, 0.3f);

                oldNameIF.onValueChanged.RemoveAllListeners();

                var oldNameSwapper = oldName.AddComponent<InputFieldSwapper>();
                oldNameSwapper.Init(oldNameIF, InputFieldSwapper.Type.String);

                var newName = GameObject.Find("TimelineBar/GameObject/Time Input").Duplicate(multiSyncRT, "new name");

                Destroy(newName.GetComponent<EventTrigger>());
                var newNameIF = newName.GetComponent<InputField>();
                newNameIF.characterValidation = InputField.CharacterValidation.None;
                newNameIF.textComponent.alignment = TextAnchor.MiddleLeft;
                newNameIF.textComponent.fontSize = 16;
                newNameIF.text = "New Name";
                ((Text)newNameIF.placeholder).text = "Enter new name...";
                ((Text)newNameIF.placeholder).alignment = TextAnchor.MiddleLeft;
                ((Text)newNameIF.placeholder).fontSize = 16;
                ((Text)newNameIF.placeholder).color = new Color(0f, 0f, 0f, 0.3f);

                newNameIF.onValueChanged.RemoveAllListeners();

                var newNameSwapper = newName.AddComponent<InputFieldSwapper>();
                newNameSwapper.Init(newNameIF, InputFieldSwapper.Type.String);

                var replace = eventButton.Duplicate(replaceName.transform, "replace");
                replace.transform.localScale = Vector3.one;
                replace.transform.AsRT().sizeDelta = new Vector2(66f, 32f);
                replace.GetComponent<LayoutElement>().minWidth = 32f;

                replace.transform.GetChild(0).GetComponent<Text>().text = "Replace";
                replace.GetComponent<Image>().color = bcol;

                var button = replace.GetComponent<Button>();
                button.onClick.ClearAll();
                button.onClick.AddListener(delegate ()
                {
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                    {
                        var bm = timelineObject.GetData<BeatmapObject>();
                        bm.name = bm.name.Replace(oldNameIF.text, newNameIF.text);
                        ObjectEditor.inst.RenderTimelineObject(timelineObject);
                    }
                });
            }
            
            // Replace Tags
            {
                labelGenerator("Replace Tags");

                var replaceName = new GameObject("replace tags");
                replaceName.transform.SetParent(parent);
                replaceName.transform.localScale = Vector3.one;

                var multiSyncRT = replaceName.AddComponent<RectTransform>();
                multiSyncRT.sizeDelta = new Vector2(390f, 32f);
                var multiSyncGLG = replaceName.AddComponent<GridLayoutGroup>();
                multiSyncGLG.spacing = new Vector2(8f, 8f);
                multiSyncGLG.cellSize = new Vector2(124f, 32f);

                var oldName = GameObject.Find("TimelineBar/GameObject/Time Input").Duplicate(multiSyncRT, "old tag");

                Destroy(oldName.GetComponent<EventTrigger>());
                var oldNameIF = oldName.GetComponent<InputField>();
                oldNameIF.characterValidation = InputField.CharacterValidation.None;
                oldNameIF.textComponent.alignment = TextAnchor.MiddleLeft;
                oldNameIF.textComponent.fontSize = 16;
                oldNameIF.text = "Old Tag";
                ((Text)oldNameIF.placeholder).text = "Enter old tag...";
                ((Text)oldNameIF.placeholder).alignment = TextAnchor.MiddleLeft;
                ((Text)oldNameIF.placeholder).fontSize = 16;
                ((Text)oldNameIF.placeholder).color = new Color(0f, 0f, 0f, 0.3f);

                oldNameIF.onValueChanged.RemoveAllListeners();

                var oldNameSwapper = oldName.AddComponent<InputFieldSwapper>();
                oldNameSwapper.Init(oldNameIF, InputFieldSwapper.Type.String);

                var newName = GameObject.Find("TimelineBar/GameObject/Time Input").Duplicate(multiSyncRT, "new tag");

                Destroy(newName.GetComponent<EventTrigger>());
                var newNameIF = newName.GetComponent<InputField>();
                newNameIF.characterValidation = InputField.CharacterValidation.None;
                newNameIF.textComponent.alignment = TextAnchor.MiddleLeft;
                newNameIF.textComponent.fontSize = 16;
                newNameIF.text = "New Tag";
                ((Text)newNameIF.placeholder).text = "Enter new tag...";
                ((Text)newNameIF.placeholder).alignment = TextAnchor.MiddleLeft;
                ((Text)newNameIF.placeholder).fontSize = 16;
                ((Text)newNameIF.placeholder).color = new Color(0f, 0f, 0f, 0.3f);

                newNameIF.onValueChanged.RemoveAllListeners();

                var newNameSwapper = newName.AddComponent<InputFieldSwapper>();
                newNameSwapper.Init(newNameIF, InputFieldSwapper.Type.String);

                var replace = eventButton.Duplicate(replaceName.transform, "replace");
                replace.transform.localScale = Vector3.one;
                replace.transform.AsRT().sizeDelta = new Vector2(66f, 32f);
                replace.GetComponent<LayoutElement>().minWidth = 32f;

                replace.transform.GetChild(0).GetComponent<Text>().text = "Replace";
                replace.GetComponent<Image>().color = bcol;

                var button = replace.GetComponent<Button>();
                button.onClick.ClearAll();
                button.onClick.AddListener(delegate ()
                {
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                    {
                        var bm = timelineObject.GetData<BeatmapObject>();
                        for (int i = 0; i < bm.tags.Count; i++)
                        {
                            bm.tags[i] = bm.tags[i].Replace(oldNameIF.text, newNameIF.text);
                        }
                    }
                });
            }
            
            // Replace Text
            {
                labelGenerator("Replace Text");

                var replaceName = new GameObject("replace text");
                replaceName.transform.SetParent(parent);
                replaceName.transform.localScale = Vector3.one;

                var multiSyncRT = replaceName.AddComponent<RectTransform>();
                multiSyncRT.sizeDelta = new Vector2(390f, 32f);
                var multiSyncGLG = replaceName.AddComponent<GridLayoutGroup>();
                multiSyncGLG.spacing = new Vector2(8f, 8f);
                multiSyncGLG.cellSize = new Vector2(124f, 32f);

                var oldName = GameObject.Find("TimelineBar/GameObject/Time Input").Duplicate(multiSyncRT, "old text");

                Destroy(oldName.GetComponent<EventTrigger>());
                var oldNameIF = oldName.GetComponent<InputField>();
                oldNameIF.characterValidation = InputField.CharacterValidation.None;
                oldNameIF.textComponent.alignment = TextAnchor.MiddleLeft;
                oldNameIF.textComponent.fontSize = 16;
                oldNameIF.text = "Old Text";
                ((Text)oldNameIF.placeholder).text = "Enter old text...";
                ((Text)oldNameIF.placeholder).alignment = TextAnchor.MiddleLeft;
                ((Text)oldNameIF.placeholder).fontSize = 16;
                ((Text)oldNameIF.placeholder).color = new Color(0f, 0f, 0f, 0.3f);

                oldNameIF.onValueChanged.RemoveAllListeners();

                var oldNameSwapper = oldName.AddComponent<InputFieldSwapper>();
                oldNameSwapper.Init(oldNameIF, InputFieldSwapper.Type.String);

                var newName = GameObject.Find("TimelineBar/GameObject/Time Input").Duplicate(multiSyncRT, "new text");

                Destroy(newName.GetComponent<EventTrigger>());
                var newNameIF = newName.GetComponent<InputField>();
                newNameIF.characterValidation = InputField.CharacterValidation.None;
                newNameIF.textComponent.alignment = TextAnchor.MiddleLeft;
                newNameIF.textComponent.fontSize = 16;
                newNameIF.text = "New Text";
                ((Text)newNameIF.placeholder).text = "Enter new text...";
                ((Text)newNameIF.placeholder).alignment = TextAnchor.MiddleLeft;
                ((Text)newNameIF.placeholder).fontSize = 16;
                ((Text)newNameIF.placeholder).color = new Color(0f, 0f, 0f, 0.3f);

                newNameIF.onValueChanged.RemoveAllListeners();

                var newNameSwapper = newName.AddComponent<InputFieldSwapper>();
                newNameSwapper.Init(newNameIF, InputFieldSwapper.Type.String);

                var replace = eventButton.Duplicate(replaceName.transform, "replace");
                replace.transform.localScale = Vector3.one;
                replace.transform.AsRT().sizeDelta = new Vector2(66f, 32f);
                replace.GetComponent<LayoutElement>().minWidth = 32f;

                replace.transform.GetChild(0).GetComponent<Text>().text = "Replace";
                replace.GetComponent<Image>().color = bcol;

                var button = replace.GetComponent<Button>();
                button.onClick.ClearAll();
                button.onClick.AddListener(delegate ()
                {
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                    {
                        var bm = timelineObject.GetData<BeatmapObject>();
                        bm.text = bm.text.Replace(oldNameIF.text, newNameIF.text);
                        Updater.UpdateProcessor(bm, "Shape");
                    }
                });
            }

            // Replace Modifier
            if (ModCompatibility.ObjectModifiersInstalled)
            {
                labelGenerator("Replace Modifier Values");

                var replaceName = new GameObject("replace modifier");
                replaceName.transform.SetParent(parent);
                replaceName.transform.localScale = Vector3.one;

                var multiSyncRT = replaceName.AddComponent<RectTransform>();
                multiSyncRT.sizeDelta = new Vector2(390f, 32f);
                var multiSyncGLG = replaceName.AddComponent<GridLayoutGroup>();
                multiSyncGLG.spacing = new Vector2(8f, 8f);
                multiSyncGLG.cellSize = new Vector2(124f, 32f);

                var oldName = GameObject.Find("TimelineBar/GameObject/Time Input").Duplicate(multiSyncRT, "old modifier");

                Destroy(oldName.GetComponent<EventTrigger>());
                var oldNameIF = oldName.GetComponent<InputField>();
                oldNameIF.characterValidation = InputField.CharacterValidation.None;
                oldNameIF.textComponent.alignment = TextAnchor.MiddleLeft;
                oldNameIF.textComponent.fontSize = 16;
                oldNameIF.text = "Old Modifier";
                ((Text)oldNameIF.placeholder).text = "Enter old modifier...";
                ((Text)oldNameIF.placeholder).alignment = TextAnchor.MiddleLeft;
                ((Text)oldNameIF.placeholder).fontSize = 16;
                ((Text)oldNameIF.placeholder).color = new Color(0f, 0f, 0f, 0.3f);

                oldNameIF.onValueChanged.RemoveAllListeners();

                var oldNameSwapper = oldName.AddComponent<InputFieldSwapper>();
                oldNameSwapper.Init(oldNameIF, InputFieldSwapper.Type.String);

                var newName = GameObject.Find("TimelineBar/GameObject/Time Input").Duplicate(multiSyncRT, "new modifier");

                Destroy(newName.GetComponent<EventTrigger>());
                var newNameIF = newName.GetComponent<InputField>();
                newNameIF.characterValidation = InputField.CharacterValidation.None;
                newNameIF.textComponent.alignment = TextAnchor.MiddleLeft;
                newNameIF.textComponent.fontSize = 16;
                newNameIF.text = "New Modifier";
                ((Text)newNameIF.placeholder).text = "Enter new modifier...";
                ((Text)newNameIF.placeholder).alignment = TextAnchor.MiddleLeft;
                ((Text)newNameIF.placeholder).fontSize = 16;
                ((Text)newNameIF.placeholder).color = new Color(0f, 0f, 0f, 0.3f);

                newNameIF.onValueChanged.RemoveAllListeners();

                var newNameSwapper = newName.AddComponent<InputFieldSwapper>();
                newNameSwapper.Init(newNameIF, InputFieldSwapper.Type.String);

                var replace = eventButton.Duplicate(replaceName.transform, "replace");
                replace.transform.localScale = Vector3.one;
                replace.transform.AsRT().sizeDelta = new Vector2(66f, 32f);
                replace.GetComponent<LayoutElement>().minWidth = 32f;

                replace.transform.GetChild(0).GetComponent<Text>().text = "Replace";
                replace.GetComponent<Image>().color = bcol;

                var button = replace.GetComponent<Button>();
                button.onClick.ClearAll();
                button.onClick.AddListener(delegate ()
                {
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                    {
                        var bm = timelineObject.GetData<BeatmapObject>();

                        foreach (var modifier in bm.modifiers)
                        {
                            for (int i = 1; i < modifier.commands.Count; i++)
                            {
                                modifier.commands[i] = modifier.commands[i].Replace(oldNameIF.text, newNameIF.text);
                            }

                            modifier.value = modifier.value.Replace(oldNameIF.text, newNameIF.text);
                        }
                    }
                });
            }

            EditorManager.inst.GetDialog("Multi Object Editor").Dialog.Find("data").GetComponent<RectTransform>().sizeDelta = new Vector2(810f, 730.11f);
            EditorManager.inst.GetDialog("Multi Object Editor").Dialog.Find("data/left").GetComponent<RectTransform>().sizeDelta = new Vector2(355f, 730f);
        }

        public void CreatePropertiesWindow()
        {
            GameObject editorProperties = Instantiate(EditorManager.inst.GetDialog("Object Selector").Dialog.gameObject);
            editorProperties.name = "Editor Properties Popup";
            editorProperties.layer = 5;
            editorProperties.transform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/Popups").transform);
            editorProperties.transform.localScale = Vector3.one;
            editorProperties.transform.localPosition = Vector3.zero;

            var eSelect = editorProperties.AddComponent<SelectGUI>();
            eSelect.target = editorProperties.transform;
            eSelect.ogPos = editorProperties.transform.position;

            Text textFont = GameObject.Find("TitleBar/File/Text").GetComponent<Text>();
            var prefabTMP = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject/prefab");

            //Set Text and stuff
            {
                var searchField = editorProperties.transform.Find("search-box").GetChild(0).GetComponent<InputField>();
                searchField.onValueChanged.RemoveAllListeners();
                searchField.onValueChanged.AddListener(delegate (string _val)
                {
                    propertiesSearch = _val;
                    RenderPropertiesWindow();
                });
                searchField.placeholder.GetComponent<Text>().text = "Search for property...";
                editorProperties.transform.Find("Panel/Text").GetComponent<Text>().text = "Editor Properties";
            }

            //Sort Layout
            {
                editorProperties.transform.Find("mask/content").GetComponent<GridLayoutGroup>().cellSize = new Vector2(750f, 32f);
                editorProperties.GetComponent<RectTransform>().sizeDelta = new Vector2(750f, 450f);
                editorProperties.transform.Find("Panel").GetComponent<RectTransform>().sizeDelta = new Vector2(782f, 32f);
                editorProperties.transform.Find("search-box").GetComponent<RectTransform>().sizeDelta = new Vector2(750f, 32f);
                editorProperties.transform.Find("search-box").localPosition = new Vector3(0f, 195f, 0f);
                editorProperties.transform.Find("crumbs").GetComponent<RectTransform>().sizeDelta = new Vector2(750f, 32f);
                editorProperties.transform.Find("crumbs").localPosition = new Vector3(0f, 225f, 0f);
                editorProperties.transform.Find("crumbs").GetComponent<HorizontalLayoutGroup>().spacing = 5.5f;
            }

            //Categories
            {
                Action<string, Color, EditorProperty.EditorPropCategory> categoryTabGenerator = delegate (string name, Color color, EditorProperty.EditorPropCategory editorPropCategory)
                {
                    var gameObject = prefabTMP.Duplicate(editorProperties.transform.Find("crumbs"), name);
                    gameObject.layer = 5;
                    var rectTransform = (RectTransform)gameObject.transform;
                    var image = gameObject.GetComponent<Image>();
                    var button = gameObject.GetComponent<Button>();

                    var hoverUI = gameObject.AddComponent<HoverUI>();
                    hoverUI.ogPos = gameObject.transform.localPosition;
                    hoverUI.animPos = new Vector3(0f, 6f, 0f);
                    hoverUI.size = 1f;
                    hoverUI.animatePos = true;
                    hoverUI.animateSca = false;

                    rectTransform.sizeDelta = new Vector2(100f, 32f);
                    rectTransform.anchorMin = new Vector2(-0.1f, -0.1f);

                    image.color = color;
                    //categoryColors.Add(LSColors.HexToColor("FFE7E7"));

                    ColorBlock cb2 = button.colors;
                    cb2.normalColor = new Color(1f, 1f, 1f, 1f);
                    cb2.pressedColor = new Color(1.5f, 1.5f, 1.5f, 1f);
                    cb2.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
                    cb2.selectedColor = new Color(1f, 1f, 1f, 1f);
                    button.colors = cb2;

                    var hoverTooltip = gameObject.GetComponent<HoverTooltip>();

                    hoverTooltip.tooltipLangauges.Clear();
                    hoverTooltip.tooltipLangauges.Add(TooltipHelper.NewTooltip("Click on this to switch category.", ""));

                    button.onClick.ClearAll();
                    button.onClick.AddListener(delegate ()
                    {
                        currentCategory = editorPropCategory;
                        RenderPropertiesWindow();
                    });

                    var textGameObject = gameObject.transform.GetChild(0).gameObject;
                    textGameObject.transform.SetParent(gameObject.transform);
                    textGameObject.layer = 5;
                    var textRectTransform = textGameObject.GetComponent<RectTransform>();
                    var textText = textGameObject.GetComponent<Text>();

                    textRectTransform.anchoredPosition = Vector2.zero;
                    textText.text = name;
                    textText.alignment = TextAnchor.MiddleCenter;
                    textText.color = LSColors.ContrastColor(color);
                    textText.font = textFont.font;
                    textText.fontSize = 20;

                    var clickable = gameObject.AddComponent<Clickable>();
                    clickable.onEnter = delegate (PointerEventData pointerEventData)
                    {
                        var animation = new AnimationManager.Animation($"{name} Hover");
                        var id = animation.id;

                        animation.colorAnimations = new List<AnimationManager.Animation.AnimationObject<Color>>()
                        {
                            new AnimationManager.Animation.AnimationObject<Color>(new List<IKeyframe<Color>>()
                            {
                                new ColorKeyframe(0f, color, Ease.Linear),
                                new ColorKeyframe(0.2f, Color.white, Ease.SineOut),
                            }, delegate (Color x)
                            {
                                image.color = x;
                                textText.color = LSColors.ContrastColor(x);
                            }),
                        };

                        AnimationManager.inst.RemoveName($"{name} Exit");
                        AnimationManager.inst.Play(animation);
                    };
                    clickable.onExit = delegate (PointerEventData pointerEventData)
                    {
                        var animation = new AnimationManager.Animation($"{name} Exit");
                        var id = animation.id;

                        animation.colorAnimations = new List<AnimationManager.Animation.AnimationObject<Color>>()
                        {
                            new AnimationManager.Animation.AnimationObject<Color>(new List<IKeyframe<Color>>()
                            {
                                new ColorKeyframe(0f, Color.white, Ease.Linear),
                                new ColorKeyframe(0.2f, color, Ease.SineIn),
                            }, delegate (Color x)
                            {
                                image.color = x;
                                textText.color = LSColors.ContrastColor(x);
                            }),
                        };

                        AnimationManager.inst.RemoveName($"{name} Hover");
                        AnimationManager.inst.Play(animation);
                    };
                };

                categoryTabGenerator("General", LSColors.HexToColor("FFE7E7"), EditorProperty.EditorPropCategory.General);
                categoryTabGenerator("Timeline", LSColors.HexToColor("C0ACE1"), EditorProperty.EditorPropCategory.Timeline);
                categoryTabGenerator("Data", LSColors.HexToColor("F17BB8"), EditorProperty.EditorPropCategory.Data);
                categoryTabGenerator("Editor GUI", LSColors.HexToColor("2F426D"), EditorProperty.EditorPropCategory.EditorGUI);
                categoryTabGenerator("Functions", LSColors.HexToColor("4076DF"), EditorProperty.EditorPropCategory.Functions);
                categoryTabGenerator("Fields", LSColors.HexToColor("6CCBCF"), EditorProperty.EditorPropCategory.Fields);
                categoryTabGenerator("Preview", LSColors.HexToColor("1B1B1C"), EditorProperty.EditorPropCategory.Preview);

                ////General
                //{
                //    var gameObject = Instantiate(prefabTMP);
                //    gameObject.name = "general";
                //    gameObject.transform.SetParent(editorProperties.transform.Find("crumbs"));
                //    gameObject.layer = 5;
                //    var rectTransform = (RectTransform)gameObject.transform;
                //    var image = gameObject.GetComponent<Image>();
                //    var button = gameObject.GetComponent<Button>();

                //    var hoverUI = gameObject.AddComponent<HoverUI>();
                //    hoverUI.ogPos = gameObject.transform.localPosition;
                //    hoverUI.animPos = new Vector3(0f, 6f, 0f);
                //    hoverUI.size = 1f;
                //    hoverUI.animatePos = true;
                //    hoverUI.animateSca = false;

                //    rectTransform.sizeDelta = new Vector2(100f, 32f);
                //    rectTransform.anchorMin = new Vector2(-0.1f, -0.1f);

                //    image.color = LSColors.HexToColor("FFE7E7");
                //    //categoryColors.Add(LSColors.HexToColor("FFE7E7"));

                //    ColorBlock cb2 = button.colors;
                //    cb2.normalColor = new Color(1f, 1f, 1f, 1f);
                //    cb2.pressedColor = new Color(1.5f, 1.5f, 1.5f, 1f);
                //    cb2.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
                //    cb2.selectedColor = new Color(1f, 1f, 1f, 1f);
                //    button.colors = cb2;

                //    var hoverTooltip = gameObject.GetComponent<HoverTooltip>();

                //    hoverTooltip.tooltipLangauges.Clear();
                //    hoverTooltip.tooltipLangauges.Add(TooltipHelper.NewTooltip("General Editor Settings", ""));

                //    button.onClick.ClearAll();
                //    button.onClick.AddListener(delegate ()
                //    {
                //        currentCategory = EditorProperty.EditorPropCategory.General;
                //        RenderPropertiesWindow();
                //    });

                //    var textGameObject = gameObject.transform.GetChild(0).gameObject;
                //    textGameObject.transform.SetParent(gameObject.transform);
                //    textGameObject.layer = 5;
                //    var textRectTransform = textGameObject.GetComponent<RectTransform>();
                //    var textText = textGameObject.GetComponent<Text>();

                //    textRectTransform.anchoredPosition = Vector2.zero;
                //    textText.text = "General";
                //    textText.alignment = TextAnchor.MiddleCenter;
                //    textText.color = new Color(0.1294f, 0.1294f, 0.1294f, 1f);
                //    textText.font = textFont.font;
                //    textText.fontSize = 20;
                //}

                ////Timeline
                //{
                //    var gameObject = Instantiate(prefabTMP);
                //    gameObject.name = "timeline";
                //    gameObject.transform.SetParent(editorProperties.transform.Find("crumbs"));
                //    gameObject.layer = 5;
                //    RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
                //    Image image = gameObject.GetComponent<Image>();
                //    Button button = gameObject.GetComponent<Button>();

                //    var hoverUI = gameObject.AddComponent<HoverUI>();
                //    hoverUI.ogPos = gameObject.transform.localPosition;
                //    hoverUI.animPos = new Vector3(0f, 6f, 0f);
                //    hoverUI.size = 1f;
                //    hoverUI.animatePos = true;
                //    hoverUI.animateSca = false;

                //    rectTransform.sizeDelta = new Vector2(100f, 32f);
                //    rectTransform.anchorMin = new Vector2(-0.1f, -0.1f);

                //    image.color = LSColors.HexToColor("C0ACE1");
                //    //categoryColors.Add(LSColors.HexToColor("C0ACE1"));

                //    ColorBlock cb2 = button.colors;
                //    cb2.normalColor = new Color(1f, 1f, 1f, 1f);
                //    cb2.pressedColor = new Color(1.5f, 1.5f, 1.5f, 1f);
                //    cb2.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
                //    cb2.selectedColor = new Color(1f, 1f, 1f, 1f);
                //    button.colors = cb2;

                //    HoverTooltip hoverTooltip = gameObject.GetComponent<HoverTooltip>();

                //    hoverTooltip.tooltipLangauges.Clear();
                //    hoverTooltip.tooltipLangauges.Add(Triggers.NewTooltip("Timeline Settings", ""));

                //    button.onClick.ClearAll();
                //    button.onClick.AddListener(delegate ()
                //    {
                //        currentCategory = EditorProperty.EditorPropCategory.Timeline;
                //        RenderPropertiesWindow();
                //    });

                //    GameObject textGameObject = gameObject.transform.GetChild(0).gameObject;
                //    textGameObject.transform.SetParent(gameObject.transform);
                //    textGameObject.layer = 5;
                //    RectTransform textRectTransform = textGameObject.GetComponent<RectTransform>();
                //    Text textText = textGameObject.GetComponent<Text>();

                //    textRectTransform.anchoredPosition = Vector2.zero;
                //    textText.text = "Timeline";
                //    textText.alignment = TextAnchor.MiddleCenter;
                //    textText.color = new Color(0.1294f, 0.1294f, 0.1294f, 1f);
                //    textText.font = textFont.font;
                //    textText.fontSize = 20;
                //}

                ////Data
                //{
                //    GameObject gameObject = Instantiate(prefabTMP);
                //    gameObject.name = "saving";
                //    gameObject.transform.SetParent(editorProperties.transform.Find("crumbs"));
                //    gameObject.layer = 5;
                //    RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
                //    Image image = gameObject.GetComponent<Image>();
                //    Button button = gameObject.GetComponent<Button>();

                //    var hoverUI = gameObject.AddComponent<HoverUI>();
                //    hoverUI.ogPos = gameObject.transform.localPosition;
                //    hoverUI.animPos = new Vector3(0f, 6f, 0f);
                //    hoverUI.size = 1f;
                //    hoverUI.animatePos = true;
                //    hoverUI.animateSca = false;

                //    rectTransform.sizeDelta = new Vector2(100f, 32f);
                //    rectTransform.anchorMin = new Vector2(-0.1f, -0.1f);

                //    image.color = LSColors.HexToColor("F17BB8");
                //    //categoryColors.Add(LSColors.HexToColor("F17BB8"));

                //    ColorBlock cb2 = button.colors;
                //    cb2.normalColor = new Color(1f, 1f, 1f, 1f);
                //    cb2.pressedColor = new Color(1.5f, 1.5f, 1.5f, 1f);
                //    cb2.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
                //    cb2.selectedColor = new Color(1f, 1f, 1f, 1f);
                //    button.colors = cb2;

                //    HoverTooltip hoverTooltip = gameObject.GetComponent<HoverTooltip>();

                //    hoverTooltip.tooltipLangauges.Clear();
                //    hoverTooltip.tooltipLangauges.Add(Triggers.NewTooltip("Data Settings", ""));

                //    button.onClick.ClearAll();
                //    button.onClick.AddListener(delegate ()
                //    {
                //        currentCategory = EditorProperty.EditorPropCategory.Data;
                //        RenderPropertiesWindow();
                //    });

                //    GameObject textGameObject = gameObject.transform.GetChild(0).gameObject;
                //    textGameObject.transform.SetParent(gameObject.transform);
                //    textGameObject.layer = 5;
                //    RectTransform textRectTransform = textGameObject.GetComponent<RectTransform>();
                //    textGameObject.GetComponent<CanvasRenderer>();
                //    Text textText = textGameObject.GetComponent<Text>();

                //    textRectTransform.anchoredPosition = Vector2.zero;
                //    textText.text = "Data";
                //    textText.alignment = TextAnchor.MiddleCenter;
                //    textText.color = new Color(0.1294f, 0.1294f, 0.1294f, 1f);
                //    textText.font = textFont.font;
                //    textText.fontSize = 20;
                //}

                ////Editor GUI
                //{
                //    GameObject gameObject = Instantiate(prefabTMP);
                //    gameObject.name = "editorgui";
                //    gameObject.transform.SetParent(editorProperties.transform.Find("crumbs"));
                //    gameObject.layer = 5;
                //    RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
                //    Image image = gameObject.GetComponent<Image>();
                //    Button button = gameObject.GetComponent<Button>();

                //    var hoverUI = gameObject.AddComponent<HoverUI>();
                //    hoverUI.ogPos = gameObject.transform.localPosition;
                //    hoverUI.animPos = new Vector3(0f, 6f, 0f);
                //    hoverUI.size = 1f;
                //    hoverUI.animatePos = true;
                //    hoverUI.animateSca = false;

                //    rectTransform.sizeDelta = new Vector2(100f, 32f);
                //    rectTransform.anchorMin = new Vector2(-0.1f, -0.1f);

                //    image.color = LSColors.HexToColor("2F426D");
                //    //categoryColors.Add(LSColors.HexToColor("2F426D"));

                //    ColorBlock cb2 = button.colors;
                //    cb2.normalColor = new Color(1f, 1f, 1f, 1f);
                //    cb2.pressedColor = new Color(1.5f, 1.5f, 1.5f, 1f);
                //    cb2.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
                //    cb2.selectedColor = new Color(1f, 1f, 1f, 1f);
                //    button.colors = cb2;

                //    HoverTooltip hoverTooltip = gameObject.GetComponent<HoverTooltip>();

                //    hoverTooltip.tooltipLangauges.Clear();
                //    hoverTooltip.tooltipLangauges.Add(Triggers.NewTooltip("GUI Settings", ""));

                //    button.onClick.ClearAll();
                //    button.onClick.AddListener(delegate ()
                //    {
                //        currentCategory = EditorProperty.EditorPropCategory.EditorGUI;
                //        RenderPropertiesWindow();
                //    });

                //    GameObject textGameObject = gameObject.transform.GetChild(0).gameObject;
                //    textGameObject.transform.SetParent(gameObject.transform);
                //    textGameObject.layer = 5;
                //    RectTransform textRectTransform = textGameObject.GetComponent<RectTransform>();
                //    Text textText = textGameObject.GetComponent<Text>();

                //    textRectTransform.anchoredPosition = Vector2.zero;
                //    textText.text = "Editor GUI";
                //    textText.alignment = TextAnchor.MiddleCenter;
                //    textText.color = new Color(0.1294f, 0.1294f, 0.1294f, 1f);
                //    textText.font = textFont.font;
                //    textText.fontSize = 20;
                //}

                ////Functions
                //{
                //    GameObject gameObject = Instantiate(prefabTMP);
                //    gameObject.name = "functions";
                //    gameObject.transform.SetParent(editorProperties.transform.Find("crumbs"));
                //    gameObject.layer = 5;
                //    RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
                //    Image image = gameObject.GetComponent<Image>();
                //    Button button = gameObject.GetComponent<Button>();

                //    var hoverUI = gameObject.AddComponent<HoverUI>();
                //    hoverUI.ogPos = gameObject.transform.localPosition;
                //    hoverUI.animPos = new Vector3(0f, 6f, 0f);
                //    hoverUI.size = 1f;
                //    hoverUI.animatePos = true;
                //    hoverUI.animateSca = false;

                //    rectTransform.sizeDelta = new Vector2(100f, 32f);
                //    rectTransform.anchorMin = new Vector2(-0.1f, -0.1f);

                //    image.color = LSColors.HexToColor("4076DF");
                //    //categoryColors.Add(LSColors.HexToColor("4076DF"));

                //    ColorBlock cb2 = button.colors;
                //    cb2.normalColor = new Color(1f, 1f, 1f, 1f);
                //    cb2.pressedColor = new Color(1.5f, 1.5f, 1.5f, 1f);
                //    cb2.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
                //    cb2.selectedColor = new Color(1f, 1f, 1f, 1f);
                //    button.colors = cb2;

                //    HoverTooltip hoverTooltip = gameObject.GetComponent<HoverTooltip>();

                //    hoverTooltip.tooltipLangauges.Clear();
                //    hoverTooltip.tooltipLangauges.Add(Triggers.NewTooltip("Functions Settings", ""));

                //    button.onClick.ClearAll();
                //    button.onClick.AddListener(delegate ()
                //    {
                //        currentCategory = EditorProperty.EditorPropCategory.Functions;
                //        RenderPropertiesWindow();
                //    });

                //    GameObject textGameObject = gameObject.transform.GetChild(0).gameObject;
                //    textGameObject.transform.SetParent(gameObject.transform);
                //    textGameObject.layer = 5;
                //    RectTransform textRectTransform = textGameObject.GetComponent<RectTransform>();
                //    Text textText = textGameObject.GetComponent<Text>();

                //    textRectTransform.anchoredPosition = Vector2.zero;
                //    textText.text = "Functions";
                //    textText.alignment = TextAnchor.MiddleCenter;
                //    textText.color = new Color(0.1294f, 0.1294f, 0.1294f, 1f);
                //    textText.font = textFont.font;
                //    textText.fontSize = 20;
                //}

                ////Fields
                //{
                //    GameObject gameObject = Instantiate(prefabTMP);
                //    gameObject.name = "fields";
                //    gameObject.transform.SetParent(editorProperties.transform.Find("crumbs"));
                //    gameObject.layer = 5;
                //    RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
                //    Image image = gameObject.GetComponent<Image>();
                //    Button button = gameObject.GetComponent<Button>();

                //    var hoverUI = gameObject.AddComponent<HoverUI>();
                //    hoverUI.ogPos = gameObject.transform.localPosition;
                //    hoverUI.animPos = new Vector3(0f, 6f, 0f);
                //    hoverUI.size = 1f;
                //    hoverUI.animatePos = true;
                //    hoverUI.animateSca = false;

                //    rectTransform.sizeDelta = new Vector2(100f, 32f);
                //    rectTransform.anchorMin = new Vector2(-0.1f, -0.1f);

                //    image.color = LSColors.HexToColor("6CCBCF");
                //    //categoryColors.Add(LSColors.HexToColor("6CCBCF"));

                //    ColorBlock cb2 = button.colors;
                //    cb2.normalColor = new Color(1f, 1f, 1f, 1f);
                //    cb2.pressedColor = new Color(1.5f, 1.5f, 1.5f, 1f);
                //    cb2.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
                //    cb2.selectedColor = new Color(1f, 1f, 1f, 1f);
                //    button.colors = cb2;

                //    HoverTooltip hoverTooltip = gameObject.GetComponent<HoverTooltip>();

                //    hoverTooltip.tooltipLangauges.Clear();
                //    hoverTooltip.tooltipLangauges.Add(Triggers.NewTooltip("Fields Settings", ""));

                //    button.onClick.ClearAll();
                //    button.onClick.AddListener(delegate ()
                //    {
                //        currentCategory = EditorProperty.EditorPropCategory.Fields;
                //        RenderPropertiesWindow();
                //    });

                //    GameObject textGameObject = gameObject.transform.GetChild(0).gameObject;
                //    textGameObject.transform.SetParent(gameObject.transform);
                //    textGameObject.layer = 5;
                //    RectTransform textRectTransform = textGameObject.GetComponent<RectTransform>();
                //    Text textText = textGameObject.GetComponent<Text>();

                //    textRectTransform.anchoredPosition = Vector2.zero;
                //    textText.text = "Fields";
                //    textText.alignment = TextAnchor.MiddleCenter;
                //    textText.color = new Color(0.1294f, 0.1294f, 0.1294f, 1f);
                //    textText.font = textFont.font;
                //    textText.fontSize = 20;
                //}

                ////Preview
                //{
                //    GameObject gameObject = Instantiate(prefabTMP);
                //    gameObject.name = "preview";
                //    gameObject.transform.SetParent(editorProperties.transform.Find("crumbs"));
                //    gameObject.layer = 5;
                //    RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
                //    Image image = gameObject.GetComponent<Image>();
                //    Button button = gameObject.GetComponent<Button>();

                //    var hoverUI = gameObject.AddComponent<HoverUI>();
                //    hoverUI.ogPos = gameObject.transform.localPosition;
                //    hoverUI.animPos = new Vector3(0f, 6f, 0f);
                //    hoverUI.size = 1f;
                //    hoverUI.animatePos = true;
                //    hoverUI.animateSca = false;

                //    rectTransform.sizeDelta = new Vector2(100f, 32f);
                //    rectTransform.anchorMin = new Vector2(-0.1f, -0.1f);

                //    image.color = LSColors.HexToColor("1B1B1C");
                //    //categoryColors.Add(LSColors.HexToColor("1B1B1C"));

                //    ColorBlock cb2 = button.colors;
                //    cb2.normalColor = new Color(1f, 1f, 1f, 1f);
                //    cb2.pressedColor = new Color(1.5f, 1.5f, 1.5f, 1f);
                //    cb2.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
                //    cb2.selectedColor = new Color(1f, 1f, 1f, 1f);
                //    button.colors = cb2;

                //    HoverTooltip hoverTooltip = gameObject.GetComponent<HoverTooltip>();

                //    hoverTooltip.tooltipLangauges.Clear();
                //    hoverTooltip.tooltipLangauges.Add(Triggers.NewTooltip("Preview Settings", ""));

                //    button.onClick.ClearAll();
                //    button.onClick.AddListener(delegate ()
                //    {
                //        currentCategory = EditorProperty.EditorPropCategory.Preview;
                //        RenderPropertiesWindow();
                //    });

                //    GameObject textGameObject = gameObject.transform.GetChild(0).gameObject;
                //    textGameObject.transform.SetParent(gameObject.transform);
                //    textGameObject.layer = 5;
                //    RectTransform textRectTransform = textGameObject.GetComponent<RectTransform>();
                //    Text textText = textGameObject.GetComponent<Text>();

                //    textRectTransform.anchoredPosition = Vector2.zero;
                //    textText.text = "Preview";
                //    textText.alignment = TextAnchor.MiddleCenter;
                //    textText.color = new Color(0.95f, 0.95f, 0.95f, 1f);
                //    textText.font = textFont.font;
                //    textText.fontSize = 20;
                //}
            }

            EditorHelper.AddEditorDropdown("Preferences", "", "Edit", SpriteManager.LoadSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_preferences-white.png"), delegate ()
            {
                OpenPropertiesWindow();
            });

            //var propWin = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/Edit/Edit Dropdown/Cut"));
            //propWin.transform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/Edit/Edit Dropdown").transform);
            //propWin.transform.localScale = Vector3.one;
            //propWin.name = "Preferences";
            //propWin.transform.Find("Text").GetComponent<Text>().text = "Preferences";
            //propWin.transform.Find("Text 1").GetComponent<Text>().text = "F10";

            //var propWinButton = propWin.GetComponent<Button>();
            //propWinButton.onClick.ClearAll();
            //propWinButton.onClick.AddListener(delegate ()
            //{
            //    OpenPropertiesWindow();
            //});

            //propWin.SetActive(true);


            //string jpgFileLocation = "BepInEx/plugins/Assets/editor_gui_preferences-white.png";

            //if (RTFile.FileExists(jpgFileLocation))
            //{
            //    Image spriteReloader = propWin.transform.Find("Image").GetComponent<Image>();

            //    EditorManager.inst.StartCoroutine(EditorManager.inst.GetSprite(RTFile.ApplicationDirectory + jpgFileLocation, new EditorManager.SpriteLimits(), delegate (Sprite cover)
            //    {
            //        spriteReloader.sprite = cover;
            //    }, delegate (string errorFile)
            //    {
            //        spriteReloader.sprite = ArcadeManager.inst.defaultImage;
            //    }));
            //}

            editorProperties.transform.Find("Panel/x").GetComponent<Button>().onClick.RemoveAllListeners();
            editorProperties.transform.Find("Panel/x").GetComponent<Button>().onClick.AddListener(delegate ()
            {
                ClosePropertiesWindow();
            });

            //Add Editor Properties Popup to EditorDialogsDictionary
            {
                EditorHelper.AddEditorPopup("Editor Properties Popup", editorProperties);
            }
        }

        public Text documentationTitle;
        public string documentationSearch;
        public Transform documentationContent;
        public void CreateDocumentation()
        {
            var objectSearch = EditorManager.inst.GetDialog("Parent Selector").Dialog.gameObject
                .Duplicate(EditorManager.inst.GetDialog("Parent Selector").Dialog.GetParent(), "Documentation Popup");
            objectSearch.transform.localPosition = Vector3.zero;

            var objectSearchRT = (RectTransform)objectSearch.transform;
            objectSearchRT.sizeDelta = new Vector2(600f, 450f);
            var objectSearchPanel = (RectTransform)objectSearch.transform.Find("Panel");
            objectSearchPanel.sizeDelta = new Vector2(632f, 32f);
            objectSearchPanel.transform.Find("Text").GetComponent<Text>().text = "Documentation";
            ((RectTransform)objectSearch.transform.Find("search-box")).sizeDelta = new Vector2(600f, 32f);
            objectSearch.transform.Find("mask/content").GetComponent<GridLayoutGroup>().cellSize = new Vector2(600f, 32f);

            var x = objectSearchPanel.transform.Find("x").GetComponent<Button>();
            x.onClick.RemoveAllListeners();
            x.onClick.AddListener(delegate ()
            {
                EditorManager.inst.HideDialog("Documentation Popup");
            });

            var searchBar = objectSearch.transform.Find("search-box/search").GetComponent<InputField>();
            searchBar.onValueChanged.ClearAll();
            searchBar.onValueChanged.AddListener(delegate (string _value)
            {
                documentationSearch = _value;
                RefreshDocumentation();
            });
            searchBar.transform.Find("Placeholder").GetComponent<Text>().text = "Search for document...";

            EditorHelper.AddEditorDropdown("Wiki / Documentation", "", "Help", SpriteManager.LoadSprite(RTFile.ApplicationDirectory + RTFunctions.FunctionsPlugin.BepInExAssetsPath + "editor_gui_question.png"), delegate ()
            {
                EditorManager.inst.ShowDialog("Documentation Popup");
                RefreshDocumentation();
            });

            EditorHelper.AddEditorPopup("Documentation Popup", objectSearch);

            var editorDialogObject = Instantiate(EditorManager.inst.GetDialog("Multi Keyframe Editor (Object)").Dialog.gameObject);
            var editorDialogTransform = editorDialogObject.transform;
            editorDialogObject.name = "DocumentationDialog";
            editorDialogObject.layer = 5;
            editorDialogTransform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs").transform);
            editorDialogTransform.localScale = Vector3.one;
            editorDialogTransform.position = new Vector3(1537.5f, 714.945f, 0f) * EditorManager.inst.ScreenScale;
            editorDialogTransform.AsRT().sizeDelta = new Vector2(0f, 32f);

            var editorDialogTitle = editorDialogTransform.GetChild(0);
            editorDialogTitle.GetComponent<Image>().color = LSColors.HexToColor("D89356");
            documentationTitle = editorDialogTitle.GetChild(0).GetComponent<Text>();
            documentationTitle.text = "- Documentation -";

            var editorDialogSpacer = editorDialogTransform.GetChild(1);
            editorDialogSpacer.AsRT().sizeDelta = new Vector2(765f, 54f);

            Destroy(editorDialogTransform.GetChild(2).gameObject);

            EditorHelper.AddEditorDialog("Documentation Dialog", editorDialogObject);

            var scrollView = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View"));
            documentationContent = scrollView.transform.Find("Viewport/Content");
            scrollView.transform.SetParent(editorDialogTransform);
            scrollView.transform.localScale = Vector3.one;
            scrollView.name = "Scroll View";

            LSHelpers.DeleteChildren(documentationContent);

            var scrollViewLE = scrollView.AddComponent<LayoutElement>();
            scrollViewLE.ignoreLayout = true;

            scrollView.transform.AsRT().anchoredPosition = new Vector2(392.5f, 320f);
            scrollView.transform.AsRT().sizeDelta = new Vector2(735f, 638f);

            // Introduction
            {
                var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(objectSearch.transform.Find("mask/content"), "Document");
                var documentation = new Document(gameObject, "Introduction", "Welcome to Project Arrhythmia.");

                // Intro
                {
                    var element = new Document.Element("Welcome to <b>Project Arrhythmia</b>!\nWhether you're new to the game, modding or have been around for a while, I'm sure this " +
                        "documentation will help massively in understanding the ins and outs of the editor and the game as a whole.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Info
                {
                    var element = new Document.Element("<b>DOCUMENTATION INFO</b>", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }
                
                // Vanilla
                {
                    var element = new Document.Element("<b>[VANILLA]</b> represents a feature from original Legacy, with very minor tweaks done to it if any.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }
                
                // Modded
                {
                    var element = new Document.Element("<b>[MODDED]</b> represents a feature added by mods. These features will not work in unmodded PA.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Patched
                {
                    var element = new Document.Element("<b>[PATCHED]</b> represents a feature modified by mods. They're either in newer versions of PA or are partially modded, meaning they might not work in regular PA.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }
                
                var htt = gameObject.AddComponent<HoverTooltip>();

                var levelTip = new HoverTooltip.Tooltip();

                levelTip.desc = documentation.Name;
                levelTip.hint = documentation.Description;
                htt.tooltipLangauges.Add(levelTip);

                var text = gameObject.transform.GetChild(0).GetComponent<Text>();

                text.text = documentation.Name;

                documentations.Add(documentation);
            }
            
            // Beatmap Objects
            {
                var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(objectSearch.transform.Find("mask/content"), "Document");
                var documentation = new Document(gameObject, "Beatmap Objects", "The very objects that make up Project Arrhythmia levels.");

                // Intro
                {
                    var element = new Document.Element("<b>Beatmap Objects</b> are the objects people use to create a variety of things for their levels. " +
                        "Whether it be backgrounds, characters, attacks, you name it! Below is a list of data Beatmap Objects have.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // ID
                {
                    var element = new Document.Element("<b>ID [PATCHED]</b>\nThe ID is used for specifying a Beatmap Object, otherwise it'd most likely get lost in a sea of other objects! " +
                        "It's mostly used with parenting. This is patched because in unmodded PA, creators aren't able to see the ID of an object unless they look at the level.lsb.\n" +
                        "Clicking on the ID will copy it to your clipboard.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }
                
                // LDM
                {
                    var element = new Document.Element("<b>LDM (Low Detail Mode) [MODDED]</b>\nLDM is useful for having objects not render for lower end devices. If the option is on and the user has " +
                        "Low Detail Mode enabled through the RTFunctions mod config, the Beatmap Object will not render.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // ID & LDM Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_id_ldm.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Name
                {
                    var element = new Document.Element("<b>Name [VANILLA]</b>\nNaming an object is incredibly helpful for readablility and knowing what an object does at a glance. " +
                        "Clicking your scroll wheel over it will flip any left / right.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Object Type
                {
                    var element = new Document.Element("<b>Object Type [PATCHED]</b>\nThis makes the objects' physics act in different ways." +
                        "\n<b>[VANILLA]</b> Normal hits the player." +
                        "\n<b>[VANILLA]</b> Helper is transparent, doesn't hit the player and is a good opacity template to use for warnings." +
                        "\n<b>[VANILLA]</b> Decoration doesn't hit the player." +
                        "\n<b>[VANILLA]</b> Empty doesn't render." +
                        "\n<b>[MODDED]</b> Solid prevents players from passing through itself but doesn't hit them.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Name & Type Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_name_type.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Tags
                {
                    var element = new Document.Element("<b>Tags [MODDED]</b>\nBeing able to group objects together or even specify things about an object is possible with Object Tags. This feature " +
                        "is mostly used by ObjectModifiers, but can be used in other ways such as a \"DontRotate\" tag which prevents Player Shapes from rotating automatically.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Tags Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_tags.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Locked
                {
                    var element = new Document.Element("<b>Locked [PATCHED]</b>\nIf on, prevents Beatmap Objects' start time from being changed. It's patched because unmodded PA doesn't " +
                        "have the toggle UI for this, however you can still use it in unmodded PA via hitting Ctrl + L.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Start Time
                {
                    var element = new Document.Element("<b>Start Time [VANILLA]</b>\nUsed for when the Beatmap Object spawns.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Start Time Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_start_time.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Time of Death
                {
                    var element = new Document.Element("<b>Time of Death [VANILLA]</b>\nUsed for when the Beatmap Object despawns." +
                        "\n<b>[PATCHED]</b> No Autokill - Beatmap Objects never despawn. This option is viable in modded PA due to heavily optimized object code, so don't worry " +
                        "about having a couple of objects with this. Just make sure to only use this when necessary, like for backgrounds or a persistent character." +
                        "\n<b>[VANILLA]</b> Last KF - Beatmap Objects despawn once all animations are finished. This does NOT include parent animations. When the level " +
                        "time reaches after the last keyframe, the object despawns." +
                        "\n<b>[VANILLA]</b> Last KF Offset - Same as above but at an offset." +
                        "\n<b>[VANILLA]</b> Fixed Time - Beatmap Objects despawn at a fixed time, regardless of animations. Fixed time is Beatmap Objects Start Time with an offset added to it." +
                        "\n<b>[VANILLA]</b> Song Time - Same as above, except it ignores the Beatmap Object Start Time, despawning the object at song time.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Collapse
                {
                    var element = new Document.Element("<b>Collapse [VANILLA]</b>\nBeatmap Objects in the editor timeline have their length shortened to the smallest amount if this is on.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Time of Death Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_tod.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Parent Search
                {
                    var element = new Document.Element("<b>Parent Search [PATCHED]</b>\nHere you can search for an object to parent the Beatmap Object to. It includes Camera Parenting.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Parent Search Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_parent_search.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Camera Parent
                {
                    var element = new Document.Element("<b>Camera Parent [MODDED]</b>\nBeatmap Objects parented to the camera will always follow it, depending on the parent settings. This includes " +
                        "anything that makes the camera follow the player. This feature does exist in modern PA, but doesn't work the same way this does.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Clear Parent
                {
                    var element = new Document.Element("<b>Clear Parent [MODDED]</b>\nClicking this will remove the Beatmap Object from its parent.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Parent Picker
                {
                    var element = new Document.Element("<b>Parent Picker [MODDED]</b>\nClicking this will activate a dropper. Right clicking will deactivate the dropper. Clicking on an object " +
                        "in the timeline will set the current selected Beatmap Objects parent to the selected Timeline Object.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Parent Display
                {
                    var element = new Document.Element("<b>Parent Display [VANILLA]</b>\nShows what the Beatmap Object is parented to. Clicking this button selects the parent. " +
                        "Hovering your mouse over it shows parent chain info in the Hover Info box.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Parent Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_parent.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Parent Settings
                {
                    var element = new Document.Element("<b>Parent Settings [PATCHED]</b>\nParent settings can be adjusted here. Each of the below settings refer to both " +
                        "position / scale / rotation. Position, scale and rotation are the rows and the types of Parent Settings are the columns.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Parent Type
                {
                    var element = new Document.Element("<b>Parent Type [VANILLA]</b>\nWhether the Beatmap Object applies this type of animation from the parent. " +
                        "It is the first column in the Parent Settings UI.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Parent Offset
                {
                    var element = new Document.Element("<b>Parent Offset [VANILLA]</b>\nParent animations applied to the Beatmap Objects own parent chain get delayed at this offset. Normally, only " +
                        "the objects current parent gets delayed. " +
                        "It is the second column in the Parent Settings UI.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Parent Additive
                {
                    var element = new Document.Element("<b>Parent Additive [MODDED]</b>\nForces Parent Offset to apply to every parent chain connected to the Beatmap Object. With this off, it only " +
                        "uses the Beatmap Objects' current parent. For example, say we have objects A, B, C and D. With this on, D delays the animation of every parent. With this off, it delays only C. " +
                        "It is the third column in the Parent Settings UI.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Parent Parallax
                {
                    var element = new Document.Element("<b>Parent Parallax [MODDED]</b>\nParent animations are multiplied by this amount, allowing for a parallax effect. Say the amount was 2 and the parent " +
                        "moves to position X 20, the object would move to 40 due to it being multiplied by 2. " +
                        "It is the fourth column in the Parent Settings UI.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Parent Advanced Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_parent_more.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Origin
                {
                    var element = new Document.Element("<b>Origin [PATCHED]</b>\nOrigin is the offset applied to the visual of the Beatmap Object. Only usable for non-Empty object types. " +
                        "It's patched because of the number input fields instead of the direction buttons.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Origin Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_origin.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Shape
                {
                    var element = new Document.Element("<b>Shape [PATCHED]</b>\nShape is whatever the visual of the Beatmap Object displays as. This doesn't just include actual shapes but stuff " +
                        "like text, images and player models too. More shape types and options were added. Unmodded PA does not include Image Shape, Pentagon Shape, Misc Shape, Player Shape.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Shape Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_shape.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Render Depth
                {
                    var element = new Document.Element("<b>Render Depth [PATCHED]</b>\nDepth is how deep an object is in visual layers. Higher amount of Render Depth means the object is lower " +
                        "in the layers. Unmodded PA Legacy allows from 219 to -98. PA Alpha only allows from 40 to 0. Player is located at -60 depth. Z Axis Position keyframes use depth as a " +
                        "multiplied offset.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Render Depth Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_depth.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Render Type
                {
                    var element = new Document.Element("<b>Render Type [MODDED]</b>\nRender Type is if the visual of the Beatmap Object renders in the 2D layer or the 3D layer, aka Foreground / Background.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Render Type Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_render_type.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Layer
                {
                    var element = new Document.Element("<b>Layer [PATCHED]</b>\nLayer is what editor layer the Beatmap Object renders on. It can go as high as 2147483646. " +
                        "In unmodded PA its limited from layers 1 to 5, though in PA Editor Alpha another layer was introduced.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Bin
                {
                    var element = new Document.Element("<b>Bin [VANILLA]</b>\nBin is what row of the timeline the Beatmap Objects' timeline object renders on.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Editor Data Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_editordata.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Object Debug
                {
                    var element = new Document.Element("<b>Object Debug [MODDED]</b>\nThis UI element only generates if UnityExplorer is installed. If it is, clicking on either button will inspect " +
                        "the internal data of the respective item.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Object Debug Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_object_debug.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                if (ModCompatibility.ObjectModifiersInstalled)
                {
                    // Integer Variable
                    {
                        var element = new Document.Element("<b>Integer Variable [MODDED]</b>\nEvery object has a whole number stored that ObjectModifiers can use.", Document.Element.Type.Text);
                        documentation.elements.Add(element);
                    }

                    // Modifiers
                    {
                        var element = new Document.Element("<b>Modifiers [MODDED]</b>\nModifiers come from the ObjectModifiers mod and are made up of two different types: Triggers and Actions. " +
                            "Triggers check if a specified thing is happening and Actions do things depending on if any triggers are active or there aren't any. A detailed description of every modifier " +
                            "can be found in the Modifiers documentation. [WIP]", Document.Element.Type.Text);
                        documentation.elements.Add(element);
                    }
                    
                    // Object Modifiers Image
                    {
                        var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_object_modifiers_edit.png", Document.Element.Type.Image);
                        documentation.elements.Add(element);
                    }
                }

                var htt = gameObject.AddComponent<HoverTooltip>();

                var levelTip = new HoverTooltip.Tooltip();

                levelTip.desc = documentation.Name;
                levelTip.hint = documentation.Description;
                htt.tooltipLangauges.Add(levelTip);

                var text = gameObject.transform.GetChild(0).GetComponent<Text>();

                text.text = documentation.Name;

                documentations.Add(documentation);
            }

            // Beatmap Object Keyframes
            //{
            //    var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(objectSearch.transform.Find("mask/content"), "Document");
            //    var documentation = new Document(gameObject, "Beatmap Object Keyframes", "The things that animate objects.");

            //    // Intro
            //    {
            //        var element = new Document.Element("The keyframes in the Beatmap Objects' keyframe timeline allow animating several aspects of a Beatmap Objects' visual.", Document.Element.Type.Text);
            //        documentation.elements.Add(element);
            //    }

            //    // None Image
            //    {
            //        var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_pos_none.png", Document.Element.Type.Image);
            //        documentation.elements.Add(element);
            //    }
                
            //    // Normal Image
            //    {
            //        var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_pos_normal.png", Document.Element.Type.Image);
            //        documentation.elements.Add(element);
            //    }
                
            //    // Toggle Image
            //    {
            //        var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_pos_toggle.png", Document.Element.Type.Image);
            //        documentation.elements.Add(element);
            //    }
                
            //    // Scale Image
            //    {
            //        var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_pos_scale.png", Document.Element.Type.Image);
            //        documentation.elements.Add(element);
            //    }
                
            //    // Static Homing Image
            //    {
            //        var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_pos_static_homing.png", Document.Element.Type.Image);
            //        documentation.elements.Add(element);
            //    }
                
            //    // Dynamic Homing Image
            //    {
            //        var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_pos_dynamic_homing.png", Document.Element.Type.Image);
            //        documentation.elements.Add(element);
            //    }
                
            //    // None Image
            //    {
            //        var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_sca_none.png", Document.Element.Type.Image);
            //        documentation.elements.Add(element);
            //    }
                
            //    // Normal Image
            //    {
            //        var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_sca_normal.png", Document.Element.Type.Image);
            //        documentation.elements.Add(element);
            //    }
                
            //    // Toggle Image
            //    {
            //        var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_sca_toggle.png", Document.Element.Type.Image);
            //        documentation.elements.Add(element);
            //    }
                
            //    // Scale Image
            //    {
            //        var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_sca_scale.png", Document.Element.Type.Image);
            //        documentation.elements.Add(element);
            //    }

            //    // None Image
            //    {
            //        var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_rot_none.png", Document.Element.Type.Image);
            //        documentation.elements.Add(element);
            //    }

            //    // Normal Image
            //    {
            //        var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_rot_normal.png", Document.Element.Type.Image);
            //        documentation.elements.Add(element);
            //    }

            //    // Toggle Image
            //    {
            //        var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_rot_toggle.png", Document.Element.Type.Image);
            //        documentation.elements.Add(element);
            //    }

            //    // Static Homing Image
            //    {
            //        var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_rot_static_homing.png", Document.Element.Type.Image);
            //        documentation.elements.Add(element);
            //    }

            //    // Dynamic Homing Image
            //    {
            //        var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_rot_dynamic_homing.png", Document.Element.Type.Image);
            //        documentation.elements.Add(element);
            //    }

            //    // None Image
            //    {
            //        var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_col_none.png", Document.Element.Type.Image);
            //        documentation.elements.Add(element);
            //    }

            //    // Dynamic Homing Image
            //    {
            //        var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_col_dynamic_homing.png", Document.Element.Type.Image);
            //        documentation.elements.Add(element);
            //    }

            //    var htt = gameObject.AddComponent<HoverTooltip>();

            //    var levelTip = new HoverTooltip.Tooltip();

            //    levelTip.desc = documentation.Name;
            //    levelTip.hint = documentation.Description;
            //    htt.tooltipLangauges.Add(levelTip);

            //    var text = gameObject.transform.GetChild(0).GetComponent<Text>();

            //    text.text = documentation.Name;

            //    documentations.Add(documentation);
            //}

            // Prefabs
            {
                var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(objectSearch.transform.Find("mask/content"), "Document");
                var documentation = new Document(gameObject, "Prefabs", "A package of objects that can be transfered to another level. They can also be added to the level as a Prefab Object.");

                // Intro
                {
                    var element = new Document.Element("Prefabs are collections of objects grouped together for easy transfering from level to level.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Name
                {
                    var element = new Document.Element("<b>Name [VANILLA]</b>\nThe name of the Prefab. External prefabs gets saved with this as its file name, but all lowercase and " +
                        "spaces replaced with underscores.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Name Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_pc_name.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Offset
                {
                    var element = new Document.Element("<b>Offset [VANILLA]</b>\nThe delay set to every Prefab Objects' spawned objects related to this Prefab.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Offset Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_pc_offset.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Type
                {
                    var element = new Document.Element("<b>Type [PATCHED]</b>\nThe group name and color of the Prefab. Good for color coding what a Prefab does at a glance.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Type Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_pc_type.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Description
                {
                    var element = new Document.Element("<b>Description [MODDED]</b>\nA good way to tell you and others what the Prefab does or contains in great detail.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Description Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_pc_description.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Seletion List
                {
                    var element = new Document.Element("<b>Seletion List [PATCHED]</b>\nShows every object, you can toggle the selection on any of them to add them to the prefab. All selected " +
                        "objects will be copied into the Prefab. This is patched because the UI and the code for it already existed in Legacy, it was just unused.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Seletion List Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_pc_search.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Create
                {
                    var element = new Document.Element("<b>Create [MODDED]</b>\nApplies all data and copies all selected objects to a new Prefab.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Create Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_pc_create.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                var htt = gameObject.AddComponent<HoverTooltip>();

                var levelTip = new HoverTooltip.Tooltip();

                levelTip.desc = documentation.Name;
                levelTip.hint = documentation.Description;
                htt.tooltipLangauges.Add(levelTip);

                var text = gameObject.transform.GetChild(0).GetComponent<Text>();

                text.text = documentation.Name;

                documentations.Add(documentation);
            }
            
            // Prefab Objects
            {
                var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(objectSearch.transform.Find("mask/content"), "Document");
                var documentation = new Document(gameObject, "Prefab Objects", "Individual instances of prefabs.");

                // Intro
                {
                    var element = new Document.Element("Prefab Objects are a copied version of the original prefab, placed into the level. They take all the objects stored in the original prefab " +
                        "and add them to the level, meaning you can have multiple copies of the same group of objects. Editing the objects of the prefab by expanding it applies all changes to " +
                        "the prefab, updating every Prefab Object (once collapsed back into a Prefab Object).", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Expand
                {
                    var element = new Document.Element("<b>Expand [VANILLA]</b>\nExpands all the objects contained within the original prefab into the level and deletes the Prefab Object.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Expand Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_object_expand.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Layer
                {
                    var element = new Document.Element("<b>Layer [PATCHED]</b>\nWhat Editor Layer the Prefab Object displays on. Can go from 1 to 2147483646. In unmodded Legacy its 1 to 5.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Layer Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_object_layer.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Time of Death
                {
                    var element = new Document.Element("<b>Time of Death [MODDED]</b>\nTime of Death allows every object spawned from the Prefab Object still alive at a certain point to despawn." +
                        "\nRegular - Just how the game handles Prefab Objects kill time normally." +
                        "\nStart Offset - Kill time is offset plus the Prefab Object start time." +
                        "\nSong Time - Kill time is song time, so no matter where you change the start time to the kill time remains the same.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Time of Death Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_object_tod.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Locked
                {
                    var element = new Document.Element("<b>Locked [PATCHED]</b>\nIf on, prevents Prefab Objects' start time from being changed. It's patched because unmodded PA doesn't " +
                        "have the toggle UI for this, however you can still use it in unmodded PA via hitting Ctrl + L.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }
                
                // Collapse
                {
                    var element = new Document.Element("<b>Collapse [PATCHED]</b>\nIf on, collapses the Prefab Objects' timeline object. This is patched because it literally doesn't " +
                        "work in unmodded PA.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }
                
                // Start Time
                {
                    var element = new Document.Element("<b>Start Time [VANILLA]</b>\nWhere the objects spawned from the Prefab Object start.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Time Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_object_time.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Position Offset
                {
                    var element = new Document.Element("<b>Position Offset [PATCHED]</b>\nEvery objects' top-most-parent has its position set to this offset. Unmodded PA technically has this " +
                        "feature, but it's not editable in the editor.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Position Offset Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_object_pos_offset.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Scale Offset
                {
                    var element = new Document.Element("<b>Scale Offset [PATCHED]</b>\nEvery objects' top-most-parent has its scale set to this offset. Unmodded PA technically has this " +
                        "feature, but it's not editable in the editor.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Scale Offset Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_object_sca_offset.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Rotation Offset
                {
                    var element = new Document.Element("<b>Rotation Offset [PATCHED]</b>\nEvery objects' top-most-parent has its rotation set to this offset. Unmodded PA technically has this " +
                        "feature, but it's not editable in the editor.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Rotation Offset Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_object_rot_offset.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Repeat
                {
                    var element = new Document.Element("<b>Repeat [MODDED]</b>\nWhen spawning the objects from the Prefab Object, every object gets repeated a set amount of times" +
                        "with their start offset added onto each time they repeat depending on the Repeat Offset Time set. The data for Repeat Count and Repeat Offset Time " +
                        "already existed in unmodded PA, it just went completely unused.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Repeat Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_object_repeat.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Speed
                {
                    var element = new Document.Element("<b>Speed [MODDED]</b>\nHow fast each object spawned from the Prefab Object spawns and is animated.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Speed Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_object_speed.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Lead Time / Offset
                {
                    var element = new Document.Element("<b>Lead Time / Offset [VANILLA]</b>\nEvery Prefab Object starts at an added offset from the Offset amount. I have no idea why " +
                        "it's called Lead Time here even though its Offset everywhere else.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Lead Time / Offset Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_lead.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Name
                {
                    var element = new Document.Element("<b>Name [MODDED]</b>\nChanges the name of the original Prefab related to the Prefab Object. This is modded because you couldn't " +
                        "change this in the Prefab Object editor.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Name Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_name.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Type
                {
                    var element = new Document.Element("<b>Type [MODDED]</b>\nChanges the Type of the original Prefab related to the Prefab Object. This is modded because you couldn't " +
                        "change this in the Prefab Object editor. (You can scroll-wheel over the input field to change the type easily)", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Type Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_type.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Save
                {
                    var element = new Document.Element("<b>Save [MODDED]</b>\nSaves all changes made to the original Prefab to any External Prefab with a matching name.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Save Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_save.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Count
                {
                    var element = new Document.Element("<b>Count [MODDED]</b>\nTells how many objects are in the original Prefab and how many Prefab Objects there are in the timeline " +
                        "for the Prefab. The Prefab Object Count goes unused for now...", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Count Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_counts.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                var htt = gameObject.AddComponent<HoverTooltip>();

                var levelTip = new HoverTooltip.Tooltip();

                levelTip.desc = documentation.Name;
                levelTip.hint = documentation.Description;
                htt.tooltipLangauges.Add(levelTip);

                var text = gameObject.transform.GetChild(0).GetComponent<Text>();

                text.text = documentation.Name;

                documentations.Add(documentation);
            }

            // Background Object
            //{
            //    var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(objectSearch.transform.Find("mask/content"), "Document");
            //    var documentation = new Document(gameObject, "Background Object", "Make classic 3D style backgrounds.");

            //    // Intro
            //    {
            //        var element = new Document.Element("Background Object intro.", Document.Element.Type.Text);
            //        documentation.elements.Add(element);
            //    }

            //    var htt = gameObject.AddComponent<HoverTooltip>();

            //    var levelTip = new HoverTooltip.Tooltip();

            //    levelTip.desc = documentation.Name;
            //    levelTip.hint = documentation.Description;
            //    htt.tooltipLangauges.Add(levelTip);

            //    var text = gameObject.transform.GetChild(0).GetComponent<Text>();

            //    text.text = documentation.Name;

            //    documentations.Add(documentation);
            //}

            // Events
            //{
            //    var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(objectSearch.transform.Find("mask/content"), "Document");
            //    var documentation = new Document(gameObject, "Events", "Effects to make your level pretty.");

            //    // Intro
            //    {
            //        var element = new Document.Element("Events intro.", Document.Element.Type.Text);
            //        documentation.elements.Add(element);
            //    }

            //    var htt = gameObject.AddComponent<HoverTooltip>();

            //    var levelTip = new HoverTooltip.Tooltip();

            //    levelTip.desc = documentation.Name;
            //    levelTip.hint = documentation.Description;
            //    htt.tooltipLangauges.Add(levelTip);

            //    var text = gameObject.transform.GetChild(0).GetComponent<Text>();

            //    text.text = documentation.Name;

            //    documentations.Add(documentation);
            //}
            
            // Text Objects
            {
                var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(objectSearch.transform.Find("mask/content"), "Document");
                var documentation = new Document(gameObject, "Text Objects", "Flavor your levels with text!");

                // Intro
                {
                    var element = new Document.Element("Text Objects can be used in extensive ways, from conveying character dialogue to decoration. This document is for showcasing usable " +
                        "fonts and formats Text Objects can use. Also do note to ignore the spaces in the formattings as the UI text will just make the text like <b>this</b>.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }
                
                // FORMATTING
                {
                    var element = new Document.Element("<b>- FORMATTING -</b>", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Bold
                {
                    var element = new Document.Element("<b>[VANILLA]</b> < b> - For making text <b>BOLD</b>. Use </ b> to clear.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }
                
                // Italic
                {
                    var element = new Document.Element("<b>[VANILLA]</b> < i> - For making text <i>italic</i>. Use </ i> to clear.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // FONTS
                {
                    var element = new Document.Element("<b>- FONTS -</b>", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // FONTS IMAGE
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_fonts.png", Document.Element.Type.Image);
                    element.Function = delegate ()
                    {
                        RTFile.OpenInFileBrowser.Open(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/Documentation");
                    };
                    documentation.elements.Add(element);
                }

                // Info
                {
                    var element = new Document.Element("To use a font, do <font=Font Name>. To clear, do </font>. Click on one of the fonts below to copy the <font=Font Name> to your clipboard. " +
                        "Click on the image above to open the folder to the documentation assets folder where a higher resolution screenshot is located.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Adam Warren Pro Bold
                {
                    var element = new Document.Element("<b>[MODDED]</b> Adam Warren Pro Bold - A comic style font.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard("<font=Adam Warren Pro Bold>");
                    };
                    documentation.elements.Add(element);
                }

                // Adam Warren Pro BoldItalic
                {
                    var element = new Document.Element("<b>[MODDED]</b> Adam Warren Pro BoldItalic - A comic style font.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard("<font=Adam Warren Pro BoldItalic>");
                    };
                    documentation.elements.Add(element);
                }

                // Adam Warren Pro
                {
                    var element = new Document.Element("<b>[MODDED]</b> Adam Warren Pro - A comic style font.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard("<font=Adam Warren Pro>");
                    };
                    documentation.elements.Add(element);
                }
                
                // Arrhythmia
                {
                    var element = new Document.Element("<b>[MODDED]</b> Arrhythmia - The font from the earliest builds of Project Arrhythmia.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard("<font=Arrhythmia>");
                    };
                    documentation.elements.Add(element);
                }

                // BadaBoom BB
                {
                    var element = new Document.Element("<b>[MODDED]</b> BadaBoom BB - A comic style font.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard("<font=BadaBoom BB>");
                    };
                    documentation.elements.Add(element);
                }

                // Matoran Language 1
                {
                    var element = new Document.Element("<b>[MODDED]</b> Matoran Language 1 - The language used by the Matoran in the BIONICLE series.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard("<font=Matoran Language 1>");
                    };
                    documentation.elements.Add(element);
                }
                
                // Matoran Language 2
                {
                    var element = new Document.Element("<b>[MODDED]</b> Matoran Language 2 - The language used by the Matoran in the BIONICLE series.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard("<font=Matoran Language 2>");
                    };
                    documentation.elements.Add(element);
                }

                // Determination Mono
                {
                    var element = new Document.Element("<b>[MODDED]</b> Determination Mono - The font UNDERTALE/deltarune uses for its interfaces.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard("<font=Determination Mono>");
                    };
                    documentation.elements.Add(element);
                }

                // determination sans
                {
                    var element = new Document.Element("<b>[MODDED]</b> determination sans - sans undertale.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard("<font=determination sans>");
                    };
                    documentation.elements.Add(element);
                }

                // Determination Wingdings
                {
                    string font = "Determination Wingdings";
                    var element = new Document.Element($"<b>[MODDED]</b> {font} - Beware the man who speaks in hands.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font={font}>");
                    };
                    documentation.elements.Add(element);
                }

                // Flow Circular
                {
                    var element = new Document.Element("<b>[MODDED]</b> Flow Circular - A fun line font suggested by ManIsLiS.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard("<font=Flow Circular>");
                    };
                    documentation.elements.Add(element);
                }

                // Fredoka One
                {
                    var element = new Document.Element("<b>[MODDED]</b> Fredoka One - The font from the Vitamin Games website.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard("<font=Fredoka One>");
                    };
                    documentation.elements.Add(element);
                }

                // Ancient Autobot
                {
                    var element = new Document.Element("<b>[MODDED]</b> Ancient Autobot - The launguage used by ancient Autobots in the original Transformers cartoon.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard("<font=Ancient Autobot>");
                    };
                    documentation.elements.Add(element);
                }

                // Hachicro
                {
                    var element = new Document.Element("<b>[MODDED]</b> Hachicro - The font used by UNDERTALE's hit text.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard("<font=Hachicro>");
                    };
                    documentation.elements.Add(element);
                }

                // Inconsolata Variable
                {
                    string font = "Inconsolata Variable";
                    var element = new Document.Element($"<b>[MODDED]</b> {font} - The default PA font.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font={font}>");
                    };
                    documentation.elements.Add(element);
                }

                // LiberationSans SDF
                {
                    string font = "LiberationSans SDF";
                    var element = new Document.Element($"<b>[VANILLA]</b> {font} - An extra font unmodded Legacy has.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font={font}>");
                    };
                    documentation.elements.Add(element);
                }

                // Komika Hand
                {
                    string font = "Komika Hand";
                    var element = new Document.Element($"<b>[MODDED]</b> {font} - A comic style font.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font={font}>");
                    };
                    documentation.elements.Add(element);
                }

                // Komika Hand Bold
                {
                    string font = "Komika Hand Bold";
                    var element = new Document.Element($"<b>[MODDED]</b> {font} - A comic style font.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font={font}>");
                    };
                    documentation.elements.Add(element);
                }

                // Komika Slick
                {
                    string font = "Komika Slick";
                    var element = new Document.Element($"<b>[MODDED]</b> {font} - A comic style font.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font={font}>");
                    };
                    documentation.elements.Add(element);
                }

                // Komika Slim
                {
                    string font = "Komika Slim";
                    var element = new Document.Element($"<b>[MODDED]</b> {font} - A comic style font.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font={font}>");
                    };
                    documentation.elements.Add(element);
                }

                // Komika Hand BoldItalic
                {
                    string font = "Komika Hand BoldItalic";
                    var element = new Document.Element($"<b>[MODDED]</b> {font} - A comic style font.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font={font}>");
                    };
                    documentation.elements.Add(element);
                }

                // Komika Hand Italic
                {
                    string font = "Komika Hand Italic";
                    var element = new Document.Element($"<b>[MODDED]</b> {font} - A comic style font.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font={font}>");
                    };
                    documentation.elements.Add(element);
                }

                // Komika Jam
                {
                    string font = "Komika Jam";
                    var element = new Document.Element($"<b>[MODDED]</b> {font} - A comic style font.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font={font}>");
                    };
                    documentation.elements.Add(element);
                }

                // Komika Jam Italic
                {
                    string font = "Komika Jam Italic";
                    var element = new Document.Element($"<b>[MODDED]</b> {font} - A comic style font.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font={font}>");
                    };
                    documentation.elements.Add(element);
                }

                // Komika Slick Italic
                {
                    string font = "Komika Slick Italic";
                    var element = new Document.Element($"<b>[MODDED]</b> {font} - A comic style font.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font={font}>");
                    };
                    documentation.elements.Add(element);
                }

                // Komika Slim Italic
                {
                    string font = "Komika Slim Italic";
                    var element = new Document.Element($"<b>[MODDED]</b> {font} - A comic style font.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font={font}>");
                    };
                    documentation.elements.Add(element);
                }

                // Minecraft Text Bold
                {
                    string font = "Minecraft Text Bold";
                    var element = new Document.Element($"<b>[MODDED]</b> {font} - The font used for the text UI in Minecraft.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font={font}>");
                    };
                    documentation.elements.Add(element);
                }

                // Minecraft Text BoldItalic
                {
                    string font = "Minecraft Text BoldItalic";
                    var element = new Document.Element($"<b>[MODDED]</b> {font} - The font used for the text UI in Minecraft.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font={font}>");
                    };
                    documentation.elements.Add(element);
                }

                // Minecraft Text Italic
                {
                    string font = "Minecraft Text Italic";
                    var element = new Document.Element($"<b>[MODDED]</b> {font} - The font used for the text UI in Minecraft.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font={font}>");
                    };
                    documentation.elements.Add(element);
                }

                // Minecraft Text
                {
                    string font = "Minecraft Text";
                    var element = new Document.Element($"<b>[MODDED]</b> {font} - The font used for the text UI in Minecraft.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font={font}>");
                    };
                    documentation.elements.Add(element);
                }

                // Minecraftory
                {
                    string font = "Minecraftory";
                    var element = new Document.Element($"<b>[MODDED]</b> {font} - Geometry Dash font mainly used in Geometry Dash SubZero.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font={font}>");
                    };
                    documentation.elements.Add(element);
                }

                // Monster Friend Back
                {
                    string font = "Monster Friend Back";
                    var element = new Document.Element($"<b>[MODDED]</b> {font} - A font based on UNDERTALE's title.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font={font}>");
                    };
                    documentation.elements.Add(element);
                }
                
                // Monster Friend Fore
                {
                    string font = "Monster Friend Fore";
                    var element = new Document.Element($"<b>[MODDED]</b> {font} - A font based on UNDERTALE's title.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font={font}>");
                    };
                    documentation.elements.Add(element);
                }

                // About Friend
                {
                    string font = "About Friend";
                    var element = new Document.Element($"<b>[MODDED]</b> {font} - A font suggested by Ama.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font={font}>");
                    };
                    documentation.elements.Add(element);
                }
                
                // Oxygene
                {
                    var element = new Document.Element("<b>[MODDED]</b> Oxygene - The font from the title of Geometry Dash.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard("<font=Oxygene>");
                    };
                    documentation.elements.Add(element);
                }

                // Piraka Theory
                {
                    string font = "Piraka Theory";
                    var element = new Document.Element($"<b>[MODDED]</b> {font} - The language used by the Piraka in the BIONICLE series.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font={font}>");
                    };
                    documentation.elements.Add(element);
                }
                
                // Piraka
                {
                    string font = "Piraka";
                    var element = new Document.Element($"<b>[MODDED]</b> {font} - The language used by the Piraka in the BIONICLE series.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font={font}>");
                    };
                    documentation.elements.Add(element);
                }

                // Pusab
                {
                    var element = new Document.Element("<b>[MODDED]</b> Pusab - The font from the hit game Geometry Dash. And yes, it is the right one.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard("<font=Pusab>");
                    };
                    documentation.elements.Add(element);
                }

                // Rahkshi
                {
                    string font = "Rahkshi";
                    var element = new Document.Element($"<b>[MODDED]</b> {font} - The font used for promoting the Rahkshi sets in the BIONICLE series.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font={font}>");
                    };
                    documentation.elements.Add(element);
                }

                // Revue
                {
                    string font = "Revue";
                    var element = new Document.Element($"<b>[MODDED]</b> {font} - The font used early 2000s Transformers titles.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font={font}>");
                    };
                    documentation.elements.Add(element);
                }

                // Transdings
                {
                    string font = "Transdings";
                    var element = new Document.Element($"<b>[MODDED]</b> {font} - A font that contains a ton of Transformer insignias / logos. Below is an image featuring each letter " +
                        $"of the alphabet.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font={font}>");
                    };
                    documentation.elements.Add(element);
                }

                // Transdings IMAGE
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_tf.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Transformers Movie
                {
                    string font = "Transformers Movie";
                    var element = new Document.Element($"<b>[MODDED]</b> {font} - A font based on the Transformers movies title font.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font={font}>");
                    };
                    documentation.elements.Add(element);
                }

                // Nexa Book
                {
                    string font = "Nexa Book";
                    var element = new Document.Element($"<b>[MODDED]</b> {font} - A font suggested by CubeCube.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font={font}>");
                    };
                    documentation.elements.Add(element);
                }

                // Nexa Bold
                {
                    string font = "Nexa Bold";
                    var element = new Document.Element($"<b>[MODDED]</b> {font} - A font suggested by CubeCube.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font={font}>");
                    };
                    documentation.elements.Add(element);
                }

                // Angsana
                {
                    string font = "Angsana";
                    var element = new Document.Element($"<b>[MODDED]</b> {font} - A font suggested by KarasuTori. Supports non-English languages like Thai.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font={font}>");
                    };
                    documentation.elements.Add(element);
                }

                // Angsana Bold
                {
                    string font = "Angsana Bold";
                    var element = new Document.Element($"<b>[MODDED]</b> {font} - A font suggested by KarasuTori. Supports non-English languages like Thai.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font={font}>");
                    };
                    documentation.elements.Add(element);
                }

                // Angsana Italic
                {
                    string font = "Angsana Italic";
                    var element = new Document.Element($"<b>[MODDED]</b> {font} - A font suggested by KarasuTori. Supports non-English languages like Thai.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font={font}>");
                    };
                    documentation.elements.Add(element);
                }

                // Angsana Bold Italic
                {
                    string font = "Angsana Bold Italic";
                    var element = new Document.Element($"<b>[MODDED]</b> {font} - A font suggested by KarasuTori. Supports non-English languages like Thai.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font={font}>");
                    };
                    documentation.elements.Add(element);
                }

                // VAG Rounded
                {
                    string font = "VAG Rounded";
                    var element = new Document.Element($"<b>[MODDED]</b> {font} - A font suggested by KarasuTori. Supports non-English languages like Russian.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font={font}>");
                    };
                    documentation.elements.Add(element);
                }

                // Comic Sans
                {
                    string font = "Comic Sans";
                    var element = new Document.Element($"<b>[MODDED]</b> {font} - You know the font.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font={font}>");
                    };
                    documentation.elements.Add(element);
                }
                
                // Comic Sans Bold
                {
                    string font = "Comic Sans Bold";
                    var element = new Document.Element($"<b>[MODDED]</b> {font} - You know the font.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font={font}>");
                    };
                    documentation.elements.Add(element);
                }

                // Comic Sans Hairline
                {
                    string font = "Comic Sans Hairline";
                    var element = new Document.Element($"<b>[MODDED]</b> {font} - You know the font.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font={font}>");
                    };
                    documentation.elements.Add(element);
                }
                
                // Comic Sans Light
                {
                    string font = "Comic Sans Light";
                    var element = new Document.Element($"<b>[MODDED]</b> {font} - You know the font.", Document.Element.Type.Text);
                    element.Function = delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<font={font}>");
                    };
                    documentation.elements.Add(element);
                }

                var htt = gameObject.AddComponent<HoverTooltip>();

                var levelTip = new HoverTooltip.Tooltip();

                levelTip.desc = documentation.Name;
                levelTip.hint = documentation.Description;
                htt.tooltipLangauges.Add(levelTip);

                var text = gameObject.transform.GetChild(0).GetComponent<Text>();

                text.text = documentation.Name;

                documentations.Add(documentation);
            }

            // Markers
            {
                var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(objectSearch.transform.Find("mask/content"), "Document");
                var documentation = new Document(gameObject, "Markers", "Organize and remember details about a level.");

                // Intro
                {
                    var element = new Document.Element("Markers can organize certain parts of your level or help with aligning objects to a specific time.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Marker Timeline
                {
                    var element = new Document.Element("In the image below is two types of markers. The blue marker is the Audio Marker and the marker with a circle on the top is just a Marker. " +
                        "Left clicking on the Marker's circle knob moves the Audio Marker to the regular Marker. Right clicking the Marker's circle knob deletes it.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Marker Timeline Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_marker_timeline.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Marker Name
                {
                    var element = new Document.Element("<b>Name [VANILLA]</b>\nThe name of the Marker. This renders next to the Marker's circle knob in the timeline.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Marker Name Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_marker_name.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Marker Time
                {
                    var element = new Document.Element("<b>Time [VANILLA]</b>\nThe time the Marker renders at in the timeline.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Marker Time Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_marker_time.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Marker Description
                {
                    var element = new Document.Element("<b>Description [PATCHED]</b>\nDescription helps you remember details about specific parts of a song or even stuff about the level you're " +
                        "editing. Typing setLayer(1) will set the editor layer to 1 when the Marker is selected. You can also have it be setLayer(events), setLayer(objects), setLayer(toggle), which " +
                        "sets the layer type to those respective types (toggle switches between Events and Objects layer types). Fun fact, the title for description in the UI in unmodded Legacy " +
                        "said \"Name\" lol.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Marker Description Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_marker_description.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Marker Colors
                {
                    var element = new Document.Element("<b>Colors [PATCHED]</b>\nWhat color the marker displays as. You can customize the colors in the Settings window.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Marker Colors Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_marker_colors.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Marker Index
                {
                    var element = new Document.Element("<b>Index [MODDED]</b>\nThe number of the Marker in the list.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Marker Index Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_marker_index.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Marker Delete
                {
                    var element = new Document.Element("On the right-hand-side of the Marker Editor window is a list of markers. At the top is a Search field and a Delete Markers button. " +
                        "Delete Markers clears every marker in the level and closes the Marker Editor.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Marker Delete Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_marker_delete.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                var htt = gameObject.AddComponent<HoverTooltip>();

                var levelTip = new HoverTooltip.Tooltip();

                levelTip.desc = documentation.Name;
                levelTip.hint = documentation.Description;
                htt.tooltipLangauges.Add(levelTip);

                var text = gameObject.transform.GetChild(0).GetComponent<Text>();

                text.text = documentation.Name;

                documentations.Add(documentation);
            }

            // Title Bar
            {
                var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(objectSearch.transform.Find("mask/content"), "Document");
                var documentation = new Document(gameObject, "Title Bar", "The thing at the top with dropdowns.");

                // Intro
                {
                    var element = new Document.Element("Title Bar has the main functions for loading, saving and editing.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Title Bar Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_td.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }
                
                // File
                {
                    var element = new Document.Element("<b>File [PATCHED]</b>" +
                        "\nPowerful functions related to the application or files." +
                        "\n<b>[VANILLA]</b> New Level - Creates a new level." +
                        "\n<b>[VANILLA]</b> Open Level - Opens the level list popup, where you can search and select a level to load." +
                        "\n<b>[VANILLA]</b> Open Level Folder - Opens the current loaded level's folder in your local file explorer." +
                        "\n<b>[MODDED]</b> Open Level Browser - Opens a built-in browser to open a level from anywhere on your computer." +
                        "\n<b>[MODDED]</b> Level Combiner - Combines multiple levels together." +
                        "\n<b>[VANILLA]</b> Save - Saves the current level." +
                        "\n<b>[PATCHED]</b> Save As - Saves a copy of the current level." +
                        "\n<b>[VANILLA]</b> Toggle Play Mode - Opens preview mode." +
                        "\n<b>[MODDED]</b> Switch to Arcade Mode - Switches to the handling of level loading in Arcade." +
                        "\n<b>[MODDED]</b> Quit to Arcade - Opens the Input Select scene just before loading arcade levels." +
                        "\n<b>[VANILLA]</b> Quit to Main Menu - Exits to the main menu." +
                        "\n<b>[VANILLA]</b> Quit Game - Quits the game.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // File Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_td_file.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Edit
                {
                    var element = new Document.Element("<b>Edit [PATCHED]</b>" +
                        "\nHow far can you edit in a modded editor?" +
                        "\n<b>[PATCHED]</b> Undo - Undoes the most recent action. Still heavily WIP. (sorry)" +
                        "\n<b>[PATCHED]</b> Redo - Same as above but goes back to the recent action when undone." +
                        "\n<b>[MODDED]</b> Search Objects - Search for specific objects by name or index. Hold Left Control to take yourself to the object in the timeline." +
                        "\n<b>[MODDED]</b> Preferences - Modify editor specific mod configs directly in the editor. Also known as Editor Properties." +
                        "\n<b>[MODDED]</b> Player Editor - Only shows if you have CreativePlayers installed. Opens the Player Editor." +
                        "\n<b>[MODDED]</b> View Keybinds - Customize the keybinds of the editor in any way you want.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Edit Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_td_edit.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // View
                {
                    var element = new Document.Element("<b>View [PATCHED]</b>" +
                        "\nView specific things." +
                        "\n<b>[MODDED]</b> Get Example - Only shows if you have ExampleCompanion installed. It summons Example to the scene." +
                        "\n<b>[VANILLA]</b> Show Help - Toggles the Info box.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // View Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_td_view.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Steam
                {
                    var element = new Document.Element("<b>Steam [VANILLA]</b>" +
                        "\nView Steam related things... even though modded PA doesn't use Steam anymore lol" +
                        "\n<b>[VANILLA]</b> Open Workshop - Opens a link to the Steam workshop." +
                        "\n<b>[VANILLA]</b> Publish / Update Level - Opens the Metadata Editor / Level Uploader.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Steam Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_td_steam.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Help
                {
                    var element = new Document.Element("<b>Help [PATCHED]</b>" +
                        "\nGet some help." +
                        "\n<b>[MODDED]</b> Modder's Discord - Opens a link to the mod creator's Discord server." +
                        "\n<b>[MODDED]</b> Watch PA History - Since there are no <i>modded</i> guides yet, this just takes you to the System Error BTS PA History playlist." +
                        "\n<b>[MODDED]</b> Wiki / Documentation - In-editor documentation of everything the game has to offer. You're reading it right now!", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Help Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_td_help.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                var htt = gameObject.AddComponent<HoverTooltip>();

                var levelTip = new HoverTooltip.Tooltip();

                levelTip.desc = documentation.Name;
                levelTip.hint = documentation.Description;
                htt.tooltipLangauges.Add(levelTip);

                var text = gameObject.transform.GetChild(0).GetComponent<Text>();

                text.text = documentation.Name;

                documentations.Add(documentation);
            }
            
            // Timeline Bar
            {
                var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(objectSearch.transform.Find("mask/content"), "Document");
                var documentation = new Document(gameObject, "Timeline Bar", "Modify stuff like audio and editor layer.");

                // Intro
                {
                    var element = new Document.Element("The Timeline Bar is where you can see and edit general game and editor info.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Audio Time (Precise)
                {
                    var element = new Document.Element("<b>Audio Time (Precise) [MODDED]</b>\nText shows the precise audio time. This can be edited to set a specific time for the audio.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Audio Time (Formatted)
                {
                    var element = new Document.Element("<b>Audio Time (Formatted) [VANILLA]</b>\nText shows the audio time formatted like \"minutes.seconds.milliseconds\". Clicking this sets the " +
                        "audio time to 0.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Pause / Play
                {
                    var element = new Document.Element("<b>Pause / Play [VANILLA]</b>\nPressing this toggles if the song is playing or not.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Pitch
                {
                    var element = new Document.Element("<b>Pitch [PATCHED]</b>\nThe speed of the song. Clicking the buttons adjust the pitch by 0.1, depending on the direction the button is facing.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Audio Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_tb_audio.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Editor Layer
                {
                    var element = new Document.Element("<b>Editor Layer [PATCHED]</b>\nEditor Layer is what objects show in the timeline, depending on their own Editor Layer. " +
                        "It can go as high as 2147483646. In unmodded PA its limited from layers 1 to 5, though in PA Editor Alpha another layer was introduced.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Editor Layer Type
                {
                    var element = new Document.Element("<b>Editor Layer Type [MODDED]</b>\nWhether the timeline shows objects or event keyframes / checkpoints.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Layer Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_tb_layer.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Prefab
                {
                    var element = new Document.Element("<b>Prefab [VANILLA]</b>\nOpens the Prefab list popups (Internal & External).", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Object
                {
                    var element = new Document.Element("<b>Object [PATCHED]</b>\nOpens a popup featuring different object templates such as Decoration, Empty, etc. It's patched because " +
                        "Persistent was replaced with No Autokill.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }
                
                // Marker
                {
                    var element = new Document.Element("<b>Marker [VANILLA]</b>\nCreates a Marker.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // BG
                {
                    var element = new Document.Element("<b>BG [VANILLA]</b>\nOpens a popup to open the BG editor or create a new BG.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Create Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_tb_create.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Preview Mode
                {
                    var element = new Document.Element("<b>Preview Mode [VANILLA]</b>\nSwitches the game to Preview Mode.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Preview Mode Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_tb_preview_mode.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                var htt = gameObject.AddComponent<HoverTooltip>();

                var levelTip = new HoverTooltip.Tooltip();

                levelTip.desc = documentation.Name;
                levelTip.hint = documentation.Description;
                htt.tooltipLangauges.Add(levelTip);

                var text = gameObject.transform.GetChild(0).GetComponent<Text>();

                text.text = documentation.Name;

                documentations.Add(documentation);
            }

            // Keybinds
            //{
            //    var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(objectSearch.transform.Find("mask/content"), "Document");
            //    var documentation = new Document(gameObject, "Keybinds", "Perform specific actions when pressing set keys.");

            //    // Intro
            //    {
            //        var element = new Document.Element("Keybinds intro.", Document.Element.Type.Text);
            //        documentation.elements.Add(element);
            //    }

            //    // Keybinds List Image
            //    {
            //        var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_keybind_list.png", Document.Element.Type.Image);
            //        documentation.elements.Add(element);
            //    }

            //    // Keybinds Editor Image
            //    {
            //        var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_keybind_editor.png", Document.Element.Type.Image);
            //        documentation.elements.Add(element);
            //    }

            //    var htt = gameObject.AddComponent<HoverTooltip>();

            //    var levelTip = new HoverTooltip.Tooltip();

            //    levelTip.desc = documentation.Name;
            //    levelTip.hint = documentation.Description;
            //    htt.tooltipLangauges.Add(levelTip);

            //    var text = gameObject.transform.GetChild(0).GetComponent<Text>();

            //    text.text = documentation.Name;

            //    documentations.Add(documentation);
            //}

            // Editor Properties
            //{
            //    var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(objectSearch.transform.Find("mask/content"), "Document");
            //    var documentation = new Document(gameObject, "Editor Properties", "Configure the editor!");

            //    // Intro
            //    {
            //        var element = new Document.Element("Editor Properties intro.", Document.Element.Type.Text);
            //        documentation.elements.Add(element);
            //    }

            //    // Editor Properties Image
            //    {
            //        var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_editor_properties.png", Document.Element.Type.Image);
            //        documentation.elements.Add(element);
            //    }

            //    var htt = gameObject.AddComponent<HoverTooltip>();

            //    var levelTip = new HoverTooltip.Tooltip();

            //    levelTip.desc = documentation.Name;
            //    levelTip.hint = documentation.Description;
            //    htt.tooltipLangauges.Add(levelTip);

            //    var text = gameObject.transform.GetChild(0).GetComponent<Text>();

            //    text.text = documentation.Name;

            //    documentations.Add(documentation);
            //}

            // Misc
            {
                var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(objectSearch.transform.Find("mask/content"), "Document");
                var documentation = new Document(gameObject, "Misc", "The stuff that didn't fit in a document of its own.");

                // Editor Level Path
                {
                    var element = new Document.Element("<b>Editor Level Path [MODDED]</b>\nThe path within the Project Arrhythmia/beatmaps directory that is used for the editor level list.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }
                
                // Refresh
                {
                    var element = new Document.Element("<b>Refresh [MODDED]</b>\nRefreshes the editor level list.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Descending
                {
                    var element = new Document.Element("<b>Descending [MODDED]</b>\nIf the editor level list should be descending or ascending.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }
                
                // Order
                {
                    var element = new Document.Element("<b>Order [MODDED]</b>\nHow the editor level list should be ordered." +
                        "\nCover - Order by if the level has a cover or not." +
                        "\nArtist - Order by Artist Name." +
                        "\nCreator - Order by Creator Name." +
                        "\nFolder - Order by Folder Name." +
                        "\nTitle - Order by Song Title." +
                        "\nDifficulty - Order by (Easy, Normal, Hard, Expert, Expert+, Master, Animation)" +
                        "\nDate Edited - Order by last saved time, so recently edited levels appear at one side and older levels appear at the other.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Open Level Top Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_open_level_top.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                // Loading Autosaves
                {
                    var element = new Document.Element("<b>Loading Autosaves [MODDED]</b>\nHolding shift when you click on a level in the level list will open an Autosave popup instead of " +
                        "loading the level. This allows you to load any autosaved file so you don't need to go into the level folder and change one of the autosaves to the level.lsb.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // Loading Autosaves Image
                {
                    var element = new Document.Element("BepInEx/plugins/Assets/Documentation/doc_autosaves.png", Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                var htt = gameObject.AddComponent<HoverTooltip>();

                var levelTip = new HoverTooltip.Tooltip();

                levelTip.desc = documentation.Name;
                levelTip.hint = documentation.Description;
                htt.tooltipLangauges.Add(levelTip);

                var text = gameObject.transform.GetChild(0).GetComponent<Text>();

                text.text = documentation.Name;

                documentations.Add(documentation);
            }

            // Modifiers
            if (ModCompatibility.ObjectModifiersInstalled)
            {
                var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(objectSearch.transform.Find("mask/content"), "Document");
                var documentation = new Document(gameObject, "Object Modifiers", "Make your levels dynamic!");

                // Intro
                {
                    var element = new Document.Element("ObjectModifiers adds a trigger / action based system to Beatmap Objects called \"Modifiers\". " +
                        "Modifiers have two types: Triggers check if something is happening and if it is, it activates any Action type modifiers. If there are no Triggers, then the Action modifiers " +
                        "activates. This document is heavily WIP and will be added to over time.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }
                
                // setPitch
                {
                    var element = new Document.Element("<b>setPitch</b> - Modifies the speed of the game and the pitch of the audio. If you have EventsCore installed, it sets a multiplied offset from the " +
                        "audio keyframe's pitch value. However unlike that, setPitch can go into the negatives allowing for reversed audio.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }
                
                // addPitch
                {
                    var element = new Document.Element("<b>addPitch</b> - Does the same as above, except adds to the pitch offset.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }
                
                // setMusicTime
                {
                    var element = new Document.Element("<b>setMusicTime</b> - Sets the Audio Time to go to any point in the song, allowing for skipping specific sections of a song.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }
                
                // playSound
                {
                    var element = new Document.Element("<b>playSound</b> - Plays an external sound. The following details what each value in the modifier does." +
                        "\nPath - If global is on, path should be set to something within beatmaps/soundlibrary directory. If global is off, then the path should be set to something within the level " +
                        "folder that has level.lsb and metadata.lsb." +
                        "\nGlobal - Affects the above setting in the way described." +
                        "\nPitch - The speed of the sound played." +
                        "\nVolume - How loud the sound is." +
                        "\nLoop - If the sound should loop while the Modifier is active.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                // playSoundOnline
                {
                    var element = new Document.Element("<b>playSoundOnline</b> - Same as above except plays from a link. The global toggle does nothing here.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }
                
                // loadLevel
                {
                    var element = new Document.Element("<b>loadLevel</b> - Loads a level from the current level folder path.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }
                
                // loadLevelInternal
                {
                    var element = new Document.Element("<b>loadLevelInternal</b> - Same as above, except it always loads from the current levels own path.", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                var htt = gameObject.AddComponent<HoverTooltip>();

                var levelTip = new HoverTooltip.Tooltip();

                levelTip.desc = documentation.Name;
                levelTip.hint = documentation.Description;
                htt.tooltipLangauges.Add(levelTip);

                var text = gameObject.transform.GetChild(0).GetComponent<Text>();

                text.text = documentation.Name;

                documentations.Add(documentation);
            }

            if (RTHelpers.AprilFools)
            {
                var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(objectSearch.transform.Find("mask/content"), "Document");
                var documentation = new Document(gameObject, "April fools!", "fol.");

                // Intro
                {
                    var element = new Document.Element("oops, i spilled my images everywhere...", Document.Element.Type.Text);
                    documentation.elements.Add(element);
                }

                var dir = Directory.GetFiles(RTFile.ApplicationDirectory, "*.png", SearchOption.AllDirectories);

                for (int i = 0; i < UnityEngine.Random.Range(0, Mathf.Clamp(dir.Length, 0, 20)); i++)
                {
                    var element = new Document.Element(dir[UnityEngine.Random.Range(0, dir.Length)].Replace("\\", "/").Replace(RTFile.ApplicationDirectory, ""), Document.Element.Type.Image);
                    documentation.elements.Add(element);
                }

                var htt = gameObject.AddComponent<HoverTooltip>();

                var levelTip = new HoverTooltip.Tooltip();

                levelTip.desc = documentation.Name;
                levelTip.hint = documentation.Description;
                htt.tooltipLangauges.Add(levelTip);

                var text = gameObject.transform.GetChild(0).GetComponent<Text>();

                text.text = documentation.Name;

                documentations.Add(documentation);
            }

            {
                //var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(objectSearch.transform.Find("mask/content"), "Document");
                //var documentation = new Document(gameObject, "Creating a new level", "Description.");

                //var htt = gameObject.AddComponent<HoverTooltip>();

                //var levelTip = new HoverTooltip.Tooltip();

                //levelTip.desc = documentation.Name;
                //levelTip.hint = documentation.Description;
                //htt.tooltipLangauges.Add(levelTip);

                //var text = gameObject.transform.GetChild(0).GetComponent<Text>();

                //text.text = documentation.Name;

                //documentations.Add(documentation);
            }
        }

        public List<string> debugs = new List<string>();
        public string debugSearch;
        public void CreateDebug()
        {
            if (ModCompatibility.mods.ContainsKey("UnityExplorer"))
            {
                var inspector = AccessTools.TypeByName("UnityExplorer.InspectorManager");
                var uiManager = AccessTools.TypeByName("UnityExplorer.UI.UIManager");

                var objectSearch = EditorManager.inst.GetDialog("Parent Selector").Dialog.gameObject
                    .Duplicate(EditorManager.inst.GetDialog("Parent Selector").Dialog.GetParent(), "Debugger Popup");
                objectSearch.transform.localPosition = Vector3.zero;

                var objectSearchRT = (RectTransform)objectSearch.transform;
                objectSearchRT.sizeDelta = new Vector2(600f, 450f);
                var objectSearchPanel = (RectTransform)objectSearch.transform.Find("Panel");
                objectSearchPanel.sizeDelta = new Vector2(632f, 32f);
                objectSearchPanel.transform.Find("Text").GetComponent<Text>().text = "Debugger";
                ((RectTransform)objectSearch.transform.Find("search-box")).sizeDelta = new Vector2(600f, 32f);
                objectSearch.transform.Find("mask/content").GetComponent<GridLayoutGroup>().cellSize = new Vector2(600f, 32f);

                var x = objectSearchPanel.transform.Find("x").GetComponent<Button>();
                x.onClick.RemoveAllListeners();
                x.onClick.AddListener(delegate ()
                {
                    EditorManager.inst.HideDialog("Debugger Popup");
                });

                var searchBar = objectSearch.transform.Find("search-box/search").GetComponent<InputField>();
                searchBar.onValueChanged.ClearAll();
                searchBar.onValueChanged.AddListener(delegate (string _value)
                {
                    debugSearch = _value;
                    RefreshDebugger();
                });
                searchBar.transform.Find("Placeholder").GetComponent<Text>().text = "Search for function...";

                EditorHelper.AddEditorDropdown("Debugger", "", "View", SpriteManager.LoadSprite(RTFile.ApplicationDirectory + RTFunctions.FunctionsPlugin.BepInExAssetsPath + "debugger.png"), delegate ()
                {
                    EditorManager.inst.ShowDialog("Debugger Popup");
                    RefreshDocumentation();
                });

                EditorHelper.AddEditorPopup("Debugger Popup", objectSearch);

                // Inspect DataManager
                {
                    string name = "DataManager";
                    var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(objectSearch.transform.Find("mask/content"), "Function");
                    debugs.Add($"Inspect {name}");

                    var htt = gameObject.AddComponent<HoverTooltip>();

                    var levelTip = new HoverTooltip.Tooltip();

                    levelTip.desc = $"Inspect {name}";
                    levelTip.hint = "DataManager is a pretty important storage component of Project Arrhythmia.";
                    htt.tooltipLangauges.Add(levelTip);

                    var button = gameObject.GetComponent<Button>();
                    button.onClick.ClearAll();
                    button.onClick.AddListener(delegate ()
                    {
                        uiManager.GetProperty("ShowMenu").SetValue(uiManager, true);
                        inspector.GetMethod("Inspect", new[] { typeof(object), AccessTools.TypeByName("UnityExplorer.CacheObject.CacheObjectBase") })
                        .Invoke(inspector, new object[] { DataManager.inst, null });
                    });
                    gameObject.transform.GetChild(0).GetComponent<Text>().text = $"Inspect {name}";
                }

                // Inspect EditorManager
                {
                    string name = "EditorManager";
                    var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(objectSearch.transform.Find("mask/content"), "Function");
                    debugs.Add($"Inspect {name}");

                    var htt = gameObject.AddComponent<HoverTooltip>();

                    var levelTip = new HoverTooltip.Tooltip();

                    levelTip.desc = $"Inspect {name}";
                    levelTip.hint = "EditorManager is the component that handles general editor stuff.";
                    htt.tooltipLangauges.Add(levelTip);

                    var button = gameObject.GetComponent<Button>();
                    button.onClick.ClearAll();
                    button.onClick.AddListener(delegate ()
                    {
                        uiManager.GetProperty("ShowMenu").SetValue(uiManager, true);
                        inspector.GetMethod("Inspect", new[] { typeof(object), AccessTools.TypeByName("UnityExplorer.CacheObject.CacheObjectBase") })
                        .Invoke(inspector, new object[] { EditorManager.inst, null });
                    });
                    gameObject.transform.GetChild(0).GetComponent<Text>().text = $"Inspect {name}";
                }

                // Inspect RTEditor
                {
                    string name = "RTEditor";
                    var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(objectSearch.transform.Find("mask/content"), "Function");
                    debugs.Add($"Inspect {name}");

                    var htt = gameObject.AddComponent<HoverTooltip>();

                    var levelTip = new HoverTooltip.Tooltip();

                    levelTip.desc = $"Inspect {name}";
                    levelTip.hint = "RTEditor is the component that handles modded general editor stuff.";
                    htt.tooltipLangauges.Add(levelTip);

                    var button = gameObject.GetComponent<Button>();
                    button.onClick.ClearAll();
                    button.onClick.AddListener(delegate ()
                    {
                        uiManager.GetProperty("ShowMenu").SetValue(uiManager, true);
                        inspector.GetMethod("Inspect", new[] { typeof(object), AccessTools.TypeByName("UnityExplorer.CacheObject.CacheObjectBase") })
                        .Invoke(inspector, new object[] { inst, null });
                    });
                    gameObject.transform.GetChild(0).GetComponent<Text>().text = $"Inspect {name}";
                }

                // Inspect ObjEditor
                {
                    string name = "ObjEditor";
                    var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(objectSearch.transform.Find("mask/content"), "Function");
                    debugs.Add($"Inspect {name}");

                    var htt = gameObject.AddComponent<HoverTooltip>();

                    var levelTip = new HoverTooltip.Tooltip();

                    levelTip.desc = $"Inspect {name}";
                    levelTip.hint = "ObjEditor is the component that handles object editor stuff.";
                    htt.tooltipLangauges.Add(levelTip);

                    var button = gameObject.GetComponent<Button>();
                    button.onClick.ClearAll();
                    button.onClick.AddListener(delegate ()
                    {
                        uiManager.GetProperty("ShowMenu").SetValue(uiManager, true);
                        inspector.GetMethod("Inspect", new[] { typeof(object), AccessTools.TypeByName("UnityExplorer.CacheObject.CacheObjectBase") })
                        .Invoke(inspector, new object[] { ObjEditor.inst, null });
                    });
                    gameObject.transform.GetChild(0).GetComponent<Text>().text = $"Inspect {name}";
                }

                // Inspect ObjectEditor
                {
                    string name = "ObjectEditor";
                    var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(objectSearch.transform.Find("mask/content"), "Function");
                    debugs.Add($"Inspect {name}");

                    var htt = gameObject.AddComponent<HoverTooltip>();

                    var levelTip = new HoverTooltip.Tooltip();

                    levelTip.desc = $"Inspect {name}";
                    levelTip.hint = "ObjectEditor is the component that handles modded object editor stuff.";
                    htt.tooltipLangauges.Add(levelTip);

                    var button = gameObject.GetComponent<Button>();
                    button.onClick.ClearAll();
                    button.onClick.AddListener(delegate ()
                    {
                        uiManager.GetProperty("ShowMenu").SetValue(uiManager, true);
                        inspector.GetMethod("Inspect", new[] { typeof(object), AccessTools.TypeByName("UnityExplorer.CacheObject.CacheObjectBase") })
                        .Invoke(inspector, new object[] { ObjectEditor.inst, null });
                    });
                    gameObject.transform.GetChild(0).GetComponent<Text>().text = $"Inspect {name}";
                }
                
                // Inspect ObjectManager
                {
                    string name = "ObjectManager";
                    var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(objectSearch.transform.Find("mask/content"), "Function");
                    debugs.Add($"Inspect {name}");

                    var htt = gameObject.AddComponent<HoverTooltip>();

                    var levelTip = new HoverTooltip.Tooltip();

                    levelTip.desc = $"Inspect {name}";
                    levelTip.hint = "ObjectManager is the component that handles regular object stuff.";
                    htt.tooltipLangauges.Add(levelTip);

                    var button = gameObject.GetComponent<Button>();
                    button.onClick.ClearAll();
                    button.onClick.AddListener(delegate ()
                    {
                        uiManager.GetProperty("ShowMenu").SetValue(uiManager, true);
                        inspector.GetMethod("Inspect", new[] { typeof(object), AccessTools.TypeByName("UnityExplorer.CacheObject.CacheObjectBase") })
                        .Invoke(inspector, new object[] { ObjectManager.inst, null });
                    });
                    gameObject.transform.GetChild(0).GetComponent<Text>().text = $"Inspect {name}";
                }

                // Inspect GameManager
                {
                    string name = "GameManager";
                    var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(objectSearch.transform.Find("mask/content"), "Function");
                    debugs.Add($"Inspect {name}");

                    var htt = gameObject.AddComponent<HoverTooltip>();

                    var levelTip = new HoverTooltip.Tooltip();

                    levelTip.desc = $"Inspect {name}";
                    levelTip.hint = "GameManager is the component that handles regular object stuff.";
                    htt.tooltipLangauges.Add(levelTip);

                    var button = gameObject.GetComponent<Button>();
                    button.onClick.ClearAll();
                    button.onClick.AddListener(delegate ()
                    {
                        uiManager.GetProperty("ShowMenu").SetValue(uiManager, true);
                        inspector.GetMethod("Inspect", new[] { typeof(object), AccessTools.TypeByName("UnityExplorer.CacheObject.CacheObjectBase") })
                        .Invoke(inspector, new object[] { GameManager.inst, null });
                    });
                    gameObject.transform.GetChild(0).GetComponent<Text>().text = $"Inspect {name}";
                }

                // Inspect Object Editor UI
                {
                    string name = "Object Editor UI";
                    var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(objectSearch.transform.Find("mask/content"), "Function");
                    debugs.Add($"Inspect {name}");

                    var htt = gameObject.AddComponent<HoverTooltip>();

                    var levelTip = new HoverTooltip.Tooltip();

                    levelTip.desc = $"Inspect {name}";
                    levelTip.hint = "Object Editor UI.";
                    htt.tooltipLangauges.Add(levelTip);

                    var button = gameObject.GetComponent<Button>();
                    button.onClick.ClearAll();
                    button.onClick.AddListener(delegate ()
                    {
                        uiManager.GetProperty("ShowMenu").SetValue(uiManager, true);
                        inspector.GetMethod("Inspect", new[] { typeof(object), AccessTools.TypeByName("UnityExplorer.CacheObject.CacheObjectBase") })
                        .Invoke(inspector, new object[] { ObjEditor.inst.ObjectView, null });
                    });
                    gameObject.transform.GetChild(0).GetComponent<Text>().text = $"Inspect {name}";
                }
            }
        }

        public string autosaveSearch;
        public Transform autosaveContent;
        public InputField autosaveSearchField;
        public void CreateAutosavePopup()
        {
            var objectSearch = EditorManager.inst.GetDialog("Parent Selector").Dialog.gameObject
                .Duplicate(EditorManager.inst.GetDialog("Parent Selector").Dialog.GetParent(), "Autosaves Popup");
            objectSearch.transform.localPosition = Vector3.zero;

            var objectSearchRT = (RectTransform)objectSearch.transform;
            objectSearchRT.anchoredPosition = new Vector2(572f, 0f);
            objectSearchRT.sizeDelta = new Vector2(460f, 350f);
            var objectSearchPanel = (RectTransform)objectSearch.transform.Find("Panel");
            objectSearchPanel.sizeDelta = new Vector2(492f, 32f);
            objectSearchPanel.transform.Find("Text").GetComponent<Text>().text = "Autosaves";
            ((RectTransform)objectSearch.transform.Find("search-box")).sizeDelta = new Vector2(460f, 32f);
            objectSearch.transform.Find("mask/content").GetComponent<GridLayoutGroup>().cellSize = new Vector2(455f, 32f);
            objectSearch.transform.Find("Scrollbar").AsRT().sizeDelta = new Vector2(32f, 350f);

            var x = objectSearchPanel.transform.Find("x").GetComponent<Button>();
            x.onClick.RemoveAllListeners();
            x.onClick.AddListener(delegate ()
            {
                EditorManager.inst.HideDialog("Autosaves Popup");
            });

            autosaveSearchField = objectSearch.transform.Find("search-box/search").GetComponent<InputField>();
            objectSearch.transform.Find("search-box/search/Placeholder").GetComponent<Text>().text = "Search for autosave...";
            autosaveContent = objectSearch.transform.Find("mask/content");

            EditorHelper.AddEditorPopup("Autosaves Popup", objectSearch);
        }

        #endregion

        #region Saving / Loading

        public void SetFileInfo(string text)
        {
            if (fileInfoText)
                fileInfoText.text = text;
        }

        public bool themesLoading = false;

        public bool autoSaving = false;

        public static bool LevelLoadsSavedTime => GetEditorProperty("Level Loads Last Time").GetConfigEntry<bool>().Value;
        public static bool LevelPausesOnStart => GetEditorProperty("Level Pauses on Start").GetConfigEntry<bool>().Value;
        
        public IEnumerator LoadLevels()
        {
            EditorManager.inst.loadedLevels.Clear();

            // We get level editor properties before iterating level list.
            var olfnm = GetEditorProperty("Open Level Folder Name Max").GetConfigEntry<int>();
            var olsnm = GetEditorProperty("Open Level Song Name Max").GetConfigEntry<int>();
            var olanm = GetEditorProperty("Open Level Artist Name Max").GetConfigEntry<int>();
            var olcnm = GetEditorProperty("Open Level Creator Name Max").GetConfigEntry<int>();
            var oldem = GetEditorProperty("Open Level Description Max").GetConfigEntry<int>();
            var oldam = GetEditorProperty("Open Level Date Max").GetConfigEntry<int>();

            int foldClamp = olfnm.Value < 3 ? olfnm.Value : (int)olfnm.DefaultValue;
            int songClamp = olsnm.Value < 3 ? olsnm.Value : (int)olsnm.DefaultValue;
            int artiClamp = olanm.Value < 3 ? olanm.Value : (int)olanm.DefaultValue;
            int creaClamp = olcnm.Value < 3 ? olcnm.Value : (int)olcnm.DefaultValue;
            int descClamp = oldem.Value < 3 ? oldem.Value : (int)oldem.DefaultValue;
            int dateClamp = oldam.Value < 3 ? oldam.Value : (int)oldam.DefaultValue;

            var transform = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("mask/content");
            var close = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("Panel/x");

            var horizontalOverflow = GetEditorProperty("Open Level Text Horizontal Wrap").GetConfigEntry<HorizontalWrapMode>().Value;
            var verticalOverflow = GetEditorProperty("Open Level Text Vertical Wrap").GetConfigEntry<VerticalWrapMode>().Value;
            var fontSize = GetEditorProperty("Open Level Text Font Size").GetConfigEntry<int>().Value;
            var format = GetEditorProperty("Open Level Text Formatting").GetConfigEntry<string>().Value;
            var buttonHoverSize = GetEditorProperty("Open Level Button Hover Size").GetConfigEntry<float>().Value;

            var iconPosition = GetEditorProperty("Open Level Cover Position").GetConfigEntry<Vector2>().Value;
            var iconScale = GetEditorProperty("Open Level Cover Scale").GetConfigEntry<Vector2>().Value;

            var showDeleteButton = GetEditorProperty("Open Level Show Delete Button").GetConfigEntry<bool>().Value;

            LSHelpers.DeleteChildren(transform);

            bool anyFailed = false;
            var failedLevels = new List<string>();

            var list = new List<Coroutine>();
            var files = Directory.GetDirectories(RTFile.ApplicationDirectory + editorListPath);

            int num = 0;
            foreach (var file in files)
            {
                int index = num;
                var path = file.Replace("\\", "/");
                var name = Path.GetFileName(path);
                var metadataStr = FileManager.inst.LoadJSONFileRaw(file + "/metadata.lsb");

                if (metadataStr != null)
                {
                    var metadata = MetaData.Parse(JSON.Parse(metadataStr));

                    var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(transform, $"Folder [{Path.GetFileName(path)}]");

                    var editorWrapper = new EditorWrapper(gameObject, metadata, path, SteamWorkshop.inst.defaultSteamImageSprite);

                    var hoverUI = gameObject.AddComponent<HoverUI>();
                    hoverUI.size = buttonHoverSize;
                    hoverUI.animatePos = false;
                    hoverUI.animateSca = true;

                    var text = gameObject.transform.GetChild(0).GetComponent<Text>();

                    text.text = string.Format(format,
                        LSText.ClampString(Path.GetFileName(path), foldClamp),
                        LSText.ClampString(metadata.song.title, songClamp),
                        LSText.ClampString(metadata.artist.Name, artiClamp),
                        LSText.ClampString(metadata.creator.steam_name, creaClamp),
                        metadata.song.difficulty,
                        LSText.ClampString(metadata.song.description, descClamp),
                        LSText.ClampString(metadata.beatmap.date_edited, dateClamp));

                    text.horizontalOverflow = horizontalOverflow;
                    text.verticalOverflow = verticalOverflow;
                    text.fontSize = fontSize;

                    var htt = gameObject.AddComponent<HoverTooltip>();

                    var levelTip = new HoverTooltip.Tooltip();

                    var difficultyColor = metadata.song.difficulty >= 0 && metadata.song.difficulty < DataManager.inst.difficulties.Count ?
                        DataManager.inst.difficulties[metadata.song.difficulty].color : LSColors.themeColors["none"].color;

                    levelTip.desc = "<#" + LSColors.ColorToHex(difficultyColor) + ">" + metadata.artist.Name + " - " + metadata.song.title;
                    levelTip.hint = "</color>" + metadata.song.description;
                    htt.tooltipLangauges.Add(levelTip);

                    gameObject.GetComponent<Button>().onClick.AddListener(delegate ()
                    {
                        // If clicking on the level button shows the autosaves popup or not.
                        if (Input.GetKey(KeyCode.LeftShift))
                        {
                            EditorManager.inst.ShowDialog("Autosaves Popup");
                            RefreshAutosaveList(editorWrapper);
                        }
                        else
                        {
                            StartCoroutine(LoadLevel(path));
                            EditorManager.inst.HideDialog("Open File Popup");
                        }
                    });

                    var icon = new GameObject("icon");
                    icon.transform.SetParent(gameObject.transform);
                    icon.transform.localScale = Vector3.one;
                    icon.layer = 5;
                    var iconRT = icon.AddComponent<RectTransform>();
                    icon.AddComponent<CanvasRenderer>();
                    var iconImage = icon.AddComponent<Image>();

                    iconRT.anchoredPosition = iconPosition;
                    iconRT.sizeDelta = iconScale;

                    // Close
                    if (showDeleteButton)
                    {
                        var delete = close.gameObject.Duplicate(gameObject.transform, "delete");

                        delete.GetComponent<RectTransform>().anchoredPosition = new Vector2(-5f, 0f);

                        string levelName = path;

                        var deleteButton = delete.GetComponent<Button>();
                        deleteButton.onClick.ClearAll();
                        deleteButton.onClick.AddListener(delegate ()
                        {
                            EditorManager.inst.ShowDialog("Warning Popup");
                            RefreshWarningPopup("Are you sure you want to delete this level? (It will be moved to a recycling folder)", delegate ()
                            {
                                DeleteLevelFunction(levelName);
                                EditorManager.inst.DisplayNotification("Deleted level!", 2f, EditorManager.NotificationType.Success);
                                EditorManager.inst.GetLevelList();
                                EditorManager.inst.HideDialog("Warning Popup");
                            }, delegate ()
                            {
                                EditorManager.inst.HideDialog("Warning Popup");
                            });
                        });
                    }

                    list.Add(StartCoroutine(GetAlbumSprite(file, delegate (Sprite cover)
                    {
                        iconImage.sprite = cover ?? SteamWorkshop.inst.defaultSteamImageSprite;
                        editorWrapper.albumArt = cover ?? SteamWorkshop.inst.defaultSteamImageSprite;

                        EditorManager.inst.loadedLevels.Add(editorWrapper);
                    }, delegate
                    {
                        anyFailed = true;
                        failedLevels.Add(Path.GetFileName(path));

                        iconImage.sprite = SteamWorkshop.inst.defaultSteamImageSprite;
                        editorWrapper.albumArt = SteamWorkshop.inst.defaultSteamImageSprite;

                        EditorManager.inst.loadedLevels.Add(editorWrapper);
                    })));
                }
                else
                    Debug.LogError($"{EditorManager.inst.className}Could not load metadata for [{name}]!");
                num++;
            }

            if (list.Count >= 1)
                yield return StartCoroutine(LSHelpers.WaitForMultipleCoroutines(list, delegate
                {
                    if (anyFailed && GetEditorProperty("Show Levels Without Cover Notification").GetConfigEntry<bool>().Value)
                        EditorManager.inst.DisplayNotification($"Levels {FontManager.TextTranslater.ArrayToString(failedLevels.ToArray())} do not have covers!", 2f * (failedLevels.Count * 0.10f), EditorManager.NotificationType.Error);
                    if (EditorManager.inst.loadedLevels.Count > 0)
                        EditorManager.inst.OpenBeatmapPopup();
                    else
                        EditorManager.inst.OpenNewLevelPopup();
                }));
            else
            {
                if (anyFailed && GetEditorProperty("Show Levels Without Cover Notification").GetConfigEntry<bool>().Value)
                    EditorManager.inst.DisplayNotification($"Levels {FontManager.TextTranslater.ArrayToString(failedLevels.ToArray())} do not have covers!", 2f * (failedLevels.Count * 0.10f), EditorManager.NotificationType.Error);
                if (EditorManager.inst.loadedLevels.Count > 0)
                    EditorManager.inst.OpenBeatmapPopup();
                else
                    EditorManager.inst.OpenNewLevelPopup();
            }

            failedLevels.Clear();
            failedLevels = null;

            yield break;
        }

        /// <summary>
        /// Loads a level in the editor from a full path. For example: E:/4.1.16/beatmaps/editor/New Awesome Beatmap.
        /// </summary>
        /// <param name="fullPath"></param>
        /// <param name="autosave"></param>
        /// <returns></returns>
        public IEnumerator LoadLevel(string fullPath, string autosave = "")
        {
            EditorManager.inst.loading = true;

            string code = $"{fullPath}/EditorLoad.cs";
            if (RTFile.FileExists(code))
            {
                var str = RTFile.ReadFromFile(code);
                if (RTCode.Validate(str))
                    yield return StartCoroutine(RTCode.IEvaluate(str));
            }

            layerType = LayerType.Objects;
            SetLayer(0);

            for (int i = 0; i < ObjEditor.inst.TimelineParents.Count; i++)
            {
                LSHelpers.DeleteChildren(ObjEditor.inst.TimelineParents[i]);
            }

            Updater.UpdateObjects(false);

            // We stop and play the doggo bop animation in case the user has looked at the settings dialog.
            EditorManager.inst.CancelInvoke("LoadingIconUpdate");
            EditorManager.inst.InvokeRepeating("LoadingIconUpdate", 0f, 0.05f);

            var name = Path.GetFileName(fullPath);

            EditorManager.inst.currentLoadedLevel = name;
            EditorManager.inst.SetPitch(1f);

            EditorManager.inst.timelineScrollbar.GetComponent<Scrollbar>().value = 0f;
            GameManager.inst.gameState = GameManager.State.Loading;
            string rawJSON = null;
            string rawMetadataJSON = null;
            AudioClip song = null;

            EditorManager.inst.ClearDialogs();
            EditorManager.inst.ShowDialog("File Info Popup");

            if (EditorManager.inst.hasLoadedLevel && RTFile.DirectoryExists(GameManager.inst.path.Replace("/level.lsb", "")))
            {
                Debug.Log($"{EditorPlugin.className}Backing up previous level {Path.GetFileName(GameManager.inst.path.Replace("/level.lsb", ""))}...");
                SetFileInfo($"Backing up previous level [ {Path.GetFileName(GameManager.inst.path.Replace("/level.lsb", ""))} ]");

                yield return StartCoroutine(ProjectData.Writer.SaveData(GameManager.inst.path.Replace("level.lsb", "level-open-backup.lsb"), GameData.Current));
            }

            SetFileInfo($"Loading Level Data for [ {name} ]");

            Debug.Log($"{EditorPlugin.className}Loading {(string.IsNullOrEmpty(autosave) ? "level.lsb" : autosave)}...");
            rawJSON = FileManager.inst.LoadJSONFileRaw(fullPath + "/" + (string.IsNullOrEmpty(autosave) ? "level.lsb" : autosave));
            rawMetadataJSON = FileManager.inst.LoadJSONFileRaw(fullPath + "/metadata.lsb");

            if (string.IsNullOrEmpty(rawMetadataJSON))
            {
                DataManager.inst.SaveMetadata(fullPath + "/metadata.lsb");
                rawMetadataJSON = FileManager.inst.LoadJSONFileRaw(fullPath + "/metadata.lsb");
            }

            GameManager.inst.path = fullPath + "/level.lsb";
            GameManager.inst.basePath = fullPath + "/";
            GameManager.inst.levelName = name;
            SetFileInfo($"Loading Level Music for [ {name} ]\n\nIf this is taking more than a minute or two check if the song file (.ogg / .wav / .mp3) is corrupt. If not, then something went really wrong.");

            string errorMessage = "";
            bool hadError = false;
            Debug.Log($"{EditorPlugin.className}Loading audio for {name}...");
            if (RTFile.FileExists(fullPath + "/level.ogg"))
            {
                yield return StartCoroutine(RTFunctions.Functions.Managers.Networking.AlephNetworkManager.DownloadAudioClip("file://" + fullPath + "/level.ogg", AudioType.OGGVORBIS, x => song = x, delegate (string onError) { hadError = true; errorMessage = onError; }));
            }
            else if (RTFile.FileExists(fullPath + "/level.wav"))
            {
                yield return StartCoroutine(RTFunctions.Functions.Managers.Networking.AlephNetworkManager.DownloadAudioClip("file://" + fullPath + "/level.wav", AudioType.WAV, x => song = x, delegate (string onError) { hadError = true; errorMessage = onError; }));
            }
            else if (RTFile.FileExists(fullPath + "/level.mp3"))
            {
                yield return song = LSAudio.CreateAudioClipUsingMP3File(fullPath + "/level.mp3");
            }

            // Wait for the song.
            while (song == null && !hadError)
                yield return null;

            if (hadError)
            {
                bool audioExists = RTFile.FileExists(fullPath + "/level.ogg") || RTFile.FileExists(fullPath + "/level.wav") || RTFile.FileExists(fullPath + "/level.mp3");

                if (audioExists)
                    SetFileInfo($"Something went wrong when loading the song file. Either the file is corrupt or something went wrong internally.");
                else
                    SetFileInfo($"Song file does not exist.");

                EditorManager.inst.DisplayNotification($"Song file could not load due to {errorMessage}", 3f, EditorManager.NotificationType.Error);

                Debug.LogError($"{EditorPlugin.className}Level loading caught an error: {errorMessage}\n" +
                    $"level.ogg exists: {RTFile.FileExists(fullPath + "/level.ogg")}\n" +
                    $"level.wav exists: {RTFile.FileExists(fullPath + "/level.wav")}\n" +
                    $"level.mp3 exists: {RTFile.FileExists(fullPath + "/level.mp3")}\n");

                yield break;
            }

            if (RTFile.FileExists(fullPath + "/bg.mp4") && RTFunctions.FunctionsPlugin.EnableVideoBackground.Value)
            {
                RTVideoManager.inst.Play(fullPath + "/bg.mp4", 1f);
                while (!RTVideoManager.inst.videoPlayer.isPrepared)
                    yield return null;
            }
            else if (RTFile.FileExists(fullPath + "/bg.mov") && RTFunctions.FunctionsPlugin.EnableVideoBackground.Value)
            {
                RTVideoManager.inst.Play(fullPath + "/bg.mov", 1f);
                while (!RTVideoManager.inst.videoPlayer.isPrepared)
                    yield return null;
            }
            else
            {
                RTVideoManager.inst.Stop();
            }

            GameManager.inst.gameState = GameManager.State.Parsing;
            SetFileInfo($"Parsing Level Data for [ {name} ]");
            if (!string.IsNullOrEmpty(rawJSON) && !string.IsNullOrEmpty(rawMetadataJSON))
            {
                try
                {
                    DataManager.inst.metaData = MetaData.Parse(JSON.Parse(rawMetadataJSON));

                    if (DataManager.inst.metaData.beatmap.game_version != "4.1.16" && DataManager.inst.metaData.beatmap.game_version != "20.4.4")
                        rawJSON = DataManager.inst.gameData.UpdateBeatmap(rawJSON, DataManager.inst.metaData.beatmap.game_version);

                    DataManager.inst.gameData = GameData.Parse(JSON.Parse(rawJSON), false);

                    if (DataManager.inst.metaData.beatmap.workshop_id == -1)
                        DataManager.inst.metaData.beatmap.workshop_id = UnityEngine.Random.Range(0, int.MaxValue);
                }
                catch (Exception ex)
                {
                    SetFileInfo($"Something went wrong when parsing the level data. Press the open log folder key ({RTFunctions.FunctionsPlugin.OpenPAPersistentFolder.Value}) and send the Player.log file to Mecha.");

                    EditorManager.inst.DisplayNotification("Level could not load.", 3f, EditorManager.NotificationType.Error);

                    Debug.LogError($"{EditorPlugin.className}Level loading caught an error: {ex}");

                    yield break;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(rawJSON) && !string.IsNullOrEmpty(rawMetadataJSON))
                    SetFileInfo($"level.lsb is empty or corrupt.");

                if (!string.IsNullOrEmpty(rawJSON) && string.IsNullOrEmpty(rawMetadataJSON))
                    SetFileInfo($"metadata.lsb is empty or corrupt.");

                if (string.IsNullOrEmpty(rawJSON) && string.IsNullOrEmpty(rawMetadataJSON))
                    SetFileInfo($"Both level.lsb and metadata.lsb are corrupt.");

                EditorManager.inst.DisplayNotification("Level could not load.", 3f, EditorManager.NotificationType.Error);

                yield break;
            }

            if (ModCompatibility.CreativePlayersInstalled)
            {
                PlayerManager.LoadGlobalModels?.Invoke();
                PlayerManager.LoadIndexes?.Invoke();
                PlayerManager.RespawnPlayers();
            }

            SetFileInfo($"Loading Themes for [ {name} ]");
            yield return StartCoroutine(LoadThemes());

            // For some reason loading themes doesn't hold the enumerator so instead we check for themesLoading.
            float delayTheme = 0f;
            while (themesLoading)
            {
                yield return new WaitForSeconds(delayTheme);
                delayTheme += 0.0001f;
            }

            Debug.Log($"{EditorPlugin.className}Music is null: {song == null}");

            SetFileInfo($"Playing Music for [ {name} ]\n\nIf it doesn't, then something went wrong!");
            AudioManager.inst.PlayMusic(null, song, true, 0f, true);
            StartCoroutine(EditorManager.inst.SpawnPlayersWithDelay(0.2f));
            if (GenerateWaveform)
            {
                SetFileInfo($"Assigning Waveform Textures for [ {name} ]");
                StartCoroutine(AssignTimelineTexture());
            }
            else
            {
                SetFileInfo($"Skipping Waveform Textures for [ {name} ]");
                TimelineImage.sprite = null;
                TimelineOverlayImage.sprite = null;
            }

            SetFileInfo($"Updating Timeline for [ {name} ]");
            EditorManager.inst.UpdateTimelineSizes();
            GameManager.inst.UpdateTimeline();
            EditorManager.inst.ClearDialogs();
            MetadataEditor.inst.Render();

            CheckpointEditor.inst.CreateGhostCheckpoints();

            SetFileInfo($"Updating states for [ {name} ]");
            RTFunctions.FunctionsPlugin.UpdateDiscordStatus($"Editing: {DataManager.inst.metaData.song.title}", "In Editor", "editor");

            ObjectManager.inst.updateObjects();
            EventEditor.inst.CreateEventObjects();
            BackgroundManager.inst.UpdateBackgrounds();
            GameManager.inst.UpdateTheme();
            MarkerEditor.inst.CreateMarkers();
            EventManager.inst.updateEvents();

            if (ModCompatibility.sharedFunctions.ContainsKey("EventsCoreResetOffsets"))
            {
                ((Action)ModCompatibility.sharedFunctions["EventsCoreResetOffsets"])?.Invoke();
            }

            SetFileInfo($"Setting first object of [ {name} ]");
            ObjectEditor.inst.CreateTimelineObjects();
            ObjectEditor.inst.RenderTimelineObjects();
            if (timelineObjects.Count > 0)
                ObjectEditor.inst.SetCurrentObject(timelineObjects[0]);

            CheckpointEditor.inst.SetCurrentCheckpoint(0);

            SetFileInfo("Done!");
            EditorManager.inst.HideDialog("File Info Popup");
            EditorManager.inst.CancelInvoke("LoadingIconUpdate");

            GameManager.inst.ResetCheckpoints(true);
            GameManager.inst.gameState = GameManager.State.Playing;

            EditorManager.inst.DisplayNotification($"{name} Level Loaded", 2f, EditorManager.NotificationType.Success);
            EditorManager.inst.UpdatePlayButton();
            EditorManager.inst.hasLoadedLevel = true;

            // Autosave handlers
            {
                if (!RTFile.DirectoryExists(GameManager.inst.basePath + "autosaves"))
                    Directory.CreateDirectory(GameManager.inst.basePath + "autosaves");

                string[] files = Directory.GetFiles(GameManager.inst.basePath + "autosaves", "autosave_*.lsb", SearchOption.TopDirectoryOnly);
                files.ToList().Sort();

                EditorManager.inst.autosaves.Clear();

                foreach (var file in files)
                {
                    EditorManager.inst.autosaves.Add(file);
                }

                SetAutosave();
            }

            TriggerHelper.AddEventTrigger(timeIF.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(timeIF, max: AudioManager.inst.CurrentAudioSource.clip.length) });

            // Load Settings like timeline position, editor layer, bpm active, etc
            LoadSettings();

            if (LevelPausesOnStart)
            {
                AudioManager.inst.CurrentAudioSource.Pause();
                EditorManager.inst.UpdatePlayButton();
            }

            if (ModCompatibility.sharedFunctions.ContainsKey("EditorOnLoadLevel"))
                ((Action)ModCompatibility.sharedFunctions["EditorOnLoadLevel"])();

            EditorManager.inst.loading = false;

            yield break;
        }

        public GameObject themeAddButton;
        public IEnumerator LoadThemes(bool refreshGUI = false)
        {
            if (themesLoading)
                yield break;

            themesLoading = true;
            DataManager.inst.CustomBeatmapThemes.Clear();
            DataManager.inst.BeatmapThemeIDToIndex.Clear();
            DataManager.inst.BeatmapThemeIndexToID.Clear();
            if (GameData.Current != null)
                GameData.Current.beatmapThemes.Clear();

            var dialogTmp = EventEditor.inst.dialogRight.GetChild(4);
            var parent = dialogTmp.Find("themes/viewport/content");

            if (ThemeEditorManager.inst.ThemePanels.Count > 0)
            {
                ThemeEditorManager.inst.ThemePanels.ForEach(x => Destroy(x.GameObject));
            }
            ThemeEditorManager.inst.ThemePanels.Clear();
            //LSHelpers.DeleteChildren(parent);

            if (themeAddButton == null)
            {
                themeAddButton = EventEditor.inst.ThemeAdd.Duplicate(parent, "Create New");
                var tf = themeAddButton.transform;
                themeAddButton.SetActive(true);
                tf.localScale = Vector2.one;
                themeAddButton.GetComponent<Button>().onClick.AddListener(delegate ()
                {
                    ThemeEditorManager.inst.RenderThemeEditor();
                });
            }

            int num = 0;
            foreach (var beatmapTheme in DataManager.inst.BeatmapThemes.Select(x => x as BeatmapTheme))
            {
                DataManager.inst.BeatmapThemeIDToIndex.Add(num, num);
                DataManager.inst.BeatmapThemeIndexToID.Add(num, num);

                var themePanel = ThemeEditorManager.inst.GenerateThemePanel(parent);
                themePanel.Theme = beatmapTheme;

                for (int j = 0; j < themePanel.Colors.Count; j++)
                {
                    themePanel.Colors[j].color = beatmapTheme.GetObjColor(j);
                }

                themePanel.UseButton.onClick.ClearAll();
                themePanel.UseButton.onClick.AddListener(delegate ()
                {
                    if (RTEventEditor.inst.SelectedKeyframes.Count > 1 && RTEventEditor.inst.SelectedKeyframes.All(x => RTEventEditor.inst.SelectedKeyframes.Min(y => y.Type) == x.Type))
                    {
                        foreach (var timelineObject in RTEventEditor.inst.SelectedKeyframes)
                        {
                            timelineObject.GetData<EventKeyframe>().eventValues[0] = Parser.TryParse(beatmapTheme.id, 0);
                        }
                    }
                    else
                    {
                        DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] = Parser.TryParse(beatmapTheme.id, 0);
                    }
                    EventManager.inst.updateEvents();
                    EventEditor.inst.RenderThemePreview(dialogTmp);

                });

                themePanel.EditButton.onClick.ClearAll();
                themePanel.EditButton.onClick.AddListener(delegate ()
                {
                    ThemeEditorManager.inst.RenderThemeEditor(Parser.TryParse(beatmapTheme.id, 0));
                });

                themePanel.DeleteButton.onClick.ClearAll();
                themePanel.DeleteButton.interactable = false;
                themePanel.Name.text = beatmapTheme.name;
                num++;
            }

            var files = Directory.GetFiles(RTFile.ApplicationDirectory + themeListPath, "*.lst");
            foreach (var file in files)
            {
                var jn = JSON.Parse(RTFile.ReadFromFile(file));
                var orig = BeatmapTheme.Parse(jn);
                DataManager.inst.CustomBeatmapThemes.Add(orig);

                if (jn["id"] != null && GameData.Current != null && GameData.Current.beatmapThemes != null && !GameData.Current.beatmapThemes.ContainsKey(jn["id"]))
                    GameData.Current.beatmapThemes.Add(jn["id"], orig);

                if (DataManager.inst.BeatmapThemeIDToIndex.ContainsKey(int.Parse(orig.id)))
                {
                    var array = DataManager.inst.CustomBeatmapThemes.Where(x => x.id == orig.id).Select(x => x.name).ToArray();
                    var str = FontManager.TextTranslater.ArrayToString(array);

                    if (EditorManager.inst != null)
                    {
                        EditorManager.inst.DisplayNotification($"Unable to Load theme [{orig.name}] due to conflicting themes: {str}", 2f * array.Length, EditorManager.NotificationType.Error);
                    }
                }
                else
                {
                    DataManager.inst.BeatmapThemeIndexToID.Add(DataManager.inst.AllThemes.Count - 1, int.Parse(orig.id));
                    DataManager.inst.BeatmapThemeIDToIndex.Add(int.Parse(orig.id), DataManager.inst.AllThemes.Count - 1);

                    var themePanel = ThemeEditorManager.inst.GenerateThemePanel(parent);
                    themePanel.Theme = orig;
                    themePanel.Path = file.Replace("\\", "/");
                    themePanel.OriginalID = orig.id;

                    for (int j = 0; j < themePanel.Colors.Count; j++)
                    {
                        themePanel.Colors[j].color = orig.GetObjColor(j);
                    }

                    themePanel.UseButton.onClick.ClearAll();
                    themePanel.UseButton.onClick.AddListener(delegate ()
                    {
                        if (RTEventEditor.inst.SelectedKeyframes.Count > 1 && RTEventEditor.inst.SelectedKeyframes.All(x => RTEventEditor.inst.SelectedKeyframes.Min(y => y.Type) == x.Type))
                        {
                            foreach (var timelineObject in RTEventEditor.inst.SelectedKeyframes)
                            {
                                timelineObject.GetData<EventKeyframe>().eventValues[0] = Parser.TryParse(orig.id, 0);
                            }
                        }
                        else
                        {
                            DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] = Parser.TryParse(orig.id, 0);
                        }
                        EventManager.inst.updateEvents();
                        EventEditor.inst.RenderThemePreview(dialogTmp);
                    });

                    themePanel.EditButton.onClick.ClearAll();
                    themePanel.EditButton.onClick.AddListener(delegate ()
                    {
                        ThemeEditorManager.inst.RenderThemeEditor(Parser.TryParse(orig.id, 0));
                    });

                    themePanel.DeleteButton.onClick.ClearAll();
                    themePanel.DeleteButton.interactable = true;
                    themePanel.DeleteButton.onClick.AddListener(delegate ()
                    {
                        ThemeEditorManager.inst.DeleteThemeDelegate(orig);
                    });
                    themePanel.Name.text = orig.name;
                }

                if (jn["id"] == null)
                {
                    var beatmapTheme = BeatmapTheme.DeepCopy(orig);
                    beatmapTheme.id = LSText.randomNumString(BeatmapTheme.IDLength);
                    DataManager.inst.CustomBeatmapThemes.Remove(orig);
                    FileManager.inst.DeleteFileRaw(file);
                    ThemeEditor.inst.SaveTheme(beatmapTheme);
                    DataManager.inst.CustomBeatmapThemes.Add(beatmapTheme);
                }
            }
            themesLoading = false;

            if (refreshGUI)
            {
                var themeSearch = dialogTmp.Find("theme-search").GetComponent<InputField>();
                yield return StartCoroutine(ThemeEditorManager.inst.RenderThemeList(themeSearch.text));
            }

            canUpdateThemes = true;

            yield break;
        }

        public bool prefabsLoading = false;
        public GameObject prefabExternalAddButton;
        public IEnumerator LoadPrefabs(PrefabEditor __instance)
        {
            if (prefabsLoading)
                yield break;

            prefabsLoading = true;

            while (!PrefabEditor.inst || !PrefabEditor.inst.externalContent)
                yield return null;

            PrefabEditorManager.inst.PrefabPanels.RemoveAll(x => x.Dialog == PrefabDialog.External);

            LSHelpers.DeleteChildren(PrefabEditor.inst.externalContent);

            var gameObject = PrefabEditor.inst.CreatePrefab.Duplicate(PrefabEditor.inst.externalContent, "add new prefab");
            gameObject.GetComponentInChildren<Text>().text = "New External Prefab";

            var hoverSize = GetEditorProperty("Prefab Button Hover Size").GetConfigEntry<float>().Value;

            var hover = gameObject.AddComponent<HoverUI>();
            hover.animateSca = true;
            hover.animatePos = false;
            hover.size = hoverSize;

            gameObject.GetComponentAndPerformAction(delegate (Button x)
            {
                x.NewOnClickListener(delegate ()
                {
                    PrefabEditor.inst.OpenDialog();
                    PrefabEditorManager.inst.createInternal = false;
                });
            });

            bool isExternal = true;

            var nameHorizontalOverflow = isExternal ?
                GetEditorProperty("Prefab External Name Horizontal Wrap").GetConfigEntry<HorizontalWrapMode>().Value :
                GetEditorProperty("Prefab Internal Name Horizontal Wrap").GetConfigEntry<HorizontalWrapMode>().Value;

            var nameVerticalOverflow = isExternal ?
                GetEditorProperty("Prefab External Name Vertical Wrap").GetConfigEntry<VerticalWrapMode>().Value :
                GetEditorProperty("Prefab Internal Name Vertical Wrap").GetConfigEntry<VerticalWrapMode>().Value;

            var nameFontSize = isExternal ?
                GetEditorProperty("Prefab External Name Font Size").GetConfigEntry<int>().Value :
                GetEditorProperty("Prefab Internal Name Font Size").GetConfigEntry<int>().Value;

            var typeHorizontalOverflow = isExternal ?
                GetEditorProperty("Prefab External Type Horizontal Wrap").GetConfigEntry<HorizontalWrapMode>().Value :
                GetEditorProperty("Prefab Internal Type Horizontal Wrap").GetConfigEntry<HorizontalWrapMode>().Value;

            var typeVerticalOverflow = isExternal ?
                GetEditorProperty("Prefab External Type Vertical Wrap").GetConfigEntry<VerticalWrapMode>().Value :
                GetEditorProperty("Prefab Internal Type Vertical Wrap").GetConfigEntry<VerticalWrapMode>().Value;

            var typeFontSize = isExternal ?
                GetEditorProperty("Prefab External Type Font Size").GetConfigEntry<int>().Value :
                GetEditorProperty("Prefab Internal Type Font Size").GetConfigEntry<int>().Value;

            var deleteAnchoredPosition = isExternal ?
                GetEditorProperty("Prefab External Delete Button Pos").GetConfigEntry<Vector2>().Value :
                GetEditorProperty("Prefab Internal Delete Button Pos").GetConfigEntry<Vector2>().Value;
            var deleteSizeDelta = isExternal ?
                GetEditorProperty("Prefab External Delete Button Sca").GetConfigEntry<Vector2>().Value :
                GetEditorProperty("Prefab Internal Delete Button Sca").GetConfigEntry<Vector2>().Value;

            int num = 0;
            foreach (var file in Directory.GetFiles(RTFile.ApplicationDirectory + prefabListPath, "*.lsp", SearchOption.TopDirectoryOnly))
            {
                var jn = JSON.Parse(RTFile.ReadFromFile(file));

                var prefab = Prefab.Parse(jn);
                prefab.objects.ForEach(x => { x.prefabID = ""; x.prefabInstanceID = ""; });
                //prefab.objects.ForEach(x => x.prefabInstanceID = "");
                __instance.LoadedPrefabs.Add(Prefab.Parse(jn));
                __instance.LoadedPrefabsFiles.Add(file);

                StartCoroutine(PrefabEditorManager.inst.CreatePrefabButton(prefab, num, PrefabDialog.External, file, false, hoverSize,
                         nameHorizontalOverflow, nameVerticalOverflow, nameFontSize,
                         typeHorizontalOverflow, typeVerticalOverflow, typeFontSize,
                         deleteAnchoredPosition, deleteSizeDelta));

                num++;
            }

            prefabsLoading = false;

            yield break;
        }

        public IEnumerator UpdatePrefabs()
        {
            PrefabEditor.inst.LoadedPrefabs.Clear();
            PrefabEditor.inst.LoadedPrefabsFiles.Clear();
            yield return inst.StartCoroutine(LoadPrefabs(PrefabEditor.inst));
            PrefabEditor.inst.ReloadExternalPrefabsInPopup();
            EditorManager.inst.DisplayNotification("Updated external prefabs!", 2f, EditorManager.NotificationType.Success);
            yield break;
        }

        public void SetAutosave()
        {
            EditorManager.inst.CancelInvoke("AutoSaveLevel");
            CancelInvoke("AutoSaveLevel");
            InvokeRepeating("AutoSaveLevel", AutoSaveLoopTime, AutoSaveLoopTime);

            //var t = Time.time - timeInEditorOffset;
            //if (t % AutoSaveLoopTime - 0.1f > AutoSaveLoopTime - 0.1f && !autoSaving)
            //    AutoSaveLevel();
        }

        public void AutoSaveLevel()
        {
            if (EditorManager.inst.loading)
                return;

            autoSaving = true;

            if (!EditorManager.inst.hasLoadedLevel)
            {
                EditorManager.inst.DisplayNotification("Beatmap can't autosave until you load a level.", 3f, EditorManager.NotificationType.Error);
                return;
            }

            if (EditorManager.inst.savingBeatmap)
            {
                EditorManager.inst.DisplayNotification("Already attempting to save the beatmap!", 2f, EditorManager.NotificationType.Error);
                return;
            }

            string autosavePath = $"{GameManager.inst.basePath}autosaves/autosave_{DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss")}.lsb";

            if (!RTFile.DirectoryExists(GameManager.inst.basePath + "autosaves"))
                Directory.CreateDirectory(GameManager.inst.basePath + "autosaves");

            EditorManager.inst.DisplayNotification("Autosaving backup!", 2f, EditorManager.NotificationType.Warning);

            EditorManager.inst.autosaves.Add(autosavePath);

            while (EditorManager.inst.autosaves.Count > AutoSaveLimit)
            {
                var first = EditorManager.inst.autosaves[0];
                if (RTFile.FileExists(first))
                    File.Delete(first);

                EditorManager.inst.autosaves.RemoveAt(0);
            }

            EditorManager.inst.StartCoroutine(ProjectData.Writer.SaveData(autosavePath, (GameData)DataManager.inst.gameData));

            EditorManager.inst.DisplayNotification("Autosaved backup!", 2f, EditorManager.NotificationType.Success);

            autoSaving = false;
        }

        public static int AutoSaveLimit => GetEditorProperty("Autosave Limit").GetConfigEntry<int>().Value;
        public static float AutoSaveLoopTime => GetEditorProperty("Autosave Loop Time").GetConfigEntry<float>().Value;

        public static float timeSinceAutosaved;

        public void CreateNewLevel()
        {
            var __instance = EditorManager.inst;
            if (!__instance.newAudioFile.ToLower().Contains(".ogg"))
            {
                __instance.DisplayNotification("The file you are trying to load doesn't appear to be a .ogg file.", 2f, EditorManager.NotificationType.Error, false);
                return;
            }
            if (!RTFile.FileExists(__instance.newAudioFile))
            {
                __instance.DisplayNotification("The file you are trying to load doesn't appear to exist.", 2f, EditorManager.NotificationType.Error, false);
                return;
            }

            bool setNew = false;
            int num = 0;
            string p = RTFile.ApplicationDirectory + editorListSlash + __instance.newLevelName;
            while (RTFile.DirectoryExists(p))
            {
                p = RTFile.ApplicationDirectory + editorListSlash + __instance.newLevelName + " - " + num.ToString();
                num += 1;
                setNew = true;

            }
            if (setNew)
                __instance.newLevelName += " - " + num.ToString();

            if (RTFile.DirectoryExists(RTFile.ApplicationDirectory + editorListSlash + __instance.newLevelName))
            {
                __instance.DisplayNotification("The level you are trying to create already exists.", 2f, EditorManager.NotificationType.Error, false);
                return;
            }
            Directory.CreateDirectory(RTFile.ApplicationDirectory + editorListSlash + __instance.newLevelName);
            if (__instance.newAudioFile.ToLower().Contains(".ogg"))
            {
                string destFileName = RTFile.ApplicationDirectory + editorListSlash + __instance.newLevelName + "/level.ogg";
                File.Copy(__instance.newAudioFile, destFileName, true);
            }

            StartCoroutine(ProjectData.Writer.SaveData(RTFile.ApplicationDirectory + editorListSlash + __instance.newLevelName + "/level.lsb", CreateBaseBeatmap()));
            var dataManager = DataManager.inst;
            var metaData = new MetaData();
            metaData.beatmap.game_version = "4.1.16";
            metaData.song.title = __instance.newLevelName;
            metaData.creator.steam_name = SteamWrapper.inst.user.displayName;
            metaData.creator.steam_id = SteamWrapper.inst.user.id;
            metaData.beatmap.workshop_id = UnityEngine.Random.Range(0, int.MaxValue);
            metaData.id = LSText.randomNumString(16);

            dataManager.metaData = metaData;

            dataManager.SaveMetadata(RTFile.ApplicationDirectory + editorListSlash + __instance.newLevelName + "/metadata.lsb");
            StartCoroutine(LoadLevel(RTFile.ApplicationDirectory + editorListSlash + __instance.newLevelName));
            __instance.HideDialog("New File Popup");
        }

        public GameData CreateBaseBeatmap()
        {
            var gameData = new GameData();
            gameData.beatmapData = new LevelBeatmapData();
            gameData.beatmapData.levelData = new DataManager.GameData.BeatmapData.LevelData();
            gameData.beatmapData.checkpoints.Add(new DataManager.GameData.BeatmapData.Checkpoint(false, "Base Checkpoint", 0f, Vector2.zero));
            var editorData = new LevelEditorData();
            gameData.beatmapData.editorData = editorData;

            if (gameData.eventObjects.allEvents == null)
                gameData.eventObjects.allEvents = new List<List<BaseEventKeyframe>>();
            gameData.eventObjects.allEvents.Clear();
            ProjectData.Reader.ClampEventListValues(gameData.eventObjects.allEvents, GameData.EventCount);

            for (int i = 0; i < 25; i++)
            {
                var backgroundObject = new BackgroundObject();
                backgroundObject.name = "bg - " + i;
                if (UnityEngine.Random.value > 0.5f)
                {
                    backgroundObject.scale = new Vector2(UnityEngine.Random.Range(2, 8), UnityEngine.Random.Range(2, 8));
                }
                else
                {
                    float num = UnityEngine.Random.Range(2, 6);
                    backgroundObject.scale = new Vector2(num, num);
                }
                backgroundObject.pos = new Vector2(UnityEngine.Random.Range(-48, 48), UnityEngine.Random.Range(-32, 32));
                backgroundObject.color = UnityEngine.Random.Range(1, 6);
                backgroundObject.layer = UnityEngine.Random.Range(0, 6);
                backgroundObject.reactive = UnityEngine.Random.value > 0.5f;
                if (backgroundObject.reactive)
                {
                    switch (UnityEngine.Random.Range(0, 4))
                    {
                        case 0:
                            backgroundObject.reactiveType = BaseBackgroundObject.ReactiveType.LOW;
                            break;
                        case 1:
                            backgroundObject.reactiveType = BaseBackgroundObject.ReactiveType.MID;
                            break;
                        case 2:
                            backgroundObject.reactiveType = BaseBackgroundObject.ReactiveType.HIGH;
                            break;
                    }
                    backgroundObject.reactiveScale = UnityEngine.Random.Range(0.01f, 0.04f);
                }

                //backgroundObject.shape = ShapeManager.inst.Shapes3D[UnityEngine.Random.Range(0, ShapeManager.inst.Shapes3D.Count - 1)][0];

                gameData.backgroundObjects.Add(backgroundObject);
            }

            var beatmapObject = ObjectEditor.CreateNewBeatmapObject(0.5f, false);
            beatmapObject.events[0].Add(new EventKeyframe(4f, new float[3] { 10f, 0f, 0f }, new float[3]));
            if (RTHelpers.AprilFools)
                beatmapObject.events[2].Add(new EventKeyframe(999f, new float[1] { 360000f }, new float[3]));

            beatmapObject.name = RTHelpers.AprilFools ? "trololololo" : "\"Default object cameo\" -Viral Mecha";
            beatmapObject.autoKillType = AutoKillType.LastKeyframeOffset;
            beatmapObject.autoKillOffset = 4f;
            beatmapObject.editorData.layer = 0;
            gameData.beatmapObjects.Add(beatmapObject);

            return gameData;
        }

        public IEnumerator GetAlbumSprite(string fullpath, Action<Sprite> callback, Action<string> onError)
        {
            string path = fullpath + "/level.jpg";
            yield return StartCoroutine(EditorManager.inst.GetSprite(path, new EditorManager.SpriteLimits(), callback, onError));
            yield break;
        }

        #endregion

        #region Refresh Popups / Dialogs

        public void RefreshObjectSearch(Action<BeatmapObject> onSelect, bool clearParent = false)
        {
            var dialog = EditorManager.inst.GetDialog("Object Search Popup").Dialog;
            var content = dialog.Find("mask/content");

            if (clearParent)
            {
                var buttonPrefab = EditorManager.inst.spriteFolderButtonPrefab.Duplicate(content, "Clear Parents");
                buttonPrefab.transform.GetChild(0).GetComponent<Text>().text = "Clear Parents";

                buttonPrefab.GetComponentAndPerformAction(delegate (Button b)
                {
                    b.NewOnClickListener(delegate ()
                    {
                        foreach (var bm in ObjectEditor.inst.SelectedObjects.FindAll(x => x.IsBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
                        {
                            bm.parent = "";
                            Updater.UpdateProcessor(bm);
                        }
                    });
                });

                var x = EditorManager.inst.GetDialog("Object Search Popup").Dialog.Find("Panel/x/Image").GetComponent<Image>().sprite;
                var image = buttonPrefab.transform.Find("Image").GetComponent<Image>();
                image.color = Color.red;
                image.sprite = x;
            }

            var searchBar = dialog.Find("search-box/search").GetComponent<InputField>();
            searchBar.onValueChanged.ClearAll();
            searchBar.onValueChanged.AddListener(delegate (string _value)
            {
                objectSearchTerm = _value;
                RefreshObjectSearch(onSelect, clearParent);
            });

            LSHelpers.DeleteChildren(content);

            var list = DataManager.inst.gameData.beatmapObjects;
            foreach (var beatmapObject in list)
            {
                var regex = new Regex(@"\[([0-9])\]");
                var match = regex.Match(objectSearchTerm);

                if (string.IsNullOrEmpty(objectSearchTerm) || beatmapObject.name.ToLower().Contains(objectSearchTerm.ToLower()) || match.Success && int.Parse(match.Groups[1].ToString()) < DataManager.inst.gameData.beatmapObjects.Count && DataManager.inst.gameData.beatmapObjects.IndexOf(beatmapObject) == int.Parse(match.Groups[1].ToString()))
                {
                    string nm = $"[{(list.IndexOf(beatmapObject) + 1).ToString("0000")}/{list.Count.ToString("0000")} - {beatmapObject.id}] : {beatmapObject.name}";
                    var buttonPrefab = EditorManager.inst.spriteFolderButtonPrefab.Duplicate(content, nm);
                    buttonPrefab.transform.GetChild(0).GetComponent<Text>().text = nm;

                    var b = buttonPrefab.GetComponent<Button>();
                    b.interactable = !beatmapObject.fromPrefab;
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(delegate ()
                    {
                        onSelect?.Invoke((BeatmapObject)beatmapObject);
                    });

                    var image = buttonPrefab.transform.Find("Image").GetComponent<Image>();
                    image.color = GetObjectColor(beatmapObject, false);

                    int n = beatmapObject.shape + 1;

                    try
                    {
                        if (beatmapObject.shape == 4 || beatmapObject.shape == 6)
                            image.sprite = ObjEditor.inst.ObjectView.transform.Find("shape/" + n.ToString() + "/Image").GetComponent<Image>().sprite;
                        else
                            image.sprite = ObjEditor.inst.ObjectView.transform.Find("shapesettings").GetChild(beatmapObject.shape).GetChild(beatmapObject.shapeOption).Find("Image").GetComponent<Image>().sprite;

                    }
                    catch
                    {

                    }

                    string desc = "";
                    string hint = "";

                    if (beatmapObject.TryGetGameObject(out GameObject gameObjectRef))
                    {
                        var transform = gameObjectRef.transform;

                        string parent = "";
                        if (!string.IsNullOrEmpty(beatmapObject.parent))
                            parent = "<br>P: " + beatmapObject.parent + " (" + beatmapObject.GetParentType() + ")";
                        else
                            parent = "<br>P: No Parent" + " (" + beatmapObject.GetParentType() + ")";

                        string text = "";
                        if (beatmapObject.shape != 4 || beatmapObject.shape != 6)
                            text = "<br>S: " + RTHelpers.GetShape(beatmapObject.shape, beatmapObject.shapeOption) +
                                "<br>T: " + beatmapObject.text;
                        if (beatmapObject.shape == 4)
                            text = "<br>S: Text" +
                                "<br>T: " + beatmapObject.text;
                        if (beatmapObject.shape == 6)
                            text = "<br>S: Image" +
                                "<br>T: " + beatmapObject.text;

                        string ptr = "";
                        if (!string.IsNullOrEmpty(beatmapObject.prefabID) && !string.IsNullOrEmpty(beatmapObject.prefabInstanceID))
                            ptr = "<br><#" + RTHelpers.ColorToHex(beatmapObject.GetPrefabTypeColor()) + ">PID: " + beatmapObject.prefabID + " | PIID: " + beatmapObject.prefabInstanceID + "</color>";
                        else
                            ptr = "<br>Not from prefab";

                        desc = "N/ST: " + beatmapObject.name + " [ " + beatmapObject.StartTime + " ]";
                        hint = "ID: {" + beatmapObject.id + "}" +
                            parent +
                            "<br>A: " + beatmapObject.TimeWithinLifespan().ToString() +
                            "<br>O: {X: " + beatmapObject.origin.x + ", Y: " + beatmapObject.origin.y + "}" +
                            text +
                            "<br>D: " + beatmapObject.Depth +
                            "<br>ED: {L: " + beatmapObject.editorData.layer + ", B: " + beatmapObject.editorData.Bin + "}" +
                            "<br>POS: {X: " + transform.position.x + ", Y: " + transform.position.y + "}" +
                            "<br>SCA: {X: " + transform.localScale.x + ", Y: " + transform.localScale.y + "}" +
                            "<br>ROT: " + transform.eulerAngles.z +
                            "<br>COL: " + "<#" + RTHelpers.ColorToHex(GetObjectColor(beatmapObject, false)) + ">" + "█ <b>#" + RTHelpers.ColorToHex(GetObjectColor(beatmapObject, true)) + "</b></color>" +
                            ptr;

                        TooltipHelper.AddTooltip(buttonPrefab, desc, hint);
                    }
                }
            }
        }

        public void RefreshWarningPopup(string warning, UnityAction confirmDelegate, UnityAction cancelDelegate, string confirm = "Yes", string cancel = "No")
        {
            var warningPopup = EditorManager.inst.GetDialog("Warning Popup").Dialog.GetChild(0);

            warningPopup.Find("Level Name").GetComponent<Text>().text = warning;

            var submit1 = warningPopup.Find("spacerL/submit1");
            var submit2 = warningPopup.Find("spacerL/submit2");

            var submit1Button = submit1.GetComponent<Button>();
            var submit2Button = submit2.GetComponent<Button>();

            submit1.Find("text").GetComponent<Text>().text = confirm;
            submit2.Find("text").GetComponent<Text>().text = cancel;

            submit1Button.onClick.RemoveAllListeners();
            submit2Button.onClick.RemoveAllListeners();

            submit1Button.onClick.AddListener(confirmDelegate);
            submit2Button.onClick.AddListener(cancelDelegate);
        }

        public void RefreshREPLEditor(string value, UnityAction<string> onEndEdit)
        {
            EditorManager.inst.ShowDialog("REPL Editor Popup");
            replEditor.onValueChanged.ClearAll();
            replEditor.text = value;

            //replText.text = RTCode.ConvertREPLTest(replEditor.textComponent.text);
            replEditor.onValueChanged.AddListener(delegate (string _val)
            {
                //StartCoroutine(SetREPLEditorTextDelay(0.2f));
            });

            replEditor.onEndEdit.RemoveAllListeners();
            replEditor.onEndEdit.AddListener(onEndEdit);
        }

        public static List<LevelFolder<MetadataWrapper>> levelItems = new List<LevelFolder<MetadataWrapper>>();

        public IEnumerator RefreshLevelList()
        {
            levelItems.Clear();

            #region Sorting

            switch (levelFilter)
            {
                case 0:
                    {
                        EditorManager.inst.loadedLevels = (levelAscend ? EditorManager.inst.loadedLevels.OrderBy(x => x.albumArt != SteamWorkshop.inst.defaultSteamImageSprite) :
                            EditorManager.inst.loadedLevels.OrderByDescending(x => x.albumArt != SteamWorkshop.inst.defaultSteamImageSprite)).ToList();
                        break;
                    }
                case 1:
                    {
                        EditorManager.inst.loadedLevels = (levelAscend ? EditorManager.inst.loadedLevels.OrderBy(x => x.metadata.artist.Name) :
                            EditorManager.inst.loadedLevels.OrderByDescending(x => x.metadata.artist.Name)).ToList();
                        break;
                    }
                case 2:
                    {
                        EditorManager.inst.loadedLevels = (levelAscend ? EditorManager.inst.loadedLevels.OrderBy(x => x.metadata.creator.steam_name) :
                            EditorManager.inst.loadedLevels.OrderByDescending(x => x.metadata.creator.steam_name)).ToList();
                        break;
                    }
                case 3:
                    {
                        EditorManager.inst.loadedLevels = (levelAscend ? EditorManager.inst.loadedLevels.OrderBy(x => x.folder) :
                            EditorManager.inst.loadedLevels.OrderByDescending(x => x.folder)).ToList();
                        break;
                    }
                case 4:
                    {
                        EditorManager.inst.loadedLevels = (levelAscend ? EditorManager.inst.loadedLevels.OrderBy(x => x.metadata.song.title) :
                            EditorManager.inst.loadedLevels.OrderByDescending(x => x.metadata.song.title)).ToList();
                        break;
                    }
                case 5:
                    {
                        EditorManager.inst.loadedLevels = (levelAscend ? EditorManager.inst.loadedLevels.OrderBy(x => x.metadata.song.difficulty) :
                            EditorManager.inst.loadedLevels.OrderByDescending(x => x.metadata.song.difficulty)).ToList();
                        break;
                    }
                case 6:
                    {
                        EditorManager.inst.loadedLevels = (levelAscend ? EditorManager.inst.loadedLevels.OrderBy(x => x.metadata.beatmap.date_edited) :
                            EditorManager.inst.loadedLevels.OrderByDescending(x => x.metadata.beatmap.date_edited)).ToList();
                        break;
                    }
                case 7:
                    {
                        EditorManager.inst.loadedLevels = (levelAscend ? EditorManager.inst.loadedLevels.OrderBy(x => ((MetaData)x.metadata).LevelBeatmap.date_created) :
                            EditorManager.inst.loadedLevels.OrderByDescending(x => ((MetaData)x.metadata).LevelBeatmap.date_created)).ToList();
                        break;
                    }
            }

            #endregion

            int num = 0;
            foreach (var metadataWrapper in EditorManager.inst.loadedLevels)
            {
                var folder = metadataWrapper.folder;
                var metadata = metadataWrapper.metadata;

                string[] difficultyNames = new string[]
                {
                    "easy",
                    "normal",
                    "hard",
                    "expert",
                    "expert+",
                    "master",
                    "animation",
                    "Unknown difficulty",
                };

                string difficultyName = difficultyNames[Mathf.Clamp(metadata.song.difficulty, 0, difficultyNames.Length - 1)];

                ((EditorWrapper)metadataWrapper).SetActive((RTFile.FileExists(folder + "/level.ogg") ||
                    RTFile.FileExists(folder + "/level.wav") ||
                    RTFile.FileExists(folder + "/level.mp3")) && RTHelpers.SearchString(Path.GetFileName(folder), EditorManager.inst.openFileSearch) ||
                        RTHelpers.SearchString(metadata.song.title, EditorManager.inst.openFileSearch) ||
                        RTHelpers.SearchString(metadata.artist.Name, EditorManager.inst.openFileSearch) ||
                        RTHelpers.SearchString(metadata.creator.steam_name, EditorManager.inst.openFileSearch) ||
                        RTHelpers.SearchString(metadata.song.description, EditorManager.inst.openFileSearch) ||
                        RTHelpers.SearchString(difficultyName, EditorManager.inst.openFileSearch));

                ((EditorWrapper)metadataWrapper).GameObject.transform.SetSiblingIndex(num);
                num++;
            }

            if (ModCompatibility.sharedFunctions.ContainsKey("EditorLevelFolders"))
                ModCompatibility.sharedFunctions["EditorLevelFolders"] = levelItems;
            else ModCompatibility.sharedFunctions.Add("EditorLevelFolders", levelItems);

            yield break;
        }

        public void RefreshParentSearch(EditorManager __instance, BeatmapObject beatmapObject)
        {
            var transform = __instance.GetDialog("Parent Selector").Dialog.Find("mask/content");

            foreach (object obj2 in transform)
            {
                Destroy(((Transform)obj2).gameObject);
            }

            var gameObject = Instantiate(__instance.folderButtonPrefab);
            gameObject.name = "No Parent";
            gameObject.transform.SetParent(transform);
            gameObject.transform.localScale = Vector3.one;
            gameObject.transform.GetChild(0).GetComponent<Text>().text = "No Parent";
            gameObject.GetComponent<Button>().onClick.AddListener(delegate ()
            {
                ObjEditor.inst.SetParent("");
                EditorManager.inst.HideDialog("Parent Selector");
            });

            if (string.IsNullOrEmpty(__instance.parentSearch) || "camera".Contains(__instance.parentSearch.ToLower()))
            {
                var cam = __instance.folderButtonPrefab.Duplicate(transform, "Camera");
                cam.transform.GetChild(0).GetComponent<Text>().text = "Camera";
                cam.GetComponent<Button>().onClick.AddListener(delegate ()
                {
                    beatmapObject.parent = "CAMERA_PARENT";
                    Updater.UpdateProcessor(beatmapObject);
                    EditorManager.inst.HideDialog("Parent Selector");
                    StartCoroutine(ObjectEditor.RefreshObjectGUI(beatmapObject));
                });
            }

            foreach (var obj in DataManager.inst.gameData.beatmapObjects)
            {
                if (!obj.fromPrefab)
                {
                    int num = DataManager.inst.gameData.beatmapObjects.IndexOf(obj);
                    if ((string.IsNullOrEmpty(__instance.parentSearch) || (obj.name + " " + num.ToString("0000")).ToLower().Contains(__instance.parentSearch.ToLower())) && obj.id != beatmapObject.id)
                    {
                        bool flag = true;
                        if (!string.IsNullOrEmpty(obj.parent))
                        {
                            string parentID = beatmapObject.id;
                            while (!string.IsNullOrEmpty(parentID))
                            {
                                if (parentID == obj.parent)
                                {
                                    flag = false;
                                    break;
                                }
                                int num2 = DataManager.inst.gameData.beatmapObjects.FindIndex(x => x.parent == parentID);
                                if (num2 != -1)
                                {
                                    parentID = DataManager.inst.gameData.beatmapObjects[num2].id;
                                }
                                else
                                {
                                    parentID = null;
                                }
                            }
                        }
                        if (flag)
                        {
                            string s = $"{obj.name} {num.ToString("0000")}";
                            var gameObject2 = __instance.folderButtonPrefab.Duplicate(transform, s);
                            gameObject2.transform.GetChild(0).GetComponent<Text>().text = s;
                            gameObject2.GetComponent<Button>().onClick.AddListener(delegate ()
                            {
                                string id = obj.id;
                                beatmapObject.parent = id;
                                Updater.UpdateProcessor(beatmapObject);
                                EditorManager.inst.HideDialog("Parent Selector");
                                StartCoroutine(ObjectEditor.RefreshObjectGUI(beatmapObject));
                                Debug.Log($"{__instance.className}Set Parent ID: {id}");
                            });
                        }
                    }
                }
            }
        }

        public void OpenPropertiesWindow(bool _toggle = false)
        {
            if (EditorManager.inst)
            {
                if (!EditorManager.inst.GetDialog("Editor Properties Popup").Dialog.gameObject.activeSelf)
                {
                    EditorManager.inst.ShowDialog("Editor Properties Popup");
                    RenderPropertiesWindow();
                }
                else if (_toggle)
                {
                    EditorManager.inst.HideDialog("Editor Properties Popup");
                }
            }
        }

        public void ClosePropertiesWindow() => EditorManager.inst.HideDialog("Editor Properties Popup");

        public List<Color> categoryColors = new List<Color>
        {
            LSColors.HexToColor("FFE7E7"),
            LSColors.HexToColor("C0ACE1"),
            LSColors.HexToColor("F17BB8"),
            LSColors.HexToColor("2F426D"),
            LSColors.HexToColor("4076DF"),
            LSColors.HexToColor("6CCBCF"),
            LSColors.HexToColor("1B1B1C")

        };

        public void RenderPropertiesWindow()
        {
            var editorDialog = EditorManager.inst.GetDialog("Editor Properties Popup").Dialog;
            var label = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content").transform.GetChild(3).gameObject;
            var singleInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move/position/x");
            var vector2Input = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move/position");
            var boolInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/toggle/toggle");
            var dropdownInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/autokill/tod-dropdown");
            var sliderFullInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/bpm");
            var stringInput = defaultIF;

            LSHelpers.DeleteChildren(editorDialog.Find("mask/content"));

            foreach (var prop in EditorProperties)
            {
                if (currentCategory == prop.propCategory && (string.IsNullOrEmpty(propertiesSearch) || prop.name.ToLower().Contains(propertiesSearch.ToLower())))
                {
                    switch (prop.valueType)
                    {
                        case EditorProperty.ValueType.Bool:
                            {
                                var bar = Instantiate(singleInput);
                                Destroy(bar.GetComponent<InputField>());
                                Destroy(bar.GetComponent<EventInfo>());
                                Destroy(bar.GetComponent<EventTrigger>());

                                LSHelpers.DeleteChildren(bar.transform);
                                bar.transform.SetParent(editorDialog.Find("mask/content"));
                                bar.transform.localScale = Vector3.one;
                                bar.name = "input [BOOL]";

                                TooltipHelper.AddTooltip(bar, prop.name, prop.description, new List<string> { prop.configEntry.BoxedValue.GetType().ToString() });

                                var l = Instantiate(label);
                                l.transform.SetParent(bar.transform);
                                l.transform.localScale = Vector3.one;
                                var text = l.transform.GetChild(0).GetComponent<Text>();
                                text.alignment = TextAnchor.MiddleLeft;
                                text.text = prop.name;
                                l.transform.AsRT().sizeDelta = new Vector2(688f, 32f);

                                l.transform.GetChild(0).AsRT().anchoredPosition = new Vector2(10f, -5f);
                                l.transform.GetChild(0).AsRT().sizeDelta = new Vector2(688f, 32f);

                                var image = bar.GetComponent<Image>();
                                image.enabled = true;
                                image.color = new Color(1f, 1f, 1f, 0.03f);

                                var x = Instantiate(boolInput);
                                x.transform.SetParent(bar.transform);
                                x.transform.localScale = Vector3.one;

                                Toggle xt = x.GetComponent<Toggle>();
                                xt.onValueChanged.RemoveAllListeners();
                                xt.isOn = (bool)prop.configEntry.BoxedValue;
                                xt.onValueChanged.AddListener(delegate (bool _val)
                                {
                                    prop.configEntry.BoxedValue = _val;
                                });
                                break;
                            }
                        case EditorProperty.ValueType.Int:
                            {
                                GameObject x = Instantiate(singleInput);
                                x.transform.SetParent(editorDialog.Find("mask/content"));
                                x.name = "input [INT]";

                                Destroy(x.GetComponent<EventInfo>());
                                Destroy(x.GetComponent<EventTrigger>());
                                Destroy(x.GetComponent<InputField>());

                                x.transform.localScale = Vector3.one;
                                x.transform.GetChild(0).localScale = Vector3.one;

                                var l = Instantiate(label);
                                l.transform.SetParent(x.transform);
                                l.transform.SetAsFirstSibling();
                                l.transform.localScale = Vector3.one;
                                var text = l.transform.GetChild(0).GetComponent<Text>();
                                text.alignment = TextAnchor.MiddleLeft;
                                text.text = prop.name;
                                l.transform.GetChild(0).AsRT().sizeDelta = new Vector2(434.4f, 32f);
                                l.transform.AsRT().sizeDelta = new Vector2(541f, 32f);

                                l.transform.GetChild(0).AsRT().anchoredPosition = new Vector2(10f, -5f);

                                var image = x.GetComponent<Image>();
                                image.enabled = true;
                                image.color = new Color(1f, 1f, 1f, 0.03f);

                                TooltipHelper.AddTooltip(x, prop.name, prop.description, new List<string> { prop.configEntry.BoxedValue.GetType().ToString() });

                                var input = x.transform.Find("input");

                                var xif = input.gameObject.AddComponent<InputField>();
                                xif.onValueChanged.RemoveAllListeners();
                                xif.textComponent = input.Find("Text").GetComponent<Text>();
                                xif.placeholder = input.Find("Placeholder").GetComponent<Text>();
                                xif.characterValidation = InputField.CharacterValidation.Integer;
                                xif.text = prop.configEntry.BoxedValue.ToString();
                                xif.onValueChanged.AddListener(delegate (string _val)
                                {
                                    prop.configEntry.BoxedValue = int.Parse(_val);
                                });

                                if (prop.configEntry.Description.AcceptableValues != null)
                                {
                                    int min = int.MinValue;
                                    int max = int.MaxValue;
                                    min = (int)prop.configEntry.Description.AcceptableValues.Clamp(min);
                                    max = (int)prop.configEntry.Description.AcceptableValues.Clamp(max);

                                    TriggerHelper.AddEventTrigger(xif.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDeltaInt(xif, 1, min, max) });

                                    TriggerHelper.IncreaseDecreaseButtonsInt(xif, 1, min, max, x.transform);
                                }
                                else
                                {
                                    TriggerHelper.AddEventTrigger(xif.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDeltaInt(xif) });

                                    TriggerHelper.IncreaseDecreaseButtonsInt(xif, t: x.transform);
                                }

                                break;
                            }
                        case EditorProperty.ValueType.Float:
                            {
                                GameObject x = Instantiate(singleInput);
                                x.transform.SetParent(editorDialog.Find("mask/content"));
                                x.name = "input [FLOAT]";

                                Destroy(x.GetComponent<EventInfo>());
                                Destroy(x.GetComponent<EventTrigger>());
                                Destroy(x.GetComponent<InputField>());

                                x.transform.localScale = Vector3.one;
                                x.transform.GetChild(0).localScale = Vector3.one;

                                var l = Instantiate(label);
                                l.transform.SetParent(x.transform);
                                l.transform.SetAsFirstSibling();
                                l.transform.localScale = Vector3.one;
                                var text = l.transform.GetChild(0).GetComponent<Text>();
                                text.alignment = TextAnchor.MiddleLeft;
                                text.text = prop.name;
                                l.transform.GetChild(0).AsRT().sizeDelta = new Vector2(434.4f, 32f);
                                l.transform.AsRT().sizeDelta = new Vector2(541f, 32f);

                                l.transform.GetChild(0).AsRT().anchoredPosition = new Vector2(10f, -5f);

                                var image = x.GetComponent<Image>();
                                image.enabled = true;
                                image.color = new Color(1f, 1f, 1f, 0.03f);

                                TooltipHelper.AddTooltip(x, prop.name, prop.description, new List<string> { prop.configEntry.BoxedValue.GetType().ToString() });

                                var input = x.transform.Find("input");

                                var xif = input.gameObject.AddComponent<InputField>();
                                xif.onValueChanged.RemoveAllListeners();
                                xif.textComponent = input.Find("Text").GetComponent<Text>();
                                xif.placeholder = input.Find("Placeholder").GetComponent<Text>();
                                xif.characterValidation = InputField.CharacterValidation.None;
                                xif.text = prop.configEntry.BoxedValue.ToString();
                                xif.onValueChanged.AddListener(delegate (string _val)
                                {
                                    prop.configEntry.BoxedValue = float.Parse(_val);
                                });

                                if (prop.configEntry.Description.AcceptableValues != null)
                                {
                                    float min = float.MinValue;
                                    float max = float.MaxValue;
                                    min = (float)prop.configEntry.Description.AcceptableValues.Clamp(min);
                                    max = (float)prop.configEntry.Description.AcceptableValues.Clamp(max);

                                    TriggerHelper.AddEventTrigger(xif.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(xif, 0.1f, 10f, min, max, false) });

                                    TriggerHelper.IncreaseDecreaseButtons(xif, 1f, 10f, min, max, x.transform);
                                }
                                else
                                {
                                    TriggerHelper.AddEventTrigger(xif.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(xif) });

                                    TriggerHelper.IncreaseDecreaseButtons(xif, t: x.transform);
                                }

                                break;
                            }
                        case EditorProperty.ValueType.IntSlider:
                            {
                                GameObject x = Instantiate(sliderFullInput);
                                x.transform.SetParent(editorDialog.Find("mask/content"));
                                x.transform.localScale = Vector3.one;
                                x.name = "input [INTSLIDER]";

                                var title = x.transform.Find("title");

                                var l = title.GetComponent<Text>();
                                l.font = FontManager.inst.Inconsolata;
                                l.text = prop.name;

                                var titleRT = title.GetComponent<RectTransform>();
                                titleRT.sizeDelta = new Vector2(220f, 32f);
                                titleRT.anchoredPosition = new Vector2(122f, -16f);

                                var image = x.AddComponent<Image>();
                                image.color = new Color(1f, 1f, 1f, 0.03f);

                                TooltipHelper.AddTooltip(x, prop.name, prop.description, new List<string> { prop.configEntry.BoxedValue.GetType().ToString() });

                                x.transform.Find("slider").GetComponent<RectTransform>().sizeDelta = new Vector2(295f, 32f);
                                var xsli = x.transform.Find("slider").GetComponent<Slider>();
                                xsli.onValueChanged.RemoveAllListeners();

                                xsli.value = (int)prop.configEntry.BoxedValue * 10;

                                xsli.maxValue = 100f;
                                xsli.minValue = -100f;

                                var xif = x.transform.Find("input").GetComponent<InputField>();
                                xif.onValueChanged.RemoveAllListeners();
                                xif.characterValidation = InputField.CharacterValidation.Integer;
                                xif.text = prop.configEntry.BoxedValue.ToString();

                                xif.onValueChanged.AddListener(delegate (string _val)
                                {
                                    if (LSHelpers.IsUsingInputField())
                                    {
                                        prop.configEntry.BoxedValue = float.Parse(_val);
                                        xsli.value = int.Parse(_val);
                                    }
                                });

                                xsli.onValueChanged.AddListener(delegate (float _val)
                                {
                                    if (!LSHelpers.IsUsingInputField())
                                    {
                                        prop.configEntry.BoxedValue = _val;
                                        xif.text = _val.ToString();
                                    }
                                });

                                if (prop.configEntry.Description.AcceptableValues != null)
                                {
                                    int min = int.MinValue;
                                    int max = int.MaxValue;
                                    min = (int)prop.configEntry.Description.AcceptableValues.Clamp(min);
                                    max = (int)prop.configEntry.Description.AcceptableValues.Clamp(max);

                                    TriggerHelper.AddEventTrigger(xif.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDeltaInt(xif, 1, min, max, false) });

                                    TriggerHelper.IncreaseDecreaseButtonsInt(xif, 1, min, max, x.transform);
                                }
                                else
                                {
                                    TriggerHelper.AddEventTrigger(xif.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDeltaInt(xif) });

                                    TriggerHelper.IncreaseDecreaseButtonsInt(xif, t: x.transform);
                                }

                                break;
                            }
                        case EditorProperty.ValueType.FloatSlider:
                            {
                                GameObject x = Instantiate(sliderFullInput);
                                x.transform.SetParent(editorDialog.Find("mask/content"));
                                x.transform.localScale = Vector3.one;
                                x.name = "input [FLOATSLIDER]";

                                var title = x.transform.Find("title");

                                var l = title.GetComponent<Text>();
                                l.font = FontManager.inst.Inconsolata;
                                l.text = prop.name;

                                x.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;

                                var titleRT = title.GetComponent<RectTransform>();
                                titleRT.sizeDelta = new Vector2(220f, 32f);
                                titleRT.anchoredPosition = new Vector2(122f, -16f);

                                var image = x.AddComponent<Image>();
                                image.color = new Color(1f, 1f, 1f, 0.03f);

                                TooltipHelper.AddTooltip(x, prop.name, prop.description, new List<string> { prop.configEntry.BoxedValue.GetType().ToString() });

                                x.transform.Find("slider").GetComponent<RectTransform>().sizeDelta = new Vector2(295f, 32f);
                                var xsli = x.transform.Find("slider").GetComponent<Slider>();
                                xsli.onValueChanged.RemoveAllListeners();

                                xsli.value = (float)prop.configEntry.BoxedValue * 10;

                                xsli.maxValue = 100f;
                                xsli.minValue = -100f;

                                var xif = x.transform.Find("input").GetComponent<InputField>();
                                xif.onValueChanged.RemoveAllListeners();
                                xif.characterValidation = InputField.CharacterValidation.None;
                                xif.text = prop.configEntry.BoxedValue.ToString();

                                xif.onValueChanged.AddListener(delegate (string _val)
                                {
                                    if (LSHelpers.IsUsingInputField())
                                    {
                                        prop.configEntry.BoxedValue = float.Parse(_val);
                                        int v = int.Parse(_val) * 10;
                                        xsli.value = v;
                                    }
                                });

                                xsli.onValueChanged.AddListener(delegate (float _val)
                                {
                                    if (!LSHelpers.IsUsingInputField())
                                    {
                                        int v = (int)_val * 10;
                                        float v2 = v / 100f;
                                        prop.configEntry.BoxedValue = v2;
                                        xif.text = v2.ToString();
                                    }
                                });

                                if (prop.configEntry.Description.AcceptableValues != null)
                                {
                                    float min = float.MinValue;
                                    float max = float.MaxValue;
                                    min = (float)prop.configEntry.Description.AcceptableValues.Clamp(min);
                                    max = (float)prop.configEntry.Description.AcceptableValues.Clamp(max);

                                    TriggerHelper.AddEventTrigger(xif.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(xif, 0.1f, 10f, min, max, false) });

                                    TriggerHelper.IncreaseDecreaseButtons(xif, 1f, 10f, min, max, x.transform);
                                }
                                else
                                {
                                    TriggerHelper.AddEventTrigger(xif.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(xif) });

                                    TriggerHelper.IncreaseDecreaseButtons(xif, t: x.transform);
                                }

                                break;
                            }
                        case EditorProperty.ValueType.String:
                            {
                                var bar = Instantiate(singleInput);

                                Destroy(bar.GetComponent<EventInfo>());
                                Destroy(bar.GetComponent<EventTrigger>());
                                Destroy(bar.GetComponent<InputField>());
                                Destroy(bar.GetComponent<InputFieldSwapper>());

                                LSHelpers.DeleteChildren(bar.transform);
                                bar.transform.SetParent(editorDialog.Find("mask/content"));
                                bar.transform.localScale = Vector3.one;
                                bar.name = "input [STRING]";

                                TooltipHelper.AddTooltip(bar, prop.name, prop.description, new List<string> { prop.configEntry.BoxedValue.GetType().ToString() });

                                var l = Instantiate(label);
                                l.transform.SetParent(bar.transform);
                                l.transform.localScale = Vector3.one;
                                var text = l.transform.GetChild(0).GetComponent<Text>();
                                text.alignment = TextAnchor.MiddleLeft;
                                text.text = prop.name;
                                l.transform.GetChild(0).AsRT().sizeDelta = new Vector2(434.4f, 32f);
                                l.transform.AsRT().sizeDelta = new Vector2(354f, 32f);

                                l.transform.GetChild(0).AsRT().anchoredPosition = new Vector2(10f, -5f);

                                var image = bar.GetComponent<Image>();
                                image.enabled = true;
                                image.color = new Color(1f, 1f, 1f, 0.03f);

                                GameObject x = Instantiate(stringInput);
                                x.transform.SetParent(bar.transform);
                                x.transform.localScale = Vector3.one;
                                Destroy(x.GetComponent<HoverTooltip>());

                                x.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(366f, 32f);

                                var xif = x.GetComponent<InputField>();
                                xif.onValueChanged.RemoveAllListeners();
                                xif.characterValidation = InputField.CharacterValidation.None;
                                xif.characterLimit = 0;
                                xif.text = prop.configEntry.BoxedValue.ToString();
                                xif.textComponent.fontSize = 18;
                                xif.onValueChanged.AddListener(delegate (string _val)
                                {
                                    prop.configEntry.BoxedValue = _val;
                                });

                                break;
                            }
                        case EditorProperty.ValueType.Vector2:
                            {
                                var bar = Instantiate(singleInput);

                                Destroy(bar.GetComponent<EventInfo>());
                                Destroy(bar.GetComponent<EventTrigger>());
                                Destroy(bar.GetComponent<InputField>());
                                Destroy(bar.GetComponent<InputFieldSwapper>());

                                LSHelpers.DeleteChildren(bar.transform);
                                bar.transform.SetParent(editorDialog.Find("mask/content"));
                                bar.transform.localScale = Vector3.one;
                                bar.name = "input [VECTOR2]";

                                TooltipHelper.AddTooltip(bar, prop.name, prop.description, new List<string> { prop.configEntry.BoxedValue.GetType().ToString() });

                                var l = Instantiate(label);
                                l.transform.SetParent(bar.transform);
                                l.transform.localScale = Vector3.one;
                                var text = l.transform.GetChild(0).GetComponent<Text>();
                                text.alignment = TextAnchor.MiddleLeft;
                                text.text = prop.name;
                                l.transform.GetChild(0).AsRT().sizeDelta = new Vector2(434.4f, 32f);
                                l.transform.AsRT().sizeDelta = new Vector2(354f, 32f);

                                l.transform.GetChild(0).AsRT().anchoredPosition = new Vector2(10f, -5f);

                                var image = bar.GetComponent<Image>();
                                image.enabled = true;
                                image.color = new Color(1f, 1f, 1f, 0.03f);

                                GameObject vector2 = Instantiate(vector2Input);
                                vector2.transform.SetParent(bar.transform);
                                vector2.transform.localScale = Vector3.one;

                                Vector2 vtmp = (Vector2)prop.configEntry.BoxedValue;

                                Destroy(vector2.transform.Find("x").GetComponent<EventInfo>());
                                vector2.transform.Find("x").localScale = Vector3.one;
                                vector2.transform.Find("x").GetChild(0).localScale = Vector3.one;
                                var vxif = vector2.transform.Find("x").GetComponent<InputField>();
                                {
                                    vxif.onValueChanged.RemoveAllListeners();

                                    vxif.text = vtmp.x.ToString();

                                    vxif.onValueChanged.AddListener(delegate (string _val)
                                    {
                                        vtmp = new Vector2(float.Parse(_val), vtmp.y);
                                        prop.configEntry.BoxedValue = vtmp;
                                    });
                                }

                                Destroy(vector2.transform.Find("y").GetComponent<EventInfo>());
                                vector2.transform.Find("y").localScale = Vector3.one;
                                vector2.transform.Find("x").GetChild(0).localScale = Vector3.one;
                                var vyif = vector2.transform.Find("y").GetComponent<InputField>();
                                {
                                    vyif.onValueChanged.RemoveAllListeners();

                                    vyif.text = vtmp.y.ToString();

                                    vyif.onValueChanged.AddListener(delegate (string _val)
                                    {
                                        vtmp = new Vector2(vtmp.x, float.Parse(_val));
                                        prop.configEntry.BoxedValue = vtmp;
                                    });
                                }

                                TriggerHelper.AddEventTrigger(vxif.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(vxif) });
                                TriggerHelper.AddEventTrigger(vyif.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(vyif) });

                                TriggerHelper.IncreaseDecreaseButtons(vxif);
                                TriggerHelper.IncreaseDecreaseButtons(vyif);

                                break;
                            }
                        case EditorProperty.ValueType.Vector3:
                            {
                                Debug.Log("lol");
                                break;
                            }
                        case EditorProperty.ValueType.Enum:
                            {
                                bool isKeyCode = prop.configEntry.GetType() == typeof(ConfigEntry<KeyCode>);

                                var bar = Instantiate(singleInput);

                                Destroy(bar.GetComponent<EventInfo>());
                                Destroy(bar.GetComponent<EventTrigger>());
                                Destroy(bar.GetComponent<InputField>());
                                Destroy(bar.GetComponent<InputFieldSwapper>());

                                LSHelpers.DeleteChildren(bar.transform);
                                bar.transform.SetParent(editorDialog.Find("mask/content"));
                                bar.transform.localScale = Vector3.one;
                                bar.name = "input [ENUM]";

                                TooltipHelper.AddTooltip(bar, prop.name, prop.description + " (You may see some Invalid Values, don't worry nothing's wrong.)", new List<string> { prop.configEntry.BoxedValue.GetType().ToString() });

                                var l = Instantiate(label);
                                l.transform.SetParent(bar.transform);
                                l.transform.localScale = Vector3.one;
                                var text = l.transform.GetChild(0).GetComponent<Text>();
                                text.alignment = TextAnchor.MiddleLeft;
                                text.text = prop.name;
                                l.transform.GetChild(0).AsRT().sizeDelta = new Vector2(434.4f, 32f);
                                l.transform.AsRT().sizeDelta = new Vector2(isKeyCode ? 482 : 522f, 32f);

                                l.transform.GetChild(0).AsRT().anchoredPosition = new Vector2(10f, -5f);

                                var im = bar.GetComponent<Image>();
                                im.enabled = true;
                                im.color = new Color(1f, 1f, 1f, 0.03f);

                                if (isKeyCode)
                                {
                                    var gameObject = new GameObject("selector");
                                    gameObject.transform.SetParent(bar.transform);
                                    gameObject.transform.localScale = Vector3.one;
                                    var rectTransform = gameObject.AddComponent<RectTransform>();
                                    gameObject.AddComponent<CanvasRenderer>();

                                    rectTransform.sizeDelta = new Vector2(32f, 32f);

                                    var image = gameObject.AddComponent<Image>();

                                    var button = gameObject.AddComponent<Button>();
                                    button.onClick.AddListener(delegate ()
                                    {
                                        selectingKey = true;
                                        setKey = delegate (KeyCode key)
                                        {
                                            prop.configEntry.BoxedValue = key;
                                        };

                                        onKeySet = RenderPropertiesWindow;
                                    });
                                }

                                GameObject x = Instantiate(dropdownInput);
                                x.transform.SetParent(bar.transform);
                                x.transform.localScale = Vector3.one;

                                RectTransform xRT = x.GetComponent<RectTransform>();
                                //xRT.anchoredPosition = ConfigEntries.OpenFileDropdownPosition.Value;

                                Destroy(x.GetComponent<HoverTooltip>());

                                var hide = x.GetComponent<HideDropdownOptions>();
                                hide.DisabledOptions.Clear();

                                Dropdown dropdown = x.GetComponent<Dropdown>();
                                dropdown.options.Clear();
                                dropdown.onValueChanged.RemoveAllListeners();
                                Type type = prop.configEntry.SettingType;

                                var enums = Enum.GetValues(prop.configEntry.SettingType);
                                for (int i = 0; i < enums.Length; i++)
                                {
                                    var str = "Invalid Value";
                                    if (Enum.GetName(prop.configEntry.SettingType, i) != null)
                                    {
                                        hide.DisabledOptions.Add(false);
                                        str = Enum.GetName(prop.configEntry.SettingType, i);
                                    }
                                    else
                                    {
                                        hide.DisabledOptions.Add(true);
                                    }

                                    dropdown.options.Add(new Dropdown.OptionData(str));
                                }

                                dropdown.value = (int)prop.configEntry.BoxedValue;
                                dropdown.onValueChanged.AddListener(delegate (int _val)
                                {
                                    prop.configEntry.BoxedValue = _val;
                                });

                                break;
                            }
                        case EditorProperty.ValueType.Color:
                            {
                                var bar = Instantiate(singleInput);

                                Destroy(bar.GetComponent<EventInfo>());
                                Destroy(bar.GetComponent<EventTrigger>());
                                Destroy(bar.GetComponent<InputField>());
                                Destroy(bar.GetComponent<InputFieldSwapper>());

                                LSHelpers.DeleteChildren(bar.transform);
                                bar.transform.SetParent(editorDialog.Find("mask/content"));
                                bar.transform.localScale = Vector3.one;
                                bar.name = "input [COLOR]";

                                TooltipHelper.AddTooltip(bar, prop.name, prop.description, new List<string> { prop.configEntry.BoxedValue.GetType().ToString() });

                                var l = Instantiate(label);
                                l.transform.SetParent(bar.transform);
                                l.transform.localScale = Vector3.one;
                                var text = l.transform.GetChild(0).GetComponent<Text>();
                                text.alignment = TextAnchor.MiddleLeft;
                                text.text = prop.name;
                                l.transform.GetChild(0).AsRT().sizeDelta = new Vector2(434.4f, 32f);
                                l.transform.AsRT().sizeDelta = new Vector2(314f, 32f);

                                l.transform.GetChild(0).AsRT().anchoredPosition = new Vector2(10f, -5f);

                                var im = bar.GetComponent<Image>();
                                im.enabled = true;
                                im.color = new Color(1f, 1f, 1f, 0.03f);

                                var bar2 = Instantiate(singleInput);
                                Destroy(bar2.GetComponent<InputField>());
                                Destroy(bar2.GetComponent<EventInfo>());
                                LSHelpers.DeleteChildren(bar2.transform);
                                bar2.transform.SetParent(bar.transform);
                                bar2.transform.localScale = Vector3.one;
                                bar2.name = "color";
                                bar2.GetComponent<RectTransform>().sizeDelta = new Vector2(32f, 32f);

                                var bar2Color = bar2.GetComponent<Image>();
                                bar2Color.enabled = true;
                                bar2Color.color = (Color)prop.configEntry.BoxedValue;

                                Image image2 = null;

                                if (EventEditor.inst.dialogLeft.TryFind("theme/theme/viewport/content/gui/preview/dropper", out Transform dropper))
                                {
                                    var drop = Instantiate(dropper.gameObject);
                                    drop.transform.SetParent(bar2.transform);
                                    drop.transform.localScale = Vector3.one;
                                    drop.name = "dropper";

                                    var dropRT = drop.GetComponent<RectTransform>();
                                    dropRT.sizeDelta = new Vector2(32f, 32f);
                                    dropRT.anchoredPosition = Vector2.zero;

                                    if (drop.TryGetComponent(out Image image))
                                    {
                                        image2 = image;
                                        image.color = RTHelpers.InvertColorHue(RTHelpers.InvertColorValue((Color)prop.configEntry.BoxedValue));
                                    }
                                }

                                GameObject x = Instantiate(stringInput);
                                x.transform.SetParent(bar.transform);
                                x.transform.localScale = Vector3.one;
                                Destroy(x.GetComponent<HoverTooltip>());

                                x.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(366f, 32f);

                                var xif = x.GetComponent<InputField>();
                                xif.onValueChanged.RemoveAllListeners();
                                xif.characterValidation = InputField.CharacterValidation.None;
                                xif.characterLimit = 8;
                                xif.text = RTHelpers.ColorToHex((Color)prop.configEntry.BoxedValue);
                                xif.textComponent.fontSize = 18;
                                xif.onValueChanged.AddListener(delegate (string _val)
                                {
                                    string v = _val;
                                    if (v.Length == 6)
                                        v += "FF";

                                    if (v.Length == 8)
                                    {
                                        prop.configEntry.BoxedValue = LSColors.HexToColorAlpha(v);
                                        bar2Color.color = (Color)prop.configEntry.BoxedValue;
                                        if (image2 != null)
                                        {
                                            image2.color = RTHelpers.InvertColorHue(RTHelpers.InvertColorValue((Color)prop.configEntry.BoxedValue));
                                        }

                                        TriggerHelper.AddEventTrigger(bar2, new List<EventTrigger.Entry> { TriggerHelper.CreatePreviewClickTrigger(bar2Color, image2, xif, (Color)prop.configEntry.BoxedValue, "Editor Properties Popup") });
                                    }
                                });

                                TriggerHelper.AddEventTrigger(bar2, new List<EventTrigger.Entry> { TriggerHelper.CreatePreviewClickTrigger(bar2Color, image2, xif, (Color)prop.configEntry.BoxedValue, "Editor Properties Popup") });

                                break;
                            }
                        case EditorProperty.ValueType.Function:
                            {
                                var x = Instantiate(singleInput);
                                x.transform.SetParent(editorDialog.Find("mask/content"));
                                x.name = "input [FUNCTION]";

                                Destroy(x.GetComponent<EventInfo>());
                                Destroy(x.GetComponent<EventTrigger>());
                                DestroyImmediate(x.GetComponent<InputField>());

                                x.transform.localScale = Vector3.one;
                                x.transform.GetChild(0).localScale = Vector3.one;

                                var l = Instantiate(label);
                                l.transform.SetParent(x.transform);
                                l.transform.SetAsFirstSibling();
                                l.transform.localScale = Vector3.one;
                                var text = l.transform.GetChild(0).GetComponent<Text>();
                                text.alignment = TextAnchor.MiddleLeft;
                                text.text = prop.name;
                                l.transform.AsRT().sizeDelta = new Vector2(541f, 32f);

                                l.transform.GetChild(0).AsRT().anchoredPosition = new Vector2(10f, -5f);
                                l.transform.GetChild(0).AsRT().sizeDelta = new Vector2(541, 32f);

                                var image = x.GetComponent<Image>();
                                image.enabled = true;
                                image.color = new Color(1f, 1f, 1f, 0.03f);

                                try
                                {
                                    if (!string.IsNullOrEmpty(prop.description))
                                        TooltipHelper.AddTooltip(x, prop.name, prop.description, new List<string> { "Function" });
                                }
                                catch (Exception ex)
                                {
                                    Debug.LogError(ex);
                                }

                                Destroy(x.transform.Find("input").gameObject);
                                Destroy(x.transform.Find("<").gameObject);
                                Destroy(x.transform.Find(">").gameObject);

                                var button = x.AddComponent<Button>();
                                button.onClick.AddListener(delegate ()
                                {
                                    prop.action?.Invoke();
                                });

                                break;
                            }
                    }
                }
            }
        }

        public void RefreshFileBrowserLevels()
        {
            if (RTFileBrowser.inst)
            {
                RTFileBrowser.inst.UpdateBrowser(RTFile.ApplicationDirectory, ".lsb", "level",  x => StartCoroutine(LoadLevel(x.Replace("\\", "/").Replace("/level.lsb", ""))));
            }
        }

        public void RefreshDocumentation()
        {
            if (documentations.Count > 0)
                foreach (var document in documentations)
                {
                    if (string.IsNullOrEmpty(documentationSearch) || document.Name.ToLower().Contains(documentationSearch.ToLower()))
                    {
                        document.PopupButton?.SetActive(true);
                        if (document.PopupButton && document.PopupButton.TryGetComponent(out Button button))
                        {
                            button.onClick.ClearAll();
                            button.onClick.AddListener(delegate ()
                            {
                                SelectDocumentation(document);
                            });
                        }
                    }
                    else
                    {
                        document.PopupButton?.SetActive(false);
                    }
                }
        }

        public void SelectDocumentation(Document document)
        {
            documentationTitle.text = $"- {document.Name} -";

            var singleInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move/position/x");
            var label = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content").transform.GetChild(3).gameObject;

            LSHelpers.DeleteChildren(documentationContent);

            int num = 0;
            foreach (var element in document.elements)
            {
                switch (element.type)
                {
                    case Document.Element.Type.Text:
                        {
                            if (element.Data is not string || string.IsNullOrEmpty((string)element.Data))
                                break;

                            var bar = Instantiate(singleInput);
                            DestroyImmediate(bar.GetComponent<InputField>());
                            DestroyImmediate(bar.GetComponent<EventInfo>());
                            DestroyImmediate(bar.GetComponent<EventTrigger>());

                            LSHelpers.DeleteChildren(bar.transform);
                            bar.transform.SetParent(documentationContent);
                            bar.transform.localScale = Vector3.one;
                            bar.name = "element";
                            bar.transform.AsRT().sizeDelta = new Vector2(722f, 22f * LSText.WordWrap((string)element.Data, 67).Count);

                            var l = Instantiate(label);
                            l.transform.SetParent(bar.transform);
                            l.transform.localScale = Vector3.one;
                            var text = l.transform.GetChild(0).GetComponent<Text>();
                            text.text = (string)element.Data;
                            text.alignment = TextAnchor.UpperLeft;

                            l.transform.AsRT().sizeDelta = new Vector2(722f, 22f);
                            l.transform.GetChild(0).AsRT().sizeDelta = new Vector2(722f, 22f);

                            l.transform.GetChild(0).AsRT().anchoredPosition = new Vector2(10f, -5f);

                            var barImage = bar.GetComponent<Image>();
                            barImage.enabled = true;
                            barImage.color = new Color(1f, 1f, 1f, 0.03f);

                            if (element.Function != null)
                            {
                                bar.AddComponent<Button>().onClick.AddListener(() => element.Function.Invoke());
                            }

                            break;
                        }
                    case Document.Element.Type.Image:
                        {
                            if (element.Data is not string)
                                break;

                            var bar = Instantiate(singleInput);
                            LSHelpers.DeleteChildren(bar.transform);
                            DestroyImmediate(bar.GetComponent<InputField>());
                            DestroyImmediate(bar.GetComponent<EventInfo>());
                            DestroyImmediate(bar.GetComponent<EventTrigger>());
                            DestroyImmediate(bar.GetComponent<HorizontalLayoutGroup>());

                            bar.transform.SetParent(documentationContent);
                            bar.transform.localScale = Vector3.one;
                            bar.name = "element";

                            var imageObj = bar.Duplicate(bar.transform, "image");
                            imageObj.transform.AsRT().anchoredPosition = Vector2.zero;

                            LSHelpers.DeleteChildren(imageObj.transform);

                            var barImage = bar.GetComponent<Image>();
                            barImage.enabled = true;
                            barImage.color = new Color(1f, 1f, 1f, 0.03f);

                            var imageObjImage = imageObj.GetComponent<Image>();
                            imageObjImage.enabled = true;
                            imageObjImage.color = new Color(1f, 1f, 1f, 1f);

                            if (RTFile.FileExists($"{RTFile.ApplicationDirectory}{(string)element.Data}"))
                                imageObjImage.sprite = SpriteManager.LoadSprite($"{RTFile.ApplicationDirectory}{(string)element.Data}");
                            else
                                imageObjImage.enabled = false;

                            if (imageObjImage.sprite && imageObjImage.sprite.texture)
                            {
                                var width = Mathf.Clamp(imageObjImage.sprite.texture.width, 0, 718);
                                bar.transform.AsRT().sizeDelta = new Vector2(width, imageObjImage.sprite.texture.height);
                                imageObj.transform.AsRT().sizeDelta = new Vector2(width, imageObjImage.sprite.texture.height);
                            }

                            if (element.Function != null)
                            {
                                bar.AddComponent<Button>().onClick.AddListener(() => element.Function.Invoke());
                            }

                            break;
                        }
                }

                // Spacer
                if (num != document.elements.Count - 1)
                {
                    var bar = Instantiate(singleInput);
                    Destroy(bar.GetComponent<InputField>());
                    Destroy(bar.GetComponent<EventInfo>());
                    Destroy(bar.GetComponent<EventTrigger>());

                    LSHelpers.DeleteChildren(bar.transform);
                    bar.transform.SetParent(documentationContent);
                    bar.transform.localScale = Vector3.one;
                    bar.name = "spacer";
                    bar.transform.AsRT().sizeDelta = new Vector2(764f, 2f);

                    var barImage = bar.GetComponent<Image>();
                    barImage.enabled = true;
                    barImage.color = new Color(1f, 1f, 1f, 0.75f);
                }
                num++;
            }

            EditorManager.inst.ShowDialog("Documentation Dialog");
        }

        public void RefreshDebugger()
        {
            var parent = EditorManager.inst.GetDialog("Debugger Popup").Dialog.transform.Find("mask/content");
            
            for (int i = 0; i < debugs.Count; i++)
            {
                var current = parent.GetChild(i);

                current.gameObject.SetActive(string.IsNullOrEmpty(debugSearch) || debugs[i].ToLower().Contains(debugSearch.ToLower()));
            }
        }

        public void RefreshAutosaveList(EditorWrapper editorWrapper)
        {
            autosaveSearchField.onValueChanged.ClearAll();
            autosaveSearchField.onValueChanged.AddListener(delegate (string _value)
            {
                autosaveSearch = _value;
                RefreshAutosaveList(editorWrapper);
            });

            var backupPrefab = timelineBar.transform.Find("event");

            var buttonHoverSize = GetEditorProperty("Open Level Button Hover Size").GetConfigEntry<float>().Value;

            LSHelpers.DeleteChildren(autosaveContent);

            var files = Directory.GetFiles(editorWrapper.folder, "autosave_*.lsb", SearchOption.AllDirectories).Union(Directory.GetFiles(editorWrapper.folder, "backup_*.lsb", SearchOption.AllDirectories));

            foreach (var file in files)
            {
                var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(autosaveContent, $"Folder [{Path.GetFileName(file)}]");

                var hoverUI = gameObject.AddComponent<HoverUI>();
                hoverUI.size = buttonHoverSize;
                hoverUI.animatePos = false;
                hoverUI.animateSca = true;

                var text = gameObject.transform.GetChild(0).GetComponent<Text>();

                text.text = Path.GetFileName(file);

                var button = gameObject.GetComponent<Button>();
                button.onClick.ClearAll();
                button.onClick.AddListener(delegate ()
                {
                    StartCoroutine(LoadLevel(editorWrapper.folder, file.Replace("\\", "/").Replace(editorWrapper.folder + "/", "")));
                    EditorManager.inst.HideDialog("Open File Popup");
                });

                string tmpFile = file;

                var backup = backupPrefab.gameObject.Duplicate(gameObject.transform, "backup");
                backup.transform.localScale = Vector3.one;
                backup.transform.AsRT().anchoredPosition = new Vector2(450f, -16f);
                backup.transform.AsRT().sizeDelta = new Vector2(80f, 28f);
                var backupText = backup.transform.GetChild(0).GetComponent<Text>();
                backupText.text = "Backup";
                backupText.color = new Color(0.1098f, 0.1098f, 0.1137f, 1f);
                var backupButton = backup.GetComponent<Button>();
                ((Image)backupButton.targetGraphic).color = new Color(0.3922f, 0.7098f, 0.9647f, 1f);
                backupButton.onClick.ClearAll();
                backupButton.onClick.AddListener(delegate ()
                {
                    var fi = new FileInfo(tmpFile);

                    tmpFile = tmpFile.Contains("autosave_") ? tmpFile.Replace("autosave_", "backup_") : tmpFile.Replace("backup_", "autosave_");

                    if (fi.Exists)
                    {
                        fi.MoveTo(tmpFile);
                    }

                    var fileName = Path.GetFileName(tmpFile);
                    text.text = fileName;
                    gameObject.name = $"Folder [{fileName}]";

                    button.onClick.ClearAll();
                    button.onClick.AddListener(delegate ()
                    {
                        StartCoroutine(LoadLevel(editorWrapper.folder, tmpFile.Replace("\\", "/").Replace(editorWrapper.folder + "/", "")));
                        EditorManager.inst.HideDialog("Open File Popup");
                    });
                });
            }
        }

        public void PlayDialogAnimation(string dialogName, bool active)
        {
            if (DialogAnimations.Has(x => x.name == dialogName) && EditorManager.inst.EditorDialogsDictionary[dialogName].Dialog.gameObject.activeSelf != active)
            {
                var dialogAnimation = DialogAnimations.Find(x => x.name == dialogName);
                var dialog = EditorManager.inst.EditorDialogsDictionary[dialogName].Dialog;

                var animation = new AnimationManager.Animation("Popup Open");
                animation.floatAnimations = new List<AnimationManager.Animation.AnimationObject<float>>
                {
                    new AnimationManager.Animation.AnimationObject<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, active ? dialogAnimation.posStart.x : dialogAnimation.posEnd.x, Ease.Linear),
                        new FloatKeyframe(active ? dialogAnimation.posXStartDuration : dialogAnimation.posXEndDuration, active ? dialogAnimation.posEnd.x : dialogAnimation.posStart.x, active ? Ease.GetEaseFunction(dialogAnimation.posXStartEase) : Ease.GetEaseFunction(dialogAnimation.posXEndEase)),
                        new FloatKeyframe(active ? dialogAnimation.posXStartDuration : dialogAnimation.posXEndDuration + 0.01f, active ? dialogAnimation.posEnd.x : dialogAnimation.posStart.x, Ease.Linear),
                    }, delegate (float x)
                    {
                        var pos = dialog.localPosition;
                        pos.x = x;
                        dialog.localPosition = pos;
                    }),
                    new AnimationManager.Animation.AnimationObject<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, active ? dialogAnimation.posStart.y : dialogAnimation.posEnd.y, Ease.Linear),
                        new FloatKeyframe(active ? dialogAnimation.posYStartDuration : dialogAnimation.posYEndDuration, active ? dialogAnimation.posEnd.y : dialogAnimation.posStart.y, active ? Ease.GetEaseFunction(dialogAnimation.posYStartEase) : Ease.GetEaseFunction(dialogAnimation.posYEndEase)),
                        new FloatKeyframe(active ? dialogAnimation.posYStartDuration : dialogAnimation.posYEndDuration + 0.01f, active ? dialogAnimation.posEnd.y : dialogAnimation.posStart.y, Ease.Linear),
                    }, delegate (float x)
                    {
                        var pos = dialog.localPosition;
                        pos.y = x;
                        dialog.localPosition = pos;
                    }),
                    new AnimationManager.Animation.AnimationObject<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, active ? dialogAnimation.scaStart.x : dialogAnimation.scaEnd.x, Ease.Linear),
                        new FloatKeyframe(active ? dialogAnimation.scaXStartDuration : dialogAnimation.scaXEndDuration, active ? dialogAnimation.scaEnd.x : dialogAnimation.scaStart.x, active ? Ease.GetEaseFunction(dialogAnimation.scaXStartEase) : Ease.GetEaseFunction(dialogAnimation.scaXEndEase)),
                        new FloatKeyframe(active ? dialogAnimation.scaXStartDuration : dialogAnimation.scaXEndDuration + 0.01f, active ? dialogAnimation.scaEnd.x : dialogAnimation.scaStart.x, Ease.Linear),
                    }, delegate (float x)
                    {
                        var pos = dialog.localScale;
                        pos.x = x;
                        dialog.localScale = pos;
                    }),
                    new AnimationManager.Animation.AnimationObject<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, active ? dialogAnimation.scaStart.y : dialogAnimation.scaEnd.y, Ease.Linear),
                        new FloatKeyframe(active ? dialogAnimation.scaYStartDuration : dialogAnimation.scaYEndDuration, active ? dialogAnimation.scaEnd.y : dialogAnimation.scaStart.y, active ? Ease.GetEaseFunction(dialogAnimation.scaYStartEase) : Ease.GetEaseFunction(dialogAnimation.scaYEndEase)),
                        new FloatKeyframe(active ? dialogAnimation.scaYStartDuration : dialogAnimation.scaYEndDuration + 0.01f, active ? dialogAnimation.scaEnd.y : dialogAnimation.scaStart.y, Ease.Linear),
                    }, delegate (float x)
                    {
                        var pos = dialog.localScale;
                        pos.y = x;
                        dialog.localScale = pos;
                    }),
                    new AnimationManager.Animation.AnimationObject<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, active ? dialogAnimation.rotStart : dialogAnimation.rotEnd, Ease.Linear),
                        new FloatKeyframe(active ? dialogAnimation.rotStartDuration : dialogAnimation.rotEndDuration, active ? dialogAnimation.rotEnd : dialogAnimation.rotStart, active ? Ease.GetEaseFunction(dialogAnimation.rotStartEase) : Ease.GetEaseFunction(dialogAnimation.rotEndEase)),
                        new FloatKeyframe(active ? dialogAnimation.rotStartDuration : dialogAnimation.rotEndDuration + 0.01f, active ? dialogAnimation.rotEnd : dialogAnimation.rotStart, Ease.Linear),
                    }, delegate (float x)
                    {
                        dialog.localRotation = Quaternion.Euler(0f, 0f, x);
                    }),
                };
                animation.id = LSText.randomNumString(16);

                animation.onComplete = delegate ()
                {
                    dialog.gameObject.SetActive(active);

                    dialog.localPosition = new Vector3(dialogAnimation.posEnd.x, dialogAnimation.posEnd.y, 0f);
                    dialog.localScale = new Vector3(dialogAnimation.scaEnd.x, dialogAnimation.scaEnd.y, 1f);
                    dialog.localRotation = Quaternion.Euler(0f, 0f, dialogAnimation.rotEnd);

                    AnimationManager.inst.RemoveID(animation.id);
                };

                AnimationManager.inst.Play(animation);
            }

            if (!DialogAnimations.Has(x => x.name == dialogName) || active)
                EditorManager.inst.EditorDialogsDictionary[dialogName].Dialog.gameObject.SetActive(active);
        }

        public void SetDialogStatus(string dialogName, bool active, bool focus = true)
        {
            if (EditorManager.inst.EditorDialogsDictionary.ContainsKey(dialogName))
            {
                PlayDialogAnimation(dialogName, active);

                if (active)
                {
                    if (focus)
                    {
                        EditorManager.inst.currentDialog = EditorManager.inst.EditorDialogsDictionary[dialogName];
                    }
                    if (!EditorManager.inst.ActiveDialogs.Contains(EditorManager.inst.EditorDialogsDictionary[dialogName]))
                    {
                        EditorManager.inst.ActiveDialogs.Add(EditorManager.inst.EditorDialogsDictionary[dialogName]);
                    }
                }
                else
                {
                    EditorManager.inst.ActiveDialogs.Remove(EditorManager.inst.EditorDialogsDictionary[dialogName]);
                    if (EditorManager.inst.currentDialog == EditorManager.inst.EditorDialogsDictionary[dialogName] && focus)
                    {
                        if (EditorManager.inst.ActiveDialogs.Count > 0)
                        {
                            EditorManager.inst.currentDialog = EditorManager.inst.ActiveDialogs.Last();
                            return;
                        }
                        EditorManager.inst.currentDialog = new EditorManager.EditorDialog();
                    }
                }
            }
            else
            {
                Debug.LogErrorFormat("{0}Can't load dialog [{1}].", new object[] { EditorManager.inst.className, dialogName });
            }
        }

        #endregion

        #region Misc Functions

        public static Color GetObjectColor(BaseBeatmapObject beatmapObject, bool ignoreTransparency)
        {
            if (beatmapObject.objectType == ObjectType.Empty)
                return Color.white;

            if (beatmapObject.TryGetGameObject(out GameObject gameObject) && gameObject.TryGetComponent(out Renderer renderer))
            {
                Color color = Color.white;
                if (AudioManager.inst.CurrentAudioSource.time < beatmapObject.StartTime)
                    color = GameManager.inst.LiveTheme.objectColors[(int)beatmapObject.events[3][0].eventValues[0]];
                else if (AudioManager.inst.CurrentAudioSource.time > beatmapObject.StartTime + beatmapObject.GetObjectLifeLength() && beatmapObject.autoKillType != AutoKillType.OldStyleNoAutokill)
                    color = GameManager.inst.LiveTheme.objectColors[(int)beatmapObject.events[3][beatmapObject.events[3].Count - 1].eventValues[0]];
                else if (renderer.material.HasProperty("_Color"))
                    color = renderer.material.color;
                if (ignoreTransparency)
                    color.a = 1f;
                return color;
            }

            return Color.white;
        }

        public static void DeleteLevelFunction(string _levelName)
        {
            if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + "recycling"))
            {
                Directory.CreateDirectory(RTFile.ApplicationDirectory + "recycling");
            }
            Directory.Move(RTFile.ApplicationDirectory + editorListSlash + _levelName, RTFile.ApplicationDirectory + "recycling/" + _levelName);

            string[] directories = Directory.GetDirectories(RTFile.ApplicationDirectory + "recycling", "*", SearchOption.AllDirectories);
            directories.ToList().Sort();
            foreach (var directory in directories)
            {
                string[] filesDir = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);
                filesDir.ToList().Sort();
            }
        }

        public static float SnapToBPM(float _time) => Mathf.RoundToInt(_time / (SettingEditor.inst.BPMMulti / BPMSnapDivisions)) * (SettingEditor.inst.BPMMulti / BPMSnapDivisions);

        #endregion

        #region Editor Properties

        public static AcceptableValueRange<int> FontSizeLimit { get; } = new AcceptableValueRange<int>(1, 40);
        public static AcceptableValueRange<float> HoverScaleLimit { get; } = new AcceptableValueRange<float>(0.7f, 1.4f);

        public static ConfigFile Config => EditorPlugin.inst.Config;

        public static EditorProperty GetEditorProperty(string name) => EditorProperties.Find(x => x.name == name);

        public static List<EditorProperty> EditorProperties => new List<EditorProperty>()
        {
            #region General

            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("General", "Debug", false, "If enabled, specific debugging functions for the editor will be enabled.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("General", "Editor Zen Mode", false, "If on, the player will not take damage in Preview Mode.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("General", "BPM Snaps Keyframes", false, "Makes object's keyframes snap if Snap BPM is enabled.")),
            new EditorProperty(EditorProperty.ValueType.Float,
                Config.Bind("General", "BPM Snap Divisions", 4f, "How many times the snap is divided into. Can be good for songs that don't do 4 divisions.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("General", "Round To Nearest", true, "If numbers should be rounded up to 3 decimal points (for example, 0.43321245 into 0.433).")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("General", "Prefab Example Template", true, "Example Template prefab will always be generated into the internal prefabs for you to use.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("General", "Paste Offset", false, "When enabled objects that are pasted will be pasted at an offset based on the distance between the audio time and the copied object. Otherwise, the objects will be pasted at the earliest objects start time.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("General", "Bring To Selection", false, "When an object is selected (whether it be a regular object, a marker, etc), it will move the layer and audio time to that object.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("General", "Create Objects at Camera Center", true, "When an object is created, its position will be set to that of the camera's.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("General", "Create Objects Scale Parent Default", true, "The default value for new Beatmap Objects' Scale Parent.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("General", "Allow Editor Keybinds With Editor Cam", true, "Allows keybinds to be used if EventsCore editor camera is on.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("General", "Rotation Event Keyframe Resets", true, "When an Event / Check rotation keyframe is created, it resets the value to 0.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("General", "Remember Last Keyframe Type", false, "When an object is selected for the first time, it selects the previous objects' keyframe selection type. For example, say you had a color keyframe selected, this newly selected object will select the first color keyframe.")),

            #endregion

            #region Timeline

            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Timeline", "Dragging main Cursor Pauses Level", true, "If dragging the cursor pauses the level.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Timeline", "Timeline Cursor Color", new Color(0.251f, 0.4627f, 0.8745f, 1f), "Color of the main timeline cursor.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Timeline", "Keyframe Cursor Color", new Color(0.251f, 0.4627f, 0.8745f, 1f), "Color of the object timeline cursor.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Timeline", "Object Selection Color", new Color(0.251f, 0.4627f, 0.8745f, 1f), "Color of selected objects.")),
            new EditorProperty(EditorProperty.ValueType.Vector2,
                Config.Bind("Timeline", "Main Zoom Bounds", new Vector2(16f, 512f), "The limits of the main timeline zoom.")),
            new EditorProperty(EditorProperty.ValueType.Vector2,
                Config.Bind("Timeline", "Keyframe Zoom Bounds", new Vector2(1f, 512f), "The limits of the keyframe timeline zoom.")),
            new EditorProperty(EditorProperty.ValueType.Float,
                Config.Bind("Timeline", "Main Zoom Amount", 0.05f, "Sets the zoom in & out amount for the main timeline.")),
            new EditorProperty(EditorProperty.ValueType.Float,
                Config.Bind("Timeline", "Keyframe Zoom Amount", 0.05f, "Sets the zoom in & out amount for the keyframe timeline.")),
            new EditorProperty(EditorProperty.ValueType.Float,
                Config.Bind("Timeline", "Keyframe End Length Offset", 2f, "Sets the amount of space you have after the last keyframe in an object.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Timeline", "Timeline Object Prefab Type Icon", true, "Shows the object's prefab type's icon.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Timeline", "Event Labels Render Left", false, "If the Event Layer labels should render on the left side or not.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Timeline", "Waveform Generate", true, "Allows the timeline waveform to generate. (Waveform might not show on some devices and will increase level load times)")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Timeline", "Waveform Re-render", false, "If the timeline waveform should update when a value is changed.")),
            new EditorProperty(EditorProperty.ValueType.Enum,
                Config.Bind("Timeline", "Waveform Mode", WaveformType.Legacy, "The mode of the timeline waveform.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Timeline", "Waveform BG Color", Color.clear, "Color of the background for the waveform.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Timeline", "Waveform Top Color", LSColors.red300, "If waveform mode is Legacy, this will be the top color. Otherwise, it will be the regular color.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Timeline", "Waveform Bottom Color", LSColors.blue300, "If waveform is Legacy, this will be the bottom color. Otherwise, it will be unused.")),
            new EditorProperty(EditorProperty.ValueType.Enum,
                Config.Bind("Timeline", "Waveform Texture Format", TextureFormat.ARGB32, "What format the waveform's texture should render under.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Timeline", "Timeline Grid Enabled", true, "If the timeline grid renders.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Timeline", "Timeline Grid Color", new Color(0.2157f, 0.2157f, 0.2196f, 1f), "The color of the timeline grid.")),
            new EditorProperty(EditorProperty.ValueType.Float,
                Config.Bind("Timeline", "Timeline Grid Thickness", 2f, "The size of each line of the timeline grid.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Timeline", "Marker Loop Active", false, "If the marker should loop between markers.")),
            new EditorProperty(EditorProperty.ValueType.Int,
                Config.Bind("Timeline", "Marker Loop Begin", 0, "Audio time gets set to this marker.")),
            new EditorProperty(EditorProperty.ValueType.Int,
                Config.Bind("Timeline", "Marker Loop End", 1, "If the audio time gets to the set marker time, it will loop to the beginning marker.")),

            #endregion

            #region Data

            new EditorProperty(EditorProperty.ValueType.Int,
                Config.Bind("Data", "Autosave Limit", 7, "If autosave count reaches this number, delete the first autosave.")),
            new EditorProperty(EditorProperty.ValueType.Float,
                Config.Bind("Data", "Autosave Loop Time", 600f, "The repeat time of autosave.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Data", "Level Loads Last Time", true, "Sets the editor position (audio time, layer, etc) to the last saved editor position on level load.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Data", "Level Pauses on Start", false, "Editor pauses on level load.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Data", "Saving Saves Beatmap Opacity", false, "Turn this off if you don't want themes to break in unmodded PA.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Data", "Update Prefab List on Files Changed", false, "When you add a prefab to your prefab path, the editor will automatically update the prefab list for you.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Data", "Update Theme List on Files Changed", false, "When you add a theme to your theme path, the editor will automatically update the theme list for you.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Data", "Show Levels Without Cover Notification", false, "Sends an error notification for what levels don't have covers.")),
            new EditorProperty(EditorProperty.ValueType.String,
                Config.Bind("Data", "Convert Level LS to VG Export Path", "", "The custom path to export a level to. If no path is set then it will export to beatmaps/exports.")),
            new EditorProperty(EditorProperty.ValueType.String,
                Config.Bind("Data", "Convert Prefab LS to VG Export Path", "", "The custom path to export a prefab to. If no path is set then it will export to beatmaps/exports.")),
            new EditorProperty(EditorProperty.ValueType.String,
                Config.Bind("Data", "Convert Theme LS to VG Export Path", "", "The custom path to export a prefab to. If no path is set then it will export to beatmaps/exports.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Data", "Theme Saves Indents", false, "If .lst files should save with multiple lines and indents.")),

            #endregion

            #region Editor GUI

            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Editor GUI", "Drag UI", false, "Specific UI popups can be dragged around (such as the parent selector, etc).")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Editor GUI", "Hover UI Play Sound", false, "Plays a sound when the hover UI element is hovered over.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Editor GUI", "Import Prefabs Directly", false, "When clicking on an External Prefab, instead of importing it directly it'll bring up a Prefab External View Dialog if this config is off.")),
            new EditorProperty(EditorProperty.ValueType.Float,
                Config.Bind("Editor GUI", "Notification Width", 221f, "Width of the notifications.")),
            new EditorProperty(EditorProperty.ValueType.Float,
                Config.Bind("Editor GUI", "Notification Size", 1f, "Total size of the notifications.")),
            new EditorProperty(EditorProperty.ValueType.Enum,
                Config.Bind("Editor GUI", "Notification Direction", Direction.Down, "Direction the notifications popup from.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Editor GUI", "Notifications Display", true, "If the notifications should display. Does not include the help box.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Editor GUI", "Adjust Position Inputs", true, "If position keyframe input fields should be adjusted so they're in a proper row rather than having Z Axis below X Axis without a label. Drawback with doing this is it makes the fields smaller than normal.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Editor GUI", "Hide Visual Elements When Object Is Empty", true, "If the Beatmap Object is empty, anything related to the visuals of the object doesn't show.")),
            new EditorProperty(EditorProperty.ValueType.Vector2,
                Config.Bind("Editor GUI", "Open Level Position", Vector2.zero, "The position of the Open Level popup.")),
            new EditorProperty(EditorProperty.ValueType.Vector2,
                Config.Bind("Editor GUI", "Open Level Scale", new Vector2(600f, 400f), "The size of the Open Level popup.")),
            new EditorProperty(EditorProperty.ValueType.Vector2,
                Config.Bind("Editor GUI", "Open Level Editor Path Pos", new Vector2(275f, 16f), "The position of the editor path input field.")),
            new EditorProperty(EditorProperty.ValueType.Float,
                Config.Bind("Editor GUI", "Open Level Editor Path Length", 104f, "The length of the editor path input field.")),
            new EditorProperty(EditorProperty.ValueType.Vector2,
                Config.Bind("Editor GUI", "Open Level List Refresh Position", new Vector2(330f, 432f), "The position of the refresh button.")),
            new EditorProperty(EditorProperty.ValueType.Vector2,
                Config.Bind("Editor GUI", "Open Level Toggle Position", new Vector2(600f, 16f), "The position of the descending toggle.")),
            new EditorProperty(EditorProperty.ValueType.Vector2,
                Config.Bind("Editor GUI", "Open Level Dropdown Position", new Vector2(501f, 416f), "The position of the sort dropdown.")),
            new EditorProperty(EditorProperty.ValueType.Vector2,
                Config.Bind("Editor GUI", "Open Level Cell Size", new Vector2(584f, 32f), "Size of each cell.")),
            new EditorProperty(EditorProperty.ValueType.Enum,
                Config.Bind("Editor GUI", "Open Level Cell Constraint Type", GridLayoutGroup.Constraint.FixedColumnCount, "How the cells are layed out.")),
            new EditorProperty(EditorProperty.ValueType.Int,
                Config.Bind("Editor GUI", "Open Level Cell Constraint Count", 1, "How many rows / columns there are, depending on Constraint Type.")),
            new EditorProperty(EditorProperty.ValueType.Vector2,
                Config.Bind("Editor GUI", "Open Level Cell Spacing", new Vector2(0f, 8f), "The space between each cell.")),
            new EditorProperty(EditorProperty.ValueType.Enum,
                Config.Bind("Editor GUI", "Open Level Text Horizontal Wrap", HorizontalWrapMode.Wrap, "Horizontal Wrap Mode of the folder button text.")),
            new EditorProperty(EditorProperty.ValueType.Enum,
                Config.Bind("Editor GUI", "Open Level Text Vertical Wrap", VerticalWrapMode.Truncate, "Vertical Wrap Mode of the folder button text.")),
            new EditorProperty(EditorProperty.ValueType.IntSlider,
                Config.Bind("Editor GUI", "Open Level Text Font Size", 20, new ConfigDescription("Font size of the folder button text.", FontSizeLimit))),

            new EditorProperty(EditorProperty.ValueType.Int,
                Config.Bind("Editor GUI", "Open Level Folder Name Max", 14, "Limited length of the folder name.")),
            new EditorProperty(EditorProperty.ValueType.Int,
                Config.Bind("Editor GUI", "Open Level Song Name Max", 22, "Limited length of the song name.")),
            new EditorProperty(EditorProperty.ValueType.Int,
                Config.Bind("Editor GUI", "Open Level Artist Name Max", 16, "Limited length of the artist name.")),
            new EditorProperty(EditorProperty.ValueType.Int,
                Config.Bind("Editor GUI", "Open Level Creator Name Max", 16, "Limited length of the creator name.")),
            new EditorProperty(EditorProperty.ValueType.Int,
                Config.Bind("Editor GUI", "Open Level Description Max", 16, "Limited length of the description.")),
            new EditorProperty(EditorProperty.ValueType.Int,
                Config.Bind("Editor GUI", "Open Level Date Max", 16, "Limited length of the date.")),
            new EditorProperty(EditorProperty.ValueType.String,
                Config.Bind("Editor GUI", "Open Level Text Formatting", ".  /{0} : {1} by {2}",
                    "The way the text is formatted for each level. {0} is folder, {1} is song, {2} is artist, {3} is creator, {4} is difficulty, {5} is description and {6} is last edited.")),

            new EditorProperty(EditorProperty.ValueType.FloatSlider,
                Config.Bind("Editor GUI", "Open Level Button Hover Size", 1f, new ConfigDescription("How big the button gets when hovered.", HoverScaleLimit))),
            new EditorProperty(EditorProperty.ValueType.Vector2,
                Config.Bind("Editor GUI", "Open Level Cover Position", new Vector2(-276f, 0f), "Position of the level cover.")),
            new EditorProperty(EditorProperty.ValueType.Vector2,
                Config.Bind("Editor GUI", "Open Level Cover Scale", new Vector2(26f, 26f), "Size of the level cover.")),

            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Editor GUI", "Changes Refresh Level List", false, "If the level list reloads whenever a change is made.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Editor GUI", "Open Level Show Delete Button", false, "Shows a delete button that can be used to move levels to a recycling folder.")),

            new EditorProperty(EditorProperty.ValueType.FloatSlider,
                Config.Bind("Editor GUI", "Timeline Object Hover Size", 1f, new ConfigDescription("How big the button gets when hovered.", HoverScaleLimit))),
            new EditorProperty(EditorProperty.ValueType.FloatSlider,
                Config.Bind("Editor GUI", "Keyframe Hover Size", 1f, new ConfigDescription("How big the button gets when hovered.", HoverScaleLimit))),
            new EditorProperty(EditorProperty.ValueType.FloatSlider,
                Config.Bind("Editor GUI", "Timeline Bar Buttons Hover Size", 1.05f, new ConfigDescription("How big the button gets when hovered.", HoverScaleLimit))),
            new EditorProperty(EditorProperty.ValueType.FloatSlider,
                Config.Bind("Editor GUI", "Prefab Button Hover Size", 1.05f, new ConfigDescription("How big the button gets when hovered.", HoverScaleLimit))),

            // Prefab Internal
            new EditorProperty(EditorProperty.ValueType.Vector2,
                Config.Bind("Editor GUI", "Prefab Internal Popup Pos", new Vector2(0f, -16f), "Position of the internal prefabs popup.")),
            new EditorProperty(EditorProperty.ValueType.Vector2,
                Config.Bind("Editor GUI", "Prefab Internal Popup Size", new Vector2(400f, -32f), "Scale of the internal prefabs popup.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Editor GUI", "Prefab Internal Horizontal Scroll", false, "If you can scroll left / right or not.")),
            new EditorProperty(EditorProperty.ValueType.Vector2,
                Config.Bind("Editor GUI", "Prefab Internal Cell Size", new Vector2(383f, 32f), "Size of each Prefab Item.")),
            new EditorProperty(EditorProperty.ValueType.Enum,
                Config.Bind("Editor GUI", "Prefab Internal Constraint Mode", GridLayoutGroup.Constraint.FixedColumnCount, "Which direction the prefab list goes.")),
            new EditorProperty(EditorProperty.ValueType.Int,
                Config.Bind("Editor GUI", "Prefab Internal Constraint", 1, "How many columns the prefabs are divided into.")),
            new EditorProperty(EditorProperty.ValueType.Vector2,
                Config.Bind("Editor GUI", "Prefab Internal Spacing", new Vector2(8f, 8f), "Distance between each Prefab Cell.")),
            new EditorProperty(EditorProperty.ValueType.Enum,
                Config.Bind("Editor GUI", "Prefab Internal Start Axis", GridLayoutGroup.Axis.Horizontal, "Start axis of the prefab list.")),
            new EditorProperty(EditorProperty.ValueType.Vector2,
                Config.Bind("Editor GUI", "Prefab Internal Delete Button Pos", new Vector2(367f, -16f), "Position of the Delete Button.")),
            new EditorProperty(EditorProperty.ValueType.Vector2,
                Config.Bind("Editor GUI", "Prefab Internal Delete Button Sca", new Vector2(32f, 32f), "Scale of the Delete Button.")),
            new EditorProperty(EditorProperty.ValueType.Enum,
                Config.Bind("Editor GUI", "Prefab Internal Name Horizontal Wrap", HorizontalWrapMode.Overflow, "If the text overflows into another line or keeps going.")),
            new EditorProperty(EditorProperty.ValueType.Enum,
                Config.Bind("Editor GUI", "Prefab Internal Name Vertical Wrap", VerticalWrapMode.Overflow, "If the text overflows into another line or keeps going.")),
            new EditorProperty(EditorProperty.ValueType.IntSlider,
                Config.Bind("Editor GUI", "Prefab Internal Name Font Size", 20, new ConfigDescription("Size of the text font.", FontSizeLimit))),
            new EditorProperty(EditorProperty.ValueType.Enum,
                Config.Bind("Editor GUI", "Prefab Internal Type Horizontal Wrap", HorizontalWrapMode.Overflow, "If the text overflows into another line or keeps going.")),
            new EditorProperty(EditorProperty.ValueType.Enum,
                Config.Bind("Editor GUI", "Prefab Internal Type Vertical Wrap", VerticalWrapMode.Overflow, "If the text overflows into another line or keeps going.")),
            new EditorProperty(EditorProperty.ValueType.IntSlider,
                Config.Bind("Editor GUI", "Prefab Internal Type Font Size", 20, new ConfigDescription("Size of the text font.", FontSizeLimit))),

            // Prefab External
            new EditorProperty(EditorProperty.ValueType.Vector2,
                Config.Bind("Editor GUI", "Prefab External Popup Pos", new Vector2(0f, -16f), "Position of the external prefabs popup.")),
            new EditorProperty(EditorProperty.ValueType.Vector2,
                Config.Bind("Editor GUI", "Prefab External Popup Size", new Vector2(400f, -32f), "Scale of the external prefabs popup.")),
            new EditorProperty(EditorProperty.ValueType.Vector2,
                Config.Bind("Editor GUI", "Prefab External Prefab Path Pos", new Vector2(325f, 15f), "Position of the prefab path input field.")),
            new EditorProperty(EditorProperty.ValueType.Float,
                Config.Bind("Editor GUI", "Prefab External Prefab Path Length", 150f, "Length of the prefab path input field.")),
            new EditorProperty(EditorProperty.ValueType.Vector2,
                Config.Bind("Editor GUI", "Prefab External Prefab Refresh Pos", new Vector2(210f, 450f), "Position of the prefab refresh button.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Editor GUI", "Prefab External Horizontal Scroll", false, "If you can scroll left / right or not.")),
            new EditorProperty(EditorProperty.ValueType.Vector2,
                Config.Bind("Editor GUI", "Prefab External Cell Size", new Vector2(383f, 32f), "Size of each Prefab Item.")),
            new EditorProperty(EditorProperty.ValueType.Enum,
                Config.Bind("Editor GUI", "Prefab External Constraint Mode", GridLayoutGroup.Constraint.FixedColumnCount, "Which direction the prefab list goes.")),
            new EditorProperty(EditorProperty.ValueType.Int,
                Config.Bind("Editor GUI", "Prefab External Constraint", 1, "How many columns the prefabs are divided into.")),
            new EditorProperty(EditorProperty.ValueType.Vector2,
                Config.Bind("Editor GUI", "Prefab External Spacing", new Vector2(8f, 8f), "Distance between each Prefab Cell.")),
            new EditorProperty(EditorProperty.ValueType.Enum,
                Config.Bind("Editor GUI", "Prefab External Start Axis", GridLayoutGroup.Axis.Horizontal, "Start axis of the prefab list.")),
            new EditorProperty(EditorProperty.ValueType.Vector2,
                Config.Bind("Editor GUI", "Prefab External Delete Button Pos", new Vector2(367f, -16f), "Position of the Delete Button.")),
            new EditorProperty(EditorProperty.ValueType.Vector2,
                Config.Bind("Editor GUI", "Prefab External Delete Button Sca", new Vector2(32f, 32f), "Scale of the Delete Button.")),
            new EditorProperty(EditorProperty.ValueType.Enum,
                Config.Bind("Editor GUI", "Prefab External Name Horizontal Wrap", HorizontalWrapMode.Overflow, "If the text overflows into another line or keeps going.")),
            new EditorProperty(EditorProperty.ValueType.Enum,
                Config.Bind("Editor GUI", "Prefab External Name Vertical Wrap", VerticalWrapMode.Overflow, "If the text overflows into another line or keeps going.")),
            new EditorProperty(EditorProperty.ValueType.IntSlider,
                Config.Bind("Editor GUI", "Prefab External Name Font Size", 20, new ConfigDescription("Size of the text font.", FontSizeLimit))),
            new EditorProperty(EditorProperty.ValueType.Enum,
                Config.Bind("Editor GUI", "Prefab External Type Horizontal Wrap", HorizontalWrapMode.Overflow, "If the text overflows into another line or keeps going.")),
            new EditorProperty(EditorProperty.ValueType.Enum,
                Config.Bind("Editor GUI", "Prefab External Type Vertical Wrap", VerticalWrapMode.Overflow, "If the text overflows into another line or keeps going.")),
            new EditorProperty(EditorProperty.ValueType.IntSlider,
                Config.Bind("Editor GUI", "Prefab External Type Font Size", 20, new ConfigDescription("Size of the text font.", FontSizeLimit))),

            #endregion

            #region Fields
            
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Fields", "Show Modified Colors", true, "Keyframe colors show any modifications done (such as hue, saturation and value).")),
            new EditorProperty(EditorProperty.ValueType.String,
                Config.Bind("Fields", "Theme Template Name", "New Theme", "Name of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template GUI", LSColors.white, "GUI Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template Tail", LSColors.white, "Tail Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template BG", LSColors.gray900, "BG Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template Player 1", LSColors.HexToColor("E57373"), "Player 1 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template Player 2", LSColors.HexToColor("64B5F6"), "Player 2 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template Player 3", LSColors.HexToColor("81C784"), "Player 3 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template Player 4", LSColors.HexToColor("FFB74D"), "Player 4 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template OBJ 1", LSColors.gray100, "OBJ 1 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template OBJ 2", LSColors.gray200, "OBJ 2 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template OBJ 3", LSColors.gray300, "OBJ 3 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template OBJ 4", LSColors.gray400, "OBJ 4 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template OBJ 5", LSColors.gray500, "OBJ 5 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template OBJ 6", LSColors.gray600, "OBJ 6 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template OBJ 7", LSColors.gray700, "OBJ 7 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template OBJ 8", LSColors.gray800, "OBJ 8 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template OBJ 9", LSColors.gray900, "OBJ 9 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template OBJ 10", LSColors.gray100, "OBJ 10 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template OBJ 11", LSColors.gray200, "OBJ 11 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template OBJ 12", LSColors.gray300, "OBJ 12 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template OBJ 13", LSColors.gray400, "OBJ 13 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template OBJ 14", LSColors.gray500, "OBJ 14 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template OBJ 15", LSColors.gray600, "OBJ 15 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template OBJ 16", LSColors.gray700, "OBJ 16 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template OBJ 17", LSColors.gray800, "OBJ 17 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template OBJ 18", LSColors.gray900, "OBJ 18 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template BG 1", LSColors.pink100, "BG 1 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template BG 2", LSColors.pink200, "BG 2 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template BG 3", LSColors.pink300, "BG 3 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template BG 4", LSColors.pink400, "BG 4 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template BG 5", LSColors.pink500, "BG 5 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template BG 6", LSColors.pink600, "BG 6 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template BG 7", LSColors.pink700, "BG 7 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template BG 8", LSColors.pink800, "BG 8 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template BG 9", LSColors.pink900, "BG 9 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template FX 1", LSColors.gray100, "FX 1 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template FX 2", LSColors.gray200, "FX 2 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template FX 3", LSColors.gray300, "FX 3 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template FX 4", LSColors.gray400, "FX 4 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template FX 5", LSColors.gray500, "FX 5 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template FX 6", LSColors.gray600, "FX 6 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template FX 7", LSColors.gray700, "FX 7 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template FX 8", LSColors.gray800, "FX 8 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template FX 9", LSColors.gray900, "FX 9 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template FX 10", LSColors.gray100, "FX 10 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template FX 11", LSColors.gray200, "FX 11 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template FX 12", LSColors.gray300, "FX 12 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template FX 13", LSColors.gray400, "FX 13 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template FX 14", LSColors.gray500, "FX 14 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template FX 15", LSColors.gray600, "FX 15 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template FX 16", LSColors.gray700, "FX 16 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template FX 17", LSColors.gray800, "FX 17 Color of the template theme.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Fields", "Theme Template FX 18", LSColors.gray900, "FX 18 Color of the template theme.")),

            #endregion

            #region Functions
            
            //new EditorProperty("Open Keybind Editor", EditorProperty.ValueType.Function, EditorProperty.EditorPropCategory.Functions,
            //    delegate ()
            //    {
            //        KeybindManager.inst.OpenPopup();
            //    }, "Opens Keybind list"),

            #endregion

            #region Preview

            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Preview", "Only Objects on Current Layer Visible", false, "If enabled, all objects not on current layer will be set to transparent")),
            new EditorProperty(EditorProperty.ValueType.Float,
                Config.Bind("Preview", "Visible object opacity", 0.2f, "Opacity of the objects not on the current layer.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Preview", "Show Empties", false, "If enabled, show all objects that are set to the empty object type.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Preview", "Only Show Damagable", false, "If enabled, only objects that can damage the player will be shown.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Preview", "Highlight Objects", true, "If enabled and if cursor hovers over an object, it will be highlighted.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Preview", "Object Highlight Amount", new Color(0.1f, 0.1f, 0.1f), "If an object is hovered, it adds this amount of color to the hovered object.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Preview", "Object Highlight Double Amount", new Color(0.5f, 0.5f, 0.5f), "If an object is hovered and shift is held, it adds this amount of color to the hovered object.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Preview", "Object Dragger Enabled", false, "If an object can be dragged around.")),
            new EditorProperty(EditorProperty.ValueType.Float,
                Config.Bind("Preview", "Object Dragger Rotator Radius", 22f, "The size of the Object Draggers' rotation ring.")),
            new EditorProperty(EditorProperty.ValueType.Float,
                Config.Bind("Preview", "Object Dragger Scaler Offset", 6f, "The distance of the Object Draggers' scale arrows.")),
            new EditorProperty(EditorProperty.ValueType.Float,
                Config.Bind("Preview", "Object Dragger Scaler Scale", 1.6f, "The size of the Object Draggers' scale arrows.")),

            #endregion
        };

        #endregion

        #region Constructors

        public static List<DialogAnimation> DialogAnimations { get; set; } = new List<DialogAnimation>
        {
            new DialogAnimation("Open File Popup")
            {
                posStart = Vector2.zero,
                posEnd = Vector2.zero,
                posXStartDuration = 0f,
                posXEndDuration = 0f,
                posXStartEase = "Linear",
                posXEndEase = "Linear",
                posYStartDuration = 0f,
                posYEndDuration = 0f,
                posYStartEase = "Linear",
                posYEndEase = "Linear",

                scaStart = Vector2.zero,
                scaEnd = Vector2.one,
                scaXStartDuration = 0.6f,
                scaXEndDuration = 0.2f,
                scaXStartEase = "OutElastic",
                scaXEndEase = "InCirc",
                scaYStartDuration = 0.6f,
                scaYEndDuration = 0.2f,
                scaYStartEase = "OutElastic",
                scaYEndEase = "InCirc",

                rotStart = 0f,
                rotEnd = 0f,
                rotStartDuration = 0f,
                rotEndDuration = 0f,
                rotStartEase = "Linear",
                rotEndEase = "Linear",
            },
            //"Open File Popup",
            //"New File Popup",
            //"Save As Popup",
            //"Quick Actions Popup",
            //"Parent Selector",
            //"Prefab Popup",
            //"Object Options Popup",
            //"BG Options Popup",
            //"Browser Popup",
            //"Object Search Popup",
            //"Warning Popup",
            //"REPL Editor Popup",
            //"Editor Properties Popup",
            //"Documentation Popup",
            //"Debugger Popup",
            //"Autosaves Popup",
            //"Default Modifiers Popup",
            //"Keybind List Popup",
        };

        public class DialogAnimation : Exists
        {
            public DialogAnimation(string name)
            {
                this.name = name;
            }

            public string name;

            public Vector2 posStart;
            public Vector2 posEnd;
            public float posXStartDuration;
            public float posXEndDuration;
            public string posXStartEase;
            public string posXEndEase;
            public float posYStartDuration;
            public float posYEndDuration;
            public string posYStartEase;
            public string posYEndEase;

            public Vector2 scaStart;
            public Vector2 scaEnd;
            public float scaXStartDuration;
            public float scaXEndDuration;
            public string scaXStartEase;
            public string scaXEndEase;
            public float scaYStartDuration;
            public float scaYEndDuration;
            public string scaYStartEase;
            public string scaYEndEase;

            public float rotStart;
            public float rotEnd;
            public float rotStartDuration;
            public float rotEndDuration;
            public string rotStartEase;
            public string rotEndEase;
        }

        public class EditorProperty : Exists
        {
            public EditorProperty()
            {
            }

            public EditorProperty(ValueType _valueType, ConfigEntryBase _configEntry)
            {
                name = _configEntry.Definition.Key;
                valueType = _valueType;
                propCategory = PropCategories.Find(x => x.ToString() == _configEntry.Definition.Section.Replace(" ", ""));
                configEntry = _configEntry;
                description = _configEntry.Description.Description;
            }
            
            public EditorProperty(ValueType _valueType, EditorPropCategory _editorProp, ConfigEntryBase _configEntry)
            {
                name = _configEntry.Definition.Key;
                valueType = _valueType;
                propCategory = _editorProp;
                configEntry = _configEntry;
                description = _configEntry.Description.Description;
            }
            
            public EditorProperty(string _name, ValueType _valueType, EditorPropCategory _editorProp, ConfigEntryBase _configEntry)
            {
                name = _name;
                valueType = _valueType;
                propCategory = _editorProp;
                configEntry = _configEntry;
                description = _configEntry.Description.Description;
            }
            
            public EditorProperty(string _name, ValueType _valueType, EditorPropCategory _editorProp, ConfigEntryBase _configEntry, string _description)
            {
                name = _name;
                valueType = _valueType;
                propCategory = _editorProp;
                configEntry = _configEntry;
                description = _description;
            }

            public EditorProperty(string _name, ValueType _valueType, EditorPropCategory _editorProp, Action action, string _description)
            {
                name = _name;
                valueType = _valueType;
                propCategory = _editorProp;
                description = _description;
                this.action = action;
            }

            public string name;
            public ValueType valueType;
            public EditorPropCategory propCategory;
            public ConfigEntryBase configEntry;
            public string description;
            public Action action;

            public ConfigEntry<T> GetConfigEntry<T>() => configEntry is ConfigEntry<T> ? (ConfigEntry<T>)configEntry : null;

            public List<EditorPropCategory> PropCategories => new List<EditorPropCategory>()
            {
                EditorPropCategory.General,
                EditorPropCategory.Timeline,
                EditorPropCategory.Data,
                EditorPropCategory.EditorGUI,
                EditorPropCategory.Functions,
                EditorPropCategory.Fields,
                EditorPropCategory.Preview,
            };

            public enum ValueType
            {
                Bool,
                Int,
                Float,
                IntSlider,
                FloatSlider,
                String,
                Vector2,
                Vector3,
                Enum,
                Color,
                Function
            }

            public enum EditorPropCategory
            {
                General,
                Timeline,
                Data,
                EditorGUI,
                Functions,
                Fields,
                Preview
            }
        }

        #endregion
    }
}
