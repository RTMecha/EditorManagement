using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using SimpleJSON;
using DG.Tweening;
using HarmonyLib;

using EditorManagement.Functions;
using EditorManagement.Functions.Tools;
using LSFunctions;
using Crosstales.FB;

namespace EditorManagement.Patchers
{
	[HarmonyPatch(typeof(EditorManager))]
    public class EditorPatch : MonoBehaviour
    {
		public static bool canZoom;
		public static bool IsOverMainTimeline;
		public static Type keybindsType;
		public static MethodInfo GetKeyCodeName;
		public static Transform timelineBar;
		public static InputField layersIF;

		[HarmonyPatch("Awake")]
		[HarmonyPostfix]
		public static void EditorAwakePatch(EditorManager __instance)
		{
			__instance.gameObject.AddComponent<RTEditor>();
			__instance.gameObject.AddComponent<EditorGUI>();

			var openFilePopup = __instance.GetDialog("Open File Popup").Dialog;
			var newFilePopup = __instance.GetDialog("New File Popup").Dialog;
			var parentSelector = __instance.GetDialog("Parent Selector").Dialog;
			var saveAsPopup = __instance.GetDialog("Save As Popup").Dialog;
			var quickActionsPopup = __instance.GetDialog("Quick Actions Popup").Dialog;
			var prefabPopup = __instance.GetDialog("Prefab Popup").Dialog;

			if (GameObject.Find("BepInEx_Manager").GetComponentByName("KeybindsPlugin"))
            {
				keybindsType = GameObject.Find("BepInEx_Manager").GetComponentByName("KeybindsPlugin").GetType();
				GetKeyCodeName = keybindsType.GetMethod("GetKeyCodeName");
			}

			var notifyRT = __instance.notification.GetComponent<RectTransform>();
			var notifyGroup = __instance.notification.GetComponent<VerticalLayoutGroup>();
			notifyRT.sizeDelta = new Vector2(ConfigEntries.NotificationWidth.Value, 632f);
			__instance.notification.transform.localScale = new Vector3(ConfigEntries.NotificationSize.Value, ConfigEntries.NotificationSize.Value, 1f);

			GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/Edit/Edit Dropdown/Redo/Text 1").GetComponent<Text>().text = "Ctrl + Shift + Z";
			GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/Edit/Edit Dropdown/Redo/Text 1").GetComponent<RectTransform>().sizeDelta = new Vector2(166f, 0f);

			if (ConfigEntries.NotificationDirection.Value == Direction.Down)
			{
				notifyRT.anchoredPosition = new Vector2(8f, 408f);
				notifyGroup.childAlignment = TextAnchor.LowerLeft;
			}
			if (ConfigEntries.NotificationDirection.Value == Direction.Up)
			{
				notifyRT.anchoredPosition = new Vector2(8f, 410f);
				notifyGroup.childAlignment = TextAnchor.UpperLeft;
			}

			timelineBar = GameObject.Find("TimelineBar/GameObject").transform;
			GameObject timeObj = timelineBar.GetChild(13).gameObject;
			timelineBar.GetChild(0).gameObject.SetActive(true);
			InputField iFtimeObj = timeObj.GetComponent<InputField>();
			timeObj.name = "Time Input";
			HoverTooltip timeObjTip = timeObj.AddComponent<HoverTooltip>();
			timeObjTip.tooltipLangauges.Add(Triggers.NewTooltip("Shows the exact current time of song.", "Type in the input field to go to a precise time in the level."));

			timeObj.transform.SetSiblingIndex(0);
			timeObj.SetActive(true);
			iFtimeObj.text = AudioManager.inst.CurrentAudioSource.time.ToString();
			iFtimeObj.characterValidation = InputField.CharacterValidation.Decimal;

			iFtimeObj.onValueChanged.AddListener(delegate (string _value)
			{
				RTEditor.SetNewTime(_value);
			});

			timeObj.AddComponent<EventTrigger>();

			GameObject tbarLayers = Instantiate(timeObj);
			{
				tbarLayers.transform.SetParent(timelineBar);
				tbarLayers.name = "layers";
				tbarLayers.transform.SetSiblingIndex(8);
				tbarLayers.transform.localScale = Vector3.one;

				for (int i = 0; i < tbarLayers.transform.childCount; i++)
				{
					tbarLayers.transform.GetChild(i).localScale = Vector3.one;
				}
				tbarLayers.GetComponent<HoverTooltip>().tooltipLangauges.Add(Triggers.NewTooltip("Input any positive number to go to that editor layer.", "Layers will only show specific objects that are on that layer. Can be good to use for organizing levels.", new List<string> { "Middle Mouse Button" }));

				layersIF = tbarLayers.GetComponent<InputField>();
				tbarLayers.transform.Find("Text").gameObject.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
				if (EditorManager.inst.layer < 5)
				{
					float l = EditorManager.inst.layer + 1;
					layersIF.text = l.ToString();
				}
				else
				{
					int l = EditorManager.inst.layer + 2;
					layersIF.text = l.ToString();
				}
				layersIF.characterValidation = InputField.CharacterValidation.Integer;
				layersIF.onValueChanged.RemoveAllListeners();
				layersIF.onValueChanged.AddListener(delegate (string _value)
				{
					if (int.Parse(_value) > 0)
					{
						if (int.Parse(_value) < 6)
						{
							RTEditor.SetLayer(int.Parse(_value) - 1);
						}
						else
						{
							RTEditor.SetLayer(int.Parse(_value));
						}
					}
					else
					{
						RTEditor.SetLayer(0);
						layersIF.text = "1";
					}
				});
				Image layerImage = tbarLayers.GetComponent<Image>();
				layerImage.color = __instance.layerColors[0];

				var tbarLayersET = tbarLayers.GetComponent<EventTrigger>();

				EventTrigger.Entry entryLayers = new EventTrigger.Entry();
				entryLayers.eventID = EventTriggerType.Scroll;
				entryLayers.callback.AddListener(delegate (BaseEventData eventData)
				{
					PointerEventData pointerEventData = (PointerEventData)eventData;
					//Normal
					if (!Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.LeftControl))
					{
						if (pointerEventData.scrollDelta.y < 0f)
						{
							int x = int.Parse(layersIF.text);
							x -= 1;
							layersIF.text = x.ToString();
							return;
						}
						if (pointerEventData.scrollDelta.y > 0f)
						{
							int x = int.Parse(layersIF.text);
							x += 1;
							layersIF.text = x.ToString();
						}
					}
				});
				tbarLayersET.triggers.Add(entryLayers);
			}

			for (int i = 1; i <= 5; i++)
			{
				GameObject.Find("TimelineBar/GameObject/" + i).SetActive(false);
			}

			EventTrigger eventTrigger = GameObject.Find("TimelineBar/GameObject/6").GetComponent<EventTrigger>();

			EventTrigger.Entry entryEvent = new EventTrigger.Entry();
			entryEvent.eventID = EventTriggerType.PointerClick;
			entryEvent.callback.AddListener(delegate (BaseEventData eventData)
			{
				PointerEventData pointerEventData = (PointerEventData)eventData;
				if (pointerEventData.clickTime > 0f)
				{
					EventEditorPatch.SetEventLayer(0);
				}
			});
			eventTrigger.triggers.Clear();
			eventTrigger.triggers.Add(entryEvent);

			//GameObject.Find to optimize
            {
				GameObject.Find("TimelineBar/GameObject/checkpoint").SetActive(false);

				GameObject.Find("Editor GUI/sizer/main/Popups/Save As Popup/New File Popup/level-name").GetComponent<InputField>().characterValidation = InputField.CharacterValidation.None;
				GameObject.Find("Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/left/theme/name").GetComponent<InputField>().characterValidation = InputField.CharacterValidation.None;
				GameObject.Find("Editor GUI/sizer/main/EditorDialogs/PrefabDialog/data/name/input").GetComponent<InputField>().characterValidation = InputField.CharacterValidation.None;
				GameObject.Find("Editor GUI/sizer/main/Popups/New File Popup/Browser Popup").SetActive(true);
				GameObject.Find("Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/name/name").GetComponent<InputField>().characterLimit = 0;

				Destroy(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/autokill/tod-dropdown").GetComponent<HideDropdownOptions>());

				var exitToArcade = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/File/File Dropdown/Quit to Main Menu"));
				exitToArcade.name = "Quit to Arcade";
				exitToArcade.transform.localScale = Vector3.one;
				exitToArcade.transform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/File/File Dropdown").transform);
				exitToArcade.transform.SetSiblingIndex(7);
				exitToArcade.transform.GetChild(0).GetComponent<Text>().text = "Quit to Arcade";

				var ex = exitToArcade.GetComponent<Button>();
				ex.onClick.m_Calls.m_ExecutingCalls.Clear();
				ex.onClick.m_Calls.m_PersistentCalls.Clear();
				ex.onClick.m_PersistentCalls.m_Calls.Clear();
				ex.onClick.RemoveAllListeners();
				ex.onClick.AddListener(delegate ()
				{
					EditorManager.inst.ShowDialog("Warning Popup");
					RTEditor.RefreshWarningPopup("Are you sure you want to quit to the arcade?", delegate ()
					{
						DOTween.Clear();
						DataManager.inst.gameData = null;
						DataManager.inst.gameData = new DataManager.GameData();
						SceneManager.inst.LoadScene("Input Select");
					}, delegate ()
					{
						EditorManager.inst.HideDialog("Warning Popup");
					});
				});

				GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/Help/Help Dropdown/Join Discord/Text").GetComponent<Text>().text = "Modder's Discord";
				GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/Help/Help Dropdown/Watch Tutorials/Text").GetComponent<Text>().text = "Watch PA History";
				GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/Help/Help Dropdown/Community Guides").SetActive(false);
				GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/Help/Help Dropdown/Which songs can I use?").SetActive(false);
				GameObject.Find("TitleBar/File/File Dropdown/Save As").SetActive(true);
			}

			var timeObjET = timeObj.gameObject.GetComponent<EventTrigger>();

			timeObjET.triggers.Clear();
			timeObjET.triggers.Add(Triggers.ScrollDelta(iFtimeObj, 0.1f, 10f));

			//Loading doggo
			{
				GameObject loading = new GameObject("loading");
				GameObject popup = GameObject.Find("Editor Systems/Editor GUI/sizer/main/Popups");
				Transform fileInfoPopup = popup.transform.GetChild(1);

				loading.transform.SetParent(fileInfoPopup);
				loading.layer = 5;
				loading.transform.localScale = Vector3.one;

				RectTransform rtLoading = loading.AddComponent<RectTransform>();
				loading.AddComponent<CanvasRenderer>();
				Image iLoading = loading.AddComponent<Image>();
				LayoutElement leLoading = loading.AddComponent<LayoutElement>();

				rtLoading.anchoredPosition = new Vector2(0f, -75f);
				rtLoading.sizeDelta = new Vector2(122f, 122f);
				iLoading.sprite = EditorManager.inst.loadingImage.sprite;
				leLoading.ignoreLayout = true;

				fileInfoPopup.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(500f, 280f);

				var fileInfoPopup1 = EditorManager.inst.EditorDialogs[7].Dialog;
				fileInfoPopup1.gameObject.GetComponent<Image>().sprite = null;

				var fileInfoPopup2 = EditorManager.inst.EditorDialogs[1].Dialog;
				Sprite sprite = fileInfoPopup2.transform.Find("data/left/Scroll View/Viewport/Content/shape/7/Background").GetComponent<Image>().sprite;
			}

            //Pathing
            {
				GameObject sortList = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/autokill/tod-dropdown"));
				sortList.transform.SetParent(EditorManager.inst.GetDialog("Open File Popup").Dialog);

				RectTransform sortListRT = sortList.GetComponent<RectTransform>();
				sortListRT.anchoredPosition = ConfigEntries.ORLDropdownPos.Value;
				HoverTooltip sortListTip = sortList.GetComponent<HoverTooltip>();
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

				Dropdown.OptionData opt0 = new Dropdown.OptionData();
				Dropdown.OptionData opt1 = new Dropdown.OptionData();
				Dropdown.OptionData opt2 = new Dropdown.OptionData();
				Dropdown.OptionData opt3 = new Dropdown.OptionData();
				Dropdown.OptionData opt4 = new Dropdown.OptionData();
				Dropdown.OptionData opt5 = new Dropdown.OptionData();
				Dropdown.OptionData opt6 = new Dropdown.OptionData();

				opt0.text = "Cover";
				opt1.text = "Artist";
				opt2.text = "Creator";
				opt3.text = "Folder";
				opt4.text = "Title";
				opt5.text = "Difficulty";
				opt6.text = "Date Edited";

				sortListDD.options = new List<Dropdown.OptionData>
				{
					opt0,
					opt1,
					opt2,
					opt3,
					opt4,
					opt5,
					opt6
				};

				sortListDD.onValueChanged.AddListener(delegate (int _value)
				{
					EditorPlugin.levelFilter = _value;
					EditorManager.inst.RenderOpenBeatmapPopup();
				});

				GameObject checkDes = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/toggle"));
				checkDes.transform.SetParent(EditorManager.inst.GetDialog("Open File Popup").Dialog);
				GameObject toggle = checkDes.transform.Find("toggle").gameObject;
				checkDes.transform.localScale = Vector3.one;

				RectTransform checkDesRT = checkDes.GetComponent<RectTransform>();
				checkDesRT.anchoredPosition = ConfigEntries.ORLTogglePos.Value;

				checkDes.transform.Find("title").GetComponent<Text>().text = "Descending?";
				RectTransform titleRT = checkDes.transform.Find("title").GetComponent<RectTransform>();
				titleRT.sizeDelta = new Vector2(110f, 32f);
				toggle.GetComponent<Toggle>().isOn = true;
				toggle.GetComponent<Toggle>().onValueChanged.AddListener(delegate (bool _value)
				{
					EditorPlugin.levelAscend = _value;
					EditorManager.inst.RenderOpenBeatmapPopup();
				});
				HoverTooltip toggleTip = toggle.AddComponent<HoverTooltip>();
				toggleTip.tooltipLangauges.Add(sortListTip.tooltipLangauges[0]);
            }

            //Level List
            {
                if (RTFile.FileExists(RTFile.GetApplicationDirectory() + "beatmaps/editorpath.lss"))
                {
                    string rawProfileJSON = null;
                    rawProfileJSON = FileManager.inst.LoadJSONFile("beatmaps/editorpath.lss");

                    JSONNode jn = JSON.Parse(rawProfileJSON);

					if (!string.IsNullOrEmpty(jn["path"]))
					{
						EditorPlugin.editorPath = jn["path"];
						EditorPlugin.levelListPath = "beatmaps/" + EditorPlugin.editorPath;
						EditorPlugin.levelListSlash = "beatmaps/" + EditorPlugin.editorPath + "/";
					}

					if (!string.IsNullOrEmpty(jn["theme_path"]))
					{
						EditorPlugin.themePath = jn["theme_path"];
						EditorPlugin.themeListPath = "beatmaps/" + EditorPlugin.themePath;
						EditorPlugin.themeListSlash = "beatmaps/" + EditorPlugin.themePath + "/";
					}

					if (!string.IsNullOrEmpty(jn["prefab_path"]))
					{
						EditorPlugin.prefabPath = jn["prefab_path"];
						EditorPlugin.prefabListPath = "beatmaps/" + EditorPlugin.prefabPath;
						EditorPlugin.prefabListSlash = "beatmaps/" + EditorPlugin.prefabPath + "/";
					}

					JSONNode jn2 = JSON.Parse("{}");

					jn2["level_path"] = EditorPlugin.editorPath;

					jn2["theme_path"] = EditorPlugin.themePath;

					jn2["prefab_path"] = EditorPlugin.prefabPath;

					RTFile.WriteToFile("settings/editor.lss", jn2.ToString(3));
					File.Delete(RTFile.GetApplicationDirectory() + "beatmaps/editorpath.lss");
				}
                else if (RTFile.FileExists(RTFile.GetApplicationDirectory() + "settings/editor.lss"))
				{
					string rawProfileJSON = null;
					rawProfileJSON = FileManager.inst.LoadJSONFile("settings/editor.lss");

					JSONNode jn = JSON.Parse(rawProfileJSON);

					if (!string.IsNullOrEmpty(jn["level_path"]))
                    {
						EditorPlugin.editorPath = jn["level_path"];
						EditorPlugin.levelListPath = "beatmaps/" + EditorPlugin.editorPath;
						EditorPlugin.levelListSlash = "beatmaps/" + EditorPlugin.editorPath + "/";
					}

					if (!string.IsNullOrEmpty(jn["theme_path"]))
                    {
						EditorPlugin.themePath = jn["theme_path"];
						EditorPlugin.themeListPath = "beatmaps/" + EditorPlugin.themePath;
						EditorPlugin.themeListSlash = "beatmaps/" + EditorPlugin.themePath + "/";
					}
					
					if (!string.IsNullOrEmpty(jn["prefab_path"]))
					{
						EditorPlugin.prefabPath = jn["prefab_path"];
						EditorPlugin.prefabListPath = "beatmaps/" + EditorPlugin.prefabPath;
						EditorPlugin.prefabListSlash = "beatmaps/" + EditorPlugin.prefabPath + "/";
					}
				}
				else
				{
					JSONNode jn = JSON.Parse("{}");

					jn["level_path"] = EditorPlugin.editorPath;

					jn["theme_path"] = EditorPlugin.themePath;

					jn["prefab_path"] = EditorPlugin.prefabPath;

					RTFile.WriteToFile("settings/editor.lss", jn.ToString(3));
				}

				GameObject levelListObj = Instantiate(GameObject.Find("TimelineBar/GameObject/Time Input"));
				levelListObj.transform.SetParent(EditorManager.inst.GetDialog("Open File Popup").Dialog);
				levelListObj.GetComponent<RectTransform>().anchoredPosition = ConfigEntries.ORLPathPos.Value;
				levelListObj.GetComponent<RectTransform>().sizeDelta = new Vector2(ConfigEntries.ORLPathLength.Value, 34f);
				levelListObj.name = "editor path";

				HoverTooltip levelListTip = levelListObj.AddComponent<HoverTooltip>();
				HoverTooltip.Tooltip llTip = new HoverTooltip.Tooltip();

				llTip.desc = "Level list path";
				llTip.hint = "Input the path you want to load levels from within the beatmaps folder. For example: inputting \"editor\" into the input field will load levels from beatmaps/editor. You can also set it to sub-directories, like: \"editor/pa levels\" will take levels from \"beatmaps/editor/pa levels\".";

				levelListTip.tooltipLangauges.Add(llTip);

				InputField levelListIF = levelListObj.GetComponent<InputField>();
				levelListIF.characterValidation = InputField.CharacterValidation.None;
				levelListIF.text = EditorPlugin.editorPath;

				levelListIF.onValueChanged.RemoveAllListeners();
				levelListIF.onValueChanged.AddListener(delegate (string _val)
				{
					if (RTFile.FileExists(RTFile.GetApplicationDirectory() + "settings/editor.lss"))
					{
						string rawProfileJSON = null;
						rawProfileJSON = FileManager.inst.LoadJSONFile("settings/editor.lss");

						JSONNode jsonnode = JSON.Parse(rawProfileJSON);

						jsonnode["level_path"] = _val;

						RTFile.WriteToFile("settings/editor.lss", jsonnode.ToString(3));
					}

					EditorPlugin.editorPath = _val;
					EditorPlugin.levelListPath = "beatmaps/" + EditorPlugin.editorPath;
					EditorPlugin.levelListSlash = "beatmaps/" + EditorPlugin.editorPath + "/";
				});

				GameObject levelListReloader = Instantiate(GameObject.Find("TimelineBar/GameObject/play"));
				levelListReloader.transform.SetParent(EditorManager.inst.GetDialog("Open File Popup").Dialog);
				levelListReloader.GetComponent<RectTransform>().anchoredPosition = ConfigEntries.ORLRefreshPos.Value;
				levelListReloader.GetComponent<RectTransform>().sizeDelta = new Vector2(32f, 32f);
				levelListReloader.name = "reload";

				HoverTooltip levelListRTip = levelListReloader.AddComponent<HoverTooltip>();
				HoverTooltip.Tooltip llRTip = new HoverTooltip.Tooltip();

				llRTip.desc = "Refresh level list";
				llRTip.hint = "Clicking this will reload the level list.";

				levelListRTip.tooltipLangauges.Add(llRTip);

				Button levelListRButton = levelListReloader.GetComponent<Button>();
				levelListRButton.onClick.m_Calls.m_ExecutingCalls.Clear();
				levelListRButton.onClick.m_Calls.m_PersistentCalls.Clear();
				levelListRButton.onClick.m_PersistentCalls.m_Calls.Clear();
				levelListRButton.onClick.RemoveAllListeners();
				levelListRButton.onClick.AddListener(delegate ()
				{
					EditorManager.inst.GetLevelList();
				});

				string jpgFileLocation = RTFile.GetApplicationDirectory() + "BepInEx/plugins/Assets/editor_gui_refresh-white.png";

				if (RTFile.FileExists("BepInEx/plugins/Assets/editor_gui_refresh-white.png"))
				{
					Image spriteReloader = levelListReloader.GetComponent<Image>();

					EditorManager.inst.StartCoroutine(EditorManager.inst.GetSprite(jpgFileLocation, new EditorManager.SpriteLimits(), delegate (Sprite cover)
					{
						spriteReloader.sprite = cover;
					}, delegate (string errorFile)
					{
						spriteReloader.sprite = ArcadeManager.inst.defaultImage;
					}));
				}
			}

			//New triggers
			{
				RTEditor.inst.StartCoroutine(RTEditor.SetupTimelineTriggers());
			}

			//Objects
			{
				var persistent = __instance.GetDialog("Object Options Popup").Dialog.Find("persistent").gameObject.GetComponent<Button>();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("persistent/text").gameObject.GetComponent<Text>().text = "No Autokill";
				persistent.onClick.m_Calls.m_ExecutingCalls.Clear();
				persistent.onClick.m_Calls.m_PersistentCalls.Clear();
				persistent.onClick.m_PersistentCalls.m_Calls.Clear();
				persistent.onClick.RemoveAllListeners();
				persistent.onClick.AddListener(delegate ()
				{
					RTEditor.CreateNewNoAutokillObject();
				});

				var empty = __instance.GetDialog("Object Options Popup").Dialog.Find("empty").gameObject.GetComponent<Button>();
				empty.onClick.m_Calls.m_ExecutingCalls.Clear();
				empty.onClick.m_Calls.m_PersistentCalls.Clear();
				empty.onClick.m_PersistentCalls.m_Calls.Clear();
				empty.onClick.RemoveAllListeners();
				empty.onClick.AddListener(delegate ()
				{
					RTEditor.CreateNewEmptyObject();
				});

				var decoration = __instance.GetDialog("Object Options Popup").Dialog.Find("decoration").gameObject.GetComponent<Button>();
				decoration.onClick.m_Calls.m_ExecutingCalls.Clear();
				decoration.onClick.m_Calls.m_PersistentCalls.Clear();
				decoration.onClick.m_PersistentCalls.m_Calls.Clear();
				decoration.onClick.RemoveAllListeners();
				decoration.onClick.AddListener(delegate ()
				{
					RTEditor.CreateNewDecorationObject();
				});

				var helper = __instance.GetDialog("Object Options Popup").Dialog.Find("helper").gameObject.GetComponent<Button>();
				helper.onClick.m_Calls.m_ExecutingCalls.Clear();
				helper.onClick.m_Calls.m_PersistentCalls.Clear();
				helper.onClick.m_PersistentCalls.m_Calls.Clear();
				helper.onClick.RemoveAllListeners();
				helper.onClick.AddListener(delegate ()
				{
					RTEditor.CreateNewHelperObject();
				});

				var normal = __instance.GetDialog("Object Options Popup").Dialog.Find("normal").gameObject.GetComponent<Button>();
				normal.onClick.m_Calls.m_ExecutingCalls.Clear();
				normal.onClick.m_Calls.m_PersistentCalls.Clear();
				normal.onClick.m_PersistentCalls.m_Calls.Clear();
				normal.onClick.RemoveAllListeners();
				normal.onClick.AddListener(delegate ()
				{
					RTEditor.CreateNewNormalObject();
				});

				var circle = __instance.GetDialog("Object Options Popup").Dialog.Find("shapes/circle").gameObject.GetComponent<Button>();
				circle.onClick.m_Calls.m_ExecutingCalls.Clear();
				circle.onClick.m_Calls.m_PersistentCalls.Clear();
				circle.onClick.m_PersistentCalls.m_Calls.Clear();
				circle.onClick.RemoveAllListeners();
				circle.onClick.AddListener(delegate ()
				{
					RTEditor.CreateNewCircleObject();
				});

				var triangle = __instance.GetDialog("Object Options Popup").Dialog.Find("shapes/triangle").gameObject.GetComponent<Button>();
				triangle.onClick.m_Calls.m_ExecutingCalls.Clear();
				triangle.onClick.m_Calls.m_PersistentCalls.Clear();
				triangle.onClick.m_PersistentCalls.m_Calls.Clear();
				triangle.onClick.RemoveAllListeners();
				triangle.onClick.AddListener(delegate ()
				{
					RTEditor.CreateNewTriangleObject();
				});

				var text = __instance.GetDialog("Object Options Popup").Dialog.Find("shapes/text").gameObject.GetComponent<Button>();
				text.onClick.m_Calls.m_ExecutingCalls.Clear();
				text.onClick.m_Calls.m_PersistentCalls.Clear();
				text.onClick.m_PersistentCalls.m_Calls.Clear();
				text.onClick.RemoveAllListeners();
				text.onClick.AddListener(delegate ()
				{
					RTEditor.CreateNewTextObject();
				});

				var hexagon = __instance.GetDialog("Object Options Popup").Dialog.Find("shapes/hexagon").gameObject.GetComponent<Button>();
				hexagon.onClick.m_Calls.m_ExecutingCalls.Clear();
				hexagon.onClick.m_Calls.m_PersistentCalls.Clear();
				hexagon.onClick.m_PersistentCalls.m_Calls.Clear();
				hexagon.onClick.RemoveAllListeners();
				hexagon.onClick.AddListener(delegate ()
				{
					RTEditor.CreateNewHexagonObject();
				});
			}

			//Select UI
			{
				var openFilePopupSelect = openFilePopup.gameObject.AddComponent<SelectUI>();
				openFilePopupSelect.target = openFilePopup;
				openFilePopupSelect.ogPos = openFilePopup.position;

				var parentSelectorSelect = parentSelector.gameObject.AddComponent<SelectUI>();
				parentSelectorSelect.target = parentSelector;
				parentSelectorSelect.ogPos = parentSelector.position;

				var saveAsPopupSelect = saveAsPopup.Find("New File Popup").gameObject.AddComponent<SelectUI>();
				saveAsPopupSelect.target = saveAsPopup;
				saveAsPopupSelect.ogPos = saveAsPopup.position;

				var quickActionsPopupSelect = quickActionsPopup.gameObject.AddComponent<SelectUI>();
				quickActionsPopupSelect.target = quickActionsPopup;
				quickActionsPopupSelect.ogPos = quickActionsPopup.position;
			}

			//Cursor Color
			{
				GameObject.Find("Editor Systems/Editor GUI/sizer/main/whole-timeline/Slider_Parent/Slider/Handle Slide Area/Image/Handle").GetComponent<Image>().color = ConfigEntries.MTSliderCol.Value;
				GameObject.Find("Editor Systems/Editor GUI/sizer/main/whole-timeline/Slider_Parent/Slider/Handle Slide Area/Image").GetComponent<Image>().color = ConfigEntries.MTSliderCol.Value;
				GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/timeline/Scroll View/Viewport/Content/time_slider/Handle Slide Area/Handle/Image").GetComponent<Image>().color = ConfigEntries.KTSliderCol.Value;
				GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/timeline/Scroll View/Viewport/Content/time_slider/Handle Slide Area/Handle").GetComponent<Image>().color = ConfigEntries.KTSliderCol.Value;
			}

			//Theme Thing
			{
				GameObject themePather = Instantiate(EditorManager.inst.GetDialog("Event Editor").Dialog.Find("data/right/theme").GetChild(2).gameObject);
				themePather.transform.SetParent(EditorManager.inst.GetDialog("Event Editor").Dialog.Find("data/right/theme"));
				themePather.transform.SetSiblingIndex(8);
				themePather.name = "themepathers";

				GameObject levelListObj = Instantiate(timeObj);
				levelListObj.transform.SetParent(themePather.transform);
				levelListObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(150f, 0f);
				levelListObj.GetComponent<RectTransform>().sizeDelta = new Vector2(300f, 34f);
				levelListObj.transform.localScale = Vector3.one;
				levelListObj.name = "themes path";

				HoverTooltip levelListTip = levelListObj.AddComponent<HoverTooltip>();
				HoverTooltip.Tooltip llTip = new HoverTooltip.Tooltip();

				llTip.desc = "Theme list path";
				llTip.hint = "Input the path you want to load themes from within the beatmaps folder. For example: inputting \"themes\" into the input field will load themes from beatmaps/themes. You can also set it to sub-directories, like: \"themes/pa colors\" will take levels from \"beatmaps/themes/pa colors\".";

				levelListTip.tooltipLangauges.Add(llTip);

				InputField levelListIF = levelListObj.GetComponent<InputField>();
				levelListIF.characterValidation = InputField.CharacterValidation.None;
				levelListIF.text = EditorPlugin.themePath;

				levelListIF.onValueChanged.RemoveAllListeners();
				levelListIF.onValueChanged.AddListener(delegate (string _val)
				{
					if (RTFile.FileExists(RTFile.GetApplicationDirectory() + "settings/editor.lss"))
					{
						string rawProfileJSON = null;
						rawProfileJSON = FileManager.inst.LoadJSONFile("settings/editor.lss");

						JSONNode jn = JSON.Parse(rawProfileJSON);

						jn["theme_path"] = _val;

						RTFile.WriteToFile("settings/editor.lss", jn.ToString(3));
					}

					EditorPlugin.themePath = _val;
					EditorPlugin.themeListPath = "beatmaps/" + EditorPlugin.themePath;
					EditorPlugin.themeListSlash = "beatmaps/" + EditorPlugin.themePath + "/";
				});

				GameObject levelListReloader = Instantiate(GameObject.Find("TimelineBar/GameObject/play"));
				levelListReloader.transform.SetParent(EditorManager.inst.GetDialog("Event Editor").Dialog.Find("data/right/theme/themepathers"));
				levelListReloader.GetComponent<RectTransform>().anchoredPosition = new Vector2(310f, 35f);
				levelListReloader.GetComponent<RectTransform>().sizeDelta = new Vector2(32f, 32f);
				levelListReloader.name = "reload themes";

				HoverTooltip levelListRTip = levelListReloader.AddComponent<HoverTooltip>();
				HoverTooltip.Tooltip llRTip = new HoverTooltip.Tooltip();

				llRTip.desc = "Refresh theme list";
				llRTip.hint = "Clicking this will reload the theme list.";

				levelListRTip.tooltipLangauges.Add(llRTip);

				Button levelListRButton = levelListReloader.GetComponent<Button>();
				levelListRButton.onClick.m_Calls.m_ExecutingCalls.Clear();
				levelListRButton.onClick.m_Calls.m_PersistentCalls.Clear();
				levelListRButton.onClick.m_PersistentCalls.m_Calls.Clear();
				levelListRButton.onClick.RemoveAllListeners();
				levelListRButton.onClick.AddListener(delegate ()
				{
					RTEditor.inst.StartCoroutine(RTEditor.LoadThemes());
					EventEditor.inst.RenderEventsDialog();
				});

				string jpgFileLocation = RTFile.GetApplicationDirectory() + "BepInEx/plugins/Assets/editor_gui_refresh-white.png";

				if (RTFile.FileExists("BepInEx/plugins/Assets/editor_gui_refresh-white.png"))
				{
					Image spriteReloader = levelListReloader.GetComponent<Image>();

					EditorManager.inst.StartCoroutine(EditorManager.inst.GetSprite(jpgFileLocation, new EditorManager.SpriteLimits(), delegate (Sprite cover)
					{
						spriteReloader.sprite = cover;
					}, delegate (string errorFile)
					{
						spriteReloader.sprite = ArcadeManager.inst.defaultImage;
					}));
				}
			}

            //Prefabs
            {
				GameObject levelListObj = Instantiate(timeObj);
				levelListObj.transform.SetParent(EditorManager.inst.GetDialog("Prefab Popup").Dialog.Find("external prefabs"));
				levelListObj.transform.localScale = Vector3.one;

				foreach (object obj in levelListObj.transform)
				{
					Transform children = (Transform)obj;
					children.localScale = Vector3.one;
				}

				levelListObj.GetComponent<RectTransform>().anchoredPosition = ConfigEntries.PrefabEXPathPos.Value;
				levelListObj.GetComponent<RectTransform>().sizeDelta = new Vector2(ConfigEntries.PrefabEXPathSca.Value, 34f);
				levelListObj.name = "prefabs path";

				HoverTooltip levelListTip = levelListObj.AddComponent<HoverTooltip>();
				HoverTooltip.Tooltip llTip = new HoverTooltip.Tooltip();
				{
					llTip.desc = "Prefab list path";
					llTip.hint = "Input the path you want to load prefabs from within the beatmaps folder. For example: inputting \"prefabs\" into the input field will load levels from beatmaps/prefabs. You can also set it to sub-directories, like: \"prefabs/pa characters\" will take levels from \"beatmaps/prefabs/pa characters\".";
				}

				levelListTip.tooltipLangauges.Add(llTip);

				InputField levelListIF = levelListObj.GetComponent<InputField>();
				levelListIF.characterValidation = InputField.CharacterValidation.None;
				levelListIF.text = EditorPlugin.prefabPath;

				levelListIF.onValueChanged.RemoveAllListeners();
				levelListIF.onValueChanged.AddListener(delegate (string _val)
				{
					if (RTFile.FileExists(RTFile.GetApplicationDirectory() + "settings/editor.lss"))
					{
						string rawProfileJSON = null;
						rawProfileJSON = FileManager.inst.LoadJSONFile("settings/editor.lss");

						JSONNode jsonnode = JSON.Parse(rawProfileJSON);

						jsonnode["prefab_path"] = _val;

						RTFile.WriteToFile("settings/editor.lss", jsonnode.ToString(3));
					}

					EditorPlugin.prefabPath = _val;
					EditorPlugin.prefabListPath = "beatmaps/" + EditorPlugin.prefabPath;
					EditorPlugin.prefabListSlash = "beatmaps/" + EditorPlugin.prefabPath + "/";
				});

				GameObject levelListReloader = Instantiate(GameObject.Find("TimelineBar/GameObject/play"));
				levelListReloader.transform.SetParent(EditorManager.inst.GetDialog("Prefab Popup").Dialog.Find("external prefabs"));
				levelListReloader.transform.localScale = Vector3.one;
				levelListReloader.GetComponent<RectTransform>().anchoredPosition = ConfigEntries.PrefabEXRefreshPos.Value;
				levelListReloader.GetComponent<RectTransform>().sizeDelta = new Vector2(32f, 32f);
				levelListReloader.name = "reload prefabs";

				HoverTooltip levelListRTip = levelListReloader.AddComponent<HoverTooltip>();
				HoverTooltip.Tooltip llRTip = new HoverTooltip.Tooltip();
				{
					llRTip.desc = "Refresh prefab list";
					llRTip.hint = "Clicking this will reload the prefab list.";
				}

				levelListRTip.tooltipLangauges.Add(llRTip);

				Button levelListRButton = levelListReloader.GetComponent<Button>();
				levelListRButton.onClick.m_Calls.m_ExecutingCalls.Clear();
				levelListRButton.onClick.m_Calls.m_PersistentCalls.Clear();
				levelListRButton.onClick.m_PersistentCalls.m_Calls.Clear();
				levelListRButton.onClick.RemoveAllListeners();
				levelListRButton.onClick.AddListener(delegate ()
				{
					EditorPlugin.inst.StartReload();
				});

				string jpgFileLocation = RTFile.GetApplicationDirectory() + "BepInEx/plugins/Assets/editor_gui_refresh-white.png";

				if (RTFile.FileExists("BepInEx/plugins/Assets/editor_gui_refresh-white.png"))
				{
					Image spriteReloader = levelListReloader.GetComponent<Image>();

					EditorManager.inst.StartCoroutine(EditorManager.inst.GetSprite(jpgFileLocation, new EditorManager.SpriteLimits(), delegate (Sprite cover)
					{
						spriteReloader.sprite = cover;
					}, delegate (string errorFile)
					{
						spriteReloader.sprite = ArcadeManager.inst.defaultImage;
					}));
				}
			}

			if (keybindsType != null && GetKeyCodeName != null)
			{
				GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/File/File Dropdown/New Level/Shortcut").SetActive(true);
				string newLevelMod = GetKeyCodeName.Invoke(keybindsType, new object[] { "New Level", false }).ToString().Replace("Left", "Left ").Replace("Right", "Right ");
				string newLevelMai = GetKeyCodeName.Invoke(keybindsType, new object[] { "New Level", true }).ToString().Replace("Left", "Left ").Replace("Right", "Right ");
				GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/File/File Dropdown/New Level/Shortcut").GetComponent<Text>().text = newLevelMod + " + " + newLevelMai;

				string saveAsMod = GetKeyCodeName.Invoke(keybindsType, new object[] { "Save Beatmap As", false }).ToString().Replace("Left", "Left ").Replace("Right", "Right ");
				string saveAsMai = GetKeyCodeName.Invoke(keybindsType, new object[] { "Save Beatmap As", true }).ToString().Replace("Left", "Left ").Replace("Right", "Right ");
				GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/File/File Dropdown/Save As/Shortcut").GetComponent<Text>().text = saveAsMod + " + " + saveAsMai;

				string openMod = GetKeyCodeName.Invoke(keybindsType, new object[] { "Open Beatmap Popup", false }).ToString().Replace("Left", "Left ").Replace("Right", "Right ");
				string openMai = GetKeyCodeName.Invoke(keybindsType, new object[] { "Open Beatmap Popup", true }).ToString().Replace("Left", "Left ").Replace("Right", "Right ");
				GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/File/File Dropdown/Save As/Shortcut").GetComponent<Text>().text = openMod + " + " + openMai;
			}

			//Animate in GUI
			{
				AnimateInGUI openFilePopupAIGUI = openFilePopup.gameObject.AddComponent<AnimateInGUI>();
				AnimationCurve animationCurve = new AnimationCurve();
				animationCurve.postWrapMode = WrapMode.Clamp;
				animationCurve.preWrapMode = WrapMode.Clamp;

				openFilePopupAIGUI.SetEasing((int)ConfigEntries.OFPAnimateEaseIn.Value, (int)ConfigEntries.OFPAnimateEaseOut.Value);
				openFilePopupAIGUI.animateX = ConfigEntries.OFPAnimateX.Value;
				openFilePopupAIGUI.animateY = ConfigEntries.OFPAnimateY.Value;
				openFilePopupAIGUI.animateInTime = ConfigEntries.OFPAnimateInOutSpeeds.Value.x;
				openFilePopupAIGUI.animateOutTime = ConfigEntries.OFPAnimateInOutSpeeds.Value.y;

				var newFilePopupAIGUI = newFilePopup.gameObject.AddComponent<AnimateInGUI>();

				newFilePopupAIGUI.SetEasing((int)ConfigEntries.NFPAnimateEaseIn.Value, (int)ConfigEntries.NFPAnimateEaseOut.Value);
				newFilePopupAIGUI.animateX = ConfigEntries.NFPAnimateX.Value;
				newFilePopupAIGUI.animateY = ConfigEntries.NFPAnimateY.Value;
				newFilePopupAIGUI.animateInTime = ConfigEntries.NFPAnimateInOutSpeeds.Value.x;
				newFilePopupAIGUI.animateOutTime = ConfigEntries.NFPAnimateInOutSpeeds.Value.y;

				var prefabPopupAIGUI = prefabPopup.gameObject.AddComponent<AnimateInGUI>();

				prefabPopupAIGUI.SetEasing((int)ConfigEntries.PPAnimateEaseIn.Value, (int)ConfigEntries.PPAnimateEaseOut.Value);
				prefabPopupAIGUI.animateX = ConfigEntries.PPAnimateX.Value;
				prefabPopupAIGUI.animateY = ConfigEntries.PPAnimateY.Value;
				prefabPopupAIGUI.animateInTime = ConfigEntries.PPAnimateInOutSpeeds.Value.x;
				prefabPopupAIGUI.animateOutTime = ConfigEntries.PPAnimateInOutSpeeds.Value.y;

				var quickActionsPopupAIGUI = quickActionsPopup.gameObject.AddComponent<AnimateInGUI>();

				quickActionsPopupAIGUI.SetEasing((int)ConfigEntries.QAPAnimateEaseIn.Value, (int)ConfigEntries.QAPAnimateEaseOut.Value);
				quickActionsPopupAIGUI.animateX = ConfigEntries.QAPAnimateX.Value;
				quickActionsPopupAIGUI.animateY = ConfigEntries.QAPAnimateY.Value;
				quickActionsPopupAIGUI.animateInTime = ConfigEntries.QAPAnimateInOutSpeeds.Value.x;
				quickActionsPopupAIGUI.animateOutTime = ConfigEntries.QAPAnimateInOutSpeeds.Value.y;

				var objectOptionsPopupAIGUI = EditorManager.inst.GetDialog("Object Options Popup").Dialog.gameObject.AddComponent<AnimateInGUI>();

				objectOptionsPopupAIGUI.SetEasing((int)ConfigEntries.OBJPAnimateEaseIn.Value, (int)ConfigEntries.OBJPAnimateEaseOut.Value);
				objectOptionsPopupAIGUI.animateX = ConfigEntries.OBJPAnimateX.Value;
				objectOptionsPopupAIGUI.animateY = ConfigEntries.OBJPAnimateY.Value;
				objectOptionsPopupAIGUI.animateInTime = ConfigEntries.OBJPAnimateInOutSpeeds.Value.x;
				objectOptionsPopupAIGUI.animateOutTime = ConfigEntries.OBJPAnimateInOutSpeeds.Value.y;

				var bgOptionsPopupAIGUI = EditorManager.inst.GetDialog("BG Options Popup").Dialog.gameObject.AddComponent<AnimateInGUI>();

				bgOptionsPopupAIGUI.SetEasing((int)ConfigEntries.BGPAnimateEaseIn.Value, (int)ConfigEntries.BGPAnimateEaseOut.Value);
				bgOptionsPopupAIGUI.animateX = ConfigEntries.BGPAnimateX.Value;
				bgOptionsPopupAIGUI.animateY = ConfigEntries.BGPAnimateY.Value;
				bgOptionsPopupAIGUI.animateInTime = ConfigEntries.BGPAnimateInOutSpeeds.Value.x;
				bgOptionsPopupAIGUI.animateOutTime = ConfigEntries.BGPAnimateInOutSpeeds.Value.y;

				var gameObjectDialogAIGUI = EditorManager.inst.GetDialog("Object Editor").Dialog.gameObject.AddComponent<AnimateInGUI>();

				gameObjectDialogAIGUI.SetEasing((int)ConfigEntries.GODAnimateEaseIn.Value, (int)ConfigEntries.GODAnimateEaseOut.Value);
				gameObjectDialogAIGUI.animateX = ConfigEntries.GODAnimateX.Value;
				gameObjectDialogAIGUI.animateY = ConfigEntries.GODAnimateY.Value;
				gameObjectDialogAIGUI.animateInTime = ConfigEntries.GODAnimateInOutSpeeds.Value.x;
				gameObjectDialogAIGUI.animateOutTime = ConfigEntries.GODAnimateInOutSpeeds.Value.y;
			}

			var hoverUIPrefab = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject/prefab").AddComponent<HoverUI>();
            {
				hoverUIPrefab.animatePos = true;
				hoverUIPrefab.animateSca = true;
				hoverUIPrefab.animPos = new Vector3(3f, 0f, 0f);
            }

			var hoverUIObject = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject/object").AddComponent<HoverUI>();
            {
				hoverUIObject.animatePos = true;
				hoverUIObject.animateSca = true;
				hoverUIObject.animPos = new Vector3(3f, 0f, 0f);
            }

			var hoverUIEvent = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject/event").AddComponent<HoverUI>();
            {
				hoverUIEvent.animatePos = true;
				hoverUIEvent.animateSca = true;
				hoverUIEvent.animPos = new Vector3(3f, 0f, 0f);
            }

			var hoverUIBG = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject/background").AddComponent<HoverUI>();
            {
				hoverUIBG.animatePos = true;
				hoverUIBG.animateSca = true;
				hoverUIBG.animPos = new Vector3(3f, 0f, 0f);
            }

			var hoverUIPlayTest = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject/playtest").AddComponent<HoverUI>();
            {
				hoverUIPlayTest.animatePos = false;
				hoverUIPlayTest.animateSca = true;
            }

			RTEditor.SearchObjectsCreator();
			RTEditor.WarningPopupCreator();
			RTEditor.inst.StartCoroutine(RTEditor.SetupTooltips());
		}

		[HarmonyPatch("Awake")]
		[HarmonyPostfix]
		private static void PropertiesWindow()
        {
			RTEditor.inst.StartCoroutine(RTEditor.CreatePropertiesWindow());
			GameObject gameObject = new GameObject("PlayerEditorManager");
			gameObject.transform.SetParent(GameObject.Find("Editor Systems").transform);
			gameObject.AddComponent<CreativePlayersEditor>();
		}

		[HarmonyPatch("Start")]
		[HarmonyPostfix]
		private static void EditorStartPatch()
		{
			EditorPlugin.RepeatReminder();

			GameObject folderButton = EditorManager.inst.folderButtonPrefab;
			Button fButtonBUTT = folderButton.GetComponent<Button>();
			Text fButtonText = folderButton.transform.Find("folder-name").GetComponent<Text>();
			//Folder button
			fButtonText.horizontalOverflow = ConfigEntries.FButtonHWrap.Value;
			fButtonText.verticalOverflow = ConfigEntries.FButtonVWrap.Value;
			fButtonText.color = ConfigEntries.FButtonTextColor.Value;
			fButtonText.fontSize = ConfigEntries.FButtonFontSize.Value;

			//Folder Button Colors
			ColorBlock cb = fButtonBUTT.colors;
			cb.normalColor = ConfigEntries.FButtonNColor.Value;
			cb.pressedColor = ConfigEntries.FButtonPColor.Value;
			cb.highlightedColor = ConfigEntries.FButtonHColor.Value;
			cb.selectedColor = ConfigEntries.FButtonSColor.Value;
			cb.fadeDuration = ConfigEntries.FButtonFadeDColor.Value;
			fButtonBUTT.colors = cb;

			EditorPlugin.timeEdit = EditorPlugin.itsTheTime;

			InputDataManager.inst.editorActions.Cut.ClearBindings();
			InputDataManager.inst.editorActions.Copy.ClearBindings();
			InputDataManager.inst.editorActions.Paste.ClearBindings();
			InputDataManager.inst.editorActions.Duplicate.ClearBindings();
			InputDataManager.inst.editorActions.Delete.ClearBindings();
			InputDataManager.inst.editorActions.Undo.ClearBindings();
			InputDataManager.inst.editorActions.Redo.ClearBindings();

			RTEditor.inst.StartCoroutine(RTEditor.StartEditorGUI());
		}

		[HarmonyPatch("Update")]
		[HarmonyPostfix]
		private static void EditorUpdatePatch()
		{
			if (EditorManager.inst.GUI.activeSelf == true && EditorManager.inst.isEditing == true)
			{
				//Create Local Variables
				GameObject timeObj = GameObject.Find("TimelineBar/GameObject/Time Input");
				InputField iFtimeObj = timeObj.GetComponent<InputField>();

				if (!iFtimeObj.isFocused)
				{
					iFtimeObj.text = AudioManager.inst.CurrentAudioSource.time.ToString();
				}

				//bool playingEd = AudioManager.inst.CurrentAudioSource.isPlaying;

				//if (playingEd == true)
				//{
				//	Vector2 point = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
				//	float value = EditorManager.inst.timelineScrollbar.GetComponent<Scrollbar>().value;
				//	Rect rect = new Rect(0f, 0.305f * (float)Screen.height, (float)Screen.width, (float)Screen.height * 0.025f);
				//	if (Input.GetMouseButton(0) && rect.Contains(point))
				//	{
				//		if (Mathf.Abs(EditorManager.inst.audioTimeForSlider / EditorManager.inst.Zoom - EditorManager.inst.prevAudioTime) < 100f)
				//		{
				//			AudioManager.inst.CurrentAudioSource.Play();
				//			EditorManager.inst.UpdatePlayButton();
				//		}
				//	}
				//}
				if (GameObject.Find("Editor Systems/Editor GUI/sizer/main/Popups/File Info Popup/loading") && GameObject.Find("Editor Systems/Editor GUI/sizer/main/Popups/File Info Popup/loading").activeSelf == true)
				{
					Image image = GameObject.Find("Editor Systems/Editor GUI/sizer/main/Popups/File Info Popup/loading").GetComponent<Image>();
					image.sprite = EditorManager.inst.loadingImage.sprite;
				}
			}
			EditorPlugin.itsTheTime = EditorPlugin.timeEdit + Time.time;

			if (EditorManager.inst.GetDialog("Multi Object Editor").Dialog.gameObject.activeSelf == true && EditorManager.inst.GetDialog("Multi Object Editor").Dialog.Find("data").GetComponent<RectTransform>().sizeDelta != new Vector2(810f, 730.11f))
			{
				EditorManager.inst.GetDialog("Multi Object Editor").Dialog.Find("data").GetComponent<RectTransform>().sizeDelta = new Vector2(810f, 730.11f);
			}
			if (EditorManager.inst.GetDialog("Multi Object Editor").Dialog.gameObject.activeSelf == true && EditorManager.inst.GetDialog("Multi Object Editor").Dialog.Find("data/left").GetComponent<RectTransform>().sizeDelta != new Vector2(355f, 730f))
			{
				EditorManager.inst.GetDialog("Multi Object Editor").Dialog.Find("data/left").GetComponent<RectTransform>().sizeDelta = new Vector2(355f, 730f);
			}

			if (!LSHelpers.IsUsingInputField())
			{
				if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.X))
				{
					RTEditor.Cut();
				}
				if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.C))
				{
					RTEditor.Copy(false, false);
				}
				if (Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.V))
				{
					if (RTEditor.ienumRunning == false)
					{
						EditorManager.inst.DisplayNotification("Performing task, please wait...", 1f, EditorManager.NotificationType.Success);
						RTEditor.Paste(0f);
					}
					else
					{
						EditorManager.inst.DisplayNotification("Wait until current task is complete!", 1f, EditorManager.NotificationType.Warning);
					}
				}
				if (Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.D))
				{
					if (RTEditor.ienumRunning == false)
					{
						EditorManager.inst.DisplayNotification("Performing task, please wait...", 1f, EditorManager.NotificationType.Success);
						RTEditor.Duplicate();
					}
					else
					{
						EditorManager.inst.DisplayNotification("Wait until current task is complete!", 1f, EditorManager.NotificationType.Warning);
					}
				}
				if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.V))
				{
					if (RTEditor.ienumRunning == false)
					{
						EditorManager.inst.DisplayNotification("Performing task, please wait...", 1f, EditorManager.NotificationType.Success);
						RTEditor.Paste(0f, false);
					}
					else
					{
						EditorManager.inst.DisplayNotification("Wait until current task is complete!", 1f, EditorManager.NotificationType.Warning);
					}
				}
				if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.D))
				{
					if (RTEditor.ienumRunning == false)
					{
						EditorManager.inst.DisplayNotification("Performing task, please wait...", 1f, EditorManager.NotificationType.Success);
						RTEditor.Duplicate(false);
					}
					else
					{
						EditorManager.inst.DisplayNotification("Wait until current task is complete!", 1f, EditorManager.NotificationType.Warning);
					}
				}
				if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete))
				{
					if (RTEditor.ienumRunning == false)
					{
						EditorManager.inst.DisplayNotification("Performing task, please wait...", 1f, EditorManager.NotificationType.Success);
						RTEditor.Delete();
					}
					else
					{
						EditorManager.inst.DisplayNotification("Wait until current task is complete!", 1f, EditorManager.NotificationType.Warning);
					}
				}
				if (Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Z))
				{
					if (RTEditor.ienumRunning == false)
					{
						EditorManager.inst.DisplayNotification("Performing task, please wait...", 1f, EditorManager.NotificationType.Success);
						EditorManager.inst.history.Undo();
					}
					else
					{
						EditorManager.inst.DisplayNotification("Wait until current task is complete!", 1f, EditorManager.NotificationType.Warning);
					}
				}
				if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Z))
				{
					if (RTEditor.ienumRunning == false)
					{
						EditorManager.inst.DisplayNotification("Performing task, please wait...", 1f, EditorManager.NotificationType.Success);
						EditorManager.inst.history.Redo();
					}
					else
					{
						EditorManager.inst.DisplayNotification("Wait until current task is complete!", 1f, EditorManager.NotificationType.Warning);
					}
				}

				if (Input.GetMouseButtonDown(2))
				{
					EditorPlugin.ListObjectLayers();
				}

				if (Input.GetKeyDown(KeyCode.PageUp))
				{
					int x = int.Parse(layersIF.text);
					x += 1;
					layersIF.text = x.ToString();
				}
				if (Input.GetKeyDown(KeyCode.PageDown))
				{
					int x = int.Parse(layersIF.text);
					x -= 1;
					layersIF.text = x.ToString();
				}
			}
		}

		[HarmonyPatch("handleViewShortcuts")]
		[HarmonyPrefix]
		private static bool ViewShortcutsPatch()
        {
			if (EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Object)
				&& !EditorManager.inst.IsOverDropDown
				&& EditorManager.inst.IsOverObjTimeline
				&& !LSHelpers.IsUsingInputField()
				&& !IsOverMainTimeline)
			{
				if (InputDataManager.inst.editorActions.ZoomIn.WasPressed && !Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.LeftShift))
				{
					ObjEditor.inst.Zoom = ObjEditor.inst.zoomFloat + ConfigEntries.ZoomAmount.Value;
				}
				if (InputDataManager.inst.editorActions.ZoomOut.WasPressed && !Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.LeftShift))
				{
					ObjEditor.inst.Zoom = ObjEditor.inst.zoomFloat - ConfigEntries.ZoomAmount.Value;
				}

				//Zooms more
				if (InputDataManager.inst.editorActions.ZoomIn.WasPressed && Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftAlt))
				{
					ObjEditor.inst.Zoom = ObjEditor.inst.zoomFloat + ConfigEntries.ZoomAmount.Value * 2f;
				}
				if (InputDataManager.inst.editorActions.ZoomOut.WasPressed && Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftAlt))
				{
					ObjEditor.inst.Zoom = ObjEditor.inst.zoomFloat - ConfigEntries.ZoomAmount.Value * 2f;
				}

				//Zooms less
				if (InputDataManager.inst.editorActions.ZoomIn.WasPressed && Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.LeftShift))
				{
					ObjEditor.inst.Zoom = ObjEditor.inst.zoomFloat + ConfigEntries.ZoomAmount.Value * 0.5f;
				}
				if (InputDataManager.inst.editorActions.ZoomOut.WasPressed && Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.LeftShift))
				{
					ObjEditor.inst.Zoom = ObjEditor.inst.zoomFloat - ConfigEntries.ZoomAmount.Value * 0.5f;
				}
			}
			if (!EditorManager.inst.IsOverDropDown && !EditorManager.inst.IsOverObjTimeline && IsOverMainTimeline)
			{
				if (InputDataManager.inst.editorActions.ZoomIn.WasPressed && !Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.LeftShift))
				{
					EditorManager.inst.Zoom = EditorManager.inst.zoomFloat + ConfigEntries.ZoomAmount.Value;
				}
				if (InputDataManager.inst.editorActions.ZoomOut.WasPressed && !Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.LeftShift))
				{
					EditorManager.inst.Zoom = EditorManager.inst.zoomFloat - ConfigEntries.ZoomAmount.Value;
				}

				//Zooms more
				if (InputDataManager.inst.editorActions.ZoomIn.WasPressed && Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftAlt))
				{
					EditorManager.inst.Zoom = EditorManager.inst.zoomFloat + ConfigEntries.ZoomAmount.Value * 2f;
				}
				if (InputDataManager.inst.editorActions.ZoomOut.WasPressed && Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftAlt))
				{
					EditorManager.inst.Zoom = EditorManager.inst.zoomFloat - ConfigEntries.ZoomAmount.Value * 2f;
				}

				//Zooms more
				if (InputDataManager.inst.editorActions.ZoomIn.WasPressed && !Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.LeftAlt))
				{
					EditorManager.inst.Zoom = EditorManager.inst.zoomFloat + ConfigEntries.ZoomAmount.Value * 0.5f;
				}
				if (InputDataManager.inst.editorActions.ZoomOut.WasPressed && !Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.LeftAlt))
				{
					EditorManager.inst.Zoom = EditorManager.inst.zoomFloat - ConfigEntries.ZoomAmount.Value * 0.5f;
				}
			}
			if (InputDataManager.inst.editorActions.ShowHelp.WasPressed)
			{
				EditorManager.inst.SetShowHelp(!EditorManager.inst.showHelp);
			}
			return false;
		}

		[HarmonyPatch("updatePointer")]
		[HarmonyPrefix]
		private static bool updatePointerPrefix()
        {
			Vector2 point = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
			float value = EditorManager.inst.timelineScrollbar.GetComponent<Scrollbar>().value;
			Rect rect = new Rect(0f, 0.305f * (float)Screen.height, (float)Screen.width, (float)Screen.height * 0.025f);
			if (EditorManager.inst.updateAudioTime && Input.GetMouseButtonUp(0) && rect.Contains(point))
			{
				AudioManager.inst.CurrentAudioSource.time = EditorManager.inst.audioTimeForSlider / EditorManager.inst.Zoom;
				EditorManager.inst.updateAudioTime = false;
			}
			if (Input.GetMouseButton(0) && rect.Contains(point))
			{
				EditorManager.inst.timelineSlider.GetComponent<Slider>().minValue = 0f;
				EditorManager.inst.timelineSlider.GetComponent<Slider>().maxValue = AudioManager.inst.CurrentAudioSource.clip.length * EditorManager.inst.Zoom;
				EditorManager.inst.audioTimeForSlider = EditorManager.inst.timelineSlider.GetComponent<Slider>().value;
				EditorManager.inst.updateAudioTime = true;
				EditorManager.inst.wasDraggingPointer = true;
				if (Mathf.Abs(EditorManager.inst.audioTimeForSlider / EditorManager.inst.Zoom - EditorManager.inst.prevAudioTime) < 2f)
				{
					if (ConfigEntries.DraggingTimelineSliderPauses.Value)
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
				EditorManager.inst.timelineSlider.GetComponent<Slider>().minValue = 0f;
				EditorManager.inst.timelineSlider.GetComponent<Slider>().maxValue = AudioManager.inst.CurrentAudioSource.clip.length * EditorManager.inst.Zoom;
				EditorManager.inst.timelineSlider.GetComponent<Slider>().value = AudioManager.inst.CurrentAudioSource.time * EditorManager.inst.Zoom;
				EditorManager.inst.audioTimeForSlider = AudioManager.inst.CurrentAudioSource.time * EditorManager.inst.Zoom;
			}
			EditorManager.inst.timelineSlider.GetComponent<RectTransform>().sizeDelta = new Vector2(AudioManager.inst.CurrentAudioSource.clip.length * EditorManager.inst.Zoom, 25f);
			return false;
		}

		[HarmonyPatch("ToggleEditor")]
		[HarmonyPostfix]
		private static void ToggleEditorPatch()
        {
			if (EditorManager.inst.isEditing)
            {
				EditorManager.inst.UpdatePlayButton();
            }
        }

		[HarmonyPatch("CloseOpenBeatmapPopup")]
		[HarmonyPrefix]
		private static bool CloseOpenFilePopupPatch()
        {
			if (EditorManager.inst.hasLoadedLevel)
            {
				RTEditor.CloseOpenFilePopup();
            }
			else
            {
				EditorManager.inst.DisplayNotification("Please select a level first!", 2f, EditorManager.NotificationType.Error, false);
            }
			return false;
        }

		[HarmonyPatch("SaveBeatmap")]
		[HarmonyPrefix]
		private static void SaveBeatmapPrefix()
		{
			string str = "beatmaps/" + EditorPlugin.editorPath + "/" + EditorManager.inst.currentLoadedLevel;
			if (RTFile.FileExists(RTFile.GetApplicationDirectory() + str + "/level-previous.lsb"))
			{
				File.Delete(RTFile.GetApplicationDirectory() + str + "/level-previous.lsb");
			}

			File.Copy(RTFile.GetApplicationDirectory() + str + "/level.lsb", RTFile.GetApplicationDirectory() + str + "/level-previous.lsb");
		}

		[HarmonyPatch("SaveBeatmap")]
		[HarmonyPostfix]
		private static void EditorSaveBeatmapPatch()
		{
			DataManager.inst.gameData.beatmapData.editorData.timelinePos = AudioManager.inst.CurrentAudioSource.time;
			DataManager.inst.metaData.song.BPM = SettingEditor.inst.SnapBPM;
			DataManager.inst.gameData.beatmapData.levelData.backgroundColor = EditorManager.inst.layer;
			EditorPlugin.scrollBar = GameObject.Find("Editor Systems/Editor GUI/sizer/main/whole-timeline/Scrollbar").GetComponent<Scrollbar>().value;

			Sprite waveform = EditorManager.inst.timeline.GetComponent<Image>().sprite;
			if (ConfigEntries.WaveformMode.Value == WaveformType.Legacy && ConfigEntries.GenerateWaveform.Value == true)
			{
				File.WriteAllBytes(RTFile.GetApplicationDirectory() + GameManager.inst.basePath + "waveform.png", EditorManager.inst.timeline.GetComponent<Image>().sprite.texture.EncodeToPNG());
			}
			if (ConfigEntries.WaveformMode.Value == WaveformType.Beta && ConfigEntries.GenerateWaveform.Value == true)
			{
				File.WriteAllBytes(RTFile.GetApplicationDirectory() + GameManager.inst.basePath + "waveform_old.png", EditorManager.inst.timeline.GetComponent<Image>().sprite.texture.EncodeToPNG());
			}

			if (RTFile.FileExists(RTFile.GetApplicationDirectory() + "beatmaps/" + EditorPlugin.editorPath + "/" + EditorManager.inst.currentLoadedLevel + "/editor.lse"))
			{
				string rawProfileJSON = null;
				rawProfileJSON = FileManager.inst.LoadJSONFile("beatmaps/" + EditorPlugin.editorPath + "/" + EditorManager.inst.currentLoadedLevel + "/editor.lse");

				JSONNode jsonnode = JSON.Parse(rawProfileJSON);

				jsonnode["timeline"]["tsc"] = GameObject.Find("Editor Systems/Editor GUI/sizer/main/whole-timeline/Scrollbar").GetComponent<Scrollbar>().value.ToString("f2");
				jsonnode["timeline"]["z"] = EditorManager.inst.zoomFloat.ToString("f3");
				jsonnode["timeline"]["l"] = EditorManager.inst.layer.ToString();
				jsonnode["editor"]["t"] = EditorPlugin.itsTheTime.ToString();
				jsonnode["editor"]["a"] = EditorPlugin.openAmount.ToString();
				jsonnode["misc"]["sn"] = SettingEditor.inst.SnapActive.ToString();

				RTFile.WriteToFile("beatmaps/" + EditorPlugin.editorPath + "/" + EditorManager.inst.currentLoadedLevel + "/editor.lse", jsonnode.ToString(3));
			}
			else
			{
				JSONNode jsonnode = JSON.Parse("{}");

				jsonnode["timeline"]["tsc"] = GameObject.Find("Editor Systems/Editor GUI/sizer/main/whole-timeline/Scrollbar").GetComponent<Scrollbar>().value.ToString("f2");
				jsonnode["timeline"]["z"] = EditorManager.inst.zoomFloat.ToString("f3");
				jsonnode["timeline"]["l"] = EditorManager.inst.layer.ToString();
				jsonnode["editor"]["t"] = EditorPlugin.itsTheTime.ToString();
				jsonnode["editor"]["a"] = EditorPlugin.openAmount.ToString();
				jsonnode["misc"]["sn"] = SettingEditor.inst.SnapActive.ToString();

				RTFile.WriteToFile("beatmaps/" + EditorPlugin.editorPath + "/" + EditorManager.inst.currentLoadedLevel + "/editor.lse", jsonnode.ToString(3));
			}

			RTEditor.lastSavedGameData = DataManager.inst.gameData;
		}

		[HarmonyPatch("SaveBeatmapAs", new Type[] { })]
		[HarmonyPostfix]
		private static void EditorSaveBeatmapAsPatch()
		{
			if (EditorManager.inst.hasLoadedLevel)
			{
				string str = "beatmaps/" + EditorPlugin.editorPath + "/" + EditorManager.inst.saveAsLevelName;
				DataManager.inst.SaveMetadata(str + "/metadata.lsb");

				if (RTFile.FileExists(RTFile.GetApplicationDirectory() + "beatmaps/" + EditorPlugin.editorPath + "/" + EditorManager.inst.currentLoadedLevel + "/editor.lse"))
				{
					string rawProfileJSON = null;
					rawProfileJSON = FileManager.inst.LoadJSONFile("beatmaps/" + EditorPlugin.editorPath + "/" + EditorManager.inst.currentLoadedLevel + "/editor.lse");

					JSONNode jsonnode = JSON.Parse(rawProfileJSON);

					jsonnode["timeline"]["tsc"] = RTMath.RoundToNearestDecimal(GameObject.Find("Editor Systems/Editor GUI/sizer/main/whole-timeline/Scrollbar").GetComponent<Scrollbar>().value, 3);
					jsonnode["timeline"]["z"] = EditorManager.inst.Zoom;
					jsonnode["timeline"]["l"] = EditorManager.inst.layer;
					jsonnode["editor"]["t"] = EditorPlugin.itsTheTime;
					jsonnode["editor"]["a"] = EditorPlugin.openAmount;
					jsonnode["misc"]["sn"] = SettingEditor.inst.SnapActive;

					RTFile.WriteToFile("beatmaps/" + EditorPlugin.editorPath + "/" + EditorManager.inst.saveAsLevelName + "/editor.lse", jsonnode.ToString(3));
				}
				else
				{
					JSONNode jsonnode = JSON.Parse("{}");

					jsonnode["timeline"]["tsc"] = RTMath.roundToNearest(GameObject.Find("Editor Systems/Editor GUI/sizer/main/whole-timeline/Scrollbar").GetComponent<Scrollbar>().value, 0.001f);
					jsonnode["timeline"]["z"] = EditorManager.inst.Zoom;
					jsonnode["timeline"]["l"] = EditorManager.inst.layer;
					jsonnode["editor"]["t"] = EditorPlugin.itsTheTime;
					jsonnode["editor"]["a"] = EditorPlugin.openAmount;
					jsonnode["misc"]["sn"] = SettingEditor.inst.SnapActive;

					RTFile.WriteToFile("beatmaps/" + EditorPlugin.editorPath + "/" + EditorManager.inst.saveAsLevelName + "/editor.lse", jsonnode.ToString(3));
				}
			}
		}

		[HarmonyPatch("RenderOpenBeatmapPopup")]
		[HarmonyPostfix]
		private static void EditorRenderOpenBeatmapPopupPatch()
		{
			EditorPlugin.RenderBeatmapSet();
		}

		[HarmonyPatch("OpenBeatmapPopup")]
		[HarmonyPostfix]
		private static void EditorOpenBeatmapPopupPatch()
		{
			//Create Local Variables
			GameObject openLevel = EditorManager.inst.GetDialog("Open File Popup").Dialog.gameObject;
			Transform openTLevel = openLevel.transform;
			RectTransform openRTLevel = openLevel.GetComponent<RectTransform>();
			GridLayoutGroup openGridLVL = openTLevel.Find("mask/content").GetComponent<GridLayoutGroup>();

			//Set Editor Zoom cap
			EditorManager.inst.zoomBounds = ConfigEntries.ETLZoomBounds.Value;

			//Set Open File Popup RectTransform
			openRTLevel.anchoredPosition = ConfigEntries.ORLAnchoredPos.Value;
			openRTLevel.sizeDelta = ConfigEntries.ORLSizeDelta.Value;

			//Set Open FIle Popup content GridLayoutGroup
			openGridLVL.cellSize = ConfigEntries.OGLVLCellSize.Value;
			openGridLVL.constraint = (GridLayoutGroup.Constraint)ConfigEntries.OGLVLConstraint.Value;
			openGridLVL.constraintCount = ConfigEntries.OGLVLConstraintCount.Value;
			openGridLVL.spacing = ConfigEntries.OGLVLSpacing.Value;
		}

		[HarmonyPatch("AssignWaveformTextures")]
		[HarmonyPrefix]
		private static bool AssignWaveformTexturesPatch()
		{
			return false;
		}

		[HarmonyPatch("RenderTimeline")]
		[HarmonyPrefix]
		private static bool RenderTimelinePatch()
		{
			if (EditorManager.inst.layer == 5)
			{
				EventEditor.inst.RenderEventObjects();
			}
			else
			{
				ObjEditor.inst.RenderTimelineObjects("");
			}
			CheckpointEditor.inst.RenderCheckpoints();
			MarkerEditor.inst.RenderMarkers();

			var editor = EditorManager.inst;
			MethodInfo updateTimelineSizes = editor.GetType().GetMethod("UpdateTimelineSizes", BindingFlags.NonPublic | BindingFlags.Instance);
			updateTimelineSizes.Invoke(editor, new object[] { });
			return false;
		}

		[HarmonyPatch("RenderParentSearchList")]
		[HarmonyPrefix]
		private static bool RenderParentSearchList(EditorManager __instance)
		{
			Transform transform = __instance.GetDialog("Parent Selector").Dialog.Find("mask/content");

			foreach (object obj2 in transform)
			{
				Destroy(((Transform)obj2).gameObject);
			}

			GameObject gameObject = Instantiate(__instance.folderButtonPrefab);
			gameObject.name = "No Parent";
			gameObject.transform.SetParent(transform);
			gameObject.transform.localScale = Vector3.one;
			gameObject.transform.GetChild(0).GetComponent<Text>().text = "No Parent";
			gameObject.GetComponent<Button>().onClick.AddListener(delegate ()
			{
				ObjEditor.inst.SetParent("");
				EditorManager.inst.HideDialog("Parent Selector");
			});

			if (RTEditor.objectModifiersPlugin != null)
			{
				if (__instance.parentSearch == null || !(__instance.parentSearch != "") || "camera".Contains(__instance.parentSearch.ToLower()))
                {
					var cam = Instantiate(__instance.folderButtonPrefab);
					cam.name = "Camera";
					cam.transform.SetParent(transform);
					cam.transform.localScale = Vector3.one;
					cam.transform.GetChild(0).GetComponent<Text>().text = "Camera";
					cam.GetComponent<Button>().onClick.AddListener(delegate ()
					{
						ObjEditor.inst.SetParent("CAMERA_PARENT");
						EditorManager.inst.HideDialog("Parent Selector");
						ObjEditor.inst.OpenDialog();
					});
				}
			}

			foreach (var obj in DataManager.inst.gameData.beatmapObjects)
			{
				if (!obj.fromPrefab)
				{
					int num = DataManager.inst.gameData.beatmapObjects.IndexOf(obj);
					if ((__instance.parentSearch == null || !(__instance.parentSearch != "") || (obj.name + " " + num.ToString("0000")).ToLower().Contains(__instance.parentSearch.ToLower())) && obj != ObjEditor.inst.currentObjectSelection.GetObjectData())
					{
						bool flag = true;
						if (!string.IsNullOrEmpty(obj.parent))
						{
							string parentID = ObjEditor.inst.currentObjectSelection.GetObjectData().id;
							while (!string.IsNullOrEmpty(parentID))
							{
								if (parentID == obj.parent)
								{
									flag = false;
									break;
								}
								int num2 = DataManager.inst.gameData.beatmapObjects.FindIndex((DataManager.GameData.BeatmapObject x) => x.parent == parentID);
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
							GameObject gameObject2 = Instantiate<GameObject>(__instance.folderButtonPrefab);
							gameObject2.name = obj.name + " " + num.ToString("0000");
							gameObject2.transform.SetParent(transform);
							gameObject2.transform.localScale = Vector3.one;
							gameObject2.transform.GetChild(0).GetComponent<Text>().text = obj.name + " " + num.ToString("0000");
							gameObject2.GetComponent<Button>().onClick.AddListener(delegate ()
							{
								string id = obj.id;
								ObjEditor.inst.SetParent(id);
								EditorManager.inst.HideDialog("Parent Selector");
								Debug.Log("ID: " + id);
							});
						}
					}
				}
			}
			return false;
		}

		[HarmonyPatch("GetLevelList")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> GetLevelListTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			return new CodeMatcher(instructions)
				.Start()
				.Advance(4)
				.ThrowIfNotMatch("Is not beatmaps/editor", new CodeMatch(OpCodes.Ldstr))
				.SetInstruction(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(EditorPlugin), "levelListPath")))
				.ThrowIfNotMatch("Is not ldsfld", new CodeMatch(OpCodes.Ldsfld))
				.InstructionEnumeration();
		}

        //[HarmonyPatch("LoadLevel", MethodType.Enumerator)]
        //[HarmonyTranspiler]
        //private static IEnumerable<CodeInstruction> LoadLevelTranspiler(IEnumerable<CodeInstruction> instructions)
        //{
        //    return new CodeMatcher(instructions)
        //        .Start()
        //        .Advance(60)
        //        .ThrowIfNotMatch("Is not beatmaps/editor", new CodeMatch(OpCodes.Ldstr))
        //        .SetInstruction(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(EditorPlugin), "levelListSlash")))
        //        .ThrowIfNotMatch("Is not ldsfld", new CodeMatch(OpCodes.Ldsfld))
        //        .InstructionEnumeration();
        //}

        [HarmonyPatch("LoadLevel")]
		[HarmonyPrefix]
		private static bool LoadLevelPrefix(EditorManager __instance, ref IEnumerator __result, string __0)
		{
			EditorPlugin.inst.StartCoroutine(EditorPlugin.SetupPlayerEditor());
			__result = RTEditor.LoadLevel(__instance, __0);
			return false;
		}

		[HarmonyPatch("CreateNewLevel")]
		[HarmonyPrefix]
		private static bool CreateNewLevelPatch(EditorManager __instance)
        {
			RTEditor.CreateNewLevel(__instance);
			return false;
        }

		//[HarmonyPatch("CreateNewLevel")]
		//[HarmonyTranspiler]
		//private static IEnumerable<CodeInstruction> CreateLevelTranspiler(IEnumerable<CodeInstruction> instructions)
		//{
		//	return new CodeMatcher(instructions)
		//		.Start()
		//		.Advance(25)
		//		.ThrowIfNotMatch("Is not 25 beatmaps/editor/", new CodeMatch(OpCodes.Ldstr))
		//		.SetInstruction(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(EditorPlugin), "levelListSlash")))
		//		.ThrowIfNotMatch("Is not ldsfld 1", new CodeMatch(OpCodes.Ldsfld))
		//		.Advance(14)
		//		.ThrowIfNotMatch("Is not 39 beatmaps/editor/", new CodeMatch(OpCodes.Ldstr))
		//		.SetInstruction(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(EditorPlugin), "levelListSlash")))
		//		.ThrowIfNotMatch("Is not ldsfld 2", new CodeMatch(OpCodes.Ldsfld))
		//		.Advance(13)
		//		.ThrowIfNotMatch("Is not 52 beatmaps/editor/", new CodeMatch(OpCodes.Ldstr))
		//		.SetInstruction(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(EditorPlugin), "levelListSlash")))
		//		.ThrowIfNotMatch("Is not ldsfld 3", new CodeMatch(OpCodes.Ldsfld))
		//		.Advance(14)
		//		.ThrowIfNotMatch("Is not 66 beatmaps/editor/", new CodeMatch(OpCodes.Ldstr))
		//		.SetInstruction(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(EditorPlugin), "levelListSlash")))
		//		.ThrowIfNotMatch("Is not ldsfld 4", new CodeMatch(OpCodes.Ldsfld))
		//		.Advance(16)
		//		.ThrowIfNotMatch("Is not 82 20.4.4", new CodeMatch(OpCodes.Ldstr))
		//		.SetInstruction(new CodeInstruction(OpCodes.Ldstr, "4.1.16"))
		//		.Advance(24)
		//		.ThrowIfNotMatch("Is not 106 beatmaps/editor/", new CodeMatch(OpCodes.Ldstr))
		//		.SetInstruction(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(EditorPlugin), "levelListSlash")))
		//		.ThrowIfNotMatch("Is not ldsfld 5", new CodeMatch(OpCodes.Ldsfld))
		//		.InstructionEnumeration();
		//}

		[HarmonyPatch("OpenAlbumArtSelector")]
		[HarmonyPrefix]
		private static void OpenAlbumArtSelector(EditorManager __instance)
		{
			string jpgFile = FileBrowser.OpenSingleFile("jpg");
			Debug.Log("Selected file: " + jpgFile);
			if (!string.IsNullOrEmpty(jpgFile))
			{
				string jpgFileLocation = RTFile.GetApplicationDirectory() + EditorPlugin.levelListSlash + __instance.currentLoadedLevel + "/level.jpg";
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
		}

		[HarmonyPatch("GetAlbumSprite", MethodType.Enumerator)]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> AlbumSpriteTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			return new CodeMatcher(instructions)
				.Start()
				.Advance(17)
				.ThrowIfNotMatch("Is not beatmaps/editor/", new CodeMatch(OpCodes.Ldstr))
				.SetInstruction(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(EditorPlugin), "levelListSlash")))
				.ThrowIfNotMatch("Is not ldsfld", new CodeMatch(OpCodes.Ldsfld))
				.InstructionEnumeration();
		}

		[HarmonyPatch("OpenLevelFolder")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> OpenLevelFolderTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			return new CodeMatcher(instructions)
				.Start()
				.Advance(4)
				.ThrowIfNotMatch("Is not beatmaps/editor/", new CodeMatch(OpCodes.Ldstr))
				.SetInstruction(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(EditorPlugin), "levelListSlash")))
				.ThrowIfNotMatch("Is not ldsfld 1", new CodeMatch(OpCodes.Ldsfld))
				.Advance(7)
				.ThrowIfNotMatch("Is not beatmaps/editor", new CodeMatch(OpCodes.Ldstr))
				.SetInstruction(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(EditorPlugin), "levelListPath")))
				.ThrowIfNotMatch("Is not ldsfld 2", new CodeMatch(OpCodes.Ldsfld))
				.InstructionEnumeration();
		}

		[HarmonyPatch("OpenTutorials")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> OpenTutorialsTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			return new CodeMatcher(instructions)
				.Start()
				.SetInstruction(new CodeInstruction(OpCodes.Ldstr, "https://www.youtube.com/playlist?list=PLMHuUok_ojlWH_UZ60tHZIRMWJTDyhRaO"))
				.InstructionEnumeration();
		}

		[HarmonyPatch("OpenDiscord")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> OpenDiscordTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			return new CodeMatcher(instructions)
				.Start()
				.SetInstruction(new CodeInstruction(OpCodes.Ldstr, "https://discord.gg/KrGrpBwYgs"))
				.InstructionEnumeration();
		}

		[HarmonyPatch("CreateGrid")]
		[HarmonyPostfix]
		public static void Reminder()
		{
			if (ConfigEntries.ReminderActive.Value == true)
			{
				int radfs = UnityEngine.Random.Range(0, 5);
				string randomtext = "";

				switch (radfs)
                {
					case 0:
                        {
							randomtext = ". You should touch some grass.";
							break;
                        }
					case 1:
						{
							randomtext = ". Have a well deserved break.";
							break;
                        }
					case 2:
						{
							randomtext = ". You doing alright there?";
							break;
                        }
					case 3:
						{
							randomtext = ". You might need some rest.";
							break;
                        }
					case 4:
						{
							randomtext = ". Best thing to do in this situation is to have a break.";
							break;
                        }
					case 5:
						{
							randomtext = ". Anyone there?";
							if (ConfigEntries.ReminderRepeat.Value >= 600f)
							{
								RTEditor.inst.StartCoroutine(RTEditor.InitiateSecret());
							}
							break;
                        }
                }

				float time = Time.time;
				if (time != 0f)
				{
					EditorManager.inst.DisplayNotification("You've been working on the game for " + RTEditor.secondsToTime(Time.time) + randomtext, 2f, EditorManager.NotificationType.Warning, false);
				}
            }
        }

		public static List<string> randomQuotes = new List<string>
		{
			"You should touch some grass.",
			"Have a well deserved break.",
			"You doing alright there?",
			"You might need some rest.",
			"Anyone there?"
		};

        [HarmonyPatch("DisplayNotification")]
        [HarmonyPrefix]
        private static bool DisplayNotificationPrefix(string __0, float __1, EditorManager.NotificationType __2)
        {
            RTEditor.inst.StartCoroutine(RTEditor.DisplayDefaultNotification(__0, __1, __2));
            return false;
        }

		[HarmonyPatch("SnapToBPM")]
		[HarmonyPrefix]
		private static bool SnapToBPMPostfix(ref float __result, float __0)
		{
			__result = RTEditor.SnapToBPM(__0);
			return false;
		}

		[HarmonyPatch("ClearDialogs")]
		[HarmonyPrefix]
		private static bool ClearDialogsPrefix(EditorManager.EditorDialog.DialogType[] __0)
        {
			foreach (EditorManager.EditorDialog editorDialog in EditorManager.inst.EditorDialogs)
			{
				if (__0.Length == 0)
				{
					if (editorDialog.Type != EditorManager.EditorDialog.DialogType.Timeline)
					{
						editorDialog.Dialog.gameObject.SetActive(false);
						EditorManager.inst.ActiveDialogs.Remove(editorDialog);
					}
				}
				else if (__0.Contains(editorDialog.Type))
				{
					if (editorDialog.Dialog.gameObject.GetComponent<AnimateInGUI>())
					{
						editorDialog.Dialog.gameObject.GetComponent<AnimateInGUI>().OnDisableManual();
					}
					else
					{
						editorDialog.Dialog.gameObject.SetActive(false);
					}
					EditorManager.inst.ActiveDialogs.Remove(editorDialog);
				}
			}
			EditorManager.inst.currentDialog = EditorManager.inst.ActiveDialogs.Last();
			return false;
        }

		//[HarmonyPatch("SetDialogStatus")]
		//[HarmonyPrefix]
		private static bool SetDialogStatusPrefix(string __0, bool __1, bool __2)
        {
			var editorDialogsDictionaryInst = AccessTools.Field(typeof(EditorManager), "EditorDialogsDictionary").GetValue(EditorManager.inst);

			var containsKey = editorDialogsDictionaryInst.GetType().GetMethod("ContainsKey");
			var dia = (Dictionary<string, EditorManager.EditorDialog>)editorDialogsDictionaryInst;

			if ((bool)containsKey.Invoke(editorDialogsDictionaryInst, new object[] { __0 }))
			{
				if (__1 == false && dia[__0].Dialog.gameObject.GetComponent<AnimateInGUI>())
                {
					dia[__0].Dialog.gameObject.GetComponent<AnimateInGUI>().OnDisableManual();
				}
				else
				{
					dia[__0].Dialog.gameObject.SetActive(__1);
				}
				if (__1)
				{
					if (__2)
					{
						EditorManager.inst.currentDialog = dia[__0];
					}
					if (!EditorManager.inst.ActiveDialogs.Contains(dia[__0]))
					{
						EditorManager.inst.ActiveDialogs.Add(dia[__0]);
						return false;
					}
				}
				else
				{
					EditorManager.inst.ActiveDialogs.Remove(dia[__0]);
					if (EditorManager.inst.currentDialog == dia[__0] && __2)
					{
						if (EditorManager.inst.ActiveDialogs.Count > 0)
						{
							EditorManager.inst.currentDialog = EditorManager.inst.ActiveDialogs.Last();
							return false;
						}
						EditorManager.inst.currentDialog = new EditorManager.EditorDialog();
						return false;
					}
				}
			}
			return false;
        }

		[HarmonyPatch("LoadBaseLevel")]
		[HarmonyPostfix]
		private static void LoadBaseLevelPostfix()
        {
			EditorManager.inst.notification.transform.Find("info").gameObject.SetActive(true);
		}

		[HarmonyPatch("QuitToMenu")]
		[HarmonyPrefix]
		private static bool QuitToMenuPrefix(EditorManager __instance)
        {
			if (RTEditor.inst.allowQuit)
            {
				return true;
            }
			EditorManager.inst.ShowDialog("Warning Popup");
			RTEditor.RefreshWarningPopup("Are you sure you want to quit to main menu?", delegate ()
			{
				RTEditor.inst.allowQuit = true;
				__instance.QuitToMenu();
			}, delegate ()
			{
				EditorManager.inst.HideDialog("Warning Popup");
			});

			return false;
        }

		[HarmonyPatch("QuitGame")]
		[HarmonyPrefix]
		private static bool QuitGamePrefix(EditorManager __instance)
        {
			if (RTEditor.inst.allowQuit)
            {
				return true;
            }
			EditorManager.inst.ShowDialog("Warning Popup");
			RTEditor.RefreshWarningPopup("Are you sure you want to quit the game entirely?", delegate ()
			{
				RTEditor.inst.allowQuit = true;
				__instance.QuitGame();
			}, delegate ()
			{
				EditorManager.inst.HideDialog("Warning Popup");
			});

			return false;
        }
	}
}
