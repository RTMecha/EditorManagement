using System;
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

        public static Font editorFont;

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

            editorFont = GameObject.Find("TitleBar/File/Text").GetComponent<Text>().font;

            editorDialogObject = Instantiate(EditorManager.inst.GetDialog("Multi Keyframe Editor (Object)").Dialog.gameObject);
            editorDialogTransform = editorDialogObject.transform;
            editorDialogObject.name = "LevelCombinerDialog";
            editorDialogObject.layer = 5;
            editorDialogTransform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs").transform);
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

            editorDialogText.GetComponent<Text>().text = "To combine levels into one, select multiple levels from the list below, set a save path and click save.";

			// Label
			{
				var label1 = Instantiate(editorDialogText.gameObject);
				label1.transform.SetParent(editorDialogTransform);
				label1.transform.localScale = Vector3.one;
				label1.name = "label";

				label1.GetComponent<RectTransform>().sizeDelta = new Vector2(765f, 32f);
				label1.GetComponent<Text>().text = "Select levels to combine";
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

            search.transform.GetChild(0).Find("Placeholder").GetComponent<Text>().text = "Search for level...";

            var scrollView = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View"));
			scrollView.transform.SetParent(editorDialogTransform);
			scrollView.transform.localScale = Vector3.one;
            scrollView.name = "Scroll View";

			editorDialogContent = scrollView.transform.Find("Viewport/Content");

			LSHelpers.DeleteChildren(editorDialogContent);

            scrollView.GetComponent<RectTransform>().sizeDelta = new Vector2(765f, 392f);

			EditorHelper.AddEditorDialog("Level Combiner", editorDialogObject);

			// Save
			{
				// Label
				{
					var label1 = Instantiate(editorDialogText.gameObject);
					label1.transform.SetParent(editorDialogTransform);
					label1.transform.localScale = Vector3.one;
					label1.name = "label";

					label1.GetComponent<RectTransform>().sizeDelta = new Vector2(765f, 32f);
					label1.GetComponent<Text>().text = "Save path";
				}

				var save = Instantiate(EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("search-box").gameObject);
				save.transform.SetParent(editorDialogTransform);
				save.transform.localScale = Vector3.one;
				save.name = "search";

				saveField = save.transform.GetChild(0).GetComponent<InputField>();

				saveField.onValueChanged.ClearAll();
				saveField.characterLimit = 0;
				saveField.text = RTFile.ApplicationDirectory + RTEditor.editorListSlash + "Combined Level/level.lsb";
				savePath = RTFile.ApplicationDirectory + RTEditor.editorListSlash + "Combined Level/level.lsb";
				saveField.onValueChanged.AddListener(delegate (string _val)
				{
					savePath = _val;
				});

				search.transform.GetChild(0).Find("Placeholder").GetComponent<Text>().text = "Set a path...";

				//Button 1
				{
					var b1 = Instantiate(EditorManager.inst.folderButtonPrefab);
					b1.transform.SetParent(editorDialogTransform);
					b1.transform.localScale = Vector3.one;
					b1.name = "combine";

					var b1RT = b1.GetComponent<RectTransform>();
					b1RT.anchoredPosition = new Vector2(436f, 55f);
					b1RT.anchoredPosition = new Vector2(436f, 55f);
					//b1RT.anchoredPosition = new Vector2(366f, 55f);
					b1RT.sizeDelta = new Vector2(100f, 50f);

					b1.transform.GetChild(0).GetComponent<Text>().text = "Combine & Save";
					var butt = b1.GetComponent<Button>();
					butt.onClick.RemoveAllListeners();
					butt.onClick.AddListener(delegate ()
					{
						Combine();
					});
				}

			}

			// Dropdown
			EditorHelper.AddEditorDropdown("Level Combiner", "", "File", SpriteManager.LoadSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_combine_t.png"), delegate ()
			{
				OpenDialog();
			}, 4);
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
			#region Clamping

			var olfnm = RTEditor.GetEditorProperty("Open Level Folder Name Max").GetConfigEntry<int>();
			var olsnm = RTEditor.GetEditorProperty("Open Level Song Name Max").GetConfigEntry<int>();
			var olanm = RTEditor.GetEditorProperty("Open Level Artist Name Max").GetConfigEntry<int>();
			var olcnm = RTEditor.GetEditorProperty("Open Level Creator Name Max").GetConfigEntry<int>();
			var oldem = RTEditor.GetEditorProperty("Open Level Description Max").GetConfigEntry<int>();
			var oldam = RTEditor.GetEditorProperty("Open Level Date Max").GetConfigEntry<int>();

			int foldClamp = olfnm.Value < 3 ? olfnm.Value : (int)olfnm.DefaultValue;
			int songClamp = olsnm.Value < 3 ? olsnm.Value : (int)olsnm.DefaultValue;
			int artiClamp = olanm.Value < 3 ? olanm.Value : (int)olanm.DefaultValue;
			int creaClamp = olcnm.Value < 3 ? olcnm.Value : (int)olcnm.DefaultValue;
			int descClamp = oldem.Value < 3 ? oldem.Value : (int)oldem.DefaultValue;
			int dateClamp = oldam.Value < 3 ? oldam.Value : (int)oldam.DefaultValue;

			#endregion

			LSHelpers.DeleteChildren(editorDialogContent);

			var horizontalOverflow = RTEditor.GetEditorProperty("Open Level Text Horizontal Wrap").GetConfigEntry<HorizontalWrapMode>().Value;
			var verticalOverflow = RTEditor.GetEditorProperty("Open Level Text Vertical Wrap").GetConfigEntry<VerticalWrapMode>().Value;
			var fontSize = RTEditor.GetEditorProperty("Open Level Text Font Size").GetConfigEntry<int>().Value;
			var format = RTEditor.GetEditorProperty("Open Level Text Formatting").GetConfigEntry<string>().Value;
			var buttonHoverSize = RTEditor.GetEditorProperty("Open Level Button Hover Size").GetConfigEntry<float>().Value;

			var iconPosition = RTEditor.GetEditorProperty("Open Level Cover Position").GetConfigEntry<Vector2>().Value;
			var iconScale = RTEditor.GetEditorProperty("Open Level Cover Scale").GetConfigEntry<Vector2>().Value;
			iconPosition.x += -80f;

			int num = 0;
			foreach (var editorWrapper in EditorManager.inst.loadedLevels.Select(x => x as EditorWrapper))
            {
				var folder = editorWrapper.folder;
				var metadata = editorWrapper.metadata;

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

				DestroyImmediate(editorWrapper.CombinerGameObject);

                {
					var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(editorDialogContent, $"Folder [{System.IO.Path.GetFileName(editorWrapper.folder)}]");
					editorWrapper.CombinerGameObject = gameObject;

					var hoverUI = gameObject.AddComponent<HoverUI>();
					hoverUI.size = buttonHoverSize;
					hoverUI.animatePos = false;
					hoverUI.animateSca = true;

					var text = gameObject.transform.GetChild(0).GetComponent<Text>();

					text.text = string.Format(format,
						LSText.ClampString(System.IO.Path.GetFileName(editorWrapper.folder), foldClamp),
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

					Action<Button> action = delegate (Button x)
					{
						var cb = x.colors;
						cb.normalColor = editorWrapper.combinerSelected ? new Color(0.7447f, 0.7247f, 0.7047f, 1f) : new Color(0.1647f, 0.1647f, 0.1647f, 1f);
						cb.selectedColor = editorWrapper.combinerSelected ? new Color(0.7447f, 0.7247f, 0.7047f, 1f) : new Color(0.1647f, 0.1647f, 0.1647f, 1f);
						cb.highlightedColor = editorWrapper.combinerSelected ? new Color(0.9447f, 0.9247f, 0.9047f, 1f) : new Color(0.2588f, 0.2588f, 0.2588f, 1f);
						cb.pressedColor = editorWrapper.combinerSelected ? new Color(0.9447f, 0.9247f, 0.9047f, 1f) : new Color(0.2588f, 0.2588f, 0.2588f, 1f);
						x.colors = cb;

						text.color = editorWrapper.combinerSelected ? new Color(0.1173f, 0.0973f, 0.0973f, 1f) : new Color(0.9373f, 0.9216f, 0.9373f, 1f);
					};

					var button = gameObject.GetComponent<Button>();
					button.onClick.AddListener(delegate ()
					{
						editorWrapper.combinerSelected = !editorWrapper.combinerSelected;
						action.Invoke(button);
					});
					action.Invoke(button);

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
				}

				string difficultyName = difficultyNames[Mathf.Clamp(metadata.song.difficulty, 0, difficultyNames.Length - 1)];

				editorWrapper.CombinerSetActive((RTFile.FileExists(folder + "/level.ogg") ||
					RTFile.FileExists(folder + "/level.wav") ||
					RTFile.FileExists(folder + "/level.mp3")) && RTHelpers.SearchString(System.IO.Path.GetFileName(folder), EditorManager.inst.openFileSearch) ||
						RTHelpers.SearchString(metadata.song.title, EditorManager.inst.openFileSearch) ||
						RTHelpers.SearchString(metadata.artist.Name, EditorManager.inst.openFileSearch) ||
						RTHelpers.SearchString(metadata.creator.steam_name, EditorManager.inst.openFileSearch) ||
						RTHelpers.SearchString(metadata.song.description, EditorManager.inst.openFileSearch) ||
						RTHelpers.SearchString(difficultyName, EditorManager.inst.openFileSearch));

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
