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
using EditorManagement.Functions.Helpers;
using EditorManagement.Functions;

using RTFunctions.Functions;
using RTFunctions.Functions.Data;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;
using RTFunctions.Functions.Optimization;

using BaseBeatmapObject = DataManager.GameData.BeatmapObject;

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
                if (ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
                    RTEditor.inst.StartCoroutine(ObjectEditor.RefreshObjectGUI(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>()));
            });

            var e = Instantiate(bmb);

            scrollView = e.transform;

            scrollView.SetParent(bmb.transform.Find("Viewport/Content"));
            scrollView.localScale = Vector3.one;
            scrollView.name = "Modifiers Scroll View";

            scrollViewRT = scrollView.GetComponent<RectTransform>();

            content = scrollView.Find("Viewport/Content");
            LSHelpers.DeleteChildren(content);

            scrollView.gameObject.SetActive(showModifiers);

            //var font = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("Panel/Text").GetComponent<Text>().font;

            //replBase = new GameObject("REPL Editor");
            //replBase.transform.SetParent(EditorManager.inst.GetDialog("Quick Actions Popup").Dialog.parent);
            //replBase.transform.localScale = Vector3.one;
            //var replRT = replBase.AddComponent<RectTransform>();

            //replRT.anchoredPosition = Vector2.zero;

            //var uiField = UIManager.GenerateUIInputField("REPL Editor", replBase.transform);

            //replEditor = (InputField)uiField["InputField"];

            //((Image)uiField["Image"]).color = new Color(0.1132075f, 0.1132075f, 0.1132075f);

            //replEditor.lineType = InputField.LineType.MultiLineNewline;
            //replEditor.textComponent.color = new Color(0.9788679f, 0.9788679f, 0.9788679f, 1f);
            //replEditor.textComponent.font = font;

            //((RectTransform)uiField["RectTransform"]).anchoredPosition = Vector2.zero;
            //((RectTransform)uiField["RectTransform"]).sizeDelta = new Vector2(1000f, 550f);

            //var uiTop = UIManager.GenerateUIImage("Panel", replBase.transform);

            //UIManager.SetRectTransform((RectTransform)uiTop["RectTransform"], new Vector2(0f, 291f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1000f, 32f));

            //((Image)uiTop["Image"]).color = new Color(0.1973585f, 0.1973585f, 0.1973585f);

            //var close = Instantiate(EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("Panel/x").gameObject);
            //close.transform.SetParent(((GameObject)uiTop["GameObject"]).transform);
            //close.transform.localScale = Vector3.one;

            //close.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            //var closeButton = close.GetComponent<Button>();
            //closeButton.onClick.ClearAll();
            //closeButton.onClick.AddListener(delegate ()
            //{
            //    EditorManager.inst.HideDialog("REPL Editor Popup");
            //    RTEditor.inst.StartCoroutine(ObjectEditor.RefreshObjectGUI(ObjectEditor.inst.CurrentSelection));
            //});

            //var uiTitle = UIManager.GenerateUIText("Title", ((GameObject)uiTop["GameObject"]).transform);
            //UIManager.SetRectTransform(((RectTransform)uiTitle["RectTransform"]), new Vector2(-350f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(300f, 32f));
            //((Text)uiTitle["Text"]).text = "REPL Editor";
            //((Text)uiTitle["Text"]).alignment = TextAnchor.MiddleLeft;
            //((Text)uiTitle["Text"]).font = font;

            //var rtext = Instantiate(replEditor.textComponent.gameObject);
            //rtext.transform.SetParent(replEditor.transform);
            //rtext.transform.localScale = Vector3.one;

            //var rttext = rtext.GetComponent<RectTransform>();
            //rttext.anchoredPosition = new Vector2(2f, 0f);
            //rttext.sizeDelta = new Vector2(-12f, -8f);

            //var selectUI = ((GameObject)uiTop["GameObject"]).AddComponent<SelectGUI>();
            //selectUI.target = replBase.transform;

            //replEditor.textComponent.color = new Color(0.9788679f, 0.9788679f, 0.9788679f, 0f);

            //replEditor.customCaretColor = true;
            //replEditor.caretColor = new Color(0.9788679f, 0.9788679f, 0.9788679f, 1f);

            //replText = rtext.GetComponent<Text>();

            //replBase.SetActive(false);

            //Triggers.AddEditorDialog("REPL Editor Popup", replBase);
        }

        //I probably should write this so it switches between what command it is and has the same copied and pasted code but altered just so it's not spaghetti
        public static IEnumerator RenderModifiers(BeatmapObject beatmapObject)
        {
            if (EditorManager.inst.isEditing)
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

                var count = beatmapObject.modifiers.Count;

                if (count > 0)
                {
                    for (int j = 0; j < count; j++)
                    {
                        var modifier = beatmapObject.modifiers[j];

                        var type = (int)modifier.type;

                        var commands = modifier.commands;

                        var value = modifier.value;

                        var constant = modifier.constant;

                        var notGate = modifier.not;

                        if (commands.Count > 0 && !string.IsNullOrEmpty(commands[0]))
                        {
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

                                    if (x.GetComponent<InputFieldSwapper>())
                                        Destroy(x.GetComponent<InputFieldSwapper>());

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
                                            toggle.isOn = constant;
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
                                            toggle.isOn = notGate;
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
                                                TooltipHelper.AddTooltip(x, commands[0], "Sets the pitch to this value. If EventsCore is installed, it will set an offset to the current pitch.");
                                                break;
                                            }
                                        case "setMusicTime":
                                            {
                                                TooltipHelper.AddTooltip(x, commands[0], "Sets the song time. Good for skipping specific parts of a song or looping it. Make sure players have a way out of this loop.");
                                                break;
                                            }
                                        case "blur":
                                            {
                                                TooltipHelper.AddTooltip(x, commands[0], "Replaces the objects' material with a blur effect.");
                                                break;
                                            }
                                        case "pitchEquals":
                                            {
                                                xRT.sizeDelta = new Vector2(350f, 160f);
                                                layout.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 40f);
                                                TooltipHelper.AddTooltip(x, commands[0], "Activates modifiers when the pitch equals the value.");
                                                break;
                                            }
                                        case "pitchLesserEquals":
                                            {
                                                xRT.sizeDelta = new Vector2(350f, 160f);
                                                layout.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 40f);
                                                TooltipHelper.AddTooltip(x, commands[0], "Activates modifiers when the pitch equals and is lesser than the value.");
                                                break;
                                            }
                                        case "pitchGreaterEquals":
                                            {
                                                xRT.sizeDelta = new Vector2(350f, 160f);
                                                layout.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 40f);
                                                TooltipHelper.AddTooltip(x, commands[0], "Activates modifiers when the pitch equals and is greater than the value.");
                                                break;
                                            }
                                        case "pitchLesser":
                                            {
                                                xRT.sizeDelta = new Vector2(350f, 160f);
                                                layout.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 40f);
                                                TooltipHelper.AddTooltip(x, commands[0], "Activates modifiers when the pitch is lesser than the value.");
                                                break;
                                            }
                                        case "pitchGreater":
                                            {
                                                xRT.sizeDelta = new Vector2(350f, 160f);
                                                layout.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 40f);
                                                TooltipHelper.AddTooltip(x, commands[0], "Activates modifiers when the pitch is greater than the value.");
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
                                                modifier.value = num.ToString();
                                            }
                                        });
                                    }

                                    var xet = input.gameObject.AddComponent<EventTrigger>();
                                    xet.triggers.Clear();
                                    xet.triggers.Add(TriggerHelper.ScrollDelta(xif));

                                    var xifh = input.gameObject.AddComponent<InputFieldSwapper>();
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
                                                modifier.value = num.ToString();
                                            }
                                        });
                                    }

                                    var xet = input.gameObject.AddComponent<EventTrigger>();
                                    xet.triggers.Clear();
                                    xet.triggers.Add(TriggerHelper.ScrollDelta(xif));

                                    var xifh = input.gameObject.AddComponent<InputFieldSwapper>();
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

                                    // If Blur should use opacity keyframes to animate blur amount
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
                                                    modifier.commands = commands;
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
                                                    modifier.commands = commands;
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
                                                    modifier.commands = commands;
                                                }
                                            });
                                        }

                                        var xet = ppinput.gameObject.AddComponent<EventTrigger>();
                                        xet.triggers.Clear();
                                        xet.triggers.Add(TriggerHelper.ScrollDelta(ppif));

                                        var xifh = ppinput.gameObject.AddComponent<InputFieldSwapper>();
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
                                                    modifier.commands = commands;
                                                }
                                            });
                                        }

                                        var xet = ppinput.gameObject.AddComponent<EventTrigger>();
                                        xet.triggers.Clear();
                                        xet.triggers.Add(TriggerHelper.ScrollDelta(ppif, max: 2f));

                                        var xifh = ppinput.gameObject.AddComponent<InputFieldSwapper>();
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
                                                    modifier.commands = commands;
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
                                            modifier.value = _val;
                                        });
                                    }
                                }

                                if (cmd == "loadLevel" || cmd == "code")
                                {
                                    TooltipHelper.AddTooltip(x, commands[0], "");

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
                                        TooltipHelper.AddTooltip(input.gameObject, "Right click this to open the REPL Editor.", "");

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

                                    TooltipHelper.AddTooltip(x, commands[0], "");

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
                                                modifier.value = num.ToString();
                                            }
                                        });
                                    }

                                    var xet = input.gameObject.AddComponent<EventTrigger>();
                                    xet.triggers.Clear();
                                    xet.triggers.Add(TriggerHelper.ScrollDelta(xif));

                                    var xifh = input.gameObject.AddComponent<InputFieldSwapper>();
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
                                                    num = Mathf.Clamp(num, 0, ObjectManager.inst.objectPrefabs.Count - 1);

                                                    commands[1] = num.ToString();
                                                    commands[2] = "0";
                                                    modifier.commands = commands;
                                                    Updater.UpdateProcessor(beatmapObject);
                                                    RTEditor.inst.StartCoroutine(ObjectEditor.RefreshObjectGUI(beatmapObject));
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(TriggerHelper.ScrollDeltaInt(ppif, max: ObjectManager.inst.objectPrefabs.Count - 1));
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
                                                    modifier.commands = commands;
                                                    Updater.UpdateProcessor(beatmapObject);
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();

                                        int n;
                                        int.TryParse(commands[1], out n);
                                        ppet.triggers.Add(TriggerHelper.ScrollDeltaInt(ppif, 1, 0, ObjectManager.inst.objectPrefabs[n].options.Count - 1));
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
                                                    modifier.commands = commands;
                                                    Updater.UpdateProcessor(beatmapObject);
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(TriggerHelper.ScrollDeltaInt(ppif, 1, 0, GameManager.inst.LiveTheme.objectColors.Count - 1));
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
                                                    modifier.commands = commands;
                                                    Updater.UpdateProcessor(beatmapObject);
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(TriggerHelper.ScrollDelta(ppif));
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
                                                    modifier.commands = commands;
                                                    Updater.UpdateProcessor(beatmapObject);
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(TriggerHelper.ScrollDelta(ppif, 0.1f, 10f));
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
                                                    modifier.commands = commands;
                                                    Updater.UpdateProcessor(beatmapObject);
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(TriggerHelper.ScrollDelta(ppif, 0.1f, 10f));
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
                                                    modifier.commands = commands;
                                                    Updater.UpdateProcessor(beatmapObject);
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(TriggerHelper.ScrollDelta(ppif, 0.1f, 10f));
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
                                                    modifier.commands = commands;
                                                    Updater.UpdateProcessor(beatmapObject);
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(TriggerHelper.ScrollDelta(ppif, 0.1f, 10f));
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
                                                    modifier.commands = commands;
                                                    Updater.UpdateProcessor(beatmapObject);
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(TriggerHelper.ScrollDelta(ppif, 0.1f, 10f));
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
                                                    modifier.commands = commands;
                                                    Updater.UpdateProcessor(beatmapObject);
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(TriggerHelper.ScrollDeltaInt(ppif, 1));
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
                                                    modifier.commands = commands;
                                                    Updater.UpdateProcessor(beatmapObject);
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(TriggerHelper.ScrollDelta(ppif, 0.1f, 10f));
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
                                                    modifier.commands = commands;
                                                    Updater.UpdateProcessor(beatmapObject);
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(TriggerHelper.ScrollDelta(ppif, 0.1f, 10f));
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
                                                    modifier.commands = commands;
                                                    Updater.UpdateProcessor(beatmapObject);
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(TriggerHelper.ScrollDelta(ppif, 0.1f, 10f));
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
                                            modifier.commands = commands;
                                            Updater.UpdateProcessor(beatmapObject);
                                        });
                                    }
                                }

                                if (cmd == "trailRenderer")
                                {
                                    xRT.sizeDelta = new Vector2(350f, 352f);
                                    layout.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 140f);

                                    TooltipHelper.AddTooltip(x, commands[0], "");

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
                                                modifier.value = num.ToString();
                                            }
                                        });
                                    }

                                    var xet = input.gameObject.AddComponent<EventTrigger>();
                                    xet.triggers.Clear();
                                    xet.triggers.Add(TriggerHelper.ScrollDelta(xif));

                                    var xifh = input.gameObject.AddComponent<InputFieldSwapper>();
                                    xifh.inputField = xif;

                                    TriggerHelper.IncreaseDecreaseButtons(xif, t: valueG.transform);

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
                                                    modifier.commands = commands;
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(TriggerHelper.ScrollDelta(ppif));
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
                                                    modifier.commands = commands;
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(TriggerHelper.ScrollDelta(ppif));
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
                                                    modifier.commands = commands;
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(TriggerHelper.ScrollDeltaInt(ppif, 1, 0, GameManager.inst.LiveTheme.objectColors.Count - 1));
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
                                                    modifier.commands = commands;
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(TriggerHelper.ScrollDeltaInt(ppif, 1, 0, GameManager.inst.LiveTheme.objectColors.Count - 1));
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
                                                    modifier.commands = commands;
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(TriggerHelper.ScrollDelta(ppif));
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
                                                    modifier.commands = commands;
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(TriggerHelper.ScrollDelta(ppif, 0.1f, 10f));
                                    }
                                }

                                if (cmd == "spawnPrefab")
                                {
                                    xRT.sizeDelta = new Vector2(350f, 352f);
                                    layout.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 140f);

                                    TooltipHelper.AddTooltip(x, commands[0], "");

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
                                    xet.triggers.Add(TriggerHelper.ScrollDeltaInt(xif, 1, 0, DataManager.inst.gameData.prefabs.Count - 1));

                                    var xifh = input.gameObject.AddComponent<InputFieldSwapper>();
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

                                    TriggerHelper.IncreaseDecreaseButtonsInt(xif, t: valueG.transform);

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
                                                    modifier.commands = commands;
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(TriggerHelper.ScrollDelta(ppif, multi: true));
                                        ppet.triggers.Add(TriggerHelper.ScrollDeltaVector2(ppif, layout.transform.Find("pos y").GetComponent<InputField>(), 0.1f, 10f));

                                        var ppifh = input.gameObject.AddComponent<InputFieldSwapper>();
                                        ppifh.inputField = ppif;

                                        TriggerHelper.IncreaseDecreaseButtons(ppif, t: w.transform);
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
                                                    modifier.commands = commands;
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(TriggerHelper.ScrollDelta(ppif, multi: true));
                                        ppet.triggers.Add(TriggerHelper.ScrollDeltaVector2(layout.transform.Find("pos x").GetComponent<InputField>(), ppif, 0.1f, 10f));

                                        var ppifh = input.gameObject.AddComponent<InputFieldSwapper>();
                                        ppifh.inputField = ppif;

                                        TriggerHelper.IncreaseDecreaseButtons(ppif, t: w.transform);
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
                                                    modifier.commands = commands;
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(TriggerHelper.ScrollDelta(ppif, multi: true));
                                        ppet.triggers.Add(TriggerHelper.ScrollDeltaVector2(ppif, layout.transform.Find("sca y").GetComponent<InputField>(), 0.1f, 10f));

                                        var ppifh = input.gameObject.AddComponent<InputFieldSwapper>();
                                        ppifh.inputField = ppif;

                                        TriggerHelper.IncreaseDecreaseButtons(ppif, t: w.transform);
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
                                                    modifier.commands = commands;
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(TriggerHelper.ScrollDelta(ppif, multi: true));
                                        ppet.triggers.Add(TriggerHelper.ScrollDeltaVector2(layout.transform.Find("sca x").GetComponent<InputField>(), ppif, 0.1f, 10f));

                                        var ppifh = input.gameObject.AddComponent<InputFieldSwapper>();
                                        ppifh.inputField = ppif;

                                        TriggerHelper.IncreaseDecreaseButtons(ppif, t: w.transform);
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
                                                    modifier.commands = commands;
                                                }
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(TriggerHelper.ScrollDelta(ppif, 15f, 3f));

                                        var ppifh = input.gameObject.AddComponent<InputFieldSwapper>();
                                        ppifh.inputField = ppif;

                                        TriggerHelper.IncreaseDecreaseButtons(ppif, t: w.transform);
                                    }
                                }

                                //Integer
                                if (cmd == "playerHit" || cmd == "playerHitAll" || cmd == "playerHeal" || cmd == "playerHealAll" || cmd == "addVariable" || cmd == "subVariable" || cmd == "setVariable" || cmd == "mouseButtonDown" || cmd == "mouseButton" || cmd == "mouseButtonUp" || cmd.Contains("playerHealth") || cmd.Contains("playerDeaths") || cmd.Contains("variable"))
                                {
                                    xRT.sizeDelta = new Vector2(350f, 160f);
                                    layout.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 40f);

                                    TooltipHelper.AddTooltip(x, commands[0], "");

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
                                                //num = Mathf.Clamp(num, 0, int.MaxValue);
                                                modifier.value = num.ToString();
                                            }
                                        });
                                    }

                                    TriggerHelper.IncreaseDecreaseButtons(xif, t: valueG.transform);
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
                                                    TooltipHelper.AddTooltip(x, commands[0], "Kills the nearest player.");
                                                    break;
                                                }
                                            case "playerKillAll":
                                                {
                                                    TooltipHelper.AddTooltip(x, commands[0], "Kills all players.");
                                                    break;
                                                }
                                            case "showMouse":
                                                {
                                                    TooltipHelper.AddTooltip(x, commands[0], "Shows the mouse in-game.");
                                                    break;
                                                }
                                            case "hideMouse":
                                                {
                                                    TooltipHelper.AddTooltip(x, commands[0], "Hides the mouse in-game. Does not change anything in edit mode.");
                                                    break;
                                                }
                                            case "playerCollide":
                                                {
                                                    TooltipHelper.AddTooltip(x, commands[0], "Activates modifiers when player collides with object.");
                                                    break;
                                                }
                                            case "playerMoving":
                                                {
                                                    TooltipHelper.AddTooltip(x, commands[0], "Activates modifiers when player moves.");
                                                    break;
                                                }
                                            case "playerBoosting":
                                                {
                                                    TooltipHelper.AddTooltip(x, commands[0], "Activates modifiers when player boosts.");
                                                    break;
                                                }
                                            case "playerAlive":
                                                {
                                                    TooltipHelper.AddTooltip(x, commands[0], "Activates modifiers when a player dies.");
                                                    break;
                                                }
                                            case "mouseOver":
                                                {
                                                    TooltipHelper.AddTooltip(x, commands[0], "Activates modifiers when the mouse is over the object.");
                                                    break;
                                                }
                                            case "playerBoost":
                                                {
                                                    TooltipHelper.AddTooltip(x, commands[0], "Forces nearest player to boost.");
                                                    break;
                                                }
                                            case "playerBoostAll":
                                                {
                                                    TooltipHelper.AddTooltip(x, commands[0], "Forces all players to boost.");
                                                    break;
                                                }
                                            case "playerDisableBoost":
                                                {
                                                    TooltipHelper.AddTooltip(x, commands[0], "Disabled the boost of all players.");
                                                    break;
                                                }
                                            case "disableObject":
                                                {
                                                    TooltipHelper.AddTooltip(x, commands[0], "Destroys this object. (Will not occur in edit mode, please save any changes before testing this.)");
                                                    break;
                                                }
                                            case "disableObjectTree":
                                                {
                                                    TooltipHelper.AddTooltip(x, commands[0], "Destroys all objects attached to this one. (Will not occur in edit mode, please save any changes before testing this. Includes children and parents)");
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
                                        modifier.value = _val.ToString();
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
                                            if (float.TryParse(_val, out float num))
                                            {
                                                modifier.value = num.ToString() + cmd == "playerMove" || cmd == "playerMoveAll" ?
                                                "." + layout.transform.Find("y/input").GetComponent<InputField>().text : "";
                                            }
                                        });
                                    }

                                    var xet = input.gameObject.AddComponent<EventTrigger>();
                                    xet.triggers.Clear();
                                    xet.triggers.Add(TriggerHelper.ScrollDelta(xif));

                                    var xifh = input.gameObject.AddComponent<InputFieldSwapper>();
                                    xifh.inputField = xif;

                                    TriggerHelper.IncreaseDecreaseButtons(xif, t: valueG.transform);

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
                                                modifier.value = layout.transform.Find("x/input").GetComponent<InputField>().text + "." + _val;
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(TriggerHelper.ScrollDelta(ppif));

                                        TriggerHelper.IncreaseDecreaseButtons(ppif, t: w.transform);
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
                                                modifier.commands[1] = _val;
                                            });
                                        }

                                        var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                        ppet.triggers.Clear();
                                        ppet.triggers.Add(TriggerHelper.ScrollDelta(ppif));

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
                                            modifier.commands[2] = _val.ToString();
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
                                            modifier.commands[1] = _val;
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
                                            modifier.commands[1] = _val;
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

                                    if (!cmd.Contains("Variable") && !cmd.ToLower().Contains("string"))
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
                                                    modifier.value = num.ToString();
                                                }
                                            });
                                        }

                                        var xet = input.gameObject.AddComponent<EventTrigger>();
                                        xet.triggers.Clear();
                                        xet.triggers.Add(TriggerHelper.ScrollDeltaInt(xif));

                                        var xifh = input.gameObject.AddComponent<InputFieldSwapper>();
                                        xifh.inputField = xif;

                                        TriggerHelper.IncreaseDecreaseButtonsInt(xif, t: valueG.transform);
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

                                    if (cmd == "reactivePos" || cmd == "reactiveSca")
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
                                            modifier.value = _val;
                                        });
                                    }

                                    var xet = input.gameObject.AddComponent<EventTrigger>();
                                    xet.triggers.Clear();
                                    xet.triggers.Add(TriggerHelper.ScrollDelta(xif));

                                    var xifh = input.gameObject.AddComponent<InputFieldSwapper>();
                                    xifh.inputField = xif;

                                    TriggerHelper.IncreaseDecreaseButtons(xif, t: valueG.transform);

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
                                                        modifier.commands[1] = num.ToString();
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
                                                        modifier.commands[2] = num.ToString();
                                                    }
                                                });
                                            }

                                            TriggerHelper.AddEventTrigger(sampleXInput.gameObject, new List<EventTrigger.Entry>
                                            {
                                                TriggerHelper.ScrollDeltaInt(sampleXIF, 1, 0, 256, true),
                                                TriggerHelper.ScrollDeltaVector2Int(sampleXIF, sampleYIF, 1, new List<int> { 0, 256 })
                                            });
                                            TriggerHelper.AddEventTrigger(sampleYInput.gameObject, new List<EventTrigger.Entry>
                                            {
                                                TriggerHelper.ScrollDeltaInt(sampleYIF, 1, 0, 256, true),
                                                TriggerHelper.ScrollDeltaVector2Int(sampleXIF, sampleYIF, 1, new List<int> { 0, 256 })
                                            });

                                            TriggerHelper.IncreaseDecreaseButtonsInt(sampleXIF, 1, 0, 256, sampleX.transform);
                                            TriggerHelper.IncreaseDecreaseButtonsInt(sampleYIF, 1, 0, 256, sampleY.transform);
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
                                                        modifier.commands[3] = num.ToString();
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
                                                        modifier.commands[4] = num.ToString();
                                                    }
                                                });
                                            }

                                            TriggerHelper.AddEventTrigger(multiplyXIF.gameObject, new List<EventTrigger.Entry>
                                            {
                                                TriggerHelper.ScrollDelta(multiplyXIF, multi: true),
                                                TriggerHelper.ScrollDeltaVector2(multiplyXIF, multiplyYIF, 0.1f, 10f)
                                            });
                                            TriggerHelper.AddEventTrigger(multiplyYIF.gameObject, new List<EventTrigger.Entry>
                                            {
                                                TriggerHelper.ScrollDelta(multiplyYIF, multi: true),
                                                TriggerHelper.ScrollDeltaVector2(multiplyXIF, multiplyYIF, 0.1f, 10f) });

                                            TriggerHelper.IncreaseDecreaseButtons(multiplyYIF, t: multiplyY.transform);
                                            TriggerHelper.IncreaseDecreaseButtons(multiplyXIF, t: multiplyX.transform);
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
                                                        modifier.commands[1] = num.ToString();
                                                    }
                                                });
                                            }

                                            var ppet = ppinput.gameObject.GetComponent<EventTrigger>();
                                            ppet.triggers.Clear();
                                            ppet.triggers.Add(TriggerHelper.ScrollDeltaInt(ppif, 1, 0, 256));

                                            TriggerHelper.IncreaseDecreaseButtonsInt(ppif, 1,0, 256, w.transform);
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
                                                if (int.TryParse(_val, out int num))
                                                {
                                                    modifier.commands[1] = Mathf.Clamp(num, 0, 3).ToString();
                                                }
                                            });
                                        }

                                        var xet = ppinput.gameObject.AddComponent<EventTrigger>();
                                        xet.triggers.Clear();
                                        xet.triggers.Add(TriggerHelper.ScrollDeltaInt(ppif, max: 3 ));

                                        var xifh = ppinput.gameObject.AddComponent<InputFieldSwapper>();
                                        xifh.inputField = ppif;

                                        TriggerHelper.IncreaseDecreaseButtonsInt(ppif, max: 3, t: ppvalueG.transform);
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
                                            modifier.value = _val;
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
                                                    modifier.commands[1] = num.ToString();
                                                }
                                            });
                                        }

                                        var xet = ppinput.gameObject.AddComponent<EventTrigger>();
                                        xet.triggers.Clear();
                                        xet.triggers.Add(TriggerHelper.ScrollDeltaInt(ppif, 1, 0, DataManager.inst.gameData.eventObjects.allEvents.Count - 1));

                                        var xifh = ppinput.gameObject.AddComponent<InputFieldSwapper>();
                                        xifh.inputField = ppif;

                                        TriggerHelper.IncreaseDecreaseButtonsInt(ppif, max: DataManager.inst.gameData.eventObjects.allEvents.Count - 1, t: ppvalueG.transform);
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
                                                    modifier.commands[2] = num.ToString();
                                                }
                                            });
                                        }

                                        var xet = ppinput.gameObject.AddComponent<EventTrigger>();
                                        xet.triggers.Clear();
                                        xet.triggers.Add(TriggerHelper.ScrollDeltaInt(ppif, 1, max: DataManager.inst.gameData.eventObjects.allEvents[int.Parse(commands[1])][0].eventValues.Length - 1));

                                        var xifh = ppinput.gameObject.AddComponent<InputFieldSwapper>();
                                        xifh.inputField = ppif;

                                        TriggerHelper.IncreaseDecreaseButtonsInt(ppif, max: DataManager.inst.gameData.eventObjects.allEvents[int.Parse(commands[1])][0].eventValues.Length - 1, t: ppvalueG.transform);
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
                                                modifier.value = num.ToString();
                                            }
                                        });

                                        TriggerHelper.IncreaseDecreaseButtons(xif, t: valueG.transform);
                                        TriggerHelper.AddEventTrigger(input.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(xif) });
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
                                            ppif.text = cmd.Contains("Other") ? commands[2] : commands[1];
                                            ppif.onValueChanged.AddListener(delegate (string _val)
                                            {
                                                if (int.TryParse(_val, out int num))
                                                {
                                                    int ind = cmd.Contains("Other") ? 2 : 1;
                                                    modifier.commands[ind] = num.ToString();
                                                }
                                            });
                                        }

                                        var xet = ppinput.gameObject.AddComponent<EventTrigger>();
                                        xet.triggers.Clear();
                                        xet.triggers.Add(TriggerHelper.ScrollDeltaInt(ppif, 1, 0, 17));

                                        var xifh = ppinput.gameObject.AddComponent<InputFieldSwapper>();
                                        xifh.inputField = ppif;

                                        TriggerHelper.IncreaseDecreaseButtonsInt(ppif, max: 17, t: ppvalueG.transform);
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

                                        TriggerHelper.IncreaseDecreaseButtons(xif, t: valueG.transform);
                                        TriggerHelper.AddEventTrigger(input.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(xif, 0.1f, 10f) });
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
                                        beatmapObject.modifiers.RemoveAt(tmpIndex);
                                        RTEditor.inst.StartCoroutine(ObjectEditor.RefreshObjectGUI(beatmapObject));
                                    });
                                }
                            }
                        }
                    }

                    scrollViewRT.sizeDelta = new Vector2(351f, count == 1 ? 174 : count == 2 ? 310f : 445f);
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
                        RefreshDefaultModifiersList(beatmapObject);
                    });
                }
            }

            yield break;
        }

        public static void SetObjectColors(Toggle[] toggles, List<string> commands, int i, BeatmapObject.Modifier modifier)
        {
            modifier.commands[2] = i.ToString();

            int num6 = 0;
            foreach (var toggle in toggles)
            {
                toggle.onValueChanged.RemoveAllListeners();
                int tmpIndex = num6;

                toggle.isOn = num6 == i;
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

                //if (!toggle.GetComponent<HoverUI>())
                //{
                //    var hoverUI = toggle.gameObject.AddComponent<HoverUI>();
                //    hoverUI.animatePos = false;
                //    hoverUI.animateSca = true;
                //    hoverUI.size = 1.1f;
                //}
                num6++;
            }
        }

        public static void OpenREPLEditor(BeatmapObject.Modifier modifier, string value)
        {
            RTEditor.inst.RefreshREPLEditor(value, delegate (string _val)
            {
                RTEditor.inst.StartCoroutine(ObjectEditor.RefreshObjectGUI(modifier.modifierObject));
            });

            RTEditor.inst.replEditor.onValueChanged.AddListener(delegate (string _val)
            {
                modifier.value = _val;
            });
        }

        public static void CreateDefaultModifiersList()
        {
            var dialog = Instantiate(EditorManager.inst.GetDialog("Quick Actions Popup").Dialog.gameObject);
            dialog.transform.SetParent(EditorManager.inst.GetDialog("Quick Actions Popup").Dialog.parent);
            dialog.transform.localScale = Vector3.one;
            dialog.transform.localPosition = Vector3.zero;
            dialog.name = "DefaultModifiersPopup";

            EditorHelper.AddEditorDialog("Default Modifiers Popup", dialog);

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
                if (ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
                    RefreshDefaultModifiersList(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>());
            });

            var close = dialog.transform.Find("Panel/x").GetComponent<Button>();
            close.onClick.ClearAll();
            close.onClick.AddListener(delegate ()
            {
                EditorManager.inst.HideDialog("Default Modifiers Popup");
            });
        }

        public static string searchTerm;
        public static void RefreshDefaultModifiersList(BeatmapObject beatmapObject)
        {
            if (ModCompatibility.sharedFunctions.ContainsKey("ObjectModifiersModifierList"))
                defaultModifiers = (List<string>)ModCompatibility.sharedFunctions["ObjectModifiersModifierList"];

            var dialog = EditorManager.inst.GetDialog("Default Modifiers Popup").Dialog.gameObject;

            var contentM = dialog.transform.Find("mask/content");
            LSFunctions.LSHelpers.DeleteChildren(contentM);

            for (int i = 0; i < defaultModifiers.Count; i++)
            {
                if (string.IsNullOrEmpty(searchTerm) || defaultModifiers[i].ToLower().Contains(searchTerm.ToLower()))
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
                        AddModifierToObject(beatmapObject, tmpIndex);
                        RTEditor.inst.StartCoroutine(ObjectEditor.RefreshObjectGUI(beatmapObject));
                        EditorManager.inst.HideDialog("Default Modifiers Popup");
                    });
                }
            }
        }

        public static void AddModifierToObject(BeatmapObject beatmapObject, int index)
        {
            if (!ModCompatibility.mods.ContainsKey("ObjectModifiers"))
                return;

            ModCompatibility.mods["ObjectModifiers"].StaticInvoke("AddModifierToObject", beatmapObject, index);
            Updater.UpdateProcessor(beatmapObject);
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
