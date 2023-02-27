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

using EditorManagement.Functions;

namespace EditorManagement
{
	[BepInPlugin("com.mecha.editormanagement", "Editor Management", " 1.6.0")]
	[BepInProcess("Project Arrhythmia.exe")]
	public class EditorPlugin : BaseUnityPlugin
	{
		public static float scrollBar;
		public static float timeEdit;
		public static float itsTheTime;
		public static int openAmount;
		public static int levelFilter = 0;
		public static bool levelAscend = true;
		public static string editorPath = "editor";
		public static WaveformType waveformType;

		public enum WaveformType
		{
			Legacy,
			Old
		}

		public static List<int> allLayers = new List<int>();

		private static void CheckAllLayers()
        {
			foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
			{
				allLayers.Add(beatmapObject.editorData.Layer);
			}
		}

		//AutoSaves
		public static ConfigEntry<float> AutoSaveRepeat { get; set; }
		public static ConfigEntry<int> AutoSaveLimit { get; set; }

		//Zoom cap
		public static ConfigEntry<Vector2> ObjZoomBounds { get; set; }
		public static ConfigEntry<Vector2> ETLZoomBounds { get; set; }

		//Open File Configs
		public static ConfigEntry<Vector2> ORLAnchoredPos { get; set; }
		public static ConfigEntry<Quaternion> ORLLocalRot { get; set; }
		public static ConfigEntry<Vector2> ORLSizeDelta { get; set; }
		public static ConfigEntry<Vector2> OGLVLCellSize { get; set; }
		public static ConfigEntry<Vector2> ORLTogglePos { get; set; }
		public static ConfigEntry<Vector2> ORLDropdownPos { get; set; }
		public static ConfigEntry<Vector2> ORLPathPos { get; set; }
		public static ConfigEntry<float> ORLPathLength { get; set; }
		public static ConfigEntry<Vector2> ORLRefreshPos { get; set; }
		public static ConfigEntry<GridLayoutGroup.Constraint> OGLVLConstraint { get; set; }
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

		public static ConfigEntry<bool> IfEditorStartTime { get; set; }
		public static ConfigEntry<bool> IfEditorPauses { get; set; }

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

		private void Awake()
		{
			inst = this;

			Logger.LogInfo("Plugin Editor Management is loaded!");

			//AutoSave Config
			AutoSaveRepeat = Config.Bind("AutoSave", "Repeat", 600f, "The repeat time of autosave.");
			AutoSaveLimit = Config.Bind("AutoSave", "Limit", 3, "If autosave count reaches this number, delete the first autosave.");

			//General Editor stuff
			IfEditorStartTime = Config.Bind("General Editor", "Load Saved Time", true, "If enabled, sets the audio time to the last saved timeline position on level load.");
			IfEditorPauses = Config.Bind("General Editor", "Editor Pauses", false, "If enabled, the editor pauses on level load.");

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
			ORLAnchoredPos = Config.Bind("Open File Popup Base", "00 Anchored Pos", Vector2.zero, "The position of the open file popup.");
			ORLLocalRot = Config.Bind("Open File Popup Base", "01 Local Rot", new Quaternion(0f, 0f, 0f, 1f), "Local rotation of the open file popup.");
			ORLSizeDelta = Config.Bind("Open File Popup Base", "02 Size Delta", new Vector2(600f, 400f), "The size of the open file popup.");
			ORLPathPos = Config.Bind("Open File Popup Base", "03 Editor Path Pos", new Vector2(125f, 16f), "The position of the editor path input field.");
			ORLPathLength = Config.Bind("Open File Popup Base", "04 Editor Path Length", 254f, "The length of the editor path input field.");
			ORLRefreshPos = Config.Bind("Open File Popup Base", "05 List Refresh Pos", new Vector2(260f, 432f), "The position of the refresh button.");
			ORLTogglePos = Config.Bind("Open File Popup Base", "06 Toggle Pos", new Vector2(600f, 16f), "The position of the descending toggle.");
			ORLDropdownPos = Config.Bind("Open File Popup Base", "07 Dropdown Pos", new Vector2(501f, 416f), "The position of the sort dropdown.");

			OGLVLCellSize = Config.Bind("Open File Popup Cells", "00 Cell Size", new Vector2(584f, 32f), "Size of each cell.");
			OGLVLConstraint = Config.Bind("Open File Popup Cells", "01 Constraint Type", GridLayoutGroup.Constraint.FixedColumnCount, "How the cells are layed out.");
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
			FButtonFormat = Config.Bind("Open File Popup Buttons", "0A Formatting", ".  /{0} : {1} by {2}", "The way the text is formatted for each level. {0} is folder, {1} is song, {2} is artist, {3} is creator, {4} is difficulty and {5} is description.");

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

			Config.SettingChanged += new EventHandler<SettingChangedEventArgs>(UpdateEditorManagementConfigs);

			//Code written by Enchart
			Harmony harmony = new Harmony("anything here");

			MethodInfo layerSetter = typeof(DataManager.GameData.BeatmapObject.EditorData).GetProperty("Layer").GetSetMethod(false);
			MethodInfo binSetter = typeof(DataManager.GameData.BeatmapObject.EditorData).GetProperty("Bin").GetSetMethod(false);

			MethodInfo prefix1 = typeof(EditorPlugin).GetMethod("LayerSetterPrefix", BindingFlags.Public | BindingFlags.Static);
			MethodInfo prefix2 = typeof(EditorPlugin).GetMethod("BinSetterPrefix", BindingFlags.Public | BindingFlags.Static);

			HarmonyMethod prefixMethod1 = new HarmonyMethod(prefix1);
			HarmonyMethod prefixMethod2 = new HarmonyMethod(prefix2);

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

			harmony.Patch(layerSetter, prefix: prefixMethod1);
			harmony.Patch(binSetter, prefix: prefixMethod2);
			harmony.Patch(loadLevelMoveNext, postfix: loadLevelPrefix);
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

		private static void UpdateEditorManagementConfigs(object sender, EventArgs e)
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

			//Create Local Variables
			GameObject openLevel = EditorManager.inst.GetDialog("Open File Popup").Dialog.gameObject;
			Transform openTLevel = openLevel.transform;
			RectTransform openRTLevel = openLevel.GetComponent<RectTransform>();
			GameObject folderButton = EditorManager.inst.folderButtonPrefab;
			Button fButtonBUTT = folderButton.GetComponent<Button>();
			GridLayoutGroup openGridLVL = openTLevel.Find("mask/content").GetComponent<GridLayoutGroup>();
			Text fButtonText = folderButton.transform.Find("folder-name").GetComponent<Text>();

			//Set Editor Zoom cap
			EditorManager.inst.zoomBounds = ETLZoomBounds.Value;

			//Set Open File Popup RectTransform
			openRTLevel.anchoredPosition = ORLAnchoredPos.Value;
			openRTLevel.localRotation = ORLLocalRot.Value;
			openRTLevel.sizeDelta = ORLSizeDelta.Value;

			//Set Open FIle Popup content GridLayoutGroup
			openGridLVL.cellSize = OGLVLCellSize.Value;
			openGridLVL.constraint = OGLVLConstraint.Value;
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
			EditorManager.inst.zoomBounds = ETLZoomBounds.Value;
			EditorManager.inst.RenderTimeline();

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
		}

		public static bool emptyDisable;
		public static bool emptyVisible;
		public static bool showDamagable;

		[HarmonyPatch(typeof(ObjectManager), "Awake")]
		[HarmonyPostfix]
		private static void ObjectAwakePatch()
        {
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
			foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
			{
				if (beatmapObject != null && ObjectManager.inst.beatmapGameObjects.ContainsKey(beatmapObject.id))
				{
					ObjectManager.GameObjectRef gameObjectRef = ObjectManager.inst.beatmapGameObjects[beatmapObject.id];
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
				}
			}
		}

		[HarmonyPatch(typeof(GameManager), "Update")]
		[HarmonyPostfix]
		private static void GameUpdatePatch()
        {
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

				GameObject scrollView = UnityEngine.Object.Instantiate<GameObject>(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View"));
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

				GameObject multiLayerSet = UnityEngine.Object.Instantiate<GameObject>(EventEditor.inst.dialogRight.transform.Find("zoom/zoom").gameObject);
				multiLayerSet.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				multiLayerSet.name = "layer";
				multiLayerSet.transform.SetSiblingIndex(1);
				multiLayerSet.transform.GetChild(0).gameObject.GetComponent<InputField>().text = "1";
				multiLayerSet.transform.GetChild(0).Find("input/Placeholder").GetComponent<Text>().text = "Enter layer...";

				multiLayerSet.GetComponent<RectTransform>().sizeDelta = new Vector2(428f, 32f);

				GameObject multiLB = UnityEngine.Object.Instantiate<GameObject>(multiLayerSet.transform.GetChild(0).Find("<").gameObject);
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

				GameObject multiDepthSet = UnityEngine.Object.Instantiate<GameObject>(EventEditor.inst.dialogRight.transform.Find("zoom/zoom").gameObject);
				multiDepthSet.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				multiDepthSet.name = "depth";
				multiDepthSet.transform.GetChild(0).gameObject.GetComponent<InputField>().text = "15";
				multiDepthSet.transform.GetChild(0).Find("input/Placeholder").GetComponent<Text>().text = "Enter depth...";

				multiDepthSet.GetComponent<RectTransform>().sizeDelta = new Vector2(428f, 32f);

				GameObject multiDB = UnityEngine.Object.Instantiate<GameObject>(multiDepthSet.transform.GetChild(0).Find("<").gameObject);
				multiDB.transform.SetParent(multiDepthSet.transform.GetChild(0));
				multiDB.transform.SetSiblingIndex(2);
				multiDB.name = "|";
				multiDB.GetComponent<Image>().sprite = barButton.GetComponent<Image>().sprite;

				multiDB.GetComponent<Button>().onClick.RemoveAllListeners();
				multiDB.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						objectSelection.GetObjectData().Depth = objectSelection.GetObjectData().Depth + int.Parse(multiDepthSet.transform.GetChild(0).gameObject.GetComponent<InputField>().text);
						ObjectManager.inst.updateObjects(objectSelection);
					}
				});

				Button mdsLeft = multiDepthSet.transform.GetChild(0).Find("<").GetComponent<Button>();
				mdsLeft.GetComponent<Button>().onClick.RemoveAllListeners();
				mdsLeft.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						objectSelection.GetObjectData().Depth -= int.Parse(multiDepthSet.transform.GetChild(0).gameObject.GetComponent<InputField>().text);
						ObjectManager.inst.updateObjects(objectSelection);
					}
				});

				Button mdsRight = multiDepthSet.transform.GetChild(0).Find(">").GetComponent<Button>();
				mdsRight.GetComponent<Button>().onClick.RemoveAllListeners();
				mdsRight.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						objectSelection.GetObjectData().Depth += int.Parse(multiDepthSet.transform.GetChild(0).gameObject.GetComponent<InputField>().text);
						ObjectManager.inst.updateObjects(objectSelection);
					}
				});

				scrollView.transform.Find("Viewport/Content/label layer").SetSiblingIndex(0);

				//Song Time
				GameObject multiTextSongT = UnityEngine.Object.Instantiate<GameObject>(scrollView.transform.Find("Viewport/Content/label layer").gameObject);
				multiTextSongT.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				multiTextSongT.transform.GetChild(0).gameObject.GetComponent<Text>().text = "Set Song Time";
				multiTextSongT.name = "label";

				GameObject multiTimeSet = UnityEngine.Object.Instantiate<GameObject>(EventEditor.inst.dialogRight.transform.Find("zoom/zoom").gameObject);
				multiTimeSet.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				multiTimeSet.name = "time";
				multiTimeSet.transform.GetChild(0).gameObject.GetComponent<InputField>().text = "0";
				multiTimeSet.transform.GetChild(0).Find("input/Placeholder").GetComponent<Text>().text = "Enter time...";

				multiTimeSet.GetComponent<RectTransform>().sizeDelta = new Vector2(428f, 32f);

				GameObject multiTB = UnityEngine.Object.Instantiate<GameObject>(multiTimeSet.transform.GetChild(0).Find("<").gameObject);
				multiTB.transform.SetParent(multiTimeSet.transform.GetChild(0));
				multiTB.transform.SetSiblingIndex(2);
				multiTB.name = "|";
				multiTB.GetComponent<Image>().sprite = barButton.GetComponent<Image>().sprite;

				multiTB.GetComponent<Button>().onClick.RemoveAllListeners();
				multiTB.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						objectSelection.GetObjectData().StartTime = AudioManager.inst.CurrentAudioSource.time;

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
						objectSelection.GetObjectData().StartTime -= float.Parse(multiTimeSet.transform.GetChild(0).gameObject.GetComponent<InputField>().text);
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
						objectSelection.GetObjectData().StartTime += float.Parse(multiTimeSet.transform.GetChild(0).gameObject.GetComponent<InputField>().text);
						ObjectManager.inst.updateObjects(objectSelection);
						ObjEditor.inst.RenderTimelineObject(objectSelection);
					}
				});

				//Name
				GameObject multiTextName = UnityEngine.Object.Instantiate<GameObject>(scrollView.transform.Find("Viewport/Content/label layer").gameObject);
				multiTextName.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				multiTextName.transform.GetChild(0).gameObject.GetComponent<Text>().text = "Set Name";
				multiTextName.name = "label";

				GameObject multiNameSet = UnityEngine.Object.Instantiate<GameObject>(EventEditor.inst.dialogRight.transform.Find("zoom/zoom").gameObject);
				multiNameSet.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				multiNameSet.name = "name";
				multiNameSet.transform.GetChild(0).gameObject.GetComponent<InputField>().characterValidation = InputField.CharacterValidation.None;
				multiNameSet.transform.GetChild(0).gameObject.GetComponent<InputField>().characterLimit = 0;
				multiNameSet.transform.GetChild(0).gameObject.GetComponent<InputField>().text = "name";
				multiNameSet.transform.GetChild(0).Find("input/Placeholder").GetComponent<Text>().text = "Enter name...";

				multiNameSet.GetComponent<RectTransform>().sizeDelta = new Vector2(428f, 32f);

				GameObject multiNB = UnityEngine.Object.Instantiate<GameObject>(multiNameSet.transform.GetChild(0).Find("<").gameObject);
				multiNB.transform.SetParent(multiNameSet.transform.GetChild(0));
				multiNB.transform.SetSiblingIndex(2);
				multiNB.name = "|";
				multiNB.GetComponent<Image>().sprite = barButton.GetComponent<Image>().sprite;

				multiNB.GetComponent<Button>().onClick.RemoveAllListeners();
				multiNB.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						objectSelection.GetObjectData().name = multiNameSet.transform.GetChild(0).gameObject.GetComponent<InputField>().text;

						ObjEditor.inst.RenderTimelineObject(objectSelection);
					}
				});

				UnityEngine.Object.Destroy(multiNameSet.transform.GetChild(0).Find("<").gameObject);
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
						objectSelection.GetObjectData().name += multiNameSet.transform.GetChild(0).gameObject.GetComponent<InputField>().text;
						ObjEditor.inst.RenderTimelineObject(objectSelection);
					}
				});

				//Song Time Autokill
				GameObject multiTextSongAK = UnityEngine.Object.Instantiate<GameObject>(scrollView.transform.Find("Viewport/Content/label layer").gameObject);
				multiTextSongAK.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				multiTextSongAK.transform.GetChild(0).gameObject.GetComponent<Text>().text = "Set Song Time Autokill to Current";
				multiTextSongAK.name = "label";

				GameObject setAutokill = UnityEngine.Object.Instantiate<GameObject>(eventButton);
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
						objectSelection.GetObjectData().autoKillType = DataManager.GameData.BeatmapObject.AutoKillType.SongTime;
						objectSelection.GetObjectData().autoKillOffset = AudioManager.inst.CurrentAudioSource.time;

						ObjectManager.inst.updateObjects(objectSelection, false);
						ObjEditor.inst.RenderTimelineObject(objectSelection);
					}
				});

				GameObject multiTextTypeCycle = UnityEngine.Object.Instantiate<GameObject>(scrollView.transform.Find("Viewport/Content/label layer").gameObject);
				multiTextTypeCycle.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				multiTextTypeCycle.transform.GetChild(0).gameObject.GetComponent<Text>().text = "Cycle object type";
				multiTextTypeCycle.name = "label";

				GameObject cycleObjType = UnityEngine.Object.Instantiate<GameObject>(eventButton);
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
						objectSelection.GetObjectData().objectType += 1;
						if ((int)objectSelection.GetObjectData().objectType > 3)
						{
							objectSelection.GetObjectData().objectType = 0;
						}

						ObjectManager.inst.updateObjects(objectSelection, false);
						ObjEditor.inst.RenderTimelineObject(objectSelection);
					}
				});

				GameObject multiTextLockSwap = UnityEngine.Object.Instantiate<GameObject>(scrollView.transform.Find("Viewport/Content/label layer").gameObject);
				multiTextLockSwap.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				multiTextLockSwap.transform.GetChild(0).gameObject.GetComponent<Text>().text = "Swap each object's lock state";
				multiTextLockSwap.name = "label";

				GameObject lockSwap = UnityEngine.Object.Instantiate<GameObject>(eventButton);
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
						objectSelection.GetObjectData().editorData.locked = !objectSelection.GetObjectData().editorData.locked;

						ObjectManager.inst.updateObjects(objectSelection, false);
						ObjEditor.inst.RenderTimelineObject(objectSelection);
					}
				});

				GameObject multiTextLockToggle = UnityEngine.Object.Instantiate<GameObject>(scrollView.transform.Find("Viewport/Content/label layer").gameObject);
				multiTextLockToggle.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				multiTextLockToggle.transform.GetChild(0).gameObject.GetComponent<Text>().text = "Toggle all object's lock state";
				multiTextLockToggle.name = "label";

				GameObject lockToggle = UnityEngine.Object.Instantiate<GameObject>(eventButton);
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
						if (loggle == false)
						{
							objectSelection.GetObjectData().editorData.locked = false;
						}
						if (loggle == true)
						{
							objectSelection.GetObjectData().editorData.locked = true;
						}

						ObjectManager.inst.updateObjects(objectSelection, false);
						ObjEditor.inst.RenderTimelineObject(objectSelection);
					}
				});

				GameObject multiTextCollapseSwap = UnityEngine.Object.Instantiate<GameObject>(scrollView.transform.Find("Viewport/Content/label layer").gameObject);
				multiTextCollapseSwap.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				multiTextCollapseSwap.transform.GetChild(0).gameObject.GetComponent<Text>().text = "Swap each object's collapse state";
				multiTextCollapseSwap.name = "label";

				GameObject collapseSwap = UnityEngine.Object.Instantiate<GameObject>(eventButton);
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
						objectSelection.GetObjectData().editorData.collapse = !objectSelection.GetObjectData().editorData.collapse;

						ObjectManager.inst.updateObjects(objectSelection, false);
						ObjEditor.inst.RenderTimelineObject(objectSelection);
					}
				});

				GameObject multiTextCollapseToggle = UnityEngine.Object.Instantiate<GameObject>(scrollView.transform.Find("Viewport/Content/label layer").gameObject);
				multiTextCollapseToggle.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				multiTextCollapseToggle.transform.GetChild(0).gameObject.GetComponent<Text>().text = "Toggle all object's collapse state";
				multiTextCollapseToggle.name = "label";

				GameObject collapseToggle = UnityEngine.Object.Instantiate<GameObject>(eventButton);
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
						if (coggle == false)
						{
							objectSelection.GetObjectData().editorData.collapse = false;
						}
						if (coggle == true)
						{
							objectSelection.GetObjectData().editorData.collapse = true;
						}

						ObjectManager.inst.updateObjects(objectSelection, false);
						ObjEditor.inst.RenderTimelineObject(objectSelection);
					}
				});

                //Sync object selection
                {
					GameObject multiTextSync = UnityEngine.Object.Instantiate<GameObject>(scrollView.transform.Find("Viewport/Content/label layer").gameObject);
					multiTextSync.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
					multiTextSync.transform.GetChild(0).gameObject.GetComponent<Text>().text = "Sync to first selected object";
					multiTextSync.name = "label";

					GameObject multiSync = new GameObject("sync layout");
					multiSync.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
					RectTransform multiSyncRT = multiSync.AddComponent<RectTransform>();
					GridLayoutGroup multiSyncGLG = multiSync.AddComponent<GridLayoutGroup>();
					multiSyncGLG.spacing = new Vector2(4f, 4f);
					multiSyncGLG.cellSize = new Vector2(61.6f, 49f);

					GameObject syncStartTime = UnityEngine.Object.Instantiate<GameObject>(eventButton);
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
							objectSelection.GetObjectData().StartTime = ObjEditor.inst.selectedObjects[0].GetObjectData().StartTime;
							ObjEditor.inst.RenderTimelineObject(objectSelection);
						}
					});

					GameObject syncName = UnityEngine.Object.Instantiate<GameObject>(eventButton);
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
							objectSelection.GetObjectData().name = ObjEditor.inst.selectedObjects[0].GetObjectData().name;
							ObjEditor.inst.RenderTimelineObject(objectSelection);
						}
					});

					GameObject syncObjectType = UnityEngine.Object.Instantiate<GameObject>(eventButton);
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
							objectSelection.GetObjectData().objectType = ObjEditor.inst.selectedObjects[0].GetObjectData().objectType;

							ObjectManager.inst.updateObjects(objectSelection, false);
							ObjEditor.inst.RenderTimelineObject(objectSelection);
						}
					});

					GameObject syncAutokillType = UnityEngine.Object.Instantiate<GameObject>(eventButton);
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
							objectSelection.GetObjectData().autoKillType = ObjEditor.inst.selectedObjects[0].GetObjectData().autoKillType;

							ObjectManager.inst.updateObjects(objectSelection, false);
							ObjEditor.inst.RenderTimelineObject(objectSelection);
						}
					});

					GameObject syncAutokillOffset = UnityEngine.Object.Instantiate<GameObject>(eventButton);
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
							objectSelection.GetObjectData().autoKillOffset = ObjEditor.inst.selectedObjects[0].GetObjectData().autoKillOffset;

							ObjectManager.inst.updateObjects(objectSelection, false);
							ObjEditor.inst.RenderTimelineObject(objectSelection);
						}
					});

					GameObject syncParent = UnityEngine.Object.Instantiate<GameObject>(eventButton);
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
							objectSelection.GetObjectData().parent = ObjEditor.inst.selectedObjects[0].GetObjectData().parent;

							ObjectManager.inst.updateObjects(objectSelection, false);
							ObjEditor.inst.RenderTimelineObject(objectSelection);
						}
					});

					GameObject syncParentType = UnityEngine.Object.Instantiate<GameObject>(eventButton);
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
							for (int i = 0; i < 3; i++)
							{
								objectSelection.GetObjectData().SetParentType(i, ObjEditor.inst.selectedObjects[0].GetObjectData().GetParentType(i));
							}

							ObjectManager.inst.updateObjects(objectSelection, false);
							ObjEditor.inst.RenderTimelineObject(objectSelection);
						}
					});

					GameObject syncParentOffset = UnityEngine.Object.Instantiate<GameObject>(eventButton);
					syncParentOffset.transform.SetParent(scrollView.transform.Find("Viewport/Content/sync layout"));
					syncParentOffset.name = "parent offset";

					syncParentOffset.transform.GetChild(0).GetComponent<Text>().text = "PT";
					syncParentOffset.GetComponent<Image>().color = bcol;

					syncParentOffset.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
					syncParentOffset.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
					syncParentOffset.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
					syncParentOffset.GetComponent<Button>().onClick.RemoveAllListeners();
					syncParentOffset.GetComponent<Button>().onClick.AddListener(delegate ()
					{
						foreach (var objectSelection in ObjEditor.inst.selectedObjects)
						{
							for (int i = 0; i < 3; i++)
							{
								objectSelection.GetObjectData().SetParentOffset(i, ObjEditor.inst.selectedObjects[0].GetObjectData().getParentOffset(i));
							}

							ObjectManager.inst.updateObjects(objectSelection, false);
							ObjEditor.inst.RenderTimelineObject(objectSelection);
						}
					});

					GameObject syncOrigin = UnityEngine.Object.Instantiate<GameObject>(eventButton);
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
							objectSelection.GetObjectData().origin = ObjEditor.inst.selectedObjects[0].GetObjectData().origin;

							ObjectManager.inst.updateObjects(objectSelection, false);
							ObjEditor.inst.RenderTimelineObject(objectSelection);
						}
					});

					GameObject syncShape = UnityEngine.Object.Instantiate<GameObject>(eventButton);
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
							objectSelection.GetObjectData().shape = ObjEditor.inst.selectedObjects[0].GetObjectData().shape;
							objectSelection.GetObjectData().shapeOption = ObjEditor.inst.selectedObjects[0].GetObjectData().shapeOption;

							ObjectManager.inst.updateObjects(objectSelection, false);
							ObjEditor.inst.RenderTimelineObject(objectSelection);
						}
					});

					GameObject syncText = UnityEngine.Object.Instantiate<GameObject>(eventButton);
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
							objectSelection.GetObjectData().text = ObjEditor.inst.selectedObjects[0].GetObjectData().text;

							ObjectManager.inst.updateObjects(objectSelection, false);
							ObjEditor.inst.RenderTimelineObject(objectSelection);
						}
					});

					GameObject syncDepth = UnityEngine.Object.Instantiate<GameObject>(eventButton);
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
							objectSelection.GetObjectData().Depth = ObjEditor.inst.selectedObjects[0].GetObjectData().Depth;

							ObjectManager.inst.updateObjects(objectSelection, false);
							ObjEditor.inst.RenderTimelineObject(objectSelection);
						}
					});

					//ISSUE: Causes newly selected objects to retain the values of the previous object for some reason
					//GameObject syncKeyframes = UnityEngine.Object.Instantiate<GameObject>(eventButton);
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
					//		ObjEditor.inst.selectedObjects[i].GetObjectData().events[0] = ObjEditor.inst.selectedObjects[0].GetObjectData().events[0];
					//		ObjEditor.inst.selectedObjects[i].GetObjectData().events[1] = ObjEditor.inst.selectedObjects[0].GetObjectData().events[1];
					//		ObjEditor.inst.selectedObjects[i].GetObjectData().events[2] = ObjEditor.inst.selectedObjects[0].GetObjectData().events[2];
					//		ObjEditor.inst.selectedObjects[i].GetObjectData().events[3] = ObjEditor.inst.selectedObjects[0].GetObjectData().events[3];
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
						  select x).ToList<EditorManager.MetadataWrapper>();

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
				UnityEngine.Object.Destroy(((Transform)obj).gameObject);
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
					GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(EditorManager.inst.folderButtonPrefab);
					gameObject.name = "Folder [" + metadataWrapper.folder + "]";
					gameObject.transform.SetParent(transform);
					gameObject.transform.localScale = Vector3.one;
					HoverTooltip htt = gameObject.AddComponent<HoverTooltip>();

					HoverTooltip.Tooltip levelTip = new HoverTooltip.Tooltip();

					if (metadata != null)
					{
						gameObject.transform.GetChild(0).GetComponent<Text>().text = string.Format(FButtonFormat.Value, LSText.ClampString(metadataWrapper.folder, foldClamp), LSText.ClampString(metadata.song.title, songClamp), LSText.ClampString(metadata.artist.Name, artiClamp), LSText.ClampString(metadata.creator.steam_name, creaClamp), metadata.song.difficulty, LSText.ClampString(metadata.song.description, descClamp));

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

			DataManager.inst.linkTypes = new List<DataManager.LinkType>
			{
				new DataManager.LinkType("Spotify", "https://open.spotify.com/artist/{0}"),
				new DataManager.LinkType("SoundCloud", "https://soundcloud.com/{0}"),
				new DataManager.LinkType("Bandcamp", "https://{0}.bandcamp.com"),
				new DataManager.LinkType("Youtube Music", "https://www.youtube.com/user/{0}"),
				new DataManager.LinkType("Newgrounds", "https://{0}.newgrounds.com/")
			};

			DataManager.inst.AnimationList[1].Animation.keys[1].m_Time = 0.9999f;
			DataManager.inst.AnimationList[1].Animation.keys[1].m_Value = 0f;
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
	}
}
