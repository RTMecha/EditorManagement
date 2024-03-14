using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BepInEx.Configuration;
using LSFunctions;
using UnityEngine;
using UnityEngine.UI;

namespace EditorManagement.Functions
{
    public class EditorConfig
    {
        public static EditorConfig Instance { get; private set; }

        public ConfigFile Config { get; set; }

        public static AcceptableValueRange<int> FontSizeLimit { get; } = new AcceptableValueRange<int>(1, 40);
        public static AcceptableValueRange<float> HoverScaleLimit { get; } = new AcceptableValueRange<float>(0.7f, 1.4f);

        public EditorConfig(ConfigFile config)
        {
            Instance = this;

            #region General

            Config = config;

            Debug = Config.Bind("General", "Debug", false, "If enabled, specific debugging functions for the editor will be enabled.");
            EditorZenMode = Config.Bind("General", "Editor Zen Mode", false, "If on, the player will not take damage in Preview Mode.");
            BPMSnapsKeyframes = Config.Bind("General", "BPM Snaps Keyframes", false, "Makes object's keyframes snap if Snap BPM is enabled.");
            BPMSnapDivisions = Config.Bind("General", "BPM Snap Divisions", 4f, "How many times the snap is divided into. Can be good for songs that don't do 4 divisions.");
            DraggingPlaysSound = Config.Bind("General", "Dragging Plays Sound", true, "If dragging an object plays a sound.");
            DraggingPlaysSoundOnlyWithBPM = Config.Bind("General", "Dragging Plays Sound Only With BPM", true, "If dragging an object plays a sound ONLY when BPM Snap is active.");
            RoundToNearest = Config.Bind("General", "Round To Nearest", true, "If numbers should be rounded up to 3 decimal points (for example, 0.43321245 into 0.433).");
            PrefabExampleTemplate = Config.Bind("General", "Prefab Example Template", true, "Example Template prefab will always be generated into the internal prefabs for you to use.");
            PasteOffset = Config.Bind("General", "Paste Offset", false, "When enabled objects that are pasted will be pasted at an offset based on the distance between the audio time and the copied object. Otherwise, the objects will be pasted at the earliest objects start time.");
            BringToSelection = Config.Bind("General", "Bring To Selection", false, "When an object is selected (whether it be a regular object, a marker, etc), it will move the layer and audio time to that object.");
            CreateObjectsatCameraCenter = Config.Bind("General", "Create Objects at Camera Center", true, "When an object is created, its position will be set to that of the camera's.");
            CreateObjectsScaleParentDefault = Config.Bind("General", "Create Objects Scale Parent Default", true, "The default value for new Beatmap Objects' Scale Parent.");
            AllowEditorKeybindsWithEditorCam = Config.Bind("General", "Allow Editor Keybinds With Editor Cam", true, "Allows keybinds to be used if EventsCore editor camera is on.");
            RotationEventKeyframeResets = Config.Bind("General", "Rotation Event Keyframe Resets", true, "When an Event / Check rotation keyframe is created, it resets the value to 0.");
            RememberLastKeyframeType = Config.Bind("General", "Remember Last Keyframe Type", false, "When an object is selected for the first time, it selects the previous objects' keyframe selection type. For example, say you had a color keyframe selected, this newly selected object will select the first color keyframe.");

            #endregion

            #region Timeline

            DraggingMainCursorPausesLevel = Config.Bind("Timeline", "Dragging Main Cursor Pauses Level", true, "If dragging the cursor pauses the level.");
            TimelineCursorColor = Config.Bind("Timeline", "Timeline Cursor Color", new Color(0.251f, 0.4627f, 0.8745f, 1f), "Color of the main timeline cursor.");
            KeyframeCursorColor = Config.Bind("Timeline", "Keyframe Cursor Color", new Color(0.251f, 0.4627f, 0.8745f, 1f), "Color of the object timeline cursor.");
            ObjectSelectionColor = Config.Bind("Timeline", "Object Selection Color", new Color(0.251f, 0.4627f, 0.8745f, 1f), "Color of selected objects.");
            MainZoomBounds = Config.Bind("Timeline", "Main Zoom Bounds", new Vector2(16f, 512f), "The limits of the main timeline zoom.");
            KeyframeZoomBounds = Config.Bind("Timeline", "Keyframe Zoom Bounds", new Vector2(1f, 512f), "The limits of the keyframe timeline zoom.");
            MainZoomAmount = Config.Bind("Timeline", "Main Zoom Amount", 0.05f, "Sets the zoom in & out amount for the main timeline.");
            KeyframeZoomAmount = Config.Bind("Timeline", "Keyframe Zoom Amount", 0.05f, "Sets the zoom in & out amount for the keyframe timeline.");
            KeyframeEndLengthOffset = Config.Bind("Timeline", "Keyframe End Length Offset", 2f, "Sets the amount of space you have after the last keyframe in an object.");
            TimelineObjectPrefabTypeIcon = Config.Bind("Timeline", "Timeline Object Prefab Type Icon", true, "Shows the object's prefab type's icon.");
            EventLabelsRenderLeft = Config.Bind("Timeline", "Event Labels Render Left", false, "If the Event Layer labels should render on the left side or not.");
            WaveformGenerate = Config.Bind("Timeline", "Waveform Generate", true, "Allows the timeline waveform to generate. (Waveform might not show on some devices and will increase level load times)");
            WaveformRerender = Config.Bind("Timeline", "Waveform Re-render", false, "If the timeline waveform should update when a value is changed.");
            WaveformMode = Config.Bind("Timeline", "Waveform Mode", WaveformType.Legacy, "The mode of the timeline waveform.");
            WaveformBGColor = Config.Bind("Timeline", "Waveform BG Color", Color.clear, "Color of the background for the waveform.");
            WaveformTopColor = Config.Bind("Timeline", "Waveform Top Color", LSColors.red300, "If waveform mode is Legacy, this will be the top color. Otherwise, it will be the regular color.");
            WaveformBottomColor = Config.Bind("Timeline", "Waveform Bottom Color", LSColors.blue300, "If waveform is Legacy, this will be the bottom color. Otherwise, it will be unused.");
            WaveformTextureFormat = Config.Bind("Timeline", "Waveform Texture Format", TextureFormat.ARGB32, "What format the waveform's texture should render under.");
            TimelineGridEnabled = Config.Bind("Timeline", "Timeline Grid Enabled", true, "If the timeline grid renders.");
            TimelineGridColor = Config.Bind("Timeline", "Timeline Grid Color", new Color(0.2157f, 0.2157f, 0.2196f, 1f), "The color of the timeline grid.");
            TimelineGridThickness = Config.Bind("Timeline", "Timeline Grid Thickness", 2f, "The size of each line of the timeline grid.");
            MarkerLoopActive = Config.Bind("Timeline", "Marker Loop Active", false, "If the marker should loop between markers.");
            MarkerLoopBegin = Config.Bind("Timeline", "Marker Loop Begin", 0, "Audio time gets set to this marker.");
            MarkerLoopEnd = Config.Bind("Timeline", "Marker Loop End", 1, "If the audio time gets to the set marker time, it will loop to the beginning marker.");

            #endregion

            #region Data

            AutosaveLimit = Config.Bind("Data", "Autosave Limit", 7, "If autosave count reaches this number, delete the first autosave.");
            AutosaveLoopTime = Config.Bind("Data", "Autosave Loop Time", 600f, "The repeat time of autosave.");
            LevelLoadsLastTime = Config.Bind("Data", "Level Loads Last Time", true, "Sets the editor position (audio time, layer, etc) to the last saved editor position on level load.");
            LevelPausesOnStart = Config.Bind("Data", "Level Pauses on Start", false, "Editor pauses on level load.");
            SavingSavesThemeOpacity = Config.Bind("Data", "Saving Saves Theme Opacity", false, "Turn this off if you don't want themes to break in unmodded PA.");
            UpdatePrefabListOnFilesChanged = Config.Bind("Data", "Update Prefab List on Files Changed", false, "When you add a prefab to your prefab path, the editor will automatically update the prefab list for you.");
            UpdateThemeListOnFilesChanged = Config.Bind("Data", "Update Theme List on Files Changed", false, "When you add a theme to your theme path, the editor will automatically update the theme list for you.");
            ShowLevelsWithoutCoverNotification = Config.Bind("Data", "Show Levels Without Cover Notification", false, "Sends an error notification for what levels don't have covers.");
            ZIPLevelExportPath = Config.Bind("Data", "ZIP Level Export Path", "", "The custom path to export a zipped level to. If no path is set then it will export to beatmaps/exports.");
            ConvertLevelLSToVGExportPath = Config.Bind("Data", "Convert Level LS to VG Export Path", "", "The custom path to export a level to. If no path is set then it will export to beatmaps/exports.");
            ConvertPrefabLSToVGExportPath = Config.Bind("Data", "Convert Prefab LS to VG Export Path", "", "The custom path to export a prefab to. If no path is set then it will export to beatmaps/exports.");
            ConvertThemeLSToVGExportPath = Config.Bind("Data", "Convert Theme LS to VG Export Path", "", "The custom path to export a prefab to. If no path is set then it will export to beatmaps/exports.");
            ThemeSavesIndents = Config.Bind("Data", "Theme Saves Indents", false, "If .lst files should save with multiple lines and indents.");

            #endregion

            #region Editor GUI

            DragUI = Config.Bind("Editor GUI", "Drag UI", true, "Specific UI popups can be dragged around (such as the parent selector, etc).");
            PlayEditorAnimations = Config.Bind("Editor GUI", "Play Editor Animations", false, "If popups should be animated.");
            ShowModdedFeaturesInEditor = Config.Bind("Editor GUI", "Show Modded Features in Editor", true, "Z axis, 10-18 color slots, homing keyframes, etc get set active / inactive with this on / off respectively");
            HoverUIPlaySound = Config.Bind("Editor GUI", "Hover UI Play Sound", false, "Plays a sound when the hover UI element is hovered over.");
            ImportPrefabsDirectly = Config.Bind("Editor GUI", "Import Prefabs Directly", false, "When clicking on an External Prefab, instead of importing it directly it'll bring up a Prefab External View Dialog if this config is off.");
            ThemesPerPage = Config.Bind("Editor GUI", "Themes Per Page", 10, "How many themes are shown per page in the Beatmap Themes popup.");
            NotificationWidth = Config.Bind("Editor GUI", "Notification Width", 221f, "Width of the notifications.");
            NotificationSize = Config.Bind("Editor GUI", "Notification Size", 1f, "Total size of the notifications.");
            NotificationDirection = Config.Bind("Editor GUI", "Notification Direction", Direction.Down, "Direction the notifications popup from.");
            NotificationsDisplay = Config.Bind("Editor GUI", "Notifications Display", true, "If the notifications should display. Does not include the help box.");
            AdjustPositionInputs = Config.Bind("Editor GUI", "Adjust Position Inputs", true, "If position keyframe input fields should be adjusted so they're in a proper row rather than having Z Axis below X Axis without a label. Drawback with doing this is it makes the fields smaller than normal.");
            HideVisualElementsWhenObjectIsEmpty = Config.Bind("Editor GUI", "Hide Visual Elements When Object Is Empty", true, "If the Beatmap Object is empty, anything related to the visuals of the object doesn't show.");
            OpenLevelPosition = Config.Bind("Editor GUI", "Open Level Position", Vector2.zero, "The position of the Open Level popup.");
            OpenLevelScale = Config.Bind("Editor GUI", "Open Level Scale", new Vector2(600f, 400f), "The size of the Open Level popup.");
            OpenLevelEditorPathPos = Config.Bind("Editor GUI", "Open Level Editor Path Pos", new Vector2(275f, 16f), "The position of the editor path input field.");
            OpenLevelEditorPathLength = Config.Bind("Editor GUI", "Open Level Editor Path Length", 104f, "The length of the editor path input field.");
            OpenLevelListRefreshPosition = Config.Bind("Editor GUI", "Open Level List Refresh Position", new Vector2(330f, 432f), "The position of the refresh button.");
            OpenLevelTogglePosition = Config.Bind("Editor GUI", "Open Level Toggle Position", new Vector2(600f, 16f), "The position of the descending toggle.");
            OpenLevelDropdownPosition = Config.Bind("Editor GUI", "Open Level Dropdown Position", new Vector2(501f, 416f), "The position of the sort dropdown.");
            OpenLevelCellSize = Config.Bind("Editor GUI", "Open Level Cell Size", new Vector2(584f, 32f), "Size of each cell.");
            OpenLevelCellConstraintType = Config.Bind("Editor GUI", "Open Level Cell Constraint Type", GridLayoutGroup.Constraint.FixedColumnCount, "How the cells are layed out.");
            OpenLevelCellConstraintCount = Config.Bind("Editor GUI", "Open Level Cell Constraint Count", 1, "How many rows / columns there are, depending on Constraint Type.");
            OpenLevelCellSpacing = Config.Bind("Editor GUI", "Open Level Cell Spacing", new Vector2(0f, 8f), "The space between each cell.");
            OpenLevelTextHorizontalWrap = Config.Bind("Editor GUI", "Open Level Text Horizontal Wrap", HorizontalWrapMode.Wrap, "Horizontal Wrap Mode of the folder button text.");
            OpenLevelTextVerticalWrap = Config.Bind("Editor GUI", "Open Level Text Vertical Wrap", VerticalWrapMode.Truncate, "Vertical Wrap Mode of the folder button text.");
            OpenLevelTextFontSize = Config.Bind("Editor GUI", "Open Level Text Font Size", 20, new ConfigDescription("Font size of the folder button text.", FontSizeLimit));

            OpenLevelFolderNameMax = Config.Bind("Editor GUI", "Open Level Folder Name Max", 14, "Limited length of the folder name.");
            OpenLevelSongNameMax = Config.Bind("Editor GUI", "Open Level Song Name Max", 22, "Limited length of the song name.");
            OpenLevelArtistNameMax = Config.Bind("Editor GUI", "Open Level Artist Name Max", 16, "Limited length of the artist name.");
            OpenLevelCreatorNameMax = Config.Bind("Editor GUI", "Open Level Creator Name Max", 16, "Limited length of the creator name.");
            OpenLevelDescriptionMax = Config.Bind("Editor GUI", "Open Level Description Max", 16, "Limited length of the description.");
            OpenLevelDateMax = Config.Bind("Editor GUI", "Open Level Date Max", 16, "Limited length of the date.");
            OpenLevelTextFormatting = Config.Bind("Editor GUI", "Open Level Text Formatting", ".  /{0} : {1} by {2}",
                    "The way the text is formatted for each level. {0} is folder, {1} is song, {2} is artist, {3} is creator, {4} is difficulty, {5} is description and {6} is last edited.");

            OpenLevelButtonHoverSize = Config.Bind("Editor GUI", "Open Level Button Hover Size", 1f, new ConfigDescription("How big the button gets when hovered.", HoverScaleLimit));
            OpenLevelCoverPosition = Config.Bind("Editor GUI", "Open Level Cover Position", new Vector2(-276f, 0f), "Position of the level cover.");
            OpenLevelCoverScale = Config.Bind("Editor GUI", "Open Level Cover Scale", new Vector2(26f, 26f), "Size of the level cover.");

            ChangesRefreshLevelList = Config.Bind("Editor GUI", "Changes Refresh Level List", false, "If the level list reloads whenever a change is made.");
            OpenLevelShowDeleteButton = Config.Bind("Editor GUI", "Open Level Show Delete Button", false, "Shows a delete button that can be used to move levels to a recycling folder.");

            TimelineObjectHoverSize = Config.Bind("Editor GUI", "Timeline Object Hover Size", 1f, new ConfigDescription("How big the button gets when hovered.", HoverScaleLimit));
            KeyframeHoverSize = Config.Bind("Editor GUI", "Keyframe Hover Size", 1f, new ConfigDescription("How big the button gets when hovered.", HoverScaleLimit));
            TimelineBarButtonsHoverSize = Config.Bind("Editor GUI", "Timeline Bar Buttons Hover Size", 1.05f, new ConfigDescription("How big the button gets when hovered.", HoverScaleLimit));
            PrefabButtonHoverSize = Config.Bind("Editor GUI", "Prefab Button Hover Size", 1.05f, new ConfigDescription("How big the button gets when hovered.", HoverScaleLimit));

            PrefabInternalPopupPos = Config.Bind("Editor GUI", "Prefab Internal Popup Pos", new Vector2(0f, -16f), "Position of the internal prefabs popup.");
            PrefabInternalPopupSize = Config.Bind("Editor GUI", "Prefab Internal Popup Size", new Vector2(400f, -32f), "Scale of the internal prefabs popup.");
            PrefabInternalHorizontalScroll = Config.Bind("Editor GUI", "Prefab Internal Horizontal Scroll", false, "If you can scroll left / right or not.");
            PrefabInternalCellSize = Config.Bind("Editor GUI", "Prefab Internal Cell Size", new Vector2(383f, 32f), "Size of each Prefab Item.");
            PrefabInternalConstraintMode = Config.Bind("Editor GUI", "Prefab Internal Constraint Mode", GridLayoutGroup.Constraint.FixedColumnCount, "Which direction the prefab list goes.");
            PrefabInternalConstraint = Config.Bind("Editor GUI", "Prefab Internal Constraint", 1, "How many columns the prefabs are divided into.");
            PrefabInternalSpacing = Config.Bind("Editor GUI", "Prefab Internal Spacing", new Vector2(8f, 8f), "Distance between each Prefab Cell.");
            PrefabInternalStartAxis = Config.Bind("Editor GUI", "Prefab Internal Start Axis", GridLayoutGroup.Axis.Horizontal, "Start axis of the prefab list.");
            PrefabInternalDeleteButtonPos = Config.Bind("Editor GUI", "Prefab Internal Delete Button Pos", new Vector2(367f, -16f), "Position of the Delete Button.");
            PrefabInternalDeleteButtonSca = Config.Bind("Editor GUI", "Prefab Internal Delete Button Sca", new Vector2(32f, 32f), "Scale of the Delete Button.");
            PrefabInternalNameHorizontalWrap = Config.Bind("Editor GUI", "Prefab Internal Name Horizontal Wrap", HorizontalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
            PrefabInternalNameVerticalWrap = Config.Bind("Editor GUI", "Prefab Internal Name Vertical Wrap", VerticalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
            PrefabInternalNameFontSize = Config.Bind("Editor GUI", "Prefab Internal Name Font Size", 20, new ConfigDescription("Size of the text font.", FontSizeLimit));
            PrefabInternalTypeHorizontalWrap = Config.Bind("Editor GUI", "Prefab Internal Type Horizontal Wrap", HorizontalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
            PrefabInternalTypeVerticalWrap = Config.Bind("Editor GUI", "Prefab Internal Type Vertical Wrap", VerticalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
            PrefabInternalTypeFontSize = Config.Bind("Editor GUI", "Prefab Internal Type Font Size", 20, new ConfigDescription("Size of the text font.", FontSizeLimit));

            PrefabExternalPopupPos = Config.Bind("Editor GUI", "Prefab External Popup Pos", new Vector2(0f, -16f), "Position of the external prefabs popup.");
            PrefabExternalPopupSize = Config.Bind("Editor GUI", "Prefab External Popup Size", new Vector2(400f, -32f), "Scale of the external prefabs popup.");
            PrefabExternalPrefabPathPos = Config.Bind("Editor GUI", "Prefab External Prefab Path Pos", new Vector2(325f, 15f), "Position of the prefab path input field.");
            PrefabExternalPrefabPathLength = Config.Bind("Editor GUI", "Prefab External Prefab Path Length", 150f, "Length of the prefab path input field.");
            PrefabExternalPrefabRefreshPos = Config.Bind("Editor GUI", "Prefab External Prefab Refresh Pos", new Vector2(210f, 450f), "Position of the prefab refresh button.");
            PrefabExternalHorizontalScroll = Config.Bind("Editor GUI", "Prefab External Horizontal Scroll", false, "If you can scroll left / right or not.");
            PrefabExternalCellSize = Config.Bind("Editor GUI", "Prefab External Cell Size", new Vector2(383f, 32f), "Size of each Prefab Item.");
            PrefabExternalConstraintMode = Config.Bind("Editor GUI", "Prefab External Constraint Mode", GridLayoutGroup.Constraint.FixedColumnCount, "Which direction the prefab list goes.");
            PrefabExternalConstraint = Config.Bind("Editor GUI", "Prefab External Constraint", 1, "How many columns the prefabs are divided into.");
            PrefabExternalSpacing = Config.Bind("Editor GUI", "Prefab External Spacing", new Vector2(8f, 8f), "Distance between each Prefab Cell.");
            PrefabExternalStartAxis = Config.Bind("Editor GUI", "Prefab External Start Axis", GridLayoutGroup.Axis.Horizontal, "Start axis of the prefab list.");
            PrefabExternalDeleteButtonPos = Config.Bind("Editor GUI", "Prefab External Delete Button Pos", new Vector2(367f, -16f), "Position of the Delete Button.");
            PrefabExternalDeleteButtonSca = Config.Bind("Editor GUI", "Prefab External Delete Button Sca", new Vector2(32f, 32f), "Scale of the Delete Button.");
            PrefabExternalNameHorizontalWrap = Config.Bind("Editor GUI", "Prefab External Name Horizontal Wrap", HorizontalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
            PrefabExternalNameVerticalWrap = Config.Bind("Editor GUI", "Prefab External Name Vertical Wrap", VerticalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
            PrefabExternalNameFontSize = Config.Bind("Editor GUI", "Prefab External Name Font Size", 20, new ConfigDescription("Size of the text font.", FontSizeLimit));
            PrefabExternalTypeHorizontalWrap = Config.Bind("Editor GUI", "Prefab External Type Horizontal Wrap", HorizontalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
            PrefabExternalTypeVerticalWrap = Config.Bind("Editor GUI", "Prefab External Type Vertical Wrap", VerticalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
            PrefabExternalTypeFontSize = Config.Bind("Editor GUI", "Prefab External Type Font Size", 20, new ConfigDescription("Size of the text font.", FontSizeLimit));

            #endregion

            #region Fields

            ScrollwheelLargeAmountKey = Config.Bind("Fields", "Scrollwheel Large Amount Key", KeyCode.LeftControl, "If this key is being held while you are scrolling over a number field, the number will change by a large amount. If the key is set to None, you will not need to hold a key.");
            ScrollwheelSmallAmountKey = Config.Bind("Fields", "Scrollwheel Small Amount Key", KeyCode.LeftAlt, "If this key is being held while you are scrolling over a number field, the number will change by a small amount. If the key is set to None, you will not need to hold a key.");
            ScrollwheelRegularAmountKey = Config.Bind("Fields", "Scrollwheel Regular Amount Key", KeyCode.None, "If this key is being held while you are scrolling over a number field, the number will change by the regular amount. If the key is set to None, you will not need to hold a key.");
            ScrollwheelVector2LargeAmountKey = Config.Bind("Fields", "Scrollwheel Vector2 Large Amount Key", KeyCode.LeftControl, "If this key is being held while you are scrolling over a number field, the number will change by a large amount. If the key is set to None, you will not need to hold a key.");
            ScrollwheelVector2SmallAmountKey = Config.Bind("Fields", "Scrollwheel Vector2 Small Amount Key", KeyCode.LeftAlt, "If this key is being held while you are scrolling over a number field, the number will change by a small amount. If the key is set to None, you will not need to hold a key.");
            ScrollwheelVector2RegularAmountKey = Config.Bind("Fields", "Scrollwheel Vector2 Regular Amount Key", KeyCode.None, "If this key is being held while you are scrolling over a number field, the number will change by the regular amount. If the key is set to None, you will not need to hold a key.");
            ShowModifiedColors = Config.Bind("Fields", "Show Modified Colors", true, "Keyframe colors show any modifications done (such as hue, saturation and value).");
            ThemeTemplateName = Config.Bind("Fields", "Theme Template Name", "New Theme", "Name of the template theme.");
            ThemeTemplateGUI = Config.Bind("Fields", "Theme Template GUI", LSColors.white, "GUI Color of the template theme.");
            ThemeTemplateTail = Config.Bind("Fields", "Theme Template Tail", LSColors.white, "Tail Color of the template theme.");
            ThemeTemplateBG = Config.Bind("Fields", "Theme Template BG", LSColors.gray900, "BG Color of the template theme.");
            ThemeTemplatePlayer1 = Config.Bind("Fields", "Theme Template Player 1", LSColors.HexToColor("E57373"), "Player 1 Color of the template theme.");
            ThemeTemplatePlayer2 = Config.Bind("Fields", "Theme Template Player 2", LSColors.HexToColor("64B5F6"), "Player 2 Color of the template theme.");
            ThemeTemplatePlayer3 = Config.Bind("Fields", "Theme Template Player 3", LSColors.HexToColor("81C784"), "Player 3 Color of the template theme.");
            ThemeTemplatePlayer4 = Config.Bind("Fields", "Theme Template Player 4", LSColors.HexToColor("FFB74D"), "Player 4 Color of the template theme.");
            ThemeTemplateOBJ1 = Config.Bind("Fields", "Theme Template OBJ 1", LSColors.gray100, "OBJ 1 Color of the template theme.");
            ThemeTemplateOBJ2 = Config.Bind("Fields", "Theme Template OBJ 2", LSColors.gray200, "OBJ 2 Color of the template theme.");
            ThemeTemplateOBJ3 = Config.Bind("Fields", "Theme Template OBJ 3", LSColors.gray300, "OBJ 3 Color of the template theme.");
            ThemeTemplateOBJ4 = Config.Bind("Fields", "Theme Template OBJ 4", LSColors.gray400, "OBJ 4 Color of the template theme.");
            ThemeTemplateOBJ5 = Config.Bind("Fields", "Theme Template OBJ 5", LSColors.gray500, "OBJ 5 Color of the template theme.");
            ThemeTemplateOBJ6 = Config.Bind("Fields", "Theme Template OBJ 6", LSColors.gray600, "OBJ 6 Color of the template theme.");
            ThemeTemplateOBJ7 = Config.Bind("Fields", "Theme Template OBJ 7", LSColors.gray700, "OBJ 7 Color of the template theme.");
            ThemeTemplateOBJ8 = Config.Bind("Fields", "Theme Template OBJ 8", LSColors.gray800, "OBJ 8 Color of the template theme.");
            ThemeTemplateOBJ9 = Config.Bind("Fields", "Theme Template OBJ 9", LSColors.gray900, "OBJ 9 Color of the template theme.");
            ThemeTemplateOBJ10 = Config.Bind("Fields", "Theme Template OBJ 10", LSColors.gray100, "OBJ 10 Color of the template theme.");
            ThemeTemplateOBJ11 = Config.Bind("Fields", "Theme Template OBJ 11", LSColors.gray200, "OBJ 11 Color of the template theme.");
            ThemeTemplateOBJ12 = Config.Bind("Fields", "Theme Template OBJ 12", LSColors.gray300, "OBJ 12 Color of the template theme.");
            ThemeTemplateOBJ13 = Config.Bind("Fields", "Theme Template OBJ 13", LSColors.gray400, "OBJ 13 Color of the template theme.");
            ThemeTemplateOBJ14 = Config.Bind("Fields", "Theme Template OBJ 14", LSColors.gray500, "OBJ 14 Color of the template theme.");
            ThemeTemplateOBJ15 = Config.Bind("Fields", "Theme Template OBJ 15", LSColors.gray600, "OBJ 15 Color of the template theme.");
            ThemeTemplateOBJ16 = Config.Bind("Fields", "Theme Template OBJ 16", LSColors.gray700, "OBJ 16 Color of the template theme.");
            ThemeTemplateOBJ17 = Config.Bind("Fields", "Theme Template OBJ 17", LSColors.gray800, "OBJ 17 Color of the template theme.");
            ThemeTemplateOBJ18 = Config.Bind("Fields", "Theme Template OBJ 18", LSColors.gray900, "OBJ 18 Color of the template theme.");
            ThemeTemplateBG1 = Config.Bind("Fields", "Theme Template BG 1", LSColors.pink100, "BG 1 Color of the template theme.");
            ThemeTemplateBG2 = Config.Bind("Fields", "Theme Template BG 2", LSColors.pink200, "BG 2 Color of the template theme.");
            ThemeTemplateBG3 = Config.Bind("Fields", "Theme Template BG 3", LSColors.pink300, "BG 3 Color of the template theme.");
            ThemeTemplateBG4 = Config.Bind("Fields", "Theme Template BG 4", LSColors.pink400, "BG 4 Color of the template theme.");
            ThemeTemplateBG5 = Config.Bind("Fields", "Theme Template BG 5", LSColors.pink500, "BG 5 Color of the template theme.");
            ThemeTemplateBG6 = Config.Bind("Fields", "Theme Template BG 6", LSColors.pink600, "BG 6 Color of the template theme.");
            ThemeTemplateBG7 = Config.Bind("Fields", "Theme Template BG 7", LSColors.pink700, "BG 7 Color of the template theme.");
            ThemeTemplateBG8 = Config.Bind("Fields", "Theme Template BG 8", LSColors.pink800, "BG 8 Color of the template theme.");
            ThemeTemplateBG9 = Config.Bind("Fields", "Theme Template BG 9", LSColors.pink900, "BG 9 Color of the template theme.");
            ThemeTemplateFX1 = Config.Bind("Fields", "Theme Template FX 1", LSColors.gray100, "FX 1 Color of the template theme.");
            ThemeTemplateFX2 = Config.Bind("Fields", "Theme Template FX 2", LSColors.gray200, "FX 2 Color of the template theme.");
            ThemeTemplateFX3 = Config.Bind("Fields", "Theme Template FX 3", LSColors.gray300, "FX 3 Color of the template theme.");
            ThemeTemplateFX4 = Config.Bind("Fields", "Theme Template FX 4", LSColors.gray400, "FX 4 Color of the template theme.");
            ThemeTemplateFX5 = Config.Bind("Fields", "Theme Template FX 5", LSColors.gray500, "FX 5 Color of the template theme.");
            ThemeTemplateFX6 = Config.Bind("Fields", "Theme Template FX 6", LSColors.gray600, "FX 6 Color of the template theme.");
            ThemeTemplateFX7 = Config.Bind("Fields", "Theme Template FX 7", LSColors.gray700, "FX 7 Color of the template theme.");
            ThemeTemplateFX8 = Config.Bind("Fields", "Theme Template FX 8", LSColors.gray800, "FX 8 Color of the template theme.");
            ThemeTemplateFX9 = Config.Bind("Fields", "Theme Template FX 9", LSColors.gray900, "FX 9 Color of the template theme.");
            ThemeTemplateFX10 = Config.Bind("Fields", "Theme Template FX 10", LSColors.gray100, "FX 10 Color of the template theme.");
            ThemeTemplateFX11 = Config.Bind("Fields", "Theme Template FX 11", LSColors.gray200, "FX 11 Color of the template theme.");
            ThemeTemplateFX12 = Config.Bind("Fields", "Theme Template FX 12", LSColors.gray300, "FX 12 Color of the template theme.");
            ThemeTemplateFX13 = Config.Bind("Fields", "Theme Template FX 13", LSColors.gray400, "FX 13 Color of the template theme.");
            ThemeTemplateFX14 = Config.Bind("Fields", "Theme Template FX 14", LSColors.gray500, "FX 14 Color of the template theme.");
            ThemeTemplateFX15 = Config.Bind("Fields", "Theme Template FX 15", LSColors.gray600, "FX 15 Color of the template theme.");
            ThemeTemplateFX16 = Config.Bind("Fields", "Theme Template FX 16", LSColors.gray700, "FX 16 Color of the template theme.");
            ThemeTemplateFX17 = Config.Bind("Fields", "Theme Template FX 17", LSColors.gray800, "FX 17 Color of the template theme.");
            ThemeTemplateFX18 = Config.Bind("Fields", "Theme Template FX 18", LSColors.gray900, "FX 18 Color of the template theme.");

            #endregion

            #region Functions

            #endregion

            #region Preview

            OnlyObjectsOnCurrentLayerVisible = Config.Bind("Preview", "Only Objects on Current Layer Visible", false, "If enabled, all objects not on current layer will be set to transparent");
            VisibleObjectOpacity = Config.Bind("Preview", "Visible Object Opacity", 0.2f, "Opacity of the objects not on the current layer.");
            ShowEmpties = Config.Bind("Preview", "Show Empties", false, "If enabled, show all objects that are set to the empty object type.");
            OnlyShowDamagable = Config.Bind("Preview", "Only Show Damagable", false, "If enabled, only objects that can damage the player will be shown.");
            HighlightObjects = Config.Bind("Preview", "Highlight Objects", true, "If enabled and if cursor hovers over an object, it will be highlighted.");
            ObjectHighlightAmount = Config.Bind("Preview", "Object Highlight Amount", new Color(0.1f, 0.1f, 0.1f), "If an object is hovered, it adds this amount of color to the hovered object.");
            ObjectHighlightDoubleAmount = Config.Bind("Preview", "Object Highlight Double Amount", new Color(0.5f, 0.5f, 0.5f), "If an object is hovered and shift is held, it adds this amount of color to the hovered object.");
            ObjectDraggerEnabled = Config.Bind("Preview", "Object Dragger Enabled", false, "If an object can be dragged around.");
            ObjectDraggerRotatorRadius = Config.Bind("Preview", "Object Dragger Rotator Radius", 22f, "The size of the Object Draggers' rotation ring.");
            ObjectDraggerScalerOffset = Config.Bind("Preview", "Object Dragger Scaler Offset", 6f, "The distance of the Object Draggers' scale arrows.");
            ObjectDraggerScalerScale = Config.Bind("Preview", "Object Dragger Scaler Scale", 1.6f, "The size of the Object Draggers' scale arrows.");

            #endregion

        }

        #region General

        public ConfigEntry<bool> Debug { get; set; }
        public ConfigEntry<bool> EditorZenMode { get; set; }
        public ConfigEntry<bool> BPMSnapsKeyframes { get; set; }
        public ConfigEntry<float> BPMSnapDivisions { get; set; }
        public ConfigEntry<bool> DraggingPlaysSound { get; set; }
        public ConfigEntry<bool> DraggingPlaysSoundOnlyWithBPM { get; set; }
        public ConfigEntry<bool> RoundToNearest { get; set; }
        public ConfigEntry<bool> PrefabExampleTemplate { get; set; }
        public ConfigEntry<bool> PasteOffset { get; set; }
        public ConfigEntry<bool> BringToSelection { get; set; }
        public ConfigEntry<bool> CreateObjectsatCameraCenter { get; set; }
        public ConfigEntry<bool> CreateObjectsScaleParentDefault { get; set; }
        public ConfigEntry<bool> AllowEditorKeybindsWithEditorCam { get; set; }
        public ConfigEntry<bool> RotationEventKeyframeResets { get; set; }
        public ConfigEntry<bool> RememberLastKeyframeType { get; set; }

        #endregion

        #region Timeline

        public ConfigEntry<bool> DraggingMainCursorPausesLevel { get; set; }
        public ConfigEntry<Color> TimelineCursorColor { get; set; }
        public ConfigEntry<Color> KeyframeCursorColor { get; set; }
        public ConfigEntry<Color> ObjectSelectionColor { get; set; }
        public ConfigEntry<Vector2> MainZoomBounds { get; set; }
        public ConfigEntry<Vector2> KeyframeZoomBounds { get; set; }
        public ConfigEntry<float> MainZoomAmount { get; set; }
        public ConfigEntry<float> KeyframeZoomAmount { get; set; }
        public ConfigEntry<float> KeyframeEndLengthOffset { get; set; }
        public ConfigEntry<bool> TimelineObjectPrefabTypeIcon { get; set; }
        public ConfigEntry<bool> EventLabelsRenderLeft { get; set; }
        public ConfigEntry<bool> WaveformGenerate { get; set; }
        public ConfigEntry<bool> WaveformRerender { get; set; }
        public ConfigEntry<WaveformType> WaveformMode { get; set; }
        public ConfigEntry<Color> WaveformBGColor { get; set; }
        public ConfigEntry<Color> WaveformTopColor { get; set; }
        public ConfigEntry<Color> WaveformBottomColor { get; set; }
        public ConfigEntry<TextureFormat> WaveformTextureFormat { get; set; }
        public ConfigEntry<bool> TimelineGridEnabled { get; set; }
        public ConfigEntry<Color> TimelineGridColor { get; set; }
        public ConfigEntry<float> TimelineGridThickness { get; set; }
        public ConfigEntry<bool> MarkerLoopActive { get; set; }
        public ConfigEntry<int> MarkerLoopBegin { get; set; }
        public ConfigEntry<int> MarkerLoopEnd { get; set; }

        #endregion

        #region Data

        public ConfigEntry<int> AutosaveLimit { get; set; }
        public ConfigEntry<float> AutosaveLoopTime { get; set; }
        public ConfigEntry<bool> LevelLoadsLastTime { get; set; }
        public ConfigEntry<bool> LevelPausesOnStart { get; set; }
        public ConfigEntry<bool> SavingSavesThemeOpacity { get; set; }
        public ConfigEntry<bool> UpdatePrefabListOnFilesChanged { get; set; }
        public ConfigEntry<bool> UpdateThemeListOnFilesChanged { get; set; }
        public ConfigEntry<bool> ShowLevelsWithoutCoverNotification { get; set; }
        public ConfigEntry<string> ZIPLevelExportPath { get; set; }
        public ConfigEntry<string> ConvertLevelLSToVGExportPath { get; set; }
        public ConfigEntry<string> ConvertPrefabLSToVGExportPath { get; set; }
        public ConfigEntry<string> ConvertThemeLSToVGExportPath { get; set; }
        public ConfigEntry<bool> ThemeSavesIndents { get; set; }

        public ConfigEntry<bool> DragUI { get; set; }
        public ConfigEntry<bool> PlayEditorAnimations { get; set; }
        public ConfigEntry<bool> ShowModdedFeaturesInEditor { get; set; }
        public ConfigEntry<bool> HoverUIPlaySound { get; set; }
        public ConfigEntry<bool> ImportPrefabsDirectly { get; set; }
        public ConfigEntry<int> ThemesPerPage { get; set; }
        public ConfigEntry<float> NotificationWidth { get; set; }
        public ConfigEntry<float> NotificationSize { get; set; }
        public ConfigEntry<Direction> NotificationDirection { get; set; }
        public ConfigEntry<bool> NotificationsDisplay { get; set; }
        public ConfigEntry<bool> AdjustPositionInputs { get; set; }
        public ConfigEntry<bool> HideVisualElementsWhenObjectIsEmpty { get; set; }
        public ConfigEntry<Vector2> OpenLevelPosition { get; set; }
        public ConfigEntry<Vector2> OpenLevelScale { get; set; }
        public ConfigEntry<Vector2> OpenLevelEditorPathPos { get; set; }
        public ConfigEntry<float> OpenLevelEditorPathLength { get; set; }
        public ConfigEntry<Vector2> OpenLevelListRefreshPosition { get; set; }
        public ConfigEntry<Vector2> OpenLevelTogglePosition { get; set; }
        public ConfigEntry<Vector2> OpenLevelDropdownPosition { get; set; }
        public ConfigEntry<Vector2> OpenLevelCellSize { get; set; }
        public ConfigEntry<GridLayoutGroup.Constraint> OpenLevelCellConstraintType { get; set; }
        public ConfigEntry<int> OpenLevelCellConstraintCount { get; set; }
        public ConfigEntry<Vector2> OpenLevelCellSpacing { get; set; }
        public ConfigEntry<HorizontalWrapMode> OpenLevelTextHorizontalWrap { get; set; }
        public ConfigEntry<VerticalWrapMode> OpenLevelTextVerticalWrap { get; set; }
        public ConfigEntry<int> OpenLevelTextFontSize { get; set; }

        public ConfigEntry<int> OpenLevelFolderNameMax { get; set; }
        public ConfigEntry<int> OpenLevelSongNameMax { get; set; }
        public ConfigEntry<int> OpenLevelArtistNameMax { get; set; }
        public ConfigEntry<int> OpenLevelCreatorNameMax { get; set; }
        public ConfigEntry<int> OpenLevelDescriptionMax { get; set; }
        public ConfigEntry<int> OpenLevelDateMax { get; set; }
        public ConfigEntry<string> OpenLevelTextFormatting { get; set; }

        public ConfigEntry<float> OpenLevelButtonHoverSize { get; set; }
        public ConfigEntry<Vector2> OpenLevelCoverPosition { get; set; }
        public ConfigEntry<Vector2> OpenLevelCoverScale { get; set; }

        public ConfigEntry<bool> ChangesRefreshLevelList { get; set; }
        public ConfigEntry<bool> OpenLevelShowDeleteButton { get; set; }

        public ConfigEntry<float> TimelineObjectHoverSize { get; set; }
        public ConfigEntry<float> KeyframeHoverSize { get; set; }
        public ConfigEntry<float> TimelineBarButtonsHoverSize { get; set; }
        public ConfigEntry<float> PrefabButtonHoverSize { get; set; }

        //Prefab Internal
        public ConfigEntry<Vector2> PrefabInternalPopupPos { get; set; }
        public ConfigEntry<Vector2> PrefabInternalPopupSize { get; set; }
        public ConfigEntry<bool> PrefabInternalHorizontalScroll { get; set; }
        public ConfigEntry<Vector2> PrefabInternalCellSize { get; set; }
        public ConfigEntry<GridLayoutGroup.Constraint> PrefabInternalConstraintMode { get; set; }
        public ConfigEntry<int> PrefabInternalConstraint { get; set; }
        public ConfigEntry<Vector2> PrefabInternalSpacing { get; set; }
        public ConfigEntry<GridLayoutGroup.Axis> PrefabInternalStartAxis { get; set; }
        public ConfigEntry<Vector2> PrefabInternalDeleteButtonPos { get; set; }
        public ConfigEntry<Vector2> PrefabInternalDeleteButtonSca { get; set; }
        public ConfigEntry<HorizontalWrapMode> PrefabInternalNameHorizontalWrap { get; set; }
        public ConfigEntry<VerticalWrapMode> PrefabInternalNameVerticalWrap { get; set; }
        public ConfigEntry<int> PrefabInternalNameFontSize { get; set; }
        public ConfigEntry<HorizontalWrapMode> PrefabInternalTypeHorizontalWrap { get; set; }
        public ConfigEntry<VerticalWrapMode> PrefabInternalTypeVerticalWrap { get; set; }
        public ConfigEntry<int> PrefabInternalTypeFontSize { get; set; }

        //Prefab External
        public ConfigEntry<Vector2> PrefabExternalPopupPos { get; set; }
        public ConfigEntry<Vector2> PrefabExternalPopupSize { get; set; }
        public ConfigEntry<Vector2> PrefabExternalPrefabPathPos { get; set; }
        public ConfigEntry<float> PrefabExternalPrefabPathLength { get; set; }
        public ConfigEntry<Vector2> PrefabExternalPrefabRefreshPos { get; set; }
        public ConfigEntry<bool> PrefabExternalHorizontalScroll { get; set; }
        public ConfigEntry<Vector2> PrefabExternalCellSize { get; set; }
        public ConfigEntry<GridLayoutGroup.Constraint> PrefabExternalConstraintMode { get; set; }
        public ConfigEntry<int> PrefabExternalConstraint { get; set; }
        public ConfigEntry<Vector2> PrefabExternalSpacing { get; set; }
        public ConfigEntry<GridLayoutGroup.Axis> PrefabExternalStartAxis { get; set; }
        public ConfigEntry<Vector2> PrefabExternalDeleteButtonPos { get; set; }
        public ConfigEntry<Vector2> PrefabExternalDeleteButtonSca { get; set; }
        public ConfigEntry<HorizontalWrapMode> PrefabExternalNameHorizontalWrap { get; set; }
        public ConfigEntry<VerticalWrapMode> PrefabExternalNameVerticalWrap { get; set; }
        public ConfigEntry<int> PrefabExternalNameFontSize { get; set; }
        public ConfigEntry<HorizontalWrapMode> PrefabExternalTypeHorizontalWrap { get; set; }
        public ConfigEntry<VerticalWrapMode> PrefabExternalTypeVerticalWrap { get; set; }
        public ConfigEntry<int> PrefabExternalTypeFontSize { get; set; }

        #endregion

        #region Fields

        public ConfigEntry<KeyCode> ScrollwheelLargeAmountKey { get; set; }
        public ConfigEntry<KeyCode> ScrollwheelSmallAmountKey { get; set; }
        public ConfigEntry<KeyCode> ScrollwheelRegularAmountKey { get; set; }
        public ConfigEntry<KeyCode> ScrollwheelVector2LargeAmountKey { get; set; }
        public ConfigEntry<KeyCode> ScrollwheelVector2SmallAmountKey { get; set; }
        public ConfigEntry<KeyCode> ScrollwheelVector2RegularAmountKey { get; set; }
        public ConfigEntry<bool> ShowModifiedColors { get; set; }
        public ConfigEntry<string> ThemeTemplateName { get; set; }
        public ConfigEntry<Color> ThemeTemplateGUI { get; set; }
        public ConfigEntry<Color> ThemeTemplateTail { get; set; }
        public ConfigEntry<Color> ThemeTemplateBG { get; set; }
        public ConfigEntry<Color> ThemeTemplatePlayer1 { get; set; }
        public ConfigEntry<Color> ThemeTemplatePlayer2 { get; set; }
        public ConfigEntry<Color> ThemeTemplatePlayer3 { get; set; }
        public ConfigEntry<Color> ThemeTemplatePlayer4 { get; set; }
        public ConfigEntry<Color> ThemeTemplateOBJ1 { get; set; }
        public ConfigEntry<Color> ThemeTemplateOBJ2 { get; set; }
        public ConfigEntry<Color> ThemeTemplateOBJ3 { get; set; }
        public ConfigEntry<Color> ThemeTemplateOBJ4 { get; set; }
        public ConfigEntry<Color> ThemeTemplateOBJ5 { get; set; }
        public ConfigEntry<Color> ThemeTemplateOBJ6 { get; set; }
        public ConfigEntry<Color> ThemeTemplateOBJ7 { get; set; }
        public ConfigEntry<Color> ThemeTemplateOBJ8 { get; set; }
        public ConfigEntry<Color> ThemeTemplateOBJ9 { get; set; }
        public ConfigEntry<Color> ThemeTemplateOBJ10 { get; set; }
        public ConfigEntry<Color> ThemeTemplateOBJ11 { get; set; }
        public ConfigEntry<Color> ThemeTemplateOBJ12 { get; set; }
        public ConfigEntry<Color> ThemeTemplateOBJ13 { get; set; }
        public ConfigEntry<Color> ThemeTemplateOBJ14 { get; set; }
        public ConfigEntry<Color> ThemeTemplateOBJ15 { get; set; }
        public ConfigEntry<Color> ThemeTemplateOBJ16 { get; set; }
        public ConfigEntry<Color> ThemeTemplateOBJ17 { get; set; }
        public ConfigEntry<Color> ThemeTemplateOBJ18 { get; set; }
        public ConfigEntry<Color> ThemeTemplateBG1 { get; set; }
        public ConfigEntry<Color> ThemeTemplateBG2 { get; set; }
        public ConfigEntry<Color> ThemeTemplateBG3 { get; set; }
        public ConfigEntry<Color> ThemeTemplateBG4 { get; set; }
        public ConfigEntry<Color> ThemeTemplateBG5 { get; set; }
        public ConfigEntry<Color> ThemeTemplateBG6 { get; set; }
        public ConfigEntry<Color> ThemeTemplateBG7 { get; set; }
        public ConfigEntry<Color> ThemeTemplateBG8 { get; set; }
        public ConfigEntry<Color> ThemeTemplateBG9 { get; set; }
        public ConfigEntry<Color> ThemeTemplateFX1 { get; set; }
        public ConfigEntry<Color> ThemeTemplateFX2 { get; set; }
        public ConfigEntry<Color> ThemeTemplateFX3 { get; set; }
        public ConfigEntry<Color> ThemeTemplateFX4 { get; set; }
        public ConfigEntry<Color> ThemeTemplateFX5 { get; set; }
        public ConfigEntry<Color> ThemeTemplateFX6 { get; set; }
        public ConfigEntry<Color> ThemeTemplateFX7 { get; set; }
        public ConfigEntry<Color> ThemeTemplateFX8 { get; set; }
        public ConfigEntry<Color> ThemeTemplateFX9 { get; set; }
        public ConfigEntry<Color> ThemeTemplateFX10 { get; set; }
        public ConfigEntry<Color> ThemeTemplateFX11 { get; set; }
        public ConfigEntry<Color> ThemeTemplateFX12 { get; set; }
        public ConfigEntry<Color> ThemeTemplateFX13 { get; set; }
        public ConfigEntry<Color> ThemeTemplateFX14 { get; set; }
        public ConfigEntry<Color> ThemeTemplateFX15 { get; set; }
        public ConfigEntry<Color> ThemeTemplateFX16 { get; set; }
        public ConfigEntry<Color> ThemeTemplateFX17 { get; set; }
        public ConfigEntry<Color> ThemeTemplateFX18 { get; set; }

        #endregion

        #region Functions?

        #endregion

        #region Preview

        public ConfigEntry<bool> OnlyObjectsOnCurrentLayerVisible { get; set; }
        public ConfigEntry<float> VisibleObjectOpacity { get; set; }
        public ConfigEntry<bool> ShowEmpties { get; set; }
        public ConfigEntry<bool> OnlyShowDamagable { get; set; }
        public ConfigEntry<bool> HighlightObjects { get; set; }
        public ConfigEntry<Color> ObjectHighlightAmount { get; set; }
        public ConfigEntry<Color> ObjectHighlightDoubleAmount { get; set; }
        public ConfigEntry<bool> ObjectDraggerEnabled { get; set; }
        public ConfigEntry<float> ObjectDraggerRotatorRadius { get; set; }
        public ConfigEntry<float> ObjectDraggerScalerOffset { get; set; }
        public ConfigEntry<float> ObjectDraggerScalerScale { get; set; }

        #endregion
    }
}
