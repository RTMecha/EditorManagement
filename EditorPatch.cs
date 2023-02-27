using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using SimpleJSON;

using HarmonyLib;

using EditorManagement.Functions;
using LSFunctions;

namespace EditorManagement
{
	[HarmonyPatch(typeof(EditorManager))]
    public class EditorPatch : MonoBehaviour
    {
		[HarmonyPatch("Awake")]
		[HarmonyPostfix]
		public static void SetEditorAwake()
		{
			GameObject.Find("Editor Systems/EditorManager").AddComponent<RTEditor>();

			GameObject timeObj = GameObject.Find("TimelineBar/GameObject").transform.GetChild(13).gameObject;
			GameObject.Find("TimelineBar/GameObject").transform.GetChild(0).gameObject.SetActive(true);
			InputField iFtimeObj = timeObj.GetComponent<InputField>();
			timeObj.name = "Time Input";

			timeObj.transform.SetSiblingIndex(0);
			timeObj.SetActive(true);
			iFtimeObj.text = AudioManager.inst.CurrentAudioSource.time.ToString();
			iFtimeObj.characterValidation = InputField.CharacterValidation.Decimal;

			iFtimeObj.onValueChanged.AddListener(delegate (string _value)
			{
				RTEditor.SetNewTime(_value);
			});

			timeObj.AddComponent<EventTrigger>();

			GameObject tbarLayers = Instantiate(GameObject.Find("TimelineBar/GameObject/Time Input"));

			tbarLayers.transform.SetParent(GameObject.Find("TimelineBar/GameObject").transform);
			tbarLayers.name = "layers";
			tbarLayers.transform.SetSiblingIndex(8);
			InputField tbarLayersIF = tbarLayers.GetComponent<InputField>();
			tbarLayers.transform.Find("Text").gameObject.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
			if (EditorManager.inst.layer < 5)
			{
				float l = EditorManager.inst.layer + 1;
				tbarLayersIF.text = l.ToString();
			}
			else
			{
				int l = EditorManager.inst.layer + 2;
				tbarLayersIF.text = l.ToString();
			}
			tbarLayersIF.characterValidation = InputField.CharacterValidation.Integer;
			tbarLayersIF.onValueChanged.RemoveAllListeners();
			tbarLayersIF.onValueChanged.AddListener(delegate (string _value)
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
					tbarLayersIF.text = "1";
				}
			});
			Image layerImage = tbarLayers.GetComponent<Image>();
			layerImage.color = EditorManager.inst.layerColors[0];

			GameObject.Find("TimelineBar/GameObject/1").SetActive(false);
			GameObject.Find("TimelineBar/GameObject/2").SetActive(false);
			GameObject.Find("TimelineBar/GameObject/3").SetActive(false);
			GameObject.Find("TimelineBar/GameObject/4").SetActive(false);
			GameObject.Find("TimelineBar/GameObject/5").SetActive(false);

			EventTrigger eventTrigger = GameObject.Find("TimelineBar/GameObject/6").GetComponent<EventTrigger>();

			EventTrigger.Entry entryEvent = new EventTrigger.Entry();
			entryEvent.eventID = EventTriggerType.PointerClick;
			entryEvent.callback.AddListener(delegate (BaseEventData eventData)
			{
				PointerEventData pointerEventData = (PointerEventData)eventData;
				if (pointerEventData.clickTime > 0f)
				{
					RTEditor.SetLayer(5);
				}
			});
			eventTrigger.triggers.Clear();
			eventTrigger.triggers.Add(entryEvent);

			GameObject.Find("TimelineBar/GameObject/checkpoint").SetActive(false);

			GameObject.Find("Editor GUI/sizer/main/Popups/Save As Popup/New File Popup/level-name").GetComponent<InputField>().characterValidation = InputField.CharacterValidation.None;
			GameObject.Find("Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/left/theme/name").GetComponent<InputField>().characterValidation = InputField.CharacterValidation.None;
			GameObject.Find("Editor GUI/sizer/main/EditorDialogs/PrefabDialog/data/name/input").GetComponent<InputField>().characterValidation = InputField.CharacterValidation.None;
			GameObject.Find("Editor GUI/sizer/main/Popups/New File Popup/Browser Popup").SetActive(true);
			GameObject.Find("Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/name/name").GetComponent<InputField>().characterLimit = 0;

			EventTrigger.Entry entryMoveX = new EventTrigger.Entry();
			entryMoveX.eventID = EventTriggerType.Scroll;
			entryMoveX.callback.AddListener(delegate (BaseEventData eventData)
			{
				float time = AudioManager.inst.CurrentAudioSource.time;
				float timeModify = EditorPlugin.TimeModify.Value;
				float timeMinus = time - timeModify;
				float timePlus = time + timeModify;

				PointerEventData pointerEventData = (PointerEventData)eventData;
				if (pointerEventData.scrollDelta.y < 0f)
				{
					RTEditor.SetNewTime(timeMinus.ToString());
					iFtimeObj.text = time.ToString();
					return;
				}
				if (pointerEventData.scrollDelta.y > 0f)
				{
					RTEditor.SetNewTime(timePlus.ToString());
					iFtimeObj.text = time.ToString();
				}
			});
			timeObj.gameObject.GetComponent<EventTrigger>().triggers.Clear();
			timeObj.gameObject.GetComponent<EventTrigger>().triggers.Add(entryMoveX);

			//Loading doggo
			GameObject loading = new GameObject("loading");
			GameObject popup = GameObject.Find("Editor Systems/Editor GUI/sizer/main/Popups");
			Transform fileInfoPopup = popup.transform.GetChild(1);

			loading.transform.SetParent(fileInfoPopup);
			loading.layer = 5;

			RectTransform rtLoading = loading.AddComponent<RectTransform>();
			loading.AddComponent<CanvasRenderer>();
			Image iLoading = loading.AddComponent<Image>();
			LayoutElement leLoading = loading.AddComponent<LayoutElement>();

			rtLoading.anchoredPosition = new Vector2(0f, -60f);
			rtLoading.sizeDelta = new Vector2(122f, 122f);
			iLoading.sprite = EditorManager.inst.loadingImage.sprite;
			leLoading.ignoreLayout = true;

			fileInfoPopup.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(500f, 280f);

			//Tooltips
			HoverTooltip depthTip = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/depth/depth").GetComponent<HoverTooltip>();

			HoverTooltip.Tooltip tooltipDepth = new HoverTooltip.Tooltip();
			tooltipDepth.desc = "Set the depth layer of the object.";
			tooltipDepth.hint = "Depth is if an object shows above or below another object. However, higher number does not equal higher depth here since it's reversed.<br>Higher number = lower depth<br>Lower number = higher depth.";

			depthTip.tooltipLangauges.Add(tooltipDepth);


			HoverTooltip timelineTip = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/timeline").GetComponent<HoverTooltip>();

			HoverTooltip.Tooltip tooltipTimeline = new HoverTooltip.Tooltip();
			tooltipTimeline.desc = "Create a keyframe in one of the four keyframe bins by right clicking.";
			tooltipTimeline.hint = "Each keyframe is located here.";

			timelineTip.tooltipLangauges.Add(tooltipTimeline);

			HoverTooltip textShapeTip = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/shapesettings/5").GetComponent<HoverTooltip>();

			HoverTooltip.Tooltip tooltipTextShape = new HoverTooltip.Tooltip();
			tooltipTextShape.desc = "Write your custom text here.";
			tooltipTextShape.hint = "Anything you write here will show up as a text object. There are a lot of formatting options, such as < b >, < i >, < br >, < color = #FFFFFF > < alpha = #FF > and more. (of course without the spaces between)";

			textShapeTip.tooltipLangauges.Add(tooltipTextShape);

			var fileInfoPopup1 = EditorManager.inst.EditorDialogs[7].Dialog;
			fileInfoPopup1.gameObject.GetComponent<Image>().sprite = null;

			var fileInfoPopup2 = EditorManager.inst.EditorDialogs[1].Dialog;
			Sprite sprite = fileInfoPopup2.transform.Find("data/left/Scroll View/Viewport/Content/shape/7/Background").GetComponent<Image>().sprite;

			GameObject sortList = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/autokill/tod-dropdown"));
			sortList.transform.SetParent(EditorManager.inst.GetDialog("Open File Popup").Dialog);

			RectTransform sortListRT = sortList.GetComponent<RectTransform>();
			sortListRT.anchoredPosition = EditorPlugin.ORLDropdownPos.Value;
			HoverTooltip sortListTip = sortList.GetComponent<HoverTooltip>();

			sortListTip.tooltipLangauges[0].desc = "Sort the order of your levels.";
			sortListTip.tooltipLangauges[0].hint = "<b>Cover</b> Sort by if level has a set cover. (Default)" +
				"<br><b>Artist</b> Sort by song artist." +
				"<br><b>Creator</b> Sort by level creator." +
				"<br><b>Folder</b> Sort by level folder name." +
				"<br><b>Title</b> Sort by song title." +
				"<br><b>Difficulty</b> Sort by level difficulty." +
				"<br><b>Date Edited</b> Sort by date edited / created.";

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

			RectTransform checkDesRT = checkDes.GetComponent<RectTransform>();
			checkDesRT.anchoredPosition = EditorPlugin.ORLTogglePos.Value;

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

			//Level List
			{
				if (RTFile.FileExists(RTFile.GetApplicationDirectory() + "beatmaps/editorpath.lss"))
				{
					string rawProfileJSON = null;
					rawProfileJSON = FileManager.inst.LoadJSONFile("beatmaps/editorpath.lss");

					JSONNode jsonnode = JSON.Parse(rawProfileJSON);

					EditorPlugin.editorPath = jsonnode["path"];
					EditorPlugin.levelListPath = "beatmaps/" + EditorPlugin.editorPath;
					EditorPlugin.levelListSlash = "beatmaps/" + EditorPlugin.editorPath + "/";
				}
				else
				{
					JSONNode jsonnode = JSON.Parse("{}");

					jsonnode["path"] = EditorPlugin.editorPath;

					RTFile.WriteToFile("beatmaps/editorpath.lss", jsonnode.ToString(3));
				}

				GameObject levelListObj = Instantiate(GameObject.Find("TimelineBar/GameObject/Time Input"));
				levelListObj.transform.SetParent(EditorManager.inst.GetDialog("Open File Popup").Dialog);
				levelListObj.GetComponent<RectTransform>().anchoredPosition = EditorPlugin.ORLPathPos.Value;
				levelListObj.GetComponent<RectTransform>().sizeDelta = new Vector2(EditorPlugin.ORLPathLength.Value, 34f);
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
					if (RTFile.FileExists(RTFile.GetApplicationDirectory() + "beatmaps/editorpath.lss"))
					{
						string rawProfileJSON = null;
						rawProfileJSON = FileManager.inst.LoadJSONFile("beatmaps/editorpath.lss");

						JSONNode jsonnode = JSON.Parse(rawProfileJSON);

						jsonnode["path"] = _val;

						RTFile.WriteToFile("beatmaps/editorpath.lss", jsonnode.ToString(3));
					}

					EditorPlugin.editorPath = _val;
					EditorPlugin.levelListPath = "beatmaps/" + EditorPlugin.editorPath;
					EditorPlugin.levelListSlash = "beatmaps/" + EditorPlugin.editorPath + "/";
				});

				GameObject levelListReloader = Instantiate(GameObject.Find("TimelineBar/GameObject/play"));
				levelListReloader.transform.SetParent(EditorManager.inst.GetDialog("Open File Popup").Dialog);
				levelListReloader.GetComponent<RectTransform>().anchoredPosition = EditorPlugin.ORLRefreshPos.Value;
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
				EventTrigger.Entry entry3 = new EventTrigger.Entry();
				entry3.eventID = EventTriggerType.EndDrag;
				entry3.callback.AddListener(delegate (BaseEventData eventData)
				{
					PointerEventData pointerEventData = (PointerEventData)eventData;
					EditorManager.inst.DragEndPos = pointerEventData.position;
					EditorManager.inst.SelectionBoxImage.gameObject.SetActive(false);
					if (EditorManager.inst.layer != 5)
					{
						if (Input.GetKey(KeyCode.LeftShift))
						{
							RTEditor.inst.StartCoroutine(RTEditor.GroupSelectObjects());

							//List<ObjEditor.ObjectSelection> list4 = new List<ObjEditor.ObjectSelection>();
							//foreach (ObjEditor.ObjectSelection objectSelection in ObjEditor.inst.selectedObjects)
							//{
							//	list4.Add(new ObjEditor.ObjectSelection(objectSelection.Type, objectSelection.ID));
							//}
							//foreach (KeyValuePair<string, GameObject> keyValuePair in ObjEditor.inst.beatmapObjects)
							//{
							//	if (EditorManager.RectTransformToScreenSpace(EditorManager.inst.SelectionBoxImage.rectTransform).Overlaps(EditorManager.RectTransformToScreenSpace(keyValuePair.Value.GetComponent<Image>().rectTransform)) && keyValuePair.Value.activeSelf)
							//	{
							//		ObjEditor.inst.AddSelectedObject(new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Object, keyValuePair.Key));
							//	}
							//}
							//foreach (KeyValuePair<string, GameObject> keyValuePair2 in ObjEditor.inst.prefabObjects)
							//{
							//	if (EditorManager.RectTransformToScreenSpace(EditorManager.inst.SelectionBoxImage.rectTransform).Overlaps(EditorManager.RectTransformToScreenSpace(keyValuePair2.Value.GetComponent<Image>().rectTransform)) && keyValuePair2.Value.activeSelf)
							//	{
							//		ObjEditor.inst.AddSelectedObject(new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Prefab, keyValuePair2.Key));
							//	}
							//}
							//foreach (ObjEditor.ObjectSelection obj in list4)
							//{
							//	ObjEditor.inst.RenderTimelineObject(obj);
							//}
							//foreach (ObjEditor.ObjectSelection obj2 in ObjEditor.inst.selectedObjects)
							//{
							//	ObjEditor.inst.RenderTimelineObject(obj2);
							//}
							//if (ObjEditor.inst.selectedObjects.Count() <= 0)
							//{
							//	CheckpointEditor.inst.SetCurrentCheckpoint(0);
							//	return;
							//}
						}
						else
						{
							RTEditor.inst.StartCoroutine(RTEditor.GroupSelectObjects(false));
							//	List<ObjEditor.ObjectSelection> list4 = new List<ObjEditor.ObjectSelection>();
							//	foreach (ObjEditor.ObjectSelection objectSelection in ObjEditor.inst.selectedObjects)
							//	{
							//		list4.Add(new ObjEditor.ObjectSelection(objectSelection.Type, objectSelection.ID));
							//	}
							//	ObjEditor.inst.selectedObjects.Clear();
							//	ObjEditor.inst.RenderTimelineObjects();
							//	foreach (KeyValuePair<string, GameObject> keyValuePair in ObjEditor.inst.beatmapObjects)
							//	{
							//		if (EditorManager.RectTransformToScreenSpace(EditorManager.inst.SelectionBoxImage.rectTransform).Overlaps(EditorManager.RectTransformToScreenSpace(keyValuePair.Value.GetComponent<Image>().rectTransform)) && keyValuePair.Value.activeSelf)
							//		{
							//			ObjEditor.inst.AddSelectedObject(new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Object, keyValuePair.Key));
							//		}
							//	}
							//	foreach (KeyValuePair<string, GameObject> keyValuePair2 in ObjEditor.inst.prefabObjects)
							//	{
							//		if (EditorManager.RectTransformToScreenSpace(EditorManager.inst.SelectionBoxImage.rectTransform).Overlaps(EditorManager.RectTransformToScreenSpace(keyValuePair2.Value.GetComponent<Image>().rectTransform)) && keyValuePair2.Value.activeSelf)
							//		{
							//			ObjEditor.inst.AddSelectedObject(new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Prefab, keyValuePair2.Key));
							//		}
							//	}
							//	foreach (ObjEditor.ObjectSelection obj in list4)
							//	{
							//		ObjEditor.inst.RenderTimelineObject(obj);
							//	}
							//	foreach (ObjEditor.ObjectSelection obj2 in ObjEditor.inst.selectedObjects)
							//	{
							//		ObjEditor.inst.RenderTimelineObject(obj2);
							//	}
							//	if (ObjEditor.inst.selectedObjects.Count() <= 0)
							//	{
							//		CheckpointEditor.inst.SetCurrentCheckpoint(0);
							//		return;
							//	}
						}
					}
					else
					{
						bool flag = false;
						int num2 = 0;
						foreach (List<GameObject> list5 in EventEditor.inst.eventObjects)
						{
							int num3 = 0;
							foreach (GameObject gameObject2 in list5)
							{
								if (EditorManager.RectTransformToScreenSpace(EditorManager.inst.SelectionBoxImage.rectTransform).Overlaps(EditorManager.RectTransformToScreenSpace(gameObject2.transform.GetChild(0).GetComponent<Image>().rectTransform)) && gameObject2.activeSelf)
								{
									if (!flag)
									{
										EventEditor.inst.SetCurrentEvent(num2, num3);
										flag = true;
									}
									else
									{
										EventEditor.inst.AddedSelectedEvent(num2, num3);
									}
								}
								num3++;
							}
							num2++;
						}
					}
				});
				EventTrigger tltrig = EditorManager.inst.timeline.GetComponent<EventTrigger>();

				tltrig.triggers.RemoveAt(3);
				tltrig.triggers.Add(entry3);

				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("persistent/text").gameObject.GetComponent<Text>().text = "No Autokill";
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("persistent").gameObject.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("persistent").gameObject.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("persistent").gameObject.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("persistent").gameObject.GetComponent<Button>().onClick.RemoveAllListeners();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("persistent").gameObject.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					RTEditor.CreateNewNoAutokillObject();
				});

				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("empty").gameObject.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("empty").gameObject.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("empty").gameObject.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("empty").gameObject.GetComponent<Button>().onClick.RemoveAllListeners();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("empty").gameObject.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					RTEditor.CreateNewEmptyObject();
				});

				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("decoration").gameObject.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("decoration").gameObject.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("decoration").gameObject.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("decoration").gameObject.GetComponent<Button>().onClick.RemoveAllListeners();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("decoration").gameObject.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					RTEditor.CreateNewDecorationObject();
				});

				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("helper").gameObject.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("helper").gameObject.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("helper").gameObject.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("helper").gameObject.GetComponent<Button>().onClick.RemoveAllListeners();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("helper").gameObject.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					RTEditor.CreateNewHelperObject();
				});

				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("normal").gameObject.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("normal").gameObject.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("normal").gameObject.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("normal").gameObject.GetComponent<Button>().onClick.RemoveAllListeners();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("normal").gameObject.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					RTEditor.CreateNewNormalObject();
				});

				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/circle").gameObject.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/circle").gameObject.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/circle").gameObject.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/circle").gameObject.GetComponent<Button>().onClick.RemoveAllListeners();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/circle").gameObject.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					RTEditor.CreateNewCircleObject();
				});

				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/triangle").gameObject.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/triangle").gameObject.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/triangle").gameObject.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/triangle").gameObject.GetComponent<Button>().onClick.RemoveAllListeners();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/triangle").gameObject.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					RTEditor.CreateNewTriangleObject();
				});

				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/text").gameObject.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/text").gameObject.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/text").gameObject.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/text").gameObject.GetComponent<Button>().onClick.RemoveAllListeners();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/text").gameObject.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					RTEditor.CreateNewTextObject();
				});

				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/hexagon").gameObject.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/hexagon").gameObject.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/hexagon").gameObject.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/hexagon").gameObject.GetComponent<Button>().onClick.RemoveAllListeners();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/hexagon").gameObject.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					RTEditor.CreateNewHexagonObject();
				});
			}

			//Animate in GUI
			AnimateInGUI openFilePopupAIGUI = EditorManager.inst.GetDialog("Open File Popup").Dialog.gameObject.AddComponent<AnimateInGUI>();
			AnimationCurve animationCurve = new AnimationCurve();
			animationCurve.postWrapMode = WrapMode.Clamp;
			animationCurve.preWrapMode = WrapMode.Clamp;

			openFilePopupAIGUI.animateInCurve = animationCurve;

			GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/Help/Help Dropdown/Join Discord/Text").GetComponent<Text>().text = "Modder's Discord";
			GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/Help/Help Dropdown/Watch Tutorials/Text").GetComponent<Text>().text = "Watch PA History";
			GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/Help/Help Dropdown/Community Guides").SetActive(false);
			GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/Help/Help Dropdown/Which songs can I use?").SetActive(false);
			GameObject.Find("TitleBar/File/File Dropdown/Save As").SetActive(true);
		}

		[HarmonyPatch("Start")]
		[HarmonyPostfix]
		private static void SetEditorStart()
		{
			EditorGUI.CreateEditorGUI();
			EditorGUI.UpdateEditorGUI();

			EditorPlugin.RepeatReminder();

			GameObject folderButton = EditorManager.inst.folderButtonPrefab;
			Button fButtonBUTT = folderButton.GetComponent<Button>();
			Text fButtonText = folderButton.transform.Find("folder-name").GetComponent<Text>();
			//Folder button
			fButtonText.horizontalOverflow = EditorPlugin.FButtonHWrap.Value;
			fButtonText.verticalOverflow = EditorPlugin.FButtonVWrap.Value;
			fButtonText.color = EditorPlugin.FButtonTextColor.Value;
			fButtonText.fontSize = EditorPlugin.FButtonFontSize.Value;

			//Folder Button Colors
			ColorBlock cb = fButtonBUTT.colors;
			cb.normalColor = EditorPlugin.FButtonNColor.Value;
			cb.pressedColor = EditorPlugin.FButtonPColor.Value;
			cb.highlightedColor = EditorPlugin.FButtonHColor.Value;
			cb.selectedColor = EditorPlugin.FButtonSColor.Value;
			cb.fadeDuration = EditorPlugin.FButtonFadeDColor.Value;
			fButtonBUTT.colors = cb;

			EditorPlugin.timeEdit = EditorPlugin.itsTheTime;

			InputDataManager.inst.editorActions.Cut.ClearBindings();
			InputDataManager.inst.editorActions.Copy.ClearBindings();
			InputDataManager.inst.editorActions.Paste.ClearBindings();
			InputDataManager.inst.editorActions.Duplicate.ClearBindings();
			InputDataManager.inst.editorActions.Delete.ClearBindings();
			InputDataManager.inst.editorActions.Undo.ClearBindings();
			InputDataManager.inst.editorActions.Redo.ClearBindings();
		}

		[HarmonyPatch("Update")]
		[HarmonyPostfix]
		private static void UpdateEditor()
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

				bool playingEd = AudioManager.inst.CurrentAudioSource.isPlaying;

				if (playingEd == true)
				{
					Vector2 point = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
					float value = EditorManager.inst.timelineScrollbar.GetComponent<Scrollbar>().value;
					Rect rect = new Rect(0f, 0.305f * (float)Screen.height, (float)Screen.width, (float)Screen.height * 0.025f);
					if (Input.GetMouseButton(0) && rect.Contains(point))
					{
						if (Mathf.Abs(EditorManager.inst.audioTimeForSlider / EditorManager.inst.Zoom - EditorManager.inst.prevAudioTime) < 100f)
						{
							AudioManager.inst.CurrentAudioSource.Play();
							EditorManager.inst.UpdatePlayButton();
						}
					}
				}
				if (GameObject.Find("Editor Systems/Editor GUI/sizer/main/Popups/File Info Popup/loading") && GameObject.Find("Editor Systems/Editor GUI/sizer/main/Popups/File Info Popup/loading").activeSelf == true)
				{
					Image image = GameObject.Find("Editor Systems/Editor GUI/sizer/main/Popups/File Info Popup/loading").GetComponent<Image>();
					image.sprite = EditorManager.inst.loadingImage.sprite;
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

			if (EditorPlugin.ShowObjectsOnLayer.Value == true)
			{
				foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
				{
					if (beatmapObject != null && ObjectManager.inst.beatmapGameObjects.ContainsKey(beatmapObject.id) && beatmapObject.editorData.Layer != EditorManager.inst.layer && EditorManager.inst.layer != 5)
					{
						if (beatmapObject.shape != 4)
						{
							ObjectManager.GameObjectRef gameObjectRef = ObjectManager.inst.beatmapGameObjects[beatmapObject.id];
							Color objColor = gameObjectRef.mat.color;
							gameObjectRef.mat.color = new Color(objColor.r, objColor.g, objColor.b, objColor.a * EditorPlugin.ShowObjectsAlpha.Value);
						}
						else
						{
							ObjectManager.GameObjectRef gameObjectRef = ObjectManager.inst.beatmapGameObjects[beatmapObject.id];
							Color objColor = gameObjectRef.obj.GetComponentInChildren<TMPro.TMP_Text>().color;
							gameObjectRef.obj.GetComponentInChildren<TMPro.TMP_Text>().color = new Color(objColor.r, objColor.g, objColor.b, objColor.a * EditorPlugin.ShowObjectsAlpha.Value);
						}
					}
				}
			}
		}

		[HarmonyPatch("SaveBeatmap")]
		[HarmonyPostfix]
		private static void SaveEditorManager()
		{
			DataManager.inst.gameData.beatmapData.editorData.timelinePos = AudioManager.inst.CurrentAudioSource.time;
			DataManager.inst.metaData.song.BPM = SettingEditor.inst.SnapBPM;
			DataManager.inst.gameData.beatmapData.levelData.backgroundColor = EditorManager.inst.layer;
			EditorPlugin.scrollBar = GameObject.Find("Editor Systems/Editor GUI/sizer/main/whole-timeline/Scrollbar").GetComponent<Scrollbar>().value;

			Sprite waveform = EditorManager.inst.timeline.GetComponent<Image>().sprite;
			if (EditorPlugin.WaveformMode.Value == EditorPlugin.WaveformType.Legacy && EditorPlugin.GenerateWaveform.Value == true)
			{
				File.WriteAllBytes(RTFile.GetApplicationDirectory() + GameManager.inst.basePath + "waveform.png", EditorManager.inst.timeline.GetComponent<Image>().sprite.texture.EncodeToPNG());
			}
			if (EditorPlugin.WaveformMode.Value == EditorPlugin.WaveformType.Old && EditorPlugin.GenerateWaveform.Value == true)
			{
				File.WriteAllBytes(RTFile.GetApplicationDirectory() + GameManager.inst.basePath + "waveform_old.png", EditorManager.inst.timeline.GetComponent<Image>().sprite.texture.EncodeToPNG());
			}

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

				RTFile.WriteToFile("beatmaps/" + EditorPlugin.editorPath + "/" + EditorManager.inst.currentLoadedLevel + "/editor.lse", jsonnode.ToString(3));
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

				RTFile.WriteToFile("beatmaps/" + EditorPlugin.editorPath + "/" + EditorManager.inst.currentLoadedLevel + "/editor.lse", jsonnode.ToString(3));
			}
		}

		[HarmonyPatch("SaveBeatmapAs", new Type[] { })]
		[HarmonyPostfix]
		private static void FixSaveAs()
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
		private static void RBSPlug()
		{
			EditorPlugin.RenderBeatmapSet();
		}

		[HarmonyPatch("OpenBeatmapPopup")]
		[HarmonyPostfix]
		private static void SetEditorRenderOBP()
		{
			//Create Local Variables
			GameObject openLevel = EditorManager.inst.GetDialog("Open File Popup").Dialog.gameObject;
			Transform openTLevel = openLevel.transform;
			RectTransform openRTLevel = openLevel.GetComponent<RectTransform>();
			GridLayoutGroup openGridLVL = openTLevel.Find("mask/content").GetComponent<GridLayoutGroup>();

			//Set Editor Zoom cap
			EditorManager.inst.zoomBounds = EditorPlugin.ETLZoomBounds.Value;

			//Set Open File Popup RectTransform
			openRTLevel.anchoredPosition = EditorPlugin.ORLAnchoredPos.Value;
			openRTLevel.localRotation = EditorPlugin.ORLLocalRot.Value;
			openRTLevel.sizeDelta = EditorPlugin.ORLSizeDelta.Value;

			//Set Open FIle Popup content GridLayoutGroup
			openGridLVL.cellSize = EditorPlugin.OGLVLCellSize.Value;
			openGridLVL.constraint = EditorPlugin.OGLVLConstraint.Value;
			openGridLVL.constraintCount = EditorPlugin.OGLVLConstraintCount.Value;
			openGridLVL.spacing = EditorPlugin.OGLVLSpacing.Value;
		}

		[HarmonyPatch("AssignWaveformTextures")]
		[HarmonyPrefix]
		private static bool RunTimelineWaveform()
		{
			return false;
		}

		[HarmonyPatch("RenderTimeline")]
		[HarmonyPostfix]
		private static void RenderLayers()
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

		[HarmonyPatch("LoadLevel", MethodType.Enumerator)]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> LoadLevelTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			return new CodeMatcher(instructions)
				.Start()
				.Advance(60)
				.ThrowIfNotMatch("Is not beatmaps/editor", new CodeMatch(OpCodes.Ldstr))
				.SetInstruction(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(EditorPlugin), "levelListSlash")))
				.ThrowIfNotMatch("Is not ldsfld", new CodeMatch(OpCodes.Ldsfld))
				.InstructionEnumeration();
		}

		[HarmonyPatch("CreateNewLevel")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> CreateLevelTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			return new CodeMatcher(instructions)
				.Start()
				.Advance(25)
				.ThrowIfNotMatch("Is not 25 beatmaps/editor/", new CodeMatch(OpCodes.Ldstr))
				.SetInstruction(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(EditorPlugin), "levelListSlash")))
				.ThrowIfNotMatch("Is not ldsfld 1", new CodeMatch(OpCodes.Ldsfld))
				.Advance(14)
				.ThrowIfNotMatch("Is not 39 beatmaps/editor/", new CodeMatch(OpCodes.Ldstr))
				.SetInstruction(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(EditorPlugin), "levelListSlash")))
				.ThrowIfNotMatch("Is not ldsfld 2", new CodeMatch(OpCodes.Ldsfld))
				.Advance(13)
				.ThrowIfNotMatch("Is not 52 beatmaps/editor/", new CodeMatch(OpCodes.Ldstr))
				.SetInstruction(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(EditorPlugin), "levelListSlash")))
				.ThrowIfNotMatch("Is not ldsfld 3", new CodeMatch(OpCodes.Ldsfld))
				.Advance(14)
				.ThrowIfNotMatch("Is not 66 beatmaps/editor/", new CodeMatch(OpCodes.Ldstr))
				.SetInstruction(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(EditorPlugin), "levelListSlash")))
				.ThrowIfNotMatch("Is not ldsfld 4", new CodeMatch(OpCodes.Ldsfld))
				.Advance(16)
				.ThrowIfNotMatch("Is not 82 20.4.4", new CodeMatch(OpCodes.Ldstr))
				.SetInstruction(new CodeInstruction(OpCodes.Ldstr, "4.1.16"))
				.Advance(24)
				.ThrowIfNotMatch("Is not 106 beatmaps/editor/", new CodeMatch(OpCodes.Ldstr))
				.SetInstruction(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(EditorPlugin), "levelListSlash")))
				.ThrowIfNotMatch("Is not ldsfld 5", new CodeMatch(OpCodes.Ldsfld))
				.InstructionEnumeration();
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
			if (EditorPlugin.ReminderActive.Value == true)
			{
				int radfs = UnityEngine.Random.Range(0, 5);
				string randomtext = "";
				if (radfs == 0)
				{
					randomtext = ". You should touch some grass.";
				}
				if (radfs == 1)
				{
					randomtext = ". Have a break.";
				}
				if (radfs == 2)
				{
					randomtext = ". You doing alright there?";
				}
				if (radfs == 3)
				{
					randomtext = ". You might need some rest.";
				}
				if (radfs == 4)
				{
					randomtext = ". Best thing to do in this situation is to have a break.";
				}
				if (radfs == 5)
				{
					randomtext = ". Anyone there?";
				}

				EditorManager.inst.DisplayNotification("You've been working on the game for " + RTEditor.secondsToTime(Time.time) + randomtext, 2f, EditorManager.NotificationType.Warning, false);
			}
		}

		public static void ThingOfTest()
        {
			EditorManager.EditorDialog.DialogType dialogType = new EditorManager.EditorDialog.DialogType();
        }
	}
}
