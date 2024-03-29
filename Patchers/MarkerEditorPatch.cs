﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using HarmonyLib;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using RTFunctions.Functions;
using RTFunctions.Functions.Components;
using RTFunctions.Functions.Managers;

using EditorManagement.Functions.Components;
using EditorManagement.Functions.Editors;
using EditorManagement.Functions.Helpers;
using EditorManagement.Functions;

namespace EditorManagement.Patchers
{
    [HarmonyPatch(typeof(MarkerEditor))]
    public class MarkerEditorPatch : MonoBehaviour
	{
		static MarkerEditor Instance { get => MarkerEditor.inst; set => MarkerEditor.inst = value; }

		static List<DataManager.GameData.BeatmapData.Marker> Markers => DataManager.inst.gameData.beatmapData.markers;

		static string className = "[<color=#FFAF38>MarkerEditor</color>] \n";

		[HarmonyPatch("Awake")]
		[HarmonyPrefix]
		static bool AwakePostfix(MarkerEditor __instance)
		{
			if (!Instance)
				Instance = __instance;
			else if (Instance != __instance)
            {
                Destroy(__instance.gameObject);
				return false;
			}

			Debug.Log($"{className}" +
				$"---------------------------------------------------------------------\n" +
				$"---------------------------- INITIALIZED ----------------------------\n" +
				$"---------------------------------------------------------------------\n");

			Instance.StartCoroutine(Wait());
			return false;
		}

		static IEnumerator Wait()
        {
			while (!EditorManager.inst)
				yield return null;

			var dialog = EditorManager.inst.GetDialog("Marker Editor").Dialog;

			Instance.left = dialog.Find("data/left");
			Instance.right = dialog.Find("data/right");

			var indexparent = new GameObject("index");
			indexparent.transform.SetParent(Instance.left);
			indexparent.transform.SetSiblingIndex(0);
			var rtindexpr = indexparent.AddComponent<RectTransform>();
			rtindexpr.pivot = new Vector2(0f, 1f);
			rtindexpr.sizeDelta = new Vector2(371f, 32f);

			var index = new GameObject("text");
			index.transform.parent = indexparent.transform;
			var rtindex = index.AddComponent<RectTransform>();
			index.AddComponent<CanvasRenderer>();
			var ttindex = index.AddComponent<Text>();

			rtindex.anchoredPosition = Vector2.zero;
			rtindex.anchorMax = Vector2.one;
			rtindex.anchorMin = Vector2.zero;
			rtindex.pivot = new Vector2(0f, 1f);
			rtindex.sizeDelta = Vector2.zero;

			ttindex.text = "Index: 0";
			ttindex.font = FontManager.inst.Inconsolata;
			ttindex.color = new Color(0.9f, 0.9f, 0.9f);
			ttindex.alignment = TextAnchor.MiddleLeft;
			ttindex.fontSize = 20;
			ttindex.horizontalOverflow = HorizontalWrapMode.Overflow;

			// Makes label consistent with other labels. Originally said "Marker Time" where other labels do not mention "Marker".
			Instance.left.GetChild(3).GetChild(0).GetComponent<Text>().text = "Time";
			// Fixes "Name" label.
			Instance.left.GetChild(5).GetChild(0).GetComponent<Text>().text = "Description";

			//EditorThemeManager.inst.AddElement(new EditorThemeManager.Element("MarkerEditor Background", "Background", dialog.gameObject, new List<Component> { dialog.GetComponent<Image>() }));
		}

		[HarmonyPatch("Update")]
		[HarmonyPostfix]
		static void UpdatePostfix()
		{
			var config = EditorConfig.Instance;

			if (config.MarkerLoopActive.Value && DataManager.inst.gameData.beatmapData.markers.Count > 0)
			{
				int markerStart = config.MarkerLoopBegin.Value;
				int markerEnd = config.MarkerLoopEnd.Value;

				if (markerStart < 0)
					markerStart = 0;
				if (markerStart > DataManager.inst.gameData.beatmapData.markers.Count - 1)
					markerStart = DataManager.inst.gameData.beatmapData.markers.Count - 1;

				if (markerEnd < 0)
					markerEnd = 0;
				if (markerEnd > DataManager.inst.gameData.beatmapData.markers.Count - 1)
					markerEnd = DataManager.inst.gameData.beatmapData.markers.Count - 1;

				if (AudioManager.inst.CurrentAudioSource.time > DataManager.inst.gameData.beatmapData.markers[markerEnd].time)
					AudioManager.inst.CurrentAudioSource.time = DataManager.inst.gameData.beatmapData.markers[markerStart].time;
			}
		}

		[HarmonyPatch("OpenDialog")]
		[HarmonyPrefix]
		static bool OpenDialogPostfix(int __0)
		{
			EditorManager.inst.ClearDialogs();
			EditorManager.inst.ShowDialog("Marker Editor");

			Instance.left.Find("color").GetComponent<GridLayoutGroup>().spacing = new Vector2(8f, 8f);
			Instance.left.Find("index/text").GetComponent<Text>().text = $"Index: {__0}";

			var marker = Markers[__0];

			var matchCollection = Regex.Matches(marker.desc, @"setLayer\((.*?)\)");

			if (matchCollection.Count > 0)
				foreach (var obj in matchCollection)
				{
					var match = (Match)obj;

					var matchGroup = match.Groups[1].ToString();
					if (matchGroup.ToLower() == "events" || matchGroup.ToLower() == "check" || matchGroup.ToLower() == "event/check" || matchGroup.ToLower() == "event")
						RTEditor.inst.SetLayer(RTEditor.LayerType.Events);
					else if (matchGroup.ToLower() == "object" || matchGroup.ToLower() == "objects")
						RTEditor.inst.SetLayer(RTEditor.LayerType.Objects);
					else if (matchGroup.ToLower() == "toggle" || matchGroup.ToLower() == "swap")
						RTEditor.inst.SetLayer(RTEditor.inst.layerType == RTEditor.LayerType.Objects ? RTEditor.LayerType.Events : RTEditor.LayerType.Objects);
					else if (int.TryParse(matchGroup, out int layer))
						RTEditor.inst.SetLayer(Mathf.Clamp(layer - 1, 0, int.MaxValue));
				}

			LSHelpers.DeleteChildren(Instance.left.Find("color"), false);
			int num = 0;
			foreach (var color in Instance.markerColors)
			{
				int index = num;
				var gameObject = EditorManager.inst.colorGUI.Duplicate(Instance.left.Find("color"), "marker color");
				gameObject.transform.localScale = Vector3.one;
				gameObject.transform.Find("Image").gameObject.SetActive(marker.color == index);
				var button = gameObject.GetComponent<Button>();
				((Image)button.targetGraphic).color = color;
				button.onClick.ClearAll();
				button.onClick.AddListener(delegate
				{
					Debug.Log($"{EditorManager.inst.className}Set Marker {__0}'s color to {index}");
					Instance.SetColor(index);
					Instance.UpdateColorSelection();
				});
				num++;
			}

			var name = Instance.left.Find("name").GetComponent<InputField>();
			name.onValueChanged.ClearAll();
			name.text = marker.name.ToString();
			name.onValueChanged.AddListener(delegate (string val)
			{
				Instance.SetName(val);
			});

			var desc = Instance.left.Find("desc").GetComponent<InputField>();
			desc.onValueChanged.ClearAll();
			desc.text = marker.desc.ToString();
			desc.onValueChanged.AddListener(delegate (string val)
			{
				Instance.SetDescription(val);
			});

			var time = Instance.left.Find("time/time").GetComponent<InputField>();
			time.onValueChanged.ClearAll();
			time.text = marker.time.ToString();
			time.onValueChanged.AddListener(delegate (string val)
			{
				Instance.SetTime(val);
			});

			TriggerHelper.AddEventTriggerParams(time.gameObject, TriggerHelper.ScrollDelta(time));
			TriggerHelper.IncreaseDecreaseButtons(time, t: Instance.left.Find("time"));

			var set = Instance.left.Find("time/|").GetComponent<Button>();
			set.onClick.ClearAll();
			set.onClick.AddListener(delegate
			{
				Instance.left.Find("time/time").GetComponent<InputField>().text = AudioManager.inst.CurrentAudioSource.time.ToString();
			});
			EditorManager.inst.ShowDialog("Marker Editor");
			Instance.UpdateMarkerList();
			Instance.RenderMarkers();

			return false;
		}

		[HarmonyPatch("RenderMarker")]
		[HarmonyPrefix]
		static bool RenderMarkersPostfix(int __0)
		{
			if (__0 >= 0 && Instance.markers.Count > __0)
			{
				var marker = Markers[__0];
				float time = marker.time;
				var markerObject = Instance.markers[__0];
				var markerColor = Instance.markerColors[Mathf.Clamp(marker.color, 0, Instance.markerColors.Count - 1)];

				var hoverTooltip = markerObject.GetComponent<HoverTooltip>();
				if (hoverTooltip)
				{
					hoverTooltip.tooltipLangauges.Clear();
					hoverTooltip.tooltipLangauges.Add(TooltipHelper.NewTooltip("<#" + LSColors.ColorToHex(markerColor) + ">" + marker.name + " [ " + marker.time + " ]</color>", marker.desc, new List<string>()));
				}

				((RectTransform)markerObject.transform).sizeDelta = new Vector2(12f, 12f);
				((RectTransform)markerObject.transform).anchoredPosition = new Vector2(time * EditorManager.inst.Zoom - 6f, -12f);
				markerObject.GetComponent<Image>().color = markerColor;
				markerObject.GetComponentInChildren<Text>().text = marker.name;
				markerObject.SetActive(true);
			}
			return false;
		}

		[HarmonyPatch("UpdateMarkerList")]
		[HarmonyPrefix]
		static bool UpdateMarkerList()
		{
			var parent = Instance.right.Find("markers/list");
			LSHelpers.DeleteChildren(parent);

			var eventButton = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject/event");

			//Delete Markers
			{
				var sortMarkers = eventButton.Duplicate(parent, "delete markers");

				sortMarkers.transform.GetChild(0).GetComponent<Text>().text = "Delete Markers";
				sortMarkers.GetComponent<Image>().color = new Color(1f, 0.131f, 0.231f, 1f);

				var sortButton = sortMarkers.GetComponent<Button>();
				sortButton.onClick.ClearAll();
				sortButton.onClick.AddListener(delegate ()
				{
					EditorManager.inst.ShowDialog("Warning Popup");
					RTEditor.inst.RefreshWarningPopup("Are you sure you want to delete ALL markers? (This is irreversible!)", delegate ()
					{
						EditorManager.inst.DisplayNotification($"Deleted {Markers.Count} markers!", 2f, EditorManager.NotificationType.Success);
						Markers.Clear();
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
					tt.tooltipLangauges.Add(TooltipHelper.NewTooltip("Delete all markers.", "Clicking this will delete every marker in the level.", new List<string>()));
				}
			}

			int num = 0;
			foreach (var marker in Markers)
			{
				if (marker.name.ToLower().Contains(Instance.sortedName.ToLower()) || marker.desc.ToLower().Contains(Instance.sortedName.ToLower()) || string.IsNullOrEmpty(Instance.sortedName))
				{
					var gameObject = Instance.markerButtonPrefab.Duplicate(parent, marker.name + "_marker");
					var name = gameObject.transform.Find("name").GetComponent<Text>();
					var pos = gameObject.transform.Find("pos").GetComponent<Text>();
					var image = gameObject.transform.Find("color").GetComponent<Image>();
					name.text = marker.name;
					pos.text = string.Format("{0:0}:{1:00}.{2:000}", Mathf.Floor(marker.time / 60f), Mathf.Floor(marker.time % 60f), Mathf.Floor(marker.time * 1000f % 1000f));
					image.color = Instance.markerColors[Mathf.Clamp(marker.color, 0, Instance.markerColors.Count - 1)];
					int markerIndexTmp = num;
					gameObject.GetComponent<Button>().onClick.AddListener(delegate ()
					{
						Instance.SetCurrentMarker(markerIndexTmp, true);
					});

					var markerColor = Instance.markerColors[Mathf.Clamp(marker.color, 0, Instance.markerColors.Count - 1)];
					TooltipHelper.AddTooltip(gameObject, "<#" + LSColors.ColorToHex(markerColor) + ">" + marker.name + " [ " + marker.time + " ]</color>", marker.desc, new List<string>());
				}
				num++;
			}
			return false;
		}

		[HarmonyPatch("CreateNewMarker", new Type[] { })]
        [HarmonyPrefix]
        static bool CreateNewMarkerPrefix()
        {
			Instance.CreateNewMarker(EditorManager.inst.CurrentAudioPos);
			return false;
        }

		[HarmonyPatch("CreateNewMarker", new Type[] { typeof(float) })]
		[HarmonyPrefix]
		static bool CreateNewMarkerPrefix(float __0)
		{
			int index;
			if (!Markers.Has(x => __0 > x.time - 0.01f && __0 < x.time + 0.01f))
			{
				var marker = new DataManager.GameData.BeatmapData.Marker();
				marker.time = __0;
				marker.name = "";
				Markers.Add(marker);
				index = Markers.Count - 1;
			}
			else
            {
				index = Markers.FindIndex(x => __0 > x.time - 0.01f && __0 < x.time + 0.01f);
			}

			Instance.CreateMarkers();
			Instance.SetCurrentMarker(index);
			Instance.UpdateMarkerList();
			return false;
		}

		[HarmonyPatch("UpdateColorSelection")]
		[HarmonyPrefix]
		static bool UpdateColorSelectionPrefix()
		{
			var marker = Markers[Instance.currentMarker];
			int num = 0;
			foreach (var color in Instance.markerColors)
			{
				Instance.left.Find("color").GetChild(num).Find("Image").gameObject.SetActive(marker.color == num);
				num++;
			}
			return false;
		}

		[HarmonyPatch("DeleteMarker")]
		[HarmonyPrefix]
		static bool DeleteMarkerPrefix(int __0)
		{
			Markers.RemoveAt(__0);
			if (Markers.Count > 0)
				Instance.UpdateMarkerList();
			else
				CheckpointEditor.inst.SetCurrentCheckpoint(0);
			Instance.CreateMarkers();
			return false;
		}
	}
}
