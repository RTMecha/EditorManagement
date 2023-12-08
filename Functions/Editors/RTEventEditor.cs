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

using EditorManagement.Functions.Components;
using EditorManagement.Functions.Helpers;
using EditorManagement.Patchers;

using RTFunctions.Functions;
using RTFunctions.Functions.Animation;
using RTFunctions.Functions.Animation.Keyframe;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Data;
using RTFunctions.Functions.Managers;
using RTFunctions.Functions.Optimization;

using BaseEventKeyframe = DataManager.GameData.EventKeyframe;

using ObjectType = DataManager.GameData.BeatmapObject.ObjectType;
using AutoKillType = DataManager.GameData.BeatmapObject.AutoKillType;

using ObjectSelection = ObjEditor.ObjectSelection;
using ObjectKeyframeSelection = ObjEditor.KeyframeSelection;
using EventKeyframeSelection = EventEditor.KeyframeSelection;

namespace EditorManagement.Functions.Editors
{
    public class RTEventEditor : MonoBehaviour
	{
		public static RTEventEditor inst;

		// TODO
		// -Gradient Intensity and Rotation need to have the same parent so the Gradient Mode won't go offscreen lol
		// -Make sure all GUI elements are consistent so the singular method works

		#region Variables

		public List<TimelineObject> SelectedKeyframes => RTEditor.inst.timelineKeyframes.FindAll(x => x.selected);

		public List<TimelineObject> copiedEventKeyframes = new List<TimelineObject>();

		public static List<List<BaseEventKeyframe>> AllEvents => DataManager.inst.gameData.eventObjects.allEvents;

		// Timeline will only ever have up to 15 "bins" and since the 15th bin is the checkpoints, we only need the first 14 bins.
		public const int EventLimit = 14;

		// Will implement this as an Editor Property later.
		public static bool ResetRotation => true;

		public List<Toggle> vignetteColorButtons = new List<Toggle>();
		public List<Toggle> bloomColorButtons = new List<Toggle>();
		public List<Toggle> gradientColor1Buttons = new List<Toggle>();
		public List<Toggle> gradientColor2Buttons = new List<Toggle>();
		public List<Toggle> bgColorButtons = new List<Toggle>();
		public List<Toggle> overlayColorButtons = new List<Toggle>();
		public List<Toggle> timelineColorButtons = new List<Toggle>();

		public static string[] EventTypes => new string[]
		{
			"Move",
			"Zoom",
			"Rotate",
			"Shake",
			"Theme",
			"Chroma",
			"Bloom",
			"Vignette",
			"Lens",
			"Grain",
			"Color Grading",
			"Ripples",
			"Radial Blur",
			"Color Split",
			"Offset",
			"Gradient",
			"Double Vision",
			"Scan Lines",
			"Blur",
			"Pixelize",
			"BG",
			"Invert",
			"Timeline",
			"Player",
			"Follow Player",
			"Audio",
			"???",
			"???",
		};

		public static Dictionary<string, Color> EventTitles => new Dictionary<string, Color>()
        {
			{ "- Move Editor -", new Color(0.3372549f, 0.2941177f, 0.4156863f, 1f) }, // 1
			{ "- Zoom Editor -", new Color(0.254902f, 0.2705882f, 0.372549f, 1f) }, // 2
			{ "- Rotation Editor -", new Color(0.2705882f, 0.3843138f, 0.4784314f, 1f) }, // 3
			{ "- Shake Editor -", new Color(0.1960784f, 0.3607843f, 0.4313726f, 1f) }, // 4
			{ "- Theme Editor -", new Color(0.2470588f, 0.427451f, 0.4509804f, 1f) }, // 5
			{ "- Chromatic Editor -", new Color(0.1882353f, 0.3372549f, 0.3254902f, 1f) }, // 6
			{ "- Bloom Editor -", new Color(0.3137255f, 0.4117647f, 0.3176471f, 1f) }, // 7
			{ "- Vignette Editor -", new Color(0.3176471f, 0.3686275f, 0.2588235f, 1f) }, // 8
			{ "- Lens Distort Editor -", new Color(0.4039216f, 0.4117647f, 0.2745098f, 1f) }, // 9
			{ "- Grain Editor -", new Color(0.4470589f, 0.3882353f, 0.2117647f, 1f) }, // 10
			{ "- Color Grading Editor -", new Color(1f, 0.5960785f, 0f, 1f) }, // 11
			{ "- Ripples Editor -", new Color(1f, 0.3490196f, 0f, 1f) }, // 12
			{ "- Radial Blur Editor -", new Color(1f, 0.1490196f, 0.03529412f, 1f) }, // 13
			{ "- Color Split Editor -", new Color(1f, 0.05882353f, 0.05882353f, 1f) }, // 14

			{ "- Camera Offset Editor -", new Color(0.3372549f, 0.2941177f, 0.4156863f, 1f) }, // 1
			{ "- Gradient Editor -", new Color(0.254902f, 0.2705882f, 0.372549f, 1f) }, // 2
			{ "- Double Vision Editor -", new Color(0.2705882f, 0.3843138f, 0.4784314f, 1f) }, // 3
			{ "- Scan Lines Editor -", new Color(0.1960784f, 0.3607843f, 0.4313726f, 1f) }, // 4
			{ "- Blur Editor -", new Color(0.2470588f, 0.427451f, 0.4509804f, 1f) }, // 5
			{ "- Pixelize Editor -", new Color(0.1882353f, 0.3372549f, 0.3254902f, 1f) }, // 6
			{ "- BG Editor -", new Color(0.3137255f, 0.4117647f, 0.3176471f, 1f) }, // 7
			{ "- Invert Editor -", new Color(0.3176471f, 0.3686275f, 0.2588235f, 1f) }, // 8
			{ "- Timeline Editor -", new Color(0.4039216f, 0.4117647f, 0.2745098f, 1f) }, // 9
			{ "- Player Event Editor -", new Color(0.4470589f, 0.3882353f, 0.2117647f, 1f) }, // 10
			{ "- Follow Player Editor -", new Color(1f, 0.5960785f, 0f, 1f) }, // 11
			{ "- Audio Editor -", new Color(1f, 0.3490196f, 0f, 1f) }, // 12
			{ "- ??? 1 Editor -", new Color(1f, 0.1490196f, 0.03529412f, 1f) }, // 13
			{ "- ??? 2 Editor -", new Color(1f, 0.05882353f, 0.05882353f, 1f) }, // 14
        };

		public static List<Color> EventLayerColors => new List<Color>
		{
			new Color(0.4039216f, 0.227451f, 0.4039216f, 0.5f),
			new Color(0.2470588f, 0.3176471f, 0.2470588f, 0.5f),
			new Color(0.1294118f, 0.5882353f, 0.1294118f, 0.5f),
			new Color(0.01176471f, 0.6627451f, 0.01176471f, 0.5f),
			new Color(0f, 0.7372549f, 0f, 0.5f),
			new Color(0f, 0.5882353f, 0f, 0.5f),
			new Color(0.2980392f, 0.6862745f, 0.2980392f, 0.5f),
			new Color(0.4862745f, 0.7019608f, 0.4862745f, 0.5f),
			new Color(0.6862745f, 0.7058824f, 0.6862745f, 0.5f),
			new Color(1f, 0.7568628f, 1f, 0.5f),
			new Color(1f, 0.5960785f, 1f, 0.5f),
			new Color(0.7267f, 0.3796f, 0.7267f, 1f),
			new Color(0.6980392f, 0.1411765f, 0.6980392f, 1f),
			new Color(0.6980392f, 0.145098f, 0.6980392f, 1f),
		};

		public static bool EventsCore => ModCompatibility.mods.ContainsKey("EventsCore");

		public Transform eventCopies;
		public Dictionary<string, GameObject> uiDictionary = new Dictionary<string, GameObject>();

		public bool debug = false;

		#endregion

		public void Log(string str)
        {
			if (debug)
				Debug.Log(str);
        }

		public static void Init(EventEditor eventEditor) => eventEditor?.gameObject?.AddComponent<RTEventEditor>();

		void Awake()
        {
            inst = this;

			EventEditor.inst.EventColors = EventLayerColors;

			SetupCopies();
		}

		#region Deleting

		public string DeleteKeyframe(int _type, int _event)
		{
			if (_event != 0)
			{
				string result = string.Format("Event [{0}][{1}]", _type, _event);
				DataManager.inst.gameData.eventObjects.allEvents[_type].RemoveAt(_event);
				CreateEventObjects();
				EventManager.inst.updateEvents();
				SetCurrentEvent(_type, _type - 1);
				return result;
			}
			EditorManager.inst.DisplayNotification("Can't delete first Keyframe", 2f, EditorManager.NotificationType.Error, false);
			return "";
		}

		public IEnumerator DeleteKeyframes()
		{
			var strs = new List<string>();
			var list = SelectedKeyframes;
			foreach (var timelineObject in list)
            {
				strs.Add(timelineObject.ID);
            }

			ClearEventObjects();

			var allEvents = DataManager.inst.gameData.eventObjects.allEvents;
			for (int i = 0; i < allEvents.Count; i++)
            {
				allEvents[i].RemoveAll(x => strs.Contains(((EventKeyframe)x).id));
			}

			CreateEventObjects();
			EventManager.inst.updateEvents();

			yield break;
		}

		#endregion

		#region Copy / Paste

		public void CopyAllSelectedEvents()
		{
			copiedEventKeyframes.Clear();
			float num = float.PositiveInfinity;
			foreach (var keyframeSelection in SelectedKeyframes)
			{
				if (DataManager.inst.gameData.eventObjects.allEvents[keyframeSelection.Type][keyframeSelection.Index].eventTime < num)
					num = DataManager.inst.gameData.eventObjects.allEvents[keyframeSelection.Type][keyframeSelection.Index].eventTime;
			}
			foreach (var keyframeSelection2 in SelectedKeyframes)
			{
				int type = keyframeSelection2.Type;
				int index = keyframeSelection2.Index;
				var eventKeyframe = EventKeyframe.DeepCopy((EventKeyframe)DataManager.inst.gameData.eventObjects.allEvents[type][index]);
				eventKeyframe.eventTime -= num;
				copiedEventKeyframes.Add(new TimelineObject(eventKeyframe));
			}
		}

		public void PasteEvents()
		{
			if (copiedEventKeyframes.Count <= 0)
			{
				Debug.LogError($"{EditorPlugin.className}No copied event yet!");
				return;
			}
			foreach (var keyframeSelection in copiedEventKeyframes)
			{
				var eventKeyframe = EventKeyframe.DeepCopy(keyframeSelection.GetData<EventKeyframe>());
				//Debug.LogFormat($"Create Keyframe at {eventKeyframe.eventTime} - {eventKeyframe.eventValues[0]}");
				eventKeyframe.eventTime = EditorManager.inst.CurrentAudioPos + eventKeyframe.eventTime;
				if (SettingEditor.inst.SnapActive)
					eventKeyframe.eventTime = EditorManager.inst.SnapToBPM(eventKeyframe.eventTime);

				DataManager.inst.gameData.eventObjects.allEvents[keyframeSelection.Type].Add(eventKeyframe);
			}

			UpdateEventOrder();
			CreateEventObjects();
			EventManager.inst.updateEvents();
		}

        #endregion

        #region Selection

        public void DeselectAllKeyframes()
        {
			if (SelectedKeyframes.Count > 0)
				foreach (var timelineObject in SelectedKeyframes)
					timelineObject.selected = false;
        }
		public void CreateNewEventObject(int _kind = 0)
		{
			CreateNewEventObject(EditorManager.inst.CurrentAudioPos, _kind);
		}

		public void CreateNewEventObject(float __0, int __1)
		{
            BaseEventKeyframe eventKeyframe = null;

			if (AllEvents[__1].Count != 0)
			{
				int num = DataManager.inst.gameData.eventObjects.allEvents[__1].FindLastIndex(x => x.eventTime <= __0);
				eventKeyframe = EventKeyframe.DeepCopy((EventKeyframe)DataManager.inst.gameData.eventObjects.allEvents[__1][num]);
			}
			else
			{
				eventKeyframe = new EventKeyframe
				{ eventTime = AudioManager.inst.CurrentAudioSource.time, eventValues = new float[9] };
			}

			eventKeyframe.eventTime = __0;
			if (__1 == 2 && ResetRotation)
				eventKeyframe.SetEventValues(new float[1]);

			DataManager.inst.gameData.eventObjects.allEvents[__1].Add(eventKeyframe);

			UpdateEventOrder();

			EventManager.inst.updateEvents();
			CreateEventObjects();
			SetCurrentEvent(__1, DataManager.inst.gameData.eventObjects.allEvents[__1].IndexOf(eventKeyframe));
		}

		public void NewKeyframeFromTimeline(int _type)
		{
			float timeTmp = EditorManager.inst.GetTimelineTime(0f);
			int num = DataManager.inst.gameData.eventObjects.allEvents[_type].FindLastIndex(x => x.eventTime <= timeTmp);
			Debug.Log("Prior Index: " + num);
			var eventKeyframe = EventKeyframe.DeepCopy((EventKeyframe)DataManager.inst.gameData.eventObjects.allEvents[_type][num], true);
			eventKeyframe.eventTime = timeTmp;

			if (_type == 2)
				eventKeyframe.SetEventValues(new float[1]);

			DataManager.inst.gameData.eventObjects.allEvents[_type].Add(eventKeyframe);

			UpdateEventOrder();

			EventManager.inst.updateEvents();
			CreateEventObjects();
			SetCurrentEvent(_type, DataManager.inst.gameData.eventObjects.allEvents[_type].IndexOf(eventKeyframe));
		}

		public void AddSelectedEvent(int type, int index)
		{
			if (!RTEditor.inst.timelineKeyframes.Has(x => x.Type == type && x.Index == index))
				CreateEventObjects();

			RTEditor.inst.timelineKeyframes.Find(x => x.Type == type && x.Index == index).selected = true;

			EventEditor.inst.currentEventType = type;
			EventEditor.inst.currentEvent = index;
			if (SelectedKeyframes.Count > 1 && EditorManager.inst.ActiveDialogs.Find(x => x.Name == "Multi Keyframe Editor") == null)
			{
				EditorManager.inst.ClearDialogs(Array.Empty<EditorManager.EditorDialog.DialogType>());
				EditorManager.inst.ShowDialog("Multi Keyframe Editor", false);
				RenderEventObjects();
				Debug.LogFormat($"{EditorPlugin.className}Add keyframe to selection -> [{type}] - [{index}]");
				return;
			}
			//OpenDialog();
		}

		public void SetCurrentEvent(int type, int index)
		{
			DeselectAllKeyframes();
			AddSelectedEvent(type, index);
			EventEditor.inst.currentEventType = type;
			EventEditor.inst.currentEvent = index;
			RenderEventObjects();
			OpenDialog();
		}

		#endregion

		#region Timeline Objects

		int lastLayer;

		public void ClearEventObjects()
		{
			foreach (var kf in RTEditor.inst.timelineKeyframes)
			{
				kf.selected = false;
				Destroy(kf.GameObject);
			}

			RTEditor.inst.timelineKeyframes.Clear();
		}

		public void CreateEventObjects()
        {
			var eventEditor = EventEditor.inst;

			var list = RTEditor.inst.timelineKeyframes.Clone();

			foreach (var kf in RTEditor.inst.timelineKeyframes)
			{
				kf.selected = false;
				Destroy(kf.GameObject);
			}

			RTEditor.inst.timelineKeyframes.Clear();

			eventEditor.eventDrag = false;

			for (int type = 0; type < AllEvents.Count; type++)
			{
				for (int index = 0; index < AllEvents[type].Count; index++)
				{
					var eventKeyframe = AllEvents[type][index];
					var gameObject = Instantiate(eventEditor.TimelinePrefab);
					gameObject.name = "new keyframe - " + (type % EventLimit).ToString();
					gameObject.transform.SetParent(eventEditor.EventHolders.transform.GetChild(type % EventLimit));
					gameObject.transform.localScale = Vector3.one;

					var image = gameObject.transform.GetChild(0).GetComponent<Image>();

					var kf = new TimelineObject((EventKeyframe)eventKeyframe);
					kf.Type = type;
					kf.Index = index;
					kf.GameObject = gameObject;
					kf.Image = image;

					if (list.Has(x => x.Type == type && x.Index == index) && lastLayer == RTEditor.inst.Layer)
						kf.selected = list.Find(x => x.Type == type && x.Index == index).selected;

					var triggers = gameObject.GetComponent<EventTrigger>().triggers;

					triggers.Clear();
					triggers.Add(TriggerHelper.CreateEventObjectTrigger(eventEditor, type, index));
					triggers.Add(TriggerHelper.CreateEventStartDragTrigger(eventEditor, type, index));
					triggers.Add(TriggerHelper.CreateEventEndDragTrigger());

					RTEditor.inst.timelineKeyframes.Add(kf);
				}
			}

			list.Clear();
			list = null;

			lastLayer = RTEditor.inst.Layer;

			RenderEventObjects();
			EventManager.inst.updateEvents();
		}

        public void RenderEventObjects()
		{
			var eventEditor = EventEditor.inst;
			for (int type = 0; type < AllEvents.Count; type++)
			{
				for (int index = 0; index < AllEvents[type].Count; index++)
				{
					var eventKeyframe = AllEvents[type][index];
					float eventTime = eventKeyframe.eventTime;
					int baseUnit = EditorManager.BaseUnit;

					var kf = RTEditor.inst.timelineKeyframes.Find(x => x.Type == type && x.Index == index);

					if (kf)
					{
						int limit = type / EventLimit;

						if (limit == RTEditor.inst.Layer)
						{
							((RectTransform)kf.GameObject.transform).anchoredPosition = new Vector2(eventTime * EditorManager.inst.Zoom - baseUnit / 2, 0.0f);

							kf.GameObject.SetActive(true);
						}
						else
							kf.GameObject.SetActive(false);
					}
				}
			}
		}

		#endregion

		#region Generate UI

		public void OpenDialog()
		{
			EditorManager.inst.ClearDialogs(Array.Empty<EditorManager.EditorDialog.DialogType>());
			EditorManager.inst.SetDialogStatus("Event Editor", true, true);

			if (EventEditor.inst.dialogRight.childCount > EventEditor.inst.currentEventType)
			{
				Debug.Log($"{EventEditor.inst.className}Dialog: {EventEditor.inst.dialogRight.GetChild(EventEditor.inst.currentEventType).name}");
				LSHelpers.SetActiveChildren(EventEditor.inst.dialogRight, false);
				EventEditor.inst.dialogRight.GetChild(EventEditor.inst.currentEventType).gameObject.SetActive(true);
				RenderEventsDialog();
				RenderEventObjects();
			}
			else
            {
				Debug.LogError($"{EventEditor.inst.className}Keyframe Type {EventEditor.inst.currentEventType} does not exist.");
            }
		}

		void SetupCopies()
		{
			if (!EventsCore)
				return;

			var gameObject = new GameObject("UI Dictionary");
			eventCopies = gameObject.transform;

			var uiCopy = Instantiate(EventEditor.inst.dialogRight.Find("grain").gameObject);
			uiCopy.transform.SetParent(eventCopies);

			for (int i = 8; i < 14; i++)
			{
				Destroy(uiCopy.transform.GetChild(i).gameObject);
			}

			uiDictionary.Add("UI Copy", uiCopy);

			var move = EventEditor.inst.dialogRight.GetChild(0);

			// Label Parent (includes two labels, can be set to any number using GenerateLabels)
			SetupCopy(move.GetChild(8).gameObject, eventCopies, "Label");
			SetupCopy(move.GetChild(9).gameObject, eventCopies, "Vector2");

			var single = Instantiate(move.GetChild(9).gameObject);

			single.transform.SetParent(eventCopies);
			Destroy(single.transform.GetChild(1).gameObject);

			uiDictionary.Add("Single", single);

			// Vector3
			{
				var vector3 = Instantiate(move.GetChild(9).gameObject);
				var z = Instantiate(vector3.transform.GetChild(1));
				z.name = "z";
				z.transform.SetParent(vector3.transform);
				z.transform.localScale = Vector3.one;

				vector3.transform.SetParent(eventCopies);
				vector3.transform.localScale = Vector3.one;

				uiDictionary.Add("Vector3", vector3);
			}

			// Vector4
			{
				var vector4 = Instantiate(move.GetChild(9).gameObject);
				var w = Instantiate(vector4.transform.GetChild(1));
				w.name = "w";
				w.transform.SetParent(vector4.transform);
				w.transform.localScale = Vector3.one;

				vector4.transform.SetParent(eventCopies);
				vector4.transform.localScale = Vector3.one;

				uiDictionary.Add("Vector4", vector4);
			}

			// Color
			{
				//var colorButtons = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/color/color");
				var colorButtons = ObjEditor.inst.KeyframeDialogs[3].transform.Find("color").gameObject;
				var colors = Instantiate(colorButtons);
				colors.transform.SetParent(eventCopies);
				colors.transform.localScale = Vector3.one;
				for (int i = 1; i < colors.transform.childCount; i++)
                {
					Destroy(colors.transform.GetChild(i).gameObject);
                }

				uiDictionary.Add("Colors", colors);
			}

            // Bool
            {
				var boolean = Instantiate(EventEditor.inst.dialogRight.Find("grain/colored").gameObject);
				boolean.transform.SetParent(eventCopies);
				boolean.transform.localScale = Vector3.one;

				uiDictionary.Add("Bool", boolean);
            }

			GenerateEventDialogs();
		}

		void SetupCopy(GameObject gameObject, Transform parent, string name)
        {
			var copy = Instantiate(gameObject);
			copy.transform.SetParent(parent);
			copy.transform.localScale = Vector3.one;
			uiDictionary.Add(name, copy);
        }

		void GenerateLabels(Transform parent, params string[] labels)
        {
			LSHelpers.DeleteChildren(parent);

			var label = uiDictionary["Label"].transform.GetChild(0).gameObject;
			for (int i = 0; i < labels.Length; i++)
			{
				var l = Instantiate(label);
				l.name = "label";
				l.transform.SetParent(parent);
				l.transform.localScale = Vector3.one;
				l.transform.GetComponent<Text>().text = labels[i];
			}
        }

		Dictionary<string, GameObject> GenerateUIElement(string name, string toCopy, Transform parent, int index, params string[] labels)
        {
			if (!uiDictionary.ContainsKey(toCopy))
				return null;

			var l = Instantiate(uiDictionary["Label"]);
			l.name = "label";
			l.transform.SetParent(parent);
			l.transform.localScale = Vector3.one;
			l.transform.SetSiblingIndex(index);
			GenerateLabels(l.transform, labels);

			var copy = Instantiate(uiDictionary[toCopy]);
			copy.name = name;
			copy.transform.SetParent(parent);
			copy.transform.localScale = Vector3.one;
			copy.transform.SetSiblingIndex(index + 1);

			return new Dictionary<string, GameObject>()
            {
				{ "Label", l },
				{ "UI", copy }
            };
        }

		GameObject GenerateEventDialog(string name)
		{
			var dialog = Instantiate(uiDictionary["UI Copy"]);
			dialog.transform.SetParent(EventEditor.inst.dialogRight);
			dialog.transform.localScale = Vector3.one;
			dialog.name = name;
			return dialog;
		}

		GameObject SetupColorButtons(string name, string label, Transform parent, int index, List<Toggle> toggles)
		{
			var colors = GenerateUIElement(name, "Colors", parent, index, label);
			var colorsObject = colors["UI"];

			for (int i = 0; i < colorsObject.transform.childCount; i++)
			{
				GameObject toggle;
				if (!colorsObject.transform.Find((i - 1).ToString()))
				{
					toggle = Instantiate(colorsObject.transform.GetChild(colorsObject.transform.childCount - 1).gameObject);
					toggle.transform.SetParent(colorsObject.transform);
					toggle.transform.localScale = Vector3.one;
				}
				else
					toggle = colorsObject.transform.Find((i - 1).ToString()).gameObject;

				toggles.Add(toggle.GetComponent<Toggle>());
			}

			return colorsObject;
		}

		void GenerateEventDialogs()
		{
			Log($"{EventEditor.inst.className}Modifying Shake Event");
			var shake = EventEditor.inst.dialogRight.Find("shake");
            {
				var direction = GenerateUIElement("direction", "Vector2", shake, 10, "Direction X", "Direction Y");
            }

			Log($"{EventEditor.inst.className}Modifying Bloom Event");
			var bloom = EventEditor.inst.dialogRight.Find("bloom");
            {
				var diffusion = GenerateUIElement("diffusion", "Single", bloom, 10, "Diffusion");
				var threshold = GenerateUIElement("threshold", "Single", bloom, 12, "Threshold");
				var ratio = GenerateUIElement("anamorphic ratio", "Single", bloom, 14, "Anamorphic Ratio");
				var colors = SetupColorButtons("colors", "Colors", bloom, 16, bloomColorButtons);
			}

			Log($"{EventEditor.inst.className}Modifying Vignette Event");
			var vignette = EventEditor.inst.dialogRight.Find("vignette");
			{
				var colors = SetupColorButtons("colors", "Colors", vignette, 16, vignetteColorButtons);
			}

			Log($"{EventEditor.inst.className}Modifying Lens Event");
			var lens = EventEditor.inst.dialogRight.Find("lens");
            {
				var center = GenerateUIElement("center", "Vector2", lens, 10, "Center X", "Center Y");
				var intensity = GenerateUIElement("intensity", "Vector2", lens, 12, "Intensity X", "Intensity Y");
				var scale = GenerateUIElement("scale", "Single", lens, 14, "Scale");
            }

			Log($"{EventEditor.inst.className}Generating ColorGrading Event");
			var colorGrading = GenerateEventDialog("colorgrading");
			{
				var hueShift = GenerateUIElement("hueshift", "Single", colorGrading.transform, 8, "Hueshift");
				var contrast = GenerateUIElement("contrast", "Single", colorGrading.transform, 10, "Contrast");
				var gamma = GenerateUIElement("gamma", "Vector4", colorGrading.transform, 12, "Gamma X", "Gamma Y", "Gamma Z", "Gamma W");
				var saturation = GenerateUIElement("saturation", "Single", colorGrading.transform, 12, "Saturation");
				var temperature = GenerateUIElement("temperature", "Single", colorGrading.transform, 14, "Temperature");
				var tint = GenerateUIElement("tint", "Single", colorGrading.transform, 16, "Tint");
			}

			Log($"{EventEditor.inst.className}Generating Ripples Event");
			var ripples = GenerateEventDialog("ripples");
			{
				var strength = GenerateUIElement("strength", "Single", ripples.transform, 8, "Strength");
				var speed = GenerateUIElement("speed", "Single", ripples.transform, 10, "Speed");
				var distance = GenerateUIElement("distance", "Single", ripples.transform, 12, "Distance");
				var size = GenerateUIElement("size", "Vector2", ripples.transform, 14, "Height", "Width");
			}

			Log($"{EventEditor.inst.className}Generating RadialBlur Event");
			var radialBlur = GenerateEventDialog("radialblur");
            {
				var intensity = GenerateUIElement("intensity", "Single", radialBlur.transform, 8, "Intensity");
				var iterations = GenerateUIElement("iterations", "Single", radialBlur.transform, 10, "Iterations");
            }

            Log($"{EventEditor.inst.className}Generating ColorSplit Event");
			var colorSplit = GenerateEventDialog("colorsplit");
            {
				var offset = GenerateUIElement("offset", "Single", colorSplit.transform, 8, "Offset");
            }

			Log($"{EventEditor.inst.className}Generating Offset Event");
			var cameraOffset = GenerateEventDialog("camoffset");
            {
				var position = GenerateUIElement("position", "Vector2", cameraOffset.transform, 8, "Offset X", "Offset Y");
            }

			Log($"{EventEditor.inst.className}Generating Gradient Event");
			var gradient = GenerateEventDialog("gradient");
            {
				var intensity = GenerateUIElement("introt", "Vector2", gradient.transform, 8, "Intensity", "Rotation");
				var colorsTop = SetupColorButtons("colors1", "Colors Top", gradient.transform, 10, gradientColor1Buttons);
				var colorsBottom = SetupColorButtons("colors2", "Colors Top", gradient.transform, 12, gradientColor2Buttons);

				var modeLabel = Instantiate(intensity["Label"]);
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
			}

			Log($"{EventEditor.inst.className}Generating DoubleVision Event");
			var doubleVision = GenerateEventDialog("doublevision");
            {
				var intensity = GenerateUIElement("intensity", "Single", doubleVision.transform, 8, "Intensity");
            }

			Log($"{EventEditor.inst.className}Generating ScanLines Event");
			var scanLines = GenerateEventDialog("scanlines");
            {
                var intensity = GenerateUIElement("intensity", "Single", scanLines.transform, 8, "Intensity");
                var amount = GenerateUIElement("amount", "Single", scanLines.transform, 10, "Amount Horizontal");
                var speed = GenerateUIElement("speed", "Single", scanLines.transform, 12, "Speed");
            }

			Log($"{EventEditor.inst.className}Generating Blur Event");
			var blur = GenerateEventDialog("blur");
            {
				var intensity = GenerateUIElement("intensity", "Single", blur.transform, 8, "Intensity");
				var iterations = GenerateUIElement("iterations", "Single", blur.transform, 10, "Iterations");
            }

			Log($"{EventEditor.inst.className}Generating Pixelize Event");
			var pixelize = GenerateEventDialog("pixelize");
            {
				var amount = GenerateUIElement("amount", "Single", pixelize.transform, 8, "Amount");
			}

			Log($"{EventEditor.inst.className}Generating BG Event");
			var bg = GenerateEventDialog("bg");
            {
				var colors = SetupColorButtons("colors", "Colors", bg.transform, 8, bgColorButtons);
            }

			Log($"{EventEditor.inst.className}Generating Invert Event");
			var invert = GenerateEventDialog("invert");
            {
				var intensity = GenerateUIElement("amount", "Single", invert.transform, 8, "Invert Amount");
            }

			Log($"{EventEditor.inst.className}Generating Timeline Event");
			var timeline = GenerateEventDialog("timeline");
            {
                var active = GenerateUIElement("active", "Bool", timeline.transform, 8, "Active");
				active["UI"].transform.Find("Text").GetComponent<Text>().text = "Active";

				var position = GenerateUIElement("position", "Vector2", timeline.transform, 10, "Position X", "Position Y");
				var scale = GenerateUIElement("scale", "Vector2", timeline.transform, 12, "Scale X", "Scale Y");
				var rotation = GenerateUIElement("rotation", "Single", timeline.transform, 14, "Rotation");
				var colors = SetupColorButtons("colors", "Colors", timeline.transform, 16, timelineColorButtons);
			}

			Log($"{EventEditor.inst.className}Generating Player Event");
			var player = GenerateEventDialog("player");
            {
				var active = GenerateUIElement("active", "Bool", player.transform, 8, "Active");
				active["UI"].transform.Find("Text").GetComponent<Text>().text = "Active";

				var moveable = GenerateUIElement("move", "Bool", player.transform, 10, "Can Move");
				active["UI"].transform.Find("Text").GetComponent<Text>().text = "Moveable";

				// I need to change Velocity to just be position.
				//var position = GenerateUIElement("position", "Vector2", player.transform, 12, "Position X", "Position Y");
				var position = GenerateUIElement("position", "Vector2", player.transform, 12, "Velocity", "Rotation");

				//var rotation = GenerateUIElement("rotation", "Single", player.transform, 14, "Rotation");
			}

			Log($"{EventEditor.inst.className}Generating Follow Player Event");
			var follow = GenerateEventDialog("follow");
			{
				var active = GenerateUIElement("active", "Bool", follow.transform, 8, "Active");
				active["UI"].transform.Find("Text").GetComponent<Text>().text = "Active";

				var moveable = GenerateUIElement("move", "Bool", follow.transform, 10, "Move Enabled");
				moveable["UI"].transform.Find("Text").GetComponent<Text>().text = "Move";

				var rotateable = GenerateUIElement("rotate", "Bool", follow.transform, 12, "Rotate Enabled");
				rotateable["UI"].transform.Find("Text").GetComponent<Text>().text = "Rotate";

				var position = GenerateUIElement("position", "Vector2", follow.transform, 14, "Sharpness", "Offset");

				var limitHorizontal = GenerateUIElement("limit horizontal", "Vector2", follow.transform, 16, "Limit Left", "Limit Right");
				var limitVertical = GenerateUIElement("limit vertical", "Vector2", follow.transform, 18, "Limit Up", "Limit Down");

				var anchor = GenerateUIElement("anchor", "Single", follow.transform, 20, "Anchor");
			}

			Log($"{EventEditor.inst.className}Generating Audio Event");
			var audio = GenerateEventDialog("audio");
            {
				var pitchVol = GenerateUIElement("music", "Vector2", audio.transform, 8, "Pitch", "Volume");
            }
        }

        #endregion

        #region Dialogs

        public static void LogIncorrectFormat(string str) => Debug.LogError($"{EventEditor.inst.className}Event Value was not in correct format! String: {str}");

		public void RenderEventsDialog()
		{
			var __instance = EventEditor.inst;
			Debug.Log($"{__instance.className}Rendering Events Dialog");
			var eventManager = EventManager.inst;
			var dialogTmp = __instance.dialogRight.GetChild(__instance.currentEventType);
			__instance.dialogLeft.Find("theme").gameObject.SetActive(false);
			var time = dialogTmp.Find("time");
			var timeTime = dialogTmp.Find("time/time").GetComponent<InputField>();

			var currentKeyframe = DataManager.inst.gameData.eventObjects.allEvents[__instance.currentEventType][__instance.currentEvent];

			timeTime.onValueChanged.RemoveAllListeners();
			timeTime.text = currentKeyframe.eventTime.ToString("f3");

			bool isNotFirst = __instance.currentEvent != 0;
			timeTime.interactable = isNotFirst;

			TriggerHelper.SetInteractable(isNotFirst,
				timeTime,
				dialogTmp.Find("time/<<").GetComponent<Button>(),
				dialogTmp.Find("time/<").GetComponent<Button>(),
				dialogTmp.Find("time/>").GetComponent<Button>(),
				dialogTmp.Find("time/>>").GetComponent<Button>());

			if (isNotFirst)
			{
				timeTime.onValueChanged.AddListener(delegate (string val)
				{
					if (float.TryParse(val, out float num))
						__instance.SetEventStartTime(float.Parse(val));
					else
						LogIncorrectFormat(val);
				});

				TriggerHelper.IncreaseDecreaseButtons(timeTime, 0.1f, 10f, t: time);
				TriggerHelper.AddEventTrigger(time.gameObject, new List<EventTrigger.Entry>
				{
					TriggerHelper.ScrollDelta(timeTime, max: 1f)
				});
			}

			switch (__instance.currentEventType)
            {
                case 0: // Move
					{
						SetVector2InputField(dialogTmp, "position", 0, 1);
						break;
					}
				case 1: // Zoom
					{
						SetFloatInputField(dialogTmp, "zoom/x", 0, min: -9999f, max: 9999f);
                        break;
					}
				case 2: // Rotate
					{
						SetFloatInputField(dialogTmp, "rotation/x", 0, 5f, 3f);
						break;
					}
				case 3: // Shake
					{
						// Shake Intensity
						SetFloatInputField(dialogTmp, "shake/x", 0, min: 0f, max: 10f, allowNegative: false);

						// Shake Intensity X / Y
						if (EventsCore)
							SetVector2InputField(dialogTmp, "direction", 1, 2, -10f, 10f);
						break;
					}
				case 4: // Theme
					{
						var theme = dialogTmp.Find("theme-search").GetComponent<InputField>();

						theme.onValueChanged.RemoveAllListeners();
						theme.onValueChanged.AddListener(delegate (string val)
						{
							ThemeEditorManager.inst.RenderThemeContent(dialogTmp, val);
						});
						ThemeEditorManager.inst.RenderThemeContent(dialogTmp, theme.text);
						__instance.RenderThemePreview(dialogTmp);
						break;
					}
				case 5: // Chromatic
					{
						SetFloatInputField(dialogTmp, "chroma/x", 0, min: 0f, max: float.PositiveInfinity, allowNegative: false);

						break;
					}
				case 6: // Bloom
					{
						//Bloom Intensity
						SetFloatInputField(dialogTmp, "bloom/x", 0, max: 1280f, allowNegative: false);

						if (EventsCore)
						{
							// Bloom Diffusion
							SetFloatInputField(dialogTmp, "diffusion/x", 1, min: 1f, max: float.PositiveInfinity, allowNegative: false);

							// Bloom Threshold
							SetFloatInputField(dialogTmp, "threshold/x", 2, min: 0f, max: 1.4f, allowNegative: false);

							// Bloom Anamorphic Ratio
							SetFloatInputField(dialogTmp, "anamorphic ratio/x", 3, min: -1f, max: 1f);
							
							// Bloom Color
							SetListColor((int)currentKeyframe.eventValues[4], 4, bloomColorButtons, Color.white);
						}
						break;
					}
				case 7: // Vignette
					{
						// Vignette Intensity
						SetFloatInputField(dialogTmp, "intensity", 0, allowNegative: false);

						// Vignette Smoothness
						SetFloatInputField(dialogTmp, "smoothness", 1);

						// Vignette Rounded
						SetToggle(dialogTmp, "roundness/rounded", 2, 1, 0);

						// Vignette Roundness
						SetFloatInputField(dialogTmp, "roundness", 3, 0.01f, 10f, float.NegativeInfinity, 1.2f);

						// Vignette Center
						SetVector2InputField(dialogTmp, "position", 4, 5);

						// Vignette Color
						if (EventsCore)
							SetListColor((int)currentKeyframe.eventValues[6], 6, vignetteColorButtons, Color.black);

						break;
					}
				case 8: // Lens
					{
						// Lens Intensity
						SetFloatInputField(dialogTmp, "lens/x", 0, 1f, 10f, -100f, 100f);

						if (EventsCore)
						{
							// Lens Center X / Y
							SetVector2InputField(dialogTmp, "center", 1, 2);

							// Lens Intensity X / Y
							SetVector2InputField(dialogTmp, "intensity", 3, 4);

							// Lens Scale
							SetFloatInputField(dialogTmp, "scale/x", 5, 0.1f, 10f, 0.001f, float.PositiveInfinity, allowNegative: false);
						}
						break;
					}
				case 9: // Grain
					{
						// Grain Intensity
						SetFloatInputField(dialogTmp, "intensity", 0, 0.1f, 10f, 0f, float.PositiveInfinity, allowNegative: false);

						// Grain Colored
						SetToggle(dialogTmp, "colored", 1, 1, 0);

						// Grain Size
						SetFloatInputField(dialogTmp, "size", 2, 0.1f, 10f, 0f, float.PositiveInfinity, allowNegative: false);

						break;
					}
				case 10: // ColorGrading
					{
						// ColorGrading Hueshift
						SetFloatInputField(dialogTmp, "hueshift/x", 0, 0.1f, 10f);

						// ColorGrading Contrast
						SetFloatInputField(dialogTmp, "contrast/x", 1, 1f, 10f);

						// ColorGrading Gamma (Not sure how to do the UI for this since it's literally four values)
						// Gamma X = 2
						// Gamma Y = 3
						// Gamma Z = 4
						// Gamma W = 5

						SetFloatInputField(dialogTmp, "gamma/x", 2);
						SetFloatInputField(dialogTmp, "gamma/y", 3);
						SetFloatInputField(dialogTmp, "gamma/z", 4);
						SetFloatInputField(dialogTmp, "gamma/w", 5);

						// ColorGrading Saturation
						SetFloatInputField(dialogTmp, "saturation/x", 6, 1f, 10f);

						// ColorGrading Temperature
						SetFloatInputField(dialogTmp, "temperature/x", 7, 1f, 10f);

						// ColorGrading Tint
						SetFloatInputField(dialogTmp, "tint/x", 8, 1f, 10f);
						break;
					}
				case 11: // Ripples
					{
						// Ripples Strength
						SetFloatInputField(dialogTmp, "strength/x", 0);

						// Ripples Speed
						SetFloatInputField(dialogTmp, "speed/x", 1);

						// Ripples Distance
						SetFloatInputField(dialogTmp, "distance/x", 2, 0.1f, 10f, 0.001f, float.PositiveInfinity);

						SetVector2InputField(dialogTmp, "size", 3, 4);

						break;
					}
				case 12: // RadialBlur
					{
						// RadialBlur Intensity
						SetFloatInputField(dialogTmp, "intensity/x", 0);

						// RadialBlur Iterations
						SetIntInputField(dialogTmp, "iterations/x", 1, 1, 1, 20);

						break;
					}
				case 13: // ColorSplit
					{
						// ColorSplit Offset
						SetFloatInputField(dialogTmp, "offset/x", 0);

						break;
					}
				case 14: // Cam Offset
					{
						SetVector2InputField(dialogTmp, "position", 0, 1);

						break;
					}
				case 15: // Gradient
					{
						// Gradient Intensity / Rotation (Had to put them together due to mode going over the timeline lol)
						SetVector2InputField(dialogTmp, "introt", 0, 1);

						// Gradient Color Top
						SetListColor((int)currentKeyframe.eventValues[2], 2, gradientColor1Buttons, new Color(0f, 0.8f, 0.56f, 0.5f));

						// Gradient Color Bottom
						SetListColor((int)currentKeyframe.eventValues[3], 3, gradientColor2Buttons, new Color(0.81f, 0.37f, 1f, 0.5f));

						// Gradient Mode (No separate method required atm)
						{
							var drp = dialogTmp.Find("mode").GetComponent<Dropdown>();
							drp.onValueChanged.RemoveAllListeners();
							drp.value = (int)currentKeyframe.eventValues[4];
							drp.onValueChanged.AddListener(delegate (int _val)
							{
								currentKeyframe.eventValues[4] = _val;
								EventManager.inst.updateEvents();
							});
						}

						break;
					}
				case 16: // DoubleVision
					{
						// DoubleVision Intensity
						SetFloatInputField(dialogTmp, "intensity", 0);

						break;
					}
				case 17: // ScanLines
					{
						// ScanLines Intensity
						SetFloatInputField(dialogTmp, "intensity", 0);

						// ScanLines Amount
						SetFloatInputField(dialogTmp, "amount", 1);

						// ScanLines Speed
						SetFloatInputField(dialogTmp, "speed", 2);
						break;
					}
				case 18: // Blur
					{
						//Blur Amount
						SetFloatInputField(dialogTmp, "intensity", 0);

						//Blur Iterations
						SetIntInputField(dialogTmp, "iterations", 1, 1, 1, 12);

						break;
					}
				case 19: // Pixelize
					{
						//Pixelize
						SetFloatInputField(dialogTmp, "amount", 0, 0.1f, 10f, 0f, 0.99999f);

						break;
					}
				case 20: // BG
					{
						SetListColor((int)currentKeyframe.eventValues[0], 0, bgColorButtons, GameManager.inst.LiveTheme.backgroundColor);

						break;
					}
				case 21: // Invert
					{
						//Invert Amount
						SetFloatInputField(dialogTmp, "amount", 0, 0.1f, 10f, 0f, 1f);

						break;
					}
				case 22: // Timeline
					{
						// Timeline Active
						SetToggle(dialogTmp, "active", 0, 0, 1);

						// Timeline Position
						SetVector2InputField(dialogTmp, "position", 1, 2);

						// Timeline Scale
						SetVector2InputField(dialogTmp, "scale", 3, 4);

						// Timeline Rotation
						SetFloatInputField(dialogTmp, "rotation", 5, 15f, 3f);

						// Timeline Color
						SetListColor((int)currentKeyframe.eventValues[6], 6, timelineColorButtons, GameManager.inst.LiveTheme.guiColor);

						break;
					}
				case 23: // Player
					{
						// Player Active
						SetToggle(dialogTmp, "active", 0, 0, 1);

						// Player Moveable
						SetToggle(dialogTmp, "moveable", 1, 0, 1);

						// Player Velocity
						SetFloatInputField(dialogTmp, "position/x", 2, 0.1f, 10f, 0f, float.PositiveInfinity);

						// Player Rotation
						SetFloatInputField(dialogTmp, "position/y", 3, 15f, 3f);

						break;
					}
				case 24: // Follow Player
					{
						// Follow Player Active
						SetToggle(dialogTmp, "active", 0, 1, 0);

						// Follow Player Move
						SetToggle(dialogTmp, "move", 1, 1, 0);

						// Follow Player Rotate
						SetToggle(dialogTmp, "rotate", 2, 1, 0);

						// Follow Player Sharpness
						SetFloatInputField(dialogTmp, "position/x", 3, 0.1f, 10f, 0.001f, 1f);

						// Follow Player Offset
						SetFloatInputField(dialogTmp, "position/y", 4);

						// Follow Player Limit Left
						SetFloatInputField(dialogTmp, "limit horizontal/x", 5);

						// Follow Player Limit Right
						SetFloatInputField(dialogTmp, "limit horizontal/y", 6);

						// Follow Player Limit Up
						SetFloatInputField(dialogTmp, "limit vertical/x", 7);

						// Follow Player Limit Down
						SetFloatInputField(dialogTmp, "limit vertical/y", 8);

						// Follow Player Anchor
						SetFloatInputField(dialogTmp, "anchor/x", 9, 0.1f, 10f);

						break;
					}
				case 25: // Audio
					{
						// Audio Pitch
						SetFloatInputField(dialogTmp, "music/x", 0, 0.1f, 10f, 0.001f, 10f, allowNegative: false);

						// Audio Volume
						SetFloatInputField(dialogTmp, "music/y", 1, 0.1f, 10f, 0f, 1f, allowNegative: false);

						break;
					}
			}

			// Curves
			{
				var curvesDropdown = dialogTmp.transform.Find("curves").GetComponent<Dropdown>();

				dialogTmp.transform.Find("curves_label").gameObject.SetActive(isNotFirst);
				curvesDropdown.gameObject.SetActive(isNotFirst);
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

				editJumpLeftLarge.interactable = isNotFirst;
				editJumpLeftLarge.onClick.RemoveAllListeners();
				editJumpLeftLarge.onClick.AddListener(delegate ()
				{
					__instance.UpdateEventOrder(false);
					__instance.SetCurrentEvent(__instance.currentEventType, 0);
				});

				editJumpLeft.interactable = isNotFirst;
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

				if (!isNotFirst)
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

				editJumpRight.interactable = (__instance.currentEvent != allEvents.Count - 1);
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
				editDelete.interactable = isNotFirst;
				editDelete.onClick.AddListener(delegate ()
				{
					__instance.DeleteEvent(__instance.currentEventType, __instance.currentEvent);
				});
			}

			RenderTitles(); // Probably need to change this to only render the singular title since that's all you see.
		}

		public void SetListColor(int value, int index, List<Toggle> toggles, Color defaultColor)
		{
			DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[index] = value;
			EventManager.inst.updateEvents();

			int num = 0;
			foreach (var toggle in toggles)
			{
				toggle.onValueChanged.RemoveAllListeners();

				toggle.isOn = num == value;

				toggle.image.color = num < 18 ? RTHelpers.BeatmapTheme.effectColors[num] : defaultColor;

				int tmpIndex = num;
				toggle.onValueChanged.AddListener(delegate (bool val)
				{
					SetListColor(tmpIndex, index, toggles, defaultColor);
				});
				num++;
			}
		}

		public void SetToggle(Transform dialogTmp, string name, int index, int onValue, int offValue)
		{
			var __instance = EventEditor.inst;
			var currentKeyframe = DataManager.inst.gameData.eventObjects.allEvents[__instance.currentEventType][__instance.currentEvent];

			var vignetteRounded = dialogTmp.Find(name).GetComponent<Toggle>();
			vignetteRounded.onValueChanged.RemoveAllListeners();
			vignetteRounded.isOn = currentKeyframe.eventValues[index] == onValue;
			vignetteRounded.onValueChanged.AddListener(delegate (bool val)
			{
				currentKeyframe.eventValues[index] = val ? onValue : offValue;
				EventManager.inst.updateEvents();
			});
		}

		public void SetFloatInputField(Transform dialogTmp, string name, int index, float increase = 0.1f, float multiply = 10f, float min = 0f, float max = 0f, bool allowNegative = true)
		{
			var __instance = EventEditor.inst;
			Debug.Log($"{__instance.className}Rendering Events Dialog");

			var currentKeyframe = DataManager.inst.gameData.eventObjects.allEvents[__instance.currentEventType][__instance.currentEvent];

			if (!dialogTmp.Find(name))
				return;

			var zoom = dialogTmp.Find($"{name}").GetComponent<InputField>();
			zoom.onValueChanged.RemoveAllListeners();

			zoom.text = currentKeyframe.eventValues[index].ToString();

			zoom.onValueChanged.AddListener(delegate (string val)
			{
				if (float.TryParse(val, out float num))
				{
					if (min != 0f && max != 0f)
						num = Mathf.Clamp(num, min, max);

					currentKeyframe.eventValues[index] = num;
					EventManager.inst.updateEvents();
				}
				else
					LogIncorrectFormat(val);
			});

			TriggerHelper.IncreaseDecreaseButtons(zoom, increase * multiply, multiply, min, max);
			TriggerHelper.AddEventTrigger(zoom.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(zoom, increase, multiply, min, max) });

			if (allowNegative && !zoom.gameObject.GetComponent<InputFieldSwapper>())
			{
				var ifh = zoom.gameObject.AddComponent<InputFieldSwapper>();
				ifh.Init(zoom, InputFieldSwapper.Type.Num);
			}
		}

		public void SetIntInputField(Transform dialogTmp, string name, int index, int increase = 1, int min = 0, int max = 0, bool allowNegative = true)
		{
			var __instance = EventEditor.inst;
			Debug.Log($"{__instance.className}Rendering Events Dialog");

			var currentKeyframe = DataManager.inst.gameData.eventObjects.allEvents[__instance.currentEventType][__instance.currentEvent];

			if (!dialogTmp.Find(name))
				return;

			var zoom = dialogTmp.Find($"{name}").GetComponent<InputField>();
			zoom.onValueChanged.RemoveAllListeners();

			zoom.text = currentKeyframe.eventValues[index].ToString();

			zoom.onValueChanged.AddListener(delegate (string val)
			{
				if (int.TryParse(val, out int num))
				{
					if (min != 0 && max != 0)
						num = Mathf.Clamp(num, min, max);

					currentKeyframe.eventValues[index] = num;
					EventManager.inst.updateEvents();
				}
				else
					LogIncorrectFormat(val);
			});

			TriggerHelper.IncreaseDecreaseButtonsInt(zoom, increase * 10, min, max);
			TriggerHelper.AddEventTrigger(zoom.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDeltaInt(zoom, increase, min, max) });

			if (allowNegative && !zoom.gameObject.GetComponent<InputFieldSwapper>())
			{
				var ifh = zoom.gameObject.AddComponent<InputFieldSwapper>();
				ifh.Init(zoom, InputFieldSwapper.Type.Num);
			}
		}

		public void SetVector2InputField(Transform dialogTmp, string name, int xindex, int yindex, float min = 0f, float max = 0f, bool allowNegative = true)
		{
			var __instance = EventEditor.inst;
			Debug.Log($"{__instance.className}Rendering Events Dialog");

			var currentKeyframe = DataManager.inst.gameData.eventObjects.allEvents[__instance.currentEventType][__instance.currentEvent];

			if (!dialogTmp.Find(name) || !dialogTmp.Find($"{name}/x") || !dialogTmp.Find($"{name}/y"))
				return;

			var posX = dialogTmp.Find($"{name}/x").GetComponent<InputField>();
			var posY = dialogTmp.Find($"{name}/y").GetComponent<InputField>();

			posX.onValueChanged.RemoveAllListeners();
			posX.text = currentKeyframe.eventValues[xindex].ToString();
			posX.onValueChanged.AddListener(delegate (string val)
			{
				if (float.TryParse(val, out float num))
				{
					if (min != 0f && max != 0f)
						num = Mathf.Clamp(num, min, max);

					currentKeyframe.eventValues[xindex] = num;
					EventManager.inst.updateEvents();
				}
				else
					LogIncorrectFormat(val);
			});

			posY.onValueChanged.RemoveAllListeners();
			posY.text = currentKeyframe.eventValues[yindex].ToString();
			posY.onValueChanged.AddListener(delegate (string val)
			{
				if (float.TryParse(val, out float num))
				{
					if (min != 0f && max != 0f)
						num = Mathf.Clamp(num, min, max);

					currentKeyframe.eventValues[yindex] = num;
					EventManager.inst.updateEvents();
				}
				else
					LogIncorrectFormat(val);
			});

			TriggerHelper.IncreaseDecreaseButtons(posX, 1f, 10f, min, max);
			TriggerHelper.IncreaseDecreaseButtons(posY, 1f, 10f, min, max);
			var clampList = new List<float> { min, max };
			TriggerHelper.AddEventTrigger(posX.gameObject, new List<EventTrigger.Entry>
			{
				TriggerHelper.ScrollDelta(posX, 0.1f, 10f, min, max, true),
				TriggerHelper.ScrollDeltaVector2(posX, posY, 0.1f, 10f, clampList)
			});
			TriggerHelper.AddEventTrigger(posY.gameObject, new List<EventTrigger.Entry>
			{
				TriggerHelper.ScrollDelta(posY, 0.1f, 10f, min, max, true),
				TriggerHelper.ScrollDeltaVector2(posX, posY, 0.1f, 10f, clampList)
			});

			if (allowNegative)
			{
				if (!posX.gameObject.GetComponent<InputFieldSwapper>())
				{
					var ifh = posX.gameObject.AddComponent<InputFieldSwapper>();
					ifh.Init(posX, InputFieldSwapper.Type.Num);
				}
				if (!posY.gameObject.GetComponent<InputFieldSwapper>())
				{
					var ifh = posY.gameObject.AddComponent<InputFieldSwapper>();
					ifh.Init(posY, InputFieldSwapper.Type.Num);
				}
			}
		}

        #endregion

        #region Rendering

		public void UpdateEventOrder()
        {
			var strs = new List<string>();
			foreach (var timelineObject in SelectedKeyframes)
				strs.Add(timelineObject.ID);

			ClearEventObjects();

			foreach (var list in DataManager.inst.gameData.eventObjects.allEvents)
            {
				list.OrderBy(x => x.eventTime);
            }

			CreateEventObjects();

			foreach (var timelineObject in RTEditor.inst.timelineKeyframes)
            {
				if (strs.Contains(timelineObject.ID))
					timelineObject.selected = true;
            }
        }

		void RenderTitles()
		{
			for (int i = 0; i < EventEditor.inst.dialogRight.childCount; i++)
			{
				var title = EventEditor.inst.dialogRight.GetChild(i).GetChild(0);
				title.GetChild(0).GetComponent<Image>().color = EventTitles.ElementAt(i).Value;
				title.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(17f, 0f);
				title.GetChild(1).GetComponent<Text>().text = EventTitles.ElementAt(i).Key;
			}
		}

		public void RenderLayerBins()
        {
			var eventLabels = EventEditor.inst.EventLabels;

			var layer = RTEditor.inst.Layer + 1;

			for (int i = 0; i < AllEvents.Count; i++)
			{
				int t = i % EventLimit;
				int num = layer * EventLimit;

				if (i < EventTypes.Length)
				{
					if (i < num && i >= num - EventLimit)
						eventLabels.transform.GetChild(t).GetChild(0).GetComponent<Text>().text = EventTypes[i];
				}
				else
					eventLabels.transform.GetChild(t).GetChild(0).GetComponent<Text>().text = "??? (No event yet)";
			}
		}

		#endregion
	}
}
