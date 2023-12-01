using System;
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
    public class ThemeEditorPatch : MonoBehaviour
    {
        public static ThemeEditor Instance { get => ThemeEditor.inst; set => ThemeEditor.inst = value; }

        public static void Init()
        {
            Patcher.CreatePatch(Instance.Awake, PatchType.Postfix, delegate (ThemeEditor __instance)
            {
                ThemeEditorManager.Init(__instance);
                Destroy(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/theme/themes/viewport/content").GetComponent<VerticalLayoutGroup>());
                return false;
            });

            Patcher.CreatePatch(Instance.Start, PatchType.Prefix, EditorPlugin.DontRun);

            Patcher.CreatePatch(Instance.DeleteTheme, PatchType.Prefix, delegate (DataManager.BeatmapTheme __0)
            {
                ThemeEditorManager.inst.DeleteTheme(__0);
                return false;
            });

            Patcher.CreatePatch(Instance.SaveTheme, PatchType.Prefix, delegate (DataManager.BeatmapTheme __0)
            {
                ThemeEditorManager.inst.SaveTheme(__0);
                return false;
            });

            Patcher.CreatePatch(AccessTools.Method(typeof(ThemeEditor), "LoadThemes"), PatchType.Prefix, AccessTools.Method(typeof(ThemeEditorPatch), "LoadThemesPrefix"));
        }

        static bool LoadThemesPrefix(ref IEnumerator __result)
        {
            __result = RTEditor.inst.LoadThemes(false);
            return false;
        }
    }
}
