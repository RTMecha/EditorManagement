using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using UnityEngine;

using HarmonyLib;

using EditorManagement.Functions;

namespace EditorManagement.Patchers
{
    [HarmonyPatch(typeof(DataManager.GameData))]
    public class DataManagerGameDataPatch
    {
        public static Stopwatch sw;

        [HarmonyPatch("ParseBeatmap")]
        [HarmonyPrefix]
        private static bool ParseBeatmapPatch(string _json)
        {
            if (EditorManager.inst != null && EditorPlugin.IfEditorSlowLoads.Value)
            {
                UnityEngine.Debug.LogFormat("{0} Parse Beatmap", EditorPlugin.className);
                RTEditor.inst.StartCoroutine(RTEditor.ParseBeatmap(_json));
                sw.Stop();
                sw.Reset();
                return false;
            }
            return true;
        }

        //[HarmonyPatch("ParseBeatmap")]
        //[HarmonyPrefix]
        private static void ParseBeatmapPrefix(string _json)
        {
            sw.Start();
        }

        //[HarmonyPatch("ParseBeatmap")]
        //[HarmonyPostfix]
        private static void ParseBeatmapPostfix(string _json)
        {
            sw.Stop();
            if (EditorManager.inst != null)
            {
                EditorManager.inst.DisplayNotification("Parsed Beatmap in time: " + sw.Elapsed, 2f, EditorManager.NotificationType.Info);
            }
            sw.Reset();
        }
    }
}
