using System;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using UnityEngine;
using UnityEngine.UI;

using EditorManagement.Functions.Editors;
using EditorManagement.Functions.Components;
using EditorManagement.Functions;

using LSFunctions;
using SimpleJSON;

namespace EditorManagement.Patchers
{
    [HarmonyPatch(typeof(ThemeEditor))]
    public class ThemeEditorPatch : MonoBehaviour
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void DestroyThemeListLayout()
        {
            Destroy(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/theme/themes/viewport/content").GetComponent<VerticalLayoutGroup>());
        }

		[HarmonyPatch("DeleteTheme")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> DeleteThemeTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			return new CodeMatcher(instructions)
				.Start()
				.Advance(1)
				.ThrowIfNotMatch("is not beatmaps/themes", new CodeMatch(OpCodes.Ldstr))
				.SetInstruction(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(EditorPlugin), "themeListPath")))
				.InstructionEnumeration();
		}

		[HarmonyPatch("SaveTheme")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> SaveThemeTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			return new CodeMatcher(instructions)
				.Start()
				.Advance(116)
				.ThrowIfNotMatch("is not beatmaps/themes", new CodeMatch(OpCodes.Ldstr))
				.SetInstruction(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(EditorPlugin), "themeListPath")))
				.InstructionEnumeration();
		}
		
		[HarmonyPatch("SaveTheme")]
		[HarmonyPrefix]
		private static bool SaveThemePrefixPatch(DataManager.BeatmapTheme __0)
		{
			Debug.LogFormat("Saving {0} ({1}) to File System!", __0.id, __0.name);
			JSONNode jsonnode = JSON.Parse("{}");
			if (string.IsNullOrEmpty(__0.id))
			{
				__0.id = LSText.randomNumString(6);
			}

			jsonnode["id"] = __0.id.ToString();
			jsonnode["name"] = __0.name;
			jsonnode["gui"] = RTEditor.ColorToHex(__0.guiColor);
			jsonnode["bg"] = LSColors.ColorToHex(__0.backgroundColor);

			for (int i = 0; i < __0.playerColors.Count; i++)
			{
				jsonnode["players"][i] = RTEditor.ColorToHex(__0.playerColors[i]);
			}
			for (int j = 0; j < __0.objectColors.Count; j++)
			{
				jsonnode["objs"][j] = RTEditor.ColorToHex(__0.objectColors[j]);
			}
			for (int k = 0; k < __0.backgroundColors.Count; k++)
			{
				jsonnode["bgs"][k] = LSColors.ColorToHex(__0.backgroundColors[k]);
			}

			FileManager.inst.SaveJSONFile(EditorPlugin.themeListPath, __0.name.ToLower().Replace(" ", "_") + ".lst", jsonnode.ToString());
			EditorManager.inst.DisplayNotification(string.Format("Saved theme [{0}]!", __0.name), 2f, EditorManager.NotificationType.Success, false);
			return false;
        }

		[HarmonyPatch("LoadThemes")]
		[HarmonyPrefix]
		private static bool LoadThemesPrefix(ThemeEditor __instance, ref IEnumerator __result)
        {
			Debug.LogFormat("{0}Started Loading themes...", EditorPlugin.className);
			__result = RTEditor.LoadThemes();
			return false;
        }
	}
}
