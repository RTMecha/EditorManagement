using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using UnityEngine;
using UnityEngine.UI;

using EditorManagement.Functions.Editors;

using RTFunctions.Functions;

namespace EditorManagement.Patchers
{
    [HarmonyPatch(typeof(CheckpointEditor))]
    public class CheckpointEditorPatch : MonoBehaviour
    {
		[HarmonyPatch("CreateNewCheckpoint", new Type[] { typeof(float), typeof(Vector2) })]
		[HarmonyPrefix]
		static bool CreateNewCheckpointPrefix(CheckpointEditor __instance, float __0, Vector2 __1)
		{
			var checkpoint = new DataManager.GameData.BeatmapData.Checkpoint();
			checkpoint.time = Mathf.Clamp(__0, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
			checkpoint.pos = __1;
			DataManager.inst.gameData.beatmapData.checkpoints.Add(checkpoint);

			(RTEditor.inst.layerType == RTEditor.LayerType.Events ? (Action)__instance.CreateCheckpoints : __instance.CreateGhostCheckpoints).Invoke();

			__instance.SetCurrentCheckpoint(DataManager.inst.gameData.beatmapData.checkpoints.Count - 1);
			GameManager.inst.UpdateTimeline();
            GameManager.inst.ResetCheckpoints();
			return false;
        }

		[HarmonyPatch("RenderCheckpoint")]
		[HarmonyPrefix]
		static bool RenderCheckpointPrefix(CheckpointEditor __instance, int __0)
		{
			if (__0 >= 0 && __instance.checkpoints.Count > __0)
			{
				float time = DataManager.inst.gameData.beatmapData.checkpoints[__0].time;
				__instance.checkpoints[__0].transform.AsRT().anchoredPosition = new Vector2(time * EditorManager.inst.Zoom - (float)(EditorManager.BaseUnit / 2), 0f);
				if (RTEditor.inst.layerType == RTEditor.LayerType.Events)
				{
					var image = __instance.checkpoints[__0].GetComponent<Image>();
					if (__instance.currentObj == __0 && EditorManager.inst.currentDialog.Type == EditorManager.EditorDialog.DialogType.Checkpoint)
					{
						for (int i = 0; i < __instance.checkpoints.Count; i++)
							image.color = __instance.deselectedColor;
						image.color = __instance.selectedColor;
					}
					else
						image.color = __instance.deselectedColor;
				}
				__instance.checkpoints[__0].SetActive(true);
			}
			return false;
		}
	}
}
