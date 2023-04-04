using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using SimpleJSON;

using HarmonyLib;

using LSFunctions;

using EditorManagement.Functions;
using EditorManagement.Functions.Tools;

namespace EditorManagement.Patchers
{
	[HarmonyPatch(typeof(ObjEditor))]
    public class ObjEditorPatch : MonoBehaviour
    {
		public static MethodInfo timeCalcObj;
		public static MethodInfo posCalcObj;

		public static float timeCalc()
        {
			return (float)timeCalcObj.Invoke(ObjEditor.inst, new object[] { });
        }

		public static float posCalc(float _time)
		{
			return (float)posCalcObj.Invoke(ObjEditor.inst, new object[] { _time });
		}

		[HarmonyPatch("Awake")]
		[HarmonyPostfix]
		private static void CreateLayers()
		{
			if (ObjEditor.inst.ObjectView.transform.Find("spacer"))
			{
				ObjEditor.inst.ObjectView.transform.GetChild(17).GetChild(1).gameObject.SetActive(true);
			}
			else
			{
				ObjEditor.inst.ObjectView.transform.GetChild(16).GetChild(1).gameObject.SetActive(true);
			}
			ObjEditor.inst.ObjectView.transform.Find("editor/bin").gameObject.SetActive(true);

			ObjEditor.inst.ObjectView.transform.Find("editor/layer").gameObject.SetActive(false);

			GameObject tbarLayers = Instantiate(ObjEditor.inst.ObjectView.transform.Find("time/time").gameObject);

			tbarLayers.transform.SetParent(ObjEditor.inst.ObjectView.transform.Find("editor"));
			tbarLayers.name = "layers";
			tbarLayers.transform.SetSiblingIndex(0);
			RectTransform tbarLayersRT = tbarLayers.GetComponent<RectTransform>();
			InputField tbarLayersIF = tbarLayers.GetComponent<InputField>();
			Image layerImage = tbarLayers.GetComponent<Image>();

			tbarLayersIF.characterValidation = InputField.CharacterValidation.Integer;

			HorizontalLayoutGroup edhlg = ObjEditor.inst.ObjectView.transform.Find("editor").GetComponent<HorizontalLayoutGroup>();
			edhlg.childControlWidth = false;
			edhlg.childForceExpandWidth = false;

			tbarLayersRT.sizeDelta = new Vector2(100f, 32f);
			ObjEditor.inst.ObjectView.transform.Find("editor/bin").GetComponent<RectTransform>().sizeDelta = new Vector2(237f, 32f);

			GameObject close = GameObject.Find("Editor Systems/Editor GUI/sizer/main/Popups/Open File Popup/Panel/x");

			GameObject parent = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/parent");

			parent.GetComponent<HorizontalLayoutGroup>().childControlWidth = false;
			parent.GetComponent<HorizontalLayoutGroup>().spacing = 4f;

			parent.transform.Find("text").GetComponent<RectTransform>().sizeDelta = new Vector2(241f, 32f);

			GameObject resetParent = Instantiate(close);
			resetParent.transform.SetParent(parent.transform);
			resetParent.transform.localScale = Vector3.one;
			resetParent.name = "clear parent";
			resetParent.transform.SetSiblingIndex(1);

			resetParent.GetComponent<Button>().onClick.RemoveAllListeners();
			resetParent.GetComponent<Button>().onClick.AddListener(delegate ()
			{
				ObjEditor.inst.currentObjectSelection.GetObjectData().parent = "";
				var objEditor = ObjEditor.inst;
				var refreshParentGUI = objEditor.GetType().GetMethod("RefreshParentGUI", BindingFlags.NonPublic | BindingFlags.Instance);

				refreshParentGUI.Invoke(objEditor, new object[] { "" });
				ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
			});

			parent.transform.Find("parent").GetComponent<RectTransform>().sizeDelta = new Vector2(32f, 32f);
			parent.transform.Find("more").GetComponent<RectTransform>().sizeDelta = new Vector2(32f, 32f);

			ObjEditor.inst.SelectedColor = EditorPlugin.ObjSelCol.Value;

            //Methods
            {
				timeCalcObj = AccessTools.Method(typeof(ObjEditor), "timeCalc");
				posCalcObj = AccessTools.Method(typeof(ObjEditor), "posCalc");
			}
		}

		[HarmonyPatch("Awake")]
		[HarmonyPostfix]
		private static void CreateNewOriginText()
		{
			//Main parent
			Transform contentOriginTF = GameObject.Find("GameObjectDialog/data/left/Scroll View/Viewport/Content/origin").transform;
			Text textFont = GameObject.Find("GameObjectDialog/data/left/Scroll View/Viewport/Content/name/name/Text").GetComponent<Text>();

			contentOriginTF.Find("origin-x").gameObject.SetActive(false);
			contentOriginTF.Find("origin-y").gameObject.SetActive(false);

			//Text input
			GameObject oxTxt = new GameObject("originxtext")
			{
				transform =
				{
					parent = contentOriginTF.transform
				}
			};
			oxTxt.transform.SetSiblingIndex(0);
			RectTransform rToxTxt = oxTxt.AddComponent<RectTransform>();
			CanvasRenderer cRoxTxt = oxTxt.AddComponent<CanvasRenderer>();
			Image ioxTxt = oxTxt.AddComponent<Image>();
			InputField iFoxTxt = oxTxt.AddComponent<InputField>();
			LayoutElement lEoxTxt = oxTxt.AddComponent<LayoutElement>();

			oxTxt.GetComponent<Graphic>().color = new Color(0.9333f, 0.9176f, 0.9333f, 1f);
			rToxTxt.anchoredPosition = new Vector2(-100f, 0f);
			rToxTxt.sizeDelta = new Vector2(150f, 32f);
			lEoxTxt.ignoreLayout = true;
			oxTxt.layer = 5;

			//Text caret
			GameObject oxTxtIC = new GameObject("originx Input Caret");
			oxTxtIC.transform.parent = oxTxt.transform;
			RectTransform rToxTxtIC = oxTxtIC.AddComponent<RectTransform>();
			CanvasRenderer cRoxTxtIC = oxTxtIC.AddComponent<CanvasRenderer>();
			LayoutElement lEoxTxtIC = oxTxtIC.AddComponent<LayoutElement>();

			rToxTxtIC.anchoredPosition = new Vector2(2f, 0f);
			rToxTxtIC.anchorMax = new Vector2(1f, 1f);
			rToxTxtIC.anchorMin = new Vector2(0f, 0f);
			rToxTxtIC.offsetMax = new Vector2(-4f, -4f);
			rToxTxtIC.offsetMin = new Vector2(8f, 4f);
			rToxTxtIC.sizeDelta = new Vector2(-12f, -8f);
			lEoxTxtIC.ignoreLayout = true;
			oxTxtIC.hideFlags = HideFlags.DontSave;
			oxTxtIC.layer = 5;

			//Text placeholder
			GameObject oxTxtPH = new GameObject("Placeholder");
			oxTxtPH.transform.parent = oxTxt.transform;
			RectTransform rToxTxtPH = oxTxtPH.AddComponent<RectTransform>();
			CanvasRenderer cRoxTxtPH = oxTxtPH.AddComponent<CanvasRenderer>();
			Text toxTxtPH = oxTxtPH.AddComponent<Text>();

			rToxTxtPH.anchoredPosition = new Vector2(-6f, 0f);
			toxTxtPH.alignment = TextAnchor.MiddleLeft;
			toxTxtPH.font = textFont.font;
			toxTxtPH.fontSize = 20;
			toxTxtPH.fontStyle = FontStyle.Italic;
			toxTxtPH.horizontalOverflow = HorizontalWrapMode.Wrap;
			toxTxtPH.resizeTextMaxSize = 42;
			toxTxtPH.resizeTextMinSize = 2;
			toxTxtPH.text = "Set origin...";
			toxTxtPH.verticalOverflow = VerticalWrapMode.Overflow;
			oxTxtPH.GetComponent<Graphic>().color = new Color(0.1961f, 0.1961f, 0.1961f, 0.5f);

			oxTxtPH.layer = 5;

			//Text
			GameObject oxTxtTE = new GameObject("Text");
			oxTxtTE.transform.parent = oxTxt.transform;
			RectTransform rToxTxtTE = oxTxtTE.AddComponent<RectTransform>();
			CanvasRenderer cRoxTxtTE = oxTxtTE.AddComponent<CanvasRenderer>();
			Text toxTxtTE = oxTxtTE.AddComponent<Text>();

			rToxTxtTE.anchoredPosition = new Vector2(-6f, 0f);
			toxTxtTE.alignment = TextAnchor.MiddleCenter;
			toxTxtTE.font = textFont.font;
			toxTxtTE.fontSize = 20;
			toxTxtTE.horizontalOverflow = HorizontalWrapMode.Overflow;
			toxTxtTE.resizeTextMaxSize = 42;
			toxTxtTE.resizeTextMinSize = 2;
			toxTxtTE.verticalOverflow = VerticalWrapMode.Overflow;
			oxTxtTE.GetComponent<Graphic>().color = new Color(0.1294f, 0.1294f, 0.1294f, 1);
			oxTxtTE.layer = 5;

			//InputField stuff
			iFoxTxt.characterValidation = InputField.CharacterValidation.Decimal;
			iFoxTxt.caretBlinkRate = 0f;
			iFoxTxt.caretColor = new Color(0.1294f, 0.1294f, 0.1294f, 1f);
			iFoxTxt.caretPosition = 0;
			iFoxTxt.caretWidth = 3;
			iFoxTxt.characterLimit = 64;
			iFoxTxt.contentType = InputField.ContentType.DecimalNumber;
			iFoxTxt.keyboardType = TouchScreenKeyboardType.Default;
			iFoxTxt.textComponent = toxTxtTE;
			iFoxTxt.placeholder = toxTxtPH;
			iFoxTxt.selectionColor = new Color(0f, 0.711f, 0.8679f, 0.7529f);
			oxTxt.GetComponent<Selectable>().transition = Selectable.Transition.Animation;

			//Text input
			GameObject oyTxt = new GameObject("originytext")
			{
				transform =
				{
					parent = contentOriginTF.transform
				}
			};
			oyTxt.transform.SetSiblingIndex(1);
			RectTransform rToyTxt = oyTxt.AddComponent<RectTransform>();
			CanvasRenderer cRoyTxt = oyTxt.AddComponent<CanvasRenderer>();
			Image ioyTxt = oyTxt.AddComponent<Image>();
			InputField iFoyTxt = oyTxt.AddComponent<InputField>();
			LayoutElement lEoyTxt = oyTxt.AddComponent<LayoutElement>();

			oyTxt.GetComponent<Graphic>().color = new Color(0.9333f, 0.9176f, 0.9333f, 1f);
			rToyTxt.anchoredPosition = new Vector2(80f, 0f);
			rToyTxt.sizeDelta = new Vector2(150f, 32f);
			lEoyTxt.ignoreLayout = true;
			oyTxt.layer = 5;

			//Text caret
			GameObject oyTxtIC = new GameObject("originy Input Caret");
			oyTxtIC.transform.parent = oyTxt.transform;
			RectTransform rToyTxtIC = oyTxtIC.AddComponent<RectTransform>();
			CanvasRenderer cRoyTxtIC = oyTxtIC.AddComponent<CanvasRenderer>();
			LayoutElement lEoyTxtIC = oyTxtIC.AddComponent<LayoutElement>();

			rToyTxtIC.anchoredPosition = new Vector2(2f, 0f);
			rToyTxtIC.anchorMax = new Vector2(1f, 1f);
			rToyTxtIC.anchorMin = new Vector2(0f, 0f);
			rToyTxtIC.offsetMax = new Vector2(-4f, -4f);
			rToyTxtIC.offsetMin = new Vector2(8f, 4f);
			rToyTxtIC.sizeDelta = new Vector2(-12f, -8f);
			lEoyTxtIC.ignoreLayout = true;
			oyTxtIC.hideFlags = HideFlags.DontSave;
			oyTxtIC.layer = 5;

			//Text placeholder
			GameObject oyTxtPH = new GameObject("Placeholder");
			oyTxtPH.transform.parent = oyTxt.transform;
			RectTransform rToyTxtPH = oyTxtPH.AddComponent<RectTransform>();
			CanvasRenderer cRoyTxtPH = oyTxtPH.AddComponent<CanvasRenderer>();
			Text toyTxtPH = oyTxtPH.AddComponent<Text>();

			rToyTxtPH.anchoredPosition = new Vector2(-6f, 0f);
			toyTxtPH.alignment = TextAnchor.MiddleLeft;
			toyTxtPH.font = textFont.font;
			toyTxtPH.fontSize = 20;
			toyTxtPH.fontStyle = FontStyle.Italic;
			toyTxtPH.horizontalOverflow = HorizontalWrapMode.Wrap;
			toyTxtPH.resizeTextMaxSize = 42;
			toyTxtPH.resizeTextMinSize = 2;
			toyTxtPH.text = "Set origin...";
			toyTxtPH.verticalOverflow = VerticalWrapMode.Overflow;
			oyTxtPH.GetComponent<Graphic>().color = new Color(0.1961f, 0.1961f, 0.1961f, 0.5f);

			oyTxtPH.layer = 5;

			//Text
			GameObject oyTxtTE = new GameObject("Text");
			oyTxtTE.transform.parent = oyTxt.transform;
			RectTransform rToyTxtTE = oyTxtTE.AddComponent<RectTransform>();
			CanvasRenderer cRoyTxtTE = oyTxtTE.AddComponent<CanvasRenderer>();
			Text toyTxtTE = oyTxtTE.AddComponent<Text>();

			rToyTxtTE.anchoredPosition = new Vector2(-6f, 0f);
			toyTxtTE.alignment = TextAnchor.MiddleCenter;
			toyTxtTE.font = textFont.font;
			toyTxtTE.fontSize = 20;
			toyTxtTE.horizontalOverflow = HorizontalWrapMode.Overflow;
			toyTxtTE.resizeTextMaxSize = 42;
			toyTxtTE.resizeTextMinSize = 2;
			toyTxtTE.verticalOverflow = VerticalWrapMode.Overflow;
			oyTxtTE.GetComponent<Graphic>().color = new Color(0.1294f, 0.1294f, 0.1294f, 1);
			oyTxtTE.layer = 5;

			//InputField stuff
			iFoyTxt.characterValidation = InputField.CharacterValidation.Decimal;
			iFoyTxt.caretBlinkRate = 0f;
			iFoyTxt.caretColor = new Color(0.1294f, 0.1294f, 0.1294f, 1f);
			iFoyTxt.caretPosition = 0;
			iFoyTxt.caretWidth = 3;
			iFoyTxt.characterLimit = 64;
			iFoyTxt.contentType = InputField.ContentType.DecimalNumber;
			iFoyTxt.keyboardType = TouchScreenKeyboardType.Default;
			iFoyTxt.textComponent = toyTxtTE;
			iFoyTxt.placeholder = toyTxtPH;
			iFoyTxt.selectionColor = new Color(0f, 0.711f, 0.8679f, 0.7529f);
			oyTxt.GetComponent<Selectable>().transition = Selectable.Transition.Animation;

			EventTrigger eToxTxt = oxTxt.AddComponent<EventTrigger>();

			EventTrigger.Entry entryOriginX = new EventTrigger.Entry();
			entryOriginX.eventID = EventTriggerType.Scroll;
			entryOriginX.callback.AddListener(delegate (BaseEventData eventDataX)
			{
				PointerEventData pointerEventData = (PointerEventData)eventDataX;
				if (InputDataManager.inst.editorActions.MultiSelect.IsPressed)
				{
					if (pointerEventData.scrollDelta.y < 0f)
					{
						float originXLower = ObjEditor.inst.currentObjectSelection.GetObjectData().origin.x - EditorPlugin.OriginXAmount.Value;
						float originYLower = ObjEditor.inst.currentObjectSelection.GetObjectData().origin.y - EditorPlugin.OriginYAmount.Value;
						SetNewOriginX(originXLower.ToString());
						SetNewOriginY(originYLower.ToString());
						iFoxTxt.text = ObjEditor.inst.currentObjectSelection.GetObjectData().origin.x.ToString();
						iFoyTxt.text = ObjEditor.inst.currentObjectSelection.GetObjectData().origin.y.ToString();
						return;
					}
					if (pointerEventData.scrollDelta.y > 0f)
					{
						float originXHigher = ObjEditor.inst.currentObjectSelection.GetObjectData().origin.x + EditorPlugin.OriginXAmount.Value;
						float originYHigher = ObjEditor.inst.currentObjectSelection.GetObjectData().origin.y + EditorPlugin.OriginYAmount.Value;
						SetNewOriginX(originXHigher.ToString());
						SetNewOriginY(originYHigher.ToString());
						iFoxTxt.text = ObjEditor.inst.currentObjectSelection.GetObjectData().origin.x.ToString();
						iFoyTxt.text = ObjEditor.inst.currentObjectSelection.GetObjectData().origin.y.ToString();
						return;
					}
				}
				else
				{
					if (pointerEventData.scrollDelta.y < 0f)
					{
						float originXLower = ObjEditor.inst.currentObjectSelection.GetObjectData().origin.x - EditorPlugin.OriginXAmount.Value;
						SetNewOriginX(originXLower.ToString());
						iFoxTxt.text = ObjEditor.inst.currentObjectSelection.GetObjectData().origin.x.ToString();
						return;
					}
					if (pointerEventData.scrollDelta.y > 0f)
					{
						float originXLower = ObjEditor.inst.currentObjectSelection.GetObjectData().origin.x + EditorPlugin.OriginXAmount.Value;
						SetNewOriginX(originXLower.ToString());
						iFoxTxt.text = ObjEditor.inst.currentObjectSelection.GetObjectData().origin.x.ToString();
					}
				}
			});
			eToxTxt.triggers.Clear();
			eToxTxt.triggers.Add(entryOriginX);


			EventTrigger eToyTxt = oyTxt.AddComponent<EventTrigger>();

			EventTrigger.Entry entryOriginY = new EventTrigger.Entry();
			entryOriginY.eventID = EventTriggerType.Scroll;
			entryOriginY.callback.AddListener(delegate (BaseEventData eventDataY)
			{
				PointerEventData pointerEventData = (PointerEventData)eventDataY;
				if (InputDataManager.inst.editorActions.MultiSelect.IsPressed)
				{
					if (pointerEventData.scrollDelta.y < 0f)
					{
						float originXLower = ObjEditor.inst.currentObjectSelection.GetObjectData().origin.x - EditorPlugin.OriginXAmount.Value;
						float originYLower = ObjEditor.inst.currentObjectSelection.GetObjectData().origin.y - EditorPlugin.OriginYAmount.Value;
						SetNewOriginX(originXLower.ToString());
						SetNewOriginY(originYLower.ToString());
						iFoxTxt.text = ObjEditor.inst.currentObjectSelection.GetObjectData().origin.x.ToString();
						iFoyTxt.text = ObjEditor.inst.currentObjectSelection.GetObjectData().origin.y.ToString();
						return;
					}
					if (pointerEventData.scrollDelta.y > 0f)
					{
						float originXHigher = ObjEditor.inst.currentObjectSelection.GetObjectData().origin.x + EditorPlugin.OriginXAmount.Value;
						float originYHigher = ObjEditor.inst.currentObjectSelection.GetObjectData().origin.y + EditorPlugin.OriginYAmount.Value;
						SetNewOriginX(originXHigher.ToString());
						SetNewOriginY(originYHigher.ToString());
						iFoxTxt.text = ObjEditor.inst.currentObjectSelection.GetObjectData().origin.x.ToString();
						iFoyTxt.text = ObjEditor.inst.currentObjectSelection.GetObjectData().origin.y.ToString();
						return;
					}
				}
				else
				{
					if (pointerEventData.scrollDelta.y < 0f)
					{
						float originYLower = ObjEditor.inst.currentObjectSelection.GetObjectData().origin.y - EditorPlugin.OriginYAmount.Value;
						SetNewOriginY(originYLower.ToString());
						iFoyTxt.text = ObjEditor.inst.currentObjectSelection.GetObjectData().origin.y.ToString();
						return;
					}
					if (pointerEventData.scrollDelta.y > 0f)
					{
						float originYLower = ObjEditor.inst.currentObjectSelection.GetObjectData().origin.y + EditorPlugin.OriginYAmount.Value;
						SetNewOriginY(originYLower.ToString());
						iFoyTxt.text = ObjEditor.inst.currentObjectSelection.GetObjectData().origin.y.ToString();
					}
				}
			});
			eToyTxt.triggers.Clear();
			eToyTxt.triggers.Add(entryOriginY);
		}

		[HarmonyPatch("OpenDialog")]
		[HarmonyPostfix]
		private static void OpenD()
		{
			if (ObjEditor.inst.currentObjectSelection.IsObject())
			{
				GameObject tbarLayers = ObjEditor.inst.ObjectView.transform.Find("editor/layers").gameObject;
				InputField tbarLayersIF = tbarLayers.GetComponent<InputField>();
				Image layerImage = tbarLayers.GetComponent<Image>();

				if (ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer < 5)
				{
					float l = ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer + 1;
					tbarLayersIF.text = l.ToString();
				}
				else
				{
					int l = ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer;
					tbarLayersIF.text = l.ToString();
				}

				if (ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer < EditorManager.inst.layerColors.Count)
				{
					layerImage.color = EditorManager.inst.layerColors[ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer];
				}
				if (ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer > 6)
				{
					layerImage.color = Color.white;
				}

				tbarLayersIF.onValueChanged.RemoveAllListeners();
				tbarLayersIF.onValueChanged.AddListener(delegate (string _value)
				{
					if (int.Parse(_value) > 0)
					{
						if (int.Parse(_value) < 6)
						{
							ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer = int.Parse(_value) - 1;
							ObjEditor.inst.RenderTimelineObject(ObjEditor.inst.currentObjectSelection);
						}
						else
						{
							ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer = int.Parse(_value);
							ObjEditor.inst.RenderTimelineObject(ObjEditor.inst.currentObjectSelection);
						}
					}
					else
					{
						ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer = 0;
						ObjEditor.inst.RenderTimelineObject(ObjEditor.inst.currentObjectSelection);
						tbarLayersIF.text = "1";
					}

					if (ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer < EditorManager.inst.layerColors.Count)
					{
						layerImage.color = EditorManager.inst.layerColors[ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer];
					}
					if (ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer > 6)
					{
						layerImage.color = Color.white;
					}
				});
			}
		}

		[HarmonyPatch("OpenDialog")]
		[HarmonyPostfix]
		private static void SetOriginVal()
		{
			//Origin X
			InputField iFoxTxt = GameObject.Find("origin/originxtext").GetComponent<InputField>();
			iFoxTxt.text = ObjEditor.inst.currentObjectSelection.GetObjectData().origin.x.ToString();

			iFoxTxt.onValueChanged.AddListener(delegate (string _value)
			{
				SetNewOriginX(_value);
			});

			//Origin Y
			InputField iFoyTxt = GameObject.Find("origin/originytext").GetComponent<InputField>();
			iFoyTxt.text = ObjEditor.inst.currentObjectSelection.GetObjectData().origin.y.ToString();

			iFoyTxt.onValueChanged.AddListener(delegate (string _value)
			{
				SetNewOriginY(_value);
			});
		}

		[HarmonyPatch("RefreshKeyframeGUI")]
		[HarmonyPostfix]
		private static void RefreshOriginValue()
		{
			if (DataManager.inst.gameData.beatmapObjects.Count > 0 && !string.IsNullOrEmpty(ObjEditor.inst.currentObjectSelection.ID) && ObjEditor.inst.currentObjectSelection.IsObject() && ObjEditor.inst.keyframeSelections.Count <= 1 && ObjEditor.inst.selectedObjects.Count < 2 && EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Object))
			{
				//Origin X
				InputField iFoxTxt = GameObject.Find("origin/originxtext").GetComponent<InputField>();

				string originxstr = ObjEditor.inst.currentObjectSelection.GetObjectData().origin.x.ToString();

				iFoxTxt.onValueChanged.RemoveAllListeners();
				iFoxTxt.text = originxstr;

				iFoxTxt.onValueChanged.AddListener(delegate (string _value)
				{
					SetNewOriginX(_value);
				});

				//Origin Y
				InputField iFoyTxt = GameObject.Find("origin/originytext").GetComponent<InputField>();

				string originystr = ObjEditor.inst.currentObjectSelection.GetObjectData().origin.y.ToString();

				iFoyTxt.onValueChanged.RemoveAllListeners();
				iFoyTxt.text = originystr;

				iFoyTxt.onValueChanged.AddListener(delegate (string _value)
				{
					SetNewOriginY(_value);
				});
			}
		}

		public static void SetNewOriginX(string _originX)
		{
			DataManager.inst.gameData.beatmapObjects[ObjEditor.inst.currentObjectSelection.Index].origin.x = float.Parse(_originX);
			ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection, false);
			ObjEditor.inst.RenderTimelineObject(ObjEditor.inst.currentObjectSelection);
		}

		public static void SetNewOriginY(string _originY)
		{
			DataManager.inst.gameData.beatmapObjects[ObjEditor.inst.currentObjectSelection.Index].origin.y = float.Parse(_originY);
			ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection, false);
			ObjEditor.inst.RenderTimelineObject(ObjEditor.inst.currentObjectSelection);
		}

		[HarmonyPatch("Awake")]
		[HarmonyPostfix]
		private static void CreateNewDepthText()
		{
			//Main parent
			Transform contentDepthTF = GameObject.Find("GameObjectDialog/data/left/Scroll View/Viewport/Content/depth").transform;
			Text textFont = GameObject.Find("GameObjectDialog/data/left/Scroll View/Viewport/Content/name/name/Text").GetComponent<Text>();

			//Add spacer
			Transform contentParent = GameObject.Find("GameObjectDialog/data/left/Scroll View/Viewport/Content").transform;
			GameObject spacer = new GameObject("spacer");
			spacer.transform.parent = contentParent;
			spacer.transform.SetSiblingIndex(15);

			RectTransform spRT = spacer.AddComponent<RectTransform>();
			HorizontalLayoutGroup spHLG = spacer.AddComponent<HorizontalLayoutGroup>();

			spRT.sizeDelta = new Vector2(30f, 30f);
			spHLG.spacing = 8;

			//Text input
			GameObject dTxt = new GameObject("depthtext")
			{
				transform =
				{
					parent = spacer.transform
				}
			};
			dTxt.transform.SetSiblingIndex(0);
			RectTransform rTdTxt = dTxt.AddComponent<RectTransform>();
			CanvasRenderer cRdTxt = dTxt.AddComponent<CanvasRenderer>();
			Image idTxt = dTxt.AddComponent<Image>();
			InputField iFdTxt = dTxt.AddComponent<InputField>();
			LayoutElement lEdTxt = dTxt.AddComponent<LayoutElement>();
			EventTrigger depthTrigger = dTxt.AddComponent<EventTrigger>();

			dTxt.GetComponent<Graphic>().color = new Color(0.9333f, 0.9176f, 0.9333f, 1f);
			rTdTxt.anchoredPosition = new Vector2(-100f, 0f);
			rTdTxt.sizeDelta = new Vector2(150f, 32f);
			lEdTxt.ignoreLayout = true;
			dTxt.layer = 5;

			//Text caret
			GameObject dTxtIC = new GameObject("depth Input Caret");
			dTxtIC.transform.parent = dTxt.transform;
			RectTransform rTdTxtIC = dTxtIC.AddComponent<RectTransform>();
			CanvasRenderer cRdTxtIC = dTxtIC.AddComponent<CanvasRenderer>();
			LayoutElement lEdTxtIC = dTxtIC.AddComponent<LayoutElement>();

			rTdTxtIC.anchoredPosition = new Vector2(2f, 0f);
			rTdTxtIC.anchorMax = new Vector2(1f, 1f);
			rTdTxtIC.anchorMin = new Vector2(0f, 0f);
			rTdTxtIC.offsetMax = new Vector2(-4f, -4f);
			rTdTxtIC.offsetMin = new Vector2(8f, 4f);
			rTdTxtIC.sizeDelta = new Vector2(-12f, -8f);
			lEdTxtIC.ignoreLayout = true;
			dTxtIC.GetComponent<UnityEngine.Object>().hideFlags = HideFlags.DontSave;
			dTxtIC.layer = 5;

			//Text placeholder
			GameObject dTxtPH = new GameObject("Placeholder");
			dTxtPH.transform.parent = dTxt.transform;
			RectTransform rTdTxtPH = dTxtPH.AddComponent<RectTransform>();
			CanvasRenderer cRdTxtPH = dTxtPH.AddComponent<CanvasRenderer>();
			Text tdTxtPH = dTxtPH.AddComponent<Text>();

			rTdTxtPH.anchoredPosition = new Vector2(-6f, 0f);
			tdTxtPH.alignment = TextAnchor.MiddleLeft;
			tdTxtPH.font = textFont.font;
			tdTxtPH.fontSize = 20;
			tdTxtPH.fontStyle = FontStyle.Italic;
			tdTxtPH.horizontalOverflow = HorizontalWrapMode.Wrap;
			tdTxtPH.resizeTextMaxSize = 42;
			tdTxtPH.resizeTextMinSize = 2;
			tdTxtPH.text = "Set depth...";
			tdTxtPH.verticalOverflow = VerticalWrapMode.Overflow;
			dTxtPH.GetComponent<Graphic>().color = new Color(0.1961f, 0.1961f, 0.1961f, 0.5f);

			dTxtPH.layer = 5;

			//Text
			GameObject dTxtTE = new GameObject("Text");
			dTxtTE.transform.parent = dTxt.transform;
			RectTransform rTdTxtTE = dTxtTE.AddComponent<RectTransform>();
			CanvasRenderer cRdTxtTE = dTxtTE.AddComponent<CanvasRenderer>();
			Text tdTxtTE = dTxtTE.AddComponent<Text>();

			rTdTxtTE.anchoredPosition = new Vector2(-6f, 0f);
			tdTxtTE.alignment = TextAnchor.MiddleCenter;
			tdTxtTE.font = textFont.font;
			tdTxtTE.fontSize = 20;
			tdTxtTE.horizontalOverflow = HorizontalWrapMode.Overflow;
			tdTxtTE.resizeTextMaxSize = 42;
			tdTxtTE.resizeTextMinSize = 2;
			tdTxtTE.verticalOverflow = VerticalWrapMode.Overflow;
			dTxtTE.GetComponent<Graphic>().color = new Color(0.1294f, 0.1294f, 0.1294f, 1);
			dTxtTE.layer = 5;

			//InputField stuff
			iFdTxt.characterValidation = InputField.CharacterValidation.Integer;
			iFdTxt.caretBlinkRate = 0f;
			iFdTxt.caretColor = new Color(0.1294f, 0.1294f, 0.1294f, 1f);
			iFdTxt.caretPosition = 0;
			iFdTxt.caretWidth = 3;
			iFdTxt.characterLimit = 64;
			iFdTxt.contentType = InputField.ContentType.IntegerNumber;
			iFdTxt.keyboardType = TouchScreenKeyboardType.Default;
			iFdTxt.textComponent = tdTxtTE;
			iFdTxt.placeholder = tdTxtPH;
			iFdTxt.selectionColor = new Color(0f, 0.711f, 0.8679f, 0.7529f);
			dTxt.GetComponent<Selectable>().transition = Selectable.Transition.Animation;

			EventTrigger.Entry entryDepth = new EventTrigger.Entry();
			entryDepth.eventID = EventTriggerType.Scroll;
			entryDepth.callback.AddListener(delegate (BaseEventData eventData)
			{
				PointerEventData pointerEventData = (PointerEventData)eventData;
				if (pointerEventData.scrollDelta.y < 0f)
				{
					int depthLower = ObjEditor.inst.currentObjectSelection.GetObjectData().Depth - EditorPlugin.DepthAmount.Value;
					SetNewDepth(depthLower.ToString());
					iFdTxt.text = ObjEditor.inst.currentObjectSelection.GetObjectData().Depth.ToString();
					return;
				}
				if (pointerEventData.scrollDelta.y > 0f)
				{
					int depthHigher = ObjEditor.inst.currentObjectSelection.GetObjectData().Depth + EditorPlugin.DepthAmount.Value;
					SetNewDepth(depthHigher.ToString());
					iFdTxt.text = ObjEditor.inst.currentObjectSelection.GetObjectData().Depth.ToString();
				}
			});
			depthTrigger.triggers.Clear();
			depthTrigger.triggers.Add(entryDepth);

			//Add text
			Transform transform1 = contentDepthTF.Find("depth/Handle Slide Area/Handle").transform;
			GameObject gameObject = new GameObject("sliderdtext")
			{
				transform =
				{
					parent = transform1
				}
			};
			RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
			gameObject.AddComponent<CanvasRenderer>();
			Text textslider = gameObject.AddComponent<Text>();
			rectTransform.anchorMin = Vector2.zero;
			rectTransform.anchorMax = new Vector2(1f, 1f);
			rectTransform.anchoredPosition = Vector2.zero;
			rectTransform.offsetMax = new Vector2(-4f, -4f);
			rectTransform.offsetMin = new Vector2(4f, 4f);
			rectTransform.pivot = new Vector2(0.5f, 0.5f);
			rectTransform.sizeDelta = new Vector2(-8f, -8f);
			textslider.color = new Color(0.9f, 0.9f, 0.9f);
			textslider.fontSize = 20;
			textslider.alignment = TextAnchor.MiddleCenter;
			textslider.horizontalOverflow = HorizontalWrapMode.Overflow;
			textslider.verticalOverflow = VerticalWrapMode.Overflow;
			textslider.font = textFont.font;
			gameObject.layer = 5;

			//Button Refresh
			GameObject refreshObj = new GameObject("Refresh");
			refreshObj.transform.parent = spacer.transform;
			RectTransform rtrObj = refreshObj.AddComponent<RectTransform>();
			Image irObj = refreshObj.AddComponent<Image>();
			Button brObj = refreshObj.AddComponent<Button>();

			LayoutElement lerObj = refreshObj.AddComponent<LayoutElement>();

			refreshObj.AddComponent<CanvasRenderer>();

			rtrObj.sizeDelta = new Vector2(25f, 50f);
			rtrObj.anchoredPosition = Vector2.zero;

			irObj.sprite = ObjEditor.inst.KeyframeSprites[0];

			ColorBlock cb = brObj.colors;
			cb.normalColor = new Color(0.9608f, 0.9608f, 0.9608f, 1f);
			cb.pressedColor = new Color(0.7843f, 0.7843f, 0.7843f, 1f);
			cb.highlightedColor = new Color(0.898f, 0.451f, 0.451f, 1f);
			cb.disabledColor = new Color(0.7843f, 0.7843f, 0.7843f, 0.502f);
			cb.fadeDuration = 0.1f;
			brObj.colors = cb;

			lerObj.ignoreLayout = true;

			//Help
			GameObject textHelp = new GameObject("TextHelpName");
			textHelp.transform.parent = spacer.transform;
			RectTransform rttHelp = textHelp.AddComponent<RectTransform>();
			Text ttHelp = textHelp.AddComponent<Text>();
			LayoutElement letHelp = textHelp.AddComponent<LayoutElement>();

			rttHelp.anchoredPosition = new Vector2(95f, 2f);

			ttHelp.text = "Refresh Dialog";
			ttHelp.font = textFont.font;
			ttHelp.color = new Color(0.9f, 0.9f, 0.9f);
			ttHelp.alignment = TextAnchor.MiddleCenter;
			ttHelp.fontSize = 20;
			ttHelp.horizontalOverflow = HorizontalWrapMode.Overflow;

			letHelp.ignoreLayout = true;

			GameObject sliderObject = ObjEditor.inst.ObjectView.transform.Find("depth/depth").gameObject;
			Slider sliderComponent = sliderObject.GetComponent<Slider>();
			EventTrigger sliderEvent = sliderObject.AddComponent<EventTrigger>();

			EventTrigger.Entry entryDepth1 = new EventTrigger.Entry();
			entryDepth1.eventID = EventTriggerType.Scroll;
			entryDepth1.callback.AddListener(delegate (BaseEventData eventData1)
			{
				PointerEventData pointerEventData = (PointerEventData)eventData1;
				if (pointerEventData.scrollDelta.y < 0f)
				{
					int depthLower = ObjEditor.inst.currentObjectSelection.GetObjectData().Depth - EditorPlugin.DepthAmount.Value;
					SetNewDepth(depthLower.ToString());
					sliderComponent.value = ObjEditor.inst.currentObjectSelection.GetObjectData().Depth;
					return;
				}
				if (pointerEventData.scrollDelta.y > 0f)
				{
					int depthHigher = ObjEditor.inst.currentObjectSelection.GetObjectData().Depth + EditorPlugin.DepthAmount.Value;
					SetNewDepth(depthHigher.ToString());
					sliderComponent.value = ObjEditor.inst.currentObjectSelection.GetObjectData().Depth;
				}
			});
			sliderEvent.triggers.Clear();
			sliderEvent.triggers.Add(entryDepth1);
		}

		[HarmonyPatch("OpenDialog")]
		[HarmonyPostfix]
		private static void SetDepthVal()
		{
			//Create Local Variables
			InputField iFdTxt = GameObject.Find("spacer/depthtext").GetComponent<InputField>();
			Text textDepth = GameObject.Find("Handle/sliderdtext").GetComponent<Text>();
			iFdTxt.text = ObjEditor.inst.currentObjectSelection.GetObjectData().Depth.ToString();
			textDepth.text = ObjEditor.inst.currentObjectSelection.GetObjectData().Depth.ToString();

			Transform depth = ObjEditor.inst.ObjectView.transform.Find("depth");
			Slider depthSlider = depth.Find("depth").GetComponent<Slider>();

			iFdTxt.onValueChanged.AddListener(delegate (string _value)
			{
				SetNewDepth(_value);
				if (EditorPlugin.DepthUpdate.Value == true)
				{
					depthSlider.value = (float)ObjEditor.inst.currentObjectSelection.GetObjectData().Depth;
				}
				textDepth.text = ObjEditor.inst.currentObjectSelection.GetObjectData().Depth.ToString();
			});
			depth.Find("<").GetComponent<Button>().onClick.AddListener(delegate ()
			{
				iFdTxt.text = ObjEditor.inst.currentObjectSelection.GetObjectData().Depth.ToString();
				textDepth.text = ObjEditor.inst.currentObjectSelection.GetObjectData().Depth.ToString();
			});
			depth.Find(">").GetComponent<Button>().onClick.AddListener(delegate ()
			{
				iFdTxt.text = ObjEditor.inst.currentObjectSelection.GetObjectData().Depth.ToString();
				textDepth.text = ObjEditor.inst.currentObjectSelection.GetObjectData().Depth.ToString();
			});
			depthSlider.onValueChanged.AddListener(delegate (float _value)
			{
				iFdTxt.text = ObjEditor.inst.currentObjectSelection.GetObjectData().Depth.ToString();
				textDepth.text = ObjEditor.inst.currentObjectSelection.GetObjectData().Depth.ToString();
			});
			GameObject.Find("GameObjectDialog/data/left/Scroll View/Viewport/Content/spacer/Refresh").GetComponent<Button>().onClick.AddListener(delegate ()
			{
				ObjEditor.inst.OpenDialog();
			});
		}

		public static void SetNewDepth(string _depth)
		{
			DataManager.inst.gameData.beatmapObjects[ObjEditor.inst.currentObjectSelection.Index].Depth = int.Parse(_depth);
			ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection, false);
			ObjEditor.inst.RenderTimelineObject(ObjEditor.inst.currentObjectSelection);
		}

		[HarmonyPatch("Start")]
		[HarmonyPostfix]
		private static void SetSliderStart()
		{
			Transform sliderObject = ObjEditor.inst.ObjectView.transform.Find("depth/depth");
			Slider sliderComponent = sliderObject.gameObject.GetComponent<Slider>();
			ColorBlock cb = sliderComponent.colors;
			cb.normalColor = EditorPlugin.DepthNormalColor.Value;
			cb.pressedColor = EditorPlugin.DepthPressedColor.Value;
			cb.highlightedColor = EditorPlugin.DepthHighlightedColor.Value;
			cb.disabledColor = EditorPlugin.DepthDisabledColor.Value;
			cb.fadeDuration = EditorPlugin.DepthFadeDuration.Value;
			sliderComponent.colors = cb;
			sliderComponent.interactable = EditorPlugin.DepthInteractable.Value;
			sliderComponent.maxValue = EditorPlugin.SliderRMax.Value;
			sliderComponent.minValue = EditorPlugin.SliderRMin.Value;
			sliderComponent.direction = EditorPlugin.SliderDDirection.Value;
		}

		[HarmonyPatch("RefreshKeyframeGUI")]
		[HarmonyPostfix]
		private static void RefreshSliderValue()
		{
			if (DataManager.inst.gameData.beatmapObjects.Count > 0 && !string.IsNullOrEmpty(ObjEditor.inst.currentObjectSelection.ID) && ObjEditor.inst.currentObjectSelection.IsObject() && ObjEditor.inst.keyframeSelections.Count <= 1 && ObjEditor.inst.selectedObjects.Count < 2 && EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Object))
			{
				InputField iFdTxt = GameObject.Find("spacer/depthtext").GetComponent<InputField>();
				Text textDepth = GameObject.Find("Handle/sliderdtext").GetComponent<Text>();

				string depthstr = ObjEditor.inst.currentObjectSelection.GetObjectData().Depth.ToString();

				iFdTxt.onValueChanged.RemoveAllListeners();
				iFdTxt.text = depthstr;
				textDepth.text = depthstr;

				Transform depth = ObjEditor.inst.ObjectView.transform.Find("depth");
				Slider depthSlider = depth.Find("depth").GetComponent<Slider>();

				iFdTxt.onValueChanged.AddListener(delegate (string _value)
				{
					SetNewDepth(_value);
					if (EditorPlugin.DepthUpdate.Value == true)
					{
						depthSlider.value = ObjEditor.inst.currentObjectSelection.GetObjectData().Depth;
					}
					textDepth.text = depthstr;
				});

				depth.Find("<").GetComponent<Button>().onClick.AddListener(delegate ()
				{
					iFdTxt.text = depthstr;
					textDepth.text = depthstr;
				});
				depth.Find(">").GetComponent<Button>().onClick.AddListener(delegate ()
				{
					iFdTxt.text = depthstr;
					textDepth.text = depthstr;
				});
			}
		}

		[HarmonyPatch("RefreshKeyframeGUI")]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> TranspilerPatch(IEnumerable<CodeInstruction> instructions)
		{
			return new CodeMatcher(instructions)
				.Start()
			  .Advance(73)
			  .ThrowIfNotMatch("Is not empty", new CodeMatch(OpCodes.Ldarg_0))
			  .RemoveInstruction()
			  .Advance(0)
			  .ThrowIfNotMatch("Is not currentObjectSelection", new CodeMatch(OpCodes.Ldfld))
			  .Set(OpCodes.Ldc_I4_0, new object[] { })
			  .Advance(1)
			  .ThrowIfNotMatch("Is not Get ObjectData", new CodeMatch(OpCodes.Callvirt))
			  .RemoveInstructions(3)
			  .InstructionEnumeration();
		}

		[HarmonyPatch("RefreshKeyframeGUI")]
		[HarmonyPostfix]
		private static void RefreshTriggers()
		{
			if (DataManager.inst.gameData.beatmapObjects.Count > 0 && !string.IsNullOrEmpty(ObjEditor.inst.currentObjectSelection.ID) && ObjEditor.inst.currentObjectSelection.IsObject() && ObjEditor.inst.keyframeSelections.Count <= 1)
			{
				//Position
				{
					InputField posX = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/position/position/x").GetComponent<InputField>();
					InputField posY = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/position/position/y").GetComponent<InputField>();
					EventTrigger posXEvent = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/position/position/x").GetComponent<EventTrigger>();
					EventTrigger posYEvent = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/position/position/y").GetComponent<EventTrigger>();

					posXEvent.triggers.Clear();
					posXEvent.triggers.Add(Triggers.ScrollDelta(posX, 0.1f, 10f, true));
					posXEvent.triggers.Add(Triggers.ScrollDeltaVector2(posX, posY, 0.1f, 10f));

					posYEvent.triggers.Clear();
					posYEvent.triggers.Add(Triggers.ScrollDelta(posY, 0.1f, 10f, true));
					posYEvent.triggers.Add(Triggers.ScrollDeltaVector2(posX, posY, 0.1f, 10f));
				}

				//Scale
				{
					InputField scaX = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/scale/scale/x").GetComponent<InputField>();
					InputField scaY = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/scale/scale/y").GetComponent<InputField>();
					EventTrigger scaXEvent = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/scale/scale/x").GetComponent<EventTrigger>();
					EventTrigger scaYEvent = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/scale/scale/y").GetComponent<EventTrigger>();

					scaXEvent.triggers.Clear();
					scaXEvent.triggers.Add(Triggers.ScrollDelta(scaX, 0.1f, 10f, true));
					scaXEvent.triggers.Add(Triggers.ScrollDeltaVector2(scaX, scaY, 0.1f, 10f));

					scaYEvent.triggers.Clear();
					scaYEvent.triggers.Add(Triggers.ScrollDelta(scaY, 0.1f, 10f, true));
					scaYEvent.triggers.Add(Triggers.ScrollDeltaVector2(scaX, scaY, 0.1f, 10f));
				}

				//Rotation
				{
					InputField rotX = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/rotation/rotation/x").GetComponent<InputField>();
					EventTrigger rotXEvent = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/rotation/rotation").GetComponent<EventTrigger>();

					rotXEvent.triggers.Clear();
					rotXEvent.triggers.Add(Triggers.ScrollDelta(rotX, 15f, 3f, false));
				}
			}
		}

		[HarmonyPatch("AddPrefabExpandedToLevel")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> AddPrefabTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			return new CodeMatcher(instructions)
				.Start()
				.Advance(190)
				.ThrowIfNotMatch("Is not editorData object", new CodeMatch(OpCodes.Ldfld))
				.RemoveInstructions(14)
				.Advance(116)
				.ThrowIfNotMatch("Is not editorData prefab", new CodeMatch(OpCodes.Ldfld))
				.RemoveInstructions(14)
				.InstructionEnumeration();
		}

		[HarmonyPatch("CreateTimelineObjects")]
		[HarmonyPostfix]
		private static void SetEditorTime()
		{
			if (!string.IsNullOrEmpty(EditorManager.inst.currentLoadedLevel))
			{
				if (EditorPlugin.IfEditorStartTime.Value == true)
				{
					AudioManager.inst.CurrentAudioSource.time = DataManager.inst.gameData.beatmapData.editorData.timelinePos;
				}
				if (EditorPlugin.IfEditorPauses.Value == true)
				{
					AudioManager.inst.CurrentAudioSource.Pause();
				}

				if (RTFile.FileExists(RTFile.GetApplicationDirectory() + "beatmaps/" + EditorPlugin.editorPath + "/" + EditorManager.inst.currentLoadedLevel + "/editor.lse"))
				{
					string rawProfileJSON = null;
					rawProfileJSON = FileManager.inst.LoadJSONFile("beatmaps/" + EditorPlugin.editorPath + "/" + EditorManager.inst.currentLoadedLevel + "/editor.lse");

					JSONNode jsonnode = JSON.Parse(rawProfileJSON);

					EditorManager.inst.Zoom = jsonnode["timeline"]["z"];
					GameObject.Find("Editor Systems/Editor GUI/sizer/main/whole-timeline/zoom-panel/Slider").GetComponent<Slider>().value = jsonnode["timeline"]["tsc"];

					RTEditor.SetLayer(jsonnode["timeline"]["l"]);

					EditorPlugin.timeEdit = jsonnode["editor"]["t"];
					EditorPlugin.openAmount = jsonnode["editor"]["a"];
					EditorPlugin.openAmount += 1;

					SettingEditor.inst.SnapActive = jsonnode["misc"]["sn"];
					SettingEditor.inst.SnapBPM = DataManager.inst.metaData.song.BPM;
				}
				else
				{
					EditorPlugin.timeEdit = 0;
				}
			}
		}

		[HarmonyPatch("CreateTimelineObject")]
		[HarmonyPostfix]
		private static void CreateTimelineObjectPostfix(ref GameObject __result)
        {
			var hoverUI = __result.AddComponent<HoverUI>();
			hoverUI.animatePos = false;
			hoverUI.animateSca = true;
			hoverUI.size = EditorPlugin.HoverUIETLSize.Value;
        }

		[HarmonyPatch("CreateKeyframes")]
		[HarmonyPostfix]
		private static void CreateKeyframesPostfix()
        {
			for (int i = 0; i < ObjEditor.inst.timelineKeyframes.Count; i++)
            {
				foreach (var obj in ObjEditor.inst.timelineKeyframes[i])
                {
					if (!obj.GetComponent<HoverUI>())
					{
						var hoverUI = obj.AddComponent<HoverUI>();
						hoverUI.animatePos = false;
						hoverUI.animateSca = true;
						hoverUI.size = EditorPlugin.HoverUIKFSize.Value;
					}
                }
            }
        }

		[HarmonyPatch("Start")]
		[HarmonyPostfix]
		private static void SetObjStart()
		{
			ObjEditor.inst.zoomBounds = EditorPlugin.ObjZoomBounds.Value;
		}

		//[HarmonyPatch("RenderTimelineObjects")]
		//[HarmonyPostfix]
		private static void CreateBeatmapTooltips()
        {
			foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
            {
				if (ObjEditor.inst.beatmapObjects.ContainsKey(beatmapObject.id) && ObjEditor.inst.beatmapObjects[beatmapObject.id] && ObjEditor.inst.beatmapObjects[beatmapObject.id].activeSelf == true)
				{
					var timelineObject = ObjEditor.inst.beatmapObjects[beatmapObject.id];
					Triggers.AddTooltip(timelineObject, beatmapObject.name + " [ " + beatmapObject.StartTime.ToString() + " ]", "P: " + beatmapObject.parent + "\nD: " + beatmapObject.Depth.ToString());
				}
            }
        }

		[HarmonyPatch("SetCurrentObj")]
		[HarmonyPostfix]
		private static void SetCurrentObjPostfix(ObjEditor.ObjectSelection __0)
        {
			if (EditorPlugin.EditorDebug.Value == true)
			{
				if (__0.IsObject() && !string.IsNullOrEmpty(__0.ID) && __0.GetObjectData() != null && !__0.GetObjectData().fromPrefab)
				{
					if (ObjectManager.inst.beatmapGameObjects.ContainsKey(__0.GetObjectData().id) && ObjectManager.inst.beatmapGameObjects[__0.GetObjectData().id] != null)
					{
						ObjectManager.GameObjectRef gameObjectRef = ObjectManager.inst.beatmapGameObjects[__0.GetObjectData().id];

						Transform transform = gameObjectRef.rend.transform.GetParent();

						var beatmapObject = __0.GetObjectData();

						string parent = "";
						{
							if (!string.IsNullOrEmpty(beatmapObject.parent))
							{
								parent = "<br>P: " + beatmapObject.parent + " (" + beatmapObject.GetParentType() + ")";
							}
							else
							{
								parent = "<br>P: No Parent" + " (" + beatmapObject.GetParentType() + ")";
							}
						}

						string text = "";
						{
							if (beatmapObject.shape != 4 || beatmapObject.shape != 6)
							{
								text = "<br>S: " + RTEditor.GetShape(beatmapObject.shape, beatmapObject.shapeOption) +
									"<br>T: " + beatmapObject.text;
							}
							if (beatmapObject.shape == 4)
							{
								text = "<br>S: Text" +
									"<br>T: " + beatmapObject.text;
							}
							if (beatmapObject.shape == 6)
							{
								text = "<br>S: Image" +
									"<br>T: " + beatmapObject.text;
							}
						}

						string ptr = "";
						{
							if (beatmapObject.fromPrefab)
							{
								ptr = "<br>PID: " + beatmapObject.prefabID + " | " + beatmapObject.prefabInstanceID;
							}
							else
							{
								ptr = "<br>Not from prefab";
							}
						}

						Color color = Color.white;
						if (AudioManager.inst.CurrentAudioSource.time < beatmapObject.StartTime)
                        {
							color = GameManager.inst.LiveTheme.objectColors[(int)beatmapObject.events[3][0].eventValues[0]];
                        }
						else if (AudioManager.inst.CurrentAudioSource.time > beatmapObject.StartTime + beatmapObject.GetObjectLifeLength() && beatmapObject.autoKillType != DataManager.GameData.BeatmapObject.AutoKillType.OldStyleNoAutokill)
						{
							color = GameManager.inst.LiveTheme.objectColors[(int)beatmapObject.events[3][beatmapObject.events[3].Count - 1].eventValues[0]];
						}
						else
                        {
							color = gameObjectRef.mat.color;
						}

						RTEditor.DisplayCustomNotification("RenderTimelineBeatmapObject", "<br>N/ST: " + beatmapObject.name + " [ " + beatmapObject.StartTime + " ]" +
							"<br>ID: {" + beatmapObject.id + "}" +
							parent +
							"<br>O: {X: " + beatmapObject.origin.x + ", Y: " + beatmapObject.origin.y + "}" +
							text +
							"<br>D: " + beatmapObject.Depth +
							"<br>ED: {L: " + beatmapObject.editorData.Layer + ", B: " + beatmapObject.editorData.Bin + "}" +
							"<br>POS: {X: " + transform.position.x + ", Y: " + transform.position.y + "}" +
							"<br>SCA: {X: " + transform.localScale.x + ", Y: " + transform.localScale.y + "}" +
							"<br>ROT: " + transform.eulerAngles.z +
							"<br>COL: " + RTEditor.ColorToHex(color) +
							ptr, 1f, LSColors.HexToColor("202020"), color, LSColors.InvertBlackWhiteColor(color), "Beatmap Object");
					}
				}
				if (__0.IsPrefab() && !string.IsNullOrEmpty(__0.ID) && __0.GetPrefabObjectData() != null)
				{
					var prefab = __0.GetPrefabData();
                    var prefabInstance = __0.GetPrefabObjectData();

                    Color prefabColor = DataManager.inst.PrefabTypes[prefab.Type].Color;
					RTEditor.DisplayCustomNotification("RenderTimelinePrefabObject", "" +
						"<br>N/ST: " + prefab.Name + " [ " + prefabInstance.StartTime.ToString() + " ]" +
						"<br>PID: {" + prefab.ID + "}" +
						"<br>PIID: {" + prefabInstance.ID + "}" +
						"<br>Type: " + DataManager.inst.PrefabTypes[prefab.Type].Name +
						"<br>O: " + prefab.Offset.ToString() +
						"<br>Count: " + prefab.objects.Count +
						"<br>ED: {L: " + prefabInstance.editorData.Layer + ", B: " + prefabInstance.editorData.Bin + "}" +
						"<br>POS: {X: " + prefabInstance.events[0].eventValues[0] + ", Y: " + prefabInstance.events[0].eventValues[1] + "}" +
						"<br>SCA: {X: " + prefabInstance.events[1].eventValues[0] + ", Y: " + prefabInstance.events[1].eventValues[1] + "}" +
						"<br>ROT: " + prefabInstance.events[2].eventValues[0] +
						"", 1f, LSColors.HexToColor("202020"), prefabColor, LSColors.InvertBlackWhiteColor(prefabColor), "Prefab Object");
				}
			}
        }

		[HarmonyPatch("RenderTimelineObject")]
		[HarmonyPrefix]
		private static bool RenderTimelineObjectPrefix(ref GameObject __result, ObjEditor.ObjectSelection __0)
        {
			__result = RTEditor.RenderTimelineObject(__0);
			return false;
        }

		[HarmonyPatch("SnapToBPM")]
		[HarmonyPostfix]
		private static void SnapToBPMPostfix(float __result, float __0)
        {
			Debug.Log("[<color=#00796b>ObjEditor</color>]\nSnap Input: " + __0 + "\nSnap Result: " + __result);
        }

		[HarmonyPatch("Update")]
		[HarmonyPrefix]
		private static bool UpdatePrefix()
        {
			if (!EditorManager.inst.IsUsingInputField())
			{
				if (InputDataManager.inst.editorActions.FirstKeyframe.WasPressed)
					ObjEditor.inst.SetCurrentKeyframe(0, true);
				if (InputDataManager.inst.editorActions.BackKeyframe.WasPressed)
					ObjEditor.inst.AddCurrentKeyframe(-1, true);
				if (InputDataManager.inst.editorActions.ForwardKeyframe.WasPressed)
					ObjEditor.inst.AddCurrentKeyframe(1, true);
				if (InputDataManager.inst.editorActions.LastKeyframe.WasPressed)
					ObjEditor.inst.AddCurrentKeyframe(10000, true);
				if (InputDataManager.inst.editorActions.LockObject.WasPressed)
					ObjEditor.inst.ToggleLockCurrentSelection();
			}
			if (!ObjEditor.inst.changingTime && ObjEditor.inst.currentObjectSelection.IsObject())
			{
				if (DataManager.inst.gameData.beatmapObjects.Count > ObjEditor.inst.currentObjectSelection.Index && ObjEditor.inst.currentObjectSelection.Index >= 0)
					ObjEditor.inst.newTime = Mathf.Clamp(EditorManager.inst.CurrentAudioPos, ObjEditor.inst.currentObjectSelection.GetObjectData().StartTime, ObjEditor.inst.currentObjectSelection.GetObjectData().StartTime + ObjEditor.inst.currentObjectSelection.GetObjectData().GetObjectLifeLength(ObjEditor.inst.ObjectLengthOffset));
				ObjEditor.inst.objTimelineSlider.value = ObjEditor.inst.newTime;
			}
			if (Input.GetMouseButtonUp(0))
			{
				ObjEditor.inst.beatmapObjectsDrag = false;
				ObjEditor.inst.timelineKeyframesDrag = false;
			}
			if (ObjEditor.inst.selectedObjects.Count > 1 && ObjEditor.inst.beatmapObjectsDrag)
			{
				if (InputDataManager.inst.editorActions.MultiSelect.IsPressed)
				{
					int num1 = 14 - Mathf.RoundToInt((float)(((double)Input.mousePosition.y - 25.0) * (double)EditorManager.inst.ScreenScaleInverse / 20.0)) + ObjEditor.inst.mouseOffsetYForDrag;
					int num2 = 0;
					foreach (ObjEditor.ObjectSelection selectedObject in ObjEditor.inst.selectedObjects)
					{
						if (selectedObject.IsObject() && !selectedObject.GetObjectData().editorData.locked)
							DataManager.inst.gameData.beatmapObjects[selectedObject.Index].editorData.Bin = Mathf.Clamp(num1 + selectedObject.BinOffset, 0, 14);
						else if (selectedObject.IsPrefab() && !selectedObject.GetPrefabObjectData().editorData.locked)
							DataManager.inst.gameData.prefabObjects[selectedObject.Index].editorData.Bin = Mathf.Clamp(num1 + selectedObject.BinOffset, 0, 14);
						ObjEditor.inst.RenderTimelineObject(selectedObject);
						++num2;
					}
				}
				else
				{
					float num3 = Mathf.Round(Mathf.Clamp(EditorManager.inst.GetTimelineTime() + ObjEditor.inst.mouseOffsetXForDrag, 0.0f, AudioManager.inst.CurrentAudioSource.clip.length) * 1000f) / 1000f;
					if (ObjEditor.inst.currentObjectSelection.IsObject() && !ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.locked)
						DataManager.inst.gameData.beatmapObjects[ObjEditor.inst.currentObjectSelection.Index].StartTime = num3;
					else if (ObjEditor.inst.currentObjectSelection.IsPrefab() && !ObjEditor.inst.currentObjectSelection.GetPrefabObjectData().editorData.locked)
						DataManager.inst.gameData.prefabObjects[ObjEditor.inst.currentObjectSelection.Index].StartTime = num3;
					ObjEditor.inst.RenderTimelineObject(ObjEditor.inst.currentObjectSelection);
					int num4 = 0;
					foreach (ObjEditor.ObjectSelection selectedObject in ObjEditor.inst.selectedObjects)
					{
						if (selectedObject.IsObject() && !selectedObject.GetObjectData().editorData.locked)
							DataManager.inst.gameData.beatmapObjects[selectedObject.Index].StartTime = Mathf.Clamp(num3 + selectedObject.TimeOffset, 0.0f, AudioManager.inst.CurrentAudioSource.clip.length);
						else if (selectedObject.IsPrefab() && !selectedObject.GetPrefabObjectData().editorData.locked)
							DataManager.inst.gameData.prefabObjects[selectedObject.Index].StartTime = Mathf.Clamp(num3 + selectedObject.TimeOffset, 0.0f, AudioManager.inst.CurrentAudioSource.clip.length);
						ObjEditor.inst.RenderTimelineObject(selectedObject);
						++num4;
					}
				}
			}
			else if (ObjEditor.inst.beatmapObjectsDrag)
			{
				if (InputDataManager.inst.editorActions.MultiSelect.IsPressed)
				{
					int num = 14 - Mathf.RoundToInt((float)(((double)Input.mousePosition.y - 25.0) * (double)EditorManager.inst.ScreenScaleInverse / 20.0));
					if (ObjEditor.inst.currentObjectSelection.IsObject() && !ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.locked)
						DataManager.inst.gameData.beatmapObjects[ObjEditor.inst.currentObjectSelection.Index].editorData.Bin = Mathf.Clamp(num, 0, 14);
					else if (ObjEditor.inst.currentObjectSelection.IsPrefab() && !ObjEditor.inst.currentObjectSelection.GetPrefabObjectData().editorData.locked)
						DataManager.inst.gameData.prefabObjects[ObjEditor.inst.currentObjectSelection.Index].editorData.Bin = Mathf.Clamp(num, 0, 14);
				}
				else
				{
					float num = Mathf.Round(EditorManager.inst.GetTimelineTime(ObjEditor.inst.mouseOffsetXForDrag) * 1000f) / 1000f;
					if (ObjEditor.inst.currentObjectSelection.IsObject() && !ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.locked)
						DataManager.inst.gameData.beatmapObjects[ObjEditor.inst.currentObjectSelection.Index].StartTime = Mathf.Clamp(num, 0.0f, AudioManager.inst.CurrentAudioSource.clip.length);
					else if (ObjEditor.inst.currentObjectSelection.IsPrefab() && !ObjEditor.inst.currentObjectSelection.GetPrefabObjectData().editorData.locked)
						DataManager.inst.gameData.prefabObjects[ObjEditor.inst.currentObjectSelection.Index].StartTime = Mathf.Clamp(num, 0.0f, AudioManager.inst.CurrentAudioSource.clip.length);
				}
				ObjEditor.inst.RenderTimelineObject(ObjEditor.inst.currentObjectSelection);
			}

			Dragger();
			return false;
        }

		private static void Dragger()
		{
			if (ObjEditor.inst.timelineKeyframesDrag)
			{
				foreach (ObjEditor.KeyframeSelection keyframeSelection in ObjEditor.inst.keyframeSelections)
				{
					if (keyframeSelection.Index != 0)
					{
						float num6 = timeCalc() + ObjEditor.inst.selectedKeyframeOffsets[ObjEditor.inst.keyframeSelections.IndexOf(keyframeSelection)] + ObjEditor.inst.mouseOffsetXForKeyframeDrag;
						num6 = Mathf.Clamp(num6, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
						num6 = Mathf.Round(num6 * 1000f) / 1000f;

						float calc = Mathf.Clamp(num6, 0f, DataManager.inst.gameData.beatmapObjects[ObjEditor.inst.currentObjectSelection.Index].GetObjectLifeLength(ObjEditor.inst.ObjectLengthOffset, false, false));

						if (SettingEditor.inst.SnapActive)
						{
							float st = ObjEditor.inst.currentObjectSelection.GetObjectData().StartTime;
							float kf = calc;

							if (og == 0)
                            {
								og = RTEditor.SnapToBPM(st + kf);
							}

							if (og != RTEditor.SnapToBPM(st + kf))
							{
								float allt = st - RTEditor.SnapToBPM(st + kf);
								og = RTEditor.SnapToBPM(st + kf);
								ObjEditor.inst.currentObjectSelection.GetObjectData().events[keyframeSelection.Type][keyframeSelection.Index].eventTime = -allt;

								float num7 = posCalc(DataManager.inst.gameData.beatmapObjects[ObjEditor.inst.currentObjectSelection.Index].events[keyframeSelection.Type][keyframeSelection.Index].eventTime);
								if (num7 < 0f)
								{
									num7 = 0f;
								}

								ObjEditor.inst.timelineKeyframes[keyframeSelection.Type][keyframeSelection.Index].GetComponent<RectTransform>().anchoredPosition = new Vector2(num7, 0f);
							}
						}
						else
						{
							ObjEditor.inst.currentObjectSelection.GetObjectData().events[keyframeSelection.Type][keyframeSelection.Index].eventTime = calc;

							float num7 = posCalc(DataManager.inst.gameData.beatmapObjects[ObjEditor.inst.currentObjectSelection.Index].events[keyframeSelection.Type][keyframeSelection.Index].eventTime);
							if (num7 < 0f)
							{
								num7 = 0f;
							}

							ObjEditor.inst.timelineKeyframes[keyframeSelection.Type][keyframeSelection.Index].GetComponent<RectTransform>().anchoredPosition = new Vector2(num7, 0f);
						}
					}
				}
				AccessTools.Method(typeof(ObjEditor), "ResizeKeyframeTimeline").Invoke(ObjEditor.inst, new object[] { });
				ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection, false);
				AccessTools.Method(typeof(ObjEditor), "UpdateHighlightedKeyframe").Invoke(ObjEditor.inst, new object[] { });
				foreach (ObjEditor.ObjectSelection obj in ObjEditor.inst.selectedObjects)
				{
					ObjEditor.inst.RenderTimelineObject(obj);
				}

			}
		}

		public static float og;

		[HarmonyPatch("SetMainTimelineZoom")]
		[HarmonyPrefix]
		private static bool TimelineZoomSizer(float __0, bool __1, ref float __2)
        {
			var resizeKeyframeTimeline = AccessTools.Method(typeof(ObjEditor), "ResizeKeyframeTimeline");
			var createKeyframes = AccessTools.Method(typeof(ObjEditor), "CreateKeyframes");

			if (__1)
			{
				resizeKeyframeTimeline.Invoke(ObjEditor.inst, new object[] { });
				createKeyframes.Invoke(ObjEditor.inst, new object[] { -1 });
			}
			if (AudioManager.inst.CurrentAudioSource.clip != null)
			{
				float time = -ObjEditor.inst.currentObjectSelection.GetObjectData().StartTime + AudioManager.inst.CurrentAudioSource.time;
				float objectLifeLength = ObjEditor.inst.currentObjectSelection.GetObjectData().GetObjectLifeLength(ObjEditor.inst.ObjectLengthOffset, false, false);

				__2 = time / objectLifeLength;
				Debug.Log("Set Timeline Zoom: " + __2 + " = " + time + " / " + objectLifeLength);
			}
			ObjEditor.inst.StartCoroutine(ObjEditor.inst.UpdateTimelineScrollRect(0f, __2));
			return false;
        }

		[HarmonyPatch(typeof(ObjEditor), "UpdateTimelineScrollRect")]
		[HarmonyPostfix]
		private static void DoTheThing(float __0, float __1)
        {
			if (GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/timeline/Scroll View/Scrollbar Horizontal") && GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/timeline/Scroll View/Scrollbar Horizontal").GetComponent<Scrollbar>())
			{
				GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/timeline/Scroll View/Scrollbar Horizontal").GetComponent<Scrollbar>().value = __1;
			}
			else
            {
				Debug.LogError("Scrollbar missing!");
            }
        }
    }
}
