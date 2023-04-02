using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using SimpleJSON;

using HarmonyLib;

using LSFunctions;

using EditorManagement.Functions;
using EditorManagement.Functions.Tools;

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

			GameObject close = GameObject.Find("Editor Systems/Editor GUI/sizer/main/Popups/Open File Popup/Panel/x");

			GameObject parent = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/parent");

			parent.GetComponent<HorizontalLayoutGroup>().childControlWidth = false;
			parent.GetComponent<HorizontalLayoutGroup>().spacing = 4f;

			parent.transform.Find("text").GetComponent<RectTransform>().sizeDelta = new Vector2(241f, 32f);

			GameObject resetParent = Instantiate(close);
			resetParent.transform.SetParent(parent.transform);
			resetParent.transform.localScale = Vector3.one;
			resetParent.name = "clear parent";
			resetParent.transform.SetSiblingIndex(1);

			resetParent.GetComponent<Button>().onClick.RemoveAllListeners();
			resetParent.GetComponent<Button>().onClick.AddListener(delegate ()
			{
				ObjEditor.inst.currentObjectSelection.GetObjectData().parent = "";
				var objEditor = ObjEditor.inst;
				var refreshParentGUI = objEditor.GetType().GetMethod("RefreshParentGUI", BindingFlags.NonPublic | BindingFlags.Instance);

				refreshParentGUI.Invoke(objEditor, new object[] { "" });
				ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
			});

			parent.transform.Find("parent").GetComponent<RectTransform>().sizeDelta = new Vector2(32f, 32f);
			parent.transform.Find("more").GetComponent<RectTransform>().sizeDelta = new Vector2(32f, 32f);
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

		[HarmonyPatch("RefreshKeyframeGUI")]
		[HarmonyPostfix]
		private static void RefreshTriggers()
		{
			if (DataManager.inst.gameData.beatmapObjects.Count > 0 && !string.IsNullOrEmpty(ObjEditor.inst.currentObjectSelection.ID) && ObjEditor.inst.currentObjectSelection.IsObject() && ObjEditor.inst.keyframeSelections.Count <= 1)
			{
				//Position
				{
					InputField posX = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/position/position/x").GetComponent<InputField>();
					InputField posY = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/position/position/y").GetComponent<InputField>();
					EventTrigger posXEvent = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/position/position/x").GetComponent<EventTrigger>();
					EventTrigger posYEvent = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/position/position/y").GetComponent<EventTrigger>();

					posXEvent.triggers.Clear();
					posXEvent.triggers.Add(Triggers.ScrollDelta(posX, 0.1f, 10f, true));
					posXEvent.triggers.Add(Triggers.ScrollDeltaVector2(posX, posY, 0.1f, 10f));

					posYEvent.triggers.Clear();
					posYEvent.triggers.Add(Triggers.ScrollDelta(posY, 0.1f, 10f, true));
					posYEvent.triggers.Add(Triggers.ScrollDeltaVector2(posX, posY, 0.1f, 10f));
				}

				//Scale
				{
					InputField scaX = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/scale/scale/x").GetComponent<InputField>();
					InputField scaY = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/scale/scale/y").GetComponent<InputField>();
					EventTrigger scaXEvent = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/scale/scale/x").GetComponent<EventTrigger>();
					EventTrigger scaYEvent = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/scale/scale/y").GetComponent<EventTrigger>();

					scaXEvent.triggers.Clear();
					scaXEvent.triggers.Add(Triggers.ScrollDelta(scaX, 0.1f, 10f, true));
					scaXEvent.triggers.Add(Triggers.ScrollDeltaVector2(scaX, scaY, 0.1f, 10f));

					scaYEvent.triggers.Clear();
					scaYEvent.triggers.Add(Triggers.ScrollDelta(scaY, 0.1f, 10f, true));
					scaYEvent.triggers.Add(Triggers.ScrollDeltaVector2(scaX, scaY, 0.1f, 10f));
				}

				//Rotation
				{
					InputField rotX = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/rotation/rotation/x").GetComponent<InputField>();
					EventTrigger rotXEvent = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/rotation/rotation").GetComponent<EventTrigger>();

					rotXEvent.triggers.Clear();
					rotXEvent.triggers.Add(Triggers.ScrollDelta(rotX, 15f, 3f, false));
				}
			}
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

		//[HarmonyPatch("RenderTimelineObjects")]
		//[HarmonyPostfix]
		private static void CreateBeatmapTooltips()
        {
			foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
            {
				if (ObjEditor.inst.beatmapObjects.ContainsKey(beatmapObject.id) && ObjEditor.inst.beatmapObjects[beatmapObject.id] && ObjEditor.inst.beatmapObjects[beatmapObject.id].activeSelf == true)
				{
					var timelineObject = ObjEditor.inst.beatmapObjects[beatmapObject.id];
					Triggers.AddTooltip(timelineObject, beatmapObject.name + " [ " + beatmapObject.StartTime.ToString() + " ]", "P: " + beatmapObject.parent + "\nD: " + beatmapObject.Depth.ToString());
				}
            }
        }

		[HarmonyPatch("SetCurrentObj")]
		[HarmonyPostfix]
		private static void SetCurrentObjPostfix(ObjEditor.ObjectSelection __0)
        {
			if (EditorPlugin.EditorDebug.Value == true)
			{
				if (__0.IsObject() && !string.IsNullOrEmpty(__0.ID) && __0.GetObjectData() != null && !__0.GetObjectData().fromPrefab)
				{
					if (ObjectManager.inst.beatmapGameObjects.ContainsKey(__0.GetObjectData().id) && ObjectManager.inst.beatmapGameObjects[__0.GetObjectData().id] != null)
					{
						ObjectManager.GameObjectRef gameObjectRef = ObjectManager.inst.beatmapGameObjects[__0.GetObjectData().id];

						Transform transform = gameObjectRef.rend.transform.GetParent();

						var beatmapObject = __0.GetObjectData();

						string parent = "";
						{
							if (!string.IsNullOrEmpty(beatmapObject.parent))
							{
								parent = "<br>P: " + beatmapObject.parent + " (" + beatmapObject.GetParentType() + ")";
							}
							else
							{
								parent = "<br>P: No Parent" + " (" + beatmapObject.GetParentType() + ")";
							}
						}

						string text = "";
						{
							if (beatmapObject.shape != 4 || beatmapObject.shape != 6)
							{
								text = "<br>S: " + RTEditor.GetShape(beatmapObject.shape, beatmapObject.shapeOption) +
									"<br>T: " + beatmapObject.text;
							}
							if (beatmapObject.shape == 4)
							{
								text = "<br>S: Text" +
									"<br>T: " + beatmapObject.text;
							}
							if (beatmapObject.shape == 6)
							{
								text = "<br>S: Image" +
									"<br>T: " + beatmapObject.text;
							}
						}

						string ptr = "";
						{
							if (beatmapObject.fromPrefab)
							{
								ptr = "<br>PID: " + beatmapObject.prefabID + " | " + beatmapObject.prefabInstanceID;
							}
							else
							{
								ptr = "<br>Not from prefab";
							}
						}

						Color color = Color.white;
						if (AudioManager.inst.CurrentAudioSource.time < beatmapObject.StartTime)
                        {
							color = GameManager.inst.LiveTheme.objectColors[(int)beatmapObject.events[3][0].eventValues[0]];
                        }
						else if (AudioManager.inst.CurrentAudioSource.time > beatmapObject.StartTime + beatmapObject.GetObjectLifeLength() && beatmapObject.autoKillType != DataManager.GameData.BeatmapObject.AutoKillType.OldStyleNoAutokill)
						{
							color = GameManager.inst.LiveTheme.objectColors[(int)beatmapObject.events[3][beatmapObject.events[3].Count - 1].eventValues[0]];
						}
						else
                        {
							color = gameObjectRef.mat.color;
						}

						RTEditor.DisplayCustomNotification("RenderTimelineBeatmapObject", "<br>N/ST: " + beatmapObject.name + " [ " + beatmapObject.StartTime + " ]" +
							"<br>ID: {" + beatmapObject.id + "}" +
							parent +
							"<br>O: {X: " + beatmapObject.origin.x + ", Y: " + beatmapObject.origin.y + "}" +
							text +
							"<br>D: " + beatmapObject.Depth +
							"<br>ED: {L: " + beatmapObject.editorData.Layer + ", B: " + beatmapObject.editorData.Bin + "}" +
							"<br>POS: {X: " + transform.position.x + ", Y: " + transform.position.y + "}" +
							"<br>SCA: {X: " + transform.localScale.x + ", Y: " + transform.localScale.y + "}" +
							"<br>ROT: " + transform.eulerAngles.z +
							"<br>COL: " + RTEditor.ColorToHex(color) +
							ptr, 1f, LSColors.HexToColor("202020"), color, LSColors.InvertBlackWhiteColor(color), "Beatmap Object");
					}
				}
				if (__0.IsPrefab() && !string.IsNullOrEmpty(__0.ID) && __0.GetPrefabObjectData() != null)
				{
					var prefab = __0.GetPrefabData();
                    var prefabInstance = __0.GetPrefabObjectData();

                    Color prefabColor = DataManager.inst.PrefabTypes[prefab.Type].Color;
					RTEditor.DisplayCustomNotification("RenderTimelinePrefabObject", "" +
						"<br>N/ST: " + prefab.Name + " [ " + prefabInstance.StartTime.ToString() + " ]" +
						"<br>PID: {" + prefab.ID + "}" +
						"<br>PIID: {" + prefabInstance.ID + "}" +
						"<br>Type: " + DataManager.inst.PrefabTypes[prefab.Type].Name +
						"<br>O: " + prefab.Offset.ToString() +
						"<br>Count: " + prefab.objects.Count +
						"<br>ED: {L: " + prefabInstance.editorData.Layer + ", B: " + prefabInstance.editorData.Bin + "}" +
						"<br>POS: {X: " + prefabInstance.events[0].eventValues[0] + ", Y: " + prefabInstance.events[0].eventValues[1] + "}" +
						"<br>SCA: {X: " + prefabInstance.events[1].eventValues[0] + ", Y: " + prefabInstance.events[1].eventValues[1] + "}" +
						"<br>ROT: " + prefabInstance.events[2].eventValues[0] +
						"", 1f, LSColors.HexToColor("202020"), prefabColor, LSColors.InvertBlackWhiteColor(prefabColor), "Prefab Object");
				}
			}
        }
	}
}
