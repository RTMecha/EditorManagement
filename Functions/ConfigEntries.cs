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
		public static ConfigEntry<bool> EXPrefab { get; set; }

		public static ConfigEntry<bool> HoverSoundsEnabled { get; set; }

		//Static Name
		public static ConfigEntry<string> PT0N { get; set; }
		public static ConfigEntry<string> PT1N { get; set; }
		public static ConfigEntry<string> PT2N { get; set; }
		public static ConfigEntry<string> PT3N { get; set; }
		public static ConfigEntry<string> PT4N { get; set; }
		public static ConfigEntry<string> PT5N { get; set; }
		public static ConfigEntry<string> PT6N { get; set; }
		public static ConfigEntry<string> PT7N { get; set; }
		public static ConfigEntry<string> PT8N { get; set; }
		public static ConfigEntry<string> PT9N { get; set; }

		//Static New Name
		public static ConfigEntry<string> PT10N { get; set; }
		public static ConfigEntry<string> PT11N { get; set; }
		public static ConfigEntry<string> PT12N { get; set; }
		public static ConfigEntry<string> PT13N { get; set; }
		public static ConfigEntry<string> PT14N { get; set; }
		public static ConfigEntry<string> PT15N { get; set; }
		public static ConfigEntry<string> PT16N { get; set; }
		public static ConfigEntry<string> PT17N { get; set; }
		public static ConfigEntry<string> PT18N { get; set; }
		public static ConfigEntry<string> PT19N { get; set; }

		//Static Color
		public static ConfigEntry<Color> PT0C { get; set; }
		public static ConfigEntry<Color> PT1C { get; set; }
		public static ConfigEntry<Color> PT2C { get; set; }
		public static ConfigEntry<Color> PT3C { get; set; }
		public static ConfigEntry<Color> PT4C { get; set; }
		public static ConfigEntry<Color> PT5C { get; set; }
		public static ConfigEntry<Color> PT6C { get; set; }
		public static ConfigEntry<Color> PT7C { get; set; }
		public static ConfigEntry<Color> PT8C { get; set; }
		public static ConfigEntry<Color> PT9C { get; set; }

		//Static New Color
		public static ConfigEntry<Color> PT10C { get; set; }
		public static ConfigEntry<Color> PT11C { get; set; }
		public static ConfigEntry<Color> PT12C { get; set; }
		public static ConfigEntry<Color> PT13C { get; set; }
		public static ConfigEntry<Color> PT14C { get; set; }
		public static ConfigEntry<Color> PT15C { get; set; }
		public static ConfigEntry<Color> PT16C { get; set; }
		public static ConfigEntry<Color> PT17C { get; set; }
		public static ConfigEntry<Color> PT18C { get; set; }
		public static ConfigEntry<Color> PT19C { get; set; }

		public static ConfigEntry<float> SizeUpper { get; set; }

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

		public static ConfigEntry<KeyCode> PQCKey0 { get; set; }
		public static ConfigEntry<KeyCode> PQCKey1 { get; set; }
		public static ConfigEntry<KeyCode> PQCKey2 { get; set; }
		public static ConfigEntry<KeyCode> PQCKey3 { get; set; }
		public static ConfigEntry<KeyCode> PQCKey4 { get; set; }

		public static ConfigEntry<bool> PQCActive0 { get; set; }
		public static ConfigEntry<bool> PQCActive1 { get; set; }
		public static ConfigEntry<bool> PQCActive2 { get; set; }
		public static ConfigEntry<bool> PQCActive3 { get; set; }
		public static ConfigEntry<bool> PQCActive4 { get; set; }

		public static ConfigEntry<int> PQCIndex0 { get; set; }
		public static ConfigEntry<int> PQCIndex1 { get; set; }
		public static ConfigEntry<int> PQCIndex2 { get; set; }
		public static ConfigEntry<int> PQCIndex3 { get; set; }
		public static ConfigEntry<int> PQCIndex4 { get; set; }

		//AutoSaves
		public static ConfigEntry<float> AutoSaveRepeat { get; set; }
		public static ConfigEntry<int> AutoSaveLimit { get; set; }
		public static ConfigEntry<bool> SavingUpdatesTime { get; set; }

		//Zoom cap
		public static ConfigEntry<Vector2> ObjZoomBounds { get; set; }
		public static ConfigEntry<Vector2> ETLZoomBounds { get; set; }
		public static ConfigEntry<float> ZoomAmount { get; set; }

		//Cursor Color
		public static ConfigEntry<Color> MTSliderCol { get; set; }
		public static ConfigEntry<Color> KTSliderCol { get; set; }
		public static ConfigEntry<Color> ObjSelCol { get; set; }

		public static ConfigEntry<float> OriginXAmount { get; set; }
		public static ConfigEntry<float> OriginYAmount { get; set; }

		//Open File Configs
		public static ConfigEntry<Vector2> ORLAnchoredPos { get; set; }
		public static ConfigEntry<Vector2> ORLSizeDelta { get; set; }
		public static ConfigEntry<Vector2> OGLVLCellSize { get; set; }
		public static ConfigEntry<Vector2> ORLTogglePos { get; set; }
		public static ConfigEntry<Vector2> ORLDropdownPos { get; set; }
		public static ConfigEntry<Vector2> ORLPathPos { get; set; }
		public static ConfigEntry<float> ORLPathLength { get; set; }
		public static ConfigEntry<Vector2> ORLRefreshPos { get; set; }
		public static ConfigEntry<EditorPlugin.Constraint> OGLVLConstraint { get; set; }
		public static ConfigEntry<int> OGLVLConstraintCount { get; set; }
		public static ConfigEntry<Vector2> OGLVLSpacing { get; set; }
		public static ConfigEntry<HorizontalWrapMode> FButtonHWrap { get; set; }
		public static ConfigEntry<VerticalWrapMode> FButtonVWrap { get; set; }
		public static ConfigEntry<int> FButtonFontSize { get; set; }
		public static ConfigEntry<Color> FButtonTextColor { get; set; }
		public static ConfigEntry<bool> FButtonTextInvert { get; set; }
		public static ConfigEntry<Color> FButtonNColor { get; set; }
		public static ConfigEntry<Color> FButtonHColor { get; set; }
		public static ConfigEntry<Color> FButtonPColor { get; set; }
		public static ConfigEntry<Color> FButtonSColor { get; set; }
		public static ConfigEntry<float> FButtonFadeDColor { get; set; }

		public static ConfigEntry<bool> FButtonDifColor { get; set; }
		public static ConfigEntry<float> FButtonDifColorMult { get; set; }
		public static ConfigEntry<int> FButtonFoldClamp { get; set; }
		public static ConfigEntry<int> FButtonSongClamp { get; set; }
		public static ConfigEntry<int> FButtonArtiClamp { get; set; }
		public static ConfigEntry<int> FButtonCreaClamp { get; set; }
		public static ConfigEntry<int> FButtonDescClamp { get; set; }
		public static ConfigEntry<int> FButtonDateClamp { get; set; }
		public static ConfigEntry<string> FButtonFormat { get; set; }
		public static ConfigEntry<Vector2> FBIconPos { get; set; }
		public static ConfigEntry<Vector2> FBIconSca { get; set; }

		public static ConfigEntry<bool> IfReloadLList { get; set; }

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
		public static ConfigEntry<bool> IfEditorStartTime { get; set; }
		public static ConfigEntry<bool> IfEditorPauses { get; set; }
		public static ConfigEntry<bool> IfEditorSlowLoads { get; set; }
		public static ConfigEntry<bool> EditorDebug { get; set; }
		public static ConfigEntry<bool> DragUI { get; set; }

		//Notifications
		public static ConfigEntry<float> NotificationWidth { get; set; }
		public static ConfigEntry<float> NotificationSize { get; set; }
		public static ConfigEntry<EditorPlugin.Direction> NotificationDirection { get; set; }

		public static ConfigEntry<float> TimeModify { get; set; }

		public static ConfigEntry<bool> RenderTimeline { get; set; }
		public static ConfigEntry<Color> TimelineBGColor { get; set; }
		public static ConfigEntry<Color> TimelineTopColor { get; set; }
		public static ConfigEntry<Color> TimelineBottomColor { get; set; }

		public static ConfigEntry<bool> ReminderActive { get; set; }
		public static ConfigEntry<float> ReminderRepeat { get; set; }

		public static ConfigEntry<EditorPlugin.WaveformType> WaveformMode { get; set; }
		public static ConfigEntry<bool> GenerateWaveform { get; set; }
		public static ConfigEntry<bool> ShowObjectsOnLayer { get; set; }
		public static ConfigEntry<float> ShowObjectsAlpha { get; set; }
		public static ConfigEntry<bool> ShowEmpties { get; set; }
		public static ConfigEntry<bool> ShowDamagable { get; set; }
		public static ConfigEntry<bool> HighlightObjects { get; set; }
		public static ConfigEntry<Color> HighlightColor { get; set; }
		public static ConfigEntry<Color> HighlightDoubleColor { get; set; }
		public static ConfigEntry<bool> PreviewSelectFix { get; set; }

		public static ConfigEntry<Color> EditorGUIColor1 { get; set; }
		public static ConfigEntry<Color> EditorGUIColor2 { get; set; }
		public static ConfigEntry<Color> EditorGUIColor3 { get; set; }
		public static ConfigEntry<Color> EditorGUIColor4 { get; set; }
		public static ConfigEntry<Color> EditorGUIColor5 { get; set; }
		public static ConfigEntry<Color> EditorGUIColor6 { get; set; }
		public static ConfigEntry<Color> EditorGUIColor7 { get; set; }
		public static ConfigEntry<Color> EditorGUIColor8 { get; set; }
		public static ConfigEntry<Color> EditorGUIColor9 { get; set; }

		public static ConfigEntry<bool> EPPAnimateX { get; set; }
		public static ConfigEntry<bool> EPPAnimateY { get; set; }
		public static ConfigEntry<Vector2> EPPAnimateInOutSpeeds { get; set; }
		public static ConfigEntry<EditorPlugin.Easings> EPPAnimateEaseIn { get; set; }
		public static ConfigEntry<EditorPlugin.Easings> EPPAnimateEaseOut { get; set; }

		public static ConfigEntry<bool> OFPAnimateX { get; set; }
		public static ConfigEntry<bool> OFPAnimateY { get; set; }
		public static ConfigEntry<Vector2> OFPAnimateInOutSpeeds { get; set; }
		public static ConfigEntry<EditorPlugin.Easings> OFPAnimateEaseIn { get; set; }
		public static ConfigEntry<EditorPlugin.Easings> OFPAnimateEaseOut { get; set; }

		public static ConfigEntry<bool> NFPAnimateX { get; set; }
		public static ConfigEntry<bool> NFPAnimateY { get; set; }
		public static ConfigEntry<Vector2> NFPAnimateInOutSpeeds { get; set; }
		public static ConfigEntry<EditorPlugin.Easings> NFPAnimateEaseIn { get; set; }
		public static ConfigEntry<EditorPlugin.Easings> NFPAnimateEaseOut { get; set; }

		public static ConfigEntry<bool> PPAnimateX { get; set; }
		public static ConfigEntry<bool> PPAnimateY { get; set; }
		public static ConfigEntry<Vector2> PPAnimateInOutSpeeds { get; set; }
		public static ConfigEntry<EditorPlugin.Easings> PPAnimateEaseIn { get; set; }
		public static ConfigEntry<EditorPlugin.Easings> PPAnimateEaseOut { get; set; }

		public static ConfigEntry<bool> OBJPAnimateX { get; set; }
		public static ConfigEntry<bool> OBJPAnimateY { get; set; }
		public static ConfigEntry<Vector2> OBJPAnimateInOutSpeeds { get; set; }
		public static ConfigEntry<EditorPlugin.Easings> OBJPAnimateEaseIn { get; set; }
		public static ConfigEntry<EditorPlugin.Easings> OBJPAnimateEaseOut { get; set; }

		public static ConfigEntry<bool> BGPAnimateX { get; set; }
		public static ConfigEntry<bool> BGPAnimateY { get; set; }
		public static ConfigEntry<Vector2> BGPAnimateInOutSpeeds { get; set; }
		public static ConfigEntry<EditorPlugin.Easings> BGPAnimateEaseIn { get; set; }
		public static ConfigEntry<EditorPlugin.Easings> BGPAnimateEaseOut { get; set; }

		public static ConfigEntry<bool> QAPAnimateX { get; set; }
		public static ConfigEntry<bool> QAPAnimateY { get; set; }
		public static ConfigEntry<Vector2> QAPAnimateInOutSpeeds { get; set; }
		public static ConfigEntry<EditorPlugin.Easings> QAPAnimateEaseIn { get; set; }
		public static ConfigEntry<EditorPlugin.Easings> QAPAnimateEaseOut { get; set; }

		public static ConfigEntry<bool> GODAnimateX { get; set; }
		public static ConfigEntry<bool> GODAnimateY { get; set; }
		public static ConfigEntry<Vector2> GODAnimateInOutSpeeds { get; set; }
		public static ConfigEntry<EditorPlugin.Easings> GODAnimateEaseIn { get; set; }
		public static ConfigEntry<EditorPlugin.Easings> GODAnimateEaseOut { get; set; }


		public static ConfigEntry<Color> DepthNormalColor { get; set; }
		public static ConfigEntry<Color> DepthPressedColor { get; set; }
		public static ConfigEntry<Color> DepthHighlightedColor { get; set; }
		public static ConfigEntry<Color> DepthDisabledColor { get; set; }
		public static ConfigEntry<float> DepthFadeDuration { get; set; }
		public static ConfigEntry<bool> DepthInteractable { get; set; }
		public static ConfigEntry<bool> DepthUpdate { get; set; }
		public static ConfigEntry<int> SliderRMax { get; set; }
		public static ConfigEntry<int> SliderRMin { get; set; }
		public static ConfigEntry<Slider.Direction> SliderDDirection { get; set; }
		public static ConfigEntry<int> DepthAmount { get; set; }

		public static ConfigEntry<bool> ShowSelector { get; set; }

		public static ConfigEntry<bool> KeyframeSnap { get; set; }
		public static ConfigEntry<float> SnapAmount { get; set; }

		public static ConfigEntry<float> HoverUIOFPSize { get; set; }
		public static ConfigEntry<float> HoverUIETLSize { get; set; }
		public static ConfigEntry<float> HoverUIKFSize { get; set; }

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

		public static ConfigEntry<bool> DraggingTimelineSliderPauses { get; set; }

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

		public static ConfigEntry<bool> ListHorizontal { get; set; }
		public static ConfigEntry<Vector2> ListCellSize { get; set; }
		public static ConfigEntry<GridLayoutGroup.Constraint> ListConstraint { get; set; }
		public static ConfigEntry<int> ListConstraintCount { get; set; }
		public static ConfigEntry<Vector2> ListSpacing { get; set; }
		public static ConfigEntry<GridLayoutGroup.Axis> ListAxis { get; set; }

		public static ConfigEntry<HorizontalWrapMode> ThemeHWM { get; set; }
		public static ConfigEntry<VerticalWrapMode> ThemeVWM { get; set; }
		public static ConfigEntry<int> ThemeFSize { get; set; }
		public static ConfigEntry<Color> ThemeTColor { get; set; }
		public static ConfigEntry<Color> ThemeBColor { get; set; }
	}
}
