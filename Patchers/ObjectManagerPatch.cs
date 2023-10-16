using System;
using System.Collections.Generic;

using HarmonyLib;

using UnityEngine;

using DG.Tweening;

using EditorManagement.Functions.Editors;
using EditorManagement.Functions.Components;
using EditorManagement.Functions;

using RTFunctions.Functions.Components;

namespace EditorManagement.Patchers
{
	[HarmonyPatch(typeof(ObjectManager))]
    public class ObjectManagerPatch : MonoBehaviour
    {
		public static Transform uiStuff;

		[HarmonyPatch("Awake")]
		[HarmonyPostfix]
		static void AwakePostfixPatch()
		{
			GameObject gameObject = new GameObject("UI stuff");
			uiStuff = gameObject.transform;

			var objectTracker = Instantiate(ObjectManager.inst.objectPrefabs[1].options[0].transform.GetChild(0).gameObject);
			objectTracker.transform.SetParent(uiStuff);
			objectTracker.name = "object tracker";
			if (objectTracker.GetComponent<SelectObjectInEditor>())
				Destroy(objectTracker.GetComponent<SelectObjectInEditor>());
			if (objectTracker.GetComponent<RTObject>())
				Destroy(objectTracker.GetComponent<RTObject>());

			objectTracker.GetComponent<MeshRenderer>().material.color = new Color(1f, 1f, 1f, 1f);
			EditorPlugin.draggableObject = objectTracker.AddComponent<DraggableObject>();
			objectTracker.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);

			EditorPlugin.draggableObject.SetActive(ConfigEntries.ShowSelector.Value);

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

		[HarmonyPatch("updateObjects", new Type[] { })]
		[HarmonyPostfix]
		static void OMUO1()
		{
			if (EditorPlugin.draggableObject && !RTEditor.ienumRunning && EditorPlugin.draggableObject.enabled)
				EditorPlugin.draggableObject.GetPosition();
		}

		[HarmonyPatch("updateObjects", new Type[] { typeof(string) })]
		[HarmonyPostfix]
		static void OMUO2()
		{
			if (EditorPlugin.draggableObject && !RTEditor.ienumRunning && EditorPlugin.draggableObject.enabled)
				EditorPlugin.draggableObject.GetPosition();
		}

		[HarmonyPatch("updateObjects", new Type[] { typeof(ObjEditor.ObjectSelection), typeof(bool) })]
		[HarmonyPostfix]
		static void OMUO3()
		{
			if (EditorPlugin.draggableObject && !RTEditor.ienumRunning && EditorPlugin.draggableObject.enabled)
				EditorPlugin.draggableObject.GetPosition();
		}

		[HarmonyPatch("PurgeObjects")]
		[HarmonyPrefix]
		static bool PurgeObjects(ObjectManager __instance)
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
