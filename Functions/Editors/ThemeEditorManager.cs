using EditorManagement.Functions.Helpers;
using LSFunctions;
using RTFunctions.Functions;
using RTFunctions.Functions.Data;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EditorManagement.Functions.Editors
{
    public class ThemeEditorManager : MonoBehaviour
    {
		public static ThemeEditorManager inst;

		public bool loadingThemes = false;

		public static void Init(ThemeEditor themeEditor) => themeEditor?.gameObject?.AddComponent<ThemeEditorManager>();

		public List<BeatmapTheme> AllThemes => DataManager.inst.AllThemes.Select(x => x as BeatmapTheme).ToList();

		void Awake()
        {
			inst = this;

			var dialog = EditorManager.inst.GetDialog("Event Editor").Dialog;
			var themeParent = dialog.Find("data/left/theme/theme/viewport/content");
			Debug.Log($"{EditorPlugin.className}ThemeParent: {themeParent == null}");
            try
			{
				for (int i = 10; i < 19; i++)
				{
					var col = themeParent.Find("object8").gameObject.Duplicate(themeParent, "object" + (i - 1).ToString(), 8 + i);
					col.transform.Find("text").GetComponent<Text>().text = i.ToString();
				}

				var guiAccent = themeParent.Find("gui").gameObject.Duplicate(themeParent, "guiaccent", 3);
				guiAccent.transform.Find("text").GetComponent<Text>().text = "Tail";
				themeParent.Find("gui/text").GetComponent<Text>().text = "GUI";

				var label = themeParent.GetChild(0).gameObject.Duplicate(themeParent, "effect_label");
				label.transform.Find("text").GetComponent<Text>().text = "Effects" + (!ModCompatibility.mods.ContainsKey("EventsCore") ? " (Requires EventsCore)" : "");

				for (int i = 0; i < 18; i++)
                {
					var col = themeParent.Find("object8").gameObject.Duplicate(themeParent, "effect" + i.ToString());
					col.transform.Find("text").GetComponent<Text>().text = (i + 1).ToString();
				}

				var actions = dialog.Find("data/left/theme/actions");
				var createNew = actions.Find("create-new");
				createNew.AsRT().sizeDelta = new Vector2(100f, 32f);
				createNew.GetChild(0).gameObject.GetComponent<Text>().fontSize = 18;
				var update = actions.Find("update");
				update.AsRT().sizeDelta = new Vector2(70f, 32f);
				update.GetChild(0).gameObject.GetComponent<Text>().fontSize = 18;
				var cancel = actions.Find("cancel");
				cancel.AsRT().sizeDelta = new Vector2(70f, 32f);
				cancel.GetChild(0).gameObject.GetComponent<Text>().fontSize = 18;

                // Save & Use
                {
					var saveUse = createNew.gameObject.Duplicate(actions, "save-use", 1);
					saveUse.transform.GetChild(0).GetComponent<Text>().text = "Save & Use";
				}

                {
					var shuffleID = createNew.gameObject.Duplicate(actions.parent, "shuffle", 3);
					shuffleID.transform.GetChild(0).GetComponent<Text>().text = "Shuffle ID";
				}

				dialog.Find("data/left/theme/theme").AsRT().sizeDelta = new Vector2(366f, 572f);

				var themeContent = dialog.Find("data/right/theme/themes/viewport/content");				
				LSHelpers.DeleteChildren(themeContent);

				CreateThemePopup();

				Debug.Log($"------ {typeof(ThemeEditorManager)} ------\n{typeof(PrefabEditor)} is null: {PrefabEditor.inst == null}\n" +
					$"{typeof(MarkerEditor)} is null: {MarkerEditor.inst == null}\n" +
					$"{typeof(ObjEditor)} is null: {ObjEditor.inst == null}\n" +
					$"{typeof(EventEditor)} is null: {EventEditor.inst == null}\n" +
					$"{typeof(BackgroundEditor)} is null: {BackgroundEditor.inst == null}\n" +
					$"{typeof(CheckpointEditor)} is null: {CheckpointEditor.inst == null}\n");

                // Prefab
                {
					var gameObject = EventEditor.inst.ThemePanel.Duplicate(transform, "theme-panel");

					var storage = gameObject.AddComponent<ThemePanelStorage>();

					var image = gameObject.transform.Find("image");

					image.gameObject.AddComponent<Mask>().showMaskGraphic = false;

					var hlg = image.gameObject.AddComponent<HorizontalLayoutGroup>();

					for (int i = 0; i < ThemePreviewColorCount; i++)
					{
						var col = new GameObject($"Col{i + 1}");
						col.transform.SetParent(image);
						col.transform.localScale = Vector3.one;

						col.AddComponent<RectTransform>();

						if (i == 0)
							storage.color1 = col.AddComponent<Image>();
						if (i == 1)
							storage.color2 = col.AddComponent<Image>();
						if (i == 2)
							storage.color3 = col.AddComponent<Image>();
						if (i == 3)
							storage.color4 = col.AddComponent<Image>();
					}

					storage.button = image.GetComponent<Button>();
					storage.baseImage = gameObject.GetComponent<Image>();
					storage.text = gameObject.transform.Find("text").GetComponent<Text>();
					storage.edit = gameObject.transform.Find("edit").GetComponent<Button>();
					storage.delete = gameObject.transform.Find("delete").GetComponent<Button>();

					EventEditor.inst.ThemePanel = gameObject;
				}
			}
			catch (Exception ex)
            {
				Debug.LogError($"{EditorPlugin.className}{ex}");
            }
		}

		public GameObject themePopupPanelPrefab;

        public string themeSearch;
		public int themePage;
        public Transform themeContent;
        public void CreateThemePopup()
		{
			try
			{
				var objectSearch = EditorManager.inst.GetDialog("Parent Selector").Dialog.gameObject
					.Duplicate(EditorManager.inst.GetDialog("Parent Selector").Dialog.GetParent(), "Theme Popup");
				objectSearch.transform.localPosition = Vector3.zero;

				var objectSearchRT = (RectTransform)objectSearch.transform;
				objectSearchRT.sizeDelta = new Vector2(600f, 450f);
				var objectSearchPanel = (RectTransform)objectSearch.transform.Find("Panel");
				objectSearchPanel.sizeDelta = new Vector2(632f, 32f);
				objectSearchPanel.transform.Find("Text").GetComponent<Text>().text = "Beatmap Themes";
				((RectTransform)objectSearch.transform.Find("search-box")).sizeDelta = new Vector2(600f, 32f);
				objectSearch.transform.Find("mask/content").GetComponent<GridLayoutGroup>().cellSize = new Vector2(600f, 362f);

				var x = objectSearchPanel.transform.Find("x").GetComponent<Button>();
				x.onClick.RemoveAllListeners();
				x.onClick.AddListener(delegate ()
				{
					EditorManager.inst.HideDialog("Theme Popup");
				});

				var searchBar = objectSearch.transform.Find("search-box/search").GetComponent<InputField>();
				searchBar.onValueChanged.ClearAll();
				searchBar.onValueChanged.AddListener(delegate (string _value)
				{
					themeSearch = _value;
					RefreshThemeSearch();
				});
				searchBar.transform.Find("Placeholder").GetComponent<Text>().text = "Search for theme...";

				EditorHelper.AddEditorDropdown("View Themes", "", "View", RTEditor.inst.SearchSprite, delegate ()
				{
					EditorManager.inst.ShowDialog("Theme Popup");
					RefreshThemeSearch();
				});

				themeContent = objectSearch.transform.Find("mask/content");

				EditorHelper.AddEditorPopup("Theme Popup", objectSearch);


				var singleInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move/position/x");

                {
					var page = singleInput.Duplicate(objectSearchPanel, "page");
					page.transform.AsRT().anchoredPosition = new Vector2(340f, 0f);

					Destroy(page.GetComponent<EventInfo>());
					Destroy(page.GetComponent<EventTrigger>());
					Destroy(page.GetComponent<InputField>());

					page.transform.localScale = Vector3.one;
					page.transform.GetChild(0).localScale = Vector3.one;

					var input = page.transform.Find("input");

					var xif = input.gameObject.AddComponent<InputField>();
					xif.onValueChanged.RemoveAllListeners();
					xif.textComponent = input.Find("Text").GetComponent<Text>();
					xif.placeholder = input.Find("Placeholder").GetComponent<Text>();
					xif.characterValidation = InputField.CharacterValidation.Integer;
					xif.text = themePage.ToString();
					xif.onValueChanged.AddListener(delegate (string _val)
					{
						if (int.TryParse(_val, out int p))
                        {
							themePage = p;
							RefreshThemeSearch();
                        }
					});

					TriggerHelper.AddEventTrigger(xif.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDeltaInt(xif, max: int.MaxValue) });

					TriggerHelper.IncreaseDecreaseButtonsInt(xif, max: int.MaxValue, t: page.transform);
				}

				// Prefab
				{
					themePopupPanelPrefab = EditorManager.inst.folderButtonPrefab.Duplicate(transform, $"theme panel");
					themePopupPanelPrefab.GetComponent<Image>().color = new Color(0.1647f, 0.1647f, 0.1647f, 1f);

					var nameText = themePopupPanelPrefab.transform.GetChild(0).GetComponent<Text>();
					nameText.transform.AsRT().anchoredPosition = new Vector2(2f, 160f);
					nameText.transform.AsRT().anchorMax = new Vector2(1f, 0.5f);
					nameText.transform.AsRT().anchorMin = new Vector2(0f, 0.5f);
					nameText.transform.AsRT().sizeDelta = new Vector2(-12f, 32f);
					nameText.text = "theme";

					// Misc Colors
					{
						var objectColorsLabel = themePopupPanelPrefab.transform.GetChild(0).gameObject.Duplicate(themePopupPanelPrefab.transform, "label");
						objectColorsLabel.transform.AsRT().anchoredPosition = new Vector2(2f, 125f);
						objectColorsLabel.transform.AsRT().anchorMax = new Vector2(1f, 0.5f);
						objectColorsLabel.transform.AsRT().anchorMin = new Vector2(0f, 0.5f);
						objectColorsLabel.transform.AsRT().sizeDelta = new Vector2(-12f, 32f);
						objectColorsLabel.GetComponent<Text>().text = "Background / GUI / Tail Colors";

						var objectColors = new GameObject("misc colors");
						objectColors.transform.SetParent(themePopupPanelPrefab.transform);
						objectColors.transform.localScale = Vector3.one;

						var objectColorsRT = objectColors.AddComponent<RectTransform>();
						objectColorsRT.anchoredPosition = new Vector2(13f, 90f);
						objectColorsRT.sizeDelta = new Vector2(600f, 32f);
						var objectColorsGLG = objectColors.AddComponent<GridLayoutGroup>();
						objectColorsGLG.cellSize = new Vector2(28f, 28f);
						objectColorsGLG.spacing = new Vector2(4f, 4f);

						for (int i = 0; i < 3; i++)
						{
							var colorSlot = new GameObject($"{i + 1}");
							colorSlot.transform.SetParent(objectColorsRT);
							colorSlot.transform.localScale = Vector3.one;

							var colorSlotRT = colorSlot.AddComponent<RectTransform>();
							var colorSlotImage = colorSlot.AddComponent<Image>();
						}
					}

					// Player Colors
					{
						var objectColorsLabel = themePopupPanelPrefab.transform.GetChild(0).gameObject.Duplicate(themePopupPanelPrefab.transform, "label");
						objectColorsLabel.transform.AsRT().anchoredPosition = new Vector2(2f, 65f);
						objectColorsLabel.transform.AsRT().anchorMax = new Vector2(1f, 0.5f);
						objectColorsLabel.transform.AsRT().anchorMin = new Vector2(0f, 0.5f);
						objectColorsLabel.transform.AsRT().sizeDelta = new Vector2(-12f, 32f);
						objectColorsLabel.GetComponent<Text>().text = "Player Colors";

						var objectColors = new GameObject("player colors");
						objectColors.transform.SetParent(themePopupPanelPrefab.transform);
						objectColors.transform.localScale = Vector3.one;

						var objectColorsRT = objectColors.AddComponent<RectTransform>();
						objectColorsRT.anchoredPosition = new Vector2(13f, 30f);
						objectColorsRT.sizeDelta = new Vector2(600f, 32f);
						var objectColorsGLG = objectColors.AddComponent<GridLayoutGroup>();
						objectColorsGLG.cellSize = new Vector2(28f, 28f);
						objectColorsGLG.spacing = new Vector2(4f, 4f);

						for (int i = 0; i < 4; i++)
						{
							var colorSlot = new GameObject($"{i + 1}");
							colorSlot.transform.SetParent(objectColorsRT);
							colorSlot.transform.localScale = Vector3.one;

							var colorSlotRT = colorSlot.AddComponent<RectTransform>();
							var colorSlotImage = colorSlot.AddComponent<Image>();
						}
					}

					// Object Colors
					{
						var objectColorsLabel = themePopupPanelPrefab.transform.GetChild(0).gameObject.Duplicate(themePopupPanelPrefab.transform, "label");
						objectColorsLabel.transform.AsRT().anchoredPosition = new Vector2(2f, 5f);
						objectColorsLabel.transform.AsRT().anchorMax = new Vector2(1f, 0.5f);
						objectColorsLabel.transform.AsRT().anchorMin = new Vector2(0f, 0.5f);
						objectColorsLabel.transform.AsRT().sizeDelta = new Vector2(-12f, 32f);
						objectColorsLabel.GetComponent<Text>().text = "Object Colors";

						var objectColors = new GameObject("object colors");
						objectColors.transform.SetParent(themePopupPanelPrefab.transform);
						objectColors.transform.localScale = Vector3.one;

						var objectColorsRT = objectColors.AddComponent<RectTransform>();
						objectColorsRT.anchoredPosition = new Vector2(13f, -30f);
						objectColorsRT.sizeDelta = new Vector2(600f, 32f);
						var objectColorsGLG = objectColors.AddComponent<GridLayoutGroup>();
						objectColorsGLG.cellSize = new Vector2(28f, 28f);
						objectColorsGLG.spacing = new Vector2(4f, 4f);

						for (int i = 0; i < 18; i++)
						{
							var colorSlot = new GameObject($"{i + 1}");
							colorSlot.transform.SetParent(objectColorsRT);
							colorSlot.transform.localScale = Vector3.one;

							var colorSlotRT = colorSlot.AddComponent<RectTransform>();
							var colorSlotImage = colorSlot.AddComponent<Image>();
						}
					}

					// Background Colors
					{
						var objectColorsLabel = themePopupPanelPrefab.transform.GetChild(0).gameObject.Duplicate(themePopupPanelPrefab.transform, "label");
						objectColorsLabel.transform.AsRT().anchoredPosition = new Vector2(2f, -55f);
						objectColorsLabel.transform.AsRT().anchorMax = new Vector2(1f, 0.5f);
						objectColorsLabel.transform.AsRT().anchorMin = new Vector2(0f, 0.5f);
						objectColorsLabel.transform.AsRT().sizeDelta = new Vector2(-12f, 32f);
						objectColorsLabel.GetComponent<Text>().text = "Background Colors";

						var objectColors = new GameObject("background colors");
						objectColors.transform.SetParent(themePopupPanelPrefab.transform);
						objectColors.transform.localScale = Vector3.one;

						var objectColorsRT = objectColors.AddComponent<RectTransform>();
						objectColorsRT.anchoredPosition = new Vector2(13f, -90f);
						objectColorsRT.sizeDelta = new Vector2(600f, 32f);
						var objectColorsGLG = objectColors.AddComponent<GridLayoutGroup>();
						objectColorsGLG.cellSize = new Vector2(28f, 28f);
						objectColorsGLG.spacing = new Vector2(4f, 4f);

						for (int i = 0; i < 9; i++)
						{
							var colorSlot = new GameObject($"{i + 1}");
							colorSlot.transform.SetParent(objectColorsRT);
							colorSlot.transform.localScale = Vector3.one;

							var colorSlotRT = colorSlot.AddComponent<RectTransform>();
							var colorSlotImage = colorSlot.AddComponent<Image>();
						}
					}

					// Effect Colors
					{
						var objectColorsLabel = themePopupPanelPrefab.transform.GetChild(0).gameObject.Duplicate(themePopupPanelPrefab.transform, "label");
						objectColorsLabel.transform.AsRT().anchoredPosition = new Vector2(2f, -115f);
						objectColorsLabel.transform.AsRT().anchorMax = new Vector2(1f, 0.5f);
						objectColorsLabel.transform.AsRT().anchorMin = new Vector2(0f, 0.5f);
						objectColorsLabel.transform.AsRT().sizeDelta = new Vector2(-12f, 32f);
						objectColorsLabel.GetComponent<Text>().text = "Effect Colors";

						var objectColors = new GameObject("effect colors");
						objectColors.transform.SetParent(themePopupPanelPrefab.transform);
						objectColors.transform.localScale = Vector3.one;

						var objectColorsRT = objectColors.AddComponent<RectTransform>();
						objectColorsRT.anchoredPosition = new Vector2(13f, -150f);
						objectColorsRT.sizeDelta = new Vector2(600f, 32f);
						var objectColorsGLG = objectColors.AddComponent<GridLayoutGroup>();
						objectColorsGLG.cellSize = new Vector2(28f, 28f);
						objectColorsGLG.spacing = new Vector2(4f, 4f);

						for (int i = 0; i < 18; i++)
						{
							var colorSlot = new GameObject($"{i + 1}");
							colorSlot.transform.SetParent(objectColorsRT);
							colorSlot.transform.localScale = Vector3.one;

							var colorSlotRT = colorSlot.AddComponent<RectTransform>();
							var colorSlotImage = colorSlot.AddComponent<Image>();
						}
					}

					var buttonsBase = new GameObject("buttons base");
					buttonsBase.transform.SetParent(themePopupPanelPrefab.transform);
					buttonsBase.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

					var buttonsBaseRT = buttonsBase.AddComponent<RectTransform>();
					buttonsBaseRT.anchoredPosition = new Vector2(140f, 160f);
					buttonsBaseRT.sizeDelta = new Vector2(0f, 0f);

					var buttons = new GameObject("buttons");
					buttons.transform.SetParent(buttonsBaseRT);
					buttons.transform.localScale = Vector3.one;

					var buttonsHLG = buttons.AddComponent<HorizontalLayoutGroup>();
					buttonsHLG.spacing = 8f;

					buttons.transform.AsRT().anchoredPosition = Vector2.zero;
					buttons.transform.AsRT().sizeDelta = new Vector2(360f, 32f);

					var tfv = ObjEditor.inst.ObjectView.transform;

					var useTheme = tfv.Find("applyprefab").gameObject.Duplicate(buttons.transform, "use");
					useTheme.SetActive(true);
					var useThemeText = useTheme.transform.GetChild(0).GetComponent<Text>();
					useThemeText.fontSize = 16;
					useThemeText.text = "Use Theme";

					var exportToVG = tfv.Find("applyprefab").gameObject.Duplicate(buttons.transform, "convert");
					exportToVG.SetActive(true);
					var exportToVGText = exportToVG.transform.GetChild(0).GetComponent<Text>();
					exportToVGText.fontSize = 16;
					exportToVGText.text = "Convert to VG Format";

					Destroy(themePopupPanelPrefab.GetComponent<Button>());
				}
            }
			catch (Exception ex)
			{
				Debug.LogError(ex);
			}
		}

		public static int themesPerPage = 10;
		public void RefreshThemeSearch()
		{
			LSHelpers.DeleteChildren(themeContent);

			var layer = themePage + 1;

			int num = 0;
			foreach (var beatmapTheme in AllThemes)
			{
				int max = layer * themesPerPage;

				var name = beatmapTheme.name == null ? "theme" : beatmapTheme.name;
				if ((string.IsNullOrEmpty(themeSearch) || name.ToLower().Contains(themeSearch.ToLower())))
				{
					if (num >= max - themesPerPage && num < max)
					{
						var gameObject = themePopupPanelPrefab.Duplicate(themeContent, name);
						gameObject.transform.localScale = Vector3.one;
						gameObject.transform.GetChild(0).GetComponent<Text>().text = $"{name} [ ID: {beatmapTheme.id} ]";

						gameObject.transform.Find("misc colors").GetChild(0).GetComponent<Image>().color = beatmapTheme.backgroundColor;
						gameObject.transform.Find("misc colors").GetChild(1).GetComponent<Image>().color = beatmapTheme.guiColor;
						gameObject.transform.Find("misc colors").GetChild(2).GetComponent<Image>().color = beatmapTheme.guiAccentColor;

						for (int i = 0; i < beatmapTheme.playerColors.Count; i++)
						{
							if (gameObject.transform.TryFind($"player colors/{i + 1}", out Transform colorSlot))
							{
								colorSlot.GetComponent<Image>().color = beatmapTheme.playerColors[i];
							}
						}

						for (int i = 0; i < beatmapTheme.objectColors.Count; i++)
						{
							if (gameObject.transform.TryFind($"object colors/{i + 1}", out Transform colorSlot))
							{
								colorSlot.GetComponent<Image>().color = beatmapTheme.objectColors[i];
							}
						}

						for (int i = 0; i < beatmapTheme.backgroundColors.Count; i++)
						{
							if (gameObject.transform.TryFind($"background colors/{i + 1}", out Transform colorSlot))
							{
								colorSlot.GetComponent<Image>().color = beatmapTheme.backgroundColors[i];
							}
						}

						for (int i = 0; i < beatmapTheme.effectColors.Count; i++)
						{
							if (gameObject.transform.TryFind($"effect colors/{i + 1}", out Transform colorSlot))
							{
								colorSlot.GetComponent<Image>().color = beatmapTheme.effectColors[i];
							}
						}

						var use = gameObject.transform.Find("buttons base/buttons/use").GetComponent<Button>();
						use.onClick.ClearAll();
						use.onClick.AddListener(delegate ()
						{
							if (RTEventEditor.inst.SelectedKeyframes.Count > 1 && RTEventEditor.inst.SelectedKeyframes.All(x => x.Type == 4))
							{
								foreach (var timelineObject in RTEventEditor.inst.SelectedKeyframes)
								{
									timelineObject.GetData<EventKeyframe>().eventValues[0] = Parser.TryParse(beatmapTheme.id, 0);
								}
							}
							else if (EventEditor.inst.currentEventType == 4)
							{
								DataManager.inst.gameData.eventObjects.allEvents[4][EventEditor.inst.currentEvent].eventValues[0] = Parser.TryParse(beatmapTheme.id, 0);
							}
							else if (DataManager.inst.gameData.eventObjects.allEvents[4].Count > 0)
                            {
								DataManager.inst.gameData.eventObjects.allEvents[4].FindLast(x => x.eventTime < AudioManager.inst.CurrentAudioSource.time).eventValues[0] = Parser.TryParse(beatmapTheme.id, 0);
							}

							EventManager.inst.updateEvents();
						});

						var convert = gameObject.transform.Find("buttons base/buttons/convert").GetComponent<Button>();
						convert.onClick.ClearAll();
						convert.onClick.AddListener(delegate ()
						{
							var exportPath = EditorConfig.Instance.ConvertThemeLSToVGExportPath.Value;

							if (string.IsNullOrEmpty(exportPath))
							{
								if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + "beatmaps/exports"))
									Directory.CreateDirectory(RTFile.ApplicationDirectory + "beatmaps/exports");
								exportPath = RTFile.ApplicationDirectory + "beatmaps/exports/";
							}

							if (!string.IsNullOrEmpty(exportPath) && exportPath[exportPath.Length - 1] != '/')
								exportPath += "/";

							if (!RTFile.DirectoryExists(Path.GetDirectoryName(exportPath)))
							{
								EditorManager.inst.DisplayNotification("Directory does not exist.", 2f, EditorManager.NotificationType.Error);
								return;
							}

							var vgjn = beatmapTheme.ToJSONVG();

							RTFile.WriteToFile($"{exportPath}{beatmapTheme.name.ToLower()}.vgt", vgjn.ToString());

							EditorManager.inst.DisplayNotification($"Converted Theme {beatmapTheme.name.ToLower()}.lst from LS format to VG format and saved to {beatmapTheme.name.ToLower()}.vgt!", 4f, EditorManager.NotificationType.Success);
						});
					}

					num++;
				}
            }
        }

		public static int ThemePreviewColorCount => 4;
        public ThemePanel GenerateThemePanel(Transform parent)
		{
			var gameObject = EventEditor.inst.ThemePanel.Duplicate(parent);

			var storage = gameObject.GetComponent<ThemePanelStorage>();

			var themePanel = new ThemePanel
			{
				GameObject = gameObject,
				UseButton = storage.button,
				EditButton = storage.edit,
				DeleteButton = storage.delete,
				Name = storage.text,
				BaseImage = storage.baseImage,
			};

			themePanel.Colors.Add(storage.color1);
			themePanel.Colors.Add(storage.color2);
			themePanel.Colors.Add(storage.color3);
			themePanel.Colors.Add(storage.color4);

			ThemePanels.Add(themePanel);

			return themePanel;
		}

		public List<ThemePanel> ThemePanels { get; set; } = new List<ThemePanel>();

		bool setupLayout;
		public void RenderThemeContent(Transform p, string search)
		{
			var parent = p.Find("themes/viewport/content");

			if (!setupLayout)
            {
				setupLayout = true;

				p.Find("themes").GetComponent<ScrollRect>().horizontal = false;
				var gridLayoutGroup = parent.GetComponent<GridLayoutGroup>() ?? parent.gameObject.AddComponent<GridLayoutGroup>();

				gridLayoutGroup.cellSize = new Vector2(344f, 30f);
				gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
				gridLayoutGroup.constraintCount = 1;
				gridLayoutGroup.spacing = new Vector2(4f, 4f);
				gridLayoutGroup.startAxis = GridLayoutGroup.Axis.Horizontal;

				parent.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.MinSize;
			}

			RTEditor.inst.themePathField.text = RTEditor.ThemePath;
			StartCoroutine(RenderThemeList(search));
		}

		public IEnumerator RenderThemeList(string search)
		{
			if (!loadingThemes && !EventEditor.inst.eventDrag)
			{
				loadingThemes = true;

				for (int i = 0; i < ThemePanels.Count; i++)
                {
					ThemePanels[i].SetActive(RTHelpers.SearchString(ThemePanels[i].Theme.name, search));
				}

				loadingThemes = false;
			}

			yield break;
		}

		public void DeleteThemeDelegate(DataManager.BeatmapTheme themeTmp)
		{
			EditorManager.inst.ShowDialog("Warning Popup");
			RTEditor.inst.RefreshWarningPopup("Are you sure you want to delete this theme?", delegate ()
			{
				ThemeEditor.inst.DeleteTheme(themeTmp);
				EventEditor.inst.previewTheme.id = null;
				//EventEditor.inst.StartCoroutine(ThemeEditor.inst.LoadThemes());
				Transform child = EventEditor.inst.dialogRight.GetChild(EventEditor.inst.currentEventType);
				EventEditor.inst.RenderThemeContent(child, child.Find("theme-search").GetComponent<InputField>().text);
				EventEditor.inst.RenderThemePreview(child);
				EventEditor.inst.showTheme = false;
				EventEditor.inst.dialogLeft.Find("theme").gameObject.SetActive(false);
				EditorManager.inst.HideDialog("Warning Popup");
			}, delegate ()
			{
				EditorManager.inst.HideDialog("Warning Popup");
			});
		}

		public BeatmapTheme PreviewTheme { get => (BeatmapTheme)EventEditor.inst.previewTheme; set => EventEditor.inst.previewTheme = value; }

		public void RenderThemeEditor(int __0 = -1)
		{
			var Instance = EventEditor.inst;

			Debug.LogFormat("{0}ID: {1}", EditorPlugin.className, __0);

			var previewTheme = __0 != -1 ? BeatmapTheme.DeepCopy((BeatmapTheme)DataManager.inst.GetTheme(__0), true) :  new BeatmapTheme();

			PreviewTheme = previewTheme;
			if (__0 == -1)
				previewTheme.ClearBeatmap();

			var theme = Instance.dialogLeft.Find("theme");
			theme.gameObject.SetActive(true);
			Instance.showTheme = true;
			var themeContent = theme.Find("theme/viewport/content");
			var actions = theme.Find("actions");

			if (!RTHelpers.AprilFools)
				theme.Find("theme").localRotation = Quaternion.Euler(Vector3.zero);

			if (!RTHelpers.AprilFools)
				foreach (var child in themeContent)
				{
					var obj = (Transform)child;
					obj.localRotation = Quaternion.Euler(Vector3.zero);
				}

			theme.Find("theme_title/Text").GetComponent<Text>().text = __0 != -1 ? $"- Theme Editor (ID: {__0}) -" : "- Theme Editor -";

			var shuffle = theme.Find("shuffle").GetComponent<Button>();
			shuffle.onClick.ClearAll();
			shuffle.gameObject.SetActive(__0 != -1 && !(__0 < DataManager.inst.BeatmapThemes.Count));
			if (__0 != -1 && !(__0 < DataManager.inst.BeatmapThemes.Count))
            {
				shuffle.onClick.AddListener(delegate ()
				{
					EditorManager.inst.ShowDialog("Warning Popup");
					RTEditor.inst.RefreshWarningPopup("Are you sure you want to shuffle the theme ID? Any levels that use this theme will need to have their theme keyframes reassigned.", delegate ()
					{
						PreviewTheme.id = LSText.randomNumString(BeatmapTheme.IDLength);
						EditorManager.inst.HideDialog("Warning Popup");
					}, delegate ()
					{
						EditorManager.inst.HideDialog("Warning Popup");
					});
				});
            }

			var name = theme.Find("name").GetComponent<InputField>();
			var cancel = actions.Find("cancel").GetComponent<Button>();
			var createNew = actions.Find("create-new").GetComponent<Button>();
			var update = actions.Find("update").GetComponent<Button>();
			var saveUse = actions.Find("save-use").GetComponent<Button>();

			name.onValueChanged.RemoveAllListeners();
			name.text = PreviewTheme.name;
			name.onValueChanged.AddListener(delegate (string val)
			{
				PreviewTheme.name = val;
			});
			cancel.onClick.RemoveAllListeners();
			cancel.onClick.AddListener(delegate ()
			{
				Instance.showTheme = false;
				theme.gameObject.SetActive(false);
			});
			createNew.onClick.RemoveAllListeners();
			update.onClick.RemoveAllListeners();

			createNew.gameObject.SetActive(true);
			update.gameObject.SetActive(!(__0 < DataManager.inst.BeatmapThemes.Count));

			createNew.onClick.AddListener(delegate ()
			{
				PreviewTheme.id = null;
				ThemeEditor.inst.SaveTheme(BeatmapTheme.DeepCopy(PreviewTheme));
				Instance.StartCoroutine(RTEditor.inst.LoadThemes(true));
				var child = Instance.dialogRight.GetChild(Instance.currentEventType);
				Instance.RenderThemePreview(child);
				Instance.showTheme = false;
				theme.gameObject.SetActive(false);
			});

			update.onClick.AddListener(delegate ()
			{
				RTEditor.inst.canUpdateThemes = false;

				if (ThemePanels.TryFind(x => x.Theme.id == PreviewTheme.id, out ThemePanel themePanel) && RTFile.FileExists(themePanel.Path))
                {
					File.Delete(themePanel.Path);
                }
				else
				{
					var fileList = FileManager.inst.GetFileList(RTEditor.themeListPath, "lst");
					fileList = (from x in fileList
								orderby x.Name.ToLower()
								select x).ToList();

					foreach (var lsfile in fileList)
					{
						if (int.Parse(BeatmapTheme.Parse(JSON.Parse(FileManager.inst.LoadJSONFileRaw(lsfile.FullPath))).id) == __0)
						{
							FileManager.inst.DeleteFileRaw(lsfile.FullPath);
						}
					}
				}

                var beatmapTheme = BeatmapTheme.DeepCopy(PreviewTheme, true);

				ThemeEditor.inst.SaveTheme(beatmapTheme);
				if (DataManager.inst.CustomBeatmapThemes.Has(x => x.id == __0.ToString()))
                {
					DataManager.inst.CustomBeatmapThemes[DataManager.inst.CustomBeatmapThemes.FindIndex(x => x.id == __0.ToString())] = beatmapTheme;
				}

				if (themePanel != null)
				{
					var dialogTmp = EventEditor.inst.dialogRight.GetChild(4);

					themePanel.Theme = beatmapTheme;
					themePanel.OriginalID = beatmapTheme.id;
					themePanel.Name.text = beatmapTheme.name;
                    themePanel.Path = RTFile.ApplicationDirectory + RTEditor.themeListSlash + beatmapTheme.name.ToLower().Replace(" ", "_") + ".lst";

					for (int j = 0; j < themePanel.Colors.Count; j++)
					{
						themePanel.Colors[j].color = beatmapTheme.GetObjColor(j);
					}

					themePanel.UseButton.onClick.ClearAll();
					themePanel.UseButton.onClick.AddListener(delegate ()
					{
						if (RTEventEditor.inst.SelectedKeyframes.Count > 1 && RTEventEditor.inst.SelectedKeyframes.All(x => RTEventEditor.inst.SelectedKeyframes.Min(y => y.Type) == x.Type))
						{
							foreach (var timelineObject in RTEventEditor.inst.SelectedKeyframes)
							{
								timelineObject.GetData<EventKeyframe>().eventValues[0] = Parser.TryParse(beatmapTheme.id, 0);
							}
						}
						else
						{
							DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] = Parser.TryParse(beatmapTheme.id, 0);
						}
						EventManager.inst.updateEvents();
						EventEditor.inst.RenderThemePreview(dialogTmp);
					});

					themePanel.EditButton.onClick.ClearAll();
					themePanel.EditButton.onClick.AddListener(delegate ()
					{
						RenderThemeEditor(Parser.TryParse(beatmapTheme.id, 0));
					});

					themePanel.DeleteButton.onClick.ClearAll();
					themePanel.DeleteButton.interactable = true;
					themePanel.DeleteButton.onClick.AddListener(delegate ()
					{
						DeleteThemeDelegate(beatmapTheme);
					});
					themePanel.Name.text = beatmapTheme.name;
				}

				var child = EventEditor.inst.dialogRight.GetChild(Instance.currentEventType);
				Instance.RenderThemePreview(child);
				Instance.showTheme = false;
				theme.gameObject.SetActive(false);
			});

			saveUse.onClick.ClearAll();
			saveUse.onClick.AddListener(delegate ()
			{
				RTEditor.inst.canUpdateThemes = false;

				BeatmapTheme beatmapTheme;
				if (__0 < DataManager.inst.BeatmapThemes.Count)
				{
					PreviewTheme.id = null;
					beatmapTheme = BeatmapTheme.DeepCopy(PreviewTheme);
				}
				else
				{
					if (ThemePanels.TryFind(x => x.Theme.id == PreviewTheme.id, out ThemePanel themePanel1) && RTFile.FileExists(themePanel1.Path))
					{
						File.Delete(themePanel1.Path);
					}
					else
					{
						var fileList = FileManager.inst.GetFileList(RTEditor.themeListPath, "lst");
						fileList = (from x in fileList
									orderby x.Name.ToLower()
									select x).ToList();

						foreach (var lsfile in fileList)
						{
							if (int.Parse(BeatmapTheme.Parse(JSON.Parse(FileManager.inst.LoadJSONFileRaw(lsfile.FullPath))).id) == __0)
							{
								FileManager.inst.DeleteFileRaw(lsfile.FullPath);
							}
						}
					}

					beatmapTheme = BeatmapTheme.DeepCopy(PreviewTheme, true);
				}

				ThemeEditor.inst.SaveTheme(beatmapTheme);
				if (__0 < DataManager.inst.BeatmapThemes.Count)
					Instance.StartCoroutine(RTEditor.inst.LoadThemes(true));

				if (DataManager.inst.CustomBeatmapThemes.Has(x => x.id == __0.ToString()))
				{
					DataManager.inst.CustomBeatmapThemes[DataManager.inst.CustomBeatmapThemes.FindIndex(x => x.id == __0.ToString())] = beatmapTheme;
				}

				if (!(__0 < DataManager.inst.BeatmapThemes.Count) && ThemePanels.TryFind(x => x.Theme.id == __0.ToString(), out ThemePanel themePanel))
				{
					var dialogTmp = EventEditor.inst.dialogRight.GetChild(4);

					themePanel.Theme = beatmapTheme;
					themePanel.OriginalID = beatmapTheme.id;
					themePanel.Name.text = beatmapTheme.name;
					themePanel.Path = RTFile.ApplicationDirectory + RTEditor.themeListSlash + beatmapTheme.name.ToLower().Replace(" ", "_") + ".lst";

					for (int j = 0; j < themePanel.Colors.Count; j++)
					{
						themePanel.Colors[j].color = beatmapTheme.GetObjColor(j);
					}

					themePanel.UseButton.onClick.ClearAll();
					themePanel.UseButton.onClick.AddListener(delegate ()
					{
						if (RTEventEditor.inst.SelectedKeyframes.Count > 1 && RTEventEditor.inst.SelectedKeyframes.All(x => RTEventEditor.inst.SelectedKeyframes.Min(y => y.Type) == x.Type))
						{
							foreach (var timelineObject in RTEventEditor.inst.SelectedKeyframes)
							{
								timelineObject.GetData<EventKeyframe>().eventValues[0] = Parser.TryParse(beatmapTheme.id, 0);
							}
						}
						else
						{
							DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] = Parser.TryParse(beatmapTheme.id, 0);
						}
						EventManager.inst.updateEvents();
						EventEditor.inst.RenderThemePreview(dialogTmp);
					});

					themePanel.EditButton.onClick.ClearAll();
					themePanel.EditButton.onClick.AddListener(delegate ()
					{
						RenderThemeEditor(Parser.TryParse(beatmapTheme.id, 0));
					});

					themePanel.DeleteButton.onClick.ClearAll();
					themePanel.DeleteButton.interactable = true;
					themePanel.DeleteButton.onClick.AddListener(delegate ()
					{
						DeleteThemeDelegate(beatmapTheme);
					});
					themePanel.Name.text = beatmapTheme.name;
				}

				var child = Instance.dialogRight.GetChild(Instance.currentEventType);
				Instance.RenderThemePreview(child);
				Instance.showTheme = false;
				theme.gameObject.SetActive(false);

				if (int.TryParse(beatmapTheme.id, out int id))
					DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventValues[0] = id;
			});

			var bgHex = themeContent.Find("bg/hex").GetComponent<InputField>();
			var bgPreview = themeContent.Find("bg/preview").GetComponent<Image>();
			var bgPreviewET = themeContent.Find("bg/preview").GetComponent<EventTrigger>();
			var bgDropper = themeContent.Find("bg/preview/dropper").GetComponent<Image>();

			bgHex.onValueChanged.RemoveAllListeners();
			bgHex.text = LSColors.ColorToHex(PreviewTheme.backgroundColor);
			bgPreview.color = PreviewTheme.backgroundColor;
			bgHex.onValueChanged.AddListener(delegate (string val)
			{
				bgPreview.color = val.Length == 6 ? LSColors.HexToColor(val) : LSColors.pink500;
				PreviewTheme.backgroundColor = val.Length == 6 ? LSColors.HexToColor(val) : LSColors.pink500;

				bgDropper.color = RTHelpers.InvertColorHue(RTHelpers.InvertColorValue(PreviewTheme.backgroundColor));
				bgPreviewET.triggers.Clear();
				bgPreviewET.triggers.Add(TriggerHelper.CreatePreviewClickTrigger(bgPreview, bgDropper, bgHex, PreviewTheme.backgroundColor));
			});

			bgDropper.color = RTHelpers.InvertColorHue(RTHelpers.InvertColorValue(PreviewTheme.backgroundColor));
			bgPreviewET.triggers.Clear();
			bgPreviewET.triggers.Add(TriggerHelper.CreatePreviewClickTrigger(bgPreview, bgDropper, bgHex, PreviewTheme.backgroundColor));

			var guiHex = themeContent.Find("gui/hex").GetComponent<InputField>();
			var guiPreview = themeContent.Find("gui/preview").GetComponent<Image>();
			var guiPreviewET = themeContent.Find("gui/preview").GetComponent<EventTrigger>();
			var guiDropper = themeContent.Find("gui/preview/dropper").GetComponent<Image>();

			guiHex.onValueChanged.RemoveAllListeners();
			guiHex.characterLimit = EditorConfig.Instance.SavingSavesThemeOpacity.Value ? 8 : 6;
			guiHex.characterValidation = InputField.CharacterValidation.None;
			guiHex.contentType = InputField.ContentType.Standard;
			guiHex.text = EditorConfig.Instance.SavingSavesThemeOpacity.Value ? RTHelpers.ColorToHex(PreviewTheme.guiColor) : LSColors.ColorToHex(PreviewTheme.guiColor);
			guiPreview.color = PreviewTheme.guiColor;
			guiHex.onValueChanged.AddListener(delegate (string val)
			{
				guiPreview.color = val.Length == 8 ? LSColors.HexToColorAlpha(val) : val.Length == 6 ? LSColors.HexToColor(val) : LSColors.pink500;
				PreviewTheme.guiColor = val.Length == 8 ? LSColors.HexToColorAlpha(val) : val.Length == 6 ? LSColors.HexToColor(val) : LSColors.pink500;

				guiDropper.color = RTHelpers.InvertColorHue(RTHelpers.InvertColorValue(PreviewTheme.guiColor));
				guiPreviewET.triggers.Clear();
				guiPreviewET.triggers.Add(TriggerHelper.CreatePreviewClickTrigger(guiPreview, guiDropper, guiHex, PreviewTheme.guiColor));
			});

			guiDropper.color = RTHelpers.InvertColorHue(RTHelpers.InvertColorValue(PreviewTheme.guiColor));
			guiPreviewET.triggers.Clear();
			guiPreviewET.triggers.Add(TriggerHelper.CreatePreviewClickTrigger(guiPreview, guiDropper, guiHex, PreviewTheme.guiColor));
			
			var guiaccentHex = themeContent.Find("guiaccent/hex").GetComponent<InputField>();
			var guiaccentPreview = themeContent.Find("guiaccent/preview").GetComponent<Image>();
			var guiaccentPreviewET = themeContent.Find("guiaccent/preview").GetComponent<EventTrigger>();
			var guiaccentDropper = themeContent.Find("guiaccent/preview/dropper").GetComponent<Image>();

			guiaccentHex.onValueChanged.RemoveAllListeners();
			guiaccentHex.characterLimit = EditorConfig.Instance.SavingSavesThemeOpacity.Value ? 8 : 6;
			guiaccentHex.characterValidation = InputField.CharacterValidation.None;
			guiaccentHex.contentType = InputField.ContentType.Standard;
			guiaccentHex.text = EditorConfig.Instance.SavingSavesThemeOpacity.Value ? RTHelpers.ColorToHex(PreviewTheme.guiAccentColor) : LSColors.ColorToHex(PreviewTheme.guiAccentColor);
			guiaccentPreview.color = PreviewTheme.guiAccentColor;
			guiaccentHex.onValueChanged.AddListener(delegate (string val)
			{
				guiaccentPreview.color = val.Length == 8 ? LSColors.HexToColorAlpha(val) : val.Length == 6 ? LSColors.HexToColor(val) : LSColors.pink500;
				PreviewTheme.guiAccentColor = val.Length == 8 ? LSColors.HexToColorAlpha(val) : val.Length == 6 ? LSColors.HexToColor(val) : LSColors.pink500;

				guiaccentDropper.color = RTHelpers.InvertColorHue(RTHelpers.InvertColorValue(PreviewTheme.guiAccentColor));
				guiaccentPreviewET.triggers.Clear();
				guiaccentPreviewET.triggers.Add(TriggerHelper.CreatePreviewClickTrigger(guiaccentPreview, guiaccentDropper, guiaccentHex, PreviewTheme.guiAccentColor));
			});

			guiaccentDropper.color = RTHelpers.InvertColorHue(RTHelpers.InvertColorValue(PreviewTheme.guiAccentColor));
			guiaccentPreviewET.triggers.Clear();
			guiaccentPreviewET.triggers.Add(TriggerHelper.CreatePreviewClickTrigger(guiaccentPreview, guiaccentDropper, guiaccentHex, PreviewTheme.guiAccentColor));

			RenderColorList(themeContent, "player", 4, PreviewTheme.playerColors, EditorConfig.Instance.SavingSavesThemeOpacity.Value);

			RenderColorList(themeContent, "object", 18, PreviewTheme.objectColors, EditorConfig.Instance.SavingSavesThemeOpacity.Value);

			RenderColorList(themeContent, "background", 9, PreviewTheme.backgroundColors, false);

			themeContent.Find("effect_label").gameObject.SetActive(RTEditor.ShowModdedUI);
			RenderColorList(themeContent, "effect", 18, PreviewTheme.effectColors);
		}

		public void RenderColorList(Transform themeContent, string name, int count, List<Color> colors, bool allowAlpha = true)
		{
			for (int i = 0; i < count; i++)
			{
				if (!themeContent.Find($"{name}{i}"))
					return;

				var p = themeContent.Find($"{name}{i}");

				// We have to rotate the element due to the rotation being off in unmodded.
				if (!RTHelpers.AprilFools)
					p.transform.localRotation = Quaternion.Euler(Vector3.zero);

				bool active = RTEditor.ShowModdedUI || !name.Contains("effect") && i < 9;
				p.gameObject.SetActive(active);

				if (active)
				{
					var hex = p.Find("hex").GetComponent<InputField>();
					var preview = p.Find("preview").GetComponent<Image>();
					var previewET = p.Find("preview").GetComponent<EventTrigger>();
					var dropper = p.Find("preview").GetChild(0).GetComponent<Image>();

					int indexTmp = i;
					hex.onValueChanged.RemoveAllListeners();
					hex.characterLimit = allowAlpha ? 8 : 6;
					hex.characterValidation = InputField.CharacterValidation.None;
					hex.contentType = InputField.ContentType.Standard;
					hex.text = allowAlpha ? RTHelpers.ColorToHex(colors[indexTmp]) : LSColors.ColorToHex(colors[indexTmp]);
					preview.color = colors[indexTmp];
					hex.onValueChanged.AddListener(delegate (string val)
					{
						var color = val.Length == 8 && allowAlpha ? LSColors.HexToColorAlpha(val) : val.Length == 6 ? LSColors.HexToColor(val) : LSColors.pink500;
						preview.color = color;
						colors[indexTmp] = color;

						SetDropper(dropper, preview, hex, previewET, colors[indexTmp]);
					});

					SetDropper(dropper, preview, hex, previewET, colors[indexTmp]);
				}
			}
		}

		public void SetDropper(Image dropper, Image preview, InputField hex, EventTrigger previewET, Color color)
		{
			dropper.color = RTHelpers.InvertColorHue(RTHelpers.InvertColorValue(color));
			previewET.triggers.Clear();
			previewET.triggers.Add(TriggerHelper.CreatePreviewClickTrigger(preview, dropper, hex, color));
		}

		public void DeleteTheme(BeatmapTheme theme)
		{
			RTEditor.inst.canUpdateThemes = false;

			File.Delete(theme.filePath);

			Predicate<ThemePanel> predicate = x => x.Theme.id == theme.id;
			if (ThemePanels.TryFind(predicate, out ThemePanel themePanel))
            {
				Destroy(themePanel.GameObject);
				ThemePanels.RemoveAt(ThemePanels.FindIndex(predicate));
			}

			StartCoroutine(SetUpdate(1f, true));
		}

		public void SaveTheme(BeatmapTheme theme)
		{
			Debug.Log($"{EventEditor.inst.className}Saving {theme.id} ({theme.name}) to File System!");

			RTEditor.inst.canUpdateThemes = false;

			if (string.IsNullOrEmpty(theme.id))
				theme.id = LSText.randomNumString(BeatmapTheme.IDLength);

			var config = EditorConfig.Instance;

			GameData.SaveOpacityToThemes = config.SavingSavesThemeOpacity.Value;
			
			var str = config.ThemeSavesIndents.Value ? theme.ToJSON().ToString(3) : theme.ToJSON().ToString();

			var path = $"{RTFile.ApplicationDirectory}{RTEditor.themeListSlash}{theme.name.ToLower().Replace(" ", "_")}.lst";

			theme.filePath = path;

			RTFile.WriteToFile(path, str);

			EditorManager.inst.DisplayNotification($"Saved theme [{theme.name}]!", 2f, EditorManager.NotificationType.Success);

			StartCoroutine(SetUpdate(1f, true));
		}

		public IEnumerator SetUpdate(float delay, bool update)
        {
			yield return new WaitForSeconds(delay);
			RTEditor.inst.canUpdateThemes = update;
        }

		public static int ColorsToShow => 4;
		public static IEnumerator GetThemeSprite(DataManager.BeatmapTheme themeTmp, Action<Sprite> _sprite)
		{
			var texture2D = new Texture2D(16, 16, TextureFormat.ARGB32, false);
			int num2 = 0;
			for (int i = 0; i < 16; i++)
			{
				if (i % ColorsToShow == 0)
				{
					num2++;
				}
				for (int j = 0; j < 16; j++)
				{
					texture2D.SetPixel(i, j, themeTmp.GetObjColor(num2 - 1));
				}
			}
			texture2D.filterMode = FilterMode.Point;
			texture2D.Apply();
			_sprite(Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f));
			yield break;
		}
	}
}
