﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using UnityEngine;
using UnityEngine.UI;

using RTFunctions.Patchers;

using EditorManagement.Functions.Editors;

namespace EditorManagement.Patchers
{
    [HarmonyPatch(typeof(ThemeEditor))]
    public class ThemeEditorPatch : MonoBehaviour
    {
        public static ThemeEditor Instance { get => ThemeEditor.inst; set => ThemeEditor.inst = value; }

        public static string className = "[<color=#3E6D73>ThemeEditor</color>] \n";

        [HarmonyPatch("Awake")]
        [HarmonyPrefix]
        static bool AwakePrefix(ThemeEditor __instance)
        {
            if (Instance == null)
                Instance = __instance;
            else if (Instance != __instance)
            {
                Destroy(__instance.gameObject);
                return false;
            }

            Debug.Log($"{className}" +
                $"---------------------------------------------------------------------\n" +
                $"---------------------------- INITIALIZED ----------------------------\n" +
                $"---------------------------------------------------------------------\n");

            Destroy(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/theme/themes/viewport/content").GetComponent<VerticalLayoutGroup>());

            return false;
        }

        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        static bool StartPrefix() => false;

        [HarmonyPatch("DeleteTheme")]
        [HarmonyPrefix]
        static bool DeleteThemePrefix(DataManager.BeatmapTheme __0)
        {
            ThemeEditorManager.inst.DeleteTheme((RTFunctions.Functions.Data.BeatmapTheme)__0);
            return false;
        }
        
        [HarmonyPatch("SaveTheme")]
        [HarmonyPrefix]
        static bool SaveThemePrefix(DataManager.BeatmapTheme __0)
        {
            ThemeEditorManager.inst.SaveTheme((RTFunctions.Functions.Data.BeatmapTheme)__0);
            return false;
        }

        [HarmonyPatch("LoadThemes")]
        [HarmonyPrefix]
        static bool LoadThemesPrefix(ref IEnumerator __result)
        {
            __result = RTEditor.inst.LoadThemes();
            return false;
        }
    }
}
