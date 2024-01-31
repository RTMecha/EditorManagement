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
					col.transform.Find("text").GetComponent<Text>().text = i.ToString();
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
					saveUse.transform.GetChild(0).gameObject.GetComponent<Text>().text = "Save & Use";
				}

				StartCoroutine(Wait());
			}
            catch (Exception ex)
            {
				Debug.LogError($"{EditorPlugin.className}{ex}");
            }
		}

		public static int ThemePreviewColorCount => 4;
		public static int ThemePoolCount => 400;
		public IEnumerator Wait()
        {
			while (!EventEditor.inst || !EventEditor.inst.dialogRight)
				yield return null;

			// Theme Panel adjustments for optimization
			{
				var dialogTmp = EventEditor.inst.dialogRight.GetChild(4);
				var parent = dialogTmp.Find("themes/viewport/content");
				LSHelpers.DeleteChildren(parent);

				var cr = Instantiate(EventEditor.inst.ThemeAdd);
				cr.name = "Create New";
				var tf = cr.transform;
				tf.SetParent(parent);
				cr.SetActive(true);
				tf.localScale = Vector2.one;
				cr.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					RenderThemeEditor();
				});

				for (int i = 0; i < ThemePoolCount; i++)
                {
					GenerateThemePanel(parent);
				}
			}
		}

		public ThemePanel GenerateThemePanel(Transform parent)
		{
			var gameObject = EventEditor.inst.ThemePanel.Duplicate(parent);

			var image = gameObject.transform.Find("image");

			image.GetComponent<Image>().enabled = false;

			var themePanel = gameObject.AddComponent<ThemePanel>();
			themePanel.UseButton = image.GetComponent<Button>();
			themePanel.EditButton = gameObject.transform.Find("edit").GetComponent<Button>();
			themePanel.DeleteButton = gameObject.transform.Find("delete").GetComponent<Button>();
			themePanel.Name = gameObject.transform.Find("text").GetComponent<Text>();

			var hlg = image.gameObject.AddComponent<HorizontalLayoutGroup>();

			for (int i = 0; i < ThemePreviewColorCount; i++)
			{
				var col = new GameObject($"Col{i + 1}");
				col.transform.SetParent(image);
				col.transform.localScale = Vector3.one;

				col.AddComponent<RectTransform>();
				themePanel.Colors.Add(col.AddComponent<Image>());
			}

			themePanel.SetActive(false);

			ThemePanels.Add(themePanel);

			return themePanel;
		}

		public List<ThemePanel> ThemePanels { get; set; } = new List<ThemePanel>();

		public void RenderThemeContent(Transform __0, string __1)
		{
			Debug.LogFormat("{0}RenderThemeContent Prefix Patch", EditorPlugin.className);
			var parent = __0.Find("themes/viewport/content");

			__0.Find("themes").GetComponent<ScrollRect>().horizontal = false;

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

			if (__0.TryFind("theme/themepathers/themes path", out Transform themePath) && themePath.gameObject.TryGetComponent(out InputField themePathIF))
				themePathIF.text = RTEditor.ThemePath;

			RTEditor.inst.StartCoroutine(RenderThemeList(__0, __1));
		}

		public IEnumerator RenderThemeList(Transform content, string search)
		{
			var eventEditor = EventEditor.inst;
			var themeEditor = ThemeEditor.inst;

			if (!loadingThemes && !eventEditor.eventDrag)
			{
				loadingThemes = true;
				Debug.LogFormat("{0}Rendering theme list...", EditorPlugin.className);
				//var sw = new System.Diagnostics.Stopwatch();
				//sw.Start();

				var parent = content.Find("themes/viewport/content");

				yield return ThemePanels.Where(x => x.gameObject.activeSelf).ToList().ForEachReturn(x =>x.SetActive(false));

                //foreach (var themePanel in ThemePanels.Where(x => x.gameObject.activeSelf))
                //{
                //    themePanel.SetActive(false);
                //}

                int num = 0;
				foreach (var beatmapTheme in DataManager.inst.AllThemes.Where(x => string.IsNullOrEmpty(search) || x.name.ToLower().Contains(search.ToLower())))
				{
					int index = num;
					string tmpThemeID = beatmapTheme.id;

					if (num > parent.childCount - 2)
                    {
						GenerateThemePanel(parent);
                    }

					var themePanel = ThemePanels[Mathf.Clamp(num + 1, 0, ThemePanels.Count - 1)];
					bool active = true;
					themePanel.SetActive(active);
                    if (active)
                    {
                        for (int j = 0; j < themePanel.Colors.Count; j++)
                        {
                            themePanel.Colors[j].color = beatmapTheme.GetObjColor(j);
                        }

                        themePanel.UseButton.onClick.ClearAll();
                        themePanel.UseButton.onClick.AddListener(delegate ()
                        {
                            DataManager.inst.gameData.eventObjects.allEvents[eventEditor.currentEventType][eventEditor.currentEvent].eventValues[0] = Parser.TryParse(tmpThemeID, 0);
                            EventManager.inst.updateEvents();
                            eventEditor.RenderThemePreview(content);
                        });

                        themePanel.EditButton.onClick.ClearAll();
                        themePanel.EditButton.onClick.AddListener(delegate ()
                        {
                            RenderThemeEditor(Parser.TryParse(tmpThemeID, 0));
                        });

                        themePanel.DeleteButton.onClick.ClearAll();
                        themePanel.DeleteButton.interactable = Parser.TryParse(tmpThemeID, 0) >= DataManager.inst.BeatmapThemes.Count;
                        themePanel.DeleteButton.onClick.AddListener(delegate ()
                        {
                            DeleteThemeDelegate(beatmapTheme);
                        });
                        themePanel.Name.text = beatmapTheme.name;
                    }

                    num++;
				}

				//Debug.LogFormat("{0}Finished rendering theme list and took {1} to complete!", EditorPlugin.className, sw.Elapsed);
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
				EventEditor.inst.StartCoroutine(ThemeEditor.inst.LoadThemes());
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

			foreach (var child in themeContent)
			{
				var obj = (Transform)child;
				obj.localRotation = Quaternion.Euler(Vector3.zero);
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
				Instance.StartCoroutine(ThemeEditor.inst.LoadThemes());
				var child = Instance.dialogRight.GetChild(Instance.currentEventType);
				Instance.RenderThemeContent(child, child.Find("theme-search").GetComponent<InputField>().text);
				Instance.RenderThemePreview(child);
				Instance.showTheme = false;
				theme.gameObject.SetActive(false);
			});
			update.onClick.AddListener(delegate ()
			{
				var fileList = FileManager.inst.GetFileList("beatmaps/themes", "lst");
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
				ThemeEditor.inst.SaveTheme(BeatmapTheme.DeepCopy(PreviewTheme, true));
				Instance.StartCoroutine(ThemeEditor.inst.LoadThemes());
				var child = EventEditor.inst.dialogRight.GetChild(Instance.currentEventType);
				Instance.RenderThemeContent(child, child.Find("theme-search").GetComponent<InputField>().text);
				Instance.RenderThemePreview(child);
				Instance.showTheme = false;
				theme.gameObject.SetActive(false);
			});

			saveUse.onClick.ClearAll();
			saveUse.onClick.AddListener(delegate ()
			{
				BeatmapTheme beatmapTheme;
				if (__0 < DataManager.inst.BeatmapThemes.Count)
				{
					PreviewTheme.id = null;
					beatmapTheme = BeatmapTheme.DeepCopy(PreviewTheme);
				}
				else
				{
					var fileList = FileManager.inst.GetFileList("beatmaps/themes", "lst");
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
					beatmapTheme = BeatmapTheme.DeepCopy(PreviewTheme, true);
				}

				ThemeEditor.inst.SaveTheme(beatmapTheme);
				Instance.StartCoroutine(ThemeEditor.inst.LoadThemes());
				var child = Instance.dialogRight.GetChild(Instance.currentEventType);
				Instance.RenderThemeContent(child, child.Find("theme-search").GetComponent<InputField>().text);
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
				hex.text = RTHelpers.ColorToHex(colors[indexTmp]);
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
			FileManager.inst.DeleteFile(RTEditor.themeListPath, _theme.name.ToLower().Replace(" ", "_") + ".lst");
			StartCoroutine(RTEditor.inst.LoadThemes(true));
		}

		public void SaveTheme(BeatmapTheme _theme)
		{
			Debug.Log($"{EventEditor.inst.className}Saving {_theme.id} ({_theme.name}) to File System!");

			if (string.IsNullOrEmpty(_theme.id))
			{
				_theme.id = LSText.randomNumString(6);
			}

			GameData.SaveOpacityToThemes = RTEditor.GetEditorProperty("Saving Saves Beatmap Opacity").GetConfigEntry<bool>().Value;

			//jsonnode["id"] = _theme.id.ToString();
			//jsonnode["name"] = _theme.name;
			//jsonnode["gui"] = saveOpacity ? RTHelpers.ColorToHex(_theme.guiColor) : LSColors.ColorToHex(_theme.guiColor);
			//jsonnode["gui_ex"] = saveOpacity ? RTHelpers.ColorToHex(_theme.guiColor) : LSColors.ColorToHex(_theme.guiColor);
			//jsonnode["bg"] = LSColors.ColorToHex(_theme.backgroundColor);

			//for (int i = 0; i < _theme.playerColors.Count; i++)
			//	jsonnode["players"][i] = saveOpacity ? RTHelpers.ColorToHex(_theme.playerColors[i]) : LSColors.ColorToHex(_theme.playerColors[i]);
			//for (int i = 0; i < _theme.objectColors.Count; i++)
			//	jsonnode["objs"][i] = saveOpacity ? RTHelpers.ColorToHex(_theme.objectColors[i]) : LSColors.ColorToHex(_theme.objectColors[i]);
			//for (int i = 0; i < _theme.backgroundColors.Count; i++)
			//	jsonnode["bgs"][i] = LSColors.ColorToHex(_theme.backgroundColors[i]);
			//for (int i = 0; i < _theme.effectColors.Count; i++)
			//	jsonnode["fx"][i] = saveOpacity ? RTHelpers.ColorToHex(_theme.effectColors[i]) : LSColors.ColorToHex(_theme.effectColors[i]);

			FileManager.inst.SaveJSONFile(RTEditor.themeListPath, _theme.name.ToLower().Replace(" ", "_") + ".lst", _theme.ToJSON().ToString());
			EditorManager.inst.DisplayNotification($"Saved theme [{_theme.name}]!", 2f, EditorManager.NotificationType.Success);
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
