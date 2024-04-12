﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using SimpleJSON;
using LSFunctions;

using EditorManagement.Functions.Components;
using EditorManagement.Functions.Helpers;
using RTFunctions.Functions;
using RTFunctions.Functions.Components;
using RTFunctions.Functions.Data;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;
using RTFunctions.Functions.Optimization;

using BaseBeatmapObject = DataManager.GameData.BeatmapObject;
using BaseEventKeyframe = DataManager.GameData.EventKeyframe;
using BasePrefab = DataManager.GameData.Prefab;
using BasePrefabObject = DataManager.GameData.PrefabObject;
using BaseBackgroundObject = DataManager.GameData.BackgroundObject;

using ObjectType = RTFunctions.Functions.Data.BeatmapObject.ObjectType;
using AutoKillType = DataManager.GameData.BeatmapObject.AutoKillType;

using SelectionType = ObjEditor.ObjectSelection.SelectionType;
using BaseObjectSelection = ObjEditor.ObjectSelection;
using BaseObjectKeyframeSelection = ObjEditor.KeyframeSelection;
using EventKeyframeSelection = EventEditor.KeyframeSelection;

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

        public static bool AllowKeys { get; set; }

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
            if (!LSHelpers.IsUsingInputField() && !dragging && EditorManager.inst.isEditing && Application.isFocused &&
                (AllowKeys || !ModCompatibility.sharedFunctions.ContainsKey("EventsCoreEditorOffset") || !(bool)ModCompatibility.sharedFunctions["EventsCoreEditorOffset"]))
            {
                foreach (var keybind in keybinds)
                {
                    if (EditorManager.inst.editorState != EditorManager.EditorState.Main && keybind.Name != "ToggleProjectPlanner"
                        || !EditorManager.inst.hasLoadedLevel && keybind.Name != "ToggleShowHelp" && keybind.Name != "TogglePlayingSong" && keybind.Name != "OpenBeatmapPopup"
                        && keybind.Name != "SaveBeatmap" && keybind.Name != "ToggleProjectPlanner")
                        continue;

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

            if (Input.GetMouseButtonDown(0))
            {
                dragging = false;
            }
            
            if (Input.GetMouseButtonDown(1) && selectedKeyframe && originalValues != null && dragging)
            {
                dragging = false;
                selectedKeyframe.eventValues = originalValues.Copy();
                if (selectionType == SelectionType.Object)
                {
                    Updater.UpdateProcessor(beatmapObject, "Keyframes");
                    ObjectEditor.inst.RenderObjectKeyframesDialog(beatmapObject);
                }
                if (selectionType == SelectionType.Prefab)
                {
                    Updater.UpdatePrefab(prefabObject, "Offset");
                    PrefabEditorManager.inst.RenderPrefabObjectDialog(prefabObject, PrefabEditor.inst);
                }
            }

            UpdateValues();
        }

        public void FixedUpdate()
        {
            if (dragging)
            {
                if (selectionType == SelectionType.Object)
                {
                    ObjectEditor.inst.RenderObjectKeyframesDialog(beatmapObject);
                }
                if (selectionType == SelectionType.Prefab)
                {
                    PrefabEditorManager.inst.RenderPrefabObjectDialog(prefabObject, PrefabEditor.inst);
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
                new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl),
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Alpha1),
            }, 11, new Dictionary<string, string>
            {
                { "Layer", "0" }
            }));

            // Set Layer 2
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl),
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Alpha2),
            }, 11, new Dictionary<string, string>
            {
                { "Layer", "1" }
            }));

            // Set Layer 3
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl),
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Alpha3),
            }, 11, new Dictionary<string, string>
            {
                { "Layer", "2" }
            }));

            // Set Layer 4
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl),
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Alpha4),
            }, 11, new Dictionary<string, string>
            {
                { "Layer", "3" }
            }));

            // Set Layer 5
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl),
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Alpha5),
            }, 11, new Dictionary<string, string>
            {
                { "Layer", "4" }
            }));

            // Set Layer 6
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl),
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
            }, 57));
            
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
            
            // ToggleZenMode
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftAlt),
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Z),
            }, 52));

            // TransformPosition
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.G)
            }, 54, new Dictionary<string, string>
            {
                { "Create Keyframe", "True" },
                { "Use Nearest", "True" },
                { "Use Previous", "False" },
            }));
            
            // TransformScale
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Y)
            }, 55, new Dictionary<string, string>
            {
                { "Create Keyframe", "True" },
                { "Use Nearest", "True" },
                { "Use Previous", "False" },
            }));
            
            // TransformRotation
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.R),
                new Keybind.Key(Keybind.Key.Type.NotPressed, KeyCode.LeftControl),
            }, 56, new Dictionary<string, string>
            {
                { "Create Keyframe", "True" },
                { "Use Nearest", "True" },
                { "Use Previous", "False" },
            }));
            
            // ToggleProjectPlanner
            keybinds.Add(new Keybind(LSText.randomNumString(16), new List<Keybind.Key>
            {
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.F10),
            }, 59));

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
        public RectTransform settingsContent;

        public GameObject keyPrefab;
        public GameObject settingsPrefab;

        public string searchTerm = "";

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

            var popupTitle = popup.transform.Find("Panel/Text").GetComponent<Text>();
            popupTitle.text = "Edit a Keybind";

            var search = popup.transform.Find("search-box/search").GetComponent<InputField>();
            ((Text)search.placeholder).text = "Search for keybind...";
            search.onValueChanged.ClearAll();
            search.onValueChanged.AddListener(delegate (string _val)
            {
                searchTerm = _val;
                RefreshKeybindPopup();
            });

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

            var clickable = editorDialog.gameObject.AddComponent<Clickable>();
            clickable.onEnable = delegate (bool enabled)
            {
                if (!enabled)
                {
                    RTEditor.inst.selectingKey = false;
                    RTEditor.inst.setKey = null;

                    RTEditor.inst.onKeySet = null;
                }
            };

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

            // Keys list
            var keysScrollRect = new GameObject("ScrollRect");
            keysScrollRect.transform.SetParent(dataRT);
            keysScrollRect.transform.localScale = Vector3.one;
            var keysScrollRectRT = keysScrollRect.AddComponent<RectTransform>();
            keysScrollRectRT.anchoredPosition = new Vector2(0f, 16f);
            keysScrollRectRT.sizeDelta = new Vector2(400f, 250f);
            var keysScrollRectSR = keysScrollRect.AddComponent<ScrollRect>();
            keysScrollRectSR.horizontal = false;

            var keysMaskGO = new GameObject("Mask");
            keysMaskGO.transform.SetParent(keysScrollRectRT);
            keysMaskGO.transform.localScale = Vector3.one;
            var keysMaskRT = keysMaskGO.AddComponent<RectTransform>();
            keysMaskRT.anchoredPosition = new Vector2(0f, 0f);
            keysMaskRT.anchorMax = new Vector2(1f, 1f);
            keysMaskRT.anchorMin = new Vector2(0f, 0f);
            keysMaskRT.sizeDelta = new Vector2(0f, 0f);
            var keysMaskImage = keysMaskGO.AddComponent<Image>();
            keysMaskImage.color = new Color(1f, 1f, 1f, 0.04f);
            keysMaskGO.AddComponent<Mask>();

            var keysContentGO = new GameObject("Content");
            keysContentGO.transform.SetParent(keysMaskRT);
            keysContentGO.transform.localScale = Vector3.one;
            keysContent = keysContentGO.AddComponent<RectTransform>();

            keysContent.anchoredPosition = new Vector2(0f, -16f);
            keysContent.anchorMax = new Vector2(0f, 1f);
            keysContent.anchorMin = new Vector2(0f, 1f);
            keysContent.pivot = new Vector2(0f, 1f);
            keysContent.sizeDelta = new Vector2(400f, 250f);

            var keysContentCSF = keysContentGO.AddComponent<ContentSizeFitter>();
            keysContentCSF.horizontalFit = ContentSizeFitter.FitMode.MinSize;
            keysContentCSF.verticalFit = ContentSizeFitter.FitMode.MinSize;

            var keysContentVLG = keysContentGO.AddComponent<VerticalLayoutGroup>();
            keysContentVLG.childControlHeight = false;
            keysContentVLG.childForceExpandHeight = false;
            keysContentVLG.spacing = 4f;

            var keysContentLE = keysContentGO.AddComponent<LayoutElement>();
            keysContentLE.layoutPriority = 10000;
            keysContentLE.minWidth = 760;

            keysScrollRectSR.content = keysContent;

            // Settings list
            var settingsScrollRect = new GameObject("ScrollRect Settings");
            settingsScrollRect.transform.SetParent(dataRT);
            settingsScrollRect.transform.localScale = Vector3.one;
            var settingsScrollRectRT = settingsScrollRect.AddComponent<RectTransform>();
            settingsScrollRectRT.anchoredPosition = new Vector2(0f, 16f);
            settingsScrollRectRT.sizeDelta = new Vector2(400f, 250f);
            var settingsScrollRectSR = settingsScrollRect.AddComponent<ScrollRect>();

            var settingsMaskGO = new GameObject("Mask");
            settingsMaskGO.transform.SetParent(settingsScrollRectRT);
            settingsMaskGO.transform.localScale = Vector3.one;
            var settingsMaskRT = settingsMaskGO.AddComponent<RectTransform>();
            settingsMaskRT.anchoredPosition = new Vector2(0f, 0f);
            settingsMaskRT.anchorMax = new Vector2(1f, 1f);
            settingsMaskRT.anchorMin = new Vector2(0f, 0f);
            settingsMaskRT.sizeDelta = new Vector2(0f, 0f);
            var settingsMaskImage = settingsMaskGO.AddComponent<Image>();
            settingsMaskImage.color = new Color(1f, 1f, 1f, 0.04f);
            settingsMaskGO.AddComponent<Mask>();

            var settingsContentGO = new GameObject("Content");
            settingsContentGO.transform.SetParent(settingsMaskRT);
            settingsContentGO.transform.localScale = Vector3.one;
            settingsContent = settingsContentGO.AddComponent<RectTransform>();

            settingsContent.anchoredPosition = new Vector2(0f, -16f);
            settingsContent.anchorMax = new Vector2(0f, 1f);
            settingsContent.anchorMin = new Vector2(0f, 1f);
            settingsContent.pivot = new Vector2(0f, 1f);
            settingsContent.sizeDelta = new Vector2(400f, 250f);

            var settingsContentCSF = settingsContentGO.AddComponent<ContentSizeFitter>();
            settingsContentCSF.horizontalFit = ContentSizeFitter.FitMode.MinSize;
            settingsContentCSF.verticalFit = ContentSizeFitter.FitMode.MinSize;

            var settingsContentVLG = settingsContentGO.AddComponent<VerticalLayoutGroup>();
            settingsContentVLG.childControlHeight = false;
            settingsContentVLG.childForceExpandHeight = false;
            settingsContentVLG.spacing = 4f;

            var settingsContentLE = settingsContentGO.AddComponent<LayoutElement>();
            settingsContentLE.layoutPriority = 10000;
            settingsContentLE.minWidth = 760;

            settingsScrollRectSR.content = settingsContent;

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

                ((RectTransform)keyTypeDropdown.transform).sizeDelta = new Vector2(220f, 32f);

                var keyTypeDropdownDD = keyTypeDropdown.GetComponent<Dropdown>();
                keyTypeDropdownDD.onValueChanged.ClearAll();
                keyTypeDropdownDD.options = Enum.GetNames(typeof(Keybind.Key.Type)).Select(x => new Dropdown.OptionData(x)).ToList();
                keyTypeDropdownDD.value = 0;

                var watchKey = EditorPrefabHolder.Instance.Function1Button.Duplicate(rectTransform, "Key Watcher");
                var text = watchKey.transform.GetChild(0).GetComponent<Text>();
                text.text = "Set Key";
                watchKey.transform.AsRT().sizeDelta = new Vector2(140f, 32f);

                var keyCodeDropdown = EditorManager.inst.GetDialog("Object Editor").Dialog.Find("data/left/Scroll View/Viewport/Content/autokill/tod-dropdown").gameObject
                .Duplicate(rectTransform, "Key Code");

                ((RectTransform)keyCodeDropdown.transform).sizeDelta = new Vector2(360f, 32f);

                var keyCodeDropdownDD = keyCodeDropdown.GetComponent<Dropdown>();
                keyCodeDropdownDD.onValueChanged.ClearAll();
                keyCodeDropdownDD.value = 0;
                keyCodeDropdownDD.options.Clear();

                var hide = keyCodeDropdown.GetComponent<HideDropdownOptions>();
                hide.DisabledOptions.Clear();
                var keyCodes = Enum.GetValues(typeof(KeyCode));
                for (int i = 0; i < keyCodes.Length; i++)
                {
                    var str = Enum.GetName(typeof(KeyCode), i) ?? "Invalid Value";

                    hide.DisabledOptions.Add(string.IsNullOrEmpty(Enum.GetName(typeof(KeyCode), i)));

                    keyCodeDropdownDD.options.Add(new Dropdown.OptionData(str));
                }

                var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(rectTransform, "Delete");
                delete.transform.AsRT().anchoredPosition = new Vector2(744f, -16f);
            }

            EditorHelper.AddEditorDialog("Keybind Editor", editorDialog.gameObject);

            EditorHelper.AddEditorDropdown("View Keybinds", "", "Edit", RTEditor.inst.SearchSprite, delegate ()
            {
                OpenPopup();
            });

            // Editor Themes
            {
                EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Background_1, popup, new List<Component>
                {
                    popup.GetComponent<Image>(),
                }, true, 1, SpriteManager.RoundedSide.W));

                var panel = popup.transform.Find("Panel").gameObject;
                EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Background_1, panel, new List<Component>
                {
                    panel.GetComponent<Image>(),
                }, true, 1, SpriteManager.RoundedSide.Top));

                EditorThemeManager.AddSelectable(panel.transform.Find("x").GetComponent<Button>(), ThemeGroup.Close);

                var parentSelectorPopupCloseX = panel.transform.Find("x").GetChild(0).gameObject;
                EditorThemeManager.AddSelectable(close, ThemeGroup.Close);

                EditorThemeManager.AddLightText(popupTitle);

                var scrollbar = popup.transform.Find("Scrollbar").gameObject;
                EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Background_1, scrollbar, new List<Component>
                {
                    scrollbar.GetComponent<Image>(),
                }, true, 1, SpriteManager.RoundedSide.Bottom_Right_I));

                var scrollbarHandle = scrollbar.transform.Find("Sliding Area/Handle").gameObject;
                EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Scrollbar_1_Handle, scrollbarHandle, new List<Component>
                {
                    scrollbarHandle.GetComponent<Image>(),
                    scrollbar.GetComponent<Scrollbar>()
                }, true, 1, SpriteManager.RoundedSide.W, true));

                EditorThemeManager.AddInputField(search, ThemeGroup.Search_Field_1, 1, SpriteManager.RoundedSide.Bottom);

                EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Background_1, editorDialog.gameObject, new List<Component>
                {
                    editorDialog.GetComponent<Image>(),
                }));

                EditorThemeManager.AddLightText(editorDialog.Find("data/action/title").GetComponent<Text>());

                EditorThemeManager.AddDropdown(this.actionDropdown, "");
            }
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
            var addText = add.transform.Find("Text").GetComponent<Text>();
            addText.text = "Add new Keybind";
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

            EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Add, add, new List<Component>
            {
                addButton.image,
            }, true, 1, SpriteManager.RoundedSide.W));

            EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Add_Text, addText.gameObject, new List<Component>
            {
                addText,
            }));

            int num = 0;
            foreach (var keybind in keybinds)
            {
                int index = num;

                var name = keybind.Name;

                if (string.IsNullOrEmpty(searchTerm) || name.ToLower().Contains(searchTerm.ToLower()))
                {
                    var gameObject = EditorManager.inst.spriteFolderButtonPrefab.Duplicate(content, name);

                    EditorThemeManager.ApplySelectable(gameObject.GetComponent<Button>(), ThemeGroup.List_Button_1);

                    var button = gameObject.transform.Find("Image").gameObject.AddComponent<Button>();
                    button.onClick.AddListener(delegate ()
                    {
                        EditorManager.inst.ShowDialog("Keybind Editor");
                        RefreshKeybindEditor(keybind);
                    });

                    EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Null, button.gameObject, new List<Component>
                    {
                        button.image
                    }, true, 1, SpriteManager.RoundedSide.W));

                    var ed1 = new GameObject("Edit");
                    ed1.transform.SetParent(gameObject.transform.Find("Image"));
                    ed1.transform.localScale = Vector3.one;

                    var rt = ed1.AddComponent<RectTransform>();
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = new Vector2(32f, 32f);

                    var hover = gameObject.transform.Find("Image").gameObject.AddComponent<HoverUI>();
                    hover.animatePos = false;
                    hover.animateSca = true;
                    hover.size = 1.1f;

                    var image = ed1.AddComponent<Image>();
                    image.sprite = editSprite;
                    image.color = Color.black;

                    if (keybind.keys != null && keybind.keys.Count > 0)
                    {
                        name += " [";
                        for (int i = 0; i < keybind.keys.Count; i++)
                        {
                            name += $"{keybind.keys[i].InteractType}: {keybind.keys[i].KeyCode}";
                            if (i != keybind.keys.Count - 1)
                                name += ", ";
                        }
                        name += "]";
                    }

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

                    var nameText = gameObject.transform.Find("folder-name").GetComponent<Text>();
                    nameText.text = name;

                    EditorThemeManager.ApplyLightText(nameText);

                    var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(gameObject.transform, "Delete").GetComponent<DeleteButtonStorage>();
                    UIManager.SetRectTransform(delete.transform.AsRT(), new Vector2(580f, 0f), new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.one, new Vector2(32f, 32f));
                    delete.button.onClick.ClearAll();
                    delete.button.onClick.AddListener(delegate ()
                    {
                        RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this keybind? You cannot undo this.", delegate ()
                        {
                            keybinds.RemoveAt(index);
                            RefreshKeybindPopup();
                            Save();
                            EditorManager.inst.HideDialog("Warning Popup");
                        }, delegate ()
                        {
                            EditorManager.inst.HideDialog("Warning Popup");
                        });
                    });

                    EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Delete, delete.gameObject, new List<Component>
                    {
                        delete.baseImage,
                    }, true, 1, SpriteManager.RoundedSide.W));

                    EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Delete_Text, delete.image.gameObject, new List<Component>
                    {
                        delete.image,
                    }));
                }

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
                keybind.settings = settings[_val] ?? new Dictionary<string, string>();
                RefreshKeybindPopup();
                RefreshKeybindEditor(keybind);
                Save();
            });

            LSHelpers.DeleteChildren(keysContent);

            var add = PrefabEditor.inst.CreatePrefab.Duplicate(keysContent, "Add Key");
            var addText = add.transform.Find("Text").GetComponent<Text>();
            addText.text = "Add new Key";
            ((RectTransform)add.transform).sizeDelta = new Vector2(760f, 32f);
            var addButton = add.GetComponent<Button>();
            addButton.onClick.ClearAll();
            addButton.onClick.AddListener(delegate ()
            {
                var key = new Keybind.Key(Keybind.Key.Type.Down, KeyCode.None);
                keybind.keys.Add(key);
                RefreshKeybindPopup();
                RefreshKeybindEditor(keybind);
                Save();
            });

            EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Add, add, new List<Component>
            {
                addButton.image,
            }, true, 1, SpriteManager.RoundedSide.W));

            EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Add_Text, addText.gameObject, new List<Component>
            {
                addText,
            }));

            int num = 0;
            foreach (var key in keybind.keys)
            {
                int index = num;
                var gameObject = keyPrefab.Duplicate(keysContent, "Key");
                var type = gameObject.transform.Find("Key Type").GetComponent<Dropdown>();
                var watch = gameObject.transform.Find("Key Watcher").GetComponent<FunctionButtonStorage>();
                var code = gameObject.transform.Find("Key Code").GetComponent<Dropdown>();

                EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.List_Button_1_Normal, gameObject, new List<Component>
                {
                    gameObject.GetComponent<Image>(),
                }, true, 1, SpriteManager.RoundedSide.W));

                var text = gameObject.transform.Find("Key Watcher").GetChild(0).GetComponent<Text>();
                text.text = "Set Key";

                type.onValueChanged.ClearAll();
                type.value = (int)key.InteractType;
                type.onValueChanged.AddListener(delegate (int _val)
                {
                    key.InteractType = (Keybind.Key.Type)_val;
                    RefreshKeybindPopup();
                    Save();
                    text.text = "Set Key";
                });

                EditorThemeManager.ApplyDropdown(type);

                watch.button.onClick.ClearAll();
                watch.button.onClick.AddListener(delegate ()
                {
                    RTEditor.inst.selectingKey = true;
                    RTEditor.inst.setKey = delegate (KeyCode keyCode)
                    {
                        key.KeyCode = keyCode;
                        RefreshKeybindPopup();
                        Save();
                        text.text = "Set Key";
                    };

                    RTEditor.inst.onKeySet = delegate()
                    {
                        code.onValueChanged.ClearAll();
                        code.value = (int)key.KeyCode;
                        code.onValueChanged.AddListener(delegate (int _val)
                        {
                            key.KeyCode = (KeyCode)_val;
                            RefreshKeybindPopup();
                            Save();
                            text.text = "Set Key";
                        });
                    };

                    text.text = "Watching Key";
                });

                EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Function_1, watch.gameObject, new List<Component>
                {
                    watch.button.image,
                }, true, 1, SpriteManager.RoundedSide.W));

                EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Function_1_Text, watch.text.gameObject, new List<Component>
                {
                    watch.text,
                }));

                code.onValueChanged.ClearAll();
                code.value = (int)key.KeyCode;
                code.onValueChanged.AddListener(delegate (int _val)
                {
                    key.KeyCode = (KeyCode)_val;
                    RefreshKeybindPopup();
                    Save();
                    text.text = "Set Key";
                });

                EditorThemeManager.ApplyDropdown(code);

                var delete = gameObject.transform.Find("Delete").GetComponent<DeleteButtonStorage>();
                delete.button.onClick.ClearAll();
                delete.button.onClick.AddListener(delegate ()
                {
                    keybind.keys.RemoveAt(index);
                    RefreshKeybindPopup();
                    RefreshKeybindEditor(keybind);
                    Save();
                });

                EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Delete, delete.gameObject, new List<Component>
                {
                    delete.button.image,
                }, true, 1, SpriteManager.RoundedSide.W));

                EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Delete_Text, delete.image.gameObject, new List<Component>
                {
                    delete.image,
                }));

                num++;
            }

            LSHelpers.DeleteChildren(settingsContent);

            var label = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content").transform.GetChild(3).gameObject;
            var singleInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move/position/x");
            var vector2Input = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move/position");
            var boolInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/toggle/toggle");
            var dropdownInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/autokill/tod-dropdown");
            var sliderFullInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/bpm");
            var stringInput = RTEditor.inst.defaultIF;

            num = 0;
            foreach (var setting in keybind.settings)
            {
                int index = num;

                var key = setting.Key;
                switch (key.ToLower())
                {
                    case "external":
                    case "useid":
                    case "remove prefab instance id":
                    case "create keyframe":
                    case "use nearest":
                    case "use previous":
                        {
                            var bar = Instantiate(singleInput);
                            Destroy(bar.GetComponent<InputField>());
                            Destroy(bar.GetComponent<EventInfo>());
                            Destroy(bar.GetComponent<EventTrigger>());

                            LSHelpers.DeleteChildren(bar.transform);
                            bar.transform.SetParent(settingsContent);
                            bar.transform.localScale = Vector3.one;
                            bar.name = "input [BOOL]";

                            TooltipHelper.AddTooltip(bar, setting.Key, "");

                            var l = label.Duplicate(bar.transform, "", 0);
                            var labelText = l.transform.GetChild(0).GetComponent<Text>();
                            labelText.text = setting.Key;
                            l.transform.AsRT().sizeDelta = new Vector2(688f, 32f);

                            l.transform.GetChild(0).AsRT().anchoredPosition = new Vector2(10f, -5f);

                            var image = bar.GetComponent<Image>();
                            image.enabled = true;
                            image.color = new Color(1f, 1f, 1f, 0.03f);

                            var x = Instantiate(boolInput);
                            x.transform.SetParent(bar.transform);
                            x.transform.localScale = Vector3.one;

                            var xt = x.GetComponent<Toggle>();
                            xt.onValueChanged.RemoveAllListeners();
                            xt.isOn = Parser.TryParse(setting.Value, false);
                            xt.onValueChanged.AddListener(delegate (bool _val)
                            {
                                keybind.settings[setting.Key] = _val.ToString();
                                Save();
                            });

                            EditorThemeManager.ApplyToggle(xt, "");

                            break;
                        }
                    case "dialog":
                    case "id":
                    case "code":
                        {
                            var bar = Instantiate(singleInput);

                            Destroy(bar.GetComponent<EventInfo>());
                            Destroy(bar.GetComponent<EventTrigger>());
                            Destroy(bar.GetComponent<InputField>());
                            Destroy(bar.GetComponent<InputFieldSwapper>());

                            LSHelpers.DeleteChildren(bar.transform);
                            bar.transform.SetParent(settingsContent);
                            bar.transform.localScale = Vector3.one;
                            bar.name = "input [STRING]";

                            TooltipHelper.AddTooltip(bar, setting.Key, "");

                            var l = label.Duplicate(bar.transform, "", 0);
                            var labelText = l.transform.GetChild(0).GetComponent<Text>();
                            labelText.text = setting.Key;
                            l.transform.AsRT().sizeDelta = new Vector2(354f, 20f);

                            l.transform.GetChild(0).AsRT().anchoredPosition = new Vector2(10f, -5f);

                            var image = bar.GetComponent<Image>();
                            image.enabled = true;
                            image.color = new Color(1f, 1f, 1f, 0.03f);

                            var x = Instantiate(stringInput);
                            x.transform.SetParent(bar.transform);
                            x.transform.localScale = Vector3.one;
                            Destroy(x.GetComponent<HoverTooltip>());

                            x.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(366f, 32f);

                            var xif = x.GetComponent<InputField>();
                            xif.onValueChanged.RemoveAllListeners();
                            xif.onEndEdit.ClearAll();
                            xif.characterValidation = InputField.CharacterValidation.None;
                            xif.characterLimit = 0;
                            xif.text = setting.Value;
                            xif.textComponent.fontSize = 18;
                            xif.onValueChanged.AddListener(delegate (string _val)
                            {
                                keybind.settings[setting.Key] = _val;
                            });
                            xif.onEndEdit.AddListener(delegate (string _val)
                            {
                                Save();
                            });

                            EditorThemeManager.ApplyInputField(xif, ThemeGroup.Input_Field);

                            break;
                        }
                    case "eventtype":
                    case "eventindex":
                    case "eventvalue":
                    case "layer":
                    case "index":
                        {
                            var x = singleInput.Duplicate(settingsContent, "input [INT]");

                            Destroy(x.GetComponent<EventInfo>());
                            Destroy(x.GetComponent<EventTrigger>());
                            Destroy(x.GetComponent<InputField>());

                            x.transform.localScale = Vector3.one;
                            x.transform.GetChild(0).localScale = Vector3.one;

                            var l = label.Duplicate(x.transform, "", 0);
                            var labelText = l.transform.GetChild(0).GetComponent<Text>();
                            labelText.text = setting.Key;
                            l.transform.AsRT().sizeDelta = new Vector2(541f, 32f);
                            
                            l.transform.GetChild(0).AsRT().anchoredPosition = new Vector2(10f, -5f);

                            var image = x.GetComponent<Image>();
                            image.enabled = true;
                            image.color = new Color(1f, 1f, 1f, 0.03f);

                            TooltipHelper.AddTooltip(x, setting.Key, "");

                            var input = x.transform.Find("input");

                            var xif = input.gameObject.AddComponent<InputField>();
                            xif.onValueChanged.RemoveAllListeners();
                            xif.onEndEdit.ClearAll();
                            xif.textComponent = input.Find("Text").GetComponent<Text>();
                            xif.placeholder = input.Find("Placeholder").GetComponent<Text>();
                            xif.characterValidation = InputField.CharacterValidation.None;
                            xif.text = Parser.TryParse(setting.Value, 0).ToString();
                            xif.onValueChanged.AddListener(delegate (string _val)
                            {
                                if (int.TryParse(_val, out int result) && keybind.settings.ContainsKey(setting.Key))
                                {
                                    keybind.settings[setting.Key] = result.ToString();
                                }
                            });
                            xif.onEndEdit.AddListener(delegate (string _val)
                            {
                                Save();
                            });

                            TriggerHelper.AddEventTrigger(xif.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDeltaInt(xif) });

                            TriggerHelper.IncreaseDecreaseButtonsInt(xif, t: x.transform);

                            EditorThemeManager.ApplyInputField(xif, ThemeGroup.Input_Field);

                            Destroy(x.transform.Find("<").GetComponent<Animator>());
                            EditorThemeManager.ApplySelectable(x.transform.Find("<").GetComponent<Button>(), ThemeGroup.Function_2, false);
                            Destroy(x.transform.Find(">").GetComponent<Animator>());
                            EditorThemeManager.ApplySelectable(x.transform.Find(">").GetComponent<Button>(), ThemeGroup.Function_2, false);

                            break;
                        }
                    case "eventamount":
                        {
                            var x = singleInput.Duplicate(settingsContent, "input [FLOAT]");

                            Destroy(x.GetComponent<EventInfo>());
                            Destroy(x.GetComponent<EventTrigger>());
                            Destroy(x.GetComponent<InputField>());

                            x.transform.localScale = Vector3.one;
                            x.transform.GetChild(0).localScale = Vector3.one;

                            var l = label.Duplicate(x.transform, "", 0);
                            var labelText = l.transform.GetChild(0).GetComponent<Text>();
                            labelText.text = setting.Key;
                            l.transform.AsRT().sizeDelta = new Vector2(541f, 20f);

                            l.transform.GetChild(0).AsRT().anchoredPosition = new Vector2(10f, -5f);

                            var image = x.GetComponent<Image>();
                            image.enabled = true;
                            image.color = new Color(1f, 1f, 1f, 0.03f);

                            TooltipHelper.AddTooltip(x, setting.Key, "");

                            var input = x.transform.Find("input");

                            var xif = input.gameObject.AddComponent<InputField>();
                            xif.onValueChanged.RemoveAllListeners();
                            xif.onEndEdit.ClearAll();
                            xif.textComponent = input.Find("Text").GetComponent<Text>();
                            xif.placeholder = input.Find("Placeholder").GetComponent<Text>();
                            xif.characterValidation = InputField.CharacterValidation.None;
                            xif.text = Parser.TryParse(setting.Value, 0f).ToString();
                            xif.onValueChanged.AddListener(delegate (string _val)
                            {
                                if (float.TryParse(_val, out float result) && keybind.settings.ContainsKey(setting.Key))
                                {
                                    keybind.settings[setting.Key] = result.ToString();
                                }
                            });
                            xif.onEndEdit.AddListener(delegate (string _val)
                            {
                                Save();
                            });

                            TriggerHelper.AddEventTrigger(xif.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(xif) });

                            TriggerHelper.IncreaseDecreaseButtons(xif, t: x.transform);

                            EditorThemeManager.ApplyInputField(xif, ThemeGroup.Input_Field);

                            Destroy(x.transform.Find("<").GetComponent<Animator>());
                            EditorThemeManager.ApplySelectable(x.transform.Find("<").GetComponent<Button>(), ThemeGroup.Function_2, false);
                            Destroy(x.transform.Find(">").GetComponent<Animator>());
                            EditorThemeManager.ApplySelectable(x.transform.Find(">").GetComponent<Button>(), ThemeGroup.Function_2, false);

                            break;
                        }
                }

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
            ToggleZenMode, // 52
            CycleGameMode, // 53
            TransformPosition, //54
            TransformScale, //55
            TransformRotation, //56
            SpawnSelectedQuickPrefab, //57
            ResetIntegerVariables, // 58
            ToggleProjectPlanner, // 59
            ForceSnapBPM, // 60
            AddLayer, // 61
            ParentPicker, // 62
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

                ObjectEditor.inst.RenderTimelineObject(timelineObject);
            }

            loggled = !loggled;
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

                ObjectEditor.inst.RenderTimelineObject(timelineObject);
            }

            coggled = !coggled;
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
                RTEditor.inst.OrderMarkers();

                var currentMarker = DataManager.inst.gameData.beatmapData.markers.FindLastIndex(x => x.time <= AudioManager.inst.CurrentAudioSource.time - 0.005f);

                if (currentMarker + 2 >= 0)
                    SetCurrentMarker(Mathf.Clamp(currentMarker + 2, 0, DataManager.inst.gameData.beatmapData.markers.Count - 1), true, EditorConfig.Instance.BringToSelection.Value);
            }
        }
        
        public static void JumpToPreviousMarker(Keybind keybind)
        {
            if (DataManager.inst.gameData.beatmapData.markers.Count > 0)
            {
                RTEditor.inst.OrderMarkers();

                var currentMarker = DataManager.inst.gameData.beatmapData.markers.FindLastIndex(x => x.time < AudioManager.inst.CurrentAudioSource.time - 0.005f);

                if (currentMarker >= 0)
                    SetCurrentMarker(Mathf.Clamp(currentMarker, 0, DataManager.inst.gameData.beatmapData.markers.Count - 1), true, EditorConfig.Instance.BringToSelection.Value);
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
                ObjectEditor.inst.SetCurrentObject(RTEditor.inst.timelineObjects[index + 1], EditorConfig.Instance.BringToSelection.Value);
            }
        }
        
        public static void SelectPreviousObject(Keybind keybind)
        {
            var currentSelection = ObjectEditor.inst.CurrentSelection;

            var index = RTEditor.inst.timelineObjects.IndexOf(currentSelection);

            if (index - 1 >= 0)
            {
                ObjectEditor.inst.SetCurrentObject(RTEditor.inst.timelineObjects[index - 1], EditorConfig.Instance.BringToSelection.Value);
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

            if (keybind.settings.ContainsKey("UseID") && bool.TryParse(keybind.settings["UseID"], out bool boolean) && keybind.settings.ContainsKey("ID") && boolean)
            {
                if (useExternal && !string.IsNullOrEmpty(keybind.settings["ID"]) && PrefabEditorManager.inst.PrefabPanels.Has(x => x.Prefab.ID == keybind.settings["ID"]))
                {
                    PrefabEditorManager.inst.AddPrefabObjectToLevel(PrefabEditorManager.inst.PrefabPanels.Find(x => x.Prefab.ID == keybind.settings["ID"]).Prefab);
                }
                else if (!useExternal && DataManager.inst.gameData.prefabs.Has(x => x.ID == keybind.settings["ID"]))
                {
                    PrefabEditorManager.inst.AddPrefabObjectToLevel(DataManager.inst.gameData.prefabs.Find(x => x.ID == keybind.settings["ID"]));
                }
            }
            else if (keybind.settings.ContainsKey("Index") && int.TryParse(keybind.settings["Index"], out int index))
            {
                if (useExternal && index >= 0 && index < PrefabEditorManager.inst.PrefabPanels.Count && PrefabEditorManager.inst.PrefabPanels[index] != null)
                {
                    PrefabEditorManager.inst.AddPrefabObjectToLevel(PrefabEditorManager.inst.PrefabPanels[index].Prefab);
                }
                else if (!useExternal && index >= 0 && index < DataManager.inst.gameData.prefabs.Count && DataManager.inst.gameData.prefabs[index] != null)
                {
                    PrefabEditorManager.inst.AddPrefabObjectToLevel(DataManager.inst.gameData.prefabs[index]);
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

        public static void Delete(Keybind keybind) => RTEditor.inst.Delete();

        public static void ToggleObjectDragger(Keybind keybind)
        {
            EditorConfig.Instance.ObjectDraggerEnabled.Value = !EditorConfig.Instance.ObjectDraggerEnabled.Value;
        }

        public static void ToggleZenMode(Keybind keybind)
        {
            var config = EditorConfig.Instance.EditorZenMode;
            config.Value = !config.Value;
            EditorManager.inst.DisplayNotification($"Set Zen Mode {(config.Value ? "On" : "Off")}", 2f, EditorManager.NotificationType.Success);
        }

        public static void CycleGameMode(Keybind keybind)
        {
            var num = DataManager.inst.GetSettingEnum("ArcadeDifficulty", 1);
            num++;
            if (num > 4)
                num = 0;
            DataManager.inst.UpdateSettingEnum("ArcadeDifficulty", num);

            string[] modes = new string[]
            {
                "Zen",
                "Normal",
                "1 Life",
                "1 Hit",
                "Practice",
            };

            EditorManager.inst.DisplayNotification($"Set Game Mode to {modes[num]} Mode!", 2f, EditorManager.NotificationType.Success);
            SaveManager.inst.UpdateSettingsFile(false);
        }

        public static void TransformPosition(Keybind keybind)
        {
            if (keybind.settings.ContainsKey("Create Keyframe") && bool.TryParse(keybind.settings["Create Keyframe"], out bool createKeyframe))
            {
                inst.createKeyframe = createKeyframe;
            }

            if (keybind.settings.ContainsKey("Use Nearest") && bool.TryParse(keybind.settings["Use Nearest"], out bool useNearest))
            {
                inst.useNearest = useNearest;
            }

            if (keybind.settings.ContainsKey("Use Previous") && bool.TryParse(keybind.settings["Use Previous"], out bool usePrevious))
            {
                inst.usePrevious = usePrevious;
            }

            inst.SetValues(0);
        }
        
        public static void TransformScale(Keybind keybind)
        {
            if (keybind.settings.ContainsKey("Create Keyframe") && bool.TryParse(keybind.settings["Create Keyframe"], out bool createKeyframe))
            {
                inst.createKeyframe = createKeyframe;
            }

            if (keybind.settings.ContainsKey("Use Nearest") && bool.TryParse(keybind.settings["Use Nearest"], out bool useNearest))
            {
                inst.useNearest = useNearest;
            }

            if (keybind.settings.ContainsKey("Use Previous") && bool.TryParse(keybind.settings["Use Previous"], out bool usePrevious))
            {
                inst.usePrevious = usePrevious;
            }

            inst.SetValues(1);
        }
        
        public static void TransformRotation(Keybind keybind)
        {
            if (keybind.settings.ContainsKey("Create Keyframe") && bool.TryParse(keybind.settings["Create Keyframe"], out bool createKeyframe))
            {
                inst.createKeyframe = createKeyframe;
            }

            if (keybind.settings.ContainsKey("Use Nearest") && bool.TryParse(keybind.settings["Use Nearest"], out bool useNearest))
            {
                inst.useNearest = useNearest;
            }

            if (keybind.settings.ContainsKey("Use Previous") && bool.TryParse(keybind.settings["Use Previous"], out bool usePrevious))
            {
                inst.usePrevious = usePrevious;
            }

            inst.SetValues(2);
        }

        public static void SpawnSelectedQuickPrefab(Keybind keybind)
        {
            if (PrefabEditor.inst.currentPrefab != null)
                PrefabEditor.inst.AddPrefabObjectToLevel(PrefabEditor.inst.currentPrefab);
            else
                EditorManager.inst.DisplayNotification("No selected quick prefab!", 1f, EditorManager.NotificationType.Error);
        }

        public static void ResetIntegerVariables(Keybind keybind)
        {
            foreach (var beatmapObject in GameData.Current.BeatmapObjects)
            {
                beatmapObject.integerVariable = 0;
            }
        }

        public static void ToggleProjectPlanner(Keybind keybind)
        {
            ProjectPlannerManager.inst?.ToggleState();
        }

        public static void ForceSnapBPM(Keybind keybind)
        {
            if (RTEditor.inst.layerType == RTEditor.LayerType.Objects && ObjectEditor.inst.SelectedObjects.Count > 0)
                foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                {
                    timelineObject.Time = RTEditor.SnapToBPM(timelineObject.Time);
                    if (timelineObject.IsBeatmapObject)
                        Updater.UpdateProcessor(timelineObject.GetData<BeatmapObject>(), "Start Time");
                    if (timelineObject.IsPrefabObject)
                        Updater.UpdatePrefab(timelineObject.GetData<PrefabObject>(), "Start Time");

                    ObjectEditor.inst.RenderTimelineObject(timelineObject);
                }

            if (ObjEditor.inst.ObjectView.activeInHierarchy
                && ObjectEditor.inst.CurrentSelection.IsBeatmapObject
                && ObjectEditor.inst.CurrentSelection.InternalSelections.Where(x => x.selected).Count() > 0)
            {
                foreach (var timelineObject in ObjectEditor.inst.CurrentSelection.InternalSelections)
                {
                    if (timelineObject.Index != 0)
                        timelineObject.Time = RTEditor.SnapToBPM(timelineObject.Time);
                }

                var bm = ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>();
                ObjectEditor.inst.RenderTimelineObject(ObjectEditor.inst.CurrentSelection);
                Updater.UpdateProcessor(bm, "Keyframes");
                ObjectEditor.inst.RenderKeyframes(bm);
            }

            if (RTEditor.inst.layerType == RTEditor.LayerType.Events && RTEventEditor.inst.SelectedKeyframes.Count > 0)
                foreach (var timelineObject in RTEventEditor.inst.SelectedKeyframes)
                {
                    if (timelineObject.Index != 0)
                        timelineObject.Time = RTEditor.SnapToBPM(timelineObject.Time);

                    RTEventEditor.inst.RenderTimelineObject(timelineObject);
                }

            if (RTEditor.DraggingPlaysSound)
            {
                SoundManager.inst.PlaySound("LeftRight", 0.7f, 0.6f);
                SoundManager.inst.PlaySound("LeftRight", 0.8f, 0.1f);
            }
        }

        public static void AddLayer(Keybind keybind)
        {
            if (keybind.settings.ContainsKey("Layer") && int.TryParse(keybind.settings["Layer"], out int num))
            {
                RTEditor.inst.Layer += num;
                RTEditor.inst.SetLayer(RTEditor.inst.Layer);
            }
        }

        public static void ParentPicker(Keybind keybind)
        {
            if (!ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
                return;

            RTEditor.inst.parentPickerEnabled = true;
            RTEditor.inst.objectToParent = ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>();
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
            null, // 51
            null, // 52
            null, // 53
            new Dictionary<string, string>
            {
                { "Create Keyframe", "True" },
                { "Use Nearest", "True" },
                { "Use Previous", "False" },
            }, // 54
            new Dictionary<string, string>
            {
                { "Create Keyframe", "True" },
                { "Use Nearest", "True" },
                { "Use Previous", "False" },
            }, // 55
            new Dictionary<string, string>
            {
                { "Create Keyframe", "True" },
                { "Use Nearest", "True" },
                { "Use Previous", "False" },
            }, // 56
            null, // 57
            null, // 58
            null, // 59
            null, // 60
            new Dictionary<string, string>
            {
                { "Layer", "1" }
            }, // 61
            null, // 62
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
            var keyCodes = Enum.GetValues(typeof(KeyCode));
            for (int i = 0; i < keyCodes.Length; i++)
            {
                var name = Enum.GetName(typeof(KeyCode), i);
                if (!string.IsNullOrEmpty(name) && name.ToLower() != "none" && Input.GetKeyDown((KeyCode)i))
                    return (KeyCode)i;
            }

            return KeyCode.None;
        }

        public void SetValues(int type)
        {
            if (ObjectEditor.inst.SelectedObjectCount > 1)
            {
                EditorManager.inst.DisplayNotification("Cannot shift multiple objects around currently.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            if (ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
            {
                selectionType = SelectionType.Object;
                beatmapObject = ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>();
                SetCurrentKeyframe(type);
            }
            if (ObjectEditor.inst.CurrentSelection.IsPrefabObject)
            {
                selectionType = SelectionType.Prefab;
                prefabObject = ObjectEditor.inst.CurrentSelection.GetData<PrefabObject>();
                selectedKeyframe = (EventKeyframe)prefabObject.events[type];
                originalValues = selectedKeyframe.eventValues.Copy();
            }

            setKeyframeValues = false;

            firstDirection = RTObject.Axis.Static;

            currentType = type;

            dragging = true;
        }

        public void UpdateValues()
        {
            var vector = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f);
            var vector2 = Camera.main.ScreenToWorldPoint(vector) * (currentType == 1 ? 0.2f : currentType == 2 ? 2f : 1f);
            var vector3 = currentType != 1 ? new Vector3((int)vector2.x, (int)vector2.y, 0f) :
                new Vector3(RTMath.RoundToNearestDecimal(vector2.x, 1), RTMath.RoundToNearestDecimal(vector2.y, 1), 0f);

            if (dragging)
            {
                switch (currentType)
                {
                    case 0:
                        {
                            if (!setKeyframeValues)
                            {
                                setKeyframeValues = true;
                                dragKeyframeValues = new Vector2(selectedKeyframe.eventValues[0], selectedKeyframe.eventValues[1]);
                                dragOffset = Input.GetKey(KeyCode.LeftShift) ? vector3 : vector2;
                            }

                            var finalVector = Input.GetKey(KeyCode.LeftShift) ? vector3 : vector2;

                            if (Input.GetKey(KeyCode.LeftControl) && firstDirection == RTObject.Axis.Static)
                            {
                                if (dragOffset.x > finalVector.x)
                                    firstDirection = RTObject.Axis.PosX;

                                if (dragOffset.x < finalVector.x)
                                    firstDirection = RTObject.Axis.NegX;

                                if (dragOffset.y > finalVector.y)
                                    firstDirection = RTObject.Axis.PosY;

                                if (dragOffset.y < finalVector.y)
                                    firstDirection = RTObject.Axis.NegY;
                            }

                            if (Input.GetKey(KeyCode.X))
                            {
                                if (dragOffset.x > finalVector.x)
                                    firstDirection = RTObject.Axis.PosX;
                                if (dragOffset.x < finalVector.x)
                                    firstDirection = RTObject.Axis.NegX;
                            }
                            
                            if (Input.GetKey(KeyCode.Y))
                            {
                                if (dragOffset.x > finalVector.x)
                                    firstDirection = RTObject.Axis.PosY;
                                if (dragOffset.x < finalVector.x)
                                    firstDirection = RTObject.Axis.NegY;
                            }

                            if (firstDirection == RTObject.Axis.NegX || firstDirection == RTObject.Axis.PosX)
                                selectedKeyframe.eventValues[1] = dragKeyframeValues.y;
                            
                            if (firstDirection == RTObject.Axis.NegY || firstDirection == RTObject.Axis.PosY)
                                selectedKeyframe.eventValues[0] = dragKeyframeValues.x;

                            if (firstDirection == RTObject.Axis.Static || firstDirection == RTObject.Axis.PosX || firstDirection == RTObject.Axis.NegX)
                                selectedKeyframe.eventValues[0] = dragKeyframeValues.x - dragOffset.x + (Input.GetKey(KeyCode.LeftShift) ? vector3.x : vector2.x);
                            if (firstDirection == RTObject.Axis.Static || firstDirection == RTObject.Axis.PosY || firstDirection == RTObject.Axis.NegY)
                                selectedKeyframe.eventValues[1] = dragKeyframeValues.y - dragOffset.y + (Input.GetKey(KeyCode.LeftShift) ? vector3.y : vector2.y);

                            break;
                        }
                    case 1:
                        {

                            if (!setKeyframeValues)
                            {
                                setKeyframeValues = true;
                                dragKeyframeValues = new Vector2(selectedKeyframe.eventValues[0], selectedKeyframe.eventValues[1]);
                                dragOffset = Input.GetKey(KeyCode.LeftShift) ? vector3 : vector2;
                            }

                            var finalVector = Input.GetKey(KeyCode.LeftShift) ? vector3 : vector2;

                            if (Input.GetKey(KeyCode.LeftControl))
                            {
                                float total = Vector2.Distance(dragOffset, finalVector);

                                selectedKeyframe.eventValues[0] = dragKeyframeValues.x + total;
                                selectedKeyframe.eventValues[1] = dragKeyframeValues.y + total;
                            }
                            else
                            {
                                selectedKeyframe.eventValues[0] = dragKeyframeValues.x - (dragOffset.x + finalVector.x);
                                selectedKeyframe.eventValues[1] = dragKeyframeValues.y - (dragOffset.y + finalVector.y);
                            }

                            break;
                        }
                    case 2:
                        {
                            var position = selectionType == SelectionType.Prefab ? new Vector3(prefabObject.events[0].eventValues[0], prefabObject.events[0].eventValues[1], 0f) : beatmapObject.levelObject?.visualObject?.GameObject.transform.position ??
                                new Vector3(beatmapObject.events[0].FindLast(x => x.eventTime < AudioManager.inst.CurrentAudioSource.time).eventValues[0], beatmapObject.events[0].FindLast(x => x.eventTime < AudioManager.inst.CurrentAudioSource.time).eventValues[1], 0f);

                            if (!setKeyframeValues)
                            {
                                setKeyframeValues = true;
                                dragKeyframeValuesFloat = selectedKeyframe.eventValues[0];
                                dragOffsetFloat = Input.GetKey(KeyCode.LeftShift) ? RTMath.roundToNearest(-RTMath.VectorAngle(position, vector2), 15f) : -RTMath.VectorAngle(transform.position, vector2);
                            }

                            selectedKeyframe.eventValues[0] =
                                Input.GetKey(KeyCode.LeftShift) ? RTMath.roundToNearest(dragKeyframeValuesFloat - dragOffsetFloat + -RTMath.VectorAngle(position, vector2), 15f) :
                                dragKeyframeValuesFloat - dragOffsetFloat + -RTMath.VectorAngle(position, vector2);

                            break;
                        }
                }

                if (selectionType == SelectionType.Object)
                {
                    Updater.UpdateProcessor(beatmapObject, "Keyframes");
                }
                if (selectionType == SelectionType.Prefab)
                {
                    Updater.UpdatePrefab(prefabObject, "Offset");
                }
            }
        }

        public void SetCurrentKeyframe(int type)
        {
            var timeOffset = AudioManager.inst.CurrentAudioSource.time - beatmapObject.StartTime;
            int nextIndex = beatmapObject.events[type].FindIndex(x => x.eventTime >= timeOffset);
            if (nextIndex < 0)
                nextIndex = beatmapObject.events[type].Count - 1;

            int index;
            if (beatmapObject.events[type].Has(x => x.eventTime > timeOffset - 0.1f && x.eventTime < timeOffset + 0.1f) && useNearest)
            {
                selectedKeyframe = (EventKeyframe)beatmapObject.events[type].Find(x => x.eventTime > timeOffset - 0.1f && x.eventTime < timeOffset + 0.1f);
                index = beatmapObject.events[type].FindIndex(x => x.eventTime > timeOffset - 0.1f && x.eventTime < timeOffset + 0.1f);
                AudioManager.inst.CurrentAudioSource.time = selectedKeyframe.eventTime + beatmapObject.StartTime;
            }
            else if (createKeyframe)
            {
                selectedKeyframe = EventKeyframe.DeepCopy((EventKeyframe)beatmapObject.events[type][nextIndex]);
                selectedKeyframe.eventTime = timeOffset;
                index = beatmapObject.events[type].Count;
                beatmapObject.events[type].Add(selectedKeyframe);
            }
            else if (usePrevious)
            {
                selectedKeyframe = (EventKeyframe)beatmapObject.events[type].FindLast(x => x.eventTime < timeOffset);
                index = beatmapObject.events[type].FindLastIndex(x => x.eventTime < timeOffset);
            }
            else
            {
                selectedKeyframe = (EventKeyframe)beatmapObject.events[type][0];
                index = 0;
            }

            originalValues = selectedKeyframe.eventValues.Copy();

            ObjectEditor.inst.RenderKeyframes(beatmapObject);
            ObjectEditor.inst.SetCurrentKeyframe(beatmapObject, type, index, false, false);
        }

        public bool createKeyframe = true;
        public bool useNearest = true;
        public bool usePrevious = false;

        public int currentType;

        public bool dragging;

        public BeatmapObject beatmapObject;
        public PrefabObject prefabObject;

        public bool setKeyframeValues;
        public Vector2 dragKeyframeValues;
        public EventKeyframe selectedKeyframe;
        public float[] originalValues;
        public Vector2 dragOffset;
        float dragOffsetFloat;
        float dragKeyframeValuesFloat;
        public RTObject.Axis firstDirection = RTObject.Axis.Static;

        public SelectionType selectionType;

        #endregion

        public bool KeyCodeHandler(Keybind keybind)
        {
            if (keybind.keys.Count > 0 && keybind.keys.All(x => Input.GetKey(x.KeyCode) && x.InteractType == Keybind.Key.Type.Pressed ||
            Input.GetKeyDown(x.KeyCode) && x.InteractType == Keybind.Key.Type.Down ||
            !Input.GetKey(x.KeyCode) && x.InteractType == Keybind.Key.Type.Up || !Input.GetKey(x.KeyCode) && x.InteractType == Keybind.Key.Type.NotPressed) && !isPressingKey)
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
                this.settings = settings ?? new Dictionary<string, string>();
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

                var setting = inst.Settings[actionType];
                if (setting != null)
                {
                    foreach (var keyValuePair in setting)
                    {
                        if (!dictionary.ContainsKey(keyValuePair.Key))
                            dictionary.Add(keyValuePair.Key, keyValuePair.Value);
                    }
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

            public string DefaultCode => $"var keybind = EditorManagement.Functions.Editors.KeybindManager.inst.keybinds.Find(x => x.id == \"{id}\");{Environment.NewLine}";

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
                    Up,
                    NotPressed
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
