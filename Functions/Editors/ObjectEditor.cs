using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
using RTFunctions.Functions.Components;
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

        public static void Init(ObjEditor objEditor) => objEditor?.gameObject?.AddComponent<ObjectEditor>();

        void Awake()
        {
            inst = this;

            if (ModCompatibility.mods.ContainsKey("EditorManagement"))
            {
                var mod = ModCompatibility.mods["EditorManagement"];
                if (!mod.Methods.ContainsKey("SetCurrentObject"))
                    mod.Methods.Add("SetCurrentObject", (Action<TimelineObject, bool>)SetCurrentObjectP);
                if (!mod.Methods.ContainsKey("AddSelectedObject"))
                    mod.Methods.Add("AddSelectedObject", (Action<TimelineObject>)AddSelectedObject);

                if (!mod.Methods.ContainsKey("RenderKeyframes"))
                    mod.Methods.Add("RenderKeyframes", (Action<BeatmapObject>)RenderKeyframes);
                if (!mod.Methods.ContainsKey("RenderKeyframeDialog"))
                    mod.Methods.Add("RenderKeyframeDialog", (Action<BeatmapObject>)RenderKeyframeDialog);
                if (!mod.Methods.ContainsKey("RenderTimelineObjectVoid"))
                    mod.Methods.Add("RenderTimelineObjectVoid", (Action<TimelineObject>)RenderTimelineObjectVoid);

                if (!mod.Methods.ContainsKey("SetCurrentKeyframe"))
                    mod.Methods.Add("SetCurrentKeyframe", (Action<BeatmapObject, int, int, bool, bool>)SetCurrentKeyframe);

                if (!mod.Methods.ContainsKey("GetTimelineObject"))
                    mod.Methods.Add("GetTimelineObject", (TimelineObjectReturn<BeatmapObject>)GetTimelineObject);
            }

            timelinePosScrollbar = ObjEditor.inst.objTimelineContent.parent.parent.GetComponent<ScrollRect>().horizontalScrollbar;
            timelinePosScrollbar.onValueChanged.AddListener(delegate (float _val)
            {
                if (CurrentSelection.IsBeatmapObject)
                    CurrentSelection.TimelinePosition = _val;
            });

            ObjEditor.inst.zoomSlider.onValueChanged.AddListener(delegate (float _val)
            {
                if (CurrentSelection.IsBeatmapObject)
                    CurrentSelection.Zoom = _val;
            });
        }


        void Update()
        {
            if (!ModCompatibility.sharedFunctions.ContainsKey("SelectedObjectCount"))
                ModCompatibility.sharedFunctions.Add("SelectedObjectCount", SelectedObjectCount);
            else
                ModCompatibility.sharedFunctions["SelectedObjectCount"] = SelectedObjectCount;
        }

        public Scrollbar timelinePosScrollbar;
        public GameObject shapeButtonPrefab;

        public TimelineObject CurrentSelection { get; set; } = new TimelineObject(null);
        //public TimelineObject CurrentBeatmapObjectSelection => SelectedBeatmapObjects.Last();
        //public TimelineObject CurrentPrefabObjectSelection => SelectedPrefabObjects.Last();

        public List<TimelineObject> SelectedObjects => RTEditor.inst.timelineObjects.FindAll(x => x.selected && !x.IsEventKeyframe);
        public List<TimelineObject> SelectedBeatmapObjects => RTEditor.inst.TimelineBeatmapObjects.FindAll(x => x.selected);
        public List<TimelineObject> SelectedPrefabObjects => RTEditor.inst.TimelinePrefabObjects.FindAll(x => x.selected);

        public int SelectedObjectCount => SelectedObjects.Count;

        public List<TimelineObject> copiedObjectKeyframes = new List<TimelineObject>();

        public EventKeyframe CopiedPositionData { get; set; }
        public EventKeyframe CopiedScaleData { get; set; }
        public EventKeyframe CopiedRotationData { get; set; }
        public EventKeyframe CopiedColorData { get; set; }

        public void OpenDialog(BeatmapObject beatmapObject)
        {
            if (CurrentSelection.IsBeatmapObject)
            {
                EditorManager.inst.ClearDialogs(Array.Empty<EditorManager.EditorDialog.DialogType>());
                EditorManager.inst.ShowDialog("Object Editor", false);

                StartCoroutine(RefreshObjectGUI(beatmapObject));

                // Here we remember an object's zoom and timeline position.
                ObjEditor.inst.Zoom = CurrentSelection.Zoom;

                timelinePosScrollbar.value = CurrentSelection.TimelinePosition;

            }
            else
                EditorManager.inst.DisplayNotification("Cannot edit non-object!", 2f, EditorManager.NotificationType.Error);
        }

        #region Deleting

        public IEnumerator DeleteObjects(bool _set = true)
        {
            var list = SelectedObjects;
            int count = SelectedObjectCount;
            int min = list.Min(x => x.Index) - 1;

            EditorManager.inst.DisplayNotification($"Deleting Beatmap Objects [ {count} ]", 1f, EditorManager.NotificationType.Success);

            var beatmapObjects = list.Where(x => x.IsBeatmapObject).Select(x => x.GetData<BeatmapObject>()).ToList();
            var beatmapObjectIDs = new List<string>();
            var prefabObjectIDs = new List<string>();

            beatmapObjectIDs.AddRange(list.Where(x => x.IsBeatmapObject).Select(x => x.ID));
            prefabObjectIDs.AddRange(list.Where(x => x.IsPrefabObject).Select(x => x.ID));

            if (prefabObjectIDs.Count > 0)
                list.Where(x => x.IsPrefabObject)
                    .Select(x => x.GetData<PrefabObject>()).ToList()
                    .ForEach(x => beatmapObjectIDs
                        .AddRange(DataManager.inst.gameData.beatmapObjects
                            .Where(c => c.prefabInstanceID == x.ID)
                        .Select(c => c.id)));

            DataManager.inst.gameData.beatmapObjects.Where(x => beatmapObjectIDs.Contains(x.id)).ToList().ForEach(x => Updater.UpdateProcessor(x, reinsert: false));
            DataManager.inst.gameData.beatmapObjects.Where(x => prefabObjectIDs.Contains(x.prefabInstanceID)).ToList().ForEach(x => Updater.UpdateProcessor(x, reinsert: false));

            DataManager.inst.gameData.beatmapObjects.RemoveAll(x => beatmapObjectIDs.Contains(x.id));
            DataManager.inst.gameData.beatmapObjects.RemoveAll(x => prefabObjectIDs.Contains(x.prefabInstanceID));
            DataManager.inst.gameData.prefabObjects.RemoveAll(x => prefabObjectIDs.Contains(x.ID));

            RTEditor.inst.timelineObjects.Where(x => beatmapObjectIDs.Contains(x.ID) || prefabObjectIDs.Contains(x.ID)).ToList().ForEach(x => Destroy(x.GameObject));
            RTEditor.inst.timelineObjects.RemoveAll(x => beatmapObjectIDs.Contains(x.ID) || prefabObjectIDs.Contains(x.ID));

            SetCurrentObject(RTEditor.inst.timelineObjects[Mathf.Clamp(min, 0, RTEditor.inst.timelineObjects.Count - 1)]);

            EditorManager.inst.DisplayNotification($"Deleted Beatmap Objects [ {count} ]", 1f, EditorManager.NotificationType.Success);
            yield break;
        }

        public IEnumerator DeleteObject(TimelineObject timelineObject, bool _set = true)
        {
            int index = timelineObject.Index;

            RTEditor.inst.RemoveTimelineObject(timelineObject);

            if (timelineObject.IsBeatmapObject)
            {
                var beatmapObject = timelineObject.GetData<BeatmapObject>();
                Updater.UpdateProcessor(beatmapObject, reinsert: false);

                if (DataManager.inst.gameData.beatmapObjects.Count > 1)
                {
                    string id = beatmapObject.id;

                    index = DataManager.inst.gameData.beatmapObjects.FindIndex(x => x.id == id);

                    DataManager.inst.gameData.beatmapObjects.RemoveAt(index);

                    foreach (var bm in DataManager.inst.gameData.beatmapObjects)
                    {
                        if (bm.parent == id)
                        {
                            bm.parent = "";

                            Updater.UpdateProcessor(bm);
                        }
                    }
                }
                else
                    EditorManager.inst.DisplayNotification("Can't delete only object", 2f, EditorManager.NotificationType.Error);
            }
            else if (timelineObject.IsPrefabObject)
            {
                var prefabObject = timelineObject.GetData<PrefabObject>();

                Updater.UpdatePrefab(prefabObject, false);

                string id = prefabObject.ID;

                index = DataManager.inst.gameData.prefabObjects.FindIndex(x => x.ID == id);
                DataManager.inst.gameData.prefabObjects.RemoveAt(index);
            }

            if (_set && RTEditor.inst.timelineObjects.Count > 0)
                SetCurrentObject(RTEditor.inst.timelineObjects[Mathf.Clamp(index - 1, 0, RTEditor.inst.timelineObjects.Count - 1)]);

            yield break;
        }

        public IEnumerator DeleteKeyframes()
        {
            if (CurrentSelection.IsBeatmapObject)
                yield return DeleteKeyframes(CurrentSelection.GetData<BeatmapObject>());
            yield break;
        }

        public IEnumerator DeleteKeyframes(BeatmapObject beatmapObject)
        {
            var bmTimelineObject = GetTimelineObject(beatmapObject);

            var list = bmTimelineObject.InternalSelections.Where(x => x.selected).ToList();
            int count = list.Where(x => x.Index != 0).Count();

            EditorManager.inst.DisplayNotification("Deleting Object Keyframes [ " + count + " ]", 0.2f, EditorManager.NotificationType.Success);

            UpdateKeyframeOrder(beatmapObject);

            var strs = new List<string>();
            foreach (var timelineObject in list)
            {
                if (timelineObject.Index != 0)
                    strs.Add(timelineObject.GetData<EventKeyframe>().id);
            }

            for (int i = 0; i < beatmapObject.events.Count; i++)
            {
                beatmapObject.events[i].RemoveAll(x => strs.Contains(((EventKeyframe)x).id));
            }

            var ti = GetTimelineObject(beatmapObject);
            ti.InternalSelections.Where(x => x.selected).ToList().ForEach(x => Destroy(x.GameObject));
            ti.InternalSelections.RemoveAll(x => x.selected);

            RenderTimelineObject(bmTimelineObject);
            Updater.UpdateProcessor(beatmapObject, "Keyframes");

            RenderKeyframes(beatmapObject);

            SetCurrentKeyframe(beatmapObject, 0);
            ResizeKeyframeTimeline(beatmapObject);

            EditorManager.inst.DisplayNotification("Deleted Object Keyframes [ " + count + " ]", 1f, EditorManager.NotificationType.Success);

            yield break;
        }

        public void DeleteKeyframe(BeatmapObject beatmapObject, TimelineObject timelineObject)
        {
            if (timelineObject.Index != 0)
            {
                Debug.Log($"{ObjEditor.inst.className}Deleting keyframe: ({timelineObject.Type}, {timelineObject.Index})");
                beatmapObject.events[timelineObject.Type].RemoveAt(timelineObject.Index);

                Destroy(timelineObject.GameObject);

                RenderTimelineObject(new TimelineObject(beatmapObject));
                if (UpdateObjects)
                    Updater.UpdateProcessor(beatmapObject, "Keyframes");
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
            //float num = float.PositiveInfinity;

            var bmTimelineObject = GetTimelineObject(beatmapObject);

            float num = bmTimelineObject.InternalSelections.Where(x => x.selected).Min(x => x.Time);

            foreach (var timelineObject in bmTimelineObject.InternalSelections.Where(x => x.selected))
            {
                int type = timelineObject.Type;
                int index = timelineObject.Index;
                var eventKeyframe = EventKeyframe.DeepCopy((EventKeyframe)beatmapObject.events[type][index]);
                eventKeyframe.eventTime -= num;

                var otherTLO = new TimelineObject(eventKeyframe);
                otherTLO.Type = type;
                otherTLO.Index = index;

                copiedObjectKeyframes.Add(otherTLO);
            }
        }

        public void PasteKeyframes(BeatmapObject beatmapObject)
        {
            if (copiedObjectKeyframes.Count <= 0)
            {
                Debug.LogError($"{ObjEditor.inst.className}No copied event yet!");
                return;
            }

            //foreach (var timelineObject in copiedObjectKeyframes)
            //{
            //    var eventKeyframe = EventKeyframe.DeepCopy(timelineObject.GetData<EventKeyframe>());

            //    eventKeyframe.eventTime = EditorManager.inst.CurrentAudioPos - beatmapObject.StartTime + eventKeyframe.eventTime;
            //    if (eventKeyframe.eventTime <= 0f)
            //        eventKeyframe.eventTime = 0.001f;

            //    beatmapObject.events[timelineObject.Type].Add(eventKeyframe);
            //}

            for (int i = 0; i < beatmapObject.events.Count; i++)
                beatmapObject.events[i].AddRange(copiedObjectKeyframes.Where(x => x.Type == i).Select(x => PasteKF(beatmapObject, x)));

            ResizeKeyframeTimeline(beatmapObject);
            UpdateKeyframeOrder(beatmapObject);
            RenderTimelineObject(new TimelineObject(beatmapObject));

            if (UpdateObjects)
                Updater.UpdateProcessor(beatmapObject, "Keyframes");
        }

        EventKeyframe PasteKF(BeatmapObject beatmapObject, TimelineObject timelineObject)
        {
            var eventKeyframe = EventKeyframe.DeepCopy(timelineObject.GetData<EventKeyframe>());

            eventKeyframe.eventTime = EditorManager.inst.CurrentAudioPos - beatmapObject.StartTime + eventKeyframe.eventTime;
            if (eventKeyframe.eventTime <= 0f)
                eventKeyframe.eventTime = 0.001f;

            return eventKeyframe;
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

            if (RTFile.FileExists(Application.persistentDataPath + "/copied_objects.lsp"))
            {
                pr = Prefab.Parse(JSON.Parse(FileManager.inst.LoadJSONFileRaw(Application.persistentDataPath + "/copied_objects.lsp")));

                ObjEditor.inst.hasCopiedObject = true;
            }

            StartCoroutine(AddPrefabExpandedToLevel(pr ?? ObjEditor.inst.beatmapObjCopy, true, _offsetTime, false, _regen));

            //if (pr == null)
            //    inst.StartCoroutine(AddPrefabExpandedToLevel(ObjEditor.inst.beatmapObjCopy, true, _offsetTime, false, _regen));
            //else
            //    inst.StartCoroutine(AddPrefabExpandedToLevel(pr, true, _offsetTime, false, _regen));
        }

        #endregion

        #region Prefabs

        public BeatmapObject Expand(BeatmapObject beatmapObject, Dictionary<string, string> ids, Dictionary<string, string> prefabInstances, float audioTime,
            BasePrefab prefab, bool select = false, float offset = 0f, bool undone = false, bool regen = false)
        {
            var beatmapObjectCopy = BeatmapObject.DeepCopy(beatmapObject, false);

            if (ids.ContainsKey(beatmapObject.id))
                beatmapObjectCopy.id = ids[beatmapObject.id];

            if (ids.ContainsKey(beatmapObject.parent))
                beatmapObjectCopy.parent = ids[beatmapObject.parent];

            else if (DataManager.inst.gameData.beatmapObjects.FindIndex(x => x.id == beatmapObject.parent) == -1)
                beatmapObjectCopy.parent = "";

            beatmapObjectCopy.prefabID = beatmapObject.prefabID;
            if (regen) // < Probably need to add another setting to have this separate an object from being a prefab or generate a new one
            {
                //beatmapObjectCopy.prefabInstanceID = dictionary2[orig.prefabInstanceID];
                beatmapObjectCopy.prefabID = "";
                beatmapObjectCopy.prefabInstanceID = "";
            }
            else
            {
                beatmapObjectCopy.prefabInstanceID = beatmapObject.prefabInstanceID;
            }

            beatmapObjectCopy.fromPrefab = false;

            beatmapObjectCopy.StartTime += offset == 0.0 ? undone ? prefab.Offset : audioTime + prefab.Offset : offset;

            beatmapObjectCopy.editorData.layer = RTEditor.inst.Layer;

            return beatmapObjectCopy;
        }

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
        public IEnumerator AddPrefabExpandedToLevel(BasePrefab prefab, bool select = false, float offset = 0f, bool undone = false, bool regen = false)
        {
            RTEditor.inst.ienumRunning = true;
            float delay = 0f;
            float audioTime = EditorManager.inst.CurrentAudioPos;
            Debug.Log($"{EditorPlugin.className}Placing prefab with {prefab.objects.Count} objects and {prefab.prefabObjects.Count} prefabs");

            if (RTEditor.inst.layerType == RTEditor.LayerType.Events)
                RTEditor.inst.SetLayer(RTEditor.LayerType.Objects);

            //Objects
            {
                var ids = prefab.objects.ToDictionary(x => x.id, x => LSText.randomString(16));

                //foreach (var beatmapObject in prefab.objects)
                //    ids.Add(beatmapObject.id, LSText.randomString(16));

                var prefabInstances = new Dictionary<string, string>();
                foreach (var beatmapObject in prefab.objects)
                    if (!string.IsNullOrEmpty(beatmapObject.prefabInstanceID) && !prefabInstances.ContainsKey(beatmapObject.prefabInstanceID))
                        prefabInstances.Add(beatmapObject.prefabInstanceID, LSText.randomString(16));

                //var list = prefab.objects.Select(x => Expand((BeatmapObject)x, ids, prefabInstances, audioTime, prefab, select, offset, undone, regen)).ToList();

                //DataManager.inst.gameData.beatmapObjects.AddRange(list);

                //yield return list.Select(x => new TimelineObject(x)).ToList().ForEachReturn(x =>
                //{
                //    RenderTimelineObject(x);

                //    if (select)
                //    {
                //        if (list.Count > 1)
                //        {
                //            x.selected = true;
                //            CurrentSelection = x;

                //            return;
                //        }

                //        SetCurrentObject(x, openDialog: false);
                //    }
                //});

                //foreach (var id in ids)
                //{
                //    if (DataManager.inst.gameData.beatmapObjects.TryFind(x => x.id == id.Value, out BaseBeatmapObject beatmapObject))
                //    {
                //        Updater.UpdateProcessor(beatmapObject);
                //    }
                //}

                foreach (var beatmapObject in prefab.objects)
                {
                    yield return new WaitForSeconds(delay);
                    var beatmapObjectCopy = BeatmapObject.DeepCopy((BeatmapObject)beatmapObject, false);

                    if (ids.ContainsKey(beatmapObject.id))
                        beatmapObjectCopy.id = ids[beatmapObject.id];

                    if (ids.ContainsKey(beatmapObject.parent))
                        beatmapObjectCopy.parent = ids[beatmapObject.parent];
                    else if (DataManager.inst.gameData.beatmapObjects.FindIndex(x => x.id == beatmapObject.parent) == -1)
                        beatmapObjectCopy.parent = "";

                    beatmapObjectCopy.prefabID = beatmapObject.prefabID;
                    if (regen) // < Probably need to add another setting to have this separate an object from being a prefab or generate a new one
                    {
                        //beatmapObjectCopy.prefabInstanceID = dictionary2[orig.prefabInstanceID];
                        beatmapObjectCopy.prefabID = "";
                        beatmapObjectCopy.prefabInstanceID = "";
                    }
                    else
                    {
                        beatmapObjectCopy.prefabInstanceID = beatmapObject.prefabInstanceID;
                    }

                    beatmapObjectCopy.fromPrefab = false;

                    beatmapObjectCopy.StartTime += offset == 0.0 ? undone ? prefab.Offset : audioTime + prefab.Offset : offset;
                    if (offset != 0.0)
                        ++beatmapObjectCopy.editorData.Bin;

                    beatmapObjectCopy.editorData.layer = RTEditor.inst.Layer;
                    DataManager.inst.gameData.beatmapObjects.Add(beatmapObjectCopy);

                    var timelineObject = new TimelineObject(beatmapObjectCopy);

                    timelineObject.selected = true;
                    CurrentSelection = timelineObject;

                    RenderTimelineObject(timelineObject);

                    Updater.UpdateProcessor(beatmapObject);

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
                    var prefabObjectCopy = PrefabObject.DeepCopy((PrefabObject)prefabObject, false);
                    if (prefabInstanceIDs.ContainsKey(prefabObject.ID))
                        prefabObjectCopy.ID = prefabInstanceIDs[prefabObject.ID];
                    prefabObjectCopy.prefabID = prefabObject.prefabID;

                    prefabObjectCopy.StartTime += offset == 0.0 ? undone ? prefab.Offset : audioTime + prefab.Offset : offset;
                    if (offset != 0.0)
                        ++prefabObjectCopy.editorData.Bin;

                    prefabObjectCopy.editorData.layer = RTEditor.inst.Layer;

                    DataManager.inst.gameData.prefabObjects.Add(prefabObjectCopy);

                    var timelineObject = new TimelineObject(prefabObjectCopy);

                    timelineObject.selected = true;
                    CurrentSelection = timelineObject;

                    RenderTimelineObject(timelineObject);

                    ObjectManager.inst.AddPrefabToLevel(prefabObjectCopy);

                    Updater.UpdatePrefab(prefabObject);

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

            if (select)
            {
                if (prefab.objects.Count > 1 || prefab.prefabObjects.Count > 1)
                    EditorManager.inst.ShowDialog("Multi Object Editor", false);
                else if (CurrentSelection.IsBeatmapObject)
                    OpenDialog(CurrentSelection.GetData<BeatmapObject>());
                else if (CurrentSelection.IsPrefabObject)
                    PrefabEditor.inst.OpenPrefabDialog();
            }

            RTEditor.inst.ienumRunning = false;
            yield break;
        }

        #endregion

        #region Create New Objects

        public static bool SetToCenterCam => RTEditor.GetEditorProperty("Create Objects at Camera Center").GetConfigEntry<bool>().Value;

        public void CreateNewNormalObject(bool _select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(_select);

            var bm = timelineObject.GetData<BeatmapObject>();
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

            var bm = timelineObject.GetData<BeatmapObject>();
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

            var bm = timelineObject.GetData<BeatmapObject>();
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

            var bm = timelineObject.GetData<BeatmapObject>();
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

            var bm = timelineObject.GetData<BeatmapObject>();
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

            var bm = timelineObject.GetData<BeatmapObject>();
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

            var bm = timelineObject.GetData<BeatmapObject>();
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

            var bm = timelineObject.GetData<BeatmapObject>();
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

            var bm = timelineObject.GetData<BeatmapObject>();
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

        public TimelineObject CreateNewDefaultObject(bool _select = true)
        {
            if (!EditorManager.inst.hasLoadedLevel)
            {
                EditorManager.inst.DisplayNotification("Can't add objects to level until a level has been loaded!", 2f, EditorManager.NotificationType.Error);
                return null;
            }

            var list = new List<List<BaseEventKeyframe>>
            {
                new List<BaseEventKeyframe>(),
                new List<BaseEventKeyframe>(),
                new List<BaseEventKeyframe>(),
                new List<BaseEventKeyframe>()
            };
            list[0].Add(new EventKeyframe(0f, new float[3]
                {
                    0f,
                    0f,
                    0f,
                }, new float[0], 0));
            list[1].Add(new EventKeyframe(0f, new float[]
            {
                1f,
                1f
            }, new float[0], 0));

            list[2].Add(new EventKeyframe(0f, new float[1], new float[0], 0));
            ((EventKeyframe)list[2][0]).relative = true;

            list[3].Add(new EventKeyframe(0f, new float[5], new float[0], 0));

            var beatmapObject = new BeatmapObject(true, AudioManager.inst.CurrentAudioSource.time, "", 0, "", list);
            beatmapObject.id = LSText.randomString(16);
            beatmapObject.autoKillType = AutoKillType.LastKeyframeOffset;
            beatmapObject.autoKillOffset = 5f;
            beatmapObject.editorData.layer = RTEditor.inst.Layer;

            if (RTEditor.inst.layerType == RTEditor.LayerType.Events)
                RTEditor.inst.SetLayer(RTEditor.LayerType.Objects);

            int num = DataManager.inst.gameData.beatmapObjects.FindIndex(x => x.fromPrefab);
            if (num == -1)
                DataManager.inst.gameData.beatmapObjects.Add(beatmapObject);
            else
                DataManager.inst.gameData.beatmapObjects.Insert(num, beatmapObject);

            var timelineObject = new TimelineObject(beatmapObject);

            RenderTimelineObject(timelineObject);
            Updater.UpdateProcessor(beatmapObject);

            AudioManager.inst.SetMusicTime(AllowTimeExactlyAtStart ? AudioManager.inst.CurrentAudioSource.time : AudioManager.inst.CurrentAudioSource.time + 0.001f);

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

            beatmapObject.editorData.layer = RTEditor.inst.Layer;

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
            eventKeyframe3.relative = true;
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

                if (inst)
                {
                    var timelineObject = new TimelineObject(beatmapObject);

                    inst.RenderTimelineObject(timelineObject);
                    Updater.UpdateProcessor(beatmapObject);
                    inst.SetCurrentObject(timelineObject);
                }
            }
            return beatmapObject;
        }

        #endregion

        #region Selection

        public IEnumerator GroupSelectObjects(bool _add = true)
        {
            EditorManager.inst.DisplayNotification("Selecting objects, please wait.", 1f, EditorManager.NotificationType.Success);
            var objEditor = ObjEditor.inst;

            if (!_add)
            {
                DeselectAllObjects();
                RenderTimelineObjects();
            }

            var list = RTEditor.inst.timelineObjects;
            list.Where(x => x.Layer == RTEditor.inst.Layer && RTMath.RectTransformToScreenSpace(EditorManager.inst.SelectionBoxImage.rectTransform)
            .Overlaps(RTMath.RectTransformToScreenSpace(x.Image.rectTransform))).ToList().ForEach(delegate (TimelineObject x)
            {
                x.selected = true;
                x.timeOffset = 0f;
                x.binOffset = 0;
            });

            if (SelectedObjectCount > 1)
                EditorManager.inst.ShowDialog("Multi Object Editor", false);

            if (SelectedObjectCount <= 0)
                CheckpointEditor.inst.SetCurrentCheckpoint(0);

            EditorManager.inst.DisplayNotification($"Selection includes {SelectedObjectCount} objects!", 1f, EditorManager.NotificationType.Success);
            yield break;
        }

        public IEnumerator GroupSelectKeyframes(bool _add = true)
        {
            if (!CurrentSelection.IsBeatmapObject)
                yield break;

            var list = CurrentSelection.InternalSelections;

            if (!_add)
                list.ForEach(x => x.selected = false);

            list.Where(x => RTMath.RectTransformToScreenSpace(ObjEditor.inst.SelectionBoxImage.rectTransform)
            .Overlaps(RTMath.RectTransformToScreenSpace(x.Image.rectTransform))).ToList().ForEach(delegate (TimelineObject x)
            {
                x.selected = true;
                x.timeOffset = 0f;
                ObjEditor.inst.currentKeyframeKind = x.Type;
                ObjEditor.inst.currentKeyframe = x.Index;
            });

            var bm = CurrentSelection.GetData<BeatmapObject>();
            RenderKeyframeDialog(bm);
            RenderKeyframes(bm);

            yield break;
        }

        public void DeselectAllObjects()
        {
            foreach (var timelineObject in SelectedObjects)
                timelineObject.selected = false;
        }

        public void AddSelectedObject(TimelineObject timelineObject)
        {
            if (SelectedObjectCount > 1)
            {
                timelineObject.selected = true;
                CurrentSelection = timelineObject;

                EditorManager.inst.ClearDialogs(Array.Empty<EditorManager.EditorDialog.DialogType>());
                EditorManager.inst.ShowDialog("Multi Object Editor", false);
                if (timelineObject.IsBeatmapObject)
                    SetCurrentKeyframe(timelineObject.GetData<BeatmapObject>(), 0, 0, false, false);

                RenderTimelineObject(timelineObject);
                return;
            }

            SetCurrentObject(timelineObject);
        }

        public void SetCurrentObjectP(TimelineObject timelineObject, bool bringTo = false) => SetCurrentObject(timelineObject, bringTo, true);

        public void SetCurrentObject(TimelineObject timelineObject, bool bringTo = false, bool openDialog = true)
        {
            if (!RTEditor.inst.timelineObjects.Has(x => x.ID == timelineObject.ID))
                RenderTimelineObject(timelineObject);

            if (CurrentSelection.IsBeatmapObject && CurrentSelection.ID != timelineObject.ID)
            {
                ClearKeyframes(CurrentSelection.GetData<BeatmapObject>());
            }

            DeselectAllObjects();

            timelineObject.selected = true;
            CurrentSelection = timelineObject;

            if (!string.IsNullOrEmpty(timelineObject.ID) && openDialog)
            {
                if (timelineObject.IsBeatmapObject)
                    OpenDialog(timelineObject.GetData<BeatmapObject>());
                if (timelineObject.IsPrefabObject)
                    PrefabEditor.inst.OpenPrefabDialog();
            }

            if (bringTo)
            {
                AudioManager.inst.SetMusicTime(timelineObject.Time);
                RTEditor.inst.layerType = RTEditor.LayerType.Objects;
                RTEditor.inst.SetLayer(timelineObject.Layer);
            }

            if (timelineObject.IsBeatmapObject)
                timelineObject.GetData<BeatmapObject>().RTObject?.GenerateDraggers();
        }

        public void SetCurrentBeatmapObject(int index) => inst.SetCurrentObject(RTEditor.inst.TimelineBeatmapObjects[index]);

        public void SetCurrentKeyframe(BeatmapObject beatmapObject, int _keyframe, bool _bringTo = false) => SetCurrentKeyframe(beatmapObject, ObjEditor.inst.currentKeyframeKind, _keyframe, _bringTo, false);

        public void AddCurrentKeyframe(BeatmapObject beatmapObject, int _add, bool _bringTo = false)
        {
            SetCurrentKeyframe(beatmapObject,
                ObjEditor.inst.currentKeyframeKind,
                Mathf.Clamp(ObjEditor.inst.currentKeyframe + _add == int.MaxValue ? 1000000 : _add, 0, beatmapObject.events[ObjEditor.inst.currentKeyframeKind].Count - 1),
                _bringTo);
        }

        public void SetCurrentKeyframe(BeatmapObject beatmapObject, int type, int index, bool _bringTo = false, bool _shift = false)
        {
            var bmTimelineObject = GetTimelineObject(beatmapObject);

            if (!ObjEditor.inst.timelineKeyframesDrag)
            {
                Debug.Log($"{ObjEditor.inst.className}Setting Current Keyframe: {type}, {index}");
                if (!_shift && bmTimelineObject.InternalSelections.Count > 0)
                    bmTimelineObject.InternalSelections.ForEach(delegate (TimelineObject x) { x.selected = false; });

                var kf = GetKeyframe(beatmapObject, type, index);

                kf.selected = !_shift || !kf.selected;
            }

            DataManager.inst.UpdateSettingInt("EditorObjKeyframeKind", type);
            DataManager.inst.UpdateSettingInt("EditorObjKeyframe", index);
            ObjEditor.inst.currentKeyframeKind = type;
            ObjEditor.inst.currentKeyframe = index;

            if (_bringTo)
            {
                float value = beatmapObject.events[ObjEditor.inst.currentKeyframeKind][ObjEditor.inst.currentKeyframe].eventTime + beatmapObject.StartTime;

                value = Mathf.Clamp(value, AllowTimeExactlyAtStart ? beatmapObject.StartTime + 0.001f : beatmapObject.StartTime, beatmapObject.StartTime + beatmapObject.GetObjectLifeLength());

                AudioManager.inst.SetMusicTime(Mathf.Clamp(value, 0f, AudioManager.inst.CurrentAudioSource.clip.length));
                AudioManager.inst.CurrentAudioSource.Pause();
                EditorManager.inst.UpdatePlayButton();
            }

            //RenderKeyframes(beatmapObject);
            RenderKeyframeDialog(beatmapObject);
        }

        public int AddEvent(BeatmapObject beatmapObject, float _time, int _kind, EventKeyframe _keyframe, bool openDialog)
        {
            var eventKeyframe = EventKeyframe.DeepCopy(_keyframe);
            eventKeyframe.eventTime = _time;

            if (eventKeyframe.relative)
                for (int i = 0; i < eventKeyframe.eventValues.Length; i++)
                    eventKeyframe.eventValues[i] = 0f;

            beatmapObject.events[_kind].Add(eventKeyframe);

            RenderTimelineObject(new TimelineObject(beatmapObject));
            if (openDialog)
                RenderKeyframeDialog(beatmapObject);
            return beatmapObject.events[_kind].FindIndex(x => x.eventTime == _time);
        }

        #endregion

        #region Timeline Objects

        public void ClearTimelineObjects()
        {
            foreach (var timelineObject in RTEditor.inst.timelineObjects)
                Destroy(timelineObject.GameObject);

            RTEditor.inst.timelineObjects.Clear();
        }

        /// <summary>
        /// Finds the timeline object with the associated BeatmapObject ID.
        /// </summary>
        /// <param name="beatmapObject"></param>
        /// <returns>Returns either the related TimelineObject or a new TimelineObject if one doesn't exist for whatever reason.</returns>
        public TimelineObject GetTimelineObject(BeatmapObject beatmapObject)
        {
            if (!beatmapObject.timelineObject)
                beatmapObject.timelineObject = new TimelineObject(beatmapObject);

            //if (!beatmapObject.timelineObject)
            //{
            //    if (RTEditor.inst.timelineObjects.Has(x => x.IsBeatmapObject && x.ID == beatmapObject.id))
            //        beatmapObject.timelineObject = RTEditor.inst.timelineObjects.Find(x => x.IsBeatmapObject && x.ID == beatmapObject.id);
            //    else
            //        beatmapObject.timelineObject = new TimelineObject(beatmapObject);
            //}

            return beatmapObject.timelineObject;
        }

        public void RenderTimelineObjectVoid(TimelineObject timelineObject) => RenderTimelineObject(timelineObject);

        public GameObject RenderTimelineObject(TimelineObject timelineObject)
        {
            GameObject gameObject = null;

            if (!string.IsNullOrEmpty(timelineObject.ID))
            {
                if (!RTEditor.inst.timelineObjects.Has(x => x.ID == timelineObject.ID))
                    RTEditor.inst.timelineObjects.Add(timelineObject);
                else
                    timelineObject = RTEditor.inst.timelineObjects.Find(x => x.ID == timelineObject.ID);

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

                    var color = ObjEditor.inst.NormalColor;

                    if (timelineObject.IsBeatmapObject)
                    {
                        var beatmapObject = timelineObject.Data as BeatmapObject;
                        beatmapObject.timelineObject = timelineObject;

                        locked = beatmapObject.editorData.locked;
                        collapsed = beatmapObject.editorData.collapse;
                        bin = beatmapObject.editorData.Bin;
                        name = beatmapObject.name;
                        startTime = beatmapObject.StartTime;
                        offset = beatmapObject.GetObjectLifeLength(_takeCollapseIntoConsideration: true);

                        image.type = GetObjectTypePattern(beatmapObject.objectType);
                        image.sprite = GetObjectTypeSprite(beatmapObject.objectType);

                        if (!string.IsNullOrEmpty(beatmapObject.prefabID))
                        {
                            if (DataManager.inst.gameData.prefabs.FindIndex(x => x.ID == beatmapObject.prefabID) != -1)
                                color = DataManager.inst.PrefabTypes[DataManager.inst.gameData.prefabs.Find(x => x.ID == beatmapObject.prefabID).Type].Color;
                            else
                            {
                                beatmapObject.prefabID = null;
                                beatmapObject.prefabInstanceID = null;
                            }
                        }
                    }

                    if (timelineObject.IsPrefabObject)
                    {
                        var prefabObject = timelineObject.Data as BasePrefabObject;
                        var prefab = DataManager.inst.gameData.prefabs.Find(x => x.ID == prefabObject.prefabID);

                        locked = prefabObject.editorData.locked;
                        collapsed = prefabObject.editorData.collapse;
                        bin = prefabObject.editorData.Bin;
                        name = prefab.Name;
                        startTime = prefabObject.StartTime + prefab.Offset;
                        offset = prefab.GetPrefabLifeLength(timelineObject.Data as BasePrefabObject, true);
                        image.type = Image.Type.Simple;
                        image.sprite = null;

                        var prefabType = prefab.Type < DataManager.inst.PrefabTypes.Count ? (PrefabType)DataManager.inst.PrefabTypes[prefab.Type] : PrefabType.InvalidType;

                        color = prefabType.Color;
                        nullName = prefabType.Name;
                    }

                    gameObject.GetComponentInChildren<TextMeshProUGUI>().text = (!string.IsNullOrEmpty(name)) ? string.Format("<mark=#000000aa>{0}</mark>", name) : nullName;

                    if (locked && !gameObject.transform.Find("icons/lock"))
                    {
                        var @lock = ObjEditor.inst.timelineObjectPrefabLock.Duplicate(gameObject.transform.Find("icons"));
                        @lock.name = "lock";
                        ((RectTransform)@lock.transform).anchoredPosition = Vector3.zero;
                    }
                    else if (locked && gameObject.transform.Find("icons/lock"))
                    {
                        var @lock = gameObject.transform.Find("icons/lock").AsRT();
                        @lock.localScale = Vector2.one;
                        @lock.anchoredPosition = Vector2.zero;
                    }
                    else if (!locked && gameObject.transform.Find("icons/lock"))
                        Destroy(gameObject.transform.Find("icons/lock").gameObject);

                    if (collapsed && !gameObject.transform.Find("icons/dots"))
                    {
                        var dots = ObjEditor.inst.timelineObjectPrefabDots.Duplicate(gameObject.transform.Find("icons"));
                        dots.name = "dots";
                        ((RectTransform)dots.transform).anchoredPosition = Vector3.zero;
                    }
                    else if (collapsed && gameObject.transform.Find("icons/dots"))
                    {
                        var dots = gameObject.transform.Find("icons/dots").AsRT();
                        dots.localScale = Vector2.one;
                        dots.anchoredPosition = Vector2.zero;
                    }
                    else if (!collapsed && gameObject.transform.Find("icons/dots"))
                        Destroy(gameObject.transform.Find("icons/dots").gameObject);

                    bool isBeatmapObject = timelineObject.IsBeatmapObject && !string.IsNullOrEmpty(timelineObject.GetData<BeatmapObject>().prefabID) && DataManager.inst.gameData.prefabs.Has(x => x.ID == timelineObject.GetData<BeatmapObject>().prefabID);
                    bool isPrefab = timelineObject.IsPrefabObject && timelineObject.GetData<PrefabObject>().GetPrefab() != null;

                    if (RTEditor.GetEditorProperty("Timeline Object Prefab Type Icon").GetConfigEntry<bool>().Value && (isBeatmapObject || isPrefab) && !gameObject.transform.Find("icons/type"))
                    {
                        if (!gameObject.transform.Find("icons").GetComponent<HorizontalLayoutGroup>())
                        {
                            var hlg = gameObject.transform.Find("icons").gameObject.AddComponent<HorizontalLayoutGroup>();
                            hlg.childControlWidth = false;
                            hlg.childForceExpandWidth = false;
                            hlg.spacing = -4f;
                            hlg.childAlignment = TextAnchor.UpperRight;

                            if (gameObject.transform.Find("icons/lock"))
                            {
                                gameObject.transform.Find("icons/lock").AsRT().sizeDelta = new Vector2(20f, 20f);
                            }

                            if (gameObject.transform.Find("icons/dots"))
                            {
                                gameObject.transform.Find("icons/dots").AsRT().sizeDelta = new Vector2(32f, 20f);
                            }
                        }

                        var b = new GameObject("type");
                        b.transform.SetParent(gameObject.transform.Find("icons"));
                        b.transform.localScale = Vector3.one;

                        var bRT = b.AddComponent<RectTransform>();
                        bRT.sizeDelta = new Vector2(20f, 20f);

                        var bImage = b.AddComponent<Image>();
                        bImage.color = new Color(0f, 0f, 0f, 0.45f);

                        var icon = new GameObject("type");
                        icon.transform.SetParent(b.transform);
                        icon.transform.localScale = Vector3.one;

                        var iconRT = icon.AddComponent<RectTransform>();
                        iconRT.anchoredPosition = Vector2.zero;
                        //iconRT.anchoredPosition = new Vector2(-115f, 0f);
                        iconRT.sizeDelta = new Vector2(20f, 20f);

                        var iconImage = icon.AddComponent<Image>();
                        int i =
                            isBeatmapObject ? DataManager.inst.gameData.prefabs.Find(x => x.ID == timelineObject.GetData<BeatmapObject>().prefabID).Type :
                            isPrefab ? timelineObject.GetData<PrefabObject>().GetPrefab().Type : 0;

                        iconImage.sprite = ((PrefabType)DataManager.inst.PrefabTypes[i]).Icon;
                    }

                    float zoom = EditorManager.inst.Zoom;

                    offset = offset <= 0.4f ? 0.4f * zoom : offset * zoom;

                    var rectTransform = (RectTransform)gameObject.transform;
                    rectTransform.sizeDelta = new Vector2(offset, 20f);
                    rectTransform.anchoredPosition = new Vector2(startTime * EditorManager.inst.Zoom, (-20 * Mathf.Clamp(bin, 0, 14)));

                    gameObject.GetComponentInChildren<TextMeshProUGUI>().color = LSColors.white;
                    gameObject.SetActive(true);
                }
            }

            return gameObject;
        }

        public void RenderTimelineObjects()
        {
            foreach (var timelineObject in RTEditor.inst.timelineObjects.FindAll(x => !x.IsEventKeyframe))
                RenderTimelineObject(timelineObject);
        }

        public GameObject CreateTimelineObject(TimelineObject timelineObject)
        {
            GameObject gameObject = null;

            if (!string.IsNullOrEmpty(timelineObject.ID))
            {
                if (!RTEditor.inst.timelineObjects.Has(x => x.ID == timelineObject.ID))
                    RTEditor.inst.timelineObjects.Add(timelineObject);
                else
                    timelineObject = RTEditor.inst.timelineObjects.Find(x => x.ID == timelineObject.ID);

                DestroyImmediate(timelineObject.GameObject);

                gameObject = ObjEditor.inst.timelineObjectPrefab.Duplicate(EditorManager.inst.timeline.transform,
                    timelineObject.IsBeatmapObject ? timelineObject.GetData<BeatmapObject>().name :
                    timelineObject.IsPrefabObject && DataManager.inst.gameData.prefabs.Find(x => timelineObject.GetData<PrefabObject>().prefabID == x.ID) != null ?
                    DataManager.inst.gameData.prefabs.Find(x => (timelineObject.Data as BasePrefabObject).prefabID == x.ID).Name : "<color=#FF0000>Null Reference Exception");

                //bool isBeatmapObject = timelineObject.IsBeatmapObject && !string.IsNullOrEmpty(timelineObject.GetData<BeatmapObject>().prefabID) && DataManager.inst.gameData.prefabs.Has(x => x.ID == timelineObject.GetData<BeatmapObject>().prefabID);
                //bool isPrefab = timelineObject.IsPrefabObject && timelineObject.GetData<PrefabObject>().GetPrefab() != null;

                //if (RTEditor.GetEditorProperty("Timeline Object Prefab Type Icon").GetConfigEntry<bool>().Value && (isBeatmapObject || isPrefab))
                //{
                //    var icon = new GameObject("Icon");
                //    icon.transform.SetParent(gameObject.transform);
                //    icon.transform.localScale = Vector3.one;

                //    var iconRT = icon.AddComponent<RectTransform>();
                //    iconRT.anchoredPosition = new Vector2(-115f, 0f);
                //    iconRT.sizeDelta = new Vector2(18f, 18f);

                //    var iconImage = icon.AddComponent<Image>();
                //    int i =
                //        isBeatmapObject ? DataManager.inst.gameData.prefabs.Find(x => x.ID == timelineObject.GetData<BeatmapObject>().prefabID).Type :
                //        isPrefab ? timelineObject.GetData<PrefabObject>().GetPrefab().Type : 0;

                //    iconImage.sprite = ((PrefabType)DataManager.inst.PrefabTypes[i]).Icon;
                //}

                var hoverUI = gameObject.AddComponent<HoverUI>();
                hoverUI.animatePos = false;
                hoverUI.animateSca = true;
                hoverUI.size = RTEditor.GetEditorProperty("Timeline Object Hover Size").GetConfigEntry<float>().Value;

                timelineObject.GameObject = gameObject;
                timelineObject.Image = gameObject.GetComponent<Image>();

                TriggerHelper.AddEventTrigger(gameObject, new List<EventTrigger.Entry>
                {
                    TriggerHelper.CreateBeatmapObjectTrigger(timelineObject),
                    TriggerHelper.CreateBeatmapObjectStartDragTrigger(timelineObject),
                    TriggerHelper.CreateBeatmapObjectEndDragTrigger(timelineObject)
                });
            }

            return gameObject;
        }

        public void CreateTimelineObjects()
        {
            if (RTEditor.inst.timelineObjects.Count > 0)
                RTEditor.inst.timelineObjects.ForEach(x => Destroy(x.GameObject));

            RTEditor.inst.timelineObjects.Clear();

            foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
            {
                if (!string.IsNullOrEmpty(beatmapObject.id) && !beatmapObject.fromPrefab)
                {
                    CreateTimelineObject(GetTimelineObject((BeatmapObject)beatmapObject));
                }
            }

            foreach (var prefabObject in DataManager.inst.gameData.prefabObjects)
            {
                if (!string.IsNullOrEmpty(prefabObject.ID))
                {
                    var timelineObject = new TimelineObject(prefabObject);
                    CreateTimelineObject(timelineObject);
                }
            }
        }

        public Sprite GetObjectTypeSprite(ObjectType objectType)
            => objectType == ObjectType.Helper ? ObjEditor.inst.HelperSprite :
            objectType == ObjectType.Decoration ? ObjEditor.inst.DecorationSprite :
            objectType == ObjectType.Empty ? ObjEditor.inst.EmptySprite : null;

        public Image.Type GetObjectTypePattern(ObjectType objectType)
            => objectType == ObjectType.Helper || objectType == ObjectType.Decoration || objectType == ObjectType.Empty ? Image.Type.Tiled : Image.Type.Simple;

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

                    if (objectUIElements == null)
                        objectUIElements = new Dictionary<string, object>();
                    objectUIElements.Clear();

                    objectUIElements.Add("ID Text", tfv.Find("id/text").GetComponent<Text>());
                    objectUIElements.Add("LDM Toggle", tfv.Find("id/ldm/toggle").GetComponent<Toggle>());

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
                    objectUIElements.Add("Autokill TOD IF", tfv.Find("autokill/tod-value").GetComponent<InputField>());
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
                    objectUIElements.Add("Origin Y IF", tfv.Find("origin/y").GetComponent<InputField>());

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
                inst.CurrentSelection = inst.GetTimelineObject(beatmapObject);
                inst.CurrentSelection.selected = true;

                inst.RenderID(beatmapObject);
                inst.RenderName(beatmapObject);
                inst.RenderObjectType(beatmapObject);

                inst.RenderStartTime(beatmapObject);
                inst.RenderAutokill(beatmapObject);

                inst.RenderParent(beatmapObject);

                inst.RenderOrigin(beatmapObject);
                inst.RenderShape(beatmapObject);
                inst.RenderDepth(beatmapObject);

                inst.RenderLayers(beatmapObject);
                inst.RenderBin(beatmapObject);

                inst.RenderGameObjectInspector(beatmapObject);

                bool fromPrefab = !string.IsNullOrEmpty(beatmapObject.prefabID);
                ((GameObject)inst.ObjectUIElements["Collapse Label"]).SetActive(fromPrefab);
                ((GameObject)inst.ObjectUIElements["Collapse Prefab"]).SetActive(fromPrefab);

                inst.RenderKeyframes(beatmapObject);
                inst.RenderKeyframeDialog(beatmapObject);

                if (ObjectModifiersEditor.inst)
                    inst.StartCoroutine(ObjectModifiersEditor.inst.RenderModifiers(beatmapObject));
            }

            yield break;
        }

        /// <summary>
        /// Renders the ID Text.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        public void RenderID(BeatmapObject beatmapObject)
        {
            var idText = (Text)ObjectUIElements["ID Text"];
            idText.text = $"ID: {beatmapObject.id}";

            var clickable = idText.transform.parent.gameObject.GetComponent<Clickable>();

            if (!clickable)
                clickable = idText.transform.parent.gameObject.AddComponent<Clickable>();

            clickable.onClick = delegate (PointerEventData pointerEventData)
            {
                Debug.Log($"{ObjEditor.inst.className}Copied ID from {beatmapObject}!");
                EditorManager.inst.DisplayNotification($"Copied ID from {beatmapObject.name}!", 2f, EditorManager.NotificationType.Success);
                LSText.CopyToClipboard(beatmapObject.id);
            };

            var ldmToggle = (Toggle)ObjectUIElements["LDM Toggle"];
            ldmToggle.onValueChanged.ClearAll();
            ldmToggle.isOn = beatmapObject.LDM;
            ldmToggle.onValueChanged.AddListener(delegate (bool _val)
            {
                beatmapObject.LDM = _val;
                Updater.UpdateProcessor(beatmapObject);
            });
        }

        /// <summary>
        /// Renders the Name InputField.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        public void RenderName(BeatmapObject beatmapObject)
        {
            var name = (InputField)ObjectUIElements["Name IF"];

            // Allows for left / right flipping.
            if (!name.GetComponent<InputFieldSwapper>() && name.gameObject)
            {
                var t = name.gameObject.AddComponent<InputFieldSwapper>();
                t.Init(name, InputFieldSwapper.Type.String);
            }

            name.onValueChanged.ClearAll();
            name.text = beatmapObject.name;
            name.onValueChanged.AddListener(delegate (string _val)
            {
                beatmapObject.name = _val;

                // Since name has no effect on the physical object, we will only need to update the timeline object.
                RenderTimelineObject(new TimelineObject(beatmapObject));
            });
        }

        bool setTypes;
        /// <summary>
        /// Renders the ObjectType Dropdown.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        public void RenderObjectType(BeatmapObject beatmapObject)
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
                RenderTimelineObject(new TimelineObject(beatmapObject));
                if (UpdateObjects)
                    Updater.UpdateProcessor(beatmapObject);
            });
        }

        /// <summary>
        /// Renders all StartTime UI.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        public void RenderStartTime(BeatmapObject beatmapObject)
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
                RenderTimelineObject(new TimelineObject(beatmapObject));
            });

            timeIF.onValueChanged.ClearAll();
            timeIF.text = beatmapObject.StartTime.ToString();
            timeIF.onValueChanged.AddListener(delegate (string _val)
            {
                if (float.TryParse(_val, out float num))
                {
                    beatmapObject.StartTime = Mathf.Clamp(num, 0f, AudioManager.inst.CurrentAudioSource.clip.length);

                    ResizeKeyframeTimeline(beatmapObject);

                    // StartTime affects both physical object and timeline object.
                    RenderTimelineObject(new TimelineObject(beatmapObject));
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

                ResizeKeyframeTimeline(beatmapObject);

                // StartTime affects both physical object and timeline object.
                RenderTimelineObject(new TimelineObject(beatmapObject));
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

                ResizeKeyframeTimeline(beatmapObject);

                // StartTime affects both physical object and timeline object.
                RenderTimelineObject(new TimelineObject(beatmapObject));
                if (UpdateObjects)
                    Updater.UpdateProcessor(beatmapObject, "StartTime");

                ResizeKeyframeTimeline(beatmapObject);
            });

            setStartToTime.onClick.RemoveAllListeners();
            setStartToTime.onClick.AddListener(delegate ()
            {
                timeIF.text = EditorManager.inst.CurrentAudioPos.ToString();

                ResizeKeyframeTimeline(beatmapObject);

                // StartTime affects both physical object and timeline object.
                RenderTimelineObject(new TimelineObject(beatmapObject));
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

                ResizeKeyframeTimeline(beatmapObject);

                // StartTime affects both physical object and timeline object.
                RenderTimelineObject(new TimelineObject(beatmapObject));
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

                ResizeKeyframeTimeline(beatmapObject);

                // StartTime affects both physical object and timeline object.
                RenderTimelineObject(new TimelineObject(beatmapObject));
                if (UpdateObjects)
                    Updater.UpdateProcessor(beatmapObject, "StartTime");

                ResizeKeyframeTimeline(beatmapObject);
            });
        }

        /// <summary>
        /// Renders all Autokill UI.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        public void RenderAutokill(BeatmapObject beatmapObject)
        {
            var akType = (Dropdown)ObjectUIElements["Autokill TOD DD"];
            akType.onValueChanged.ClearAll();
            akType.value = (int)beatmapObject.autoKillType;
            akType.onValueChanged.AddListener(delegate (int _val)
            {
                beatmapObject.autoKillType = (AutoKillType)_val;
                // AutoKillType affects both physical object and timeline object.
                RenderTimelineObject(new TimelineObject(beatmapObject));
                if (UpdateObjects)
                    Updater.UpdateProcessor(beatmapObject, "Autokill");
                RenderAutokill(beatmapObject);

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
                    RenderTimelineObject(new TimelineObject(beatmapObject));
                    if (UpdateObjects)
                        Updater.UpdateProcessor(beatmapObject, "Autokill");
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
                    RenderTimelineObject(new TimelineObject(beatmapObject));
                    if (UpdateObjects)
                        Updater.UpdateProcessor(beatmapObject, "Autokill");
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
                RenderTimelineObject(new TimelineObject(beatmapObject));
            });
        }

        /// <summary>
        /// Renders all Parent UI.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        public void RenderParent(BeatmapObject beatmapObject)
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
                    RTEditor.inst.timelineObjects.TryFind(x => x.ID == parent, out TimelineObject timelineObject))
                        SetCurrentObject(timelineObject);
                    else if (parent == "CAMERA_PARENT")
                    {
                        RTEditor.inst.SetLayer(RTEditor.LayerType.Events);
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
                    var _p = (Transform)ObjectUIElements[$"Parent Offset {i + 1}"];

                    var parentOffset = beatmapObject.getParentOffset(i);

                    var index = i;

                    var tog = _p.GetChild(2).GetComponent<Toggle>();
                    tog.onValueChanged.RemoveAllListeners();
                    tog.isOn = beatmapObject.GetParentType(i);
                    tog.onValueChanged.AddListener(delegate (bool _value)
                    {
                        beatmapObject.SetParentType(index, _value);

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
                            beatmapObject.SetParentOffset(index, num);

                            // Since updating parent type has no affect on the timeline object, we will only need to update the physical object.
                            if (UpdateObjects)
                                Updater.UpdateProcessor(beatmapObject, "Parent Offset");
                        }
                    });

                    TriggerHelper.AddEventTrigger(pif.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(pif) });

                    //if (_p.transform.Find("<<"))
                    //    Destroy(_p.transform.Find("<<"));

                    //if (_p.transform.Find("<"))
                    //    Destroy(_p.transform.Find("<"));

                    //if (_p.transform.Find(">"))
                    //    Destroy(_p.transform.Find(">"));

                    //if (_p.transform.Find(">>"))
                    //    Destroy(_p.transform.Find(">>"));

                    //TriggerHelper.IncreaseDecreaseButtons(pif, t: _p.transform);

                    var additive = _p.GetChild(4).GetComponent<Toggle>();
                    additive.onValueChanged.ClearAll();
                    additive.isOn = beatmapObject.parentAdditive[i] == '1';
                    additive.onValueChanged.AddListener(delegate (bool _val)
                    {
                        beatmapObject.SetParentAdditive(index, _val);
                        if (UpdateObjects)
                            Updater.UpdateProcessor(beatmapObject);
                    });

                    var parallax = _p.GetChild(5).GetComponent<InputField>();
                    parallax.onValueChanged.RemoveAllListeners();
                    parallax.text = beatmapObject.parallaxSettings[index].ToString();
                    parallax.onValueChanged.AddListener(delegate (string _value)
                    {
                        if (float.TryParse(_value, out float num))
                        {
                            beatmapObject.parallaxSettings[index] = num;

                            // Since updating parent type has no affect on the timeline object, we will only need to update the physical object.
                            if (UpdateObjects)
                                Updater.UpdateProcessor(beatmapObject);
                        }
                    });

                    TriggerHelper.AddEventTrigger(parallax.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(parallax) });
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
        public void RenderOrigin(BeatmapObject beatmapObject)
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

        public void LastGameObject(Transform parent)
        {
            var gameObject = new GameObject("GameObject");
            gameObject.transform.SetParent(parent);
            gameObject.transform.localScale = Vector3.one;

            var rectTransform = gameObject.AddComponent<RectTransform>();

            rectTransform.anchorMax = new Vector2(0f, 0f);
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(0f, 32f);

            var layoutElement = gameObject.AddComponent<LayoutElement>();
            layoutElement.layoutPriority = 1;
            layoutElement.preferredWidth = 1000f;
        }

        /// <summary>
        /// Renders the Shape ToggleGroup.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        public void RenderShape(BeatmapObject beatmapObject)
        {
            var shape = (Transform)ObjectUIElements["Shape"];
            var shapeSettings = (Transform)ObjectUIElements["Shape Settings"];

            shape.GetComponent<GridLayoutGroup>().spacing = new Vector2(7.6f, 0f);

            // Initial removing
            //Debug.Log($"{ObjEditor.inst.className}Adding shapes to list for removal...");
            DestroyImmediate(shape.GetComponent<ToggleGroup>());
            //var toggleGroup = shape.GetComponent<ToggleGroup>();
            //var toggles = (List<Toggle>)toggleGroup.GetType().GetField("m_Toggles", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(toggleGroup);
            //toggles.Clear();

            //ShapeUI.SetupSprites();

            var toDestroy = new List<GameObject>();

            for (int i = 0; i < shape.childCount; i++)
            {
                toDestroy.Add(shape.GetChild(i).gameObject);
            }

            //Debug.Log($"{ObjEditor.inst.className}Adding shape settings to list for removal...");
            for (int i = 0; i < shapeSettings.childCount; i++)
            {
                if (i != 4 && i != 6)
                    for (int j = 0; j < shapeSettings.GetChild(i).childCount; j++)
                    {
                        toDestroy.Add(shapeSettings.GetChild(i).GetChild(j).gameObject);
                    }
            }

            //Debug.Log($"{ObjEditor.inst.className}Removing all...");
            foreach (var obj in toDestroy)
                DestroyImmediate(obj);

            toDestroy = null;

            // Re-add everything
            for (int i = 0; i < ShapeManager.inst.Shapes2D.Count; i++)
            {
                var obj = shapeButtonPrefab.Duplicate(shape, (i + 1).ToString(), i);
                if (obj.transform.Find("Image") && obj.transform.Find("Image").gameObject.TryGetComponent(out Image image))
                    image.sprite = ShapeManager.inst.Shapes2D[i][0].Icon;

                if (i != 4 && i != 6)
                {
                    if (!shapeSettings.Find((i + 1).ToString()))
                    {
                        shapeSettings.Find("6").gameObject.Duplicate(shapeSettings, (i + 1).ToString());
                    }

                    var so = shapeSettings.Find((i + 1).ToString());

                    var rect = (RectTransform)so;
                    if (!so.GetComponent<ScrollRect>())
                    {
                        var scroll = so.gameObject.AddComponent<ScrollRect>();
                        so.gameObject.AddComponent<Mask>();
                        var ad = so.gameObject.AddComponent<Image>();

                        scroll.horizontal = true;
                        scroll.vertical = false;
                        scroll.content = rect;
                        scroll.viewport = rect;
                        ad.color = new Color(1f, 1f, 1f, 0.01f);
                    }

                    for (int j = 0; j < ShapeManager.inst.Shapes2D[i].Count; j++)
                    {
                        var opt = shapeButtonPrefab.Duplicate(shapeSettings.GetChild(i), (j + 1).ToString(), j);
                        if (opt.transform.Find("Image") && opt.transform.Find("Image").gameObject.TryGetComponent(out Image image1))
                            image1.sprite = ShapeManager.inst.Shapes2D[i][j].Icon;

                        var layoutElement = opt.AddComponent<LayoutElement>();
                        layoutElement.layoutPriority = 1;
                        layoutElement.minWidth = 32f;

                        ((RectTransform)opt.transform).sizeDelta = new Vector2(32f, 32f);

                        if (!opt.GetComponent<HoverUI>())
                        {
                            var he = opt.AddComponent<HoverUI>();
                            he.animatePos = false;
                            he.animateSca = true;
                            he.size = 1.1f;
                        }
                    }

                    LastGameObject(shapeSettings.GetChild(i));
                }
            }

            LSHelpers.SetActiveChildren(shapeSettings, false);

            if (beatmapObject.shape >= shapeSettings.childCount)
            {
                Debug.Log($"{ObjEditor.inst.className}Somehow, the object ended up being at a higher shape than normal.");
                beatmapObject.shape = shapeSettings.childCount - 1;
                // Since shape has no affect on the timeline object, we will only need to update the physical object.
                if (UpdateObjects)
                    Updater.UpdateProcessor(beatmapObject, "Shape");

                RenderShape(beatmapObject);
            }

            if (beatmapObject.shape == 4)
            {
                Debug.Log($"{ObjEditor.inst.className}Shape is text, so we make the size larger for better readability.");
                shapeSettings.GetComponent<RectTransform>().sizeDelta = new Vector2(351f, 74f);
                var child = shapeSettings.GetChild(4);
                child.GetComponent<RectTransform>().sizeDelta = new Vector2(351f, 74f);
                child.Find("Text").GetComponent<Text>().alignment = TextAnchor.UpperLeft;
                child.Find("Placeholder").GetComponent<Text>().alignment = TextAnchor.UpperLeft;
                child.GetComponent<InputField>().lineType = InputField.LineType.MultiLineNewline;
            }
            else
            {
                Debug.Log($"{ObjEditor.inst.className}Shape is not text so we reset size.");
                shapeSettings.GetComponent<RectTransform>().sizeDelta = new Vector2(351f, 32f);
                shapeSettings.GetChild(4).GetComponent<RectTransform>().sizeDelta = new Vector2(351f, 32f);
            }

            Debug.Log($"{ObjEditor.inst.className}Set the shape option as active.");
            shapeSettings.GetChild(beatmapObject.shape).gameObject.SetActive(true);
            for (int i = 1; i <= ObjectManager.inst.objectPrefabs.Count; i++)
            {
                int buttonTmp = i - 1;

                if (shape.Find(i.ToString()))
                {
                    var shoggle = shape.Find(i.ToString()).GetComponent<Toggle>();
                    shoggle.onValueChanged.ClearAll();
                    shoggle.isOn = beatmapObject.shape == buttonTmp;
                    shoggle.onValueChanged.AddListener(delegate (bool _value)
                    {
                        if (_value)
                        {
                            beatmapObject.shape = buttonTmp;
                            beatmapObject.shapeOption = 0;

                            // Since shape has no affect on the timeline object, we will only need to update the physical object.
                            if (UpdateObjects)
                                Updater.UpdateProcessor(beatmapObject, "Shape");

                            RenderShape(beatmapObject);
                        }
                    });

                    if (!shape.Find(i.ToString()).GetComponent<HoverUI>())
                    {
                        var hoverUI = shape.Find(i.ToString()).gameObject.AddComponent<HoverUI>();
                        hoverUI.animatePos = false;
                        hoverUI.animateSca = true;
                        hoverUI.size = 1.1f;
                    }
                }
            }

            if (beatmapObject.shape != 4 && beatmapObject.shape != 6)
            {
                for (int i = 0; i < shapeSettings.GetChild(beatmapObject.shape).childCount - 1; i++)
                {
                    int buttonTmp = i;
                    var shoggle = shapeSettings.GetChild(beatmapObject.shape).GetChild(i).GetComponent<Toggle>();

                    shoggle.onValueChanged.RemoveAllListeners();
                    shoggle.isOn = beatmapObject.shapeOption == i;
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

        public void SetDepthSlider(BeatmapObject beatmapObject, float _value, InputField inputField, Slider slider)
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

        public void SetDepthInputField(BeatmapObject beatmapObject, string _value, InputField inputField, Slider slider)
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
        public void RenderDepth(BeatmapObject beatmapObject)
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
        public void RenderGameObjectInspector(BeatmapObject beatmapObject)
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
                    label.transform.GetChild(0).GetComponent<Text>().text = "Unity Explorer";

                    var inspect = tfv.Find("applyprefab").gameObject.Duplicate(tfv);
                    inspect.SetActive(true);
                    inspect.transform.SetSiblingIndex(index + 1);
                    inspect.name = "inspectbeatmapobject";

                    inspect.transform.GetChild(0).GetComponent<Text>().text = "Inspect BeatmapObject";

                    var inspectGameObject = tfv.Find("applyprefab").gameObject.Duplicate(tfv);
                    inspectGameObject.SetActive(true);
                    inspectGameObject.transform.SetSiblingIndex(index + 2);
                    inspectGameObject.name = "inspect";

                    inspectGameObject.transform.GetChild(0).GetComponent<Text>().text = "Inspect GameObject";
                }
                
                if (tfv.Find("inspect"))
                {
                    bool active = beatmapObject.objectType != ObjectType.Empty || !RTFunctions.FunctionsPlugin.LDM.Value || beatmapObject.LDM;
                    tfv.Find("inspect").gameObject.SetActive(active);
                    if (active)
                    {
                        var deleteButton = tfv.Find("inspect").GetComponent<Button>();
                        deleteButton.onClick.ClearAll();
                        deleteButton.onClick.AddListener(delegate ()
                        {
                            if (Updater.TryGetObject(beatmapObject, out RTFunctions.Functions.Optimization.Objects.LevelObject levelObject))
                                inspector.GetMethod("Inspect", new[] { typeof(object), AccessTools.TypeByName("UnityExplorer.CacheObject.CacheObjectBase") }).Invoke(inspector, new object[] { levelObject, null });
                        });
                    }
                }

                if (tfv.Find("inspectbeatmapobject"))
                {
                    var deleteButton = tfv.Find("inspectbeatmapobject").GetComponent<Button>();
                    deleteButton.onClick.ClearAll();
                    deleteButton.onClick.AddListener(delegate ()
                    {
                        inspector.GetMethod("Inspect", new[] { typeof(object), AccessTools.TypeByName("UnityExplorer.CacheObject.CacheObjectBase") }).Invoke(inspector, new object[] { beatmapObject, null });
                    });
                }
            }
        }

        /// <summary>
        /// Renders the Layers InputField.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        public void RenderLayers(BeatmapObject beatmapObject)
        {
            var editorLayersIF = (InputField)ObjectUIElements["Layers IF"];
            var editorLayersImage = (Image)ObjectUIElements["Layers Image"];

            editorLayersIF.onValueChanged.RemoveAllListeners();
            editorLayersIF.text = (beatmapObject.editorData.layer + 1).ToString();
            editorLayersImage.color = RTEditor.GetLayerColor(beatmapObject.editorData.layer);
            editorLayersIF.onValueChanged.AddListener(delegate (string _value)
            {
                if (int.TryParse(_value, out int num))
                {
                    num = Mathf.Clamp(num - 1, 0, int.MaxValue);
                    beatmapObject.editorData.layer = num;

                    // Since layers have no effect on the physical object, we will only need to update the timeline object.
                    RenderTimelineObject(new TimelineObject(beatmapObject));

                    //editorLayersImage.color = RTEditor.GetLayerColor(beatmapObject.editorData.Layer);
                    RenderLayers(beatmapObject);
                }
            });

            if (editorLayersIF.gameObject)
                TriggerHelper.AddEventTrigger(editorLayersIF.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDeltaInt(editorLayersIF, 1, 1, int.MaxValue) });
        }

        /// <summary>
        /// Renders the Bin Slider.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        public void RenderBin(BeatmapObject beatmapObject)
        {
            var editorBin = (Slider)ObjectUIElements["Bin Slider"];
            editorBin.onValueChanged.RemoveAllListeners();
            editorBin.value = beatmapObject.editorData.Bin;
            editorBin.onValueChanged.AddListener(delegate (float _value)
            {
                beatmapObject.editorData.Bin = (int)Mathf.Clamp(_value, 0f, 14f);

                // Since bin has no effect on the physical object, we will only need to update the timeline object.
                RenderTimelineObject(new TimelineObject(beatmapObject));
            });
        }

        public void RenderKeyframeDialog(BeatmapObject beatmapObject)
        {
            if (beatmapObject.timelineObject.InternalSelections.Where(x => x.selected).ToList().Count == 1)
            {
                for (int i = 0; i < ObjEditor.inst.KeyframeDialogs.Count; i++)
                {
                    ObjEditor.inst.KeyframeDialogs[i].SetActive(i == ObjEditor.inst.currentKeyframeKind);
                }

                //Keyframes
                {
                    var kf = beatmapObject.timelineObject.InternalSelections.Where(x => x.selected).ToList()[0];

                    var kfdialog = ObjEditor.inst.KeyframeDialogs[ObjEditor.inst.currentKeyframeKind].transform;
                    if (beatmapObject.events[ObjEditor.inst.currentKeyframeKind].Count > 0)
                        StartCoroutine(IRenderKeyframeDialog(kfdialog, ObjEditor.inst.currentKeyframeKind, beatmapObject));

                    var timeDecreaseGreat = kfdialog.Find("time/<<").GetComponent<Button>();
                    var timeDecrease = kfdialog.Find("time/<").GetComponent<Button>();
                    var timeIncrease = kfdialog.Find("time/>").GetComponent<Button>();
                    var timeIncreaseGreat = kfdialog.Find("time/>>").GetComponent<Button>();
                    var timeSet = kfdialog.Find("time/time").GetComponent<InputField>();

                    if (kf.Index == 0)
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
                    superLeft.interactable = kf.Index != 0;
                    superLeft.onClick.AddListener(delegate ()
                    {
                        SetCurrentKeyframe(beatmapObject, 0, true);
                    });

                    var left = kfdialog.Find("edit/<").GetComponent<Button>();

                    left.onClick.RemoveAllListeners();
                    left.interactable = ObjEditor.inst.currentKeyframe != 0;
                    left.onClick.AddListener(delegate ()
                    {
                        AddCurrentKeyframe(beatmapObject, -1, true);
                    });

                    string text = kf.Index.ToString();
                    if (kf.Index == 0)
                    {
                        text = "S";
                    }
                    else if (kf.Index == beatmapObject.events[kf.Type].Count - 1)
                    {
                        text = "E";
                    }

                    kfdialog.Find("edit/|").GetComponentInChildren<Text>().text = text;

                    var right = kfdialog.Find("edit/>").GetComponent<Button>();

                    right.onClick.RemoveAllListeners();
                    right.interactable = kf.Index < beatmapObject.events[kf.Type].Count - 1;
                    right.onClick.AddListener(delegate ()
                    {
                        AddCurrentKeyframe(beatmapObject, 1, true);
                    });

                    var superRight = kfdialog.Find("edit/>>").GetComponent<Button>();

                    superRight.onClick.RemoveAllListeners();
                    superRight.interactable = kf.Index < beatmapObject.events[kf.Type].Count - 1;
                    superRight.onClick.AddListener(delegate ()
                    {
                        AddCurrentKeyframe(beatmapObject, int.MaxValue, true);
                    });

                    var copy = kfdialog.Find("edit/copy").GetComponent<Button>();
                    copy.onClick.ClearAll();
                    copy.onClick.AddListener(delegate ()
                    {
                        switch (kf.Type)
                        {
                            case 0:
                                CopiedPositionData = EventKeyframe.DeepCopy((EventKeyframe)beatmapObject.events[kf.Type][kf.Index]);
                                break;
                            case 1:
                                CopiedScaleData = EventKeyframe.DeepCopy((EventKeyframe)beatmapObject.events[kf.Type][kf.Index]);
                                break;
                            case 2:
                                CopiedRotationData = EventKeyframe.DeepCopy((EventKeyframe)beatmapObject.events[kf.Type][kf.Index]);
                                break;
                            case 3:
                                CopiedColorData = EventKeyframe.DeepCopy((EventKeyframe)beatmapObject.events[kf.Type][kf.Index]);
                                break;
                        }
                        EditorManager.inst.DisplayNotification("Copied keyframe data!", 2f, EditorManager.NotificationType.Success);
                    });
                    
                    var paste = kfdialog.Find("edit/paste").GetComponent<Button>();
                    paste.onClick.ClearAll();
                    paste.onClick.AddListener(delegate ()
                    {
                        var eventKeyframe = beatmapObject.events[ObjEditor.inst.currentKeyframeKind][ObjEditor.inst.currentKeyframe];
                        var time = eventKeyframe.eventTime;

                        switch (kf.Type)
                        {
                            case 0:
                                if (CopiedPositionData != null)
                                {
                                    ClearKeyframes(beatmapObject);
                                    beatmapObject.events[kf.Type][kf.Index].eventValues = CopiedPositionData.eventValues.Copy();
                                    beatmapObject.events[kf.Type][kf.Index].eventRandomValues = CopiedPositionData.eventRandomValues.Copy();
                                    beatmapObject.events[kf.Type][kf.Index].random = CopiedPositionData.random;
                                    ((EventKeyframe)beatmapObject.events[kf.Type][kf.Index]).relative = CopiedPositionData.relative;
                                    //beatmapObject.events[kf.Type][kf.Index] = EventKeyframe.DeepCopy(CopiedPositionData);
                                    //beatmapObject.events[kf.Type][kf.Index].eventTime = time;
                                    RenderKeyframes(beatmapObject);
                                    RenderKeyframeDialog(beatmapObject);
                                    Updater.UpdateProcessor(beatmapObject, "Keyframes");
                                    EditorManager.inst.DisplayNotification("Pasted position keyframe data to current selected keyframe!", 2f, EditorManager.NotificationType.Success);
                                }
                                else
                                    EditorManager.inst.DisplayNotification("Position keyframe data not copied yet!", 2f, EditorManager.NotificationType.Error);
                                break;
                            case 1:
                                if (CopiedScaleData != null)
                                {
                                    ClearKeyframes(beatmapObject);
                                    beatmapObject.events[kf.Type][kf.Index].eventValues = CopiedScaleData.eventValues.Copy();
                                    beatmapObject.events[kf.Type][kf.Index].eventRandomValues = CopiedScaleData.eventRandomValues.Copy();
                                    beatmapObject.events[kf.Type][kf.Index].random = CopiedScaleData.random;
                                    ((EventKeyframe)beatmapObject.events[kf.Type][kf.Index]).relative = CopiedScaleData.relative;
                                    //beatmapObject.events[kf.Type][kf.Index] = EventKeyframe.DeepCopy(CopiedScaleData);
                                    //beatmapObject.events[kf.Type][kf.Index].eventTime = time;
                                    RenderKeyframes(beatmapObject);
                                    RenderKeyframeDialog(beatmapObject);
                                    Updater.UpdateProcessor(beatmapObject, "Keyframes");
                                    EditorManager.inst.DisplayNotification("Pasted scale keyframe data to current selected keyframe!", 2f, EditorManager.NotificationType.Success);
                                }
                                else
                                    EditorManager.inst.DisplayNotification("Scale keyframe data not copied yet!", 2f, EditorManager.NotificationType.Error);
                                break;
                            case 2:
                                if (CopiedRotationData != null)
                                {
                                    ClearKeyframes(beatmapObject);
                                    beatmapObject.events[kf.Type][kf.Index].eventValues = CopiedRotationData.eventValues.Copy();
                                    beatmapObject.events[kf.Type][kf.Index].eventRandomValues = CopiedRotationData.eventRandomValues.Copy();
                                    beatmapObject.events[kf.Type][kf.Index].random = CopiedRotationData.random;
                                    ((EventKeyframe)beatmapObject.events[kf.Type][kf.Index]).relative = CopiedRotationData.relative;
                                    //beatmapObject.events[kf.Type][kf.Index] = EventKeyframe.DeepCopy(CopiedRotationData);
                                    //beatmapObject.events[kf.Type][kf.Index].eventTime = time;
                                    RenderKeyframes(beatmapObject);
                                    RenderKeyframeDialog(beatmapObject);
                                    Updater.UpdateProcessor(beatmapObject, "Keyframes");
                                    EditorManager.inst.DisplayNotification("Pasted rotation keyframe data to current selected keyframe!", 2f, EditorManager.NotificationType.Success);
                                }
                                else
                                    EditorManager.inst.DisplayNotification("Rotation keyframe data not copied yet!", 2f, EditorManager.NotificationType.Error);
                                break;
                            case 3:
                                if (CopiedColorData != null)
                                {
                                    ClearKeyframes(beatmapObject);
                                    beatmapObject.events[kf.Type][kf.Index].eventValues = CopiedColorData.eventValues.Copy();
                                    beatmapObject.events[kf.Type][kf.Index].eventRandomValues = CopiedColorData.eventRandomValues.Copy();
                                    beatmapObject.events[kf.Type][kf.Index].random = CopiedColorData.random;
                                    ((EventKeyframe)beatmapObject.events[kf.Type][kf.Index]).relative = CopiedColorData.relative;
                                    //beatmapObject.events[kf.Type][kf.Index] = EventKeyframe.DeepCopy(CopiedColorData);
                                    //beatmapObject.events[kf.Type][kf.Index].eventTime = time;
                                    RenderKeyframes(beatmapObject);
                                    RenderKeyframeDialog(beatmapObject);
                                    Updater.UpdateProcessor(beatmapObject, "Keyframes");
                                    EditorManager.inst.DisplayNotification("Pasted color keyframe data to current selected keyframe!", 2f, EditorManager.NotificationType.Success);
                                }
                                else
                                    EditorManager.inst.DisplayNotification("Color keyframe data not copied yet!", 2f, EditorManager.NotificationType.Error);
                                break;
                        }
                    });

                    var deleteKey = kfdialog.Find("edit/del").GetComponent<Button>();

                    deleteKey.onClick.RemoveAllListeners();
                    deleteKey.interactable = kf.Index != 0;
                    deleteKey.onClick.AddListener(delegate ()
                    {
                        StartCoroutine(DeleteKeyframes(beatmapObject));
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
            float x = ObjEditor.inst.posCalc(objectLifeLength);

            ((RectTransform)ObjEditor.inst.objTimelineContent).sizeDelta = new Vector2(x, 0f);
            ((RectTransform)ObjEditor.inst.objTimelineGrid).sizeDelta = new Vector2(x, 122f);

            // Whether the value should clamp at 0.001 over StartTime or not.
            ObjEditor.inst.objTimelineSlider.minValue = AllowTimeExactlyAtStart ? beatmapObject.StartTime : beatmapObject.StartTime + 0.001f;
            ObjEditor.inst.objTimelineSlider.maxValue = beatmapObject.StartTime + objectLifeLength;

            ObjEditor.inst.objTimelineGrid.DeleteChildren();

            var gameObject = ObjEditor.inst.KeyframeEndPrefab.Duplicate(ObjEditor.inst.objTimelineGrid, "end keyframe");

            var rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(4f, 122f);
            rectTransform.anchoredPosition = new Vector2(beatmapObject.GetObjectLifeLength() * ObjEditor.inst.Zoom * 14f, 0f);

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
                var entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.PointerUp;
                entry.callback.AddListener(delegate (BaseEventData eventData)
                {
                    if (((PointerEventData)eventData).button == PointerEventData.InputButton.Right)
                    {
                        float timeTmp = ObjEditorPatch.timeCalc();

                        int index = beatmapObject.events[tmpIndex].FindLastIndex(x => x.eventTime <= timeTmp);
                        AddEvent(beatmapObject, timeTmp, tmpIndex, (EventKeyframe)beatmapObject.events[tmpIndex][index], false);
                        UpdateKeyframeOrder(beatmapObject);

                        RenderKeyframes(beatmapObject);

                        int keyframe = beatmapObject.events[tmpIndex].FindLastIndex(x => x.eventTime == timeTmp);
                        SetCurrentKeyframe(beatmapObject, tmpIndex, keyframe, false, InputDataManager.inst.editorActions.MultiSelect.IsPressed);
                        ResizeKeyframeTimeline(beatmapObject);

                        RenderKeyframeDialog(beatmapObject);

                        // Keyframes affect both physical object and timeline object.
                        RenderTimelineObject(new TimelineObject(beatmapObject));
                        if (UpdateObjects)
                            Updater.UpdateProcessor(beatmapObject, "Keyframes");
                    }
                });
                var comp = ObjEditor.inst.TimelineParents[tmpIndex].GetComponent<EventTrigger>();
                comp.triggers.RemoveAll(x => x.eventID == EventTriggerType.PointerUp);
                comp.triggers.Add(entry);
            }
        }

        public void ClearKeyframes(BeatmapObject beatmapObject)
        {
            var timelineObject = GetTimelineObject(beatmapObject);

            foreach (var kf in timelineObject.InternalSelections)
                Destroy(kf.GameObject);
        }

        public TimelineObject GetKeyframe(BeatmapObject beatmapObject, int type, int index)
        {
            var bmTimelineObject = GetTimelineObject(beatmapObject);

            var kf = bmTimelineObject.InternalSelections.Find(x => x.Type == type && x.Index == index);

            if (!kf)
                kf = bmTimelineObject.InternalSelections.Find(x => x.ID == (beatmapObject.events[type][index] as EventKeyframe).id);

            if (!kf)
            {
                kf = CreateKeyframe(beatmapObject, type, index);
                bmTimelineObject.InternalSelections.Add(kf);
            }

            if (!kf.GameObject)
            {
                kf.GameObject = KeyframeObject(beatmapObject, kf);
                kf.Image = kf.GameObject.transform.GetChild(0).GetComponent<Image>();
            }

            return kf;
        }

        public void CreateKeyframes(BeatmapObject beatmapObject)
        {
            ClearKeyframes(beatmapObject);

            for (int i = 0; i < beatmapObject.events.Count; i++)
            {
                if (beatmapObject.events[i].Count > 0)
                {
                    for (int j = 0; j < beatmapObject.events[i].Count; j++)
                    {
                        if (beatmapObject.timelineObject)
                        {
                            var keyframe = (EventKeyframe)beatmapObject.events[i][j];
                            var kf = beatmapObject.timelineObject.InternalSelections.Find(x => x.ID == keyframe.id);
                            if (!kf)
                            {
                                kf = CreateKeyframe(beatmapObject, i, j);
                                beatmapObject.timelineObject.InternalSelections.Add(kf);
                            }

                            if (!kf.GameObject)
                            {
                                kf.GameObject = KeyframeObject(beatmapObject, kf);
                                kf.Image = kf.GameObject.transform.GetChild(0).GetComponent<Image>();
                            }

                            RenderKeyframe(beatmapObject, kf);
                        }
                    }
                }
            }
        }

        public TimelineObject CreateKeyframe(BeatmapObject beatmapObject, int type, int index)
        {
            var eventKeyframe = beatmapObject.events[type][index];

            var kf = new TimelineObject(eventKeyframe)
            {
                Type = type,
                Index = index
            };

            kf.GameObject = KeyframeObject(beatmapObject, kf);
            kf.Image = kf.GameObject.transform.GetChild(0).GetComponent<Image>();

            return kf;
        }

        public GameObject KeyframeObject(BeatmapObject beatmapObject, TimelineObject kf)
        {
            var gameObject = ObjEditor.inst.objTimelinePrefab.Duplicate(ObjEditor.inst.TimelineParents[kf.Type], $"{IntToType(kf.Type)}_{kf.Index}");

            var button = gameObject.GetComponent<Button>();
            button.onClick.ClearAll();
            button.onClick.AddListener(delegate ()
            {
                if (!Input.GetMouseButtonDown(2))
                    SetCurrentKeyframe(beatmapObject, kf.Type, kf.Index, false, InputDataManager.inst.editorActions.MultiSelect.IsPressed);
            });

            TriggerHelper.AddEventTriggerParams(gameObject,
                TriggerHelper.CreateKeyframeStartDragTrigger(beatmapObject, kf),
                TriggerHelper.CreateKeyframeEndDragTrigger(beatmapObject, kf),
                TriggerHelper.CreateKeyframeSelectTrigger(beatmapObject, kf));

            return gameObject;
        }

        public void RenderKeyframes(BeatmapObject beatmapObject)
        {
            for (int i = 0; i < beatmapObject.events.Count; i++)
            {
                for (int j = 0; j < beatmapObject.events[i].Count; j++)
                {
                    var kf = GetKeyframe(beatmapObject, i, j);

                    RenderKeyframe(beatmapObject, kf);
                }
            }

            var timelineObject = GetTimelineObject(beatmapObject);
            if (timelineObject.InternalSelections.Count > 0 && timelineObject.InternalSelections.Where(x => x.selected).Count() == 0)
                timelineObject.InternalSelections[0].selected = true;
        }

        public void RenderKeyframe(BeatmapObject beatmapObject, TimelineObject timelineObject)
        {
            if (beatmapObject.events[timelineObject.Type].Has(x => (x as EventKeyframe).id == timelineObject.ID))
                timelineObject.Index = beatmapObject.events[timelineObject.Type].FindIndex(x => (x as EventKeyframe).id == timelineObject.ID);

            var eventKeyframe = timelineObject.GetData<EventKeyframe>();
            timelineObject.Image.sprite =
                                RTEditor.GetKeyframeIcon(eventKeyframe.curveType,
                                beatmapObject.events[timelineObject.Type].Count > timelineObject.Index + 1 ?
                                beatmapObject.events[timelineObject.Type][timelineObject.Index + 1].curveType : DataManager.inst.AnimationList[0]);

            float x = ObjEditor.inst.posCalc(eventKeyframe.eventTime);

            var rectTransform = (RectTransform)timelineObject.GameObject.transform;
            rectTransform.sizeDelta = new Vector2(14f, 25f);
            rectTransform.anchoredPosition = new Vector2(x, 0f);
        }

        IEnumerator IRenderKeyframeDialog(Transform p, int type, BeatmapObject beatmapObject)
        {
            if (beatmapObject.events[type].Count > 0)
            {
                var keyframe = (EventKeyframe)beatmapObject.events[type][ObjEditor.inst.currentKeyframe];
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

                TriggerHelper.IncreaseDecreaseButtons(tif, t: p.Find("time"));

                p.Find("curves_label").gameObject.SetActive(ObjEditor.inst.currentKeyframe != 0);
                p.Find("curves").gameObject.SetActive(ObjEditor.inst.currentKeyframe != 0);
                var curves = p.Find("curves").GetComponent<Dropdown>();
                curves.onValueChanged.RemoveAllListeners();
                if (DataManager.inst.AnimationListDictionaryBack.ContainsKey(keyframe.curveType))
                {
                    curves.value = DataManager.inst.AnimationListDictionaryBack[keyframe.curveType];
                }
                curves.onValueChanged.AddListener(delegate (int _value)
                {
                    keyframe.curveType = DataManager.inst.AnimationListDictionary[_value];

                    // Since keyframe curve has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        Updater.UpdateProcessor(beatmapObject, "Keyframes");
                    RenderKeyframes(beatmapObject);
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

                    var relative = p.Find("relative").GetComponent<Toggle>();
                    relative.onValueChanged.ClearAll();
                    relative.isOn = keyframe.relative;
                    relative.onValueChanged.AddListener(delegate (bool _val)
                    {
                        keyframe.relative = _val;

                        // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                        if (UpdateObjects)
                            Updater.UpdateProcessor(beatmapObject, "Keyframes");
                    });
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

                        if (RTEditor.ShowModifiedColors)
                        {
                            var color = RTHelpers.BeatmapTheme.GetObjColor(tmpIndex);

                            float hue = beatmapObject.Interpolate(type, 2);
                            float sat = beatmapObject.Interpolate(type, 3);
                            float val = beatmapObject.Interpolate(type, 4);

                            toggle.GetComponent<Image>().color = RTHelpers.ChangeColorHSV(color, hue, sat, val);
                        }
                        else
                            toggle.GetComponent<Image>().color = RTHelpers.BeatmapTheme.GetObjColor(tmpIndex);

                        if (!toggle.GetComponent<HoverUI>())
                        {
                            var hoverUI = toggle.gameObject.AddComponent<HoverUI>();
                            hoverUI.animatePos = false;
                            hoverUI.animateSca = true;
                            hoverUI.size = 1.1f;
                        }
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
                                        Updater.UpdateProcessor(beatmapObject);
                                    //Updater.UpdateProcessor(beatmapObject, "Keyframes");
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
                                        Updater.UpdateProcessor(beatmapObject);
                                    //Updater.UpdateProcessor(beatmapObject, "Keyframes");
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
                                        Updater.UpdateProcessor(beatmapObject);
                                    //Updater.UpdateProcessor(beatmapObject, "Keyframes");
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

        public void UpdateKeyframeOrder(BeatmapObject beatmapObject)
        {
            //ClearKeyframes(beatmapObject);

            for (int i = 0; i < beatmapObject.events.Count; i++)
            {
                beatmapObject.events[i] = (from x in beatmapObject.events[i]
                                            orderby x.eventTime
                                            select x).ToList();

                //for (int j = 0; j < beatmapObject.events[i].Count; j++)
                //{
                //    if (beatmapObject.timelineObject.InternalSelections.TryFind(x => x.ID == (beatmapObject.events[i][j] as EventKeyframe).id, out TimelineObject timelineObject))
                //    {
                //        timelineObject.Type = i;
                //        timelineObject.Index = j;
                //    }
                //}
            }

            RenderKeyframes(beatmapObject);
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

                ResizeKeyframeTimeline(beatmapObject);

                RenderKeyframes(beatmapObject);

                // Keyframe Time affects both physical object and timeline object.
                RenderTimelineObject(new TimelineObject(beatmapObject));
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
                Updater.UpdateProcessor(beatmapObject);
            //Updater.UpdateProcessor(beatmapObject, "Keyframes");

            int num = 0;
            foreach (var toggle in ObjEditor.inst.colorButtons)
            {
                toggle.onValueChanged.RemoveAllListeners();

                toggle.isOn = num == value;

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

            RenderTimelineObject(new TimelineObject(beatmapObject));

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
            beatmapObject.editorData.layer = _layer;

            RenderTimelineObject(new TimelineObject(beatmapObject));
        }

        public void AddToLayer(BeatmapObject beatmapObject, int _add)
        {
            int num = beatmapObject.editorData.layer + _add;
            SetLayer(beatmapObject, num);
        }

        public void SetBin(BeatmapObject beatmapObject, int _bin)
        {
            beatmapObject.editorData.Bin = Mathf.Clamp(_bin, 0, 14);

            RenderTimelineObject(new TimelineObject(beatmapObject));
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

            RenderTimelineObject(new TimelineObject(beatmapObject));
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
