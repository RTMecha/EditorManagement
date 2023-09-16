using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using BepInEx;
using BepInEx.Configuration;

using HarmonyLib;

using LSFunctions;
using RTFunctions.Functions;
using SimpleJSON;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using EditorManagement.Functions.Editors;
using EditorManagement.Functions.Components;
using EditorManagement.Functions;
using EditorManagement.Functions.Tools;
using EditorManagement.Patchers;

using RTFunctions.Functions.Components;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;

namespace EditorManagement
{
    [BepInPlugin("com.mecha.editormanagement", "Editor Management", " 1.10.2")]
	[BepInProcess("Project Arrhythmia.exe")]
	//[BepInIncompatibility("com.mecha.renderdepthunlimited")]
	//[BepInIncompatibility("com.mecha.originoffset")]
	//[BepInIncompatibility("com.mecha.cursorcolor")]
	//[BepInIncompatibility("com.mecha.noautokillselectable")]
	//[BepInIncompatibility("com.mecha.eventsplus")]
	//[BepInIncompatibility("com.mecha.newthemesystems")]
	//[BepInIncompatibility("com.mecha.prefabadditions")]
	[BepInDependency("com.mecha.rtfunctions")]
	public class EditorPlugin : BaseUnityPlugin
	{
		//TODO
		//Clean up some code, optimize and bug fix.
		//Fix the prefab search bug. (Kinda fixed?)
		//Work on a prefab keybind thing. (Later)
		//Add randomization accessibility to Prefab Object dialog since prefab objects can use randomization.
		//Fix Object Dragger so it creates a keyframe at the current audio time, interpolates the value between the previous and next keyframes and sets the new keyframe's value to that.

		//Update list

		public static EditorPlugin inst;
		public static string className = "[<color=#F6AC1A>Editor</color><color=#2FCBD6>Management</color>] " + PluginInfo.PLUGIN_VERSION + "\n";
		private readonly Harmony harmony = new Harmony("Editor");

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

		public static List<int> allLayers = new List<int>();

		public static RTEditor editor;

		public static bool createInternal = true;

		public static List<SaveManager.ArcadeLevel> arcadeQueue = new List<SaveManager.ArcadeLevel>();
		public static int current;

		public static Type catalyst;
		public static int catInstalled = 0;

		public static List<ConfigEntry<Color>> markerColors;

		public static List<DataManager.PrefabType> defaultPrefabTypes = new List<DataManager.PrefabType>
		{
			new DataManager.PrefabType
			{
				Color = LSColors.HexToColorAlpha("E91E63FF"),
				Name = "Bombs"
			},
			new DataManager.PrefabType
			{
				Color = LSColors.HexToColorAlpha("9C27B0FF"),
				Name = "Bullets"
			},
			new DataManager.PrefabType
			{
				Color = LSColors.HexToColorAlpha("3F51B5FF"),
				Name = "Beams"
			},
			new DataManager.PrefabType
			{
				Color = LSColors.HexToColorAlpha("03A9F4FF"),
				Name = "Spinners"
			},
			new DataManager.PrefabType
			{
				Color = LSColors.HexToColorAlpha("009688FF"),
				Name = "Pulses"
			},
			new DataManager.PrefabType
			{
				Color = LSColors.HexToColorAlpha("8BC34AFF"),
				Name = "Characters"
			},
			new DataManager.PrefabType
			{
				Color = LSColors.HexToColorAlpha("FFEB3BFF"),
				Name = "Misc 1"
			},
			new DataManager.PrefabType
			{
				Color = LSColors.HexToColorAlpha("FF9800FF"),
				Name = "Misc 2"
			},
			new DataManager.PrefabType
			{
				Color = LSColors.HexToColorAlpha("FF5722FF"),
				Name = "Misc 3"
			},
			new DataManager.PrefabType
			{
				Color = LSColors.HexToColorAlpha("FF1D22FF"),
				Name = "Misc 4"
			},
		};

		private void Awake()
		{
			inst = this;

			Logger.LogInfo("Plugin Editor Management is loaded!");

			var fontLimit = new AcceptableValueRange<int>(1, 40);

			//General
			{
				ConfigEntries.EditorDebug = Config.Bind("General", "Debug", false, "If enabled, specific debugging functions for the editor will be enabled.");
				ConfigEntries.ReminderActive = Config.Bind("General", "Reminder Active", true, "Will enable the reminder to tell you to have a break.");
				ConfigEntries.ReminderLoopTime = Config.Bind("General", "Reminder Loop Time", 600f, "The time between each reminder.");

				ConfigEntries.BPMSnapsKeyframes = Config.Bind("General", "BPM Snaps Keyframes", false, "Makes object's keyframes snap if Snap BPM is enabled.");
				ConfigEntries.BPMSnapDivisions = Config.Bind("General", "BPM Snap Divisions", 4f, "How many times the snap is divided into. Can be good for songs that don't do 4 divisions.");

				ConfigEntries.EditorPropertiesKey = Config.Bind("General", "Preferences Open Key", KeyCode.F10, "The key to press to open the Editor Properties / Preferences window.");
				ConfigEntries.OpenPlayerEditor = Config.Bind("General", "Player Editor Open Key", KeyCode.F6, "The key to press to open the Player Editor window.");

				ConfigEntries.PrefabExampleTemplate = Config.Bind("General", "Prefab Example Template", true, "Example Template prefab will always be generated into the internal prefabs for you to use.");

				ConfigEntries.PasteOffset = Config.Bind("General", "Paste Offset", false, "When enabled objects that are pasted will be pasted at an offset based on the distance between the audio time and the copied object. Otherwise, the objects will be pasted at the earliest objects start time.");
			}

            //Timeline
            {
				ConfigEntries.DraggingMainCursorPausesLevel = Config.Bind("Timeline", "Dragging main Cursor Pauses Level", true, "If dragging the cursor pauses the level.");

				ConfigEntries.MainTimelineSliderColor = Config.Bind("Timeline", "Timeline Cursor Color", new Color(0.251f, 0.4627f, 0.8745f, 1f), "Color of the main timeline cursor.");
				ConfigEntries.KeyframeTimelineSliderColor = Config.Bind("Timeline", "Keyframe Cursor Color", new Color(0.251f, 0.4627f, 0.8745f, 1f), "Color of the object timeline cursor.");
				ConfigEntries.ObjectSelectionColor = Config.Bind("Timeline", "Object Selection Color", new Color(0.251f, 0.4627f, 0.8745f, 1f), "Color of selected objects.");

				prevMainTimelineColor = ConfigEntries.MainTimelineSliderColor.Value;
				prevKeyframeTimelineColor = ConfigEntries.KeyframeTimelineSliderColor.Value;
				prevSelectedObjectsColor = ConfigEntries.ObjectSelectionColor.Value;

				ConfigEntries.MainZoomBounds = Config.Bind("Timeline", "Main Zoom Bounds", new Vector2(16f, 512f), "The limits of the main timeline zoom.");
				ConfigEntries.KeyframeZoomBounds = Config.Bind("Timeline", "Keyframe Zoom Bounds", new Vector2(1f, 512f), "The limits of the keyframe timeline zoom.");
				ConfigEntries.MainZoomAmount = Config.Bind("Timeline", "Main Zoom Amount", 0.05f, "Sets the zoom in & out amount for the main timeline.");
				ConfigEntries.KeyframeZoomAmount = Config.Bind("Timeline", "Keyframe Zoom Amount", 0.05f, "Sets the zoom in & out amount for the keyframe timeline.");

				lastMainZoomBounds = ConfigEntries.MainZoomBounds.Value;
				lastKeyframeZoomBounds = ConfigEntries.KeyframeZoomBounds.Value;

				ConfigEntries.GenerateWaveform = Config.Bind("Timeline", "Waveform Generate", true, "Allows the timeline waveform to generate. (Waveform might not show on some devices and will increase level load times)");
				ConfigEntries.RenderTimeline = Config.Bind("Timeline", "Waveform Re-render", false, "If the timeline waveform should update when a value is changed.");
				ConfigEntries.WaveformMode = Config.Bind("Timeline", "Waveform Mode", WaveformType.Legacy, "The mode of the timeline waveform.");
				ConfigEntries.WaveformBGColor = Config.Bind("Timeline", "Waveform BG Color", Color.clear, "Color of the background for the waveform.");
				ConfigEntries.WaveformTopColor = Config.Bind("Timeline", "Waveform Top Color", LSColors.red300, "If waveform mode is Legacy, this will be the top color. Otherwise, it will be the base color.");
				ConfigEntries.WaveformBottomColor = Config.Bind("Timeline", "Waveform Bottom Color", LSColors.blue300, "If waveform is Legacy, this will be the bottom color. Otherwise, it will be unused.");

				lastWaveformType = ConfigEntries.WaveformMode.Value;
				lastWaveformBGColor = ConfigEntries.WaveformBGColor.Value;
				lastWaveformTopColor = ConfigEntries.WaveformTopColor.Value;
				lastWaveformBottomColor = ConfigEntries.WaveformBottomColor.Value;

				ConfigEntries.MarkerLoop = Config.Bind("Timeline", "Marker Loop Active", false, "If the marker should loop between markers.");
				ConfigEntries.MarkerStartIndex = Config.Bind("Timeline", "Marker Loop Begin", 0, "Audio time gets set to this marker.");
				ConfigEntries.MarkerEndIndex = Config.Bind("Timeline", "Marker Loop End", 1, "If the audio time gets to the set marker time, it will loop to the beginning marker.");
			}

			//Data
			{
				ConfigEntries.AutoSaveLimit = Config.Bind("Data", "Autosave Limit", 3, "If autosave count reaches this number, delete the first autosave.");
				ConfigEntries.AutoSaveLoopTime = Config.Bind("Data", "Autosave Loop Time", 600f, "The repeat time of autosave.");
				ConfigEntries.SavingUpdatesTime = Config.Bind("Data", "Saving Updates Edited Date", false, "Enabling this will save date_edited in metadata.lsb as the most recent.");
				ConfigEntries.LevelLoadsSavedTime = Config.Bind("Data", "Level Loads Last Time", true, "Sets the editor position (audio time, layer, etc) to the last saved editor position on level load.");
				ConfigEntries.LevelPausesOnStart = Config.Bind("Data", "Level Pauses on Start", false, "Editor pauses on level load.");
			}

			//Editor GUI
			{
				var hoverRange = new AcceptableValueRange<float>(0.7f, 1.4f);

				ConfigEntries.DragUI = Config.Bind("Editor GUI", "Drag UI", false, "Specific UI popups can be dragged around (such as the parent selector, etc).");
				ConfigEntries.HoverSoundsEnabled = Config.Bind("Editor GUI", "Hover UI Play Sound", false, "Plays a sound when the hover UI element is hovered over.");
				ConfigEntries.NotificationWidth = Config.Bind("Editor GUI", "Notification Width", 221f, "Width of the notifications.");
				ConfigEntries.NotificationSize = Config.Bind("Editor GUI", "Notification Size", 1f, "Total size of the notifications.");
				ConfigEntries.NotificationDirection = Config.Bind("Editor GUI", "Notification Direction", Direction.Down, "Direction the notifications popup from.");
				ConfigEntries.DisplayNotifications = Config.Bind("Editor GUI", "Notifications Display", true, "If the notifications should display. Does not include the help box.");

				ConfigEntries.OpenFilePosition = Config.Bind("Editor GUI", "Open Level Position", Vector2.zero, "The position of the Open Level popup.");
				ConfigEntries.OpenFileScale = Config.Bind("Editor GUI", "Open Level Scale", new Vector2(600f, 400f), "The size of the Open Level popup.");
				ConfigEntries.OpenFilePathPos = Config.Bind("Editor GUI", "Open Level Editor Path Pos", new Vector2(275f, 16f), "The position of the editor path input field.");
				ConfigEntries.OpenFilePathLength = Config.Bind("Editor GUI", "Open Level Editor Path Length", 104f, "The length of the editor path input field.");
				ConfigEntries.OpenFileRefreshPosition = Config.Bind("Editor GUI", "Open Level List Refresh Position", new Vector2(330f, 432f), "The position of the refresh button.");
				ConfigEntries.OpenFileTogglePosition = Config.Bind("Editor GUI", "Open Level Toggle Position", new Vector2(600f, 16f), "The position of the descending toggle.");
				ConfigEntries.OpenFileDropdownPosition = Config.Bind("Editor GUI", "Open Level Dropdown Position", new Vector2(501f, 416f), "The position of the sort dropdown.");

				ConfigEntries.OpenFileCellSize = Config.Bind("Editor GUI", "Open Level Cell Size", new Vector2(584f, 32f), "Size of each cell.");
				ConfigEntries.OpenFileCellConstraintType = Config.Bind("Editor GUI", "Open Level Cell Constraint Type", Constraint.FixedColumnCount, "How the cells are layed out.");
				ConfigEntries.OpenFileCellConstraintCount = Config.Bind("Editor GUI", "Open Level Cell Constraint Count", 1, "How many rows / columns there are, depending on Constraint Type.");
				ConfigEntries.OpenFileCellSpacing = Config.Bind("Editor GUI", "Open Level Cell Spacing", new Vector2(0f, 8f), "The space between each cell.");

				ConfigEntries.OpenFileTextHorizontalWrap = Config.Bind("Editor GUI", "Open Level Text Horizontal Wrap", HorizontalWrapMode.Wrap, "Horizontal Wrap Mode of the folder button text.");
				ConfigEntries.OpenFileTextVerticalWrap = Config.Bind("Editor GUI", "Open Level Text Vertical Wrap", VerticalWrapMode.Truncate, "Vertical Wrap Mode of the folder button text.");
				ConfigEntries.OpenFileTextColor = Config.Bind("Editor GUI", "Open Level Text Color", new Color(0.9373f, 0.9216f, 0.9373f, 1f), "Color of the folder button text.");
				ConfigEntries.OpenFileTextInvert = Config.Bind("Editor GUI", "Open Level Text Invert", true, "If the text should invert if the difficulty color is dark.");
				ConfigEntries.OpenFileTextFontSize = Config.Bind("Editor GUI", "Open Level Text Font Size", 20, new ConfigDescription("Font size of the folder button text.", fontLimit));

				ConfigEntries.OpenFileFolderNameMax = Config.Bind("Editor GUI", "Open Level Folder Name Max", 14, "Limited length of the folder name.");
				ConfigEntries.OpenFileSongNameMax = Config.Bind("Editor GUI", "Open Level Song Name Max", 22, "Limited length of the song name.");
				ConfigEntries.OpenFileArtistNameMax = Config.Bind("Editor GUI", "Open Level Artist Name Max", 16, "Limited length of the artist name.");
				ConfigEntries.OpenFileCreatorNameMax = Config.Bind("Editor GUI", "Open Level Creator Name Max", 16, "Limited length of the creator name.");
				ConfigEntries.OpenFileDescriptionMax = Config.Bind("Editor GUI", "Open Level Description Max", 16, "Limited length of the description.");
				ConfigEntries.OpenFileDateMax = Config.Bind("Editor GUI", "Open Level Date Clamp", 16, "Limited length of the date.");
				ConfigEntries.OpenFileTextFormatting = Config.Bind("Editor GUI", "Open Level Text Formatting", ".  /{0} : {1} by {2}", "The way the text is formatted for each level. {0} is folder, {1} is song, {2} is artist, {3} is creator, {4} is difficulty, {5} is description and {6} is last edited.");

				ConfigEntries.OpenFileButtonDifficultyColor = Config.Bind("Editor GUI", "Open Level Button Difficulty Color", false, "If each button matches its associated difficulty color.");
				ConfigEntries.OpenFileButtonDifficultyMultiply = Config.Bind("Editor GUI", "Open Level Button Difficulty Mulity", 1.5f, "How much each buttons' color multiplies by difficulty color.");

				ConfigEntries.OpenFileButtonNormalColor = Config.Bind("Editor GUI", "Open Level Button Normal Color", new Color(0.1647f, 0.1647f, 0.1647f, 1f), "Normal color of the folder button.");
				ConfigEntries.OpenFileButtonHighlightedColor = Config.Bind("Editor GUI", "Open Level Button Highlighted Color", new Color(0.2588f, 0.2588f, 0.2588f, 1f), "Highlighted color of the folder button.");
				ConfigEntries.OpenFileButtonPressedColor = Config.Bind("Editor GUI", "Open Level Button Pressed Color", new Color(0.2588f, 0.2588f, 0.2588f, 1f), "Pressed color of the folder button.");
				ConfigEntries.OpenFileButtonSelectedColor = Config.Bind("Editor GUI", "Open Level Button Selected Color", new Color(0.2588f, 0.2588f, 0.2588f, 1f), "Selected color of the folder button.");
				ConfigEntries.OpenFileButtonFadeDuration = Config.Bind("Editor GUI", "Open Level Button Fade Duration", 0.2f, "Fade duration of the folder button.");

				ConfigEntries.OpenFileButtonHoverSize = Config.Bind("Editor GUI", "Open Level Button Hover Size", 1f, new ConfigDescription("How big the button gets when hovered.", hoverRange));

				ConfigEntries.OpenFileCoverPosition = Config.Bind("Editor GUI", "Open Level Cover Position", new Vector2(-276f, 0f), "Position of the level cover.");
				ConfigEntries.OpenFileCoverScale = Config.Bind("Editor GUI", "Open Level Cover Size", new Vector2(26f, 26f), "Size of the level cover.");

				ConfigEntries.ChangesRefreshLevelList = Config.Bind("Editor GUI", "Changes Refresh Level List", false, "If the level list reloads whenever a change is made.");

				ConfigEntries.ShowLevelDeleteButton = Config.Bind("Editor GUI", "Open Level Show Delete Button", false, "Shows a delete button that can be used to move levels to a recycling folder.");

				ConfigEntries.TimelineObjectHoverSize = Config.Bind("Editor GUI", "Timeline Object Hover Size", 1f, new ConfigDescription("How big the button gets when hovered.", hoverRange));
				ConfigEntries.KeyframeHoverSize = Config.Bind("Editor GUI", "Keyframe Hover Size", 1f, new ConfigDescription("How big the button gets when hovered.", hoverRange));
				ConfigEntries.TimelineBarButtonsHoverSize = Config.Bind("Editor GUI", "Timeline Bar Buttons Hover Size", 1f, new ConfigDescription("How big the button gets when hovered.", hoverRange));

				ConfigEntries.MarkerColN0 = Config.Bind("Editor GUI", "Marker Color 1", Color.white, "Color 1 of the second set of marker colors.");
				ConfigEntries.MarkerColN1 = Config.Bind("Editor GUI", "Marker Color 2", Color.white, "Color 2 of the second set of marker colors.");
				ConfigEntries.MarkerColN2 = Config.Bind("Editor GUI", "Marker Color 3", Color.white, "Color 3 of the second set of marker colors.");
				ConfigEntries.MarkerColN3 = Config.Bind("Editor GUI", "Marker Color 4", Color.white, "Color 4 of the second set of marker colors.");
				ConfigEntries.MarkerColN4 = Config.Bind("Editor GUI", "Marker Color 5", Color.white, "Color 5 of the second set of marker colors.");
				ConfigEntries.MarkerColN5 = Config.Bind("Editor GUI", "Marker Color 6", Color.white, "Color 6 of the second set of marker colors.");
				ConfigEntries.MarkerColN6 = Config.Bind("Editor GUI", "Marker Color 7", Color.white, "Color 7 of the second set of marker colors.");
				ConfigEntries.MarkerColN7 = Config.Bind("Editor GUI", "Marker Color 8", Color.white, "Color 8 of the second set of marker colors.");
				ConfigEntries.MarkerColN8 = Config.Bind("Editor GUI", "Marker Color 9", Color.white, "Color 9 of the second set of marker colors.");

				ConfigEntries.PrefabButtonHoverSize = Config.Bind("Editor GUI", "Prefab Button Hover Scale", 1.05f, new ConfigDescription("How big the button gets when hovered.", hoverRange));

				ConfigEntries.PrefabINHScroll = Config.Bind("Editor GUI", "Prefab Internal Horizontal Scroll", false, "If you can scroll left / right or not.");
				ConfigEntries.PrefabINCellSize = Config.Bind("Editor GUI", "Prefab Internal Cell Size", new Vector2(383f, 32f), "Size of each Prefab Cell. Recommended values are 383 and 503.");
				ConfigEntries.PrefabINConstraint = Config.Bind("Editor GUI", "Prefab Internal Constraint Mode", GridLayoutGroup.Constraint.FixedColumnCount, "Which direction the prefab list goes.");
				ConfigEntries.PrefabINConstraintColumns = Config.Bind("Editor GUI", "Prefab Internal Constraint", 1, "How many columns the prefabs are divided into.");
				ConfigEntries.PrefabINCellSpacing = Config.Bind("Editor GUI", "Prefab Internal Spacing", new Vector2(8f, 8f), "Distance between each Prefab Cell.");
				ConfigEntries.PrefabINAxis = Config.Bind("Editor GUI", "Prefab Internal Start Axis", GridLayoutGroup.Axis.Horizontal, "Start axis of the prefab list.");
				ConfigEntries.PrefabINLDeletePos = Config.Bind("Editor GUI", "Prefab Internal Delete Button Pos", new Vector2(367f, -16f), "Position of the Delete Button. Recommended values are 367, -16 and 484, -16.");
				ConfigEntries.PrefabINLDeleteSca = Config.Bind("Editor GUI", "Prefab Internal Delete Button Sca", new Vector2(32f, 32f), "Scale of the Delete Button.");

				ConfigEntries.PrefabINNameHOverflow = Config.Bind("Editor GUI", "Prefab Internal Name HOverflow", HorizontalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
				ConfigEntries.PrefabINNameVOverflow = Config.Bind("Editor GUI", "Prefab Internal Name VOverflow", VerticalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
				ConfigEntries.PrefabINNameFontSize = Config.Bind("Editor GUI", "Prefab Internal Name Font Size", 20, "Size of the text font.");
				ConfigEntries.PrefabINTypeHOverflow = Config.Bind("Editor GUI", "Prefab Internal Type HOverflow", HorizontalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
				ConfigEntries.PrefabINTypeVOverflow = Config.Bind("Editor GUI", "Prefab Internal Type VOverflow", VerticalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
				ConfigEntries.PrefabINTypeFontSize = Config.Bind("Editor GUI", "Prefab Internal Type Font Size", 20, new ConfigDescription("Size of the text font.", fontLimit));

				ConfigEntries.PrefabEXHScroll = Config.Bind("Editor GUI", "Prefab External Horizontal Scroll", false, "If you can scroll left / right or not.");
				ConfigEntries.PrefabEXCellSize = Config.Bind("Editor GUI", "Prefab External Cell Size", new Vector2(383f, 32f), "Size of each Prefab Cell. Recommended values are 383 and 503.");
				ConfigEntries.PrefabEXConstraint = Config.Bind("Editor GUI", "Prefab External Constraint Mode", GridLayoutGroup.Constraint.FixedColumnCount, "Which direction the prefab list goes.");
				ConfigEntries.PrefabEXConstraintColumns = Config.Bind("Editor GUI", "Prefab External Constraint", 1, "How many columns the prefabs are divided into.");
				ConfigEntries.PrefabEXCellSpacing = Config.Bind("Editor GUI", "Prefab External Spacing", new Vector2(8f, 8f), "Distance between each Prefab Cell.");
				ConfigEntries.PrefabEXAxis = Config.Bind("Editor GUI", "Prefab External Start Axis", GridLayoutGroup.Axis.Horizontal, "Start axis of the prefab list.");
				ConfigEntries.PrefabEXLDeletePos = Config.Bind("Editor GUI", "Prefab External Delete Button Pos", new Vector2(367f, -16f), "Position of the Delete Button.");
				ConfigEntries.PrefabEXLDeleteSca = Config.Bind("Editor GUI", "Prefab External Delete Button Sca", new Vector2(32f, 32f), "Scale of the Delete Button.");

				ConfigEntries.PrefabEXNameHOverflow = Config.Bind("Editor GUI", "Prefab Internal Name HOverflow", HorizontalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
				ConfigEntries.PrefabEXNameVOverflow = Config.Bind("Editor GUI", "Prefab Internal Name VOverflow", VerticalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
				ConfigEntries.PrefabEXNameFontSize = Config.Bind("Editor GUI", "Prefab Internal Name Font Size", 20, "Size of the text font.");
				ConfigEntries.PrefabEXTypeHOverflow = Config.Bind("Editor GUI", "Prefab Internal Type HOverflow", HorizontalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
				ConfigEntries.PrefabEXTypeVOverflow = Config.Bind("Editor GUI", "Prefab Internal Type VOverflow", VerticalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
				ConfigEntries.PrefabEXTypeFontSize = Config.Bind("Editor GUI", "Prefab Internal Type Font Size", 20, new ConfigDescription("Size of the text font.", fontLimit));

				ConfigEntries.PrefabINANCH = Config.Bind("Editor GUI", "Prefab Internal Popup Pos", new Vector2(0f, -16f), "Position of the internal prefabs popup.");
				ConfigEntries.PrefabINSD = Config.Bind("Editor GUI", "Prefab Internal Popup Size", new Vector2(400f, -32f), "Scale of the internal prefabs popup.");
				ConfigEntries.PrefabEXANCH = Config.Bind("Editor GUI", "Prefab External Popup Pos", new Vector2(-32f, -16f), "Position of the external prefabs popup.");
				ConfigEntries.PrefabEXSD = Config.Bind("Editor GUI", "Prefab External Popup Size", new Vector2(400f, -32f), "Scale of the external prefabs popup.");
				ConfigEntries.PrefabEXPathPos = Config.Bind("Editor GUI", "Prefab External Prefab Path Pos", new Vector2(325f, 15f), "Position of the prefab path input field.");
				ConfigEntries.PrefabEXPathSca = Config.Bind("Editor GUI", "Prefab External Prefab Path Length", 150f, "Length of the prefab path input field.");
				ConfigEntries.PrefabEXRefreshPos = Config.Bind("Editor GUI", "Prefab External Prefab Refresh Pos", new Vector2(210f, 450f), "Position of the prefab refresh button.");
			}

			//Fields
			{
				ConfigEntries.TemplateThemeName = Config.Bind("Fields", "Theme Template Name", "New Theme", "Name of the template theme.");
				ConfigEntries.TemplateThemeGUIColor = Config.Bind("Fields", "Theme Template GUI", LSColors.white, "GUI Color of the template theme.");
				ConfigEntries.TemplateThemeBGColor = Config.Bind("Fields", "Theme Template BG", LSColors.gray900, "BG Color of the template theme.");
				ConfigEntries.TemplateThemePlayerColor1 = Config.Bind("Fields", "Theme Template Player 1", LSColors.HexToColor("E57373"), "Player 1 Color of the template theme.");
				ConfigEntries.TemplateThemePlayerColor2 = Config.Bind("Fields", "Theme Template Player 2", LSColors.HexToColor("64B5F6"), "Player 2 Color of the template theme.");
				ConfigEntries.TemplateThemePlayerColor3 = Config.Bind("Fields", "Theme Template Player 3", LSColors.HexToColor("81C784"), "Player 3 Color of the template theme.");
				ConfigEntries.TemplateThemePlayerColor4 = Config.Bind("Fields", "Theme Template Player 4", LSColors.HexToColor("FFB74D"), "Player 4 Color of the template theme.");
				ConfigEntries.TemplateThemeOBJColor1 = Config.Bind("Fields", "Theme Template OBJ 1", LSColors.gray100, "OBJ 1 Color of the template theme.");
				ConfigEntries.TemplateThemeOBJColor2 = Config.Bind("Fields", "Theme Template OBJ 2", LSColors.gray200, "OBJ 2 Color of the template theme.");
				ConfigEntries.TemplateThemeOBJColor3 = Config.Bind("Fields", "Theme Template OBJ 3", LSColors.gray300, "OBJ 3 Color of the template theme.");
				ConfigEntries.TemplateThemeOBJColor4 = Config.Bind("Fields", "Theme Template OBJ 4", LSColors.gray400, "OBJ 4 Color of the template theme.");
				ConfigEntries.TemplateThemeOBJColor5 = Config.Bind("Fields", "Theme Template OBJ 5", LSColors.gray500, "OBJ 5 Color of the template theme.");
				ConfigEntries.TemplateThemeOBJColor6 = Config.Bind("Fields", "Theme Template OBJ 6", LSColors.gray600, "OBJ 6 Color of the template theme.");
				ConfigEntries.TemplateThemeOBJColor7 = Config.Bind("Fields", "Theme Template OBJ 7", LSColors.gray700, "OBJ 7 Color of the template theme.");
				ConfigEntries.TemplateThemeOBJColor8 = Config.Bind("Fields", "Theme Template OBJ 8", LSColors.gray800, "OBJ 8 Color of the template theme.");
				ConfigEntries.TemplateThemeOBJColor9 = Config.Bind("Fields", "Theme Template OBJ 9", LSColors.gray900, "OBJ 9 Color of the template theme.");
				ConfigEntries.TemplateThemeBGColor1 = Config.Bind("Fields", "Theme Template BG 1", LSColors.pink100, "BG 1 Color of the template theme.");
				ConfigEntries.TemplateThemeBGColor2 = Config.Bind("Fields", "Theme Template BG 2", LSColors.pink200, "BG 2 Color of the template theme.");
				ConfigEntries.TemplateThemeBGColor3 = Config.Bind("Fields", "Theme Template BG 3", LSColors.pink300, "BG 3 Color of the template theme.");
				ConfigEntries.TemplateThemeBGColor4 = Config.Bind("Fields", "Theme Template BG 4", LSColors.pink400, "BG 4 Color of the template theme.");
				ConfigEntries.TemplateThemeBGColor5 = Config.Bind("Fields", "Theme Template BG 5", LSColors.pink500, "BG 5 Color of the template theme.");
				ConfigEntries.TemplateThemeBGColor6 = Config.Bind("Fields", "Theme Template BG 6", LSColors.pink600, "BG 6 Color of the template theme.");
				ConfigEntries.TemplateThemeBGColor7 = Config.Bind("Fields", "Theme Template BG 7", LSColors.pink700, "BG 7 Color of the template theme.");
				ConfigEntries.TemplateThemeBGColor8 = Config.Bind("Fields", "Theme Template BG 8", LSColors.pink800, "BG 8 Color of the template theme.");
				ConfigEntries.TemplateThemeBGColor9 = Config.Bind("Fields", "Theme Template BG 9", LSColors.pink900, "BG 9 Color of the template theme.");

			}

			//Preview
			{
				ConfigEntries.ShowObjectsOnLayer = Config.Bind("Preview", "Show only objects on current layer?", false, "If enabled, all objects not on current layer will be set to transparent");
				ConfigEntries.ShowObjectsAlpha = Config.Bind("Preview", "Visible object opacity", 0.2f, "Opacity of the objects not on the current layer.");
				ConfigEntries.ShowEmpties = Config.Bind("Preview", "Show empties", false, "If enabled, show all objects that are set to the empty object type.");
				ConfigEntries.ShowDamagable = Config.Bind("Preview", "Only Show Damagable", false, "If enabled, only objects that can damage the player will be shown.");
				ConfigEntries.HighlightObjects = Config.Bind("Preview", "Highlight Objects", true, "If enabled and if cursor hovers over an object, it will be highlighted.");
				ConfigEntries.HighlightColor = Config.Bind("Preview", "Object Highlight Amount", new Color(0.1f, 0.1f, 0.1f), "If an object is hovered, it adds this amount of color to the hovered object.");
				ConfigEntries.HighlightDoubleColor = Config.Bind("Preview", "Object Highlight Double Amount", new Color(0.5f, 0.5f, 0.5f), "If an object is hovered and shift is held, it adds this amount of color to all color channels.");
				ConfigEntries.PreviewSelectFix = Config.Bind("Preview", "Empties not selectable in preview", true, "If enabled, empties will not be selectable in preview.");
				ConfigEntries.ShowSelector = Config.Bind("Preview", "Show Drag Selector", false, "If enabled, a circular point with arrows will appear that allows you to move objects when the circlular point is dragged around and scale the object when the arrows are dragged.");
				emptyDisable = ConfigEntries.PreviewSelectFix.Value;
				emptyVisible = ConfigEntries.ShowEmpties.Value;
			}

			markerColors = new List<ConfigEntry<Color>>
			{
				ConfigEntries.MarkerColN0,
				ConfigEntries.MarkerColN1,
				ConfigEntries.MarkerColN2,
				ConfigEntries.MarkerColN3,
				ConfigEntries.MarkerColN4,
				ConfigEntries.MarkerColN5,
				ConfigEntries.MarkerColN6,
				ConfigEntries.MarkerColN7,
				ConfigEntries.MarkerColN8,
				ConfigEntries.MarkerColN0,
				ConfigEntries.MarkerColN1,
				ConfigEntries.MarkerColN2,
				ConfigEntries.MarkerColN3,
				ConfigEntries.MarkerColN4,
				ConfigEntries.MarkerColN5,
				ConfigEntries.MarkerColN6,
				ConfigEntries.MarkerColN7,
				ConfigEntries.MarkerColN8
			};

			Config.SettingChanged += new EventHandler<SettingChangedEventArgs>(UpdateEditorManagementConfigs);

			//Patch classes
			{
				MethodInfo layerSetter = typeof(DataManager.GameData.BeatmapObject.EditorData).GetProperty("Layer").GetSetMethod(false);
				MethodInfo binSetter = typeof(DataManager.GameData.BeatmapObject.EditorData).GetProperty("Bin").GetSetMethod(false);
				MethodInfo depthSetter = typeof(DataManager.GameData.BeatmapObject).GetProperty("Depth").GetSetMethod(false);

				MethodInfo layerPrefix = typeof(EditorPlugin).GetMethod("LayerSetterPrefix", BindingFlags.Public | BindingFlags.Static);
				MethodInfo binPrefix = typeof(EditorPlugin).GetMethod("BinSetterPrefix", BindingFlags.Public | BindingFlags.Static);
				MethodInfo depthPrefix = typeof(EditorPlugin).GetMethod("DepthSetterPrefix", BindingFlags.Public | BindingFlags.Static);

				HarmonyMethod layerPatch = new HarmonyMethod(layerPrefix);
				HarmonyMethod binPatch = new HarmonyMethod(binPrefix);
				HarmonyMethod depthPatch = new HarmonyMethod(depthPrefix);

				harmony.PatchAll(typeof(EditorPlugin));
				harmony.PatchAll(typeof(MetadataPatch));
				harmony.PatchAll(typeof(ObjEditorPatch));
				harmony.PatchAll(typeof(EditorPatch));
				harmony.PatchAll(typeof(SettingEditorPatch));
				harmony.PatchAll(typeof(MarkerEditorPatch));
				harmony.PatchAll(typeof(PrefabEditorPatch));
				harmony.PatchAll(typeof(DataManagerPatch));
				harmony.PatchAll(typeof(DataManagerBeatmapThemePatch));
				harmony.PatchAll(typeof(EventEditorPatch));
				harmony.PatchAll(typeof(ThemeEditorPatch));
				harmony.PatchAll(typeof(ObjectManagerPatch));
				harmony.PatchAll(typeof(EventsInstance));
				harmony.PatchAll(typeof(DataManagerPrefabObjectPatch));

				harmony.Patch(layerSetter, prefix: layerPatch);
				harmony.Patch(binSetter, prefix: binPatch);
				harmony.Patch(depthSetter, prefix: depthPatch);
			}

			if (!ModCompatibility.mods.ContainsKey("EditorManagement"))
            {
				var mod = new ModCompatibility.Mod(this, GetType());
				mod.methods.Add("SetConfigEntry", GetType().GetMethod("SetConfigEntry"));
				mod.methods.Add("RefreshObjectGUI", GetType().GetMethod("RefreshObjectGUI"));
				ModCompatibility.mods.Add("EditorManagement", mod);
            }
		}

		void Update()
        {
			if (EditorManager.inst == null)
				EventEditorPatch.doneEvents = false;
        }

		#region Property Setters (Helped by Enchart)

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

        #endregion

        public static void SetCatalyst()
        {
			if (catalyst == null && catInstalled == 0)
            {
				if (!GameObject.Find("BepInEx_Manager").GetComponentByName("CatalystBase"))
				{
					catInstalled = 1;
					return;
				}

				catInstalled = 2;
				catalyst = GameObject.Find("BepInEx_Manager").GetComponentByName("CatalystBase").GetType();
				
				if ((string)catalyst.GetField("Name").GetValue(catalyst) == "Editor Catalyst")
                {
					catInstalled = 3;
                }
			}
        }

		#region Comparison Variables

		public static List<Color> prevMarkerColors = new List<Color>();

		public static float prevAutoSaveRepeat;

		public static Vector2 lastMainZoomBounds;
		public static Vector2 lastKeyframeZoomBounds;

		public static WaveformType lastWaveformType;
		public static Color lastWaveformTopColor;
		public static Color lastWaveformBottomColor;
		public static Color lastWaveformBGColor;

		public static Color prevMainTimelineColor;
		public static Color prevKeyframeTimelineColor;
		public static Color prevSelectedObjectsColor;

		#endregion

		private static void UpdateEditorManagementConfigs(object sender, EventArgs e)
		{
			if (EditorManager.inst != null)
			{
				Debug.LogFormat("{0}Repeating Reminder", className);
				RepeatReminder();

				if (ConfigEntries.RenderTimeline.Value && ConfigEntries.GenerateWaveform.Value && (lastWaveformType != ConfigEntries.WaveformMode.Value || lastWaveformBGColor != ConfigEntries.WaveformBGColor.Value || lastWaveformTopColor != ConfigEntries.WaveformTopColor.Value || lastWaveformBottomColor != ConfigEntries.WaveformBottomColor.Value))
				{
					Debug.LogFormat("{0}Setting Waveform", className);
					RTEditor.inst.StartCoroutine(RTEditor.AssignTimelineTexture());

					lastWaveformType = ConfigEntries.WaveformMode.Value;
					lastWaveformBGColor = ConfigEntries.WaveformBGColor.Value;
					lastWaveformTopColor = ConfigEntries.WaveformTopColor.Value;
					lastWaveformBottomColor = ConfigEntries.WaveformBottomColor.Value;
				}

				Debug.LogFormat("{0}Setting Markers", className);
				if (prevMarkerColors.Count < 1)
				{
					foreach (var col in MarkerEditor.inst.markerColors)
					{
						prevMarkerColors.Add(col);
					}
				}

				if (MarkerEditor.inst.markerColors.Count > 9)
				{
					for (int i = 9; i < MarkerEditor.inst.markerColors.Count; i++)
					{
						if (MarkerEditor.inst.markerColors.Count > i && MarkerEditor.inst.markerColors[i] != markerColors[i].Value)
						{
							MarkerEditor.inst.markerColors[i] = markerColors[i].Value;
						}
					}
				}

				if (ConfigEntries.AutoSaveLoopTime.Value != prevAutoSaveRepeat)
				{
					Debug.LogFormat("{0}Setting Autosaving", className);
					prevAutoSaveRepeat = ConfigEntries.AutoSaveLoopTime.Value;

					RTEditor.SetAutosave();
				}

				if (ConfigEntries.ChangesRefreshLevelList.Value && EditorManager.inst.loadedLevels.Count < 60)
				{
					Debug.LogFormat("{0}Getting Level List", className);
					EditorManager.inst.GetLevelList();
					RTEditor.RenderBeatmapSet();
				}

				if (draggableObject != null)
				{
					Debug.LogFormat("{0}Show Selector: {1}", className, ConfigEntries.ShowSelector.Value);
					draggableObject.SetActive(ConfigEntries.ShowSelector.Value);
				}

				//Open File Popup
				{
					Debug.LogFormat("{0}Open Level Values", className);

					var openLevel = EditorManager.inst.GetDialog("Open File Popup").Dialog.gameObject;
					var openTLevel = openLevel.transform;
					var openRTLevel = openLevel.GetComponent<RectTransform>();
					var folderButton = EditorManager.inst.folderButtonPrefab;
					var fButtonBUTT = folderButton.GetComponent<Button>();
					var openGridLVL = openTLevel.Find("mask/content").GetComponent<GridLayoutGroup>();
					var fButtonText = folderButton.transform.Find("folder-name").GetComponent<Text>();

					//Set Open File Popup RectTransform
					openRTLevel.anchoredPosition = ConfigEntries.OpenFilePosition.Value;
					openRTLevel.sizeDelta = ConfigEntries.OpenFileScale.Value;

					//Set Open FIle Popup content GridLayoutGroup
					openGridLVL.cellSize = ConfigEntries.OpenFileCellSize.Value;
					openGridLVL.constraint = (GridLayoutGroup.Constraint)ConfigEntries.OpenFileCellConstraintType.Value;
					openGridLVL.constraintCount = ConfigEntries.OpenFileCellConstraintCount.Value;
					openGridLVL.spacing = ConfigEntries.OpenFileCellSpacing.Value;

					//Folder Button
					fButtonText.horizontalOverflow = ConfigEntries.OpenFileTextHorizontalWrap.Value;
					fButtonText.verticalOverflow = ConfigEntries.OpenFileTextVerticalWrap.Value;
					fButtonText.color = ConfigEntries.OpenFileTextColor.Value;
					fButtonText.fontSize = ConfigEntries.OpenFileTextFontSize.Value;

					//Folder Button Colors
					ColorBlock cb = fButtonBUTT.colors;
					cb.normalColor = ConfigEntries.OpenFileButtonNormalColor.Value;
					cb.pressedColor = ConfigEntries.OpenFileButtonPressedColor.Value;
					cb.highlightedColor = ConfigEntries.OpenFileButtonHighlightedColor.Value;
					cb.selectedColor = ConfigEntries.OpenFileButtonSelectedColor.Value;
					cb.fadeDuration = ConfigEntries.OpenFileButtonFadeDuration.Value;
					fButtonBUTT.colors = cb;
				}

				Debug.LogFormat("{0}Setting Notifications", className);
				var notifications = EditorManager.inst.notification;

				var notifyRT = notifications.GetComponent<RectTransform>();
				var notifyGroup = notifications.GetComponent<VerticalLayoutGroup>();
				notifyRT.sizeDelta = new Vector2(ConfigEntries.NotificationWidth.Value, 632f);
				notifications.transform.localScale = new Vector3(ConfigEntries.NotificationSize.Value, ConfigEntries.NotificationSize.Value, 1f);

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

				Debug.LogFormat("{0}Setting Zoom Cap", className);
				if (ConfigEntries.KeyframeZoomBounds.Value != lastKeyframeZoomBounds)
				{
					lastKeyframeZoomBounds = ConfigEntries.KeyframeZoomBounds.Value;
					ObjEditor.inst.zoomBounds = ConfigEntries.KeyframeZoomBounds.Value;
				}

				if (ConfigEntries.MainZoomBounds.Value != lastMainZoomBounds)
				{
					lastMainZoomBounds = ConfigEntries.MainZoomBounds.Value;
					EditorManager.inst.zoomBounds = ConfigEntries.MainZoomBounds.Value;
					EditorManager.inst.RenderTimeline();
				}

				Debug.LogFormat("{0}Setting Other File Popups", className);
				RectTransform dropdownRT = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("tod-dropdown(Clone)").gameObject.GetComponent<RectTransform>();
				dropdownRT.anchoredPosition = ConfigEntries.OpenFileDropdownPosition.Value;

				RectTransform toggleRT = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("toggle(Clone)").gameObject.GetComponent<RectTransform>();
				toggleRT.anchoredPosition = ConfigEntries.OpenFileTogglePosition.Value;

				var eprt = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("editor path").gameObject.GetComponent<RectTransform>();
				eprt.anchoredPosition = ConfigEntries.OpenFilePathPos.Value;
				eprt.sizeDelta = new Vector2(ConfigEntries.OpenFilePathLength.Value, 34f);
				EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("reload").gameObject.GetComponent<RectTransform>().anchoredPosition = ConfigEntries.OpenFileRefreshPosition.Value;

				Debug.LogFormat("{0}Setting Objects", className);
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

				Debug.LogFormat("{0}Setting Other Mods", className);
				{
					SetShowable();
				}

				//There's a problem somewhere below but Idk where

				Debug.LogFormat("{0}Setting Cursor Color", className);
				{
					if (prevMainTimelineColor != ConfigEntries.MainTimelineSliderColor.Value)
					{
						prevMainTimelineColor = ConfigEntries.MainTimelineSliderColor.Value;
						if (RTEditor.timelineSliderHandle != null)
						{
							RTEditor.timelineSliderHandle.color = ConfigEntries.MainTimelineSliderColor.Value;
						}
						else if (RTExtensions.TryFind("Editor Systems/Editor GUI/sizer/main/whole-timeline/Slider_Parent/Slider/Handle Slide Area/Image/Handle", out GameObject gm) && gm.TryGetComponent(out Image image))
						{
							RTEditor.timelineSliderHandle = image;
							RTEditor.timelineSliderHandle.color = ConfigEntries.MainTimelineSliderColor.Value;
						}
						else
						{
							RTEditor.DisplayCustomNotification("CD", "Whoooops you gotta put this CD up your-", 1f, LSColors.HexToColor("202020"), ConfigEntries.MainTimelineSliderColor.Value, Color.white, "Whoops!");
						}

						if (RTEditor.timelineSliderRuler != null)
						{
							RTEditor.timelineSliderRuler.color = ConfigEntries.MainTimelineSliderColor.Value;
						}
						else if (RTExtensions.TryFind("Editor Systems/Editor GUI/sizer/main/whole-timeline/Slider_Parent/Slider/Handle Slide Area/Image", out GameObject gm) && gm.TryGetComponent(out Image image))
						{
							RTEditor.timelineSliderRuler = GameObject.Find("Editor Systems/Editor GUI/sizer/main/whole-timeline/Slider_Parent/Slider/Handle Slide Area/Image").GetComponent<Image>();
							RTEditor.timelineSliderRuler.color = ConfigEntries.MainTimelineSliderColor.Value;
						}
						else
						{
							RTEditor.DisplayCustomNotification("CD", "Whoooops you gotta put this CD up your-", 1f, LSColors.HexToColor("202020"), ConfigEntries.MainTimelineSliderColor.Value, Color.white, "Whoops!");
						}
					}


					if (prevKeyframeTimelineColor != ConfigEntries.KeyframeTimelineSliderColor.Value)
					{
						prevKeyframeTimelineColor = ConfigEntries.KeyframeTimelineSliderColor.Value;
						if (RTEditor.keyframeTimelineSliderHandle != null)
						{
							RTEditor.keyframeTimelineSliderHandle.color = ConfigEntries.KeyframeTimelineSliderColor.Value;
						}
						else if (RTExtensions.TryFind("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/timeline/Scroll View/Viewport/Content/time_slider/Handle Slide Area/Handle/Image", out GameObject gm) && gm.TryGetComponent(out Image image))
						{
							RTEditor.keyframeTimelineSliderHandle = image;
							RTEditor.keyframeTimelineSliderHandle.color = ConfigEntries.KeyframeTimelineSliderColor.Value;
						}
						else
						{
							RTEditor.DisplayCustomNotification("CD", "Whoooops you gotta put this CD up your-", 1f, LSColors.HexToColor("202020"), ConfigEntries.MainTimelineSliderColor.Value, Color.white, "Whoops!");
						}

						if (RTEditor.keyframeTimelineSliderRuler != null)
						{
							RTEditor.keyframeTimelineSliderRuler.color = ConfigEntries.KeyframeTimelineSliderColor.Value;
						}
						else if (RTExtensions.TryFind("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/timeline/Scroll View/Viewport/Content/time_slider/Handle Slide Area/Handle", out GameObject gm) && gm.TryGetComponent(out Image image))
						{
							RTEditor.keyframeTimelineSliderRuler = image;
							RTEditor.keyframeTimelineSliderRuler.color = ConfigEntries.KeyframeTimelineSliderColor.Value;
						}
						else
						{
							RTEditor.DisplayCustomNotification("CD", "Whoooops you gotta put this CD up your-", 1f, LSColors.HexToColor("202020"), ConfigEntries.KeyframeTimelineSliderColor.Value, Color.white, "Whoops!");
						}
					}

					if (prevSelectedObjectsColor != ConfigEntries.ObjectSelectionColor.Value)
					{
						prevSelectedObjectsColor = ConfigEntries.ObjectSelectionColor.Value;
						ObjEditor.inst.SelectedColor = ConfigEntries.ObjectSelectionColor.Value;
					}
				}

				Debug.LogFormat("{0}Setting Prefabs", className);
				{
					//Create Local Variables

					Debug.LogFormat("{0}Setting Prefab Variables", className);
					GameObject internalPrefab = GameObject.Find("Prefab Popup/internal prefabs");
					GameObject externalPrefab = GameObject.Find("Prefab Popup/external prefabs");

					Debug.LogFormat("{0}Getting Components", className);
					if (internalPrefab.transform.Find("mask/content").gameObject.TryGetComponent(out GridLayoutGroup inPMCGridLay))
					{
						Debug.LogFormat("{0}Internal Values", className);
						internalPrefab.GetComponent<RectTransform>().anchoredPosition = ConfigEntries.PrefabINANCH.Value;
						internalPrefab.GetComponent<RectTransform>().sizeDelta = ConfigEntries.PrefabINSD.Value;
						inPMCGridLay.spacing = ConfigEntries.PrefabINCellSpacing.Value;
						inPMCGridLay.cellSize = ConfigEntries.PrefabINCellSize.Value;
						inPMCGridLay.constraint = ConfigEntries.PrefabINConstraint.Value;
						inPMCGridLay.constraintCount = ConfigEntries.PrefabINConstraintColumns.Value;
						inPMCGridLay.startAxis = ConfigEntries.PrefabINAxis.Value;
						internalPrefab.GetComponent<ScrollRect>().horizontal = ConfigEntries.PrefabINHScroll.Value;
					}

					if (externalPrefab.transform.Find("mask/content").gameObject.TryGetComponent(out GridLayoutGroup exPMCGridLay))
					{
						Debug.LogFormat("{0}External Values", className);
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
						if (PrefabEditorPatch.internalContent != null)
						{
							Debug.LogFormat("{0}Internal Buttons", className);
							foreach (object obj in PrefabEditorPatch.internalContent.transform)
							{
								Transform child = (Transform)obj;


								if (child.gameObject.TryGetComponent(out HoverUI hoverUI))
								{
									hoverUI.size = ConfigEntries.PrefabButtonHoverSize.Value;
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
						}

						if (PrefabEditorPatch.externalContent != null)
						{
							Debug.LogFormat("{0}External Buttons", className);
							foreach (object obj in PrefabEditorPatch.externalContent.transform)
							{
								Transform child = (Transform)obj;

								if (child.gameObject.TryGetComponent(out HoverUI hoverUI))
								{
									hoverUI.size = ConfigEntries.PrefabButtonHoverSize.Value;
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
		}

		public static void SetConfigEntry(string name, object value)
        {
			if (RTEditor.editorProperties.TryFind(x => x.name == name, out RTEditor.EditorProperty editorProperty))
            {
				try
                {
					editorProperty.configEntry.BoxedValue = value;
                }
				catch
                {

                }
            }
        }

		public static void RefreshObjectGUI() => inst.StartCoroutine(RTEditor.RefreshObjectGUI());

		public static void SetShowable()
		{
			if (GameObject.Find("BepInEx_Manager").GetComponentByName("ObjectModifiersPlugin"))
			{
				var objectModifiersPlugin = GameObject.Find("BepInEx_Manager").GetComponentByName("ObjectModifiersPlugin").GetType();

				objectModifiersPlugin.GetMethod("SetShowable").Invoke(objectModifiersPlugin, new object[] { ConfigEntries.ShowObjectsOnLayer.Value, ConfigEntries.ShowObjectsAlpha.Value, ConfigEntries.HighlightObjects.Value, ConfigEntries.HighlightColor.Value, ConfigEntries.HighlightDoubleColor.Value });
			}

			if (GameObject.Find("BepInEx_Manager").GetComponentByName("EventsCorePlugin"))
			{
				var eventsCorePlugin = GameObject.Find("BepInEx_Manager").GetComponentByName("EventsCorePlugin").GetType();

				eventsCorePlugin.GetMethod("SetShowable").Invoke(eventsCorePlugin, new object[] { ConfigEntries.ShowObjectsOnLayer.Value, ConfigEntries.ShowObjectsAlpha.Value, ConfigEntries.HighlightObjects.Value, ConfigEntries.HighlightColor.Value, ConfigEntries.HighlightDoubleColor.Value });
			}
		}

		public static bool emptyDisable;
		public static bool emptyVisible;
		public static bool showDamagable;
		public static GameObject tracker;
		public static DraggableObject draggableObject;

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

		[HarmonyPatch(typeof(GameManager), "Update")]
		[HarmonyPostfix]
		private static void GameUpdatePatch()
        {
			if (EditorManager.inst == null && tracker != null)
            {
				Destroy(tracker);
            }

			if (EditorManager.inst != null && catalyst == null)
			{
				foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
				{
					if (beatmapObject != null && Objects.beatmapObjects.ContainsKey(beatmapObject.id))
					{
						var functionObject = Objects.beatmapObjects[beatmapObject.id];

						var gameObject = functionObject.gameObject;
						Material mat = null;
						if (functionObject.renderer != null)
						{
							mat = functionObject.renderer.material;
						}

                        if (EditorManager.inst.isEditing == true && mat != null && mat.HasProperty("_Color") && functionObject.rtObject != null && functionObject.rtObject.selected == true && ConfigEntries.HighlightObjects.Value == true)
                        {
                            if (Input.GetKey(KeyCode.LeftShift))
                            {
                                Color colorHover = new Color(ConfigEntries.HighlightDoubleColor.Value.r, ConfigEntries.HighlightDoubleColor.Value.g, ConfigEntries.HighlightDoubleColor.Value.b);

                                if (mat.color.r > 0.9f && mat.color.g > 0.9f && mat.color.b > 0.9f)
                                {
                                    colorHover = new Color(-ConfigEntries.HighlightDoubleColor.Value.r, -ConfigEntries.HighlightDoubleColor.Value.g, -ConfigEntries.HighlightDoubleColor.Value.b);
                                }

								mat.color += new Color(colorHover.r, colorHover.g, colorHover.b, 0f);
                            }
                            else
                            {
                                Color colorHover = new Color(ConfigEntries.HighlightColor.Value.r, ConfigEntries.HighlightColor.Value.g, ConfigEntries.HighlightColor.Value.b);

                                if (mat.color.r > 0.95f && mat.color.g > 0.95f && mat.color.b > 0.95f)
                                {
                                    colorHover = new Color(-ConfigEntries.HighlightColor.Value.r, -ConfigEntries.HighlightColor.Value.g, -ConfigEntries.HighlightColor.Value.b);
                                }

								mat.color += new Color(colorHover.r, colorHover.g, colorHover.b, 0f);
                            }
                        }
                    }
				}
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

		public static void RepeatReminder()
        {
			EditorManager.inst.CancelInvoke("CreateGrid");
			EditorManager.inst.InvokeRepeating("CreateGrid", ConfigEntries.ReminderLoopTime.Value, ConfigEntries.ReminderLoopTime.Value);
		}

		[HarmonyPatch(typeof(ColorPicker), "UpdateValues")]
		[HarmonyPrefix]
		private static bool UpdateValuesPrefix(ColorPicker __instance, ref Color __0)
		{
			double hue;
			double sat;
			double val;
			LSColors.ColorToHSV(__0, out hue, out sat, out val);

			string text = RTEditor.ColorToHex(__0);

			var RInput = __instance.rgb.transform.Find("R/input").GetComponent<InputField>();
			var GInput = __instance.rgb.transform.Find("G/input").GetComponent<InputField>();
			var BInput = __instance.rgb.transform.Find("B/input").GetComponent<InputField>();
			var HInput = __instance.hsv.transform.Find("H/input").GetComponent<InputField>();
			var SInput = __instance.hsv.transform.Find("S/input").GetComponent<InputField>();
			var VInput = __instance.hsv.transform.Find("V/input").GetComponent<InputField>();

			RInput.onValueChanged.RemoveAllListeners();
			RInput.text = (__0.r * 255f).ToString();
			RInput.onValueChanged.AddListener(delegate (string _val)
			{
				var col = new Color((float)int.Parse(_val) / 255f, (float)int.Parse(GInput.text) / 255f, (float)int.Parse(BInput.text) / 255f, 255f);
				__instance.SwitchCurrentColor(col);
			});

			GInput.onValueChanged.RemoveAllListeners();
			GInput.text = (__0.g * 255f).ToString();
			GInput.onValueChanged.AddListener(delegate (string _val)
			{
				var col = new Color((float)int.Parse(RInput.text) / 255f, (float)int.Parse(_val) / 255f, (float)int.Parse(BInput.text) / 255f, 255f);
				__instance.SwitchCurrentColor(col);
			});

			BInput.onValueChanged.RemoveAllListeners();
			BInput.text = (__0.b * 255f).ToString();
			BInput.onValueChanged.AddListener(delegate (string _val)
			{
				var col = new Color((float)int.Parse(RInput.text) / 255f, (float)int.Parse(GInput.text) / 255f, (float)int.Parse(_val) / 255f, 255f);
				__instance.SwitchCurrentColor(col);
			});

			HInput.onValueChanged.RemoveAllListeners();
			HInput.text = Mathf.RoundToInt((float)hue).ToString();
			HInput.onValueChanged.AddListener(delegate (string _val)
			{
				var col = LSColors.ColorFromHSV(double.Parse(_val), double.Parse(SInput.text) / 100.0, double.Parse(VInput.text) / 100.0);
				__instance.SwitchCurrentColor(col);
			});

			SInput.onValueChanged.RemoveAllListeners();
			SInput.text = Mathf.RoundToInt((float)sat * 100f).ToString();
			SInput.onValueChanged.AddListener(delegate (string _val)
			{
				var col = LSColors.ColorFromHSV(double.Parse(HInput.text), double.Parse(_val) / 100.0, double.Parse(VInput.text) / 100.0);
				__instance.SwitchCurrentColor(col);
			});

			VInput.onValueChanged.RemoveAllListeners();
			VInput.text = Mathf.RoundToInt((float)val * 100f).ToString();
			VInput.onValueChanged.AddListener(delegate (string _val)
			{
				var col = LSColors.ColorFromHSV(double.Parse(HInput.text), double.Parse(SInput.text) / 100.0, double.Parse(_val) / 100.0);
				__instance.SwitchCurrentColor(col);
			});

			var hexInput = __instance.hex.GetComponent<InputField>();

			hexInput.onValueChanged.RemoveAllListeners();
			hexInput.characterLimit = 8;
			hexInput.text = text;
			hexInput.onValueChanged.AddListener(delegate (string _val)
			{
				var col = LSColors.HexToColorAlpha(_val);
				__instance.SwitchCurrentColor(col);
			});

			__instance.currentHex = text;
			__instance.preview.GetComponent<Image>().color = __0;
			__instance.currentColor = __0;

			__instance.hueSlider.GetComponent<Slider>().value = (float)hue / 359f;
			__instance.GenerateBrightnessPanel((float)hue / 359f);
			__instance.UpdateSlider((float)sat, (float)val, RTMath.RectTransformToScreenSpace2(__instance.brightnessPanel.GetComponent<RectTransform>()));
			return false;
		}

		[HarmonyPatch(typeof(ColorPicker), "Start")]
		[HarmonyPrefix]
		private static bool ColorPickerStartPrefix(ColorPicker __instance)
		{
			__instance.hueSliderTexture = new Texture2D(1, 359, TextureFormat.ARGB32, false);
			Color[] array = new Color[359];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = LSColors.ColorFromHSV((double)(359f - (float)i), 1.0, 1.0);
			}
			__instance.hueSliderTexture.SetPixels(array);
			__instance.hueSliderTexture.wrapMode = TextureWrapMode.Repeat;
			__instance.hueSliderTexture.filterMode = FilterMode.Point;
			__instance.hueSliderTexture.Apply();
			__instance.hueSlider.transform.Find("Background").GetComponent<Image>().sprite = Sprite.Create(__instance.hueSliderTexture, new Rect(0f, 0f, 1f, 359f), new Vector2(0.5f, 0.5f), 100f);

			var slider = __instance.hueSlider.GetComponent<Slider>();

			slider.onValueChanged.RemoveAllListeners();
			slider.onValueChanged.AddListener(delegate (float _value)
			{
				__instance.GenerateBrightnessPanel(_value);
				double num;
				double saturation;
				double value;
				LSColors.ColorToHSV(__instance.currentColor, out num, out saturation, out value);
				__instance.UpdateValues(LSColors.ColorFromHSV((double)(_value * 359f), saturation, value));
			});
			//__instance.GenerateBrightnessPanel(slider.value);
			//__instance.UpdateValues(LSColors.black);

			var createClickTrigger = __instance.GetType().GetMethod("CreateClickTrigger", BindingFlags.NonPublic | BindingFlags.Instance);

			Triggers.AddEventTrigger(__instance.brightnessPanel, new List<EventTrigger.Entry> { (EventTrigger.Entry)createClickTrigger.Invoke(__instance, new object[] { EventTriggerType.PointerClick }), (EventTrigger.Entry)createClickTrigger.Invoke(__instance, new object[] { EventTriggerType.Drag }) });
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
			if (ConfigEntries.EditorDebug.Value)
			{
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
			}

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
					if (ConfigEntries.EditorDebug.Value)
						Debug.Log(__1.beatmap.date_edited);
					jn["beatmap"]["date_original"] = __1.beatmap.date_edited;
				}
				if (ConfigEntries.EditorDebug.Value)
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

			var destroyAll = Instantiate(bgRight.transform.Find("create").gameObject);
			destroyAll.transform.SetParent(bgRight.transform);
			destroyAll.transform.localScale = Vector3.one;
			destroyAll.transform.SetSiblingIndex(2);
			destroyAll.name = "destroy";

			destroyAll.GetComponent<Image>().color = new Color(1f, 0.131f, 0.231f, 1f);
			destroyAll.transform.GetChild(0).GetComponent<Text>().text = "Delete All Backgrounds";
			destroyAll.transform.GetChild(0).localScale = Vector3.one;

			var destroyAllButtons = destroyAll.GetComponent<Button>();
			destroyAllButtons.onClick.m_Calls.m_ExecutingCalls.Clear();
			destroyAllButtons.onClick.m_Calls.m_PersistentCalls.Clear();
			destroyAllButtons.onClick.m_PersistentCalls.m_Calls.Clear();
			destroyAllButtons.onClick.RemoveAllListeners();
			destroyAllButtons.onClick.AddListener(delegate ()
			{
				if (DataManager.inst.gameData.backgroundObjects.Count > 1)
				{
					EditorManager.inst.ShowDialog("Warning Popup");
					RTEditor.RefreshWarningPopup("Are you sure you want to delete all backgrounds?", delegate ()
					{
						RTEditor.DeleteAllBackgrounds();
						EditorManager.inst.HideDialog("Warning Popup");
					}, delegate ()
					{
						EditorManager.inst.HideDialog("Warning Popup");
					});
				}
				else
                {
					EditorManager.inst.DisplayNotification("Cannot delete only background object.", 2f, EditorManager.NotificationType.Warning);
                }
			});

			var destroyAllTip = destroyAll.GetComponent<HoverTooltip>();
			HoverTooltip.Tooltip destroyAllTooltip = new HoverTooltip.Tooltip();
			destroyAllTooltip.desc = "Destroy All Objects";
			destroyAllTooltip.hint = "Press this to destroy all background objects, EXCEPT the first one.";
			destroyAllTip.tooltipLangauges.Clear();
			destroyAllTip.tooltipLangauges.Add(destroyAllTooltip);

			var createBGs = Instantiate(bgLeft.transform.Find("name").gameObject);
			createBGs.transform.SetParent(bgRight.transform);
			createBGs.transform.localScale = Vector3.one;
			createBGs.transform.SetSiblingIndex(2);
			createBGs.name = "create bgs";

			var name = createBGs.transform.Find("name").GetComponent<InputField>();
			var nameRT = name.GetComponent<RectTransform>();

			name.onValueChanged.m_Calls.m_ExecutingCalls.Clear();
			name.onValueChanged.m_Calls.m_PersistentCalls.Clear();
			name.onValueChanged.m_PersistentCalls.m_Calls.Clear();
			name.onValueChanged.RemoveAllListeners();

			Destroy(createBGs.transform.Find("active").gameObject);
			nameRT.localScale = Vector3.one;
			name.text = "12";
			name.characterValidation = InputField.CharacterValidation.Integer;
			nameRT.sizeDelta = new Vector2(80f, 34f);

			var createAll = Instantiate(bgRight.transform.Find("create").gameObject);
			createAll.transform.SetParent(createBGs.transform);
			createAll.transform.localScale = Vector3.one;
			createAll.name = "create";

			createAll.GetComponent<Image>().color = new Color(0.6252f, 0.2789f, 0.6649f, 1f);
			createAll.GetComponent<RectTransform>().sizeDelta = new Vector2(278f, 34f);
			createAll.transform.GetChild(0).GetComponent<Text>().text = "Create Backgrounds";
			createAll.transform.GetChild(0).localScale = Vector3.one;

			var buttonCreate = createAll.GetComponent<Button>();
			buttonCreate.onClick.m_Calls.m_ExecutingCalls.Clear();
			buttonCreate.onClick.m_Calls.m_PersistentCalls.Clear();
			buttonCreate.onClick.m_PersistentCalls.m_Calls.Clear();
			buttonCreate.onClick.RemoveAllListeners();
			buttonCreate.onClick.AddListener(delegate ()
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
