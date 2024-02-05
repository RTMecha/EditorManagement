using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using SimpleJSON;
using LSFunctions;

using RTFunctions.Functions;
using RTFunctions.Functions.Data;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;

using EditorManagement.Functions.Components;
using EditorManagement.Functions.Helpers;

namespace EditorManagement.Functions.Editors
{
    public class ThemeEditorManager : MonoBehaviour
    {
		public static ThemeEditorManager inst;

		public bool loadingThemes = false;

		public static void Init(ThemeEditor themeEditor) => themeEditor?.gameObject?.AddComponent<ThemeEditorManager>();

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

				var label = themeParent.GetChild(0).gameObject.Duplicate(themeParent, "label");
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

				//StartCoroutine(Wait());
			}
			catch (Exception ex)
            {
				Debug.LogError($"{EditorPlugin.className}{ex}");
            }
		}

		public static int ThemePreviewColorCount => 4;
		public static int ThemePoolCount => 400;
        //public IEnumerator Wait()
        //{
        //    while (!EventEditor.inst || !EventEditor.inst.dialogRight)
        //        yield return null;

        //    // Theme Panel adjustments for optimization
        //    {
        //        var dialogTmp = EventEditor.inst.dialogRight.GetChild(4);
        //        var parent = dialogTmp.Find("themes/viewport/content");
        //        LSHelpers.DeleteChildren(parent);

        //        var cr = Instantiate(EventEditor.inst.ThemeAdd);
        //        cr.name = "Create New";
        //        var tf = cr.transform;
        //        tf.SetParent(parent);
        //        cr.SetActive(true);
        //        tf.localScale = Vector2.one;
        //        cr.GetComponent<Button>().onClick.AddListener(delegate ()
        //        {
        //            RenderThemeEditor();
        //        });

        //        for (int i = 0; i < ThemePoolCount; i++)
        //        {
        //            GenerateThemePanel(parent);
        //        }
        //    }
        //}

        public ThemePanel GenerateThemePanel(Transform parent)
		{
			var gameObject = EventEditor.inst.ThemePanel.Duplicate(parent);

			var image = gameObject.transform.Find("image");

			image.GetComponent<Image>().enabled = false;

			var themePanel = new ThemePanel
			{
				GameObject = gameObject,
				UseButton = image.GetComponent<Button>(),
				EditButton = gameObject.transform.Find("edit").GetComponent<Button>(),
				DeleteButton = gameObject.transform.Find("delete").GetComponent<Button>(),
				Name = gameObject.transform.Find("text").GetComponent<Text>()
			};

			var hlg = image.gameObject.AddComponent<HorizontalLayoutGroup>();

			for (int i = 0; i < ThemePreviewColorCount; i++)
			{
				var col = new GameObject($"Col{i + 1}");
				col.transform.SetParent(image);
				col.transform.localScale = Vector3.one;

				col.AddComponent<RectTransform>();
				themePanel.Colors.Add(col.AddComponent<Image>());
			}

			//themePanel.SetActive(false);

			ThemePanels.Add(themePanel);

			return themePanel;
		}

		public List<ThemePanel> ThemePanels { get; set; } = new List<ThemePanel>();

		public void RenderThemeContent(Transform p, string search)
		{
			var parent = p.Find("themes/viewport/content");

			p.Find("themes").GetComponent<ScrollRect>().horizontal = false;

			if (!parent.GetComponent<GridLayoutGroup>())
			{
				parent.gameObject.AddComponent<GridLayoutGroup>();
			}

			var prefabLay = parent.GetComponent<GridLayoutGroup>();
			prefabLay.cellSize = new Vector2(344f, 30f);
			prefabLay.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
			prefabLay.constraintCount = 1;
			prefabLay.spacing = new Vector2(4f, 4f);
			prefabLay.startAxis = GridLayoutGroup.Axis.Horizontal;

			parent.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.MinSize;

			if (p.TryFind("theme/themepathers/themes path", out Transform themePath) && themePath.gameObject.TryGetComponent(out InputField themePathIF))
				themePathIF.text = RTEditor.ThemePath;

			RTEditor.inst.StartCoroutine(RenderThemeList(search));
		}

		public IEnumerator RenderThemeList(string search)
		{
			if (!loadingThemes && !EventEditor.inst.eventDrag)
			{
				loadingThemes = true;

				ThemePanels.ForEach(x => x.SetActive(RTHelpers.SearchString(x.Theme.name, search)));

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
					System.IO.File.Delete(themePanel.Path);
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
						System.IO.File.Delete(themePanel1.Path);
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
			guiHex.characterLimit = 8;
			guiHex.characterValidation = InputField.CharacterValidation.None;
			guiHex.contentType = InputField.ContentType.Standard;
			guiHex.text = RTHelpers.ColorToHex(PreviewTheme.guiColor);
			guiPreview.color = PreviewTheme.guiColor;
			guiHex.onValueChanged.AddListener(delegate (string val)
			{
				guiPreview.color = val.Length == 8 ? LSColors.HexToColorAlpha(val) : LSColors.pink500;
				PreviewTheme.guiColor = val.Length == 8 ? LSColors.HexToColorAlpha(val) : LSColors.pink500;

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
			guiaccentHex.characterLimit = 8;
			guiaccentHex.characterValidation = InputField.CharacterValidation.None;
			guiaccentHex.contentType = InputField.ContentType.Standard;
			guiaccentHex.text = RTHelpers.ColorToHex(PreviewTheme.guiAccentColor);
			guiaccentPreview.color = PreviewTheme.guiAccentColor;
			guiaccentHex.onValueChanged.AddListener(delegate (string val)
			{
				guiaccentPreview.color = val.Length == 8 ? LSColors.HexToColorAlpha(val) : LSColors.pink500;
				PreviewTheme.guiAccentColor = val.Length == 8 ? LSColors.HexToColorAlpha(val) : LSColors.pink500;

				guiaccentDropper.color = RTHelpers.InvertColorHue(RTHelpers.InvertColorValue(PreviewTheme.guiAccentColor));
				guiaccentPreviewET.triggers.Clear();
				guiaccentPreviewET.triggers.Add(TriggerHelper.CreatePreviewClickTrigger(guiaccentPreview, guiaccentDropper, guiaccentHex, PreviewTheme.guiAccentColor));
			});

			guiaccentDropper.color = RTHelpers.InvertColorHue(RTHelpers.InvertColorValue(PreviewTheme.guiAccentColor));
			guiaccentPreviewET.triggers.Clear();
			guiaccentPreviewET.triggers.Add(TriggerHelper.CreatePreviewClickTrigger(guiaccentPreview, guiaccentDropper, guiaccentHex, PreviewTheme.guiAccentColor));

			RenderColorList(themeContent, "player", 4, PreviewTheme.playerColors);

			RenderColorList(themeContent, "object", 18, PreviewTheme.objectColors);

			RenderColorList(themeContent, "background", 9, PreviewTheme.backgroundColors, false);

			RenderColorList(themeContent, "effect", 18, PreviewTheme.effectColors);
		}

		public void RenderColorList(Transform themeContent, string name, int count, List<Color> colors, bool allowAlpha = true)
		{
			for (int i = 0; i < count; i++)
			{
				if (!themeContent.Find($"{name}{i}"))
					return;

				themeContent.Find($"{name}{i}").transform.localRotation = Quaternion.Euler(Vector3.zero);
				var hex = themeContent.Find($"{name}{i}/hex").GetComponent<InputField>();
				var preview = themeContent.Find($"{name}{i}/preview").GetComponent<Image>();
				var previewET = themeContent.Find($"{name}{i}/preview").GetComponent<EventTrigger>();
				var dropper = themeContent.Find($"{name}{i}/preview").GetChild(0).GetComponent<Image>();
				int indexTmp = i;
				hex.onValueChanged.RemoveAllListeners();
				hex.characterLimit = 8;
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

		public void SetDropper(Image dropper, Image preview, InputField hex, EventTrigger previewET, Color color)
		{
			dropper.color = RTHelpers.InvertColorHue(RTHelpers.InvertColorValue(color));
			previewET.triggers.Clear();
			previewET.triggers.Add(TriggerHelper.CreatePreviewClickTrigger(preview, dropper, hex, color));
		}

		public void DeleteTheme(DataManager.BeatmapTheme _theme)
		{
			RTEditor.inst.canUpdateThemes = false;

			FileManager.inst.DeleteFile(RTEditor.themeListPath, _theme.name.ToLower().Replace(" ", "_") + ".lst");

			Predicate<ThemePanel> predicate = x => x.Theme.id == _theme.id;
			if (ThemePanels.TryFind(predicate, out ThemePanel themePanel))
            {
				Destroy(themePanel.GameObject);
				ThemePanels.RemoveAt(ThemePanels.FindIndex(predicate));
			}

			StartCoroutine(SetUpdate(1f, true));
		}

		public void SaveTheme(BeatmapTheme _theme)
		{
			Debug.Log($"{EventEditor.inst.className}Saving {_theme.id} ({_theme.name}) to File System!");

			RTEditor.inst.canUpdateThemes = false;

			if (string.IsNullOrEmpty(_theme.id))
				_theme.id = LSText.randomNumString(BeatmapTheme.IDLength);

			GameData.SaveOpacityToThemes = RTEditor.GetEditorProperty("Saving Saves Beatmap Opacity").GetConfigEntry<bool>().Value;
			
			var str = RTEditor.GetEditorProperty("Theme Saves Indents").GetConfigEntry<bool>().Value ? _theme.ToJSON().ToString(3) : _theme.ToJSON().ToString();
			FileManager.inst.SaveJSONFile(RTEditor.themeListPath, _theme.name.ToLower().Replace(" ", "_") + ".lst", str);
			EditorManager.inst.DisplayNotification($"Saved theme [{_theme.name}]!", 2f, EditorManager.NotificationType.Success);

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
