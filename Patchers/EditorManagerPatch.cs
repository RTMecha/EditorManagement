using CielaSpike;
using Crosstales.FB;
using EditorManagement.Functions;
using EditorManagement.Functions.Editors;
using EditorManagement.Functions.Helpers;
using HarmonyLib;
using LSFunctions;
using RTFunctions.Functions;
using RTFunctions.Functions.Data;
using RTFunctions.Functions.Data.Player;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;
using RTFunctions.Functions.Managers.Networking;
using RTFunctions.Functions.Optimization;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DGEase = DG.Tweening.Ease;

namespace EditorManagement.Patchers
{
    [HarmonyPatch(typeof(EditorManager))]
    public class EditorManagerPatch : MonoBehaviour
    {
        static EditorManager Instance { get => EditorManager.inst; set => EditorManager.inst = value; }

        static bool April => RTHelpers.AprilFools;

        [HarmonyPatch("Awake")]
        [HarmonyPrefix]
        static bool AwakePrefix(EditorManager __instance)
        {
            if (!Instance)
                Instance = __instance;
            else if (Instance != __instance)
            {
                Destroy(__instance.gameObject);
                return false;
            }

            Debug.Log($"{__instance.className}" +
                $"---------------------------------------------------------------------\n" +
                $"---------------------------- INITIALIZED ----------------------------\n" +
                $"---------------------------------------------------------------------\n");

            FontManager.inst.ChangeAllFontsInEditor();

            InputDataManager.inst.BindMenuKeys();
            InputDataManager.inst.BindEditorKeys();
            __instance.ScreenScale = Screen.width / 1920f;
            __instance.ScreenScaleInverse = 1f / __instance.ScreenScale;
            __instance.RefreshDialogDictionary();

            var easingDropdowns = (from x in Resources.FindObjectsOfTypeAll<Dropdown>()
                        where x.gameObject != null && x.gameObject.name == "curves"
                        select x).ToList();

            RTEditor.EasingDropdowns.Clear();

            var easings = __instance.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList();

            foreach (var dropdown in easingDropdowns)
            {
                dropdown.ClearOptions();
                dropdown.AddOptions(easings);

                TriggerHelper.AddEventTriggerParams(dropdown.gameObject, TriggerHelper.CreateEntry(EventTriggerType.Scroll, delegate (BaseEventData baseEventData)
                {
                    if (!EditorConfig.Instance.ScrollOnEasing.Value)
                        return;

                    var pointerEventData = (PointerEventData)baseEventData;
                    if (pointerEventData.scrollDelta.y > 0f)
                        dropdown.value = dropdown.value == 0 ? dropdown.options.Count - 1 : dropdown.value - 1;
                    if (pointerEventData.scrollDelta.y < 0f)
                        dropdown.value = dropdown.value == dropdown.options.Count - 1 ? 0 : dropdown.value + 1;
                }));
            }

            RTEditor.EasingDropdowns = easingDropdowns;

            if (Updater.levelProcessor)
            {
                Updater.levelProcessor.Dispose();
                Updater.levelProcessor = null;
            }

            // Editor Theme Setup
            try
            {
                var timelineBackground = EditorManager.inst.timeline.transform.parent.Find("Panel 2").gameObject;
                EditorThemeManager.AddElement(new EditorThemeManager.Element("Timeline Background", "Timeline Background", timelineBackground, new List<Component>
                {
                    timelineBackground.GetComponent<Image>(),
                }));

                var openFilePopup = __instance.GetDialog("Open File Popup").Dialog.gameObject;
                EditorThemeManager.AddElement(new EditorThemeManager.Element("Open File Popup", "Background", openFilePopup, new List<Component>
                {
                    openFilePopup.GetComponent<Image>(),
                }, true, 1, SpriteManager.RoundedSide.Bottom_Left_I));

                var openFilePopupPanel = openFilePopup.transform.Find("Panel").gameObject;
                EditorThemeManager.AddElement(new EditorThemeManager.Element("Open File Popup Panel", "Background", openFilePopupPanel, new List<Component>
                {
                    openFilePopupPanel.GetComponent<Image>(),
                }, true, 1, SpriteManager.RoundedSide.Top));

                var openFilePopupClose = openFilePopupPanel.transform.Find("x").gameObject;
                EditorThemeManager.AddElement(new EditorThemeManager.Element("Open File Popup Close", "Close", openFilePopupClose, new List<Component>
                {
                    openFilePopupClose.GetComponent<Image>(),
                    openFilePopupClose.GetComponent<Button>(),
                }, true, 1, SpriteManager.RoundedSide.W, true));
                
                var openFilePopupCloseX = openFilePopupClose.transform.GetChild(0).gameObject;
                EditorThemeManager.AddElement(new EditorThemeManager.Element("Open File Popup Close X", "Close X", openFilePopupCloseX, new List<Component>
                {
                    openFilePopupCloseX.GetComponent<Image>(),
                }));
                
                var openFilePopupTitle = openFilePopupPanel.transform.Find("Text").gameObject;
                EditorThemeManager.AddElement(new EditorThemeManager.Element("Open File Popup Title", "Light Text", openFilePopupTitle, new List<Component>
                {
                    openFilePopupTitle.GetComponent<Text>(),
                }));

                var openFilePopupScrollbar = openFilePopup.transform.Find("Scrollbar").gameObject;
                EditorThemeManager.AddElement(new EditorThemeManager.Element("Open File Popup Scrollbar", "Background", openFilePopupScrollbar, new List<Component>
                {
                    openFilePopupScrollbar.GetComponent<Image>(),
                }, true, 1, SpriteManager.RoundedSide.Bottom_Right_I));

                var openFilePopupScrollbarHandle = openFilePopupScrollbar.transform.Find("Sliding Area/Handle").gameObject;
                EditorThemeManager.AddElement(new EditorThemeManager.Element("Open File Popup Scrollbar Handle", "Scrollbar Handle", openFilePopupScrollbarHandle, new List<Component>
                {
                    openFilePopupScrollbarHandle.GetComponent<Image>(),
                    openFilePopupScrollbar.GetComponent<Scrollbar>()
                }, true, 1, SpriteManager.RoundedSide.W, true));

                EditorThemeManager.AddInputField(openFilePopup.transform.Find("search-box/search").GetComponent<InputField>(), "Open File Popup Search", "Search Field 1", 1, SpriteManager.RoundedSide.Bottom);

                EditorThemeManager.AddElement(new EditorThemeManager.Element("Editor Dialog", "Background", EditorManager.inst.dialogs.gameObject, new List<Component>
                {
                    EditorManager.inst.dialogs.GetComponent<Image>(),
                }));

                var titleBar = EditorManager.inst.GUIMain.transform.Find("TitleBar").gameObject;
                EditorThemeManager.AddElement(new EditorThemeManager.Element("Title Bar Base", "Background", titleBar, new List<Component>
                {
                    titleBar.GetComponent<Image>(),
                }));

                for (int i = 0; i < titleBar.transform.childCount; i++)
                {
                    var child = titleBar.transform.GetChild(i).gameObject;
                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Title Bar {child.name}", "Title Bar Button", child, new List<Component>
                    {
                        child.GetComponent<Image>(),
                        child.GetComponent<Button>(),
                    }, isSelectable: true));

                    var text = child.transform.GetChild(0).gameObject;
                    EditorThemeManager.AddElement(new EditorThemeManager.Element($"Title Bar {child.name} Text", "Title Bar Text", text, new List<Component>
                    {
                        text.GetComponent<Text>(),
                    }));

                    if (child.transform.childCount > 1)
                    {
                        var dropdownBase = child.transform.GetChild(1).gameObject;

                        dropdownBase.AddComponent<Mask>();

                        EditorThemeManager.AddElement(new EditorThemeManager.Element($"Title Bar {child.name} Dropdown", "Title Bar Dropdown Normal", dropdownBase, new List<Component>
                        {
                            dropdownBase.GetComponent<Image>(),
                        }, true, 1, SpriteManager.RoundedSide.Bottom));

                        for (int j = 0; j < dropdownBase.transform.childCount; j++)
                        {
                            var childB = dropdownBase.transform.GetChild(j).gameObject;
                            EditorThemeManager.AddElement(new EditorThemeManager.Element($"Title Bar {child.name} - {childB.name}", "Title Bar Dropdown", childB, new List<Component>
                            {
                                childB.GetComponent<Image>(),
                                childB.GetComponent<Button>(),
                            }, isSelectable: true));

                            var text2 = childB.transform.GetChild(0).gameObject;
                            EditorThemeManager.AddElement(new EditorThemeManager.Element($"Title Bar {child.name} - {childB.name} Text", "Title Bar Text", text2, new List<Component>
                            {
                                text2.GetComponent<Text>(),
                            }));

                            var image = childB.transform.Find("Image").gameObject;
                            EditorThemeManager.AddElement(new EditorThemeManager.Element($"Title Bar {child.name} - {childB.name} Image", "Title Bar Text", image, new List<Component>
                            {
                                image.GetComponent<Image>(),
                            }));
                        }
                    }
                }

                var saveAsPopup = __instance.GetDialog("Save As Popup").Dialog.GetChild(0).gameObject;
                EditorThemeManager.AddElement(new EditorThemeManager.Element("Save As Popup", "Background", saveAsPopup, new List<Component>
                {
                    saveAsPopup.GetComponent<Image>(),
                }, true, 1, SpriteManager.RoundedSide.W));

                var saveAsPopupPanel = saveAsPopup.transform.Find("Panel").gameObject;
                EditorThemeManager.AddElement(new EditorThemeManager.Element("Save As Popup Panel", "Background", saveAsPopupPanel, new List<Component>
                {
                    saveAsPopupPanel.GetComponent<Image>(),
                }, true, 1, SpriteManager.RoundedSide.Top));

                var saveAsPopupClose = saveAsPopupPanel.transform.Find("x").gameObject;
                Destroy(saveAsPopupClose.GetComponent<Animator>());
                var saveAsPopupCloseButton = saveAsPopupClose.GetComponent<Button>();
                saveAsPopupCloseButton.transition = Selectable.Transition.ColorTint;
                EditorThemeManager.AddElement(new EditorThemeManager.Element("Save As Popup Close", "Close", saveAsPopupClose, new List<Component>
                {
                    saveAsPopupClose.GetComponent<Image>(),
                    saveAsPopupCloseButton,
                }, true, 1, SpriteManager.RoundedSide.W, true));

                var saveAsPopupCloseX = saveAsPopupClose.transform.GetChild(0).gameObject;
                EditorThemeManager.AddElement(new EditorThemeManager.Element("Save As Popup Close X", "Close X", saveAsPopupCloseX, new List<Component>
                {
                    saveAsPopupCloseX.GetComponent<Image>(),
                }));

                EditorThemeManager.AddLightText(saveAsPopupPanel.transform.Find("Text").GetComponent<Text>());
                EditorThemeManager.AddLightText(saveAsPopup.transform.Find("Level Name").GetComponent<Text>());

                EditorThemeManager.AddInputField(saveAsPopup.transform.Find("level-name").GetComponent<InputField>(), ThemeGroup.Input_Field);

                var create = saveAsPopup.transform.Find("submit").gameObject;
                Destroy(create.GetComponent<Animator>());
                create.GetComponent<Button>().transition = Selectable.Transition.ColorTint;
                EditorThemeManager.AddElement(new EditorThemeManager.Element("Save As Popup Create", "Add", create, new List<Component>
                {
                    create.GetComponent<Image>(),
                }, true, 1, SpriteManager.RoundedSide.W));

                var createText = create.transform.Find("text").gameObject;
                EditorThemeManager.AddElement(new EditorThemeManager.Element("Save As Popup Create Text", "Add Text", createText, new List<Component>
                {
                    createText.GetComponent<Text>(),
                }));

                var fileInfoPopup = __instance.GetDialog("File Info Popup").Dialog.gameObject;
                EditorThemeManager.AddElement(new EditorThemeManager.Element("File Info Popup", "Background", fileInfoPopup, new List<Component>
                {
                    fileInfoPopup.GetComponent<Image>(),
                }, true, 1, SpriteManager.RoundedSide.W));

                EditorThemeManager.AddLightText(fileInfoPopup.transform.Find("title").GetComponent<Text>());

                EditorThemeManager.AddLightText(fileInfoPopup.transform.Find("text").GetComponent<Text>());

                var parentSelectorPopup = EditorManager.inst.GetDialog("Parent Selector").Dialog;

                EditorThemeManager.AddElement(new EditorThemeManager.Element("Parent Selector Popup", "Background", parentSelectorPopup.gameObject, new List<Component>
                {
                    parentSelectorPopup.GetComponent<Image>(),
                }, true, 1, SpriteManager.RoundedSide.W));

                var parentSelectorPopupPanel = parentSelectorPopup.transform.Find("Panel").gameObject;
                EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Background_1, parentSelectorPopupPanel, new List<Component>
                {
                    parentSelectorPopupPanel.GetComponent<Image>(),
                }, true, 1, SpriteManager.RoundedSide.Top));

                EditorThemeManager.AddSelectable(parentSelectorPopupPanel.transform.Find("x").GetComponent<Button>(), ThemeGroup.Close);

                var parentSelectorPopupCloseX = parentSelectorPopupPanel.transform.Find("x").GetChild(0).gameObject;
                EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Close_X, parentSelectorPopupCloseX, new List<Component>
                {
                    parentSelectorPopupCloseX.GetComponent<Image>(),
                }));

                EditorThemeManager.AddLightText(parentSelectorPopupPanel.transform.Find("Text").GetComponent<Text>());

                var parentSelectorPopupScrollbar = parentSelectorPopup.transform.Find("Scrollbar").gameObject;
                EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Background_1, parentSelectorPopupScrollbar, new List<Component>
                {
                    parentSelectorPopupScrollbar.GetComponent<Image>(),
                }, true, 1, SpriteManager.RoundedSide.Bottom_Right_I));

                var parentSelectorPopupScrollbarHandle = parentSelectorPopupScrollbar.transform.Find("Sliding Area/Handle").gameObject;
                EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Scrollbar_1_Handle, parentSelectorPopupScrollbarHandle, new List<Component>
                {
                    parentSelectorPopupScrollbarHandle.GetComponent<Image>(),
                    parentSelectorPopupScrollbar.GetComponent<Scrollbar>()
                }, true, 1, SpriteManager.RoundedSide.W, true));

                EditorThemeManager.AddInputField(parentSelectorPopup.transform.Find("search-box/search").GetComponent<InputField>(), ThemeGroup.Search_Field_1, 1, SpriteManager.RoundedSide.Bottom);

            }
            catch (Exception ex)
            {
                Debug.LogError($"{EditorPlugin.className}Failed to setup Editor Theme elements.\nException: {ex}");
            }

            __instance.hasLoadedLevel = false;
            __instance.loading = false;

            RTEditor.Init(__instance);
            KeybindManager.Init(__instance);

            ThemeEditorManager.Init(ThemeEditor.inst);

            // New Level Name input field contains text but newLevelName does not, so people might end up making an empty named level if they don't name it anything else.
            __instance.newLevelName = "New Awesome Beatmap";

            return false;
        }

        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        static bool StartPrefix()
        {
            Instance.GetLevelList();

            RTFunctions.FunctionsPlugin.UpdateDiscordStatus("", "In Editor", "editor");

            Instance.SetDialogStatus("Timeline", true, true);

            InputDataManager.inst.players.Clear();
            InputDataManager.inst.players.Add(new CustomPlayer(true, 0, null));

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

            InputDataManager.inst.editorActions.Cut.ClearBindings();
            InputDataManager.inst.editorActions.Copy.ClearBindings();
            InputDataManager.inst.editorActions.Paste.ClearBindings();
            InputDataManager.inst.editorActions.Duplicate.ClearBindings();
            InputDataManager.inst.editorActions.Delete.ClearBindings();
            InputDataManager.inst.editorActions.Undo.ClearBindings();
            InputDataManager.inst.editorActions.Redo.ClearBindings();
            InputDataManager.inst.editorActions.CreateMarker.ClearBindings();

            Instance.notification.transform.Find("info").gameObject.SetActive(true);

            //Set Editor Zoom cap
            Instance.zoomBounds = EditorConfig.Instance.MainZoomBounds.Value;

            return false;
        }

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static bool UpdatePrefix()
        {
            Instance.ScreenScale = Screen.width / 1920f;
            Instance.ScreenScaleInverse = 1f / Instance.ScreenScale;

            if (GameManager.inst.gameState == GameManager.State.Playing)
            {
                if (Instance.canEdit)
                {
                    if (InputDataManager.inst.editorActions.ToggleEditor.WasPressed && !Instance.IsUsingInputField() || Input.GetKeyDown(KeyCode.Escape) && !Instance.isEditing)
                        Instance.ToggleEditor();

                    if (Instance.isEditing)
                    {
                        if (!Instance.IsUsingInputField())
                        {
                            Instance.handleViewShortcuts();
                        }
                    }
                    if (!Instance.firstOpened)
                    {
                        try
                        {
                            Instance.StartCoroutine(RTEditor.inst.AssignTimelineTexture());
                            Instance.UpdateTimelineSizes();
                            Instance.firstOpened = true;

                            RTEventEditor.inst.CreateEventObjects();
                            CheckpointEditor.inst.CreateGhostCheckpoints();
                            GameManager.inst.UpdateTimeline();
                            CheckpointEditor.inst.SetCurrentCheckpoint(0);
                            if (!April)
                                Instance.TogglePlayingSong();
                            else
                                Instance.DisplayNotification("Welcome to the 3.0.0 update!\njk, April Fools!", 6f, EditorManager.NotificationType.Error);
                            Instance.ClearDialogs();
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

            if (Instance.GUI.activeSelf == true && Instance.isEditing == true)
            {
                if (RTEditor.inst.timeIF && !RTEditor.inst.timeIF.isFocused)
                    RTEditor.inst.timeIF.text = AudioManager.inst.CurrentAudioSource.time.ToString();

                if (RTEditor.inst.pitchIF && !RTEditor.inst.pitchIF.isFocused)
                    RTEditor.inst.pitchIF.text = ModCompatibility.sharedFunctions.ContainsKey("EventsCorePitchOffset") ?
                        ((float)ModCompatibility.sharedFunctions["EventsCorePitchOffset"]).ToString() : AudioManager.inst.pitch.ToString();

                if (RTEditor.inst.doggoImage)
                    RTEditor.inst.doggoImage.sprite = Instance.loadingImage.sprite;
            }

            var multi = Instance.GetDialog("Multi Object Editor").Dialog;
            if (multi.gameObject.activeSelf && ((RectTransform)multi.Find("data")).sizeDelta != new Vector2(810f, 730.11f))
            {
                ((RectTransform)multi.Find("data")).sizeDelta = new Vector2(810f, 730.11f);
            }
            if (multi.gameObject.activeSelf && ((RectTransform)multi.Find("data/left")).sizeDelta != new Vector2(355f, 730f))
            {
                ((RectTransform)multi.Find("data/left")).sizeDelta = new Vector2(355f, 730f);
            }

            Instance.prevAudioTime = AudioManager.inst.CurrentAudioSource.time;
            return false;
        }

        [HarmonyPatch("TogglePlayingSong")]
        [HarmonyPrefix]
        static bool TogglePlayingSongPrefix()
        {
            if (April || Instance.hasLoadedLevel)
            {
                if (AudioManager.inst.CurrentAudioSource.isPlaying)
                    AudioManager.inst.CurrentAudioSource.Pause();
                else
                    AudioManager.inst.CurrentAudioSource.Play();
                Instance.UpdatePlayButton();
            }
            else
            {
                AudioManager.inst.CurrentAudioSource.Pause();
                Instance.UpdatePlayButton();
            }
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
            Application.OpenURL("https://www.youtube.com/playlist?list=PLMHuUok_ojlX89xw2z6hUFF3meXFXz9DL");
            Instance.DisplayNotification("PA Mod Showcases playlist will open in your browser!", 2f, EditorManager.NotificationType.Success);
            return false;
        }

        [HarmonyPatch("OpenVerifiedSongs")]
        [HarmonyPrefix]
        static bool OpenVerifiedSongsPrefix()
        {
            Application.OpenURL("https://www.youtube.com/playlist?list=PLMHuUok_ojlX89xw2z6hUFF3meXFXz9DL");
            Instance.DisplayNotification("PA Mod Showcases playlist will open in your browser!", 2f, EditorManager.NotificationType.Success);
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
            if (April)
            {
                Instance.StartCoroutine(AlephNetworkManager.DownloadJSONFile("https://drive.google.com/uc?export=download&id=1QJUeviLerCX1tZXW7QxpBC6K1BjtG1KT", delegate (string json)
                {
                    DataManager.inst.gameData = GameData.Parse(JSON.Parse(json));

                    Instance.StartCoroutine(AlephNetworkManager.DownloadAudioClip("https://drive.google.com/uc?export=download&id=1BDrRqX1IDk7bKo2hhYDqDqWLncMy7FkP", AudioType.OGGVORBIS, delegate (AudioClip audioClip)
                    {
                        AudioManager.inst.PlayMusic(null, audioClip, true, 0f);
                        GameManager.inst.gameState = GameManager.State.Playing;

                        Instance.StartCoroutine(Updater.IUpdateObjects(true));
                    }));
                }));

                return false;
            }

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
        static bool LoadLevelPrefix(ref IEnumerator __result, string __0)
        {
            __result = RTEditor.inst.LoadLevel($"{RTFile.ApplicationDirectory}{RTEditor.editorListSlash}{__0}");
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
            var config = EditorConfig.Instance;

            if (Instance.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Object)
                && Instance.IsOverObjTimeline
                && !LSHelpers.IsUsingInputField()
                && !RTEditor.inst.isOverMainTimeline)
            {
                float multiply = 1f;
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    multiply = 2f;
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    multiply = 0.1f;

                if (InputDataManager.inst.editorActions.ZoomIn.WasPressed)
                    ObjEditor.inst.Zoom = ObjEditor.inst.zoomFloat + config.KeyframeZoomAmount.Value * multiply;
                if (InputDataManager.inst.editorActions.ZoomOut.WasPressed)
                    ObjEditor.inst.Zoom = ObjEditor.inst.zoomFloat - config.KeyframeZoomAmount.Value * multiply;
            }

            if (!Instance.IsOverObjTimeline && RTEditor.inst.isOverMainTimeline)
            {
                float multiply = 1f;
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    multiply = 2f;
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    multiply = 0.1f;

                if (InputDataManager.inst.editorActions.ZoomIn.WasPressed)
                    Instance.Zoom = Instance.zoomFloat + config.MainZoomAmount.Value * multiply;
                if (InputDataManager.inst.editorActions.ZoomOut.WasPressed)
                    Instance.Zoom = Instance.zoomFloat - config.MainZoomAmount.Value * multiply;
            }

            return false;
        }

        [HarmonyPatch("updatePointer")]
        [HarmonyPrefix]
        static bool updatePointerPrefix()
        {
            Vector2 point = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            Rect rect = new Rect(0f, 0.305f * (float)Screen.height, (float)Screen.width, (float)Screen.height * 0.025f);
            if (Instance.updateAudioTime && Input.GetMouseButtonUp(0) && rect.Contains(point))
            {
                AudioManager.inst.CurrentAudioSource.time = Instance.audioTimeForSlider / Instance.Zoom;
                Instance.updateAudioTime = false;
            }
            if (Input.GetMouseButton(0) && rect.Contains(point) && RTEditor.inst && RTEditor.inst.timelineSlider)
            {
                var slider = RTEditor.inst.timelineSlider;
                slider.minValue = 0f;
                slider.maxValue = AudioManager.inst.CurrentAudioSource.clip.length * Instance.Zoom;
                Instance.audioTimeForSlider = Instance.timelineSlider.GetComponent<Slider>().value;
                Instance.updateAudioTime = true;
                Instance.wasDraggingPointer = true;
                if (Mathf.Abs(Instance.audioTimeForSlider / Instance.Zoom - Instance.prevAudioTime) < 2f)
                {
                    if (EditorConfig.Instance.DraggingMainCursorPausesLevel.Value)
                    {
                        AudioManager.inst.CurrentAudioSource.Pause();
                        Instance.UpdatePlayButton();
                    }
                    AudioManager.inst.CurrentAudioSource.time = Instance.audioTimeForSlider / Instance.Zoom;
                }
            }
            else if (Instance.updateAudioTime && Instance.wasDraggingPointer && !rect.Contains(point))
            {
                AudioManager.inst.CurrentAudioSource.time = Instance.audioTimeForSlider / Instance.Zoom;
                Instance.updateAudioTime = false;
                Instance.wasDraggingPointer = false;
            }
            else if (RTEditor.inst && RTEditor.inst.timelineSlider)
            {
                var slider = RTEditor.inst.timelineSlider;

                slider.minValue = 0f;
                slider.maxValue = AudioManager.inst.CurrentAudioSource.clip.length * Instance.Zoom;
                slider.value = AudioManager.inst.CurrentAudioSource.time * Instance.Zoom;
                Instance.audioTimeForSlider = AudioManager.inst.CurrentAudioSource.time * Instance.Zoom;
            }
            Instance.timelineSlider.transform.AsRT().sizeDelta = new Vector2(AudioManager.inst.CurrentAudioSource.clip.length * Instance.Zoom, 25f);
            return false;
        }

        [HarmonyPatch("AddToPitch")]
        [HarmonyPrefix]
        static bool AddToPitchPrefix(float __0)
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
            if (Instance.isEditing)
            {
                Instance.UpdatePlayButton();
            }
            GameManager.inst.ResetCheckpoints();
        }

        [HarmonyPatch("CloseOpenBeatmapPopup")]
        [HarmonyPrefix]
        static bool CloseOpenBeatmapPopupPrefix()
        {
            Instance.HideDialog("Open File Popup");
            return false;
        }

        [HarmonyPatch("SaveBeatmap")]
        [HarmonyPrefix]
        static bool SaveBeatmapPrefix()
        {
            if (!Instance.hasLoadedLevel)
            {
                Instance.DisplayNotification("Beatmap can't be saved until you load a level.", 5f, EditorManager.NotificationType.Error);
                return false;
            }
            if (Instance.savingBeatmap)
            {
                Instance.DisplayNotification("Attempting to save beatmap already, please wait!", 2f, EditorManager.NotificationType.Error);
                return false;
            }

            if (RTFile.FileExists(GameManager.inst.basePath + "level-previous.lsb"))
                File.Delete(GameManager.inst.basePath + "level-previous.lsb");

            if (RTFile.FileExists(GameManager.inst.basePath + "level.lsb"))
                File.Copy(GameManager.inst.basePath + "level.lsb", GameManager.inst.basePath + "level-previous.lsb");

            DataManager.inst.SaveMetadata(GameManager.inst.basePath + "metadata.lsb");
            Instance.StartCoroutine(SaveData(GameManager.inst.path));
            PlayerManager.SaveLocalModels?.Invoke();

            RTEditor.inst.SaveSettings();

            return false;
        }

        public static IEnumerator SaveData(string _path)
        {
            if (Instance != null)
            {
                Instance.DisplayNotification("Saving Beatmap!", 1f, EditorManager.NotificationType.Warning);
                Instance.savingBeatmap = true;
            }
            Task task;
            yield return DataManager.inst.StartCoroutineAsync(RTFunctions.Functions.ProjectData.Writer.SaveData(_path, (GameData)DataManager.inst.gameData), out task);
            yield return new WaitForSeconds(0.5f);
            if (Instance != null)
            {
                Instance.DisplayNotification("Saved Beatmap!", 2f, EditorManager.NotificationType.Success);
                Instance.savingBeatmap = false;
            }
            yield break;
        }

        [HarmonyPatch("OpenSaveAs")]
        [HarmonyPrefix]
        static bool OpenSaveAsPrefix()
        {
            if (Instance.hasLoadedLevel)
            {
                Instance.ClearDialogs();
                Instance.ShowDialog("Save As Popup");
                return false;
            }
            Instance.DisplayNotification("Beatmap can't be saved as until you load a level.", 5f, EditorManager.NotificationType.Error);
            return false;
        }

        [HarmonyPatch("SaveBeatmapAs", new Type[] { typeof(string) })]
        [HarmonyPrefix]
        static bool SaveBeatmapAsPrefix(string __0)
        {
            if (Instance.hasLoadedLevel)
            {
                string str = RTFile.ApplicationDirectory + RTEditor.editorListSlash + __0;
                if (!RTFile.DirectoryExists(str))
                    Directory.CreateDirectory(str);

                var files = Directory.GetFiles(RTFile.ApplicationDirectory + RTEditor.editorListSlash + GameManager.inst.levelName);

                foreach (var file in files)
                {
                    if (!RTFile.DirectoryExists(Path.GetDirectoryName(file)))
                        Directory.CreateDirectory(Path.GetDirectoryName(file));

                    string saveTo = file.Replace("\\", "/").Replace(RTFile.ApplicationDirectory + RTEditor.editorListSlash + GameManager.inst.levelName, str);
                    File.Copy(file, saveTo, RTFile.FileExists(saveTo));
                }

                Instance.StartCoroutine(ProjectData.Writer.SaveData(str + "/level.lsb", GameData.Current, delegate ()
                {
                    Instance.DisplayNotification($"Saved beatmap to {__0}", 3f, EditorManager.NotificationType.Success);
                }));
                return false;
            }
            Instance.DisplayNotification("Beatmap can't be saved as until you load a level.", 3f, EditorManager.NotificationType.Error);
            return false;
        }

        [HarmonyPatch("OpenBeatmapPopup")]
        [HarmonyPrefix]
        static bool OpenBeatmapPopupPrefix()
        {
            Debug.LogFormat("{0}Open Beatmap Popup", EditorPlugin.className);
            var component = Instance.GetDialog("Open File Popup").Dialog.Find("search-box/search").GetComponent<InputField>();
            if (Instance.openFileSearch == null)
                Instance.openFileSearch = "";

            component.text = Instance.openFileSearch;
            Instance.ClearDialogs(EditorManager.EditorDialog.DialogType.Popup);
            Instance.RenderOpenBeatmapPopup();
            Instance.ShowDialog("Open File Popup");

            var config = EditorConfig.Instance;

            try
            {
                //Create Local Variables
                var openLevel = Instance.GetDialog("Open File Popup").Dialog.gameObject;
                var openTLevel = openLevel.transform;
                var openRTLevel = openLevel.GetComponent<RectTransform>();
                var openGridLVL = openTLevel.Find("mask/content").GetComponent<GridLayoutGroup>();

                //Set Open File Popup RectTransform
                openRTLevel.anchoredPosition = config.OpenLevelPosition.Value;
                openRTLevel.sizeDelta = config.OpenLevelScale.Value;

                //Set Open FIle Popup content GridLayoutGroup
                openGridLVL.cellSize = config.OpenLevelCellSize.Value;
                openGridLVL.constraint = config.OpenLevelCellConstraintType.Value;
                openGridLVL.constraintCount = config.OpenLevelCellConstraintCount.Value;
                openGridLVL.spacing = config.OpenLevelCellSpacing.Value;
            }
            catch (Exception ex)
            {
                Debug.Log($"OpenBeatmapPopup {ex}");
            }

            return false;
        }

        [HarmonyPatch("AssignWaveformTextures")]
        [HarmonyPrefix]
        static bool AssignWaveformTexturesPatch() => false;

        [HarmonyPatch("RenderOpenBeatmapPopup")]
        [HarmonyPrefix]
        static bool RenderOpenBeatmapPopupPrefix()
        {
            RTEditor.inst.StartCoroutine(RTEditor.inst.RefreshLevelList());
            return false;
        }

        [HarmonyPatch("RenderParentSearchList")]
        [HarmonyPrefix]
        static bool RenderParentSearchList()
        {
            if (ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
                RTEditor.inst.RefreshParentSearch(Instance, ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>());
            return false;
        }

        [HarmonyPatch("OpenAlbumArtSelector")]
        [HarmonyPrefix]
        static bool OpenAlbumArtSelector()
        {
            string jpgFile = FileBrowser.OpenSingleFile("jpg");
            Debug.Log("Selected file: " + jpgFile);
            if (!string.IsNullOrEmpty(jpgFile))
            {
                string jpgFileLocation = RTFile.ApplicationDirectory + RTEditor.editorListSlash + Instance.currentLoadedLevel + "/level.jpg";
                Instance.StartCoroutine(Instance.GetSprite(jpgFile, new EditorManager.SpriteLimits(new Vector2(512f, 512f)), delegate (Sprite cover)
                {
                    File.Copy(jpgFile, jpgFileLocation, true);
                    Instance.GetDialog("Metadata Editor").Dialog.transform.Find("Scroll View/Viewport/Content/creator/cover_art/image").GetComponent<Image>().sprite = cover;
                    MetadataEditor.inst.currentLevelCover = cover;
                }, delegate (string errorFile)
                {
                    Instance.DisplayNotification("Please resize your image to be less then or equal to 512 x 512 pixels. It must also be a jpg.", 2f, EditorManager.NotificationType.Error, false);
                }));
            }
            return false;
        }

        [HarmonyPatch("RenderTimeline")]
        [HarmonyPrefix]
        static bool RenderTimelinePatch()
        {
            if (RTEditor.inst.layerType == RTEditor.LayerType.Events)
                EventEditor.inst.RenderEventObjects();
            else
                ObjectEditor.inst.RenderTimelineObjectsPositions();

            CheckpointEditor.inst.RenderCheckpoints();
            MarkerEditor.inst.RenderMarkers();

            Instance.UpdateTimelineSizes();

            RTEditor.inst.SetTimelineGridSize();

            return false;
        }

        [HarmonyPatch("QuitToMenu")]
        [HarmonyPrefix]
        static bool QuitToMenuPrefix()
        {
            if (Instance.savingBeatmap)
            {
                Instance.DisplayNotification("Please wait until the beatmap finishes saving!", 2f, EditorManager.NotificationType.Error);
                return false;
            }

            EditorManager.inst.ShowDialog("Warning Popup");
            RTEditor.inst.RefreshWarningPopup("Are you sure you want to quit to the main menu? Any unsaved progress will be lost!", delegate ()
            {
                if (Instance.savingBeatmap)
                {
                    Instance.DisplayNotification("Please wait until the beatmap finishes saving!", 2f, EditorManager.NotificationType.Error);
                    return;
                }

                DG.Tweening.DOTween.KillAll(false);
                DG.Tweening.DOTween.Clear(true);
                Instance.loadedLevels.Clear();
                DataManager.inst.gameData = null;
                DataManager.inst.gameData = new GameData();
                DiscordController.inst.OnIconChange("");
                DiscordController.inst.OnStateChange("");
                Debug.Log($"{Instance.className}Quit to Main Menu");
                InputDataManager.inst.players.Clear();
                SceneManager.inst.LoadScene("Main Menu");
            }, delegate ()
            {
                EditorManager.inst.HideDialog("Warning Popup");
            });

            return false;
        }

        [HarmonyPatch("QuitGame")]
        [HarmonyPrefix]
        static bool QuitGamePrefix()
        {
            if (Instance.savingBeatmap)
            {
                Instance.DisplayNotification("Please wait until the beatmap finishes saving!", 2f, EditorManager.NotificationType.Error, false);
                return false;
            }
            EditorManager.inst.ShowDialog("Warning Popup");
            RTEditor.inst.RefreshWarningPopup("Are you sure you want to quit the game? Any unsaved progress will be lost!", delegate ()
            {
                if (Instance.savingBeatmap)
                {
                    Instance.DisplayNotification("Please wait until the beatmap finishes saving!", 2f, EditorManager.NotificationType.Error);
                    return;
                }

                DiscordController.inst.OnIconChange("");
                DiscordController.inst.OnStateChange("");
                Debug.Log($"{Instance.className}Quit Game");
                Application.Quit();
            }, delegate ()
            {
                EditorManager.inst.HideDialog("Warning Popup");
            });

            return false;
        }

        [HarmonyPatch("CreateNewLevel")]
        [HarmonyPrefix]
        static bool CreateNewLevelPrefix()
        {
            RTEditor.inst.CreateNewLevel();
            return false;
        }

        [HarmonyPatch("OpenLevelFolder")]
        [HarmonyPrefix]
        static bool OpenLevelFolder()
        {
            if (RTFile.DirectoryExists(GameManager.inst.basePath.Substring(0, GameManager.inst.basePath.LastIndexOf("/"))))
            {
                RTFile.OpenInFileBrowser.Open(GameManager.inst.basePath);
                return false;
            }
            RTFile.OpenInFileBrowser.Open(RTFile.ApplicationDirectory + RTEditor.editorListPath);
            return false;
        }

        [HarmonyPatch("SetEditRenderArea")]
        [HarmonyPrefix]
        static bool SetEditRenderAreaPrefix()
        {
            if (Instance.hasLoadedLevel)
                WindowController.ResetResolution();

            EventManager.inst.cam.rect = new Rect(0f, 0.3708f, 0.601f, 0.601f);
            EventManager.inst.camPer.rect = new Rect(0f, 0.3708f, 0.602f, 0.601f);
            return false;
        }

        [HarmonyPatch("SetNormalRenderArea")]
        [HarmonyPrefix]
        static bool SetNormalRenderAreaPrefix()
        {
            EventManager.inst.cam.rect = new Rect(0f, 0f, 1f, 1f);
            EventManager.inst.camPer.rect = new Rect(0f, 0f, 1f, 1f);
            return false;
        }

        [HarmonyPatch("SetDialogStatus")]
        [HarmonyPrefix]
        static bool SetDialogStatusPrefix(string __0, bool __1, bool __2 = true)
        {
            RTEditor.inst.SetDialogStatus(__0, __1, __2);
            return false;
        }

        [HarmonyPatch("ClearDialogs")]
        [HarmonyPrefix]
        static bool ClearDialogsPrefix(params EditorManager.EditorDialog.DialogType[] __0)
        {
            var play = EditorConfig.Instance.PlayEditorAnimations.Value;

            var editorDialogs = Instance.EditorDialogs;
            for (int i = 0; i < editorDialogs.Count; i++)
            {
                var editorDialog = editorDialogs[i];
                if (__0.Length == 0)
                {
                    if (editorDialog.Type != EditorManager.EditorDialog.DialogType.Timeline)
                    {
                        if (play)
                            RTEditor.inst.PlayDialogAnimation(editorDialog.Dialog.gameObject, editorDialog.Name, false);
                        else
                            editorDialog.Dialog.gameObject.SetActive(false);

                        Instance.ActiveDialogs.Remove(editorDialog);
                    }
                }
                else if (__0.Contains(editorDialog.Type))
                {
                    if (play)
                        RTEditor.inst.PlayDialogAnimation(editorDialog.Dialog.gameObject, editorDialog.Name, false);
                    else
                        editorDialog.Dialog.gameObject.SetActive(false);

                    Instance.ActiveDialogs.Remove(editorDialog);
                }
            }

            //foreach (var editorDialog in Instance.EditorDialogs)
            //{
            //    if (__0.Length == 0)
            //    {
            //        if (editorDialog.Type != EditorManager.EditorDialog.DialogType.Timeline)
            //        {
            //            RTEditor.inst.PlayDialogAnimation(editorDialog.Dialog.gameObject, editorDialog.Name, false);
            //            Instance.ActiveDialogs.Remove(editorDialog);
            //        }
            //    }
            //    else if (__0.Contains(editorDialog.Type))
            //    {
            //        RTEditor.inst.PlayDialogAnimation(editorDialog.Dialog.gameObject, editorDialog.Name, false);
            //        Instance.ActiveDialogs.Remove(editorDialog);
            //    }
            //}
            Instance.currentDialog = Instance.ActiveDialogs.Last();

            return false;
        }

        [HarmonyPatch("ToggleDropdown")]
        [HarmonyPrefix]
        static bool ToggleDropdownPrefix(GameObject __0)
        {
            bool flag = !__0.activeSelf;
            foreach (var gameObject in Instance.DropdownMenus)
            {
                RTEditor.inst.PlayDialogAnimation(gameObject, gameObject.name, false);
            }

            RTEditor.inst.PlayDialogAnimation(__0, __0.name, flag);

            return false;
        }

        [HarmonyPatch("HideAllDropdowns")]
        [HarmonyPrefix]
        static bool HideAllDropdownsPrefix()
        {
            foreach (var gameObject in Instance.DropdownMenus)
            {
                RTEditor.inst.PlayDialogAnimation(gameObject, gameObject.name, false);
            }
            EventSystem.current.SetSelectedGameObject(null);
            return false;
        }

        [HarmonyPatch("GetTimelineTime")]
        [HarmonyPrefix]
        static bool GetTimelineTimePrefix(ref float __result, float __0)
        {
            __result = GetTimelineTime(__0);
            return false;
        }

        public static float GetTimelineTime(float _offset = 0f)
        {
            float num = Input.mousePosition.x;
            num += Mathf.Abs(Instance.timeline.transform.AsRT().position.x);
            if (SettingEditor.inst.SnapActive && !Input.GetKey(KeyCode.LeftAlt))
                return Instance.SnapToBPM(num * Instance.ScreenScaleInverse / Instance.Zoom);
            return num * Instance.ScreenScaleInverse / Instance.Zoom + _offset;
        }

        [HarmonyPatch("SetShowHelp")]
        [HarmonyPostfix]
        static void SetShowHelpPostfix(bool __0)
        {
            if (__0)
                RTEditor.inst.RebuildNotificationLayout();
        }

        [HarmonyPatch("GetSprite")]
        [HarmonyPrefix]
        static bool GetSpritePrefix(ref IEnumerator __result, string __0, EditorManager.SpriteLimits __1, Action<Sprite> __2, Action<string> __3)
        {
            __result = GetSprite(__0, __1, __2, __3);
            return false;
        }

        public static IEnumerator GetSprite(string _path, EditorManager.SpriteLimits _limits, Action<Sprite> callback, Action<string> onError)
        {
            yield return Instance.StartCoroutine(FileManager.inst.LoadImageFileRaw(_path, delegate (Sprite _texture)
            {
                if ((_texture.texture.width > _limits.size.x && _limits.size.x > 0f) || (_texture.texture.height > _limits.size.y && _limits.size.y > 0f))
                {
                    onError?.Invoke(_path);
                    return;
                }
                callback?.Invoke(_texture);
            }, delegate (string error)
            {
                onError?.Invoke(_path);
            }));
            yield break;
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
