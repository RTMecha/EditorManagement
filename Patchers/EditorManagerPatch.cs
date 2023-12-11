﻿using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using HarmonyLib;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using LSFunctions;
using SimpleJSON;
using Crosstales.FB;
using CielaSpike;

using RTFunctions.Functions.IO;
using RTFunctions.Functions.Data;
using RTFunctions.Functions.Managers;
using RTFunctions.Functions.Optimization;
using RTFunctions.Patchers;

using EditorManagement.Functions;
using EditorManagement.Functions.Editors;

using BaseBeatmapObject = DataManager.GameData.BeatmapObject;
using EventKeyframe = DataManager.GameData.EventKeyframe;
using Prefab = DataManager.GameData.Prefab;
using PrefabObject = DataManager.GameData.PrefabObject;
using BackgroundObject = DataManager.GameData.BackgroundObject;

using ObjectType = DataManager.GameData.BeatmapObject.ObjectType;
using AutoKillType = DataManager.GameData.BeatmapObject.AutoKillType;

using ObjectSelection = ObjEditor.ObjectSelection;
using ObjectKeyframeSelection = ObjEditor.KeyframeSelection;
using EventKeyframeSelection = EventEditor.KeyframeSelection;

using DGEase = DG.Tweening.Ease;

namespace EditorManagement.Patchers
{
    [HarmonyPatch(typeof(EditorManager))]
    public class EditorManagerPatch : MonoBehaviour
    {
        static EditorManager Instance { get => EditorManager.inst; set => EditorManager.inst = value; }

        [HarmonyPatch("Awake")]
        [HarmonyPrefix]
        static bool AwakePrefix(EditorManager __instance)
        {
            if (!Instance)
                Instance = __instance;
            else if (Instance != __instance)
                Destroy(__instance.gameObject);

            FontManager.inst.ChangeAllFontsInEditor();

            //OG Code
            {
                InputDataManager.inst.BindMenuKeys();
                InputDataManager.inst.BindEditorKeys();
                __instance.ScreenScale = Screen.width / 1920f;
                __instance.ScreenScaleInverse = 1f / __instance.ScreenScale;
                __instance.curveDictionary.Add(0, DGEase.Linear);
                __instance.curveDictionary.Add(1, DGEase.InSine);
                __instance.curveDictionary.Add(2, DGEase.OutSine);
                __instance.curveDictionary.Add(3, DGEase.InOutSine);
                __instance.curveDictionary.Add(4, DGEase.InElastic);
                __instance.curveDictionary.Add(5, DGEase.OutElastic);
                __instance.curveDictionary.Add(6, DGEase.InOutElastic);
                __instance.curveDictionary.Add(7, DGEase.InBack);
                __instance.curveDictionary.Add(8, DGEase.OutBack);
                __instance.curveDictionary.Add(9, DGEase.InOutBack);
                __instance.curveDictionary.Add(10, DGEase.InBounce);
                __instance.curveDictionary.Add(11, DGEase.OutBounce);
                __instance.curveDictionary.Add(12, DGEase.InOutBounce);
                __instance.curveDictionaryBack.Add(DGEase.Linear, 0);
                __instance.curveDictionaryBack.Add(DGEase.InSine, 1);
                __instance.curveDictionaryBack.Add(DGEase.OutSine, 2);
                __instance.curveDictionaryBack.Add(DGEase.InOutSine, 3);
                __instance.curveDictionaryBack.Add(DGEase.InElastic, 4);
                __instance.curveDictionaryBack.Add(DGEase.OutElastic, 5);
                __instance.curveDictionaryBack.Add(DGEase.InOutElastic, 6);
                __instance.curveDictionaryBack.Add(DGEase.InBack, 7);
                __instance.curveDictionaryBack.Add(DGEase.OutBack, 8);
                __instance.curveDictionaryBack.Add(DGEase.InOutBack, 9);
                __instance.curveDictionaryBack.Add(DGEase.InBounce, 10);
                __instance.curveDictionaryBack.Add(DGEase.OutBounce, 11);
                __instance.curveDictionaryBack.Add(DGEase.InOutBounce, 12);
                __instance.RefreshDialogDictionary();

                var list = (from x in Resources.FindObjectsOfTypeAll<Dropdown>()
                            where x.gameObject != null && x.gameObject.name == "curves"
                            select x).ToList();

                //List<GameObject> list = (from obj in Resources.FindObjectsOfTypeAll<GameObject>()
                //						 where obj.name == "curves" && obj.GetComponent<Dropdown>() != null
                //						 select obj).ToList<GameObject>();

                List<Dropdown.OptionData> list2 = new List<Dropdown.OptionData>();
                foreach (var curveOption in __instance.CurveOptions)
                {
                    list2.Add(new Dropdown.OptionData(curveOption.name, curveOption.icon));
                }
                foreach (var gameObject in list)
                {
                    gameObject.ClearOptions();
                    gameObject.AddOptions(list2);
                }
                EventTrigger.Entry entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.BeginDrag;
                entry.callback.AddListener(delegate (BaseEventData eventData)
                {
                    PointerEventData pointerEventData = (PointerEventData)eventData;
                    __instance.SelectionBoxImage.gameObject.SetActive(true);
                    __instance.DragStartPos = pointerEventData.position * __instance.ScreenScaleInverse;
                    __instance.SelectionRect = default(Rect);
                });
                EventTrigger.Entry entry2 = new EventTrigger.Entry();
                entry2.eventID = EventTriggerType.Drag;
                entry2.callback.AddListener(delegate (BaseEventData eventData)
                {
                    Vector3 vector = ((PointerEventData)eventData).position * __instance.ScreenScaleInverse;
                    if (vector.x < __instance.DragStartPos.x)
                    {
                        __instance.SelectionRect.xMin = vector.x;
                        __instance.SelectionRect.xMax = __instance.DragStartPos.x;
                    }
                    else
                    {
                        __instance.SelectionRect.xMin = __instance.DragStartPos.x;
                        __instance.SelectionRect.xMax = vector.x;
                    }
                    if (vector.y < __instance.DragStartPos.y)
                    {
                        __instance.SelectionRect.yMin = vector.y;
                        __instance.SelectionRect.yMax = __instance.DragStartPos.y;
                    }
                    else
                    {
                        __instance.SelectionRect.yMin = __instance.DragStartPos.y;
                        __instance.SelectionRect.yMax = vector.y;
                    }
                    __instance.SelectionBoxImage.rectTransform.offsetMin = __instance.SelectionRect.min;
                    __instance.SelectionBoxImage.rectTransform.offsetMax = __instance.SelectionRect.max;
                });

                var timelineET = __instance.timeline.GetComponent<EventTrigger>();
                timelineET.triggers.Add(entry);
                timelineET.triggers.Add(entry2);
            }

            RTEditor.Init(__instance);
            KeybindManager.Init(__instance);

            ThemeEditorManager.Init(ThemeEditor.inst);
            EditorThemeManager.Init();

            return false;
        }

        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        static bool StartPrefix()
        {
            Instance.GetLevelList();
            DiscordController.inst.OnIconChange("editor");
            DiscordController.inst.OnDetailsChange("In Editor");
            DiscordController.inst.OnStateChange("");
            Instance.SetDialogStatus("Timeline", true, true);
            InputDataManager.inst.players.Clear();
            InputDataManager.inst.players.Add(new InputDataManager.CustomPlayer(true, 0, null));
            Instance.GUI.SetActive(false);
            Instance.canEdit = DataManager.inst.GetSettingBool("CanEdit", false);
            if (Instance.canEdit)
            {
                Instance.isEditing = true;
                Instance.SetCreatorName(SteamWrapper.inst.user.displayName);
                Instance.SetGridScale(1);
                Instance.SetShowGrid(true);
                Instance.showTooltip = false;
                Instance.SetTooltipDisappear(0f);
                Instance.SetShowHelp(false);
                Instance.ToggleShowHelp();
                Instance.UpdateTooltip();
                Instance.tooltip.transform.parent.gameObject.SetActive(false);
                Instance.Zoom = 0.05f;
                Instance.SetLayer(0);
                Instance.SetDifficulty(0);
                Instance.firstOpened = false;
            }
            LoadBaseLevelPrefix();
            Instance.DisplayNotification("Base Level Loaded", 2f, EditorManager.NotificationType.Info);

            EditorPlugin.timeOffset = Time.time;

            InputDataManager.inst.editorActions.Cut.ClearBindings();
            InputDataManager.inst.editorActions.Copy.ClearBindings();
            InputDataManager.inst.editorActions.Paste.ClearBindings();
            InputDataManager.inst.editorActions.Duplicate.ClearBindings();
            InputDataManager.inst.editorActions.Delete.ClearBindings();
            InputDataManager.inst.editorActions.Undo.ClearBindings();
            InputDataManager.inst.editorActions.Redo.ClearBindings();

            EditorManager.inst.notification.transform.Find("info").gameObject.SetActive(true);

            return false;
        }

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static bool UpdatePrefix()
        {
            Instance.ScreenScale = Screen.width / 1920f;
            Instance.ScreenScaleInverse = 1f / Instance.ScreenScale;

            //if (Instance.showHelp)
            //{
            //float num = (float)Screen.width / 1920f;
            //num = 1f / num;
            //float x = Instance.mouseTooltip.GetComponent<RectTransform>().sizeDelta.x;
            //float y = Instance.mouseTooltip.GetComponent<RectTransform>().sizeDelta.y;
            //Vector3 zero = Vector3.zero;
            //if ((Input.mousePosition.x + x + 32f) * num >= 1920f)
            //{
            //	zero.x -= x;
            //}
            //if ((Input.mousePosition.y + y + 32f) * num >= 1080f)
            //{
            //	zero.y -= y;
            //}
            //Instance.mouseTooltip.GetComponent<RectTransform>().anchoredPosition = (Input.mousePosition + zero) * num;
            //}

            if (GameManager.inst.gameState == GameManager.State.Playing)
            {
                if (Instance.canEdit)
                {
                    if (InputDataManager.inst.editorActions.ToggleEditor.WasPressed && !Instance.IsUsingInputField())
                        Instance.ToggleEditor();

                    foreach (var customPlayer in InputDataManager.inst.players)
                    {
                        if (customPlayer.player && customPlayer.player.Actions.Pause.WasPressed && !Instance.isEditing)
                        {
                            Instance.isEditing = true;
                        }
                    }

                    if (Instance.isEditing)
                    {
                        if (!Instance.IsUsingInputField())
                        {
                            // Probably replace this with new KeybindManager system

                            Instance.handleViewShortcuts();
                            //if (InputDataManager.inst.editorActions.OpenLevel.WasPressed)
                            //    Instance.OpenBeatmapPopup();
                            //if (InputDataManager.inst.editorActions.SaveLevel.WasPressed)
                            //    Instance.SaveBeatmap();
                            //if (InputDataManager.inst.editorActions.Cut.WasPressed)
                            //    Instance.Cut();
                            //if (InputDataManager.inst.editorActions.Copy.WasPressed)
                            //    Instance.Copy();
                            //if (InputDataManager.inst.editorActions.Duplicate.WasPressed)
                            //    Instance.Duplicate();
                            //if (InputDataManager.inst.editorActions.Paste.WasPressed)
                            //    Instance.Paste();
                            //if (InputDataManager.inst.editorActions.Delete.WasPressed)
                            //    Instance.Delete();
                        }
                    }
                    if (!Instance.firstOpened)
                    {
                        try
                        {
                            Instance.AssignWaveformTextures();
                            Instance.UpdateTimelineSizes();
                            Instance.firstOpened = true;

                            // If the NullReference is in this area, then it's somewhere below.

                            Debug.Log($"{Instance.className}First opened: Create Timeline Objects");
                            ObjectEditor.inst.CreateTimelineObjects();
                            Debug.Log($"{Instance.className}First opened: Create Event Objects");
                            RTEventEditor.inst.CreateEventObjects();
                            Debug.Log($"{Instance.className}First opened: Create Ghost Checkpoints");
                            CheckpointEditor.inst.CreateGhostCheckpoints();
                            Debug.Log($"{Instance.className}First opened: Update Timeline");
                            GameManager.inst.UpdateTimeline();
                            if (RTEditor.inst.TimelineBeatmapObjects.Count > 0)
                            {
                                Debug.Log($"{Instance.className}First opened: Set Current Object");
                                ObjectEditor.inst.SetCurrentObject(RTEditor.inst.TimelineBeatmapObjects[0], openDialog: false);
                            }
                            Debug.Log($"{Instance.className}First opened: Set Current Checkpoint");
                            CheckpointEditor.inst.SetCurrentCheckpoint(0);
                            Debug.Log($"{Instance.className}First opened: Toggle Playing Song");
                            Instance.TogglePlayingSong();
                            Debug.Log($"{Instance.className}First opened: Clear Dialogs");
                            Instance.ClearDialogs(Array.Empty<EditorManager.EditorDialog.DialogType>());
                            Debug.Log($"{Instance.className}First opened: Open Beatmap / New Level Popup");
                            if (Instance.loadedLevels.Count > 0)
                                Instance.OpenBeatmapPopup();
                            else
                                Instance.OpenNewLevelPopup();
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"{Instance.className}First opened error!{ex}");
                        }
                    }

                    if (Instance.OpenedEditor)
                    {
                        GameManager.inst.ResetCheckpoints(true);
                        GameManager.inst.playerGUI.SetActive(false);
                        LSHelpers.ShowCursor();
                        Instance.GUI.SetActive(true);
                        Instance.ShowGUI();
                        Instance.SetPlayersInvinsible(true);
                        Instance.SetEditRenderArea();
                        GameManager.inst.UpdateTimeline();
                    }
                    else if (Instance.ClosedEditor)
                    {
                        GameManager.inst.playerGUI.SetActive(true);
                        LSHelpers.HideCursor();
                        Instance.GUI.SetActive(false);
                        AudioManager.inst.CurrentAudioSource.Play();
                        Instance.SetNormalRenderArea();
                        GameManager.inst.UpdateTimeline();
                    }

                    Instance.updatePointer();
                    Instance.UpdateTooltip();
                    Instance.UpdateEditButtons();

                    if (RTEditor.inst.timelineTime)
                        RTEditor.inst.timelineTime.text = string.Format("{0:0}:{1:00}.{2:000}",
                            Mathf.Floor(Instance.CurrentAudioPos / 60f),
                            Mathf.Floor(Instance.CurrentAudioPos % 60f),
                            Mathf.Floor(AudioManager.inst.CurrentAudioSource.time * 1000f % 1000f));

                    Instance.wasEditing = Instance.isEditing;
                }
                else if (!Instance.canEdit && Instance.isEditing)
                {
                    Instance.GUI.SetActive(false);
                    AudioManager.inst.SetPitch(1f);
                    Instance.SetNormalRenderArea();
                    Instance.isEditing = false;
                }
            }


            if (EditorManager.inst.GUI.activeSelf == true && EditorManager.inst.isEditing == true)
            {
                // Create Local Variables
                if (RTEditor.inst.timeIF && !RTEditor.inst.timeIF.isFocused)
                    RTEditor.inst.timeIF.text = AudioManager.inst.CurrentAudioSource.time.ToString();

                if (RTEditor.inst.pitchIF && !RTEditor.inst.pitchIF.isFocused)
                    RTEditor.inst.pitchIF.text = ModCompatibility.sharedFunctions.ContainsKey("EventsCorePitchOffset") ?
                        ((float)ModCompatibility.sharedFunctions["EventsCorePitchOffset"]).ToString() : AudioManager.inst.pitch.ToString();

                if (RTEditor.inst.doggoImage)
                    RTEditor.inst.doggoImage.sprite = EditorManager.inst.loadingImage.sprite;
            }

            var multi = EditorManager.inst.GetDialog("Multi Object Editor").Dialog;
            if (multi.gameObject.activeSelf && ((RectTransform)multi.Find("data")).sizeDelta != new Vector2(810f, 730.11f))
            {
                ((RectTransform)multi.Find("data")).sizeDelta = new Vector2(810f, 730.11f);
            }
            if (multi.gameObject.activeSelf && ((RectTransform)multi.Find("data/left")).sizeDelta != new Vector2(355f, 730f))
            {
                ((RectTransform)multi.Find("data/left")).sizeDelta = new Vector2(355f, 730f);
            }

            if (!LSHelpers.IsUsingInputField() && Input.GetMouseButtonDown(2))
            {
                EditorPlugin.ListObjectLayers();
            }

            EditorPlugin.timeEditing = EditorPlugin.timeOffset + Time.time;

            Instance.prevAudioTime = AudioManager.inst.CurrentAudioSource.time;
            return false;
        }

        [HarmonyPatch("OpenGuides")]
        [HarmonyPrefix]
        static bool OpenGuidesPrefix()
        {
            Application.OpenURL("https://steamcommunity.com/app/440310/guides");
            Instance.DisplayNotification("Guides Link will open in your browser!", 2f, EditorManager.NotificationType.Success);
            return false;
        }

        [HarmonyPatch("OpenSteamWorkshop")]
        [HarmonyPrefix]
        static bool OpenSteamWorkshopPrefix()
        {
            Application.OpenURL("https://steamcommunity.com/workshop/browse/?appid=440310&requiredtags[]=level");
            Instance.DisplayNotification("Steam will open in your browser!", 2f, EditorManager.NotificationType.Success);
            return false;
        }

        [HarmonyPatch("OpenDiscord")]
        [HarmonyPrefix]
        static bool OpenDiscordPrefix()
        {
            Application.OpenURL("https://discord.gg/KrGrpBwYgs");
            Instance.DisplayNotification("Modders' Discord will open in your browser!", 2f, EditorManager.NotificationType.Success);
            return false;
        }

        [HarmonyPatch("OpenTutorials")]
        [HarmonyPrefix]
        static bool OpenTutorialsPrefix()
        {
            Application.OpenURL("https://www.youtube.com/playlist?list=PLMHuUok_ojlWH_UZ60tHZIRMWJTDyhRaO");
            Instance.DisplayNotification("PA History playlist will open in your browser!", 2f, EditorManager.NotificationType.Success);
            return false;
        }

        [HarmonyPatch("OpenVerifiedSongs")]
        [HarmonyPrefix]
        static bool OpenVerifiedSongsPrefix()
        {
            Application.OpenURL("https://www.youtube.com/playlist?list=PLMHuUok_ojlWH_UZ60tHZIRMWJTDyhRaO");
            Instance.DisplayNotification("PA History playlist will open in your browser!", 2f, EditorManager.NotificationType.Success);
            return false;
        }

        [HarmonyPatch("SnapToBPM")]
        [HarmonyPrefix]
        static bool SnapToBPMPrefix(ref float __result, float __0)
        {
            __result = RTEditor.SnapToBPM(__0);
            return false;
        }

        [HarmonyPatch("SetLayer")]
        [HarmonyPrefix]
        static bool SetLayerPrefix(int __0)
        {
            RTEditor.inst.SetLayer(__0);
            return false;
        }

        [HarmonyPatch("GetLevelList")]
        [HarmonyPrefix]
        static bool GetLevelListPrefix()
        {
            Instance.StartCoroutine(RTEditor.inst.LoadLevels());
            return false;
        }

        [HarmonyPatch("LoadBaseLevel")]
        [HarmonyPrefix]
        static bool LoadBaseLevelPrefix()
        {
            GameManager.inst.ResetCheckpoints(true);
            DataManager.inst.gameData = RTEditor.inst.CreateBaseBeatmap();
            AudioManager.inst.PlayMusic(null, Instance.baseSong, true, 0f);
            GameManager.inst.gameState = GameManager.State.Playing;
            return false;
        }

        [HarmonyPatch("SetFileInfoPopupText")]
        [HarmonyPrefix]
        static bool SetFileInfoPopupTextPrefix(string __0)
        {
            if (RTEditor.inst.fileInfoText)
                RTEditor.inst.fileInfoText.text = __0;
            return false;
        }

        [HarmonyPatch("LoadLevel")]
        [HarmonyPrefix]
        static bool LoadLevelPrefix(EditorManager __instance, ref IEnumerator __result, string __0)
        {
            __result = RTEditor.inst.LoadLevel(__instance, $"{RTFile.ApplicationDirectory}{RTEditor.editorListSlash}{__0}");
            return false;
        }

        [HarmonyPatch("DisplayNotification")]
        [HarmonyPrefix]
        static bool DisplayNotificationPrefix(string __0, float __1, EditorManager.NotificationType __2 = EditorManager.NotificationType.Info, bool __3 = false)
        {
            RTEditor.inst.DisplayNotification(__0, __0, __1, __2);
            return false;
        }

        [HarmonyPatch("Copy")]
        [HarmonyPrefix]
        static bool CopyPrefix(bool _cut = false, bool _dup = false)
        {
            RTEditor.inst.Copy(_cut, _dup);

            return false;
        }

        [HarmonyPatch("Paste")]
        [HarmonyPrefix]
        static bool PastePrefix(float __0)
        {
            RTEditor.inst.Paste(__0);

            return false;
        }

        [HarmonyPatch("Delete")]
        [HarmonyPrefix]
        static bool DeletePrefix()
        {
            RTEditor.inst.Delete();

            return false;
        }

        [HarmonyPatch("handleViewShortcuts")]
        [HarmonyPrefix]
        static bool handleViewShortcutsPrefix()
        {
            if (EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Object)
                && EditorManager.inst.IsOverObjTimeline
                && !LSHelpers.IsUsingInputField()
                && !RTEditor.inst.isOverMainTimeline && (!ModCompatibility.sharedFunctions.ContainsKey("EventsCoreEditorOffset") || !(bool)ModCompatibility.sharedFunctions["EventsCoreEditorOffset"]))
            {
                float multiply = 1f;
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    multiply = 2f;
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    multiply = 0.1f;

                if (InputDataManager.inst.editorActions.ZoomIn.WasPressed)
                    ObjEditor.inst.Zoom = ObjEditor.inst.zoomFloat + RTEditor.GetEditorProperty("Keyframe Zoom Amount").GetConfigEntry<float>().Value * multiply;
                if (InputDataManager.inst.editorActions.ZoomOut.WasPressed)
                    ObjEditor.inst.Zoom = ObjEditor.inst.zoomFloat - RTEditor.GetEditorProperty("Keyframe Zoom Amount").GetConfigEntry<float>().Value * multiply;
            }

            if (!EditorManager.inst.IsOverObjTimeline && RTEditor.inst.isOverMainTimeline && (!ModCompatibility.sharedFunctions.ContainsKey("EventsCoreEditorOffset") || !(bool)ModCompatibility.sharedFunctions["EventsCoreEditorOffset"]))
            {
                float multiply = 1f;
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    multiply = 2f;
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    multiply = 0.1f;

                if (InputDataManager.inst.editorActions.ZoomIn.WasPressed)
                    EditorManager.inst.Zoom = EditorManager.inst.zoomFloat + RTEditor.GetEditorProperty("Main Zoom Amount").GetConfigEntry<float>().Value * multiply;
                if (InputDataManager.inst.editorActions.ZoomOut.WasPressed)
                    EditorManager.inst.Zoom = EditorManager.inst.zoomFloat - RTEditor.GetEditorProperty("Main Zoom Amount").GetConfigEntry<float>().Value * multiply;
            }
            //if (InputDataManager.inst.editorActions.ShowHelp.WasPressed)
            //    EditorManager.inst.SetShowHelp(!EditorManager.inst.showHelp);

            return false;
        }

        [HarmonyPatch("updatePointer")]
        [HarmonyPrefix]
        static bool updatePointerPrefix()
        {
            Vector2 point = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            Rect rect = new Rect(0f, 0.305f * (float)Screen.height, (float)Screen.width, (float)Screen.height * 0.025f);
            if (EditorManager.inst.updateAudioTime && Input.GetMouseButtonUp(0) && rect.Contains(point))
            {
                AudioManager.inst.CurrentAudioSource.time = EditorManager.inst.audioTimeForSlider / EditorManager.inst.Zoom;
                EditorManager.inst.updateAudioTime = false;
            }
            if (Input.GetMouseButton(0) && rect.Contains(point))
            {
                var slider = EditorManager.inst.timelineSlider.GetComponent<Slider>();
                slider.minValue = 0f;
                slider.maxValue = AudioManager.inst.CurrentAudioSource.clip.length * EditorManager.inst.Zoom;
                EditorManager.inst.audioTimeForSlider = EditorManager.inst.timelineSlider.GetComponent<Slider>().value;
                EditorManager.inst.updateAudioTime = true;
                EditorManager.inst.wasDraggingPointer = true;
                if (Mathf.Abs(EditorManager.inst.audioTimeForSlider / EditorManager.inst.Zoom - EditorManager.inst.prevAudioTime) < 2f)
                {
                    if (RTEditor.GetEditorProperty("Dragging main Cursor Pauses Level").GetConfigEntry<bool>().Value)
                    {
                        AudioManager.inst.CurrentAudioSource.Pause();
                        EditorManager.inst.UpdatePlayButton();
                    }
                    AudioManager.inst.CurrentAudioSource.time = EditorManager.inst.audioTimeForSlider / EditorManager.inst.Zoom;
                }
            }
            else if (EditorManager.inst.updateAudioTime && EditorManager.inst.wasDraggingPointer && !rect.Contains(point))
            {
                AudioManager.inst.CurrentAudioSource.time = EditorManager.inst.audioTimeForSlider / EditorManager.inst.Zoom;
                EditorManager.inst.updateAudioTime = false;
                EditorManager.inst.wasDraggingPointer = false;
            }
            else
            {
                var slider = EditorManager.inst.timelineSlider.GetComponent<Slider>();
                slider.minValue = 0f;
                slider.maxValue = AudioManager.inst.CurrentAudioSource.clip.length * EditorManager.inst.Zoom;
                slider.value = AudioManager.inst.CurrentAudioSource.time * EditorManager.inst.Zoom;
                EditorManager.inst.audioTimeForSlider = AudioManager.inst.CurrentAudioSource.time * EditorManager.inst.Zoom;
            }
            EditorManager.inst.timelineSlider.GetComponent<RectTransform>().sizeDelta = new Vector2(AudioManager.inst.CurrentAudioSource.clip.length * EditorManager.inst.Zoom, 25f);
            return false;
        }

        [HarmonyPatch("AddToPitch")]
        [HarmonyPrefix]
        static bool AddToPitchPrefix(EditorManager __instance, float __0)
        {
            if (Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftAlt))
            {
                AudioManager.inst.SetPitch(AudioManager.inst.pitch + __0 * 10f);
            }
            if (Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.LeftControl))
            {
                AudioManager.inst.SetPitch(AudioManager.inst.pitch + __0 / 10f);
            }
            if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftAlt))
            {
                AudioManager.inst.SetPitch(AudioManager.inst.pitch + __0);
            }

            return false;
        }

        [HarmonyPatch("ToggleEditor")]
        [HarmonyPostfix]
        static void ToggleEditorPrefix()
        {
            if (EditorManager.inst.isEditing)
            {
                EditorManager.inst.UpdatePlayButton();
            }
            GameManager.inst.ResetCheckpoints();
        }

        [HarmonyPatch("CloseOpenBeatmapPopup")]
        [HarmonyPrefix]
        static bool CloseOpenBeatmapPopupPrefix(EditorManager __instance)
        {
            if (EditorManager.inst.hasLoadedLevel)
            {
                __instance.HideDialog("Open File Popup");
            }
            else
            {
                EditorManager.inst.DisplayNotification("Please select a level first!", 2f, EditorManager.NotificationType.Error);
            }
            return false;
        }

        static void SaveBeatmapPrefix()
        {
            string str = "beatmaps/" + RTEditor.EditorPath + "/" + EditorManager.inst.currentLoadedLevel;
            if (RTFile.FileExists(RTFile.ApplicationDirectory + str + "/level-previous.lsb"))
            {
                File.Delete(RTFile.ApplicationDirectory + str + "/level-previous.lsb");
            }

            if (RTFile.FileExists(RTFile.ApplicationDirectory + str + "/level.lsb"))
                File.Copy(RTFile.ApplicationDirectory + str + "/level.lsb", RTFile.ApplicationDirectory + str + "/level-previous.lsb");
        }
        
        static void EditorSaveBeatmapPatch()
        {
            DataManager.inst.gameData.beatmapData.editorData.timelinePos = AudioManager.inst.CurrentAudioSource.time;
            DataManager.inst.metaData.song.BPM = SettingEditor.inst.SnapBPM;
            DataManager.inst.gameData.beatmapData.levelData.backgroundColor = EditorManager.inst.layer;
            //EditorPlugin.scrollBar = GameObject.Find("Editor Systems/Editor GUI/sizer/main/whole-timeline/Scrollbar").GetComponent<Scrollbar>().value;

            Sprite waveform = EditorManager.inst.timeline.GetComponent<Image>().sprite;
            if (RTEditor.GetEditorProperty("Waveform Mode").GetConfigEntry<WaveformType>().Value == WaveformType.Legacy &&
                RTEditor.GetEditorProperty("Waveform Generate").GetConfigEntry<bool>().Value)
            {
                File.WriteAllBytes(RTFile.ApplicationDirectory + GameManager.inst.basePath + "waveform.png", EditorManager.inst.timeline.GetComponent<Image>().sprite.texture.EncodeToPNG());
            }
            if (RTEditor.GetEditorProperty("Waveform Mode").GetConfigEntry<WaveformType>().Value == WaveformType.Beta && RTEditor.GetEditorProperty("Waveform Generate").GetConfigEntry<bool>().Value)
            {
                File.WriteAllBytes(RTFile.ApplicationDirectory + GameManager.inst.basePath + "waveform_old.png", EditorManager.inst.timeline.GetComponent<Image>().sprite.texture.EncodeToPNG());
            }

            // Reimplement this layer
            //if (RTFile.FileExists(RTFile.ApplicationDirectory + "beatmaps/" + RTEditor.editorListSlash + EditorManager.inst.currentLoadedLevel + "/editor.lse"))
            //{
            //    string rawProfileJSON = null;
            //    rawProfileJSON = FileManager.inst.LoadJSONFile("beatmaps/" + RTEditor.editorListSlash + EditorManager.inst.currentLoadedLevel + "/editor.lse");

            //    var jsonnode = JSON.Parse(rawProfileJSON);

            //    jsonnode["timeline"]["tsc"] = GameObject.Find("Editor Systems/Editor GUI/sizer/main/whole-timeline/Scrollbar").GetComponent<Scrollbar>().value.ToString("f2");
            //    jsonnode["timeline"]["z"] = EditorManager.inst.zoomFloat.ToString("f3");
            //    jsonnode["timeline"]["l"] = EditorManager.inst.layer.ToString();
            //    jsonnode["editor"]["t"] = EditorPlugin.itsTheTime.ToString();
            //    jsonnode["editor"]["a"] = EditorPlugin.openAmount.ToString();
            //    jsonnode["misc"]["sn"] = SettingEditor.inst.SnapActive.ToString();

            //    RTFile.WriteToFile("beatmaps/" + EditorPlugin.editorPath + "/" + EditorManager.inst.currentLoadedLevel + "/editor.lse", jsonnode.ToString(3));
            //}
            //else
            //{
            //    var jsonnode = JSON.Parse("{}");

            //    jsonnode["timeline"]["tsc"] = GameObject.Find("Editor Systems/Editor GUI/sizer/main/whole-timeline/Scrollbar").GetComponent<Scrollbar>().value.ToString("f2");
            //    jsonnode["timeline"]["z"] = EditorManager.inst.zoomFloat.ToString("f3");
            //    jsonnode["timeline"]["l"] = EditorManager.inst.layer.ToString();
            //    jsonnode["editor"]["t"] = EditorPlugin.itsTheTime.ToString();
            //    jsonnode["editor"]["a"] = EditorPlugin.openAmount.ToString();
            //    jsonnode["misc"]["sn"] = SettingEditor.inst.SnapActive.ToString();

            //    RTFile.WriteToFile("beatmaps/" + RTEditor.editorListSlash + "/" + EditorManager.inst.currentLoadedLevel + "/editor.lse", jsonnode.ToString(3));
            //}
        }

        [HarmonyPatch("SaveBeatmap")]
        [HarmonyPrefix]
        static bool SaveBeatmapPrefix(EditorManager __instance)
        {
            if (!__instance.hasLoadedLevel)
            {
                __instance.DisplayNotification("Beatmap Can't be saved till you load a level.", 5f, EditorManager.NotificationType.Error);
                return false;
            }
            if (__instance.savingBeatmap)
            {
                __instance.DisplayNotification("Already attempting to save beatmap already, please wait!", 2f, EditorManager.NotificationType.Error);
                return false;
            }

            if (RTFile.FileExists(GameManager.inst.basePath + "level-previous.lsb"))
                File.Delete(GameManager.inst.basePath + "level-previous.lsb");

            if (RTFile.FileExists(GameManager.inst.basePath + "level.lsb"))
                File.Copy(GameManager.inst.basePath + "level.lsb", GameManager.inst.basePath + "level-previous.lsb");

            DataManager.inst.SaveMetadata(GameManager.inst.basePath + "metadata.lsb");
            __instance.StartCoroutine(SaveData(GameManager.inst.path));

            var jn = JSON.Parse(RTFile.FileExists(GameManager.inst.basePath + "editor.lse") ? FileManager.inst.LoadJSONFileRaw(GameManager.inst.basePath + "editor.lse") : "{}");

            jn["timeline"]["tsc"] = GameObject.Find("Editor Systems/Editor GUI/sizer/main/whole-timeline/Scrollbar").GetComponent<Scrollbar>().value.ToString("f2");
            jn["timeline"]["z"] = EditorManager.inst.zoomFloat.ToString("f3");
            jn["timeline"]["l"] = EditorManager.inst.layer.ToString();
            jn["editor"]["t"] = EditorPlugin.timeEditing.ToString();
            jn["editor"]["a"] = EditorPlugin.openAmount.ToString();
            jn["misc"]["sn"] = SettingEditor.inst.SnapActive.ToString();

            RTFile.WriteToFile(GameManager.inst.basePath + "editor.lse", jn.ToString(3));

            return false;
        }

        public static IEnumerator SaveData(string _path)
        {
            if (EditorManager.inst != null)
            {
                EditorManager.inst.DisplayNotification("Saving Beatmap!", 1f, EditorManager.NotificationType.Warning);
                EditorManager.inst.savingBeatmap = true;
            }
            //Task task;
            yield return DataManager.inst.StartCoroutine(RTFunctions.Functions.ProjectData.Writer.SaveData(_path, (GameData)DataManager.inst.gameData));
            yield return new WaitForSeconds(0.5f);
            if (EditorManager.inst != null)
            {
                EditorManager.inst.DisplayNotification("Saved Beatmap!", 2f, EditorManager.NotificationType.Success);
                EditorManager.inst.savingBeatmap = false;
            }
            yield break;
        }

        [HarmonyPatch("SaveBeatmapAs", new Type[] { typeof(string) })]
        [HarmonyPrefix]
        static bool SaveBeatmapAsPrefix(EditorManager __instance, string __0)
        {
            if (__instance.hasLoadedLevel)
            {
                string str = "/beatmaps/editor/" + __0;
                Directory.CreateDirectory(LSFile.GetApplicationDirectory() + str);
                File.Copy(LSFile.GetApplicationDirectory() + "/" + RTEditor.editorListSlash + GameManager.inst.levelName + "/level.ogg", LSFile.GetApplicationDirectory() + str + "/level.ogg", true);
                __instance.StartCoroutine(DataManager.inst.SaveData(RTFile.ApplicationDirectory + str + "/level.lsb"));
                return false;
            }
            __instance.DisplayNotification("Beatmap Can't be saved till you load a level.", 5f, EditorManager.NotificationType.Error);
            return false;
        }

        [HarmonyPatch("OpenBeatmapPopup")]
        [HarmonyPrefix]
        static bool OpenBeatmapPopupPrefix(EditorManager __instance)
        {
            Debug.LogFormat("{0}Open Beatmap Popup", EditorPlugin.className);
            InputField component = __instance.GetDialog("Open File Popup").Dialog.Find("search-box/search").GetComponent<InputField>();
            if (__instance.openFileSearch == null)
                __instance.openFileSearch = "";

            component.text = __instance.openFileSearch;
            __instance.ClearDialogs(new EditorManager.EditorDialog.DialogType[1]);
            __instance.RenderOpenBeatmapPopup();
            __instance.ShowDialog("Open File Popup");

            //Create Local Variables
            GameObject openLevel = __instance.GetDialog("Open File Popup").Dialog.gameObject;
            Transform openTLevel = openLevel.transform;
            RectTransform openRTLevel = openLevel.GetComponent<RectTransform>();
            GridLayoutGroup openGridLVL = openTLevel.Find("mask/content").GetComponent<GridLayoutGroup>();

            //Set Editor Zoom cap
            EditorManager.inst.zoomBounds = RTEditor.GetEditorProperty("Main Zoom Amount").GetConfigEntry<Vector2>().Value;

            //Set Open File Popup RectTransform
            openRTLevel.anchoredPosition = RTEditor.GetEditorProperty("Open Level Position").GetConfigEntry<Vector2>().Value;
            openRTLevel.sizeDelta = RTEditor.GetEditorProperty("Open Level Scale").GetConfigEntry<Vector2>().Value;

            //Set Open FIle Popup content GridLayoutGroup
            openGridLVL.cellSize = RTEditor.GetEditorProperty("Open Level Cell Size").GetConfigEntry<Vector2>().Value;
            openGridLVL.constraint = RTEditor.GetEditorProperty("Open Level Cell Constraint Type").GetConfigEntry<GridLayoutGroup.Constraint>().Value;
            openGridLVL.constraintCount = RTEditor.GetEditorProperty("Open Level Cell Constraint Count").GetConfigEntry<int>().Value;
            openGridLVL.spacing = RTEditor.GetEditorProperty("Open Level Cell Spacing").GetConfigEntry<Vector2>().Value;

            return false;
        }

        [HarmonyPatch("AssignWaveformTextures")]
        [HarmonyPrefix]
        static bool AssignWaveformTexturesPatch() => false;

        [HarmonyPatch("RenderOpenBeatmapPopup")]
        [HarmonyPostfix]
        static void RenderOpenBeatmapPopupPrefix() => RTEditor.inst.RefreshLevelList();

        [HarmonyPatch("RenderParentSearchList")]
        [HarmonyPrefix]
        static bool RenderParentSearchList(EditorManager __instance)
        {
            if (ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
                RTEditor.inst.RefreshParentSearch(__instance, ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>());
            return false;
        }

        [HarmonyPatch("OpenAlbumArtSelector")]
        [HarmonyPrefix]
        static bool OpenAlbumArtSelector(EditorManager __instance)
        {
            string jpgFile = FileBrowser.OpenSingleFile("jpg");
            Debug.Log("Selected file: " + jpgFile);
            if (!string.IsNullOrEmpty(jpgFile))
            {
                string jpgFileLocation = RTFile.ApplicationDirectory + RTEditor.editorListSlash + __instance.currentLoadedLevel + "/level.jpg";
                __instance.StartCoroutine(__instance.GetSprite(jpgFile, new EditorManager.SpriteLimits(new Vector2(512f, 512f)), delegate (Sprite cover)
                {
                    File.Copy(jpgFile, jpgFileLocation, true);
                    EditorManager.inst.GetDialog("Metadata Editor").Dialog.transform.Find("Scroll View/Viewport/Content/creator/cover_art/image").GetComponent<Image>().sprite = cover;
                    MetadataEditor.inst.currentLevelCover = cover;
                }, delegate (string errorFile)
                {
                    __instance.DisplayNotification("Please resize your image to be less then or equal to 512 x 512 pixels. It must also be a jpg.", 2f, EditorManager.NotificationType.Error, false);
                }));
            }
            return false;
        }

        [HarmonyPatch("RenderTimeline")]
        [HarmonyPrefix]
        static bool RenderTimelinePatch(EditorManager __instance)
        {
            if (RTEditor.inst.layerType == RTEditor.LayerType.Events)
                EventEditor.inst.RenderEventObjects();
            else
                ObjEditor.inst.RenderTimelineObjects();

            CheckpointEditor.inst.RenderCheckpoints();
            MarkerEditor.inst.RenderMarkers();

            __instance.UpdateTimelineSizes();
            return false;
        }

        [HarmonyPatch("QuitToMenu")]
        [HarmonyPrefix]
        static bool QuitToMenuPrefix(EditorManager __instance)
        {
            if (__instance.savingBeatmap)
            {
                __instance.DisplayNotification("Please wait till the beatmap finishes saving!", 2f, EditorManager.NotificationType.Error);
                return false;
            }

            DG.Tweening.DOTween.KillAll(false);
            DG.Tweening.DOTween.Clear(true);
            __instance.loadedLevels.Clear();
            DataManager.inst.gameData = null;
            DataManager.inst.gameData = new RTFunctions.Functions.Data.GameData();
            DiscordController.inst.OnIconChange("");
            DiscordController.inst.OnStateChange("");
            Debug.Log($"{__instance.className}Quit to Main Menu");
            InputDataManager.inst.players.Clear();
            SceneManager.inst.LoadScene("Main Menu");
            return false;
        }

        [HarmonyPatch("CreateNewLevel")]
        [HarmonyPrefix]
        static bool CreateNewLevelPrefix()
        {
            RTEditor.inst.CreateNewLevel();
            return false;
        }

        [HarmonyPatch("OpenedLevel", MethodType.Getter)]
        [HarmonyPrefix]
        static bool OpenedLevelPrefix(EditorManager __instance, ref bool __result)
        {
            __result = __instance.wasOpenLevel && __instance.hasLoadedLevel;
            return false;
        }
    }
}
