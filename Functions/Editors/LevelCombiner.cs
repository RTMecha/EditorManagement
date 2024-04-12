﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

using LSFunctions;

using RTFunctions.Functions;
using RTFunctions.Functions.Components;
using RTFunctions.Functions.Data;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;

using EditorManagement.Functions.Helpers;
using EditorManagement.Functions.Components;

namespace EditorManagement.Functions.Editors
{
    public class LevelCombiner : MonoBehaviour
    {
        public static LevelCombiner inst;

        #region UI Objects

        public static GameObject editorDialogObject;
        public static Transform editorDialogTransform;
        public static Transform editorDialogTitle;
        public static Transform editorDialogSpacer;
        public static Transform editorDialogContent;
        public static Transform editorDialogText;

        public static InputField searchField;
        public static string searchTerm;

		public static InputField saveField;

        #endregion

        #region Variables

        public static EditorManager.MetadataWrapper first;
        public static EditorManager.MetadataWrapper second;
		public static string savePath;

        #endregion

        void Awake()
        {
            if (inst == null)
                inst = this;
            else if (inst != this)
                Destroy(gameObject);

			editorDialogObject = EditorManager.inst.GetDialog("Multi Keyframe Editor (Object)").Dialog.gameObject.Duplicate(EditorManager.inst.dialogs, "LevelCombinerDialog");

			editorDialogTransform = editorDialogObject.transform;
            editorDialogObject.layer = 5;
            editorDialogTransform.localScale = Vector3.one;
            editorDialogTransform.position = new Vector3(1537.5f, 714.945f, 0f) * EditorManager.inst.ScreenScale;
            editorDialogObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 32f);

            editorDialogTitle = editorDialogTransform.GetChild(0);
            editorDialogTitle.GetComponent<Image>().color = LSColors.HexToColor("E57373");
            editorDialogTitle.GetChild(0).GetComponent<Text>().text = "- Level Combiner -";

            editorDialogSpacer = editorDialogTransform.GetChild(1);
            editorDialogSpacer.GetComponent<RectTransform>().sizeDelta = new Vector2(765f, 12f);

            editorDialogText = editorDialogTransform.GetChild(2);

            editorDialogText.SetSiblingIndex(1);

			var infoText = editorDialogText.GetComponent<Text>();
			infoText.text = "To combine levels into one, select multiple levels from the list below, set a save path and click save.";

			// Label
			{
				var label1 = editorDialogText.gameObject.Duplicate(editorDialogTransform, "label");
				label1.transform.localScale = Vector3.one;

				label1.transform.AsRT().sizeDelta = new Vector2(765f, 32f);
				var labelText = label1.GetComponent<Text>();
				labelText.text = "Select levels to combine";

				EditorThemeManager.AddElement(new EditorThemeManager.Element("Level Combiner Label", "Light Text", labelText.gameObject, new List<Component>
				{
					labelText,
				}));
			}

			var search = Instantiate(EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("search-box").gameObject);
            search.transform.SetParent(editorDialogTransform);
            search.transform.localScale = Vector3.one;
            search.name = "search";

            searchField = search.transform.GetChild(0).GetComponent<InputField>();

            searchField.onValueChanged.ClearAll();
            searchField.text = "";
            searchField.onValueChanged.AddListener(delegate (string _val)
            {
                searchTerm = _val;
                StartCoroutine(RenderDialog());
            });

			EditorThemeManager.AddInputField(searchField, "Level Combiner Search", "Search Field 1", 1, SpriteManager.RoundedSide.Bottom);

			search.transform.GetChild(0).Find("Placeholder").GetComponent<Text>().text = "Search for level...";

            var scrollView = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View"));
			scrollView.transform.SetParent(editorDialogTransform);
			scrollView.transform.localScale = Vector3.one;
            scrollView.name = "Scroll View";

			editorDialogContent = scrollView.transform.Find("Viewport/Content");

			LSHelpers.DeleteChildren(editorDialogContent);

			editorDialogContent.GetComponent<VerticalLayoutGroup>().spacing = 4f;


			scrollView.transform.AsRT().sizeDelta = new Vector2(765f, 392f);

			EditorHelper.AddEditorDialog("Level Combiner", editorDialogObject);

			// Save
			{
				// Label
				{
					var label1 = editorDialogText.gameObject.Duplicate(editorDialogTransform, "label");
					label1.transform.localScale = Vector3.one;

					label1.transform.AsRT().sizeDelta = new Vector2(765f, 32f);
					var labelText = label1.GetComponent<Text>();
					labelText.text = "Save path";

					EditorThemeManager.AddElement(new EditorThemeManager.Element("Level Combiner Label", "Light Text", labelText.gameObject, new List<Component>
					{
						labelText,
					}));
				}

				var save = Instantiate(EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("search-box").gameObject);
				save.transform.SetParent(editorDialogTransform);
				save.transform.localScale = Vector3.one;
				save.name = "search";

				saveField = save.transform.GetChild(0).GetComponent<InputField>();
				UIManager.SetRectTransform(saveField.image.rectTransform, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(700f, 32f));
				saveField.onValueChanged.ClearAll();
				saveField.characterLimit = 0;
				saveField.text = RTFile.ApplicationDirectory + RTEditor.editorListSlash + "Combined Level/level.lsb";
				savePath = RTFile.ApplicationDirectory + RTEditor.editorListSlash + "Combined Level/level.lsb";
				saveField.onValueChanged.AddListener(delegate (string _val)
				{
					savePath = _val;
				});

				EditorThemeManager.AddInputField(saveField, "Level Combiner Save Field", "Input Field");

				((Text)saveField.placeholder).text = "Set a path...";

				//Button 1
				{
					var buttonBase = new GameObject("combine");
					buttonBase.transform.SetParent(editorDialogTransform);
					buttonBase.transform.localScale = Vector3.one;

					var buttonBaseRT = buttonBase.AddComponent<RectTransform>();
					buttonBaseRT.anchoredPosition = new Vector2(436f, 55f);
					buttonBaseRT.sizeDelta = new Vector2(100f, 50f);

					var button = EditorPrefabHolder.Instance.Function2Button.Duplicate(buttonBase.transform, "combine");
					button.transform.localScale = Vector3.one;

					var buttonStorage = button.GetComponent<FunctionButtonStorage>();

					UIManager.SetRectTransform(button.transform.AsRT(), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(400f, 50f));

					buttonStorage.text.text = "Combine & Save";
					buttonStorage.button.onClick.RemoveAllListeners();
					buttonStorage.button.onClick.AddListener(delegate ()
					{
						Combine();
					});

					Destroy(button.GetComponent<Animator>());
					buttonStorage.button.transition = Selectable.Transition.ColorTint;
					EditorThemeManager.AddElement(new EditorThemeManager.Element("Level Combiner Button", "Function 2", button, new List<Component>
					{
						buttonStorage.button.image,
						buttonStorage.button,
					}, true, 1, SpriteManager.RoundedSide.W, true));

					EditorThemeManager.AddElement(new EditorThemeManager.Element("Level Combiner Button Text", "Function 2 Text", buttonStorage.text.gameObject, new List<Component>
					{
						buttonStorage.text,
					}));
				}
			}

			// Dropdown
			EditorHelper.AddEditorDropdown("Level Combiner", "", "File", SpriteManager.LoadSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_combine_t.png"), delegate ()
			{
				OpenDialog();
			}, 4);

			EditorThemeManager.AddElement(new EditorThemeManager.Element("Level Combiner Dialog", "Background", editorDialogObject, new List<Component>
			{
				editorDialogObject.GetComponent<Image>(),
			}));

			EditorThemeManager.AddElement(new EditorThemeManager.Element("Level Combiner Label", "Light Text", infoText.gameObject, new List<Component>
			{
				infoText,
			}));
		}

        void Update()
        {

        }

        public void OpenDialog()
        {
            EditorManager.inst.ShowDialog("Level Combiner");
            StartCoroutine(RenderDialog());
        }

        public IEnumerator RenderDialog()
        {
			var config = EditorConfig.Instance;

			#region Clamping

			var olfnm = config.OpenLevelFolderNameMax;
			var olsnm = config.OpenLevelSongNameMax;
			var olanm = config.OpenLevelArtistNameMax;
			var olcnm = config.OpenLevelCreatorNameMax;
			var oldem = config.OpenLevelDescriptionMax;
			var oldam = config.OpenLevelDateMax;

			int foldClamp = olfnm.Value < 3 ? olfnm.Value : (int)olfnm.DefaultValue;
			int songClamp = olsnm.Value < 3 ? olsnm.Value : (int)olsnm.DefaultValue;
			int artiClamp = olanm.Value < 3 ? olanm.Value : (int)olanm.DefaultValue;
			int creaClamp = olcnm.Value < 3 ? olcnm.Value : (int)olcnm.DefaultValue;
			int descClamp = oldem.Value < 3 ? oldem.Value : (int)oldem.DefaultValue;
			int dateClamp = oldam.Value < 3 ? oldam.Value : (int)oldam.DefaultValue;

			#endregion

			LSHelpers.DeleteChildren(editorDialogContent);

			var horizontalOverflow = config.OpenLevelTextHorizontalWrap.Value;
			var verticalOverflow = config.OpenLevelTextVerticalWrap.Value;
			var fontSize = config.OpenLevelTextFontSize.Value;
			var format = config.OpenLevelTextFormatting.Value;
			var buttonHoverSize = config.OpenLevelButtonHoverSize.Value;

			var iconPosition = config.OpenLevelCoverPosition.Value;
			var iconScale = config.OpenLevelCoverScale.Value;
			iconPosition.x += -75f;

			string[] difficultyNames = new string[]
			{
				"easy",
				"normal",
				"hard",
				"expert",
				"expert+",
				"master",
				"animation",
				"Unknown difficulty",
			};

			int num = 0;
			foreach (var editorWrapper in EditorManager.inst.loadedLevels.Select(x => x as EditorWrapper))
			{
				var folder = editorWrapper.folder;
				var metadata = editorWrapper.metadata;

				DestroyImmediate(editorWrapper.CombinerGameObject);

				var gameObjectBase = new GameObject($"Folder [{Path.GetFileName(editorWrapper.folder)}]");
				gameObjectBase.transform.SetParent(editorDialogContent);
				gameObjectBase.transform.localScale = Vector3.one;
				var rectTransform = gameObjectBase.AddComponent<RectTransform>();

				var image = gameObjectBase.AddComponent<Image>();

				EditorThemeManager.ApplyElement(new EditorThemeManager.Element("Level Combiner Base", "Function 1", gameObjectBase, new List<Component>
				{
					image,
				}, true, 1, SpriteManager.RoundedSide.W));

				rectTransform.sizeDelta = new Vector2(750f, 42f);

				var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(rectTransform, "Button");
				UIManager.SetRectTransform(gameObject.transform.AsRT(), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(740f, 32f));
				editorWrapper.CombinerGameObject = gameObjectBase;

				var hoverUI = gameObject.AddComponent<HoverUI>();
				hoverUI.size = buttonHoverSize;
				hoverUI.animatePos = false;
				hoverUI.animateSca = true;

				var text = gameObject.transform.GetChild(0).GetComponent<Text>();

				text.text = string.Format(format,
					LSText.ClampString(Path.GetFileName(editorWrapper.folder), foldClamp),
					LSText.ClampString(metadata.song.title, songClamp),
					LSText.ClampString(metadata.artist.Name, artiClamp),
					LSText.ClampString(metadata.creator.steam_name, creaClamp),
					metadata.song.difficulty,
					LSText.ClampString(metadata.song.description, descClamp),
					LSText.ClampString(metadata.beatmap.date_edited, dateClamp));

				text.horizontalOverflow = horizontalOverflow;
				text.verticalOverflow = verticalOverflow;
				text.fontSize = fontSize;

				var htt = gameObject.AddComponent<HoverTooltip>();

				var levelTip = new HoverTooltip.Tooltip();

				var difficultyColor = metadata.song.difficulty >= 0 && metadata.song.difficulty < DataManager.inst.difficulties.Count ?
					DataManager.inst.difficulties[metadata.song.difficulty].color : LSColors.themeColors["none"].color;

				levelTip.desc = "<#" + LSColors.ColorToHex(difficultyColor) + ">" + metadata.artist.Name + " - " + metadata.song.title;
				levelTip.hint = "</color>" + metadata.song.description;
				htt.tooltipLangauges.Add(levelTip);

				Action action = delegate ()
				{
					image.enabled = editorWrapper.combinerSelected;

					//var cb = x.colors;
					//cb.normalColor = editorWrapper.combinerSelected ? new Color(0.7447f, 0.7247f, 0.7047f, 1f) : new Color(0.1647f, 0.1647f, 0.1647f, 1f);
					//cb.selectedColor = editorWrapper.combinerSelected ? new Color(0.7447f, 0.7247f, 0.7047f, 1f) : new Color(0.1647f, 0.1647f, 0.1647f, 1f);
					//cb.highlightedColor = editorWrapper.combinerSelected ? new Color(0.9447f, 0.9247f, 0.9047f, 1f) : new Color(0.2588f, 0.2588f, 0.2588f, 1f);
					//cb.pressedColor = editorWrapper.combinerSelected ? new Color(0.9447f, 0.9247f, 0.9047f, 1f) : new Color(0.2588f, 0.2588f, 0.2588f, 1f);
					//x.colors = cb;

					//text.color = editorWrapper.combinerSelected ? new Color(0.1173f, 0.0973f, 0.0973f, 1f) : new Color(0.9373f, 0.9216f, 0.9373f, 1f);
				};

				var button = gameObject.GetComponent<Button>();
				button.onClick.AddListener(delegate ()
				{
					editorWrapper.combinerSelected = !editorWrapper.combinerSelected;
					action.Invoke();
				});
				action.Invoke();

				var icon = new GameObject("icon");
				icon.transform.SetParent(gameObject.transform);
				icon.transform.localScale = Vector3.one;
				icon.layer = 5;
				var iconRT = icon.AddComponent<RectTransform>();
				icon.AddComponent<CanvasRenderer>();
				var iconImage = icon.AddComponent<Image>();

				iconRT.anchoredPosition = iconPosition;
				iconRT.sizeDelta = iconScale;

				iconImage.sprite = editorWrapper.albumArt ?? SteamWorkshop.inst.defaultSteamImageSprite;

				EditorThemeManager.ApplyElement(new EditorThemeManager.Element($"Level Combiner Button {num}", "List Button 1", gameObject, new List<Component>
				{
					gameObject.GetComponent<Image>(),
					button,
				}, true, 1, SpriteManager.RoundedSide.W, true));

				EditorThemeManager.ApplyElement(new EditorThemeManager.Element($"Level Button {num} Text", "Light Text", text.gameObject, new List<Component>
				{
					text,
				}));

				string difficultyName = difficultyNames[Mathf.Clamp(metadata.song.difficulty, 0, difficultyNames.Length - 1)];

				editorWrapper.CombinerSetActive((RTFile.FileExists(folder + "/level.ogg") ||
					RTFile.FileExists(folder + "/level.wav") ||
					RTFile.FileExists(folder + "/level.mp3")) && RTHelpers.SearchString(System.IO.Path.GetFileName(folder), searchTerm) ||
						RTHelpers.SearchString(metadata.song.title, searchTerm) ||
						RTHelpers.SearchString(metadata.artist.Name, searchTerm) ||
						RTHelpers.SearchString(metadata.creator.steam_name, searchTerm) ||
						RTHelpers.SearchString(metadata.song.description, searchTerm) ||
						RTHelpers.SearchString(difficultyName, searchTerm));

				editorWrapper.CombinerGameObject.transform.SetSiblingIndex(num);

				num++;
			}

            yield break;
        }

        public void Combine()
        {
			var combineList = new List<GameData>();
			var list = new List<string>();
			var paths = new List<string>();
			var selected = EditorManager.inst.loadedLevels.Select(x => x as EditorWrapper).Where(x => x.combinerSelected);

			foreach (var editorWrapper in selected)
            {
				if (RTFile.FileExists(editorWrapper.folder + "/level.lsb"))
                {
					Debug.Log($"{EditorManager.inst.className}Parsing GameData from {Path.GetFileName(editorWrapper.folder)}");
					paths.Add(editorWrapper.folder + "/level.lsb");
					list.Add(Path.GetFileName(editorWrapper.folder));
					combineList.Add(GameData.Parse(SimpleJSON.JSON.Parse(RTFile.ReadFromFile(editorWrapper.folder + "/level.lsb"))));
                }
            }

			Debug.Log($"{EditorManager.inst.className}Can Combine: {combineList.Count > 0 && !string.IsNullOrEmpty(savePath)}" +
				$"\nGameData Count: {combineList.Count}" +
				$"\nSavePath: {savePath}");

			if (combineList.Count > 0 && !string.IsNullOrEmpty(savePath))
            {
				var combinedGameData = ProjectData.Combiner.Combine(combineList.ToArray());

				string save = savePath;
				if (!save.Contains("level.lsb") && save.LastIndexOf('/') == save.Length - 1)
					save += "level.lsb";
				else if (!save.Contains("/level.lsb"))
					save += "/level.lsb";

				if (!save.Contains(RTFile.ApplicationDirectory) && !save.Contains(RTEditor.editorListSlash))
					save = RTFile.ApplicationDirectory + RTEditor.editorListSlash + save;
				else if (!save.Contains(RTFile.ApplicationDirectory))
					save = RTFile.ApplicationDirectory + save;

				foreach (var file in paths)
				{
					if (!RTFile.FileExists(file))
						return;

					var directory = Path.GetDirectoryName(save);
					if (!RTFile.DirectoryExists(directory))
						Directory.CreateDirectory(directory);

					var files1 = Directory.GetFiles(Path.GetDirectoryName(file));

					foreach (var file2 in files1)
					{
						string dir = Path.GetDirectoryName(file2);
						if (!RTFile.DirectoryExists(dir))
						{
							Directory.CreateDirectory(dir);
						}

						if (Path.GetFileName(file2) != "level.lsb" && !RTFile.FileExists(file2.Replace(Path.GetDirectoryName(file), directory)))
							File.Copy(file2, file2.Replace(Path.GetDirectoryName(file), directory));
					}
				}

				StartCoroutine(ProjectData.Writer.SaveData(save, combinedGameData, delegate ()
				{
					EditorManager.inst.DisplayNotification($"Combined {FontManager.TextTranslater.ArrayToString(list.ToArray())} to {savePath}!", 3f, EditorManager.NotificationType.Success);
				}));
			}

            if (selected.Count() < 1)
                EditorManager.inst.DisplayNotification("Cannot combine without a any levels selected!", 1f, EditorManager.NotificationType.Error);
			else if (string.IsNullOrEmpty(savePath))
				EditorManager.inst.DisplayNotification("Cannot combine with an empty path!", 1f, EditorManager.NotificationType.Error);
        }
    }
}
