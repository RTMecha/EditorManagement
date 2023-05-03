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

using EditorManagement.Functions;
using EditorManagement.Functions.Tools;

namespace EditorManagement.Patchers
{
	[HarmonyPatch(typeof(MarkerEditor))]
	public class MarkerEditorPatch : MonoBehaviour
    {
		[HarmonyPatch("Awake")]
		[HarmonyPostfix]
		private static void MarkerStart()
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
		private static void UpdateMakr()
		{
			if (ConfigEntries.MarkerLoop.Value == true && DataManager.inst.gameData.beatmapData.markers.Count != 0)
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

			if (EditorManager.inst.GetDialog("Marker Editor").Dialog.gameObject.activeSelf == true)
			{
				GameObject markerList = EditorManager.inst.GetDialog("Marker Editor").Dialog.Find("data/right/markers/list").gameObject;
				GameObject eventButton = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject/event");
				Color bcol = new Color(0.3922f, 0.7098f, 0.9647f, 1f);

				if (!markerList.transform.Find("sort markers"))
				{
					GameObject sortMarkers = Instantiate(eventButton);
					sortMarkers.transform.SetParent(markerList.transform);
					sortMarkers.transform.SetAsFirstSibling();
					sortMarkers.name = "sort markers";

					sortMarkers.transform.GetChild(0).GetComponent<Text>().text = "Sort Markers";
					sortMarkers.GetComponent<Image>().color = bcol;

					sortMarkers.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
					sortMarkers.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
					sortMarkers.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
					sortMarkers.GetComponent<Button>().onClick.RemoveAllListeners();
					sortMarkers.GetComponent<Button>().onClick.AddListener(delegate ()
					{
						List<DataManager.GameData.BeatmapData.Marker> result = new List<DataManager.GameData.BeatmapData.Marker>();
						result = (from x in DataManager.inst.gameData.beatmapData.markers
								  orderby x.time ascending
								  select x).ToList<DataManager.GameData.BeatmapData.Marker>();

						DataManager.inst.gameData.beatmapData.markers = result;
						MarkerEditor.inst.UpdateMarkerList();
					});

					HoverTooltip markerHoverTooltip = sortMarkers.AddComponent<HoverTooltip>();
					HoverTooltip.Tooltip markerTip = new HoverTooltip.Tooltip();

					markerTip.desc = "Sort markers by time.";
					markerTip.hint = "Clicking this will sort and update all markers in the list by song time.<br><#FF0000><b>WARNING!</color></b><br>This is not reversible. Only click this if you really want to sort your markers. (This will only affect the internal marker list, you don't have to worry about the markers in the timeline changing.)";

					markerHoverTooltip.tooltipLangauges.Add(markerTip);
				}

				if (markerList.transform.GetChild(0).name != "sort markers")
				{
					markerList.transform.Find("sort markers").SetAsFirstSibling();
				}

				foreach (var marker in DataManager.inst.gameData.beatmapData.markers)
				{
					var regex = new System.Text.RegularExpressions.Regex(@"hideObjects\((.*?)\)");
					var match = regex.Match(marker.desc);
					if (match.Success)
					{
						if (ConfigEntries.ShowObjectsOnLayer.Value == true)
						{
							foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
							{
								if (beatmapObject.editorData.Layer != int.Parse(match.Groups[1].ToString()))
								{
									ObjectManager.GameObjectRef gameObjectRef = ObjectManager.inst.beatmapGameObjects[beatmapObject.id];
									Color objColor = gameObjectRef.mat.color;
									gameObjectRef.mat.color = new Color(objColor.r, objColor.g, objColor.b, objColor.a * ConfigEntries.ShowObjectsAlpha.Value);
								}
							}
						}
					}
				}
			}
		}

		[HarmonyPatch("OpenDialog")]
		[HarmonyPostfix]
		private static void MarkerOpenDialog(MarkerEditor __instance, int __0)
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
	}
}
