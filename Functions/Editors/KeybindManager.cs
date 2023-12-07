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

namespace EditorManagement.Functions.Editors
{
    public class KeybindManager : MonoBehaviour
    {
        public static string className = "[<color=#F44336>KeybindManager</color>] \n";
        public static KeybindManager inst;

        public static string FilePath => $"{RTFile.ApplicationDirectory}settings/keybinds.lss";

        public bool isPressingKey;

        static Scrollbar tlScrollbar;

        public static KeyCode[] keyCodes = RTFunctions.Enums.EnumUtils.GetAll<KeyCode>();

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
        }

        void Update()
        {
            if (!LSHelpers.IsUsingInputField() && (!ModCompatibility.sharedFunctions.ContainsKey("EventsCoreEditorOffset") || !(bool)ModCompatibility.sharedFunctions["EventsCoreEditorOffset"]))
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
            OpenPopup, // 8
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

        public static void OpenPopup(Keybind keybind)
        {
            if (EditorManager.inst && keybind.settings.ContainsKey("Popup") && EditorManager.inst.EditorDialogsDictionary.ContainsKey(keybind.settings["Popup"]))
            {
                EditorManager.inst.ShowDialog(keybind.settings["Popup"]);
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
                    timelineObject.GetData<BeatmapObject>().editorData.Layer++;
                if (timelineObject.IsPrefabObject)
                    timelineObject.GetData<PrefabObject>().editorData.Layer++;

                ObjectEditor.inst.RenderTimelineObject(timelineObject);
            }
        }
        
        public static void SubObjectLayer(Keybind keybind)
        {
            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
            {
                if (timelineObject.IsBeatmapObject && timelineObject.GetData<BeatmapObject>().editorData.Layer > 0)
                    timelineObject.GetData<BeatmapObject>().editorData.Layer--;
                if (timelineObject.IsPrefabObject && timelineObject.GetData<PrefabObject>().editorData.Layer > 0)
                    timelineObject.GetData<PrefabObject>().editorData.Layer--;

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

                    if ((int)bm.objectType > RTFunctions.Enums.EnumUtils.GetAll(bm.objectType).Length)
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
                        e = RTFunctions.Enums.EnumUtils.GetAll(bm.objectType).Length - 1;

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
                ObjEditor.inst.SetCurrentKeyframe(0, true);
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
                ObjEditor.inst.AddCurrentKeyframe(10000, true);
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
                ObjEditor.inst.AddCurrentKeyframe(1, true);
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
                ObjEditor.inst.AddCurrentKeyframe(-1, true);
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

        #endregion

        #region Settings



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
            for (int i = 0; i < keyCodes.Length; i++)
            {
                if (Input.GetKeyDown(keyCodes[i]))
                    return keyCodes[i];
            }

            return KeyCode.None;
        }

        #endregion

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
                        dictionary.Add(jn["settings"][i], jn["settings"][i][0]);
                }

                return new Keybind(id, keys, actionType, dictionary);
            }

            public JSONNode ToJSON()
            {
                var jn = JSON.Parse("{}");

                jn["id"] = id;
                if (ActionType >= 0 && ActionType < KeybinderMethods.Count)
                    jn["name"] = KeybinderMethods[ActionType].Method.Name;
                else
                    jn["name"] = "Invalid method";

                jn["action"] = ActionType.ToString();
                for (int i = 0; i < keys.Count; i++)
                {
                    jn["keys"][i] = keys[i].ToJSON();
                }

                for (int i = 0; i < settings.Count; i++)
                {
                    var element = settings.ElementAt(i);
                    jn["settings"][element.Key] = element.Value;
                }

                return jn;
            }

            public void Activate()
            {
                if (keys.All(x => Input.GetKey(x.KeyCode) && x.InteractType == Key.Type.Pressed ||
                Input.GetKeyDown(x.KeyCode) && x.InteractType == Key.Type.Down ||
                Input.GetKeyUp(x.KeyCode) && x.InteractType == Key.Type.Up) && !inst.isPressingKey)
                {
                    Debug.Log($"{EditorPlugin.className}Pressed!");
                    inst.isPressingKey = true;
                    Action?.Invoke(this);
                }
                else
                    inst.isPressingKey = false;
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

            public bool watchingKeybind;

            public string DefaultCode => $"var keybind = EditorManagement.Functions.Editors.KeybindManager.inst.keybinds.Find(x => x.id == {id});{Environment.NewLine}";

            public List<Key> keys = new List<Key>();

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
