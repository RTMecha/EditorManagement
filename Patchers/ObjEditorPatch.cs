﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using HarmonyLib;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using SimpleJSON;
using Crosstales.FB;
using TMPro;
using LSFunctions;

using EditorManagement.Functions;
using EditorManagement.Functions.Components;
using EditorManagement.Functions.Editors;
using EditorManagement.Functions.Helpers;

using RTFunctions.Functions;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Data;
using RTFunctions.Functions.Managers;
using RTFunctions.Patchers;

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

namespace EditorManagement.Patchers
{
	[HarmonyPatch(typeof(ObjEditor))]
    public class ObjEditorPatch : MonoBehaviour
    {
        static ObjEditor Instance { get => ObjEditor.inst; set => ObjEditor.inst = value; }
		static Type Type { get; set; }

        #region Properties

        static RectTransform timelineScroll;
        public static RectTransform TimelineScroll
        {
            get
            {
                if (!timelineScroll)
                    timelineScroll = Instance.timelineScroll.GetComponent<RectTransform>();
                return timelineScroll;
            }
        }

        #endregion

		#region Patches


		[HarmonyPatch("SetCurrentObj")]
		[HarmonyPrefix]
		static bool SetCurrentObjPrefix() => EditorPlugin.DontRun();
		
		[HarmonyPatch("UpdateHighlightedKeyframe")]
		[HarmonyPrefix]
		static bool UpdateHighlightedKeyframePrefix() => EditorPlugin.DontRun();
		
		[HarmonyPatch("DeRenderObject")]
		[HarmonyPrefix]
		static bool DeRenderObjectPrefix() => EditorPlugin.DontRun();
		
		[HarmonyPatch("RenderTimelineObject")]
		[HarmonyPrefix]
		static bool RenderTimelineObjectPrefix() => EditorPlugin.DontRun();
		
		[HarmonyPatch("RenderTimelineObjects")]
		[HarmonyPrefix]
		static bool RenderTimelineObjectsPrefix() => EditorPlugin.DontRun();
		
		[HarmonyPatch("DeleteObject")]
		[HarmonyPrefix]
		static bool DeleteObjectPrefix() => EditorPlugin.DontRun();
		
		[HarmonyPatch("DeleteObjects")]
		[HarmonyPrefix]
		static bool DeleteObjectsPrefix() => EditorPlugin.DontRun();
		
		[HarmonyPatch("AddPrefabExpandedToLevel")]
		[HarmonyPrefix]
		static bool AddPrefabExpandedToLevelPrefix() => EditorPlugin.DontRun();
		
		[HarmonyPatch("AddSelectedObject")]
		[HarmonyPrefix]
		static bool AddSelectedObjectPrefix() => EditorPlugin.DontRun();
		
		[HarmonyPatch("AddSelectedObjectOnly")]
		[HarmonyPrefix]
		static bool AddSelectedObjectOnlyPrefix() => EditorPlugin.DontRun();
		
		[HarmonyPatch("ContainedInSelectedObjects")]
		[HarmonyPrefix]
		static bool ContainedInSelectedObjectsPrefix() => EditorPlugin.DontRun();
		
		[HarmonyPatch("RefreshParentGUI")]
		[HarmonyPrefix]
		static bool RefreshParentGUIPrefix() => EditorPlugin.DontRun();

		[HarmonyPatch("Awake")]
		[HarmonyPrefix]
        static bool AwakePrefix(ObjEditor __instance)
		{
			//og code
			{
				if (!Instance)
					Instance = __instance;
				else if (Instance != __instance)
					Destroy(__instance.gameObject);

				__instance.timelineKeyframes.Add(new List<GameObject>());
				__instance.timelineKeyframes.Add(new List<GameObject>());
				__instance.timelineKeyframes.Add(new List<GameObject>());
				__instance.timelineKeyframes.Add(new List<GameObject>());

				var beginDragTrigger = new EventTrigger.Entry();
				beginDragTrigger.eventID = EventTriggerType.BeginDrag;
				beginDragTrigger.callback.AddListener(delegate (BaseEventData eventData)
				{
					//Debug.Log("START DRAG");
					PointerEventData pointerEventData = (PointerEventData)eventData;
					__instance.SelectionBoxImage.gameObject.SetActive(true);
					__instance.DragStartPos = pointerEventData.position * EditorManager.inst.ScreenScaleInverse;
					__instance.SelectionRect = default(Rect);
					//Debug.Log("Start Drag");
				});

				var dragTrigger = new EventTrigger.Entry();
				dragTrigger.eventID = EventTriggerType.Drag;
				dragTrigger.callback.AddListener(delegate (BaseEventData eventData)
				{
					Vector3 vector = ((PointerEventData)eventData).position * EditorManager.inst.ScreenScaleInverse;
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

				var endDragTrigger = new EventTrigger.Entry();
				endDragTrigger.eventID = EventTriggerType.EndDrag;
				endDragTrigger.callback.AddListener(delegate (BaseEventData eventData)
				{
					PointerEventData pointerEventData = (PointerEventData)eventData;
					__instance.DragEndPos = pointerEventData.position;
					__instance.SelectionBoxImage.gameObject.SetActive(false);

					RTEditor.inst.StartCoroutine(ObjectEditor.inst.GroupSelectKeyframes(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)));
				});
				foreach (var gameObject in __instance.SelectionArea)
				{
					var eventTr = gameObject.GetComponent<EventTrigger>();
					eventTr.triggers.Add(beginDragTrigger);
					eventTr.triggers.Add(dragTrigger);
					eventTr.triggers.Add(endDragTrigger);
				}
			}

			//Add spacer
			var contentParent = GameObject.Find("GameObjectDialog/data/left/Scroll View/Viewport/Content").transform;
			var spacer = new GameObject("spacer");
			spacer.transform.parent = contentParent;
			spacer.transform.SetSiblingIndex(15);

			var spRT = spacer.AddComponent<RectTransform>();
			var spHLG = spacer.AddComponent<HorizontalLayoutGroup>();

			spRT.sizeDelta = new Vector2(30f, 30f);
			spHLG.spacing = 8;

			var singleInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move/position/x");

			Destroy(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/autokill/tod-dropdown").GetComponent<HideDropdownOptions>());

			GameObject.Find("Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/name/name").GetComponent<InputField>().characterLimit = 0;

			// Depth
			{
				var depth = Instantiate(singleInput);
				depth.transform.SetParent(spacer.transform);
				depth.transform.localScale = Vector3.one;
				depth.name = "depth";
				depth.transform.Find("input").GetComponent<RectTransform>().sizeDelta = new Vector2(110f, 32f);

				Destroy(depth.GetComponent<EventInfo>());

				var depthif = depth.GetComponent<InputField>();
				depthif.onValueChanged.RemoveAllListeners();

				var sliderObject = ObjEditor.inst.ObjectView.transform.Find("depth/depth").gameObject;

				Destroy(ObjEditor.inst.ObjectView.transform.Find("depth/<").gameObject);
				Destroy(ObjEditor.inst.ObjectView.transform.Find("depth/>").gameObject);

				sliderObject.GetComponent<RectTransform>().sizeDelta = new Vector2(352f, 32f);
				ObjEditor.inst.ObjectView.transform.Find("depth").GetComponent<RectTransform>().sizeDelta = new Vector2(261f, 32f);
			}

			// Lock
			{
				var timeParent = ObjEditor.inst.ObjectView.transform.Find("time");

				var locker = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/toggle/toggle"));
				locker.transform.SetParent(timeParent.transform);
				locker.transform.localScale = Vector3.one;
				locker.transform.SetAsFirstSibling();
				locker.name = "lock";

				var timeLayout = timeParent.GetComponent<HorizontalLayoutGroup>();
				timeLayout.childControlWidth = false;
				timeLayout.childForceExpandWidth = false;

				locker.GetComponent<RectTransform>().sizeDelta = new Vector2(32f, 32f);

				var time = timeParent.Find("time");
				time.GetComponent<RectTransform>().sizeDelta = new Vector2(151, 32f);

				locker.transform.Find("Background/Checkmark").GetComponent<Image>().sprite = ObjEditor.inst.timelineObjectPrefabLock.transform.Find("lock (1)").GetComponent<Image>().sprite;

				timeParent.Find("<<").GetComponent<RectTransform>().sizeDelta = new Vector2(32f, 32f);
				timeParent.Find("<").GetComponent<RectTransform>().sizeDelta = new Vector2(16f, 32f);
				timeParent.Find("|").GetComponent<RectTransform>().sizeDelta = new Vector2(16f, 32f);
				timeParent.Find(">").GetComponent<RectTransform>().sizeDelta = new Vector2(16f, 32f);
				timeParent.Find(">>").GetComponent<RectTransform>().sizeDelta = new Vector2(32f, 32f);
			}

			// Colors
			{
				var colorParent = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/color/color").transform;
				colorParent.GetComponent<GridLayoutGroup>().spacing = new Vector2(9.32f, 9.32f);
				for (int i = 10; i < 19; i++)
				{
					GameObject col = Instantiate(colorParent.Find("9").gameObject);
					col.name = i.ToString();
					col.transform.SetParent(colorParent);
				}
			}

			// Origin X / Y
			{
				var contentOriginTF = ObjEditor.inst.ObjectView.transform.Find("origin").transform;

				contentOriginTF.Find("origin-x").gameObject.SetActive(false);
				contentOriginTF.Find("origin-y").gameObject.SetActive(false);

				var xo = Instantiate(singleInput);
				xo.transform.SetParent(contentOriginTF.transform);
				xo.transform.localScale = Vector3.one;
				xo.name = "x";
				xo.transform.Find("input").GetComponent<RectTransform>().sizeDelta = new Vector2(110f, 32f);

				Destroy(xo.GetComponent<EventInfo>());

				var xoif = xo.GetComponent<InputField>();
				xoif.onValueChanged.RemoveAllListeners();

				var yo = Instantiate(singleInput);
				yo.transform.SetParent(contentOriginTF.transform);
				yo.transform.localScale = Vector3.one;
				yo.name = "y";
				yo.transform.Find("input").GetComponent<RectTransform>().sizeDelta = new Vector2(110f, 32f);

				Destroy(yo.GetComponent<EventInfo>());

				var yoif = yo.GetComponent<InputField>();
				yoif.onValueChanged.RemoveAllListeners();
			}

			// Opacity
			{
				var opacityLabel = Instantiate(__instance.KeyframeDialogs[3].transform.Find("label").gameObject);
				opacityLabel.transform.SetParent(__instance.KeyframeDialogs[3].transform);
				opacityLabel.transform.localScale = Vector3.one;
				opacityLabel.transform.GetChild(0).GetComponent<Text>().text = "Opacity";
				opacityLabel.name = "label";

				var opacity = Instantiate(__instance.KeyframeDialogs[2].transform.Find("rotation").gameObject);
				opacity.transform.SetParent(__instance.KeyframeDialogs[3].transform);
				opacity.transform.localScale = Vector3.one;
				opacity.name = "opacity";

				//Triggers.AddTooltip(opacity.gameObject, "Set the opacity percentage here.", "If the color is already slightly transparent or the object is a helper, it will multiply the current opacity value with the helper and/or color alpha values.");
			}

			// Hue/Sat/Val
			{
				var opacityLabel = __instance.KeyframeDialogs[2].transform.Find("label").gameObject.Duplicate(__instance.KeyframeDialogs[3].transform);
				opacityLabel.transform.GetChild(0).GetComponent<Text>().text = "Hue";
				opacityLabel.name = "label";

				opacityLabel.AddComponent<HorizontalLayoutGroup>();

				var n2 = Instantiate(opacityLabel.transform.GetChild(0).gameObject);
				n2.transform.SetParent(opacityLabel.transform);
				n2.transform.localScale = Vector3.one;
				if (n2.TryGetComponent(out Text saturation))
					saturation.text = "Saturation";

				var n3 = Instantiate(opacityLabel.transform.GetChild(0).gameObject);
				n3.transform.SetParent(opacityLabel.transform);
				n3.transform.localScale = Vector3.one;
				if (n3.TryGetComponent(out Text value))
					value.text = "Value";

				var opacity = Instantiate(__instance.KeyframeDialogs[1].transform.Find("scale").gameObject);
				opacity.transform.SetParent(__instance.KeyframeDialogs[3].transform);
				opacity.transform.localScale = Vector3.one;
				opacity.name = "huesatval";

				var z = Instantiate(opacity.transform.GetChild(1).gameObject);
				z.transform.SetParent(opacity.transform);
				z.transform.localScale = Vector3.one;
				z.name = "z";

				for (int i = 0; i < opacity.transform.childCount; i++)
				{
					if (!opacity.transform.GetChild(i).GetComponent<InputFieldSwapper>())
						opacity.transform.GetChild(i).gameObject.AddComponent<InputFieldSwapper>();

					var horizontal = opacity.transform.GetChild(i).GetComponent<HorizontalLayoutGroup>();
					var input = opacity.transform.GetChild(i).Find("input").GetComponent<RectTransform>();

					horizontal.childControlWidth = false;

					input.sizeDelta = new Vector2(60f, 32f);

					var layout = opacity.transform.GetChild(i).GetComponent<LayoutElement>();
					layout.minWidth = 109f;
				}

				//Triggers.AddTooltip(opacity.gameObject, "Set the hue value here.", "Shifts the hue levels of the objects' color.");
			}

			// Position Z
			{
				var positionBase = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/position/position");

				var posZ = Instantiate(positionBase.transform.Find("x"));
				posZ.transform.SetParent(positionBase.transform);
				posZ.transform.localScale = Vector3.one;
				posZ.name = "z";

				positionBase.GetComponent<RectTransform>().sizeDelta = new Vector2(553f, 64f);
				DestroyImmediate(positionBase.GetComponent<HorizontalLayoutGroup>());
				var grp = positionBase.AddComponent<GridLayoutGroup>();
				grp.cellSize = new Vector2(183f, 40f);
			}

			// Layers
			{
				if (ObjEditor.inst.ObjectView.transform.Find("spacer"))
					ObjEditor.inst.ObjectView.transform.GetChild(17).GetChild(1).gameObject.SetActive(true);
				else
					ObjEditor.inst.ObjectView.transform.GetChild(16).GetChild(1).gameObject.SetActive(true);

				//ObjEditor.inst.ObjectView.transform.Find("editor/bin").gameObject.SetActive(true);

				Destroy(ObjEditor.inst.ObjectView.transform.Find("editor/layer").gameObject);

				GameObject layers = Instantiate(ObjEditor.inst.ObjectView.transform.Find("time/time").gameObject);

				layers.transform.SetParent(ObjEditor.inst.ObjectView.transform.Find("editor"));
				layers.name = "layers";
				layers.transform.SetSiblingIndex(0);
				RectTransform layersRT = layers.GetComponent<RectTransform>();
				InputField layersIF = layers.GetComponent<InputField>();
				Image layersImage = layers.GetComponent<Image>();

				layersIF.characterValidation = InputField.CharacterValidation.Integer;

				HorizontalLayoutGroup edhlg = ObjEditor.inst.ObjectView.transform.Find("editor").GetComponent<HorizontalLayoutGroup>();
				edhlg.childControlWidth = false;
				edhlg.childForceExpandWidth = false;

				layersRT.sizeDelta = new Vector2(100f, 32f);
				ObjEditor.inst.ObjectView.transform.Find("editor/bin").GetComponent<RectTransform>().sizeDelta = new Vector2(237f, 32f);
			}

			// Clear Parent
			{
				GameObject close = GameObject.Find("Editor Systems/Editor GUI/sizer/main/Popups/Open File Popup/Panel/x");

				GameObject parent = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/parent");

				parent.GetComponent<HorizontalLayoutGroup>().childControlWidth = false;
				parent.GetComponent<HorizontalLayoutGroup>().spacing = 4f;

				parent.transform.Find("text").GetComponent<RectTransform>().sizeDelta = new Vector2(201f, 32f);

				var resetParent = Instantiate(close);
				resetParent.transform.SetParent(parent.transform);
				resetParent.transform.localScale = Vector3.one;
				resetParent.name = "clear parent";
				resetParent.transform.SetSiblingIndex(1);

				//var resetParentButton = resetParent.GetComponent<Button>();

				//resetParentButton.onClick.ClearAll();
				//resetParentButton.onClick.AddListener(delegate ()
				//{
				//	ObjEditor.inst.currentObjectSelection.GetObjectData().parent = "";

				//	RTEditor.inst.StartCoroutine(RTEditor.RefreshObjectGUI(ObjEditor.inst.currentObjectSelection.GetObjectData()));

				//	ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
				//});

				var parentPicker = Instantiate(close);
				parentPicker.transform.SetParent(parent.transform);
				parentPicker.transform.localScale = Vector3.one;
				parentPicker.name = "parent picker";
				parentPicker.transform.SetSiblingIndex(2);

				var parentPickerButton = parentPicker.GetComponent<Button>();

				//parentPickerButton.onClick.ClearAll();
				//parentPickerButton.onClick.AddListener(delegate ()
				//{
				//	RTEditor.inst.parentPickerEnabled = true;
				//});

				var cb = parentPickerButton.colors;
				cb.normalColor = new Color(0.0569f, 0.4827f, 0.9718f, 1f);
				parentPickerButton.colors = cb;

				if (parentPicker.transform.childCount >= 0 && RTExtensions.TryFind("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/left/theme/theme/viewport/content/player0/preview/dropper", out GameObject dropper) && dropper.TryGetComponent(out Image dropperImage))
					parentPicker.transform.GetChild(0).GetComponent<Image>().sprite = dropperImage.sprite;

				parent.transform.Find("parent").GetComponent<RectTransform>().sizeDelta = new Vector2(32f, 32f);
				parent.transform.Find("more").GetComponent<RectTransform>().sizeDelta = new Vector2(32f, 32f);
			}

            // ID
            {
				var label = ObjEditor.inst.ObjectView.transform.GetChild(0);
				var id = label.gameObject.Duplicate(ObjEditor.inst.ObjectView.transform);
				id.transform.SetSiblingIndex(0);
				id.name = "id";
			}

			Destroy(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/rotation").transform.GetChild(1).gameObject);
			Destroy(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/color").transform.GetChild(1).gameObject);

			return false;
        }

		[HarmonyPatch("Start")]
		[HarmonyPrefix]
		static bool StartPrefix(ObjEditor __instance)
		{
			__instance.colorButtons.Clear();
			for (int i = 1; i <= 18; i++)
			{
				__instance.colorButtons.Add(__instance.KeyframeDialogs[3].transform.Find("color/" + i).GetComponent<Toggle>());
			}

			if (RTFile.FileExists(Application.persistentDataPath + "/copied_objects.lsp"))
			{
				JSONNode jn = JSON.Parse(FileManager.inst.LoadJSONFileRaw(Application.persistentDataPath + "/copied_objects.lsp"));

				List<BaseBeatmapObject> _objects = new List<BaseBeatmapObject>();
				for (int aIndex = 0; aIndex < jn["objects"].Count; ++aIndex)
					_objects.Add(BaseBeatmapObject.ParseGameObject(jn["objects"][aIndex]));

				List<BasePrefabObject> _prefabObjects = new List<BasePrefabObject>();
				for (int aIndex = 0; aIndex < jn["prefab_objects"].Count; ++aIndex)
					_prefabObjects.Add(DataManager.inst.gameData.ParsePrefabObject(jn["prefab_objects"][aIndex]));

				__instance.beatmapObjCopy = new BasePrefab(jn["name"], jn["type"].AsInt, jn["offset"].AsFloat, _objects, _prefabObjects);
				__instance.hasCopiedObject = true;
			}

			__instance.zoomBounds = Vector2.zero;
			return false;
		}

		[HarmonyPatch("Update")]
		[HarmonyPrefix]
		static bool UpdatePrefix(ObjEditor __instance)
        {
			if (!EditorManager.inst.IsUsingInputField())
			{
				// Replace this with new Keybind system.
				if (InputDataManager.inst.editorActions.FirstKeyframe.WasPressed)
					ObjectEditor.inst.SetCurrentKeyframe(ObjectEditor.inst.CurrentBeatmapObjectSelection.Data, 0, true);
				if (InputDataManager.inst.editorActions.BackKeyframe.WasPressed)
					ObjectEditor.inst.AddCurrentKeyframe(ObjectEditor.inst.CurrentBeatmapObjectSelection.Data, -1, true);
				if (InputDataManager.inst.editorActions.ForwardKeyframe.WasPressed)
					ObjectEditor.inst.AddCurrentKeyframe(ObjectEditor.inst.CurrentBeatmapObjectSelection.Data, 1, true);
				if (InputDataManager.inst.editorActions.LastKeyframe.WasPressed)
					ObjectEditor.inst.AddCurrentKeyframe(ObjectEditor.inst.CurrentBeatmapObjectSelection.Data, 10000, true);
				if (InputDataManager.inst.editorActions.LockObject.WasPressed)
					ObjEditor.inst.ToggleLockCurrentSelection();
			}

			if (!ObjEditor.inst.changingTime && ObjectEditor.inst.CurrentSelection)
			{
				// Sets new audio time using the Object Keyframe timeline cursor.
				ObjEditor.inst.newTime = Mathf.Clamp(EditorManager.inst.CurrentAudioPos, ObjectEditor.inst.CurrentSelection.StartTime, ObjectEditor.inst.CurrentSelection.StartTime + ObjectEditor.inst.CurrentSelection.GetObjectLifeLength(ObjEditor.inst.ObjectLengthOffset));
				ObjEditor.inst.objTimelineSlider.value = ObjEditor.inst.newTime;
			}

			if (Input.GetMouseButtonUp(0))
			{
				ObjEditor.inst.beatmapObjectsDrag = false;
				ObjEditor.inst.timelineKeyframesDrag = false;
			}

			if (ObjectEditor.inst.SelectedObjectCount > 1 && ObjEditor.inst.beatmapObjectsDrag)
			{
				if (InputDataManager.inst.editorActions.MultiSelect.IsPressed)
				{
					int num1 = 14 - Mathf.RoundToInt((float)((Input.mousePosition.y - 25.0) * EditorManager.inst.ScreenScaleInverse / 20.0)) + ObjEditor.inst.mouseOffsetYForDrag;
					foreach (var timelineObject in ObjectEditor.inst.SelectedBeatmapObjects)
					{
						if (timelineObject.Data && !timelineObject.Data.editorData.locked)
							timelineObject.Data.editorData.Bin = Mathf.Clamp(num1 + timelineObject.binOffset, 0, 14);
						ObjectEditor.RenderTimelineObject(timelineObject);
					}
					foreach (var timelineObject in ObjectEditor.inst.SelectedBeatmapObjects)
					{
						if (timelineObject.Data && !timelineObject.Data.editorData.locked)
							timelineObject.Data.editorData.Bin = Mathf.Clamp(num1 + timelineObject.binOffset, 0, 14);
						ObjectEditor.RenderTimelineObject(timelineObject);
					}
				}
				else
				{
					float num3 = Mathf.Round(Mathf.Clamp(EditorManager.inst.GetTimelineTime() + ObjEditor.inst.mouseOffsetXForDrag, 0.0f, AudioManager.inst.CurrentAudioSource.clip.length) * 1000f) / 1000f;
					if (ObjectEditor.inst.CurrentSelection && !ObjectEditor.inst.CurrentSelection.editorData.locked)
						ObjectEditor.inst.CurrentSelection.StartTime = num3;
					else if (ObjectEditor.inst.CurrentPrefabObjectSelection && ObjectEditor.inst.CurrentPrefabObjectSelection.Data && !ObjectEditor.inst.CurrentPrefabObjectSelection.Data.editorData.locked)
						ObjectEditor.inst.CurrentPrefabObjectSelection.Data.StartTime = num3;
					if (ObjectEditor.inst.CurrentBeatmapObjectSelection)
						ObjectEditor.RenderTimelineObject(ObjectEditor.inst.CurrentBeatmapObjectSelection);
					if (ObjectEditor.inst.CurrentPrefabObjectSelection)
						ObjectEditor.RenderTimelineObject(ObjectEditor.inst.CurrentPrefabObjectSelection);

					foreach (var timelineObject in ObjectEditor.inst.SelectedBeatmapObjects)
					{
						if (timelineObject.Data && !timelineObject.Data.editorData.locked)
							timelineObject.Data.StartTime = Mathf.Clamp(num3 + timelineObject.timeOffset, 0.0f, AudioManager.inst.CurrentAudioSource.clip.length);
						ObjectEditor.RenderTimelineObject(timelineObject);
					}

					foreach (var timelineObject in ObjectEditor.inst.SelectedPrefabObjects)
					{
						if (timelineObject.Data && !timelineObject.Data.editorData.locked)
							timelineObject.Data.StartTime = Mathf.Clamp(num3 + timelineObject.timeOffset, 0.0f, AudioManager.inst.CurrentAudioSource.clip.length);
						ObjectEditor.RenderTimelineObject(timelineObject);
					}
				}
			}
			else if (ObjEditor.inst.beatmapObjectsDrag)
			{
				if (InputDataManager.inst.editorActions.MultiSelect.IsPressed)
				{
					int num = 14 - Mathf.RoundToInt((float)((Input.mousePosition.y - 25.0) * EditorManager.inst.ScreenScaleInverse / 20.0));
					if (ObjectEditor.inst.CurrentBeatmapObjectSelection.Data && !ObjectEditor.inst.CurrentBeatmapObjectSelection.Data.editorData.locked)
						ObjectEditor.inst.CurrentBeatmapObjectSelection.Data.editorData.Bin = Mathf.Clamp(num, 0, 14);
					else if (ObjectEditor.inst.CurrentPrefabObjectSelection.Data && !ObjectEditor.inst.CurrentPrefabObjectSelection.Data.editorData.locked)
						ObjectEditor.inst.CurrentPrefabObjectSelection.Data.editorData.Bin = Mathf.Clamp(num, 0, 14);
				}
				else
				{
					float num = Mathf.Round(EditorManager.inst.GetTimelineTime(ObjEditor.inst.mouseOffsetXForDrag) * 1000f) / 1000f;
					if (ObjectEditor.inst.CurrentBeatmapObjectSelection &&
						ObjectEditor.inst.CurrentBeatmapObjectSelection.Data &&
						!ObjectEditor.inst.CurrentBeatmapObjectSelection.Data.editorData.locked)
						ObjectEditor.inst.CurrentBeatmapObjectSelection.Data.StartTime = Mathf.Clamp(num, 0.0f, AudioManager.inst.CurrentAudioSource.clip.length);
					else if (ObjectEditor.inst.CurrentPrefabObjectSelection &&
						ObjectEditor.inst.CurrentPrefabObjectSelection.Data &&
						!ObjectEditor.inst.CurrentPrefabObjectSelection.Data.editorData.locked)
						ObjectEditor.inst.CurrentPrefabObjectSelection.Data.StartTime = Mathf.Clamp(num, 0.0f, AudioManager.inst.CurrentAudioSource.clip.length);
				}

				if (ObjectEditor.inst.CurrentBeatmapObjectSelection)
					ObjectEditor.RenderTimelineObject(ObjectEditor.inst.CurrentBeatmapObjectSelection);
				if (ObjectEditor.inst.CurrentPrefabObjectSelection)
					ObjectEditor.RenderTimelineObject(ObjectEditor.inst.CurrentPrefabObjectSelection);
			}

			Dragger();
			return false;
		}

		static void Dragger()
		{
			if (ObjEditor.inst.timelineKeyframesDrag)
			{
				foreach (var timelineObject in ObjectEditor.inst.SelectedBeatmapObjectKeyframes)
				{
					if (timelineObject.Index != 0)
					{
						float num6 = timeCalc() + ObjEditor.inst.selectedKeyframeOffsets[ObjectEditor.inst.SelectedBeatmapObjectKeyframes.IndexOf(timelineObject)] + ObjEditor.inst.mouseOffsetXForKeyframeDrag;
						num6 = Mathf.Clamp(num6, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
						num6 = Mathf.Round(num6 * 1000f) / 1000f;

						float calc = Mathf.Clamp(num6, 0f, ObjectEditor.inst.CurrentBeatmapObjectSelection.Data.GetObjectLifeLength(ObjEditor.inst.ObjectLengthOffset));

						if (SettingEditor.inst.SnapActive && RTEditor.BPMSnapKeyframes)
						{
							float st = ObjectEditor.inst.CurrentBeatmapObjectSelection.Data.StartTime;
							float kf = calc;

							if (og == 0)
								og = RTEditor.SnapToBPM(st + kf);

							if (og != RTEditor.SnapToBPM(st + kf))
							{
								float allt = st - RTEditor.SnapToBPM(st + kf);
								og = RTEditor.SnapToBPM(st + kf);
								ObjectEditor.inst.CurrentBeatmapObjectSelection.Data.events[timelineObject.Type][timelineObject.Index].eventTime = -allt;

								float num7 = posCalc(ObjectEditor.inst.CurrentBeatmapObjectSelection.Data.events[timelineObject.Type][timelineObject.Index].eventTime);
								if (num7 < 0f)
									num7 = 0f;

								((RectTransform)ObjEditor.inst.timelineKeyframes[timelineObject.Type][timelineObject.Index].transform).anchoredPosition = new Vector2(num7, 0f);
							}
						}
						else
						{
							ObjectEditor.inst.CurrentBeatmapObjectSelection.Data.events[timelineObject.Type][timelineObject.Index].eventTime = calc;

							float num7 = posCalc(ObjectEditor.inst.CurrentBeatmapObjectSelection.Data.events[timelineObject.Type][timelineObject.Index].eventTime);
							if (num7 < 0f)
								num7 = 0f;

							((RectTransform)ObjEditor.inst.timelineKeyframes[timelineObject.Type][timelineObject.Index].transform).anchoredPosition = new Vector2(num7, 0f);
						}
					}
				}

				ObjectEditor.inst.ResizeKeyframeTimeline(ObjectEditor.inst.CurrentBeatmapObjectSelection.Data);
				//ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection, false);

				foreach (var timelineObject in ObjectEditor.inst.SelectedBeatmapObjects)
					ObjectEditor.RenderTimelineObject(timelineObject);
			}
		}

		public static float og;

		[HarmonyPatch("CopyAllSelectedEvents")]
		[HarmonyPrefix]
		static bool CopyAllSelectedEventsPrefix()
        {
			ObjectEditor.inst.CopyAllSelectedEvents(ObjectEditor.inst.CurrentSelection);
			return false;
        }

		[HarmonyPatch("PasteKeyframes")]
		[HarmonyPrefix]
		static bool PasteKeyframesPrefix()
		{
			ObjectEditor.inst.PasteKeyframes(ObjectEditor.inst.CurrentSelection);
			return false;
		}

		[HarmonyPatch("OpenDialog")]
		[HarmonyPrefix]
		static bool OpenDialogPrefix()
        {
			ObjectEditor.inst.OpenDialog();
			return false;
        }

		[HarmonyPatch("SetCurrentKeyframe", new Type[] { typeof(int), typeof(bool) })]
		[HarmonyPrefix]
		static bool SetCurrentKeyframePrefix(int __0, bool __1 = false)
        {
			ObjectEditor.inst.SetCurrentKeyframe(ObjectEditor.inst.CurrentSelection, __0, __1);
			return false;
        }

		[HarmonyPatch("SetCurrentKeyframe", new Type[] { typeof(int), typeof(int), typeof(bool), typeof(bool) })]
		[HarmonyPrefix]
		static bool SetCurrentKeyframePrefix(int __0, int __1, bool __2 = false, bool __3 = false)
        {
			ObjectEditor.inst.SetCurrentKeyframe(ObjectEditor.inst.CurrentSelection, __0, __1);
			return false;
        }

		[HarmonyPatch("AddCurrentKeyframe")]
		[HarmonyPrefix]
		static bool AddCurrentKeyframePrefix(int __0, bool __1 = false)
        {
			ObjectEditor.inst.AddCurrentKeyframe(ObjectEditor.inst.CurrentSelection, __0, __1);
			return false;
        }

		[HarmonyPatch("ResizeKeyframeTimeline")]
		[HarmonyPrefix]
		static bool ResizeKeyframeTimelinePrefix()
		{
			ObjectEditor.inst.ResizeKeyframeTimeline(ObjectEditor.inst.CurrentSelection);
			return false;
		}

		[HarmonyPatch("SetAudioTime")]
		[HarmonyPrefix]
		static bool SetAudioTimePrefix(float __0)
		{
			if (Instance.changingTime)
			{
				Instance.newTime = __0;
				AudioManager.inst.SetMusicTime(Mathf.Clamp(__0, 0f, AudioManager.inst.CurrentAudioSource.clip.length));
			}
			return false;
		}

		[HarmonyPatch("GetKeyframeIcon")]
		[HarmonyPrefix]
		static bool GetKeyframeIconPrefix(ref Sprite __result, DataManager.LSAnimation __0, DataManager.LSAnimation __1)
        {
			__result = ObjectEditor.GetKeyframeIcon(__0, __1);
			return false;
        }

		[HarmonyPatch("CreateKeyframes")]
		[HarmonyPrefix]
		static bool CreateKeyframesPrefix()
		{
			ObjectEditor.inst.CreateKeyframes(ObjectEditor.inst.CurrentSelection);
            return false;
		}

		[HarmonyPatch("CreateKeyframeStartDragTrigger")]
		[HarmonyPrefix]
		static bool CreateKeyframeStartDragTriggerPrefix(ref EventTrigger.Entry __result, EventTriggerType __0, int __1, int __2)
        {
			__result = TriggerHelper.CreateKeyframeStartDragTrigger(ObjectEditor.inst.CurrentSelection, RTEditor.inst.timelineKeyframes.Find(x => x.Type == __1 && x.Index == __2));
			return false;
        }

		[HarmonyPatch("CreateKeyframeEndDragTrigger")]
		[HarmonyPrefix]
		static bool CreateKeyframeEndDragTriggerPrefix(ref EventTrigger.Entry __result, EventTriggerType __0, int __1, int __2)
        {
			__result = TriggerHelper.CreateKeyframeEndDragTrigger(ObjectEditor.inst.CurrentSelection, RTEditor.inst.timelineKeyframes.Find(x => x.Type == __1 && x.Index == __2));
			return false;
        }

		[HarmonyPatch("DeRenderSelectedObjects")]
		[HarmonyPrefix]
		static bool DeRenderSelectedObjectsPrefix()
        {
			ObjectEditor.inst.DeselectAllObjects();
			return false;
        }

		[HarmonyPatch("CopyObject")]
		[HarmonyPrefix]
		static bool CopyObjectPrefix()
		{
			var a = new List<BaseBeatmapObject>();
			foreach (var prefab in ObjectEditor.inst.SelectedBeatmapObjects)
				a.Add(prefab.Data);

			a = (from x in a
				 orderby x.StartTime
				 select x).ToList();

			var b = new List<BasePrefabObject>();
			foreach (var prefab in ObjectEditor.inst.SelectedPrefabObjects)
				b.Add(prefab.Data);

			b = (from x in b
				 orderby x.StartTime
				 select x).ToList();

			float start = 0f;
			//if (ConfigEntries.PasteOffset.Value)
			//{
			//	start = -AudioManager.inst.CurrentAudioSource.time + e[0].StartTime();
			//}

			Instance.beatmapObjCopy = new BasePrefab("copied prefab", 0, start, a, b);
			Instance.hasCopiedObject = true;

			JSONNode jsonnode = DataManager.inst.GeneratePrefabJSON(Instance.beatmapObjCopy);

			RTFile.WriteToFile(Application.persistentDataPath + "/copied_objects.lsp", jsonnode.ToString());
			return false;
		}

		[HarmonyPatch("PasteObject")]
		[HarmonyPrefix]
		static bool PasteObjectPrefix(float __0)
        {
			ObjectEditor.inst.PasteObject(__0);
			return false;
        }

		[HarmonyPatch("AddEvent")]
		[HarmonyPrefix]
		static bool AddEventPrefix(ref int __result, float __0, int __1, BaseEventKeyframe __2)
        {
			__result = ObjectEditor.inst.AddEvent(__0, __1, (EventKeyframe)__2);
			return false;
        }

		[HarmonyPatch("ToggleLockCurrentSelection")]
		[HarmonyPrefix]
		static bool ToggleLockCurrentSelectionPrefix()
		{
			foreach (var timelineObject in ObjectEditor.inst.SelectedBeatmapObjects)
            {
				timelineObject.Data.editorData.locked = !timelineObject.Data.editorData.locked;
				ObjectEditor.RenderTimelineObject(timelineObject);
            }
			
			foreach (var timelineObject in ObjectEditor.inst.SelectedPrefabObjects)
            {
				timelineObject.Data.editorData.locked = !timelineObject.Data.editorData.locked;
				ObjectEditor.RenderTimelineObject(timelineObject);
            }

			return false;
		}

		[HarmonyPatch("UpdateKeyframeOrder")]
		[HarmonyPrefix]
		static bool UpdateKeyframeOrderPrefix(bool _setCurrent = true)
		{
			ObjectEditor.inst.UpdateKeyframeOrder(ObjectEditor.inst.CurrentSelection, _setCurrent);
			return false;
		}

		[HarmonyPatch("SnapToBPM")]
		[HarmonyPrefix]
		static bool SnapToBPMPrefix(ref float __result, float __0)
		{
			__result = RTEditor.SnapToBPM(__0);
			return false;
		}

		[HarmonyPatch("posCalc")]
		[HarmonyPrefix]
		static bool posCalcPrefix(ref float __result, float __0)
        {
			__result = posCalc(__0);
			return false;
        }

		[HarmonyPatch("timeCalc")]
		[HarmonyPrefix]
		static bool timeCalcPrefix(ref float __result)
        {
			__result = timeCalc();
			return false;
        }

		[HarmonyPatch("RefreshKeyframeGUI")]
		[HarmonyPrefix]
		static bool RefreshKeyframeGUIPrefix()
        {
			ObjectEditor.inst.StartCoroutine(ObjectEditor.RefreshObjectGUI(ObjectEditor.inst.CurrentBeatmapObjectSelection.Data));
			return false;
        }

        #endregion

        public static float posCalc(float _time) => _time * 14f * Instance.Zoom + 5f;

		public static float timeCalc()
		{
			float num = Screen.width * ((1155f - Mathf.Abs(TimelineScroll.anchoredPosition.x) + 7f) / 1920f);
			float num2 = 1f / (Screen.width / 1920f);
			float num3 = Input.mousePosition.x;
			if (num3 < num)
				num3 = num;

			return (num3 - num) / Instance.Zoom / 14f * num2;
		}

	}
}
