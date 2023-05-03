using System;
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
		public static List<Toggle> gradientColor1Buttons = new List<Toggle>();
		public static List<Toggle> gradientColor2Buttons = new List<Toggle>();

		[HarmonyPatch("Awake")]
		[HarmonyPostfix]
		private static void AwakePatch()
        {
			GameObject copyBase = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/grain");
			GameObject cgDialog = Instantiate(copyBase);

			Debug.LogFormat("{0}ColorGrading", EditorPlugin.className);
			cgDialog.transform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right").transform);
			cgDialog.transform.localScale = Vector3.one;
			cgDialog.name = "colorgrading";

			var intensityLabel = cgDialog.transform.GetChild(8);
			intensityLabel.GetChild(0).GetComponent<Text>().text = "intensity";

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
				GameObject col = UnityEngine.Object.Instantiate(themeParent.Find("object8").gameObject);
				col.name = "object" + (i - 1).ToString();
				col.transform.SetParent(themeParent);
				col.transform.Find("text").GetComponent<Text>().text = i.ToString();
				col.transform.SetSiblingIndex(8 + i);
			}
		}

        [HarmonyPatch("RenderEventsDialog")]
        [HarmonyPrefix]
        private static bool RenderEventsDialogPatch()
        {
            Transform dialogTmp = EventEditor.inst.dialogRight.GetChild(EventEditor.inst.currentEventType);
			EventEditor.inst.dialogLeft.Find("theme").gameObject.SetActive(false);
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

			timeTime.onValueChanged.RemoveAllListeners();
			timeTime.text = DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventTime.ToString("f2");

			if (EventEditor.inst.currentEvent != 0)
			{
				timeTime.interactable = true;
				timeJumpLeftLarge.interactable = true;
				timeJumpLeftSmall.interactable = true;
				timeJumpRightSmall.interactable = true;
				timeJumpRightLarge.interactable = true;

				timeTime.onValueChanged.AddListener(delegate (string val)
				{
					EventEditor.inst.SetEventStartTime(float.Parse(val));
				});
				timeJumpLeftLarge.onClick.RemoveAllListeners();
				timeJumpLeftLarge.interactable = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventTime > 0f);
				timeJumpLeftLarge.onClick.AddListener(delegate ()
				{
					timeTime.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventTime - 1f).ToString("f2");
				});
				timeJumpLeftSmall.onClick.RemoveAllListeners();
				timeJumpLeftSmall.interactable = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventTime > 0f);
				timeJumpLeftSmall.onClick.AddListener(delegate ()
				{
					timeTime.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventTime - 0.1f).ToString("f2");
				});
				timeJumpRightSmall.onClick.RemoveAllListeners();
				timeJumpRightSmall.onClick.AddListener(delegate ()
				{
					timeTime.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventTime + 0.1f).ToString("f2");
				});
				timeJumpRightLarge.onClick.RemoveAllListeners();
				timeJumpRightLarge.onClick.AddListener(delegate ()
				{
					timeTime.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventTime + 1f).ToString("f2");
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
			switch (EventEditor.inst.currentEventType)
            {
                case 0:
                    {
                        var posX = dialogTmp.Find("position/x").GetComponent<InputField>();
                        var posY = dialogTmp.Find("position/y").GetComponent<InputField>();

						posX.onValueChanged.RemoveAllListeners();
						posX.text = DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0].ToString("f2");
						posX.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] = num;
							EventManager.inst.updateEvents();
						});

						posY.onValueChanged.RemoveAllListeners();
						posY.text = DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[1].ToString("f2");
						posY.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[1] = num;
							EventManager.inst.updateEvents();
						});

						var posXLeft = posX.transform.Find("<").GetComponent<Button>();
						var posXRight = posX.transform.Find(">").GetComponent<Button>();
						var posYLeft = posY.transform.Find("<").GetComponent<Button>();
						var posYRight = posY.transform.Find(">").GetComponent<Button>();

						posXLeft.onClick.RemoveAllListeners();
						posXLeft.onClick.AddListener(delegate ()
						{
							posX.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] - ConfigEntries.EventMoveModify.Value).ToString();
						});

						posXRight.onClick.RemoveAllListeners();
						posXRight.onClick.AddListener(delegate ()
						{
							posX.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] + ConfigEntries.EventMoveModify.Value).ToString();
						});

						posYLeft.onClick.RemoveAllListeners();
						posYLeft.onClick.AddListener(delegate ()
						{
							posY.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[1] - ConfigEntries.EventMoveModify.Value).ToString();
						});

						posYRight.onClick.RemoveAllListeners();
						posYRight.onClick.AddListener(delegate ()
						{
							posY.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[1] + ConfigEntries.EventMoveModify.Value).ToString();
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
				case 1:
                    {
						var zoom = dialogTmp.Find("zoom/x").GetComponent<InputField>();
						zoom.onValueChanged.RemoveAllListeners();
						zoom.text = DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0].ToString("f2");
						zoom.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							num = Mathf.Clamp(num, -9999f, 9999f);
							DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] = num;
							EventManager.inst.updateEvents();
						});

						var zoomLeft = zoom.transform.Find("<").GetComponent<Button>();
						var zoomRight = zoom.transform.Find(">").GetComponent<Button>();

						zoomLeft.onClick.RemoveAllListeners();
						zoomLeft.onClick.AddListener(delegate ()
						{
							zoom.text = Mathf.Clamp(DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] - ConfigEntries.EventZoomModify.Value, -9999f, 9999f).ToString();
							EventManager.inst.updateEvents();
						});

						zoomRight.onClick.RemoveAllListeners();
						zoomRight.onClick.AddListener(delegate ()
						{
							zoom.text = Mathf.Clamp(DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] + ConfigEntries.EventZoomModify.Value, -9999f, 9999f).ToString();
							EventManager.inst.updateEvents();
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
				case 2:
                    {
						var rotate = dialogTmp.Find("rotation/x").GetComponent<InputField>();
						rotate.onValueChanged.RemoveAllListeners();
						rotate.text = DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0].ToString("f2");
						rotate.onValueChanged.AddListener(delegate (string val)
						{
							DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] = float.Parse(val);
							EventManager.inst.updateEvents();
						});

						var rotateLeft = rotate.transform.Find("<").GetComponent<Button>();
						var rotateRight = rotate.transform.Find("<").GetComponent<Button>();

						rotateLeft.onClick.RemoveAllListeners();
						rotateLeft.onClick.AddListener(delegate ()
						{
							rotate.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] - ConfigEntries.EventRotateModify.Value).ToString();
							EventManager.inst.updateEvents();
						});

						rotateRight.onClick.RemoveAllListeners();
						rotateRight.onClick.AddListener(delegate ()
						{
							rotate.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] + ConfigEntries.EventRotateModify.Value).ToString();
							EventManager.inst.updateEvents();
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
				case 3:
                    {
						var shake = dialogTmp.Find("shake/x").GetComponent<InputField>();
						shake.onValueChanged.RemoveAllListeners();
						shake.text = DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0].ToString("f2");
						shake.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							num = Mathf.Clamp(num, 0f, 10f);
							DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] = num;
							EventManager.inst.updateEvents();
						});

						var shakeLeft = shake.transform.Find("<").GetComponent<Button>();
						var shakeRight = shake.transform.Find(">").GetComponent<Button>();

						shakeLeft.onClick.RemoveAllListeners();
						shakeLeft.onClick.AddListener(delegate ()
						{
							shake.text = Mathf.Clamp(DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] - ConfigEntries.EventShakeModify.Value, 0f, 10f).ToString();
							EventManager.inst.updateEvents();
						});

						shakeRight.onClick.RemoveAllListeners();
						shakeRight.onClick.AddListener(delegate ()
						{
							shake.text = Mathf.Clamp(DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] + ConfigEntries.EventShakeModify.Value, 0f, 10f).ToString();
							EventManager.inst.updateEvents();
						});

						if (!dialogTmp.Find("shake/x").GetComponent<EventTrigger>())
                        {
							dialogTmp.Find("shake/x").gameObject.AddComponent<EventTrigger>();
                        }

						var shakeET = dialogTmp.Find("shake/x").GetComponent<EventTrigger>();

						shakeET.triggers.Clear();
						shakeET.triggers.Add(Triggers.ScrollDelta(shake, 0.1f, 10f));
						break;
                    }
				case 4:
                    {
						var theme = dialogTmp.Find("theme-search").GetComponent<InputField>();

						theme.onValueChanged.RemoveAllListeners();
						theme.onValueChanged.AddListener(delegate (string val)
						{
							EventEditor.inst.RenderThemeContent(dialogTmp, val);
						});
						EventEditor.inst.RenderThemeContent(dialogTmp, theme.text);
						EventEditor.inst.RenderThemePreview(dialogTmp);
						break;
                    }
				case 5:
                    {
						var chroma = dialogTmp.Find("chroma/x").GetComponent<InputField>();
						chroma.onValueChanged.RemoveAllListeners();
						chroma.text = DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0].ToString("f2");
						chroma.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							num = Mathf.Clamp(num, 0f, 9999f);
							DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] = num;
							EventManager.inst.updateEvents();
						});

						var chromaLeft = chroma.transform.Find("<").GetComponent<Button>();
						var chromaRight = chroma.transform.Find(">").GetComponent<Button>();

						chromaLeft.onClick.RemoveAllListeners();
						chromaLeft.onClick.AddListener(delegate ()
						{
							chroma.text = Mathf.Clamp(DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] - ConfigEntries.EventChromaModify.Value, 0f, 9999f).ToString();
							EventManager.inst.updateEvents();
						});

						chromaRight.onClick.RemoveAllListeners();
						chromaRight.onClick.AddListener(delegate ()
						{
							chroma.text = Mathf.Clamp(DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] + ConfigEntries.EventChromaModify.Value, 0f, 9999f).ToString();
							EventManager.inst.updateEvents();
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
				case 6:
                    {
						var bloom = dialogTmp.Find("bloom/x").GetComponent<InputField>();
						bloom.onValueChanged.RemoveAllListeners();
						bloom.text = DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0].ToString("f2");
						bloom.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							num = Mathf.Clamp(num, 0f, 1280f);
							DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] = num;
							EventManager.inst.updateEvents();
						});

						var bloomLeft = bloom.transform.Find("<").GetComponent<Button>();
						var bloomRight = bloom.transform.Find(">").GetComponent<Button>();

						bloomLeft.onClick.RemoveAllListeners();
						bloomLeft.onClick.AddListener(delegate ()
						{
							bloom.text = Mathf.Clamp(DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] - ConfigEntries.EventBloomModify.Value, 0f, 1280f).ToString();
							EventManager.inst.updateEvents();
						});

						bloomRight.onClick.RemoveAllListeners();
						bloomRight.onClick.AddListener(delegate ()
						{
							bloom.text = Mathf.Clamp(DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] + ConfigEntries.EventBloomModify.Value, 0f, 1280f).ToString();
							EventManager.inst.updateEvents();
						});

						if (!dialogTmp.Find("bloom/x").GetComponent<EventTrigger>())
                        {
							dialogTmp.Find("bloom/x").gameObject.AddComponent<EventTrigger>();
                        }

						var bloomET = dialogTmp.Find("bloom/x").GetComponent<EventTrigger>();

						bloomET.triggers.Clear();
						bloomET.triggers.Add(Triggers.ScrollDelta(bloom, 0.1f, 10f));
						break;
                    }
				case 7:
                    {
						var vignetteIntensity = dialogTmp.Find("intensity").GetComponent<InputField>();

						vignetteIntensity.onValueChanged.RemoveAllListeners();
						vignetteIntensity.text = DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0].ToString("f2");
						vignetteIntensity.onValueChanged.AddListener(delegate (string val)
						{
							DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] = float.Parse(val);
							EventManager.inst.updateEvents();
						});

						var vignetteIntensityLeft = vignetteIntensity.transform.Find("<").GetComponent<Button>();
						var vignetteIntensityRight = vignetteIntensity.transform.Find(">").GetComponent<Button>();

						vignetteIntensityLeft.onClick.RemoveAllListeners();
						vignetteIntensityLeft.onClick.AddListener(delegate ()
						{
							vignetteIntensity.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] - ConfigEntries.EventVignetteIntensityModify.Value).ToString();
							EventManager.inst.updateEvents();
						});

						vignetteIntensityRight.onClick.RemoveAllListeners();
						vignetteIntensityRight.onClick.AddListener(delegate ()
						{
							vignetteIntensity.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] + ConfigEntries.EventVignetteIntensityModify.Value).ToString();
							EventManager.inst.updateEvents();
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
						vignetteSmoothness.text = DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[1].ToString("f2");
						vignetteSmoothness.onValueChanged.AddListener(delegate (string val)
						{
							DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[1] = float.Parse(val);
							EventManager.inst.updateEvents();
						});

						var vignetteSmoothnessLeft = vignetteSmoothness.transform.Find("<").GetComponent<Button>();
						var vignetteSmoothnessRight = vignetteSmoothness.transform.Find(">").GetComponent<Button>();

						vignetteSmoothnessLeft.onClick.RemoveAllListeners();
						vignetteSmoothnessLeft.onClick.AddListener(delegate ()
						{
							vignetteSmoothness.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[1] - ConfigEntries.EventVignetteSmoothnessModify.Value).ToString();
							EventManager.inst.updateEvents();
						});

						vignetteSmoothnessRight.onClick.RemoveAllListeners();
						vignetteSmoothnessRight.onClick.AddListener(delegate ()
						{
							vignetteSmoothness.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[1] + ConfigEntries.EventVignetteSmoothnessModify.Value).ToString();
							EventManager.inst.updateEvents();
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
						vignetteRounded.isOn = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[2] == 1f);
						vignetteRounded.onValueChanged.AddListener(delegate (bool val)
						{
							DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[2] = (val ? 1 : 0);
							EventManager.inst.updateEvents();
						});

						var vignetteRoundness = dialogTmp.Find("roundness").GetComponent<InputField>();

						vignetteRoundness.onValueChanged.RemoveAllListeners();
						vignetteRoundness.text = DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[3].ToString("f2");
						vignetteRoundness.onValueChanged.AddListener(delegate (string val)
						{
							DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[3] = float.Parse(val);
							EventManager.inst.updateEvents();
						});

						var vignetteRoundnessLeft = vignetteRoundness.transform.Find("<").GetComponent<Button>();
						var vignetteRoundnessRight = vignetteRoundness.transform.Find(">").GetComponent<Button>();

						vignetteRoundnessLeft.onClick.RemoveAllListeners();
						vignetteRoundnessLeft.onClick.AddListener(delegate ()
						{
							vignetteRoundness.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] - ConfigEntries.EventVignetteRoundnessModify.Value).ToString();
							EventManager.inst.updateEvents();
						});

						vignetteRoundnessRight.onClick.RemoveAllListeners();
						vignetteRoundnessRight.onClick.AddListener(delegate ()
						{
							vignetteRoundness.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] + ConfigEntries.EventVignetteRoundnessModify.Value).ToString();
							EventManager.inst.updateEvents();
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
						vignetteCenterX.text = DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[4].ToString("f2");
						vignetteCenterX.onValueChanged.AddListener(delegate (string val)
						{
							DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[4] = float.Parse(val);
							EventManager.inst.updateEvents();
						});

						vignetteCenterY.onValueChanged.RemoveAllListeners();
						vignetteCenterY.text = DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[5].ToString("f2");
						vignetteCenterY.onValueChanged.AddListener(delegate (string val)
						{
							DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[5] = float.Parse(val);
							EventManager.inst.updateEvents();
						});

						var vignetteCenterXLeft = vignetteCenterX.transform.Find("<").GetComponent<Button>();
						var vignetteCenterXRight = vignetteCenterX.transform.Find(">").GetComponent<Button>();
						var vignetteCenterYLeft = vignetteCenterX.transform.Find("<").GetComponent<Button>();
						var vignetteCenterYRight = vignetteCenterX.transform.Find(">").GetComponent<Button>();

						vignetteCenterXLeft.onClick.RemoveAllListeners();
						vignetteCenterXLeft.onClick.AddListener(delegate ()
						{
							vignetteCenterX.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[4] - ConfigEntries.EventVignettePosModify.Value).ToString();
							EventManager.inst.updateEvents();
						});

						vignetteCenterXLeft.onClick.RemoveAllListeners();
						vignetteCenterXLeft.onClick.AddListener(delegate ()
						{
							vignetteCenterX.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[4] + ConfigEntries.EventVignettePosModify.Value).ToString();
							EventManager.inst.updateEvents();
						});

						vignetteCenterYLeft.onClick.RemoveAllListeners();
						vignetteCenterYLeft.onClick.AddListener(delegate ()
						{
							vignetteCenterY.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[5] - ConfigEntries.EventVignettePosModify.Value).ToString();
							EventManager.inst.updateEvents();
						});

						vignetteCenterYLeft.onClick.RemoveAllListeners();
						vignetteCenterYLeft.onClick.AddListener(delegate ()
						{
							vignetteCenterY.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[5] + ConfigEntries.EventVignettePosModify.Value).ToString();
							EventManager.inst.updateEvents();
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
						break;
                    }
				case 8:
                    {
						var lens = dialogTmp.Find("lens/x").GetComponent<InputField>();
						lens.onValueChanged.RemoveAllListeners();
						lens.text = DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0].ToString("f2");
						lens.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							num = Mathf.Clamp(num, -100f, 100f);
							DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] = num;
							EventManager.inst.updateEvents();
						});

						var lensLeft = lens.transform.Find("<").GetComponent<Button>();
						var lensRight = lens.transform.Find(">").GetComponent<Button>();

						lensLeft.onClick.RemoveAllListeners();
						lensLeft.onClick.AddListener(delegate ()
						{
							lens.text = Mathf.Clamp(DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] - ConfigEntries.EventLensModify.Value, -100f, 100f).ToString();
							EventManager.inst.updateEvents();
						});

						lensLeft.onClick.RemoveAllListeners();
						lensLeft.onClick.AddListener(delegate ()
						{
							lens.text = Mathf.Clamp(DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] + ConfigEntries.EventLensModify.Value, -100f, 100f).ToString();
							EventManager.inst.updateEvents();
						});

						if (!dialogTmp.Find("lens/x").GetComponent<EventTrigger>())
                        {
							dialogTmp.Find("lens/x").gameObject.AddComponent<EventTrigger>();
                        }

						var lensET = dialogTmp.Find("lens/x").GetComponent<EventTrigger>();

						lensET.triggers.Clear();
						lensET.triggers.Add(Triggers.ScrollDelta(lens, 1f, 10f));
						break;
                    }
				case 9:
                    {
						var grainIntensity = dialogTmp.Find("intensity").GetComponent<InputField>();
						grainIntensity.onValueChanged.RemoveAllListeners();
						grainIntensity.text = DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0].ToString("f2");
						grainIntensity.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							num = Mathf.Clamp(num, 0f, 9999f);
							DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] = num;
							EventManager.inst.updateEvents();
						});

						var grainIntensityLeft = grainIntensity.transform.Find("<").GetComponent<Button>();
						var grainIntensityRight = grainIntensity.transform.Find(">").GetComponent<Button>();

						grainIntensityLeft.onClick.RemoveAllListeners();
						grainIntensityLeft.onClick.AddListener(delegate ()
						{
							grainIntensity.text = Mathf.Clamp(DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] - ConfigEntries.EventGrainIntensityModify.Value, 0f, 9999f).ToString();
							EventManager.inst.updateEvents();
						});

						grainIntensityRight.onClick.RemoveAllListeners();
						grainIntensityRight.onClick.AddListener(delegate ()
						{
							grainIntensity.text = Mathf.Clamp(DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] + ConfigEntries.EventGrainIntensityModify.Value, 0f, 9999f).ToString();
							EventManager.inst.updateEvents();
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
						grainColored.isOn = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[1] == 1f);
						grainColored.onValueChanged.AddListener(delegate (bool val)
						{
							DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[1] = (float)(val ? 1 : 0);
							EventManager.inst.updateEvents();
						});

						var grainSize = dialogTmp.Find("size").GetComponent<InputField>();
						grainSize.onValueChanged.RemoveAllListeners();
						grainSize.text = DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[2].ToString("f2");
						grainSize.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							num = Mathf.Clamp(num, 0f, 9999f);
							DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[2] = num;
							EventManager.inst.updateEvents();
						});

						var grainSizeLeft = grainSize.transform.Find("<").GetComponent<Button>();
						var grainSizeRight = grainSize.transform.Find(">").GetComponent<Button>();

						grainSizeLeft.onClick.RemoveAllListeners();
						grainSizeLeft.onClick.AddListener(delegate ()
						{
							grainSize.text = Mathf.Clamp(DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[2] - ConfigEntries.EventGrainSizeModify.Value, 0f, 9999f).ToString();
							EventManager.inst.updateEvents();
						});

						grainSizeRight.onClick.RemoveAllListeners();
						grainSizeRight.onClick.AddListener(delegate ()
						{
							grainSize.text = Mathf.Clamp(DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[2] + ConfigEntries.EventGrainSizeModify.Value, 0f, 9999f).ToString();
							EventManager.inst.updateEvents();
						});
						break;
					}
				case 10:
					{
						//intensity
						var intensity = dialogTmp.Find("intensity").GetComponent<InputField>();
						intensity.onValueChanged.RemoveAllListeners();
						intensity.text = DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0].ToString("f2");
						intensity.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] = num;
							EventManager.inst.updateEvents();
						});

						var intensityLeft = intensity.transform.Find("<").GetComponent<Button>();
						var intensityRight = intensity.transform.Find(">").GetComponent<Button>();

						intensityLeft.onClick.RemoveAllListeners();
						intensityLeft.onClick.AddListener(delegate ()
						{
							intensity.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] - ConfigEntries.EventMoveModify.Value).ToString();
							EventManager.inst.updateEvents();
						});

						intensityRight.onClick.RemoveAllListeners();
						intensityRight.onClick.AddListener(delegate ()
						{
							intensity.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] + ConfigEntries.EventMoveModify.Value).ToString();
							EventManager.inst.updateEvents();
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
						contrast.text = DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[1].ToString("f2");
						contrast.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[1] = num;
							EventManager.inst.updateEvents();
						});

						var contrastLeft = contrast.transform.Find("<").GetComponent<Button>();
						var contrastRight = contrast.transform.Find(">").GetComponent<Button>();

						contrastLeft.onClick.RemoveAllListeners();
						contrastLeft.onClick.AddListener(delegate ()
						{
							contrast.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[1] - ConfigEntries.EventMoveModify.Value).ToString();
							EventManager.inst.updateEvents();
						});

						contrastRight.onClick.RemoveAllListeners();
						contrastRight.onClick.AddListener(delegate ()
						{
							contrast.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[1] + ConfigEntries.EventMoveModify.Value).ToString();
							EventManager.inst.updateEvents();
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
						saturation.text = DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[6].ToString("f2");
						saturation.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[6] = num;
							EventManager.inst.updateEvents();
						});

						var saturationLeft = saturation.transform.Find("<").GetComponent<Button>();
						var saturationRight = saturation.transform.Find(">").GetComponent<Button>();

						saturationLeft.onClick.RemoveAllListeners();
						saturationLeft.onClick.AddListener(delegate ()
						{
							saturation.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[6] - ConfigEntries.EventMoveModify.Value).ToString();
							EventManager.inst.updateEvents();
						});

						saturationRight.onClick.RemoveAllListeners();
						saturationRight.onClick.AddListener(delegate ()
						{
							saturation.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[6] + ConfigEntries.EventMoveModify.Value).ToString();
							EventManager.inst.updateEvents();
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
						temperature.text = DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[7].ToString("f2");
						temperature.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[7] = num;
							EventManager.inst.updateEvents();
						});

						var temperatureLeft = temperature.transform.Find("<").GetComponent<Button>();
						var temperatureRight = temperature.transform.Find(">").GetComponent<Button>();

						temperatureLeft.onClick.RemoveAllListeners();
						temperatureLeft.onClick.AddListener(delegate ()
						{
							temperature.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[7] - ConfigEntries.EventMoveModify.Value).ToString();
							EventManager.inst.updateEvents();
						});

						temperatureRight.onClick.RemoveAllListeners();
						temperatureRight.onClick.AddListener(delegate ()
						{
							temperature.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[7] + ConfigEntries.EventMoveModify.Value).ToString();
							EventManager.inst.updateEvents();
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
						tint.text = DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[8].ToString("f2");
						tint.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[8] = num;
							EventManager.inst.updateEvents();
						});

						var tintLeft = tint.transform.Find("<").GetComponent<Button>();
						var tintRight = tint.transform.Find(">").GetComponent<Button>();

						tintLeft.onClick.RemoveAllListeners();
						tintLeft.onClick.AddListener(delegate ()
						{
							tint.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[8] - ConfigEntries.EventMoveModify.Value).ToString();
							EventManager.inst.updateEvents();
						});

						tintRight.onClick.RemoveAllListeners();
						tintRight.onClick.AddListener(delegate ()
						{
							tint.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[8] + ConfigEntries.EventMoveModify.Value).ToString();
							EventManager.inst.updateEvents();
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
				case 11:
					{
						//Strength
						var strength = dialogTmp.Find("strength").GetComponent<InputField>();
						strength.onValueChanged.RemoveAllListeners();
						strength.text = DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0].ToString("f2");
						strength.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] = num;
							EventManager.inst.updateEvents();
						});

						var strengthLeft = strength.transform.Find("<").GetComponent<Button>();
						var strengthRight = strength.transform.Find(">").GetComponent<Button>();

						strengthLeft.onClick.RemoveAllListeners();
						strengthLeft.onClick.AddListener(delegate ()
						{
							strength.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] - ConfigEntries.EventMoveModify.Value).ToString();
							EventManager.inst.updateEvents();
						});

						strengthRight.onClick.RemoveAllListeners();
						strengthRight.onClick.AddListener(delegate ()
						{
							strength.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] + ConfigEntries.EventMoveModify.Value).ToString();
							EventManager.inst.updateEvents();
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
						speed.text = DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[1].ToString("f2");
						speed.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[1] = num;
							EventManager.inst.updateEvents();
						});

						var speedLeft = speed.transform.Find("<").GetComponent<Button>();
						var speedRight = speed.transform.Find(">").GetComponent<Button>();

						speedLeft.onClick.RemoveAllListeners();
						speedLeft.onClick.AddListener(delegate ()
						{
							speed.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[1] - ConfigEntries.EventMoveModify.Value).ToString();
							EventManager.inst.updateEvents();
						});

						speedRight.onClick.RemoveAllListeners();
						speedRight.onClick.AddListener(delegate ()
						{
							speed.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[1] + ConfigEntries.EventMoveModify.Value).ToString();
							EventManager.inst.updateEvents();
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
						distance.text = DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[2].ToString("f2");
						distance.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[2] = num;
							EventManager.inst.updateEvents();
						});

						var distanceLeft = distance.transform.Find("<").GetComponent<Button>();
						var distanceRight = distance.transform.Find(">").GetComponent<Button>();

						distanceLeft.onClick.RemoveAllListeners();
						distanceLeft.onClick.AddListener(delegate ()
						{
							distance.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[2] - ConfigEntries.EventMoveModify.Value).ToString();
							EventManager.inst.updateEvents();
						});

						distanceRight.onClick.RemoveAllListeners();
						distanceRight.onClick.AddListener(delegate ()
						{
							distance.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[2] + ConfigEntries.EventMoveModify.Value).ToString();
							EventManager.inst.updateEvents();
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
						height.text = DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[3].ToString("f2");
						height.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[3] = num;
							EventManager.inst.updateEvents();
						});

						var heightLeft = height.transform.Find("<").GetComponent<Button>();
						var heightRight = height.transform.Find(">").GetComponent<Button>();

						heightLeft.onClick.RemoveAllListeners();
						heightLeft.onClick.AddListener(delegate ()
						{
							height.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[3] - ConfigEntries.EventMoveModify.Value).ToString();
							EventManager.inst.updateEvents();
						});

						heightRight.onClick.RemoveAllListeners();
						heightRight.onClick.AddListener(delegate ()
						{
							height.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[3] + ConfigEntries.EventMoveModify.Value).ToString();
							EventManager.inst.updateEvents();
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
						width.text = DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[4].ToString("f2");
						width.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[4] = num;
							EventManager.inst.updateEvents();
						});

						var widthLeft = width.transform.Find("<").GetComponent<Button>();
						var widthRight = width.transform.Find(">").GetComponent<Button>();

						widthLeft.onClick.RemoveAllListeners();
						widthLeft.onClick.AddListener(delegate ()
						{
							width.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[4] - ConfigEntries.EventMoveModify.Value).ToString();
							EventManager.inst.updateEvents();
						});

						widthRight.onClick.RemoveAllListeners();
						widthRight.onClick.AddListener(delegate ()
						{
							width.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[4] + ConfigEntries.EventMoveModify.Value).ToString();
							EventManager.inst.updateEvents();
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
				case 12:
					{
						//RadialBlur
						var intensity = dialogTmp.Find("intensity").GetComponent<InputField>();
						intensity.onValueChanged.RemoveAllListeners();
						intensity.text = DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0].ToString("f2");
						intensity.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] = num;
							EventManager.inst.updateEvents();
						});

						var intensityLeft = intensity.transform.Find("<").GetComponent<Button>();
						var intensityRight = intensity.transform.Find(">").GetComponent<Button>();

						intensityLeft.onClick.RemoveAllListeners();
						intensityLeft.onClick.AddListener(delegate ()
						{
							intensity.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] - ConfigEntries.EventMoveModify.Value).ToString();
							EventManager.inst.updateEvents();
						});

						intensityRight.onClick.RemoveAllListeners();
						intensityRight.onClick.AddListener(delegate ()
						{
							intensity.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] + ConfigEntries.EventMoveModify.Value).ToString();
							EventManager.inst.updateEvents();
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
						iterations.text = DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[1].ToString("f2");
						iterations.onValueChanged.AddListener(delegate (string val)
						{
							int num = int.Parse(val);
							num = Mathf.Clamp(num, 1, 16);
							DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[1] = num;
							EventManager.inst.updateEvents();
						});

						var iterationsLeft = iterations.transform.Find("<").GetComponent<Button>();
						var iterationsRight = iterations.transform.Find(">").GetComponent<Button>();

						iterationsLeft.onClick.RemoveAllListeners();
						iterationsLeft.onClick.AddListener(delegate ()
						{
							iterations.text = Mathf.Clamp(DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[1] - (int)ConfigEntries.EventMoveModify.Value, 1, 16).ToString();
							EventManager.inst.updateEvents();
						});

						iterationsRight.onClick.RemoveAllListeners();
						iterationsRight.onClick.AddListener(delegate ()
						{
							iterations.text = Mathf.Clamp(DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[1] + (int)ConfigEntries.EventMoveModify.Value, 1, 16).ToString();
							EventManager.inst.updateEvents();
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
				case 13:
					{
						//ColorSplit
						var intensity = dialogTmp.Find("offset").GetComponent<InputField>();
						intensity.onValueChanged.RemoveAllListeners();
						intensity.text = DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0].ToString("f2");
						intensity.onValueChanged.AddListener(delegate (string val)
						{
							float num = float.Parse(val);
							DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] = num;
							EventManager.inst.updateEvents();
						});

						var intensityLeft = intensity.transform.Find("<").GetComponent<Button>();
						var intensityRight = intensity.transform.Find(">").GetComponent<Button>();

						intensityLeft.onClick.RemoveAllListeners();
						intensityLeft.onClick.AddListener(delegate ()
						{
							intensity.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] - ConfigEntries.EventMoveModify.Value).ToString();
							EventManager.inst.updateEvents();
						});

						intensityRight.onClick.RemoveAllListeners();
						intensityRight.onClick.AddListener(delegate ()
						{
							intensity.text = (DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] + ConfigEntries.EventMoveModify.Value).ToString();
							EventManager.inst.updateEvents();
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
			}

			var curvesDropdown = dialogTmp.transform.Find("curves").GetComponent<Dropdown>();

			dialogTmp.transform.Find("curves_label").gameObject.SetActive(EventEditor.inst.currentEvent != 0);
			curvesDropdown.gameObject.SetActive(EventEditor.inst.currentEvent != 0);
			curvesDropdown.onValueChanged.RemoveAllListeners();
			if (DataManager.inst.AnimationListDictionaryBack.ContainsKey(DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].curveType))
			{
				curvesDropdown.value = DataManager.inst.AnimationListDictionaryBack[DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].curveType];
			}
			curvesDropdown.onValueChanged.AddListener(delegate (int _value)
			{
				DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].curveType = DataManager.inst.AnimationListDictionary[_value];
				EventManager.inst.updateEvents();
			});

			var editJumpLeftLarge = dialogTmp.Find("edit/<<").GetComponent<Button>();
			var editJumpLeft = dialogTmp.Find("edit/<").GetComponent<Button>();
			var editJumpRight = dialogTmp.Find("edit/>").GetComponent<Button>();
			var editJumpRightLarge = dialogTmp.Find("edit/>>").GetComponent<Button>();

			editJumpLeftLarge.interactable = (EventEditor.inst.currentEvent != 0);
			editJumpLeftLarge.onClick.RemoveAllListeners();
			editJumpLeftLarge.onClick.AddListener(delegate ()
			{
				EventEditor.inst.UpdateEventOrder(false);
				EventEditor.inst.SetCurrentEvent(EventEditor.inst.currentEventType, 0);
			});
			editJumpLeft.interactable = (EventEditor.inst.currentEvent != 0);
			editJumpLeft.onClick.RemoveAllListeners();
			editJumpLeft.onClick.AddListener(delegate ()
			{
				EventEditor.inst.UpdateEventOrder(false);
				int num = EventEditor.inst.currentEvent - 1;
				if (num < 0)
				{
					num = 0;
				}
				EventEditor.inst.SetCurrentEvent(EventEditor.inst.currentEventType, num);
			});
			if (EventEditor.inst.currentEvent == 0)
			{
				dialogTmp.Find("edit/|/text").GetComponent<Text>().text = "S";
			}
			else if (EventEditor.inst.currentEvent == DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType].Count() - 1)
			{
				dialogTmp.Find("edit/|/text").GetComponent<Text>().text = "E";
			}
			else
			{
				dialogTmp.Find("edit/|/text").GetComponent<Text>().text = EventEditor.inst.currentEvent.ToString();
			}
			editJumpRight.interactable = (EventEditor.inst.currentEvent != DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType].Count() - 1);
			editJumpRight.onClick.RemoveAllListeners();
			editJumpRight.onClick.AddListener(delegate ()
			{
				EventEditor.inst.UpdateEventOrder(false);
				int num = EventEditor.inst.currentEvent + 1;
				if (num >= DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType].Count())
				{
					num = DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType].Count() - 1;
				}
				EventEditor.inst.SetCurrentEvent(EventEditor.inst.currentEventType, num);
			});
			editJumpRightLarge.interactable = (EventEditor.inst.currentEvent != DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType].Count() - 1);
			editJumpRightLarge.onClick.RemoveAllListeners();
			editJumpRightLarge.onClick.AddListener(delegate ()
			{
				EventEditor.inst.UpdateEventOrder(false);
				EventEditor.inst.SetCurrentEvent(EventEditor.inst.currentEventType, DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType].IndexOf(DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType].Last()));
			});

			var editDelete = dialogTmp.Find("edit/del").GetComponent<Button>();

			editDelete.onClick.RemoveAllListeners();
			editDelete.interactable = (EventEditor.inst.currentEvent != 0);
			editDelete.onClick.AddListener(delegate ()
			{
				EventEditor.inst.DeleteEvent(EventEditor.inst.currentEventType, EventEditor.inst.currentEvent);
			});
			return false;
        }

		public static void SetGradientColor1(int _value)
		{
			DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[1] = (float)_value;
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
			DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[2] = (float)_value;
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

		[HarmonyPatch("RenderThemeEditor")]
		[HarmonyPostfix]
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
		[HarmonyPostfix]
		public static void AddThemeListLayout()
		{
			GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/theme/themes").GetComponent<ScrollRect>().horizontal = ConfigEntries.ListHorizontal.Value;

			if (!GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/theme/themes/viewport/content").GetComponent<GridLayoutGroup>())
			{
				GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/theme/themes/viewport/content").AddComponent<GridLayoutGroup>();
			}

			GridLayoutGroup prefabLay = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/theme/themes/viewport/content").GetComponent<GridLayoutGroup>();
			prefabLay.cellSize = ConfigEntries.ListCellSize.Value;
			prefabLay.constraint = ConfigEntries.ListConstraint.Value;
			prefabLay.constraintCount = ConfigEntries.ListConstraintCount.Value;
			prefabLay.spacing = ConfigEntries.ListSpacing.Value;
			prefabLay.startAxis = ConfigEntries.ListAxis.Value;

			ContentSizeFitter csf = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/theme/themes/viewport/content").GetComponent<ContentSizeFitter>();
			csf.horizontalFit = ContentSizeFitter.FitMode.MinSize;

			GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/theme/themes/viewport/content/theme-add(Clone)").transform.SetAsFirstSibling();

			//LayoutElement layoutElement = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/theme/themes").AddComponent<LayoutElement>();
			//layoutElement.ignoreLayout = true;
		}

		[HarmonyPatch("RenderThemeEditor")]
		[HarmonyPrefix]
		private static bool RenderThemeEditor(int __0)
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
	}
}
