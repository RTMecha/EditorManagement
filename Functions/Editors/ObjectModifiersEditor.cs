using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using LSFunctions;

using EditorManagement.Functions.Components;
using EditorManagement.Functions.Tools;
using EditorManagement.Functions;

using RTFunctions.Functions;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;

using BeatmapObject = DataManager.GameData.BeatmapObject;

namespace EditorManagement.Functions.Editors
{
    public class ObjectModifiersEditor : MonoBehaviour
    {
        public static ObjectModifiersEditor inst;

        public static Type objectModifiersPlugin;

        public static bool installed = false;

        public static Transform content;
        public static Transform scrollView;
        public static RectTransform scrollViewRT;

        public static bool showModifiers;

        public static InputField replEditor;
        public static GameObject replBase;
        public static Text replText;

        void Awake()
        {
            if (!GameObject.Find("BepInEx_Manager").GetComponentByName("ObjectModifiersPlugin"))
            {
                Destroy(gameObject);
            }
            else
            {
                objectModifiersPlugin = GameObject.Find("BepInEx_Manager").GetComponentByName("ObjectModifiersPlugin").GetType();

                inst = this;

                CreateModifiersOnAwake();
                CreateDefaultModifiersList();
            }
        }

        float time;
        float timeOffset;
        bool setTime;

        void Update()
        {
            if (!setTime)
            {
                timeOffset = Time.time;
                setTime = true;
            }

            time = timeOffset - Time.time;
            timeOffset = Time.time;
        }

        #region Plugin stuff

        public static void AddModifierObject(BeatmapObject _beatmapObject)
        {
            if (objectModifiersPlugin == null)
                return;

            if (!_beatmapObject.fromPrefab)
            {
                objectModifiersPlugin.GetMethod("AddModifierObject").Invoke(objectModifiersPlugin, new object[] { _beatmapObject });
            }
        }

        public static void RemoveModifierObject(BeatmapObject _beatmapObject)
        {
            if (objectModifiersPlugin == null)
                return;

            objectModifiersPlugin.GetMethod("RemoveModifierObject").Invoke(objectModifiersPlugin, new object[] { _beatmapObject });
        }

        public static void ClearModifierObjects()
        {
            if (objectModifiersPlugin == null)
                return;

            objectModifiersPlugin.GetMethod("ClearModifierObjects").Invoke(objectModifiersPlugin, new object[] { });
        }

        public static object GetModifierIndex(BeatmapObject _beatmapObject, int index)
        {
            if (objectModifiersPlugin == null)
                return null;

            return objectModifiersPlugin.GetMethod("GetModifierIndex").Invoke(objectModifiersPlugin, new object[] { _beatmapObject, index });
        }

        public static int GetModifierCount(BeatmapObject _beatmapObject)
        {
            if (objectModifiersPlugin == null)
                return 0;

            return (int)objectModifiersPlugin.GetMethod("GetModifierCount").Invoke(objectModifiersPlugin, new object[] { _beatmapObject });
        }

        public static object GetModifierObject(BeatmapObject _beatmapObject)
        {
            if (objectModifiersPlugin == null)
                return null;

            return objectModifiersPlugin.GetMethod("GetModifierObject").Invoke(objectModifiersPlugin, new object[] { _beatmapObject });
        }

        public static void RemoveModifierIndex(BeatmapObject _beatmapObject, int index)
        {
            if (objectModifiersPlugin == null)
                return;

            objectModifiersPlugin.GetMethod("RemoveModifierIndex").Invoke(objectModifiersPlugin, new object[] { _beatmapObject, index });
            ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
        }

        public static void AddModifierToObject(BeatmapObject _beatmapObject, int index)
        {
            if (objectModifiersPlugin == null)
                return;

            objectModifiersPlugin.GetMethod("AddModifierToObject").Invoke(objectModifiersPlugin, new object[] { _beatmapObject, index });
            ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
        }

        #endregion

        public static void CreateModifiersOnAwake()
        {
            var bmb = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View");

            var act = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/grain/colored"));
            act.transform.SetParent(bmb.transform.Find("Viewport/Content"));
            act.transform.localScale = Vector3.one;
            act.name = "active";
            act.transform.Find("Text").GetComponent<Text>().text = "Show Modifiers";

            var toggle = act.GetComponent<Toggle>();
            toggle.onValueChanged.RemoveAllListeners();
            toggle.isOn = showModifiers;
            toggle.onValueChanged.AddListener(delegate (bool _val)
            {
                showModifiers = _val;
                scrollView.gameObject.SetActive(showModifiers);
                RTEditor.inst.StartCoroutine(RTEditor.RefreshObjectGUI());
            });

            var e = Instantiate(bmb);

            scrollView = e.transform;

            scrollView.SetParent(bmb.transform.Find("Viewport/Content"));
            scrollView.localScale = Vector3.one;
            scrollView.name = "Modifiers Scroll View";

            scrollViewRT = scrollView.GetComponent<RectTransform>();

            content = scrollView.Find("Viewport/Content");
            LSFunctions.LSHelpers.DeleteChildren(content);

            scrollView.gameObject.SetActive(showModifiers);

            var font = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("Panel/Text").GetComponent<Text>().font;

            replBase = new GameObject("REPL Editor");
            replBase.transform.SetParent(EditorManager.inst.GetDialog("Quick Actions Popup").Dialog.parent);
            replBase.transform.localScale = Vector3.one;
            var replRT = replBase.AddComponent<RectTransform>();

            replRT.anchoredPosition = Vector2.zero;

            var uiField = UIManager.GenerateUIInputField("REPL Editor", replBase.transform);

            replEditor = (InputField)uiField["InputField"];

            ((Image)uiField["Image"]).color = new Color(0.1132075f, 0.1132075f, 0.1132075f);

            replEditor.lineType = InputField.LineType.MultiLineNewline;
            replEditor.textComponent.color = new Color(0.9788679f, 0.9788679f, 0.9788679f, 1f);
            replEditor.textComponent.font = font;

            ((RectTransform)uiField["RectTransform"]).anchoredPosition = Vector2.zero;
            ((RectTransform)uiField["RectTransform"]).sizeDelta = new Vector2(1000f, 550f);

            var uiTop = UIManager.GenerateUIImage("Panel", replBase.transform);

            UIManager.SetRectTransform((RectTransform)uiTop["RectTransform"], new Vector2(0f, 291f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1000f, 32f));

            ((Image)uiTop["Image"]).color = new Color(0.1973585f, 0.1973585f, 0.1973585f);

            var close = Instantiate(EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("Panel/x").gameObject);
            close.transform.SetParent(((GameObject)uiTop["GameObject"]).transform);
            close.transform.localScale = Vector3.one;

            close.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            var closeButton = close.GetComponent<Button>();
            closeButton.onClick.ClearAll();
            closeButton.onClick.AddListener(delegate ()
            {
                EditorManager.inst.HideDialog("REPL Editor Popup");
                RTEditor.inst.StartCoroutine(RTEditor.RefreshObjectGUI());
            });

            var uiTitle = UIManager.GenerateUIText("Title", ((GameObject)uiTop["GameObject"]).transform);
            UIManager.SetRectTransform(((RectTransform)uiTitle["RectTransform"]), new Vector2(-350f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(300f, 32f));
            ((Text)uiTitle["Text"]).text = "REPL Editor";
            ((Text)uiTitle["Text"]).alignment = TextAnchor.MiddleLeft;
            ((Text)uiTitle["Text"]).font = font;

            var rtext = Instantiate(replEditor.textComponent.gameObject);
            rtext.transform.SetParent(replEditor.transform);
            rtext.transform.localScale = Vector3.one;

            var rttext = rtext.GetComponent<RectTransform>();
            rttext.anchoredPosition = new Vector2(2f, 0f);
            rttext.sizeDelta = new Vector2(-12f, -8f);

            var selectUI = ((GameObject)uiTop["GameObject"]).AddComponent<SelectUI>();
            selectUI.target = replBase.transform;

            //((RectTransform)replEditor.textComponent.transform).anchoredPosition = new Vector2(9999f, 9999f);
            replEditor.textComponent.color = new Color(0.9788679f, 0.9788679f, 0.9788679f, 0f);
            //replEditor.textComponent.GetComponent<CanvasRenderer>().cull = true;

            replEditor.customCaretColor = true;
            replEditor.caretColor = new Color(0.9788679f, 0.9788679f, 0.9788679f, 1f);

            replText = rtext.GetComponent<Text>();

            replBase.SetActive(false);

            Triggers.AddEditorDialog("REPL Editor Popup", replBase);
        }

        //I probably should write this so it switches between what command it is and has the same copied and pasted code but altered just so it's not spaghetti
        public static IEnumerator RenderModifiers()
        {
            var beatmapObject = ObjEditor.inst.currentObjectSelection.GetObjectData();
            var modifierObject = GetModifierObject(beatmapObject);

            if (modifierObject != null && EditorManager.inst.isEditing)
            {
                var label = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content").transform.GetChild(2).gameObject;
                var singleInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move/position/x");
                var vector2Input = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move/position");
                var boolInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/toggle/toggle");
                var dropdownInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/autokill/tod-dropdown");
                var sliderFullInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/bpm");
                var stringInput = GameObject.Find("TimelineBar/GameObject/Time Input");

                Text textFont = GameObject.Find("TitleBar/File/Text").GetComponent<Text>();
                var close = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("Panel/x");

                LSFunctions.LSHelpers.DeleteChildren(content);

                var count = GetModifierCount(beatmapObject);

                if (count > 0)
                {
                    for (int j = 0; j < count; j++)
                    {
                        var modifier = GetModifierIndex(beatmapObject, j);

                        var type = (int)modifier.GetType().GetField("type", BindingFlags.Public | BindingFlags.Instance).GetValue(modifier);

                        List<string> commands = (List<string>)modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).GetValue(modifier);

                        var value = (string)modifier.GetType().GetField("value", BindingFlags.Public | BindingFlags.Instance).GetValue(modifier);

                        var constant = ((bool)modifier.GetType().GetField("constant", BindingFlags.Public | BindingFlags.Instance).GetValue(modifier)).ToString();

                        var notGate = ((bool)modifier.GetType().GetField("not", BindingFlags.Public | BindingFlags.Instance).GetValue(modifier)).ToString();

                        if (commands.Count > 0 && !string.IsNullOrEmpty(commands[0]))
                        {
                            #region Old Code

                            //{
                            //    if (commands[0] != "playerCollide" && commands[0] != "playerKill" && commands[0] != "playerKillAll" && !commands[0].Contains("playerWarp") && commands[0] != "mouseOver" && commands[0] != "showMouse")
                            //    {
                            //        GameObject x = Instantiate(singleInput);
                            //        x.transform.SetParent(content);
                            //        x.name = commands[0];

                            //        //Main Label
                            //        {
                            //            var l = Instantiate(label);
                            //            l.name = "label";
                            //            l.transform.SetParent(x.transform);
                            //            l.transform.SetAsFirstSibling();
                            //            l.transform.localScale = Vector3.one;
                            //            l.transform.GetChild(0).GetComponent<Text>().text = commands[0];
                            //            l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(187f, 20f);
                            //            l.transform.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                            //            var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
                            //            {
                            //                ltextrt.anchoredPosition = new Vector2(10f, -5f);
                            //            }
                            //        }

                            //        x.transform.localScale = Vector3.one;
                            //        x.transform.GetChild(0).localScale = Vector3.one;

                            //        var xRT = x.GetComponent<RectTransform>();
                            //        {
                            //            xRT.sizeDelta = new Vector2(350f, 128f);
                            //        }

                            //        x.GetComponent<Image>().enabled = true;
                            //        x.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);

                            //        Triggers.AddTooltip(x, commands[0], "");

                            //        //Destroy
                            //        {
                            //            if (x.GetComponent<EventInfo>())
                            //                Destroy(x.GetComponent<EventInfo>());

                            //            if (x.GetComponent<HorizontalLayoutGroup>())
                            //                Destroy(x.GetComponent<HorizontalLayoutGroup>());

                            //            if (x.GetComponent<InputField>())
                            //                Destroy(x.GetComponent<InputField>());

                            //            if (x.GetComponent<InputFieldHelper>())
                            //                Destroy(x.GetComponent<InputFieldHelper>());

                            //            if (x.GetComponent<EventTrigger>())
                            //                Destroy(x.GetComponent<EventTrigger>());
                            //        }

                            //        bool playerHealthCommand = commands[0] == "playerHealthEquals" || commands[0] == "playerHealthLesserEquals" || commands[0] == "playerHealthGreaterEquals" || commands[0] == "playerHealthLesser" || commands[0] == "playerHealthGreater";
                            //        bool playerDeathCommand = commands[0] == "playerDeathsEquals" || commands[0] == "playerDeathsLesserEquals" || commands[0] == "playerDeathsGreaterEquals" || commands[0] == "playerDeathsLesser" || commands[0] == "playerDeathsGreater";
                            //        bool mouseCommand = commands[0] == "mouseButtonDown" || commands[0] == "mouseButton" || commands[0] == "mouseButtonUp";
                            //        bool variableCommand = commands[0] == "variableEquals" || commands[0] == "variableLesserEquals" || commands[0] == "variableGreaterEquals" || commands[0] == "variableLesser" || commands[0] == "variableGreater" || commands[0] == "addVariable" || commands[0] == "subVariable" || commands[0] == "setVariable";

                            //        var input = x.transform.Find("input");
                            //        var xif = input.gameObject.AddComponent<InputField>();
                            //        {
                            //            xif.onValueChanged.RemoveAllListeners();
                            //            xif.characterValidation = InputField.CharacterValidation.None;
                            //            xif.characterLimit = 0;
                            //            xif.textComponent = input.Find("Text").GetComponent<Text>();
                            //            xif.placeholder = input.Find("Placeholder").GetComponent<Text>();
                            //            xif.text = value;
                            //            xif.onValueChanged.AddListener(delegate (string _val)
                            //            {
                            //                if (commands[0] == "setMusicTime" || commands[0] == "setPitch" || commands[0] == "blur" || commands[0] == "particleSystem" || commands[0] == "trailRenderer" || commands[0] == "playerDistanceGreater" || commands[0] == "playerDistanceLesser")
                            //                {
                            //                    if (float.TryParse(_val, out float num))
                            //                    {
                            //                        modifier.GetType().GetField("value", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, num.ToString());
                            //                    }
                            //                }
                            //                else if (playerHealthCommand || playerDeathCommand || mouseCommand || variableCommand)
                            //                {
                            //                    if (int.TryParse(_val, out int num))
                            //                    {
                            //                        modifier.GetType().GetField("value", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, num.ToString());
                            //                    }
                            //                }
                            //                else if (commands[0] == "playSound" || commands[0] == "loadLevel" || commands[0] == "video")
                            //                {
                            //                    modifier.GetType().GetField("value", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, _val);
                            //                }
                            //            });
                            //        }

                            //        if (commands[0] == "setMusicTime" || commands[0] == "setPitch" || commands[0] == "blur" || commands[0] == "particleSystem" || commands[0] == "trailRenderer" || commands[0] == "playerDistanceGreater" || commands[0] == "playerDistanceLesser")
                            //        {
                            //            var xet = input.gameObject.AddComponent<EventTrigger>();
                            //            xet.triggers.Clear();
                            //            xet.triggers.Add(Triggers.ScrollDelta(xif, 0.1f, 10f));

                            //            var xifh = input.gameObject.AddComponent<InputFieldHelper>();
                            //            xifh.inputField = xif;
                            //        }
                            //        if (playerHealthCommand || playerDeathCommand || mouseCommand || variableCommand)
                            //        {
                            //            var xet = input.gameObject.AddComponent<EventTrigger>();
                            //            xet.triggers.Clear();
                            //            xet.triggers.Add(Triggers.ScrollDeltaInt(xif, 1, false, new List<int> { 0, int.MaxValue }));
                            //        }

                            //        var increase = x.transform.Find(">").GetComponent<Button>();
                            //        {
                            //            increase.onClick.RemoveAllListeners();
                            //            increase.onClick.AddListener(delegate ()
                            //            {
                            //                if (!playerHealthCommand && !playerDeathCommand && !mouseCommand && !variableCommand)
                            //                    xif.text = (float.Parse(xif.text) + 0.1f).ToString();
                            //                else
                            //                    xif.text = (int.Parse(xif.text) + 1).ToString();
                            //            });
                            //        }

                            //        var decrease = x.transform.Find("<").GetComponent<Button>();
                            //        {
                            //            decrease.onClick.RemoveAllListeners();
                            //            decrease.onClick.AddListener(delegate ()
                            //            {
                            //                if (!playerHealthCommand && !playerDeathCommand && !mouseCommand && !variableCommand)
                            //                    xif.text = (float.Parse(xif.text) - 0.1f).ToString();
                            //                else
                            //                    xif.text = (int.Parse(xif.text) - 1).ToString();
                            //            });
                            //        }

                            //        var layout = new GameObject("layout");
                            //        {
                            //            layout.transform.SetParent(x.transform);
                            //            layout.transform.localScale = Vector3.one;

                            //            var layoutRT = layout.AddComponent<RectTransform>();
                            //            var layoutVLG = layout.AddComponent<VerticalLayoutGroup>();

                            //            layoutRT.anchoredPosition = new Vector2(0f, 30f);
                            //            layoutRT.sizeDelta = Vector2.zero;
                            //            layoutVLG.childAlignment = TextAnchor.UpperCenter;
                            //            layoutVLG.spacing = 6f;
                            //        }

                            //        var valueG = new GameObject("value");
                            //        {
                            //            valueG.transform.SetParent(layout.transform);
                            //            valueG.transform.localScale = Vector3.one;

                            //            var valueGRT = valueG.AddComponent<RectTransform>();
                            //            var valueGHLG = valueG.AddComponent<HorizontalLayoutGroup>();

                            //            valueGHLG.childControlHeight = false;
                            //            valueGHLG.childControlWidth = false;
                            //            valueGHLG.childForceExpandWidth = false;
                            //            valueGHLG.spacing = 8f;
                            //        }

                            //        //Layout
                            //        {
                            //            xif.transform.SetParent(valueG.transform);
                            //            decrease.transform.SetParent(valueG.transform);
                            //            increase.transform.SetParent(valueG.transform);

                            //            if (commands[0] == "playSound" || commands[0] == "loadLevel")
                            //            {
                            //                Destroy(decrease.gameObject);
                            //                Destroy(increase.gameObject);
                            //                input.GetComponent<RectTransform>().sizeDelta = new Vector2(170f, 32f);
                            //            }
                            //        }

                            //        //Value Label
                            //        {
                            //            var l = Instantiate(label);
                            //            l.name = "label";
                            //            l.transform.SetParent(valueG.transform);
                            //            l.transform.SetAsFirstSibling();
                            //            l.transform.localScale = Vector3.one;
                            //            if (commands[0] == "particleSystem")
                            //            {
                            //                l.transform.GetChild(0).GetComponent<Text>().text = "LifeTime";
                            //            }
                            //            else if (commands[0] == "trailRenderer")
                            //            {
                            //                l.transform.GetChild(0).GetComponent<Text>().text = "Time";
                            //            }
                            //            else
                            //            {
                            //                l.transform.GetChild(0).GetComponent<Text>().text = "Value";
                            //            }
                            //            l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(107f, 20f);
                            //            l.transform.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                            //            var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
                            //            {
                            //                ltextrt.anchoredPosition = new Vector2(10f, -5f);
                            //            }
                            //        }

                            //        //Constant
                            //        {
                            //            var constantG = new GameObject("constant");
                            //            {
                            //                constantG.transform.SetParent(layout.transform);
                            //                constantG.transform.localScale = Vector3.one;
                            //                constantG.transform.SetAsFirstSibling();

                            //                var valueGRT = constantG.AddComponent<RectTransform>();
                            //                var valueGHLG = constantG.AddComponent<HorizontalLayoutGroup>();

                            //                valueGHLG.childControlHeight = false;
                            //                valueGHLG.childControlWidth = false;
                            //                valueGHLG.childForceExpandWidth = false;
                            //                valueGHLG.spacing = 8f;
                            //            }

                            //            var bo = Instantiate(boolInput);
                            //            {
                            //                bo.transform.SetParent(constantG.transform);
                            //                bo.transform.localScale = Vector3.one;

                            //                if (bo.GetComponent<Toggle>())
                            //                {
                            //                    var toggle = bo.GetComponent<Toggle>();
                            //                    toggle.onValueChanged.RemoveAllListeners();
                            //                    toggle.isOn = bool.Parse(constant);
                            //                    toggle.onValueChanged.AddListener(delegate (bool _val)
                            //                    {
                            //                        modifier.GetType().GetField("constant", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, _val);
                            //                    });
                            //                }
                            //            }

                            //            //Constant Label
                            //            {
                            //                var l = Instantiate(label);
                            //                l.name = "label";
                            //                l.transform.SetParent(constantG.transform);
                            //                l.transform.SetAsFirstSibling();
                            //                l.transform.localScale = Vector3.one;
                            //                l.transform.GetChild(0).GetComponent<Text>().text = "Constant";
                            //                l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(247f, 20f);
                            //                l.transform.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                            //                var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
                            //                {
                            //                    ltextrt.anchoredPosition = new Vector2(10f, -5f);
                            //                }
                            //            }
                            //        }

                            //        //NotGate
                            //        if (type == 0)
                            //        {
                            //            var constantG = new GameObject("not");
                            //            {
                            //                constantG.transform.SetParent(layout.transform);
                            //                constantG.transform.localScale = Vector3.one;
                            //                constantG.transform.SetAsFirstSibling();

                            //                var valueGRT = constantG.AddComponent<RectTransform>();
                            //                var valueGHLG = constantG.AddComponent<HorizontalLayoutGroup>();

                            //                valueGHLG.childControlHeight = false;
                            //                valueGHLG.childControlWidth = false;
                            //                valueGHLG.childForceExpandWidth = false;
                            //                valueGHLG.spacing = 8f;
                            //            }

                            //            var bo = Instantiate(boolInput);
                            //            {
                            //                bo.transform.SetParent(constantG.transform);
                            //                bo.transform.localScale = Vector3.one;

                            //                if (bo.GetComponent<Toggle>())
                            //                {
                            //                    var toggle = bo.GetComponent<Toggle>();
                            //                    toggle.onValueChanged.RemoveAllListeners();
                            //                    toggle.isOn = bool.Parse(notGate);
                            //                    toggle.onValueChanged.AddListener(delegate (bool _val)
                            //                    {
                            //                        modifier.GetType().GetField("not", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, _val);
                            //                    });
                            //                }
                            //            }

                            //            //Constant Label
                            //            {
                            //                var l = Instantiate(label);
                            //                l.name = "label";
                            //                l.transform.SetParent(constantG.transform);
                            //                l.transform.SetAsFirstSibling();
                            //                l.transform.localScale = Vector3.one;
                            //                l.transform.GetChild(0).GetComponent<Text>().text = "Not";
                            //                l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(247f, 20f);
                            //                l.transform.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                            //                var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
                            //                {
                            //                    ltextrt.anchoredPosition = new Vector2(10f, -5f);
                            //                }
                            //            }
                            //        }

                            //        //playSound settings
                            //        if (commands[0] == "playSound")
                            //        {
                            //            if (commands.Count == 1)
                            //            {
                            //                commands.Add("False");
                            //                commands.Add("1");
                            //                modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                            //            }

                            //            xRT.sizeDelta = new Vector2(350f, 224f);
                            //            layout.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 60f);

                            //            //Global playSound (Takes from soundlibrary if true
                            //            {
                            //                var constantG = new GameObject("global");
                            //                {
                            //                    constantG.transform.SetParent(layout.transform);
                            //                    constantG.transform.localScale = Vector3.one;

                            //                    var valueGRT = constantG.AddComponent<RectTransform>();
                            //                    var valueGHLG = constantG.AddComponent<HorizontalLayoutGroup>();

                            //                    valueGHLG.childControlHeight = false;
                            //                    valueGHLG.childControlWidth = false;
                            //                    valueGHLG.childForceExpandWidth = false;
                            //                    valueGHLG.spacing = 8f;
                            //                }

                            //                var bo = Instantiate(boolInput);
                            //                {
                            //                    bo.transform.SetParent(constantG.transform);
                            //                    bo.transform.localScale = Vector3.one;

                            //                    if (bo.GetComponent<Toggle>())
                            //                    {
                            //                        var toggle = bo.GetComponent<Toggle>();
                            //                        toggle.onValueChanged.RemoveAllListeners();
                            //                        toggle.isOn = bool.Parse(commands[1]);
                            //                        toggle.onValueChanged.AddListener(delegate (bool _val)
                            //                        {
                            //                            commands[1] = _val.ToString();
                            //                            modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                            //                        });
                            //                    }
                            //                }

                            //                //Constant Label
                            //                {
                            //                    var l = Instantiate(label);
                            //                    l.name = "label";
                            //                    l.transform.SetParent(constantG.transform);
                            //                    l.transform.SetAsFirstSibling();
                            //                    l.transform.localScale = Vector3.one;
                            //                    l.transform.GetChild(0).GetComponent<Text>().text = "Global";
                            //                    l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(247f, 20f);
                            //                    l.transform.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                            //                    var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
                            //                    {
                            //                        ltextrt.anchoredPosition = new Vector2(10f, -5f);
                            //                    }
                            //                }
                            //            }

                            //            //Pitch (multiplies by current global pitch)
                            //            {
                            //                GameObject p = Instantiate(singleInput);
                            //                p.transform.SetParent(content);
                            //                p.name = "pitch";

                            //                //Destroy
                            //                {
                            //                    if (p.GetComponent<EventInfo>())
                            //                        Destroy(p.GetComponent<EventInfo>());

                            //                    if (p.GetComponent<HorizontalLayoutGroup>())
                            //                        Destroy(p.GetComponent<HorizontalLayoutGroup>());

                            //                    if (p.GetComponent<InputField>())
                            //                        Destroy(p.GetComponent<InputField>());

                            //                    if (p.GetComponent<InputFieldHelper>())
                            //                        Destroy(p.GetComponent<InputFieldHelper>());

                            //                    if (p.GetComponent<EventTrigger>())
                            //                        Destroy(p.GetComponent<EventTrigger>());
                            //                }

                            //                p.transform.localScale = Vector3.one;
                            //                p.transform.GetChild(0).localScale = Vector3.one;

                            //                var pRT = p.GetComponent<RectTransform>();
                            //                {
                            //                    pRT.sizeDelta = new Vector2(350f, 128f);
                            //                }

                            //                var ppinput = p.transform.Find("input");
                            //                var ppif = ppinput.gameObject.AddComponent<InputField>();
                            //                {
                            //                    ppif.onValueChanged.RemoveAllListeners();
                            //                    ppif.characterValidation = InputField.CharacterValidation.None;
                            //                    ppif.characterLimit = 0;
                            //                    ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                            //                    ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                            //                    ppif.text = commands[2];
                            //                    ppif.onValueChanged.AddListener(delegate (string _val)
                            //                    {
                            //                        if (float.TryParse(_val, out float num))
                            //                        {
                            //                            commands[2] = num.ToString();
                            //                            modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                            //                        }
                            //                    });
                            //                }

                            //                var xet = ppinput.gameObject.AddComponent<EventTrigger>();
                            //                xet.triggers.Clear();
                            //                xet.triggers.Add(Triggers.ScrollDelta(ppif, 0.1f, 10f));

                            //                var xifh = ppinput.gameObject.AddComponent<InputFieldHelper>();
                            //                xifh.inputField = ppif;

                            //                var ppincrease = p.transform.Find(">").GetComponent<Button>();
                            //                {
                            //                    increase.onClick.RemoveAllListeners();
                            //                    increase.onClick.AddListener(delegate ()
                            //                    {
                            //                        ppif.text = (float.Parse(ppif.text) + 0.1f).ToString();
                            //                    });
                            //                }

                            //                var ppdecrease = p.transform.Find("<").GetComponent<Button>();
                            //                {
                            //                    decrease.onClick.RemoveAllListeners();
                            //                    decrease.onClick.AddListener(delegate ()
                            //                    {
                            //                        xif.text = (float.Parse(ppif.text) - 0.1f).ToString();
                            //                    });
                            //                }

                            //                var ppvalueG = new GameObject("pitch");
                            //                {
                            //                    ppvalueG.transform.SetParent(layout.transform);
                            //                    ppvalueG.transform.localScale = Vector3.one;

                            //                    var valueGRT = ppvalueG.AddComponent<RectTransform>();
                            //                    var valueGHLG = ppvalueG.AddComponent<HorizontalLayoutGroup>();

                            //                    valueGHLG.childControlHeight = false;
                            //                    valueGHLG.childControlWidth = false;
                            //                    valueGHLG.childForceExpandWidth = false;
                            //                    valueGHLG.spacing = 8f;
                            //                }

                            //                //Layout
                            //                {
                            //                    ppif.transform.SetParent(ppvalueG.transform);
                            //                    ppdecrease.transform.SetParent(ppvalueG.transform);
                            //                    ppincrease.transform.SetParent(ppvalueG.transform);
                            //                }

                            //                //Value Label
                            //                {
                            //                    var l = Instantiate(label);
                            //                    l.name = "label";
                            //                    l.transform.SetParent(ppvalueG.transform);
                            //                    l.transform.SetAsFirstSibling();
                            //                    l.transform.localScale = Vector3.one;
                            //                    l.transform.GetChild(0).GetComponent<Text>().text = "Pitch";
                            //                    l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(107f, 20f);
                            //                    l.transform.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                            //                    var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
                            //                    {
                            //                        ltextrt.anchoredPosition = new Vector2(10f, -5f);
                            //                    }
                            //                }

                            //                Destroy(p);
                            //            }
                            //        }

                            //        if (commands[0] == "trailRenderer")
                            //        {
                            //            xRT.sizeDelta = new Vector2(350f, 352f);
                            //            layout.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 140f);

                            //            //startWidth
                            //            {
                            //                var w = Instantiate(valueG);
                            //                w.transform.SetParent(layout.transform);
                            //                w.transform.localScale = Vector3.one;

                            //                w.name = "startWidth";

                            //                w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "StartWidth";

                            //                var ppinput = w.transform.Find("input");
                            //                var ppif = ppinput.gameObject.GetComponent<InputField>();
                            //                {
                            //                    ppif.onValueChanged.RemoveAllListeners();
                            //                    ppif.characterValidation = InputField.CharacterValidation.None;
                            //                    ppif.characterLimit = 0;
                            //                    ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                            //                    ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                            //                    ppif.text = commands[1];
                            //                    ppif.onValueChanged.AddListener(delegate (string _val)
                            //                    {
                            //                        if (float.TryParse(_val, out float num))
                            //                        {
                            //                            commands[1] = num.ToString();
                            //                            modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                            //                        }
                            //                    });
                            //                }

                            //                var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                            //                ppet.triggers.Clear();
                            //                ppet.triggers.Add(Triggers.ScrollDelta(ppif, 0.1f, 10f));
                            //            }

                            //            //endWidth
                            //            {
                            //                var w = Instantiate(valueG);
                            //                w.transform.SetParent(layout.transform);
                            //                w.transform.localScale = Vector3.one;

                            //                w.name = "endWidth";

                            //                w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "EndWidth";


                            //                var ppinput = w.transform.Find("input");
                            //                var ppif = ppinput.gameObject.GetComponent<InputField>();
                            //                {
                            //                    ppif.onValueChanged.RemoveAllListeners();
                            //                    ppif.characterValidation = InputField.CharacterValidation.None;
                            //                    ppif.characterLimit = 0;
                            //                    ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                            //                    ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                            //                    ppif.text = commands[2];
                            //                    ppif.onValueChanged.AddListener(delegate (string _val)
                            //                    {
                            //                        if (float.TryParse(_val, out float num))
                            //                        {
                            //                            commands[2] = num.ToString();
                            //                            modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                            //                        }
                            //                    });
                            //                }

                            //                var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                            //                ppet.triggers.Clear();
                            //                ppet.triggers.Add(Triggers.ScrollDelta(ppif, 0.1f, 10f));
                            //            }

                            //            //startColor
                            //            {
                            //                var w = Instantiate(valueG);
                            //                w.transform.SetParent(layout.transform);
                            //                w.transform.localScale = Vector3.one;

                            //                w.name = "startColor";

                            //                w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "StartColor";


                            //                var ppinput = w.transform.Find("input");
                            //                var ppif = ppinput.gameObject.GetComponent<InputField>();
                            //                {
                            //                    ppif.onValueChanged.RemoveAllListeners();
                            //                    ppif.characterValidation = InputField.CharacterValidation.Integer;
                            //                    ppif.characterLimit = 0;
                            //                    ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                            //                    ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                            //                    ppif.text = commands[3];
                            //                    ppif.onValueChanged.AddListener(delegate (string _val)
                            //                    {
                            //                        if (float.TryParse(_val, out float num))
                            //                        {
                            //                            commands[3] = ((int)num).ToString();
                            //                            modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                            //                        }
                            //                    });
                            //                }

                            //                var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                            //                ppet.triggers.Clear();
                            //                ppet.triggers.Add(Triggers.ScrollDeltaInt(ppif, 1, false, new List<int> { 0, GameManager.inst.LiveTheme.objectColors.Count - 1 }));
                            //            }

                            //            //endColor
                            //            {
                            //                var w = Instantiate(valueG);
                            //                w.transform.SetParent(layout.transform);
                            //                w.transform.localScale = Vector3.one;

                            //                w.name = "endColor";

                            //                w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "EndColor";


                            //                var ppinput = w.transform.Find("input");
                            //                var ppif = ppinput.gameObject.GetComponent<InputField>();
                            //                {
                            //                    ppif.onValueChanged.RemoveAllListeners();
                            //                    ppif.characterValidation = InputField.CharacterValidation.Integer;
                            //                    ppif.characterLimit = 0;
                            //                    ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                            //                    ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                            //                    ppif.text = commands[5];
                            //                    ppif.onValueChanged.AddListener(delegate (string _val)
                            //                    {
                            //                        if (float.TryParse(_val, out float num))
                            //                        {
                            //                            commands[5] = ((int)num).ToString();
                            //                            modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                            //                        }
                            //                    });
                            //                }

                            //                var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                            //                ppet.triggers.Clear();
                            //                ppet.triggers.Add(Triggers.ScrollDeltaInt(ppif, 1, false, new List<int> { 0, GameManager.inst.LiveTheme.objectColors.Count - 1 }));
                            //            }

                            //            //startOpacity
                            //            {
                            //                var w = Instantiate(valueG);
                            //                w.transform.SetParent(layout.transform);
                            //                w.transform.localScale = Vector3.one;

                            //                w.name = "startOpacity";

                            //                w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "StartAlpha";


                            //                var ppinput = w.transform.Find("input");
                            //                var ppif = ppinput.gameObject.GetComponent<InputField>();
                            //                {
                            //                    ppif.onValueChanged.RemoveAllListeners();
                            //                    ppif.characterValidation = InputField.CharacterValidation.None;
                            //                    ppif.characterLimit = 0;
                            //                    ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                            //                    ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                            //                    ppif.text = commands[4];
                            //                    ppif.onValueChanged.AddListener(delegate (string _val)
                            //                    {
                            //                        if (float.TryParse(_val, out float num))
                            //                        {
                            //                            commands[4] = num.ToString();
                            //                            modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                            //                        }
                            //                    });
                            //                }

                            //                var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                            //                ppet.triggers.Clear();
                            //                ppet.triggers.Add(Triggers.ScrollDelta(ppif, 0.1f, 10f));
                            //            }

                            //            //endOpacity
                            //            {
                            //                var w = Instantiate(valueG);
                            //                w.transform.SetParent(layout.transform);
                            //                w.transform.localScale = Vector3.one;

                            //                w.name = "endOpacity";

                            //                w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "EndAlpha";


                            //                var ppinput = w.transform.Find("input");
                            //                var ppif = ppinput.gameObject.GetComponent<InputField>();
                            //                {
                            //                    ppif.onValueChanged.RemoveAllListeners();
                            //                    ppif.characterValidation = InputField.CharacterValidation.None;
                            //                    ppif.characterLimit = 0;
                            //                    ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                            //                    ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                            //                    ppif.text = commands[6];
                            //                    ppif.onValueChanged.AddListener(delegate (string _val)
                            //                    {
                            //                        if (float.TryParse(_val, out float num))
                            //                        {
                            //                            commands[6] = num.ToString();
                            //                            modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                            //                        }
                            //                    });
                            //                }

                            //                var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                            //                ppet.triggers.Clear();
                            //                ppet.triggers.Add(Triggers.ScrollDelta(ppif, 0.1f, 10f));
                            //            }
                            //        }

                            //        if (commands[0] == "particleSystem")
                            //        {
                            //            xRT.sizeDelta = new Vector2(350f, 640f);
                            //            layout.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 280f);

                            //            //Shape
                            //            {
                            //                var w = Instantiate(valueG);
                            //                w.transform.SetParent(layout.transform);
                            //                w.transform.localScale = Vector3.one;

                            //                w.name = "shape";

                            //                w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Shape";

                            //                var ppinput = w.transform.Find("input");
                            //                var ppif = ppinput.gameObject.GetComponent<InputField>();
                            //                {
                            //                    ppif.onValueChanged.RemoveAllListeners();
                            //                    ppif.characterValidation = InputField.CharacterValidation.Integer;
                            //                    ppif.characterLimit = 0;
                            //                    ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                            //                    ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                            //                    ppif.text = commands[1];
                            //                    ppif.onValueChanged.AddListener(delegate (string _val)
                            //                    {
                            //                        if (int.TryParse(_val, out int num))
                            //                        {
                            //                            commands[1] = num.ToString();
                            //                            commands[2] = "0";
                            //                            modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                            //                            ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                            //                            RTEditor.inst.StartCoroutine(RTEditor.RefreshObjectGUI());
                            //                        }
                            //                    });
                            //                }

                            //                var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                            //                ppet.triggers.Clear();
                            //                ppet.triggers.Add(Triggers.ScrollDeltaInt(ppif, 1, false, new List<int> { 0, ObjectManager.inst.objectPrefabs.Count - 1 }));
                            //            }

                            //            //Shape Option
                            //            {
                            //                var w = Instantiate(valueG);
                            //                w.transform.SetParent(layout.transform);
                            //                w.transform.localScale = Vector3.one;

                            //                w.name = "shapeOpt";

                            //                w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "ShapeOpt";

                            //                var ppinput = w.transform.Find("input");
                            //                var ppif = ppinput.gameObject.GetComponent<InputField>();
                            //                {
                            //                    ppif.onValueChanged.RemoveAllListeners();
                            //                    ppif.characterValidation = InputField.CharacterValidation.Integer;
                            //                    ppif.characterLimit = 0;
                            //                    ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                            //                    ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                            //                    ppif.text = commands[2];
                            //                    ppif.onValueChanged.AddListener(delegate (string _val)
                            //                    {
                            //                        if (int.TryParse(_val, out int num))
                            //                        {
                            //                            commands[2] = num.ToString();
                            //                            modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                            //                            ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                            //                        }
                            //                    });
                            //                }

                            //                var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                            //                ppet.triggers.Clear();
                            //                ppet.triggers.Add(Triggers.ScrollDeltaInt(ppif, 1, false, new List<int> { 0, ObjectManager.inst.objectPrefabs[int.Parse(commands[1])].options.Count - 1 }));
                            //            }

                            //            //Color
                            //            {
                            //                var w = Instantiate(valueG);
                            //                w.transform.SetParent(layout.transform);
                            //                w.transform.localScale = Vector3.one;

                            //                w.name = "color";

                            //                w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Color";


                            //                var ppinput = w.transform.Find("input");
                            //                var ppif = ppinput.gameObject.GetComponent<InputField>();
                            //                {
                            //                    ppif.onValueChanged.RemoveAllListeners();
                            //                    ppif.characterValidation = InputField.CharacterValidation.Integer;
                            //                    ppif.characterLimit = 0;
                            //                    ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                            //                    ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                            //                    ppif.text = commands[3];
                            //                    ppif.onValueChanged.AddListener(delegate (string _val)
                            //                    {
                            //                        if (float.TryParse(_val, out float num))
                            //                        {
                            //                            commands[3] = ((int)num).ToString();
                            //                            modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                            //                            ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                            //                        }
                            //                    });
                            //                }

                            //                var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                            //                ppet.triggers.Clear();
                            //                ppet.triggers.Add(Triggers.ScrollDeltaInt(ppif, 1, false, new List<int> { 0, GameManager.inst.LiveTheme.objectColors.Count - 1 }));
                            //            }

                            //            //startOpacity
                            //            {
                            //                var w = Instantiate(valueG);
                            //                w.transform.SetParent(layout.transform);
                            //                w.transform.localScale = Vector3.one;

                            //                w.name = "startOpacity";

                            //                w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "StartAlpha";


                            //                var ppinput = w.transform.Find("input");
                            //                var ppif = ppinput.gameObject.GetComponent<InputField>();
                            //                {
                            //                    ppif.onValueChanged.RemoveAllListeners();
                            //                    ppif.characterValidation = InputField.CharacterValidation.None;
                            //                    ppif.characterLimit = 0;
                            //                    ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                            //                    ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                            //                    ppif.text = commands[4];
                            //                    ppif.onValueChanged.AddListener(delegate (string _val)
                            //                    {
                            //                        if (float.TryParse(_val, out float num))
                            //                        {
                            //                            commands[4] = num.ToString();
                            //                            modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                            //                            ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                            //                        }
                            //                    });
                            //                }

                            //                var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                            //                ppet.triggers.Clear();
                            //                ppet.triggers.Add(Triggers.ScrollDelta(ppif, 0.1f, 10f));
                            //            }

                            //            //endOpacity
                            //            {
                            //                var w = Instantiate(valueG);
                            //                w.transform.SetParent(layout.transform);
                            //                w.transform.localScale = Vector3.one;

                            //                w.name = "endOpacity";

                            //                w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "EndAlpha";


                            //                var ppinput = w.transform.Find("input");
                            //                var ppif = ppinput.gameObject.GetComponent<InputField>();
                            //                {
                            //                    ppif.onValueChanged.RemoveAllListeners();
                            //                    ppif.characterValidation = InputField.CharacterValidation.None;
                            //                    ppif.characterLimit = 0;
                            //                    ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                            //                    ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                            //                    ppif.text = commands[5];
                            //                    ppif.onValueChanged.AddListener(delegate (string _val)
                            //                    {
                            //                        if (float.TryParse(_val, out float num))
                            //                        {
                            //                            commands[5] = num.ToString();
                            //                            modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                            //                            ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                            //                        }
                            //                    });
                            //                }

                            //                var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                            //                ppet.triggers.Clear();
                            //                ppet.triggers.Add(Triggers.ScrollDelta(ppif, 0.1f, 10f));
                            //            }

                            //            //startScale
                            //            {
                            //                var w = Instantiate(valueG);
                            //                w.transform.SetParent(layout.transform);
                            //                w.transform.localScale = Vector3.one;

                            //                w.name = "startScale";

                            //                w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "StartScale";


                            //                var ppinput = w.transform.Find("input");
                            //                var ppif = ppinput.gameObject.GetComponent<InputField>();
                            //                {
                            //                    ppif.onValueChanged.RemoveAllListeners();
                            //                    ppif.characterValidation = InputField.CharacterValidation.None;
                            //                    ppif.characterLimit = 0;
                            //                    ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                            //                    ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                            //                    ppif.text = commands[6];
                            //                    ppif.onValueChanged.AddListener(delegate (string _val)
                            //                    {
                            //                        if (float.TryParse(_val, out float num))
                            //                        {
                            //                            commands[6] = num.ToString();
                            //                            modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                            //                            ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                            //                        }
                            //                    });
                            //                }

                            //                var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                            //                ppet.triggers.Clear();
                            //                ppet.triggers.Add(Triggers.ScrollDelta(ppif, 0.1f, 10f));
                            //            }

                            //            //endScale
                            //            {
                            //                var w = Instantiate(valueG);
                            //                w.transform.SetParent(layout.transform);
                            //                w.transform.localScale = Vector3.one;

                            //                w.name = "endOpacity";

                            //                w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "EndScale";


                            //                var ppinput = w.transform.Find("input");
                            //                var ppif = ppinput.gameObject.GetComponent<InputField>();
                            //                {
                            //                    ppif.onValueChanged.RemoveAllListeners();
                            //                    ppif.characterValidation = InputField.CharacterValidation.None;
                            //                    ppif.characterLimit = 0;
                            //                    ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                            //                    ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                            //                    ppif.text = commands[7];
                            //                    ppif.onValueChanged.AddListener(delegate (string _val)
                            //                    {
                            //                        if (float.TryParse(_val, out float num))
                            //                        {
                            //                            commands[7] = num.ToString();
                            //                            modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                            //                            ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                            //                        }
                            //                    });
                            //                }

                            //                var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                            //                ppet.triggers.Clear();
                            //                ppet.triggers.Add(Triggers.ScrollDelta(ppif, 0.1f, 10f));
                            //            }

                            //            //rotation
                            //            {
                            //                var w = Instantiate(valueG);
                            //                w.transform.SetParent(layout.transform);
                            //                w.transform.localScale = Vector3.one;

                            //                w.name = "rotation";

                            //                w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Rotation";


                            //                var ppinput = w.transform.Find("input");
                            //                var ppif = ppinput.gameObject.GetComponent<InputField>();
                            //                {
                            //                    ppif.onValueChanged.RemoveAllListeners();
                            //                    ppif.characterValidation = InputField.CharacterValidation.None;
                            //                    ppif.characterLimit = 0;
                            //                    ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                            //                    ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                            //                    ppif.text = commands[8];
                            //                    ppif.onValueChanged.AddListener(delegate (string _val)
                            //                    {
                            //                        if (float.TryParse(_val, out float num))
                            //                        {
                            //                            commands[8] = num.ToString();
                            //                            modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                            //                            ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                            //                        }
                            //                    });
                            //                }

                            //                var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                            //                ppet.triggers.Clear();
                            //                ppet.triggers.Add(Triggers.ScrollDelta(ppif, 0.1f, 10f));
                            //            }

                            //            //speed
                            //            {
                            //                var w = Instantiate(valueG);
                            //                w.transform.SetParent(layout.transform);
                            //                w.transform.localScale = Vector3.one;

                            //                w.name = "speed";

                            //                w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Speed";


                            //                var ppinput = w.transform.Find("input");
                            //                var ppif = ppinput.gameObject.GetComponent<InputField>();
                            //                {
                            //                    ppif.onValueChanged.RemoveAllListeners();
                            //                    ppif.characterValidation = InputField.CharacterValidation.None;
                            //                    ppif.characterLimit = 0;
                            //                    ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                            //                    ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                            //                    ppif.text = commands[9];
                            //                    ppif.onValueChanged.AddListener(delegate (string _val)
                            //                    {
                            //                        if (float.TryParse(_val, out float num))
                            //                        {
                            //                            commands[9] = num.ToString();
                            //                            modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                            //                            ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                            //                        }
                            //                    });
                            //                }

                            //                var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                            //                ppet.triggers.Clear();
                            //                ppet.triggers.Add(Triggers.ScrollDelta(ppif, 0.1f, 10f));
                            //            }

                            //            //Amount
                            //            {
                            //                var w = Instantiate(valueG);
                            //                w.transform.SetParent(layout.transform);
                            //                w.transform.localScale = Vector3.one;

                            //                w.name = "amount";

                            //                w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Amount";


                            //                var ppinput = w.transform.Find("input");
                            //                var ppif = ppinput.gameObject.GetComponent<InputField>();
                            //                {
                            //                    ppif.onValueChanged.RemoveAllListeners();
                            //                    ppif.characterValidation = InputField.CharacterValidation.Integer;
                            //                    ppif.characterLimit = 0;
                            //                    ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                            //                    ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                            //                    ppif.text = commands[10];
                            //                    ppif.onValueChanged.AddListener(delegate (string _val)
                            //                    {
                            //                        if (float.TryParse(_val, out float num))
                            //                        {
                            //                            commands[10] = ((int)num).ToString();
                            //                            modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                            //                            ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                            //                        }
                            //                    });
                            //                }

                            //                var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                            //                ppet.triggers.Clear();
                            //                ppet.triggers.Add(Triggers.ScrollDeltaInt(ppif, 1));
                            //            }

                            //            //Duration
                            //            {
                            //                var w = Instantiate(valueG);
                            //                w.transform.SetParent(layout.transform);
                            //                w.transform.localScale = Vector3.one;

                            //                w.name = "duration";

                            //                w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Duration";


                            //                var ppinput = w.transform.Find("input");
                            //                var ppif = ppinput.gameObject.GetComponent<InputField>();
                            //                {
                            //                    ppif.onValueChanged.RemoveAllListeners();
                            //                    ppif.characterValidation = InputField.CharacterValidation.None;
                            //                    ppif.characterLimit = 0;
                            //                    ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                            //                    ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                            //                    ppif.text = commands[11];
                            //                    ppif.onValueChanged.AddListener(delegate (string _val)
                            //                    {
                            //                        if (float.TryParse(_val, out float num))
                            //                        {
                            //                            commands[11] = num.ToString();
                            //                            modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                            //                            ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                            //                        }
                            //                    });
                            //                }

                            //                var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                            //                ppet.triggers.Clear();
                            //                ppet.triggers.Add(Triggers.ScrollDelta(ppif, 0.1f, 10f));
                            //            }

                            //            //Force X
                            //            {
                            //                var w = Instantiate(valueG);
                            //                w.transform.SetParent(layout.transform);
                            //                w.transform.localScale = Vector3.one;

                            //                w.name = "force x";

                            //                w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Force X";


                            //                var ppinput = w.transform.Find("input");
                            //                var ppif = ppinput.gameObject.GetComponent<InputField>();
                            //                {
                            //                    ppif.onValueChanged.RemoveAllListeners();
                            //                    ppif.characterValidation = InputField.CharacterValidation.None;
                            //                    ppif.characterLimit = 0;
                            //                    ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                            //                    ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                            //                    ppif.text = commands[12];
                            //                    ppif.onValueChanged.AddListener(delegate (string _val)
                            //                    {
                            //                        if (float.TryParse(_val, out float num))
                            //                        {
                            //                            commands[12] = num.ToString();
                            //                            modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                            //                            ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                            //                        }
                            //                    });
                            //                }

                            //                var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                            //                ppet.triggers.Clear();
                            //                ppet.triggers.Add(Triggers.ScrollDelta(ppif, 0.1f, 10f));
                            //            }

                            //            //Force Y
                            //            {
                            //                var w = Instantiate(valueG);
                            //                w.transform.SetParent(layout.transform);
                            //                w.transform.localScale = Vector3.one;

                            //                w.name = "force y";

                            //                w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Force Y";


                            //                var ppinput = w.transform.Find("input");
                            //                var ppif = ppinput.gameObject.GetComponent<InputField>();
                            //                {
                            //                    ppif.onValueChanged.RemoveAllListeners();
                            //                    ppif.characterValidation = InputField.CharacterValidation.None;
                            //                    ppif.characterLimit = 0;
                            //                    ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                            //                    ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                            //                    ppif.text = commands[13];
                            //                    ppif.onValueChanged.AddListener(delegate (string _val)
                            //                    {
                            //                        if (float.TryParse(_val, out float num))
                            //                        {
                            //                            commands[13] = num.ToString();
                            //                            modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                            //                            ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                            //                        }
                            //                    });
                            //                }

                            //                var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                            //                ppet.triggers.Clear();
                            //                ppet.triggers.Add(Triggers.ScrollDelta(ppif, 0.1f, 10f));
                            //            }

                            //            //Trail Emit
                            //            {
                            //                var w = Instantiate(layout.transform.Find("constant").gameObject);
                            //                w.transform.SetParent(layout.transform);
                            //                w.transform.localScale = Vector3.one;
                            //                w.name = "trail";

                            //                w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Trail";

                            //                var tog = w.transform.GetChild(1).GetComponent<Toggle>();
                            //                tog.onValueChanged.RemoveAllListeners();
                            //                tog.isOn = bool.Parse(commands[14]);
                            //                tog.onValueChanged.AddListener(delegate (bool _val)
                            //                {
                            //                    commands[14] = _val.ToString();
                            //                    modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                            //                    ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                            //                });
                            //            }
                            //        }

                            //        if (commands[0].Contains("keyPress"))
                            //        {
                            //            xRT.sizeDelta = new Vector2(350f, 160f);
                            //            layout.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 40f);

                            //            Destroy(input.gameObject);
                            //            Destroy(increase.gameObject);
                            //            Destroy(decrease.gameObject);

                            //            var dd = Instantiate(dropdownInput);
                            //            dd.transform.SetParent(valueG.transform);
                            //            dd.transform.localScale = Vector3.one;

                            //            Destroy(dd.GetComponent<HoverTooltip>());
                            //            Destroy(dd.GetComponent<HideDropdownOptions>());

                            //            var d = dd.GetComponent<Dropdown>();
                            //            d.onValueChanged.RemoveAllListeners();
                            //            d.options.Clear();

                            //            string[] PieceTypeNames = Enum.GetNames(typeof(KeyCode));
                            //            for (int i = 0; i < PieceTypeNames.Length; i++)
                            //            {
                            //                d.options.Add(new Dropdown.OptionData(((KeyCode)i).ToString()));
                            //            }

                            //            d.value = int.Parse(value);

                            //            d.onValueChanged.AddListener(delegate (int _val)
                            //            {
                            //                modifier.GetType().GetField("value", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, _val.ToString());
                            //            });
                            //        }

                            //        if (commands[0].Contains("Variable"))
                            //        {
                            //            var w = Instantiate(valueG);
                            //            w.transform.SetParent(layout.transform);
                            //            w.transform.localScale = Vector3.one;

                            //            w.name = "variable-object";

                            //            w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Objects Name";

                            //            Destroy(w.transform.Find("<").gameObject);
                            //            Destroy(w.transform.Find(">").gameObject);

                            //            var ppinput = w.transform.Find("input").GetComponent<InputField>();
                            //            ppinput.onValueChanged.RemoveAllListeners();
                            //            ppinput.text = commands[1];
                            //            ppinput.onValueChanged.AddListener(delegate (string _val)
                            //            {
                            //                commands[1] = _val;
                            //                modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                            //            });
                            //        }

                            //        //Delete Modifier
                            //        {
                            //            int tmpIndex = j;

                            //            var delete = Instantiate(close.gameObject);
                            //            delete.transform.SetParent(x.transform);
                            //            delete.transform.localScale = Vector3.one;
                            //            //delete.GetComponent<RectTransform>().anchoredPosition = new Vector2(333f, 0f);
                            //            delete.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                            //            delete.name = "delete";

                            //            var deleteButton = delete.GetComponent<Button>();
                            //            deleteButton.onClick.m_Calls.m_ExecutingCalls.Clear();
                            //            deleteButton.onClick.m_Calls.m_PersistentCalls.Clear();
                            //            deleteButton.onClick.m_PersistentCalls.m_Calls.Clear();
                            //            deleteButton.onClick.RemoveAllListeners();
                            //            deleteButton.onClick.AddListener(delegate ()
                            //            {
                            //                RemoveModifierIndex(beatmapObject, tmpIndex);
                            //                RTEditor.inst.StartCoroutine(RTEditor.RefreshObjectGUI());
                            //            });
                            //        }
                            //    }
                            //    else
                            //    {
                            //        GameObject x = Instantiate(singleInput);
                            //        x.transform.SetParent(content);
                            //        x.name = commands[0];

                            //        //Main Label
                            //        {
                            //            var l = Instantiate(label);
                            //            l.name = "label";
                            //            l.transform.SetParent(x.transform);
                            //            l.transform.SetAsFirstSibling();
                            //            l.transform.localScale = Vector3.one;
                            //            l.transform.GetChild(0).GetComponent<Text>().text = commands[0];
                            //            l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(187f, 20f);
                            //            l.transform.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                            //            var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
                            //            {
                            //                ltextrt.anchoredPosition = new Vector2(10f, -5f);
                            //            }
                            //        }

                            //        x.transform.localScale = Vector3.one;
                            //        x.transform.GetChild(0).localScale = Vector3.one;

                            //        var xRT = x.GetComponent<RectTransform>();
                            //        {
                            //            xRT.sizeDelta = new Vector2(350f, 128f);
                            //        }

                            //        x.GetComponent<Image>().enabled = true;
                            //        x.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);

                            //        Triggers.AddTooltip(x, commands[0], "");

                            //        //Destroy
                            //        {
                            //            if (x.GetComponent<EventInfo>())
                            //                Destroy(x.GetComponent<EventInfo>());

                            //            if (x.GetComponent<HorizontalLayoutGroup>())
                            //                Destroy(x.GetComponent<HorizontalLayoutGroup>());

                            //            if (x.GetComponent<InputField>())
                            //                Destroy(x.GetComponent<InputField>());

                            //            if (x.GetComponent<InputFieldHelper>())
                            //                Destroy(x.GetComponent<InputFieldHelper>());

                            //            if (x.GetComponent<EventTrigger>())
                            //                Destroy(x.GetComponent<EventTrigger>());

                            //            if (!commands[0].Contains("playerWarp"))
                            //            {
                            //                if (x.transform.Find("input"))
                            //                    Destroy(x.transform.Find("input").gameObject);

                            //                if (x.transform.Find("<"))
                            //                    Destroy(x.transform.Find("<").gameObject);

                            //                if (x.transform.Find(">"))
                            //                    Destroy(x.transform.Find(">").gameObject);
                            //            }
                            //        }

                            //        var layout = new GameObject("layout");
                            //        {
                            //            layout.transform.SetParent(x.transform);
                            //            layout.transform.localScale = Vector3.one;

                            //            var layoutRT = layout.AddComponent<RectTransform>();
                            //            var layoutVLG = layout.AddComponent<VerticalLayoutGroup>();

                            //            layoutRT.anchoredPosition = new Vector2(0f, 30f);
                            //            layoutRT.sizeDelta = Vector2.zero;
                            //            layoutVLG.childAlignment = TextAnchor.UpperCenter;
                            //            layoutVLG.spacing = 6f;
                            //        }

                            //        if (commands[0].Contains("playerWarp"))
                            //        {
                            //            xRT.sizeDelta = new Vector2(350f, 256f);
                            //            layout.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 90f);

                            //            string[] vector = new string[2];

                            //            if (commands[0] == "playerWarp" || commands[0] == "playerWarpAll")
                            //            {
                            //                vector = value.Split(new char[] { '.' });
                            //            }

                            //            var input = x.transform.Find("input");
                            //            var xif = input.gameObject.AddComponent<InputField>();
                            //            {
                            //                xif.onValueChanged.RemoveAllListeners();
                            //                xif.characterValidation = InputField.CharacterValidation.None;
                            //                xif.characterLimit = 0;
                            //                xif.textComponent = input.Find("Text").GetComponent<Text>();
                            //                xif.placeholder = input.Find("Placeholder").GetComponent<Text>();
                            //                if (commands[0] == "playerWarp" || commands[0] == "playerWarpAll")
                            //                {
                            //                    xif.text = vector[0];
                            //                }
                            //                else
                            //                {
                            //                    xif.text = value;
                            //                }
                            //                xif.onValueChanged.AddListener(delegate (string _val)
                            //                {
                            //                    if (commands[0] == "playerWarp" || commands[0] == "playerWarpAll")
                            //                    {
                            //                        modifier.GetType().GetField("value", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, _val + "." + layout.transform.Find("y/input").GetComponent<InputField>().text);
                            //                    }
                            //                    else
                            //                    {
                            //                        modifier.GetType().GetField("value", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, _val);
                            //                    }
                            //                });
                            //            }

                            //            var xet = input.gameObject.AddComponent<EventTrigger>();
                            //            xet.triggers.Clear();
                            //            xet.triggers.Add(Triggers.ScrollDelta(xif, 0.1f, 10f));

                            //            var xifh = input.gameObject.AddComponent<InputFieldHelper>();
                            //            xifh.inputField = xif;

                            //            var increase = x.transform.Find(">").GetComponent<Button>();
                            //            {
                            //                increase.onClick.RemoveAllListeners();
                            //                increase.onClick.AddListener(delegate ()
                            //                {
                            //                    xif.text = (float.Parse(xif.text) + 0.1f).ToString();
                            //                });
                            //            }

                            //            var decrease = x.transform.Find("<").GetComponent<Button>();
                            //            {
                            //                decrease.onClick.RemoveAllListeners();
                            //                decrease.onClick.AddListener(delegate ()
                            //                {
                            //                    xif.text = (float.Parse(xif.text) - 0.1f).ToString();
                            //                });
                            //            }

                            //            var valueG = new GameObject("x");
                            //            {
                            //                valueG.transform.SetParent(layout.transform);
                            //                valueG.transform.localScale = Vector3.one;

                            //                var valueGRT = valueG.AddComponent<RectTransform>();
                            //                var valueGHLG = valueG.AddComponent<HorizontalLayoutGroup>();

                            //                valueGHLG.childControlHeight = false;
                            //                valueGHLG.childControlWidth = false;
                            //                valueGHLG.childForceExpandWidth = false;
                            //                valueGHLG.spacing = 8f;
                            //            }

                            //            //Layout
                            //            {
                            //                xif.transform.SetParent(valueG.transform);
                            //                decrease.transform.SetParent(valueG.transform);
                            //                increase.transform.SetParent(valueG.transform);
                            //            }

                            //            //Value Label
                            //            {
                            //                var l = Instantiate(label);
                            //                l.name = "label";
                            //                l.transform.SetParent(valueG.transform);
                            //                l.transform.localScale = Vector3.one;
                            //                l.transform.SetAsFirstSibling();
                            //                if (commands[0].Contains("X") || !commands[0].Contains("X") && !commands[0].Contains("Y"))
                            //                    l.transform.GetChild(0).GetComponent<Text>().text = "X";
                            //                if (commands[0].Contains("Y"))
                            //                    l.transform.GetChild(0).GetComponent<Text>().text = "Y";
                            //                l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(107f, 20f);
                            //                l.transform.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                            //                var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
                            //                {
                            //                    ltextrt.anchoredPosition = new Vector2(10f, -5f);
                            //                }
                            //            }

                            //            //Y
                            //            if (commands[0] == "playerWarp" || commands[0] == "playerWarpAll")
                            //            {
                            //                var w = Instantiate(valueG);
                            //                w.transform.SetParent(layout.transform);
                            //                w.transform.localScale = Vector3.one;

                            //                w.name = "y";

                            //                w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Y";

                            //                var ppinput = w.transform.Find("input");
                            //                var ppif = ppinput.gameObject.GetComponent<InputField>();
                            //                {
                            //                    ppif.onValueChanged.RemoveAllListeners();
                            //                    ppif.characterValidation = InputField.CharacterValidation.None;
                            //                    ppif.characterLimit = 0;
                            //                    ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                            //                    ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                            //                    ppif.text = vector[1];
                            //                    ppif.onValueChanged.AddListener(delegate (string _val)
                            //                    {
                            //                        modifier.GetType().GetField("value", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, layout.transform.Find("x/input").GetComponent<InputField>().text + "." + _val);
                            //                    });
                            //                }

                            //                var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                            //                ppet.triggers.Clear();
                            //                ppet.triggers.Add(Triggers.ScrollDelta(ppif, 0.1f, 10f));

                            //                var increase2 = w.transform.Find(">").GetComponent<Button>();
                            //                {
                            //                    increase2.onClick.RemoveAllListeners();
                            //                    increase2.onClick.AddListener(delegate ()
                            //                    {
                            //                        ppif.text = (float.Parse(ppif.text) + 0.1f).ToString();
                            //                    });
                            //                }

                            //                var decrease2 = w.transform.Find("<").GetComponent<Button>();
                            //                {
                            //                    decrease2.onClick.RemoveAllListeners();
                            //                    decrease2.onClick.AddListener(delegate ()
                            //                    {
                            //                        ppif.text = (float.Parse(ppif.text) - 0.1f).ToString();
                            //                    });
                            //                }
                            //            }

                            //            //Duration
                            //            {
                            //                var w = Instantiate(valueG);
                            //                w.transform.SetParent(layout.transform);
                            //                w.transform.localScale = Vector3.one;

                            //                w.name = "duration";

                            //                w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Duration";

                            //                var ppinput = w.transform.Find("input");
                            //                var ppif = ppinput.gameObject.GetComponent<InputField>();
                            //                {
                            //                    ppif.onValueChanged.RemoveAllListeners();
                            //                    ppif.characterValidation = InputField.CharacterValidation.None;
                            //                    ppif.characterLimit = 0;
                            //                    ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                            //                    ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                            //                    ppif.text = commands[1];
                            //                    ppif.onValueChanged.AddListener(delegate (string _val)
                            //                    {
                            //                        commands[1] = _val;
                            //                        modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                            //                    });
                            //                }

                            //                var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                            //                ppet.triggers.Clear();
                            //                ppet.triggers.Add(Triggers.ScrollDelta(ppif, 0.1f, 10f));

                            //                var increase2 = w.transform.Find(">").GetComponent<Button>();
                            //                {
                            //                    increase2.onClick.RemoveAllListeners();
                            //                    increase2.onClick.AddListener(delegate ()
                            //                    {
                            //                        ppif.text = (float.Parse(ppif.text) + 0.1f).ToString();
                            //                    });
                            //                }

                            //                var decrease2 = w.transform.Find("<").GetComponent<Button>();
                            //                {
                            //                    decrease2.onClick.RemoveAllListeners();
                            //                    decrease2.onClick.AddListener(delegate ()
                            //                    {
                            //                        ppif.text = (float.Parse(ppif.text) - 0.1f).ToString();
                            //                    });
                            //                }
                            //            }

                            //            //Easing
                            //            {
                            //                var w = Instantiate(valueG);
                            //                w.transform.SetParent(layout.transform);
                            //                w.transform.localScale = Vector3.one;

                            //                w.name = "easing";

                            //                w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Easing";

                            //                Destroy(w.transform.Find("input").gameObject);
                            //                Destroy(w.transform.Find("<").gameObject);
                            //                Destroy(w.transform.Find(">").gameObject);

                            //                var dd = Instantiate(dropdownInput);
                            //                dd.transform.SetParent(w.transform);
                            //                dd.transform.localScale = Vector3.one;

                            //                Destroy(dd.GetComponent<HoverTooltip>());
                            //                Destroy(dd.GetComponent<HideDropdownOptions>());

                            //                var d = dd.GetComponent<Dropdown>();
                            //                d.onValueChanged.RemoveAllListeners();
                            //                d.options.Clear();

                            //                foreach (var anim in DataManager.inst.AnimationList)
                            //                {
                            //                    d.options.Add(new Dropdown.OptionData(anim.Name));
                            //                }

                            //                d.value = int.Parse(commands[2]);

                            //                d.onValueChanged.AddListener(delegate (int _val)
                            //                {
                            //                    commands[2] = _val.ToString();
                            //                    modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                            //                });
                            //            }
                            //        }

                            //        //Constant
                            //        {
                            //            var constantG = new GameObject("constant");
                            //            {
                            //                constantG.transform.SetParent(layout.transform);
                            //                constantG.transform.localScale = Vector3.one;
                            //                constantG.transform.SetAsFirstSibling();

                            //                var valueGRT = constantG.AddComponent<RectTransform>();
                            //                var valueGHLG = constantG.AddComponent<HorizontalLayoutGroup>();

                            //                valueGHLG.childControlHeight = false;
                            //                valueGHLG.childControlWidth = false;
                            //                valueGHLG.childForceExpandWidth = false;
                            //                valueGHLG.spacing = 8f;
                            //            }

                            //            var bo = Instantiate(boolInput);
                            //            {
                            //                bo.transform.SetParent(constantG.transform);
                            //                bo.transform.localScale = Vector3.one;

                            //                if (bo.GetComponent<Toggle>())
                            //                {
                            //                    var toggle = bo.GetComponent<Toggle>();
                            //                    toggle.onValueChanged.RemoveAllListeners();
                            //                    toggle.isOn = bool.Parse(constant);
                            //                    toggle.onValueChanged.AddListener(delegate (bool _val)
                            //                    {
                            //                        modifier.GetType().GetField("constant", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, _val);
                            //                    });
                            //                }
                            //            }

                            //            //Constant Label
                            //            {
                            //                var l = Instantiate(label);
                            //                l.name = "label";
                            //                l.transform.SetParent(constantG.transform);
                            //                l.transform.SetAsFirstSibling();
                            //                l.transform.localScale = Vector3.one;
                            //                l.transform.GetChild(0).GetComponent<Text>().text = "Constant";
                            //                l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(247f, 20f);
                            //                l.transform.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                            //                var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
                            //                {
                            //                    ltextrt.anchoredPosition = new Vector2(10f, -5f);
                            //                }
                            //            }
                            //        }

                            //        //NotGate
                            //        if (type == 0)
                            //        {
                            //            var constantG = new GameObject("not");
                            //            {
                            //                constantG.transform.SetParent(layout.transform);
                            //                constantG.transform.localScale = Vector3.one;
                            //                constantG.transform.SetAsFirstSibling();

                            //                var valueGRT = constantG.AddComponent<RectTransform>();
                            //                var valueGHLG = constantG.AddComponent<HorizontalLayoutGroup>();

                            //                valueGHLG.childControlHeight = false;
                            //                valueGHLG.childControlWidth = false;
                            //                valueGHLG.childForceExpandWidth = false;
                            //                valueGHLG.spacing = 8f;
                            //            }

                            //            var bo = Instantiate(boolInput);
                            //            {
                            //                bo.transform.SetParent(constantG.transform);
                            //                bo.transform.localScale = Vector3.one;

                            //                if (bo.GetComponent<Toggle>())
                            //                {
                            //                    var toggle = bo.GetComponent<Toggle>();
                            //                    toggle.onValueChanged.RemoveAllListeners();
                            //                    toggle.isOn = bool.Parse(notGate);
                            //                    toggle.onValueChanged.AddListener(delegate (bool _val)
                            //                    {
                            //                        modifier.GetType().GetField("not", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, _val);
                            //                    });
                            //                }
                            //            }

                            //            //Constant Label
                            //            {
                            //                var l = Instantiate(label);
                            //                l.name = "label";
                            //                l.transform.SetParent(constantG.transform);
                            //                l.transform.SetAsFirstSibling();
                            //                l.transform.localScale = Vector3.one;
                            //                l.transform.GetChild(0).GetComponent<Text>().text = "Not";
                            //                l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(247f, 20f);
                            //                l.transform.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                            //                var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
                            //                {
                            //                    ltextrt.anchoredPosition = new Vector2(10f, -5f);
                            //                }
                            //            }
                            //        }

                            //        //Delete Modifier
                            //        {
                            //            int tmpIndex = j;

                            //            var delete = Instantiate(close.gameObject);
                            //            delete.transform.SetParent(x.transform);
                            //            delete.transform.localScale = Vector3.one;
                            //            //delete.GetComponent<RectTransform>().anchoredPosition = new Vector2(333f, 0f);
                            //            delete.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                            //            delete.name = "delete";

                            //            var deleteButton = delete.GetComponent<Button>();
                            //            deleteButton.onClick.m_Calls.m_ExecutingCalls.Clear();
                            //            deleteButton.onClick.m_Calls.m_PersistentCalls.Clear();
                            //            deleteButton.onClick.m_PersistentCalls.m_Calls.Clear();
                            //            deleteButton.onClick.RemoveAllListeners();
                            //            deleteButton.onClick.AddListener(delegate ()
                            //            {
                            //                RemoveModifierIndex(beatmapObject, tmpIndex);
                            //                RTEditor.inst.StartCoroutine(RTEditor.RefreshObjectGUI());
                            //            });
                            //        }
                            //    }
                            //}

                            #endregion

                            {
                                var cmd = commands[0];

                                GameObject x = Instantiate(singleInput);
                                x.transform.SetParent(content);
                                x.name = cmd;

                                //Main Label
                                {
                                    var l = Instantiate(label);
                                    l.name = "label";
                                    l.transform.SetParent(x.transform);
                                    l.transform.SetAsFirstSibling();
                                    l.transform.localScale = Vector3.one;
                                    l.transform.GetChild(0).GetComponent<Text>().text = cmd;
                                    l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(187f, 20f);
                                    l.transform.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                                    var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
                                    {
                                        ltextrt.anchoredPosition = new Vector2(10f, -5f);
                                    }
                                }

                                x.transform.localScale = Vector3.one;
                                x.transform.GetChild(0).localScale = Vector3.one;

                                var xRT = x.GetComponent<RectTransform>();
                                {
                                    xRT.sizeDelta = new Vector2(350f, 128f);
                                }

                                x.GetComponent<Image>().enabled = true;
                                x.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);

                                //Destroy
                                {
                                    if (x.GetComponent<EventInfo>())
                                        Destroy(x.GetComponent<EventInfo>());

                                    if (x.GetComponent<HorizontalLayoutGroup>())
                                        Destroy(x.GetComponent<HorizontalLayoutGroup>());

                                    if (x.GetComponent<InputField>())
                                        DestroyImmediate(x.GetComponent<InputField>());

                                    if (x.GetComponent<InputFieldHelper>())
                                        Destroy(x.GetComponent<InputFieldHelper>());

                                    if (x.GetComponent<EventTrigger>())
                                        Destroy(x.GetComponent<EventTrigger>());
                                }

                                var layout = new GameObject("layout");
                                {
                                    layout.transform.SetParent(x.transform);
                                    layout.transform.localScale = Vector3.one;

                                    var layoutRT = layout.AddComponent<RectTransform>();
                                    var layoutVLG = layout.AddComponent<VerticalLayoutGroup>();

                                    layoutRT.anchoredPosition = new Vector2(0f, 30f);
                                    layoutRT.sizeDelta = Vector2.zero;
                                    layoutVLG.childAlignment = TextAnchor.UpperCenter;
                                    layoutVLG.spacing = 6f;
                                }

                                var valueG = new GameObject("value");
                                {
                                    valueG.transform.SetParent(layout.transform);
                                    valueG.transform.localScale = Vector3.one;

                                    var valueGRT = valueG.AddComponent<RectTransform>();
                                    var valueGHLG = valueG.AddComponent<HorizontalLayoutGroup>();

                                    valueGHLG.childControlHeight = false;
                                    valueGHLG.childControlWidth = false;
                                    valueGHLG.childForceExpandWidth = false;
                                    valueGHLG.spacing = 8f;
                                }

                                //Value Label
                                {
                                    var l = Instantiate(label);
                                    l.name = "label";
                                    l.transform.SetParent(valueG.transform);
                                    l.transform.SetAsFirstSibling();
                                    l.transform.localScale = Vector3.one;
                                    l.transform.GetChild(0).GetComponent<Text>().text = "Value";
                                    l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(107f, 20f);
                                    l.transform.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                                    var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
                                    {
                                        ltextrt.anchoredPosition = new Vector2(10f, -5f);
                                    }
                                }

                                //Layout
                                {
                                    x.transform.Find("input").SetParent(valueG.transform);
                                    x.transform.Find("<").SetParent(valueG.transform);
                                    x.transform.Find(">").SetParent(valueG.transform);
                                }

                                //Constant
                                {
                                    var constantG = new GameObject("constant");
                                    {
                                        constantG.transform.SetParent(layout.transform);
                                        constantG.transform.localScale = Vector3.one;
                                        constantG.transform.SetAsFirstSibling();

                                        var valueGRT = constantG.AddComponent<RectTransform>();
                                        var valueGHLG = constantG.AddComponent<HorizontalLayoutGroup>();

                                        valueGHLG.childControlHeight = false;
                                        valueGHLG.childControlWidth = false;
                                        valueGHLG.childForceExpandWidth = false;
                                        valueGHLG.spacing = 8f;
                                    }

                                    var bo = Instantiate(boolInput);
                                    {
                                        bo.transform.SetParent(constantG.transform);
                                        bo.transform.localScale = Vector3.one;

                                        if (bo.GetComponent<Toggle>())
                                        {
                                            var toggle = bo.GetComponent<Toggle>();
                                            toggle.onValueChanged.RemoveAllListeners();
                                            toggle.isOn = bool.Parse(constant);
                                            toggle.onValueChanged.AddListener(delegate (bool _val)
                                            {
                                                modifier.GetType().GetField("constant", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, _val);
                                            });
                                        }
                                    }

                                    //Constant Label
                                    {
                                        var l = Instantiate(label);
                                        l.name = "label";
                                        l.transform.SetParent(constantG.transform);
                                        l.transform.SetAsFirstSibling();
                                        l.transform.localScale = Vector3.one;
                                        l.transform.GetChild(0).GetComponent<Text>().text = "Constant";
                                        l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(247f, 20f);
                                        l.transform.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                                        var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
                                        {
                                            ltextrt.anchoredPosition = new Vector2(10f, -5f);
                                        }
                                    }
                                }

                                //NotGate
                                if (type == 0)
                                {
                                    var constantG = new GameObject("not");
                                    {
                                        constantG.transform.SetParent(layout.transform);
                                        constantG.transform.localScale = Vector3.one;
                                        constantG.transform.SetAsFirstSibling();

                                        var valueGRT = constantG.AddComponent<RectTransform>();
                                        var valueGHLG = constantG.AddComponent<HorizontalLayoutGroup>();

                                        valueGHLG.childControlHeight = false;
                                        valueGHLG.childControlWidth = false;
                                        valueGHLG.childForceExpandWidth = false;
                                        valueGHLG.spacing = 8f;
                                    }

                                    var bo = Instantiate(boolInput);
                                    {
                                        bo.transform.SetParent(constantG.transform);
                                        bo.transform.localScale = Vector3.one;

                                        if (bo.GetComponent<Toggle>())
                                        {
                                            var toggle = bo.GetComponent<Toggle>();
                                            toggle.onValueChanged.RemoveAllListeners();
                                            toggle.isOn = bool.Parse(notGate);
                                            toggle.onValueChanged.AddListener(delegate (bool _val)
                                            {
                                                modifier.GetType().GetField("not", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, _val);
                                            });
                                        }
                                    }

                                    //Constant Label
                                    {
                                        var l = Instantiate(label);
                                        l.name = "label";
                                        l.transform.SetParent(constantG.transform);
                                        l.transform.SetAsFirstSibling();
                                        l.transform.localScale = Vector3.one;
                                        l.transform.GetChild(0).GetComponent<Text>().text = "Not";
                                        l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(247f, 20f);
                                        l.transform.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                                        var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
                                        {
                                            ltextrt.anchoredPosition = new Vector2(10f, -5f);
                                        }
                                    }
                                }

                                //Float
                                if (cmd == "setPitch" || cmd == "addPitch" || cmd == "setMusicTime" || cmd == "pitchEquals" || cmd == "pitchLesserEquals" || cmd == "pitchGreaterEquals" || cmd == "pitchLesser" || cmd == "pitchGreater" || cmd == "playerDistanceLesser" || cmd == "playerDistanceGreater" || cmd == "blackHole" || cmd.Contains("setAlpha"))
                                {
                                    //xRT.sizeDelta = new Vector2(350f, 224f);
                                    //layout.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 60f);

                                    switch (commands[0])
                                    {
                                        case "setPitch":
                                            {
                                                Triggers.AddTooltip(x, commands[0], "Sets the pitch to this value. If EventsCore is installed, it will set an offset to the current pitch.");
                                                break;
                                            }
                                        case "setMusicTime":
                                            {
                                                Triggers.AddTooltip(x, commands[0], "Sets the song time. Good for skipping specific parts of a song or looping it. Make sure players have a way out of this loop.");
                                                break;
                                            }
                                        case "blur":
                                            {
                                                Triggers.AddTooltip(x, commands[0], "Replaces the objects' material with a blur effect.");
                                                break;
                                            }
                                        case "pitchEquals":
                                            {
                                                xRT.sizeDelta = new Vector2(350f, 160f);
                                                layout.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 40f);
                                                Triggers.AddTooltip(x, commands[0], "Activates modifiers when the pitch equals the value.");
                                                break;
                                            }
                                        case "pitchLesserEquals":
                                            {
                                                xRT.sizeDelta = new Vector2(350f, 160f);
                                                layout.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 40f);
                                                Triggers.AddTooltip(x, commands[0], "Activates modifiers when the pitch equals and is lesser than the value.");
                                                break;
                                            }
                                        case "pitchGreaterEquals":
                                            {
                                                xRT.sizeDelta = new Vector2(350f, 160f);
                                                layout.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 40f);
                                                Triggers.AddTooltip(x, commands[0], "Activates modifiers when the pitch equals and is greater than the value.");
                                                break;
                                            }
                                        case "pitchLesser":
                                            {
                                                xRT.sizeDelta = new Vector2(350f, 160f);
                                                layout.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 40f);
                                                Triggers.AddTooltip(x, commands[0], "Activates modifiers when the pitch is lesser than the value.");
                                                break;
                                            }
                                        case "pitchGreater":
                                            {
                                                xRT.sizeDelta = new Vector2(350f, 160f);
                                                layout.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 40f);
                                                Triggers.AddTooltip(x, commands[0], "Activates modifiers when the pitch is greater than the value.");
                                                break;
                                            }
                                    }

                                    var input = valueG.transform.Find("input");
                                    var xif = input.gameObject.AddComponent<InputField>();
                                    {
                                        xif.onValueChanged.RemoveAllListeners();
                                        xif.characterValidation = InputField.CharacterValidation.None;
                                        xif.characterLimit = 0;
                                        xif.textComponent = input.Find("Text").GetComponent<Text>();
                                        xif.placeholder = input.Find("Placeholder").GetComponent<Text>();
                                        xif.text = value;
                                        xif.onValueChanged.AddListener(delegate (string _val)
                                        {
                                            if (float.TryParse(_val, out float num))
                                            {
                                                modifier.GetType().GetField("value", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, num.ToString());
                                            }
                                        });
                                    }

                                    var xet = input.gameObject.AddComponent<EventTrigger>();
                                    xet.triggers.Clear();
                                    xet.triggers.Add(Triggers.ScrollDelta(xif, 0.1f, 10f));

                                    var xifh = input.gameObject.AddComponent<InputFieldHelper>();
                                    xifh.inputField = xif;

                                    var increase = valueG.transform.Find(">").GetComponent<Button>();
                                    {
                                        increase.onClick.RemoveAllListeners();
                                        increase.onClick.AddListener(delegate ()
                                        {
                                            xif.text = (float.Parse(xif.text) + 0.1f).ToString();
                                        });
                                    }

                                    var decrease = valueG.transform.Find("<").GetComponent<Button>();
                                    {
                                        decrease.onClick.RemoveAllListeners();
                                        decrease.onClick.AddListener(delegate ()
                                        {
                                            xif.text = (float.Parse(xif.text) - 0.1f).ToString();
                                        });
                                    }
                                }

                                if (cmd == "blur")
                                {

                                    var input = valueG.transform.Find("input");
                                    var xif = input.gameObject.AddComponent<InputField>();
                                    {
                                        xif.onValueChanged.RemoveAllListeners();
                                        xif.characterValidation = InputField.CharacterValidation.None;
                                        xif.characterLimit = 0;
                                        xif.textComponent = input.Find("Text").GetComponent<Text>();
                                        xif.placeholder = input.Find("Placeholder").GetComponent<Text>();
                                        xif.text = value;
                                        xif.onValueChanged.AddListener(delegate (string _val)
                                        {
                                            if (float.TryParse(_val, out float num))
                                            {
                                                modifier.GetType().GetField("value", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, num.ToString());
                                            }
                                        });
                                    }

                                    var xet = input.gameObject.AddComponent<EventTrigger>();
                                    xet.triggers.Clear();
                                    xet.triggers.Add(Triggers.ScrollDelta(xif, 0.1f, 10f));

                                    var xifh = input.gameObject.AddComponent<InputFieldHelper>();
                                    xifh.inputField = xif;

                                    var increase = valueG.transform.Find(">").GetComponent<Button>();
                                    {
                                        increase.onClick.RemoveAllListeners();
                                        increase.onClick.AddListener(delegate ()
                                        {
                                            xif.text = (float.Parse(xif.text) + 0.1f).ToString();
                                        });
                                    }

                                    var decrease = valueG.transform.Find("<").GetComponent<Button>();
                                    {
                                        decrease.onClick.RemoveAllListeners();
                                        decrease.onClick.AddListener(delegate ()
                                        {
                                            xif.text = (float.Parse(xif.text) - 0.1f).ToString();
                                        });
                                    }

                                    xRT.sizeDelta = new Vector2(350f, 224f);
                                    layout.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 60f);

                                    //Global playSound (Takes from soundlibrary if true, else takes from the current level folder)
                                    {
                                        var constantG = new GameObject("global");
                                        {
                                            constantG.transform.SetParent(layout.transform);
                                            constantG.transform.localScale = Vector3.one;

                                            var valueGRT = constantG.AddComponent<RectTransform>();
                                            var valueGHLG = constantG.AddComponent<HorizontalLayoutGroup>();

                                            valueGHLG.childControlHeight = false;
                                            valueGHLG.childControlWidth = false;
                                            valueGHLG.childForceExpandWidth = false;
                                            valueGHLG.spacing = 8f;
                                        }

                                        var bo = Instantiate(boolInput);
                                        {
                                            bo.transform.SetParent(constantG.transform);
                                            bo.transform.localScale = Vector3.one;

                                            if (bo.GetComponent<Toggle>())
                                            {
                                                var toggle = bo.GetComponent<Toggle>();
                                                toggle.onValueChanged.RemoveAllListeners();
                                                toggle.isOn = bool.Parse(commands[1]);
                                                toggle.onValueChanged.AddListener(delegate (bool _val)
                                                {
                                                    commands[1] = _val.ToString();
                                                    modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                                });
                                            }
                                        }

                                        //Label
                                        {
                                            var l = Instantiate(label);
                                            l.name = "label";
                                            l.transform.SetParent(constantG.transform);
                                            l.transform.SetAsFirstSibling();
                                            l.transform.localScale = Vector3.one;
                                            l.transform.GetChild(0).GetComponent<Text>().text = "Use Opacity";
                                            l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(247f, 20f);
                                            l.transform.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                                            var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
                                            {
                                                ltextrt.anchoredPosition = new Vector2(10f, -5f);
                                            }
                                        }
                                    }
                                }

                                if (cmd == "playSound" || cmd == "playSoundOnline")
                                {
                                    if (commands.Count == 1)
                                    {
                                        commands.Add("False");
                                        commands.Add("1");
                                        modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                    }

                                    //xRT.sizeDelta = new Vector2(350f, 224f);
                                    xRT.sizeDelta = new Vector2(350f, 284f);
                                    layout.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 100f);

                                    //Global playSound (Takes from soundlibrary if true, else takes from the current level folder)
                                    {
                                        var constantG = new GameObject("global");
                                        {
                                            constantG.transform.SetParent(layout.transform);
                                            constantG.transform.localScale = Vector3.one;

                                            var valueGRT = constantG.AddComponent<RectTransform>();
                                            var valueGHLG = constantG.AddComponent<HorizontalLayoutGroup>();

                                            valueGHLG.childControlHeight = false;
                                            valueGHLG.childControlWidth = false;
                                            valueGHLG.childForceExpandWidth = false;
                                            valueGHLG.spacing = 8f;
                                        }

                                        var bo = Instantiate(boolInput);
                                        {
                                            bo.transform.SetParent(constantG.transform);
                                            bo.transform.localScale = Vector3.one;

                                            if (bo.GetComponent<Toggle>())
                                            {
                                                var toggle = bo.GetComponent<Toggle>();
                                                toggle.onValueChanged.RemoveAllListeners();
                                                toggle.isOn = bool.Parse(commands[1]);
                                                toggle.onValueChanged.AddListener(delegate (bool _val)
                                                {
                                                    commands[1] = _val.ToString();
                                                    modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                                });
                                            }
                                        }

                                        //Label
                                        {
                                            var l = Instantiate(label);
                                            l.name = "label";
                                            l.transform.SetParent(constantG.transform);
                                            l.transform.SetAsFirstSibling();
                                            l.transform.localScale = Vector3.one;
                                            l.transform.GetChild(0).GetComponent<Text>().text = "Global";
                                            l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(247f, 20f);
                                            l.transform.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                                            var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
                                            {
                                                ltextrt.anchoredPosition = new Vector2(10f, -5f);
                                            }
                                        }
                                    }

                                    //Pitch (multiplies by current global pitch)
                                    {
                                        var ppvalueG = Instantiate(valueG);
                                        ppvalueG.transform.SetParent(layout.transform);
                                        ppvalueG.transform.localScale = Vector3.one;

                                        ppvalueG.name = "pitch";

                                        ppvalueG.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Pitch";

                                        var ppinput = ppvalueG.transform.Find("input");
                                        var ppif = ppinput.gameObject.AddComponent<InputField>();
                                        {
                                            ppif.onValueChanged.RemoveAllListeners();
                                            ppif.characterValidation = InputField.CharacterValidation.None;
                                            ppif.characterLimit = 0;
                                            ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                                            ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                                            ppif.text = commands[2];
                                            ppif.onValueChanged.AddListener(delegate (string _val)
                                            {
                                                if (float.TryParse(_val, out float num))
                                                {
                                                    commands[2] = num.ToString();
                                                    modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                                }
                                            });
                                        }

                                        var xet = ppinput.gameObject.AddComponent<EventTrigger>();
                                        xet.triggers.Clear();
                                        xet.triggers.Add(Triggers.ScrollDelta(ppif, 0.1f, 10f));

                                        var xifh = ppinput.gameObject.AddComponent<InputFieldHelper>();
                                        xifh.inputField = ppif;

                                        var ppincrease = ppvalueG.transform.Find(">").GetComponent<Button>();
                                        {
                                            ppincrease.onClick.RemoveAllListeners();
                                            ppincrease.onClick.AddListener(delegate ()
                                            {
                                                ppif.text = (float.Parse(ppif.text) + 0.1f).ToString();
                                            });
                                        }

                                        var ppdecrease = ppvalueG.transform.Find("<").GetComponent<Button>();
                                        {
                                            ppdecrease.onClick.RemoveAllListeners();
                                            ppdecrease.onClick.AddListener(delegate ()
                                            {
                                                ppif.text = (float.Parse(ppif.text) - 0.1f).ToString();
                                            });
                                        }
                                    }

                                    //Volume
                                    if (commands.Count > 3)
                                    {
                                        var ppvalueG = Instantiate(valueG);
                                        ppvalueG.transform.SetParent(layout.transform);
                                        ppvalueG.transform.localScale = Vector3.one;

                                        ppvalueG.name = "volume";

                                        ppvalueG.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Volume";

                                        var ppinput = ppvalueG.transform.Find("input");
                                        var ppif = ppinput.gameObject.AddComponent<InputField>();
                                        {
                                            ppif.onValueChanged.RemoveAllListeners();
                                            ppif.characterValidation = InputField.CharacterValidation.None;
                                            ppif.characterLimit = 0;
                                            ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                                            ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                                            ppif.text = commands[3];
                                            ppif.onValueChanged.AddListener(delegate (string _val)
                                            {
                                                if (float.TryParse(_val, out float num))
                                                {
                                                    commands[3] = num.ToString();
                                                    modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                                }
                                            });
                                        }

                                        var xet = ppinput.gameObject.AddComponent<EventTrigger>();
                                        xet.triggers.Clear();
                                        xet.triggers.Add(Triggers.ScrollDelta(ppif, 0.1f, 10f, clamp: new List<float> { 0f, 2f }));

                                        var xifh = ppinput.gameObject.AddComponent<InputFieldHelper>();
                                        xifh.inputField = ppif;

                                        var ppincrease = ppvalueG.transform.Find(">").GetComponent<Button>();
                                        {
                                            ppincrease.onClick.RemoveAllListeners();
                                            ppincrease.onClick.AddListener(delegate ()
                                            {
                                                ppif.text = Mathf.Clamp(float.Parse(ppif.text) + 0.1f, 0f, 2f).ToString();
                                            });
                                        }

                                        var ppdecrease = ppvalueG.transform.Find("<").GetComponent<Button>();
                                        {
                                            ppdecrease.onClick.RemoveAllListeners();
                                            ppdecrease.onClick.AddListener(delegate ()
                                            {
                                                ppif.text = Mathf.Clamp(float.Parse(ppif.text) - 0.1f, 0f, 2f).ToString();
                                            });
                                        }
                                    }

                                    //Loop
                                    if (commands.Count > 4)
                                    {
                                        var constantG = new GameObject("loop");
                                        {
                                            constantG.transform.SetParent(layout.transform);
                                            constantG.transform.localScale = Vector3.one;

                                            var valueGRT = constantG.AddComponent<RectTransform>();
                                            var valueGHLG = constantG.AddComponent<HorizontalLayoutGroup>();

                                            valueGHLG.childControlHeight = false;
                                            valueGHLG.childControlWidth = false;
                                            valueGHLG.childForceExpandWidth = false;
                                            valueGHLG.spacing = 8f;
                                        }

                                        var bo = Instantiate(boolInput);
                                        {
                                            bo.transform.SetParent(constantG.transform);
                                            bo.transform.localScale = Vector3.one;

                                            if (bo.GetComponent<Toggle>())
                                            {
                                                var toggle = bo.GetComponent<Toggle>();
                                                toggle.onValueChanged.RemoveAllListeners();
                                                toggle.isOn = bool.Parse(commands[4]);
                                                toggle.onValueChanged.AddListener(delegate (bool _val)
                                                {
                                                    commands[4] = _val.ToString();
                                                    modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                                });
                                            }
                                        }

                                        //Label
                                        {
                                            var l = Instantiate(label);
                                            l.name = "label";
                                            l.transform.SetParent(constantG.transform);
                                            l.transform.SetAsFirstSibling();
                                            l.transform.localScale = Vector3.one;
                                            l.transform.GetChild(0).GetComponent<Text>().text = "Loop";
                                            l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(247f, 20f);
                                            l.transform.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                                            var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
                                            {
                                                ltextrt.anchoredPosition = new Vector2(10f, -5f);
                                            }
                                        }
                                    }

                                    Destroy(valueG.transform.Find("<").gameObject);
                                    Destroy(valueG.transform.Find(">").gameObject);

                                    var input = valueG.transform.Find("input");
                                    var xif = input.gameObject.AddComponent<InputField>();
                                    {
                                        xif.onValueChanged.RemoveAllListeners();
                                        xif.characterValidation = InputField.CharacterValidation.None;
                                        xif.characterLimit = 0;
                                        xif.textComponent = input.Find("Text").GetComponent<Text>();
                                        xif.placeholder = input.Find("Placeholder").GetComponent<Text>();
                                        xif.text = value;
                                        xif.onValueChanged.AddListener(delegate (string _val)
                                        {
                                            modifier.GetType().GetField("value", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, _val);
                                        });
                                    }
                                }

                                if (cmd == "updateObject" || cmd == "copyColor")
                                {
                                    Destroy(valueG.transform.Find("<").gameObject);
                                    Destroy(valueG.transform.Find(">").gameObject);

                                    var input = valueG.transform.Find("input");
                                    var xif = input.gameObject.AddComponent<InputField>();
                                    {
                                        xif.onValueChanged.RemoveAllListeners();
                                        xif.characterValidation = InputField.CharacterValidation.None;
                                        xif.characterLimit = 0;
                                        xif.textComponent = input.Find("Text").GetComponent<Text>();
                                        xif.placeholder = input.Find("Placeholder").GetComponent<Text>();
                                        xif.text = value;
                                        xif.onValueChanged.AddListener(delegate (string _val)
                                        {
                                            modifier.GetType().GetField("value", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, _val);
                                        });
                                    }
                                }

                                if (cmd == "loadLevel" || cmd == "code")
                                {
                                    Triggers.AddTooltip(x, commands[0], "");

                                    var input = valueG.transform.Find("input");
                                    var xif = input.gameObject.AddComponent<InputField>();
                                    {
                                        xif.onValueChanged.RemoveAllListeners();
                                        xif.characterValidation = InputField.CharacterValidation.None;
                                        xif.characterLimit = 0;
                                        xif.textComponent = input.Find("Text").GetComponent<Text>();
                                        xif.placeholder = input.Find("Placeholder").GetComponent<Text>();
                                        xif.text = value;
                                        xif.onValueChanged.AddListener(delegate (string _val)
                                        {
                                            modifier.GetType().GetField("value", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, _val);
                                        });
                                    }

                                    if (cmd == "code")
                                    {
                                        Triggers.AddTooltip(input.gameObject, "Right click this to open the REPL Editor.", "");

                                        var clickable = input.gameObject.AddComponent<EditorClickable>();
                                        clickable.onClick = delegate (PointerEventData x)
                                        {
                                            if (x.button == PointerEventData.InputButton.Right)
                                            {
                                                OpenREPLEditor(modifier, value);
                                            }
                                        };
                                    }

                                    Destroy(valueG.transform.Find(">").gameObject);
                                    Destroy(valueG.transform.Find("<").gameObject);
                                }

                                if (cmd == "particleSystem")
                                {
                                    xRT.sizeDelta = new Vector2(350f, 640f);
                                    layout.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 280f);

                                    Triggers.AddTooltip(x, commands[0], "");

                                    valueG.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "LifeTime";

                                    var input = valueG.transform.Find("input");
                                    var xif = input.gameObject.AddComponent<InputField>();
                                    {
                                        xif.onValueChanged.RemoveAllListeners();
                                        xif.characterValidation = InputField.CharacterValidation.None;
                                        xif.characterLimit = 0;
                                        xif.textComponent = input.Find("Text").GetComponent<Text>();
                                        xif.placeholder = input.Find("Placeholder").GetComponent<Text>();
                                        xif.text = value;
                                        xif.onValueChanged.AddListener(delegate (string _val)
                                        {
                                            if (float.TryParse(_val, out float num))
                                            {
                                                modifier.GetType().GetField("value", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, num.ToString());
                                            }
                                        });
                                    }

                                    var xet = input.gameObject.AddComponent<EventTrigger>();
                                    xet.triggers.Clear();
                                    xet.triggers.Add(Triggers.ScrollDelta(xif, 0.1f, 10f));

                                    var xifh = input.gameObject.AddComponent<InputFieldHelper>();
                                    xifh.inputField = xif;

                                    var increase = valueG.transform.Find(">").GetComponent<Button>();
                                    {
                                        increase.onClick.RemoveAllListeners();
                                        increase.onClick.AddListener(delegate ()
                                        {
                                            xif.text = (float.Parse(xif.text) + 0.1f).ToString();
                                        });
                                    }

                                    var decrease = valueG.transform.Find("<").GetComponent<Button>();
                                    {
                                        decrease.onClick.RemoveAllListeners();
                                        decrease.onClick.AddListener(delegate ()
                                        {
                                            xif.text = (float.Parse(xif.text) - 0.1f).ToString();
                                        });
                                    }

                                    //shape
                                    {
                                        var w = Instantiate(valueG);
                                        w.transform.SetParent(layout.transform);
                                        w.transform.localScale = Vector3.one;

                                        w.name = "shape";

                                        w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Shape";

                                        var ppinput = w.transform.Find("input");
                                        var ppif = ppinput.gameObject.GetComponent<InputField>();
                                        {
                                            ppif.onValueChanged.RemoveAllListeners();
                                            ppif.characterValidation = InputField.CharacterValidation.Integer;
                                            ppif.characterLimit = 0;
                                            ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                                            ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                                            ppif.text = commands[1];
                                            ppif.onValueChanged.AddListener(delegate (string _val)
                                            {
                                                if (int.TryParse(_val, out int num))
                                                {
                                                    commands[1] = num.ToString();
                                                    commands[2] = "0";
                                                    modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                                    ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                                                    RTEditor.inst.StartCoroutine(RTEditor.RefreshObjectGUI());
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(Triggers.ScrollDeltaInt(ppif, 1, false, new List<int> { 0, ObjectManager.inst.objectPrefabs.Count - 1 }));
                                    }

                                    //shapeOpt
                                    {
                                        var w = Instantiate(valueG);
                                        w.transform.SetParent(layout.transform);
                                        w.transform.localScale = Vector3.one;

                                        w.name = "shapeOpt";

                                        w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "ShapeOpt";

                                        var ppinput = w.transform.Find("input");
                                        var ppif = ppinput.gameObject.GetComponent<InputField>();
                                        {
                                            ppif.onValueChanged.RemoveAllListeners();
                                            ppif.characterValidation = InputField.CharacterValidation.Integer;
                                            ppif.characterLimit = 0;
                                            ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                                            ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                                            ppif.text = commands[2];
                                            ppif.onValueChanged.AddListener(delegate (string _val)
                                            {
                                                if (int.TryParse(_val, out int num))
                                                {
                                                    commands[2] = num.ToString();
                                                    modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                                    ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(Triggers.ScrollDeltaInt(ppif, 1, false, new List<int> { 0, ObjectManager.inst.objectPrefabs[int.Parse(commands[1])].options.Count - 1 }));
                                    }

                                    //color
                                    {
                                        var w = Instantiate(valueG);
                                        w.transform.SetParent(layout.transform);
                                        w.transform.localScale = Vector3.one;

                                        w.name = "color";

                                        w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Color";


                                        var ppinput = w.transform.Find("input");
                                        var ppif = ppinput.gameObject.GetComponent<InputField>();
                                        {
                                            ppif.onValueChanged.RemoveAllListeners();
                                            ppif.characterValidation = InputField.CharacterValidation.Integer;
                                            ppif.characterLimit = 0;
                                            ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                                            ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                                            ppif.text = commands[3];
                                            ppif.onValueChanged.AddListener(delegate (string _val)
                                            {
                                                if (float.TryParse(_val, out float num))
                                                {
                                                    commands[3] = ((int)num).ToString();
                                                    modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                                    ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(Triggers.ScrollDeltaInt(ppif, 1, false, new List<int> { 0, GameManager.inst.LiveTheme.objectColors.Count - 1 }));
                                    }

                                    //startOpacity
                                    {
                                        var w = Instantiate(valueG);
                                        w.transform.SetParent(layout.transform);
                                        w.transform.localScale = Vector3.one;

                                        w.name = "startOpacity";

                                        w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "StartAlpha";


                                        var ppinput = w.transform.Find("input");
                                        var ppif = ppinput.gameObject.GetComponent<InputField>();
                                        {
                                            ppif.onValueChanged.RemoveAllListeners();
                                            ppif.characterValidation = InputField.CharacterValidation.None;
                                            ppif.characterLimit = 0;
                                            ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                                            ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                                            ppif.text = commands[4];
                                            ppif.onValueChanged.AddListener(delegate (string _val)
                                            {
                                                if (float.TryParse(_val, out float num))
                                                {
                                                    commands[4] = num.ToString();
                                                    modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                                    ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(Triggers.ScrollDelta(ppif, 0.1f, 10f));
                                    }

                                    //endOpacity
                                    {
                                        var w = Instantiate(valueG);
                                        w.transform.SetParent(layout.transform);
                                        w.transform.localScale = Vector3.one;

                                        w.name = "endOpacity";

                                        w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "EndAlpha";


                                        var ppinput = w.transform.Find("input");
                                        var ppif = ppinput.gameObject.GetComponent<InputField>();
                                        {
                                            ppif.onValueChanged.RemoveAllListeners();
                                            ppif.characterValidation = InputField.CharacterValidation.None;
                                            ppif.characterLimit = 0;
                                            ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                                            ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                                            ppif.text = commands[5];
                                            ppif.onValueChanged.AddListener(delegate (string _val)
                                            {
                                                if (float.TryParse(_val, out float num))
                                                {
                                                    commands[5] = num.ToString();
                                                    modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                                    ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(Triggers.ScrollDelta(ppif, 0.1f, 10f));
                                    }

                                    //startScale
                                    {
                                        var w = Instantiate(valueG);
                                        w.transform.SetParent(layout.transform);
                                        w.transform.localScale = Vector3.one;

                                        w.name = "startScale";

                                        w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "StartScale";


                                        var ppinput = w.transform.Find("input");
                                        var ppif = ppinput.gameObject.GetComponent<InputField>();
                                        {
                                            ppif.onValueChanged.RemoveAllListeners();
                                            ppif.characterValidation = InputField.CharacterValidation.None;
                                            ppif.characterLimit = 0;
                                            ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                                            ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                                            ppif.text = commands[6];
                                            ppif.onValueChanged.AddListener(delegate (string _val)
                                            {
                                                if (float.TryParse(_val, out float num))
                                                {
                                                    commands[6] = num.ToString();
                                                    modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                                    ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(Triggers.ScrollDelta(ppif, 0.1f, 10f));
                                    }

                                    //endScale
                                    {
                                        var w = Instantiate(valueG);
                                        w.transform.SetParent(layout.transform);
                                        w.transform.localScale = Vector3.one;

                                        w.name = "endOpacity";

                                        w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "EndScale";


                                        var ppinput = w.transform.Find("input");
                                        var ppif = ppinput.gameObject.GetComponent<InputField>();
                                        {
                                            ppif.onValueChanged.RemoveAllListeners();
                                            ppif.characterValidation = InputField.CharacterValidation.None;
                                            ppif.characterLimit = 0;
                                            ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                                            ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                                            ppif.text = commands[7];
                                            ppif.onValueChanged.AddListener(delegate (string _val)
                                            {
                                                if (float.TryParse(_val, out float num))
                                                {
                                                    commands[7] = num.ToString();
                                                    modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                                    ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(Triggers.ScrollDelta(ppif, 0.1f, 10f));
                                    }

                                    //rotation
                                    {
                                        var w = Instantiate(valueG);
                                        w.transform.SetParent(layout.transform);
                                        w.transform.localScale = Vector3.one;

                                        w.name = "rotation";

                                        w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Rotation";


                                        var ppinput = w.transform.Find("input");
                                        var ppif = ppinput.gameObject.GetComponent<InputField>();
                                        {
                                            ppif.onValueChanged.RemoveAllListeners();
                                            ppif.characterValidation = InputField.CharacterValidation.None;
                                            ppif.characterLimit = 0;
                                            ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                                            ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                                            ppif.text = commands[8];
                                            ppif.onValueChanged.AddListener(delegate (string _val)
                                            {
                                                if (float.TryParse(_val, out float num))
                                                {
                                                    commands[8] = num.ToString();
                                                    modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                                    ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(Triggers.ScrollDelta(ppif, 0.1f, 10f));
                                    }

                                    //speed
                                    {
                                        var w = Instantiate(valueG);
                                        w.transform.SetParent(layout.transform);
                                        w.transform.localScale = Vector3.one;

                                        w.name = "speed";

                                        w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Speed";


                                        var ppinput = w.transform.Find("input");
                                        var ppif = ppinput.gameObject.GetComponent<InputField>();
                                        {
                                            ppif.onValueChanged.RemoveAllListeners();
                                            ppif.characterValidation = InputField.CharacterValidation.None;
                                            ppif.characterLimit = 0;
                                            ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                                            ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                                            ppif.text = commands[9];
                                            ppif.onValueChanged.AddListener(delegate (string _val)
                                            {
                                                if (float.TryParse(_val, out float num))
                                                {
                                                    commands[9] = num.ToString();
                                                    modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                                    ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(Triggers.ScrollDelta(ppif, 0.1f, 10f));
                                    }

                                    //amount
                                    {
                                        var w = Instantiate(valueG);
                                        w.transform.SetParent(layout.transform);
                                        w.transform.localScale = Vector3.one;

                                        w.name = "amount";

                                        w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Amount";


                                        var ppinput = w.transform.Find("input");
                                        var ppif = ppinput.gameObject.GetComponent<InputField>();
                                        {
                                            ppif.onValueChanged.RemoveAllListeners();
                                            ppif.characterValidation = InputField.CharacterValidation.Integer;
                                            ppif.characterLimit = 0;
                                            ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                                            ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                                            ppif.text = commands[10];
                                            ppif.onValueChanged.AddListener(delegate (string _val)
                                            {
                                                if (float.TryParse(_val, out float num))
                                                {
                                                    commands[10] = ((int)num).ToString();
                                                    modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                                    ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(Triggers.ScrollDeltaInt(ppif, 1));
                                    }

                                    //duration
                                    {
                                        var w = Instantiate(valueG);
                                        w.transform.SetParent(layout.transform);
                                        w.transform.localScale = Vector3.one;

                                        w.name = "duration";

                                        w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Duration";


                                        var ppinput = w.transform.Find("input");
                                        var ppif = ppinput.gameObject.GetComponent<InputField>();
                                        {
                                            ppif.onValueChanged.RemoveAllListeners();
                                            ppif.characterValidation = InputField.CharacterValidation.None;
                                            ppif.characterLimit = 0;
                                            ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                                            ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                                            ppif.text = commands[11];
                                            ppif.onValueChanged.AddListener(delegate (string _val)
                                            {
                                                if (float.TryParse(_val, out float num))
                                                {
                                                    commands[11] = num.ToString();
                                                    modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                                    ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(Triggers.ScrollDelta(ppif, 0.1f, 10f));
                                    }

                                    //force X
                                    {
                                        var w = Instantiate(valueG);
                                        w.transform.SetParent(layout.transform);
                                        w.transform.localScale = Vector3.one;

                                        w.name = "force x";

                                        w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Force X";


                                        var ppinput = w.transform.Find("input");
                                        var ppif = ppinput.gameObject.GetComponent<InputField>();
                                        {
                                            ppif.onValueChanged.RemoveAllListeners();
                                            ppif.characterValidation = InputField.CharacterValidation.None;
                                            ppif.characterLimit = 0;
                                            ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                                            ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                                            ppif.text = commands[12];
                                            ppif.onValueChanged.AddListener(delegate (string _val)
                                            {
                                                if (float.TryParse(_val, out float num))
                                                {
                                                    commands[12] = num.ToString();
                                                    modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                                    ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(Triggers.ScrollDelta(ppif, 0.1f, 10f));
                                    }

                                    //force Y
                                    {
                                        var w = Instantiate(valueG);
                                        w.transform.SetParent(layout.transform);
                                        w.transform.localScale = Vector3.one;

                                        w.name = "force y";

                                        w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Force Y";


                                        var ppinput = w.transform.Find("input");
                                        var ppif = ppinput.gameObject.GetComponent<InputField>();
                                        {
                                            ppif.onValueChanged.RemoveAllListeners();
                                            ppif.characterValidation = InputField.CharacterValidation.None;
                                            ppif.characterLimit = 0;
                                            ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                                            ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                                            ppif.text = commands[13];
                                            ppif.onValueChanged.AddListener(delegate (string _val)
                                            {
                                                if (float.TryParse(_val, out float num))
                                                {
                                                    commands[13] = num.ToString();
                                                    modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                                    ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(Triggers.ScrollDelta(ppif, 0.1f, 10f));
                                    }

                                    //trail Emit
                                    {
                                        var w = Instantiate(layout.transform.Find("constant").gameObject);
                                        w.transform.SetParent(layout.transform);
                                        w.transform.localScale = Vector3.one;
                                        w.name = "trail";

                                        w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Trail";

                                        var tog = w.transform.GetChild(1).GetComponent<Toggle>();
                                        tog.onValueChanged.RemoveAllListeners();
                                        tog.isOn = bool.Parse(commands[14]);
                                        tog.onValueChanged.AddListener(delegate (bool _val)
                                        {
                                            commands[14] = _val.ToString();
                                            modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                            ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                                        });
                                    }
                                }

                                if (cmd == "trailRenderer")
                                {
                                    xRT.sizeDelta = new Vector2(350f, 352f);
                                    layout.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 140f);

                                    Triggers.AddTooltip(x, commands[0], "");

                                    valueG.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Time";

                                    var input = valueG.transform.Find("input");
                                    var xif = input.gameObject.AddComponent<InputField>();
                                    {
                                        xif.onValueChanged.RemoveAllListeners();
                                        xif.characterValidation = InputField.CharacterValidation.None;
                                        xif.characterLimit = 0;
                                        xif.textComponent = input.Find("Text").GetComponent<Text>();
                                        xif.placeholder = input.Find("Placeholder").GetComponent<Text>();
                                        xif.text = value;
                                        xif.onValueChanged.AddListener(delegate (string _val)
                                        {
                                            if (float.TryParse(_val, out float num))
                                            {
                                                modifier.GetType().GetField("value", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, num.ToString());
                                            }
                                        });
                                    }

                                    var xet = input.gameObject.AddComponent<EventTrigger>();
                                    xet.triggers.Clear();
                                    xet.triggers.Add(Triggers.ScrollDelta(xif, 0.1f, 10f));

                                    var xifh = input.gameObject.AddComponent<InputFieldHelper>();
                                    xifh.inputField = xif;

                                    var increase = valueG.transform.Find(">").GetComponent<Button>();
                                    {
                                        increase.onClick.RemoveAllListeners();
                                        increase.onClick.AddListener(delegate ()
                                        {
                                            xif.text = (float.Parse(xif.text) + 0.1f).ToString();
                                        });
                                    }

                                    var decrease = valueG.transform.Find("<").GetComponent<Button>();
                                    {
                                        decrease.onClick.RemoveAllListeners();
                                        decrease.onClick.AddListener(delegate ()
                                        {
                                            xif.text = (float.Parse(xif.text) - 0.1f).ToString();
                                        });
                                    }

                                    //startWidth
                                    {
                                        var w = Instantiate(valueG);
                                        w.transform.SetParent(layout.transform);
                                        w.transform.localScale = Vector3.one;

                                        w.name = "startWidth";

                                        w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "StartWidth";

                                        var ppinput = w.transform.Find("input");
                                        var ppif = ppinput.gameObject.GetComponent<InputField>();
                                        {
                                            ppif.onValueChanged.RemoveAllListeners();
                                            ppif.characterValidation = InputField.CharacterValidation.None;
                                            ppif.characterLimit = 0;
                                            ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                                            ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                                            ppif.text = commands[1];
                                            ppif.onValueChanged.AddListener(delegate (string _val)
                                            {
                                                if (float.TryParse(_val, out float num))
                                                {
                                                    commands[1] = num.ToString();
                                                    modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(Triggers.ScrollDelta(ppif, 0.1f, 10f));
                                    }

                                    //endWidth
                                    {
                                        var w = Instantiate(valueG);
                                        w.transform.SetParent(layout.transform);
                                        w.transform.localScale = Vector3.one;

                                        w.name = "endWidth";

                                        w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "EndWidth";


                                        var ppinput = w.transform.Find("input");
                                        var ppif = ppinput.gameObject.GetComponent<InputField>();
                                        {
                                            ppif.onValueChanged.RemoveAllListeners();
                                            ppif.characterValidation = InputField.CharacterValidation.None;
                                            ppif.characterLimit = 0;
                                            ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                                            ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                                            ppif.text = commands[2];
                                            ppif.onValueChanged.AddListener(delegate (string _val)
                                            {
                                                if (float.TryParse(_val, out float num))
                                                {
                                                    commands[2] = num.ToString();
                                                    modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(Triggers.ScrollDelta(ppif, 0.1f, 10f));
                                    }

                                    //startColor
                                    {
                                        var w = Instantiate(valueG);
                                        w.transform.SetParent(layout.transform);
                                        w.transform.localScale = Vector3.one;

                                        w.name = "startColor";

                                        w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "StartColor";


                                        var ppinput = w.transform.Find("input");
                                        var ppif = ppinput.gameObject.GetComponent<InputField>();
                                        {
                                            ppif.onValueChanged.RemoveAllListeners();
                                            ppif.characterValidation = InputField.CharacterValidation.Integer;
                                            ppif.characterLimit = 0;
                                            ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                                            ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                                            ppif.text = commands[3];
                                            ppif.onValueChanged.AddListener(delegate (string _val)
                                            {
                                                if (float.TryParse(_val, out float num))
                                                {
                                                    commands[3] = ((int)num).ToString();
                                                    modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(Triggers.ScrollDeltaInt(ppif, 1, false, new List<int> { 0, GameManager.inst.LiveTheme.objectColors.Count - 1 }));
                                    }

                                    //endColor
                                    {
                                        var w = Instantiate(valueG);
                                        w.transform.SetParent(layout.transform);
                                        w.transform.localScale = Vector3.one;

                                        w.name = "endColor";

                                        w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "EndColor";


                                        var ppinput = w.transform.Find("input");
                                        var ppif = ppinput.gameObject.GetComponent<InputField>();
                                        {
                                            ppif.onValueChanged.RemoveAllListeners();
                                            ppif.characterValidation = InputField.CharacterValidation.Integer;
                                            ppif.characterLimit = 0;
                                            ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                                            ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                                            ppif.text = commands[5];
                                            ppif.onValueChanged.AddListener(delegate (string _val)
                                            {
                                                if (float.TryParse(_val, out float num))
                                                {
                                                    commands[5] = ((int)num).ToString();
                                                    modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(Triggers.ScrollDeltaInt(ppif, 1, false, new List<int> { 0, GameManager.inst.LiveTheme.objectColors.Count - 1 }));
                                    }

                                    //startOpacity
                                    {
                                        var w = Instantiate(valueG);
                                        w.transform.SetParent(layout.transform);
                                        w.transform.localScale = Vector3.one;

                                        w.name = "startOpacity";

                                        w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "StartAlpha";


                                        var ppinput = w.transform.Find("input");
                                        var ppif = ppinput.gameObject.GetComponent<InputField>();
                                        {
                                            ppif.onValueChanged.RemoveAllListeners();
                                            ppif.characterValidation = InputField.CharacterValidation.None;
                                            ppif.characterLimit = 0;
                                            ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                                            ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                                            ppif.text = commands[4];
                                            ppif.onValueChanged.AddListener(delegate (string _val)
                                            {
                                                if (float.TryParse(_val, out float num))
                                                {
                                                    commands[4] = num.ToString();
                                                    modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(Triggers.ScrollDelta(ppif, 0.1f, 10f));
                                    }

                                    //endOpacity
                                    {
                                        var w = Instantiate(valueG);
                                        w.transform.SetParent(layout.transform);
                                        w.transform.localScale = Vector3.one;

                                        w.name = "endOpacity";

                                        w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "EndAlpha";


                                        var ppinput = w.transform.Find("input");
                                        var ppif = ppinput.gameObject.GetComponent<InputField>();
                                        {
                                            ppif.onValueChanged.RemoveAllListeners();
                                            ppif.characterValidation = InputField.CharacterValidation.None;
                                            ppif.characterLimit = 0;
                                            ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                                            ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                                            ppif.text = commands[6];
                                            ppif.onValueChanged.AddListener(delegate (string _val)
                                            {
                                                if (float.TryParse(_val, out float num))
                                                {
                                                    commands[6] = num.ToString();
                                                    modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(Triggers.ScrollDelta(ppif, 0.1f, 10f));
                                    }
                                }

                                if (cmd == "spawnPrefab")
                                {
                                    xRT.sizeDelta = new Vector2(350f, 352f);
                                    layout.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 140f);

                                    Triggers.AddTooltip(x, commands[0], "");

                                    var input = valueG.transform.Find("input");
                                    var xif = input.gameObject.AddComponent<InputField>();
                                    {
                                        xif.onValueChanged.RemoveAllListeners();
                                        xif.characterValidation = InputField.CharacterValidation.None;
                                        xif.characterLimit = 0;
                                        xif.textComponent = input.Find("Text").GetComponent<Text>();
                                        xif.placeholder = input.Find("Placeholder").GetComponent<Text>();
                                        xif.text = Mathf.Clamp(int.Parse(value), 0, DataManager.inst.gameData.prefabs.Count - 1).ToString();
                                        xif.onValueChanged.AddListener(delegate (string _val)
                                        {
                                            if (int.TryParse(_val, out int num))
                                            {
                                                num = Mathf.Clamp(num, 0, DataManager.inst.gameData.prefabs.Count - 1);
                                                modifier.GetType().GetField("value", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, num.ToString());
                                            }
                                        });
                                    }

                                    var xet = input.gameObject.AddComponent<EventTrigger>();
                                    xet.triggers.Clear();
                                    xet.triggers.Add(Triggers.ScrollDeltaInt(xif, 1, false, new List<int> { 0, DataManager.inst.gameData.prefabs.Count - 1 }));

                                    var xifh = input.gameObject.AddComponent<InputFieldHelper>();
                                    xifh.inputField = xif;

                                    var increase = valueG.transform.Find(">").GetComponent<Button>();
                                    {
                                        increase.onClick.RemoveAllListeners();
                                        increase.onClick.AddListener(delegate ()
                                        {
                                            xif.text = Mathf.Clamp(int.Parse(xif.text) + 1, 0, DataManager.inst.gameData.prefabs.Count - 1).ToString();
                                        });
                                    }

                                    var decrease = valueG.transform.Find("<").GetComponent<Button>();
                                    {
                                        decrease.onClick.RemoveAllListeners();
                                        decrease.onClick.AddListener(delegate ()
                                        {
                                            xif.text = Mathf.Clamp(int.Parse(xif.text) - 1, 0, DataManager.inst.gameData.prefabs.Count - 1).ToString();
                                        });
                                    }

                                    //Pos X
                                    {
                                        var w = Instantiate(valueG);
                                        w.transform.SetParent(layout.transform);
                                        w.transform.localScale = Vector3.one;

                                        w.name = "pos x";

                                        w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Pos X";

                                        var ppinput = w.transform.Find("input");
                                        var ppif = ppinput.gameObject.GetComponent<InputField>();
                                        {
                                            ppif.onValueChanged.RemoveAllListeners();
                                            ppif.characterValidation = InputField.CharacterValidation.None;
                                            ppif.characterLimit = 0;
                                            ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                                            ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                                            ppif.text = commands[1];
                                            ppif.onValueChanged.AddListener(delegate (string _val)
                                            {
                                                if (float.TryParse(_val, out float num))
                                                {
                                                    commands[1] = num.ToString();
                                                    modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(Triggers.ScrollDelta(ppif, 0.1f, 10f, true));
                                        ppet.triggers.Add(Triggers.ScrollDeltaVector2(ppif, layout.transform.Find("pos y").GetComponent<InputField>(), 0.1f, 10f));

                                        var ppifh = input.gameObject.AddComponent<InputFieldHelper>();
                                        ppifh.inputField = ppif;

                                        var ppincrease = valueG.transform.Find(">").GetComponent<Button>();
                                        {
                                            ppincrease.onClick.RemoveAllListeners();
                                            ppincrease.onClick.AddListener(delegate ()
                                            {
                                                ppif.text = (float.Parse(ppif.text) + 1).ToString();
                                            });
                                        }

                                        var ppdecrease = valueG.transform.Find("<").GetComponent<Button>();
                                        {
                                            ppdecrease.onClick.RemoveAllListeners();
                                            ppdecrease.onClick.AddListener(delegate ()
                                            {
                                                ppif.text = (float.Parse(ppif.text) - 1).ToString();
                                            });
                                        }
                                    }

                                    //Pos Y
                                    {
                                        var w = Instantiate(valueG);
                                        w.transform.SetParent(layout.transform);
                                        w.transform.localScale = Vector3.one;

                                        w.name = "pos y";

                                        w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Pos Y";

                                        var ppinput = w.transform.Find("input");
                                        var ppif = ppinput.gameObject.GetComponent<InputField>();
                                        {
                                            ppif.onValueChanged.RemoveAllListeners();
                                            ppif.characterValidation = InputField.CharacterValidation.None;
                                            ppif.characterLimit = 0;
                                            ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                                            ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                                            ppif.text = commands[2];
                                            ppif.onValueChanged.AddListener(delegate (string _val)
                                            {
                                                if (float.TryParse(_val, out float num))
                                                {
                                                    commands[2] = num.ToString();
                                                    modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(Triggers.ScrollDelta(ppif, 0.1f, 10f, true));
                                        ppet.triggers.Add(Triggers.ScrollDeltaVector2(layout.transform.Find("pos x").GetComponent<InputField>(), ppif, 0.1f, 10f));

                                        var ppifh = input.gameObject.AddComponent<InputFieldHelper>();
                                        ppifh.inputField = ppif;

                                        var ppincrease = valueG.transform.Find(">").GetComponent<Button>();
                                        {
                                            ppincrease.onClick.RemoveAllListeners();
                                            ppincrease.onClick.AddListener(delegate ()
                                            {
                                                ppif.text = (float.Parse(ppif.text) + 1).ToString();
                                            });
                                        }

                                        var ppdecrease = valueG.transform.Find("<").GetComponent<Button>();
                                        {
                                            ppdecrease.onClick.RemoveAllListeners();
                                            ppdecrease.onClick.AddListener(delegate ()
                                            {
                                                ppif.text = (float.Parse(ppif.text) - 1).ToString();
                                            });
                                        }
                                    }

                                    //Sca X
                                    {
                                        var w = Instantiate(valueG);
                                        w.transform.SetParent(layout.transform);
                                        w.transform.localScale = Vector3.one;

                                        w.name = "sca x";

                                        w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Sca X";

                                        var ppinput = w.transform.Find("input");
                                        var ppif = ppinput.gameObject.GetComponent<InputField>();
                                        {
                                            ppif.onValueChanged.RemoveAllListeners();
                                            ppif.characterValidation = InputField.CharacterValidation.None;
                                            ppif.characterLimit = 0;
                                            ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                                            ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                                            ppif.text = commands[3];
                                            ppif.onValueChanged.AddListener(delegate (string _val)
                                            {
                                                if (float.TryParse(_val, out float num))
                                                {
                                                    commands[3] = num.ToString();
                                                    modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(Triggers.ScrollDelta(ppif, 0.1f, 10f, true));
                                        ppet.triggers.Add(Triggers.ScrollDeltaVector2(ppif, layout.transform.Find("sca y").GetComponent<InputField>(), 0.1f, 10f));

                                        var ppifh = input.gameObject.AddComponent<InputFieldHelper>();
                                        ppifh.inputField = ppif;

                                        var ppincrease = valueG.transform.Find(">").GetComponent<Button>();
                                        {
                                            ppincrease.onClick.RemoveAllListeners();
                                            ppincrease.onClick.AddListener(delegate ()
                                            {
                                                ppif.text = (float.Parse(ppif.text) + 1).ToString();
                                            });
                                        }

                                        var ppdecrease = valueG.transform.Find("<").GetComponent<Button>();
                                        {
                                            ppdecrease.onClick.RemoveAllListeners();
                                            ppdecrease.onClick.AddListener(delegate ()
                                            {
                                                ppif.text = (float.Parse(ppif.text) - 1).ToString();
                                            });
                                        }
                                    }

                                    //Sca Y
                                    {
                                        var w = Instantiate(valueG);
                                        w.transform.SetParent(layout.transform);
                                        w.transform.localScale = Vector3.one;

                                        w.name = "sca y";

                                        w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Sca Y";

                                        var ppinput = w.transform.Find("input");
                                        var ppif = ppinput.gameObject.GetComponent<InputField>();
                                        {
                                            ppif.onValueChanged.RemoveAllListeners();
                                            ppif.characterValidation = InputField.CharacterValidation.None;
                                            ppif.characterLimit = 0;
                                            ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                                            ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                                            ppif.text = commands[4];
                                            ppif.onValueChanged.AddListener(delegate (string _val)
                                            {
                                                if (float.TryParse(_val, out float num))
                                                {
                                                    commands[4] = num.ToString();
                                                    modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(Triggers.ScrollDelta(ppif, 0.1f, 10f, true));
                                        ppet.triggers.Add(Triggers.ScrollDeltaVector2(layout.transform.Find("sca x").GetComponent<InputField>(), ppif, 0.1f, 10f));

                                        var ppifh = input.gameObject.AddComponent<InputFieldHelper>();
                                        ppifh.inputField = ppif;

                                        var ppincrease = valueG.transform.Find(">").GetComponent<Button>();
                                        {
                                            ppincrease.onClick.RemoveAllListeners();
                                            ppincrease.onClick.AddListener(delegate ()
                                            {
                                                ppif.text = (float.Parse(ppif.text) + 1).ToString();
                                            });
                                        }

                                        var ppdecrease = valueG.transform.Find("<").GetComponent<Button>();
                                        {
                                            ppdecrease.onClick.RemoveAllListeners();
                                            ppdecrease.onClick.AddListener(delegate ()
                                            {
                                                ppif.text = (float.Parse(ppif.text) - 1).ToString();
                                            });
                                        }
                                    }

                                    //Rot
                                    {
                                        var w = Instantiate(valueG);
                                        w.transform.SetParent(layout.transform);
                                        w.transform.localScale = Vector3.one;

                                        w.name = "rot";

                                        w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Rot";

                                        var ppinput = w.transform.Find("input");
                                        var ppif = ppinput.gameObject.GetComponent<InputField>();
                                        {
                                            ppif.onValueChanged.RemoveAllListeners();
                                            ppif.characterValidation = InputField.CharacterValidation.None;
                                            ppif.characterLimit = 0;
                                            ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                                            ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                                            ppif.text = commands[5];
                                            ppif.onValueChanged.AddListener(delegate (string _val)
                                            {
                                                if (float.TryParse(_val, out float num))
                                                {
                                                    commands[5] = num.ToString();
                                                    modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(Triggers.ScrollDelta(ppif, 15f, 3f));

                                        var ppifh = input.gameObject.AddComponent<InputFieldHelper>();
                                        ppifh.inputField = ppif;

                                        var ppincrease = valueG.transform.Find(">").GetComponent<Button>();
                                        {
                                            ppincrease.onClick.RemoveAllListeners();
                                            ppincrease.onClick.AddListener(delegate ()
                                            {
                                                ppif.text = (float.Parse(ppif.text) + 1f).ToString();
                                            });
                                        }

                                        var ppdecrease = valueG.transform.Find("<").GetComponent<Button>();
                                        {
                                            ppdecrease.onClick.RemoveAllListeners();
                                            ppdecrease.onClick.AddListener(delegate ()
                                            {
                                                ppif.text = (float.Parse(ppif.text) - 1f).ToString();
                                            });
                                        }
                                    }
                                }

                                //Integer
                                if (cmd == "playerHit" || cmd == "playerHitAll" || cmd == "playerHeal" || cmd == "playerHealAll" || cmd == "addVariable" || cmd == "subVariable" || cmd == "setVariable" || cmd == "mouseButtonDown" || cmd == "mouseButton" || cmd == "mouseButtonUp" || cmd.Contains("playerHealth") || cmd.Contains("playerDeaths") || cmd.Contains("variable"))
                                {
                                    xRT.sizeDelta = new Vector2(350f, 160f);
                                    layout.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 40f);

                                    Triggers.AddTooltip(x, commands[0], "");

                                    var input = valueG.transform.Find("input");
                                    var xif = input.gameObject.AddComponent<InputField>();
                                    {
                                        xif.onValueChanged.RemoveAllListeners();
                                        xif.characterValidation = InputField.CharacterValidation.None;
                                        xif.characterLimit = 0;
                                        xif.textComponent = input.Find("Text").GetComponent<Text>();
                                        xif.placeholder = input.Find("Placeholder").GetComponent<Text>();
                                        xif.text = Mathf.Clamp(int.Parse(value), 0, int.MaxValue).ToString();
                                        xif.onValueChanged.AddListener(delegate (string _val)
                                        {
                                            if (int.TryParse(_val, out int num))
                                            {
                                                num = Mathf.Clamp(num, 0, int.MaxValue);
                                                modifier.GetType().GetField("value", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, num.ToString());
                                            }
                                        });
                                    }

                                    var xet = input.gameObject.AddComponent<EventTrigger>();
                                    xet.triggers.Clear();
                                    xet.triggers.Add(Triggers.ScrollDeltaInt(xif, 1, false, new List<int> { 0, int.MaxValue }));

                                    var increase = valueG.transform.Find(">").GetComponent<Button>();
                                    {
                                        increase.onClick.RemoveAllListeners();
                                        increase.onClick.AddListener(delegate ()
                                        {
                                            xif.text = Mathf.Clamp(int.Parse(xif.text) + 1, 0, int.MaxValue).ToString();
                                        });
                                    }

                                    var decrease = valueG.transform.Find("<").GetComponent<Button>();
                                    {
                                        decrease.onClick.RemoveAllListeners();
                                        decrease.onClick.AddListener(delegate ()
                                        {
                                            xif.text = Mathf.Clamp(int.Parse(xif.text) - 1, 0, int.MaxValue).ToString();
                                        });
                                    }
                                }

                                //No Values
                                {
                                    bool player = cmd == "playerKill" || cmd == "playerKillAll" || cmd == "playerCollide" || cmd == "playerMoving" || cmd == "playerBoosting" || cmd == "playerAlive" || cmd == "playerBoost" || cmd == "playerBoostAll" || cmd == "playerDisableBoost" || cmd == "onPlayerHit";

                                    bool mode = cmd == "inZenMode" || cmd == "inNormal" || cmd == "in1Life" || cmd == "inNoHit" || cmd == "inEditor";

                                    if (player || mode || cmd == "showMouse" || cmd == "hideMouse" || cmd == "mouseOver" || cmd == "disableObject" || cmd == "disableObjectTree" || cmd == "bulletCollide" || cmd == "updateObjects")
                                    {
                                        switch (commands[0])
                                        {
                                            case "playerKill":
                                                {
                                                    Triggers.AddTooltip(x, commands[0], "Kills the nearest player.");
                                                    break;
                                                }
                                            case "playerKillAll":
                                                {
                                                    Triggers.AddTooltip(x, commands[0], "Kills all players.");
                                                    break;
                                                }
                                            case "showMouse":
                                                {
                                                    Triggers.AddTooltip(x, commands[0], "Shows the mouse in-game.");
                                                    break;
                                                }
                                            case "hideMouse":
                                                {
                                                    Triggers.AddTooltip(x, commands[0], "Hides the mouse in-game. Does not change anything in edit mode.");
                                                    break;
                                                }
                                            case "playerCollide":
                                                {
                                                    Triggers.AddTooltip(x, commands[0], "Activates modifiers when player collides with object.");
                                                    break;
                                                }
                                            case "playerMoving":
                                                {
                                                    Triggers.AddTooltip(x, commands[0], "Activates modifiers when player moves.");
                                                    break;
                                                }
                                            case "playerBoosting":
                                                {
                                                    Triggers.AddTooltip(x, commands[0], "Activates modifiers when player boosts.");
                                                    break;
                                                }
                                            case "playerAlive":
                                                {
                                                    Triggers.AddTooltip(x, commands[0], "Activates modifiers when a player dies.");
                                                    break;
                                                }
                                            case "mouseOver":
                                                {
                                                    Triggers.AddTooltip(x, commands[0], "Activates modifiers when the mouse is over the object.");
                                                    break;
                                                }
                                            case "playerBoost":
                                                {
                                                    Triggers.AddTooltip(x, commands[0], "Forces nearest player to boost.");
                                                    break;
                                                }
                                            case "playerBoostAll":
                                                {
                                                    Triggers.AddTooltip(x, commands[0], "Forces all players to boost.");
                                                    break;
                                                }
                                            case "playerDisableBoost":
                                                {
                                                    Triggers.AddTooltip(x, commands[0], "Disabled the boost of all players.");
                                                    break;
                                                }
                                            case "disableObject":
                                                {
                                                    Triggers.AddTooltip(x, commands[0], "Destroys this object. (Will not occur in edit mode, please save any changes before testing this.)");
                                                    break;
                                                }
                                            case "disableObjectTree":
                                                {
                                                    Triggers.AddTooltip(x, commands[0], "Destroys all objects attached to this one. (Will not occur in edit mode, please save any changes before testing this. Includes children and parents)");
                                                    break;
                                                }
                                        }

                                        Destroy(valueG);
                                    }
                                }

                                if (cmd == "keyPressDown" || cmd == "keyPress" || cmd == "keyPressUp")
                                {
                                    xRT.sizeDelta = new Vector2(350f, 160f);
                                    layout.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 40f);

                                    Destroy(valueG.transform.Find("input").gameObject);
                                    Destroy(valueG.transform.Find("<").gameObject);
                                    Destroy(valueG.transform.Find(">").gameObject);

                                    var dd = Instantiate(dropdownInput);
                                    dd.transform.SetParent(valueG.transform);
                                    dd.transform.localScale = Vector3.one;

                                    Destroy(dd.GetComponent<HoverTooltip>());
                                    Destroy(dd.GetComponent<HideDropdownOptions>());

                                    var d = dd.GetComponent<Dropdown>();
                                    d.onValueChanged.RemoveAllListeners();
                                    d.options.Clear();

                                    string[] PieceTypeNames = Enum.GetNames(typeof(KeyCode));
                                    for (int i = 0; i < PieceTypeNames.Length; i++)
                                    {
                                        d.options.Add(new Dropdown.OptionData(((KeyCode)i).ToString()));
                                    }

                                    d.value = int.Parse(value);

                                    d.onValueChanged.AddListener(delegate (int _val)
                                    {
                                        modifier.GetType().GetField("value", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, _val.ToString());
                                    });
                                }

                                if (cmd.Contains("playerMove") || cmd.Contains("playerRotate"))
                                {
                                    xRT.sizeDelta = new Vector2(350f, 192f);
                                    layout.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 60f);

                                    string[] vector = new string[2];

                                    if (commands[0] == "playerMove" || commands[0] == "playerMoveAll")
                                    {
                                        xRT.sizeDelta = new Vector2(350f, 256f);
                                        layout.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 90f);
                                        vector = value.Split(new char[] { '.' });
                                    }

                                    var input = valueG.transform.Find("input");
                                    var xif = input.gameObject.AddComponent<InputField>();
                                    {
                                        xif.onValueChanged.RemoveAllListeners();
                                        xif.characterValidation = InputField.CharacterValidation.None;
                                        xif.characterLimit = 0;
                                        xif.textComponent = input.Find("Text").GetComponent<Text>();
                                        xif.placeholder = input.Find("Placeholder").GetComponent<Text>();
                                        if (commands[0] == "playerMove" || commands[0] == "playerMoveAll")
                                        {
                                            xif.text = vector[0];
                                        }
                                        else
                                        {
                                            xif.text = value;
                                        }
                                        xif.onValueChanged.AddListener(delegate (string _val)
                                        {
                                            if (commands[0] == "playerMove" || commands[0] == "playerMoveAll")
                                            {
                                                modifier.GetType().GetField("value", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, _val + "." + layout.transform.Find("y/input").GetComponent<InputField>().text);
                                            }
                                            else
                                            {
                                                modifier.GetType().GetField("value", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, _val);
                                            }
                                        });
                                    }

                                    var xet = input.gameObject.AddComponent<EventTrigger>();
                                    xet.triggers.Clear();
                                    xet.triggers.Add(Triggers.ScrollDelta(xif, 0.1f, 10f));

                                    var xifh = input.gameObject.AddComponent<InputFieldHelper>();
                                    xifh.inputField = xif;

                                    var increase = valueG.transform.Find(">").GetComponent<Button>();
                                    {
                                        increase.onClick.RemoveAllListeners();
                                        increase.onClick.AddListener(delegate ()
                                        {
                                            xif.text = (float.Parse(xif.text) + 0.1f).ToString();
                                        });
                                    }

                                    var decrease = valueG.transform.Find("<").GetComponent<Button>();
                                    {
                                        decrease.onClick.RemoveAllListeners();
                                        decrease.onClick.AddListener(delegate ()
                                        {
                                            xif.text = (float.Parse(xif.text) - 0.1f).ToString();
                                        });
                                    }

                                    if (commands[0].Contains("X") || !commands[0].Contains("X") && !commands[0].Contains("Y"))
                                        valueG.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "X";
                                    if (commands[0].Contains("Y"))
                                        valueG.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Y";

                                    //Y
                                    if (commands[0] == "playerMove" || commands[0] == "playerMoveAll")
                                    {
                                        var w = Instantiate(valueG);
                                        w.transform.SetParent(layout.transform);
                                        w.transform.localScale = Vector3.one;

                                        w.name = "y";

                                        w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Y";

                                        var ppinput = w.transform.Find("input");
                                        var ppif = ppinput.gameObject.GetComponent<InputField>();
                                        {
                                            ppif.onValueChanged.RemoveAllListeners();
                                            ppif.characterValidation = InputField.CharacterValidation.None;
                                            ppif.characterLimit = 0;
                                            ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                                            ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                                            ppif.text = vector[1];
                                            ppif.onValueChanged.AddListener(delegate (string _val)
                                            {
                                                modifier.GetType().GetField("value", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, layout.transform.Find("x/input").GetComponent<InputField>().text + "." + _val);
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(Triggers.ScrollDelta(ppif, 0.1f, 10f));

                                        var increase2 = w.transform.Find(">").GetComponent<Button>();
                                        {
                                            increase2.onClick.RemoveAllListeners();
                                            increase2.onClick.AddListener(delegate ()
                                            {
                                                ppif.text = (float.Parse(ppif.text) + 0.1f).ToString();
                                            });
                                        }

                                        var decrease2 = w.transform.Find("<").GetComponent<Button>();
                                        {
                                            decrease2.onClick.RemoveAllListeners();
                                            decrease2.onClick.AddListener(delegate ()
                                            {
                                                ppif.text = (float.Parse(ppif.text) - 0.1f).ToString();
                                            });
                                        }
                                    }

                                    //Duration
                                    {
                                        var w = Instantiate(valueG);
                                        w.transform.SetParent(layout.transform);
                                        w.transform.localScale = Vector3.one;

                                        w.name = "duration";

                                        w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Duration";

                                        var ppinput = w.transform.Find("input");
                                        var ppif = ppinput.gameObject.GetComponent<InputField>();
                                        {
                                            ppif.onValueChanged.RemoveAllListeners();
                                            ppif.characterValidation = InputField.CharacterValidation.None;
                                            ppif.characterLimit = 0;
                                            ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                                            ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                                            ppif.text = commands[1];
                                            ppif.onValueChanged.AddListener(delegate (string _val)
                                            {
                                                commands[1] = _val;
                                                modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(Triggers.ScrollDelta(ppif, 0.1f, 10f));

                                        var increase2 = w.transform.Find(">").GetComponent<Button>();
                                        {
                                            increase2.onClick.RemoveAllListeners();
                                            increase2.onClick.AddListener(delegate ()
                                            {
                                                ppif.text = (float.Parse(ppif.text) + 0.1f).ToString();
                                            });
                                        }

                                        var decrease2 = w.transform.Find("<").GetComponent<Button>();
                                        {
                                            decrease2.onClick.RemoveAllListeners();
                                            decrease2.onClick.AddListener(delegate ()
                                            {
                                                ppif.text = (float.Parse(ppif.text) - 0.1f).ToString();
                                            });
                                        }
                                    }

                                    //Easing
                                    {
                                        var w = Instantiate(valueG);
                                        w.transform.SetParent(layout.transform);
                                        w.transform.localScale = Vector3.one;

                                        w.name = "easing";

                                        w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Easing";

                                        Destroy(w.transform.Find("input").gameObject);
                                        Destroy(w.transform.Find("<").gameObject);
                                        Destroy(w.transform.Find(">").gameObject);

                                        var dd = Instantiate(dropdownInput);
                                        dd.transform.SetParent(w.transform);
                                        dd.transform.localScale = Vector3.one;

                                        Destroy(dd.GetComponent<HoverTooltip>());
                                        Destroy(dd.GetComponent<HideDropdownOptions>());

                                        var d = dd.GetComponent<Dropdown>();
                                        d.onValueChanged.RemoveAllListeners();
                                        d.options.Clear();

                                        foreach (var anim in DataManager.inst.AnimationList)
                                        {
                                            d.options.Add(new Dropdown.OptionData(anim.Name));
                                        }

                                        d.value = int.Parse(commands[2]);

                                        d.onValueChanged.AddListener(delegate (int _val)
                                        {
                                            commands[2] = _val.ToString();
                                            modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                        });
                                    }
                                }

                                if (cmd == "addVariable" || cmd == "subVariable" || cmd == "setVariable" || cmd.Contains("variableOther") || cmd == "setAlphaOther" || cmd == "addColorOther")
                                {
                                    try
                                    {
                                        var w = Instantiate(valueG);
                                        w.transform.SetParent(layout.transform);
                                        w.transform.localScale = Vector3.one;

                                        w.name = "variable-object";

                                        w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Objects Name";

                                        Destroy(w.transform.Find("<").gameObject);
                                        Destroy(w.transform.Find(">").gameObject);

                                        InputField ppinput;
                                        if (w.transform.Find("input").GetComponent<InputField>())
                                            ppinput = w.transform.Find("input").GetComponent<InputField>();
                                        else
                                        {
                                            ppinput = w.transform.Find("input").gameObject.AddComponent<InputField>();
                                            ppinput.characterValidation = InputField.CharacterValidation.None;
                                            ppinput.characterLimit = 0;
                                            ppinput.textComponent = w.transform.Find("input/Text").GetComponent<Text>();
                                            ppinput.placeholder = w.transform.Find("input/Placeholder").GetComponent<Text>();
                                        }

                                        ppinput.onValueChanged.ClearAll();
                                        ppinput.text = commands[1];
                                        ppinput.onValueChanged.AddListener(delegate (string _val)
                                        {
                                            commands[1] = _val;
                                            modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                        });
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.LogFormat("{0}Fricked.\nEXCEPTION: {1}\nSTACKTRACE: {2}", EditorPlugin.className, ex.Message, ex.StackTrace);
                                    }
                                }

                                if (cmd == "loadEquals" || cmd == "loadLesserEquals" || cmd == "loadGreaterEquals" || cmd == "loadLesser" || cmd == "loadGreater" || cmd == "save" || cmd == "saveVariable")
                                {
                                    xRT.sizeDelta = new Vector2(350f, 256f);
                                    layout.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 80f);

                                    var g1 = Instantiate(valueG);
                                    g1.transform.SetParent(layout.transform);
                                    g1.transform.localScale = Vector3.one;

                                    Destroy(g1.transform.Find("<").gameObject);
                                    Destroy(g1.transform.Find(">").gameObject);

                                    var input1 = g1.transform.Find("input");
                                    var xif1 = input1.gameObject.AddComponent<InputField>();
                                    {
                                        xif1.onValueChanged.RemoveAllListeners();
                                        xif1.characterValidation = InputField.CharacterValidation.None;
                                        xif1.characterLimit = 0;
                                        xif1.textComponent = input1.Find("Text").GetComponent<Text>();
                                        xif1.placeholder = input1.Find("Placeholder").GetComponent<Text>();
                                        xif1.text = commands[1];
                                        xif1.onValueChanged.AddListener(delegate (string _val)
                                        {
                                            commands[1] = _val;
                                            modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                        });
                                    }

                                    var g2 = Instantiate(valueG);
                                    g2.transform.SetParent(layout.transform);
                                    g2.transform.localScale = Vector3.one;

                                    Destroy(g2.transform.Find("<").gameObject);
                                    Destroy(g2.transform.Find(">").gameObject);

                                    var input2 = g2.transform.Find("input");
                                    var xif2 = input2.gameObject.AddComponent<InputField>();
                                    {
                                        xif2.onValueChanged.RemoveAllListeners();
                                        xif2.characterValidation = InputField.CharacterValidation.None;
                                        xif2.characterLimit = 0;
                                        xif2.textComponent = input2.Find("Text").GetComponent<Text>();
                                        xif2.placeholder = input2.Find("Placeholder").GetComponent<Text>();
                                        xif2.text = commands[2];
                                        xif2.onValueChanged.AddListener(delegate (string _val)
                                        {
                                            commands[2] = _val;
                                            modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                        });
                                    }

                                    var g3 = Instantiate(valueG);
                                    g3.transform.SetParent(layout.transform);
                                    g3.transform.localScale = Vector3.one;

                                    Destroy(g3.transform.Find("<").gameObject);
                                    Destroy(g3.transform.Find(">").gameObject);

                                    var input3 = g3.transform.Find("input");
                                    var xif3 = input3.gameObject.AddComponent<InputField>();
                                    {
                                        xif3.onValueChanged.RemoveAllListeners();
                                        xif3.characterValidation = InputField.CharacterValidation.None;
                                        xif3.characterLimit = 0;
                                        xif3.textComponent = input3.Find("Text").GetComponent<Text>();
                                        xif3.placeholder = input3.Find("Placeholder").GetComponent<Text>();
                                        xif3.text = commands[3];
                                        xif3.onValueChanged.AddListener(delegate (string _val)
                                        {
                                            commands[3] = _val;
                                            modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                        });
                                    }

                                    if (!commands.Contains("Variable"))
                                    {
                                        valueG.transform.SetAsLastSibling();

                                        var input = valueG.transform.Find("input");
                                        var xif = input.gameObject.AddComponent<InputField>();
                                        {
                                            xif.onValueChanged.RemoveAllListeners();
                                            xif.characterValidation = InputField.CharacterValidation.None;
                                            xif.characterLimit = 0;
                                            xif.textComponent = input.Find("Text").GetComponent<Text>();
                                            xif.placeholder = input.Find("Placeholder").GetComponent<Text>();
                                            xif.text = value;
                                            xif.onValueChanged.AddListener(delegate (string _val)
                                            {
                                                if (int.TryParse(_val, out int num))
                                                {
                                                    modifier.GetType().GetField("value", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, num.ToString());
                                                }
                                            });
                                        }

                                        var xet = input.gameObject.AddComponent<EventTrigger>();
                                        xet.triggers.Clear();
                                        xet.triggers.Add(Triggers.ScrollDeltaInt(xif, 1));

                                        var xifh = input.gameObject.AddComponent<InputFieldHelper>();
                                        xifh.inputField = xif;

                                        var increase = valueG.transform.Find(">").GetComponent<Button>();
                                        {
                                            increase.onClick.RemoveAllListeners();
                                            increase.onClick.AddListener(delegate ()
                                            {
                                                xif.text = (int.Parse(xif.text) + 1).ToString();
                                            });
                                        }

                                        var decrease = valueG.transform.Find("<").GetComponent<Button>();
                                        {
                                            decrease.onClick.RemoveAllListeners();
                                            decrease.onClick.AddListener(delegate ()
                                            {
                                                xif.text = (int.Parse(xif.text) - 1).ToString();
                                            });
                                        }
                                    }
                                    else
                                    {
                                        xRT.sizeDelta = new Vector2(350f, 192f);
                                        layout.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 60f);

                                        Destroy(valueG);
                                    }
                                }

                                if (cmd.Contains("reactive"))
                                {
                                    xRT.sizeDelta = new Vector2(350f, 192f);
                                    layout.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 60f);

                                    if (commands[0] == "reactivePos" || commands[0] == "reactiveSca")
                                    {
                                        xRT.sizeDelta = new Vector2(350f, 256f);
                                        layout.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 90f);
                                    }

                                    var input = valueG.transform.Find("input");
                                    var xif = input.gameObject.AddComponent<InputField>();
                                    {
                                        xif.onValueChanged.RemoveAllListeners();
                                        xif.characterValidation = InputField.CharacterValidation.None;
                                        xif.characterLimit = 0;
                                        xif.textComponent = input.Find("Text").GetComponent<Text>();
                                        xif.placeholder = input.Find("Placeholder").GetComponent<Text>();
                                        xif.text = value;
                                        xif.onValueChanged.AddListener(delegate (string _val)
                                        {
                                            modifier.GetType().GetField("value", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, _val);
                                        });
                                    }

                                    var xet = input.gameObject.AddComponent<EventTrigger>();
                                    xet.triggers.Clear();
                                    xet.triggers.Add(Triggers.ScrollDelta(xif, 0.1f, 10f));

                                    var xifh = input.gameObject.AddComponent<InputFieldHelper>();
                                    xifh.inputField = xif;

                                    var increase = valueG.transform.Find(">").GetComponent<Button>();
                                    {
                                        increase.onClick.RemoveAllListeners();
                                        increase.onClick.AddListener(delegate ()
                                        {
                                            xif.text = (float.Parse(xif.text) + 0.1f).ToString();
                                        });
                                    }

                                    var decrease = valueG.transform.Find("<").GetComponent<Button>();
                                    {
                                        decrease.onClick.RemoveAllListeners();
                                        decrease.onClick.AddListener(delegate ()
                                        {
                                            xif.text = (float.Parse(xif.text) - 0.1f).ToString();
                                        });
                                    }

                                    valueG.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Total Multiply";

                                    if (cmd == "reactivePos" || cmd == "reactiveSca")
                                    {
                                        //Samples
                                        {
                                            var sampleX = Instantiate(valueG);
                                            sampleX.transform.SetParent(layout.transform);
                                            sampleX.transform.localScale = Vector3.one;

                                            sampleX.name = "sample x";

                                            sampleX.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Sample X";

                                            var sampleXInput = sampleX.transform.Find("input");
                                            var sampleXIF = sampleXInput.gameObject.GetComponent<InputField>();
                                            {
                                                sampleXIF.onValueChanged.RemoveAllListeners();
                                                sampleXIF.characterValidation = InputField.CharacterValidation.None;
                                                sampleXIF.characterLimit = 0;
                                                sampleXIF.textComponent = sampleXInput.Find("Text").GetComponent<Text>();
                                                sampleXIF.placeholder = sampleXInput.Find("Placeholder").GetComponent<Text>();
                                                sampleXIF.text = commands[1];
                                                sampleXIF.onValueChanged.AddListener(delegate (string _val)
                                                {
                                                    if (int.TryParse(_val, out int num))
                                                    {
                                                        commands[1] = num.ToString();
                                                        modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                                    }
                                                });
                                            }

                                            var sampleY = Instantiate(valueG);
                                            sampleY.transform.SetParent(layout.transform);
                                            sampleY.transform.localScale = Vector3.one;

                                            sampleY.name = "sample y";

                                            sampleY.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Sample Y";

                                            var sampleYInput = sampleY.transform.Find("input");
                                            var sampleYIF = sampleYInput.gameObject.GetComponent<InputField>();
                                            {
                                                sampleYIF.onValueChanged.RemoveAllListeners();
                                                sampleYIF.characterValidation = InputField.CharacterValidation.None;
                                                sampleYIF.characterLimit = 0;
                                                sampleYIF.textComponent = sampleYInput.Find("Text").GetComponent<Text>();
                                                sampleYIF.placeholder = sampleYInput.Find("Placeholder").GetComponent<Text>();
                                                sampleYIF.text = commands[2];
                                                sampleYIF.onValueChanged.AddListener(delegate (string _val)
                                                {
                                                    if (int.TryParse(_val, out int num))
                                                    {
                                                        commands[2] = num.ToString();
                                                        modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                                    }
                                                });
                                            }

                                            Triggers.AddEventTrigger(sampleXInput.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDeltaInt(sampleXIF, 1, true, new List<int> { 0, 256 }), Triggers.ScrollDeltaVector2Int(sampleXIF, sampleYIF, 1, new List<int> { 0, 256 }) });
                                            Triggers.AddEventTrigger(sampleYInput.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDeltaInt(sampleYIF, 1, true, new List<int> { 0, 256 }), Triggers.ScrollDeltaVector2Int(sampleXIF, sampleYIF, 1, new List<int> { 0, 256 }) });

                                            Triggers.IncreaseDecreaseButtonsInt(sampleXIF, 1, sampleX.transform, new List<int> { 0, 256 });
                                            Triggers.IncreaseDecreaseButtonsInt(sampleYIF, 1, sampleY.transform, new List<int> { 0, 256 });
                                        }

                                        //Multiplies
                                        {
                                            var multiplyX = Instantiate(valueG);
                                            multiplyX.transform.SetParent(layout.transform);
                                            multiplyX.transform.localScale = Vector3.one;

                                            multiplyX.name = "multiply x";

                                            multiplyX.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Multiply X";

                                            var multiplyXInput = multiplyX.transform.Find("input");
                                            var multiplyXIF = multiplyXInput.gameObject.GetComponent<InputField>();
                                            {
                                                multiplyXIF.onValueChanged.RemoveAllListeners();
                                                multiplyXIF.characterValidation = InputField.CharacterValidation.None;
                                                multiplyXIF.characterLimit = 0;
                                                multiplyXIF.textComponent = multiplyXInput.Find("Text").GetComponent<Text>();
                                                multiplyXIF.placeholder = multiplyXInput.Find("Placeholder").GetComponent<Text>();
                                                multiplyXIF.text = commands[3];
                                                multiplyXIF.onValueChanged.AddListener(delegate (string _val)
                                                {
                                                    if (float.TryParse(_val, out float num))
                                                    {
                                                        commands[3] = num.ToString();
                                                        modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                                    }
                                                });
                                            }

                                            var multiplyY = Instantiate(valueG);
                                            multiplyY.transform.SetParent(layout.transform);
                                            multiplyY.transform.localScale = Vector3.one;

                                            multiplyY.name = "multiply y";

                                            multiplyY.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Multiply Y";

                                            var multiplyYInput = multiplyY.transform.Find("input");
                                            var multiplyYIF = multiplyYInput.gameObject.GetComponent<InputField>();
                                            {
                                                multiplyYIF.onValueChanged.RemoveAllListeners();
                                                multiplyYIF.characterValidation = InputField.CharacterValidation.None;
                                                multiplyYIF.characterLimit = 0;
                                                multiplyYIF.textComponent = multiplyYInput.Find("Text").GetComponent<Text>();
                                                multiplyYIF.placeholder = multiplyYInput.Find("Placeholder").GetComponent<Text>();
                                                multiplyYIF.text = commands[4];
                                                multiplyYIF.onValueChanged.AddListener(delegate (string _val)
                                                {
                                                    if (float.TryParse(_val, out float num))
                                                    {
                                                        commands[4] = num.ToString();
                                                        modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                                    }
                                                });
                                            }

                                            Triggers.AddEventTrigger(multiplyXIF.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(multiplyXIF, 0.1f, 10f, true), Triggers.ScrollDeltaVector2(multiplyXIF, multiplyYIF, 0.1f, 10f) });
                                            Triggers.AddEventTrigger(multiplyYIF.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(multiplyYIF, 0.1f, 10f, true), Triggers.ScrollDeltaVector2(multiplyXIF, multiplyYIF, 0.1f, 10f) });

                                            Triggers.IncreaseDecreaseButtons(multiplyYIF, 0.1f, 10f, multiplyY.transform);
                                            Triggers.IncreaseDecreaseButtons(multiplyXIF, 0.1f, 10f, multiplyX.transform);
                                        }
                                    }
                                    else
                                    {
                                        //Sample
                                        {
                                            var w = Instantiate(valueG);
                                            w.transform.SetParent(layout.transform);
                                            w.transform.localScale = Vector3.one;

                                            w.name = "sample";

                                            w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Sample";

                                            var ppinput = w.transform.Find("input");
                                            var ppif = ppinput.gameObject.GetComponent<InputField>();
                                            {
                                                ppif.onValueChanged.RemoveAllListeners();
                                                ppif.characterValidation = InputField.CharacterValidation.None;
                                                ppif.characterLimit = 0;
                                                ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                                                ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                                                ppif.text = commands[1];
                                                ppif.onValueChanged.AddListener(delegate (string _val)
                                                {
                                                    if (int.TryParse(_val, out int num))
                                                    {
                                                        commands[1] = num.ToString();
                                                        modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                                    }
                                                });
                                            }

                                            var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                            ppet.triggers.Clear();
                                            ppet.triggers.Add(Triggers.ScrollDeltaInt(ppif, 1, false, new List<int> { 0, 256 }));

                                            Triggers.IncreaseDecreaseButtonsInt(ppif, 1, w.transform, new List<int> { 0, 256 });
                                        }

                                        if (cmd == "reactiveCol")
                                        {
                                            //Color
                                            {
                                                var w = Instantiate(valueG);
                                                w.transform.SetParent(layout.transform);
                                                w.transform.localScale = Vector3.one;

                                                w.name = "color";

                                                w.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Color";

                                                Destroy(w.transform.Find("input").gameObject);
                                                Destroy(w.transform.Find(">").gameObject);
                                                Destroy(w.transform.Find("<").gameObject);

                                                var color = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/color/color"));
                                                color.transform.SetParent(w.transform);
                                                color.transform.localScale = Vector3.one;
                                                color.name = "color";

                                                if (color.TryGetComponent(out GridLayoutGroup glg))
                                                {
                                                    glg.cellSize = new Vector2(16f, 16f);
                                                    glg.spacing = new Vector2(4.66f, 4.66f);
                                                }
                                                if (color.TryGetComponent(out RectTransform rt))
                                                {
                                                    rt.sizeDelta = new Vector2(183f, 32f);
                                                }

                                                SetObjectColors(color.GetComponentsInChildren<Toggle>(), commands, int.Parse(commands[2]), modifier);
                                            }
                                        }
                                    }
                                }

                                if (cmd == "setPlayerModel")
                                {
                                    xRT.sizeDelta = new Vector2(350f, 164f);
                                    layout.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 40f);

                                    //Pitch (multiplies by current global pitch)
                                    {
                                        var ppvalueG = Instantiate(valueG);
                                        ppvalueG.transform.SetParent(layout.transform);
                                        ppvalueG.transform.localScale = Vector3.one;

                                        ppvalueG.name = "index";

                                        ppvalueG.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Index";

                                        var ppinput = ppvalueG.transform.Find("input");
                                        var ppif = ppinput.gameObject.AddComponent<InputField>();
                                        {
                                            ppif.onValueChanged.RemoveAllListeners();
                                            ppif.characterValidation = InputField.CharacterValidation.None;
                                            ppif.characterLimit = 0;
                                            ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                                            ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                                            ppif.text = commands[1];
                                            ppif.onValueChanged.AddListener(delegate (string _val)
                                            {
                                                if (float.TryParse(_val, out float num))
                                                {
                                                    commands[1] = Mathf.Clamp(num, 0, 3).ToString();
                                                    modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                                }
                                            });
                                        }

                                        var xet = ppinput.gameObject.AddComponent<EventTrigger>();
                                        xet.triggers.Clear();
                                        xet.triggers.Add(Triggers.ScrollDeltaInt(ppif, 1, clamp: new List<int> { 0, 3 }));

                                        var xifh = ppinput.gameObject.AddComponent<InputFieldHelper>();
                                        xifh.inputField = ppif;

                                        var ppincrease = ppvalueG.transform.Find(">").GetComponent<Button>();
                                        {
                                            ppincrease.onClick.RemoveAllListeners();
                                            ppincrease.onClick.AddListener(delegate ()
                                            {
                                                ppif.text = Mathf.Clamp(int.Parse(ppif.text) + 1, 0, 3).ToString();
                                            });
                                        }

                                        var ppdecrease = ppvalueG.transform.Find("<").GetComponent<Button>();
                                        {
                                            ppdecrease.onClick.RemoveAllListeners();
                                            ppdecrease.onClick.AddListener(delegate ()
                                            {
                                                ppif.text = Mathf.Clamp(int.Parse(ppif.text) - 1, 0, 3).ToString();
                                            });
                                        }
                                    }

                                    Destroy(valueG.transform.Find("<").gameObject);
                                    Destroy(valueG.transform.Find(">").gameObject);

                                    var input = valueG.transform.Find("input");
                                    var xif = input.gameObject.AddComponent<InputField>();
                                    {
                                        xif.onValueChanged.RemoveAllListeners();
                                        xif.characterValidation = InputField.CharacterValidation.None;
                                        xif.characterLimit = 0;
                                        xif.textComponent = input.Find("Text").GetComponent<Text>();
                                        xif.placeholder = input.Find("Placeholder").GetComponent<Text>();
                                        xif.text = value;
                                        xif.onValueChanged.AddListener(delegate (string _val)
                                        {
                                            modifier.GetType().GetField("value", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, _val);
                                        });
                                    }
                                }

                                if (cmd == "eventOffset")
                                {
                                    xRT.sizeDelta = new Vector2(350f, 194f);
                                    layout.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 60f);

                                    //Pitch (multiplies by current global pitch)
                                    {
                                        var ppvalueG = Instantiate(valueG);
                                        ppvalueG.transform.SetParent(layout.transform);
                                        ppvalueG.transform.localScale = Vector3.one;

                                        ppvalueG.name = "index";

                                        ppvalueG.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Index";

                                        var ppinput = ppvalueG.transform.Find("input");
                                        var ppif = ppinput.gameObject.AddComponent<InputField>();
                                        {
                                            ppif.onValueChanged.RemoveAllListeners();
                                            ppif.characterValidation = InputField.CharacterValidation.None;
                                            ppif.characterLimit = 0;
                                            ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                                            ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                                            ppif.text = commands[1];
                                            ppif.onValueChanged.AddListener(delegate (string _val)
                                            {
                                                if (int.TryParse(_val, out int num))
                                                {
                                                    commands[1] = num.ToString();
                                                    modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                                }
                                            });
                                        }

                                        var xet = ppinput.gameObject.AddComponent<EventTrigger>();
                                        xet.triggers.Clear();
                                        xet.triggers.Add(Triggers.ScrollDeltaInt(ppif, 1, clamp: new List<int> { 0, 25 }));

                                        var xifh = ppinput.gameObject.AddComponent<InputFieldHelper>();
                                        xifh.inputField = ppif;

                                        var ppincrease = ppvalueG.transform.Find(">").GetComponent<Button>();
                                        {
                                            ppincrease.onClick.RemoveAllListeners();
                                            ppincrease.onClick.AddListener(delegate ()
                                            {
                                                ppif.text = Mathf.Clamp(int.Parse(ppif.text) + 1, 0, 25).ToString();
                                            });
                                        }

                                        var ppdecrease = ppvalueG.transform.Find("<").GetComponent<Button>();
                                        {
                                            ppdecrease.onClick.RemoveAllListeners();
                                            ppdecrease.onClick.AddListener(delegate ()
                                            {
                                                ppif.text = Mathf.Clamp(int.Parse(ppif.text) - 1, 0, 25).ToString();
                                            });
                                        }
                                    }

                                    //Pitch (multiplies by current global pitch)
                                    {
                                        var ppvalueG = Instantiate(valueG);
                                        ppvalueG.transform.SetParent(layout.transform);
                                        ppvalueG.transform.localScale = Vector3.one;

                                        ppvalueG.name = "index";

                                        ppvalueG.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "VIndex";

                                        var ppinput = ppvalueG.transform.Find("input");
                                        var ppif = ppinput.gameObject.AddComponent<InputField>();
                                        {
                                            ppif.onValueChanged.RemoveAllListeners();
                                            ppif.characterValidation = InputField.CharacterValidation.None;
                                            ppif.characterLimit = 0;
                                            ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                                            ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                                            ppif.text = commands[2];
                                            ppif.onValueChanged.AddListener(delegate (string _val)
                                            {
                                                if (int.TryParse(_val, out int num))
                                                {
                                                    commands[2] = num.ToString();
                                                    modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                                }
                                            });
                                        }

                                        var xet = ppinput.gameObject.AddComponent<EventTrigger>();
                                        xet.triggers.Clear();
                                        xet.triggers.Add(Triggers.ScrollDeltaInt(ppif, 1, clamp: new List<int> { 0, DataManager.inst.gameData.eventObjects.allEvents[int.Parse(commands[1])][0].eventValues.Length - 1 }));

                                        var xifh = ppinput.gameObject.AddComponent<InputFieldHelper>();
                                        xifh.inputField = ppif;

                                        var ppincrease = ppvalueG.transform.Find(">").GetComponent<Button>();
                                        {
                                            ppincrease.onClick.RemoveAllListeners();
                                            ppincrease.onClick.AddListener(delegate ()
                                            {
                                                ppif.text = Mathf.Clamp(int.Parse(ppif.text) + 1, 0, 16).ToString();
                                            });
                                        }

                                        var ppdecrease = ppvalueG.transform.Find("<").GetComponent<Button>();
                                        {
                                            ppdecrease.onClick.RemoveAllListeners();
                                            ppdecrease.onClick.AddListener(delegate ()
                                            {
                                                ppif.text = Mathf.Clamp(int.Parse(ppif.text) - 1, 0, 16).ToString();
                                            });
                                        }
                                    }

                                    //Destroy(valueG.transform.Find("<").gameObject);
                                    //Destroy(valueG.transform.Find(">").gameObject);

                                    var input = valueG.transform.Find("input");
                                    var xif = input.gameObject.AddComponent<InputField>();
                                    {
                                        xif.onValueChanged.RemoveAllListeners();
                                        xif.characterValidation = InputField.CharacterValidation.None;
                                        xif.characterLimit = 0;
                                        xif.textComponent = input.Find("Text").GetComponent<Text>();
                                        xif.placeholder = input.Find("Placeholder").GetComponent<Text>();
                                        xif.text = value;
                                        xif.onValueChanged.AddListener(delegate (string _val)
                                        {
                                            if (float.TryParse(_val, out float num))
                                            {
                                                modifier.GetType().GetField("value", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, num.ToString());
                                            }
                                        });

                                        Triggers.IncreaseDecreaseButtons(xif, 0.1f, 10f, valueG.transform);
                                        Triggers.AddEventTrigger(input.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(xif, 0.1f, 10f) });
                                    }
                                }

                                if (cmd.Contains("addColor"))
                                {
                                    xRT.sizeDelta = new Vector2(350f, 194f);
                                    layout.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 60f);

                                    //Index
                                    {
                                        var ppvalueG = Instantiate(valueG);
                                        ppvalueG.transform.SetParent(layout.transform);
                                        ppvalueG.transform.localScale = Vector3.one;

                                        ppvalueG.name = "index";

                                        ppvalueG.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Index";

                                        var ppinput = ppvalueG.transform.Find("input");
                                        var ppif = ppinput.gameObject.AddComponent<InputField>();
                                        {
                                            ppif.onValueChanged.RemoveAllListeners();
                                            ppif.characterValidation = InputField.CharacterValidation.None;
                                            ppif.characterLimit = 0;
                                            ppif.textComponent = ppinput.Find("Text").GetComponent<Text>();
                                            ppif.placeholder = ppinput.Find("Placeholder").GetComponent<Text>();
                                            if (!cmd.Contains("Other"))
                                                ppif.text = commands[1];
                                            else
                                                ppif.text = commands[2];
                                            ppif.onValueChanged.AddListener(delegate (string _val)
                                            {
                                                if (int.TryParse(_val, out int num))
                                                {
                                                    if (!cmd.Contains("Other"))
                                                        commands[1] = num.ToString();
                                                    else
                                                        commands[2] = num.ToString();
                                                    modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
                                                }
                                            });
                                        }

                                        var xet = ppinput.gameObject.AddComponent<EventTrigger>();
                                        xet.triggers.Clear();
                                        xet.triggers.Add(Triggers.ScrollDeltaInt(ppif, 1, clamp: new List<int> { 0, 16 }));

                                        var xifh = ppinput.gameObject.AddComponent<InputFieldHelper>();
                                        xifh.inputField = ppif;

                                        var ppincrease = ppvalueG.transform.Find(">").GetComponent<Button>();
                                        {
                                            ppincrease.onClick.RemoveAllListeners();
                                            ppincrease.onClick.AddListener(delegate ()
                                            {
                                                ppif.text = Mathf.Clamp(int.Parse(ppif.text) + 1, 0, 16).ToString();
                                            });
                                        }

                                        var ppdecrease = ppvalueG.transform.Find("<").GetComponent<Button>();
                                        {
                                            ppdecrease.onClick.RemoveAllListeners();
                                            ppdecrease.onClick.AddListener(delegate ()
                                            {
                                                ppif.text = Mathf.Clamp(int.Parse(ppif.text) - 1, 0, 16).ToString();
                                            });
                                        }
                                    }

                                    //Destroy(valueG.transform.Find("<").gameObject);
                                    //Destroy(valueG.transform.Find(">").gameObject);

                                    var input = valueG.transform.Find("input");
                                    var xif = input.gameObject.AddComponent<InputField>();
                                    {
                                        xif.onValueChanged.RemoveAllListeners();
                                        xif.characterValidation = InputField.CharacterValidation.None;
                                        xif.characterLimit = 0;
                                        xif.textComponent = input.Find("Text").GetComponent<Text>();
                                        xif.placeholder = input.Find("Placeholder").GetComponent<Text>();
                                        xif.text = value;
                                        xif.onValueChanged.AddListener(delegate (string _val)
                                        {
                                            if (float.TryParse(_val, out float num))
                                            {
                                                modifier.GetType().GetField("value", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, num.ToString());
                                            }
                                        });

                                        Triggers.IncreaseDecreaseButtons(xif, 0.1f, 10f, valueG.transform);
                                        Triggers.AddEventTrigger(input.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(xif, 0.1f, 10f) });
                                    }
                                }

                                //Delete Modifier
                                {
                                    int tmpIndex = j;

                                    var delete = Instantiate(close.gameObject);
                                    delete.transform.SetParent(x.transform);
                                    delete.transform.localScale = Vector3.one;
                                    //delete.GetComponent<RectTransform>().anchoredPosition = new Vector2(333f, 0f);
                                    delete.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                                    delete.name = "delete";

                                    var deleteButton = delete.GetComponent<Button>();
                                    deleteButton.onClick.ClearAll();
                                    deleteButton.onClick.AddListener(delegate ()
                                    {
                                        RemoveModifierIndex(beatmapObject, tmpIndex);
                                        RTEditor.inst.StartCoroutine(RTEditor.RefreshObjectGUI());
                                    });
                                }
                            }
                        }
                    }

                    if (count == 1)
                    {
                        scrollViewRT.sizeDelta = new Vector2(351f, 174f);
                    }
                    else if (count == 2)
                    {
                        scrollViewRT.sizeDelta = new Vector2(351f, 310f);
                    }
                    else
                    {
                        scrollViewRT.sizeDelta = new Vector2(351f, 445f);
                    }
                }
                else
                {
                    scrollViewRT.sizeDelta = new Vector2(351f, 72f);
                }

                //Add Modifier
                {
                    var button = Instantiate(EditorManager.inst.folderButtonPrefab);
                    button.transform.SetParent(content);
                    button.transform.localScale = Vector3.one;
                    button.name = "add modifier";

                    button.transform.GetChild(0).GetComponent<Text>().text = "+";
                    button.transform.GetChild(0).GetComponent<Text>().alignment = TextAnchor.MiddleCenter;

                    var butt = button.GetComponent<Button>();
                    butt.onClick.RemoveAllListeners();
                    butt.onClick.AddListener(delegate ()
                    {
                        EditorManager.inst.ShowDialog("Default Modifiers Popup");
                    });
                }
            }

            yield break;
        }

        public static void SetObjectColors(Toggle[] toggles, List<string> commands, int i, object modifier)
        {
            commands[2] = i.ToString();
            modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, commands);
            int num6 = 0;
            foreach (Toggle toggle in toggles)
            {
                toggle.onValueChanged.RemoveAllListeners();
                int tmpIndex = num6;
                if (num6 == i)
                {
                    toggle.isOn = true;
                }
                else
                {
                    toggle.isOn = false;
                }
                toggle.onValueChanged.AddListener(delegate (bool _value)
                {
                    SetObjectColors(toggles, commands, tmpIndex, modifier);
                });
                toggle.GetComponent<Image>().color = GameManager.inst.LiveTheme.GetObjColor(tmpIndex);
                if (!toggle.GetComponent<HoverUI>())
                {
                    var hoverUI = toggle.gameObject.AddComponent<HoverUI>();
                    hoverUI.animatePos = false;
                    hoverUI.animateSca = true;
                    hoverUI.size = 1.1f;
                }
                num6++;
            }
        }

        public static void OpenREPLEditor(object modifier, string value)
        {
            EditorManager.inst.ShowDialog("REPL Editor Popup");
            replEditor.onValueChanged.ClearAll();
            replEditor.text = value;
            replText.text = RTCode.ConvertREPL(value);
            replEditor.onValueChanged.AddListener(delegate (string _val)
            {
                replText.text = RTCode.ConvertREPL(_val);
                modifier.GetType().GetField("value", BindingFlags.Public | BindingFlags.Instance).SetValue(modifier, _val);
            });

            replEditor.onEndEdit.RemoveAllListeners();
            replEditor.onEndEdit.AddListener(delegate (string _val)
            {
                RTEditor.inst.StartCoroutine(RTEditor.RefreshObjectGUI());
            });
        }

        public static void CreateDefaultModifiersList()
        {
            var dialog = Instantiate(EditorManager.inst.GetDialog("Quick Actions Popup").Dialog.gameObject);
            dialog.transform.SetParent(EditorManager.inst.GetDialog("Quick Actions Popup").Dialog.parent);
            dialog.transform.localScale = Vector3.one;
            dialog.transform.localPosition = Vector3.zero;
            dialog.name = "DefaultModifiersPopup";

            Triggers.AddEditorDialog("Default Modifiers Popup", dialog);

            if (dialog.transform.Find("Panel/mod-helper"))
            {
                Destroy(dialog.transform.Find("Panel/mod-helper").gameObject);
            }
            if (dialog.transform.Find("command-input"))
            {
                Destroy(dialog.transform.Find("command-input").gameObject);
            }

            var search = dialog.transform.Find("search-box/search").GetComponent<InputField>();
            search.onValueChanged.ClearAll();
            search.text = searchTerm;
            search.onValueChanged.AddListener(delegate (string _val)
            {
                searchTerm = _val;
                RefreshDefaultModifiersList();
            });

            var close = dialog.transform.Find("Panel/x").GetComponent<Button>();
            close.onClick.ClearAll();
            close.onClick.AddListener(delegate ()
            {
                EditorManager.inst.HideDialog("Default Modifiers Popup");
            });

            RefreshDefaultModifiersList();
        }

        public static string searchTerm;
        public static void RefreshDefaultModifiersList()
        {
            if (ModCompatibility.sharedFunctions.ContainsKey("ObjectModifiersModifierList"))
                defaultModifiers = (List<string>)ModCompatibility.sharedFunctions["ObjectModifiersModifierList"];

            var dialog = EditorManager.inst.GetDialog("Default Modifiers Popup").Dialog.gameObject;

            var contentM = dialog.transform.Find("mask/content");
            LSFunctions.LSHelpers.DeleteChildren(contentM);

            for (int i = 0; i < defaultModifiers.Count; i++)
            {
                if (searchTerm == null || !(searchTerm != "") || defaultModifiers[i].ToLower().Contains(searchTerm.ToLower()))
                {
                    int tmpIndex = i;

                    var button = Instantiate(EditorManager.inst.folderButtonPrefab);
                    button.transform.SetParent(contentM);
                    button.transform.localScale = Vector3.one;
                    button.name = defaultModifiers[i];

                    button.transform.GetChild(0).GetComponent<Text>().text = defaultModifiers[i];

                    var butt = button.GetComponent<Button>();
                    butt.onClick.RemoveAllListeners();
                    butt.onClick.AddListener(delegate ()
                    {
                        if (ObjEditor.inst.currentObjectSelection.IsObject())
                        {
                            AddModifierToObject(ObjEditor.inst.currentObjectSelection.GetObjectData(), tmpIndex);
                            RTEditor.inst.StartCoroutine(RTEditor.RefreshObjectGUI());
                            EditorManager.inst.HideDialog("Default Modifiers Popup");
                        }
                        else
                        {
                            EditorManager.inst.DisplayNotification("Cannot add modifier to prefab!", 2f, EditorManager.NotificationType.Error);
                            EditorManager.inst.HideDialog("Default Modifiers Popup");
                        }
                    });
                }
            }
        }

        public static List<string> defaultModifiers = new List<string>
        {
            "setPitch (Action)",
            "addPitch (Action)",
            "setMusicTime (Action)",
            "playSound (Action)",
            "loadLevel (Action)",
            "blur (Action)",
            "particleSystem (Action)",
            "trailRenderer (Action)",
            "spawnPrefab (Action)",
            "playerHeal (Action)",
            "playerHealAll (Action)",
            "playerHit (Action)",
            "playerHitAll (Action)",
            "playerKill (Action)",
            "playerKillAll (Action)",
            "playerMove (Action)",
            "playerMoveAll (Action)",
            "playerMoveX (Action)",
            "playerMoveXAll (Action)",
            "playerMoveY (Action)",
            "playerMoveYAll (Action)",
            "playerRotate (Action)",
            "playerRotateAll (Action)",
            "playerBoost (Action)",
            "playerBoostAll (Action)",
            "playerDisableBoost (Action)",
            "showMouse (Action)",
            "hideMouse (Action)",
            "addVariable (Action)",
            "subVariable (Action)",
            "setVariable (Action)",
            "quitToMenu (Action)",
            "quitToArcade (Action)",
            "disableObject (Action)",
            "disableObjectTree (Action)",
            "save (Action)",
            "saveVariable (Action)",
            "reactivePos (Action)",
            "reactiveSca (Action)",
            "reactiveRot (Action)",
            "reactiveCol (Action)",
            "playerCollide (Trigger)",
            "playerHealthEquals (Trigger)",
            "playerHealthLesserEquals (Trigger)",
            "playerHealthGreaterEquals (Trigger)",
            "playerHealthLesser (Trigger)",
            "playerHealthGreater (Trigger)",
            "playerDeathsEquals (Trigger)",
            "playerDeathsLesserEquals (Trigger)",
            "playerDeathsGreaterEquals (Trigger)",
            "playerDeathsLesser (Trigger)",
            "playerDeathsGreater (Trigger)",
            "playerMoving (Trigger)",
            "playerBoosting (Trigger)",
            "playerAlive (Trigger)",
            "playerDistanceLesser (Trigger)",
            "playerDistanceGreater (Trigger)",
            "keyPressDown (Trigger)",
            "keyPress (Trigger)",
            "keyPressUp (Trigger)",
            "mouseButtonDown (Trigger)",
            "mouseButton (Trigger)",
            "mouseButtonUp (Trigger)",
            "mouseOver (Trigger)",
            "loadEquals (Trigger)",
            "loadLesserEquals (Trigger)",
            "loadGreaterEquals (Trigger)",
            "loadLesser (Trigger)",
            "loadGreater (Trigger)",
            "variableEquals (Trigger)",
            "variableLesserEquals (Trigger)",
            "variableGreaterEquals (Trigger)",
            "variableLesser (Trigger)",
            "variableGreater (Trigger)",
            "pitchEquals (Trigger)",
            "pitchLesserEquals (Trigger)",
            "pitchGreaterEquals (Trigger)",
            "pitchLesser (Trigger)",
            "pitchGreater (Trigger)",
        };

        public static void CreateHomingTimeline()
        {

        }

        public static void CreateHomingDialog()
        {

        }

        public static GameObject Keyframe()
        {
            return null;
        }
    }
}
