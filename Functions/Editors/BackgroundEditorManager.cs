using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using LSFunctions;

using RTFunctions.Functions;
using RTFunctions.Functions.Components;
using RTFunctions.Functions.Data;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;

using EditorManagement.Functions;
using EditorManagement.Functions.Components;
using EditorManagement.Functions.Helpers;

namespace EditorManagement.Functions.Editors
{
    public class BackgroundEditorManager : MonoBehaviour
    {
        public static BackgroundEditorManager inst;

		public static BackgroundObject CurrentSelectedBG => BackgroundEditor.inst == null ? null : (BackgroundObject)DataManager.inst.gameData.backgroundObjects[BackgroundEditor.inst.currentObj];

		public static void Init(BackgroundEditor backgroundEditor) => backgroundEditor.gameObject.AddComponent<BackgroundEditorManager>();

		public GameObject shapeButtonCopy;

        void Awake()
        {
            inst = this;
        }

		public void OpenDialog(int index)
		{
			var __instance = BackgroundEditor.inst;

			EditorManager.inst.ClearDialogs(Array.Empty<EditorManager.EditorDialog.DialogType>());
			EditorManager.inst.SetDialogStatus("Background Editor", true, true);
			//__instance.left = __instance.dialog.Find("data/left/Object Scroll View/Viewport/Content");
			//__instance.right = __instance.dialog.Find("data/right");

			var backgroundObject = (BackgroundObject)DataManager.inst.gameData.backgroundObjects[index];

			__instance.left.Find("name/active").GetComponent<Toggle>().isOn = backgroundObject.active;
			__instance.left.Find("name/name").GetComponent<InputField>().text = backgroundObject.name;
			//__instance.left.Find("depth/layer").GetComponent<Slider>().value = (float)backgroundObject.layer;

			SetSingleInputFieldInt(__instance.left, "iterations/x", backgroundObject.depth);

			SetSingleInputField(__instance.left, "depth/x", backgroundObject.layer);

			SetSingleInputField(__instance.left, "zscale/x", backgroundObject.zscale);

			var fade = __instance.left.Find("fade").GetComponent<Toggle>();

			fade.interactable = false;
			fade.isOn = backgroundObject.drawFade;
			fade.interactable = true;

			SetVector2InputField(__instance.left, "position", backgroundObject.pos);

			SetVector2InputField(__instance.left, "scale", backgroundObject.scale);

			SetSingleInputField(__instance.left, "rotation/x", backgroundObject.rot, 15f, 3f);

			var rotSlider = __instance.left.Find("rotation/slider").GetComponent<Slider>();
			rotSlider.maxValue = 360f;
			rotSlider.minValue = -360f;
			rotSlider.value = backgroundObject.rot;

			// 3D Rotation
			SetVector2InputField(__instance.left, "depth-rotation", backgroundObject.rotation, 15f, 3f);

			try
			{
				__instance.left.Find("reactive-ranges").GetChild(!backgroundObject.reactive ? (int)(backgroundObject.reactiveType + 1) : 0).GetComponent<Toggle>().isOn = true;
			}
			catch
			{
				__instance.left.Find("reactive-ranges").GetChild(0).GetComponent<Toggle>().isOn = true;
				Debug.LogError($"{EditorPlugin.className}Custom Reactive not implemented.");
			}

			__instance.left.Find("reactive/x").GetComponent<InputField>().text = backgroundObject.reactiveScale.ToString("f2");
			__instance.left.Find("reactive/slider").GetComponent<Slider>().value = backgroundObject.reactiveScale;

			LSHelpers.DeleteChildren(__instance.left.Find("color"));
			LSHelpers.DeleteChildren(__instance.left.Find("fade-color"));
			LSHelpers.DeleteChildren(__instance.left.Find("reactive-color"));

			int num = 0;
			foreach (var col in GameManager.inst.LiveTheme.backgroundColors)
			{
				int colTmp = num;
				// Top Color
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

					gameObject.GetComponent<Button>().onClick.AddListener(delegate ()
					{
						__instance.SetColor(colTmp);
					});
				}

				// Fade Color
				{
					var gameObject = Instantiate(EditorManager.inst.colorGUI, Vector3.zero, Quaternion.identity);
					gameObject.name = "color gui";
					gameObject.transform.SetParent(__instance.left.Find("fade-color"));
					gameObject.transform.localScale = Vector3.one;
					gameObject.GetComponent<Image>().color = LSColors.fadeColor(col, 1f);
					gameObject.transform.Find("Image").gameObject.SetActive(false);

					if (backgroundObject.FadeColor == num)
					{
						gameObject.transform.Find("Image").gameObject.SetActive(true);
					}

					gameObject.GetComponent<Button>().onClick.AddListener(delegate ()
					{
						SetColor(__instance, colTmp);
					});
				}

				// Reactive Color
				{
					var gameObject = Instantiate(EditorManager.inst.colorGUI, Vector3.zero, Quaternion.identity);
					gameObject.name = "color gui";
					gameObject.transform.SetParent(__instance.left.Find("reactive-color"));
					gameObject.transform.localScale = Vector3.one;
					gameObject.GetComponent<Image>().color = LSColors.fadeColor(col, 1f);
					gameObject.transform.Find("Image").gameObject.SetActive(false);

					if (backgroundObject.reactiveCol == num)
					{
						gameObject.transform.Find("Image").gameObject.SetActive(true);
					}

					gameObject.GetComponent<Button>().onClick.AddListener(delegate ()
					{
						SetColorReactive(__instance, colTmp);
					});
				}
				num++;
			}

			SetShape(backgroundObject, index);

			// Reactive Position Samples
			SetVector2InputFieldInt(__instance.left, "reactive-position-samples", backgroundObject.reactivePosSamples);

			// Reactive Position Intensity
			SetVector2InputField(__instance.left, "reactive-position-intensity", backgroundObject.reactivePosIntensity);

			// Reactive Scale Samples
			SetVector2InputFieldInt(__instance.left, "reactive-scale-samples", backgroundObject.reactiveScaSamples);

			// Reactive Scale Intensity
			SetVector2InputField(__instance.left, "reactive-scale-intensity", backgroundObject.reactiveScaIntensity);

			// Reactive Rotation Samples
			SetSingleInputFieldInt(__instance.left, "reactive-rotation-sample/x", backgroundObject.reactiveRotSample);

			// Reactive Rotation Intensity
			SetSingleInputField(__instance.left, "reactive-rotation-intensity/x", backgroundObject.reactiveRotIntensity);

			// Reactive Color Samples
			SetSingleInputFieldInt(__instance.left, "reactive-color-sample/x", backgroundObject.reactiveColSample);

			// Reactive Color Intensity
			SetSingleInputField(__instance.left, "reactive-color-intensity/x", backgroundObject.reactiveColIntensity);

			// Reactive Z Samples
			SetSingleInputFieldInt(__instance.left, "reactive-z-sample/x", backgroundObject.reactiveZSample);

			// Reactive Z Intensity
			SetSingleInputField(__instance.left, "reactive-z-intensity/x", backgroundObject.reactiveZIntensity);

			__instance.UpdateBackgroundList();
			__instance.dialog.gameObject.SetActive(true);
		}

		public void SetShape(BackgroundObject backgroundObject, int index)
        {
			var shape = BackgroundEditor.inst.left.Find("shape");
			var shapeSettings = BackgroundEditor.inst.left.Find("shapesettings");

			shape.GetComponent<GridLayoutGroup>().spacing = new Vector2(7.6f, 0f);

			DestroyImmediate(shape.GetComponent<ToggleGroup>());

			var toDestroy = new List<GameObject>();

			for (int i = 0; i < shape.childCount; i++)
			{
				toDestroy.Add(shape.GetChild(i).gameObject);
			}

			for (int i = 0; i < shapeSettings.childCount; i++)
			{
				if (i != 4 && i != 6)
					for (int j = 0; j < shapeSettings.GetChild(i).childCount; j++)
					{
						toDestroy.Add(shapeSettings.GetChild(i).GetChild(j).gameObject);
					}
			}

			//Debug.Log($"{ObjEditor.inst.className}Removing all...");
			foreach (var obj in toDestroy)
				DestroyImmediate(obj);

			toDestroy = null;

			// Re-add everything
			for (int i = 0; i < ShapeManager.inst.Shapes3D.Count; i++)
			{
				var obj = ObjectEditor.inst.shapeButtonPrefab.Duplicate(shape, (i + 1).ToString(), i);
				if (obj.transform.Find("Image") && obj.transform.Find("Image").gameObject.TryGetComponent(out Image image))
					image.sprite = ShapeManager.inst.Shapes3D[i][0].Icon;

				if (i != 4 && i != 6)
				{
					if (!shapeSettings.Find((i + 1).ToString()))
					{
						shapeSettings.Find("6").gameObject.Duplicate(shapeSettings, (i + 1).ToString());
					}

					var so = shapeSettings.Find((i + 1).ToString());

					var rect = (RectTransform)so;
					if (!so.GetComponent<ScrollRect>())
					{
						var scroll = so.gameObject.AddComponent<ScrollRect>();
						so.gameObject.AddComponent<Mask>();
						var ad = so.gameObject.AddComponent<Image>();

						scroll.horizontal = true;
						scroll.vertical = false;
						scroll.content = rect;
						scroll.viewport = rect;
						ad.color = new Color(1f, 1f, 1f, 0.01f);
					}

					for (int j = 0; j < ShapeManager.inst.Shapes3D[i].Count; j++)
					{
						var opt = ObjectEditor.inst.shapeButtonPrefab.Duplicate(shapeSettings.GetChild(i), (j + 1).ToString(), j);
						if (opt.transform.Find("Image") && opt.transform.Find("Image").gameObject.TryGetComponent(out Image image1))
							image1.sprite = ShapeManager.inst.Shapes3D[i][j].Icon;

						var layoutElement = opt.AddComponent<LayoutElement>();
						layoutElement.layoutPriority = 1;
						layoutElement.minWidth = 32f;

						((RectTransform)opt.transform).sizeDelta = new Vector2(32f, 32f);

						if (!opt.GetComponent<HoverUI>())
						{
							var he = opt.AddComponent<HoverUI>();
							he.animatePos = false;
							he.animateSca = true;
							he.size = 1.1f;
						}
					}

					ObjectEditor.inst.LastGameObject(shapeSettings.GetChild(i));
				}
			}

			LSHelpers.SetActiveChildren(shapeSettings, false);

			if (backgroundObject.shape.type >= shapeSettings.childCount)
			{
				Debug.Log($"{BackgroundEditor.inst.className}Somehow, the object ended up being at a higher shape than normal.");
				backgroundObject.SetShape(shapeSettings.childCount - 1 - 1, 0);

				BackgroundEditor.inst.OpenDialog(index);
			}

			if (backgroundObject.shape.type == 4)
			{
				Debug.Log($"{ObjEditor.inst.className}Shape is text, so we make the size larger for better readability.");
				shapeSettings.GetComponent<RectTransform>().sizeDelta = new Vector2(351f, 74f);
				var child = shapeSettings.GetChild(4);
				child.GetComponent<RectTransform>().sizeDelta = new Vector2(351f, 74f);
				child.Find("Text").GetComponent<Text>().alignment = TextAnchor.UpperLeft;
				child.Find("Placeholder").GetComponent<Text>().alignment = TextAnchor.UpperLeft;
				child.GetComponent<InputField>().lineType = InputField.LineType.MultiLineNewline;
			}
			else
			{
				Debug.Log($"{ObjEditor.inst.className}Shape is not text so we reset size.");
				shapeSettings.GetComponent<RectTransform>().sizeDelta = new Vector2(351f, 32f);
				shapeSettings.GetChild(4).GetComponent<RectTransform>().sizeDelta = new Vector2(351f, 32f);
			}

			Debug.Log($"{ObjEditor.inst.className}Set the shape option as active.");
			shapeSettings.GetChild(backgroundObject.shape.type).gameObject.SetActive(true);
			for (int i = 1; i <= ShapeManager.inst.Shapes3D.Count; i++)
			{
				int buttonTmp = i - 1;

				if (shape.Find(i.ToString()))
				{
					var shoggle = shape.Find(i.ToString()).GetComponent<Toggle>();
					shoggle.onValueChanged.ClearAll();
					shoggle.isOn = backgroundObject.shape.type == buttonTmp;
					shoggle.onValueChanged.AddListener(delegate (bool _value)
					{
						if (_value)
						{
							backgroundObject.SetShape(buttonTmp, 0);

							BackgroundEditor.inst.OpenDialog(index);
						}
					});

					if (!shape.Find(i.ToString()).GetComponent<HoverUI>())
					{
						var hoverUI = shape.Find(i.ToString()).gameObject.AddComponent<HoverUI>();
						hoverUI.animatePos = false;
						hoverUI.animateSca = true;
						hoverUI.size = 1.1f;
					}
				}
			}

			if (backgroundObject.shape.type != 4 && backgroundObject.shape.type != 6)
			{
				for (int i = 0; i < shapeSettings.GetChild(backgroundObject.shape.type).childCount - 1; i++)
				{
					int buttonTmp = i;
					var shoggle = shapeSettings.GetChild(backgroundObject.shape.type).GetChild(i).GetComponent<Toggle>();

					shoggle.onValueChanged.RemoveAllListeners();
					shoggle.isOn = backgroundObject.shape.option == i;
					shoggle.onValueChanged.AddListener(delegate (bool _value)
					{
						if (_value)
						{
							backgroundObject.SetShape(backgroundObject.shape.type, buttonTmp);

							BackgroundEditor.inst.OpenDialog(index);
						}
					});
				}
			}
			else if (backgroundObject.shape.type == 4)
			{
				EditorManager.inst.DisplayNotification("Text background not supported.", 2f, EditorManager.NotificationType.Error);
				backgroundObject.SetShape(0, 0);
				//var textIF = shapeSettings.Find("5").GetComponent<InputField>();
				//textIF.onValueChanged.ClearAll();
				//textIF.text = backgroundObject.text;
				//textIF.onValueChanged.AddListener(delegate (string _value)
				//{
				//	backgroundObject.text = _value;

				//	BackgroundManager.inst.UpdateBackgrounds();
				//});
			}
			else if (backgroundObject.shape.type == 6)
			{
				EditorManager.inst.DisplayNotification("Image background not supported.", 2f, EditorManager.NotificationType.Error);
				backgroundObject.SetShape(0, 0);
				//var select = shapeSettings.Find("7/select").GetComponent<Button>();
				//select.onClick.RemoveAllListeners();
				//select.onClick.AddListener(delegate ()
				//{
				//	OpenImageSelector(beatmapObject);
				//});
				//shapeSettings.Find("7/text").GetComponent<Text>().text = string.IsNullOrEmpty(backgroundObject.text) ? "No image selected" : backgroundObject.text;
			}
		}

		void SetSingleInputField(Transform dialogTmp, string name, float value, float amount = 0.1f, float multiply = 10f)
        {
			var reactiveX = dialogTmp.Find(name).GetComponent<InputField>();
			reactiveX.text = value.ToString();

			if (!reactiveX.GetComponent<EventTrigger>())
			{
				var etX = reactiveX.gameObject.AddComponent<EventTrigger>();

				etX.triggers.Add(TriggerHelper.ScrollDelta(reactiveX, amount, multiply));
			}
		}
		
		void SetSingleInputFieldInt(Transform dialogTmp, string name, int value)
        {
			var reactiveX = dialogTmp.Find(name).GetComponent<InputField>();
			reactiveX.text = value.ToString();

			if (!reactiveX.GetComponent<EventTrigger>())
			{
				var etX = reactiveX.gameObject.AddComponent<EventTrigger>();

				etX.triggers.Add(TriggerHelper.ScrollDeltaInt(reactiveX, 1));
			}
		}
		
		void SetVector2InputField(Transform dialogTmp, string name, Vector2 value, float amount = 0.1f, float multiply = 10f)
        {
			var reactiveX = dialogTmp.Find($"{name}/x").GetComponent<InputField>();
			reactiveX.text = value.x.ToString();

			var reactiveY = dialogTmp.Find($"{name}/y").GetComponent<InputField>();
			reactiveY.text = value.y.ToString();

			if (!reactiveX.GetComponent<EventTrigger>())
			{
				var etX = reactiveX.gameObject.AddComponent<EventTrigger>();

				etX.triggers.Add(TriggerHelper.ScrollDelta(reactiveX, amount, multiply, multi: true));
				etX.triggers.Add(TriggerHelper.ScrollDeltaVector2(reactiveX, reactiveY, amount, multiply));
			}

			if (!reactiveY.GetComponent<EventTrigger>())
			{
				var etY = reactiveY.gameObject.AddComponent<EventTrigger>();

				etY.triggers.Add(TriggerHelper.ScrollDelta(reactiveY, amount, multiply, multi: true));
				etY.triggers.Add(TriggerHelper.ScrollDelta(reactiveY, amount, multiply, multi: true));
				etY.triggers.Add(TriggerHelper.ScrollDeltaVector2(reactiveX, reactiveY, amount, multiply));
			}

		}

		void SetVector2InputFieldInt(Transform dialogTmp, string name, Vector2 value)
        {
			var reactiveX = dialogTmp.Find($"{name}/x").GetComponent<InputField>();
			reactiveX.text = value.x.ToString();

			if (!reactiveX.GetComponent<EventTrigger>())
			{
				var etX = reactiveX.gameObject.AddComponent<EventTrigger>();

				etX.triggers.Add(TriggerHelper.ScrollDeltaInt(reactiveX, 1));
			}

			var reactiveY = dialogTmp.Find($"{name}/y").GetComponent<InputField>();
			reactiveY.text = value.y.ToString();

			if (!reactiveY.GetComponent<EventTrigger>())
			{
				var etX = reactiveY.gameObject.AddComponent<EventTrigger>();

				etX.triggers.Add(TriggerHelper.ScrollDeltaInt(reactiveY, 1));
			}
		}

		public void SetColor(BackgroundEditor __instance, int _col)
		{
			CurrentSelectedBG.FadeColor = _col;
			__instance.UpdateBackground(__instance.currentObj);
			UpdateColorList("fade-color");
		}

		public void SetColorReactive(BackgroundEditor __instance, int _col)
		{
			CurrentSelectedBG.reactiveCol = _col;
			__instance.UpdateBackground(__instance.currentObj);
			UpdateColorList("reactive-color");
		}

		void UpdateColorList(string name)
        {
			var bg = CurrentSelectedBG;
			var colorList = BackgroundEditor.inst.left.Find(name);

			for (int i = 0; i < GameManager.inst.LiveTheme.backgroundColors.Count; i++)
				if (colorList.childCount > i)
					colorList.GetChild(i).Find("Image").gameObject.SetActive(bg.reactiveCol == i);
        }

		public void CreateBackgrounds(int _amount)
		{
			int number = Mathf.Clamp(_amount, 0, 100);

			for (int i = 0; i < number; i++)
			{
				var backgroundObject = new BackgroundObject();
				backgroundObject.name = "bg - " + i;

				float num = UnityEngine.Random.Range(2, 6);
				backgroundObject.scale = UnityEngine.Random.value > 0.5f ? new Vector2((float)UnityEngine.Random.Range(2, 8), (float)UnityEngine.Random.Range(2, 8)) : new Vector2(num, num);

				backgroundObject.pos = new Vector2((float)UnityEngine.Random.Range(-48, 48), (float)UnityEngine.Random.Range(-32, 32));
				backgroundObject.color = UnityEngine.Random.Range(1, 6);
				backgroundObject.layer = UnityEngine.Random.Range(0, 6);
				backgroundObject.reactive = (UnityEngine.Random.value > 0.5f);

				if (backgroundObject.reactive)
				{
					backgroundObject.reactiveType = (DataManager.GameData.BackgroundObject.ReactiveType)UnityEngine.Random.Range(0, 4);

					backgroundObject.reactiveScale = UnityEngine.Random.Range(0.01f, 0.04f);
				}

				backgroundObject.reactivePosIntensity = new Vector2(UnityEngine.Random.Range(0, 100) > 65 ? UnityEngine.Random.Range(0f, 1f) : 0f, UnityEngine.Random.Range(0, 100) > 65 ? UnityEngine.Random.Range(0f, 1f) : 0f);
				backgroundObject.reactiveScaIntensity = new Vector2(UnityEngine.Random.Range(0, 100) > 45 ? UnityEngine.Random.Range(0f, 1f) : 0f, UnityEngine.Random.Range(0, 100) > 45 ? UnityEngine.Random.Range(0f, 1f) : 0f);
				backgroundObject.reactiveRotIntensity = UnityEngine.Random.Range(0, 100) > 45 ? UnityEngine.Random.Range(0f, 1f) : 0f;
				backgroundObject.reactiveCol = UnityEngine.Random.Range(1, 6);
				backgroundObject.shape = ShapeManager.Shapes[UnityEngine.Random.Range(0, ShapeManager.Shapes.Count - 1)];

				DataManager.inst.gameData.backgroundObjects.Add(backgroundObject);
			}

			BackgroundManager.inst.UpdateBackgrounds();
			BackgroundEditor.inst.UpdateBackgroundList();
		}

		public void DeleteAllBackgrounds()
		{
			int num = DataManager.inst.gameData.backgroundObjects.Count;

			for (int i = 1; i < num; i++)
			{
				int nooo = Mathf.Clamp(i, 1, DataManager.inst.gameData.backgroundObjects.Count - 1);
				DataManager.inst.gameData.backgroundObjects.RemoveAt(nooo);
			}

			BackgroundEditor.inst.SetCurrentBackground(0);
			BackgroundManager.inst.UpdateBackgrounds();
			BackgroundEditor.inst.UpdateBackgroundList();

			EditorManager.inst.DisplayNotification("Deleted " + (num - 1).ToString() + " backgrounds!", 2f, EditorManager.NotificationType.Success);
		}

	}
}
