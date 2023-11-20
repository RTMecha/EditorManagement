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

		public List<TimelineObject<EventKeyframe>> SelectedKeyframes => RTEditor.inst.timelineKeyframes.FindAll(x => x.selected);

		public List<TimelineObject<EventKeyframe>> copiedEventKeyframes = new List<TimelineObject<EventKeyframe>>();

		public static List<List<BaseEventKeyframe>> AllEvents => DataManager.inst.gameData.eventObjects.allEvents;

		public const int EventLimit = 14;

		public static bool ResetRotation => true;


		#region Variables

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

		public static Dictionary<string, Color> EventTiles => new Dictionary<string, Color>()
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
			{ "- ??? Editor -", new Color(1f, 0.1490196f, 0.03529412f, 1f) }, // 13
			{ "- ??? Editor -", new Color(1f, 0.05882353f, 0.05882353f, 1f) }, // 14
        };

        #endregion

        void Awake()
        {
            inst = this;
        }

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
				copiedEventKeyframes.Add(new TimelineObject<EventKeyframe>(eventKeyframe));
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
				var eventKeyframe = EventKeyframe.DeepCopy(keyframeSelection.Data);
				//Debug.LogFormat($"Create Keyframe at {eventKeyframe.eventTime} - {eventKeyframe.eventValues[0]}");
				eventKeyframe.eventTime = EditorManager.inst.CurrentAudioPos + eventKeyframe.eventTime;
				if (SettingEditor.inst.SnapActive)
					eventKeyframe.eventTime = EditorManager.inst.SnapToBPM(eventKeyframe.eventTime);

				DataManager.inst.gameData.eventObjects.allEvents[keyframeSelection.Type].Add(eventKeyframe);
			}

			ReorderTime();
			CreateEventObjects();
			EventManager.inst.updateEvents();
		}

		#endregion

		#region Selection

		public void DeselectAllKeyframes()
        {
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

			ReorderTime();

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

			ReorderTime();

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
			//OpenDialog();
		}

		#endregion

		#region Timeline Objects

		public void CreateEventObjects()
        {
			var eventEditor = EventEditor.inst;
			//if (eventEditor.eventObjects.Count > 0)
			//{
			//	foreach (var eventObject in eventEditor.eventObjects)
			//	{
			//		foreach (var @object in eventObject)
			//			Destroy(@object);
			//		eventObject.Clear();
			//	}
			//}

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

					var kf = new TimelineObject<EventKeyframe>((EventKeyframe)eventKeyframe);
					kf.Type = type;
					kf.Index = index;
					kf.GameObject = gameObject;
					kf.Image = image;

					if (kf && EditorManager.inst.currentDialog.Type == EditorManager.EditorDialog.DialogType.Event)
						image.color = eventEditor.Selected;
					else if (eventEditor.currentEvent == index && eventEditor.currentEventType == type && EditorManager.inst.currentDialog.Type == EditorManager.EditorDialog.DialogType.Event)
						image.color = eventEditor.Selected;
					else
						image.color = eventEditor.EventColors[type % EventLimit];

					//if (eventEditor.keyframeSelections.FindIndex(x => x.Type == type && x.Index == index) != -1 && EditorManager.inst.currentDialog.Type == EditorManager.EditorDialog.DialogType.Event)
					//	image.color = eventEditor.Selected;
					//else if (eventEditor.currentEvent == index && eventEditor.currentEventType == type && EditorManager.inst.currentDialog.Type == EditorManager.EditorDialog.DialogType.Event)
					//	image.color = eventEditor.Selected;
					//else
					//	image.color = eventEditor.EventColors[type % EventLimit];

					//eventEditor.eventObjects[type % EventLimit].Add(gameObject);

					var triggers = gameObject.GetComponent<EventTrigger>().triggers;

					triggers.Clear();
					triggers.Add(Triggers.CreateEventObjectTrigger(eventEditor, type, index));
					triggers.Add(Triggers.CreateEventStartDragTrigger(eventEditor, type, index));
					triggers.Add(Triggers.CreateEventEndDragTrigger());

					RTEditor.inst.timelineKeyframes.Add(kf);
				}
			}

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

							var image = kf.Image;

							if (kf.selected)
								image.color = LSColors.white;
							else
								image.color = eventEditor.EventColors[type % EventLimit];

							kf.GameObject.SetActive(true);
						}
						else
							kf.GameObject.SetActive(false);
					}
				}
			}
		}

		#endregion

		#region Rendering

		public void ReorderTime()
        {
			var dictionary = new Dictionary<string, BaseEventKeyframe>();

			int type = 0;
			foreach (var list in DataManager.inst.gameData.eventObjects.allEvents)
            {
				int index = 0;
				foreach (var kf in list)
                {
					var str = $"{type}, {index}";
					if (!dictionary.ContainsKey(str))
						dictionary.Add(str, kf);
                }

				list.OrderBy(x => x.eventTime);
            }

			foreach (var pair in dictionary)
            {
				var str = pair.Key.Replace(" ", "").Split(',');
				int i = int.Parse(str[0]);
				int j = int.Parse(str[1]);

				if (RTEditor.inst.timelineKeyframes.Has(x => x.Type == i && x.Index == j))
					RTEditor.inst.timelineKeyframes.Find(x => x.Type == i && x.Index == j).Index = DataManager.inst.gameData.eventObjects.allEvents[i].IndexOf(pair.Value);
            }
        }

		void RenderTitles()
		{
			for (int i = 0; i < EventEditor.inst.dialogRight.childCount; i++)
			{
				var title = EventEditor.inst.dialogRight.GetChild(i).GetChild(0);
				title.GetChild(0).GetComponent<Image>().color = EventTiles.ElementAt(i).Value;
				title.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(17f, 0f);
				title.GetChild(1).GetComponent<Text>().text = EventTiles.ElementAt(i).Key;
			}
		}

		void RenderLayerBins()
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
