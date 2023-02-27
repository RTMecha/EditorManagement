using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

using SimpleJSON;

using HarmonyLib;

using EditorManagement.Functions;
using LSFunctions;

namespace EditorManagement
{
	[HarmonyPatch(typeof(ObjEditor))]
    public class ObjEditorPatch : MonoBehaviour
    {
		[HarmonyPatch("Awake")]
		[HarmonyPostfix]
		private static void CreateLayers()
		{
			if (ObjEditor.inst.ObjectView.transform.Find("spacer"))
			{
				ObjEditor.inst.ObjectView.transform.GetChild(17).GetChild(1).gameObject.SetActive(true);
			}
			else
			{
				ObjEditor.inst.ObjectView.transform.GetChild(16).GetChild(1).gameObject.SetActive(true);
			}
			ObjEditor.inst.ObjectView.transform.Find("editor/bin").gameObject.SetActive(true);

			ObjEditor.inst.ObjectView.transform.Find("editor/layer").gameObject.SetActive(false);

			GameObject tbarLayers = Instantiate(ObjEditor.inst.ObjectView.transform.Find("time/time").gameObject);

			tbarLayers.transform.SetParent(ObjEditor.inst.ObjectView.transform.Find("editor"));
			tbarLayers.name = "layers";
			tbarLayers.transform.SetSiblingIndex(0);
			RectTransform tbarLayersRT = tbarLayers.GetComponent<RectTransform>();
			InputField tbarLayersIF = tbarLayers.GetComponent<InputField>();
			Image layerImage = tbarLayers.GetComponent<Image>();

			tbarLayersIF.characterValidation = InputField.CharacterValidation.Integer;

			HorizontalLayoutGroup edhlg = ObjEditor.inst.ObjectView.transform.Find("editor").GetComponent<HorizontalLayoutGroup>();
			edhlg.childControlWidth = false;
			edhlg.childForceExpandWidth = false;

			tbarLayersRT.sizeDelta = new Vector2(100f, 32f);
			ObjEditor.inst.ObjectView.transform.Find("editor/bin").GetComponent<RectTransform>().sizeDelta = new Vector2(237f, 32f);
		}

		[HarmonyPatch(typeof(ObjEditor), "OpenDialog")]
		[HarmonyPostfix]
		private static void OpenD()
		{
			if (ObjEditor.inst.currentObjectSelection.IsObject())
			{
				GameObject tbarLayers = ObjEditor.inst.ObjectView.transform.Find("editor/layers").gameObject;
				InputField tbarLayersIF = tbarLayers.GetComponent<InputField>();
				Image layerImage = tbarLayers.GetComponent<Image>();

				if (ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer < 5)
				{
					float l = ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer + 1;
					tbarLayersIF.text = l.ToString();
				}
				else
				{
					int l = ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer;
					tbarLayersIF.text = l.ToString();
				}

				if (ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer < EditorManager.inst.layerColors.Count)
				{
					layerImage.color = EditorManager.inst.layerColors[ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer];
				}
				if (ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer > 6)
				{
					layerImage.color = Color.white;
				}

				tbarLayersIF.onValueChanged.RemoveAllListeners();
				tbarLayersIF.onValueChanged.AddListener(delegate (string _value)
				{
					if (int.Parse(_value) > 0)
					{
						if (int.Parse(_value) < 6)
						{
							ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer = int.Parse(_value) - 1;
							ObjEditor.inst.RenderTimelineObject(ObjEditor.inst.currentObjectSelection);
						}
						else
						{
							ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer = int.Parse(_value);
							ObjEditor.inst.RenderTimelineObject(ObjEditor.inst.currentObjectSelection);
						}
					}
					else
					{
						ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer = 0;
						ObjEditor.inst.RenderTimelineObject(ObjEditor.inst.currentObjectSelection);
						tbarLayersIF.text = "1";
					}

					if (ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer < EditorManager.inst.layerColors.Count)
					{
						layerImage.color = EditorManager.inst.layerColors[ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer];
					}
					if (ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer > 6)
					{
						layerImage.color = Color.white;
					}
				});
			}
		}

		[HarmonyPatch("RefreshKeyframeGUI")]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> TranspilerPatch(IEnumerable<CodeInstruction> instructions)
		{
			return new CodeMatcher(instructions)
				.Start()
			  .Advance(73)
			  .ThrowIfNotMatch("Is not empty", new CodeMatch(OpCodes.Ldarg_0))
			  .RemoveInstruction()
			  .Advance(0)
			  .ThrowIfNotMatch("Is not currentObjectSelection", new CodeMatch(OpCodes.Ldfld))
			  .Set(OpCodes.Ldc_I4_0, new object[] { })
			  .Advance(1)
			  .ThrowIfNotMatch("Is not Get ObjectData", new CodeMatch(OpCodes.Callvirt))
			  .RemoveInstructions(3)
			  .InstructionEnumeration();
		}

		[HarmonyPatch("AddPrefabExpandedToLevel")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> AddPrefabTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			return new CodeMatcher(instructions)
				.Start()
				.Advance(190)
				.ThrowIfNotMatch("Is not editorData object", new CodeMatch(OpCodes.Ldfld))
				.RemoveInstructions(14)
				.Advance(116)
				.ThrowIfNotMatch("Is not editorData prefab", new CodeMatch(OpCodes.Ldfld))
				.RemoveInstructions(14)
				.InstructionEnumeration();
		}

		[HarmonyPatch("CreateTimelineObjects")]
		[HarmonyPostfix]
		private static void SetEditorTime()
		{
			if (!string.IsNullOrEmpty(EditorManager.inst.currentLoadedLevel))
			{
				if (EditorPlugin.IfEditorStartTime.Value == true)
				{
					AudioManager.inst.CurrentAudioSource.time = DataManager.inst.gameData.beatmapData.editorData.timelinePos;
				}
				if (EditorPlugin.IfEditorPauses.Value == true)
				{
					AudioManager.inst.CurrentAudioSource.Pause();
				}

				if (RTFile.FileExists(RTFile.GetApplicationDirectory() + "beatmaps/" + EditorPlugin.editorPath + "/" + EditorManager.inst.currentLoadedLevel + "/editor.lse"))
				{
					string rawProfileJSON = null;
					rawProfileJSON = FileManager.inst.LoadJSONFile("beatmaps/" + EditorPlugin.editorPath + "/" + EditorManager.inst.currentLoadedLevel + "/editor.lse");

					JSONNode jsonnode = JSON.Parse(rawProfileJSON);

					EditorManager.inst.Zoom = jsonnode["timeline"]["z"];
					GameObject.Find("Editor Systems/Editor GUI/sizer/main/whole-timeline/zoom-panel/Slider").GetComponent<Slider>().value = jsonnode["timeline"]["tsc"];

					RTEditor.SetLayer(jsonnode["timeline"]["l"]);

					EditorPlugin.timeEdit = jsonnode["editor"]["t"];
					EditorPlugin.openAmount = jsonnode["editor"]["a"];
					EditorPlugin.openAmount += 1;

					SettingEditor.inst.SnapActive = jsonnode["misc"]["sn"];
					SettingEditor.inst.SnapBPM = DataManager.inst.metaData.song.BPM;
				}
				else
				{
					EditorPlugin.timeEdit = 0;
				}
			}
		}

		[HarmonyPatch("Start")]
		[HarmonyPostfix]
		private static void SetObjStart()
		{
			ObjEditor.inst.zoomBounds = EditorPlugin.ObjZoomBounds.Value;
		}
	}
}
