using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using HarmonyLib;

using EditorManagement.Functions;

namespace EditorManagement
{
    [HarmonyPatch(typeof(DataManager.GameData))]
    public class DataManagerGameDataPatch
    {
        [HarmonyPatch("ParseBeatmap")]
        [HarmonyPrefix]
        private static bool ParseBeatmapPatch(string _json)
        {
            if (EditorManager.inst != null && EditorPlugin.IfEditorSlowLoads.Value)
            {
                Debug.Log("EditorManagement Parse Beatmap");
                RTEditor.inst.StartCoroutine(RTEditor.ParseBeatmap(_json));
                return false;
            }
            return true;
        }
    }
}
