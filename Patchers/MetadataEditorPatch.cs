using EditorManagement.Functions.Editors;
using HarmonyLib;
using LSFunctions;
using RTFunctions.Functions;
using RTFunctions.Functions.Data;
using RTFunctions.Functions.IO;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace EditorManagement.Patchers
{
    [HarmonyPatch(typeof(MetadataEditor))]
    public class MetadataEditorPatch : MonoBehaviour
    {
		static MetadataEditor Instance { get => MetadataEditor.inst; set => MetadataEditor.inst = value; }

		static GameObject difficultyToggle;

		static GameObject tagPrefab;

		[HarmonyPatch("Awake")]
		[HarmonyPrefix]
		static bool Awake(MetadataEditor __instance)
		{
			if (Instance == null)
				Instance = __instance;
			else if (Instance != __instance)
				Destroy(__instance.gameObject);

			Instance.StartCoroutine(Wait());

			return false;
		}

		static IEnumerator Wait()
        {
			yield return new WaitForSeconds(0.2f);

			var content = EditorManager.inst.GetDialog("Metadata Editor").Dialog.Find("Scroll View/Viewport/Content");

			difficultyToggle = content.Find("song/difficulty/toggles/easy").gameObject;
			difficultyToggle.transform.SetParent(null);
			LSHelpers.DeleteChildren(content.Find("song/difficulty/toggles"));
			Destroy(content.Find("song/difficulty/toggles").GetComponent<ToggleGroup>());

			// Button
			{
				if (!content.Find("artist/link/inputs/openurl"))
				{
					var openLink = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("Panel/x").gameObject.Duplicate(content.Find("artist/link/inputs"), "openurl", 0);
					openLink.transform.Find("Image").gameObject.GetComponent<Image>().sprite = EditorManager.inst.DropdownMenus[3].transform.Find("Open Workshop").Find("Image").gameObject.GetComponent<Image>().sprite;

					var openLinkRT = (RectTransform)openLink.transform;
					var openLinkLE = openLink.AddComponent<LayoutElement>();
					var openLinkButton = openLink.GetComponent<Button>();

					openLinkLE.minWidth = 32f;
					openLinkButton.onClick.RemoveAllListeners();
					//openLinkButton.onClick.AddListener(delegate ()
					//{
					//	Application.OpenURL(DataManager.inst.metaData.artist.getUrl());
					//});

					var cb = openLinkButton.colors;
					cb.normalColor = new Color(0f, 0.5f, 1f, 1f);
					cb.pressedColor = new Color(0.6f, 0.9f, 1f, 1f);
					cb.highlightedColor = new Color(0.3f, 0.6f, 1f, 1f);
					cb.selectedColor = new Color(0f, 0.5f, 1f, 1f);
					openLinkButton.colors = cb;
				}
			}

			if (!content.Find("creator/link"))
			{
				var gameObject = content.Find("artist/link").gameObject.Duplicate(content.Find("creator"), "link", 3);
			}

            {
				var tagParent = content.Find("creator/description (1)");
				tagParent.name = "tags";
				tagParent.gameObject.SetActive(true);
				Destroy(tagParent.Find("input").gameObject);

				((RectTransform)tagParent).sizeDelta = new Vector2(757f, 32f);

				tagParent.Find("Panel/title").GetComponent<Text>().text = "Tags";

				var tagScrollView = new GameObject("Scroll View");
				tagScrollView.transform.SetParent(tagParent);
				tagScrollView.transform.localScale = Vector3.one;

				var tagScrollViewRT = tagScrollView.AddComponent<RectTransform>();
				tagScrollViewRT.sizeDelta = new Vector2(522f, 40f);
				var scroll = tagScrollView.AddComponent<ScrollRect>();
				//layout.AddComponent<Mask>();
				//var image = layout.AddComponent<Image>();

				scroll.horizontal = true;
				scroll.vertical = false;

				var image = tagScrollView.AddComponent<Image>();
				image.color = new Color(1f, 1f, 1f, 0.01f);

				var mask = tagScrollView.AddComponent<Mask>();

				var tagViewport = new GameObject("Viewport");
				tagViewport.transform.SetParent(tagScrollViewRT);
				tagViewport.transform.localScale = Vector3.one;

				var tagViewPortRT = tagViewport.AddComponent<RectTransform>();
				tagViewPortRT.anchoredPosition = Vector2.zero;
				tagViewPortRT.anchorMax = Vector2.one;
				tagViewPortRT.anchorMin = Vector2.zero;
				tagViewPortRT.sizeDelta = Vector2.zero;

				var tagContent = new GameObject("Content");
				tagContent.transform.SetParent(tagViewPortRT);
				tagContent.transform.localScale = Vector3.one;

				var tagContentRT = tagContent.AddComponent<RectTransform>();

				var tagContentGLG = tagContent.AddComponent<GridLayoutGroup>();
				tagContentGLG.cellSize = new Vector2(168f, 32f);
				tagContentGLG.constraint = GridLayoutGroup.Constraint.FixedRowCount;
				tagContentGLG.constraintCount = 1;
				tagContentGLG.childAlignment = TextAnchor.MiddleLeft;
				tagContentGLG.spacing = new Vector2(8f, 0f);

				var tagContentCSF = tagContent.AddComponent<ContentSizeFitter>();
				tagContentCSF.horizontalFit = ContentSizeFitter.FitMode.MinSize;
				tagContentCSF.verticalFit = ContentSizeFitter.FitMode.MinSize;

				scroll.viewport = tagViewPortRT;
				scroll.content = tagContentRT;

				tagPrefab = new GameObject("Tag");
				var tagPrefabRT = tagPrefab.AddComponent<RectTransform>();
				var tagPrefabImage = tagPrefab.AddComponent<Image>();
				tagPrefabImage.color = new Color(1f, 1f, 1f, 0.12f);
				var tagPrefabLayout = tagPrefab.AddComponent<HorizontalLayoutGroup>();
				tagPrefabLayout.childControlWidth = false;
				tagPrefabLayout.childForceExpandWidth = false;

				var input = RTEditor.inst.defaultIF.Duplicate(tagPrefabRT, "Input");
				((RectTransform)input.transform).sizeDelta = new Vector2(136f, 32f);
				input.transform.Find("Text").GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
				input.transform.Find("Text").GetComponent<Text>().fontSize = 17;

				var delete = EditorManager.inst.GetDialog("Quick Actions Popup").Dialog.Find("Panel/x").gameObject.Duplicate(tagPrefabRT, "Delete");
				((RectTransform)delete.transform).sizeDelta = new Vector2(32f, 32f);
			}
		}

		static void SetToggleList()
		{
			var content = EditorManager.inst.GetDialog("Metadata Editor").Dialog.Find("Scroll View/Viewport/Content");
			var toggles = content.Find("song/difficulty/toggles");
			LSHelpers.DeleteChildren(toggles);
			int num = 0;
			foreach (var difficulty in DataManager.inst.difficulties)
            {
				int index = num;
				var gameObject = difficultyToggle.Duplicate(toggles, difficulty.name.ToLower());
				gameObject.transform.localScale = Vector3.one;

				((RectTransform)gameObject.transform).sizeDelta = new Vector2(69f, 32f);

				gameObject.transform.Find("Background").GetComponent<Image>().color = difficulty.color;
				var text = gameObject.transform.Find("Background/Text").GetComponent<Text>();
				text.color = LSColors.ContrastColor(difficulty.color);
				text.text = num == DataManager.inst.difficulties.Count - 1 ? "Anim" : difficulty.name;
				text.fontSize = 17;
				var toggle = gameObject.GetComponent<Toggle>();
				toggle.group = null;
				toggle.onValueChanged.ClearAll();
				toggle.isOn = DataManager.inst.metaData.song.difficulty == num;
				toggle.onValueChanged.AddListener(delegate (bool _val)
				{
					if (_val)
                    {
						DataManager.inst.metaData.song.difficulty = index;
						SetToggleList();
                    }
				});

				num++;
			}
		}

		[HarmonyPatch("Render")]
		[HarmonyPrefix]
		static bool Render()
		{
			Debug.Log($"{Instance.className}Render the Metadata Editor!");

            var content = EditorManager.inst.GetDialog("Metadata Editor").Dialog.Find("Scroll View/Viewport/Content");

			((RectTransform)content.Find("spacer (1)")).sizeDelta = new Vector2(732f, 80f);

			var openArtistURL = content.Find("artist/link/inputs/openurl").GetComponent<Button>();
			openArtistURL.onClick.ClearAll();
			openArtistURL.onClick.AddListener(delegate ()
			{
				Application.OpenURL(DataManager.inst.metaData.artist.getUrl());
			});

			var artistName = content.Find("artist/name/input").GetComponent<InputField>();
			artistName.onEndEdit.RemoveAllListeners();
			artistName.text = DataManager.inst.metaData.artist.Name;
			artistName.onEndEdit.AddListener(delegate (string _val)
			{
				string oldVal = DataManager.inst.metaData.artist.Name;
				DataManager.inst.metaData.artist.Name = _val;
				EditorManager.inst.history.Add(new History.Command("Change Artist Name", delegate ()
				{
					DataManager.inst.metaData.artist.Name = _val;
					Instance.Render();
				}, delegate ()
				{
					DataManager.inst.metaData.artist.Name = oldVal;
					Instance.Render();
				}), false);
			});

			var artistLink = content.Find("artist/link/inputs/input").GetComponent<InputField>();
			artistLink.onEndEdit.RemoveAllListeners();
			artistLink.text = DataManager.inst.metaData.artist.Link;
            artistLink.onEndEdit.AddListener(delegate (string _val)
            {
                string oldVal = DataManager.inst.metaData.artist.Link;
                DataManager.inst.metaData.artist.Link = _val;
                EditorManager.inst.history.Add(new History.Command("Change Artist Link", delegate ()
                {
                    DataManager.inst.metaData.artist.Link = _val;
					Instance.Render();
                }, delegate ()
                {
                    DataManager.inst.metaData.artist.Link = oldVal;
					Instance.Render();
                }), false);
            });

			var artistLinkTypes = content.Find("artist/link/inputs/dropdown").GetComponent<Dropdown>();
			artistLinkTypes.options.Clear();
			artistLinkTypes.onValueChanged.RemoveAllListeners();
			foreach (var linkType in DataManager.inst.linkTypes)
			{
				artistLinkTypes.options.Add(new Dropdown.OptionData(linkType.name));
			}
			artistLinkTypes.value = DataManager.inst.metaData.artist.LinkType;
			artistLinkTypes.onValueChanged.AddListener(delegate (int _val)
			{
				int oldVal = DataManager.inst.metaData.artist.LinkType;
				DataManager.inst.metaData.artist.LinkType = _val;
				EditorManager.inst.history.Add(new History.Command("Change Artist Link", delegate ()
				{
					DataManager.inst.metaData.artist.LinkType = _val;
					Instance.Render();
				}, delegate ()
				{
					DataManager.inst.metaData.artist.LinkType = oldVal;
					Instance.Render();
				}), false);
			});

			var creatorName = content.Find("creator/name/input").GetComponent<InputField>();
			creatorName.onValueChanged.RemoveAllListeners();
			creatorName.text = DataManager.inst.metaData.creator.steam_name;
			creatorName.onValueChanged.AddListener(delegate (string _val)
			{
				DataManager.inst.metaData.creator.steam_name = _val;
			});

			var songTitle = content.Find("song/title/input").GetComponent<InputField>();
			songTitle.onValueChanged.RemoveAllListeners();
			songTitle.text = DataManager.inst.metaData.song.title;
			songTitle.onValueChanged.AddListener(delegate (string _val)
			{
				DataManager.inst.metaData.song.title = _val;
			});

			var moddedMetadata = MetaData.Current;

			var openCreatorURL = content.Find("creator/link/inputs/openurl").GetComponent<Button>();
			openCreatorURL.onClick.ClearAll();
			openCreatorURL.onClick.AddListener(delegate ()
			{
				if (moddedMetadata.LevelCreator.URL != null)
					Application.OpenURL(moddedMetadata.LevelCreator.URL);
			});

			var creatorLink = content.Find("creator/link/inputs/input").GetComponent<InputField>();
			creatorLink.onEndEdit.RemoveAllListeners();
			creatorLink.text = moddedMetadata.LevelCreator.link;
			creatorLink.onEndEdit.AddListener(delegate (string _val)
			{
				string oldVal = moddedMetadata.LevelCreator.link;
				moddedMetadata.LevelCreator.link = _val;
				EditorManager.inst.history.Add(new History.Command("Change Artist Link", delegate ()
				{
					moddedMetadata.LevelCreator.link = _val;
					Instance.Render();
				}, delegate ()
				{
					moddedMetadata.LevelCreator.link = oldVal;
					Instance.Render();
				}), false);
			});

			var creatorLinkTypes = content.Find("creator/link/inputs/dropdown").GetComponent<Dropdown>();
			creatorLinkTypes.options.Clear();
			creatorLinkTypes.onValueChanged.RemoveAllListeners();
			foreach (var linkType in LevelCreator.creatorLinkTypes)
			{
				creatorLinkTypes.options.Add(new Dropdown.OptionData(linkType.name));
			}

			creatorLinkTypes.value = moddedMetadata.LevelCreator.linkType;
			creatorLinkTypes.onValueChanged.AddListener(delegate (int _val)
			{
				int oldVal = moddedMetadata.LevelCreator.linkType;
				moddedMetadata.LevelCreator.linkType = _val;
				EditorManager.inst.history.Add(new History.Command("Change Artist Link", delegate ()
				{
					moddedMetadata.LevelCreator.linkType = _val;
					Instance.Render();
				}, delegate ()
				{
					moddedMetadata.LevelCreator.linkType = oldVal;
					Instance.Render();
				}), false);
			});

			((RectTransform)content.Find("creator/description")).sizeDelta = new Vector2(757f, 140f);
			((RectTransform)content.Find("creator/description/input")).sizeDelta = new Vector2(523f, 140f);
			var creatorDescription = content.Find("creator/description/input").GetComponent<InputField>();
			creatorDescription.onValueChanged.RemoveAllListeners();
			creatorDescription.text = DataManager.inst.metaData.song.description;
			creatorDescription.onValueChanged.AddListener(delegate (string _val)
			{
				DataManager.inst.metaData.song.description = _val;
			});

			SetToggleList();

			RenderTags();
			content.Find("agreement/text").GetComponent<Text>().text = "Currently there is no way to upload to any online service. A custom online arcade is planned, so stay tuned! For now, " +
				"you can convert the level to the current level format for vanilla PA and upload it to the workshop. Beware any modded features not in current PA will not be saved.";
			for (int i = 5; i < 9; i++)
            {
				content.GetChild(i).gameObject.SetActive(false);
            }

            try
            {
				content.Find("spacer").gameObject.SetActive(true);
				content.Find("submit").gameObject.SetActive(true);
				var button = content.Find("submit/submit").GetComponent<Button>();
				button.GetComponent<Image>().sprite = null;
				content.Find("submit/submit").AsRT().sizeDelta = new Vector2(300f, 48f);
				content.Find("submit/submit/Text").GetComponent<Text>().text = "Convert to VG Format";

				button.onClick.ClearAll();
				button.onClick.AddListener(delegate ()
				{
					var exportPath = RTEditor.GetEditorProperty("Convert Level LS to VG Export Path").GetConfigEntry<string>().Value;

					if (string.IsNullOrEmpty(exportPath))
					{
						if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + "beatmaps/exports"))
							Directory.CreateDirectory(RTFile.ApplicationDirectory + "beatmaps/exports");
						exportPath = RTFile.ApplicationDirectory + "beatmaps/exports/";
					}

					if (exportPath[exportPath.Length - 1] != '/')
						exportPath += "/";

					if (!RTFile.DirectoryExists(Path.GetDirectoryName(exportPath)))
                    {
						EditorManager.inst.DisplayNotification("Directory does not exist.", 2f, EditorManager.NotificationType.Error);
						return;
                    }

					var vg = GameData.Current.ToJSONVG();

					var metadata = MetaData.Current.ToJSONVG();

					var path = exportPath + EditorManager.inst.currentLoadedLevel;

					if (!RTFile.DirectoryExists(path))
						Directory.CreateDirectory(path);

					var ogPath = GameManager.inst.path.Replace("/level.lsb", "");

					if (RTFile.FileExists(ogPath + "/level.ogg"))
                    {
						File.Copy(ogPath + "/level.ogg", path + "/audio.ogg", RTFile.FileExists(path + "/audio.ogg"));
                    }

					if (RTFile.FileExists(ogPath + "/level.jpg"))
                    {
						File.Copy(ogPath + "/level.jpg", path + "/cover.jpg", RTFile.FileExists(path + "/cover.jpg"));
                    }

					try
					{
						RTFile.WriteToFile(path + "/metadata.vgm", metadata.ToString());
					}
					catch (Exception ex)
					{
						Debug.LogError($"{Instance.className}Convert to VG error (MetaData) {ex}");
					}

					try
					{
						RTFile.WriteToFile(path + "/level.vgd", vg.ToString());
					}
                    catch (Exception ex)
                    {
						Debug.LogError($"{Instance.className}Convert to VG error (GameData) {ex}");
					}

					EditorManager.inst.DisplayNotification($"Converted Level \"{EditorManager.inst.currentLoadedLevel}\" from LS format to VG format and saved to {Path.GetFileName(path)}.", 4f,
						EditorManager.NotificationType.Success);
				});
			}
			catch (Exception ex)
            {
				Debug.LogError(ex);
            }

			//bool hasID = DataManager.inst.metaData.beatmap.workshop_id != -1;
			//content.Find("id/id").GetComponent<Text>().text = hasID ? ("ID: " + DataManager.inst.metaData.beatmap.workshop_id.ToString()) : "Upload to get more info!";
			//content.Find("id/revisions").gameObject.SetActive(hasID);
			//content.Find("id/revisions").GetComponent<Text>().text = hasID ? ("Version: " + DataManager.inst.metaData.beatmap.version_number.ToString()) : "";
			//content.Find("submit/submit").GetComponentInChildren<Text>().text = hasID ? "Update" : "Upload";

			//var uploadButton = content.Find("submit/submit").GetComponent<Button>();
			//uploadButton.onClick.RemoveAllListeners();
			//uploadButton.onClick.AddListener(delegate ()
			//{
			//	Instance.StartCoroutine(Instance.Upload());
			//});

			return false;
		}

		public static void RenderTags()
		{
			var content = EditorManager.inst.GetDialog("Metadata Editor").Dialog.Find("Scroll View/Viewport/Content");
			var parent = content.Find("creator/tags/Scroll View/Viewport/Content");
			var moddedMetadata = MetaData.Current;

			LSHelpers.DeleteChildren(parent);

			for (int i = 0; i < moddedMetadata.LevelSong.tags.Length; i++)
			{
				int index = i;
				var tag = moddedMetadata.LevelSong.tags[i];
				var gameObject = tagPrefab.Duplicate(parent, index.ToString());
				var input = gameObject.transform.Find("Input").GetComponent<InputField>();
				input.onValueChanged.ClearAll();
				input.text = tag;
				input.onValueChanged.AddListener(delegate (string _val)
				{
					moddedMetadata.LevelSong.tags[index] = _val;
				});

				var delete = gameObject.transform.Find("Delete").GetComponent<Button>();
				delete.onClick.ClearAll();
				delete.onClick.AddListener(delegate ()
				{
					var list = moddedMetadata.LevelSong.tags.ToList();
					list.RemoveAt(index);
					moddedMetadata.LevelSong.tags = list.ToArray();
					RenderTags();
				});
			}

			var add = PrefabEditor.inst.CreatePrefab.Duplicate(content.Find("creator/tags/Scroll View/Viewport/Content"), "Add");
			add.transform.Find("Text").GetComponent<Text>().text = "Add Tag";
			var addButton = add.GetComponent<Button>();
			addButton.onClick.ClearAll();
			addButton.onClick.AddListener(delegate ()
			{
				var list = moddedMetadata.LevelSong.tags.ToList();
				list.Add("New Tag");
				moddedMetadata.LevelSong.tags = list.ToArray();
				RenderTags();
			});
		}
	}
}
