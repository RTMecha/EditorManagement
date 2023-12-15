using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

using SimpleJSON;
using LSFunctions;

using EditorManagement.Functions.Editors;
using RTFunctions.Functions;
using RTFunctions.Functions.Data;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;
using RTFunctions.Functions.Optimization;

using BaseBeatmapObject = DataManager.GameData.BeatmapObject;
using BaseEventKeyframe = DataManager.GameData.EventKeyframe;
using BasePrefab = DataManager.GameData.Prefab;
using BasePrefabObject = DataManager.GameData.PrefabObject;
using BaseBackgroundObject = DataManager.GameData.BackgroundObject;

using ObjectType = DataManager.GameData.BeatmapObject.ObjectType;
using AutoKillType = DataManager.GameData.BeatmapObject.AutoKillType;

using BaseObjectSelection = ObjEditor.ObjectSelection;
using BaseObjectKeyframeSelection = ObjEditor.KeyframeSelection;
using EventKeyframeSelection = EventEditor.KeyframeSelection;
using EditorManagement.Functions.Helpers;

namespace EditorManagement.Functions.Editors
{
    public class KeybindManager : MonoBehaviour
    {
        public static string className = "[<color=#F44336>KeybindManager</color>] \n";
        public static KeybindManager inst;

        public static string FilePath => $"{RTFile.ApplicationDirectory}settings/keybinds.lss";

        public bool isPressingKey;

        static Scrollbar tlScrollbar;

        public int currentKey;

        public static void Init(EditorManager editorManager)
        {
            var gameObject = new GameObject("KeybindManager");
            gameObject.AddComponent<KeybindManager>();
            gameObject.transform.SetParent(editorManager.transform.parent);
        }

        void Awake()
        {
            inst = this;

            if (!RTFile.FileExists(FilePath))
                FirstInit();
            else
                Load();

            GenerateKeybindEditorPopupDialog();
        }

        void Update()
        {
            if (!LSHelpers.IsUsingInputField() && EditorManager.inst.isEditing && (!ModCompatibility.sharedFunctions.ContainsKey("EventsCoreEditorOffset") || !(bool)ModCompatibility.sharedFunctions["EventsCoreEditorOffset"]))
            {
                foreach (var keybind in keybinds)
                {
                    if (!keybind.watchingKeybind)
                        keybind.Activate();
                    else
                    {
                        var watch = WatchKeyCode();
                        if (watch != KeyCode.None)
                            keybind.keys[Mathf.Clamp(currentKey, 0, keybind.keys.Count - 1)].KeyCode = watch;
                    }
                }
            }
        }

        public void FirstInit()
        {
            // Save
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl),
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.S),
            }, 9));

            // Open Beatmap Popup
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl),
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.O),
            }, 10));

            // Set Layer 1
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Alpha1),
            }, 11, new Dictionary<string, string>
            {
                { "Layer", "0" }
            }));

            // Set Layer 2
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Alpha2),
            }, 11, new Dictionary<string, string>
            {
                { "Layer", "1" }
            }));

            // Set Layer 3
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Alpha3),
            }, 11, new Dictionary<string, string>
            {
                { "Layer", "2" }
            }));

            // Set Layer 4
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Alpha4),
            }, 11, new Dictionary<string, string>
            {
                { "Layer", "3" }
            }));

            // Set Layer 5
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Alpha5),
            }, 11, new Dictionary<string, string>
            {
                { "Layer", "4" }
            }));

            // Set Layer 6
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Alpha6),
            }, 11, new Dictionary<string, string>
            {
                { "Layer", "5" }
            }));

            // Toggle Event Layer
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftShift),
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.E),
            }, 12));
            
            // Undo
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl),
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Z),
            }, 13));
            
            // Redo
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl),
                new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftShift),
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Z),
            }, 14));
            
            // Toggle Playing Song
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Space),
            }, 15));

            // Swap Lock Selection
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl),
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.L),
            }, 19));

            // Update Object
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl),
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.R),
            }, 3));

            // Update Everything
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl),
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.T),
            }, 2));

            // Set First Keyframe In Type
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl),
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Period),
            }, 34));
            
            // Set Last Keyframe In Type
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl),
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Comma),
            }, 35));
            
            // Set Next Keyframe In Type
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Period),
            }, 36));
            
            // Set Previous Keyframe In Type
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Comma),
            }, 37));
            
            // Add Pitch
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.UpArrow),
            }, 38));
            
            // Sub Pitch
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.DownArrow),
            }, 39));
            
            // Toggle Show Help
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl),
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.H),
            }, 40));
            
            // Go To Current
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Insert),
            }, 41));
            
            // Go To Start
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Home),
            }, 42));
            
            // Go To End
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.End),
            }, 43));
            
            // Create New Marker
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl),
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.M),
            }, 44));
            
            // Spawn Prefab
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Slash),
            }, 45, new Dictionary<string, string>
            {
                { "External", "False" },
                { "UseID", "True" },
                { "ID", "" },
                { "Index", "0" },
            }));
            
            // Cut
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl),
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.X),
            }, 46));
            
            // Copy
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl),
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.C),
            }, 47));
            
            // Paste
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl),
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.V),
            }, 48, new Dictionary<string, string>
            {
                { "Remove Prefab Instance ID", "False" }
            }));
            
            // Duplicate
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl),
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.D),
            }, 49, new Dictionary<string, string>
            {
                { "Remove Prefab Instance ID", "False" }
            }));

            // Delete (Backspace key)
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Backspace),
            }, 50));
            
            // Delete (Delete key)
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Delete),
            }, 50));

            // Custom Code
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl),
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.I),
            }, 0, new Dictionary<string, string>
            {
                { "Code", "Debug.Log($\"{EditorManagement.Functions.Editors.KeybindManager.className} This is an example! You can use the keybind variable to check any settings you may have.\");" }
            }));

            Save();
        }

        public void Save()
        {
            var jn = JSON.Parse("{}");
            for (int i = 0; i < keybinds.Count; i++)
            {
                jn["keybinds"][i] = keybinds[i].ToJSON();
            }

            RTFile.WriteToFile(FilePath, jn.ToString());
        }

        public void Load()
        {
            if (RTFile.FileExists(FilePath))
            {
                var jn = JSON.Parse(RTFile.ReadFromFile(FilePath));
                for (int i = 0; i < jn["keybinds"].Count; i++)
                    keybinds.Add(Keybind.Parse(jn["keybinds"][i]));
            }
        }

        #region Dialog

        public Transform content;
        public Sprite editSprite;
        public Transform editorDialog;
        public Dropdown actionDropdown;
        public RectTransform keysContent;

        public GameObject keyPrefab;

        public void GenerateKeybindEditorPopupDialog()
        {
            var qap = EditorManager.inst.GetDialog("Quick Actions Popup").Dialog;
            var popup = qap.gameObject.Duplicate(qap.parent, "Keybind List Popup");
            content = popup.transform.Find("mask/content");

            StartCoroutine(EditorManager.inst.GetSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_edit.png", new EditorManager.SpriteLimits(), delegate (Sprite sprite)
            {
                editSprite = sprite;
            }, delegate (string onError)
            {

            }));

            var close = popup.transform.Find("Panel/x").GetComponent<Button>();
            close.onClick.ClearAll();
            close.onClick.AddListener(delegate ()
            {
                EditorManager.inst.HideDialog("Keybind List Popup");
            });

            EditorHelper.AddEditorPopup("Keybind List Popup", popup);

            var dialog = EditorManager.inst.GetDialog("Multi Keyframe Editor (Object)").Dialog;
            editorDialog = dialog.gameObject.Duplicate(dialog.parent, "KeybindEditor").transform;
            editorDialog.position = new Vector3(1537.5f, 714.945f, 0f) * EditorManager.inst.ScreenScale;
            ((RectTransform)editorDialog).sizeDelta = new Vector2(0f, 32f);

            editorDialog.Find("title/Text").GetComponent<Text>().text = "- Keybind Editor -";
            Destroy(editorDialog.Find("Text").gameObject);

            var data = new GameObject("data");
            data.transform.SetParent(editorDialog);
            data.transform.localScale = Vector3.one;
            var dataRT = data.AddComponent<RectTransform>();
            dataRT.sizeDelta = new Vector2(765f, 300f);
            var dataVLG = data.AddComponent<VerticalLayoutGroup>();
            dataVLG.childControlHeight = false;
            dataVLG.childForceExpandHeight = false;
            dataVLG.spacing = 4f;

            var action = new GameObject("action");
            action.transform.SetParent(dataRT);
            action.transform.localScale = Vector3.one;
            var actionRT = action.AddComponent<RectTransform>();
            actionRT.sizeDelta = new Vector2(765f, 32f);
            var actionHLG = action.AddComponent<HorizontalLayoutGroup>();
            actionHLG.childControlWidth = false;
            actionHLG.childForceExpandWidth = false;

            var title = EditorManager.inst.GetDialog("Prefab Editor").Dialog.Find("data/name/title").gameObject
                .Duplicate(actionRT, "title");
            title.GetComponent<Text>().text = "Action";

            var actionDropdown = EditorManager.inst.GetDialog("Object Editor").Dialog.Find("data/left/Scroll View/Viewport/Content/autokill/tod-dropdown").gameObject
                .Duplicate(actionRT, "dropdown");

            ((RectTransform)actionDropdown.transform).sizeDelta = new Vector2(632f, 32f);

            this.actionDropdown = actionDropdown.GetComponent<Dropdown>();
            this.actionDropdown.onValueChanged.ClearAll();
            this.actionDropdown.options = KeybinderMethods.Select(x => new Dropdown.OptionData(x.Method.Name)).ToList();
            this.actionDropdown.value = 0;

            var scrollRect = new GameObject("ScrollRect");
            scrollRect.transform.SetParent(dataRT);
            scrollRect.transform.localScale = Vector3.one;
            var scrollRectRT = scrollRect.AddComponent<RectTransform>();
            scrollRectRT.anchoredPosition = new Vector2(0f, 16f);
            scrollRectRT.sizeDelta = new Vector2(400f, 250f);
            var scrollRectSR = scrollRect.AddComponent<ScrollRect>();

            var maskGO = new GameObject("Mask");
            maskGO.transform.SetParent(scrollRectRT);
            maskGO.transform.localScale = Vector3.one;
            var maskRT = maskGO.AddComponent<RectTransform>();
            maskRT.anchoredPosition = new Vector2(0f, 0f);
            maskRT.anchorMax = new Vector2(1f, 1f);
            maskRT.anchorMin = new Vector2(0f, 0f);
            maskRT.sizeDelta = new Vector2(0f, 0f);
            var maskImage = maskGO.AddComponent<Image>();
            maskImage.color = new Color(1f, 1f, 1f, 0.04f);
            var mask = maskGO.AddComponent<Mask>();

            var keysContentGO = new GameObject("Content");
            keysContentGO.transform.SetParent(maskRT);
            keysContentGO.transform.localScale = Vector3.one;
            keysContent = keysContentGO.AddComponent<RectTransform>();

            keysContent.anchoredPosition = new Vector2(0f, -16f);
            keysContent.anchorMax = new Vector2(0f, 1f);
            keysContent.anchorMin = new Vector2(0f, 1f);
            keysContent.pivot = new Vector2(0f, 1f);
            keysContent.sizeDelta = new Vector2(400f, 250f);

            var contentSizeFitter = keysContentGO.AddComponent<ContentSizeFitter>();
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.MinSize;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;

            var contentVLG = keysContentGO.AddComponent<VerticalLayoutGroup>();
            contentVLG.childControlHeight = false;
            contentVLG.childForceExpandHeight = false;
            contentVLG.spacing = 4f;

            var contentLE = keysContentGO.AddComponent<LayoutElement>();
            contentLE.layoutPriority = 10000;
            contentLE.minWidth = 760;

            scrollRectSR.content = keysContent;

            // Key Prefab
            {
                keyPrefab = new GameObject("Key");
                var rectTransform = keyPrefab.AddComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(400f, 32f);
                var image = keyPrefab.AddComponent<Image>();
                image.color = new Color(0.2f, 0.2f, 0.2f);

                var horizontalLayoutGroup = keyPrefab.AddComponent<HorizontalLayoutGroup>();
                horizontalLayoutGroup.childControlWidth = false;
                horizontalLayoutGroup.childForceExpandWidth = false;
                horizontalLayoutGroup.spacing = 4;

                var keyTypeDropdown = EditorManager.inst.GetDialog("Object Editor").Dialog.Find("data/left/Scroll View/Viewport/Content/autokill/tod-dropdown").gameObject
                .Duplicate(rectTransform, "Key Type");

                ((RectTransform)keyTypeDropdown.transform).sizeDelta = new Vector2(360f, 32f);

                var keyTypeDropdownDD = keyTypeDropdown.GetComponent<Dropdown>();
                keyTypeDropdownDD.onValueChanged.ClearAll();
                keyTypeDropdownDD.options = Enum.GetNames(typeof(Keybind.Key.Type)).Select(x => new Dropdown.OptionData(x)).ToList();
                keyTypeDropdownDD.value = 0;

                var keyCodeDropdown = EditorManager.inst.GetDialog("Object Editor").Dialog.Find("data/left/Scroll View/Viewport/Content/autokill/tod-dropdown").gameObject
                .Duplicate(rectTransform, "Key Code");

                ((RectTransform)keyCodeDropdown.transform).sizeDelta = new Vector2(360f, 32f);

                var keyCodeDropdownDD = keyCodeDropdown.GetComponent<Dropdown>();
                keyCodeDropdownDD.onValueChanged.ClearAll();
                keyCodeDropdownDD.value = 0;
                keyCodeDropdownDD.options.Clear();

                var hide = keyCodeDropdown.AddComponent<HideDropdownOptions>();

                var keyCodes = Enum.GetValues(typeof(KeyCode));
                for (int i = 0; i < keyCodes.Length; i++)
                {
                    var str = Enum.GetName(typeof(KeyCode), i) != null ? Enum.GetName(typeof(KeyCode), i) : "Invalid Value";

                    hide.DisabledOptions.Add(Enum.GetName(typeof(KeyCode), i) == null);

                    keyCodeDropdownDD.options.Add(new Dropdown.OptionData(str));
                }

                var delete = EditorManager.inst.GetDialog("Keybind List Popup").Dialog.Find("Panel/x").gameObject.Duplicate(rectTransform, "Delete");
            }

            EditorHelper.AddEditorDialog("Keybind Editor", editorDialog.gameObject);

            EditorHelper.AddEditorDropdown("View Keybinds", "", "Edit", RTEditor.inst.SearchSprite, delegate ()
            {
                OpenPopup();
            });
        }

        public void OpenPopup()
        {
            EditorManager.inst.ShowDialog("Keybind List Popup");
            RefreshKeybindPopup();
        }

        public void RefreshKeybindPopup()
        {
            LSHelpers.DeleteChildren(content);

            var add = PrefabEditor.inst.CreatePrefab.Duplicate(content);
            add.transform.Find("Text").GetComponent<Text>().text = "Add new Keybind";
            var addButton = add.GetComponent<Button>();
            addButton.onClick.ClearAll();
            addButton.onClick.AddListener(delegate ()
            {
                var keybind = new Keybind(LSText.randomNumString(16), new List<Keybind.Key> { new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Alpha0) }, 1, Settings[1]);
                keybinds.Add(keybind);
                RefreshKeybindPopup();

                EditorManager.inst.ShowDialog("Keybind Editor");
                RefreshKeybindEditor(keybind);
            });

            int num = 0;
            foreach (var keybind in keybinds)
            {
                int index = num;
                var gameObject = EditorManager.inst.spriteFolderButtonPrefab.Duplicate(content, keybind.Name);
                var button = gameObject.transform.Find("Image").gameObject.AddComponent<Button>();
                button.onClick.AddListener(delegate ()
                {
                    EditorManager.inst.ShowDialog("Keybind Editor");
                    RefreshKeybindEditor(keybind);
                });

                var ed1 = new GameObject("Edit");
                ed1.transform.SetParent(gameObject.transform.Find("Image"));
                ed1.transform.localScale = Vector3.one;

                var rt = ed1.AddComponent<RectTransform>();
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(32f, 32f);

                var hover = gameObject.transform.Find("Image").gameObject.AddComponent<Components.HoverUI>();
                hover.animatePos = false;
                hover.animateSca = true;
                hover.size = 1.1f;

                var image = ed1.AddComponent<Image>();
                image.sprite = editSprite;
                image.color = Color.black;

                var name = keybind.Name;

                if (keybind.settings != null && keybind.settings.Count > 0)
                {
                    name += " (";
                    for (int i = 0; i < keybind.settings.Count; i++)
                    {
                        name += $"{keybind.settings.ElementAt(i).Key}: {keybind.settings.ElementAt(i).Value}";
                        if (i != keybind.settings.Count - 1)
                            name += ", ";
                    }
                    name += ")";
                }

                gameObject.transform.Find("folder-name").GetComponent<Text>().text = name;

                var delete = EditorManager.inst.GetDialog("Keybind List Popup").Dialog.Find("Panel/x").gameObject.Duplicate(gameObject.transform, "Delete").GetComponent<Button>();
                ((RectTransform)delete.transform).anchoredPosition = Vector2.zero;
                delete.onClick.ClearAll();
                delete.onClick.AddListener(delegate ()
                {
                    keybinds.RemoveAt(index);
                    RefreshKeybindPopup();
                    Save();
                });
                num++;
            }
        }

        public void RefreshKeybindEditor(Keybind keybind)
        {
            actionDropdown.onValueChanged.ClearAll();
            actionDropdown.value = keybind.ActionType;
            actionDropdown.onValueChanged.AddListener(delegate (int _val)
            {
                keybind.ActionType = _val;
                var settings = Settings;
                keybind.settings = settings[_val] == null ? new Dictionary<string, string>() : settings[_val];
                Save();
            });

            LSHelpers.DeleteChildren(keysContent);

            var add = PrefabEditor.inst.CreatePrefab.Duplicate(keysContent, "Add Key");
            add.transform.Find("Text").GetComponent<Text>().text = "Add new Key";
            ((RectTransform)add.transform).sizeDelta = new Vector2(760f, 32f);
            var addButton = add.GetComponent<Button>();
            addButton.onClick.ClearAll();
            addButton.onClick.AddListener(delegate ()
            {
                var key = new Keybind.Key(Keybind.Key.Type.Down, KeyCode.None);
                keybind.keys.Add(key);
                RefreshKeybindEditor(keybind);
                Save();
            });

            int num = 0;
            foreach (var key in keybind.keys)
            {
                int index = num;
                var gameObject = keyPrefab.Duplicate(keysContent, "Key");
                var type = gameObject.transform.Find("Key Type").GetComponent<Dropdown>();
                type.value = (int)key.InteractType;
                type.onValueChanged.AddListener(delegate (int _val)
                {
                    key.InteractType = (Keybind.Key.Type)_val;
                    Save();
                });

                var code = gameObject.transform.Find("Key Code").GetComponent<Dropdown>();

                code.value = (int)key.KeyCode;
                code.onValueChanged.AddListener(delegate (int _val)
                {
                    key.KeyCode = (KeyCode)_val;
                    Save();
                });

                var delete = gameObject.transform.Find("Delete").GetComponent<Button>();
                delete.onClick.ClearAll();
                delete.onClick.AddListener(delegate ()
                {
                    keybind.keys.RemoveAt(index);
                    RefreshKeybindEditor(keybind);
                    Save();
                });
                num++;
            }
        }

        #endregion

        #region Methods

        public static List<Action<Keybind>> KeybinderMethods { get; } = new List<Action<Keybind>>
        {
            CustomCode, // 0
            ToggleEditor, // 1
            UpdateEverything, // 2
            UpdateObject, // 3
            OpenPrefabDialog, // 4
            CollapsePrefab, // 5
            ExpandPrefab, // 6
            SetSongTimeAutokill, // 7
            OpenDialog, // 8
            SaveBeatmap, // 9
            OpenBeatmapPopup, // 10
            SetLayer, // 11
            ToggleEventLayer, // 12
            Undo, // 13
            Redo, // 14
            TogglePlayingSong, // 15
            IncreaseKeyframeValue, // 16
            DecreaseKeyframeValue, // 17
            SetKeyframeValue, // 18
            SwapLockSelection, // 19
            ToggleLockSelection, // 20
            SwapCollapseSelection, // 21
            ToggleCollapseSelection, // 22
            AddObjectLayer, // 23
            SubObjectLayer, // 24
            CycleObjectTypeUp, // 25
            CycleObjectTypeDown, // 26
            JumpToNextMarker, // 27
            JumpToPreviousMarker, // 28
            OpenSaveAs, // 29
            OpenNewLevel, // 30
            ToggleBPMSnap, // 31
            SelectNextObject, // 32
            SelectPreviousObject, // 33
            SetFirstKeyframeInType, // 34
            SetLastKeyframeInType, // 35
            SetNextKeyframeInType, // 36
            SetPreviousKeyframeInType, // 37
            AddPitch, // 38
            SubPitch, // 39
            ToggleShowHelp, // 40
            GoToCurrent, // 41
            GoToStart, // 42
            GoToEnd, // 43
            CreateNewMarker, // 44
            SpawnPrefab, // 45
            Cut, // 46
            Copy, // 47
            Paste, // 48
            Duplicate, // 49
            Delete, // 50
            ToggleObjectDragger, // 51
        };

        public static void CustomCode(Keybind keybind)
        {
            if (keybind.settings.ContainsKey("Code"))
                RTCode.Evaluate(keybind.DefaultCode + keybind.settings["Code"]);
        }

        public static void ToggleEditor(Keybind keybind) => EditorManager.inst.ToggleEditor();

        public static void UpdateEverything(Keybind keybind)
        {
            EventManager.inst.updateEvents();
            ObjectManager.inst.updateObjects();
        }

        public static void UpdateObject(Keybind keybind)
        {
            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
            {
                if (timelineObject.IsBeatmapObject)
                    Updater.UpdateProcessor(timelineObject.GetData<BeatmapObject>());
                if (timelineObject.IsPrefabObject)
                    Updater.UpdatePrefab(timelineObject.GetData<PrefabObject>());
            }
        }

        public static void OpenPrefabDialog(Keybind keybind)
        {
            PrefabEditor.inst.OpenDialog();
        }

        public static void CollapsePrefab(Keybind keybind)
        {
            if (ObjectEditor.inst.SelectedBeatmapObjects.Count == 1 &&
                ObjectEditor.inst.CurrentSelection.IsBeatmapObject &&
                !string.IsNullOrEmpty(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>().prefabInstanceID))
                PrefabEditor.inst.CollapseCurrentPrefab();
        }

        public static void ExpandPrefab(Keybind keybind)
        {
            if (ObjectEditor.inst.SelectedPrefabObjects.Count == 1 && ObjectEditor.inst.CurrentSelection && ObjectEditor.inst.CurrentSelection.Data != null && ObjectEditor.inst.CurrentSelection.IsPrefabObject)
                PrefabEditor.inst.ExpandCurrentPrefab();
        }

        public static void SetSongTimeAutokill(Keybind keybind)
        {
            foreach (var timelineObject in ObjectEditor.inst.SelectedBeatmapObjects)
            {
                var bm = timelineObject.GetData<BeatmapObject>();

                bm.autoKillType = AutoKillType.SongTime;
                bm.autoKillOffset = AudioManager.inst.CurrentAudioSource.time;
                bm.editorData.collapse = true;

                Updater.UpdateProcessor(bm);
                ObjectEditor.inst.RenderTimelineObject(timelineObject);
            }
        }

        public static void OpenDialog(Keybind keybind)
        {
            if (EditorManager.inst && keybind.settings.ContainsKey("Dialog") && EditorManager.inst.EditorDialogsDictionary.ContainsKey(keybind.settings["Popup"]))
            {
                EditorManager.inst.ShowDialog(keybind.settings["Dialog"]);
            }
        }

        public static void SaveBeatmap(Keybind keybind) => EditorManager.inst.SaveBeatmap();

        public static void OpenBeatmapPopup(Keybind keybind) => EditorManager.inst.OpenBeatmapPopup();

        public static void SetLayer(Keybind keybind)
        {
            if (keybind.settings.ContainsKey("Layer") && int.TryParse(keybind.settings["Layer"], out int num))
            {
                RTEditor.inst.SetLayer(num);
            }
        }

        public static void ToggleEventLayer(Keybind keybind)
            => RTEditor.inst.SetLayer(RTEditor.inst.layerType == RTEditor.LayerType.Objects ? RTEditor.LayerType.Events : RTEditor.LayerType.Objects);

        public static void Undo(Keybind keybind)
        {
            if (!RTEditor.inst.ienumRunning)
            {
                EditorManager.inst.DisplayNotification("Performing task, please wait...", 1f, EditorManager.NotificationType.Success);
                EditorManager.inst.Undo();
            }
            else
            {
                EditorManager.inst.DisplayNotification("Wait until current task is complete!", 1f, EditorManager.NotificationType.Warning);
            }
        }

        public static void Redo(Keybind keybind)
        {
            if (!RTEditor.inst.ienumRunning)
            {
                EditorManager.inst.DisplayNotification("Performing task, please wait...", 1f, EditorManager.NotificationType.Success);
                EditorManager.inst.Redo();
            }
            else
            {
                EditorManager.inst.DisplayNotification("Wait until current task is complete!", 1f, EditorManager.NotificationType.Warning);
            }
        }

        public static void TogglePlayingSong(Keybind keybind) => EditorManager.inst.TogglePlayingSong();

        public static void IncreaseKeyframeValue(Keybind keybind)
        {
            var type = 0;
            if (keybind.settings.ContainsKey("EventType") && int.TryParse(keybind.settings["EventType"], out type))
            {

            }

            var index = 0;
            if (keybind.settings.ContainsKey("EventIndex") && int.TryParse(keybind.settings["EventIndex"], out index))
            {

            }

            var value = 0;
            if (keybind.settings.ContainsKey("EventValue") && int.TryParse(keybind.settings["EventValue"], out value))
            {

            }
            
            var amount = 1f;
            if (keybind.settings.ContainsKey("EventAmount") && float.TryParse(keybind.settings["EventAmount"], out amount))
            {

            }

            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
            {
                if (timelineObject.IsBeatmapObject)
                {
                    var bm = timelineObject.GetData<BeatmapObject>();

                    type = Mathf.Clamp(type, 0, bm.events.Count - 1);
                    index = Mathf.Clamp(index, 0, bm.events[type].Count - 1);
                    value = Mathf.Clamp(value, 0, bm.events[type][index].eventValues.Length - 1);

                    var val = bm.events[type][index].eventValues[value];

                    if (type == 3 && val == 0)
                        val = Mathf.Clamp(val + amount, 0, GameManager.inst.LiveTheme.objectColors.Count - 1);
                    else
                        val += amount;

                    bm.events[type][index].eventValues[value] = val;

                    Updater.UpdateProcessor(bm, "Keyframes");
                }
                if (timelineObject.IsPrefabObject)
                {
                    var po = timelineObject.GetData<PrefabObject>();

                    type = Mathf.Clamp(type, 0, po.events.Count - 1);
                    value = Mathf.Clamp(value, 0, po.events[type].eventValues.Length - 1);

                    po.events[type].eventValues[value] += amount;

                    Updater.UpdatePrefab(po);
                }
            }
        }
        
        public static void DecreaseKeyframeValue(Keybind keybind)
        {
            var type = 0;
            if (keybind.settings.ContainsKey("EventType") && int.TryParse(keybind.settings["EventType"], out type))
            {

            }

            var index = 0;
            if (keybind.settings.ContainsKey("EventIndex") && int.TryParse(keybind.settings["EventIndex"], out index))
            {

            }

            var value = 0;
            if (keybind.settings.ContainsKey("EventValue") && int.TryParse(keybind.settings["EventValue"], out value))
            {

            }
            
            var amount = 1f;
            if (keybind.settings.ContainsKey("EventAmount") && float.TryParse(keybind.settings["EventAmount"], out amount))
            {

            }

            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
            {
                if (timelineObject.IsBeatmapObject)
                {
                    var bm = timelineObject.GetData<BeatmapObject>();

                    type = Mathf.Clamp(type, 0, bm.events.Count - 1);
                    index = Mathf.Clamp(index, 0, bm.events[type].Count - 1);
                    value = Mathf.Clamp(value, 0, bm.events[type][index].eventValues.Length - 1);

                    var val = bm.events[type][index].eventValues[value];

                    if (type == 3 && val == 0)
                        val = Mathf.Clamp(val - amount, 0, GameManager.inst.LiveTheme.objectColors.Count - 1);
                    else
                        val -= amount;

                    bm.events[type][index].eventValues[value] = val;

                    Updater.UpdateProcessor(bm, "Keyframes");
                }
                if (timelineObject.IsPrefabObject)
                {
                    var po = timelineObject.GetData<PrefabObject>();

                    type = Mathf.Clamp(type, 0, po.events.Count - 1);
                    value = Mathf.Clamp(value, 0, po.events[type].eventValues.Length - 1);

                    po.events[type].eventValues[value] -= amount;

                    Updater.UpdatePrefab(po);
                }
            }
        }
        
        public static void SetKeyframeValue(Keybind keybind)
        {
            var type = 0;
            if (keybind.settings.ContainsKey("EventType") && int.TryParse(keybind.settings["EventType"], out type))
            {

            }

            var index = 0;
            if (keybind.settings.ContainsKey("EventIndex") && int.TryParse(keybind.settings["EventIndex"], out index))
            {

            }

            var value = 0;
            if (keybind.settings.ContainsKey("EventValue") && int.TryParse(keybind.settings["EventValue"], out value))
            {

            }
            
            var amount = 1f;
            if (keybind.settings.ContainsKey("EventAmount") && float.TryParse(keybind.settings["EventAmount"], out amount))
            {

            }

            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
            {
                if (timelineObject.IsBeatmapObject)
                {
                    var bm = timelineObject.GetData<BeatmapObject>();

                    type = Mathf.Clamp(type, 0, bm.events.Count - 1);
                    index = Mathf.Clamp(index, 0, bm.events[type].Count - 1);
                    value = Mathf.Clamp(value, 0, bm.events[type][index].eventValues.Length - 1);

                    var val = bm.events[type][index].eventValues[value];

                    if (type == 3 && val == 0)
                        val = Mathf.Clamp(amount, 0, GameManager.inst.LiveTheme.objectColors.Count - 1);
                    else
                        val = amount;

                    bm.events[type][index].eventValues[value] = val;

                    Updater.UpdateProcessor(bm, "Keyframes");
                }
                if (timelineObject.IsPrefabObject)
                {
                    var po = timelineObject.GetData<PrefabObject>();

                    type = Mathf.Clamp(type, 0, po.events.Count - 1);
                    value = Mathf.Clamp(value, 0, po.events[type].eventValues.Length - 1);

                    po.events[type].eventValues[value] = amount;

                    Updater.UpdatePrefab(po);
                }
            }
        }

        public static void SwapLockSelection(Keybind keybind)
        {
            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
            {
                if (timelineObject.IsBeatmapObject)
                    timelineObject.GetData<BeatmapObject>().editorData.locked = !timelineObject.GetData<BeatmapObject>().editorData.locked;
                if (timelineObject.IsPrefabObject)
                    timelineObject.GetData<PrefabObject>().editorData.locked = !timelineObject.GetData<PrefabObject>().editorData.locked;

                ObjectEditor.inst.RenderTimelineObject(timelineObject);
            }
        }

        public static bool loggled = true;
        public static void ToggleLockSelection(Keybind keybind)
        {
            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
            {
                if (timelineObject.IsBeatmapObject)
                    timelineObject.GetData<BeatmapObject>().editorData.locked = loggled;
                if (timelineObject.IsPrefabObject)
                    timelineObject.GetData<PrefabObject>().editorData.locked = loggled;

                loggled = !loggled;

                ObjectEditor.inst.RenderTimelineObject(timelineObject);
            }
        }
        
        public static void SwapCollapseSelection(Keybind keybind)
        {
            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
            {
                if (timelineObject.IsBeatmapObject)
                    timelineObject.GetData<BeatmapObject>().editorData.collapse = !timelineObject.GetData<BeatmapObject>().editorData.collapse;
                if (timelineObject.IsPrefabObject)
                    timelineObject.GetData<PrefabObject>().editorData.collapse = !timelineObject.GetData<PrefabObject>().editorData.collapse;

                ObjectEditor.inst.RenderTimelineObject(timelineObject);
            }
        }

        public static bool coggled = true;
        public static void ToggleCollapseSelection(Keybind keybind)
        {
            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
            {
                if (timelineObject.IsBeatmapObject)
                    timelineObject.GetData<BeatmapObject>().editorData.collapse = coggled;
                if (timelineObject.IsPrefabObject)
                    timelineObject.GetData<PrefabObject>().editorData.collapse = coggled;

                coggled = !coggled;

                ObjectEditor.inst.RenderTimelineObject(timelineObject);
            }
        }

        public static void AddObjectLayer(Keybind keybind)
        {
            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
            {
                if (timelineObject.IsBeatmapObject)
                    timelineObject.GetData<BeatmapObject>().editorData.layer++;
                if (timelineObject.IsPrefabObject)
                    timelineObject.GetData<PrefabObject>().editorData.layer++;

                ObjectEditor.inst.RenderTimelineObject(timelineObject);
            }
        }
        
        public static void SubObjectLayer(Keybind keybind)
        {
            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
            {
                if (timelineObject.IsBeatmapObject && timelineObject.GetData<BeatmapObject>().editorData.layer > 0)
                    timelineObject.GetData<BeatmapObject>().editorData.layer--;
                if (timelineObject.IsPrefabObject && timelineObject.GetData<PrefabObject>().editorData.layer > 0)
                    timelineObject.GetData<PrefabObject>().editorData.layer--;

                ObjectEditor.inst.RenderTimelineObject(timelineObject);
            }
        }

        public static void CycleObjectTypeUp(Keybind keybind)
        {
            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
            {
                if (timelineObject.IsBeatmapObject)
                {
                    var bm = timelineObject.GetData<BeatmapObject>();

                    bm.objectType++;

                    if ((int)bm.objectType > Enum.GetNames(typeof(ObjectType)).Length)
                        bm.objectType = 0;

                    Updater.UpdateProcessor(bm);
                    ObjectEditor.inst.RenderTimelineObject(timelineObject);
                }
            }
        }
        
        public static void CycleObjectTypeDown(Keybind keybind)
        {
            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
            {
                if (timelineObject.IsBeatmapObject)
                {
                    var bm = timelineObject.GetData<BeatmapObject>();

                    var e = (int)bm.objectType - 1;

                    if (e < 0)
                        e = Enum.GetValues(bm.objectType.GetType()).Length - 1;

                    bm.objectType = (ObjectType)e;

                    Updater.UpdateProcessor(bm);
                    ObjectEditor.inst.RenderTimelineObject(timelineObject);
                }
            }
        }

        public static void JumpToNextMarker(Keybind keybind)
        {
            if (DataManager.inst.gameData.beatmapData.markers.Count > 0)
            {
                var list = (from x in DataManager.inst.gameData.beatmapData.markers
                                                                 orderby x.time
                                                                 select x).ToList();

                var currentMarker = list.FindIndex(x => x.time > AudioManager.inst.CurrentAudioSource.time + 0.005f);

                if (currentMarker >= 0)
                    SetCurrentMarker(Mathf.Clamp(currentMarker, 0, list.Count - 1), true, RTEditor.GetEditorProperty("Bring To Selection").GetConfigEntry<bool>().Value);
            }
        }
        
        public static void JumpToPreviousMarker(Keybind keybind)
        {
            if (DataManager.inst.gameData.beatmapData.markers.Count > 0)
            {
                var list = (from x in DataManager.inst.gameData.beatmapData.markers
                                                                 orderby x.time
                                                                 select x).ToList();

                var currentMarker = list.FindIndex(x => x.time > AudioManager.inst.CurrentAudioSource.time + 0.005f);

                if (currentMarker - 2 >= 0)
                    SetCurrentMarker(Mathf.Clamp(currentMarker - 2, 0, list.Count - 1), true, RTEditor.GetEditorProperty("Bring To Selection").GetConfigEntry<bool>().Value);
            }
        }

        public static void OpenSaveAs(Keybind keybind)
        {
            EditorManager.inst.ClearDialogs(new EditorManager.EditorDialog.DialogType[1]);
            EditorManager.inst.ShowDialog("Save As Popup");
        }
        
        public static void OpenNewLevel(Keybind keybind)
        {
            EditorManager.inst.ClearDialogs(new EditorManager.EditorDialog.DialogType[1]);
            EditorManager.inst.ShowDialog("New File Popup");
        }

        public static void ToggleBPMSnap(Keybind keybind)
        {
            SettingEditor.inst.SnapActive = !SettingEditor.inst.SnapActive;
            if (EditorManager.inst.GetDialog("Settings Editor").Dialog)
            {
                var dialog = EditorManager.inst.GetDialog("Settings Editor").Dialog;
                dialog.Find("snap/toggle/toggle").GetComponent<Toggle>().isOn = SettingEditor.inst.SnapActive;
            }
        }

        public static void SelectNextObject(Keybind keybind)
        {
            var currentSelection = ObjectEditor.inst.CurrentSelection;

            var index = RTEditor.inst.timelineObjects.IndexOf(currentSelection);

            if (index + 1 < RTEditor.inst.timelineObjects.Count)
            {
                ObjectEditor.inst.SetCurrentObject(RTEditor.inst.timelineObjects[index + 1], RTEditor.GetEditorProperty("Bring To Selection").GetConfigEntry<bool>().Value);
            }
        }
        
        public static void SelectPreviousObject(Keybind keybind)
        {
            var currentSelection = ObjectEditor.inst.CurrentSelection;

            var index = RTEditor.inst.timelineObjects.IndexOf(currentSelection);

            if (index - 1 >= 0)
            {
                ObjectEditor.inst.SetCurrentObject(RTEditor.inst.timelineObjects[index - 1], RTEditor.GetEditorProperty("Bring To Selection").GetConfigEntry<bool>().Value);
            }
        }

        public static void SetFirstKeyframeInType(Keybind keybind)
        {
            if (RTEditor.inst.layerType == RTEditor.LayerType.Objects)
            {
                if (ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
                {
                    var bm = ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>();
                    ObjectEditor.inst.UpdateKeyframeOrder(bm);
                    ObjectEditor.inst.SetCurrentKeyframe(bm, ObjEditor.inst.currentKeyframeKind, 0, true);
                }
            }
            if (RTEditor.inst.layerType == RTEditor.LayerType.Events)
            {
                EventEditor.inst.SetCurrentEvent(EventEditor.inst.currentEventType, 0);
                AudioManager.inst.SetMusicTime(DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventTime);
            }
        }
        
        public static void SetLastKeyframeInType(Keybind keybind)
        {
            if (RTEditor.inst.layerType == RTEditor.LayerType.Objects)
            {
                if (ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
                {
                    var bm = ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>();
                    ObjectEditor.inst.UpdateKeyframeOrder(bm);
                    ObjectEditor.inst.SetCurrentKeyframe(bm, ObjEditor.inst.currentKeyframeKind, bm.events[ObjEditor.inst.currentKeyframeKind].Count - 1, true);
                }
            }
            if (RTEditor.inst.layerType == RTEditor.LayerType.Events)
            {
                EventEditor.inst.SetCurrentEvent(EventEditor.inst.currentEventType, DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType].Count - 1);
                AudioManager.inst.SetMusicTime(DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventTime);
            }
        }
        
        public static void SetNextKeyframeInType(Keybind keybind)
        {
            if (RTEditor.inst.layerType == RTEditor.LayerType.Objects)
            {
                if (ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
                {
                    var bm = ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>();
                    ObjectEditor.inst.UpdateKeyframeOrder(bm);
                    ObjectEditor.inst.SetCurrentKeyframe(bm, ObjEditor.inst.currentKeyframeKind, Mathf.Clamp(ObjEditor.inst.currentKeyframe + 1, 0, bm.events[ObjEditor.inst.currentKeyframeKind].Count - 1), true);
                }
            }
            if (RTEditor.inst.layerType == RTEditor.LayerType.Events)
            {
                var allEvents = DataManager.inst.gameData.eventObjects.allEvents;
                int count = allEvents[EventEditor.inst.currentEventType].Count;
                int num = EventEditor.inst.currentEvent + 1 >= count ? count - 1 : EventEditor.inst.currentEvent + 1;

                EventEditor.inst.SetCurrentEvent(EventEditor.inst.currentEventType, num);
                AudioManager.inst.SetMusicTime(DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventTime);
            }
        }
        
        public static void SetPreviousKeyframeInType(Keybind keybind)
        {
            if (RTEditor.inst.layerType == RTEditor.LayerType.Objects)
            {
                if (ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
                {
                    var bm = ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>();
                    ObjectEditor.inst.UpdateKeyframeOrder(bm);
                    ObjectEditor.inst.SetCurrentKeyframe(bm, ObjEditor.inst.currentKeyframeKind, Mathf.Clamp(ObjEditor.inst.currentKeyframe - 1, 0, bm.events[ObjEditor.inst.currentKeyframeKind].Count - 1), true);
                }
            }
            if (RTEditor.inst.layerType == RTEditor.LayerType.Events)
            {
                int num = EventEditor.inst.currentEvent - 1 < 0 ? 0 : EventEditor.inst.currentEvent - 1;

                EventEditor.inst.SetCurrentEvent(EventEditor.inst.currentEventType, num);
                AudioManager.inst.SetMusicTime(DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].eventTime);
            }
        }

        public static void AddPitch(Keybind keybind)
        {
            if (ModCompatibility.sharedFunctions.ContainsKey("EventsCorePitchOffset"))
                AudioManager.inst.SetPitch((float)ModCompatibility.sharedFunctions["EventsCorePitchOffset"] + 0.1f);
            else
                AudioManager.inst.SetPitch(AudioManager.inst.CurrentAudioSource.pitch + 0.1f);
        }

        public static void SubPitch(Keybind keybind)
        {
            if (ModCompatibility.sharedFunctions.ContainsKey("EventsCorePitchOffset"))
                AudioManager.inst.SetPitch((float)ModCompatibility.sharedFunctions["EventsCorePitchOffset"] - 0.1f);
            else
                AudioManager.inst.SetPitch(AudioManager.inst.CurrentAudioSource.pitch - 0.1f);
        }

        public static void ToggleShowHelp(Keybind keybind) => EditorManager.inst.SetShowHelp(!EditorManager.inst.showHelp);

        public static void GoToCurrent(Keybind keybind)
        {
            if (!tlScrollbar && RTExtensions.TryFind("Editor Systems/Editor GUI/sizer/main/whole-timeline/Scrollbar", out GameObject gm) && gm.TryGetComponent(out Scrollbar sb))
                tlScrollbar = sb;

            if (tlScrollbar)
                tlScrollbar.value = AudioManager.inst.CurrentAudioSource.time / AudioManager.inst.CurrentAudioSource.clip.length;
        }
        
        public static void GoToStart(Keybind keybind)
        {
            if (!tlScrollbar && RTExtensions.TryFind("Editor Systems/Editor GUI/sizer/main/whole-timeline/Scrollbar", out GameObject gm) && gm.TryGetComponent(out Scrollbar sb))
                tlScrollbar = sb;

            if (tlScrollbar)
                tlScrollbar.value = 0f;
        }
        
        public static void GoToEnd(Keybind keybind)
        {
            if (!tlScrollbar && RTExtensions.TryFind("Editor Systems/Editor GUI/sizer/main/whole-timeline/Scrollbar", out GameObject gm) && gm.TryGetComponent(out Scrollbar sb))
                tlScrollbar = sb;

            if (tlScrollbar)
                tlScrollbar.value = 1f;
        }

        public static void CreateNewMarker(Keybind keybind) => MarkerEditor.inst.CreateNewMarker();

        public static void SpawnPrefab(Keybind keybind)
        {
            bool useExternal = keybind.settings.ContainsKey("External") && bool.TryParse(keybind.settings["External"], out useExternal);

            if (keybind.settings.ContainsKey("UseID") && bool.TryParse(keybind.settings["UseID"], out bool boolean) && keybind.settings.ContainsKey("ID"))
            {
                if (useExternal && !string.IsNullOrEmpty(keybind.settings["ID"]) && PrefabEditor.inst.LoadedPrefabs.Has(x => x.ID == keybind.settings["ID"]))
                {
                    PrefabEditor.inst.AddPrefabObjectToLevel(PrefabEditor.inst.LoadedPrefabs.Find(x => x.ID == keybind.settings["ID"]));
                }
                else if (!useExternal && DataManager.inst.gameData.prefabs.Has(x => x.ID == keybind.settings["ID"]))
                {
                    PrefabEditor.inst.AddPrefabObjectToLevel(DataManager.inst.gameData.prefabs.Find(x => x.ID == keybind.settings["ID"]));
                }
            }
            else if (keybind.settings.ContainsKey("Index") && int.TryParse(keybind.settings["Index"], out int index))
            {
                if (useExternal && index > 0 && index < PrefabEditor.inst.LoadedPrefabs.Count)
                {
                    PrefabEditor.inst.AddPrefabObjectToLevel(PrefabEditor.inst.LoadedPrefabs[index]);
                }
                else if (!useExternal && index > 0 && index < DataManager.inst.gameData.prefabs.Count)
                {
                    PrefabEditor.inst.AddPrefabObjectToLevel(DataManager.inst.gameData.prefabs[index]);
                }
            }
        }

        public static void Cut(Keybind keybind) => EditorManager.inst.Cut();

        public static void Copy(Keybind keybind) => EditorManager.inst.Copy();

        public static void Paste(Keybind keybind)
        {
            if (!RTEditor.inst.ienumRunning)
            {
                EditorManager.inst.DisplayNotification("Performing task, please wait...", 1f, EditorManager.NotificationType.Success);

                bool regen = true;

                if (keybind.settings.ContainsKey("Remove Prefab Instance ID") && bool.TryParse(keybind.settings["Remove Prefab Instance ID"], out bool result))
                    regen = result;

                RTEditor.inst.Paste(0f, regen);
            }
            else
            {
                EditorManager.inst.DisplayNotification("Wait until current task is complete!", 1f, EditorManager.NotificationType.Warning);
            }
        }

        public static void Duplicate(Keybind keybind)
        {
            if (!RTEditor.inst.ienumRunning)
            {
                EditorManager.inst.DisplayNotification("Performing task, please wait...", 1f, EditorManager.NotificationType.Success);

                bool regen = true;

                if (keybind.settings.ContainsKey("Remove Prefab Instance ID") && bool.TryParse(keybind.settings["Remove Prefab Instance ID"], out bool result))
                    regen = result;

                RTEditor.inst.Duplicate(regen);
            }
            else
            {
                EditorManager.inst.DisplayNotification("Wait until current task is complete!", 1f, EditorManager.NotificationType.Warning);
            }
        }

        public static void Delete(Keybind keybind)
        {
            if (!RTEditor.inst.ienumRunning)
            {
                EditorManager.inst.DisplayNotification("Performing task, please wait...", 1f, EditorManager.NotificationType.Success);
                RTEditor.inst.Delete();
            }
            else
            {
                EditorManager.inst.DisplayNotification("Wait until current task is complete!", 1f, EditorManager.NotificationType.Warning);
            }
        }

        public static void ToggleObjectDragger(Keybind keybind)
        {
            RTEditor.GetEditorProperty("Object Dragger Enabled").GetConfigEntry<bool>().Value = !RTEditor.GetEditorProperty("Object Dragger Enabled").GetConfigEntry<bool>().Value;
        }

        public static void DuplicateKeyframe(Keybind keybind)
        {

        }

        #endregion

        #region Settings

        public List<Dictionary<string, string>> Settings => new List<Dictionary<string, string>>
        {
            new Dictionary<string, string>
            {
                { "Code", "Debug.Log($\"{EditorManagement.Functions.Editors.KeybindManager.className} This is an example! You can use the keybind variable to check any settings you may have.\");" }
            }, // 0
            null, // 1
            null, // 2
            null, // 3
            null, // 4
            null, // 5
            null, // 6
            null, // 7
            new Dictionary<string, string>
            {
                { "Dialog", "Open File Popup" }
            }, // 8
            null, // 9
            null, // 10
            new Dictionary<string, string>
            {
                { "Layer", "0" }
            }, // 11
            null, // 12
            null, // 13
            null, // 14
            null, // 15
            new Dictionary<string, string>
            {
                { "EventType", "0" },
                { "EventIndex", "0" },
                { "EventValue", "0" },
                { "EventAmount", "0" },
            }, // 16
            new Dictionary<string, string>
            {
                { "EventType", "0" },
                { "EventIndex", "0" },
                { "EventValue", "0" },
                { "EventAmount", "0" },
            }, // 17
            new Dictionary<string, string>
            {
                { "EventType", "0" },
                { "EventIndex", "0" },
                { "EventValue", "0" },
                { "EventAmount", "0" },
            }, // 18
            null, // 19
            null, // 20
            null, // 21
            null, // 22
            null, // 23
            null, // 24
            null, // 25
            null, // 26
            null, // 27
            null, // 28
            null, // 29
            null, // 30
            null, // 31
            null, // 32
            null, // 33
            null, // 34
            null, // 35
            null, // 36
            null, // 37
            null, // 38
            null, // 39
            null, // 40
            null, // 41
            null, // 42
            null, // 43
            null, // 44
            new Dictionary<string, string>
            {
                { "External", "False" },
                { "UseID", "False" },
                { "ID", "" },
                { "Index", "0" }
            }, // 45
            null, // 46
            null, // 47
            new Dictionary<string, string>
            {
                { "Remove Prefab Instance ID", "True" }
            }, // 48
            new Dictionary<string, string>
            {
                { "Remove Prefab Instance ID", "True" }
            }, // 49
            null, // 50
        };

        #endregion

        #region Functions

        public static void SetCurrentMarker(int _marker, bool _bringTo = false, bool _showDialog = false)
        {
            DataManager.inst.UpdateSettingInt("EditorMarker", _marker);
            MarkerEditor.inst.currentMarker = _marker;

            if (_showDialog)
                MarkerEditor.inst.OpenDialog(_marker);

            if (_bringTo)
            {
                float time = DataManager.inst.gameData.beatmapData.markers[MarkerEditor.inst.currentMarker].time;
                AudioManager.inst.SetMusicTime(Mathf.Clamp(time, 0f, AudioManager.inst.CurrentAudioSource.clip.length));
                AudioManager.inst.CurrentAudioSource.Pause();
                EditorManager.inst.UpdatePlayButton();
            }
        }

        public static KeyCode WatchKeyCode()
        {
            var keyCodes = Enum.GetNames(typeof(KeyCode));
            for (int i = 0; i < keyCodes.Length; i++)
            {
                if (Input.GetKeyDown(keyCodes[i]))
                    return (KeyCode)Enum.Parse(typeof(KeyCode), keyCodes[i]);
            }

            return KeyCode.None;
        }

        #endregion

        public bool KeyCodeHandler(Keybind keybind)
        {
            if (keybind.keys.Count > 0 && keybind.keys.All(x => Input.GetKey(x.KeyCode) && x.InteractType == Keybind.Key.Type.Pressed ||
            Input.GetKeyDown(x.KeyCode) && x.InteractType == Keybind.Key.Type.Down ||
            !Input.GetKey(x.KeyCode) && x.InteractType == Keybind.Key.Type.Up) && !isPressingKey)
            {
                isPressingKey = true;
                return true;
            }
            else
            {
                isPressingKey = false;
                return false;
            }
        }

        public List<Keybind> keybinds = new List<Keybind>();

        public class Keybind
        {
            public Keybind(string id, List<Key> keys, int actionType, Dictionary<string, string> settings = null)
            {
                this.id = id;
                this.keys = keys;
                ActionType = actionType;
                this.settings = settings == null ? new Dictionary<string, string>() : settings;
            }

            public static Keybind Parse(JSONNode jn)
            {
                string id = jn["id"];

                int actionType = jn["action"].AsInt;

                var keys = new List<Key>();
                for (int i = 0; i < jn["keys"].Count; i++)
                    keys.Add(Key.Parse(jn["keys"][i]));

                var dictionary = new Dictionary<string, string>();

                for (int i = 0; i < jn["settings"].Count; i++)
                {
                    if (!dictionary.ContainsKey(jn["settings"][i]))
                        dictionary.Add(jn["settings"][i]["type"], jn["settings"][i]["value"]);
                }

                return new Keybind(id, keys, actionType, dictionary);
            }

            public JSONNode ToJSON()
            {
                var jn = JSON.Parse("{}");

                jn["id"] = id;
                jn["name"] = Name;

                jn["action"] = ActionType.ToString();
                for (int i = 0; i < keys.Count; i++)
                {
                    jn["keys"][i] = keys[i].ToJSON();
                }

                for (int i = 0; i < settings.Count; i++)
                {
                    var element = settings.ElementAt(i);
                    jn["settings"][i]["type"] = element.Key;
                    jn["settings"][i]["value"] = element.Value;
                }

                return jn;
            }

            public void Activate()
            {
                //if (keys.All(x => Input.GetKey(x.KeyCode) && x.InteractType == Key.Type.Pressed ||
                //Input.GetKeyDown(x.KeyCode) && x.InteractType == Key.Type.Down ||
                //Input.GetKeyUp(x.KeyCode) && x.InteractType == Key.Type.Up) && !inst.isPressingKey)
                //{
                //    Debug.Log($"{EditorPlugin.className}Pressed!");
                //    inst.isPressingKey = true;
                //    Action?.Invoke(this);
                //}
                //else
                //    inst.isPressingKey = false;

                if (inst.KeyCodeHandler(this))
                    Action?.Invoke(this);
            }

            public string id;
            public Dictionary<string, string> settings = new Dictionary<string, string>();
            public KeyCode KeyCode { get; set; }
            public KeyCode KeyCodeDown { get; set; }
            public int ActionType { get; set; }
            public Action<Keybind> Action
            {
                get
                {
                    if (ActionType < 0 || ActionType > KeybinderMethods.Count - 1)
                        return delegate (Keybind keybind)
                        {
                            Debug.LogError($"{className}No action assigned to key!");
                        };

                    return KeybinderMethods[ActionType];
                }
            }

            public string Name => ActionType >= 0 && ActionType < KeybinderMethods.Count ? KeybinderMethods[ActionType].Method.Name : "Invalid method";

            public bool watchingKeybind;

            public string DefaultCode => $"var keybind = EditorManagement.Functions.Editors.KeybindManager.inst.keybinds.Find(x => x.id == {id});{Environment.NewLine}";

            public List<Key> keys = new List<Key>();

            public override string ToString() => Name;

            public class Key
            {
                public Key(Type type, KeyCode keyCode)
                {
                    InteractType = type;
                    KeyCode = keyCode;
                }

                public enum Type
                {
                    Down,
                    Pressed,
                    Up
                }

                public Type InteractType { get; set; }
                public KeyCode KeyCode { get; set; }

                public static Key Parse(JSONNode jn) => new Key((Type)jn["type"].AsInt, (KeyCode)jn["key"].AsInt);

                public JSONNode ToJSON()
                {
                    var jn = JSON.Parse("{}");

                    jn["type"] = ((int)InteractType).ToString();
                    jn["key"] = ((int)KeyCode).ToString();

                    return jn;
                }
            }
        }
    }
}
