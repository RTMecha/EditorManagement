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

        public static void Init(EditorManager editorManager) => editorManager?.gameObject?.AddComponent<RTEditor>();

        void Awake()
        {
            inst = this;

            SetupNotificationValues();
            SetupTimelineBar();
            SetupTimelineTriggers();
            SetupSelectGUI();
            SetupCreateObjects();
            SetupDropdowns();
            SetupDoggo();
            SetupPaths();
            CreateObjectSearch();
            CreateWarningPopup();
            CreateREPLEditor();
            CreateMultiObjectEditor();

            if (!RTFile.FileExists(EditorSettingsPath))
                CreatePaths();
            else
                LoadPaths();

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

            //timelineKeyframes.Cast<TimelineObject<BaseEventKeyframe>>().ToList();

            try
            {
                doggoObject = GameObject.Find("Editor Systems/Editor GUI/sizer/main/Popups/File Info Popup/loading");
                doggoImage = doggoObject.GetComponent<Image>();
                timelineTime = EditorManager.inst.timelineTime.GetComponent<Text>();
            }
            catch
            {

            }
        }

        void Update()
        {
            foreach (var timelineObject in timelineObjects)
            {
                if (timelineObject.Data != null && timelineObject.GameObject && timelineObject.Image)
                {
                    bool isCurrentLayer = timelineObject.Layer == Layer && layerType == LayerType.Objects;
                    timelineObject.GameObject.SetActive(isCurrentLayer);
                    if (isCurrentLayer)
                    {
                        var color = timelineObject.selected ? ObjEditor.inst.SelectedColor :
                            timelineObject.IsBeatmapObject && !string.IsNullOrEmpty(timelineObject.GetData<BeatmapObject>().prefabID) ? timelineObject.GetData<BeatmapObject>().GetPrefabTypeColor() :
                            timelineObject.IsPrefabObject ? timelineObject.GetData<PrefabObject>().GetPrefabTypeColor() : ObjEditor.inst.NormalColor;

                        timelineObject.Image.color = color;
                    }
                }
            }

            foreach (var timelineObject in timelineBeatmapObjectKeyframes)
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
                    timelineObject.GameObject.SetActive(true);

                    timelineObject.Image.color = timelineObject.selected ? EventEditor.inst.Selected : EventEditor.inst.EventColors[timelineObject.Type % RTEventEditor.EventLimit];
                }
            }
        }

        public static void Nullify()
        {
            if (inst)
            {
                inst.objectToParent = null;
            }
        }

        #region Variables

        //public string LookingAtDialog { get; set; }

        public static bool RoundToNearest => GetEditorProperty("Round To Nearest").GetConfigEntry<bool>().Value;
        public static bool ShowModifiedColors => GetEditorProperty("Show Modified Colors").GetConfigEntry<bool>().Value;
        public static float BPMSnapDivisions => GetEditorProperty("BPM Snap Divisions").GetConfigEntry<float>().Value;
        public static bool BPMSnapKeyframes => GetEditorProperty("BPM Snaps Keyframes").GetConfigEntry<bool>().Value;

        public bool ienumRunning;
        public bool parentPickerEnabled;
        public BaseBeatmapObject objectToParent;

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

        public string objectSearchTerm;

        public GameObject replBase;
        public InputField replEditor;
        public Text replText;

        public Transform titleBar;

        public Text fileInfoText;

        public GameObject doggoObject;
        public Image doggoImage;

        public Text timelineTime;

        public Image timelineSliderHandle;
        public Image timelineSliderRuler;
        public Image keyframeTimelineSliderHandle;
        public Image keyframeTimelineSliderRuler;

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

        public static float SnapToBPM(float _time) => Mathf.RoundToInt(_time / (SettingEditor.inst.BPMMulti / BPMSnapDivisions)) * (SettingEditor.inst.BPMMulti / BPMSnapDivisions);

        #endregion

        #region Timeline Objects

        public List<TimelineObject> timelineObjects = new List<TimelineObject>();
        public List<TimelineObject> timelineKeyframes = new List<TimelineObject>();
        public List<TimelineObject> timelineBeatmapObjectKeyframes = new List<TimelineObject>();

        //public Dictionary<string, TimelineObject> TimelineObjectsDictionary => timelineObjects.ToDictionary(x => x.ID, x => x);

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

        #endregion

        #region Timeline Textures

        public static bool GenerateWaveform => true;

        public static WaveformType WaveformMode => GetEditorProperty("Waveform Mode").GetConfigEntry<WaveformType>().Value;
        public static Color WaveformBGColor => GetEditorProperty("Waveform BG Color").GetConfigEntry<Color>().Value;
        public static Color WaveformTopColor => GetEditorProperty("Waveform Top Color").GetConfigEntry<Color>().Value;
        public static Color WaveformBottomColor => GetEditorProperty("Waveform Bottom Color").GetConfigEntry<Color>().Value;
        public static TextureFormat WaveformFormat => GetEditorProperty("Waveform Texture Format").GetConfigEntry<TextureFormat>().Value;

        public IEnumerator AssignTimelineTexture()
        {
            //int num = Mathf.Clamp((int)AudioManager.inst.CurrentAudioSource.clip.length * 48, 100, 15000);
            int num = Mathf.Clamp((int)AudioManager.inst.CurrentAudioSource.clip.length * 48, 100, 15000);
            Texture2D waveform = null;

            if (WaveformMode == WaveformType.Legacy)
                yield return inst.StartCoroutine(Legacy(AudioManager.inst.CurrentAudioSource.clip, num, 300, WaveformBGColor, WaveformTopColor, WaveformBottomColor, delegate (Texture2D _tex) { waveform = _tex; }));
            if (WaveformMode == WaveformType.Beta)
                yield return inst.StartCoroutine(Beta(AudioManager.inst.CurrentAudioSource.clip, num, 300, WaveformBGColor, WaveformTopColor, delegate (Texture2D _tex) { waveform = _tex; }));
            if (WaveformMode == WaveformType.BetaFast)
                yield return inst.StartCoroutine(BetaFast(AudioManager.inst.CurrentAudioSource.clip, 1f, num, 300, WaveformBGColor, WaveformTopColor, delegate (Texture2D _tex) { waveform = _tex; }));
            if (WaveformMode == WaveformType.LegacyFast)
                yield return inst.StartCoroutine(LegacyFast(AudioManager.inst.CurrentAudioSource.clip, 1f, num, 300, WaveformBGColor, WaveformTopColor, WaveformBottomColor, delegate (Texture2D _tex) { waveform = _tex; }));

            var waveSprite = Sprite.Create(waveform, new Rect(0f, 0f, (float)num, 300f), new Vector2(0.5f, 0.5f), 100f);
            TimelineImage.sprite = waveSprite;
            TimelineOverlayImage.sprite = TimelineImage.sprite;
            yield break;
        }

        public static object SetColor(Color[] array, int num, Color color)
        {
            array[num] = color;
            return null;
        }

        public static IEnumerator Beta(AudioClip clip, int textureWidth, int textureHeight, Color background, Color waveform, Action<Texture2D> action)
        {
            Debug.LogFormat("{0}Generating Beta Waveform", EditorPlugin.className);
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            int num = 100;
            Texture2D texture2D = new Texture2D(textureWidth, textureHeight, WaveformFormat, false);
            Color[] array = new Color[texture2D.width * texture2D.height];
            for (int i = 0; i < array.Length; i++)
            {
                //array[i] = background;

                yield return SetColor(array, i, background);
            }
            Debug.LogFormat("{0}Generating Beta Waveform at {1}", EditorPlugin.className, sw.Elapsed);
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
            Debug.LogFormat("{0}Generating Beta Waveform at {1}", EditorPlugin.className, sw.Elapsed);
            for (int l = 0; l < array3.Length - 1; l++)
            {
                int num2 = 0;
                while ((float)num2 < (float)textureHeight * array3[l] + 1f)
                {
                    texture2D.SetPixel(textureWidth * l / array3.Length, (int)((float)textureHeight * (array3[l] + 1f) / 2f) - num2, waveform);
                    num2++;
                }
            }
            Debug.LogFormat("{0}Generating Beta Waveform at {1}", EditorPlugin.className, sw.Elapsed);
            texture2D.wrapMode = TextureWrapMode.Clamp;
            texture2D.filterMode = FilterMode.Point;
            texture2D.Apply();
            action(texture2D);
            Debug.LogFormat("{0}Generating Beta Waveform at {1}", EditorPlugin.className, sw.Elapsed);
            sw.Stop();
            yield break;
        }

        public static IEnumerator Legacy(AudioClip clip, int textureWidth, int textureHeight, Color background, Color _top, Color _bottom, Action<Texture2D> action)
        {
            Debug.LogFormat("{0}Generating Legacy Waveform", EditorPlugin.className);
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            int num = 160;
            num = clip.frequency / num;
            Texture2D texture2D = new Texture2D(textureWidth, textureHeight, WaveformFormat, false);
            Color[] array = new Color[texture2D.width * texture2D.height];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = background;
            }
            Debug.LogFormat("{0}Generating Legacy Waveform at {1}", EditorPlugin.className, sw.Elapsed);
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
            Debug.LogFormat("{0}Generating Legacy Waveform at {1}", EditorPlugin.className, sw.Elapsed);
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
            Debug.LogFormat("{0}Generating Legacy Waveform at {1}", EditorPlugin.className, sw.Elapsed);
            for (int l = 0; l < array6.Length - 1; l++)
            {
                int num2 = 0;
                while ((float)num2 < (float)textureHeight * array6[l])
                {
                    texture2D.SetPixel(textureWidth * l / array6.Length, (int)((float)textureHeight * array6[l]) - num2, _top);
                    num2++;
                }
            }
            Debug.LogFormat("{0}Generating Legacy Waveform at {1}", EditorPlugin.className, sw.Elapsed);
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
            Debug.LogFormat("{0}Generating Legacy Waveform at {1}", EditorPlugin.className, sw.Elapsed);
            for (int num3 = 0; num3 < array6.Length - 1; num3++)
            {
                int num4 = 0;
                while ((float)num4 < (float)textureHeight * array6[num3])
                {
                    int x = textureWidth * num3 / array6.Length;
                    int y = (int)array4[num3 * num + num4] - num4;
                    if (texture2D.GetPixel(x, y) == _top)
                    {
                        texture2D.SetPixel(x, y, MixColors(new List<Color> { _top, _bottom }));
                    }
                    else
                    {
                        texture2D.SetPixel(x, y, _bottom);
                    }
                    num4++;
                }
            }
            Debug.LogFormat("{0}Generating Legacy Waveform at {1}", EditorPlugin.className, sw.Elapsed);
            texture2D.wrapMode = TextureWrapMode.Clamp;
            texture2D.filterMode = FilterMode.Point;
            texture2D.Apply();
            action(texture2D);
            Debug.LogFormat("{0}Generating Legacy Waveform at {1}", EditorPlugin.className, sw.Elapsed);
            sw.Stop();
            yield break;
        }

        public static IEnumerator BetaFast(AudioClip audio, float saturation, int width, int height, Color background, Color col, Action<Texture2D> action)
        {
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

        public static IEnumerator LegacyFast(AudioClip audio, float saturation, int width, int height, Color background, Color colTop, Color colBot, Action<Texture2D> action)
        {
            Debug.LogFormat("{0}Generating Legacy Waveform (Fast)", EditorPlugin.className);
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
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
                    if (tex.GetPixel(x, y) == colTop)
                    {
                        tex.SetPixel(x, y, MixColors(new List<Color> { colTop, colBot }));
                    }
                    else
                    {
                        tex.SetPixel(x, y, colBot);
                    }
                }
            }
            tex.Apply();

            action(tex);
            Debug.LogFormat("{0}Generated Legacy Waveform at {1}", EditorPlugin.className, sw.Elapsed);
            sw.Stop();
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

        #endregion

        #region Paths

        public int levelFilter;
        public bool levelAscend;

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

        public static void CreatePaths()
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

                RTFile.WriteToFile(EditorSettingsPath, jn.ToString(3));
            }
        }

        public static void LoadPaths()
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
        }

        public static void SavePaths()
        {
            var jn = JSON.Parse(RTFile.ReadFromFile(EditorSettingsPath));

            jn["paths"]["editor"] = EditorPath;
            jn["paths"]["themes"] = ThemePath;
            jn["paths"]["prefabs"] = PrefabPath;

            RTFile.WriteToFile(EditorSettingsPath, jn.ToString(3));
        }

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
                EventEditor.inst.PasteEvents();
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

        public static void PasteObject(float _offsetTime = 0f, bool _regen = true)
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

            inst.StartCoroutine(ObjectEditor.inst.AddPrefabExpandedToLevel(pr == null ? ObjEditor.inst.beatmapObjCopy : pr, true, _offsetTime, false, _regen));
        }

        public void Delete()
        {
            if (IsObjectDialog)
            {
                if (ObjEditor.inst.currentKeyframe != 0)
                {
                    inst.StartCoroutine(ObjectEditor.inst.DeleteKeyframes());
                    //EditorManager.inst.DisplayNotification("Deleted Beatmap Object Keyframe.", 1f, EditorManager.NotificationType.Success);
                }
                else
                    EditorManager.inst.DisplayNotification("Can't Delete First Keyframe.", 1f, EditorManager.NotificationType.Error);
                return;
            }
            if (IsTimeline)
            {
                if (DataManager.inst.gameData.beatmapObjects.Count > 1)
                {
                    //if (ObjectEditor.inst.SelectedObjectCount > 1)
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
                    //else
                    //{
                    //    Debug.LogFormat("{0}Deleting single object...", EditorPlugin.className);
                    //    float startTime = ObjectEditor.inst.CurrentSelection.Time;

                    //    Debug.LogFormat("{0}Assigning prefab for undo...", EditorPlugin.className);
                    //    var prefab = new Prefab("deleted object", 0, startTime, ObjectEditor.inst.SelectedObjects.Select(x => x.GetData<BeatmapObject>()).ToList(), ObjectEditor.inst.SelectedObjects.Select(x => x.GetData<PrefabObject>()).ToList());

                    //    Debug.LogFormat("{0}Setting history...", EditorPlugin.className);
                    //    EditorManager.inst.history.Add(new History.Command("Delete Object", delegate ()
                    //    {
                    //        Delete();
                    //    }, delegate ()
                    //    {
                    //        ObjectEditor.inst.DeselectAllObjects();
                    //        inst.StartCoroutine(ObjectEditor.inst.AddPrefabExpandedToLevel(prefab, true, 0f, true));
                    //    }), false);

                    //    Debug.LogFormat("{0}Finally deleting object...", EditorPlugin.className);
                    //    StartCoroutine(ObjectEditor.inst.DeleteObject(ObjectEditor.inst.CurrentSelection));

                    //    EditorManager.inst.DisplayNotification("Deleted Beatmap Object!", 1f, EditorManager.NotificationType.Success);
                    //    Debug.LogFormat("{0}Done!", EditorPlugin.className);
                    //}
                }
                else
                    EditorManager.inst.DisplayNotification("Can't Delete Only Beatmap Object", 1f, EditorManager.NotificationType.Error);
                return;
            }
            if (EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Event))
            {
                if (RTEventEditor.inst.SelectedKeyframes.Count > 1)
                {
                    EditorManager.inst.ClearDialogs(EditorManager.EditorDialog.DialogType.Event);

                    //EditorManager.inst.history.Add(new History.Command("Delete Event Keyframes", delegate ()
                    //{
                    //	List<EventEditor.KeyframeSelection> list3 = new List<EventEditor.KeyframeSelection>();
                    //	foreach (var keyframeSelection in EventEditor.inst.keyframeSelections)
                    //	{
                    //		if (keyframeSelection.Index != 0)
                    //		{
                    //			list3.Add(keyframeSelection);
                    //		}
                    //		else
                    //		{
                    //			EditorManager.inst.DisplayNotification("Can't Delete First Event Keyframe.", 1f, EditorManager.NotificationType.Error, false);
                    //		}
                    //	}
                    //	EditorManager.inst.ClearDialogs(new EditorManager.EditorDialog.DialogType[]
                    //	{
                    //	EditorManager.EditorDialog.DialogType.Event
                    //	});
                    //	list3 = (from x in list3
                    //			 orderby x.Index descending
                    //			 select x).ToList();
                    //	inst.StartCoroutine(DeleteEvent(list3));
                    //}, delegate ()
                    //{
                    //	PasteEventKeyframes(dictionary);
                    //}));

                    StartCoroutine(RTEventEditor.inst.DeleteKeyframes());
                    EditorManager.inst.DisplayNotification("Deleted Event Keyframes.", 1f, EditorManager.NotificationType.Success);
                }
                else if (EventEditor.inst.currentEvent != 0)
                {
                    EventEditor.inst.DeleteEvent(EventEditor.inst.currentEventType, EventEditor.inst.currentEvent);
                    EditorManager.inst.DisplayNotification("Deleted Event Keyframe.", 1f, EditorManager.NotificationType.Success);
                }
                else
                {
                    EditorManager.inst.DisplayNotification("Can't Delete First Event Keyframe.", 1f, EditorManager.NotificationType.Error);
                }
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
                EditorManager.inst.DisplayNotification("Can't Delete First Checkpoint.", 1f, EditorManager.NotificationType.Error);
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

        public LayerType layerType;

        public enum LayerType
        {
            Objects,
            Events
        }

        public static int GetLayer(int _layer) => Mathf.Clamp(_layer, 0, int.MaxValue);

        public static string GetLayerString(int _layer) => (_layer + 1).ToString();

        public static Color GetLayerColor(int _layer)
        {
            if (_layer < EditorManager.inst.layerColors.Count)
                return EditorManager.inst.layerColors[_layer];
            return Color.white;
        }

        public void SetLayer(LayerType layerType)
        {
            this.layerType = layerType;
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
            
            switch (layerType)
            {
                case LayerType.Objects:
                    {
                        EventEditor.inst.EventLabels.SetActive(false);
                        EventEditor.inst.EventHolders.SetActive(false);

                        foreach (var timelineObject in timelineKeyframes)
                            Destroy(timelineObject.GameObject);

                        ObjectEditor.inst.RenderTimelineObjects();

                        if (CheckpointEditor.inst.checkpoints.Count > 0)
                        {
                            foreach (var obj2 in CheckpointEditor.inst.checkpoints)
                                Destroy(obj2);

                            CheckpointEditor.inst.checkpoints.Clear();
                        }

                        CheckpointEditor.inst.CreateGhostCheckpoints();
                        if (LayerToggle)
                            LayerToggle.isOn = false;
                        break;
                    }
                case LayerType.Events:
                    {
                        EventEditor.inst.EventLabels.SetActive(true);
                        EventEditor.inst.EventHolders.SetActive(true);
                        RTEventEditor.inst.CreateEventObjects();
                        CheckpointEditor.inst.CreateCheckpoints();

                        RTEventEditor.inst.RenderLayerBins();

                        if (LayerToggle)
                            LayerToggle.isOn = true;
                        break;
                    }
            }

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

            Debug.LogFormat("{0}Removing unused object", EditorPlugin.className);
            var t = timelineBar.transform.Find("Time");
            defaultIF = t.gameObject;
            defaultIF.SetActive(true);
            t.SetParent(null);
            __instance.speedText.transform.parent.SetParent(null);

            if (defaultIF.TryGetComponent(out InputField frick))
            {
                frick.textComponent.fontSize = 19;
            }

            Debug.LogFormat("{0}Instantiating new time object", EditorPlugin.className);
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
                timeIF.text = AudioManager.inst.CurrentAudioSource.time.ToString();
                timeIF.characterValidation = InputField.CharacterValidation.Decimal;

                timeIF.onValueChanged.AddListener(delegate (string _value)
                {
                    //SetNewTime(_value);
                });
            }

            Debug.LogFormat("{0}Instantiating new layer object", EditorPlugin.className);
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
                layersIF.onValueChanged.RemoveAllListeners();
                layersIF.onValueChanged.AddListener(delegate (string _value)
                {
                    if (int.TryParse(_value, out int num))
                    {
                        SetLayer(Mathf.Clamp(num - 1, 0, int.MaxValue));
                    }
                });

                layerImage.color = GetLayerColor(EditorManager.inst.layer);

                TriggerHelper.AddEventTrigger(layersObj, new List<EventTrigger.Entry> { TriggerHelper.ScrollDeltaInt(layersIF, 1, 1, int.MaxValue) });
            }

            Debug.LogFormat("{0}Instantiating new pitch object", EditorPlugin.className);
            var pitchObj = Instantiate(timeObj);
            {
                pitchObj.transform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject").transform);
                pitchObj.transform.SetSiblingIndex(5);
                pitchObj.name = "pitch";
                pitchObj.transform.localScale = Vector3.one;

                pitchIF = pitchObj.GetComponent<InputField>();
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

            timelineBar.transform.Find("checkpoint").gameObject.SetActive(false);
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

                    //if (et.triggers.Count > 3)
                    //{
                    //	et.triggers.RemoveAt(3);
                    //}

                    int typeTmp = i;
                    var entry3 = new EventTrigger.Entry();
                    entry3.eventID = EventTriggerType.PointerDown;
                    entry3.callback.AddListener(delegate (BaseEventData eventData)
                    {
                        Debug.LogFormat("{0}EventHolder: {1}\nActual Event: {2}", EditorPlugin.className, typeTmp, typeTmp + 14);
                        if (((PointerEventData)eventData).button == PointerEventData.InputButton.Right)
                        {
                            var layer = Layer + 1;
                            int num = layer * RTEventEditor.EventLimit;

                            if (RTEventEditor.EventTypes.Length > typeTmp * num && DataManager.inst.gameData.eventObjects.allEvents.Count > typeTmp * num)
                                RTEventEditor.inst.NewKeyframeFromTimeline(typeTmp * num);
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

            // Quit to Arcade
            {
                var exitToArcade = Instantiate(titleBar.Find("File/File Dropdown/Quit to Main Menu").gameObject);
                exitToArcade.name = "Quit to Arcade";
                exitToArcade.transform.SetParent(titleBar.Find("File/File Dropdown"));
                exitToArcade.transform.localScale = Vector3.one;
                exitToArcade.transform.SetSiblingIndex(7);
                exitToArcade.transform.GetChild(0).GetComponent<Text>().text = "Quit to Arcade";

                var ex = exitToArcade.GetComponent<Button>();
                ex.onClick.ClearAll();
                ex.onClick.AddListener(delegate ()
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
                });
            }

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
                    if (ModCompatibility.mods["ExampleCompanion"].methods.ContainsKey("InitExample"))
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
                EditorManager.inst.RenderOpenBeatmapPopup();
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
                EditorManager.inst.RenderOpenBeatmapPopup();
            });

            if (toggle.gameObject)
                TooltipHelper.AddTooltip(toggle.gameObject, new List<HoverTooltip.Tooltip> { sortListTip.tooltipLangauges[0] });

            CreatePaths();
            LoadPaths();

            // EditorPath
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
                    SavePaths();
                });

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

            //ThemePath
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
                    SavePaths();
                });

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
                    StartCoroutine(LoadThemes());
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

            //PrefabPath
            {
                var prefabPathGO = timeIF.gameObject.Duplicate(EditorManager.inst.GetDialog("Prefab Popup").Dialog.Find("external prefabs"), "prefabs path");

                ((RectTransform)prefabPathGO.transform).anchoredPosition = GetEditorProperty("Prefab External Prefab Path Pos").GetConfigEntry<Vector2>().Value;
                ((RectTransform)prefabPathGO.transform).sizeDelta = new Vector2(GetEditorProperty("Prefab External Prefab Path Length").GetConfigEntry<float>().Value, 34f);

                var levelListTip = prefabPathGO.AddComponent<HoverTooltip>();
                var llTip = new HoverTooltip.Tooltip();

                llTip.desc = "Prefab list path";
                llTip.hint = "Input the path you want to load prefabs from within the beatmaps folder. For example: inputting \"prefabs\" into the input field will load levels from beatmaps/prefabs. You can also set it to sub-directories, like: \"prefabs/pa characters\" will take levels from \"beatmaps/prefabs/pa characters\".";

                levelListTip.tooltipLangauges.Add(llTip);

                InputField levelListIF = prefabPathGO.GetComponent<InputField>();
                levelListIF.characterValidation = InputField.CharacterValidation.None;
                levelListIF.text = PrefabPath;

                levelListIF.onValueChanged.RemoveAllListeners();
                levelListIF.onValueChanged.AddListener(delegate (string _val)
                {
                    PrefabPath = _val;
                    SavePaths();
                });

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
                RefreshObjectSearch(delegate (BeatmapObject beatmapObject)
                {
                    ObjectEditor.inst.SetCurrentObject(new TimelineObject(beatmapObject), true);
                });
            });
            searchBar.transform.Find("Placeholder").GetComponent<Text>().text = "Search for object...";

            // Turn this into a separate method
            var propWin = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/Edit/Edit Dropdown/Cut")
                .Duplicate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/Edit/Edit Dropdown").transform, "Search Objects");

            propWin.transform.Find("Text").GetComponent<Text>().text = "Search Objects";
            ((RectTransform)propWin.transform.Find("Text")).sizeDelta = new Vector2(224f, 0f);
            propWin.transform.Find("Text 1").GetComponent<Text>().text = "";

            var propWinButton = propWin.GetComponent<Button>();
            propWinButton.onClick.ClearAll();
            propWinButton.onClick.AddListener(delegate ()
            {
                EditorManager.inst.ShowDialog("Object Search Popup");
                RefreshObjectSearch(delegate (BeatmapObject beatmapObject)
                {
                    ObjectEditor.inst.SetCurrentObject(new TimelineObject(beatmapObject), true);
                });
            });

            propWin.SetActive(true);

            propWin.transform.Find("Image").GetComponent<Image>().sprite = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/parent/parent/image").GetComponent<Image>().sprite;

            //EditorHelper.AddEditorDropdown("Search Objects", "", "Edit", GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/parent/parent/image").GetComponent<Image>().sprite, delegate ()
            //{
            //    EditorManager.inst.ShowDialog("Object Search Popup");
            //    RefreshObjectSearch(delegate (BeatmapObject beatmapObject)
            //    {
            //        ObjectEditor.inst.SetCurrentObject(new TimelineObject(beatmapObject), true);
            //    });
            //});

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

            var rtext = Instantiate(replEditor.textComponent.gameObject);
            rtext.transform.SetParent(replEditor.transform);
            rtext.transform.localScale = Vector3.one;

            var rttext = rtext.GetComponent<RectTransform>();
            rttext.anchoredPosition = new Vector2(2f, 0f);
            rttext.sizeDelta = new Vector2(-12f, -8f);

            var selectUI = ((GameObject)uiTop["GameObject"]).AddComponent<SelectGUI>();
            selectUI.target = replBase.transform;

            //((RectTransform)replEditor.textComponent.transform).anchoredPosition = new Vector2(9999f, 9999f);
            replEditor.textComponent.color = new Color(0.9788679f, 0.9788679f, 0.9788679f, 0f);
            //replEditor.textComponent.GetComponent<CanvasRenderer>().cull = true;

            replEditor.customCaretColor = true;
            replEditor.caretColor = new Color(0.9788679f, 0.9788679f, 0.9788679f, 1f);

            replText = rtext.GetComponent<Text>();

            var uiBottom = UIManager.GenerateUIImage("Panel", replBase.transform);

            UIManager.SetRectTransform((RectTransform)uiBottom["RectTransform"], new Vector2(0f, -291f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1000f, 32f));

            ((Image)uiBottom["Image"]).color = new Color(0.1973585f, 0.1973585f, 0.1973585f);

            var evaluator = UIManager.GenerateUIButton("Evaluate", ((GameObject)uiBottom["GameObject"]).transform);

            var button = (Button)evaluator["Button"];
            button.onClick.AddListener(delegate ()
            {
                RTCode.Evaluate(replEditor.text);
            });

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

            //dataLeft.GetChild(1).GetChild(0).gameObject.GetComponent<Text>().text = "Set Group Layer";

            dataLeft.GetChild(3).gameObject.SetActive(true);

            dataLeft.GetChild(3).gameObject.name = "label depth";

            //dataLeft.GetChild(3).GetChild(0).gameObject.GetComponent<Text>().text = "Set Group Depth";

            dataLeft.GetChild(1).SetParent(parent);

            dataLeft.GetChild(2).SetParent(parent);

            var textHolder = EditorManager.inst.GetDialog("Multi Object Editor").Dialog.Find("data/right/text holder/Text");
            var textHolderText = textHolder.GetComponent<Text>();
            textHolderText.text = textHolderText.text.Replace(
                "The current version of the editor doesn't support any editing functionality.",
                "On the left you'll see all the multi object editor tools you'll need.");

            textHolderText.fontSize = 22;

            textHolder.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -125f);

            textHolder.GetComponent<RectTransform>().sizeDelta = new Vector2(-68f, 0f);

            var zoom = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/zoom/zoom");

            var labelL = parent.Find("label layer");
            labelL.SetParent(null);
            Destroy(parent.Find("label depth").gameObject);

            Action<string, int, string, bool, UnityAction, UnityAction, UnityAction> action
                = delegate (string name, int siblingIndex, string placeHolder, bool doMiddle, UnityAction leftButton, UnityAction middleButton, UnityAction rightButton)
            {
                var gameObject = zoom.Duplicate(parent, name, siblingIndex);
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
                label.transform.GetChild(0).gameObject.GetComponent<Text>().text = name;
            };

            Action<string, string, Transform, UnityAction> buttonGenerator = delegate (string name, string text, Transform parent, UnityAction unityAction)
            {
                var gameObject = eventButton.Duplicate(parent, name);

                ((RectTransform)gameObject.transform).sizeDelta = new Vector2(404f, 32f);

                gameObject.transform.GetChild(0).GetComponent<Text>().text = text;
                gameObject.GetComponent<Image>().color = bcol;

                var button = gameObject.GetComponent<Button>();
                button.onClick.ClearAll();
                button.onClick.AddListener(unityAction);
            };

            //Layers
            {
                labelGenerator("Set Group Layer");

                action("layer", 1, "Enter layer...", true, delegate ()
                {
                    if (parent.Find("layer") && parent.Find("layer").GetChild(0).gameObject.TryGetComponent(out InputField inputField))
                    {
                        if (int.TryParse(inputField.text, out int num))
                        {
                            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                            {
                                if (timelineObject.IsBeatmapObject)
                                    timelineObject.GetData<BeatmapObject>().editorData.Layer = Mathf.Clamp(timelineObject.GetData<BeatmapObject>().editorData.Layer - 1, 0, int.MaxValue);
                                if (timelineObject.IsPrefabObject)
                                    timelineObject.GetData<PrefabObject>().editorData.Layer = Mathf.Clamp(timelineObject.GetData<PrefabObject>().editorData.Layer - 1, 0, int.MaxValue);
                            }
                        }
                    }

                }, delegate ()
                {
                    if (parent.Find("layer") && parent.Find("layer").GetChild(0).gameObject.TryGetComponent(out InputField inputField))
                    {
                        inputField.text = "1";
                        TriggerHelper.AddEventTrigger(parent.Find("layer").gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDeltaInt(inputField, min: 0) });

                        if (int.TryParse(inputField.text, out int num))
                        {
                            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                            {
                                if (timelineObject.IsBeatmapObject)
                                    timelineObject.GetData<BeatmapObject>().editorData.Layer = Mathf.Clamp(num - 1, 0, int.MaxValue);
                                if (timelineObject.IsPrefabObject)
                                    timelineObject.GetData<PrefabObject>().editorData.Layer = Mathf.Clamp(num - 1, 0, int.MaxValue);
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
                                    timelineObject.GetData<BeatmapObject>().editorData.Layer = Mathf.Clamp(timelineObject.GetData<BeatmapObject>().editorData.Layer + 1, 0, int.MaxValue);
                                if (timelineObject.IsPrefabObject)
                                    timelineObject.GetData<PrefabObject>().editorData.Layer = Mathf.Clamp(timelineObject.GetData<PrefabObject>().editorData.Layer + 1, 0, int.MaxValue);
                            }
                        }
                    }
                });
            }

            //Depth
            {
                labelGenerator("Set Group Depth");

                action("depth", parent.childCount - 1, "Enter depth...", true, delegate ()
                {
                    if (parent.Find("depth") && parent.Find("depth").GetChild(0).gameObject.TryGetComponent(out InputField inputField))
                    {
                        if (int.TryParse(inputField.text, out int num))
                        {
                            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                bm.Depth -= num;
                            }
                        }
                    }
                }, delegate ()
                {
                    if (parent.Find("depth") && parent.Find("depth").GetChild(0).gameObject.TryGetComponent(out InputField inputField))
                    {
                        inputField.text = "15";
                        TriggerHelper.AddEventTrigger(parent.Find("depth").gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDeltaInt(inputField, min: 0) });

                        if (int.TryParse(inputField.text, out int num))
                        {
                            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                bm.Depth = num;
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
                            }
                        }
                    }
                });
            }

            //Song Time
            {
                labelGenerator("Set Song Time");

                action("time", parent.childCount - 1, "Enter time...", true, delegate ()
                {
                    if (parent.Find("time") && parent.Find("time").GetChild(0).gameObject.TryGetComponent(out InputField inputField))
                    {
                        if (float.TryParse(inputField.text, out float num))
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
                    }
                }, delegate ()
                {
                    if (parent.Find("time") && parent.Find("time").GetChild(0).gameObject.TryGetComponent(out InputField inputField))
                    {
                        inputField.text = "0";
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
                    if (parent.Find("time") && parent.Find("time").GetChild(0).gameObject.TryGetComponent(out InputField inputField))
                    {
                        if (int.TryParse(inputField.text, out int num))
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
                    }
                });
            }

            //Name
            {
                labelGenerator("Set Name");

                var multiNameSet = zoom.Duplicate(parent, "name");

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

            //Song Time Autokill
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
                        Updater.UpdateProcessor(timelineObject.GetData<BeatmapObject>(), "StartTime");
                    }
                });
            }

            //Cycle Object Type
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

            //Lock Swap
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

            //Lock Toggle
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

            //Collapse Swap
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

            //Collapse Toggle
            {
                labelGenerator("Toggle all object's collapse state");

                bool coggle = false;

                buttonGenerator("collapse toggle", "Toggle Collapse", parent, delegate ()
                {
                    coggle = !coggle;
                    foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                    {
                        timelineObject.Locked = coggle;

                        ObjectEditor.inst.RenderTimelineObject(timelineObject);
                    }
                });
            }

            //Sync object selection
            {
                labelGenerator("Sync to specific object");

                var syncLayout = new GameObject("sync layout");
                syncLayout.transform.SetParent(parent);
                syncLayout.transform.localScale = Vector3.one;

                var multiSyncRT = syncLayout.AddComponent<RectTransform>();
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
                                for (int i = 0; i < timelineObject.GetData<BeatmapObject>().events.Count; i++)
                                    timelineObject.GetData<BeatmapObject>().events[i] = beatmapObject.events[i].Clone();

                                Updater.UpdateProcessor(timelineObject.GetData<BeatmapObject>(), "Keyframes");
                            }
                        });
                    });
                }
            }

            EditorManager.inst.GetDialog("Multi Object Editor").Dialog.Find("data").GetComponent<RectTransform>().sizeDelta = new Vector2(810f, 730.11f);
            EditorManager.inst.GetDialog("Multi Object Editor").Dialog.Find("data/left").GetComponent<RectTransform>().sizeDelta = new Vector2(355f, 730f);
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

        public static bool LevelLoadsSavedTime => true;
        public static bool LevelPausesOnStart => true;

        public static IEnumerator GetFileList(string path, string fileType = null, Action<List<FileManager.LSFile>> files = null)
        {
            files?.Invoke(FileManager.inst.GetFileList(path, fileType));
            yield break;
        }

        public IEnumerator LoadLevels()
        {
            EditorManager.inst.loadedLevels.Clear();
            var folderList = FileManager.inst.GetFolderList(editorListPath);
            var list = new List<Coroutine>();
            foreach (var folder in folderList)
            {
                var metadataStr = FileManager.inst.LoadJSONFileRaw(folder.fullPath + "/metadata.lsb");
                if (metadataStr != null)
                {
                    list.Add(EditorManager.inst.StartCoroutine(EditorManager.inst.GetAlbumSprite(folder.name, delegate (Sprite cover)
                    {
                        EditorManager.inst.loadedLevels.Add(new MetadataWrapper(DataManager.inst.ParseMetadata(metadataStr, false), folder.name, (cover != null) ? cover : SteamWorkshop.inst.defaultSteamImageSprite));
                    }, delegate
                    {
                        EditorManager.inst.loadedLevels.Add(new MetadataWrapper(DataManager.inst.ParseMetadata(metadataStr, false), folder.name, SteamWorkshop.inst.defaultSteamImageSprite));
                    })));
                }
                else
                    Debug.LogError($"{EditorManager.inst.className}Could not load metadata for [{folder.name}]!");
            }

            if (list.Count >= 1)
                yield return EditorManager.inst.StartCoroutine(LSHelpers.WaitForMultipleCoroutines(list, delegate
                {
                    EditorManager.inst.FinishedLoadingLevelsFunc();
                }));
            else
                EditorManager.inst.FinishedLoadingLevelsFunc();

            yield break;
        }

        public IEnumerator LoadLevel(EditorManager __instance, string _levelName) => LoadLevel(_levelName);

        public IEnumerator LoadLevel(string _levelName)
        {
            var __instance = EditorManager.inst;
            var objectManager = ObjectManager.inst;
            var objEditor = ObjEditor.inst;
            var gameManager = GameManager.inst;
            var dataManager = DataManager.inst;

            __instance.loading = true;

            string code = $"{_levelName}/EditorLoad.cs";
            if (RTFile.FileExists(code))
            {
                yield return StartCoroutine(RTCode.IEvaluate(RTFile.ReadFromFile(code)));
            }

            layerType = LayerType.Objects;
            SetLayer(0);

            Updater.UpdateObjects(false);

            __instance.InvokeRepeating("LoadingIconUpdate", 0f, 0.05f);

            var withoutFullPath = _levelName.Replace(RTFile.ApplicationDirectory, "");
            var withoutList = withoutFullPath.Replace(editorListSlash, "");

            __instance.currentLoadedLevel = withoutList;
            __instance.SetPitch(1f);
            
            __instance.timelineScrollbar.GetComponent<Scrollbar>().value = 0f;
            gameManager.gameState = GameManager.State.Loading;
            string rawJSON = null;
            string rawMetadataJSON = null;
            AudioClip song = null;

            __instance.ClearDialogs(new EditorManager.EditorDialog.DialogType[1]);
            __instance.ShowDialog("File Info Popup");

            var fileInfo = __instance.GetDialog("File Info Popup").Dialog.transform.Find("text").GetComponent<Text>();

            fileInfo.text = "Loading Level Data for [" + withoutList + "]";

            Debug.LogFormat("{0}Loading {1}...", EditorPlugin.className, _levelName);
            rawJSON = FileManager.inst.LoadJSONFileRaw(_levelName + "/level.lsb");
            rawMetadataJSON = FileManager.inst.LoadJSONFileRaw(_levelName + "/metadata.lsb");

            if (string.IsNullOrEmpty(rawMetadataJSON))
            {
                dataManager.SaveMetadata(_levelName + "/metadata.lsb");
            }

            gameManager.path = _levelName + "/level.lsb";
            gameManager.basePath = _levelName + "/";
            gameManager.levelName = withoutList;
            fileInfo.text = "Loading Level Music for [" + withoutList + "]\n\nIf this is taking more than a minute or two check if the .ogg file is corrupt.";

            Debug.LogFormat("{0}Loading audio for {1}...", EditorPlugin.className, _levelName);
            if (RTFile.FileExists(_levelName + "/level.ogg"))
            {
                yield return StartCoroutine(FileManager.inst.LoadMusicFile(withoutFullPath + "/level.ogg", delegate (AudioClip _song)
                {
                    _song.name = withoutList;
                    if (_song)
                    {
                        song = _song;
                    }
                }));
            }
            else if (RTFile.FileExists(_levelName + "/level.wav"))
            {
                yield return StartCoroutine(FileManager.inst.LoadMusicFile(withoutFullPath + "/level.wav", delegate (AudioClip _song)
                {
                    _song.name = withoutList;
                    if (_song)
                    {
                        song = _song;
                    }
                }));
            }

            Debug.LogFormat("{0}Parsing level data for {1}...", EditorPlugin.className, _levelName);
            gameManager.gameState = GameManager.State.Parsing;
            fileInfo.text = "Parsing Level Data for [" + withoutList + "]";
            if (!string.IsNullOrEmpty(rawJSON) && !string.IsNullOrEmpty(rawMetadataJSON))
            {
                dataManager.ParseMetadata(rawMetadataJSON);
                if (DataManager.inst.metaData.beatmap.game_version != "4.1.16")
                    rawJSON = dataManager.gameData.UpdateBeatmap(rawJSON, DataManager.inst.metaData.beatmap.game_version);
                //dataManager.gameData.eventObjects = new DataManager.GameData.EventObjects();
                //StartCoroutine(Parser.ParseBeatmap(rawJSON, true));

                dataManager.gameData = GameData.Parse(JSON.Parse(rawJSON));

                if (dataManager.metaData.beatmap.workshop_id == -1)
                    dataManager.metaData.beatmap.workshop_id = UnityEngine.Random.Range(0, int.MaxValue);
            }

            if (GameObject.Find("BepInEx_Manager").GetComponentByName("PlayerPlugin"))
            {
                var playerPlugin = GameObject.Find("BepInEx_Manager").GetComponentByName("PlayerPlugin");
                var c = playerPlugin.GetType().GetField("className").GetValue(playerPlugin);

                if (c != null)
                {
                    playerPlugin.GetType().GetMethod("LoadIndexes").Invoke(playerPlugin, new object[] { });
                    playerPlugin.GetType().GetMethod("StartRespawnPlayers").Invoke(playerPlugin, new object[] { });
                }
            }

            fileInfo.text = "Loading Themes for [" + withoutList + "]";
            Debug.LogFormat("{0}Loading themes for {1}...", EditorPlugin.className, _levelName);
            yield return inst.StartCoroutine(LoadThemes());
            float delayTheme = 0f;
            while (themesLoading)
            {
                yield return new WaitForSeconds(delayTheme);
                delayTheme += 0.0001f;
            }

            Debug.LogFormat("{0}Music is null: ", EditorPlugin.className, song == null);

            fileInfo.text = "Playing Music for [" + withoutList + "]\n\nIf it doesn't, then something went wrong!";
            AudioManager.inst.PlayMusic(null, song, true, 0f, true);
            inst.StartCoroutine((IEnumerator)AccessTools.Method(typeof(EditorManager), "SpawnPlayersWithDelay").Invoke(EditorManager.inst, new object[] { 0.2f }));
            if (GenerateWaveform)
            {
                fileInfo.text = "Assigning Waveform Textures for [" + withoutList + "]";
                Debug.LogFormat("{0}Assigning timeline textures for {1}...", EditorPlugin.className, _levelName);
                var image = EditorManager.inst.timeline.GetComponent<Image>();
                yield return AssignTimelineTexture();
                float delay = 0f;
                while (image.sprite == null)
                {
                    yield return new WaitForSeconds(delay);
                    delay += 0.0001f;
                }
            }
            else
            {
                fileInfo.text = "Skipping Waveform Textures for [" + withoutList + "]";
                Debug.LogFormat("{0}Skipping Waveform Textures for {1}...", EditorPlugin.className, _levelName);
                EditorManager.inst.timeline.GetComponent<Image>().sprite = null;
                EditorManager.inst.timelineWaveformOverlay.GetComponent<Image>().sprite = null;
            }

            fileInfo.text = "Updating Timeline for [" + withoutList + "]";
            Debug.LogFormat("{0}Updating editor for {1}...", EditorPlugin.className, _levelName);
            AccessTools.Method(typeof(EditorManager), "UpdateTimelineSizes").Invoke(EditorManager.inst, new object[] { });
            gameManager.UpdateTimeline();
            __instance.ClearDialogs(Array.Empty<EditorManager.EditorDialog.DialogType>());
            MetadataEditor.inst.Render();
            if (layerType == LayerType.Events)
                CheckpointEditor.inst.CreateCheckpoints();
            else
                CheckpointEditor.inst.CreateGhostCheckpoints();

            fileInfo.text = "Updating states for [" + withoutList + "]";
            DiscordController.inst.OnStateChange("Editing: " + DataManager.inst.metaData.song.title);
            objectManager.updateObjects();
            EventEditor.inst.CreateEventObjects();
            BackgroundManager.inst.UpdateBackgrounds();
            gameManager.UpdateTheme();
            MarkerEditor.inst.CreateMarkers();
            EventManager.inst.updateEvents();

            fileInfo.text = "Setting first object of [" + withoutList + "]";
            ObjectEditor.inst.CreateTimelineObjects();
            ObjectEditor.inst.RenderTimelineObjects();
            ObjectEditor.inst.SetCurrentObject(timelineObjects[0]);
            //if (timelineObjects[0].IsBeatmapObject)
            //    ObjectEditor.inst.OpenDialog(timelineObjects[0].GetData<BeatmapObject>());

            CheckpointEditor.inst.SetCurrentCheckpoint(0);

            fileInfo.text = "Done!";
            __instance.HideDialog("File Info Popup");
            __instance.CancelInvoke("LoadingIconUpdate");

            gameManager.ResetCheckpoints(true);
            gameManager.gameState = GameManager.State.Playing;

            __instance.DisplayNotification(withoutList + " Level Loaded", 2f, EditorManager.NotificationType.Success, false);
            __instance.UpdatePlayButton();
            __instance.hasLoadedLevel = true;

            if (!RTFile.DirectoryExists(GameManager.inst.basePath + "autosaves"))
                Directory.CreateDirectory(GameManager.inst.basePath + "autosaves");

            string[] files = Directory.GetFiles(GameManager.inst.basePath + "autosaves", "autosave_*.lsb", SearchOption.TopDirectoryOnly);
            files.ToList().Sort();

            __instance.autosaves.Clear();

            foreach (var file in files)
            {
                __instance.autosaves.Add(file);
            }

            SetAutosave();


            TriggerHelper.AddEventTrigger(timeIF.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(timeIF, max: AudioManager.inst.CurrentAudioSource.clip.length) });

            {
                if (LevelLoadsSavedTime)
                {
                    AudioManager.inst.CurrentAudioSource.time = DataManager.inst.gameData.beatmapData.editorData.timelinePos;
                }
                if (LevelPausesOnStart)
                {
                    AudioManager.inst.CurrentAudioSource.Pause();
                    __instance.UpdatePlayButton();
                }

                //if (RTFile.FileExists(RTFile.ApplicationDirectory + text + "/editor.lse"))
                //{
                //    string rawProfileJSON = FileManager.inst.LoadJSONFile(text + "/editor.lse");

                //    var jn = JSON.Parse(rawProfileJSON);

                //    if (jn["timeline"]["z"] != null)
                //        EditorManager.inst.zoomSlider.value = jn["timeline"]["z"].AsFloat;

                //    if (jn["timeline"]["tsc"] != null)
                //        EditorManager.inst.timelineScrollRectBar.value = jn["timeline"]["tsc"].AsFloat;

                //    if (jn["timeline"]["l"] != null)
                //        SetLayer(jn["timeline"]["l"].AsInt);

                //    if (jn["timeline"]["lt"] != null)
                //        SetLayer((LayerType)jn["timeline"]["lt"].AsInt);

                //    if (jn["editor"]["t"] != null)
                //        EditorPlugin.timeEdit = jn["editor"]["t"].AsFloat;

                //    if (jn["editor"]["a"] != null)
                //        EditorPlugin.openAmount = jn["editor"]["a"].AsInt;

                //    EditorPlugin.openAmount += 1;

                //    if (jn["misc"]["sn"] != null)
                //        SettingEditor.inst.SnapActive = jn["misc"]["sn"].AsBool;

                //    SettingEditor.inst.SnapBPM = DataManager.inst.metaData.song.BPM;
                //}
                //else
                //{
                //    EditorPlugin.timeEdit = 0f;
                //}
            }

            if (ModCompatibility.sharedFunctions.ContainsKey("EditorOnLoadLevel"))
                ((Action)ModCompatibility.sharedFunctions["EditorOnLoadLevel"])();

            __instance.loading = false;

            yield break;
        }

        public IEnumerator LoadThemes(bool refreshGUI = false)
        {
            themesLoading = true;
            DataManager.inst.CustomBeatmapThemes.Clear();
            DataManager.inst.BeatmapThemeIDToIndex.Clear();
            DataManager.inst.BeatmapThemeIndexToID.Clear();
            ((GameData)DataManager.inst.gameData).beatmapThemes.Clear();

            int num = 0;
            foreach (var beatmapTheme in DataManager.inst.BeatmapThemes)
            {
                DataManager.inst.BeatmapThemeIDToIndex.Add(num, num);
                DataManager.inst.BeatmapThemeIndexToID.Add(num, num);
                num++;
            }

            yield return inst.StartCoroutine(GetFileList(themeListPath, "lst", delegate (List<FileManager.LSFile> folders)
            {
                folders = (from x in folders
                           orderby x.Name.ToLower()
                           select x).ToList();

                foreach (var folder in folders)
                {
                    var lsfile = folder;
                    var jn = JSON.Parse(FileManager.inst.LoadJSONFileRaw(lsfile.FullPath));
                    var orig = BeatmapTheme.Parse(jn);
                    DataManager.inst.CustomBeatmapThemes.Add(orig);

                    if (jn["id"] != null && !((GameData)DataManager.inst.gameData).beatmapThemes.ContainsKey(jn["id"]))
                        ((GameData)DataManager.inst.gameData).beatmapThemes.Add(jn["id"], orig);

                    if (DataManager.inst.BeatmapThemeIDToIndex.ContainsKey(int.Parse(orig.id)))
                    {
                        var list = DataManager.inst.CustomBeatmapThemes.Where(x => x.id == orig.id).ToList();
                        var str = "";
                        for (int i = 0; i < list.Count; i++)
                        {
                            str += list[i].name;
                            if (i != list.Count - 1)
                                str += ", ";
                        }

                        if (EditorManager.inst != null)
                        {
                            EditorManager.inst.DisplayNotification($"Unable to Load theme [{orig.name}] due to conflicting themes: {str}", 2f * list.Count, EditorManager.NotificationType.Error);
                        }
                    }
                    else
                    {
                        DataManager.inst.BeatmapThemeIndexToID.Add(DataManager.inst.AllThemes.Count - 1, int.Parse(orig.id));
                        DataManager.inst.BeatmapThemeIDToIndex.Add(int.Parse(orig.id), DataManager.inst.AllThemes.Count - 1);
                    }

                    if (jn["id"] == null)
                    {
                        var beatmapTheme = BeatmapTheme.DeepCopy(orig);
                        beatmapTheme.id = LSText.randomNumString(6);
                        DataManager.inst.CustomBeatmapThemes.Remove(orig);
                        FileManager.inst.DeleteFileRaw(lsfile.FullPath);
                        ThemeEditor.inst.SaveTheme(beatmapTheme);
                        DataManager.inst.CustomBeatmapThemes.Add(beatmapTheme);
                    }
                }
                themesLoading = false;

            }));

            if (refreshGUI)
            {
                var dialogTmp = EventEditor.inst.dialogRight.GetChild(4);
                var themeSearch = dialogTmp.Find("theme-search").GetComponent<InputField>();
                yield return StartCoroutine(ThemeEditorManager.inst.RenderThemeList(dialogTmp, themeSearch.text));
            }

            yield break;
        }

        public IEnumerator LoadPrefabs(PrefabEditor __instance)
        {
            yield return inst.StartCoroutine(GetFileList(prefabListPath, "lsp", delegate (List<FileManager.LSFile> folders)
            {
                foreach (var lsFile in folders)
                {
                    var jn = JSON.Parse(FileManager.inst.LoadJSONFileRaw(lsFile.FullPath));

                    __instance.LoadedPrefabs.Add(Prefab.Parse(jn));
                    __instance.LoadedPrefabsFiles.Add(lsFile.FullPath);
                }
            }));
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

        // Implement new ToJSON
        public void SetAutosave()
        {
            EditorManager.inst.CancelInvoke("AutoSaveLevel");
            CancelInvoke("AutoSaveLevel");
            timeSinceAutosaved = Time.time;
            InvokeRepeating("AutoSaveLevel", AutoSaveLoopTime, AutoSaveLoopTime);
        }

        public void AutoSaveLevel()
        {
            if (timeSinceAutosaved - Time.time < 0.5f || EditorManager.inst.loading)
                return;

            if (!EditorManager.inst.hasLoadedLevel)
            {
                EditorManager.inst.DisplayNotification("Beatmap can't autosave until you load a level.", 3f, EditorManager.NotificationType.Error, false);
                return;
            }

            if (EditorManager.inst.savingBeatmap)
            {
                EditorManager.inst.DisplayNotification("Already attempting to save the beatmap!", 2f, EditorManager.NotificationType.Error, false);
                return;
            }

            string autosavePath = $"{GameManager.inst.basePath}autosaves/autosave_{DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss")}.lsb";

            //string autosavePath = string.Concat(new string[]
            //{
            //	FileManager.GetAppPath(),
            //	"/",
            //	GameManager.inst.basePath,
            //	"autosaves/autosave_",
            //	DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss"),
            //	".lsb"
            //});

            if (!RTFile.DirectoryExists(GameManager.inst.basePath + "autosaves"))
                Directory.CreateDirectory(GameManager.inst.basePath + "autosaves");

            EditorManager.inst.DisplayNotification("Autosaving backup!", 2f, EditorManager.NotificationType.Warning, false);

            EditorManager.inst.autosaves.Add(autosavePath);

            while (EditorManager.inst.autosaves.Count > AutoSaveLimit)
            {
                var first = EditorManager.inst.autosaves[0];
                if (RTFile.FileExists(first))
                    File.Delete(first);

                EditorManager.inst.autosaves.RemoveAt(0);
            }

            EditorManager.inst.StartCoroutine(RTFunctions.Functions.ProjectData.Writer.SaveData(autosavePath, (GameData)DataManager.inst.gameData));

            autoSaving = true;
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
            var metaData = new Metadata();
            metaData.beatmap.game_version = "4.1.16";
            metaData.song.title = __instance.newLevelName;
            metaData.creator.steam_name = SteamWrapper.inst.user.displayName;
            metaData.creator.steam_id = SteamWrapper.inst.user.id;
            metaData.beatmap.workshop_id = UnityEngine.Random.Range(0, int.MaxValue);
            metaData.id = LSText.randomNumString(16);

            dataManager.metaData = metaData;

            dataManager.SaveMetadata(RTFile.ApplicationDirectory + editorListSlash + __instance.newLevelName + "/metadata.lsb");
            StartCoroutine(LoadLevel(__instance, RTFile.ApplicationDirectory + editorListSlash + __instance.newLevelName));
            __instance.HideDialog("New File Popup");
        }

        public GameData CreateBaseBeatmap()
        {
            Debug.Log($"{EditorPlugin.className}Creating new GameData...");
            var gameData = new GameData();
            gameData.beatmapData = new LevelBeatmapData();
            gameData.beatmapData.levelData = new DataManager.GameData.BeatmapData.LevelData();
            gameData.beatmapData.checkpoints.Add(new DataManager.GameData.BeatmapData.Checkpoint(false, "Base Checkpoint", 0f, Vector2.zero));
            var editorData = new LevelEditorData();
            gameData.beatmapData.editorData = editorData;

            Debug.Log($"{EditorPlugin.className}Cloning Default Keyframes...");
            var list = GameData.DefaultKeyframes;

            if (!ModCompatibility.mods.ContainsKey("EventsCore"))
            {
                while (list.Count > 10)
                {
                    list.RemoveAt(list.Count - 1);
                }
            }

            Debug.Log($"{EditorPlugin.className}Clearing current list");
            if (gameData.eventObjects.allEvents == null)
                gameData.eventObjects.allEvents = new List<List<BaseEventKeyframe>>();
            gameData.eventObjects.allEvents.Clear();
            for (int i = 0; i < list.Count; i++)
            {
                gameData.eventObjects.allEvents.Add(new List<BaseEventKeyframe>());
                var kf = EventKeyframe.DeepCopy((EventKeyframe)list[i]);
                gameData.eventObjects.allEvents[i].Add(kf);
            }

            Debug.Log($"{EditorPlugin.className}Creating BackgroundObjects");
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
                backgroundObject.reactive = (UnityEngine.Random.value > 0.5f);
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

                backgroundObject.shape = ShapeManager.Shapes3D[UnityEngine.Random.Range(0, 23)];

                gameData.backgroundObjects.Add(backgroundObject);
            }

            Debug.Log($"{EditorPlugin.className}Creating Default Object\ndefault object cameo :D");
            var beatmapObject = ObjectEditor.CreateNewBeatmapObject(0.5f, false);
            var objectEvents = beatmapObject.events[0];
            float time = 4f;
            float[] array2 = new float[3];
            array2[0] = 10f;
            objectEvents.Add(new EventKeyframe(time, array2, new float[2], 0));
            beatmapObject.name = "\"Default object cameo\" -Viral Mecha";
            beatmapObject.autoKillType = AutoKillType.LastKeyframeOffset;
            beatmapObject.autoKillOffset = 4f;
            beatmapObject.editorData.Layer = 0;
            gameData.beatmapObjects.Add(beatmapObject);

            return gameData;
        }

        #endregion

        #region Refresh Popups / Dialogs

        public void RefreshObjectSearch(Action<BeatmapObject> onSelect, bool clearParent = false)
        {
            var content = EditorManager.inst.GetDialog("Object Search Popup").Dialog.Find("mask/content");

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

            LSHelpers.DeleteChildren(content);

            var list = DataManager.inst.gameData.beatmapObjects;
            foreach (var beatmapObject in list)
            {
                var regex = new Regex(@"\[([0-9])\]");
                var match = regex.Match(objectSearchTerm);

                if (!beatmapObject.fromPrefab && string.IsNullOrEmpty(objectSearchTerm) || beatmapObject.name.ToLower().Contains(objectSearchTerm.ToLower()) || match.Success && int.Parse(match.Groups[1].ToString()) < DataManager.inst.gameData.beatmapObjects.Count && DataManager.inst.gameData.beatmapObjects.IndexOf(beatmapObject) == int.Parse(match.Groups[1].ToString()))
                {
                    string nm = $"[{(list.IndexOf(beatmapObject) + 1).ToString("0000")}/{list.Count.ToString("0000")} - {beatmapObject.id}] : {beatmapObject.name}";
                    var buttonPrefab = EditorManager.inst.spriteFolderButtonPrefab.Duplicate(content, nm);
                    buttonPrefab.transform.GetChild(0).GetComponent<Text>().text = nm;

                    var b = buttonPrefab.GetComponent<Button>();
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
                            "<br>ED: {L: " + beatmapObject.editorData.Layer + ", B: " + beatmapObject.editorData.Bin + "}" +
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

            replText.text = RTCode.ConvertREPLTest(replEditor.textComponent.text);
            replEditor.onValueChanged.AddListener(delegate (string _val)
            {
                replText.text = RTCode.ConvertREPLTest(replEditor.textComponent.text);
            });

            replEditor.onEndEdit.RemoveAllListeners();
            replEditor.onEndEdit.AddListener(onEndEdit);
        }


        public static List<LevelFolder<MetadataWrapper>> levelItems = new List<LevelFolder<MetadataWrapper>>();

        public void RefreshLevelList()
        {
            levelItems.Clear();

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

            #region Sorting

            var ien = EditorManager.inst.loadedLevels.AsEnumerable();
            if (!EditorPlugin.levelAscend)
            {
                switch (EditorPlugin.levelFilter)
                {
                    case 0:
                        {
                            ien = ien.OrderByDescending(x => x.albumArt != EditorManager.inst.AlbumArt);
                            break;
                        }
                    case 1:
                        {
                            ien = ien.OrderByDescending(x => x.metadata.artist.Name);
                            break;
                        }
                    case 2:
                        {
                            ien = ien.OrderByDescending(x => x.metadata.creator.steam_name);
                            break;
                        }
                    case 3:
                        {
                            ien = ien.OrderByDescending(x => x.folder);
                            break;
                        }
                    case 4:
                        {
                            ien = ien.OrderByDescending(x => x.metadata.song.title);
                            break;
                        }
                    case 5:
                        {
                            ien = ien.OrderByDescending(x => x.metadata.song.difficulty);
                            break;
                        }
                    case 6:
                        {
                            ien = ien.OrderByDescending(x => x.metadata.beatmap.date_edited);
                            break;
                        }
                    case 7:
                        {
                            ien = ien.OrderByDescending(x => ((Metadata)x.metadata).LevelBeatmap.date_created);
                            break;
                        }
                }
            }
            else
            {
                switch (EditorPlugin.levelFilter)
                {
                    case 0:
                        {
                            ien = ien.OrderBy(x => x.albumArt != EditorManager.inst.AlbumArt);
                            break;
                        }
                    case 1:
                        {
                            ien = ien.OrderBy(x => x.metadata.artist.Name);
                            break;
                        }
                    case 2:
                        {
                            ien = ien.OrderBy(x => x.metadata.creator.steam_name);
                            break;
                        }
                    case 3:
                        {
                            ien = ien.OrderBy(x => x.folder);
                            break;
                        }
                    case 4:
                        {
                            ien = ien.OrderBy(x => x.metadata.song.title);
                            break;
                        }
                    case 5:
                        {
                            ien = ien.OrderBy(x => x.metadata.song.difficulty);
                            break;
                        }
                    case 6:
                        {
                            ien = ien.OrderBy(x => x.metadata.beatmap.date_edited);
                            break;
                        }
                    case 7:
                        {
                            ien = ien.OrderBy(x => ((Metadata)x.metadata).LevelBeatmap.date_created);
                            break;
                        }
                }
            }

            EditorManager.inst.loadedLevels = ien.ToList();

            #endregion

            var transform = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("mask").Find("content");
            var close = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("Panel/x");

            LSHelpers.DeleteChildren(transform);

            foreach (var metadataWrapper in EditorManager.inst.loadedLevels)
            {
                var metadata = metadataWrapper.metadata;
                string name = metadataWrapper.folder;

                if (metadata == null)
                    continue;

                string difficultyName = "None";

                string[] difficultyNames = new string[]
                {
                    "easy",
                    "normal",
                    "hard",
                    "expert",
                    "expert+",
                    "master",
                    "animation",
                };

                difficultyName = difficultyNames[metadata.song.difficulty];

                if (RTFile.FileExists(RTFile.ApplicationDirectory + editorListSlash + metadataWrapper.folder + "/level.ogg") ||
                    RTFile.FileExists(RTFile.ApplicationDirectory + editorListSlash + metadataWrapper.folder + "/level.wav"))
                {
                    if (RTHelpers.SearchString(name, EditorManager.inst.openFileSearch) ||
                        RTHelpers.SearchString(metadata.song.title, EditorManager.inst.openFileSearch) ||
                        RTHelpers.SearchString(metadata.artist.Name, EditorManager.inst.openFileSearch) ||
                        RTHelpers.SearchString(metadata.creator.steam_name, EditorManager.inst.openFileSearch) ||
                        RTHelpers.SearchString(metadata.song.description, EditorManager.inst.openFileSearch) ||
                        RTHelpers.SearchString(difficultyName, EditorManager.inst.openFileSearch))
                    {
                        var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(transform, $"Folder [{metadataWrapper.folder}]");

                        var hoverUI = gameObject.AddComponent<HoverUI>();
                        hoverUI.size = GetEditorProperty("Open Level Button Hover Size").GetConfigEntry<float>().Value;
                        hoverUI.animatePos = false;
                        hoverUI.animateSca = true;
                        var htt = gameObject.AddComponent<HoverTooltip>();

                        var levelTip = new HoverTooltip.Tooltip();

                        var text = gameObject.transform.GetChild(0).GetComponent<Text>();

                        text.text = string.Format(GetEditorProperty("Open Level Text Formatting").GetConfigEntry<string>().Value,
                            LSText.ClampString(metadataWrapper.folder, foldClamp),
                            LSText.ClampString(metadata.song.title, songClamp),
                            LSText.ClampString(metadata.artist.Name, artiClamp),
                            LSText.ClampString(metadata.creator.steam_name, creaClamp),
                            metadata.song.difficulty,
                            LSText.ClampString(metadata.song.description, descClamp),
                            LSText.ClampString(metadata.beatmap.date_edited, dateClamp));

                        text.horizontalOverflow = GetEditorProperty("Open Level Text Horizontal Wrap").GetConfigEntry<HorizontalWrapMode>().Value;
                        text.verticalOverflow = GetEditorProperty("Open Level Text Vertical Wrap").GetConfigEntry<VerticalWrapMode>().Value;
                        //text.color = ConfigEntries.OpenFileTextColor.Value;
                        text.fontSize = GetEditorProperty("Open Level Text Font Size").GetConfigEntry<int>().Value;

                        var difficultyColor = metadata.song.difficulty >= 0 && metadata.song.difficulty < DataManager.inst.difficulties.Count ?
                            DataManager.inst.difficulties[metadata.song.difficulty].color : LSColors.themeColors["none"].color;

                        levelTip.desc = "<#" + LSColors.ColorToHex(difficultyColor) + ">" + metadata.artist.Name + " - " + metadata.song.title;
                        levelTip.hint = "</color>" + metadata.song.description;
                        htt.tooltipLangauges.Add(levelTip);

                        gameObject.GetComponent<Button>().onClick.AddListener(delegate ()
                        {
                            inst.StartCoroutine(EditorManager.inst.LoadLevel(name));
                            EditorManager.inst.HideDialog("Open File Popup");

                            //if (RTEditor.CompareLastSaved())
                            //{
                            //	EditorManager.inst.ShowDialog("Warning Popup");
                            //	RTEditor.RefreshWarningPopup("You haven't saved! Are you sure you want to exit the level before saving?", delegate ()
                            //	{
                            //		RTEditor.inst.StartCoroutine(RTEditor.LoadLevel(EditorManager.inst, name));
                            //		EditorManager.inst.HideDialog("Open File Popup");
                            //		EditorManager.inst.HideDialog("Warning Popup");
                            //	}, delegate ()
                            //	{
                            //		EditorManager.inst.HideDialog("Warning Popup");
                            //	});
                            //}
                            //else
                            //{
                            //	RTEditor.inst.StartCoroutine(RTEditor.LoadLevel(EditorManager.inst, name));
                            //	EditorManager.inst.HideDialog("Open File Popup");
                            //}
                        });

                        var icon = new GameObject("icon");
                        icon.transform.SetParent(gameObject.transform);
                        icon.transform.localScale = Vector3.one;
                        icon.layer = 5;
                        var iconRT = icon.AddComponent<RectTransform>();
                        icon.AddComponent<CanvasRenderer>();
                        var iconImage = icon.AddComponent<Image>();

                        iconRT.anchoredPosition = GetEditorProperty("Open Level Cover Position").GetConfigEntry<Vector2>().Value;
                        iconRT.sizeDelta = GetEditorProperty("Open Level Cover Scale").GetConfigEntry<Vector2>().Value;

                        iconImage.sprite = metadataWrapper.albumArt;

                        //Close
                        if (GetEditorProperty("Open Level Show Delete Button").GetConfigEntry<bool>().Value)
                        {
                            var delete = close.gameObject.Duplicate(gameObject.transform, "delete");

                            delete.GetComponent<RectTransform>().anchoredPosition = new Vector2(-5f, 0f);

                            string levelName = metadataWrapper.folder;

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

                        levelItems.Add(new LevelFolder<MetadataWrapper>(metadataWrapper, gameObject, gameObject.GetComponent<RectTransform>(), iconImage));
                    }
                }
            }

            if (ModCompatibility.sharedFunctions.ContainsKey("EditorLevelFolders"))
                ModCompatibility.sharedFunctions["EditorLevelFolders"] = levelItems;
            else ModCompatibility.sharedFunctions.Add("EditorLevelFolders", levelItems);
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

            if (string.IsNullOrEmpty(__instance.parentSearch) || "player".Contains(__instance.parentSearch.ToLower()))
            {
                var cam = __instance.folderButtonPrefab.Duplicate(transform, "Player");
                cam.transform.localScale = Vector3.one;
                cam.transform.GetChild(0).GetComponent<Text>().text = "Nearest Player";
                cam.GetComponent<Button>().onClick.AddListener(delegate ()
                {
                    beatmapObject.parent = "PLAYER_PARENT";
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
                Config.Bind("General", "BPM Snaps Keyframes", false, "Makes object's keyframes snap if Snap BPM is enabled.")),
            new EditorProperty(EditorProperty.ValueType.Float,
                Config.Bind("General", "BPM Snap Divisions", 4f, "How many times the snap is divided into. Can be good for songs that don't do 4 divisions.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("General", "Round To Nearest", true, "If numbers should be rounded up to 3 decimal points (for example, 0.43321245 into 0.433).")),
            new EditorProperty(EditorProperty.ValueType.Enum,
                Config.Bind("General", "Preferences Open Key", KeyCode.F10, "The key to press to open the Editor Properties / Preferences window.")),
            new EditorProperty(EditorProperty.ValueType.Enum,
                Config.Bind("General", "Player Editor Open Key", KeyCode.F6, "The key to press to open the Player Editor window.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("General", "Prefab Example Template", true, "Example Template prefab will always be generated into the internal prefabs for you to use.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("General", "Paste Offset", false, "When enabled objects that are pasted will be pasted at an offset based on the distance between the audio time and the copied object. Otherwise, the objects will be pasted at the earliest objects start time.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("General", "Bring To Selection", false, "When an object is selected (whether it be a regular object, a marker, etc), it will move the layer and audio time to that object.")),

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
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Timeline", "Waveform Generate", true, "Allows the timeline waveform to generate. (Waveform might not show on some devices and will increase level load times)")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Timeline", "Waveform Re-render", false, "If the timeline waveform should update when a value is changed.")),
            new EditorProperty(EditorProperty.ValueType.Enum,
                Config.Bind("Timeline", "Waveform Mode", WaveformType.Legacy, "The mode of the timeline waveform.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Timeline", "Waveform BG Color", Color.clear, "Color of the background for the waveform.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Timeline", "Waveform BG Color", Color.clear, "Color of the background for the waveform.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Timeline", "Waveform Top Color", LSColors.red300, "If waveform mode is Legacy, this will be the top color. Otherwise, it will be the regular color.")),
            new EditorProperty(EditorProperty.ValueType.Color,
                Config.Bind("Timeline", "Waveform Bottom Color", LSColors.blue300, "If waveform is Legacy, this will be the bottom color. Otherwise, it will be unused.")),
            new EditorProperty(EditorProperty.ValueType.Enum,
                Config.Bind("Timeline", "Waveform Texture Format", TextureFormat.ARGB32, "What format the waveform's texture should render under.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Timeline", "Marker Loop Active", false, "If the marker should loop between markers.")),
            new EditorProperty(EditorProperty.ValueType.Int,
                Config.Bind("Timeline", "Marker Loop Begin", 0, "Audio time gets set to this marker.")),
            new EditorProperty(EditorProperty.ValueType.Int,
                Config.Bind("Timeline", "Marker Loop End", 1, "If the audio time gets to the set marker time, it will loop to the beginning marker.")),

            #endregion

            #region Data

            new EditorProperty(EditorProperty.ValueType.Int,
                Config.Bind("Data", "Autosave Limit", 3, "If autosave count reaches this number, delete the first autosave.")),
            new EditorProperty(EditorProperty.ValueType.Float,
                Config.Bind("Data", "Autosave Loop Time", 600f, "The repeat time of autosave.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Data", "Level Loads Last Time", true, "Sets the editor position (audio time, layer, etc) to the last saved editor position on level load.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Data", "Level Pauses on Start", false, "Editor pauses on level load.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Data", "Saving Saves Beatmap Opacity", true, "Turn this off if you don't want themes to break in unmodded PA.")),

            #endregion

            #region Editor GUI

            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Editor GUI", "Drag UI", false, "Specific UI popups can be dragged around (such as the parent selector, etc).")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Editor GUI", "Hover UI Play Sound", false, "Plays a sound when the hover UI element is hovered over.")),
            new EditorProperty(EditorProperty.ValueType.Float,
                Config.Bind("Editor GUI", "Notification Width", 221f, "Width of the notifications.")),
            new EditorProperty(EditorProperty.ValueType.Float,
                Config.Bind("Editor GUI", "Notification Size", 1f, "Total size of the notifications.")),
            new EditorProperty(EditorProperty.ValueType.Enum,
                Config.Bind("Editor GUI", "Notification Direction", Direction.Down, "Direction the notifications popup from.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Editor GUI", "Notifications Display", true, "If the notifications should display. Does not include the help box.")),
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
            new EditorProperty(EditorProperty.ValueType.Enum,
                Config.Bind("Editor GUI", "Prefab Internal Type Font Size", 20, new ConfigDescription("Size of the text font.", FontSizeLimit))),

            // Prefab External
            new EditorProperty(EditorProperty.ValueType.Vector2,
                Config.Bind("Editor GUI", "Prefab External Popup Pos", new Vector2(0f, -16f), "Position of the external prefabs popup.")),
            new EditorProperty(EditorProperty.ValueType.Vector2,
                Config.Bind("Editor GUI", "Prefab External Popup Size", new Vector2(400f, -32f), "Scale of the external prefabs popup.")),
            new EditorProperty(EditorProperty.ValueType.Vector2,
                Config.Bind("Editor GUI", "Prefab External Popup Pos", new Vector2(-32f, -16f), "Position of the external prefabs popup.")),
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
            new EditorProperty(EditorProperty.ValueType.Enum,
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
                Config.Bind("Fields", "Theme Template GUI Accent", LSColors.white, "GUI Accent Color of the template theme.")),
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

            #endregion

            #region Functions
            
            new EditorProperty("Open Keybind Editor", EditorProperty.ValueType.Function, EditorProperty.EditorPropCategory.Functions,
                delegate () { EditorManager.inst.ShowDialog("Keybind List Popup"); }, ""),

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

            #endregion
        };

        #endregion

        #region Constructors

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

            public ConfigEntry<T> GetConfigEntry<T>()
            {
                if (configEntry is ConfigEntry<T>)
                    return (ConfigEntry<T>)configEntry;
                return null;
            }

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
