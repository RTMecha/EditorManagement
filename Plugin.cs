using System;
using System.Reflection;
using System.Reflection.Emit;
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

namespace EditorManagement
{
	[BepInPlugin("com.mecha.editormanagement", "Editor Management", " 1.5.6")]
	[BepInProcess("Project Arrhythmia.exe")]
	public class Plugin : BaseUnityPlugin
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

		//AutoSaves
		private static ConfigEntry<float> AutoSaveRepeat { get; set; }
		private static ConfigEntry<int> AutoSaveLimit { get; set; }

		//Zoom cap
		private static ConfigEntry<Vector2> ObjZoomBounds { get; set; }
		private static ConfigEntry<Vector2> ETLZoomBounds { get; set; }

		//Open File Configs
		private static ConfigEntry<Vector2> ORLAnchoredPos { get; set; }
		private static ConfigEntry<Quaternion> ORLLocalRot { get; set; }
		private static ConfigEntry<Vector2> ORLSizeDelta { get; set; }
		private static ConfigEntry<Vector2> OGLVLCellSize { get; set; }
		private static ConfigEntry<Vector2> ORLTogglePos { get; set; }
		private static ConfigEntry<Vector2> ORLDropdownPos { get; set; }
		private static ConfigEntry<Vector2> ORLPathPos { get; set; }
		private static ConfigEntry<float> ORLPathLength { get; set; }
		private static ConfigEntry<Vector2> ORLRefreshPos { get; set; }
		private static ConfigEntry<GridLayoutGroup.Constraint> OGLVLConstraint { get; set; }
		private static ConfigEntry<int> OGLVLConstraintCount { get; set; }
		private static ConfigEntry<Vector2> OGLVLSpacing { get; set; }
		private static ConfigEntry<HorizontalWrapMode> FButtonHWrap { get; set; }
		private static ConfigEntry<VerticalWrapMode> FButtonVWrap { get; set; }
		private static ConfigEntry<int> FButtonFontSize { get; set; }
		private static ConfigEntry<Color> FButtonTextColor { get; set; }
		private static ConfigEntry<bool> FButtonTextInvert { get; set; }
		private static ConfigEntry<Color> FButtonNColor { get; set; }
		private static ConfigEntry<Color> FButtonHColor { get; set; }
		private static ConfigEntry<Color> FButtonPColor { get; set; }
		private static ConfigEntry<Color> FButtonSColor { get; set; }
		private static ConfigEntry<float> FButtonFadeDColor { get; set; }

		private static ConfigEntry<bool> FButtonDifColor { get; set; }
		private static ConfigEntry<float> FButtonDifColorMult { get; set; }
		private static ConfigEntry<int> FButtonFoldClamp { get; set; }
		private static ConfigEntry<int> FButtonSongClamp { get; set; }
		private static ConfigEntry<int> FButtonArtiClamp { get; set; }
		private static ConfigEntry<int> FButtonCreaClamp { get; set; }
		private static ConfigEntry<int> FButtonDescClamp { get; set; }
		private static ConfigEntry<string> FButtonFormat { get; set; }
		private static ConfigEntry<Vector2> FBIconPos { get; set; }
		private static ConfigEntry<Vector2> FBIconSca { get; set; }

		private static ConfigEntry<bool> IfReloadLList { get; set; }

		//New Markers
		private static ConfigEntry<Color> MarkerColN0 { get; set; }
		private static ConfigEntry<Color> MarkerColN1 { get; set; }
		private static ConfigEntry<Color> MarkerColN2 { get; set; }
		private static ConfigEntry<Color> MarkerColN3 { get; set; }
		private static ConfigEntry<Color> MarkerColN4 { get; set; }
		private static ConfigEntry<Color> MarkerColN5 { get; set; }
		private static ConfigEntry<Color> MarkerColN6 { get; set; }
		private static ConfigEntry<Color> MarkerColN7 { get; set; }
		private static ConfigEntry<Color> MarkerColN8 { get; set; }

		private static ConfigEntry<bool> MarkerLoop { get; set; }
		private static ConfigEntry<int> MarkerEndIndex { get; set; }
		private static ConfigEntry<int> MarkerStartIndex { get; set; }

		private static ConfigEntry<bool> IfEditorStartTime { get; set; }
		private static ConfigEntry<bool> IfEditorPauses { get; set; }

		private static ConfigEntry<float> TimeModify { get; set; }

		private static ConfigEntry<bool> RenderTimeline { get; set; }
		private static ConfigEntry<Color> TimelineBGColor { get; set; }
		private static ConfigEntry<Color> TimelineTopColor { get; set; }
		private static ConfigEntry<Color> TimelineBottomColor { get; set; }

		private static ConfigEntry<bool> ReminderActive { get; set; }
		private static ConfigEntry<float> ReminderRepeat { get; set; }

		private static ConfigEntry<WaveformType> WaveformMode { get; set; }
		private static ConfigEntry<bool> GenerateWaveform { get; set; }
		private static ConfigEntry<bool> ShowObjectsOnLayer { get; set; }
		private static ConfigEntry<float> ShowObjectsAlpha { get; set; }
		private static ConfigEntry<bool> ShowEmpties { get; set; }
		private static ConfigEntry<bool> ShowDamagable { get; set; }
		private static ConfigEntry<bool> HighlightObjects { get; set; }
		private static ConfigEntry<Color> HighlightColor { get; set; }
		private static ConfigEntry<Color> HighlightDoubleColor { get; set; }
		private static ConfigEntry<bool> PreviewSelectFix { get; set; }

		private void Awake()
		{
			base.Logger.LogInfo("Plugin Editor Management is loaded!");

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

			Config.SettingChanged += new EventHandler<SettingChangedEventArgs>(UpdateEditorManagementConfigs);

			//Code written by Enchart
			Harmony harmony = new Harmony("anything here");

			MethodInfo layerSetter = typeof(DataManager.GameData.BeatmapObject.EditorData).GetProperty("Layer").GetSetMethod(false);
			MethodInfo binSetter = typeof(DataManager.GameData.BeatmapObject.EditorData).GetProperty("Bin").GetSetMethod(false);

			MethodInfo prefix1 = typeof(Plugin).GetMethod("LayerSetterPrefix", BindingFlags.Public | BindingFlags.Static);
			MethodInfo prefix2 = typeof(Plugin).GetMethod("BinSetterPrefix", BindingFlags.Public | BindingFlags.Static);

			HarmonyMethod prefixMethod1 = new HarmonyMethod(prefix1);
			HarmonyMethod prefixMethod2 = new HarmonyMethod(prefix2);

			var loadLevelEnumeratorMethod = AccessTools.Method(typeof(EditorManager), "LoadLevel");
			var loadLevelMoveNext = AccessTools.EnumeratorMoveNext(loadLevelEnumeratorMethod);

			MethodInfo prefix3 = typeof(Plugin).GetMethod("LoadLevelEnumerator", BindingFlags.Public | BindingFlags.Static);
			HarmonyMethod loadLevelPrefix = new HarmonyMethod(prefix3);

			harmony.PatchAll(typeof(Plugin));
			harmony.Patch(layerSetter, prefix: prefixMethod1);
			harmony.Patch(binSetter, prefix: prefixMethod2);
			harmony.Patch(loadLevelMoveNext, postfix: loadLevelPrefix);
		}

		public static Plugin inst;

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
			if (ShowDamagable.Value == false)
            {
				ObjectManager.inst.updateObjects();
			}
		}

		public static bool emptyDisable;
		public static bool emptyVisible;

		[HarmonyPatch(typeof(ObjEditor), "Awake")]
		[HarmonyPostfix]
		private static void CreateLayers()
		{
			if (ObjEditor.inst.ObjectView.transform.Find("spacer"))
            {
				ObjEditor.inst.ObjectView.transform.GetChild(17).GetChild(1).gameObject.SetActive(true);
			}
			else
            {
				ObjEditor.inst.ObjectView.transform.GetChild(16).GetChild(1).gameObject.SetActive(true);
			}
			ObjEditor.inst.ObjectView.transform.Find("editor/bin").gameObject.SetActive(true);

			ObjEditor.inst.ObjectView.transform.Find("editor/layer").gameObject.SetActive(false);

			GameObject tbarLayers = UnityEngine.Object.Instantiate<GameObject>(ObjEditor.inst.ObjectView.transform.Find("time/time").gameObject);

			tbarLayers.transform.SetParent(ObjEditor.inst.ObjectView.transform.Find("editor"));
			tbarLayers.name = "layers";
            tbarLayers.transform.SetSiblingIndex(0);
			RectTransform tbarLayersRT = tbarLayers.GetComponent<RectTransform>();
            InputField tbarLayersIF = tbarLayers.GetComponent<InputField>();
			Image layerImage = tbarLayers.GetComponent<Image>();

			tbarLayersIF.characterValidation = InputField.CharacterValidation.Integer;

			HorizontalLayoutGroup edhlg = ObjEditor.inst.ObjectView.transform.Find("editor").GetComponent<HorizontalLayoutGroup>();
			edhlg.childControlWidth = false;
			edhlg.childForceExpandWidth = false;

			tbarLayersRT.sizeDelta = new Vector2(100f, 32f);
			ObjEditor.inst.ObjectView.transform.Find("editor/bin").GetComponent<RectTransform>().sizeDelta = new Vector2(237f, 32f);
		}

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

		[HarmonyPatch(typeof(GameManager), "Update")]
		[HarmonyPostfix]
		private static void GameUpdatePatch()
        {
			foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
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

		[HarmonyPatch(typeof(ObjEditor), "OpenDialog")]
		[HarmonyPostfix]
		private static void OpenD()
		{
			if (ObjEditor.inst.currentObjectSelection.IsObject())
			{
				GameObject tbarLayers = ObjEditor.inst.ObjectView.transform.Find("editor/layers").gameObject;
				InputField tbarLayersIF = tbarLayers.GetComponent<InputField>();
				Image layerImage = tbarLayers.GetComponent<Image>();

				if (ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer < 5)
				{
					float l = ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer + 1;
					tbarLayersIF.text = l.ToString();
				}
				else
				{
					int l = ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer;
					tbarLayersIF.text = l.ToString();
				}

				if (ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer < EditorManager.inst.layerColors.Count)
				{
					layerImage.color = EditorManager.inst.layerColors[ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer];
				}
				if (ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer > 6)
				{
					layerImage.color = Color.white;
				}

				tbarLayersIF.onValueChanged.RemoveAllListeners();
				tbarLayersIF.onValueChanged.AddListener(delegate (string _value)
				{
					if (int.Parse(_value) > 0)
					{
						if (int.Parse(_value) < 6)
						{
							ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer = int.Parse(_value) - 1;
							ObjEditor.inst.RenderTimelineObject(ObjEditor.inst.currentObjectSelection);
						}
						else
						{
							ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer = int.Parse(_value);
							ObjEditor.inst.RenderTimelineObject(ObjEditor.inst.currentObjectSelection);
						}
					}
					else
					{
						ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer = 0;
						ObjEditor.inst.RenderTimelineObject(ObjEditor.inst.currentObjectSelection);
						tbarLayersIF.text = "1";
					}

					if (ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer < EditorManager.inst.layerColors.Count)
					{
						layerImage.color = EditorManager.inst.layerColors[ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer];
					}
					if (ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer > 6)
					{
						layerImage.color = Color.white;
					}
				});
			}
		}

		[HarmonyPatch(typeof(ObjEditor), "RefreshKeyframeGUI")]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> TranspilerPatch(IEnumerable<CodeInstruction> instructions)
		{
			return new CodeMatcher(instructions)
				.Start()
			  .Advance(73)
			  .ThrowIfNotMatch("Is not empty", new CodeMatch(OpCodes.Ldarg_0))
			  .RemoveInstruction()
			  .Advance(0)
			  .ThrowIfNotMatch("Is not currentObjectSelection", new CodeMatch(OpCodes.Ldfld))
			  .Set(OpCodes.Ldc_I4_0, new object[] { })
			  .Advance(1)
			  .ThrowIfNotMatch("Is not Get ObjectData", new CodeMatch(OpCodes.Callvirt))
			  .RemoveInstructions(3)
			  .InstructionEnumeration();
		}
		
		[HarmonyPatch(typeof(ObjEditor), "AddPrefabExpandedToLevel")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> AddPrefabTranspiler(IEnumerable<CodeInstruction> instructions)
        {
			return new CodeMatcher(instructions)
				.Start()
				.Advance(190)
				.ThrowIfNotMatch("Is not editorData object", new CodeMatch(OpCodes.Ldfld))
				.RemoveInstructions(14)
				.Advance(116)
				.ThrowIfNotMatch("Is not editorData prefab", new CodeMatch(OpCodes.Ldfld))
				.RemoveInstructions(14)
				.InstructionEnumeration();
        }

		[HarmonyPatch(typeof(EditorManager), "GetLevelList")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> GetLevelListTranspiler(IEnumerable<CodeInstruction> instructions)
        {
			return new CodeMatcher(instructions)
				.Start()
				.Advance(4)
				.ThrowIfNotMatch("Is not beatmaps/editor", new CodeMatch(OpCodes.Ldstr))
				.SetInstruction(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Plugin), "levelListPath")))
				.ThrowIfNotMatch("Is not ldsfld", new CodeMatch(OpCodes.Ldsfld))
				.InstructionEnumeration();
		}

		[HarmonyPatch(typeof(EditorManager), "LoadLevel", MethodType.Enumerator)]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> LoadLevelTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			return new CodeMatcher(instructions)
				.Start()
				.Advance(60)
				.ThrowIfNotMatch("Is not beatmaps/editor", new CodeMatch(OpCodes.Ldstr))
				.SetInstruction(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Plugin), "levelListSlash")))
				.ThrowIfNotMatch("Is not ldsfld", new CodeMatch(OpCodes.Ldsfld))
				.InstructionEnumeration();
		}

		[HarmonyPatch(typeof(EditorManager), "CreateNewLevel")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> CreateLevelTranspiler(IEnumerable<CodeInstruction> instructions)
        {
			return new CodeMatcher(instructions)
				.Start()
				.Advance(25)
				.ThrowIfNotMatch("Is not 25 beatmaps/editor/", new CodeMatch(OpCodes.Ldstr))
				.SetInstruction(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Plugin), "levelListSlash")))
				.ThrowIfNotMatch("Is not ldsfld 1", new CodeMatch(OpCodes.Ldsfld))
				.Advance(14)
				.ThrowIfNotMatch("Is not 39 beatmaps/editor/", new CodeMatch(OpCodes.Ldstr))
				.SetInstruction(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Plugin), "levelListSlash")))
				.ThrowIfNotMatch("Is not ldsfld 2", new CodeMatch(OpCodes.Ldsfld))
				.Advance(13)
				.ThrowIfNotMatch("Is not 52 beatmaps/editor/", new CodeMatch(OpCodes.Ldstr))
				.SetInstruction(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Plugin), "levelListSlash")))
				.ThrowIfNotMatch("Is not ldsfld 3", new CodeMatch(OpCodes.Ldsfld))
				.Advance(14)
				.ThrowIfNotMatch("Is not 66 beatmaps/editor/", new CodeMatch(OpCodes.Ldstr))
				.SetInstruction(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Plugin), "levelListSlash")))
				.ThrowIfNotMatch("Is not ldsfld 4", new CodeMatch(OpCodes.Ldsfld))
				.Advance(16)
				.ThrowIfNotMatch("Is not 82 20.4.4", new CodeMatch(OpCodes.Ldstr))
				.SetInstruction(new CodeInstruction(OpCodes.Ldstr, "4.1.16"))
				.Advance(24)
				.ThrowIfNotMatch("Is not 106 beatmaps/editor/", new CodeMatch(OpCodes.Ldstr))
				.SetInstruction(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Plugin), "levelListSlash")))
				.ThrowIfNotMatch("Is not ldsfld 5", new CodeMatch(OpCodes.Ldsfld))
				.InstructionEnumeration();
        }

		[HarmonyPatch(typeof(EditorManager), "GetAlbumSprite", MethodType.Enumerator)]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> AlbumSpriteTranspiler(IEnumerable<CodeInstruction> instructions)
        {
			return new CodeMatcher(instructions)
				.Start()
				.Advance(17)
				.ThrowIfNotMatch("Is not beatmaps/editor/", new CodeMatch(OpCodes.Ldstr))
				.SetInstruction(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Plugin), "levelListSlash")))
				.ThrowIfNotMatch("Is not ldsfld", new CodeMatch(OpCodes.Ldsfld))
				.InstructionEnumeration();
        }

		[HarmonyPatch(typeof(EditorManager), "OpenLevelFolder")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> OpenLevelFolderTranspiler(IEnumerable<CodeInstruction> instructions)
        {
			return new CodeMatcher(instructions)
				.Start()
				.Advance(4)
				.ThrowIfNotMatch("Is not beatmaps/editor/", new CodeMatch(OpCodes.Ldstr))
				.SetInstruction(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Plugin), "levelListSlash")))
				.ThrowIfNotMatch("Is not ldsfld 1", new CodeMatch(OpCodes.Ldsfld))
				.Advance(7)
				.ThrowIfNotMatch("Is not beatmaps/editor", new CodeMatch(OpCodes.Ldstr))
				.SetInstruction(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Plugin), "levelListPath")))
				.ThrowIfNotMatch("Is not ldsfld 2", new CodeMatch(OpCodes.Ldsfld))
				.InstructionEnumeration();
		}

		[HarmonyPatch(typeof(EditorManager), "OpenTutorials")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> OpenTutorialsTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			return new CodeMatcher(instructions)
				.Start()
				.SetInstruction(new CodeInstruction(OpCodes.Ldstr, "https://www.youtube.com/playlist?list=PLMHuUok_ojlWH_UZ60tHZIRMWJTDyhRaO"))
				.InstructionEnumeration();
		}

		[HarmonyPatch(typeof(EditorManager), "OpenDiscord")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> OpenDiscordTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			return new CodeMatcher(instructions)
				.Start()
				.SetInstruction(new CodeInstruction(OpCodes.Ldstr, "https://discord.gg/KrGrpBwYgs"))
				.InstructionEnumeration();
		}

		public static string levelListPath = "beatmaps/editor";
		public static string levelListSlash = "beatmaps/editor/";

		[HarmonyPatch(typeof(EditorManager), "RenderTimeline")]
		[HarmonyPostfix]
		private static void RenderLayers()
        {
			if (EditorManager.inst.layer == 5)
			{
				EventEditor.inst.RenderEventObjects();
			}
			else
			{
				ObjEditor.inst.RenderTimelineObjects("");
			}
			CheckpointEditor.inst.RenderCheckpoints();
			MarkerEditor.inst.RenderMarkers();

			var editor = EditorManager.inst;
			MethodInfo updateTimelineSizes = editor.GetType().GetMethod("UpdateTimelineSizes", BindingFlags.NonPublic | BindingFlags.Instance);
			updateTimelineSizes.Invoke(editor, new object[] { });
		}

		[HarmonyPatch(typeof(MetadataEditor), "Render")]
		[HarmonyPostfix]
		private static void MetadataRender()
        {
			if (!EditorManager.inst.GetDialog("Metadata Editor").Dialog.Find("Scroll View").Find("Viewport").Find("Content").Find("artist").Find("x(Clone)"))
            {
				GameObject openLink = UnityEngine.Object.Instantiate<GameObject>(EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("Panel").Find("x").gameObject);

				openLink.transform.SetParent(EditorManager.inst.GetDialog("Metadata Editor").Dialog.Find("Scroll View").Find("Viewport").Find("Content").Find("artist"));

				openLink.transform.Find("Image").gameObject.GetComponent<Image>().sprite = EditorManager.inst.DropdownMenus[3].transform.Find("Open Workshop").Find("Image").gameObject.GetComponent<Image>().sprite;

				RectTransform openLinkRT = openLink.GetComponent<RectTransform>();
				LayoutElement openLinkLE = openLink.AddComponent<LayoutElement>();
				Button openLinkButton = openLink.GetComponent<Button>();

				openLinkRT.anchoredPosition = new Vector2(-520f, -72f);
				openLinkLE.ignoreLayout = true;
				openLinkButton.onClick.RemoveAllListeners();
				openLinkButton.onClick.AddListener(delegate ()
				{
					Application.OpenURL(string.Format(DataManager.inst.linkTypes[DataManager.inst.metaData.artist.LinkType].linkFormat, DataManager.inst.metaData.artist.Link));
				});

				ColorBlock cb = openLinkButton.colors;
				cb.normalColor = new Color(0f, 0.5f, 1f, 1f);
				cb.pressedColor = new Color(0.6f, 0.9f, 1f, 1f);
				cb.highlightedColor = new Color(0.3f, 0.6f, 1f, 1f);
				cb.selectedColor = new Color(0f, 0.5f, 1f, 1f);
				openLinkButton.colors = cb;
			}

			if (!EditorManager.inst.GetDialog("Metadata Editor").Dialog.Find("Scroll View/Viewport/Content/song/difficulty/toggles/master"))
            {
				GameObject master = UnityEngine.Object.Instantiate<GameObject>(EditorManager.inst.GetDialog("Metadata Editor").Dialog.Find("Scroll View/Viewport/Content/song/difficulty/toggles/expert +").gameObject);
				master.transform.SetParent(EditorManager.inst.GetDialog("Metadata Editor").Dialog.Find("Scroll View/Viewport/Content/song/difficulty/toggles"));
				master.name = "master";
				master.transform.Find("Background/Text").GetComponent<Text>().text = "Master";
			}
			if (!EditorManager.inst.GetDialog("Metadata Editor").Dialog.Find("Scroll View/Viewport/Content/song/difficulty/toggles/none"))
			{
				GameObject animation = UnityEngine.Object.Instantiate<GameObject>(EditorManager.inst.GetDialog("Metadata Editor").Dialog.Find("Scroll View/Viewport/Content/song/difficulty/toggles/expert +").gameObject);
				animation.transform.SetParent(EditorManager.inst.GetDialog("Metadata Editor").Dialog.Find("Scroll View/Viewport/Content/song/difficulty/toggles"));
				animation.name = "none";
				animation.transform.Find("Background/Text").GetComponent<Text>().text = "None";
			}

			Transform transform = EditorManager.inst.GetDialog("Metadata Editor").Dialog.Find("Scroll View/Viewport/Content");

			for (int i = 0; i < 7; i++)
			{
				transform.Find("song/difficulty/toggles").GetChild(i).gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(69f, 32f);
				transform.Find("song/difficulty/toggles").GetChild(i).Find("Background").gameObject.GetComponent<Image>().color = DataManager.inst.difficulties[i].color;

				int tmpIndex = i;
				transform.Find("song/difficulty/toggles").GetChild(i).GetComponent<Toggle>().onValueChanged.RemoveAllListeners();
				transform.Find("song/difficulty/toggles").GetChild(i).GetComponent<Toggle>().isOn = (DataManager.inst.metaData.song.difficulty == i);
				transform.Find("song/difficulty/toggles").GetChild(i).GetComponent<Toggle>().onValueChanged.AddListener(delegate (bool _val)
				{
					DataManager.inst.metaData.song.difficulty = tmpIndex;
				});
			}

			transform.Find("song/difficulty/toggles").gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(468f, -16f);

			Button uploadButton = EditorManager.inst.GetDialog("Metadata Editor").Dialog.Find("Scroll View/Viewport/Content/submit/submit").gameObject.GetComponent<Button>();
			uploadButton.onClick.m_Calls.m_ExecutingCalls.Clear();
			uploadButton.onClick.m_Calls.m_PersistentCalls.Clear();
			uploadButton.onClick.m_PersistentCalls.m_Calls.Clear();
			uploadButton.onClick.RemoveAllListeners();
			uploadButton.onClick.AddListener(delegate ()
			{
				string rawProfileJSON = null;
				rawProfileJSON = FileManager.inst.LoadJSONFile(levelListSlash + EditorManager.inst.currentLoadedLevel + "/level.lsb");

				JSONNode jsonnode = JSON.Parse(rawProfileJSON);

				RTFile.WriteToFile(levelListSlash + EditorManager.inst.currentLoadedLevel + "/encryptedlevel.lsb", LSEncryption.EncryptText(rawProfileJSON, "5erewtdvtedsfdSFCDS"));
				EditorManager.inst.DisplayNotification("Encrypted file to " + levelListSlash + EditorManager.inst.currentLoadedLevel + "/encryptedlevel.lsb", 2f, EditorManager.NotificationType.Success, false);
			});
		}

		[HarmonyPatch(typeof(SettingEditor), "Awake")]
		[HarmonyPostfix]
		private static void SettingObjects()
		{
			//Main Variables
			Transform transform = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog").transform;
			Text textFont = GameObject.Find("TitleBar/File/Text").GetComponent<Text>();


			transform.Find("snap/bpm/slider").gameObject.GetComponent<Slider>().maxValue = 999f;
			transform.Find("snap/bpm/slider").gameObject.GetComponent<Slider>().minValue = 0f;

			//Object Count
			GameObject count = new GameObject("object count");
			count.transform.parent = transform;
			RectTransform countRect = count.AddComponent<RectTransform>();
			Text countTXT = count.AddComponent<Text>();
			LayoutElement countLE = count.AddComponent<LayoutElement>();

			countRect.anchoredPosition = new Vector2(-300f, 164f);
			countTXT.font = textFont.font;
			countTXT.text = "Object Count";
			countTXT.fontSize = 32;
			countTXT.horizontalOverflow = HorizontalWrapMode.Overflow;
			countTXT.verticalOverflow = VerticalWrapMode.Overflow;

			countLE.ignoreLayout = true;

			//Event Count
			GameObject eventCount = new GameObject("event count");
			eventCount.transform.parent = transform;
			RectTransform eventCountRect = eventCount.AddComponent<RectTransform>();
			Text eventCountTXT = eventCount.AddComponent<Text>();
			LayoutElement eventCountLE = eventCount.AddComponent<LayoutElement>();

			eventCountRect.anchoredPosition = new Vector2(-300f, 134f);
			eventCountTXT.font = textFont.font;
			eventCountTXT.text = "Event Count";
			eventCountTXT.fontSize = 32;
			eventCountTXT.horizontalOverflow = HorizontalWrapMode.Overflow;
			eventCountTXT.verticalOverflow = VerticalWrapMode.Overflow;

			eventCountLE.ignoreLayout = true;

			//Theme count
			GameObject themeCount = new GameObject("theme count");
			themeCount.transform.parent = transform;
			RectTransform themeCountRect = themeCount.AddComponent<RectTransform>();
			Text themeCountTXT = themeCount.AddComponent<Text>();
			LayoutElement themeCountLE = themeCount.AddComponent<LayoutElement>();

			themeCountRect.anchoredPosition = new Vector2(-300f, 104f);
			themeCountTXT.font = textFont.font;
			themeCountTXT.text = "Theme Count";
			themeCountTXT.fontSize = 32;
			themeCountTXT.horizontalOverflow = HorizontalWrapMode.Overflow;
			themeCountTXT.verticalOverflow = VerticalWrapMode.Overflow;

			themeCountLE.ignoreLayout = true;

			//Prefab External count
			GameObject prefabEXCount = new GameObject("prefabex count");
			prefabEXCount.transform.parent = transform;
			RectTransform prefabEXCountRect = prefabEXCount.AddComponent<RectTransform>();
			Text prefabEXCountTXT = prefabEXCount.AddComponent<Text>();
			LayoutElement prefabEXCountLE = prefabEXCount.AddComponent<LayoutElement>();

			prefabEXCountRect.anchoredPosition = new Vector2(-300f, 74f);
			prefabEXCountTXT.font = textFont.font;
			prefabEXCountTXT.text = "Prefab External Count";
			prefabEXCountTXT.fontSize = 32;
			prefabEXCountTXT.horizontalOverflow = HorizontalWrapMode.Overflow;
			prefabEXCountTXT.verticalOverflow = VerticalWrapMode.Overflow;

			prefabEXCountLE.ignoreLayout = true;

			//Prefab Internal count
			GameObject prefabINCount = new GameObject("prefabin count");
			prefabINCount.transform.parent = transform;
			RectTransform prefabINCountRect = prefabINCount.AddComponent<RectTransform>();
			Text prefabINCountTXT = prefabINCount.AddComponent<Text>();
			LayoutElement prefabINCountLE = prefabINCount.AddComponent<LayoutElement>();

			prefabINCountRect.anchoredPosition = new Vector2(-300f, 44f);
			prefabINCountTXT.font = textFont.font;
			prefabINCountTXT.text = "Prefab External Count";
			prefabINCountTXT.fontSize = 32;
			prefabINCountTXT.horizontalOverflow = HorizontalWrapMode.Overflow;
			prefabINCountTXT.verticalOverflow = VerticalWrapMode.Overflow;

			prefabINCountLE.ignoreLayout = true;

			//No Autokill count
			GameObject noAutokillCount = new GameObject("noautokill count");
			noAutokillCount.transform.parent = transform;
			RectTransform noAutokillCountRect = noAutokillCount.AddComponent<RectTransform>();
			Text noAutokillCountTXT = noAutokillCount.AddComponent<Text>();
			LayoutElement noAutokillCountLE = noAutokillCount.AddComponent<LayoutElement>();

			noAutokillCountRect.anchoredPosition = new Vector2(-300f, 14f);
			noAutokillCountTXT.font = textFont.font;
			noAutokillCountTXT.text = "No Autokill Count";
			noAutokillCountTXT.fontSize = 32;
			noAutokillCountTXT.horizontalOverflow = HorizontalWrapMode.Overflow;
			noAutokillCountTXT.verticalOverflow = VerticalWrapMode.Overflow;

			noAutokillCountLE.ignoreLayout = true;

			//Autokill Offset count
			GameObject offsetCount = new GameObject("offset count");
			offsetCount.transform.parent = transform;
			RectTransform offsetCountRect = offsetCount.AddComponent<RectTransform>();
			Text offsetCountTXT = offsetCount.AddComponent<Text>();
			LayoutElement offsetCountLE = offsetCount.AddComponent<LayoutElement>();

			offsetCountRect.anchoredPosition = new Vector2(-300f, -16f);
			offsetCountTXT.font = textFont.font;
			offsetCountTXT.text = "Autokill Offset Count";
			offsetCountTXT.fontSize = 32;
			offsetCountTXT.horizontalOverflow = HorizontalWrapMode.Overflow;
			offsetCountTXT.verticalOverflow = VerticalWrapMode.Overflow;

			offsetCountLE.ignoreLayout = true;

			//Text count
			GameObject textCount = new GameObject("text count");
			textCount.transform.parent = transform;
			RectTransform textCountRect = textCount.AddComponent<RectTransform>();
			Text textCountTXT = textCount.AddComponent<Text>();
			LayoutElement textCountLE = textCount.AddComponent<LayoutElement>();

			textCountRect.anchoredPosition = new Vector2(-300f, -46f);
			textCountTXT.font = textFont.font;
			textCountTXT.text = "Text Object Count";
			textCountTXT.fontSize = 32;
			textCountTXT.horizontalOverflow = HorizontalWrapMode.Overflow;
			textCountTXT.verticalOverflow = VerticalWrapMode.Overflow;

			textCountLE.ignoreLayout = true;

			//Text Total count
			GameObject textLengthCount = new GameObject("texttotal count");
			textLengthCount.transform.parent = transform;
			RectTransform textLengthCountRect = textLengthCount.AddComponent<RectTransform>();
			Text textLengthCountTXT = textLengthCount.AddComponent<Text>();
			LayoutElement textLengthCountLE = textLengthCount.AddComponent<LayoutElement>();

			textLengthCountRect.anchoredPosition = new Vector2(-300f, -76f);
			textLengthCountTXT.font = textFont.font;
			textLengthCountTXT.text = "Text Total Count";
			textLengthCountTXT.fontSize = 32;
			textLengthCountTXT.horizontalOverflow = HorizontalWrapMode.Overflow;
			textLengthCountTXT.verticalOverflow = VerticalWrapMode.Overflow;

			textLengthCountLE.ignoreLayout = true;

			//Layer count
			GameObject layerCount = new GameObject("layer count");
			layerCount.transform.parent = transform;
			RectTransform layerCountRect = layerCount.AddComponent<RectTransform>();
			Text layerCountTXT = layerCount.AddComponent<Text>();
			LayoutElement layerCountLE = layerCount.AddComponent<LayoutElement>();

			layerCountRect.anchoredPosition = new Vector2(-300f, -106f);
			layerCountTXT.font = textFont.font;
			layerCountTXT.text = "Object Layer Count";
			layerCountTXT.fontSize = 32;
			layerCountTXT.horizontalOverflow = HorizontalWrapMode.Overflow;
			layerCountTXT.verticalOverflow = VerticalWrapMode.Overflow;

			layerCountLE.ignoreLayout = true;

			//Marker count
			GameObject markerCount = new GameObject("marker count");
			markerCount.transform.parent = transform;
			RectTransform markerCountRect = markerCount.AddComponent<RectTransform>();
			Text markerCountTXT = markerCount.AddComponent<Text>();
			LayoutElement markerCountLE = markerCount.AddComponent<LayoutElement>();

			markerCountRect.anchoredPosition = new Vector2(-300f, -136f);
			markerCountTXT.font = textFont.font;
			markerCountTXT.text = "Marker Count";
			markerCountTXT.fontSize = 32;
			markerCountTXT.horizontalOverflow = HorizontalWrapMode.Overflow;
			markerCountTXT.verticalOverflow = VerticalWrapMode.Overflow;

			markerCountLE.ignoreLayout = true;

			//Time count
			GameObject timeCount = new GameObject("time count");
			timeCount.transform.parent = transform;
			RectTransform timeCountRect = timeCount.AddComponent<RectTransform>();
			Text timeCountTXT = timeCount.AddComponent<Text>();
			LayoutElement timeCountLE = timeCount.AddComponent<LayoutElement>();

			timeCountRect.anchoredPosition = new Vector2(-300f, -226f);
			timeCountTXT.font = textFont.font;
			timeCountTXT.text = "Time Count";
			timeCountTXT.fontSize = 32;
			timeCountTXT.horizontalOverflow = HorizontalWrapMode.Overflow;
			timeCountTXT.verticalOverflow = VerticalWrapMode.Overflow;

			timeCountLE.ignoreLayout = true;

			//Range count
			GameObject rangeCount = new GameObject("range count");
			rangeCount.transform.parent = transform;
			RectTransform rangeCountRect = rangeCount.AddComponent<RectTransform>();
			Text rangeCountTXT = rangeCount.AddComponent<Text>();
			LayoutElement rangeCountLE = rangeCount.AddComponent<LayoutElement>();

			rangeCountRect.anchoredPosition = new Vector2(-300f, -166f);
			rangeCountTXT.font = textFont.font;
			rangeCountTXT.text = "Range Count";
			rangeCountTXT.fontSize = 32;
			rangeCountTXT.horizontalOverflow = HorizontalWrapMode.Overflow;
			rangeCountTXT.verticalOverflow = VerticalWrapMode.Overflow;

			rangeCountLE.ignoreLayout = true;

			//OnScreen count
			GameObject onScreenCount = new GameObject("onscreen count");
			onScreenCount.transform.parent = transform;
			RectTransform onScreenCountRect = onScreenCount.AddComponent<RectTransform>();
			Text onScreenCountTXT = onScreenCount.AddComponent<Text>();
			LayoutElement onScreenCountLE = onScreenCount.AddComponent<LayoutElement>();

			onScreenCountRect.anchoredPosition = new Vector2(-300f, -196f);
			onScreenCountTXT.font = textFont.font;
			onScreenCountTXT.text = "OnScreen Count";
			onScreenCountTXT.fontSize = 32;
			onScreenCountTXT.horizontalOverflow = HorizontalWrapMode.Overflow;
			onScreenCountTXT.verticalOverflow = VerticalWrapMode.Overflow;

			onScreenCountLE.ignoreLayout = true;

			//SongPercent count
			GameObject songPercentCount = new GameObject("songpercent count");
			songPercentCount.transform.parent = transform;
			RectTransform songPercentCountRect = songPercentCount.AddComponent<RectTransform>();
			Text songPercentCountTXT = songPercentCount.AddComponent<Text>();
			LayoutElement songPercentCountLE = songPercentCount.AddComponent<LayoutElement>();

			songPercentCountRect.anchoredPosition = new Vector2(-300f, -256f);
			songPercentCountTXT.font = textFont.font;
			songPercentCountTXT.text = "SongPercent Count";
			songPercentCountTXT.fontSize = 32;
			songPercentCountTXT.horizontalOverflow = HorizontalWrapMode.Overflow;
			songPercentCountTXT.verticalOverflow = VerticalWrapMode.Overflow;

			songPercentCountLE.ignoreLayout = true;

			//Doggo
			GameObject loadingDoggo = new GameObject("loading doggo");
			loadingDoggo.transform.parent = transform;
			RectTransform loadingDoggoRect = loadingDoggo.AddComponent<RectTransform>();
			loadingDoggo.AddComponent<CanvasRenderer>();
			Image loadingDoggoImage = loadingDoggo.AddComponent<Image>();
			LayoutElement loadingDoggoLE = loadingDoggo.AddComponent<LayoutElement>();

			loadingDoggoRect.anchoredPosition = new Vector2(UnityEngine.Random.Range(-320f, 320f), UnityEngine.Random.Range(-300f, -275f));
			float sizeRandom = 64f * UnityEngine.Random.Range(0.5f, 1f);
			loadingDoggoRect.sizeDelta = new Vector2(sizeRandom, sizeRandom);

			loadingDoggoLE.ignoreLayout = true;
		}

		[HarmonyPatch(typeof(SettingEditor), "Update")]
		[HarmonyPostfix]
		private static void SettingsUpdate()
		{
			if (EditorManager.inst.isEditing == true && EditorManager.inst.hasLoadedLevel && EditorManager.inst != null)
			{
				if (GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog") && GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog").activeSelf == true)
                {
					//Create Local Variables
					Transform transform = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog").transform;
					int eventnum = 0;
					int autokillnum = 0;
					int offsetnum = 0;
					int textnum = 0;
					int texttotalnum = 0;
					int layernum = 0;
					int onscreennum = 0;

					int posnum = 0;
					float camPosX = 1.775f * EventManager.inst.camZoom + EventManager.inst.camPos.x;
					float camPosY = 1f * EventManager.inst.camZoom + EventManager.inst.camPos.y;

					if (DataManager.inst.gameData.beatmapObjects.Count > 0 && DataManager.inst.gameData.eventObjects.allEvents.Count > 0 && EditorManager.inst.hasLoadedLevel)
					{
						var ae = new DataManager.GameData.EventObjects();

						foreach (var keyframes in ae.allEvents)
						{
							eventnum = keyframes.Count;
						}

						eventnum += EventEditor.inst.eventObjects[0].Count + EventEditor.inst.eventObjects[1].Count + EventEditor.inst.eventObjects[2].Count + EventEditor.inst.eventObjects[3].Count + EventEditor.inst.eventObjects[4].Count + EventEditor.inst.eventObjects[5].Count + EventEditor.inst.eventObjects[6].Count + EventEditor.inst.eventObjects[7].Count + EventEditor.inst.eventObjects[8].Count + EventEditor.inst.eventObjects[9].Count;

						foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
						{
							if (beatmapObject.autoKillType == DataManager.GameData.BeatmapObject.AutoKillType.OldStyleNoAutokill)
							{
								autokillnum += 1;
							}

							if (beatmapObject.autoKillOffset >= AudioManager.inst.CurrentAudioSource.clip.length)
							{
								offsetnum += 1;
							}

							if (beatmapObject.shape == 4)
							{
								textnum += 1;
							}

							if (beatmapObject.editorData.Layer == EditorManager.inst.layer)
							{
								layernum += 1;
							}

							if (AudioManager.inst.CurrentAudioSource.time >= beatmapObject.StartTime && AudioManager.inst.CurrentAudioSource.time <= beatmapObject.StartTime + beatmapObject.autoKillOffset && beatmapObject.autoKillType != DataManager.GameData.BeatmapObject.AutoKillType.OldStyleNoAutokill)
							{
								onscreennum += 1;
							}
							if (AudioManager.inst.CurrentAudioSource.time >= beatmapObject.StartTime && beatmapObject.autoKillType == DataManager.GameData.BeatmapObject.AutoKillType.OldStyleNoAutokill)
							{
								onscreennum += 1;
							}

							foreach (var keyframe in beatmapObject.events[0])
							{
								if (keyframe.eventValues[0] > camPosX || keyframe.eventValues[0] < -camPosX || keyframe.eventValues[1] > camPosY || keyframe.eventValues[1] < -camPosY)
								{
									posnum += 1;
								}
							}

							texttotalnum += beatmapObject.text.Length;
						}

						int songPercent = (int)(AudioManager.inst.CurrentAudioSource.time / AudioManager.inst.CurrentAudioSource.clip.length * 100);

						string timeString = secondsToTime(itsTheTime);

						transform.Find("object count").GetComponent<Text>().text = "Object Count: [ " + DataManager.inst.gameData.beatmapObjects.Count.ToString() + " ]";
						transform.Find("event count").GetComponent<Text>().text = "Event Count: [ " + eventnum.ToString() + " ]";
						transform.Find("theme count").GetComponent<Text>().text = "Theme Count: [ " + DataManager.inst.CustomBeatmapThemes.Count.ToString() + " ]";
						transform.Find("prefabex count").GetComponent<Text>().text = "Prefab External Count: [ " + PrefabEditor.inst.LoadedPrefabs.Count.ToString() + " ]";
						transform.Find("prefabin count").GetComponent<Text>().text = "Prefab Internal Count: [ " + DataManager.inst.gameData.prefabs.Count.ToString() + " ]";
						transform.Find("noautokill count").GetComponent<Text>().text = "No Autokill Count: [ " + autokillnum.ToString() + " ]";
						transform.Find("offset count").GetComponent<Text>().text = "KFOffsets > Song Length Count: [ " + offsetnum.ToString() + " ]";
						transform.Find("text count").GetComponent<Text>().text = "Text Object Count: [ " + textnum.ToString() + " ]";
						transform.Find("texttotal count").GetComponent<Text>().text = "Text Symbol Total Count: [ " + texttotalnum.ToString() + " ]";
						transform.Find("layer count").GetComponent<Text>().text = "Objects in Current Layer Count: [ " + layernum.ToString() + " ]";
						transform.Find("marker count").GetComponent<Text>().text = "Markers Count: [ " + DataManager.inst.gameData.beatmapData.markers.Count.ToString() + " ]";
						transform.Find("time count").GetComponent<Text>().text = "Time in Editor: [ " + timeString + " ]";
						transform.Find("range count").GetComponent<Text>().text = "Objects Outside Camera Count: [ " + posnum.ToString() + " ]";
						transform.Find("onscreen count").GetComponent<Text>().text = "Objects Alive Count: [ " + onscreennum.ToString() + " ]";
						transform.Find("songpercent count").GetComponent<Text>().text = "Song progress: [ " + songPercent.ToString() + "% ]";
						transform.Find("loading doggo").GetComponent<Image>().sprite = EditorManager.inst.loadingImage.sprite;
						if (EditorManager.inst.loading)
						{
							timeEdit = 0f;
						}
					}
				}
			}
		}

		[HarmonyPatch(typeof(SettingEditor), "Render")]
		[HarmonyPostfix]
		private static void SetDoggo()
        {
			EditorManager.inst.CancelInvoke("LoadingIconUpdate");
			EditorManager.inst.InvokeRepeating("LoadingIconUpdate", 0f, UnityEngine.Random.Range(0.01f, 0.4f));

			Transform transform = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog").transform;
			RectTransform loadingDoggoRect = transform.Find("loading doggo").GetComponent<RectTransform>();

			loadingDoggoRect.anchoredPosition = new Vector2(UnityEngine.Random.Range(-320f, 320f), UnityEngine.Random.Range(-300f, -275f));
			float sizeRandom = 64 * UnityEngine.Random.Range(0.5f, 1f);
			loadingDoggoRect.sizeDelta = new Vector2(sizeRandom, sizeRandom);

			transform.Find("snap/bpm/slider").GetComponent<Slider>().onValueChanged.RemoveAllListeners();
			transform.Find("snap/bpm/slider").GetComponent<Slider>().onValueChanged.AddListener(delegate (float _val)
			{
				DataManager.inst.metaData.song.BPM = _val;
				SettingEditor.inst.SnapBPM = _val;
				transform.Find("snap/bpm/input").GetComponent<InputField>().text = SettingEditor.inst.SnapBPM.ToString();
			});
			transform.Find("snap/bpm/input").GetComponent<InputField>().onValueChanged.RemoveAllListeners();
			transform.Find("snap/bpm/input").GetComponent<InputField>().onValueChanged.AddListener(delegate (string _val)
			{
				DataManager.inst.metaData.song.BPM = float.Parse(_val);
				SettingEditor.inst.SnapBPM = float.Parse(_val);
				transform.Find("snap/bpm/slider").GetComponent<Slider>().value = SettingEditor.inst.SnapBPM;
			});
			transform.Find("snap/bpm/<").GetComponent<Button>().onClick.RemoveAllListeners();
			transform.Find("snap/bpm/<").GetComponent<Button>().onClick.AddListener(delegate ()
			{
				DataManager.inst.metaData.song.BPM -= 1f;
				SettingEditor.inst.SnapBPM -= 1f;
				transform.Find("snap/bpm/input").GetComponent<InputField>().text = SettingEditor.inst.SnapBPM.ToString();
				transform.Find("snap/bpm/slider").GetComponent<Slider>().value = SettingEditor.inst.SnapBPM;
			});
			transform.Find("snap/bpm/>").GetComponent<Button>().onClick.RemoveAllListeners();
			transform.Find("snap/bpm/>").GetComponent<Button>().onClick.AddListener(delegate ()
			{
				DataManager.inst.metaData.song.BPM += 1f;
				SettingEditor.inst.SnapBPM += 1f;
				transform.Find("snap/bpm/input").GetComponent<InputField>().text = SettingEditor.inst.SnapBPM.ToString();
				transform.Find("snap/bpm/slider").GetComponent<Slider>().value = SettingEditor.inst.SnapBPM;
			});
		}

		//Set Time Stuff
		[HarmonyPatch(typeof(EditorManager), "Awake")]
		[HarmonyPostfix]
		public static void SetEditorAwake()
		{
			GameObject.Find("Editor Systems/EditorManager").AddComponent<RTEditor>();

			//Create Local Variables
			GameObject timeObj = GameObject.Find("TimelineBar/GameObject").transform.GetChild(13).gameObject;
			GameObject.Find("TimelineBar/GameObject").transform.GetChild(0).gameObject.SetActive(true);
			InputField iFtimeObj = timeObj.GetComponent<InputField>();
			timeObj.name = "Time Input";

			timeObj.transform.SetSiblingIndex(0);
			timeObj.SetActive(true);
			iFtimeObj.text = AudioManager.inst.CurrentAudioSource.time.ToString();
			iFtimeObj.characterValidation = InputField.CharacterValidation.Decimal;

			iFtimeObj.onValueChanged.AddListener(delegate (string _value)
			{
				SetNewTime(_value);
			});

			timeObj.AddComponent<EventTrigger>();

			GameObject tbarLayers = UnityEngine.Object.Instantiate<GameObject>(GameObject.Find("TimelineBar/GameObject/Time Input"));

			tbarLayers.transform.SetParent(GameObject.Find("TimelineBar/GameObject").transform);
			tbarLayers.name = "layers";
			tbarLayers.transform.SetSiblingIndex(8);
			InputField tbarLayersIF = tbarLayers.GetComponent<InputField>();
			tbarLayers.transform.Find("Text").gameObject.GetComponent<Text>().alignment = UnityEngine.TextAnchor.MiddleCenter;
			if (EditorManager.inst.layer < 5)
            {
				float l = EditorManager.inst.layer + 1;
				tbarLayersIF.text = l.ToString();
			}
			else
            {
				int l = EditorManager.inst.layer + 2;
				tbarLayersIF.text = l.ToString();
			}
			tbarLayersIF.characterValidation = InputField.CharacterValidation.Integer;
			tbarLayersIF.onValueChanged.RemoveAllListeners();
			tbarLayersIF.onValueChanged.AddListener(delegate (string _value)
			{
				if (int.Parse(_value) > 0)
                {
					if (int.Parse(_value) < 6)
					{
						Plugin.SetLayer(int.Parse(_value) - 1);
					}
					else
					{
						Plugin.SetLayer(int.Parse(_value));
					}
				}
				else
                {
					Plugin.SetLayer(0);
					tbarLayersIF.text = "1";
				}
			});
			Image layerImage = tbarLayers.GetComponent<Image>();
			layerImage.color = EditorManager.inst.layerColors[0];

			GameObject.Find("TimelineBar/GameObject/1").SetActive(false);
			GameObject.Find("TimelineBar/GameObject/2").SetActive(false);
			GameObject.Find("TimelineBar/GameObject/3").SetActive(false);
			GameObject.Find("TimelineBar/GameObject/4").SetActive(false);
			GameObject.Find("TimelineBar/GameObject/5").SetActive(false);

			EventTrigger eventTrigger = GameObject.Find("TimelineBar/GameObject/6").GetComponent<EventTrigger>();

			EventTrigger.Entry entryEvent = new EventTrigger.Entry();
			entryEvent.eventID = EventTriggerType.PointerClick;
			entryEvent.callback.AddListener(delegate (BaseEventData eventData)
			{
				PointerEventData pointerEventData = (PointerEventData)eventData;
				if (pointerEventData.clickTime > 0f)
                {
					Plugin.SetLayer(5);
				}
			});
			eventTrigger.triggers.Clear();
			eventTrigger.triggers.Add(entryEvent);

			GameObject.Find("TimelineBar/GameObject/checkpoint").SetActive(false);

			GameObject.Find("Editor GUI/sizer/main/Popups/Save As Popup/New File Popup/level-name").GetComponent<InputField>().characterValidation = InputField.CharacterValidation.None;
			GameObject.Find("Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/left/theme/name").GetComponent<InputField>().characterValidation = InputField.CharacterValidation.None;
			GameObject.Find("Editor GUI/sizer/main/EditorDialogs/PrefabDialog/data/name/input").GetComponent<InputField>().characterValidation = InputField.CharacterValidation.None;
			GameObject.Find("Editor GUI/sizer/main/Popups/New File Popup/Browser Popup").SetActive(true);
			GameObject.Find("Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/name/name").GetComponent<InputField>().characterLimit = 0;

			EventTrigger.Entry entryMoveX = new EventTrigger.Entry();
			entryMoveX.eventID = EventTriggerType.Scroll;
			entryMoveX.callback.AddListener(delegate (BaseEventData eventData)
			{
				float time = AudioManager.inst.CurrentAudioSource.time;
				float timeModify = TimeModify.Value;
				float timeMinus = time - timeModify;
				float timePlus = time + timeModify;

				PointerEventData pointerEventData = (PointerEventData)eventData;
				if (pointerEventData.scrollDelta.y < 0f)
				{
					Plugin.SetNewTime(timeMinus.ToString());
					iFtimeObj.text = time.ToString();
					return;
				}
				if (pointerEventData.scrollDelta.y > 0f)
				{
					Plugin.SetNewTime(timePlus.ToString());
					iFtimeObj.text = time.ToString();
				}
			});
			timeObj.gameObject.GetComponent<EventTrigger>().triggers.Clear();
			timeObj.gameObject.GetComponent<EventTrigger>().triggers.Add(entryMoveX);

			//Loading doggo
			GameObject loading = new GameObject("loading");
			GameObject popup = GameObject.Find("Editor Systems/Editor GUI/sizer/main/Popups");
			Transform fileInfoPopup = popup.transform.GetChild(1);

			loading.transform.SetParent(fileInfoPopup);
			loading.layer = 5;

			RectTransform rtLoading = loading.AddComponent<RectTransform>();
			loading.AddComponent<CanvasRenderer>();
			Image iLoading = loading.AddComponent<Image>();
			LayoutElement leLoading = loading.AddComponent<LayoutElement>();

			rtLoading.anchoredPosition = new Vector2(0f, -60f);
			rtLoading.sizeDelta = new Vector2(122f, 122f);
			iLoading.sprite = EditorManager.inst.loadingImage.sprite;
			leLoading.ignoreLayout = true;

			fileInfoPopup.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(500f, 280f);

			//Tooltips
			HoverTooltip depthTip = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/depth/depth").GetComponent<HoverTooltip>();

			HoverTooltip.Tooltip tooltipDepth = new HoverTooltip.Tooltip();
			tooltipDepth.desc = "Set the depth layer of the object.";
			tooltipDepth.hint = "Depth is if an object shows above or below another object. However, higher number does not equal higher depth here since it's reversed.<br>Higher number = lower depth<br>Lower number = higher depth.";

			depthTip.tooltipLangauges.Add(tooltipDepth);


			HoverTooltip timelineTip = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/timeline").GetComponent<HoverTooltip>();

			HoverTooltip.Tooltip tooltipTimeline = new HoverTooltip.Tooltip();
			tooltipTimeline.desc = "Create a keyframe in one of the four keyframe bins by right clicking.";
			tooltipTimeline.hint = "Each keyframe is located here.";

			timelineTip.tooltipLangauges.Add(tooltipTimeline);

			HoverTooltip textShapeTip = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/shapesettings/5").GetComponent<HoverTooltip>();

			HoverTooltip.Tooltip tooltipTextShape = new HoverTooltip.Tooltip();
			tooltipTextShape.desc = "Write your custom text here.";
			tooltipTextShape.hint = "Anything you write here will show up as a text object. There are a lot of formatting options, such as < b >, < i >, < br >, < color = #FFFFFF > < alpha = #FF > and more. (of course without the spaces between)";

			textShapeTip.tooltipLangauges.Add(tooltipTextShape);

			var fileInfoPopup1 = EditorManager.inst.EditorDialogs[7].Dialog;
			fileInfoPopup1.gameObject.GetComponent<Image>().sprite = null;

			var fileInfoPopup2 = EditorManager.inst.EditorDialogs[1].Dialog;
			Sprite sprite = fileInfoPopup2.transform.Find("data/left/Scroll View/Viewport/Content/shape/7/Background").GetComponent<Image>().sprite;

			GameObject sortList = UnityEngine.Object.Instantiate<GameObject>(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/autokill/tod-dropdown"));
			sortList.transform.SetParent(EditorManager.inst.GetDialog("Open File Popup").Dialog);

			RectTransform sortListRT = sortList.GetComponent<RectTransform>();
			sortListRT.anchoredPosition = ORLDropdownPos.Value;
			HoverTooltip sortListTip = sortList.GetComponent<HoverTooltip>();

			sortListTip.tooltipLangauges[0].desc = "Sort the order of your levels.";
			sortListTip.tooltipLangauges[0].hint = "<b>Cover</b> Sort by if level has a set cover. (Default)" +
				"<br><b>Artist</b> Sort by song artist." +
				"<br><b>Creator</b> Sort by level creator." +
				"<br><b>Folder</b> Sort by level folder name." +
				"<br><b>Title</b> Sort by song title." +
				"<br><b>Difficulty</b> Sort by level difficulty." +
				"<br><b>Date Edited</b> Sort by date edited / created.";

            Dropdown sortListDD = sortList.GetComponent<Dropdown>();
			UnityEngine.Object.Destroy(sortList.GetComponent<HideDropdownOptions>());
			sortListDD.options.Clear();
			sortListDD.onValueChanged.RemoveAllListeners();

			Dropdown.OptionData opt0 = new Dropdown.OptionData();
			Dropdown.OptionData opt1 = new Dropdown.OptionData();
			Dropdown.OptionData opt2 = new Dropdown.OptionData();
			Dropdown.OptionData opt3 = new Dropdown.OptionData();
			Dropdown.OptionData opt4 = new Dropdown.OptionData();
			Dropdown.OptionData opt5 = new Dropdown.OptionData();
			Dropdown.OptionData opt6 = new Dropdown.OptionData();

			opt0.text = "Cover";
			opt1.text = "Artist";
			opt2.text = "Creator";
			opt3.text = "Folder";
			opt4.text = "Title";
			opt5.text = "Difficulty";
			opt6.text = "Date Edited";

			sortListDD.options = new List<Dropdown.OptionData>
			{
				opt0,
				opt1,
				opt2,
				opt3,
				opt4,
				opt5,
				opt6
			};

			sortListDD.onValueChanged.AddListener(delegate (int _value)
			{
				levelFilter = _value;
				EditorManager.inst.RenderOpenBeatmapPopup();
			});

			GameObject checkDes = UnityEngine.Object.Instantiate<GameObject>(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/toggle"));
			checkDes.transform.SetParent(EditorManager.inst.GetDialog("Open File Popup").Dialog);
			GameObject toggle = checkDes.transform.Find("toggle").gameObject;

			RectTransform checkDesRT = checkDes.GetComponent<RectTransform>();
			checkDesRT.anchoredPosition = ORLTogglePos.Value;

			checkDes.transform.Find("title").GetComponent<Text>().text = "Descending?";
			RectTransform titleRT = checkDes.transform.Find("title").GetComponent<RectTransform>();
			titleRT.sizeDelta = new Vector2(110f, 32f);
			toggle.GetComponent<Toggle>().isOn = true;
			toggle.GetComponent<Toggle>().onValueChanged.AddListener(delegate (bool _value)
			{
				levelAscend = _value;
				EditorManager.inst.RenderOpenBeatmapPopup();
			});
			HoverTooltip toggleTip = toggle.AddComponent<HoverTooltip>();
			toggleTip.tooltipLangauges.Add(sortListTip.tooltipLangauges[0]);

			//Level List
			{
				if (RTFile.FileExists(RTFile.GetApplicationDirectory() + "beatmaps/editorpath.lss"))
				{
					string rawProfileJSON = null;
					rawProfileJSON = FileManager.inst.LoadJSONFile("beatmaps/editorpath.lss");

					JSONNode jsonnode = JSON.Parse(rawProfileJSON);

					editorPath = jsonnode["path"];
					levelListPath = "beatmaps/" + editorPath;
					levelListSlash = "beatmaps/" + editorPath + "/";
				}
				else
				{
					JSONNode jsonnode = JSON.Parse("{}");

					jsonnode["path"] = editorPath;

					RTFile.WriteToFile("beatmaps/editorpath.lss", jsonnode.ToString(3));
				}

				GameObject levelListObj = UnityEngine.Object.Instantiate<GameObject>(GameObject.Find("TimelineBar/GameObject/Time Input"));
				levelListObj.transform.SetParent(EditorManager.inst.GetDialog("Open File Popup").Dialog);
				levelListObj.GetComponent<RectTransform>().anchoredPosition = ORLPathPos.Value;
				levelListObj.GetComponent<RectTransform>().sizeDelta = new Vector2(ORLPathLength.Value, 34f);
				levelListObj.name = "editor path";

				HoverTooltip levelListTip = levelListObj.AddComponent<HoverTooltip>();
				HoverTooltip.Tooltip llTip = new HoverTooltip.Tooltip();

				llTip.desc = "Level list path";
				llTip.hint = "Input the path you want to load levels from within the beatmaps folder. For example: inputting \"editor\" into the input field will load levels from beatmaps/editor. You can also set it to sub-directories, like: \"editor/pa levels\" will take levels from \"beatmaps/editor/pa levels\".";

				levelListTip.tooltipLangauges.Add(llTip);

				InputField levelListIF = levelListObj.GetComponent<InputField>();
				levelListIF.characterValidation = InputField.CharacterValidation.None;
				levelListIF.text = editorPath;

				levelListIF.onValueChanged.RemoveAllListeners();
				levelListIF.onValueChanged.AddListener(delegate (string _val)
				{
					if (RTFile.FileExists(RTFile.GetApplicationDirectory() + "beatmaps/editorpath.lss"))
					{
						string rawProfileJSON = null;
						rawProfileJSON = FileManager.inst.LoadJSONFile("beatmaps/editorpath.lss");

						JSONNode jsonnode = JSON.Parse(rawProfileJSON);

						jsonnode["path"] = _val;

						RTFile.WriteToFile("beatmaps/editorpath.lss", jsonnode.ToString(3));
					}

					editorPath = _val;
					levelListPath = "beatmaps/" + editorPath;
					levelListSlash = "beatmaps/" + editorPath + "/";
				});

				GameObject levelListReloader = UnityEngine.Object.Instantiate<GameObject>(GameObject.Find("TimelineBar/GameObject/play"));
				levelListReloader.transform.SetParent(EditorManager.inst.GetDialog("Open File Popup").Dialog);
				levelListReloader.GetComponent<RectTransform>().anchoredPosition = ORLRefreshPos.Value;
				levelListReloader.GetComponent<RectTransform>().sizeDelta = new Vector2(32f, 32f);
				levelListReloader.name = "reload";

				HoverTooltip levelListRTip = levelListReloader.AddComponent<HoverTooltip>();
				HoverTooltip.Tooltip llRTip = new HoverTooltip.Tooltip();

				llRTip.desc = "Refresh level list";
				llRTip.hint = "Clicking this will reload the level list.";

				levelListRTip.tooltipLangauges.Add(llRTip);

				Button levelListRButton = levelListReloader.GetComponent<Button>();
				levelListRButton.onClick.m_Calls.m_ExecutingCalls.Clear();
				levelListRButton.onClick.m_Calls.m_PersistentCalls.Clear();
				levelListRButton.onClick.m_PersistentCalls.m_Calls.Clear();
				levelListRButton.onClick.RemoveAllListeners();
				levelListRButton.onClick.AddListener(delegate ()
				{
					EditorManager.inst.GetLevelList();
				});

				string jpgFileLocation = RTFile.GetApplicationDirectory() + "BepInEx/plugins/Assets/editor_gui_refresh-white.png";

				if (RTFile.FileExists("BepInEx/plugins/Assets/editor_gui_refresh-white.png"))
				{
					Image spriteReloader = levelListReloader.GetComponent<Image>();

					EditorManager.inst.StartCoroutine(EditorManager.inst.GetSprite(jpgFileLocation, new EditorManager.SpriteLimits(), delegate (Sprite cover)
					{
						spriteReloader.sprite = cover;
					}, delegate (string errorFile)
					{
						spriteReloader.sprite = ArcadeManager.inst.defaultImage;
					}));
				}
			}

			//New triggers
			{
				EventTrigger.Entry entry3 = new EventTrigger.Entry();
				entry3.eventID = EventTriggerType.EndDrag;
				entry3.callback.AddListener(delegate (BaseEventData eventData)
				{
					PointerEventData pointerEventData = (PointerEventData)eventData;
					EditorManager.inst.DragEndPos = pointerEventData.position;
					EditorManager.inst.SelectionBoxImage.gameObject.SetActive(false);
					if (EditorManager.inst.layer != 5)
					{
						List<ObjEditor.ObjectSelection> list4 = new List<ObjEditor.ObjectSelection>();
						foreach (ObjEditor.ObjectSelection objectSelection in ObjEditor.inst.selectedObjects)
						{
							list4.Add(new ObjEditor.ObjectSelection(objectSelection.Type, objectSelection.ID));
						}
						ObjEditor.inst.selectedObjects.Clear();
						foreach (KeyValuePair<string, GameObject> keyValuePair in ObjEditor.inst.beatmapObjects)
						{
							if (EditorManager.RectTransformToScreenSpace(EditorManager.inst.SelectionBoxImage.rectTransform).Overlaps(EditorManager.RectTransformToScreenSpace(keyValuePair.Value.GetComponent<Image>().rectTransform)) && keyValuePair.Value.activeSelf)
							{
								ObjEditor.inst.AddSelectedObject(new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Object, keyValuePair.Key));
							}
						}
						foreach (KeyValuePair<string, GameObject> keyValuePair2 in ObjEditor.inst.prefabObjects)
						{
							if (EditorManager.RectTransformToScreenSpace(EditorManager.inst.SelectionBoxImage.rectTransform).Overlaps(EditorManager.RectTransformToScreenSpace(keyValuePair2.Value.GetComponent<Image>().rectTransform)) && keyValuePair2.Value.activeSelf)
							{
								ObjEditor.inst.AddSelectedObject(new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Prefab, keyValuePair2.Key));
							}
						}
						foreach (ObjEditor.ObjectSelection obj in list4)
						{
							ObjEditor.inst.RenderTimelineObject(obj);
						}
						foreach (ObjEditor.ObjectSelection obj2 in ObjEditor.inst.selectedObjects)
						{
							ObjEditor.inst.RenderTimelineObject(obj2);
						}
						if (ObjEditor.inst.selectedObjects.Count<ObjEditor.ObjectSelection>() <= 0)
						{
							CheckpointEditor.inst.SetCurrentCheckpoint(0);
							return;
						}
					}
					else
					{
						bool flag = false;
						int num2 = 0;
						foreach (List<GameObject> list5 in EventEditor.inst.eventObjects)
						{
							int num3 = 0;
							foreach (GameObject gameObject2 in list5)
							{
								if (EditorManager.RectTransformToScreenSpace(EditorManager.inst.SelectionBoxImage.rectTransform).Overlaps(EditorManager.RectTransformToScreenSpace(gameObject2.transform.GetChild(0).GetComponent<Image>().rectTransform)) && gameObject2.activeSelf)
								{
									if (!flag)
									{
										EventEditor.inst.SetCurrentEvent(num2, num3);
										flag = true;
									}
									else
									{
										EventEditor.inst.AddedSelectedEvent(num2, num3);
									}
								}
								num3++;
							}
							num2++;
						}
					}
				});
				EventTrigger tltrig = EditorManager.inst.timeline.GetComponent<EventTrigger>();

				tltrig.triggers.RemoveAt(3);
				tltrig.triggers.Add(entry3);

				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("persistent/text").gameObject.GetComponent<Text>().text = "No Autokill";
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("persistent").gameObject.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("persistent").gameObject.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("persistent").gameObject.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("persistent").gameObject.GetComponent<Button>().onClick.RemoveAllListeners();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("persistent").gameObject.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					Plugin.CreateNewNoAutokillObject();
				});

				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("empty").gameObject.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("empty").gameObject.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("empty").gameObject.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("empty").gameObject.GetComponent<Button>().onClick.RemoveAllListeners();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("empty").gameObject.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					Plugin.CreateNewEmptyObject();
				});

				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("decoration").gameObject.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("decoration").gameObject.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("decoration").gameObject.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("decoration").gameObject.GetComponent<Button>().onClick.RemoveAllListeners();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("decoration").gameObject.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					Plugin.CreateNewDecorationObject();
				});

				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("helper").gameObject.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("helper").gameObject.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("helper").gameObject.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("helper").gameObject.GetComponent<Button>().onClick.RemoveAllListeners();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("helper").gameObject.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					Plugin.CreateNewHelperObject();
				});

				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("normal").gameObject.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("normal").gameObject.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("normal").gameObject.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("normal").gameObject.GetComponent<Button>().onClick.RemoveAllListeners();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("normal").gameObject.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					Plugin.CreateNewNormalObject();
				});

				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/circle").gameObject.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/circle").gameObject.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/circle").gameObject.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/circle").gameObject.GetComponent<Button>().onClick.RemoveAllListeners();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/circle").gameObject.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					Plugin.CreateNewCircleObject();
				});

				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/triangle").gameObject.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/triangle").gameObject.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/triangle").gameObject.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/triangle").gameObject.GetComponent<Button>().onClick.RemoveAllListeners();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/triangle").gameObject.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					Plugin.CreateNewTriangleObject();
				});

				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/text").gameObject.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/text").gameObject.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/text").gameObject.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/text").gameObject.GetComponent<Button>().onClick.RemoveAllListeners();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/text").gameObject.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					Plugin.CreateNewTextObject();
				});

				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/hexagon").gameObject.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/hexagon").gameObject.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/hexagon").gameObject.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/hexagon").gameObject.GetComponent<Button>().onClick.RemoveAllListeners();
				EditorManager.inst.GetDialog("Object Options Popup").Dialog.Find("shapes/hexagon").gameObject.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					Plugin.CreateNewHexagonObject();
				});
			}

			GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/Help/Help Dropdown/Join Discord/Text").GetComponent<Text>().text = "Modder's Discord";
			GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/Help/Help Dropdown/Watch Tutorials/Text").GetComponent<Text>().text = "Watch PA History";
			GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/Help/Help Dropdown/Community Guides").SetActive(false);
			GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/Help/Help Dropdown/Which songs can I use?").SetActive(false);
			GameObject.Find("TitleBar/File/File Dropdown/Save As").SetActive(true);
		}

        public static void CreateNewNormalObject(bool _select = true)
		{
			ObjEditor.ObjectSelection tmpSelection = Plugin.CreateNewDefaultObject(_select);
			EditorManager.inst.history.Add(new History.Command("Create New Normal Object", delegate ()
			{
				Plugin.CreateNewDefaultObject(_select);
			}, delegate ()
			{
				ObjEditor.inst.DeleteObject(tmpSelection, true);
			}), false);
		}

		public static void CreateNewCircleObject(bool _select = true)
		{
			ObjEditor.ObjectSelection tmpSelection = Plugin.CreateNewDefaultObject(_select);
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].shape = 1;
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].shapeOption = 0;
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].name = "circle";
			ObjectManager.inst.updateObjects(tmpSelection, false);
			ObjEditor.inst.RenderTimelineObject(tmpSelection);
			EditorManager.inst.history.Add(new History.Command("Create New Normal Circle Object", delegate ()
			{
				Plugin.CreateNewCircleObject(_select);
			}, delegate ()
			{
				ObjEditor.inst.DeleteObject(tmpSelection, true);
			}), false);
		}

		public static void CreateNewTriangleObject(bool _select = true)
		{
			ObjEditor.ObjectSelection tmpSelection = Plugin.CreateNewDefaultObject(_select);
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].shape = 2;
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].shapeOption = 0;
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].name = "triangle";
			ObjectManager.inst.updateObjects(tmpSelection, false);
			ObjEditor.inst.RenderTimelineObject(tmpSelection);
			EditorManager.inst.history.Add(new History.Command("Create New Normal Triangle Object", delegate ()
			{
				Plugin.CreateNewTriangleObject(_select);
			}, delegate ()
			{
				ObjEditor.inst.DeleteObject(tmpSelection, true);
			}), false);
		}

		public static void CreateNewTextObject(bool _select = true)
		{
			ObjEditor.ObjectSelection tmpSelection = Plugin.CreateNewDefaultObject(_select);
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].shape = 4;
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].shapeOption = 0;
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].text = "<line-height=14.5><cspace=-0.65><#00000000>████████████████████████████████████████████████████████████████████████<br>████████████████████████████████████████████████████████████████████████<br>████████████████████████████████████████████████████████████████████████<br>████████████████████████████████████████████████████████████████████████<br>████████████████████████████████████████████████████████████████████████<br>████████████████████████████████████████████████████████████████████████<br>████████████████████████████████████████████████████████████████████████<br>████████████████████████████████████████████████████████████████████████<br>████████████████████████████████████████████████████████████████████████<br>████████████████████████████████████████████████████████████████████████<br>████████████████████████████████████████████████████████████████████████<br>████████████████████████████████████████████████████████████████████████<br>████████████████████████████████████████████████████████████████████████<br>███████████████████████<#00000002>█<#00000003>██<#00000002>█<#00000000>███████████████████<#00000002>█<#00000003>██<#00000001>█<#00000000>██████████████████████<br>█████████████████████<#00000001>█<#00000000>█████<#00000002>█<#00000000>██████████████████████<#00000002>█<#00000000>█████████████████████<br>████████████████████<#00000002>█<#00000000>██<#1010103e>█<#0e0e0e6e>█<#13131376>█<#0d0d0d27>█<#00000000>█<#00000001>█<#00000000>█████████████████<#0d0d0a4c>█<#1515157c>█<#08080862>█<#17171721>█<#00000000>██████████████████████<br>███████████████████<#00000002>█<#00000000>█<#1111111e>█<#010101cb>█<#010101ff>█<#050505ff>█<#000000ff>██<#09090955>█<#00000000>█<#00000002>█<#00000000>███████████████<#030303a0>█<#000000ff>█<#020202ff>██<#010101ff>█<#0505058e>█<#00000000>█████████████████████<br>████████████████████<#39393909>█<#0b0b0ba8>█<#09090994>█<#10101041>█<#39323224>█<#1818182b>█<#0f0f0f69>█<#0b0b0b73>█<#00000000>██████████████<#00000001>█<#00000000>█<#17171721>█<#09090987>█<#0d0d0a4c>█<#2d2d2d22>█<#2424242b>█<#09090955>█<#070707b6>█<#0e0e0e6e>█<#00000000>█<#00000002>█<#00000000>██████████████████<br>██████████████████<#00000001>█<#00000000>█<#21212127>█<#1818182b>█<#00000000>██<#0000003d>██<#00000000>██<#00000002>█<#00000000>███████████████<#00000001>█<#00000000>█<#33333305>█<#0606064f>█<#00000020>█<#00000000>██<#1010103e>█<#5555400c>█<#00000000>███████████████████<br>██████████████████████<#80808004>█<#000000a9>█<#181818ff>█<#1e1e1eff>█<#000000a9>█<#ffffff02>█<#00000000>███████████████<#00000002>█<#00000000>█<#0606062d>█<#0a0a0ae5>█<#3f3f3fff>█<#1c1b1bff>█<#00000060>█<#00000000>██<#00000001>█<#00000000>███████████████████<br>████████████████████<#00000003>█<#00000000>█<#04040474>█<#0b0b0bff>█<#666666fb>█<#dddddefc>█<#4f4f4fff>█<#00000076>█<#00000000>█<#00000003>█<#00000000>██████████████<#00000001>█<#010101cb>█<#212121ff>█<#aaaaaaf7>█<#d9dadaff>█<#1a1a19f7>█<#0d0d0d27>█<#00000000>█<#00000001>█<#00000000>███████████████████<br>███████████████████<#00000001>█<#00000000>█<#5555400c>█<#070605e0>█<#000000ff>█<#555555fd>█<#fffffffc>█<#d9dadaff>█<#050505e2>█<#3333330f>█<#00000000>█<#00000001>█<#00000000>███████████<#00000003>█<#00000000>█<#0606064f>█<#010101ff>█<#000000fb>█<#abababff>█<#fffffffb>█<#767676ff>█<#02020293>█<#00000000>█<#00000003>█<#00000000>███████████████████<br>███████████████████<#00000002>█<#00000000>█<#1815114a>█<#020202ff>█<#000000fd>█<#212121ff>█<#797979ff>█<#6f6f6ffc>█<#323232ff>█<#0000004c>█<#00000000>█<#00000003>█<#00000000>███████████<#00000002>█<#00000000>█<#0b0909a3>█<#000000ff>█<#000000fb>█<#3a3a3afe>█<#555555fd>█<#5b5b5bfe>█<#141313db>█<#39393909>█<#00000000>████████████████████<br>███████████████████<#00000003>█<#00000000>█<#100e0c80>█<#000000ff>█<#000000fb>█<#000000fe>██<#050505fb>█<#454545ff>█<#04040281>█<#00000000>█<#00000003>█<#00000000>████████████<#80808004>█<#020202d1>█<#000000ff>█<#000000fc>█<#000000fe>██<#2f2f2fff>█<#292828fb>█<#0606062d>█<#00000000>█<#00000002>█<#00000000>██████████████████<br>███████████████████<#00000002>█<#00000000>█<#0b0909a3>█<#000000ff>█<#000000fb>█<#000000fe>█<#050505ff>█<#000000fb>█<#2a2a2aff>█<#100e0da2>█<#00000000>█<#00000002>█<#00000000>██████████<#00000001>█<#00000000>█<#40404018>█<#040403e9>█<#000000ff>█<#000000fd>█<#000000fe>██<#111111fd>█<#212121ff>█<#0d0d0a4c>█<#00000000>█<#00000003>█<#00000000>██████████████████<br>███████████████████<#00000001>█<#00000000>█<#0b0909b2>█<#000000ff>█<#000000fc>█<#000000fe>██<#000000fb>█<#0b0b0bff>█<#131313b2>█<#00000000>█<#00000002>█<#00000000>██████████<#00000001>█<#00000000>█<#40404020>█<#050505f4>█<#000000ff>█<#000000fe>███<#000000fc>█<#0f0f0fff>█<#1916135d>█<#00000000>█<#00000003>█<#00000000>██████████████████<br>███████████████████<#00000001>█<#00000000>█<#0b0909b2>█<#000000ff>█<#000000fc>█<#000000fe>██<#000000fb>█<#050505ff>█<#131313b2>█<#00000000>█<#00000002>█<#00000000>██████████<#00000001>█<#00000000>█<#40404020>█<#050505f4>█<#000000ff>█<#000000fe>███<#000000fc>█<#050505ff>█<#1916135d>█<#00000000>█<#00000003>█<#00000000>██████████████████<br>███████████████████<#00000002>█<#00000000>█<#100e0da2>█<#2f2f2fff>█<#050505fb>█<#000000fe>██<#000000fb>█<#000000ff>█<#100e0da2>█<#00000000>█<#00000002>█<#00000000>██████████<#00000001>█<#00000000>█<#17171716>█<#1a1a1ae7>█<#323232ff>█<#000000fd>█<#000000fe>██<#000000fd>█<#020202ff>█<#2e24214d>█<#00000000>█<#00000003>█<#00000000>██████████████████<br>███████████████████<#00000003>█<#00000000>█<#100a0882>█<#767676ff>█<#1f1f1ffb>█<#000000fe>██<#000000fb>█<#000000ff>█<#0e0c0c7c>█<#00000000>█<#00000003>█<#00000000>████████████<#00000003>█<#2b2a29ce>█<#7c7c7dff>█<#000000fc>█<#000000fe>██<#000000ff>█<#040403fa>█<#4a403a30>█<#00000000>█<#00000002>█<#00000000>██████████████████<br>███████████████████<#00000002>█<#00000000>█<#0000004c>█<#818080ff>█<#5b5b5bfe>█<#000000fe>█<#050505ff>█<#000000fd>█<#010101ff>█<#221e1e44>█<#00000000>█<#00000002>█<#00000000>███████████<#00000002>█<#00000000>█<#1a19199b>█<#a6a6a6ff>█<#141414fb>█<#000000ff>█<#000000fd>█<#000000ff>█<#0a0807db>█<#eabfaa0c>█<#00000000>█<#00000001>█<#00000000>██████████████████<br>███████████████████<#00000001>█<#00000000>█<#20100010>█<#474443de>█<#979898ff>█<#000000fc>██<#000000ff>█<#0a0807db>█<#aa8e7109>█<#00000000>█████████████<#00000003>█<#00000000>█<#00000049>█<#797979ff>█<#5e5e5ffb>█<#000000fe>█<#040403fa>█<#000000ff>█<#211c1892>█<#00000000>█<#00000002>█<#00000000>███████████████████<br>████████████████████<#00000002>█<#00000000>█<#21160f74>█<#6a6a6afe>█<#3a3a3afe>█<#000000fd>█<#020202ff>█<#1f1d1a6b>█<#00000000>█<#00000003>█<#00000000>███████████████<#242120bf>█<#767676ff>█<#0c0c0cf9>█<#000000ff>█<#0a0807f1>█<#826b5f2b>█<#00000000>█<#00000001>█<#00000000>███████████████████<br>██████████████████████<#ffffff07>█<#322a259f>█<#1c1b1bff>█<#020202ff>█<#231e1a9a>█<#00000000>████████████████<#00000002>█<#00000000>█<#39323224>█<#201c1ad4>█<#0b0b0bff>█<#0a0807f1>█<#55473f5d>█<#00000000>█<#00000001>█<#00000000>████████████████████<br>███████████████████████<#ffffff07>█<#614b4447>█<#6b585043>█<#00000000>██████████████████<#00000001>█<#00000000>█<#b38c7314>█<#56474153>█<#9d82772f>█<#00000000>███████████████████████<br>█████████████████████████████████████████████<#00000001>█<#00000000>██████████████████████████<br>████████████████████████<#00000002>██<#00000000>████████████████████<#00000001>█<#00000002>█<#00000001>█<#00000000>███████████████████████<br>████████████████████████████████████████████████████████████████████████<br>████████████████████████████████████████████████████████████████████████<br>████████████████████████████████████████████████████████████████████████<br>████████████████████████████████████████████████████████████████████████<br>████████████████████████████████████████████████████████████████████████<br>████████████████████████████████████████████████████████████████████████<br>████████████████████<#00000001>██<#00000000>████████████████████████████<#00000001>██<#00000000>████████████████████<br>███████████████████<#00000001>█<#00000000>██<#00000003>█<#00000000>██████████████████████████<#00000003>█<#00000000>██<#00000001>█<#00000000>███████████████████<br>██████████████████<#00000001>█<#00000000>█<#35353530>█<#40404018>█<#00000000>█<#33333305>█<#00000001>█<#00000000>██████████████████████<#00000001>█<#33333305>█<#00000000>█<#79797913>█<#39393936>█<#00000000>█<#00000001>█<#00000000>██████████████████<br>██████████████████<#00000002>█<#00000000>█<#221e1e44>█<#1e1e1ece>█<#3e3e3e29>█<#00000000>██<#00000003>█<#00000001>█<#00000000>██████████████████<#00000001>█<#00000003>█<#00000000>██<#5a5a5a25>█<#222222d1>█<#18181854>█<#00000000>█<#00000002>█<#00000000>██████████████████<br>███████████████████<#00000001>█<#00000000>█<#0e0e0ec2>█<#252525ff>█<#2f2f2f77>█<#5555400c>█<#00000000>██<#00000003>██<#00000001>█<#00000000>████████████<#00000001>█<#00000003>██<#00000000>██<#39393909>█<#2f2f2f77>█<#252525ff>█<#0b0b0bd3>█<#00000000>█████████████████████<br>███████████████████<#00000002>█<#00000000>█<#10101041>█<#2a2a2aff>█<#525254ff>█<#3b3b3cde>█<#24242472>█<#17171716>█<#00000000>███<#00000002>█<#00000003>██<#00000002>██<#00000001>██<#00000002>██<#00000003>██<#00000001>█<#00000000>███<#1a1a1a14>█<#1f1d1a6b>█<#333335da>█<#575759ff>█<#323232ff>█<#12121254>█<#00000000>█<#00000003>█<#00000000>███████████████████<br>████████████████████<#00000002>█<#00000000>█<#0b0b0ba8>█<#636366ff>█<#8b8b8fff>█<#727273ff>█<#4b4b4beb>█<#2a2a2a9c>█<#0e0e0b48>█<#20100010>█<#00000000>████████████<#1a1a1a14>█<#0d0d0a4c>█<#2121219b>█<#444444e9>█<#727273ff>█<#8f8f93ff>█<#747478ff>█<#111111b9>█<#00000000>██████████████████████<br>████████████████████<#00000002>█<#00000000>█<#00000020>█<#29292aee>█<#8f8f93ff>█<#adadb0f9>█<#b5b5b8ff>█<#a5a5a7ff>█<#7c7c7dff>█<#575758e4>█<#3a3a3bb0>█<#2727277c>█<#09090955>█<#00000032>█<#0606062d>█<#00000020>██<#0606062d>█<#00000035>█<#0b0b0b5b>█<#11111185>█<#252525b8>█<#434343e7>█<#707070ff>█<#a0a0a2ff>█<#b9b9bcff>█<#b4b4b7fb>█<#9b9b9dff>█<#313133f9>█<#0606062d>█<#00000000>█<#00000002>█<#00000000>████████████████████<br>█████████████████████<#00000003>█<#00000000>█<#00000058>█<#49494bff>█<#acacaffd>█<#b4b4b7fb>█<#c9c9ccfb>█<#dddddefc>█<#e2e2e4ff>█<#d6d6d7ff>█<#bdbdbeff>█<#9b9b9dff>█<#848484ff>█<#6f6f6ffc>█<#646464f0>█<#616161f1>█<#6a6a6afe>█<#767676ff>█<#8c8c8dff>█<#abababff>█<#cacacbff>█<#e1e1e2fe>█<#e2e2e4fd>█<#d0d0d2fd>█<#b8b8bbfc>█<#b5b5b7fe>█<#575759ff>█<#0000006a>█<#00000000>█<#00000003>█<#00000000>█████████████████████<br>██████████████████████<#00000002>█<#00000000>█<#100e0c80>█<#5d5d5eff>█<#bdbdbeff>█<#c9c9ccfb>█<#cbcbccff>█<#dbdbddfe>█<#eeeeeffc>█<#fffffffb>█<#fffffffc>█<#ffffffff>██████<#fffffffc>█<#fffffffb>█<#f5f5f5fc>█<#e1e1e2fe>█<#d0d0d2fd>█<#cdcdcffb>█<#c4c4c7fe>█<#6b6b6dff>█<#0e0e0e90>█<#00000000>█<#00000001>█<#00000000>██████████████████████<br>█████████████████████████<#04040474>█<#404041fe>█<#a6a6a8ff>█<#dbdbddfe>█<#ececedfc>███<#f1f1f2fe>█<#f6f6f7fe>█<#fbfbfbfe>████<#f6f6f7fe>█<#f1f1f2fe>█<#eeeeeefe>█<#ececedfc>█<#eeeeeffc>█<#e1e1e2fe>█<#afafb1ff>█<#49494bff>█<#04040281>█<#00000000>██<#00000001>█<#00000000>██████████████████████<br>████████████████████████<#00000001>█<#00000000>█<#00000035>█<#161616bb>█<#545455ff>█<#a6a6a8ff>█<#e1e1e2fe>█<#fdfdfdff>█<#ffffffff>█<#fffffffb>██████<#ffffffff>██<#e6e6e7ff>█<#afafb1ff>█<#5d5d5eff>█<#1a1a1ac4>█<#00000040>█<#00000000>██<#00000001>█<#00000000>███████████████████████<br>█████████████████████████<#00000003>█<#00000000>██<#00000035>█<#0000008a>█<#313131c2>█<#626262e9>█<#8c8c8dff>█<#b2b2b2ff>█<#cbcbccff>█<#d6d6d6ff>██<#cdcdcdff>█<#b5b5b7fe>█<#919191fd>█<#666666ec>█<#353535cf>█<#02020293>█<#00000040>█<#00000000>██<#00000002>█<#00000000>█████████████████████████<br>██████████████████████████<#00000002>█<#00000001>█<#00000000>█<#ffffff02>█<#2f2f2f3c>█<#00000049>█<#00000052>█<#08080862>█<#0e0e0e6e>█<#1515157c>█<#1818187f>█<#0b0b0b73>█<#04040474>█<#0000006c>█<#08080862>█<#2d2d2d22>█<#00000000>███<#00000002>█<#00000000>██████████████████████████<br>████████████████████████████<#00000002>█<#00000003>█<#4f4f4f1d>█<#2222225a>█<#1717177b>█<#1414148a>█<#0d0d0d8c>█<#09090987>█<#0b0b0b8b>█<#1212128e>█<#1515157c>█<#22222261>█<#5a5a5a25>█<#00000001>█<#00000003>█<#00000002>█<#00000000>████████████████████████████<br>████████████████████████████████<#33333305>█<#2424242b>█<#0e0e0b48>█<#0b0b0b5b>█<#0606064f>█<#19191933>█<#3333330f>█<#00000000>█████████████████████████████████<br>██████████████████████████████<#00000001>█<#00000002>█<#00000000>███████<#00000002>█<#00000001>█<#00000000>███████████████████████████████<br>█████████████████████████████████<#00000001>█<#00000002>███<#00000001>█<#00000000>██████████████████████████████████<br>████████████████████████████████████████████████████████████████████████<br>████████████████████████████████████████████████████████████████████████<br>████████████████████████████████████████████████████████████████████████<br>████████████████████████████████████████████████████████████████████████<br>████████████████████████████████████████████████████████████████████████<br>████████████████████████████████████████████████████████████████████████<br>████████████████████████████████████████████████████████████████████████<br>████████████████████████████████████████████████████████████████████████<br>████████████████████████████████████████████████████████████████████████<br>████████████████████████████████████████████████████████████████████████<br>████████████████████████████████████████████████████████████████████████<br>████████████████████████████████████████████████████████████████████████<br>";
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].name = "text";
			ObjectManager.inst.updateObjects(tmpSelection, false);
			ObjEditor.inst.RenderTimelineObject(tmpSelection);
			EditorManager.inst.history.Add(new History.Command("Create New Normal Text Object", delegate ()
			{
				Plugin.CreateNewTextObject(_select);
			}, delegate ()
			{
				ObjEditor.inst.DeleteObject(tmpSelection, true);
			}), false);
		}

		public static void CreateNewHexagonObject(bool _select = true)
		{
			ObjEditor.ObjectSelection tmpSelection = Plugin.CreateNewDefaultObject(_select);
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].shape = 5;
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].shapeOption = 0;
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].name = "hexagon";
			ObjectManager.inst.updateObjects(tmpSelection, false);
			ObjEditor.inst.RenderTimelineObject(tmpSelection);
			EditorManager.inst.history.Add(new History.Command("Create New Normal Hexagon Object", delegate ()
			{
				Plugin.CreateNewHexagonObject(_select);
			}, delegate ()
			{
				ObjEditor.inst.DeleteObject(tmpSelection, true);
			}), false);
		}

		public static void CreateNewHelperObject(bool _select = true)
		{
			ObjEditor.ObjectSelection tmpSelection = Plugin.CreateNewDefaultObject(_select);
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].objectType = DataManager.GameData.BeatmapObject.ObjectType.Helper;
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].name = "helper";
			ObjectManager.inst.updateObjects(tmpSelection, false);
			ObjEditor.inst.RenderTimelineObject(tmpSelection);
			EditorManager.inst.history.Add(new History.Command("Create New Helper Object", delegate ()
			{
				Plugin.CreateNewHelperObject(_select);
			}, delegate ()
			{
				ObjEditor.inst.DeleteObject(tmpSelection, true);
			}), false);
		}

		public static void CreateNewDecorationObject(bool _select = true)
		{
			ObjEditor.ObjectSelection tmpSelection = Plugin.CreateNewDefaultObject(_select);
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].objectType = DataManager.GameData.BeatmapObject.ObjectType.Decoration;
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].name = "decoration";
			ObjectManager.inst.updateObjects(tmpSelection, false);
			ObjEditor.inst.RenderTimelineObject(tmpSelection);
			EditorManager.inst.history.Add(new History.Command("Create New Decoration Object", delegate ()
			{
				Plugin.CreateNewDecorationObject(_select);
			}, delegate ()
			{
				ObjEditor.inst.DeleteObject(tmpSelection, true);
			}), false);
		}

		public static void CreateNewEmptyObject(bool _select = true)
		{
			ObjEditor.ObjectSelection tmpSelection = Plugin.CreateNewDefaultObject(_select);
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].objectType = DataManager.GameData.BeatmapObject.ObjectType.Empty;
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].name = "empty";
			ObjectManager.inst.updateObjects(tmpSelection, false);
			ObjEditor.inst.RenderTimelineObject(tmpSelection);
			EditorManager.inst.history.Add(new History.Command("Create New Empty Object", delegate ()
			{
				Plugin.CreateNewEmptyObject(_select);
			}, delegate ()
			{
				ObjEditor.inst.DeleteObject(tmpSelection, true);
			}), false);
		}

		public static void CreateNewNoAutokillObject(bool _select = true)
		{
			ObjEditor.ObjectSelection tmpSelection = Plugin.CreateNewDefaultObject(_select);
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].autoKillType = DataManager.GameData.BeatmapObject.AutoKillType.OldStyleNoAutokill;
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].name = "no autokill";
			ObjectManager.inst.updateObjects(tmpSelection, false);
			ObjEditor.inst.RenderTimelineObject(tmpSelection);
			EditorManager.inst.history.Add(new History.Command("Create New No Autokill Object", delegate ()
			{
				Plugin.CreateNewNoAutokillObject(_select);
			}, delegate ()
			{
				ObjEditor.inst.DeleteObject(tmpSelection, true);
			}), false);
		}

		public static ObjEditor.ObjectSelection CreateNewDefaultObject(bool _select = true)
		{
			if (!EditorManager.inst.hasLoadedLevel)
			{
				EditorManager.inst.DisplayNotification("Can't add objects to level until a level has been loaded!", 2f, EditorManager.NotificationType.Error, false);
				return null;
			}
			List<List<DataManager.GameData.EventKeyframe>> list = new List<List<DataManager.GameData.EventKeyframe>>();
			list.Add(new List<DataManager.GameData.EventKeyframe>());
			list.Add(new List<DataManager.GameData.EventKeyframe>());
			list.Add(new List<DataManager.GameData.EventKeyframe>());
			list.Add(new List<DataManager.GameData.EventKeyframe>());
			list[0].Add(new DataManager.GameData.EventKeyframe(0f, new float[2], new float[0], 0));
			list[1].Add(new DataManager.GameData.EventKeyframe(0f, new float[]
			{
			1f,
			1f
			}, new float[0], 0));
			list[2].Add(new DataManager.GameData.EventKeyframe(0f, new float[1], new float[0], 0));
			list[3].Add(new DataManager.GameData.EventKeyframe(0f, new float[1], new float[0], 0));
			DataManager.GameData.BeatmapObject beatmapObject = new DataManager.GameData.BeatmapObject(true, AudioManager.inst.CurrentAudioSource.time, "", 0, "", list);
			beatmapObject.id = LSText.randomString(16);
			beatmapObject.autoKillType = DataManager.GameData.BeatmapObject.AutoKillType.LastKeyframeOffset;
			beatmapObject.autoKillOffset = 5f;
			if (EditorManager.inst.layer == 5)
			{
				beatmapObject.editorData.Layer = EditorManager.inst.lastLayer;
				SetLayer(EditorManager.inst.lastLayer);
			}
            else
            {
				beatmapObject.editorData.Layer = EditorManager.inst.layer;
			}
			int num = DataManager.inst.gameData.beatmapObjects.FindIndex((DataManager.GameData.BeatmapObject x) => x.fromPrefab);
			if (num == -1)
			{
				DataManager.inst.gameData.beatmapObjects.Add(beatmapObject);
			}
			else
			{
				DataManager.inst.gameData.beatmapObjects.Insert(num, beatmapObject);
			}
			ObjEditor.ObjectSelection objectSelection = new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Object, beatmapObject.id);
			ObjEditor.inst.CreateTimelineObject(objectSelection);
			ObjEditor.inst.RenderTimelineObject(objectSelection);
			ObjectManager.inst.updateObjects(objectSelection, false);
			AudioManager.inst.SetMusicTime(AudioManager.inst.CurrentAudioSource.time + 0.001f);
			if (_select)
			{
				ObjEditor.inst.SetCurrentObj(objectSelection);
			}
			return objectSelection;
		}

		public static void SetLayer(int _layer)
        {
            Image layerImage = GameObject.Find("TimelineBar/GameObject/layers").GetComponent<Image>();
            DataManager.inst.UpdateSettingInt("EditorLayer", _layer);
			int oldLayer = EditorManager.inst.layer;
			EditorManager.inst.layer = _layer;
			if (_layer < EditorManager.inst.layerColors.Count)
            {
				EditorManager.inst.timelineWaveformOverlay.GetComponent<Image>().color = EditorManager.inst.layerColors[_layer];
				layerImage.color = EditorManager.inst.layerColors[_layer];
			}
			if (_layer > 6)
            {
				layerImage.color = Color.white;
            }
			if (EditorManager.inst.layer == 5 && EditorManager.inst.lastLayer != 5)
			{
				EventEditor.inst.EventLabels.SetActive(true);
				EventEditor.inst.EventHolders.SetActive(true);
				EventEditor.inst.CreateEventObjects();
				CheckpointEditor.inst.CreateCheckpoints();
				ObjEditor.inst.RenderTimelineObjects("");
				GameObject.Find("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject/6").GetComponent<Toggle>().isOn = true;
			}
			else if (EditorManager.inst.layer != 5 && EditorManager.inst.lastLayer == 5)
			{
				EventEditor.inst.EventLabels.SetActive(false);
				EventEditor.inst.EventHolders.SetActive(false);
				if (EventEditor.inst.eventObjects.Count > 0)
				{
					foreach (List<GameObject> list in EventEditor.inst.eventObjects)
					{
						foreach (GameObject obj in list)
						{
							UnityEngine.Object.Destroy(obj);
						}
					}
					foreach (List<GameObject> list2 in EventEditor.inst.eventObjects)
					{
						list2.Clear();
					}
				}
				ObjEditor.inst.RenderTimelineObjects("");
				if (CheckpointEditor.inst.checkpoints.Count > 0)
				{
					foreach (GameObject obj2 in CheckpointEditor.inst.checkpoints)
					{
						UnityEngine.Object.Destroy(obj2);
					}
					CheckpointEditor.inst.checkpoints.Clear();
				}
				CheckpointEditor.inst.CreateGhostCheckpoints();
				GameObject.Find("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject/6").GetComponent<Toggle>().isOn = false;
			}
			else
			{
				ObjEditor.inst.RenderTimelineObjects("");
				GameObject.Find("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject/6").GetComponent<Toggle>().isOn = false;
			}

			if (_layer < EditorManager.inst.layerSelectors.Count)
            {
				EditorManager.inst.layerSelectors[_layer].GetComponent<Toggle>().isOn = true;
			}
			
			EditorManager.inst.history.Add(new History.Command("Change Layer", delegate ()
			{
				Plugin.SetLayer(_layer);
			}, delegate ()
			{
				Plugin.SetLayer(oldLayer);
			}), false);
		}

		public static void SetMultiObjectLayer(int _layer, bool _add = false)
        {
			foreach (var objectSelection in ObjEditor.inst.selectedObjects)
            {
				if (_add == false)
                {
					objectSelection.GetObjectData().editorData.Layer = _layer;
					ObjEditor.inst.RenderTimelineObject(objectSelection);
				}
				else
                {
					objectSelection.GetObjectData().editorData.Layer += _layer;
					ObjEditor.inst.RenderTimelineObject(objectSelection);
				}
			}
        }

		public static void RepeatReminder()
        {
			EditorManager.inst.CancelInvoke("CreateGrid");
			EditorManager.inst.InvokeRepeating("CreateGrid", ReminderRepeat.Value, ReminderRepeat.Value);
		}

		[HarmonyPatch(typeof(EditorManager), "CreateGrid")]
		[HarmonyPostfix]
		public static void Reminder()
        {
			if (ReminderActive.Value == true)
			{
				int radfs = UnityEngine.Random.Range(0, 4);
				string randomtext = "";
				if (radfs == 0)
				{
					randomtext = ". You should touch some grass.";
				}
				if (radfs == 1)
				{
					randomtext = ". Have a break.";
				}
				if (radfs == 2)
				{
					randomtext = ". You doing alright there?";
				}
				if (radfs == 3)
				{
					randomtext = ". You might need some rest.";
				}
				if (radfs == 4)
				{
					randomtext = ". Best thing to do in this situation is to have a break.";
				}

				EditorManager.inst.DisplayNotification("You've been working on the game for " + secondsToTime(Time.time) + randomtext, 2f, EditorManager.NotificationType.Warning, false);
			}
		}

		[HarmonyPatch(typeof(EditorManager), "Start")]
		[HarmonyPostfix]
		private static void SetEditorStart()
		{
			Plugin.RepeatReminder();

			GameObject folderButton = EditorManager.inst.folderButtonPrefab;
			Button fButtonBUTT = folderButton.GetComponent<Button>();
			Text fButtonText = folderButton.transform.Find("folder-name").GetComponent<Text>();
			//Folder button
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

			timeEdit = itsTheTime;
		}

		[HarmonyPatch(typeof(EditorManager), "Update")]
		[HarmonyPostfix]
		private static void UpdateEditor()
		{
			if (EditorManager.inst.GUI.activeSelf == true && EditorManager.inst.isEditing == true)
			{
				//Create Local Variables
				GameObject timeObj = GameObject.Find("TimelineBar/GameObject/Time Input");
				InputField iFtimeObj = timeObj.GetComponent<InputField>();

				if (!iFtimeObj.isFocused)
				{
					iFtimeObj.text = AudioManager.inst.CurrentAudioSource.time.ToString();
				}

				bool playingEd = AudioManager.inst.CurrentAudioSource.isPlaying;

				if (playingEd == true)
				{
					Vector2 point = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
					float value = EditorManager.inst.timelineScrollbar.GetComponent<Scrollbar>().value;
					Rect rect = new Rect(0f, 0.305f * (float)Screen.height, (float)Screen.width, (float)Screen.height * 0.025f);
					if (Input.GetMouseButton(0) && rect.Contains(point))
					{
						if (Mathf.Abs(EditorManager.inst.audioTimeForSlider / EditorManager.inst.Zoom - EditorManager.inst.prevAudioTime) < 2f)
						{
							AudioManager.inst.CurrentAudioSource.Play();
							EditorManager.inst.UpdatePlayButton();
						}
					}
				}
				if (GameObject.Find("Editor Systems/Editor GUI/sizer/main/Popups/File Info Popup/loading") && GameObject.Find("Editor Systems/Editor GUI/sizer/main/Popups/File Info Popup/loading").activeSelf == true)
				{
					Image image = GameObject.Find("Editor Systems/Editor GUI/sizer/main/Popups/File Info Popup/loading").GetComponent<Image>();
					image.sprite = EditorManager.inst.loadingImage.sprite;
				}
			}
			itsTheTime = timeEdit + Time.time;

			if (EditorManager.inst.GetDialog("Multi Object Editor").Dialog.gameObject.activeSelf == true && EditorManager.inst.GetDialog("Multi Object Editor").Dialog.Find("data").GetComponent<RectTransform>().sizeDelta != new Vector2(810f, 730.11f))
            {
				EditorManager.inst.GetDialog("Multi Object Editor").Dialog.Find("data").GetComponent<RectTransform>().sizeDelta = new Vector2(810f, 730.11f);
			}
			if (EditorManager.inst.GetDialog("Multi Object Editor").Dialog.gameObject.activeSelf == true && EditorManager.inst.GetDialog("Multi Object Editor").Dialog.Find("data/left").GetComponent<RectTransform>().sizeDelta != new Vector2(355f, 730f))
            {
				EditorManager.inst.GetDialog("Multi Object Editor").Dialog.Find("data/left").GetComponent<RectTransform>().sizeDelta = new Vector2(355f, 730f);
			}

			if (ShowObjectsOnLayer.Value == true)
            {
				foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
                {
					if (beatmapObject.editorData.Layer != EditorManager.inst.layer && EditorManager.inst.layer != 5)
                    {
						if (beatmapObject.shape != 4)
						{
							ObjectManager.GameObjectRef gameObjectRef = ObjectManager.inst.beatmapGameObjects[beatmapObject.id];
							Color objColor = gameObjectRef.mat.color;
							gameObjectRef.mat.color = new Color(objColor.r, objColor.g, objColor.b, objColor.a * ShowObjectsAlpha.Value);
						}
						else
						{
							ObjectManager.GameObjectRef gameObjectRef = ObjectManager.inst.beatmapGameObjects[beatmapObject.id];
							Color objColor = gameObjectRef.obj.GetComponentInChildren<TMPro.TMP_Text>().color;
							gameObjectRef.obj.GetComponentInChildren<TMPro.TMP_Text>().color = new Color(objColor.r, objColor.g, objColor.b, objColor.a * ShowObjectsAlpha.Value);
						}
					}
                }
            }
		}

		public static bool multiObjectState = true;

		[HarmonyPatch(typeof(EditorManager), "SaveBeatmap")]
		[HarmonyPostfix]
		private static void SaveEditorManager()
        {
			DataManager.inst.gameData.beatmapData.editorData.timelinePos = AudioManager.inst.CurrentAudioSource.time;
			DataManager.inst.metaData.song.BPM = SettingEditor.inst.SnapBPM;
			DataManager.inst.gameData.beatmapData.levelData.backgroundColor = EditorManager.inst.layer;
			scrollBar = GameObject.Find("Editor Systems/Editor GUI/sizer/main/whole-timeline/Scrollbar").GetComponent<Scrollbar>().value;

			Sprite waveform = EditorManager.inst.timeline.GetComponent<Image>().sprite;
			if (WaveformMode.Value == WaveformType.Legacy && GenerateWaveform.Value == true)
            {
				File.WriteAllBytes(RTFile.GetApplicationDirectory() + GameManager.inst.basePath + "waveform.png", EditorManager.inst.timeline.GetComponent<Image>().sprite.texture.EncodeToPNG());
			}
			if (WaveformMode.Value == WaveformType.Old && GenerateWaveform.Value == true)
            {
				File.WriteAllBytes(RTFile.GetApplicationDirectory() + GameManager.inst.basePath + "waveform_old.png", EditorManager.inst.timeline.GetComponent<Image>().sprite.texture.EncodeToPNG());
			}

			if (RTFile.FileExists(RTFile.GetApplicationDirectory() + "beatmaps/" + editorPath + "/" + EditorManager.inst.currentLoadedLevel + "/editor.lse"))
			{
				string rawProfileJSON = null;
				rawProfileJSON = FileManager.inst.LoadJSONFile("beatmaps/" + editorPath + "/" + EditorManager.inst.currentLoadedLevel + "/editor.lse");

				JSONNode jsonnode = JSON.Parse(rawProfileJSON);

				jsonnode["timeline"]["tsc"] = RTMath.RoundToNearestDecimal(GameObject.Find("Editor Systems/Editor GUI/sizer/main/whole-timeline/Scrollbar").GetComponent<Scrollbar>().value, 3);
				jsonnode["timeline"]["z"] = EditorManager.inst.Zoom;
				jsonnode["timeline"]["l"] = EditorManager.inst.layer;
				jsonnode["editor"]["t"] = itsTheTime;
				jsonnode["editor"]["a"] = openAmount;
				jsonnode["misc"]["sn"] = SettingEditor.inst.SnapActive;

				RTFile.WriteToFile("beatmaps/" + editorPath + "/" + EditorManager.inst.currentLoadedLevel + "/editor.lse", jsonnode.ToString(3));
			}
			else
			{
				JSONNode jsonnode = JSON.Parse("{}");

				jsonnode["timeline"]["tsc"] = RTMath.roundToNearest(GameObject.Find("Editor Systems/Editor GUI/sizer/main/whole-timeline/Scrollbar").GetComponent<Scrollbar>().value, 0.001f);
				jsonnode["timeline"]["z"] = EditorManager.inst.Zoom;
				jsonnode["timeline"]["l"] = EditorManager.inst.layer;
				jsonnode["editor"]["t"] = itsTheTime;
				jsonnode["editor"]["a"] = openAmount;
				jsonnode["misc"]["sn"] = SettingEditor.inst.SnapActive;

				RTFile.WriteToFile("beatmaps/" + editorPath + "/" + EditorManager.inst.currentLoadedLevel + "/editor.lse", jsonnode.ToString(3));
			}
		}

		[HarmonyPatch(typeof(EditorManager), "SaveBeatmapAs", new Type[] { })]
		[HarmonyPostfix]
		private static void FixSaveAs()
		{
			if (EditorManager.inst.hasLoadedLevel)
			{
				string str = "beatmaps/" + editorPath + "/" + EditorManager.inst.saveAsLevelName;
				DataManager.inst.SaveMetadata(str + "/metadata.lsb");
				if (RTFile.FileExists(RTFile.GetApplicationDirectory() + "beatmaps/" + editorPath + "/" + EditorManager.inst.currentLoadedLevel + "/waveform.png"))
                {
					File.Copy(RTFile.GetApplicationDirectory() + "/beatmaps/" + editorPath + "/" + EditorManager.inst.currentLoadedLevel + "/waveform.png", RTFile.GetApplicationDirectory() + str + "/waveform.png", true);
				}
				if (RTFile.FileExists(RTFile.GetApplicationDirectory() + "beatmaps/" + editorPath + "/" + EditorManager.inst.currentLoadedLevel + "/waveform_old.png"))
				{
					File.Copy(RTFile.GetApplicationDirectory() + "/beatmaps/" + editorPath + "/" + EditorManager.inst.currentLoadedLevel + "/waveform_old.png", RTFile.GetApplicationDirectory() + str + "/waveform_old.png", true);
				}

				if (RTFile.FileExists(RTFile.GetApplicationDirectory() + "beatmaps/" + editorPath + "/" + EditorManager.inst.currentLoadedLevel + "/editor.lse"))
				{
					string rawProfileJSON = null;
					rawProfileJSON = FileManager.inst.LoadJSONFile("beatmaps/" + editorPath + "/" + EditorManager.inst.currentLoadedLevel + "/editor.lse");

					JSONNode jsonnode = JSON.Parse(rawProfileJSON);

					jsonnode["timeline"]["tsc"] = RTMath.RoundToNearestDecimal(GameObject.Find("Editor Systems/Editor GUI/sizer/main/whole-timeline/Scrollbar").GetComponent<Scrollbar>().value, 3);
					jsonnode["timeline"]["z"] = EditorManager.inst.Zoom;
					jsonnode["timeline"]["l"] = EditorManager.inst.layer;
					jsonnode["editor"]["t"] = itsTheTime;
					jsonnode["editor"]["a"] = openAmount;
					jsonnode["misc"]["sn"] = SettingEditor.inst.SnapActive;

					RTFile.WriteToFile("beatmaps/" + editorPath + "/" + EditorManager.inst.saveAsLevelName + "/editor.lse", jsonnode.ToString(3));
				}
				else
				{
					JSONNode jsonnode = JSON.Parse("{}");

					jsonnode["timeline"]["tsc"] = RTMath.roundToNearest(GameObject.Find("Editor Systems/Editor GUI/sizer/main/whole-timeline/Scrollbar").GetComponent<Scrollbar>().value, 0.001f);
					jsonnode["timeline"]["z"] = EditorManager.inst.Zoom;
					jsonnode["timeline"]["l"] = EditorManager.inst.layer;
					jsonnode["editor"]["t"] = itsTheTime;
					jsonnode["editor"]["a"] = openAmount;
					jsonnode["misc"]["sn"] = SettingEditor.inst.SnapActive;

					RTFile.WriteToFile("beatmaps/" + editorPath + "/" + EditorManager.inst.saveAsLevelName + "/editor.lse", jsonnode.ToString(3));
				}
			}
		}

		public static void LoadLevelEnumerator()
        {
			SetAutosave();
			if (!RTFile.DirectoryExists(RTFile.GetApplicationDirectory() + GameManager.inst.basePath + "autosaves"))
			{
				Directory.CreateDirectory(RTFile.GetApplicationDirectory() + GameManager.inst.basePath + "autosaves");
			}
			string[] files = Directory.GetFiles(FileManager.GetAppPath() + "/" + GameManager.inst.basePath, "autosaves/autosave_*.lsb", SearchOption.TopDirectoryOnly);
			files.ToList<string>().Sort();
			int num = 0;
			foreach (string text2 in files)
			{
				if (num != files.Count<string>() - 1)
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

		[HarmonyPatch(typeof(RTEditor), "AutoSaveLevel")]
		[HarmonyPostfix]
		public static void AutoSaveLevel()
		{
			if (!EditorManager.inst.hasLoadedLevel)
			{
				EditorManager.inst.DisplayNotification("Beatmap can't autosave until you load a level.", 3f, EditorManager.NotificationType.Error, false);
				return;
			}
			if (EditorManager.inst.savingBeatmap)
			{
				EditorManager.inst.DisplayNotification("Already attempting to save the beatmap!", 2f, EditorManager.NotificationType.Error, false);
				return;
			}
			string text = string.Concat(new string[]
			{
			FileManager.GetAppPath(),
			"/",
			GameManager.inst.basePath,
			"autosaves/autosave_",
			DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss"),
			".lsb"
			});
			if (!RTFile.DirectoryExists(RTFile.GetApplicationDirectory() + GameManager.inst.basePath + "autosaves"))
			{
				Directory.CreateDirectory(RTFile.GetApplicationDirectory() + GameManager.inst.basePath + "autosaves");
			}
			EditorManager.inst.DisplayNotification("Autosaving backup!", 2f, EditorManager.NotificationType.Warning, false);
			EditorManager.inst.autosaves.Add(text);
			while (EditorManager.inst.autosaves.Count > AutoSaveLimit.Value)
			{
				File.Delete(EditorManager.inst.autosaves.First<string>());
				EditorManager.inst.autosaves.RemoveAt(0);
			}
			EditorManager.inst.StartCoroutine(DataManager.inst.SaveData(text));
		}

		[HarmonyPatch(typeof(ObjEditor), "CreateTimelineObjects")]
		[HarmonyPostfix]
		private static void SetEditorTime()
		{
			if (!string.IsNullOrEmpty(EditorManager.inst.currentLoadedLevel))
            {
				if (IfEditorStartTime.Value == true)
				{
					AudioManager.inst.CurrentAudioSource.time = DataManager.inst.gameData.beatmapData.editorData.timelinePos;
				}
				if (IfEditorPauses.Value == true)
				{
					AudioManager.inst.CurrentAudioSource.Pause();
				}

				if (RTFile.FileExists(RTFile.GetApplicationDirectory() + "beatmaps/" + editorPath + "/" + EditorManager.inst.currentLoadedLevel + "/editor.lse"))
				{
					string rawProfileJSON = null;
					rawProfileJSON = FileManager.inst.LoadJSONFile("beatmaps/" + editorPath + "/" + EditorManager.inst.currentLoadedLevel + "/editor.lse");

					JSONNode jsonnode = JSON.Parse(rawProfileJSON);

					EditorManager.inst.Zoom = jsonnode["timeline"]["z"];
					GameObject.Find("Editor Systems/Editor GUI/sizer/main/whole-timeline/zoom-panel/Slider").GetComponent<Slider>().value = jsonnode["timeline"]["tsc"];

                    Plugin.SetLayer(jsonnode["timeline"]["l"]);

					timeEdit = jsonnode["editor"]["t"];
					openAmount = jsonnode["editor"]["a"];
					openAmount += 1;

					SettingEditor.inst.SnapActive = jsonnode["misc"]["sn"];
					SettingEditor.inst.SnapBPM = DataManager.inst.metaData.song.BPM;
				}
				else
				{
					timeEdit = 0;
				}
			}
		}

		[HarmonyPatch(typeof(EditorManager), "RenderOpenBeatmapPopup")]
		[HarmonyPostfix]
		private static void RBSPlug()
        {
			Plugin.RenderBeatmapSet();
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

		[HarmonyPatch(typeof(EditorManager), "AssignWaveformTextures")]
		[HarmonyPrefix]
		private static bool RunTimelineWaveform()
        {
			return false;
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
					string waveformLocation = RTFile.GetApplicationDirectory() + "beatmaps/" + editorPath + "/" + EditorManager.inst.currentLoadedLevel + "/waveform.png";
					if (RTFile.FileExists(waveformLocation))
					{
						NewTexture(waveformLocation);
					}
					else
					{
						LegacyWaveform();
					}
				}
				else
				{
					string waveformLocation = RTFile.GetApplicationDirectory() + "beatmaps/" + editorPath + "/" + EditorManager.inst.currentLoadedLevel + "/waveform_old.png";
					if (RTFile.FileExists(waveformLocation))
					{
						NewTexture(waveformLocation);
					}
					else
					{
						OldWaveform();
					}
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

		[HarmonyPatch(typeof(EditorManager), "OpenBeatmapPopup")]
		[HarmonyPostfix]
		private static void SetEditorRenderOBP()
		{
			//Create Local Variables
			GameObject openLevel = EditorManager.inst.GetDialog("Open File Popup").Dialog.gameObject;
			Transform openTLevel = openLevel.transform;
			RectTransform openRTLevel = openLevel.GetComponent<RectTransform>();
			GridLayoutGroup openGridLVL = openTLevel.Find("mask/content").GetComponent<GridLayoutGroup>();

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
		}

		[HarmonyPatch(typeof(MarkerEditor), "Update")]
		[HarmonyPostfix]
		private static void UpdateMakr()
		{
			if (MarkerLoop.Value == true && DataManager.inst.gameData.beatmapData.markers.Count != 0)
			{
				int markerEnd = MarkerEndIndex.Value;
				int markerStart = MarkerStartIndex.Value;
				if (MarkerEndIndex.Value < 0)
                {
					markerEnd = 0;
                }
				if (MarkerEndIndex.Value > DataManager.inst.gameData.beatmapData.markers.Count - 1)
                {
					markerEnd = DataManager.inst.gameData.beatmapData.markers.Count - 1;

				}
				if (MarkerStartIndex.Value < 0)
				{
					markerStart = 0;
				}
				if (MarkerStartIndex.Value > DataManager.inst.gameData.beatmapData.markers.Count - 1)
				{
					markerStart = DataManager.inst.gameData.beatmapData.markers.Count - 1;

				}

				if (AudioManager.inst.CurrentAudioSource.time > DataManager.inst.gameData.beatmapData.markers[markerEnd].time)
				{
					AudioManager.inst.CurrentAudioSource.time = DataManager.inst.gameData.beatmapData.markers[markerStart].time;
				}
			}


			if (EditorManager.inst.GetDialog("Marker Editor").Dialog.gameObject.activeSelf == true)
            {
				GameObject markerList = EditorManager.inst.GetDialog("Marker Editor").Dialog.Find("data/right/markers/list").gameObject;
				GameObject eventButton = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject/event");
				Color bcol = new Color(0.3922f, 0.7098f, 0.9647f, 1f);

				if (!markerList.transform.Find("sort markers"))
				{
					GameObject sortMarkers = UnityEngine.Object.Instantiate<GameObject>(eventButton);
					sortMarkers.transform.SetParent(markerList.transform);
					sortMarkers.transform.SetAsFirstSibling();
					sortMarkers.name = "sort markers";

					sortMarkers.transform.GetChild(0).GetComponent<Text>().text = "Sort Markers";
					sortMarkers.GetComponent<Image>().color = bcol;

					sortMarkers.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
					sortMarkers.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
					sortMarkers.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
					sortMarkers.GetComponent<Button>().onClick.RemoveAllListeners();
					sortMarkers.GetComponent<Button>().onClick.AddListener(delegate ()
					{
						List<DataManager.GameData.BeatmapData.Marker> result = new List<DataManager.GameData.BeatmapData.Marker>();
						result = (from x in DataManager.inst.gameData.beatmapData.markers
								  orderby x.time ascending
								  select x).ToList<DataManager.GameData.BeatmapData.Marker>();

						DataManager.inst.gameData.beatmapData.markers = result;
						MarkerEditor.inst.UpdateMarkerList();
					});

					HoverTooltip markerHoverTooltip = sortMarkers.AddComponent<HoverTooltip>();
					HoverTooltip.Tooltip markerTip = new HoverTooltip.Tooltip();

					markerTip.desc = "Sort markers by time.";
					markerTip.hint = "Clicking this will sort and update all markers in the list by song time.<br><#FF0000><b>WARNING!</color></b><br>This is not reversible. Only click this if you really want to sort your markers. (This will only affect the internal marker list, you don't have to worry about the markers in the timeline changing.)";

					markerHoverTooltip.tooltipLangauges.Add(markerTip);
				}

				if (markerList.transform.GetChild(0).name != "sort markers")
				{
					markerList.transform.Find("sort markers").SetAsFirstSibling();
				}

				foreach (var marker in DataManager.inst.gameData.beatmapData.markers)
				{
					var regex = new System.Text.RegularExpressions.Regex(@"hideObjects\((.*?)\)");
					var match = regex.Match(marker.desc);
					if (match.Success)
					{
						if (ShowObjectsOnLayer.Value == true)
						{
							foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
							{
								if (beatmapObject.editorData.Layer != int.Parse(match.Groups[1].ToString()))
								{
									ObjectManager.GameObjectRef gameObjectRef = ObjectManager.inst.beatmapGameObjects[beatmapObject.id];
									Color objColor = gameObjectRef.mat.color;
									gameObjectRef.mat.color = new Color(objColor.r, objColor.g, objColor.b, objColor.a * ShowObjectsAlpha.Value);
								}
							}
						}
					}
				}
			}
		}

		private static void SetNewTime(string _value)
        {
			AudioManager.inst.CurrentAudioSource.time = float.Parse(_value);
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

			bool no = false;
            {
				if (no == true)
                {
					DataManager.inst.AnimationList[1].Animation.keys[1].m_Time = 0.9999f;
					DataManager.inst.AnimationList[1].Animation.keys[1].m_Value = 0f;
				}
            }
		}

		[HarmonyPatch(typeof(ObjEditor), "Start")]
		[HarmonyPostfix]
		private static void SetObjStart()
        {
			ObjEditor.inst.zoomBounds = ObjZoomBounds.Value;
        }

		[HarmonyPatch(typeof(MarkerEditor), "Awake")]
		[HarmonyPostfix]
		private static void MarkerStart()
        {
			SetNewMarkerColors();

			Transform transform = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/MarkerDialog/data/left").transform;
			Text textFont = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/MarkerDialog/data/left/label/text").GetComponent<Text>();

			GameObject indexparent = new GameObject("index");
			indexparent.transform.parent = transform;
			RectTransform rtindexpr = indexparent.AddComponent<RectTransform>();
			rtindexpr.anchoredPosition = new Vector2(0f, -80f);
			LayoutElement leindexpr = indexparent.AddComponent<LayoutElement>();

			leindexpr.ignoreLayout = true;

			GameObject index = new GameObject("text");
			index.transform.parent = indexparent.transform;
			RectTransform rtindex = index.AddComponent<RectTransform>();
			index.AddComponent<CanvasRenderer>();
			Text ttindex = index.AddComponent<Text>();

			rtindex.anchoredPosition = new Vector2(-135f, 2f);

			ttindex.text = "Index: " + MarkerEditor.inst.currentMarker.ToString();
			ttindex.font = textFont.font;
			ttindex.color = new Color(0.9f, 0.9f, 0.9f);
			ttindex.alignment = TextAnchor.MiddleLeft;
			ttindex.fontSize = 24;
			ttindex.horizontalOverflow = HorizontalWrapMode.Overflow;
		}

		[HarmonyPatch(typeof(MarkerEditor), "OpenDialog")]
		[HarmonyPostfix]
		private static void MarkerOpenDialog(MarkerEditor __instance, int __0)
        {
			SetNewMarkerColors();
			GameObject.Find("EditorDialogs/MarkerDialog/data/left/color").GetComponent<GridLayoutGroup>().spacing = new Vector2(8f, 8f);
			GameObject.Find("EditorDialogs/MarkerDialog/data/left/index/text").GetComponent<Text>().text = "Index: " + MarkerEditor.inst.currentMarker.ToString();

			var regex = new System.Text.RegularExpressions.Regex(@"setLayer\((.*?)\)");
			var match = regex.Match(DataManager.inst.gameData.beatmapData.markers[__0].desc);
			if (match.Success)
			{
				if (match.Groups[1].ToString().ToLower() == "events" || match.Groups[1].ToString().ToLower() == "check" || match.Groups[1].ToString().ToLower() == "event/check")
                {
					SetLayer(5);
				}
				else
                {
					if (int.Parse(match.Groups[1].ToString()) > 0)
					{
						if (int.Parse(match.Groups[1].ToString()) < 6)
						{
							SetLayer(int.Parse(match.Groups[1].ToString()) - 1);
						}
						else
						{
							SetLayer(int.Parse(match.Groups[1].ToString()));
						}
					}
					else
					{
						SetLayer(0);
					}
				}
			}
		}


		private static void SetNewMarkerColors()
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

		public static string secondsToTime(float _seconds)
		{
			TimeSpan timeSpan = TimeSpan.FromSeconds((double)_seconds);
			return string.Format("{0:D0}:{1:D1}:{2:D2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
		}

	}
}
