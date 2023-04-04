using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using LSFunctions;

using BepInEx;
using BepInEx.Configuration;

using HarmonyLib;

using UnityEngine;
using UnityEngine.UI;

using SimpleJSON;

using EditorManagement.Functions;
using EditorManagement.Functions.Tools;
using EditorManagement.Patchers;

namespace EditorManagement
{
	[BepInPlugin("com.mecha.editormanagement", "Editor Management", " 1.7.3")]
	[BepInProcess("Project Arrhythmia.exe")]
	[BepInIncompatibility("com.mecha.renderdepthunlimited")]
	[BepInIncompatibility("com.mecha.originoffset")]
	[BepInIncompatibility("com.mecha.cursorcolor")]
	[BepInIncompatibility("com.mecha.noautokillselectable")]
	public class EditorPlugin : BaseUnityPlugin
	{
		//TODO
		//Implement EventsPlus (wait until I got a full property system setup), NewThemeSystems, PrefabAdditions

		//Update list

		public static string className = "[<color=#F6AC1A>Editor</color><color=#2FCBD6>Management</color>]";

		public static float scrollBar;
		public static float timeEdit;
		public static float itsTheTime;
		public static int openAmount;
		public static int levelFilter = 0;
		public static bool levelAscend = true;
		public static string editorPath = "editor";
		public static WaveformType waveformType;
		public static Direction direction;
		public static Easings easing;

		public enum WaveformType
		{
			Legacy,
			Old
		}

		public enum Direction
        {
			Up,
			Down
        }

		public enum Easings
		{
			Linear,
			Instant,
			InSine,
			OutSine,
			InOutSine,
			InElastic,
			OutElastic,
			InOutElastic,
			InBack,
			OutBack,
			InOutBack,
			InBounce,
			OutBounce,
			InOutBounce,
			InQuad,
			OutQuad,
			InOutQuad,
			InCirc,
			OutCirc,
			InOutCirc,
			InExpo,
			OutExpo,
			InOutExpo
        }

		public enum Constraint
        {
			Flexible,
			FixedColumnCount,
			FixedRowCount
        }

		public static List<int> allLayers = new List<int>();

		public static RTEditor editor;

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
		public static ConfigEntry<Constraint> OGLVLConstraint { get; set; }
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
		public static ConfigEntry<Direction> NotificationDirection { get; set; }

		public static ConfigEntry<float> TimeModify { get; set; }

		public static ConfigEntry<bool> RenderTimeline { get; set; }
		public static ConfigEntry<Color> TimelineBGColor { get; set; }
		public static ConfigEntry<Color> TimelineTopColor { get; set; }
		public static ConfigEntry<Color> TimelineBottomColor { get; set; }

		public static ConfigEntry<bool> ReminderActive { get; set; }
		public static ConfigEntry<float> ReminderRepeat { get; set; }

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
		public static ConfigEntry<Easings> EPPAnimateEaseIn { get; set; }
		public static ConfigEntry<Easings> EPPAnimateEaseOut { get; set; }

		public static ConfigEntry<bool> OFPAnimateX { get; set; }
		public static ConfigEntry<bool> OFPAnimateY { get; set; }
		public static ConfigEntry<Vector2> OFPAnimateInOutSpeeds { get; set; }
		public static ConfigEntry<Easings> OFPAnimateEaseIn { get; set; }
		public static ConfigEntry<Easings> OFPAnimateEaseOut { get; set; }

		public static ConfigEntry<bool> NFPAnimateX { get; set; }
		public static ConfigEntry<bool> NFPAnimateY { get; set; }
		public static ConfigEntry<Vector2> NFPAnimateInOutSpeeds { get; set; }
		public static ConfigEntry<Easings> NFPAnimateEaseIn { get; set; }
		public static ConfigEntry<Easings> NFPAnimateEaseOut { get; set; }

		public static ConfigEntry<bool> PPAnimateX { get; set; }
		public static ConfigEntry<bool> PPAnimateY { get; set; }
		public static ConfigEntry<Vector2> PPAnimateInOutSpeeds { get; set; }
		public static ConfigEntry<Easings> PPAnimateEaseIn { get; set; }
		public static ConfigEntry<Easings> PPAnimateEaseOut { get; set; }

		public static ConfigEntry<bool> OBJPAnimateX { get; set; }
		public static ConfigEntry<bool> OBJPAnimateY { get; set; }
		public static ConfigEntry<Vector2> OBJPAnimateInOutSpeeds { get; set; }
		public static ConfigEntry<Easings> OBJPAnimateEaseIn { get; set; }
		public static ConfigEntry<Easings> OBJPAnimateEaseOut { get; set; }

		public static ConfigEntry<bool> BGPAnimateX { get; set; }
		public static ConfigEntry<bool> BGPAnimateY { get; set; }
		public static ConfigEntry<Vector2> BGPAnimateInOutSpeeds { get; set; }
		public static ConfigEntry<Easings> BGPAnimateEaseIn { get; set; }
		public static ConfigEntry<Easings> BGPAnimateEaseOut { get; set; }

		public static ConfigEntry<bool> QAPAnimateX { get; set; }
		public static ConfigEntry<bool> QAPAnimateY { get; set; }
		public static ConfigEntry<Vector2> QAPAnimateInOutSpeeds { get; set; }
		public static ConfigEntry<Easings> QAPAnimateEaseIn { get; set; }
		public static ConfigEntry<Easings> QAPAnimateEaseOut { get; set; }

		public static ConfigEntry<bool> GODAnimateX { get; set; }
		public static ConfigEntry<bool> GODAnimateY { get; set; }
		public static ConfigEntry<Vector2> GODAnimateInOutSpeeds { get; set; }
		public static ConfigEntry<Easings> GODAnimateEaseIn { get; set; }
		public static ConfigEntry<Easings> GODAnimateEaseOut { get; set; }


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

		public static List<Type> types = new List<Type>();

        private void Awake()
		{
			inst = this;

			Logger.LogInfo("Plugin Editor Management is loaded!");

			EditorPropertiesKey = Config.Bind("Editor Properties", "KeyCode", KeyCode.F10, "The key to press to open the Editor Properties / Preferences window.");

			//Animate GUI (Editor Properties Popup)
			EPPAnimateX = Config.Bind("Animate GUI", "Editor Properties Popup Animate X", true, "If the X scale should animate or not.");
			EPPAnimateY = Config.Bind("Animate GUI", "Editor Properties Popup Animate Y", true, "If the Y scale should animate or not.");
			EPPAnimateInOutSpeeds = Config.Bind("Animate GUI", "Editor Properties Popup Speeds (Open | Close)", new Vector2(0.2f, 0.2f), "How fast the animation should play. First is open speed, second is close speed.");
			EPPAnimateEaseIn = Config.Bind("Animate GUI", "Editor Properties Popup Easing Open", Easings.Linear, "What type of easing the animation should use.");
			EPPAnimateEaseOut = Config.Bind("Animate GUI", "Editor Properties Popup Easing Close", Easings.Linear, "What type of easing the animation should use.");

			//Animate GUI (Open File Popup)
			OFPAnimateX = Config.Bind("Animate GUI", "Open File Popup Animate X", true, "If the X scale should animate or not.");
			OFPAnimateY = Config.Bind("Animate GUI", "Open File Popup Animate Y", true, "If the Y scale should animate or not.");
			OFPAnimateInOutSpeeds = Config.Bind("Animate GUI", "Open File Popup Speeds (Open | Close)", new Vector2(0.2f, 0.2f), "How fast the animation should play. First is open speed, second is close speed.");
			OFPAnimateEaseIn = Config.Bind("Animate GUI", "Open File Popup Easing Open", Easings.Linear, "What type of easing the animation should use.");
			OFPAnimateEaseOut = Config.Bind("Animate GUI", "Open File Popup Easing Close", Easings.Linear, "What type of easing the animation should use.");

			NFPAnimateX = Config.Bind("Animate GUI", "New File Popup Animate X", true, "If the X scale should animate or not.");
			NFPAnimateY = Config.Bind("Animate GUI", "New File Popup Animate Y", true, "If the Y scale should animate or not.");
			NFPAnimateInOutSpeeds = Config.Bind("Animate GUI", "New File Popup Speeds (Open | Close)", new Vector2(0.2f, 0.2f), "How fast the animation should play. First is open speed, second is close speed.");
			NFPAnimateEaseIn = Config.Bind("Animate GUI", "New File Popup Easing Open", Easings.Linear, "What type of easing the animation should use.");
			NFPAnimateEaseOut = Config.Bind("Animate GUI", "New File Popup Easing Close", Easings.Linear, "What type of easing the animation should use.");

			PPAnimateX = Config.Bind("Animate GUI", "Prefab Popup Animate X", true, "If the X scale should animate or not.");
			PPAnimateY = Config.Bind("Animate GUI", "Prefab Popup Animate Y", true, "If the Y scale should animate or not.");
			PPAnimateInOutSpeeds = Config.Bind("Animate GUI", "Prefab Popup Speeds (Open | Close)", new Vector2(0.2f, 0.2f), "How fast the animation should play. First is open speed, second is close speed.");
			PPAnimateEaseIn = Config.Bind("Animate GUI", "Prefab Popup Easing Open", Easings.Linear, "What type of easing the animation should use.");
			PPAnimateEaseOut = Config.Bind("Animate GUI", "Prefab Popup Easing Close", Easings.Linear, "What type of easing the animation should use.");

			QAPAnimateX = Config.Bind("Animate GUI", "Object Tags Popup Animate X", true, "If the X scale should animate or not.");
			QAPAnimateY = Config.Bind("Animate GUI", "Object Tags Popup Animate Y", true, "If the Y scale should animate or not.");
			QAPAnimateInOutSpeeds = Config.Bind("Animate GUI", "Object Tags Popup Speeds (Open | Close)", new Vector2(0.2f, 0.2f), "How fast the animation should play. First is open speed, second is close speed.");
			QAPAnimateEaseIn = Config.Bind("Animate GUI", "Object Tags Popup Easing Open", Easings.Linear, "What type of easing the animation should use.");
			QAPAnimateEaseOut = Config.Bind("Animate GUI", "Object Tags Popup Easing Close", Easings.Linear, "What type of easing the animation should use.");

			OBJPAnimateX = Config.Bind("Animate GUI", "Create Object Popup Animate X", true, "If the X scale should animate or not.");
			OBJPAnimateY = Config.Bind("Animate GUI", "Create Object Popup Animate Y", true, "If the Y scale should animate or not.");
			OBJPAnimateInOutSpeeds = Config.Bind("Animate GUI", "Create Object Popup Speeds (Open | Close)", new Vector2(0.2f, 0.2f), "How fast the animation should play. First is open speed, second is close speed.");
			OBJPAnimateEaseIn = Config.Bind("Animate GUI", "Create Object Popup Easing Open", Easings.Linear, "What type of easing the animation should use.");
			OBJPAnimateEaseOut = Config.Bind("Animate GUI", "Create Object Popup Easing Close", Easings.Linear, "What type of easing the animation should use.");

			BGPAnimateX = Config.Bind("Animate GUI", "Create BG Popup Animate X", true, "If the X scale should animate or not.");
			BGPAnimateY = Config.Bind("Animate GUI", "Create BG Popup Animate Y", true, "If the Y scale should animate or not.");
			BGPAnimateInOutSpeeds = Config.Bind("Animate GUI", "Create BG Popup Speeds (Open | Close)", new Vector2(0.2f, 0.2f), "How fast the animation should play. First is open speed, second is close speed.");
			BGPAnimateEaseIn = Config.Bind("Animate GUI", "Create BG Popup Easing Open", Easings.Linear, "What type of easing the animation should use.");
			BGPAnimateEaseOut = Config.Bind("Animate GUI", "Create BG Popup Easing Close", Easings.Linear, "What type of easing the animation should use.");

			GODAnimateX = Config.Bind("Animate GUI", "Object Editor Animate X", true, "If the X scale should animate or not.");
			GODAnimateY = Config.Bind("Animate GUI", "Object Editor Animate Y", true, "If the Y scale should animate or not.");
			GODAnimateInOutSpeeds = Config.Bind("Animate GUI", "Object Editor Speeds (Open | Close)", new Vector2(0.2f, 0.2f), "How fast the animation should play. First is open speed, second is close speed.");
			GODAnimateEaseIn = Config.Bind("Animate GUI", "Object Editor Easing Open", Easings.Linear, "What type of easing the animation should use.");
			GODAnimateEaseOut = Config.Bind("Animate GUI", "Object Editor Easing Close", Easings.Linear, "What type of easing the animation should use.");

			HoverUIOFPSize = Config.Bind("Hover UI", "Open File Popup Size", 1.1f, "How big the button gets when hovered.");
			HoverUIETLSize = Config.Bind("Hover UI", "Timeline Object Size", 1.1f, "How big the button gets when hovered.");
			HoverUIKFSize = Config.Bind("Hover UI", "Object Keyframe Size", 1.1f, "How big the button gets when hovered.");

			//AutoSave Config
			AutoSaveRepeat = Config.Bind("AutoSave", "Repeat", 600f, "The repeat time of autosave.");
			AutoSaveLimit = Config.Bind("AutoSave", "Limit", 3, "If autosave count reaches this number, delete the first autosave.");
			SavingUpdatesTime = Config.Bind("Saving", "Update Date Edited to Recent", false, "Enabling this will save date_edited in metadata.lsb as the most recent.");

			//General Editor stuff
			IfEditorStartTime = Config.Bind("General Editor", "Load Saved Time", true, "If enabled, sets the audio time to the last saved timeline position on level load.");
			IfEditorPauses = Config.Bind("General Editor", "Editor Pauses", false, "If enabled, the editor pauses on level load.");
			IfEditorSlowLoads = Config.Bind("General Editor", "One by one load", false, "If enabled, the editor will load each object individually rather than all at once.");
			EditorDebug = Config.Bind("General Editor", "Debug", false, "If enabled, specific debugging functions for the editor will be enabled.");
			DragUI = Config.Bind("General Editor", "Drag UI", false, "if enabled, specific UI popups can be dragged around.");
			NotificationWidth = Config.Bind("Editor Notifications", "Notification Width", 221f, "Width of the notification popups.");
			NotificationSize = Config.Bind("Editor Notifications", "Notification Size", 1f, "Size of the notification popups.");
			NotificationDirection = Config.Bind("Editor Notifications", "Notification Direction", Direction.Down, "Size of the notification popups.");

			//New Markers Config
			MarkerColN0 = Config.Bind("Markers", "Color 0", Color.white, "Color 0 of the second set of marker colors.");
			MarkerColN1 = Config.Bind("Markers", "Color 1", Color.white, "Color 1 of the second set of marker colors.");
			MarkerColN2 = Config.Bind("Markers", "Color 2", Color.white, "Color 2 of the second set of marker colors.");
			MarkerColN3 = Config.Bind("Markers", "Color 3", Color.white, "Color 3 of the second set of marker colors.");
			MarkerColN4 = Config.Bind("Markers", "Color 4", Color.white, "Color 4 of the second set of marker colors.");
			MarkerColN5 = Config.Bind("Markers", "Color 5", Color.white, "Color 5 of the second set of marker colors.");
			MarkerColN6 = Config.Bind("Markers", "Color 6", Color.white, "Color 6 of the second set of marker colors.");
			MarkerColN7 = Config.Bind("Markers", "Color 7", Color.white, "Color 7 of the second set of marker colors.");
			MarkerColN8 = Config.Bind("Markers", "Color 8", Color.white, "Color 8 of the second set of marker colors.");

			MarkerLoop = Config.Bind("Markers", "Marker Loop Active", false, "If the marker should loop between markers.");
			MarkerStartIndex = Config.Bind("Markers", "Marker Loop Begin", 0, "Audio time gets set to this marker.");
			MarkerEndIndex = Config.Bind("Markers", "Marker Loop End", 1, "If the audio time gets to the set marker time, it will loop to the beginning marker.");

			//Open File Popup Configs
			ORLAnchoredPos = Config.Bind("Open File Popup Base", "00 Position", Vector2.zero, "The position of the open file popup.");
			ORLSizeDelta = Config.Bind("Open File Popup Base", "01 Scale", new Vector2(600f, 400f), "The size of the open file popup.");
			ORLPathPos = Config.Bind("Open File Popup Base", "02 Editor Path Pos", new Vector2(125f, 16f), "The position of the editor path input field.");
			ORLPathLength = Config.Bind("Open File Popup Base", "03 Editor Path Length", 254f, "The length of the editor path input field.");
			ORLRefreshPos = Config.Bind("Open File Popup Base", "04 List Refresh Pos", new Vector2(260f, 432f), "The position of the refresh button.");
			ORLTogglePos = Config.Bind("Open File Popup Base", "05 Toggle Pos", new Vector2(600f, 16f), "The position of the descending toggle.");
			ORLDropdownPos = Config.Bind("Open File Popup Base", "06 Dropdown Pos", new Vector2(501f, 416f), "The position of the sort dropdown.");

			OGLVLCellSize = Config.Bind("Open File Popup Cells", "00 Cell Size", new Vector2(584f, 32f), "Size of each cell.");
			OGLVLConstraint = Config.Bind("Open File Popup Cells", "01 Constraint Type", Constraint.FixedColumnCount, "How the cells are layed out.");
			OGLVLConstraintCount = Config.Bind("Open File Popup Cells", "02 Constraint Count", 1, "How many rows / columns there are, depending on Constraint Type.");
			OGLVLSpacing = Config.Bind("Open File Popup Cells", "03 Spacing", new Vector2(0f, 8f), "The space between each cell.");

			//Folder Button Configs
			FButtonHWrap = Config.Bind("Open File Popup Buttons", "00 Horizontal Wrap", HorizontalWrapMode.Wrap, "Horizontal Wrap Mode of the folder button text.");
			FButtonVWrap = Config.Bind("Open File Popup Buttons", "01 Vertical Wrap", VerticalWrapMode.Truncate, "Vertical Wrap Mode of the folder button text.");
			FButtonTextColor = Config.Bind("Open File Popup Buttons", "02 Text Color", new Color(0.9373f, 0.9216f, 0.9373f, 1f), "Color of the folder button text.");
			FButtonTextInvert = Config.Bind("Open File Popup Buttons", "03 Text Invert", true, "If the text should invert if the difficulty color is dark.");
			FButtonFontSize = Config.Bind("Open File Popup Buttons", "04 Text Font Size", 20, "Font size of the folder button text.");

			FButtonFoldClamp = Config.Bind("Open File Popup Buttons", "05 Folder Name Clamp", 14, "Limited length of the folder name.");
			FButtonSongClamp = Config.Bind("Open File Popup Buttons", "06 Song Name Clamp", 22, "Limited length of the song name.");
			FButtonArtiClamp = Config.Bind("Open File Popup Buttons", "07 Artist Name Clamp", 16, "Limited length of the artist name.");
			FButtonCreaClamp = Config.Bind("Open File Popup Buttons", "08 Creator Name Clamp", 16, "Limited length of the creator name.");
			FButtonDescClamp = Config.Bind("Open File Popup Buttons", "09 Description Clamp", 16, "Limited length of the description.");
			FButtonDateClamp = Config.Bind("Open File Popup Buttons", "0A Date Clamp", 16, "Limited length of the date.");
			FButtonFormat = Config.Bind("Open File Popup Buttons", "0B Formatting", ".  /{0} : {1} by {2}", "The way the text is formatted for each level. {0} is folder, {1} is song, {2} is artist, {3} is creator, {4} is difficulty, {5} is description and {6} is last edited.");

			FButtonDifColor = Config.Bind("Open File Popup Buttons", "10 Difficulty Color", false, "If each button matches its associated difficulty color.");
			FButtonDifColorMult = Config.Bind("Open File Popup Buttons", "11 Difficulty Multiply", 1.5f, "How much each buttons' color multiplies by difficulty color.");

			FButtonNColor = Config.Bind("Open File Popup Buttons", "12 Normal Color", new Color(0.1647f, 0.1647f, 0.1647f, 1f), "Normal color of the folder button.");
			FButtonHColor = Config.Bind("Open File Popup Buttons", "13 Highlighted Color", new Color(0.2588f, 0.2588f, 0.2588f, 1f), "Highlighted color of the folder button.");
			FButtonPColor = Config.Bind("Open File Popup Buttons", "14 Pressed Color", new Color(0.2588f, 0.2588f, 0.2588f, 1f), "Pressed color of the folder button.");
			FButtonSColor = Config.Bind("Open File Popup Buttons", "15 Selected Color", new Color(0.2588f, 0.2588f, 0.2588f, 1f), "Selected color of the folder button.");
			FButtonFadeDColor = Config.Bind("Open File Popup Buttons", "16 Fade Duration", 0.2f, "Fade duration of the folder button.");

			//Cover Art Configs
			FBIconPos = Config.Bind("Open File Popup Buttons", "17 Cover Position", new Vector2(-276f, 0f), "Position of the level cover.");
			FBIconSca = Config.Bind("Open File Popup Buttons", "18 Cover Size", new Vector2(26f, 26f), "Size of the level cover.");

			IfReloadLList = Config.Bind("Open File Popup Buttons", "Changes Refresh List (Read desc)", false, "If the level list reloads whenever a change is made. DO NOT SET AS ENABLED IF YOU HAVE LOADS OF LEVELS!!");

			TimeModify = Config.Bind("Timeline Bar", "Time Scroll Input", 0.1f, "The amount the time input increases when you scroll on it.");

			//Zoom Cap Config
			ObjZoomBounds = Config.Bind("Zoom Bounds", "Object timeline", new Vector2(1f, 512f), "The cap of the object timeline zoom.");
			ETLZoomBounds = Config.Bind("Zoom Bounds", "Editor timeline", new Vector2(16f, 512f), "The cap of the editor timeline zoom.");
			ZoomAmount = Config.Bind("Zoom Bounds", "Zoom Amount", 0.05f, "How much the timeline should zoom.");
			lastEdtBounds = ETLZoomBounds.Value;

			RenderTimeline = Config.Bind("Timeline", "00 Re-render Timeline", false, "If the timeline waveform should update when value is changed.");
			TimelineBGColor = Config.Bind("Timeline", "01 BG Color", Color.clear, "Color of the background of the timeline. (Only for Legacy waveform type)");
			TimelineTopColor = Config.Bind("Timeline", "02 Top Color", LSColors.red300, "Color of the top part of the timeline. (Only for Legacy waveform type)");
			TimelineBottomColor = Config.Bind("Timeline", "03 Bottom Color", LSColors.blue300, "Color of the bottom part of the timeline waveform. (Only for Legacy waveform type)");
			WaveformMode = Config.Bind("Timeline", "04 Mode", WaveformType.Legacy, "The mode of the timeline waveform.");
			GenerateWaveform = Config.Bind("Timeline", "05 Generate?", true, "If disabled, timeline will not generate when you load into a level and will decrease load time.");

			ReminderActive = Config.Bind("Reminder", "Active", true, "A little reminder will popup every now and then reminding you to have a break.");
			ReminderRepeat = Config.Bind("Reminder", "Repeat", 600f, "How often the reminder will occur.");

			ShowObjectsOnLayer = Config.Bind("Preview", "00 Show only objects on current layer?", false, "If enabled, all objects not on current layer will be set to transparent");
			ShowObjectsAlpha = Config.Bind("Preview", "01 Visible object opacity", 0.2f, "Opacity of the objects not on the current layer.");
			ShowEmpties = Config.Bind("Preview", "02 Show empties?", false, "If enabled, show all objects that are set to the empty object type.");
			ShowDamagable = Config.Bind("Preview", "03 Only Show Damagable?", false, "If enabled, only objects that can damage the player will be shown.");
			HighlightObjects = Config.Bind("Preview", "04 Highlight Objects?", true, "If enabled and if cursor hovers over an object, it will be highlighted.");
			HighlightColor = Config.Bind("Preview", "05 Object Highlight Amount", new Color(0.1f, 0.1f, 0.1f), "If an object is hovered, it adds this amount of color to the hovered object.");
			HighlightDoubleColor = Config.Bind("Preview", "06 Object Highlight Double Amount", new Color(0.5f, 0.5f, 0.5f), "If an object is hovered and shift is held, it adds this amount of color to all color channels.");
			PreviewSelectFix = Config.Bind("Preview", "07 Empties not selectable in preview?", false, "If enabled, empties will not be selectable in preview.");
			ShowSelector = Config.Bind("Preview", "Show Drag Selector?", true, "If enabled, a circular point will appear that allows you to move objects when the circlular point is dragged around.");
			emptyDisable = PreviewSelectFix.Value;
			emptyVisible = ShowEmpties.Value;

			EditorGUIColor1 = Config.Bind("Editor GUI", "Color 1", new Color(0.1294118f, 0.1294118f, 0.1294118f, 1f), "Color theme slot 1.");
			EditorGUIColor2 = Config.Bind("Editor GUI", "Color 2", new Color(0.1058824f, 0.1058824f, 0.1098039f, 1f), "Color theme slot 2.");
			EditorGUIColor3 = Config.Bind("Editor GUI", "Color 3", new Color(0.937255f, 0.9215687f, 0.937255f, 1f), "Color theme slot 3.");
			EditorGUIColor4 = Config.Bind("Editor GUI", "Color 4", new Color(0.1882353f, 0.1882353f, 0.1882353f, 1f), "Color theme slot 4.");
			EditorGUIColor5 = Config.Bind("Editor GUI", "Color 5", new Color(0.2431373f, 0.2431373f, 0.2588235f, 1f), "Color theme slot 5.");
			EditorGUIColor6 = Config.Bind("Editor GUI", "Color 6", new Color(0.9333334f, 0.9176471f, 0.9333334f, 1f), "Color theme slot 6.");
			EditorGUIColor7 = Config.Bind("Editor GUI", "Color 7", new Color(0.2156863f, 0.2156863f, 0.2196079f, 1f), "Color theme slot 7.");
			EditorGUIColor8 = Config.Bind("Editor GUI", "Color 8", new Color(0.1960784f, 0.1960784f, 0.1960784f, 1f), "Color theme slot 8.");
			EditorGUIColor9 = Config.Bind("Editor GUI", "Color 9", new Color(0.1215686f, 0.1215686f, 0.1215686f, 1f), "Color theme slot 9.");

			OriginXAmount = Config.Bind("Origin Offset", "X Amount", 0.1f, "The amount the origin X increases when the mouse scroll wheel is wheeled over the inputfield.");
			OriginYAmount = Config.Bind("Origin Offset", "Y Amount", 0.1f, "The amount the origin Y increases when the mouse scroll wheel is wheeled over the inputfield.");


			string dSl = "Render Depth";
			string co = " Color";
			string rdslh = " color of the Render Depth Slider handle.";

			DepthNormalColor = Config.Bind(dSl, "00 Normal" + co, new Color(0.2588f, 0.2588f, 0.2588f, 1f), "Normal" + rdslh);
			DepthPressedColor = Config.Bind(dSl, "01 Pressed" + co, new Color(0.2588f, 0.2588f, 0.2588f, 1f), "Pressed" + rdslh);
			DepthHighlightedColor = Config.Bind(dSl, "02 Highlighted" + co, new Color(0.2588f, 0.2588f, 0.2588f, 1f), "Highlighted" + rdslh);
			DepthDisabledColor = Config.Bind(dSl, "03 Disabled" + co, new Color(0.5882f, 0.5882f, 0.5882f, 0.502f), "Disabled" + rdslh);
			DepthFadeDuration = Config.Bind(dSl, "04 Fade Duration", 0.01f, "How quick the highlighted / pressed color sets.");
			DepthInteractable = Config.Bind(dSl, "05 Interactable", true, "If the Depth Slider is interactable or not.");
			DepthUpdate = Config.Bind(dSl, "06 Update", false, "If the Depth Slider updates. If true, setting the depth via the text box will result in a slight glitch with setting the value.");
			SliderRMax = Config.Bind(dSl, "07 Slider Max", 220, "Max value the slider can show.");
			SliderRMin = Config.Bind(dSl, "08 Slider Min", -100, "Min value the slider can show.");
			SliderDDirection = Config.Bind(dSl, "09 Direction", Slider.Direction.RightToLeft, "Direction the slider goes in. BottomToTop / TopToBottom not recommended.");
			DepthAmount = Config.Bind(dSl, "0A Depth Amount", 1, "The amount the depth increases when the mouse scroll wheel is wheeled over the inputfield.");

			string tlSl = " Timeline Slider";
			string tcot = "The color of ";
			string tcs = "timeline cursor / scrubber.";

			MTSliderCol = Config.Bind("Cursor Color", "00 Timeline", new Color(0.251f, 0.4627f, 0.8745f, 1f), tcot + "the main " + tcs);
			KTSliderCol = Config.Bind("Cursor Color", "01 Object", new Color(0.251f, 0.4627f, 0.8745f, 1f), tcot + "the keyframe " + tcs);
            ObjSelCol = Config.Bind("Cursor Color", "02 Object Selection", new Color(0.251f, 0.4627f, 0.8745f, 1f), tcot + "a selected object.");

			KeyframeSnap = Config.Bind("BPM Snap", "Snap Affects Keyframes", false, "If the BPM snap should snap the object keyframes when dragged.");
			SnapAmount = Config.Bind("BPM Snap", "Snap Divisions", 4f, "H");

			Config.SettingChanged += new EventHandler<SettingChangedEventArgs>(UpdateEditorManagementConfigs);

			//Patch classes
			{
				Harmony harmony = new Harmony("anything here");

				MethodInfo layerSetter = typeof(DataManager.GameData.BeatmapObject.EditorData).GetProperty("Layer").GetSetMethod(false);
				MethodInfo binSetter = typeof(DataManager.GameData.BeatmapObject.EditorData).GetProperty("Bin").GetSetMethod(false);
				MethodInfo depthSetter = typeof(DataManager.GameData.BeatmapObject).GetProperty("Depth").GetSetMethod(false);

				MethodInfo layerPrefix = typeof(EditorPlugin).GetMethod("LayerSetterPrefix", BindingFlags.Public | BindingFlags.Static);
				MethodInfo binPrefix = typeof(EditorPlugin).GetMethod("BinSetterPrefix", BindingFlags.Public | BindingFlags.Static);
				MethodInfo depthPrefix = typeof(EditorPlugin).GetMethod("DepthSetterPrefix", BindingFlags.Public | BindingFlags.Static);

				HarmonyMethod layerPatch = new HarmonyMethod(layerPrefix);
				HarmonyMethod binPatch = new HarmonyMethod(binPrefix);
				HarmonyMethod depthPatch = new HarmonyMethod(depthPrefix);

				var loadLevelEnumeratorMethod = AccessTools.Method(typeof(EditorManager), "LoadLevel");
				var loadLevelMoveNext = AccessTools.EnumeratorMoveNext(loadLevelEnumeratorMethod);

				MethodInfo prefix3 = typeof(EditorPlugin).GetMethod("LoadLevelEnumerator", BindingFlags.Public | BindingFlags.Static);
				HarmonyMethod loadLevelPrefix = new HarmonyMethod(prefix3);

				harmony.PatchAll(typeof(EditorPlugin));
				harmony.PatchAll(typeof(MetadataPatch));
				harmony.PatchAll(typeof(ObjEditorPatch));
				harmony.PatchAll(typeof(EditorPatch));
				harmony.PatchAll(typeof(SettingEditorPatch));
				harmony.PatchAll(typeof(MarkerEditorPatch));
				harmony.PatchAll(typeof(PrefabEditorPatch));
				harmony.PatchAll(typeof(DataManagerGameDataPatch));
				harmony.PatchAll(typeof(GameManagerPatch));

				harmony.Patch(layerSetter, prefix: layerPatch);
				harmony.Patch(binSetter, prefix: binPatch);
				harmony.Patch(depthSetter, prefix: depthPatch);
				harmony.Patch(loadLevelMoveNext, postfix: loadLevelPrefix);
			}
		}

		public static EditorPlugin inst;

		//Code written by Enchart
		public static bool LayerSetterPrefix(int value, DataManager.GameData.BeatmapObject.EditorData __instance)
		{
			var field = __instance.GetType().GetField("layer", BindingFlags.NonPublic | BindingFlags.Instance);
			field.SetValue(__instance, value);
			return false;
		}
		public static bool BinSetterPrefix(int value, DataManager.GameData.BeatmapObject.EditorData __instance)
		{
			var field = __instance.GetType().GetField("bin", BindingFlags.NonPublic | BindingFlags.Instance);
			field.SetValue(__instance, value);
			return false;
		}
		public static bool DepthSetterPrefix(int value, DataManager.GameData.BeatmapObject __instance)
		{
			var field = __instance.GetType().GetField("depth", BindingFlags.NonPublic | BindingFlags.Instance);
			field.SetValue(__instance, value);
			return false;
		}

		private static void UpdateEditorManagementConfigs(object sender, EventArgs e)
		{
			if (EditorManager.inst != null)
			{
				EditorGUI.UpdateEditorGUI();

				RepeatReminder();

				if (RenderTimeline.Value == true && GenerateWaveform.Value == true)
				{
					AssignTimelineTexture();
					if (WaveformMode.Value == WaveformType.Legacy)
					{
						LegacyWaveform();
					}
					if (WaveformMode.Value == WaveformType.Old)
					{
						OldWaveform();
					}
				}

				SetNewMarkerColors();
				SetAutosave();

				if (IfReloadLList.Value == true)
				{
					EditorManager.inst.GetLevelList();
					RenderBeatmapSet();
				}

				tracker.GetComponent<DraggableObject>().enabled = ShowSelector.Value;
				tracker.GetComponent<PolygonCollider2D>().enabled = ShowSelector.Value;
				tracker.GetComponent<MeshRenderer>().enabled = ShowSelector.Value;

				//Animate GUI
				{
					var editorPropertiesPopupAIGUI = EditorManager.inst.GetDialog("Editor Properties Popup").Dialog.gameObject.GetComponent<AnimateInGUI>();

					editorPropertiesPopupAIGUI.SetEasing((int)EPPAnimateEaseIn.Value, (int)EPPAnimateEaseOut.Value);
					editorPropertiesPopupAIGUI.animateX = EPPAnimateX.Value;
					editorPropertiesPopupAIGUI.animateY = EPPAnimateY.Value;
					editorPropertiesPopupAIGUI.animateInTime = EPPAnimateInOutSpeeds.Value.x;
					editorPropertiesPopupAIGUI.animateOutTime = EPPAnimateInOutSpeeds.Value.y;

					var openFilePopupAIGUI = EditorManager.inst.GetDialog("Open File Popup").Dialog.gameObject.GetComponent<AnimateInGUI>();

					openFilePopupAIGUI.SetEasing((int)OFPAnimateEaseIn.Value, (int)OFPAnimateEaseOut.Value);
					openFilePopupAIGUI.animateX = OFPAnimateX.Value;
					openFilePopupAIGUI.animateY = OFPAnimateY.Value;
					openFilePopupAIGUI.animateInTime = OFPAnimateInOutSpeeds.Value.x;
					openFilePopupAIGUI.animateOutTime = OFPAnimateInOutSpeeds.Value.y;

					var newFilePopupAIGUI = EditorManager.inst.GetDialog("New File Popup").Dialog.gameObject.GetComponent<AnimateInGUI>();

					newFilePopupAIGUI.SetEasing((int)NFPAnimateEaseIn.Value, (int)NFPAnimateEaseOut.Value);
					newFilePopupAIGUI.animateX = NFPAnimateX.Value;
					newFilePopupAIGUI.animateY = NFPAnimateY.Value;
					newFilePopupAIGUI.animateInTime = NFPAnimateInOutSpeeds.Value.x;
					newFilePopupAIGUI.animateOutTime = NFPAnimateInOutSpeeds.Value.y;

					var prefabPopupAIGUI = EditorManager.inst.GetDialog("Prefab Popup").Dialog.gameObject.GetComponent<AnimateInGUI>();

					prefabPopupAIGUI.SetEasing((int)PPAnimateEaseIn.Value, (int)PPAnimateEaseOut.Value);
					prefabPopupAIGUI.animateX = PPAnimateX.Value;
					prefabPopupAIGUI.animateY = PPAnimateY.Value;
					prefabPopupAIGUI.animateInTime = PPAnimateInOutSpeeds.Value.x;
					prefabPopupAIGUI.animateOutTime = PPAnimateInOutSpeeds.Value.y;

					var quickActionsPopupAIGUI = EditorManager.inst.GetDialog("Quick Actions Popup").Dialog.gameObject.GetComponent<AnimateInGUI>();

					quickActionsPopupAIGUI.SetEasing((int)QAPAnimateEaseIn.Value, (int)QAPAnimateEaseOut.Value);
					quickActionsPopupAIGUI.animateX = QAPAnimateX.Value;
					quickActionsPopupAIGUI.animateY = QAPAnimateY.Value;
					quickActionsPopupAIGUI.animateInTime = QAPAnimateInOutSpeeds.Value.x;
					quickActionsPopupAIGUI.animateOutTime = QAPAnimateInOutSpeeds.Value.y;

					var objectOptionsPopupAIGUI = EditorManager.inst.GetDialog("Object Options Popup").Dialog.gameObject.GetComponent<AnimateInGUI>();

					objectOptionsPopupAIGUI.SetEasing((int)OBJPAnimateEaseIn.Value, (int)OBJPAnimateEaseOut.Value);
					objectOptionsPopupAIGUI.animateX = OBJPAnimateX.Value;
					objectOptionsPopupAIGUI.animateY = OBJPAnimateY.Value;
					objectOptionsPopupAIGUI.animateInTime = OBJPAnimateInOutSpeeds.Value.x;
					objectOptionsPopupAIGUI.animateOutTime = OBJPAnimateInOutSpeeds.Value.y;

					var bgOptionsPopupAIGUI = EditorManager.inst.GetDialog("BG Options Popup").Dialog.gameObject.GetComponent<AnimateInGUI>();

					bgOptionsPopupAIGUI.SetEasing((int)BGPAnimateEaseIn.Value, (int)BGPAnimateEaseOut.Value);
					bgOptionsPopupAIGUI.animateX = BGPAnimateX.Value;
					bgOptionsPopupAIGUI.animateY = BGPAnimateY.Value;
					bgOptionsPopupAIGUI.animateInTime = BGPAnimateInOutSpeeds.Value.x;
					bgOptionsPopupAIGUI.animateOutTime = BGPAnimateInOutSpeeds.Value.y;

					var gameObjectDialogAIGUI = EditorManager.inst.GetDialog("Object Editor").Dialog.gameObject.GetComponent<AnimateInGUI>();

					gameObjectDialogAIGUI.SetEasing((int)GODAnimateEaseIn.Value, (int)GODAnimateEaseOut.Value);
					gameObjectDialogAIGUI.animateX = GODAnimateX.Value;
					gameObjectDialogAIGUI.animateY = GODAnimateY.Value;
					gameObjectDialogAIGUI.animateInTime = GODAnimateInOutSpeeds.Value.x;
					gameObjectDialogAIGUI.animateOutTime = GODAnimateInOutSpeeds.Value.y;

					if (EditorManager.inst.GetDialog("Player Editor").Dialog && EditorManager.inst.GetDialog("Object Editor").Dialog.gameObject.GetComponent<AnimateInGUI>())
					{
						var playerEditorDialogAIGUI = EditorManager.inst.GetDialog("Object Editor").Dialog.gameObject.GetComponent<AnimateInGUI>();

						playerEditorDialogAIGUI.SetEasing((int)GODAnimateEaseIn.Value, (int)GODAnimateEaseOut.Value);
						playerEditorDialogAIGUI.animateX = GODAnimateX.Value;
						playerEditorDialogAIGUI.animateY = GODAnimateY.Value;
						playerEditorDialogAIGUI.animateInTime = GODAnimateInOutSpeeds.Value.x;
						playerEditorDialogAIGUI.animateOutTime = GODAnimateInOutSpeeds.Value.y;
					}
				}

				//Create Local Variables
				GameObject openLevel = EditorManager.inst.GetDialog("Open File Popup").Dialog.gameObject;
				Transform openTLevel = openLevel.transform;
				RectTransform openRTLevel = openLevel.GetComponent<RectTransform>();
				GameObject folderButton = EditorManager.inst.folderButtonPrefab;
				Button fButtonBUTT = folderButton.GetComponent<Button>();
				GridLayoutGroup openGridLVL = openTLevel.Find("mask/content").GetComponent<GridLayoutGroup>();
				Text fButtonText = folderButton.transform.Find("folder-name").GetComponent<Text>();
				var notifyRT = GameObject.Find("Editor Systems/Editor GUI/sizer/main/Notifications").GetComponent<RectTransform>();
				var notifyGroup = GameObject.Find("Editor Systems/Editor GUI/sizer/main/Notifications").GetComponent<VerticalLayoutGroup>();
				notifyRT.sizeDelta = new Vector2(NotificationWidth.Value, 632f);
				GameObject.Find("Editor Systems/Editor GUI/sizer/main/Notifications").transform.localScale = new Vector3(NotificationSize.Value, NotificationSize.Value, 1f);

				if (NotificationDirection.Value == Direction.Down)
                {
					notifyRT.anchoredPosition = new Vector2(8f, 408f);
					notifyGroup.childAlignment = TextAnchor.LowerLeft;


				}
				if (NotificationDirection.Value == Direction.Up)
                {
					notifyRT.anchoredPosition = new Vector2(8f, 410f);
					notifyGroup.childAlignment = TextAnchor.UpperLeft;
				}

				//Set Editor Zoom cap
				EditorManager.inst.zoomBounds = ETLZoomBounds.Value;

				//Set Open File Popup RectTransform
				openRTLevel.anchoredPosition = ORLAnchoredPos.Value;
				openRTLevel.sizeDelta = ORLSizeDelta.Value;

				//Set Open FIle Popup content GridLayoutGroup
				openGridLVL.cellSize = OGLVLCellSize.Value;
				openGridLVL.constraint = (GridLayoutGroup.Constraint)OGLVLConstraint.Value;
				openGridLVL.constraintCount = OGLVLConstraintCount.Value;
				openGridLVL.spacing = OGLVLSpacing.Value;

				//Folder Button
				fButtonText.horizontalOverflow = FButtonHWrap.Value;
				fButtonText.verticalOverflow = FButtonVWrap.Value;
				fButtonText.color = FButtonTextColor.Value;
				fButtonText.fontSize = FButtonFontSize.Value;

				//Folder Button Colors
				ColorBlock cb = fButtonBUTT.colors;
				cb.normalColor = FButtonNColor.Value;
				cb.pressedColor = FButtonPColor.Value;
				cb.highlightedColor = FButtonHColor.Value;
				cb.selectedColor = FButtonSColor.Value;
				cb.fadeDuration = FButtonFadeDColor.Value;
				fButtonBUTT.colors = cb;

				ObjEditor.inst.zoomBounds = ObjZoomBounds.Value;
				if (ETLZoomBounds.Value != lastEdtBounds)
				{
					lastEdtBounds = ETLZoomBounds.Value;
					EditorManager.inst.zoomBounds = ETLZoomBounds.Value;
					EditorManager.inst.RenderTimeline();
				}

				RectTransform dropdownRT = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("tod-dropdown(Clone)").gameObject.GetComponent<RectTransform>();
				dropdownRT.anchoredPosition = ORLDropdownPos.Value;

				RectTransform toggleRT = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("toggle(Clone)").gameObject.GetComponent<RectTransform>();
				toggleRT.anchoredPosition = ORLTogglePos.Value;

				EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("editor path").gameObject.GetComponent<RectTransform>().anchoredPosition = ORLPathPos.Value;
				EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("editor path").gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(ORLPathLength.Value, 34f);
				EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("reload").gameObject.GetComponent<RectTransform>().anchoredPosition = ORLRefreshPos.Value;

				if (PreviewSelectFix.Value != emptyDisable)
				{
					emptyDisable = PreviewSelectFix.Value;
					ObjectManager.inst.updateObjects();
				}
				if (ShowEmpties.Value != emptyVisible)
				{
					emptyVisible = ShowEmpties.Value;
					ObjectManager.inst.updateObjects();
				}
				if (ShowDamagable.Value != showDamagable)
				{
					showDamagable = ShowDamagable.Value;
					ObjectManager.inst.updateObjects();
				}

                //Cursor Color
                {
					if (GameObject.Find("Editor Systems/Editor GUI/sizer/main/whole-timeline/Slider_Parent/Slider/Handle Slide Area/Image/Handle"))
					{
						GameObject.Find("Editor Systems/Editor GUI/sizer/main/whole-timeline/Slider_Parent/Slider/Handle Slide Area/Image/Handle").GetComponent<Image>().color = MTSliderCol.Value;
					}
					else
                    {
						RTEditor.DisplayCustomNotification("CD", "Whoooops you gotta put this CD up your-", 1f, LSColors.HexToColor("202020"), MTSliderCol.Value, Color.white, "Whoops!");
                    }

					if (GameObject.Find("Editor Systems/Editor GUI/sizer/main/whole-timeline/Slider_Parent/Slider/Handle Slide Area/Image"))
					{
						GameObject.Find("Editor Systems/Editor GUI/sizer/main/whole-timeline/Slider_Parent/Slider/Handle Slide Area/Image").GetComponent<Image>().color = MTSliderCol.Value;
					}
					else
					{
						RTEditor.DisplayCustomNotification("CD", "Whoooops you gotta put this CD up your-", 1f, LSColors.HexToColor("202020"), MTSliderCol.Value, Color.white, "Whoops!");
					}

					if (GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/timeline/Scroll View/Viewport/Content/time_slider/Handle Slide Area/Handle/Image"))
					{
						GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/timeline/Scroll View/Viewport/Content/time_slider/Handle Slide Area/Handle/Image").GetComponent<Image>().color = KTSliderCol.Value;
					}
					else
					{
						RTEditor.DisplayCustomNotification("CD", "Whoooops you gotta put this CD up your-", 1f, LSColors.HexToColor("202020"), KTSliderCol.Value, Color.white, "Whoops!");
					}

					if (GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/timeline/Scroll View/Viewport/Content/time_slider/Handle Slide Area/Handle"))
					{
						GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/timeline/Scroll View/Viewport/Content/time_slider/Handle Slide Area/Handle").GetComponent<Image>().color = KTSliderCol.Value;
					}
					else
					{
						RTEditor.DisplayCustomNotification("CD", "Whoooops you gotta put this CD up your-", 1f, LSColors.HexToColor("202020"), KTSliderCol.Value, Color.white, "Whoops!");
					}

					ObjEditor.inst.SelectedColor = ObjSelCol.Value;
				}

                //Render Depth
                {
					Transform sliderObject = ObjEditor.inst.ObjectView.transform.Find("depth/depth");
					Slider sliderComponent = sliderObject.GetComponent<Slider>();
					ColorBlock cbd = sliderComponent.colors;
					cbd.normalColor = DepthNormalColor.Value;
					cbd.pressedColor = DepthPressedColor.Value;
					cbd.highlightedColor = DepthHighlightedColor.Value;
					cbd.disabledColor = DepthDisabledColor.Value;
					cbd.fadeDuration = DepthFadeDuration.Value;
					sliderComponent.colors = cbd;
					sliderComponent.interactable = DepthInteractable.Value;
					sliderComponent.maxValue = SliderRMax.Value;
					sliderComponent.minValue = SliderRMin.Value;
					sliderComponent.direction = SliderDDirection.Value;
				}
			}
		}

		public static Vector2 lastEdtBounds;

		public static bool emptyDisable;
		public static bool emptyVisible;
		public static bool showDamagable;
		public static GameObject tracker;

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

			foreach (var l in allLayers)
			{
				int num = l + 1;
				if (num > 5)
				{
					num -= 1;
				}
				if (!lister.Contains(num.ToString()))
				{
					lister += num.ToString() + ", ";
				}
			}

			RTEditor.DisplayNotification("List Object Layers", "Objects on Layers:<br>[ " + lister + " ]", 2f, EditorManager.NotificationType.Info);
		}

		[HarmonyPatch(typeof(ObjectManager), "Awake")]
		[HarmonyPostfix]
		private static void ObjectAwakePatch()
        {
			GameObject gameObject = new GameObject("UI stuff");

			var objectTracker = Instantiate(ObjectManager.inst.objectPrefabs[1].options[0].transform.GetChild(0).gameObject);
			objectTracker.transform.SetParent(gameObject.transform);
			if (objectTracker.GetComponent<SelectObjectInEditor>())
			{
				Destroy(objectTracker.GetComponent<SelectObjectInEditor>());
			}
			if (objectTracker.GetComponent<RTObject>())
			{
				Destroy(objectTracker.GetComponent<RTObject>());
			}
			objectTracker.GetComponent<PolygonCollider2D>().enabled = ShowSelector.Value;
			objectTracker.GetComponent<MeshRenderer>().enabled = ShowSelector.Value;
			objectTracker.GetComponent<MeshRenderer>().material.color = new Color(1f, 1f, 1f, 1f);
			objectTracker.AddComponent<DraggableObject>();
			objectTracker.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);

			objectTracker.GetComponent<DraggableObject>().enabled = ShowSelector.Value;

			tracker = objectTracker;

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

		[HarmonyPatch(typeof(ObjectManager), "Update")]
		[HarmonyPostfix]
		private static void ObjectUpdatePatch()
		{
			if (EditorManager.inst != null)
			{
				foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
				{
					if (beatmapObject != null && ObjectManager.inst.beatmapGameObjects.ContainsKey(beatmapObject.id) && ObjectManager.inst.beatmapGameObjects[beatmapObject.id] != null)
					{
						ObjectManager.GameObjectRef gameObjectRef = ObjectManager.inst.beatmapGameObjects[beatmapObject.id];
						Transform transform = null;
						if (gameObjectRef.rend != null)
                        {
							transform = gameObjectRef.rend.transform.GetParent();
						}

						if (beatmapObject.objectType == DataManager.GameData.BeatmapObject.ObjectType.Empty && PreviewSelectFix.Value == true)
						{
							if (gameObjectRef.obj.GetComponentInChildren<SelectObjectInEditor>())
							{
								Destroy(gameObjectRef.obj.GetComponentInChildren<SelectObjectInEditor>());
							}
							if (gameObjectRef.obj.GetComponentInChildren<RTObject>())
							{
								Destroy(gameObjectRef.obj.GetComponentInChildren<RTObject>());
							}
						}

						if (ShowEmpties.Value == true)
						{
							if (gameObjectRef.obj.GetComponentInChildren<Collider2D>() && !gameObjectRef.obj.GetComponentInChildren<MeshFilter>() && !gameObjectRef.obj.GetComponentInChildren<MeshRenderer>())
							{
								MeshFilter mesh = gameObjectRef.obj.GetComponentInChildren<Collider2D>().gameObject.AddComponent<MeshFilter>();
								gameObjectRef.obj.GetComponentInChildren<Collider2D>().gameObject.AddComponent<MeshRenderer>();

								mesh.mesh = ObjectManager.inst.objectPrefabs[0].options[0].GetComponentInChildren<MeshFilter>().mesh;

								gameObjectRef.obj.GetComponentInChildren<Collider2D>().transform.localPosition = new Vector3(gameObjectRef.obj.GetComponentInChildren<Collider2D>().transform.localPosition.x, gameObjectRef.obj.GetComponentInChildren<Collider2D>().transform.localPosition.y, -9.6f);
							}
						}

						if (ShowDamagable.Value == true)
						{
							if (beatmapObject.objectType != DataManager.GameData.BeatmapObject.ObjectType.Normal && beatmapObject.objectType != DataManager.GameData.BeatmapObject.ObjectType.Empty)
							{
								ObjectManager.GameObjectRef gameObjectRef1 = ObjectManager.inst.beatmapGameObjects[beatmapObject.id];
								gameObjectRef1.obj.GetComponentInChildren<Renderer>().enabled = false;
							}
						}

						if (EditorDebug.Value == true)
						{
							if (gameObjectRef.rend != null && gameObjectRef.rend.GetComponent<RTObject>())
							{
								var rtobj = gameObjectRef.rend.GetComponent<RTObject>();
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
									text = "<br>S: " + RTEditor.GetShape(beatmapObject.shape, beatmapObject.shapeOption) +
										"<br>T: " + beatmapObject.text;
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
									"<br>POS: {X: " + transform.position.x + ", Y: " + transform.position.y + "}" +
									"<br>SCA: {X: " + transform.localScale.x + ", Y: " + transform.localScale.y + "}" +
									"<br>ROT: " + transform.eulerAngles.z +
									"<br>COL: " + RTEditor.ColorToHex(gameObjectRef.mat.color) +
									ptr)
								{
									rtobj.tooltipLanguages[0].hint = "ID: {" + beatmapObject.id + "}" +
										parent +
										"<br>O: {X: " + beatmapObject.origin.x + ", Y: " + beatmapObject.origin.y + "}" +
										text +
										"<br>D: " + beatmapObject.Depth +
										"<br>ED: {L: " + beatmapObject.editorData.Layer + ", B: " + beatmapObject.editorData.Bin + "}" +
										"<br>POS: {X: " + transform.position.x + ", Y: " + transform.position.y + "}" +
										"<br>SCA: {X: " + transform.localScale.x + ", Y: " + transform.localScale.y + "}" +
										"<br>ROT: " + transform.eulerAngles.z +
										"<br>COL: " + RTEditor.ColorToHex(gameObjectRef.mat.color) +
										ptr;
								}
							}
						}
						else
						{
							if (gameObjectRef.rend != null && gameObjectRef.rend.GetComponent<RTObject>())
							{
								gameObjectRef.rend.GetComponent<RTObject>().tipEnabled = false;
							}
						}
					}
				}
			}
		}

		[HarmonyPatch(typeof(GameManager), "Update")]
		[HarmonyPostfix]
		private static void GameUpdatePatch()
        {
			if (EditorManager.inst == null)
            {
				Destroy(tracker);
            }

			foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
			{
				if (beatmapObject != null && ObjectManager.inst.beatmapGameObjects.ContainsKey(beatmapObject.id))
				{
					ObjectManager.GameObjectRef gameObjectRef = ObjectManager.inst.beatmapGameObjects[beatmapObject.id];

					if (EditorManager.inst != null && EditorManager.inst.isEditing == true && gameObjectRef.mat && gameObjectRef.obj.GetComponentInChildren<RTObject>() && gameObjectRef.obj.GetComponentInChildren<RTObject>().selected == true && HighlightObjects.Value == true)
					{
						if (Input.GetKey(KeyCode.LeftShift))
						{
							Color colorHover = new Color(HighlightDoubleColor.Value.r, HighlightDoubleColor.Value.g, HighlightDoubleColor.Value.b);

							if (gameObjectRef.mat.color.r > 0.9f && gameObjectRef.mat.color.g > 0.9f && gameObjectRef.mat.color.b > 0.9f)
							{
								colorHover = new Color(-HighlightDoubleColor.Value.r, -HighlightDoubleColor.Value.g, -HighlightDoubleColor.Value.b);
							}

							gameObjectRef.mat.color += new Color(colorHover.r, colorHover.g, colorHover.b, 0f);
						}
						else
						{
							Color colorHover = new Color(HighlightColor.Value.r, HighlightColor.Value.g, HighlightColor.Value.b);

							if (gameObjectRef.mat.color.r > 0.95f && gameObjectRef.mat.color.g > 0.95f && gameObjectRef.mat.color.b > 0.95f)
							{
								colorHover = new Color(-HighlightColor.Value.r, -HighlightColor.Value.g, -HighlightColor.Value.b);
							}

							gameObjectRef.mat.color += new Color(colorHover.r, colorHover.g, colorHover.b, 0f);
						}
					}
				}
            }
        }

		public static void CreateMultiObjectEditor()
        {
			//Multi Object Editor
			if (!GameObject.Find("RT Multi Object Editor"))
			{
				GameObject blocker = new GameObject("RT Multi Object Editor");

				GameObject barButton = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/time").transform.GetChild(3).gameObject;
				GameObject eventButton = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject/event");

				Color bcol = new Color(0.3922f, 0.7098f, 0.9647f, 1f);

				EditorManager.inst.GetDialog("Multi Object Editor").Dialog.Find("data/left").gameObject.SetActive(true);

				GameObject scrollView = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View"));
				scrollView.transform.SetParent(EditorManager.inst.GetDialog("Multi Object Editor").Dialog.Find("data/left"));
				LSHelpers.DeleteChildren(scrollView.transform.Find("Viewport/Content"), false);
				scrollView.GetComponent<RectTransform>().sizeDelta = new Vector2(383f, 690f);

				EditorManager.inst.GetDialog("Multi Object Editor").Dialog.Find("data/left").GetChild(1).gameObject.SetActive(true);
				EditorManager.inst.GetDialog("Multi Object Editor").Dialog.Find("data/left").GetChild(1).gameObject.name = "label layer";
				EditorManager.inst.GetDialog("Multi Object Editor").Dialog.Find("data/left").GetChild(1).GetChild(0).gameObject.GetComponent<Text>().text = "Set Group Layer";

				EditorManager.inst.GetDialog("Multi Object Editor").Dialog.Find("data/left").GetChild(3).gameObject.SetActive(true);
				EditorManager.inst.GetDialog("Multi Object Editor").Dialog.Find("data/left").GetChild(3).gameObject.name = "label depth";
				EditorManager.inst.GetDialog("Multi Object Editor").Dialog.Find("data/left").GetChild(3).GetChild(0).gameObject.GetComponent<Text>().text = "Set Group Depth";

				EditorManager.inst.GetDialog("Multi Object Editor").Dialog.Find("data/left").GetChild(1).SetParent(scrollView.transform.Find("Viewport/Content"));
				EditorManager.inst.GetDialog("Multi Object Editor").Dialog.Find("data/left").GetChild(2).SetParent(scrollView.transform.Find("Viewport/Content"));

				EditorManager.inst.GetDialog("Multi Object Editor").Dialog.Find("data/right/text holder/Text").GetComponent<Text>().text = EditorManager.inst.GetDialog("Multi Object Editor").Dialog.Find("data/right/text holder/Text").GetComponent<Text>().text.Replace("The current version of the editor doesn't support any editing functionality.", "On the left you'll see all the multi object editor tools you'll need. If you have any suggestions, feel free to let me (Mecha, the mod maker) know!");
				EditorManager.inst.GetDialog("Multi Object Editor").Dialog.Find("data/right/text holder/Text").GetComponent<Text>().fontSize = 22;
				EditorManager.inst.GetDialog("Multi Object Editor").Dialog.Find("data/right/text holder/Text").GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -125f);
				EditorManager.inst.GetDialog("Multi Object Editor").Dialog.Find("data/right/text holder/Text").GetComponent<RectTransform>().sizeDelta = new Vector2(-68f, 0f);

				GameObject multiLayerSet = Instantiate(EventEditor.inst.dialogRight.transform.Find("zoom/zoom").gameObject);
				multiLayerSet.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				multiLayerSet.name = "layer";
				multiLayerSet.transform.SetSiblingIndex(1);
				multiLayerSet.transform.GetChild(0).gameObject.GetComponent<InputField>().text = "1";
				multiLayerSet.transform.GetChild(0).Find("input/Placeholder").GetComponent<Text>().text = "Enter layer...";

				multiLayerSet.GetComponent<RectTransform>().sizeDelta = new Vector2(428f, 32f);

				GameObject multiLB = Instantiate(multiLayerSet.transform.GetChild(0).Find("<").gameObject);
				multiLB.transform.SetParent(multiLayerSet.transform.GetChild(0));
				multiLB.transform.SetSiblingIndex(2);
				multiLB.name = "|";
				multiLB.GetComponent<Image>().sprite = barButton.GetComponent<Image>().sprite;

				multiLB.GetComponent<Button>().onClick.RemoveAllListeners();
				multiLB.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						if (int.Parse(multiLayerSet.transform.GetChild(0).gameObject.GetComponent<InputField>().text) > 0)
						{
							objectSelection.GetObjectData().editorData.Layer = int.Parse(multiLayerSet.transform.GetChild(0).gameObject.GetComponent<InputField>().text) - 1;
						}
						else
						{
							objectSelection.GetObjectData().editorData.Layer = 0;
						}
						ObjEditor.inst.RenderTimelineObject(objectSelection);
					}
				});

				Button mlsLeft = multiLayerSet.transform.GetChild(0).Find("<").GetComponent<Button>();
				mlsLeft.GetComponent<Button>().onClick.RemoveAllListeners();
				mlsLeft.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						if (int.Parse(multiLayerSet.transform.GetChild(0).gameObject.GetComponent<InputField>().text) > 0 && objectSelection.GetObjectData().editorData.Layer > 0)
						{
							if (objectSelection.GetObjectData().editorData.Layer != 6)
							{
								objectSelection.GetObjectData().editorData.Layer -= int.Parse(multiLayerSet.transform.GetChild(0).gameObject.GetComponent<InputField>().text);
							}
							else
							{
								objectSelection.GetObjectData().editorData.Layer -= int.Parse(multiLayerSet.transform.GetChild(0).gameObject.GetComponent<InputField>().text) + 1;
							}
						}
						else
						{
							objectSelection.GetObjectData().editorData.Layer = 0;
						}
						ObjEditor.inst.RenderTimelineObject(objectSelection);
					}
				});

				Button mlsRight = multiLayerSet.transform.GetChild(0).Find(">").GetComponent<Button>();
				mlsRight.GetComponent<Button>().onClick.RemoveAllListeners();
				mlsRight.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						if (objectSelection.GetObjectData().editorData.Layer != 4)
						{
							objectSelection.GetObjectData().editorData.Layer += int.Parse(multiLayerSet.transform.GetChild(0).gameObject.GetComponent<InputField>().text);
						}
						else
						{
							objectSelection.GetObjectData().editorData.Layer += int.Parse(multiLayerSet.transform.GetChild(0).gameObject.GetComponent<InputField>().text) + 1;
						}
						ObjEditor.inst.RenderTimelineObject(objectSelection);
					}
				});

				GameObject multiDepthSet = Instantiate(EventEditor.inst.dialogRight.transform.Find("zoom/zoom").gameObject);
				multiDepthSet.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				multiDepthSet.name = "depth";
				multiDepthSet.transform.GetChild(0).gameObject.GetComponent<InputField>().text = "15";
				multiDepthSet.transform.GetChild(0).Find("input/Placeholder").GetComponent<Text>().text = "Enter depth...";

				multiDepthSet.GetComponent<RectTransform>().sizeDelta = new Vector2(428f, 32f);

				GameObject multiDB = Instantiate(multiDepthSet.transform.GetChild(0).Find("<").gameObject);
				multiDB.transform.SetParent(multiDepthSet.transform.GetChild(0));
				multiDB.transform.SetSiblingIndex(2);
				multiDB.name = "|";
				multiDB.GetComponent<Image>().sprite = barButton.GetComponent<Image>().sprite;

				multiDB.GetComponent<Button>().onClick.RemoveAllListeners();
				multiDB.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						if (objectSelection.IsObject())
						{
							objectSelection.GetObjectData().Depth = objectSelection.GetObjectData().Depth + int.Parse(multiDepthSet.transform.GetChild(0).gameObject.GetComponent<InputField>().text);
							ObjectManager.inst.updateObjects(objectSelection);
						}
						if (objectSelection.IsPrefab())
                        {
							RTEditor.DisplayNotification("MSDP", "Cannot modify the depth of a prefab!", 1f, EditorManager.NotificationType.Error);
                        }
					}
				});

				Button mdsLeft = multiDepthSet.transform.GetChild(0).Find("<").GetComponent<Button>();
				mdsLeft.GetComponent<Button>().onClick.RemoveAllListeners();
				mdsLeft.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						if (objectSelection.IsObject())
						{
							objectSelection.GetObjectData().Depth -= int.Parse(multiDepthSet.transform.GetChild(0).gameObject.GetComponent<InputField>().text);
							ObjectManager.inst.updateObjects(objectSelection);
						}
						if (objectSelection.IsPrefab())
						{
							RTEditor.DisplayNotification("MSDP", "Cannot modify the depth of a prefab!", 1f, EditorManager.NotificationType.Error);
						}
					}
				});

				Button mdsRight = multiDepthSet.transform.GetChild(0).Find(">").GetComponent<Button>();
				mdsRight.GetComponent<Button>().onClick.RemoveAllListeners();
				mdsRight.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						if (objectSelection.IsObject())
						{
							objectSelection.GetObjectData().Depth += int.Parse(multiDepthSet.transform.GetChild(0).gameObject.GetComponent<InputField>().text);
							ObjectManager.inst.updateObjects(objectSelection);
						}
						if (objectSelection.IsPrefab())
						{
							RTEditor.DisplayNotification("MSDP", "Cannot modify the depth of a prefab!", 1f, EditorManager.NotificationType.Error);
						}
					}
				});

				scrollView.transform.Find("Viewport/Content/label layer").SetSiblingIndex(0);

				//Song Time
				GameObject multiTextSongT = Instantiate(scrollView.transform.Find("Viewport/Content/label layer").gameObject);
				multiTextSongT.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				multiTextSongT.transform.GetChild(0).gameObject.GetComponent<Text>().text = "Set Song Time";
				multiTextSongT.name = "label";

				GameObject multiTimeSet = Instantiate(EventEditor.inst.dialogRight.transform.Find("zoom/zoom").gameObject);
				multiTimeSet.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				multiTimeSet.name = "time";
				multiTimeSet.transform.GetChild(0).gameObject.GetComponent<InputField>().text = "0";
				multiTimeSet.transform.GetChild(0).Find("input/Placeholder").GetComponent<Text>().text = "Enter time...";

				multiTimeSet.GetComponent<RectTransform>().sizeDelta = new Vector2(428f, 32f);

				GameObject multiTB = Instantiate(multiTimeSet.transform.GetChild(0).Find("<").gameObject);
				multiTB.transform.SetParent(multiTimeSet.transform.GetChild(0));
				multiTB.transform.SetSiblingIndex(2);
				multiTB.name = "|";
				multiTB.GetComponent<Image>().sprite = barButton.GetComponent<Image>().sprite;

				multiTB.GetComponent<Button>().onClick.RemoveAllListeners();
				multiTB.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						if (objectSelection.IsObject())
						{
							objectSelection.GetObjectData().StartTime = AudioManager.inst.CurrentAudioSource.time;
						}
						if (objectSelection.IsPrefab())
						{
							objectSelection.GetPrefabObjectData().StartTime = AudioManager.inst.CurrentAudioSource.time;
						}

						ObjectManager.inst.updateObjects(objectSelection);
						ObjEditor.inst.RenderTimelineObject(objectSelection);
					}
				});

				Button mtsLeft = multiTimeSet.transform.GetChild(0).Find("<").GetComponent<Button>();
				mtsLeft.GetComponent<Button>().onClick.RemoveAllListeners();
				mtsLeft.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						if (objectSelection.IsObject())
						{
							objectSelection.GetObjectData().StartTime -= float.Parse(multiTimeSet.transform.GetChild(0).gameObject.GetComponent<InputField>().text);
						}
						if (objectSelection.IsPrefab())
						{
							objectSelection.GetPrefabObjectData().StartTime -= float.Parse(multiTimeSet.transform.GetChild(0).gameObject.GetComponent<InputField>().text);
						}

						ObjectManager.inst.updateObjects(objectSelection);
						ObjEditor.inst.RenderTimelineObject(objectSelection);
					}
				});

				Button mtsRight = multiTimeSet.transform.GetChild(0).Find(">").GetComponent<Button>();
				mtsRight.GetComponent<Button>().onClick.RemoveAllListeners();
				mtsRight.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						if (objectSelection.IsObject())
						{
							objectSelection.GetObjectData().StartTime += float.Parse(multiTimeSet.transform.GetChild(0).gameObject.GetComponent<InputField>().text);
						}
						if (objectSelection.IsPrefab())
						{
							objectSelection.GetPrefabObjectData().StartTime += float.Parse(multiTimeSet.transform.GetChild(0).gameObject.GetComponent<InputField>().text);
						}

						ObjectManager.inst.updateObjects(objectSelection);
						ObjEditor.inst.RenderTimelineObject(objectSelection);
					}
				});

				//Name
				GameObject multiTextName = Instantiate(scrollView.transform.Find("Viewport/Content/label layer").gameObject);
				multiTextName.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				multiTextName.transform.GetChild(0).gameObject.GetComponent<Text>().text = "Set Name";
				multiTextName.name = "label";

				GameObject multiNameSet = Instantiate(EventEditor.inst.dialogRight.transform.Find("zoom/zoom").gameObject);
				multiNameSet.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				multiNameSet.name = "name";
				multiNameSet.transform.GetChild(0).gameObject.GetComponent<InputField>().characterValidation = InputField.CharacterValidation.None;
				multiNameSet.transform.GetChild(0).gameObject.GetComponent<InputField>().characterLimit = 0;
				multiNameSet.transform.GetChild(0).gameObject.GetComponent<InputField>().text = "name";
				multiNameSet.transform.GetChild(0).Find("input/Placeholder").GetComponent<Text>().text = "Enter name...";

				multiNameSet.GetComponent<RectTransform>().sizeDelta = new Vector2(428f, 32f);

				GameObject multiNB = Instantiate(multiNameSet.transform.GetChild(0).Find("<").gameObject);
				multiNB.transform.SetParent(multiNameSet.transform.GetChild(0));
				multiNB.transform.SetSiblingIndex(2);
				multiNB.name = "|";
				multiNB.GetComponent<Image>().sprite = barButton.GetComponent<Image>().sprite;

				multiNB.GetComponent<Button>().onClick.RemoveAllListeners();
				multiNB.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						if (objectSelection.IsObject())
						{
							objectSelection.GetObjectData().name = multiNameSet.transform.GetChild(0).gameObject.GetComponent<InputField>().text;
							ObjEditor.inst.RenderTimelineObject(objectSelection);
						}
						if (objectSelection.IsPrefab())
						{
							RTEditor.DisplayNotification("MSNP", "Cannot modify the name of a prefab!", 1f, EditorManager.NotificationType.Error);
						}
					}
				});

				Destroy(multiNameSet.transform.GetChild(0).Find("<").gameObject);
				Button mtnLeft = multiNameSet.transform.GetChild(0).Find("<").GetComponent<Button>();

				Button mtnRight = multiNameSet.transform.GetChild(0).Find(">").GetComponent<Button>();

				string jpgFileLocation = RTFile.GetApplicationDirectory() + "BepInEx/plugins/Assets/add.png";

				if (RTFile.FileExists("BepInEx/plugins/Assets/add.png"))
				{
					Image spriteReloader = multiNameSet.transform.GetChild(0).Find(">").GetComponent<Image>();

					EditorManager.inst.StartCoroutine(EditorManager.inst.GetSprite(jpgFileLocation, new EditorManager.SpriteLimits(), delegate (Sprite cover)
					{
						spriteReloader.sprite = cover;
					}, delegate (string errorFile)
					{
						spriteReloader.sprite = ArcadeManager.inst.defaultImage;
					}));
				}

				LayoutElement mtnLeftLE = multiNameSet.transform.GetChild(0).Find(">").gameObject.AddComponent<LayoutElement>();
				mtnLeftLE.ignoreLayout = true;

				RectTransform mtnLeftRT = multiNameSet.transform.GetChild(0).Find(">").GetComponent<RectTransform>();
				mtnLeftRT.anchoredPosition = new Vector2(339f, 0f);
				mtnLeftRT.sizeDelta = new Vector2(32f, 32f);

				mtnRight.GetComponent<Button>().onClick.RemoveAllListeners();
				mtnRight.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						if (objectSelection.IsObject())
						{
							objectSelection.GetObjectData().name += multiNameSet.transform.GetChild(0).gameObject.GetComponent<InputField>().text;
							ObjEditor.inst.RenderTimelineObject(objectSelection);
						}
						if (objectSelection.IsPrefab())
						{
							RTEditor.DisplayNotification("MSNP", "Cannot modify the name of a prefab!", 1f, EditorManager.NotificationType.Error);
						}
					}
				});

				//Song Time Autokill
				GameObject multiTextSongAK = Instantiate(scrollView.transform.Find("Viewport/Content/label layer").gameObject);
				multiTextSongAK.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				multiTextSongAK.transform.GetChild(0).gameObject.GetComponent<Text>().text = "Set Song Time Autokill to Current";
				multiTextSongAK.name = "label";

				GameObject setAutokill = Instantiate(eventButton);
				setAutokill.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				setAutokill.name = "set autokill";

				setAutokill.transform.GetChild(0).GetComponent<Text>().text = "Set";
				setAutokill.GetComponent<Image>().color = bcol;

				setAutokill.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
				setAutokill.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
				setAutokill.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
				setAutokill.GetComponent<Button>().onClick.RemoveAllListeners();
				setAutokill.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						if (objectSelection.IsObject())
						{
							objectSelection.GetObjectData().autoKillType = DataManager.GameData.BeatmapObject.AutoKillType.SongTime;
							objectSelection.GetObjectData().autoKillOffset = AudioManager.inst.CurrentAudioSource.time;
							ObjectManager.inst.updateObjects(objectSelection, false);
							ObjEditor.inst.RenderTimelineObject(objectSelection);
						}
						if (objectSelection.IsPrefab())
						{
							RTEditor.DisplayNotification("MSAKP", "Cannot set autokill of a prefab!", 1f, EditorManager.NotificationType.Error);
						}
					}
				});

				GameObject multiTextTypeCycle = Instantiate(scrollView.transform.Find("Viewport/Content/label layer").gameObject);
				multiTextTypeCycle.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				multiTextTypeCycle.transform.GetChild(0).gameObject.GetComponent<Text>().text = "Cycle object type";
				multiTextTypeCycle.name = "label";

				GameObject cycleObjType = Instantiate(eventButton);
				cycleObjType.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				cycleObjType.name = "cycle obj type";

				cycleObjType.transform.GetChild(0).GetComponent<Text>().text = "Cycle";
				cycleObjType.GetComponent<Image>().color = bcol;

				cycleObjType.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
				cycleObjType.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
				cycleObjType.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
				cycleObjType.GetComponent<Button>().onClick.RemoveAllListeners();
				cycleObjType.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						if (objectSelection.IsObject())
						{
							objectSelection.GetObjectData().objectType += 1;
							if ((int)objectSelection.GetObjectData().objectType > 3)
							{
								objectSelection.GetObjectData().objectType = 0;
							}
							ObjectManager.inst.updateObjects(objectSelection, false);
							ObjEditor.inst.RenderTimelineObject(objectSelection);
						}
						if (objectSelection.IsPrefab())
						{
							RTEditor.DisplayNotification("MSOTP", "Cannot set object type of a prefab!", 1f, EditorManager.NotificationType.Error);
						}
					}
				});

				GameObject multiTextLockSwap = Instantiate(scrollView.transform.Find("Viewport/Content/label layer").gameObject);
				multiTextLockSwap.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				multiTextLockSwap.transform.GetChild(0).gameObject.GetComponent<Text>().text = "Swap each object's lock state";
				multiTextLockSwap.name = "label";

				GameObject lockSwap = Instantiate(eventButton);
				lockSwap.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				lockSwap.name = "lock swap";

				lockSwap.transform.GetChild(0).GetComponent<Text>().text = "Swap Lock";
				lockSwap.GetComponent<Image>().color = bcol;

				lockSwap.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
				lockSwap.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
				lockSwap.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
				lockSwap.GetComponent<Button>().onClick.RemoveAllListeners();
				lockSwap.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						if (objectSelection.IsObject())
						{
							objectSelection.GetObjectData().editorData.locked = !objectSelection.GetObjectData().editorData.locked;
						}
						if (objectSelection.IsPrefab())
						{
							objectSelection.GetPrefabObjectData().editorData.locked = !objectSelection.GetPrefabObjectData().editorData.locked;
						}

						ObjectManager.inst.updateObjects(objectSelection, false);
						ObjEditor.inst.RenderTimelineObject(objectSelection);
					}
				});

				GameObject multiTextLockToggle = Instantiate(scrollView.transform.Find("Viewport/Content/label layer").gameObject);
				multiTextLockToggle.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				multiTextLockToggle.transform.GetChild(0).gameObject.GetComponent<Text>().text = "Toggle all object's lock state";
				multiTextLockToggle.name = "label";

				GameObject lockToggle = Instantiate(eventButton);
				lockToggle.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				lockToggle.name = "lock toggle";

				lockToggle.transform.GetChild(0).GetComponent<Text>().text = "Toggle Lock";
				lockToggle.GetComponent<Image>().color = bcol;

				bool loggle = false;

				lockToggle.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
				lockToggle.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
				lockToggle.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
				lockToggle.GetComponent<Button>().onClick.RemoveAllListeners();
				lockToggle.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					loggle = !loggle;
					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						if (objectSelection.IsObject())
						{
							if (loggle == false)
							{
								objectSelection.GetObjectData().editorData.locked = false;
							}
							if (loggle == true)
							{
								objectSelection.GetObjectData().editorData.locked = true;
							}
						}
						if (objectSelection.IsPrefab())
						{
							if (loggle == false)
							{
								objectSelection.GetPrefabObjectData().editorData.locked = false;
							}
							if (loggle == true)
							{
								objectSelection.GetPrefabObjectData().editorData.locked = true;
							}
						}

						ObjectManager.inst.updateObjects(objectSelection, false);
						ObjEditor.inst.RenderTimelineObject(objectSelection);
					}
				});

				GameObject multiTextCollapseSwap = Instantiate(scrollView.transform.Find("Viewport/Content/label layer").gameObject);
				multiTextCollapseSwap.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				multiTextCollapseSwap.transform.GetChild(0).gameObject.GetComponent<Text>().text = "Swap each object's collapse state";
				multiTextCollapseSwap.name = "label";

				GameObject collapseSwap = Instantiate(eventButton);
				collapseSwap.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				collapseSwap.name = "collapse swap";

				collapseSwap.transform.GetChild(0).GetComponent<Text>().text = "Swap Collapse";
				collapseSwap.GetComponent<Image>().color = bcol;

				collapseSwap.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
				collapseSwap.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
				collapseSwap.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
				collapseSwap.GetComponent<Button>().onClick.RemoveAllListeners();
				collapseSwap.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						if (objectSelection.IsObject())
						{
							objectSelection.GetObjectData().editorData.collapse = !objectSelection.GetObjectData().editorData.collapse;
						}
						if (objectSelection.IsPrefab())
						{
							objectSelection.GetPrefabObjectData().editorData.collapse = !objectSelection.GetPrefabObjectData().editorData.collapse;
						}

						ObjectManager.inst.updateObjects(objectSelection, false);
						ObjEditor.inst.RenderTimelineObject(objectSelection);
					}
				});

				GameObject multiTextCollapseToggle = Instantiate(scrollView.transform.Find("Viewport/Content/label layer").gameObject);
				multiTextCollapseToggle.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				multiTextCollapseToggle.transform.GetChild(0).gameObject.GetComponent<Text>().text = "Toggle all object's collapse state";
				multiTextCollapseToggle.name = "label";

				GameObject collapseToggle = Instantiate(eventButton);
				collapseToggle.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				collapseToggle.name = "collapse toggle";

				collapseToggle.transform.GetChild(0).GetComponent<Text>().text = "Toggle Collapse";
				collapseToggle.GetComponent<Image>().color = bcol;

				bool coggle = false;

				collapseToggle.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
				collapseToggle.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
				collapseToggle.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
				collapseToggle.GetComponent<Button>().onClick.RemoveAllListeners();
				collapseToggle.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					coggle = !coggle;
					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						if (objectSelection.IsObject())
						{
							if (coggle == false)
							{
								objectSelection.GetObjectData().editorData.collapse = false;
							}
							if (coggle == true)
							{
								objectSelection.GetObjectData().editorData.collapse = true;
							}
						}
						if (objectSelection.IsPrefab())
						{
							if (coggle == false)
							{
								objectSelection.GetPrefabObjectData().editorData.collapse = false;
							}
							if (coggle == true)
							{
								objectSelection.GetPrefabObjectData().editorData.collapse = true;
							}
						}

						ObjectManager.inst.updateObjects(objectSelection, false);
						ObjEditor.inst.RenderTimelineObject(objectSelection);
					}
				});

                //Sync object selection
                {
					GameObject multiTextSync = Instantiate(scrollView.transform.Find("Viewport/Content/label layer").gameObject);
					multiTextSync.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
					multiTextSync.transform.GetChild(0).gameObject.GetComponent<Text>().text = "Sync to first selected object";
					multiTextSync.name = "label";

					GameObject multiSync = new GameObject("sync layout");
					multiSync.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
					RectTransform multiSyncRT = multiSync.AddComponent<RectTransform>();
					GridLayoutGroup multiSyncGLG = multiSync.AddComponent<GridLayoutGroup>();
					multiSyncGLG.spacing = new Vector2(4f, 4f);
					multiSyncGLG.cellSize = new Vector2(61.6f, 49f);

					GameObject syncStartTime = Instantiate(eventButton);
					syncStartTime.transform.SetParent(scrollView.transform.Find("Viewport/Content/sync layout"));
					syncStartTime.name = "start time";

					syncStartTime.transform.GetChild(0).GetComponent<Text>().text = "ST";
					syncStartTime.GetComponent<Image>().color = bcol;

					syncStartTime.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
					syncStartTime.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
					syncStartTime.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
					syncStartTime.GetComponent<Button>().onClick.RemoveAllListeners();
					syncStartTime.GetComponent<Button>().onClick.AddListener(delegate ()
					{
						foreach (var objectSelection in ObjEditor.inst.selectedObjects)
						{
							if (objectSelection.IsObject() && ObjEditor.inst.selectedObjects[0].IsObject())
							{
								objectSelection.GetObjectData().StartTime = ObjEditor.inst.selectedObjects[0].GetObjectData().StartTime;
								ObjectManager.inst.updateObjects(objectSelection, false);
								ObjEditor.inst.RenderTimelineObject(objectSelection);
							}
							if (objectSelection.IsPrefab())
							{
								RTEditor.DisplayNotification("MSSSTP", "Cannot sync prefab to object!", 1f, EditorManager.NotificationType.Error);
							}
						}
					});

					GameObject syncName = Instantiate(eventButton);
					syncName.transform.SetParent(scrollView.transform.Find("Viewport/Content/sync layout"));
					syncName.name = "name";

					syncName.transform.GetChild(0).GetComponent<Text>().text = "N";
					syncName.GetComponent<Image>().color = bcol;

					syncName.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
					syncName.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
					syncName.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
					syncName.GetComponent<Button>().onClick.RemoveAllListeners();
					syncName.GetComponent<Button>().onClick.AddListener(delegate ()
					{
						foreach (var objectSelection in ObjEditor.inst.selectedObjects)
						{
							if (objectSelection.IsObject() && ObjEditor.inst.selectedObjects[0].IsObject())
							{
								objectSelection.GetObjectData().name = ObjEditor.inst.selectedObjects[0].GetObjectData().name;
								ObjEditor.inst.RenderTimelineObject(objectSelection);
							}
							if (objectSelection.IsPrefab())
							{
								RTEditor.DisplayNotification("MSSNP", "Cannot sync prefab to object!", 1f, EditorManager.NotificationType.Error);
							}
						}
					});

					GameObject syncObjectType = Instantiate(eventButton);
					syncObjectType.transform.SetParent(scrollView.transform.Find("Viewport/Content/sync layout"));
					syncObjectType.name = "object type";

					syncObjectType.transform.GetChild(0).GetComponent<Text>().text = "OT";
					syncObjectType.GetComponent<Image>().color = bcol;

					syncObjectType.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
					syncObjectType.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
					syncObjectType.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
					syncObjectType.GetComponent<Button>().onClick.RemoveAllListeners();
					syncObjectType.GetComponent<Button>().onClick.AddListener(delegate ()
					{
						foreach (var objectSelection in ObjEditor.inst.selectedObjects)
						{
							if (objectSelection.IsObject() && ObjEditor.inst.selectedObjects[0].IsObject())
							{
								objectSelection.GetObjectData().objectType = ObjEditor.inst.selectedObjects[0].GetObjectData().objectType;
								ObjectManager.inst.updateObjects(objectSelection, false);
								ObjEditor.inst.RenderTimelineObject(objectSelection);
							}
							if (objectSelection.IsPrefab())
							{
								RTEditor.DisplayNotification("MSSOTP", "Cannot sync prefab to object!", 1f, EditorManager.NotificationType.Error);
							}
						}
					});

					GameObject syncAutokillType = Instantiate(eventButton);
					syncAutokillType.transform.SetParent(scrollView.transform.Find("Viewport/Content/sync layout"));
					syncAutokillType.name = "autokill type";

					syncAutokillType.transform.GetChild(0).GetComponent<Text>().text = "AKT";
					syncAutokillType.GetComponent<Image>().color = bcol;

					syncAutokillType.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
					syncAutokillType.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
					syncAutokillType.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
					syncAutokillType.GetComponent<Button>().onClick.RemoveAllListeners();
					syncAutokillType.GetComponent<Button>().onClick.AddListener(delegate ()
					{
						foreach (var objectSelection in ObjEditor.inst.selectedObjects)
						{
							if (objectSelection.IsObject() && ObjEditor.inst.selectedObjects[0].IsObject())
							{
								objectSelection.GetObjectData().autoKillType = ObjEditor.inst.selectedObjects[0].GetObjectData().autoKillType;
								ObjectManager.inst.updateObjects(objectSelection, false);
								ObjEditor.inst.RenderTimelineObject(objectSelection);
							}
							if (objectSelection.IsPrefab())
							{
								RTEditor.DisplayNotification("MSSAKTP", "Cannot sync prefab to object!", 1f, EditorManager.NotificationType.Error);
							}
						}
					});

					GameObject syncAutokillOffset = Instantiate(eventButton);
					syncAutokillOffset.transform.SetParent(scrollView.transform.Find("Viewport/Content/sync layout"));
					syncAutokillOffset.name = "autokill offset";

					syncAutokillOffset.transform.GetChild(0).GetComponent<Text>().text = "AKO";
					syncAutokillOffset.GetComponent<Image>().color = bcol;

					syncAutokillOffset.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
					syncAutokillOffset.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
					syncAutokillOffset.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
					syncAutokillOffset.GetComponent<Button>().onClick.RemoveAllListeners();
					syncAutokillOffset.GetComponent<Button>().onClick.AddListener(delegate ()
					{
						foreach (var objectSelection in ObjEditor.inst.selectedObjects)
						{
							if (objectSelection.IsObject() && ObjEditor.inst.selectedObjects[0].IsObject())
							{
								objectSelection.GetObjectData().autoKillOffset = ObjEditor.inst.selectedObjects[0].GetObjectData().autoKillOffset;
								ObjectManager.inst.updateObjects(objectSelection, false);
								ObjEditor.inst.RenderTimelineObject(objectSelection);
							}
							if (objectSelection.IsPrefab())
							{
								RTEditor.DisplayNotification("MSSAKOP", "Cannot sync prefab to object!", 1f, EditorManager.NotificationType.Error);
							}
						}
					});

					GameObject syncParent = Instantiate(eventButton);
					syncParent.transform.SetParent(scrollView.transform.Find("Viewport/Content/sync layout"));
					syncParent.name = "parent";

					syncParent.transform.GetChild(0).GetComponent<Text>().text = "P";
					syncParent.GetComponent<Image>().color = bcol;

					syncParent.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
					syncParent.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
					syncParent.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
					syncParent.GetComponent<Button>().onClick.RemoveAllListeners();
					syncParent.GetComponent<Button>().onClick.AddListener(delegate ()
					{
						foreach (var objectSelection in ObjEditor.inst.selectedObjects)
						{
							if (objectSelection.IsObject() && ObjEditor.inst.selectedObjects[0].IsObject())
							{
								objectSelection.GetObjectData().parent = ObjEditor.inst.selectedObjects[0].GetObjectData().parent;
								ObjectManager.inst.updateObjects(objectSelection, false);
								ObjEditor.inst.RenderTimelineObject(objectSelection);
							}
							if (objectSelection.IsPrefab())
							{
								RTEditor.DisplayNotification("MSSPP", "Cannot sync prefab to object!", 1f, EditorManager.NotificationType.Error);
							}
						}
					});

					GameObject syncParentType = Instantiate(eventButton);
					syncParentType.transform.SetParent(scrollView.transform.Find("Viewport/Content/sync layout"));
					syncParentType.name = "parent type";

					syncParentType.transform.GetChild(0).GetComponent<Text>().text = "PT";
					syncParentType.GetComponent<Image>().color = bcol;

					syncParentType.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
					syncParentType.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
					syncParentType.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
					syncParentType.GetComponent<Button>().onClick.RemoveAllListeners();
					syncParentType.GetComponent<Button>().onClick.AddListener(delegate ()
					{
						foreach (var objectSelection in ObjEditor.inst.selectedObjects)
						{
							if (objectSelection.IsObject() && ObjEditor.inst.selectedObjects[0].IsObject())
							{
								for (int i = 0; i < 3; i++)
								{
									objectSelection.GetObjectData().SetParentType(i, ObjEditor.inst.selectedObjects[0].GetObjectData().GetParentType(i));
								}
								ObjectManager.inst.updateObjects(objectSelection, false);
								ObjEditor.inst.RenderTimelineObject(objectSelection);
							}
							if (objectSelection.IsPrefab())
							{
								RTEditor.DisplayNotification("MSSPTP", "Cannot sync prefab to object!", 1f, EditorManager.NotificationType.Error);
							}
						}
					});

					GameObject syncParentOffset = Instantiate(eventButton);
					syncParentOffset.transform.SetParent(scrollView.transform.Find("Viewport/Content/sync layout"));
					syncParentOffset.name = "parent offset";

					syncParentOffset.transform.GetChild(0).GetComponent<Text>().text = "PO";
					syncParentOffset.GetComponent<Image>().color = bcol;

					syncParentOffset.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
					syncParentOffset.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
					syncParentOffset.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
					syncParentOffset.GetComponent<Button>().onClick.RemoveAllListeners();
					syncParentOffset.GetComponent<Button>().onClick.AddListener(delegate ()
					{
						foreach (var objectSelection in ObjEditor.inst.selectedObjects)
						{
							if (objectSelection.IsObject() && ObjEditor.inst.selectedObjects[0].IsObject())
							{
								for (int i = 0; i < 3; i++)
								{
									objectSelection.GetObjectData().SetParentOffset(i, ObjEditor.inst.selectedObjects[0].GetObjectData().getParentOffset(i));
								}
								ObjectManager.inst.updateObjects(objectSelection, false);
								ObjEditor.inst.RenderTimelineObject(objectSelection);
							}
							if (objectSelection.IsPrefab())
							{
								RTEditor.DisplayNotification("MSSPOP", "Cannot sync prefab to object!", 1f, EditorManager.NotificationType.Error);
							}
						}
					});

					GameObject syncOrigin = Instantiate(eventButton);
					syncOrigin.transform.SetParent(scrollView.transform.Find("Viewport/Content/sync layout"));
					syncOrigin.name = "origin";

					syncOrigin.transform.GetChild(0).GetComponent<Text>().text = "O";
					syncOrigin.GetComponent<Image>().color = bcol;

					syncOrigin.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
					syncOrigin.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
					syncOrigin.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
					syncOrigin.GetComponent<Button>().onClick.RemoveAllListeners();
					syncOrigin.GetComponent<Button>().onClick.AddListener(delegate ()
					{
						foreach (var objectSelection in ObjEditor.inst.selectedObjects)
						{
							if (objectSelection.IsObject() && ObjEditor.inst.selectedObjects[0].IsObject())
							{
								objectSelection.GetObjectData().origin = ObjEditor.inst.selectedObjects[0].GetObjectData().origin;
								ObjectManager.inst.updateObjects(objectSelection, false);
								ObjEditor.inst.RenderTimelineObject(objectSelection);
							}
							if (objectSelection.IsPrefab())
							{
								RTEditor.DisplayNotification("MSSOP", "Cannot sync prefab to object!", 1f, EditorManager.NotificationType.Error);
							}
						}
					});

					GameObject syncShape = Instantiate(eventButton);
					syncShape.transform.SetParent(scrollView.transform.Find("Viewport/Content/sync layout"));
					syncShape.name = "shape";

					syncShape.transform.GetChild(0).GetComponent<Text>().text = "S";
					syncShape.GetComponent<Image>().color = bcol;

					syncShape.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
					syncShape.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
					syncShape.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
					syncShape.GetComponent<Button>().onClick.RemoveAllListeners();
					syncShape.GetComponent<Button>().onClick.AddListener(delegate ()
					{
						foreach (var objectSelection in ObjEditor.inst.selectedObjects)
						{
							if (objectSelection.IsObject() && ObjEditor.inst.selectedObjects[0].IsObject())
							{
								objectSelection.GetObjectData().shape = ObjEditor.inst.selectedObjects[0].GetObjectData().shape;
								objectSelection.GetObjectData().shapeOption = ObjEditor.inst.selectedObjects[0].GetObjectData().shapeOption;
								ObjectManager.inst.updateObjects(objectSelection, false);
								ObjEditor.inst.RenderTimelineObject(objectSelection);
							}
							if (objectSelection.IsPrefab())
							{
								RTEditor.DisplayNotification("MSSSP", "Cannot sync prefab to object!", 1f, EditorManager.NotificationType.Error);
							}
						}
					});

					GameObject syncText = Instantiate(eventButton);
					syncText.transform.SetParent(scrollView.transform.Find("Viewport/Content/sync layout"));
					syncText.name = "text";

					syncText.transform.GetChild(0).GetComponent<Text>().text = "T";
					syncText.GetComponent<Image>().color = bcol;

					syncText.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
					syncText.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
					syncText.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
					syncText.GetComponent<Button>().onClick.RemoveAllListeners();
					syncText.GetComponent<Button>().onClick.AddListener(delegate ()
					{
						foreach (var objectSelection in ObjEditor.inst.selectedObjects)
						{
							if (objectSelection.IsObject() && ObjEditor.inst.selectedObjects[0].IsObject())
							{
								objectSelection.GetObjectData().text = ObjEditor.inst.selectedObjects[0].GetObjectData().text;
								ObjectManager.inst.updateObjects(objectSelection, false);
								ObjEditor.inst.RenderTimelineObject(objectSelection);
							}
							if (objectSelection.IsPrefab())
							{
								RTEditor.DisplayNotification("MSTP", "Cannot sync prefab to object!", 1f, EditorManager.NotificationType.Error);
							}
						}
					});

					GameObject syncDepth = Instantiate(eventButton);
					syncDepth.transform.SetParent(scrollView.transform.Find("Viewport/Content/sync layout"));
					syncDepth.name = "depth";

					syncDepth.transform.GetChild(0).GetComponent<Text>().text = "D";
					syncDepth.GetComponent<Image>().color = bcol;

					syncDepth.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
					syncDepth.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
					syncDepth.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
					syncDepth.GetComponent<Button>().onClick.RemoveAllListeners();
					syncDepth.GetComponent<Button>().onClick.AddListener(delegate ()
					{
						foreach (var objectSelection in ObjEditor.inst.selectedObjects)
						{
							if (objectSelection.IsObject() && ObjEditor.inst.selectedObjects[0].IsObject())
							{
								objectSelection.GetObjectData().Depth = ObjEditor.inst.selectedObjects[0].GetObjectData().Depth;
								ObjectManager.inst.updateObjects(objectSelection, false);
								ObjEditor.inst.RenderTimelineObject(objectSelection);
							}
							if (objectSelection.IsPrefab())
							{
								RTEditor.DisplayNotification("MSDP", "Cannot sync prefab to object!", 1f, EditorManager.NotificationType.Error);
							}
						}
					});

					//ISSUE: Causes newly selected objects to retain the values of the previous object for some reason
					//GameObject syncKeyframes = Instantiate(eventButton);
					//syncKeyframes.transform.SetParent(scrollView.transform.Find("Viewport/Content/sync layout"));

					//syncKeyframes.transform.GetChild(0).GetComponent<Text>().text = "KF";
					//syncKeyframes.GetComponent<Image>().color = bcol;

					//syncKeyframes.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
					//syncKeyframes.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
					//syncKeyframes.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
					//syncKeyframes.GetComponent<Button>().onClick.RemoveAllListeners();
					//syncKeyframes.GetComponent<Button>().onClick.AddListener(delegate ()
					//{
					//	for (int i = 1; i < ObjEditor.inst.selectedObjects.Count; i++)
					//    {
					//		for (int j = 1; j < ObjEditor.inst.selectedObjects[i].GetObjectData().events[0].Count; j++)
                    //        {
					//			ObjEditor.inst.selectedObjects[i].GetObjectData().events[0].Clear();
					//			ObjEditor.inst.selectedObjects[i].GetObjectData().events[0].Add(ObjEditor.inst.selectedObjects[0].GetObjectData().events[0][j]);
					//		}
					//		for (int j = 1; j < ObjEditor.inst.selectedObjects[i].GetObjectData().events[1].Count; j++)
                    //        {
					//			ObjEditor.inst.selectedObjects[i].GetObjectData().events[1].Clear();
					//			ObjEditor.inst.selectedObjects[i].GetObjectData().events[1].Add(ObjEditor.inst.selectedObjects[0].GetObjectData().events[1][j]);
					//		}
					//		for (int j = 1; j < ObjEditor.inst.selectedObjects[i].GetObjectData().events[2].Count; j++)
                    //        {
					//			ObjEditor.inst.selectedObjects[i].GetObjectData().events[2].Clear();
					//			ObjEditor.inst.selectedObjects[i].GetObjectData().events[2].Add(ObjEditor.inst.selectedObjects[0].GetObjectData().events[2][j]);
					//		}
					//		for (int j = 1; j < ObjEditor.inst.selectedObjects[i].GetObjectData().events[3].Count; j++)
                    //        {
					//			ObjEditor.inst.selectedObjects[i].GetObjectData().events[3].Clear();
					//			ObjEditor.inst.selectedObjects[i].GetObjectData().events[3].Add(ObjEditor.inst.selectedObjects[0].GetObjectData().events[3][j]);
					//		}
					//
					//		ObjectManager.inst.updateObjects(ObjEditor.inst.selectedObjects[i], false);
					//		ObjEditor.inst.RenderTimelineObject(ObjEditor.inst.selectedObjects[i]);
					//	}
					//});
				}

				EditorManager.inst.GetDialog("Multi Object Editor").Dialog.Find("data").GetComponent<RectTransform>().sizeDelta = new Vector2(810f, 730.11f);
				EditorManager.inst.GetDialog("Multi Object Editor").Dialog.Find("data/left").GetComponent<RectTransform>().sizeDelta = new Vector2(355f, 730f);
			}
		}

		[HarmonyPatch(typeof(PrefabEditor), "CollapseCurrentPrefab")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> CCPrefabTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			var matcher = new CodeMatcher(instructions).Start().Advance(128);
			matcher = matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ObjEditor), "inst")));
			matcher = matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ObjEditor), "beatmapObjects")));
			matcher = matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, 9));
			matcher = matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(DataManager.GameData.BeatmapObject), "id")));
			matcher = matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Dictionary<string, GameObject>), "Remove", new[] { typeof(string) })));
			matcher = matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Pop));
			return matcher.InstructionEnumeration();
		}

		public static string levelListPath = "beatmaps/editor";
		public static string levelListSlash = "beatmaps/editor/";

		[HarmonyPatch(typeof(SystemManager), "Update")]
		[HarmonyPrefix]
		private static bool SystemManagerUpdatePrefix()
		{
			if ((Input.GetKeyDown(KeyCode.P) && !LSHelpers.IsUsingInputField()) || (Input.GetKeyDown(KeyCode.F9) && !LSHelpers.IsUsingInputField()))
			{
				TakeScreenshot();
			}
			if (Input.GetKeyDown(KeyCode.F11) && !LSHelpers.IsUsingInputField())
			{
				DataManager.inst.UpdateSettingBool("FullScreen", !DataManager.inst.GetSettingBool("FullScreen"));
				SaveManager.inst.ApplyVideoSettings();
			}
			return false;
        }

		public static void TakeScreenshot()
		{
			string text = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf("/")) + "/screenshots";
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			ScreenCapture.CaptureScreenshot(text + "/" + DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss") + ".png", 1);
			Debug.Log(string.Concat(new string[]
			{
				"Took Screenshot! - ",
				text,
				"/",
				DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss"),
				".png"
			}));
		}

		public static void RepeatReminder()
        {
			EditorManager.inst.CancelInvoke("CreateGrid");
			EditorManager.inst.InvokeRepeating("CreateGrid", ReminderRepeat.Value, ReminderRepeat.Value);
		}

		public static bool multiObjectState = true;

		public static void LoadLevelEnumerator()
        {
			SetAutosave();
			if (!RTFile.DirectoryExists(RTFile.GetApplicationDirectory() + GameManager.inst.basePath + "autosaves"))
			{
				Directory.CreateDirectory(RTFile.GetApplicationDirectory() + GameManager.inst.basePath + "autosaves");
			}
			string[] files = Directory.GetFiles(FileManager.GetAppPath() + "/" + GameManager.inst.basePath, "autosaves/autosave_*.lsb", SearchOption.TopDirectoryOnly);
			files.ToList().Sort();
			int num = 0;
			foreach (string text2 in files)
			{
				if (num != files.Count() - 1)
				{
					File.Delete(text2);
				}
				num++;
			}
			CreateMultiObjectEditor();
			if (GenerateWaveform.Value == true)
            {
				AssignTimelineTexture();
			}
			else
            {
				EditorManager.inst.timeline.GetComponent<Image>().sprite = null;
				EditorManager.inst.timelineWaveformOverlay.GetComponent<Image>().sprite = null;
			}
		}

		public static void SetAutosave()
        {
			EditorManager.inst.CancelInvoke("AutoSaveLevel");
			RTEditor.inst.CancelInvoke("AutoSaveLevel");
			RTEditor.inst.InvokeRepeating("AutoSaveLevel", AutoSaveRepeat.Value, AutoSaveRepeat.Value);
		}

		public static void RenderBeatmapSet()
		{
			int foldClamp = FButtonFoldClamp.Value;
			int songClamp = FButtonSongClamp.Value;
			int artiClamp = FButtonArtiClamp.Value;
			int creaClamp = FButtonCreaClamp.Value;
			int descClamp = FButtonDescClamp.Value;
			int dateClamp = FButtonDateClamp.Value;


			if (FButtonFoldClamp.Value < 3)
			{
				foldClamp = 14;
			}

			if (FButtonSongClamp.Value < 3)
			{
				songClamp = 22;
			}

			if (FButtonArtiClamp.Value < 3)
			{
				artiClamp = 16;
			}

			if (FButtonCreaClamp.Value < 3)
			{
				creaClamp = 16;
			}

			if (FButtonDescClamp.Value < 3)
			{
				descClamp = 16;
			}

			if (FButtonDateClamp.Value < 3)
			{
				dateClamp = 16;
			}

			//Cover
			if (levelFilter == 0 && levelAscend == false)
			{
				List<EditorManager.MetadataWrapper> result = new List<EditorManager.MetadataWrapper>();
				result = (from x in EditorManager.inst.loadedLevels
						  orderby x.albumArt != EditorManager.inst.AlbumArt descending
						  select x).ToList<EditorManager.MetadataWrapper>();

				EditorManager.inst.loadedLevels = result;
			}
			if (levelFilter == 0 && levelAscend == true)
			{
				List<EditorManager.MetadataWrapper> result = new List<EditorManager.MetadataWrapper>();
				result = (from x in EditorManager.inst.loadedLevels
						  orderby x.albumArt != EditorManager.inst.AlbumArt ascending
						  select x).ToList<EditorManager.MetadataWrapper>();

				EditorManager.inst.loadedLevels = result;
			}

			//Artist
			if (levelFilter == 1 && levelAscend == false)
			{
				List<EditorManager.MetadataWrapper> result = new List<EditorManager.MetadataWrapper>();
				result = (from x in EditorManager.inst.loadedLevels
						  orderby x.metadata.artist.Name descending
						  select x).ToList<EditorManager.MetadataWrapper>();

				EditorManager.inst.loadedLevels = result;
			}
			if (levelFilter == 1 && levelAscend == true)
			{
				List<EditorManager.MetadataWrapper> result = new List<EditorManager.MetadataWrapper>();
				result = (from x in EditorManager.inst.loadedLevels
						  orderby x.metadata.artist.Name ascending
						  select x).ToList<EditorManager.MetadataWrapper>();

				EditorManager.inst.loadedLevels = result;
			}

			//Creator
			if (levelFilter == 2 && levelAscend == false)
			{
				List<EditorManager.MetadataWrapper> result = new List<EditorManager.MetadataWrapper>();
				result = (from x in EditorManager.inst.loadedLevels
						  orderby x.metadata.creator.steam_name descending
						  select x).ToList<EditorManager.MetadataWrapper>();

				EditorManager.inst.loadedLevels = result;
			}
			if (levelFilter == 2 && levelAscend == true)
			{
				List<EditorManager.MetadataWrapper> result = new List<EditorManager.MetadataWrapper>();
				result = (from x in EditorManager.inst.loadedLevels
						  orderby x.metadata.creator.steam_name ascending
						  select x).ToList<EditorManager.MetadataWrapper>();

				EditorManager.inst.loadedLevels = result;
			}

			//Folder
			if (levelFilter == 3 && levelAscend == false)
			{
				List<EditorManager.MetadataWrapper> result = new List<EditorManager.MetadataWrapper>();
				result = (from x in EditorManager.inst.loadedLevels
						  orderby x.folder descending
						  select x).ToList<EditorManager.MetadataWrapper>();

				EditorManager.inst.loadedLevels = result;
			}
			if (levelFilter == 3 && levelAscend == true)
			{
				List<EditorManager.MetadataWrapper> result = new List<EditorManager.MetadataWrapper>();
				result = (from x in EditorManager.inst.loadedLevels
						  orderby x.folder ascending
						  select x).ToList<EditorManager.MetadataWrapper>();

				EditorManager.inst.loadedLevels = result;
			}

			//Title
			if (levelFilter == 4 && levelAscend == false)
			{
				List<EditorManager.MetadataWrapper> result = new List<EditorManager.MetadataWrapper>();
				result = (from x in EditorManager.inst.loadedLevels
						  orderby x.metadata.song.title descending
						  select x).ToList<EditorManager.MetadataWrapper>();

				EditorManager.inst.loadedLevels = result;
			}
			if (levelFilter == 4 && levelAscend == true)
			{
				List<EditorManager.MetadataWrapper> result = new List<EditorManager.MetadataWrapper>();
				result = (from x in EditorManager.inst.loadedLevels
						  orderby x.metadata.song.title ascending
						  select x).ToList<EditorManager.MetadataWrapper>();

				EditorManager.inst.loadedLevels = result;
			}

			//Difficulty
			if (levelFilter == 5 && levelAscend == false)
			{
				List<EditorManager.MetadataWrapper> result = new List<EditorManager.MetadataWrapper>();
				result = (from x in EditorManager.inst.loadedLevels
						  orderby x.metadata.song.difficulty descending
						  select x).ToList<EditorManager.MetadataWrapper>();

				EditorManager.inst.loadedLevels = result;
			}
			if (levelFilter == 5 && levelAscend == true)
			{
				List<EditorManager.MetadataWrapper> result = new List<EditorManager.MetadataWrapper>();
				result = (from x in EditorManager.inst.loadedLevels
						  orderby x.metadata.song.difficulty ascending
						  select x).ToList<EditorManager.MetadataWrapper>();

				EditorManager.inst.loadedLevels = result;
			}

			//Date Edited
			if (levelFilter == 6 && levelAscend == false)
			{
				List<EditorManager.MetadataWrapper> result = new List<EditorManager.MetadataWrapper>();
				result = (from x in EditorManager.inst.loadedLevels
						  orderby x.metadata.beatmap.date_edited descending
						  select x).ToList();

				EditorManager.inst.loadedLevels = result;
			}
			if (levelFilter == 6 && levelAscend == true)
			{
				List<EditorManager.MetadataWrapper> result = new List<EditorManager.MetadataWrapper>();
				result = (from x in EditorManager.inst.loadedLevels
						  orderby x.metadata.beatmap.date_edited ascending
						  select x).ToList<EditorManager.MetadataWrapper>();

				EditorManager.inst.loadedLevels = result;
			}

			Transform transform = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("mask").Find("content");
			foreach (object obj in transform)
			{
				Destroy(((Transform)obj).gameObject);
			}
			foreach (EditorManager.MetadataWrapper metadataWrapper in EditorManager.inst.loadedLevels)
			{
				DataManager.MetaData metadata = metadataWrapper.metadata;
				string name = metadataWrapper.folder;

				string difficultyName = "None";
				if (metadata.song.difficulty == 0)
				{
					difficultyName = "easy";
				}
				if (metadata.song.difficulty == 1)
				{
					difficultyName = "normal";
				}
				if (metadata.song.difficulty == 2)
				{
					difficultyName = "hard";
				}
				if (metadata.song.difficulty == 3)
				{
					difficultyName = "expert";
				}
				if (metadata.song.difficulty == 4)
				{
					difficultyName = "expert+";
				}
				if (metadata.song.difficulty == 5)
				{
					difficultyName = "master";
				}
				if (metadata.song.difficulty == 6)
				{
					difficultyName = "animation";
				}

				if (EditorManager.inst.openFileSearch == null || !(EditorManager.inst.openFileSearch != "") || name.ToLower().Contains(EditorManager.inst.openFileSearch.ToLower()) || metadata.song.title.ToLower().Contains(EditorManager.inst.openFileSearch.ToLower()) || metadata.artist.Name.ToLower().Contains(EditorManager.inst.openFileSearch.ToLower()) || metadata.creator.steam_name.ToLower().Contains(EditorManager.inst.openFileSearch.ToLower()) || metadata.song.description.ToLower().Contains(EditorManager.inst.openFileSearch.ToLower()) || difficultyName.Contains(EditorManager.inst.openFileSearch.ToLower()))
				{
					GameObject gameObject = Instantiate(EditorManager.inst.folderButtonPrefab);
					gameObject.name = "Folder [" + metadataWrapper.folder + "]";
					gameObject.transform.SetParent(transform);
					gameObject.transform.localScale = Vector3.one;
					var hoverUI = gameObject.AddComponent<HoverUI>();
					hoverUI.ogPos = gameObject.transform.localPosition;
					hoverUI.animPos = new Vector3(-0.5f, 0f);
					hoverUI.size = HoverUIOFPSize.Value;
					hoverUI.animatePos = true;
					hoverUI.animateSca = true;
					HoverTooltip htt = gameObject.AddComponent<HoverTooltip>();

					HoverTooltip.Tooltip levelTip = new HoverTooltip.Tooltip();

					if (metadata != null)
					{
						gameObject.transform.GetChild(0).GetComponent<Text>().text = string.Format(FButtonFormat.Value, LSText.ClampString(metadataWrapper.folder, foldClamp), LSText.ClampString(metadata.song.title, songClamp), LSText.ClampString(metadata.artist.Name, artiClamp), LSText.ClampString(metadata.creator.steam_name, creaClamp), metadata.song.difficulty, LSText.ClampString(metadata.song.description, descClamp), LSText.ClampString(metadata.beatmap.date_edited, dateClamp));

						if (metadata.song.difficulty == 4 && FButtonTextInvert.Value == true && FButtonDifColor.Value == true || metadata.song.difficulty == 5 && FButtonTextInvert.Value == true && FButtonDifColor.Value == true)
						{
							gameObject.transform.GetChild(0).GetComponent<Text>().color = LSColors.ChangeColorBrightness(FButtonTextColor.Value, 0.7f);
						}

						Color difficultyColor = Color.white;

						for (int i = 0; i < 6; i++)
						{
							if (metadata.song.difficulty == i)
							{
								difficultyColor = DataManager.inst.difficulties[i].color;
							}
							if (FButtonDifColor.Value == true)
							{
								gameObject.GetComponent<Image>().color = difficultyColor * FButtonDifColorMult.Value;
							}
						}
						levelTip.desc = "<#" + LSColors.ColorToHex(difficultyColor) + ">" + metadata.artist.Name + " - " + metadata.song.title;
						levelTip.hint = "</color>" + metadata.song.description;
						htt.tooltipLangauges.Add(levelTip);
					}
					else
					{
						gameObject.transform.GetChild(0).GetComponent<Text>().text = string.Format("/{0} : {1}", LSText.ClampString(metadataWrapper.folder, foldClamp), LSText.ClampString("No MetaData File", songClamp));
					}
					gameObject.GetComponent<Button>().onClick.AddListener(delegate ()
					{
						EditorManager.inst.StartCoroutine(EditorManager.inst.LoadLevel(name));
						EditorManager.inst.HideDialog("Open File Popup");
					});

					GameObject icon = new GameObject("icon");
					icon.transform.SetParent(gameObject.transform);
					icon.transform.localScale = Vector3.one;
					icon.layer = 5;
					RectTransform iconRT = icon.AddComponent<RectTransform>();
					icon.AddComponent<CanvasRenderer>();
					Image iconImage = icon.AddComponent<Image>();

					iconRT.anchoredPosition = FBIconPos.Value;
					iconRT.sizeDelta = FBIconSca.Value;

					iconImage.sprite = metadataWrapper.albumArt;
				}
			}
		}

		public static void LegacyWaveform()
        {
			int num = Mathf.Clamp((int)AudioManager.inst.CurrentAudioSource.clip.length * 48, 100, 15000);
			Sprite waveform = Sprite.Create(GetWaveformTextureAdvanced(AudioManager.inst.CurrentAudioSource.clip, num, 300, TimelineBGColor.Value, TimelineTopColor.Value, TimelineBottomColor.Value), new Rect(0f, 0f, (float)num, 300f), new Vector2(0.5f, 0.5f), 100f);
			EditorManager.inst.timeline.GetComponent<Image>().sprite = waveform;
			EditorManager.inst.timelineWaveformOverlay.GetComponent<Image>().sprite = EditorManager.inst.timeline.GetComponent<Image>().sprite;
		}

		public static void OldWaveform()
		{
			int num = Mathf.Clamp((int)AudioManager.inst.CurrentAudioSource.clip.length * 48, 100, 15000);
			Sprite waveform = Sprite.Create(LSImage.GetWaveformTexture(AudioManager.inst.CurrentAudioSource.clip, num, 300, TimelineBGColor.Value, TimelineTopColor.Value), new Rect(0f, 0f, (float)num, 300f), new Vector2(0.5f, 0.5f), 100f);
			EditorManager.inst.timeline.GetComponent<Image>().sprite = waveform;
			EditorManager.inst.timelineWaveformOverlay.GetComponent<Image>().sprite = EditorManager.inst.timeline.GetComponent<Image>().sprite;
		}

		public static void NewTexture(string _path)
        {
			int num = Mathf.Clamp((int)AudioManager.inst.CurrentAudioSource.clip.length * 48, 100, 15000);

			var texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);
			var bytes = File.ReadAllBytes(_path);
			texture.LoadImage(bytes);

			texture.wrapMode = TextureWrapMode.Clamp;
			texture.filterMode = FilterMode.Point;
			texture.Apply();

			EditorManager.inst.timeline.GetComponent<Image>().sprite = Sprite.Create(texture, new Rect(0f, 0f, (float)num, 300f), new Vector2(0.5f, 0.5f), 100f);
			EditorManager.inst.timelineWaveformOverlay.GetComponent<Image>().sprite = EditorManager.inst.timeline.GetComponent<Image>().sprite;
		}

		public static void AssignTimelineTexture()
		{
			if (EditorManager.inst.hasLoadedLevel)
            {
				if (WaveformMode.Value == WaveformType.Legacy)
				{
					LegacyWaveform();
				}
				if (WaveformMode.Value == WaveformType.Old)
				{
					OldWaveform();
				}
			}
		}

		public static Texture2D GetWaveformTextureAdvanced(AudioClip clip, int textureWidth, int textureHeight, Color background, Color _top, Color _bottom)
		{
			int num = 160;
			num = clip.frequency / num;
			Texture2D texture2D = new Texture2D(textureWidth, textureHeight, TextureFormat.ARGB32, false);
			Color[] array = new Color[texture2D.width * texture2D.height];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = background;
			}
			texture2D.SetPixels(array);
			float[] array3;
			float[] array4;
			if (clip.channels > 1)
			{
				float[] array2 = new float[clip.samples * clip.channels];
				array3 = new float[clip.samples];
				array4 = new float[clip.samples];
				clip.GetData(array2, 0);
				array3 = array2.Where((float value, int index) => index % 2 != 0).ToArray<float>();
				array4 = array2.Where((float value, int index) => index % 2 == 0).ToArray<float>();
			}
			else
			{
				float[] array5 = new float[clip.samples * clip.channels];
				array3 = new float[clip.samples];
				array4 = new float[clip.samples];
				clip.GetData(array5, 0);
				array3 = array5;
				array4 = array5;
			}
			float[] array6 = new float[array3.Length / num];
			for (int j = 0; j < array6.Length; j++)
			{
				array6[j] = 0f;
				for (int k = 0; k < num; k++)
				{
					array6[j] += Mathf.Abs(array3[j * num + k]);
				}
				array6[j] /= (float)num;
				array6[j] *= 0.85f;
			}
			for (int l = 0; l < array6.Length - 1; l++)
			{
				int num2 = 0;
				while ((float)num2 < (float)textureHeight * array6[l])
				{
					texture2D.SetPixel(textureWidth * l / array6.Length, (int)((float)textureHeight * array6[l]) - num2, _top);
					num2++;
				}
			}
			array6 = new float[array4.Length / num];
			for (int m = 0; m < array6.Length; m++)
			{
				array6[m] = 0f;
				for (int n = 0; n < num; n++)
				{
					array6[m] += Mathf.Abs(array4[m * num + n]);
				}
				array6[m] /= (float)num;
				array6[m] *= 0.85f;
			}
			for (int num3 = 0; num3 < array6.Length - 1; num3++)
			{
				int num4 = 0;
				while ((float)num4 < (float)textureHeight * array6[num3])
				{
					int x = textureWidth * num3 / array6.Length;
					int y = (int)array4[num3 * num + num4] - num4;
					if (texture2D.GetPixel(x, y) == _top)
					{
						texture2D.SetPixel(x, y, _top + _bottom);
					}
					else
					{
						texture2D.SetPixel(x, y, _bottom);
					}
					num4++;
				}
			}
			texture2D.wrapMode = TextureWrapMode.Clamp;
			texture2D.filterMode = FilterMode.Point;
			texture2D.Apply();
			return texture2D;
		}

		[HarmonyPatch(typeof(DataManager), "Start")]
		[HarmonyPostfix]
		private static void DataLists()
		{
			if (DataManager.inst.difficulties.Count != 7)
			{
				DataManager.inst.difficulties = new List<DataManager.Difficulty>
				{
					new DataManager.Difficulty("Easy", LSColors.GetThemeColor("easy")),
					new DataManager.Difficulty("Normal", LSColors.GetThemeColor("normal")),
					new DataManager.Difficulty("Hard", LSColors.GetThemeColor("hard")),
					new DataManager.Difficulty("Expert", LSColors.GetThemeColor("expert")),
					new DataManager.Difficulty("Expert+", LSColors.GetThemeColor("expert+")),
					new DataManager.Difficulty("Master", new Color(0.25f, 0.01f, 0.01f)),
					new DataManager.Difficulty("Animation", LSColors.GetThemeColor("none"))
				};
			}

			if (DataManager.inst.linkTypes[3].name != "YouTube")
			{
				DataManager.inst.linkTypes = new List<DataManager.LinkType>
				{
					new DataManager.LinkType("Spotify", "https://open.spotify.com/artist/{0}"),
					new DataManager.LinkType("SoundCloud", "https://soundcloud.com/{0}"),
					new DataManager.LinkType("Bandcamp", "https://{0}.bandcamp.com"),
					new DataManager.LinkType("Youtube", "https://www.youtube.com/user/{0}"),
					new DataManager.LinkType("Newgrounds", "https://{0}.newgrounds.com/")
				};
			}

			if (DataManager.inst.AnimationList[1].Animation.keys[1].m_Time != 0.9999f)
			{
				DataManager.inst.AnimationList[1].Animation.keys[1].m_Time = 0.9999f;
				DataManager.inst.AnimationList[1].Animation.keys[1].m_Value = 0f;
			}
		}

		//[HarmonyPatch(typeof(DataManager), "SaveData", typeof(string), typeof(DataManager.GameData))]
		//[HarmonyPrefix]
		private static bool DataSaver(DataManager __instance, IEnumerator __result, string __0, DataManager.GameData __1)
        {
			RTEditor.inst.StartCoroutine(RTEditor.SaveData(__0, __1));
			return false;
        }

		[HarmonyPatch(typeof(DataManager), "SaveMetadata", typeof(string), typeof(DataManager.MetaData))]
		[HarmonyPrefix]
		private static bool SaveMetadataPrefix(LSError __result, string __0, DataManager.MetaData __1)
		{
			string rawProfileJSON = "{}";
			if (RTFile.FileExists(__0))
			{
				rawProfileJSON = FileManager.inst.LoadJSONFile(__0);
			}
			
			JSONNode jn = JSON.Parse(rawProfileJSON);
			Debug.Log("Saving Metadata to " + __0);
			Debug.Log("A-N: " + __1.artist.Name);
			Debug.Log("A-L: " + __1.artist.Link);
			Debug.Log("A-LT: " + __1.artist.LinkType.ToString());
			Debug.Log("C-SN: " + __1.creator.steam_name);
			Debug.Log("C-SID: " + __1.creator.steam_id.ToString());
			Debug.Log("S-T: " + __1.song.title);
			Debug.Log("S-D: " + __1.song.difficulty.ToString());
			Debug.Log("S-DE: " + __1.song.description);
			Debug.Log("S-BPM: " + __1.song.BPM.ToString());
			Debug.Log("S-T: " + __1.song.time.ToString());
			Debug.Log("S-PS: " + __1.song.previewStart.ToString());
			Debug.Log("S-PL: " + __1.song.previewLength.ToString());

			jn["artist"]["name"] = __1.artist.Name;
			jn["artist"]["link"] = __1.artist.Link;
			jn["artist"]["linkType"] = __1.artist.LinkType.ToString();
			jn["creator"]["steam_name"] = __1.creator.steam_name;
			jn["creator"]["steam_id"] = __1.creator.steam_id.ToString();
			jn["song"]["title"] = __1.song.title;
			jn["song"]["difficulty"] = __1.song.difficulty.ToString();
			jn["song"]["description"] = __1.song.description;
			jn["song"]["bpm"] = __1.song.BPM.ToString();
			jn["song"]["t"] = __1.song.time.ToString();
			jn["song"]["preview_start"] = __1.song.previewStart.ToString();
			jn["song"]["preview_length"] = __1.song.previewLength.ToString();

			if (SavingUpdatesTime.Value == true)
            {
				if (string.IsNullOrEmpty(jn["beatmap"]["date_original"]))
				{
					Debug.Log(__1.beatmap.date_edited);
					jn["beatmap"]["date_original"] = __1.beatmap.date_edited;
				}

				Debug.Log(DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss"));
				jn["beatmap"]["date_edited"] = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");
			}
			else
			{
				jn["beatmap"]["date_edited"] = __1.beatmap.date_edited;
			}

			jn["beatmap"]["version_number"] = __1.beatmap.version_number.ToString();
			jn["beatmap"]["game_version"] = __1.beatmap.game_version;
			jn["beatmap"]["workshop_id"] = __1.beatmap.workshop_id.ToString();
			RTFile.WriteToFile(__0, jn.ToString());
			return false;
        }

		private static void EEEE()
        {
			JSONNode jn = JSON.Parse("{}");
			AccessTools.Method(typeof(JSONNode), "ToString", new Type[] { typeof(int) }).Invoke(jn, new object[] { 3 });
		}

		[HarmonyPatch(typeof(BackgroundEditor), "Awake")]
		[HarmonyPostfix]
		private static void BackgroundEditorAwakePostfix()
        {
			GameObject bgRight = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/BackgroundDialog/data/right");
			GameObject bgLeft = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/BackgroundDialog/data/left");

			var createTip = bgRight.transform.Find("create").GetComponent<HoverTooltip>();
			HoverTooltip.Tooltip createTooltip = new HoverTooltip.Tooltip();
			createTooltip.desc = "Create New Background Object";
			createTooltip.hint = "Press this to create a new background object.";
			createTip.tooltipLangauges.Add(createTooltip);

			GameObject destroyAll = Instantiate(bgRight.transform.Find("create").gameObject);
			destroyAll.transform.SetParent(bgRight.transform);
			destroyAll.transform.localScale = Vector3.one;
			destroyAll.transform.SetSiblingIndex(2);
			destroyAll.name = "destroy";

			destroyAll.GetComponent<Image>().color = new Color(1f, 0.131f, 0.231f, 1f);
			destroyAll.transform.GetChild(0).GetComponent<Text>().text = "Delete All Backgrounds";
			destroyAll.transform.GetChild(0).localScale = Vector3.one;

			destroyAll.GetComponent<Button>().onClick.RemoveAllListeners();
			destroyAll.GetComponent<Button>().onClick.AddListener(delegate ()
			{
				RTEditor.DeleteAllBackgrounds();
			});

			var destroyAllTip = destroyAll.GetComponent<HoverTooltip>();
			HoverTooltip.Tooltip destroyAllTooltip = new HoverTooltip.Tooltip();
			destroyAllTooltip.desc = "Destroy All Objects";
			destroyAllTooltip.hint = "Press this to destroy all background objects, EXCEPT the first one.";
			destroyAllTip.tooltipLangauges.Clear();
			destroyAllTip.tooltipLangauges.Add(destroyAllTooltip);

			GameObject createBGs = Instantiate(bgLeft.transform.Find("name").gameObject);
			createBGs.transform.SetParent(bgRight.transform);
			createBGs.transform.localScale = Vector3.one;
			createBGs.transform.SetSiblingIndex(2);
			createBGs.name = "create bgs";

			createBGs.transform.Find("name").GetComponent<InputField>().onValueChanged.m_Calls.m_ExecutingCalls.Clear();
			createBGs.transform.Find("name").GetComponent<InputField>().onValueChanged.m_Calls.m_PersistentCalls.Clear();
			createBGs.transform.Find("name").GetComponent<InputField>().onValueChanged.m_PersistentCalls.m_Calls.Clear();
			createBGs.transform.Find("name").GetComponent<InputField>().onValueChanged.RemoveAllListeners();

			Destroy(createBGs.transform.Find("active").gameObject);
			createBGs.transform.Find("name").localScale = Vector3.one;
			createBGs.transform.Find("name").GetComponent<InputField>().text = "12";
			createBGs.transform.Find("name").GetComponent<InputField>().characterValidation = InputField.CharacterValidation.Integer;
			createBGs.transform.Find("name").GetComponent<RectTransform>().sizeDelta = new Vector2(80f, 34f);

			GameObject createAll = Instantiate(bgRight.transform.Find("create").gameObject);
			createAll.transform.SetParent(createBGs.transform);
			createAll.transform.localScale = Vector3.one;
			createAll.name = "create";

			createAll.GetComponent<Image>().color = new Color(0.6252f, 0.2789f, 0.6649f, 1f);
			createAll.GetComponent<RectTransform>().sizeDelta = new Vector2(278f, 34f);
			createAll.transform.GetChild(0).GetComponent<Text>().text = "Create Backgrounds";
			createAll.transform.GetChild(0).localScale = Vector3.one;

			createAll.GetComponent<Button>().onClick.RemoveAllListeners();
			createAll.GetComponent<Button>().onClick.AddListener(delegate ()
			{
				RTEditor.CreateBackgrounds(int.Parse(createBGs.transform.GetChild(0).GetComponent<InputField>().text));
			});

			bgRight.transform.Find("backgrounds").GetComponent<RectTransform>().sizeDelta = new Vector2(366f, 524f);
		}

		public static void SetNewMarkerColors()
        {
			MarkerEditor.inst.markerColors = new List<Color>
			{
				new Color(0.8745f, 0.4745f, 0.4392f, 1f),
				new Color(1f, 0.502f, 0.6706f, 1f),
				new Color(0.9176f, 0.502f, 0.9882f, 1f),
				new Color(0.702f, 0.5333f, 1f, 1f),
				new Color(0.549f, 0.6196f, 1f, 1f),
				new Color(0.502f, 0.8471f, 1f, 1f),
				new Color(0.6549f, 1f, 0.9216f, 1f),
				new Color(0.9569f, 1f, 0.5059f, 1f),
				new Color(1f, 0.8196f, 0.502f, 1f),
				MarkerColN0.Value,
				MarkerColN1.Value,
				MarkerColN2.Value,
				MarkerColN3.Value,
				MarkerColN4.Value,
				MarkerColN5.Value,
				MarkerColN6.Value,
				MarkerColN7.Value,
				MarkerColN8.Value,
			};
		}

		[HarmonyPatch(typeof(Debug), "Log", new Type[] { typeof(object) })]
		[HarmonyPostfix]
		private static void LogNotifications(object __0)
        {
			if (EditorManager.inst != null && EditorDebug.Value == true)
            {
				string str = __0.ToString();
				str = str.Replace(@"\n", "<br>");
				RTEditor.DisplayNotification(__0.ToString(), str, 2f, EditorManager.NotificationType.Success);
            }
        }

		[HarmonyPatch(typeof(Debug), "LogError", new Type[] { typeof(object) })]
		[HarmonyPostfix]
		private static void LogErrorNotifications(object __0)
        {
			if (EditorManager.inst != null && EditorDebug.Value == true)
            {
				string str = __0.ToString();
				str = str.Replace(@"\n", "<br>");
				RTEditor.DisplayNotification(__0.ToString(), str, 2f, EditorManager.NotificationType.Error);
            }
        }

		[HarmonyPatch(typeof(Debug), "LogWarning", new Type[] { typeof(object) })]
		[HarmonyPostfix]
		private static void LogWarningNotifications(object __0)
        {
			if (EditorManager.inst != null && EditorDebug.Value == true)
            {
				string str = __0.ToString();
				str = str.Replace(@"\n", "<br>");
				RTEditor.DisplayNotification(__0.ToString(), str, 2f, EditorManager.NotificationType.Warning);
            }
        }
	}
}
