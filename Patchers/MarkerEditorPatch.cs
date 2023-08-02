using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using HarmonyLib;

using LSFunctions;

using EditorManagement.Functions.Editors;
using EditorManagement.Functions.Components;
using EditorManagement.Functions;
using EditorManagement.Functions.Tools;

using RTFunctions.Functions;

namespace EditorManagement.Patchers
{
	[HarmonyPatch(typeof(MarkerEditor))]
	public class MarkerEditorPatch : MonoBehaviour
    {
		[HarmonyPatch("Awake")]
		[HarmonyPostfix]
		private static void AwakePostfix()
		{
			EditorPlugin.SetNewMarkerColors();

			Transform transform = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/MarkerDialog/data/left").transform;
			Text textFont = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/MarkerDialog/data/left/label/text").GetComponent<Text>();

			GameObject indexparent = new GameObject("index");
			indexparent.transform.parent = transform;
			RectTransform rtindexpr = indexparent.AddComponent<RectTransform>();
			rtindexpr.anchoredPosition = new Vector2(0f, -80f);
			LayoutElement leindexpr = indexparent.AddComponent<LayoutElement>();

			leindexpr.ignoreLayout = true;

			GameObject index = new GameObject("text");
			index.transform.parent = indexparent.transform;
			RectTransform rtindex = index.AddComponent<RectTransform>();
			index.AddComponent<CanvasRenderer>();
			Text ttindex = index.AddComponent<Text>();

			rtindex.anchoredPosition = new Vector2(-135f, 2f);

			ttindex.text = "Index: " + MarkerEditor.inst.currentMarker.ToString();
			ttindex.font = textFont.font;
			ttindex.color = new Color(0.9f, 0.9f, 0.9f);
			ttindex.alignment = TextAnchor.MiddleLeft;
			ttindex.fontSize = 24;
			ttindex.horizontalOverflow = HorizontalWrapMode.Overflow;
		}

		[HarmonyPatch("Update")]
		[HarmonyPostfix]
		private static void UpdatePostfix()
		{
			if (ConfigEntries.MarkerLoop.Value == true && DataManager.inst.gameData.beatmapData.markers.Count > 0)
			{
				int markerEnd = ConfigEntries.MarkerEndIndex.Value;
				int markerStart = ConfigEntries.MarkerStartIndex.Value;
				if (ConfigEntries.MarkerEndIndex.Value < 0)
				{
					markerEnd = 0;
				}
				if (ConfigEntries.MarkerEndIndex.Value > DataManager.inst.gameData.beatmapData.markers.Count - 1)
				{
					markerEnd = DataManager.inst.gameData.beatmapData.markers.Count - 1;

				}
				if (ConfigEntries.MarkerStartIndex.Value < 0)
				{
					markerStart = 0;
				}
				if (ConfigEntries.MarkerStartIndex.Value > DataManager.inst.gameData.beatmapData.markers.Count - 1)
				{
					markerStart = DataManager.inst.gameData.beatmapData.markers.Count - 1;

				}

				if (AudioManager.inst.CurrentAudioSource.time > DataManager.inst.gameData.beatmapData.markers[markerEnd].time)
				{
					AudioManager.inst.CurrentAudioSource.time = DataManager.inst.gameData.beatmapData.markers[markerStart].time;
				}
			}
		}

		[HarmonyPatch("OpenDialog")]
		[HarmonyPostfix]
		private static void OpenDialogPostfix(MarkerEditor __instance, int __0)
		{
			EditorPlugin.SetNewMarkerColors();
			GameObject.Find("EditorDialogs/MarkerDialog/data/left/color").GetComponent<GridLayoutGroup>().spacing = new Vector2(8f, 8f);
			GameObject.Find("EditorDialogs/MarkerDialog/data/left/index/text").GetComponent<Text>().text = "Index: " + MarkerEditor.inst.currentMarker.ToString();

			var regex = new System.Text.RegularExpressions.Regex(@"setLayer\((.*?)\)");
			var match = regex.Match(DataManager.inst.gameData.beatmapData.markers[__0].desc);
			if (match.Success)
			{
				if (match.Groups[1].ToString().ToLower() == "events" || match.Groups[1].ToString().ToLower() == "check" || match.Groups[1].ToString().ToLower() == "event/check")
				{
					RTEditor.SetLayer(5);
				}
				else
				{
					if (int.Parse(match.Groups[1].ToString()) > 0)
					{
						if (int.Parse(match.Groups[1].ToString()) < 6)
						{
							RTEditor.SetLayer(int.Parse(match.Groups[1].ToString()) - 1);
						}
						else
						{
							RTEditor.SetLayer(int.Parse(match.Groups[1].ToString()));
						}
					}
					else
					{
						RTEditor.SetLayer(0);
					}
				}
			}
		}

		[HarmonyPatch("RenderMarker")]
		[HarmonyPrefix]
		private static bool RenderMarkersPostfix(int __0)
        {
			if (__0 >= 0 && MarkerEditor.inst.markers.Count > __0)
			{
				DataManager.GameData.BeatmapData.Marker marker = DataManager.inst.gameData.beatmapData.markers[__0];
				float time = marker.time;
				GameObject markerObject = MarkerEditor.inst.markers[__0];
				Color markerColor = MarkerEditor.inst.markerColors[Mathf.Clamp(marker.color, 0, MarkerEditor.inst.markerColors.Count() - 1)];

				if (markerObject.GetComponent<HoverTooltip>())
				{
					markerObject.GetComponent<HoverTooltip>().tooltipLangauges.Clear();
					markerObject.GetComponent<HoverTooltip>().tooltipLangauges.Add(Triggers.NewTooltip("<#" + LSColors.ColorToHex(markerColor) + ">" + marker.name + " [ " + marker.time + " ]</color>", marker.desc, new List<string>()));
				}

				markerObject.GetComponent<RectTransform>().sizeDelta = new Vector2(12f, 12f);
				markerObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(time * EditorManager.inst.Zoom - 6f, -12f);
				markerObject.GetComponent<Image>().color = markerColor;
				markerObject.GetComponentInChildren<Text>().text = marker.name;
				markerObject.SetActive(true);
			}
			return false;
		}

		[HarmonyPatch("UpdateMarkerList")]
		[HarmonyPrefix]
		private static bool UpdateMarkerList(MarkerEditor __instance)
		{
			Transform parent = __instance.right.Find("markers/list");
			LSHelpers.DeleteChildren(parent);

			GameObject eventButton = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject/event");

			//if (DataManager.inst.gameData.beatmapData.markers.Count > 0)
			//{
			//	var result = new List<DataManager.GameData.BeatmapData.Marker>();
			//	result = (from x in DataManager.inst.gameData.beatmapData.markers
			//			  orderby x.time ascending
			//			  select x).ToList();

			//	DataManager.inst.gameData.beatmapData.markers = result;
			//}

			//Delete Markers
            {
				GameObject sortMarkers = Instantiate(eventButton);
				sortMarkers.transform.SetParent(parent);
				sortMarkers.name = "delete markers";

				sortMarkers.transform.GetChild(0).GetComponent<Text>().text = "Delete Markers";
				sortMarkers.GetComponent<Image>().color = new Color(1f, 0.131f, 0.231f, 1f);

				var sortButton = sortMarkers.GetComponent<Button>();
				sortButton.onClick.ClearAll();
				sortButton.onClick.AddListener(delegate ()
				{
					EditorManager.inst.ShowDialog("Warning Popup");
					RTEditor.RefreshWarningPopup("Are you sure you want to delete ALL markers? (This is irreversible!)", delegate ()
					{
						EditorManager.inst.DisplayNotification("Deleted " + (DataManager.inst.gameData.beatmapData.markers.Count - 1).ToString() + " markers!", 2f, EditorManager.NotificationType.Success);
						DataManager.inst.gameData.beatmapData.markers.Clear();
						MarkerEditor.inst.UpdateMarkerList();
						MarkerEditor.inst.CreateMarkers();
						EditorManager.inst.HideDialog("Warning Popup");
						EditorManager.inst.HideDialog("Marker Editor");
						CheckpointEditor.inst.SetCurrentCheckpoint(0);
					}, delegate ()
					{
						EditorManager.inst.HideDialog("Warning Popup");
					});
				});

				if (sortMarkers.GetComponent<HoverUI>())
				{
					Destroy(sortMarkers.GetComponent<HoverUI>());
				}
				if (sortMarkers.GetComponent<HoverTooltip>())
				{
					var tt = sortMarkers.GetComponent<HoverTooltip>();
					tt.tooltipLangauges.Clear();
					tt.tooltipLangauges.Add(Triggers.NewTooltip("Delete all markers.", "Clicking this will delete every marker in the level.", new List<string>()));
				}
			}

			int num = 0;
			foreach (DataManager.GameData.BeatmapData.Marker marker in DataManager.inst.gameData.beatmapData.markers)
			{
				if (marker.name.ToLower().Contains(__instance.sortedName.ToLower()) || marker.desc.ToLower().Contains(__instance.sortedName.ToLower()) || string.IsNullOrEmpty(__instance.sortedName))
				{
					GameObject gameObject = Instantiate(__instance.markerButtonPrefab, Vector3.zero, Quaternion.identity);
					gameObject.name = marker.name + "_marker";
					gameObject.transform.SetParent(parent);
					gameObject.transform.localScale = Vector3.one;
					Text component = gameObject.transform.Find("name").GetComponent<Text>();
					Text component2 = gameObject.transform.Find("pos").GetComponent<Text>();
					Graphic component3 = gameObject.transform.Find("color").GetComponent<Image>();
					component.text = marker.name;
					component2.text = string.Format("{0:0}:{1:00}.{2:000}", Mathf.Floor(marker.time / 60f), Mathf.Floor(marker.time % 60f), Mathf.Floor(marker.time * 1000f % 1000f));
					component3.color = __instance.markerColors[marker.color];
					int markerIndexTmp = num;
					gameObject.GetComponent<Button>().onClick.AddListener(delegate ()
					{
						__instance.SetCurrentMarker(markerIndexTmp, true);
					});

					Color markerColor = MarkerEditor.inst.markerColors[Mathf.Clamp(marker.color, 0, MarkerEditor.inst.markerColors.Count() - 1)];
					Triggers.AddTooltip(gameObject, "<#" + LSColors.ColorToHex(markerColor) + ">" + marker.name + " [ " + marker.time + " ]</color>", marker.desc, new List<string>());
				}
				num++;
			}
			return false;
		}
	}
}
