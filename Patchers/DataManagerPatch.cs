using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using HarmonyLib;
using BepInEx.Configuration;

using UnityEngine;

using LSFunctions;
using SimpleJSON;
using CielaSpike;

using EditorManagement.Functions.Editors;
using EditorManagement.Functions.Components;
using EditorManagement.Functions;
using EditorManagement.Functions.Tools;

using RTFunctions.Functions;

namespace EditorManagement.Patchers
{
    [HarmonyPatch(typeof(DataManager))]
    public class DataManagerPatch : MonoBehaviour
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        private static void StartPostfix(DataManager __instance)
        {
            int num = 0;
            while (__instance.PrefabTypes.Count < 20)
            {
                var prefabType = new DataManager.PrefabType();
                prefabType.Color = Color.white;
                prefabType.Name = "NewType " + num.ToString();

                __instance.PrefabTypes.Add(prefabType);
                num++;
            }
        }

        [HarmonyPatch("SaveData", typeof(string), typeof(DataManager.GameData))]
        [HarmonyPrefix]
        private static bool DataSaver(DataManager __instance, ref IEnumerator __result, string __0, DataManager.GameData __1)
        {
            __result = RTEditor.SaveData(__0, __1);
            return false;
        }

        [HarmonyPatch("CreateBaseBeatmap")]
        [HarmonyPrefix]
        private static bool CreateBaseBeatmapPatch(ref DataManager.GameData __result)
        {
            __result = RTEditor.CreateBaseBeatmap();
            return false;
        }
    }

    [HarmonyPatch(typeof(DataManager.BeatmapTheme))]
    public class DataManagerBeatmapThemePatch : MonoBehaviour
    {
        [HarmonyPatch("ClearBeatmap")]
        [HarmonyPrefix]
        private static bool ClearBeatmapPrefix(DataManager.BeatmapTheme __instance)
        {
            __instance.playerColors.Clear();
            __instance.objectColors.Clear();
            __instance.backgroundColors.Clear();
            __instance.id = LSText.randomNumString(6);
            __instance.name = ConfigEntries.TemplateThemeName.Value;
            __instance.guiColor = ConfigEntries.TemplateThemeGUIColor.Value;
            __instance.backgroundColor = ConfigEntries.TemplateThemeBGColor.Value;
            __instance.playerColors.Add(ConfigEntries.TemplateThemePlayerColor1.Value);
            __instance.playerColors.Add(ConfigEntries.TemplateThemePlayerColor2.Value);
            __instance.playerColors.Add(ConfigEntries.TemplateThemePlayerColor3.Value);
            __instance.playerColors.Add(ConfigEntries.TemplateThemePlayerColor4.Value);
            __instance.objectColors.Add(ConfigEntries.TemplateThemeOBJColor1.Value);
            __instance.objectColors.Add(ConfigEntries.TemplateThemeOBJColor2.Value);
            __instance.objectColors.Add(ConfigEntries.TemplateThemeOBJColor3.Value);
            __instance.objectColors.Add(ConfigEntries.TemplateThemeOBJColor4.Value);
            __instance.objectColors.Add(ConfigEntries.TemplateThemeOBJColor5.Value);
            __instance.objectColors.Add(ConfigEntries.TemplateThemeOBJColor6.Value);
            __instance.objectColors.Add(ConfigEntries.TemplateThemeOBJColor7.Value);
            __instance.objectColors.Add(ConfigEntries.TemplateThemeOBJColor8.Value);
            __instance.objectColors.Add(ConfigEntries.TemplateThemeOBJColor9.Value);
            __instance.objectColors.Add(ConfigEntries.TemplateThemeOBJColor1.Value);
            __instance.objectColors.Add(ConfigEntries.TemplateThemeOBJColor2.Value);
            __instance.objectColors.Add(ConfigEntries.TemplateThemeOBJColor3.Value);
            __instance.objectColors.Add(ConfigEntries.TemplateThemeOBJColor4.Value);
            __instance.objectColors.Add(ConfigEntries.TemplateThemeOBJColor5.Value);
            __instance.objectColors.Add(ConfigEntries.TemplateThemeOBJColor6.Value);
            __instance.objectColors.Add(ConfigEntries.TemplateThemeOBJColor7.Value);
            __instance.objectColors.Add(ConfigEntries.TemplateThemeOBJColor8.Value);
            __instance.objectColors.Add(ConfigEntries.TemplateThemeOBJColor9.Value);
            __instance.backgroundColors.Add(ConfigEntries.TemplateThemeBGColor1.Value);
            __instance.backgroundColors.Add(ConfigEntries.TemplateThemeBGColor2.Value);
            __instance.backgroundColors.Add(ConfigEntries.TemplateThemeBGColor3.Value);
            __instance.backgroundColors.Add(ConfigEntries.TemplateThemeBGColor4.Value);
            __instance.backgroundColors.Add(ConfigEntries.TemplateThemeBGColor5.Value);
            __instance.backgroundColors.Add(ConfigEntries.TemplateThemeBGColor6.Value);
            __instance.backgroundColors.Add(ConfigEntries.TemplateThemeBGColor7.Value);
            __instance.backgroundColors.Add(ConfigEntries.TemplateThemeBGColor8.Value);
            __instance.backgroundColors.Add(ConfigEntries.TemplateThemeBGColor9.Value);
            return false;
        }
    }
}
