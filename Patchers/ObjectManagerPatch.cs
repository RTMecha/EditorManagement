using System;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;
using DG.Tweening;

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
		[HarmonyPrefix]
		private static void AwakePrefixPatch()
        {
			EditorPlugin.SetCatalyst();
			Debug.LogFormat("{0}Catalyst Installed Level: {1}", EditorPlugin.className, EditorPlugin.catInstalled);
		}

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
					if (beatmapObject != null && beatmapObject.GetGameObject() != null)
					{
						var gameObject = beatmapObject.GetGameObject();
						var transform = beatmapObject.GetGameObject().transform.GetParent();
						Material mat = null;
						if (gameObject.GetComponent<Renderer>())
						{
							mat = gameObject.GetComponent<Renderer>().material;
						}

						if (beatmapObject.objectType == DataManager.GameData.BeatmapObject.ObjectType.Empty && ConfigEntries.PreviewSelectFix.Value)
						{
							if (gameObject.GetComponent<SelectObjectInEditor>())
							{
								Destroy(gameObject.GetComponent<SelectObjectInEditor>());
							}
							if (gameObject.GetComponent<RTObject>())
							{
								Destroy(gameObject.GetComponent<RTObject>());
							}
						}

						if (ConfigEntries.ShowEmpties.Value)
						{
							if (!gameObject.GetComponent<MeshFilter>() && !gameObject.GetComponent<MeshRenderer>())
							{
								MeshFilter mesh = gameObject.AddComponent<MeshFilter>();
								gameObject.AddComponent<MeshRenderer>();

								mesh.mesh = ObjectManager.inst.objectPrefabs[0].options[0].GetComponentInChildren<MeshFilter>().mesh;

								gameObject.transform.localPosition = new Vector3(gameObject.transform.localPosition.x, gameObject.transform.localPosition.y, -9.6f);
							}
						}

						if (ConfigEntries.ShowDamagable.Value)
						{
							if (beatmapObject.objectType != DataManager.GameData.BeatmapObject.ObjectType.Normal && beatmapObject.objectType != DataManager.GameData.BeatmapObject.ObjectType.Empty && gameObject.GetComponent<Renderer>())
							{
								gameObject.GetComponent<Renderer>().enabled = false;
							}
						}

						if (gameObject.GetComponent<RTObject>())
						{
							var rtobj = gameObject.GetComponent<RTObject>();
							rtobj.id = beatmapObject.id;
							if (mat != null && mat.HasProperty("_Color"))
							{
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
									text = "<br>S: " + RTEditor.GetShape(beatmapObject.shape, beatmapObject.shapeOption).Replace("eight_circle", "eighth_circle").Replace("eigth_circle_outline", "eighth_circle_outline");

									if (!string.IsNullOrEmpty(beatmapObject.text))
                                    {
										text += "<br>T: " + beatmapObject.text;
                                    }
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
									"<br>POS: {X: " + transform.position.x + ", Y: " + transform.position.y + ", Z: " + transform.position.z + "}" +
									"<br>SCA: {X: " + transform.localScale.x + ", Y: " + transform.localScale.y + "}" +
									"<br>ROT: " + transform.eulerAngles.z +
									"<br>COL: " + RTEditor.ColorToHex(mat.color) +
									ptr)
								{
									rtobj.tooltipLanguages[0].hint = "ID: {" + beatmapObject.id + "}" +
										parent +
										"<br>O: {X: " + beatmapObject.origin.x + ", Y: " + beatmapObject.origin.y + "}" +
										text +
										"<br>D: " + beatmapObject.Depth +
										"<br>ED: {L: " + beatmapObject.editorData.Layer + ", B: " + beatmapObject.editorData.Bin + "}" +
										"<br>POS: {X: " + transform.position.x + ", Y: " + transform.position.y + ", Z: " + transform.position.z + "}" +
										"<br>SCA: {X: " + transform.localScale.x + ", Y: " + transform.localScale.y + "}" +
										"<br>ROT: " + transform.eulerAngles.z +
										"<br>COL: " + RTEditor.ColorToHex(mat.color) +
										ptr;
								}
							}
						}

						if (ConfigEntries.ShowObjectsOnLayer.Value && mat != null && mat.HasProperty("_Color"))
						{
							Color objColor = mat.color;
							mat.color = LSColors.fadeColor(objColor, objColor.a * ConfigEntries.ShowObjectsAlpha.Value);
						}
					}
				}
			}
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

		[HarmonyPatch("PurgeObjects")]
		[HarmonyPrefix]
		private static bool PurgeObjects(ObjectManager __instance)
		{
			foreach (var keyValuePair in __instance.beatmapGameObjects)
			{
				keyValuePair.Value.sequence.all.Kill(false);
				keyValuePair.Value.sequence.col.Kill(false);
			}
			__instance.beatmapGameObjects = new Dictionary<string, ObjectManager.GameObjectRef>();
			DataManager.inst.gameData.beatmapObjects.Clear();
			if (EditorManager.inst == null)
				DataManager.inst.CustomBeatmapThemes.Clear();

			if (EditorManager.inst != null)
			{
				foreach (KeyValuePair<string, GameObject> keyValuePair2 in ObjEditor.inst.beatmapObjects)
				{
					Destroy(keyValuePair2.Value);
				}
				ObjEditor.inst.beatmapObjects = new Dictionary<string, GameObject>();
				foreach (KeyValuePair<string, GameObject> keyValuePair3 in ObjEditor.inst.prefabObjects)
				{
					Destroy(keyValuePair3.Value);
				}
				ObjEditor.inst.prefabObjects = new Dictionary<string, GameObject>();
            }
            foreach (object obj in __instance.objectParent.transform)
            {
                Destroy(((Transform)obj).gameObject);
            }
			return false;
        }
    }
}
