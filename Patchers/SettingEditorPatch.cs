using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using RTFunctions.Functions;

using EditorManagement.Functions.Editors;
using RTFunctions.Functions.Managers;
using RTFunctions.Functions.IO;
using EditorManagement.Functions.Helpers;

namespace EditorManagement.Patchers
{
    [HarmonyPatch(typeof(SettingEditor))]
    public class SettingEditorPatch
    {
        static SettingEditor Instance { get => SettingEditor.inst; set => SettingEditor.inst = value; }

        static Dictionary<string, Text> info = new Dictionary<string, Text>();
        static Image doggo;

        static Transform markerColorsContent;
        static Transform layerColorsContent;

        static GameObject colorPrefab;

        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        static void AwakePostfix()
        {
            //var transform = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog").transform;
            var transform = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog").transform;

            var slider = transform.Find("snap/bpm/slider").gameObject.GetComponent<Slider>();
            slider.maxValue = 999f;
            slider.minValue = 0f;

            try
            {
                var title1 = transform.Find("snap").GetChild(0).gameObject.Duplicate(transform);
                title1.transform.Find("title").GetComponent<Text>().text = "Editor Information";
            }
            catch
            {

            }

            info.Clear();
            var scrollView = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View").gameObject.Duplicate(transform, "Scroll View");
            scrollView.transform.AsRT().sizeDelta = new Vector2(765f, 120f);
            LSHelpers.DeleteChildren(scrollView.transform.Find("Viewport/Content"));

            string[] array = new string[]
            {
                "Object Count",
                "Event Count",
                "Theme Count",
                "Prefab External Count",
                "Prefab Internal Count",
                "Prefab Objects Count",
                "No Autokill Count",
                "Keyframe Offsets > Song Length Count",
                "Text Object Count",
                "Text Symbol Total Count",
                "Objects in Current Layer Count",
                "Markers Count",
                "Objects Alive Count",
                "Time in Editor",
                "Song Progress",
                "Camera Position",
                "Camera Zoom",
                "Camera Rotation",
            };

            for (int i = 0; i < array.Length; i++)
            {
                var baseInfo = new GameObject("Info");
                baseInfo.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
                baseInfo.transform.localScale = Vector3.one;
                var iRT = baseInfo.AddComponent<RectTransform>();
                iRT.sizeDelta = new Vector2(750f, 32f);
                var iImage = baseInfo.AddComponent<Image>();

                iImage.color = new Color(1f, 1f, 1f, 0.12f);

                var iHLG = baseInfo.AddComponent<HorizontalLayoutGroup>();

                var title = new GameObject("Title");
                title.transform.SetParent(iRT);
                title.transform.localScale = Vector3.one;
                title.AddComponent<RectTransform>();

                var titleText = title.AddComponent<Text>();
                titleText.font = FontManager.inst.Inconsolata;
                titleText.fontSize = 19;
                titleText.alignment = TextAnchor.MiddleLeft;
                titleText.text = "  " + array[i];

                var infoGO = new GameObject("Title");
                infoGO.transform.SetParent(iRT);
                infoGO.transform.localScale = Vector3.one;
                infoGO.AddComponent<RectTransform>();

                var infoText = infoGO.AddComponent<Text>();
                infoText.font = FontManager.inst.Inconsolata;
                infoText.fontSize = 19;
                infoText.alignment = TextAnchor.MiddleRight;
                infoText.text = "[ 0 ]";

                info.Add(array[i], infoText);
            }

            //Doggo
            var loadingDoggo = new GameObject("loading doggo");
            loadingDoggo.transform.parent = transform;
            var loadingDoggoRect = loadingDoggo.AddComponent<RectTransform>();
            loadingDoggo.AddComponent<CanvasRenderer>();
            doggo = loadingDoggo.AddComponent<Image>();
            var loadingDoggoLE = loadingDoggo.AddComponent<LayoutElement>();

            loadingDoggoRect.anchoredPosition = new Vector2(UnityEngine.Random.Range(-320f, 320f), UnityEngine.Random.Range(-300f, -275f));
            float sizeRandom = 64f * UnityEngine.Random.Range(0.5f, 1f);
            loadingDoggoRect.sizeDelta = new Vector2(sizeRandom, sizeRandom);

            loadingDoggoLE.ignoreLayout = true;

            try
            {
                var title2 = transform.Find("snap").GetChild(0).gameObject.Duplicate(transform);
                title2.transform.Find("title").GetComponent<Text>().text = "Marker Colors";
            }
            catch
            {

            }

            // Marker Colors
            {
                var markersScrollView = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View").gameObject.Duplicate(transform, "Scroll View");
                markersScrollView.transform.AsRT().sizeDelta = new Vector2(765f, 120f);
                LSHelpers.DeleteChildren(markersScrollView.transform.Find("Viewport/Content"));

                markerColorsContent = markersScrollView.transform.Find("Viewport/Content");
            }

            try
            {
                var title3 = transform.Find("snap").GetChild(0).gameObject.Duplicate(transform);
                title3.transform.Find("title").GetComponent<Text>().text = "Layer Colors";
            }
            catch
            {

            }

            // Layer Colors
            {
                var layersScrollView = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View").gameObject.Duplicate(transform, "Scroll View");
                layersScrollView.transform.AsRT().sizeDelta = new Vector2(765f, 120f);
                LSHelpers.DeleteChildren(layersScrollView.transform.Find("Viewport/Content"));

                layerColorsContent = layersScrollView.transform.Find("Viewport/Content");
            }

            Instance.StartCoroutine(Wait());
        }

        static IEnumerator Wait()
        {
            yield return new WaitForSeconds(0.4f);

            colorPrefab = new GameObject("Color");
            var tagPrefabRT = colorPrefab.AddComponent<RectTransform>();
            var tagPrefabImage = colorPrefab.AddComponent<Image>();
            tagPrefabImage.color = new Color(1f, 1f, 1f, 0.12f);
            var tagPrefabLayout = colorPrefab.AddComponent<HorizontalLayoutGroup>();
            tagPrefabLayout.childControlWidth = false;
            tagPrefabLayout.childForceExpandWidth = false;

            var input = RTEditor.inst.defaultIF.Duplicate(tagPrefabRT, "Input");
            input.transform.localScale = Vector3.one;
            ((RectTransform)input.transform).sizeDelta = new Vector2(136f, 32f);
            var text = input.transform.Find("Text").GetComponent<Text>();
            text.alignment = TextAnchor.MiddleLeft;
            text.fontSize = 17;

            var delete = EditorManager.inst.GetDialog("Quick Actions Popup").Dialog.Find("Panel/x").gameObject.Duplicate(tagPrefabRT, "Delete");
            ((RectTransform)delete.transform).sizeDelta = new Vector2(32f, 32f);
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void UpdatePostfix()
        {
            if (EditorManager.inst && EditorManager.inst.isEditing && EditorManager.inst.hasLoadedLevel)
            {
                var transform = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog").transform;

                if (info.ContainsKey("Object Count") && info["Object Count"])
                {
                    info["Object Count"].text = $"[ {DataManager.inst.gameData.beatmapObjects.Where(x => !x.fromPrefab).Count()} ]  ";
                }
                if (info.ContainsKey("Event Count") && info["Event Count"])
                {
                    int num = 0;
                    for (int i = 0; i < DataManager.inst.gameData.eventObjects.allEvents.Count; i++)
                        num += DataManager.inst.gameData.eventObjects.allEvents[i].Count;

                    info["Event Count"].text = $"[ {num} ]  ";
                }
                if (info.ContainsKey("Theme Count") && info["Theme Count"])
                {
                    info["Theme Count"].text = $"[ {DataManager.inst.AllThemes.Count} ]  ";
                }
                if (info.ContainsKey("Prefab External Count") && info["Prefab External Count"])
                {
                    info["Prefab External Count"].text = $"[ {PrefabEditor.inst.LoadedPrefabs.Count} ]  ";
                }
                if (info.ContainsKey("Prefab Internal Count") && info["Prefab Internal Count"])
                {
                    info["Prefab Internal Count"].text = $"[ {DataManager.inst.gameData.prefabs.Count} ]  ";
                }
                if (info.ContainsKey("Prefab Objects Count") && info["Prefab Objects Count"])
                {
                    info["Prefab Objects Count"].text = $"[ {DataManager.inst.gameData.prefabObjects.Count} ]  ";
                }
                if (info.ContainsKey("No Autokill Count") && info["No Autokill Count"])
                {
                    info["No Autokill Count"].text = $"[ {DataManager.inst.gameData.beatmapObjects.Where(x => x.autoKillType == DataManager.GameData.BeatmapObject.AutoKillType.OldStyleNoAutokill).Count()} ]  ";
                }
                if (info.ContainsKey("Keyframe Offsets > Song Length Count") && info["Keyframe Offsets > Song Length Count"])
                {
                    info["Keyframe Offsets > Song Length Count"].text = $"[ {DataManager.inst.gameData.beatmapObjects.Where(x => x.autoKillOffset > AudioManager.inst.CurrentAudioSource.clip.length).Count()} ]  ";
                }
                if (info.ContainsKey("Text Object Count") && info["Text Object Count"])
                {
                    info["Text Object Count"].text = $"[ {DataManager.inst.gameData.beatmapObjects.Where(x => x.shape == 4 && x.objectType != DataManager.GameData.BeatmapObject.ObjectType.Empty).Count()} ]  ";
                }
                if (info.ContainsKey("Text Symbol Total Count") && info["Text Symbol Total Count"])
                {
                    int num = 0;
                    foreach (var bm in DataManager.inst.gameData.beatmapObjects.Where(x => x.shape == 4 && x.objectType != DataManager.GameData.BeatmapObject.ObjectType.Empty))
                    {
                        num += bm.text.Length;
                    }

                    info["Text Symbol Total Count"].text = $"[ {num} ]  ";
                }
                if (info.ContainsKey("Objects in Current Layer Count") && info["Objects in Current Layer Count"])
                {
                    info["Objects in Current Layer Count"].text = $"[ {DataManager.inst.gameData.beatmapObjects.Where(x => x.editorData.layer == EditorManager.inst.layer).Count()} ]  ";
                }
                if (info.ContainsKey("Markers Count") && info["Markers Count"])
                {
                    info["Markers Count"].text = $"[ {DataManager.inst.gameData.beatmapData.markers.Count} ]  ";
                }
                if (info.ContainsKey("Objects Alive Count") && info["Objects Alive Count"])
                {
                    info["Objects Alive Count"].text = $"[ {DataManager.inst.gameData.beatmapObjects.Where(x => x.TimeWithinLifespan()).Count()} ]  ";
                }
                if (info.ContainsKey("Time in Editor") && info["Time in Editor"])
                {
                    info["Time in Editor"].text = $"[ {FontManager.TextTranslater.SecondsToTime(RTEditor.inst.timeEditing)} ]  ";
                }
                if (info.ContainsKey("Song Progress") && info["Song Progress"])
                {
                    info["Song Progress"].text = $"[ {FontManager.TextTranslater.Percentage(AudioManager.inst.CurrentAudioSource.time, AudioManager.inst.CurrentAudioSource.clip.length)}% ]  ";
                }

                if (info.ContainsKey("Camera Position") && info["Camera Position"])
                    info["Camera Position"].text = $"[ X: {Camera.main.transform.position.x}, Y: {Camera.main.transform.position.y} ]  ";
                
                if (info.ContainsKey("Camera Zoom") && info["Camera Zoom"])
                    info["Camera Zoom"].text = $"[ {Camera.main.orthographicSize} ]  ";
                
                if (info.ContainsKey("Camera Rotation") && info["Camera Rotation"])
                    info["Camera Rotation"].text = $"[ {Camera.main.transform.rotation.eulerAngles.z} ]  ";

                if (doggo)
                    doggo.sprite = EditorManager.inst.loadingImage.sprite;
            }
        }

        static void SetBPMSlider(Slider slider, InputField input)
        {
            slider.onValueChanged.RemoveAllListeners();
            slider.value = SettingEditor.inst.SnapBPM;
            slider.onValueChanged.AddListener(delegate (float _val)
            {
                DataManager.inst.metaData.song.BPM = _val;
                SettingEditor.inst.SnapBPM = _val;
                SetBPMInputField(slider, input);
                RTEditor.inst.SetTimelineGridSize();
            });
        }
        
        static void SetBPMInputField(Slider slider, InputField input)
        {
            input.onValueChanged.RemoveAllListeners();
            input.text = SettingEditor.inst.SnapBPM.ToString();
            input.onValueChanged.AddListener(delegate (string _val)
            {
                var bpm = Parser.TryParse(_val, 120f);
                DataManager.inst.metaData.song.BPM = bpm;
                SettingEditor.inst.SnapBPM = bpm;
                SetBPMSlider(slider, input);
                RTEditor.inst.SetTimelineGridSize();
            });
        }

        [HarmonyPatch("Render")]
        [HarmonyPrefix]
        static bool RenderPrefix()
        {
            EditorManager.inst.CancelInvoke("LoadingIconUpdate");
            EditorManager.inst.InvokeRepeating("LoadingIconUpdate", 0f, UnityEngine.Random.Range(0.01f, 0.4f));

            EditorManager.inst.ClearDialogs(Array.Empty<EditorManager.EditorDialog.DialogType>());
            EditorManager.inst.ShowDialog("Settings Editor");

            var transform = EditorManager.inst.GetDialog("Settings Editor").Dialog;
            var loadingDoggoRect = transform.Find("loading doggo").GetComponent<RectTransform>();

            loadingDoggoRect.anchoredPosition = new Vector2(UnityEngine.Random.Range(-320f, 320f), UnityEngine.Random.Range(-300f, -275f));
            float sizeRandom = 64 * UnityEngine.Random.Range(0.5f, 1f);
            loadingDoggoRect.sizeDelta = new Vector2(sizeRandom, sizeRandom);

            var toggle = transform.Find("snap/toggle/toggle").GetComponent<Toggle>();
            toggle.onValueChanged.RemoveAllListeners();
            toggle.isOn = SettingEditor.inst.SnapActive;
            toggle.onValueChanged.AddListener(delegate (bool _val)
            {
                SettingEditor.inst.SnapActive = _val;
            });

            var slider = transform.Find("snap/bpm/slider").GetComponent<Slider>();
            var input = transform.Find("snap/bpm/input").GetComponent<InputField>();
            SetBPMSlider(slider, input);
            SetBPMInputField(slider, input);

            TriggerHelper.IncreaseDecreaseButtons(input, t: transform.Find("snap/bpm"));
            TriggerHelper.AddEventTriggerParams(input.gameObject,
                TriggerHelper.ScrollDelta(input, 1f));

            RenderMarkerColors();
            RenderLayerColors();

            return false;
        }

        public static void RenderMarkerColors()
        {
            LSHelpers.DeleteChildren(markerColorsContent);

            var add = PrefabEditor.inst.CreatePrefab.Duplicate(markerColorsContent, "Add");

            ((RectTransform)add.transform).sizeDelta = new Vector2(402f, 32f);
            add.transform.Find("Text").GetComponent<Text>().text = "Add Marker Color";
            var addButton = add.GetComponent<Button>();
            addButton.onClick.ClearAll();
            addButton.onClick.AddListener(delegate ()
            {
                MarkerEditor.inst.markerColors.Add(LSColors.pink500);
                RTEditor.inst.SaveGlobalSettings();
                RenderMarkerColors();
            });

            int num = 0;
            foreach (var markerColor in MarkerEditor.inst.markerColors)
            {
                int index = num;

                var gameObject = colorPrefab.Duplicate(markerColorsContent, "Color");
                gameObject.transform.AsRT().sizeDelta = new Vector2(402f, 32f);
                var image = gameObject.GetComponent<Image>();
                image.color = markerColor;

                var input = gameObject.transform.Find("Input").GetComponent<InputField>();
                input.onValueChanged.ClearAll();
                input.onEndEdit.ClearAll();
                input.text = LSColors.ColorToHex(markerColor);
                input.onValueChanged.AddListener(delegate (string _val)
                {
                    MarkerEditor.inst.markerColors[index] = _val.Length == 6 ? LSColors.HexToColor(_val) : LSColors.pink500;
                    image.color = MarkerEditor.inst.markerColors[index];
                });
                input.onEndEdit.AddListener(delegate (string _val)
                {
                    RTEditor.inst.SaveGlobalSettings();
                });

                var delete = gameObject.transform.Find("Delete").GetComponent<Button>();
                delete.onClick.ClearAll();
                delete.onClick.AddListener(delegate ()
                {
                    MarkerEditor.inst.markerColors.RemoveAt(index);
                    RenderMarkerColors();
                    RTEditor.inst.SaveGlobalSettings();
                });

                num++;
            }
        }

        public static void RenderLayerColors()
        {
            LSHelpers.DeleteChildren(layerColorsContent);

            var add = PrefabEditor.inst.CreatePrefab.Duplicate(layerColorsContent, "Add");

            ((RectTransform)add.transform).sizeDelta = new Vector2(402f, 32f);
            add.transform.Find("Text").GetComponent<Text>().text = "Add Layer Color";
            var addButton = add.GetComponent<Button>();
            addButton.onClick.ClearAll();
            addButton.onClick.AddListener(delegate ()
            {
                EditorManager.inst.layerColors.Add(LSColors.pink500);
                RTEditor.inst.SaveGlobalSettings();
                RenderLayerColors();
            });

            int num = 0;
            foreach (var layerColor in EditorManager.inst.layerColors)
            {
                int index = num;

                var gameObject = colorPrefab.Duplicate(layerColorsContent, "Color");
                gameObject.transform.AsRT().sizeDelta = new Vector2(402f, 32f);
                var image = gameObject.GetComponent<Image>();
                image.color = layerColor;

                var input = gameObject.transform.Find("Input").GetComponent<InputField>();
                input.onValueChanged.ClearAll();
                input.onEndEdit.ClearAll();
                input.text = LSColors.ColorToHex(layerColor);
                input.onValueChanged.AddListener(delegate (string _val)
                {
                    EditorManager.inst.layerColors[index] = _val.Length == 6 ? LSColors.HexToColor(_val) : LSColors.pink500;
                    image.color = EditorManager.inst.layerColors[index];
                });
                input.onEndEdit.AddListener(delegate (string _val)
                {
                    RTEditor.inst.SaveGlobalSettings();
                });

                var delete = gameObject.transform.Find("Delete").GetComponent<Button>();
                delete.onClick.ClearAll();
                delete.onClick.AddListener(delegate ()
                {
                    EditorManager.inst.layerColors.RemoveAt(index);
                    RenderLayerColors();
                    RTEditor.inst.SaveGlobalSettings();
                });

                num++;
            }
        }
    }
}
