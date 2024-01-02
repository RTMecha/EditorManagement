using System;
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
using RTFunctions.Functions.Optimization;
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

		#region Patches

		[HarmonyPatch("SetMainTimelineZoom")]
		[HarmonyPrefix]
		static bool SetMainTimelineZoom(float __0, bool __1 = true)
		{
			if (__1)
			{
				ObjectEditor.inst.ResizeKeyframeTimeline(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>());
				ObjectEditor.inst.RenderKeyframes(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>());
			}
			float f = ObjEditor.inst.objTimelineSlider.value;
			if (AudioManager.inst.CurrentAudioSource.clip != null)
				f = AudioManager.inst.CurrentAudioSource.time / AudioManager.inst.CurrentAudioSource.clip.length;

			Instance.StartCoroutine(Instance.UpdateTimelineScrollRect(0f, f));
			return false;
		}

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
		static bool RenderTimelineObjectsPrefix()
        {
			ObjectEditor.inst.RenderTimelineObjects();
			return false;
        }
		
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
				var positionBase = ObjEditor.inst.KeyframeDialogs[0].transform.Find("position").gameObject;

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
				var close = GameObject.Find("Editor Systems/Editor GUI/sizer/main/Popups/Open File Popup/Panel/x");

				var parent = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/parent");
				var hlg = parent.GetComponent<HorizontalLayoutGroup>();
				hlg.childControlWidth = false;
				hlg.spacing = 4f;

				((RectTransform)parent.transform.Find("text")).sizeDelta = new Vector2(201f, 32f);

				var resetParent = close.Duplicate(parent.transform, "clear parent", 1);

                var resetParentButton = resetParent.GetComponent<Button>();

                resetParentButton.onClick.ClearAll();
                resetParentButton.onClick.AddListener(delegate ()
                {
					var bm = ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>();

					bm.parent = "";

					ObjectEditor.inst.StartCoroutine(ObjectEditor.RefreshObjectGUI(bm));

					Updater.UpdateProcessor(bm);
                });

                var parentPicker = close.Duplicate(parent.transform, "parent picker", 2);

				var parentPickerButton = parentPicker.GetComponent<Button>();

                parentPickerButton.onClick.ClearAll();
                parentPickerButton.onClick.AddListener(delegate ()
                {
                    RTEditor.inst.parentPickerEnabled = true;
                });

                var cb = parentPickerButton.colors;
				cb.normalColor = new Color(0.0569f, 0.4827f, 0.9718f, 1f);
				parentPickerButton.colors = cb;

				if (parentPicker.transform.childCount >= 0
					&& RTExtensions.TryFind("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/left/theme/theme/viewport/content/player0/preview/dropper",
					out GameObject dropper)
					&& dropper.TryGetComponent(out Image dropperImage))
					parentPicker.transform.GetChild(0).GetComponent<Image>().sprite = dropperImage.sprite;

				parent.transform.Find("parent").GetComponent<RectTransform>().sizeDelta = new Vector2(32f, 32f);
				parent.transform.Find("more").GetComponent<RectTransform>().sizeDelta = new Vector2(32f, 32f);
			}

			// ID & LDM
			{
				var id = ObjEditor.inst.ObjectView.transform.GetChild(0).gameObject.Duplicate(ObjEditor.inst.ObjectView.transform, "id", 0);
				Destroy(id.transform.GetChild(1).gameObject);

				((RectTransform)id.transform).sizeDelta = new Vector2(515, 32f);
				((RectTransform)id.transform.GetChild(0)).sizeDelta = new Vector2(188f, 32f);

				var text = id.transform.GetChild(0).GetComponent<Text>();
				text.text = "ID:";
				text.alignment = TextAnchor.MiddleLeft;
				text.horizontalOverflow = HorizontalWrapMode.Overflow;

				if (!id.GetComponent<Image>())
				{
					var image = id.AddComponent<Image>();
					image.color = new Color(1f, 1f, 1f, 0.07f);
				}

				var ldm = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/toggle").Duplicate(id.transform, "ldm");

				ldm.transform.Find("title").GetComponent<Text>().text = "LDM";
			}

			// Relative / Copy / Paste
			{
				var button = GameObject.Find("TimelineBar/GameObject/event");
				for (int i = 0; i < 4; i++)
				{
					var parent = ObjEditor.inst.KeyframeDialogs[i].transform;
					if (i != 3)
					{
						var di = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/grain").transform;
						var toggleLabel = di.GetChild(12).gameObject.Duplicate(parent, "label");
						toggleLabel.transform.GetChild(0).GetComponent<Text>().text = "Value Additive";
						var toggle = di.GetChild(13).gameObject.Duplicate(parent, "relative");
						toggle.transform.GetChild(1).GetComponent<Text>().text = "Relative";
					}

					var edit = parent.Find("edit");
					DestroyImmediate(edit.Find("spacer").gameObject);

					var copy = button.Duplicate(edit, "copy", 5);
					copy.transform.GetChild(0).GetComponent<Text>().text = "Copy";
					copy.transform.GetComponent<Image>().color = new Color(0.24f, 0.6792f, 1f);
					((RectTransform)copy.transform).sizeDelta = new Vector2(70f, 32f);
					var paste = button.Duplicate(edit, "paste", 6);
					paste.transform.GetChild(0).GetComponent<Text>().text = "Paste";
					((RectTransform)paste.transform).sizeDelta = new Vector2(70f, 32f);
				}
			}

			Destroy(ObjEditor.inst.KeyframeDialogs[2].transform.GetChild(1).gameObject);
			Destroy(ObjEditor.inst.KeyframeDialogs[3].transform.GetChild(1).gameObject);

            // Parent Settings
            {
				var array = new string[]
				{
					"pos",
					"sca",
					"rot",
				};
				for (int i = 0; i < 3; i++)
				{
					var parent = ObjEditor.inst.ObjectView.transform.Find("parent_more").GetChild(i + 1);

                    if (parent.Find("<<"))
                        Destroy(parent.Find("<<").gameObject);

                    if (parent.Find("<"))
                        Destroy(parent.Find("<").gameObject);

                    if (parent.Find(">"))
                        Destroy(parent.Find(">").gameObject);

                    if (parent.Find(">>"))
                        Destroy(parent.Find(">>").gameObject);

                    var additive = parent.GetChild(2).gameObject.Duplicate(parent, $"{array}_add");
					var parallax = parent.GetChild(3).gameObject.Duplicate(parent, $"{array}_parallax");
				}

			}

			// Make Shape list scrollable, for any more shapes I decide to add.
			{
				var shape = ObjEditor.inst.ObjectView.transform.Find("shape");
				var rect = (RectTransform)shape;
				var scroll = shape.gameObject.AddComponent<ScrollRect>();
				shape.gameObject.AddComponent<Mask>();
				var image = shape.gameObject.AddComponent<Image>();

				scroll.horizontal = true;
				scroll.vertical = false;
				scroll.content = rect;
				scroll.viewport = rect;
				image.color = new Color(1f, 1f, 1f, 0.01f);
			}

			ObjectEditor.Init(__instance);

			ObjectEditor.inst.shapeButtonPrefab = ObjEditor.inst.ObjectView.transform.Find("shape/1").gameObject.Duplicate(null);

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

			__instance.zoomBounds = RTEditor.GetEditorProperty("Keyframe Zoom Bounds").GetConfigEntry<Vector2>().Value;
			return false;
		}

		[HarmonyPatch("Update")]
		[HarmonyPrefix]
		static bool UpdatePrefix(ObjEditor __instance)
        {
			if (!__instance.changingTime && ObjectEditor.inst.CurrentSelection && ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
			{
				// Sets new audio time using the Object Keyframe timeline cursor.
				__instance.newTime = Mathf.Clamp(EditorManager.inst.CurrentAudioPos,
					ObjectEditor.inst.CurrentSelection.Time,
					ObjectEditor.inst.CurrentSelection.Time + ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>().GetObjectLifeLength(__instance.ObjectLengthOffset));
				__instance.objTimelineSlider.value = __instance.newTime;
			}

			if (Input.GetMouseButtonUp(0))
			{
				__instance.beatmapObjectsDrag = false;
				__instance.timelineKeyframesDrag = false;
			}

			if (__instance.beatmapObjectsDrag)
            {
				if (InputDataManager.inst.editorActions.MultiSelect.IsPressed)
				{
					int binOffset = 14 - Mathf.RoundToInt((float)((Input.mousePosition.y - 25) * EditorManager.inst.ScreenScaleInverse / 20)) + __instance.mouseOffsetYForDrag;

					foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                    {
						int binCalc = Mathf.Clamp(binOffset + timelineObject.binOffset, 0, 14);

						if (!timelineObject.Locked)
							timelineObject.Bin = binCalc;

						ObjectEditor.inst.RenderTimelineObject(timelineObject);
					}
				}
				else
                {
					float timeOffset = Mathf.Round(Mathf.Clamp(EditorManager.inst.GetTimelineTime() + __instance.mouseOffsetXForDrag,
						0f, AudioManager.inst.CurrentAudioSource.clip.length) * 1000f) / 1000f;

					foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                    {
						float timeCalc = Mathf.Clamp(timeOffset + timelineObject.timeOffset, 0f, AudioManager.inst.CurrentAudioSource.clip.length);

						if (!timelineObject.Locked)
							timelineObject.Time = timeCalc;

						ObjectEditor.inst.RenderTimelineObject(timelineObject);

						if (timelineObject.IsBeatmapObject)
						{
							Updater.UpdateProcessor(timelineObject.GetData<BeatmapObject>(), "StartTime");
							ObjectEditor.inst.RenderStartTime(timelineObject.GetData<BeatmapObject>());
						}
					}
				}
            }

            if (__instance.timelineKeyframesDrag && ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
			{
				var beatmapObject = ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>();

				foreach (var timelineObject in ObjectEditor.inst.CurrentSelection.InternalSelections.Where(x => x.selected))
				{
					if (timelineObject.Index != 0)
					{
						float timeOffset = timeCalc() + timelineObject.timeOffset + __instance.mouseOffsetXForKeyframeDrag;
						//timeOffset = Mathf.Clamp(timeOffset, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
						//timeOffset = Mathf.Round(timeOffset * 1000f) / 1000f;

						//float calc = Mathf.Clamp(timeOffset, 0f, beatmapObject.GetObjectLifeLength(__instance.ObjectLengthOffset));

						float calc = Mathf.Clamp(Mathf.Round(Mathf.Clamp(timeOffset, 0f, AudioManager.inst.CurrentAudioSource.clip.length) * 1000f) / 1000f, 0f, beatmapObject.GetObjectLifeLength(__instance.ObjectLengthOffset));

						float st = beatmapObject.StartTime;

						beatmapObject.events[timelineObject.Type][timelineObject.Index].eventTime =
							SettingEditor.inst.SnapActive && RTEditor.BPMSnapKeyframes ? -(st - RTEditor.SnapToBPM(st + calc)) : calc;

						float timePosition = posCalc(calc);

						((RectTransform)timelineObject.GameObject.transform).anchoredPosition = new Vector2(timePosition, 0f);

						Updater.UpdateProcessor(beatmapObject, "Keyframes");

						ObjectEditor.inst.RenderKeyframe(beatmapObject, timelineObject);

						ObjectEditor.inst.RenderKeyframeDialog(beatmapObject);
					}
				}

				ObjectEditor.inst.ResizeKeyframeTimeline(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>());

				foreach (var timelineObject in ObjectEditor.inst.SelectedBeatmapObjects)
					ObjectEditor.inst.RenderTimelineObject(timelineObject);
			}
			return false;
		}

		[HarmonyPatch("CopyAllSelectedEvents")]
		[HarmonyPrefix]
		static bool CopyAllSelectedEventsPrefix()
        {
			if (ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
				ObjectEditor.inst.CopyAllSelectedEvents(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>());
			return false;
        }

		[HarmonyPatch("PasteKeyframes")]
		[HarmonyPrefix]
		static bool PasteKeyframesPrefix()
		{
			if (ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
				ObjectEditor.inst.PasteKeyframes(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>());
			return false;
		}

		[HarmonyPatch("OpenDialog")]
		[HarmonyPrefix]
		static bool OpenDialogPrefix()
        {
			ObjectEditor.inst.OpenDialog(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>());
			return false;
        }

		[HarmonyPatch("SetCurrentKeyframe", new Type[] { typeof(int), typeof(bool) })]
		[HarmonyPrefix]
		static bool SetCurrentKeyframePrefix(int __0, bool __1 = false)
		{
			if (ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
				ObjectEditor.inst.SetCurrentKeyframe(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>(), __0, __1);
			return false;
        }

		[HarmonyPatch("SetCurrentKeyframe", new Type[] { typeof(int), typeof(int), typeof(bool), typeof(bool) })]
		[HarmonyPrefix]
		static bool SetCurrentKeyframePrefix(int __0, int __1, bool __2 = false, bool __3 = false)
		{
			if (ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
				ObjectEditor.inst.SetCurrentKeyframe(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>(), __0, __1);
			return false;
        }

		[HarmonyPatch("AddCurrentKeyframe")]
		[HarmonyPrefix]
		static bool AddCurrentKeyframePrefix(int __0, bool __1 = false)
		{
			if (ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
				ObjectEditor.inst.AddCurrentKeyframe(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>(), __0, __1);
			return false;
        }

		[HarmonyPatch("ResizeKeyframeTimeline")]
		[HarmonyPrefix]
		static bool ResizeKeyframeTimelinePrefix()
		{
			if (ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
				ObjectEditor.inst.ResizeKeyframeTimeline(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>());
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
			if (ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
				ObjectEditor.inst.CreateKeyframes(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>());
            return false;
		}

		[HarmonyPatch("CreateKeyframeStartDragTrigger")]
		[HarmonyPrefix]
		static bool CreateKeyframeStartDragTriggerPrefix(ref EventTrigger.Entry __result, EventTriggerType __0, int __1, int __2)
        {
			__result = TriggerHelper.CreateKeyframeStartDragTrigger(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>(), RTEditor.inst.timelineKeyframes.Find(x => x.Type == __1 && x.Index == __2));
			return false;
        }

		[HarmonyPatch("CreateKeyframeEndDragTrigger")]
		[HarmonyPrefix]
		static bool CreateKeyframeEndDragTriggerPrefix(ref EventTrigger.Entry __result, EventTriggerType __0, int __1, int __2)
        {
			__result = TriggerHelper.CreateKeyframeEndDragTrigger(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>(), RTEditor.inst.timelineKeyframes.Find(x => x.Type == __1 && x.Index == __2));
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
			var a = new List<TimelineObject>();
			foreach (var prefab in ObjectEditor.inst.SelectedObjects)
				a.Add(prefab);

			a = (from x in a
				 orderby x.Time
				 select x).ToList();

			float start = 0f;
			//if (ConfigEntries.PasteOffset.Value)
			//{
			//	start = -AudioManager.inst.CurrentAudioSource.time + e[0].StartTime();
			//}

			var copy = new Prefab("copied prefab", 0, start,
				a.Where(x => x.IsBeatmapObject).Select(x => x.GetData<BeatmapObject>()).ToList(),
				a.Where(x => x.IsPrefabObject).Select(x => x.GetData<PrefabObject>()).ToList());

			copy.description = "Take me wherever you go!";
			Instance.beatmapObjCopy = copy;
			Instance.hasCopiedObject = true;

			RTFile.WriteToFile(Application.persistentDataPath + "/copied_objects.lsp", copy.ToJSON().ToString());
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
			if (ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
				__result = ObjectEditor.inst.AddEvent(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>(), __0, __1, (EventKeyframe)__2, true);
			return false;
        }

		[HarmonyPatch("ToggleLockCurrentSelection")]
		[HarmonyPrefix]
		static bool ToggleLockCurrentSelectionPrefix()
		{
			foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
            {
				if (timelineObject.IsBeatmapObject)
					timelineObject.GetData<BeatmapObject>().editorData.locked = !timelineObject.GetData<BeatmapObject>().editorData.locked;
				if (timelineObject.IsPrefabObject)
					timelineObject.GetData<PrefabObject>().editorData.locked = !timelineObject.GetData<PrefabObject>().editorData.locked;

				ObjectEditor.inst.RenderTimelineObject(timelineObject);
            }
			
			return false;
		}

		[HarmonyPatch("UpdateKeyframeOrder")]
		[HarmonyPrefix]
		static bool UpdateKeyframeOrderPrefix(bool _setCurrent = true)
		{
			if (ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
				ObjectEditor.inst.UpdateKeyframeOrder(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>());
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
			if (ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
				ObjectEditor.inst.StartCoroutine(ObjectEditor.RefreshObjectGUI(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>()));
			return false;
        }

        #endregion

        public static float posCalc(float _time) => _time * 14f * Instance.zoomVal + 5f;

		public static float timeCalc()
		{
			float num = Screen.width * ((1155f - Mathf.Abs(((RectTransform)Instance.timelineScroll.transform).anchoredPosition.x) + 7f) / 1920f);
			float screenScale = 1f / (Screen.width / 1920f);
			float mouseX = Input.mousePosition.x < num ? num : Input.mousePosition.x;

			return (mouseX - num) / Instance.Zoom / 14f * screenScale;
		}

		[HarmonyPatch("CreateNewNormalObject")]
        [HarmonyPrefix]
        static bool CreateNewNormalObjectPrefix(bool __0)
        {
			ObjectEditor.inst.CreateNewNormalObject(__0);
			return false;
        }

		[HarmonyPatch("CreateNewCircleObject")]
        [HarmonyPrefix]
        static bool CreateNewCircleObjectPrefix(bool __0)
        {
			ObjectEditor.inst.CreateNewCircleObject(__0);
			return false;
        }

		[HarmonyPatch("CreateNewTriangleObject")]
        [HarmonyPrefix]
        static bool CreateNewTriangleObjectPrefix(bool __0)
        {
			ObjectEditor.inst.CreateNewTriangleObject(__0);
			return false;
        }

		[HarmonyPatch("CreateNewTextObject")]
        [HarmonyPrefix]
        static bool CreateNewTextObjectPrefix(bool __0)
        {
			ObjectEditor.inst.CreateNewTextObject(__0);
			return false;
        }

		[HarmonyPatch("CreateNewHexagonObject")]
        [HarmonyPrefix]
        static bool CreateNewHexagonObjectPrefix(bool __0)
        {
			ObjectEditor.inst.CreateNewHexagonObject(__0);
			return false;
        }

		[HarmonyPatch("CreateNewHelperObject")]
        [HarmonyPrefix]
        static bool CreateNewHelperObjectPrefix(bool __0)
        {
			ObjectEditor.inst.CreateNewHelperObject(__0);
			return false;
        }

		[HarmonyPatch("CreateNewDecorationObject")]
        [HarmonyPrefix]
        static bool CreateNewDecorationObjectPrefix(bool __0)
        {
			ObjectEditor.inst.CreateNewDecorationObject(__0);
			return false;
        }

		[HarmonyPatch("CreateNewEmptyObject")]
        [HarmonyPrefix]
        static bool CreateNewEmptyObjectPrefix(bool __0)
        {
			ObjectEditor.inst.CreateNewEmptyObject(__0);
			return false;
        }

		[HarmonyPatch("CreateNewPersistentObject")]
        [HarmonyPrefix]
        static bool CreateNewPersistentObjectPrefix(bool __0)
        {
			ObjectEditor.inst.CreateNewNoAutokillObject(__0);
			return false;
        }
	}
}
