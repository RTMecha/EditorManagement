using System;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using UnityEngine;
using UnityEngine.UI;

using EditorManagement.Functions;

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

		[HarmonyPatch("LoadThemes")]
		[HarmonyPostfix]
		private static void LoadThemesPrefix()
        {
			RTEditor.inst.StartCoroutine(RTEditor.LoadThemes());
        }
	}
}
