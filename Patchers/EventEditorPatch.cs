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

using LSFunctions;
using SimpleJSON;

using EditorManagement.Functions;
using EditorManagement.Functions.Editors;
using EditorManagement.Functions.Helpers;

using RTFunctions.Functions;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Data;
using RTFunctions.Functions.Managers;
using RTFunctions.Patchers;

using BaseBeatmapObject = DataManager.GameData.BeatmapObject;
using BaseEventKeyframe = DataManager.GameData.EventKeyframe;
using BasePrefab = DataManager.GameData.Prefab;
using BasePrefabObject = DataManager.GameData.PrefabObject;
using BaseBackgroundObject = DataManager.GameData.BackgroundObject;

using ObjectType = DataManager.GameData.BeatmapObject.ObjectType;
using AutoKillType = DataManager.GameData.BeatmapObject.AutoKillType;

using ObjectSelection = ObjEditor.ObjectSelection;
using ObjectKeyframeSelection = ObjEditor.KeyframeSelection;
using EventKeyframeSelection = EventEditor.KeyframeSelection;

namespace EditorManagement.Patchers
{
	[HarmonyPatch(typeof(EventEditor))]
    public class EventEditorPatch : MonoBehaviour
    {
        static EventEditor Instance { get => EventEditor.inst; set => EventEditor.inst = value; }

		public static bool EventsCore => ModCompatibility.mods.ContainsKey("EventsCore");

		[HarmonyPatch("Awake")]
		[HarmonyPrefix]
		static bool AwakePrefix(EventEditor __instance)
        {
			// Sets the instance
			if (Instance == null)
				Instance = __instance;
			else if (Instance != __instance)
				Destroy(__instance.gameObject);

			// Regular Legacy only allows for using the first 10 event rows, EventsCore adds the rest.
			for (int i = 0; i < (EventsCore ? 14 : 10); i++)
				Instance.eventObjects.Add(new List<GameObject>());

			for (int i = 0; i < 14; i++)
			{
				var img = Instance.EventLabels.transform.GetChild(i).GetComponent<Image>();
				img.color = RTEventEditor.EventLayerColors[i];
				img.enabled = i < (EventsCore ? 14 : 10);
				Instance.EventLabels.transform.GetChild(i).GetChild(0).GetComponent<Text>().enabled = i < (EventsCore ? 14 : 10);
			}

			for (int i = 0; i < 9; i++)
			{
				__instance.previewTheme.objectColors.Add(LSColors.pink900);
			}

			var beatmapTheme = __instance.previewTheme;

			__instance.previewTheme = new BeatmapTheme
			{
				id = beatmapTheme.id,
				name = beatmapTheme.name,
				expanded = beatmapTheme.expanded,
				backgroundColor = beatmapTheme.backgroundColor,
				guiAccentColor = beatmapTheme.guiColor,
				guiColor = beatmapTheme.guiColor,
				playerColors = beatmapTheme.playerColors,
				objectColors = beatmapTheme.objectColors,
				backgroundColors = beatmapTheme.backgroundColors,
				effectColors = new List<Color>
				{
					LSColors.pink500,
					LSColors.pink500,
					LSColors.pink500,
					LSColors.pink500,
					LSColors.pink500,
					LSColors.pink500,
					LSColors.pink500,
					LSColors.pink500,
					LSColors.pink500,
					LSColors.pink500,
					LSColors.pink500,
					LSColors.pink500,
					LSColors.pink500,
					LSColors.pink500,
					LSColors.pink500,
					LSColors.pink500,
					LSColors.pink500,
					LSColors.pink500,
				},
			};

			return false;
        }

		[HarmonyPatch("Start")]
		[HarmonyPrefix]
		static bool StartPrefix()
		{
			Instance.dialogLeft = EditorManager.inst.GetDialog("Event Editor").Dialog.Find("data/left");
			Instance.dialogRight = EditorManager.inst.GetDialog("Event Editor").Dialog.Find("data/right");
			Instance.EventLabels.SetActive(false);
			Instance.EventHolders.SetActive(false);
			//int num = 0;
			//foreach (var list in DataManager.inst.gameData.eventObjects.allEvents)
			//{
			//	int typeTmp = num;
			//	var entry = new EventTrigger.Entry();
			//	entry.eventID = EventTriggerType.PointerDown;
			//	entry.callback.AddListener(delegate (BaseEventData eventData)
			//	{
			//		Debug.Log("Pointer Down on Position");
			//		if (((PointerEventData)eventData).button == PointerEventData.InputButton.Right)
			//		{
			//			Debug.Log("Right Clicked Position");
			//			Instance.NewKeyframeFromTimeline(typeTmp);
			//		}
			//	});
			//	Instance.EventHolders.transform.GetChild(typeTmp).GetComponent<EventTrigger>().triggers.Add(entry);
			//	num++;
			//}

			RTEventEditor.Init(Instance);

			return false;
		}

		[HarmonyPatch("Update")]
		[HarmonyPrefix]
		static bool UpdatePrefix()
		{
			if (Input.GetMouseButtonUp(0))
				Instance.eventDrag = false;

			if (Instance.eventDrag)
			{
				foreach (var timelineObject in RTEventEditor.inst.SelectedKeyframes)
                {
					if (timelineObject.Index != 0)
                    {
						float num = EditorManager.inst.GetTimelineTime() + timelineObject.timeOffset + Instance.mouseOffsetXForDrag;
						num = Mathf.Clamp(num, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
						if (SettingEditor.inst.SnapActive)
							num = EditorManager.inst.SnapToBPM(num);
						DataManager.inst.gameData.eventObjects.allEvents[timelineObject.Type][timelineObject.Index].eventTime = num;
					}
                }

				if (preNumber != EditorManager.inst.GetTimelineTime())
				{
					Instance.RenderEventObjects();
					preNumber = EditorManager.inst.GetTimelineTime();
				}
			}

			return false;
		}

		public static float preNumber = 0f;

		[HarmonyPatch("CopyAllSelectedEvents")]
		[HarmonyPrefix]
		static bool CopyAllSelectedEventsPrefix()
        {
			RTEventEditor.inst.CopyAllSelectedEvents();
			return false;
        }

		[HarmonyPatch("AddedSelectedEvent")]
		[HarmonyPrefix]
		static bool AddedSelectedEventPrefix(int __0, int __1)
        {
			RTEventEditor.inst.AddSelectedEvent(__0, __1);
			return false;
        }

		[HarmonyPatch("SetCurrentEvent")]
		[HarmonyPrefix]
		static bool SetCurrentEventPrefix(int __0, int __1)
        {
			RTEventEditor.inst.SetCurrentEvent(__0, __1);
			return false;
        }

		[HarmonyPatch("CreateNewEventObject", new Type[] { typeof(int) })]
		[HarmonyPrefix]
		static bool CreateNewEventObjectPrefix(int __0)
        {
			RTEventEditor.inst.CreateNewEventObject(__0);
			return false;
        }

		[HarmonyPatch("CreateNewEventObject", new Type[] { typeof(float), typeof(int) })]
		[HarmonyPrefix]
		static bool CreateNewEventObjectPrefix(float __0, int __1)
        {
			RTEventEditor.inst.CreateNewEventObject(__0, __1);
			return false;
		}

		[HarmonyPatch("NewKeyframeFromTimeline")]
		[HarmonyPrefix]
		static bool NewKeyframeFromTimelinePrefix(int __0)
        {
			RTEventEditor.inst.NewKeyframeFromTimeline(__0);
			return false;
        }

		[HarmonyPatch("CreateEventObjects")]
		[HarmonyPrefix]
		static bool CreateEventObjectsPrefix()
		{
			RTEventEditor.inst.CreateEventObjects();
			return false;
		}

		[HarmonyPatch("RenderEventObjects")]
		[HarmonyPrefix]
		static bool RenderEventObjectsPatch()
		{
			RTEventEditor.inst.RenderEventObjects();
			return false;
		}

		[HarmonyPatch("OpenDialog")]
		[HarmonyPrefix]
		static bool OpenDialogPrefix()
		{
			EditorManager.inst.ClearDialogs(Array.Empty<EditorManager.EditorDialog.DialogType>());
			EditorManager.inst.SetDialogStatus("Event Editor", true);
			Debug.Log($"{Instance.className}Opening Event Editor Dialog: {Instance.dialogRight.GetChild(Instance.currentEventType).name}");
			LSHelpers.SetActiveChildren(Instance.dialogRight, false);
			Instance.dialogRight.GetChild(Instance.currentEventType).gameObject.SetActive(true);

			Instance.RenderEventsDialog();
			Instance.RenderEventObjects();
			return false;
		}

		[HarmonyPatch("RenderThemeContent")]
		[HarmonyPrefix]
		static bool RenderThemeContentPrefix(Transform __0, string __1)
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

			if (__0.TryFind("theme/themepathers/themes path", out Transform themePath) && themePath.gameObject.TryGetComponent(out InputField themePathIF))
				themePathIF.text = RTEditor.ThemePath;

			RTEditor.inst.StartCoroutine(ThemeEditorManager.inst.RenderThemeList(__0, __1));
			return false;
        }

		[HarmonyPatch("RenderThemeEditor")]
		[HarmonyPrefix]
		static bool RenderThemeEditorPrefix(int __0 = -1)
        {
			ThemeEditorManager.inst.RenderThemeEditor(__0);
			return false;
		}

		[HarmonyPatch("RenderEventsDialog")]
		[HarmonyPrefix]
		static bool RenderEventsDialogPrefix()
        {
			RTEventEditor.inst.RenderEventsDialog();
			return false;
        }

		[HarmonyPatch("UpdateEventOrder")]
        [HarmonyPrefix]
        static bool UpdateEventOrderPrefix()
        {
			RTEventEditor.inst.UpdateEventOrder();
			return false;
        }

		[HarmonyPatch("DeleteEvent", new Type[] { typeof(int), typeof(int) })]
        [HarmonyPrefix]
        static bool DeleteEventPrefix(int __0, int __1)
        {
			RTEventEditor.inst.DeleteKeyframe(__0, __1);
			return false;
        }

		[HarmonyPatch("DeleteEvent", new Type[] { typeof(List<EventKeyframeSelection>) })]
        [HarmonyPrefix]
        static bool DeleteEventPrefix(ref IEnumerator __result)
        {
			__result = RTEventEditor.inst.DeleteKeyframes();
			return false;
        }
	}
}
