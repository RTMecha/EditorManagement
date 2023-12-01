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
using RTFunctions.Functions.Data;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;
using RTFunctions.Functions.Optimization;

using BaseBeatmapObject = DataManager.GameData.BeatmapObject;
using BaseEventKeyframe = DataManager.GameData.EventKeyframe;
using BasePrefab = DataManager.GameData.Prefab;
using BasePrefabObject = DataManager.GameData.PrefabObject;
using BaseBackgroundObject = DataManager.GameData.BackgroundObject;

using ObjectType = DataManager.GameData.BeatmapObject.ObjectType;
using AutoKillType = DataManager.GameData.BeatmapObject.AutoKillType;

using BaseObjectSelection = ObjEditor.ObjectSelection;
using BaseObjectKeyframeSelection = ObjEditor.KeyframeSelection;
using EventKeyframeSelection = EventEditor.KeyframeSelection;

namespace EditorManagement.Functions.Editors
{
    public class ObjectEditor : MonoBehaviour
    {
        public static ObjectEditor inst;
        void Awake()
        {
            inst = this;
        }
        
        public BeatmapObject CurrentSelection { get; set; }
        public TimelineObject<BeatmapObject> CurrentBeatmapObjectSelection => SelectedBeatmapObjects.Last();
        public TimelineObject<PrefabObject> CurrentPrefabObjectSelection => SelectedPrefabObjects.Last();
        public TimelineObject<EventKeyframe> CurrentBeatmapObjectKeyframeSelection => SelectedBeatmapObjectKeyframes.Last();

        public List<TimelineObject<BeatmapObject>> SelectedBeatmapObjects => RTEditor.inst.TimelineBeatmapObjects.FindAll(x => x.selected);
        public List<TimelineObject<PrefabObject>> SelectedPrefabObjects => RTEditor.inst.TimelinePrefabObjects.FindAll(x => x.selected);
        public List<TimelineObject<EventKeyframe>> SelectedBeatmapObjectKeyframes => RTEditor.inst.timelineBeatmapObjectKeyframes.FindAll(x => x.selected);

        public int SelectedObjectCount => RTEditor.inst.TimelineBeatmapObjects.Count + RTEditor.inst.TimelinePrefabObjects.Count;

        public List<TimelineObject<EventKeyframe>> copiedObjectKeyframes = new List<TimelineObject<EventKeyframe>>();

        public EventKeyframe CopiedPositionData { get; set; }
        public EventKeyframe CopiedScaleData { get; set; }
        public EventKeyframe CopiedRotationData { get; set; }
        public EventKeyframe CopiedColorData { get; set; }

        public void OpenDialog()
        {
            if (SelectedBeatmapObjects.Count == 1)
            {
                EditorManager.inst.ClearDialogs(Array.Empty<EditorManager.EditorDialog.DialogType>());
                EditorManager.inst.ShowDialog("Object Editor", false);
            }
        }

        #region Deleting

        public IEnumerator DeleteObjects<T>(List<TimelineObject<T>> timelineObjects, bool _set = true)
        {
            RTEditor.inst.ienumRunning = true;

            float delay = 0f;
            int count = SelectedObjectCount;

            int num = DataManager.inst.gameData.beatmapObjects.Count;
            foreach (var obj in timelineObjects)
            {
                if (obj.Index < num)
                {
                    num = obj.Index;
                }
            }

            EditorManager.inst.DisplayNotification("Deleting Beatmap Objects [ " + count + " ]", 1f, EditorManager.NotificationType.Success);

            foreach (var obj in timelineObjects)
            {
                yield return new WaitForSeconds(delay);
                inst.StartCoroutine(DeleteObject(obj, false));
                delay += 0.0001f;
            }

            //ObjEditor.inst.SetCurrentObj(new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Object, Mathf.Clamp(num - 1, 0, DataManager.inst.gameData.beatmapObjects.Count)));
            EditorManager.inst.DisplayNotification("Deleted Beatmap Objects [ " + count + " ]", 1f, EditorManager.NotificationType.Success);

            RTEditor.inst.ienumRunning = false;
            yield break;
        }

        public IEnumerator DeleteObject<T>(TimelineObject<T> timelineObject, bool _set = true)
        {
            int index = timelineObject.Index;
            string id = timelineObject.ID;

            if (timelineObject.IsBeatmapObject)
            {
                Updater.UpdateProcessor(timelineObject.Data as BaseBeatmapObject, reinsert: false);
                if (DataManager.inst.gameData.beatmapObjects.Count > 1)
                {
                    //if (ObjectModifiersEditor.inst != null && _obj.GetObjectData() != null)
                    //{
                    //    ObjectModifiersEditor.RemoveModifierObject(_obj.GetObjectData());
                    //}

                    Destroy(RTEditor.inst.timelineBeatmapObjects[id].GameObject);
                    RTEditor.inst.timelineBeatmapObjects.Remove(id);

                    DataManager.inst.gameData.beatmapObjects.RemoveAt(index);

                    if (_set)
                        if (DataManager.inst.gameData.beatmapObjects.Count > 0)
                            SetCurrentObject(RTEditor.inst.TimelineBeatmapObjects[Mathf.Clamp(index - 1, 0, DataManager.inst.gameData.beatmapObjects.Count - 1)]);

                    foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
                    {
                        if (beatmapObject.parent == id)
                        {
                            beatmapObject.parent = "";

                            Updater.UpdateProcessor(beatmapObject);
                        }
                    }
                }
                else
                    EditorManager.inst.DisplayNotification("Can't delete only object", 2f, EditorManager.NotificationType.Error);
            }
            else if (timelineObject.IsPrefabObject)
            {
                foreach (var bm in DataManager.inst.gameData.beatmapObjects.FindAll(x => x.prefabInstanceID == timelineObject.ID))
                    Updater.UpdateProcessor(bm, reinsert: false);

                DataManager.inst.gameData.prefabObjects.RemoveAt(index);
                if (_set)
                {
                    if (DataManager.inst.gameData.prefabObjects.Count > 0)
                        SetCurrentObject(RTEditor.inst.TimelinePrefabObjects[Mathf.Clamp(index - 1, 0, DataManager.inst.gameData.prefabObjects.Count - 1)]);
                    else if (DataManager.inst.gameData.beatmapObjects.Count > 0)
                        SetCurrentObject(RTEditor.inst.TimelineBeatmapObjects[Mathf.Clamp(index - 1, 0, DataManager.inst.gameData.beatmapObjects.Count - 1)]);
                    else if (DataManager.inst.gameData.beatmapData.checkpoints.Count > 0)
                        CheckpointEditor.inst.SetCurrentCheckpoint(0);
                }
            }
            yield break;
        }

        public IEnumerator DeleteKeyframes()
        {
            RTEditor.inst.ienumRunning = true;

            float delay = 0f;

            var list = new List<TimelineObject<BaseEventKeyframe>>();

            foreach (var timelineObject in SelectedBeatmapObjectKeyframes)
            {
                var otherTLO = new TimelineObject<BaseEventKeyframe>(timelineObject.Data);

                otherTLO.Type = timelineObject.Type;
                otherTLO.Index = otherTLO.Index;

                list.Add(otherTLO);
            }

            list = (from x in list
                    orderby x.Index descending
                    select x).ToList();

            int count = list.Count;

            EditorManager.inst.DisplayNotification("Deleting Object Keyframes [ " + count + " ]", 2f, EditorManager.NotificationType.Success);

            var beatmapObject = CurrentBeatmapObjectSelection.Data;

            foreach (var timelineObject in list)
            {
                if (timelineObject.Index != 0)
                {
                    yield return new WaitForSeconds(delay);

                    beatmapObject.events[timelineObject.Type].RemoveAt(timelineObject.Index);

                    delay += 0.0001f;
                }
                else
                    EditorManager.inst.DisplayNotification("Can't delete first Keyframe", 2f, EditorManager.NotificationType.Error, false);
            }

            SetCurrentKeyframe(beatmapObject, 0);
            if (RTEditor.inst.timelineBeatmapObjects.ContainsKey(beatmapObject.id))
                RenderTimelineObject(RTEditor.inst.timelineBeatmapObjects[beatmapObject.id]);
            if (UpdateObjects)
                Updater.UpdateProcessor(beatmapObject, "Keyframes");

            CreateKeyframes(beatmapObject);
            RenderKeyframes(beatmapObject);
            ResizeKeyframeTimeline(beatmapObject);

            EditorManager.inst.DisplayNotification("Deleted Object Keyframes [ " + count + " ]", 1f, EditorManager.NotificationType.Success);

            RTEditor.inst.ienumRunning = false;

            yield break;
        }

        public void DeleteKeyframe() => DeleteKeyframe(CurrentBeatmapObjectSelection.Data, ObjEditor.inst.currentKeyframeKind, ObjEditor.inst.currentKeyframe);

        public void DeleteKeyframe(int _type, int _index) => DeleteKeyframe(CurrentBeatmapObjectSelection.Data, _type, _index);

        public void DeleteKeyframe(BeatmapObject beatmapObject, int _type, int _index)
        {
            if (_index != 0)
            {
                AddCurrentKeyframe(beatmapObject, -1, false);
                beatmapObject.events[_type].RemoveAt(_index);
                if (RTEditor.inst.timelineBeatmapObjects.ContainsKey(beatmapObject.id))
                    RenderTimelineObject(RTEditor.inst.timelineBeatmapObjects[beatmapObject.id]);
                if (UpdateObjects)
                    Updater.UpdateProcessor(beatmapObject, "Keyframes");
                CreateKeyframes(beatmapObject);
                RenderKeyframes(beatmapObject);
                ResizeKeyframeTimeline(beatmapObject);
                return;
            }
            EditorManager.inst.DisplayNotification("Can't delete first Keyframe", 2f, EditorManager.NotificationType.Error, false);
        }

        #endregion

        #region Copy / Paste

        public void CopyAllSelectedEvents(BeatmapObject beatmapObject)
        {
            copiedObjectKeyframes.Clear();
            UpdateKeyframeOrder(beatmapObject);
            float num = float.PositiveInfinity;

            foreach (var timelineObject in SelectedBeatmapObjectKeyframes)
            {
                if (beatmapObject.events[timelineObject.Type][timelineObject.Index].eventTime < num)
                    num = beatmapObject.events[timelineObject.Type][timelineObject.Index].eventTime;
            }

            foreach (var timelineObject in SelectedBeatmapObjectKeyframes)
            {
                int type = timelineObject.Type;
                int index = timelineObject.Index;
                var eventKeyframe = EventKeyframe.DeepCopy((EventKeyframe)beatmapObject.events[type][index]);
                eventKeyframe.eventTime -= num;

                var otherTLO = new TimelineObject<EventKeyframe>(eventKeyframe);
                otherTLO.Type = type;
                otherTLO.Index = index;

                copiedObjectKeyframes.Add(otherTLO);
            }
        }

        public void PasteKeyframes(BeatmapObject beatmapObject)
        {
            if (ObjEditor.inst.copiedObjectKeyframes.Keys.Count <= 0)
            {
                Debug.LogError($"{EditorPlugin.className}No copied event yet!");
                return;
            }
            foreach (var timelineObject in copiedObjectKeyframes)
            {
                var eventKeyframe = EventKeyframe.DeepCopy(timelineObject.Data);
                Debug.Log($"Create Keyframe -> {eventKeyframe.eventTime} - {eventKeyframe.eventValues[0]}");

                eventKeyframe.eventTime = EditorManager.inst.CurrentAudioPos - beatmapObject.StartTime + eventKeyframe.eventTime;
                if (eventKeyframe.eventTime <= 0f)
                    eventKeyframe.eventTime = 0.001f;

                beatmapObject.events[timelineObject.Type].Add(eventKeyframe);
            }

            UpdateKeyframeOrder(beatmapObject);
            CreateKeyframes(beatmapObject);
            ResizeKeyframeTimeline(beatmapObject);

            if (RTEditor.inst.timelineBeatmapObjects.ContainsKey(beatmapObject.id))
                RenderTimelineObject(RTEditor.inst.timelineBeatmapObjects[beatmapObject.id]);

            if (UpdateObjects)
                Updater.UpdateProcessor(beatmapObject, "Keyframes");
        }

        public void PasteObject(float _offsetTime = 0f, bool _regen = true)
        {
            if (!ObjEditor.inst.hasCopiedObject || ObjEditor.inst.beatmapObjCopy == null || (ObjEditor.inst.beatmapObjCopy.prefabObjects.Count <= 0 && ObjEditor.inst.beatmapObjCopy.objects.Count <= 0))
            {
                EditorManager.inst.DisplayNotification("No copied object yet!", 1f, EditorManager.NotificationType.Error, false);
                return;
            }

            DeselectAllObjects();
            EditorManager.inst.DisplayNotification("Pasting objects, please wait.", 1f, EditorManager.NotificationType.Success);

            BasePrefab pr = null;
            Dictionary<string, Dictionary<string, object>> modifiers = null;

            if (RTFile.FileExists(Application.persistentDataPath + "/copied_objects.lsp"))
            {
                JSONNode jn = JSON.Parse(FileManager.inst.LoadJSONFileRaw(Application.persistentDataPath + "/copied_objects.lsp"));

                pr = Prefab.Parse(jn);

                ObjEditor.inst.hasCopiedObject = true;
            }

            if (pr == null)
                inst.StartCoroutine(AddPrefabExpandedToLevel(ObjEditor.inst.beatmapObjCopy, true, _offsetTime, false, _regen));
            else
                inst.StartCoroutine(AddPrefabExpandedToLevel(pr, true, _offsetTime, false, _regen, modifiers));

            //Keyframe bug testing
            foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
            {
                if (ObjEditor.inst.keyframeTimelineSelections.ContainsKey(beatmapObject.id))
                {
                    if (ObjEditor.inst.keyframeTimelineSelections[beatmapObject.id].Count == 0)
                    {
                        var item = new BaseObjectKeyframeSelection(0, 0);
                        ObjEditor.inst.keyframeTimelineSelections[beatmapObject.id].Add(item);
                    }
                }
            }
        }

        #endregion

        #region Prefabs

        /// <summary>
        /// Expands a prefab into the level.
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="select"></param>
        /// <param name="offset"></param>
        /// <param name="undone"></param>
        /// <param name="regen"></param>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        public IEnumerator AddPrefabExpandedToLevel(BasePrefab prefab, bool select = false, float offset = 0f, bool undone = false, bool regen = false, Dictionary<string, Dictionary<string, object>> dictionary = null)
        {
            RTEditor.inst.ienumRunning = true;
            float delay = 0f;
            float audioTime = EditorManager.inst.CurrentAudioPos;
            Debug.Log($"{EditorPlugin.className}Placing prefab with {prefab.objects.Count} objects and {prefab.prefabObjects.Count} prefabs");

            //Objects
            {
                var ids = new Dictionary<string, string>();
                foreach (var beatmapObject in prefab.objects)
                    ids.Add(beatmapObject.id, LSText.randomString(16));

                var prefabInstances = new Dictionary<string, string>();
                foreach (var beatmapObject in prefab.objects)
                    if (!string.IsNullOrEmpty(beatmapObject.prefabInstanceID) && !prefabInstances.ContainsKey(beatmapObject.prefabInstanceID))
                        prefabInstances.Add(beatmapObject.prefabInstanceID, LSText.randomString(16));

                foreach (var beatmapObject in prefab.objects)
                {
                    yield return new WaitForSeconds(delay);
                    var beatmapObjectCopy = BaseBeatmapObject.DeepCopy(beatmapObject, false);

                    if (ids.ContainsKey(beatmapObject.id))
                        beatmapObjectCopy.id = ids[beatmapObject.id];

                    if (ids.ContainsKey(beatmapObject.parent))
                        beatmapObjectCopy.parent = ids[beatmapObject.parent];
                    else if (DataManager.inst.gameData.beatmapObjects.FindIndex(x => x.id == beatmapObject.parent) == -1)
                        beatmapObjectCopy.parent = "";

                    beatmapObjectCopy.prefabID = beatmapObject.prefabID;
                    if (regen)
                    {
                        //beatmapObject2.prefabInstanceID = dictionary2[orig.prefabInstanceID];
                        beatmapObjectCopy.prefabID = "";
                        beatmapObjectCopy.prefabInstanceID = "";
                    }
                    else
                    {
                        beatmapObjectCopy.prefabInstanceID = beatmapObject.prefabInstanceID;
                    }

                    beatmapObjectCopy.fromPrefab = beatmapObject.fromPrefab;
                    if (undone == false)
                    {
                        if (offset == 0.0)
                        {
                            beatmapObjectCopy.StartTime += audioTime;
                            beatmapObjectCopy.StartTime += prefab.Offset;
                        }
                        else
                        {
                            beatmapObjectCopy.StartTime += offset;
                            ++beatmapObjectCopy.editorData.Bin;
                        }
                    }
                    else
                    {
                        if (offset == 0.0)
                        {
                            beatmapObjectCopy.StartTime += prefab.Offset;
                        }
                        else
                        {
                            beatmapObjectCopy.StartTime += offset;
                            ++beatmapObjectCopy.editorData.Bin;
                        }
                    }

                    beatmapObjectCopy.editorData.Layer = RTEditor.inst.Layer;
                    beatmapObjectCopy.fromPrefab = false;
                    DataManager.inst.gameData.beatmapObjects.Add(beatmapObjectCopy);

                    var timelineObject = new TimelineObject<BaseBeatmapObject>(beatmapObjectCopy);
                    RenderTimelineObject(timelineObject);

                    if (select)
                        AddSelectedObject(timelineObject);

                    // Rework the instances.
                    //if (ObjectModifiersEditor.inst != null)
                    //{
                    //    ObjectModifiersEditor.AddModifierObject(beatmapObject2);
                    //}

                    //if (GameObject.Find("BepInEx_Manager").GetComponentByName("ObjectModifiersPlugin") && _dictionary != null)
                    //{
                    //    var objectModifiersPlugin = GameObject.Find("BepInEx_Manager").GetComponentByName("ObjectModifiersPlugin").GetType();
                    //    objectModifiersPlugin.GetMethod("AddModifierObjectWithValues").Invoke(objectModifiersPlugin, new object[] { beatmapObject2, _dictionary[orig.id] });
                    //}

                    delay += 0.0001f;
                }

            }

            //Prefabs
            {
                Dictionary<string, string> prefabInstanceIDs = new Dictionary<string, string>();
                foreach (var prefabObject in prefab.prefabObjects)
                    prefabInstanceIDs.Add(prefabObject.ID, LSText.randomString(16));

                foreach (var prefabObject in prefab.prefabObjects)
                {
                    yield return new WaitForSeconds(delay);
                    var prefabObjectCopy = BasePrefabObject.DeepCopy(prefabObject, false);
                    if (prefabInstanceIDs.ContainsKey(prefabObject.ID))
                        prefabObjectCopy.ID = prefabInstanceIDs[prefabObject.ID];
                    prefabObjectCopy.prefabID = prefabObject.prefabID;

                    if (undone == false)
                    {
                        if (offset == 0.0)
                        {
                            prefabObjectCopy.StartTime += audioTime;
                            prefabObjectCopy.StartTime += prefab.Offset;
                        }
                        else
                        {
                            prefabObjectCopy.StartTime += offset;
                            ++prefabObjectCopy.editorData.Bin;
                        }
                    }
                    else
                    {
                        if (offset == 0.0)
                        {
                            prefabObjectCopy.StartTime += prefab.Offset;
                        }
                        else
                        {
                            prefabObjectCopy.StartTime += offset;
                            ++prefabObjectCopy.editorData.Bin;
                        }
                    }

                    prefabObjectCopy.editorData.Layer = RTEditor.inst.Layer;

                    DataManager.inst.gameData.prefabObjects.Add(prefabObjectCopy);

                    var timelineObject = new TimelineObject<BasePrefabObject>(prefabObjectCopy);

                    CreateTimelineObject(timelineObject);
                    RenderTimelineObject(timelineObject);

                    ObjectManager.inst.AddPrefabToLevel(prefabObjectCopy);

                    if (select)
                        AddSelectedObject(timelineObject);

                    delay += 0.0001f;
                }
            }

            string stri = "object";
            if (prefab.objects.Count == 1)
            {
                stri = prefab.objects[0].name;
            }
            if (prefab.objects.Count > 1)
            {
                stri = prefab.Name;
            }
            {
                string s = prefab.objects.Count == 1 ? "" : "s";
                var reg = regen ? "" : $"and kept Prefab Instance ID [ {prefab.ID} ]";
                EditorManager.inst.DisplayNotification($"Pasted Beatmap Object{s} [ {stri} ]{reg}!", 1.5f, EditorManager.NotificationType.Success);
            }

            if (select && (prefab.objects.Count > 1 || prefab.prefabObjects.Count > 1))
            {
                EditorManager.inst.ShowDialog("Multi Object Editor", false);
            }

            RTEditor.inst.ienumRunning = false;
            yield break;
        }

        #endregion

        #region Create New Objects

        public static bool SetToCenterCam => true;

        public void CreateNewNormalObject(bool _select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(_select);

            var bm = timelineObject.Data;
            if (SetToCenterCam)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].eventValues[0] = pos.x;
                bm.events[0][0].eventValues[1] = pos.y;
            }

            Updater.UpdateProcessor(bm);
            RenderTimelineObject(timelineObject);
            ObjEditor.inst.OpenDialog();

            if (setHistory)
            {
                EditorManager.inst.history.Add(new History.Command("Create New Normal Object", delegate ()
                {
                    CreateNewNormalObject(_select, false);
                }, delegate ()
                {
                    inst.StartCoroutine(DeleteObject(timelineObject));
                }), false);
            }
        }

        public void CreateNewCircleObject(bool _select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(_select);

            var bm = timelineObject.Data;
            bm.shape = 1;
            bm.shapeOption = 0;
            bm.name = "circle";

            if (SetToCenterCam)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].eventValues[0] = pos.x;
                bm.events[0][0].eventValues[1] = pos.y;
            }

            Updater.UpdateProcessor(bm);
            RenderTimelineObject(timelineObject);
            ObjEditor.inst.OpenDialog();

            if (setHistory)
            {
                EditorManager.inst.history.Add(new History.Command("Create New Normal Circle Object", delegate ()
                {
                    CreateNewCircleObject(_select, false);
                }, delegate ()
                {
                    inst.StartCoroutine(DeleteObject(timelineObject));
                }), false);
            }
        }

        public void CreateNewTriangleObject(bool _select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(_select);

            var bm = timelineObject.Data;
            bm.shape = 2;
            bm.shapeOption = 0;
            bm.name = "triangle";

            if (SetToCenterCam)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].eventValues[0] = pos.x;
                bm.events[0][0].eventValues[1] = pos.y;
            }

            Updater.UpdateProcessor(bm);
            RenderTimelineObject(timelineObject);
            ObjEditor.inst.OpenDialog();

            if (setHistory)
            {
                EditorManager.inst.history.Add(new History.Command("Create New Normal Triangle Object", delegate ()
                {
                    CreateNewTriangleObject(_select, false);
                }, delegate ()
                {
                    inst.StartCoroutine(DeleteObject(timelineObject));
                }), false);
            }
        }

        public void CreateNewTextObject(bool _select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(_select);

            var bm = timelineObject.Data;
            bm.shape = 4;
            bm.shapeOption = 0;
            bm.text = "text";
            bm.name = "text";
            bm.objectType = ObjectType.Decoration;

            if (SetToCenterCam)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].eventValues[0] = pos.x;
                bm.events[0][0].eventValues[1] = pos.y;
            }

            Updater.UpdateProcessor(bm);
            RenderTimelineObject(timelineObject);
            ObjEditor.inst.OpenDialog();

            if (setHistory)
            {
                EditorManager.inst.history.Add(new History.Command("Create New Normal Text Object", delegate ()
                {
                    CreateNewTextObject(_select, false);
                }, delegate ()
                {
                    inst.StartCoroutine(DeleteObject(timelineObject));
                }), false);
            }
        }

        public void CreateNewHexagonObject(bool _select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(_select);

            var bm = timelineObject.Data;
            bm.shape = 5;
            bm.shapeOption = 0;
            bm.name = "hexagon";

            if (SetToCenterCam)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].eventValues[0] = pos.x;
                bm.events[0][0].eventValues[1] = pos.y;
            }

            Updater.UpdateProcessor(bm);
            RenderTimelineObject(timelineObject);
            ObjEditor.inst.OpenDialog();

            if (setHistory)
            {
                EditorManager.inst.history.Add(new History.Command("Create New Normal Hexagon Object", delegate ()
                {
                    CreateNewHexagonObject(_select, false);
                }, delegate ()
                {
                    inst.StartCoroutine(DeleteObject(timelineObject));
                }), false);
            }
        }

        public void CreateNewHelperObject(bool _select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(_select);

            var bm = timelineObject.Data;
            bm.name = "helper";
            bm.objectType = ObjectType.Helper;

            if (SetToCenterCam)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].eventValues[0] = pos.x;
                bm.events[0][0].eventValues[1] = pos.y;
            }

            Updater.UpdateProcessor(bm);
            RenderTimelineObject(timelineObject);
            ObjEditor.inst.OpenDialog();

            if (setHistory)
            {
                EditorManager.inst.history.Add(new History.Command("Create New Helper Object", delegate ()
                {
                    CreateNewHelperObject(_select, false);
                }, delegate ()
                {
                    inst.StartCoroutine(DeleteObject(timelineObject));
                }), false);
            }
        }

        public void CreateNewDecorationObject(bool _select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(_select);

            var bm = timelineObject.Data;
            bm.name = "decoration";
            bm.objectType = ObjectType.Decoration;

            if (SetToCenterCam)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].eventValues[0] = pos.x;
                bm.events[0][0].eventValues[1] = pos.y;
            }

            Updater.UpdateProcessor(bm);
            RenderTimelineObject(timelineObject);
            ObjEditor.inst.OpenDialog();

            if (setHistory)
            {
                EditorManager.inst.history.Add(new History.Command("Create New Decoration Object", delegate ()
                {
                    CreateNewDecorationObject(_select, false);
                }, delegate ()
                {
                    inst.StartCoroutine(DeleteObject(timelineObject));
                }), false);
            }
        }

        public void CreateNewEmptyObject(bool _select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(_select);

            var bm = timelineObject.Data;
            bm.name = "empty";
            bm.objectType = ObjectType.Empty;

            if (SetToCenterCam)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].eventValues[0] = pos.x;
                bm.events[0][0].eventValues[1] = pos.y;
            }

            Updater.UpdateProcessor(bm);
            RenderTimelineObject(timelineObject);
            ObjEditor.inst.OpenDialog();

            if (setHistory)
            {
                EditorManager.inst.history.Add(new History.Command("Create New Empty Object", delegate ()
                {
                    CreateNewEmptyObject(_select, false);
                }, delegate ()
                {
                    inst.StartCoroutine(DeleteObject(timelineObject));
                }), false);
            }
        }

        public void CreateNewNoAutokillObject(bool _select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(_select);

            var bm = timelineObject.Data;
            bm.name = "no autokill";
            bm.autoKillType = AutoKillType.OldStyleNoAutokill;

            if (SetToCenterCam)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].eventValues[0] = pos.x;
                bm.events[0][0].eventValues[1] = pos.y;
            }

            Updater.UpdateProcessor(bm);
            RenderTimelineObject(timelineObject);
            ObjEditor.inst.OpenDialog();

            if (setHistory)
            {
                EditorManager.inst.history.Add(new History.Command("Create New No Autokill Object", delegate ()
                {
                    CreateNewNoAutokillObject(_select, false);
                }, delegate ()
                {
                    inst.StartCoroutine(DeleteObject(timelineObject));
                }), false);
            }
        }

        public TimelineObject<BeatmapObject> CreateNewDefaultObject(bool _select = true)
        {
            if (!EditorManager.inst.hasLoadedLevel)
            {
                EditorManager.inst.DisplayNotification("Can't add objects to level until a level has been loaded!", 2f, EditorManager.NotificationType.Error);
                return null;
            }

            var list = new List<List<BaseEventKeyframe>>();
            list.Add(new List<BaseEventKeyframe>());
            list.Add(new List<BaseEventKeyframe>());
            list.Add(new List<BaseEventKeyframe>());
            list.Add(new List<BaseEventKeyframe>());
            list[0].Add(new EventKeyframe(0f, new float[3], new float[0], 0));
            list[1].Add(new EventKeyframe(0f, new float[]
            {
                1f,
                1f
            }, new float[0], 0));

            list[2].Add(new EventKeyframe(0f, new float[1], new float[0], 0));

            list[3].Add(new EventKeyframe(0f, new float[5], new float[0], 0));

            var beatmapObject = new BeatmapObject(true, AudioManager.inst.CurrentAudioSource.time, "", 0, "", list);
            beatmapObject.id = LSText.randomString(16);
            beatmapObject.autoKillType = AutoKillType.LastKeyframeOffset;
            beatmapObject.autoKillOffset = 5f;
            beatmapObject.editorData.Layer = RTEditor.inst.Layer;

            if (RTEditor.inst.layerType == RTEditor.LayerType.Events)
                RTEditor.inst.SetLayer(RTEditor.LayerType.Objects);

            int num = DataManager.inst.gameData.beatmapObjects.FindIndex(x => x.fromPrefab);
            if (num == -1)
                DataManager.inst.gameData.beatmapObjects.Add(beatmapObject);
            else
                DataManager.inst.gameData.beatmapObjects.Insert(num, beatmapObject);

            var timelineObject = new TimelineObject<BeatmapObject>(beatmapObject);

            if (!RTEditor.inst.timelineBeatmapObjects.ContainsKey(beatmapObject.id))
                RTEditor.inst.timelineBeatmapObjects.Add(beatmapObject.id, timelineObject);

            RenderTimelineObject(timelineObject);
            Updater.UpdateProcessor(beatmapObject);

            if (AllowTimeExactlyAtStart)
                AudioManager.inst.SetMusicTime(AudioManager.inst.CurrentAudioSource.time);
            else
                AudioManager.inst.SetMusicTime(AudioManager.inst.CurrentAudioSource.time + 0.001f);

            if (_select)
                SetCurrentObject(timelineObject);

            //if (ObjectModifiersEditor.inst != null)
            //{
            //    ObjectModifiersEditor.AddModifierObject(beatmapObject);
            //}

            return timelineObject;
        }

        public static BeatmapObject CreateNewBeatmapObject(float _time, bool _add = true)
        {
            var beatmapObject = new BeatmapObject();
            beatmapObject.id = LSText.randomString(16);
            beatmapObject.StartTime = _time;

            beatmapObject.editorData.Layer = RTEditor.inst.Layer;

            var eventKeyframe = new EventKeyframe();
            eventKeyframe.eventTime = 0f;
            eventKeyframe.SetEventValues(new float[3]);

            var eventKeyframe2 = new EventKeyframe();
            eventKeyframe2.eventTime = 0f;
            eventKeyframe2.SetEventValues(new float[]
            {
                1f,
                1f
            });

            var eventKeyframe3 = new EventKeyframe();
            eventKeyframe3.eventTime = 0f;
            eventKeyframe3.SetEventValues(new float[1]);

            var eventKeyframe4 = new EventKeyframe();
            eventKeyframe4.eventTime = 0f;
            eventKeyframe4.SetEventValues(new float[]
            {
                0f,
                0f,
                0f,
                0f,
                0f
            });

            beatmapObject.events[0].Add(eventKeyframe);
            beatmapObject.events[1].Add(eventKeyframe2);
            beatmapObject.events[2].Add(eventKeyframe3);
            beatmapObject.events[3].Add(eventKeyframe4);

            if (_add)
            {
                DataManager.inst.gameData.beatmapObjects.Add(beatmapObject);


                var timelineObject = new TimelineObject<BeatmapObject>(beatmapObject);

                if (!RTEditor.inst.timelineBeatmapObjects.ContainsKey(beatmapObject.id))
                    RTEditor.inst.timelineBeatmapObjects.Add(beatmapObject.id, timelineObject);

                RenderTimelineObject(timelineObject);
                Updater.UpdateProcessor(beatmapObject);
                inst.SetCurrentObject(timelineObject);

                //if (ObjectModifiersEditor.inst != null)
                //{
                //    ObjectModifiersEditor.AddModifierObject(beatmapObject);
                //}
            }
            return beatmapObject;
        }

        #endregion

        #region Selection

        public IEnumerator GroupSelectObjects(bool _add = true)
        {
            RTEditor.inst.ienumRunning = true;
            EditorManager.inst.DisplayNotification("Selecting objects, please wait.", 1f, EditorManager.NotificationType.Success);
            float delay = 0f;
            var objEditor = ObjEditor.inst;

            if (!_add)
            {
                DeselectAllObjects();

                objEditor.selectedObjects.Clear();
                RenderTimelineObjects();
            }

            foreach (var timelineObject in RTEditor.inst.TimelineBeatmapObjects.FindAll(x => x.Layer == RTEditor.inst.Layer))
            {
                if (RTMath.RectTransformToScreenSpace(EditorManager.inst.SelectionBoxImage.rectTransform).Overlaps(RTMath.RectTransformToScreenSpace(timelineObject.Image.rectTransform)))
                {
                    yield return new WaitForSeconds(delay);
                    AddSelectedObject(timelineObject);
                    delay += 0.0001f;
                }
            }

            foreach (var timelineObject in RTEditor.inst.TimelinePrefabObjects.FindAll(x => x.Layer == RTEditor.inst.Layer))
            {
                if (RTMath.RectTransformToScreenSpace(EditorManager.inst.SelectionBoxImage.rectTransform).Overlaps(RTMath.RectTransformToScreenSpace(timelineObject.Image.rectTransform)))
                {
                    yield return new WaitForSeconds(delay);
                    AddSelectedObject(timelineObject);
                    delay += 0.0001f;
                }
            }

            //if (objEditor.selectedObjects.Count > 0)
            //{
            //    objEditor.selectedObjects = (from x in objEditor.selectedObjects
            //                                 orderby x.Index ascending
            //                                 select x).ToList();
            //}

            if (SelectedObjectCount > 1)
                EditorManager.inst.ShowDialog("Multi Object Editor", false);

            if (SelectedObjectCount <= 0)
                CheckpointEditor.inst.SetCurrentCheckpoint(0);

            EditorManager.inst.DisplayNotification("Selection includes " + objEditor.selectedObjects.Count + " objects!", 1f, EditorManager.NotificationType.Success);
            RTEditor.inst.ienumRunning = false;
            yield break;
        }

        public IEnumerator GroupSelectKeyframes(bool _add = true)
        {
            RTEditor.inst.ienumRunning = true;

            float delay = 0f;
            bool flag = false;

            foreach (var timelineObject in RTEditor.inst.timelineBeatmapObjectKeyframes)
            {
                if (RTMath.RectTransformToScreenSpace(ObjEditor.inst.SelectionBoxImage.rectTransform).Overlaps(RTMath.RectTransformToScreenSpace(timelineObject.Image.rectTransform)))
                {
                    yield return new WaitForSeconds(delay);
                    SetCurrentKeyframe(CurrentBeatmapObjectSelection.Data, timelineObject.Type, timelineObject.Index, false, !flag || _add);
                    flag = true;
                    delay += 0.0001f;
                }
            }

            RTEditor.inst.ienumRunning = false;
            yield break;
        }

        public void DeselectAllObjects()
        {
            foreach (var timelineObject in RTEditor.inst.timelineBeatmapObjects)
            {
                timelineObject.Value.selected = false;
            }

            foreach (var timelineObject in RTEditor.inst.timelinePrefabObjects)
            {
                timelineObject.Value.selected = false;
            }
        }

        public void AddSelectedObject<T>(TimelineObject<T> timelineObject)
        {
            timelineObject.selected = true;

            if (SelectedObjectCount > 1)
            {
                EditorManager.inst.ClearDialogs(Array.Empty<EditorManager.EditorDialog.DialogType>());
                EditorManager.inst.ShowDialog("Multi Object Editor", false);
                if (timelineObject.IsBeatmapObject)
                    SetCurrentKeyframe(timelineObject.Data as BeatmapObject, 0, 0, false, false);

                RenderTimelineObject(timelineObject);
                return;
            }

            SetCurrentObject(timelineObject);
        }

        //public void AddSelectedObjectOnly(ObjectSelection _obj)
        //{
        //    if (ObjEditor.inst.ContainedInSelectedObjects(_obj))
        //        ObjEditor.inst.selectedObjects.Remove(_obj);
        //    else
        //        ObjEditor.inst.selectedObjects.Add(_obj);

        //    // 0 was originally 1
        //    if (ObjEditor.inst.selectedObjects.Count > 0)
        //    {
        //        if (ObjEditor.inst.ContainedInSelectedObjects(_obj))
        //            _obj.GetTimelineObject().GetComponent<Image>().color = ObjEditor.inst.SelectedColor;
        //        else
        //            _obj.GetTimelineObject().GetComponent<Image>().color = ObjEditor.inst.NormalColor;
        //    }
        //    // Not sure if this should be used.
        //    else if (ObjEditor.inst.currentObjectSelection == _obj)
        //        _obj.GetTimelineObject().GetComponent<Image>().color = ObjEditor.inst.SelectedColor;
        //    else
        //        _obj.GetTimelineObject().GetComponent<Image>().color = ObjEditor.inst.NormalColor;
        //}

        public void SetCurrentObject<T>(TimelineObject<T> timelineObject, bool bringTo = false)
        {
            if (DataManager.inst.gameData.beatmapObjects.Count > 0)
            {
                DeselectAllObjects();
                timelineObject.selected = true;

                if (!string.IsNullOrEmpty(timelineObject.ID))
                {
                    if (timelineObject.IsBeatmapObject)
                        ObjEditor.inst.OpenDialog();
                    if (timelineObject.IsPrefabObject)
                        PrefabEditor.inst.OpenPrefabDialog();
                }

                if (bringTo)
                    AudioManager.inst.SetMusicTime(timelineObject.Time);
            }
        }

        public static void SetCurrentBeatmapObject(int index) => inst.SetCurrentObject(RTEditor.inst.TimelineBeatmapObjects[index]);

        public void SetCurrentKeyframe(BeatmapObject beatmapObject, int _keyframe, bool _bringTo = false)
        {
            if (CurrentBeatmapObjectSelection)
            {
                SetCurrentKeyframe(beatmapObject, ObjEditor.inst.currentKeyframeKind, _keyframe, _bringTo, false);
            }
        }

        public void AddCurrentKeyframe(BeatmapObject beatmapObject, int _add, bool _bringTo = false)
        {
            if (!string.IsNullOrEmpty(beatmapObject.id))
            {
                if (_add == int.MaxValue)
                {
                    _add = 1000000;
                }

                int num = ObjEditor.inst.currentKeyframe + _add;
                if (num < 0)
                    num = 0;
                if (num > beatmapObject.events[ObjEditor.inst.currentKeyframeKind].Count - 1)
                    num = beatmapObject.events[ObjEditor.inst.currentKeyframeKind].Count - 1;

                SetCurrentKeyframe(beatmapObject, ObjEditor.inst.currentKeyframeKind, num, _bringTo);
            }
        }

        public void SetCurrentKeyframe(BeatmapObject beatmapObject, int _kind, int _keyframe, bool _bringTo = false, bool _shift = false)
        {
            if (!string.IsNullOrEmpty(beatmapObject.id))
            {
                if (!ObjEditor.inst.timelineKeyframesDrag)
                {
                    if (_shift)
                    {
                        var kf = RTEditor.inst.timelineBeatmapObjectKeyframes.Find(x => x.Type == _kind && x.Index == _keyframe);
                        if (kf)
                        {
                            ObjEditor.inst.selectedKeyframeOffsets.RemoveAt(RTEditor.inst.timelineBeatmapObjectKeyframes.FindIndex(x => x.Type == _kind && x.Index == _keyframe));
                            kf.selected = false;
                        }
                        else
                        {
                            ObjEditor.inst.selectedKeyframeOffsets.Add(0f);
                            var timelineObject = new TimelineObject<EventKeyframe>((EventKeyframe)beatmapObject.events[_kind][_keyframe]);
                            timelineObject.selected = true;
                            RTEditor.inst.timelineBeatmapObjectKeyframes.Add(timelineObject);
                        }
                    }
                    else
                    {
                        ObjEditor.inst.selectedKeyframeOffsets.Clear();
                        ObjEditor.inst.selectedKeyframeOffsets.Add(0f);

                        if (RTEditor.inst.timelineBeatmapObjectKeyframes.Count > 0)
                            RTEditor.inst.timelineBeatmapObjectKeyframes.ForEach(delegate (TimelineObject<EventKeyframe> x) { x.selected = false; });

                        var kf = RTEditor.inst.timelineBeatmapObjectKeyframes.Find(x => x.Type == _kind && x.Index == _keyframe);
                        if (kf)
                        {
                            kf.selected = true;
                        }
                        else
                        {
                            var timelineObject = new TimelineObject<EventKeyframe>((EventKeyframe)beatmapObject.events[_kind][_keyframe]);
                            timelineObject.selected = true;
                            RTEditor.inst.timelineBeatmapObjectKeyframes.Add(timelineObject);

                            CreateKeyframes(beatmapObject);
                        }
                    }
                }
                DataManager.inst.UpdateSettingInt("EditorObjKeyframeKind", _kind);
                DataManager.inst.UpdateSettingInt("EditorObjKeyframe", _keyframe);
                ObjEditor.inst.currentKeyframeKind = _kind;
                ObjEditor.inst.currentKeyframe = _keyframe;
                if (_bringTo)
                {
                    float value = beatmapObject.events[ObjEditor.inst.currentKeyframeKind][ObjEditor.inst.currentKeyframe].eventTime + beatmapObject.StartTime;
                    if (AllowTimeExactlyAtStart)
                        value = Mathf.Clamp(value, beatmapObject.StartTime + 0.001f, beatmapObject.StartTime + beatmapObject.GetObjectLifeLength());
                    else
                        value = Mathf.Clamp(value, beatmapObject.StartTime, beatmapObject.StartTime + beatmapObject.GetObjectLifeLength());

                    AudioManager.inst.SetMusicTime(Mathf.Clamp(value, 0f, AudioManager.inst.CurrentAudioSource.clip.length));
                    AudioManager.inst.CurrentAudioSource.Pause();
                    EditorManager.inst.UpdatePlayButton();
                }
                StartCoroutine(RefreshObjectGUI(beatmapObject));
            }
        }

        public int AddEvent(float _time, int _kind, EventKeyframe _keyframe)
        {
            var eventKeyframe = EventKeyframe.DeepCopy(_keyframe, true);
            eventKeyframe.eventTime = _time;

            if (_kind == 2)
            {
                eventKeyframe.SetEventValues(new float[2]);
                eventKeyframe.SetEventRandomValues(new float[3]);
            }

            var bm = CurrentBeatmapObjectSelection.Data;

            bm.events[_kind].Add(eventKeyframe);
            RenderTimelineObject(CurrentBeatmapObjectSelection);
            StartCoroutine(RefreshObjectGUI(bm));
            return bm.events[_kind].FindIndex(x => x.eventTime == _time);
        }

        #endregion

        #region Timeline Objects

        //public static GameObject RenderTimelineObject(ObjectSelection objectSelection)
        //{
        //    GameObject gameObject = null;

        //    if (!string.IsNullOrEmpty(objectSelection.ID))
        //    {
        //        if (!objectSelection.HasTimelineObject())
        //        {
        //            gameObject = CreateTimelineObject(objectSelection);
        //        }
        //        else
        //        {
        //            gameObject = objectSelection.GetTimelineObject();
        //        }

        //        if (EditorManager.inst.layer != objectSelection.GetLayer())
        //        {
        //            gameObject.SetActive(false);
        //        }
        //        else
        //        {
        //            bool locked = objectSelection.GetLocked();
        //            bool collapsed = objectSelection.GetCollapsed();
        //            var name = objectSelection.GetName();
        //            float startTime = objectSelection.GetStartTime();

        //            string nullName = "";

        //            var image = gameObject.GetComponent<Image>();

        //            Color color = ObjEditor.inst.NormalColor;
        //            if (objectSelection.TryGetObject(out BeatmapObject beatmapObject))
        //            {
        //                if (beatmapObject.objectType == ObjectType.Helper || beatmapObject.objectType == ObjectType.Decoration || beatmapObject.objectType == ObjectType.Empty)
        //                    image.type = Image.Type.Tiled;
        //                else
        //                    image.type = Image.Type.Simple;

        //                switch (beatmapObject.objectType)
        //                {
        //                    case ObjectType.Helper:
        //                        image.sprite = ObjEditor.inst.HelperSprite;
        //                        break;
        //                    case ObjectType.Decoration:
        //                        image.sprite = ObjEditor.inst.DecorationSprite;
        //                        break;
        //                    case ObjectType.Empty:
        //                        image.sprite = ObjEditor.inst.EmptySprite;
        //                        break;
        //                    default:
        //                        image.sprite = null;
        //                        break;
        //                }

        //                if (!string.IsNullOrEmpty(beatmapObject.prefabID))
        //                {
        //                    if (DataManager.inst.gameData.prefabs.FindIndex(x => x.ID == beatmapObject.prefabID) != -1)
        //                    {
        //                        color = DataManager.inst.PrefabTypes[DataManager.inst.gameData.prefabs.Find(x => x.ID == beatmapObject.prefabID).Type].Color;
        //                    }
        //                    else
        //                    {
        //                        DataManager.inst.gameData.beatmapObjects[objectSelection.Index].prefabID = null;
        //                        DataManager.inst.gameData.beatmapObjects[objectSelection.Index].prefabInstanceID = null;
        //                    }
        //                }
        //            }

        //            if (objectSelection.TryGetPrefabObject(out PrefabObject prefabObject) && objectSelection.TryGetPrefab(out Prefab prefab))
        //            {
        //                startTime += prefab.Offset;
        //                image.sprite = null;
        //                image.type = Image.Type.Simple;
        //                color = DataManager.inst.PrefabTypes[prefab.Type].Color;
        //                nullName = DataManager.inst.PrefabTypes[prefab.Type].Name;
        //            }

        //            gameObject.GetComponentInChildren<TextMeshProUGUI>().text = ((!string.IsNullOrEmpty(name)) ? string.Format("<mark=#000000aa>{0}</mark>", name) : nullName);

        //            if (ObjEditor.inst.ContainedInSelectedObjects(objectSelection))
        //                image.color = ObjEditor.inst.SelectedColor;
        //            else
        //                image.color = color;

        //            if (locked && gameObject.transform.Find("icons/lock") == null)
        //            {
        //                var @lock = ObjEditor.inst.timelineObjectPrefabLock.Duplicate(gameObject.transform.Find("icons"));
        //                @lock.name = "lock";
        //            }
        //            //else if (locked && gameObject.transform.Find("icons/lock") != null)
        //            //{
        //            //    gameObject.transform.Find("icons/lock").GetComponent<RectTransform>().localScale = Vector2.one;
        //            //    gameObject.transform.Find("icons/lock").GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        //            //}
        //            else if (!locked && gameObject.transform.Find("icons/lock") != null)
        //                Destroy(gameObject.transform.Find("icons/lock").gameObject);

        //            if (collapsed && gameObject.transform.Find("icons/dots") == null)
        //            {
        //                var dots = ObjEditor.inst.timelineObjectPrefabDots.Duplicate(gameObject.transform.Find("icons"));
        //                dots.name = "dots";
        //            }
        //            //else if (collapsed && gameObject4.transform.Find("icons/dots") != null)
        //            //{
        //            //    gameObject4.transform.Find("icons/dots").GetComponent<RectTransform>().localScale = Vector2.one;
        //            //    gameObject4.transform.Find("icons/dots").GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        //            //}
        //            else if (!collapsed && gameObject.transform.Find("icons/dots") != null)
        //                Destroy(gameObject.transform.Find("icons/dots").gameObject);

        //            float offset = objectSelection.GetLifeLength();
        //            float zoom = EditorManager.inst.Zoom;
        //            if (offset <= 0.4f)
        //                offset = 0.4f * zoom;
        //            else
        //                offset *= zoom;

        //            var rectTransform = gameObject.GetComponent<RectTransform>();
        //            rectTransform.sizeDelta = new Vector2(offset, 20f);
        //            rectTransform.anchoredPosition = new Vector2(startTime * EditorManager.inst.Zoom, (float)(-20 * Mathf.Clamp(objectSelection.GetBin(), 0, 14)));

        //            gameObject.GetComponentInChildren<TextMeshProUGUI>().color = LSColors.white;
        //            gameObject.SetActive(true);
        //        }
        //    }
        //    return gameObject;
        //}

        public static GameObject RenderTimelineObject<T>(TimelineObject<T> timelineObject)
        {
            GameObject gameObject = null;

            if (!string.IsNullOrEmpty(timelineObject.ID))
            {
                if (!timelineObject.GameObject)
                    gameObject = CreateTimelineObject(timelineObject);
                else
                    gameObject = timelineObject.GameObject;

                if (RTEditor.inst.Layer == timelineObject.Layer)
                {
                    bool locked = false;
                    bool collapsed = false;
                    int bin = 0;
                    string name = "object name";
                    float startTime = 0f;
                    float offset = 0f;

                    string nullName = "";

                    var image = gameObject.GetComponent<Image>();

                    Color color = ObjEditor.inst.NormalColor;
                    if (timelineObject.IsBeatmapObject)
                    {
                        if (!RTEditor.inst.timelineBeatmapObjects.ContainsKey(timelineObject.ID))
                            RTEditor.inst.timelineBeatmapObjects.Add(timelineObject.ID, timelineObject as TimelineObject<BeatmapObject>);

                        var beatmapObject = timelineObject.Data as BaseBeatmapObject;

                        locked = beatmapObject.editorData.locked;
                        collapsed = beatmapObject.editorData.collapse;
                        bin = beatmapObject.editorData.Bin;
                        name = beatmapObject.name;
                        startTime = beatmapObject.StartTime;
                        offset = beatmapObject.GetObjectLifeLength(_takeCollapseIntoConsideration: true);

                        if (beatmapObject.objectType == ObjectType.Helper || beatmapObject.objectType == ObjectType.Decoration || beatmapObject.objectType == ObjectType.Empty)
                            image.type = Image.Type.Tiled;
                        else
                            image.type = Image.Type.Simple;

                        switch (beatmapObject.objectType)
                        {
                            case ObjectType.Helper:
                                image.sprite = ObjEditor.inst.HelperSprite;
                                break;
                            case ObjectType.Decoration:
                                image.sprite = ObjEditor.inst.DecorationSprite;
                                break;
                            case ObjectType.Empty:
                                image.sprite = ObjEditor.inst.EmptySprite;
                                break;
                            default:
                                image.sprite = null;
                                break;
                        }

                        if (!string.IsNullOrEmpty(beatmapObject.prefabID))
                        {
                            if (DataManager.inst.gameData.prefabs.FindIndex(x => x.ID == beatmapObject.prefabID) != -1)
                            {
                                color = DataManager.inst.PrefabTypes[DataManager.inst.gameData.prefabs.Find(x => x.ID == beatmapObject.prefabID).Type].Color;
                            }
                            else
                            {
                                beatmapObject.prefabID = null;
                                beatmapObject.prefabInstanceID = null;
                            }
                        }
                    }

                    if (timelineObject.IsPrefabObject)
                    {
                        if (!RTEditor.inst.timelinePrefabObjects.ContainsKey(timelineObject.ID))
                            RTEditor.inst.timelinePrefabObjects.Add(timelineObject.ID, timelineObject as TimelineObject<PrefabObject>);

                        var prefabObject = timelineObject.Data as BasePrefabObject;
                        var prefab = DataManager.inst.gameData.prefabs.Find(x => x.ID == prefabObject.prefabID);

                        locked = prefabObject.editorData.locked;
                        collapsed = prefabObject.editorData.collapse;
                        bin = prefabObject.editorData.Bin;
                        name = prefab.Name;
                        startTime = prefabObject.StartTime + prefab.Offset;
                        offset = prefab.GetPrefabLifeLength(timelineObject.Data as BasePrefabObject, true);
                        image.sprite = null;
                        image.type = Image.Type.Simple;
                        color = DataManager.inst.PrefabTypes[prefab.Type].Color;
                        nullName = DataManager.inst.PrefabTypes[prefab.Type].Name;
                    }

                    gameObject.GetComponentInChildren<TextMeshProUGUI>().text = (!string.IsNullOrEmpty(name)) ? string.Format("<mark=#000000aa>{0}</mark>", name) : nullName;

                    if (timelineObject.selected)
                        image.color = ObjEditor.inst.SelectedColor;
                    else
                        image.color = color;

                    if (locked && gameObject.transform.Find("icons/lock") == null)
                    {
                        var @lock = ObjEditor.inst.timelineObjectPrefabLock.Duplicate(gameObject.transform.Find("icons"));
                        @lock.name = "lock";
                    }
                    //else if (locked && gameObject.transform.Find("icons/lock") != null)
                    //{
                    //    gameObject.transform.Find("icons/lock").GetComponent<RectTransform>().localScale = Vector2.one;
                    //    gameObject.transform.Find("icons/lock").GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                    //}
                    else if (!locked && gameObject.transform.Find("icons/lock") != null)
                        Destroy(gameObject.transform.Find("icons/lock").gameObject);

                    if (collapsed && gameObject.transform.Find("icons/dots") == null)
                    {
                        var dots = ObjEditor.inst.timelineObjectPrefabDots.Duplicate(gameObject.transform.Find("icons"));
                        dots.name = "dots";
                    }
                    //else if (collapsed && gameObject4.transform.Find("icons/dots") != null)
                    //{
                    //    gameObject4.transform.Find("icons/dots").GetComponent<RectTransform>().localScale = Vector2.one;
                    //    gameObject4.transform.Find("icons/dots").GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                    //}
                    else if (!collapsed && gameObject.transform.Find("icons/dots") != null)
                        Destroy(gameObject.transform.Find("icons/dots").gameObject);


                    float zoom = EditorManager.inst.Zoom;
                    if (offset <= 0.4f)
                        offset = 0.4f * zoom;
                    else
                        offset *= zoom;

                    var rectTransform = gameObject.GetComponent<RectTransform>();
                    rectTransform.sizeDelta = new Vector2(offset, 20f);
                    rectTransform.anchoredPosition = new Vector2(startTime * EditorManager.inst.Zoom, (float)(-20 * Mathf.Clamp(bin, 0, 14)));

                    gameObject.GetComponentInChildren<TextMeshProUGUI>().color = LSColors.white;
                    gameObject.SetActive(true);
                }
            }
            return gameObject;
        }

        public static void RenderTimelineObjects(string _both = "")
        {
            if (DataManager.inst.gameData.beatmapObjects != null && DataManager.inst.gameData.beatmapObjects.Count > 0)
            {
                if (_both == "" || _both == "obj")
                {
                    //foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
                    //{
                    //    var objectSelection = new ObjectSelection(ObjectSelection.SelectionType.Object, beatmapObject.id);
                    //    if (beatmapObject.editorData.Layer == EditorManager.inst.layer && !beatmapObject.fromPrefab)
                    //    {
                    //        RenderTimelineObject(objectSelection);
                    //    }
                    //    else if (objectSelection.HasTimelineObject())
                    //    {
                    //        objectSelection.GetTimelineObject().SetActive(false);
                    //    }
                    //}

                    foreach (var timelineObject in RTEditor.inst.TimelineBeatmapObjects)
                    {
                        RenderTimelineObject(timelineObject);
                    }
                }
                if (_both == "" || _both == "prefab")
                {
                    //foreach (var prefabObject in DataManager.inst.gameData.prefabObjects)
                    //{
                    //    var objectSelection = new ObjectSelection(ObjectSelection.SelectionType.Prefab, prefabObject.ID);
                    //    if (prefabObject.editorData.Layer == EditorManager.inst.layer)
                    //    {
                    //        RenderTimelineObject(objectSelection);
                    //    }
                    //    else if (objectSelection.HasTimelineObject())
                    //    {
                    //        objectSelection.GetTimelineObject().SetActive(false);
                    //    }
                    //}

                    foreach (var timelineObject in RTEditor.inst.TimelinePrefabObjects)
                    {
                        RenderTimelineObject(timelineObject);
                    }
                }
            }
        }

        //public static GameObject CreateTimelineObject(ObjectSelection objectSelection)
        //{
        //    GameObject gameObject = null;

        //    if (!string.IsNullOrEmpty(objectSelection.ID))
        //    {
        //        if (ObjEditor.inst.beatmapObjects.ContainsKey(objectSelection.ID))
        //            Destroy(ObjEditor.inst.beatmapObjects[objectSelection.ID]);
        //        if (ObjEditor.inst.prefabObjects.ContainsKey(objectSelection.ID))
        //            Destroy(ObjEditor.inst.prefabObjects[objectSelection.ID]);

        //        gameObject = ObjEditor.inst.timelineObjectPrefab.Duplicate(EditorManager.inst.timeline.transform);
        //        gameObject.name = objectSelection.GetName();

        //        ObjectSelection obj = null;
        //        if (objectSelection.IsObject())
        //        {
        //            if (ObjEditor.inst.beatmapObjects.ContainsKey(objectSelection.ID))
        //                ObjEditor.inst.beatmapObjects[objectSelection.ID] = gameObject;
        //            else
        //                ObjEditor.inst.beatmapObjects.Add(objectSelection.ID, gameObject);
        //            obj = new ObjectSelection(ObjectSelection.SelectionType.Object, objectSelection.ID);
        //        }

        //        if (objectSelection.IsPrefab())
        //        {
        //            if (ObjEditor.inst.prefabObjects.ContainsKey(objectSelection.ID))
        //                ObjEditor.inst.prefabObjects[objectSelection.ID] = gameObject;
        //            else
        //                ObjEditor.inst.prefabObjects.Add(objectSelection.ID, gameObject);
        //            obj = new ObjectSelection(ObjectSelection.SelectionType.Prefab, objectSelection.ID);
        //        }

        //        Triggers.AddEventTrigger(gameObject, new List<EventTrigger.Entry>
        //        {
        //            Triggers.CreateBeatmapObjectTrigger(obj),
        //            Triggers.CreateBeatmapObjectStartDragTrigger(obj),
        //            Triggers.CreateBeatmapObjectEndDragTrigger(obj)
        //        });
        //    }

        //    //if (gameObject != null)
        //    //{
        //    //    var hoverUI = gameObject.AddComponent<HoverUI>();
        //    //    hoverUI.animatePos = false;
        //    //    hoverUI.animateSca = true;
        //    //    hoverUI.size = ConfigEntries.TimelineObjectHoverSize.Value;
        //    //}

        //    return gameObject;
        //}

        public static GameObject CreateTimelineObject<T>(TimelineObject<T> timelineObject)
        {
            GameObject gameObject = null;

            if (!string.IsNullOrEmpty(timelineObject.ID))
            {
                if (timelineObject.IsBeatmapObject && !RTEditor.inst.timelineBeatmapObjects.ContainsKey(timelineObject.ID))
                    RTEditor.inst.timelineBeatmapObjects.Add(timelineObject.ID, timelineObject as TimelineObject<BeatmapObject>);
                if (timelineObject.IsPrefabObject && !RTEditor.inst.timelinePrefabObjects.ContainsKey(timelineObject.ID))
                    RTEditor.inst.timelinePrefabObjects.Add(timelineObject.ID, timelineObject as TimelineObject<PrefabObject>);

                if (timelineObject.GameObject)
                    Destroy(timelineObject.GameObject);

                gameObject = ObjEditor.inst.timelineObjectPrefab.Duplicate(EditorManager.inst.timeline.transform);
                if (timelineObject.IsBeatmapObject)
                    gameObject.name = (timelineObject.Data as BaseBeatmapObject).name;
                if (timelineObject.IsPrefabObject && DataManager.inst.gameData.prefabs.Find(x => (timelineObject.Data as BasePrefabObject).prefabID == x.ID) != null)
                    gameObject.name = DataManager.inst.gameData.prefabs.Find(x => (timelineObject.Data as BasePrefabObject).prefabID == x.ID).Name;

                timelineObject.GameObject = gameObject;
                timelineObject.Image = gameObject.GetComponent<Image>();

                TriggerHelper.AddEventTrigger(gameObject, new List<EventTrigger.Entry>
                {
                    TriggerHelper.CreateBeatmapObjectTrigger(timelineObject),
                    TriggerHelper.CreateBeatmapObjectStartDragTrigger(timelineObject),
                    TriggerHelper.CreateBeatmapObjectEndDragTrigger(timelineObject)
                });
            }

            //if (gameObject != null)
            //{
            //    var hoverUI = gameObject.AddComponent<HoverUI>();
            //    hoverUI.animatePos = false;
            //    hoverUI.animateSca = true;
            //    hoverUI.size = ConfigEntries.TimelineObjectHoverSize.Value;
            //}

            return gameObject;
        }

        public static void CreateTimelineObjects()
        {
            //if (ObjEditor.inst.beatmapObjects.Count > 0)
            //{
            //    foreach (var keyValuePair in ObjEditor.inst.beatmapObjects)
            //        Destroy(keyValuePair.Value);
            //    ObjEditor.inst.beatmapObjects.Clear();
            //}

            //if (ObjEditor.inst.prefabObjects.Count > 0)
            //{
            //    foreach (var keyValuePair2 in ObjEditor.inst.prefabObjects)
            //        Destroy(keyValuePair2.Value);
            //    ObjEditor.inst.prefabObjects.Clear();
            //}

            foreach (var timelineObject in RTEditor.inst.TimelineBeatmapObjects)
            {
                Destroy(timelineObject.GameObject);
            }
            RTEditor.inst.TimelineBeatmapObjects.Clear();

            foreach (var timelineObject in RTEditor.inst.TimelinePrefabObjects)
            {
                Destroy(timelineObject.GameObject);
            }
            RTEditor.inst.TimelinePrefabObjects.Clear();

            foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
            {
                //var objectSelection = new ObjectSelection(ObjectSelection.SelectionType.Object, beatmapObject.id);
                //if (!string.IsNullOrEmpty(objectSelection.ID) && objectSelection.Index != -1)
                //    CreateTimelineObject(objectSelection);

                if (!string.IsNullOrEmpty(beatmapObject.id))
                {
                    var timelineObject = new TimelineObject<BaseBeatmapObject>(beatmapObject);
                    CreateTimelineObject(timelineObject);
                }
            }

            foreach (var prefabObject in DataManager.inst.gameData.prefabObjects)
            {
                //var objectSelection2 = new ObjectSelection(ObjectSelection.SelectionType.Prefab, prefabObject.ID);
                //if (!string.IsNullOrEmpty(objectSelection2.ID) && objectSelection2.Index != -1)
                //    CreateTimelineObject(objectSelection2);

                if (!string.IsNullOrEmpty(prefabObject.ID))
                {
                    var timelineObject = new TimelineObject<BasePrefabObject>(prefabObject);
                    CreateTimelineObject(timelineObject);
                }
            }
        }

        #endregion
        
        #region RefreshObjectGUI

        public static bool UpdateObjects => true;

        Dictionary<string, object> objectUIElements;
        public Dictionary<string, object> ObjectUIElements
        {
            get
            {
                if (objectUIElements == null || objectUIElements.Count == 0 || objectUIElements.Any(x => x.Value == null))
                {
                    var objEditor = ObjEditor.inst;
                    var tfv = objEditor.ObjectView.transform;
                    objectUIElements.Add("ID Text", tfv.Find("id/text")?.GetComponent<Text>());
                    objectUIElements.Add("Name IF", tfv.Find("name/name").GetComponent<InputField>());
                    objectUIElements.Add("Object Type DD", tfv.Find("name/object-type").GetComponent<Dropdown>());

                    objectUIElements.Add("Start Time ET", tfv.Find("time").GetComponent<EventTrigger>());
                    objectUIElements.Add("Start Time IF", tfv.Find("time/time").GetComponent<InputField>());
                    objectUIElements.Add("Start Time Lock", tfv.Find("time/lock")?.GetComponent<Toggle>());
                    objectUIElements.Add("Start Time <<", tfv.Find("time/<<").GetComponent<Button>());
                    objectUIElements.Add("Start Time <", tfv.Find("time/<").GetComponent<Button>());
                    objectUIElements.Add("Start Time |", tfv.Find("time/|").GetComponent<Button>());
                    objectUIElements.Add("Start Time >", tfv.Find("time/>").GetComponent<Button>());
                    objectUIElements.Add("Start Time >>", tfv.Find("time/>>").GetComponent<Button>());

                    objectUIElements.Add("Autokill TOD DD", tfv.Find("autokill/tod-dropdown").GetComponent<Dropdown>());
                    objectUIElements.Add("Autokill TOD IF", tfv.Find("autokill/tod-value").GetComponent<Dropdown>());
                    objectUIElements.Add("Autokill TOD Value", tfv.Find("autokill/tod-value"));
                    objectUIElements.Add("Autokill TOD Set", tfv.Find("autokill/|"));
                    objectUIElements.Add("Autokill TOD Set B", tfv.Find("autokill/|").GetComponent<Button>());
                    objectUIElements.Add("Autokill Collapse", tfv.Find("autokill/collapse").GetComponent<Toggle>());

                    objectUIElements.Add("Parent Name", tfv.Find("parent/text/text").GetComponent<Text>());
                    objectUIElements.Add("Parent Select", tfv.Find("parent/text").GetComponent<Button>());
                    objectUIElements.Add("Parent Info", tfv.Find("parent/text").GetComponent<HoverTooltip>());
                    objectUIElements.Add("Parent More B", tfv.Find("parent/more").GetComponent<Button>());
                    objectUIElements.Add("Parent More", tfv.Find("parent_more"));
                    objectUIElements.Add("Parent Search Open", tfv.Find("parent/parent").GetComponent<Button>());
                    objectUIElements.Add("Parent Clear", tfv.Find("parent/clear parent").GetComponent<Button>());
                    objectUIElements.Add("Parent Picker", tfv.Find("parent/parent picker").GetComponent<Button>());

                    objectUIElements.Add("Parent Offset 1", tfv.Find("parent_more/pos_row"));
                    objectUIElements.Add("Parent Offset 2", tfv.Find("parent_more/sca_row"));
                    objectUIElements.Add("Parent Offset 3", tfv.Find("parent_more/rot_row"));

                    objectUIElements.Add("Origin X IF", tfv.Find("origin/x").GetComponent<InputField>());
                    objectUIElements.Add("Origin X <", tfv.Find("origin/x/<").GetComponent<InputField>());
                    objectUIElements.Add("Origin X >", tfv.Find("origin/x/>").GetComponent<InputField>());
                    objectUIElements.Add("Origin Y IF", tfv.Find("origin/y").GetComponent<InputField>());
                    objectUIElements.Add("Origin Y <", tfv.Find("origin/y/<").GetComponent<InputField>());
                    objectUIElements.Add("Origin Y >", tfv.Find("origin/y/>").GetComponent<InputField>());

                    objectUIElements.Add("Shape", tfv.Find("shape"));
                    objectUIElements.Add("Shape Settings", tfv.Find("shapesettings"));

                    objectUIElements.Add("Depth Slider", tfv.Find("depth/depth").GetComponent<Slider>());
                    objectUIElements.Add("Depth IF", tfv.Find("spacer/depth")?.GetComponent<InputField>());
                    objectUIElements.Add("Depth <", tfv.Find("spacer/depth/<")?.GetComponent<Button>());
                    objectUIElements.Add("Depth >", tfv.Find("spacer/depth/>")?.GetComponent<Button>());

                    objectUIElements.Add("Bin Slider", tfv.Find("editor/bin").GetComponent<Slider>());
                    objectUIElements.Add("Layers IF", tfv.Find("editor/layers")?.GetComponent<InputField>());
                    objectUIElements.Add("Layers Image", tfv.Find("editor/layers")?.GetComponent<Image>());

                    objectUIElements.Add("Collapse Label", tfv.Find("collapselabel").gameObject);
                    objectUIElements.Add("Collapse Prefab", tfv.Find("applyprefab").gameObject);
                }

                return objectUIElements;
            }
            set => objectUIElements = value;
        }

        /// <summary>
        /// Refreshes the Object Editor to the specified BeatmapObject, allowing for any object to be edited from anywhere.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        /// <returns></returns>
        public static IEnumerator RefreshObjectGUI(BeatmapObject beatmapObject)
        {
            if (EditorManager.inst.hasLoadedLevel && !string.IsNullOrEmpty(beatmapObject.id))
            {
                inst.CurrentSelection = beatmapObject;

                inst.RenderID(beatmapObject);
                inst.RenderName(beatmapObject);
                inst.RenderObjectType(beatmapObject);

                inst.RenderStartTime(beatmapObject);
                inst.RenderAutokill(beatmapObject);

                inst.RenderParent(beatmapObject);

                inst.RenderOrigin(beatmapObject);

                inst.RenderDepth(beatmapObject);

                inst.RenderLayers(beatmapObject);
                inst.RenderBin(beatmapObject);

                inst.RenderGameObjectInspector(beatmapObject);

                bool fromPrefab = !string.IsNullOrEmpty(beatmapObject.prefabID);
                ((GameObject)inst.ObjectUIElements["Collapse Label"]).SetActive(fromPrefab);
                ((GameObject)inst.ObjectUIElements["Collapse Prefab"]).SetActive(fromPrefab);

                inst.RenderKeyframeDialog(beatmapObject);
                inst.RenderKeyframes(beatmapObject);
            }

            yield break;
        }

        /// <summary>
        /// Renders the ID Text.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        void RenderID(BeatmapObject beatmapObject)
        {
            var idText = (Text)ObjectUIElements["ID Text"];
            idText.text = beatmapObject.id;
        }

        /// <summary>
        /// Renders the Name InputField.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        void RenderName(BeatmapObject beatmapObject)
        {
            var name = (InputField)ObjectUIElements["Name IF"];

            // Allows for left / right flipping.
            if (!name.GetComponent<InputFieldSwapper>() && name.gameObject)
            {
                var t = name.gameObject.AddComponent<InputFieldSwapper>();
                t.Init(name, InputFieldSwapper.Type.String);
            }

            name.onValueChanged.ClearAll(
                );
            name.text = beatmapObject.name;
            name.onValueChanged.AddListener(delegate (string _val)
            {
                beatmapObject.name = _val;

                // Since name has no effect on the physical object, we will only need to update the timeline object.
                if (RTEditor.inst.timelineBeatmapObjects.ContainsKey(beatmapObject.id))
                    RenderTimelineObject(RTEditor.inst.timelineBeatmapObjects[beatmapObject.id]);
            });
        }

        bool setTypes;
        /// <summary>
        /// Renders the ObjectType Dropdown.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        void RenderObjectType(BeatmapObject beatmapObject)
        {
            var objType = (Dropdown)ObjectUIElements["Object Type DD"];

            // if the new ObjectTypes hasn't been set yet.
            if (!setTypes)
            {
                setTypes = true;
                objType.options = new List<Dropdown.OptionData>
                {
                    new Dropdown.OptionData("Normal"),
                    new Dropdown.OptionData("Helper"),
                    new Dropdown.OptionData("Decoration"),
                    new Dropdown.OptionData("Empty"),
                    new Dropdown.OptionData("Solid")
                };
            }

            objType.onValueChanged.RemoveAllListeners();
            objType.value = (int)beatmapObject.objectType;
            objType.onValueChanged.AddListener(delegate (int _val)
            {
                beatmapObject.objectType = (ObjectType)_val;

                // ObjectType affects both physical object and timeline object.
                if (RTEditor.inst.timelineBeatmapObjects.ContainsKey(beatmapObject.id))
                    RenderTimelineObject(RTEditor.inst.timelineBeatmapObjects[beatmapObject.id]);
                if (UpdateObjects)
                    Updater.UpdateProcessor(beatmapObject);
            });
        }

        /// <summary>
        /// Renders all StartTime UI.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        void RenderStartTime(BeatmapObject beatmapObject)
        {
            var time = (EventTrigger)ObjectUIElements["Start Time ET"];
            var timeIF = (InputField)ObjectUIElements["Start Time IF"];
            var locker = (Toggle)ObjectUIElements["Start Time Lock"];
            var timeJumpLargeLeft = (Button)ObjectUIElements["Start Time <<"];
            var timeJumpLeft = (Button)ObjectUIElements["Start Time <"];
            var setStartToTime = (Button)ObjectUIElements["Start Time |"];
            var timeJumpRight = (Button)ObjectUIElements["Start Time >"];
            var timeJumpLargeRight = (Button)ObjectUIElements["Start Time >>"];

            locker.onValueChanged.RemoveAllListeners();
            locker.isOn = beatmapObject.editorData.locked;
            locker.onValueChanged.AddListener(delegate (bool _val)
            {
                beatmapObject.editorData.locked = _val;

                // Since locking has no effect on the physical object, we will only need to update the timeline object.
                if (RTEditor.inst.timelineBeatmapObjects.ContainsKey(beatmapObject.id))
                    RenderTimelineObject(RTEditor.inst.timelineBeatmapObjects[beatmapObject.id]);
            });

            timeIF.onValueChanged.ClearAll();
            timeIF.text = beatmapObject.StartTime.ToString();
            timeIF.onValueChanged.AddListener(delegate (string _val)
            {
                if (float.TryParse(_val, out float num))
                {
                    beatmapObject.StartTime = Mathf.Clamp(num, 0f, AudioManager.inst.CurrentAudioSource.clip.length);

                    // StartTime affects both physical object and timeline object.
                    if (RTEditor.inst.timelineBeatmapObjects.ContainsKey(beatmapObject.id))
                        RenderTimelineObject(RTEditor.inst.timelineBeatmapObjects[beatmapObject.id]);
                    if (UpdateObjects)
                        Updater.UpdateProcessor(beatmapObject, "StartTime");
                }
            });

            time.triggers.Clear();
            time.triggers.Add(TriggerHelper.ScrollDelta(timeIF, max: AudioManager.inst.CurrentAudioSource.clip.length));

            timeJumpLargeLeft.onClick.RemoveAllListeners();
            timeJumpLargeLeft.interactable = (beatmapObject.StartTime > 0f);
            timeJumpLargeLeft.onClick.AddListener(delegate ()
            {
                float moveTime = beatmapObject.StartTime - 1f;
                moveTime = Mathf.Clamp(moveTime, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                timeIF.text = moveTime.ToString();

                // StartTime affects both physical object and timeline object.
                if (RTEditor.inst.timelineBeatmapObjects.ContainsKey(beatmapObject.id))
                    RenderTimelineObject(RTEditor.inst.timelineBeatmapObjects[beatmapObject.id]);
                if (UpdateObjects)
                    Updater.UpdateProcessor(beatmapObject, "StartTime");

                ResizeKeyframeTimeline(beatmapObject);
            });

            timeJumpLeft.onClick.RemoveAllListeners();
            timeJumpLeft.interactable = (beatmapObject.StartTime > 0f);
            timeJumpLeft.onClick.AddListener(delegate ()
            {
                float moveTime = beatmapObject.StartTime - 0.1f;
                moveTime = Mathf.Clamp(moveTime, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                timeIF.text = moveTime.ToString();

                // StartTime affects both physical object and timeline object.
                if (RTEditor.inst.timelineBeatmapObjects.ContainsKey(beatmapObject.id))
                    RenderTimelineObject(RTEditor.inst.timelineBeatmapObjects[beatmapObject.id]);
                if (UpdateObjects)
                    Updater.UpdateProcessor(beatmapObject, "StartTime");

                ResizeKeyframeTimeline(beatmapObject);
            });

            setStartToTime.onClick.RemoveAllListeners();
            setStartToTime.onClick.AddListener(delegate ()
            {
                timeIF.text = EditorManager.inst.CurrentAudioPos.ToString();

                // StartTime affects both physical object and timeline object.
                if (RTEditor.inst.timelineBeatmapObjects.ContainsKey(beatmapObject.id))
                    RenderTimelineObject(RTEditor.inst.timelineBeatmapObjects[beatmapObject.id]);
                if (UpdateObjects)
                    Updater.UpdateProcessor(beatmapObject, "StartTime");

                ResizeKeyframeTimeline(beatmapObject);
            });

            timeJumpRight.onClick.RemoveAllListeners();
            timeJumpRight.onClick.AddListener(delegate ()
            {
                float moveTime = beatmapObject.StartTime + 0.1f;
                moveTime = Mathf.Clamp(moveTime, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                timeIF.text = moveTime.ToString();

                // StartTime affects both physical object and timeline object.
                if (RTEditor.inst.timelineBeatmapObjects.ContainsKey(beatmapObject.id))
                    RenderTimelineObject(RTEditor.inst.timelineBeatmapObjects[beatmapObject.id]);
                if (UpdateObjects)
                    Updater.UpdateProcessor(beatmapObject, "StartTime");

                ResizeKeyframeTimeline(beatmapObject);
            });

            timeJumpLargeRight.onClick.RemoveAllListeners();
            timeJumpLargeRight.onClick.AddListener(delegate ()
            {
                float moveTime = beatmapObject.StartTime + 1f;
                moveTime = Mathf.Clamp(moveTime, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                timeIF.text = moveTime.ToString();

                // StartTime affects both physical object and timeline object.
                if (RTEditor.inst.timelineBeatmapObjects.ContainsKey(beatmapObject.id))
                    RenderTimelineObject(RTEditor.inst.timelineBeatmapObjects[beatmapObject.id]);
                if (UpdateObjects)
                    Updater.UpdateProcessor(beatmapObject, "StartTime");

                ResizeKeyframeTimeline(beatmapObject);
            });
        }

        /// <summary>
        /// Renders all Autokill UI.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        void RenderAutokill(BeatmapObject beatmapObject)
        {
            var akType = (Dropdown)ObjectUIElements["Autokill TOD DD"];
            akType.onValueChanged.ClearAll();
            akType.value = (int)beatmapObject.autoKillType;
            akType.onValueChanged.AddListener(delegate (int _val)
            {
                beatmapObject.autoKillType = (AutoKillType)_val;
                // AutoKillType affects both physical object and timeline object.
                if (RTEditor.inst.timelineBeatmapObjects.ContainsKey(beatmapObject.id))
                    RenderTimelineObject(RTEditor.inst.timelineBeatmapObjects[beatmapObject.id]);
                if (UpdateObjects)
                    Updater.UpdateProcessor(beatmapObject, "Autokill");

            });

            var todValue = (Transform)ObjectUIElements["Autokill TOD Value"];
            var akOffset = todValue.GetComponent<InputField>();
            var akset = (Transform)ObjectUIElements["Autokill TOD Set"];
            var aksetButt = (Button)ObjectUIElements["Autokill TOD Set B"];

            if (beatmapObject.autoKillType == AutoKillType.FixedTime ||
                beatmapObject.autoKillType == AutoKillType.SongTime ||
                beatmapObject.autoKillType == AutoKillType.LastKeyframeOffset)
            {
                todValue.gameObject.SetActive(true);

                akOffset.onValueChanged.RemoveAllListeners();
                akOffset.text = beatmapObject.autoKillOffset.ToString();
                akOffset.onValueChanged.AddListener(delegate (string _value)
                {
                    float num = float.Parse(_value);
                    if (beatmapObject.autoKillType == AutoKillType.SongTime)
                    {
                        float startTime = beatmapObject.StartTime;
                        if (num < startTime)
                            num = startTime + 0.1f;
                    }

                    if (num < 0f)
                        num = 0f;

                    beatmapObject.autoKillOffset = num;

                    // AutoKillType affects both physical object and timeline object.
                    if (RTEditor.inst.timelineBeatmapObjects.ContainsKey(beatmapObject.id))
                    {
                        RenderTimelineObject(RTEditor.inst.timelineBeatmapObjects[beatmapObject.id]);
                        if (UpdateObjects)
                            Updater.UpdateProcessor(beatmapObject, "Autokill");
                    }
                });

                akset.gameObject.SetActive(true);
                aksetButt.onClick.RemoveAllListeners();
                aksetButt.onClick.AddListener(delegate ()
                {
                    float num = 0f;

                    if (beatmapObject.autoKillType == AutoKillType.SongTime)
                        num = AudioManager.inst.CurrentAudioSource.time;
                    else num = AudioManager.inst.CurrentAudioSource.time - beatmapObject.StartTime;

                    if (num < 0f)
                        num = 0f;

                    beatmapObject.autoKillOffset = num;

                    // AutoKillType affects both physical object and timeline object.
                    if (RTEditor.inst.timelineBeatmapObjects.ContainsKey(beatmapObject.id))
                    {
                        RenderTimelineObject(RTEditor.inst.timelineBeatmapObjects[beatmapObject.id]);
                        if (UpdateObjects)
                            Updater.UpdateProcessor(beatmapObject, "Autokill");
                    }
                });

                // Add Scrolling for easy changing of values.
                TriggerHelper.AddEventTrigger(todValue.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(akOffset, 0.1f, 10f, 0f, float.PositiveInfinity) });
            }
            else
            {
                todValue.gameObject.SetActive(false);
                akOffset.onValueChanged.RemoveAllListeners();
                akset.gameObject.SetActive(false);
                aksetButt.onClick.RemoveAllListeners();
            }

            var collapse = (Toggle)ObjectUIElements["Autokill Collapse"];

            collapse.onValueChanged.RemoveAllListeners();
            collapse.isOn = beatmapObject.editorData.collapse;
            collapse.onValueChanged.AddListener(delegate (bool _value)
            {
                beatmapObject.editorData.collapse = _value;

                // Since autokill collapse has no affect on the physical object, we will only need to update the timeline object.
                if (RTEditor.inst.timelineBeatmapObjects.ContainsKey(beatmapObject.id))
                    RenderTimelineObject(RTEditor.inst.timelineBeatmapObjects[beatmapObject.id]);
            });
        }

        /// <summary>
        /// Renders all Parent UI.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        void RenderParent(BeatmapObject beatmapObject)
        {
            string parent = beatmapObject.parent;

            var parentTextText = (Text)ObjectUIElements["Parent Name"];
            var parentText = (Button)ObjectUIElements["Parent Select"];
            var parentMore = (Button)ObjectUIElements["Parent More B"];
            var parent_more = (Transform)ObjectUIElements["Parent More"];

            var parentParent = (Button)ObjectUIElements["Parent Search Open"];
            parentParent.onClick.RemoveAllListeners();
            parentParent.onClick.AddListener(delegate ()
            {
                EditorManager.inst.OpenParentPopup();
            });

            var parentClear = (Button)ObjectUIElements["Parent Clear"];
            parentClear.onClick.RemoveAllListeners();
            parentClear.onClick.AddListener(delegate ()
            {
                beatmapObject.parent = "";

                // Since parent has no affect on the timeline object, we will only need to update the physical object.
                if (UpdateObjects)
                    Updater.UpdateProcessor(beatmapObject, "Parent");

                RenderParent(beatmapObject);
            });

            var parentPicker = (Button)ObjectUIElements["Parent Picker"];
            parentPicker.onClick.RemoveAllListeners();
            parentPicker.onClick.AddListener(delegate ()
            {
                RTEditor.inst.parentPickerEnabled = true;
                RTEditor.inst.objectToParent = beatmapObject;
            });

            if (!string.IsNullOrEmpty(parent))
            {
                BaseBeatmapObject beatmapObjectParent = null;
                if (DataManager.inst.gameData.beatmapObjects.Find(x => x.id == parent) != null)
                {
                    beatmapObjectParent = DataManager.inst.gameData.beatmapObjects.Find(x => x.id == parent);
                    parentTextText.text = beatmapObjectParent.name;
                    ((HoverTooltip)ObjectUIElements["Parent Info"]).tooltipLangauges[0].hint = string.Format("Parent chain count: [{0}]\n(Inclusive)", beatmapObject.GetParentChain().Count);
                }
                else if (parent == "CAMERA_PARENT")
                    parentTextText.text = "[CAMERA]";
                else if (parent == "PLAYER_PARENT")
                    parentTextText.text = "[NEAREST PLAYER]";

                parentText.interactable = true;
                parentText.onClick.RemoveAllListeners();
                parentText.onClick.AddListener(delegate ()
                {
                    if (DataManager.inst.gameData.beatmapObjects.Find(x => x.id == parent) != null &&
                    parent != "CAMERA_PARENT" &&
                    parent != "PLAYER_PARENT" &&
                    RTEditor.inst.timelineBeatmapObjects.ContainsKey(parent))
                        SetCurrentObject(RTEditor.inst.timelineBeatmapObjects[parent]);
                    else
                    {
                        EditorManager.inst.SetLayer(5);
                        RTEditor.inst.layerType = RTEditor.LayerType.Events;
                        EventEditor.inst.SetCurrentEvent(0, RTExtensions.ClosestEventKeyframe(0));
                    }
                });
                parentMore.onClick.RemoveAllListeners();
                parentMore.interactable = true;
                parentMore.onClick.AddListener(delegate ()
                {
                    ObjEditor.inst.advancedParent = !ObjEditor.inst.advancedParent;
                    parent_more.gameObject.SetActive(ObjEditor.inst.advancedParent);
                });
                parent_more.gameObject.SetActive(ObjEditor.inst.advancedParent);

                for (int i = 0; i < 3; i++)
                {
                    var _p = (Transform)ObjectUIElements[$"Parent Offset{i + 1}"];

                    var parentOffset = beatmapObject.getParentOffset(i);

                    var iTmp = i;

                    var tog = _p.GetChild(2).GetComponent<Toggle>();
                    tog.onValueChanged.RemoveAllListeners();
                    tog.isOn = beatmapObject.GetParentType(i);
                    tog.onValueChanged.AddListener(delegate (bool _value)
                    {
                        beatmapObject.SetParentType(iTmp, _value);

                        // Since updating parent type has no affect on the timeline object, we will only need to update the physical object.
                        if (UpdateObjects)
                            Updater.UpdateProcessor(beatmapObject, "Parent Offset");
                    });

                    var pif = _p.GetChild(3).GetComponent<InputField>();
                    pif.onValueChanged.RemoveAllListeners();
                    pif.text = parentOffset.ToString();
                    pif.onValueChanged.AddListener(delegate (string _value)
                    {
                        if (float.TryParse(_value, out float num))
                        {
                            beatmapObject.SetParentOffset(iTmp, num);

                            // Since updating parent type has no affect on the timeline object, we will only need to update the physical object.
                            if (UpdateObjects)
                                Updater.UpdateProcessor(beatmapObject, "Parent Offset");
                        }
                    });

                    if (!_p.GetComponent<EventTrigger>())
                    {
                        _p.gameObject.AddComponent<EventTrigger>();
                    }

                    var pet = _p.GetComponent<EventTrigger>();
                    pet.triggers.Clear();
                    pet.triggers.Add(TriggerHelper.ScrollDelta(pif, 0.1f, 10f));

                    TriggerHelper.IncreaseDecreaseButtons(pif, 0.1f, 10f);
                }
            }
            else
            {
                parentTextText.text = "No Parent Object";
                parentText.interactable = false;
                parentText.onClick.RemoveAllListeners();
                parentMore.onClick.RemoveAllListeners();
                parentMore.interactable = false;
                parent_more.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Renders the Origin InputFields.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        void RenderOrigin(BeatmapObject beatmapObject)
        {
            var oxIF = (InputField)ObjectUIElements["Origin X IF"];

            if (!oxIF.gameObject.GetComponent<InputFieldSwapper>())
            {
                var ifh = oxIF.gameObject.AddComponent<InputFieldSwapper>();
                ifh.Init(oxIF, InputFieldSwapper.Type.Num);
            }

            oxIF.onValueChanged.RemoveAllListeners();
            oxIF.text = beatmapObject.origin.x.ToString();
            oxIF.onValueChanged.AddListener(delegate (string _value)
            {
                if (float.TryParse(_value, out float num))
                {
                    beatmapObject.origin.x = num;

                    // Since origin has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        Updater.UpdateProcessor(beatmapObject, "Origin");
                }
            });

            var oyIF = (InputField)ObjectUIElements["Origin Y IF"];

            if (!oyIF.gameObject.GetComponent<InputFieldSwapper>())
            {
                var ifh = oyIF.gameObject.AddComponent<InputFieldSwapper>();
                ifh.Init(oyIF, InputFieldSwapper.Type.Num);
            }

            oyIF.onValueChanged.RemoveAllListeners();
            oyIF.text = beatmapObject.origin.y.ToString();
            oyIF.onValueChanged.AddListener(delegate (string _value)
            {
                if (float.TryParse(_value, out float num))
                {
                    beatmapObject.origin.y = num;

                    // Since origin has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        Updater.UpdateProcessor(beatmapObject, "Origin");
                }
            });

            TriggerHelper.IncreaseDecreaseButtons(oxIF, 0.1f, 10f);
            TriggerHelper.IncreaseDecreaseButtons(oyIF, 0.1f, 10f);

            TriggerHelper.AddEventTrigger(oxIF.gameObject, new List<EventTrigger.Entry>
            { TriggerHelper.ScrollDelta(oxIF, multi: true), TriggerHelper.ScrollDeltaVector2(oxIF, oyIF, 0.1f, 10f) });
            TriggerHelper.AddEventTrigger(oyIF.gameObject, new List<EventTrigger.Entry>
            { TriggerHelper.ScrollDelta(oyIF, multi: true), TriggerHelper.ScrollDeltaVector2(oxIF, oyIF, 0.1f, 10f) });
        }

        /// <summary>
        /// Renders the Shape ToggleGroup.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        void RenderShape(BeatmapObject beatmapObject)
        {
            var shape = (Transform)ObjectUIElements["Shape"];
            var shapeSettings = (Transform)ObjectUIElements["Shape Settings"];
            foreach (object obj3 in shapeSettings)
            {
                var ch = (Transform)obj3;
                //if (ch.name != "5" && ch.name != "7")
                //{
                //    foreach (var c in ch)
                //    {
                //        var e = (Transform)c;
                //        if (!e.GetComponent<HoverUI>())
                //        {
                //            var he = e.gameObject.AddComponent<HoverUI>();
                //            he.animatePos = false;
                //            he.animateSca = true;
                //            he.size = 1.1f;
                //        }
                //    }
                //}
                ch.gameObject.SetActive(false);
            }
            if (beatmapObject.shape >= shapeSettings.childCount)
            {
                beatmapObject.shape = shapeSettings.childCount - 1;
                // Since shape has no affect on the timeline object, we will only need to update the physical object.
                if (UpdateObjects)
                    Updater.UpdateProcessor(beatmapObject, "Shape");

                RenderShape(beatmapObject);
            }
            if (beatmapObject.shape == 4)
            {
                shapeSettings.GetComponent<RectTransform>().sizeDelta = new Vector2(351f, 74f);
                var child = shapeSettings.GetChild(4);
                child.GetComponent<RectTransform>().sizeDelta = new Vector2(351f, 74f);
                child.Find("Text").GetComponent<Text>().alignment = TextAnchor.UpperLeft;
                child.Find("Placeholder").GetComponent<Text>().alignment = TextAnchor.UpperLeft;
                child.GetComponent<InputField>().lineType = InputField.LineType.MultiLineNewline;
            }
            else
            {
                shapeSettings.GetComponent<RectTransform>().sizeDelta = new Vector2(351f, 32f);
                shapeSettings.GetChild(4).GetComponent<RectTransform>().sizeDelta = new Vector2(351f, 32f);
            }

            shapeSettings.GetChild(beatmapObject.shape).gameObject.SetActive(true);
            for (int j = 1; j <= ObjectManager.inst.objectPrefabs.Count; j++)
            {
                int buttonTmp = j - 1;

                if (shape.Find(j.ToString()))
                {
                    var shoggle = shape.Find(j.ToString()).GetComponent<Toggle>();
                    shoggle.onValueChanged.RemoveAllListeners();

                    if (beatmapObject.shape == buttonTmp)
                        shoggle.isOn = true;
                    else
                        shoggle.isOn = false;

                    shoggle.onValueChanged.AddListener(delegate (bool _value)
                    {
                        if (_value)
                        {
                            beatmapObject.shape = buttonTmp;

                            // Since shape has no affect on the timeline object, we will only need to update the physical object.
                            if (UpdateObjects)
                                Updater.UpdateProcessor(beatmapObject, "Shape");

                            RenderShape(beatmapObject);
                        }
                    });
                }

                //if (!tfv.Find("shape/" + j).GetComponent<HoverUI>())
                //{
                //    var hoverUI = tfv.Find("shape/" + j).gameObject.AddComponent<HoverUI>();
                //    hoverUI.animatePos = false;
                //    hoverUI.animateSca = true;
                //    hoverUI.size = 1.1f;
                //}
            }
            if (beatmapObject.shape != 4 && beatmapObject.shape != 6)
            {
                for (int k = 0; k < shapeSettings.GetChild(beatmapObject.shape).childCount - 1; k++)
                {
                    int buttonTmp = k;
                    var shoggle = shapeSettings.GetChild(beatmapObject.shape).GetChild(k).GetComponent<Toggle>();

                    shoggle.onValueChanged.RemoveAllListeners();

                    if (beatmapObject.shapeOption == k)
                        shoggle.isOn = true;
                    else
                        shoggle.isOn = false;

                    shoggle.onValueChanged.AddListener(delegate (bool _value)
                    {
                        if (_value)
                        {
                            beatmapObject.shapeOption = buttonTmp;

                            // Since shape has no affect on the timeline object, we will only need to update the physical object.
                            if (UpdateObjects)
                                Updater.UpdateProcessor(beatmapObject, "Shape");

                            RenderShape(beatmapObject);
                        }
                    });
                }
            }
            else if (beatmapObject.shape == 4)
            {
                var textIF = shapeSettings.Find("5").GetComponent<InputField>();
                textIF.onValueChanged.ClearAll();
                textIF.text = beatmapObject.text;
                textIF.onValueChanged.AddListener(delegate (string _value)
                {
                    beatmapObject.text = _value;

                    // Since text has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        Updater.UpdateProcessor(beatmapObject, "Shape");
                });
            }
            else if (beatmapObject.shape == 6)
            {
                var select = shapeSettings.Find("7/select").GetComponent<Button>();
                select.onClick.RemoveAllListeners();
                select.onClick.AddListener(delegate ()
                {
                    OpenImageSelector(beatmapObject);
                });
                shapeSettings.Find("7/text").GetComponent<Text>().text = string.IsNullOrEmpty(beatmapObject.text) ? "No image selected" : beatmapObject.text;
            }
        }

        void SetDepthSlider(BeatmapObject beatmapObject, float _value, InputField inputField, Slider slider)
        {
            var num = (int)_value;

            beatmapObject.Depth = num;

            slider.onValueChanged.RemoveAllListeners();
            slider.value = num;
            slider.onValueChanged.AddListener(delegate (float _val)
            {
                SetDepthInputField(beatmapObject, ((int)_val).ToString(), inputField, slider);
            });

            // Since depth has no affect on the timeline object, we will only need to update the physical object.
            if (UpdateObjects)
                Updater.UpdateProcessor(beatmapObject, "Depth");
        }

        void SetDepthInputField(BeatmapObject beatmapObject, string _value, InputField inputField, Slider slider)
        {
            var num = int.Parse(_value);

            beatmapObject.Depth = num;

            inputField.onValueChanged.RemoveAllListeners();
            inputField.text = num.ToString();
            inputField.onValueChanged.AddListener(delegate (string _val)
            {
                if (int.TryParse(_val, out int numb))
                    SetDepthSlider(beatmapObject, numb, inputField, slider);
            });

            // Since depth has no affect on the timeline object, we will only need to update the physical object.
            if (UpdateObjects)
                Updater.UpdateProcessor(beatmapObject, "Depth");
        }

        /// <summary>
        /// Renders the Depth InputField and Slider.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        void RenderDepth(BeatmapObject beatmapObject)
        {
            var depthSlider = (Slider)ObjectUIElements["Depth Slider"];
            var depthText = (InputField)ObjectUIElements["Depth IF"];

            if (!depthText.GetComponent<InputFieldSwapper>())
            {
                var ifh = depthText.gameObject.AddComponent<InputFieldSwapper>();
                ifh.Init(depthText, InputFieldSwapper.Type.Num);
            }

            depthText.onValueChanged.RemoveAllListeners();
            depthText.text = beatmapObject.Depth.ToString();

            depthText.onValueChanged.AddListener(delegate (string _val)
            {
                if (int.TryParse(_val, out int num))
                    SetDepthSlider(beatmapObject, num, depthText, depthSlider);
            });

            bool showAcceptableRange = true;
            if (showAcceptableRange)
            {
                depthSlider.maxValue = 219;
                depthSlider.minValue = -98;
            }
            else
            {
                depthSlider.maxValue = 30;
                depthSlider.minValue = 0;
            }

            depthSlider.onValueChanged.RemoveAllListeners();
            depthSlider.value = beatmapObject.Depth;
            depthSlider.onValueChanged.AddListener(delegate (float _val)
            {
                SetDepthInputField(beatmapObject, _val.ToString(), depthText, depthSlider);
            });

            TriggerHelper.IncreaseDecreaseButtonsInt(depthText, -1);
            TriggerHelper.AddEventTrigger(depthText.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDeltaInt(depthText, 1) });
        }

        /// <summary>
        /// Creates and Renders the UnityExplorer GameObject Inspector.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to get.</param>
        void RenderGameObjectInspector(BeatmapObject beatmapObject)
        {
            if (ModCompatibility.mods.ContainsKey("UnityExplorer"))
            {
                var tfv = ObjEditor.inst.ObjectView.transform;

                var inspector = AccessTools.TypeByName("UnityExplorer.InspectorManager");
                if (inspector != null && !tfv.Find("inspect"))
                {
                    var label = tfv.ChildList().First(x => x.name == "label").gameObject.Duplicate(tfv);
                    var index = tfv.Find("editor").GetSiblingIndex() + 1;
                    label.transform.SetSiblingIndex(index);

                    Destroy(label.transform.GetChild(1).gameObject);
                    label.transform.GetChild(0).GetComponent<Text>().text = "UnityExplorer Inspect GameObject";

                    var inspect = tfv.Find("applyprefab").gameObject.Duplicate(tfv);
                    inspect.SetActive(true);
                    inspect.transform.SetSiblingIndex(index);
                    inspect.name = "inspect";

                    inspect.transform.GetChild(0).GetComponent<Text>().text = "Inspect";
                }

                if (tfv.Find("inspect"))
                {
                    var deleteButton = tfv.Find("inspect").GetComponent<Button>();
                    deleteButton.onClick.ClearAll();
                    deleteButton.onClick.AddListener(delegate ()
                    {
                        if (beatmapObject.TryGetGameObject(out GameObject gameObject))
                            inspector.GetMethod("Inspect", new[] { typeof(object), AccessTools.TypeByName("UnityExplorer.CacheObject.CacheObjectBase") }).Invoke(inspector, new object[] { gameObject, null });
                    });
                }
            }
        }

        /// <summary>
        /// Renders the Layers InputField.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        void RenderLayers(BeatmapObject beatmapObject)
        {
            var editorLayersIF = (InputField)ObjectUIElements["Layers IF"];
            var editorLayersImage = (Image)ObjectUIElements["Layers Image"];

            editorLayersIF.onValueChanged.RemoveAllListeners();
            editorLayersIF.text = beatmapObject.editorData.Layer.ToString();
            editorLayersImage.color = RTEditor.GetLayerColor(beatmapObject.editorData.Layer);
            editorLayersIF.onValueChanged.AddListener(delegate (string _value)
            {
                if (int.TryParse(_value, out int num))
                {
                    num = Mathf.Clamp(num, 0, int.MaxValue);
                    beatmapObject.editorData.Layer = num;
                    editorLayersIF.text = num.ToString();
                }

                // Since layers have no effect on the physical object, we will only need to update the timeline object.
                if (RTEditor.inst.timelineBeatmapObjects.ContainsKey(beatmapObject.id))
                    RenderTimelineObject(RTEditor.inst.timelineBeatmapObjects[beatmapObject.id]);

                editorLayersImage.color = RTEditor.GetLayerColor(beatmapObject.editorData.Layer);
            });

            if (editorLayersIF.gameObject)
                TriggerHelper.AddEventTrigger(editorLayersIF.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDeltaInt(editorLayersIF, 1, 1, int.MaxValue) });
        }

        /// <summary>
        /// Renders the Bin Slider.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        void RenderBin(BeatmapObject beatmapObject)
        {
            var editorBin = (Slider)ObjectUIElements["Bin Slider"];
            editorBin.onValueChanged.RemoveAllListeners();
            editorBin.value = beatmapObject.editorData.Bin;
            editorBin.onValueChanged.AddListener(delegate (float _value)
            {
                beatmapObject.editorData.Bin = (int)Mathf.Clamp(_value, 0f, 14f);

                // Since bin has no effect on the physical object, we will only need to update the timeline object.
                if (RTEditor.inst.timelineBeatmapObjects.ContainsKey(beatmapObject.id))
                    RenderTimelineObject(RTEditor.inst.timelineBeatmapObjects[beatmapObject.id]);
            });
        }

        void RenderKeyframeDialog(BeatmapObject beatmapObject)
        {
            if (SelectedBeatmapObjectKeyframes.Count <= 1)
            {
                for (int i = 0; i < ObjEditor.inst.KeyframeDialogs.Count; i++)
                {
                    ObjEditor.inst.KeyframeDialogs[i].SetActive(i == ObjEditor.inst.currentKeyframeKind);
                }

                //Keyframes
                {
                    var kfdialog = ObjEditor.inst.KeyframeDialogs[ObjEditor.inst.currentKeyframeKind].transform;
                    if (beatmapObject.events[ObjEditor.inst.currentKeyframeKind].Count > 0)
                        StartCoroutine(IRenderKeyframeDialog(kfdialog, ObjEditor.inst.currentKeyframeKind, beatmapObject));

                    var timeDecreaseGreat = kfdialog.Find("time/<<").GetComponent<Button>();
                    var timeDecrease = kfdialog.Find("time/<").GetComponent<Button>();
                    var timeIncrease = kfdialog.Find("time/>").GetComponent<Button>();
                    var timeIncreaseGreat = kfdialog.Find("time/>>").GetComponent<Button>();
                    var timeSet = kfdialog.Find("time/time").GetComponent<InputField>();

                    if (ObjEditor.inst.currentKeyframe == 0)
                    {
                        timeDecreaseGreat.interactable = false;
                        timeDecrease.interactable = false;
                        timeIncrease.interactable = false;
                        timeIncreaseGreat.interactable = false;
                        timeSet.interactable = false;
                    }
                    else
                    {
                        timeDecreaseGreat.interactable = true;
                        timeDecrease.interactable = true;
                        timeIncrease.interactable = true;
                        timeIncreaseGreat.interactable = true;
                        timeSet.interactable = true;
                    }

                    var superLeft = kfdialog.Find("edit/<<").GetComponent<Button>();

                    superLeft.onClick.RemoveAllListeners();
                    superLeft.interactable = (ObjEditor.inst.currentKeyframe != 0);
                    superLeft.onClick.AddListener(delegate ()
                    {
                        SetCurrentKeyframe(beatmapObject, 0, true);
                    });

                    var left = kfdialog.Find("edit/<").GetComponent<Button>();

                    left.onClick.RemoveAllListeners();
                    left.interactable = (ObjEditor.inst.currentKeyframe != 0);
                    left.onClick.AddListener(delegate ()
                    {
                        AddCurrentKeyframe(beatmapObject, -1, true);
                    });

                    string text = ObjEditor.inst.currentKeyframe.ToString();
                    if (ObjEditor.inst.currentKeyframe == 0)
                    {
                        text = "S";
                    }
                    else if (ObjEditor.inst.currentKeyframe == beatmapObject.events[ObjEditor.inst.currentKeyframeKind].Count - 1)
                    {
                        text = "E";
                    }

                    kfdialog.Find("edit/|").GetComponentInChildren<Text>().text = text;

                    var right = kfdialog.Find("edit/>").GetComponent<Button>();

                    right.onClick.RemoveAllListeners();
                    right.interactable = (ObjEditor.inst.currentKeyframe < beatmapObject.events[ObjEditor.inst.currentKeyframeKind].Count - 1);
                    right.onClick.AddListener(delegate ()
                    {
                        AddCurrentKeyframe(beatmapObject, 1, true);
                    });

                    var superRight = kfdialog.Find("edit/>>").GetComponent<Button>();

                    superRight.onClick.RemoveAllListeners();
                    superRight.interactable = (ObjEditor.inst.currentKeyframe < beatmapObject.events[ObjEditor.inst.currentKeyframeKind].Count - 1);
                    superRight.onClick.AddListener(delegate ()
                    {
                        AddCurrentKeyframe(beatmapObject, int.MaxValue, true);
                    });

                    var deleteKey = kfdialog.Find("edit/del").GetComponent<Button>();

                    deleteKey.onClick.RemoveAllListeners();
                    deleteKey.interactable = ObjEditor.inst.currentKeyframe != 0;
                    deleteKey.onClick.AddListener(delegate ()
                    {
                        ObjEditor.inst.DeleteKeyframe();
                    });
                }
            }
            else
            {
                for (int num7 = 0; num7 < ObjEditor.inst.KeyframeDialogs.Count; num7++)
                {
                    ObjEditor.inst.KeyframeDialogs[num7].SetActive(false);
                }
                ObjEditor.inst.KeyframeDialogs[4].SetActive(true);
            }
        }

        public void OpenImageSelector(BeatmapObject beatmapObject)
        {
            var editorPath = RTFile.ApplicationDirectory + RTEditor.editorListSlash + EditorManager.inst.currentLoadedLevel;
            string jpgFile = FileBrowser.OpenSingleFile("Select an image!", editorPath, new string[] { "png", "jpg" });
            Debug.Log($"{EditorPlugin.className}Selected file: {jpgFile}");
            if (!string.IsNullOrEmpty(jpgFile))
            {
                string jpgFileLocation = editorPath + "/" + Path.GetFileName(jpgFile);
                Debug.Log($"{EditorPlugin.className}jpgFileLocation: {jpgFileLocation}");

                var levelPath = jpgFile.Replace("\\", "/").Replace(editorPath + "/", "");
                Debug.Log("{EditorPlugin.className}levelPath: {levelPath}");

                if (!RTFile.FileExists(jpgFileLocation) && !jpgFile.Replace("\\", "/").Contains(editorPath))
                {
                    File.Copy(jpgFile, jpgFileLocation);
                    Debug.Log("{EditorPlugin.className}Copied file to : {jpgFileLocation}");
                }
                else
                    jpgFileLocation = editorPath + "/" + levelPath;

                Debug.Log($"{EditorPlugin.className}jpgFileLocation: {jpgFileLocation}");
                beatmapObject.text = jpgFileLocation.Replace(jpgFileLocation.Substring(0, jpgFileLocation.LastIndexOf('/') + 1), "");

                // Since setting image has no affect on the timeline object, we will only need to update the physical object.
                if (UpdateObjects)
                    Updater.UpdateProcessor(beatmapObject, "Shape");

                RenderShape(beatmapObject);
            }
        }

        #endregion

        #region Keyframe Handlers

        public static bool AllowTimeExactlyAtStart => true;
        public void ResizeKeyframeTimeline(BeatmapObject beatmapObject)
        {
            // ObjEditor.inst.ObjectLengthOffset is the offset from the last keyframe. Could allow for more timeline space.
            float objectLifeLength = beatmapObject.GetObjectLifeLength(ObjEditor.inst.ObjectLengthOffset);
            float x = ObjEditorPatch.posCalc(objectLifeLength);

            ObjEditor.inst.objTimelineContent.GetComponent<RectTransform>().sizeDelta = new Vector2(x, 0f);

            // Whether the value should clamp at 0.001 over StartTime or not.
            if (AllowTimeExactlyAtStart)
                ObjEditor.inst.objTimelineSlider.minValue = beatmapObject.StartTime;
            else
                ObjEditor.inst.objTimelineSlider.minValue = beatmapObject.StartTime + 0.001f;

            ObjEditor.inst.objTimelineSlider.maxValue = beatmapObject.StartTime + objectLifeLength;
            ObjEditor.inst.objTimelineGrid.GetComponent<RectTransform>().sizeDelta = new Vector2(x, 122f);

            ObjEditor.inst.objTimelineGrid.DeleteChildren();

            GameObject gameObject = Instantiate(ObjEditor.inst.KeyframeEndPrefab);
            gameObject.name = "end keyframe";
            gameObject.transform.SetParent(ObjEditor.inst.objTimelineGrid);
            gameObject.transform.localScale = Vector2.one;

            var rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(4f, 122f);
            rectTransform.anchoredPosition = new Vector2(beatmapObject.GetObjectLifeLength(0f, false, false) * ObjEditor.inst.Zoom * 14f, 0f);

            ObjEditor.inst.objTimelineSlider.onValueChanged.RemoveAllListeners();
            ObjEditor.inst.objTimelineSlider.onValueChanged.AddListener(delegate (float _val)
            {
                if (ObjEditor.inst.changingTime)
                {
                    ObjEditor.inst.newTime = _val;
                    AudioManager.inst.SetMusicTime(Mathf.Clamp(_val, 0f, AudioManager.inst.CurrentAudioSource.clip.length));
                }
            });

            for (int i = 0; i < ObjEditor.inst.TimelineParents.Count; i++)
            {
                int tmpIndex = i;
                EventTrigger.Entry entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.PointerUp;
                entry.callback.AddListener(delegate (BaseEventData eventData)
                {
                    if (((PointerEventData)eventData).button == PointerEventData.InputButton.Right)
                    {
                        float timeTmp = ObjEditorPatch.timeCalc();
                        int index = beatmapObject.events[tmpIndex].FindLastIndex(x => x.eventTime <= timeTmp);
                        ObjEditor.inst.AddEvent(timeTmp, tmpIndex, beatmapObject.events[tmpIndex][index]);
                        ObjEditor.inst.UpdateKeyframeOrder(true);
                        int keyframe = beatmapObject.events[tmpIndex].FindLastIndex(x => x.eventTime == timeTmp);
                        ObjEditor.inst.SetCurrentKeyframe(tmpIndex, keyframe, false, InputDataManager.inst.editorActions.MultiSelect.IsPressed);
                        ResizeKeyframeTimeline(beatmapObject);

                        CreateKeyframes(beatmapObject);

                        // Keyframes affect both physical object and timeline object.
                        if (RTEditor.inst.timelineBeatmapObjects.ContainsKey(beatmapObject.id))
                        {
                            RenderTimelineObject(RTEditor.inst.timelineBeatmapObjects[beatmapObject.id]);
                            if (UpdateObjects)
                                Updater.UpdateProcessor(beatmapObject, "Keyframes");
                        }
                    }
                });
                var comp = ObjEditor.inst.TimelineParents[tmpIndex].GetComponent<EventTrigger>();
                comp.triggers.RemoveAll(x => x.eventID == EventTriggerType.PointerUp);
                comp.triggers.Add(entry);
            }
        }

        public void CreateKeyframes(BeatmapObject beatmapObject)
        {
            foreach (var kf in RTEditor.inst.timelineBeatmapObjectKeyframes)
                Destroy(kf.GameObject);

            RTEditor.inst.timelineBeatmapObjectKeyframes.Clear();

            //for (int i = 0; i < ObjEditor.inst.timelineKeyframes.Count; i++)
            //{
            //    foreach (var obj in ObjEditor.inst.timelineKeyframes[i])
            //        Destroy(obj);

            //    ObjEditor.inst.timelineKeyframes[i].Clear();
            //}

            if (!string.IsNullOrEmpty(beatmapObject.id))
            {
                for (int j = 0; j < ObjEditor.inst.TimelineParents.Count; j++)
                {
                    if (beatmapObject.events[j].Count > 0)
                    {
                        int num = 0;
                        foreach (var eventKeyframe in beatmapObject.events[j])
                        {
                            GameObject gameObject = Instantiate(ObjEditor.inst.objTimelinePrefab);
                            gameObject.transform.SetParent(ObjEditor.inst.TimelineParents[j]);
                            gameObject.transform.localScale = Vector3.one;
                            gameObject.name = $"{IntToType(j)}_{num}";

                            //ObjEditor.inst.timelineKeyframes[j].Insert(num, gameObject);

                            var timelineObject = new TimelineObject<EventKeyframe>((EventKeyframe)eventKeyframe, gameObject, gameObject.transform.GetChild(0).GetComponent<Image>());

                            RTEditor.inst.timelineBeatmapObjectKeyframes.Add(timelineObject);

                            RenderKeyframe(beatmapObject, timelineObject);

                            float x = ObjEditorPatch.posCalc(eventKeyframe.eventTime);

                            var rectTransform = gameObject.GetComponent<RectTransform>();
                            rectTransform.sizeDelta = new Vector2(14f, 25f);
                            rectTransform.anchoredPosition = new Vector2(x, 0f);

                            int tmpType = j;
                            int tmpIndex = num;
                            var button = gameObject.GetComponent<Button>();
                            button.onClick.ClearAll();
                            button.onClick.AddListener(delegate ()
                            {
                                inst.SetCurrentKeyframe(beatmapObject, tmpType, tmpIndex, false, InputDataManager.inst.editorActions.MultiSelect.IsPressed);
                            });

                            var eventTrigger = gameObject.GetComponent<EventTrigger>();
                            eventTrigger.triggers.Add(TriggerHelper.CreateKeyframeStartDragTrigger(beatmapObject, timelineObject));
                            eventTrigger.triggers.Add(TriggerHelper.CreateKeyframeEndDragTrigger(beatmapObject, timelineObject));
                            num++;
                        }
                    }
                }
                //UpdateHighlightedKeyframe();
            }
        }

        public void CreateKeyframe(BeatmapObject beatmapObject, int type, int index)
        {
            GameObject gameObject = Instantiate(ObjEditor.inst.objTimelinePrefab);
            gameObject.transform.SetParent(ObjEditor.inst.TimelineParents[type]);
            gameObject.transform.localScale = Vector3.one;
            gameObject.name = $"{IntToType(type)}_{index}";

            var eventKeyframe = beatmapObject.events[type][index];

            var timelineObject = new TimelineObject<EventKeyframe>((EventKeyframe)eventKeyframe, gameObject, gameObject.transform.GetChild(0).GetComponent<Image>());

            var kf = RTEditor.inst.timelineBeatmapObjectKeyframes.Find(x => x.Type == type && x.Index == index);
            var kfIndex = RTEditor.inst.timelineBeatmapObjectKeyframes.FindIndex(x => x.Type == type && x.Index == index);

            if (!kf)
            {
                RTEditor.inst.timelineBeatmapObjectKeyframes.Add(timelineObject);
            }
            else if (kfIndex > -1)
            {
                RTEditor.inst.timelineBeatmapObjectKeyframes[kfIndex] = timelineObject;
            }

            RenderKeyframe(beatmapObject, timelineObject);

            int tmpType = type;
            int tmpIndex = index;
            var button = gameObject.GetComponent<Button>();
            button.onClick.ClearAll();
            button.onClick.AddListener(delegate ()
            {
                inst.SetCurrentKeyframe(beatmapObject, tmpType, tmpIndex, false, InputDataManager.inst.editorActions.MultiSelect.IsPressed);
            });

            var eventTrigger = gameObject.GetComponent<EventTrigger>();
            eventTrigger.triggers.Add(TriggerHelper.CreateKeyframeStartDragTrigger(beatmapObject, timelineObject));
            eventTrigger.triggers.Add(TriggerHelper.CreateKeyframeEndDragTrigger(beatmapObject, timelineObject));
        }

        public void RenderKeyframes(BeatmapObject beatmapObject)
        {
            for (int i = 0; i < beatmapObject.events.Count; i++)
            {
                for (int j = 0; j < beatmapObject.events[i].Count; j++)
                {
                    var kf = RTEditor.inst.timelineBeatmapObjectKeyframes.Find(x => x.Type == i && x.Index == j);
                    if (!kf || !kf.GameObject)
                    {
                        CreateKeyframe(beatmapObject, i, j);
                    }
                }
            }

            foreach (var timelineObject in RTEditor.inst.timelineBeatmapObjectKeyframes)
            {
                if (!ObjEditor.inst.keyframeTimelinePositions.ContainsKey(beatmapObject.id))
                {
                    ObjEditor.inst.keyframeTimelinePositions.Add(beatmapObject.id, 0f);
                }
                else
                {
                    // Need to store ScrollRect as a variable so it doesn't lag the game with GetComponent()
                    ObjEditor.inst.keyframeTimelinePositions[beatmapObject.id] = ObjEditor.inst.objTimelineContent.parent.parent.GetComponent<ScrollRect>().horizontalScrollbar.value;
                }

                // Here we remember an object's zoom.
                if (!ObjEditor.inst.keyframeTimelineZooms.ContainsKey(beatmapObject.id))
                {
                    ObjEditor.inst.keyframeTimelineZooms.Add(beatmapObject.id, 0.05f);
                }
                else
                {
                    ObjEditor.inst.keyframeTimelineZooms[beatmapObject.id] = ObjEditor.inst.zoomFloat;
                }

                if (!string.IsNullOrEmpty(beatmapObject.id))
                {
                    if (!ObjEditor.inst.keyframeTimelineZooms.ContainsKey(beatmapObject.id))
                    {
                        ObjEditor.inst.keyframeTimelineZooms.Add(beatmapObject.id, 0f);
                        ObjEditor.inst.Zoom = 0.05f;
                    }
                    else
                    {
                        ObjEditor.inst.Zoom = ObjEditor.inst.keyframeTimelineZooms[beatmapObject.id];
                    }

                    if (!ObjEditor.inst.keyframeTimelinePositions.ContainsKey(beatmapObject.id))
                    {
                        ObjEditor.inst.keyframeTimelinePositions.Add(beatmapObject.id, 0f);
                        ObjEditor.inst.objTimelineContent.parent.parent.GetComponent<ScrollRect>().horizontalScrollbar.value = 0f;
                    }
                    else if (ObjEditor.inst.keyframeTimelinePositions.ContainsKey(CurrentBeatmapObjectSelection.ID))
                    {
                        ObjEditor.inst.objTimelineContent.parent.parent.GetComponent<ScrollRect>().horizontalScrollbar.value = ObjEditor.inst.keyframeTimelinePositions[CurrentBeatmapObjectSelection.ID];
                    }

                    if (!ObjEditor.inst.keyframeTimelineSelections.ContainsKey(beatmapObject.id))
                    {
                        ObjEditor.inst.keyframeTimelineSelections.Add(beatmapObject.id, new List<BaseObjectKeyframeSelection>
                        {
                            new BaseObjectKeyframeSelection()
                        });
                        ObjEditor.inst.keyframeSelections = ObjEditor.inst.keyframeTimelineSelections[beatmapObject.id];
                    }
                    else
                    {
                        ObjEditor.inst.keyframeSelections = ObjEditor.inst.keyframeTimelineSelections[beatmapObject.id];
                    }

                    //if (!ObjEditor.inst.keyframeTimelineCurrentSelections.ContainsKey(beatmapObject.id))
                    //{
                    //    ObjEditor.inst.keyframeTimelineCurrentSelections.Add(beatmapObject.id, new ObjectKeyframeSelection());
                    //    ObjEditor.inst.currentKeyframeKind = ObjEditor.inst.keyframeTimelineCurrentSelections[beatmapObject.id].Type;
                    //    ObjEditor.inst.currentKeyframe = ObjEditor.inst.keyframeTimelineCurrentSelections[beatmapObject.id].Index;
                    //}
                    //else
                    //{
                    //    ObjEditor.inst.currentKeyframeKind = ObjEditor.inst.keyframeTimelineCurrentSelections[beatmapObject.id].Type;
                    //    ObjEditor.inst.currentKeyframe = ObjEditor.inst.keyframeTimelineCurrentSelections[beatmapObject.id].Index;
                    //}
                }

                //if (!ObjEditor.inst.keyframeTimelineSelections.ContainsKey(beatmapObject.id))
                //{
                //    ObjEditor.inst.keyframeTimelineSelections.Add(beatmapObject.id, new List<ObjectKeyframeSelection>
                //    {
                //        new ObjectKeyframeSelection()
                //    });
                //}
                //else
                //{
                //    List<ObjectKeyframeSelection> list = new List<ObjectKeyframeSelection>();
                //    foreach (var orig in ObjEditor.inst.keyframeSelections)
                //    {
                //        list.Add(ObjectKeyframeSelection.DeepCopy(orig));
                //    }
                //    ObjEditor.inst.keyframeTimelineSelections[beatmapObject.id] = list;
                //}
                //if (!ObjEditor.inst.keyframeTimelineCurrentSelections.ContainsKey(beatmapObject.id))
                //{
                //    ObjEditor.inst.keyframeTimelineCurrentSelections.Add(beatmapObject.id, new ObjectKeyframeSelection());
                //}
                //else
                //{
                //    ObjEditor.inst.keyframeTimelineCurrentSelections[beatmapObject.id] = new ObjectKeyframeSelection(ObjEditor.inst.currentKeyframeKind, ObjEditor.inst.currentKeyframe);
                //}
            }
        }

        public static void RenderKeyframe(BeatmapObject beatmapObject, TimelineObject<EventKeyframe> timelineObject)
        {
            var eventKeyframe = timelineObject.Data;
            timelineObject.Image.sprite =
                                GetKeyframeIcon(eventKeyframe.curveType,
                                beatmapObject.events[timelineObject.Type].Count > timelineObject.Index + 1 ?
                                beatmapObject.events[timelineObject.Type][timelineObject.Index + 1].curveType : DataManager.inst.AnimationList[0]);

            float x = ObjEditorPatch.posCalc(eventKeyframe.eventTime);

            var rectTransform = (RectTransform)timelineObject.GameObject.transform;
            rectTransform.sizeDelta = new Vector2(14f, 25f);
            rectTransform.anchoredPosition = new Vector2(x, 0f);
        }

        //public static void UpdateHighlightedKeyframe()
        //{
        //    int typeIndex = 0;
        //    foreach (var list in ObjEditor.inst.timelineKeyframes)
        //    {
        //        int keyframeIndex = 0;
        //        int num;
        //        foreach (var gameObject in list)
        //        {
        //            var componentInChildren = gameObject.GetComponentInChildren<Image>();
        //            var list2 = ObjEditor.inst.keyframeSelections;

        //            if (list2.FindIndex(x => x.Index == keyframeIndex && x.Type == typeIndex) != -1)
        //                componentInChildren.color = ObjEditor.inst.SelectedColor;
        //            else
        //                componentInChildren.color = ObjEditor.inst.NormalColor;

        //            num = keyframeIndex;
        //            keyframeIndex = num + 1;
        //        }
        //        num = typeIndex;
        //        typeIndex = num + 1;
        //    }
        //}

        IEnumerator IRenderKeyframeDialog(Transform p, int type, BeatmapObject beatmapObject)
        {
            if (beatmapObject.events[type].Count > 0)
            {
                var keyframe = beatmapObject.events[type][ObjEditor.inst.currentKeyframe];
                float eventTime = keyframe.eventTime;

                var tet = p.Find("time").GetComponent<EventTrigger>();
                var tif = p.Find("time/time").GetComponent<InputField>();

                tet.triggers.Clear();
                if (ObjEditor.inst.currentKeyframe != 0)
                    tet.triggers.Add(TriggerHelper.ScrollDelta(tif, 0.1f, 10f));

                tif.onValueChanged.RemoveAllListeners();
                tif.text = keyframe.eventTime.ToString();
                tif.onValueChanged.AddListener(delegate (string _value)
                {
                    SetKeyframeTime(beatmapObject, float.Parse(_value), false);
                });

                TriggerHelper.IncreaseDecreaseButtons(tif, 0.1f, 10f, t: p);

                p.Find("curves_label").gameObject.SetActive(ObjEditor.inst.currentKeyframe != 0);
                p.Find("curves").gameObject.SetActive(ObjEditor.inst.currentKeyframe != 0);
                p.Find("curves").GetComponent<Dropdown>().onValueChanged.RemoveAllListeners();
                if (DataManager.inst.AnimationListDictionaryBack.ContainsKey(keyframe.curveType))
                {
                    p.Find("curves").GetComponent<Dropdown>().value = DataManager.inst.AnimationListDictionaryBack[keyframe.curveType];
                }
                p.Find("curves").GetComponent<Dropdown>().onValueChanged.AddListener(delegate (int _value)
                {
                    keyframe.curveType = DataManager.inst.AnimationListDictionary[_value];

                    // Since keyframe curve has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        Updater.UpdateProcessor(beatmapObject, "Keyframes");
                    CreateKeyframes(beatmapObject);
                });

                if (type != 3)
                {
                    int limt = 1;
                    if (type != 2)
                        limt = keyframe.eventValues.Count();
                    else
                        limt = 1;

                    for (int i = 0; i < limt; i++)
                    {
                        if (p.GetChild(9).childCount > i && p.GetChild(9).GetChild(i) != null)
                        {
                            var pos = p.GetChild(9).GetChild(i);
                            EventTrigger posET;

                            // Checks if type is rotation.
                            if (type != 2)
                                posET = pos.GetComponent<EventTrigger>();
                            else
                                posET = p.GetChild(9).GetComponent<EventTrigger>();

                            var posIF = pos.GetComponent<InputField>();
                            var posLeft = pos.Find("<").GetComponent<Button>();
                            var posRight = pos.Find(">").GetComponent<Button>();

                            if (!pos.GetComponent<InputFieldSwapper>())
                            {
                                var ifh = pos.gameObject.AddComponent<InputFieldSwapper>();
                                ifh.Init(posIF, InputFieldSwapper.Type.Num);
                            }

                            posET.triggers.Clear();
                            if (type != 2)
                            {
                                //Debug.LogFormat("{0}Refresh Object GUI: Keyframe " + type + " [" + (i + 1) + "/" + limt + "]", EditorPlugin.className);
                                posET.triggers.Add(TriggerHelper.ScrollDelta(posIF, multi: true));
                                posET.triggers.Add(TriggerHelper.ScrollDeltaVector2(p.GetChild(9).GetChild(0).GetComponent<InputField>(), p.GetChild(9).GetChild(1).GetComponent<InputField>(), 0.1f, 10f));
                            }
                            else
                            {
                                //Debug.LogFormat("{0}Refresh Object GUI: Keyframe " + type + " [" + (i + 1) + "/" + limt + "]", EditorPlugin.className);
                                posET.triggers.Add(TriggerHelper.ScrollDelta(posIF, 15f, 3f));
                            }

                            int current = i;

                            posIF.onValueChanged.RemoveAllListeners();
                            posIF.text = keyframe.eventValues[i].ToString();
                            posIF.onValueChanged.AddListener(delegate (string _value)
                            {
                                keyframe.eventValues[current] = float.Parse(_value);

                                // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                if (UpdateObjects)
                                    Updater.UpdateProcessor(beatmapObject, "Keyframes");
                            });

                            posLeft.onClick.RemoveAllListeners();
                            posLeft.onClick.AddListener(delegate ()
                            {
                                float x = keyframe.eventValues[current];

                                if (type != 2)
                                    x -= 1f;
                                else
                                    x -= 15f;

                                posIF.text = x.ToString();
                            });

                            posRight.onClick.RemoveAllListeners();
                            posRight.onClick.AddListener(delegate ()
                            {
                                float x = keyframe.eventValues[current];

                                if (type != 2)
                                    x += 1f;
                                else
                                    x += 15f;

                                posIF.text = x.ToString();
                            });
                        }
                    }

                    //Debug.Log($"{EditorPlugin.className}Refresh Object GUI: Keyframe Random Base");
                    var randomValue = p.GetChild(11);

                    int random = keyframe.random;

                    if (type != 2)
                    {
                        for (int n = 0; n <= 3; n++)
                        {
                            //Debug.Log($"{EditorPlugin.className}Refresh Object GUI: Keyframe Random Toggle [{n}]");
                            int buttonTmp = (n >= 2) ? (n + 1) : n;
                            var child = p.GetChild(13).GetChild(n).GetComponent<Toggle>();
                            child.onValueChanged.RemoveAllListeners();
                            child.isOn = random == buttonTmp;
                            child.onValueChanged.AddListener(delegate (bool _value)
                            {
                                if (_value)
                                {
                                    keyframe.random = buttonTmp;
                                    p.GetChild(10).gameObject.SetActive(buttonTmp != 0);
                                    p.GetChild(11).gameObject.SetActive(buttonTmp != 0);
                                    p.GetChild(10).GetChild(0).GetComponent<Text>().text = (buttonTmp == 4) ? "Random Scale Min" : "Random X";
                                    if (p.GetChild(10).childCount > 1)
                                        p.GetChild(10).GetChild(1).GetComponent<Text>().text = (buttonTmp == 4) ? "Random Scale Max" : "Random Y";
                                    p.Find("random/interval-input").gameObject.SetActive(buttonTmp != 0 && buttonTmp != 3);
                                    p.Find("r_label/interval").gameObject.SetActive(buttonTmp != 0 && buttonTmp != 3);

                                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                    if (UpdateObjects)
                                        Updater.UpdateProcessor(beatmapObject, "Keyframes");
                                }
                            });
                            //if (!child.GetComponent<HoverUI>())
                            //{
                            //    var hoverUI = child.gameObject.AddComponent<HoverUI>();
                            //    hoverUI.animatePos = false;
                            //    hoverUI.animateSca = true;
                            //    hoverUI.size = 1.1f;
                            //}
                        }
                    }
                    else
                    {
                        for (int n = 0; n <= 2; n++)
                        {
                            //Debug.Log($"{EditorPlugin.className}Refresh Object GUI: Keyframe Random Toggle [{n}]");
                            int buttonTmp = (n >= 2) ? (n + 1) : n;
                            var child = p.GetChild(13).GetChild(n).GetComponent<Toggle>();
                            child.NewValueChangedListener(random == buttonTmp, delegate (bool _val)
                            {
                                if (_val)
                                {
                                    keyframe.random = buttonTmp;
                                    p.GetChild(10).gameObject.SetActive(buttonTmp != 0);
                                    p.GetChild(11).gameObject.SetActive(buttonTmp != 0);
                                    p.GetChild(10).GetChild(0).GetComponent<Text>().text = (buttonTmp == 4) ? "Random Scale Min" : "Random X";
                                    if (p.GetChild(10).childCount > 1)
                                        p.GetChild(10).GetChild(1).GetComponent<Text>().text = (buttonTmp == 4) ? "Random Scale Max" : "Random Y";
                                    p.Find("random/interval-input").gameObject.SetActive(buttonTmp != 0 && buttonTmp != 3);
                                    p.Find("r_label/interval").gameObject.SetActive(buttonTmp != 0 && buttonTmp != 3);

                                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                    if (UpdateObjects)
                                        Updater.UpdateProcessor(beatmapObject, "Keyframes");
                                }
                            });
                            //if (!child.GetComponent<HoverUI>())
                            //{
                            //    var hoverUI = child.gameObject.AddComponent<HoverUI>();
                            //    hoverUI.animatePos = false;
                            //    hoverUI.animateSca = true;
                            //    hoverUI.size = 1.1f;
                            //}
                        }
                    }

                    Debug.Log($"{EditorPlugin.className}Refresh Object GUI: Keyframe Random Value [{random}]");
                    float num = 0f;
                    if (keyframe.eventRandomValues.Length > 2)
                        num = keyframe.eventRandomValues[2];

                    p.GetChild(10).gameObject.SetActive(random != 0);
                    p.GetChild(10).GetChild(0).GetComponent<Text>().text = (random == 4) ? "Random Scale Min" : "Random X";
                    if (p.GetChild(10).childCount > 1 && p.GetChild(10).GetChild(1) != null && p.GetChild(10).GetChild(1).GetComponent<Text>())
                        p.GetChild(10).GetChild(1).GetComponent<Text>().text = (random == 4) ? "Random Scale Max" : "Random Y";

                    randomValue.gameObject.SetActive(random != 0);
                    bool active = random != 0 && random != 3;
                    p.Find("r_label/interval").gameObject.SetActive(active);

                    var randomInterval = p.Find("random/interval-input");
                    var randomIntervalButton = randomInterval.Find("x").GetComponent<Button>();
                    var randomIntervalIF = randomInterval.GetComponent<InputField>();

                    p.Find("random/interval-input").gameObject.SetActive(active);
                    randomIntervalButton.onClick.RemoveAllListeners();
                    randomIntervalButton.onClick.AddListener(delegate ()
                    {
                        randomIntervalIF.text = "0";
                    });

                    randomIntervalIF.NewValueChangedListener(num.ToString(), delegate (string _val)
                    {
                        if (float.TryParse(_val, out float num))
                        {
                            keyframe.eventRandomValues[2] = num;

                            // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                            if (UpdateObjects)
                                Updater.UpdateProcessor(beatmapObject, "Keyframes");
                        }
                    });

                    if (!randomInterval.GetComponent<InputFieldSwapper>())
                    {
                        var ifh = randomInterval.gameObject.AddComponent<InputFieldSwapper>();
                        ifh.Init(randomIntervalIF, InputFieldSwapper.Type.Num);
                    }

                    TriggerHelper.AddEventTrigger(randomInterval.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(randomIntervalIF, 0.1f, 10f) });

                    for (int kf = 0; kf < keyframe.eventRandomValues.Count() - 1; kf++)
                    {
                        if (kf < randomValue.childCount && randomValue.GetChild(kf))
                        {
                            int index = kf;
                            //Debug.Log($"{EditorPlugin.className}Refresh Object GUI: Keyframe Random KF [{(kf + 1)}/{keyframe.eventRandomValues.Count()}]");
                            var randomValueX = randomValue.GetChild(index).GetComponent<InputField>();

                            randomValueX.NewValueChangedListener(keyframe.eventRandomValues[index].ToString(), delegate (string _val)
                            {
                                if (float.TryParse(_val, out float num))
                                {
                                    keyframe.eventRandomValues[index] = num;

                                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                    if (UpdateObjects)
                                        Updater.UpdateProcessor(beatmapObject, "Keyframes");
                                }
                            });

                            TriggerHelper.IncreaseDecreaseButtons(randomValueX, 0.1f, 10f);

                            if (type != 2)
                            {
                                if (!randomValue.GetChild(index).GetComponent<InputFieldSwapper>())
                                {
                                    var ifh = randomValue.GetChild(index).gameObject.AddComponent<InputFieldSwapper>();
                                    ifh.Init(randomValueX, InputFieldSwapper.Type.Num);
                                }

                                var randET = randomValue.GetChild(index).GetComponent<EventTrigger>();
                                randET.triggers.Clear();
                                randET.triggers.Add(TriggerHelper.ScrollDelta(randomValueX, multi: true));
                                randET.triggers.Add(TriggerHelper.ScrollDeltaVector2(randomValue.GetChild(0).GetComponent<InputField>(), randomValue.GetChild(1).GetComponent<InputField>(), 0.1f, 10f));
                            }
                            else
                            {
                                if (!randomValue.GetComponent<InputFieldSwapper>())
                                {
                                    var ifh = randomValue.gameObject.AddComponent<InputFieldSwapper>();
                                    ifh.Init(randomValueX, InputFieldSwapper.Type.Num);
                                }

                                var randET = randomValue.GetComponent<EventTrigger>();
                                randET.triggers.Clear();
                                randET.triggers.Add(TriggerHelper.ScrollDelta(randomValueX, 15f, 3f));
                            }
                        }
                    }
                }
                else
                {
                    int num6 = 0;
                    foreach (var toggle in ObjEditor.inst.colorButtons)
                    {
                        toggle.onValueChanged.RemoveAllListeners();
                        int tmpIndex = num6;

                        toggle.NewValueChangedListener(num6 == keyframe.eventValues[0], delegate (bool _val)
                        {
                            SetKeyframeColor(beatmapObject, 0, tmpIndex);
                        });

                        toggle.onValueChanged.AddListener(delegate (bool _value)
                        {
                            SetKeyframeColor(beatmapObject, 0, tmpIndex);
                        });

                        if (RTEditor.ShowModifiedColors)
                        {
                            var color = GameManager.inst.LiveTheme.GetObjColor(tmpIndex);

                            float hue = beatmapObject.Interpolate(type, 2);
                            float sat = beatmapObject.Interpolate(type, 3);
                            float val = beatmapObject.Interpolate(type, 4);

                            toggle.GetComponent<Image>().color = RTHelpers.ChangeColorHSV(color, hue, sat, val);
                        }
                        else
                            toggle.GetComponent<Image>().color = GameManager.inst.LiveTheme.GetObjColor(tmpIndex);

                        //if (!toggle.GetComponent<HoverUI>())
                        //{
                        //    var hoverUI = toggle.gameObject.AddComponent<HoverUI>();
                        //    hoverUI.animatePos = false;
                        //    hoverUI.animateSca = true;
                        //    hoverUI.size = 1.1f;
                        //}
                        num6++;

                        if (p.Find("opacity"))
                        {
                            p.Find("color").GetComponent<RectTransform>().sizeDelta = new Vector2(366f, 78f);

                            var opacity = p.Find("opacity/x").GetComponent<InputField>();

                            opacity.onValueChanged.RemoveAllListeners();
                            opacity.text = Mathf.Clamp(-keyframe.eventValues[1] + 1, 0f, 1f).ToString();
                            opacity.onValueChanged.AddListener(delegate (string _val)
                            {
                                if (float.TryParse(_val, out float n))
                                {
                                    keyframe.eventValues[1] = Mathf.Clamp(-n + 1, 0f, 1f);

                                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                    Updater.UpdateProcessor(beatmapObject);
                                }
                            });

                            TriggerHelper.AddEventTrigger(p.Find("opacity").gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(opacity, 0.1f, 10f, 0f, 1f) });
                        }

                        if (p.Find("huesatval"))
                        {
                            var hue = p.Find("huesatval/x").GetComponent<InputField>();

                            hue.onValueChanged.RemoveAllListeners();
                            hue.text = keyframe.eventValues[2].ToString();
                            hue.onValueChanged.AddListener(delegate (string _val)
                            {
                                if (float.TryParse(_val, out float n))
                                {
                                    keyframe.eventValues[2] = n;

                                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                    if (UpdateObjects)
                                        Updater.UpdateProcessor(beatmapObject, "Keyframes");
                                }
                            });

                            //hue.gameObject.AddComponent<InputFieldHelper>();

                            Destroy(p.transform.Find("huesatval").GetComponent<EventTrigger>());

                            TriggerHelper.AddEventTrigger(hue.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(hue, 0.1f, 10f) });
                            TriggerHelper.IncreaseDecreaseButtons(hue, 0.1f, 10f);

                            var sat = p.Find("huesatval/y").GetComponent<InputField>();

                            sat.onValueChanged.RemoveAllListeners();
                            sat.text = keyframe.eventValues[3].ToString();
                            sat.onValueChanged.AddListener(delegate (string _val)
                            {
                                if (float.TryParse(_val, out float n))
                                {
                                    keyframe.eventValues[3] = n;

                                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                    if (UpdateObjects)
                                        Updater.UpdateProcessor(beatmapObject, "Keyframes");
                                }
                            });

                            //sat.gameObject.AddComponent<InputFieldHelper>();

                            TriggerHelper.AddEventTrigger(sat.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(sat, 0.1f, 10f) });
                            TriggerHelper.IncreaseDecreaseButtons(sat, 0.1f, 10f);

                            var val = p.Find("huesatval/z").GetComponent<InputField>();

                            val.onValueChanged.RemoveAllListeners();
                            val.text = keyframe.eventValues[4].ToString();
                            val.onValueChanged.AddListener(delegate (string _val)
                            {
                                if (float.TryParse(_val, out float n))
                                {
                                    keyframe.eventValues[4] = n;

                                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                    if (UpdateObjects)
                                        Updater.UpdateProcessor(beatmapObject, "Keyframes");
                                }
                            });

                            //val.gameObject.AddComponent<InputFieldHelper>();

                            TriggerHelper.AddEventTrigger(val.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(val, 0.1f, 10f) });
                            TriggerHelper.IncreaseDecreaseButtons(val, 0.1f, 10f);
                        }
                    }
                }
                //Debug.LogFormat("{0}Refresh Object GUI: Keyframes done", EditorPlugin.className);
            }

            yield break;
        }

        public static Sprite GetKeyframeIcon(DataManager.LSAnimation _ease, DataManager.LSAnimation _easeNext)
        {
            if (_ease.Name.ToString().Contains("Out") && _easeNext.Name.ToString().Contains("In"))
                return ObjEditor.inst.KeyframeSprites[3];
            if (_ease.Name.ToString().Contains("Out"))
                return ObjEditor.inst.KeyframeSprites[2];
            if (_easeNext.Name.ToString().Contains("In"))
                return ObjEditor.inst.KeyframeSprites[1];
            return ObjEditor.inst.KeyframeSprites[0];
        }

        public void UpdateKeyframeOrder(BeatmapObject beatmapObject, bool _setCurrent = true)
        {
            var list = new List<BaseEventKeyframe>();
            foreach (var timelineObject in SelectedBeatmapObjectKeyframes)
            {
                list.Add(timelineObject.Data);
            }

            for (int i = 0; i <= 3; i++)
            {
                beatmapObject.events[i] = (from x in beatmapObject.events[i]
                                            orderby x.eventTime
                                            select x).ToList();
            }

            int num = 0;
            foreach (var item in list)
            {
                int index = beatmapObject.events[SelectedBeatmapObjectKeyframes[num].Type].IndexOf(item);
                SelectedBeatmapObjectKeyframes[num].Index = index;
                num++;
            }
            list.Clear();
            list = null;
        }

        public static string IntToAxis(int num)
        {
            switch (num)
            {
                case 0: return "x";
                case 1: return "y";
                case 2: return "z";
                case 3: return "w";
                default: throw new Exception("Axis out of dimensional range.");
            }
        }

        public static string IntToType(int num)
        {
            switch (num)
            {
                case 0: return "pos";
                case 1: return "sca";
                case 2: return "rot";
                case 3: return "col";
                default: throw new Exception($"No recognized Keyframe Type at {num}.");
            }
        }

        #endregion

        #region Set Values

        public void SetKeyframeTime(BeatmapObject beatmapObject, float val, bool updateText)
        {
            if (!ObjEditor.inst.timelineKeyframesDrag)
            {
                if (val < 0f)
                    val = 0f;

                if (RTEditor.RoundToNearest)
                    val = RTMath.RoundToNearestDecimal(val, 3);

                if (updateText)
                    ObjEditor.inst.KeyframeDialogs[ObjEditor.inst.currentKeyframeKind].transform.Find("time/time").GetComponent<InputField>().text = val.ToString();

                beatmapObject.events[ObjEditor.inst.currentKeyframeKind][ObjEditor.inst.currentKeyframe].eventTime = val;

                inst.ResizeKeyframeTimeline(beatmapObject);

                inst.CreateKeyframes(beatmapObject);

                // Keyframe Time affects both physical object and timeline object.
                if (RTEditor.inst.timelineBeatmapObjects.ContainsKey(beatmapObject.id))
                    RenderTimelineObject(RTEditor.inst.timelineBeatmapObjects[beatmapObject.id]);
                if (UpdateObjects)
                    Updater.UpdateProcessor(beatmapObject, "Keyframes");
            }
        }

        public void AddKeyframeTime(BeatmapObject beatmapObject, float val, bool updateText)
        {
            if (RTEditor.RoundToNearest)
                val = RTMath.RoundToNearestDecimal(val);

            if (RTEditor.RoundToNearest)
                val = RTMath.RoundToNearestDecimal(beatmapObject.events[ObjEditor.inst.currentKeyframeKind][ObjEditor.inst.currentKeyframe].eventTime + val);
            else
                val = beatmapObject.events[ObjEditor.inst.currentKeyframeKind][ObjEditor.inst.currentKeyframe].eventTime + val;

            SetKeyframeTime(beatmapObject, val, updateText);
        }

        public void SetKeyframePositionR(BeatmapObject beatmapObject, int index, float val, bool updateText)
        {
            beatmapObject.events[0][ObjEditor.inst.currentKeyframe].eventRandomValues[index] = val;
            if (updateText)
                ObjEditor.inst.KeyframeDialogs[ObjEditor.inst.currentKeyframeKind].transform.Find("r_position/" + ((index == 0) ? "x" : "y")).GetComponent<InputField>().text =
                    beatmapObject.events[0][ObjEditor.inst.currentKeyframe].eventRandomValues[index].ToString();

            if (UpdateObjects)
                Updater.UpdateProcessor(beatmapObject, "Keyframes");
        }

        public void AddKeyframePositionR(BeatmapObject beatmapObject, int index, float add, bool updateText)
            => SetKeyframePositionR(beatmapObject, index, beatmapObject.events[0][ObjEditor.inst.currentKeyframe].eventRandomValues[index] + add, updateText);

        public void SetKeyframePosition(BeatmapObject beatmapObject, int index, float value, bool updateText)
        {
            beatmapObject.events[0][ObjEditor.inst.currentKeyframe].eventValues[index] = value;
            if (updateText)
                ObjEditor.inst.KeyframeDialogs[ObjEditor.inst.currentKeyframeKind].transform.Find("position/" + ((index == 0) ? "x" : "y")).GetComponent<InputField>().text =
                    beatmapObject.events[0][ObjEditor.inst.currentKeyframe].eventValues[index].ToString();

            if (UpdateObjects)
                Updater.UpdateProcessor(beatmapObject, "Keyframes");
        }

        public void AddKeyframePosition(BeatmapObject beatmapObject, int _index, float _add, bool _updateText)
            => SetKeyframePosition(beatmapObject, _index, beatmapObject.events[0][ObjEditor.inst.currentKeyframe].eventValues[_index] + _add, _updateText);

        public void SetKeyframeScaleR(BeatmapObject beatmapObject, int _index, float _value, bool _updateText)
        {
            beatmapObject.events[1][ObjEditor.inst.currentKeyframe].eventRandomValues[_index] = _value;
            if (_updateText)
                ObjEditor.inst.KeyframeDialogs[ObjEditor.inst.currentKeyframeKind].transform.Find("r_scale/" + ((_index == 0) ? "x" : "y")).GetComponent<InputField>().text =
                    beatmapObject.events[1][ObjEditor.inst.currentKeyframe].eventRandomValues[_index].ToString();

            if (UpdateObjects)
                Updater.UpdateProcessor(beatmapObject, "Keyframes");
        }

        public void AddKeyframeScaleR(BeatmapObject beatmapObject, int _index, float _add, bool _updateText)
            => SetKeyframeScaleR(beatmapObject, _index, beatmapObject.events[1][ObjEditor.inst.currentKeyframe].eventRandomValues[_index] + _add, _updateText);

        public void SetKeyframeScale(BeatmapObject beatmapObject, int _index, float _value, bool _updateText, bool _updateSlider)
        {
            beatmapObject.events[1][ObjEditor.inst.currentKeyframe].eventValues[_index] = _value;
            if (_updateText)
                ObjEditor.inst.KeyframeDialogs[ObjEditor.inst.currentKeyframeKind].transform.Find("scale/" + ((_index == 0) ? "x" : "y")).GetComponent<InputField>().text =
                    beatmapObject.events[1][ObjEditor.inst.currentKeyframe].eventValues[_index].ToString();

            if (UpdateObjects)
                Updater.UpdateProcessor(beatmapObject, "Keyframes");
        }
        // ew why did this say "_updateSlider" before
        public void AddKeyframeScale(BeatmapObject beatmapObject, int _index, float _add, bool _updateText, bool _updateSlider)
           => SetKeyframeScale(beatmapObject, _index, beatmapObject.events[1][ObjEditor.inst.currentKeyframe].eventValues[_index] + _add, _updateText, _updateSlider);

        public void SetKeyframeRotationR(BeatmapObject beatmapObject, int _index, float _value, bool _updateText)
        {
            beatmapObject.events[2][ObjEditor.inst.currentKeyframe].eventRandomValues[_index] = _value;
            if (_updateText)
                ObjEditor.inst.KeyframeDialogs[ObjEditor.inst.currentKeyframeKind].transform.Find("r_rotation/" + ((_index == 0) ? "x" : "y")).GetComponent<InputField>().text =
                    beatmapObject.events[2][ObjEditor.inst.currentKeyframe].eventRandomValues[_index].ToString();

            if (UpdateObjects)
                Updater.UpdateProcessor(beatmapObject, "Keyframes");
        }

        public void AddKeyframeRotationR(BeatmapObject beatmapObject, int _index, float _add, bool _updateText)
            => SetKeyframeRotationR(beatmapObject, _index, beatmapObject.events[2][ObjEditor.inst.currentKeyframe].eventRandomValues[_index] + _add, _updateText);

        public void SetKeyframeRotation(BeatmapObject beatmapObject, int _index, float _value, bool _updateText)
        {
            beatmapObject.events[2][ObjEditor.inst.currentKeyframe].eventValues[_index] = _value;
            if (_updateText)
            {
                ObjEditor.inst.KeyframeDialogs[ObjEditor.inst.currentKeyframeKind].transform.Find("rotation/" + ((_index == 0) ? "x" : "y")).GetComponent<InputField>().text =
                    beatmapObject.events[2][ObjEditor.inst.currentKeyframe].eventValues[_index].ToString();
            }

            if (UpdateObjects)
                Updater.UpdateProcessor(beatmapObject, "Keyframes");
        }

        public void AddKeyframeRotation(BeatmapObject beatmapObject, int _index, float _add, bool _updateText)
            => SetKeyframeRotation(beatmapObject, _index, beatmapObject.events[2][ObjEditor.inst.currentKeyframe].eventValues[_index] + _add, _updateText);

        public void SetKeyframeColor(BeatmapObject beatmapObject, int index, int value)
        {
            beatmapObject.events[3][ObjEditor.inst.currentKeyframe].eventValues[index] = (float)value;

            // Since keyframe color has no affect on the timeline object, we will only need to update the physical object.
            if (UpdateObjects)
                Updater.UpdateProcessor(beatmapObject, "Keyframes");

            int num = 0;
            foreach (var toggle in ObjEditor.inst.colorButtons)
            {
                toggle.onValueChanged.RemoveAllListeners();

                if (num == value)
                    toggle.isOn = true;
                else
                    toggle.isOn = false;

                int tmpIndex = num;
                toggle.onValueChanged.AddListener(delegate (bool val)
                {
                    SetKeyframeColor(beatmapObject, 0, tmpIndex);
                });
                num++;
            }
        }

        public void AddKeyframeColor(BeatmapObject beatmapObject, int _index, int _add)
        {
            int value = Mathf.Clamp((int)beatmapObject.events[3][ObjEditor.inst.currentKeyframe].eventValues[_index] + _add, -1, 5);
            SetKeyframeColor(beatmapObject, _index, value);
        }

        public void SetParent(BeatmapObject beatmapObject, string _parent)
        {
            beatmapObject.parent = _parent;
            if (UpdateObjects)
                Updater.UpdateProcessor(beatmapObject, "Parent");
            RenderParent(beatmapObject);
        }

        public void SetStartTime(BeatmapObject beatmapObject, float _time)
        {
            beatmapObject.StartTime = Mathf.Clamp(_time, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
            if (UpdateObjects)
                Updater.UpdateProcessor(beatmapObject, "Start Time");

            if (RTEditor.inst.timelineBeatmapObjects.ContainsKey(beatmapObject.id))
                RenderTimelineObject(RTEditor.inst.timelineBeatmapObjects[beatmapObject.id]);

            ResizeKeyframeTimeline(beatmapObject);
        }

        public void SetStartTime(BeatmapObject beatmapObject, string _time)
        {
            if (float.TryParse(_time, out float num))
                SetStartTime(beatmapObject, num);
        }

        public void SetStartTimeToCurrentTime(BeatmapObject beatmapObject) => SetStartTime(beatmapObject, EditorManager.inst.CurrentAudioPos);

        public void AddToStartTime(BeatmapObject beatmapObject, float _add)
        {
            float num = beatmapObject.StartTime + _add;
            SetStartTime(beatmapObject, num);
        }

        public void SetLayer(BeatmapObject beatmapObject, int _layer)
        {
            beatmapObject.editorData.Layer = _layer;

            if (RTEditor.inst.timelineBeatmapObjects.ContainsKey(beatmapObject.id))
                RenderTimelineObject(RTEditor.inst.timelineBeatmapObjects[beatmapObject.id]);
        }

        public void AddToLayer(BeatmapObject beatmapObject, int _add)
        {
            int num = beatmapObject.editorData.Layer + _add;
            SetLayer(beatmapObject, num);
        }

        public void SetBin(BeatmapObject beatmapObject, int _bin)
        {
            beatmapObject.editorData.Bin = Mathf.Clamp(_bin, 0, 14);

            if (RTEditor.inst.timelineBeatmapObjects.ContainsKey(beatmapObject.id))
                RenderTimelineObject(RTEditor.inst.timelineBeatmapObjects[beatmapObject.id]);
        }

        public void AddToBin(BeatmapObject beatmapObject, int _add)
        {
            int num = beatmapObject.editorData.Bin + _add;
            SetBin(beatmapObject, num);
        }

        public void SetDepth(BeatmapObject beatmapObject, int _depth)
        {
            beatmapObject.Depth = _depth;
            if (UpdateObjects)
                Updater.UpdateProcessor(beatmapObject, "Depth");

            if (RTEditor.inst.timelineBeatmapObjects.ContainsKey(beatmapObject.id))
                RenderTimelineObject(RTEditor.inst.timelineBeatmapObjects[beatmapObject.id]);
        }

        public void SetShape(BeatmapObject beatmapObject, int _x, int _y) => SetShape(beatmapObject, _x, _y, "");

        public void SetShape(BeatmapObject beatmapObject, string text) => SetShape(beatmapObject, 6, 0, text);

        public void SetShape(BeatmapObject beatmapObject, int s, int so, string text)
        {
            beatmapObject.shape = s;
            beatmapObject.shapeOption = so;
            beatmapObject.text = text;

            if (UpdateObjects)
                Updater.UpdateProcessor(beatmapObject, "Shape");

            RenderShape(beatmapObject);
        }

        public void SetOriginX(BeatmapObject beatmapObject, float _x) => SetOrigin(beatmapObject, 0, _x);

        public void SetOriginY(BeatmapObject beatmapObject, float _y) => SetOrigin(beatmapObject, 1, _y);

        public void SetOrigin(BeatmapObject beatmapObject, int value, float v)
        {
            switch (value)
            {
                case 0:
                    {
                        beatmapObject.origin.x = v;
                        break;
                    }
                case 1:
                    {
                        beatmapObject.origin.y = v;
                        break;
                    }
            }

            if (UpdateObjects)
                Updater.UpdateProcessor(beatmapObject, "Origin");
        }

        #endregion
    }
}
