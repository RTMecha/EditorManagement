using System;
using System.Collections.Generic;
using System.Linq;

using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

using UnityEngine;
using UnityEngine.UI;

using EditorManagement.Patchers;
using EditorManagement.Functions;
using EditorManagement.Functions.Editors;

using RTFunctions.Functions;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;

namespace EditorManagement
{
    [BepInPlugin("com.mecha.editormanagement", "EditorManagement", "2.0.0")]
    public class EditorPlugin : BaseUnityPlugin
    {
        // TO DO BEFORE RELEASE
        // Replace "settings" with a duplicate of the other dropdowns and put the "Level Settings" as one of the tabs.
		// Redo keyframe system because it's really annoying to deal with internal "type" and "index" stuff. Instead gotta make a wrapped list...
		// And probably redo the keyframe creation, rendering and dragging system.
		// Add Copy Keyframe Data button.
		// Redo every Hovertooltip.

        public static EditorPlugin inst;
        public static string className = $"[<color=#F6AC1A>Editor</color><color=#2FCBD6>Management</color>] {PluginInfo.PLUGIN_VERSION}\n";
        readonly Harmony harmony = new Harmony("EditorManagement");

		public static List<int> allLayers = new List<int>();

		public static float timeOffset;
        public static float timeEditing;
		public static int openAmount;

        public static int levelFilter = 0;
        public static bool levelAscend = true;

        void Awake()
        {
            inst = this;

            try
			{
				harmony.PatchAll();
			}
			catch (Exception ex)
            {
				Logger.LogError("PatchAll Error" + ex.ToString());
            }
			
            try
			{
				if (!ModCompatibility.mods.ContainsKey("EditorManagement"))
				{
					var mod = new ModCompatibility.Mod(this, GetType());
					ModCompatibility.mods.Add("EditorManagement", mod);
				}
			}
			catch (Exception ex)
            {
				Logger.LogError("Mod Error" + ex.ToString());
            }

			SetPreviewConfig();

			RTFunctions.Functions.Components.SelectGUI.DragGUI = RTEditor.GetEditorProperty("Drag UI").GetConfigEntry<bool>().Value;

			Config.SettingChanged += new EventHandler<SettingChangedEventArgs>(UpdateEditorManagementConfigs);

			// Plugin startup logic
			Logger.LogInfo($"Plugin EditorManagement is loaded!");
        }

        void UpdateEditorManagementConfigs(object sender, SettingChangedEventArgs e)
		{
			SetPreviewConfig();
			if (EditorManager.inst)
            {
				SetNotificationProperties();
				EditorManager.inst.zoomBounds = RTEditor.GetEditorProperty("Main Zoom Bounds").GetConfigEntry<Vector2>().Value;
				ObjEditor.inst.zoomBounds = RTEditor.GetEditorProperty("Keyframe Zoom Bounds").GetConfigEntry<Vector2>().Value;
			}
		}

		public static void SetPreviewConfig()
		{
			try
			{
				if (!ModCompatibility.sharedFunctions.ContainsKey("HighlightColor"))
					ModCompatibility.sharedFunctions.Add("HighlightColor", RTEditor.GetEditorProperty("Object Highlight Amount").GetConfigEntry<Color>().Value);
				else
					ModCompatibility.sharedFunctions["HighlightColor"] = RTEditor.GetEditorProperty("Object Highlight Amount").GetConfigEntry<Color>().Value;
				if (!ModCompatibility.sharedFunctions.ContainsKey("HighlightDoubleColor"))
					ModCompatibility.sharedFunctions.Add("HighlightDoubleColor", RTEditor.GetEditorProperty("Object Highlight Double Amount").GetConfigEntry<Color>().Value);
				else
					ModCompatibility.sharedFunctions["HighlightDoubleColor"] = RTEditor.GetEditorProperty("Object Highlight Double Amount").GetConfigEntry<Color>().Value;
				if (!ModCompatibility.sharedFunctions.ContainsKey("CanHightlightObjects"))
					ModCompatibility.sharedFunctions.Add("CanHightlightObjects", RTEditor.GetEditorProperty("Highlight Objects").GetConfigEntry<bool>().Value);
				else
					ModCompatibility.sharedFunctions["CanHightlightObjects"] = RTEditor.GetEditorProperty("Highlight Objects").GetConfigEntry<bool>().Value;

				if (!ModCompatibility.sharedFunctions.ContainsKey("ShowObjectsAlpha"))
					ModCompatibility.sharedFunctions.Add("ShowObjectsAlpha", RTEditor.GetEditorProperty("Visible object opacity").GetConfigEntry<float>().Value);
				else
					ModCompatibility.sharedFunctions["ShowObjectsAlpha"] = RTEditor.GetEditorProperty("Visible object opacity").GetConfigEntry<float>().Value;
				if (!ModCompatibility.sharedFunctions.ContainsKey("ShowObjectsOnLayer"))
					ModCompatibility.sharedFunctions.Add("ShowObjectsOnLayer", RTEditor.GetEditorProperty("Only Objects on Current Layer Visible").GetConfigEntry<bool>().Value);
				else
					ModCompatibility.sharedFunctions["ShowObjectsOnLayer"] = RTEditor.GetEditorProperty("Only Objects on Current Layer Visible").GetConfigEntry<bool>().Value;
				if (!ModCompatibility.sharedFunctions.ContainsKey("ShowEmpties"))
					ModCompatibility.sharedFunctions.Add("ShowEmpties", RTEditor.GetEditorProperty("Show Empties").GetConfigEntry<bool>().Value);
				else
					ModCompatibility.sharedFunctions["ShowEmpties"] = RTEditor.GetEditorProperty("Show Empties").GetConfigEntry<bool>().Value;
				if (!ModCompatibility.sharedFunctions.ContainsKey("ShowDamagable"))
					ModCompatibility.sharedFunctions.Add("ShowDamagable", RTEditor.GetEditorProperty("Only Show Damagable").GetConfigEntry<bool>().Value);
				else
					ModCompatibility.sharedFunctions["ShowDamagable"] = RTEditor.GetEditorProperty("Only Show Damagable").GetConfigEntry<bool>().Value;
			}
			catch (Exception ex)
			{
				inst.Logger.LogError($"SharedFunctions Error{ex}");
			}
		}

		public static void SetTimelineColors()
        {
			Debug.LogFormat("{0}Setting Timeline Cursor Colors", className);
			{
				Debug.LogFormat("{0}Cursor Color Handle 1", EditorPlugin.className);
				if (RTExtensions.TryFind("Editor Systems/Editor GUI/sizer/main/whole-timeline/Slider_Parent/Slider/Handle Slide Area/Image/Handle", out GameObject gm) && gm.TryGetComponent(out Image image))
				{
					RTEditor.inst.timelineSliderHandle = image;
					RTEditor.inst.timelineSliderHandle.color = RTEditor.GetEditorProperty("Timeline Cursor Color").GetConfigEntry<Color>().Value;
				}
				else
				{
					Debug.LogFormat("{0}Whoooops you gotta put this CD up your-", EditorPlugin.className);
				}

				Debug.LogFormat("{0}Cursor Color Ruler 1", EditorPlugin.className);
				if (RTExtensions.TryFind("Editor Systems/Editor GUI/sizer/main/whole-timeline/Slider_Parent/Slider/Handle Slide Area/Image", out GameObject gm1) && gm1.TryGetComponent(out Image image1))
				{
					RTEditor.inst.timelineSliderRuler = image1;
					RTEditor.inst.timelineSliderRuler.color = RTEditor.GetEditorProperty("Timeline Cursor Color").GetConfigEntry<Color>().Value;
				}
				else
				{
					Debug.LogFormat("{0}Whoooops you gotta put this CD up your-", EditorPlugin.className);
				}

				Debug.LogFormat("{0}Cursor Color Handle 2", EditorPlugin.className);
				if (RTExtensions.TryFind("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/timeline/Scroll View/Viewport/Content/time_slider/Handle Slide Area/Handle/Image", out GameObject gm2) && gm2.TryGetComponent(out Image image2))
				{
					RTEditor.inst.keyframeTimelineSliderHandle = image2;
					RTEditor.inst.keyframeTimelineSliderHandle.color = RTEditor.GetEditorProperty("Keyframe Cursor Color").GetConfigEntry<Color>().Value;
				}
				else
				{
					Debug.LogFormat("{0}Whoooops you gotta put this CD up your-", EditorPlugin.className);
				}

				Debug.LogFormat("{0}Cursor Color Ruler 2", EditorPlugin.className);
				if (RTExtensions.TryFind("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/timeline/Scroll View/Viewport/Content/time_slider/Handle Slide Area/Handle", out GameObject gm3) && gm3.TryGetComponent(out Image image3))
				{
					RTEditor.inst.keyframeTimelineSliderRuler = image3;
					RTEditor.inst.keyframeTimelineSliderRuler.color = RTEditor.GetEditorProperty("Keyframe Cursor Color").GetConfigEntry<Color>().Value;
				}
				else
				{
					Debug.LogFormat("{0}Whoooops you gotta put this CD up your-", EditorPlugin.className);
				}
			}

			ObjEditor.inst.SelectedColor = RTEditor.GetEditorProperty("Object Selection Color").GetConfigEntry<Color>().Value;
        }

		public static void SetNotificationProperties()
		{
			Debug.Log($"{className}Setting Notification values");
			var notifyRT = EditorManager.inst.notification.GetComponent<RectTransform>();
			var notifyGroup = EditorManager.inst.notification.GetComponent<VerticalLayoutGroup>();
			notifyRT.sizeDelta = new Vector2(RTEditor.GetEditorProperty("Notification Width").GetConfigEntry<float>().Value, 632f);
			EditorManager.inst.notification.transform.localScale =
				new Vector3(
					RTEditor.GetEditorProperty("Notification Size").GetConfigEntry<float>().Value,
					RTEditor.GetEditorProperty("Notification Size").GetConfigEntry<float>().Value,
					1f);

			var direction = RTEditor.GetEditorProperty("Notification Direction").GetConfigEntry<Direction>().Value;

			notifyRT.anchoredPosition = new Vector2(8f, direction == Direction.Up ? 408f : 410f);
			notifyGroup.childAlignment = direction == Direction.Up ? TextAnchor.LowerLeft : TextAnchor.UpperLeft;
		}

		public static void ListObjectLayers()
		{
			allLayers.Clear();
			foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
			{
				if (!allLayers.Contains(beatmapObject.editorData.Layer))
				{
					allLayers.Add(beatmapObject.editorData.Layer);
				}
			}

			allLayers = (from x in allLayers
						 orderby x ascending
						 select x).ToList();

			string lister = "";

			int i = 0;
			foreach (var l in allLayers)
			{
				int num = l + 1;
				if (num > 5)
				{
					num -= 1;
				}
				if (!lister.Contains(num.ToString()))
				{
					lister += num.ToString();
					if (i != allLayers.Count - 1)
						lister += ", ";
				}
				i++;
			}

			EditorManager.inst.DisplayNotification("Objects on Layers:<br>[ " + lister + " ]", 2f, EditorManager.NotificationType.Info);
		}

		public static bool DontRun() => false;
    }
}
