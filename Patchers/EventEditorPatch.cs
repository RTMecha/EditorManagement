﻿using EditorManagement.Functions.Editors;
using HarmonyLib;
using LSFunctions;
using RTFunctions.Functions.Data;
using RTFunctions.Functions.Managers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
            {
                Destroy(__instance.gameObject);
                return false;
            }

            Debug.Log($"{__instance.className}" +
                $"---------------------------------------------------------------------\n" +
                $"---------------------------- INITIALIZED ----------------------------\n" +
                $"---------------------------------------------------------------------\n");

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

            RTEventEditor.Init(Instance);

            return false;
        }

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static bool UpdatePrefix()
        {
            if (Input.GetMouseButtonUp(0))
            {
                Instance.eventDrag = false;
                RTEditor.inst.dragOffset = -1f;
            }

            if (Instance.eventDrag)
            {
                var timelineTime = EditorManager.inst.GetTimelineTime();
                foreach (var timelineObject in RTEventEditor.inst.SelectedKeyframes)
                {
                    if (timelineObject.Index != 0)
                    {
                        float num = timelineTime + Instance.mouseOffsetXForDrag + timelineObject.timeOffset;
                        num = Mathf.Clamp(num, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                        DataManager.inst.gameData.eventObjects.allEvents[timelineObject.Type][timelineObject.Index].eventTime = num;
                    }
                }

                if (RTEditor.inst.dragOffset != timelineTime + Instance.mouseOffsetXForDrag)
                {
                    if (RTEditor.DraggingPlaysSound && (SettingEditor.inst.SnapActive || !RTEditor.DraggingPlaysSoundBPM))
                        SoundManager.inst.PlaySound("LeftRight", SettingEditor.inst.SnapActive ? 0.6f : 0.1f, 0.8f);

                    RTEditor.inst.dragOffset = timelineTime + Instance.mouseOffsetXForDrag;

                    RTEventEditor.inst.RenderEventObjects();
                    RTEventEditor.inst.RenderEventsDialog();
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
            RTEventEditor.inst.OpenDialog();
            return false;
        }

        [HarmonyPatch("RenderThemeContent")]
        [HarmonyPrefix]
        static bool RenderThemeContentPrefix(Transform __0, string __1)
        {
            Debug.LogFormat("{0}RenderThemeContent Prefix Patch", EditorPlugin.className);
            ThemeEditorManager.inst.RenderThemeContent(__0, __1);
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
