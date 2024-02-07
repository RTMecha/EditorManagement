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
		
		#region Variables

		public List<TimelineObject> SelectedKeyframes => RTEditor.inst.timelineKeyframes.FindAll(x => x.selected);

		public List<TimelineObject> copiedEventKeyframes = new List<TimelineObject>();

		public static List<List<BaseEventKeyframe>> AllEvents => DataManager.inst.gameData.eventObjects.allEvents;

		// Timeline will only ever have up to 15 "bins" and since the 15th bin is the checkpoints, we only need the first 14 bins.
		public const int EventLimit = 14;

		public static bool ResetRotation => RTEditor.GetEditorProperty("Rotation Event Keyframe Resets").GetConfigEntry<bool>().Value;

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
			"???", //"Video BG Parent",
			"???", //"Video BG",
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
			{ /*"- Video BG Parent Editor -"*/ "- ??? 1 Editor -", new Color(1f, 0.1490196f, 0.03529412f, 1f) }, // 13
			{ /*"- Video BG Editor -"*/ "- ??? 2 Editor -", new Color(1f, 0.05882353f, 0.05882353f, 1f) }, // 14
        };

		public static List<Color> EventLayerColors => new List<Color>
		{
			new Color(0.4039216f, 0.227451f, 0.7176471f, 0.5f),
			new Color(0.2470588f, 0.3176471f, 0.7098039f, 0.5f),
			new Color(0.1294118f, 0.5882353f, 0.9529412f, 0.5f),
			new Color(0.01176471f, 0.6627451f, 0.9568628f, 0.5f),
			new Color(0f, 0.7372549f, 0.8313726f, 0.5f),
			new Color(0f, 0.5882353f, 0.5333334f, 0.5f),
			new Color(0.2980392f, 0.6862745f, 0.3137255f, 0.5f),
			new Color(0.4862745f, 0.7019608f, 0.2588235f, 0.5f),
			new Color(0.6862745f, 0.7058824f, 0.1686275f, 0.5f),
			new Color(1f, 0.7568628f, 0.02745098f, 0.5f),
			new Color(1f, 0.5960785f, 0f, 0.5f),
			new Color(0.7267f, 0.3796f, 0f, 0.5f),
			new Color(0.6980392f, 0.1411765f, 0.06666667f, 0.5f),
			new Color(0.6980392f, 0.145098f, 0.145098f, 0.5f),
			new Color(0.3921569f, 0.7098039f, 0.9647059f, 0.5f),
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
			var count = list.Count;
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

			SetCurrentEvent(0, 0);

			EditorManager.inst.DisplayNotification($"Deleted Event Keyframes [ {count} ]", 1f, EditorManager.NotificationType.Success);

			yield break;
		}

		public IEnumerator DeleteKeyframes(List<TimelineObject> kfs)
		{
			var strs = new List<string>();
			var list = kfs;
			var count = list.Count;
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

			SetCurrentEvent(0, 0);

			EditorManager.inst.DisplayNotification($"Deleted Event Keyframes [ {count} ]", 1f, EditorManager.NotificationType.Success);

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
				var timelineObject = new TimelineObject(eventKeyframe);
				timelineObject.Type = type;
				copiedEventKeyframes.Add(timelineObject);
			}
		}

		public void PasteEvents(bool setTime = true) => PasteEvents(copiedEventKeyframes, setTime);

		public void PasteEvents(List<TimelineObject> kfs, bool setTime = true)
		{
			if (kfs.Count <= 0)
			{
				Debug.LogError($"{EditorPlugin.className}No copied event yet!");
				return;
			}

			RTEditor.inst.timelineKeyframes.ForEach(x => x.selected = false);

			foreach (var keyframeSelection in kfs)
			{
				var eventKeyframe = EventKeyframe.DeepCopy(keyframeSelection.GetData<EventKeyframe>());
				if (setTime)
				{
					eventKeyframe.eventTime = EditorManager.inst.CurrentAudioPos + eventKeyframe.eventTime;
					if (SettingEditor.inst.SnapActive)
						eventKeyframe.eventTime = RTEditor.SnapToBPM(eventKeyframe.eventTime);
				}

				var index = AllEvents[keyframeSelection.Type].FindIndex(x => x.eventTime > eventKeyframe.eventTime) - 1;
				if (index < 0)
					index = AllEvents[keyframeSelection.Type].Count;

				DataManager.inst.gameData.eventObjects.allEvents[keyframeSelection.Type].Insert(index, eventKeyframe);

				var kf = CreateEventObject(keyframeSelection.Type, index);
				RenderTimelineObject(kf);
				kf.selected = true;
				RTEditor.inst.timelineKeyframes.Add(kf);
			}

			//UpdateEventOrder();
			//CreateEventObjects();
			OpenDialog();
			EventManager.inst.updateEvents();
		}

        #endregion

        #region Selection

		public IEnumerator GroupSelectKeyframes(bool _add)
        {
			var list = RTEditor.inst.timelineKeyframes;

			if (!_add)
				DeselectAllKeyframes();

			list.Where(x => RTMath.RectTransformToScreenSpace(EditorManager.inst.SelectionBoxImage.rectTransform)
			.Overlaps(RTMath.RectTransformToScreenSpace(x.Image.rectTransform))).ToList().ForEach(delegate (TimelineObject x)
			{
				x.selected = true;
				x.timeOffset = 0f;
			});

			RenderEventObjects();
			OpenDialog();
			yield break;
		}

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


			if (SettingEditor.inst.SnapActive)
				__0 = RTEditor.SnapToBPM(__0);


			eventKeyframe.eventTime = __0;
			if (__1 == 2 && ResetRotation)
				eventKeyframe.SetEventValues(new float[1]);

			DataManager.inst.gameData.eventObjects.allEvents[__1].Add(eventKeyframe);

			UpdateEventOrder();

			EventManager.inst.updateEvents();
			CreateEventObjects();
			SetCurrentEvent(__1, DataManager.inst.gameData.eventObjects.allEvents[__1].IndexOf(eventKeyframe));
		}

		public float NewKeyframeOffset { get; set; } = -0.1f;
		public void NewKeyframeFromTimeline(int _type)
		{
			if (!(DataManager.inst.gameData.eventObjects.allEvents.Count > _type))
			{
				EditorManager.inst.DisplayNotification("Keyframe type doesn't exist!" + (ModCompatibility.mods.ContainsKey("EventsCore") ? "" : " If you want to have more events, then feel free to add EventsCore to your mods list."), 4f, EditorManager.NotificationType.Warning);
				return;
			}

			float timeTmp = EditorManager.inst.GetTimelineTime(NewKeyframeOffset);

			if (SettingEditor.inst.SnapActive)
				timeTmp = RTEditor.SnapToBPM(timeTmp);

			int num = DataManager.inst.gameData.eventObjects.allEvents[_type].FindLastIndex(x => x.eventTime <= timeTmp);
			Debug.Log($"{EventEditor.inst.className}Prior Index: {num}");

			EventKeyframe eventKeyframe;

			if (num < 0)
			{
				eventKeyframe = EventKeyframe.DeepCopy((EventKeyframe)GameData.DefaultKeyframes[_type]);
				eventKeyframe.eventTime = 0f;
			}
			else
            {
				eventKeyframe = EventKeyframe.DeepCopy((EventKeyframe)DataManager.inst.gameData.eventObjects.allEvents[_type][num], true);
				eventKeyframe.eventTime = timeTmp;

                if (_type == 2 && ResetRotation)
                    eventKeyframe.SetEventValues(new float[1]);
            }

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
			RenderEventObjects();
			OpenDialog();
		}

		public void SetCurrentEvent(int type, int index)
		{
			DeselectAllKeyframes();
			AddSelectedEvent(type, index);
			EventEditor.inst.currentEventType = type;
			EventEditor.inst.currentEvent = index;
			//RenderEventObjects();
			//OpenDialog();
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

			ClearEventObjects();

			eventEditor.eventDrag = false;

			for (int type = 0; type < AllEvents.Count; type++)
			{
				for (int index = 0; index < AllEvents[type].Count; index++)
				{
					var kf = CreateEventObject(type, index);

					RTEditor.inst.timelineKeyframes.Add(kf);
				}
			}

			lastLayer = RTEditor.inst.Layer;

			RenderEventObjects();
		}

		public TimelineObject CreateEventObject(int type, int index)
        {
			var eventKeyframe = AllEvents[type][index];

			var kf = new TimelineObject((EventKeyframe)eventKeyframe);
			kf.Type = type;
			kf.Index = index;
			kf.GameObject = EventGameObject(kf);
			kf.Image = kf.GameObject.transform.GetChild(0).GetComponent<Image>();

			TriggerHelper.AddEventTriggerParams(kf.GameObject,
				TriggerHelper.CreateEventObjectTrigger(EventEditor.inst, type, index),
				TriggerHelper.CreateEventStartDragTrigger(EventEditor.inst, type, index),
				TriggerHelper.CreateEventEndDragTrigger(),
				TriggerHelper.CreateEventSelectTrigger(kf));

			return kf;
		}

		public GameObject EventGameObject(TimelineObject kf)
        {
			var gameObject = EventEditor.inst.TimelinePrefab.Duplicate(EventEditor.inst.EventHolders.transform.GetChild(kf.Type % EventLimit), $"keyframe - {kf.Type}");
			return gameObject;
        }

        public void RenderEventObjects()
		{
			var eventEditor = EventEditor.inst;
			for (int type = 0; type < AllEvents.Count; type++)
			{
				for (int index = 0; index < AllEvents[type].Count; index++)
				{
					var kf = RTEditor.inst.timelineKeyframes.Find(x => x.Type == type && x.Index == index);

					if (!kf)
						kf = RTEditor.inst.timelineKeyframes.Find(x => x.ID == (AllEvents[type][index] as EventKeyframe).id);

					if (!kf)
					{
						kf = CreateEventObject(type, index);
						RTEditor.inst.timelineKeyframes.Add(kf);
					}
					if (!kf.GameObject)
						kf.GameObject = EventGameObject(kf);
					RenderTimelineObject(kf);
				}
			}
		}

		public void RenderTimelineObject(TimelineObject kf)
		{
			if (AllEvents[kf.Type].Has(x => (x as EventKeyframe).id == kf.ID))
				kf.Index = AllEvents[kf.Type].FindIndex(x => (x as EventKeyframe).id == kf.ID);

			var eventKeyframe = AllEvents[kf.Type][kf.Index];
			float eventTime = eventKeyframe.eventTime;
			int baseUnit = EditorManager.BaseUnit;
			int limit = kf.Type / EventLimit;

			if (limit == RTEditor.inst.Layer)
			{
				((RectTransform)kf.GameObject.transform).anchoredPosition = new Vector2(eventTime * EditorManager.inst.Zoom - baseUnit / 2, 0.0f);
				// Fixes the keyframes being off center.
				((RectTransform)kf.GameObject.transform).pivot = new Vector2(0f, 1f);

				kf.Image.sprite =
					RTEditor.GetKeyframeIcon(eventKeyframe.curveType,
					AllEvents[kf.Type].Count > kf.Index + 1 ?
					AllEvents[kf.Type][kf.Index + 1].curveType : DataManager.inst.AnimationList[0]);
			}
		}

		#endregion

		#region Generate UI

		public void OpenDialog()
		{
			if (SelectedKeyframes.Count > 1 && !SelectedKeyframes.All(x => x.Type == SelectedKeyframes.Min(y => y.Type)))
			{
				EditorManager.inst.ClearDialogs();
				EditorManager.inst.ShowDialog("Multi Keyframe Editor", false);
				RenderMultiEventsDialog();
			}
			else if (SelectedKeyframes.Count > 0)
			{
				EditorManager.inst.ClearDialogs();
				EditorManager.inst.SetDialogStatus("Event Editor", true);

				EventEditor.inst.currentEventType = SelectedKeyframes[0].Type;
				EventEditor.inst.currentEvent = SelectedKeyframes[0].Index;

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
			else
            {
				CheckpointEditor.inst.SetCurrentCheckpoint(0);
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

			while (uiCopy.transform.childCount > 8)
				DestroyImmediate(uiCopy.transform.GetChild(uiCopy.transform.childCount - 1).gameObject);

			//for (int i = 8; i < 14; i++)
			//{
			//	DestroyImmediate(uiCopy.transform.GetChild(i).gameObject);
			//}

			uiDictionary.Add("UI Copy", uiCopy);

			var move = EventEditor.inst.dialogRight.GetChild(0);

			// Label Parent (includes two labels, can be set to any number using GenerateLabels)
			SetupCopy(move.GetChild(8).gameObject, eventCopies, "Label");
			SetupCopy(move.GetChild(9).gameObject, eventCopies, "Vector2");

			var single = Instantiate(move.GetChild(9).gameObject);

			single.transform.SetParent(eventCopies);
			DestroyImmediate(single.transform.GetChild(1).gameObject);

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
				var vector4 = Instantiate(uiDictionary["Vector3"]);
				var w = Instantiate(vector4.transform.GetChild(1));
				w.name = "w";
				w.transform.SetParent(vector4.transform);
				w.transform.localScale = Vector3.one;

				vector4.transform.SetParent(eventCopies);
				vector4.transform.localScale = Vector3.one;

				for (int i = 0; i < vector4.transform.childCount; i++)
                {
					((RectTransform)vector4.transform.GetChild(i)).sizeDelta = new Vector2(85f, 32f);
					((RectTransform)vector4.transform.GetChild(i).GetChild(0)).sizeDelta = new Vector2(40f, 32f);
                }

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

				var colorButton = colors.transform.GetChild(0).gameObject.Duplicate(eventCopies);

				uiDictionary.Add("Colors", colors);
				uiDictionary.Add("Color Button", colorButton);
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

			colorsObject.GetComponent<GridLayoutGroup>().spacing = new Vector2(5f, 5f);
			((RectTransform)colorsObject.transform).sizeDelta = new Vector2(366f, 64f);

			LSHelpers.DeleteChildren(colorsObject.transform);

			for (int i = 0; i < 19; i++)
			{
				GameObject toggle = uiDictionary["Color Button"].Duplicate(colorsObject.transform, (i + 1).ToString());

				toggle.GetComponent<Image>().enabled = true;
				toggle.transform.Find("Image").GetComponent<Image>().color = new Color(0.1294f, 0.1294f, 0.1294f);
				var t = toggle.GetComponent<Toggle>();
				t.enabled = true;
				toggles.Add(t);
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
				var colorsBottom = SetupColorButtons("colors2", "Colors Bottom", gradient.transform, 12, gradientColor2Buttons);

				var modeLabel = intensity["Label"].Duplicate(gradient.transform);
                GenerateLabels(modeLabel.transform, "Mode");

				var mode = gradient.transform.Find("curves").gameObject.Duplicate(gradient.transform, "mode");
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
				moveable["UI"].transform.Find("Text").GetComponent<Text>().text = "Moveable";

				var position = GenerateUIElement("position", "Vector2", player.transform, 12, "Position X", "Position Y");

				var rotation = GenerateUIElement("rotation", "Single", player.transform, 14, "Rotation");
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

            try
            {
				var move = EventEditor.inst.dialogRight.Find("move");
				var multiKeyframeEditor = EditorManager.inst.GetDialog("Multi Keyframe Editor").Dialog;

				multiKeyframeEditor.Find("Text").AsRT().sizeDelta = new Vector2(765f, 120f);

				// Label
				{
					var labelBase1 = new GameObject("label base");
					labelBase1.transform.SetParent(multiKeyframeEditor);
					labelBase1.transform.localScale = Vector3.one;
					var labelBase1RT = labelBase1.AddComponent<RectTransform>();
					labelBase1RT.sizeDelta = new Vector2(765f, 38f);

					var l = Instantiate(uiDictionary["Label"]);
					l.name = "label";
					l.transform.SetParent(labelBase1RT);
					l.transform.localScale = Vector3.one;
					GenerateLabels(l.transform, "Time");
					l.transform.AsRT().anchoredPosition = new Vector2(8f, 0f);
				}

				var timeBase = new GameObject("time");
				timeBase.transform.SetParent(multiKeyframeEditor);
				timeBase.transform.localScale = Vector3.one;
				var timeBaseRT = timeBase.AddComponent<RectTransform>();
				timeBaseRT.sizeDelta = new Vector2(765f, 38f);

				var time = move.Find("time").gameObject.Duplicate(timeBaseRT, "time");
				time.transform.AsRT().anchoredPosition = new Vector2(191f, 0f);

				// Label
				{
					var labelBase1 = new GameObject("label base");
					labelBase1.transform.SetParent(multiKeyframeEditor);
					labelBase1.transform.localScale = Vector3.one;
					var labelBase1RT = labelBase1.AddComponent<RectTransform>();
					labelBase1RT.sizeDelta = new Vector2(765f, 38f);

					var l = Instantiate(uiDictionary["Label"]);
					l.name = "label";
					l.transform.SetParent(labelBase1RT);
					l.transform.localScale = Vector3.one;
					GenerateLabels(l.transform, "Ease / Animation Type");
					l.transform.AsRT().anchoredPosition = new Vector2(8f, 0f);
				}

				var curveBase = new GameObject("curves");
				curveBase.transform.SetParent(multiKeyframeEditor);
				curveBase.transform.localScale = Vector3.one;
				var curveBaseRT = curveBase.AddComponent<RectTransform>();
				curveBaseRT.sizeDelta = new Vector2(765f, 38f);

				var curves = move.Find("curves").gameObject.Duplicate(curveBaseRT, "curves");
				curves.transform.AsRT().anchoredPosition = new Vector2(191f, 0f);

				//var valueIndexBase = new GameObject("value index");
				//valueIndexBase.transform.SetParent(multiKeyframeEditor);
				//valueIndexBase.transform.localScale = Vector3.one;
				//var valueIndexBaseRT = valueIndexBase.AddComponent<RectTransform>();
				//valueIndexBaseRT.sizeDelta = new Vector2(765f, 38f);

				var valueIndex = GenerateUIElement("value index", "Single", multiKeyframeEditor, 7, "Value Index");

				//var valueBase = new GameObject("value");
				//valueBase.transform.SetParent(multiKeyframeEditor);
				//valueBase.transform.localScale = Vector3.one;
				//var valueBaseRT = valueBase.AddComponent<RectTransform>();
				//valueBaseRT.sizeDelta = new Vector2(765f, 38f);

				var value = GenerateUIElement("value", "Single", multiKeyframeEditor, 9, "Value");
			}
			catch (Exception ex)
            {
				Debug.LogError($"{EventEditor.inst.className}{ex}");
			}
        }

        #endregion

        #region Dialogs

        public static void LogIncorrectFormat(string str) => Debug.LogError($"{EventEditor.inst.className}Event Value was not in correct format! String: {str}");

		public void RenderMultiEventsDialog()
        {
			var dialog = EditorManager.inst.GetDialog("Multi Keyframe Editor").Dialog;
			var time = dialog.Find("time/time/time").GetComponent<InputField>();
			time.onValueChanged.ClearAll();
			if (time.text == "100.000")
				time.text = "10";
			time.onValueChanged.AddListener(delegate (string _val)
			{
				if (float.TryParse(_val, out float num))
				{
					num = Mathf.Clamp(num, 0f, AudioManager.inst.CurrentAudioSource.clip.length);

                    foreach (var kf in SelectedKeyframes.Where(x => x.Index != 0))
                    {
                        kf.GetData<EventKeyframe>().eventTime = num;
                    }

					UpdateEventOrder();
					RenderEventObjects();
					EventManager.inst.updateEvents();
				}
				else
					LogIncorrectFormat(_val);
			});

			TriggerHelper.IncreaseDecreaseButtons(time, t: dialog.Find("time/time"));
			TriggerHelper.AddEventTriggerParams(time.gameObject, TriggerHelper.ScrollDelta(time));

			var curves = dialog.Find("curves/curves").GetComponent<Dropdown>();
			curves.onValueChanged.ClearAll();
			curves.onValueChanged.AddListener(delegate (int _val)
			{
				if (DataManager.inst.AnimationListDictionary.ContainsKey(_val))
				{
					foreach (var kf in SelectedKeyframes.Where(x => x.Index != 0))
					{
						kf.GetData<EventKeyframe>().curveType = DataManager.inst.AnimationListDictionary[_val];
					}

					EventManager.inst.updateEvents();
				}
			});

			var valueIndex = dialog.Find("value index/x").GetComponent<InputField>();
			valueIndex.onValueChanged.ClearAll();
			if (valueIndex.text == "25.0")
				valueIndex.text = "0";
			valueIndex.onValueChanged.AddListener(delegate (string _val)
			{
				if (!int.TryParse(_val, out int n))
					valueIndex.text = "0";
			});

			TriggerHelper.IncreaseDecreaseButtonsInt(valueIndex);
			TriggerHelper.AddEventTriggerParams(valueIndex.gameObject, TriggerHelper.ScrollDeltaInt(valueIndex));

			var value = dialog.Find("value/x").GetComponent<InputField>();
			value.onValueChanged.ClearAll();
			value.onValueChanged.AddListener(delegate (string _val)
			{
				if (float.TryParse(_val, out float num))
				{
					foreach (var kf in SelectedKeyframes)
					{
						var index = Parser.TryParse(valueIndex.text, 0);

						index = Mathf.Clamp(index, 0, kf.GetData<EventKeyframe>().eventValues.Length - 1);
						kf.GetData<EventKeyframe>().eventValues[index] = num;
					}
				}
				else
					LogIncorrectFormat(_val);
			});

			TriggerHelper.IncreaseDecreaseButtons(value);
			TriggerHelper.AddEventTriggerParams(value.gameObject, TriggerHelper.ScrollDelta(value));
		}

		public void RenderEventsDialog()
		{
			var __instance = EventEditor.inst;
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
                    {

						num = Mathf.Clamp(num, 0f, AudioManager.inst.CurrentAudioSource.clip.length);

						foreach (var kf in SelectedKeyframes.Where(x => x.Index != 0 && x.Type == __instance.currentEventType))
						{
							kf.GetData<EventKeyframe>().eventTime = num;
						}

						UpdateEventOrder();
						RenderEventObjects();
						EventManager.inst.updateEvents();

						//__instance.SetEventStartTime(float.Parse(val));
					}
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
						SetFloatInputField(dialogTmp, "rotation/x", 0, 15f, 3f);
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
						SetFloatInputField(dialogTmp, "intensity/x", 0);

						break;
					}
				case 17: // ScanLines
					{
						// ScanLines Intensity
						SetFloatInputField(dialogTmp, "intensity/x", 0);

						// ScanLines Amount
						SetFloatInputField(dialogTmp, "amount/x", 1);

						// ScanLines Speed
						SetFloatInputField(dialogTmp, "speed/x", 2);
						break;
					}
				case 18: // Blur
					{
						//Blur Amount
						SetFloatInputField(dialogTmp, "intensity/x", 0);

						//Blur Iterations
						SetIntInputField(dialogTmp, "iterations/x", 1, 1, 1, 12);

						break;
					}
				case 19: // Pixelize
					{
						//Pixelize
						SetFloatInputField(dialogTmp, "amount/x", 0, 0.1f, 10f, 0f, 0.99f);

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
						SetFloatInputField(dialogTmp, "amount/x", 0, 0.1f, 10f, 0f, 1f);

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
						SetFloatInputField(dialogTmp, "rotation/x", 5, 15f, 3f);

						// Timeline Color
						SetListColor((int)currentKeyframe.eventValues[6], 6, timelineColorButtons, GameManager.inst.LiveTheme.guiColor);

						break;
					}
				case 23: // Player
					{
						// Player Active
						SetToggle(dialogTmp, "active", 0, 0, 1);

						// Player Moveable
						SetToggle(dialogTmp, "move", 1, 0, 1);

						//// Player Velocity
						//SetFloatInputField(dialogTmp, "position/x", 2);

						//// Player Rotation
						//SetFloatInputField(dialogTmp, "position/y", 3);

						// Player Position
						SetVector2InputField(dialogTmp, "position", 2, 3);

						// Player Rotation
						SetFloatInputField(dialogTmp, "rotation/x", 4, 15f, 3f);

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
					foreach (var kf in SelectedKeyframes.Where(x => x.Index != 0 && x.Type == __instance.currentEventType))
					{
						kf.GetData<EventKeyframe>().curveType = DataManager.inst.AnimationListDictionary[_value];
					}

					//currentKeyframe.curveType = DataManager.inst.AnimationListDictionary[_value];
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
			foreach (var kf in SelectedKeyframes.Where(x => x.Type == EventEditor.inst.currentEventType))
			{
				kf.GetData<EventKeyframe>().eventValues[index] = value;
			}

			//DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[index] = value;
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
				//currentKeyframe.eventValues[index] = val ? onValue : offValue;

				foreach (var kf in SelectedKeyframes.Where(x => x.Type == __instance.currentEventType))
				{
					kf.GetData<EventKeyframe>().eventValues[index] = val ? onValue : offValue;
				}

				EventManager.inst.updateEvents();
			});
		}

		public void SetFloatInputField(Transform dialogTmp, string name, int index, float increase = 0.1f, float multiply = 10f, float min = 0f, float max = 0f, bool allowNegative = true)
		{
			var __instance = EventEditor.inst;

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
					if (min != 0f || max != 0f)
						num = Mathf.Clamp(num, min, max);

					//currentKeyframe.eventValues[index] = num;

					foreach (var kf in SelectedKeyframes.Where(x => x.Type == __instance.currentEventType))
					{
						kf.GetData<EventKeyframe>().eventValues[index] = num;
					}

					EventManager.inst.updateEvents();
				}
				else
					LogIncorrectFormat(val);
			});

			if (dialogTmp.Find($"{name}/<") && dialogTmp.Find($"{name}/>"))
			{
				var tf = dialogTmp.Find($"{name}");

				float num = 1f;

				var btR = tf.Find("<").GetComponent<Button>();
				var btL = tf.Find(">").GetComponent<Button>();

				btR.onClick.ClearAll();
				btR.onClick.AddListener(delegate ()
				{
					if (float.TryParse(zoom.text, out float result))
					{
						result -= Input.GetKey(KeyCode.LeftAlt) ? num / 10f : Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;

						if (min != 0f || max != 0f)
							result = Mathf.Clamp(result, min, max);

						var list = SelectedKeyframes.Where(x => x.Type == __instance.currentEventType);

						if (list.Count() > 1)
							foreach (var kf in list)
							{
								kf.GetData<EventKeyframe>().eventValues[index] -= Input.GetKey(KeyCode.LeftAlt) ? num / 10f : Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;
							}
						else
							zoom.text = result.ToString();
					}
				});

				btL.onClick.ClearAll();
				btL.onClick.AddListener(delegate ()
				{
					if (float.TryParse(zoom.text, out float result))
					{
						result -= Input.GetKey(KeyCode.LeftAlt) ? num / 10f : Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;

						if (min != 0f || max != 0f)
							result = Mathf.Clamp(result, min, max);

						var list = SelectedKeyframes.Where(x => x.Type == __instance.currentEventType);

						if (list.Count() > 1)
							foreach (var kf in list)
							{
								kf.GetData<EventKeyframe>().eventValues[index] += Input.GetKey(KeyCode.LeftAlt) ? num / 10f : Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;
							}
						else
							zoom.text = result.ToString();
					}
				});
			}

			//TriggerHelper.IncreaseDecreaseButtons(zoom, increase * multiply, multiply, min, max);
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

					//currentKeyframe.eventValues[index] = num;

					foreach (var kf in SelectedKeyframes.Where(x => x.Type == __instance.currentEventType))
					{
						kf.GetData<EventKeyframe>().eventValues[index] = num;
					}

					EventManager.inst.updateEvents();
				}
				else
					LogIncorrectFormat(val);
			});

			if (dialogTmp.Find($"{name}/<") && dialogTmp.Find($"{name}/>"))
			{
				var tf = dialogTmp.Find($"{name}");

				float num = 1f;

				var btR = tf.Find("<").GetComponent<Button>();
				var btL = tf.Find(">").GetComponent<Button>();

				btR.onClick.ClearAll();
				btR.onClick.AddListener(delegate ()
				{
					if (float.TryParse(zoom.text, out float result))
					{
						result -= Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;

						if (min != 0f || max != 0f)
							result = Mathf.Clamp(result, min, max);

						var list = SelectedKeyframes.Where(x => x.Type == __instance.currentEventType);

						if (list.Count() > 1)
							foreach (var kf in list)
							{
								kf.GetData<EventKeyframe>().eventValues[index] -= Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;
							}
						else
							zoom.text = result.ToString();
					}
				});

				btL.onClick.ClearAll();
				btL.onClick.AddListener(delegate ()
				{
					if (float.TryParse(zoom.text, out float result))
					{
						result -= Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;

						if (min != 0f || max != 0f)
							result = Mathf.Clamp(result, min, max);

						var list = SelectedKeyframes.Where(x => x.Type == __instance.currentEventType);

						if (list.Count() > 1)
							foreach (var kf in list)
							{
								kf.GetData<EventKeyframe>().eventValues[index] += Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;
							}
						else
							zoom.text = result.ToString();
					}
				});
			}

			//TriggerHelper.IncreaseDecreaseButtonsInt(zoom, increase * 10, min, max);
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

					//currentKeyframe.eventValues[xindex] = num;

					foreach (var kf in SelectedKeyframes.Where(x => x.Type == __instance.currentEventType))
                    {
						kf.GetData<EventKeyframe>().eventValues[xindex] = num;
                    }

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

					//currentKeyframe.eventValues[yindex] = num;

					foreach (var kf in SelectedKeyframes.Where(x => x.Type == __instance.currentEventType))
					{
						kf.GetData<EventKeyframe>().eventValues[yindex] = num;
					}

					EventManager.inst.updateEvents();
				}
				else
					LogIncorrectFormat(val);
			});

			if (dialogTmp.Find($"{name}/x/<") && dialogTmp.Find($"{name}/x/>"))
            {
				var tf = dialogTmp.Find($"{name}/x");

				float num = 1f;

				var btR = tf.Find("<").GetComponent<Button>();
				var btL = tf.Find(">").GetComponent<Button>();

				btR.onClick.ClearAll();
				btR.onClick.AddListener(delegate ()
				{
					if (float.TryParse(posX.text, out float result))
					{
						result -= Input.GetKey(KeyCode.LeftAlt) ? num / 10f : Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;

						if (min != 0f || max != 0f)
							result = Mathf.Clamp(result, min, max);

						var list = SelectedKeyframes.Where(x => x.Type == __instance.currentEventType);

						if (list.Count() > 1)
							foreach (var kf in list)
							{
								kf.GetData<EventKeyframe>().eventValues[xindex] -= Input.GetKey(KeyCode.LeftAlt) ? num / 10f : Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;
							}
						else
							posX.text = result.ToString();
					}
				});

				btL.onClick.ClearAll();
				btL.onClick.AddListener(delegate ()
				{
					if (float.TryParse(posX.text, out float result))
					{
						result -= Input.GetKey(KeyCode.LeftAlt) ? num / 10f : Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;

						if (min != 0f || max != 0f)
							result = Mathf.Clamp(result, min, max);

						var list = SelectedKeyframes.Where(x => x.Type == __instance.currentEventType);

						if (list.Count() > 1)
							foreach (var kf in list)
							{
								kf.GetData<EventKeyframe>().eventValues[xindex] += Input.GetKey(KeyCode.LeftAlt) ? num / 10f : Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;
							}
						else
							posX.text = result.ToString();
					}
				});
			}
			
			if (dialogTmp.Find($"{name}/y/<") && dialogTmp.Find($"{name}/y/>"))
            {
				var tf = dialogTmp.Find($"{name}/y");

				float num = 1f;

				var btR = tf.Find("<").GetComponent<Button>();
				var btL = tf.Find(">").GetComponent<Button>();

				btR.onClick.ClearAll();
				btR.onClick.AddListener(delegate ()
				{
					if (float.TryParse(posY.text, out float result))
					{
						result -= Input.GetKey(KeyCode.LeftAlt) ? num / 10f : Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;

						if (min != 0f || max != 0f)
							result = Mathf.Clamp(result, min, max);

						var list = SelectedKeyframes.Where(x => x.Type == __instance.currentEventType);

						if (list.Count() > 1)
							foreach (var kf in list)
							{
								kf.GetData<EventKeyframe>().eventValues[yindex] -= Input.GetKey(KeyCode.LeftAlt) ? num / 10f : Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;
							}
						else
							posY.text = result.ToString();
					}
				});

				btL.onClick.ClearAll();
				btL.onClick.AddListener(delegate ()
				{
					if (float.TryParse(posY.text, out float result))
					{
						result -= Input.GetKey(KeyCode.LeftAlt) ? num / 10f : Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;

						if (min != 0f || max != 0f)
							result = Mathf.Clamp(result, min, max);

						var list = SelectedKeyframes.Where(x => x.Type == __instance.currentEventType);

						if (list.Count() > 1)
							foreach (var kf in list)
							{
								kf.GetData<EventKeyframe>().eventValues[yindex] += Input.GetKey(KeyCode.LeftAlt) ? num / 10f : Input.GetKey(KeyCode.LeftControl) ? num * 10f : num;
							}
						else
							posY.text = result.ToString();
					}
				});
			}
			
			//TriggerHelper.IncreaseDecreaseButtons(posX, 1f, 10f, min, max);
			//TriggerHelper.IncreaseDecreaseButtons(posY, 1f, 10f, min, max);

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

			for (int i = 0; i < AllEvents.Count; i++)
            {
				DataManager.inst.gameData.eventObjects.allEvents[i] = DataManager.inst.gameData.eventObjects.allEvents[i].OrderBy(x => x.eventTime).ToList();
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
				var image = title.GetChild(0).GetComponent<Image>();
				image.color = EventTitles.ElementAt(i).Value;
				image.rectTransform.sizeDelta = new Vector2(17f, 0f);
				title.GetChild(1).GetComponent<Text>().text = EventTitles.ElementAt(i).Key;
			}
		}

		public static string NoEventLabel => "??? (No event yet)";

		public void RenderLayerBins()
        {
			var renderLeft = RTEditor.GetEditorProperty("Event Labels Render Left").GetConfigEntry<bool>().Value;
			var eventLabels = EventEditor.inst.EventLabels;

			var layer = RTEditor.inst.Layer + 1;

			for (int i = 0; i < AllEvents.Count; i++)
			{
				int t = i % EventLimit;
				int num = layer * EventLimit;

				var text = eventLabels.transform.GetChild(t).GetChild(0).GetComponent<Text>();

				if (i < EventTypes.Length)
                {
                    if (i >= num - EventLimit && i < num)
						text.text = EventTypes[i];
                    else if (i < num)
						text.text = NoEventLabel;
                }
                else
					text.text = NoEventLabel;

				text.alignment = renderLeft ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
			}
		}

		public void SetEventActive(bool active)
        {
			EventEditor.inst.EventLabels.SetActive(active);
			EventEditor.inst.EventHolders.SetActive(active);
		}

		#endregion
	}
}
