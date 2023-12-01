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
            CreateObjectSearch();
            CreateWarningPopup();
            CreateREPLEditor();

            // Player Editor
            {
                GameObject gameObject = new GameObject("PlayerEditorManager");
                gameObject.transform.SetParent(GameObject.Find("Editor Systems").transform);
                gameObject.AddComponent<CreativePlayersEditor>();
            }

            // Object Modifiers Editor
            {
                GameObject gameObject = new GameObject("ObjectModifiersEditor");
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
        }

        void Update()
        {
            foreach (var timelineObject in TimelineBeatmapObjects)
            {
                if (timelineObject.Data != null && timelineObject.GameObject && timelineObject.Image &&
                    timelineObject.Data.editorData.Layer == Layer && layerType == LayerType.Objects)
                {
                    timelineObject.GameObject.SetActive(true);

                    var color = ObjEditor.inst.NormalColor;

                    if (!string.IsNullOrEmpty(timelineObject.Data.prefabID))
                        color = timelineObject.Data.GetPrefabTypeColor();

                    if (timelineObject.selected)
                        timelineObject.Image.color = ObjEditor.inst.SelectedColor;
                    else
                        timelineObject.Image.color = color;
                }
                else if (timelineObject.GameObject)
                    timelineObject.GameObject.SetActive(false);
            }

            foreach (var timelineObject in TimelinePrefabObjects)
            {
                if (timelineObject.Data != null && timelineObject.GameObject && timelineObject.Image &&
                    timelineObject.Data.editorData.Layer == Layer && layerType == LayerType.Objects)
                {
                    timelineObject.GameObject.SetActive(true);
                    
                    if (timelineObject.selected)
                        timelineObject.Image.color = ObjEditor.inst.SelectedColor;
                    else
                        timelineObject.Image.color = timelineObject.Data.GetPrefabTypeColor();
                }
                else if (timelineObject.GameObject)
                    timelineObject.GameObject.SetActive(false);
            }

            foreach (var timelineObject in timelineBeatmapObjectKeyframes.Union(timelineKeyframes))
            {
                if (timelineObject.Data != null && timelineObject.GameObject && timelineObject.Image)
                {
                    timelineObject.GameObject.SetActive(true);

                    if (timelineObject.selected)
                        timelineObject.Image.color = ObjEditor.inst.SelectedColor;
                    else
                        timelineObject.Image.color = ObjEditor.inst.NormalColor;
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

        public static bool RoundToNearest => true;
        public static bool ShowModifiedColors => true;
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
                if (!layerToggle)
                    layerToggle = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject/6").GetComponent<Toggle>();
                return layerToggle;
            }
        }

        public GameObject timelineBar;

        public static InputField pitchIF;
        public static InputField timeIF;

        public static GameObject defaultIF;

        public string objectSearchTerm;

        public GameObject replBase;
        public InputField replEditor;
        public Text replText;

        public Transform titleBar;

        public Text fileInfoText;

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

        public static float SnapToBPM(float _time) => Mathf.RoundToInt(_time / (SettingEditor.inst.BPMMulti / BPMSnapDivisions)) * (SettingEditor.inst.BPMMulti / BPMSnapDivisions);

        #endregion

        #region Timeline Objects

        public Dictionary<string, TimelineObject<BeatmapObject>> timelineBeatmapObjects = new Dictionary<string, TimelineObject<BeatmapObject>>();
        public Dictionary<string, TimelineObject<PrefabObject>> timelinePrefabObjects = new Dictionary<string, TimelineObject<PrefabObject>>();
        public List<TimelineObject<EventKeyframe>> timelineKeyframes = new List<TimelineObject<EventKeyframe>>();
        public List<TimelineObject<EventKeyframe>> timelineBeatmapObjectKeyframes = new List<TimelineObject<EventKeyframe>>();
        
        public List<TimelineObject<BeatmapObject>> TimelineBeatmapObjects => timelineBeatmapObjects.Values.ToList();
        public List<TimelineObject<PrefabObject>> TimelinePrefabObjects => timelinePrefabObjects.Values.ToList();

        #endregion

        #region Timeline Textures

        public static bool GenerateWaveform => true;

        public static WaveformType WaveformMode => WaveformType.Legacy;
        public static Color WaveformBGColor => new Color(0f, 0f, 0f);
        public static Color WaveformTopColor => new Color(0f, 0f, 0f);
        public static Color WaveformBottomColor => new Color(0f, 0f, 0f);
        public static TextureFormat WaveformFormat => TextureFormat.ARGB32;

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
        static string editorPath;
        public static string editorListPath;
        public static string editorListSlash;

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
        static string themePath;
        public static string themeListPath;
        public static string themeListSlash;

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
        static string prefabPath;
        public static string prefabListPath;
        public static string prefabListSlash;

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
                        foreach (ObjectKeyframeSelection keyframeSelection in ObjEditor.inst.copiedObjectKeyframes.Keys)
                        {
                            ObjEditor.inst.DeleteKeyframe(keyframeSelection.Type, keyframeSelection.Index);
                        }
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
                float offsetTime;
                var offsetTimeBeatmap = ObjectEditor.inst.SelectedBeatmapObjects.Min(x => x.Time);
                var offsetTimePrefab = ObjectEditor.inst.SelectedPrefabObjects.Min(x => x.Time);

                if (offsetTimeBeatmap > offsetTimePrefab)
                    offsetTime = offsetTimePrefab;
                else
                    offsetTime = offsetTimeBeatmap;

                ObjEditor.inst.CopyObject();
                if (!_cut)
                {
                    EditorManager.inst.DisplayNotification("Copied Beatmap Object", 1f, EditorManager.NotificationType.Success, false);
                }
                else
                {
                    ObjEditor.inst.DeleteObject(ObjEditor.inst.currentObjectSelection, true);
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
            if (EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Object))
            {
                if (ObjEditor.inst.currentKeyframe != 0)
                {
                    inst.StartCoroutine(DeleteKeyframes());
                    EditorManager.inst.DisplayNotification("Deleted Beatmap Object Keyframe.", 1f, EditorManager.NotificationType.Success, false);
                }
                else
                {
                    EditorManager.inst.DisplayNotification("Can't Delete First Keyframe.", 1f, EditorManager.NotificationType.Error, false);
                }
                return;
            }
            if ((EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Timeline) && EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Object)) || EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Prefab))
            {
                if (DataManager.inst.gameData.beatmapObjects.Count > 1)
                {
                    if (ObjectEditor.inst.SelectedObjectCount > 1)
                    {
                        var list = new List<BeatmapObject>();
                        foreach (var timelineObject in ObjectEditor.inst.SelectedBeatmapObjects)
                            list.Add(timelineObject.Data);

                        var list2 = new List<PrefabObject>();
                        foreach (var timelineObject in ObjectEditor.inst.SelectedPrefabObjects)
                            list2.Add(timelineObject.Data);

                        EditorManager.inst.ClearDialogs(EditorManager.EditorDialog.DialogType.Object, EditorManager.EditorDialog.DialogType.Prefab);

                        float startTime = 0f;

                        List<float> startTimeList = new List<float>();
                        foreach (var bm in list)
                            startTimeList.Add(bm.StartTime);
                        foreach (var pr in list2)
                            startTimeList.Add(pr.StartTime);

                        startTimeList = (from x in startTimeList
                                         orderby x ascending
                                         select x).ToList();

                        startTime = startTimeList[0];

                        var prefab = new Prefab("deleted objects", 0, startTime, list, list2);

                        EditorManager.inst.history.Add(new History.Command("Delete Objects", delegate ()
                        {
                            Delete();
                        }, delegate ()
                        {
                            ObjectEditor.inst.DeselectAllObjects();
                            inst.StartCoroutine(ObjectEditor.inst.AddPrefabExpandedToLevel(prefab, true, 0f, true));
                        }));

                        inst.StartCoroutine(DeleteObjects());
                    }
                    else
                    {
                        Debug.LogFormat("{0}Deleting single object...", EditorPlugin.className);
                        float startTime = 0f;
                        if (ObjectEditor.inst.CurrentBeatmapObjectSelection)
                            startTime = ObjectEditor.inst.CurrentBeatmapObjectSelection.Data.StartTime;
                        else if (ObjectEditor.inst.CurrentPrefabObjectSelection)
                            startTime = ObjectEditor.inst.CurrentPrefabObjectSelection.Data.StartTime;

                        Debug.LogFormat("{0}Assigning prefab for undo...", EditorPlugin.className);
                        BasePrefab prefab = new BasePrefab("deleted object", 0, startTime, ObjEditor.inst.selectedObjects);

                        Debug.LogFormat("{0}Setting history...", EditorPlugin.className);
                        EditorManager.inst.history.Add(new History.Command("Delete Object", delegate ()
                        {
                            Delete();
                        }, delegate ()
                        {
                            ObjectEditor.inst.DeselectAllObjects();
                            inst.StartCoroutine(ObjectEditor.inst.AddPrefabExpandedToLevel(prefab, true, 0f, true));
                        }), false);

                        Debug.LogFormat("{0}Finally deleting object...", EditorPlugin.className);
                        if (ObjectEditor.inst.CurrentBeatmapObjectSelection)
                            inst.StartCoroutine(DeleteObject(ObjectEditor.inst.CurrentBeatmapObjectSelection));
                        if (ObjectEditor.inst.CurrentPrefabObjectSelection)
                            inst.StartCoroutine(DeleteObject(ObjectEditor.inst.CurrentPrefabObjectSelection));

                        EditorManager.inst.DisplayNotification("Deleted Beatmap Object!", 1f, EditorManager.NotificationType.Success);
                        Debug.LogFormat("{0}Done!", EditorPlugin.className);
                    }
                }
                else
                {
                    EditorManager.inst.DisplayNotification("Can't Delete Only Beatmap Object", 1f, EditorManager.NotificationType.Error, false);
                }
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

                    inst.StartCoroutine(DeleteEvent(RTEventEditor.inst.SelectedKeyframes));
                    EventEditor.inst.SetCurrentEvent(0, 0);
                    EditorManager.inst.DisplayNotification("Deleted Event Keyframes.", 1f, EditorManager.NotificationType.Success, false);
                }
                else if (EventEditor.inst.currentEvent != 0)
                {
                    EventEditor.inst.DeleteEvent(EventEditor.inst.currentEventType, EventEditor.inst.currentEvent);
                    EditorManager.inst.DisplayNotification("Deleted Event Keyframe.", 1f, EditorManager.NotificationType.Success, false);
                }
                else
                {
                    EditorManager.inst.DisplayNotification("Can't Delete First Event Keyframe.", 1f, EditorManager.NotificationType.Error, false);
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
                    EditorManager.inst.DisplayNotification("Deleted Checkpoint.", 1f, EditorManager.NotificationType.Success, false);
                }
                EditorManager.inst.DisplayNotification("Can't Delete First Checkpoint.", 1f, EditorManager.NotificationType.Error, false);
                return;
            }
        }

        public IEnumerator DeleteObjects(bool _set = true)
        {
            ienumRunning = true;

            float delay = 0f;
            var list = ObjectEditor.inst.SelectedBeatmapObjects;
            var list2 = ObjectEditor.inst.SelectedPrefabObjects;
            int count = ObjectEditor.inst.SelectedObjectCount;

            int num = DataManager.inst.gameData.beatmapObjects.Count;
            foreach (var obj in list)
            {
                if (obj.Index < num)
                {
                    num = obj.Index;
                }
            }
            foreach (var obj in list2)
            {
                if (obj.Index < num)
                {
                    num = obj.Index;
                }
            }

            EditorManager.inst.DisplayNotification("Deleting Beatmap Objects [ " + count + " ]", 1f, EditorManager.NotificationType.Success);

            foreach (var obj in list)
            {
                yield return new WaitForSeconds(delay);
                inst.StartCoroutine(DeleteObject(obj, _set));
                delay += 0.0001f;
            }
            
            foreach (var obj in list2)
            {
                yield return new WaitForSeconds(delay);
                inst.StartCoroutine(DeleteObject(obj, _set));
                delay += 0.0001f;
            }

            EditorManager.inst.DisplayNotification("Deleted Beatmap Objects [ " + count + " ]", 1f, EditorManager.NotificationType.Success);

            ienumRunning = false;
            yield break;
        }

        public IEnumerator DeleteObject<T>(TimelineObject<T> timelineObject, bool _set = true)
        {
            int index = timelineObject.Index;

            if (timelineObject.IsBeatmapObject)
            {
                var beatmapObject = timelineObject.Data as BaseBeatmapObject;
                Updater.UpdateProcessor(beatmapObject, false);

                if (DataManager.inst.gameData.beatmapObjects.Count > 1)
                {
                    string id = beatmapObject.id;

                    if (timelineBeatmapObjects.ContainsKey(id))
                    {
                        timelineBeatmapObjects[id].selected = false;
                        Destroy(timelineBeatmapObjects[id].GameObject);
                        timelineBeatmapObjects.Remove(id);
                    }

                    index = DataManager.inst.gameData.beatmapObjects.FindIndex(x => x.id == id);

                    DataManager.inst.gameData.beatmapObjects.RemoveAt(index);

                    if (_set)
                    {
                        if (DataManager.inst.gameData.beatmapObjects.Count > 0)
                        {
                            ObjectEditor.inst.SetCurrentObject(TimelineBeatmapObjects[Mathf.Clamp(index - 1, 0, DataManager.inst.gameData.beatmapObjects.Count - 1)]);
                        }
                    }

                    foreach (var bm in DataManager.inst.gameData.beatmapObjects)
                    {
                        if (bm.parent == id)
                        {
                            bm.parent = "";

                            Updater.UpdateProcessor(bm);
                        }
                    }
                }
                else
                    EditorManager.inst.DisplayNotification("Can't delete only object", 2f, EditorManager.NotificationType.Error);
            }
            else if (timelineObject.IsPrefabObject)
            {
                var prefabObject = timelineObject.Data as BasePrefabObject;

                Updater.UpdatePrefab(prefabObject, false);

                string id = prefabObject.ID;

                if (timelineBeatmapObjects.ContainsKey(id))
                {
                    timelineBeatmapObjects[id].selected = false;
                    Destroy(timelineBeatmapObjects[id].GameObject);
                    timelineBeatmapObjects.Remove(id);
                }

                index = DataManager.inst.gameData.prefabObjects.FindIndex(x => x.ID == id);
                DataManager.inst.gameData.prefabObjects.RemoveAt(index);
                if (_set)
                {
                    if (DataManager.inst.gameData.prefabObjects.Count > 0)
                    {
                        ObjectEditor.inst.SetCurrentObject(TimelinePrefabObjects[Mathf.Clamp(index - 1, 0, DataManager.inst.gameData.prefabObjects.Count - 1)]);
                    }
                    else if (DataManager.inst.gameData.beatmapData.checkpoints.Count > 0)
                    {
                        CheckpointEditor.inst.SetCurrentCheckpoint(0);
                    }
                }
            }
            yield break;
        }

        public IEnumerator DeleteKeyframes()
        {
            ienumRunning = true;

            float delay = 0f;
            var list = new List<TimelineObject<EventKeyframe>>();
            foreach (var keyframeSelection in RTEventEditor.inst.SelectedKeyframes)
            {
                list.Add(keyframeSelection);
            }

            list = (from x in list
                    orderby x.Index descending
                    select x).ToList();

            int count = list.Count;

            EditorManager.inst.DisplayNotification("Deleting Object Keyframes [ " + count + " ]", 2f, EditorManager.NotificationType.Success);

            var selection = ObjectEditor.inst.CurrentSelection;

            foreach (var keyframeSelection2 in RTEventEditor.inst.SelectedKeyframes)
            {
                if (keyframeSelection2.Index != 0)
                {
                    yield return new WaitForSeconds(delay);

                    selection.events[keyframeSelection2.Type].RemoveAt(keyframeSelection2.Index);

                    delay += 0.0001f;
                }
                else
                    EditorManager.inst.DisplayNotification("Can't delete first Keyframe", 2f, EditorManager.NotificationType.Error);
            }

            ObjEditor.inst.SetCurrentKeyframe(0);
            ObjectEditor.RenderTimelineObject(ObjectEditor.inst.CurrentBeatmapObjectSelection);
            Updater.UpdateProcessor(selection, "Keyframes");

            ObjectEditor.inst.RenderKeyframes(selection);

            EditorManager.inst.DisplayNotification("Deleted Object Keyframes [ " + count + " ]", 1f, EditorManager.NotificationType.Success);

            ienumRunning = false;

            yield break;
        }

        public IEnumerator DeleteEvent(List<TimelineObject<EventKeyframe>> _keyframes)
        {
            ienumRunning = true;

            float delay = 0f;
            foreach (var selection in _keyframes)
            {
                yield return new WaitForSeconds(delay);
                DataManager.inst.gameData.eventObjects.allEvents[selection.Type].RemoveAt(selection.Index);
                delay += 0.0001f;
            }
            EventEditor.inst.CreateEventObjects();
            EventManager.inst.updateEvents();

            ienumRunning = false;
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

        public static int GetLayer(int _layer)
        {
            if (_layer > 0)
            {
                if (_layer < 5)
                {
                    int l = _layer;
                    return l;
                }
                else
                {
                    int l = _layer + 1;
                    return l;
                }
            }
            return 0;
        }

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
            Image layerImage = layersIF.gameObject.GetComponent<Image>();
            DataManager.inst.UpdateSettingInt("EditorLayer", layer);
            int oldLayer = Layer;

            Layer = layer;
            TimelineOverlayImage.color = GetLayerColor(layer);
            layerImage.color = GetLayerColor(layer);

            layersIF.onValueChanged.RemoveAllListeners();
            layersIF.text = layer.ToString();
            layersIF.onValueChanged.AddListener(delegate (string _value)
            {
                if (int.TryParse(_value, out int num))
                    SetLayer(Mathf.Clamp(num - 1, 0, int.MaxValue));
            });
            
            switch (layerType)
            {
                case LayerType.Objects:
                    {
                        EventEditor.inst.EventLabels.SetActive(false);
                        EventEditor.inst.EventHolders.SetActive(false);

                        foreach (var timelineObject in timelineKeyframes)
                            Destroy(timelineObject.GameObject);

                        ObjectEditor.RenderTimelineObjects();

                        if (CheckpointEditor.inst.checkpoints.Count > 0)
                        {
                            foreach (var obj2 in CheckpointEditor.inst.checkpoints)
                                Destroy(obj2);

                            CheckpointEditor.inst.checkpoints.Clear();
                        }

                        CheckpointEditor.inst.CreateGhostCheckpoints();
                        LayerToggle.isOn = false;
                        break;
                    }
                case LayerType.Events:
                    {
                        EventEditor.inst.EventLabels.SetActive(true);
                        EventEditor.inst.EventHolders.SetActive(true);
                        RTEventEditor.inst.CreateEventObjects();
                        CheckpointEditor.inst.CreateCheckpoints();
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
            var sortList = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/autokill/tod-dropdown"));
            sortList.transform.SetParent(EditorManager.inst.GetDialog("Open File Popup").Dialog);
            sortList.transform.localScale = Vector3.one;

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

            Dropdown sortListDD = sortList.GetComponent<Dropdown>();
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

            var checkDes = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/toggle"));
            checkDes.transform.SetParent(EditorManager.inst.GetDialog("Open File Popup").Dialog);
            checkDes.transform.localScale = Vector3.one;

            var checkDesRT = checkDes.GetComponent<RectTransform>();
            checkDesRT.anchoredPosition = GetEditorProperty("Open Level Toggle Position").GetConfigEntry<Vector2>().Value;

            checkDes.transform.Find("title").GetComponent<Text>().enabled = false;
            var titleRT = checkDes.transform.Find("title").GetComponent<RectTransform>();
            titleRT.sizeDelta = new Vector2(110f, 32f);

            var toggle = checkDes.transform.Find("toggle").GetComponent<Toggle>();
            toggle.GetComponent<Toggle>().isOn = true;
            toggle.GetComponent<Toggle>().onValueChanged.AddListener(delegate (bool _value)
            {
                levelAscend = _value;
                EditorManager.inst.RenderOpenBeatmapPopup();
            });

            if (toggle.gameObject)
                TooltipHelper.AddTooltip(toggle.gameObject, new List<HoverTooltip.Tooltip> { sortListTip.tooltipLangauges[0] });
        }

        public void CreateObjectSearch()
        {
            var objectSearch = Instantiate(EditorManager.inst.GetDialog("Parent Selector").Dialog.gameObject);
            objectSearch.transform.SetParent(EditorManager.inst.GetDialog("Parent Selector").Dialog.GetParent());
            objectSearch.transform.localScale = Vector3.one;
            objectSearch.transform.localPosition = Vector3.zero;
            objectSearch.name = "Object Search";

            var objectSearchRT = objectSearch.GetComponent<RectTransform>();
            objectSearchRT.sizeDelta = new Vector2(600f, 450f);
            var objectSearchPanel = objectSearch.transform.Find("Panel").GetComponent<RectTransform>();
            objectSearchPanel.sizeDelta = new Vector2(632f, 32f);
            objectSearchPanel.transform.Find("Text").GetComponent<Text>().text = "Object Search";
            objectSearch.transform.Find("search-box").GetComponent<RectTransform>().sizeDelta = new Vector2(600f, 32f);
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
                RefreshObjectSearch(delegate (BaseBeatmapObject beatmapObject)
                {
                    if (timelineBeatmapObjects.ContainsKey(beatmapObject.id))
                        ObjectEditor.inst.SetCurrentObject(timelineBeatmapObjects[beatmapObject.id], true);
                });
            });
            searchBar.transform.Find("Placeholder").GetComponent<Text>().text = "Search for object...";

            // Turn this into a separate method
            var propWin = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/Edit/Edit Dropdown/Cut"));
            propWin.transform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/Edit/Edit Dropdown").transform);
            propWin.transform.localScale = Vector3.one;
            propWin.name = "Search Objects";
            propWin.transform.Find("Text").GetComponent<Text>().text = "Search Objects";
            propWin.transform.Find("Text").GetComponent<RectTransform>().sizeDelta = new Vector2(224f, 0f);
            propWin.transform.Find("Text 1").GetComponent<Text>().text = "";

            var propWinButton = propWin.GetComponent<Button>();
            propWinButton.onClick.ClearAll();
            propWinButton.onClick.AddListener(delegate ()
            {
                EditorManager.inst.ShowDialog("Object Search Popup");
                RefreshObjectSearch(delegate (BaseBeatmapObject beatmapObject)
                {
                    if (timelineBeatmapObjects.ContainsKey(beatmapObject.id))
                        ObjectEditor.inst.SetCurrentObject(timelineBeatmapObjects[beatmapObject.id], true);
                });
            });

            propWin.SetActive(true);

            propWin.transform.Find("Image").GetComponent<Image>().sprite = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/parent/parent/image").GetComponent<Image>().sprite;

            EditorHelper.AddEditorPopup("Object Search Popup", objectSearch);
        }

        public void CreateWarningPopup()
        {
            var warningPopup = Instantiate(EditorManager.inst.GetDialog("Save As Popup").Dialog.gameObject);
            var warningPopupTF = warningPopup.transform;
            warningPopupTF.SetParent(EditorManager.inst.GetDialog("Save As Popup").Dialog.GetParent());
            warningPopupTF.localScale = Vector3.one;
            warningPopupTF.localPosition = Vector3.zero;
            warningPopup.name = "Warning Popup";

            var main = warningPopupTF.GetChild(0);

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
                yield return inst.StartCoroutine(RTCode.IEvaluate(RTFile.ReadFromFile(code)));
            }

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
            string text = withoutFullPath + "/";
            __instance.ClearDialogs(new EditorManager.EditorDialog.DialogType[1]);
            __instance.ShowDialog("File Info Popup");

            var fileInfo = __instance.GetDialog("File Info Popup").Dialog.transform.Find("text").GetComponent<Text>();

            fileInfo.text = "Loading Level Data for [" + withoutList + "]";

            Debug.LogFormat("{0}Loading {1}...", EditorPlugin.className, text);
            rawJSON = FileManager.inst.LoadJSONFile(text + "level.lsb");
            rawMetadataJSON = FileManager.inst.LoadJSONFile(text + "metadata.lsb");

            if (string.IsNullOrEmpty(rawMetadataJSON))
            {
                dataManager.SaveMetadata(text + "metadata.lsb");
            }

            gameManager.path = text + "level.lsb";
            gameManager.basePath = text;
            gameManager.levelName = withoutList;
            fileInfo.text = "Loading Level Music for [" + withoutList + "]\n\nIf this is taking more than a minute or two check if the .ogg file is corrupt.";

            Debug.LogFormat("{0}Loading audio for {1}...", EditorPlugin.className, _levelName);
            if (RTFile.FileExists(text + "level.ogg"))
            {
                yield return inst.StartCoroutine(FileManager.inst.LoadMusicFile(text + "level.ogg", delegate (AudioClip _song)
                {
                    _song.name = withoutList;
                    if (_song)
                    {
                        song = _song;
                    }
                }));
            }
            else if (RTFile.FileExists(text + "level.wav"))
            {
                yield return inst.StartCoroutine(FileManager.inst.LoadMusicFile(text + "level.wav", delegate (AudioClip _song)
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
                dataManager.ParseMetadata(rawMetadataJSON, true);
                rawJSON = dataManager.gameData.UpdateBeatmap(rawJSON, DataManager.inst.metaData.beatmap.game_version);
                dataManager.gameData.eventObjects = new DataManager.GameData.EventObjects();
                inst.StartCoroutine(Parser.ParseBeatmap(rawJSON, true));

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
            EventEditor.inst.SetCurrentEvent(0, 0);
            CheckpointEditor.inst.SetCurrentCheckpoint(0);
            MetadataEditor.inst.Render();
            if (__instance.layer == 5)
            {
                CheckpointEditor.inst.CreateCheckpoints();
            }
            else
            {
                CheckpointEditor.inst.CreateGhostCheckpoints();
            }
            fileInfo.text = "Updating states for [" + withoutList + "]";
            DiscordController.inst.OnStateChange("Editing: " + DataManager.inst.metaData.song.title);
            objEditor.CreateTimelineObjects();
            objectManager.updateObjects();
            EventEditor.inst.CreateEventObjects();
            BackgroundManager.inst.UpdateBackgrounds();
            gameManager.UpdateTheme();
            MarkerEditor.inst.CreateMarkers();
            EventManager.inst.updateEvents();

            //SetLastSaved();

            ObjectEditor.CreateTimelineObjects();
            ObjectEditor.RenderTimelineObjects();
            ObjectEditor.inst.SetCurrentObject(TimelineBeatmapObjects[0]);

            __instance.HideDialog("File Info Popup");
            __instance.CancelInvoke("LoadingIconUpdate");

            gameManager.ResetCheckpoints(true);
            gameManager.gameState = GameManager.State.Playing;
            __instance.DisplayNotification(withoutList + " Level Loaded", 2f, EditorManager.NotificationType.Success, false);
            __instance.UpdatePlayButton();
            __instance.hasLoadedLevel = true;

            if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + GameManager.inst.basePath + "autosaves"))
            {
                Directory.CreateDirectory(RTFile.ApplicationDirectory + GameManager.inst.basePath + "autosaves");
            }

            // Change this to instead add the files to EditorManager.inst.autosaves
            {
                string[] files = Directory.GetFiles(FileManager.GetAppPath() + "/" + GameManager.inst.basePath, "autosaves/autosave_*.lsb", SearchOption.TopDirectoryOnly);
                files.ToList().Sort();
                //int num = 0;
                //foreach (string text2 in files)
                //{
                //	if (num != files.Count() - 1)
                //	{
                //		File.Delete(text2);
                //	}
                //	num++;
                //}

                __instance.autosaves.Clear();

                foreach (var file in files)
                {
                    __instance.autosaves.Add(file);
                }

                SetAutosave();
            }


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
                    var orig = DataManager.BeatmapTheme.Parse(jn);
                    if (jn["id"] == null)
                    {
                        var beatmapTheme = DataManager.BeatmapTheme.DeepCopy(orig);
                        beatmapTheme.id = LSText.randomNumString(6);
                        DataManager.inst.BeatmapThemes.Remove(orig);
                        FileManager.inst.DeleteFileRaw(lsfile.FullPath);
                        ThemeEditor.inst.SaveTheme(beatmapTheme);
                        DataManager.inst.BeatmapThemes.Add(beatmapTheme);
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

            string autosavePath = $"{RTFile.ApplicationDirectory}{GameManager.inst.basePath}autosaves/autosave_{DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss")}.lsb";

            //string autosavePath = string.Concat(new string[]
            //{
            //	FileManager.GetAppPath(),
            //	"/",
            //	GameManager.inst.basePath,
            //	"autosaves/autosave_",
            //	DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss"),
            //	".lsb"
            //});
            if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + GameManager.inst.basePath + "autosaves"))
                Directory.CreateDirectory(RTFile.ApplicationDirectory + GameManager.inst.basePath + "autosaves");

            EditorManager.inst.DisplayNotification("Autosaving backup!", 2f, EditorManager.NotificationType.Warning, false);

            EditorManager.inst.autosaves.Add(autosavePath);

            while (EditorManager.inst.autosaves.Count > AutoSaveLimit)
            {
                var first = EditorManager.inst.autosaves[0];
                if (RTFile.FileExists(first))
                    File.Delete(first);

                EditorManager.inst.autosaves.RemoveAt(0);
            }

            EditorManager.inst.StartCoroutine(DataManager.inst.SaveData(autosavePath));

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
            inst.StartCoroutine(ProjectData.Writer.SaveData(RTFile.ApplicationDirectory + editorListSlash + __instance.newLevelName + "/level.lsb", CreateBaseBeatmap()));
            var dataManager = DataManager.inst;
            var metaData = new Metadata();
            metaData.beatmap.game_version = "4.1.16";
            metaData.song.title = __instance.newLevelName;
            metaData.creator.steam_name = SteamWrapper.inst.user.displayName;
            metaData.creator.steam_id = SteamWrapper.inst.user.id;
            metaData.beatmap.workshop_id = UnityEngine.Random.Range(0, int.MaxValue);
            metaData.id = LSText.randomNumString(16);

            dataManager.SaveMetadata(RTFile.ApplicationDirectory + editorListSlash + __instance.newLevelName + "/metadata.lsb");
            inst.StartCoroutine(LoadLevel(__instance, __instance.newLevelName));
            __instance.HideDialog("New File Popup");
        }

        public GameData CreateBaseBeatmap()
        {
            var gameData = new GameData();
            gameData.beatmapData = new DataManager.GameData.BeatmapData();
            gameData.beatmapData.levelData = new DataManager.GameData.BeatmapData.LevelData();
            gameData.beatmapData.checkpoints.Add(new DataManager.GameData.BeatmapData.Checkpoint(false, "Base Checkpoint", 0f, Vector2.zero));
            var editorData = new LevelEditorData();
            gameData.beatmapData.editorData = editorData;

            #region Events
            //Move
            {
                List<BaseEventKeyframe> list = new List<BaseEventKeyframe>();
                BaseEventKeyframe eventKeyframe = new BaseEventKeyframe();
                eventKeyframe.eventTime = 0f;
                eventKeyframe.SetEventValues(new float[2]);
                list.Add(eventKeyframe);

                gameData.eventObjects.allEvents[0] = list;
            }

            //Zoom
            {
                List<BaseEventKeyframe> list2 = new List<BaseEventKeyframe>();
                BaseEventKeyframe eventKeyframe2 = new BaseEventKeyframe();
                eventKeyframe2.eventTime = 0f;
                BaseEventKeyframe eventKeyframe3 = eventKeyframe2;
                float[] array = new float[2];
                array[0] = 20f;
                eventKeyframe3.SetEventValues(array);
                list2.Add(eventKeyframe2);

                gameData.eventObjects.allEvents[1] = list2;
            }

            //Rotate
            {
                List<BaseEventKeyframe> list3 = new List<BaseEventKeyframe>();
                BaseEventKeyframe eventKeyframe4 = new BaseEventKeyframe();
                eventKeyframe4.eventTime = 0f;
                eventKeyframe4.SetEventValues(new float[2]);
                list3.Add(eventKeyframe4);

                gameData.eventObjects.allEvents[2] = list3;
            }

            //Shake
            {
                List<BaseEventKeyframe> list4 = new List<BaseEventKeyframe>();
                BaseEventKeyframe eventKeyframe5 = new BaseEventKeyframe();
                eventKeyframe5.eventTime = 0f;
                eventKeyframe5.SetEventValues(new float[3]
                    {
                        0f,
                        1f,
                        1f
                    });
                list4.Add(eventKeyframe5);

                gameData.eventObjects.allEvents[3] = list4;
            }

            //Theme
            {
                List<BaseEventKeyframe> list5 = new List<BaseEventKeyframe>();
                BaseEventKeyframe eventKeyframe6 = new BaseEventKeyframe();
                eventKeyframe6.eventTime = 0f;
                eventKeyframe6.SetEventValues(new float[2]);
                list5.Add(eventKeyframe6);

                gameData.eventObjects.allEvents[4] = list5;
            }

            //Chromatic
            {
                List<BaseEventKeyframe> list6 = new List<BaseEventKeyframe>();
                BaseEventKeyframe eventKeyframe7 = new BaseEventKeyframe();
                eventKeyframe7.eventTime = 0f;
                eventKeyframe7.SetEventValues(new float[2]);
                list6.Add(eventKeyframe7);

                gameData.eventObjects.allEvents[5] = list6;
            }

            //Bloom
            {
                List<BaseEventKeyframe> list7 = new List<BaseEventKeyframe>();
                BaseEventKeyframe eventKeyframe8 = new BaseEventKeyframe();
                eventKeyframe8.eventTime = 0f;
                eventKeyframe8.SetEventValues(new float[5]
                    {
                        0f,
                        7f,
                        1f,
                        0f,
                        18f
                    });
                list7.Add(eventKeyframe8);

                gameData.eventObjects.allEvents[6] = list7;
            }

            //Vignette
            {
                List<BaseEventKeyframe> list8 = new List<BaseEventKeyframe>();
                BaseEventKeyframe eventKeyframe9 = new BaseEventKeyframe();
                eventKeyframe9.eventTime = 0f;
                eventKeyframe9.SetEventValues(new float[7]
                    {
                        0f,
                        0f,
                        0f,
                        0f,
                        0f,
                        0f,
                        18f
                    });
                list8.Add(eventKeyframe9);

                gameData.eventObjects.allEvents[7] = list8;
            }

            //Lens
            {
                List<BaseEventKeyframe> list9 = new List<BaseEventKeyframe>();
                BaseEventKeyframe eventKeyframe10 = new BaseEventKeyframe();
                eventKeyframe10.eventTime = 0f;
                eventKeyframe10.SetEventValues(new float[6]
                    {
                        0f,
                        0f,
                        0f,
                        1f,
                        1f,
                        1f
                    });
                list9.Add(eventKeyframe10);

                gameData.eventObjects.allEvents[8] = list9;
            }

            //Grain
            {
                List<BaseEventKeyframe> list10 = new List<BaseEventKeyframe>();
                BaseEventKeyframe eventKeyframe11 = new BaseEventKeyframe();
                eventKeyframe11.eventTime = 0f;
                eventKeyframe11.SetEventValues(new float[3]);
                list10.Add(eventKeyframe11);

                gameData.eventObjects.allEvents[9] = list10;
            }

            //ColorGrading
            if (gameData.eventObjects.allEvents.Count > 10)
            {
                List<BaseEventKeyframe> list11 = new List<BaseEventKeyframe>();
                BaseEventKeyframe eventKeyframe12 = new BaseEventKeyframe();
                eventKeyframe12.eventTime = 0f;
                eventKeyframe12.SetEventValues(new float[9]);
                list11.Add(eventKeyframe12);

                gameData.eventObjects.allEvents[10] = list11;
            }

            //Ripples
            if (gameData.eventObjects.allEvents.Count > 11)
            {
                List<BaseEventKeyframe> list12 = new List<BaseEventKeyframe>();
                BaseEventKeyframe eventKeyframe13 = new BaseEventKeyframe();
                eventKeyframe13.eventTime = 0f;
                eventKeyframe13.SetEventValues(new float[5]
                    {
                        0f,
                        0f,
                        1f,
                        0f,
                        0f
                    });
                list12.Add(eventKeyframe13);

                gameData.eventObjects.allEvents[11] = list12;
            }

            //RadialBlur
            if (gameData.eventObjects.allEvents.Count > 12)
            {
                List<BaseEventKeyframe> list13 = new List<BaseEventKeyframe>();
                BaseEventKeyframe eventKeyframe14 = new BaseEventKeyframe();
                eventKeyframe14.eventTime = 0f;
                eventKeyframe14.SetEventValues(new float[2]
                    {
                        0f,
                        6f
                    });
                list13.Add(eventKeyframe14);

                gameData.eventObjects.allEvents[12] = list13;
            }

            //ColorSplit
            if (gameData.eventObjects.allEvents.Count > 13)
            {
                List<BaseEventKeyframe> list14 = new List<BaseEventKeyframe>();
                BaseEventKeyframe eventKeyframe15 = new BaseEventKeyframe();
                eventKeyframe15.eventTime = 0f;
                eventKeyframe15.SetEventValues(new float[2]);
                list14.Add(eventKeyframe15);

                gameData.eventObjects.allEvents[13] = list14;
            }

            //Camera Offset
            if (gameData.eventObjects.allEvents.Count > 14)
            {
                List<BaseEventKeyframe> list14 = new List<BaseEventKeyframe>();
                BaseEventKeyframe eventKeyframe15 = new BaseEventKeyframe();
                eventKeyframe15.eventTime = 0f;
                eventKeyframe15.SetEventValues(new float[2]);
                list14.Add(eventKeyframe15);

                gameData.eventObjects.allEvents[14] = list14;
            }

            //Gradient
            if (gameData.eventObjects.allEvents.Count > 15)
            {
                List<BaseEventKeyframe> list14 = new List<BaseEventKeyframe>();
                BaseEventKeyframe eventKeyframe15 = new BaseEventKeyframe();
                eventKeyframe15.eventTime = 0f;
                eventKeyframe15.SetEventValues(new float[5]
                    {
                        0f,
                        0f,
                        18f,
                        18f,
                        0f
                    });
                list14.Add(eventKeyframe15);

                gameData.eventObjects.allEvents[15] = list14;
            }

            //DoubleVision
            if (gameData.eventObjects.allEvents.Count > 16)
            {
                List<BaseEventKeyframe> list14 = new List<BaseEventKeyframe>();
                BaseEventKeyframe eventKeyframe15 = new BaseEventKeyframe();
                eventKeyframe15.eventTime = 0f;
                eventKeyframe15.SetEventValues(new float[2]);
                list14.Add(eventKeyframe15);

                gameData.eventObjects.allEvents[16] = list14;
            }

            //ScanLines
            if (gameData.eventObjects.allEvents.Count > 17)
            {
                List<BaseEventKeyframe> list14 = new List<BaseEventKeyframe>();
                BaseEventKeyframe eventKeyframe15 = new BaseEventKeyframe();
                eventKeyframe15.eventTime = 0f;
                eventKeyframe15.SetEventValues(new float[3]);
                list14.Add(eventKeyframe15);

                gameData.eventObjects.allEvents[17] = list14;
            }

            //Blur
            if (gameData.eventObjects.allEvents.Count > 18)
            {
                List<BaseEventKeyframe> list14 = new List<BaseEventKeyframe>();
                BaseEventKeyframe eventKeyframe15 = new BaseEventKeyframe();
                eventKeyframe15.eventTime = 0f;
                eventKeyframe15.SetEventValues(new float[2]
                    {
                        0f,
                        6f
                    });
                list14.Add(eventKeyframe15);

                gameData.eventObjects.allEvents[18] = list14;
            }

            //Pixelize
            if (gameData.eventObjects.allEvents.Count > 19)
            {
                List<BaseEventKeyframe> list14 = new List<BaseEventKeyframe>();
                BaseEventKeyframe eventKeyframe15 = new BaseEventKeyframe();
                eventKeyframe15.eventTime = 0f;
                eventKeyframe15.SetEventValues(new float[2]);
                list14.Add(eventKeyframe15);

                gameData.eventObjects.allEvents[19] = list14;
            }

            //BG
            if (gameData.eventObjects.allEvents.Count > 20)
            {
                List<BaseEventKeyframe> list14 = new List<BaseEventKeyframe>();
                BaseEventKeyframe eventKeyframe15 = new BaseEventKeyframe();
                eventKeyframe15.eventTime = 0f;
                eventKeyframe15.SetEventValues(new float[2]
                    {
                        18f,
                        0f
                    });
                list14.Add(eventKeyframe15);

                gameData.eventObjects.allEvents[20] = list14;
            }

            //Invert
            if (gameData.eventObjects.allEvents.Count > 21)
            {
                List<BaseEventKeyframe> list14 = new List<BaseEventKeyframe>();
                BaseEventKeyframe eventKeyframe15 = new BaseEventKeyframe();
                eventKeyframe15.eventTime = 0f;
                eventKeyframe15.SetEventValues(new float[2]
                {
                    0f,
                    0f
                });
                list14.Add(eventKeyframe15);

                gameData.eventObjects.allEvents[21] = list14;
            }

            //Timeline
            if (gameData.eventObjects.allEvents.Count > 22)
            {
                List<BaseEventKeyframe> list14 = new List<BaseEventKeyframe>();
                BaseEventKeyframe eventKeyframe15 = new BaseEventKeyframe();
                eventKeyframe15.eventTime = 0f;
                eventKeyframe15.SetEventValues(new float[7]
                {
                    0f,
                    0f,
                    -342f,
                    1f,
                    1f,
                    0f,
                    18f
                });
                list14.Add(eventKeyframe15);

                gameData.eventObjects.allEvents[22] = list14;
            }

            //Player
            if (gameData.eventObjects.allEvents.Count > 23)
            {
                List<BaseEventKeyframe> list14 = new List<BaseEventKeyframe>();
                BaseEventKeyframe eventKeyframe15 = new BaseEventKeyframe();
                eventKeyframe15.eventTime = 0f;
                eventKeyframe15.SetEventValues(new float[4]
                    {
                        0f,
                        0f,
                        0f,
                        0f
                    });
                list14.Add(eventKeyframe15);

                gameData.eventObjects.allEvents[23] = list14;
            }

            //Follow Player
            if (gameData.eventObjects.allEvents.Count > 24)
            {
                List<BaseEventKeyframe> list14 = new List<BaseEventKeyframe>();
                BaseEventKeyframe eventKeyframe15 = new BaseEventKeyframe();
                eventKeyframe15.eventTime = 0f;
                eventKeyframe15.SetEventValues(new float[10]
                {
                    0f,
                    0f,
                    0f,
                    0.5f,
                    0f,
                    9999f,
                    -9999f,
                    9999f,
                    -9999f,
                    1f
                });
                list14.Add(eventKeyframe15);

                gameData.eventObjects.allEvents[24] = list14;
            }

            //Audio
            if (gameData.eventObjects.allEvents.Count > 25)
            {
                List<BaseEventKeyframe> list14 = new List<BaseEventKeyframe>();
                BaseEventKeyframe eventKeyframe15 = new BaseEventKeyframe();
                eventKeyframe15.eventTime = 0f;
                eventKeyframe15.SetEventValues(new float[2]
                {
                    1f,
                    1f
                });
                list14.Add(eventKeyframe15);

                gameData.eventObjects.allEvents[25] = list14;
            }

            #endregion

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
                            backgroundObject.reactiveType = DataManager.GameData.BackgroundObject.ReactiveType.LOW;
                            break;
                        case 1:
                            backgroundObject.reactiveType = DataManager.GameData.BackgroundObject.ReactiveType.MID;
                            break;
                        case 2:
                            backgroundObject.reactiveType = DataManager.GameData.BackgroundObject.ReactiveType.HIGH;
                            break;
                    }
                    backgroundObject.reactiveScale = UnityEngine.Random.Range(0.01f, 0.04f);
                }

                backgroundObject.shape = Objects.Shapes3D[UnityEngine.Random.Range(0, 27)];

                gameData.backgroundObjects.Add(backgroundObject);
            }

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

        public void RefreshObjectSearch(Action<BaseBeatmapObject> onSelect, bool clearParent = false)
        {
            var content = EditorManager.inst.GetDialog("Object Search Popup").Dialog.Find("mask/content");

            if (clearParent)
            {
                var buttonPrefab = Instantiate(EditorManager.inst.spriteFolderButtonPrefab);
                buttonPrefab.transform.SetParent(content);
                buttonPrefab.transform.localScale = Vector3.one;
                buttonPrefab.name = "Clear Parents";
                buttonPrefab.transform.GetChild(0).GetComponent<Text>().text = "Clear Parents";

                var b = buttonPrefab.GetComponent<Button>();
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(delegate ()
                {
                    foreach (var bm in ObjectEditor.inst.SelectedBeatmapObjects.Select(x => x.Data))
                    {
                        bm.parent = "";
                        Updater.UpdateProcessor(bm);
                    }
                });

                var x = EditorManager.inst.GetDialog("Object Search Popup").Dialog.Find("Panel/x/Image").GetComponent<Image>().sprite;
                var image = buttonPrefab.transform.Find("Image").GetComponent<Image>();
                image.color = Color.red;
                image.sprite = x;
            }

            LSHelpers.DeleteChildren(content);

            foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
            {
                var regex = new Regex(@"\[([0-9])\]");
                var match = regex.Match(objectSearchTerm);

                if (string.IsNullOrEmpty(objectSearchTerm) || beatmapObject.name.ToLower().Contains(objectSearchTerm.ToLower()) || match.Success && int.Parse(match.Groups[1].ToString()) < DataManager.inst.gameData.beatmapObjects.Count && DataManager.inst.gameData.beatmapObjects.IndexOf(beatmapObject) == int.Parse(match.Groups[1].ToString()))
                {
                    var buttonPrefab = Instantiate(EditorManager.inst.spriteFolderButtonPrefab);
                    buttonPrefab.transform.SetParent(content.transform);
                    buttonPrefab.transform.localScale = Vector3.one;
                    string nm = "[" + DataManager.inst.gameData.beatmapObjects.IndexOf(beatmapObject).ToString("0000") + "/" + (DataManager.inst.gameData.beatmapObjects.Count - 1).ToString("0000") + " - " + beatmapObject.id + "] : " + beatmapObject.name;
                    buttonPrefab.name = nm;
                    buttonPrefab.transform.GetChild(0).GetComponent<Text>().text = nm;

                    var b = buttonPrefab.GetComponent<Button>();
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(delegate ()
                    {
                        onSelect?.Invoke(beatmapObject);
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
                    catch (Exception ex)
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

            int foldClamp = ConfigEntries.OpenFileFolderNameMax.Value;
            int songClamp = ConfigEntries.OpenFileSongNameMax.Value;
            int artiClamp = ConfigEntries.OpenFileArtistNameMax.Value;
            int creaClamp = ConfigEntries.OpenFileCreatorNameMax.Value;
            int descClamp = ConfigEntries.OpenFileDescriptionMax.Value;
            int dateClamp = ConfigEntries.OpenFileDateMax.Value;

            if (ConfigEntries.OpenFileFolderNameMax.Value < 3)
            {
                foldClamp = 14;
            }

            if (ConfigEntries.OpenFileSongNameMax.Value < 3)
            {
                songClamp = 22;
            }

            if (ConfigEntries.OpenFileArtistNameMax.Value < 3)
            {
                artiClamp = 16;
            }

            if (ConfigEntries.OpenFileCreatorNameMax.Value < 3)
            {
                creaClamp = 16;
            }

            if (ConfigEntries.OpenFileDescriptionMax.Value < 3)
            {
                descClamp = 16;
            }

            if (ConfigEntries.OpenFileDateMax.Value < 3)
            {
                dateClamp = 16;
            }

            #region Sorting

            //Cover
            if (EditorPlugin.levelFilter == 0 && EditorPlugin.levelAscend == false)
            {
                var result = new List<MetadataWrapper>();
                result = (from x in EditorManager.inst.loadedLevels
                          orderby x.albumArt != EditorManager.inst.AlbumArt descending
                          select x).ToList();

                EditorManager.inst.loadedLevels = result;
            }
            if (EditorPlugin.levelFilter == 0 && EditorPlugin.levelAscend == true)
            {
                var result = new List<MetadataWrapper>();
                result = (from x in EditorManager.inst.loadedLevels
                          orderby x.albumArt != EditorManager.inst.AlbumArt ascending
                          select x).ToList();

                EditorManager.inst.loadedLevels = result;
            }

            //Artist
            if (EditorPlugin.levelFilter == 1 && EditorPlugin.levelAscend == false)
            {
                var result = new List<MetadataWrapper>();
                result = (from x in EditorManager.inst.loadedLevels
                          orderby x.metadata.artist.Name descending
                          select x).ToList();

                EditorManager.inst.loadedLevels = result;
            }
            if (EditorPlugin.levelFilter == 1 && EditorPlugin.levelAscend == true)
            {
                var result = new List<MetadataWrapper>();
                result = (from x in EditorManager.inst.loadedLevels
                          orderby x.metadata.artist.Name ascending
                          select x).ToList();

                EditorManager.inst.loadedLevels = result;
            }

            //Creator
            if (EditorPlugin.levelFilter == 2 && EditorPlugin.levelAscend == false)
            {
                var result = new List<MetadataWrapper>();
                result = (from x in EditorManager.inst.loadedLevels
                          orderby x.metadata.creator.steam_name descending
                          select x).ToList();

                EditorManager.inst.loadedLevels = result;
            }
            if (EditorPlugin.levelFilter == 2 && EditorPlugin.levelAscend == true)
            {
                var result = new List<MetadataWrapper>();
                result = (from x in EditorManager.inst.loadedLevels
                          orderby x.metadata.creator.steam_name ascending
                          select x).ToList();

                EditorManager.inst.loadedLevels = result;
            }

            //Folder
            if (EditorPlugin.levelFilter == 3 && EditorPlugin.levelAscend == false)
            {
                var result = new List<MetadataWrapper>();
                result = (from x in EditorManager.inst.loadedLevels
                          orderby x.folder descending
                          select x).ToList();

                EditorManager.inst.loadedLevels = result;
            }
            if (EditorPlugin.levelFilter == 3 && EditorPlugin.levelAscend == true)
            {
                var result = new List<MetadataWrapper>();
                result = (from x in EditorManager.inst.loadedLevels
                          orderby x.folder ascending
                          select x).ToList();

                EditorManager.inst.loadedLevels = result;
            }

            //Title
            if (EditorPlugin.levelFilter == 4 && EditorPlugin.levelAscend == false)
            {
                var result = new List<MetadataWrapper>();
                result = (from x in EditorManager.inst.loadedLevels
                          orderby x.metadata.song.title descending
                          select x).ToList();

                EditorManager.inst.loadedLevels = result;
            }
            if (EditorPlugin.levelFilter == 4 && EditorPlugin.levelAscend == true)
            {
                var result = new List<MetadataWrapper>();
                result = (from x in EditorManager.inst.loadedLevels
                          orderby x.metadata.song.title ascending
                          select x).ToList();

                EditorManager.inst.loadedLevels = result;
            }

            //Difficulty
            if (EditorPlugin.levelFilter == 5 && EditorPlugin.levelAscend == false)
            {
                var result = new List<MetadataWrapper>();
                result = (from x in EditorManager.inst.loadedLevels
                          orderby x.metadata.song.difficulty descending
                          select x).ToList();

                EditorManager.inst.loadedLevels = result;
            }
            if (EditorPlugin.levelFilter == 5 && EditorPlugin.levelAscend == true)
            {
                var result = new List<MetadataWrapper>();
                result = (from x in EditorManager.inst.loadedLevels
                          orderby x.metadata.song.difficulty ascending
                          select x).ToList();

                EditorManager.inst.loadedLevels = result;
            }

            //Date Edited
            if (EditorPlugin.levelFilter == 6 && EditorPlugin.levelAscend == false)
            {
                var result = new List<MetadataWrapper>();
                result = (from x in EditorManager.inst.loadedLevels
                          orderby x.metadata.beatmap.date_edited descending
                          select x).ToList();

                EditorManager.inst.loadedLevels = result;
            }
            if (EditorPlugin.levelFilter == 6 && EditorPlugin.levelAscend == true)
            {
                var result = new List<MetadataWrapper>();
                result = (from x in EditorManager.inst.loadedLevels
                          orderby x.metadata.beatmap.date_edited ascending
                          select x).ToList();

                EditorManager.inst.loadedLevels = result;
            }

            #endregion

            Transform transform = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("mask").Find("content");
            var close = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("Panel/x");
            foreach (object obj in transform)
            {
                Destroy(((Transform)obj).gameObject);
            }
            foreach (var metadataWrapper in EditorManager.inst.loadedLevels)
            {
                var metadata = metadataWrapper.metadata;
                string name = metadataWrapper.folder;

                string difficultyName = "None";
                if (metadata.song.difficulty == 0)
                {
                    difficultyName = "easy";
                }
                if (metadata.song.difficulty == 1)
                {
                    difficultyName = "normal";
                }
                if (metadata.song.difficulty == 2)
                {
                    difficultyName = "hard";
                }
                if (metadata.song.difficulty == 3)
                {
                    difficultyName = "expert";
                }
                if (metadata.song.difficulty == 4)
                {
                    difficultyName = "expert+";
                }
                if (metadata.song.difficulty == 5)
                {
                    difficultyName = "master";
                }
                if (metadata.song.difficulty == 6)
                {
                    difficultyName = "animation";
                }

                if (RTFile.FileExists(RTFile.ApplicationDirectory + editorListSlash + metadataWrapper.folder + "/level.ogg"))
                {
                    if (EditorManager.inst.openFileSearch == null || !(EditorManager.inst.openFileSearch != "") || name.ToLower().Contains(EditorManager.inst.openFileSearch.ToLower()) || metadata.song.title.ToLower().Contains(EditorManager.inst.openFileSearch.ToLower()) || metadata.artist.Name.ToLower().Contains(EditorManager.inst.openFileSearch.ToLower()) || metadata.creator.steam_name.ToLower().Contains(EditorManager.inst.openFileSearch.ToLower()) || metadata.song.description.ToLower().Contains(EditorManager.inst.openFileSearch.ToLower()) || difficultyName.Contains(EditorManager.inst.openFileSearch.ToLower()))
                    {
                        GameObject gameObject = Instantiate(EditorManager.inst.folderButtonPrefab);
                        gameObject.name = "Folder [" + metadataWrapper.folder + "]";
                        gameObject.transform.SetParent(transform);
                        gameObject.transform.localScale = Vector3.one;
                        //var hoverUI = gameObject.AddComponent<HoverUI>();
                        //hoverUI.size = ConfigEntries.OpenFileButtonHoverSize.Value;
                        //hoverUI.animatePos = false;
                        //hoverUI.animateSca = true;
                        HoverTooltip htt = gameObject.AddComponent<HoverTooltip>();

                        HoverTooltip.Tooltip levelTip = new HoverTooltip.Tooltip();

                        if (metadata != null)
                        {
                            gameObject.transform.GetChild(0).GetComponent<Text>().text = string.Format(ConfigEntries.OpenFileTextFormatting.Value, LSText.ClampString(metadataWrapper.folder, foldClamp), LSText.ClampString(metadata.song.title, songClamp), LSText.ClampString(metadata.artist.Name, artiClamp), LSText.ClampString(metadata.creator.steam_name, creaClamp), metadata.song.difficulty, LSText.ClampString(metadata.song.description, descClamp), LSText.ClampString(metadata.beatmap.date_edited, dateClamp));

                            if (metadata.song.difficulty == 4 && ConfigEntries.OpenFileTextInvert.Value == true && ConfigEntries.OpenFileButtonDifficultyColor.Value == true || metadata.song.difficulty == 5 && ConfigEntries.OpenFileTextInvert.Value == true && ConfigEntries.OpenFileButtonDifficultyColor.Value == true)
                            {
                                gameObject.transform.GetChild(0).GetComponent<Text>().color = LSColors.ChangeColorBrightness(ConfigEntries.OpenFileTextColor.Value, 0.7f);
                            }

                            Color difficultyColor = Color.white;

                            for (int i = 0; i < DataManager.inst.difficulties.Count; i++)
                            {
                                if (metadata.song.difficulty == i)
                                {
                                    difficultyColor = DataManager.inst.difficulties[i].color;
                                }
                                if (ConfigEntries.OpenFileButtonDifficultyColor.Value == true)
                                {
                                    gameObject.GetComponent<Image>().color = difficultyColor * ConfigEntries.OpenFileButtonDifficultyMultiply.Value;
                                }
                            }
                            levelTip.desc = "<#" + LSColors.ColorToHex(difficultyColor) + ">" + metadata.artist.Name + " - " + metadata.song.title;
                            levelTip.hint = "</color>" + metadata.song.description;
                            htt.tooltipLangauges.Add(levelTip);
                        }
                        else
                        {
                            gameObject.transform.GetChild(0).GetComponent<Text>().text = string.Format("/{0} : {1}", LSText.ClampString(metadataWrapper.folder, foldClamp), LSText.ClampString("No MetaData File", songClamp));
                        }

                        gameObject.GetComponent<Button>().onClick.AddListener(delegate ()
                        {
                            inst.StartCoroutine(LoadLevel(EditorManager.inst, name));
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

                        GameObject icon = new GameObject("icon");
                        icon.transform.SetParent(gameObject.transform);
                        icon.transform.localScale = Vector3.one;
                        icon.layer = 5;
                        RectTransform iconRT = icon.AddComponent<RectTransform>();
                        icon.AddComponent<CanvasRenderer>();
                        Image iconImage = icon.AddComponent<Image>();

                        iconRT.anchoredPosition = ConfigEntries.OpenFileCoverPosition.Value;
                        iconRT.sizeDelta = ConfigEntries.OpenFileCoverScale.Value;

                        iconImage.sprite = metadataWrapper.albumArt;

                        //Close
                        if (ConfigEntries.ShowLevelDeleteButton.Value)
                        {
                            var delete = Instantiate(close.gameObject);
                            var deleteTF = delete.transform;
                            deleteTF.SetParent(gameObject.transform);
                            deleteTF.localScale = Vector3.one;

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

        public static void SetupTempConfigs()
        {
            var fontLimit = new AcceptableValueRange<int>(1, 40);
            var hoverRange = new AcceptableValueRange<float>(0.7f, 1.4f);

            // Editor GUI
            {
                ConfigEntries.OpenFileTextHorizontalWrap = Config.Bind("Editor GUI", "Open Level Text Horizontal Wrap", HorizontalWrapMode.Wrap, "Horizontal Wrap Mode of the folder button text.");
                ConfigEntries.OpenFileTextVerticalWrap = Config.Bind("Editor GUI", "Open Level Text Vertical Wrap", VerticalWrapMode.Truncate, "Vertical Wrap Mode of the folder button text.");
                ConfigEntries.OpenFileTextColor = Config.Bind("Editor GUI", "Open Level Text Color", new Color(0.9373f, 0.9216f, 0.9373f, 1f), "Color of the folder button text.");
                ConfigEntries.OpenFileTextInvert = Config.Bind("Editor GUI", "Open Level Text Invert", true, "If the text should invert if the difficulty color is dark.");
                ConfigEntries.OpenFileTextFontSize = Config.Bind("Editor GUI", "Open Level Text Font Size", 20, new ConfigDescription("Font size of the folder button text.", fontLimit));

                ConfigEntries.OpenFileFolderNameMax = Config.Bind("Editor GUI", "Open Level Folder Name Max", 14, "Limited length of the folder name.");
                ConfigEntries.OpenFileSongNameMax = Config.Bind("Editor GUI", "Open Level Song Name Max", 22, "Limited length of the song name.");
                ConfigEntries.OpenFileArtistNameMax = Config.Bind("Editor GUI", "Open Level Artist Name Max", 16, "Limited length of the artist name.");
                ConfigEntries.OpenFileCreatorNameMax = Config.Bind("Editor GUI", "Open Level Creator Name Max", 16, "Limited length of the creator name.");
                ConfigEntries.OpenFileDescriptionMax = Config.Bind("Editor GUI", "Open Level Description Max", 16, "Limited length of the description.");
                ConfigEntries.OpenFileDateMax = Config.Bind("Editor GUI", "Open Level Date Clamp", 16, "Limited length of the date.");
                ConfigEntries.OpenFileTextFormatting = Config.Bind("Editor GUI", "Open Level Text Formatting", ".  /{0} : {1} by {2}", "The way the text is formatted for each level. {0} is folder, {1} is song, {2} is artist, {3} is creator, {4} is difficulty, {5} is description and {6} is last edited.");

                ConfigEntries.OpenFileButtonDifficultyColor = Config.Bind("Editor GUI", "Open Level Button Difficulty Color", false, "If each button matches its associated difficulty color.");
                ConfigEntries.OpenFileButtonDifficultyMultiply = Config.Bind("Editor GUI", "Open Level Button Difficulty Mulity", 1.5f, "How much each buttons' color multiplies by difficulty color.");

                ConfigEntries.OpenFileButtonNormalColor = Config.Bind("Editor GUI", "Open Level Button Normal Color", new Color(0.1647f, 0.1647f, 0.1647f, 1f), "Normal color of the folder button.");
                ConfigEntries.OpenFileButtonHighlightedColor = Config.Bind("Editor GUI", "Open Level Button Highlighted Color", new Color(0.2588f, 0.2588f, 0.2588f, 1f), "Highlighted color of the folder button.");
                ConfigEntries.OpenFileButtonPressedColor = Config.Bind("Editor GUI", "Open Level Button Pressed Color", new Color(0.2588f, 0.2588f, 0.2588f, 1f), "Pressed color of the folder button.");
                ConfigEntries.OpenFileButtonSelectedColor = Config.Bind("Editor GUI", "Open Level Button Selected Color", new Color(0.2588f, 0.2588f, 0.2588f, 1f), "Selected color of the folder button.");
                ConfigEntries.OpenFileButtonFadeDuration = Config.Bind("Editor GUI", "Open Level Button Fade Duration", 0.2f, "Fade duration of the folder button.");

                ConfigEntries.OpenFileButtonHoverSize = Config.Bind("Editor GUI", "Open Level Button Hover Size", 1f, new ConfigDescription("How big the button gets when hovered.", hoverRange));

                ConfigEntries.OpenFileCoverPosition = Config.Bind("Editor GUI", "Open Level Cover Position", new Vector2(-276f, 0f), "Position of the level cover.");
                ConfigEntries.OpenFileCoverScale = Config.Bind("Editor GUI", "Open Level Cover Size", new Vector2(26f, 26f), "Size of the level cover.");

                ConfigEntries.ChangesRefreshLevelList = Config.Bind("Editor GUI", "Changes Refresh Level List", false, "If the level list reloads whenever a change is made.");

                ConfigEntries.ShowLevelDeleteButton = Config.Bind("Editor GUI", "Open Level Show Delete Button", false, "Shows a delete button that can be used to move levels to a recycling folder.");

                ConfigEntries.TimelineObjectHoverSize = Config.Bind("Editor GUI", "Timeline Object Hover Size", 1f, new ConfigDescription("How big the button gets when hovered.", hoverRange));
                ConfigEntries.KeyframeHoverSize = Config.Bind("Editor GUI", "Keyframe Hover Size", 1f, new ConfigDescription("How big the button gets when hovered.", hoverRange));
                ConfigEntries.TimelineBarButtonsHoverSize = Config.Bind("Editor GUI", "Timeline Bar Buttons Hover Size", 1f, new ConfigDescription("How big the button gets when hovered.", hoverRange));

                ConfigEntries.MarkerColN0 = Config.Bind("Editor GUI", "Marker Color 1", Color.white, "Color 1 of the second set of marker colors.");
                ConfigEntries.MarkerColN1 = Config.Bind("Editor GUI", "Marker Color 2", Color.white, "Color 2 of the second set of marker colors.");
                ConfigEntries.MarkerColN2 = Config.Bind("Editor GUI", "Marker Color 3", Color.white, "Color 3 of the second set of marker colors.");
                ConfigEntries.MarkerColN3 = Config.Bind("Editor GUI", "Marker Color 4", Color.white, "Color 4 of the second set of marker colors.");
                ConfigEntries.MarkerColN4 = Config.Bind("Editor GUI", "Marker Color 5", Color.white, "Color 5 of the second set of marker colors.");
                ConfigEntries.MarkerColN5 = Config.Bind("Editor GUI", "Marker Color 6", Color.white, "Color 6 of the second set of marker colors.");
                ConfigEntries.MarkerColN6 = Config.Bind("Editor GUI", "Marker Color 7", Color.white, "Color 7 of the second set of marker colors.");
                ConfigEntries.MarkerColN7 = Config.Bind("Editor GUI", "Marker Color 8", Color.white, "Color 8 of the second set of marker colors.");
                ConfigEntries.MarkerColN8 = Config.Bind("Editor GUI", "Marker Color 9", Color.white, "Color 9 of the second set of marker colors.");

                ConfigEntries.PrefabButtonHoverSize = Config.Bind("Editor GUI", "Prefab Button Hover Scale", 1.05f, new ConfigDescription("How big the button gets when hovered.", hoverRange));

                ConfigEntries.PrefabINHScroll = Config.Bind("Editor GUI", "Prefab Internal Horizontal Scroll", false, "If you can scroll left / right or not.");
                ConfigEntries.PrefabINCellSize = Config.Bind("Editor GUI", "Prefab Internal Cell Size", new Vector2(383f, 32f), "Size of each Prefab Cell. Recommended values are 383 and 503.");
                ConfigEntries.PrefabINConstraint = Config.Bind("Editor GUI", "Prefab Internal Constraint Mode", GridLayoutGroup.Constraint.FixedColumnCount, "Which direction the prefab list goes.");
                ConfigEntries.PrefabINConstraintColumns = Config.Bind("Editor GUI", "Prefab Internal Constraint", 1, "How many columns the prefabs are divided into.");
                ConfigEntries.PrefabINCellSpacing = Config.Bind("Editor GUI", "Prefab Internal Spacing", new Vector2(8f, 8f), "Distance between each Prefab Cell.");
                ConfigEntries.PrefabINAxis = Config.Bind("Editor GUI", "Prefab Internal Start Axis", GridLayoutGroup.Axis.Horizontal, "Start axis of the prefab list.");
                ConfigEntries.PrefabINLDeletePos = Config.Bind("Editor GUI", "Prefab Internal Delete Button Pos", new Vector2(367f, -16f), "Position of the Delete Button. Recommended values are 367, -16 and 484, -16.");
                ConfigEntries.PrefabINLDeleteSca = Config.Bind("Editor GUI", "Prefab Internal Delete Button Sca", new Vector2(32f, 32f), "Scale of the Delete Button.");

                ConfigEntries.PrefabINNameHOverflow = Config.Bind("Editor GUI", "Prefab Internal Name HOverflow", HorizontalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
                ConfigEntries.PrefabINNameVOverflow = Config.Bind("Editor GUI", "Prefab Internal Name VOverflow", VerticalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
                ConfigEntries.PrefabINNameFontSize = Config.Bind("Editor GUI", "Prefab Internal Name Font Size", 20, "Size of the text font.");
                ConfigEntries.PrefabINTypeHOverflow = Config.Bind("Editor GUI", "Prefab Internal Type HOverflow", HorizontalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
                ConfigEntries.PrefabINTypeVOverflow = Config.Bind("Editor GUI", "Prefab Internal Type VOverflow", VerticalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
                ConfigEntries.PrefabINTypeFontSize = Config.Bind("Editor GUI", "Prefab Internal Type Font Size", 20, new ConfigDescription("Size of the text font.", fontLimit));

                ConfigEntries.PrefabEXHScroll = Config.Bind("Editor GUI", "Prefab External Horizontal Scroll", false, "If you can scroll left / right or not.");
                ConfigEntries.PrefabEXCellSize = Config.Bind("Editor GUI", "Prefab External Cell Size", new Vector2(383f, 32f), "Size of each Prefab Cell. Recommended values are 383 and 503.");
                ConfigEntries.PrefabEXConstraint = Config.Bind("Editor GUI", "Prefab External Constraint Mode", GridLayoutGroup.Constraint.FixedColumnCount, "Which direction the prefab list goes.");
                ConfigEntries.PrefabEXConstraintColumns = Config.Bind("Editor GUI", "Prefab External Constraint", 1, "How many columns the prefabs are divided into.");
                ConfigEntries.PrefabEXCellSpacing = Config.Bind("Editor GUI", "Prefab External Spacing", new Vector2(8f, 8f), "Distance between each Prefab Cell.");
                ConfigEntries.PrefabEXAxis = Config.Bind("Editor GUI", "Prefab External Start Axis", GridLayoutGroup.Axis.Horizontal, "Start axis of the prefab list.");
                ConfigEntries.PrefabEXLDeletePos = Config.Bind("Editor GUI", "Prefab External Delete Button Pos", new Vector2(367f, -16f), "Position of the Delete Button.");
                ConfigEntries.PrefabEXLDeleteSca = Config.Bind("Editor GUI", "Prefab External Delete Button Sca", new Vector2(32f, 32f), "Scale of the Delete Button.");

                ConfigEntries.PrefabEXNameHOverflow = Config.Bind("Editor GUI", "Prefab Internal Name HOverflow", HorizontalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
                ConfigEntries.PrefabEXNameVOverflow = Config.Bind("Editor GUI", "Prefab Internal Name VOverflow", VerticalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
                ConfigEntries.PrefabEXNameFontSize = Config.Bind("Editor GUI", "Prefab Internal Name Font Size", 20, "Size of the text font.");
                ConfigEntries.PrefabEXTypeHOverflow = Config.Bind("Editor GUI", "Prefab Internal Type HOverflow", HorizontalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
                ConfigEntries.PrefabEXTypeVOverflow = Config.Bind("Editor GUI", "Prefab Internal Type VOverflow", VerticalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
                ConfigEntries.PrefabEXTypeFontSize = Config.Bind("Editor GUI", "Prefab Internal Type Font Size", 20, new ConfigDescription("Size of the text font.", fontLimit));

                ConfigEntries.PrefabINANCH = Config.Bind("Editor GUI", "Prefab Internal Popup Pos", new Vector2(0f, -16f), "Position of the internal prefabs popup.");
                ConfigEntries.PrefabINSD = Config.Bind("Editor GUI", "Prefab Internal Popup Size", new Vector2(400f, -32f), "Scale of the internal prefabs popup.");
                ConfigEntries.PrefabEXANCH = Config.Bind("Editor GUI", "Prefab External Popup Pos", new Vector2(-32f, -16f), "Position of the external prefabs popup.");
                ConfigEntries.PrefabEXSD = Config.Bind("Editor GUI", "Prefab External Popup Size", new Vector2(400f, -32f), "Scale of the external prefabs popup.");
                ConfigEntries.PrefabEXPathPos = Config.Bind("Editor GUI", "Prefab External Prefab Path Pos", new Vector2(325f, 15f), "Position of the prefab path input field.");
                ConfigEntries.PrefabEXPathSca = Config.Bind("Editor GUI", "Prefab External Prefab Path Length", 150f, "Length of the prefab path input field.");
                ConfigEntries.PrefabEXRefreshPos = Config.Bind("Editor GUI", "Prefab External Prefab Refresh Pos", new Vector2(210f, 450f), "Position of the prefab refresh button.");
            }

            //Fields
            {
                ConfigEntries.TemplateThemeName = Config.Bind("Fields", "Theme Template Name", "New Theme", "Name of the template theme.");
                ConfigEntries.TemplateThemeGUIColor = Config.Bind("Fields", "Theme Template GUI", LSColors.white, "GUI Color of the template theme.");
                ConfigEntries.TemplateThemeBGColor = Config.Bind("Fields", "Theme Template BG", LSColors.gray900, "BG Color of the template theme.");
                ConfigEntries.TemplateThemePlayerColor1 = Config.Bind("Fields", "Theme Template Player 1", LSColors.HexToColor("E57373"), "Player 1 Color of the template theme.");
                ConfigEntries.TemplateThemePlayerColor2 = Config.Bind("Fields", "Theme Template Player 2", LSColors.HexToColor("64B5F6"), "Player 2 Color of the template theme.");
                ConfigEntries.TemplateThemePlayerColor3 = Config.Bind("Fields", "Theme Template Player 3", LSColors.HexToColor("81C784"), "Player 3 Color of the template theme.");
                ConfigEntries.TemplateThemePlayerColor4 = Config.Bind("Fields", "Theme Template Player 4", LSColors.HexToColor("FFB74D"), "Player 4 Color of the template theme.");
                ConfigEntries.TemplateThemeOBJColor1 = Config.Bind("Fields", "Theme Template OBJ 1", LSColors.gray100, "OBJ 1 Color of the template theme.");
                ConfigEntries.TemplateThemeOBJColor2 = Config.Bind("Fields", "Theme Template OBJ 2", LSColors.gray200, "OBJ 2 Color of the template theme.");
                ConfigEntries.TemplateThemeOBJColor3 = Config.Bind("Fields", "Theme Template OBJ 3", LSColors.gray300, "OBJ 3 Color of the template theme.");
                ConfigEntries.TemplateThemeOBJColor4 = Config.Bind("Fields", "Theme Template OBJ 4", LSColors.gray400, "OBJ 4 Color of the template theme.");
                ConfigEntries.TemplateThemeOBJColor5 = Config.Bind("Fields", "Theme Template OBJ 5", LSColors.gray500, "OBJ 5 Color of the template theme.");
                ConfigEntries.TemplateThemeOBJColor6 = Config.Bind("Fields", "Theme Template OBJ 6", LSColors.gray600, "OBJ 6 Color of the template theme.");
                ConfigEntries.TemplateThemeOBJColor7 = Config.Bind("Fields", "Theme Template OBJ 7", LSColors.gray700, "OBJ 7 Color of the template theme.");
                ConfigEntries.TemplateThemeOBJColor8 = Config.Bind("Fields", "Theme Template OBJ 8", LSColors.gray800, "OBJ 8 Color of the template theme.");
                ConfigEntries.TemplateThemeOBJColor9 = Config.Bind("Fields", "Theme Template OBJ 9", LSColors.gray900, "OBJ 9 Color of the template theme.");
                ConfigEntries.TemplateThemeBGColor1 = Config.Bind("Fields", "Theme Template BG 1", LSColors.pink100, "BG 1 Color of the template theme.");
                ConfigEntries.TemplateThemeBGColor2 = Config.Bind("Fields", "Theme Template BG 2", LSColors.pink200, "BG 2 Color of the template theme.");
                ConfigEntries.TemplateThemeBGColor3 = Config.Bind("Fields", "Theme Template BG 3", LSColors.pink300, "BG 3 Color of the template theme.");
                ConfigEntries.TemplateThemeBGColor4 = Config.Bind("Fields", "Theme Template BG 4", LSColors.pink400, "BG 4 Color of the template theme.");
                ConfigEntries.TemplateThemeBGColor5 = Config.Bind("Fields", "Theme Template BG 5", LSColors.pink500, "BG 5 Color of the template theme.");
                ConfigEntries.TemplateThemeBGColor6 = Config.Bind("Fields", "Theme Template BG 6", LSColors.pink600, "BG 6 Color of the template theme.");
                ConfigEntries.TemplateThemeBGColor7 = Config.Bind("Fields", "Theme Template BG 7", LSColors.pink700, "BG 7 Color of the template theme.");
                ConfigEntries.TemplateThemeBGColor8 = Config.Bind("Fields", "Theme Template BG 8", LSColors.pink800, "BG 8 Color of the template theme.");
                ConfigEntries.TemplateThemeBGColor9 = Config.Bind("Fields", "Theme Template BG 9", LSColors.pink900, "BG 9 Color of the template theme.");
            }

            //Preview
            {
                ConfigEntries.ShowObjectsOnLayer = Config.Bind("Preview", "Show only objects on current layer?", false, "If enabled, all objects not on current layer will be set to transparent");
                ConfigEntries.ShowObjectsAlpha = Config.Bind("Preview", "Visible object opacity", 0.2f, "Opacity of the objects not on the current layer.");
                //ConfigEntries.ShowEmpties = Config.Bind("Preview", "Show empties (Does not work)", false, "If enabled, show all objects that are set to the empty object type.");
                ConfigEntries.ShowDamagable = Config.Bind("Preview", "Only Show Damagable (Does not work)", false, "If enabled, only objects that can damage the player will be shown.");
                ConfigEntries.HighlightObjects = Config.Bind("Preview", "Highlight Objects", true, "If enabled and if cursor hovers over an object, it will be highlighted.");
                ConfigEntries.HighlightColor = Config.Bind("Preview", "Object Highlight Amount", new Color(0.1f, 0.1f, 0.1f), "If an object is hovered, it adds this amount of color to the hovered object.");
                ConfigEntries.HighlightDoubleColor = Config.Bind("Preview", "Object Highlight Double Amount", new Color(0.5f, 0.5f, 0.5f), "If an object is hovered and shift is held, it adds this amount of color to all color channels.");
            }

        }

        public static ConfigFile Config => EditorPlugin.inst.Config;

        public static EditorProperty GetEditorProperty(string name) => EditorProperties.Find(x => x.name == name);

        public static List<EditorProperty> EditorProperties => new List<EditorProperty>()
        {
            // General
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("General", "Debug", false, "If enabled, specific debugging functions for the editor will be enabled.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("General", "BPM Snaps Keyframes", false, "Makes object's keyframes snap if Snap BPM is enabled.")),
            new EditorProperty(EditorProperty.ValueType.Float,
                Config.Bind("General", "BPM Snap Divisions", 4f, "How many times the snap is divided into. Can be good for songs that don't do 4 divisions.")),
            new EditorProperty(EditorProperty.ValueType.Enum,
                Config.Bind("General", "Preferences Open Key", KeyCode.F10, "The key to press to open the Editor Properties / Preferences window.")),
            new EditorProperty(EditorProperty.ValueType.Enum,
                Config.Bind("General", "Player Editor Open Key", KeyCode.F6, "The key to press to open the Player Editor window.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("General", "Prefab Example Template", true, "Example Template prefab will always be generated into the internal prefabs for you to use.")),
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("General", "Paste Offset", false, "When enabled objects that are pasted will be pasted at an offset based on the distance between the audio time and the copied object. Otherwise, the objects will be pasted at the earliest objects start time.")),

            // Timeline
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
            new EditorProperty(EditorProperty.ValueType.Bool,
                Config.Bind("Timeline", "Marker Loop Active", false, "If the marker should loop between markers.")),
            new EditorProperty(EditorProperty.ValueType.Int,
                Config.Bind("Timeline", "Marker Loop Begin", 0, "Audio time gets set to this marker.")),
            new EditorProperty(EditorProperty.ValueType.Int,
                Config.Bind("Timeline", "Marker Loop End", 1, "If the audio time gets to the set marker time, it will loop to the beginning marker.")),

            // Data
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

            // Editor GUI
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
