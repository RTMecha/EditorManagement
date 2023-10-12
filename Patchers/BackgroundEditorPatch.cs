using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using LSFunctions;

using EditorManagement.Functions.Editors;
using EditorManagement.Functions.Tools;
using EditorManagement.Functions.Components;

using RTFunctions.Functions;
using RTFunctions.Functions.Managers;

using BackgroundObject = DataManager.GameData.BackgroundObject;

namespace EditorManagement.Patchers
{
    [HarmonyPatch(typeof(BackgroundEditor))]
    public class BackgroundEditorPatch : MonoBehaviour
    {
		public static BackgroundObject CurrentSelectedBG => BackgroundEditor.inst == null ? null : DataManager.inst.gameData.backgroundObjects[BackgroundEditor.inst.currentObj];
		public static Objects.BackgroundObject CurrentSelectedModBG => BackgroundEditor.inst == null ? null : Objects.backgroundObjects[BackgroundEditor.inst.currentObj];

		[HarmonyPatch("Awake")]
		[HarmonyPostfix]
		static void AwakePostfix(BackgroundEditor __instance)
		{
			GameObject bgRight = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/BackgroundDialog/data/right");
			GameObject bgLeft = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/BackgroundDialog/data/left");

            #region Right

            var createTip = bgRight.transform.Find("create").GetComponent<HoverTooltip>();
			HoverTooltip.Tooltip createTooltip = new HoverTooltip.Tooltip();
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
					RTEditor.RefreshWarningPopup("Are you sure you want to delete all backgrounds?", delegate ()
					{
						RTEditor.DeleteAllBackgrounds();
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
			HoverTooltip.Tooltip destroyAllTooltip = new HoverTooltip.Tooltip();
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
				RTEditor.CreateBackgrounds(int.Parse(createBGs.transform.GetChild(0).GetComponent<InputField>().text));
			});

			bgRight.transform.Find("backgrounds").GetComponent<RectTransform>().sizeDelta = new Vector2(366f, 524f);

			#endregion

			#region Left

			__instance.StartCoroutine(SetupShapes(__instance));

			#endregion
		}

		public static IEnumerator SetupShapes(BackgroundEditor __instance)
        {
			yield return new WaitForSeconds(4f);

			GameObject bgRight = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/BackgroundDialog/data/right");
			GameObject bgLeft = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/BackgroundDialog/data/left");

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

				var scrollViewRT = scrollView2.GetComponent<RectTransform>();
				scrollViewRT.sizeDelta = new Vector2(366f, 690f);

				var content = scrollView2.Find("Viewport/Content");
				LSHelpers.DeleteChildren(content);

				yield return new WaitForSeconds(0.4f);

				foreach (var l in listtoadd)
				{
					l.SetParent(content);
					l.transform.localScale = Vector3.one;
				}

				__instance.left = content;
			}

			yield return new WaitForSeconds(0.4f);

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

			try
			{
				DestroyImmediate(shapeBG.transform.GetChild(6).gameObject);
				DestroyImmediate(shapeBG.transform.GetChild(4).gameObject);

				for (int i = 0; i < shapeBG.transform.childCount; i++)
				{
					shapeBG.transform.GetChild(i).gameObject.name = (i + 1).ToString();
				}

				DestroyImmediate(shapeOptionBG.transform.GetChild(6).gameObject);
				DestroyImmediate(shapeOptionBG.transform.GetChild(4).gameObject);

				for (int i = 0; i < shapeOptionBG.transform.childCount; i++)
				{
					shapeOptionBG.transform.GetChild(i).gameObject.name = (i + 1).ToString();
				}
			}
            catch (Exception ex)
            {

            }

			// Depth
			{
				DestroyImmediate(__instance.left.Find("depth").gameObject);

				var iterations = Instantiate(__instance.left.Find("position").gameObject);
				iterations.transform.SetParent(__instance.left);
				iterations.transform.localScale = Vector3.one;
				iterations.name = "depth";
				DestroyImmediate(iterations.transform.GetChild(1).gameObject);
				iterations.transform.SetSiblingIndex(3);

				var x = iterations.transform.Find("x");
				var xif = x.GetComponent<InputField>();
				var left = x.Find("<").GetComponent<Button>();
				var right = x.Find(">").GetComponent<Button>();

				xif.onValueChanged.ClearAll();
				xif.onValueChanged.AddListener(delegate (string _val)
				{
					if (CurrentSelectedBG != null)
						CurrentSelectedBG.layer = int.Parse(_val);
					BackgroundManager.inst.UpdateBackgrounds();
				});

				left.onClick.ClearAll();
				left.onClick.AddListener(delegate ()
				{
					if (CurrentSelectedBG != null)
						CurrentSelectedBG.layer -= 1;
					BackgroundManager.inst.UpdateBackgrounds();
				});

				right.onClick.ClearAll();
				right.onClick.AddListener(delegate ()
				{
					if (CurrentSelectedBG != null)
						CurrentSelectedBG.layer += 1;
					BackgroundManager.inst.UpdateBackgrounds();
				});
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
					if (CurrentSelectedModBG != null)
						Objects.backgroundObjects[__instance.currentObj].depth = int.Parse(_val);
					BackgroundManager.inst.UpdateBackgrounds();
				});

				left.onClick.ClearAll();
				left.onClick.AddListener(delegate ()
				{
					if (CurrentSelectedModBG != null)
						Objects.backgroundObjects[__instance.currentObj].depth -= 1;
					BackgroundManager.inst.UpdateBackgrounds();
				});

				right.onClick.ClearAll();
				right.onClick.AddListener(delegate ()
				{
					if (CurrentSelectedModBG != null)
						Objects.backgroundObjects[__instance.currentObj].depth += 1;
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
					Objects.backgroundObjects[__instance.currentObj].zscale = float.Parse(_val);
					BackgroundManager.inst.UpdateBackgrounds();
				});

				left.onClick.ClearAll();
				left.onClick.AddListener(delegate ()
				{
					Objects.backgroundObjects[__instance.currentObj].zscale -= 0.1f;
					BackgroundManager.inst.UpdateBackgrounds();
				});

				right.onClick.ClearAll();
				right.onClick.AddListener(delegate ()
				{
					Objects.backgroundObjects[__instance.currentObj].zscale += 0.1f;
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
						CurrentSelectedBG.reactiveType = (BackgroundObject.ReactiveType)3;
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

						var x = position.transform.Find("x");
						var xif = x.GetComponent<InputField>();
						var xleft = x.Find("<").GetComponent<Button>();
						var xright = x.Find(">").GetComponent<Button>();

						var y = position.transform.Find("y");
						var yif = y.GetComponent<InputField>();
						var yleft = y.Find("<").GetComponent<Button>();
						var yright = y.Find(">").GetComponent<Button>();

						xif.onValueChanged.ClearAll();
						xif.onValueChanged.AddListener(delegate (string _val)
						{
							Objects.backgroundObjects[__instance.currentObj].reactivePosSamples.x = int.Parse(_val);
							BackgroundManager.inst.UpdateBackgrounds();
						});

						xleft.onClick.ClearAll();
						xleft.onClick.AddListener(delegate ()
						{
							Objects.backgroundObjects[__instance.currentObj].reactivePosSamples.x -= 1;
							BackgroundManager.inst.UpdateBackgrounds();
						});

						xright.onClick.ClearAll();
						xright.onClick.AddListener(delegate ()
						{
							Objects.backgroundObjects[__instance.currentObj].reactivePosSamples.x += 1;
							BackgroundManager.inst.UpdateBackgrounds();
						});

						yif.onValueChanged.ClearAll();
						yif.onValueChanged.AddListener(delegate (string _val)
						{
							Objects.backgroundObjects[__instance.currentObj].reactivePosSamples.y = int.Parse(_val);
							BackgroundManager.inst.UpdateBackgrounds();
						});

						yleft.onClick.ClearAll();
						yleft.onClick.AddListener(delegate ()
						{
							Objects.backgroundObjects[__instance.currentObj].reactivePosSamples.y -= 1;
							BackgroundManager.inst.UpdateBackgrounds();
						});

						yright.onClick.ClearAll();
						yright.onClick.AddListener(delegate ()
						{
							Objects.backgroundObjects[__instance.currentObj].reactivePosSamples.y += 1;
							BackgroundManager.inst.UpdateBackgrounds();
						});
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

						var x = position.transform.Find("x");
						var xif = x.GetComponent<InputField>();
						var xleft = x.Find("<").GetComponent<Button>();
						var xright = x.Find(">").GetComponent<Button>();

						var y = position.transform.Find("y");
						var yif = y.GetComponent<InputField>();
						var yleft = y.Find("<").GetComponent<Button>();
						var yright = y.Find(">").GetComponent<Button>();

						xif.onValueChanged.ClearAll();
						xif.onValueChanged.AddListener(delegate (string _val)
						{
							Objects.backgroundObjects[__instance.currentObj].reactivePosIntensity.x = float.Parse(_val);
							BackgroundManager.inst.UpdateBackgrounds();
						});

						xleft.onClick.ClearAll();
						xleft.onClick.AddListener(delegate ()
						{
							Objects.backgroundObjects[__instance.currentObj].reactivePosIntensity.x -= 0.1f;
							BackgroundManager.inst.UpdateBackgrounds();
						});

						xright.onClick.ClearAll();
						xright.onClick.AddListener(delegate ()
						{
							Objects.backgroundObjects[__instance.currentObj].reactivePosIntensity.x += 0.1f;
							BackgroundManager.inst.UpdateBackgrounds();
						});

						yif.onValueChanged.ClearAll();
						yif.onValueChanged.AddListener(delegate (string _val)
						{
							Objects.backgroundObjects[__instance.currentObj].reactivePosIntensity.y = float.Parse(_val);
							BackgroundManager.inst.UpdateBackgrounds();
						});

						yleft.onClick.ClearAll();
						yleft.onClick.AddListener(delegate ()
						{
							Objects.backgroundObjects[__instance.currentObj].reactivePosIntensity.y -= 0.1f;
							BackgroundManager.inst.UpdateBackgrounds();
						});

						yright.onClick.ClearAll();
						yright.onClick.AddListener(delegate ()
						{
							Objects.backgroundObjects[__instance.currentObj].reactivePosIntensity.y += 0.1f;
							BackgroundManager.inst.UpdateBackgrounds();
						});
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

						var x = position.transform.Find("x");
						var xif = x.GetComponent<InputField>();
						var xleft = x.Find("<").GetComponent<Button>();
						var xright = x.Find(">").GetComponent<Button>();

						var y = position.transform.Find("y");
						var yif = y.GetComponent<InputField>();
						var yleft = y.Find("<").GetComponent<Button>();
						var yright = y.Find(">").GetComponent<Button>();

						xif.onValueChanged.ClearAll();
						xif.onValueChanged.AddListener(delegate (string _val)
						{
							Objects.backgroundObjects[__instance.currentObj].reactiveScaSamples.x = int.Parse(_val);
							BackgroundManager.inst.UpdateBackgrounds();
						});

						xleft.onClick.ClearAll();
						xleft.onClick.AddListener(delegate ()
						{
							Objects.backgroundObjects[__instance.currentObj].reactiveScaSamples.x -= 1;
							BackgroundManager.inst.UpdateBackgrounds();
						});

						xright.onClick.ClearAll();
						xright.onClick.AddListener(delegate ()
						{
							Objects.backgroundObjects[__instance.currentObj].reactiveScaSamples.x += 1;
							BackgroundManager.inst.UpdateBackgrounds();
						});

						yif.onValueChanged.ClearAll();
						yif.onValueChanged.AddListener(delegate (string _val)
						{
							Objects.backgroundObjects[__instance.currentObj].reactiveScaSamples.y = int.Parse(_val);
							BackgroundManager.inst.UpdateBackgrounds();
						});

						yleft.onClick.ClearAll();
						yleft.onClick.AddListener(delegate ()
						{
							Objects.backgroundObjects[__instance.currentObj].reactiveScaSamples.y -= 1;
							BackgroundManager.inst.UpdateBackgrounds();
						});

						yright.onClick.ClearAll();
						yright.onClick.AddListener(delegate ()
						{
							Objects.backgroundObjects[__instance.currentObj].reactiveScaSamples.y += 1;
							BackgroundManager.inst.UpdateBackgrounds();
						});
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

						var x = position.transform.Find("x");
						var xif = x.GetComponent<InputField>();
						var xleft = x.Find("<").GetComponent<Button>();
						var xright = x.Find(">").GetComponent<Button>();

						var y = position.transform.Find("y");
						var yif = y.GetComponent<InputField>();
						var yleft = y.Find("<").GetComponent<Button>();
						var yright = y.Find(">").GetComponent<Button>();

						xif.onValueChanged.ClearAll();
						xif.onValueChanged.AddListener(delegate (string _val)
						{
							Objects.backgroundObjects[__instance.currentObj].reactiveScaIntensity.x = float.Parse(_val);
							BackgroundManager.inst.UpdateBackgrounds();
						});

						xleft.onClick.ClearAll();
						xleft.onClick.AddListener(delegate ()
						{
							Objects.backgroundObjects[__instance.currentObj].reactiveScaIntensity.x -= 0.1f;
							BackgroundManager.inst.UpdateBackgrounds();
						});

						xright.onClick.ClearAll();
						xright.onClick.AddListener(delegate ()
						{
							Objects.backgroundObjects[__instance.currentObj].reactiveScaIntensity.x += 0.1f;
							BackgroundManager.inst.UpdateBackgrounds();
						});

						yif.onValueChanged.ClearAll();
						yif.onValueChanged.AddListener(delegate (string _val)
						{
							Objects.backgroundObjects[__instance.currentObj].reactiveScaIntensity.y = float.Parse(_val);
							BackgroundManager.inst.UpdateBackgrounds();
						});

						yleft.onClick.ClearAll();
						yleft.onClick.AddListener(delegate ()
						{
							Objects.backgroundObjects[__instance.currentObj].reactiveScaIntensity.y -= 0.1f;
							BackgroundManager.inst.UpdateBackgrounds();
						});

						yright.onClick.ClearAll();
						yright.onClick.AddListener(delegate ()
						{
							Objects.backgroundObjects[__instance.currentObj].reactiveScaIntensity.y += 0.1f;
							BackgroundManager.inst.UpdateBackgrounds();
						});
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

						var x = position.transform.Find("x");
						var xif = x.GetComponent<InputField>();
						var xleft = x.Find("<").GetComponent<Button>();
						var xright = x.Find(">").GetComponent<Button>();


						xif.onValueChanged.ClearAll();
						xif.onValueChanged.AddListener(delegate (string _val)
						{
							Objects.backgroundObjects[__instance.currentObj].reactiveRotSample = int.Parse(_val);
							BackgroundManager.inst.UpdateBackgrounds();
						});

						xleft.onClick.ClearAll();
						xleft.onClick.AddListener(delegate ()
						{
							Objects.backgroundObjects[__instance.currentObj].reactiveRotSample -= 1;
							BackgroundManager.inst.UpdateBackgrounds();
						});

						xright.onClick.ClearAll();
						xright.onClick.AddListener(delegate ()
						{
							Objects.backgroundObjects[__instance.currentObj].reactiveRotSample += 1;
							BackgroundManager.inst.UpdateBackgrounds();
						});
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

						var x = position.transform.Find("x");
						var xif = x.GetComponent<InputField>();
						var xleft = x.Find("<").GetComponent<Button>();
						var xright = x.Find(">").GetComponent<Button>();

						xif.onValueChanged.ClearAll();
						xif.onValueChanged.AddListener(delegate (string _val)
						{
							Objects.backgroundObjects[__instance.currentObj].reactiveRotIntensity = float.Parse(_val);
							BackgroundManager.inst.UpdateBackgrounds();
						});

						xleft.onClick.ClearAll();
						xleft.onClick.AddListener(delegate ()
						{
							Objects.backgroundObjects[__instance.currentObj].reactiveRotIntensity -= 0.1f;
							BackgroundManager.inst.UpdateBackgrounds();
						});

						xright.onClick.ClearAll();
						xright.onClick.AddListener(delegate ()
						{
							Objects.backgroundObjects[__instance.currentObj].reactiveRotIntensity += 0.1f;
							BackgroundManager.inst.UpdateBackgrounds();
						});
					}
				}

                // Reactive Z
                {

                }
			}
		}

        [HarmonyPatch("OpenDialog")]
		[HarmonyPrefix]
		static bool OpenDialog(BackgroundEditor __instance, int _bg)
		{
			EditorManager.inst.ClearDialogs(Array.Empty<EditorManager.EditorDialog.DialogType>());
			EditorManager.inst.SetDialogStatus("Background Editor", true, true);
			__instance.left = __instance.dialog.Find("data/left/Object Scroll View/Viewport/Content");
			__instance.right = __instance.dialog.Find("data/right");

			DataManager.GameData.BackgroundObject backgroundObject = DataManager.inst.gameData.backgroundObjects[_bg];
			var bg = Objects.backgroundObjects[_bg];

			__instance.left.Find("name/active").GetComponent<Toggle>().isOn = backgroundObject.active;
			__instance.left.Find("name/name").GetComponent<InputField>().text = backgroundObject.name;
			//__instance.left.Find("depth/layer").GetComponent<Slider>().value = (float)backgroundObject.layer;

			var iterations = __instance.left.Find("iterations/x").GetComponent<InputField>();
			iterations.text = bg.depth.ToString();

			if (!iterations.GetComponent<EventTrigger>())
			{
				var etX = iterations.gameObject.AddComponent<EventTrigger>();

				etX.triggers.Add(Triggers.ScrollDeltaInt(iterations, 1));
			}
			
			var depth = __instance.left.Find("depth/x").GetComponent<InputField>();
			depth.text = backgroundObject.layer.ToString();

			if (!depth.GetComponent<EventTrigger>())
			{
				var etX = depth.gameObject.AddComponent<EventTrigger>();

				etX.triggers.Add(Triggers.ScrollDeltaInt(depth, 1));
			}

			var zscale = __instance.left.Find("zscale/x").GetComponent<InputField>();
			zscale.text = bg.zscale.ToString();

			if (!zscale.GetComponent<EventTrigger>())
			{
				var etX = zscale.gameObject.AddComponent<EventTrigger>();

				etX.triggers.Add(Triggers.ScrollDelta(zscale, 0.1f, 10f));
			}

			var fade = __instance.left.Find("fade").GetComponent<Toggle>();

			fade.interactable = false;
			fade.isOn = backgroundObject.drawFade;
			fade.interactable = true;

			var posX = __instance.left.Find("position/x");
			var posY = __instance.left.Find("position/y");

			var posXIF = posX.GetComponent<InputField>();
			var posYIF = posY.GetComponent<InputField>();

			posXIF.text = backgroundObject.pos.x.ToString();
			posYIF.text = backgroundObject.pos.y.ToString();

			if (!posX.GetComponent<EventTrigger>())
            {
				var etX = posX.gameObject.AddComponent<EventTrigger>();
				var etY = posY.gameObject.AddComponent<EventTrigger>();

				etX.triggers.Add(Triggers.ScrollDelta(posXIF, 0.1f, 10f, true));
				etX.triggers.Add(Triggers.ScrollDeltaVector2(posXIF, posYIF, 0.1f, 10f));
				etY.triggers.Add(Triggers.ScrollDelta(posYIF, 0.1f, 10f, true));
				etY.triggers.Add(Triggers.ScrollDeltaVector2(posXIF, posYIF, 0.1f, 10f));
			}

			var scaX = __instance.left.Find("scale/x");
			var scaY = __instance.left.Find("scale/y");

			var scaXIF = scaX.GetComponent<InputField>();
			var scaYIF = scaY.GetComponent<InputField>();

			scaXIF.text = backgroundObject.scale.x.ToString();
			scaYIF.text = backgroundObject.scale.y.ToString();

			if (!scaX.GetComponent<EventTrigger>())
			{
				var etX = scaX.gameObject.AddComponent<EventTrigger>();
				var etY = scaY.gameObject.AddComponent<EventTrigger>();

				etX.triggers.Add(Triggers.ScrollDelta(scaXIF, 0.1f, 10f, true));
				etX.triggers.Add(Triggers.ScrollDeltaVector2(scaXIF, scaYIF, 0.1f, 10f));
				etY.triggers.Add(Triggers.ScrollDelta(scaYIF, 0.1f, 10f, true));
				etY.triggers.Add(Triggers.ScrollDeltaVector2(scaXIF, scaYIF, 0.1f, 10f));
			}

			var rot = __instance.left.Find("rotation/x");
			var rotIF = rot.GetComponent<InputField>();

			rotIF.text = backgroundObject.rot.ToString();

			if (!rot.GetComponent<EventTrigger>())
			{
				var et = rot.gameObject.AddComponent<EventTrigger>();
				et.triggers.Add(Triggers.ScrollDelta(rotIF, 15f, 3));
			}

			__instance.left.Find("rotation/slider").GetComponent<Slider>().value = backgroundObject.rot;

			try
			{
				if (!backgroundObject.reactive)
					__instance.left.Find("reactive-ranges").GetChild(0).GetComponent<Toggle>().isOn = true;
				else
					__instance.left.Find("reactive-ranges").GetChild((int)(backgroundObject.reactiveType + 1)).GetComponent<Toggle>().isOn = true;
			}
			catch
			{
				__instance.left.Find("reactive-ranges").GetChild(0).GetComponent<Toggle>().isOn = true;
				Debug.LogError($"{EditorPlugin.className}Custom Reactive not implemented.");
			}

			__instance.left.Find("reactive/x").GetComponent<InputField>().text = backgroundObject.reactiveScale.ToString("f2");
			__instance.left.Find("reactive/slider").GetComponent<Slider>().value = backgroundObject.reactiveScale;

			LSHelpers.DeleteChildren(__instance.left.Find("color"));

			int num = 0;
			foreach (var col in GameManager.inst.LiveTheme.backgroundColors)
			{
				var gameObject = Instantiate(EditorManager.inst.colorGUI, Vector3.zero, Quaternion.identity);
				gameObject.name = "color gui";
				gameObject.transform.SetParent(__instance.left.Find("color"));
				gameObject.transform.localScale = Vector3.one;
				gameObject.GetComponent<Image>().color = LSColors.fadeColor(col, 1f);
				gameObject.transform.Find("Image").gameObject.SetActive(false);

				if (backgroundObject.color == num)
				{
					gameObject.transform.Find("Image").gameObject.SetActive(true);
				}

				int colTmp = num;
				gameObject.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					__instance.SetColor(colTmp);
				});
				num++;
			}

			if (__instance.left.transform.TryFind("shape", out Transform shape) && __instance.left.transform.TryFind("shapesettings", out Transform shapeOption))
			{
				foreach (object obj3 in shapeOption)
				{
					var ch = (Transform)obj3;
					foreach (var c in ch)
					{
						var e = (Transform)c;
						if (!e.GetComponent<HoverUI>())
						{
							var he = e.gameObject.AddComponent<HoverUI>();
							he.animatePos = false;
							he.animateSca = true;
							he.size = 1.1f;
						}
					}
					ch.gameObject.SetActive(false);
				}

				var current = shapeOption.GetChild(bg.shape.Type);
				current.gameObject.SetActive(true);
				for (int j = 1; j <= shape.childCount; j++)
				{
					if (__instance.left.transform.Find("shape/" + j))
					{
						int buttonTmp = j;
						var shoggle = __instance.left.transform.Find("shape/" + j).GetComponent<Toggle>();
						shoggle.onValueChanged.RemoveAllListeners();
						if (bg.shape.Type == buttonTmp - 1)
						{
							__instance.left.transform.Find("shape/" + j).GetComponent<Toggle>().isOn = true;
						}
						else
						{
							__instance.left.transform.Find("shape/" + j).GetComponent<Toggle>().isOn = false;
						}
						shoggle.onValueChanged.AddListener(delegate (bool _value)
						{
							if (_value)
							{
								bg.SetShape(buttonTmp - 1, 0);

								__instance.OpenDialog(_bg);
							}
						});

						if (!__instance.left.transform.Find("shape/" + j).GetComponent<HoverUI>())
						{
							var hoverUI = __instance.left.transform.Find("shape/" + j).gameObject.AddComponent<HoverUI>();
							hoverUI.animatePos = false;
							hoverUI.animateSca = true;
							hoverUI.size = 1.1f;
						}
					}
				}

				for (int k = 0; k < current.childCount - 1; k++)
				{
					int buttonTmp = k;

					var toggle = current.GetChild(k).GetComponent<Toggle>();

					toggle.onValueChanged.RemoveAllListeners();
					if (bg.shape.Option == k)
						toggle.isOn = true;
					else
						toggle.isOn = false;
					toggle.onValueChanged.AddListener(delegate (bool _value)
					{
						if (_value)
							bg.SetShape(bg.shape.Type, buttonTmp);
					});
				}
			}

            // Reactive
            {
                // Position
                {
					// Samples
					{
						var reactiveX = __instance.left.Find("reactive-position-samples/x").GetComponent<InputField>();
						reactiveX.text = bg.reactivePosSamples.x.ToString();

						if (!reactiveX.GetComponent<EventTrigger>())
						{
							var etX = reactiveX.gameObject.AddComponent<EventTrigger>();

							etX.triggers.Add(Triggers.ScrollDeltaInt(reactiveX, 1));
						}

						var reactiveY = __instance.left.Find("reactive-position-samples/y").GetComponent<InputField>();
						reactiveY.text = bg.reactivePosSamples.y.ToString();

						if (!reactiveY.GetComponent<EventTrigger>())
						{
							var etX = reactiveY.gameObject.AddComponent<EventTrigger>();

							etX.triggers.Add(Triggers.ScrollDeltaInt(reactiveY, 1));
						}
					}

					// Intensity
					{
						var reactiveX = __instance.left.Find("reactive-position-intensity/x").GetComponent<InputField>();
						reactiveX.text = bg.reactivePosIntensity.x.ToString();

						if (!reactiveX.GetComponent<EventTrigger>())
						{
							var etX = reactiveX.gameObject.AddComponent<EventTrigger>();

							etX.triggers.Add(Triggers.ScrollDelta(reactiveX, 0.1f, 10f));
						}

						var reactiveY = __instance.left.Find("reactive-position-intensity/y").GetComponent<InputField>();
						reactiveY.text = bg.reactivePosIntensity.y.ToString();

						if (!reactiveY.GetComponent<EventTrigger>())
						{
							var etX = reactiveY.gameObject.AddComponent<EventTrigger>();

							etX.triggers.Add(Triggers.ScrollDelta(reactiveY, 0.1f, 10f));
						}
					}
                }

                // Scale
                {
					// Samples
					{
						var reactiveX = __instance.left.Find("reactive-scale-samples/x").GetComponent<InputField>();
						reactiveX.text = bg.reactiveScaSamples.x.ToString();

						if (!reactiveX.GetComponent<EventTrigger>())
						{
							var etX = reactiveX.gameObject.AddComponent<EventTrigger>();

							etX.triggers.Add(Triggers.ScrollDeltaInt(reactiveX, 1));
						}

						var reactiveY = __instance.left.Find("reactive-scale-samples/y").GetComponent<InputField>();
						reactiveY.text = bg.reactiveScaSamples.y.ToString();

						if (!reactiveY.GetComponent<EventTrigger>())
						{
							var etX = reactiveY.gameObject.AddComponent<EventTrigger>();

							etX.triggers.Add(Triggers.ScrollDeltaInt(reactiveY, 1));
						}
					}

					// Intensity
					{
						var reactiveX = __instance.left.Find("reactive-scale-intensity/x").GetComponent<InputField>();
						reactiveX.text = bg.reactiveScaIntensity.x.ToString();

						if (!reactiveX.GetComponent<EventTrigger>())
						{
							var etX = reactiveX.gameObject.AddComponent<EventTrigger>();

							etX.triggers.Add(Triggers.ScrollDelta(reactiveX, 0.1f, 10f));
						}

						var reactiveY = __instance.left.Find("reactive-scale-intensity/y").GetComponent<InputField>();
						reactiveY.text = bg.reactiveScaIntensity.y.ToString();

						if (!reactiveY.GetComponent<EventTrigger>())
						{
							var etX = reactiveY.gameObject.AddComponent<EventTrigger>();

							etX.triggers.Add(Triggers.ScrollDelta(reactiveY, 0.1f, 10f));
						}
					}
				}

				// Rotation
				{
					// Samples
					{
						var reactiveX = __instance.left.Find("reactive-rotation-sample/x").GetComponent<InputField>();
						reactiveX.text = bg.reactiveRotSample.ToString();

						if (!reactiveX.GetComponent<EventTrigger>())
						{
							var etX = reactiveX.gameObject.AddComponent<EventTrigger>();

							etX.triggers.Add(Triggers.ScrollDeltaInt(reactiveX, 1));
						}
					}

					// Intensity
					{
						var reactiveX = __instance.left.Find("reactive-rotation-intensity/x").GetComponent<InputField>();
						reactiveX.text = bg.reactiveRotIntensity.ToString();

						if (!reactiveX.GetComponent<EventTrigger>())
						{
							var etX = reactiveX.gameObject.AddComponent<EventTrigger>();

							etX.triggers.Add(Triggers.ScrollDelta(reactiveX, 0.1f, 10f));
						}
					}
				}
			}

			__instance.UpdateBackgroundList();
			__instance.dialog.gameObject.SetActive(true);

			return false;
		}

		[HarmonyPatch("CreateNewBackground")]
		[HarmonyPrefix]
		static bool CreateNewBackground(BackgroundEditor __instance)
		{
			var backgroundObject = new DataManager.GameData.BackgroundObject();
			backgroundObject.name = "Background";
			backgroundObject.scale = new Vector2(2f, 2f);
			backgroundObject.pos = Vector2.zero;

			DataManager.inst.gameData.backgroundObjects.Add(backgroundObject);

			var bg = new Objects.BackgroundObject(backgroundObject);
			Objects.backgroundObjects.Add(bg);

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
					text.text = string.Concat(new object[]
					{
						"(",
						backgroundObject.pos.x,
						", ",
						backgroundObject.pos.y,
						")"
					});
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
			var bg = Objects.backgroundObjects[__0];

			var backgroundObject = bg.bg;

			var bgGameObject = BackgroundManager.inst.backgroundObjects[__0];

			bgGameObject.SetActive(backgroundObject.active);
			bgGameObject.transform.localPosition = new Vector3(backgroundObject.pos.x, backgroundObject.pos.y, (float)(32 + backgroundObject.layer * 10));
			bgGameObject.transform.localScale = new Vector3(backgroundObject.scale.x, backgroundObject.scale.y, 10f);
			bgGameObject.transform.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, backgroundObject.rot));
			//bgGameObject.GetComponent<Renderer>().material.color = GameManager.inst.LiveTheme.backgroundColors[Mathf.Clamp(backgroundObject.color, 0, GameManager.inst.LiveTheme.backgroundColors.Count - 1)];

			foreach (object obj in BackgroundManager.inst.backgroundObjects[__0].transform)
				Destroy(((Transform)obj).gameObject);

			bg.gameObjects.Clear();
			bg.transforms.Clear();
			bg.renderers.Clear();

			bg.gameObjects.Add(bgGameObject);
			bg.transforms.Add(bgGameObject.transform);
			bg.renderers.Add(bgGameObject.GetComponent<Renderer>());

			if (backgroundObject.drawFade)
			{
				for (int i = 1; i < bg.depth - backgroundObject.layer; i++)
				{
					var gameObject = Instantiate(BackgroundManager.inst.backgroundFadePrefab, Vector3.zero, Quaternion.identity);
					gameObject.name = "Fade [" + i + "]";
					gameObject.transform.SetParent(BackgroundManager.inst.backgroundObjects[__0].transform);
					gameObject.transform.localPosition = new Vector3(0f, 0f, (float)i);
					gameObject.transform.localScale = Vector3.one;
					gameObject.transform.localRotation = Quaternion.Euler(Vector3.zero);
					gameObject.layer = 9;

					bg.gameObjects.Add(gameObject);
					bg.transforms.Add(gameObject.transform);
					bg.renderers.Add(gameObject.GetComponent<Renderer>());
				}
			}

			bg.SetShape(bg.shape.Type, bg.shape.Option);

			return false;
		}

		[HarmonyPatch("CopyBackground")]
		[HarmonyPrefix]
		static bool CopyBackground(BackgroundEditor __instance)
		{
			Debug.Log($"{EditorPlugin.className}Copied Background Object");
			__instance.backgroundObjCopy = DataManager.GameData.BackgroundObject.DeepCopy(DataManager.inst.gameData.backgroundObjects[__instance.currentObj]);
			bgCopy = Objects.BackgroundObject.DeepCopy(Objects.backgroundObjects[__instance.currentObj]);
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

			Objects.backgroundObjects.RemoveAt(__0);

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
			if (!__instance.hasCopiedObject || __instance.backgroundObjCopy == null || !bgCopy)
			{
				EditorManager.inst.DisplayNotification("No copied background yet!", 2f, EditorManager.NotificationType.Error);
				__result = "";
				return false;
			}

			var backgroundObject = DataManager.GameData.BackgroundObject.DeepCopy(__instance.backgroundObjCopy);
			DataManager.inst.gameData.backgroundObjects.Add(backgroundObject);

			var bg = Objects.BackgroundObject.DeepCopy(bgCopy);
			bg.bg = backgroundObject;
			Objects.backgroundObjects.Add(bg);

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

		public static Objects.BackgroundObject bgCopy;
	}
}
