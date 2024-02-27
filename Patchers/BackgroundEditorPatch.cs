using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using LSFunctions;

using RTFunctions.Functions;
using RTFunctions.Functions.Data;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Optimization;
using RTFunctions.Patchers;

using EditorManagement.Functions;
using EditorManagement.Functions.Editors;
using EditorManagement.Functions.Helpers;

using BeatmapObject = DataManager.GameData.BeatmapObject;
using EventKeyframe = DataManager.GameData.EventKeyframe;
using Prefab = DataManager.GameData.Prefab;
using PrefabObject = DataManager.GameData.PrefabObject;
using BaseBackgroundObject = DataManager.GameData.BackgroundObject;

using ObjectType = DataManager.GameData.BeatmapObject.ObjectType;
using AutoKillType = DataManager.GameData.BeatmapObject.AutoKillType;

using ObjectSelection = ObjEditor.ObjectSelection;
using ObjectKeyframeSelection = ObjEditor.KeyframeSelection;
using EventKeyframeSelection = EventEditor.KeyframeSelection;

namespace EditorManagement.Patchers
{
	[HarmonyPatch(typeof(BackgroundEditor))]
    public class BackgroundEditorPatch : MonoBehaviour
    {
		public static BackgroundEditor Instance { get => BackgroundEditor.inst; set => BackgroundEditor.inst = value; }

		public static BackgroundObject CurrentSelectedBG => BackgroundEditor.inst == null ? null : (BackgroundObject)DataManager.inst.gameData.backgroundObjects[BackgroundEditor.inst.currentObj];

		[HarmonyPatch("Awake")]
		[HarmonyPrefix]
		static bool AwakePrefix(BackgroundEditor __instance)
		{
			if (Instance == null)
				BackgroundEditor.inst = __instance;
			else if (Instance != __instance)
			{
				Destroy(__instance.gameObject);
				return false;
			}

			Debug.Log($"{__instance.className}" +
				$"---------------------------------------------------------------------\n" +
				$"---------------------------- INITIALIZED ----------------------------\n" +
				$"---------------------------------------------------------------------\n");

			BackgroundEditorManager.Init(__instance);

			var bgRight = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/BackgroundDialog/data/right");
			var bgLeft = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/BackgroundDialog/data/left");

			#region Right

			var createTip = bgRight.transform.Find("create").GetComponent<HoverTooltip>();
			var createTooltip = new HoverTooltip.Tooltip();
			createTooltip.desc = "Create New Background Object";
			createTooltip.hint = "Press this to create a new background object.";
			createTip.tooltipLangauges.Add(createTooltip);

			var destroyAll = Instantiate(bgRight.transform.Find("create").gameObject);
			destroyAll.transform.SetParent(bgRight.transform);
			destroyAll.transform.localScale = Vector3.one;
			destroyAll.transform.SetSiblingIndex(2);
			destroyAll.name = "destroy";

			destroyAll.GetComponent<Image>().color = new Color(1f, 0.131f, 0.231f, 1f);
			destroyAll.transform.GetChild(0).GetComponent<Text>().text = "Delete All Backgrounds";
			destroyAll.transform.GetChild(0).localScale = Vector3.one;

			var destroyAllButtons = destroyAll.GetComponent<Button>();
			destroyAllButtons.onClick.ClearAll();
			destroyAllButtons.onClick.RemoveAllListeners();
			destroyAllButtons.onClick.AddListener(delegate ()
			{
				if (DataManager.inst.gameData.backgroundObjects.Count > 1)
				{
					EditorManager.inst.ShowDialog("Warning Popup");
					RTEditor.inst.RefreshWarningPopup("Are you sure you want to delete all backgrounds?", delegate ()
					{
						BackgroundEditorManager.inst.DeleteAllBackgrounds();
						EditorManager.inst.HideDialog("Warning Popup");
					}, delegate ()
					{
						EditorManager.inst.HideDialog("Warning Popup");
					});
				}
				else
					EditorManager.inst.DisplayNotification("Cannot delete only background object.", 2f, EditorManager.NotificationType.Warning);
			});

			var destroyAllTip = destroyAll.GetComponent<HoverTooltip>();
			var destroyAllTooltip = new HoverTooltip.Tooltip();
			destroyAllTooltip.desc = "Destroy All Objects";
			destroyAllTooltip.hint = "Press this to destroy all background objects, EXCEPT the first one.";
			destroyAllTip.tooltipLangauges.Clear();
			destroyAllTip.tooltipLangauges.Add(destroyAllTooltip);

			var createBGs = Instantiate(bgLeft.transform.Find("name").gameObject);
			createBGs.transform.SetParent(bgRight.transform);
			createBGs.transform.localScale = Vector3.one;
			createBGs.transform.SetSiblingIndex(2);
			createBGs.name = "create bgs";

			var name = createBGs.transform.Find("name").GetComponent<InputField>();
			var nameRT = name.GetComponent<RectTransform>();

			name.onValueChanged.ClearAll();
			name.onValueChanged.RemoveAllListeners();

			Destroy(createBGs.transform.Find("active").gameObject);
			nameRT.localScale = Vector3.one;
			name.text = "12";
			name.characterValidation = InputField.CharacterValidation.Integer;
			nameRT.sizeDelta = new Vector2(80f, 34f);

			var createAll = Instantiate(bgRight.transform.Find("create").gameObject);
			createAll.transform.SetParent(createBGs.transform);
			createAll.transform.localScale = Vector3.one;
			createAll.name = "create";

			createAll.GetComponent<Image>().color = new Color(0.6252f, 0.2789f, 0.6649f, 1f);
			createAll.GetComponent<RectTransform>().sizeDelta = new Vector2(278f, 34f);
			createAll.transform.GetChild(0).GetComponent<Text>().text = "Create Backgrounds";
			createAll.transform.GetChild(0).localScale = Vector3.one;

			var buttonCreate = createAll.GetComponent<Button>();
			buttonCreate.onClick.ClearAll();
			buttonCreate.onClick.RemoveAllListeners();
			buttonCreate.onClick.AddListener(delegate ()
			{
				BackgroundEditorManager.inst.CreateBackgrounds(int.Parse(createBGs.transform.GetChild(0).GetComponent<InputField>().text));
			});

			bgRight.transform.Find("backgrounds").GetComponent<RectTransform>().sizeDelta = new Vector2(366f, 524f);

			#endregion

			#region Left

			//Set UI Parents
			{
				var listtoadd = new List<Transform>();
				for (int i = 0; i < bgLeft.transform.childCount; i++)
					listtoadd.Add(bgLeft.transform.GetChild(i));

				var bmb = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View");

				var e = Instantiate(bmb);

				var scrollView2 = e.transform;

				scrollView2.SetParent(bgLeft.transform);
				scrollView2.localScale = Vector3.one;
				scrollView2.name = "Object Scroll View";

				var content = scrollView2.Find("Viewport/Content");
				var contentChildren = new List<Transform>();
				for (int i = 0; i < content.childCount; i++)
					contentChildren.Add(content.GetChild(i));

				foreach (var child in contentChildren)
                {
					DestroyImmediate(child.gameObject);
                }

				int num = 0;
				while (num < 20)
					num++;

				var scrollViewRT = scrollView2.GetComponent<RectTransform>();
				scrollViewRT.sizeDelta = new Vector2(366f, 690f);

				foreach (var l in listtoadd)
				{
					l.SetParent(content);
					l.transform.localScale = Vector3.one;
				}

				__instance.left = content;
			}

			__instance.right = __instance.dialog.Find("data/right");

			// Adjustments
			{
				var position = __instance.left.Find("position");
				var scale = __instance.left.Find("scale");

				DestroyImmediate(position.GetComponent<HorizontalLayoutGroup>());
				DestroyImmediate(scale.GetComponent<HorizontalLayoutGroup>());

				position.Find("x").GetComponent<HorizontalLayoutGroup>().spacing = 4f;
				position.Find("y").GetComponent<HorizontalLayoutGroup>().spacing = 4f;
				position.Find("x/text-field").GetComponent<RectTransform>().sizeDelta = new Vector2(125f, 32f);
				position.Find("y/text-field").GetComponent<RectTransform>().sizeDelta = new Vector2(125f, 32f);

				scale.Find("x").GetComponent<HorizontalLayoutGroup>().spacing = 4f;
				scale.Find("y").GetComponent<HorizontalLayoutGroup>().spacing = 4f;
				scale.Find("x/text-field").GetComponent<RectTransform>().sizeDelta = new Vector2(125f, 32f);
				scale.Find("y/text-field").GetComponent<RectTransform>().sizeDelta = new Vector2(125f, 32f);

				__instance.left.Find("color").GetComponent<GridLayoutGroup>().spacing = new Vector2(7.7f, 0f);

				var rotSlider = __instance.left.Find("rotation/slider").GetComponent<Slider>();
				rotSlider.maxValue = 360f;
				rotSlider.minValue = -360f;
			}

			var label = __instance.left.GetChild(10).gameObject;

			var shape = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/shape");
			var shapeOption = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/shapesettings");

			var labelShape = Instantiate(label);
			labelShape.transform.SetParent(__instance.left);
			labelShape.transform.localScale = Vector3.one;
			labelShape.transform.SetSiblingIndex(12);
			labelShape.name = "label";
			labelShape.transform.GetChild(0).GetComponent<Text>().text = "Shape";

			var shapeBG = Instantiate(shape);
			shapeBG.transform.SetParent(__instance.left);
			shapeBG.transform.localScale = Vector3.one;
			shapeBG.transform.SetSiblingIndex(13);
			shapeBG.name = "shape";

			var shapeOptionBG = Instantiate(shapeOption);
			shapeOptionBG.transform.SetParent(__instance.left);
			shapeOptionBG.transform.localScale = Vector3.one;
			shapeOptionBG.transform.SetSiblingIndex(14);
			shapeOptionBG.name = "shapesettings";
			var shapeSettings = shapeOptionBG.transform;

			//try
			//{
			//	// Initial removing
			//	for (int i = 0; i < shapeSettings.childCount; i++)
			//	{
			//		for (int j = 1; j < shapeSettings.GetChild(i).childCount - 1; i++)
			//		{
			//			if (i != 4 && i != 6)
			//			{
			//				Destroy(shapeSettings.GetChild(i).GetChild(j).gameObject);
			//			}
			//		}
			//	}

			//	// Readd everything
			//	var list = ShapeUI.Dictionary.Values.ToList();

			//	int shapeCount = -1;
			//	for (int i = 0; i < list.Count; i++)
			//	{
			//		if (list[i].shape > shapeCount + 1)
			//			shapeCount = list[i].shape + 1;
			//	}

			//	if (shapeCount > 0)
			//		for (int i = 0; i < shapeCount; i++)
			//		{
			//			if (shapeBG.transform.childCount < i)
			//			{
			//				var icon = Instantiate(shapeBG.transform.GetChild(shapeBG.transform.childCount - 1).gameObject);
			//				icon.transform.SetParent(shapeBG.transform);
			//				icon.transform.localScale = Vector3.one;
			//				icon.name = (i + 1).ToString();
			//			}

			//			int shapeOptionCount = -1;
			//			for (int j = 0; j < list.Count; j++)
			//			{
			//				if (list[j].shapeOption > shapeOptionCount + 1)
			//					shapeOptionCount = list[j].shapeOption + 1;
			//			}

			//			if (shapeOptionCount > 0)
			//				for (int j = 0; j < shapeOptionCount; j++)
			//				{
			//					var element = ShapeUI.Dictionary.ElementAt(i);
			//					if (!shapeSettings.Find(element.Key))
			//					{
			//						var obj = Instantiate(shapeSettings.GetChild(0).GetChild(0).gameObject);
			//						obj.transform.SetParent(shapeSettings.GetChild(i));
			//						obj.transform.SetSiblingIndex(i);
			//						obj.transform.localScale = Vector3.one;
			//						obj.name = element.Key;
			//						if (obj.transform.Find("Image") && obj.transform.Find("Image").gameObject.TryGetComponent(out Image image))
			//							image.sprite = element.Value.sprite;
			//					}
			//				}
			//		}

			//	DestroyImmediate(shapeBG.transform.GetChild(6).gameObject);
			//	DestroyImmediate(shapeBG.transform.GetChild(4).gameObject);

			//	for (int i = 0; i < shapeBG.transform.childCount; i++)
			//	{
			//		shapeBG.transform.GetChild(i).gameObject.name = (i + 1).ToString();
			//	}

			//	DestroyImmediate(shapeOptionBG.transform.GetChild(6).gameObject);
			//	DestroyImmediate(shapeOptionBG.transform.GetChild(4).gameObject);

			//	for (int i = 0; i < shapeOptionBG.transform.childCount; i++)
			//	{
			//		shapeOptionBG.transform.GetChild(i).gameObject.name = (i + 1).ToString();
			//	}
			//}
			//catch (Exception ex)
			//{

			//}

			// Depth
			{
				DestroyImmediate(__instance.left.Find("depth").gameObject);

				var iterations = Instantiate(__instance.left.Find("position").gameObject);
				iterations.transform.SetParent(__instance.left);
				iterations.transform.localScale = Vector3.one;
				iterations.name = "depth";
				DestroyImmediate(iterations.transform.GetChild(1).gameObject);
				iterations.transform.SetSiblingIndex(3);

				var xif = iterations.transform.Find("x").GetComponent<InputField>();

				xif.onValueChanged.ClearAll();
				xif.onValueChanged.AddListener(delegate (string _val)
				{
					if (int.TryParse(_val, out int num))
					{
						CurrentSelectedBG.layer = num;
						BackgroundManager.inst.UpdateBackgrounds();
					}
				});

				TriggerHelper.IncreaseDecreaseButtons(xif);
			}

			// Iterations
			{
				var iLabel = Instantiate(label);
				iLabel.transform.SetParent(__instance.left);
				iLabel.transform.localScale = Vector3.one;
				iLabel.name = "label";
				iLabel.transform.GetChild(0).GetComponent<Text>().text = "Iterations";
				iLabel.transform.SetSiblingIndex(4);

				var iterations = Instantiate(__instance.left.Find("position").gameObject);
				iterations.transform.SetParent(__instance.left);
				iterations.transform.localScale = Vector3.one;
				iterations.name = "iterations";
				DestroyImmediate(iterations.transform.GetChild(1).gameObject);
				iterations.transform.SetSiblingIndex(5);

				var x = iterations.transform.Find("x");
				var xif = x.GetComponent<InputField>();
				var left = x.Find("<").GetComponent<Button>();
				var right = x.Find(">").GetComponent<Button>();

				xif.onValueChanged.ClearAll();
				xif.onValueChanged.AddListener(delegate (string _val)
				{
					CurrentSelectedBG.depth = int.Parse(_val);
					BackgroundManager.inst.UpdateBackgrounds();
				});

				left.onClick.ClearAll();
				left.onClick.AddListener(delegate ()
				{
					CurrentSelectedBG.depth -= 1;
					BackgroundManager.inst.UpdateBackgrounds();
				});

				right.onClick.ClearAll();
				right.onClick.AddListener(delegate ()
				{
					CurrentSelectedBG.depth += 1;
					BackgroundManager.inst.UpdateBackgrounds();
				});
			}

			// ZScale
			{
				var iLabel = Instantiate(label);
				iLabel.transform.SetParent(__instance.left);
				iLabel.transform.localScale = Vector3.one;
				iLabel.name = "label";
				iLabel.transform.GetChild(0).GetComponent<Text>().text = "Z Scale";
				iLabel.transform.SetSiblingIndex(6);

				var iterations = Instantiate(__instance.left.Find("position").gameObject);
				iterations.transform.SetParent(__instance.left);
				iterations.transform.localScale = Vector3.one;
				iterations.name = "zscale";
				DestroyImmediate(iterations.transform.GetChild(1).gameObject);
				iterations.transform.SetSiblingIndex(7);

				var x = iterations.transform.Find("x");
				var xif = x.GetComponent<InputField>();
				var left = x.Find("<").GetComponent<Button>();
				var right = x.Find(">").GetComponent<Button>();

				xif.onValueChanged.ClearAll();
				xif.onValueChanged.AddListener(delegate (string _val)
				{
					CurrentSelectedBG.zscale = float.Parse(_val);
					BackgroundManager.inst.UpdateBackgrounds();
				});

				left.onClick.ClearAll();
				left.onClick.AddListener(delegate ()
				{
					CurrentSelectedBG.zscale -= 0.1f;
					BackgroundManager.inst.UpdateBackgrounds();
				});

				right.onClick.ClearAll();
				right.onClick.AddListener(delegate ()
				{
					CurrentSelectedBG.zscale += 0.1f;
					BackgroundManager.inst.UpdateBackgrounds();
				});
			}

			// Reactive
			{
				var reactiveRanges = __instance.left.Find("reactive-ranges");

				reactiveRanges.GetComponent<GridLayoutGroup>().cellSize = new Vector2(62f, 32f);

				var custom = Instantiate(reactiveRanges.GetChild(3).gameObject);
				custom.transform.SetParent(reactiveRanges);
				custom.transform.localScale = Vector3.one;
				custom.name = "custom";
				custom.transform.GetChild(1).GetComponent<Text>().text = "Custom";

				var toggle = custom.GetComponent<Toggle>();
				toggle.onValueChanged.ClearAll();
				toggle.onValueChanged.AddListener(delegate (bool _val)
				{
					if (_val && CurrentSelectedBG != null)
					{
						CurrentSelectedBG.reactiveType = (BaseBackgroundObject.ReactiveType)3;
						CurrentSelectedBG.reactive = true;
					}
				});

				var reactive = __instance.left.Find("reactive");
				var slider = reactive.Find("slider").GetComponent<RectTransform>();
				slider.sizeDelta = new Vector2(205f, 32f);

				// Reactive Position
				{
					// Samples
					{
						var iLabel = Instantiate(label);
						iLabel.transform.SetParent(__instance.left);
						iLabel.transform.localScale = Vector3.one;
						iLabel.name = "label";
						iLabel.transform.GetChild(0).GetComponent<Text>().text = "Reactive Position Samples";
						iLabel.transform.SetSiblingIndex(22);

						var position = Instantiate(__instance.left.Find("position").gameObject);
						position.transform.SetParent(__instance.left);
						position.transform.localScale = Vector3.one;
						position.name = "reactive-position-samples";
						position.transform.SetSiblingIndex(23);

						var xif = position.transform.Find("x").GetComponent<InputField>();
						var yif = position.transform.Find("y").GetComponent<InputField>();

						xif.onValueChanged.ClearAll();
						xif.onValueChanged.AddListener(delegate (string _val)
						{
							if (int.TryParse(_val, out int num))
							{
								CurrentSelectedBG.reactivePosSamples.x = num;
								BackgroundManager.inst.UpdateBackgrounds();
							}
						});

						yif.onValueChanged.ClearAll();
						yif.onValueChanged.AddListener(delegate (string _val)
						{
							if (int.TryParse(_val, out int num))
							{
								CurrentSelectedBG.reactivePosSamples.y = num;
								BackgroundManager.inst.UpdateBackgrounds();
							}
						});

						TriggerHelper.IncreaseDecreaseButtonsInt(xif, max: 255);
						TriggerHelper.IncreaseDecreaseButtonsInt(yif, max: 255);
					}

					// Intensity
					{
						var iLabel = Instantiate(label);
						iLabel.transform.SetParent(__instance.left);
						iLabel.transform.localScale = Vector3.one;
						iLabel.name = "label";
						iLabel.transform.GetChild(0).GetComponent<Text>().text = "Reactive Position Intensity";
						iLabel.transform.SetSiblingIndex(24);

						var position = Instantiate(__instance.left.Find("position").gameObject);
						position.transform.SetParent(__instance.left);
						position.transform.localScale = Vector3.one;
						position.name = "reactive-position-intensity";
						position.transform.SetSiblingIndex(25);

						var xif = position.transform.Find("x").GetComponent<InputField>();
						var yif = position.transform.Find("y").GetComponent<InputField>();

						xif.onValueChanged.ClearAll();
						xif.onValueChanged.AddListener(delegate (string _val)
						{
							if (float.TryParse(_val, out float num))
							{
								CurrentSelectedBG.reactivePosIntensity.x = num;
								BackgroundManager.inst.UpdateBackgrounds();
							}
						});

						yif.onValueChanged.ClearAll();
						yif.onValueChanged.AddListener(delegate (string _val)
						{
							if (float.TryParse(_val, out float num))
							{
								CurrentSelectedBG.reactivePosIntensity.y = num;
								BackgroundManager.inst.UpdateBackgrounds();
							}
						});

						TriggerHelper.IncreaseDecreaseButtons(xif, max: 255);
						TriggerHelper.IncreaseDecreaseButtons(yif, max: 255);
					}
				}

				// Reactive Scale
				{
					// Samples
					{
						var iLabel = Instantiate(label);
						iLabel.transform.SetParent(__instance.left);
						iLabel.transform.localScale = Vector3.one;
						iLabel.name = "label";
						iLabel.transform.GetChild(0).GetComponent<Text>().text = "Reactive Scale Samples";
						iLabel.transform.SetSiblingIndex(26);

						var position = Instantiate(__instance.left.Find("position").gameObject);
						position.transform.SetParent(__instance.left);
						position.transform.localScale = Vector3.one;
						position.name = "reactive-scale-samples";
						position.transform.SetSiblingIndex(27);

						var xif = position.transform.Find("x").GetComponent<InputField>();
						var yif = position.transform.Find("y").GetComponent<InputField>();

						xif.onValueChanged.ClearAll();
						xif.onValueChanged.AddListener(delegate (string _val)
						{
							if (int.TryParse(_val, out int num))
							{
								CurrentSelectedBG.reactiveScaSamples.x = num;
								BackgroundManager.inst.UpdateBackgrounds();
							}
						});

						yif.onValueChanged.ClearAll();
						yif.onValueChanged.AddListener(delegate (string _val)
						{
							if (int.TryParse(_val, out int num))
							{
								CurrentSelectedBG.reactiveScaSamples.y = num;
								BackgroundManager.inst.UpdateBackgrounds();
							}
						});

						TriggerHelper.IncreaseDecreaseButtonsInt(xif, max: 255);
						TriggerHelper.IncreaseDecreaseButtonsInt(yif, max: 255);
					}

					// Intensity
					{
						var iLabel = Instantiate(label);
						iLabel.transform.SetParent(__instance.left);
						iLabel.transform.localScale = Vector3.one;
						iLabel.name = "label";
						iLabel.transform.GetChild(0).GetComponent<Text>().text = "Reactive Scale Intensity";
						iLabel.transform.SetSiblingIndex(28);

						var position = Instantiate(__instance.left.Find("position").gameObject);
						position.transform.SetParent(__instance.left);
						position.transform.localScale = Vector3.one;
						position.name = "reactive-scale-intensity";
						position.transform.SetSiblingIndex(29);

						var xif = position.transform.Find("x").GetComponent<InputField>();
						var yif = position.transform.Find("y").GetComponent<InputField>();

						xif.onValueChanged.ClearAll();
						xif.onValueChanged.AddListener(delegate (string _val)
						{
							if (float.TryParse(_val, out float num))
							{
								CurrentSelectedBG.reactiveScaIntensity.x = num;
								BackgroundManager.inst.UpdateBackgrounds();
							}
						});

						yif.onValueChanged.ClearAll();
						yif.onValueChanged.AddListener(delegate (string _val)
						{
							if (float.TryParse(_val, out float num))
							{
								CurrentSelectedBG.reactiveScaIntensity.y = num;
								BackgroundManager.inst.UpdateBackgrounds();
							}
						});

						TriggerHelper.IncreaseDecreaseButtons(xif, max: 255);
						TriggerHelper.IncreaseDecreaseButtons(yif, max: 255);
					}
				}

				// Reactive Rotation
				{
					// Samples
					{
						var iLabel = Instantiate(label);
						iLabel.transform.SetParent(__instance.left);
						iLabel.transform.localScale = Vector3.one;
						iLabel.name = "label";
						iLabel.transform.GetChild(0).GetComponent<Text>().text = "Reactive Rotation Sample";
						iLabel.transform.SetSiblingIndex(30);

						var position = Instantiate(__instance.left.Find("position").gameObject);
						position.transform.SetParent(__instance.left);
						position.transform.localScale = Vector3.one;
						position.name = "reactive-rotation-sample";
						position.transform.SetSiblingIndex(31);

						DestroyImmediate(position.transform.Find("y").gameObject);

						var xif = position.transform.Find("x").GetComponent<InputField>();

						xif.onValueChanged.ClearAll();
						xif.onValueChanged.AddListener(delegate (string _val)
						{
							if (int.TryParse(_val, out int num))
							{
								CurrentSelectedBG.reactiveRotSample = num;
								BackgroundManager.inst.UpdateBackgrounds();
							}
						});

						TriggerHelper.IncreaseDecreaseButtonsInt(xif, max: 255);
					}

					// Intensity
					{
						var iLabel = Instantiate(label);
						iLabel.transform.SetParent(__instance.left);
						iLabel.transform.localScale = Vector3.one;
						iLabel.name = "label";
						iLabel.transform.GetChild(0).GetComponent<Text>().text = "Reactive Rotation Intensity";
						iLabel.transform.SetSiblingIndex(32);

						var position = Instantiate(__instance.left.Find("position").gameObject);
						position.transform.SetParent(__instance.left);
						position.transform.localScale = Vector3.one;
						position.name = "reactive-rotation-intensity";
						position.transform.SetSiblingIndex(33);

						DestroyImmediate(position.transform.Find("y").gameObject);

						var xif = position.transform.Find("x").GetComponent<InputField>();

						xif.onValueChanged.ClearAll();
						xif.onValueChanged.AddListener(delegate (string _val)
						{
							if (float.TryParse(_val, out float num))
							{
								CurrentSelectedBG.reactiveRotIntensity = num;
								BackgroundManager.inst.UpdateBackgrounds();
							}
						});

						TriggerHelper.IncreaseDecreaseButtons(xif, max: 255);
					}
				}

				// Reactive Color
				{
					// Samples
					{
						var iLabel = Instantiate(label);
						iLabel.transform.SetParent(__instance.left);
						iLabel.transform.localScale = Vector3.one;
						iLabel.name = "label";
						iLabel.transform.GetChild(0).GetComponent<Text>().text = "Reactive Color Sample";
						iLabel.transform.SetSiblingIndex(34);

						var position = Instantiate(__instance.left.Find("position").gameObject);
						position.transform.SetParent(__instance.left);
						position.transform.localScale = Vector3.one;
						position.name = "reactive-color-sample";
						position.transform.SetSiblingIndex(35);

						DestroyImmediate(position.transform.Find("y").gameObject);

						var xif = position.transform.Find("x").GetComponent<InputField>();

						xif.onValueChanged.ClearAll();
						xif.onValueChanged.AddListener(delegate (string _val)
						{
							if (int.TryParse(_val, out int num))
							{
								CurrentSelectedBG.reactiveColSample = num;
								BackgroundManager.inst.UpdateBackgrounds();
							}
						});

						TriggerHelper.IncreaseDecreaseButtonsInt(xif, max: 255);
					}

					// Intensity
					{
						var iLabel = Instantiate(label);
						iLabel.transform.SetParent(__instance.left);
						iLabel.transform.localScale = Vector3.one;
						iLabel.name = "label";
						iLabel.transform.GetChild(0).GetComponent<Text>().text = "Reactive Color Intensity";
						iLabel.transform.SetSiblingIndex(36);

						var position = Instantiate(__instance.left.Find("position").gameObject);
						position.transform.SetParent(__instance.left);
						position.transform.localScale = Vector3.one;
						position.name = "reactive-color-intensity";
						position.transform.SetSiblingIndex(37);

						DestroyImmediate(position.transform.Find("y").gameObject);

						var xif = position.transform.Find("x").GetComponent<InputField>();

						xif.onValueChanged.ClearAll();
						xif.onValueChanged.AddListener(delegate (string _val)
						{
							if (float.TryParse(_val, out float num))
							{
								CurrentSelectedBG.reactiveColIntensity = num;
								BackgroundManager.inst.UpdateBackgrounds();
							}
						});

						TriggerHelper.IncreaseDecreaseButtons(xif, max: 255);
					}

					// Reactive Color
					{
						var colorLabel = Instantiate(label);
						colorLabel.transform.SetParent(__instance.left);
						colorLabel.transform.localScale = Vector3.one;
						colorLabel.name = "label";
						colorLabel.transform.SetSiblingIndex(38);
						colorLabel.transform.GetChild(0).GetComponent<Text>().text = "Reactive Color";

						var color = __instance.left.Find("color");
						var fadeColor = Instantiate(color.gameObject);
						fadeColor.transform.SetParent(__instance.left);
						fadeColor.transform.localScale = Vector3.one;
						fadeColor.name = "reactive-color";
						fadeColor.transform.SetSiblingIndex(39);
					}
				}

				// Reactive Z
				{
					// Samples
					{
						var iLabel = Instantiate(label);
						iLabel.transform.SetParent(__instance.left);
						iLabel.transform.localScale = Vector3.one;
						iLabel.name = "label";
						iLabel.transform.GetChild(0).GetComponent<Text>().text = "Reactive Z Sample";
						iLabel.transform.SetSiblingIndex(40);

						var position = Instantiate(__instance.left.Find("position").gameObject);
						position.transform.SetParent(__instance.left);
						position.transform.localScale = Vector3.one;
						position.name = "reactive-z-sample";
						position.transform.SetSiblingIndex(41);

						DestroyImmediate(position.transform.Find("y").gameObject);

						var xif = position.transform.Find("x").GetComponent<InputField>();

						xif.onValueChanged.ClearAll();
						xif.onValueChanged.AddListener(delegate (string _val)
						{
							if (int.TryParse(_val, out int num))
							{
								CurrentSelectedBG.reactiveColSample = num;
								BackgroundManager.inst.UpdateBackgrounds();
							}
						});

						TriggerHelper.IncreaseDecreaseButtonsInt(xif, max: 255);
					}

					// Intensity
					{
						var iLabel = Instantiate(label);
						iLabel.transform.SetParent(__instance.left);
						iLabel.transform.localScale = Vector3.one;
						iLabel.name = "label";
						iLabel.transform.GetChild(0).GetComponent<Text>().text = "Reactive Z Intensity";
						iLabel.transform.SetSiblingIndex(42);

						var position = Instantiate(__instance.left.Find("position").gameObject);
						position.transform.SetParent(__instance.left);
						position.transform.localScale = Vector3.one;
						position.name = "reactive-z-intensity";
						position.transform.SetSiblingIndex(43);

						DestroyImmediate(position.transform.Find("y").gameObject);

						var xif = position.transform.Find("x").GetComponent<InputField>();

						xif.onValueChanged.ClearAll();
						xif.onValueChanged.AddListener(delegate (string _val)
						{
							if (float.TryParse(_val, out float num))
							{
								CurrentSelectedBG.reactiveZIntensity = num;
								BackgroundManager.inst.UpdateBackgrounds();
							}
						});

						TriggerHelper.IncreaseDecreaseButtons(xif, max: 255);
					}
				}
			}

			// Fade Color
			{
				var colorLabel = Instantiate(label);
				colorLabel.transform.SetParent(__instance.left);
				colorLabel.transform.localScale = Vector3.one;
				colorLabel.name = "label";
				colorLabel.transform.SetSiblingIndex(14);
				colorLabel.transform.GetChild(0).GetComponent<Text>().text = "Fade Color";

				var color = __instance.left.Find("color");
				var fadeColor = Instantiate(color.gameObject);
				fadeColor.transform.SetParent(__instance.left);
				fadeColor.transform.localScale = Vector3.one;
				fadeColor.name = "fade-color";
				fadeColor.transform.SetSiblingIndex(15);
			}

			// Rotation
			{
				var index = __instance.left.Find("rotation").GetSiblingIndex();

				var iLabel = Instantiate(label);
				iLabel.transform.SetParent(__instance.left);
				iLabel.transform.localScale = Vector3.one;
				iLabel.name = "label";
				iLabel.transform.GetChild(0).GetComponent<Text>().text = "3D Rotation";
				iLabel.transform.SetSiblingIndex(index - 1);

				var iterations = Instantiate(__instance.left.Find("position").gameObject);
				iterations.transform.SetParent(__instance.left);
				iterations.transform.localScale = Vector3.one;
				iterations.name = "depth-rotation";
				iterations.transform.SetSiblingIndex(index);

				var xif = iterations.transform.Find("x").GetComponent<InputField>();

				xif.onValueChanged.ClearAll();
				xif.onValueChanged.AddListener(delegate (string _val)
				{
					if (float.TryParse(_val, out float num))
					{
						CurrentSelectedBG.rotation.x = num;
						BackgroundManager.inst.UpdateBackgrounds();
					}
				});

				TriggerHelper.IncreaseDecreaseButtons(xif, 15f, 3f);

				var yif = iterations.transform.Find("y").GetComponent<InputField>();

				yif.onValueChanged.ClearAll();
				yif.onValueChanged.AddListener(delegate (string _val)
				{
					if (float.TryParse(_val, out float num))
					{
						CurrentSelectedBG.rotation.y = num;
						BackgroundManager.inst.UpdateBackgrounds();
					}
				});

				TriggerHelper.IncreaseDecreaseButtons(yif, 15f, 3f);
			}

			#endregion

			return false;
		}

		[HarmonyPatch("OpenDialog")]
		[HarmonyPrefix]
		static bool OpenDialog(BackgroundEditor __instance, int _bg)
		{
			BackgroundEditorManager.inst.OpenDialog(_bg);
			return false;
		}

		[HarmonyPatch("CreateNewBackground")]
		[HarmonyPrefix]
		static bool CreateNewBackground(BackgroundEditor __instance)
		{
			var backgroundObject = new BackgroundObject();
			backgroundObject.name = "Background";
			backgroundObject.scale = new Vector2(2f, 2f);
			backgroundObject.pos = Vector2.zero;

			DataManager.inst.gameData.backgroundObjects.Add(backgroundObject);

			BackgroundManager.inst.CreateBackgroundObject(backgroundObject);
			__instance.SetCurrentBackground(DataManager.inst.gameData.backgroundObjects.Count - 1);
			__instance.OpenDialog(DataManager.inst.gameData.backgroundObjects.Count - 1);

			return false;
		}

		[HarmonyPatch("UpdateBackgroundList")]
		[HarmonyPrefix]
		static bool UpdateBackgroundList(BackgroundEditor __instance)
		{
			var parent = __instance.right.Find("backgrounds/viewport/content");
			LSHelpers.DeleteChildren(parent);
			int num = 0;
			foreach (var backgroundObject in DataManager.inst.gameData.backgroundObjects)
			{
				if (backgroundObject.name.ToLower().Contains(__instance.sortedName.ToLower()) || string.IsNullOrEmpty(__instance.sortedName))
				{
					var gameObject = Instantiate(__instance.backgroundButtonPrefab, Vector3.zero, Quaternion.identity);
					gameObject.name = backgroundObject.name + "_bg";
					gameObject.transform.SetParent(parent);
					gameObject.transform.localScale = Vector3.one;

					var name = gameObject.transform.Find("name").GetComponent<Text>();
					var text = gameObject.transform.Find("pos").GetComponent<Text>();
					var image = gameObject.transform.Find("color").GetComponent<Image>();

					name.text = backgroundObject.name;
					text.text = $"({backgroundObject.pos.x}, {backgroundObject.pos.y})";

					image.color = GameManager.inst.LiveTheme.backgroundColors[backgroundObject.color];

					int bgIndexTmp = num;
					gameObject.GetComponent<Button>().onClick.AddListener(delegate ()
					{
						__instance.SetCurrentBackground(bgIndexTmp);
					});
				}
				num++;
			}

			return false;
		}

		[HarmonyPatch("UpdateBackground")]
		[HarmonyPrefix]
		static bool UpdateBackground(int __0)
		{
			var backgroundObject = (BackgroundObject)DataManager.inst.gameData.backgroundObjects[__0];

			var bgGameObject = BackgroundManager.inst.backgroundObjects[__0];

			bgGameObject.SetActive(backgroundObject.active);
			bgGameObject.transform.localPosition = new Vector3(backgroundObject.pos.x, backgroundObject.pos.y, 32f + backgroundObject.layer * 10f);
			bgGameObject.transform.localScale = new Vector3(backgroundObject.scale.x, backgroundObject.scale.y, 10f);
			bgGameObject.transform.localRotation = Quaternion.Euler(new Vector3(backgroundObject.rotation.x, backgroundObject.rotation.y, backgroundObject.rot));

			foreach (object obj in bgGameObject.transform)
				Destroy(((Transform)obj).gameObject);

			backgroundObject.gameObjects.Clear();
			backgroundObject.transforms.Clear();
			backgroundObject.renderers.Clear();

			backgroundObject.gameObjects.Add(bgGameObject);
			backgroundObject.transforms.Add(bgGameObject.transform);
			backgroundObject.renderers.Add(bgGameObject.GetComponent<Renderer>());

			if (backgroundObject.drawFade)
			{
				for (int i = 1; i < backgroundObject.depth - backgroundObject.layer; i++)
				{
					var gameObject = Instantiate(BackgroundManager.inst.backgroundFadePrefab, Vector3.zero, Quaternion.identity);
					gameObject.name = $"{backgroundObject.name} Fade [{i}]";
					gameObject.transform.SetParent(BackgroundManager.inst.backgroundObjects[__0].transform);
					gameObject.transform.localPosition = new Vector3(0f, 0f, (float)i);
					gameObject.transform.localScale = Vector3.one;
					gameObject.transform.localRotation = Quaternion.Euler(Vector3.zero);
					gameObject.layer = 9;

					backgroundObject.gameObjects.Add(gameObject);
					backgroundObject.transforms.Add(gameObject.transform);
					backgroundObject.renderers.Add(gameObject.GetComponent<Renderer>());
				}
			}

			backgroundObject.SetShape(backgroundObject.shape.Type, backgroundObject.shape.Option);

			return false;
		}

		[HarmonyPatch("CopyBackground")]
		[HarmonyPrefix]
		static bool CopyBackground(BackgroundEditor __instance)
		{
			Debug.Log($"{EditorPlugin.className}Copied Background Object");
			__instance.backgroundObjCopy = BackgroundObject.DeepCopy((BackgroundObject)DataManager.inst.gameData.backgroundObjects[__instance.currentObj]);
			__instance.hasCopiedObject = true;

			return false;
		}

		[HarmonyPatch("DeleteBackground")]
		[HarmonyPrefix]
		static bool DeleteBackground(BackgroundEditor __instance, ref string __result, int __0)
		{
			if (DataManager.inst.gameData.backgroundObjects.Count <= 1)
			{
				EditorManager.inst.DisplayNotification("Unable to delete last background element! Consider moving it off screen or turning it into your first background element for the level.", 2f, EditorManager.NotificationType.Error, false);
				__result = null;
				return false;
			}

			string name = DataManager.inst.gameData.backgroundObjects[__0].name;
			DataManager.inst.gameData.backgroundObjects.RemoveAt(__0);

			if (DataManager.inst.gameData.backgroundObjects.Count > 0)
				__instance.SetCurrentBackground(Mathf.Clamp(__instance.currentObj - 1, 0, DataManager.inst.gameData.backgroundObjects.Count - 1));

			BackgroundManager.inst.UpdateBackgrounds();

			__result = name;
			return false;
		}

		[HarmonyPatch("PasteBackground")]
		[HarmonyPrefix]
		static bool PasteBackground(BackgroundEditor __instance, ref string __result)
		{
			if (!__instance.hasCopiedObject || __instance.backgroundObjCopy == null)
			{
				EditorManager.inst.DisplayNotification("No copied background yet!", 2f, EditorManager.NotificationType.Error);
				__result = "";
				return false;
			}

			var backgroundObject = BackgroundObject.DeepCopy((BackgroundObject)__instance.backgroundObjCopy);
			DataManager.inst.gameData.backgroundObjects.Add(backgroundObject);

			int currentBackground = DataManager.inst.gameData.backgroundObjects.IndexOf(backgroundObject);
			BackgroundManager.inst.CreateBackgroundObject(backgroundObject);
			__instance.SetCurrentBackground(currentBackground);
			__result = backgroundObject.name.ToString();
			return false;
		}

		[HarmonyPatch("SetRot", new Type[] { typeof(string) })]
		[HarmonyPrefix]
		static bool SetRot(BackgroundEditor __instance, string __0)
		{
			if (float.TryParse(__0, out float rot))
			{
				DataManager.inst.gameData.backgroundObjects[__instance.currentObj].rot = rot;
				__instance.left.Find("rotation/slider").GetComponent<Slider>().value = rot;
				__instance.UpdateBackground(__instance.currentObj);
			}

			return false;
		}
	}
}
