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
using RTFunctions.Functions.Data;

namespace EditorManagement
{
    [BepInPlugin("com.mecha.editormanagement", "EditorManagement", "2.5.0")]
    public class EditorPlugin : BaseUnityPlugin
    {
        public static EditorPlugin inst;
        public static string className = $"[<color=#F6AC1A>Editor</color><color=#2FCBD6>Management</color>] {PluginInfo.PLUGIN_VERSION}\n";
        readonly Harmony harmony = new Harmony("EditorManagement");

		public static List<int> allLayers = new List<int>();

		public static EditorConfig EditorConfig { get; set; }

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
				Logger.LogError($"Mod failed to register to the ModCompatibility. \nException: {ex}");
				throw;
            }

            try
            {
				EditorConfig = new EditorConfig(Config);
			}
            catch (Exception ex)
            {
				Logger.LogError($"Mod failed to initialize configs.\nException: {ex}");
				throw;
            }

			SetPreviewConfig();
			UpdateDefaultThemeValues();

			SelectGUI.DragGUI = EditorConfig.DragUI.Value;
			ObjectEditor.RenderPrefabTypeIcon = EditorConfig.TimelineObjectPrefabTypeIcon.Value;
			ObjectEditor.TimelineObjectHoverSize = EditorConfig.TimelineObjectHoverSize.Value;
			ObjectEditor.HideVisualElementsWhenObjectIsEmpty = EditorConfig.HideVisualElementsWhenObjectIsEmpty.Value;
			KeybindManager.AllowKeys = EditorConfig.AllowEditorKeybindsWithEditorCam.Value;
			PrefabEditorManager.ImportPrefabsDirectly = EditorConfig.ImportPrefabsDirectly.Value;
			ThemeEditorManager.themesPerPage = EditorConfig.ThemesPerPage.Value;
			RTEditor.DraggingPlaysSound = EditorConfig.DraggingPlaysSound.Value;
			RTEditor.DraggingPlaysSoundBPM = EditorConfig.DraggingPlaysSoundOnlyWithBPM.Value;
			RTEditor.ShowModdedUI = EditorConfig.ShowModdedFeaturesInEditor.Value;
			EditorThemeManager.currentTheme = (int)EditorConfig.EditorTheme.Value;

			SetupSettingChanged();

			Config.SettingChanged += new EventHandler<SettingChangedEventArgs>(UpdateEditorManagementConfigs);

			// Plugin startup logic
			Logger.LogInfo($"Plugin EditorManagement is loaded!");
        }

		void Update()
        {
			EditorThemeManager.Update();
        }

		void SetupSettingChanged()
		{
			EditorConfig.TimelineGridEnabled.SettingChanged += TimelineGridChanged;
			EditorConfig.TimelineGridThickness.SettingChanged += TimelineGridChanged;
			EditorConfig.TimelineGridColor.SettingChanged += TimelineGridChanged;

			EditorConfig.BPMSnapDivisions.SettingChanged += TimelineGridChanged;
			EditorConfig.DragUI.SettingChanged += DragUIChanged;

			EditorConfig.HideVisualElementsWhenObjectIsEmpty.SettingChanged += ObjectEditorChanged;
			EditorConfig.KeyframeZoomBounds.SettingChanged += ObjectEditorChanged;
			EditorConfig.ObjectSelectionColor.SettingChanged += ObjectEditorChanged;

			EditorConfig.ThemeTemplateName.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateGUI.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateTail.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateBG.SettingChanged += ThemeTemplateChanged;

			EditorConfig.ThemeTemplatePlayer1.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplatePlayer2.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplatePlayer3.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplatePlayer4.SettingChanged += ThemeTemplateChanged;

			EditorConfig.ThemeTemplateOBJ1.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateOBJ2.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateOBJ3.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateOBJ4.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateOBJ5.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateOBJ6.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateOBJ7.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateOBJ8.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateOBJ9.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateOBJ10.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateOBJ11.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateOBJ12.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateOBJ13.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateOBJ14.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateOBJ15.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateOBJ16.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateOBJ17.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateOBJ18.SettingChanged += ThemeTemplateChanged;

			EditorConfig.ThemeTemplateBG1.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateBG2.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateBG3.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateBG4.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateBG5.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateBG6.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateBG7.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateBG8.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateBG9.SettingChanged += ThemeTemplateChanged;

			EditorConfig.ThemeTemplateFX1.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateFX2.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateFX3.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateFX4.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateFX5.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateFX6.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateFX7.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateFX8.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateFX9.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateFX10.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateFX11.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateFX12.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateFX13.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateFX14.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateFX15.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateFX16.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateFX17.SettingChanged += ThemeTemplateChanged;
			EditorConfig.ThemeTemplateFX18.SettingChanged += ThemeTemplateChanged;

            EditorConfig.ThemesPerPage.SettingChanged += ThemePopupChanged;

            EditorConfig.WaveformRerender.SettingChanged += TimelineWaveformChanged;

            EditorConfig.DraggingPlaysSound.SettingChanged += DraggingChanged;
            EditorConfig.DraggingPlaysSoundOnlyWithBPM.SettingChanged += DraggingChanged;

            EditorConfig.ShowModdedFeaturesInEditor.SettingChanged += ModdedEditorChanged;

            EditorConfig.MarkerLineColor.SettingChanged += MarkerChanged;
			EditorConfig.MarkerLineWidth.SettingChanged += MarkerChanged;
			EditorConfig.MarkerTextWidth.SettingChanged += MarkerChanged;

            EditorConfig.EditorTheme.SettingChanged += EditorThemeChanged;
            EditorConfig.RoundedUI.SettingChanged += EditorThemeChanged;

            EditorConfig.AutosaveLoopTime.SettingChanged += AutosaveChanged;
		}

        void AutosaveChanged(object sender, EventArgs e)
        {
			if (EditorManager.inst && EditorManager.inst.hasLoadedLevel)
				RTEditor.inst.SetAutoSave();
        }

        void EditorThemeChanged(object sender, EventArgs e)
        {
			EditorThemeManager.currentTheme = (int)EditorConfig.EditorTheme.Value;
			StartCoroutine(EditorThemeManager.RenderElements());
        }

        void MarkerChanged(object sender, EventArgs e)
        {
			MarkerEditor.inst?.RenderMarkers();
        }

        void ModdedEditorChanged(object sender, EventArgs e)
		{
			RTEditor.ShowModdedUI = EditorConfig.ShowModdedFeaturesInEditor.Value;

			AdjustPositionInputsChanged?.Invoke();

			if (ObjectEditor.inst && ObjectEditor.inst.SelectedObjectCount == 1 && ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
            {
				StartCoroutine(ObjectEditor.RefreshObjectGUI(ObjectEditor.inst.CurrentSelection.GetData<RTFunctions.Functions.Data.BeatmapObject>()));
            }

			if (RTEditor.inst && RTEditor.inst.layerType == RTEditor.LayerType.Events)
            {
				RTEventEditor.inst.RenderLayerBins();
				if (EventEditor.inst.dialogRight.gameObject.activeInHierarchy)
					RTEventEditor.inst.RenderEventsDialog();
			}

			if (PrefabEditorManager.inst)
			{
				var prefabSelectorLeft = EditorManager.inst.GetDialog("Prefab Selector").Dialog.Find("data/left");

				if (!prefabSelectorLeft.gameObject.activeInHierarchy)
					PrefabEditorManager.inst.UpdateModdedVisbility();
				else if (ObjectEditor.inst.CurrentSelection.IsPrefabObject)
					PrefabEditorManager.inst.RenderPrefabObjectDialog(ObjectEditor.inst.CurrentSelection.GetData<PrefabObject>(), PrefabEditor.inst);
			}
		}

		void DraggingChanged(object sender, EventArgs e)
        {
			RTEditor.DraggingPlaysSound = EditorConfig.DraggingPlaysSound.Value;
			RTEditor.DraggingPlaysSoundBPM = EditorConfig.DraggingPlaysSoundOnlyWithBPM.Value;
		}

        void TimelineWaveformChanged(object sender, EventArgs e)
		{
			if (EditorConfig.WaveformRerender.Value)
			{
				RTEditor.inst.StartCoroutine(RTEditor.inst.AssignTimelineTexture());
			}
		}

		void ThemePopupChanged(object sender, EventArgs e)
		{
			ThemeEditorManager.themesPerPage = EditorConfig.ThemesPerPage.Value;
		}

        void ObjectEditorChanged(object sender, EventArgs e)
        {
			ObjectEditor.HideVisualElementsWhenObjectIsEmpty = EditorConfig.HideVisualElementsWhenObjectIsEmpty.Value;

			if (ObjEditor.inst)
            {
				ObjEditor.inst.zoomBounds = EditorConfig.KeyframeZoomBounds.Value;
				ObjEditor.inst.ObjectLengthOffset = EditorConfig.KeyframeEndLengthOffset.Value;
				ObjEditor.inst.SelectedColor = EditorConfig.ObjectSelectionColor.Value;
			}
		}

        void TimelineGridChanged(object sender, EventArgs e)
        {
			RTEditor.inst.timelineGridRenderer.enabled = false;

			RTEditor.inst.timelineGridRenderer.color = EditorConfig.TimelineGridColor.Value;
			RTEditor.inst.timelineGridRenderer.thickness = EditorConfig.TimelineGridThickness.Value;

			RTEditor.inst.SetTimelineGridSize();
		}

		void ThemeTemplateChanged(object sender, EventArgs e) => UpdateDefaultThemeValues();

		void DragUIChanged(object sender, EventArgs e) => SelectGUI.DragGUI = EditorConfig.DragUI.Value;

		public static Action AdjustPositionInputsChanged { get; set; }

        void UpdateEditorManagementConfigs(object sender, SettingChangedEventArgs e)
		{
			SetPreviewConfig();

			KeybindManager.AllowKeys = EditorConfig.AllowEditorKeybindsWithEditorCam.Value;
			ObjectEditor.RenderPrefabTypeIcon = EditorConfig.TimelineObjectPrefabTypeIcon.Value;
			ObjectEditor.TimelineObjectHoverSize = EditorConfig.TimelineObjectHoverSize.Value;
			PrefabEditorManager.ImportPrefabsDirectly = EditorConfig.ImportPrefabsDirectly.Value;

			if (EditorManager.inst)
            {
				SetNotificationProperties();
				EditorManager.inst.zoomBounds = EditorConfig.MainZoomBounds.Value;

				SetTimelineColors();
				AdjustPositionInputsChanged?.Invoke();

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
				ModCompatibility.sharedFunctions.AddSet("ShowEmpties", EditorConfig.ShowEmpties.Value);
				ModCompatibility.sharedFunctions.AddSet("ShowDamagable", EditorConfig.OnlyShowDamagable.Value);

                RTObject.Enabled = EditorConfig.ObjectDraggerEnabled.Value;
				RTObject.HighlightColor = EditorConfig.ObjectHighlightAmount.Value;
				RTObject.HighlightDoubleColor = EditorConfig.ObjectHighlightDoubleAmount.Value;
				RTObject.HighlightObjects = EditorConfig.HighlightObjects.Value;
				RTObject.ShowObjectsOnlyOnLayer = EditorConfig.OnlyObjectsOnCurrentLayerVisible.Value;
				RTObject.LayerOpacity = EditorConfig.VisibleObjectOpacity.Value;

				RTRotator.RotatorRadius = EditorConfig.ObjectDraggerRotatorRadius.Value;
                RTScaler.ScalerOffset = EditorConfig.ObjectDraggerScalerOffset.Value;
                RTScaler.ScalerScale = EditorConfig.ObjectDraggerScalerScale.Value;

				RTPlayer.ZenModeInEditor = EditorConfig.EditorZenMode.Value;
			}
			catch (Exception ex)
			{
				inst.Logger.LogError($"SharedFunctions Error{ex}");
			}
		}

		public static void SetTimelineColors()
		{
			var timelineCursorColor = EditorConfig.TimelineCursorColor.Value;
			var KeyframeCursorColor = EditorConfig.KeyframeCursorColor.Value;

			RTEditor.inst.timelineSliderHandle.color = timelineCursorColor;

			RTEditor.inst.timelineSliderRuler.color = timelineCursorColor;

			RTEditor.inst.keyframeTimelineSliderHandle.color = KeyframeCursorColor;
			RTEditor.inst.keyframeTimelineSliderRuler.color = KeyframeCursorColor;
		}

		public static void SetNotificationProperties()
		{
			Debug.Log($"{className}Setting Notification values");
			var notifyRT = EditorManager.inst.notification.transform.AsRT();
			var notifyGroup = EditorManager.inst.notification.GetComponent<VerticalLayoutGroup>();
			notifyRT.sizeDelta = new Vector2(EditorConfig.NotificationWidth.Value, 632f);
			EditorManager.inst.notification.transform.localScale =
				new Vector3(EditorConfig.NotificationSize.Value, EditorConfig.NotificationSize.Value,
					1f);

			var direction = EditorConfig.NotificationDirection.Value;

			notifyRT.anchoredPosition = new Vector2(8f, direction == Direction.Up ? 408f : 410f);
			notifyGroup.childAlignment = direction != Direction.Up ? TextAnchor.LowerLeft : TextAnchor.UpperLeft;
		}

		public static void UpdateDefaultThemeValues()
        {
            BeatmapTheme.DefaultName = EditorConfig.ThemeTemplateName.Value;
            BeatmapTheme.DefaultGUIColor = EditorConfig.ThemeTemplateGUI.Value;
            BeatmapTheme.DefaultTailColor = EditorConfig.ThemeTemplateTail.Value;
            BeatmapTheme.DefaultBGColor = EditorConfig.ThemeTemplateBG.Value;

            try
			{
				BeatmapTheme.DefaultPlayerColors[0] = EditorConfig.ThemeTemplatePlayer1.Value;
				BeatmapTheme.DefaultPlayerColors[1] = EditorConfig.ThemeTemplatePlayer2.Value;
				BeatmapTheme.DefaultPlayerColors[2] = EditorConfig.ThemeTemplatePlayer3.Value;
				BeatmapTheme.DefaultPlayerColors[3] = EditorConfig.ThemeTemplatePlayer4.Value;

				BeatmapTheme.DefaultObjectColors[0] = EditorConfig.ThemeTemplateOBJ1.Value;
				BeatmapTheme.DefaultObjectColors[1] = EditorConfig.ThemeTemplateOBJ2.Value;
				BeatmapTheme.DefaultObjectColors[2] = EditorConfig.ThemeTemplateOBJ3.Value;
				BeatmapTheme.DefaultObjectColors[3] = EditorConfig.ThemeTemplateOBJ4.Value;
				BeatmapTheme.DefaultObjectColors[4] = EditorConfig.ThemeTemplateOBJ5.Value;
				BeatmapTheme.DefaultObjectColors[5] = EditorConfig.ThemeTemplateOBJ6.Value;
				BeatmapTheme.DefaultObjectColors[6] = EditorConfig.ThemeTemplateOBJ7.Value;
				BeatmapTheme.DefaultObjectColors[7] = EditorConfig.ThemeTemplateOBJ8.Value;
				BeatmapTheme.DefaultObjectColors[8] = EditorConfig.ThemeTemplateOBJ9.Value;
				BeatmapTheme.DefaultObjectColors[9] = EditorConfig.ThemeTemplateOBJ10.Value;
				BeatmapTheme.DefaultObjectColors[10] = EditorConfig.ThemeTemplateOBJ11.Value;
				BeatmapTheme.DefaultObjectColors[11] = EditorConfig.ThemeTemplateOBJ12.Value;
				BeatmapTheme.DefaultObjectColors[12] = EditorConfig.ThemeTemplateOBJ13.Value;
				BeatmapTheme.DefaultObjectColors[13] = EditorConfig.ThemeTemplateOBJ14.Value;
				BeatmapTheme.DefaultObjectColors[14] = EditorConfig.ThemeTemplateOBJ15.Value;
				BeatmapTheme.DefaultObjectColors[15] = EditorConfig.ThemeTemplateOBJ16.Value;
				BeatmapTheme.DefaultObjectColors[16] = EditorConfig.ThemeTemplateOBJ17.Value;
				BeatmapTheme.DefaultObjectColors[17] = EditorConfig.ThemeTemplateOBJ18.Value;

				BeatmapTheme.DefaulBackgroundColors[0] = EditorConfig.ThemeTemplateBG1.Value;
				BeatmapTheme.DefaulBackgroundColors[1] = EditorConfig.ThemeTemplateBG2.Value;
				BeatmapTheme.DefaulBackgroundColors[2] = EditorConfig.ThemeTemplateBG3.Value;
				BeatmapTheme.DefaulBackgroundColors[3] = EditorConfig.ThemeTemplateBG4.Value;
				BeatmapTheme.DefaulBackgroundColors[4] = EditorConfig.ThemeTemplateBG5.Value;
				BeatmapTheme.DefaulBackgroundColors[5] = EditorConfig.ThemeTemplateBG6.Value;
				BeatmapTheme.DefaulBackgroundColors[6] = EditorConfig.ThemeTemplateBG7.Value;
				BeatmapTheme.DefaulBackgroundColors[7] = EditorConfig.ThemeTemplateBG8.Value;
				BeatmapTheme.DefaulBackgroundColors[8] = EditorConfig.ThemeTemplateBG9.Value;

				BeatmapTheme.DefaultEffectColors[0] = EditorConfig.ThemeTemplateFX1.Value;
				BeatmapTheme.DefaultEffectColors[1] = EditorConfig.ThemeTemplateFX2.Value;
				BeatmapTheme.DefaultEffectColors[2] = EditorConfig.ThemeTemplateFX3.Value;
				BeatmapTheme.DefaultEffectColors[3] = EditorConfig.ThemeTemplateFX4.Value;
				BeatmapTheme.DefaultEffectColors[4] = EditorConfig.ThemeTemplateFX5.Value;
				BeatmapTheme.DefaultEffectColors[5] = EditorConfig.ThemeTemplateFX6.Value;
				BeatmapTheme.DefaultEffectColors[6] = EditorConfig.ThemeTemplateFX7.Value;
				BeatmapTheme.DefaultEffectColors[7] = EditorConfig.ThemeTemplateFX8.Value;
				BeatmapTheme.DefaultEffectColors[8] = EditorConfig.ThemeTemplateFX9.Value;
				BeatmapTheme.DefaultEffectColors[9] = EditorConfig.ThemeTemplateFX10.Value;
				BeatmapTheme.DefaultEffectColors[10] = EditorConfig.ThemeTemplateFX11.Value;
				BeatmapTheme.DefaultEffectColors[11] = EditorConfig.ThemeTemplateFX12.Value;
				BeatmapTheme.DefaultEffectColors[12] = EditorConfig.ThemeTemplateFX13.Value;
				BeatmapTheme.DefaultEffectColors[13] = EditorConfig.ThemeTemplateFX14.Value;
				BeatmapTheme.DefaultEffectColors[14] = EditorConfig.ThemeTemplateFX15.Value;
				BeatmapTheme.DefaultEffectColors[15] = EditorConfig.ThemeTemplateFX16.Value;
				BeatmapTheme.DefaultEffectColors[16] = EditorConfig.ThemeTemplateFX17.Value;
				BeatmapTheme.DefaultEffectColors[17] = EditorConfig.ThemeTemplateFX18.Value;
            }
            catch (Exception ex)
            {
				Debug.LogError($"{className}{nameof(UpdateDefaultThemeValues)} had an error! \n{ex}");
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

	[HarmonyPatch(typeof(HoverTooltip))]
	public class HoverTooltipPatch
    {
		[HarmonyPatch("OnPointerEnter")]
		[HarmonyPrefix]
		static bool OnPointerEnter(HoverTooltip __instance)
		{
			var index = (int)DataManager.inst.GetCurrentLanguageEnum();

			var tooltip = __instance.tooltipLangauges.Find(x => (int)x.language == index);
			var hasTooltip = tooltip != null;

			EditorManager.inst.SetTooltip(hasTooltip ? tooltip.keys : new List<string>(), hasTooltip ? tooltip.desc : "No tooltip added yet!", hasTooltip ? tooltip.hint : __instance.gameObject.name);
			return false;
		}
	}
}
