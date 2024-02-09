using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using EditorManagement.Patchers;
using EditorManagement.Functions;
using EditorManagement.Functions.Editors;

using RTFunctions.Functions;
using RTFunctions.Functions.Components;
using RTFunctions.Functions.Components.Player;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;

namespace EditorManagement
{
    [BepInPlugin("com.mecha.editormanagement", "EditorManagement", "2.3.4")]
    public class EditorPlugin : BaseUnityPlugin
    {
        public static EditorPlugin inst;
        public static string className = $"[<color=#F6AC1A>Editor</color><color=#2FCBD6>Management</color>] {PluginInfo.PLUGIN_VERSION}\n";
        readonly Harmony harmony = new Harmony("EditorManagement");

		public static List<int> allLayers = new List<int>();

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
			UpdateDefaultThemeValues();

			SelectGUI.DragGUI = RTEditor.GetEditorProperty("Drag UI").GetConfigEntry<bool>().Value;
			ObjectEditor.RenderPrefabTypeIcon = RTEditor.GetEditorProperty("Timeline Object Prefab Type Icon").GetConfigEntry<bool>().Value;
			ObjectEditor.TimelineObjectHoverSize = RTEditor.GetEditorProperty("Timeline Object Hover Size").GetConfigEntry<float>().Value;
			KeybindManager.AllowKeys = RTEditor.GetEditorProperty("Allow Editor Keybinds With Editor Cam").GetConfigEntry<bool>().Value;
			PrefabEditorManager.ImportPrefabsDirectly = RTEditor.GetEditorProperty("Import Prefabs Directly").GetConfigEntry<bool>().Value;

			SetupSettingChanged();

			Config.SettingChanged += new EventHandler<SettingChangedEventArgs>(UpdateEditorManagementConfigs);

			// Plugin startup logic
			Logger.LogInfo($"Plugin EditorManagement is loaded!");
        }

		void SetupSettingChanged()
		{
            RTEditor.GetEditorProperty("Timeline Grid Enabled").GetConfigEntry<bool>().SettingChanged += TimelineGridChanged;
            RTEditor.GetEditorProperty("Timeline Grid Thickness").GetConfigEntry<float>().SettingChanged += TimelineGridChanged;
            RTEditor.GetEditorProperty("Timeline Grid Color").GetConfigEntry<Color>().SettingChanged += TimelineGridChanged;
            RTEditor.GetEditorProperty("BPM Snap Divisions").GetConfigEntry<float>().SettingChanged += TimelineGridChanged;
			RTEditor.GetEditorProperty("Drag UI").GetConfigEntry<bool>().SettingChanged += DragUIChanged;
			RTEditor.GetEditorProperty("Theme Template Name").GetConfigEntry<string>().SettingChanged += ThemeTemplateChanged;
			RTEditor.GetEditorProperty("Theme Template GUI").GetConfigEntry<Color>().SettingChanged += ThemeTemplateChanged;
			RTEditor.GetEditorProperty("Theme Template Tail").GetConfigEntry<Color>().SettingChanged += ThemeTemplateChanged;
			RTEditor.GetEditorProperty("Theme Template BG").GetConfigEntry<Color>().SettingChanged += ThemeTemplateChanged;

			for (int i = 0; i < RTFunctions.Functions.Data.BeatmapTheme.DefaultPlayerColors.Count; i++)
			{
				try
				{
					if (RTEditor.EditorProperties.Has(x => x.name == $"Theme Template Player {i + 1}"))
						RTEditor.GetEditorProperty($"Theme Template Player {i + 1}").GetConfigEntry<Color>().SettingChanged += ThemeTemplateChanged;
				}
				catch
				{

				}
			}

			for (int i = 0; i < RTFunctions.Functions.Data.BeatmapTheme.DefaultObjectColors.Count; i++)
			{
				try
				{
					if (RTEditor.EditorProperties.Has(x => x.name == $"Theme Template OBJ {i + 1}"))
						RTEditor.GetEditorProperty($"Theme Template OBJ {i + 1}").GetConfigEntry<Color>().SettingChanged += ThemeTemplateChanged;
				}
				catch
				{

				}
			}

			for (int i = 0; i < RTFunctions.Functions.Data.BeatmapTheme.DefaulBackgroundColors.Count; i++)
			{
				try
				{
					if (RTEditor.EditorProperties.Has(x => x.name == $"Theme Template BG {i + 1}"))
						RTEditor.GetEditorProperty($"Theme Template BG {i + 1}").GetConfigEntry<Color>().SettingChanged += ThemeTemplateChanged;
				}
				catch
				{

				}
			}

			for (int i = 0; i < RTFunctions.Functions.Data.BeatmapTheme.DefaultEffectColors.Count; i++)
			{
				try
				{
					if (RTEditor.EditorProperties.Has(x => x.name == $"Theme Template FX {i + 1}"))
						RTEditor.GetEditorProperty($"Theme Template FX {i + 1}").GetConfigEntry<Color>().SettingChanged += ThemeTemplateChanged;
				}
				catch
				{

				}
			}
		}

        void TimelineGridChanged(object sender, EventArgs e)
        {
			RTEditor.inst.timelineGridRenderer.enabled = false;

			RTEditor.inst.timelineGridRenderer.color = RTEditor.GetEditorProperty("Timeline Grid Color").GetConfigEntry<Color>().Value;
			RTEditor.inst.timelineGridRenderer.thickness = RTEditor.GetEditorProperty("Timeline Grid Thickness").GetConfigEntry<float>().Value;

			RTEditor.inst.SetTimelineGridSize();
		}

		void ThemeTemplateChanged(object sender, EventArgs e) => UpdateDefaultThemeValues();

		void DragUIChanged(object sender, EventArgs e) => SelectGUI.DragGUI = RTEditor.GetEditorProperty("Drag UI").GetConfigEntry<bool>().Value;

		public static Action AdjustPositionInputsChanged { get; set; }

        void UpdateEditorManagementConfigs(object sender, SettingChangedEventArgs e)
		{
			SetPreviewConfig();

			KeybindManager.AllowKeys = RTEditor.GetEditorProperty("Allow Editor Keybinds With Editor Cam").GetConfigEntry<bool>().Value;
			ObjectEditor.RenderPrefabTypeIcon = RTEditor.GetEditorProperty("Timeline Object Prefab Type Icon").GetConfigEntry<bool>().Value;
			ObjectEditor.TimelineObjectHoverSize = RTEditor.GetEditorProperty("Timeline Object Hover Size").GetConfigEntry<float>().Value;
			PrefabEditorManager.ImportPrefabsDirectly = RTEditor.GetEditorProperty("Import Prefabs Directly").GetConfigEntry<bool>().Value;

			if (EditorManager.inst)
            {
				SetNotificationProperties();
				EditorManager.inst.zoomBounds = RTEditor.GetEditorProperty("Main Zoom Bounds").GetConfigEntry<Vector2>().Value;
				ObjEditor.inst.zoomBounds = RTEditor.GetEditorProperty("Keyframe Zoom Bounds").GetConfigEntry<Vector2>().Value;

				if (RTEditor.GetEditorProperty("Waveform Re-render").GetConfigEntry<bool>().Value)
                {
					RTEditor.inst.StartCoroutine(RTEditor.inst.AssignTimelineTexture());
                }

				SetTimelineColors();
				AdjustPositionInputsChanged?.Invoke();

				ObjEditor.inst.ObjectLengthOffset = RTEditor.GetEditorProperty("Keyframe End Length Offset").GetConfigEntry<float>().Value;
				if (RTEditor.inst.layerType == RTEditor.LayerType.Events)
                {
					RTEventEditor.inst.RenderLayerBins();
				}
			}
		}

		public static void SetPreviewConfig()
		{
			try
			{
				//if (!ModCompatibility.sharedFunctions.ContainsKey("HighlightColor"))
				//	ModCompatibility.sharedFunctions.Add("HighlightColor", RTEditor.GetEditorProperty("Object Highlight Amount").GetConfigEntry<Color>().Value);
				//else
				//	ModCompatibility.sharedFunctions["HighlightColor"] = RTEditor.GetEditorProperty("Object Highlight Amount").GetConfigEntry<Color>().Value;
				//if (!ModCompatibility.sharedFunctions.ContainsKey("HighlightDoubleColor"))
				//	ModCompatibility.sharedFunctions.Add("HighlightDoubleColor", RTEditor.GetEditorProperty("Object Highlight Double Amount").GetConfigEntry<Color>().Value);
				//else
				//	ModCompatibility.sharedFunctions["HighlightDoubleColor"] = RTEditor.GetEditorProperty("Object Highlight Double Amount").GetConfigEntry<Color>().Value;
				//if (!ModCompatibility.sharedFunctions.ContainsKey("CanHightlightObjects"))
				//	ModCompatibility.sharedFunctions.Add("CanHightlightObjects", RTEditor.GetEditorProperty("Highlight Objects").GetConfigEntry<bool>().Value);
				//else
				//	ModCompatibility.sharedFunctions["CanHightlightObjects"] = RTEditor.GetEditorProperty("Highlight Objects").GetConfigEntry<bool>().Value;

				//if (!ModCompatibility.sharedFunctions.ContainsKey("ShowObjectsAlpha"))
				//	ModCompatibility.sharedFunctions.Add("ShowObjectsAlpha", RTEditor.GetEditorProperty("Visible object opacity").GetConfigEntry<float>().Value);
				//else
				//	ModCompatibility.sharedFunctions["ShowObjectsAlpha"] = RTEditor.GetEditorProperty("Visible object opacity").GetConfigEntry<float>().Value;
				//if (!ModCompatibility.sharedFunctions.ContainsKey("ShowObjectsOnLayer"))
				//	ModCompatibility.sharedFunctions.Add("ShowObjectsOnLayer", RTEditor.GetEditorProperty("Only Objects on Current Layer Visible").GetConfigEntry<bool>().Value);
				//else
				//	ModCompatibility.sharedFunctions["ShowObjectsOnLayer"] = RTEditor.GetEditorProperty("Only Objects on Current Layer Visible").GetConfigEntry<bool>().Value;

				if (!ModCompatibility.sharedFunctions.ContainsKey("ShowEmpties"))
					ModCompatibility.sharedFunctions.Add("ShowEmpties", RTEditor.GetEditorProperty("Show Empties").GetConfigEntry<bool>().Value);
				else
					ModCompatibility.sharedFunctions["ShowEmpties"] = RTEditor.GetEditorProperty("Show Empties").GetConfigEntry<bool>().Value;
				if (!ModCompatibility.sharedFunctions.ContainsKey("ShowDamagable"))
					ModCompatibility.sharedFunctions.Add("ShowDamagable", RTEditor.GetEditorProperty("Only Show Damagable").GetConfigEntry<bool>().Value);
				else
					ModCompatibility.sharedFunctions["ShowDamagable"] = RTEditor.GetEditorProperty("Only Show Damagable").GetConfigEntry<bool>().Value;

                RTObject.Enabled = RTEditor.GetEditorProperty("Object Dragger Enabled").GetConfigEntry<bool>().Value;
				RTObject.HighlightColor = RTEditor.GetEditorProperty("Object Highlight Amount").GetConfigEntry<Color>().Value;
				RTObject.HighlightDoubleColor = RTEditor.GetEditorProperty("Object Highlight Double Amount").GetConfigEntry<Color>().Value;
				RTObject.HighlightObjects = RTEditor.GetEditorProperty("Highlight Objects").GetConfigEntry<bool>().Value;
				RTObject.ShowObjectsOnlyOnLayer = RTEditor.GetEditorProperty("Only Objects on Current Layer Visible").GetConfigEntry<bool>().Value;
				RTObject.LayerOpacity = RTEditor.GetEditorProperty("Visible object opacity").GetConfigEntry<float>().Value;

				RTRotator.RotatorRadius = RTEditor.GetEditorProperty("Object Dragger Rotator Radius").GetConfigEntry<float>().Value;
                RTScaler.ScalerOffset = RTEditor.GetEditorProperty("Object Dragger Scaler Offset").GetConfigEntry<float>().Value;
                RTScaler.ScalerScale = RTEditor.GetEditorProperty("Object Dragger Scaler Scale").GetConfigEntry<float>().Value;

				RTPlayer.ZenModeInEditor = RTEditor.GetEditorProperty("Editor Zen Mode").GetConfigEntry<bool>().Value;
			}
			catch (Exception ex)
			{
				inst.Logger.LogError($"SharedFunctions Error{ex}");
			}
		}

		public static void SetTimelineColors()
        {
			Debug.LogFormat($"{className}Setting Timeline Cursor Colors");
			{
				var timelineCursorColor = RTEditor.GetEditorProperty("Timeline Cursor Color").GetConfigEntry<Color>().Value;
				var KeyframeCursorColor = RTEditor.GetEditorProperty("Keyframe Cursor Color").GetConfigEntry<Color>().Value;

				if (RTEditor.inst.timelineSliderHandle)
					RTEditor.inst.timelineSliderHandle.color = timelineCursorColor;
				else
					Debug.LogError($"{className}Whoooops you gotta put this CD up your-");

				if (RTEditor.inst.timelineSliderRuler)
					RTEditor.inst.timelineSliderRuler.color = timelineCursorColor;
				else
					Debug.LogError($"{className}Whoooops you gotta put this CD up your-");

				if (RTEditor.inst.keyframeTimelineSliderHandle)
					RTEditor.inst.keyframeTimelineSliderHandle.color = KeyframeCursorColor;
				else
					Debug.LogError($"{className}Whoooops you gotta put this CD up your-");

				if (RTEditor.inst.keyframeTimelineSliderRuler)
					RTEditor.inst.keyframeTimelineSliderRuler.color = KeyframeCursorColor;
				else
					Debug.LogError($"{className}Whoooops you gotta put this CD up your-");
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
			notifyGroup.childAlignment = direction != Direction.Up ? TextAnchor.LowerLeft : TextAnchor.UpperLeft;
		}

		public static void UpdateDefaultThemeValues()
        {
			RTFunctions.Functions.Data.BeatmapTheme.DefaultName = RTEditor.GetEditorProperty("Theme Template Name").GetConfigEntry<string>().Value;
			RTFunctions.Functions.Data.BeatmapTheme.DefaultGUIColor = RTEditor.GetEditorProperty("Theme Template GUI").GetConfigEntry<Color>().Value;
			RTFunctions.Functions.Data.BeatmapTheme.DefaultTailColor = RTEditor.GetEditorProperty("Theme Template Tail").GetConfigEntry<Color>().Value;
			RTFunctions.Functions.Data.BeatmapTheme.DefaultBGColor = RTEditor.GetEditorProperty("Theme Template BG").GetConfigEntry<Color>().Value;

			for (int i = 0; i < RTFunctions.Functions.Data.BeatmapTheme.DefaultPlayerColors.Count; i++)
			{
				try
				{
					if (RTEditor.EditorProperties.Has(x => x.name == $"Theme Template Player {i + 1}"))
						RTFunctions.Functions.Data.BeatmapTheme.DefaultPlayerColors[i] = RTEditor.GetEditorProperty($"Theme Template Player {i + 1}").GetConfigEntry<Color>().Value;
				}
				catch (Exception ex)
				{

				}
			}

			for (int i = 0; i < RTFunctions.Functions.Data.BeatmapTheme.DefaultObjectColors.Count; i++)
			{
				try
				{
					if (RTEditor.EditorProperties.Has(x => x.name == $"Theme Template OBJ {i + 1}"))
						RTFunctions.Functions.Data.BeatmapTheme.DefaultObjectColors[i] = RTEditor.GetEditorProperty($"Theme Template OBJ {i + 1}").GetConfigEntry<Color>().Value;
				}
				catch (Exception ex)
				{

				}
			}

			for (int i = 0; i < RTFunctions.Functions.Data.BeatmapTheme.DefaulBackgroundColors.Count; i++)
			{
				try
				{
					if (RTEditor.EditorProperties.Has(x => x.name == $"Theme Template BG {i + 1}"))
						RTFunctions.Functions.Data.BeatmapTheme.DefaulBackgroundColors[i] = RTEditor.GetEditorProperty($"Theme Template BG {i + 1}").GetConfigEntry<Color>().Value;
				}
				catch (Exception ex)
				{

				}
			}

			for (int i = 0; i < RTFunctions.Functions.Data.BeatmapTheme.DefaultEffectColors.Count; i++)
			{
				try
				{
					if (RTEditor.EditorProperties.Has(x => x.name == $"Theme Template FX {i + 1}"))
						RTFunctions.Functions.Data.BeatmapTheme.DefaultEffectColors[i] = RTEditor.GetEditorProperty($"Theme Template FX {i + 1}").GetConfigEntry<Color>().Value;
				}
				catch (Exception ex)
				{

				}
			}
		}

		public static void ListObjectLayers()
		{
			allLayers.Clear();
			foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
			{
				if (!allLayers.Contains(beatmapObject.editorData.layer))
					allLayers.Add(beatmapObject.editorData.layer);
			}

			foreach (var prefabObject in DataManager.inst.gameData.prefabObjects)
            {
				if (!allLayers.Contains(prefabObject.editorData.layer))
					allLayers.Add(prefabObject.editorData.layer);
            }

			allLayers = (from x in allLayers
						 orderby x ascending
						 select x).ToList();

			string lister = "";

			int i = 0;
			foreach (var l in allLayers)
			{
				int num = l + 1;
				if (!lister.Contains(num.ToString()))
				{
					lister += num.ToString();
					if (i != allLayers.Count - 1)
						lister += ", ";
				}
				i++;
			}

			EditorManager.inst.DisplayNotification($"Objects on Layers:<br>[ {lister} ]", 2f, EditorManager.NotificationType.Info);
		}

		public static bool DontRun() => false;
	}


	[HarmonyPatch(typeof(Dropdown))]
	public class DropdownPatch
	{
		[HarmonyPatch("Show")]
		[HarmonyPostfix]
		static void ShowPostfix(Dropdown __instance)
		{
			if (__instance.gameObject && __instance.transform && __instance.gameObject.TryGetComponent(out HideDropdownOptions hideDropdownOptions))
			{
				var content = __instance.transform.Find("Dropdown List/Viewport/Content");
				Debug.Log(content);
				for (int i = 0; i < hideDropdownOptions.DisabledOptions.Count; i++)
				{
					if (hideDropdownOptions.remove && hideDropdownOptions.DisabledOptions[i])
					{
						if (content.childCount > i + 1)
							content.GetChild(i + 1).gameObject.SetActive(false);
					}
					else
					{
						if (content.childCount > i + 1)
							content.GetChild(i + 1).GetComponent<Toggle>().interactable = !hideDropdownOptions.DisabledOptions[i];
					}
					if (content.childCount > i + 1)
						content.GetChild(i + 1).AsRT().sizeDelta = new Vector2(0f, 32f);
				}
			}
		}
	}

	[HarmonyPatch(typeof(HideDropdownOptions))]
	public class HideDropdownOptionsPatch
	{
		[HarmonyPatch("OnPointerClick")]
		[HarmonyPrefix]
		static bool OnPointerClickPrefix(HideDropdownOptions __instance, PointerEventData __0)
		{
			return false;
		}
	}

	[HarmonyPatch(typeof(LSFunctions.LSHelpers))]
	public class LSHelpersPatch
    {
		[HarmonyPatch("IsUsingInputField")]
		[HarmonyPrefix]
		static bool IsUsingInputFieldPrefix(ref bool __result)
		{
			__result = EventSystem.current && EventSystem.current.currentSelectedGameObject &&
				(EventSystem.current.currentSelectedGameObject.GetComponentInChildren<InputField>() || EventSystem.current.currentSelectedGameObject.GetComponentInChildren<TMPro.TMP_InputField>());
			return false;
		}
	}
}
