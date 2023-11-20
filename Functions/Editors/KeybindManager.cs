using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

using SimpleJSON;

using EditorManagement.Functions.Editors;
using RTFunctions.Functions;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;
using RTFunctions.Functions.Optimization;

namespace EditorManagement.Functions.Editors
{
    public class KeybindManager : MonoBehaviour
    {
        public static string className = "[<color=#F44336>KeybindManager</color>] \n";
        public static KeybindManager inst;

        public bool isPressingKey;

        void Awake()
        {
            inst = this;
        }

        void Update()
        {
            foreach (var keybind in keybinds)
            {
                keybind.Activate();
            }
        }

        public bool KeyCodeHandler(KeyCode key, KeyCode keyDown)
        {
            var bv = Input.GetKey(key) && Input.GetKeyDown(keyDown) || key == KeyCode.None && Input.GetKeyDown(keyDown) && !isPressingKey;
            if (bv)
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

        #region Methods

        public static void ToggleEditor(Keybind keybind) => EditorManager.inst.ToggleEditor();

        public static void UpdateEverything(Keybind keybind)
        {
            EventManager.inst.updateEvents();
            ObjectManager.inst.updateObjects();
        }

        public static void OpenPrefabDialog(Keybind keybind)
        {
            PrefabEditor.inst.OpenDialog();
        }

        public static void CollapsePrefab(Keybind keybind)
        {
            if (ObjectEditor.inst.SelectedBeatmapObjects.Count == 1 && ObjectEditor.inst.CurrentBeatmapObjectSelection && !string.IsNullOrEmpty(ObjectEditor.inst.CurrentBeatmapObjectSelection.Data.prefabInstanceID))
                PrefabEditor.inst.CollapseCurrentPrefab();
        }

        public static void ExpandPrefab(Keybind keybind)
        {
            if (ObjectEditor.inst.SelectedPrefabObjects.Count == 1 && ObjectEditor.inst.CurrentPrefabObjectSelection && ObjectEditor.inst.CurrentPrefabObjectSelection.Data != null)
                PrefabEditor.inst.ExpandCurrentPrefab();
        }

        public static void SetSongTimeAutokill(Keybind keybind)
        {
            foreach (var timelineObject in ObjectEditor.inst.SelectedBeatmapObjects)
            {
                var bm = timelineObject.Data;

                bm.autoKillType = DataManager.GameData.BeatmapObject.AutoKillType.SongTime;
                bm.autoKillOffset = AudioManager.inst.CurrentAudioSource.time;
                bm.editorData.collapse = true;

                Updater.UpdateProcessor(bm);
                ObjectEditor.RenderTimelineObject(timelineObject);
            }
        }

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

            foreach (var timelineObject in ObjectEditor.inst.SelectedBeatmapObjects)
            {
                var bm = timelineObject.Data;

                type = Mathf.Clamp(type, 0, bm.events.Count - 1);
                index = Mathf.Clamp(index, 0, bm.events[type].Count - 1);
                value = Mathf.Clamp(value, 0, bm.events[type][index].eventValues.Length - 1);

                var val = bm.events[type][index].eventValues[value];

                if (type == 3 && val == 0)
                    val = Mathf.Clamp(val + 0, 0, GameManager.inst.LiveTheme.objectColors.Count - 1);
                else
                    val += 0;

                bm.events[type][index].eventValues[value] = val;

                Updater.UpdateProcessor(bm, "Keyframes");
            }
            
            foreach (var timelineObject in ObjectEditor.inst.SelectedPrefabObjects)
            {
                var po = timelineObject.Data;

                type = Mathf.Clamp(type, 0, po.events.Count - 1);
                value = Mathf.Clamp(value, 0, po.events[type].eventValues.Length - 1);

                po.events[type].eventValues[value] += 0;

                Updater.UpdatePrefab(po);
            }
        }

        #endregion

        public List<Keybind> keybinds = new List<Keybind>();

        public class Keybind
        {
            public Keybind(string id, KeyCode keyCode, KeyCode keyCodeDown, Action<Keybind> action)
            {
                this.id = id;
                KeyCode = keyCode;
                KeyCodeDown = keyCodeDown;
                Action = action;
            }

            public static Keybind ParseKeyAction(JSONNode jn)
            {
                string id = jn["id"];

                Action<Keybind> action = delegate (Keybind keybind)
                {
                    Debug.LogError($"{className}No action assigned to key!");
                };

                switch (jn["action"].AsInt)
                {
                    case 0:
                        {
                            action = ToggleEditor;
                            break;
                        }
                    case 1:
                        {
                            action = UpdateEverything;
                            break;
                        }
                    case 2:
                        {
                            action = OpenPrefabDialog;
                            break;
                        }
                    case 3:
                        {
                            action = CollapsePrefab;
                            break;
                        }
                    case 4:
                        {
                            action = ExpandPrefab;
                            break;
                        }
                    case 5:
                        {
                            action = SetSongTimeAutokill;
                            break;
                        }
                    case 234324:
                        {
                            action = delegate (Keybind keybind)
                            {
                                string str = $"var keybind = EditorManagement.Functions.Editors.KeybindManager.inst.keybinds.Find(x => x.id == {id});";

                                str += jn["custom"];

                                RTCode.Evaluate(jn["custom"]);
                            };

                            break;
                        }
                }

                return new Keybind(id, (KeyCode)jn["key"].AsInt, (KeyCode)jn["keydown"].AsInt, action);
            }

            public void Activate()
            {
                if (inst.KeyCodeHandler(KeyCode, KeyCodeDown))
                    Action?.Invoke(this);
            }

            public string id;
            public Dictionary<string, string> settings = new Dictionary<string, string>();
            public KeyCode KeyCode { get; set; }
            public KeyCode KeyCodeDown { get; set; }
            public Action<Keybind> Action { get; set; }
        }
    }
}
