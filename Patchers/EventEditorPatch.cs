using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using LSFunctions;

using EditorManagement.Functions;
using EditorManagement.Functions.Editors;
using EditorManagement.Functions.Helpers;

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
    public class EventEditorPatch : MonoBehaviour
    {
        static EventEditor Instance => EventEditor.inst;

        public static void Init()
        {

        }

		static bool AwakePrefix(EventEditor __instance)
        {
			bool eventsCore = true;
			// Regular Legacy only allows for using the first 10 event rows, EventsCore adds the rest.
			for (int i = 0; i < (eventsCore ? 14 : 10); i++)
				__instance.eventObjects.Add(new List<GameObject>());

			if (EventEditor.inst == null)
			{
				EventEditor.inst = __instance;
				return false;
			}
			if (EventEditor.inst != __instance)
				Destroy(__instance.gameObject);
			return false;
        }

		static bool StartPrefix()
		{
			Instance.dialogLeft = EditorManager.inst.GetDialog("Event Editor").Dialog.Find("data/left");
			Instance.dialogRight = EditorManager.inst.GetDialog("Event Editor").Dialog.Find("data/right");
			Instance.EventLabels.SetActive(false);
			Instance.EventHolders.SetActive(false);
			int num = 0;
			foreach (var list in DataManager.inst.gameData.eventObjects.allEvents)
			{
				int typeTmp = num;
				var entry = new EventTrigger.Entry();
				entry.eventID = EventTriggerType.PointerDown;
				entry.callback.AddListener(delegate (BaseEventData eventData)
				{
					Debug.Log("Pointer Down on Position");
					if (((PointerEventData)eventData).button == PointerEventData.InputButton.Right)
					{
						Debug.Log("Right Clicked Position");
						Instance.NewKeyframeFromTimeline(typeTmp);
					}
				});
				Instance.EventHolders.transform.GetChild(typeTmp).GetComponent<EventTrigger>().triggers.Add(entry);
				num++;
			}
			return false;
		}
		

		static bool CreateEventObjectsPrefix()
		{
			return false;
		}

		static bool RenderEventObjectsPatch()
		{
			return false;
		}
	}
}
