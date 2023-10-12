using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using HarmonyLib;
using BepInEx;
using BepInEx.Configuration;

namespace EditorManagement.Functions
{
    public static class ConfigEntries
	{
		public static ConfigEntry<bool> SaveOpacityToThemes { get; set; }

		public static ConfigEntry<KeyCode> OpenPlayerEditor { get; set; }

		public static ConfigEntry<float> TimelineBarButtonsHoverSize { get; set; }

		public static ConfigEntry<bool> ShowLevelDeleteButton { get; set; }

		public static ConfigEntry<bool> PasteOffset { get; set; }
		public static ConfigEntry<bool> DisplayNotifications { get; set; }

		public static ConfigEntry<bool> PrefabExampleTemplate { get; set; }

		public static ConfigEntry<bool> HoverSoundsEnabled { get; set; }

		public static ConfigEntry<float> PrefabButtonHoverSize { get; set; }

		//Cell Size
		public static ConfigEntry<bool> PrefabINHScroll { get; set; }
		public static ConfigEntry<Vector2> PrefabINCellSize { get; set; }
		public static ConfigEntry<Vector2> PrefabINCellSpacing { get; set; }
		public static ConfigEntry<int> PrefabINConstraintColumns { get; set; }
		public static ConfigEntry<GridLayoutGroup.Constraint> PrefabINConstraint { get; set; }
		public static ConfigEntry<GridLayoutGroup.Axis> PrefabINAxis { get; set; }
		public static ConfigEntry<bool> PrefabEXHScroll { get; set; }
		public static ConfigEntry<Vector2> PrefabEXCellSize { get; set; }
		public static ConfigEntry<Vector2> PrefabEXCellSpacing { get; set; }
		public static ConfigEntry<int> PrefabEXConstraintColumns { get; set; }
		public static ConfigEntry<GridLayoutGroup.Constraint> PrefabEXConstraint { get; set; }
		public static ConfigEntry<GridLayoutGroup.Axis> PrefabEXAxis { get; set; }
		public static ConfigEntry<Vector2> PrefabINLDeletePos { get; set; }
		public static ConfigEntry<Vector2> PrefabINLDeleteSca { get; set; }
		public static ConfigEntry<Vector2> PrefabEXLDeletePos { get; set; }
		public static ConfigEntry<Vector2> PrefabEXLDeleteSca { get; set; }

		public static ConfigEntry<HorizontalWrapMode> PrefabINNameHOverflow { get; set; }
		public static ConfigEntry<VerticalWrapMode> PrefabINNameVOverflow { get; set; }
		public static ConfigEntry<int> PrefabINNameFontSize { get; set; }
		public static ConfigEntry<HorizontalWrapMode> PrefabINTypeHOverflow { get; set; }
		public static ConfigEntry<VerticalWrapMode> PrefabINTypeVOverflow { get; set; }
		public static ConfigEntry<int> PrefabINTypeFontSize { get; set; }

		public static ConfigEntry<HorizontalWrapMode> PrefabEXNameHOverflow { get; set; }
		public static ConfigEntry<VerticalWrapMode> PrefabEXNameVOverflow { get; set; }
		public static ConfigEntry<int> PrefabEXNameFontSize { get; set; }
		public static ConfigEntry<HorizontalWrapMode> PrefabEXTypeHOverflow { get; set; }
		public static ConfigEntry<VerticalWrapMode> PrefabEXTypeVOverflow { get; set; }
		public static ConfigEntry<int> PrefabEXTypeFontSize { get; set; }

		public static ConfigEntry<Vector2> PrefabINANCH { get; set; }
		public static ConfigEntry<Vector2> PrefabINSD { get; set; }
		public static ConfigEntry<Vector2> PrefabEXANCH { get; set; }
		public static ConfigEntry<Vector2> PrefabEXSD { get; set; }
		public static ConfigEntry<Vector2> PrefabEXPathPos { get; set; }
		public static ConfigEntry<float> PrefabEXPathSca { get; set; }
		public static ConfigEntry<Vector2> PrefabEXRefreshPos { get; set; }

		//AutoSaves
		public static ConfigEntry<float> AutoSaveLoopTime { get; set; }
		public static ConfigEntry<int> AutoSaveLimit { get; set; }
		public static ConfigEntry<bool> SavingUpdatesTime { get; set; }

		//Zoom cap
		public static ConfigEntry<Vector2> KeyframeZoomBounds { get; set; }
		public static ConfigEntry<Vector2> MainZoomBounds { get; set; }
		public static ConfigEntry<float> MainZoomAmount { get; set; }
		public static ConfigEntry<float> KeyframeZoomAmount { get; set; }

		//Cursor Color
		public static ConfigEntry<Color> MainTimelineSliderColor { get; set; }
		public static ConfigEntry<Color> KeyframeTimelineSliderColor { get; set; }
		public static ConfigEntry<Color> ObjectSelectionColor { get; set; }

		//Open File Configs
		public static ConfigEntry<Vector2> OpenFilePosition { get; set; }
		public static ConfigEntry<Vector2> OpenFileScale { get; set; }
		public static ConfigEntry<Vector2> OpenFileCellSize { get; set; }
		public static ConfigEntry<Vector2> OpenFileTogglePosition { get; set; }
		public static ConfigEntry<Vector2> OpenFileDropdownPosition { get; set; }
		public static ConfigEntry<Vector2> OpenFilePathPos { get; set; }
		public static ConfigEntry<float> OpenFilePathLength { get; set; }
		public static ConfigEntry<Vector2> OpenFileRefreshPosition { get; set; }
		public static ConfigEntry<Constraint> OpenFileCellConstraintType { get; set; }
		public static ConfigEntry<int> OpenFileCellConstraintCount { get; set; }
		public static ConfigEntry<Vector2> OpenFileCellSpacing { get; set; }
		public static ConfigEntry<HorizontalWrapMode> OpenFileTextHorizontalWrap { get; set; }
		public static ConfigEntry<VerticalWrapMode> OpenFileTextVerticalWrap { get; set; }
		public static ConfigEntry<int> OpenFileTextFontSize { get; set; }
		public static ConfigEntry<Color> OpenFileTextColor { get; set; }
		public static ConfigEntry<bool> OpenFileTextInvert { get; set; }
		public static ConfigEntry<Color> OpenFileButtonNormalColor { get; set; }
		public static ConfigEntry<Color> OpenFileButtonHighlightedColor { get; set; }
		public static ConfigEntry<Color> OpenFileButtonPressedColor { get; set; }
		public static ConfigEntry<Color> OpenFileButtonSelectedColor { get; set; }
		public static ConfigEntry<float> OpenFileButtonFadeDuration { get; set; }

		public static ConfigEntry<bool> OpenFileButtonDifficultyColor { get; set; }
		public static ConfigEntry<float> OpenFileButtonDifficultyMultiply { get; set; }
		public static ConfigEntry<int> OpenFileFolderNameMax { get; set; }
		public static ConfigEntry<int> OpenFileSongNameMax { get; set; }
		public static ConfigEntry<int> OpenFileArtistNameMax { get; set; }
		public static ConfigEntry<int> OpenFileCreatorNameMax { get; set; }
		public static ConfigEntry<int> OpenFileDescriptionMax { get; set; }
		public static ConfigEntry<int> OpenFileDateMax { get; set; }
		public static ConfigEntry<string> OpenFileTextFormatting { get; set; }
		public static ConfigEntry<Vector2> OpenFileCoverPosition { get; set; }
		public static ConfigEntry<Vector2> OpenFileCoverScale { get; set; }

		public static ConfigEntry<bool> ChangesRefreshLevelList { get; set; }

		//New Markers
		public static ConfigEntry<Color> MarkerColN0 { get; set; }
		public static ConfigEntry<Color> MarkerColN1 { get; set; }
		public static ConfigEntry<Color> MarkerColN2 { get; set; }
		public static ConfigEntry<Color> MarkerColN3 { get; set; }
		public static ConfigEntry<Color> MarkerColN4 { get; set; }
		public static ConfigEntry<Color> MarkerColN5 { get; set; }
		public static ConfigEntry<Color> MarkerColN6 { get; set; }
		public static ConfigEntry<Color> MarkerColN7 { get; set; }
		public static ConfigEntry<Color> MarkerColN8 { get; set; }

		public static ConfigEntry<bool> MarkerLoop { get; set; }
		public static ConfigEntry<int> MarkerEndIndex { get; set; }
		public static ConfigEntry<int> MarkerStartIndex { get; set; }

		//General Editor
		public static ConfigEntry<bool> LevelLoadsSavedTime { get; set; }
		public static ConfigEntry<bool> LevelPausesOnStart { get; set; }
		public static ConfigEntry<bool> EditorDebug { get; set; }
		public static ConfigEntry<bool> DragUI { get; set; }

		//Notifications
		public static ConfigEntry<float> NotificationWidth { get; set; }
		public static ConfigEntry<float> NotificationSize { get; set; }
		public static ConfigEntry<Direction> NotificationDirection { get; set; }

		public static ConfigEntry<bool> RenderTimeline { get; set; }
		public static ConfigEntry<Color> WaveformBGColor { get; set; }
		public static ConfigEntry<Color> WaveformTopColor { get; set; }
		public static ConfigEntry<Color> WaveformBottomColor { get; set; }

		public static ConfigEntry<bool> ReminderActive { get; set; }
		public static ConfigEntry<float> ReminderLoopTime { get; set; }

		public static ConfigEntry<WaveformType> WaveformMode { get; set; }
		public static ConfigEntry<bool> GenerateWaveform { get; set; }
		public static ConfigEntry<bool> ShowObjectsOnLayer { get; set; }
		public static ConfigEntry<float> ShowObjectsAlpha { get; set; }
		public static ConfigEntry<bool> ShowEmpties { get; set; }
		public static ConfigEntry<bool> ShowDamagable { get; set; }
		public static ConfigEntry<bool> HighlightObjects { get; set; }
		public static ConfigEntry<Color> HighlightColor { get; set; }
		public static ConfigEntry<Color> HighlightDoubleColor { get; set; }
		public static ConfigEntry<bool> PreviewSelectFix { get; set; }

		//public static ConfigEntry<Color> EditorGUIColor1 { get; set; }
		//public static ConfigEntry<Color> EditorGUIColor2 { get; set; }
		//public static ConfigEntry<Color> EditorGUIColor3 { get; set; }
		//public static ConfigEntry<Color> EditorGUIColor4 { get; set; }
		//public static ConfigEntry<Color> EditorGUIColor5 { get; set; }
		//public static ConfigEntry<Color> EditorGUIColor6 { get; set; }
		//public static ConfigEntry<Color> EditorGUIColor7 { get; set; }
		//public static ConfigEntry<Color> EditorGUIColor8 { get; set; }
		//public static ConfigEntry<Color> EditorGUIColor9 { get; set; }

		//public static ConfigEntry<bool> EPPAnimateX { get; set; }
		//public static ConfigEntry<bool> EPPAnimateY { get; set; }
		//public static ConfigEntry<Vector2> EPPAnimateInOutSpeeds { get; set; }
		//public static ConfigEntry<Easings> EPPAnimateEaseIn { get; set; }
		//public static ConfigEntry<Easings> EPPAnimateEaseOut { get; set; }

		//public static ConfigEntry<bool> OFPAnimateX { get; set; }
		//public static ConfigEntry<bool> OFPAnimateY { get; set; }
		//public static ConfigEntry<Vector2> OFPAnimateInOutSpeeds { get; set; }
		//public static ConfigEntry<Easings> OFPAnimateEaseIn { get; set; }
		//public static ConfigEntry<Easings> OFPAnimateEaseOut { get; set; }

		//public static ConfigEntry<bool> NFPAnimateX { get; set; }
		//public static ConfigEntry<bool> NFPAnimateY { get; set; }
		//public static ConfigEntry<Vector2> NFPAnimateInOutSpeeds { get; set; }
		//public static ConfigEntry<Easings> NFPAnimateEaseIn { get; set; }
		//public static ConfigEntry<Easings> NFPAnimateEaseOut { get; set; }

		//public static ConfigEntry<bool> PPAnimateX { get; set; }
		//public static ConfigEntry<bool> PPAnimateY { get; set; }
		//public static ConfigEntry<Vector2> PPAnimateInOutSpeeds { get; set; }
		//public static ConfigEntry<Easings> PPAnimateEaseIn { get; set; }
		//public static ConfigEntry<Easings> PPAnimateEaseOut { get; set; }

		//public static ConfigEntry<bool> OBJPAnimateX { get; set; }
		//public static ConfigEntry<bool> OBJPAnimateY { get; set; }
		//public static ConfigEntry<Vector2> OBJPAnimateInOutSpeeds { get; set; }
		//public static ConfigEntry<Easings> OBJPAnimateEaseIn { get; set; }
		//public static ConfigEntry<Easings> OBJPAnimateEaseOut { get; set; }

		//public static ConfigEntry<bool> BGPAnimateX { get; set; }
		//public static ConfigEntry<bool> BGPAnimateY { get; set; }
		//public static ConfigEntry<Vector2> BGPAnimateInOutSpeeds { get; set; }
		//public static ConfigEntry<Easings> BGPAnimateEaseIn { get; set; }
		//public static ConfigEntry<Easings> BGPAnimateEaseOut { get; set; }

		//public static ConfigEntry<bool> QAPAnimateX { get; set; }
		//public static ConfigEntry<bool> QAPAnimateY { get; set; }
		//public static ConfigEntry<Vector2> QAPAnimateInOutSpeeds { get; set; }
		//public static ConfigEntry<Easings> QAPAnimateEaseIn { get; set; }
		//public static ConfigEntry<Easings> QAPAnimateEaseOut { get; set; }

		//public static ConfigEntry<bool> GODAnimateX { get; set; }
		//public static ConfigEntry<bool> GODAnimateY { get; set; }
		//public static ConfigEntry<Vector2> GODAnimateInOutSpeeds { get; set; }
		//public static ConfigEntry<Easings> GODAnimateEaseIn { get; set; }
		//public static ConfigEntry<Easings> GODAnimateEaseOut { get; set; }

		public static ConfigEntry<bool> ShowSelector { get; set; }

		public static ConfigEntry<bool> BPMSnapsKeyframes { get; set; }
		public static ConfigEntry<float> BPMSnapDivisions { get; set; }

		public static ConfigEntry<float> OpenFileButtonHoverSize { get; set; }
		public static ConfigEntry<float> TimelineObjectHoverSize { get; set; }
		public static ConfigEntry<float> KeyframeHoverSize { get; set; }

		public static ConfigEntry<KeyCode> EditorPropertiesKey { get; set; }

		public static ConfigEntry<string> TemplateThemeName { get; set; }
		public static ConfigEntry<Color> TemplateThemeGUIColor { get; set; }
		public static ConfigEntry<Color> TemplateThemeBGColor { get; set; }
		public static ConfigEntry<Color> TemplateThemePlayerColor1 { get; set; }
		public static ConfigEntry<Color> TemplateThemePlayerColor2 { get; set; }
		public static ConfigEntry<Color> TemplateThemePlayerColor3 { get; set; }
		public static ConfigEntry<Color> TemplateThemePlayerColor4 { get; set; }

		public static ConfigEntry<Color> TemplateThemeOBJColor1 { get; set; }
		public static ConfigEntry<Color> TemplateThemeOBJColor2 { get; set; }
		public static ConfigEntry<Color> TemplateThemeOBJColor3 { get; set; }
		public static ConfigEntry<Color> TemplateThemeOBJColor4 { get; set; }
		public static ConfigEntry<Color> TemplateThemeOBJColor5 { get; set; }
		public static ConfigEntry<Color> TemplateThemeOBJColor6 { get; set; }
		public static ConfigEntry<Color> TemplateThemeOBJColor7 { get; set; }
		public static ConfigEntry<Color> TemplateThemeOBJColor8 { get; set; }
		public static ConfigEntry<Color> TemplateThemeOBJColor9 { get; set; }

		public static ConfigEntry<Color> TemplateThemeBGColor1 { get; set; }
		public static ConfigEntry<Color> TemplateThemeBGColor2 { get; set; }
		public static ConfigEntry<Color> TemplateThemeBGColor3 { get; set; }
		public static ConfigEntry<Color> TemplateThemeBGColor4 { get; set; }
		public static ConfigEntry<Color> TemplateThemeBGColor5 { get; set; }
		public static ConfigEntry<Color> TemplateThemeBGColor6 { get; set; }
		public static ConfigEntry<Color> TemplateThemeBGColor7 { get; set; }
		public static ConfigEntry<Color> TemplateThemeBGColor8 { get; set; }
		public static ConfigEntry<Color> TemplateThemeBGColor9 { get; set; }

		public static ConfigEntry<bool> DraggingMainCursorPausesLevel { get; set; }

		//Events
		public static ConfigEntry<float> EventMoveModify { get; set; }
		public static ConfigEntry<float> EventZoomModify { get; set; }
		public static ConfigEntry<float> EventRotateModify { get; set; }
		public static ConfigEntry<float> EventShakeModify { get; set; }
		public static ConfigEntry<float> EventChromaModify { get; set; }
		public static ConfigEntry<float> EventBloomModify { get; set; }
		public static ConfigEntry<float> EventVignetteIntensityModify { get; set; }
		public static ConfigEntry<float> EventVignetteSmoothnessModify { get; set; }
		public static ConfigEntry<float> EventVignetteRoundnessModify { get; set; }
		public static ConfigEntry<float> EventVignettePosModify { get; set; }
		public static ConfigEntry<float> EventLensModify { get; set; }
		public static ConfigEntry<float> EventGrainIntensityModify { get; set; }
		public static ConfigEntry<float> EventGrainSizeModify { get; set; }
	}
}
