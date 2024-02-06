using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

using SimpleJSON;
using LSFunctions;
using TMPro;

using RTFunctions.Functions;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;

using EditorManagement.Functions.Components;
using EditorManagement.Functions.Editors;
using EditorManagement.Functions.Helpers;

namespace EditorManagement.Functions
{
    public class ProjectPlannerManager : MonoBehaviour
    {
        public static ProjectPlannerManager inst;

        #region Variables

        public Transform plannerBase;
        public Transform planner;
        public Transform topBarBase;
        public Transform contentBase;
        public Transform contentScroll;
        public Transform content;
        public GridLayoutGroup contentLayout;

        public Transform assetsParent;

        public List<Toggle> tabs = new List<Toggle>();

        public int CurrentTab { get; set; }
        public string SearchTerm { get; set; }

        public string[] tabNames = new string[]
        {
            "Documents",
            "TO DO",
            "Characters",
            "Timelines",
            "Schedules",
            "Notes",
        };

        public Vector2[] tabCellSizes = new Vector2[]
        {
            new Vector2(232f, 400f),
            new Vector2(1280f, 64f),
            new Vector2(296f, 270f),
            new Vector2(339f, 400f),
            new Vector2(339f, 400f),
            new Vector2(339f, 400f),
        };

        public int[] tabConstraintCounts = new int[]
        {
            5,
            1,
            4,
            5,
            5,
            5,
        };

        public GameObject tabPrefab;

        public GameObject closePrefab;

        public GameObject baseCardPrefab;

        public List<GameObject> prefabs = new List<GameObject>();

        public Sprite gradientSprite;

        public List<PlannerItem> planners = new List<PlannerItem>();

        public string PlannersPath { get; set; } = "planners";

        #endregion

        void Awake()
        {
            inst = this;
            plannerBase = GameObject.Find("Editor Systems/Editor GUI/sizer").transform.GetChild(1);
            plannerBase.gameObject.SetActive(true);

            planner = plannerBase.GetChild(0);
            topBarBase = planner.GetChild(0);

            var assets = new GameObject("Planner Assets");
            assetsParent = assets.transform;
            assetsParent.transform.SetParent(transform);

            tabPrefab = topBarBase.GetChild(0).gameObject;
            tabPrefab.transform.SetParent(assetsParent);
            tabPrefab.transform.AsRT().sizeDelta = new Vector2(200f, 54f);

            LSHelpers.DeleteChildren(topBarBase);

            Destroy(topBarBase.GetComponent<ToggleGroup>());
            tabPrefab.GetComponent<Toggle>().group = null;

            for (int i = 0; i < tabNames.Length; i++)
                GenerateTab(tabNames[i]);

            closePrefab = GameObject.Find("Editor Systems/Editor GUI/sizer/main/Popups/Open File Popup/Panel/x").Duplicate(assetsParent, "x");

            Spacer("topbar spacer", topBarBase, new Vector2(435f, 32f));

            var close = closePrefab.Duplicate(topBarBase, "close");
            close.transform.localScale = Vector3.one;

            close.transform.AsRT().sizeDelta = new Vector2(48f, 48f);

            var closeButton = close.GetComponent<Button>();
            closeButton.onClick.ClearAll();
            closeButton.onClick.AddListener(delegate ()
            {
                Close();
            });

            EditorHelper.AddEditorDropdown("Open Project Planner", "", "Edit", SpriteManager.LoadSprite(RTFile.ApplicationDirectory + RTFunctions.FunctionsPlugin.BepInExAssetsPath + "editor_gui_planner.png"), delegate ()
            {
                Open();
            });

            contentBase = planner.Find("content/recent");
            Destroy(contentBase.GetComponent<VerticalLayoutGroup>());
            contentBase.gameObject.name = "content base";

            contentScroll = contentBase.Find("recent scroll");
            contentScroll.gameObject.name = "content scroll";
            contentScroll.AsRT().anchoredPosition = new Vector2(690f, -572f);
            contentScroll.AsRT().sizeDelta = new Vector2(1384f, 892f);

            contentScroll.GetComponent<ScrollRect>().horizontal = false;

            content = contentScroll.Find("Viewport/Content");
            contentLayout = content.GetComponent<GridLayoutGroup>();

            baseCardPrefab = content.GetChild(0).gameObject;
            baseCardPrefab.transform.SetParent(assetsParent);
            baseCardPrefab.SetActive(true);
            var baseCardPrefabButton = baseCardPrefab.GetComponent<Button>();
            baseCardPrefabButton.colors = UIManager.SetColorBlock(baseCardPrefabButton.colors, new Color(0.1294f, 0.1294f, 0.1294f, 1f), new Color(0.3f, 0.3f, 0.3f, 1f), new Color(0.3f, 0.3f, 0.3f, 1f), new Color(0.3f, 0.3f, 0.3f, 1f), LSColors.red700);

            var scrollBarVertical = contentScroll.Find("Scrollbar Vertical");
            scrollBarVertical.GetComponent<Image>().color = new Color(0.11f, 0.11f, 0.11f, 1f);
            var handleImage = scrollBarVertical.Find("Sliding Area/Handle").GetComponent<Image>();
            handleImage.color = new Color(0.878f, 0.878f, 0.878f, 1f);
            handleImage.sprite = null;

            contentBase.Find("Image").AsRT().anchoredPosition = new Vector2(690f, /*-94f*/ -104f);
            contentBase.Find("Image").AsRT().sizeDelta = new Vector2(1384f, 48f);

            var searchBase = GameObject.Find("Editor Systems/Editor GUI/sizer/main/Popups/Open File Popup/search-box").Duplicate(contentBase.Find("Image"), "search base");
            searchBase.transform.localScale = Vector3.one;
            searchBase.transform.AsRT().anchoredPosition = Vector2.zero;
            searchBase.transform.AsRT().sizeDelta = new Vector2(0f, 48f);
            searchBase.transform.GetChild(0).AsRT().sizeDelta = new Vector2(0f, 48f);

            var searchField = searchBase.transform.GetChild(0).GetComponent<InputField>();
            searchField.onValueChanged.ClearAll();
            ((Text)searchField.placeholder).text = "Search...";
            searchField.onValueChanged.AddListener(delegate (string _val)
            {
                Debug.Log($"{EditorPlugin.className}Searching {_val}");
                SearchTerm = _val;
                RefreshList();
            });

            var tfv = ObjEditor.inst.ObjectView.transform;

            var addNewItem = tfv.Find("applyprefab").gameObject.Duplicate(contentBase);
            addNewItem.SetActive(true);
            addNewItem.name = "new";
            addNewItem.transform.SetSiblingIndex(1);
            addNewItem.transform.AsRT().anchoredPosition = new Vector2(120f, 970f);
            addNewItem.transform.AsRT().sizeDelta = new Vector2(200f, 32f);
            addNewItem.transform.GetChild(0).GetComponent<Text>().text = "Add New Item";
            var addNewItemButton = addNewItem.GetComponent<Button>();
            addNewItemButton.onClick.ClearAll();
            addNewItemButton.onClick.AddListener(delegate ()
            {
                Debug.Log($"{EditorPlugin.className}Create new {tabNames[CurrentTab]}");
                var path = $"{RTFile.ApplicationDirectory}beatmaps/{PlannersPath}";
                switch (CurrentTab)
                {
                    case 0:
                        {
                            var list = planners.Where(x => x.PlannerType == PlannerItem.Type.Document && x is DocumentItem).Select(x => x as DocumentItem);

                            var document = new DocumentItem();
                            document.PlannerType = PlannerItem.Type.Document;
                            document.Name = $"New Story {list.Count() + 1}";
                            document.Text = "<align=center>Plan out your story!";
                            planners.Add(document);
                            GenerateDocument(document);

                            SaveDocuments();

                            break;
                        } // Document
                    case 1:
                        {
                            var list = planners.Where(x => x.PlannerType == PlannerItem.Type.TODO && x is TODOItem).Select(x => x as TODOItem);

                            var todo = new TODOItem();
                            todo.PlannerType = PlannerItem.Type.TODO;
                            todo.Checked = false;
                            todo.Text = "Do this.";
                            todo.Priority = list.Count();
                            planners.Add(todo);
                            GenerateTODO(todo);

                            SaveTODO();

                            break;
                        } // TODO
                    case 2:
                        {
                            var list = planners.Where(x => x.PlannerType == PlannerItem.Type.Character && x is CharacterItem).Select(x => x as CharacterItem);

                            if (string.IsNullOrEmpty(Path.GetFileName(path)))
                                return;

                            if (!RTFile.DirectoryExists(path + "/characters"))
                            {
                                Directory.CreateDirectory(path + "/characters");
                            }

                            var fullPath = path + "/characters/New Character";

                            int num = 1;
                            while (RTFile.DirectoryExists(fullPath))
                            {
                                fullPath = $"{path}/characters/New Character {num}";
                                num++;
                            }

                            Directory.CreateDirectory(fullPath);

                            var character = new CharacterItem();

                            for (int i = 0; i < 3; i++)
                            {
                                character.CharacterTraits.Add("???");
                                character.CharacterLore.Add("???");
                                character.CharacterAbilities.Add("???");
                            }

                            character.CharacterSprite = SpriteManager.LoadSprite(RTFile.ApplicationDirectory + RTFunctions.FunctionsPlugin.BepInExAssetsPath + "CA Hal.png");
                            character.Name = Path.GetFileName(fullPath);
                            character.FullPath = fullPath;
                            character.Gender = "He";
                            character.Description = "This is the default description";
                            planners.Add(character);
                            GenerateCharacter(character);
                            character.Save();

                            break;
                        } // Character
                    default:
                        {
                            Debug.LogWarning($"{EditorPlugin.className}Not implemented yet!");
                            break;
                        }
                }
                RefreshList();
            });

            var reload = tfv.Find("applyprefab").gameObject.Duplicate(contentBase);
            reload.SetActive(true);
            reload.name = "reload";
            reload.transform.SetSiblingIndex(2);
            reload.transform.AsRT().anchoredPosition = new Vector2(370f, 970f);
            reload.transform.AsRT().sizeDelta = new Vector2(200f, 32f);
            reload.transform.GetChild(0).GetComponent<Text>().text = "Reload";
            var reloadButton = reload.GetComponent<Button>();
            reloadButton.onClick.ClearAll();
            reloadButton.onClick.AddListener(delegate ()
            {
                Load();
            });

            gradientSprite = SpriteManager.LoadSprite(RTFile.ApplicationDirectory + RTFunctions.FunctionsPlugin.BepInExAssetsPath + "linear_gradient.png");

            // Item Prefabs
            {
                // Document
                {
                    var prefab = baseCardPrefab.Duplicate(assetsParent, "document prefab");
                    var albumArt = prefab.transform.GetChild(0);
                    var title = prefab.transform.GetChild(1);
                    var artist = prefab.transform.GetChild(2);

                    albumArt.SetSiblingIndex(2);

                    prefab.GetComponent<Image>().sprite = null;

                    albumArt.name = "gradient";
                    title.name = "name";
                    artist.name = "words";

                    title.AsRT().anchoredPosition = new Vector2(0f, 150f);
                    title.AsRT().sizeDelta = new Vector2(-32f, 80f);

                    var albumArtImage = albumArt.GetComponent<Image>();
                    albumArtImage.sprite = gradientSprite;
                    albumArtImage.color = new Color(0.1294f, 0.1294f, 0.1294f, 1f);

                    albumArt.AsRT().anchoredPosition = Vector2.zero;
                    albumArt.AsRT().sizeDelta = new Vector2(232f, 400f);

                    artist.AsRT().anchoredPosition = new Vector2(0f, -60f);
                    artist.AsRT().sizeDelta = new Vector2(-32f, 260f);
                    var tmp = artist.GetComponent<TextMeshProUGUI>();
                    tmp.alignment = TextAlignmentOptions.TopLeft;
                    tmp.enableWordWrapping = true;
                    tmp.text = "This is your story.";

                    prefabs.Add(prefab);
                }

                // TODO
                {
                    var prefab = baseCardPrefab.Duplicate(assetsParent, "todo prefab");
                    var albumArt = prefab.transform.GetChild(0);
                    var title = prefab.transform.GetChild(1);
                    var artist = prefab.transform.GetChild(2);

                    Destroy(albumArt.gameObject);
                    Destroy(artist.gameObject);

                    prefab.GetComponent<Image>().sprite = null;

                    title.name = "text";

                    title.AsRT().anchoredPosition = Vector2.zero;
                    title.AsRT().sizeDelta = new Vector2(-120f, 64f);

                    var toggle = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/toggle/toggle").Duplicate(prefab.transform, "checked");
                    toggle.transform.AsRT().anchoredPosition = new Vector2(32f, -32f);
                    toggle.transform.AsRT().sizeDelta = Vector2.zero;
                    toggle.transform.GetChild(0).AsRT().pivot = new Vector2(0.5f, 0.5f);
                    toggle.transform.GetChild(0).AsRT().sizeDelta = new Vector2(38f, 38f);
                    toggle.transform.GetChild(0).GetChild(0).AsRT().sizeDelta = new Vector2(36f, 36f);

                    var tmp = title.GetComponent<TextMeshProUGUI>();
                    tmp.alignment = TextAlignmentOptions.Left;
                    tmp.enableWordWrapping = false;
                    tmp.text = "Do this.";

                    var delete = closePrefab.Duplicate(prefab.transform, "delete");
                    delete.transform.AsRT().anchoredPosition = new Vector2(-32f, -32f);
                    delete.transform.AsRT().pivot = new Vector2(0.5f, 0.5f);
                    delete.transform.AsRT().sizeDelta = new Vector2(38f, 38f);

                    prefabs.Add(prefab);
                }

                // Character
                {
                    var prefab = baseCardPrefab.Duplicate(assetsParent, "todo prefab");
                    var albumArt = prefab.transform.GetChild(0);
                    var title = prefab.transform.GetChild(1);
                    var artist = prefab.transform.GetChild(2);

                    albumArt.name = "profile";
                    title.name = "details";
                    artist.name = "description";

                    albumArt.AsRT().anchoredPosition = new Vector2(80f, 66f);
                    albumArt.AsRT().sizeDelta = new Vector2(115f, 115f);

                    title.AsRT().anchoredPosition = new Vector2(0f, 2f);
                    title.AsRT().sizeDelta = new Vector2(-32f, 240f);

                    artist.AsRT().anchoredPosition = new Vector2(60f, -64f);
                    artist.AsRT().sizeDelta = new Vector2(-130f, 130f);

                    var tmpTitle = title.GetComponent<TextMeshProUGUI>();
                    tmpTitle.lineSpacing = -20;
                    tmpTitle.fontSize = 13;
                    tmpTitle.fontStyle = FontStyles.Normal;
                    tmpTitle.alignment = TextAlignmentOptions.TopLeft;
                    tmpTitle.enableWordWrapping = true;
                    tmpTitle.text = CharacterItem.DefaultCharacterDescription;

                    var tmpArtist = artist.GetComponent<TextMeshProUGUI>();
                    tmpArtist.alignment = TextAlignmentOptions.TopRight;
                    tmpArtist.fontSize = 12;
                    tmpArtist.enableWordWrapping = true;
                    tmpArtist.text = "Description";

                    prefabs.Add(prefab);
                }
            }

            RenderTabs();
            Load();
        }

        void Start()
        {

        }

        void Update()
        {

        }

        #region Save / Load

        public void Load()
        {
            planners.Clear();
            LSHelpers.DeleteChildren(content);

            var path = $"{RTFile.ApplicationDirectory}beatmaps/{PlannersPath}";
            if (string.IsNullOrEmpty(Path.GetFileName(path)))
                return;

            if (!RTFile.DirectoryExists(path))
                Directory.CreateDirectory(path);

            LoadDocuments();
            LoadTODO();
            LoadTimelines();
            LoadSchedules();
            LoadNotes();

            if (!RTFile.DirectoryExists(path + "/characters"))
            {
                Directory.CreateDirectory(path + "/characters");
            }
            else
            {
                var directories = Directory.GetDirectories(path + "/characters", "*", SearchOption.TopDirectoryOnly);
                if (directories.Length > 0)
                {
                    foreach (var folder in directories)
                    {
                        var character = new CharacterItem(folder);
                        planners.Add(character);
                        GenerateCharacter(character);
                    }
                }
            }

            RefreshList();
        }

        public void SaveDocuments()
        {
            var path = $"{RTFile.ApplicationDirectory}beatmaps/{PlannersPath}";
            if (string.IsNullOrEmpty(Path.GetFileName(path)) || !RTFile.DirectoryExists(path))
                return;

            var jn = JSON.Parse("{}");

            var list = planners.Where(x => x.PlannerType == PlannerItem.Type.Document && x is DocumentItem).Select(x => x as DocumentItem).ToList();

            for (int i = 0; i < list.Count; i++)
            {
                var document = list[i];
                jn["documents"][i]["name"] = document.Name;
                jn["documents"][i]["text"] = document.Text;
            }

            RTFile.WriteToFile(path + "/documents.lsn", jn.ToString(3));
        }

        public void LoadDocuments()
        {
            var path = $"{RTFile.ApplicationDirectory}beatmaps/{PlannersPath}";
            if (!RTFile.FileExists(path + "/documents.lsn"))
                return;

            var jn = JSON.Parse(RTFile.ReadFromFile(path + "/documents.lsn"));

            for (int i = 0; i < jn["documents"].Count; i++)
            {
                var document = new DocumentItem();

                document.Name = jn["documents"][i]["name"];
                document.Text = jn["documents"][i]["text"];

                GenerateDocument(document);

                planners.Add(document);
            }
        }

        public void SaveTODO()
        {
            var path = $"{RTFile.ApplicationDirectory}beatmaps/{PlannersPath}";
            if (string.IsNullOrEmpty(Path.GetFileName(path)) || !RTFile.DirectoryExists(path))
                return;

            var jn = JSON.Parse("{}");

            var list = planners.Where(x => x.PlannerType == PlannerItem.Type.TODO && x is TODOItem).Select(x => x as TODOItem).OrderBy(x => x.Priority).ToList();

            for (int i = 0; i < list.Count; i++)
            {
                jn["todo"][i]["ch"] = list[i].Checked.ToString();
                jn["todo"][i]["pr"] = list[i].Priority.ToString();
                jn["todo"][i]["text"] = list[i].Text;
            }

            RTFile.WriteToFile(path + "/todo.lsn", jn.ToString(3));
        }

        public void LoadTODO()
        {
            var path = $"{RTFile.ApplicationDirectory}beatmaps/{PlannersPath}";
            if (!RTFile.FileExists(path + "/todo.lsn"))
                return;

            var jn = JSON.Parse(RTFile.ReadFromFile(path + "/todo.lsn"));

            var todos = new List<TODOItem>();
            for (int i = 0; i < jn["todo"].Count; i++)
            {
                var todo = new TODOItem();
                todo.Checked = jn["todo"][i]["ch"].AsBool;
                todo.Priority = jn["todo"][i]["pr"].AsInt;
                todo.Text = jn["todo"][i]["text"];
                todos.Add(todo);
            }

            todos = todos.OrderBy(x => x.Priority).ToList();

            todos.ForEach(x =>
            {
                GenerateTODO(x);
            });

            planners.AddRange(todos);

            todos.Clear();
            todos = null;
        }

        public void SaveTimelines()
        {
            var path = $"{RTFile.ApplicationDirectory}beatmaps/{PlannersPath}";
            if (string.IsNullOrEmpty(Path.GetFileName(path)) || !RTFile.DirectoryExists(path))
                return;

            var jn = JSON.Parse("{}");

            var list = planners.Where(x => x.PlannerType == PlannerItem.Type.Timeline && x is TimelineItem).Select(x => x as TimelineItem).ToList();

            for (int i = 0; i < list.Count; i++)
            {
                for (int j = 0; j < list[i].Levels.Count; j++)
                {
                    var level = list[i].Levels[j];
                    jn["timelines"][i]["levels"][j]["n"] = level.Name;
                    jn["timelines"][i]["levels"][j]["p"] = level.Path;
                    jn["timelines"][i]["levels"][j]["d"] = level.Description;
                }

                for (int j = 0; j < list[i].Stories.Count; j++)
                {
                    var story = list[i].Stories[j];
                    jn["timelines"][i]["stories"][j]["n"] = story.Name;
                    jn["timelines"][i]["stories"][j]["d"] = story.Description;
                }
            }

            RTFile.WriteToFile(path + "/timelines.lsn", jn.ToString(3));
        }

        public void LoadTimelines()
        {
            var path = $"{RTFile.ApplicationDirectory}beatmaps/{PlannersPath}";
            if (!RTFile.FileExists(path + "/timelines.lsn"))
                return;

            var jn = JSON.Parse(RTFile.ReadFromFile(path + "/timelines.lsn"));

            for (int i = 0; i < jn["timelines"].Count; i++)
            {
                var timeline = new TimelineItem();

                for (int j = 0; j < jn["timelines"][i]["levels"].Count; j++)
                {
                    timeline.Levels.Add(new TimelineItem.Level
                    {
                        Name = jn["timelines"][i]["levels"][j]["n"],
                        Path = jn["timelines"][i]["levels"][j]["p"],
                        Description = jn["timelines"][i]["levels"][j]["d"],
                    });
                }

                for (int j = 0; j < jn["timelines"][i]["stories"].Count; j++)
                {
                    timeline.Stories.Add(new TimelineItem.Story
                    {
                        Name = jn["timelines"][i]["stories"][j]["n"],
                        Description = jn["timelines"][i]["stories"][j]["d"],
                    });
                }

                // Generate Timelines goes here

                planners.Add(timeline);
            }
        }

        public void SaveSchedules()
        {
            var path = $"{RTFile.ApplicationDirectory}beatmaps/{PlannersPath}";
            if (string.IsNullOrEmpty(Path.GetFileName(path)) || !RTFile.DirectoryExists(path))
                return;

            var jn = JSON.Parse("{}");

            var list = planners.Where(x => x.PlannerType == PlannerItem.Type.Schedule && x is ScheduleItem).Select(x => x as ScheduleItem).ToList();

            for (int i = 0; i < list.Count; i++)
            {
                var schedule = list[i];
                jn["schedules"][i]["date"] = schedule.Date;
                jn["schedules"][i]["desc"] = schedule.Description;
            }

            RTFile.WriteToFile(path + "/schedules.lsn", jn.ToString(3));
        }

        public void LoadSchedules()
        {
            var path = $"{RTFile.ApplicationDirectory}beatmaps/{PlannersPath}";
            if (!RTFile.FileExists(path + "/schedules.lsn"))
                return;

            var jn = JSON.Parse(RTFile.ReadFromFile(path + "/schedules.lsn"));

            for (int i = 0; i < jn["schedules"].Count; i++)
            {
                var schedule = new ScheduleItem();
                schedule.Date = jn["schedules"][i]["date"];
                schedule.Description = jn["schedules"][i]["schedules"];

                // Generate Shedule goes here

                planners.Add(schedule);
            }
        }

        public void SaveNotes()
        {
            var path = $"{RTFile.ApplicationDirectory}beatmaps/{PlannersPath}";
            if (string.IsNullOrEmpty(Path.GetFileName(path)) || !RTFile.DirectoryExists(path))
                return;

            var jn = JSON.Parse("{}");

            var list = planners.Where(x => x.PlannerType == PlannerItem.Type.Note && x is NoteItem).Select(x => x as NoteItem).ToList();

            for (int i = 0; i < list.Count; i++)
            {
                var note = list[i];

                jn["notes"][i]["active"] = note.Active.ToString();
                jn["notes"][i]["pos"]["x"] = note.Position.x.ToString();
                jn["notes"][i]["pos"]["y"] = note.Position.y.ToString();
                jn["notes"][i]["sca"]["x"] = note.Scale.x.ToString();
                jn["notes"][i]["sca"]["y"] = note.Scale.y.ToString();
                jn["notes"][i]["col"] = note.Color.ToString();
                jn["notes"][i]["text"] = note.Text;
            }

            RTFile.WriteToFile(path + "/notes.lsn", jn.ToString(3));
        }

        public void LoadNotes()
        {
            var path = $"{RTFile.ApplicationDirectory}beatmaps/{PlannersPath}";
            if (!RTFile.FileExists(path + "/notes.lsn"))
                return;

            var jn = JSON.Parse(RTFile.ReadFromFile(path + "/notes.lsn"));

            for (int i = 0; i < jn["notes"].Count; i++)
            {
                var note = new NoteItem();

                note.Active = jn["notes"][i]["active"].AsBool;
                note.Position = new Vector2(jn["notes"][i]["pos"]["x"].AsFloat, jn["notes"][i]["pos"]["y"].AsFloat);
                note.Scale = new Vector2(jn["notes"][i]["sca"]["x"].AsFloat, jn["notes"][i]["sca"]["y"].AsFloat);
                note.Text = jn["notes"][i]["text"];

                // Generate Note goes here

                planners.Add(note);
            }
        }

        #endregion

        #region Refresh GUI

        public void RenderTabs()
        {
            contentLayout.cellSize = tabCellSizes[CurrentTab];
            contentLayout.constraintCount = tabConstraintCounts[CurrentTab];
            int num = 0;
            foreach (var tab in tabs)
            {
                int index = num;

                tab.onValueChanged.ClearAll();
                tab.isOn = index == CurrentTab;
                tab.onValueChanged.AddListener(delegate (bool value)
                {
                    if (value)
                    {
                        Debug.Log($"{EditorPlugin.className}Set tab to {tabNames[index]}");
                        CurrentTab = index;
                        RenderTabs();
                        RefreshList();
                    }
                });

                num++;
            }
        }

        public void RefreshList()
        {
            foreach (var plan in planners)
            {
                plan.GameObject?.SetActive(plan.PlannerType == (PlannerItem.Type)CurrentTab && (string.IsNullOrEmpty(SearchTerm) ||
                    plan is DocumentItem document && !string.IsNullOrEmpty(document.Name) && document.Name.ToLower().Contains(SearchTerm.ToLower()) ||
                    plan is TODOItem todo && (CheckOn(SearchTerm.ToLower()) && todo.Checked || CheckOff(SearchTerm.ToLower()) && !todo.Checked || todo.Text.ToLower().Contains(SearchTerm.ToLower())) ||
                    plan is CharacterItem character && (!string.IsNullOrEmpty(character.Name) && character.Name.ToLower().Contains(SearchTerm.ToLower()) || !string.IsNullOrEmpty(character.Description) && character.Description.ToLower().Contains(SearchTerm.ToLower())) ||
                    plan is TimelineItem timeline && (timeline.Levels.Has(x => x.Name.ToLower().Contains(SearchTerm.ToLower())) || timeline.Stories.Has(x => x.Name.ToLower().Contains(SearchTerm.ToLower()))) ||
                    plan is ScheduleItem schedule && (!string.IsNullOrEmpty(schedule.Description) && schedule.Description.ToLower().Contains(SearchTerm.ToLower()) || schedule.Date.ToLower().Contains(SearchTerm.ToLower())) ||
                    plan is NoteItem note && !string.IsNullOrEmpty(note.Text) && note.Text.ToLower().Contains(SearchTerm.ToLower())));
            }
        }

        bool CheckOn(string searchTerm)
            => searchTerm == "\"true\"" || searchTerm == "\"on\"" || searchTerm == "\"done\"" || searchTerm == "\"finished\"" || searchTerm == "\"checked\"";
        
        bool CheckOff(string searchTerm)
            => searchTerm == "\"false\"" || searchTerm == "\"off\"" || searchTerm == "\"not done\"" || searchTerm == "\"not finished\"" || searchTerm == "\"unfinished\"" || searchTerm == "\"unchecked\"";

        #endregion

        #region Generate UI

        public GameObject Spacer(string name, Transform parent, Vector2 size)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent);
            gameObject.transform.localScale = Vector3.one;

            var rt = gameObject.AddComponent<RectTransform>();

            rt.sizeDelta = size;

            return gameObject;
        }

        public GameObject GenerateTab(string name)
        {
            var gameObject = tabPrefab.Duplicate(topBarBase, name);
            gameObject.transform.localScale = Vector3.one;
            var text = gameObject.transform.Find("Background/Text").GetComponent<Text>();
            text.fontSize = 26;
            text.fontStyle = FontStyle.Bold;
            text.text = name;
            tabs.Add(gameObject.GetComponent<Toggle>());
            return gameObject;
        }

        public GameObject GenerateDocument(DocumentItem document)
        {
            var gameObject = prefabs[0].Duplicate(content, "document");
            document.GameObject = gameObject;
            gameObject.transform.Find("name").GetComponent<TextMeshProUGUI>().text = document.Name;
            gameObject.transform.Find("words").GetComponent<TextMeshProUGUI>().text = document.Text;
            return gameObject;
        }

        public GameObject GenerateTODO(TODOItem todo)
        {
            var gameObject = prefabs[1].Duplicate(content, "todo");
            todo.GameObject = gameObject;

            gameObject.transform.Find("text").GetComponent<TextMeshProUGUI>().text = todo.Text;

            var toggle = gameObject.transform.Find("checked").GetComponent<Toggle>();
            toggle.onValueChanged.ClearAll();
            toggle.isOn = todo.Checked;
            toggle.onValueChanged.AddListener(delegate (bool _val)
            {
                todo.Checked = _val;
                SaveTODO();
            });

            var delete = gameObject.transform.Find("delete").GetComponent<Button>();
            delete.onClick.ClearAll();
            delete.onClick.AddListener(delegate ()
            {
                planners.RemoveAll(x => x is TODOItem && x.ID == todo.ID);
                SaveTODO();
                Destroy(gameObject);
            });

            return gameObject;
        }

        public GameObject GenerateCharacter(CharacterItem character)
        {
            var gameObject = prefabs[2].Duplicate(content, "character");
            character.GameObject = gameObject;

            var profile = gameObject.transform.Find("profile").GetComponent<Image>();

            var details = gameObject.transform.Find("details").GetComponent<TextMeshProUGUI>();

            var description = gameObject.transform.Find("description").GetComponent<TextMeshProUGUI>();

            profile.sprite = character.CharacterSprite;
            details.text = character.FormatDetails;
            description.text = character.Description;

            return gameObject;
        }

        #endregion

        #region Open / Close UI

        public void Open()
        {
            EditorManager.inst.editorState = EditorManager.EditorState.Intro;
            UpdateStateUI();
        }
        
        public void Close()
        {
            EditorManager.inst.editorState = EditorManager.EditorState.Main;
            UpdateStateUI();
        }

        public void ToggleState() => SetState(EditorManager.inst.editorState == EditorManager.EditorState.Main ? EditorManager.EditorState.Intro : EditorManager.EditorState.Main);

        public void SetState(EditorManager.EditorState editorState)
        {
            EditorManager.inst.editorState = editorState;
            UpdateStateUI();
        }

        public void UpdateStateUI()
        {
            var editorState = EditorManager.inst.editorState;
            EditorManager.inst.GUIMain.SetActive(editorState == EditorManager.EditorState.Main);
            EditorManager.inst.GUIIntro.SetActive(editorState == EditorManager.EditorState.Intro);
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Base Planner class. FullPath must be the directory without a slash at the end. For example: Application Directory/beatmaps/planners/to do/item
        /// </summary>
        public class PlannerItem
        {
            public PlannerItem()
            {

            }

            public string ID { get; set; } = LSText.randomNumString(10);

            public GameObject GameObject { get; set; }

            public Type PlannerType { get; set; }

            public enum Type
            {
                Document,
                TODO,
                Character,
                Timeline,
                Schedule,
                Note
            }
        }

        public class DocumentItem : PlannerItem
        {
            public DocumentItem()
            {
                PlannerType = Type.Document;
            }

            public string Name { get; set; }
            public string Text { get; set; }
        }

        public class TODOItem : PlannerItem
        {
            public TODOItem()
            {
                PlannerType = Type.TODO;
            }

            public string Text { get; set; }
            public bool Checked { get; set; }
            public int Priority { get; set; }
        }

        public class CharacterItem : PlannerItem
        {
            public CharacterItem()
            {
                PlannerType = Type.Character;
            }

            public CharacterItem(string fullPath)
            {
                CharacterSprite = RTFile.FileExists(fullPath + "/profile.png") ? SpriteManager.LoadSprite(fullPath + "/profile.png") : SteamWorkshop.inst.defaultSteamImageSprite;

                if (RTFile.FileExists(fullPath + "/info.lsn"))
                {
                    var jn = JSON.Parse(RTFile.ReadFromFile(fullPath + "/info.lsn"));

                    Name = jn["name"];
                    Gender = jn["gender"];
                    Description = jn["desc"];

                    for (int i = 0; i < jn["tr"].Count; i++)
                    {
                        CharacterTraits.Add(jn["tr"][i]);
                    }

                    for (int i = 0; i < jn["lo"].Count; i++)
                    {
                        CharacterLore.Add(jn["lo"][i]);
                    }

                    for (int i = 0; i < jn["ab"].Count; i++)
                    {
                        CharacterAbilities.Add(jn["ab"][i]);
                    }
                }

                FullPath = fullPath;
                PlannerType = Type.Character;
            }

            public string Name { get; set; }
            public string Gender { get; set; }
            public List<string> CharacterTraits { get; set; } = new List<string>();
            public List<string> CharacterLore { get; set; } = new List<string>();
            public List<string> CharacterAbilities { get; set; } = new List<string>();
            public string Description { get; set; }
            public Sprite CharacterSprite { get; set; }

            public string FullPath { get; set; }

            public string FormatDetails
            {
                get
                {
                    var str = "";

                    str += $"<b>Name</b>: {Name}" + Environment.NewLine;
                    str += $"<b>Gender</b>: {Gender}" + Environment.NewLine;
                    str += $"<b>Character Traits</b>:" + Environment.NewLine;

                    for (int i = 0; i < CharacterTraits.Count; i++)
                    {
                        str += $"- {CharacterTraits[i]}" + Environment.NewLine;
                    }
                    str += Environment.NewLine;

                    str += $"<b>Lore</b>:" + Environment.NewLine;

                    for (int i = 0; i < CharacterLore.Count; i++)
                    {
                        str += $"- {CharacterLore[i]}" + Environment.NewLine;
                    }
                    str += Environment.NewLine;

                    str += $"<b>Abilities</b>:" + Environment.NewLine;

                    for (int i = 0; i < CharacterAbilities.Count; i++)
                    {
                        str += $"- {CharacterAbilities[i]}" + Environment.NewLine;
                    }

                    return str;
                }
            }

            public static string DefaultCharacterDescription => "<b>Name</b>: Viral Mecha" + Environment.NewLine +
                                        "<b>Gender</b>: He" + Environment.NewLine + Environment.NewLine +
                                        "<b>Character Traits</b>:" + Environment.NewLine +
                                        "- ???" + Environment.NewLine +
                                        "- ???" + Environment.NewLine +
                                        "- ???" + Environment.NewLine + Environment.NewLine +
                                        "<b>Lore</b>:" + Environment.NewLine +
                                        "- ???" + Environment.NewLine +
                                        "- ???" + Environment.NewLine +
                                        "- ???" + Environment.NewLine + Environment.NewLine +
                                        "<b>Abilities</b>:" + Environment.NewLine +
                                        "- ???" + Environment.NewLine +
                                        "- ???" + Environment.NewLine +
                                        "- ???";

            public void Save()
            {
                var jn = JSON.Parse("{}");

                jn["name"] = Name;
                jn["gender"] = Gender;
                jn["desc"] = Description;

                for (int i = 0; i < CharacterTraits.Count; i++)
                    jn["tr"][i] = CharacterTraits[i];
                
                for (int i = 0; i < CharacterLore.Count; i++)
                    jn["lo"][i] = CharacterLore[i];
                
                for (int i = 0; i < CharacterAbilities.Count; i++)
                    jn["ab"][i] = CharacterAbilities[i];

                RTFile.WriteToFile(FullPath + "/info.lsn", jn.ToString(3));

                SpriteManager.SaveSprite(CharacterSprite, FullPath + "/profile.png");
            }
        }

        public class TimelineItem : PlannerItem
        {
            public TimelineItem()
            {
                PlannerType = Type.Timeline;
            }

            public List<Level> Levels { get; set; } = new List<Level>();
            public List<Story> Stories { get; set; } = new List<Story>();

            public class Level
            {
                public string Name { get; set; }
                /// <summary>
                /// Path to level to open when level button is clicked.
                /// </summary>
                public string Path { get; set; }
                /// <summary>
                /// Not the actual metadata description. Just a timeline description.
                /// </summary>
                public string Description { get; set; }
            }

            public class Story
            {
                public string Name { get; set; }
                public string Description { get; set; }
            }
        }

        public class ScheduleItem : PlannerItem
        {
            public ScheduleItem()
            {
                PlannerType = Type.Schedule;
            }

            public string Date { get; set; } = DateTime.Now.ToString("G");
            public string Description { get; set; }
        }

        public class NoteItem : PlannerItem
        {
            public NoteItem()
            {
                PlannerType = Type.Note;
            }

            public bool Active { get; set; }
            public Vector2 Position { get; set; } = Vector2.zero;
            public Vector2 Scale { get; set; } = new Vector2(600f, 300f);
            public int Color { get; set; }
            public string Text { get; set; }

            public Color TopColor => Color > 0 && Color < MarkerEditor.inst.markerColors.Count ? MarkerEditor.inst.markerColors[Color] : LSColors.red700;
        }

        #endregion
    }
}
