using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

using LSFunctions;

using RTFunctions.Functions;
using RTFunctions.Functions.Data;
using RTFunctions.Functions.IO;

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

		public static Transform combinerContent;

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

            var label = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content").transform.GetChild(2).gameObject;
            var singleInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move/position/x");
            var vector2Input = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move/position");
            var boolInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/toggle/toggle");
            var sliderFullInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/bpm");
            var stringInput = GameObject.Find("TimelineBar/GameObject/Time Input");
            var dropdownInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/autokill/tod-dropdown");
            var colorsInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/color/color");

            editorDialogText = editorDialogTransform.GetChild(2);

            editorDialogText.SetSiblingIndex(1);

            editorDialogText.GetComponent<Text>().text = "To combine two levels, select two levels from the list below, set a save path and click save.";

			// Label
			{
				var label1 = Instantiate(editorDialogText.gameObject);
				label1.transform.SetParent(editorDialogTransform);
				label1.transform.localScale = Vector3.one;
				label1.name = "label";

				label1.GetComponent<RectTransform>().sizeDelta = new Vector2(765f, 32f);
				label1.GetComponent<Text>().text = "Pick two levels to combine";
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
                inst.StartCoroutine(RenderDialog());
            });

            search.transform.GetChild(0).Find("Placeholder").GetComponent<Text>().text = "Search for level...";

            var scrollView = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View"));
			scrollView.transform.SetParent(editorDialogTransform);
			scrollView.transform.localScale = Vector3.one;
            scrollView.name = "Scroll View";

			editorDialogContent = scrollView.transform.Find("Viewport/Content");

			//var glg = scrollView.AddComponent<GridLayoutGroup>();
			//glg.cellSize = new Vector2(124f, 64f);
			//glg.spacing = new Vector2(4f, 4f);

			LSHelpers.DeleteChildren(editorDialogContent, false);

            scrollView.GetComponent<RectTransform>().sizeDelta = new Vector2(765f, 256f);

			// Label
			{
				var label1 = Instantiate(editorDialogText.gameObject);
				label1.transform.SetParent(editorDialogTransform);
				label1.transform.localScale = Vector3.one;
				label1.name = "label";

				label1.GetComponent<RectTransform>().sizeDelta = new Vector2(765f, 32f);
				label1.GetComponent<Text>().text = "Combine list";
			}

			var combinerContentGO = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View"));
			combinerContent = combinerContentGO.transform.Find("Viewport/Content");
			combinerContentGO.transform.SetParent(editorDialogTransform);
			combinerContentGO.transform.localScale = Vector3.one;
			combinerContentGO.transform.name = "Combine List";

			combinerContentGO.GetComponent<RectTransform>().sizeDelta = new Vector2(765f, 76f);

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
			{

				var propWin = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/Edit/Edit Dropdown/Cut"));
				propWin.transform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/File/File Dropdown").transform);
				propWin.transform.localScale = Vector3.one;
				propWin.transform.SetSiblingIndex(4);
				propWin.name = "Level Combiner";
				propWin.transform.Find("Text").GetComponent<Text>().text = "Level Combiner";
				propWin.transform.Find("Text").GetComponent<RectTransform>().sizeDelta = new Vector2(224f, 0f);
				propWin.transform.Find("Text 1").GetComponent<Text>().text = "";

				TooltipHelper.AddTooltip(propWin, "Open Level Combiner", "Here you can combine two levels together, allowing for collaborations to be better organized.");

				var propWinButton = propWin.GetComponent<Button>();
				propWinButton.onClick.ClearAll();
				propWinButton.onClick.AddListener(delegate ()
				{
					OpenDialog();
				});

				string jpgFileLocation = "BepInEx/plugins/Assets/editor_gui_combine_t.png";

				if (RTFile.FileExists(jpgFileLocation))
				{
					Image spriteReloader = propWin.transform.Find("Image").GetComponent<Image>();

					EditorManager.inst.StartCoroutine(EditorManager.inst.GetSprite(RTFile.ApplicationDirectory + jpgFileLocation, new EditorManager.SpriteLimits(), delegate (Sprite cover)
					{
						spriteReloader.sprite = cover;
					}, delegate (string errorFile)
					{
						spriteReloader.sprite = ArcadeManager.inst.defaultImage;
					}));
				}

				propWin.SetActive(true);

				propWin.transform.Find("Image").GetComponent<Image>().sprite = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/parent/parent/image").GetComponent<Image>().sprite;
			}
		}

        void Update()
        {

        }

        public static void OpenDialog()
        {
            if (inst == null)
                return;

            Debug.LogFormat("{0}Attempting to open Level Combiner!", EditorPlugin.className);
            EditorManager.inst.ShowDialog("Level Combiner");
            inst.StartCoroutine(inst.RenderDialog());
        }

        public IEnumerator RenderDialog()
        {
			var close = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("Panel/x");
			//foreach (object obj in transform)
			//{
			//	Destroy(((Transform)obj).gameObject);
			//}

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
			LSHelpers.DeleteChildren(combinerContent);

			foreach (var metadataWrapper in EditorManager.inst.loadedLevels)
			{
				if (metadataWrapper.metadata == null || !(metadataWrapper.metadata is Metadata))
					continue;

				var metadata = (Metadata)metadataWrapper.metadata;
				string name = metadataWrapper.folder;

				string difficultyName = "None";

				string[] difficultyNames = new string[]
				{
					"easy",
					"normal",
					"hard",
					"expert",
					"expert+",
					"master",
					"animation",
				};

				difficultyName = difficultyNames[metadata.song.difficulty];

				if (RTFile.FileExists(RTEditor.editorListSlash + metadataWrapper.folder + "/level.ogg"))
				{
					if (((first == null || first.folder != metadataWrapper.folder) && (second == null || second.folder != metadataWrapper.folder)) && (searchTerm == null
						|| !(searchTerm != "")
						|| name.ToLower().Contains(searchTerm.ToLower())
						|| metadata.song.title.ToLower().Contains(searchTerm.ToLower())
						|| metadata.artist.Name.ToLower().Contains(searchTerm.ToLower())
						|| metadata.creator.steam_name.ToLower().Contains(searchTerm.ToLower())
						|| metadata.song.description.ToLower().Contains(searchTerm.ToLower())
						|| difficultyName.Contains(searchTerm.ToLower())))
					{
						CreateFolder(metadata, metadataWrapper, foldClamp, songClamp, artiClamp, creaClamp, descClamp, dateClamp, editorDialogContent, delegate ()
						{
                            if (first == null)
                            {
                                Debug.Log($"{EditorPlugin.className}Selected {metadataWrapper.folder} and set it to first selected.");
                                first = metadataWrapper;
                            }
                            else
                            {
                                Debug.Log($"{EditorPlugin.className}Selected {metadataWrapper.folder} and set it to second selected.");
                                second = metadataWrapper;
                            }

                            inst.StartCoroutine(RenderDialog());
                        });
                    }
					else if (first != null && first.folder == metadataWrapper.folder || second != null && second.folder == metadataWrapper.folder)
					{
						CreateFolder(metadata, metadataWrapper, foldClamp, songClamp, artiClamp, creaClamp, descClamp, dateClamp, combinerContent, delegate ()
						{
                            if (first != null && first.folder == metadataWrapper.folder)
                            {
                                Debug.Log($"{EditorPlugin.className}Removed {metadataWrapper.folder} from first selected.");
                                first = null;
                            }
                            else if (second != null && second.folder == metadataWrapper.folder)
                            {
                                Debug.Log($"{EditorPlugin.className}Removed {metadataWrapper.folder} from second selected.");
                                second = null;
                            }

                            inst.StartCoroutine(RenderDialog());
                        });
                    }
				}
			}

			yield break;
        }

		public void CreateFolder(Metadata metadata, EditorManager.MetadataWrapper metadataWrapper,
			int foldClamp, int songClamp, int artiClamp, int creaClamp, int descClamp, int dateClamp,
			Transform parent, UnityAction onClick)
        {
			var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(parent, $"Folder [{metadataWrapper.folder}]");

			var hoverUI = gameObject.AddComponent<HoverUI>();
			hoverUI.size = RTEditor.GetEditorProperty("Open Level Button Hover Size").GetConfigEntry<float>().Value;
			hoverUI.animatePos = false;
			hoverUI.animateSca = true;

			var htt = gameObject.AddComponent<HoverTooltip>();

			var levelTip = new HoverTooltip.Tooltip();

			var text = gameObject.transform.GetChild(0).GetComponent<Text>();

			if (metadata != null)
			{
				text.text =
					string.Format(RTEditor.GetEditorProperty("Open Level Text Formatting").GetConfigEntry<string>().Value,
					LSText.ClampString(metadataWrapper.folder, foldClamp),
					LSText.ClampString(metadata.song.title, songClamp),
					LSText.ClampString(metadata.artist.Name, artiClamp),
					LSText.ClampString(metadata.creator.steam_name, creaClamp),
					metadata.song.difficulty,
					LSText.ClampString(metadata.song.description, descClamp),
					LSText.ClampString(metadata.beatmap.date_edited, dateClamp));

				text.horizontalOverflow = RTEditor.GetEditorProperty("Open Level Text Horizontal Wrap").GetConfigEntry<HorizontalWrapMode>().Value;
				text.verticalOverflow = RTEditor.GetEditorProperty("Open Level Text Vertical Wrap").GetConfigEntry<VerticalWrapMode>().Value;
				//text.color = ConfigEntries.OpenFileTextColor.Value;
				text.fontSize = RTEditor.GetEditorProperty("Open Level Text Font Size").GetConfigEntry<int>().Value;

				var difficultyColor = metadata.song.difficulty >= 0 && metadata.song.difficulty < DataManager.inst.difficulties.Count ?
					DataManager.inst.difficulties[metadata.song.difficulty].color : LSColors.themeColors["none"].color;

				levelTip.desc = "<#" + LSColors.ColorToHex(difficultyColor) + ">" + metadata.artist.Name + " - " + metadata.song.title;
				levelTip.hint = "</color>" + metadata.song.description;
				htt.tooltipLangauges.Add(levelTip);
			}
			else
			{
				text.GetComponent<Text>().text = string.Format("/{0} : {1}", LSText.ClampString(metadataWrapper.folder, foldClamp), LSText.ClampString("No MetaData File", songClamp));
			}

			gameObject.GetComponent<Button>().onClick.AddListener(onClick);

			var icon = new GameObject("icon");
			icon.transform.SetParent(gameObject.transform);
			icon.transform.localScale = Vector3.one;
			icon.layer = 5;
			var iconRT = icon.AddComponent<RectTransform>();
			icon.AddComponent<CanvasRenderer>();
			var iconImage = icon.AddComponent<Image>();

			iconRT.anchoredPosition = RTEditor.GetEditorProperty("Open Level Cover Position").GetConfigEntry<Vector2>().Value;
			iconRT.sizeDelta = RTEditor.GetEditorProperty("Open Level Cover Scale").GetConfigEntry<Vector2>().Value;

			iconImage.sprite = metadataWrapper.albumArt;
		}

        public void Combine()
        {
            if (first != null || second != null || !string.IsNullOrEmpty(savePath))
			{
				ProjectData.Writer.onSave = delegate ()
				{
					EditorManager.inst.DisplayNotification($"Combined {first.folder} and {second.folder} to {savePath}!", 3f, EditorManager.NotificationType.Success);
				};

				string save = savePath;
				if (!save.Contains("level.lsb") && save.LastIndexOf('/') == save.Length - 1)
					save += "level.lsb";
				else if (!save.Contains("/level.lsb"))
					save += "/level.lsb";

				if (!save.Contains(RTFile.ApplicationDirectory) && !save.Contains(RTEditor.editorListSlash))
					save = RTFile.ApplicationDirectory + RTEditor.editorListSlash + save;
				else if (!save.Contains(RTFile.ApplicationDirectory))
					save = RTFile.ApplicationDirectory + save;

				ProjectData.Combiner.Combine(
					RTFile.ApplicationDirectory + RTEditor.editorListSlash + first.folder + "/level.lsb",
					RTFile.ApplicationDirectory + RTEditor.editorListSlash + second.folder + "/level.lsb",
					save);
			}

			if (first == null)
				EditorManager.inst.DisplayNotification("Cannot combine without a first level selected!", 1f, EditorManager.NotificationType.Error);

			if (second == null)
				EditorManager.inst.DisplayNotification("Cannot combine without a second level selected!", 1f, EditorManager.NotificationType.Error);

			if (string.IsNullOrEmpty(savePath))
				EditorManager.inst.DisplayNotification("Cannot combine with an empty path!", 1f, EditorManager.NotificationType.Error);
        }
    }
}
