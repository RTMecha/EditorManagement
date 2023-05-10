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
using UnityEngine.EventSystems;

using SimpleJSON;
using DG.Tweening;
using DG.Tweening.Core;

using EditorManagement.Functions;
using EditorManagement.Functions.Tools;
using EditorManagement.Patchers;

namespace EditorManagement
{
	[BepInPlugin("com.mecha.editormanagement", "Editor Management", " 1.7.11")]
	[BepInProcess("Project Arrhythmia.exe")]
	[BepInIncompatibility("com.mecha.renderdepthunlimited")]
	[BepInIncompatibility("com.mecha.originoffset")]
	[BepInIncompatibility("com.mecha.cursorcolor")]
	[BepInIncompatibility("com.mecha.noautokillselectable")]
	[BepInIncompatibility("com.mecha.eventsplus")]
	[BepInIncompatibility("com.mecha.newthemesystems")]
	[BepInIncompatibility("com.mecha.prefabadditions")]
	public class EditorPlugin : BaseUnityPlugin
	{
		//TODO
		//Clean up some code, optimize and bug fix.
		//Fix the prefab search bug. (Kinda fixed?)
		//Create editor stuff for Object Modifiers.
		//Change Sync to First Selected Object in the Multi Object Editor to use the Search Objects function.

		//Fixed pos Z axis breaking prefabs. They now properly render.
		//I tried fixing the event drag selection issue where you can't drag select from the new event rows, but it hasn't been working at all for some reason.
		//Added a config option for the sound that plays when you hover over a UI element. Find it in Editor Preferences > Editor GUI > Hover UI Sound.

		public static string className = "[<color=#F6AC1A>Editor</color><color=#2FCBD6>Management</color>] " + PluginInfo.PLUGIN_VERSION + "\n";

		public static float scrollBar;
		public static float timeEdit;
		public static float itsTheTime;
		public static int openAmount;
		public static int levelFilter = 0;
		public static bool levelAscend = true;

		public static string editorPath = "editor";
		public static string levelListPath = "beatmaps/editor";
		public static string levelListSlash = "beatmaps/editor/";

		public static string themePath = "themes";
		public static string themeListPath = "beatmaps/themes";
		public static string themeListSlash = "beatmaps/themes/";

		public static string prefabPath = "prefabs";
		public static string prefabListPath = "beatmaps/prefabs";
		public static string prefabListSlash = "beatmaps/prefabs/";

		public static WaveformType waveformType;
		public static Direction direction;
		public static Easings easing;
		public static PrefabDialog prefabDialog;

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
		public enum PrefabDialog
		{
			Internal,
			External
		}

		public static List<int> allLayers = new List<int>();

		public static RTEditor editor;

		public static bool createInternal = true;

		private void Awake()
		{
			inst = this;

			Logger.LogInfo("Plugin Editor Management is loaded!");

			ConfigEntries.EditorPropertiesKey = Config.Bind("Editor Properties", "KeyCode", KeyCode.F10, "The key to press to open the Editor Properties / Preferences window.");


			ConfigEntries.EXPrefab = Config.Bind("Prefabs", "Example Prefab Template", true, "If enabled, an Example Template prefab will always be imported into the internal prefabs.");

			ConfigEntries.HoverSoundsEnabled = Config.Bind("Hover UI", "Play Sound", false, "If enabled, plays a sound when the hover UI element is hovered over.");

			//Event Modifiers
			{
				ConfigEntries.EventMoveModify = Config.Bind("Event Modifiers", "00 Move Amount", 1f, "Amount this particular event increases by when arrow clicked.");
				ConfigEntries.EventZoomModify = Config.Bind("Event Modifiers", "01 Zoom Amount", 1f, "Amount this particular event increases by when arrow clicked.");
				ConfigEntries.EventRotateModify = Config.Bind("Event Modifiers", "02 Rotate Amount", 5f, "Amount this particular event increases by when arrow clicked.");
				ConfigEntries.EventShakeModify = Config.Bind("Event Modifiers", "03 Shake Amount", 0.1f, "Amount this particular event increases by when arrow clicked.");
				ConfigEntries.EventChromaModify = Config.Bind("Event Modifiers", "04 Chroma Amount", 0.1f, "Amount this particular event increases by when arrow clicked.");
				ConfigEntries.EventBloomModify = Config.Bind("Event Modifiers", "05 Bloom Amount", 1f, "Amount this particular event increases by when arrow clicked.");
				ConfigEntries.EventVignetteIntensityModify = Config.Bind("Event Modifiers", "06 Vignette Intensity Amount", 1f, "Amount this particular event increases by when arrow clicked.");
				ConfigEntries.EventVignetteSmoothnessModify = Config.Bind("Event Modifiers", "07 Vignette Smoothness Amount", 1f, "Amount this particular event increases by when arrow clicked.");
				ConfigEntries.EventVignetteRoundnessModify = Config.Bind("Event Modifiers", "08 Vignette Roundness Amount", 0.01f, "Amount this particular event increases by when arrow clicked.");
				ConfigEntries.EventVignettePosModify = Config.Bind("Event Modifiers", "09 Vignette Amount", 0.1f, "Amount this particular event increases by when arrow clicked.");
				ConfigEntries.EventLensModify = Config.Bind("Event Modifiers", "0A Lens Intensity Amount", 10f, "Amount this particular event increases by when arrow clicked.");
				ConfigEntries.EventGrainIntensityModify = Config.Bind("Event Modifiers", "0A Grain Intensity Amount", 1f, "Amount this particular event increases by when arrow clicked.");
				ConfigEntries.EventGrainSizeModify = Config.Bind("Event Modifiers", "0B Grain Size Amount", 0.1f, "Amount this particular event increases by when arrow clicked.");

				ConfigEntries.ListHorizontal = Config.Bind("Theme List", "00 Horizontal Scroll", false, "If you can scroll left / right or not.");
				ConfigEntries.ListCellSize = Config.Bind("Theme List", "01 Cell Size", new Vector2(344f, 30f), "Cell size of each theme.");
				ConfigEntries.ListConstraint = Config.Bind("Theme List", "02 Constraint", GridLayoutGroup.Constraint.FixedColumnCount, "Which direction the theme list goes.");
				ConfigEntries.ListConstraintCount = Config.Bind("Theme List", "03 Constraint Count", 1, "Amount of collumns / rows the grid has (depending on the previous setting).");
				ConfigEntries.ListSpacing = Config.Bind("Theme List", "04 Spacing", new Vector2(4f, 4f), "Spacing in-between themes.");
				ConfigEntries.ListAxis = Config.Bind("Theme List", "05 Start Axis", GridLayoutGroup.Axis.Horizontal, "Start axis of the theme list.");
			}

			//Theme
			{
				ConfigEntries.TemplateThemeName = Config.Bind("Beatmap Theme Template", "Name", "New Theme", "Name of the template theme.");
				ConfigEntries.TemplateThemeGUIColor = Config.Bind("Beatmap Theme Template", "GUI", LSColors.white, "GUI Color of the template theme.");
				ConfigEntries.TemplateThemeBGColor = Config.Bind("Beatmap Theme Template", "BG", LSColors.gray900, "BG Color of the template theme.");
				ConfigEntries.TemplateThemePlayerColor1 = Config.Bind("Beatmap Theme Template", "Player 1", LSColors.HexToColor("E57373"), "Player 1 Color of the template theme.");
				ConfigEntries.TemplateThemePlayerColor2 = Config.Bind("Beatmap Theme Template", "Player 2", LSColors.HexToColor("64B5F6"), "Player 2 Color of the template theme.");
				ConfigEntries.TemplateThemePlayerColor3 = Config.Bind("Beatmap Theme Template", "Player 3", LSColors.HexToColor("81C784"), "Player 3 Color of the template theme.");
				ConfigEntries.TemplateThemePlayerColor4 = Config.Bind("Beatmap Theme Template", "Player 4", LSColors.HexToColor("FFB74D"), "Player 4 Color of the template theme.");
				ConfigEntries.TemplateThemeOBJColor1 = Config.Bind("Beatmap Theme Template", "OBJ 1", LSColors.gray100, "OBJ 1 Color of the template theme.");
				ConfigEntries.TemplateThemeOBJColor2 = Config.Bind("Beatmap Theme Template", "OBJ 2", LSColors.gray200, "OBJ 2 Color of the template theme.");
				ConfigEntries.TemplateThemeOBJColor3 = Config.Bind("Beatmap Theme Template", "OBJ 3", LSColors.gray300, "OBJ 3 Color of the template theme.");
				ConfigEntries.TemplateThemeOBJColor4 = Config.Bind("Beatmap Theme Template", "OBJ 4", LSColors.gray400, "OBJ 4 Color of the template theme.");
				ConfigEntries.TemplateThemeOBJColor5 = Config.Bind("Beatmap Theme Template", "OBJ 5", LSColors.gray500, "OBJ 5 Color of the template theme.");
				ConfigEntries.TemplateThemeOBJColor6 = Config.Bind("Beatmap Theme Template", "OBJ 6", LSColors.gray600, "OBJ 6 Color of the template theme.");
				ConfigEntries.TemplateThemeOBJColor7 = Config.Bind("Beatmap Theme Template", "OBJ 7", LSColors.gray700, "OBJ 7 Color of the template theme.");
				ConfigEntries.TemplateThemeOBJColor8 = Config.Bind("Beatmap Theme Template", "OBJ 8", LSColors.gray800, "OBJ 8 Color of the template theme.");
				ConfigEntries.TemplateThemeOBJColor9 = Config.Bind("Beatmap Theme Template", "OBJ 9", LSColors.gray900, "OBJ 9 Color of the template theme.");
				ConfigEntries.TemplateThemeBGColor1 = Config.Bind("Beatmap Theme Template", "BG 1", LSColors.pink100, "BG 1 Color of the template theme.");
				ConfigEntries.TemplateThemeBGColor2 = Config.Bind("Beatmap Theme Template", "BG 2", LSColors.pink200, "BG 2 Color of the template theme.");
				ConfigEntries.TemplateThemeBGColor3 = Config.Bind("Beatmap Theme Template", "BG 3", LSColors.pink300, "BG 3 Color of the template theme.");
				ConfigEntries.TemplateThemeBGColor4 = Config.Bind("Beatmap Theme Template", "BG 4", LSColors.pink400, "BG 4 Color of the template theme.");
				ConfigEntries.TemplateThemeBGColor5 = Config.Bind("Beatmap Theme Template", "BG 5", LSColors.pink500, "BG 5 Color of the template theme.");
				ConfigEntries.TemplateThemeBGColor6 = Config.Bind("Beatmap Theme Template", "BG 6", LSColors.pink600, "BG 6 Color of the template theme.");
				ConfigEntries.TemplateThemeBGColor7 = Config.Bind("Beatmap Theme Template", "BG 7", LSColors.pink700, "BG 7 Color of the template theme.");
				ConfigEntries.TemplateThemeBGColor8 = Config.Bind("Beatmap Theme Template", "BG 8", LSColors.pink800, "BG 8 Color of the template theme.");
				ConfigEntries.TemplateThemeBGColor9 = Config.Bind("Beatmap Theme Template", "BG 9", LSColors.pink900, "BG 9 Color of the template theme.");

				ConfigEntries.ThemeHWM = Config.Bind("Theme Buttons", "00 Text Horizontal Wrap Mode", HorizontalWrapMode.Wrap, "Horizontal wrap mode of the theme name.");
				ConfigEntries.ThemeVWM = Config.Bind("Theme Buttons", "01 Text Vertical Wrap Mode", VerticalWrapMode.Truncate, "Vertical wrap mode of the theme name.");
				ConfigEntries.ThemeFSize = Config.Bind("Theme Buttons", "02 Text Font Size", 20, "Font size of the theme name.");
				ConfigEntries.ThemeTColor = Config.Bind("Theme Buttons", "03 Text Color", new Color(0.1961f, 0.1961f, 0.1961f, 1f), "Color of the theme name.");
				ConfigEntries.ThemeBColor = Config.Bind("Theme Buttons", "04 Button Color", new Color(0.9294f, 0.9137f, 0.9294f, 1f), "Color of the theme button. Not the one that previews the theme, the one that's normally white.");
			}

            //Animate GUI
            {
				//Animate GUI (Editor Properties Popup)
				ConfigEntries.EPPAnimateX = Config.Bind("Animate GUI", "Editor Properties Popup Animate X", true, "If the X scale should animate or not.");
				ConfigEntries.EPPAnimateY = Config.Bind("Animate GUI", "Editor Properties Popup Animate Y", true, "If the Y scale should animate or not.");
				ConfigEntries.EPPAnimateInOutSpeeds = Config.Bind("Animate GUI", "Editor Properties Popup Speeds (Open | Close)", new Vector2(0.2f, 0.2f), "How fast the animation should play. First is open speed, second is close speed.");
				ConfigEntries.EPPAnimateEaseIn = Config.Bind("Animate GUI", "Editor Properties Popup Easing Open", Easings.Linear, "What type of easing the animation should use.");
				ConfigEntries.EPPAnimateEaseOut = Config.Bind("Animate GUI", "Editor Properties Popup Easing Close", Easings.Linear, "What type of easing the animation should use.");

				//Animate GUI (Open File Popup)
				ConfigEntries.OFPAnimateX = Config.Bind("Animate GUI", "Open File Popup Animate X", true, "If the X scale should animate or not.");
				ConfigEntries.OFPAnimateY = Config.Bind("Animate GUI", "Open File Popup Animate Y", true, "If the Y scale should animate or not.");
				ConfigEntries.OFPAnimateInOutSpeeds = Config.Bind("Animate GUI", "Open File Popup Speeds (Open | Close)", new Vector2(0.2f, 0.2f), "How fast the animation should play. First is open speed, second is close speed.");
				ConfigEntries.OFPAnimateEaseIn = Config.Bind("Animate GUI", "Open File Popup Easing Open", Easings.Linear, "What type of easing the animation should use.");
				ConfigEntries.OFPAnimateEaseOut = Config.Bind("Animate GUI", "Open File Popup Easing Close", Easings.Linear, "What type of easing the animation should use.");

				ConfigEntries.NFPAnimateX = Config.Bind("Animate GUI", "New File Popup Animate X", true, "If the X scale should animate or not.");
				ConfigEntries.NFPAnimateY = Config.Bind("Animate GUI", "New File Popup Animate Y", true, "If the Y scale should animate or not.");
				ConfigEntries.NFPAnimateInOutSpeeds = Config.Bind("Animate GUI", "New File Popup Speeds (Open | Close)", new Vector2(0.2f, 0.2f), "How fast the animation should play. First is open speed, second is close speed.");
				ConfigEntries.NFPAnimateEaseIn = Config.Bind("Animate GUI", "New File Popup Easing Open", Easings.Linear, "What type of easing the animation should use.");
				ConfigEntries.NFPAnimateEaseOut = Config.Bind("Animate GUI", "New File Popup Easing Close", Easings.Linear, "What type of easing the animation should use.");

				ConfigEntries.PPAnimateX = Config.Bind("Animate GUI", "Prefab Popup Animate X", true, "If the X scale should animate or not.");
				ConfigEntries.PPAnimateY = Config.Bind("Animate GUI", "Prefab Popup Animate Y", true, "If the Y scale should animate or not.");
				ConfigEntries.PPAnimateInOutSpeeds = Config.Bind("Animate GUI", "Prefab Popup Speeds (Open | Close)", new Vector2(0.2f, 0.2f), "How fast the animation should play. First is open speed, second is close speed.");
				ConfigEntries.PPAnimateEaseIn = Config.Bind("Animate GUI", "Prefab Popup Easing Open", Easings.Linear, "What type of easing the animation should use.");
				ConfigEntries.PPAnimateEaseOut = Config.Bind("Animate GUI", "Prefab Popup Easing Close", Easings.Linear, "What type of easing the animation should use.");

				ConfigEntries.QAPAnimateX = Config.Bind("Animate GUI", "Object Tags Popup Animate X", true, "If the X scale should animate or not.");
				ConfigEntries.QAPAnimateY = Config.Bind("Animate GUI", "Object Tags Popup Animate Y", true, "If the Y scale should animate or not.");
				ConfigEntries.QAPAnimateInOutSpeeds = Config.Bind("Animate GUI", "Object Tags Popup Speeds (Open | Close)", new Vector2(0.2f, 0.2f), "How fast the animation should play. First is open speed, second is close speed.");
				ConfigEntries.QAPAnimateEaseIn = Config.Bind("Animate GUI", "Object Tags Popup Easing Open", Easings.Linear, "What type of easing the animation should use.");
				ConfigEntries.QAPAnimateEaseOut = Config.Bind("Animate GUI", "Object Tags Popup Easing Close", Easings.Linear, "What type of easing the animation should use.");

				ConfigEntries.OBJPAnimateX = Config.Bind("Animate GUI", "Create Object Popup Animate X", true, "If the X scale should animate or not.");
				ConfigEntries.OBJPAnimateY = Config.Bind("Animate GUI", "Create Object Popup Animate Y", true, "If the Y scale should animate or not.");
				ConfigEntries.OBJPAnimateInOutSpeeds = Config.Bind("Animate GUI", "Create Object Popup Speeds (Open | Close)", new Vector2(0.2f, 0.2f), "How fast the animation should play. First is open speed, second is close speed.");
				ConfigEntries.OBJPAnimateEaseIn = Config.Bind("Animate GUI", "Create Object Popup Easing Open", Easings.Linear, "What type of easing the animation should use.");
				ConfigEntries.OBJPAnimateEaseOut = Config.Bind("Animate GUI", "Create Object Popup Easing Close", Easings.Linear, "What type of easing the animation should use.");

				ConfigEntries.BGPAnimateX = Config.Bind("Animate GUI", "Create BG Popup Animate X", true, "If the X scale should animate or not.");
				ConfigEntries.BGPAnimateY = Config.Bind("Animate GUI", "Create BG Popup Animate Y", true, "If the Y scale should animate or not.");
				ConfigEntries.BGPAnimateInOutSpeeds = Config.Bind("Animate GUI", "Create BG Popup Speeds (Open | Close)", new Vector2(0.2f, 0.2f), "How fast the animation should play. First is open speed, second is close speed.");
				ConfigEntries.BGPAnimateEaseIn = Config.Bind("Animate GUI", "Create BG Popup Easing Open", Easings.Linear, "What type of easing the animation should use.");
				ConfigEntries.BGPAnimateEaseOut = Config.Bind("Animate GUI", "Create BG Popup Easing Close", Easings.Linear, "What type of easing the animation should use.");

				ConfigEntries.GODAnimateX = Config.Bind("Animate GUI", "Object Editor Animate X", true, "If the X scale should animate or not.");
				ConfigEntries.GODAnimateY = Config.Bind("Animate GUI", "Object Editor Animate Y", true, "If the Y scale should animate or not.");
				ConfigEntries.GODAnimateInOutSpeeds = Config.Bind("Animate GUI", "Object Editor Speeds (Open | Close)", new Vector2(0.2f, 0.2f), "How fast the animation should play. First is open speed, second is close speed.");
				ConfigEntries.GODAnimateEaseIn = Config.Bind("Animate GUI", "Object Editor Easing Open", Easings.Linear, "What type of easing the animation should use.");
				ConfigEntries.GODAnimateEaseOut = Config.Bind("Animate GUI", "Object Editor Easing Close", Easings.Linear, "What type of easing the animation should use.");
			}

			ConfigEntries.HoverUIOFPSize = Config.Bind("Hover UI", "Open File Popup Size", 1.1f, "How big the button gets when hovered.");
			ConfigEntries.HoverUIETLSize = Config.Bind("Hover UI", "Timeline Object Size", 1.1f, "How big the button gets when hovered.");
			ConfigEntries.HoverUIKFSize = Config.Bind("Hover UI", "Object Keyframe Size", 1.1f, "How big the button gets when hovered.");

			//AutoSave Config
			ConfigEntries.AutoSaveRepeat = Config.Bind("AutoSave", "Repeat", 600f, "The repeat time of autosave.");
			ConfigEntries.AutoSaveLimit = Config.Bind("AutoSave", "Limit", 3, "If autosave count reaches this number, delete the first autosave.");
			ConfigEntries.SavingUpdatesTime = Config.Bind("Saving", "Update Date Edited to Recent", false, "Enabling this will save date_edited in metadata.lsb as the most recent.");

			//General Editor stuff
			{
				ConfigEntries.IfEditorStartTime = Config.Bind("General Editor", "Load Saved Time", true, "If enabled, sets the audio time to the last saved timeline position on level load.");
				ConfigEntries.IfEditorPauses = Config.Bind("General Editor", "Editor Pauses", false, "If enabled, the editor pauses on level load.");
				ConfigEntries.IfEditorSlowLoads = Config.Bind("General Editor", "One by one load", false, "If enabled, the editor will load each object individually rather than all at once.");
				ConfigEntries.EditorDebug = Config.Bind("General Editor", "Debug", false, "If enabled, specific debugging functions for the editor will be enabled.");
				ConfigEntries.DragUI = Config.Bind("General Editor", "Drag UI", false, "if enabled, specific UI popups can be dragged around.");
				ConfigEntries.NotificationWidth = Config.Bind("Editor Notifications", "Notification Width", 221f, "Width of the notification popups.");
				ConfigEntries.NotificationSize = Config.Bind("Editor Notifications", "Notification Size", 1f, "Size of the notification popups.");
				ConfigEntries.NotificationDirection = Config.Bind("Editor Notifications", "Notification Direction", Direction.Down, "Size of the notification popups.");
			}

			//New Markers Config
			{
				ConfigEntries.MarkerColN0 = Config.Bind("Markers", "Color 0", Color.white, "Color 0 of the second set of marker colors.");
				ConfigEntries.MarkerColN1 = Config.Bind("Markers", "Color 1", Color.white, "Color 1 of the second set of marker colors.");
				ConfigEntries.MarkerColN2 = Config.Bind("Markers", "Color 2", Color.white, "Color 2 of the second set of marker colors.");
				ConfigEntries.MarkerColN3 = Config.Bind("Markers", "Color 3", Color.white, "Color 3 of the second set of marker colors.");
				ConfigEntries.MarkerColN4 = Config.Bind("Markers", "Color 4", Color.white, "Color 4 of the second set of marker colors.");
				ConfigEntries.MarkerColN5 = Config.Bind("Markers", "Color 5", Color.white, "Color 5 of the second set of marker colors.");
				ConfigEntries.MarkerColN6 = Config.Bind("Markers", "Color 6", Color.white, "Color 6 of the second set of marker colors.");
				ConfigEntries.MarkerColN7 = Config.Bind("Markers", "Color 7", Color.white, "Color 7 of the second set of marker colors.");
				ConfigEntries.MarkerColN8 = Config.Bind("Markers", "Color 8", Color.white, "Color 8 of the second set of marker colors.");

				ConfigEntries.MarkerLoop = Config.Bind("Markers", "Marker Loop Active", false, "If the marker should loop between markers.");
				ConfigEntries.MarkerStartIndex = Config.Bind("Markers", "Marker Loop Begin", 0, "Audio time gets set to this marker.");
				ConfigEntries.MarkerEndIndex = Config.Bind("Markers", "Marker Loop End", 1, "If the audio time gets to the set marker time, it will loop to the beginning marker.");
			}

			//Open File Popup Configs
			{
				ConfigEntries.ORLAnchoredPos = Config.Bind("Open File Popup Base", "00 Position", Vector2.zero, "The position of the open file popup.");
				ConfigEntries.ORLSizeDelta = Config.Bind("Open File Popup Base", "01 Scale", new Vector2(600f, 400f), "The size of the open file popup.");
				ConfigEntries.ORLPathPos = Config.Bind("Open File Popup Base", "02 Editor Path Pos", new Vector2(125f, 16f), "The position of the editor path input field.");
				ConfigEntries.ORLPathLength = Config.Bind("Open File Popup Base", "03 Editor Path Length", 254f, "The length of the editor path input field.");
				ConfigEntries.ORLRefreshPos = Config.Bind("Open File Popup Base", "04 List Refresh Pos", new Vector2(260f, 432f), "The position of the refresh button.");
				ConfigEntries.ORLTogglePos = Config.Bind("Open File Popup Base", "05 Toggle Pos", new Vector2(600f, 16f), "The position of the descending toggle.");
				ConfigEntries.ORLDropdownPos = Config.Bind("Open File Popup Base", "06 Dropdown Pos", new Vector2(501f, 416f), "The position of the sort dropdown.");

				ConfigEntries.OGLVLCellSize = Config.Bind("Open File Popup Cells", "00 Cell Size", new Vector2(584f, 32f), "Size of each cell.");
				ConfigEntries.OGLVLConstraint = Config.Bind("Open File Popup Cells", "01 Constraint Type", Constraint.FixedColumnCount, "How the cells are layed out.");
				ConfigEntries.OGLVLConstraintCount = Config.Bind("Open File Popup Cells", "02 Constraint Count", 1, "How many rows / columns there are, depending on Constraint Type.");
				ConfigEntries.OGLVLSpacing = Config.Bind("Open File Popup Cells", "03 Spacing", new Vector2(0f, 8f), "The space between each cell.");

				//Folder Button Configs
				ConfigEntries.FButtonHWrap = Config.Bind("Open File Popup Buttons", "00 Horizontal Wrap", HorizontalWrapMode.Wrap, "Horizontal Wrap Mode of the folder button text.");
				ConfigEntries.FButtonVWrap = Config.Bind("Open File Popup Buttons", "01 Vertical Wrap", VerticalWrapMode.Truncate, "Vertical Wrap Mode of the folder button text.");
				ConfigEntries.FButtonTextColor = Config.Bind("Open File Popup Buttons", "02 Text Color", new Color(0.9373f, 0.9216f, 0.9373f, 1f), "Color of the folder button text.");
				ConfigEntries.FButtonTextInvert = Config.Bind("Open File Popup Buttons", "03 Text Invert", true, "If the text should invert if the difficulty color is dark.");
				ConfigEntries.FButtonFontSize = Config.Bind("Open File Popup Buttons", "04 Text Font Size", 20, "Font size of the folder button text.");

				ConfigEntries.FButtonFoldClamp = Config.Bind("Open File Popup Buttons", "05 Folder Name Clamp", 14, "Limited length of the folder name.");
				ConfigEntries.FButtonSongClamp = Config.Bind("Open File Popup Buttons", "06 Song Name Clamp", 22, "Limited length of the song name.");
				ConfigEntries.FButtonArtiClamp = Config.Bind("Open File Popup Buttons", "07 Artist Name Clamp", 16, "Limited length of the artist name.");
				ConfigEntries.FButtonCreaClamp = Config.Bind("Open File Popup Buttons", "08 Creator Name Clamp", 16, "Limited length of the creator name.");
				ConfigEntries.FButtonDescClamp = Config.Bind("Open File Popup Buttons", "09 Description Clamp", 16, "Limited length of the description.");
				ConfigEntries.FButtonDateClamp = Config.Bind("Open File Popup Buttons", "0A Date Clamp", 16, "Limited length of the date.");
				ConfigEntries.FButtonFormat = Config.Bind("Open File Popup Buttons", "0B Formatting", ".  /{0} : {1} by {2}", "The way the text is formatted for each level. {0} is folder, {1} is song, {2} is artist, {3} is creator, {4} is difficulty, {5} is description and {6} is last edited.");

				ConfigEntries.FButtonDifColor = Config.Bind("Open File Popup Buttons", "10 Difficulty Color", false, "If each button matches its associated difficulty color.");
				ConfigEntries.FButtonDifColorMult = Config.Bind("Open File Popup Buttons", "11 Difficulty Multiply", 1.5f, "How much each buttons' color multiplies by difficulty color.");

				ConfigEntries.FButtonNColor = Config.Bind("Open File Popup Buttons", "12 Normal Color", new Color(0.1647f, 0.1647f, 0.1647f, 1f), "Normal color of the folder button.");
				ConfigEntries.FButtonHColor = Config.Bind("Open File Popup Buttons", "13 Highlighted Color", new Color(0.2588f, 0.2588f, 0.2588f, 1f), "Highlighted color of the folder button.");
				ConfigEntries.FButtonPColor = Config.Bind("Open File Popup Buttons", "14 Pressed Color", new Color(0.2588f, 0.2588f, 0.2588f, 1f), "Pressed color of the folder button.");
				ConfigEntries.FButtonSColor = Config.Bind("Open File Popup Buttons", "15 Selected Color", new Color(0.2588f, 0.2588f, 0.2588f, 1f), "Selected color of the folder button.");
				ConfigEntries.FButtonFadeDColor = Config.Bind("Open File Popup Buttons", "16 Fade Duration", 0.2f, "Fade duration of the folder button.");

				//Cover Art Configs
				ConfigEntries.FBIconPos = Config.Bind("Open File Popup Buttons", "17 Cover Position", new Vector2(-276f, 0f), "Position of the level cover.");
				ConfigEntries.FBIconSca = Config.Bind("Open File Popup Buttons", "18 Cover Size", new Vector2(26f, 26f), "Size of the level cover.");

				ConfigEntries.IfReloadLList = Config.Bind("Open File Popup Buttons", "Changes Refresh List (Read desc)", false, "If the level list reloads whenever a change is made. DO NOT SET AS ENABLED IF YOU HAVE LOADS OF LEVELS!!");
			}


			//Timeline
			{
				ConfigEntries.TimeModify = Config.Bind("Timeline Bar", "Time Scroll Input", 0.1f, "The amount the time input increases when you scroll on it.");

				//Zoom Cap Config
				ConfigEntries.ObjZoomBounds = Config.Bind("Zoom Bounds", "Object timeline", new Vector2(1f, 512f), "The cap of the object timeline zoom.");
				ConfigEntries.ETLZoomBounds = Config.Bind("Zoom Bounds", "Editor timeline", new Vector2(16f, 512f), "The cap of the editor timeline zoom.");
				ConfigEntries.ZoomAmount = Config.Bind("Zoom Bounds", "Zoom Amount", 0.05f, "How much the timeline should zoom.");
				lastEdtBounds = ConfigEntries.ETLZoomBounds.Value;

				ConfigEntries.RenderTimeline = Config.Bind("Timeline", "00 Re-render Timeline", false, "If the timeline waveform should update when value is changed.");
				ConfigEntries.TimelineBGColor = Config.Bind("Timeline", "01 BG Color", Color.clear, "Color of the background of the timeline. (Only for Legacy waveform type)");
				ConfigEntries.TimelineTopColor = Config.Bind("Timeline", "02 Top Color", LSColors.red300, "Color of the top part of the timeline. (Only for Legacy waveform type)");
				ConfigEntries.TimelineBottomColor = Config.Bind("Timeline", "03 Bottom Color", LSColors.blue300, "Color of the bottom part of the timeline waveform. (Only for Legacy waveform type)");
				ConfigEntries.WaveformMode = Config.Bind("Timeline", "04 Mode", WaveformType.Legacy, "The mode of the timeline waveform.");
				ConfigEntries.GenerateWaveform = Config.Bind("Timeline", "05 Generate?", true, "If disabled, timeline will not generate when you load into a level and will decrease load time.");

				ConfigEntries.DraggingTimelineSliderPauses = Config.Bind("Timeline", "06 Drag Cursor Pauses", true, "If dragging the cursor pauses the level.");

				ConfigEntries.ReminderActive = Config.Bind("Reminder", "Active", true, "A little reminder will popup every now and then reminding you to have a break.");
				ConfigEntries.ReminderRepeat = Config.Bind("Reminder", "Repeat", 600f, "How often the reminder will occur.");
			}

			//Preview
			{
				ConfigEntries.ShowObjectsOnLayer = Config.Bind("Preview", "00 Show only objects on current layer?", false, "If enabled, all objects not on current layer will be set to transparent");
				ConfigEntries.ShowObjectsAlpha = Config.Bind("Preview", "01 Visible object opacity", 0.2f, "Opacity of the objects not on the current layer.");
				ConfigEntries.ShowEmpties = Config.Bind("Preview", "02 Show empties?", false, "If enabled, show all objects that are set to the empty object type.");
				ConfigEntries.ShowDamagable = Config.Bind("Preview", "03 Only Show Damagable?", false, "If enabled, only objects that can damage the player will be shown.");
				ConfigEntries.HighlightObjects = Config.Bind("Preview", "04 Highlight Objects?", true, "If enabled and if cursor hovers over an object, it will be highlighted.");
				ConfigEntries.HighlightColor = Config.Bind("Preview", "05 Object Highlight Amount", new Color(0.1f, 0.1f, 0.1f), "If an object is hovered, it adds this amount of color to the hovered object.");
				ConfigEntries.HighlightDoubleColor = Config.Bind("Preview", "06 Object Highlight Double Amount", new Color(0.5f, 0.5f, 0.5f), "If an object is hovered and shift is held, it adds this amount of color to all color channels.");
				ConfigEntries.PreviewSelectFix = Config.Bind("Preview", "07 Empties not selectable in preview?", false, "If enabled, empties will not be selectable in preview.");
				ConfigEntries.ShowSelector = Config.Bind("Preview", "Show Drag Selector?", true, "If enabled, a circular point will appear that allows you to move objects when the circlular point is dragged around.");
				emptyDisable = ConfigEntries.PreviewSelectFix.Value;
				emptyVisible = ConfigEntries.ShowEmpties.Value;
			}

			//Editor GUI
			{
				ConfigEntries.EditorGUIColor1 = Config.Bind("Editor GUI", "Color 1", new Color(0.1294118f, 0.1294118f, 0.1294118f, 1f), "Color theme slot 1.");
				ConfigEntries.EditorGUIColor2 = Config.Bind("Editor GUI", "Color 2", new Color(0.1058824f, 0.1058824f, 0.1098039f, 1f), "Color theme slot 2.");
				ConfigEntries.EditorGUIColor3 = Config.Bind("Editor GUI", "Color 3", new Color(0.937255f, 0.9215687f, 0.937255f, 1f), "Color theme slot 3.");
				ConfigEntries.EditorGUIColor4 = Config.Bind("Editor GUI", "Color 4", new Color(0.1882353f, 0.1882353f, 0.1882353f, 1f), "Color theme slot 4.");
				ConfigEntries.EditorGUIColor5 = Config.Bind("Editor GUI", "Color 5", new Color(0.2431373f, 0.2431373f, 0.2588235f, 1f), "Color theme slot 5.");
				ConfigEntries.EditorGUIColor6 = Config.Bind("Editor GUI", "Color 6", new Color(0.9333334f, 0.9176471f, 0.9333334f, 1f), "Color theme slot 6.");
				ConfigEntries.EditorGUIColor7 = Config.Bind("Editor GUI", "Color 7", new Color(0.2156863f, 0.2156863f, 0.2196079f, 1f), "Color theme slot 7.");
				ConfigEntries.EditorGUIColor8 = Config.Bind("Editor GUI", "Color 8", new Color(0.1960784f, 0.1960784f, 0.1960784f, 1f), "Color theme slot 8.");
				ConfigEntries.EditorGUIColor9 = Config.Bind("Editor GUI", "Color 9", new Color(0.1215686f, 0.1215686f, 0.1215686f, 1f), "Color theme slot 9.");
			}

			ConfigEntries.OriginXAmount = Config.Bind("Origin Offset", "X Amount", 0.1f, "The amount the origin X increases when the mouse scroll wheel is wheeled over the inputfield.");
			ConfigEntries.OriginYAmount = Config.Bind("Origin Offset", "Y Amount", 0.1f, "The amount the origin Y increases when the mouse scroll wheel is wheeled over the inputfield.");


			string dSl = "Render Depth";
			string co = " Color";
			string rdslh = " color of the Render Depth Slider handle.";

			//Depth
			{
				ConfigEntries.DepthNormalColor = Config.Bind(dSl, "00 Normal" + co, new Color(0.2588f, 0.2588f, 0.2588f, 1f), "Normal" + rdslh);
				ConfigEntries.DepthPressedColor = Config.Bind(dSl, "01 Pressed" + co, new Color(0.2588f, 0.2588f, 0.2588f, 1f), "Pressed" + rdslh);
				ConfigEntries.DepthHighlightedColor = Config.Bind(dSl, "02 Highlighted" + co, new Color(0.2588f, 0.2588f, 0.2588f, 1f), "Highlighted" + rdslh);
				ConfigEntries.DepthDisabledColor = Config.Bind(dSl, "03 Disabled" + co, new Color(0.5882f, 0.5882f, 0.5882f, 0.502f), "Disabled" + rdslh);
				ConfigEntries.DepthFadeDuration = Config.Bind(dSl, "04 Fade Duration", 0.01f, "How quick the highlighted / pressed color sets.");
				ConfigEntries.DepthInteractable = Config.Bind(dSl, "05 Interactable", true, "If the Depth Slider is interactable or not.");
				ConfigEntries.DepthUpdate = Config.Bind(dSl, "06 Update", false, "If the Depth Slider updates. If true, setting the depth via the text box will result in a slight glitch with setting the value.");
				ConfigEntries.SliderRMax = Config.Bind(dSl, "07 Slider Max", 220, "Max value the slider can show.");
				ConfigEntries.SliderRMin = Config.Bind(dSl, "08 Slider Min", -100, "Min value the slider can show.");
				ConfigEntries.SliderDDirection = Config.Bind(dSl, "09 Direction", Slider.Direction.RightToLeft, "Direction the slider goes in. BottomToTop / TopToBottom not recommended.");
				ConfigEntries.DepthAmount = Config.Bind(dSl, "0A Depth Amount", 1, "The amount the depth increases when the mouse scroll wheel is wheeled over the inputfield.");
			}

			string tcot = "The color of ";
			string tcs = "timeline cursor / scrubber.";

			ConfigEntries.MTSliderCol = Config.Bind("Cursor Color", "00 Timeline", new Color(0.251f, 0.4627f, 0.8745f, 1f), tcot + "the main " + tcs);
			ConfigEntries.KTSliderCol = Config.Bind("Cursor Color", "01 Object", new Color(0.251f, 0.4627f, 0.8745f, 1f), tcot + "the keyframe " + tcs);
			ConfigEntries.ObjSelCol = Config.Bind("Cursor Color", "02 Object Selection", new Color(0.251f, 0.4627f, 0.8745f, 1f), tcot + "a selected object.");

			ConfigEntries.KeyframeSnap = Config.Bind("BPM Snap", "Snap Affects Keyframes", false, "If the BPM snap should snap the object keyframes when dragged.");
			ConfigEntries.SnapAmount = Config.Bind("BPM Snap", "Snap Divisions", 4f, "H");

			//Prefabs
            {
				//Prefab Type Name Config
				ConfigEntries.PT0N = Config.Bind("Prefab Types", "00 Name", "Bombs", "Name of Prefab Type 0.");
				ConfigEntries.PT1N = Config.Bind("Prefab Types", "01 Name", "Bullets", "Name of Prefab Type 1.");
				ConfigEntries.PT2N = Config.Bind("Prefab Types", "02 Name", "Beams", "Name of Prefab Type 2.");
				ConfigEntries.PT3N = Config.Bind("Prefab Types", "03 Name", "Spinners", "Name of Prefab Type 3.");
				ConfigEntries.PT4N = Config.Bind("Prefab Types", "04 Name", "Pulses", "Name of Prefab Type 4.");
				ConfigEntries.PT5N = Config.Bind("Prefab Types", "05 Name", "Characters", "Name of Prefab Type 5.");
				ConfigEntries.PT6N = Config.Bind("Prefab Types", "06 Name", "Misc 1", "Name of Prefab Type 6.");
				ConfigEntries.PT7N = Config.Bind("Prefab Types", "07 Name", "Misc 2", "Name of Prefab Type 7.");
				ConfigEntries.PT8N = Config.Bind("Prefab Types", "08 Name", "Misc 3", "Name of Prefab Type 8.");
				ConfigEntries.PT9N = Config.Bind("Prefab Types", "09 Name", "Misc 4", "Name of Prefab Type 9.");

				//New Prefab Type Name Config
				ConfigEntries.PT10N = Config.Bind("Prefab Types", "10 Name", "NewType 0", "Name of Prefab Type 10.");
				ConfigEntries.PT11N = Config.Bind("Prefab Types", "11 Name", "NewType 1", "Name of Prefab Type 11.");
				ConfigEntries.PT12N = Config.Bind("Prefab Types", "12 Name", "NewType 2", "Name of Prefab Type 12.");
				ConfigEntries.PT13N = Config.Bind("Prefab Types", "13 Name", "NewType 3", "Name of Prefab Type 13.");
				ConfigEntries.PT14N = Config.Bind("Prefab Types", "14 Name", "NewType 4", "Name of Prefab Type 14.");
				ConfigEntries.PT15N = Config.Bind("Prefab Types", "15 Name", "NewType 5", "Name of Prefab Type 15.");
				ConfigEntries.PT16N = Config.Bind("Prefab Types", "16 Name", "NewType 6", "Name of Prefab Type 16.");
				ConfigEntries.PT17N = Config.Bind("Prefab Types", "17 Name", "NewType 7", "Name of Prefab Type 17.");
				ConfigEntries.PT18N = Config.Bind("Prefab Types", "18 Name", "NewType 8", "Name of Prefab Type 18.");
				ConfigEntries.PT19N = Config.Bind("Prefab Types", "19 Name", "NewType 9", "Name of Prefab Type 19.");

				//Prefab Type Color Config
				ConfigEntries.PT0C = Config.Bind("Prefab Types", "00 Color", new Color(0.9137f, 0.1176f, 0.3882f), "Color of Prefab Type 0.");
				ConfigEntries.PT1C = Config.Bind("Prefab Types", "01 Color", new Color(0.6118f, 0.1529f, 0.6902f), "Color of Prefab Type 1.");
				ConfigEntries.PT2C = Config.Bind("Prefab Types", "02 Color", new Color(0.2471f, 0.3176f, 0.7098f), "Color of Prefab Type 2.");
				ConfigEntries.PT3C = Config.Bind("Prefab Types", "03 Color", new Color(0.0118f, 0.6627f, 0.9569f), "Color of Prefab Type 3.");
				ConfigEntries.PT4C = Config.Bind("Prefab Types", "04 Color", new Color(0f, 0.5882f, 0.5333f), "Color of Prefab Type 4.");
				ConfigEntries.PT5C = Config.Bind("Prefab Types", "05 Color", new Color(0.5451f, 0.7647f, 0.2902f), "Color of Prefab Type 5.");
				ConfigEntries.PT6C = Config.Bind("Prefab Types", "06 Color", new Color(1f, 0.9216f, 0.2314f), "Color of Prefab Type 6.");
				ConfigEntries.PT7C = Config.Bind("Prefab Types", "07 Color", new Color(1f, 0.5961f, 0f), "Color of Prefab Type 7.");
				ConfigEntries.PT8C = Config.Bind("Prefab Types", "08 Color", new Color(1f, 0.3412f, 0.1333f), "Color of Prefab Type 8.");
				ConfigEntries.PT9C = Config.Bind("Prefab Types", "09 Color", new Color(1f, 0.1137f, 0.1333f), "Color of Prefab Type 9.");

				//New Prefab Type Color Config
				ConfigEntries.PT10C = Config.Bind("Prefab Types", "10 Color", new Color(1f, 1f, 1f, 1f), "Color of Prefab Type 10.");
				ConfigEntries.PT11C = Config.Bind("Prefab Types", "11 Color", new Color(1f, 1f, 1f, 1f), "Color of Prefab Type 11.");
				ConfigEntries.PT12C = Config.Bind("Prefab Types", "12 Color", new Color(1f, 1f, 1f, 1f), "Color of Prefab Type 12.");
				ConfigEntries.PT13C = Config.Bind("Prefab Types", "13 Color", new Color(1f, 1f, 1f, 1f), "Color of Prefab Type 13.");
				ConfigEntries.PT14C = Config.Bind("Prefab Types", "14 Color", new Color(1f, 1f, 1f, 1f), "Color of Prefab Type 14.");
				ConfigEntries.PT15C = Config.Bind("Prefab Types", "15 Color", new Color(1f, 1f, 1f, 1f), "Color of Prefab Type 15.");
				ConfigEntries.PT16C = Config.Bind("Prefab Types", "16 Color", new Color(1f, 1f, 1f, 1f), "Color of Prefab Type 16.");
				ConfigEntries.PT17C = Config.Bind("Prefab Types", "17 Color", new Color(1f, 1f, 1f, 1f), "Color of Prefab Type 17.");
				ConfigEntries.PT18C = Config.Bind("Prefab Types", "18 Color", new Color(1f, 1f, 1f, 1f), "Color of Prefab Type 18.");
				ConfigEntries.PT19C = Config.Bind("Prefab Types", "19 Color", new Color(1f, 1f, 1f, 1f), "Color of Prefab Type 19.");

				//Prefab Popup Config
				ConfigEntries.SizeUpper = Config.Bind("Prefab Popup", "00 Button Hover Scale", 1.05f, new ConfigDescription("How much hovering over one of the buttons should increase its scale.", new AcceptableValueRange<float>(0.7f, 1.4f)));

				ConfigEntries.PrefabINHScroll = Config.Bind("Prefab Popup", "Internal Horizontal Scroll", false, "If you can scroll left / right or not.");
				ConfigEntries.PrefabINCellSize = Config.Bind("Prefab Popup", "Internal Cell Size", new Vector2(383f, 32f), "Size of each Prefab Cell. Recommended values are 383 and 503.");
				ConfigEntries.PrefabINConstraint = Config.Bind("Prefab Popup", "Internal Constraint Mode", GridLayoutGroup.Constraint.FixedColumnCount, "Which direction the prefab list goes.");
				ConfigEntries.PrefabINConstraintColumns = Config.Bind("Prefab Popup", "Internal Constraint", 1, "How many columns the prefabs are divided into.");
				ConfigEntries.PrefabINCellSpacing = Config.Bind("Prefab Popup", "Internal Spacing", new Vector2(8f, 8f), "Distance between each Prefab Cell.");
				ConfigEntries.PrefabINAxis = Config.Bind("Prefab Popup", "Internal Start Axis", GridLayoutGroup.Axis.Horizontal, "Start axis of the prefab list.");
				ConfigEntries.PrefabINLDeletePos = Config.Bind("Prefab Popup", "Internal Delete Button Pos", new Vector2(367f, -16f), "Position of the Delete Button. Recommended values are 367, -16 and 484, -16.");
				ConfigEntries.PrefabINLDeleteSca = Config.Bind("Prefab Popup", "Internal Delete Button Sca", new Vector2(32f, 32f), "Scale of the Delete Button.");

				ConfigEntries.PrefabINNameHOverflow = Config.Bind("Prefab Popup", "Internal Name HOverflow", HorizontalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
				ConfigEntries.PrefabINNameVOverflow = Config.Bind("Prefab Popup", "Internal Name VOverflow", VerticalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
				ConfigEntries.PrefabINNameFontSize = Config.Bind("Prefab Popup", "Internal Name Font Size", 20, "Size of the text font.");
				ConfigEntries.PrefabINTypeHOverflow = Config.Bind("Prefab Popup", "Internal Type HOverflow", HorizontalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
				ConfigEntries.PrefabINTypeVOverflow = Config.Bind("Prefab Popup", "Internal Type VOverflow", VerticalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
				ConfigEntries.PrefabINTypeFontSize = Config.Bind("Prefab Popup", "Internal Type Font Size", 20, "Size of the text font.");

				ConfigEntries.PrefabEXHScroll = Config.Bind("Prefab Popup", "External Horizontal Scroll", false, "If you can scroll left / right or not.");
				ConfigEntries.PrefabEXCellSize = Config.Bind("Prefab Popup", "External Cell Size", new Vector2(383f, 32f), "Size of each Prefab Cell. Recommended values are 383 and 503.");
				ConfigEntries.PrefabEXConstraint = Config.Bind("Prefab Popup", "External Constraint Mode", GridLayoutGroup.Constraint.FixedColumnCount, "Which direction the prefab list goes.");
				ConfigEntries.PrefabEXConstraintColumns = Config.Bind("Prefab Popup", "External Constraint", 1, "How many columns the prefabs are divided into.");
				ConfigEntries.PrefabEXCellSpacing = Config.Bind("Prefab Popup", "External Spacing", new Vector2(8f, 8f), "Distance between each Prefab Cell.");
				ConfigEntries.PrefabEXAxis = Config.Bind("Prefab Popup", "External Start Axis", GridLayoutGroup.Axis.Horizontal, "Start axis of the prefab list.");
				ConfigEntries.PrefabEXLDeletePos = Config.Bind("Prefab Popup", "External Delete Button Pos", new Vector2(367f, -16f), "Position of the Delete Button. Recommended values are 367, -16 and 484, -16.");
				ConfigEntries.PrefabEXLDeleteSca = Config.Bind("Prefab Popup", "External Delete Button Sca", new Vector2(32f, 32f), "Scale of the Delete Button.");

				ConfigEntries.PrefabEXNameHOverflow = Config.Bind("Prefab Popup", "Internal Name HOverflow", HorizontalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
				ConfigEntries.PrefabEXNameVOverflow = Config.Bind("Prefab Popup", "Internal Name VOverflow", VerticalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
				ConfigEntries.PrefabEXNameFontSize = Config.Bind("Prefab Popup", "Internal Name Font Size", 20, "Size of the text font.");
				ConfigEntries.PrefabEXTypeHOverflow = Config.Bind("Prefab Popup", "Internal Type HOverflow", HorizontalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
				ConfigEntries.PrefabEXTypeVOverflow = Config.Bind("Prefab Popup", "Internal Type VOverflow", VerticalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
				ConfigEntries.PrefabEXTypeFontSize = Config.Bind("Prefab Popup", "Internal Type Font Size", 20, "Size of the text font.");

				ConfigEntries.PrefabINANCH = Config.Bind("Prefab Popup", "Internal Popup Pos", new Vector2(0f, -16f), "Position of the internal prefabs popup. Recommended values are 0, -16 and -100, -16.");
				ConfigEntries.PrefabINSD = Config.Bind("Prefab Popup", "Internal Popup Size", new Vector2(400f, -32f), "Scale of the internal prefabs popup. Recommended values are 400, -32 and 520, -32.");
				ConfigEntries.PrefabEXANCH = Config.Bind("Prefab Popup", "External Popup Pos", new Vector2(-32f, -16f), "Position of the external prefabs popup. Recommended values are -32, -16 and 68, -16.");
				ConfigEntries.PrefabEXSD = Config.Bind("Prefab Popup", "External Popup Size", new Vector2(400f, -32f), "Scale of the external prefabs popup. Recommended values are 400, -32 and 520, -32.");
				ConfigEntries.PrefabEXPathPos = Config.Bind("Prefab Popup", "External Prefab Path Pos", new Vector2(325f, 15f), "Position of the prefab path input field.");
				ConfigEntries.PrefabEXPathSca = Config.Bind("Prefab Popup", "External Prefab Path Length", 150f, "Length of the prefab path input field.");
				ConfigEntries.PrefabEXRefreshPos = Config.Bind("Prefab Popup", "External Prefab Refresh", new Vector2(210f, 450f), "Position of the prefab refresh button.");

				ConfigEntries.PQCKey0 = Config.Bind("Quick Prefab Create", "00 Prefab Key", KeyCode.F, "Keycode for 1st Spawn Prefab.");
				ConfigEntries.PQCKey1 = Config.Bind("Quick Prefab Create", "01 Prefab Key", KeyCode.G, "Keycode for 2nd Spawn Prefab.");
				ConfigEntries.PQCKey2 = Config.Bind("Quick Prefab Create", "02 Prefab Key", KeyCode.H, "Keycode for 3rd Spawn Prefab.");
				ConfigEntries.PQCKey3 = Config.Bind("Quick Prefab Create", "03 Prefab Key", KeyCode.J, "Keycode for 4th Spawn Prefab.");
				ConfigEntries.PQCKey4 = Config.Bind("Quick Prefab Create", "04 Prefab Key", KeyCode.K, "Keycode for 5th Spawn Prefab.");

				ConfigEntries.PQCIndex0 = Config.Bind("Quick Prefab Create", "10 Prefab Index", 0, "Prefab Index for 1st Spawn Prefab.");
				ConfigEntries.PQCIndex1 = Config.Bind("Quick Prefab Create", "11 Prefab Index", 0, "Prefab Index for 2nd Spawn Prefab.");
				ConfigEntries.PQCIndex2 = Config.Bind("Quick Prefab Create", "12 Prefab Index", 0, "Prefab Index for 3rd Spawn Prefab.");
				ConfigEntries.PQCIndex3 = Config.Bind("Quick Prefab Create", "13 Prefab Index", 0, "Prefab Index for 4th Spawn Prefab.");
				ConfigEntries.PQCIndex4 = Config.Bind("Quick Prefab Create", "14 Prefab Index", 0, "Prefab Index for 5th Spawn Prefab.");

				ConfigEntries.PQCActive0 = Config.Bind("Quick Prefab Create", "20 Binding Active", false, "If binding is active.");
				ConfigEntries.PQCActive1 = Config.Bind("Quick Prefab Create", "21 Binding Active", false, "If binding is active.");
				ConfigEntries.PQCActive2 = Config.Bind("Quick Prefab Create", "22 Binding Active", false, "If binding is active.");
				ConfigEntries.PQCActive3 = Config.Bind("Quick Prefab Create", "23 Binding Active", false, "If binding is active.");
				ConfigEntries.PQCActive4 = Config.Bind("Quick Prefab Create", "24 Binding Active", false, "If binding is active.");
			}

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
				harmony.PatchAll(typeof(DataManagerPatch));
				harmony.PatchAll(typeof(DataManagerBeatmapThemePatch));
				harmony.PatchAll(typeof(DataManagerGameDataPatch));
				harmony.PatchAll(typeof(GameManagerPatch));
				harmony.PatchAll(typeof(EventEditorPatch));
				harmony.PatchAll(typeof(ThemeEditorPatch));
				harmony.PatchAll(typeof(ObjectManagerPatch));

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

				if (ConfigEntries.RenderTimeline.Value == true && ConfigEntries.GenerateWaveform.Value == true)
				{
					AssignTimelineTexture();
					if (ConfigEntries.WaveformMode.Value == WaveformType.Legacy)
					{
						LegacyWaveform();
					}
					if (ConfigEntries.WaveformMode.Value == WaveformType.Old)
					{
						OldWaveform();
					}
				}

				SetNewMarkerColors();
				SetAutosave();

				if (ConfigEntries.IfReloadLList.Value == true)
				{
					EditorManager.inst.GetLevelList();
					RenderBeatmapSet();
				}

				tracker.GetComponent<DraggableObject>().enabled = ConfigEntries.ShowSelector.Value;
				tracker.GetComponent<PolygonCollider2D>().enabled = ConfigEntries.ShowSelector.Value;
				tracker.GetComponent<MeshRenderer>().enabled = ConfigEntries.ShowSelector.Value;

				//Animate GUI
				{
					var editorPropertiesPopupAIGUI = EditorManager.inst.GetDialog("Editor Properties Popup").Dialog.gameObject.GetComponent<AnimateInGUI>();

					editorPropertiesPopupAIGUI.SetEasing((int)ConfigEntries.EPPAnimateEaseIn.Value, (int)ConfigEntries.EPPAnimateEaseOut.Value);
					editorPropertiesPopupAIGUI.animateX = ConfigEntries.EPPAnimateX.Value;
					editorPropertiesPopupAIGUI.animateY = ConfigEntries.EPPAnimateY.Value;
					editorPropertiesPopupAIGUI.animateInTime = ConfigEntries.EPPAnimateInOutSpeeds.Value.x;
					editorPropertiesPopupAIGUI.animateOutTime = ConfigEntries.EPPAnimateInOutSpeeds.Value.y;

					var openFilePopupAIGUI = EditorManager.inst.GetDialog("Open File Popup").Dialog.gameObject.GetComponent<AnimateInGUI>();

					openFilePopupAIGUI.SetEasing((int)ConfigEntries.OFPAnimateEaseIn.Value, (int)ConfigEntries.OFPAnimateEaseOut.Value);
					openFilePopupAIGUI.animateX = ConfigEntries.OFPAnimateX.Value;
					openFilePopupAIGUI.animateY = ConfigEntries.OFPAnimateY.Value;
					openFilePopupAIGUI.animateInTime = ConfigEntries.OFPAnimateInOutSpeeds.Value.x;
					openFilePopupAIGUI.animateOutTime = ConfigEntries.OFPAnimateInOutSpeeds.Value.y;

					var newFilePopupAIGUI = EditorManager.inst.GetDialog("New File Popup").Dialog.gameObject.GetComponent<AnimateInGUI>();

					newFilePopupAIGUI.SetEasing((int)ConfigEntries.NFPAnimateEaseIn.Value, (int)ConfigEntries.NFPAnimateEaseOut.Value);
					newFilePopupAIGUI.animateX = ConfigEntries.NFPAnimateX.Value;
					newFilePopupAIGUI.animateY = ConfigEntries.NFPAnimateY.Value;
					newFilePopupAIGUI.animateInTime = ConfigEntries.NFPAnimateInOutSpeeds.Value.x;
					newFilePopupAIGUI.animateOutTime = ConfigEntries.NFPAnimateInOutSpeeds.Value.y;

					var prefabPopupAIGUI = EditorManager.inst.GetDialog("Prefab Popup").Dialog.gameObject.GetComponent<AnimateInGUI>();

					prefabPopupAIGUI.SetEasing((int)ConfigEntries.PPAnimateEaseIn.Value, (int)ConfigEntries.PPAnimateEaseOut.Value);
					prefabPopupAIGUI.animateX = ConfigEntries.PPAnimateX.Value;
					prefabPopupAIGUI.animateY = ConfigEntries.PPAnimateY.Value;
					prefabPopupAIGUI.animateInTime = ConfigEntries.PPAnimateInOutSpeeds.Value.x;
					prefabPopupAIGUI.animateOutTime = ConfigEntries.PPAnimateInOutSpeeds.Value.y;

					var quickActionsPopupAIGUI = EditorManager.inst.GetDialog("Quick Actions Popup").Dialog.gameObject.GetComponent<AnimateInGUI>();

					quickActionsPopupAIGUI.SetEasing((int)ConfigEntries.QAPAnimateEaseIn.Value, (int)ConfigEntries.QAPAnimateEaseOut.Value);
					quickActionsPopupAIGUI.animateX = ConfigEntries.QAPAnimateX.Value;
					quickActionsPopupAIGUI.animateY = ConfigEntries.QAPAnimateY.Value;
					quickActionsPopupAIGUI.animateInTime = ConfigEntries.QAPAnimateInOutSpeeds.Value.x;
					quickActionsPopupAIGUI.animateOutTime = ConfigEntries.QAPAnimateInOutSpeeds.Value.y;

					var objectOptionsPopupAIGUI = EditorManager.inst.GetDialog("Object Options Popup").Dialog.gameObject.GetComponent<AnimateInGUI>();

					objectOptionsPopupAIGUI.SetEasing((int)ConfigEntries.OBJPAnimateEaseIn.Value, (int)ConfigEntries.OBJPAnimateEaseOut.Value);
					objectOptionsPopupAIGUI.animateX = ConfigEntries.OBJPAnimateX.Value;
					objectOptionsPopupAIGUI.animateY = ConfigEntries.OBJPAnimateY.Value;
					objectOptionsPopupAIGUI.animateInTime = ConfigEntries.OBJPAnimateInOutSpeeds.Value.x;
					objectOptionsPopupAIGUI.animateOutTime = ConfigEntries.OBJPAnimateInOutSpeeds.Value.y;

					var bgOptionsPopupAIGUI = EditorManager.inst.GetDialog("BG Options Popup").Dialog.gameObject.GetComponent<AnimateInGUI>();

					bgOptionsPopupAIGUI.SetEasing((int)ConfigEntries.BGPAnimateEaseIn.Value, (int)ConfigEntries.BGPAnimateEaseOut.Value);
					bgOptionsPopupAIGUI.animateX = ConfigEntries.BGPAnimateX.Value;
					bgOptionsPopupAIGUI.animateY = ConfigEntries.BGPAnimateY.Value;
					bgOptionsPopupAIGUI.animateInTime = ConfigEntries.BGPAnimateInOutSpeeds.Value.x;
					bgOptionsPopupAIGUI.animateOutTime = ConfigEntries.BGPAnimateInOutSpeeds.Value.y;

					var gameObjectDialogAIGUI = EditorManager.inst.GetDialog("Object Editor").Dialog.gameObject.GetComponent<AnimateInGUI>();

					gameObjectDialogAIGUI.SetEasing((int)ConfigEntries.GODAnimateEaseIn.Value, (int)ConfigEntries.GODAnimateEaseOut.Value);
					gameObjectDialogAIGUI.animateX = ConfigEntries.GODAnimateX.Value;
					gameObjectDialogAIGUI.animateY = ConfigEntries.GODAnimateY.Value;
					gameObjectDialogAIGUI.animateInTime = ConfigEntries.GODAnimateInOutSpeeds.Value.x;
					gameObjectDialogAIGUI.animateOutTime = ConfigEntries.GODAnimateInOutSpeeds.Value.y;

					if (EditorManager.inst.GetDialog("Player Editor").Dialog && EditorManager.inst.GetDialog("Object Editor").Dialog.gameObject.GetComponent<AnimateInGUI>())
					{
						var playerEditorDialogAIGUI = EditorManager.inst.GetDialog("Object Editor").Dialog.gameObject.GetComponent<AnimateInGUI>();

						playerEditorDialogAIGUI.SetEasing((int)ConfigEntries.GODAnimateEaseIn.Value, (int)ConfigEntries.GODAnimateEaseOut.Value);
						playerEditorDialogAIGUI.animateX = ConfigEntries.GODAnimateX.Value;
						playerEditorDialogAIGUI.animateY = ConfigEntries.GODAnimateY.Value;
						playerEditorDialogAIGUI.animateInTime = ConfigEntries.GODAnimateInOutSpeeds.Value.x;
						playerEditorDialogAIGUI.animateOutTime = ConfigEntries.GODAnimateInOutSpeeds.Value.y;
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
				notifyRT.sizeDelta = new Vector2(ConfigEntries.NotificationWidth.Value, 632f);
				GameObject.Find("Editor Systems/Editor GUI/sizer/main/Notifications").transform.localScale = new Vector3(ConfigEntries.NotificationSize.Value, ConfigEntries.NotificationSize.Value, 1f);

				if (ConfigEntries.NotificationDirection.Value == Direction.Down)
                {
					notifyRT.anchoredPosition = new Vector2(8f, 408f);
					notifyGroup.childAlignment = TextAnchor.LowerLeft;


				}
				if (ConfigEntries.NotificationDirection.Value == Direction.Up)
                {
					notifyRT.anchoredPosition = new Vector2(8f, 410f);
					notifyGroup.childAlignment = TextAnchor.UpperLeft;
				}

				//Set Editor Zoom cap
				EditorManager.inst.zoomBounds = ConfigEntries.ETLZoomBounds.Value;

				//Set Open File Popup RectTransform
				openRTLevel.anchoredPosition = ConfigEntries.ORLAnchoredPos.Value;
				openRTLevel.sizeDelta = ConfigEntries.ORLSizeDelta.Value;

				//Set Open FIle Popup content GridLayoutGroup
				openGridLVL.cellSize = ConfigEntries.OGLVLCellSize.Value;
				openGridLVL.constraint = (GridLayoutGroup.Constraint)ConfigEntries.OGLVLConstraint.Value;
				openGridLVL.constraintCount = ConfigEntries.OGLVLConstraintCount.Value;
				openGridLVL.spacing = ConfigEntries.OGLVLSpacing.Value;

				//Folder Button
				fButtonText.horizontalOverflow = ConfigEntries.FButtonHWrap.Value;
				fButtonText.verticalOverflow = ConfigEntries.FButtonVWrap.Value;
				fButtonText.color = ConfigEntries.FButtonTextColor.Value;
				fButtonText.fontSize = ConfigEntries.FButtonFontSize.Value;

				//Folder Button Colors
				ColorBlock cb = fButtonBUTT.colors;
				cb.normalColor = ConfigEntries.FButtonNColor.Value;
				cb.pressedColor = ConfigEntries.FButtonPColor.Value;
				cb.highlightedColor = ConfigEntries.FButtonHColor.Value;
				cb.selectedColor = ConfigEntries.FButtonSColor.Value;
				cb.fadeDuration = ConfigEntries.FButtonFadeDColor.Value;
				fButtonBUTT.colors = cb;

				ObjEditor.inst.zoomBounds = ConfigEntries.ObjZoomBounds.Value;
				if (ConfigEntries.ETLZoomBounds.Value != lastEdtBounds)
				{
					lastEdtBounds = ConfigEntries.ETLZoomBounds.Value;
					EditorManager.inst.zoomBounds = ConfigEntries.ETLZoomBounds.Value;
					EditorManager.inst.RenderTimeline();
				}

				RectTransform dropdownRT = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("tod-dropdown(Clone)").gameObject.GetComponent<RectTransform>();
				dropdownRT.anchoredPosition = ConfigEntries.ORLDropdownPos.Value;

				RectTransform toggleRT = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("toggle(Clone)").gameObject.GetComponent<RectTransform>();
				toggleRT.anchoredPosition = ConfigEntries.ORLTogglePos.Value;

				EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("editor path").gameObject.GetComponent<RectTransform>().anchoredPosition = ConfigEntries.ORLPathPos.Value;
				EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("editor path").gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(ConfigEntries.ORLPathLength.Value, 34f);
				EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("reload").gameObject.GetComponent<RectTransform>().anchoredPosition = ConfigEntries.ORLRefreshPos.Value;

				if (ConfigEntries.PreviewSelectFix.Value != emptyDisable)
				{
					emptyDisable = ConfigEntries.PreviewSelectFix.Value;
					ObjectManager.inst.updateObjects();
				}
				if (ConfigEntries.ShowEmpties.Value != emptyVisible)
				{
					emptyVisible = ConfigEntries.ShowEmpties.Value;
					ObjectManager.inst.updateObjects();
				}
				if (ConfigEntries.ShowDamagable.Value != showDamagable)
				{
					showDamagable = ConfigEntries.ShowDamagable.Value;
					ObjectManager.inst.updateObjects();
				}

                //Cursor Color
                {
					if (GameObject.Find("Editor Systems/Editor GUI/sizer/main/whole-timeline/Slider_Parent/Slider/Handle Slide Area/Image/Handle"))
					{
						GameObject.Find("Editor Systems/Editor GUI/sizer/main/whole-timeline/Slider_Parent/Slider/Handle Slide Area/Image/Handle").GetComponent<Image>().color = ConfigEntries.MTSliderCol.Value;
					}
					else
                    {
						RTEditor.DisplayCustomNotification("CD", "Whoooops you gotta put this CD up your-", 1f, LSColors.HexToColor("202020"), ConfigEntries.MTSliderCol.Value, Color.white, "Whoops!");
                    }

					if (GameObject.Find("Editor Systems/Editor GUI/sizer/main/whole-timeline/Slider_Parent/Slider/Handle Slide Area/Image"))
					{
						GameObject.Find("Editor Systems/Editor GUI/sizer/main/whole-timeline/Slider_Parent/Slider/Handle Slide Area/Image").GetComponent<Image>().color = ConfigEntries.MTSliderCol.Value;
					}
					else
					{
						RTEditor.DisplayCustomNotification("CD", "Whoooops you gotta put this CD up your-", 1f, LSColors.HexToColor("202020"), ConfigEntries.MTSliderCol.Value, Color.white, "Whoops!");
					}

					if (GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/timeline/Scroll View/Viewport/Content/time_slider/Handle Slide Area/Handle/Image"))
					{
						GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/timeline/Scroll View/Viewport/Content/time_slider/Handle Slide Area/Handle/Image").GetComponent<Image>().color = ConfigEntries.KTSliderCol.Value;
					}
					else
					{
						RTEditor.DisplayCustomNotification("CD", "Whoooops you gotta put this CD up your-", 1f, LSColors.HexToColor("202020"), ConfigEntries.KTSliderCol.Value, Color.white, "Whoops!");
					}

					if (GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/timeline/Scroll View/Viewport/Content/time_slider/Handle Slide Area/Handle"))
					{
						GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/timeline/Scroll View/Viewport/Content/time_slider/Handle Slide Area/Handle").GetComponent<Image>().color = ConfigEntries.KTSliderCol.Value;
					}
					else
					{
						RTEditor.DisplayCustomNotification("CD", "Whoooops you gotta put this CD up your-", 1f, LSColors.HexToColor("202020"), ConfigEntries.KTSliderCol.Value, Color.white, "Whoops!");
					}

					ObjEditor.inst.SelectedColor = ConfigEntries.ObjSelCol.Value;
				}

                //Render Depth
                {
					Transform sliderObject = ObjEditor.inst.ObjectView.transform.Find("depth/depth");
					Slider sliderComponent = sliderObject.GetComponent<Slider>();
					ColorBlock cbd = sliderComponent.colors;
					cbd.normalColor = ConfigEntries.DepthNormalColor.Value;
					cbd.pressedColor = ConfigEntries.DepthPressedColor.Value;
					cbd.highlightedColor = ConfigEntries.DepthHighlightedColor.Value;
					cbd.disabledColor = ConfigEntries.DepthDisabledColor.Value;
					cbd.fadeDuration = ConfigEntries.DepthFadeDuration.Value;
					sliderComponent.colors = cbd;
					sliderComponent.interactable = ConfigEntries.DepthInteractable.Value;
					sliderComponent.maxValue = ConfigEntries.SliderRMax.Value;
					sliderComponent.minValue = ConfigEntries.SliderRMin.Value;
					sliderComponent.direction = ConfigEntries.SliderDDirection.Value;
				}

                //Theme
                {
					EventEditor.inst.ThemePanel.transform.GetChild(1).GetComponent<Text>().horizontalOverflow = ConfigEntries.ThemeHWM.Value;
					EventEditor.inst.ThemePanel.transform.GetChild(1).GetComponent<Text>().verticalOverflow = ConfigEntries.ThemeVWM.Value;
					EventEditor.inst.ThemePanel.transform.GetChild(1).GetComponent<Text>().fontSize = ConfigEntries.ThemeFSize.Value;
					EventEditor.inst.ThemePanel.transform.GetChild(1).GetComponent<Text>().color = ConfigEntries.ThemeTColor.Value;
					EventEditor.inst.ThemePanel.GetComponent<Image>().color = ConfigEntries.ThemeBColor.Value;
				}

				//Prefabs
                {
					//Create Local Variables
					GameObject internalPrefab = GameObject.Find("Prefab Popup/internal prefabs");
					GameObject externalPrefab = GameObject.Find("Prefab Popup/external prefabs");
					GridLayoutGroup inPMCGridLay = internalPrefab.transform.Find("mask/content").GetComponent<GridLayoutGroup>();
					GridLayoutGroup exPMCGridLay = externalPrefab.transform.Find("mask/content").GetComponent<GridLayoutGroup>();

					//Update Name
					{
						DataManager.inst.PrefabTypes[0].Name = ConfigEntries.PT0N.Value;
						DataManager.inst.PrefabTypes[1].Name = ConfigEntries.PT1N.Value;
						DataManager.inst.PrefabTypes[2].Name = ConfigEntries.PT2N.Value;
						DataManager.inst.PrefabTypes[3].Name = ConfigEntries.PT3N.Value;
						DataManager.inst.PrefabTypes[4].Name = ConfigEntries.PT4N.Value;
						DataManager.inst.PrefabTypes[5].Name = ConfigEntries.PT5N.Value;
						DataManager.inst.PrefabTypes[6].Name = ConfigEntries.PT6N.Value;
						DataManager.inst.PrefabTypes[7].Name = ConfigEntries.PT7N.Value;
						DataManager.inst.PrefabTypes[8].Name = ConfigEntries.PT8N.Value;
						DataManager.inst.PrefabTypes[9].Name = ConfigEntries.PT9N.Value;
					}

					//Update New Name
					{
						DataManager.inst.PrefabTypes[10].Name = ConfigEntries.PT10N.Value;
						DataManager.inst.PrefabTypes[11].Name = ConfigEntries.PT11N.Value;
						DataManager.inst.PrefabTypes[12].Name = ConfigEntries.PT12N.Value;
						DataManager.inst.PrefabTypes[13].Name = ConfigEntries.PT13N.Value;
						DataManager.inst.PrefabTypes[14].Name = ConfigEntries.PT14N.Value;
						DataManager.inst.PrefabTypes[15].Name = ConfigEntries.PT15N.Value;
						DataManager.inst.PrefabTypes[16].Name = ConfigEntries.PT16N.Value;
						DataManager.inst.PrefabTypes[17].Name = ConfigEntries.PT17N.Value;
						DataManager.inst.PrefabTypes[18].Name = ConfigEntries.PT18N.Value;
						DataManager.inst.PrefabTypes[19].Name = ConfigEntries.PT19N.Value;
					}

					//Update Color
					{
						DataManager.inst.PrefabTypes[0].Color = ConfigEntries.PT0C.Value;
						DataManager.inst.PrefabTypes[1].Color = ConfigEntries.PT1C.Value;
						DataManager.inst.PrefabTypes[2].Color = ConfigEntries.PT2C.Value;
						DataManager.inst.PrefabTypes[3].Color = ConfigEntries.PT3C.Value;
						DataManager.inst.PrefabTypes[4].Color = ConfigEntries.PT4C.Value;
						DataManager.inst.PrefabTypes[5].Color = ConfigEntries.PT5C.Value;
						DataManager.inst.PrefabTypes[6].Color = ConfigEntries.PT6C.Value;
						DataManager.inst.PrefabTypes[7].Color = ConfigEntries.PT7C.Value;
						DataManager.inst.PrefabTypes[8].Color = ConfigEntries.PT8C.Value;
						DataManager.inst.PrefabTypes[9].Color = ConfigEntries.PT9C.Value;
					}

					//Update New Color
					{
						DataManager.inst.PrefabTypes[10].Color = ConfigEntries.PT10C.Value;
						DataManager.inst.PrefabTypes[11].Color = ConfigEntries.PT11C.Value;
						DataManager.inst.PrefabTypes[12].Color = ConfigEntries.PT12C.Value;
						DataManager.inst.PrefabTypes[13].Color = ConfigEntries.PT13C.Value;
						DataManager.inst.PrefabTypes[14].Color = ConfigEntries.PT14C.Value;
						DataManager.inst.PrefabTypes[15].Color = ConfigEntries.PT15C.Value;
						DataManager.inst.PrefabTypes[16].Color = ConfigEntries.PT16C.Value;
						DataManager.inst.PrefabTypes[17].Color = ConfigEntries.PT17C.Value;
						DataManager.inst.PrefabTypes[18].Color = ConfigEntries.PT18C.Value;
						DataManager.inst.PrefabTypes[19].Color = ConfigEntries.PT19C.Value;
					}

					//Internal Config
					{
						internalPrefab.GetComponent<RectTransform>().anchoredPosition = ConfigEntries.PrefabINANCH.Value;
						internalPrefab.GetComponent<RectTransform>().sizeDelta = ConfigEntries.PrefabINSD.Value;
						inPMCGridLay.spacing = ConfigEntries.PrefabINCellSpacing.Value;
						inPMCGridLay.cellSize = ConfigEntries.PrefabINCellSize.Value;
						inPMCGridLay.constraint = ConfigEntries.PrefabINConstraint.Value;
						inPMCGridLay.constraintCount = ConfigEntries.PrefabINConstraintColumns.Value;
						inPMCGridLay.startAxis = ConfigEntries.PrefabINAxis.Value;
						internalPrefab.GetComponent<ScrollRect>().horizontal = ConfigEntries.PrefabINHScroll.Value;
					}

					//External Config
					{
						externalPrefab.GetComponent<RectTransform>().anchoredPosition = ConfigEntries.PrefabEXANCH.Value;
						externalPrefab.GetComponent<RectTransform>().sizeDelta = ConfigEntries.PrefabEXSD.Value;
						exPMCGridLay.spacing = ConfigEntries.PrefabEXCellSpacing.Value;
						exPMCGridLay.cellSize = ConfigEntries.PrefabEXCellSize.Value;
						exPMCGridLay.constraint = ConfigEntries.PrefabEXConstraint.Value;
						exPMCGridLay.startAxis = ConfigEntries.PrefabEXAxis.Value;
						exPMCGridLay.constraintCount = ConfigEntries.PrefabEXConstraintColumns.Value;

						externalPrefab.GetComponent<ScrollRect>().horizontal = ConfigEntries.PrefabEXHScroll.Value;

						externalPrefab.transform.Find("prefabs path").GetComponent<RectTransform>().anchoredPosition = ConfigEntries.PrefabEXPathPos.Value;
						externalPrefab.transform.Find("prefabs path").GetComponent<RectTransform>().sizeDelta = new Vector2(ConfigEntries.PrefabEXPathSca.Value, 34f);

						externalPrefab.transform.Find("reload prefabs").GetComponent<RectTransform>().anchoredPosition = ConfigEntries.PrefabEXRefreshPos.Value;
					}

					//Update Buttons
					{
						foreach (object obj in PrefabEditorPatch.internalContent.transform)
						{
							Transform child = (Transform)obj;

							if (child.GetComponent<HoverUI>())
							{
								var hoverUI = child.GetComponent<HoverUI>();
								hoverUI.size = ConfigEntries.SizeUpper.Value;
							}

							if (child.Find("name") && child.Find("type-name") && child.Find("delete"))
							{
								var name = child.Find("name").GetComponent<Text>();
								var typeName = child.Find("type-name").GetComponent<Text>();
								var deleteRT = child.Find("delete").GetComponent<RectTransform>();

								//Name Text
								{
									name.horizontalOverflow = ConfigEntries.PrefabINNameHOverflow.Value;
									name.verticalOverflow = ConfigEntries.PrefabINNameVOverflow.Value;
									name.fontSize = ConfigEntries.PrefabINNameFontSize.Value;
								}
								//Type Text
								{
									typeName.horizontalOverflow = ConfigEntries.PrefabINTypeHOverflow.Value;
									typeName.verticalOverflow = ConfigEntries.PrefabINTypeVOverflow.Value;
									typeName.fontSize = ConfigEntries.PrefabINTypeFontSize.Value;
								}

								deleteRT.anchoredPosition = ConfigEntries.PrefabINLDeletePos.Value;
								deleteRT.sizeDelta = ConfigEntries.PrefabINLDeleteSca.Value;
							}
						}

						foreach (object obj in PrefabEditorPatch.externalContent.transform)
						{
							Transform child = (Transform)obj;

							if (child.GetComponent<HoverUI>())
							{
								var hoverUI = child.GetComponent<HoverUI>();
								hoverUI.size = ConfigEntries.SizeUpper.Value;
							}

							if (child.Find("name") && child.Find("type-name") && child.Find("delete"))
							{
								var name = child.Find("name").GetComponent<Text>();
								var typeName = child.Find("type-name").GetComponent<Text>();
								var deleteRT = child.Find("delete").GetComponent<RectTransform>();

								//Name Text
								{
									name.horizontalOverflow = ConfigEntries.PrefabEXNameHOverflow.Value;
									name.verticalOverflow = ConfigEntries.PrefabEXNameVOverflow.Value;
									name.fontSize = ConfigEntries.PrefabEXNameFontSize.Value;
								}
								//Type Text
								{
									typeName.horizontalOverflow = ConfigEntries.PrefabEXTypeHOverflow.Value;
									typeName.verticalOverflow = ConfigEntries.PrefabEXTypeVOverflow.Value;
									typeName.fontSize = ConfigEntries.PrefabEXTypeFontSize.Value;
								}

								deleteRT.anchoredPosition = ConfigEntries.PrefabEXLDeletePos.Value;
								deleteRT.sizeDelta = ConfigEntries.PrefabEXLDeleteSca.Value;
							}
						}
					}
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

		public static string ColorToHex(Color32 color)
		{
			return color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2") + color.a.ToString("X2");
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

					if (EditorManager.inst != null && EditorManager.inst.isEditing == true && gameObjectRef.mat && gameObjectRef.obj.GetComponentInChildren<RTObject>() && gameObjectRef.obj.GetComponentInChildren<RTObject>().selected == true && ConfigEntries.HighlightObjects.Value == true)
					{
						if (Input.GetKey(KeyCode.LeftShift))
						{
							Color colorHover = new Color(ConfigEntries.HighlightDoubleColor.Value.r, ConfigEntries.HighlightDoubleColor.Value.g, ConfigEntries.HighlightDoubleColor.Value.b);

							if (gameObjectRef.mat.color.r > 0.9f && gameObjectRef.mat.color.g > 0.9f && gameObjectRef.mat.color.b > 0.9f)
							{
								colorHover = new Color(-ConfigEntries.HighlightDoubleColor.Value.r, -ConfigEntries.HighlightDoubleColor.Value.g, -ConfigEntries.HighlightDoubleColor.Value.b);
							}

							gameObjectRef.mat.color += new Color(colorHover.r, colorHover.g, colorHover.b, 0f);
						}
						else
						{
							Color colorHover = new Color(ConfigEntries.HighlightColor.Value.r, ConfigEntries.HighlightColor.Value.g, ConfigEntries.HighlightColor.Value.b);

							if (gameObjectRef.mat.color.r > 0.95f && gameObjectRef.mat.color.g > 0.95f && gameObjectRef.mat.color.b > 0.95f)
							{
								colorHover = new Color(-ConfigEntries.HighlightColor.Value.r, -ConfigEntries.HighlightColor.Value.g, -ConfigEntries.HighlightColor.Value.b);
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

				GameObject barButton = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/time").transform.GetChild(4).gameObject;
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
					multiTextSync.transform.GetChild(0).gameObject.GetComponent<Text>().text = "Sync to specific object";
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
						EditorManager.inst.ShowDialog("Object Search Popup");
						RTEditor.RefreshObjectSearch(true, "startTime", true, true);
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
						RTEditor.objectData = RTEditor.ObjectData.N;
						EditorManager.inst.ShowDialog("Object Search Popup");
						RTEditor.RefreshObjectSearch(true, "name", true, false);
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
						RTEditor.objectData = RTEditor.ObjectData.OT;
						EditorManager.inst.ShowDialog("Object Search Popup");
						RTEditor.RefreshObjectSearch(true, "objectType", true, true);
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
						RTEditor.objectData = RTEditor.ObjectData.AKT;
						EditorManager.inst.ShowDialog("Object Search Popup");
						RTEditor.RefreshObjectSearch(true, "autoKillType", true, true);
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
						RTEditor.objectData = RTEditor.ObjectData.AKO;
						EditorManager.inst.ShowDialog("Object Search Popup");
						RTEditor.RefreshObjectSearch(true, "autoKillOffset", true, true);
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
						RTEditor.objectData = RTEditor.ObjectData.P;
						EditorManager.inst.ShowDialog("Object Search Popup");
						RTEditor.RefreshObjectSearch(true, "parent", false, true);
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
						RTEditor.objectData = RTEditor.ObjectData.PT;
						EditorManager.inst.ShowDialog("Object Search Popup");
						RTEditor.RefreshObjectSearch(true, "parentType", false, true);
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
						RTEditor.objectData = RTEditor.ObjectData.PO;
						EditorManager.inst.ShowDialog("Object Search Popup");
						RTEditor.RefreshObjectSearch(true, "parentOffset", false, true);
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
						RTEditor.objectData = RTEditor.ObjectData.O;
						EditorManager.inst.ShowDialog("Object Search Popup");
						RTEditor.RefreshObjectSearch(true, "origin", false, true);
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
						RTEditor.objectData = RTEditor.ObjectData.S;
						EditorManager.inst.ShowDialog("Object Search Popup");
						RTEditor.RefreshObjectSearch(true, "shape", false, true);
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
						RTEditor.objectData = RTEditor.ObjectData.T;
						EditorManager.inst.ShowDialog("Object Search Popup");
						RTEditor.RefreshObjectSearch(true, "text", false, true);
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
						RTEditor.objectData = RTEditor.ObjectData.D;
						EditorManager.inst.ShowDialog("Object Search Popup");
						RTEditor.RefreshObjectSearch(true, "depth", false, true);
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
			EditorManager.inst.InvokeRepeating("CreateGrid", ConfigEntries.ReminderRepeat.Value, ConfigEntries.ReminderRepeat.Value);
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
			if (ConfigEntries.GenerateWaveform.Value == true)
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
			RTEditor.inst.InvokeRepeating("AutoSaveLevel", ConfigEntries.AutoSaveRepeat.Value, ConfigEntries.AutoSaveRepeat.Value);
		}

		public static void RenderBeatmapSet()
		{
			int foldClamp = ConfigEntries.FButtonFoldClamp.Value;
			int songClamp = ConfigEntries.FButtonSongClamp.Value;
			int artiClamp = ConfigEntries.FButtonArtiClamp.Value;
			int creaClamp = ConfigEntries.FButtonCreaClamp.Value;
			int descClamp = ConfigEntries.FButtonDescClamp.Value;
			int dateClamp = ConfigEntries.FButtonDateClamp.Value;


			if (ConfigEntries.FButtonFoldClamp.Value < 3)
			{
				foldClamp = 14;
			}

			if (ConfigEntries.FButtonSongClamp.Value < 3)
			{
				songClamp = 22;
			}

			if (ConfigEntries.FButtonArtiClamp.Value < 3)
			{
				artiClamp = 16;
			}

			if (ConfigEntries.FButtonCreaClamp.Value < 3)
			{
				creaClamp = 16;
			}

			if (ConfigEntries.FButtonDescClamp.Value < 3)
			{
				descClamp = 16;
			}

			if (ConfigEntries.FButtonDateClamp.Value < 3)
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

				if (RTFile.FileExists(levelListSlash + metadataWrapper.folder + "/level.ogg"))
				{
					if (EditorManager.inst.openFileSearch == null || !(EditorManager.inst.openFileSearch != "") || name.ToLower().Contains(EditorManager.inst.openFileSearch.ToLower()) || metadata.song.title.ToLower().Contains(EditorManager.inst.openFileSearch.ToLower()) || metadata.artist.Name.ToLower().Contains(EditorManager.inst.openFileSearch.ToLower()) || metadata.creator.steam_name.ToLower().Contains(EditorManager.inst.openFileSearch.ToLower()) || metadata.song.description.ToLower().Contains(EditorManager.inst.openFileSearch.ToLower()) || difficultyName.Contains(EditorManager.inst.openFileSearch.ToLower()))
					{
						GameObject gameObject = Instantiate(EditorManager.inst.folderButtonPrefab);
						gameObject.name = "Folder [" + metadataWrapper.folder + "]";
						gameObject.transform.SetParent(transform);
						gameObject.transform.localScale = Vector3.one;
						var hoverUI = gameObject.AddComponent<HoverUI>();
						hoverUI.size = ConfigEntries.HoverUIOFPSize.Value;
						hoverUI.animatePos = false;
						hoverUI.animateSca = true;
						HoverTooltip htt = gameObject.AddComponent<HoverTooltip>();

						HoverTooltip.Tooltip levelTip = new HoverTooltip.Tooltip();

						if (metadata != null)
						{
							gameObject.transform.GetChild(0).GetComponent<Text>().text = string.Format(ConfigEntries.FButtonFormat.Value, LSText.ClampString(metadataWrapper.folder, foldClamp), LSText.ClampString(metadata.song.title, songClamp), LSText.ClampString(metadata.artist.Name, artiClamp), LSText.ClampString(metadata.creator.steam_name, creaClamp), metadata.song.difficulty, LSText.ClampString(metadata.song.description, descClamp), LSText.ClampString(metadata.beatmap.date_edited, dateClamp));

							if (metadata.song.difficulty == 4 && ConfigEntries.FButtonTextInvert.Value == true && ConfigEntries.FButtonDifColor.Value == true || metadata.song.difficulty == 5 && ConfigEntries.FButtonTextInvert.Value == true && ConfigEntries.FButtonDifColor.Value == true)
							{
								gameObject.transform.GetChild(0).GetComponent<Text>().color = LSColors.ChangeColorBrightness(ConfigEntries.FButtonTextColor.Value, 0.7f);
							}

							Color difficultyColor = Color.white;

							for (int i = 0; i < DataManager.inst.difficulties.Count; i++)
							{
								if (metadata.song.difficulty == i)
								{
									difficultyColor = DataManager.inst.difficulties[i].color;
								}
								if (ConfigEntries.FButtonDifColor.Value == true)
								{
									gameObject.GetComponent<Image>().color = difficultyColor * ConfigEntries.FButtonDifColorMult.Value;
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

						iconRT.anchoredPosition = ConfigEntries.FBIconPos.Value;
						iconRT.sizeDelta = ConfigEntries.FBIconSca.Value;

						iconImage.sprite = metadataWrapper.albumArt;
					}
				}
			}
		}

		public static void LegacyWaveform()
        {
			int num = Mathf.Clamp((int)AudioManager.inst.CurrentAudioSource.clip.length * 48, 100, 15000);
			Sprite waveform = Sprite.Create(GetWaveformTextureAdvanced(AudioManager.inst.CurrentAudioSource.clip, num, 300, ConfigEntries.TimelineBGColor.Value, ConfigEntries.TimelineTopColor.Value, ConfigEntries.TimelineBottomColor.Value), new Rect(0f, 0f, (float)num, 300f), new Vector2(0.5f, 0.5f), 100f);
			EditorManager.inst.timeline.GetComponent<Image>().sprite = waveform;
			EditorManager.inst.timelineWaveformOverlay.GetComponent<Image>().sprite = EditorManager.inst.timeline.GetComponent<Image>().sprite;
		}

		public static void OldWaveform()
		{
			int num = Mathf.Clamp((int)AudioManager.inst.CurrentAudioSource.clip.length * 48, 100, 15000);
			Sprite waveform = Sprite.Create(LSImage.GetWaveformTexture(AudioManager.inst.CurrentAudioSource.clip, num, 300, ConfigEntries.TimelineBGColor.Value, ConfigEntries.TimelineTopColor.Value), new Rect(0f, 0f, (float)num, 300f), new Vector2(0.5f, 0.5f), 100f);
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
				if (ConfigEntries.WaveformMode.Value == WaveformType.Legacy)
				{
					LegacyWaveform();
				}
				if (ConfigEntries.WaveformMode.Value == WaveformType.Old)
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

		//public static void GeneratePassword()
		//      {
		//	var crypt = LSEncryption.EncryptText("{\"name\":\"7th Element\",\"type\":\"8\",\"offset\":\" - 1.03\",\"objects\":[{\"id\":\"B!wlC) + dAZq™&N]▤\",\"pt\":\"111\",\"p\":\" > W0y▼%▉ð ?¤☳i®'c@\",\"d\":\"15\",\"ot\":1,\"st\":\"0\",\"name\":\"custom element\",\"akt\":0,\"ako\":5,\"o\":{\"x\":\"0\",\"y\":\"0\"},\"ed\":{\"bin\":\"0\",\"layer\":\"1\"},\"events\":{\"pos\":[{\"t\":\"0\",\"x\":\"0\",\"y\":\"0\"}],\"sca\":[{\"t\":\"0\",\"x\":\"5\",\"y\":\"3\"}],\"rot\":[{\"t\":\"0\",\"x\":\"0\"}],\"col\":[{\"t\":\"0\",\"x\":\"0\"}]}},{\"id\":\"▧▤▦O▒☳T;l0▆▩9¶▩œ\",\"pt\":\"111\",\"p\":\">W0y▼%▉ð?¤☳i®'c@\",\"d\":\"15\",\"ot\":1,\"st\":\"0.09300041\",\"name\":\"custom element\",\"akt\":0,\"ako\":5,\"o\":{\"x\":\"0\",\"y\":\"0.5\"},\"ed\":{\"bin\":\"0\",\"layer\":\"1\"},\"events\":{\"pos\":[{\"t\":\"0\",\"x\":\"0\",\"y\":\"1.5\"}],\"sca\":[{\"t\":\"0\",\"x\":\"3\",\"y\":\"1\"}],\"rot\":[{\"t\":\"0\",\"x\":\"0\"}],\"col\":[{\"t\":\"0\",\"x\":\"0\"}]}},{\"id\":\"c▆▥☷◄™i▦O6H@{µjr\",\"pt\":\"111\",\"p\":\" > W0y▼%▉ð ?¤☳i®'c@\",\"d\":\"15\",\"ot\":1,\"st\":\"0.1860008\",\"name\":\"custom element\",\"akt\":0,\"ako\":5,\"o\":{\"x\":\"0\",\"y\":\"-0.5\"},\"ed\":{\"bin\":\"0\",\"layer\":\"1\"},\"events\":{\"pos\":[{\"t\":\"0\",\"x\":\"0\",\"y\":\"-1.5\"}],\"sca\":[{\"t\":\"0\",\"x\":\"3\",\"y\":\"1\"}],\"rot\":[{\"t\":\"0\",\"x\":\"0\"}],\"col\":[{\"t\":\"0\",\"x\":\"0\"}]}},{\"id\":\"?E_☲Irv.☰▧▉Zm0xf\",\"pt\":\"111\",\"p\":\">W0y▼%▉ð?¤☳i®'c@\",\"d\":\"15\",\"ot\":1,\"st\":\"0.2780008\",\"name\":\"custom element\",\"shape\":\"1\",\"akt\":0,\"ako\":5,\"so\":\"5\",\"o\":{\"x\":\"0\",\"y\":\"0\"},\"ed\":{\"bin\":\"0\",\"layer\":\"1\"},\"events\":{\"pos\":[{\"t\":\"0\",\"x\":\"1.5\",\"y\":\"1.5\"}],\"sca\":[{\"t\":\"0\",\"x\":\"2\",\"y\":\"2\"}],\"rot\":[{\"t\":\"0\",\"x\":\"0\"}],\"col\":[{\"t\":\"0\",\"x\":\"0\"}]}},{\"id\":\"▬t▼G◠m4☷ð▬!ZÃÿ☳¾\",\"pt\":\"111\",\"p\":\" > W0y▼%▉ð ?¤☳i®'c@\",\"d\":\"15\",\"ot\":1,\"st\":\"0.3710003\",\"name\":\"custom element\",\"shape\":\"1\",\"akt\":0,\"ako\":5,\"so\":\"5\",\"o\":{\"x\":\"0\",\"y\":\"0\"},\"ed\":{\"bin\":\"0\",\"layer\":\"1\"},\"events\":{\"pos\":[{\"t\":\"0\",\"x\":\"1.5\",\"y\":\"-1.5\"}],\"sca\":[{\"t\":\"0\",\"x\":\"2\",\"y\":\"2\"}],\"rot\":[{\"t\":\"0\",\"x\":\"-90\"}],\"col\":[{\"t\":\"0\",\"x\":\"0\"}]}},{\"id\":\"Gt▤V☵]^▦gZOW,?F.\",\"pt\":\"111\",\"p\":\">W0y▼%▉ð?¤☳i®'c@\",\"d\":\"15\",\"ot\":1,\"st\":\"0.4630013\",\"name\":\"custom element\",\"shape\":\"1\",\"akt\":0,\"ako\":5,\"so\":\"5\",\"o\":{\"x\":\"0\",\"y\":\"0\"},\"ed\":{\"bin\":\"0\",\"layer\":\"1\"},\"events\":{\"pos\":[{\"t\":\"0\",\"x\":\" - 1.5\",\"y\":\" - 1.5\"}],\"sca\":[{\"t\":\"0\",\"x\":\"2\",\"y\":\"2\"}],\"rot\":[{\"t\":\"0\",\"x\":\" - 180\"}],\"col\":[{\"t\":\"0\",\"x\":\"0\"}]}},{\"id\":\"¶☴¾kIW{4ZÿU▥D:¥Y\",\"pt\":\"111\",\"p\":\" > W0y▼%▉ð ?¤☳i®'c@\",\"d\":\"15\",\"ot\":1,\"st\":\"0.5560007\",\"name\":\"custom element\",\"shape\":\"1\",\"akt\":0,\"ako\":5,\"so\":\"5\",\"o\":{\"x\":\"0\",\"y\":\"0\"},\"ed\":{\"bin\":\"0\",\"layer\":\"1\"},\"events\":{\"pos\":[{\"t\":\"0\",\"x\":\"-1.5\",\"y\":\"1.5\"}],\"sca\":[{\"t\":\"0\",\"x\":\"2\",\"y\":\"2\"}],\"rot\":[{\"t\":\"0\",\"x\":\"-270\"}],\"col\":[{\"t\":\"0\",\"x\":\"0\"}]}},{\"id\":\"!*5q:■▐?tEXZ☶a5◠\",\"pt\":\"111\",\"p\":\">W0y▼%▉ð?¤☳i®'c@\",\"d\":\"15\",\"ot\":1,\"st\":\"0\",\"name\":\"custom element\",\"shape\":\"1\",\"akt\":0,\"ako\":5,\"o\":{\"x\":\"0\",\"y\":\"0\"},\"ed\":{\"bin\":\"1\",\"layer\":\"1\"},\"events\":{\"pos\":[{\"t\":\"0\",\"x\":\"0\",\"y\":\"1.5\"}],\"sca\":[{\"t\":\"0\",\"x\":\"5\",\"y\":\"0.2\"}],\"rot\":[{\"t\":\"0\",\"x\":\"0\"}],\"col\":[{\"t\":\"0\",\"x\":\"0\"}]}},{\"id\":\"œJf8▩D◠6⁕XM¶Mð8▬\",\"pt\":\"111\",\"p\":\" > W0y▼%▉ð ?¤☳i®'c@\",\"d\":\"15\",\"ot\":1,\"st\":\"0.09300041\",\"name\":\"custom element\",\"shape\":\"1\",\"akt\":0,\"ako\":5,\"o\":{\"x\":\"0\",\"y\":\"0\"},\"ed\":{\"bin\":\"1\",\"layer\":\"1\"},\"events\":{\"pos\":[{\"t\":\"0\",\"x\":\"0\",\"y\":\"-1.5\"}],\"sca\":[{\"t\":\"0\",\"x\":\"5\",\"y\":\"0.2\"}],\"rot\":[{\"t\":\"0\",\"x\":\"0\"}],\"col\":[{\"t\":\"0\",\"x\":\"0\"}]}},{\"id\":\"r4j▢I¥?4_?l^⁕E[v\",\"pt\":\"111\",\"p\":\">W0y▼%▉ð?¤☳i®'c@\",\"d\":\"15\",\"ot\":1,\"st\":\"0.1860008\",\"name\":\"custom element\",\"shape\":\"1\",\"akt\":0,\"ako\":5,\"o\":{\"x\":\"0\",\"y\":\"0\"},\"ed\":{\"bin\":\"1\",\"layer\":\"1\"},\"events\":{\"pos\":[{\"t\":\"0\",\"x\":\"1.5\",\"y\":\"0\"}],\"sca\":[{\"t\":\"0\",\"x\":\"0.2\",\"y\":\"5\"}],\"rot\":[{\"t\":\"0\",\"x\":\"0\"}],\"col\":[{\"t\":\"0\",\"x\":\"0\"}]}},{\"id\":\"][^>$]™0Gµ)B7s▨#\",\"pt\":\"111\",\"p\":\">W0y▼%▉ð?¤☳i®'c@\",\"d\":\"15\",\"ot\":1,\"st\":\"0.2780008\",\"name\":\"custom element\",\"shape\":\"1\",\"akt\":0,\"ako\":5,\"o\":{\"x\":\"0\",\"y\":\"0\"},\"ed\":{\"bin\":\"1\",\"layer\":\"1\"},\"events\":{\"pos\":[{\"t\":\"0\",\"x\":\"-1.5\",\"y\":\"0\"}],\"sca\":[{\"t\":\"0\",\"x\":\"0.2\",\"y\":\"5\"}],\"rot\":[{\"t\":\"0\",\"x\":\"0\"}],\"col\":[{\"t\":\"0\",\"x\":\"0\"}]}},{\"id\":\"|~mL⁕1/s▨|s▉▒QIC\",\"pt\":\"111\",\"p\":\">W0y▼%▉ð?¤☳i®'c@\",\"d\":\"15\",\"ot\":1,\"st\":\"0.3710012\",\"name\":\"custom element\",\"akt\":0,\"ako\":5,\"o\":{\"x\":\"0\",\"y\":\"0\"},\"ed\":{\"bin\":\"1\",\"layer\":\"1\"},\"events\":{\"pos\":[{\"t\":\"0\",\"x\":\"0\",\"y\":\"0\"}],\"sca\":[{\"t\":\"0\",\"x\":\"3\",\"y\":\"3\"}],\"rot\":[{\"t\":\"0\",\"x\":\"0\"}],\"col\":[{\"t\":\"0\",\"x\":\"0\"}]}},{\"id\":\"P¶l(X▢GR⁕▢Ã¶;_LA\",\"pt\":\"111\",\"p\":\"è▒EJ5dsu~I▒P:▢%☵\",\"d\":\"16\",\"ot\":2,\"st\":\"0\",\"name\":\"custom element\",\"akt\":0,\"ako\":5,\"o\":{\"x\":\"0\",\"y\":\"0\"},\"ed\":{\"bin\":\"2\",\"layer\":\"1\"},\"events\":{\"pos\":[{\"t\":\"0\",\"x\":\"-1\",\"y\":\"0\"}],\"sca\":[{\"t\":\"0\",\"x\":\"0.5\",\"y\":\"3\"}],\"rot\":[{\"t\":\"0\",\"x\":\"0\"}],\"col\":[{\"t\":\"0\",\"x\":\"1\"}]}},{\"id\":\"3_▤2C▩/kµ[▥ti◠<▣\",\"pt\":\"111\",\"p\":\"è▒EJ5dsu~I▒P:▢%☵\",\"d\":\"16\",\"ot\":2,\"st\":\"0.09300137\",\"name\":\"custom element\",\"akt\":0,\"ako\":5,\"o\":{\"x\":\"0.5\",\"y\":\"-0.5\"},\"ed\":{\"bin\":\"2\",\"layer\":\"1\"},\"events\":{\"pos\":[{\"t\":\"0\",\"x\":\"-1\",\"y\":\"-1\"}],\"sca\":[{\"t\":\"0\",\"x\":\"2.5\",\"y\":\"0.5\"}],\"rot\":[{\"t\":\"0\",\"x\":\"0\"}],\"col\":[{\"t\":\"0\",\"x\":\"1\"}]}},{\"id\":\"K◄☳▬▓w!VEfr▬<f6_\",\"pt\":\"111\",\"p\":\"è▒EJ5dsu~I▒P:▢%☵\",\"d\":\"16\",\"ot\":2,\"st\":\"0.1850014\",\"name\":\"custom element\",\"akt\":0,\"ako\":5,\"o\":{\"x\":\"0.5\",\"y\":\"0.5\"},\"ed\":{\"bin\":\"2\",\"layer\":\"1\"},\"events\":{\"pos\":[{\"t\":\"0\",\"x\":\"-1\",\"y\":\"1\"}],\"sca\":[{\"t\":\"0\",\"x\":\"2.5\",\"y\":\"0.5\"}],\"rot\":[{\"t\":\"0\",\"x\":\"0\"}],\"col\":[{\"t\":\"0\",\"x\":\"1\"}]}},{\"id\":\">W0y▼%▉ð?¤☳i®'c@\",\"p\":\"\",\"d\":\"15\",\"ot\":3,\"st\":\"1.040001\",\"name\":\"7th Element Case\",\"akt\":2,\"ako\":5,\"o\":{\"x\":\"0\",\"y\":\"0\"},\"ed\":{\"bin\":\"0\",\"layer\":\"1\"},\"events\":{\"pos\":[{\"t\":\"0\",\"x\":\"0\",\"y\":\"-11\"}],\"sca\":[{\"t\":\"0\",\"x\":\"1\",\"y\":\"1\"}],\"rot\":[{\"t\":\"0\",\"x\":\"-5\"}],\"col\":[{\"t\":\"0\",\"x\":\"0\"}]}},{\"id\":\"è▒EJ5dsu~I▒P:▢%☵\",\"pt\":\"111\",\"p\":\">W0y▼%▉ð?¤☳i®'c@\",\"d\":\"15\",\"ot\":3,\"st\":\"1.040001\",\"name\":\"7th Element\",\"akt\":2,\"ako\":5,\"o\":{\"x\":\"0\",\"y\":\"0\"},\"ed\":{\"bin\":\"1\",\"layer\":\"1\"},\"events\":{\"pos\":[{\"t\":\"0\",\"x\":\"0\",\"y\":\"0\"}],\"sca\":[{\"t\":\"0\",\"x\":\"1\",\"y\":\"1\"}],\"rot\":[{\"t\":\"0\",\"x\":\"15\"}],\"col\":[{\"t\":\"0\",\"x\":\"0\"}]}}]}", "lol");
		//	byte[] vb = new byte[crypt.Length];
		//	foreach (var c in crypt)
		//          {
		//		vb.AddItem((byte)c);
		//          }

		//	password = vb;
		//      }

		//public static byte[] password = LSEncryption.AES_Encrypt(new byte[] { 9, 5, 7, 6, 4, 38, 6, 4, 3, 66, 43, 6, 47, 8, 54, 6 }, new byte[] { 99, 53, 43, 36, 43, 65, 43, 45 });

		//public static IEnumerator DecryptLevel(string _filepath, Action<AudioClip> callback)
		//{
		//	Debug.LogFormat("{0}Loading song...", className);
		//	string songPath = _filepath + "song.lsen";
		//	var songBytes = File.ReadAllBytes(songPath);

		//	Debug.LogFormat("{0}Decrypting song...", className);
		//	var decryptedSong = LSEncryption.AES_Decrypt(songBytes, password);

		//	File.WriteAllBytes(_filepath + "encryptedsong.ogg", decryptedSong);

		//	Debug.LogFormat("{0}Writing song to " + _filepath + "encryptedsong.ogg", className);
		//	AudioClip clipper = new();

		//	FileManager.inst.StartCoroutine(FileManager.inst.LoadMusicFileRaw(_filepath + "encryptedsong.ogg", false, delegate (AudioClip audioClip)
		//	{
		//		clipper = audioClip;
		//	}));

		//	callback(clipper);

		//	yield break;
		//}

		//public static IEnumerator PlayDecryptedLevel(string _path)
		//      {
		//	yield return inst.StartCoroutine(DecryptLevel(_path, delegate (AudioClip audioClip)
		//	{
		//		SaveManager.inst.ArcadeQueue.AudioFileStr = SaveManager.inst.ArcadeQueue.AudioFileStr.Replace("\\level.ogg", "\\encryptedsong.ogg");
		//	}));

		//	Debug.LogFormat("{0}Playing song.lsen from (" + _path + ")", className);

		//	Debug.LogFormat("{0}ArcadeQueue: \n{1}", className, SaveManager.inst.ArcadeQueue.AudioFileStr);
		//	var e = AccessTools.Method(typeof(GameManager), "LoadLevelFromArcadeQueue");
		//	GameManager.inst.StartCoroutine((IEnumerator)e.Invoke(GameManager.inst, new object[] { SaveManager.inst.ArcadeQueue }));

		//	yield return new WaitForSeconds(2f);

		//	File.Delete(_path + "encryptedsong.ogg");
		//}

		[HarmonyPatch(typeof(EditorManager), "Update")]
		[HarmonyPostfix]
		private static void NewQuickPrefabBindings()
		{
			if (Input.GetKeyDown(ConfigEntries.PQCKey0.Value) && ConfigEntries.PQCActive0.Value == true)
			{
				PrefabEditor.inst.AddPrefabObjectToLevel(DataManager.inst.gameData.prefabs[ConfigEntries.PQCIndex0.Value]);
			}
			if (Input.GetKeyDown(ConfigEntries.PQCKey1.Value) && ConfigEntries.PQCActive1.Value == true)
			{
				PrefabEditor.inst.AddPrefabObjectToLevel(DataManager.inst.gameData.prefabs[ConfigEntries.PQCIndex1.Value]);
			}
			if (Input.GetKeyDown(ConfigEntries.PQCKey2.Value) && ConfigEntries.PQCActive2.Value == true)
			{
				PrefabEditor.inst.AddPrefabObjectToLevel(DataManager.inst.gameData.prefabs[ConfigEntries.PQCIndex2.Value]);
			}
			if (Input.GetKeyDown(ConfigEntries.PQCKey3.Value) && ConfigEntries.PQCActive3.Value == true)
			{
				PrefabEditor.inst.AddPrefabObjectToLevel(DataManager.inst.gameData.prefabs[ConfigEntries.PQCIndex3.Value]);
			}
			if (Input.GetKeyDown(ConfigEntries.PQCKey4.Value) && ConfigEntries.PQCActive4.Value == true)
			{
				PrefabEditor.inst.AddPrefabObjectToLevel(DataManager.inst.gameData.prefabs[ConfigEntries.PQCIndex4.Value]);
			}
		}

		[HarmonyPatch(typeof(ColorPicker), "UpdateValues")]
		[HarmonyPrefix]
		private static bool UpdateValues(ColorPicker __instance, ref Color __0)
		{
			double num;
			double num2;
			double num3;
			LSColors.ColorToHSV(__0, out num, out num2, out num3);
			string text = RTEditor.ColorToHex(__0);
			InputField RInput = __instance.rgb.transform.Find("R/input").GetComponent<InputField>();
			InputField GInput = __instance.rgb.transform.Find("G/input").GetComponent<InputField>();
			InputField BInput = __instance.rgb.transform.Find("B/input").GetComponent<InputField>();
			InputField HInput = __instance.hsv.transform.Find("H/input").GetComponent<InputField>();
			InputField SInput = __instance.hsv.transform.Find("S/input").GetComponent<InputField>();
			InputField VInput = __instance.hsv.transform.Find("V/input").GetComponent<InputField>();
			RInput.onValueChanged.RemoveAllListeners();
			RInput.text = (__0.r * 255f).ToString();
			RInput.onValueChanged.AddListener(delegate (string _val)
			{
				__instance.SwitchCurrentColor(new Color((float)int.Parse(_val) / 255f, (float)int.Parse(GInput.text) / 255f, (float)int.Parse(BInput.text) / 255f, 255f));
			});
			GInput.onValueChanged.RemoveAllListeners();
			GInput.text = (__0.g * 255f).ToString();
			GInput.onValueChanged.AddListener(delegate (string _val)
			{
				__instance.SwitchCurrentColor(new Color((float)int.Parse(RInput.text) / 255f, (float)int.Parse(_val) / 255f, (float)int.Parse(BInput.text) / 255f, 255f));
			});
			BInput.onValueChanged.RemoveAllListeners();
			BInput.text = (__0.b * 255f).ToString();
			BInput.onValueChanged.AddListener(delegate (string _val)
			{
				__instance.SwitchCurrentColor(new Color((float)int.Parse(RInput.text) / 255f, (float)int.Parse(GInput.text) / 255f, (float)int.Parse(_val) / 255f, 255f));
			});
			HInput.onValueChanged.RemoveAllListeners();
			HInput.text = Mathf.RoundToInt((float)num).ToString();
			HInput.onValueChanged.AddListener(delegate (string _val)
			{
				__instance.SwitchCurrentColor(LSColors.ColorFromHSV(double.Parse(_val), double.Parse(SInput.text) / 100.0, double.Parse(VInput.text) / 100.0));
			});
			SInput.onValueChanged.RemoveAllListeners();
			SInput.text = Mathf.RoundToInt((float)num2 * 100f).ToString();
			SInput.onValueChanged.AddListener(delegate (string _val)
			{
				__instance.SwitchCurrentColor(LSColors.ColorFromHSV(double.Parse(HInput.text), double.Parse(_val) / 100.0, double.Parse(VInput.text) / 100.0));
			});
			VInput.onValueChanged.RemoveAllListeners();
			VInput.text = Mathf.RoundToInt((float)num3 * 100f).ToString();
			VInput.onValueChanged.AddListener(delegate (string _val)
			{
				__instance.SwitchCurrentColor(LSColors.ColorFromHSV(double.Parse(HInput.text), double.Parse(SInput.text) / 100.0, double.Parse(_val) / 100.0));
			});
			__instance.hex.GetComponent<InputField>().onValueChanged.RemoveAllListeners();
			__instance.hex.GetComponent<InputField>().text = text;
			__instance.hex.GetComponent<InputField>().onValueChanged.AddListener(delegate (string _val)
			{
				__instance.SwitchCurrentColor(LSColors.HexToColor(_val));
			});
			__instance.currentHex = text;
			__instance.preview.GetComponent<Image>().color = __0;
			__instance.currentColor = __0;

			return false;
		}

		public void StartReload()
		{
			StartCoroutine(UpdatePrefabs());
		}

		public static IEnumerator UpdatePrefabs()
		{
			PrefabEditor.inst.LoadedPrefabs.Clear();
			PrefabEditor.inst.LoadedPrefabsFiles.Clear();
			yield return PrefabEditor.inst.StartCoroutine(PrefabEditor.inst.LoadExternalPrefabs());
			PrefabEditor.inst.ReloadExternalPrefabsInPopup(false);
			EditorManager.inst.DisplayNotification("Updated external prefabs!", 2f, EditorManager.NotificationType.Success, false);
			yield break;
		}

		public static IEnumerator LoadExternalPrefabs()
		{
			float delay = 0f;
			List<FileManager.LSFile> folders = FileManager.inst.GetFileList("beatmaps/prefabs", "lsp");
			while (folders.Count <= 0)
				yield return null;
			foreach (FileManager.LSFile lsFile in folders)
			{
				yield return new WaitForSeconds(delay);
				JSONNode jsonNode = JSON.Parse(FileManager.inst.LoadJSONFileRaw(lsFile.FullPath));
				List<DataManager.GameData.BeatmapObject> _objects = new List<DataManager.GameData.BeatmapObject>();
				for (int aIndex = 0; aIndex < jsonNode["objects"].Count; ++aIndex)
					_objects.Add(DataManager.GameData.BeatmapObject.ParseGameObject(jsonNode["objects"][aIndex]));
				List<DataManager.GameData.PrefabObject> _prefabObjects = new List<DataManager.GameData.PrefabObject>();
				for (int aIndex = 0; aIndex < jsonNode["prefab_objects"].Count; ++aIndex)
					_prefabObjects.Add(DataManager.inst.gameData.ParsePrefabObject(jsonNode["prefab_objects"][aIndex]));
				PrefabEditor.inst.LoadedPrefabs.Add(new DataManager.GameData.Prefab(jsonNode["name"], jsonNode["type"].AsInt, jsonNode["offset"].AsFloat, _objects, _prefabObjects));
				PrefabEditor.inst.LoadedPrefabsFiles.Add(lsFile.FullPath);
				delay += 0.0001f;
			}
		}

		[HarmonyPatch(typeof(EditorManager), "Start")]
		[HarmonyPrefix]
		private static void DeleteComp()
		{
			if (GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/PrefabDialog/data/type/types").GetComponent<VerticalLayoutGroup>())
			{
				Destroy(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/PrefabDialog/data/type/types").GetComponent<VerticalLayoutGroup>());
			}
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

			if (ConfigEntries.SavingUpdatesTime.Value == true)
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

		private float[] ConvertByteToFloat(byte[] array)
		{
			float[] floatArr = new float[array.Length / 4];
			for (int i = 0; i < floatArr.Length; i++)
			{
				if (BitConverter.IsLittleEndian)
					Array.Reverse(array, i * 4, 4);
				floatArr[i] = BitConverter.ToSingle(array, i * 4) / 0x80000000;
			}
			return floatArr;
		}


		private byte[] ConvertFloatToByte(float[] array)
		{
			byte[] byteArr = new byte[array.Length * 4];
			for (int i = 0; i < array.Length; i++)
			{
				var bytes = BitConverter.GetBytes(array[i] * 0x80000000);
				Array.Copy(bytes, 0, byteArr, i * 4, bytes.Length);
				if (BitConverter.IsLittleEndian)
					Array.Reverse(byteArr, i * 4, 4);
			}
			return byteArr;
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
				ConfigEntries.MarkerColN0.Value,
				ConfigEntries.MarkerColN1.Value,
				ConfigEntries.MarkerColN2.Value,
				ConfigEntries.MarkerColN3.Value,
				ConfigEntries.MarkerColN4.Value,
				ConfigEntries.MarkerColN5.Value,
				ConfigEntries.MarkerColN6.Value,
				ConfigEntries.MarkerColN7.Value,
				ConfigEntries.MarkerColN8.Value,
			};
		}

		[HarmonyPatch(typeof(Debug), "Log", new Type[] { typeof(object) })]
		[HarmonyPostfix]
		private static void LogNotifications(object __0)
        {
			if (EditorManager.inst != null && ConfigEntries.EditorDebug.Value == true)
            {
				string str = __0.ToString();
				str = str.Replace(@"\n", "<br>");
				RTEditor.DisplayNotification(__0.ToString(), str, 2f, EditorManager.NotificationType.Success);
            }
        }

		[HarmonyPatch(typeof(Debug), "LogFormat", typeof(string), typeof(object[]))]
		[HarmonyPostfix]
		private static void LogNotifications(string __0, params object[] __1)
        {
			if (EditorManager.inst != null && ConfigEntries.EditorDebug.Value == true)
            {
				string str = string.Format(__0, __1);
				RTEditor.DisplayNotification(__0, str, 2f, EditorManager.NotificationType.Success);
            }
        }

		[HarmonyPatch(typeof(Debug), "LogError", new Type[] { typeof(object) })]
		[HarmonyPostfix]
		private static void LogErrorNotifications(object __0)
        {
			if (EditorManager.inst != null && ConfigEntries.EditorDebug.Value == true)
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
			if (EditorManager.inst != null && ConfigEntries.EditorDebug.Value == true)
            {
				string str = __0.ToString();
				str = str.Replace(@"\n", "<br>");
				RTEditor.DisplayNotification(__0.ToString(), str, 2f, EditorManager.NotificationType.Warning);
            }
        }
	}
}
