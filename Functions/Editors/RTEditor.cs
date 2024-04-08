using BepInEx.Configuration;
using Crosstales.FB;
using EditorManagement.Functions.Components;
using EditorManagement.Functions.Helpers;
using HarmonyLib;
using LSFunctions;
using RTFunctions.Functions;
using RTFunctions.Functions.Animation;
using RTFunctions.Functions.Animation.Keyframe;
using RTFunctions.Functions.Components;
using RTFunctions.Functions.Components.Player;
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
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using AutoKillType = DataManager.GameData.BeatmapObject.AutoKillType;
using BaseBackgroundObject = DataManager.GameData.BackgroundObject;
using BaseBeatmapObject = DataManager.GameData.BeatmapObject;
using BaseEventKeyframe = DataManager.GameData.EventKeyframe;
using EventKeyframeSelection = EventEditor.KeyframeSelection;
using MetadataWrapper = EditorManager.MetadataWrapper;
using ObjectType = DataManager.GameData.BeatmapObject.ObjectType;

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
                if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + editorListPath))
                    Directory.CreateDirectory(RTFile.ApplicationDirectory + editorListPath);
                if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + prefabListPath))
                    Directory.CreateDirectory(RTFile.ApplicationDirectory + prefabListPath);
                if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + themeListPath))
                    Directory.CreateDirectory(RTFile.ApplicationDirectory + themeListPath);

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
            SetupNewFilePopup();
            CreatePreviewCover();
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
            
            // Level Modifiers Editor
            {
                var gameObject = new GameObject("LevelModifiersEditor");
                gameObject.transform.SetParent(GameObject.Find("Editor Systems").transform);
                gameObject.AddComponent<LevelModifiersEditor>();
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

            Action<string, UnityAction, UnityAction, string, string> showWarningPopup = delegate (string warning, UnityAction confirmDelegate, UnityAction cancelDelegate, string confirm, string cancel)
            {
                EditorManager.inst.ShowDialog("Warning Popup");
                RefreshWarningPopup(warning, confirmDelegate, cancelDelegate, confirm, cancel);
            };


            if (!ModCompatibility.sharedFunctions.ContainsKey("ShowWarningPopup"))
            {
                ModCompatibility.sharedFunctions.Add("ShowWarningPopup", showWarningPopup);
            }
            else
            {
                ModCompatibility.sharedFunctions["ShowWarningPopup"] = showWarningPopup;
            }

            if (ModCompatibility.sharedFunctions.ContainsKey("EventsCoreConfigs") && ModCompatibility.sharedFunctions["EventsCoreConfigs"] is List<ConfigEntryBase> configs)
            {
                foreach (var config in configs)
                {
                    if (otherProperties.Has(x => x.name == config.Definition.Key))
                        continue;

                    var editorProperty = EditorProperty.ValueType.Bool;

                    switch (config.Definition.Key)
                    {
                        case "Editor Camera Offset":
                        case "Players & GUI Active":
                        case "Show Intro":
                        case "Show Effects":
                        case "Editor Camera Use Keys":
                        case "Editor Camera Reset Values":
                            {
                                editorProperty = EditorProperty.ValueType.Bool;
                                break;
                            }
                        case "Editor Camera Speed":
                        case "Editor Camera Slow Speed":
                        case "Editor Camera Fast Speed":
                            {
                                editorProperty = EditorProperty.ValueType.Float;
                                break;
                            }
                        case "Editor Camera Toggle Key":
                        case "Players & GUI Toggle Key":
                        case "Shake Mode":
                            {
                                editorProperty = EditorProperty.ValueType.Enum;
                                break;
                            }
                    }

                    otherProperties.Add(new EditorProperty(editorProperty, config));
                }
            }
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
                    bool active = isCurrentLayer && (ShowModdedUI || timelineObject.Type < 10);

                    timelineObject.GameObject.SetActive(active);

                    if (active)
                    {
                        var color = EventEditor.inst.EventColors[timelineObject.Type % RTEventEditor.EventLimit];
                        color.a = 1f;

                        timelineObject.Image.color = timelineObject.selected ? EventEditor.inst.Selected : color;
                    }
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

            ModCompatibility.sharedFunctions.AddSet("SelectedObjects", ObjectEditor.inst.SelectedObjects);

            if (RTHelpers.AprilFools && UnityEngine.Random.Range(0, 10000) > 9996)
            {
                var array = new string[]
                {
                    "BRO",
                    "Go touch some grass.",
                    "Hello, hello? I wanted to record this message for you to get you settled in your first night. The animatronic characters DO get a bit quirky at night",
                    "",
                    "L + Ratio",
                    "Hi Diggy",
                    "Hi KarasuTori",
                    "Hi MoNsTeR",
                    "Hi RTMecha",
                    "Hi Example",
                    $"Hi {RTFunctions.FunctionsPlugin.displayName}!",
                    "Kweeble kweeble kweeble",
                    "Testing... is this thing on?",
                    "When life gives you lemons, don't make lemonade.",
                    "AMONGUS",
                    "I fear no man, but THAT thing, it scares me.",
                    "/summon minecraft:wither",
                    "Autobots, transform and roll out.",
                    "sands undertraveler",
                };

                EditorManager.inst.DisplayNotification(array[UnityEngine.Random.Range(0, array.Length)], 4f, EditorManager.NotificationType.Info);
            }
        }

        #region Variables

        public int currentLevelTemplate = -1;

        public float dragOffset = -1f;
        public int dragBinOffset = -100;

        public EditorThemeManager.Element PreviewCover { get; set; }

        public static bool DraggingPlaysSound { get; set; }
        public static bool DraggingPlaysSoundBPM { get; set; }
        public static bool ShowModdedUI { get; set; }

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

        Toggle layerToggle;
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

        public static List<Dropdown> EasingDropdowns { get; set; } = new List<Dropdown>();

        #endregion

        #region Settings

        public float bpmOffset = 0f;

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
            jn["misc"]["so"] = bpmOffset.ToString();
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
                if (jn["misc"]["t"] != null && EditorConfig.Instance.LevelLoadsLastTime.Value)
                    AudioManager.inst.SetMusicTime(jn["misc"]["t"].AsFloat);
                if (jn["misc"]["so"] != null)
                    bpmOffset = jn["misc"]["so"].AsFloat;
                else
                    bpmOffset = 0f;

                SettingEditor.inst.SnapBPM = DataManager.inst.metaData.song.BPM;
            }

            prevLayer = EditorManager.inst.layer;
            prevLayerType = layerType;

            SetTimelineGridSize();
        }

        #endregion

        #region Notifications

        public List<string> notifications = new List<string>();

        public void DisplayNotification(string name, string text, float time, EditorManager.NotificationType type)
        {
            StartCoroutine(DisplayNotificationLoop(name, text, time, type));
        }

        public void DisplayCustomNotification(string _name, string _text, float _time, Color _base, Color _top, Color _icCol, string _title, Sprite _icon = null)
        {
            StartCoroutine(DisplayCustomNotificationLoop(_name, _text, _time, _base, _top, _icCol, _title, _icon));
        }

        public void RebuildNotificationLayout()
        {
            try
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(EditorManager.inst.notification.transform.Find("info/text").AsRT());
                LayoutRebuilder.ForceRebuildLayoutImmediate(EditorManager.inst.notification.transform.Find("info").AsRT());
                LayoutRebuilder.ForceRebuildLayoutImmediate(EditorManager.inst.notification.transform.AsRT());
            }
            catch (Exception ex)
            {
                Debug.LogError($"{EditorPlugin.className}There was some sort of error with rebuilding the layout. {ex}");
            }
        }

        public IEnumerator DisplayNotificationLoop(string name, string text, float time, EditorManager.NotificationType type)
        {
            var config = EditorConfig.Instance;

            if (!config.Debug.Value)
                Debug.Log("<color=#F6AC1A>Editor</color><color=#2FCBD6>Management</color>\nNotification: " + name + "\nText: " + text + "\nTime: " + time + "\nType: " + type);

            if (!notifications.Contains(name) && notifications.Count < 20 && config.NotificationsDisplay.Value)
            {
                var notif = Instantiate(EditorManager.inst.notificationPrefabs[(int)type], Vector3.zero, Quaternion.identity);
                Destroy(notif, time);

                Graphic textComponent = type == EditorManager.NotificationType.Info ? notif.transform.Find("text").GetComponent<TextMeshProUGUI>() : notif.transform.Find("text").GetComponent<Text>();

                if (type == EditorManager.NotificationType.Info)
                    ((TextMeshProUGUI)textComponent).text = text;
                else
                    ((Text)textComponent).text = text;

                //if (type == EditorManager.NotificationType.Info)
                //    notif.transform.Find("text").GetComponent<TextMeshProUGUI>().text = text;
                //else
                //    notif.transform.Find("text").GetComponent<Text>().text = text;
                notif.transform.SetParent(EditorManager.inst.notification.transform);
                if (config.NotificationDirection.Value == Direction.Down)
                    notif.transform.SetAsFirstSibling();
                notif.transform.localScale = Vector3.one;

                EditorThemeManager.ApplyElement(new EditorThemeManager.Element("Tooltip Background", "Notification Background", notif, new List<Component>
                {
                    notif.GetComponent<Image>(),
                }, true, 1, SpriteManager.RoundedSide.W));

                var notifPanel = notif.transform.Find("bg/bg").gameObject;
                EditorThemeManager.ApplyElement(new EditorThemeManager.Element("Tooltip Panel", $"Notification {type}", notifPanel, new List<Component>
                {
                    notifPanel.GetComponent<Image>(),
                }, true, 1, SpriteManager.RoundedSide.Top));

                var notifText = notif.transform.Find("text").gameObject;
                EditorThemeManager.ApplyElement(new EditorThemeManager.Element("Tooltip Text", "Light Text", notifText, new List<Component>
                {
                    textComponent,
                }));

                var notifIcon = notif.transform.Find("bg/Image").gameObject;
                EditorThemeManager.ApplyElement(new EditorThemeManager.Element("Tooltip Icon", "Light Text", notifIcon, new List<Component>
                {
                    notifIcon.GetComponent<Image>(),
                }));

                var notifTitle = notif.transform.Find("bg/title").gameObject;
                EditorThemeManager.ApplyElement(new EditorThemeManager.Element("Tooltip Title", "Light Text", notifTitle, new List<Component>
                {
                    notifTitle.GetComponent<Text>(),
                }));

                RebuildNotificationLayout();

                notifications.Add(name);

                yield return new WaitForSeconds(time);
                notifications.Remove(name);
            }
            yield break;
        }

        public IEnumerator DisplayCustomNotificationLoop(string name, string text, float time, Color baseColor, Color topColor, Color iconCOlor, string _title, Sprite _icon = null)
        {
            var config = EditorConfig.Instance;

            if (!notifications.Contains(name) && notifications.Count < 20 && config.NotificationsDisplay.Value)
            {
                notifications.Add(name);
                var gameObject = Instantiate(EditorManager.inst.notificationPrefabs[0], Vector3.zero, Quaternion.identity);
                Destroy(gameObject, time);
                gameObject.transform.Find("text").GetComponent<TextMeshProUGUI>().text = text;
                gameObject.transform.SetParent(EditorManager.inst.notification.transform);
                if (config.NotificationDirection.Value == Direction.Down)
                    gameObject.transform.SetAsFirstSibling();
                gameObject.transform.localScale = Vector3.one;

                gameObject.GetComponent<Image>().color = baseColor;
                var bg = gameObject.transform.Find("bg");
                var img = bg.Find("Image").GetComponent<Image>();
                bg.Find("bg").GetComponent<Image>().color = topColor;
                if (_icon != null)
                    img.sprite = _icon;

                img.color = iconCOlor;
                bg.Find("title").GetComponent<Text>().text = _title;

                LayoutRebuilder.ForceRebuildLayoutImmediate(EditorManager.inst.notification.transform.Find("info/text").AsRT());
                LayoutRebuilder.ForceRebuildLayoutImmediate(EditorManager.inst.notification.transform.Find("info").AsRT());
                LayoutRebuilder.ForceRebuildLayoutImmediate(EditorManager.inst.notification.transform.AsRT());

                yield return new WaitForSeconds(time);
                notifications.Remove(name);
            }

            yield break;
        }

        public void SetupNotificationValues()
        {
            var config = EditorConfig.Instance;

            var notifyRT = EditorManager.inst.notification.GetComponent<RectTransform>();
            var notifyGroup = EditorManager.inst.notification.GetComponent<VerticalLayoutGroup>();
            notifyRT.sizeDelta = new Vector2(config.NotificationWidth.Value, 632f);
            EditorManager.inst.notification.transform.localScale =
                new Vector3(config.NotificationSize.Value, config.NotificationSize.Value, 1f);

            if (config.NotificationDirection.Value == Direction.Down)
            {
                notifyRT.anchoredPosition = new Vector2(8f, 408f);
                notifyGroup.childAlignment = TextAnchor.LowerLeft;
            }

            if (config.NotificationDirection.Value == Direction.Up)
            {
                notifyRT.anchoredPosition = new Vector2(8f, 410f);
                notifyGroup.childAlignment = TextAnchor.UpperLeft;
            }

            var tooltip = EditorManager.inst.tooltip.transform.parent.gameObject;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("Tooltip Background", "Notification Background", tooltip, new List<Component>
            {
                tooltip.GetComponent<Image>(),
            }, true, 1, SpriteManager.RoundedSide.W));

            var tooltipPanel = tooltip.transform.Find("bg/bg").gameObject;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("Tooltip Panel", "Notification Info", tooltipPanel, new List<Component>
            {
                tooltipPanel.GetComponent<Image>(),
            }, true, 1, SpriteManager.RoundedSide.Top));

            var tooltipText = tooltip.transform.Find("text").gameObject;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("Tooltip Text", "Light Text", tooltipText, new List<Component>
            {
                tooltipText.GetComponent<TextMeshProUGUI>(),
            }));

            var tooltipIcon = tooltip.transform.Find("bg/Image").gameObject;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("Tooltip Icon", "Light Text", tooltipIcon, new List<Component>
            {
                tooltipIcon.GetComponent<Image>(),
            }));

            var tooltipTitle = tooltip.transform.Find("bg/title").gameObject;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("Tooltip Title", "Light Text", tooltipTitle, new List<Component>
            {
                tooltipTitle.GetComponent<Text>(),
            }));
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

        public IEnumerator AssignTimelineTexture()
        {
            var config = EditorConfig.Instance;

            if ((!EditorManager.inst.hasLoadedLevel && !EditorManager.inst.loading && !RTFile.FileExists($"{RTFile.ApplicationDirectory}settings/waveform-{config.WaveformMode.Value.ToString().ToLower()}.png") ||
                !RTFile.FileExists(GameManager.inst.basePath + $"waveform-{config.WaveformMode.Value.ToString().ToLower()}.png")) && !config.WaveformRerender.Value || config.WaveformRerender.Value)
            {
                int num = Mathf.Clamp((int)AudioManager.inst.CurrentAudioSource.clip.length * 48, 100, 15000);
                Texture2D waveform = null;

                if (config.WaveformMode.Value == WaveformType.Legacy)
                    StartCoroutine(Legacy(AudioManager.inst.CurrentAudioSource.clip, num, 300, config.WaveformBGColor.Value, config.WaveformTopColor.Value, config.WaveformBottomColor.Value, delegate (Texture2D _tex) { waveform = _tex; }));
                if (config.WaveformMode.Value == WaveformType.Beta)
                    StartCoroutine(Beta(AudioManager.inst.CurrentAudioSource.clip, num, 300, config.WaveformBGColor.Value, config.WaveformTopColor.Value, delegate (Texture2D _tex) { waveform = _tex; }));
                if (config.WaveformMode.Value == WaveformType.BetaFast)
                    StartCoroutine(BetaFast(AudioManager.inst.CurrentAudioSource.clip, 1f, num, 300, config.WaveformBGColor.Value, config.WaveformTopColor.Value, delegate (Texture2D _tex) { waveform = _tex; }));
                if (config.WaveformMode.Value == WaveformType.LegacyFast)
                    StartCoroutine(LegacyFast(AudioManager.inst.CurrentAudioSource.clip, 1f, num, 300, config.WaveformBGColor.Value, config.WaveformTopColor.Value, config.WaveformBottomColor.Value, delegate (Texture2D _tex) { waveform = _tex; }));

                while (waveform == null)
                    yield return null;

                var waveSprite = Sprite.Create(waveform, new Rect(0f, 0f, (float)num, 300f), new Vector2(0.5f, 0.5f), 100f);
                TimelineImage.sprite = waveSprite;
                TimelineOverlayImage.sprite = TimelineImage.sprite;
            }
            else
            {
                var waveSprite = SpriteManager.LoadSprite(!EditorManager.inst.hasLoadedLevel && !EditorManager.inst.loading ?
                    $"{RTFile.ApplicationDirectory}settings/waveform-{config.WaveformMode.Value.ToString().ToLower()}.png" :
                    GameManager.inst.basePath + $"waveform-{config.WaveformMode.Value.ToString().ToLower()}.png");
                TimelineImage.sprite = waveSprite;
                TimelineOverlayImage.sprite = TimelineImage.sprite;
            }

            TimelineImage.sprite.Save(!EditorManager.inst.hasLoadedLevel && !EditorManager.inst.loading ?
                    $"{RTFile.ApplicationDirectory}settings/waveform-{config.WaveformMode.Value.ToString().ToLower()}.png" :
                    GameManager.inst.basePath + $"waveform-{config.WaveformMode.Value.ToString().ToLower()}.png");

            SetTimelineGridSize();

            yield break;
        }

        public IEnumerator Beta(AudioClip clip, int textureWidth, int textureHeight, Color background, Color waveform, Action<Texture2D> action)
        {
            Debug.LogFormat("{0}Generating Beta Waveform", EditorPlugin.className);
            int num = 100;
            var texture2D = new Texture2D(textureWidth, textureHeight, EditorConfig.Instance.WaveformTextureFormat.Value, false);
            var array = new Color[texture2D.width * texture2D.height];
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
            var texture2D = new Texture2D(textureWidth, textureHeight, EditorConfig.Instance.WaveformTextureFormat.Value, false);
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
            var tex = new Texture2D(width, height, EditorConfig.Instance.WaveformTextureFormat.Value, false);
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
            var tex = new Texture2D(width, height, EditorConfig.Instance.WaveformTextureFormat.Value, false);
            
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
                invertedColorSum += Color.white - color;

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
            var snapDivisions = EditorConfig.Instance.BPMSnapDivisions.Value * 2f;
            if (timelineGridRenderer && EditorManager.inst.Zoom > unrender && EditorConfig.Instance.TimelineGridEnabled.Value)
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
            if (RTFile.FileExists(EditorSettingsPath))
                return;

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
            var jn = JSON.Parse("{}");

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
            if (canUpdatePrefabs && EditorConfig.Instance.UpdatePrefabListOnFilesChanged.Value)
            {
                StartCoroutine(UpdatePrefabs());
            }
        }

        public void OnThemePathChanged(object sender, FileSystemEventArgs e)
        {
            if (canUpdateThemes && EditorConfig.Instance.UpdateThemeListOnFilesChanged.Value)
            {
                StartCoroutine(LoadThemes(EventEditor.inst.dialogRight.GetChild(4).gameObject.activeInHierarchy));
            }
        }

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
                    EditorManager.inst.DisplayNotification("Can't duplicate Checkpoint", 1f, EditorManager.NotificationType.Error, false);
                }
            }

            if (!isOverMainTimeline && EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Object))
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
                    EditorManager.inst.DisplayNotification("Can't duplicate Object Keyframe", 1f, EditorManager.NotificationType.Error, false);
                }
            }

            if (isOverMainTimeline && layerType == LayerType.Events)
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
                    EditorManager.inst.DisplayNotification("Can't duplicate Event Keyframe", 1f, EditorManager.NotificationType.Error, false);
                }
            }

            if (isOverMainTimeline && layerType == LayerType.Objects)
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
            if (isOverMainTimeline && layerType == LayerType.Objects)
            {
                PasteObject(_offsetTime, _regen);
            }

            if (isOverMainTimeline && layerType == LayerType.Events)
            {
                if (RTFile.FileExists($"{Application.persistentDataPath}/copied_events.lsev"))
                {
                    var jn = JSON.Parse(RTFile.ReadFromFile($"{Application.persistentDataPath}/copied_events.lsev"));

                    RTEventEditor.inst.copiedEventKeyframes.Clear();

                    for (int i = 0; i < GameData.EventTypes.Length; i++)
                    {
                        if (jn["events"][GameData.EventTypes[i]] != null)
                        {
                            for (int j = 0; j < jn["events"][GameData.EventTypes[i]].Count; j++)
                            {
                                var timelineObject = new TimelineObject(EventKeyframe.Parse(jn["events"][GameData.EventTypes[i]][j], i, GameData.DefaultKeyframes[i].eventValues.Length));
                                timelineObject.Type = i;
                                timelineObject.Index = j;
                                RTEventEditor.inst.copiedEventKeyframes.Add(timelineObject);
                            }
                        }
                    }
                }

                RTEventEditor.inst.PasteEvents();
                EditorManager.inst.DisplayNotification($"Pasted Event Keyframe{(RTEventEditor.inst.copiedEventKeyframes.Count > 1 ? "s" : "")}", 1f, EditorManager.NotificationType.Success);
            }

            if (!isOverMainTimeline && EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Object))
            {
                ObjEditor.inst.PasteKeyframes();
                EditorManager.inst.DisplayNotification($"Pasted Object Keyframe{(ObjectEditor.inst.copiedObjectKeyframes.Count > 1 ? "s" : "")}", 1f, EditorManager.NotificationType.Success);
            }

            if ((isOverMainTimeline && EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Checkpoint)) || EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Checkpoint))
            {
                CheckpointEditor.inst.PasteCheckpoint();
                EditorManager.inst.DisplayNotification("Pasted Checkpoint", 1f, EditorManager.NotificationType.Success);
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
                var jn = JSON.Parse(RTFile.ReadFromFile(Application.persistentDataPath + "/copied_objects.lsp"));

                pr = Prefab.Parse(jn);

                ObjEditor.inst.hasCopiedObject = true;
            }

            StartCoroutine(ObjectEditor.inst.AddPrefabExpandedToLevel(pr ?? (Prefab)ObjEditor.inst.beatmapObjCopy, true, _offsetTime, false, _regen));
        }

        public void Delete()
        {
            if (!isOverMainTimeline && EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Object))
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
                            StartCoroutine(ObjectEditor.inst.DeleteKeyframes());
                        }, delegate ()
                        {
                            ObjectEditor.inst.PasteKeyframes(beatmapObject, list, false);
                        }));

                        StartCoroutine(ObjectEditor.inst.DeleteKeyframes());
                    }
                else
                    EditorManager.inst.DisplayNotification("Can't delete first keyframe.", 1f, EditorManager.NotificationType.Error);
                return;
            }
            if (isOverMainTimeline && layerType == LayerType.Objects)
            {
                if (DataManager.inst.gameData.beatmapObjects.Count > 1 && ObjectEditor.inst.SelectedObjectCount != DataManager.inst.gameData.beatmapObjects.Count)
                {
                    var list = new List<TimelineObject>();
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                        list.Add(timelineObject);

                    EditorManager.inst.ClearDialogs(EditorManager.EditorDialog.DialogType.Object, EditorManager.EditorDialog.DialogType.Prefab);

                    float startTime = 0f;

                    var startTimeList = new List<float>();
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
                        StartCoroutine(ObjectEditor.inst.AddPrefabExpandedToLevel(prefab, true, 0f, true, retainID: true));
                    }));

                    StartCoroutine(ObjectEditor.inst.DeleteObjects());
                }
                else
                    EditorManager.inst.DisplayNotification("Can't delete only Beatmap Object", 1f, EditorManager.NotificationType.Error);
                return;
            }
            if (isOverMainTimeline && layerType == LayerType.Events)
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
                            ObjectEditor.inst.RenderTimelineObjectsPositions();

                            if (prevLayerType != layerType)
                            {
                                if (CheckpointEditor.inst.checkpoints.Count > 0)
                                {
                                    foreach (var obj2 in CheckpointEditor.inst.checkpoints)
                                        Destroy(obj2);

                                    CheckpointEditor.inst.checkpoints.Clear();
                                }

                                CheckpointEditor.inst.CreateGhostCheckpoints();
                            }

                            break;
                        }
                    case LayerType.Events:
                        {
                            RTEventEditor.inst.RenderEventObjects();
                            CheckpointEditor.inst.CreateCheckpoints();

                            RTEventEditor.inst.RenderLayerBins();

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
                    Debug.Log($"{EditorPlugin.className}Redone layer: {tmpLayer}");
                    SetLayer(tmpLayer, false);
                }, delegate ()
                {
                    Debug.Log($"{EditorPlugin.className}Undone layer: {oldLayer}");
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
                timelineBar.transform.Find(i.ToString()).SetParent(transform);

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

            var timeDefault = timelineBar.transform.GetChild(0).gameObject;
            timeDefault.name = "Time Default";

            var t = timelineBar.transform.Find("Time");
            defaultIF = t.gameObject;
            defaultIF.SetActive(true);
            t.SetParent(transform);
            __instance.speedText.transform.parent.SetParent(transform);

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
                layersObj.transform.SetSiblingIndex(7);
                layersObj.transform.localScale = Vector3.one;

                for (int i = 0; i < layersObj.transform.childCount; i++)
                {
                    layersObj.transform.GetChild(i).localScale = Vector3.one;
                }
                //layersObj.GetComponent<HoverTooltip>().tooltipLangauges.Add(Triggers.NewTooltip("Input any positive number to go to that editor layer.", "Layers will only show specific objects that are on that layer. Can be good to use for organizing levels.", new List<string> { "Middle Mouse Button" }));

                layersIF = layersObj.GetComponent<InputField>();
                layersIF.textComponent.alignment = TextAnchor.MiddleCenter;

                layersIF.text = GetLayerString(EditorManager.inst.layer);

                var layerImage = layersObj.GetComponent<Image>();

                layersObj.AddComponent<ContrastColors>().Init(layersIF.textComponent, layerImage);

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

            var timelineBarBase = timelineBar.transform.parent.gameObject;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("Timeline Bar", "Timeline Bar", timelineBarBase, new List<Component>
            {
                timelineBarBase.GetComponent<Image>()
            }));

            var timeButton = timeDefault.AddComponent<Button>();
            EditorThemeManager.AddElement(new EditorThemeManager.Element("Format Time Button", "List Button 1", timeDefault, new List<Component>
            {
                timeDefault.GetComponent<Image>(),
                timeButton
            }, true, 1, SpriteManager.RoundedSide.W, true));

            EditorThemeManager.AddElement(new EditorThemeManager.Element("Time Input Field", "Input Field", timeObj, new List<Component>
            {
                timeIF.image,
            }, true, 1, SpriteManager.RoundedSide.W));

            EditorThemeManager.AddElement(new EditorThemeManager.Element("Time Input Field Text", "Input Field Text", timeIF.textComponent.gameObject, new List<Component>
            {
                timeIF.textComponent,
            }));

            var play = timelineBar.transform.Find("play").gameObject;
            Destroy(play.GetComponent<Animator>());
            var playButton = play.GetComponent<Button>();
            playButton.transition = Selectable.Transition.ColorTint;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("Play Button", "Function 2", play, new List<Component>
            {
                play.GetComponent<Image>(),
                playButton
            }, isSelectable: true));

            var leftPitch = timelineBar.transform.Find("<").gameObject;
            Destroy(leftPitch.GetComponent<Animator>());
            var leftPitchButton = leftPitch.GetComponent<Button>();
            leftPitchButton.transition = Selectable.Transition.ColorTint;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("Left Pitch Button", "Function 2", leftPitch, new List<Component>
            {
                leftPitch.GetComponent<Image>(),
                leftPitchButton
            }, isSelectable: true));

            EditorThemeManager.AddElement(new EditorThemeManager.Element("Pitch Input Field", "Input Field", pitchObj, new List<Component>
            {
                pitchIF.image,
            }, true, 1, SpriteManager.RoundedSide.W));

            EditorThemeManager.AddElement(new EditorThemeManager.Element("Pitch Input Field Text", "Input Field Text", pitchIF.textComponent.gameObject, new List<Component>
            {
                pitchIF.textComponent,
            }));

            var rightPitch = timelineBar.transform.Find(">").gameObject;
            Destroy(rightPitch.GetComponent<Animator>());
            var rightPitchButton = rightPitch.GetComponent<Button>();
            rightPitchButton.transition = Selectable.Transition.ColorTint;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("Right Pitch Button", "Function 2", rightPitch, new List<Component>
            {
                rightPitch.GetComponent<Image>(),
                rightPitchButton
            }, isSelectable: true));

            // Leave this group empty since the color is already handled via the custom layer colors. This is only here for the rounded edges.
            EditorThemeManager.AddElement(new EditorThemeManager.Element("Layer Input Field", "", layersObj, new List<Component>
            {
                layersIF.image,
            }, true, 1, SpriteManager.RoundedSide.W));

            EditorThemeManager.AddElement(new EditorThemeManager.Element("Layer Event Toggle", "Event/Check", eventToggle.gameObject, new List<Component>
            {
                eventToggle.image,
            }, true, 1, SpriteManager.RoundedSide.W));

            var eventToggleText = eventToggle.transform.Find("Background/Text").gameObject;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("Layer Event Toggle Text", "Event/Check Text", eventToggleText, new List<Component>
            {
                eventToggleText.GetComponent<Text>(),
            }));

            EditorThemeManager.AddElement(new EditorThemeManager.Element("Layer Event Toggle Checkmark", "Timeline Bar", eventToggle.graphic.gameObject, new List<Component>
            {
                (Image)eventToggle.graphic,
            }));

            var prefabButton = timelineBar.transform.Find("prefab").gameObject;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("Prefab Button", "Prefab", prefabButton, new List<Component>
            {
                prefabButton.GetComponent<Image>()
            }, true, 1, SpriteManager.RoundedSide.W));

            var prefabButtonText = prefabButton.transform.GetChild(0).gameObject;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("Prefab Button Text", "Prefab Text", prefabButtonText, new List<Component>
            {
                prefabButtonText.GetComponent<Text>()
            }));

            var objectButton = timelineBar.transform.Find("object").gameObject;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("Object Button", "Object", objectButton, new List<Component>
            {
                objectButton.GetComponent<Image>()
            }, true, 1, SpriteManager.RoundedSide.W));

            var objectButtonText = objectButton.transform.GetChild(0).gameObject;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("Object Button Text", "Object Text", objectButtonText, new List<Component>
            {
                objectButtonText.GetComponent<Text>()
            }));

            var markerButton = timelineBar.transform.Find("event").gameObject;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("Marker Button", "Marker", markerButton, new List<Component>
            {
                markerButton.GetComponent<Image>()
            }, true, 1, SpriteManager.RoundedSide.W));

            var markerButtonText = markerButton.transform.GetChild(0).gameObject;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("Marker Button Text", "Marker Text", markerButtonText, new List<Component>
            {
                markerButtonText.GetComponent<Text>()
            }));

            var checkpointButton = timelineBar.transform.Find("checkpoint").gameObject;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("Checkpoint Button", "Checkpoint", markerButton, new List<Component>
            {
                checkpointButton.GetComponent<Image>()
            }, true, 1, SpriteManager.RoundedSide.W));

            var checkpointButtonText = checkpointButton.transform.GetChild(0).gameObject;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("Checkpoint Button Text", "Checkpoint Text", checkpointButtonText, new List<Component>
            {
                checkpointButtonText.GetComponent<Text>()
            }));

            var backgroundButton = timelineBar.transform.Find("background").gameObject;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("Background Button", "Background Object", markerButton, new List<Component>
            {
                backgroundButton.GetComponent<Image>()
            }, true, 1, SpriteManager.RoundedSide.W));

            var backgroundButtonText = backgroundButton.transform.GetChild(0).gameObject;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("Background Button Text", "Background Object Text", backgroundButtonText, new List<Component>
            {
                backgroundButtonText.GetComponent<Text>()
            }));

            var playTest = timelineBar.transform.Find("playtest").gameObject;
            Destroy(playTest.GetComponent<Animator>());
            var playTestButton = playTest.GetComponent<Button>();
            playTestButton.transition = Selectable.Transition.ColorTint;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("Play Test Button", "Function 2", playTest, new List<Component>
            {
                playTest.GetComponent<Image>(),
                playTestButton
            }, isSelectable: true));
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
            tltrig.triggers.Add(TriggerHelper.StartDragTrigger());
            tltrig.triggers.Add(TriggerHelper.DragTrigger());
            tltrig.triggers.Add(TriggerHelper.EndDragTrigger());

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
                var eventKeyframeCreation = new EventTrigger.Entry();
                eventKeyframeCreation.eventID = EventTriggerType.PointerDown;
                eventKeyframeCreation.callback.AddListener(delegate (BaseEventData eventData)
                {
                    var layer = Layer + 1;
                    int max = RTEventEditor.EventLimit * layer;
                    int min = max - RTEventEditor.EventLimit;

                    Debug.Log($"{EditorPlugin.className}EventHolder: {typeTmp}\nMax: {max}\nMin: {min}\nCurrent Event: {min + typeTmp}");
                    if (((PointerEventData)eventData).button == PointerEventData.InputButton.Right)
                    {
                        if (RTEventEditor.EventTypes.Length > min + typeTmp && (ShowModdedUI && DataManager.inst.gameData.eventObjects.allEvents.Count > min + typeTmp || 10 > min + typeTmp))
                            RTEventEditor.inst.NewKeyframeFromTimeline(min + typeTmp);
                    }
                });
                et.triggers.Add(eventKeyframeCreation);
            }

            TriggerHelper.AddEventTriggerParams(EditorManager.inst.timelineScrollbar, TriggerHelper.CreateEntry(EventTriggerType.Scroll, delegate (BaseEventData baseEventData)
            {
                var pointerEventData = (PointerEventData)baseEventData;

                var scrollBar = EditorManager.inst.timelineScrollRectBar;
                float multiply = Input.GetKey(KeyCode.LeftAlt) ? 0.1f : Input.GetKey(KeyCode.LeftControl) ? 10f : 1f;

                scrollBar.value = pointerEventData.scrollDelta.y > 0f ? scrollBar.value + (0.005f * multiply) : pointerEventData.scrollDelta.y < 0f ? scrollBar.value - (0.005f * multiply) : 0f;
            }));
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

            if (ModCompatibility.ArcadiaCustomsInstalled)
                EditorHelper.AddEditorDropdown("Quit to Arcade", "", "File", titleBar.Find("File/File Dropdown/Quit to Main Menu/Image").GetComponent<Image>().sprite, delegate ()
                {
                    EditorManager.inst.ShowDialog("Warning Popup");
                    RefreshWarningPopup("Are you sure you want to quit to the arcade? Any unsaved progress will be lost!", delegate ()
                    {
                        DG.Tweening.DOTween.Clear();
                        Updater.UpdateObjects(false);
                        DataManager.inst.gameData = null;
                        DataManager.inst.gameData = new GameData();

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
                    EditorManager.inst.ShowDialog("Warning Popup");
                    RefreshWarningPopup("Are you sure you want to switch to Arcade Mode? Any unsaved progress will be lost!", delegate ()
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
                    }, delegate ()
                    {
                        EditorManager.inst.HideDialog("Warning Popup");
                    });
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

            EditorHelper.AddEditorDropdown("Clear Sprite Data", "", "Edit", null, delegate ()
            {
                EditorManager.inst.ShowDialog("Warning Popup");
                RefreshWarningPopup("Are you sure you want to clear sprite data? Any Image Shapes that use a stored image will have their images cleared and you will need to set them again.", delegate ()
                {
                    AssetManager.SpriteAssets.Clear();
                }, delegate ()
                {
                    EditorManager.inst.HideDialog("Warning Popup");
                });
            });

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

            EditorThemeManager.AddElement(new EditorThemeManager.Element("Open File Popup Orderby", "Dropdown 1", sortList, new List<Component>
            {
                sortList.GetComponent<Image>(),
            }, true, 1, SpriteManager.RoundedSide.W));
            
            EditorThemeManager.AddElement(new EditorThemeManager.Element("Open File Popup Orderby Text", "Dropdown 1 Overlay", sortList.transform.Find("Label").gameObject, new List<Component>
            {
                sortList.transform.Find("Label").gameObject.GetComponent<Text>(),
            }));
            
            EditorThemeManager.AddElement(new EditorThemeManager.Element("Open File Popup Orderby Arrow", "Dropdown 1 Overlay", sortList.transform.Find("Arrow").gameObject, new List<Component>
            {
                sortList.transform.Find("Arrow").gameObject.GetComponent<Image>(),
            }));

            var template = sortList.transform.Find("Template").gameObject;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("Open File Popup Orderby Template", "Dropdown 1", template, new List<Component>
            {
                template.GetComponent<Image>(),
            }, true, 1, SpriteManager.RoundedSide.Bottom));
            
            var templateItem = template.transform.Find("Viewport/Content/Item/Item Background").gameObject;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("Open File Popup Orderby Template", "Dropdown 1 Item", templateItem, new List<Component>
            {
                templateItem.GetComponent<Image>(),
            }, true, 1, SpriteManager.RoundedSide.W));
            
            var templateItemCheckmark = template.transform.Find("Viewport/Content/Item/Item Checkmark").gameObject;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("Open File Popup Orderby Template Checkmark", "Dropdown 1 Overlay", templateItemCheckmark, new List<Component>
            {
                templateItemCheckmark.GetComponent<Image>(),
            }));

            var templateItemLabel = template.transform.Find("Viewport/Content/Item/Item Label").gameObject;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("Open File Popup Orderby Template Label", "Dropdown 1 Overlay", templateItemLabel, new List<Component>
            {
                templateItemLabel.GetComponent<Text>(),
            }));

            var config = EditorConfig.Instance;

            var sortListRT = sortList.GetComponent<RectTransform>();
            sortListRT.anchoredPosition = config.OpenLevelDropdownPosition.Value;
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
            checkDesRT.anchoredPosition = config.OpenLevelTogglePosition.Value;

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

            var toggleB = checkDes.transform.Find("toggle/Background").gameObject;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("Open File Popup Ascend", "Toggle 1", toggleB, new List<Component>
            {
                toggleB.GetComponent<Image>(),
            }, true, 1, SpriteManager.RoundedSide.W));
            
            var toggleC = toggleB.transform.Find("Checkmark").gameObject;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("Open File Popup Ascend Check", "Toggle 1 Check", toggleC, new List<Component>
            {
                toggleC.GetComponent<Image>(),
            }));

            TooltipHelper.AddTooltip(toggleB, new List<HoverTooltip.Tooltip> { sortListTip.tooltipLangauges[0] });

            CreateGlobalSettings();
            LoadGlobalSettings();

            // Editor Path
            {
                var editorPathGO = GameObject.Find("TimelineBar/GameObject/Time Input")
                    .Duplicate(EditorManager.inst.GetDialog("Open File Popup").Dialog, "editor path");
                ((RectTransform)editorPathGO.transform).anchoredPosition = config.OpenLevelEditorPathPos.Value;
                ((RectTransform)editorPathGO.transform).sizeDelta = new Vector2(config.OpenLevelEditorPathLength.Value, 32f);

                EditorThemeManager.AddElement(new EditorThemeManager.Element("Open File Popup Path", "Input Field", editorPathGO, new List<Component>
                {
                    editorPathGO.GetComponent<Image>(),
                }, true, 1, SpriteManager.RoundedSide.W));

                var text = editorPathGO.transform.Find("Text").gameObject;
                var textComponent = text.GetComponent<Text>();
                textComponent.alignment = TextAnchor.MiddleLeft;
                textComponent.fontSize = 16;

                EditorThemeManager.AddElement(new EditorThemeManager.Element("Open File Popup Path Text", "Input Field Text", text, new List<Component>
                {
                    textComponent,
                }));

                var levelListTip = editorPathGO.GetComponent<HoverTooltip>();
                if (!levelListTip)
                    levelListTip = editorPathGO.AddComponent<HoverTooltip>();

                var llTip = new HoverTooltip.Tooltip();

                llTip.desc = "Level list path";
                llTip.hint = "Input the path you want to load levels from within the beatmaps folder. For example: inputting \"editor\" into the input field will load levels from beatmaps/editor. You can also set it to sub-directories, like: \"editor/pa levels\" will take levels from \"beatmaps/editor/pa levels\".";

                levelListTip.tooltipLangauges.Add(llTip);

                var editorPathIF = editorPathGO.GetComponent<InputField>();
                editorPathIF.characterValidation = InputField.CharacterValidation.None;
                editorPathIF.onValueChanged.RemoveAllListeners();
                editorPathIF.text = EditorPath;
                editorPathIF.onValueChanged.AddListener(delegate (string _val)
                {
                    EditorPath = _val;
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
                ((RectTransform)levelListReloader.transform).anchoredPosition = config.OpenLevelListRefreshPosition.Value;
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
                    if (editorListPath[editorListPath.Length - 1] == '/')
                        return;

                    if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + editorListPath))
                        Directory.CreateDirectory(RTFile.ApplicationDirectory + editorListPath);

                    SaveGlobalSettings();

                    EditorManager.inst.GetLevelList();
                });

                string refreshImage = RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_refresh-white.png";

                EditorThemeManager.AddElement(new EditorThemeManager.Element("Open File Popup Reloader", "Function 2", levelListReloader, new List<Component>
                {
                    levelListReloader.GetComponent<Image>(),
                    levelListRButton
                }, isSelectable: true));

                if (RTFile.FileExists(refreshImage))
                {
                    var spriteReloader = levelListReloader.GetComponent<Image>();

                    spriteReloader.sprite = SpriteManager.LoadSprite(refreshImage);
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
                themePathIF.onValueChanged.RemoveAllListeners();
                themePathIF.text = ThemePath;
                themePathIF.onValueChanged.AddListener(delegate (string _val)
                {
                    ThemePath = _val;
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
                    if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + themeListPath))
                        Directory.CreateDirectory(RTFile.ApplicationDirectory + themeListPath);

                    SaveGlobalSettings();

                    StartCoroutine(LoadThemes(true));
                    EventEditor.inst.RenderEventsDialog();
                });

                string refreshImage = RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_refresh-white.png";

                if (RTFile.FileExists(refreshImage))
                {
                    var spriteReloader = themePathReloader.GetComponent<Image>();

                    spriteReloader.sprite = SpriteManager.LoadSprite(refreshImage);
                }
            }

            // Prefab Path
            {
                var prefabPathGO = timeIF.gameObject.Duplicate(EditorManager.inst.GetDialog("Prefab Popup").Dialog.Find("external prefabs"), "prefabs path");

                ((RectTransform)prefabPathGO.transform).anchoredPosition = config.PrefabExternalPrefabPathPos.Value;
                ((RectTransform)prefabPathGO.transform).sizeDelta = new Vector2(config.PrefabExternalPrefabPathLength.Value, 34f);

                var levelListTip = prefabPathGO.AddComponent<HoverTooltip>();
                var llTip = new HoverTooltip.Tooltip();

                llTip.desc = "Prefab list path";
                llTip.hint = "Input the path you want to load prefabs from within the beatmaps folder. For example: inputting \"prefabs\" into the input field will load levels from beatmaps/prefabs. You can also set it to sub-directories, like: \"prefabs/pa characters\" will take levels from \"beatmaps/prefabs/pa characters\".";

                levelListTip.tooltipLangauges.Add(llTip);

                var prefabPathIF = prefabPathGO.GetComponent<InputField>();
                prefabPathIF.characterValidation = InputField.CharacterValidation.None;
                prefabPathIF.onValueChanged.RemoveAllListeners();
                prefabPathIF.text = PrefabPath;
                prefabPathIF.onValueChanged.AddListener(delegate (string _val)
                {
                    PrefabPath = _val;
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
                ((RectTransform)levelListReloader.transform).anchoredPosition = config.PrefabExternalPrefabRefreshPos.Value;
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
                    if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + prefabListPath))
                        Directory.CreateDirectory(RTFile.ApplicationDirectory + prefabListPath);
                    SaveGlobalSettings();

                    StartCoroutine(UpdatePrefabs());
                });

                string refreshImage = RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_refresh-white.png";

                if (RTFile.FileExists(refreshImage))
                {
                    var spriteReloader = levelListReloader.GetComponent<Image>();

                    spriteReloader.sprite = SpriteManager.LoadSprite(refreshImage);
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

            var config = EditorConfig.Instance;

            try
            {
                timelineSliderHandle = GameObject.Find("Editor Systems/Editor GUI/sizer/main/whole-timeline/Slider_Parent/Slider/Handle Slide Area/Image/Handle").GetComponent<Image>();
                timelineSliderHandle.color = config.TimelineCursorColor.Value;

                timelineSliderRuler = GameObject.Find("Editor Systems/Editor GUI/sizer/main/whole-timeline/Slider_Parent/Slider/Handle Slide Area/Image").GetComponent<Image>();
                timelineSliderRuler.color = config.TimelineCursorColor.Value;

                keyframeTimelineSliderHandle = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/timeline/Scroll View/Viewport/Content/time_slider/Handle Slide Area/Handle/Image").GetComponent<Image>();
                keyframeTimelineSliderHandle.color = config.KeyframeCursorColor.Value;

                keyframeTimelineSliderRuler = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/timeline/Scroll View/Viewport/Content/time_slider/Handle Slide Area/Handle").GetComponent<Image>();
                keyframeTimelineSliderRuler.color = config.KeyframeCursorColor.Value;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{EditorPlugin.className}SetupTimelineElements Error {ex}");
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

            var config = EditorConfig.Instance;

            timelineGridRenderer.color = config.TimelineGridColor.Value;
            timelineGridRenderer.thickness = config.TimelineGridThickness.Value;

            timelineGridRenderer.enabled = config.TimelineGridEnabled.Value;

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

        public void SetupNewFilePopup()
        {
            var newFilePopupBase = EditorManager.inst.GetDialog("New File Popup").Dialog;

            var newFilePopup = newFilePopupBase.Find("New File Popup");

            EditorThemeManager.AddElement(new EditorThemeManager.Element("New File Popup", "Background", newFilePopup.gameObject, new List<Component>
            {
                newFilePopup.GetComponent<Image>(),
            }, true, 1, SpriteManager.RoundedSide.W));

            var newFilePopupPanel = newFilePopup.Find("Panel").gameObject;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("New File Popup Panel", "Background", newFilePopupPanel, new List<Component>
            {
                newFilePopupPanel.GetComponent<Image>(),
            }, true, 1, SpriteManager.RoundedSide.Top));

            var newFilePopupClose = newFilePopupPanel.transform.Find("x").gameObject;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("New File Popup Close", "Close", newFilePopupClose, new List<Component>
            {
                newFilePopupClose.GetComponent<Image>(),
                newFilePopupClose.GetComponent<Button>(),
            }, true, 1, SpriteManager.RoundedSide.W, true));

            var newFilePopupCloseX = newFilePopupClose.transform.GetChild(0).gameObject;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("Open File Popup Close X", "Close X", newFilePopupCloseX, new List<Component>
            {
                newFilePopupCloseX.GetComponent<Image>(),
            }));

            var newFilePopupTitle = newFilePopupPanel.transform.Find("Title").gameObject;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("New File Popup Title", "Light Text", newFilePopupTitle, new List<Component>
            {
                newFilePopupTitle.GetComponent<TextMeshProUGUI>(),
            }));

            var openFilePopupSelect = newFilePopup.gameObject.AddComponent<SelectGUI>();
            openFilePopupSelect.target = newFilePopup;
            openFilePopupSelect.ogPos = newFilePopup.position;

            var pather = newFilePopup.Find("GameObject");
            var spacer = newFilePopup.GetChild(6);

            var path = pather.Find("song-filename").GetComponent<InputField>();

            spacer.gameObject.Duplicate(newFilePopup, "spacer", 7);

            var browseBase = pather.gameObject.Duplicate(newFilePopup, "browse", 7);

            Destroy(browseBase.transform.GetChild(0).gameObject);
            Destroy(pather.GetChild(1).gameObject);

            newFilePopup.Find("Song Filename").GetComponent<Text>().text = "Song Path";

            var browseLocal = browseBase.transform.Find("browse");
            var browseLocalText = browseLocal.Find("Text").GetComponent<Text>();
            browseLocalText.text = "Local Browser";
            var browseLocalButton = browseLocal.GetComponent<Button>();
            browseLocalButton.onClick.ClearAll();
            browseLocalButton.onClick.AddListener(delegate ()
            {
                string text = FileBrowser.OpenSingleFile("Select a song to use!", RTFile.ApplicationDirectory, "ogg", "wav", "mp3");
                if (!string.IsNullOrEmpty(text))
                {
                    path.text = text;
                }
            });

            var browseInternal = browseLocal.gameObject.Duplicate(browseBase.transform, "internal browse");
            var browseInternalText = browseInternal.transform.Find("Text").GetComponent<Text>();
            browseInternalText.text = "In-game Browser";
            var browseInternalButton = browseInternal.GetComponent<Button>();
            browseInternalButton.onClick.ClearAll();
            browseInternalButton.onClick.AddListener(delegate ()
            {
                EditorManager.inst.ShowDialog("Browser Popup");
                RTFileBrowser.inst.UpdateBrowser(Directory.GetCurrentDirectory(), new string[] { ".ogg", ".wav", ".mp3" }, onSelectFile: delegate (string _val)
                {
                    if (!string.IsNullOrEmpty(_val))
                    {
                        EditorManager.inst.HideDialog("Browser Popup");
                        path.text = _val;
                    }
                });
            });

            var chooseTemplate = browseLocal.gameObject.Duplicate(newFilePopup, "choose template", 8);
            var chooseTemplateText = chooseTemplate.transform.Find("Text").GetComponent<Text>();
            chooseTemplateText.text = "Choose Template";
            var chooseTemplateButton = chooseTemplate.GetComponent<Button>();
            chooseTemplateButton.onClick.ClearAll();
            chooseTemplateButton.onClick.AddListener(delegate ()
            {
                EditorManager.inst.DisplayNotification("wip", 0.5f, EditorManager.NotificationType.Info);
            });
            chooseTemplate.transform.AsRT().sizeDelta = new Vector2(384f, 32f);

            spacer.gameObject.Duplicate(newFilePopup, "spacer", 8);

            var hlg = browseBase.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;

            pather.gameObject.AddComponent<HorizontalLayoutGroup>();

            var newFilePopupLabel1 = newFilePopup.Find("Level Name").gameObject;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("New File Popup Label 1", "Light Text", newFilePopupLabel1, new List<Component>
            {
                newFilePopupLabel1.GetComponent<Text>(),
            }));

            var levelName = newFilePopup.Find("level-name").gameObject;
            var levelNameImage = levelName.GetComponent<Image>();
            levelNameImage.fillCenter = true;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("Open File Popup Level Name", "Input Field", levelName, new List<Component>
            {
                levelNameImage,
            }, true, 1, SpriteManager.RoundedSide.W));

            var levelNameText = levelName.transform.Find("Text").gameObject;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("New File Popup Level Name Text", "Input Field Text", levelNameText, new List<Component>
            {
                levelNameText.GetComponent<Text>(),
            }));

            var pathImage = path.GetComponent<Image>();
            pathImage.fillCenter = true;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("Open File Popup Path", "Input Field", path.gameObject, new List<Component>
            {
                pathImage,
            }, true, 1, SpriteManager.RoundedSide.W));

            var pathText = path.transform.Find("Text").gameObject;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("New File Popup Path Text", "Input Field Text", pathText, new List<Component>
            {
                pathText.GetComponent<Text>(),
            }));

            EditorThemeManager.AddElement(new EditorThemeManager.Element("New File Popup Browse Local", "Function 2 Normal", browseLocal.gameObject, new List<Component>
            {
                browseLocal.GetComponent<Image>(),
            }, true, 1, SpriteManager.RoundedSide.W));

            EditorThemeManager.AddElement(new EditorThemeManager.Element("New File Popup Browse Local Text", "Function 2 Text", browseLocalText.gameObject, new List<Component>
            {
                browseLocalText,
            }));

            EditorThemeManager.AddElement(new EditorThemeManager.Element("New File Popup Browse Internal", "Function 2 Normal", browseInternal.gameObject, new List<Component>
            {
                browseInternal.GetComponent<Image>(),
            }, true, 1, SpriteManager.RoundedSide.W));

            EditorThemeManager.AddElement(new EditorThemeManager.Element("New File Popup Browse Internal Text", "Function 2 Text", browseInternalText.gameObject, new List<Component>
            {
                browseInternalText,
            }));

            EditorThemeManager.AddElement(new EditorThemeManager.Element("New File Popup Choose Template", "Function 2 Normal", chooseTemplate.gameObject, new List<Component>
            {
                chooseTemplate.GetComponent<Image>(),
            }, true, 1, SpriteManager.RoundedSide.W));

            EditorThemeManager.AddElement(new EditorThemeManager.Element("New File Popup Choose Template Text", "Function 2 Text", chooseTemplateText.gameObject, new List<Component>
            {
                chooseTemplateText,
            }));

            var create = newFilePopup.Find("submit").gameObject;
            Destroy(create.GetComponent<Animator>());
            create.GetComponent<Button>().transition = Selectable.Transition.ColorTint;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("New File Popup Create", "Add", create, new List<Component>
            {
                create.GetComponent<Image>(),
            }, true, 1, SpriteManager.RoundedSide.W));

            var createText = create.transform.Find("text").gameObject;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("New File Popup Create Text", "Add Text", createText, new List<Component>
            {
                createText.GetComponent<Text>(),
            }));
        }

        public void CreatePreviewCover()
        {
            var gameObject = new GameObject("Preview Cover");
            gameObject.transform.SetParent(EditorManager.inst.dialogs.parent);
            gameObject.transform.localScale = Vector3.one;

            gameObject.transform.SetSiblingIndex(1);

            var rectTransform = gameObject.AddComponent<RectTransform>();
            var image = gameObject.AddComponent<Image>();

            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(10000f, 10000f);

            PreviewCover = new EditorThemeManager.Element("Preview Cover", "Preview Cover", gameObject, new List<Component>
            {
                image,
            });
            EditorThemeManager.AddElement(PreviewCover);

            gameObject.SetActive(!RTHelpers.AprilFools);
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

            var submit1Image = submit1.GetComponent<Image>();
            submit1Image.color = new Color(1f, 0.2137f, 0.2745f, 1f);
            var submit2Image = submit2.GetComponent<Image>();
            submit2Image.color = new Color(0.302f, 0.7137f, 0.6745f, 1f);

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

            var panel = main.Find("Panel");

            var close = panel.Find("x").GetComponent<Button>();
            close.onClick.ClearAll();
            close.onClick.RemoveAllListeners();
            close.onClick.AddListener(delegate ()
            {
                EditorManager.inst.HideDialog("Warning Popup");
            });

            var title = panel.Find("Text").GetComponent<Text>();
            title.text = "Warning!";

            EditorHelper.AddEditorPopup("Warning Popup", warningPopup);

            EditorThemeManager.AddElement(new EditorThemeManager.Element("Warning Popup", "Background", main.gameObject, new List<Component>
            {
                main.GetComponent<Image>(),
            }, true, 1, SpriteManager.RoundedSide.W));

            EditorThemeManager.AddElement(new EditorThemeManager.Element("Warning Popup Panel", "Background", panel.gameObject, new List<Component>
            {
                panel.GetComponent<Image>(),
            }, true, 1, SpriteManager.RoundedSide.Top));

            EditorThemeManager.AddElement(new EditorThemeManager.Element("Warning Popup Close", "Close", close.gameObject, new List<Component>
            {
                close.GetComponent<Image>(),
                close,
            }, true, 1, SpriteManager.RoundedSide.W, true));

            var warningPopupCloseX = close.transform.GetChild(0).gameObject;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("Warning Popup Close X", "Close X", warningPopupCloseX, new List<Component>
            {
                warningPopupCloseX.GetComponent<Image>(),
            }));

            EditorThemeManager.AddElement(new EditorThemeManager.Element("Warning Popup Title", "Light Text", title.gameObject, new List<Component>
            {
                title,
            }));

            var warningPopupText = main.Find("Level Name").gameObject;
            EditorThemeManager.AddElement(new EditorThemeManager.Element("Warning Popup Label", "Light Text", warningPopupText, new List<Component>
            {
                warningPopupText.GetComponent<Text>(),
            }));

            EditorThemeManager.AddElement(new EditorThemeManager.Element("Warning Popup Confirm", "Warning Confirm", submit1Button.gameObject, new List<Component>
            {
                submit1Image
            }, true, 1, SpriteManager.RoundedSide.W));
            EditorThemeManager.AddElement(new EditorThemeManager.Element("Warning Popup Confirm Text", "Add Text", submit1Button.transform.GetChild(0).gameObject, new List<Component>
            {
                submit1Button.transform.GetChild(0).GetComponent<Text>(),
            }));

            EditorThemeManager.AddElement(new EditorThemeManager.Element("Warning Popup Cancel", "Warning Cancel", submit2Button.gameObject, new List<Component>
            {
                submit2Image
            }, true, 1, SpriteManager.RoundedSide.W));
            EditorThemeManager.AddElement(new EditorThemeManager.Element("Warning Popup Cancel Text", "Add Text", submit2Button.transform.GetChild(0).gameObject, new List<Component>
            {
                submit2Button.transform.GetChild(0).GetComponent<Text>(),
            }));
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
            var barButtonImage = barButton.GetComponent<Image>();

            var eventButton = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject/event");

            var multiObjectEditorDialog = EditorManager.inst.GetDialog("Multi Object Editor").Dialog;

            EditorThemeManager.AddElement(new EditorThemeManager.Element("Multi Object Editor", "Background", multiObjectEditorDialog.gameObject, new List<Component>
            {
                multiObjectEditorDialog.GetComponent<Image>(),
            }));

            var dataLeft = multiObjectEditorDialog.Find("data/left");

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

            var textHolder = multiObjectEditorDialog.Find("data/right/text holder/Text");
            var textHolderText = textHolder.GetComponent<Text>();
            textHolderText.text = textHolderText.text.Replace(
                "The current version of the editor doesn't support any editing functionality.",
                "On the left you'll see all the Multi Object Editor tools you'll need.");

            EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor Info", "Light Text", textHolderText.gameObject, new List<Component>
            {
                textHolderText,
            }));

            var updateMultiObjectInfo = textHolder.gameObject.AddComponent<UpdateMultiObjectInfo>();
            updateMultiObjectInfo.Text = textHolderText;

            textHolderText.fontSize = 22;

            textHolder.AsRT().anchoredPosition = new Vector2(0f, -125f);

            textHolder.AsRT().sizeDelta = new Vector2(-68f, 0f);

            var zoom = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/zoom/zoom");

            var labelL = parent.Find("label layer");
            labelL.SetParent(transform);
            Destroy(parent.Find("label depth").gameObject);

            Action<string, string, bool, UnityAction, UnityAction, UnityAction> inputFieldGenerator
                = delegate (string name, string placeHolder, bool doMiddle, UnityAction leftButton, UnityAction middleButton, UnityAction rightButton)
            {
                var gameObject = zoom.Duplicate(parent, name);
                gameObject.transform.localScale = Vector3.one;
                gameObject.transform.GetChild(0).Find("input/Placeholder").GetComponent<Text>().text = placeHolder;

                ((RectTransform)gameObject.transform).sizeDelta = new Vector2(428f, 32f);

                var x = gameObject.transform.GetChild(0);
                var layerIF = x.GetComponent<InputField>();
                layerIF.text = "1";

                if (doMiddle)
                {
                    var multiLB = gameObject.transform.GetChild(0).Find("<").gameObject
                    .Duplicate(gameObject.transform.GetChild(0), "|", 2);
                    multiLB.GetComponent<Image>().sprite = barButtonImage.sprite;

                    var multiLBB = multiLB.GetComponent<Button>();

                    multiLBB.onClick.RemoveAllListeners();
                    multiLBB.onClick.AddListener(middleButton);
                    Destroy(multiLBB.GetComponent<Animator>());
                    multiLBB.transition = Selectable.Transition.ColorTint;

                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor | {name}", "Function 2", multiLBB.gameObject, new List<Component>
                    {
                        multiLBB.GetComponent<Image>(),
                        multiLBB,
                    }, isSelectable: true));
                }

                var mlsLeft = x.Find("<").GetComponent<Button>();
                mlsLeft.onClick.RemoveAllListeners();
                mlsLeft.onClick.AddListener(leftButton);
                Destroy(mlsLeft.GetComponent<Animator>());
                mlsLeft.transition = Selectable.Transition.ColorTint;

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor < {name}", "Function 2", mlsLeft.gameObject, new List<Component>
                {
                    mlsLeft.GetComponent<Image>(),
                    mlsLeft,
                }, isSelectable: true));

                var mlsRight = x.Find(">").GetComponent<Button>();
                mlsRight.onClick.RemoveAllListeners();
                mlsRight.onClick.AddListener(rightButton);
                Destroy(mlsRight.GetComponent<Animator>());
                mlsRight.transition = Selectable.Transition.ColorTint;

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor > {name}", "Function 2", mlsRight.gameObject, new List<Component>
                {
                    mlsRight.GetComponent<Image>(),
                    mlsRight,
                }, isSelectable: true));

                var input = x.Find("input");
                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField {name}", "Input Field", input.gameObject, new List<Component>
                {
                    input.GetComponent<Image>(),
                }, true, 1, SpriteManager.RoundedSide.W));

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Text {name}", "Input Field Text", layerIF.textComponent.gameObject, new List<Component>
                {
                    layerIF.textComponent,
                }));
            };

            Action<string> labelGenerator = delegate (string name)
            {
                var label = labelL.gameObject.Duplicate(parent, "label");
                label.transform.localScale = Vector3.one;
                var text = label.transform.GetChild(0).gameObject.GetComponent<Text>();
                text.text = name;

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor Button Text {name}", "Light Text", text.gameObject, new List<Component>
                {
                    text
                }));
            };

            Action<string, string, Transform, UnityAction> buttonGenerator = delegate (string name, string text, Transform parent, UnityAction unityAction)
            {
                var gameObject = eventButton.Duplicate(parent, name);
                gameObject.transform.localScale = Vector3.one;

                ((RectTransform)gameObject.transform).sizeDelta = new Vector2(404f, 32f);

                var textComponent = gameObject.transform.GetChild(0).GetComponent<Text>();
                textComponent.text = text;

                var button = gameObject.GetComponent<Button>();
                button.onClick.ClearAll();
                button.onClick.AddListener(unityAction);

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor Button {name}", "Function 1", gameObject, new List<Component>
                {
                    gameObject.GetComponent<Image>()
                }, true, 1, SpriteManager.RoundedSide.W));

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor Button Text {name}", "Function 1 Text", textComponent.gameObject, new List<Component>
                {
                    textComponent
                }));
            };

            // man i need to clean this up but aaaa

            // Layers
            {
                labelGenerator("Set Group Layer");

                inputFieldGenerator("layer", "Enter layer...", true, delegate ()
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

                inputFieldGenerator("depth", "Enter depth...", true, delegate ()
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

                inputFieldGenerator("time", "Enter time...", true, delegate ()
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

                inputFieldGenerator("autokill offset", "Enter autokill...", true, delegate ()
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

                var inputFieldInput = multiNameSet.transform.GetChild(0).Find("input");
                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Name", "Input Field", inputFieldInput.gameObject, new List<Component>
                {
                    inputFieldInput.GetComponent<Image>(),
                }, true, 1, SpriteManager.RoundedSide.W));

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Text Name", "Input Field Text", inputField.textComponent.gameObject, new List<Component>
                {
                    inputField.textComponent,
                }));

                var multiNB = multiNameSet.transform.GetChild(0).Find("<").gameObject;

                multiNB.name = "|";
                var multiNBImage = multiNB.GetComponent<Image>();
                multiNBImage.sprite = barButtonImage.sprite;

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
                Destroy(multiNBB.GetComponent<Animator>());
                multiNBB.transition = Selectable.Transition.ColorTint;

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor | name", "Function 2", multiNBB.gameObject, new List<Component>
                {
                    multiNBImage,
                    multiNBB,
                }, isSelectable: true));

                var add = multiNameSet.transform.GetChild(0).Find(">");
                add.name = "+";

                var addButton = add.GetComponent<Button>();
                var addImage = add.GetComponent<Image>();

                var addFilePath = RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/add.png";

                if (RTFile.FileExists(addFilePath))
                {
                    EditorManager.inst.StartCoroutine(EditorManager.inst.GetSprite(addFilePath, new EditorManager.SpriteLimits(), delegate (Sprite cover)
                    {
                        addImage.sprite = cover;
                    }, delegate (string errorFile)
                    {
                        addImage.sprite = ArcadeManager.inst.defaultImage;
                    }));
                }

                var mtnLeftLE = add.gameObject.AddComponent<LayoutElement>();
                mtnLeftLE.ignoreLayout = true;

                add.AsRT().anchoredPosition = new Vector2(339f, 0f);
                add.AsRT().sizeDelta = new Vector2(32f, 32f);

                addButton.onClick.RemoveAllListeners();
                addButton.onClick.AddListener(delegate ()
                {
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                    {
                        timelineObject.GetData<BeatmapObject>().name += inputField.text;
                        ObjectEditor.inst.RenderTimelineObject(timelineObject);
                    }
                });
                Destroy(add.GetComponent<Animator>());
                addButton.transition = Selectable.Transition.ColorTint;

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor + name", "Function 2", add.gameObject, new List<Component>
                {
                    addImage,
                    addButton,
                }, isSelectable: true));
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

                var inputFieldInput = multiNameSet.transform.GetChild(0).Find("input");
                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Tags", "Input Field", inputFieldInput.gameObject, new List<Component>
                {
                    inputFieldInput.GetComponent<Image>(),
                }, true, 1, SpriteManager.RoundedSide.W));

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Text Tags", "Input Field Text", inputField.textComponent.gameObject, new List<Component>
                {
                    inputField.textComponent,
                }));

                Destroy(multiNameSet.transform.GetChild(0).Find("<").gameObject);

                var add = multiNameSet.transform.GetChild(0).Find(">");
                add.name = "+";

                var addButton = add.GetComponent<Button>();
                var addImage = add.GetComponent<Image>();

                string addFilePath = RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/add.png";

                if (RTFile.FileExists(addFilePath))
                {
                    EditorManager.inst.StartCoroutine(EditorManager.inst.GetSprite(addFilePath, new EditorManager.SpriteLimits(), delegate (Sprite cover)
                    {
                        addImage.sprite = cover;
                    }, delegate (string errorFile)
                    {
                        addImage.sprite = ArcadeManager.inst.defaultImage;
                    }));
                }

                var mtnLeftLE = add.gameObject.AddComponent<LayoutElement>();
                mtnLeftLE.ignoreLayout = true;

                var mtnLeftRT = add.GetComponent<RectTransform>();
                mtnLeftRT.anchoredPosition = new Vector2(339f, 0f);
                mtnLeftRT.sizeDelta = new Vector2(32f, 32f);

                addButton.onClick.RemoveAllListeners();
                addButton.onClick.AddListener(delegate ()
                {
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                    {
                        timelineObject.GetData<BeatmapObject>().tags.Add(inputField.text);
                    }
                });
                Destroy(add.GetComponent<Animator>());
                addButton.transition = Selectable.Transition.ColorTint;

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor + tags", "Function 2", add.gameObject, new List<Component>
                {
                    addImage,
                    addButton,
                }, isSelectable: true));
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

            // LDM Swap
            {
                labelGenerator("Swap each object's LDM");

                buttonGenerator("ldm swap", "Swap LDM", parent, delegate ()
                {
                    foreach (var beatmapObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
                    {
                        beatmapObject.LDM = !beatmapObject.LDM;
                        Updater.UpdateProcessor(beatmapObject);
                    }
                });
            }

            // LDM Toggle
            {
                labelGenerator("Toggle all object's LDM");

                bool doggle = false;

                buttonGenerator("ldm toggle", "Toggle LDM", parent, delegate ()
                {
                    doggle = !doggle;
                    foreach (var beatmapObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
                    {
                        beatmapObject.LDM = doggle;
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

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Name Swapper", "Input Field", oldName, new List<Component>
                {
                    oldName.GetComponent<Image>(),
                }, true, 1, SpriteManager.RoundedSide.W));

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Text Name Swapper", "Input Field Text", oldNameIF.textComponent.gameObject, new List<Component>
                {
                    oldNameIF.textComponent,
                }));

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

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Name Swapper", "Input Field", newName, new List<Component>
                {
                    newName.GetComponent<Image>(),
                }, true, 1, SpriteManager.RoundedSide.W));

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Text Name Swapper", "Input Field Text", newNameIF.textComponent.gameObject, new List<Component>
                {
                    newNameIF.textComponent,
                }));

                var replace = eventButton.Duplicate(replaceName.transform, "replace");
                replace.transform.localScale = Vector3.one;
                replace.transform.AsRT().sizeDelta = new Vector2(66f, 32f);
                replace.GetComponent<LayoutElement>().minWidth = 32f;

                var replaceText = replace.transform.GetChild(0).GetComponent<Text>();

                replaceText.text = "Replace";

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor Button Replace", "Function 1", replace, new List<Component>
                {
                    replace.GetComponent<Image>()
                }, true, 1, SpriteManager.RoundedSide.W));

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor Button Text Replace", "Function 1 Text", replaceText.gameObject, new List<Component>
                {
                    replaceText
                }));

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

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Tag Swapper", "Input Field", oldName, new List<Component>
                {
                    oldName.GetComponent<Image>(),
                }, true, 1, SpriteManager.RoundedSide.W));

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Text Tag Swapper", "Input Field Text", oldNameIF.textComponent.gameObject, new List<Component>
                {
                    oldNameIF.textComponent,
                }));

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

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Tag Swapper", "Input Field", newName, new List<Component>
                {
                    newName.GetComponent<Image>(),
                }, true, 1, SpriteManager.RoundedSide.W));

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Text Tag Swapper", "Input Field Text", newNameIF.textComponent.gameObject, new List<Component>
                {
                    newNameIF.textComponent,
                }));

                var replace = eventButton.Duplicate(replaceName.transform, "replace");
                replace.transform.localScale = Vector3.one;
                replace.transform.AsRT().sizeDelta = new Vector2(66f, 32f);
                replace.GetComponent<LayoutElement>().minWidth = 32f;

                var replaceText = replace.transform.GetChild(0).GetComponent<Text>();

                replaceText.text = "Replace";

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor Button Replace", "Function 1", replace, new List<Component>
                {
                    replace.GetComponent<Image>()
                }, true, 1, SpriteManager.RoundedSide.W));

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor Button Text Replace", "Function 1 Text", replaceText.gameObject, new List<Component>
                {
                    replaceText
                }));

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

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Text Swapper", "Input Field", oldName, new List<Component>
                {
                    oldName.GetComponent<Image>(),
                }, true, 1, SpriteManager.RoundedSide.W));

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Text Text Swapper", "Input Field Text", oldNameIF.textComponent.gameObject, new List<Component>
                {
                    oldNameIF.textComponent,
                }));

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

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Text Swapper", "Input Field", newName, new List<Component>
                {
                    newName.GetComponent<Image>(),
                }, true, 1, SpriteManager.RoundedSide.W));

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Text Text Swapper", "Input Field Text", newNameIF.textComponent.gameObject, new List<Component>
                {
                    newNameIF.textComponent,
                }));

                var replace = eventButton.Duplicate(replaceName.transform, "replace");
                replace.transform.localScale = Vector3.one;
                replace.transform.AsRT().sizeDelta = new Vector2(66f, 32f);
                replace.GetComponent<LayoutElement>().minWidth = 32f;

                var replaceText = replace.transform.GetChild(0).GetComponent<Text>();

                replaceText.text = "Replace";

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor Button Replace", "Function 1", replace, new List<Component>
                {
                    replace.GetComponent<Image>()
                }, true, 1, SpriteManager.RoundedSide.W));

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor Button Text Replace", "Function 1 Text", replaceText.gameObject, new List<Component>
                {
                    replaceText
                }));

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

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Modifier Swapper", "Input Field", oldName, new List<Component>
                {
                    oldName.GetComponent<Image>(),
                }, true, 1, SpriteManager.RoundedSide.W));

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Text Modifier Swapper", "Input Field Text", oldNameIF.textComponent.gameObject, new List<Component>
                {
                    oldNameIF.textComponent,
                }));

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

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Modifier Swapper", "Input Field", newName, new List<Component>
                {
                    newName.GetComponent<Image>(),
                }, true, 1, SpriteManager.RoundedSide.W));

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Text Modifier Swapper", "Input Field Text", newNameIF.textComponent.gameObject, new List<Component>
                {
                    newNameIF.textComponent,
                }));

                var replace = eventButton.Duplicate(replaceName.transform, "replace");
                replace.transform.localScale = Vector3.one;
                replace.transform.AsRT().sizeDelta = new Vector2(66f, 32f);
                replace.GetComponent<LayoutElement>().minWidth = 32f;

                var replaceText = replace.transform.GetChild(0).GetComponent<Text>();

                replaceText.text = "Replace";

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor Button Replace", "Function 1", replace, new List<Component>
                {
                    replace.GetComponent<Image>()
                }, true, 1, SpriteManager.RoundedSide.W));

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor Button Text Replace", "Function 1 Text", replaceText.gameObject, new List<Component>
                {
                    replaceText
                }));

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

            // Assign Colors
            {
                labelGenerator("Assign Colors");

                var colorLayout = new GameObject("color layout");
                colorLayout.transform.SetParent(parent);
                colorLayout.transform.localScale = Vector3.one;

                var colorLayoutRT = colorLayout.AddComponent<RectTransform>();
                colorLayoutRT.sizeDelta = new Vector2(390f, 76f);
                var colorLayoutGLG = colorLayout.AddComponent<GridLayoutGroup>();
                colorLayoutGLG.spacing = new Vector2(4f, 4f);
                colorLayoutGLG.cellSize = new Vector2(36f, 36f);

                for (int i = 0; i < 18; i++)
                {
                    var index = i;
                    var colorGUI = EditorManager.inst.colorGUI.Duplicate(colorLayoutRT, (i + 1).ToString());
                    var assigner = colorGUI.AddComponent<AssignToTheme>();
                    assigner.Index = i;
                    var image = colorGUI.GetComponent<Image>();
                    assigner.Graphic = image;

                    var selected = colorGUI.transform.GetChild(0).gameObject;
                    selected.SetActive(i == 0);

                    var button = colorGUI.GetComponent<Button>();
                    button.onClick.ClearAll();
                    button.onClick.AddListener(delegate ()
                    {
                        currentMultiColorSelection = index;
                        UpdateMultiColorButtons();
                    });

                    multiColorButtons.Add(new MultiColorButton
                    {
                        Button = button,
                        Image = image,
                        Selected = selected
                    });
                }

                labelGenerator("Opacity");

                var opacityObject = zoom.Duplicate(parent, "opacity");
                opacityObject.transform.localScale = Vector3.one;
                var opacityPlaceholder = opacityObject.transform.GetChild(0).Find("input/Placeholder").GetComponent<Text>();
                opacityPlaceholder.text = "Enter value... (Keep me empty to not set)";
                opacityPlaceholder.fontSize = 13;

                ((RectTransform)opacityObject.transform).sizeDelta = new Vector2(428f, 32f);

                var opacityIF = opacityObject.transform.GetChild(0).gameObject.GetComponent<InputField>();
                opacityIF.text = "";

                TriggerHelper.AddEventTriggerParams(opacityObject, TriggerHelper.ScrollDelta(opacityIF, max: 1f));
                TriggerHelper.IncreaseDecreaseButtons(opacityIF, max: 1f);

                var opacityInput = opacityObject.transform.GetChild(0).Find("input");
                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Opacity", "Input Field", opacityInput.gameObject, new List<Component>
                {
                    opacityInput.GetComponent<Image>(),
                }, true, 1, SpriteManager.RoundedSide.W));

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Text Opacity", "Input Field Text", opacityIF.textComponent.gameObject, new List<Component>
                {
                    opacityIF.textComponent,
                }));

                var opacityButtonLeft = opacityIF.transform.Find("<").GetComponent<Button>();

                Destroy(opacityButtonLeft.GetComponent<Animator>());
                opacityButtonLeft.transition = Selectable.Transition.ColorTint;

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor < Opacity", "Function 2", opacityButtonLeft.gameObject, new List<Component>
                {
                    opacityButtonLeft.GetComponent<Image>(),
                    opacityButtonLeft,
                }, isSelectable: true));

                var opacityButtonRight = opacityIF.transform.Find(">").GetComponent<Button>();

                Destroy(opacityButtonRight.GetComponent<Animator>());
                opacityButtonRight.transition = Selectable.Transition.ColorTint;

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor > Opacity", "Function 2", opacityButtonRight.gameObject, new List<Component>
                {
                    opacityButtonRight.GetComponent<Image>(),
                    opacityButtonRight,
                }, isSelectable: true));

                labelGenerator("Hue");

                var hueObject = zoom.Duplicate(parent, "hue");
                hueObject.transform.localScale = Vector3.one;
                var huePlaceholder = hueObject.transform.GetChild(0).Find("input/Placeholder").GetComponent<Text>();
                huePlaceholder.text = "Enter value... (Keep me empty to not set)";
                huePlaceholder.fontSize = 13;

                ((RectTransform)hueObject.transform).sizeDelta = new Vector2(428f, 32f);

                var hueIF = hueObject.transform.GetChild(0).gameObject.GetComponent<InputField>();
                hueIF.text = "";

                TriggerHelper.AddEventTriggerParams(hueObject, TriggerHelper.ScrollDelta(hueIF));
                TriggerHelper.IncreaseDecreaseButtons(hueIF);

                var hueInput = hueObject.transform.GetChild(0).Find("input");
                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Hue", "Input Field", hueInput.gameObject, new List<Component>
                {
                    hueInput.GetComponent<Image>(),
                }, true, 1, SpriteManager.RoundedSide.W));

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Text Hue", "Input Field Text", hueIF.textComponent.gameObject, new List<Component>
                {
                    hueIF.textComponent,
                }));

                var hueButtonLeft = hueIF.transform.Find("<").GetComponent<Button>();

                Destroy(hueButtonLeft.GetComponent<Animator>());
                hueButtonLeft.transition = Selectable.Transition.ColorTint;

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor < Hue", "Function 2", hueButtonLeft.gameObject, new List<Component>
                {
                    hueButtonLeft.GetComponent<Image>(),
                    hueButtonLeft,
                }, isSelectable: true));

                var hueButtonRight = hueIF.transform.Find(">").GetComponent<Button>();

                Destroy(hueButtonRight.GetComponent<Animator>());
                hueButtonRight.transition = Selectable.Transition.ColorTint;

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor > Hue", "Function 2", hueButtonRight.gameObject, new List<Component>
                {
                    hueButtonRight.GetComponent<Image>(),
                    hueButtonRight,
                }, isSelectable: true));

                labelGenerator("Saturation");

                var satObject = zoom.Duplicate(parent, "sat");
                satObject.transform.localScale = Vector3.one;
                var satPlaceholder = satObject.transform.GetChild(0).Find("input/Placeholder").GetComponent<Text>();
                satPlaceholder.text = "Enter value... (Keep me empty to not set)";
                satPlaceholder.fontSize = 13;

                ((RectTransform)satObject.transform).sizeDelta = new Vector2(428f, 32f);

                var satIF = satObject.transform.GetChild(0).gameObject.GetComponent<InputField>();
                satIF.text = "";

                TriggerHelper.AddEventTriggerParams(satObject, TriggerHelper.ScrollDelta(satIF));
                TriggerHelper.IncreaseDecreaseButtons(satIF);

                var satInput = satObject.transform.GetChild(0).Find("input");
                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Saturation", "Input Field", satInput.gameObject, new List<Component>
                {
                    satInput.GetComponent<Image>(),
                }, true, 1, SpriteManager.RoundedSide.W));

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Text Saturation", "Input Field Text", satIF.textComponent.gameObject, new List<Component>
                {
                    satIF.textComponent,
                }));

                var satButtonLeft = satIF.transform.Find("<").GetComponent<Button>();

                Destroy(satButtonLeft.GetComponent<Animator>());
                satButtonLeft.transition = Selectable.Transition.ColorTint;

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor < Saturation", "Function 2", satButtonLeft.gameObject, new List<Component>
                {
                    satButtonLeft.GetComponent<Image>(),
                    satButtonLeft,
                }, isSelectable: true));

                var satButtonRight = satIF.transform.Find(">").GetComponent<Button>();

                Destroy(satButtonRight.GetComponent<Animator>());
                satButtonRight.transition = Selectable.Transition.ColorTint;

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor > Saturation", "Function 2", satButtonRight.gameObject, new List<Component>
                {
                    satButtonRight.GetComponent<Image>(),
                    satButtonRight,
                }, isSelectable: true));

                labelGenerator("Value");

                var valObject = zoom.Duplicate(parent, "sat");
                valObject.transform.localScale = Vector3.one;
                var valPlaceholder = valObject.transform.GetChild(0).Find("input/Placeholder").GetComponent<Text>();
                valPlaceholder.text = "Enter value... (Keep me empty to not set)";
                valPlaceholder.fontSize = 13;

                ((RectTransform)valObject.transform).sizeDelta = new Vector2(428f, 32f);

                var valIF = valObject.transform.GetChild(0).gameObject.GetComponent<InputField>();
                valIF.text = "";

                TriggerHelper.AddEventTriggerParams(valObject, TriggerHelper.ScrollDelta(valIF));
                TriggerHelper.IncreaseDecreaseButtons(valIF);

                var valInput = valObject.transform.GetChild(0).Find("input");
                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Value", "Input Field", valInput.gameObject, new List<Component>
                {
                    valInput.GetComponent<Image>(),
                }, true, 1, SpriteManager.RoundedSide.W));

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Text Value", "Input Field Text", valIF.textComponent.gameObject, new List<Component>
                {
                    valIF.textComponent,
                }));

                var valButtonLeft = valIF.transform.Find("<").GetComponent<Button>();

                Destroy(valButtonLeft.GetComponent<Animator>());
                valButtonLeft.transition = Selectable.Transition.ColorTint;

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor < Value", "Function 2", valButtonLeft.gameObject, new List<Component>
                {
                    valButtonLeft.GetComponent<Image>(),
                    valButtonLeft,
                }, isSelectable: true));

                var valButtonRight = valIF.transform.Find(">").GetComponent<Button>();

                Destroy(valButtonRight.GetComponent<Animator>());
                valButtonRight.transition = Selectable.Transition.ColorTint;

                EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor > Value", "Function 2", valButtonRight.gameObject, new List<Component>
                {
                    valButtonRight.GetComponent<Image>(),
                    valButtonRight,
                }, isSelectable: true));

                labelGenerator("Ease Type");

                var curvesObject = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/position/curves").Duplicate(parent, "curves");
                var curves = curvesObject.GetComponent<Dropdown>();
                curves.onValueChanged.ClearAll();
                curves.options.Insert(0, new Dropdown.OptionData("None (Doesn't Set Easing)"));

                TriggerHelper.AddEventTriggerParams(curves.gameObject, TriggerHelper.CreateEntry(EventTriggerType.Scroll, delegate (BaseEventData baseEventData)
                {
                    if (!EditorConfig.Instance.ScrollOnEasing.Value)
                        return;

                    var pointerEventData = (PointerEventData)baseEventData;
                    if (pointerEventData.scrollDelta.y > 0f)
                        curves.value = curves.value == 0 ? curves.options.Count - 1 : curves.value - 1;
                    if (pointerEventData.scrollDelta.y < 0f)
                        curves.value = curves.value == curves.options.Count - 1 ? 0 : curves.value + 1;
                }));

                EditorThemeManager.AddElement(new EditorThemeManager.Element("Open File Popup Orderby", "Dropdown 1", curvesObject, new List<Component>
                {
                    curvesObject.GetComponent<Image>(),
                }, true, 1, SpriteManager.RoundedSide.W));

                EditorThemeManager.AddElement(new EditorThemeManager.Element("Open File Popup Orderby Text", "Dropdown 1 Overlay", curvesObject.transform.Find("Label").gameObject, new List<Component>
                {
                    curvesObject.transform.Find("Label").gameObject.GetComponent<Text>(),
                }));

                EditorThemeManager.AddElement(new EditorThemeManager.Element("Open File Popup Orderby Arrow", "Dropdown 1 Overlay", curvesObject.transform.Find("Arrow").gameObject, new List<Component>
                {
                    curvesObject.transform.Find("Arrow").gameObject.GetComponent<Image>(),
                }));

                if (curvesObject.transform.Find("Preview"))
                    EditorThemeManager.AddElement(new EditorThemeManager.Element("Open File Popup Orderby Preview", "Dropdown 1 Overlay", curvesObject.transform.Find("Preview").gameObject, new List<Component>
                    {
                        curvesObject.transform.Find("Preview").gameObject.GetComponent<Image>(),
                    }));

                var template = curvesObject.transform.Find("Template").gameObject;
                EditorThemeManager.AddElement(new EditorThemeManager.Element("Open File Popup Orderby Template", "Dropdown 1", template, new List<Component>
                {
                    template.GetComponent<Image>(),
                }, true, 1, SpriteManager.RoundedSide.Bottom));

                var templateItem = template.transform.Find("Viewport/Content/Item/Item Background").gameObject;
                EditorThemeManager.AddElement(new EditorThemeManager.Element("Open File Popup Orderby Template", "Dropdown 1 Item", templateItem, new List<Component>
                {
                    templateItem.GetComponent<Image>(),
                }, true, 1, SpriteManager.RoundedSide.W));

                var templateItemCheckmark = template.transform.Find("Viewport/Content/Item/Item Checkmark").gameObject;
                EditorThemeManager.AddElement(new EditorThemeManager.Element("Open File Popup Orderby Template Checkmark", "Dropdown 1 Overlay", templateItemCheckmark, new List<Component>
                {
                    templateItemCheckmark.GetComponent<Image>(),
                }));

                var templateItemLabel = template.transform.Find("Viewport/Content/Item/Item Label").gameObject;
                EditorThemeManager.AddElement(new EditorThemeManager.Element("Open File Popup Orderby Template Label", "Dropdown 1 Overlay", templateItemLabel, new List<Component>
                {
                    templateItemLabel.GetComponent<Text>(),
                }));

                // Assign to All
                {
                    labelGenerator("Assign to All Color Keyframes");

                    buttonGenerator("assign to all", "Assign", parent, delegate ()
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                        {
                            if (timelineObject.IsBeatmapObject)
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();

                                for (int i = 0; i < bm.events[3].Count; i++)
                                {
                                    var kf = bm.events[3][i];
                                    if (curves.value != 0 && DataManager.inst.AnimationListDictionary.ContainsKey(curves.value - 1))
                                        kf.curveType = DataManager.inst.AnimationListDictionary[curves.value - 1];
                                    kf.eventValues[0] = Mathf.Clamp(currentMultiColorSelection, 0, 18);
                                    if (!string.IsNullOrEmpty(opacityIF.text))
                                        kf.eventValues[1] = -Mathf.Clamp(Parser.TryParse(opacityIF.text, 1f), 0f, 1f) + 1f;
                                    if (!string.IsNullOrEmpty(hueIF.text))
                                        kf.eventValues[2] = Parser.TryParse(hueIF.text, 0f);
                                    if (!string.IsNullOrEmpty(satIF.text))
                                        kf.eventValues[3] = Parser.TryParse(satIF.text, 0f);
                                    if (!string.IsNullOrEmpty(valIF.text))
                                        kf.eventValues[4] = Parser.TryParse(valIF.text, 0f);
                                }

                                Updater.UpdateProcessor(bm, "Keyframes");
                            }
                        }
                    });
                }

                // Assign to Index
                {
                    labelGenerator("Assign to Index");

                    var gameObject = zoom.Duplicate(parent, "index");
                    gameObject.transform.localScale = Vector3.one;
                    gameObject.transform.GetChild(0).Find("input/Placeholder").GetComponent<Text>().text = "Enter index...";

                    ((RectTransform)gameObject.transform).sizeDelta = new Vector2(428f, 32f);

                    var indexIF = gameObject.transform.GetChild(0).gameObject.GetComponent<InputField>();
                    indexIF.text = "0";

                    TriggerHelper.AddEventTriggerParams(gameObject, TriggerHelper.ScrollDeltaInt(indexIF));
                    TriggerHelper.IncreaseDecreaseButtonsInt(indexIF);

                    var input = gameObject.transform.GetChild(0).Find("input");
                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Index", "Input Field", input.gameObject, new List<Component>
                    {
                        input.GetComponent<Image>(),
                    }, true, 1, SpriteManager.RoundedSide.W));

                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Text Index", "Input Field Text", indexIF.textComponent.gameObject, new List<Component>
                    {
                        indexIF.textComponent,
                    }));

                    var indexButtonLeft = indexIF.transform.Find("<").GetComponent<Button>();

                    Destroy(indexButtonLeft.GetComponent<Animator>());
                    indexButtonLeft.transition = Selectable.Transition.ColorTint;

                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor < Index", "Function 2", indexButtonLeft.gameObject, new List<Component>
                    {
                        indexButtonLeft.GetComponent<Image>(),
                        indexButtonLeft,
                    }, isSelectable: true));

                    var indexButtonRight = indexIF.transform.Find(">").GetComponent<Button>();

                    Destroy(indexButtonRight.GetComponent<Animator>());
                    indexButtonRight.transition = Selectable.Transition.ColorTint;

                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor > Index", "Function 2", indexButtonRight.gameObject, new List<Component>
                    {
                        indexButtonRight.GetComponent<Image>(),
                        indexButtonRight,
                    }, isSelectable: true));

                    buttonGenerator("assign to index", "Assign", parent, delegate ()
                    {
                        if (int.TryParse(indexIF.text, out int num))
                        {
                            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                            {
                                if (timelineObject.IsBeatmapObject)
                                {
                                    var bm = timelineObject.GetData<BeatmapObject>();

                                    var kf = bm.events[3][Mathf.Clamp(num, 0, bm.events[3].Count - 1)];
                                    if (curves.value != 0 && DataManager.inst.AnimationListDictionary.ContainsKey(curves.value - 1))
                                        kf.curveType = DataManager.inst.AnimationListDictionary[curves.value - 1];
                                    kf.eventValues[0] = Mathf.Clamp(currentMultiColorSelection, 0, 18);
                                    if (!string.IsNullOrEmpty(opacityIF.text))
                                        kf.eventValues[1] = -Mathf.Clamp(Parser.TryParse(opacityIF.text, 1f), 0f, 1f) + 1f;
                                    if (!string.IsNullOrEmpty(hueIF.text))
                                        kf.eventValues[2] = Parser.TryParse(hueIF.text, 0f);
                                    if (!string.IsNullOrEmpty(satIF.text))
                                        kf.eventValues[3] = Parser.TryParse(satIF.text, 0f);
                                    if (!string.IsNullOrEmpty(valIF.text))
                                        kf.eventValues[4] = Parser.TryParse(valIF.text, 0f);

                                    Updater.UpdateProcessor(bm, "Keyframes");
                                }
                            }
                        }
                    });
                }

                // Create Color Keyframe
                {
                    labelGenerator("Create Color Keyframe");

                    buttonGenerator("create", "Create", parent, delegate ()
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                        {
                            if (timelineObject.IsBeatmapObject)
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();

                                var currentTime = AudioManager.inst.CurrentAudioSource.time;

                                var index = bm.events[3].FindLastIndex(x => currentTime > bm.StartTime + x.eventTime);

                                if (index >= 0 && currentTime > bm.StartTime)
                                {
                                    var kf = EventKeyframe.DeepCopy((EventKeyframe)bm.events[3][index]);
                                    kf.eventTime = currentTime - bm.StartTime;
                                    if (curves.value != 0 && DataManager.inst.AnimationListDictionary.ContainsKey(curves.value - 1))
                                        kf.curveType = DataManager.inst.AnimationListDictionary[curves.value - 1];

                                    kf.eventValues[0] = Mathf.Clamp(currentMultiColorSelection, 0, 18);
                                    if (!string.IsNullOrEmpty(opacityIF.text))
                                        kf.eventValues[1] = -Mathf.Clamp(Parser.TryParse(opacityIF.text, 1f), 0f, 1f) + 1f;
                                    if (!string.IsNullOrEmpty(hueIF.text))
                                        kf.eventValues[2] = Parser.TryParse(hueIF.text, 0f);
                                    if (!string.IsNullOrEmpty(satIF.text))
                                        kf.eventValues[3] = Parser.TryParse(satIF.text, 0f);
                                    if (!string.IsNullOrEmpty(valIF.text))
                                        kf.eventValues[4] = Parser.TryParse(valIF.text, 0f);
                                    bm.events[3].Add(kf);
                                }

                                Updater.UpdateProcessor(bm, "Keyframes");
                                ObjectEditor.inst.RenderTimelineObject(ObjectEditor.inst.GetTimelineObject(bm));
                            }
                        }
                    });
                }
            }

            // Paste
            {
                labelGenerator("Paste Keyframe Data (All Types)");

                // All Types
                {
                    var pasteAllTypesToAllIndex = zoom.Duplicate(parent, "index");
                    pasteAllTypesToAllIndex.transform.localScale = Vector3.one;
                    pasteAllTypesToAllIndex.transform.GetChild(0).Find("input/Placeholder").GetComponent<Text>().text = "Enter index...";

                    ((RectTransform)pasteAllTypesToAllIndex.transform).sizeDelta = new Vector2(428f, 32f);

                    var pasteAllTypesToAllIndexIF = pasteAllTypesToAllIndex.transform.GetChild(0).gameObject.GetComponent<InputField>();
                    pasteAllTypesToAllIndexIF.text = "0";

                    TriggerHelper.AddEventTriggerParams(pasteAllTypesToAllIndex, TriggerHelper.ScrollDeltaInt(pasteAllTypesToAllIndexIF, max: int.MaxValue));
                    TriggerHelper.IncreaseDecreaseButtonsInt(pasteAllTypesToAllIndexIF, max: int.MaxValue);

                    var input = pasteAllTypesToAllIndex.transform.GetChild(0).Find("input");
                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Index", "Input Field", input.gameObject, new List<Component>
                    {
                        input.GetComponent<Image>(),
                    }, true, 1, SpriteManager.RoundedSide.W));

                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Text Index", "Input Field Text", pasteAllTypesToAllIndexIF.textComponent.gameObject, new List<Component>
                    {
                        pasteAllTypesToAllIndexIF.textComponent,
                    }));

                    var pasteAllTypesToAllIndexButtonLeft = pasteAllTypesToAllIndexIF.transform.Find("<").GetComponent<Button>();

                    Destroy(pasteAllTypesToAllIndexButtonLeft.GetComponent<Animator>());
                    pasteAllTypesToAllIndexButtonLeft.transition = Selectable.Transition.ColorTint;

                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor < Index", "Function 2", pasteAllTypesToAllIndexButtonLeft.gameObject, new List<Component>
                    {
                        pasteAllTypesToAllIndexButtonLeft.GetComponent<Image>(),
                        pasteAllTypesToAllIndexButtonLeft,
                    }, isSelectable: true));

                    var pasteAllTypesToAllIndexButtonRight = pasteAllTypesToAllIndexIF.transform.Find(">").GetComponent<Button>();

                    Destroy(pasteAllTypesToAllIndexButtonRight.GetComponent<Animator>());
                    pasteAllTypesToAllIndexButtonRight.transition = Selectable.Transition.ColorTint;

                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor > Index", "Function 2", pasteAllTypesToAllIndexButtonRight.gameObject, new List<Component>
                    {
                        pasteAllTypesToAllIndexButtonRight.GetComponent<Image>(),
                        pasteAllTypesToAllIndexButtonRight,
                    }, isSelectable: true));

                    var pasteAllTypesBase = new GameObject("paste all types");
                    pasteAllTypesBase.transform.SetParent(parent);
                    pasteAllTypesBase.transform.localScale = Vector3.one;

                    var pasteAllTypesBaseRT = pasteAllTypesBase.AddComponent<RectTransform>();
                    pasteAllTypesBaseRT.sizeDelta = new Vector2(390f, 32f);

                    var pasteAllTypesBaseHLG = pasteAllTypesBase.AddComponent<HorizontalLayoutGroup>();
                    pasteAllTypesBaseHLG.childControlHeight = false;
                    pasteAllTypesBaseHLG.childControlWidth = false;
                    pasteAllTypesBaseHLG.childForceExpandHeight = false;
                    pasteAllTypesBaseHLG.childForceExpandWidth = false;
                    pasteAllTypesBaseHLG.spacing = 8f;

                    var pasteAllTypesToAllObject = eventButton.Duplicate(pasteAllTypesBaseRT, name);
                    pasteAllTypesToAllObject.transform.localScale = Vector3.one;

                    ((RectTransform)pasteAllTypesToAllObject.transform).sizeDelta = new Vector2(180f, 32f);

                    var pasteAllTypesToAllText = pasteAllTypesToAllObject.transform.GetChild(0).GetComponent<Text>();
                    pasteAllTypesToAllText.text = "Paste to All";

                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor Button Paste", "Paste", pasteAllTypesToAllObject, new List<Component>
                    {
                        pasteAllTypesToAllObject.GetComponent<Image>()
                    }, true, 1, SpriteManager.RoundedSide.W));

                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor Button Text Paste", "Paste Text", pasteAllTypesToAllText.gameObject, new List<Component>
                    {
                        pasteAllTypesToAllText
                    }));

                    var pasteAllTypesToAll = pasteAllTypesToAllObject.GetComponent<Button>();
                    pasteAllTypesToAll.onClick.ClearAll();
                    pasteAllTypesToAll.onClick.AddListener(delegate ()
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                        {
                            if (timelineObject.IsBeatmapObject)
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                if (ObjectEditor.inst.CopiedPositionData != null)
                                    for (int i = 0; i < bm.events[0].Count; i++)
                                    {
                                        var kf = (EventKeyframe)bm.events[0][i];
                                        kf.curveType = ObjectEditor.inst.CopiedPositionData.curveType;
                                        kf.eventValues = ObjectEditor.inst.CopiedPositionData.eventValues.Copy();
                                        kf.eventRandomValues = ObjectEditor.inst.CopiedPositionData.eventRandomValues.Copy();
                                        kf.random = ObjectEditor.inst.CopiedPositionData.random;
                                        kf.relative = ObjectEditor.inst.CopiedPositionData.relative;

                                        ObjectEditor.inst.RenderKeyframes(bm);
                                        ObjectEditor.inst.RenderObjectKeyframesDialog(bm);
                                        Updater.UpdateProcessor(bm, "Keyframes");
                                        EditorManager.inst.DisplayNotification("Pasted position keyframe data to all keyframes.", 2f, EditorManager.NotificationType.Success);
                                    }
                                else
                                    EditorManager.inst.DisplayNotification("Position keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                                if (ObjectEditor.inst.CopiedScaleData != null)
                                    for (int i = 0; i < bm.events[1].Count; i++)
                                    {
                                        var kf = (EventKeyframe)bm.events[1][i];
                                        kf.curveType = ObjectEditor.inst.CopiedScaleData.curveType;
                                        kf.eventValues = ObjectEditor.inst.CopiedScaleData.eventValues.Copy();
                                        kf.eventRandomValues = ObjectEditor.inst.CopiedScaleData.eventRandomValues.Copy();
                                        kf.random = ObjectEditor.inst.CopiedScaleData.random;
                                        kf.relative = ObjectEditor.inst.CopiedScaleData.relative;

                                        ObjectEditor.inst.RenderKeyframes(bm);
                                        ObjectEditor.inst.RenderObjectKeyframesDialog(bm);
                                        Updater.UpdateProcessor(bm, "Keyframes");
                                        EditorManager.inst.DisplayNotification("Pasted scale keyframe data to all keyframes.", 2f, EditorManager.NotificationType.Success);
                                    }
                                else
                                    EditorManager.inst.DisplayNotification("Scale keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                                if (ObjectEditor.inst.CopiedRotationData != null)
                                    for (int i = 0; i < bm.events[2].Count; i++)
                                    {
                                        var kf = (EventKeyframe)bm.events[2][i];
                                        kf.curveType = ObjectEditor.inst.CopiedRotationData.curveType;
                                        kf.eventValues = ObjectEditor.inst.CopiedRotationData.eventValues.Copy();
                                        kf.eventRandomValues = ObjectEditor.inst.CopiedRotationData.eventRandomValues.Copy();
                                        kf.random = ObjectEditor.inst.CopiedRotationData.random;
                                        kf.relative = ObjectEditor.inst.CopiedRotationData.relative;

                                        ObjectEditor.inst.RenderKeyframes(bm);
                                        ObjectEditor.inst.RenderObjectKeyframesDialog(bm);
                                        Updater.UpdateProcessor(bm, "Keyframes");
                                        EditorManager.inst.DisplayNotification("Pasted rotation keyframe data to all keyframes.", 2f, EditorManager.NotificationType.Success);
                                    }
                                else
                                    EditorManager.inst.DisplayNotification("Rotation keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                                if (ObjectEditor.inst.CopiedColorData != null)
                                    for (int i = 0; i < bm.events[3].Count; i++)
                                    {
                                        var kf = (EventKeyframe)bm.events[3][i];
                                        kf.curveType = ObjectEditor.inst.CopiedColorData.curveType;
                                        kf.eventValues = ObjectEditor.inst.CopiedColorData.eventValues.Copy();
                                        kf.eventRandomValues = ObjectEditor.inst.CopiedColorData.eventRandomValues.Copy();
                                        kf.random = ObjectEditor.inst.CopiedColorData.random;
                                        kf.relative = ObjectEditor.inst.CopiedColorData.relative;

                                        ObjectEditor.inst.RenderKeyframes(bm);
                                        ObjectEditor.inst.RenderObjectKeyframesDialog(bm);
                                        Updater.UpdateProcessor(bm, "Keyframes");
                                        EditorManager.inst.DisplayNotification("Pasted color keyframe data to all keyframes.", 2f, EditorManager.NotificationType.Success);
                                    }
                                else
                                    EditorManager.inst.DisplayNotification("Color keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                            }
                        }
                    });

                    var pasteAllTypesToIndexObject = eventButton.Duplicate(pasteAllTypesBaseRT, name);
                    pasteAllTypesToIndexObject.transform.localScale = Vector3.one;

                    ((RectTransform)pasteAllTypesToIndexObject.transform).sizeDelta = new Vector2(180f, 32f);

                    var pasteAllTypesToIndexText = pasteAllTypesToIndexObject.transform.GetChild(0).GetComponent<Text>();
                    pasteAllTypesToIndexText.text = "Paste to Index";

                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor Button Paste", "Paste", pasteAllTypesToIndexObject, new List<Component>
                    {
                        pasteAllTypesToIndexObject.GetComponent<Image>()
                    }, true, 1, SpriteManager.RoundedSide.W));

                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor Button Text Paste", "Paste Text", pasteAllTypesToIndexText.gameObject, new List<Component>
                    {
                        pasteAllTypesToIndexText
                    }));

                    var pasteAllTypesToIndex = pasteAllTypesToIndexObject.GetComponent<Button>();
                    pasteAllTypesToIndex.onClick.ClearAll();
                    pasteAllTypesToIndex.onClick.AddListener(delegate ()
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                        {
                            if (timelineObject.IsBeatmapObject && int.TryParse(pasteAllTypesToAllIndexIF.text, out int num))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                if (ObjectEditor.inst.CopiedPositionData != null)
                                {
                                    var kf = (EventKeyframe)bm.events[0][Mathf.Clamp(num, 0, bm.events[0].Count - 1)];
                                    kf.curveType = ObjectEditor.inst.CopiedPositionData.curveType;
                                    kf.eventValues = ObjectEditor.inst.CopiedPositionData.eventValues.Copy();
                                    kf.eventRandomValues = ObjectEditor.inst.CopiedPositionData.eventRandomValues.Copy();
                                    kf.random = ObjectEditor.inst.CopiedPositionData.random;
                                    kf.relative = ObjectEditor.inst.CopiedPositionData.relative;

                                    ObjectEditor.inst.RenderKeyframes(bm);
                                    ObjectEditor.inst.RenderObjectKeyframesDialog(bm);
                                    Updater.UpdateProcessor(bm, "Keyframes");
                                    EditorManager.inst.DisplayNotification("Pasted position keyframe data to current selected keyframe.", 2f, EditorManager.NotificationType.Success);
                                }
                                else
                                    EditorManager.inst.DisplayNotification("Position keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                                if (ObjectEditor.inst.CopiedScaleData != null)
                                {
                                    var kf = (EventKeyframe)bm.events[1][Mathf.Clamp(num, 0, bm.events[1].Count - 1)];
                                    kf.curveType = ObjectEditor.inst.CopiedScaleData.curveType;
                                    kf.eventValues = ObjectEditor.inst.CopiedScaleData.eventValues.Copy();
                                    kf.eventRandomValues = ObjectEditor.inst.CopiedScaleData.eventRandomValues.Copy();
                                    kf.random = ObjectEditor.inst.CopiedScaleData.random;
                                    kf.relative = ObjectEditor.inst.CopiedScaleData.relative;

                                    ObjectEditor.inst.RenderKeyframes(bm);
                                    ObjectEditor.inst.RenderObjectKeyframesDialog(bm);
                                    Updater.UpdateProcessor(bm, "Keyframes");
                                    EditorManager.inst.DisplayNotification("Pasted scale keyframe data to all keyframes.", 2f, EditorManager.NotificationType.Success);
                                }
                                else
                                    EditorManager.inst.DisplayNotification("Scale keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                                if (ObjectEditor.inst.CopiedRotationData != null)
                                {
                                    var kf = (EventKeyframe)bm.events[2][Mathf.Clamp(num, 0, bm.events[2].Count - 1)];
                                    kf.curveType = ObjectEditor.inst.CopiedRotationData.curveType;
                                    kf.eventValues = ObjectEditor.inst.CopiedRotationData.eventValues.Copy();
                                    kf.eventRandomValues = ObjectEditor.inst.CopiedRotationData.eventRandomValues.Copy();
                                    kf.random = ObjectEditor.inst.CopiedRotationData.random;
                                    kf.relative = ObjectEditor.inst.CopiedRotationData.relative;

                                    ObjectEditor.inst.RenderKeyframes(bm);
                                    ObjectEditor.inst.RenderObjectKeyframesDialog(bm);
                                    Updater.UpdateProcessor(bm, "Keyframes");
                                    EditorManager.inst.DisplayNotification("Pasted rotation keyframe data to all keyframes.", 2f, EditorManager.NotificationType.Success);
                                }
                                else
                                    EditorManager.inst.DisplayNotification("Rotation keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                                if (ObjectEditor.inst.CopiedColorData != null)
                                {
                                    var kf = (EventKeyframe)bm.events[3][Mathf.Clamp(num, 0, bm.events[3].Count - 1)];
                                    kf.curveType = ObjectEditor.inst.CopiedColorData.curveType;
                                    kf.eventValues = ObjectEditor.inst.CopiedColorData.eventValues.Copy();
                                    kf.eventRandomValues = ObjectEditor.inst.CopiedColorData.eventRandomValues.Copy();
                                    kf.random = ObjectEditor.inst.CopiedColorData.random;
                                    kf.relative = ObjectEditor.inst.CopiedColorData.relative;

                                    ObjectEditor.inst.RenderKeyframes(bm);
                                    ObjectEditor.inst.RenderObjectKeyframesDialog(bm);
                                    Updater.UpdateProcessor(bm, "Keyframes");
                                    EditorManager.inst.DisplayNotification("Pasted color keyframe data to all keyframes.", 2f, EditorManager.NotificationType.Success);
                                }
                                else
                                    EditorManager.inst.DisplayNotification("Color keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                            }
                        }
                    });
                }

                labelGenerator("Paste Keyframe Data (Position)");

                // Position
                {
                    var pasteAllTypesToAllIndex = zoom.Duplicate(parent, "index");
                    pasteAllTypesToAllIndex.transform.localScale = Vector3.one;
                    pasteAllTypesToAllIndex.transform.GetChild(0).Find("input/Placeholder").GetComponent<Text>().text = "Enter index...";

                    ((RectTransform)pasteAllTypesToAllIndex.transform).sizeDelta = new Vector2(428f, 32f);

                    var pasteAllTypesToAllIndexIF = pasteAllTypesToAllIndex.transform.GetChild(0).gameObject.GetComponent<InputField>();
                    pasteAllTypesToAllIndexIF.text = "0";

                    TriggerHelper.AddEventTriggerParams(pasteAllTypesToAllIndex, TriggerHelper.ScrollDeltaInt(pasteAllTypesToAllIndexIF, max: int.MaxValue));
                    TriggerHelper.IncreaseDecreaseButtonsInt(pasteAllTypesToAllIndexIF, max: int.MaxValue);

                    var input = pasteAllTypesToAllIndex.transform.GetChild(0).Find("input");
                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Index", "Input Field", input.gameObject, new List<Component>
                    {
                        input.GetComponent<Image>(),
                    }, true, 1, SpriteManager.RoundedSide.W));

                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Text Index", "Input Field Text", pasteAllTypesToAllIndexIF.textComponent.gameObject, new List<Component>
                    {
                        pasteAllTypesToAllIndexIF.textComponent,
                    }));

                    var pasteAllTypesToAllIndexButtonLeft = pasteAllTypesToAllIndexIF.transform.Find("<").GetComponent<Button>();

                    Destroy(pasteAllTypesToAllIndexButtonLeft.GetComponent<Animator>());
                    pasteAllTypesToAllIndexButtonLeft.transition = Selectable.Transition.ColorTint;

                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor < Index", "Function 2", pasteAllTypesToAllIndexButtonLeft.gameObject, new List<Component>
                    {
                        pasteAllTypesToAllIndexButtonLeft.GetComponent<Image>(),
                        pasteAllTypesToAllIndexButtonLeft,
                    }, isSelectable: true));

                    var pasteAllTypesToAllIndexButtonRight = pasteAllTypesToAllIndexIF.transform.Find(">").GetComponent<Button>();

                    Destroy(pasteAllTypesToAllIndexButtonRight.GetComponent<Animator>());
                    pasteAllTypesToAllIndexButtonRight.transition = Selectable.Transition.ColorTint;

                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor > Index", "Function 2", pasteAllTypesToAllIndexButtonRight.gameObject, new List<Component>
                    {
                        pasteAllTypesToAllIndexButtonRight.GetComponent<Image>(),
                        pasteAllTypesToAllIndexButtonRight,
                    }, isSelectable: true));

                    var pasteAllTypesBase = new GameObject("paste position");
                    pasteAllTypesBase.transform.SetParent(parent);
                    pasteAllTypesBase.transform.localScale = Vector3.one;

                    var pasteAllTypesBaseRT = pasteAllTypesBase.AddComponent<RectTransform>();
                    pasteAllTypesBaseRT.sizeDelta = new Vector2(390f, 32f);

                    var pasteAllTypesBaseHLG = pasteAllTypesBase.AddComponent<HorizontalLayoutGroup>();
                    pasteAllTypesBaseHLG.childControlHeight = false;
                    pasteAllTypesBaseHLG.childControlWidth = false;
                    pasteAllTypesBaseHLG.childForceExpandHeight = false;
                    pasteAllTypesBaseHLG.childForceExpandWidth = false;
                    pasteAllTypesBaseHLG.spacing = 8f;

                    var pasteAllTypesToAllObject = eventButton.Duplicate(pasteAllTypesBaseRT, name);
                    pasteAllTypesToAllObject.transform.localScale = Vector3.one;

                    ((RectTransform)pasteAllTypesToAllObject.transform).sizeDelta = new Vector2(180f, 32f);

                    var pasteAllTypesToAllText = pasteAllTypesToAllObject.transform.GetChild(0).GetComponent<Text>();
                    pasteAllTypesToAllText.text = "Paste to All";

                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor Button Paste", "Paste", pasteAllTypesToAllObject, new List<Component>
                    {
                        pasteAllTypesToAllObject.GetComponent<Image>()
                    }, true, 1, SpriteManager.RoundedSide.W));

                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor Button Text Paste", "Paste Text", pasteAllTypesToAllText.gameObject, new List<Component>
                    {
                        pasteAllTypesToAllText
                    }));

                    var pasteAllTypesToAll = pasteAllTypesToAllObject.GetComponent<Button>();
                    pasteAllTypesToAll.onClick.ClearAll();
                    pasteAllTypesToAll.onClick.AddListener(delegate ()
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                        {
                            if (timelineObject.IsBeatmapObject)
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                if (ObjectEditor.inst.CopiedPositionData != null)
                                    for (int i = 0; i < bm.events[0].Count; i++)
                                    {
                                        var kf = (EventKeyframe)bm.events[0][i];
                                        kf.curveType = ObjectEditor.inst.CopiedPositionData.curveType;
                                        kf.eventValues = ObjectEditor.inst.CopiedPositionData.eventValues.Copy();
                                        kf.eventRandomValues = ObjectEditor.inst.CopiedPositionData.eventRandomValues.Copy();
                                        kf.random = ObjectEditor.inst.CopiedPositionData.random;
                                        kf.relative = ObjectEditor.inst.CopiedPositionData.relative;

                                        ObjectEditor.inst.RenderKeyframes(bm);
                                        ObjectEditor.inst.RenderObjectKeyframesDialog(bm);
                                        Updater.UpdateProcessor(bm, "Keyframes");
                                        EditorManager.inst.DisplayNotification("Pasted position keyframe data to current selected keyframe.", 2f, EditorManager.NotificationType.Success);
                                    }
                                else
                                    EditorManager.inst.DisplayNotification("Position keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                            }
                        }
                    });

                    var pasteAllTypesToIndexObject = eventButton.Duplicate(pasteAllTypesBaseRT, name);
                    pasteAllTypesToIndexObject.transform.localScale = Vector3.one;

                    ((RectTransform)pasteAllTypesToIndexObject.transform).sizeDelta = new Vector2(180f, 32f);

                    var pasteAllTypesToIndexText = pasteAllTypesToIndexObject.transform.GetChild(0).GetComponent<Text>();
                    pasteAllTypesToIndexText.text = "Paste to Index";

                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor Button Paste", "Paste", pasteAllTypesToIndexObject, new List<Component>
                    {
                        pasteAllTypesToIndexObject.GetComponent<Image>()
                    }, true, 1, SpriteManager.RoundedSide.W));

                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor Button Text Paste", "Paste Text", pasteAllTypesToIndexText.gameObject, new List<Component>
                    {
                        pasteAllTypesToIndexText
                    }));

                    var pasteAllTypesToIndex = pasteAllTypesToIndexObject.GetComponent<Button>();
                    pasteAllTypesToIndex.onClick.ClearAll();
                    pasteAllTypesToIndex.onClick.AddListener(delegate ()
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                        {
                            if (timelineObject.IsBeatmapObject && int.TryParse(pasteAllTypesToAllIndexIF.text, out int num))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                if (ObjectEditor.inst.CopiedPositionData != null)
                                {
                                    var kf = (EventKeyframe)bm.events[0][Mathf.Clamp(num, 0, bm.events[0].Count - 1)];
                                    kf.curveType = ObjectEditor.inst.CopiedPositionData.curveType;
                                    kf.eventValues = ObjectEditor.inst.CopiedPositionData.eventValues.Copy();
                                    kf.eventRandomValues = ObjectEditor.inst.CopiedPositionData.eventRandomValues.Copy();
                                    kf.random = ObjectEditor.inst.CopiedPositionData.random;
                                    kf.relative = ObjectEditor.inst.CopiedPositionData.relative;

                                    ObjectEditor.inst.RenderKeyframes(bm);
                                    ObjectEditor.inst.RenderObjectKeyframesDialog(bm);
                                    Updater.UpdateProcessor(bm, "Keyframes");
                                    EditorManager.inst.DisplayNotification("Pasted position keyframe data to current selected keyframe.", 2f, EditorManager.NotificationType.Success);
                                }
                                else
                                    EditorManager.inst.DisplayNotification("Position keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                            }
                        }
                    });
                }

                labelGenerator("Paste Keyframe Data (Scale)");

                // Scale
                {
                    var pasteAllTypesToAllIndex = zoom.Duplicate(parent, "index");
                    pasteAllTypesToAllIndex.transform.localScale = Vector3.one;
                    pasteAllTypesToAllIndex.transform.GetChild(0).Find("input/Placeholder").GetComponent<Text>().text = "Enter index...";

                    ((RectTransform)pasteAllTypesToAllIndex.transform).sizeDelta = new Vector2(428f, 32f);

                    var pasteAllTypesToAllIndexIF = pasteAllTypesToAllIndex.transform.GetChild(0).gameObject.GetComponent<InputField>();
                    pasteAllTypesToAllIndexIF.text = "0";

                    TriggerHelper.AddEventTriggerParams(pasteAllTypesToAllIndex, TriggerHelper.ScrollDeltaInt(pasteAllTypesToAllIndexIF, max: int.MaxValue));
                    TriggerHelper.IncreaseDecreaseButtonsInt(pasteAllTypesToAllIndexIF, max: int.MaxValue);

                    var input = pasteAllTypesToAllIndex.transform.GetChild(0).Find("input");
                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Index", "Input Field", input.gameObject, new List<Component>
                    {
                        input.GetComponent<Image>(),
                    }, true, 1, SpriteManager.RoundedSide.W));

                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Text Index", "Input Field Text", pasteAllTypesToAllIndexIF.textComponent.gameObject, new List<Component>
                    {
                        pasteAllTypesToAllIndexIF.textComponent,
                    }));

                    var pasteAllTypesToAllIndexButtonLeft = pasteAllTypesToAllIndexIF.transform.Find("<").GetComponent<Button>();

                    Destroy(pasteAllTypesToAllIndexButtonLeft.GetComponent<Animator>());
                    pasteAllTypesToAllIndexButtonLeft.transition = Selectable.Transition.ColorTint;

                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor < Index", "Function 2", pasteAllTypesToAllIndexButtonLeft.gameObject, new List<Component>
                    {
                        pasteAllTypesToAllIndexButtonLeft.GetComponent<Image>(),
                        pasteAllTypesToAllIndexButtonLeft,
                    }, isSelectable: true));

                    var pasteAllTypesToAllIndexButtonRight = pasteAllTypesToAllIndexIF.transform.Find(">").GetComponent<Button>();

                    Destroy(pasteAllTypesToAllIndexButtonRight.GetComponent<Animator>());
                    pasteAllTypesToAllIndexButtonRight.transition = Selectable.Transition.ColorTint;

                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor > Index", "Function 2", pasteAllTypesToAllIndexButtonRight.gameObject, new List<Component>
                    {
                        pasteAllTypesToAllIndexButtonRight.GetComponent<Image>(),
                        pasteAllTypesToAllIndexButtonRight,
                    }, isSelectable: true));

                    var pasteAllTypesBase = new GameObject("paste scale");
                    pasteAllTypesBase.transform.SetParent(parent);
                    pasteAllTypesBase.transform.localScale = Vector3.one;

                    var pasteAllTypesBaseRT = pasteAllTypesBase.AddComponent<RectTransform>();
                    pasteAllTypesBaseRT.sizeDelta = new Vector2(390f, 32f);

                    var pasteAllTypesBaseHLG = pasteAllTypesBase.AddComponent<HorizontalLayoutGroup>();
                    pasteAllTypesBaseHLG.childControlHeight = false;
                    pasteAllTypesBaseHLG.childControlWidth = false;
                    pasteAllTypesBaseHLG.childForceExpandHeight = false;
                    pasteAllTypesBaseHLG.childForceExpandWidth = false;
                    pasteAllTypesBaseHLG.spacing = 8f;

                    var pasteAllTypesToAllObject = eventButton.Duplicate(pasteAllTypesBaseRT, name);
                    pasteAllTypesToAllObject.transform.localScale = Vector3.one;

                    ((RectTransform)pasteAllTypesToAllObject.transform).sizeDelta = new Vector2(180f, 32f);

                    var pasteAllTypesToAllText = pasteAllTypesToAllObject.transform.GetChild(0).GetComponent<Text>();
                    pasteAllTypesToAllText.text = "Paste to All";

                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor Button Paste", "Paste", pasteAllTypesToAllObject, new List<Component>
                    {
                        pasteAllTypesToAllObject.GetComponent<Image>()
                    }, true, 1, SpriteManager.RoundedSide.W));

                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor Button Text Paste", "Paste Text", pasteAllTypesToAllText.gameObject, new List<Component>
                    {
                        pasteAllTypesToAllText
                    }));

                    var pasteAllTypesToAll = pasteAllTypesToAllObject.GetComponent<Button>();
                    pasteAllTypesToAll.onClick.ClearAll();
                    pasteAllTypesToAll.onClick.AddListener(delegate ()
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                        {
                            if (timelineObject.IsBeatmapObject)
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                if (ObjectEditor.inst.CopiedScaleData != null)
                                    for (int i = 0; i < bm.events[1].Count; i++)
                                    {
                                        var kf = (EventKeyframe)bm.events[1][i];
                                        kf.curveType = ObjectEditor.inst.CopiedScaleData.curveType;
                                        kf.eventValues = ObjectEditor.inst.CopiedScaleData.eventValues.Copy();
                                        kf.eventRandomValues = ObjectEditor.inst.CopiedScaleData.eventRandomValues.Copy();
                                        kf.random = ObjectEditor.inst.CopiedScaleData.random;
                                        kf.relative = ObjectEditor.inst.CopiedScaleData.relative;

                                        ObjectEditor.inst.RenderKeyframes(bm);
                                        ObjectEditor.inst.RenderObjectKeyframesDialog(bm);
                                        Updater.UpdateProcessor(bm, "Keyframes");
                                        EditorManager.inst.DisplayNotification("Pasted scale keyframe data to all keyframes.", 2f, EditorManager.NotificationType.Success);
                                    }
                                else
                                    EditorManager.inst.DisplayNotification("Scale keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                            }
                        }
                    });

                    var pasteAllTypesToIndexObject = eventButton.Duplicate(pasteAllTypesBaseRT, name);
                    pasteAllTypesToIndexObject.transform.localScale = Vector3.one;

                    ((RectTransform)pasteAllTypesToIndexObject.transform).sizeDelta = new Vector2(180f, 32f);

                    var pasteAllTypesToIndexText = pasteAllTypesToIndexObject.transform.GetChild(0).GetComponent<Text>();
                    pasteAllTypesToIndexText.text = "Paste to Index";

                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor Button Paste", "Paste", pasteAllTypesToIndexObject, new List<Component>
                    {
                        pasteAllTypesToIndexObject.GetComponent<Image>()
                    }, true, 1, SpriteManager.RoundedSide.W));

                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor Button Text Paste", "Paste Text", pasteAllTypesToIndexText.gameObject, new List<Component>
                    {
                        pasteAllTypesToIndexText
                    }));

                    var pasteAllTypesToIndex = pasteAllTypesToIndexObject.GetComponent<Button>();
                    pasteAllTypesToIndex.onClick.ClearAll();
                    pasteAllTypesToIndex.onClick.AddListener(delegate ()
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                        {
                            if (timelineObject.IsBeatmapObject && int.TryParse(pasteAllTypesToAllIndexIF.text, out int num))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                if (ObjectEditor.inst.CopiedScaleData != null)
                                {
                                    var kf = (EventKeyframe)bm.events[1][Mathf.Clamp(num, 0, bm.events[1].Count - 1)];
                                    kf.curveType = ObjectEditor.inst.CopiedScaleData.curveType;
                                    kf.eventValues = ObjectEditor.inst.CopiedScaleData.eventValues.Copy();
                                    kf.eventRandomValues = ObjectEditor.inst.CopiedScaleData.eventRandomValues.Copy();
                                    kf.random = ObjectEditor.inst.CopiedScaleData.random;
                                    kf.relative = ObjectEditor.inst.CopiedScaleData.relative;

                                    ObjectEditor.inst.RenderKeyframes(bm);
                                    ObjectEditor.inst.RenderObjectKeyframesDialog(bm);
                                    Updater.UpdateProcessor(bm, "Keyframes");
                                    EditorManager.inst.DisplayNotification("Pasted scale keyframe data to all keyframes.", 2f, EditorManager.NotificationType.Success);
                                }
                                else
                                    EditorManager.inst.DisplayNotification("Scale keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                            }
                        }
                    });
                }

                labelGenerator("Paste Keyframe Data (Rotation)");

                // Rotation
                {
                    var pasteAllTypesToAllIndex = zoom.Duplicate(parent, "index");
                    pasteAllTypesToAllIndex.transform.localScale = Vector3.one;
                    pasteAllTypesToAllIndex.transform.GetChild(0).Find("input/Placeholder").GetComponent<Text>().text = "Enter index...";

                    ((RectTransform)pasteAllTypesToAllIndex.transform).sizeDelta = new Vector2(428f, 32f);

                    var pasteAllTypesToAllIndexIF = pasteAllTypesToAllIndex.transform.GetChild(0).gameObject.GetComponent<InputField>();
                    pasteAllTypesToAllIndexIF.text = "0";

                    TriggerHelper.AddEventTriggerParams(pasteAllTypesToAllIndex, TriggerHelper.ScrollDeltaInt(pasteAllTypesToAllIndexIF, max: int.MaxValue));
                    TriggerHelper.IncreaseDecreaseButtonsInt(pasteAllTypesToAllIndexIF, max: int.MaxValue);

                    var input = pasteAllTypesToAllIndex.transform.GetChild(0).Find("input");
                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Index", "Input Field", input.gameObject, new List<Component>
                    {
                        input.GetComponent<Image>(),
                    }, true, 1, SpriteManager.RoundedSide.W));

                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Text Index", "Input Field Text", pasteAllTypesToAllIndexIF.textComponent.gameObject, new List<Component>
                    {
                        pasteAllTypesToAllIndexIF.textComponent,
                    }));

                    var pasteAllTypesToAllIndexButtonLeft = pasteAllTypesToAllIndexIF.transform.Find("<").GetComponent<Button>();

                    Destroy(pasteAllTypesToAllIndexButtonLeft.GetComponent<Animator>());
                    pasteAllTypesToAllIndexButtonLeft.transition = Selectable.Transition.ColorTint;

                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor < Index", "Function 2", pasteAllTypesToAllIndexButtonLeft.gameObject, new List<Component>
                    {
                        pasteAllTypesToAllIndexButtonLeft.GetComponent<Image>(),
                        pasteAllTypesToAllIndexButtonLeft,
                    }, isSelectable: true));

                    var pasteAllTypesToAllIndexButtonRight = pasteAllTypesToAllIndexIF.transform.Find(">").GetComponent<Button>();

                    Destroy(pasteAllTypesToAllIndexButtonRight.GetComponent<Animator>());
                    pasteAllTypesToAllIndexButtonRight.transition = Selectable.Transition.ColorTint;

                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor > Index", "Function 2", pasteAllTypesToAllIndexButtonRight.gameObject, new List<Component>
                    {
                        pasteAllTypesToAllIndexButtonRight.GetComponent<Image>(),
                        pasteAllTypesToAllIndexButtonRight,
                    }, isSelectable: true));

                    var pasteAllTypesBase = new GameObject("paste rotation");
                    pasteAllTypesBase.transform.SetParent(parent);
                    pasteAllTypesBase.transform.localScale = Vector3.one;

                    var pasteAllTypesBaseRT = pasteAllTypesBase.AddComponent<RectTransform>();
                    pasteAllTypesBaseRT.sizeDelta = new Vector2(390f, 32f);

                    var pasteAllTypesBaseHLG = pasteAllTypesBase.AddComponent<HorizontalLayoutGroup>();
                    pasteAllTypesBaseHLG.childControlHeight = false;
                    pasteAllTypesBaseHLG.childControlWidth = false;
                    pasteAllTypesBaseHLG.childForceExpandHeight = false;
                    pasteAllTypesBaseHLG.childForceExpandWidth = false;
                    pasteAllTypesBaseHLG.spacing = 8f;

                    var pasteAllTypesToAllObject = eventButton.Duplicate(pasteAllTypesBaseRT, name);
                    pasteAllTypesToAllObject.transform.localScale = Vector3.one;

                    ((RectTransform)pasteAllTypesToAllObject.transform).sizeDelta = new Vector2(180f, 32f);

                    var pasteAllTypesToAllText = pasteAllTypesToAllObject.transform.GetChild(0).GetComponent<Text>();
                    pasteAllTypesToAllText.text = "Paste to All";

                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor Button Paste", "Paste", pasteAllTypesToAllObject, new List<Component>
                    {
                        pasteAllTypesToAllObject.GetComponent<Image>()
                    }, true, 1, SpriteManager.RoundedSide.W));

                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor Button Text Paste", "Paste Text", pasteAllTypesToAllText.gameObject, new List<Component>
                    {
                        pasteAllTypesToAllText
                    }));

                    var pasteAllTypesToAll = pasteAllTypesToAllObject.GetComponent<Button>();
                    pasteAllTypesToAll.onClick.ClearAll();
                    pasteAllTypesToAll.onClick.AddListener(delegate ()
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                        {
                            if (timelineObject.IsBeatmapObject)
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                if (ObjectEditor.inst.CopiedRotationData != null)
                                    for (int i = 0; i < bm.events[2].Count; i++)
                                    {
                                        var kf = (EventKeyframe)bm.events[2][i];
                                        kf.curveType = ObjectEditor.inst.CopiedRotationData.curveType;
                                        kf.eventValues = ObjectEditor.inst.CopiedRotationData.eventValues.Copy();
                                        kf.eventRandomValues = ObjectEditor.inst.CopiedRotationData.eventRandomValues.Copy();
                                        kf.random = ObjectEditor.inst.CopiedRotationData.random;
                                        kf.relative = ObjectEditor.inst.CopiedRotationData.relative;

                                        ObjectEditor.inst.RenderKeyframes(bm);
                                        ObjectEditor.inst.RenderObjectKeyframesDialog(bm);
                                        Updater.UpdateProcessor(bm, "Keyframes");
                                        EditorManager.inst.DisplayNotification("Pasted rotation keyframe data to all keyframes.", 2f, EditorManager.NotificationType.Success);
                                    }
                                else
                                    EditorManager.inst.DisplayNotification("Rotation keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                            }
                        }
                    });

                    var pasteAllTypesToIndexObject = eventButton.Duplicate(pasteAllTypesBaseRT, name);
                    pasteAllTypesToIndexObject.transform.localScale = Vector3.one;

                    ((RectTransform)pasteAllTypesToIndexObject.transform).sizeDelta = new Vector2(180f, 32f);

                    var pasteAllTypesToIndexText = pasteAllTypesToIndexObject.transform.GetChild(0).GetComponent<Text>();
                    pasteAllTypesToIndexText.text = "Paste to Index";

                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor Button Paste", "Paste", pasteAllTypesToIndexObject, new List<Component>
                    {
                        pasteAllTypesToIndexObject.GetComponent<Image>()
                    }, true, 1, SpriteManager.RoundedSide.W));

                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor Button Text Paste", "Paste Text", pasteAllTypesToIndexText.gameObject, new List<Component>
                    {
                        pasteAllTypesToIndexText
                    }));

                    var pasteAllTypesToIndex = pasteAllTypesToIndexObject.GetComponent<Button>();
                    pasteAllTypesToIndex.onClick.ClearAll();
                    pasteAllTypesToIndex.onClick.AddListener(delegate ()
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                        {
                            if (timelineObject.IsBeatmapObject && int.TryParse(pasteAllTypesToAllIndexIF.text, out int num))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                if (ObjectEditor.inst.CopiedRotationData != null)
                                {
                                    var kf = (EventKeyframe)bm.events[2][Mathf.Clamp(num, 0, bm.events[2].Count - 1)];
                                    kf.curveType = ObjectEditor.inst.CopiedRotationData.curveType;
                                    kf.eventValues = ObjectEditor.inst.CopiedRotationData.eventValues.Copy();
                                    kf.eventRandomValues = ObjectEditor.inst.CopiedRotationData.eventRandomValues.Copy();
                                    kf.random = ObjectEditor.inst.CopiedRotationData.random;
                                    kf.relative = ObjectEditor.inst.CopiedRotationData.relative;

                                    ObjectEditor.inst.RenderKeyframes(bm);
                                    ObjectEditor.inst.RenderObjectKeyframesDialog(bm);
                                    Updater.UpdateProcessor(bm, "Keyframes");
                                    EditorManager.inst.DisplayNotification("Pasted rotation keyframe data to all keyframes.", 2f, EditorManager.NotificationType.Success);
                                }
                                else
                                    EditorManager.inst.DisplayNotification("Rotation keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                            }
                        }
                    });
                }

                labelGenerator("Paste Keyframe Data (Color)");

                // Color
                {
                    var pasteAllTypesToAllIndex = zoom.Duplicate(parent, "index");
                    pasteAllTypesToAllIndex.transform.localScale = Vector3.one;
                    pasteAllTypesToAllIndex.transform.GetChild(0).Find("input/Placeholder").GetComponent<Text>().text = "Enter index...";

                    ((RectTransform)pasteAllTypesToAllIndex.transform).sizeDelta = new Vector2(428f, 32f);

                    var pasteAllTypesToAllIndexIF = pasteAllTypesToAllIndex.transform.GetChild(0).gameObject.GetComponent<InputField>();
                    pasteAllTypesToAllIndexIF.text = "0";

                    TriggerHelper.AddEventTriggerParams(pasteAllTypesToAllIndex, TriggerHelper.ScrollDeltaInt(pasteAllTypesToAllIndexIF, max: int.MaxValue));
                    TriggerHelper.IncreaseDecreaseButtonsInt(pasteAllTypesToAllIndexIF, max: int.MaxValue);

                    var input = pasteAllTypesToAllIndex.transform.GetChild(0).Find("input");
                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Index", "Input Field", input.gameObject, new List<Component>
                    {
                        input.GetComponent<Image>(),
                    }, true, 1, SpriteManager.RoundedSide.W));

                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor InputField Text Index", "Input Field Text", pasteAllTypesToAllIndexIF.textComponent.gameObject, new List<Component>
                    {
                        pasteAllTypesToAllIndexIF.textComponent,
                    }));

                    var pasteAllTypesToAllIndexButtonLeft = pasteAllTypesToAllIndexIF.transform.Find("<").GetComponent<Button>();

                    Destroy(pasteAllTypesToAllIndexButtonLeft.GetComponent<Animator>());
                    pasteAllTypesToAllIndexButtonLeft.transition = Selectable.Transition.ColorTint;

                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor < Index", "Function 2", pasteAllTypesToAllIndexButtonLeft.gameObject, new List<Component>
                    {
                        pasteAllTypesToAllIndexButtonLeft.GetComponent<Image>(),
                        pasteAllTypesToAllIndexButtonLeft,
                    }, isSelectable: true));

                    var pasteAllTypesToAllIndexButtonRight = pasteAllTypesToAllIndexIF.transform.Find(">").GetComponent<Button>();

                    Destroy(pasteAllTypesToAllIndexButtonRight.GetComponent<Animator>());
                    pasteAllTypesToAllIndexButtonRight.transition = Selectable.Transition.ColorTint;

                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor > Index", "Function 2", pasteAllTypesToAllIndexButtonRight.gameObject, new List<Component>
                    {
                        pasteAllTypesToAllIndexButtonRight.GetComponent<Image>(),
                        pasteAllTypesToAllIndexButtonRight,
                    }, isSelectable: true));

                    var pasteAllTypesBase = new GameObject("paste color");
                    pasteAllTypesBase.transform.SetParent(parent);
                    pasteAllTypesBase.transform.localScale = Vector3.one;

                    var pasteAllTypesBaseRT = pasteAllTypesBase.AddComponent<RectTransform>();
                    pasteAllTypesBaseRT.sizeDelta = new Vector2(390f, 32f);

                    var pasteAllTypesBaseHLG = pasteAllTypesBase.AddComponent<HorizontalLayoutGroup>();
                    pasteAllTypesBaseHLG.childControlHeight = false;
                    pasteAllTypesBaseHLG.childControlWidth = false;
                    pasteAllTypesBaseHLG.childForceExpandHeight = false;
                    pasteAllTypesBaseHLG.childForceExpandWidth = false;
                    pasteAllTypesBaseHLG.spacing = 8f;

                    var pasteAllTypesToAllObject = eventButton.Duplicate(pasteAllTypesBaseRT, name);
                    pasteAllTypesToAllObject.transform.localScale = Vector3.one;

                    ((RectTransform)pasteAllTypesToAllObject.transform).sizeDelta = new Vector2(180f, 32f);

                    var pasteAllTypesToAllText = pasteAllTypesToAllObject.transform.GetChild(0).GetComponent<Text>();
                    pasteAllTypesToAllText.text = "Paste to All";

                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor Button Paste", "Paste", pasteAllTypesToAllObject, new List<Component>
                    {
                        pasteAllTypesToAllObject.GetComponent<Image>()
                    }, true, 1, SpriteManager.RoundedSide.W));

                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor Button Text Paste", "Paste Text", pasteAllTypesToAllText.gameObject, new List<Component>
                    {
                        pasteAllTypesToAllText
                    }));

                    var pasteAllTypesToAll = pasteAllTypesToAllObject.GetComponent<Button>();
                    pasteAllTypesToAll.onClick.ClearAll();
                    pasteAllTypesToAll.onClick.AddListener(delegate ()
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                        {
                            if (timelineObject.IsBeatmapObject)
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                if (ObjectEditor.inst.CopiedColorData != null)
                                    for (int i = 0; i < bm.events[3].Count; i++)
                                    {
                                        var kf = (EventKeyframe)bm.events[3][i];
                                        kf.curveType = ObjectEditor.inst.CopiedColorData.curveType;
                                        kf.eventValues = ObjectEditor.inst.CopiedColorData.eventValues.Copy();
                                        kf.eventRandomValues = ObjectEditor.inst.CopiedColorData.eventRandomValues.Copy();
                                        kf.random = ObjectEditor.inst.CopiedColorData.random;
                                        kf.relative = ObjectEditor.inst.CopiedColorData.relative;

                                        ObjectEditor.inst.RenderKeyframes(bm);
                                        ObjectEditor.inst.RenderObjectKeyframesDialog(bm);
                                        Updater.UpdateProcessor(bm, "Keyframes");
                                        EditorManager.inst.DisplayNotification("Pasted color keyframe data to all keyframes.", 2f, EditorManager.NotificationType.Success);
                                    }
                                else
                                    EditorManager.inst.DisplayNotification("Color keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                            }
                        }
                    });

                    var pasteAllTypesToIndexObject = eventButton.Duplicate(pasteAllTypesBaseRT, name);
                    pasteAllTypesToIndexObject.transform.localScale = Vector3.one;

                    ((RectTransform)pasteAllTypesToIndexObject.transform).sizeDelta = new Vector2(180f, 32f);

                    var pasteAllTypesToIndexText = pasteAllTypesToIndexObject.transform.GetChild(0).GetComponent<Text>();
                    pasteAllTypesToIndexText.text = "Paste to Index";

                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor Button Paste", "Paste", pasteAllTypesToIndexObject, new List<Component>
                    {
                        pasteAllTypesToIndexObject.GetComponent<Image>()
                    }, true, 1, SpriteManager.RoundedSide.W));

                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Multi Object Editor Button Text Paste", "Paste Text", pasteAllTypesToIndexText.gameObject, new List<Component>
                    {
                        pasteAllTypesToIndexText
                    }));

                    var pasteAllTypesToIndex = pasteAllTypesToIndexObject.GetComponent<Button>();
                    pasteAllTypesToIndex.onClick.ClearAll();
                    pasteAllTypesToIndex.onClick.AddListener(delegate ()
                    {
                        foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                        {
                            if (timelineObject.IsBeatmapObject && int.TryParse(pasteAllTypesToAllIndexIF.text, out int num))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                if (ObjectEditor.inst.CopiedColorData != null)
                                {
                                    var kf = (EventKeyframe)bm.events[3][Mathf.Clamp(num, 0, bm.events[3].Count - 1)];
                                    kf.curveType = ObjectEditor.inst.CopiedColorData.curveType;
                                    kf.eventValues = ObjectEditor.inst.CopiedColorData.eventValues.Copy();
                                    kf.eventRandomValues = ObjectEditor.inst.CopiedColorData.eventRandomValues.Copy();
                                    kf.random = ObjectEditor.inst.CopiedColorData.random;
                                    kf.relative = ObjectEditor.inst.CopiedColorData.relative;

                                    ObjectEditor.inst.RenderKeyframes(bm);
                                    ObjectEditor.inst.RenderObjectKeyframesDialog(bm);
                                    Updater.UpdateProcessor(bm, "Keyframes");
                                    EditorManager.inst.DisplayNotification("Pasted color keyframe data to all keyframes.", 2f, EditorManager.NotificationType.Success);
                                }
                                else
                                    EditorManager.inst.DisplayNotification("Color keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                            }
                        }
                    });
                }
            }

            EditorManager.inst.GetDialog("Multi Object Editor").Dialog.Find("data").AsRT().sizeDelta = new Vector2(810f, 730.11f);
            EditorManager.inst.GetDialog("Multi Object Editor").Dialog.Find("data/left").AsRT().sizeDelta = new Vector2(355f, 730f);
        }

        List<MultiColorButton> multiColorButtons = new List<MultiColorButton>();
        class MultiColorButton
        {
            public Button Button { get; set; }
            public Image Image { get; set; }
            public GameObject Selected { get; set; }
        }

        int currentMultiColorSelection = 0;

        void UpdateMultiColorButtons()
        {
            for (int i = 0; i < multiColorButtons.Count; i++)
            {
                var multiColorButton = multiColorButtons[i];
                multiColorButton.Selected.gameObject.SetActive(currentMultiColorSelection == i);
            }
        }

        public void CreatePropertiesWindow()
        {
            var editorProperties = Instantiate(EditorManager.inst.GetDialog("Object Selector").Dialog.gameObject);
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
                editorProperties.transform.Find("mask/content").GetComponent<GridLayoutGroup>().cellSize = new Vector2(737f, 32f);
                editorProperties.transform.Find("mask/content").AsRT().anchorMin = new Vector2(0.01f, 1f);
                editorProperties.transform.Find("mask/content").AsRT().anchorMax = new Vector2(0.01f, 1f);
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
                categoryTabGenerator("Animations", LSColors.HexToColor("4076DF"), EditorProperty.EditorPropCategory.Animations);
                categoryTabGenerator("Fields", LSColors.HexToColor("6CCBCF"), EditorProperty.EditorPropCategory.Fields);
                categoryTabGenerator("Preview", LSColors.HexToColor("1B1B1C"), EditorProperty.EditorPropCategory.Preview);
            }

            EditorHelper.AddEditorDropdown("Preferences", "", "Edit", SpriteManager.LoadSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_preferences-white.png"), delegate ()
            {
                OpenPropertiesWindow();
            });

            editorProperties.transform.Find("Panel/x").GetComponent<Button>().onClick.RemoveAllListeners();
            editorProperties.transform.Find("Panel/x").GetComponent<Button>().onClick.AddListener(delegate ()
            {
                ClosePropertiesWindow();
            });

            EditorHelper.AddEditorPopup("Editor Properties Popup", editorProperties);
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
                objectSearchPanel.transform.Find("Text").GetComponent<Text>().text = "Debugger (Only use this if you know what you're doing)";
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

                // Inspect LevelProcessor
                {
                    string name = "LevelProcessor";
                    var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(objectSearch.transform.Find("mask/content"), "Function");
                    debugs.Add($"Inspect {name}");

                    var htt = gameObject.AddComponent<HoverTooltip>();

                    var levelTip = new HoverTooltip.Tooltip();

                    levelTip.desc = $"Inspect {name}";
                    levelTip.hint = "LevelProcessor is the main handler for updating object animation and spawning / despawning objects.";
                    htt.tooltipLangauges.Add(levelTip);

                    var button = gameObject.GetComponent<Button>();
                    button.onClick.ClearAll();
                    button.onClick.AddListener(delegate ()
                    {
                        uiManager.GetProperty("ShowMenu").SetValue(uiManager, true);
                        inspector.GetMethod("Inspect", new[] { typeof(object), AccessTools.TypeByName("UnityExplorer.CacheObject.CacheObjectBase") })
                        .Invoke(inspector, new object[] { Updater.levelProcessor, null });
                    });
                    gameObject.transform.GetChild(0).GetComponent<Text>().text = $"Inspect {name}";
                }

                // Inspect GameData
                {
                    string name = "GameData";
                    var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(objectSearch.transform.Find("mask/content"), "Function");
                    debugs.Add($"Inspect {name}");

                    var htt = gameObject.AddComponent<HoverTooltip>();

                    var levelTip = new HoverTooltip.Tooltip();

                    levelTip.desc = $"Inspect {name}";
                    levelTip.hint = "GameData stores all the level data.";
                    htt.tooltipLangauges.Add(levelTip);

                    var button = gameObject.GetComponent<Button>();
                    button.onClick.ClearAll();
                    button.onClick.AddListener(delegate ()
                    {
                        uiManager.GetProperty("ShowMenu").SetValue(uiManager, true);
                        inspector.GetMethod("Inspect", new[] { typeof(object), AccessTools.TypeByName("UnityExplorer.CacheObject.CacheObjectBase") })
                        .Invoke(inspector, new object[] { GameData.Current, null });
                    });
                    gameObject.transform.GetChild(0).GetComponent<Text>().text = $"Inspect {name}";
                }

                // Inspect Current Event Keyframe
                {
                    string name = "Current Event Keyframe";
                    var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(objectSearch.transform.Find("mask/content"), "Function");
                    debugs.Add($"Inspect {name}");

                    var htt = gameObject.AddComponent<HoverTooltip>();

                    var levelTip = new HoverTooltip.Tooltip();

                    levelTip.desc = $"Inspect {name}";
                    levelTip.hint = "The current selected Event Keyframe. Based on the type and index number.";
                    htt.tooltipLangauges.Add(levelTip);

                    var button = gameObject.GetComponent<Button>();
                    button.onClick.ClearAll();
                    button.onClick.AddListener(delegate ()
                    {
                        if (EventEditor.inst.currentEventType >= GameData.Current.eventObjects.allEvents.Count || EventEditor.inst.currentEvent >= GameData.Current.eventObjects.allEvents[EventEditor.inst.currentEventType].Count )
                            return;

                        uiManager.GetProperty("ShowMenu").SetValue(uiManager, true);
                        inspector.GetMethod("Inspect", new[] { typeof(object), AccessTools.TypeByName("UnityExplorer.CacheObject.CacheObjectBase") })
                        .Invoke(inspector, new object[] { GameData.Current.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent], null });
                    });
                    gameObject.transform.GetChild(0).GetComponent<Text>().text = $"Inspect {name}";
                }

                var functions = RTFile.ApplicationDirectory + "beatmaps/functions";
                if (!RTFile.DirectoryExists(functions))
                    return;

                var files = Directory.GetFiles(functions, "*.cs");
                for (int i = 0; i < files.Length; i++)
                {
                    var file = files[i];

                    var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(objectSearch.transform.Find("mask/content"), "Function");
                    debugs.Add($"Custom Code Function: {file}");

                    var htt = gameObject.AddComponent<HoverTooltip>();

                    var levelTip = new HoverTooltip.Tooltip();

                    levelTip.desc = $"{file}";
                    levelTip.hint = "A custom code file. Make sure you know what you're doing before using this.";
                    htt.tooltipLangauges.Add(levelTip);

                    var button = gameObject.GetComponent<Button>();
                    button.onClick.ClearAll();
                    button.onClick.AddListener(delegate ()
                    {
                        RTCode.Evaluate(RTFile.ReadFromFile(file));
                    });
                    gameObject.transform.GetChild(0).GetComponent<Text>().text = $"Custom Code Function: {file}";
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

        public void SetFileInfo(string text) => fileInfoText?.SetText(text);

        public bool themesLoading = false;

        public bool autoSaving = false;

        public IEnumerator LoadLevels()
        {
            EditorManager.inst.loadedLevels.Clear();

            var config = EditorConfig.Instance;

            var olfnm = config.OpenLevelFolderNameMax;
            var olsnm = config.OpenLevelSongNameMax;
            var olanm = config.OpenLevelArtistNameMax;
            var olcnm = config.OpenLevelCreatorNameMax;
            var oldem = config.OpenLevelDescriptionMax;
            var oldam = config.OpenLevelDateMax;

            int foldClamp = olfnm.Value < 3 ? olfnm.Value : (int)olfnm.DefaultValue;
            int songClamp = olsnm.Value < 3 ? olsnm.Value : (int)olsnm.DefaultValue;
            int artiClamp = olanm.Value < 3 ? olanm.Value : (int)olanm.DefaultValue;
            int creaClamp = olcnm.Value < 3 ? olcnm.Value : (int)olcnm.DefaultValue;
            int descClamp = oldem.Value < 3 ? oldem.Value : (int)oldem.DefaultValue;
            int dateClamp = oldam.Value < 3 ? oldam.Value : (int)oldam.DefaultValue;

            var transform = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("mask/content");
            var close = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("Panel/x");

            var horizontalOverflow = config.OpenLevelTextHorizontalWrap.Value;
            var verticalOverflow = config.OpenLevelTextVerticalWrap.Value;
            var fontSize = config.OpenLevelTextFontSize.Value;
            var format = config.OpenLevelTextFormatting.Value;
            var buttonHoverSize = config.OpenLevelButtonHoverSize.Value;

            var iconPosition = config.OpenLevelCoverPosition.Value;
            var iconScale = config.OpenLevelCoverScale.Value;

            var showDeleteButton = config.OpenLevelShowDeleteButton.Value;

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

                    var button = gameObject.GetComponent<Button>();
                    button.onClick.AddListener(delegate ()
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

                    EditorThemeManager.ApplyElement(new EditorThemeManager.Element($"Level Button {num}", "List Button 1", gameObject, new List<Component>
                    {
                        gameObject.GetComponent<Image>(),
                        button,
                    }, true, 1, SpriteManager.RoundedSide.W, true));

                    EditorThemeManager.ApplyElement(new EditorThemeManager.Element($"Level Button {num} Text", "Light Text", text.gameObject, new List<Component>
                    {
                        text,
                    }));

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
                    if (anyFailed && config.ShowLevelsWithoutCoverNotification.Value)
                        EditorManager.inst.DisplayNotification($"Levels {FontManager.TextTranslater.ArrayToString(failedLevels.ToArray())} do not have covers!", 2f * (failedLevels.Count * 0.10f), EditorManager.NotificationType.Error);
                    if (EditorManager.inst.loadedLevels.Count > 0)
                    {
                        EditorManager.inst.ShowDialog("Open File Popup");
                        EditorManager.inst.RenderOpenBeatmapPopup();
                    }
                    else
                        EditorManager.inst.OpenNewLevelPopup();
                }));
            else
            {
                if (anyFailed && config.ShowLevelsWithoutCoverNotification.Value)
                    EditorManager.inst.DisplayNotification($"Levels {FontManager.TextTranslater.ArrayToString(failedLevels.ToArray())} do not have covers!", 2f * (failedLevels.Count * 0.10f), EditorManager.NotificationType.Error);
                if (EditorManager.inst.loadedLevels.Count > 0)
                {
                    EditorManager.inst.ShowDialog("Open File Popup");
                    EditorManager.inst.RenderOpenBeatmapPopup();
                }
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

            RTPlayer.LockBoost = false;
            RTPlayer.SpeedMultiplier = 1f;

            WindowController.ResetTitle();

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

                    if (MetaData.Current.arcadeID == null || MetaData.Current.arcadeID == "0" || MetaData.Current.arcadeID == "-1")
                    {
                        MetaData.Current.arcadeID = LSText.randomNumString(16);
                        DataManager.inst.SaveMetadata(fullPath + "/metadata.lsb");
                    }

                    if (DataManager.inst.metaData.beatmap.game_version != "4.1.16" && DataManager.inst.metaData.beatmap.game_version != "20.4.4")
                        rawJSON = DataManager.inst.gameData.UpdateBeatmap(rawJSON, DataManager.inst.metaData.beatmap.game_version);

                    DataManager.inst.gameData = GameData.Parse(JSON.Parse(rawJSON), false);
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
                    SetFileInfo($"Both level.lsb and metadata.lsb are empty or corrupt.");

                EditorManager.inst.DisplayNotification("Level could not load.", 3f, EditorManager.NotificationType.Error);

                yield break;
            }

            PreviewCover?.GameObject?.SetActive(false);

            if (ModCompatibility.CreativePlayersInstalled)
            {
                PlayerManager.LoadGlobalModels?.Invoke();
                PlayerManager.LoadIndexes?.Invoke();
                PlayerManager.RespawnPlayers();
            }

            SetFileInfo($"Loading Themes for [ {name} ]");
            /*yield return */StartCoroutine(LoadThemes());

            Debug.Log($"{EditorPlugin.className}Music is null: {song == null}");

            SetFileInfo($"Playing Music for [ {name} ]\n\nIf it doesn't, then something went wrong!");
            AudioManager.inst.PlayMusic(null, song, true, 0f, true);
            StartCoroutine(EditorManager.inst.SpawnPlayersWithDelay(0.2f));
            if (EditorConfig.Instance.WaveformGenerate.Value)
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

            StartCoroutine(Updater.IUpdateObjects(true));

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

            SetAutoSave();

            TriggerHelper.AddEventTrigger(timeIF.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(timeIF, max: AudioManager.inst.CurrentAudioSource.clip.length) });

            // Load Settings like timeline position, editor layer, bpm active, etc
            LoadSettings();

            if (EditorConfig.Instance.LevelPausesOnStart.Value)
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
            if (DataManager.inst.gameData is GameData)
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

            var search = EventEditor.inst.dialogRight.GetChild(4).Find("theme-search").GetComponent<InputField>().text;
            var files = Directory.GetFiles(RTFile.ApplicationDirectory + themeListPath, "*.lst");
            foreach (var file in files)
            {
                var jn = JSON.Parse(RTFile.ReadFromFile(file));
                var orig = BeatmapTheme.Parse(jn);
                orig.filePath = file.Replace("\\", "/");
                DataManager.inst.CustomBeatmapThemes.Add(orig);

                if (jn["id"] != null && DataManager.inst.gameData is GameData && GameData.Current.beatmapThemes != null && !GameData.Current.beatmapThemes.ContainsKey(jn["id"]))
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

                    themePanel.SetActive(RTHelpers.SearchString(themePanel.Theme.name, search));
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

            var config = EditorConfig.Instance;

            var hoverSize = config.PrefabButtonHoverSize.Value;

            var hover = gameObject.AddComponent<HoverUI>();
            hover.animateSca = true;
            hover.animatePos = false;
            hover.size = hoverSize;

            gameObject.GetComponentAndPerformAction(delegate (Button x)
            {
                x.NewOnClickListener(delegate ()
                {
                    if (PrefabEditorManager.inst.savingToPrefab && PrefabEditorManager.inst.prefabToSaveFrom != null)
                    {
                        PrefabEditorManager.inst.savingToPrefab = false;
                        PrefabEditorManager.inst.SavePrefab(PrefabEditorManager.inst.prefabToSaveFrom);

                        EditorManager.inst.HideDialog("Prefab Popup");

                        PrefabEditorManager.inst.prefabToSaveFrom = null;

                        EditorManager.inst.DisplayNotification("Applied all changes to External Prefab.", 2f, EditorManager.NotificationType.Success);

                        return;
                    }

                    PrefabEditor.inst.OpenDialog();
                    PrefabEditorManager.inst.createInternal = false;
                });
            });

            bool isExternal = true;

            var nameHorizontalOverflow = isExternal ? config.PrefabExternalNameHorizontalWrap.Value : config.PrefabInternalNameHorizontalWrap.Value;

            var nameVerticalOverflow = isExternal ? config.PrefabExternalNameVerticalWrap.Value : config.PrefabInternalNameVerticalWrap.Value;

            var nameFontSize = isExternal ? config.PrefabExternalNameFontSize.Value : config.PrefabInternalNameFontSize.Value;

            var typeHorizontalOverflow = isExternal ? config.PrefabExternalTypeHorizontalWrap.Value : config.PrefabInternalTypeHorizontalWrap.Value;

            var typeVerticalOverflow = isExternal ? config.PrefabExternalTypeVerticalWrap.Value : config.PrefabInternalTypeVerticalWrap.Value;

            var typeFontSize = isExternal ? config.PrefabExternalTypeFontSize.Value : config.PrefabInternalTypeFontSize.Value;

            var deleteAnchoredPosition = isExternal ? config.PrefabExternalDeleteButtonPos.Value : config.PrefabInternalDeleteButtonPos.Value;
            var deleteSizeDelta = isExternal ? config.PrefabExternalDeleteButtonSca.Value : config.PrefabInternalDeleteButtonSca.Value;

            while (PrefabEditorManager.loadingPrefabTypes)
                yield return null;

            int num = 0;
            foreach (var file in Directory.GetFiles(RTFile.ApplicationDirectory + prefabListPath, "*.lsp", SearchOption.TopDirectoryOnly))
            {
                var jn = JSON.Parse(RTFile.ReadFromFile(file));

                var prefab = Prefab.Parse(jn);
                prefab.objects.ForEach(x => { x.prefabID = ""; x.prefabInstanceID = ""; });
                prefab.filePath = file.Replace("\\", "/");

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
            yield return inst.StartCoroutine(LoadPrefabs(PrefabEditor.inst));
            PrefabEditor.inst.ReloadExternalPrefabsInPopup();
            EditorManager.inst.DisplayNotification("Updated external prefabs!", 2f, EditorManager.NotificationType.Success);
            yield break;
        }

        public void SetAutoSave()
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

            EditorManager.inst.CancelInvoke("AutoSaveLevel");
            CancelInvoke("AutoSaveLevel");
            InvokeRepeating("AutoSaveLevel", EditorConfig.Instance.AutosaveLoopTime.Value, EditorConfig.Instance.AutosaveLoopTime.Value);
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

            while (EditorManager.inst.autosaves.Count > EditorConfig.Instance.AutosaveLimit.Value)
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

        public static float timeSinceAutosaved;

        public void CreateNewLevel()
        {
            if (string.IsNullOrEmpty(EditorManager.inst.newAudioFile))
            {
                EditorManager.inst.DisplayNotification("The file path is empty.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            if (!EditorManager.inst.newAudioFile.ToLower().Contains(".ogg") && !EditorManager.inst.newAudioFile.ToLower().Contains(".wav") && !EditorManager.inst.newAudioFile.ToLower().Contains(".mp3"))
            {
                EditorManager.inst.DisplayNotification("The file you are trying to load doesn't appear to be a song file.", 2f, EditorManager.NotificationType.Error);
                return;
            }
            if (!RTFile.FileExists(EditorManager.inst.newAudioFile))
            {
                EditorManager.inst.DisplayNotification("The file you are trying to load doesn't appear to exist.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            bool setNew = false;
            int num = 0;
            string p = RTFile.ApplicationDirectory + editorListSlash + EditorManager.inst.newLevelName;
            while (RTFile.DirectoryExists(p))
            {
                p = RTFile.ApplicationDirectory + editorListSlash + EditorManager.inst.newLevelName + " - " + num.ToString();
                num += 1;
                setNew = true;

            }
            if (setNew)
                EditorManager.inst.newLevelName += " - " + num.ToString();

            if (RTFile.DirectoryExists(RTFile.ApplicationDirectory + editorListSlash + EditorManager.inst.newLevelName))
            {
                EditorManager.inst.DisplayNotification("The level you are trying to create already exists.", 2f, EditorManager.NotificationType.Error);
                return;
            }
            Directory.CreateDirectory(RTFile.ApplicationDirectory + editorListSlash + EditorManager.inst.newLevelName);

            if (EditorManager.inst.newAudioFile.ToLower().Contains(".ogg"))
            {
                string destFileName = RTFile.ApplicationDirectory + editorListSlash + EditorManager.inst.newLevelName + "/level.ogg";
                File.Copy(EditorManager.inst.newAudioFile, destFileName, true);
            }
            if (EditorManager.inst.newAudioFile.ToLower().Contains(".wav"))
            {
                string destFileName = RTFile.ApplicationDirectory + editorListSlash + EditorManager.inst.newLevelName + "/level.wav";
                File.Copy(EditorManager.inst.newAudioFile, destFileName, true);
            }
            if (EditorManager.inst.newAudioFile.ToLower().Contains(".mp3"))
            {
                string destFileName = RTFile.ApplicationDirectory + editorListSlash + EditorManager.inst.newLevelName + "/level.mp3";
                File.Copy(EditorManager.inst.newAudioFile, destFileName, true);
            }

            var json = currentLevelTemplate >= 0 && currentLevelTemplate < NewLevelTemplates.Count && RTFile.FileExists(NewLevelTemplates[currentLevelTemplate].filePath) ? RTFile.ReadFromFile(NewLevelTemplates[currentLevelTemplate].filePath) : null;

            var gameData = !string.IsNullOrEmpty(json) ? GameData.Parse(JSON.Parse(json), false) : CreateBaseBeatmap();

            StartCoroutine(ProjectData.Writer.SaveData(RTFile.ApplicationDirectory + editorListSlash + EditorManager.inst.newLevelName + "/level.lsb", gameData));
            var metaData = new MetaData();
            metaData.beatmap.game_version = "4.1.16";
            metaData.arcadeID = LSText.randomNumString(16);
            metaData.song.title = EditorManager.inst.newLevelName;
            metaData.creator.steam_name = SteamWrapper.inst.user.displayName;
            metaData.creator.steam_id = SteamWrapper.inst.user.id;

            DataManager.inst.metaData = metaData;

            DataManager.inst.SaveMetadata(RTFile.ApplicationDirectory + editorListSlash + EditorManager.inst.newLevelName + "/metadata.lsb");
            StartCoroutine(LoadLevel(RTFile.ApplicationDirectory + editorListSlash + EditorManager.inst.newLevelName));
            EditorManager.inst.HideDialog("New File Popup");
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

            for (int i = 0; i < (RTHelpers.AprilFools ? 45 : 25); i++)
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

                var randomShape = UnityEngine.Random.Range(0, ShapeManager.inst.Shapes3D.Count - 1);
                if (RTHelpers.AprilFools)
                {
                    if (randomShape != 4 && randomShape != 6)
                        backgroundObject.shape = ShapeManager.inst.Shapes3D[randomShape][0];
                }

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

                EditorThemeManager.ApplyElement(new EditorThemeManager.Element($"Level Button {num}", "List Button 1", ((EditorWrapper)metadataWrapper).GameObject, new List<Component>
                {
                    ((EditorWrapper)metadataWrapper).GameObject.GetComponent<Image>(),
                    ((EditorWrapper)metadataWrapper).GameObject.GetComponent<Button>(),
                }, true, 1, SpriteManager.RoundedSide.W, true));

                var text = ((EditorWrapper)metadataWrapper).GameObject.transform.GetChild(0).gameObject;
                EditorThemeManager.ApplyElement(new EditorThemeManager.Element($"Level Button {num} Text", "Light Text", text, new List<Component>
                {
                    text.GetComponent<Text>(),
                }));

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

        bool generatedPropertiesWindow = false;

        public void RenderPropertiesWindow() => StartCoroutine(IRenderPropertiesWindow());

        IEnumerator IRenderPropertiesWindow()
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

            var list = EditorProperties.Union(otherProperties).ToList();

            for (int i = 0; i < list.Count; i++)
            {
                var prop = list[i];

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
                                l.SetActive(true);
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
                                l.SetActive(true);
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
                                l.SetActive(true);
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
                                l.SetActive(true);
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
                                l.SetActive(true);
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
                                l.SetActive(true);
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
                                for (int j = 0; j < enums.Length; j++)
                                {
                                    var str = "Invalid Value";
                                    if (Enum.GetName(prop.configEntry.SettingType, j) != null)
                                    {
                                        hide.DisabledOptions.Add(false);
                                        str = Enum.GetName(prop.configEntry.SettingType, j);
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
                                l.SetActive(true);
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

            generatedPropertiesWindow = true;
            yield break;
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

            var buttonHoverSize = EditorConfig.Instance.OpenLevelButtonHoverSize.Value;

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

        public void PlayDialogAnimation(GameObject gameObject, string dialogName, bool active)
        {
            var play = EditorConfig.Instance.PlayEditorAnimations.Value;
            if (play && DialogAnimations.Has(x => x.name == dialogName) && gameObject.activeSelf != active)
            {
                var dialogAnimation = DialogAnimations.Find(x => x.name == dialogName);

                if (!dialogAnimation.Active)
                {
                    gameObject.SetActive(active);

                    return;
                }

                var dialog = gameObject.transform;

                var scrollbar = dialog.GetComponentInChildren<Scrollbar>();

                var animation = new AnimationManager.Animation("Popup Open");
                animation.floatAnimations = new List<AnimationManager.Animation.AnimationObject<float>>
                {
                    new AnimationManager.Animation.AnimationObject<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, active ? dialogAnimation.PosStart.x : dialogAnimation.PosEnd.x, Ease.Linear),
                        new FloatKeyframe(active ? dialogAnimation.PosXStartDuration : dialogAnimation.PosXEndDuration, active ? dialogAnimation.PosEnd.x : dialogAnimation.PosStart.x, active ? Ease.GetEaseFunction(dialogAnimation.PosXStartEase) : Ease.GetEaseFunction(dialogAnimation.PosXEndEase)),
                        new FloatKeyframe(active ? dialogAnimation.PosXStartDuration : dialogAnimation.PosXEndDuration + 0.01f, active ? dialogAnimation.PosEnd.x : dialogAnimation.PosStart.x, Ease.Linear),
                    }, delegate (float x)
                    {
                        if (dialogAnimation.PosActive)
                        {
                            var pos = dialog.localPosition;
                            pos.x = x;
                            dialog.localPosition = pos;
                        }
                    }),
                    new AnimationManager.Animation.AnimationObject<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, active ? dialogAnimation.PosStart.y : dialogAnimation.PosEnd.y, Ease.Linear),
                        new FloatKeyframe(active ? dialogAnimation.PosYStartDuration : dialogAnimation.PosYEndDuration, active ? dialogAnimation.PosEnd.y : dialogAnimation.PosStart.y, active ? Ease.GetEaseFunction(dialogAnimation.PosYStartEase) : Ease.GetEaseFunction(dialogAnimation.PosYEndEase)),
                        new FloatKeyframe(active ? dialogAnimation.PosYStartDuration : dialogAnimation.PosYEndDuration + 0.01f, active ? dialogAnimation.PosEnd.y : dialogAnimation.PosStart.y, Ease.Linear),
                    }, delegate (float x)
                    {
                        if (dialogAnimation.PosActive)
                        {
                            var pos = dialog.localPosition;
                            pos.y = x;
                            dialog.localPosition = pos;
                        }
                    }),
                    new AnimationManager.Animation.AnimationObject<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, active ? dialogAnimation.ScaStart.x : dialogAnimation.ScaEnd.x, Ease.Linear),
                        new FloatKeyframe(active ? dialogAnimation.ScaXStartDuration : dialogAnimation.ScaXEndDuration, active ? dialogAnimation.ScaEnd.x : dialogAnimation.ScaStart.x, active ? Ease.GetEaseFunction(dialogAnimation.ScaXStartEase) : Ease.GetEaseFunction(dialogAnimation.ScaXEndEase)),
                        new FloatKeyframe(active ? dialogAnimation.ScaXStartDuration : dialogAnimation.ScaXEndDuration + 0.01f, active ? dialogAnimation.ScaEnd.x : dialogAnimation.ScaStart.x, Ease.Linear),
                    }, delegate (float x)
                    {
                        if (dialogAnimation.ScaActive)
                        {
                            var pos = dialog.localScale;
                            pos.x = x;
                            dialog.localScale = pos;
                            if (scrollbar && active)
                                scrollbar.value = 1f;
                        }
                    }),
                    new AnimationManager.Animation.AnimationObject<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, active ? dialogAnimation.ScaStart.y : dialogAnimation.ScaEnd.y, Ease.Linear),
                        new FloatKeyframe(active ? dialogAnimation.ScaYStartDuration : dialogAnimation.ScaYEndDuration, active ? dialogAnimation.ScaEnd.y : dialogAnimation.ScaStart.y, active ? Ease.GetEaseFunction(dialogAnimation.ScaYStartEase) : Ease.GetEaseFunction(dialogAnimation.ScaYEndEase)),
                        new FloatKeyframe(active ? dialogAnimation.ScaYStartDuration : dialogAnimation.ScaYEndDuration + 0.01f, active ? dialogAnimation.ScaEnd.y : dialogAnimation.ScaStart.y, Ease.Linear),
                    }, delegate (float x)
                    {
                        if (dialogAnimation.ScaActive)
                        {
                            var pos = dialog.localScale;
                            pos.y = x;
                            dialog.localScale = pos;
                            if (scrollbar && active)
                                scrollbar.value = 1f;
                        }
                    }),
                    new AnimationManager.Animation.AnimationObject<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, active ? dialogAnimation.RotStart : dialogAnimation.RotEnd, Ease.Linear),
                        new FloatKeyframe(active ? dialogAnimation.RotStartDuration : dialogAnimation.RotEndDuration, active ? dialogAnimation.RotEnd : dialogAnimation.RotStart, active ? Ease.GetEaseFunction(dialogAnimation.RotStartEase) : Ease.GetEaseFunction(dialogAnimation.RotEndEase)),
                        new FloatKeyframe(active ? dialogAnimation.RotStartDuration : dialogAnimation.RotEndDuration + 0.01f, active ? dialogAnimation.RotEnd : dialogAnimation.RotStart, Ease.Linear),
                    }, delegate (float x)
                    {
                        if (dialogAnimation.RotActive)
                        {
                            dialog.localRotation = Quaternion.Euler(0f, 0f, x);
                        }
                    }),
                };
                animation.id = LSText.randomNumString(16);

                animation.onComplete = delegate ()
                {
                    dialog.gameObject.SetActive(active);

                    if (dialogAnimation.PosActive)
                        dialog.localPosition = new Vector3(dialogAnimation.PosEnd.x, dialogAnimation.PosEnd.y, 0f);
                    if (dialogAnimation.ScaActive)
                        dialog.localScale = new Vector3(dialogAnimation.ScaEnd.x, dialogAnimation.ScaEnd.y, 1f);
                    if (dialogAnimation.RotActive)
                        dialog.localRotation = Quaternion.Euler(0f, 0f, dialogAnimation.RotEnd);

                    AnimationManager.inst.RemoveID(animation.id);
                };

                AnimationManager.inst.Play(animation);
            }

            if (!play || !DialogAnimations.Has(x => x.name == dialogName) || active)
                gameObject.SetActive(active);
        }

        public void SetDialogStatus(string dialogName, bool active, bool focus = true)
        {
            if (!EditorManager.inst.EditorDialogsDictionary.ContainsKey(dialogName))
            {
                Debug.LogError($"{EditorManager.inst.className}Can't load dialog [{dialogName}].");
                return;
            }

            PlayDialogAnimation(EditorManager.inst.EditorDialogsDictionary[dialogName].Dialog.gameObject, dialogName, active);

            if (active)
            {
                if (focus)
                    EditorManager.inst.currentDialog = EditorManager.inst.EditorDialogsDictionary[dialogName];
                if (!EditorManager.inst.ActiveDialogs.Contains(EditorManager.inst.EditorDialogsDictionary[dialogName]))
                    EditorManager.inst.ActiveDialogs.Add(EditorManager.inst.EditorDialogsDictionary[dialogName]);
            }
            else
            {
                EditorManager.inst.ActiveDialogs.Remove(EditorManager.inst.EditorDialogsDictionary[dialogName]);
                if (EditorManager.inst.currentDialog == EditorManager.inst.EditorDialogsDictionary[dialogName] && focus)
                    EditorManager.inst.currentDialog = EditorManager.inst.ActiveDialogs.Count > 0 ? EditorManager.inst.ActiveDialogs.Last() : new EditorManager.EditorDialog();
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
                //Color color = Color.white;
                //if (AudioManager.inst.CurrentAudioSource.time < beatmapObject.StartTime)
                //    color = RTHelpers.BeatmapTheme.GetObjColor((int)beatmapObject.events[3][0].eventValues[0]);
                //else if (AudioManager.inst.CurrentAudioSource.time > beatmapObject.StartTime + beatmapObject.GetObjectLifeLength() && beatmapObject.autoKillType != AutoKillType.OldStyleNoAutokill)
                //    color = RTHelpers.BeatmapTheme.GetObjColor((int)beatmapObject.events[3][beatmapObject.events[3].Count - 1].eventValues[0]);
                //else if (renderer.material.HasProperty("_Color"))
                //    color = renderer.material.color;

                var color = AudioManager.inst.CurrentAudioSource.time < beatmapObject.StartTime ? RTHelpers.BeatmapTheme.GetObjColor((int)beatmapObject.events[3][0].eventValues[0])
                    : AudioManager.inst.CurrentAudioSource.time > beatmapObject.StartTime + beatmapObject.GetObjectLifeLength() && beatmapObject.autoKillType != AutoKillType.OldStyleNoAutokill
                    ? RTHelpers.BeatmapTheme.GetObjColor((int)beatmapObject.events[3][beatmapObject.events[3].Count - 1].eventValues[0])
                    : renderer.material.HasProperty("_Color") ? renderer.material.color : Color.white;

                if (ignoreTransparency)
                    color.a = 1f;

                return color;
            }

            return Color.white;
        }

        public static void DeleteLevelFunction(string level)
        {
            if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + "recycling"))
                Directory.CreateDirectory(RTFile.ApplicationDirectory + "recycling");

            Directory.Move(RTFile.ApplicationDirectory + editorListSlash + level, RTFile.ApplicationDirectory + "recycling/" + level);
        }

        public static float SnapToBPM(float time) => Mathf.RoundToInt((time + inst.bpmOffset) / (SettingEditor.inst.BPMMulti / EditorConfig.Instance.BPMSnapDivisions.Value)) * (SettingEditor.inst.BPMMulti / EditorConfig.Instance.BPMSnapDivisions.Value);

        public static void SetActive(GameObject gameObject, bool active)
        {
            gameObject.SetActive(active);
            gameObject.transform.parent.GetChild(gameObject.transform.GetSiblingIndex() - 1).gameObject.SetActive(active);
        }

        public void OrderMarkers()
        {
            DataManager.inst.gameData.beatmapData.markers = (from x in DataManager.inst.gameData.beatmapData.markers
                                                             orderby x.time
                                                             select x).ToList();
            MarkerEditor.inst.CreateMarkers();
        }


        #endregion

        #region Editor Properties

        public static List<EditorProperty> EditorProperties => new List<EditorProperty>()
        {
            #region General
            
            new EditorProperty("Reset to Defaults", EditorProperty.ValueType.Function, EditorProperty.EditorPropCategory.Timeline,
                delegate ()
                {
                    EditorManager.inst.ShowDialog("Warning Popup");
                    inst.RefreshWarningPopup("Are you sure you want to revert every config? THIS CANNOT BE UNDONE!",
                        delegate ()
                        {
                            var list = EditorProperties.Where(x => x.configEntry != null).ToList();

                            for (int i = 0; i < list.Count; i++)
                            {
                                list[i].configEntry.BoxedValue = list[i].configEntry.DefaultValue;
                            }

                        }, delegate ()
                        {

                        });

                }, "Reverts every config to their default value."),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.Debug),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.EditorZenMode),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.BPMSnapsKeyframes),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.BPMSnapDivisions),
            new EditorProperty(EditorProperty.ValueType.Bool, RTFunctions.FunctionsPlugin.IncreasedClipPlanes),
            new EditorProperty(EditorProperty.ValueType.Bool, RTFunctions.FunctionsPlugin.DiscordShowLevel),
            new EditorProperty(EditorProperty.ValueType.Bool, RTFunctions.FunctionsPlugin.EnableVideoBackground),
            new EditorProperty(EditorProperty.ValueType.Bool, RTFunctions.FunctionsPlugin.RunInBackground),
            new EditorProperty(EditorProperty.ValueType.Bool, RTFunctions.FunctionsPlugin.Fullscreen),
            new EditorProperty(EditorProperty.ValueType.Enum, RTFunctions.FunctionsPlugin.Resolution),
            new EditorProperty(EditorProperty.ValueType.IntSlider, RTFunctions.FunctionsPlugin.MasterVol),
            new EditorProperty(EditorProperty.ValueType.IntSlider, RTFunctions.FunctionsPlugin.MusicVol),
            new EditorProperty(EditorProperty.ValueType.IntSlider, RTFunctions.FunctionsPlugin.SFXVol),
            new EditorProperty(EditorProperty.ValueType.Enum, RTFunctions.FunctionsPlugin.Language),
            new EditorProperty(EditorProperty.ValueType.Bool, RTFunctions.FunctionsPlugin.ControllerRumble),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.DraggingPlaysSound),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.DraggingPlaysSoundOnlyWithBPM),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.RoundToNearest),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.ScrollOnEasing),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.PrefabExampleTemplate),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.PasteOffset),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.BringToSelection),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.CreateObjectsatCameraCenter),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.CreateObjectsScaleParentDefault),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.AllowEditorKeybindsWithEditorCam),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.RotationEventKeyframeResets),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.RememberLastKeyframeType),

            #endregion

            #region Timeline

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.DraggingMainCursorPausesLevel),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.TimelineCursorColor),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.KeyframeCursorColor),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ObjectSelectionColor),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.MainZoomBounds),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.KeyframeZoomBounds),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.MainZoomAmount),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.KeyframeZoomAmount),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.KeyframeEndLengthOffset),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.TimelineObjectPrefabTypeIcon),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.EventLabelsRenderLeft),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.WaveformGenerate),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.WaveformRerender),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.WaveformMode),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.WaveformBGColor),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.WaveformTopColor),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.WaveformBottomColor),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.WaveformTextureFormat),
            new EditorProperty("Render Waveform", EditorProperty.ValueType.Function, EditorProperty.EditorPropCategory.Timeline,
                delegate ()
                {
                    inst.StartCoroutine(inst.AssignTimelineTexture());
                }, "Renders the timeline waveform."),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.TimelineGridEnabled),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.TimelineGridColor),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.TimelineGridThickness),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.MarkerLineColor),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.MarkerLineWidth),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.MarkerTextWidth),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.MarkerLoopActive),
            new EditorProperty(EditorProperty.ValueType.Int, EditorPlugin.EditorConfig.MarkerLoopBegin),
            new EditorProperty(EditorProperty.ValueType.Int, EditorPlugin.EditorConfig.MarkerLoopEnd),

            #endregion

            #region Data

            new EditorProperty(EditorProperty.ValueType.Int, EditorPlugin.EditorConfig.AutosaveLimit),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.AutosaveLoopTime),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.LevelLoadsLastTime),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.LevelPausesOnStart),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.SavingSavesThemeOpacity),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.UpdatePrefabListOnFilesChanged),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.UpdateThemeListOnFilesChanged),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.ShowLevelsWithoutCoverNotification),
            new EditorProperty(EditorProperty.ValueType.String, EditorPlugin.EditorConfig.ZIPLevelExportPath),
            new EditorProperty(EditorProperty.ValueType.String, EditorPlugin.EditorConfig.ConvertLevelLSToVGExportPath),
            new EditorProperty(EditorProperty.ValueType.String, EditorPlugin.EditorConfig.ConvertPrefabLSToVGExportPath),
            new EditorProperty(EditorProperty.ValueType.String, EditorPlugin.EditorConfig.ConvertThemeLSToVGExportPath),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.ThemeSavesIndents),

            #endregion

            #region Editor GUI

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.DragUI),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.EditorTheme),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.RoundedUI),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.ShowModdedFeaturesInEditor),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.HoverUIPlaySound),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.ImportPrefabsDirectly),
            new EditorProperty(EditorProperty.ValueType.Int, EditorPlugin.EditorConfig.ThemesPerPage),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.NotificationWidth),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.NotificationSize),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.NotificationDirection),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.NotificationsDisplay),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.AdjustPositionInputs),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.HideVisualElementsWhenObjectIsEmpty),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.OpenLevelPosition),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.OpenLevelScale),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.OpenLevelEditorPathPos),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.OpenLevelEditorPathLength),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.OpenLevelListRefreshPosition),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.OpenLevelTogglePosition),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.OpenLevelDropdownPosition),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.OpenLevelCellSize),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.OpenLevelCellConstraintType),
            new EditorProperty(EditorProperty.ValueType.Int, EditorPlugin.EditorConfig.OpenLevelCellConstraintCount),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.OpenLevelCellSpacing),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.OpenLevelTextHorizontalWrap),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.OpenLevelTextVerticalWrap),
            new EditorProperty(EditorProperty.ValueType.IntSlider, EditorPlugin.EditorConfig.OpenLevelTextFontSize),

            new EditorProperty(EditorProperty.ValueType.Int, EditorPlugin.EditorConfig.OpenLevelFolderNameMax),
            new EditorProperty(EditorProperty.ValueType.Int, EditorPlugin.EditorConfig.OpenLevelSongNameMax),
            new EditorProperty(EditorProperty.ValueType.Int, EditorPlugin.EditorConfig.OpenLevelArtistNameMax),
            new EditorProperty(EditorProperty.ValueType.Int, EditorPlugin.EditorConfig.OpenLevelCreatorNameMax),
            new EditorProperty(EditorProperty.ValueType.Int, EditorPlugin.EditorConfig.OpenLevelDescriptionMax),
            new EditorProperty(EditorProperty.ValueType.Int, EditorPlugin.EditorConfig.OpenLevelDateMax),
            new EditorProperty(EditorProperty.ValueType.String, EditorPlugin.EditorConfig.OpenLevelTextFormatting),

            new EditorProperty(EditorProperty.ValueType.FloatSlider, EditorPlugin.EditorConfig.OpenLevelButtonHoverSize),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.OpenLevelCoverPosition),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.OpenLevelCoverScale),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.ChangesRefreshLevelList),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.OpenLevelShowDeleteButton),

            new EditorProperty(EditorProperty.ValueType.FloatSlider, EditorPlugin.EditorConfig.TimelineObjectHoverSize),
            new EditorProperty(EditorProperty.ValueType.FloatSlider, EditorPlugin.EditorConfig.KeyframeHoverSize),
            new EditorProperty(EditorProperty.ValueType.FloatSlider, EditorPlugin.EditorConfig.TimelineBarButtonsHoverSize),
            new EditorProperty(EditorProperty.ValueType.FloatSlider, EditorPlugin.EditorConfig.PrefabButtonHoverSize),

            // Prefab Internal
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.PrefabInternalPopupPos),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.PrefabInternalPopupSize),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.PrefabInternalHorizontalScroll),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.PrefabInternalCellSize),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.PrefabInternalConstraintMode),
            new EditorProperty(EditorProperty.ValueType.Int, EditorPlugin.EditorConfig.PrefabInternalConstraint),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.PrefabInternalSpacing),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.PrefabInternalStartAxis),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.PrefabInternalDeleteButtonPos),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.PrefabInternalDeleteButtonSca),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.PrefabInternalNameHorizontalWrap),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.PrefabInternalNameVerticalWrap),
            new EditorProperty(EditorProperty.ValueType.IntSlider, EditorPlugin.EditorConfig.PrefabInternalNameFontSize),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.PrefabInternalTypeHorizontalWrap),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.PrefabInternalTypeVerticalWrap),
            new EditorProperty(EditorProperty.ValueType.IntSlider, EditorPlugin.EditorConfig.PrefabInternalTypeFontSize),

            // Prefab External
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.PrefabExternalPopupPos),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.PrefabExternalPopupSize),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.PrefabExternalPrefabPathPos),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.PrefabExternalPrefabPathLength),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.PrefabExternalPrefabRefreshPos),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.PrefabExternalHorizontalScroll),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.PrefabExternalCellSize),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.PrefabExternalConstraintMode),
            new EditorProperty(EditorProperty.ValueType.Int, EditorPlugin.EditorConfig.PrefabExternalConstraint),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.PrefabExternalSpacing),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.PrefabExternalStartAxis),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.PrefabExternalDeleteButtonPos),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.PrefabExternalDeleteButtonSca),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.PrefabExternalNameHorizontalWrap),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.PrefabExternalNameVerticalWrap),
            new EditorProperty(EditorProperty.ValueType.IntSlider, EditorPlugin.EditorConfig.PrefabExternalNameFontSize),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.PrefabExternalTypeHorizontalWrap),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.PrefabExternalTypeVerticalWrap),
            new EditorProperty(EditorProperty.ValueType.IntSlider, EditorPlugin.EditorConfig.PrefabExternalTypeFontSize),

            #endregion

            #region Fields
            
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ScrollwheelLargeAmountKey),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ScrollwheelSmallAmountKey),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ScrollwheelRegularAmountKey),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ScrollwheelVector2LargeAmountKey),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ScrollwheelVector2SmallAmountKey),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ScrollwheelVector2RegularAmountKey),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.ShowModifiedColors),
            new EditorProperty(EditorProperty.ValueType.String, EditorPlugin.EditorConfig.ThemeTemplateName),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateGUI),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateTail),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateBG),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplatePlayer1),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplatePlayer2),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplatePlayer3),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplatePlayer4),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateOBJ1),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateOBJ2),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateOBJ3),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateOBJ4),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateOBJ5),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateOBJ6),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateOBJ7),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateOBJ8),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateOBJ9),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateOBJ10),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateOBJ11),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateOBJ12),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateOBJ13),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateOBJ14),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateOBJ15),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateOBJ16),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateOBJ17),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateOBJ18),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateBG1),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateBG2),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateBG3),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateBG4),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateBG5),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateBG6),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateBG7),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateBG8),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateBG9),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateFX1),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateFX2),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateFX3),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateFX4),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateFX5),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateFX6),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateFX7),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateFX8),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateFX9),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateFX10),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateFX11),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateFX12),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateFX13),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateFX14),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateFX15),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateFX16),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateFX17),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ThemeTemplateFX18),

            #endregion

            #region Animations
            
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.PlayEditorAnimations),

            #region OpenFilePopup

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.OpenFilePopupActive),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.OpenFilePopupPosActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.OpenFilePopupPosOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.OpenFilePopupPosClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.OpenFilePopupPosOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.OpenFilePopupPosCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.OpenFilePopupPosXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.OpenFilePopupPosXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.OpenFilePopupPosYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.OpenFilePopupPosYCloseEase),
            
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.OpenFilePopupScaActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.OpenFilePopupScaOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.OpenFilePopupScaClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.OpenFilePopupScaOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.OpenFilePopupScaCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.OpenFilePopupScaXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.OpenFilePopupScaXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.OpenFilePopupScaYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.OpenFilePopupScaYCloseEase),
            
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.OpenFilePopupRotActive),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.OpenFilePopupRotOpen),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.OpenFilePopupRotClose),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.OpenFilePopupRotOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.OpenFilePopupRotCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.OpenFilePopupRotOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.OpenFilePopupRotCloseEase),

            #endregion
            
            #region NewFilePopup

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.NewFilePopupActive),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.NewFilePopupPosActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.NewFilePopupPosOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.NewFilePopupPosClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.NewFilePopupPosOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.NewFilePopupPosCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.NewFilePopupPosXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.NewFilePopupPosXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.NewFilePopupPosYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.NewFilePopupPosYCloseEase),
            
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.NewFilePopupScaActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.NewFilePopupScaOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.NewFilePopupScaClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.NewFilePopupScaOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.NewFilePopupScaCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.NewFilePopupScaXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.NewFilePopupScaXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.NewFilePopupScaYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.NewFilePopupScaYCloseEase),
            
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.NewFilePopupRotActive),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.NewFilePopupRotOpen),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.NewFilePopupRotClose),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.NewFilePopupRotOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.NewFilePopupRotCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.NewFilePopupRotOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.NewFilePopupRotCloseEase),

            #endregion
            
            #region SaveAsPopup

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.SaveAsPopupActive),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.SaveAsPopupPosActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.SaveAsPopupPosOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.SaveAsPopupPosClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.SaveAsPopupPosOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.SaveAsPopupPosCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.SaveAsPopupPosXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.SaveAsPopupPosXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.SaveAsPopupPosYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.SaveAsPopupPosYCloseEase),
            
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.SaveAsPopupScaActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.SaveAsPopupScaOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.SaveAsPopupScaClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.SaveAsPopupScaOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.SaveAsPopupScaCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.SaveAsPopupScaXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.SaveAsPopupScaXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.SaveAsPopupScaYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.SaveAsPopupScaYCloseEase),
            
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.SaveAsPopupRotActive),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.SaveAsPopupRotOpen),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.SaveAsPopupRotClose),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.SaveAsPopupRotOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.SaveAsPopupRotCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.SaveAsPopupRotOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.SaveAsPopupRotCloseEase),

            #endregion
            
            #region QuickActionsPopup

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.QuickActionsPopupActive),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.QuickActionsPopupPosActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.QuickActionsPopupPosOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.QuickActionsPopupPosClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.QuickActionsPopupPosOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.QuickActionsPopupPosCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.QuickActionsPopupPosXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.QuickActionsPopupPosXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.QuickActionsPopupPosYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.QuickActionsPopupPosYCloseEase),
            
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.QuickActionsPopupScaActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.QuickActionsPopupScaOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.QuickActionsPopupScaClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.QuickActionsPopupScaOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.QuickActionsPopupScaCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.QuickActionsPopupScaXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.QuickActionsPopupScaXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.QuickActionsPopupScaYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.QuickActionsPopupScaYCloseEase),
            
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.QuickActionsPopupRotActive),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.QuickActionsPopupRotOpen),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.QuickActionsPopupRotClose),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.QuickActionsPopupRotOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.QuickActionsPopupRotCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.QuickActionsPopupRotOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.QuickActionsPopupRotCloseEase),

            #endregion
            
            #region ParentSelectorPopup

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.ParentSelectorPopupActive),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.ParentSelectorPopupPosActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ParentSelectorPopupPosOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ParentSelectorPopupPosClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ParentSelectorPopupPosOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ParentSelectorPopupPosCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ParentSelectorPopupPosXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ParentSelectorPopupPosXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ParentSelectorPopupPosYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ParentSelectorPopupPosYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.ParentSelectorPopupScaActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ParentSelectorPopupScaOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ParentSelectorPopupScaClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ParentSelectorPopupScaOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ParentSelectorPopupScaCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ParentSelectorPopupScaXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ParentSelectorPopupScaXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ParentSelectorPopupScaYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ParentSelectorPopupScaYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.ParentSelectorPopupRotActive),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.ParentSelectorPopupRotOpen),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.ParentSelectorPopupRotClose),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.ParentSelectorPopupRotOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.ParentSelectorPopupRotCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ParentSelectorPopupRotOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ParentSelectorPopupRotCloseEase),

            #endregion
            
            #region PrefabPopup

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.PrefabPopupActive),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.PrefabPopupPosActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.PrefabPopupPosOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.PrefabPopupPosClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.PrefabPopupPosOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.PrefabPopupPosCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.PrefabPopupPosXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.PrefabPopupPosXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.PrefabPopupPosYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.PrefabPopupPosYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.PrefabPopupScaActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.PrefabPopupScaOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.PrefabPopupScaClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.PrefabPopupScaOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.PrefabPopupScaCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.PrefabPopupScaXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.PrefabPopupScaXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.PrefabPopupScaYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.PrefabPopupScaYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.PrefabPopupRotActive),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.PrefabPopupRotOpen),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.PrefabPopupRotClose),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.PrefabPopupRotOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.PrefabPopupRotCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.PrefabPopupRotOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.PrefabPopupRotCloseEase),

            #endregion
            
            #region ObjectOptionsPopup

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.ObjectOptionsPopupActive),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.ObjectOptionsPopupPosActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ObjectOptionsPopupPosOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ObjectOptionsPopupPosClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ObjectOptionsPopupPosOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ObjectOptionsPopupPosCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ObjectOptionsPopupPosXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ObjectOptionsPopupPosXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ObjectOptionsPopupPosYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ObjectOptionsPopupPosYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.ObjectOptionsPopupScaActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ObjectOptionsPopupScaOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ObjectOptionsPopupScaClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ObjectOptionsPopupScaOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ObjectOptionsPopupScaCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ObjectOptionsPopupScaXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ObjectOptionsPopupScaXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ObjectOptionsPopupScaYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ObjectOptionsPopupScaYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.ObjectOptionsPopupRotActive),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.ObjectOptionsPopupRotOpen),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.ObjectOptionsPopupRotClose),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.ObjectOptionsPopupRotOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.ObjectOptionsPopupRotCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ObjectOptionsPopupRotOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ObjectOptionsPopupRotCloseEase),

            #endregion
            
            #region BGOptionsPopup

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.BGOptionsPopupActive),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.BGOptionsPopupPosActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.BGOptionsPopupPosOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.BGOptionsPopupPosClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.BGOptionsPopupPosOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.BGOptionsPopupPosCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.BGOptionsPopupPosXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.BGOptionsPopupPosXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.BGOptionsPopupPosYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.BGOptionsPopupPosYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.BGOptionsPopupScaActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.BGOptionsPopupScaOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.BGOptionsPopupScaClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.BGOptionsPopupScaOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.BGOptionsPopupScaCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.BGOptionsPopupScaXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.BGOptionsPopupScaXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.BGOptionsPopupScaYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.BGOptionsPopupScaYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.BGOptionsPopupRotActive),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.BGOptionsPopupRotOpen),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.BGOptionsPopupRotClose),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.BGOptionsPopupRotOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.BGOptionsPopupRotCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.BGOptionsPopupRotOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.BGOptionsPopupRotCloseEase),

            #endregion
            
            #region BrowserPopup

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.BrowserPopupActive),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.BrowserPopupPosActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.BrowserPopupPosOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.BrowserPopupPosClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.BrowserPopupPosOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.BrowserPopupPosCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.BrowserPopupPosXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.BrowserPopupPosXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.BrowserPopupPosYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.BrowserPopupPosYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.BrowserPopupScaActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.BrowserPopupScaOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.BrowserPopupScaClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.BrowserPopupScaOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.BrowserPopupScaCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.BrowserPopupScaXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.BrowserPopupScaXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.BrowserPopupScaYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.BrowserPopupScaYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.BrowserPopupRotActive),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.BrowserPopupRotOpen),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.BrowserPopupRotClose),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.BrowserPopupRotOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.BrowserPopupRotCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.BrowserPopupRotOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.BrowserPopupRotCloseEase),

            #endregion
            
            #region ObjectSearchPopup

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.ObjectSearchPopupActive),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.ObjectSearchPopupPosActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ObjectSearchPopupPosOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ObjectSearchPopupPosClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ObjectSearchPopupPosOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ObjectSearchPopupPosCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ObjectSearchPopupPosXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ObjectSearchPopupPosXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ObjectSearchPopupPosYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ObjectSearchPopupPosYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.ObjectSearchPopupScaActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ObjectSearchPopupScaOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ObjectSearchPopupScaClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ObjectSearchPopupScaOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ObjectSearchPopupScaCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ObjectSearchPopupScaXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ObjectSearchPopupScaXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ObjectSearchPopupScaYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ObjectSearchPopupScaYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.ObjectSearchPopupRotActive),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.ObjectSearchPopupRotOpen),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.ObjectSearchPopupRotClose),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.ObjectSearchPopupRotOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.ObjectSearchPopupRotCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ObjectSearchPopupRotOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ObjectSearchPopupRotCloseEase),

            #endregion
            
            #region WarningPopup

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.WarningPopupActive),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.WarningPopupPosActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.WarningPopupPosOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.WarningPopupPosClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.WarningPopupPosOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.WarningPopupPosCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.WarningPopupPosXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.WarningPopupPosXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.WarningPopupPosYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.WarningPopupPosYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.WarningPopupScaActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.WarningPopupScaOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.WarningPopupScaClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.WarningPopupScaOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.WarningPopupScaCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.WarningPopupScaXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.WarningPopupScaXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.WarningPopupScaYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.WarningPopupScaYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.WarningPopupRotActive),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.WarningPopupRotOpen),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.WarningPopupRotClose),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.WarningPopupRotOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.WarningPopupRotCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.WarningPopupRotOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.WarningPopupRotCloseEase),

            #endregion
            
            #region REPLEditorPopup

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.REPLEditorPopupActive),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.REPLEditorPopupPosActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.REPLEditorPopupPosOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.REPLEditorPopupPosClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.REPLEditorPopupPosOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.REPLEditorPopupPosCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.REPLEditorPopupPosXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.REPLEditorPopupPosXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.REPLEditorPopupPosYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.REPLEditorPopupPosYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.REPLEditorPopupScaActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.REPLEditorPopupScaOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.REPLEditorPopupScaClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.REPLEditorPopupScaOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.REPLEditorPopupScaCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.REPLEditorPopupScaXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.REPLEditorPopupScaXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.REPLEditorPopupScaYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.REPLEditorPopupScaYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.REPLEditorPopupRotActive),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.REPLEditorPopupRotOpen),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.REPLEditorPopupRotClose),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.REPLEditorPopupRotOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.REPLEditorPopupRotCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.REPLEditorPopupRotOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.REPLEditorPopupRotCloseEase),

            #endregion
            
            #region EditorPropertiesPopup

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.EditorPropertiesPopupActive),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.EditorPropertiesPopupPosActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.EditorPropertiesPopupPosOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.EditorPropertiesPopupPosClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.EditorPropertiesPopupPosOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.EditorPropertiesPopupPosCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.EditorPropertiesPopupPosXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.EditorPropertiesPopupPosXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.EditorPropertiesPopupPosYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.EditorPropertiesPopupPosYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.EditorPropertiesPopupScaActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.EditorPropertiesPopupScaOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.EditorPropertiesPopupScaClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.EditorPropertiesPopupScaOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.EditorPropertiesPopupScaCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.EditorPropertiesPopupScaXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.EditorPropertiesPopupScaXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.EditorPropertiesPopupScaYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.EditorPropertiesPopupScaYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.EditorPropertiesPopupRotActive),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.EditorPropertiesPopupRotOpen),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.EditorPropertiesPopupRotClose),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.EditorPropertiesPopupRotOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.EditorPropertiesPopupRotCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.EditorPropertiesPopupRotOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.EditorPropertiesPopupRotCloseEase),

            #endregion
            
            #region DocumentationPopup

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.DocumentationPopupActive),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.DocumentationPopupPosActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.DocumentationPopupPosOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.DocumentationPopupPosClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.DocumentationPopupPosOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.DocumentationPopupPosCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.DocumentationPopupPosXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.DocumentationPopupPosXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.DocumentationPopupPosYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.DocumentationPopupPosYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.DocumentationPopupScaActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.DocumentationPopupScaOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.DocumentationPopupScaClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.DocumentationPopupScaOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.DocumentationPopupScaCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.DocumentationPopupScaXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.DocumentationPopupScaXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.DocumentationPopupScaYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.DocumentationPopupScaYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.DocumentationPopupRotActive),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.DocumentationPopupRotOpen),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.DocumentationPopupRotClose),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.DocumentationPopupRotOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.DocumentationPopupRotCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.DocumentationPopupRotOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.DocumentationPopupRotCloseEase),

            #endregion
            
            #region DebuggerPopup

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.DebuggerPopupActive),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.DebuggerPopupPosActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.DebuggerPopupPosOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.DebuggerPopupPosClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.DebuggerPopupPosOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.DebuggerPopupPosCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.DebuggerPopupPosXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.DebuggerPopupPosXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.DebuggerPopupPosYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.DebuggerPopupPosYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.DebuggerPopupScaActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.DebuggerPopupScaOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.DebuggerPopupScaClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.DebuggerPopupScaOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.DebuggerPopupScaCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.DebuggerPopupScaXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.DebuggerPopupScaXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.DebuggerPopupScaYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.DebuggerPopupScaYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.DebuggerPopupRotActive),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.DebuggerPopupRotOpen),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.DebuggerPopupRotClose),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.DebuggerPopupRotOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.DebuggerPopupRotCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.DebuggerPopupRotOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.DebuggerPopupRotCloseEase),

            #endregion
            
            #region AutosavesPopup

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.AutosavesPopupActive),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.AutosavesPopupPosActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.AutosavesPopupPosOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.AutosavesPopupPosClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.AutosavesPopupPosOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.AutosavesPopupPosCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.AutosavesPopupPosXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.AutosavesPopupPosXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.AutosavesPopupPosYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.AutosavesPopupPosYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.AutosavesPopupScaActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.AutosavesPopupScaOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.AutosavesPopupScaClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.AutosavesPopupScaOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.AutosavesPopupScaCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.AutosavesPopupScaXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.AutosavesPopupScaXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.AutosavesPopupScaYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.AutosavesPopupScaYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.AutosavesPopupRotActive),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.AutosavesPopupRotOpen),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.AutosavesPopupRotClose),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.AutosavesPopupRotOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.AutosavesPopupRotCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.AutosavesPopupRotOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.AutosavesPopupRotCloseEase),

            #endregion
            
            #region DefaultModifiersPopup

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.DefaultModifiersPopupActive),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.DefaultModifiersPopupPosActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.DefaultModifiersPopupPosOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.DefaultModifiersPopupPosClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.DefaultModifiersPopupPosOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.DefaultModifiersPopupPosCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.DefaultModifiersPopupPosXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.DefaultModifiersPopupPosXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.DefaultModifiersPopupPosYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.DefaultModifiersPopupPosYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.DefaultModifiersPopupScaActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.DefaultModifiersPopupScaOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.DefaultModifiersPopupScaClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.DefaultModifiersPopupScaOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.DefaultModifiersPopupScaCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.DefaultModifiersPopupScaXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.DefaultModifiersPopupScaXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.DefaultModifiersPopupScaYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.DefaultModifiersPopupScaYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.DefaultModifiersPopupRotActive),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.DefaultModifiersPopupRotOpen),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.DefaultModifiersPopupRotClose),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.DefaultModifiersPopupRotOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.DefaultModifiersPopupRotCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.DefaultModifiersPopupRotOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.DefaultModifiersPopupRotCloseEase),

            #endregion
            
            #region KeybindListPopup

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.KeybindListPopupActive),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.KeybindListPopupPosActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.KeybindListPopupPosOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.KeybindListPopupPosClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.KeybindListPopupPosOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.KeybindListPopupPosCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.KeybindListPopupPosXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.KeybindListPopupPosXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.KeybindListPopupPosYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.KeybindListPopupPosYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.KeybindListPopupScaActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.KeybindListPopupScaOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.KeybindListPopupScaClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.KeybindListPopupScaOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.KeybindListPopupScaCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.KeybindListPopupScaXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.KeybindListPopupScaXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.KeybindListPopupScaYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.KeybindListPopupScaYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.KeybindListPopupRotActive),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.KeybindListPopupRotOpen),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.KeybindListPopupRotClose),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.KeybindListPopupRotOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.KeybindListPopupRotCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.KeybindListPopupRotOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.KeybindListPopupRotCloseEase),

            #endregion
            
            #region ThemePopup

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.ThemePopupActive),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.ThemePopupPosActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ThemePopupPosOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ThemePopupPosClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ThemePopupPosOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ThemePopupPosCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ThemePopupPosXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ThemePopupPosXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ThemePopupPosYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ThemePopupPosYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.ThemePopupScaActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ThemePopupScaOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ThemePopupScaClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ThemePopupScaOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ThemePopupScaCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ThemePopupScaXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ThemePopupScaXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ThemePopupScaYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ThemePopupScaYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.ThemePopupRotActive),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.ThemePopupRotOpen),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.ThemePopupRotClose),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.ThemePopupRotOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.ThemePopupRotCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ThemePopupRotOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ThemePopupRotCloseEase),

            #endregion
            
            #region PrefabTypesPopup

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.PrefabTypesPopupActive),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.PrefabTypesPopupPosActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.PrefabTypesPopupPosOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.PrefabTypesPopupPosClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.PrefabTypesPopupPosOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.PrefabTypesPopupPosCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.PrefabTypesPopupPosXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.PrefabTypesPopupPosXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.PrefabTypesPopupPosYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.PrefabTypesPopupPosYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.PrefabTypesPopupScaActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.PrefabTypesPopupScaOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.PrefabTypesPopupScaClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.PrefabTypesPopupScaOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.PrefabTypesPopupScaCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.PrefabTypesPopupScaXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.PrefabTypesPopupScaXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.PrefabTypesPopupScaYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.PrefabTypesPopupScaYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.PrefabTypesPopupRotActive),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.PrefabTypesPopupRotOpen),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.PrefabTypesPopupRotClose),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.PrefabTypesPopupRotOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.PrefabTypesPopupRotCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.PrefabTypesPopupRotOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.PrefabTypesPopupRotCloseEase),

            #endregion
            
            #region FileDropdown

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.FileDropdownActive),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.FileDropdownPosActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.FileDropdownPosOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.FileDropdownPosClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.FileDropdownPosOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.FileDropdownPosCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.FileDropdownPosXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.FileDropdownPosXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.FileDropdownPosYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.FileDropdownPosYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.FileDropdownScaActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.FileDropdownScaOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.FileDropdownScaClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.FileDropdownScaOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.FileDropdownScaCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.FileDropdownScaXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.FileDropdownScaXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.FileDropdownScaYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.FileDropdownScaYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.FileDropdownRotActive),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.FileDropdownRotOpen),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.FileDropdownRotClose),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.FileDropdownRotOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.FileDropdownRotCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.FileDropdownRotOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.FileDropdownRotCloseEase),

            #endregion
            
            #region EditDropdown

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.EditDropdownActive),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.EditDropdownPosActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.EditDropdownPosOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.EditDropdownPosClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.EditDropdownPosOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.EditDropdownPosCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.EditDropdownPosXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.EditDropdownPosXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.EditDropdownPosYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.EditDropdownPosYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.EditDropdownScaActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.EditDropdownScaOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.EditDropdownScaClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.EditDropdownScaOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.EditDropdownScaCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.EditDropdownScaXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.EditDropdownScaXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.EditDropdownScaYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.EditDropdownScaYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.EditDropdownRotActive),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.EditDropdownRotOpen),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.EditDropdownRotClose),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.EditDropdownRotOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.EditDropdownRotCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.EditDropdownRotOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.EditDropdownRotCloseEase),

            #endregion
            
            #region ViewDropdown

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.ViewDropdownActive),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.ViewDropdownPosActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ViewDropdownPosOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ViewDropdownPosClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ViewDropdownPosOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ViewDropdownPosCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ViewDropdownPosXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ViewDropdownPosXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ViewDropdownPosYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ViewDropdownPosYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.ViewDropdownScaActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ViewDropdownScaOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ViewDropdownScaClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ViewDropdownScaOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.ViewDropdownScaCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ViewDropdownScaXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ViewDropdownScaXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ViewDropdownScaYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ViewDropdownScaYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.ViewDropdownRotActive),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.ViewDropdownRotOpen),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.ViewDropdownRotClose),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.ViewDropdownRotOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.ViewDropdownRotCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ViewDropdownRotOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.ViewDropdownRotCloseEase),

            #endregion
            
            #region SteamDropdown

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.SteamDropdownActive),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.SteamDropdownPosActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.SteamDropdownPosOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.SteamDropdownPosClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.SteamDropdownPosOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.SteamDropdownPosCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.SteamDropdownPosXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.SteamDropdownPosXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.SteamDropdownPosYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.SteamDropdownPosYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.SteamDropdownScaActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.SteamDropdownScaOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.SteamDropdownScaClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.SteamDropdownScaOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.SteamDropdownScaCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.SteamDropdownScaXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.SteamDropdownScaXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.SteamDropdownScaYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.SteamDropdownScaYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.SteamDropdownRotActive),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.SteamDropdownRotOpen),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.SteamDropdownRotClose),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.SteamDropdownRotOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.SteamDropdownRotCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.SteamDropdownRotOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.SteamDropdownRotCloseEase),

            #endregion
            
            #region HelpDropdown

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.HelpDropdownActive),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.HelpDropdownPosActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.HelpDropdownPosOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.HelpDropdownPosClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.HelpDropdownPosOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.HelpDropdownPosCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.HelpDropdownPosXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.HelpDropdownPosXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.HelpDropdownPosYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.HelpDropdownPosYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.HelpDropdownScaActive),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.HelpDropdownScaOpen),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.HelpDropdownScaClose),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.HelpDropdownScaOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Vector2, EditorPlugin.EditorConfig.HelpDropdownScaCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.HelpDropdownScaXOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.HelpDropdownScaXCloseEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.HelpDropdownScaYOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.HelpDropdownScaYCloseEase),

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.HelpDropdownRotActive),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.HelpDropdownRotOpen),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.HelpDropdownRotClose),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.HelpDropdownRotOpenDuration),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.HelpDropdownRotCloseDuration),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.HelpDropdownRotOpenEase),
            new EditorProperty(EditorProperty.ValueType.Enum, EditorPlugin.EditorConfig.HelpDropdownRotCloseEase),

            #endregion

            #endregion

            #region Preview

            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.OnlyObjectsOnCurrentLayerVisible),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.VisibleObjectOpacity),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.ShowEmpties),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.OnlyShowDamagable),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.HighlightObjects),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ObjectHighlightAmount),
            new EditorProperty(EditorProperty.ValueType.Color, EditorPlugin.EditorConfig.ObjectHighlightDoubleAmount),
            new EditorProperty(EditorProperty.ValueType.Bool, EditorPlugin.EditorConfig.ObjectDraggerEnabled),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.ObjectDraggerRotatorRadius),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.ObjectDraggerScalerOffset),
            new EditorProperty(EditorProperty.ValueType.Float, EditorPlugin.EditorConfig.ObjectDraggerScalerScale),

            #endregion
        };

        public List<EditorProperty> otherProperties = new List<EditorProperty>();

        #endregion

        #region Constructors

        public List<DialogAnimation> DialogAnimations { get; set; } = new List<DialogAnimation>
        {
            new DialogAnimation("Open File Popup")
            {
                ActiveConfig = EditorConfig.Instance.OpenFilePopupActive,

                PosActiveConfig = EditorConfig.Instance.OpenFilePopupPosActive,
                PosOpenConfig = EditorConfig.Instance.OpenFilePopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.OpenFilePopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.OpenFilePopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.OpenFilePopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.OpenFilePopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.OpenFilePopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.OpenFilePopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.OpenFilePopupPosYCloseEase,
                
                ScaActiveConfig = EditorConfig.Instance.OpenFilePopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.OpenFilePopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.OpenFilePopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.OpenFilePopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.OpenFilePopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.OpenFilePopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.OpenFilePopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.OpenFilePopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.OpenFilePopupScaYCloseEase,
                
                RotActiveConfig = EditorConfig.Instance.OpenFilePopupRotActive,
                RotOpenConfig = EditorConfig.Instance.OpenFilePopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.OpenFilePopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.OpenFilePopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.OpenFilePopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.OpenFilePopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.OpenFilePopupRotCloseEase,
            },
            new DialogAnimation("New File Popup")
            {
                ActiveConfig = EditorConfig.Instance.NewFilePopupActive,

                PosActiveConfig = EditorConfig.Instance.NewFilePopupPosActive,
                PosOpenConfig = EditorConfig.Instance.NewFilePopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.NewFilePopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.NewFilePopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.NewFilePopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.NewFilePopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.NewFilePopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.NewFilePopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.NewFilePopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.NewFilePopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.NewFilePopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.NewFilePopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.NewFilePopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.NewFilePopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.NewFilePopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.NewFilePopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.NewFilePopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.NewFilePopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.NewFilePopupRotActive,
                RotOpenConfig = EditorConfig.Instance.NewFilePopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.NewFilePopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.NewFilePopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.NewFilePopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.NewFilePopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.NewFilePopupRotCloseEase,
            },
            new DialogAnimation("Save As Popup")
            {
                ActiveConfig = EditorConfig.Instance.SaveAsPopupActive,

                PosActiveConfig = EditorConfig.Instance.SaveAsPopupPosActive,
                PosOpenConfig = EditorConfig.Instance.SaveAsPopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.SaveAsPopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.SaveAsPopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.SaveAsPopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.SaveAsPopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.SaveAsPopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.SaveAsPopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.SaveAsPopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.SaveAsPopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.SaveAsPopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.SaveAsPopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.SaveAsPopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.SaveAsPopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.SaveAsPopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.SaveAsPopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.SaveAsPopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.SaveAsPopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.SaveAsPopupRotActive,
                RotOpenConfig = EditorConfig.Instance.SaveAsPopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.SaveAsPopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.SaveAsPopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.SaveAsPopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.SaveAsPopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.SaveAsPopupRotCloseEase,
            },
            new DialogAnimation("Quick Actions Popup")
            {
                ActiveConfig = EditorConfig.Instance.NewFilePopupActive,

                PosActiveConfig = EditorConfig.Instance.NewFilePopupPosActive,
                PosOpenConfig = EditorConfig.Instance.NewFilePopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.NewFilePopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.NewFilePopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.NewFilePopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.NewFilePopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.NewFilePopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.NewFilePopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.NewFilePopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.NewFilePopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.NewFilePopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.NewFilePopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.NewFilePopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.NewFilePopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.NewFilePopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.NewFilePopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.NewFilePopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.NewFilePopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.NewFilePopupRotActive,
                RotOpenConfig = EditorConfig.Instance.NewFilePopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.NewFilePopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.NewFilePopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.NewFilePopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.NewFilePopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.NewFilePopupRotCloseEase,
            },
            new DialogAnimation("Parent Selector")
            {
                ActiveConfig = EditorConfig.Instance.ParentSelectorPopupActive,

                PosActiveConfig = EditorConfig.Instance.ParentSelectorPopupPosActive,
                PosOpenConfig = EditorConfig.Instance.ParentSelectorPopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.ParentSelectorPopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.ParentSelectorPopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.ParentSelectorPopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.ParentSelectorPopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.ParentSelectorPopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.ParentSelectorPopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.ParentSelectorPopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.ParentSelectorPopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.ParentSelectorPopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.ParentSelectorPopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.ParentSelectorPopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.ParentSelectorPopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.ParentSelectorPopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.ParentSelectorPopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.ParentSelectorPopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.ParentSelectorPopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.ParentSelectorPopupRotActive,
                RotOpenConfig = EditorConfig.Instance.ParentSelectorPopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.ParentSelectorPopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.ParentSelectorPopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.ParentSelectorPopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.ParentSelectorPopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.ParentSelectorPopupRotCloseEase,
            },
            new DialogAnimation("Prefab Popup")
            {
                ActiveConfig = EditorConfig.Instance.PrefabPopupActive,

                PosActiveConfig = EditorConfig.Instance.PrefabPopupPosActive,
                PosOpenConfig = EditorConfig.Instance.PrefabPopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.PrefabPopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.PrefabPopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.PrefabPopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.PrefabPopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.PrefabPopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.PrefabPopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.PrefabPopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.PrefabPopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.PrefabPopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.PrefabPopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.PrefabPopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.PrefabPopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.PrefabPopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.PrefabPopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.PrefabPopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.PrefabPopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.PrefabPopupRotActive,
                RotOpenConfig = EditorConfig.Instance.PrefabPopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.PrefabPopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.PrefabPopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.PrefabPopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.PrefabPopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.PrefabPopupRotCloseEase,
            },
            new DialogAnimation("Object Options Popup")
            {
                ActiveConfig = EditorConfig.Instance.NewFilePopupActive,

                PosActiveConfig = EditorConfig.Instance.NewFilePopupPosActive,
                PosOpenConfig = EditorConfig.Instance.NewFilePopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.NewFilePopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.NewFilePopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.NewFilePopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.NewFilePopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.NewFilePopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.NewFilePopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.NewFilePopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.NewFilePopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.NewFilePopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.NewFilePopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.NewFilePopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.NewFilePopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.NewFilePopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.NewFilePopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.NewFilePopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.NewFilePopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.NewFilePopupRotActive,
                RotOpenConfig = EditorConfig.Instance.NewFilePopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.NewFilePopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.NewFilePopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.NewFilePopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.NewFilePopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.NewFilePopupRotCloseEase,
            },
            new DialogAnimation("BG Options Popup")
            {
                ActiveConfig = EditorConfig.Instance.BGOptionsPopupActive,

                PosActiveConfig = EditorConfig.Instance.BGOptionsPopupPosActive,
                PosOpenConfig = EditorConfig.Instance.BGOptionsPopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.BGOptionsPopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.BGOptionsPopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.BGOptionsPopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.BGOptionsPopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.BGOptionsPopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.BGOptionsPopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.BGOptionsPopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.BGOptionsPopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.BGOptionsPopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.BGOptionsPopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.BGOptionsPopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.BGOptionsPopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.BGOptionsPopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.BGOptionsPopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.BGOptionsPopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.BGOptionsPopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.BGOptionsPopupRotActive,
                RotOpenConfig = EditorConfig.Instance.BGOptionsPopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.BGOptionsPopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.BGOptionsPopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.BGOptionsPopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.BGOptionsPopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.BGOptionsPopupRotCloseEase,
            },
            new DialogAnimation("Browser Popup")
            {
                ActiveConfig = EditorConfig.Instance.BrowserPopupActive,

                PosActiveConfig = EditorConfig.Instance.BrowserPopupPosActive,
                PosOpenConfig = EditorConfig.Instance.BrowserPopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.BrowserPopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.BrowserPopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.BrowserPopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.BrowserPopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.BrowserPopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.BrowserPopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.BrowserPopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.BrowserPopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.BrowserPopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.BrowserPopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.BrowserPopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.BrowserPopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.BrowserPopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.BrowserPopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.BrowserPopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.BrowserPopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.BrowserPopupRotActive,
                RotOpenConfig = EditorConfig.Instance.BrowserPopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.BrowserPopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.BrowserPopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.BrowserPopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.BrowserPopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.BrowserPopupRotCloseEase,
            },
            new DialogAnimation("Object Search Popup")
            {
                ActiveConfig = EditorConfig.Instance.ObjectSearchPopupActive,

                PosActiveConfig = EditorConfig.Instance.ObjectSearchPopupPosActive,
                PosOpenConfig = EditorConfig.Instance.ObjectSearchPopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.ObjectSearchPopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.ObjectSearchPopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.ObjectSearchPopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.ObjectSearchPopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.ObjectSearchPopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.ObjectSearchPopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.ObjectSearchPopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.ObjectSearchPopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.ObjectSearchPopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.ObjectSearchPopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.ObjectSearchPopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.ObjectSearchPopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.ObjectSearchPopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.ObjectSearchPopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.ObjectSearchPopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.ObjectSearchPopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.ObjectSearchPopupRotActive,
                RotOpenConfig = EditorConfig.Instance.ObjectSearchPopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.ObjectSearchPopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.ObjectSearchPopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.ObjectSearchPopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.ObjectSearchPopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.ObjectSearchPopupRotCloseEase,
            },
            new DialogAnimation("Warning Popup")
            {
                ActiveConfig = EditorConfig.Instance.WarningPopupActive,

                PosActiveConfig = EditorConfig.Instance.WarningPopupPosActive,
                PosOpenConfig = EditorConfig.Instance.WarningPopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.WarningPopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.WarningPopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.WarningPopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.WarningPopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.WarningPopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.WarningPopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.WarningPopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.WarningPopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.WarningPopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.WarningPopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.WarningPopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.WarningPopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.WarningPopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.WarningPopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.WarningPopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.WarningPopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.WarningPopupRotActive,
                RotOpenConfig = EditorConfig.Instance.WarningPopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.WarningPopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.WarningPopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.WarningPopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.WarningPopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.WarningPopupRotCloseEase,
            },
            new DialogAnimation("REPL Editor Popup")
            {
                ActiveConfig = EditorConfig.Instance.REPLEditorPopupActive,

                PosActiveConfig = EditorConfig.Instance.REPLEditorPopupPosActive,
                PosOpenConfig = EditorConfig.Instance.REPLEditorPopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.REPLEditorPopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.REPLEditorPopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.REPLEditorPopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.REPLEditorPopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.REPLEditorPopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.REPLEditorPopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.REPLEditorPopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.REPLEditorPopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.REPLEditorPopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.REPLEditorPopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.REPLEditorPopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.REPLEditorPopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.REPLEditorPopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.REPLEditorPopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.REPLEditorPopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.REPLEditorPopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.REPLEditorPopupRotActive,
                RotOpenConfig = EditorConfig.Instance.REPLEditorPopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.REPLEditorPopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.REPLEditorPopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.REPLEditorPopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.REPLEditorPopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.REPLEditorPopupRotCloseEase,
            },
            new DialogAnimation("Editor Properties Popup")
            {
                ActiveConfig = EditorConfig.Instance.EditorPropertiesPopupActive,

                PosActiveConfig = EditorConfig.Instance.EditorPropertiesPopupPosActive,
                PosOpenConfig = EditorConfig.Instance.EditorPropertiesPopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.EditorPropertiesPopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.EditorPropertiesPopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.EditorPropertiesPopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.EditorPropertiesPopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.EditorPropertiesPopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.EditorPropertiesPopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.EditorPropertiesPopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.EditorPropertiesPopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.EditorPropertiesPopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.EditorPropertiesPopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.EditorPropertiesPopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.EditorPropertiesPopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.EditorPropertiesPopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.EditorPropertiesPopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.EditorPropertiesPopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.EditorPropertiesPopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.EditorPropertiesPopupRotActive,
                RotOpenConfig = EditorConfig.Instance.EditorPropertiesPopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.EditorPropertiesPopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.EditorPropertiesPopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.EditorPropertiesPopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.EditorPropertiesPopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.EditorPropertiesPopupRotCloseEase,
            },
            new DialogAnimation("Documentation Popup")
            {
                ActiveConfig = EditorConfig.Instance.DocumentationPopupActive,

                PosActiveConfig = EditorConfig.Instance.DocumentationPopupPosActive,
                PosOpenConfig = EditorConfig.Instance.DocumentationPopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.DocumentationPopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.DocumentationPopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.DocumentationPopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.DocumentationPopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.DocumentationPopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.DocumentationPopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.DocumentationPopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.DocumentationPopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.DocumentationPopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.DocumentationPopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.DocumentationPopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.DocumentationPopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.DocumentationPopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.DocumentationPopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.DocumentationPopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.DocumentationPopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.DocumentationPopupRotActive,
                RotOpenConfig = EditorConfig.Instance.DocumentationPopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.DocumentationPopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.DocumentationPopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.DocumentationPopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.DocumentationPopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.DocumentationPopupRotCloseEase,
            },
            new DialogAnimation("Debugger Popup")
            {
                ActiveConfig = EditorConfig.Instance.DebuggerPopupActive,

                PosActiveConfig = EditorConfig.Instance.DebuggerPopupPosActive,
                PosOpenConfig = EditorConfig.Instance.DebuggerPopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.DebuggerPopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.DebuggerPopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.DebuggerPopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.DebuggerPopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.DebuggerPopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.DebuggerPopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.DebuggerPopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.DebuggerPopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.DebuggerPopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.DebuggerPopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.DebuggerPopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.DebuggerPopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.DebuggerPopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.DebuggerPopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.DebuggerPopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.DebuggerPopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.DebuggerPopupRotActive,
                RotOpenConfig = EditorConfig.Instance.DebuggerPopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.DebuggerPopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.DebuggerPopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.DebuggerPopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.DebuggerPopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.DebuggerPopupRotCloseEase,
            },
            new DialogAnimation("Autosaves Popup")
            {
                ActiveConfig = EditorConfig.Instance.AutosavesPopupActive,

                PosActiveConfig = EditorConfig.Instance.AutosavesPopupPosActive,
                PosOpenConfig = EditorConfig.Instance.AutosavesPopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.AutosavesPopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.AutosavesPopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.AutosavesPopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.AutosavesPopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.AutosavesPopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.AutosavesPopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.AutosavesPopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.AutosavesPopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.AutosavesPopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.AutosavesPopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.AutosavesPopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.AutosavesPopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.AutosavesPopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.AutosavesPopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.AutosavesPopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.AutosavesPopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.AutosavesPopupRotActive,
                RotOpenConfig = EditorConfig.Instance.AutosavesPopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.AutosavesPopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.AutosavesPopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.AutosavesPopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.AutosavesPopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.AutosavesPopupRotCloseEase,
            },
            new DialogAnimation("Default Modifiers Popup")
            {
                ActiveConfig = EditorConfig.Instance.DefaultModifiersPopupActive,

                PosActiveConfig = EditorConfig.Instance.DefaultModifiersPopupPosActive,
                PosOpenConfig = EditorConfig.Instance.DefaultModifiersPopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.DefaultModifiersPopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.DefaultModifiersPopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.DefaultModifiersPopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.DefaultModifiersPopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.DefaultModifiersPopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.DefaultModifiersPopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.DefaultModifiersPopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.DefaultModifiersPopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.DefaultModifiersPopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.DefaultModifiersPopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.DefaultModifiersPopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.DefaultModifiersPopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.DefaultModifiersPopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.DefaultModifiersPopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.DefaultModifiersPopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.DefaultModifiersPopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.DefaultModifiersPopupRotActive,
                RotOpenConfig = EditorConfig.Instance.DefaultModifiersPopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.DefaultModifiersPopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.DefaultModifiersPopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.DefaultModifiersPopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.DefaultModifiersPopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.DefaultModifiersPopupRotCloseEase,
            },
            new DialogAnimation("Keybind List Popup")
            {
                ActiveConfig = EditorConfig.Instance.KeybindListPopupActive,

                PosActiveConfig = EditorConfig.Instance.KeybindListPopupPosActive,
                PosOpenConfig = EditorConfig.Instance.KeybindListPopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.KeybindListPopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.KeybindListPopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.KeybindListPopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.KeybindListPopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.KeybindListPopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.KeybindListPopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.KeybindListPopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.KeybindListPopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.KeybindListPopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.KeybindListPopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.KeybindListPopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.KeybindListPopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.KeybindListPopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.KeybindListPopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.KeybindListPopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.KeybindListPopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.KeybindListPopupRotActive,
                RotOpenConfig = EditorConfig.Instance.KeybindListPopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.KeybindListPopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.KeybindListPopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.KeybindListPopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.KeybindListPopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.KeybindListPopupRotCloseEase,
            },
            new DialogAnimation("Theme Popup")
            {
                ActiveConfig = EditorConfig.Instance.ThemePopupActive,

                PosActiveConfig = EditorConfig.Instance.ThemePopupPosActive,
                PosOpenConfig = EditorConfig.Instance.ThemePopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.ThemePopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.ThemePopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.ThemePopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.ThemePopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.ThemePopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.ThemePopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.ThemePopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.ThemePopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.ThemePopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.ThemePopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.ThemePopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.ThemePopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.ThemePopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.ThemePopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.ThemePopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.ThemePopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.ThemePopupRotActive,
                RotOpenConfig = EditorConfig.Instance.ThemePopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.ThemePopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.ThemePopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.ThemePopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.ThemePopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.ThemePopupRotCloseEase,
            },
            new DialogAnimation("Prefab Types Popup")
            {
                ActiveConfig = EditorConfig.Instance.PrefabTypesPopupActive,

                PosActiveConfig = EditorConfig.Instance.PrefabTypesPopupPosActive,
                PosOpenConfig = EditorConfig.Instance.PrefabTypesPopupPosOpen,
                PosCloseConfig = EditorConfig.Instance.PrefabTypesPopupPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.PrefabTypesPopupPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.PrefabTypesPopupPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.PrefabTypesPopupPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.PrefabTypesPopupPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.PrefabTypesPopupPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.PrefabTypesPopupPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.PrefabTypesPopupScaActive,
                ScaOpenConfig = EditorConfig.Instance.PrefabTypesPopupScaOpen,
                ScaCloseConfig = EditorConfig.Instance.PrefabTypesPopupScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.PrefabTypesPopupScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.PrefabTypesPopupScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.PrefabTypesPopupScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.PrefabTypesPopupScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.PrefabTypesPopupScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.PrefabTypesPopupScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.PrefabTypesPopupRotActive,
                RotOpenConfig = EditorConfig.Instance.PrefabTypesPopupRotOpen,
                RotCloseConfig = EditorConfig.Instance.PrefabTypesPopupRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.PrefabTypesPopupRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.PrefabTypesPopupRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.PrefabTypesPopupRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.PrefabTypesPopupRotCloseEase,
            },
            new DialogAnimation("File Dropdown")
            {
                ActiveConfig = EditorConfig.Instance.FileDropdownActive,

                PosActiveConfig = EditorConfig.Instance.FileDropdownPosActive,
                PosOpenConfig = EditorConfig.Instance.FileDropdownPosOpen,
                PosCloseConfig = EditorConfig.Instance.FileDropdownPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.FileDropdownPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.FileDropdownPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.FileDropdownPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.FileDropdownPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.FileDropdownPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.FileDropdownPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.FileDropdownScaActive,
                ScaOpenConfig = EditorConfig.Instance.FileDropdownScaOpen,
                ScaCloseConfig = EditorConfig.Instance.FileDropdownScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.FileDropdownScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.FileDropdownScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.FileDropdownScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.FileDropdownScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.FileDropdownScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.FileDropdownScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.FileDropdownRotActive,
                RotOpenConfig = EditorConfig.Instance.FileDropdownRotOpen,
                RotCloseConfig = EditorConfig.Instance.FileDropdownRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.FileDropdownRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.FileDropdownRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.FileDropdownRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.FileDropdownRotCloseEase,
            },
            new DialogAnimation("Edit Dropdown")
            {
                ActiveConfig = EditorConfig.Instance.EditDropdownActive,

                PosActiveConfig = EditorConfig.Instance.EditDropdownPosActive,
                PosOpenConfig = EditorConfig.Instance.EditDropdownPosOpen,
                PosCloseConfig = EditorConfig.Instance.EditDropdownPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.EditDropdownPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.EditDropdownPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.EditDropdownPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.EditDropdownPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.EditDropdownPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.EditDropdownPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.EditDropdownScaActive,
                ScaOpenConfig = EditorConfig.Instance.EditDropdownScaOpen,
                ScaCloseConfig = EditorConfig.Instance.EditDropdownScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.EditDropdownScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.EditDropdownScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.EditDropdownScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.EditDropdownScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.EditDropdownScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.EditDropdownScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.EditDropdownRotActive,
                RotOpenConfig = EditorConfig.Instance.EditDropdownRotOpen,
                RotCloseConfig = EditorConfig.Instance.EditDropdownRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.EditDropdownRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.EditDropdownRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.EditDropdownRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.EditDropdownRotCloseEase,
            },
            new DialogAnimation("View Dropdown")
            {
                ActiveConfig = EditorConfig.Instance.ViewDropdownActive,

                PosActiveConfig = EditorConfig.Instance.ViewDropdownPosActive,
                PosOpenConfig = EditorConfig.Instance.ViewDropdownPosOpen,
                PosCloseConfig = EditorConfig.Instance.ViewDropdownPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.ViewDropdownPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.ViewDropdownPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.ViewDropdownPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.ViewDropdownPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.ViewDropdownPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.ViewDropdownPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.ViewDropdownScaActive,
                ScaOpenConfig = EditorConfig.Instance.ViewDropdownScaOpen,
                ScaCloseConfig = EditorConfig.Instance.ViewDropdownScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.ViewDropdownScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.ViewDropdownScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.ViewDropdownScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.ViewDropdownScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.ViewDropdownScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.ViewDropdownScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.ViewDropdownRotActive,
                RotOpenConfig = EditorConfig.Instance.ViewDropdownRotOpen,
                RotCloseConfig = EditorConfig.Instance.ViewDropdownRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.ViewDropdownRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.ViewDropdownRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.ViewDropdownRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.ViewDropdownRotCloseEase,
            },
            new DialogAnimation("Steam Dropdown")
            {
                ActiveConfig = EditorConfig.Instance.SteamDropdownActive,

                PosActiveConfig = EditorConfig.Instance.SteamDropdownPosActive,
                PosOpenConfig = EditorConfig.Instance.SteamDropdownPosOpen,
                PosCloseConfig = EditorConfig.Instance.SteamDropdownPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.SteamDropdownPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.SteamDropdownPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.SteamDropdownPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.SteamDropdownPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.SteamDropdownPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.SteamDropdownPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.SteamDropdownScaActive,
                ScaOpenConfig = EditorConfig.Instance.SteamDropdownScaOpen,
                ScaCloseConfig = EditorConfig.Instance.SteamDropdownScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.SteamDropdownScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.SteamDropdownScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.SteamDropdownScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.SteamDropdownScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.SteamDropdownScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.SteamDropdownScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.SteamDropdownRotActive,
                RotOpenConfig = EditorConfig.Instance.SteamDropdownRotOpen,
                RotCloseConfig = EditorConfig.Instance.SteamDropdownRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.SteamDropdownRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.SteamDropdownRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.SteamDropdownRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.SteamDropdownRotCloseEase,
            },
            new DialogAnimation("Help Dropdown")
            {
                ActiveConfig = EditorConfig.Instance.HelpDropdownActive,

                PosActiveConfig = EditorConfig.Instance.HelpDropdownPosActive,
                PosOpenConfig = EditorConfig.Instance.HelpDropdownPosOpen,
                PosCloseConfig = EditorConfig.Instance.HelpDropdownPosClose,
                PosOpenDurationConfig = EditorConfig.Instance.HelpDropdownPosOpenDuration,
                PosCloseDurationConfig = EditorConfig.Instance.HelpDropdownPosCloseDuration,
                PosXOpenEaseConfig = EditorConfig.Instance.HelpDropdownPosXOpenEase,
                PosYOpenEaseConfig = EditorConfig.Instance.HelpDropdownPosYOpenEase,
                PosXCloseEaseConfig = EditorConfig.Instance.HelpDropdownPosXCloseEase,
                PosYCloseEaseConfig = EditorConfig.Instance.HelpDropdownPosYCloseEase,

                ScaActiveConfig = EditorConfig.Instance.HelpDropdownScaActive,
                ScaOpenConfig = EditorConfig.Instance.HelpDropdownScaOpen,
                ScaCloseConfig = EditorConfig.Instance.HelpDropdownScaClose,
                ScaOpenDurationConfig = EditorConfig.Instance.HelpDropdownScaOpenDuration,
                ScaCloseDurationConfig = EditorConfig.Instance.HelpDropdownScaCloseDuration,
                ScaXOpenEaseConfig = EditorConfig.Instance.HelpDropdownScaXOpenEase,
                ScaYOpenEaseConfig = EditorConfig.Instance.HelpDropdownScaYOpenEase,
                ScaXCloseEaseConfig = EditorConfig.Instance.HelpDropdownScaXCloseEase,
                ScaYCloseEaseConfig = EditorConfig.Instance.HelpDropdownScaYCloseEase,

                RotActiveConfig = EditorConfig.Instance.HelpDropdownRotActive,
                RotOpenConfig = EditorConfig.Instance.HelpDropdownRotOpen,
                RotCloseConfig = EditorConfig.Instance.HelpDropdownRotClose,
                RotOpenDurationConfig = EditorConfig.Instance.HelpDropdownRotOpenDuration,
                RotCloseDurationConfig = EditorConfig.Instance.HelpDropdownRotCloseDuration,
                RotOpenEaseConfig = EditorConfig.Instance.HelpDropdownRotOpenEase,
                RotCloseEaseConfig = EditorConfig.Instance.HelpDropdownRotCloseEase,
            },
        };

        public class DialogAnimation : Exists
        {
            public DialogAnimation(string name)
            {
                this.name = name;
            }

            public string name;

            #region Configs

            public ConfigEntry<bool> ActiveConfig { get; set; }

            // Position
            public ConfigEntry<bool> PosActiveConfig { get; set; }
            public ConfigEntry<Vector2> PosOpenConfig { get; set; }
            public ConfigEntry<Vector2> PosCloseConfig { get; set; }
            public ConfigEntry<Vector2> PosOpenDurationConfig { get; set; }
            public ConfigEntry<Vector2> PosCloseDurationConfig { get; set; }
            public ConfigEntry<Easings> PosXOpenEaseConfig { get; set; }
            public ConfigEntry<Easings> PosXCloseEaseConfig { get; set; }
            public ConfigEntry<Easings> PosYOpenEaseConfig { get; set; }
            public ConfigEntry<Easings> PosYCloseEaseConfig { get; set; }
            
            // Scale
            public ConfigEntry<bool> ScaActiveConfig { get; set; }
            public ConfigEntry<Vector2> ScaOpenConfig { get; set; }
            public ConfigEntry<Vector2> ScaCloseConfig { get; set; }
            public ConfigEntry<Vector2> ScaOpenDurationConfig { get; set; }
            public ConfigEntry<Vector2> ScaCloseDurationConfig { get; set; }
            public ConfigEntry<Easings> ScaXOpenEaseConfig { get; set; }
            public ConfigEntry<Easings> ScaXCloseEaseConfig { get; set; }
            public ConfigEntry<Easings> ScaYOpenEaseConfig { get; set; }
            public ConfigEntry<Easings> ScaYCloseEaseConfig { get; set; }
            
            // Rotation
            public ConfigEntry<bool> RotActiveConfig { get; set; }
            public ConfigEntry<float> RotOpenConfig { get; set; }
            public ConfigEntry<float> RotCloseConfig { get; set; }
            public ConfigEntry<float> RotOpenDurationConfig { get; set; }
            public ConfigEntry<float> RotCloseDurationConfig { get; set; }
            public ConfigEntry<Easings> RotOpenEaseConfig { get; set; }
            public ConfigEntry<Easings> RotCloseEaseConfig { get; set; }

            #endregion

            public bool Active => ActiveConfig.Value;

            public bool PosActive => PosActiveConfig.Value;
            public Vector2 PosStart => PosCloseConfig.Value;
            public Vector2 PosEnd => PosOpenConfig.Value;
            public float PosXStartDuration => PosOpenDurationConfig.Value.x;
            public float PosXEndDuration => PosCloseDurationConfig.Value.x;
            public string PosXStartEase => PosXOpenEaseConfig.Value.ToString();
            public string PosXEndEase => PosXCloseEaseConfig.Value.ToString();
            public float PosYStartDuration => PosOpenDurationConfig.Value.y;
            public float PosYEndDuration => PosCloseDurationConfig.Value.y;
            public string PosYStartEase => PosYOpenEaseConfig.Value.ToString();
            public string PosYEndEase => PosYCloseEaseConfig.Value.ToString();

            public bool ScaActive => ScaActiveConfig.Value;
            public Vector2 ScaStart => ScaCloseConfig.Value;
            public Vector2 ScaEnd => ScaOpenConfig.Value;
            public float ScaXStartDuration => ScaOpenDurationConfig.Value.x;
            public float ScaXEndDuration => ScaCloseDurationConfig.Value.x;
            public string ScaXStartEase => ScaXOpenEaseConfig.Value.ToString();
            public string ScaXEndEase => ScaXCloseEaseConfig.Value.ToString();
            public float ScaYStartDuration => ScaOpenDurationConfig.Value.y;
            public float ScaYEndDuration => ScaCloseDurationConfig.Value.y;
            public string ScaYStartEase => ScaYOpenEaseConfig.Value.ToString();
            public string ScaYEndEase => ScaYCloseEaseConfig.Value.ToString();

            public bool RotActive => RotActiveConfig.Value;
            public float RotStart => RotCloseConfig.Value;
            public float RotEnd => RotOpenConfig.Value;
            public float RotStartDuration => RotOpenDurationConfig.Value;
            public float RotEndDuration => RotCloseDurationConfig.Value;
            public string RotStartEase => RotOpenEaseConfig.Value.ToString();
            public string RotEndEase => RotCloseEaseConfig.Value.ToString();
        }

        public List<NewLevelTemplate> NewLevelTemplates { get; set; } = new List<NewLevelTemplate>();

        public class NewLevelTemplate
        {
            public GameObject GameObject { get; set; }
            public string filePath;
            public Image Preview { get; set; }
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

                var p = PropCategories.FindIndex(x => x.ToString() == _configEntry.Definition.Section.Replace(" ", ""));

                propCategory = p >= 0 ? PropCategories[p] : EditorPropCategory.General;
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
                EditorPropCategory.Animations,
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
                Animations,
                Fields,
                Preview
            }
        }

        #endregion
    }
}
