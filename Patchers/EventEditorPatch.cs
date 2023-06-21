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

using EditorManagement.Functions;
using EditorManagement.Functions.Tools;

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
			eventEditorTitleColors.Add("- Screen Overlay Editor -", new Color(0.3176471f, 0.3686275f, 0.2588235f, 1f)); //8
			eventEditorTitleColors.Add("- Timeline Editor -", new Color(0.4039216f, 0.4117647f, 0.2745098f, 1f)); //9
			eventEditorTitleColors.Add("- Player Event Editor -", new Color(0.4470589f, 0.3882353f, 0.2117647f, 1f)); //10

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
						eventLabels.transform.GetChild(7).GetChild(0).GetComponent<Text>().text = "Screen Overlay";
						eventLabels.transform.GetChild(8).GetChild(0).GetComponent<Text>().text = "Timeline";
						eventLabels.transform.GetChild(9).GetChild(0).GetComponent<Text>().text = "Player (Coming soon)";
						eventLabels.transform.GetChild(10).GetChild(0).GetComponent<Text>().text = "Follow Player (Coming soon)";
						eventLabels.transform.GetChild(11).GetChild(0).GetComponent<Text>().text = "??? (Coming soon)";
						eventLabels.transform.GetChild(12).GetChild(0).GetComponent<Text>().text = "AnalogGlitch (Coming soon)";
						eventLabels.transform.GetChild(13).GetChild(0).GetComponent<Text>().text = "DigitalGlitch (Coming soon)";
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

			if (GameObject.Find("BepInEx_Manager").GetComponentByName("EventsCorePlugin"))
            {
				eventsCore = true;
            }

			var colorButtons = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/color/color");
			var vignette = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/vignette").transform;
			Destroy(vignette.GetChild(8).gameObject);
			Destroy(vignette.Find("color").gameObject);

			if (eventsCore)
			{
				var cb1 = Instantiate(colorButtons);
				cb1.transform.localScale = Vector3.one;
				cb1.transform.SetParent(vignette);

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
				cb2.transform.localScale = Vector3.one;
				cb2.transform.SetParent(bloom);

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
				overlay.name = "overlay";

				overlay.transform.GetChild(8).GetChild(0).GetComponent<Text>().text = "Overlay Opacity";
				overlay.transform.Find("offset").gameObject.name = "alpha";

				var colorOverlayLabel = Instantiate(overlay.transform.GetChild(8).gameObject);
				colorOverlayLabel.transform.SetParent(overlay.transform);
				colorOverlayLabel.transform.localScale = Vector3.one;
				colorOverlayLabel.transform.GetChild(0).GetComponent<Text>().text = "Overlay Color";

				var colorOverlay = Instantiate(gradient.transform.Find("color1").gameObject);
				colorOverlay.transform.localScale = Vector3.one;
				colorOverlay.transform.SetParent(overlay.transform);
				colorOverlay.name = "color";

				overlayColorButtons.Clear();
				for (int i = 0; i < 19; i++)
				{
					overlayColorButtons.Add(colorOverlay.transform.GetChild(i).gameObject.GetComponent<Toggle>());
				}

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
				colorTimeline.transform.localScale = Vector3.one;
				colorTimeline.transform.SetParent(tl.transform);
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

				var pp = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move"));
				pp.transform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right").transform);
				pp.transform.localScale = Vector3.one;
			}

			yield break;
        }

		public static Toggle loggle;

		[HarmonyPatch("Update")]
		[HarmonyPrefix]
		private static bool UpdatePatch(EventEditor __instance)
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
					EventManager.inst.updateEvents();
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
        private static bool RenderEventsDialogPatch(EventEditor __instance)
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
							float num = float.Parse(val);
							currentKeyframe.eventValues[0] = num;
							eventManager.updateEvents();
						});

						posY.onValueChanged.RemoveAllListeners();
						posY.text = currentKeyframe.eventValues[1].ToString("f2");
						posY.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							currentKeyframe.eventValues[1] = num;
							eventManager.updateEvents();
						});

						var posXLeft = posX.transform.Find("<").GetComponent<Button>();
						var posXRight = posX.transform.Find(">").GetComponent<Button>();
						var posYLeft = posY.transform.Find("<").GetComponent<Button>();
						var posYRight = posY.transform.Find(">").GetComponent<Button>();

						posXLeft.onClick.RemoveAllListeners();
						posXLeft.onClick.AddListener(delegate ()
						{
							posX.text = (currentKeyframe.eventValues[0] - ConfigEntries.EventMoveModify.Value).ToString();
						});

						posXRight.onClick.RemoveAllListeners();
						posXRight.onClick.AddListener(delegate ()
						{
							posX.text = (currentKeyframe.eventValues[0] + ConfigEntries.EventMoveModify.Value).ToString();
						});

						posYLeft.onClick.RemoveAllListeners();
						posYLeft.onClick.AddListener(delegate ()
						{
							posY.text = (currentKeyframe.eventValues[1] - ConfigEntries.EventMoveModify.Value).ToString();
						});

						posYRight.onClick.RemoveAllListeners();
						posYRight.onClick.AddListener(delegate ()
						{
							posY.text = (currentKeyframe.eventValues[1] + ConfigEntries.EventMoveModify.Value).ToString();
						});

						if (!dialogTmp.Find("position/x").GetComponent<EventTrigger>())
                        {
							dialogTmp.Find("position/x").gameObject.AddComponent<EventTrigger>();
                        }

						if (!dialogTmp.Find("position/y").GetComponent<EventTrigger>())
                        {
							dialogTmp.Find("position/y").gameObject.AddComponent<EventTrigger>();
                        }

						var posXET = dialogTmp.Find("position/x").GetComponent<EventTrigger>();
						var posYET = dialogTmp.Find("position/y").GetComponent<EventTrigger>();

						posXET.triggers.Clear();
						posXET.triggers.Add(Triggers.ScrollDelta(posX, 0.1f, 10f, true));
						posXET.triggers.Add(Triggers.ScrollDeltaVector2(posX, posY, 0.1f, 10f));

						posYET.triggers.Clear();
						posYET.triggers.Add(Triggers.ScrollDelta(posY, 0.1f, 10f, true));
						posYET.triggers.Add(Triggers.ScrollDeltaVector2(posX, posY, 0.1f, 10f));
                        break;
                    }
				case 1: //Zoom
                    {
						var zoom = dialogTmp.Find("zoom/x").GetComponent<InputField>();
						zoom.onValueChanged.RemoveAllListeners();
						zoom.text = currentKeyframe.eventValues[0].ToString("f2");
						zoom.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							num = Mathf.Clamp(num, -9999f, 9999f);
							currentKeyframe.eventValues[0] = num;
							eventManager.updateEvents();
						});

						var zoomLeft = zoom.transform.Find("<").GetComponent<Button>();
						var zoomRight = zoom.transform.Find(">").GetComponent<Button>();

						zoomLeft.onClick.RemoveAllListeners();
						zoomLeft.onClick.AddListener(delegate ()
						{
							zoom.text = Mathf.Clamp(currentKeyframe.eventValues[0] - ConfigEntries.EventZoomModify.Value, -9999f, 9999f).ToString();
							eventManager.updateEvents();
						});

						zoomRight.onClick.RemoveAllListeners();
						zoomRight.onClick.AddListener(delegate ()
						{
							zoom.text = Mathf.Clamp(currentKeyframe.eventValues[0] + ConfigEntries.EventZoomModify.Value, -9999f, 9999f).ToString();
							eventManager.updateEvents();
						});

						if (!dialogTmp.Find("zoom/x").GetComponent<EventTrigger>())
                        {
							dialogTmp.Find("zoom/x").gameObject.AddComponent<EventTrigger>();
                        }

						var zoomET = dialogTmp.Find("zoom/x").GetComponent<EventTrigger>();

						zoomET.triggers.Clear();
						zoomET.triggers.Add(Triggers.ScrollDelta(zoom, 0.1f, 10f));
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

						var rotateLeft = rotate.transform.Find("<").GetComponent<Button>();
						var rotateRight = rotate.transform.Find("<").GetComponent<Button>();

						rotateLeft.onClick.RemoveAllListeners();
						rotateLeft.onClick.AddListener(delegate ()
						{
							rotate.text = (currentKeyframe.eventValues[0] - ConfigEntries.EventRotateModify.Value).ToString();
							eventManager.updateEvents();
						});

						rotateRight.onClick.RemoveAllListeners();
						rotateRight.onClick.AddListener(delegate ()
						{
							rotate.text = (currentKeyframe.eventValues[0] + ConfigEntries.EventRotateModify.Value).ToString();
							eventManager.updateEvents();
						});

						if (!dialogTmp.Find("rotation/x").GetComponent<EventTrigger>())
                        {
							dialogTmp.Find("rotation/x").gameObject.AddComponent<EventTrigger>();
                        }

						var rotateET = dialogTmp.Find("rotation/x").GetComponent<EventTrigger>();

						rotateET.triggers.Clear();
						rotateET.triggers.Add(Triggers.ScrollDelta(rotate, 15f, 3f));
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

							var shakeLeft = shake.transform.Find("<").GetComponent<Button>();
							var shakeRight = shake.transform.Find(">").GetComponent<Button>();

							shakeLeft.onClick.RemoveAllListeners();
							shakeLeft.onClick.AddListener(delegate ()
							{
								shake.text = Mathf.Clamp(currentKeyframe.eventValues[0] - ConfigEntries.EventShakeModify.Value, 0f, 10f).ToString();
								eventManager.updateEvents();
							});

							shakeRight.onClick.RemoveAllListeners();
							shakeRight.onClick.AddListener(delegate ()
							{
								shake.text = Mathf.Clamp(currentKeyframe.eventValues[0] + ConfigEntries.EventShakeModify.Value, 0f, 10f).ToString();
								eventManager.updateEvents();
							});

							if (!dialogTmp.Find("shake/x").GetComponent<EventTrigger>())
							{
								dialogTmp.Find("shake/x").gameObject.AddComponent<EventTrigger>();
							}

							var shakeET = dialogTmp.Find("shake/x").GetComponent<EventTrigger>();

							shakeET.triggers.Clear();
							shakeET.triggers.Add(Triggers.ScrollDelta(shake, 0.1f, 10f));
						}
						if (eventsCore)
						{
							//Shake Intensity X
							{
								var shake = dialogTmp.Find("direction/x").GetComponent<InputField>();
								shake.onValueChanged.RemoveAllListeners();
								shake.text = currentKeyframe.eventValues[1].ToString("f2");
								shake.onValueChanged.AddListener(delegate (string val)
								{
									float num = float.Parse(val);
									currentKeyframe.eventValues[1] = num;
									eventManager.updateEvents();
								});

								var shakeLeft = shake.transform.Find("<").GetComponent<Button>();
								var shakeRight = shake.transform.Find(">").GetComponent<Button>();

								shakeLeft.onClick.RemoveAllListeners();
								shakeLeft.onClick.AddListener(delegate ()
								{
									shake.text = (currentKeyframe.eventValues[1] - ConfigEntries.EventShakeModify.Value).ToString();
									eventManager.updateEvents();
								});

								shakeRight.onClick.RemoveAllListeners();
								shakeRight.onClick.AddListener(delegate ()
								{
									shake.text = (currentKeyframe.eventValues[1] + ConfigEntries.EventShakeModify.Value).ToString();
									eventManager.updateEvents();
								});

								if (!dialogTmp.Find("direction/x").GetComponent<EventTrigger>())
								{
									dialogTmp.Find("direction/x").gameObject.AddComponent<EventTrigger>();
								}

								var shakeET = dialogTmp.Find("direction/x").GetComponent<EventTrigger>();

								shakeET.triggers.Clear();
								shakeET.triggers.Add(Triggers.ScrollDelta(shake, 0.1f, 10f));
							}

							//Shake Intensity Y
							{
								var shake = dialogTmp.Find("direction/y").GetComponent<InputField>();
								shake.onValueChanged.RemoveAllListeners();
								shake.text = currentKeyframe.eventValues[2].ToString("f2");
								shake.onValueChanged.AddListener(delegate (string val)
								{
									float num = float.Parse(val);
									currentKeyframe.eventValues[2] = num;
									eventManager.updateEvents();
								});

								var shakeLeft = shake.transform.Find("<").GetComponent<Button>();
								var shakeRight = shake.transform.Find(">").GetComponent<Button>();

								shakeLeft.onClick.RemoveAllListeners();
								shakeLeft.onClick.AddListener(delegate ()
								{
									shake.text = (currentKeyframe.eventValues[2] - ConfigEntries.EventShakeModify.Value).ToString();
									eventManager.updateEvents();
								});

								shakeRight.onClick.RemoveAllListeners();
								shakeRight.onClick.AddListener(delegate ()
								{
									shake.text = (currentKeyframe.eventValues[2] + ConfigEntries.EventShakeModify.Value).ToString();
									eventManager.updateEvents();
								});

								if (!dialogTmp.Find("direction/y").GetComponent<EventTrigger>())
								{
									dialogTmp.Find("direction/y").gameObject.AddComponent<EventTrigger>();
								}

								var shakeET = dialogTmp.Find("direction/y").GetComponent<EventTrigger>();

								shakeET.triggers.Clear();
								shakeET.triggers.Add(Triggers.ScrollDelta(shake, 0.1f, 10f));
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
						var chroma = dialogTmp.Find("chroma/x").GetComponent<InputField>();
						chroma.onValueChanged.RemoveAllListeners();
						chroma.text = currentKeyframe.eventValues[0].ToString("f2");
						chroma.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							num = Mathf.Clamp(num, 0f, 9999f);
							currentKeyframe.eventValues[0] = num;
							eventManager.updateEvents();
						});

						var chromaLeft = chroma.transform.Find("<").GetComponent<Button>();
						var chromaRight = chroma.transform.Find(">").GetComponent<Button>();

						chromaLeft.onClick.RemoveAllListeners();
						chromaLeft.onClick.AddListener(delegate ()
						{
							chroma.text = Mathf.Clamp(currentKeyframe.eventValues[0] - ConfigEntries.EventChromaModify.Value, 0f, 9999f).ToString();
							eventManager.updateEvents();
						});

						chromaRight.onClick.RemoveAllListeners();
						chromaRight.onClick.AddListener(delegate ()
						{
							chroma.text = Mathf.Clamp(currentKeyframe.eventValues[0] + ConfigEntries.EventChromaModify.Value, 0f, 9999f).ToString();
							eventManager.updateEvents();
						});

						if (!dialogTmp.Find("chroma/x").GetComponent<EventTrigger>())
                        {
							dialogTmp.Find("chroma/x").gameObject.AddComponent<EventTrigger>();
                        }

						var chromaET = dialogTmp.Find("chroma/x").GetComponent<EventTrigger>();

						chromaET.triggers.Clear();
						chromaET.triggers.Add(Triggers.ScrollDelta(chroma, 0.1f, 10f));
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

							var bloomLeft = bloom.transform.Find("<").GetComponent<Button>();
							var bloomRight = bloom.transform.Find(">").GetComponent<Button>();

							bloomLeft.onClick.RemoveAllListeners();
							bloomLeft.onClick.AddListener(delegate ()
							{
								bloom.text = Mathf.Clamp(currentKeyframe.eventValues[0] - ConfigEntries.EventBloomModify.Value, 0f, 1280f).ToString();
								eventManager.updateEvents();
							});

							bloomRight.onClick.RemoveAllListeners();
							bloomRight.onClick.AddListener(delegate ()
							{
								bloom.text = Mathf.Clamp(currentKeyframe.eventValues[0] + ConfigEntries.EventBloomModify.Value, 0f, 1280f).ToString();
								eventManager.updateEvents();
							});

							if (!dialogTmp.Find("bloom/x").GetComponent<EventTrigger>())
							{
								dialogTmp.Find("bloom/x").gameObject.AddComponent<EventTrigger>();
							}

							var bloomET = dialogTmp.Find("bloom/x").GetComponent<EventTrigger>();

							bloomET.triggers.Clear();
							bloomET.triggers.Add(Triggers.ScrollDelta(bloom, 0.1f, 10f));
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
									currentKeyframe.eventValues[1] = num;
									eventManager.updateEvents();
								});

								var bloomLeft = bloom.transform.Find("<").GetComponent<Button>();
								var bloomRight = bloom.transform.Find(">").GetComponent<Button>();

								bloomLeft.onClick.RemoveAllListeners();
								bloomLeft.onClick.AddListener(delegate ()
								{
									bloom.text = (currentKeyframe.eventValues[1] - ConfigEntries.EventBloomModify.Value).ToString();
									eventManager.updateEvents();
								});

								bloomRight.onClick.RemoveAllListeners();
								bloomRight.onClick.AddListener(delegate ()
								{
									bloom.text = (currentKeyframe.eventValues[1] + ConfigEntries.EventBloomModify.Value).ToString();
									eventManager.updateEvents();
								});

								if (!dialogTmp.Find("diffusion").GetComponent<EventTrigger>())
								{
									dialogTmp.Find("diffusion").gameObject.AddComponent<EventTrigger>();
								}

								var bloomET = dialogTmp.Find("diffusion").GetComponent<EventTrigger>();

								bloomET.triggers.Clear();
								bloomET.triggers.Add(Triggers.ScrollDelta(bloom, 0.1f, 10f));
							}

							//Bloom Threshold
							{
								var bloom = dialogTmp.Find("threshold").GetComponent<InputField>();
								bloom.onValueChanged.RemoveAllListeners();
								bloom.text = currentKeyframe.eventValues[2].ToString("f2");
								bloom.onValueChanged.AddListener(delegate (string val)
								{
									float num = float.Parse(val);
									currentKeyframe.eventValues[2] = num;
									eventManager.updateEvents();
								});

								var bloomLeft = bloom.transform.Find("<").GetComponent<Button>();
								var bloomRight = bloom.transform.Find(">").GetComponent<Button>();

								bloomLeft.onClick.RemoveAllListeners();
								bloomLeft.onClick.AddListener(delegate ()
								{
									bloom.text = (currentKeyframe.eventValues[2] - ConfigEntries.EventBloomModify.Value).ToString();
									eventManager.updateEvents();
								});

								bloomRight.onClick.RemoveAllListeners();
								bloomRight.onClick.AddListener(delegate ()
								{
									bloom.text = (currentKeyframe.eventValues[2] + ConfigEntries.EventBloomModify.Value).ToString();
									eventManager.updateEvents();
								});

								if (!dialogTmp.Find("threshold").GetComponent<EventTrigger>())
								{
									dialogTmp.Find("threshold").gameObject.AddComponent<EventTrigger>();
								}

								var bloomET = dialogTmp.Find("threshold").GetComponent<EventTrigger>();

								bloomET.triggers.Clear();
								bloomET.triggers.Add(Triggers.ScrollDelta(bloom, 0.1f, 10f));
							}

							//Bloom Anamorphic Ratio
							{
								var bloom = dialogTmp.Find("anamorphic ratio").GetComponent<InputField>();
								bloom.onValueChanged.RemoveAllListeners();
								bloom.text = currentKeyframe.eventValues[3].ToString("f2");
								bloom.onValueChanged.AddListener(delegate (string val)
								{
									float num = float.Parse(val);
									currentKeyframe.eventValues[3] = num;
									eventManager.updateEvents();
								});

								var bloomLeft = bloom.transform.Find("<").GetComponent<Button>();
								var bloomRight = bloom.transform.Find(">").GetComponent<Button>();

								bloomLeft.onClick.RemoveAllListeners();
								bloomLeft.onClick.AddListener(delegate ()
								{
									bloom.text = (currentKeyframe.eventValues[3] - ConfigEntries.EventBloomModify.Value).ToString();
									eventManager.updateEvents();
								});

								bloomRight.onClick.RemoveAllListeners();
								bloomRight.onClick.AddListener(delegate ()
								{
									bloom.text = (currentKeyframe.eventValues[3] + ConfigEntries.EventBloomModify.Value).ToString();
									eventManager.updateEvents();
								});

								if (!dialogTmp.Find("anamorphic ratio").GetComponent<EventTrigger>())
								{
									dialogTmp.Find("anamorphic ratio").gameObject.AddComponent<EventTrigger>();
								}

								var bloomET = dialogTmp.Find("anamorphic ratio").GetComponent<EventTrigger>();

								bloomET.triggers.Clear();
								bloomET.triggers.Add(Triggers.ScrollDelta(bloom, 0.1f, 10f));
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
						var vignetteIntensity = dialogTmp.Find("intensity").GetComponent<InputField>();

						vignetteIntensity.onValueChanged.RemoveAllListeners();
						vignetteIntensity.text = currentKeyframe.eventValues[0].ToString("f2");
						vignetteIntensity.onValueChanged.AddListener(delegate (string val)
						{
							currentKeyframe.eventValues[0] = float.Parse(val);
							eventManager.updateEvents();
						});

						var vignetteIntensityLeft = vignetteIntensity.transform.Find("<").GetComponent<Button>();
						var vignetteIntensityRight = vignetteIntensity.transform.Find(">").GetComponent<Button>();

						vignetteIntensityLeft.onClick.RemoveAllListeners();
						vignetteIntensityLeft.onClick.AddListener(delegate ()
						{
							vignetteIntensity.text = (currentKeyframe.eventValues[0] - ConfigEntries.EventVignetteIntensityModify.Value).ToString();
							eventManager.updateEvents();
						});

						vignetteIntensityRight.onClick.RemoveAllListeners();
						vignetteIntensityRight.onClick.AddListener(delegate ()
						{
							vignetteIntensity.text = (currentKeyframe.eventValues[0] + ConfigEntries.EventVignetteIntensityModify.Value).ToString();
							eventManager.updateEvents();
						});

						if (!dialogTmp.Find("intensity").GetComponent<EventTrigger>())
                        {
							dialogTmp.Find("intensity").gameObject.AddComponent<EventTrigger>();
                        }

						var vignetteIntensityET = dialogTmp.Find("intensity").GetComponent<EventTrigger>();

						vignetteIntensityET.triggers.Clear();
						vignetteIntensityET.triggers.Add(Triggers.ScrollDelta(vignetteIntensity, 0.1f, 10f));

						var vignetteSmoothness = dialogTmp.Find("smoothness").GetComponent<InputField>();

						vignetteSmoothness.onValueChanged.RemoveAllListeners();
						vignetteSmoothness.text = currentKeyframe.eventValues[1].ToString("f2");
						vignetteSmoothness.onValueChanged.AddListener(delegate (string val)
						{
							currentKeyframe.eventValues[1] = float.Parse(val);
							eventManager.updateEvents();
						});

						var vignetteSmoothnessLeft = vignetteSmoothness.transform.Find("<").GetComponent<Button>();
						var vignetteSmoothnessRight = vignetteSmoothness.transform.Find(">").GetComponent<Button>();

						vignetteSmoothnessLeft.onClick.RemoveAllListeners();
						vignetteSmoothnessLeft.onClick.AddListener(delegate ()
						{
							vignetteSmoothness.text = (currentKeyframe.eventValues[1] - ConfigEntries.EventVignetteSmoothnessModify.Value).ToString();
							eventManager.updateEvents();
						});

						vignetteSmoothnessRight.onClick.RemoveAllListeners();
						vignetteSmoothnessRight.onClick.AddListener(delegate ()
						{
							vignetteSmoothness.text = (currentKeyframe.eventValues[1] + ConfigEntries.EventVignetteSmoothnessModify.Value).ToString();
							eventManager.updateEvents();
						});

						if (!dialogTmp.Find("smoothness").GetComponent<EventTrigger>())
						{
							dialogTmp.Find("smoothness").gameObject.AddComponent<EventTrigger>();
						}

						var vignetteSmoothnessET = dialogTmp.Find("smoothness").GetComponent<EventTrigger>();

						vignetteSmoothnessET.triggers.Clear();
						vignetteSmoothnessET.triggers.Add(Triggers.ScrollDelta(vignetteSmoothness, 0.1f, 10f));

						var vignetteRounded = dialogTmp.Find("roundness/rounded").GetComponent<Toggle>();
						vignetteRounded.onValueChanged.RemoveAllListeners();
						vignetteRounded.isOn = (currentKeyframe.eventValues[2] == 1f);
						vignetteRounded.onValueChanged.AddListener(delegate (bool val)
						{
							currentKeyframe.eventValues[2] = (val ? 1 : 0);
							eventManager.updateEvents();
						});

						var vignetteRoundness = dialogTmp.Find("roundness").GetComponent<InputField>();

						vignetteRoundness.onValueChanged.RemoveAllListeners();
						vignetteRoundness.text = currentKeyframe.eventValues[3].ToString("f2");
						vignetteRoundness.onValueChanged.AddListener(delegate (string val)
						{
							currentKeyframe.eventValues[3] = float.Parse(val);
							eventManager.updateEvents();
						});

						var vignetteRoundnessLeft = vignetteRoundness.transform.Find("<").GetComponent<Button>();
						var vignetteRoundnessRight = vignetteRoundness.transform.Find(">").GetComponent<Button>();

						vignetteRoundnessLeft.onClick.RemoveAllListeners();
						vignetteRoundnessLeft.onClick.AddListener(delegate ()
						{
							vignetteRoundness.text = (currentKeyframe.eventValues[0] - ConfigEntries.EventVignetteRoundnessModify.Value).ToString();
							eventManager.updateEvents();
						});

						vignetteRoundnessRight.onClick.RemoveAllListeners();
						vignetteRoundnessRight.onClick.AddListener(delegate ()
						{
							vignetteRoundness.text = (currentKeyframe.eventValues[0] + ConfigEntries.EventVignetteRoundnessModify.Value).ToString();
							eventManager.updateEvents();
						});

						if (!dialogTmp.Find("roundness").GetComponent<EventTrigger>())
						{
							dialogTmp.Find("roundness").gameObject.AddComponent<EventTrigger>();
						}

						var vignetteRoundnessET = dialogTmp.Find("roundness").GetComponent<EventTrigger>();

						vignetteRoundnessET.triggers.Clear();
						vignetteRoundnessET.triggers.Add(Triggers.ScrollDelta(vignetteRoundness, 0.1f, 10f));

						var vignetteCenterX = dialogTmp.Find("position/x").GetComponent<InputField>();
						var vignetteCenterY = dialogTmp.Find("position/y").GetComponent<InputField>();

						vignetteCenterX.onValueChanged.RemoveAllListeners();
						vignetteCenterX.text = currentKeyframe.eventValues[4].ToString("f2");
						vignetteCenterX.onValueChanged.AddListener(delegate (string val)
						{
							currentKeyframe.eventValues[4] = float.Parse(val);
							eventManager.updateEvents();
						});

						vignetteCenterY.onValueChanged.RemoveAllListeners();
						vignetteCenterY.text = currentKeyframe.eventValues[5].ToString("f2");
						vignetteCenterY.onValueChanged.AddListener(delegate (string val)
						{
							currentKeyframe.eventValues[5] = float.Parse(val);
							eventManager.updateEvents();
						});

						var vignetteCenterXLeft = vignetteCenterX.transform.Find("<").GetComponent<Button>();
						var vignetteCenterXRight = vignetteCenterX.transform.Find(">").GetComponent<Button>();
						var vignetteCenterYLeft = vignetteCenterX.transform.Find("<").GetComponent<Button>();
						var vignetteCenterYRight = vignetteCenterX.transform.Find(">").GetComponent<Button>();

						vignetteCenterXLeft.onClick.RemoveAllListeners();
						vignetteCenterXLeft.onClick.AddListener(delegate ()
						{
							vignetteCenterX.text = (currentKeyframe.eventValues[4] - ConfigEntries.EventVignettePosModify.Value).ToString();
							eventManager.updateEvents();
						});

						vignetteCenterXLeft.onClick.RemoveAllListeners();
						vignetteCenterXLeft.onClick.AddListener(delegate ()
						{
							vignetteCenterX.text = (currentKeyframe.eventValues[4] + ConfigEntries.EventVignettePosModify.Value).ToString();
							eventManager.updateEvents();
						});

						vignetteCenterYLeft.onClick.RemoveAllListeners();
						vignetteCenterYLeft.onClick.AddListener(delegate ()
						{
							vignetteCenterY.text = (currentKeyframe.eventValues[5] - ConfigEntries.EventVignettePosModify.Value).ToString();
							eventManager.updateEvents();
						});

						vignetteCenterYLeft.onClick.RemoveAllListeners();
						vignetteCenterYLeft.onClick.AddListener(delegate ()
						{
							vignetteCenterY.text = (currentKeyframe.eventValues[5] + ConfigEntries.EventVignettePosModify.Value).ToString();
							eventManager.updateEvents();
						});

						if (!dialogTmp.Find("position/x").GetComponent<EventTrigger>())
                        {
							dialogTmp.Find("position/x").gameObject.AddComponent<EventTrigger>();
                        }

						if (!dialogTmp.Find("position/y").GetComponent<EventTrigger>())
                        {
							dialogTmp.Find("position/y").gameObject.AddComponent<EventTrigger>();
                        }

						var vignetteCenterXET = dialogTmp.Find("position/x").GetComponent<EventTrigger>();
						var vignetteCenterYET = dialogTmp.Find("position/y").GetComponent<EventTrigger>();

						vignetteCenterXET.triggers.Clear();
						vignetteCenterXET.triggers.Add(Triggers.ScrollDelta(vignetteCenterX, 0.1f, 10f, true));
						vignetteCenterXET.triggers.Add(Triggers.ScrollDeltaVector2(vignetteCenterX, vignetteCenterY, 0.1f, 10f));

						vignetteCenterYET.triggers.Clear();
						vignetteCenterYET.triggers.Add(Triggers.ScrollDelta(vignetteCenterY, 0.1f, 10f, true));
						vignetteCenterYET.triggers.Add(Triggers.ScrollDeltaVector2(vignetteCenterX, vignetteCenterY, 0.1f, 10f));

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
							var lens = dialogTmp.Find("lens/x").GetComponent<InputField>();
							lens.onValueChanged.RemoveAllListeners();
							lens.text = currentKeyframe.eventValues[0].ToString("f2");
							lens.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								num = Mathf.Clamp(num, -100f, 100f);
								currentKeyframe.eventValues[0] = num;
								eventManager.updateEvents();
							});

							var lensLeft = lens.transform.Find("<").GetComponent<Button>();
							var lensRight = lens.transform.Find(">").GetComponent<Button>();

							lensLeft.onClick.RemoveAllListeners();
							lensLeft.onClick.AddListener(delegate ()
							{
								lens.text = Mathf.Clamp(currentKeyframe.eventValues[0] - ConfigEntries.EventLensModify.Value, -100f, 100f).ToString();
								eventManager.updateEvents();
							});

							lensLeft.onClick.RemoveAllListeners();
							lensLeft.onClick.AddListener(delegate ()
							{
								lens.text = Mathf.Clamp(currentKeyframe.eventValues[0] + ConfigEntries.EventLensModify.Value, -100f, 100f).ToString();
								eventManager.updateEvents();
							});

							if (!dialogTmp.Find("lens/x").GetComponent<EventTrigger>())
							{
								dialogTmp.Find("lens/x").gameObject.AddComponent<EventTrigger>();
							}

							var lensET = dialogTmp.Find("lens/x").GetComponent<EventTrigger>();

							lensET.triggers.Clear();
							lensET.triggers.Add(Triggers.ScrollDelta(lens, 1f, 10f));
						}

						if (eventsCore)
						{
							//Center X
							{
								var lens = dialogTmp.Find("center/x").GetComponent<InputField>();
								lens.onValueChanged.RemoveAllListeners();
								lens.text = currentKeyframe.eventValues[1].ToString("f2");
								lens.onValueChanged.AddListener(delegate (string val)
								{
									float num = float.Parse(val);
									num = Mathf.Clamp(num, -100f, 100f);
									currentKeyframe.eventValues[1] = num;
									eventManager.updateEvents();
								});

								var lensLeft = lens.transform.Find("<").GetComponent<Button>();
								var lensRight = lens.transform.Find(">").GetComponent<Button>();

								lensLeft.onClick.RemoveAllListeners();
								lensLeft.onClick.AddListener(delegate ()
								{
									lens.text = Mathf.Clamp(currentKeyframe.eventValues[1] - ConfigEntries.EventLensModify.Value, -100f, 100f).ToString();
									eventManager.updateEvents();
								});

								lensLeft.onClick.RemoveAllListeners();
								lensLeft.onClick.AddListener(delegate ()
								{
									lens.text = Mathf.Clamp(currentKeyframe.eventValues[1] + ConfigEntries.EventLensModify.Value, -100f, 100f).ToString();
									eventManager.updateEvents();
								});

								if (!dialogTmp.Find("center/x").GetComponent<EventTrigger>())
								{
									dialogTmp.Find("center/x").gameObject.AddComponent<EventTrigger>();
								}

								var lensET = dialogTmp.Find("center/x").GetComponent<EventTrigger>();

								lensET.triggers.Clear();
								lensET.triggers.Add(Triggers.ScrollDelta(lens, 0.1f, 10f));
							}

							//Center Y
							{
								var lens = dialogTmp.Find("center/y").GetComponent<InputField>();
								lens.onValueChanged.RemoveAllListeners();
								lens.text = currentKeyframe.eventValues[2].ToString("f2");
								lens.onValueChanged.AddListener(delegate (string val)
								{
									float num = float.Parse(val);
									num = Mathf.Clamp(num, -100f, 100f);
									currentKeyframe.eventValues[2] = num;
									eventManager.updateEvents();
								});

								var lensLeft = lens.transform.Find("<").GetComponent<Button>();
								var lensRight = lens.transform.Find(">").GetComponent<Button>();

								lensLeft.onClick.RemoveAllListeners();
								lensLeft.onClick.AddListener(delegate ()
								{
									lens.text = Mathf.Clamp(currentKeyframe.eventValues[2] - ConfigEntries.EventLensModify.Value, -100f, 100f).ToString();
									eventManager.updateEvents();
								});

								lensLeft.onClick.RemoveAllListeners();
								lensLeft.onClick.AddListener(delegate ()
								{
									lens.text = Mathf.Clamp(currentKeyframe.eventValues[2] + ConfigEntries.EventLensModify.Value, -100f, 100f).ToString();
									eventManager.updateEvents();
								});

								if (!dialogTmp.Find("center/y").GetComponent<EventTrigger>())
								{
									dialogTmp.Find("center/y").gameObject.AddComponent<EventTrigger>();
								}

								var lensET = dialogTmp.Find("center/y").GetComponent<EventTrigger>();

								lensET.triggers.Clear();
								lensET.triggers.Add(Triggers.ScrollDelta(lens, 0.1f, 10f));
							}

							//Intensity X
							{
								var lens = dialogTmp.Find("intensity/x").GetComponent<InputField>();
								lens.onValueChanged.RemoveAllListeners();
								lens.text = currentKeyframe.eventValues[3].ToString("f2");
								lens.onValueChanged.AddListener(delegate (string val)
								{
									float num = float.Parse(val);
									num = Mathf.Clamp(num, -100f, 100f);
									currentKeyframe.eventValues[3] = num;
									eventManager.updateEvents();
								});

								var lensLeft = lens.transform.Find("<").GetComponent<Button>();
								var lensRight = lens.transform.Find(">").GetComponent<Button>();

								lensLeft.onClick.RemoveAllListeners();
								lensLeft.onClick.AddListener(delegate ()
								{
									lens.text = Mathf.Clamp(currentKeyframe.eventValues[3] - ConfigEntries.EventLensModify.Value, -100f, 100f).ToString();
									eventManager.updateEvents();
								});

								lensLeft.onClick.RemoveAllListeners();
								lensLeft.onClick.AddListener(delegate ()
								{
									lens.text = Mathf.Clamp(currentKeyframe.eventValues[3] + ConfigEntries.EventLensModify.Value, -100f, 100f).ToString();
									eventManager.updateEvents();
								});

								if (!dialogTmp.Find("intensity/x").GetComponent<EventTrigger>())
								{
									dialogTmp.Find("intensity/x").gameObject.AddComponent<EventTrigger>();
								}

								var lensET = dialogTmp.Find("intensity/x").GetComponent<EventTrigger>();

								lensET.triggers.Clear();
								lensET.triggers.Add(Triggers.ScrollDelta(lens, 0.1f, 10f));
							}

							//Intensity Y
							{
								var lens = dialogTmp.Find("intensity/y").GetComponent<InputField>();
								lens.onValueChanged.RemoveAllListeners();
								lens.text = currentKeyframe.eventValues[4].ToString("f2");
								lens.onValueChanged.AddListener(delegate (string val)
								{
									float num = float.Parse(val);
									num = Mathf.Clamp(num, -100f, 100f);
									currentKeyframe.eventValues[4] = num;
									eventManager.updateEvents();
								});

								var lensLeft = lens.transform.Find("<").GetComponent<Button>();
								var lensRight = lens.transform.Find(">").GetComponent<Button>();

								lensLeft.onClick.RemoveAllListeners();
								lensLeft.onClick.AddListener(delegate ()
								{
									lens.text = Mathf.Clamp(currentKeyframe.eventValues[4] - ConfigEntries.EventLensModify.Value, -100f, 100f).ToString();
									eventManager.updateEvents();
								});

								lensLeft.onClick.RemoveAllListeners();
								lensLeft.onClick.AddListener(delegate ()
								{
									lens.text = Mathf.Clamp(currentKeyframe.eventValues[4] + ConfigEntries.EventLensModify.Value, -100f, 100f).ToString();
									eventManager.updateEvents();
								});

								if (!dialogTmp.Find("intensity/y").GetComponent<EventTrigger>())
								{
									dialogTmp.Find("intensity/y").gameObject.AddComponent<EventTrigger>();
								}

								var lensET = dialogTmp.Find("intensity/y").GetComponent<EventTrigger>();

								lensET.triggers.Clear();
								lensET.triggers.Add(Triggers.ScrollDelta(lens, 0.1f, 10f));
							}

							//Scale
							{
								var lens = dialogTmp.Find("scale").GetComponent<InputField>();
								lens.onValueChanged.RemoveAllListeners();
								lens.text = currentKeyframe.eventValues[5].ToString("f2");
								lens.onValueChanged.AddListener(delegate (string val)
								{
									float num = float.Parse(val);
									num = Mathf.Clamp(num, -100f, 100f);
									currentKeyframe.eventValues[5] = num;
									eventManager.updateEvents();
								});

								var lensLeft = lens.transform.Find("<").GetComponent<Button>();
								var lensRight = lens.transform.Find(">").GetComponent<Button>();

								lensLeft.onClick.RemoveAllListeners();
								lensLeft.onClick.AddListener(delegate ()
								{
									lens.text = Mathf.Clamp(currentKeyframe.eventValues[5] - ConfigEntries.EventLensModify.Value, -100f, 100f).ToString();
									eventManager.updateEvents();
								});

								lensLeft.onClick.RemoveAllListeners();
								lensLeft.onClick.AddListener(delegate ()
								{
									lens.text = Mathf.Clamp(currentKeyframe.eventValues[5] + ConfigEntries.EventLensModify.Value, -100f, 100f).ToString();
									eventManager.updateEvents();
								});

								if (!dialogTmp.Find("scale").GetComponent<EventTrigger>())
								{
									dialogTmp.Find("scale").gameObject.AddComponent<EventTrigger>();
								}

								var lensET = dialogTmp.Find("scale").GetComponent<EventTrigger>();

								lensET.triggers.Clear();
								lensET.triggers.Add(Triggers.ScrollDelta(lens, 0.1f, 10f));
							}
						}
						break;
                    }
				case 9: //Grain
                    {
						var grainIntensity = dialogTmp.Find("intensity").GetComponent<InputField>();
						grainIntensity.onValueChanged.RemoveAllListeners();
						grainIntensity.text = currentKeyframe.eventValues[0].ToString("f2");
						grainIntensity.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							num = Mathf.Clamp(num, 0f, 9999f);
							currentKeyframe.eventValues[0] = num;
							eventManager.updateEvents();
						});

						var grainIntensityLeft = grainIntensity.transform.Find("<").GetComponent<Button>();
						var grainIntensityRight = grainIntensity.transform.Find(">").GetComponent<Button>();

						grainIntensityLeft.onClick.RemoveAllListeners();
						grainIntensityLeft.onClick.AddListener(delegate ()
						{
							grainIntensity.text = Mathf.Clamp(currentKeyframe.eventValues[0] - ConfigEntries.EventGrainIntensityModify.Value, 0f, 9999f).ToString();
							eventManager.updateEvents();
						});

						grainIntensityRight.onClick.RemoveAllListeners();
						grainIntensityRight.onClick.AddListener(delegate ()
						{
							grainIntensity.text = Mathf.Clamp(currentKeyframe.eventValues[0] + ConfigEntries.EventGrainIntensityModify.Value, 0f, 9999f).ToString();
							eventManager.updateEvents();
						});

						if (!dialogTmp.Find("intensity").GetComponent<EventTrigger>())
                        {
							dialogTmp.Find("intensity").gameObject.AddComponent<EventTrigger>();
                        }

						var grainIntensityET = dialogTmp.Find("intensity").GetComponent<EventTrigger>();
						grainIntensityET.triggers.Clear();
						grainIntensityET.triggers.Add(Triggers.ScrollDelta(grainIntensity, 0.1f, 10f));

						var grainColored = dialogTmp.Find("colored").GetComponent<Toggle>();
						grainColored.onValueChanged.RemoveAllListeners();
						grainColored.isOn = (currentKeyframe.eventValues[1] == 1f);
						grainColored.onValueChanged.AddListener(delegate (bool val)
						{
							currentKeyframe.eventValues[1] = (float)(val ? 1 : 0);
							eventManager.updateEvents();
						});

						var grainSize = dialogTmp.Find("size").GetComponent<InputField>();
						grainSize.onValueChanged.RemoveAllListeners();
						grainSize.text = currentKeyframe.eventValues[2].ToString("f2");
						grainSize.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							num = Mathf.Clamp(num, 0f, 9999f);
							currentKeyframe.eventValues[2] = num;
							eventManager.updateEvents();
						});

						var grainSizeLeft = grainSize.transform.Find("<").GetComponent<Button>();
						var grainSizeRight = grainSize.transform.Find(">").GetComponent<Button>();

						grainSizeLeft.onClick.RemoveAllListeners();
						grainSizeLeft.onClick.AddListener(delegate ()
						{
							grainSize.text = Mathf.Clamp(currentKeyframe.eventValues[2] - ConfigEntries.EventGrainSizeModify.Value, 0f, 9999f).ToString();
							eventManager.updateEvents();
						});

						grainSizeRight.onClick.RemoveAllListeners();
						grainSizeRight.onClick.AddListener(delegate ()
						{
							grainSize.text = Mathf.Clamp(currentKeyframe.eventValues[2] + ConfigEntries.EventGrainSizeModify.Value, 0f, 9999f).ToString();
							eventManager.updateEvents();
						});
						break;
					}
				case 10: //ColorGrading
					{
						//intensity
						var intensity = dialogTmp.Find("intensity").GetComponent<InputField>();
						intensity.onValueChanged.RemoveAllListeners();
						intensity.text = currentKeyframe.eventValues[0].ToString("f2");
						intensity.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							currentKeyframe.eventValues[0] = num;
							eventManager.updateEvents();
						});

						var intensityLeft = intensity.transform.Find("<").GetComponent<Button>();
						var intensityRight = intensity.transform.Find(">").GetComponent<Button>();

						intensityLeft.onClick.RemoveAllListeners();
						intensityLeft.onClick.AddListener(delegate ()
						{
							intensity.text = (currentKeyframe.eventValues[0] - ConfigEntries.EventMoveModify.Value).ToString();
							eventManager.updateEvents();
						});

						intensityRight.onClick.RemoveAllListeners();
						intensityRight.onClick.AddListener(delegate ()
						{
							intensity.text = (currentKeyframe.eventValues[0] + ConfigEntries.EventMoveModify.Value).ToString();
							eventManager.updateEvents();
						});

						if (!dialogTmp.Find("intensity").GetComponent<EventTrigger>())
						{
							dialogTmp.Find("intensity").gameObject.AddComponent<EventTrigger>();
						}

						var intensityET = dialogTmp.Find("intensity").GetComponent<EventTrigger>();
						intensityET.triggers.Clear();
						intensityET.triggers.Add(Triggers.ScrollDelta(intensity, 0.1f, 10f));

						//Contrast
						var contrast = dialogTmp.Find("contrast").GetComponent<InputField>();
						contrast.onValueChanged.RemoveAllListeners();
						contrast.text = currentKeyframe.eventValues[1].ToString("f2");
						contrast.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							currentKeyframe.eventValues[1] = num;
							eventManager.updateEvents();
						});

						var contrastLeft = contrast.transform.Find("<").GetComponent<Button>();
						var contrastRight = contrast.transform.Find(">").GetComponent<Button>();

						contrastLeft.onClick.RemoveAllListeners();
						contrastLeft.onClick.AddListener(delegate ()
						{
							contrast.text = (currentKeyframe.eventValues[1] - ConfigEntries.EventMoveModify.Value).ToString();
							eventManager.updateEvents();
						});

						contrastRight.onClick.RemoveAllListeners();
						contrastRight.onClick.AddListener(delegate ()
						{
							contrast.text = (currentKeyframe.eventValues[1] + ConfigEntries.EventMoveModify.Value).ToString();
							eventManager.updateEvents();
						});

						if (!dialogTmp.Find("contrast").GetComponent<EventTrigger>())
						{
							dialogTmp.Find("contrast").gameObject.AddComponent<EventTrigger>();
						}

						var contrastET = dialogTmp.Find("contrast").GetComponent<EventTrigger>();
						contrastET.triggers.Clear();
						contrastET.triggers.Add(Triggers.ScrollDelta(contrast, 0.1f, 10f));

						//Saturation
						var saturation = dialogTmp.Find("saturation").GetComponent<InputField>();
						saturation.onValueChanged.RemoveAllListeners();
						saturation.text = currentKeyframe.eventValues[6].ToString("f2");
						saturation.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							currentKeyframe.eventValues[6] = num;
							eventManager.updateEvents();
						});

						var saturationLeft = saturation.transform.Find("<").GetComponent<Button>();
						var saturationRight = saturation.transform.Find(">").GetComponent<Button>();

						saturationLeft.onClick.RemoveAllListeners();
						saturationLeft.onClick.AddListener(delegate ()
						{
							saturation.text = (currentKeyframe.eventValues[6] - ConfigEntries.EventMoveModify.Value).ToString();
							eventManager.updateEvents();
						});

						saturationRight.onClick.RemoveAllListeners();
						saturationRight.onClick.AddListener(delegate ()
						{
							saturation.text = (currentKeyframe.eventValues[6] + ConfigEntries.EventMoveModify.Value).ToString();
							eventManager.updateEvents();
						});

						if (!dialogTmp.Find("saturation").GetComponent<EventTrigger>())
						{
							dialogTmp.Find("saturation").gameObject.AddComponent<EventTrigger>();
						}

						var saturationET = dialogTmp.Find("saturation").GetComponent<EventTrigger>();
						saturationET.triggers.Clear();
						saturationET.triggers.Add(Triggers.ScrollDelta(saturation, 0.1f, 10f));

						//Temperature
						var temperature = dialogTmp.Find("temperature").GetComponent<InputField>();
						temperature.onValueChanged.RemoveAllListeners();
						temperature.text = currentKeyframe.eventValues[7].ToString("f2");
						temperature.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							currentKeyframe.eventValues[7] = num;
							eventManager.updateEvents();
						});

						var temperatureLeft = temperature.transform.Find("<").GetComponent<Button>();
						var temperatureRight = temperature.transform.Find(">").GetComponent<Button>();

						temperatureLeft.onClick.RemoveAllListeners();
						temperatureLeft.onClick.AddListener(delegate ()
						{
							temperature.text = (currentKeyframe.eventValues[7] - ConfigEntries.EventMoveModify.Value).ToString();
							eventManager.updateEvents();
						});

						temperatureRight.onClick.RemoveAllListeners();
						temperatureRight.onClick.AddListener(delegate ()
						{
							temperature.text = (currentKeyframe.eventValues[7] + ConfigEntries.EventMoveModify.Value).ToString();
							eventManager.updateEvents();
						});

						if (!dialogTmp.Find("temperature").GetComponent<EventTrigger>())
						{
							dialogTmp.Find("temperature").gameObject.AddComponent<EventTrigger>();
						}

						var temperatureET = dialogTmp.Find("temperature").GetComponent<EventTrigger>();
						temperatureET.triggers.Clear();
						temperatureET.triggers.Add(Triggers.ScrollDelta(temperature, 0.1f, 10f));

						//Tint
						var tint = dialogTmp.Find("tint").GetComponent<InputField>();
						tint.onValueChanged.RemoveAllListeners();
						tint.text = currentKeyframe.eventValues[8].ToString("f2");
						tint.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							currentKeyframe.eventValues[8] = num;
							eventManager.updateEvents();
						});

						var tintLeft = tint.transform.Find("<").GetComponent<Button>();
						var tintRight = tint.transform.Find(">").GetComponent<Button>();

						tintLeft.onClick.RemoveAllListeners();
						tintLeft.onClick.AddListener(delegate ()
						{
							tint.text = (currentKeyframe.eventValues[8] - ConfigEntries.EventMoveModify.Value).ToString();
							eventManager.updateEvents();
						});

						tintRight.onClick.RemoveAllListeners();
						tintRight.onClick.AddListener(delegate ()
						{
							tint.text = (currentKeyframe.eventValues[8] + ConfigEntries.EventMoveModify.Value).ToString();
							eventManager.updateEvents();
						});

						if (!dialogTmp.Find("tint").GetComponent<EventTrigger>())
						{
							dialogTmp.Find("tint").gameObject.AddComponent<EventTrigger>();
						}

						var tintET = dialogTmp.Find("tint").GetComponent<EventTrigger>();
						tintET.triggers.Clear();
						tintET.triggers.Add(Triggers.ScrollDelta(tint, 0.1f, 10f));
						break;
					}
				case 11: //Ripples
					{
						//Strength
						var strength = dialogTmp.Find("strength").GetComponent<InputField>();
						strength.onValueChanged.RemoveAllListeners();
						strength.text = currentKeyframe.eventValues[0].ToString("f2");
						strength.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							currentKeyframe.eventValues[0] = num;
							eventManager.updateEvents();
						});

						var strengthLeft = strength.transform.Find("<").GetComponent<Button>();
						var strengthRight = strength.transform.Find(">").GetComponent<Button>();

						strengthLeft.onClick.RemoveAllListeners();
						strengthLeft.onClick.AddListener(delegate ()
						{
							strength.text = (currentKeyframe.eventValues[0] - ConfigEntries.EventMoveModify.Value).ToString();
							eventManager.updateEvents();
						});

						strengthRight.onClick.RemoveAllListeners();
						strengthRight.onClick.AddListener(delegate ()
						{
							strength.text = (currentKeyframe.eventValues[0] + ConfigEntries.EventMoveModify.Value).ToString();
							eventManager.updateEvents();
						});

						if (!dialogTmp.Find("strength").GetComponent<EventTrigger>())
						{
							dialogTmp.Find("strength").gameObject.AddComponent<EventTrigger>();
						}

						var strengthET = dialogTmp.Find("strength").GetComponent<EventTrigger>();
						strengthET.triggers.Clear();
						strengthET.triggers.Add(Triggers.ScrollDelta(strength, 0.1f, 10f));

						//Speed
						var speed = dialogTmp.Find("speed").GetComponent<InputField>();
						speed.onValueChanged.RemoveAllListeners();
						speed.text = currentKeyframe.eventValues[1].ToString("f2");
						speed.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							currentKeyframe.eventValues[1] = num;
							eventManager.updateEvents();
						});

						var speedLeft = speed.transform.Find("<").GetComponent<Button>();
						var speedRight = speed.transform.Find(">").GetComponent<Button>();

						speedLeft.onClick.RemoveAllListeners();
						speedLeft.onClick.AddListener(delegate ()
						{
							speed.text = (currentKeyframe.eventValues[1] - ConfigEntries.EventMoveModify.Value).ToString();
							eventManager.updateEvents();
						});

						speedRight.onClick.RemoveAllListeners();
						speedRight.onClick.AddListener(delegate ()
						{
							speed.text = (currentKeyframe.eventValues[1] + ConfigEntries.EventMoveModify.Value).ToString();
							eventManager.updateEvents();
						});

						if (!dialogTmp.Find("speed").GetComponent<EventTrigger>())
						{
							dialogTmp.Find("speed").gameObject.AddComponent<EventTrigger>();
						}

						var speedET = dialogTmp.Find("speed").GetComponent<EventTrigger>();
						speedET.triggers.Clear();
						speedET.triggers.Add(Triggers.ScrollDelta(speed, 0.1f, 10f));

						//Distance
						var distance = dialogTmp.Find("distance").GetComponent<InputField>();
						distance.onValueChanged.RemoveAllListeners();
						distance.text = currentKeyframe.eventValues[2].ToString("f2");
						distance.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							currentKeyframe.eventValues[2] = num;
							eventManager.updateEvents();
						});

						var distanceLeft = distance.transform.Find("<").GetComponent<Button>();
						var distanceRight = distance.transform.Find(">").GetComponent<Button>();

						distanceLeft.onClick.RemoveAllListeners();
						distanceLeft.onClick.AddListener(delegate ()
						{
							distance.text = (currentKeyframe.eventValues[2] - ConfigEntries.EventMoveModify.Value).ToString();
							eventManager.updateEvents();
						});

						distanceRight.onClick.RemoveAllListeners();
						distanceRight.onClick.AddListener(delegate ()
						{
							distance.text = (currentKeyframe.eventValues[2] + ConfigEntries.EventMoveModify.Value).ToString();
							eventManager.updateEvents();
						});

						if (!dialogTmp.Find("distance").GetComponent<EventTrigger>())
						{
							dialogTmp.Find("distance").gameObject.AddComponent<EventTrigger>();
						}

						var distanceET = dialogTmp.Find("distance").GetComponent<EventTrigger>();
						distanceET.triggers.Clear();
						distanceET.triggers.Add(Triggers.ScrollDelta(distance, 0.1f, 10f));

						//Height
						var height = dialogTmp.Find("height").GetComponent<InputField>();
						height.onValueChanged.RemoveAllListeners();
						height.text = currentKeyframe.eventValues[3].ToString("f2");
						height.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							currentKeyframe.eventValues[3] = num;
							eventManager.updateEvents();
						});

						var heightLeft = height.transform.Find("<").GetComponent<Button>();
						var heightRight = height.transform.Find(">").GetComponent<Button>();

						heightLeft.onClick.RemoveAllListeners();
						heightLeft.onClick.AddListener(delegate ()
						{
							height.text = (currentKeyframe.eventValues[3] - ConfigEntries.EventMoveModify.Value).ToString();
							eventManager.updateEvents();
						});

						heightRight.onClick.RemoveAllListeners();
						heightRight.onClick.AddListener(delegate ()
						{
							height.text = (currentKeyframe.eventValues[3] + ConfigEntries.EventMoveModify.Value).ToString();
							eventManager.updateEvents();
						});

						if (!dialogTmp.Find("height").GetComponent<EventTrigger>())
						{
							dialogTmp.Find("height").gameObject.AddComponent<EventTrigger>();
						}

						var heightET = dialogTmp.Find("height").GetComponent<EventTrigger>();
						heightET.triggers.Clear();
						heightET.triggers.Add(Triggers.ScrollDelta(height, 0.1f, 10f));

						//Width
						var width = dialogTmp.Find("width").GetComponent<InputField>();
						width.onValueChanged.RemoveAllListeners();
						width.text = currentKeyframe.eventValues[4].ToString("f2");
						width.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							currentKeyframe.eventValues[4] = num;
							eventManager.updateEvents();
						});

						var widthLeft = width.transform.Find("<").GetComponent<Button>();
						var widthRight = width.transform.Find(">").GetComponent<Button>();

						widthLeft.onClick.RemoveAllListeners();
						widthLeft.onClick.AddListener(delegate ()
						{
							width.text = (currentKeyframe.eventValues[4] - ConfigEntries.EventMoveModify.Value).ToString();
							eventManager.updateEvents();
						});

						widthRight.onClick.RemoveAllListeners();
						widthRight.onClick.AddListener(delegate ()
						{
							width.text = (currentKeyframe.eventValues[4] + ConfigEntries.EventMoveModify.Value).ToString();
							eventManager.updateEvents();
						});

						if (!dialogTmp.Find("width").GetComponent<EventTrigger>())
						{
							dialogTmp.Find("width").gameObject.AddComponent<EventTrigger>();
						}

						var widthET = dialogTmp.Find("width").GetComponent<EventTrigger>();
						widthET.triggers.Clear();
						widthET.triggers.Add(Triggers.ScrollDelta(width, 0.1f, 10f));
						break;
					}
				case 12: //RadialBlur
					{
						//RadialBlur
						var intensity = dialogTmp.Find("intensity").GetComponent<InputField>();
						intensity.onValueChanged.RemoveAllListeners();
						intensity.text = currentKeyframe.eventValues[0].ToString("f2");
						intensity.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							currentKeyframe.eventValues[0] = num;
							eventManager.updateEvents();
						});

						var intensityLeft = intensity.transform.Find("<").GetComponent<Button>();
						var intensityRight = intensity.transform.Find(">").GetComponent<Button>();

						intensityLeft.onClick.RemoveAllListeners();
						intensityLeft.onClick.AddListener(delegate ()
						{
							intensity.text = (currentKeyframe.eventValues[0] - ConfigEntries.EventMoveModify.Value).ToString();
							eventManager.updateEvents();
						});

						intensityRight.onClick.RemoveAllListeners();
						intensityRight.onClick.AddListener(delegate ()
						{
							intensity.text = (currentKeyframe.eventValues[0] + ConfigEntries.EventMoveModify.Value).ToString();
							eventManager.updateEvents();
						});

						if (!dialogTmp.Find("intensity").GetComponent<EventTrigger>())
						{
							dialogTmp.Find("intensity").gameObject.AddComponent<EventTrigger>();
						}

						var intensityET = dialogTmp.Find("intensity").GetComponent<EventTrigger>();
						intensityET.triggers.Clear();
						intensityET.triggers.Add(Triggers.ScrollDelta(intensity, 0.1f, 10f));

						//RadialBlur
						var iterations = dialogTmp.Find("iterations").GetComponent<InputField>();
						iterations.onValueChanged.RemoveAllListeners();
						iterations.text = currentKeyframe.eventValues[1].ToString("f2");
						iterations.onValueChanged.AddListener(delegate (string val)
						{
							int num = int.Parse(val);
							num = Mathf.Clamp(num, 1, 16);
							currentKeyframe.eventValues[1] = num;
							eventManager.updateEvents();
						});

						var iterationsLeft = iterations.transform.Find("<").GetComponent<Button>();
						var iterationsRight = iterations.transform.Find(">").GetComponent<Button>();

						iterationsLeft.onClick.RemoveAllListeners();
						iterationsLeft.onClick.AddListener(delegate ()
						{
							iterations.text = Mathf.Clamp(currentKeyframe.eventValues[1] - (int)ConfigEntries.EventMoveModify.Value, 1, 16).ToString();
							eventManager.updateEvents();
						});

						iterationsRight.onClick.RemoveAllListeners();
						iterationsRight.onClick.AddListener(delegate ()
						{
							iterations.text = Mathf.Clamp(currentKeyframe.eventValues[1] + (int)ConfigEntries.EventMoveModify.Value, 1, 16).ToString();
							eventManager.updateEvents();
						});

						if (!dialogTmp.Find("iterations").GetComponent<EventTrigger>())
						{
							dialogTmp.Find("iterations").gameObject.AddComponent<EventTrigger>();
						}

						var iterationsET = dialogTmp.Find("iterations").GetComponent<EventTrigger>();
						iterationsET.triggers.Clear();
						iterationsET.triggers.Add(Triggers.ScrollDelta(iterations, 1f, 10f));
						break;
					}
				case 13: //ColorSplit
					{
						//ColorSplit
						var intensity = dialogTmp.Find("offset").GetComponent<InputField>();
						intensity.onValueChanged.RemoveAllListeners();
						intensity.text = currentKeyframe.eventValues[0].ToString("f2");
						intensity.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							currentKeyframe.eventValues[0] = num;
							eventManager.updateEvents();
						});

						var intensityLeft = intensity.transform.Find("<").GetComponent<Button>();
						var intensityRight = intensity.transform.Find(">").GetComponent<Button>();

						intensityLeft.onClick.RemoveAllListeners();
						intensityLeft.onClick.AddListener(delegate ()
						{
							intensity.text = (currentKeyframe.eventValues[0] - ConfigEntries.EventMoveModify.Value).ToString();
							eventManager.updateEvents();
						});

						intensityRight.onClick.RemoveAllListeners();
						intensityRight.onClick.AddListener(delegate ()
						{
							intensity.text = (currentKeyframe.eventValues[0] + ConfigEntries.EventMoveModify.Value).ToString();
							eventManager.updateEvents();
						});

						if (!dialogTmp.Find("offset").GetComponent<EventTrigger>())
						{
							dialogTmp.Find("offset").gameObject.AddComponent<EventTrigger>();
						}

						var intensityET = dialogTmp.Find("offset").GetComponent<EventTrigger>();
						intensityET.triggers.Clear();
						intensityET.triggers.Add(Triggers.ScrollDelta(intensity, 0.1f, 10f));
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
							float num = float.Parse(val);
							currentKeyframe.eventValues[0] = num;
							eventManager.updateEvents();
						});

						posY.onValueChanged.RemoveAllListeners();
						posY.text = currentKeyframe.eventValues[1].ToString("f2");
						posY.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							currentKeyframe.eventValues[1] = num;
							eventManager.updateEvents();
						});

						var posXLeft = posX.transform.Find("<").GetComponent<Button>();
						var posXRight = posX.transform.Find(">").GetComponent<Button>();
						var posYLeft = posY.transform.Find("<").GetComponent<Button>();
						var posYRight = posY.transform.Find(">").GetComponent<Button>();

						posXLeft.onClick.RemoveAllListeners();
						posXLeft.onClick.AddListener(delegate ()
						{
							posX.text = (currentKeyframe.eventValues[0] - ConfigEntries.EventMoveModify.Value).ToString();
						});

						posXRight.onClick.RemoveAllListeners();
						posXRight.onClick.AddListener(delegate ()
						{
							posX.text = (currentKeyframe.eventValues[0] + ConfigEntries.EventMoveModify.Value).ToString();
						});

						posYLeft.onClick.RemoveAllListeners();
						posYLeft.onClick.AddListener(delegate ()
						{
							posY.text = (currentKeyframe.eventValues[1] - ConfigEntries.EventMoveModify.Value).ToString();
						});

						posYRight.onClick.RemoveAllListeners();
						posYRight.onClick.AddListener(delegate ()
						{
							posY.text = (currentKeyframe.eventValues[1] + ConfigEntries.EventMoveModify.Value).ToString();
						});

						if (!dialogTmp.Find("position/x").GetComponent<EventTrigger>())
						{
							dialogTmp.Find("position/x").gameObject.AddComponent<EventTrigger>();
						}

						if (!dialogTmp.Find("position/y").GetComponent<EventTrigger>())
						{
							dialogTmp.Find("position/y").gameObject.AddComponent<EventTrigger>();
						}

						var posXET = dialogTmp.Find("position/x").GetComponent<EventTrigger>();
						var posYET = dialogTmp.Find("position/y").GetComponent<EventTrigger>();

						posXET.triggers.Clear();
						posXET.triggers.Add(Triggers.ScrollDelta(posX, 0.1f, 10f, true));
						posXET.triggers.Add(Triggers.ScrollDeltaVector2(posX, posY, 0.1f, 10f));

						posYET.triggers.Clear();
						posYET.triggers.Add(Triggers.ScrollDelta(posY, 0.1f, 10f, true));
						posYET.triggers.Add(Triggers.ScrollDeltaVector2(posX, posY, 0.1f, 10f));
						break;
                    }
				case 15: //Gradient
					{
						//Gradient Intensity
						{
							var bloom = dialogTmp.Find("bloom/x").GetComponent<InputField>();
							bloom.onValueChanged.RemoveAllListeners();
							bloom.text = currentKeyframe.eventValues[0].ToString("f2");
							bloom.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								currentKeyframe.eventValues[0] = num;
								eventManager.updateEvents();
							});

							var bloomLeft = bloom.transform.Find("<").GetComponent<Button>();
							var bloomRight = bloom.transform.Find(">").GetComponent<Button>();

							bloomLeft.onClick.RemoveAllListeners();
							bloomLeft.onClick.AddListener(delegate ()
							{
								bloom.text = (currentKeyframe.eventValues[0] - ConfigEntries.EventBloomModify.Value).ToString();
								eventManager.updateEvents();
							});

							bloomRight.onClick.RemoveAllListeners();
							bloomRight.onClick.AddListener(delegate ()
							{
								bloom.text = (currentKeyframe.eventValues[0] + ConfigEntries.EventBloomModify.Value).ToString();
								eventManager.updateEvents();
							});

							if (!dialogTmp.Find("bloom/x").GetComponent<EventTrigger>())
							{
								dialogTmp.Find("bloom/x").gameObject.AddComponent<EventTrigger>();
							}

							var bloomET = dialogTmp.Find("bloom/x").GetComponent<EventTrigger>();

							bloomET.triggers.Clear();
							bloomET.triggers.Add(Triggers.ScrollDelta(bloom, 0.1f, 10f));
						}

						//Bloom Diffusion
						{
							var bloom = dialogTmp.Find("diffusion").GetComponent<InputField>();
							bloom.onValueChanged.RemoveAllListeners();
							bloom.text = currentKeyframe.eventValues[1].ToString("f2");
							bloom.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								currentKeyframe.eventValues[1] = num;
								eventManager.updateEvents();
							});

							var bloomLeft = bloom.transform.Find("<").GetComponent<Button>();
							var bloomRight = bloom.transform.Find(">").GetComponent<Button>();

							bloomLeft.onClick.RemoveAllListeners();
							bloomLeft.onClick.AddListener(delegate ()
							{
								bloom.text = (currentKeyframe.eventValues[1] - ConfigEntries.EventBloomModify.Value).ToString();
								eventManager.updateEvents();
							});

							bloomRight.onClick.RemoveAllListeners();
							bloomRight.onClick.AddListener(delegate ()
							{
								bloom.text = (currentKeyframe.eventValues[1] + ConfigEntries.EventBloomModify.Value).ToString();
								eventManager.updateEvents();
							});

							if (!dialogTmp.Find("diffusion").GetComponent<EventTrigger>())
							{
								dialogTmp.Find("diffusion").gameObject.AddComponent<EventTrigger>();
							}

							var bloomET = dialogTmp.Find("diffusion").GetComponent<EventTrigger>();

							bloomET.triggers.Clear();
							bloomET.triggers.Add(Triggers.ScrollDelta(bloom, 0.1f, 10f));
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
						//ColorSplit
						var intensity = dialogTmp.Find("offset").GetComponent<InputField>();
						intensity.onValueChanged.RemoveAllListeners();
						intensity.text = currentKeyframe.eventValues[0].ToString("f2");
						intensity.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							currentKeyframe.eventValues[0] = num;
							eventManager.updateEvents();
						});

						var intensityLeft = intensity.transform.Find("<").GetComponent<Button>();
						var intensityRight = intensity.transform.Find(">").GetComponent<Button>();

						intensityLeft.onClick.RemoveAllListeners();
						intensityLeft.onClick.AddListener(delegate ()
						{
							intensity.text = (currentKeyframe.eventValues[0] - ConfigEntries.EventMoveModify.Value).ToString();
							eventManager.updateEvents();
						});

						intensityRight.onClick.RemoveAllListeners();
						intensityRight.onClick.AddListener(delegate ()
						{
							intensity.text = (currentKeyframe.eventValues[0] + ConfigEntries.EventMoveModify.Value).ToString();
							eventManager.updateEvents();
						});

						if (!dialogTmp.Find("offset").GetComponent<EventTrigger>())
						{
							dialogTmp.Find("offset").gameObject.AddComponent<EventTrigger>();
						}

						var intensityET = dialogTmp.Find("offset").GetComponent<EventTrigger>();
						intensityET.triggers.Clear();
						intensityET.triggers.Add(Triggers.ScrollDelta(intensity, 0.1f, 10f));
						break;
					}
				case 17: //ScanLines
                    {
						//intensity
						var intensity = dialogTmp.Find("intensity").GetComponent<InputField>();
						intensity.onValueChanged.RemoveAllListeners();
						intensity.text = currentKeyframe.eventValues[0].ToString("f2");
						intensity.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							currentKeyframe.eventValues[0] = num;
							eventManager.updateEvents();
						});

						var intensityLeft = intensity.transform.Find("<").GetComponent<Button>();
						var intensityRight = intensity.transform.Find(">").GetComponent<Button>();

						intensityLeft.onClick.RemoveAllListeners();
						intensityLeft.onClick.AddListener(delegate ()
						{
							intensity.text = (currentKeyframe.eventValues[0] - ConfigEntries.EventMoveModify.Value).ToString();
							eventManager.updateEvents();
						});

						intensityRight.onClick.RemoveAllListeners();
						intensityRight.onClick.AddListener(delegate ()
						{
							intensity.text = (currentKeyframe.eventValues[0] + ConfigEntries.EventMoveModify.Value).ToString();
							eventManager.updateEvents();
						});

						if (!dialogTmp.Find("intensity").GetComponent<EventTrigger>())
						{
							dialogTmp.Find("intensity").gameObject.AddComponent<EventTrigger>();
						}

						var intensityET = dialogTmp.Find("intensity").GetComponent<EventTrigger>();
						intensityET.triggers.Clear();
						intensityET.triggers.Add(Triggers.ScrollDelta(intensity, 0.1f, 10f));

						//Amount
						var iterations = dialogTmp.Find("iterations").GetComponent<InputField>();
						iterations.onValueChanged.RemoveAllListeners();
						iterations.text = currentKeyframe.eventValues[1].ToString("f2");
						iterations.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							currentKeyframe.eventValues[1] = num;
							eventManager.updateEvents();
						});

						var iterationsLeft = iterations.transform.Find("<").GetComponent<Button>();
						var iterationsRight = iterations.transform.Find(">").GetComponent<Button>();

						iterationsLeft.onClick.RemoveAllListeners();
						iterationsLeft.onClick.AddListener(delegate ()
						{
							iterations.text = Mathf.Clamp(currentKeyframe.eventValues[1] - ConfigEntries.EventMoveModify.Value, 1, 16).ToString();
							eventManager.updateEvents();
						});

						iterationsRight.onClick.RemoveAllListeners();
						iterationsRight.onClick.AddListener(delegate ()
						{
							iterations.text = Mathf.Clamp(currentKeyframe.eventValues[1] + ConfigEntries.EventMoveModify.Value, 1, 16).ToString();
							eventManager.updateEvents();
						});

						if (!dialogTmp.Find("iterations").GetComponent<EventTrigger>())
						{
							dialogTmp.Find("iterations").gameObject.AddComponent<EventTrigger>();
						}

						var iterationsET = dialogTmp.Find("iterations").GetComponent<EventTrigger>();
						iterationsET.triggers.Clear();
						iterationsET.triggers.Add(Triggers.ScrollDelta(iterations, 1f, 10f));

						//Speed
						var speed = dialogTmp.Find("speed").GetComponent<InputField>();
						speed.onValueChanged.RemoveAllListeners();
						speed.text = currentKeyframe.eventValues[2].ToString("f2");
						speed.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							currentKeyframe.eventValues[2] = num;
							eventManager.updateEvents();
						});

						var speedLeft = speed.transform.Find("<").GetComponent<Button>();
						var speedRight = speed.transform.Find(">").GetComponent<Button>();

						speedLeft.onClick.RemoveAllListeners();
						speedLeft.onClick.AddListener(delegate ()
						{
							speed.text = Mathf.Clamp(currentKeyframe.eventValues[2] - ConfigEntries.EventMoveModify.Value, 1, 16).ToString();
							eventManager.updateEvents();
						});

						speedRight.onClick.RemoveAllListeners();
						speedRight.onClick.AddListener(delegate ()
						{
							speed.text = Mathf.Clamp(currentKeyframe.eventValues[2] + ConfigEntries.EventMoveModify.Value, 1, 16).ToString();
							eventManager.updateEvents();
						});

						if (!dialogTmp.Find("speed").GetComponent<EventTrigger>())
						{
							dialogTmp.Find("speed").gameObject.AddComponent<EventTrigger>();
						}

						var speedET = dialogTmp.Find("speed").GetComponent<EventTrigger>();
						speedET.triggers.Clear();
						speedET.triggers.Add(Triggers.ScrollDelta(speed, 1f, 10f));
						break;
					}
				case 18: //Blur
					{
						//Blur Amount
						var intensity = dialogTmp.Find("intensity").GetComponent<InputField>();
						intensity.onValueChanged.RemoveAllListeners();
						intensity.text = currentKeyframe.eventValues[0].ToString("f2");
						intensity.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							currentKeyframe.eventValues[0] = num;
							eventManager.updateEvents();
						});

						var intensityLeft = intensity.transform.Find("<").GetComponent<Button>();
						var intensityRight = intensity.transform.Find(">").GetComponent<Button>();

						intensityLeft.onClick.RemoveAllListeners();
						intensityLeft.onClick.AddListener(delegate ()
						{
							intensity.text = (currentKeyframe.eventValues[0] - ConfigEntries.EventMoveModify.Value).ToString();
							eventManager.updateEvents();
						});

						intensityRight.onClick.RemoveAllListeners();
						intensityRight.onClick.AddListener(delegate ()
						{
							intensity.text = (currentKeyframe.eventValues[0] + ConfigEntries.EventMoveModify.Value).ToString();
							eventManager.updateEvents();
						});

						if (!dialogTmp.Find("intensity").GetComponent<EventTrigger>())
						{
							dialogTmp.Find("intensity").gameObject.AddComponent<EventTrigger>();
						}

						var intensityET = dialogTmp.Find("intensity").GetComponent<EventTrigger>();
						intensityET.triggers.Clear();
						intensityET.triggers.Add(Triggers.ScrollDelta(intensity, 0.1f, 10f));

						//Blur Iterations
						var iterations = dialogTmp.Find("iterations").GetComponent<InputField>();
						iterations.onValueChanged.RemoveAllListeners();
						iterations.text = Mathf.Clamp(currentKeyframe.eventValues[1], 1, 12).ToString("f1");
						iterations.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							num = Mathf.Clamp(num, 1, 12);
							currentKeyframe.eventValues[1] = Mathf.RoundToInt(num);
							eventManager.updateEvents();
						});

						var iterationsLeft = iterations.transform.Find("<").GetComponent<Button>();
						var iterationsRight = iterations.transform.Find(">").GetComponent<Button>();

						iterationsLeft.onClick.RemoveAllListeners();
						iterationsLeft.onClick.AddListener(delegate ()
						{
							iterations.text = Mathf.Clamp(currentKeyframe.eventValues[1] - ConfigEntries.EventMoveModify.Value, 1, 12).ToString();
							eventManager.updateEvents();
						});

						iterationsRight.onClick.RemoveAllListeners();
						iterationsRight.onClick.AddListener(delegate ()
						{
							iterations.text = Mathf.Clamp(currentKeyframe.eventValues[1] + ConfigEntries.EventMoveModify.Value, 1, 12).ToString();
							eventManager.updateEvents();
						});

						if (!dialogTmp.Find("iterations").GetComponent<EventTrigger>())
						{
							dialogTmp.Find("iterations").gameObject.AddComponent<EventTrigger>();
						}

						var iterationsET = dialogTmp.Find("iterations").GetComponent<EventTrigger>();
						iterationsET.triggers.Clear();
						iterationsET.triggers.Add(Triggers.ScrollDelta(iterations, 1f, 1f, false, new List<float> { 1f, 12f }));
						break;
                    }
				case 19: //Pixelize
                    {
						//Pixelize
						var intensity = dialogTmp.Find("offset").GetComponent<InputField>();
						intensity.onValueChanged.RemoveAllListeners();
						intensity.text = currentKeyframe.eventValues[0].ToString("f2");
						intensity.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							num = Mathf.Clamp(num, 0f, 0.99999f);
							currentKeyframe.eventValues[0] = num;
							eventManager.updateEvents();
						});

						var intensityLeft = intensity.transform.Find("<").GetComponent<Button>();
						var intensityRight = intensity.transform.Find(">").GetComponent<Button>();

						intensityLeft.onClick.RemoveAllListeners();
						intensityLeft.onClick.AddListener(delegate ()
						{
							intensity.text = Mathf.Clamp(currentKeyframe.eventValues[0] - ConfigEntries.EventMoveModify.Value, 0f, 0.99999f).ToString();
							eventManager.updateEvents();
						});

						intensityRight.onClick.RemoveAllListeners();
						intensityRight.onClick.AddListener(delegate ()
						{
							intensity.text = Mathf.Clamp(currentKeyframe.eventValues[0] + ConfigEntries.EventMoveModify.Value, 0f, 0.99999f).ToString();
							eventManager.updateEvents();
						});

						if (!dialogTmp.Find("offset").GetComponent<EventTrigger>())
						{
							dialogTmp.Find("offset").gameObject.AddComponent<EventTrigger>();
						}

						var intensityET = dialogTmp.Find("offset").GetComponent<EventTrigger>();
						intensityET.triggers.Clear();
						intensityET.triggers.Add(Triggers.ScrollDelta(intensity, 0.1f, 10f, false, new List<float> { 0f, 0.99999f }));
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
				case 21: //Overlay
					{
						int num = 0;
						foreach (Toggle toggle in overlayColorButtons)
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
								SetOverlayColor(tmpIndex);
							});
							num++;
						}

						var intensity = dialogTmp.Find("alpha").GetComponent<InputField>();
						intensity.onValueChanged.RemoveAllListeners();
						intensity.text = currentKeyframe.eventValues[1].ToString("f2");
						intensity.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							num = Mathf.Clamp(num, 0f, 1f);
							currentKeyframe.eventValues[1] = num;
							eventManager.updateEvents();
						});

						var intensityLeft = intensity.transform.Find("<").GetComponent<Button>();
						var intensityRight = intensity.transform.Find(">").GetComponent<Button>();

						intensityLeft.onClick.RemoveAllListeners();
						intensityLeft.onClick.AddListener(delegate ()
						{
							intensity.text = Mathf.Clamp(currentKeyframe.eventValues[1] - ConfigEntries.EventMoveModify.Value, 0f, 1f).ToString();
							eventManager.updateEvents();
						});

						intensityRight.onClick.RemoveAllListeners();
						intensityRight.onClick.AddListener(delegate ()
						{
							intensity.text = Mathf.Clamp(currentKeyframe.eventValues[1] + ConfigEntries.EventMoveModify.Value, 0f, 1f).ToString();
							eventManager.updateEvents();
						});

						if (!dialogTmp.Find("alpha").GetComponent<EventTrigger>())
						{
							dialogTmp.Find("alpha").gameObject.AddComponent<EventTrigger>();
						}

						var intensityET = dialogTmp.Find("alpha").GetComponent<EventTrigger>();
						intensityET.triggers.Clear();
						intensityET.triggers.Add(Triggers.ScrollDelta(intensity, 0.1f, 10f, false, new List<float> { 0f, 1f }));
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
								float num = float.Parse(val);
								currentKeyframe.eventValues[1] = num;
								eventManager.updateEvents();
							});

							posY.onValueChanged.RemoveAllListeners();
							posY.text = currentKeyframe.eventValues[2].ToString("f2");
							posY.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								currentKeyframe.eventValues[2] = num;
								eventManager.updateEvents();
							});

							var posXLeft = posX.transform.Find("<").GetComponent<Button>();
							var posXRight = posX.transform.Find(">").GetComponent<Button>();
							var posYLeft = posY.transform.Find("<").GetComponent<Button>();
							var posYRight = posY.transform.Find(">").GetComponent<Button>();

							posXLeft.onClick.RemoveAllListeners();
							posXLeft.onClick.AddListener(delegate ()
							{
								posX.text = (currentKeyframe.eventValues[1] - ConfigEntries.EventMoveModify.Value).ToString();
							});

							posXRight.onClick.RemoveAllListeners();
							posXRight.onClick.AddListener(delegate ()
							{
								posX.text = (currentKeyframe.eventValues[1] + ConfigEntries.EventMoveModify.Value).ToString();
							});

							posYLeft.onClick.RemoveAllListeners();
							posYLeft.onClick.AddListener(delegate ()
							{
								posY.text = (currentKeyframe.eventValues[2] - ConfigEntries.EventMoveModify.Value).ToString();
							});

							posYRight.onClick.RemoveAllListeners();
							posYRight.onClick.AddListener(delegate ()
							{
								posY.text = (currentKeyframe.eventValues[2] + ConfigEntries.EventMoveModify.Value).ToString();
							});

							if (!dialogTmp.Find("position/x").GetComponent<EventTrigger>())
							{
								dialogTmp.Find("position/x").gameObject.AddComponent<EventTrigger>();
							}

							if (!dialogTmp.Find("position/y").GetComponent<EventTrigger>())
							{
								dialogTmp.Find("position/y").gameObject.AddComponent<EventTrigger>();
							}

							var posXET = dialogTmp.Find("position/x").GetComponent<EventTrigger>();
							var posYET = dialogTmp.Find("position/y").GetComponent<EventTrigger>();

							posXET.triggers.Clear();
							posXET.triggers.Add(Triggers.ScrollDelta(posX, 0.1f, 10f, true));
							posXET.triggers.Add(Triggers.ScrollDeltaVector2(posX, posY, 0.1f, 10f));

							posYET.triggers.Clear();
							posYET.triggers.Add(Triggers.ScrollDelta(posY, 0.1f, 10f, true));
							posYET.triggers.Add(Triggers.ScrollDeltaVector2(posX, posY, 0.1f, 10f));
						}

						//Scale
                        {
							var posX = dialogTmp.Find("scale/x").GetComponent<InputField>();
							var posY = dialogTmp.Find("scale/y").GetComponent<InputField>();

							posX.onValueChanged.RemoveAllListeners();
							posX.text = currentKeyframe.eventValues[3].ToString("f2");
							posX.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								currentKeyframe.eventValues[3] = num;
								eventManager.updateEvents();
							});

							posY.onValueChanged.RemoveAllListeners();
							posY.text = currentKeyframe.eventValues[4].ToString("f2");
							posY.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								currentKeyframe.eventValues[4] = num;
								eventManager.updateEvents();
							});

							var posXLeft = posX.transform.Find("<").GetComponent<Button>();
							var posXRight = posX.transform.Find(">").GetComponent<Button>();
							var posYLeft = posY.transform.Find("<").GetComponent<Button>();
							var posYRight = posY.transform.Find(">").GetComponent<Button>();

							posXLeft.onClick.RemoveAllListeners();
							posXLeft.onClick.AddListener(delegate ()
							{
								posX.text = (currentKeyframe.eventValues[3] - ConfigEntries.EventMoveModify.Value).ToString();
							});

							posXRight.onClick.RemoveAllListeners();
							posXRight.onClick.AddListener(delegate ()
							{
								posX.text = (currentKeyframe.eventValues[3] + ConfigEntries.EventMoveModify.Value).ToString();
							});

							posYLeft.onClick.RemoveAllListeners();
							posYLeft.onClick.AddListener(delegate ()
							{
								posY.text = (currentKeyframe.eventValues[4] - ConfigEntries.EventMoveModify.Value).ToString();
							});

							posYRight.onClick.RemoveAllListeners();
							posYRight.onClick.AddListener(delegate ()
							{
								posY.text = (currentKeyframe.eventValues[4] + ConfigEntries.EventMoveModify.Value).ToString();
							});

							if (!dialogTmp.Find("scale/x").GetComponent<EventTrigger>())
							{
								dialogTmp.Find("scale/x").gameObject.AddComponent<EventTrigger>();
							}

							if (!dialogTmp.Find("scale/y").GetComponent<EventTrigger>())
							{
								dialogTmp.Find("scale/y").gameObject.AddComponent<EventTrigger>();
							}

							var posXET = dialogTmp.Find("scale/x").GetComponent<EventTrigger>();
							var posYET = dialogTmp.Find("scale/y").GetComponent<EventTrigger>();

							posXET.triggers.Clear();
							posXET.triggers.Add(Triggers.ScrollDelta(posX, 0.1f, 10f, true));
							posXET.triggers.Add(Triggers.ScrollDeltaVector2(posX, posY, 0.1f, 10f));

							posYET.triggers.Clear();
							posYET.triggers.Add(Triggers.ScrollDelta(posY, 0.1f, 10f, true));
							posYET.triggers.Add(Triggers.ScrollDeltaVector2(posX, posY, 0.1f, 10f));
						}

                        //Rotation
                        {
							var intensity = dialogTmp.Find("rotation").GetComponent<InputField>();
							intensity.onValueChanged.RemoveAllListeners();
							intensity.text = currentKeyframe.eventValues[5].ToString("f2");
							intensity.onValueChanged.AddListener(delegate (string val)
							{
								float num = float.Parse(val);
								currentKeyframe.eventValues[5] = num;
								eventManager.updateEvents();
							});

							var intensityLeft = intensity.transform.Find("<").GetComponent<Button>();
							var intensityRight = intensity.transform.Find(">").GetComponent<Button>();

							intensityLeft.onClick.RemoveAllListeners();
							intensityLeft.onClick.AddListener(delegate ()
							{
								intensity.text = (currentKeyframe.eventValues[5] - ConfigEntries.EventRotateModify.Value).ToString();
								eventManager.updateEvents();
							});

							intensityRight.onClick.RemoveAllListeners();
							intensityRight.onClick.AddListener(delegate ()
							{
								intensity.text = (currentKeyframe.eventValues[5] + ConfigEntries.EventRotateModify.Value).ToString();
								eventManager.updateEvents();
							});

							if (!dialogTmp.Find("rotation").GetComponent<EventTrigger>())
							{
								dialogTmp.Find("rotation").gameObject.AddComponent<EventTrigger>();
							}

							var intensityET = dialogTmp.Find("rotation").GetComponent<EventTrigger>();
							intensityET.triggers.Clear();
							intensityET.triggers.Add(Triggers.ScrollDelta(intensity, 15f, 3f));
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

		//[HarmonyPatch("RenderThemeEditor")]
		//[HarmonyPostfix]
		private static void RenderThemeList()
		{
			Transform transform = EventEditor.inst.dialogLeft.Find("theme");
			Transform transform2 = transform.Find("actions");
			transform2.Find("update").GetComponent<Button>().onClick.RemoveAllListeners();
			transform2.Find("update").GetComponent<Button>().onClick.AddListener(delegate ()
			{
				List<FileManager.LSFile> fileList = FileManager.inst.GetFileList(EditorPlugin.themeListPath, "lst");
				fileList = (from x in fileList
							orderby x.Name.ToLower()
							select x).ToList();

				foreach (FileManager.LSFile lsfile in fileList)
				{
					if (int.Parse(DataManager.BeatmapTheme.Parse(JSON.Parse(FileManager.inst.LoadJSONFileRaw(lsfile.FullPath)), false).id) == (int.Parse(EventEditor.inst.previewTheme.id)))
					{
						FileManager.inst.DeleteFileRaw(lsfile.FullPath);
					}
				}

				ThemeEditor.inst.SaveTheme(DataManager.BeatmapTheme.DeepCopy(EventEditor.inst.previewTheme, true));
				EventEditor.inst.StartCoroutine(ThemeEditor.inst.LoadThemes());
				Transform child = EventEditor.inst.dialogRight.GetChild(EventEditor.inst.currentEventType);
				EventEditor.inst.RenderThemeContent(child, child.Find("theme-search").GetComponent<InputField>().text);
				EventEditor.inst.RenderThemePreview(child);
				EventEditor.inst.showTheme = false;
				EventEditor.inst.dialogLeft.Find("theme").gameObject.SetActive(false);
			});
		}

		[HarmonyPatch("RenderThemeContent")]
		[HarmonyPrefix]
		public static bool RenderThemeContentPatch(Transform __0, string __1)
		{
			Debug.LogFormat("{0}RenderThemeContent Prefix Patch", EditorPlugin.className);
			Transform parent = __0.Find("themes/viewport/content");

			__0.Find("themes").GetComponent<ScrollRect>().horizontal = ConfigEntries.ListHorizontal.Value;

			if (!parent.GetComponent<GridLayoutGroup>())
			{
				parent.gameObject.AddComponent<GridLayoutGroup>();
			}

			var prefabLay = parent.GetComponent<GridLayoutGroup>();
			prefabLay.cellSize = ConfigEntries.ListCellSize.Value;
			prefabLay.constraint = ConfigEntries.ListConstraint.Value;
			prefabLay.constraintCount = ConfigEntries.ListConstraintCount.Value;
			prefabLay.spacing = ConfigEntries.ListSpacing.Value;
			prefabLay.startAxis = ConfigEntries.ListAxis.Value;

			parent.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.MinSize;

			RTEditor.inst.StartCoroutine(RTEditor.RenderThemeList(__0, __1));

			return false;
		}

		[HarmonyPatch("RenderThemeEditor")]
		[HarmonyPrefix]
		public static bool RenderThemeEditor(int __0)
		{
			Debug.Log("ID: " + __0);
			if (__0 != -1)
			{
				EventEditor.inst.previewTheme = DataManager.BeatmapTheme.DeepCopy(DataManager.inst.GetTheme(__0), true);
			}
			else
			{
				EventEditor.inst.previewTheme = new DataManager.BeatmapTheme();
				EventEditor.inst.previewTheme.ClearBeatmap();
			}

			Transform theme = EventEditor.inst.dialogLeft.Find("theme");
			theme.gameObject.SetActive(true);
			EventEditor.inst.showTheme = true;
			Transform themeContent = theme.Find("theme/viewport/content");
			Transform actions = theme.Find("actions");
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
			name.text = EventEditor.inst.previewTheme.name;
			name.onValueChanged.AddListener(delegate (string val)
			{
				EventEditor.inst.previewTheme.name = val;
			});
			cancel.onClick.RemoveAllListeners();
			cancel.onClick.AddListener(delegate ()
			{
				EventEditor.inst.showTheme = false;
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
				EventEditor.inst.previewTheme.id = null;
				ThemeEditor.inst.SaveTheme(DataManager.BeatmapTheme.DeepCopy(EventEditor.inst.previewTheme, false));
				EventEditor.inst.StartCoroutine(ThemeEditor.inst.LoadThemes());
				Transform child = EventEditor.inst.dialogRight.GetChild(EventEditor.inst.currentEventType);
				EventEditor.inst.RenderThemeContent(child, child.Find("theme-search").GetComponent<InputField>().text);
				EventEditor.inst.RenderThemePreview(child);
				EventEditor.inst.showTheme = false;
				theme.gameObject.SetActive(false);
			});
			update.onClick.AddListener(delegate ()
			{
				List<FileManager.LSFile> fileList = FileManager.inst.GetFileList("beatmaps/themes", "lst");
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
				ThemeEditor.inst.SaveTheme(DataManager.BeatmapTheme.DeepCopy(EventEditor.inst.previewTheme, true));
				EventEditor.inst.StartCoroutine(ThemeEditor.inst.LoadThemes());
				Transform child = EventEditor.inst.dialogRight.GetChild(EventEditor.inst.currentEventType);
				EventEditor.inst.RenderThemeContent(child, child.Find("theme-search").GetComponent<InputField>().text);
				EventEditor.inst.RenderThemePreview(child);
				EventEditor.inst.showTheme = false;
				theme.gameObject.SetActive(false);
			});

			var bgHex = themeContent.Find("bg/hex").GetComponent<InputField>();
			var bgPreview = themeContent.Find("bg/preview").GetComponent<Image>();
			var bgPreviewET = themeContent.Find("bg/preview").GetComponent<EventTrigger>();

			bgHex.onValueChanged.RemoveAllListeners();
			bgHex.text = LSColors.ColorToHex(EventEditor.inst.previewTheme.backgroundColor);
			bgPreview.color = EventEditor.inst.previewTheme.backgroundColor;
			bgHex.onValueChanged.AddListener(delegate (string val)
			{
				bgPreview.color = LSColors.HexToColor(val);
				EventEditor.inst.previewTheme.backgroundColor = LSColors.HexToColor(val);
				bgPreviewET.triggers.Clear();
				bgPreviewET.triggers.Add(CreatePreviewClickTrigger(bgPreview.transform, bgHex.transform, EventEditor.inst.previewTheme.backgroundColor));
			});

			bgPreviewET.triggers.Clear();
			bgPreviewET.triggers.Add(CreatePreviewClickTrigger(bgPreview.transform, bgHex.transform, EventEditor.inst.previewTheme.backgroundColor));

			var guiHex = themeContent.Find("gui/hex").GetComponent<InputField>();
			var guiPreview = themeContent.Find("gui/preview").GetComponent<Image>();
			var guiPreviewET = themeContent.Find("gui/preview").GetComponent<EventTrigger>();

			guiHex.onValueChanged.RemoveAllListeners();
			guiHex.characterLimit = 8;
			guiHex.text = RTEditor.ColorToHex(EventEditor.inst.previewTheme.guiColor);
			guiPreview.color = EventEditor.inst.previewTheme.guiColor;
			guiHex.onValueChanged.AddListener(delegate (string val)
			{
				guiPreview.color = LSColors.HexToColor(val);
				EventEditor.inst.previewTheme.guiColor = LSColors.HexToColorAlpha(val);
				guiPreviewET.triggers.Clear();
				guiPreviewET.triggers.Add(CreatePreviewClickTrigger(guiPreview.transform, guiHex.transform, EventEditor.inst.previewTheme.guiColor));
			});

			guiPreviewET.triggers.Clear();
			guiPreviewET.triggers.Add(CreatePreviewClickTrigger(guiPreview.transform, guiHex.transform, EventEditor.inst.previewTheme.guiColor));

			for (int i = 0; i <= 3; i++)
			{
				var playerHex = themeContent.Find("player" + i + "/hex").GetComponent<InputField>();
				var playerPreview = themeContent.Find("player" + i + "/preview").GetComponent<Image>();
				var playerPreviewET = themeContent.Find("player" + i + "/preview").GetComponent<EventTrigger>();
				int indexTmp = i;
				playerHex.onValueChanged.RemoveAllListeners();
				playerHex.characterLimit = 8;
				playerHex.text = RTEditor.ColorToHex(EventEditor.inst.previewTheme.playerColors[indexTmp]);
				playerPreview.color = EventEditor.inst.previewTheme.playerColors[indexTmp];
				playerHex.GetComponent<InputField>().onValueChanged.AddListener(delegate (string val)
				{
					playerPreview.color = LSColors.HexToColorAlpha(val);
					EventEditor.inst.previewTheme.playerColors[indexTmp] = LSColors.HexToColorAlpha(val);
					playerPreviewET.triggers.Clear();
					playerPreviewET.triggers.Add(CreatePreviewClickTrigger(playerPreview.transform, playerHex.transform, EventEditor.inst.previewTheme.playerColors[indexTmp]));
				});
				playerPreviewET.triggers.Clear();
				playerPreviewET.triggers.Add(CreatePreviewClickTrigger(playerPreview.transform, playerHex.transform, EventEditor.inst.previewTheme.playerColors[indexTmp]));
			}
			for (int j = 0; j <= 17; j++)
			{
				themeContent.Find("object" + j).transform.localRotation = Quaternion.Euler(Vector3.zero);
				var objectHex = themeContent.Find("object" + j + "/hex").GetComponent<InputField>();
				var objectPreview = themeContent.Find("object" + j + "/preview").GetComponent<Image>();
				var objectPreviewET = themeContent.Find("object" + j + "/preview").GetComponent<EventTrigger>();
				int indexTmp = j;
				objectHex.onValueChanged.RemoveAllListeners();
				objectHex.characterLimit = 8;
				objectHex.text = RTEditor.ColorToHex(EventEditor.inst.previewTheme.objectColors[indexTmp]);
				objectPreview.color = EventEditor.inst.previewTheme.objectColors[indexTmp];
				objectHex.onValueChanged.AddListener(delegate (string val)
				{
					objectPreview.color = LSColors.HexToColorAlpha(val);
					EventEditor.inst.previewTheme.objectColors[indexTmp] = LSColors.HexToColorAlpha(val);
					objectPreviewET.triggers.Clear();
					objectPreviewET.triggers.Add(CreatePreviewClickTrigger(objectPreview.transform, objectHex.transform, EventEditor.inst.previewTheme.objectColors[indexTmp]));
				});
				objectPreviewET.triggers.Clear();
				objectPreviewET.triggers.Add(CreatePreviewClickTrigger(objectPreview.transform, objectHex.transform, EventEditor.inst.previewTheme.objectColors[indexTmp]));
			}
			for (int k = 0; k <= 8; k++)
			{
				var bgsHex = themeContent.Find("background" + k + "/hex").GetComponent<InputField>();
				var bgsPreview = themeContent.Find("background" + k + "/preview").GetComponent<Image>();
				var bgsPreviewET = themeContent.Find("background" + k + "/preview").GetComponent<EventTrigger>();
				int indexTmp = k;
				bgsHex.onValueChanged.RemoveAllListeners();
				bgsHex.text = LSColors.ColorToHex(EventEditor.inst.previewTheme.backgroundColors[indexTmp]);
				bgsPreview.color = EventEditor.inst.previewTheme.backgroundColors[indexTmp];
				bgsHex.onValueChanged.AddListener(delegate (string val)
				{
					bgsPreview.GetComponent<Image>().color = LSColors.HexToColor(val);
					EventEditor.inst.previewTheme.backgroundColors[indexTmp] = LSColors.HexToColor(val);
					bgsPreviewET.triggers.Clear();
					bgsPreviewET.triggers.Add(CreatePreviewClickTrigger(bgsPreview.transform, bgsHex.transform, EventEditor.inst.previewTheme.backgroundColors[indexTmp]));
				});

				bgsPreviewET.triggers.Clear();
				bgsPreviewET.triggers.Add(CreatePreviewClickTrigger(bgsPreview.transform, bgsHex.transform, EventEditor.inst.previewTheme.backgroundColors[indexTmp]));
			}
			return false;
		}

		public static EventTrigger.Entry CreatePreviewClickTrigger(Transform _preview, Transform _hex, Color _col)
        {
			return (EventTrigger.Entry)AccessTools.Method(typeof(EventEditor), "CreatePreviewClickTrigger").Invoke(EventEditor.inst, new object[] { _preview, _hex, _col });
		}

		[HarmonyPatch("CreatePreviewClickTrigger")]
		[HarmonyPrefix]
		private static bool CreatePreviewClickTriggerPatch(ref EventTrigger.Entry __result, Transform __0, Transform __1, Color __2)
        {
			__result = Triggers.CreatePreviewClickTrigger(__0, __1, __2);
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
