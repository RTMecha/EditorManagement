﻿using Crosstales.FB;
using EditorManagement.Functions.Components;
using EditorManagement.Functions.Helpers;
using EditorManagement.Patchers;
using HarmonyLib;
using LSFunctions;
using RTFunctions.Functions;
using RTFunctions.Functions.Components;
using RTFunctions.Functions.Data;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;
using RTFunctions.Functions.Optimization;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using AutoKillType = DataManager.GameData.BeatmapObject.AutoKillType;
using BaseBeatmapObject = DataManager.GameData.BeatmapObject;
using BaseEventKeyframe = DataManager.GameData.EventKeyframe;
using BasePrefab = DataManager.GameData.Prefab;
using ObjectType = RTFunctions.Functions.Data.BeatmapObject.ObjectType;

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
                    mod.Methods.Add("RenderKeyframeDialog", (Action<BeatmapObject>)RenderObjectKeyframesDialog);
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

            StartCoroutine(Wait());

            if (!ModCompatibility.sharedFunctions.ContainsKey("RefreshObjectGUI"))
                ModCompatibility.sharedFunctions.Add("RefreshObjectGUI", (Action<BeatmapObject>)delegate (BeatmapObject beatmapObject) { StartCoroutine(RefreshObjectGUI(beatmapObject)); });
            if (ModCompatibility.sharedFunctions.ContainsKey("RefreshObjectGUI"))
                ModCompatibility.sharedFunctions["RefreshObjectGUI"] = (Action<BeatmapObject>)delegate (BeatmapObject beatmapObject) { StartCoroutine(RefreshObjectGUI(beatmapObject)); };

            var idRight = ObjEditor.inst.objTimelineContent.parent.Find("id/right");
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

                        var beatmapObject = CurrentSelection.GetData<BeatmapObject>();

                        int index = beatmapObject.events[tmpIndex].FindLastIndex(x => x.eventTime <= timeTmp);
                        var eventKeyfame = AddEvent(beatmapObject, timeTmp, tmpIndex, (EventKeyframe)beatmapObject.events[tmpIndex][index], false);
                        UpdateKeyframeOrder(beatmapObject);

                        RenderKeyframes(beatmapObject);

                        int keyframe = beatmapObject.events[tmpIndex].FindLastIndex(x => x.eventTime == eventKeyfame.eventTime);
                        if (keyframe < 0)
                            keyframe = 0;

                        SetCurrentKeyframe(beatmapObject, tmpIndex, keyframe, false, InputDataManager.inst.editorActions.MultiSelect.IsPressed);
                        ResizeKeyframeTimeline(beatmapObject);

                        RenderObjectKeyframesDialog(beatmapObject);

                        // Keyframes affect both physical object and timeline object.
                        RenderTimelineObject(new TimelineObject(beatmapObject));
                        if (UpdateObjects)
                            Updater.UpdateProcessor(beatmapObject, "Keyframes");
                    }
                });
                var comp = ObjEditor.inst.TimelineParents[tmpIndex].GetComponent<EventTrigger>();
                comp.triggers.RemoveAll(x => x.eventID == EventTriggerType.PointerUp);
                comp.triggers.Add(entry);

                var image = idRight.GetChild(i).GetComponent<Image>();

                EditorThemeManager.AddElement(new EditorThemeManager.Element("Object Editor Keyframe Bin", $"Object Keyframe Color {i + 1}", image.gameObject, new List<Component>
                {
                    image,
                }));
            }

            ObjEditor.inst.objTimelineSlider.onValueChanged.RemoveAllListeners();
            ObjEditor.inst.objTimelineSlider.onValueChanged.AddListener(delegate (float _val)
            {
                if (ObjEditor.inst.changingTime)
                {
                    ObjEditor.inst.newTime = _val;
                    AudioManager.inst.SetMusicTime(Mathf.Clamp(_val, 0f, AudioManager.inst.CurrentAudioSource.clip.length));
                }
            });

            var objectKeyframeTimelineEventTrigger = ObjEditor.inst.objTimelineContent.parent.parent.parent.GetComponent<EventTrigger>();
            ObjEditor.inst.objTimelineContent.GetComponent<EventTrigger>().triggers.AddRange(objectKeyframeTimelineEventTrigger.triggers);
            objectKeyframeTimelineEventTrigger.triggers.Clear();

            TriggerHelper.AddEventTriggerParams(timelinePosScrollbar.gameObject, TriggerHelper.CreateEntry(EventTriggerType.Scroll, delegate (BaseEventData baseEventData)
            {
                var pointerEventData = (PointerEventData)baseEventData;

                var scrollBar = timelinePosScrollbar;
                float multiply = Input.GetKey(KeyCode.LeftAlt) ? 0.1f : Input.GetKey(KeyCode.LeftControl) ? 10f : 1f;

                scrollBar.value = pointerEventData.scrollDelta.y > 0f ? scrollBar.value + (0.005f * multiply) : pointerEventData.scrollDelta.y < 0f ? scrollBar.value - (0.005f * multiply) : 0f;
            }));
        }

        void Update()
        {
            if (!ModCompatibility.sharedFunctions.ContainsKey("SelectedObjectCount"))
                ModCompatibility.sharedFunctions.Add("SelectedObjectCount", SelectedObjectCount);
            else
                ModCompatibility.sharedFunctions["SelectedObjectCount"] = SelectedObjectCount;

            if (!ModCompatibility.sharedFunctions.ContainsKey("CurrentSelection"))
                ModCompatibility.sharedFunctions.Add("CurrentSelection", CurrentSelection);
            if (ModCompatibility.sharedFunctions.ContainsKey("CurrentSelection"))
                ModCompatibility.sharedFunctions["CurrentSelection"] = CurrentSelection;
        }

        public IEnumerator Wait()
        {
            while (!PrefabEditor.inst || !RTEditor.inst || !RTEditor.inst.defaultIF)
                yield return null;

            try
            {
                tagPrefab = new GameObject("Tag");
                tagPrefab.transform.SetParent(transform);
                var tagPrefabRT = tagPrefab.AddComponent<RectTransform>();
                var tagPrefabImage = tagPrefab.AddComponent<Image>();
                tagPrefabImage.color = new Color(1f, 1f, 1f, 1f);
                var tagPrefabLayout = tagPrefab.AddComponent<HorizontalLayoutGroup>();
                tagPrefabLayout.childControlWidth = false;
                tagPrefabLayout.childForceExpandWidth = false;

                var input = RTEditor.inst.defaultIF.Duplicate(tagPrefabRT, "Input");
                input.transform.localScale = Vector3.one;
                ((RectTransform)input.transform).sizeDelta = new Vector2(136f, 32f);
                var text = input.transform.Find("Text").GetComponent<Text>();
                text.alignment = TextAnchor.MiddleCenter;
                text.fontSize = 17;

                var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(tagPrefabRT, "Delete");
                UIManager.SetRectTransform(delete.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.one, Vector2.one, new Vector2(32f, 32f));
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.ToString());
            }
        }

        public Scrollbar timelinePosScrollbar;
        public GameObject shapeButtonPrefab;

        public TimelineObject CurrentSelection { get; set; } = new TimelineObject(null);

        public List<TimelineObject> SelectedObjects => RTEditor.inst.timelineObjects.FindAll(x => x.selected && !x.IsEventKeyframe);
        public List<TimelineObject> SelectedBeatmapObjects => RTEditor.inst.TimelineBeatmapObjects.FindAll(x => x.selected);
        public List<TimelineObject> SelectedPrefabObjects => RTEditor.inst.TimelinePrefabObjects.FindAll(x => x.selected);

        public int SelectedObjectCount => SelectedObjects.Count;

        public List<TimelineObject> copiedObjectKeyframes = new List<TimelineObject>();

        public EventKeyframe CopiedPositionData { get; set; }
        public EventKeyframe CopiedScaleData { get; set; }
        public EventKeyframe CopiedRotationData { get; set; }
        public EventKeyframe CopiedColorData { get; set; }

        public bool colorShifted;

        public static bool RenderPrefabTypeIcon { get; set; }

        public static float TimelineObjectHoverSize { get; set; }

        public GameObject tagPrefab;

        public void OpenDialog(BeatmapObject beatmapObject)
        {
            if (CurrentSelection.IsBeatmapObject)
            {
                if (EditorManager.inst.ActiveDialogs.Count > 2 || !EditorManager.inst.ActiveDialogs.Has(x => x.Name == "Object Editor"))
                {
                    EditorManager.inst.ClearDialogs();
                    EditorManager.inst.ShowDialog("Object Editor");
                }

                StartCoroutine(RefreshObjectGUI(beatmapObject));
                StartCoroutine(RememberTimeline());
            }
            else
                EditorManager.inst.DisplayNotification("Cannot edit non-object!", 2f, EditorManager.NotificationType.Error);
        }

        public IEnumerator RememberTimeline()
        {
            // Here we remember an object's zoom and timeline position.
            ObjEditor.inst.Zoom = CurrentSelection.Zoom;
            yield return new WaitForSeconds(0.001f);
            timelinePosScrollbar.value = CurrentSelection.TimelinePosition;
            yield break;
        }

        #region Deleting

        public IEnumerator DeleteObjects(bool _set = true)
        {
            var list = SelectedObjects;
            int count = SelectedObjectCount;

            if (count == DataManager.inst.gameData.beatmapObjects.FindAll(x => !x.fromPrefab).Count + DataManager.inst.gameData.prefabObjects.Count)
            {
                yield break;
            }

            int min = list.Min(x => x.Index) - 1;

            var beatmapObjects = list.Where(x => x.IsBeatmapObject).Select(x => x.GetData<BeatmapObject>()).ToList();
            var beatmapObjectIDs = new List<string>();
            var prefabObjectIDs = new List<string>();

            beatmapObjectIDs.AddRange(list.Where(x => x.IsBeatmapObject).Select(x => x.ID));
            prefabObjectIDs.AddRange(list.Where(x => x.IsPrefabObject).Select(x => x.ID));

            if (beatmapObjectIDs.Count == DataManager.inst.gameData.beatmapObjects.FindAll(x => !x.fromPrefab).Count)
            {
                yield break;
            }
            
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

            if (count < 1)
            {
                EditorManager.inst.DisplayNotification($"No Object keyframes to delete.", 2f, EditorManager.NotificationType.Warning);
                yield break;
            }

            int index = list.Min(x => x.Index);
            int type = list.Min(x => x.Type);
            bool allOfTheSameType = list.All(x => x.Type == list.Min(y => y.Type));

            EditorManager.inst.DisplayNotification($"Deleting Object Keyframes [ {count} ]", 0.2f, EditorManager.NotificationType.Success);

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

            bmTimelineObject.InternalSelections.Where(x => x.selected).ToList().ForEach(x => Destroy(x.GameObject));
            bmTimelineObject.InternalSelections.RemoveAll(x => x.selected);

            RenderTimelineObject(bmTimelineObject);
            Updater.UpdateProcessor(beatmapObject, "Keyframes");

            if (beatmapObject.autoKillType == AutoKillType.LastKeyframe || beatmapObject.autoKillType == AutoKillType.LastKeyframeOffset)
                Updater.UpdateProcessor(beatmapObject, "Autokill");

            RenderKeyframes(beatmapObject);

            if (count == 1 || allOfTheSameType)
                SetCurrentKeyframe(beatmapObject, type, Mathf.Clamp(index - 1, 0, beatmapObject.events[type].Count - 1));
            else
                SetCurrentKeyframe(beatmapObject, type, 0);

            ResizeKeyframeTimeline(beatmapObject);

            EditorManager.inst.DisplayNotification("Deleted Object Keyframes [ " + count + " ]", 2f, EditorManager.NotificationType.Success);

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

        public void PasteKeyframes(BeatmapObject beatmapObject, bool setTime = true) => PasteKeyframes(beatmapObject, copiedObjectKeyframes, setTime);

        public void PasteKeyframes(BeatmapObject beatmapObject, List<TimelineObject> kfs, bool setTime = true)
        {
            if (kfs.Count <= 0)
            {
                Debug.LogError($"{ObjEditor.inst.className}No copied event yet!");
                return;
            }

            var ids = new List<string>();
            for (int i = 0; i < beatmapObject.events.Count; i++)
                beatmapObject.events[i].AddRange(kfs.Where(x => x.Type == i).Select(x =>
                {
                    var kf = PasteKF(beatmapObject, x, setTime);
                    ids.Add(kf.id);
                    return kf;
                }));

            ResizeKeyframeTimeline(beatmapObject);
            UpdateKeyframeOrder(beatmapObject);
            RenderKeyframes(beatmapObject);

            if (EditorConfig.Instance.SelectPasted.Value)
            {
                var timelineObject = GetTimelineObject(beatmapObject);
                foreach (var kf in timelineObject.InternalSelections)
                {
                    kf.selected = ids.Contains(kf.ID);
                }
            }

            RenderObjectKeyframesDialog(beatmapObject);
            RenderTimelineObject(new TimelineObject(beatmapObject));

            if (UpdateObjects)
                Updater.UpdateProcessor(beatmapObject, "Keyframes");
        }

        EventKeyframe PasteKF(BeatmapObject beatmapObject, TimelineObject timelineObject, bool setTime = true)
        {
            var eventKeyframe = EventKeyframe.DeepCopy(timelineObject.GetData<EventKeyframe>());

            if (setTime)
            {
                eventKeyframe.eventTime = EditorManager.inst.CurrentAudioPos - beatmapObject.StartTime + eventKeyframe.eventTime;
                if (eventKeyframe.eventTime <= 0f)
                    eventKeyframe.eventTime = 0.001f;
            }

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

            Prefab pr = null;

            if (RTFile.FileExists(Application.persistentDataPath + "/copied_objects.lsp"))
            {
                pr = Prefab.Parse(JSON.Parse(RTFile.ReadFromFile(Application.persistentDataPath + "/copied_objects.lsp")));

                ObjEditor.inst.hasCopiedObject = true;
            }

            StartCoroutine(AddPrefabExpandedToLevel(pr ?? (Prefab)ObjEditor.inst.beatmapObjCopy, true, _offsetTime, false, _regen));

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

        public IEnumerable<BeatmapObject> PasteBeatmapObjects(BasePrefab prefab, Dictionary<string, string> ids, Dictionary<string, string> prefabInstances, float audioTime,
            bool select = false, float offset = 0f, bool undone = false, bool regen = false)
        {
            foreach (var beatmapObject in prefab.objects)
            {
                yield return Expand((BeatmapObject)beatmapObject, ids, prefabInstances, audioTime, prefab, select, offset, undone, regen);
            }
        }

        public IEnumerator ToTimelineObjects(IEnumerable<BeatmapObject> beatmapObjects, bool select)
        {
            float delay = 0f;
            int count = beatmapObjects.Count();
            var timelineObjects = beatmapObjects.Select(x => new TimelineObject(x));
            foreach (var timelineObject in timelineObjects)
            {
                yield return new WaitForSeconds(delay);
                RenderTimelineObject(timelineObject);
                if (select)
                {
                    if (count > 1)
                    {
                        timelineObject.selected = true;
                        CurrentSelection = timelineObject;
                    }
                    else
                        SetCurrentObject(timelineObject, openDialog: false);
                }
                delay += 0.0001f;
            }
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
        public IEnumerator AddPrefabExpandedToLevel(Prefab prefab, bool select = false, float offset = 0f, bool undone = false, bool regen = false, bool retainID = false)
        {
            RTEditor.inst.ienumRunning = true;
            float delay = 0f;
            float audioTime = EditorManager.inst.CurrentAudioPos;
            Debug.Log($"{EditorPlugin.className}Placing prefab with {prefab.objects.Count} objects and {prefab.prefabObjects.Count} prefabs");

            if (RTEditor.inst.layerType == RTEditor.LayerType.Events)
                RTEditor.inst.SetLayer(RTEditor.LayerType.Objects);

            if (CurrentSelection.IsBeatmapObject && prefab.objects.Count > 0)
            {
                ClearKeyframes(CurrentSelection.GetData<BeatmapObject>());
            }

            if (prefab.objects.Count > 1 || prefab.prefabObjects.Count > 1)
                EditorManager.inst.ClearDialogs();

            //Objects
            {
                var ids = prefab.objects.ToDictionary(x => x.id, x => LSText.randomString(16));

                var prefabInstances = new Dictionary<string, string>();
                foreach (var beatmapObject in prefab.objects)
                    if (!string.IsNullOrEmpty(beatmapObject.prefabInstanceID) && !prefabInstances.ContainsKey(beatmapObject.prefabInstanceID))
                        prefabInstances.Add(beatmapObject.prefabInstanceID, LSText.randomString(16));

                //var beatmapObjects = PasteBeatmapObjects(prefab, ids, prefabInstances, audioTime, select, offset, undone, regen);
                //DataManager.inst.gameData.beatmapObjects.AddRange(beatmapObjects);

                //foreach (var beatmapObject in beatmapObjects)
                //{
                //    Updater.UpdateProcessor(beatmapObject);
                //}

                //yield return StartCoroutine(ToTimelineObjects(beatmapObjects, select));

                var pastedObjects = new List<BeatmapObject>();
                foreach (var beatmapObject in prefab.objects)
                {
                    yield return new WaitForSeconds(delay);
                    var beatmapObjectCopy = BeatmapObject.DeepCopy((BeatmapObject)beatmapObject, false);

                    if (ids.ContainsKey(beatmapObject.id) && !retainID)
                        beatmapObjectCopy.id = ids[beatmapObject.id];

                    if (ids.ContainsKey(beatmapObject.parent) && !retainID)
                        beatmapObjectCopy.parent = ids[beatmapObject.parent];
                    else if (DataManager.inst.gameData.beatmapObjects.FindIndex(x => x.id == beatmapObject.parent) == -1 && beatmapObjectCopy.parent != "CAMERA_PARENT" && !retainID)
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

                    if (!AssetManager.SpriteAssets.ContainsKey(beatmapObject.text) && prefab.SpriteAssets.ContainsKey(beatmapObject.text))
                    {
                        AssetManager.SpriteAssets.Add(beatmapObject.text, prefab.SpriteAssets[beatmapObject.text]);
                    }

                    beatmapObjectCopy.editorData.layer = RTEditor.inst.Layer;
                    DataManager.inst.gameData.beatmapObjects.Add(beatmapObjectCopy);
                    if (Updater.levelProcessor && Updater.levelProcessor.converter != null && !Updater.levelProcessor.converter.beatmapObjects.ContainsKey(beatmapObjectCopy.id))
                        Updater.levelProcessor.converter.beatmapObjects.Add(beatmapObjectCopy.id, beatmapObjectCopy);

                    pastedObjects.Add(beatmapObjectCopy);

                    var timelineObject = new TimelineObject(beatmapObjectCopy);

                    timelineObject.selected = true;
                    CurrentSelection = timelineObject;

                    RenderTimelineObject(timelineObject);

                    delay += 0.0001f;
                }

                foreach (var beatmapObject in pastedObjects)
                {
                    Updater.UpdateProcessor(beatmapObject);
                }

                pastedObjects.Clear();
                pastedObjects = null;
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

                    Updater.AddPrefabToLevel(prefabObject);

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

        public static bool SetToCenterCam => EditorConfig.Instance.CreateObjectsatCameraCenter.Value;

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
            OpenDialog(bm);

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
            bm.name = RTHelpers.AprilFools ? "<font=Arrhythmia>bro" : "circle";

            if (SetToCenterCam)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].eventValues[0] = pos.x;
                bm.events[0][0].eventValues[1] = pos.y;
            }

            Updater.UpdateProcessor(bm);
            RenderTimelineObject(timelineObject);
            OpenDialog(bm);

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
            bm.name = RTHelpers.AprilFools ? "baracuda <i>beat plays</i>" : "triangle";

            if (SetToCenterCam)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].eventValues[0] = pos.x;
                bm.events[0][0].eventValues[1] = pos.y;
            }

            Updater.UpdateProcessor(bm);
            RenderTimelineObject(timelineObject);
            OpenDialog(bm);

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
            bm.text = RTHelpers.AprilFools ? "Never gonna give you up<br>" +
                                            "Never gonna let you down<br>" +
                                            "Never gonna run around and desert you<br>" +
                                            "Never gonna make you cry<br>" +
                                            "Never gonna say goodbye<br>" +
                                            "Never gonna tell a lie and hurt you" : "text";
            bm.name = RTHelpers.AprilFools ? "Don't look at my text" : "text";
            bm.objectType = ObjectType.Decoration;
            if (RTHelpers.AprilFools)
                bm.StartTime += 1f;

            if (SetToCenterCam)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].eventValues[0] = pos.x;
                bm.events[0][0].eventValues[1] = pos.y;
            }

            Updater.UpdateProcessor(bm);
            RenderTimelineObject(timelineObject);

            if (!RTHelpers.AprilFools)
                OpenDialog(bm);

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
            bm.name = RTHelpers.AprilFools ? "super" : "hexagon";

            if (SetToCenterCam)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].eventValues[0] = pos.x;
                bm.events[0][0].eventValues[1] = pos.y;
            }

            Updater.UpdateProcessor(bm);
            RenderTimelineObject(timelineObject);
            OpenDialog(bm);

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
            bm.name = RTHelpers.AprilFools ? "totally not deprecated object" : "helper";
            bm.objectType = RTHelpers.AprilFools ? ObjectType.Decoration : ObjectType.Helper;
            if (RTHelpers.AprilFools)
                bm.events[3][0].eventValues[1] = 0.65f;

            if (SetToCenterCam)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].eventValues[0] = pos.x;
                bm.events[0][0].eventValues[1] = pos.y;
            }

            Updater.UpdateProcessor(bm);
            RenderTimelineObject(timelineObject);
            OpenDialog(bm);

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
            if (!RTHelpers.AprilFools)
                bm.objectType = ObjectType.Decoration;

            if (SetToCenterCam)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].eventValues[0] = pos.x;
                bm.events[0][0].eventValues[1] = pos.y;
            }

            Updater.UpdateProcessor(bm);
            RenderTimelineObject(timelineObject);
            OpenDialog(bm);

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
            if (!RTHelpers.AprilFools)
                bm.objectType = ObjectType.Empty;

            if (SetToCenterCam)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].eventValues[0] = pos.x;
                bm.events[0][0].eventValues[1] = pos.y + (RTHelpers.AprilFools ? 999f : 0f);
            }

            Updater.UpdateProcessor(bm);
            RenderTimelineObject(timelineObject);
            OpenDialog(bm);

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
            bm.name = RTHelpers.AprilFools ? "dead" : "no autokill";
            bm.autoKillType = AutoKillType.OldStyleNoAutokill;

            if (SetToCenterCam)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].eventValues[0] = pos.x;
                bm.events[0][0].eventValues[1] = pos.y;
            }

            Updater.UpdateProcessor(bm);
            RenderTimelineObject(timelineObject);
            OpenDialog(bm);

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
            // Position
            list[0].Add(new EventKeyframe(0f, new float[3]
            {
                0f,
                0f,
                0f,
            }, new float[4], 0));
            // Scale
            list[1].Add(new EventKeyframe(0f, new float[]
            {
                1f,
                1f
            }, new float[3], 0));
            // Rotation
            list[2].Add(new EventKeyframe(0f, new float[1], new float[3], 0));
            ((EventKeyframe)list[2][0]).relative = true;
            // Color
            list[3].Add(new EventKeyframe(0f, new float[5], new float[4], 0));

            var beatmapObject = new BeatmapObject(true, AudioManager.inst.CurrentAudioSource.time, "", 0, "", list);
            beatmapObject.id = LSText.randomString(16);
            beatmapObject.autoKillType = AutoKillType.LastKeyframeOffset;
            beatmapObject.autoKillOffset = 5f;

            if (!RTHelpers.AprilFools)
                beatmapObject.editorData.layer = RTEditor.inst.Layer;
            beatmapObject.parentType = EditorConfig.Instance.CreateObjectsScaleParentDefault.Value ? "111" : "101";

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

            return timelineObject;
        }

        public static BeatmapObject CreateNewBeatmapObject(float _time, bool _add = true)
        {
            var beatmapObject = new BeatmapObject();
            beatmapObject.id = LSText.randomString(16);
            beatmapObject.StartTime = _time;

            if (!RTHelpers.AprilFools)
                beatmapObject.editorData.layer = RTEditor.inst.Layer;

            var positionKeyframe = new EventKeyframe();
            positionKeyframe.eventTime = 0f;
            positionKeyframe.SetEventValues(new float[3]);
            positionKeyframe.SetEventRandomValues(new float[4]);

            var scaleKeyframe = new EventKeyframe();
            scaleKeyframe.eventTime = 0f;
            scaleKeyframe.SetEventValues(new float[]
            {
                1f,
                1f
            });

            var rotationKeyframe = new EventKeyframe();
            rotationKeyframe.eventTime = 0f;
            rotationKeyframe.relative = true;
            rotationKeyframe.SetEventValues(new float[1]);

            var colorKeyframe = new EventKeyframe();
            colorKeyframe.eventTime = 0f;
            colorKeyframe.SetEventValues(new float[]
            {
                0f,
                0f,
                0f,
                0f,
                0f
            });
            colorKeyframe.SetEventRandomValues(0f, 0f, 0f, 0f);

            beatmapObject.events[0].Add(positionKeyframe);
            beatmapObject.events[1].Add(scaleKeyframe);
            beatmapObject.events[2].Add(rotationKeyframe);
            beatmapObject.events[3].Add(colorKeyframe);

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
            if (!_add)
                DeselectAllObjects();

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
            RenderObjectKeyframesDialog(bm);
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
            if (SelectedObjectCount + 1 > 1)
            {
                timelineObject.selected = true;

                EditorManager.inst.ClearDialogs();
                EditorManager.inst.ShowDialog("Multi Object Editor", false);

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

            if (RTObject.Enabled && timelineObject.IsBeatmapObject && timelineObject.GetData<BeatmapObject>().RTObject)
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

            RenderObjectKeyframesDialog(beatmapObject);
        }

        public EventKeyframe AddEvent(BeatmapObject beatmapObject, float time, int type, EventKeyframe _keyframe, bool openDialog)
        {
            var eventKeyframe = EventKeyframe.DeepCopy(_keyframe);
            var t = SettingEditor.inst.SnapActive && EditorConfig.Instance.BPMSnapsKeyframes.Value ? -(beatmapObject.StartTime - RTEditor.SnapToBPM(beatmapObject.StartTime + time)) : time;
            eventKeyframe.eventTime = t;

            if (eventKeyframe.relative)
                for (int i = 0; i < eventKeyframe.eventValues.Length; i++)
                    eventKeyframe.eventValues[i] = 0f;

            beatmapObject.events[type].Add(eventKeyframe);

            RenderTimelineObject(new TimelineObject(beatmapObject));
            if (openDialog)
            {
                ResizeKeyframeTimeline(beatmapObject);
                RenderObjectKeyframesDialog(beatmapObject);
            }
            return eventKeyframe;
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

        public GameObject RenderTimelineObject(TimelineObject timelineObject, bool ignoreLayer = true)
        {
            GameObject gameObject = null;

            if (!RTEditor.inst.timelineObjects.Has(x => x.ID == timelineObject.ID))
                RTEditor.inst.timelineObjects.Add(timelineObject);
            else
                timelineObject = RTEditor.inst.timelineObjects.Find(x => x.ID == timelineObject.ID);

            gameObject = !timelineObject.GameObject ? CreateTimelineObject(timelineObject) : timelineObject.GameObject;

            if (ignoreLayer || RTEditor.inst.Layer == timelineObject.Layer)
            {
                bool locked = false;
                bool collapsed = false;
                int bin = 0;
                string name = "object name";
                float startTime = 0f;
                float offset = 0f;

                string nullName = "";

                var image =  timelineObject.Image;

                var color = ObjEditor.inst.NormalColor;

                if (timelineObject.IsBeatmapObject)
                {
                    var beatmapObject = timelineObject.GetData<BeatmapObject>();
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
                    var prefabObject = timelineObject.GetData<PrefabObject>();
                    var prefab = DataManager.inst.gameData.prefabs.Find(x => x.ID == prefabObject.prefabID);

                    locked = prefabObject.editorData.locked;
                    collapsed = prefabObject.editorData.collapse;
                    bin = prefabObject.editorData.Bin;
                    name = prefab.Name;
                    startTime = prefabObject.StartTime + prefab.Offset;
                    offset = prefab.GetPrefabLifeLength(prefabObject, true);
                    image.type = Image.Type.Simple;
                    image.sprite = null;

                    var prefabType = prefab.Type >= 0 && prefab.Type < DataManager.inst.PrefabTypes.Count ? (PrefabType)DataManager.inst.PrefabTypes[prefab.Type] : PrefabType.InvalidType;

                    color = prefabType.Color;
                    nullName = prefabType.Name;
                }

                if (timelineObject.Text)
                {
                    var textMeshNoob = timelineObject.Text;
                    textMeshNoob.text = (!string.IsNullOrEmpty(name)) ? string.Format("<mark=#000000aa>{0}</mark>", name) : nullName;
                    textMeshNoob.color = LSColors.white;
                }

                bool isBeatmapObject = timelineObject.IsBeatmapObject && !string.IsNullOrEmpty(timelineObject.GetData<BeatmapObject>().prefabID) && DataManager.inst.gameData.prefabs.Has(x => x.ID == timelineObject.GetData<BeatmapObject>().prefabID);
                bool isPrefab = timelineObject.IsPrefabObject && timelineObject.GetData<PrefabObject>().GetPrefab() != null;

                gameObject.transform.Find("icons/lock").gameObject.SetActive(locked);
                gameObject.transform.Find("icons/dots").gameObject.SetActive(collapsed);
                gameObject.transform.Find("icons/type").gameObject.SetActive(RenderPrefabTypeIcon && (isBeatmapObject || isPrefab));

                if (RenderPrefabTypeIcon && (isBeatmapObject || isPrefab))
                {
                    var iconImage = gameObject.transform.Find("icons/type/type").GetComponent<Image>();
                    int i =
                        isBeatmapObject ? DataManager.inst.gameData.prefabs.Find(x => x.ID == timelineObject.GetData<BeatmapObject>().prefabID).Type :
                        isPrefab ? timelineObject.GetData<PrefabObject>().GetPrefab().Type : 0;

                    iconImage.sprite = (i < DataManager.inst.PrefabTypes.Count ? (PrefabType)DataManager.inst.PrefabTypes[i] : PrefabType.InvalidType).Icon;
                }

                float zoom = EditorManager.inst.Zoom;

                offset = offset <= 0.4f ? 0.4f * zoom : offset * zoom;

                var rectTransform = (RectTransform)gameObject.transform;
                rectTransform.sizeDelta = new Vector2(offset, 20f);
                rectTransform.anchoredPosition = new Vector2(startTime * zoom, (-20 * Mathf.Clamp(bin, 0, 14)));
                if (timelineObject.Hover)
                    timelineObject.Hover.size = TimelineObjectHoverSize;
                gameObject.SetActive(true);
            }

            return gameObject;
        }

        public void RenderTimelineObjects()
        {
            foreach (var timelineObject in RTEditor.inst.timelineObjects.FindAll(x => !x.IsEventKeyframe))
                RenderTimelineObject(timelineObject);
        }

        public void RenderTimelineObjectPosition(TimelineObject timelineObject)
        {
            float offset = 0f;
            float timeOffset = 0f;

            if (timelineObject.IsBeatmapObject)
            {
                var beatmapObject = timelineObject.GetData<BeatmapObject>();
                offset = beatmapObject.GetObjectLifeLength(_takeCollapseIntoConsideration: true);
            }

            if (timelineObject.IsPrefabObject)
            {
                var prefabObject = timelineObject.GetData<PrefabObject>();
                var prefab = DataManager.inst.gameData.prefabs.Find(x => x.ID == prefabObject.prefabID);

                offset = prefab.GetPrefabLifeLength(prefabObject, true);
                timeOffset = prefab.Offset;
            }

            float zoom = EditorManager.inst.Zoom;

            offset = offset <= 0.4f ? 0.4f * zoom : offset * zoom;

            var rectTransform = (RectTransform)timelineObject.GameObject.transform;
            rectTransform.sizeDelta = new Vector2(offset, 20f);
            rectTransform.anchoredPosition = new Vector2((timelineObject.Time + timeOffset) * zoom, (-20 * Mathf.Clamp(timelineObject.Bin, 0, 14)));
            if (timelineObject.Hover)
                timelineObject.Hover.size = TimelineObjectHoverSize;
        }

        public void RenderTimelineObjectsPositions()
        {
            foreach (var timelineObject in RTEditor.inst.timelineObjects.FindAll(x => !x.IsEventKeyframe && RTEditor.inst.Layer == x.Layer))
            {
                RenderTimelineObjectPosition(timelineObject);
            }
        }

        public GameObject CreateTimelineObject(TimelineObject timelineObject)
        {
            GameObject gameObject = null;

            if (!RTEditor.inst.timelineObjects.Has(x => x.ID == timelineObject.ID))
                RTEditor.inst.timelineObjects.Add(timelineObject);
            else
                timelineObject = RTEditor.inst.timelineObjects.Find(x => x.ID == timelineObject.ID);

            if (timelineObject.GameObject)
                Destroy(timelineObject.GameObject);

            gameObject = ObjEditor.inst.timelineObjectPrefab.Duplicate(EditorManager.inst.timeline.transform, "timeline object");
            var storage = gameObject.GetComponent<TimelineObjectStorage>();

            timelineObject.Hover = storage.hoverUI;
            timelineObject.GameObject = gameObject;
            timelineObject.Image = storage.image;
            timelineObject.Text = storage.text;

            storage.eventTrigger.triggers.Clear();
            storage.eventTrigger.triggers.Add(TriggerHelper.CreateBeatmapObjectTrigger(timelineObject));
            storage.eventTrigger.triggers.Add(TriggerHelper.CreateBeatmapObjectStartDragTrigger(timelineObject));
            storage.eventTrigger.triggers.Add(TriggerHelper.CreateBeatmapObjectEndDragTrigger(timelineObject));

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

                    objectUIElements.Add("ID Base", tfv.Find("id"));
                    objectUIElements.Add("ID Text", tfv.Find("id/text").GetComponent<Text>());
                    objectUIElements.Add("LDM Toggle", tfv.Find("id/ldm/toggle").GetComponent<Toggle>());

                    objectUIElements.Add("Name IF", tfv.Find("name/name").GetComponent<InputField>());
                    objectUIElements.Add("Object Type DD", tfv.Find("name/object-type").GetComponent<Dropdown>());
                    objectUIElements.Add("Tags Content", tfv.Find("Tags Scroll View/Viewport/Content").transform);

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
                    objectUIElements.Add("Parent Spawn Once", tfv.Find("parent_more/spawn_once").GetComponent<Toggle>());
                    objectUIElements.Add("Parent Search Open", tfv.Find("parent/parent").GetComponent<Button>());
                    objectUIElements.Add("Parent Clear", tfv.Find("parent/clear parent").GetComponent<Button>());
                    objectUIElements.Add("Parent Picker", tfv.Find("parent/parent picker").GetComponent<Button>());

                    objectUIElements.Add("Parent Offset 1", tfv.Find("parent_more/pos_row"));
                    objectUIElements.Add("Parent Offset 2", tfv.Find("parent_more/sca_row"));
                    objectUIElements.Add("Parent Offset 3", tfv.Find("parent_more/rot_row"));

                    objectUIElements.Add("Origin", tfv.Find("origin"));
                    objectUIElements.Add("Origin X IF", tfv.Find("origin/x").GetComponent<InputField>());
                    objectUIElements.Add("Origin Y IF", tfv.Find("origin/y").GetComponent<InputField>());

                    objectUIElements.Add("Shape", tfv.Find("shape"));
                    objectUIElements.Add("Shape Settings", tfv.Find("shapesettings"));

                    objectUIElements.Add("Depth", tfv.Find("depth"));
                    objectUIElements.Add("Depth T", tfv.Find("spacer"));
                    objectUIElements.Add("Depth Slider", tfv.Find("depth/depth").GetComponent<Slider>());
                    objectUIElements.Add("Depth IF", tfv.Find("spacer/depth")?.GetComponent<InputField>());
                    objectUIElements.Add("Depth <", tfv.Find("spacer/depth/<")?.GetComponent<Button>());
                    objectUIElements.Add("Depth >", tfv.Find("spacer/depth/>")?.GetComponent<Button>());
                    objectUIElements.Add("Render Type T", tfv.Find("rendertype"));
                    objectUIElements.Add("Render Type", tfv.Find("rendertype")?.GetComponent<Dropdown>());

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

        public static bool HideVisualElementsWhenObjectIsEmpty { get; set; }

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

                inst.RenderIDLDM(beatmapObject);
                inst.RenderName(beatmapObject);
                inst.RenderObjectType(beatmapObject);

                inst.RenderStartTime(beatmapObject);
                inst.RenderAutokill(beatmapObject);

                inst.RenderParent(beatmapObject);

                inst.RenderEmpty(beatmapObject);

                if (!HideVisualElementsWhenObjectIsEmpty || beatmapObject.objectType != ObjectType.Empty)
                {
                    inst.RenderOrigin(beatmapObject);
                    inst.RenderShape(beatmapObject);
                    inst.RenderDepth(beatmapObject);
                }

                inst.RenderLayers(beatmapObject);
                inst.RenderBin(beatmapObject);

                inst.RenderGameObjectInspector(beatmapObject);

                bool fromPrefab = !string.IsNullOrEmpty(beatmapObject.prefabID);
                ((GameObject)inst.ObjectUIElements["Collapse Label"]).SetActive(fromPrefab);
                ((GameObject)inst.ObjectUIElements["Collapse Prefab"]).SetActive(fromPrefab);

                inst.RenderKeyframes(beatmapObject);
                inst.RenderObjectKeyframesDialog(beatmapObject);

                if (ObjectModifiersEditor.inst)
                    inst.StartCoroutine(ObjectModifiersEditor.inst.RenderModifiers(beatmapObject));
            }

            yield break;
        }

        public void RenderEmpty(BeatmapObject beatmapObject)
        {
            var active = !HideVisualElementsWhenObjectIsEmpty || beatmapObject.objectType != ObjectType.Empty;
            var shapeTF = (Transform)ObjectUIElements["Shape"];
            var shapeTFPActive = shapeTF.parent.GetChild(shapeTF.GetSiblingIndex() - 1).gameObject.activeSelf;
            shapeTF.parent.GetChild(shapeTF.GetSiblingIndex() - 1).gameObject.SetActive(active);
            shapeTF.gameObject.SetActive(active);

            ((Transform)ObjectUIElements["Shape Settings"]).gameObject.SetActive(active);
            ((Transform)ObjectUIElements["Depth"]).gameObject.SetActive(active);
            var depthTf = (Transform)ObjectUIElements["Depth T"];
            depthTf.parent.GetChild(depthTf.GetSiblingIndex() - 1).gameObject.SetActive(active);
            depthTf.gameObject.SetActive(active);

            var renderTypeTF = (Transform)ObjectUIElements["Render Type T"];
            renderTypeTF.parent.GetChild(renderTypeTF.GetSiblingIndex() - 1).gameObject.SetActive(active && RTEditor.ShowModdedUI);
            renderTypeTF.gameObject.SetActive(active && RTEditor.ShowModdedUI);

            var originTF = (Transform)ObjectUIElements["Origin"];
            originTF.parent.GetChild(originTF.GetSiblingIndex() - 1).gameObject.SetActive(active);
            originTF.gameObject.SetActive(active);

            var tagsParent = ObjEditor.inst.ObjectView.transform.Find("Tags Scroll View");
            tagsParent.parent.GetChild(tagsParent.GetSiblingIndex() - 1).gameObject.SetActive(RTEditor.ShowModdedUI);
            bool tagsActive = tagsParent.gameObject.activeSelf;
            tagsParent.gameObject.SetActive(RTEditor.ShowModdedUI);

            ((Transform)ObjectUIElements["ID Base"]).Find("ldm").gameObject.SetActive(RTEditor.ShowModdedUI);

            if (ModCompatibility.ObjectModifiersInstalled)
            {
                ObjEditor.inst.ObjectView.transform.Find("int_variable").gameObject.SetActive(RTEditor.ShowModdedUI);
                ObjEditor.inst.ObjectView.transform.Find("ignore life").gameObject.SetActive(RTEditor.ShowModdedUI);

                var activeModifiers = ObjEditor.inst.ObjectView.transform.Find("active").gameObject;

                if (!RTEditor.ShowModdedUI)
                    activeModifiers.GetComponent<Toggle>().isOn = false;

                activeModifiers.SetActive(RTEditor.ShowModdedUI);
            }

            if (active && !shapeTFPActive)
            {
                RenderOrigin(beatmapObject);
                RenderShape(beatmapObject);
                RenderDepth(beatmapObject);
            }

            if (RTEditor.ShowModdedUI && !tagsActive)
            {
                RenderIDLDM(beatmapObject);
                RenderName(beatmapObject);
            }
        }

        /// <summary>
        /// Renders the ID Text and LDM Toggle.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        public void RenderIDLDM(BeatmapObject beatmapObject)
        {
            var idText = (Text)ObjectUIElements["ID Text"];
            idText.text = $"ID: {beatmapObject.id}";

            var clickable = idText.transform.parent.gameObject.GetComponent<Clickable>();

            if (!clickable)
                clickable = idText.transform.parent.gameObject.AddComponent<Clickable>();

            clickable.onClick = delegate (PointerEventData pointerEventData)
            {
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

            var tagsParent = (Transform)ObjectUIElements["Tags Content"];

            if (RTEditor.ShowModdedUI)
            {
                LSHelpers.DeleteChildren(tagsParent);

                int num = 0;
                foreach (var tag in beatmapObject.tags)
                {
                    int index = num;
                    var gameObject = tagPrefab.Duplicate(tagsParent, index.ToString());
                    gameObject.transform.localScale = Vector3.one;
                    var input = gameObject.transform.Find("Input").GetComponent<InputField>();
                    input.onValueChanged.ClearAll();
                    input.text = tag;
                    input.onValueChanged.AddListener(delegate (string _val)
                    {
                        beatmapObject.tags[index] = _val;
                    });

                    var inputFieldSwapper = gameObject.AddComponent<InputFieldSwapper>();
                    inputFieldSwapper.Init(input, InputFieldSwapper.Type.String);

                    var delete = gameObject.transform.Find("Delete").GetComponent<Button>();
                    delete.onClick.ClearAll();
                    delete.onClick.AddListener(delegate ()
                    {
                        beatmapObject.tags.RemoveAt(index);
                        RenderName(beatmapObject);
                    });

                    num++;
                }

                var add = PrefabEditor.inst.CreatePrefab.Duplicate(tagsParent, "Add");
                add.transform.localScale = Vector3.one;
                add.transform.Find("Text").GetComponent<Text>().text = "Add Tag";
                var addButton = add.GetComponent<Button>();
                addButton.onClick.ClearAll();
                addButton.onClick.AddListener(delegate ()
                {
                    beatmapObject.tags.Add("New Tag");
                    RenderName(beatmapObject);
                });
            }
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
                RenderGameObjectInspector(beatmapObject);
                // ObjectType affects both physical object and timeline object.
                RenderTimelineObject(new TimelineObject(beatmapObject));
                if (UpdateObjects)
                    Updater.UpdateProcessor(beatmapObject);

                RenderEmpty(beatmapObject);
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
                ResizeKeyframeTimeline(beatmapObject);
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
                    if (float.TryParse(_value, out float num))
                    {
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
                        ResizeKeyframeTimeline(beatmapObject);
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

                    akOffset.text = num.ToString();

                    //beatmapObject.autoKillOffset = num;

                    //// AutoKillType affects both physical object and timeline object.
                    //RenderTimelineObject(new TimelineObject(beatmapObject));
                    //if (UpdateObjects)
                    //    Updater.UpdateProcessor(beatmapObject, "Autokill");
                    //ResizeKeyframeTimeline(beatmapObject);
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
            var parentClear = (Button)ObjectUIElements["Parent Clear"];
            var parentPicker = (Button)ObjectUIElements["Parent Picker"];

            parentText.transform.AsRT().sizeDelta = new Vector2(!string.IsNullOrEmpty(parent) ? 201f : 241f, 32f);

            parentParent.onClick.RemoveAllListeners();
            parentParent.onClick.AddListener(delegate ()
            {
                EditorManager.inst.OpenParentPopup();
            });

            parentClear.onClick.RemoveAllListeners();

            parentPicker.onClick.RemoveAllListeners();
            parentPicker.onClick.AddListener(delegate ()
            {
                RTEditor.inst.parentPickerEnabled = true;
                RTEditor.inst.objectToParent = beatmapObject;
            });

            parentClear.gameObject.SetActive(!string.IsNullOrEmpty(parent));

            parent_more.AsRT().sizeDelta = new Vector2(351f, RTEditor.ShowModdedUI ? 152f : 112f);

            if (string.IsNullOrEmpty(parent))
            {
                parentText.interactable = false;
                parentMore.interactable = false;
                parent_more.gameObject.SetActive(false);
                parentTextText.text = "No Parent Object";

                ((HoverTooltip)ObjectUIElements["Parent Info"]).tooltipLangauges[0].hint = string.IsNullOrEmpty(parent) ? "Object not parented." : "No parent found.";
                parentText.onClick.RemoveAllListeners();
                parentMore.onClick.RemoveAllListeners();

                return;
            }

            string p = null;

            if (DataManager.inst.gameData.beatmapObjects.Has(x => x.id == parent))
            {
                p = DataManager.inst.gameData.beatmapObjects.Find(x => x.id == parent).name;
                ((HoverTooltip)ObjectUIElements["Parent Info"]).tooltipLangauges[0].hint = string.Format("Parent chain count: [{0}]\n(Inclusive)", beatmapObject.GetParentChain().Count);
            }
            else if (parent == "CAMERA_PARENT")
            {
                p = "[CAMERA]";
                ((HoverTooltip)ObjectUIElements["Parent Info"]).tooltipLangauges[0].hint = "Object parented to the camera.";
            }

            parentText.interactable = p != null;
            parentMore.interactable = p != null;

            parent_more.gameObject.SetActive(p != null && ObjEditor.inst.advancedParent);

            parentClear.onClick.AddListener(delegate ()
            {
                beatmapObject.parent = "";

                // Since parent has no affect on the timeline object, we will only need to update the physical object.
                if (UpdateObjects)
                    Updater.UpdateProcessor(beatmapObject, "Parent");

                RenderParent(beatmapObject);
            });

            if (p == null)
            {
                parentTextText.text = "No Parent Object";
                ((HoverTooltip)ObjectUIElements["Parent Info"]).tooltipLangauges[0].hint = string.IsNullOrEmpty(parent) ? "Object not parented." : "No parent found.";
                parentText.onClick.RemoveAllListeners();
                parentMore.onClick.RemoveAllListeners();

                return;
            }

            parentTextText.text = p;

            parentText.onClick.RemoveAllListeners();
            parentText.onClick.AddListener(delegate ()
            {
                if (DataManager.inst.gameData.beatmapObjects.Find(x => x.id == parent) != null &&
                parent != "CAMERA_PARENT" &&
                RTEditor.inst.timelineObjects.TryFind(x => x.ID == parent, out TimelineObject timelineObject))
                    SetCurrentObject(timelineObject);
                else if (parent == "CAMERA_PARENT")
                {
                    RTEditor.inst.SetLayer(RTEditor.LayerType.Events);
                    EventEditor.inst.SetCurrentEvent(0, RTExtensions.ClosestEventKeyframe(0));
                }
            });

            parentMore.onClick.RemoveAllListeners();
            parentMore.onClick.AddListener(delegate ()
            {
                ObjEditor.inst.advancedParent = !ObjEditor.inst.advancedParent;
                parent_more.gameObject.SetActive(ObjEditor.inst.advancedParent);
            });
            parent_more.gameObject.SetActive(ObjEditor.inst.advancedParent);

            var spawnOnce = (Toggle)ObjectUIElements["Parent Spawn Once"];
            spawnOnce.onValueChanged.ClearAll();
            spawnOnce.gameObject.SetActive(RTEditor.ShowModdedUI);
            if (RTEditor.ShowModdedUI)
            {
                spawnOnce.isOn = beatmapObject.desync;
                spawnOnce.onValueChanged.AddListener(delegate (bool _val)
                {
                    beatmapObject.desync = _val;
                    Updater.UpdateProcessor(beatmapObject);
                });
            }

            for (int i = 0; i < 3; i++)
            {
                var _p = (Transform)ObjectUIElements[$"Parent Offset {i + 1}"];

                var parentOffset = beatmapObject.getParentOffset(i);

                var index = i;

                // Parent Type
                var tog = _p.GetChild(2).GetComponent<Toggle>();
                tog.onValueChanged.RemoveAllListeners();
                tog.isOn = beatmapObject.GetParentType(i);
                tog.graphic.color = new Color(0.1294f, 0.1294f, 0.1294f, 1f);
                tog.onValueChanged.AddListener(delegate (bool _value)
                {
                    beatmapObject.SetParentType(index, _value);

                    // Since updating parent type has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects && !string.IsNullOrEmpty(beatmapObject.parent) && beatmapObject.parent != "CAMERA_PARENT")
                        Updater.UpdateProcessor(beatmapObject.GetParent());
                    else if (UpdateObjects && beatmapObject.parent == "CAMERA_PARENT")
                        Updater.UpdateProcessor(beatmapObject);
                });

                // Parent Offset
                var pif = _p.GetChild(3).GetComponent<InputField>();
                var lel = _p.GetChild(3).GetComponent<LayoutElement>();
                lel.minWidth = RTEditor.ShowModdedUI ? 64f : 128f;
                lel.preferredWidth = RTEditor.ShowModdedUI ? 64f : 128f;
                pif.onValueChanged.RemoveAllListeners();
                pif.text = parentOffset.ToString();
                pif.onValueChanged.AddListener(delegate (string _value)
                {
                    if (float.TryParse(_value, out float num))
                    {
                        beatmapObject.SetParentOffset(index, num);

                        // Since updating parent type has no affect on the timeline object, we will only need to update the physical object.
                        if (UpdateObjects && !string.IsNullOrEmpty(beatmapObject.parent) && beatmapObject.parent != "CAMERA_PARENT")
                            Updater.UpdateProcessor(beatmapObject.GetParent());
                        else if (UpdateObjects && beatmapObject.parent == "CAMERA_PARENT")
                            Updater.UpdateProcessor(beatmapObject);
                    }
                });

                TriggerHelper.AddEventTrigger(pif.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(pif) });

                var additive = _p.GetChild(4).GetComponent<Toggle>();
                additive.onValueChanged.ClearAll();
                additive.gameObject.SetActive(RTEditor.ShowModdedUI);
                var parallax = _p.GetChild(5).GetComponent<InputField>();
                parallax.onValueChanged.RemoveAllListeners();
                parallax.gameObject.SetActive(RTEditor.ShowModdedUI);

                if (RTEditor.ShowModdedUI)
                {
                    additive.isOn = beatmapObject.parentAdditive[i] == '1';
                    additive.graphic.color = new Color(0.1294f, 0.1294f, 0.1294f, 1f);
                    additive.onValueChanged.AddListener(delegate (bool _val)
                    {
                        beatmapObject.SetParentAdditive(index, _val);
                        if (UpdateObjects)
                            Updater.UpdateProcessor(beatmapObject);
                    });
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

        bool updatedShapes = false;
        public List<Toggle> shapeToggles = new List<Toggle>();
        public List<List<Toggle>> shapeOptionToggles = new List<List<Toggle>>();
        public static int[] UnmoddedShapeCounts => new int[]
        {
            3,
            9,
            4,
            2,
            1,
            6
        };

        /// <summary>
        /// Renders the Shape ToggleGroup.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        public void RenderShape(BeatmapObject beatmapObject)
        {
            var shape = (Transform)ObjectUIElements["Shape"];
            var shapeSettings = (Transform)ObjectUIElements["Shape Settings"];

            var shapeGLG = shape.GetComponent<GridLayoutGroup>();
            shapeGLG.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            shapeGLG.constraintCount = 1;
            shapeGLG.spacing = new Vector2(7.6f, 0f);

            if (!updatedShapes)
            {
                // Initial removing
                DestroyImmediate(shape.GetComponent<ToggleGroup>());

                var toDestroy = new List<GameObject>();

                for (int i = 0; i < shape.childCount; i++)
                {
                    toDestroy.Add(shape.GetChild(i).gameObject);
                }

                for (int i = 0; i < shapeSettings.childCount; i++)
                {
                    if (i != 4 && i != 6)
                        for (int j = 0; j < shapeSettings.GetChild(i).childCount; j++)
                        {
                            toDestroy.Add(shapeSettings.GetChild(i).GetChild(j).gameObject);
                        }
                }

                foreach (var obj in toDestroy)
                    DestroyImmediate(obj);

                toDestroy = null;

                for (int i = 0; i < ShapeManager.inst.Shapes2D.Count; i++)
                {
                    var obj = shapeButtonPrefab.Duplicate(shape, (i + 1).ToString(), i);
                    if (obj.transform.Find("Image") && obj.transform.Find("Image").gameObject.TryGetComponent(out Image image))
                        image.sprite = ShapeManager.inst.Shapes2D[i][0].Icon;

                    if (!obj.GetComponent<HoverUI>())
                    {
                        var hoverUI = obj.AddComponent<HoverUI>();
                        hoverUI.animatePos = false;
                        hoverUI.animateSca = true;
                        hoverUI.size = 1.1f;
                    }

                    shapeToggles.Add(obj.GetComponent<Toggle>());

                    shapeOptionToggles.Add(new List<Toggle>());

                    if (i != 4 && i != 6)
                    {
                        if (!shapeSettings.Find((i + 1).ToString()))
                        {
                            var sh = shapeSettings.Find("6").gameObject.Duplicate(shapeSettings, (i + 1).ToString());
                            LSHelpers.DeleteChildren(sh.transform, true);

                            var d = new List<GameObject>();
                            for (int j = 0; j < sh.transform.childCount; j++)
                            {
                                d.Add(sh.transform.GetChild(j).gameObject);
                            }
                            foreach (var go in d)
                                DestroyImmediate(go);
                            d.Clear();
                            d = null;
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

                            if (!opt.GetComponent<HoverUI>())
                            {
                                var hoverUI = opt.AddComponent<HoverUI>();
                                hoverUI.animatePos = false;
                                hoverUI.animateSca = true;
                                hoverUI.size = 1.1f;
                            }

                            shapeOptionToggles[i].Add(opt.GetComponent<Toggle>());

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

                if (ObjectManager.inst.objectPrefabs.Count > 9)
                {
                    var playerSprite = SpriteManager.LoadSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_player.png");
                    int i = shape.childCount;
                    var obj = shapeButtonPrefab.Duplicate(shape, (i + 1).ToString());
                    if (obj.transform.Find("Image") && obj.transform.Find("Image").gameObject.TryGetComponent(out Image image))
                        image.sprite = playerSprite;

                    var so = shapeSettings.Find((i + 1).ToString());

                    if (!so)
                    {
                        so = shapeSettings.Find("6").gameObject.Duplicate(shapeSettings, (i + 1).ToString()).transform;
                        LSHelpers.DeleteChildren(so, true);

                        var d = new List<GameObject>();
                        for (int j = 0; j < so.transform.childCount; j++)
                        {
                            d.Add(so.transform.GetChild(j).gameObject);
                        }
                        foreach (var go in d)
                            DestroyImmediate(go);
                        d.Clear();
                        d = null;
                    }

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

                    shapeToggles.Add(obj.GetComponent<Toggle>());

                    shapeOptionToggles.Add(new List<Toggle>());

                    for (int j = 0; j < ObjectManager.inst.objectPrefabs[9].options.Count; j++)
                    {
                        var opt = shapeButtonPrefab.Duplicate(shapeSettings.GetChild(i), (j + 1).ToString(), j);
                        if (opt.transform.Find("Image") && opt.transform.Find("Image").gameObject.TryGetComponent(out Image image1))
                            image1.sprite = playerSprite;

                        shapeOptionToggles[i].Add(opt.GetComponent<Toggle>());

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

                updatedShapes = true;
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
                shapeSettings.AsRT().sizeDelta = new Vector2(351f, 74f);
                var child = shapeSettings.GetChild(4);
                child.AsRT().sizeDelta = new Vector2(351f, 74f);
                child.Find("Text").GetComponent<Text>().alignment = TextAnchor.UpperLeft;
                child.Find("Placeholder").GetComponent<Text>().alignment = TextAnchor.UpperLeft;
                child.GetComponent<InputField>().lineType = InputField.LineType.MultiLineNewline;
            }
            else
            {
                shapeSettings.AsRT().sizeDelta = new Vector2(351f, 32f);
                shapeSettings.GetChild(4).AsRT().sizeDelta = new Vector2(351f, 32f);
            }

            shapeSettings.GetChild(beatmapObject.shape).gameObject.SetActive(true);

            int num = 0;
            foreach (var toggle in shapeToggles)
            {
                int index = num;
                toggle.onValueChanged.ClearAll();
                toggle.isOn = beatmapObject.shape == index;
                toggle.gameObject.SetActive(RTEditor.ShowModdedUI || index < UnmoddedShapeCounts.Length);

                if (RTEditor.ShowModdedUI || index < UnmoddedShapeCounts.Length)
                    toggle.onValueChanged.AddListener(delegate (bool _val)
                    {
                        beatmapObject.shape = index;
                        beatmapObject.shapeOption = 0;
                        //beatmapObject.text = "";

                        // Since shape has no affect on the timeline object, we will only need to update the physical object.
                        if (UpdateObjects)
                            Updater.UpdateProcessor(beatmapObject, "Shape");

                        RenderShape(beatmapObject);
                    });


                num++;
            }
            
            if (beatmapObject.shape != 4 && beatmapObject.shape != 6)
            {
                num = 0;
                foreach (var toggle in shapeOptionToggles[beatmapObject.shape])
                {
                    int index = num;
                    toggle.onValueChanged.ClearAll();
                    toggle.isOn = beatmapObject.shapeOption == index;
                    toggle.gameObject.SetActive(RTEditor.ShowModdedUI || index < UnmoddedShapeCounts[beatmapObject.shape]);

                    if (RTEditor.ShowModdedUI || index < UnmoddedShapeCounts[beatmapObject.shape])
                        toggle.onValueChanged.AddListener(delegate (bool _val)
                        {
                            beatmapObject.shapeOption = index;

                            // Since shape has no affect on the timeline object, we will only need to update the physical object.
                            if (UpdateObjects)
                                Updater.UpdateProcessor(beatmapObject, "Shape");

                            RenderShape(beatmapObject);
                        });

                    num++;
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

                // Sets Image Data for transfering of Image Objects between levels.
                var dataText = shapeSettings.Find("7/set/Text").GetComponent<Text>();
                dataText.text = !AssetManager.SpriteAssets.ContainsKey(beatmapObject.text) ? "Set Data" : "Clear Data";
                var set = shapeSettings.Find("7/set").GetComponent<Button>();
                set.onClick.ClearAll();
                set.onClick.AddListener(delegate ()
                {
                    if (!AssetManager.SpriteAssets.ContainsKey(beatmapObject.text))
                    {
                        var regex = new System.Text.RegularExpressions.Regex(@"img\((.*?)\)");
                        var match = regex.Match(beatmapObject.text);

                        var path = match.Success ? RTFile.BasePath + match.Groups[1].ToString() : RTFile.BasePath + beatmapObject.text;

                        if (RTFile.FileExists(path))
                        {
                            var imageData = File.ReadAllBytes(path);

                            var texture2d = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                            texture2d.LoadImage(imageData);

                            texture2d.wrapMode = TextureWrapMode.Clamp;
                            texture2d.filterMode = FilterMode.Point;
                            texture2d.Apply();

                            AssetManager.SpriteAssets.Add(beatmapObject.text, SpriteManager.CreateSprite(texture2d));
                        }
                        else
                        {
                            var imageData = ArcadeManager.inst.defaultImage.texture.EncodeToPNG();

                            var texture2d = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                            texture2d.LoadImage(imageData);

                            texture2d.wrapMode = TextureWrapMode.Clamp;
                            texture2d.filterMode = FilterMode.Point;
                            texture2d.Apply();

                            AssetManager.SpriteAssets.Add(beatmapObject.text, SpriteManager.CreateSprite(texture2d));
                        }

                        Updater.UpdateProcessor(beatmapObject);
                    }
                    else
                    {
                        AssetManager.SpriteAssets.Remove(beatmapObject.text);

                        Updater.UpdateProcessor(beatmapObject);
                    }

                    dataText.text = !AssetManager.SpriteAssets.ContainsKey(beatmapObject.text) ? "Set Data" : "Clear Data";
                });
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

            var renderType = (Dropdown)ObjectUIElements["Render Type"];
            renderType.onValueChanged.ClearAll();
            renderType.value = beatmapObject.background ? 1 : 0;
            renderType.onValueChanged.AddListener(delegate (int _val)
            {
                beatmapObject.background = _val == 1;
                Updater.UpdateProcessor(beatmapObject);
            });
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
                var uiManager = AccessTools.TypeByName("UnityExplorer.UI.UIManager");

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

                    inspectGameObject.transform.GetChild(0).GetComponent<Text>().text = "Inspect LevelObject";
                }
                
                if (tfv.Find("inspect"))
                {
                    bool active = Updater.TryGetObject(beatmapObject, out RTFunctions.Functions.Optimization.Objects.LevelObject levelObject);
                    tfv.Find("inspect").gameObject.SetActive(active);
                    var deleteButton = tfv.Find("inspect").GetComponent<Button>();
                    deleteButton.onClick.ClearAll();
                    if (active)
                        deleteButton.onClick.AddListener(delegate ()
                        {
                            uiManager.GetProperty("ShowMenu").SetValue(uiManager, true);
                            inspector.GetMethod("Inspect", new[] { typeof(object), AccessTools.TypeByName("UnityExplorer.CacheObject.CacheObjectBase") }).Invoke(inspector, new object[] { levelObject, null });
                        });
                }

                if (tfv.Find("inspectbeatmapobject"))
                {
                    var deleteButton = tfv.Find("inspectbeatmapobject").GetComponent<Button>();
                    deleteButton.onClick.ClearAll();
                    deleteButton.onClick.AddListener(delegate ()
                    {
                        uiManager.GetProperty("ShowMenu").SetValue(uiManager, true);
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

        void KeyframeHandler(Transform kfdialog, int type, IEnumerable<TimelineObject> selected, TimelineObject firstKF, BeatmapObject beatmapObject, string typeName, int i, string valueType)
        {
            var valueBase = kfdialog.Find(typeName);
            var value = valueBase.Find(valueType);

            if (!value)
            {
                Debug.LogError($"{EditorPlugin.className}Value {valueType} is null.");
                return;
            }

            var valueEventTrigger = typeName != "rotation" ? value.GetComponent<EventTrigger>() : kfdialog.GetChild(9).GetComponent<EventTrigger>();

            var valueInputField = value.GetComponent<InputField>();
            var valueButtonLeft = value.Find("<").GetComponent<Button>();
            var valueButtonRight = value.Find(">").GetComponent<Button>();

            if (!value.GetComponent<InputFieldSwapper>())
            {
                var ifh = value.gameObject.AddComponent<InputFieldSwapper>();
                ifh.Init(valueInputField, InputFieldSwapper.Type.Num);
            }

            valueEventTrigger.triggers.Clear();
            if (type != 2)
            {
                valueEventTrigger.triggers.Add(TriggerHelper.ScrollDelta(valueInputField, multi: true));
                valueEventTrigger.triggers.Add(TriggerHelper.ScrollDeltaVector2(kfdialog.GetChild(9).GetChild(0).GetComponent<InputField>(), kfdialog.GetChild(9).GetChild(1).GetComponent<InputField>(), 0.1f, 10f));
            }
            else
            {
                valueEventTrigger.triggers.Add(TriggerHelper.ScrollDelta(valueInputField, 15f, 3f));
            }

            int current = i;

            valueInputField.characterValidation = InputField.CharacterValidation.None;
            valueInputField.contentType = InputField.ContentType.Standard;
            valueInputField.keyboardType = TouchScreenKeyboardType.Default;

            valueInputField.onValueChanged.RemoveAllListeners();
            valueInputField.text = selected.Count() == 1 ? firstKF.GetData<EventKeyframe>().eventValues[i].ToString() : typeName == "rotation" ? "15" : "1";
            valueInputField.onValueChanged.AddListener(delegate (string _value)
            {
                if (float.TryParse(_value, out float num) && selected.Count() == 1)
                {
                    firstKF.GetData<EventKeyframe>().eventValues[current] = num;

                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        Updater.UpdateProcessor(beatmapObject, "Keyframes");
                }
            });

            valueButtonLeft.onClick.RemoveAllListeners();
            valueButtonLeft.onClick.AddListener(delegate ()
            {
                if (float.TryParse(valueInputField.text, out float x))
                {
                    if (selected.Count() == 1)
                        valueInputField.text = (x - (typeName == "rotation" ? 5f : 1f)).ToString();
                    else
                    {
                        foreach (var keyframe in selected)
                        {
                            keyframe.GetData<EventKeyframe>().eventValues[current] -= x;
                        }

                        // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                        if (UpdateObjects)
                            Updater.UpdateProcessor(beatmapObject, "Keyframes");
                    }
                }
            });

            valueButtonRight.onClick.RemoveAllListeners();
            valueButtonRight.onClick.AddListener(delegate ()
            {
                if (float.TryParse(valueInputField.text, out float x))
                {
                    if (selected.Count() == 1)
                        valueInputField.text = (x + (typeName == "rotation" ? 5f : 1f)).ToString();
                    else
                    {
                        foreach (var keyframe in selected)
                        {
                            keyframe.GetData<EventKeyframe>().eventValues[current] += x;
                        }

                        // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                        if (UpdateObjects)
                            Updater.UpdateProcessor(beatmapObject, "Keyframes");
                    }
                }
            });
        }

        void UpdateKeyframeRandomDialog(Transform kfdialog, Transform randomValueLabel, Transform randomValue, int type,
            IEnumerable<TimelineObject> selected, TimelineObject firstKF, BeatmapObject beatmapObject, string typeName, int randomType)
        {
            if (kfdialog.Find("r_axis"))
                kfdialog.Find("r_axis").gameObject.SetActive(RTEditor.ShowModdedUI && (randomType == 5 || randomType == 6));

            randomValueLabel.gameObject.SetActive(randomType != 0 && randomType != 5);
            randomValue.gameObject.SetActive(randomType != 0 && randomType != 5);
            randomValueLabel.GetChild(0).GetComponent<Text>().text = (randomType == 4) ? "Random Scale Min" : randomType == 6 ? "Minimum Range" : "Random X";
            randomValueLabel.GetChild(1).gameObject.SetActive(type != 2 || randomType == 6);
            randomValueLabel.GetChild(1).GetComponent<Text>().text = (randomType == 4) ? "Random Scale Max" : randomType == 6 ? "Maximum Range" : "Random Y";
            kfdialog.Find("random/interval-input").gameObject.SetActive(randomType != 0 && randomType != 3 && randomType != 5);
            kfdialog.Find("r_label/interval").gameObject.SetActive(randomType != 0 && randomType != 3 && randomType != 5);

            if (kfdialog.Find("relative-label"))
            {
                kfdialog.Find("relative-label").gameObject.SetActive(RTEditor.ShowModdedUI);
                if (RTEditor.ShowModdedUI)
                {
                    kfdialog.Find("relative-label").GetChild(0).GetComponent<Text>().text =
                        randomType == 6 && type != 2 ? "Object Flees from Player" : randomType == 6 ? "Object Turns Away from Player" : "Value Additive";
                    kfdialog.Find("relative").GetChild(1).GetComponent<Text>().text =
                        randomType == 6 && type != 2 ? "Flee" : randomType == 6 ? "Turn Away" : "Relative";
                }
            }

            randomValue.GetChild(1).gameObject.SetActive(type != 2 || randomType == 6);

            randomValue.GetChild(0).GetChild(0).AsRT().sizeDelta = new Vector2(type != 2 || randomType == 6 ? 117 : 317f, 32f);
            randomValue.GetChild(1).GetChild(0).AsRT().sizeDelta = new Vector2(type != 2 || randomType == 6 ? 117 : 317f, 32f);

            if (randomType != 0 && randomType != 3 && randomType != 5)
            {
                kfdialog.Find("r_label/interval").GetComponent<Text>().text = randomType == 6 ? "Speed" : "Random Interval";
            }
        }

        void KeyframeRandomHandler(Transform kfdialog, int type, IEnumerable<TimelineObject> selected, TimelineObject firstKF, BeatmapObject beatmapObject, string typeName)
        {
            var randomValueLabel = kfdialog.Find($"r_{typeName}_label");
            var randomValue = kfdialog.Find($"r_{typeName}");

            int random = firstKF.GetData<EventKeyframe>().random;

            if (kfdialog.Find("r_axis") && kfdialog.Find("r_axis").gameObject.TryGetComponent(out Dropdown rAxis))
            {
                rAxis.gameObject.SetActive(random == 5 || random == 6);
                rAxis.onValueChanged.ClearAll();
                rAxis.value = Mathf.Clamp((int)firstKF.GetData<EventKeyframe>().eventRandomValues[3], 0, 3);
                rAxis.onValueChanged.AddListener(delegate (int _val)
                {
                    foreach (var keyframe in selected.Select(x => x.GetData<EventKeyframe>()))
                        keyframe.eventRandomValues[3] = _val;
                    Updater.UpdateProcessor(beatmapObject, "Keyframes");
                });
            }

            for (int n = 0; n <= (type == 0 ? 5 : type == 2 ? 4 : 3); n++)
            {
                // We skip the 2nd random type for compatibility with old PA levels.
                int buttonTmp = (n >= 2 && (type != 2 || n < 3)) ? (n + 1) : (n > 2 && type == 2) ? n + 2 : n;

                var randomToggles = kfdialog.Find("random");

                randomToggles.GetChild(n).gameObject.SetActive(buttonTmp != 5 && buttonTmp != 6 || RTEditor.ShowModdedUI);

                if (buttonTmp != 5 && buttonTmp != 6 || RTEditor.ShowModdedUI)
                {
                    var toggle = randomToggles.GetChild(n).GetComponent<Toggle>();
                    toggle.onValueChanged.ClearAll();
                    toggle.isOn = random == buttonTmp;
                    toggle.onValueChanged.AddListener(delegate (bool _val)
                    {
                        if (_val)
                        {
                            foreach (var keyframe in selected.Select(x => x.GetData<EventKeyframe>()))
                                keyframe.random = buttonTmp;

                            // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                            if (UpdateObjects)
                                Updater.UpdateProcessor(beatmapObject, "Keyframes");
                        }

                        UpdateKeyframeRandomDialog(kfdialog, randomValueLabel, randomValue, type, selected, firstKF, beatmapObject, typeName, buttonTmp);
                    });
                    if (!toggle.GetComponent<HoverUI>())
                    {
                        var hoverUI = toggle.gameObject.AddComponent<HoverUI>();
                        hoverUI.animatePos = false;
                        hoverUI.animateSca = true;
                        hoverUI.size = 1.1f;
                    }
                }
            }

            UpdateKeyframeRandomDialog(kfdialog, randomValueLabel, randomValue, type, selected, firstKF, beatmapObject, typeName, random);

            float num = 0f;
            if (firstKF.GetData<EventKeyframe>().eventRandomValues.Length > 2)
                num = firstKF.GetData<EventKeyframe>().eventRandomValues[2];

            var randomInterval = kfdialog.Find("random/interval-input");
            var randomIntervalIF = randomInterval.GetComponent<InputField>();
            randomIntervalIF.NewValueChangedListener(num.ToString(), delegate (string _val)
            {
                if (float.TryParse(_val, out float num))
                {
                    foreach (var keyframe in selected.Select(x => x.GetData<EventKeyframe>()))
                        keyframe.eventRandomValues[2] = num;

                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        Updater.UpdateProcessor(beatmapObject, "Keyframes");
                }
            });

            TriggerHelper.AddEventTriggerParams(randomIntervalIF.gameObject,
                TriggerHelper.ScrollDelta(randomIntervalIF, 0.01f));

            if (!randomInterval.GetComponent<InputFieldSwapper>())
            {
                var ifh = randomInterval.gameObject.AddComponent<InputFieldSwapper>();
                ifh.Init(randomIntervalIF, InputFieldSwapper.Type.Num);
            }

            TriggerHelper.AddEventTrigger(randomInterval.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(randomIntervalIF, max: random == 6 ? 1f : 0f) });
        }

        void KeyframeRandomValueHandler(Transform kfdialog, int type, IEnumerable<TimelineObject> selected, TimelineObject firstKF, BeatmapObject beatmapObject, string typeName, int i, string valueType)
        {
            var randomValueLabel = kfdialog.Find($"r_{typeName}_label");
            var randomValueBase = kfdialog.Find($"r_{typeName}");

            if (!randomValueBase)
            {
                Debug.LogError($"{EditorPlugin.className}Value {valueType} (Base) is null.");
                return;
            }

            var randomValue = randomValueBase.Find(valueType);

            if (!randomValue)
            {
                Debug.LogError($"{EditorPlugin.className}Value {valueType} is null.");
                return;
            }

            var random = firstKF.GetData<EventKeyframe>().random;

            var valueButtonLeft = randomValue.Find("<").GetComponent<Button>();
            var valueButtonRight = randomValue.Find(">").GetComponent<Button>();

            var randomValueInputField = randomValue.GetComponent<InputField>();

            randomValueInputField.characterValidation = InputField.CharacterValidation.None;
            randomValueInputField.contentType = InputField.ContentType.Standard;
            randomValueInputField.keyboardType = TouchScreenKeyboardType.Default;

            randomValueInputField.NewValueChangedListener(selected.Count() == 1 ? firstKF.GetData<EventKeyframe>().eventRandomValues[i].ToString() : typeName == "rotation" ? "15" : "1", delegate (string _val)
            {
                if (float.TryParse(_val, out float num) && selected.Count() == 1)
                {
                    firstKF.GetData<EventKeyframe>().eventRandomValues[i] = num;

                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        Updater.UpdateProcessor(beatmapObject, "Keyframes");
                }
            });

            valueButtonLeft.onClick.RemoveAllListeners();
            valueButtonLeft.onClick.AddListener(delegate ()
            {
                if (float.TryParse(randomValueInputField.text, out float x))
                {
                    if (selected.Count() == 1)
                        randomValueInputField.text = (x - (typeName == "rotation" ? 15f : 1f)).ToString();
                    else
                    {
                        foreach (var keyframe in selected)
                        {
                            keyframe.GetData<EventKeyframe>().eventRandomValues[i] -= x;
                        }

                        // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                        if (UpdateObjects)
                            Updater.UpdateProcessor(beatmapObject, "Keyframes");
                    }
                }
            });

            valueButtonRight.onClick.RemoveAllListeners();
            valueButtonRight.onClick.AddListener(delegate ()
            {
                if (float.TryParse(randomValueInputField.text, out float x))
                {
                    if (selected.Count() == 1)
                        randomValueInputField.text = (x + (typeName == "rotation" ? 15f : 1f)).ToString();
                    else
                    {
                        foreach (var keyframe in selected)
                        {
                            keyframe.GetData<EventKeyframe>().eventRandomValues[i] += x;
                        }

                        // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                        if (UpdateObjects)
                            Updater.UpdateProcessor(beatmapObject, "Keyframes");
                    }
                }
            });

            TriggerHelper.AddEventTriggerParams(randomValue.gameObject,
                TriggerHelper.ScrollDelta(randomValueInputField, type == 2 && random != 6 ? 15f : 0.1f, type == 2 && random != 6 ? 3f : 10f, multi: true),
                TriggerHelper.ScrollDeltaVector2(randomValueInputField, randomValueBase.GetChild(1).GetComponent<InputField>(), type == 2 && random != 6 ? 15f : 0.1f, type == 2 && random != 6 ? 3f : 10f));

            if (!randomValue.GetComponent<InputFieldSwapper>())
            {
                var ifh = randomValue.gameObject.AddComponent<InputFieldSwapper>();
                ifh.Init(randomValueInputField, InputFieldSwapper.Type.Num);
            }
        }

        public void RenderObjectKeyframesDialog(BeatmapObject beatmapObject)
        {
            var selected = beatmapObject.timelineObject.InternalSelections.Where(x => x.selected);

            for (int i = 0; i < ObjEditor.inst.KeyframeDialogs.Count; i++)
                ObjEditor.inst.KeyframeDialogs[i].SetActive(false);

            if (selected.Count() < 1)
            {
                return;
            }

            if (!(selected.Count() == 1 || selected.All(x => x.Type == selected.Min(y => y.Type))))
            {
                ObjEditor.inst.KeyframeDialogs[4].SetActive(true);

                try
                {
                    var dialog = ObjEditor.inst.KeyframeDialogs[4].transform;
                    var time = dialog.Find("time/time/time").GetComponent<InputField>();
                    time.onValueChanged.ClearAll();
                    if (time.text == "100.000")
                        time.text = "10";

                    var setTime = dialog.Find("time/time").GetChild(3).GetComponent<Button>();
                    setTime.onClick.ClearAll();
                    setTime.onClick.AddListener(delegate ()
                    {
                        if (float.TryParse(time.text, out float num))
                        {
                            if (num < 0f)
                                num = 0f;

                            if (EditorConfig.Instance.RoundToNearest.Value)
                                num = RTMath.RoundToNearestDecimal(num, 3);

                            foreach (var kf in selected.Where(x => x.Index != 0))
                            {
                                kf.Time = num;
                            }

                            ResizeKeyframeTimeline(beatmapObject);

                            RenderKeyframes(beatmapObject);

                            // Keyframe Time affects both physical object and timeline object.
                            RenderTimelineObject(new TimelineObject(beatmapObject));
                            if (UpdateObjects)
                                Updater.UpdateProcessor(beatmapObject, "Keyframes");
                        }
                    });

                    var decreaseTimeGreat = dialog.Find("time/time/<<").GetComponent<Button>();
                    var decreaseTime = dialog.Find("time/time/<").GetComponent<Button>();
                    var increaseTimeGreat = dialog.Find("time/time/>>").GetComponent<Button>();
                    var increaseTime = dialog.Find("time/time/>").GetComponent<Button>();

                    decreaseTime.onClick.ClearAll();
                    decreaseTime.onClick.AddListener(delegate ()
                    {
                        if (float.TryParse(time.text, out float num))
                        {
                            if (num < 0f)
                                num = 0f;

                            if (EditorConfig.Instance.RoundToNearest.Value)
                                num = RTMath.RoundToNearestDecimal(num, 3);

                            foreach (var kf in selected.Where(x => x.Index != 0))
                            {
                                kf.Time = Mathf.Clamp(kf.Time - num, 0f, float.MaxValue);
                            }

                            ResizeKeyframeTimeline(beatmapObject);

                            RenderKeyframes(beatmapObject);

                            // Keyframe Time affects both physical object and timeline object.
                            RenderTimelineObject(new TimelineObject(beatmapObject));
                            if (UpdateObjects)
                                Updater.UpdateProcessor(beatmapObject, "Keyframes");
                        }
                    });

                    increaseTime.onClick.ClearAll();
                    increaseTime.onClick.AddListener(delegate ()
                    {
                        if (float.TryParse(time.text, out float num))
                        {
                            if (num < 0f)
                                num = 0f;

                            if (EditorConfig.Instance.RoundToNearest.Value)
                                num = RTMath.RoundToNearestDecimal(num, 3);

                            foreach (var kf in selected.Where(x => x.Index != 0))
                            {
                                kf.Time = Mathf.Clamp(kf.Time + num, 0f, float.MaxValue);
                            }

                            ResizeKeyframeTimeline(beatmapObject);

                            RenderKeyframes(beatmapObject);

                            // Keyframe Time affects both physical object and timeline object.
                            RenderTimelineObject(new TimelineObject(beatmapObject));
                            if (UpdateObjects)
                                Updater.UpdateProcessor(beatmapObject, "Keyframes");
                        }
                    });

                    decreaseTimeGreat.onClick.ClearAll();
                    decreaseTimeGreat.onClick.AddListener(delegate ()
                    {
                        if (float.TryParse(time.text, out float num))
                        {
                            if (num < 0f)
                                num = 0f;

                            if (EditorConfig.Instance.RoundToNearest.Value)
                                num = RTMath.RoundToNearestDecimal(num, 3);

                            foreach (var kf in selected.Where(x => x.Index != 0))
                            {
                                kf.Time = Mathf.Clamp(kf.Time - (num * 10f), 0f, float.MaxValue);
                            }

                            ResizeKeyframeTimeline(beatmapObject);

                            RenderKeyframes(beatmapObject);

                            // Keyframe Time affects both physical object and timeline object.
                            RenderTimelineObject(new TimelineObject(beatmapObject));
                            if (UpdateObjects)
                                Updater.UpdateProcessor(beatmapObject, "Keyframes");
                        }
                    });

                    increaseTimeGreat.onClick.ClearAll();
                    increaseTimeGreat.onClick.AddListener(delegate ()
                    {
                        if (float.TryParse(time.text, out float num))
                        {
                            if (num < 0f)
                                num = 0f;

                            if (EditorConfig.Instance.RoundToNearest.Value)
                                num = RTMath.RoundToNearestDecimal(num, 3);

                            foreach (var kf in selected.Where(x => x.Index != 0))
                            {
                                kf.Time = Mathf.Clamp(kf.Time + (num * 10f), 0f, float.MaxValue);
                            }

                            ResizeKeyframeTimeline(beatmapObject);

                            RenderKeyframes(beatmapObject);

                            // Keyframe Time affects both physical object and timeline object.
                            RenderTimelineObject(new TimelineObject(beatmapObject));
                            if (UpdateObjects)
                                Updater.UpdateProcessor(beatmapObject, "Keyframes");
                        }
                    });

                    TriggerHelper.AddEventTriggerParams(time.gameObject, TriggerHelper.ScrollDelta(time));

                    var curvesMulti = dialog.Find("curves/curves").GetComponent<Dropdown>();
                    curvesMulti.onValueChanged.ClearAll();
                    curvesMulti.onValueChanged.AddListener(delegate (int _val)
                    {
                        if (DataManager.inst.AnimationListDictionary.ContainsKey(_val))
                        {
                            foreach (var keyframe in selected.Where(x => x.Index != 0).Select(x => x.GetData<EventKeyframe>()))
                            {
                                keyframe.curveType = DataManager.inst.AnimationListDictionary[_val];
                            }

                            ResizeKeyframeTimeline(beatmapObject);

                            RenderKeyframes(beatmapObject);

                            // Keyframe Time affects both physical object and timeline object.
                            RenderTimelineObject(new TimelineObject(beatmapObject));
                            if (UpdateObjects)
                                Updater.UpdateProcessor(beatmapObject, "Keyframes");
                        }
                    });

                    var valueIndex = dialog.Find("value base/value index").GetComponent<InputField>();
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

                    var value = dialog.Find("value base/value").GetComponent<InputField>();
                    value.onValueChanged.ClearAll();
                    value.onValueChanged.AddListener(delegate (string _val)
                    {
                        if (!float.TryParse(_val, out float n))
                            value.text = "0";
                    });

                    var setValue = value.transform.GetChild(2).GetComponent<Button>();
                    setValue.onClick.ClearAll();
                    setValue.onClick.AddListener(delegate ()
                    {
                        if (float.TryParse(value.text, out float num))
                        {
                            foreach (var kf in selected)
                            {
                                var keyframe = kf.GetData<EventKeyframe>();

                                var index = Parser.TryParse(valueIndex.text, 0);

                                index = Mathf.Clamp(index, 0, keyframe.eventValues.Length - 1);
                                if (index >= 0 && index < keyframe.eventValues.Length)
                                    keyframe.eventValues[index] = kf.Type == 3 ? Mathf.Clamp((int)num, 0, RTHelpers.BeatmapTheme.objectColors.Count - 1) : num;
                            }

                            if (UpdateObjects)
                                Updater.UpdateProcessor(beatmapObject, "Keyframes");
                        }
                    });

                    TriggerHelper.IncreaseDecreaseButtons(value);
                    TriggerHelper.AddEventTriggerParams(value.gameObject, TriggerHelper.ScrollDelta(value));

                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }

                return;
            }

            var firstKF = selected.ElementAt(0);
            var type = firstKF.Type;

            Debug.Log($"{EditorPlugin.className}Selected Keyframe:\nID - {firstKF.ID}\nType: {firstKF.Type}\nIndex {firstKF.Index}");

            ObjEditor.inst.KeyframeDialogs[type].SetActive(true);

            ObjEditor.inst.currentKeyframeKind = type;
            ObjEditor.inst.currentKeyframe = firstKF.Index;

            var kfdialog = ObjEditor.inst.KeyframeDialogs[type].transform;

            var timeDecreaseGreat = kfdialog.Find("time/<<").GetComponent<Button>();
            var timeDecrease = kfdialog.Find("time/<").GetComponent<Button>();
            var timeIncrease = kfdialog.Find("time/>").GetComponent<Button>();
            var timeIncreaseGreat = kfdialog.Find("time/>>").GetComponent<Button>();
            var timeSet = kfdialog.Find("time/time").GetComponent<InputField>();

            timeDecreaseGreat.interactable = firstKF.Index != 0;
            timeDecrease.interactable = firstKF.Index != 0;
            timeIncrease.interactable = firstKF.Index != 0;
            timeIncreaseGreat.interactable = firstKF.Index != 0;
            timeSet.interactable = firstKF.Index != 0;

            var superLeft = kfdialog.Find("edit/<<").GetComponent<Button>();

            superLeft.onClick.RemoveAllListeners();
            superLeft.interactable = firstKF.Index != 0;
            superLeft.onClick.AddListener(delegate ()
            {
                SetCurrentKeyframe(beatmapObject, 0, true);
            });

            var left = kfdialog.Find("edit/<").GetComponent<Button>();

            left.onClick.RemoveAllListeners();
            left.interactable = selected.Count() == 1 && firstKF.Index != 0;
            left.onClick.AddListener(delegate ()
            {
                AddCurrentKeyframe(beatmapObject, -1, true);
            });

            kfdialog.Find("edit/|").GetComponentInChildren<Text>().text = firstKF.Index == 0 ? "S" : firstKF.Index == beatmapObject.events[firstKF.Type].Count - 1 ? "E" : firstKF.Index.ToString();

            var right = kfdialog.Find("edit/>").GetComponent<Button>();

            right.onClick.RemoveAllListeners();
            right.interactable = selected.Count() == 1 && firstKF.Index < beatmapObject.events[type].Count - 1;
            right.onClick.AddListener(delegate ()
            {
                AddCurrentKeyframe(beatmapObject, 1, true);
            });

            var superRight = kfdialog.Find("edit/>>").GetComponent<Button>();

            superRight.onClick.RemoveAllListeners();
            superRight.interactable = selected.Count() == 1 && firstKF.Index < beatmapObject.events[type].Count - 1;
            superRight.onClick.AddListener(delegate ()
            {
                AddCurrentKeyframe(beatmapObject, int.MaxValue, true);
            });

            var copy = kfdialog.Find("edit/copy").GetComponent<Button>();
            copy.onClick.ClearAll();
            copy.onClick.AddListener(delegate ()
            {
                switch (type)
                {
                    case 0:
                        CopiedPositionData = EventKeyframe.DeepCopy(firstKF.GetData<EventKeyframe>());
                        break;
                    case 1:
                        CopiedScaleData = EventKeyframe.DeepCopy(firstKF.GetData<EventKeyframe>());
                        break;
                    case 2:
                        CopiedRotationData = EventKeyframe.DeepCopy(firstKF.GetData<EventKeyframe>());
                        break;
                    case 3:
                        CopiedColorData = EventKeyframe.DeepCopy(firstKF.GetData<EventKeyframe>());
                        break;
                }
                EditorManager.inst.DisplayNotification("Copied keyframe data!", 2f, EditorManager.NotificationType.Success);
            });

            var paste = kfdialog.Find("edit/paste").GetComponent<Button>();
            paste.onClick.ClearAll();
            paste.onClick.AddListener(delegate ()
            {
                switch (type)
                {
                    case 0:
                        if (CopiedPositionData != null)
                        {
                            foreach (var timelineObject in selected)
                            {
                                var kf = timelineObject.GetData<EventKeyframe>();
                                kf.curveType = CopiedPositionData.curveType;
                                kf.eventValues = CopiedPositionData.eventValues.Copy();
                                kf.eventRandomValues = CopiedPositionData.eventRandomValues.Copy();
                                kf.random = CopiedPositionData.random;
                                kf.relative = CopiedPositionData.relative;
                            }

                            RenderKeyframes(beatmapObject);
                            RenderObjectKeyframesDialog(beatmapObject);
                            Updater.UpdateProcessor(beatmapObject, "Keyframes");
                            EditorManager.inst.DisplayNotification("Pasted position keyframe data to current selected keyframe.", 2f, EditorManager.NotificationType.Success);
                        }
                        else
                            EditorManager.inst.DisplayNotification("Position keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                        break;
                    case 1:
                        if (CopiedScaleData != null)
                        {
                            foreach (var timelineObject in selected)
                            {
                                var kf = timelineObject.GetData<EventKeyframe>();
                                kf.curveType = CopiedScaleData.curveType;
                                kf.eventValues = CopiedScaleData.eventValues.Copy();
                                kf.eventRandomValues = CopiedScaleData.eventRandomValues.Copy();
                                kf.random = CopiedScaleData.random;
                                kf.relative = CopiedScaleData.relative;
                            }

                            RenderKeyframes(beatmapObject);
                            RenderObjectKeyframesDialog(beatmapObject);
                            Updater.UpdateProcessor(beatmapObject, "Keyframes");
                            EditorManager.inst.DisplayNotification("Pasted scale keyframe data to current selected keyframe.", 2f, EditorManager.NotificationType.Success);
                        }
                        else
                            EditorManager.inst.DisplayNotification("Scale keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                        break;
                    case 2:
                        if (CopiedRotationData != null)
                        {
                            foreach (var timelineObject in selected)
                            {
                                var kf = timelineObject.GetData<EventKeyframe>();
                                kf.curveType = CopiedRotationData.curveType;
                                kf.eventValues = CopiedRotationData.eventValues.Copy();
                                kf.eventRandomValues = CopiedRotationData.eventRandomValues.Copy();
                                kf.random = CopiedRotationData.random;
                                kf.relative = CopiedRotationData.relative;
                            }

                            RenderKeyframes(beatmapObject);
                            RenderObjectKeyframesDialog(beatmapObject);
                            Updater.UpdateProcessor(beatmapObject, "Keyframes");
                            EditorManager.inst.DisplayNotification("Pasted rotation keyframe data to current selected keyframe.", 2f, EditorManager.NotificationType.Success);
                        }
                        else
                            EditorManager.inst.DisplayNotification("Rotation keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                        break;
                    case 3:
                        if (CopiedColorData != null)
                        {
                            foreach (var timelineObject in selected)
                            {
                                var kf = timelineObject.GetData<EventKeyframe>();
                                kf.curveType = CopiedColorData.curveType;
                                kf.eventValues = CopiedColorData.eventValues.Copy();
                                kf.eventRandomValues = CopiedColorData.eventRandomValues.Copy();
                                kf.random = CopiedColorData.random;
                                kf.relative = CopiedColorData.relative;
                            }

                            RenderKeyframes(beatmapObject);
                            RenderObjectKeyframesDialog(beatmapObject);
                            Updater.UpdateProcessor(beatmapObject, "Keyframes");
                            EditorManager.inst.DisplayNotification("Pasted color keyframe data to current selected keyframe.", 2f, EditorManager.NotificationType.Success);
                        }
                        else
                            EditorManager.inst.DisplayNotification("Color keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                        break;
                }
            });

            var deleteKey = kfdialog.Find("edit/del").GetComponent<Button>();

            deleteKey.onClick.RemoveAllListeners();
            deleteKey.onClick.AddListener(delegate ()
            {
                StartCoroutine(DeleteKeyframes(beatmapObject));
            });

            var tet = kfdialog.Find("time").GetComponent<EventTrigger>();
            var tif = kfdialog.Find("time/time").GetComponent<InputField>();

            tet.triggers.Clear();
            if (selected.Count() == 1 && firstKF.Index != 0 || selected.Count() > 1)
                tet.triggers.Add(TriggerHelper.ScrollDelta(tif));

            tif.onValueChanged.RemoveAllListeners();
            tif.text = selected.Count() == 1 ? firstKF.Time.ToString() : "1";
            tif.onValueChanged.AddListener(delegate (string _value)
            {
                if (float.TryParse(_value, out float num) && !ObjEditor.inst.timelineKeyframesDrag && selected.Count() == 1)
                {
                    if (num < 0f)
                        num = 0f;

                    if (EditorConfig.Instance.RoundToNearest.Value)
                        num = RTMath.RoundToNearestDecimal(num, 3);

                    firstKF.Time = num;

                    ResizeKeyframeTimeline(beatmapObject);

                    RenderKeyframes(beatmapObject);

                    // Keyframe Time affects both physical object and timeline object.
                    RenderTimelineObject(new TimelineObject(beatmapObject));
                    if (UpdateObjects)
                        Updater.UpdateProcessor(beatmapObject, "Keyframes");
                }
            });

            if (selected.Count() == 1)
                TriggerHelper.IncreaseDecreaseButtons(tif, t: kfdialog.Find("time"));
            else
            {
                var btR = kfdialog.Find("time/<").GetComponent<Button>();
                var btL = kfdialog.Find("time/>").GetComponent<Button>();
                var btGR = kfdialog.Find("time/<<").GetComponent<Button>();
                var btGL = kfdialog.Find("time/>>").GetComponent<Button>();

                btR.onClick.ClearAll();
                btR.onClick.AddListener(delegate ()
                {
                    if (float.TryParse(tif.text, out float result))
                    {
                        var num = Input.GetKey(KeyCode.LeftAlt) ? 0.1f / 10f : Input.GetKey(KeyCode.LeftControl) ? 0.1f * 10f : 0.1f;
                        result -= num;

                        if (selected.Count() == 1)
                            tif.text = result.ToString();
                        else
                        {
                            foreach (var keyframe in selected)
                            {
                                keyframe.Time = Mathf.Clamp(keyframe.Time - num, 0.001f, float.PositiveInfinity);
                            }
                        }
                    }
                });

                btL.onClick.ClearAll();
                btL.onClick.AddListener(delegate ()
                {
                    if (float.TryParse(tif.text, out float result))
                    {
                        var num = Input.GetKey(KeyCode.LeftAlt) ? 0.1f / 10f : Input.GetKey(KeyCode.LeftControl) ? 0.1f * 10f : 0.1f;
                        result += num;

                        if (selected.Count() == 1)
                            tif.text = result.ToString();
                        else
                        {
                            foreach (var keyframe in selected)
                            {
                                keyframe.Time = Mathf.Clamp(keyframe.Time + num, 0.001f, float.PositiveInfinity);
                            }
                        }
                    }
                });

                btGR.onClick.ClearAll();
                btGR.onClick.AddListener(delegate ()
                {
                    if (float.TryParse(tif.text, out float result))
                    {
                        var num = (Input.GetKey(KeyCode.LeftAlt) ? 0.1f / 10f : Input.GetKey(KeyCode.LeftControl) ? 0.1f * 10f : 0.1f) * 10f;
                        result -= num;

                        if (selected.Count() == 1)
                            tif.text = result.ToString();
                        else
                        {
                            foreach (var keyframe in selected)
                            {
                                keyframe.Time = Mathf.Clamp(keyframe.Time - num, 0.001f, float.PositiveInfinity);
                            }
                        }
                    }
                });

                btGL.onClick.ClearAll();
                btGL.onClick.AddListener(delegate ()
                {
                    if (float.TryParse(tif.text, out float result))
                    {
                        var num = (Input.GetKey(KeyCode.LeftAlt) ? 0.1f / 10f : Input.GetKey(KeyCode.LeftControl) ? 0.1f * 10f : 0.1f) * 10f;
                        result += num;

                        if (selected.Count() == 1)
                            tif.text = result.ToString();
                        else
                        {
                            foreach (var keyframe in selected)
                            {
                                keyframe.Time = Mathf.Clamp(keyframe.Time + num, 0.001f, float.PositiveInfinity);
                            }
                        }
                    }
                });
            }

            kfdialog.Find("curves_label").gameObject.SetActive(selected.Count() == 1 && firstKF.Index != 0 || selected.Count() > 1);
            kfdialog.Find("curves").gameObject.SetActive(selected.Count() == 1 && firstKF.Index != 0 || selected.Count() > 1);
            var curves = kfdialog.Find("curves").GetComponent<Dropdown>();
            curves.onValueChanged.RemoveAllListeners();

            if (DataManager.inst.AnimationListDictionaryBack.ContainsKey(firstKF.GetData<EventKeyframe>().curveType))
            {
                curves.value = DataManager.inst.AnimationListDictionaryBack[firstKF.GetData<EventKeyframe>().curveType];
            }

            curves.onValueChanged.AddListener(delegate (int _value)
            {
                foreach (var keyframe in selected.Select(x => x.GetData<EventKeyframe>()))
                {
                    keyframe.curveType = DataManager.inst.AnimationListDictionary[_value];
                }

                // Since keyframe curve has no affect on the timeline object, we will only need to update the physical object.
                if (UpdateObjects)
                    Updater.UpdateProcessor(beatmapObject, "Keyframes");
                RenderKeyframes(beatmapObject);
            });

            switch (type)
            {
                case 0:
                    {
                        KeyframeHandler(kfdialog, type, selected, firstKF, beatmapObject, "position", 0, "x");
                        KeyframeHandler(kfdialog, type, selected, firstKF, beatmapObject, "position", 1, "y");
                        KeyframeHandler(kfdialog, type, selected, firstKF, beatmapObject, "position", 2, "z");

                        KeyframeRandomHandler(kfdialog, type, selected, firstKF, beatmapObject, "position");
                        KeyframeRandomValueHandler(kfdialog, type, selected, firstKF, beatmapObject, "position", 0, "x");
                        KeyframeRandomValueHandler(kfdialog, type, selected, firstKF, beatmapObject, "position", 1, "y");

                        break;
                    }
                case 1:
                    {
                        KeyframeHandler(kfdialog, type, selected, firstKF, beatmapObject, "scale", 0, "x");
                        KeyframeHandler(kfdialog, type, selected, firstKF, beatmapObject, "scale", 1, "y");

                        KeyframeRandomHandler(kfdialog, type, selected, firstKF, beatmapObject, "scale");
                        KeyframeRandomValueHandler(kfdialog, type, selected, firstKF, beatmapObject, "scale", 0, "x");
                        KeyframeRandomValueHandler(kfdialog, type, selected, firstKF, beatmapObject, "scale", 1, "y");

                        break;
                    }
                case 2:
                    {
                        KeyframeHandler(kfdialog, type, selected, firstKF, beatmapObject, "rotation", 0, "x");

                        KeyframeRandomHandler(kfdialog, type, selected, firstKF, beatmapObject, "rotation");
                        KeyframeRandomValueHandler(kfdialog, type, selected, firstKF, beatmapObject, "rotation", 0, "x");
                        KeyframeRandomValueHandler(kfdialog, type, selected, firstKF, beatmapObject, "rotation", 1, "y");

                        break;
                    }
                case 3:
                    {
                        bool showModifiedColors = EditorConfig.Instance.ShowModifiedColors.Value;
                        int index = 0;
                        foreach (var toggle in ObjEditor.inst.colorButtons)
                        {
                            toggle.onValueChanged.RemoveAllListeners();
                            int tmpIndex = index;

                            toggle.gameObject.SetActive(RTEditor.ShowModdedUI || tmpIndex < 9);

                            toggle.onValueChanged.ClearAll();
                            toggle.isOn = index == firstKF.GetData<EventKeyframe>().eventValues[0];
                            toggle.onValueChanged.AddListener(delegate (bool _val)
                            {
                                SetKeyframeColor(beatmapObject, 0, tmpIndex, selected);
                            });

                            if (showModifiedColors)
                            {
                                var color = RTHelpers.BeatmapTheme.GetObjColor(tmpIndex);

                                float hueNum = beatmapObject.Interpolate(type, 2);
                                float satNum = beatmapObject.Interpolate(type, 3);
                                float valNum = beatmapObject.Interpolate(type, 4);

                                toggle.image.color = RTHelpers.ChangeColorHSV(color, hueNum, satNum, valNum);
                            }
                            else
                                toggle.image.color = RTHelpers.BeatmapTheme.GetObjColor(tmpIndex);

                            if (!toggle.GetComponent<HoverUI>())
                            {
                                var hoverUI = toggle.gameObject.AddComponent<HoverUI>();
                                hoverUI.animatePos = false;
                                hoverUI.animateSca = true;
                                hoverUI.size = 1.1f;
                            }
                            index++;
                        }

                        var random = firstKF.GetData<EventKeyframe>().random;

                        kfdialog.Find("opacity_label").gameObject.SetActive(RTEditor.ShowModdedUI);
                        kfdialog.Find("opacity").gameObject.SetActive(RTEditor.ShowModdedUI);

                        kfdialog.Find("huesatval_label").gameObject.SetActive(RTEditor.ShowModdedUI);
                        kfdialog.Find("huesatval").gameObject.SetActive(RTEditor.ShowModdedUI);

                        kfdialog.Find("r_color_label").gameObject.SetActive(RTEditor.ShowModdedUI);
                        kfdialog.Find("r_color").gameObject.SetActive(RTEditor.ShowModdedUI);

                        kfdialog.Find("r_color_target_label").gameObject.SetActive(RTEditor.ShowModdedUI);
                        kfdialog.Find("r_color_target").gameObject.SetActive(RTEditor.ShowModdedUI);

                        kfdialog.Find("r_label").gameObject.SetActive(RTEditor.ShowModdedUI);
                        kfdialog.Find("random").gameObject.SetActive(RTEditor.ShowModdedUI);

                        kfdialog.Find("shift").gameObject.SetActive(RTEditor.ShowModdedUI);

                        if (!RTEditor.ShowModdedUI)
                            break;

                        kfdialog.Find("color").GetComponent<RectTransform>().sizeDelta = new Vector2(366f, 78f);

                        var opacity = kfdialog.Find("opacity/x").GetComponent<InputField>();

                        opacity.onValueChanged.RemoveAllListeners();
                        opacity.text = Mathf.Clamp(-firstKF.GetData<EventKeyframe>().eventValues[1] + 1, 0f, 1f).ToString();
                        opacity.onValueChanged.AddListener(delegate (string _val)
                        {
                            if (float.TryParse(_val, out float n))
                            {
                                foreach (var keyframe in selected.Select(x => x.GetData<EventKeyframe>()))
                                    keyframe.eventValues[1] = Mathf.Clamp(-n + 1, 0f, 1f);

                                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                    if (UpdateObjects)
                                    Updater.UpdateProcessor(beatmapObject, "Keyframes");
                            }
                        });

                        TriggerHelper.AddEventTrigger(kfdialog.Find("opacity").gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(opacity, 0.1f, 10f, 0f, 1f) });

                        TriggerHelper.IncreaseDecreaseButtons(opacity);

                        var collision = kfdialog.Find("opacity/collision").GetComponent<Toggle>();
                        collision.onValueChanged.ClearAll();
                        collision.isOn = beatmapObject.opacityCollision;
                        collision.onValueChanged.AddListener(delegate (bool _val)
                        {
                            beatmapObject.opacityCollision = _val;
                                // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                if (UpdateObjects)
                                Updater.UpdateProcessor(beatmapObject);
                        });

                        var hue = kfdialog.Find("huesatval/x").GetComponent<InputField>();

                        hue.onValueChanged.RemoveAllListeners();
                        hue.text = firstKF.GetData<EventKeyframe>().eventValues[2].ToString();
                        hue.onValueChanged.AddListener(delegate (string _val)
                        {
                            if (float.TryParse(_val, out float n))
                            {
                                firstKF.GetData<EventKeyframe>().eventValues[2] = n;

                                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                    if (UpdateObjects)
                                    Updater.UpdateProcessor(beatmapObject, "Keyframes");
                            }
                        });

                        Destroy(kfdialog.transform.Find("huesatval").GetComponent<EventTrigger>());

                        TriggerHelper.AddEventTrigger(hue.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(hue, 0.1f, 10f) });
                        TriggerHelper.IncreaseDecreaseButtons(hue, 0.1f, 10f);

                        var sat = kfdialog.Find("huesatval/y").GetComponent<InputField>();

                        sat.onValueChanged.RemoveAllListeners();
                        sat.text = firstKF.GetData<EventKeyframe>().eventValues[3].ToString();
                        sat.onValueChanged.AddListener(delegate (string _val)
                        {
                            if (float.TryParse(_val, out float n))
                            {
                                firstKF.GetData<EventKeyframe>().eventValues[3] = n;

                                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                    if (UpdateObjects)
                                    Updater.UpdateProcessor(beatmapObject, "Keyframes");
                            }
                        });

                        TriggerHelper.AddEventTrigger(sat.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(sat, 0.1f, 10f) });
                        TriggerHelper.IncreaseDecreaseButtons(sat, 0.1f, 10f);

                        var val = kfdialog.Find("huesatval/z").GetComponent<InputField>();

                        val.onValueChanged.RemoveAllListeners();
                        val.text = firstKF.GetData<EventKeyframe>().eventValues[4].ToString();
                        val.onValueChanged.AddListener(delegate (string _val)
                        {
                            if (float.TryParse(_val, out float n))
                            {
                                firstKF.GetData<EventKeyframe>().eventValues[4] = n;

                                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                    if (UpdateObjects)
                                    Updater.UpdateProcessor(beatmapObject, "Keyframes");
                            }
                        });

                        TriggerHelper.AddEventTrigger(val.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(val, 0.1f, 10f) });
                        TriggerHelper.IncreaseDecreaseButtons(val, 0.1f, 10f);

                        var rColor = kfdialog.Find("r_color");

                        kfdialog.Find("r_color_label").gameObject.SetActive(random != 0);
                        rColor.gameObject.SetActive(random != 0);

                        kfdialog.Find("r_color_target_label").gameObject.SetActive(random != 0);

                        var randomColorTarget = kfdialog.Find("r_color_target");
                        randomColorTarget.gameObject.SetActive(random != 0);

                        kfdialog.Find("r_label/interval").gameObject.SetActive(random != 0);
                        kfdialog.Find("r_label/interval").GetComponent<Text>().text = random == 6 ? "Speed" : "Random Interval";
                        var rand = kfdialog.Find("random");
                        var none = rand.Find("none").GetComponent<Toggle>();
                        var homingDynamic = rand.Find("homing-dynamic").GetComponent<Toggle>();

                        none.onValueChanged.ClearAll();
                        none.isOn = random == 0;
                        none.onValueChanged.AddListener(delegate (bool _val)
                        {
                            if (_val)
                            {
                                foreach (var keyframe in selected.Select(x => x.GetData<EventKeyframe>()))
                                    keyframe.random = 0;
                                if (UpdateObjects)
                                    Updater.UpdateProcessor(beatmapObject, "Keyframes");
                                RenderObjectKeyframesDialog(beatmapObject);
                            }
                        });

                        homingDynamic.onValueChanged.ClearAll();
                        homingDynamic.isOn = random == 5;
                        homingDynamic.onValueChanged.AddListener(delegate (bool _val)
                        {
                            if (_val)
                            {
                                foreach (var keyframe in selected.Select(x => x.GetData<EventKeyframe>()))
                                    keyframe.random = 5;
                                if (UpdateObjects)
                                    Updater.UpdateProcessor(beatmapObject, "Keyframes");
                                RenderObjectKeyframesDialog(beatmapObject);
                            }
                        });

                        rand.Find("interval-input").gameObject.SetActive(random != 0);

                        if (random != 0)
                        {
                            var x = rColor.Find("x").GetComponent<InputField>();
                            var y = rColor.Find("y").GetComponent<InputField>();

                            x.onValueChanged.ClearAll();
                            x.text = firstKF.GetData<EventKeyframe>().eventRandomValues[0].ToString();
                            x.onValueChanged.AddListener(delegate (string _val)
                            {
                                if (float.TryParse(_val, out float n))
                                {
                                    firstKF.GetData<EventKeyframe>().eventRandomValues[0] = n;
                                    if (UpdateObjects)
                                        Updater.UpdateProcessor(beatmapObject, "Keyframes");
                                }
                            });

                            y.onValueChanged.ClearAll();
                            y.text = firstKF.GetData<EventKeyframe>().eventRandomValues[1].ToString();
                            y.onValueChanged.AddListener(delegate (string _val)
                            {
                                if (float.TryParse(_val, out float n))
                                {
                                    firstKF.GetData<EventKeyframe>().eventRandomValues[1] = n;
                                    if (UpdateObjects)
                                        Updater.UpdateProcessor(beatmapObject, "Keyframes");
                                }
                            });

                            TriggerHelper.AddEventTriggerParams(x.gameObject,
                                TriggerHelper.ScrollDelta(x, multi: true),
                                TriggerHelper.ScrollDeltaVector2(x, y, 0.1f, 10f));
                            TriggerHelper.AddEventTriggerParams(y.gameObject,
                                TriggerHelper.ScrollDelta(y, multi: true),
                                TriggerHelper.ScrollDeltaVector2(x, y, 0.1f, 10f));

                            TriggerHelper.IncreaseDecreaseButtons(x);
                            TriggerHelper.IncreaseDecreaseButtons(y);

                            var toggles = randomColorTarget.GetComponentsInChildren<Toggle>();
                            int num6 = 0;
                            foreach (var toggle in toggles)
                            {
                                toggle.onValueChanged.RemoveAllListeners();
                                int tmpIndex = num6;

                                toggle.NewValueChangedListener(num6 == firstKF.GetData<EventKeyframe>().eventRandomValues[3], delegate (bool _val)
                                {
                                    SetKeyframeRandomColorTarget(beatmapObject, 3, tmpIndex, toggles);
                                });

                                if (showModifiedColors)
                                {
                                    var color = RTHelpers.BeatmapTheme.GetObjColor(tmpIndex);

                                    float hueNum = beatmapObject.Interpolate(type, 2);
                                    float satNum = beatmapObject.Interpolate(type, 3);
                                    float valNum = beatmapObject.Interpolate(type, 4);

                                    toggle.image.color = RTHelpers.ChangeColorHSV(color, hueNum, satNum, valNum);
                                }
                                else
                                    toggle.image.color = RTHelpers.BeatmapTheme.GetObjColor(tmpIndex);

                                if (!toggle.GetComponent<HoverUI>())
                                {
                                    var hoverUI = toggle.gameObject.AddComponent<HoverUI>();
                                    hoverUI.animatePos = false;
                                    hoverUI.animateSca = true;
                                    hoverUI.size = 1.1f;
                                }
                                num6++;
                            }

                            var intervalInput = rand.Find("interval-input").GetComponent<InputField>();
                            intervalInput.onValueChanged.ClearAll();
                            intervalInput.text = firstKF.GetData<EventKeyframe>().eventRandomValues[2].ToString();
                            intervalInput.onValueChanged.AddListener(delegate (string _val)
                            {
                                if (float.TryParse(_val, out float n))
                                {
                                    foreach (var keyframe in selected.Select(x => x.GetData<EventKeyframe>()))
                                        keyframe.eventRandomValues[2] = n;
                                    if (UpdateObjects)
                                        Updater.UpdateProcessor(beatmapObject, "Keyframes");
                                }
                            });

                            TriggerHelper.AddEventTriggerParams(intervalInput.gameObject,
                                TriggerHelper.ScrollDelta(intervalInput, 0.01f, max: random == 6 ? 1f : 0f));
                        }

                        break;
                    }
            }

            var relativeBase = kfdialog.Find("relative");

            if (!relativeBase)
                return;

            RTEditor.SetActive(relativeBase.gameObject, RTEditor.ShowModdedUI);
            if (RTEditor.ShowModdedUI)
            {
                var relative = relativeBase.GetComponent<Toggle>();
                relative.onValueChanged.ClearAll();
                relative.isOn = firstKF.GetData<EventKeyframe>().relative;
                relative.onValueChanged.AddListener(delegate (bool _val)
                {
                    foreach (var keyframe in selected.Select(x => x.GetData<EventKeyframe>()))
                        keyframe.relative = _val;

                        // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                        if (UpdateObjects)
                        Updater.UpdateProcessor(beatmapObject, "Keyframes");
                });
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
                Debug.Log($"{EditorPlugin.className}levelPath: {levelPath}");

                if (!RTFile.FileExists(jpgFileLocation) && !jpgFile.Replace("\\", "/").Contains(editorPath))
                {
                    File.Copy(jpgFile, jpgFileLocation);
                    Debug.Log($"{EditorPlugin.className}Copied file to : {jpgFileLocation}");
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

        public GameObject keyframeEnd;

        public static bool AllowTimeExactlyAtStart => false;
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

            if (!keyframeEnd)
            {
                ObjEditor.inst.objTimelineGrid.DeleteChildren();
                keyframeEnd = ObjEditor.inst.KeyframeEndPrefab.Duplicate(ObjEditor.inst.objTimelineGrid, "end keyframe");
            }

            var rectTransform = keyframeEnd.transform.AsRT();
            rectTransform.sizeDelta = new Vector2(4f, 122f);
            rectTransform.anchoredPosition = new Vector2(beatmapObject.GetObjectLifeLength() * ObjEditor.inst.Zoom * 14f, 0f);
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
            {
                if (EditorConfig.Instance.RememberLastKeyframeType.Value && timelineObject.InternalSelections.TryFind(x => x.Type == ObjEditor.inst.currentKeyframeKind, out TimelineObject kf))
                    kf.selected = true;
                else
                    timelineObject.InternalSelections[0].selected = true;
            }
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

        public void UpdateKeyframeOrder(BeatmapObject beatmapObject)
        {
            for (int i = 0; i < beatmapObject.events.Count; i++)
            {
                beatmapObject.events[i] = (from x in beatmapObject.events[i]
                                            orderby x.eventTime
                                            select x).ToList();
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

                if (EditorConfig.Instance.RoundToNearest.Value)
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
            if (EditorConfig.Instance.RoundToNearest.Value)
                val = RTMath.RoundToNearestDecimal(val);

            if (EditorConfig.Instance.RoundToNearest.Value)
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

                toggle.isOn = num == value;

                int tmpIndex = num;
                toggle.onValueChanged.AddListener(delegate (bool val)
                {
                    SetKeyframeColor(beatmapObject, 0, tmpIndex);
                });
                num++;
            }
        }

        public void SetKeyframeColor(BeatmapObject beatmapObject, int index, int value, IEnumerable<TimelineObject> selected)
        {
            foreach (var keyframe in selected.Select(x => x.GetData<EventKeyframe>()))
                keyframe.eventValues[index] = value;

            // Since keyframe color has no affect on the timeline object, we will only need to update the physical object.
            if (UpdateObjects)
                Updater.UpdateProcessor(beatmapObject, "Keyframes");

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

        public void SetKeyframeRandomColorTarget(BeatmapObject beatmapObject, int index, int value, Toggle[] toggles)
        {
            beatmapObject.events[3][ObjEditor.inst.currentKeyframe].eventRandomValues[index] = (float)value;

            // Since keyframe color has no affect on the timeline object, we will only need to update the physical object.
            //if (UpdateObjects)
            //    Updater.UpdateProcessor(beatmapObject);
            Updater.UpdateProcessor(beatmapObject, "Keyframes");

            int num = 0;
            foreach (var toggle in toggles)
            {
                toggle.onValueChanged.RemoveAllListeners();

                toggle.isOn = num == value;

                int tmpIndex = num;
                toggle.onValueChanged.AddListener(delegate (bool val)
                {
                    SetKeyframeRandomColorTarget(beatmapObject, index, tmpIndex, toggles);
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
