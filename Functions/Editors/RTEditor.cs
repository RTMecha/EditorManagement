using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using HarmonyLib;
using BepInEx.Configuration;

using UnityEngine;
using UnityEngine.UI;
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

        void Awake()
        {
            inst = this;

            timelineKeyframes.Cast<TimelineObject<BaseEventKeyframe>>().ToList();
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

        public static bool RoundToNearest => true;
        public static bool ShowModifiedColors => true;
        public static int BPMSnapDivisions => 4;
        public static bool BPMSnapKeyframes => true;

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

        public static void Nullify()
        {
            if (inst)
            {
                inst.objectToParent = null;
            }
        }

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
            var jn = JSON.Parse("{}");

            jn["paths"]["editor"] = EditorPath;
            jn["paths"]["themes"] = ThemePath;
            jn["paths"]["prefabs"] = PrefabPath;

            RTFile.WriteToFile(EditorSettingsPath, jn.ToString(3));
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

                TriggerHelper.AddEventTrigger(layersObj, new List<EventTrigger.Entry> { TriggerHelper.ScrollDeltaInt(layersIF, 1, false, new List<int> { 1, int.MaxValue }) });
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

                pitchObj.AddComponent<InputFieldHelper>();
            }

            timelineBar.transform.Find("checkpoint").gameObject.SetActive(false);
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
            GameObject.Find("Editor GUI/sizer/main/Popups/Save As Popup/New File Popup/level-name").GetComponent<InputField>().characterValidation = InputField.CharacterValidation.None;
            GameObject.Find("Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/left/theme/name").GetComponent<InputField>().characterValidation = InputField.CharacterValidation.None;
            GameObject.Find("Editor GUI/sizer/main/EditorDialogs/PrefabDialog/data/name/input").GetComponent<InputField>().characterValidation = InputField.CharacterValidation.None;
            GameObject.Find("Editor GUI/sizer/main/Popups/New File Popup/Browser Popup").SetActive(true);

            // Quit to Arcade
            {
                var exitToArcade = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/File/File Dropdown/Quit to Main Menu"));
                exitToArcade.name = "Quit to Arcade";
                exitToArcade.transform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/File/File Dropdown").transform);
                exitToArcade.transform.localScale = Vector3.one;
                exitToArcade.transform.SetSiblingIndex(7);
                exitToArcade.transform.GetChild(0).GetComponent<Text>().text = "Quit to Arcade";

                var ex = exitToArcade.GetComponent<Button>();
                ex.onClick.ClearAll();
                ex.onClick.AddListener(delegate ()
                {
                    //if (ExampleManager.inst)
                    //{
                    //	ExampleManager.inst.Move(new List<IKeyframe<float>> { new FloatKeyframe(3f, -65f, Ease.SineInOut), new FloatKeyframe(4f, -64f, Ease.SineInOut) }, new List<IKeyframe<float>> { new FloatKeyframe(4f, 176f, Ease.SineInOut) });
                    //	var animation = new ExampleManager.Animation("Face Sad");

                    //	float tbrowLeft = -15f;

                    //	float tbrowRight = 15f;
                    //	if (ExampleManager.inst.browRight.localRotation.eulerAngles.z > 180f)
                    //		tbrowRight = 345f;

                    //	animation.floatAnimations = new List<ExampleManager.Animation.AnimationObject<float>>
                    //	{
                    //		new ExampleManager.Animation.AnimationObject<float>(new List<IKeyframe<float>>
                    //		{
                    //			new FloatKeyframe(0f, ExampleManager.inst.browLeft.localRotation.eulerAngles.z, Ease.Linear),
                    //			new FloatKeyframe(0.7f, tbrowLeft, Ease.SineInOut),
                    //		}, delegate (float x)
                    //		{
                    //			ExampleManager.inst.browLeft.localRotation = Quaternion.Euler(0f, 0f, x);
                    //		}),
                    //		new ExampleManager.Animation.AnimationObject<float>(new List<IKeyframe<float>>
                    //		{
                    //			new FloatKeyframe(0f, ExampleManager.inst.browRight.localRotation.eulerAngles.z, Ease.Linear),
                    //			new FloatKeyframe(0.7f, tbrowRight, Ease.SineInOut),
                    //		}, delegate (float x)
                    //		{
                    //			ExampleManager.inst.browRight.localRotation = Quaternion.Euler(0f, 0f, x);
                    //		}),
                    //	};

                    //	ExampleManager.inst.PlayOnce(animation);
                    //	ExampleManager.inst.Say("Are you sure you want to quit to the arcade?", onComplete: delegate () { ExampleManager.inst.talking = false; });
                    //}

                    EditorManager.inst.ShowDialog("Warning Popup");
                    //RTEditor.RefreshWarningPopup("Are you sure you want to quit to the arcade?", delegate ()
                    //{
                    //    DOTween.Clear();
                    //    ObjectManager.inst.updateObjects();
                    //    DataManager.inst.gameData = null;
                    //    DataManager.inst.gameData = new DataManager.GameData();
                    //    ObjectManager.inst.updateObjects();

                    //    ArcadeManager.inst.skippedLoad = false;
                    //    ArcadeManager.inst.forcedSkip = false;
                    //    DataManager.inst.UpdateSettingBool("IsArcade", true);

                    //    SceneManager.inst.LoadScene("Input Select");
                    //}, delegate ()
                    //{
                    //    EditorManager.inst.HideDialog("Warning Popup");
                    //});
                });
            }

            if (ModCompatibility.mods.ContainsKey("ExampleCompanion"))
            {
                var exitToArcade = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/File/File Dropdown/Quit to Main Menu"));
                exitToArcade.name = "Get Example";
                exitToArcade.transform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/View/View Dropdown").transform);
                exitToArcade.transform.localScale = Vector3.one;
                exitToArcade.transform.SetSiblingIndex(4);
                exitToArcade.transform.GetChild(0).GetComponent<Text>().text = "Get Example";

                var ex = exitToArcade.GetComponent<Button>();
                ex.onClick.ClearAll();
                ex.onClick.AddListener(delegate ()
                {
                    //if (!ExampleManager.inst)
                    //	ExampleManager.Init();

                    if (ModCompatibility.mods["ExampleCompanion"].methods.ContainsKey("InitExample"))
                        ModCompatibility.mods["ExampleCompanion"].Invoke("InitExample", new object[] { });
                });
            }

            GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/Help/Help Dropdown/Join Discord/Text").GetComponent<Text>().text = "Modder's Discord";
            GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/Help/Help Dropdown/Watch Tutorials/Text").GetComponent<Text>().text = "Watch PA History";
            GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/Help/Help Dropdown/Community Guides").SetActive(false);
            GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/Help/Help Dropdown/Which songs can I use?").SetActive(false);
            GameObject.Find("TitleBar/File/File Dropdown/Save As").SetActive(true);
        }

        #endregion

        #region Saving / Loading

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

        // Change this to fullpath
        public IEnumerator LoadLevel(EditorManager __instance, string _levelName)
        {
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

            objEditor.CreateTimelineObjects();
            objEditor.RenderTimelineObjects();
            objEditor.SetCurrentObj(new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Object, 0));

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


            TriggerHelper.AddEventTrigger(timeIF.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(timeIF, 0.1f, 10f, false, new List<float> { 0f, AudioManager.inst.CurrentAudioSource.clip.length }) });

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

        public IEnumerator LoadThemes()
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

        public static int AutoSaveLimit => 6;
        public static float AutoSaveLoopTime => 600f;

        public static float timeSinceAutosaved;

        #endregion

        #region Functions

        #endregion

        #region Editor Properties

        public static List<EditorProperty> EditorProperties => new List<EditorProperty>()
        {
            new EditorProperty("", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.General, EditorPlugin.inst.Config.Bind("", "", false, ""), ""),
        };

        #endregion

        #region Constructors

        public class EditorProperty
        {
            public EditorProperty()
            {
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
