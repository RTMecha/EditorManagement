using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using UnityEngine;

using RTFunctions.Functions;

using BeatmapObject = DataManager.GameData.BeatmapObject;
using EventKeyframe = DataManager.GameData.EventKeyframe;
using Prefab = DataManager.GameData.Prefab;
using PrefabObject = DataManager.GameData.PrefabObject;
using BackgroundObject = DataManager.GameData.BackgroundObject;

using ObjectSelection = ObjEditor.ObjectSelection;
using KeyframeSelection = EventEditor.KeyframeSelection;

namespace EditorManagement.Functions
{
    public static class EditorExtensions
    {
        public static MethodBase GetMethod(this object type, string method, Type[] parameters = null, Type[] generics = null) => AccessTools.Method(type.GetType(), method, parameters, generics);

        #region Data

        public static ObjectSelection GetObjectSelection(this BeatmapObject beatmapObject) => ObjEditor.inst.selectedObjects.Find(x => x.ID == beatmapObject.id);

        public static bool TryGetObjectSelection(this BeatmapObject beatmapObject, out ObjectSelection objectSelection)
        {
            var obj = beatmapObject.GetObjectSelection();
            if (obj != null)
            {
                objectSelection = obj;
                return true;
            }
            objectSelection = null;
            return false;
        }

        public static bool TryGetObject(this ObjectSelection objectSelection, out BeatmapObject beatmapObject)
        {
            if (objectSelection.IsObject())
            {
                beatmapObject = objectSelection.GetObjectData();
                return true;
            }
            beatmapObject = null;
            return false;
        }

        public static bool TryGetPrefabObject(this ObjectSelection objectSelection, out PrefabObject prefabObject)
        {
            if (objectSelection.IsPrefab())
            {
                prefabObject = objectSelection.GetPrefabObjectData();
                return true;
            }
            prefabObject = null;
            return false;
        }
        
        public static bool TryGetPrefab(this ObjectSelection objectSelection, out Prefab prefab)
        {
            if (objectSelection.IsPrefab())
            {
                prefab = objectSelection.GetPrefabData();
                return true;
            }
            prefab = null;
            return false;
        }

        public static EventKeyframe GetBeatmapObjectKeyframe(this KeyframeSelection keyframeSelection) => ObjEditor.inst.currentObjectSelection?.GetObjectData().events[keyframeSelection.Type][keyframeSelection.Index];

        public static EventKeyframe GetEventKeyframe(this KeyframeSelection keyframeSelection) => DataManager.inst.gameData.eventObjects.allEvents[keyframeSelection.Type][keyframeSelection.Index];

        /// <summary>
        /// Set an EventKeyframe's easing via an integer. If the number is within the range of the list, then the ease is set.
        /// </summary>
        /// <param name="eventKeyframe">The EventKeyframe instance</param>
        /// <param name="ease">The ease to set to the keyframe</param>
        public static void SetEasing(this EventKeyframe eventKeyframe, int ease)
        {
            if (ease >= 0 && ease < DataManager.inst.AnimationList.Count)
                eventKeyframe.curveType = DataManager.inst.AnimationList[ease];
        }

        /// <summary>
        /// Set an EventKeyframe's easing via a string. If the AnimationList contains the specified string, then the ease is set.
        /// </summary>
        /// <param name="eventKeyframe">The EventKeyframe instance</param>
        /// <param name="ease">The ease to set to the keyframe</param>
        public static void SetEasing(this EventKeyframe eventKeyframe, string ease)
        {
            if (DataManager.inst.AnimationList.TryFind(x => x.Name == ease, out DataManager.LSAnimation anim))
                eventKeyframe.curveType = anim;
        }

        /// <summary>
        /// Collapse is now taken into consideration for both the prefab and all objects inside.
        /// </summary>
        /// <param name="prefab">The Prefab instance</param>
        /// <param name="prefabObject">The reference PrefabObject</param>
        /// <param name="collapse">If collapse should be taken into consideration.</param>
        /// <returns></returns>
        public static float GetPrefabLifeLength(this Prefab prefab, PrefabObject prefabObject, bool collapse = false)
        {
            if (collapse && prefabObject.editorData.collapse)
                return 0.2f;

            float firstPrefabObjectTime;
            if (prefab.prefabObjects.Count <= 0)
                firstPrefabObjectTime = 0f;
            else
                firstPrefabObjectTime = (from x in prefab.prefabObjects
                     orderby x.StartTime
                     select x).First().StartTime;

            float firstObjectTime;
            if (prefab.objects.Count <= 0)
                firstObjectTime = 0f;
            else
                firstObjectTime = (from x in prefab.objects
                     orderby x.StartTime
                     select x).First().StartTime;

            float time = Mathf.Min(firstPrefabObjectTime, firstObjectTime);
            float length = 0f;

            foreach (var beatmapObject in prefab.objects)
            {
                float num3 = beatmapObject.StartTime + beatmapObject.GetObjectLifeLength(0f, true, true);
                num3 -= time;
                if (length < num3)
                    length = num3;
            }

            return length;
        }

        /// <summary>
        /// Gets the ObjectSelection StartTime.
        /// </summary>
        /// <param name="objectSelection"></param>
        /// <returns></returns>
        public static float GetStartTime(this ObjectSelection objectSelection)
        {
            if (objectSelection.TryGetObject(out BeatmapObject beatmapObject))
                return beatmapObject.StartTime;

            if (objectSelection.TryGetPrefabObject(out PrefabObject prefabObject))
                return prefabObject.StartTime;

            return 0f;
        }

        public static int GetBin(this ObjectSelection objectSelection)
        {
            if (objectSelection.TryGetObject(out BeatmapObject beatmapObject))
                return beatmapObject.editorData.Bin;
            if (objectSelection.TryGetPrefabObject(out PrefabObject prefabObject))
                return prefabObject.editorData.Bin;
            return 0;
        }
        
        public static int GetLayer(this ObjectSelection objectSelection)
        {
            if (objectSelection.TryGetObject(out BeatmapObject beatmapObject))
                return beatmapObject.editorData.Layer;
            if (objectSelection.TryGetPrefabObject(out PrefabObject prefabObject))
                return prefabObject.editorData.Layer;
            return 0;
        }

        public static bool GetCollapsed(this ObjectSelection objectSelection)
        {
            if (objectSelection.TryGetObject(out BeatmapObject beatmapObject))
                return beatmapObject.editorData.collapse;
            if (objectSelection.TryGetPrefabObject(out PrefabObject prefabObject))
                return prefabObject.editorData.collapse;
            return false;
        }
        
        public static bool GetLocked(this ObjectSelection objectSelection)
        {
            if (objectSelection.TryGetObject(out BeatmapObject beatmapObject))
                return beatmapObject.editorData.locked;
            if (objectSelection.TryGetPrefabObject(out PrefabObject prefabObject))
                return prefabObject.editorData.locked;
            return false;
        }

        public static string GetName(this ObjectSelection objectSelection)
        {
            if (objectSelection.TryGetObject(out BeatmapObject beatmapObject))
                return beatmapObject.name;

            if (objectSelection.IsPrefab())
                return objectSelection.GetPrefabData()?.Name;

            return "Object";
        }

        public static float GetLifeLength(this ObjectSelection objectSelection)
        {
            if (objectSelection.TryGetObject(out BeatmapObject beatmapObject))
                return beatmapObject.GetObjectLifeLength(_takeCollapseIntoConsideration: true);

            if (objectSelection.TryGetPrefab(out Prefab prefab))
                return prefab.GetPrefabLifeLength(objectSelection.GetPrefabObjectData(), true);

            return 0f;
        }

        public static Color GetPrefabTypeColor(this BeatmapObject _beatmapObject)
        {
            var prefab = DataManager.inst.gameData.prefabs.Find(x => x.ID == _beatmapObject.prefabID);
            if (prefab == null)
                return ObjEditor.inst.NormalColor;

            return DataManager.inst.PrefabTypes[prefab.Type].Color;
        }
        
        public static Color GetPrefabTypeColor(this PrefabObject _beatmapObject)
        {
            var prefab = DataManager.inst.gameData.prefabs.Find(x => x.ID == _beatmapObject.prefabID);
            if (prefab == null)
                return ObjEditor.inst.NormalColor;

            return DataManager.inst.PrefabTypes[prefab.Type].Color;
        }

        #endregion
    }
}
