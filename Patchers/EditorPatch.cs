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

using EditorManagement.Functions.Editors;
using EditorManagement.Functions.Components;
using EditorManagement.Functions;
using EditorManagement.Functions.Tools;
using LSFunctions;
using Crosstales.FB;

using RTFunctions.Functions;

namespace EditorManagement.Patchers
{
	[HarmonyPatch(typeof(EditorManager))]
    public class EditorPatch : MonoBehaviour
    {
		public static bool canZoom;
		public static bool IsOverMainTimeline;
		public static Type keybindsType;
		public static MethodInfo GetKeyCodeName;

		[HarmonyPatch("Awake")]
		[HarmonyPrefix]
		public static bool EditorAwakePatch(EditorManager __instance)
		{
			//OG Code
			{
				InputDataManager.inst.BindMenuKeys();
				InputDataManager.inst.BindEditorKeys();
				__instance.ScreenScale = (float)Screen.width / 1920f;
				__instance.ScreenScaleInverse = 1f / __instance.ScreenScale;
				if (EditorManager.inst == null)
				{
					EditorManager.inst = __instance;
				}
				else if (EditorManager.inst != __instance)
				{
					Destroy(__instance.gameObject);
				}
				__instance.curveDictionary.Add(0, Ease.Linear);
				__instance.curveDictionary.Add(1, Ease.InSine);
				__instance.curveDictionary.Add(2, Ease.OutSine);
				__instance.curveDictionary.Add(3, Ease.InOutSine);
				__instance.curveDictionary.Add(4, Ease.InElastic);
				__instance.curveDictionary.Add(5, Ease.OutElastic);
				__instance.curveDictionary.Add(6, Ease.InOutElastic);
				__instance.curveDictionary.Add(7, Ease.InBack);
				__instance.curveDictionary.Add(8, Ease.OutBack);
				__instance.curveDictionary.Add(9, Ease.InOutBack);
				__instance.curveDictionary.Add(10, Ease.InBounce);
				__instance.curveDictionary.Add(11, Ease.OutBounce);
				__instance.curveDictionary.Add(12, Ease.InOutBounce);
				__instance.curveDictionaryBack.Add(Ease.Linear, 0);
				__instance.curveDictionaryBack.Add(Ease.InSine, 1);
				__instance.curveDictionaryBack.Add(Ease.OutSine, 2);
				__instance.curveDictionaryBack.Add(Ease.InOutSine, 3);
				__instance.curveDictionaryBack.Add(Ease.InElastic, 4);
				__instance.curveDictionaryBack.Add(Ease.OutElastic, 5);
				__instance.curveDictionaryBack.Add(Ease.InOutElastic, 6);
				__instance.curveDictionaryBack.Add(Ease.InBack, 7);
				__instance.curveDictionaryBack.Add(Ease.OutBack, 8);
				__instance.curveDictionaryBack.Add(Ease.InOutBack, 9);
				__instance.curveDictionaryBack.Add(Ease.InBounce, 10);
				__instance.curveDictionaryBack.Add(Ease.OutBounce, 11);
				__instance.curveDictionaryBack.Add(Ease.InOutBounce, 12);
				__instance.RefreshDialogDictionary();

				var list = (from x in Resources.FindObjectsOfTypeAll<Dropdown>()
						   where x.gameObject != null && x.gameObject.name == "curves"
						   select x).ToList();

				//List<GameObject> list = (from obj in Resources.FindObjectsOfTypeAll<GameObject>()
				//						 where obj.name == "curves" && obj.GetComponent<Dropdown>() != null
				//						 select obj).ToList<GameObject>();

				List<Dropdown.OptionData> list2 = new List<Dropdown.OptionData>();
				foreach (EditorManager.CurveOption curveOption in __instance.CurveOptions)
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

				__instance.timeline.GetComponent<EventTrigger>().triggers.Add(entry);
				__instance.timeline.GetComponent<EventTrigger>().triggers.Add(entry2);
			}

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

			// Adds time input, layer input and pitch input.
			RTEditor.ModifyTimelineBar(__instance);

			Debug.LogFormat("{0}Redo keybind set to Ctrl + Shift + Z", EditorPlugin.className);
			GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/Edit/Edit Dropdown/Redo/Text 1").GetComponent<Text>().text = "Ctrl + Shift + Z";
			GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/Edit/Edit Dropdown/Redo/Text 1").GetComponent<RectTransform>().sizeDelta = new Vector2(166f, 0f);

			Debug.LogFormat("{0}Setting Notification values", EditorPlugin.className);
			var notifyRT = __instance.notification.GetComponent<RectTransform>();
			var notifyGroup = __instance.notification.GetComponent<VerticalLayoutGroup>();
			notifyRT.sizeDelta = new Vector2(ConfigEntries.NotificationWidth.Value, 632f);
			__instance.notification.transform.localScale = new Vector3(ConfigEntries.NotificationSize.Value, ConfigEntries.NotificationSize.Value, 1f);

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

			Debug.LogFormat("{0}Layers", EditorPlugin.className);
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
			Debug.LogFormat("{0}TitleBar", EditorPlugin.className);
			{
				GameObject.Find("Editor GUI/sizer/main/Popups/Save As Popup/New File Popup/level-name").GetComponent<InputField>().characterValidation = InputField.CharacterValidation.None;
				GameObject.Find("Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/left/theme/name").GetComponent<InputField>().characterValidation = InputField.CharacterValidation.None;
				GameObject.Find("Editor GUI/sizer/main/EditorDialogs/PrefabDialog/data/name/input").GetComponent<InputField>().characterValidation = InputField.CharacterValidation.None;
				GameObject.Find("Editor GUI/sizer/main/Popups/New File Popup/Browser Popup").SetActive(true);
				GameObject.Find("Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/name/name").GetComponent<InputField>().characterLimit = 0;

				Destroy(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/autokill/tod-dropdown").GetComponent<HideDropdownOptions>());

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
					EditorManager.inst.ShowDialog("Warning Popup");
					RTEditor.RefreshWarningPopup("Are you sure you want to quit to the arcade?", delegate ()
					{
						DOTween.Clear();
						ObjectManager.inst.updateObjects();
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

			//Loading doggo
			Debug.LogFormat("{0}Loading doggo!", EditorPlugin.className);
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
			Debug.LogFormat("{0}Loading general pathing", EditorPlugin.className);
			{
				GameObject sortList = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/autokill/tod-dropdown"));
				sortList.transform.SetParent(EditorManager.inst.GetDialog("Open File Popup").Dialog);
				sortList.transform.localScale = Vector3.one;

				RectTransform sortListRT = sortList.GetComponent<RectTransform>();
				sortListRT.anchoredPosition = ConfigEntries.OpenFileDropdownPosition.Value;
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
				checkDesRT.anchoredPosition = ConfigEntries.OpenFileTogglePosition.Value;

				checkDes.transform.Find("title").GetComponent<Text>().enabled = false;
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
			Debug.LogFormat("{0}Creating level pathing", EditorPlugin.className);
			{
                if (RTFile.FileExists(RTFile.ApplicationDirectory + "beatmaps/editorpath.lss"))
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
					File.Delete(RTFile.ApplicationDirectory + "beatmaps/editorpath.lss");
				}
                else if (RTFile.FileExists(RTFile.ApplicationDirectory + "settings/editor.lss"))
				{
					//Load
					{
						string rawProfileJSON = FileManager.inst.LoadJSONFile("settings/editor.lss");

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

						for (int i = 0; i < jn["prefab_types"].Count; i++)
						{
							if (DataManager.inst.PrefabTypes.Count > i)
							{
								if (!string.IsNullOrEmpty(jn["prefab_types"][i]["name"]))
								{
									DataManager.inst.PrefabTypes[i].Name = jn["prefab_types"][i]["name"];
								}
								if (!string.IsNullOrEmpty(jn["prefab_types"][i]["color"]))
								{
									DataManager.inst.PrefabTypes[i].Color = LSColors.HexToColorAlpha(jn["prefab_types"][i]["color"]);
								}
							}
						}
					}

					//Save
					{
						JSONNode jn = JSON.Parse("{}");

						jn["level_path"] = EditorPlugin.editorPath;

						jn["theme_path"] = EditorPlugin.themePath;

						jn["prefab_path"] = EditorPlugin.prefabPath;

						for (int i = 0; i < DataManager.inst.PrefabTypes.Count; i++)
						{
							jn["prefab_types"][i]["name"] = DataManager.inst.PrefabTypes[i].Name;
							jn["prefab_types"][i]["color"] = RTEditor.ColorToHex(DataManager.inst.PrefabTypes[i].Color);
						}

						RTFile.WriteToFile("settings/editor.lss", jn.ToString(3));
					}
				}
				else
				{
					JSONNode jn = JSON.Parse("{}");

					jn["level_path"] = EditorPlugin.editorPath;

					jn["theme_path"] = EditorPlugin.themePath;

					jn["prefab_path"] = EditorPlugin.prefabPath;

					for (int i = 0; i < DataManager.inst.PrefabTypes.Count; i++)
					{
						jn["prefab_types"][i]["name"] = DataManager.inst.PrefabTypes[i].Name;
						jn["prefab_types"][i]["color"] = RTEditor.ColorToHex(DataManager.inst.PrefabTypes[i].Color);
					}

					RTFile.WriteToFile("settings/editor.lss", jn.ToString(3));
				}

				GameObject levelListObj = Instantiate(GameObject.Find("TimelineBar/GameObject/Time Input"));
				levelListObj.transform.SetParent(EditorManager.inst.GetDialog("Open File Popup").Dialog);
				levelListObj.GetComponent<RectTransform>().anchoredPosition = ConfigEntries.OpenFilePathPos.Value;
				levelListObj.GetComponent<RectTransform>().sizeDelta = new Vector2(ConfigEntries.OpenFilePathLength.Value, 34f);
				levelListObj.name = "editor path";
				levelListObj.transform.localScale = Vector3.one;

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
					if (RTFile.FileExists(RTFile.ApplicationDirectory + "settings/editor.lss"))
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
				levelListReloader.GetComponent<RectTransform>().anchoredPosition = ConfigEntries.OpenFileRefreshPosition.Value;
				levelListReloader.GetComponent<RectTransform>().sizeDelta = new Vector2(32f, 32f);
				levelListReloader.name = "reload";
				levelListReloader.transform.localScale = Vector3.one;

				HoverTooltip levelListRTip = levelListReloader.AddComponent<HoverTooltip>();
				HoverTooltip.Tooltip llRTip = new HoverTooltip.Tooltip();

				llRTip.desc = "Refresh level list";
				llRTip.hint = "Clicking this will reload the level list.";

				levelListRTip.tooltipLangauges.Add(llRTip);

				Button levelListRButton = levelListReloader.GetComponent<Button>();
				levelListRButton.onClick.ClearAll();
				levelListRButton.onClick.AddListener(delegate ()
				{
					EditorManager.inst.GetLevelList();
				});

				string jpgFileLocation = RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_refresh-white.png";

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

			RTEditor.inst.StartCoroutine(RTEditor.SetupTimelineTriggers());

			//Objects
			Debug.LogFormat("{0}Create Objects", EditorPlugin.className);
			{
				var persistent = __instance.GetDialog("Object Options Popup").Dialog.Find("persistent").gameObject.GetComponent<Button>();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("persistent/text").gameObject.GetComponent<Text>().text = "No Autokill";
				persistent.onClick.ClearAll();
				persistent.onClick.AddListener(delegate ()
				{
					RTEditor.CreateNewNoAutokillObject();
				});

				var empty = __instance.GetDialog("Object Options Popup").Dialog.Find("empty").gameObject.GetComponent<Button>();
				empty.onClick.ClearAll();
				empty.onClick.AddListener(delegate ()
				{
					RTEditor.CreateNewEmptyObject();
				});

				var decoration = __instance.GetDialog("Object Options Popup").Dialog.Find("decoration").gameObject.GetComponent<Button>();
				decoration.onClick.ClearAll();
				decoration.onClick.AddListener(delegate ()
				{
					RTEditor.CreateNewDecorationObject();
				});

				var helper = __instance.GetDialog("Object Options Popup").Dialog.Find("helper").gameObject.GetComponent<Button>();
				helper.onClick.ClearAll();
				helper.onClick.AddListener(delegate ()
				{
					RTEditor.CreateNewHelperObject();
				});

				var normal = __instance.GetDialog("Object Options Popup").Dialog.Find("normal").gameObject.GetComponent<Button>();
				normal.onClick.ClearAll();
				normal.onClick.AddListener(delegate ()
				{
					RTEditor.CreateNewNormalObject();
				});

				var circle = __instance.GetDialog("Object Options Popup").Dialog.Find("shapes/circle").gameObject.GetComponent<Button>();
				circle.onClick.ClearAll();
				circle.onClick.AddListener(delegate ()
				{
					RTEditor.CreateNewCircleObject();
				});

				var triangle = __instance.GetDialog("Object Options Popup").Dialog.Find("shapes/triangle").gameObject.GetComponent<Button>();
				triangle.onClick.ClearAll();
				triangle.onClick.AddListener(delegate ()
				{
					RTEditor.CreateNewTriangleObject();
				});

				var text = __instance.GetDialog("Object Options Popup").Dialog.Find("shapes/text").gameObject.GetComponent<Button>();
				text.onClick.ClearAll();
				text.onClick.AddListener(delegate ()
				{
					RTEditor.CreateNewTextObject();
				});

				var hexagon = __instance.GetDialog("Object Options Popup").Dialog.Find("shapes/hexagon").gameObject.GetComponent<Button>();
				hexagon.onClick.ClearAll();
				hexagon.onClick.AddListener(delegate ()
				{
					RTEditor.CreateNewHexagonObject();
				});
			}

			//Select UI
			Debug.LogFormat("{0}Selectable UI elements", EditorPlugin.className);
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
			Debug.LogFormat("{0}Setting Timeline Cursor Colors", EditorPlugin.className);
			{
				Debug.LogFormat("{0}Cursor Color Handle 1", EditorPlugin.className);
				if (RTExtensions.TryFind("Editor Systems/Editor GUI/sizer/main/whole-timeline/Slider_Parent/Slider/Handle Slide Area/Image/Handle", out GameObject gm) && gm.TryGetComponent(out Image image))
				{
					RTEditor.timelineSliderHandle = image;
					RTEditor.timelineSliderHandle.color = ConfigEntries.MainTimelineSliderColor.Value;
				}
				else
				{
					Debug.LogFormat("{0}Whoooops you gotta put this CD up your-", EditorPlugin.className);
				}

				Debug.LogFormat("{0}Cursor Color Ruler 1", EditorPlugin.className);
				if (RTExtensions.TryFind("Editor Systems/Editor GUI/sizer/main/whole-timeline/Slider_Parent/Slider/Handle Slide Area/Image", out GameObject gm1) && gm1.TryGetComponent(out Image image1))
				{
					RTEditor.timelineSliderRuler = image1;
					RTEditor.timelineSliderRuler.color = ConfigEntries.MainTimelineSliderColor.Value;
				}
				else
				{
					Debug.LogFormat("{0}Whoooops you gotta put this CD up your-", EditorPlugin.className);
				}

				Debug.LogFormat("{0}Cursor Color Handle 2", EditorPlugin.className);
				if (RTExtensions.TryFind("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/timeline/Scroll View/Viewport/Content/time_slider/Handle Slide Area/Handle/Image", out GameObject gm2) && gm2.TryGetComponent(out Image image2))
				{
					RTEditor.keyframeTimelineSliderHandle = image2;
					RTEditor.keyframeTimelineSliderHandle.color = ConfigEntries.KeyframeTimelineSliderColor.Value;
				}
				else
				{
					Debug.LogFormat("{0}Whoooops you gotta put this CD up your-", EditorPlugin.className);
				}

				Debug.LogFormat("{0}Cursor Color Ruler 2", EditorPlugin.className);
				if (RTExtensions.TryFind("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/timeline/Scroll View/Viewport/Content/time_slider/Handle Slide Area/Handle", out GameObject gm3) && gm3.TryGetComponent(out Image image3))
				{
					RTEditor.keyframeTimelineSliderRuler = image3;
					RTEditor.keyframeTimelineSliderRuler.color = ConfigEntries.KeyframeTimelineSliderColor.Value;
				}
				else
				{
					Debug.LogFormat("{0}Whoooops you gotta put this CD up your-", EditorPlugin.className);
				}
			}

			//Theme Pathing
			Debug.LogFormat("{0}Creating theme pathing", EditorPlugin.className);
			{
				GameObject themePather = Instantiate(EditorManager.inst.GetDialog("Event Editor").Dialog.Find("data/right/theme").GetChild(2).gameObject);
				themePather.transform.SetParent(EditorManager.inst.GetDialog("Event Editor").Dialog.Find("data/right/theme"));
				themePather.transform.SetSiblingIndex(8);
				themePather.name = "themepathers";
				themePather.transform.localScale = Vector3.one;

				GameObject levelListObj = Instantiate(RTEditor.timeIF.gameObject);
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
					if (RTFile.FileExists(RTFile.ApplicationDirectory + "settings/editor.lss"))
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
				levelListReloader.transform.localScale = Vector3.one;

				HoverTooltip levelListRTip = levelListReloader.AddComponent<HoverTooltip>();
				HoverTooltip.Tooltip llRTip = new HoverTooltip.Tooltip();

				llRTip.desc = "Refresh theme list";
				llRTip.hint = "Clicking this will reload the theme list.";

				levelListRTip.tooltipLangauges.Add(llRTip);

				Button levelListRButton = levelListReloader.GetComponent<Button>();
				levelListRButton.onClick.ClearAll();
				levelListRButton.onClick.AddListener(delegate ()
				{
					RTEditor.inst.StartCoroutine(RTEditor.LoadThemes());
					EventEditor.inst.RenderEventsDialog();
				});

				string jpgFileLocation = RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_refresh-white.png";

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
			Debug.LogFormat("{0}Creating prefab pathing", EditorPlugin.className);
			{
				GameObject levelListObj = Instantiate(RTEditor.timeIF.gameObject);
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
					if (RTFile.FileExists(RTFile.ApplicationDirectory + "settings/editor.lss"))
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
				levelListRButton.onClick.ClearAll();
				levelListRButton.onClick.AddListener(delegate ()
				{
					RTEditor.inst.StartCoroutine(RTEditor.UpdatePrefabs());
				});

				string jpgFileLocation = RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_refresh-white.png";

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

            RTEditor.SearchObjectsCreator();
			RTEditor.WarningPopupCreator();
			RTEditor.inst.StartCoroutine(RTEditor.SetupTooltips());
			RTEditor.CreateMultiObjectEditor();

			return false;
		}

		[HarmonyPatch("Awake")]
		[HarmonyPostfix]
		private static void PropertiesWindow(EditorManager __instance)
        {
			RTEditor.inst.StartCoroutine(RTEditor.CreatePropertiesWindow());
			//Player Editor
			{
				GameObject gameObject = new GameObject("PlayerEditorManager");
				gameObject.transform.SetParent(GameObject.Find("Editor Systems").transform);
				gameObject.AddComponent<CreativePlayersEditor>();
			}

			//Object Modifiers Editor
			{
				GameObject gameObject = new GameObject("ObjectModifiersEditor");
				gameObject.transform.SetParent(GameObject.Find("Editor Systems").transform);
				gameObject.AddComponent<ObjectModifiersEditor>();
			}

			EditorPlugin.SetShowable();
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
			fButtonText.horizontalOverflow = ConfigEntries.OpenFileTextHorizontalWrap.Value;
			fButtonText.verticalOverflow = ConfigEntries.OpenFileTextVerticalWrap.Value;
			fButtonText.color = ConfigEntries.OpenFileTextColor.Value;
			fButtonText.fontSize = ConfigEntries.OpenFileTextFontSize.Value;

			//Folder Button Colors
			ColorBlock cb = fButtonBUTT.colors;
			cb.normalColor = ConfigEntries.OpenFileButtonNormalColor.Value;
			cb.pressedColor = ConfigEntries.OpenFileButtonPressedColor.Value;
			cb.highlightedColor = ConfigEntries.OpenFileButtonHighlightedColor.Value;
			cb.selectedColor = ConfigEntries.OpenFileButtonSelectedColor.Value;
			cb.fadeDuration = ConfigEntries.OpenFileButtonFadeDuration.Value;
			fButtonBUTT.colors = cb;

			EditorPlugin.timeEdit = EditorPlugin.itsTheTime;

			InputDataManager.inst.editorActions.Cut.ClearBindings();
			InputDataManager.inst.editorActions.Copy.ClearBindings();
			InputDataManager.inst.editorActions.Paste.ClearBindings();
			InputDataManager.inst.editorActions.Duplicate.ClearBindings();
			InputDataManager.inst.editorActions.Delete.ClearBindings();
			InputDataManager.inst.editorActions.Undo.ClearBindings();
			InputDataManager.inst.editorActions.Redo.ClearBindings();

			//RTEditor.inst.StartCoroutine(RTEditor.StartEditorGUI());
			InputPlayers();
		}

		[HarmonyPatch("Start")]
		[HarmonyPrefix]
		private static void StartPrefix()
        {
			Debug.LogFormat("{0}Player Count: {1}", EditorPlugin.className, InputDataManager.inst.players.Count);
			playerStorage.Clear();
			foreach (var player in InputDataManager.inst.players)
            {
				playerStorage.Add(player);
            }
        }

		public static List<InputDataManager.CustomPlayer> playerStorage = new List<InputDataManager.CustomPlayer>();
		public static bool reloadSelectedPlayers = true;

		public static void InputPlayers()
        {
			InputDataManager.inst.players.Clear();
			if (reloadSelectedPlayers && playerStorage.Count > 1)
			{
				foreach (var player in playerStorage)
                {
					InputDataManager.inst.players.Add(player);
                }
            }
			else
				InputDataManager.inst.players.Add(new InputDataManager.CustomPlayer(true, 0, null));
		}

		[HarmonyPatch("Update")]
		[HarmonyPostfix]
		private static void EditorUpdatePatch()
		{
			if (EditorManager.inst.GUI.activeSelf == true && EditorManager.inst.isEditing == true)
			{
				//Create Local Variables

				if (RTEditor.timeIF != null && !RTEditor.timeIF.isFocused)
				{
					RTEditor.timeIF.text = AudioManager.inst.CurrentAudioSource.time.ToString();
				}

				if (GameObject.Find("Game Systems/EventManager") && GameObject.Find("Game Systems/EventManager").GetComponentByName("RTEventManager"))
				{
					var rt = GameObject.Find("Game Systems/EventManager").GetComponentByName("RTEventManager");

					var f = (float)rt.GetType().GetField("pitchOffset", BindingFlags.Public | BindingFlags.Instance).GetValue(rt);

					if (RTEditor.pitchIF != null && !RTEditor.pitchIF.isFocused)
					{
						RTEditor.pitchIF.text = f.ToString();
					}
				}
				else
                {
					if (RTEditor.pitchIF != null && !RTEditor.pitchIF.isFocused)
					{
						RTEditor.pitchIF.text = AudioManager.inst.pitch.ToString();
					}
				}

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
					int x = int.Parse(RTEditor.layersIF.text);
					x += 1;
					RTEditor.layersIF.text = x.ToString();
				}
				if (Input.GetKeyDown(KeyCode.PageDown))
				{
					int x = int.Parse(RTEditor.layersIF.text);
					x -= 1;
					RTEditor.layersIF.text = x.ToString();
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
				&& !IsOverMainTimeline && (!ModCompatibility.sharedFunctions.ContainsKey("EventsCoreEditorOffset") || !(bool)ModCompatibility.sharedFunctions["EventsCoreEditorOffset"]))
			{
				float multiply = 1f;
				if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
					multiply = 2f;
				if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
					multiply = 0.1f;

				if (InputDataManager.inst.editorActions.ZoomIn.WasPressed)
				{
					ObjEditor.inst.Zoom = ObjEditor.inst.zoomFloat + ConfigEntries.KeyframeZoomAmount.Value * multiply;
				}
				if (InputDataManager.inst.editorActions.ZoomOut.WasPressed)
				{
					ObjEditor.inst.Zoom = ObjEditor.inst.zoomFloat - ConfigEntries.KeyframeZoomAmount.Value * multiply;
				}
			}
			if (!EditorManager.inst.IsOverDropDown && !EditorManager.inst.IsOverObjTimeline && IsOverMainTimeline && (!ModCompatibility.sharedFunctions.ContainsKey("EventsCoreEditorOffset") || !(bool)ModCompatibility.sharedFunctions["EventsCoreEditorOffset"]))
			{
				float multiply = 1f;
				if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
					multiply = 2f;
				if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
					multiply = 0.1f;

				if (InputDataManager.inst.editorActions.ZoomIn.WasPressed)
				{
					EditorManager.inst.Zoom = EditorManager.inst.zoomFloat + ConfigEntries.MainZoomAmount.Value * multiply;
				}
				if (InputDataManager.inst.editorActions.ZoomOut.WasPressed)
				{
					EditorManager.inst.Zoom = EditorManager.inst.zoomFloat - ConfigEntries.MainZoomAmount.Value * multiply;
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
					if (ConfigEntries.DraggingMainCursorPausesLevel.Value)
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

		[HarmonyPatch("AddToPitch")]
		[HarmonyPrefix]
		private static bool AddToPitchPrefix(EditorManager __instance, float __0)
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
		private static void ToggleEditorPatch()
        {
			if (EditorManager.inst.isEditing)
            {
				EditorManager.inst.UpdatePlayButton();
            }
			GameManager.inst.ResetCheckpoints();
        }

		[HarmonyPatch("CloseOpenBeatmapPopup")]
		[HarmonyPrefix]
		private static bool CloseOpenFilePopupPatch(EditorManager __instance)
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

		[HarmonyPatch("SaveBeatmap")]
		[HarmonyPrefix]
		private static void SaveBeatmapPrefix()
		{
			string str = "beatmaps/" + EditorPlugin.editorPath + "/" + EditorManager.inst.currentLoadedLevel;
			if (RTFile.FileExists(RTFile.ApplicationDirectory + str + "/level-previous.lsb"))
			{
				File.Delete(RTFile.ApplicationDirectory + str + "/level-previous.lsb");
			}

			if (RTFile.FileExists(RTFile.ApplicationDirectory + str + "/level.lsb"))
				File.Copy(RTFile.ApplicationDirectory + str + "/level.lsb", RTFile.ApplicationDirectory + str + "/level-previous.lsb");
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
				File.WriteAllBytes(RTFile.ApplicationDirectory + GameManager.inst.basePath + "waveform.png", EditorManager.inst.timeline.GetComponent<Image>().sprite.texture.EncodeToPNG());
			}
			if (ConfigEntries.WaveformMode.Value == WaveformType.Beta && ConfigEntries.GenerateWaveform.Value == true)
			{
				File.WriteAllBytes(RTFile.ApplicationDirectory + GameManager.inst.basePath + "waveform_old.png", EditorManager.inst.timeline.GetComponent<Image>().sprite.texture.EncodeToPNG());
			}

			if (RTFile.FileExists(RTFile.ApplicationDirectory + "beatmaps/" + EditorPlugin.editorPath + "/" + EditorManager.inst.currentLoadedLevel + "/editor.lse"))
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
		}

		[HarmonyPatch("SaveBeatmapAs", new Type[] { })]
		[HarmonyPostfix]
		private static void EditorSaveBeatmapAsPatch()
		{
			if (EditorManager.inst.hasLoadedLevel)
			{
				string str = "beatmaps/" + EditorPlugin.editorPath + "/" + EditorManager.inst.saveAsLevelName;
				DataManager.inst.SaveMetadata(str + "/metadata.lsb");

				if (RTFile.FileExists(RTFile.ApplicationDirectory + "beatmaps/" + EditorPlugin.editorPath + "/" + EditorManager.inst.currentLoadedLevel + "/editor.lse"))
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
			RTEditor.RenderBeatmapSet();
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
			EditorManager.inst.zoomBounds = ConfigEntries.MainZoomBounds.Value;

			//Set Open File Popup RectTransform
			openRTLevel.anchoredPosition = ConfigEntries.OpenFilePosition.Value;
			openRTLevel.sizeDelta = ConfigEntries.OpenFileScale.Value;

			//Set Open FIle Popup content GridLayoutGroup
			openGridLVL.cellSize = ConfigEntries.OpenFileCellSize.Value;
			openGridLVL.constraint = (GridLayoutGroup.Constraint)ConfigEntries.OpenFileCellConstraintType.Value;
			openGridLVL.constraintCount = ConfigEntries.OpenFileCellConstraintCount.Value;
			openGridLVL.spacing = ConfigEntries.OpenFileCellSpacing.Value;
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
				ObjEditor.inst.RenderTimelineObjects();
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

			if (ObjectModifiersEditor.inst != null && ObjectModifiersEditor.objectModifiersPlugin != null)
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
						ObjEditor.inst.currentObjectSelection.GetObjectData().parent = "CAMERA_PARENT";
						ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
						EditorManager.inst.HideDialog("Parent Selector");
						RTEditor.inst.StartCoroutine(RTEditor.RefreshObjectGUI());
					});
				}

				if (__instance.parentSearch == null || !(__instance.parentSearch != "") || "player".Contains(__instance.parentSearch.ToLower()))
                {
					var cam = Instantiate(__instance.folderButtonPrefab);
					cam.name = "Player";
					cam.transform.SetParent(transform);
					cam.transform.localScale = Vector3.one;
					cam.transform.GetChild(0).GetComponent<Text>().text = "Nearest Player";
					cam.GetComponent<Button>().onClick.AddListener(delegate ()
					{
						ObjEditor.inst.currentObjectSelection.GetObjectData().parent = "PLAYER_PARENT";
						ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
						EditorManager.inst.HideDialog("Parent Selector");
						RTEditor.inst.StartCoroutine(RTEditor.RefreshObjectGUI());
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
							GameObject gameObject2 = Instantiate(__instance.folderButtonPrefab);
							gameObject2.name = obj.name + " " + num.ToString("0000");
							gameObject2.transform.SetParent(transform);
							gameObject2.transform.localScale = Vector3.one;
							gameObject2.transform.GetChild(0).GetComponent<Text>().text = obj.name + " " + num.ToString("0000");
							gameObject2.GetComponent<Button>().onClick.AddListener(delegate ()
							{
								string id = obj.id;
								ObjEditor.inst.currentObjectSelection.GetObjectData().parent = id;
								ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
								EditorManager.inst.HideDialog("Parent Selector");
								RTEditor.inst.StartCoroutine(RTEditor.RefreshObjectGUI());
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
				string jpgFileLocation = RTFile.ApplicationDirectory + EditorPlugin.levelListSlash + __instance.currentLoadedLevel + "/level.jpg";
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
				int radfs = UnityEngine.Random.Range(0, randomQuotes.Count - 1);
				float time = Time.time;
				if (time != 0f)
				{
					EditorManager.inst.DisplayNotification("You've been working on the game for " + RTEditor.secondsToTime(Time.time) + ". " + randomQuotes[radfs], 2f, EditorManager.NotificationType.Warning, false);
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
