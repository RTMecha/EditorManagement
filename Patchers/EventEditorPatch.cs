using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using SimpleJSON;
using LSFunctions;

using EditorManagement.Functions.Editors;
using EditorManagement.Functions.Components;
using EditorManagement.Functions;
using EditorManagement.Functions.Tools;

using RTFunctions.Functions;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;

namespace EditorManagement.Patchers
{
    [HarmonyPatch(typeof(EventEditor))]
    public class EventEditorPatch : MonoBehaviour
    {
		public static List<Toggle> vignetteColorButtons = new List<Toggle>();
		public static List<Toggle> bloomColorButtons = new List<Toggle>();
		public static List<Toggle> gradientColor1Buttons = new List<Toggle>();
		public static List<Toggle> gradientColor2Buttons = new List<Toggle>();
		public static List<Toggle> bgColorButtons = new List<Toggle>();
		public static List<Toggle> overlayColorButtons = new List<Toggle>();
		public static List<Toggle> timelineColorButtons = new List<Toggle>();

		public static Dictionary<string, Color> eventEditorTitleColors = new Dictionary<string, Color>();

		public static void SetupEventList()
        {
			eventEditorTitleColors.Clear();
			eventEditorTitleColors.Add("- Move Editor -", new Color(0.3372549f, 0.2941177f, 0.4156863f, 1f)); //1
			eventEditorTitleColors.Add("- Zoom Editor -", new Color(0.254902f, 0.2705882f, 0.372549f, 1f)); //2
			eventEditorTitleColors.Add("- Rotation Editor -", new Color(0.2705882f, 0.3843138f, 0.4784314f, 1f)); //3
			eventEditorTitleColors.Add("- Shake Editor -", new Color(0.1960784f, 0.3607843f, 0.4313726f, 1f)); //4
			eventEditorTitleColors.Add("- Theme Editor -", new Color(0.2470588f, 0.427451f, 0.4509804f, 1f)); //5
			eventEditorTitleColors.Add("- Chromatic Editor -", new Color(0.1882353f, 0.3372549f, 0.3254902f, 1f)); //6
			eventEditorTitleColors.Add("- Bloom Editor -", new Color(0.3137255f, 0.4117647f, 0.3176471f, 1f)); //7
			eventEditorTitleColors.Add("- Vignette Editor -", new Color(0.3176471f, 0.3686275f, 0.2588235f, 1f)); //8
			eventEditorTitleColors.Add("- Lens Distort Editor -", new Color(0.4039216f, 0.4117647f, 0.2745098f, 1f)); //9
			eventEditorTitleColors.Add("- Grain Editor -", new Color(0.4470589f, 0.3882353f, 0.2117647f, 1f)); //10
			eventEditorTitleColors.Add("- Color Grading Editor -", new Color(1f, 0.5960785f, 0f, 1f)); //11
			eventEditorTitleColors.Add("- Ripples Editor -", new Color(1f, 0.3490196f, 0f, 1f)); //12
			eventEditorTitleColors.Add("- Radial Blur Editor -", new Color(1f, 0.1490196f, 0.03529412f, 1f)); //13
			eventEditorTitleColors.Add("- Color Split Editor -", new Color(1f, 0.05882353f, 0.05882353f, 1f)); //14

			eventEditorTitleColors.Add("- Camera Offset Editor -", new Color(0.3372549f, 0.2941177f, 0.4156863f, 1f)); //1
			eventEditorTitleColors.Add("- Gradient Editor -", new Color(0.254902f, 0.2705882f, 0.372549f, 1f)); //2
			eventEditorTitleColors.Add("- Double Vision Editor -", new Color(0.2705882f, 0.3843138f, 0.4784314f, 1f)); //3
			eventEditorTitleColors.Add("- Scan Lines Editor -", new Color(0.1960784f, 0.3607843f, 0.4313726f, 1f)); //4
			eventEditorTitleColors.Add("- Blur Editor -", new Color(0.2470588f, 0.427451f, 0.4509804f, 1f)); //5
			eventEditorTitleColors.Add("- Pixelize Editor -", new Color(0.1882353f, 0.3372549f, 0.3254902f, 1f)); //6
			eventEditorTitleColors.Add("- BG Editor -", new Color(0.3137255f, 0.4117647f, 0.3176471f, 1f)); //7
			eventEditorTitleColors.Add("- Invert Editor -", new Color(0.3176471f, 0.3686275f, 0.2588235f, 1f)); //8
			eventEditorTitleColors.Add("- Timeline Editor -", new Color(0.4039216f, 0.4117647f, 0.2745098f, 1f)); //9
			eventEditorTitleColors.Add("- Player Event Editor -", new Color(0.4470589f, 0.3882353f, 0.2117647f, 1f)); //10
			eventEditorTitleColors.Add("- Follow Player Editor -", new Color(1f, 0.5960785f, 0f, 1f)); //11
			eventEditorTitleColors.Add("- Audio Editor -", new Color(1f, 0.3490196f, 0f, 1f)); //12
			//eventEditorTitleColors.Add("- Glitch Editor -", new Color(1f, 0.1490196f, 0.03529412f, 1f)); //13
			//eventEditorTitleColors.Add("- Misc Editor -", new Color(1f, 0.05882353f, 0.05882353f, 1f)); //14

			RenderTitles();
		}

		public static void RenderTitles()
        {
			for (int i = 0; i < EventEditor.inst.dialogRight.childCount; i++)
			{
				var title = EventEditor.inst.dialogRight.GetChild(i).GetChild(0);
				title.GetChild(0).GetComponent<Image>().color = eventEditorTitleColors.ElementAt(i).Value;
				title.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(17f, 0f);
				title.GetChild(1).GetComponent<Text>().text = eventEditorTitleColors.ElementAt(i).Key;
			}
		}

		public static int eventLayer = 0;
		public static bool eventsCore = false;
		public static void SetEventLayer(int _layer)
        {
			eventLayer = _layer;
			RTEditor.SetLayer(5);
			CreateEventObjectsPrefix();
			if (eventLayer == 0)
            {
				if (GameObject.Find("TimelineBar/GameObject/6").GetComponent<Toggle>())
					GameObject.Find("TimelineBar/GameObject/6").GetComponent<Toggle>().isOn = true;
				if (loggle != null)
					loggle.isOn = false;
			}
			if (eventLayer == 1)
			{
				if (GameObject.Find("TimelineBar/GameObject/6").GetComponent<Toggle>())
					GameObject.Find("TimelineBar/GameObject/6").GetComponent<Toggle>().isOn = false;
				if (loggle != null)
					loggle.isOn = true;
            }

			var eventLabels = EventEditor.inst.EventLabels;

			switch (_layer)
            {
				case 0:
					{
						eventLabels.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Move";
						eventLabels.transform.GetChild(1).GetChild(0).GetComponent<Text>().text = "Zoom";
						eventLabels.transform.GetChild(2).GetChild(0).GetComponent<Text>().text = "Rotate";
						eventLabels.transform.GetChild(3).GetChild(0).GetComponent<Text>().text = "Shake";
						eventLabels.transform.GetChild(4).GetChild(0).GetComponent<Text>().text = "Theme";
						eventLabels.transform.GetChild(5).GetChild(0).GetComponent<Text>().text = "Chromatic";
						eventLabels.transform.GetChild(6).GetChild(0).GetComponent<Text>().text = "Bloom";
						eventLabels.transform.GetChild(7).GetChild(0).GetComponent<Text>().text = "Vignette";
						eventLabels.transform.GetChild(8).GetChild(0).GetComponent<Text>().text = "Lens";
						eventLabels.transform.GetChild(9).GetChild(0).GetComponent<Text>().text = "Grain";
						eventLabels.transform.GetChild(10).GetChild(0).GetComponent<Text>().text = "ColorGrading";
						eventLabels.transform.GetChild(11).GetChild(0).GetComponent<Text>().text = "Ripples";
						eventLabels.transform.GetChild(12).GetChild(0).GetComponent<Text>().text = "RadialBlur";
						eventLabels.transform.GetChild(13).GetChild(0).GetComponent<Text>().text = "ColorSplit";
						break;
                    }
				case 1:
					{
						eventLabels.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Offset";
						eventLabels.transform.GetChild(1).GetChild(0).GetComponent<Text>().text = "Gradient";
						eventLabels.transform.GetChild(2).GetChild(0).GetComponent<Text>().text = "DoubleVision";
						eventLabels.transform.GetChild(3).GetChild(0).GetComponent<Text>().text = "ScanLines";
						eventLabels.transform.GetChild(4).GetChild(0).GetComponent<Text>().text = "Blur";
						eventLabels.transform.GetChild(5).GetChild(0).GetComponent<Text>().text = "Pixelize";
						eventLabels.transform.GetChild(6).GetChild(0).GetComponent<Text>().text = "BG";
						eventLabels.transform.GetChild(7).GetChild(0).GetComponent<Text>().text = "Invert";
						eventLabels.transform.GetChild(8).GetChild(0).GetComponent<Text>().text = "Timeline";
						eventLabels.transform.GetChild(9).GetChild(0).GetComponent<Text>().text = "Player";
						eventLabels.transform.GetChild(10).GetChild(0).GetComponent<Text>().text = "Follow Player";
						eventLabels.transform.GetChild(11).GetChild(0).GetComponent<Text>().text = "Audio";
						eventLabels.transform.GetChild(12).GetChild(0).GetComponent<Text>().text = "Glitch (Coming soon)";
						eventLabels.transform.GetChild(13).GetChild(0).GetComponent<Text>().text = "Misc (Coming soon)";
						break;
                    }
            }
		}

		[HarmonyPatch("Awake")]
		[HarmonyPostfix]
		private static void AwakePatch()
        {
			if (GameObject.Find("BepInEx_Manager").GetComponentByName("EventsCorePlugin"))
			{
				eventsCore = true;
			}

			GameObject copyBase = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/grain");
			GameObject cgDialog = Instantiate(copyBase);

			Debug.LogFormat("{0}ColorGrading", EditorPlugin.className);
			cgDialog.transform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right").transform);
			cgDialog.transform.localScale = Vector3.one;
			cgDialog.name = "colorgrading";

			var intensityLabel = cgDialog.transform.GetChild(8);
			intensityLabel.GetChild(0).GetComponent<Text>().text = "Hueshift";

			var intensity = cgDialog.transform.Find("intensity");
			intensity.name = "intensity";

			Destroy(cgDialog.transform.GetChild(10).gameObject);
			Destroy(cgDialog.transform.GetChild(11).gameObject);
			Destroy(cgDialog.transform.GetChild(12).gameObject);
			Destroy(cgDialog.transform.GetChild(13).gameObject);

			cgDialog.transform.Find("grain_title/bg").GetComponent<Image>().color = EventEditor.inst.EventColors[10];
			cgDialog.transform.Find("grain_title/Text").GetComponent<Text>().text = "- ColorGrading Keyframe Editor -";

			var contrastLabel = Instantiate(intensityLabel.gameObject);
			contrastLabel.transform.SetParent(cgDialog.transform);
			contrastLabel.transform.localScale = Vector3.one;
			contrastLabel.transform.GetChild(0).GetComponent<Text>().text = "Contrast";

			var contrast = Instantiate(intensity.gameObject);
			contrast.transform.SetParent(cgDialog.transform);
			contrast.transform.localScale = Vector3.one;
			contrast.name = "contrast";

			var saturationLabel = Instantiate(intensityLabel.gameObject);
			saturationLabel.transform.SetParent(cgDialog.transform);
			saturationLabel.transform.localScale = Vector3.one;
			saturationLabel.transform.GetChild(0).GetComponent<Text>().text = "Saturation";

			var saturation = Instantiate(intensity.gameObject);
			saturation.transform.SetParent(cgDialog.transform);
			saturation.transform.localScale = Vector3.one;
			saturation.name = "saturation";

			var temperatureLabel = Instantiate(intensityLabel.gameObject);
			temperatureLabel.transform.SetParent(cgDialog.transform);
			temperatureLabel.transform.localScale = Vector3.one;
			temperatureLabel.transform.GetChild(0).GetComponent<Text>().text = "Temperature";

			var temperature = Instantiate(intensity.gameObject);
			temperature.transform.SetParent(cgDialog.transform);
			temperature.transform.localScale = Vector3.one;
			temperature.name = "temperature";

			var tintLabel = Instantiate(intensityLabel.gameObject);
			tintLabel.transform.SetParent(cgDialog.transform);
			tintLabel.transform.localScale = Vector3.one;
			tintLabel.transform.GetChild(0).GetComponent<Text>().text = "Tint";

			var tint = Instantiate(intensity.gameObject);
			tint.transform.SetParent(cgDialog.transform);
			tint.transform.localScale = Vector3.one;
			tint.name = "tint";


			Debug.LogFormat("{0}Ripples", EditorPlugin.className);
			GameObject ripDialog = Instantiate(copyBase);
			ripDialog.transform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right").transform);
			ripDialog.transform.localScale = Vector3.one;
			ripDialog.name = "ripples";
			ripDialog.transform.Find("grain_title/bg").GetComponent<Image>().color = LSColors.HexToColor("FF5900");
			ripDialog.transform.Find("grain_title/Text").GetComponent<Text>().text = "- Ripples Keyframe Editor -";

			var gIntensityLabel = ripDialog.transform.GetChild(8);
			gIntensityLabel.transform.SetParent(ripDialog.transform);
			gIntensityLabel.transform.localScale = Vector3.one;
			gIntensityLabel.transform.GetChild(0).GetComponent<Text>().text = "Strength";

			var ripStrength = ripDialog.transform.Find("intensity");
			ripStrength.transform.SetParent(ripDialog.transform);
			ripStrength.transform.localScale = Vector3.one;
			ripStrength.name = "strength";

			var ripSpeedLabel = Instantiate(intensityLabel.gameObject);
			ripSpeedLabel.transform.SetParent(ripDialog.transform);
			ripSpeedLabel.transform.localScale = Vector3.one;
			ripSpeedLabel.transform.GetChild(0).GetComponent<Text>().text = "Speed";

			var ripSpeed = Instantiate(intensity.gameObject);
			ripSpeed.transform.SetParent(ripDialog.transform);
			ripSpeed.transform.localScale = Vector3.one;
			ripSpeed.name = "speed";

			var ripDistanceLabel = Instantiate(intensityLabel.gameObject);
			ripDistanceLabel.transform.SetParent(ripDialog.transform);
			ripDistanceLabel.transform.localScale = Vector3.one;
			ripDistanceLabel.transform.GetChild(0).GetComponent<Text>().text = "Distance";

			var ripDistance = Instantiate(intensity.gameObject);
			ripDistance.transform.SetParent(ripDialog.transform);
			ripDistance.transform.localScale = Vector3.one;
			ripDistance.name = "distance";

			var ripHeightLabel = Instantiate(intensityLabel.gameObject);
			ripHeightLabel.transform.SetParent(ripDialog.transform);
			ripHeightLabel.transform.localScale = Vector3.one;
			ripHeightLabel.transform.GetChild(0).GetComponent<Text>().text = "Height";

			var ripHeight = Instantiate(intensity.gameObject);
			ripHeight.transform.SetParent(ripDialog.transform);
			ripHeight.transform.localScale = Vector3.one;
			ripHeight.name = "height";

			var ripWidthLabel = Instantiate(intensityLabel.gameObject);
			ripWidthLabel.transform.SetParent(ripDialog.transform);
			ripWidthLabel.transform.localScale = Vector3.one;
			ripWidthLabel.transform.GetChild(0).GetComponent<Text>().text = "Width";

			var ripWidth = Instantiate(intensity.gameObject);
			ripWidth.transform.SetParent(ripDialog.transform);
			ripWidth.transform.localScale = Vector3.one;
			ripWidth.name = "width";

			Destroy(ripDialog.transform.GetChild(10).gameObject);
			Destroy(ripDialog.transform.GetChild(11).gameObject);
			Destroy(ripDialog.transform.GetChild(12).gameObject);
			Destroy(ripDialog.transform.GetChild(13).gameObject);


			Debug.LogFormat("{0}RadialBlur", EditorPlugin.className);
			GameObject rbDialog = Instantiate(copyBase);
			rbDialog.transform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right").transform);
			rbDialog.transform.localScale = Vector3.one;
			rbDialog.name = "radialblur";
			rbDialog.transform.Find("grain_title/bg").GetComponent<Image>().color = LSColors.HexToColor("FF2609");
			rbDialog.transform.Find("grain_title/Text").GetComponent<Text>().text = "- RadialBlur Keyframe Editor -";

			var rbIntensityLabel = rbDialog.transform.GetChild(8);

			var rbIntensity = rbDialog.transform.Find("intensity");

			Destroy(rbDialog.transform.GetChild(10).gameObject);
			Destroy(rbDialog.transform.GetChild(11).gameObject);
			Destroy(rbDialog.transform.GetChild(12).gameObject);
			Destroy(rbDialog.transform.GetChild(13).gameObject);

			var rbIterationsLabel = Instantiate(intensityLabel.gameObject);
			rbIterationsLabel.transform.SetParent(rbDialog.transform);
			rbIterationsLabel.transform.localScale = Vector3.one;
			rbIterationsLabel.transform.GetChild(0).GetComponent<Text>().text = "Iterations";

			var rbIterations = Instantiate(intensity.gameObject);
			rbIterations.transform.SetParent(rbDialog.transform);
			rbIterations.transform.localScale = Vector3.one;
			rbIterations.name = "iterations";


			Debug.LogFormat("{0}ColorSplit", EditorPlugin.className);
			GameObject csDialog = Instantiate(copyBase);
			csDialog.transform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right").transform);
			csDialog.transform.localScale = Vector3.one;
			csDialog.name = "colorsplit";
			csDialog.transform.Find("grain_title/bg").GetComponent<Image>().color = LSColors.HexToColor("FF0F0F");
			csDialog.transform.Find("grain_title/Text").GetComponent<Text>().text = "- ColorSplit Keyframe Editor -";

			var csOffsetLabel = csDialog.transform.GetChild(8);
			csOffsetLabel.GetChild(0).GetComponent<Text>().text = "Offset";

			var csOffset = csDialog.transform.Find("intensity");
			csOffset.name = "offset";

			Destroy(csDialog.transform.GetChild(10).gameObject);
			Destroy(csDialog.transform.GetChild(11).gameObject);
			Destroy(csDialog.transform.GetChild(12).gameObject);
			Destroy(csDialog.transform.GetChild(13).gameObject);

			var themeParent = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/left/theme/theme/viewport/content").transform;
			for (int i = 10; i < 19; i++)
			{
				GameObject col = Instantiate(themeParent.Find("object8").gameObject);
				col.name = "object" + (i - 1).ToString();
				col.transform.SetParent(themeParent);
				col.transform.Find("text").GetComponent<Text>().text = i.ToString();
				col.transform.SetSiblingIndex(8 + i);
			}

			if (GameObject.Find("BepInEx_Manager").GetComponentByName("EventsCorePlugin"))
			{
				eventsCore = true;
			}

			if (eventsCore)
			{
				var bloomP = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/bloom").transform;

				var bloom1Label = Instantiate(intensityLabel.gameObject);
				bloom1Label.transform.SetParent(bloomP);
				bloom1Label.transform.localScale = Vector3.one;
				bloom1Label.transform.GetChild(0).GetComponent<Text>().text = "Diffusion";

				var bloom1 = Instantiate(intensity.gameObject);
				bloom1.transform.SetParent(bloomP);
				bloom1.transform.localScale = Vector3.one;
				bloom1.name = "diffusion";

				var bloom2Label = Instantiate(intensityLabel.gameObject);
				bloom2Label.transform.SetParent(bloomP);
				bloom2Label.transform.localScale = Vector3.one;
				bloom2Label.transform.GetChild(0).GetComponent<Text>().text = "Threshold";

				var bloom2 = Instantiate(intensity.gameObject);
				bloom2.transform.SetParent(bloomP);
				bloom2.transform.localScale = Vector3.one;
				bloom2.name = "threshold";

				var bloom3Label = Instantiate(intensityLabel.gameObject);
				bloom3Label.transform.SetParent(bloomP);
				bloom3Label.transform.localScale = Vector3.one;
				bloom3Label.transform.GetChild(0).GetComponent<Text>().text = "Anamorphic Ratio";

				var bloom3 = Instantiate(intensity.gameObject);
				bloom3.transform.SetParent(bloomP);
				bloom3.transform.localScale = Vector3.one;
				bloom3.name = "anamorphic ratio";

				var lensP = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/lens").transform;

				var lens1Label = Instantiate(intensityLabel.gameObject);
				lens1Label.transform.SetParent(lensP);
				lens1Label.transform.localScale = Vector3.one;
				lens1Label.transform.GetChild(0).GetComponent<Text>().text = "Center";

				var lens1 = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move/position"));
				lens1.transform.SetParent(lensP);
				lens1.transform.localScale = Vector3.one;
				lens1.name = "center";

				var lens3Label = Instantiate(intensityLabel.gameObject);
				lens3Label.transform.SetParent(lensP);
				lens3Label.transform.localScale = Vector3.one;
				lens3Label.transform.GetChild(0).GetComponent<Text>().text = "Intensity";

				var lens3 = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move/position"));
				lens3.transform.SetParent(lensP);
				lens3.transform.localScale = Vector3.one;
				lens3.name = "intensity";

				var lens5Label = Instantiate(intensityLabel.gameObject);
				lens5Label.transform.SetParent(lensP);
				lens5Label.transform.localScale = Vector3.one;
				lens5Label.transform.GetChild(0).GetComponent<Text>().text = "Scale";

				var lens5 = Instantiate(intensity.gameObject);
				lens5.transform.SetParent(lensP);
				lens5.transform.localScale = Vector3.one;
				lens5.name = "scale";

				var shakeP = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/shake").transform;

				var shakeLabel = Instantiate(intensityLabel.gameObject);
				shakeLabel.transform.SetParent(shakeP);
				shakeLabel.transform.localScale = Vector3.one;
				shakeLabel.transform.GetChild(0).GetComponent<Text>().text = "Direction";

				var shakeXY = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move/position"));
				shakeXY.transform.SetParent(shakeP);
				shakeXY.transform.localScale = Vector3.one;
				shakeXY.name = "direction";
			}

			var camOffset = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move"));
			camOffset.transform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right").transform);
			camOffset.transform.localScale = Vector3.one;
			camOffset.name = "camoffset";

			EventEditor.inst.StartCoroutine(SetupColors());
		}

		[HarmonyPatch("Start")]
		[HarmonyPostfix]
		private static void StartPatch()
        {
			SetupEventList();
		}

		public static IEnumerator SetupColors()
        {
			yield return new WaitForSeconds(1f);
			var colorButtons = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/color/color");
			var vignette = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/vignette").transform;
			Destroy(vignette.GetChild(8).gameObject);
			Destroy(vignette.Find("color").gameObject);

			if (eventsCore)
			{
				var cb1 = Instantiate(colorButtons);
				cb1.transform.SetParent(vignette);
				cb1.transform.localScale = Vector3.one;

				var defaultB = Instantiate(cb1.transform.GetChild(cb1.transform.childCount - 1).gameObject);
				defaultB.transform.SetParent(cb1.transform);
				defaultB.transform.localScale = Vector3.one;

				vignetteColorButtons.Clear();
				for (int i = 0; i < 19; i++)
				{
					vignetteColorButtons.Add(cb1.transform.GetChild(i).gameObject.GetComponent<Toggle>());
				}

				var bloom = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/bloom").transform;

				var cb2 = Instantiate(colorButtons);
				cb2.transform.SetParent(bloom);
				cb2.transform.localScale = Vector3.one;

				var defaultB2 = Instantiate(cb2.transform.GetChild(cb2.transform.childCount - 1).gameObject);
				defaultB2.transform.SetParent(cb2.transform);
				defaultB2.transform.localScale = Vector3.one;

				bloomColorButtons.Clear();
				for (int i = 0; i < 19; i++)
				{
					bloomColorButtons.Add(cb2.transform.GetChild(i).gameObject.GetComponent<Toggle>());
				}

				GameObject.Find("TimelineBar/GameObject/6/Background/Text").GetComponent<Text>().text = "Events 1";
				var toggle = Instantiate(GameObject.Find("TimelineBar/GameObject/6"));
				toggle.transform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject").transform);
				toggle.transform.localScale = Vector3.one;
				toggle.transform.SetSiblingIndex(15);
				toggle.name = "events2";
				toggle.transform.Find("Background/Text").GetComponent<Text>().text = "Events 2";

				Triggers.AddTooltip(toggle, "Switch to layer 2", "Toggling this on will switch the event layer to the second event layer.", new List<string>());

				loggle = toggle.GetComponent<Toggle>();
				loggle.onValueChanged.RemoveAllListeners();
				loggle.isOn = false;

				EventTrigger.Entry entryEvent = new EventTrigger.Entry();
				entryEvent.eventID = EventTriggerType.PointerClick;
				entryEvent.callback.AddListener(delegate (BaseEventData eventData)
				{
					PointerEventData pointerEventData = (PointerEventData)eventData;
					if (pointerEventData.clickTime > 0f)
					{
						SetEventLayer(1);
					}
				});
				toggle.GetComponent<EventTrigger>().triggers.Clear();
				toggle.GetComponent<EventTrigger>().triggers.Add(entryEvent);
				var gradient = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/bloom"));
				gradient.transform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right").transform);
				gradient.transform.localScale = Vector3.one;
				gradient.name = "gradient";

				List<GameObject> gameObjects = new List<GameObject>
				{
					gradient.transform.GetChild(12).gameObject,
					gradient.transform.GetChild(13).gameObject,
					gradient.transform.GetChild(14).gameObject,
					gradient.transform.GetChild(15).gameObject,
				};

				foreach (var obj in gameObjects)
				{
					Debug.LogFormat("{0}Deleting obj: {1}", EditorPlugin.className, obj);
					Destroy(obj);
				}

				var color1Label = Instantiate(gradient.transform.GetChild(8).gameObject);
				color1Label.transform.SetParent(gradient.transform);
				color1Label.transform.localScale = Vector3.one;
				color1Label.transform.GetChild(0).GetComponent<Text>().text = "Color Top";
				color1Label.transform.SetSiblingIndex(12);

				var color1 = gradient.transform.Find("color(Clone)").gameObject;
				color1.name = "color1";
				color1.GetComponent<RectTransform>().sizeDelta = new Vector2(366f, 128f);

				var color2Label = Instantiate(gradient.transform.GetChild(8).gameObject);
				color2Label.transform.SetParent(gradient.transform);
				color2Label.transform.localScale = Vector3.one;
				color2Label.transform.GetChild(0).GetComponent<Text>().text = "Color Bottom";

				var color2 = Instantiate(color1);
				color2.transform.SetParent(gradient.transform);
				color2.transform.localScale = Vector3.one;
				color2.name = "color2";

				gradientColor1Buttons.Clear();
				gradientColor2Buttons.Clear();
				for (int i = 0; i < 19; i++)
				{
					gradientColor1Buttons.Add(color1.transform.GetChild(i).gameObject.GetComponent<Toggle>());
					gradientColor2Buttons.Add(color2.transform.GetChild(i).gameObject.GetComponent<Toggle>());
				}

				gradient.transform.GetChild(8).GetChild(0).GetComponent<Text>().text = "Intensity";
				gradient.transform.GetChild(10).GetChild(0).GetComponent<Text>().text = "Rotation";

				var modeLabel = Instantiate(gradient.transform.GetChild(8).gameObject);
				modeLabel.transform.SetParent(gradient.transform);
				modeLabel.transform.localScale = Vector3.one;
				modeLabel.transform.GetChild(0).GetComponent<Text>().text = "Mode";

				var mode = Instantiate(gradient.transform.Find("curves").gameObject);
				mode.transform.SetParent(gradient.transform);
				mode.transform.localScale = Vector3.one;
				mode.name = "mode";
				mode.GetComponent<Dropdown>().options = new List<Dropdown.OptionData>
				{
					new Dropdown.OptionData("Linear"),
					new Dropdown.OptionData("Additive"),
					new Dropdown.OptionData("Multiply"),
					new Dropdown.OptionData("Screen"),
				};

				//Double Vision
				var doubleVision = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/colorsplit"));
				doubleVision.transform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right").transform);
				doubleVision.transform.localScale = Vector3.one;
				doubleVision.name = "doublevision";

				doubleVision.transform.GetChild(8).GetChild(0).GetComponent<Text>().text = "Intensity";

				//Scanlines
				var scanLines = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/radialblur"));
				scanLines.transform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right").transform);
				scanLines.transform.localScale = Vector3.one;
				scanLines.name = "scanlines";

				scanLines.transform.GetChild(8).GetChild(0).GetComponent<Text>().text = "Intensity";
				scanLines.transform.GetChild(10).GetChild(0).GetComponent<Text>().text = "Amount Horizontal";

				var l1 = Instantiate(scanLines.transform.GetChild(10).gameObject);
				l1.transform.SetParent(scanLines.transform);
				l1.transform.localScale = Vector3.one;
				l1.transform.GetChild(0).GetComponent<Text>().text = "Speed";

				var l2 = Instantiate(scanLines.transform.GetChild(11).gameObject);
				l2.transform.SetParent(scanLines.transform);
				l2.transform.localScale = Vector3.one;
				l2.name = "speed";

				//Blur
				var blur = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/radialblur"));
				blur.transform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right").transform);
				blur.transform.localScale = Vector3.one;
				blur.name = "blur";

				//Pixelize
				var pixelize = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/colorsplit"));
				pixelize.transform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right").transform);
				pixelize.transform.localScale = Vector3.one;
				pixelize.name = "pixelize";

				pixelize.transform.GetChild(8).GetChild(0).GetComponent<Text>().text = "Amount";

				//BG
				var bg = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/colorsplit"));
				bg.transform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right").transform);
				bg.transform.localScale = Vector3.one;
				bg.name = "bg";

				bg.transform.GetChild(8).GetChild(0).GetComponent<Text>().text = "BG Color";

				Destroy(bg.transform.Find("offset").gameObject);

				var colorBG = Instantiate(gradient.transform.Find("color1").gameObject);
				colorBG.transform.localScale = Vector3.one;
				colorBG.transform.SetParent(bg.transform);
				colorBG.name = "color";

				bgColorButtons.Clear();
				for (int i = 0; i < 19; i++)
				{
					bgColorButtons.Add(colorBG.transform.GetChild(i).gameObject.GetComponent<Toggle>());
				}

				//Overlay
				var overlay = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/colorsplit"));
				overlay.transform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right").transform);
				overlay.transform.localScale = Vector3.one;
				overlay.name = "invert";

				overlay.transform.GetChild(8).GetChild(0).GetComponent<Text>().text = "Invert Amount";
				overlay.transform.Find("offset").gameObject.name = "alpha";

				//var colorOverlayLabel = Instantiate(overlay.transform.GetChild(8).gameObject);
				//colorOverlayLabel.transform.SetParent(overlay.transform);
				//colorOverlayLabel.transform.localScale = Vector3.one;
				//colorOverlayLabel.transform.GetChild(0).GetComponent<Text>().text = "Overlay Color";

				//var colorOverlay = Instantiate(gradient.transform.Find("color1").gameObject);
				//colorOverlay.transform.SetParent(overlay.transform);
				//colorOverlay.transform.localScale = Vector3.one;
				//colorOverlay.name = "color";

				//overlayColorButtons.Clear();
				//for (int i = 0; i < 19; i++)
				//{
				//	overlayColorButtons.Add(colorOverlay.transform.GetChild(i).gameObject.GetComponent<Toggle>());
				//}

				//Timeline
				var tl = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move"));
				tl.transform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right").transform);
				tl.transform.localScale = Vector3.one;
				tl.name = "timeline";

				var tlScaLabel = Instantiate(tl.transform.GetChild(8).gameObject);
				tlScaLabel.transform.SetParent(tl.transform);
				tlScaLabel.transform.localScale = Vector3.one;
				tlScaLabel.transform.GetChild(0).GetComponent<Text>().text = "Scale X";
				tlScaLabel.transform.GetChild(1).GetComponent<Text>().text = "Scale Y";

				var tlSca = Instantiate(tl.transform.Find("position").gameObject);
				tlSca.transform.SetParent(tl.transform);
				tlSca.transform.localScale = Vector3.one;
				tlSca.name = "scale";

				var tlRotLabel = Instantiate(tl.transform.GetChild(8).gameObject);
				tlRotLabel.transform.SetParent(tl.transform);
				tlRotLabel.transform.localScale = Vector3.one;
				tlRotLabel.transform.GetChild(0).GetComponent<Text>().text = "Rotation";
				Destroy(tlRotLabel.transform.GetChild(1).gameObject);

				var tlRot = Instantiate(overlay.transform.Find("alpha").gameObject);
				tlRot.transform.SetParent(tl.transform);
				tlRot.transform.localScale = Vector3.one;
				tlRot.name = "rotation";

				var tlColLabel = Instantiate(tl.transform.GetChild(8).gameObject);
				tlColLabel.transform.SetParent(tl.transform);
				tlColLabel.transform.localScale = Vector3.one;
				tlColLabel.transform.GetChild(0).GetComponent<Text>().text = "Color";
				Destroy(tlColLabel.transform.GetChild(1).gameObject);

				var colorTimeline = Instantiate(gradient.transform.Find("color1").gameObject);
				colorTimeline.transform.SetParent(tl.transform);
				colorTimeline.transform.localScale = Vector3.one;
				colorTimeline.name = "color";

				timelineColorButtons.Clear();
				for (int i = 0; i < 19; i++)
				{
					timelineColorButtons.Add(colorTimeline.transform.GetChild(i).gameObject.GetComponent<Toggle>());
				}

				var act = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/grain/colored"));
				act.transform.SetParent(tl.transform);
				act.transform.localScale = Vector3.one;
				act.name = "active";
				act.transform.SetSiblingIndex(8);
				act.transform.Find("Text").GetComponent<Text>().text = "Active";

				var tlActLabel = Instantiate(tl.transform.GetChild(9).gameObject);
				tlActLabel.transform.SetParent(tl.transform);
				tlActLabel.transform.localScale = Vector3.one;
				tlActLabel.transform.SetSiblingIndex(8);
				tlActLabel.transform.GetChild(0).GetComponent<Text>().text = "Active";
				Destroy(tlActLabel.transform.GetChild(1).gameObject);

				//Player
				{
					var pp = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move"));
					pp.transform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right").transform);
					pp.transform.localScale = Vector3.one;
					pp.name = "player";

					pp.transform.GetChild(8).GetChild(0).GetComponent<Text>().text = "Velocity";
					pp.transform.GetChild(8).GetChild(1).GetComponent<Text>().text = "Rotation";

					var act2 = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/grain/colored"));
					act2.transform.SetParent(pp.transform);
					act2.transform.localScale = Vector3.one;
					act2.name = "moveable";
					act2.transform.SetSiblingIndex(8);
					act2.transform.Find("Text").GetComponent<Text>().text = "Moveable";

					var tlAct2Label = Instantiate(pp.transform.GetChild(9).gameObject);
					tlAct2Label.transform.SetParent(pp.transform);
					tlAct2Label.transform.localScale = Vector3.one;
					tlAct2Label.transform.SetSiblingIndex(8);
					tlAct2Label.transform.GetChild(0).GetComponent<Text>().text = "Can Move";
					Destroy(tlAct2Label.transform.GetChild(1).gameObject);

					var act3 = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/grain/colored"));
					act3.transform.SetParent(pp.transform);
					act3.transform.localScale = Vector3.one;
					act3.name = "active";
					act3.transform.SetSiblingIndex(8);
					act3.transform.Find("Text").GetComponent<Text>().text = "Active";

					var tlAct3Label = Instantiate(pp.transform.GetChild(9).gameObject);
					tlAct3Label.transform.SetParent(pp.transform);
					tlAct3Label.transform.localScale = Vector3.one;
					tlAct3Label.transform.SetSiblingIndex(8);
					tlAct3Label.transform.GetChild(0).GetComponent<Text>().text = "Active";
					Destroy(tlAct3Label.transform.GetChild(1).gameObject);
				}

				//Follow Player
				{
					var pp = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move"));
					pp.transform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right").transform);
					pp.transform.localScale = Vector3.one;
					pp.name = "follow";

					var limitHLabel = Instantiate(pp.transform.GetChild(8).gameObject);
					var limitH = Instantiate(pp.transform.GetChild(9).gameObject);

					pp.transform.GetChild(8).GetChild(0).GetComponent<Text>().text = "Sharpness";
					pp.transform.GetChild(8).GetChild(1).GetComponent<Text>().text = "Offset";

					var act1 = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/grain/colored"));
					act1.transform.SetParent(pp.transform);
					act1.transform.localScale = Vector3.one;
					act1.name = "rotate";
					act1.transform.SetSiblingIndex(8);
					act1.transform.Find("Text").GetComponent<Text>().text = "Rotate";

					var tlAct1Label = Instantiate(pp.transform.GetChild(9).gameObject);
					tlAct1Label.transform.SetParent(pp.transform);
					tlAct1Label.transform.localScale = Vector3.one;
					tlAct1Label.transform.SetSiblingIndex(8);
					tlAct1Label.transform.GetChild(0).GetComponent<Text>().text = "Rotate Enabled";
					Destroy(tlAct1Label.transform.GetChild(1).gameObject);

					var act2 = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/grain/colored"));
					act2.transform.SetParent(pp.transform);
					act2.transform.localScale = Vector3.one;
					act2.name = "move";
					act2.transform.SetSiblingIndex(8);
					act2.transform.Find("Text").GetComponent<Text>().text = "Move";

					var tlAct2Label = Instantiate(pp.transform.GetChild(9).gameObject);
					tlAct2Label.transform.SetParent(pp.transform);
					tlAct2Label.transform.localScale = Vector3.one;
					tlAct2Label.transform.SetSiblingIndex(8);
					tlAct2Label.transform.GetChild(0).GetComponent<Text>().text = "Move Enabled";
					Destroy(tlAct2Label.transform.GetChild(1).gameObject);

					var act3 = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/grain/colored"));
					act3.transform.SetParent(pp.transform);
					act3.transform.localScale = Vector3.one;
					act3.name = "active";
					act3.transform.SetSiblingIndex(8);
					act3.transform.Find("Text").GetComponent<Text>().text = "Active";

					var tlAct3Label = Instantiate(pp.transform.GetChild(9).gameObject);
					tlAct3Label.transform.SetParent(pp.transform);
					tlAct3Label.transform.localScale = Vector3.one;
					tlAct3Label.transform.SetSiblingIndex(8);
					tlAct3Label.transform.GetChild(0).GetComponent<Text>().text = "Active";
					Destroy(tlAct3Label.transform.GetChild(1).gameObject);

					limitHLabel.transform.SetParent(pp.transform);
					limitHLabel.transform.localScale = Vector3.one;
					limitHLabel.name = "label";

					limitHLabel.transform.GetChild(0).GetComponent<Text>().text = "Left";
					limitHLabel.transform.GetChild(1).GetComponent<Text>().text = "Right";

					limitH.transform.SetParent(pp.transform);
					limitH.transform.localScale = Vector3.one;
					limitH.name = "limit horizontal";

					var limitVLabel = Instantiate(limitHLabel);
					limitVLabel.transform.SetParent(pp.transform);
					limitVLabel.transform.localScale = Vector3.one;
					limitVLabel.name = "label";

					limitVLabel.transform.GetChild(0).GetComponent<Text>().text = "Up";
					limitVLabel.transform.GetChild(1).GetComponent<Text>().text = "Down";

					var limitV = Instantiate(limitH);
					limitV.transform.SetParent(pp.transform);
					limitV.transform.localScale = Vector3.one;
					limitV.name = "limit vertical";

					var anchorLabel = Instantiate(limitHLabel);
					anchorLabel.transform.SetParent(pp.transform);
					anchorLabel.transform.localScale = Vector3.one;
					anchorLabel.name = "label";

					anchorLabel.transform.GetChild(0).GetComponent<Text>().text = "Anchor Multiply";
					Destroy(anchorLabel.transform.GetChild(1).gameObject);

					var anchor = Instantiate(limitH);
					anchor.transform.SetParent(pp.transform);
					anchor.transform.localScale = Vector3.one;
					anchor.name = "anchor";

					Destroy(anchor.transform.GetChild(1).gameObject);
				}

                //Music
                {
					var pp = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move"));
					pp.transform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right").transform);
					pp.transform.localScale = Vector3.one;
					pp.name = "music";

					pp.transform.GetChild(8).GetChild(0).GetComponent<Text>().text = "Pitch";
					pp.transform.GetChild(8).GetChild(1).GetComponent<Text>().text = "Volume";
				}
			}

			yield break;
        }

		public static Toggle loggle;

		[HarmonyPatch("Update")]
		[HarmonyPrefix]
		static bool UpdatePatch(EventEditor __instance)
        {
			if (__instance.previewTheme.objectColors.Count == 9)
			{
				for (int i = 0; i < 9; i++)
				{
					__instance.previewTheme.objectColors.Add(LSColors.pink900);
				}
			}

			if (Input.GetMouseButtonUp(0))
			{
				__instance.eventDrag = false;
			}
			if (__instance.eventDrag)
			{
				foreach (var keyframeSelection in __instance.keyframeSelections)
				{
					if (keyframeSelection.Index != 0)
					{
						float num = EditorManager.inst.GetTimelineTime() + __instance.selectedKeyframeOffsets[__instance.keyframeSelections.IndexOf(keyframeSelection)] + __instance.mouseOffsetXForDrag;
						num = Mathf.Clamp(num, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
						if (SettingEditor.inst.SnapActive)
						{
							num = RTMath.RoundToNearestDecimal(EditorManager.inst.SnapToBPM(num), 3);
						}
						DataManager.inst.gameData.eventObjects.allEvents[keyframeSelection.Type][keyframeSelection.Index].eventTime = num;
					}
				}

				if (preNumber != EditorManager.inst.GetTimelineTime())
				{
					__instance.RenderEventsDialog();
					__instance.UpdateEventOrder();
					__instance.RenderEventObjects();
					//EventManager.inst.updateEvents();
					preNumber = EditorManager.inst.GetTimelineTime();
				}
			}

			if (!doneEvents)
            {
				SetEvents();
            }

			return false;
		}

		public static float preNumber = 0f;
		public static bool doneEvents = false;

		public static void SetEvents()
        {
			if (DataManager.inst.gameData.eventObjects.allEvents.Count > 11)
			{
				//Add event objects
				EventEditor.inst.eventObjects.Add(new List<GameObject>()); //ColorGrading
				EventEditor.inst.eventObjects.Add(new List<GameObject>()); //Ripples
				EventEditor.inst.eventObjects.Add(new List<GameObject>()); //RadialBlur
				EventEditor.inst.eventObjects.Add(new List<GameObject>()); //ColorSplit

				EventEditor.inst.EventColors.Add(LSColors.HexToColor("FF5900"));
				EventEditor.inst.EventColors.Add(LSColors.HexToColor("FF2609"));
				EventEditor.inst.EventColors.Add(LSColors.HexToColor("FF0F0F"));

				EventEditor.inst.EventLabels.transform.GetChild(10).GetComponent<Image>().enabled = true;
				EventEditor.inst.EventLabels.transform.GetChild(10).GetChild(0).GetComponent<Text>().enabled = true;
				EventEditor.inst.EventLabels.transform.GetChild(10).GetChild(0).GetComponent<Text>().text = "ColorGrading";

				EventEditor.inst.EventLabels.transform.GetChild(11).GetComponent<Image>().enabled = true;
				EventEditor.inst.EventLabels.transform.GetChild(11).GetComponent<Image>().color = new Color(0.7267f, 0.3796f, 0f, 1f);
				EventEditor.inst.EventLabels.transform.GetChild(11).GetChild(0).GetComponent<Text>().enabled = true;
				EventEditor.inst.EventLabels.transform.GetChild(11).GetChild(0).GetComponent<Text>().text = "Ripples";

				EventEditor.inst.EventLabels.transform.GetChild(12).GetComponent<Image>().enabled = true;
				EventEditor.inst.EventLabels.transform.GetChild(12).GetComponent<Image>().color = LSColors.HexToColor("B22411");
				EventEditor.inst.EventLabels.transform.GetChild(12).GetChild(0).GetComponent<Text>().enabled = true;
				EventEditor.inst.EventLabels.transform.GetChild(12).GetChild(0).GetComponent<Text>().text = "RadialBlur";

				EventEditor.inst.EventLabels.transform.GetChild(13).GetComponent<Image>().enabled = true;
				EventEditor.inst.EventLabels.transform.GetChild(13).GetComponent<Image>().color = LSColors.HexToColor("B22525");
				EventEditor.inst.EventLabels.transform.GetChild(13).GetChild(0).GetComponent<Text>().enabled = true;
				EventEditor.inst.EventLabels.transform.GetChild(13).GetChild(0).GetComponent<Text>().text = "ColorSplit";
				doneEvents = true;
			}
		}

        [HarmonyPatch("RenderEventsDialog")]
        [HarmonyPrefix]
        static bool RenderEventsDialogPatch(EventEditor __instance)
        {
			Debug.LogFormat("{0}Rendering Events Dialog", EditorPlugin.className);
			var eventManager = EventManager.inst;
            Transform dialogTmp = __instance.dialogRight.GetChild(__instance.currentEventType);
			__instance.dialogLeft.Find("theme").gameObject.SetActive(false);
			var time = dialogTmp.Find("time");
			var timeTime = dialogTmp.Find("time/time").GetComponent<InputField>();
			var timeJumpLeftLarge = dialogTmp.Find("time/<<").GetComponent<Button>();
			var timeJumpLeftSmall = dialogTmp.Find("time/<").GetComponent<Button>();
			var timeJumpRightSmall = dialogTmp.Find("time/>").GetComponent<Button>();
			var timeJumpRightLarge = dialogTmp.Find("time/>>").GetComponent<Button>();

			if (!time.GetComponent<EventTrigger>())
            {
				time.gameObject.AddComponent<EventTrigger>();
            }

			var currentKeyframe = DataManager.inst.gameData.eventObjects.allEvents[__instance.currentEventType][__instance.currentEvent];

			timeTime.onValueChanged.RemoveAllListeners();
			timeTime.text = currentKeyframe.eventTime.ToString("f3");

			if (__instance.currentEvent != 0)
			{
				timeTime.interactable = true;
				timeJumpLeftLarge.interactable = true;
				timeJumpLeftSmall.interactable = true;
				timeJumpRightSmall.interactable = true;
				timeJumpRightLarge.interactable = true;

				timeTime.onValueChanged.AddListener(delegate (string val)
				{
					__instance.SetEventStartTime(float.Parse(val));
				});
				timeJumpLeftLarge.onClick.RemoveAllListeners();
				timeJumpLeftLarge.interactable = (currentKeyframe.eventTime > 0f);
				timeJumpLeftLarge.onClick.AddListener(delegate ()
				{
					timeTime.text = (currentKeyframe.eventTime - 1f).ToString("f3");
				});
				timeJumpLeftSmall.onClick.RemoveAllListeners();
				timeJumpLeftSmall.interactable = (currentKeyframe.eventTime > 0f);
				timeJumpLeftSmall.onClick.AddListener(delegate ()
				{
					timeTime.text = (currentKeyframe.eventTime - 0.1f).ToString("f3");
				});
				timeJumpRightSmall.onClick.RemoveAllListeners();
				timeJumpRightSmall.onClick.AddListener(delegate ()
				{
					timeTime.text = (currentKeyframe.eventTime + 0.1f).ToString("f3");
				});
				timeJumpRightLarge.onClick.RemoveAllListeners();
				timeJumpRightLarge.onClick.AddListener(delegate ()
				{
					timeTime.text = (currentKeyframe.eventTime + 1f).ToString("f3");
				});

				var timeTrigger = time.GetComponent<EventTrigger>();
				timeTrigger.triggers.Add(Triggers.ScrollDelta(timeTime, 0.1f, 10f));
			}
			else
			{
				timeTime.interactable = false;
				timeJumpLeftLarge.interactable = false;
				timeJumpLeftSmall.interactable = false;
				timeJumpRightSmall.interactable = false;
				timeJumpRightLarge.interactable = false;
			}
			switch (__instance.currentEventType)
            {
                case 0: //Move
                    {
                        var posX = dialogTmp.Find("position/x").GetComponent<InputField>();
                        var posY = dialogTmp.Find("position/y").GetComponent<InputField>();

						posX.onValueChanged.RemoveAllListeners();
						posX.text = currentKeyframe.eventValues[0].ToString("f2");
						posX.onValueChanged.AddListener(delegate (string val)
						{
							if (float.TryParse(val, out float num))
							{
								currentKeyframe.eventValues[0] = num;
								eventManager.updateEvents();
							}
							else
                            {
								Debug.LogErrorFormat("{0}Event Value was not in correct format!", EditorPlugin.className);
                            }
						});

						posY.onValueChanged.RemoveAllListeners();
						posY.text = currentKeyframe.eventValues[1].ToString("f2");
						posY.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							currentKeyframe.eventValues[1] = num;
							eventManager.updateEvents();
						});

						Triggers.IncreaseDecreaseButtons(posX, 1f, 10f);
						Triggers.IncreaseDecreaseButtons(posY, 1f, 10f);
						Triggers.AddEventTrigger(posX.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(posX, 0.1f, 10f, true), Triggers.ScrollDeltaVector2(posX, posY, 0.1f, 10f) });
						Triggers.AddEventTrigger(posY.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(posY, 0.1f, 10f, true), Triggers.ScrollDeltaVector2(posX, posY, 0.1f, 10f) });

						if (!posX.gameObject.GetComponent<InputFieldHelper>())
                        {
							posX.gameObject.AddComponent<InputFieldHelper>();
                        }
						if (!posY.gameObject.GetComponent<InputFieldHelper>())
                        {
							posY.gameObject.AddComponent<InputFieldHelper>();
                        }
                        break;
                    }
				case 1: //Zoom
                    {
						var zoom = dialogTmp.Find("zoom/x").GetComponent<InputField>();
						zoom.onValueChanged.RemoveAllListeners();
						zoom.text = currentKeyframe.eventValues[0].ToString("f2");
						zoom.onValueChanged.AddListener(delegate (string val)
						{
							if (float.TryParse(val, out float num))
							{
								num = Mathf.Clamp(num, -9999f, 9999f);
								currentKeyframe.eventValues[0] = num;
								eventManager.updateEvents();
							}
							else
                            {
								Debug.LogErrorFormat("{0}Event Value was not in correct format!", EditorPlugin.className);
							}
						});

						Triggers.IncreaseDecreaseButtons(zoom, 1f, 10f, null, new List<float> { -9999f, 9999f });
						Triggers.AddEventTrigger(zoom.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(zoom, 0.1f, 10f, false, new List<float> { -9999f, 9999f }) });

						if (!zoom.gameObject.GetComponent<InputFieldHelper>())
						{
							zoom.gameObject.AddComponent<InputFieldHelper>();
						}
						break;
                    }
				case 2: //Rotate
                    {
						var rotate = dialogTmp.Find("rotation/x").GetComponent<InputField>();
						rotate.onValueChanged.RemoveAllListeners();
						rotate.text = currentKeyframe.eventValues[0].ToString("f2");
						rotate.onValueChanged.AddListener(delegate (string val)
						{
							currentKeyframe.eventValues[0] = float.Parse(val);
							eventManager.updateEvents();
						});

						Triggers.IncreaseDecreaseButtons(rotate, 15f, 3f);
						Triggers.AddEventTrigger(rotate.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(rotate, 15f, 3f) });

						if (!rotate.gameObject.GetComponent<InputFieldHelper>())
						{
							rotate.gameObject.AddComponent<InputFieldHelper>();
						}
						break;
                    }
				case 3: //Shake
                    {
						//Shake Intensity
						{
							var shake = dialogTmp.Find("shake/x").GetComponent<InputField>();
							shake.onValueChanged.RemoveAllListeners();
							shake.text = currentKeyframe.eventValues[0].ToString("f2");
							shake.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								num = Mathf.Clamp(num, 0f, 10f);
								currentKeyframe.eventValues[0] = num;
								eventManager.updateEvents();
							});

							Triggers.IncreaseDecreaseButtons(shake, 1f, 10f, null, new List<float> { 0f, 10f });
							Triggers.AddEventTrigger(shake.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(shake, 0.1f, 10f, false, new List<float> { 0f, 10f }) });

							if (!shake.gameObject.GetComponent<InputFieldHelper>())
							{
								shake.gameObject.AddComponent<InputFieldHelper>();
							}
						}
						if (eventsCore)
						{
							//Shake Intensity X / Y
							{
								var xif = dialogTmp.Find("direction/x").GetComponent<InputField>();
								xif.onValueChanged.RemoveAllListeners();
								xif.text = currentKeyframe.eventValues[1].ToString("f2");
								xif.onValueChanged.AddListener(delegate (string val)
								{
									float num = float.Parse(val);
									num = Mathf.Clamp(num, -10f, 10f);
									currentKeyframe.eventValues[1] = num;
									eventManager.updateEvents();
								});

								var yif = dialogTmp.Find("direction/y").GetComponent<InputField>();
								yif.onValueChanged.RemoveAllListeners();
								yif.text = currentKeyframe.eventValues[2].ToString("f2");
								yif.onValueChanged.AddListener(delegate (string val)
								{
									float num = float.Parse(val);
									currentKeyframe.eventValues[2] = num;
									eventManager.updateEvents();
								});

								Triggers.IncreaseDecreaseButtons(xif, 1f, 10f, null, new List<float> { -10f, 10f });
								Triggers.IncreaseDecreaseButtons(yif, 1f, 10f, null, new List<float> { -10f, 10f });
								Triggers.AddEventTrigger(xif.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(xif, 0.1f, 10f, true, new List<float> { -10f, 10f }), Triggers.ScrollDeltaVector2(xif, yif, 0.1f, 10f, new List<float> { -10f, 10f }) });
								Triggers.AddEventTrigger(yif.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(yif, 0.1f, 10f, true, new List<float> { -10f, 10f }), Triggers.ScrollDeltaVector2(xif, yif, 0.1f, 10f, new List<float> { -10f, 10f }) });

								if (!xif.gameObject.GetComponent<InputFieldHelper>())
								{
									xif.gameObject.AddComponent<InputFieldHelper>();
								}

								if (!yif.gameObject.GetComponent<InputFieldHelper>())
								{
									yif.gameObject.AddComponent<InputFieldHelper>();
								}
							}

						}
						break;
                    }
				case 4: //Theme
                    {
						var theme = dialogTmp.Find("theme-search").GetComponent<InputField>();

						theme.onValueChanged.RemoveAllListeners();
						theme.onValueChanged.AddListener(delegate (string val)
						{
							RenderThemeContentPatch(dialogTmp, val);
						});
						RenderThemeContentPatch(dialogTmp, theme.text);
						__instance.RenderThemePreview(dialogTmp);
						break;
                    }
				case 5: //Chromatic
                    {
						var inputField = dialogTmp.Find("chroma/x").GetComponent<InputField>();
						inputField.onValueChanged.RemoveAllListeners();
						inputField.text = currentKeyframe.eventValues[0].ToString("f2");
						inputField.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							num = Mathf.Clamp(num, 0f, float.PositiveInfinity);
							currentKeyframe.eventValues[0] = num;
							eventManager.updateEvents();
						});

						Triggers.IncreaseDecreaseButtons(inputField, 1f, 10f, null, new List<float> { 0f, float.PositiveInfinity });
						Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 0.1f, 10f, false, new List<float> { 0f, float.PositiveInfinity }) });

						if (!inputField.gameObject.GetComponent<InputFieldHelper>())
						{
							inputField.gameObject.AddComponent<InputFieldHelper>();
						}
						break;
                    }
				case 6: //Bloom
                    {
						//Bloom Intensity
						{
							var bloom = dialogTmp.Find("bloom/x").GetComponent<InputField>();
							bloom.onValueChanged.RemoveAllListeners();
							bloom.text = currentKeyframe.eventValues[0].ToString("f2");
							bloom.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								num = Mathf.Clamp(num, 0f, 1280f);
								currentKeyframe.eventValues[0] = num;
								eventManager.updateEvents();
							});

							Triggers.IncreaseDecreaseButtons(bloom, 1f, 10f, null, new List<float> { 0f, 1280 });
							Triggers.AddEventTrigger(bloom.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(bloom, 0.1f, 10f, false, new List<float> { 0f, 1280f }) });

							if (!bloom.gameObject.GetComponent<InputFieldHelper>())
							{
								bloom.gameObject.AddComponent<InputFieldHelper>();
							}
						}

						if (eventsCore)
						{
							//Bloom Diffusion
							{
								var bloom = dialogTmp.Find("diffusion").GetComponent<InputField>();
								bloom.onValueChanged.RemoveAllListeners();
								bloom.text = currentKeyframe.eventValues[1].ToString("f2");
								bloom.onValueChanged.AddListener(delegate (string val)
								{
									float num = float.Parse(val);
									num = Mathf.Clamp(num, 1f, float.PositiveInfinity);
									currentKeyframe.eventValues[1] = num;
									eventManager.updateEvents();
								});

								Triggers.IncreaseDecreaseButtons(bloom, 1f, 10f, null, new List<float> { 1f, float.PositiveInfinity });
								Triggers.AddEventTrigger(bloom.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(bloom, 0.1f, 10f, false, new List<float> { 1f, float.PositiveInfinity }) });

								if (!bloom.gameObject.GetComponent<InputFieldHelper>())
								{
									bloom.gameObject.AddComponent<InputFieldHelper>();
								}
							}

							//Bloom Threshold
							{
								var bloom = dialogTmp.Find("threshold").GetComponent<InputField>();
								bloom.onValueChanged.RemoveAllListeners();
								bloom.text = currentKeyframe.eventValues[2].ToString("f2");
								bloom.onValueChanged.AddListener(delegate (string val)
								{
									float num = float.Parse(val);
									num = Mathf.Clamp(num, 0f, 1.4f);
									currentKeyframe.eventValues[2] = num;
									eventManager.updateEvents();
								});

								Triggers.IncreaseDecreaseButtons(bloom, 1f, 10f, null, new List<float> { 0f, 1.4f });
								Triggers.AddEventTrigger(bloom.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(bloom, 0.1f, 10f, false, new List<float> { 0f, 1.4f }) });

								if (!bloom.gameObject.GetComponent<InputFieldHelper>())
								{
									bloom.gameObject.AddComponent<InputFieldHelper>();
								}
							}

							//Bloom Anamorphic Ratio
							{
								var bloom = dialogTmp.Find("anamorphic ratio").GetComponent<InputField>();
								bloom.onValueChanged.RemoveAllListeners();
								bloom.text = currentKeyframe.eventValues[3].ToString("f2");
								bloom.onValueChanged.AddListener(delegate (string val)
								{
									float num = float.Parse(val);
									num = Mathf.Clamp(num, -1f, 1f);
									currentKeyframe.eventValues[3] = num;
									eventManager.updateEvents();
								});

								var bloomLeft = bloom.transform.Find("<").GetComponent<Button>();
								var bloomRight = bloom.transform.Find(">").GetComponent<Button>();

								Triggers.IncreaseDecreaseButtons(bloom, 1f, 10f, null, new List<float> { -1f, 1f });
								Triggers.AddEventTrigger(bloom.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(bloom, 0.1f, 10f, false, new List<float> { -1f, 1f }) });

								if (!bloom.gameObject.GetComponent<InputFieldHelper>())
								{
									bloom.gameObject.AddComponent<InputFieldHelper>();
								}
							}

							int num = 0;
							foreach (Toggle toggle in bloomColorButtons)
							{
								if (num < 18)
								{
									toggle.GetComponent<Image>().color = GameManager.inst.LiveTheme.objectColors[num];
								}
								else
								{
									toggle.GetComponent<Image>().color = Color.white;
								}
								toggle.onValueChanged.RemoveAllListeners();
								if (num == currentKeyframe.eventValues[4])
								{
									toggle.isOn = true;
								}
								else
								{
									toggle.isOn = false;
								}
								int tmpIndex = num;
								toggle.onValueChanged.AddListener(delegate (bool val)
								{
									SetBloomColor(tmpIndex);
								});
								num++;
							}
						}
						break;
                    }
				case 7: //Vignette
                    {
						//Intensity
						{
							var inputField = dialogTmp.Find("intensity").GetComponent<InputField>();

							inputField.onValueChanged.RemoveAllListeners();
							inputField.text = currentKeyframe.eventValues[0].ToString("f2");
							inputField.onValueChanged.AddListener(delegate (string val)
							{
								if (float.TryParse(val, out float num))
								{
									num = Mathf.Clamp(num, 0f, float.PositiveInfinity);
									currentKeyframe.eventValues[0] = num;
									eventManager.updateEvents();
								}
								else
								{
									Debug.LogErrorFormat("{0}Event Value was not in correct format!", EditorPlugin.className);
								}
							});

							Triggers.IncreaseDecreaseButtons(inputField, 1f, 10f, null, new List<float> { 0f, float.PositiveInfinity });
							Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 0.1f, 10f, false, new List<float> { 0f, float.PositiveInfinity }) });

							if (!inputField.gameObject.GetComponent<InputFieldHelper>())
							{
								inputField.gameObject.AddComponent<InputFieldHelper>();
							}
						}

						//Smoothness
						{
							var inputField = dialogTmp.Find("smoothness").GetComponent<InputField>();

							inputField.onValueChanged.RemoveAllListeners();
							inputField.text = currentKeyframe.eventValues[1].ToString("f2");
							inputField.onValueChanged.AddListener(delegate (string val)
							{
								currentKeyframe.eventValues[1] = float.Parse(val);
								eventManager.updateEvents();
							});

							Triggers.IncreaseDecreaseButtons(inputField, 1f, 10f);
							Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 0.1f, 10f) });

							if (!inputField.gameObject.GetComponent<InputFieldHelper>())
							{
								inputField.gameObject.AddComponent<InputFieldHelper>();
							}
						}

						//Rounded
						{
							var vignetteRounded = dialogTmp.Find("roundness/rounded").GetComponent<Toggle>();
							vignetteRounded.onValueChanged.RemoveAllListeners();
							vignetteRounded.isOn = (currentKeyframe.eventValues[2] == 1f);
							vignetteRounded.onValueChanged.AddListener(delegate (bool val)
							{
								currentKeyframe.eventValues[2] = (val ? 1 : 0);
								eventManager.updateEvents();
							});
						}

						//Roundness
						{
							var inputField = dialogTmp.Find("roundness").GetComponent<InputField>();

							inputField.onValueChanged.RemoveAllListeners();
							inputField.text = currentKeyframe.eventValues[3].ToString("f2");
							inputField.onValueChanged.AddListener(delegate (string val)
							{
								if (float.TryParse(val, out float num))
								{
									num = Mathf.Clamp(num, float.NegativeInfinity, 1.2f);
									currentKeyframe.eventValues[3] = num;
									eventManager.updateEvents();
								}
								else
								{
									Debug.LogErrorFormat("{0}Event Value was not in correct format!", EditorPlugin.className);
								}
							});

							Triggers.IncreaseDecreaseButtons(inputField, 0.1f, 10f, null, new List<float> { float.NegativeInfinity, 1.2f });
							Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 0.1f, 10f, false, new List<float> { float.NegativeInfinity, 1.2f }) });

							if (!inputField.gameObject.GetComponent<InputFieldHelper>())
							{
								inputField.gameObject.AddComponent<InputFieldHelper>();
							}
						}

						//Center
						{
							var xif = dialogTmp.Find("position/x").GetComponent<InputField>();
							var yif = dialogTmp.Find("position/y").GetComponent<InputField>();

							xif.onValueChanged.RemoveAllListeners();
							xif.text = currentKeyframe.eventValues[4].ToString("f2");
							xif.onValueChanged.AddListener(delegate (string val)
							{
								currentKeyframe.eventValues[4] = float.Parse(val);
								eventManager.updateEvents();
							});

							yif.onValueChanged.RemoveAllListeners();
							yif.text = currentKeyframe.eventValues[5].ToString("f2");
							yif.onValueChanged.AddListener(delegate (string val)
							{
								currentKeyframe.eventValues[5] = float.Parse(val);
								eventManager.updateEvents();
							});

							Triggers.IncreaseDecreaseButtons(xif, 1f, 10f);
							Triggers.IncreaseDecreaseButtons(yif, 1f, 10f);
							Triggers.AddEventTrigger(xif.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(xif, 0.1f, 10f, true), Triggers.ScrollDeltaVector2(xif, yif, 0.1f, 10f) });
							Triggers.AddEventTrigger(yif.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(yif, 0.1f, 10f, true), Triggers.ScrollDeltaVector2(xif, yif, 0.1f, 10f) });

							if (!xif.gameObject.GetComponent<InputFieldHelper>())
							{
								xif.gameObject.AddComponent<InputFieldHelper>();
							}
							if (!yif.gameObject.GetComponent<InputFieldHelper>())
							{
								yif.gameObject.AddComponent<InputFieldHelper>();
							}
						}

						if (eventsCore)
						{
							int num = 0;
							foreach (Toggle toggle in vignetteColorButtons)
							{
								if (num < 18)
								{
									toggle.GetComponent<Image>().color = GameManager.inst.LiveTheme.objectColors[num];
								}
								else
								{
									toggle.GetComponent<Image>().color = Color.black;
								}
								toggle.onValueChanged.RemoveAllListeners();
								if (num == currentKeyframe.eventValues[6])
								{
									toggle.isOn = true;
								}
								else
								{
									toggle.isOn = false;
								}
								int tmpIndex = num;
								toggle.onValueChanged.AddListener(delegate (bool val)
								{
									SetVignetteColor(tmpIndex);
								});
								num++;
							}
						}
						break;
                    }
				case 8: //Lens
                    {
						//Intensity
						{
							var inputField = dialogTmp.Find("lens/x").GetComponent<InputField>();
							inputField.onValueChanged.RemoveAllListeners();
							inputField.text = currentKeyframe.eventValues[0].ToString("f2");
							inputField.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								num = Mathf.Clamp(num, -100f, 100f);
								currentKeyframe.eventValues[0] = num;
								eventManager.updateEvents();
							});

							Triggers.IncreaseDecreaseButtons(inputField, 1f, 10f, null, new List<float> { -100f, 100f });
							Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 1f, 10f, false, new List<float> { -100f, 100f }) });

							if (!inputField.gameObject.GetComponent<InputFieldHelper>())
							{
								inputField.gameObject.AddComponent<InputFieldHelper>();
							}
						}

						if (eventsCore)
						{
							//Center X / Y
							{
								var xif = dialogTmp.Find("center/x").GetComponent<InputField>();
								xif.onValueChanged.RemoveAllListeners();
								xif.text = currentKeyframe.eventValues[1].ToString("f2");
								xif.onValueChanged.AddListener(delegate (string val)
								{
									float num = float.Parse(val);
									currentKeyframe.eventValues[1] = num;
									eventManager.updateEvents();
								});

								var yif = dialogTmp.Find("center/y").GetComponent<InputField>();
								yif.onValueChanged.RemoveAllListeners();
								yif.text = currentKeyframe.eventValues[2].ToString("f2");
								yif.onValueChanged.AddListener(delegate (string val)
								{
									float num = float.Parse(val);
									currentKeyframe.eventValues[2] = num;
									eventManager.updateEvents();
								});

								Triggers.IncreaseDecreaseButtons(xif, 1f, 10f);
								Triggers.IncreaseDecreaseButtons(yif, 1f, 10f);
								Triggers.AddEventTrigger(xif.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(xif, 0.1f, 10f, true), Triggers.ScrollDeltaVector2(xif, yif, 0.1f, 10f) });
								Triggers.AddEventTrigger(yif.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(yif, 0.1f, 10f, true), Triggers.ScrollDeltaVector2(xif, yif, 0.1f, 10f) });

								if (!xif.gameObject.GetComponent<InputFieldHelper>())
								{
									xif.gameObject.AddComponent<InputFieldHelper>();
								}

								if (!yif.gameObject.GetComponent<InputFieldHelper>())
								{
									yif.gameObject.AddComponent<InputFieldHelper>();
								}
							}

							//Intensity X / Y
							{
								var xif = dialogTmp.Find("intensity/x").GetComponent<InputField>();
								xif.onValueChanged.RemoveAllListeners();
								xif.text = currentKeyframe.eventValues[3].ToString("f2");
								xif.onValueChanged.AddListener(delegate (string val)
								{
									float num = float.Parse(val);
									num = Mathf.Clamp(num, 0f, float.PositiveInfinity);
									currentKeyframe.eventValues[3] = num;
									eventManager.updateEvents();
								});

								var yif = dialogTmp.Find("intensity/y").GetComponent<InputField>();
								yif.onValueChanged.RemoveAllListeners();
								yif.text = currentKeyframe.eventValues[4].ToString("f2");
								yif.onValueChanged.AddListener(delegate (string val)
								{
									float num = float.Parse(val);
									num = Mathf.Clamp(num, 0f, float.PositiveInfinity);
									currentKeyframe.eventValues[4] = num;
									eventManager.updateEvents();
								});

								Triggers.IncreaseDecreaseButtons(xif, 1f, 10f, null, new List<float> { 0f, float.PositiveInfinity });
								Triggers.IncreaseDecreaseButtons(yif, 1f, 10f, null, new List<float> { 0f, float.PositiveInfinity });
								Triggers.AddEventTrigger(xif.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(xif, 0.1f, 10f, false, new List<float> { 0f, float.PositiveInfinity }), Triggers.ScrollDeltaVector2(xif, yif, 0.1f, 10f, new List<float> { 0f, float.PositiveInfinity }) });
								Triggers.AddEventTrigger(yif.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(yif, 0.1f, 10f, false, new List<float> { 0f, float.PositiveInfinity }), Triggers.ScrollDeltaVector2(xif, yif, 0.1f, 10f, new List<float> { 0f, float.PositiveInfinity }) });

								if (!xif.gameObject.GetComponent<InputFieldHelper>())
								{
									xif.gameObject.AddComponent<InputFieldHelper>();
								}

								if (!yif.gameObject.GetComponent<InputFieldHelper>())
								{
									yif.gameObject.AddComponent<InputFieldHelper>();
								}
							}

							//Scale
							{
								var inputField = dialogTmp.Find("scale").GetComponent<InputField>();
								inputField.onValueChanged.RemoveAllListeners();
								inputField.text = currentKeyframe.eventValues[5].ToString("f2");
								inputField.onValueChanged.AddListener(delegate (string val)
								{
									float num = float.Parse(val);
									num = Mathf.Clamp(num, 0.001f, float.PositiveInfinity);
									currentKeyframe.eventValues[5] = num;
									eventManager.updateEvents();
								});
								
								Triggers.IncreaseDecreaseButtons(inputField, 1f, 10f, null, new List<float> { 0.001f, float.PositiveInfinity });
								Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 0.1f, 10f, false, new List<float> { 0.001f, float.PositiveInfinity }) });

								if (!inputField.gameObject.GetComponent<InputFieldHelper>())
								{
									inputField.gameObject.AddComponent<InputFieldHelper>();
								}
							}
						}
						break;
                    }
				case 9: //Grain
                    {
						//Grain
						{
							var inputField = dialogTmp.Find("intensity").GetComponent<InputField>();
							inputField.onValueChanged.RemoveAllListeners();
							inputField.text = currentKeyframe.eventValues[0].ToString("f2");
							inputField.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								num = Mathf.Clamp(num, 0f, float.PositiveInfinity);
								currentKeyframe.eventValues[0] = num;
								eventManager.updateEvents();
							});
							
							Triggers.IncreaseDecreaseButtons(inputField, 1f, 10f, null, new List<float> { 0f, float.PositiveInfinity });
							Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 0.1f, 10f, false, new List<float> { 0f, float.PositiveInfinity }) });

							if (!inputField.gameObject.GetComponent<InputFieldHelper>())
							{
								inputField.gameObject.AddComponent<InputFieldHelper>();
							}
						}

						//Colored
						{
							var grainColored = dialogTmp.Find("colored").GetComponent<Toggle>();
							grainColored.onValueChanged.RemoveAllListeners();
							grainColored.isOn = (currentKeyframe.eventValues[1] == 1f);
							grainColored.onValueChanged.AddListener(delegate (bool val)
							{
								currentKeyframe.eventValues[1] = (float)(val ? 1 : 0);
								eventManager.updateEvents();
							});
						}

						//Size
						{
							var inputField = dialogTmp.Find("size").GetComponent<InputField>();
							inputField.onValueChanged.RemoveAllListeners();
							inputField.text = currentKeyframe.eventValues[2].ToString("f2");
							inputField.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								num = Mathf.Clamp(num, 0f, float.PositiveInfinity);
								currentKeyframe.eventValues[2] = num;
								eventManager.updateEvents();
							});

							Triggers.IncreaseDecreaseButtons(inputField, 1f, 10f, null, new List<float> { 0f, float.PositiveInfinity });
							Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 0.1f, 10f, false, new List<float> { 0f, float.PositiveInfinity }) });

							if (!inputField.gameObject.GetComponent<InputFieldHelper>())
							{
								inputField.gameObject.AddComponent<InputFieldHelper>();
							}
						}
						break;
					}
				case 10: //ColorGrading
					{
						//Hueshift
						{
							var inputField = dialogTmp.Find("intensity").GetComponent<InputField>();
							inputField.onValueChanged.RemoveAllListeners();
							inputField.text = currentKeyframe.eventValues[0].ToString("f2");
							inputField.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								currentKeyframe.eventValues[0] = num;
								eventManager.updateEvents();
							});

							Triggers.IncreaseDecreaseButtons(inputField, 1f, 10f);
							Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 0.1f, 10f) });

							if (!inputField.gameObject.GetComponent<InputFieldHelper>())
							{
								inputField.gameObject.AddComponent<InputFieldHelper>();
							}
						}

						//Contrast
						{
							var inputField = dialogTmp.Find("contrast").GetComponent<InputField>();
							inputField.onValueChanged.RemoveAllListeners();
							inputField.text = currentKeyframe.eventValues[1].ToString("f2");
							inputField.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								currentKeyframe.eventValues[1] = num;
								eventManager.updateEvents();
							});

							Triggers.IncreaseDecreaseButtons(inputField, 1f, 10f);
							Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 0.1f, 10f) });

							if (!inputField.gameObject.GetComponent<InputFieldHelper>())
							{
								inputField.gameObject.AddComponent<InputFieldHelper>();
							}
						}

						//Saturation
						{
							var inputField = dialogTmp.Find("saturation").GetComponent<InputField>();
							inputField.onValueChanged.RemoveAllListeners();
							inputField.text = currentKeyframe.eventValues[6].ToString("f2");
							inputField.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								currentKeyframe.eventValues[6] = num;
								eventManager.updateEvents();
							});

							Triggers.IncreaseDecreaseButtons(inputField, 1f, 10f);
							Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 0.1f, 10f) });

							if (!inputField.gameObject.GetComponent<InputFieldHelper>())
							{
								inputField.gameObject.AddComponent<InputFieldHelper>();
							}
						}

						//Temperature
						{
							var inputField = dialogTmp.Find("temperature").GetComponent<InputField>();
							inputField.onValueChanged.RemoveAllListeners();
							inputField.text = currentKeyframe.eventValues[7].ToString("f2");
							inputField.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								currentKeyframe.eventValues[7] = num;
								eventManager.updateEvents();
							});

							Triggers.IncreaseDecreaseButtons(inputField, 1f, 10f);
							Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 0.1f, 10f) });

							if (!inputField.gameObject.GetComponent<InputFieldHelper>())
							{
								inputField.gameObject.AddComponent<InputFieldHelper>();
							}
						}

						//Tint
						{
							var inputField = dialogTmp.Find("tint").GetComponent<InputField>();
							inputField.onValueChanged.RemoveAllListeners();
							inputField.text = currentKeyframe.eventValues[8].ToString("f2");
							inputField.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								currentKeyframe.eventValues[8] = num;
								eventManager.updateEvents();
							});

							Triggers.IncreaseDecreaseButtons(inputField, 1f, 10f);
							Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 0.1f, 10f) });

							if (!inputField.gameObject.GetComponent<InputFieldHelper>())
							{
								inputField.gameObject.AddComponent<InputFieldHelper>();
							}
						}
						break;
					}
				case 11: //Ripples
					{
						//Strength
						{
							var inputField = dialogTmp.Find("strength").GetComponent<InputField>();
							inputField.onValueChanged.RemoveAllListeners();
							inputField.text = currentKeyframe.eventValues[0].ToString("f2");
							inputField.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								currentKeyframe.eventValues[0] = num;
								eventManager.updateEvents();
							});

							Triggers.IncreaseDecreaseButtons(inputField, 1f, 10f);
							Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 0.1f, 10f) });

							if (!inputField.gameObject.GetComponent<InputFieldHelper>())
							{
								inputField.gameObject.AddComponent<InputFieldHelper>();
							}
						}

						//Speed
						{
							var inputField = dialogTmp.Find("speed").GetComponent<InputField>();
							inputField.onValueChanged.RemoveAllListeners();
							inputField.text = currentKeyframe.eventValues[1].ToString("f2");
							inputField.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								currentKeyframe.eventValues[1] = num;
								eventManager.updateEvents();
							});

							Triggers.IncreaseDecreaseButtons(inputField, 1f, 10f);
							Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 0.1f, 10f) });

							if (!inputField.gameObject.GetComponent<InputFieldHelper>())
							{
								inputField.gameObject.AddComponent<InputFieldHelper>();
							}
						}

						//Distance
						{
							var inputField = dialogTmp.Find("distance").GetComponent<InputField>();
							inputField.onValueChanged.RemoveAllListeners();
							inputField.text = currentKeyframe.eventValues[2].ToString("f2");
							inputField.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								num = Mathf.Clamp(num, 0.001f,float.PositiveInfinity);
								currentKeyframe.eventValues[2] = num;
								eventManager.updateEvents();
							});

							Triggers.IncreaseDecreaseButtons(inputField, 1f, 10f, null, new List<float> { 0.001f, float.PositiveInfinity });
							Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 0.1f, 10f, false, new List<float> { 0.001f, float.PositiveInfinity }) });

							if (!inputField.gameObject.GetComponent<InputFieldHelper>())
							{
								inputField.gameObject.AddComponent<InputFieldHelper>();
							}
						}

						//Height
						{
							var inputField = dialogTmp.Find("height").GetComponent<InputField>();
							inputField.onValueChanged.RemoveAllListeners();
							inputField.text = currentKeyframe.eventValues[3].ToString("f2");
							inputField.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								currentKeyframe.eventValues[3] = num;
								eventManager.updateEvents();
							});

							Triggers.IncreaseDecreaseButtons(inputField, 1f, 10f);
							Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 0.1f, 10f) });

							if (!inputField.gameObject.GetComponent<InputFieldHelper>())
							{
								inputField.gameObject.AddComponent<InputFieldHelper>();
							}
						}

						//Width
						{
							var inputField = dialogTmp.Find("width").GetComponent<InputField>();
							inputField.onValueChanged.RemoveAllListeners();
							inputField.text = currentKeyframe.eventValues[4].ToString("f2");
							inputField.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								currentKeyframe.eventValues[4] = num;
								eventManager.updateEvents();
							});

							Triggers.IncreaseDecreaseButtons(inputField, 1f, 10f);
							Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 0.1f, 10f) });

							if (!inputField.gameObject.GetComponent<InputFieldHelper>())
							{
								inputField.gameObject.AddComponent<InputFieldHelper>();
							}
						}
						break;
					}
				case 12: //RadialBlur
					{
						//RadialBlur
						{
							var inputField = dialogTmp.Find("intensity").GetComponent<InputField>();
							inputField.onValueChanged.RemoveAllListeners();
							inputField.text = currentKeyframe.eventValues[0].ToString("f2");
							inputField.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								currentKeyframe.eventValues[0] = num;
								eventManager.updateEvents();
							});

							Triggers.IncreaseDecreaseButtons(inputField, 1f, 10f);
							Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 0.1f, 10f) });

							if (!inputField.gameObject.GetComponent<InputFieldHelper>())
							{
								inputField.gameObject.AddComponent<InputFieldHelper>();
							}
						}

						//Iterations
						{
							var inputField = dialogTmp.Find("iterations").GetComponent<InputField>();
							inputField.onValueChanged.RemoveAllListeners();
							inputField.text = currentKeyframe.eventValues[1].ToString();
							inputField.onValueChanged.AddListener(delegate (string val)
							{
								int num = int.Parse(val);
								num = Mathf.Clamp(num, 1, 20);
								currentKeyframe.eventValues[1] = num;
								eventManager.updateEvents();
							});

							Triggers.IncreaseDecreaseButtonsInt(inputField, 1, null, new List<int> { 1, 20 });
							Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDeltaInt(inputField, 1, false, new List<int> { 1, 20 }) });
						}
						break;
					}
				case 13: //ColorSplit
					{
						//ColorSplit
						var inputField = dialogTmp.Find("offset").GetComponent<InputField>();
						inputField.onValueChanged.RemoveAllListeners();
						inputField.text = currentKeyframe.eventValues[0].ToString("f2");
						inputField.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							currentKeyframe.eventValues[0] = num;
							eventManager.updateEvents();
						});

						Triggers.IncreaseDecreaseButtons(inputField, 1f, 10f);
						Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 0.1f, 10f) });

						if (!inputField.gameObject.GetComponent<InputFieldHelper>())
						{
							inputField.gameObject.AddComponent<InputFieldHelper>();
						}
						break;
					}
				case 14: //Cam Offset
					{
						var posX = dialogTmp.Find("position/x").GetComponent<InputField>();
						var posY = dialogTmp.Find("position/y").GetComponent<InputField>();

						posX.onValueChanged.RemoveAllListeners();
						posX.text = currentKeyframe.eventValues[0].ToString("f2");
						posX.onValueChanged.AddListener(delegate (string val)
						{
							if (float.TryParse(val, out float num))
							{
								currentKeyframe.eventValues[0] = num;
								eventManager.updateEvents();
							}
							else
							{
								Debug.LogErrorFormat("{0}Event Value was not in correct format!", EditorPlugin.className);
							}
						});

						posY.onValueChanged.RemoveAllListeners();
						posY.text = currentKeyframe.eventValues[1].ToString("f2");
						posY.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							currentKeyframe.eventValues[1] = num;
							eventManager.updateEvents();
						});

						Triggers.IncreaseDecreaseButtons(posX, 1f, 10f);
						Triggers.IncreaseDecreaseButtons(posY, 1f, 10f);
						Triggers.AddEventTrigger(posX.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(posX, 0.1f, 10f, true), Triggers.ScrollDeltaVector2(posX, posY, 0.1f, 10f) });
						Triggers.AddEventTrigger(posY.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(posY, 0.1f, 10f, true), Triggers.ScrollDeltaVector2(posX, posY, 0.1f, 10f) });

						if (!posX.gameObject.GetComponent<InputFieldHelper>())
						{
							posX.gameObject.AddComponent<InputFieldHelper>();
						}
						if (!posY.gameObject.GetComponent<InputFieldHelper>())
						{
							posY.gameObject.AddComponent<InputFieldHelper>();
						}
						break;
                    }
				case 15: //Gradient
					{
						//Gradient Intensity
						{
							var inputField = dialogTmp.Find("bloom/x").GetComponent<InputField>();
							inputField.onValueChanged.RemoveAllListeners();
							inputField.text = currentKeyframe.eventValues[0].ToString("f2");
							inputField.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								currentKeyframe.eventValues[0] = num;
								eventManager.updateEvents();
							});

							Triggers.IncreaseDecreaseButtons(inputField, 1f, 10f);
							Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 0.1f, 10f) });

							if (!inputField.gameObject.GetComponent<InputFieldHelper>())
							{
								inputField.gameObject.AddComponent<InputFieldHelper>();
							}
						}

						//Gradient Rotation
						{
							var inputField = dialogTmp.Find("diffusion").GetComponent<InputField>();
							inputField.onValueChanged.RemoveAllListeners();
							inputField.text = currentKeyframe.eventValues[1].ToString("f2");
							inputField.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								currentKeyframe.eventValues[1] = num;
								eventManager.updateEvents();
							});

							Triggers.IncreaseDecreaseButtons(inputField, 1f, 10f);
							Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 0.1f, 10f) });

							if (!inputField.gameObject.GetComponent<InputFieldHelper>())
							{
								inputField.gameObject.AddComponent<InputFieldHelper>();
							}
						}

						int num = 0;
						foreach (Toggle toggle in gradientColor1Buttons)
						{
							if (num < 18)
							{
								toggle.GetComponent<Image>().color = GameManager.inst.LiveTheme.objectColors[num];
							}
							else
							{
								toggle.GetComponent<Image>().color = new Color(0f, 0.8f, 0.56f, 0.5f);
							}
							toggle.onValueChanged.RemoveAllListeners();
							if (num == currentKeyframe.eventValues[2])
							{
								toggle.isOn = true;
							}
							else
							{
								toggle.isOn = false;
							}
							int tmpIndex = num;
							toggle.onValueChanged.AddListener(delegate (bool val)
							{
								SetGradientColor1(tmpIndex);
							});
							num++;
						}
						
						int num2 = 0;
						foreach (Toggle toggle in gradientColor2Buttons)
						{
							if (num2 < 18)
							{
								toggle.GetComponent<Image>().color = GameManager.inst.LiveTheme.objectColors[num2];
							}
							else
							{
								toggle.GetComponent<Image>().color = new Color(0.81f, 0.37f, 1f, 0.5f);
							}
							toggle.onValueChanged.RemoveAllListeners();
							if (num2 == currentKeyframe.eventValues[3])
							{
								toggle.isOn = true;
							}
							else
							{
								toggle.isOn = false;
							}
							int tmpIndex = num2;
							toggle.onValueChanged.AddListener(delegate (bool val)
							{
								SetGradientColor2(tmpIndex);
							});
							num2++;
						}

						var drp = dialogTmp.Find("mode").GetComponent<Dropdown>();
						drp.onValueChanged.RemoveAllListeners();
						drp.value = (int)currentKeyframe.eventValues[4];
						drp.onValueChanged.AddListener(delegate (int _val)
						{
							currentKeyframe.eventValues[4] = _val;
							EventManager.inst.updateEvents();
						});
						break;
                    }
				case 16: //DoubleVision
					{
						//Intensity
						var inputField = dialogTmp.Find("offset").GetComponent<InputField>();
						inputField.onValueChanged.RemoveAllListeners();
						inputField.text = currentKeyframe.eventValues[0].ToString("f2");
						inputField.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							currentKeyframe.eventValues[0] = num;
							eventManager.updateEvents();
						});

						Triggers.IncreaseDecreaseButtons(inputField, 1f, 10f);
						Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 0.1f, 10f) });

						if (!inputField.gameObject.GetComponent<InputFieldHelper>())
						{
							inputField.gameObject.AddComponent<InputFieldHelper>();
						}
						break;
					}
				case 17: //ScanLines
                    {
						//Intensity
						{
							var inputField = dialogTmp.Find("intensity").GetComponent<InputField>();
							inputField.onValueChanged.RemoveAllListeners();
							inputField.text = currentKeyframe.eventValues[0].ToString("f2");
							inputField.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								currentKeyframe.eventValues[0] = num;
								eventManager.updateEvents();
							});

							Triggers.IncreaseDecreaseButtons(inputField, 1f, 10f);
							Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 0.1f, 10f) });

							if (!inputField.gameObject.GetComponent<InputFieldHelper>())
							{
								inputField.gameObject.AddComponent<InputFieldHelper>();
							}
						}

						//Amount
						{
							var inputField = dialogTmp.Find("iterations").GetComponent<InputField>();
							inputField.onValueChanged.RemoveAllListeners();
							inputField.text = currentKeyframe.eventValues[1].ToString("f2");
							inputField.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								currentKeyframe.eventValues[1] = num;
								eventManager.updateEvents();
							});

							Triggers.IncreaseDecreaseButtons(inputField, 1f, 10f);
							Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 0.1f, 10f) });

							if (!inputField.gameObject.GetComponent<InputFieldHelper>())
							{
								inputField.gameObject.AddComponent<InputFieldHelper>();
							}
						}

						//Speed
						{
							var inputField = dialogTmp.Find("speed").GetComponent<InputField>();
							inputField.onValueChanged.RemoveAllListeners();
							inputField.text = currentKeyframe.eventValues[2].ToString("f2");
							inputField.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								currentKeyframe.eventValues[2] = num;
								eventManager.updateEvents();
							});

							Triggers.IncreaseDecreaseButtons(inputField, 1f, 10f);
							Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 0.1f, 10f) });

							if (!inputField.gameObject.GetComponent<InputFieldHelper>())
							{
								inputField.gameObject.AddComponent<InputFieldHelper>();
							}
						}
						break;
					}
				case 18: //Blur
					{
						//Blur Amount
						{
							var inputField = dialogTmp.Find("intensity").GetComponent<InputField>();
							inputField.onValueChanged.RemoveAllListeners();
							inputField.text = currentKeyframe.eventValues[0].ToString("f2");
							inputField.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								currentKeyframe.eventValues[0] = num;
								eventManager.updateEvents();
							});

							Triggers.IncreaseDecreaseButtons(inputField, 1f, 10f);
							Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 0.1f, 10f) });

							if (!inputField.gameObject.GetComponent<InputFieldHelper>())
							{
								inputField.gameObject.AddComponent<InputFieldHelper>();
							}
						}

						//Blur Iterations
						{
							var inputField = dialogTmp.Find("iterations").GetComponent<InputField>();
							inputField.onValueChanged.RemoveAllListeners();
							inputField.text = Mathf.Clamp(currentKeyframe.eventValues[1], 1, 12).ToString("f1");
							inputField.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								num = Mathf.Clamp(num, 1, 12);
								currentKeyframe.eventValues[1] = Mathf.RoundToInt(num);
								eventManager.updateEvents();
							});

							Triggers.IncreaseDecreaseButtonsInt(inputField, 1, null, new List<int> { 1, 12 });
							Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDeltaInt(inputField, 1, false, new List<int> { 1, 12 }) });

							if (!inputField.gameObject.GetComponent<InputFieldHelper>())
							{
								inputField.gameObject.AddComponent<InputFieldHelper>();
							}
						}
						break;
                    }
				case 19: //Pixelize
                    {
						//Pixelize
						var inputField = dialogTmp.Find("offset").GetComponent<InputField>();
						inputField.onValueChanged.RemoveAllListeners();
						inputField.text = currentKeyframe.eventValues[0].ToString("f2");
						inputField.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							num = Mathf.Clamp(num, 0f, 0.99999f);
							currentKeyframe.eventValues[0] = num;
							eventManager.updateEvents();
						});

						Triggers.IncreaseDecreaseButtons(inputField, 1f, 10f, null, new List<float> { 0f, 0.99999f });
						Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 0.1f, 10f, false, new List<float> { 0f, 0.99999f }) });

						if (!inputField.gameObject.GetComponent<InputFieldHelper>())
						{
							inputField.gameObject.AddComponent<InputFieldHelper>();
						}
						break;
                    }
				case 20: //BG
					{
						int num = 0;
						foreach (Toggle toggle in bgColorButtons)
						{
							if (num < 18)
							{
								toggle.GetComponent<Image>().color = GameManager.inst.LiveTheme.objectColors[num];
							}
							else
							{
								toggle.GetComponent<Image>().color = GameManager.inst.LiveTheme.backgroundColor;
							}
							toggle.onValueChanged.RemoveAllListeners();
							if (num == currentKeyframe.eventValues[0])
							{
								toggle.isOn = true;
							}
							else
							{
								toggle.isOn = false;
							}
							int tmpIndex = num;
							toggle.onValueChanged.AddListener(delegate (bool val)
							{
								SetBGColor(tmpIndex);
							});
							num++;
						}
						break;
                    }
				case 21: //Invert
					{
						//int num = 0;
						//foreach (Toggle toggle in overlayColorButtons)
						//{
						//	if (num < 18)
						//	{
						//		toggle.GetComponent<Image>().color = GameManager.inst.LiveTheme.objectColors[num];
						//	}
						//	else
						//	{
						//		toggle.GetComponent<Image>().color = Color.black;
						//	}
						//	toggle.onValueChanged.RemoveAllListeners();
						//	if (num == currentKeyframe.eventValues[0])
						//	{
						//		toggle.isOn = true;
						//	}
						//	else
						//	{
						//		toggle.isOn = false;
						//	}
						//	int tmpIndex = num;
						//	toggle.onValueChanged.AddListener(delegate (bool val)
						//	{
						//		SetOverlayColor(tmpIndex);
						//	});
						//	num++;
						//}

						var inputField = dialogTmp.Find("alpha").GetComponent<InputField>();
						inputField.onValueChanged.RemoveAllListeners();
						inputField.text = currentKeyframe.eventValues[0].ToString("f2");
						inputField.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							num = Mathf.Clamp(num, 0f, 1f);
							currentKeyframe.eventValues[0] = num;
							eventManager.updateEvents();
						});

						Triggers.IncreaseDecreaseButtons(inputField, 1f, 10f, null, new List<float> { 0f, 1f });
						Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 0.1f, 10f, false, new List<float> { 0f, 1f }) });
						
						//Triggers.IncreaseDecreaseButtons(inputField, 1f, 10f);
						//Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 0.1f, 10f) });

						break;
                    }
				case 22: //Timeline
                    {
                        //Active
                        {
							var active = dialogTmp.Find("active").GetComponent<Toggle>();
							active.onValueChanged.RemoveAllListeners();
							if ((int)currentKeyframe.eventValues[0] == 0)
                            {
								active.isOn = true;
                            }
							else
                            {
								active.isOn = false;
                            }
							active.onValueChanged.AddListener(delegate (bool _val)
							{
								if (_val)
                                {
									currentKeyframe.eventValues[0] = 0f;
                                }
								else
                                {
									currentKeyframe.eventValues[0] = 1f;
                                }
								eventManager.updateEvents();
							});
                        }

						//Position
						{
							var posX = dialogTmp.Find("position/x").GetComponent<InputField>();
							var posY = dialogTmp.Find("position/y").GetComponent<InputField>();

							posX.onValueChanged.RemoveAllListeners();
							posX.text = currentKeyframe.eventValues[1].ToString("f2");
							posX.onValueChanged.AddListener(delegate (string val)
							{
								if (float.TryParse(val, out float num))
								{
									currentKeyframe.eventValues[1] = num;
									eventManager.updateEvents();
								}
								else
								{
									Debug.LogErrorFormat("{0}Event Value was not in correct format!", EditorPlugin.className);
								}
							});

							posY.onValueChanged.RemoveAllListeners();
							posY.text = currentKeyframe.eventValues[2].ToString("f2");
							posY.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								currentKeyframe.eventValues[2] = num;
								eventManager.updateEvents();
							});

							Triggers.IncreaseDecreaseButtons(posX, 1f, 10f);
							Triggers.IncreaseDecreaseButtons(posY, 1f, 10f);
							Triggers.AddEventTrigger(posX.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(posX, 0.1f, 10f, true), Triggers.ScrollDeltaVector2(posX, posY, 0.1f, 10f) });
							Triggers.AddEventTrigger(posY.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(posY, 0.1f, 10f, true), Triggers.ScrollDeltaVector2(posX, posY, 0.1f, 10f) });

							if (!posX.gameObject.GetComponent<InputFieldHelper>())
							{
								posX.gameObject.AddComponent<InputFieldHelper>();
							}
							if (!posY.gameObject.GetComponent<InputFieldHelper>())
							{
								posY.gameObject.AddComponent<InputFieldHelper>();
							}
						}

						//Scale
						{
							var posX = dialogTmp.Find("scale/x").GetComponent<InputField>();
							var posY = dialogTmp.Find("scale/y").GetComponent<InputField>();

							posX.onValueChanged.RemoveAllListeners();
							posX.text = currentKeyframe.eventValues[3].ToString("f2");
							posX.onValueChanged.AddListener(delegate (string val)
							{
								if (float.TryParse(val, out float num))
								{
									currentKeyframe.eventValues[3] = num;
									eventManager.updateEvents();
								}
								else
								{
									Debug.LogErrorFormat("{0}Event Value was not in correct format!", EditorPlugin.className);
								}
							});

							posY.onValueChanged.RemoveAllListeners();
							posY.text = currentKeyframe.eventValues[4].ToString("f2");
							posY.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								currentKeyframe.eventValues[4] = num;
								eventManager.updateEvents();
							});

							Triggers.IncreaseDecreaseButtons(posX, 1f, 10f);
							Triggers.IncreaseDecreaseButtons(posY, 1f, 10f);
							Triggers.AddEventTrigger(posX.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(posX, 0.1f, 10f, true), Triggers.ScrollDeltaVector2(posX, posY, 0.1f, 10f) });
							Triggers.AddEventTrigger(posY.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(posY, 0.1f, 10f, true), Triggers.ScrollDeltaVector2(posX, posY, 0.1f, 10f) });

							if (!posX.gameObject.GetComponent<InputFieldHelper>())
							{
								posX.gameObject.AddComponent<InputFieldHelper>();
							}
							if (!posY.gameObject.GetComponent<InputFieldHelper>())
							{
								posY.gameObject.AddComponent<InputFieldHelper>();
							}
						}

                        //Rotation
                        {
							var inputField = dialogTmp.Find("rotation").GetComponent<InputField>();
							inputField.onValueChanged.RemoveAllListeners();
							inputField.text = currentKeyframe.eventValues[5].ToString("f2");
							inputField.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								currentKeyframe.eventValues[5] = num;
								eventManager.updateEvents();
							});

							Triggers.IncreaseDecreaseButtons(inputField, 1f, 10f, null);
							Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 0.1f, 10f) });

							if (!inputField.gameObject.GetComponent<InputFieldHelper>())
							{
								inputField.gameObject.AddComponent<InputFieldHelper>();
							}
						}

						//Color
						{
							int num = 0;
							foreach (Toggle toggle in timelineColorButtons)
							{
								if (num < 18)
								{
									toggle.GetComponent<Image>().color = GameManager.inst.LiveTheme.objectColors[num];
								}
								else
								{
									toggle.GetComponent<Image>().color = GameManager.inst.LiveTheme.guiColor;
								}
								toggle.onValueChanged.RemoveAllListeners();
								if (num == currentKeyframe.eventValues[6])
								{
									toggle.isOn = true;
								}
								else
								{
									toggle.isOn = false;
								}
								int tmpIndex = num;
								toggle.onValueChanged.AddListener(delegate (bool val)
								{
									SetTimelineColor(tmpIndex);
								});
								num++;
							}
						}
						break;
                    }
				case 23: //Player
					{
						//Active
						{
							var active = dialogTmp.Find("active").GetComponent<Toggle>();
							active.onValueChanged.RemoveAllListeners();
							if ((int)currentKeyframe.eventValues[0] == 0)
							{
								active.isOn = true;
							}
							else
							{
								active.isOn = false;
							}
							active.onValueChanged.AddListener(delegate (bool _val)
							{
								if (_val)
								{
									currentKeyframe.eventValues[0] = 0f;
								}
								else
								{
									currentKeyframe.eventValues[0] = 1f;
								}
								eventManager.updateEvents();
							});
						}

						//Moveable
						{
							var active = dialogTmp.Find("moveable").GetComponent<Toggle>();
							active.onValueChanged.RemoveAllListeners();
							if ((int)currentKeyframe.eventValues[1] == 0)
							{
								active.isOn = true;
							}
							else
							{
								active.isOn = false;
							}
							active.onValueChanged.AddListener(delegate (bool _val)
							{
								if (_val)
								{
									currentKeyframe.eventValues[1] = 0f;
								}
								else
								{
									currentKeyframe.eventValues[1] = 1f;
								}
								eventManager.updateEvents();
							});
						}

						//Velocity
						{
							var inputField = dialogTmp.Find("position/x").GetComponent<InputField>();
							inputField.onValueChanged.RemoveAllListeners();
							inputField.text = currentKeyframe.eventValues[2].ToString("f2");
							inputField.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								currentKeyframe.eventValues[2] = num;
								eventManager.updateEvents();
							});

							Triggers.IncreaseDecreaseButtons(inputField, 1f, 10f, null, new List<float> { 0f, float.PositiveInfinity });
							Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 0.1f, 10f, false, new List<float> { 0f, float.PositiveInfinity }) });

							if (!inputField.gameObject.GetComponent<InputFieldHelper>())
							{
								inputField.gameObject.AddComponent<InputFieldHelper>();
							}
						}

						//Rotation
						{
							var inputField = dialogTmp.Find("position/y").GetComponent<InputField>();
							inputField.onValueChanged.RemoveAllListeners();
							inputField.text = currentKeyframe.eventValues[3].ToString("f2");
							inputField.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								currentKeyframe.eventValues[3] = num;
								eventManager.updateEvents();
							});

							Triggers.IncreaseDecreaseButtons(inputField, 15f, 3f, null, new List<float> { 0f, float.PositiveInfinity });
							Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 15f, 3f, false, new List<float> { 0f, float.PositiveInfinity }) });

							if (!inputField.gameObject.GetComponent<InputFieldHelper>())
							{
								inputField.gameObject.AddComponent<InputFieldHelper>();
							}
						}
						break;
                    }
				case 24: //Follow Player
					{
						//Active
						{
							var active = dialogTmp.Find("active").GetComponent<Toggle>();
							active.onValueChanged.RemoveAllListeners();
							if ((int)currentKeyframe.eventValues[0] == 0)
							{
								active.isOn = false;
							}
							else
							{
								active.isOn = true;
							}
							active.onValueChanged.AddListener(delegate (bool _val)
							{
								if (_val)
								{
									currentKeyframe.eventValues[0] = 1f;
								}
								else
								{
									currentKeyframe.eventValues[0] = 0f;
								}
								eventManager.updateEvents();
							});
						}

						//Move
						{
							var active = dialogTmp.Find("move").GetComponent<Toggle>();
							active.onValueChanged.RemoveAllListeners();
							if ((int)currentKeyframe.eventValues[1] == 0)
							{
								active.isOn = false;
							}
							else
							{
								active.isOn = true;
							}
							active.onValueChanged.AddListener(delegate (bool _val)
							{
								if (_val)
								{
									currentKeyframe.eventValues[1] = 1f;
								}
								else
								{
									currentKeyframe.eventValues[1] = 0f;
								}
								eventManager.updateEvents();
							});
						}

						//Rotate
						{
							var active = dialogTmp.Find("rotate").GetComponent<Toggle>();
							active.onValueChanged.RemoveAllListeners();
							if ((int)currentKeyframe.eventValues[2] == 0)
							{
								active.isOn = false;
							}
							else
							{
								active.isOn = true;
							}
							active.onValueChanged.AddListener(delegate (bool _val)
							{
								if (_val)
								{
									currentKeyframe.eventValues[2] = 1f;
								}
								else
								{
									currentKeyframe.eventValues[2] = 0f;
								}
								eventManager.updateEvents();
							});
						}

						//Sharpness
						{
							var inputField = dialogTmp.Find("position/x").GetComponent<InputField>();
							inputField.onValueChanged.RemoveAllListeners();
							inputField.text = currentKeyframe.eventValues[3].ToString("f2");
							inputField.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								currentKeyframe.eventValues[3] = Mathf.Clamp(num, 0.001f, 1f);
								eventManager.updateEvents();
							});

							Triggers.IncreaseDecreaseButtons(inputField, 1f, 10f, null, new List<float> { 0.001f, 1f });
							Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 0.1f, 10f, false, new List<float> { 0.001f, 1f }) });
						}

						//Offset
						{
							var inputField = dialogTmp.Find("position/y").GetComponent<InputField>();
							inputField.onValueChanged.RemoveAllListeners();
							inputField.text = currentKeyframe.eventValues[4].ToString("f2");
							inputField.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								currentKeyframe.eventValues[4] = num;
								eventManager.updateEvents();
							});

							Triggers.IncreaseDecreaseButtons(inputField, 1f, 10f);
							Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 0.1f, 10f) });

							if (!inputField.gameObject.GetComponent<InputFieldHelper>())
							{
								inputField.gameObject.AddComponent<InputFieldHelper>();
							}
						}

						//Limit Left
						{
							var inputField = dialogTmp.Find("limit horizontal/x").GetComponent<InputField>();
							inputField.onValueChanged.RemoveAllListeners();
							inputField.text = currentKeyframe.eventValues[5].ToString("f2");
							inputField.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								currentKeyframe.eventValues[5] = num;
								eventManager.updateEvents();
							});

							Triggers.IncreaseDecreaseButtons(inputField, 1f, 10f);
							Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 0.1f, 10f) });

							if (!inputField.gameObject.GetComponent<InputFieldHelper>())
							{
								inputField.gameObject.AddComponent<InputFieldHelper>();
							}
						}

						//Limit Right
						{
							var inputField = dialogTmp.Find("limit horizontal/y").GetComponent<InputField>();
							inputField.onValueChanged.RemoveAllListeners();
							inputField.text = currentKeyframe.eventValues[6].ToString("f2");
							inputField.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								currentKeyframe.eventValues[6] = num;
								eventManager.updateEvents();
							});

							Triggers.IncreaseDecreaseButtons(inputField, 1f, 10f);
							Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 0.1f, 10f) });

							if (!inputField.gameObject.GetComponent<InputFieldHelper>())
							{
								inputField.gameObject.AddComponent<InputFieldHelper>();
							}
						}

						//Limit Up
						{
							var inputField = dialogTmp.Find("limit vertical/x").GetComponent<InputField>();
							inputField.onValueChanged.RemoveAllListeners();
							inputField.text = currentKeyframe.eventValues[7].ToString("f2");
							inputField.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								currentKeyframe.eventValues[7] = num;
								eventManager.updateEvents();
							});

							Triggers.IncreaseDecreaseButtons(inputField, 1f, 10f);
							Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 0.1f, 10f) });

							if (!inputField.gameObject.GetComponent<InputFieldHelper>())
							{
								inputField.gameObject.AddComponent<InputFieldHelper>();
							}
						}

						//Limit Down
						{
							var inputField = dialogTmp.Find("limit vertical/y").GetComponent<InputField>();
							inputField.onValueChanged.RemoveAllListeners();
							inputField.text = currentKeyframe.eventValues[8].ToString("f2");
							inputField.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								currentKeyframe.eventValues[8] = num;
								eventManager.updateEvents();
							});

							Triggers.IncreaseDecreaseButtons(inputField, 1f, 10f);
							Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 0.1f, 10f) });

							if (!inputField.gameObject.GetComponent<InputFieldHelper>())
							{
								inputField.gameObject.AddComponent<InputFieldHelper>();
							}
						}

						//Anchor
						{
							var inputField = dialogTmp.Find("anchor/x").GetComponent<InputField>();
							inputField.onValueChanged.RemoveAllListeners();
							inputField.text = currentKeyframe.eventValues[9].ToString("f2");
							inputField.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								currentKeyframe.eventValues[9] = num;
								eventManager.updateEvents();
							});

							Triggers.IncreaseDecreaseButtons(inputField, 1f, 10f);
							Triggers.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 0.1f, 10f) });

							if (!inputField.gameObject.GetComponent<InputFieldHelper>())
							{
								inputField.gameObject.AddComponent<InputFieldHelper>();
							}
						}
						break;
                    }
				case 25: //Audio
					{
						var posX = dialogTmp.Find("position/x").GetComponent<InputField>();
						var posY = dialogTmp.Find("position/y").GetComponent<InputField>();

						posX.onValueChanged.RemoveAllListeners();
						posX.text = currentKeyframe.eventValues[0].ToString("f2");
						posX.onValueChanged.AddListener(delegate (string val)
						{
							if (float.TryParse(val, out float num))
							{
								currentKeyframe.eventValues[0] = num;
								eventManager.updateEvents();
							}
							else
							{
								Debug.LogErrorFormat("{0}Event Value was not in correct format!", EditorPlugin.className);
							}
						});

						posY.onValueChanged.RemoveAllListeners();
						posY.text = currentKeyframe.eventValues[1].ToString("f2");
						posY.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							currentKeyframe.eventValues[1] = num;
							eventManager.updateEvents();
						});

						Triggers.IncreaseDecreaseButtons(posX, 1f, 10f, null, new List<float> { 0.001f, 10f });
						Triggers.IncreaseDecreaseButtons(posY, 1f, 10f, null, new List<float> { 0f, 1f });
						Triggers.AddEventTrigger(posX.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(posX, 0.1f, 10f, true, new List<float> { 0.001f, 10f }), Triggers.ScrollDeltaVector2(posX, posY, 0.1f, 10f, new List<float> { 0.001f, 10f, 0f, 1f }) });
						Triggers.AddEventTrigger(posY.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(posY, 0.1f, 10f, true, new List<float> { 0f, 1f }), Triggers.ScrollDeltaVector2(posX, posY, 0.1f, 10f, new List<float> { 0.001f, 10f, 0f, 1f }) });

						if (!posX.gameObject.GetComponent<InputFieldHelper>())
						{
							posX.gameObject.AddComponent<InputFieldHelper>();
						}
						if (!posY.gameObject.GetComponent<InputFieldHelper>())
						{
							posY.gameObject.AddComponent<InputFieldHelper>();
						}
						break;
                    }
			}

			var curvesDropdown = dialogTmp.transform.Find("curves").GetComponent<Dropdown>();

			dialogTmp.transform.Find("curves_label").gameObject.SetActive(__instance.currentEvent != 0);
			curvesDropdown.gameObject.SetActive(__instance.currentEvent != 0);
			curvesDropdown.onValueChanged.RemoveAllListeners();
			if (DataManager.inst.AnimationListDictionaryBack.ContainsKey(currentKeyframe.curveType))
			{
				curvesDropdown.value = DataManager.inst.AnimationListDictionaryBack[currentKeyframe.curveType];
			}
			curvesDropdown.onValueChanged.AddListener(delegate (int _value)
			{
				currentKeyframe.curveType = DataManager.inst.AnimationListDictionary[_value];
				eventManager.updateEvents();
			});

			var editJumpLeftLarge = dialogTmp.Find("edit/<<").GetComponent<Button>();
			var editJumpLeft = dialogTmp.Find("edit/<").GetComponent<Button>();
			var editJumpRight = dialogTmp.Find("edit/>").GetComponent<Button>();
			var editJumpRightLarge = dialogTmp.Find("edit/>>").GetComponent<Button>();

			editJumpLeftLarge.interactable = (__instance.currentEvent != 0);
			editJumpLeftLarge.onClick.RemoveAllListeners();
			editJumpLeftLarge.onClick.AddListener(delegate ()
			{
				__instance.UpdateEventOrder(false);
				__instance.SetCurrentEvent(__instance.currentEventType, 0);
			});
			editJumpLeft.interactable = (__instance.currentEvent != 0);
			editJumpLeft.onClick.RemoveAllListeners();
			editJumpLeft.onClick.AddListener(delegate ()
			{
				__instance.UpdateEventOrder(false);
				int num = __instance.currentEvent - 1;
				if (num < 0)
				{
					num = 0;
				}
				__instance.SetCurrentEvent(__instance.currentEventType, num);
			});

			var tex = dialogTmp.Find("edit/|/text").GetComponent<Text>();
			var allEvents = DataManager.inst.gameData.eventObjects.allEvents[__instance.currentEventType];

			if (__instance.currentEvent == 0)
			{
				tex.text = "S";
			}
			else if (__instance.currentEvent == allEvents.Count() - 1)
			{
				tex.text = "E";
			}
			else
			{
				tex.text = __instance.currentEvent.ToString();
			}
			editJumpRight.interactable = (__instance.currentEvent != allEvents.Count() - 1);
			editJumpRight.onClick.RemoveAllListeners();
			editJumpRight.onClick.AddListener(delegate ()
			{
				__instance.UpdateEventOrder(false);
				int num = __instance.currentEvent + 1;
				if (num >= allEvents.Count())
				{
					num = allEvents.Count() - 1;
				}
				__instance.SetCurrentEvent(__instance.currentEventType, num);
			});
			editJumpRightLarge.interactable = (__instance.currentEvent != allEvents.Count() - 1);
			editJumpRightLarge.onClick.RemoveAllListeners();
			editJumpRightLarge.onClick.AddListener(delegate ()
			{
				__instance.UpdateEventOrder(false);
				__instance.SetCurrentEvent(__instance.currentEventType, allEvents.IndexOf(allEvents.Last()));
			});

			var editDelete = dialogTmp.Find("edit/del").GetComponent<Button>();

			editDelete.onClick.RemoveAllListeners();
			editDelete.interactable = (__instance.currentEvent != 0);
			editDelete.onClick.AddListener(delegate ()
			{
				__instance.DeleteEvent(__instance.currentEventType, __instance.currentEvent);
			});

			RenderTitles();
			return false;
        }

		public static void SetVignetteColor(int _value)
		{
			DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[6] = (float)_value;
			EventManager.inst.updateEvents();
			int num = 0;
			foreach (Toggle toggle in vignetteColorButtons)
			{
				toggle.onValueChanged.RemoveAllListeners();
				if (num == _value)
				{
					toggle.isOn = true;
				}
				else
				{
					toggle.isOn = false;
				}
				int tmpIndex = num;
				toggle.onValueChanged.AddListener(delegate (bool val)
				{
					SetVignetteColor(tmpIndex);
				});
				num++;
			}
		}

		public static void SetBloomColor(int _value)
		{
			DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[4] = (float)_value;
			EventManager.inst.updateEvents();
			int num = 0;
			foreach (Toggle toggle in bloomColorButtons)
			{
				toggle.onValueChanged.RemoveAllListeners();
				if (num == _value)
				{
					toggle.isOn = true;
				}
				else
				{
					toggle.isOn = false;
				}
				int tmpIndex = num;
				toggle.onValueChanged.AddListener(delegate (bool val)
				{
					SetBloomColor(tmpIndex);
				});
				num++;
			}
		}

		public static void SetGradientColor1(int _value)
		{
			DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[2] = (float)_value;
			EventManager.inst.updateEvents();
			int num = 0;
			foreach (Toggle toggle in gradientColor1Buttons)
			{
				toggle.onValueChanged.RemoveAllListeners();
				if (num == _value)
				{
					toggle.isOn = true;
				}
				else
				{
					toggle.isOn = false;
				}
				int tmpIndex = num;
				toggle.onValueChanged.AddListener(delegate (bool val)
				{
					SetGradientColor1(tmpIndex);
				});
				num++;
			}
		}

		public static void SetGradientColor2(int _value)
		{
			DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[3] = (float)_value;
			EventManager.inst.updateEvents();
			int num = 0;
			foreach (Toggle toggle in gradientColor2Buttons)
			{
				toggle.onValueChanged.RemoveAllListeners();
				if (num == _value)
				{
					toggle.isOn = true;
				}
				else
				{
					toggle.isOn = false;
				}
				int tmpIndex = num;
				toggle.onValueChanged.AddListener(delegate (bool val)
				{
					SetGradientColor2(tmpIndex);
				});
				num++;
			}
		}

		public static void SetBGColor(int _value)
		{
			DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] = (float)_value;
			EventManager.inst.updateEvents();
			int num = 0;
			foreach (Toggle toggle in bgColorButtons)
			{
				toggle.onValueChanged.RemoveAllListeners();
				if (num == _value)
				{
					toggle.isOn = true;
				}
				else
				{
					toggle.isOn = false;
				}
				int tmpIndex = num;
				toggle.onValueChanged.AddListener(delegate (bool val)
				{
					SetBGColor(tmpIndex);
				});
				num++;
			}
		}
		
		public static void SetOverlayColor(int _value)
		{
			DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] = (float)_value;
			EventManager.inst.updateEvents();
			int num = 0;
			foreach (Toggle toggle in overlayColorButtons)
			{
				toggle.onValueChanged.RemoveAllListeners();
				if (num == _value)
				{
					toggle.isOn = true;
				}
				else
				{
					toggle.isOn = false;
				}
				int tmpIndex = num;
				toggle.onValueChanged.AddListener(delegate (bool val)
				{
					SetOverlayColor(tmpIndex);
				});
				num++;
			}
		}

		public static void SetTimelineColor(int _value)
		{
			DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[6] = (float)_value;
			EventManager.inst.updateEvents();
			int num = 0;
			foreach (Toggle toggle in timelineColorButtons)
			{
				toggle.onValueChanged.RemoveAllListeners();
				if (num == _value)
				{
					toggle.isOn = true;
				}
				else
				{
					toggle.isOn = false;
				}
				int tmpIndex = num;
				toggle.onValueChanged.AddListener(delegate (bool val)
				{
					SetTimelineColor(tmpIndex);
				});
				num++;
			}
		}

		[HarmonyPatch("RenderThemeContent")]
		[HarmonyPrefix]
		public static bool RenderThemeContentPatch(Transform __0, string __1)
		{
			Debug.LogFormat("{0}RenderThemeContent Prefix Patch", EditorPlugin.className);
			Transform parent = __0.Find("themes/viewport/content");

			__0.Find("themes").GetComponent<ScrollRect>().horizontal = false;

			if (!parent.GetComponent<GridLayoutGroup>())
			{
				parent.gameObject.AddComponent<GridLayoutGroup>();
			}

			var prefabLay = parent.GetComponent<GridLayoutGroup>();
			prefabLay.cellSize = new Vector2(344f, 30f);
			prefabLay.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
			prefabLay.constraintCount = 1;
			prefabLay.spacing = new Vector2(4f, 4f);
			prefabLay.startAxis = GridLayoutGroup.Axis.Horizontal;

			parent.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.MinSize;

			RTEditor.inst.StartCoroutine(RTEditor.RenderThemeList(__0, __1));

			return false;
		}

		[HarmonyPatch("RenderThemeEditor")]
		[HarmonyPrefix]
		public static bool RenderThemeEditor(EventEditor __instance, int __0)
		{
			Debug.LogFormat("{0}ID: {1}", EditorPlugin.className, __0);
			if (__0 != -1)
			{
				__instance.previewTheme = DataManager.BeatmapTheme.DeepCopy(DataManager.inst.GetTheme(__0), true);
			}
			else
			{
				__instance.previewTheme = new DataManager.BeatmapTheme();
				__instance.previewTheme.ClearBeatmap();
			}

			var theme = __instance.dialogLeft.Find("theme");
			theme.gameObject.SetActive(true);
			__instance.showTheme = true;
			var themeContent = theme.Find("theme/viewport/content");
			var actions = theme.Find("actions");
			theme.Find("theme").localRotation = Quaternion.Euler(Vector3.zero);

			foreach (var child in themeContent)
            {
				var obj = (Transform)child;
				obj.localRotation = Quaternion.Euler(Vector3.zero);
            }

			var name = theme.Find("name").GetComponent<InputField>();
			var cancel = actions.Find("cancel").GetComponent<Button>();
			var createNew = actions.Find("create-new").GetComponent<Button>();
			var update = actions.Find("update").GetComponent<Button>();

			name.onValueChanged.RemoveAllListeners();
			name.text = __instance.previewTheme.name;
			name.onValueChanged.AddListener(delegate (string val)
			{
				__instance.previewTheme.name = val;
			});
			cancel.onClick.RemoveAllListeners();
			cancel.onClick.AddListener(delegate ()
			{
				__instance.showTheme = false;
				theme.gameObject.SetActive(false);
			});
			createNew.onClick.RemoveAllListeners();
			update.onClick.RemoveAllListeners();
			if (__0 < DataManager.inst.BeatmapThemes.Count())
			{
				createNew.gameObject.SetActive(true);
				update.gameObject.SetActive(false);
			}
			else
			{
				createNew.gameObject.SetActive(true);
				update.gameObject.SetActive(true);
			}
			createNew.onClick.AddListener(delegate ()
			{
				__instance.previewTheme.id = null;
				ThemeEditor.inst.SaveTheme(DataManager.BeatmapTheme.DeepCopy(__instance.previewTheme));
				__instance.StartCoroutine(ThemeEditor.inst.LoadThemes());
				var child = __instance.dialogRight.GetChild(__instance.currentEventType);
				__instance.RenderThemeContent(child, child.Find("theme-search").GetComponent<InputField>().text);
				__instance.RenderThemePreview(child);
				__instance.showTheme = false;
				theme.gameObject.SetActive(false);
			});
			update.onClick.AddListener(delegate ()
			{
				var fileList = FileManager.inst.GetFileList("beatmaps/themes", "lst");
				fileList = (from x in fileList
							orderby x.Name.ToLower()
							select x).ToList();
				foreach (FileManager.LSFile lsfile in fileList)
				{
					if (int.Parse(DataManager.BeatmapTheme.Parse(JSON.Parse(FileManager.inst.LoadJSONFileRaw(lsfile.FullPath)), false).id) == __0)
					{
						FileManager.inst.DeleteFileRaw(lsfile.FullPath);
					}
				}
				ThemeEditor.inst.SaveTheme(DataManager.BeatmapTheme.DeepCopy(__instance.previewTheme, true));
				__instance.StartCoroutine(ThemeEditor.inst.LoadThemes());
				var child = EventEditor.inst.dialogRight.GetChild(__instance.currentEventType);
				__instance.RenderThemeContent(child, child.Find("theme-search").GetComponent<InputField>().text);
				__instance.RenderThemePreview(child);
				__instance.showTheme = false;
				theme.gameObject.SetActive(false);
			});

			var bgHex = themeContent.Find("bg/hex").GetComponent<InputField>();
			var bgPreview = themeContent.Find("bg/preview").GetComponent<Image>();
			var bgPreviewET = themeContent.Find("bg/preview").GetComponent<EventTrigger>();
			var bgDropper = themeContent.Find("bg/preview/dropper").GetComponent<Image>();

			bgHex.onValueChanged.RemoveAllListeners();
			bgHex.text = LSColors.ColorToHex(__instance.previewTheme.backgroundColor);
			bgPreview.color = __instance.previewTheme.backgroundColor;
			bgHex.onValueChanged.AddListener(delegate (string val)
			{
				if (val.Length == 6)
				{
					bgPreview.color = LSColors.HexToColor(val);
					__instance.previewTheme.backgroundColor = LSColors.HexToColor(val);
				}
                else
                {
					bgPreview.color = LSColors.pink500;
					__instance.previewTheme.backgroundColor = LSColors.pink500;
				}

				bgDropper.color = Triggers.InvertColorHue(Triggers.InvertColorValue(__instance.previewTheme.backgroundColor));
				bgPreviewET.triggers.Clear();
				bgPreviewET.triggers.Add(Triggers.CreatePreviewClickTrigger(bgPreview, bgDropper, bgHex, __instance.previewTheme.backgroundColor));
			});

			bgDropper.color = Triggers.InvertColorHue(Triggers.InvertColorValue(__instance.previewTheme.backgroundColor));
			bgPreviewET.triggers.Clear();
			bgPreviewET.triggers.Add(Triggers.CreatePreviewClickTrigger(bgPreview, bgDropper, bgHex, __instance.previewTheme.backgroundColor));

			var guiHex = themeContent.Find("gui/hex").GetComponent<InputField>();
			var guiPreview = themeContent.Find("gui/preview").GetComponent<Image>();
			var guiPreviewET = themeContent.Find("gui/preview").GetComponent<EventTrigger>();
			var guiDropper = themeContent.Find("gui/preview/dropper").GetComponent<Image>();

			guiHex.onValueChanged.RemoveAllListeners();
			guiHex.characterLimit = 8;
			guiHex.characterValidation = InputField.CharacterValidation.None;
			guiHex.contentType = InputField.ContentType.Standard;
			guiHex.text = RTEditor.ColorToHex(__instance.previewTheme.guiColor);
			guiPreview.color = __instance.previewTheme.guiColor;
			guiHex.onValueChanged.AddListener(delegate (string val)
			{
				if (val.Length == 8)
				{
					guiPreview.color = LSColors.HexToColorAlpha(val);
					__instance.previewTheme.guiColor = LSColors.HexToColorAlpha(val);
				}
				else
				{
					guiPreview.color = LSColors.pink500;
					__instance.previewTheme.guiColor = LSColors.pink500;
				}

				guiDropper.color = Triggers.InvertColorHue(Triggers.InvertColorValue(__instance.previewTheme.guiColor));
				guiPreviewET.triggers.Clear();
				guiPreviewET.triggers.Add(Triggers.CreatePreviewClickTrigger(guiPreview, guiDropper, guiHex, __instance.previewTheme.guiColor));
			});

			guiDropper.color = Triggers.InvertColorHue(Triggers.InvertColorValue(__instance.previewTheme.guiColor));
			guiPreviewET.triggers.Clear();
			guiPreviewET.triggers.Add(Triggers.CreatePreviewClickTrigger(guiPreview, guiDropper, guiHex, __instance.previewTheme.guiColor));

			for (int i = 0; i < 4; i++)
			{
				var hex = themeContent.Find("player" + i + "/hex").GetComponent<InputField>();
				var preview = themeContent.Find("player" + i + "/preview").GetComponent<Image>();
				var previewET = themeContent.Find("player" + i + "/preview").GetComponent<EventTrigger>();
				var dropper = themeContent.Find("player" + i + "/preview").GetChild(0).GetComponent<Image>();
				int indexTmp = i;
				hex.onValueChanged.RemoveAllListeners();
				hex.characterLimit = 8;
				hex.characterValidation = InputField.CharacterValidation.None;
				hex.contentType = InputField.ContentType.Standard;
				hex.text = RTEditor.ColorToHex(__instance.previewTheme.playerColors[indexTmp]);
				preview.color = __instance.previewTheme.playerColors[indexTmp];
				hex.GetComponent<InputField>().onValueChanged.AddListener(delegate (string val)
				{
					if (val.Length == 8)
					{
						preview.color = LSColors.HexToColorAlpha(val);
						__instance.previewTheme.playerColors[indexTmp] = LSColors.HexToColorAlpha(val);
					}
					else
                    {
						preview.color = LSColors.pink500;
						__instance.previewTheme.playerColors[indexTmp] = LSColors.pink500;
					}

					dropper.color = Triggers.InvertColorHue(Triggers.InvertColorValue(__instance.previewTheme.playerColors[indexTmp]));
					previewET.triggers.Clear();
					previewET.triggers.Add(Triggers.CreatePreviewClickTrigger(preview, dropper, hex, __instance.previewTheme.playerColors[indexTmp]));
				});

				dropper.color = Triggers.InvertColorHue(Triggers.InvertColorValue(__instance.previewTheme.playerColors[indexTmp]));
				previewET.triggers.Clear();
				previewET.triggers.Add(Triggers.CreatePreviewClickTrigger(preview, dropper, hex, __instance.previewTheme.playerColors[indexTmp]));
			}

			for (int i = 0; i < 18; i++)
			{
				themeContent.Find("object" + i).transform.localRotation = Quaternion.Euler(Vector3.zero);
				var hex = themeContent.Find("object" + i + "/hex").GetComponent<InputField>();
				var preview = themeContent.Find("object" + i + "/preview").GetComponent<Image>();
				var previewET = themeContent.Find("object" + i + "/preview").GetComponent<EventTrigger>();
				var dropper = themeContent.Find("object" + i + "/preview").GetChild(0).GetComponent<Image>();
				int indexTmp = i;
				hex.onValueChanged.RemoveAllListeners();
				hex.characterLimit = 8;
				hex.characterValidation = InputField.CharacterValidation.None;
				hex.contentType = InputField.ContentType.Standard;
				hex.text = RTEditor.ColorToHex(__instance.previewTheme.objectColors[indexTmp]);
				preview.color = __instance.previewTheme.objectColors[indexTmp];
				hex.onValueChanged.AddListener(delegate (string val)
				{
					if (val.Length == 8)
					{
						preview.color = LSColors.HexToColorAlpha(val);
						__instance.previewTheme.objectColors[indexTmp] = LSColors.HexToColorAlpha(val);
					}
					else
                    {
						preview.color = LSColors.pink500;
						__instance.previewTheme.objectColors[indexTmp] = LSColors.pink500;
					}

					dropper.color = Triggers.InvertColorHue(Triggers.InvertColorValue(__instance.previewTheme.objectColors[indexTmp]));
					previewET.triggers.Clear();
					previewET.triggers.Add(Triggers.CreatePreviewClickTrigger(preview, dropper, hex, __instance.previewTheme.objectColors[indexTmp]));
				});

				dropper.color = Triggers.InvertColorHue(Triggers.InvertColorValue(__instance.previewTheme.objectColors[indexTmp]));
				previewET.triggers.Clear();
				previewET.triggers.Add(Triggers.CreatePreviewClickTrigger(preview, dropper, hex, __instance.previewTheme.objectColors[indexTmp]));
			}

			for (int i = 0; i < 9; i++)
			{
				var hex = themeContent.Find("background" + i + "/hex").GetComponent<InputField>();
				var preview = themeContent.Find("background" + i + "/preview").GetComponent<Image>();
				var previewET = themeContent.Find("background" + i + "/preview").GetComponent<EventTrigger>();
				var dropper = themeContent.Find("background" + i + "/preview").GetChild(0).GetComponent<Image>();
				int indexTmp = i;
				hex.onValueChanged.RemoveAllListeners();
				hex.text = LSColors.ColorToHex(__instance.previewTheme.backgroundColors[indexTmp]);
				preview.color = __instance.previewTheme.backgroundColors[indexTmp];
				hex.onValueChanged.AddListener(delegate (string val)
				{
					if (val.Length == 6)
					{
						preview.color = LSColors.HexToColor(val);
						__instance.previewTheme.backgroundColors[indexTmp] = LSColors.HexToColor(val);
					}
					else
                    {
						preview.GetComponent<Image>().color = LSColors.pink500;
						__instance.previewTheme.backgroundColors[indexTmp] = LSColors.pink500;
					}

					dropper.color = Triggers.InvertColorHue(Triggers.InvertColorValue(__instance.previewTheme.objectColors[indexTmp]));
					previewET.triggers.Clear();
					previewET.triggers.Add(Triggers.CreatePreviewClickTrigger(preview, dropper, hex, __instance.previewTheme.backgroundColors[indexTmp]));
				});

				dropper.color = Triggers.InvertColorHue(Triggers.InvertColorValue(__instance.previewTheme.objectColors[indexTmp]));
				previewET.triggers.Clear();
				previewET.triggers.Add(Triggers.CreatePreviewClickTrigger(preview, dropper, hex, __instance.previewTheme.backgroundColors[indexTmp]));
			}
			return false;
		}

		[HarmonyPatch("CreatePreviewClickTrigger")]
		[HarmonyPrefix]
		static bool CreatePreviewClickTriggerPatch(ref EventTrigger.Entry __result, Transform __0, Transform __1, Color __2)
        {
			__result = Triggers.CreatePreviewClickTrigger(__0.GetComponent<Image>(), __0.GetChild(0).GetComponent<Image>(), __1.GetComponent<InputField>(), __2);
			return false;
        }

		[HarmonyPatch("CreateNewEventObject", typeof(float), typeof(int))]
		[HarmonyPrefix]
		public static bool CreateNewEventObject(float __0, int __1)
		{
			DataManager.GameData.EventKeyframe eventKeyframe = null;
			if (DataManager.inst.gameData.eventObjects.allEvents[__1].Count != 0)
			{
				int num = DataManager.inst.gameData.eventObjects.allEvents[__1].FindLastIndex((DataManager.GameData.EventKeyframe x) => x.eventTime <= __0);
				Debug.Log("Prior Index: " + num);
				eventKeyframe = DataManager.GameData.EventKeyframe.DeepCopy(DataManager.inst.gameData.eventObjects.allEvents[__1][num], true);
			}
			else
            {
				eventKeyframe = new DataManager.GameData.EventKeyframe { eventTime = AudioManager.inst.CurrentAudioSource.time, eventValues = new float[9] };
            }
			eventKeyframe.eventTime = __0;
			if (__1 == 2)
			{
				eventKeyframe.SetEventValues(new float[1]);
			}
			DataManager.inst.gameData.eventObjects.allEvents[__1].Add(eventKeyframe);
			DataManager.inst.gameData.eventObjects.allEvents[__1] = (from x in DataManager.inst.gameData.eventObjects.allEvents[__1]
																	   orderby x.eventTime
																	   select x).ToList();
			EventManager.inst.updateEvents();
			EventEditor.inst.SetCurrentEvent(__1, DataManager.inst.gameData.eventObjects.allEvents[__1].Count - 1);
			EventEditor.inst.CreateEventObjects();
			return false;
		}

		[HarmonyPatch("CreateEventObjects")]
		[HarmonyPrefix]
		private static bool CreateEventObjectsPrefix()
        {
			var eventEditor = EventEditor.inst;
			if (eventEditor.eventObjects.Count > 0)
			{
				foreach (var eventObject in eventEditor.eventObjects)
				{
					foreach (var @object in eventObject)
						Destroy(@object);
					eventObject.Clear();
				}
			}
			eventEditor.eventDrag = false;

			for (int type = 0; type < DataManager.inst.gameData.eventObjects.allEvents.Count; type++)
            {
				for (int index = 0; index < DataManager.inst.gameData.eventObjects.allEvents[type].Count; index++)
				{
					var eventKeyframe = DataManager.inst.gameData.eventObjects.allEvents[type][index];
					if (eventLayer == 0 && type < 14)
					{
						double eventTime = eventKeyframe.eventTime;
						var gameObject = Instantiate(eventEditor.TimelinePrefab);
						gameObject.name = "new keyframe - " + type.ToString();
						gameObject.transform.SetParent(eventEditor.EventHolders.transform.GetChild(type));
						gameObject.transform.localScale = Vector3.one;

						var image = gameObject.transform.GetChild(0).GetComponent<Image>();

						if (eventEditor.keyframeSelections.FindIndex(x => x.Type == type && x.Index == index) != -1 && EditorManager.inst.currentDialog.Type == EditorManager.EditorDialog.DialogType.Event)
							image.color = eventEditor.Selected;
						else if (eventEditor.currentEvent == index && eventEditor.currentEventType == type && EditorManager.inst.currentDialog.Type == EditorManager.EditorDialog.DialogType.Event)
							image.color = eventEditor.Selected;
						else
							image.color = eventEditor.EventColors[type];
						eventEditor.eventObjects[type].Add(gameObject);

						var triggers = gameObject.GetComponent<EventTrigger>().triggers;

						triggers.Clear();
						triggers.Add(Triggers.CreateEventObjectTrigger(eventEditor, type, index));
						triggers.Add(Triggers.CreateEventStartDragTrigger(eventEditor, type, index));
						triggers.Add(Triggers.CreateEventEndDragTrigger());
					}
					else if (eventLayer == 1 && type >= 14)
					{
						double eventTime = eventKeyframe.eventTime;
						var gameObject = Instantiate(eventEditor.TimelinePrefab);
						gameObject.name = "new keyframe - " + (type - 14).ToString();
						gameObject.transform.SetParent(eventEditor.EventHolders.transform.GetChild(type - 14));
						gameObject.transform.localScale = Vector3.one;

						var image = gameObject.transform.GetChild(0).GetComponent<Image>();

						if (eventEditor.keyframeSelections.FindIndex(x => x.Type == type && x.Index == index) != -1 && EditorManager.inst.currentDialog.Type == EditorManager.EditorDialog.DialogType.Event)
							image.color = eventEditor.Selected;
						else if (eventEditor.currentEvent == index && eventEditor.currentEventType == type && EditorManager.inst.currentDialog.Type == EditorManager.EditorDialog.DialogType.Event)
							image.color = eventEditor.Selected;
						else
							image.color = eventEditor.EventColors[type - 14];
						eventEditor.eventObjects[type - 14].Add(gameObject);

						var triggers = gameObject.GetComponent<EventTrigger>().triggers;

						triggers.Clear();
						triggers.Add(Triggers.CreateEventObjectTrigger(eventEditor, type, index));
						triggers.Add(Triggers.CreateEventStartDragTrigger(eventEditor, type, index));
						triggers.Add(Triggers.CreateEventEndDragTrigger());
					}
				}
            }

			RenderEventObjectsPatch();
			EventManager.inst.updateEvents();
			return false;
		}

		[HarmonyPatch("RenderEventObjects")]
		[HarmonyPrefix]
		private static bool RenderEventObjectsPatch()
		{
			var eventEditor = EventEditor.inst;
			if (EditorManager.inst.layer == 5)
			{
				for (int type = 0; type < DataManager.inst.gameData.eventObjects.allEvents.Count; type++)
                {
					for (int index = 0; index < DataManager.inst.gameData.eventObjects.allEvents[type].Count; index++)
					{
						var eventKeyframe = DataManager.inst.gameData.eventObjects.allEvents[type][index];
						if (eventLayer == 0 && type < 14)
						{
							float eventTime = eventKeyframe.eventTime;
							int baseUnit = EditorManager.BaseUnit;
							eventEditor.eventObjects[type][index].GetComponent<RectTransform>().anchoredPosition = new Vector2(eventTime * EditorManager.inst.Zoom - EditorManager.BaseUnit / 2, 0.0f);

							var image = eventEditor.eventObjects[type][index].transform.GetChild(0).GetComponent<Image>();

							if (eventEditor.keyframeSelections.FindIndex(x => x.Type == type && x.Index == index) != -1)
								image.color = LSColors.white;
							else
								image.color = eventEditor.EventColors[type];
							eventEditor.eventObjects[type][index].SetActive(true);
						}
						else if (eventLayer == 1 && type >= 14)
						{
							float eventTime = eventKeyframe.eventTime;
							int baseUnit = EditorManager.BaseUnit;
							eventEditor.eventObjects[type - 14][index].GetComponent<RectTransform>().anchoredPosition = new Vector2(eventTime * EditorManager.inst.Zoom - EditorManager.BaseUnit / 2, 0.0f);

							var image = eventEditor.eventObjects[type - 14][index].transform.GetChild(0).GetComponent<Image>();

							if (eventEditor.keyframeSelections.FindIndex(x => x.Type == type && x.Index == index) != -1)
								image.color = LSColors.white;
							else
								image.color = eventEditor.EventColors[type - 14];
							eventEditor.eventObjects[type - 14][index].SetActive(true);
						}
					}
                }
			}
			return false;
		}

		[HarmonyPatch("SetCurrentEvent")]
		[HarmonyPrefix]
		private static bool SetCurrentEvent(EventEditor __instance, int __0, int __1)
        {
			__instance.keyframeSelections.Clear();
			RTEditor.AddEvent(__instance, __0, __1);
			Debug.LogFormat("{0}Select Event Keyframe -> Type {1} -> Event {2}", new object[]
			{
				"[<color=#e65100>EventEditor</color>]\n",
				__0,
				__1
			});
			__instance.currentEventType = __0;
			__instance.currentEvent = __1;
			__instance.RenderEventObjects();
			__instance.OpenDialog();
			return false;
        }

		[HarmonyPatch("AddedSelectedEvent")]
		[HarmonyPrefix]
		public static bool AddedSelectedEvent(EventEditor __instance, int __0, int __1)
		{
			RTEditor.AddEvent(__instance, __0, __1);
			return false;
		}

		[HarmonyPatch("NewKeyframeFromTimeline")]
		[HarmonyPrefix]
		public static bool NewKeyframeFromTimeline(EventEditor __instance, int __0)
		{
			float timeTmp = EditorManager.inst.GetTimelineTime(0f);
			Debug.LogFormat("{0}Current Type: {1}", EditorPlugin.className, __0);
			int num = DataManager.inst.gameData.eventObjects.allEvents[__0].FindLastIndex((DataManager.GameData.EventKeyframe x) => x.eventTime <= timeTmp);
			Debug.LogFormat("{0}Prior Index: {1}", EditorPlugin.className, num);
			DataManager.GameData.EventKeyframe eventKeyframe = DataManager.GameData.EventKeyframe.DeepCopy(DataManager.inst.gameData.eventObjects.allEvents[__0][num], true);
			eventKeyframe.eventTime = timeTmp;
			if (__0 == 2)
			{
				eventKeyframe.SetEventValues(new float[1]);
			}
			DataManager.inst.gameData.eventObjects.allEvents[__0].Add(eventKeyframe);
			EventManager.inst.updateEvents();
			__instance.CreateEventObjects();
			DataManager.inst.gameData.eventObjects.allEvents[__0] = (from x in DataManager.inst.gameData.eventObjects.allEvents[__0]
																	   orderby x.eventTime
																	   select x).ToList();
			__instance.SetCurrentEvent(__0, DataManager.inst.gameData.eventObjects.allEvents[__0].IndexOf(eventKeyframe));
			return false;
		}
	}
}
