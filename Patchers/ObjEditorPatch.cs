﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using BepInEx.Configuration;
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
using RTFunctions.Functions.Animation;
using RTFunctions.Functions.Animation.Keyframe;
using RTFunctions.Functions.Components;
using RTFunctions.Functions.Data;
using RTFunctions.Functions.IO;
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

		[HarmonyPatch("Awake")]
		[HarmonyPrefix]
		static bool AwakePrefix(ObjEditor __instance)
		{
			// og code
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

				__instance.timelineKeyframes.Add(new List<GameObject>());
				__instance.timelineKeyframes.Add(new List<GameObject>());
				__instance.timelineKeyframes.Add(new List<GameObject>());
				__instance.timelineKeyframes.Add(new List<GameObject>());

				var beginDragTrigger = new EventTrigger.Entry();
				beginDragTrigger.eventID = EventTriggerType.BeginDrag;
				beginDragTrigger.callback.AddListener(delegate (BaseEventData eventData)
				{
					var pointerEventData = (PointerEventData)eventData;
					__instance.SelectionBoxImage.gameObject.SetActive(true);
					__instance.DragStartPos = pointerEventData.position * EditorManager.inst.ScreenScaleInverse;
					__instance.SelectionRect = default(Rect);
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
					var pointerEventData = (PointerEventData)eventData;
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

			var objectView = ObjEditor.inst.ObjectView.transform;
			var dialog = ObjEditor.inst.ObjectView.transform.parent.parent.parent.parent.parent;
			var right = dialog.Find("data/right");

			// Add spacer
			var spacer = new GameObject("spacer");
			spacer.transform.SetParent(objectView);
			spacer.transform.SetSiblingIndex(15);

			var spRT = spacer.AddComponent<RectTransform>();
			var spHLG = spacer.AddComponent<HorizontalLayoutGroup>();

			spRT.sizeDelta = new Vector2(30f, 30f);
			spHLG.spacing = 8;

			var singleInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move/position/x");

			var todDropdown = objectView.Find("autokill/tod-dropdown");
			var hide = todDropdown.GetComponent<HideDropdownOptions>();
			hide.DisabledOptions[0] = false;
			hide.remove = true;
			var template = todDropdown.transform.Find("Template/Viewport/Content").gameObject;
			var vlg = template.AddComponent<VerticalLayoutGroup>();
			vlg.childControlHeight = false;
			vlg.childForceExpandHeight = false;

			var csf = template.AddComponent<ContentSizeFitter>();
			csf.verticalFit = ContentSizeFitter.FitMode.MinSize;

			objectView.Find("name/name").GetComponent<InputField>().characterLimit = 0;

			// Labels
			for (int j = 0; j < objectView.childCount; j++)
			{
				var label = objectView.GetChild(j);
				if (label.name == "label" || label.name == "collapselabel")
				{
					for (int k = 0; k < label.childCount; k++)
					{
						var labelText = label.GetChild(k).GetComponent<Text>();
						EditorThemeManager.AddLightText(labelText);
					}
                }
            }

            for (int i = 0; i < __instance.KeyframeDialogs.Count; i++)
            {
                var kfdialog = __instance.KeyframeDialogs[i].transform;

				for (int j = 0; j < kfdialog.childCount; j++)
				{
					var label = kfdialog.GetChild(j);
					if (label.name == "label")
					{
						for (int k = 0; k < label.childCount; k++)
						{
							var labelText = label.GetChild(k).GetComponent<Text>();
							EditorThemeManager.AddLightText(labelText);
						}
					}
				}
			}

            var labelToCopy = objectView.ChildList().First(x => x.name == "label").gameObject;

			// Depth
			{
				var depth = singleInput.Duplicate(spacer.transform, "depth");
				depth.transform.localScale = Vector3.one;
				depth.transform.Find("input").AsRT().sizeDelta = new Vector2(110f, 32f);

				Destroy(depth.GetComponent<EventInfo>());

				var depthif = depth.GetComponent<InputField>();
				depthif.onValueChanged.RemoveAllListeners();

				var sliderObject = objectView.Find("depth/depth").gameObject;

				Destroy(objectView.Find("depth/<").gameObject);
				Destroy(objectView.Find("depth/>").gameObject);

				sliderObject.transform.AsRT().sizeDelta = new Vector2(352f, 32f);
				objectView.Find("depth").AsRT().sizeDelta = new Vector2(261f, 32f);

				EditorThemeManager.AddInputField(depthif);
				var leftButton = depth.transform.Find(">").GetComponent<Button>();
				var rightButton = depth.transform.Find("<").GetComponent<Button>();
				Destroy(leftButton.GetComponent<Animator>());
				Destroy(rightButton.GetComponent<Animator>());
				leftButton.transition = Selectable.Transition.ColorTint;
				rightButton.transition = Selectable.Transition.ColorTint;

				EditorThemeManager.AddSelectable(leftButton, ThemeGroup.Function_2, false);
				EditorThemeManager.AddSelectable(rightButton, ThemeGroup.Function_2, false);

				var depthSlider = sliderObject.GetComponent<Slider>();
				var depthSliderImage = sliderObject.transform.Find("Image").GetComponent<Image>();
				depthSlider.colors = UIManager.SetColorBlock(depthSlider.colors, Color.white, new Color(0.9f, 0.9f, 0.9f), Color.white, Color.white, Color.white);
				EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Slider_2, depthSliderImage.gameObject, new List<Component>
				{
					depthSliderImage,
				}, true, 1, SpriteManager.RoundedSide.W));

				EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Slider_2_Handle, depthSlider.image.gameObject, new List<Component>
				{
					depthSlider.image,
				}, true, 1, SpriteManager.RoundedSide.W));
			}

			// Lock
			{
				var timeParent = objectView.Find("time");

				var locker = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/toggle/toggle").Duplicate(timeParent.transform, "lock", 0);
				locker.transform.localScale = Vector3.one;

				var timeLayout = timeParent.GetComponent<HorizontalLayoutGroup>();
				timeLayout.childControlWidth = false;
				timeLayout.childForceExpandWidth = false;

				locker.transform.AsRT().sizeDelta = new Vector2(32f, 32f);

				var time = timeParent.Find("time");
				time.AsRT().sizeDelta = new Vector2(151, 32f);
				var lockToggle = locker.GetComponent<Toggle>();

				((Image)lockToggle.graphic).sprite = ObjEditor.inst.timelineObjectPrefabLock.transform.Find("lock (1)").GetComponent<Image>().sprite;

				EditorThemeManager.AddToggle(lockToggle);

				timeParent.Find("<<").AsRT().sizeDelta = new Vector2(32f, 32f);
				timeParent.Find("<").AsRT().sizeDelta = new Vector2(16f, 32f);
				timeParent.Find("|").AsRT().sizeDelta = new Vector2(16f, 32f);
				timeParent.Find(">").AsRT().sizeDelta = new Vector2(16f, 32f);
				timeParent.Find(">>").AsRT().sizeDelta = new Vector2(32f, 32f);

				DestroyImmediate(timeParent.Find("<<").GetComponent<Animator>());
				var leftGreaterButton = timeParent.Find("<<").GetComponent<Button>();
				leftGreaterButton.transition = Selectable.Transition.ColorTint;
				DestroyImmediate(timeParent.Find("<").GetComponent<Animator>());
				var leftButton = timeParent.Find("<").GetComponent<Button>();
				leftButton.transition = Selectable.Transition.ColorTint;
				DestroyImmediate(timeParent.Find("|").GetComponent<Animator>());
				var middleButton = timeParent.Find("|").GetComponent<Button>();
				middleButton.transition = Selectable.Transition.ColorTint;
				DestroyImmediate(timeParent.Find(">").GetComponent<Animator>());
				var rightButton = timeParent.Find(">").GetComponent<Button>();
				rightButton.transition = Selectable.Transition.ColorTint;
				DestroyImmediate(timeParent.Find(">>").GetComponent<Animator>());
				var rightGreaterButton = timeParent.Find(">>").GetComponent<Button>();
				rightGreaterButton.transition = Selectable.Transition.ColorTint;

				EditorThemeManager.AddSelectable(leftGreaterButton, ThemeGroup.Function_2, false);
				EditorThemeManager.AddSelectable(leftButton, ThemeGroup.Function_2, false);
				EditorThemeManager.AddSelectable(middleButton, ThemeGroup.Function_2, false);
				EditorThemeManager.AddSelectable(rightButton, ThemeGroup.Function_2, false);
				EditorThemeManager.AddSelectable(rightGreaterButton, ThemeGroup.Function_2, false);
				EditorThemeManager.AddInputField(timeParent.Find("time").GetComponent<InputField>());
			}

			// Colors
			{
				var colorParent = __instance.KeyframeDialogs[3].transform.Find("color").transform;
				colorParent.GetComponent<GridLayoutGroup>().spacing = new Vector2(9.32f, 9.32f);

				for (int i = 1; i < 19; i++)
				{
					if (i >= 10)
						colorParent.Find("9").gameObject.Duplicate(colorParent, i.ToString());

					var toggle = colorParent.Find(i.ToString()).GetComponent<Toggle>();

					EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Null, toggle.gameObject, new List<Component>
					{
						toggle.image,
					}, true, 1, SpriteManager.RoundedSide.W));

					EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Background_1, toggle.graphic.gameObject, new List<Component>
					{
						toggle.graphic,
					}));
				}
			}

			// Origin X / Y
			{
				var contentOriginTF = objectView.transform.Find("origin").transform;

				contentOriginTF.Find("origin-x").gameObject.SetActive(false);
				contentOriginTF.Find("origin-y").gameObject.SetActive(false);

				var xo = singleInput.Duplicate(contentOriginTF.transform, "x");
				xo.transform.localScale = Vector3.one;
				xo.transform.Find("input").AsRT().sizeDelta = new Vector2(110f, 32f);

				Destroy(xo.GetComponent<EventInfo>());

				var xoif = xo.GetComponent<InputField>();
				xoif.onValueChanged.RemoveAllListeners();

				var yo = singleInput.Duplicate(contentOriginTF, "y");
				yo.transform.localScale = Vector3.one;
				yo.transform.Find("input").AsRT().sizeDelta = new Vector2(110f, 32f);

				Destroy(yo.GetComponent<EventInfo>());

				var yoif = yo.GetComponent<InputField>();
				yoif.onValueChanged.RemoveAllListeners();

				EditorThemeManager.AddInputField(xoif);
				var xLeftButton = xo.transform.Find(">").GetComponent<Button>();
				var xRightButton = xo.transform.Find("<").GetComponent<Button>();
				Destroy(xLeftButton.GetComponent<Animator>());
				Destroy(xRightButton.GetComponent<Animator>());
				xLeftButton.transition = Selectable.Transition.ColorTint;
				xRightButton.transition = Selectable.Transition.ColorTint;

				EditorThemeManager.AddSelectable(xLeftButton, ThemeGroup.Function_2, false);
				EditorThemeManager.AddSelectable(xRightButton, ThemeGroup.Function_2, false);

				EditorThemeManager.AddInputField(yoif);
				var yLeftButton = yo.transform.Find(">").GetComponent<Button>();
				var yRightButton = yo.transform.Find("<").GetComponent<Button>();
				Destroy(yLeftButton.GetComponent<Animator>());
				Destroy(yRightButton.GetComponent<Animator>());
				yLeftButton.transition = Selectable.Transition.ColorTint;
				yRightButton.transition = Selectable.Transition.ColorTint;

				EditorThemeManager.AddSelectable(yLeftButton, ThemeGroup.Function_2, false);
				EditorThemeManager.AddSelectable(yRightButton, ThemeGroup.Function_2, false);
			}

			// Opacity
			{
				var opacityLabel = __instance.KeyframeDialogs[3].transform.Find("label").gameObject.Duplicate(__instance.KeyframeDialogs[3].transform, "opacity_label");
				opacityLabel.transform.localScale = Vector3.one;
				var opacityLabelText = opacityLabel.transform.GetChild(0).GetComponent<Text>();
				opacityLabelText.text = "Opacity";

				EditorThemeManager.AddLightText(opacityLabelText);

				var opacity = Instantiate(__instance.KeyframeDialogs[2].transform.Find("rotation").gameObject);
				opacity.transform.SetParent(__instance.KeyframeDialogs[3].transform);
				opacity.transform.localScale = Vector3.one;
				opacity.name = "opacity";

				var collisionToggle = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/grain/colored").Duplicate(opacity.transform, "collision");

				var collisionToggleText = collisionToggle.transform.Find("Text").GetComponent<Text>();
				collisionToggleText.text = "Collide";
				opacity.transform.Find("x/input").AsRT().sizeDelta = new Vector2(136f, 32f);

				EditorThemeManager.AddToggle(collisionToggle.GetComponent<Toggle>(), text: collisionToggleText);
			}

			// Hue / Sat / Val
			{
				var opacityLabel = __instance.KeyframeDialogs[2].transform.Find("label").gameObject.Duplicate(__instance.KeyframeDialogs[3].transform);
				opacityLabel.transform.GetChild(0).GetComponent<Text>().text = "Hue";
				opacityLabel.name = "huesatval_label";

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
					{
						var inputField = opacity.transform.GetChild(i).GetComponent<InputField>();
						var swapper = opacity.transform.GetChild(i).gameObject.AddComponent<InputFieldSwapper>();
						swapper.inputField = inputField;

						inputField.characterValidation = InputField.CharacterValidation.None;
						inputField.contentType = InputField.ContentType.Standard;
						inputField.keyboardType = TouchScreenKeyboardType.Default;
					}

					var horizontal = opacity.transform.GetChild(i).GetComponent<HorizontalLayoutGroup>();
					var input = opacity.transform.GetChild(i).Find("input").AsRT();

					horizontal.childControlWidth = false;

					input.sizeDelta = new Vector2(60f, 32f);

					var layout = opacity.transform.GetChild(i).GetComponent<LayoutElement>();
					layout.minWidth = 109f;
				}

				//Triggers.AddTooltip(opacity.gameObject, "Set the hue value here.", "Shifts the hue levels of the objects' color.");
			}

			// Position Z
			{
				var positionBase = ObjEditor.inst.KeyframeDialogs[0].transform.Find("position");

				var posZ = positionBase.Find("x").gameObject.Duplicate(positionBase, "z");

				DestroyImmediate(positionBase.GetComponent<HorizontalLayoutGroup>());
				var grp = positionBase.gameObject.AddComponent<GridLayoutGroup>();

				DestroyImmediate(ObjEditor.inst.KeyframeDialogs[0].transform.Find("position/x/input").GetComponent<LayoutElement>());
				DestroyImmediate(ObjEditor.inst.KeyframeDialogs[0].transform.Find("position/y/input").GetComponent<LayoutElement>());
				DestroyImmediate(ObjEditor.inst.KeyframeDialogs[0].transform.Find("position/z/input").GetComponent<LayoutElement>());

				var xLayout = positionBase.Find("x/input").GetComponent<LayoutElement>();
				var yLayout = positionBase.Find("y/input").GetComponent<LayoutElement>();
				var zLayout = positionBase.Find("z/input").GetComponent<LayoutElement>();

				xLayout.preferredWidth = -1;
				yLayout.preferredWidth = -1;
				zLayout.preferredWidth = -1;

				var labels = ObjEditor.inst.KeyframeDialogs[0].transform.GetChild(8);
				var posZLabel = labels.GetChild(1).gameObject.Duplicate(labels, "text");
				posZLabel.GetComponent<Text>().text = "Position Z";

				EditorPlugin.AdjustPositionInputsChanged = delegate ()
				{
					if (!ObjectEditor.inst)
						return;

					bool adjusted = EditorConfig.Instance.AdjustPositionInputs.Value && RTEditor.ShowModdedUI;
					positionBase.AsRT().sizeDelta = new Vector2(553f, adjusted ? 32f : 64f);
					grp.cellSize = new Vector2(adjusted ? 122f : 183f, 40f);

					var minWidth = adjusted ? 65f : 125.3943f;
					xLayout.minWidth = minWidth;
					yLayout.minWidth = minWidth;
					zLayout.minWidth = minWidth;
					posZLabel.gameObject.SetActive(adjusted);
					positionBase.gameObject.SetActive(false);
					positionBase.gameObject.SetActive(true);

					posZ.gameObject.SetActive(RTEditor.ShowModdedUI);
				};

				bool adjusted = EditorConfig.Instance.AdjustPositionInputs.Value && RTEditor.ShowModdedUI;
				positionBase.AsRT().sizeDelta = new Vector2(553f, adjusted ? 32f : 64f);
				grp.cellSize = new Vector2(adjusted ? 122f : 183f, 40f);

				var minWidth = adjusted ? 65f : 125.3943f;
				xLayout.minWidth = minWidth;
				yLayout.minWidth = minWidth;
				zLayout.minWidth = minWidth;
				posZLabel.gameObject.SetActive(adjusted);

				posZ.gameObject.SetActive(RTEditor.ShowModdedUI);
			}

			// Layers
			{
				objectView.GetChild(objectView.Find("spacer") ? 17 : 16).GetChild(1).gameObject.SetActive(true);

				Destroy(objectView.Find("editor/layer").gameObject);

				var layers = Instantiate(objectView.Find("time/time").gameObject);

				layers.transform.SetParent(objectView.transform.Find("editor"));
				layers.name = "layers";
				layers.transform.SetSiblingIndex(0);
				var layersIF = layers.GetComponent<InputField>();

				layersIF.characterValidation = InputField.CharacterValidation.Integer;

				var edhlg = objectView.transform.Find("editor").GetComponent<HorizontalLayoutGroup>();
				edhlg.childControlWidth = false;
				edhlg.childForceExpandWidth = false;

				layers.transform.AsRT().sizeDelta = new Vector2(100f, 32f);
				objectView.Find("editor/bin").AsRT().sizeDelta = new Vector2(237f, 32f);

				layers.AddComponent<ContrastColors>().Init(layersIF.textComponent, layersIF.image);

				EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Null, layers, new List<Component>
				{
					layersIF.image,
				}, true, 1, SpriteManager.RoundedSide.W));

				var binSlider = objectView.Find("editor/bin").GetComponent<Slider>();
				var binSliderImage = binSlider.transform.Find("Image").GetComponent<Image>();
				binSlider.colors = UIManager.SetColorBlock(binSlider.colors, Color.white, new Color(0.9f, 0.9f, 0.9f), Color.white, Color.white, Color.white);
				EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Slider_2, binSliderImage.gameObject, new List<Component>
				{
					binSliderImage,
				}, true, 1, SpriteManager.RoundedSide.W));

				EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Slider_2_Handle, binSlider.image.gameObject, new List<Component>
				{
					binSlider.image,
				}, true, 1, SpriteManager.RoundedSide.W));
			}

			// Clear Parent
			{
				var close = GameObject.Find("Editor Systems/Editor GUI/sizer/main/Popups/Open File Popup/Panel/x");

				var parent = objectView.Find("parent");
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

				parent.transform.Find("parent").AsRT().sizeDelta = new Vector2(32f, 32f);
				parent.transform.Find("more").AsRT().sizeDelta = new Vector2(32f, 32f);
			}

			// ID & LDM
			{
				var id = objectView.GetChild(0).gameObject.Duplicate(objectView, "id", 0);
				Destroy(id.transform.GetChild(1).gameObject);

				((RectTransform)id.transform).sizeDelta = new Vector2(515, 32f);
				((RectTransform)id.transform.GetChild(0)).sizeDelta = new Vector2(226f, 32f);

				var text = id.transform.GetChild(0).GetComponent<Text>();
				text.fontSize = 18;
				text.text = "ID:";
				text.alignment = TextAnchor.MiddleLeft;
				text.horizontalOverflow = HorizontalWrapMode.Overflow;

				if (!id.GetComponent<Image>())
				{
					var image = id.AddComponent<Image>();
					image.color = new Color(1f, 1f, 1f, 0.07f);
				}

				var ldm = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/toggle").Duplicate(id.transform, "ldm");

				ldm.transform.Find("title").AsRT().sizeDelta = new Vector2(44f, 32f);
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
						var toggleLabel = di.GetChild(12).gameObject.Duplicate(parent, "relative-label");
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

			// Homing Buttons
			{
				var position = ObjEditor.inst.KeyframeDialogs[0].transform;
				var randomPosition = position.transform.Find("random");
				randomPosition.Find("interval-input/x").gameObject.SetActive(false);
				var homingStaticPosition = randomPosition.Find("none").gameObject.Duplicate(randomPosition, "homing-static", 4);

				if (RTFile.FileExists(RTFile.ApplicationDirectory + RTFunctions.FunctionsPlugin.BepInExAssetsPath + "editor_gui__s_homing.png"))
					homingStaticPosition.transform.Find("Image").GetComponent<Image>().sprite = SpriteManager.LoadSprite(RTFile.ApplicationDirectory + RTFunctions.FunctionsPlugin.BepInExAssetsPath + "editor_gui__s_homing.png");

				var homingDynamicPosition = randomPosition.Find("none").gameObject.Duplicate(randomPosition, "homing-dynamic", 5);

				if (RTFile.FileExists(RTFile.ApplicationDirectory + RTFunctions.FunctionsPlugin.BepInExAssetsPath + "editor_gui_d_homing.png"))
					homingDynamicPosition.transform.Find("Image").GetComponent<Image>().sprite = SpriteManager.LoadSprite(RTFile.ApplicationDirectory + RTFunctions.FunctionsPlugin.BepInExAssetsPath + "editor_gui_d_homing.png");

				var rotation = ObjEditor.inst.KeyframeDialogs[2].transform;
				var randomRotation = rotation.Find("random");
				randomRotation.Find("interval-input/x").gameObject.SetActive(false);
				var homingStaticRotation = randomRotation.Find("none").gameObject.Duplicate(randomRotation, "homing-static", 3);

				if (RTFile.FileExists(RTFile.ApplicationDirectory + RTFunctions.FunctionsPlugin.BepInExAssetsPath + "editor_gui__s_homing.png"))
					homingStaticRotation.transform.Find("Image").GetComponent<Image>().sprite = SpriteManager.LoadSprite(RTFile.ApplicationDirectory + RTFunctions.FunctionsPlugin.BepInExAssetsPath + "editor_gui__s_homing.png");

				var homingDynamicRotation = randomRotation.Find("none").gameObject.Duplicate(randomRotation, "homing-dynamic", 4);

				if (RTFile.FileExists(RTFile.ApplicationDirectory + RTFunctions.FunctionsPlugin.BepInExAssetsPath + "editor_gui_d_homing.png"))
					homingDynamicRotation.transform.Find("Image").GetComponent<Image>().sprite = SpriteManager.LoadSprite(RTFile.ApplicationDirectory + RTFunctions.FunctionsPlugin.BepInExAssetsPath + "editor_gui_d_homing.png");

				var rRotation = rotation.Find("r_rotation");
				var rRotationX = rRotation.Find("x");

				var rRotationY = rRotationX.gameObject.Duplicate(rRotation, "y");

				var rRotationLabel = rotation.Find("r_rotation_label");
				var l = rRotationLabel.GetChild(0);
				var max = l.gameObject.Duplicate(rRotationLabel, "text");

				Destroy(rRotation.GetComponent<EventTrigger>());

				var color = ObjEditor.inst.KeyframeDialogs[3].transform;
				var randomColorRangeLabel = position.Find("r_position_label").gameObject.Duplicate(color, "r_color_label");
				randomColorRangeLabel.transform.GetChild(0).GetComponent<Text>().text = "Minimum Range";
				randomColorRangeLabel.transform.GetChild(1).GetComponent<Text>().text = "Maximum Range";
				var randomColorRange = position.Find("r_position").gameObject.Duplicate(color, "r_color");

				var randomColorTargetLabel = position.GetChild(3).gameObject.Duplicate(color, "r_color_target_label");
				randomColorTargetLabel.transform.GetChild(0).GetComponent<Text>().text = "Color Target";

				var randomColorTarget = color.Find("color").gameObject.Duplicate(color, "r_color_target");
				randomColorTarget.transform.AsRT().sizeDelta = new Vector2(366f, 64f);

				var randomColorLabel = position.Find("r_label").gameObject.Duplicate(color, "r_label");
				var randomColor = randomPosition.gameObject.Duplicate(color, "random");

				Destroy(randomColor.transform.Find("normal").gameObject);
				Destroy(randomColor.transform.Find("toggle").gameObject);
				Destroy(randomColor.transform.Find("scale").gameObject);
				Destroy(randomColor.transform.Find("homing-static").gameObject);

				var rAxis = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/autokill/tod-dropdown")
					.Duplicate(position, "r_axis", 14);
				var rAxisDD = rAxis.GetComponent<Dropdown>();
				rAxisDD.options.Clear();
				rAxisDD.options = new List<Dropdown.OptionData>
				{
					new Dropdown.OptionData("Both"),
					new Dropdown.OptionData("X Only"),
					new Dropdown.OptionData("Y Only"),
				};

				EditorThemeManager.AddDropdown(rAxisDD);
			}

			// Object Tags
			{
				var label = objectView.ChildList().First(x => x.name == "label").gameObject.Duplicate(objectView, "tags_label");
				var index = objectView.Find("name").GetSiblingIndex() + 1;
				label.transform.SetSiblingIndex(index);

				Destroy(label.transform.GetChild(1).gameObject);
				label.transform.GetChild(0).GetComponent<Text>().text = "Tags";

				// Tags Scroll View/Viewport/Content
				var tagScrollView = new GameObject("Tags Scroll View");
				tagScrollView.transform.SetParent(objectView);
				tagScrollView.transform.SetSiblingIndex(index + 1);
				tagScrollView.transform.localScale = Vector3.one;

				var tagScrollViewRT = tagScrollView.AddComponent<RectTransform>();
				tagScrollViewRT.sizeDelta = new Vector2(522f, 40f);
				var scroll = tagScrollView.AddComponent<ScrollRect>();

				scroll.horizontal = true;
				scroll.vertical = false;

				var image = tagScrollView.AddComponent<Image>();
				image.color = new Color(1f, 1f, 1f, 0.01f);

				var mask = tagScrollView.AddComponent<Mask>();

				var tagViewport = new GameObject("Viewport");
				tagViewport.transform.SetParent(tagScrollViewRT);
				tagViewport.transform.localScale = Vector3.one;

				var tagViewPortRT = tagViewport.AddComponent<RectTransform>();
				tagViewPortRT.anchoredPosition = Vector2.zero;
				tagViewPortRT.anchorMax = Vector2.one;
				tagViewPortRT.anchorMin = Vector2.zero;
				tagViewPortRT.sizeDelta = Vector2.zero;

				var tagContent = new GameObject("Content");
				tagContent.transform.SetParent(tagViewPortRT);
				tagContent.transform.localScale = Vector3.one;

				var tagContentRT = tagContent.AddComponent<RectTransform>();

				var tagContentGLG = tagContent.AddComponent<GridLayoutGroup>();
				tagContentGLG.cellSize = new Vector2(168f, 32f);
				tagContentGLG.constraint = GridLayoutGroup.Constraint.FixedRowCount;
				tagContentGLG.constraintCount = 1;
				tagContentGLG.childAlignment = TextAnchor.MiddleLeft;
				tagContentGLG.spacing = new Vector2(8f, 0f);

				var tagContentCSF = tagContent.AddComponent<ContentSizeFitter>();
				tagContentCSF.horizontalFit = ContentSizeFitter.FitMode.MinSize;
				tagContentCSF.verticalFit = ContentSizeFitter.FitMode.MinSize;

				scroll.viewport = tagViewPortRT;
				scroll.content = tagContentRT;
			}

			// Render Type
			{
				var label = objectView.ChildList().First(x => x.name == "label").gameObject.Duplicate(objectView, "rendertype_label");
				var index = objectView.Find("depth").GetSiblingIndex() + 1;
				label.transform.SetSiblingIndex(index);

				Destroy(label.transform.GetChild(1).gameObject);
				label.transform.GetChild(0).GetComponent<Text>().text = "Render Type";

				var renderType = objectView.Find("autokill/tod-dropdown").gameObject
					.Duplicate(objectView, "rendertype", index + 1);
				var renderTypeDD = renderType.GetComponent<Dropdown>();
				renderTypeDD.options.Clear();
				renderTypeDD.options = new List<Dropdown.OptionData>
				{
					new Dropdown.OptionData("Foreground"),
					new Dropdown.OptionData("Background"),
				};

				EditorThemeManager.AddDropdown(renderTypeDD);
			}

			DestroyImmediate(ObjEditor.inst.KeyframeDialogs[2].transform.GetChild(1).gameObject);
			DestroyImmediate(ObjEditor.inst.KeyframeDialogs[3].transform.GetChild(1).gameObject);

			var multiKF = ObjEditor.inst.KeyframeDialogs[4];
			multiKF.transform.AsRT().anchorMax = new Vector2(0f, 1f);
			multiKF.transform.AsRT().anchorMin = new Vector2(0f, 1f);

			// Shift Dialogs
			{
				ObjEditor.inst.KeyframeDialogs[0].transform.GetChild(2).gameObject.SetActive(false);
				ObjEditor.inst.KeyframeDialogs[0].transform.GetChild(7).gameObject.SetActive(false);
				ObjEditor.inst.KeyframeDialogs[1].transform.GetChild(2).gameObject.SetActive(false);
				ObjEditor.inst.KeyframeDialogs[1].transform.GetChild(7).gameObject.SetActive(false);
				ObjEditor.inst.KeyframeDialogs[2].transform.GetChild(2).gameObject.SetActive(false);
				ObjEditor.inst.KeyframeDialogs[2].transform.GetChild(7).gameObject.SetActive(false);
				ObjEditor.inst.KeyframeDialogs[3].transform.GetChild(2).gameObject.SetActive(false);
				ObjEditor.inst.KeyframeDialogs[3].transform.GetChild(7).gameObject.SetActive(false);

				DestroyImmediate(GameObject.Find("Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right").GetComponent<VerticalLayoutGroup>());

				var di = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/grain").transform;
				var toggle = di.GetChild(13).gameObject.Duplicate(ObjEditor.inst.KeyframeDialogs[3].transform, "shift", 16);
				var text = toggle.transform.GetChild(1).GetComponent<Text>();
				text.text = "Shift Dialog Down";
				toggle.GetComponentAndPerformAction(delegate (Toggle shift)
				{
					shift.onValueChanged.ClearAll();
					shift.isOn = false;
					shift.onValueChanged.AddListener(delegate (bool _val)
					{
						ObjectEditor.inst.colorShifted = _val;
						text.text = _val ? "Shift Dialog Up" : "Shift Dialog Down";
						var animation = new AnimationManager.Animation("shift color UI");
						animation.floatAnimations = new List<AnimationManager.Animation.AnimationObject<float>>
						{
							new AnimationManager.Animation.AnimationObject<float>(new List<IKeyframe<float>>
							{
								new FloatKeyframe(0f, _val ? 0f : 115f, Ease.Linear),
								new FloatKeyframe(0.3f, _val ? 115f : 0f, Ease.CircOut),
								new FloatKeyframe(0.32f, _val ? 115f : 0f, Ease.Linear),
							}, delegate (float x)
							{
								ObjEditor.inst.KeyframeDialogs[3].transform.AsRT().anchoredPosition = new Vector2(0f, x);
							}),
						};

						animation.onComplete = delegate ()
						{
							ObjEditor.inst.KeyframeDialogs[3].transform.AsRT().anchoredPosition = new Vector2(0f, _val ? 115f : 0f);
							AnimationManager.inst.RemoveID(animation.id);
						};

						AnimationManager.inst.Play(animation);
					});

					EditorThemeManager.AddSelectable(shift, ThemeGroup.Function_2);
					EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Function_2_Text, text.gameObject, new List<Component>
					{
						text,
					}));
				});
			}

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
					var parent = objectView.Find("parent_more").GetChild(i + 1);

					if (parent.Find("<<"))
						Destroy(parent.Find("<<").gameObject);

					if (parent.Find("<"))
						Destroy(parent.Find("<").gameObject);

					if (parent.Find(">"))
						Destroy(parent.Find(">").gameObject);

					if (parent.Find(">>"))
						Destroy(parent.Find(">>").gameObject);

					var additive = parent.GetChild(2).gameObject.Duplicate(parent, $"{array[i]}_add");
					var parallax = parent.GetChild(3).gameObject.Duplicate(parent, $"{array[i]}_parallax");

					if (parent.Find("text"))
					{
						parent.Find("text").GetComponent<Text>().fontSize = 19;
					}
				}

			}

			// Make Shape list scrollable, for any more shapes I decide to add.
			{
				var shape = objectView.Find("shape");
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

			// Timeline Object adjustments
			{
				var gameObject = ObjEditor.inst.timelineObjectPrefab.Duplicate(null, ObjEditor.inst.timelineObjectPrefab.name);
				var icons = gameObject.transform.Find("icons");

				if (!icons.gameObject.GetComponent<HorizontalLayoutGroup>())
				{
					var timelineObjectStorage = gameObject.AddComponent<TimelineObjectStorage>();

					var @lock = ObjEditor.inst.timelineObjectPrefabLock.Duplicate(icons);
					@lock.name = "lock";
					((RectTransform)@lock.transform).anchoredPosition = Vector3.zero;

					var dots = ObjEditor.inst.timelineObjectPrefabDots.Duplicate(icons);
					dots.name = "dots";
					((RectTransform)dots.transform).anchoredPosition = Vector3.zero;

					var hlg = icons.gameObject.AddComponent<HorizontalLayoutGroup>();
					hlg.childControlWidth = false;
					hlg.childForceExpandWidth = false;
					hlg.spacing = -4f;
					hlg.childAlignment = TextAnchor.UpperRight;

					((RectTransform)@lock.transform).sizeDelta = new Vector2(20f, 20f);

					((RectTransform)dots.transform).sizeDelta = new Vector2(32f, 20f);

					var b = new GameObject("type");
					b.transform.SetParent(icons);
					b.transform.localScale = Vector3.one;

					var bRT = b.AddComponent<RectTransform>();
					bRT.sizeDelta = new Vector2(20f, 20f);

					var bImage = b.AddComponent<Image>();
					bImage.color = new Color(0f, 0f, 0f, 0.45f);

					var icon = new GameObject("type");
					icon.transform.SetParent(b.transform);
					icon.transform.localScale = Vector3.one;

					var iconRT = icon.AddComponent<RectTransform>();
					iconRT.anchoredPosition = Vector2.zero;
					iconRT.sizeDelta = new Vector2(20f, 20f);

					var iconImage = icon.AddComponent<Image>();

					var hoverUI = gameObject.AddComponent<HoverUI>();
					hoverUI.animatePos = false;
					hoverUI.animateSca = true;

					timelineObjectStorage.hoverUI = hoverUI;
					timelineObjectStorage.image = gameObject.GetComponent<Image>();
					timelineObjectStorage.eventTrigger = gameObject.GetComponent<EventTrigger>() ?? gameObject.AddComponent<EventTrigger>();
					timelineObjectStorage.text = gameObject.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
				}

				ObjEditor.inst.timelineObjectPrefab = gameObject;
			}

			// Store Image Shape
			{
				if (objectView.Find("shapesettings/7"))
				{
					var button = GameObject.Find("TimelineBar/GameObject/event");
					var copy = button.Duplicate(objectView.Find("shapesettings/7"), "set", 5);
					var text = copy.transform.GetChild(0).GetComponent<Text>();
					text.text = "Set Data";
					copy.transform.GetComponent<Image>().color = new Color(0.3922f, 0.7098f, 0.9647f, 1f);
					((RectTransform)copy.transform).sizeDelta = new Vector2(70f, 32f);

					copy.GetComponent<LayoutElement>().minWidth = 130f;
				}
			}

            // Parent Desync
            {
				var parentMore = objectView.Find("parent_more");
				var ignoreGameObject = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/grain/colored"));
				ignoreGameObject.transform.SetParent(parentMore);
				ignoreGameObject.transform.SetSiblingIndex(1);
				ignoreGameObject.transform.localScale = Vector3.one;
				ignoreGameObject.name = "spawn_once";
				ignoreGameObject.transform.Find("Text").GetComponent<Text>().text = "Parent Desync";

				parentMore.AsRT().sizeDelta = new Vector2(351f, 152f);
			}

            // Assign Prefab
            {
				var collapseLabel = __instance.ObjectView.transform.Find("collapselabel");
                var applyPrefab = __instance.ObjectView.transform.Find("applyprefab");
				var siblingIndex = applyPrefab.GetSiblingIndex();
				var applyPrefabText = applyPrefab.transform.GetChild(0).GetComponent<Text>();

				var applyPrefabButton = applyPrefab.GetComponent<Button>();
				Destroy(applyPrefab.GetComponent<Animator>());
				applyPrefabButton.transition = Selectable.Transition.ColorTint;
				EditorThemeManager.AddSelectable(applyPrefabButton, ThemeGroup.Function_2);
				EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Function_2_Text, applyPrefabText.gameObject, new List<Component>
				{
					applyPrefabText,
				}));

				var assignPrefabLabel = collapseLabel.gameObject.Duplicate(__instance.ObjectView.transform, "assignlabel", siblingIndex + 1);
				assignPrefabLabel.transform.GetChild(0).GetComponent<Text>().text = "Assign Object to a Prefab";
                var assignPrefab = applyPrefab.gameObject.Duplicate(__instance.ObjectView.transform, "assign", siblingIndex + 2);
				var assignPrefabText = assignPrefab.transform.GetChild(0).GetComponent<Text>();
				assignPrefabText.text = "Assign";
				var assignPrefabButton = assignPrefab.GetComponent<Button>();
				Destroy(assignPrefab.GetComponent<Animator>());
				assignPrefabButton.transition = Selectable.Transition.ColorTint;
				EditorThemeManager.AddSelectable(assignPrefabButton, ThemeGroup.Function_2);
				EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Function_2_Text, assignPrefabText.gameObject, new List<Component>
				{
					assignPrefabText,
				}));

				assignPrefabButton.onClick.ClearAll();
				assignPrefabButton.onClick.AddListener(delegate ()
				{
					RTEditor.inst.selectingMultiple = false;
					RTEditor.inst.prefabPickerEnabled = true;
				});

				var removePrefab = applyPrefab.gameObject.Duplicate(__instance.ObjectView.transform, "remove", siblingIndex + 3);
				var removePrefabText = removePrefab.transform.GetChild(0).GetComponent<Text>();
				removePrefabText.text = "Remove";
				var removePrefabButton = removePrefab.GetComponent<Button>();
				Destroy(removePrefab.GetComponent<Animator>());
				removePrefabButton.transition = Selectable.Transition.ColorTint;
				EditorThemeManager.AddSelectable(removePrefabButton, ThemeGroup.Function_2);
				EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Function_2_Text, removePrefabText.gameObject, new List<Component>
				{
					removePrefabText,
				}));

				removePrefabButton.onClick.ClearAll();
				removePrefabButton.onClick.AddListener(delegate ()
				{
					var beatmapObject = ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>();
					beatmapObject.prefabID = "";
					beatmapObject.prefabInstanceID = "";
					ObjectEditor.inst.RenderTimelineObject(ObjectEditor.inst.CurrentSelection);
					ObjectEditor.inst.OpenDialog(beatmapObject);
				});
			}

			// Editor Themes
			{
				EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Background_1, dialog.gameObject, new List<Component>
				{
					dialog.GetComponent<Image>(),
				}));

				EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Background_3, right.gameObject, new List<Component>
				{
					right.GetComponent<Image>(),
				}));

				EditorThemeManager.AddInputField(objectView.Find("name/name").GetComponent<InputField>());
				EditorThemeManager.AddDropdown(objectView.Find("name/object-type").GetComponent<Dropdown>());
				EditorThemeManager.AddDropdown(todDropdown.GetComponent<Dropdown>());

				var autokill = objectView.Find("autokill");
				EditorThemeManager.AddInputField(autokill.Find("tod-value").GetComponent<InputField>());

				var setAutokillButton = autokill.Find("|").GetComponent<Button>();
				Destroy(setAutokillButton.GetComponent<Animator>());
				setAutokillButton.transition = Selectable.Transition.ColorTint;
				EditorThemeManager.AddSelectable(setAutokillButton, ThemeGroup.Function_2, false);

				var collapse = autokill.Find("collapse").GetComponent<Toggle>();

				EditorThemeManager.AddToggle(collapse, ThemeGroup.Background_1);

				for (int i = 0; i < collapse.transform.Find("dots").childCount; i++)
                {
					var dot = collapse.transform.Find("dots").GetChild(i).GetComponent<Image>();
					EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Dark_Text, dot.gameObject, new List<Component>
					{
						dot,
					}));
                }

				var parentButton = objectView.Find("parent/text").GetComponent<Button>();
				EditorThemeManager.AddSelectable(parentButton, ThemeGroup.Function_2);
				EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Function_2_Text, parentButton.transform.GetChild(0).gameObject, new List<Component>
				{
					parentButton.transform.GetChild(0).GetComponent<Text>(),
				}));

				var moreButton = objectView.Find("parent/more").GetComponent<Button>();
				Destroy(moreButton.GetComponent<Animator>());
				moreButton.transition = Selectable.Transition.ColorTint;
                EditorThemeManager.AddSelectable(moreButton, ThemeGroup.Function_2, false);

				EditorThemeManager.AddInputField(objectView.transform.Find("shapesettings/5").GetComponent<InputField>());
            }

            try
			{
				var move = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move").transform;
				var multiKeyframeEditor = multiKF.transform;

				multiKeyframeEditor.GetChild(1).gameObject.SetActive(false);

				//multiKeyframeEditor.Find("Text").AsRT().sizeDelta = new Vector2(765f, 120f);

				// Label
				{
					var label = labelToCopy.Duplicate(multiKeyframeEditor, "time_label");

					Destroy(label.transform.GetChild(1).gameObject);
					label.transform.GetChild(0).GetComponent<Text>().text = "Time";
				}

				var timeBase = new GameObject("time");
				timeBase.transform.SetParent(multiKeyframeEditor);
				timeBase.transform.localScale = Vector3.one;
				var timeBaseRT = timeBase.AddComponent<RectTransform>();
				timeBaseRT.sizeDelta = new Vector2(765f, 38f);

				var time = move.Find("time").gameObject.Duplicate(timeBaseRT, "time");
				time.transform.AsRT().anchoredPosition = new Vector2(182f, -19f);
				time.GetComponent<HorizontalLayoutGroup>().spacing = 5f;

				var barButton = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/time").transform.GetChild(4).gameObject;

				var multiTimeB = barButton
				.Duplicate(time.transform, "|", 3);
				multiTimeB.GetComponent<Image>().sprite = barButton.GetComponent<Image>().sprite;

				// Label
				{
					var label = labelToCopy.Duplicate(multiKeyframeEditor, "curve_label");

					Destroy(label.transform.GetChild(1).gameObject);
					label.transform.GetChild(0).GetComponent<Text>().text = "Ease Type";
				}

				var curveBase = new GameObject("curves");
				curveBase.transform.SetParent(multiKeyframeEditor);
				curveBase.transform.localScale = Vector3.one;
				var curveBaseRT = curveBase.AddComponent<RectTransform>();
				curveBaseRT.sizeDelta = new Vector2(765f, 38f);

				var curves = move.Find("curves").gameObject.Duplicate(curveBaseRT, "curves");
				curves.transform.AsRT().anchoredPosition = new Vector2(182f, -19f);

				// Label
				{
					var label = labelToCopy.Duplicate(multiKeyframeEditor, "value index_label");

					Destroy(label.transform.GetChild(1).gameObject);
					label.transform.GetChild(0).GetComponent<Text>().text = "Value Index / Value";
				}

				var valueBase = new GameObject("value base");
				valueBase.transform.SetParent(multiKeyframeEditor);
				valueBase.transform.localScale = Vector3.one;

				var valueBaseRT = valueBase.AddComponent<RectTransform>();
				valueBaseRT.sizeDelta = new Vector2(364f, 32f);

				var valueBaseHLG = valueBase.AddComponent<HorizontalLayoutGroup>();
				valueBaseHLG.childControlHeight = false;
				valueBaseHLG.childControlWidth = false;
				valueBaseHLG.childForceExpandHeight = false;
				valueBaseHLG.childForceExpandWidth = false;

				var valueIndex = singleInput.Duplicate(valueBaseRT, "value index");
				valueIndex.transform.Find("input").AsRT().sizeDelta = new Vector2(110f, 32f);

				Destroy(valueIndex.GetComponent<EventInfo>());

				var value = singleInput.Duplicate(valueBaseRT, "value");
				value.transform.Find("input").AsRT().sizeDelta = new Vector2(110f, 32f);

				Destroy(value.GetComponent<EventInfo>());

				var multiValueB = barButton
				.Duplicate(value.transform, "|", 2);
				multiValueB.GetComponent<Image>().sprite = barButton.GetComponent<Image>().sprite;

				// Label
				{
					var label = labelToCopy.Duplicate(multiKeyframeEditor, "snap_label");

					Destroy(label.transform.GetChild(1).gameObject);
					label.transform.GetChild(0).GetComponent<Text>().text = "Force Snap Time to BPM";
				}

				var eventButton = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject/event");

				var snapToBPMObject = eventButton.Duplicate(multiKeyframeEditor, "snap bpm");
				snapToBPMObject.transform.localScale = Vector3.one;

				((RectTransform)snapToBPMObject.transform).sizeDelta = new Vector2(404f, 32f);

				snapToBPMObject.transform.GetChild(0).GetComponent<Text>().text = "Snap";
				snapToBPMObject.GetComponent<Image>().color = new Color(0.3922f, 0.7098f, 0.9647f, 1f);

				var snapToBPM = snapToBPMObject.GetComponent<Button>();
				snapToBPM.onClick.ClearAll();
				snapToBPM.onClick.AddListener(delegate ()
				{
					var beatmapObject = ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>();
					foreach (var timelineObject in ObjectEditor.inst.CurrentSelection.InternalSelections.Where(x => x.selected))
					{
						if (timelineObject.Index != 0)
							timelineObject.Time = RTEditor.SnapToBPM(timelineObject.Time);

						float st = beatmapObject.StartTime;

						st = -(st - RTEditor.SnapToBPM(st + timelineObject.Time));

						float timePosition = posCalc(st);

						((RectTransform)timelineObject.GameObject.transform).anchoredPosition = new Vector2(timePosition, 0f);

						Updater.UpdateProcessor(beatmapObject, "Keyframes");

						ObjectEditor.inst.RenderKeyframe(beatmapObject, timelineObject);
					}
				});

				// Label
				{
					var label = labelToCopy.Duplicate(multiKeyframeEditor, "paste_label");

					Destroy(label.transform.GetChild(1).gameObject);
					label.transform.GetChild(0).GetComponent<Text>().text = "All Types";
				}

				var pasteAllObject = eventButton.Duplicate(multiKeyframeEditor, "paste");
				pasteAllObject.transform.localScale = Vector3.one;

				((RectTransform)pasteAllObject.transform).sizeDelta = new Vector2(404f, 32f);

				pasteAllObject.transform.GetChild(0).GetComponent<Text>().text = "Paste";

				var pasteAll = pasteAllObject.GetComponent<Button>();
				pasteAll.onClick.ClearAll();
				pasteAll.onClick.AddListener(delegate ()
				{
					var beatmapObject = ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>();
					var list = ObjectEditor.inst.CurrentSelection.InternalSelections.Where(x => x.selected);

					foreach (var timelineObject in list)
					{
						var kf = timelineObject.GetData<EventKeyframe>();
						switch (timelineObject.Type)
						{
							case 0:
								if (ObjectEditor.inst.CopiedPositionData != null)
								{
									kf.curveType = ObjectEditor.inst.CopiedPositionData.curveType;
									kf.eventValues = ObjectEditor.inst.CopiedPositionData.eventValues.Copy();
									kf.eventRandomValues = ObjectEditor.inst.CopiedPositionData.eventRandomValues.Copy();
									kf.random = ObjectEditor.inst.CopiedPositionData.random;
									kf.relative = ObjectEditor.inst.CopiedPositionData.relative;
								}
								break;
							case 1:
								if (ObjectEditor.inst.CopiedScaleData != null)
								{
									kf.curveType = ObjectEditor.inst.CopiedScaleData.curveType;
									kf.eventValues = ObjectEditor.inst.CopiedScaleData.eventValues.Copy();
									kf.eventRandomValues = ObjectEditor.inst.CopiedScaleData.eventRandomValues.Copy();
									kf.random = ObjectEditor.inst.CopiedScaleData.random;
									kf.relative = ObjectEditor.inst.CopiedScaleData.relative;
								}
								break;
							case 2:
								if (ObjectEditor.inst.CopiedRotationData != null)
								{
									kf.curveType = ObjectEditor.inst.CopiedRotationData.curveType;
									kf.eventValues = ObjectEditor.inst.CopiedRotationData.eventValues.Copy();
									kf.eventRandomValues = ObjectEditor.inst.CopiedRotationData.eventRandomValues.Copy();
									kf.random = ObjectEditor.inst.CopiedRotationData.random;
									kf.relative = ObjectEditor.inst.CopiedRotationData.relative;
								}
								break;
							case 3:
								if (ObjectEditor.inst.CopiedColorData != null)
								{
									kf.curveType = ObjectEditor.inst.CopiedColorData.curveType;
									kf.eventValues = ObjectEditor.inst.CopiedColorData.eventValues.Copy();
									kf.eventRandomValues = ObjectEditor.inst.CopiedColorData.eventRandomValues.Copy();
									kf.random = ObjectEditor.inst.CopiedColorData.random;
									kf.relative = ObjectEditor.inst.CopiedColorData.relative;
								}
								break;
						}
					}

					ObjectEditor.inst.RenderKeyframes(beatmapObject);
					ObjectEditor.inst.RenderObjectKeyframesDialog(beatmapObject);
					Updater.UpdateProcessor(beatmapObject, "Keyframes");
					EditorManager.inst.DisplayNotification("Pasted keyframe data to selected keyframes!", 2f, EditorManager.NotificationType.Success);
				});

				// Label
				{
					var label = labelToCopy.Duplicate(multiKeyframeEditor, "paste_label");

					Destroy(label.transform.GetChild(1).gameObject);
					label.transform.GetChild(0).GetComponent<Text>().text = "Position / Scale";
				}

				var pastePosScaObject = new GameObject("paste pos sca base");
				pastePosScaObject.transform.SetParent(multiKeyframeEditor);
				pastePosScaObject.transform.localScale = Vector3.one;

				var pastePosScaRT = pastePosScaObject.AddComponent<RectTransform>();
                pastePosScaRT.sizeDelta = new Vector2(364f, 32f);

				var pastePosScaHLG = pastePosScaObject.AddComponent<HorizontalLayoutGroup>();
				pastePosScaHLG.childControlHeight = false;
				pastePosScaHLG.childControlWidth = false;
				pastePosScaHLG.childForceExpandHeight = false;
				pastePosScaHLG.childForceExpandWidth = false;
				pastePosScaHLG.spacing = 8f;

				var pastePosObject = eventButton.Duplicate(pastePosScaRT, "paste");
				pastePosObject.transform.localScale = Vector3.one;

				((RectTransform)pastePosObject.transform).sizeDelta = new Vector2(180f, 32f);

				pastePosObject.transform.GetChild(0).GetComponent<Text>().text = "Paste Pos";

				var pastePos = pastePosObject.GetComponent<Button>();
				pastePos.onClick.ClearAll();
				pastePos.onClick.AddListener(delegate ()
				{
					var beatmapObject = ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>();
					var list = ObjectEditor.inst.CurrentSelection.InternalSelections.Where(x => x.selected);

					foreach (var timelineObject in list)
					{
						if (timelineObject.Type != 0)
							continue;

						var kf = timelineObject.GetData<EventKeyframe>();

						if (ObjectEditor.inst.CopiedPositionData != null)
						{
							kf.curveType = ObjectEditor.inst.CopiedPositionData.curveType;
							kf.eventValues = ObjectEditor.inst.CopiedPositionData.eventValues.Copy();
							kf.eventRandomValues = ObjectEditor.inst.CopiedPositionData.eventRandomValues.Copy();
							kf.random = ObjectEditor.inst.CopiedPositionData.random;
							kf.relative = ObjectEditor.inst.CopiedPositionData.relative;
						}
					}

					ObjectEditor.inst.RenderKeyframes(beatmapObject);
					ObjectEditor.inst.RenderObjectKeyframesDialog(beatmapObject);
					Updater.UpdateProcessor(beatmapObject, "Keyframes");
					EditorManager.inst.DisplayNotification("Pasted position keyframe data to selected position keyframes!", 3f, EditorManager.NotificationType.Success);
				});

				var pasteScaObject = eventButton.Duplicate(pastePosScaRT, "paste");
				pasteScaObject.transform.localScale = Vector3.one;

				((RectTransform)pasteScaObject.transform).sizeDelta = new Vector2(180f, 32f);

				pasteScaObject.transform.GetChild(0).GetComponent<Text>().text = "Paste Scale";

				var pasteSca = pasteScaObject.GetComponent<Button>();
				pasteSca.onClick.ClearAll();
				pasteSca.onClick.AddListener(delegate ()
				{
					var beatmapObject = ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>();
					var list = ObjectEditor.inst.CurrentSelection.InternalSelections.Where(x => x.selected);

					foreach (var timelineObject in list)
					{
						if (timelineObject.Type != 1)
							continue;

						var kf = timelineObject.GetData<EventKeyframe>();

						if (ObjectEditor.inst.CopiedScaleData != null)
						{
							kf.curveType = ObjectEditor.inst.CopiedScaleData.curveType;
							kf.eventValues = ObjectEditor.inst.CopiedScaleData.eventValues.Copy();
							kf.eventRandomValues = ObjectEditor.inst.CopiedScaleData.eventRandomValues.Copy();
							kf.random = ObjectEditor.inst.CopiedScaleData.random;
							kf.relative = ObjectEditor.inst.CopiedScaleData.relative;
						}
					}

					ObjectEditor.inst.RenderKeyframes(beatmapObject);
					ObjectEditor.inst.RenderObjectKeyframesDialog(beatmapObject);
					Updater.UpdateProcessor(beatmapObject, "Keyframes");
					EditorManager.inst.DisplayNotification("Pasted scale keyframe data to selected scale keyframes!", 3f, EditorManager.NotificationType.Success);
				});

				// Label
				{
					var label = labelToCopy.Duplicate(multiKeyframeEditor, "paste_label");

					Destroy(label.transform.GetChild(1).gameObject);
					label.transform.GetChild(0).GetComponent<Text>().text = "Rotation / Color";
				}

				var pasteRotColObject = new GameObject("paste rot col base");
				pasteRotColObject.transform.SetParent(multiKeyframeEditor);
				pasteRotColObject.transform.localScale = Vector3.one;

				var pasteRotColObjectRT = pasteRotColObject.AddComponent<RectTransform>();
				pasteRotColObjectRT.sizeDelta = new Vector2(364f, 32f);

				var pasteRotColObjectHLG = pasteRotColObject.AddComponent<HorizontalLayoutGroup>();
				pasteRotColObjectHLG.childControlHeight = false;
				pasteRotColObjectHLG.childControlWidth = false;
				pasteRotColObjectHLG.childForceExpandHeight = false;
				pasteRotColObjectHLG.childForceExpandWidth = false;
				pasteRotColObjectHLG.spacing = 8f;

				var pasteRotObject = eventButton.Duplicate(pasteRotColObjectRT, "paste");
				pasteRotObject.transform.localScale = Vector3.one;

				((RectTransform)pasteRotObject.transform).sizeDelta = new Vector2(180f, 32f);

				pasteRotObject.transform.GetChild(0).GetComponent<Text>().text = "Paste Rot";

				var pasteRot = pasteRotObject.GetComponent<Button>();
				pasteRot.onClick.ClearAll();
				pasteRot.onClick.AddListener(delegate ()
				{
					var beatmapObject = ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>();
					var list = ObjectEditor.inst.CurrentSelection.InternalSelections.Where(x => x.selected);

					foreach (var timelineObject in list)
					{
						if (timelineObject.Type != 3)
							continue;

						var kf = timelineObject.GetData<EventKeyframe>();

						if (ObjectEditor.inst.CopiedRotationData != null)
						{
							kf.curveType = ObjectEditor.inst.CopiedRotationData.curveType;
							kf.eventValues = ObjectEditor.inst.CopiedRotationData.eventValues.Copy();
							kf.eventRandomValues = ObjectEditor.inst.CopiedRotationData.eventRandomValues.Copy();
							kf.random = ObjectEditor.inst.CopiedRotationData.random;
							kf.relative = ObjectEditor.inst.CopiedRotationData.relative;
						}
					}

					ObjectEditor.inst.RenderKeyframes(beatmapObject);
					ObjectEditor.inst.RenderObjectKeyframesDialog(beatmapObject);
					Updater.UpdateProcessor(beatmapObject, "Keyframes");
					EditorManager.inst.DisplayNotification("Pasted rotation keyframe data to selected rotation keyframes!", 3f, EditorManager.NotificationType.Success);
				});

				var pasteColObject = eventButton.Duplicate(pasteRotColObjectRT, "paste");
				pasteColObject.transform.localScale = Vector3.one;

				((RectTransform)pasteColObject.transform).sizeDelta = new Vector2(180f, 32f);

				pasteColObject.transform.GetChild(0).GetComponent<Text>().text = "Paste Col";

				var pasteCol = pasteColObject.GetComponent<Button>();
				pasteCol.onClick.ClearAll();
				pasteCol.onClick.AddListener(delegate ()
				{
					var beatmapObject = ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>();
					var list = ObjectEditor.inst.CurrentSelection.InternalSelections.Where(x => x.selected);

					foreach (var timelineObject in list)
					{
						if (timelineObject.Type != 4)
							continue;

						var kf = timelineObject.GetData<EventKeyframe>();

						if (ObjectEditor.inst.CopiedColorData != null)
						{
							kf.curveType = ObjectEditor.inst.CopiedColorData.curveType;
							kf.eventValues = ObjectEditor.inst.CopiedColorData.eventValues.Copy();
							kf.eventRandomValues = ObjectEditor.inst.CopiedColorData.eventRandomValues.Copy();
							kf.random = ObjectEditor.inst.CopiedColorData.random;
							kf.relative = ObjectEditor.inst.CopiedColorData.relative;
						}
					}

					ObjectEditor.inst.RenderKeyframes(beatmapObject);
					ObjectEditor.inst.RenderObjectKeyframesDialog(beatmapObject);
					Updater.UpdateProcessor(beatmapObject, "Keyframes");
					EditorManager.inst.DisplayNotification("Pasted color keyframe data to selected color keyframes!", 3f, EditorManager.NotificationType.Success);
				});
			}
			catch (Exception ex)
			{
				Debug.LogError($"{__instance.className}{ex}");
			}

			__instance.SelectedColor = EditorConfig.Instance.ObjectSelectionColor.Value;
			__instance.ObjectLengthOffset = EditorConfig.Instance.KeyframeEndLengthOffset.Value;

			ObjectEditor.Init(__instance);

			ObjectEditor.inst.shapeButtonPrefab = __instance.ObjectView.transform.Find("shape/1").gameObject.Duplicate(__instance.transform);

			return false;
		}

		[HarmonyPatch("Start")]
		[HarmonyPrefix]
		static bool StartPrefix()
		{
			Instance.colorButtons.Clear();
			for (int i = 1; i <= 18; i++)
			{
				Instance.colorButtons.Add(Instance.KeyframeDialogs[3].transform.Find("color/" + i).GetComponent<Toggle>());
			}

			if (RTFile.FileExists(Application.persistentDataPath + "/copied_objects.lsp"))
			{
				var jn = JSON.Parse(FileManager.inst.LoadJSONFileRaw(Application.persistentDataPath + "/copied_objects.lsp"));

				var objects = new List<BaseBeatmapObject>();
				for (int i = 0; i < jn["objects"].Count; ++i)
					objects.Add(BaseBeatmapObject.ParseGameObject(jn["objects"][i]));

				var prefabObjects = new List<BasePrefabObject>();
				for (int i = 0; i < jn["prefab_objects"].Count; ++i)
					prefabObjects.Add(DataManager.inst.gameData.ParsePrefabObject(jn["prefab_objects"][i]));

				Instance.beatmapObjCopy = new BasePrefab(jn["name"], jn["type"].AsInt, jn["offset"].AsFloat, objects, prefabObjects);
				Instance.hasCopiedObject = true;
			}

			Instance.zoomBounds = EditorConfig.Instance.KeyframeZoomBounds.Value;
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
				RTEditor.inst.dragOffset = -1f;
				RTEditor.inst.dragBinOffset = -100;
			}

			if (__instance.beatmapObjectsDrag)
			{
				if (InputDataManager.inst.editorActions.MultiSelect.IsPressed)
				{
					int binOffset = 14 - Mathf.RoundToInt((float)((Input.mousePosition.y - 25) * EditorManager.inst.ScreenScaleInverse / 20)) + __instance.mouseOffsetYForDrag;

					bool hasChanged = false;

					foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
					{
						int binCalc = Mathf.Clamp(binOffset + timelineObject.binOffset, 0, 14);

						if (!timelineObject.Locked)
						{
							if (timelineObject.Bin != binCalc)
                            {
								hasChanged = true;
							}

							timelineObject.Bin = binCalc;
							ObjectEditor.inst.RenderTimelineObjectPosition(timelineObject);
							if (timelineObject.IsBeatmapObject && ObjectEditor.inst.SelectedObjects.Count == 1)
							{
								ObjectEditor.inst.RenderBin(timelineObject.GetData<BeatmapObject>());
							}
						}
					}

					if (RTEditor.inst.dragBinOffset != binOffset && !ObjectEditor.inst.SelectedObjects.All(x => x.Locked))
					{
						if (hasChanged && RTEditor.DraggingPlaysSound)
							SoundManager.inst.PlaySound("UpDown", 0.4f, 0.6f);

						RTEditor.inst.dragBinOffset = binOffset;
					}
				}
				else
				{
					float timeOffset = Mathf.Round(Mathf.Clamp(EditorManager.inst.GetTimelineTime() + __instance.mouseOffsetXForDrag,
						0f, AudioManager.inst.CurrentAudioSource.clip.length) * 1000f) / 1000f;

					if (RTEditor.inst.dragOffset != timeOffset && !ObjectEditor.inst.SelectedObjects.All(x => x.Locked))
					{
						if (RTEditor.DraggingPlaysSound && (SettingEditor.inst.SnapActive || !RTEditor.DraggingPlaysSoundBPM))
							SoundManager.inst.PlaySound("LeftRight", SettingEditor.inst.SnapActive ? 0.6f : 0.1f, 0.7f);

						RTEditor.inst.dragOffset = timeOffset;
					}

					foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
					{
						float timeCalc = Mathf.Clamp(timeOffset + timelineObject.timeOffset, 0f, AudioManager.inst.CurrentAudioSource.clip.length);

						if (!timelineObject.Locked)
							timelineObject.Time = timeCalc;

						ObjectEditor.inst.RenderTimelineObjectPosition(timelineObject);

						if (timelineObject.IsBeatmapObject)
						{
							var beatmapObject = timelineObject.GetData<BeatmapObject>();
							Updater.UpdateProcessor(beatmapObject, "StartTime");

							if (ObjectEditor.inst.SelectedObjects.Count == 1)
							{
								ObjectEditor.inst.RenderStartTime(beatmapObject);
								ObjectEditor.inst.ResizeKeyframeTimeline(beatmapObject);
							}
						}

						if (timelineObject.IsPrefabObject)
						{
							var prefabObject = timelineObject.GetData<PrefabObject>();
							PrefabEditorManager.inst.RenderPrefabObjectDialog(prefabObject, PrefabEditor.inst);
							Updater.UpdatePrefab(prefabObject, "Start Time");
						}
					}
				}
			}

			if (__instance.timelineKeyframesDrag && ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
			{
				var beatmapObject = ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>();

				var snap = EditorConfig.Instance.BPMSnapsKeyframes.Value;

				foreach (var timelineObject in ObjectEditor.inst.CurrentSelection.InternalSelections.Where(x => x.selected))
				{
					if (timelineObject.Index != 0)
					{
						float timeOffset = timeCalc() + timelineObject.timeOffset + __instance.mouseOffsetXForKeyframeDrag;

						float calc = Mathf.Clamp(Mathf.Round(Mathf.Clamp(timeOffset, 0f, AudioManager.inst.CurrentAudioSource.clip.length) * 1000f) / 1000f, 0f, beatmapObject.GetObjectLifeLength(__instance.ObjectLengthOffset));

						float st = beatmapObject.StartTime;

						st = SettingEditor.inst.SnapActive && snap && !Input.GetKey(KeyCode.LeftAlt) ? -(st - RTEditor.SnapToBPM(st + calc)) : calc;

						beatmapObject.events[timelineObject.Type][timelineObject.Index].eventTime = st;

						if (RTEditor.inst.dragOffset != timeOffset)
						{
							if (RTEditor.DraggingPlaysSound && (SettingEditor.inst.SnapActive && snap || !RTEditor.DraggingPlaysSoundBPM))
								SoundManager.inst.PlaySound("LeftRight", SettingEditor.inst.SnapActive && snap ? 0.6f : 0.1f, 0.8f);

							RTEditor.inst.dragOffset = timeOffset;
						}

						float timePosition = posCalc(st);

						((RectTransform)timelineObject.GameObject.transform).anchoredPosition = new Vector2(timePosition, 0f);

						Updater.UpdateProcessor(beatmapObject, "Keyframes");

						ObjectEditor.inst.RenderKeyframe(beatmapObject, timelineObject);
					}
				}

				ObjectEditor.inst.RenderObjectKeyframesDialog(beatmapObject);

				ObjectEditor.inst.ResizeKeyframeTimeline(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>());

				foreach (var timelineObject in ObjectEditor.inst.SelectedBeatmapObjects)
					ObjectEditor.inst.RenderTimelineObject(timelineObject);
			}
			return false;
		}

		[HarmonyPatch("SetMainTimelineZoom")]
		[HarmonyPrefix]
		static bool SetMainTimelineZoom(float __0, bool __1 = true)
		{
			var beatmapObject = ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>();
			if (__1)
			{
				ObjectEditor.inst.ResizeKeyframeTimeline(beatmapObject);
				ObjectEditor.inst.RenderKeyframes(beatmapObject);
			}
			float f = ObjEditor.inst.objTimelineSlider.value;
			if (AudioManager.inst.CurrentAudioSource.clip != null)
            {
				float time = -beatmapObject.StartTime + AudioManager.inst.CurrentAudioSource.time;
				float objectLifeLength = beatmapObject.GetObjectLifeLength(ObjEditor.inst.ObjectLengthOffset);

				f = time / objectLifeLength;
			}

			Instance.StartCoroutine(UpdateTimelineScrollRect(0f, f));

			return false;
		}

		public static IEnumerator UpdateTimelineScrollRect(float _delay, float _val)
		{
			yield return new WaitForSeconds(_delay);
			if (ObjectEditor.inst.timelinePosScrollbar)
				ObjectEditor.inst.timelinePosScrollbar.value = _val;

			yield break;
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
			__result = RTEditor.GetKeyframeIcon(__0, __1);
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
			//if (ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
			//	__result = ObjectEditor.inst.AddEvent(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>(), __0, __1, (EventKeyframe)__2, true);
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
