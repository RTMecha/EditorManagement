﻿using System;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using EditorManagement.Functions;
using EditorManagement.Functions.Tools;

namespace EditorManagement.Patchers
{
	[HarmonyPatch(typeof(ObjectManager))]
    public class ObjectManagerPatch : MonoBehaviour
    {
		public static Transform uiStuff;
		public static DraggableObject tracker;

		[HarmonyPatch("Awake")]
		[HarmonyPostfix]
		private static void AwakePostfixPatch()
		{
			GameObject gameObject = new GameObject("UI stuff");
			uiStuff = gameObject.transform;

			var objectTracker = Instantiate(ObjectManager.inst.objectPrefabs[1].options[0].transform.GetChild(0).gameObject);
			objectTracker.transform.SetParent(uiStuff);
			objectTracker.name = "object tracker";
			if (objectTracker.GetComponent<SelectObjectInEditor>())
			{
				Destroy(objectTracker.GetComponent<SelectObjectInEditor>());
			}
			if (objectTracker.GetComponent<RTObject>())
			{
				Destroy(objectTracker.GetComponent<RTObject>());
			}
			objectTracker.GetComponent<PolygonCollider2D>().enabled = ConfigEntries.ShowSelector.Value;
			objectTracker.GetComponent<MeshRenderer>().enabled = ConfigEntries.ShowSelector.Value;
			objectTracker.GetComponent<MeshRenderer>().material.color = new Color(1f, 1f, 1f, 1f);
			tracker = objectTracker.AddComponent<DraggableObject>();
			objectTracker.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);

			objectTracker.GetComponent<DraggableObject>().enabled = ConfigEntries.ShowSelector.Value;

			EditorPlugin.tracker = objectTracker;

			var scaleXLeft = Instantiate(ObjectManager.inst.objectPrefabs[2].options[0].transform.GetChild(0).gameObject);
			{
				scaleXLeft.transform.SetParent(objectTracker.transform);
				scaleXLeft.name = "X>";
				if (scaleXLeft.GetComponent<SelectObjectInEditor>())
				{
					Destroy(scaleXLeft.GetComponent<SelectObjectInEditor>());
				}
				if (scaleXLeft.GetComponent<RTObject>())
				{
					Destroy(scaleXLeft.GetComponent<RTObject>());
				}
				scaleXLeft.GetComponent<PolygonCollider2D>().enabled = ConfigEntries.ShowSelector.Value;
				scaleXLeft.GetComponent<MeshRenderer>().enabled = ConfigEntries.ShowSelector.Value;
				scaleXLeft.GetComponent<MeshRenderer>().material.color = Color.red;
				scaleXLeft.AddComponent<DraggableChildren>();
				scaleXLeft.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
				scaleXLeft.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 270f));
			}

			var scaleXRight = Instantiate(ObjectManager.inst.objectPrefabs[2].options[0].transform.GetChild(0).gameObject);
			{
				scaleXRight.transform.SetParent(objectTracker.transform);
				scaleXRight.name = "X<";
				if (scaleXRight.GetComponent<SelectObjectInEditor>())
				{
					Destroy(scaleXRight.GetComponent<SelectObjectInEditor>());
				}
				if (scaleXRight.GetComponent<RTObject>())
				{
					Destroy(scaleXRight.GetComponent<RTObject>());
				}
				scaleXRight.GetComponent<PolygonCollider2D>().enabled = ConfigEntries.ShowSelector.Value;
				scaleXRight.GetComponent<MeshRenderer>().enabled = ConfigEntries.ShowSelector.Value;
				scaleXRight.GetComponent<MeshRenderer>().material.color = Color.red;
				scaleXRight.AddComponent<DraggableChildren>();
				scaleXRight.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
				scaleXRight.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 90f));
			}

			var scaleYUp = Instantiate(ObjectManager.inst.objectPrefabs[2].options[0].transform.GetChild(0).gameObject);
			{
				scaleYUp.transform.SetParent(objectTracker.transform);
				scaleYUp.name = "YU";
				if (scaleYUp.GetComponent<SelectObjectInEditor>())
				{
					Destroy(scaleYUp.GetComponent<SelectObjectInEditor>());
				}
				if (scaleYUp.GetComponent<RTObject>())
				{
					Destroy(scaleYUp.GetComponent<RTObject>());
				}
				scaleYUp.GetComponent<PolygonCollider2D>().enabled = ConfigEntries.ShowSelector.Value;
				scaleYUp.GetComponent<MeshRenderer>().enabled = ConfigEntries.ShowSelector.Value;
				scaleYUp.GetComponent<MeshRenderer>().material.color = Color.green;
				scaleYUp.AddComponent<DraggableChildren>();
				scaleYUp.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
				scaleYUp.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 0f));
			}

			var scaleYDown = Instantiate(ObjectManager.inst.objectPrefabs[2].options[0].transform.GetChild(0).gameObject);
			{
				scaleYDown.transform.SetParent(objectTracker.transform);
				scaleYDown.name = "YD";
				if (scaleYDown.GetComponent<SelectObjectInEditor>())
				{
					Destroy(scaleYDown.GetComponent<SelectObjectInEditor>());
				}
				if (scaleYDown.GetComponent<RTObject>())
				{
					Destroy(scaleYDown.GetComponent<RTObject>());
				}
				scaleYDown.GetComponent<PolygonCollider2D>().enabled = ConfigEntries.ShowSelector.Value;
				scaleYDown.GetComponent<MeshRenderer>().enabled = ConfigEntries.ShowSelector.Value;
				scaleYDown.GetComponent<MeshRenderer>().material.color = Color.green;
				scaleYDown.AddComponent<DraggableChildren>();
				scaleYDown.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
				scaleYDown.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 180f));
			}

			for (int i = 0; i < ObjectManager.inst.objectPrefabs.Count; i++)
			{
				if (i != 4)
				{
					for (int j = 0; j < ObjectManager.inst.objectPrefabs[i].options.Count; j++)
					{
						ObjectManager.inst.objectPrefabs[i].options[j].transform.GetChild(0).gameObject.AddComponent<RTObject>();
					}
				}
			}
		}

		[HarmonyPatch("Update")]
		[HarmonyPostfix]
		private static void UpdatePostfixPatch()
		{
			if (EditorManager.inst != null)
			{
				foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
				{
					if (beatmapObject != null && ObjectManager.inst.beatmapGameObjects.ContainsKey(beatmapObject.id) && ObjectManager.inst.beatmapGameObjects[beatmapObject.id] != null && beatmapObject.GetGameObject() != null)
					{
						var gameObjectRef = ObjectManager.inst.beatmapGameObjects[beatmapObject.id];
						var gameObject = beatmapObject.GetGameObject();
						var transform = beatmapObject.GetGameObject().transform.GetParent();

						if (beatmapObject.objectType == DataManager.GameData.BeatmapObject.ObjectType.Empty && ConfigEntries.PreviewSelectFix.Value == true)
						{
							if (gameObjectRef.obj.GetComponentInChildren<SelectObjectInEditor>())
							{
								Destroy(gameObjectRef.obj.GetComponentInChildren<SelectObjectInEditor>());
							}
							if (gameObjectRef.obj.GetComponentInChildren<RTObject>())
							{
								Destroy(gameObjectRef.obj.GetComponentInChildren<RTObject>());
							}
						}

						if (ConfigEntries.ShowEmpties.Value == true)
						{
							if (gameObjectRef.obj.GetComponentInChildren<Collider2D>() && !gameObjectRef.obj.GetComponentInChildren<MeshFilter>() && !gameObjectRef.obj.GetComponentInChildren<MeshRenderer>())
							{
								MeshFilter mesh = gameObjectRef.obj.GetComponentInChildren<Collider2D>().gameObject.AddComponent<MeshFilter>();
								gameObjectRef.obj.GetComponentInChildren<Collider2D>().gameObject.AddComponent<MeshRenderer>();

								mesh.mesh = ObjectManager.inst.objectPrefabs[0].options[0].GetComponentInChildren<MeshFilter>().mesh;

								gameObjectRef.obj.GetComponentInChildren<Collider2D>().transform.localPosition = new Vector3(gameObjectRef.obj.GetComponentInChildren<Collider2D>().transform.localPosition.x, gameObjectRef.obj.GetComponentInChildren<Collider2D>().transform.localPosition.y, -9.6f);
							}
						}

						if (ConfigEntries.ShowDamagable.Value == true)
						{
							if (beatmapObject.objectType != DataManager.GameData.BeatmapObject.ObjectType.Normal && beatmapObject.objectType != DataManager.GameData.BeatmapObject.ObjectType.Empty)
							{
								ObjectManager.GameObjectRef gameObjectRef1 = ObjectManager.inst.beatmapGameObjects[beatmapObject.id];
								gameObjectRef1.obj.GetComponentInChildren<Renderer>().enabled = false;
							}
						}

						if (ConfigEntries.EditorDebug.Value == true)
						{
							if (gameObject.GetComponent<RTObject>())
							{
								var rtobj = gameObject.GetComponent<RTObject>();
								rtobj.tipEnabled = true;
								if (rtobj.tooltipLanguages.Count == 0)
								{
									rtobj.tooltipLanguages.Add(Triggers.NewTooltip(beatmapObject.name + " [ " + beatmapObject.StartTime + " ]", "", new List<string>()));
								}

								string parent = "";
								if (!string.IsNullOrEmpty(beatmapObject.parent))
								{
									parent = "<br>P: " + beatmapObject.parent + " (" + beatmapObject.GetParentType() + ")";
								}
								else
								{
									parent = "<br>P: No Parent" + " (" + beatmapObject.GetParentType() + ")";
								}

								string text = "";
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

								string ptr = "";
								if (beatmapObject.fromPrefab && !string.IsNullOrEmpty(beatmapObject.prefabID) && !string.IsNullOrEmpty(beatmapObject.prefabInstanceID))
								{
									ptr = "<br>PID: " + beatmapObject.prefabID + " | " + beatmapObject.prefabInstanceID;
								}
								else
								{
									ptr = "<br>Not from prefab";
								}

								if (rtobj.tooltipLanguages[0].desc != "N/ST: " + beatmapObject.name + " [ " + beatmapObject.StartTime + " ]")
								{
									rtobj.tooltipLanguages[0].desc = "N/ST: " + beatmapObject.name + " [ " + beatmapObject.StartTime + " ]";
								}
								if (rtobj.tooltipLanguages[0].hint != "ID: {" + beatmapObject.id + "}" +
									parent +
									"<br>O: {X: " + beatmapObject.origin.x + ", Y: " + beatmapObject.origin.y + "}" +
									text +
									"<br>D: " + beatmapObject.Depth +
									"<br>ED: {L: " + beatmapObject.editorData.Layer + ", B: " + beatmapObject.editorData.Bin + "}" +
									"<br>POS: {X: " + transform.position.x + ", Y: " + transform.position.y + "}" +
									"<br>SCA: {X: " + transform.localScale.x + ", Y: " + transform.localScale.y + "}" +
									"<br>ROT: " + transform.eulerAngles.z +
									"<br>COL: " + RTEditor.ColorToHex(gameObjectRef.mat.color) +
									ptr)
								{
									rtobj.tooltipLanguages[0].hint = "ID: {" + beatmapObject.id + "}" +
										parent +
										"<br>O: {X: " + beatmapObject.origin.x + ", Y: " + beatmapObject.origin.y + "}" +
										text +
										"<br>D: " + beatmapObject.Depth +
										"<br>ED: {L: " + beatmapObject.editorData.Layer + ", B: " + beatmapObject.editorData.Bin + "}" +
										"<br>POS: {X: " + transform.position.x + ", Y: " + transform.position.y + "}" +
										"<br>SCA: {X: " + transform.localScale.x + ", Y: " + transform.localScale.y + "}" +
										"<br>ROT: " + transform.eulerAngles.z +
										"<br>COL: " + RTEditor.ColorToHex(gameObjectRef.mat.color) +
										ptr;
								}
							}
						}
						else
						{
							if (gameObject.GetComponent<RTObject>())
							{
								gameObject.GetComponent<RTObject>().tipEnabled = false;
							}
						}
					}
				}
			}
		}

		[HarmonyPatch("Update")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> UpdateTranspilerFixed(IEnumerable<CodeInstruction> instructions)
		{
			var match = new CodeMatcher(instructions);

			match = match.Start();
			match = match.Advance(522);
			match = match.ThrowIfNotMatch("Is not 0.0005f 1", new CodeMatch(OpCodes.Ldc_R4));
			match = match.SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, 21));
			match = match.Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Triggers), "DummyNumber")));

			match = match.Start();
			match = match.Advance(1138); //1137
			match = match.ThrowIfNotMatch("Is not 0.0005f 2", new CodeMatch(OpCodes.Ldc_R4));
			match = match.SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, 50));
			match = match.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Triggers), "EventValuesZ1", new[] { typeof(DataManager.GameData.EventKeyframe) })));

			match = match.Start();
			match = match.Advance(1186); //1184
			match = match.ThrowIfNotMatch("Is not 0.0005f 3", new CodeMatch(OpCodes.Ldc_R4));
			match = match.SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, 50));
			match = match.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Triggers), "EventValuesZ1", new[] { typeof(DataManager.GameData.EventKeyframe) })));

			match = match.Start();
			match = match.Advance(1800); //1797
			match = match.ThrowIfNotMatch("Is not 0.1f 1", new CodeMatch(OpCodes.Ldc_R4));
			match = match.SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, 80));
			match = match.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Triggers), "EventValuesZ2", new[] { typeof(DataManager.GameData.EventKeyframe) })));

			match = match.Start();
			match = match.Advance(1832); //1828
			match = match.ThrowIfNotMatch("Is not 0.1f 2", new CodeMatch(OpCodes.Ldc_R4));
			match = match.SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, 80));
			match = match.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Triggers), "EventValuesZ2", new[] { typeof(DataManager.GameData.EventKeyframe) })));

			match = match.Start();
			match = match.Advance(1834);
			match = match.ThrowIfNotMatch("Is not DataManager.inst at 1834", new CodeMatch(OpCodes.Ldsfld));
			match = match.RemoveInstructions(10);

			match = match.Start();
			match = match.Advance(1802);
			match = match.ThrowIfNotMatch("Is not DataManager.inst at 1802", new CodeMatch(OpCodes.Ldsfld));
			match = match.RemoveInstructions(10);

			match = match.Start();
			match = match.Advance(1188);
			match = match.ThrowIfNotMatch("Is not DataManager.inst at 1188", new CodeMatch(OpCodes.Ldsfld));
			match = match.RemoveInstructions(10);

			match = match.Start();
			match = match.Advance(1140);
			match = match.ThrowIfNotMatch("Is not DataManager.inst at 1140", new CodeMatch(OpCodes.Ldsfld));
			match = match.RemoveInstructions(10);

			match = match.Start();
			match = match.Advance(524);
			match = match.ThrowIfNotMatch("Is not DataManager.inst at 524", new CodeMatch(OpCodes.Ldsfld));
			match = match.RemoveInstructions(10);

			//??? + 5 - 50
			//match = match.Start();
			//match = match.Advance(2290);
			//match = match.ThrowIfNotMatch("is not ldc.i4.3", new CodeMatch(OpCodes.Ldc_I4_3));
			//match = match.SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldloc, 100));
			//match = match.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Triggers), "EventValuesRMode", new[] { typeof(DataManager.GameData.EventKeyframe) })));

			//1623 + 3 - 30 = 1596
			//match = match.Start();
			//match = match.Advance(1596);
			//match = match.ThrowIfNotMatch("is not ldc.i4.3", new CodeMatch(OpCodes.Ldc_I4_3));
			//match = match.SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldloc, 72));
			//match = match.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Triggers), "EventValuesRMode", new[] { typeof(DataManager.GameData.EventKeyframe) })));

			//843 + 1 - 10
			//match = match.Start();
			//match = match.Advance(834);
			//match = match.ThrowIfNotMatch("is not ldc.i4.3", new CodeMatch(OpCodes.Ldc_I4_3));
			//match = match.SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldloc, 37));
			//match = match.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Triggers), "EventValuesRMode", new[] { typeof(DataManager.GameData.EventKeyframe) })));


			return match.InstructionEnumeration();
		}

		[HarmonyPatch("updateObjects", new Type[] { })]
		[HarmonyPostfix]
		private static void OMUO1()
		{
			if (tracker != null && !RTEditor.ienumRunning)
			{
				tracker.GetPosition();
			}
		}

		[HarmonyPatch("updateObjects", new Type[] { typeof(string) })]
		[HarmonyPostfix]
		private static void OMUO2()
		{
			if (tracker != null && !RTEditor.ienumRunning)
			{
				tracker.GetPosition();
			}
		}

		[HarmonyPatch("updateObjects", new Type[] { typeof(ObjEditor.ObjectSelection), typeof(bool) })]
		[HarmonyPostfix]
		private static void OMUO3()
		{
			if (tracker != null && !RTEditor.ienumRunning)
			{
				tracker.GetPosition();
			}
		}
	}
}
