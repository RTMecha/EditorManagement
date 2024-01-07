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
using RTFunctions.Functions.Components;
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

        public GameObject modifierCardPrefab;
        public GameObject modifierAddPrefab;

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

        public void CreateModifiersOnAwake()
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

            modifierCardPrefab = new GameObject("Modifier Prefab");
            var mcpRT = modifierCardPrefab.AddComponent<RectTransform>();
            mcpRT.sizeDelta = new Vector2(336f, 128f);

            var mcpImage = modifierCardPrefab.AddComponent<Image>();
            mcpImage.color = new Color(1f, 1f, 1f, 0.03f);

            var mcpVLG = modifierCardPrefab.AddComponent<VerticalLayoutGroup>();
            mcpVLG.childControlHeight = false;
            mcpVLG.childForceExpandHeight = false;

            var mcpCSF = modifierCardPrefab.AddComponent<ContentSizeFitter>();
            mcpCSF.verticalFit = ContentSizeFitter.FitMode.MinSize;

            var mcpLabel = new GameObject("Label");
            mcpLabel.transform.SetParent(mcpRT);
            mcpLabel.transform.localScale = Vector3.one;

            var mcpLabelRT = mcpLabel.AddComponent<RectTransform>();
            mcpLabelRT.anchorMax = new Vector2(0f, 1f);
            mcpLabelRT.anchorMin = new Vector2(0f, 1f);
            mcpLabelRT.pivot = new Vector2(0f, 1f);
            mcpLabelRT.sizeDelta = new Vector2(187f, 32f);

            var mcpLabelHLG = mcpLabel.AddComponent<HorizontalLayoutGroup>();
            mcpLabelHLG.childControlWidth = false;
            mcpLabelHLG.childForceExpandWidth = false;

            var mcpText = new GameObject("Text");
            mcpText.transform.SetParent(mcpLabelRT);
            mcpText.transform.localScale = Vector3.one;
            var mcpTextRT = mcpText.AddComponent<RectTransform>();
            mcpTextRT.anchoredPosition = new Vector2(10f, -5f);
            mcpTextRT.anchorMax = Vector2.one;
            mcpTextRT.anchorMin = Vector2.zero;
            mcpTextRT.pivot = new Vector2(0f, 1f);
            mcpTextRT.sizeDelta = new Vector2(312f, 32f);

            var mcpTextText = mcpText.AddComponent<Text>();
            mcpTextText.alignment = TextAnchor.MiddleLeft;
            mcpTextText.font = FontManager.inst.Inconsolata;
            mcpTextText.fontSize = 19;
            mcpTextText.color = new Color(0.9373f, 0.9216f, 0.9373f);

            var delete = new GameObject("Delete");
            delete.transform.SetParent(mcpLabelRT);
            delete.transform.localScale = Vector3.one;

            var deleteRT = delete.AddComponent<RectTransform>();
            var deleteImage = delete.AddComponent<Image>();
            var deleteButton = delete.AddComponent<Button>();
            deleteButton.colors = UIManager.SetColorBlock(deleteButton.colors,
                new Color(0.9569f, 0.2627f, 0.2118f),
                new Color(0.1647f, 0.1647f, 0.1647f),
                new Color(0.1294f, 0.1294f, 0.1294f),
                new Color(0.1647f, 0.1647f, 0.1647f),
                new Color(0.7843f, 0.7843f, 0.7843f, 0.502f), 0.1f);

            var deleteLayoutElement = delete.AddComponent<LayoutElement>();
            deleteLayoutElement.minWidth = 32f;

            deleteRT.sizeDelta = new Vector2(32f, 32f);

            var deleteX = new GameObject("Image");
            deleteX.transform.SetParent(deleteRT);
            deleteX.transform.localScale = Vector3.one;

            var deleteXRT = deleteX.AddComponent<RectTransform>();
            deleteXRT.anchoredPosition = Vector2.zero;
            deleteXRT.anchorMax = Vector2.one;
            deleteXRT.anchorMin = Vector2.zero;
            deleteXRT.pivot = new Vector2(0.5f, 0.5f);
            deleteXRT.sizeDelta = new Vector2(-8f, -8f);

            var deleteXImage = deleteX.AddComponent<Image>();
            deleteXImage.sprite = SpriteManager.LoadSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_close.png");

            var layout = new GameObject("Layout");
            layout.transform.SetParent(mcpRT);
            layout.transform.localScale = Vector3.one;

            var layoutRT = layout.AddComponent<RectTransform>();

            var layoutVLG = layout.AddComponent<VerticalLayoutGroup>();
            layoutVLG.childControlHeight = false;
            layoutVLG.childForceExpandHeight = false;
            layoutVLG.spacing = 4f;

            var layoutCSF = layout.AddComponent<ContentSizeFitter>();
            layoutCSF.verticalFit = ContentSizeFitter.FitMode.MinSize;

            modifierAddPrefab = EditorManager.inst.folderButtonPrefab.Duplicate(null, "add modifier");

            var text = modifierAddPrefab.transform.GetChild(0).GetComponent<Text>();
            text.text = "+";
            text.alignment = TextAnchor.MiddleCenter;

            booleanBar = Boolean();

            numberInput = NumberInput();

            stringInput = StringInput();

            dropdownBar = Dropdown();

            //var button = modifierAddPrefab.GetComponent<Button>();
            //button.onClick.RemoveAllListeners();
            //button.onClick.AddListener(delegate ()
            //{
            //    EditorManager.inst.ShowDialog("Default Modifiers Popup");
            //    if (ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
            //        RefreshDefaultModifiersList(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>());
            //});

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

        public IEnumerator RenderModifiers(BeatmapObject beatmapObject)
        {
            if (EditorManager.inst.isEditing)
            {
                var label = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content").transform.GetChild(2).gameObject;
                var singleInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move/position/x");
                var vector2Input = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move/position");
                var boolInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/toggle/toggle");
                //var dropdownInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/autokill/tod-dropdown");
                var sliderFullInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/bpm");
                //var stringInput = GameObject.Find("TimelineBar/GameObject/Time Input");

                Text textFont = GameObject.Find("TitleBar/File/Text").GetComponent<Text>();
                var close = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("Panel/x");

                LSHelpers.DeleteChildren(content);

                ((RectTransform)content.parent.parent).sizeDelta = new Vector2(351f, 300f * Mathf.Clamp(beatmapObject.modifiers.Count, 1, 5));

                int num = 0;
                foreach (var modifier in beatmapObject.modifiers)
                {
                    int index = num;
                    var gameObject = modifierCardPrefab.Duplicate(content, modifier.commands[0]);
                    gameObject.transform.Find("Label/Text").GetComponent<Text>().text = modifier.commands[0];

                    var delete = gameObject.transform.Find("Label/Delete").GetComponent<Button>();
                    delete.onClick.ClearAll();
                    delete.onClick.AddListener(delegate ()
                    {
                        beatmapObject.modifiers.RemoveAt(index);
                        Updater.UpdateProcessor(beatmapObject);
                        StartCoroutine(RenderModifiers(beatmapObject));
                    });

                    var layout = gameObject.transform.Find("Layout");

                    var constant = booleanBar.Duplicate(layout, "Constant");

                    constant.transform.Find("Text").GetComponent<Text>().text = "Constant";

                    var toggle = constant.transform.Find("Toggle").GetComponent<Toggle>();
                    toggle.onValueChanged.ClearAll();
                    toggle.isOn = modifier.constant;
                    toggle.onValueChanged.AddListener(delegate (bool _val)
                    {
                        modifier.constant = _val;
                        modifier.active = false;
                    });

                    if (modifier.type == BeatmapObject.Modifier.Type.Trigger)
                    {
                        var not = booleanBar.Duplicate(layout, "Not");
                        not.transform.Find("Text").GetComponent<Text>().text = "Not";

                        var notToggle = not.transform.Find("Toggle").GetComponent<Toggle>();
                        notToggle.onValueChanged.ClearAll();
                        notToggle.isOn = modifier.not;
                        notToggle.onValueChanged.AddListener(delegate (bool _val)
                        {
                            modifier.not = _val;
                            modifier.active = false;
                        });
                    }

                    var cmd = modifier.commands[0];
                    switch (cmd)
                    {
                        case "setPitch":
                        case "addPitch":
                        case "setMusicTime":
                        case "pitchEquals":
                        case "pitchLesserEquals":
                        case "pitchGreaterEquals":
                        case "pitchLesser":
                        case "pitchGreater":
                        case "playerDistanceLesser":
                        case "playerDistanceGreater":
                        case "setAlpha":
                        case "setAlphaOther":
                        case "blackHole":
                        case "musicTimeGreater":
                        case "musicTimeLesser":
                            {
                                var single = numberInput.Duplicate(layout, "Value");
                                single.transform.Find("Text").GetComponent<Text>().text = "Value";

                                var inputField = single.transform.Find("Input").GetComponent<InputField>();
                                inputField.onValueChanged.ClearAll();
                                inputField.textComponent.alignment = TextAnchor.MiddleCenter;
                                inputField.text = modifier.value;
                                inputField.onValueChanged.AddListener(delegate (string _val)
                                {
                                    modifier.value = _val;
                                    modifier.active = false;
                                });

                                TriggerHelper.IncreaseDecreaseButtons(inputField, t: single.transform);
                                TriggerHelper.AddEventTrigger(single, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(inputField) });

                                if (cmd == "setAlphaOther")
                                {
                                    var path = stringInput.Duplicate(layout, "Objects");
                                    path.transform.Find("Text").GetComponent<Text>().text = "Objects Name";

                                    var pathInputField = path.transform.Find("Input").GetComponent<InputField>();
                                    pathInputField.onValueChanged.ClearAll();
                                    pathInputField.textComponent.alignment = TextAnchor.MiddleLeft;
                                    pathInputField.text = modifier.commands[1];
                                    pathInputField.onValueChanged.AddListener(delegate (string _val)
                                    {
                                        modifier.commands[1] = _val;
                                        modifier.active = false;
                                    });

                                    var clickable = path.transform.Find("Input").gameObject.AddComponent<Clickable>();
                                    clickable.onDown = delegate (PointerEventData pointerEventData)
                                    {
                                        if (pointerEventData.button == PointerEventData.InputButton.Right)
                                        {
                                            EditorManager.inst.ShowDialog("Object Search Popup");
                                            RTEditor.inst.RefreshObjectSearch(delegate (BeatmapObject x)
                                            {
                                                pathInputField.text = x.name;
                                                EditorManager.inst.HideDialog("Object Search Popup");
                                            });
                                        }
                                    };
                                }

                                break;
                            }
                        case "playSoundOnline":
                        case "playSound":
                            {
                                var path = stringInput.Duplicate(layout, "Path");
                                path.transform.Find("Text").GetComponent<Text>().text = "Path";

                                var pathInputField = path.transform.Find("Input").GetComponent<InputField>();
                                pathInputField.onValueChanged.ClearAll();
                                pathInputField.textComponent.alignment = TextAnchor.MiddleLeft;
                                pathInputField.text = modifier.value;
                                pathInputField.onValueChanged.AddListener(delegate (string _val)
                                {
                                    modifier.value = _val;
                                    modifier.active = false;
                                });

                                var global = booleanBar.Duplicate(layout, "Global");
                                global.transform.Find("Text").GetComponent<Text>().text = "Global";

                                var globalToggle = global.transform.Find("Toggle").GetComponent<Toggle>();
                                globalToggle.onValueChanged.ClearAll();
                                globalToggle.isOn = Parser.TryParse(modifier.commands[1], false);
                                globalToggle.onValueChanged.AddListener(delegate (bool _val)
                                {
                                    modifier.commands[1] = _val.ToString();
                                    modifier.active = false;
                                });

                                {
                                    var single = numberInput.Duplicate(layout, "Pitch");
                                    single.transform.Find("Text").GetComponent<Text>().text = "Pitch";

                                    var inputField = single.transform.Find("Input").GetComponent<InputField>();
                                    inputField.onValueChanged.ClearAll();
                                    inputField.textComponent.alignment = TextAnchor.MiddleCenter;
                                    inputField.text = Parser.TryParse(modifier.commands[2], 1f).ToString();
                                    inputField.onValueChanged.AddListener(delegate (string _val)
                                    {
                                        if (float.TryParse(_val, out float result))
                                        {
                                            modifier.commands[2] = result.ToString();
                                            modifier.active = false;
                                        }
                                    });

                                    TriggerHelper.IncreaseDecreaseButtons(inputField, t: single.transform);
                                    TriggerHelper.AddEventTrigger(single, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(inputField) });
                                }
                                
                                {
                                    var single = numberInput.Duplicate(layout, "Volume");
                                    single.transform.Find("Text").GetComponent<Text>().text = "Volume";

                                    var inputField = single.transform.Find("Input").GetComponent<InputField>();
                                    inputField.onValueChanged.ClearAll();
                                    inputField.textComponent.alignment = TextAnchor.MiddleCenter;
                                    inputField.text = Parser.TryParse(modifier.commands[3], 1f).ToString();
                                    inputField.onValueChanged.AddListener(delegate (string _val)
                                    {
                                        if (float.TryParse(_val, out float result))
                                        {
                                            result = Mathf.Clamp(result, 0f, 2f);
                                            modifier.commands[3] = result.ToString();
                                            modifier.active = false;
                                        }
                                    });

                                    TriggerHelper.IncreaseDecreaseButtons(inputField, t: single.transform, max: 2f);
                                    TriggerHelper.AddEventTrigger(single, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(inputField, max: 2f) });
                                }

                                var loop = booleanBar.Duplicate(layout, "Loop");
                                loop.transform.Find("Text").GetComponent<Text>().text = "Loop";

                                var loopToggle = loop.transform.Find("Toggle").GetComponent<Toggle>();
                                loopToggle.onValueChanged.ClearAll();
                                loopToggle.isOn = Parser.TryParse(modifier.commands[4], false);
                                loopToggle.onValueChanged.AddListener(delegate (bool _val)
                                {
                                    modifier.commands[4] = _val.ToString();
                                    modifier.active = false;
                                });

                                break;
                            }
                        case "updateObject":
                        case "copyColor":
                        case "loadLevel":
                        case "code":
                        case "setText":
                        case "setTextOther":
                            {
                                var path = stringInput.Duplicate(layout, "Path");
                                path.transform.Find("Text").GetComponent<Text>().text = cmd == "updateObject" || cmd == "copyColor" || cmd == "setTextOther" ? "Objects Name" : cmd == "loadLevel" ? "Path" : "Right Click to Edit";

                                var pathInputField = path.transform.Find("Input").GetComponent<InputField>();
                                pathInputField.onValueChanged.ClearAll();
                                pathInputField.textComponent.alignment = TextAnchor.MiddleLeft;
                                pathInputField.text = modifier.value;
                                pathInputField.onValueChanged.AddListener(delegate (string _val)
                                {
                                    modifier.value = _val;
                                    modifier.active = false;
                                });

                                if (cmd == "updateObject" || cmd == "copyColor" || cmd == "code" || cmd == "setTextOther")
                                {
                                    var clickable = path.transform.Find("Input").gameObject.AddComponent<Clickable>();
                                    clickable.onDown = delegate (PointerEventData pointerEventData)
                                    {
                                        if (pointerEventData.button == PointerEventData.InputButton.Right && cmd != "code")
                                        {
                                            EditorManager.inst.ShowDialog("Object Search Popup");
                                            RTEditor.inst.RefreshObjectSearch(delegate (BeatmapObject x)
                                            {
                                                pathInputField.text = x.name;
                                                EditorManager.inst.HideDialog("Object Search Popup");
                                            });
                                        }
                                        else if (pointerEventData.button == PointerEventData.InputButton.Right)
                                        {
                                            OpenREPLEditor(modifier, modifier.value);
                                        }
                                    };
                                }

                                break;
                            }
                        case "blur":
                            {
                                var single = numberInput.Duplicate(layout, "Amount");
                                single.transform.Find("Text").GetComponent<Text>().text = "Amount";

                                var inputField = single.transform.Find("Input").GetComponent<InputField>();
                                inputField.onValueChanged.ClearAll();
                                inputField.textComponent.alignment = TextAnchor.MiddleCenter;
                                inputField.text = modifier.value;
                                inputField.onValueChanged.AddListener(delegate (string _val)
                                {
                                    if (float.TryParse(_val, out float n))
                                        modifier.value = n.ToString();
                                    modifier.active = false;
                                });

                                TriggerHelper.IncreaseDecreaseButtons(inputField, t: single.transform);
                                TriggerHelper.AddEventTrigger(single, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(inputField) });

                                var useOpacity = booleanBar.Duplicate(layout, "Use Opacity");
                                useOpacity.transform.Find("Text").GetComponent<Text>().text = "Use Opacity";

                                var useOpacityToggle = useOpacity.transform.Find("Toggle").GetComponent<Toggle>();
                                useOpacityToggle.onValueChanged.ClearAll();
                                useOpacityToggle.isOn = Parser.TryParse(modifier.commands[1], false);
                                useOpacityToggle.onValueChanged.AddListener(delegate (bool _val)
                                {
                                    modifier.commands[1] = _val.ToString();
                                    modifier.active = false;
                                });

                                break;
                            }
                        case "particleSystem":
                            {
                                var lifeTime = numberInput.Duplicate(layout, "LifeTime");
                                lifeTime.transform.Find("Text").GetComponent<Text>().text = "LifeTime";

                                var lifeTimeIF = lifeTime.transform.Find("Input").GetComponent<InputField>();
                                lifeTimeIF.onValueChanged.ClearAll();
                                lifeTimeIF.textComponent.alignment = TextAnchor.MiddleCenter;
                                lifeTimeIF.text = modifier.value;
                                lifeTimeIF.onValueChanged.AddListener(delegate (string _val)
                                {
                                    modifier.value = _val;
                                    modifier.active = false;
                                });

                                TriggerHelper.IncreaseDecreaseButtons(lifeTimeIF, t: lifeTime.transform);
                                TriggerHelper.AddEventTrigger(lifeTime, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(lifeTimeIF) });

                                break;
                            }
                        case "trailRenderer":
                            {
                                var time = numberInput.Duplicate(layout, "Time");
                                time.transform.Find("Text").GetComponent<Text>().text = "Time";

                                var timeIF = time.transform.Find("Input").GetComponent<InputField>();
                                timeIF.onValueChanged.ClearAll();
                                timeIF.textComponent.alignment = TextAnchor.MiddleCenter;
                                timeIF.text = modifier.value;
                                timeIF.onValueChanged.AddListener(delegate (string _val)
                                {
                                    modifier.value = _val;
                                    modifier.active = false;
                                });

                                var startWidth = numberInput.Duplicate(layout, "StartWidth");
                                startWidth.transform.Find("Text").GetComponent<Text>().text = "StartWidth";

                                var startWidthIF = startWidth.transform.Find("Input").GetComponent<InputField>();
                                startWidthIF.onValueChanged.ClearAll();
                                startWidthIF.textComponent.alignment = TextAnchor.MiddleCenter;
                                startWidthIF.text = modifier.commands[1];
                                startWidthIF.onValueChanged.AddListener(delegate (string _val)
                                {
                                    modifier.commands[1] = _val;
                                    modifier.active = false;
                                });
                                
                                var endWidth = numberInput.Duplicate(layout, "EndWidth");
                                endWidth.transform.Find("Text").GetComponent<Text>().text = "EndWidth";

                                var endWidthIF = endWidth.transform.Find("Input").GetComponent<InputField>();
                                endWidthIF.onValueChanged.ClearAll();
                                endWidthIF.textComponent.alignment = TextAnchor.MiddleCenter;
                                endWidthIF.text = modifier.commands[2];
                                endWidthIF.onValueChanged.AddListener(delegate (string _val)
                                {
                                    modifier.commands[2] = _val;
                                    modifier.active = false;
                                });

                                //var startColor = numberInput.Duplicate(layout, "StartColor");
                                //startColor.transform.Find("Text").GetComponent<Text>().text = "StartColor";

                                //var startColorIF = startColor.transform.Find("Input").GetComponent<InputField>();
                                //startColorIF.onValueChanged.ClearAll();
                                //startColorIF.textComponent.alignment = TextAnchor.MiddleCenter;
                                //startColorIF.text = Mathf.Clamp(Parser.TryParse(modifier.commands[3], 0), 0, RTHelpers.BeatmapTheme.objectColors.Count - 1).ToString();
                                //startColorIF.onValueChanged.AddListener(delegate (string _val)
                                //{
                                //    if (int.TryParse(_val, out int result))
                                //    {
                                //        result = Mathf.Clamp(result, 0, RTHelpers.BeatmapTheme.objectColors.Count - 1);
                                //        modifier.commands[3] = result.ToString();
                                //        modifier.active = false;
                                //    }
                                //});


                                var startColorBase = numberInput.Duplicate(layout, "StartColor");

                                startColorBase.transform.Find("Text").GetComponent<Text>().text = "StartColor";

                                Destroy(startColorBase.transform.Find("Input").gameObject);
                                Destroy(startColorBase.transform.Find(">").gameObject);
                                Destroy(startColorBase.transform.Find("<").gameObject);

                                var startColors = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/color/color"));
                                startColors.transform.SetParent(startColorBase.transform);
                                startColors.transform.localScale = Vector3.one;
                                startColors.name = "color";

                                if (startColors.TryGetComponent(out GridLayoutGroup scglg))
                                {
                                    scglg.cellSize = new Vector2(16f, 16f);
                                    scglg.spacing = new Vector2(4.66f, 2.5f);
                                }
                                if (startColors.TryGetComponent(out RectTransform scrt))
                                {
                                    scrt.sizeDelta = new Vector2(183f, 32f);
                                }

                                SetObjectColors(startColors.GetComponentsInChildren<Toggle>(), 3, Parser.TryParse(modifier.commands[3], 0), modifier);

                                //var endColor = numberInput.Duplicate(layout, "EndColor");
                                //endColor.transform.Find("Text").GetComponent<Text>().text = "EndColor";

                                //var endColorIF = endColor.transform.Find("Input").GetComponent<InputField>();
                                //endColorIF.onValueChanged.ClearAll();
                                //endColorIF.textComponent.alignment = TextAnchor.MiddleCenter;
                                //endColorIF.text = Mathf.Clamp(Parser.TryParse(modifier.commands[5], 0), 0, RTHelpers.BeatmapTheme.objectColors.Count - 1).ToString();
                                //endColorIF.onValueChanged.AddListener(delegate (string _val)
                                //{
                                //    if (int.TryParse(_val, out int result))
                                //    {
                                //        result = Mathf.Clamp(result, 0, RTHelpers.BeatmapTheme.objectColors.Count - 1);
                                //        modifier.commands[5] = result.ToString();
                                //        modifier.active = false;
                                //    }
                                //});


                                var endColorBase = numberInput.Duplicate(layout, "EndColor");

                                endColorBase.transform.Find("Text").GetComponent<Text>().text = "EndColor";

                                Destroy(endColorBase.transform.Find("Input").gameObject);
                                Destroy(endColorBase.transform.Find(">").gameObject);
                                Destroy(endColorBase.transform.Find("<").gameObject);

                                var endColors = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/color/color"));
                                endColors.transform.SetParent(endColorBase.transform);
                                endColors.transform.localScale = Vector3.one;
                                endColors.name = "color";

                                if (endColors.TryGetComponent(out GridLayoutGroup ecglg))
                                {
                                    ecglg.cellSize = new Vector2(16f, 16f);
                                    ecglg.spacing = new Vector2(4.66f, 2.5f);
                                }
                                if (endColors.TryGetComponent(out RectTransform ecrt))
                                {
                                    ecrt.sizeDelta = new Vector2(183f, 32f);
                                }

                                SetObjectColors(endColors.GetComponentsInChildren<Toggle>(), 5, Parser.TryParse(modifier.commands[5], 0), modifier);

                                var startOpacity = numberInput.Duplicate(layout, "StartOpacity");
                                startOpacity.transform.Find("Text").GetComponent<Text>().text = "StartOpacity";

                                var startOpacityIF = startOpacity.transform.Find("Input").GetComponent<InputField>();
                                startOpacityIF.onValueChanged.ClearAll();
                                startOpacityIF.textComponent.alignment = TextAnchor.MiddleCenter;
                                startOpacityIF.text = modifier.commands[4];
                                startOpacityIF.onValueChanged.AddListener(delegate (string _val)
                                {
                                    modifier.commands[4] = _val;
                                    modifier.active = false;
                                });
                                
                                var endOpacity = numberInput.Duplicate(layout, "EndOpacity");
                                endOpacity.transform.Find("Text").GetComponent<Text>().text = "EndOpacity";

                                var endOpacityIF = endOpacity.transform.Find("Input").GetComponent<InputField>();
                                endOpacityIF.onValueChanged.ClearAll();
                                endOpacityIF.textComponent.alignment = TextAnchor.MiddleCenter;
                                endOpacityIF.text = modifier.commands[6];
                                endOpacityIF.onValueChanged.AddListener(delegate (string _val)
                                {
                                    modifier.commands[6] = _val;
                                    modifier.active = false;
                                });

                                break;
                            }
                        case "playerHit":
                        case "playerHitAll":
                        case "playerHeal":
                        case "playerHealAll":
                        case "addVariable":
                        case "subVariable":
                        case "setVariable":
                        case "mouseButtonDown":
                        case "mouseButton":
                        case "mouseButtonUp":
                        case "playerHealthEquals":
                        case "playerHealthLesserEquals":
                        case "playerHealthGreaterEquals":
                        case "playerHealthLesser":
                        case "playerHealthGreater":
                        case "playerDeathsEquals":
                        case "playerDeathsLesserEquals":
                        case "playerDeathsGreaterEquals":
                        case "playerDeathsLesser":
                        case "playerDeathsGreater":
                        case "variableEquals":
                        case "variableLesserEquals":
                        case "variableGreaterEquals":
                        case "variableLesser":
                        case "variableGreater":
                        case "variableOtherEquals":
                        case "variableOtherLesserEquals":
                        case "variableOtherGreaterEquals":
                        case "variableOtherLesser":
                        case "variableOtherGreater":
                            {
                                var single = numberInput.Duplicate(layout, "Value");
                                single.transform.Find("Text").GetComponent<Text>().text = "Value";

                                var inputField = single.transform.Find("Input").GetComponent<InputField>();
                                inputField.onValueChanged.ClearAll();
                                inputField.textComponent.alignment = TextAnchor.MiddleCenter;
                                inputField.text = Parser.TryParse(modifier.value, 0).ToString();
                                inputField.onValueChanged.AddListener(delegate (string _val)
                                {
                                    if (int.TryParse(_val, out int result))
                                    {
                                        modifier.value = result.ToString();
                                        modifier.active = false;
                                    }
                                });

                                TriggerHelper.IncreaseDecreaseButtonsInt(inputField, t: single.transform);
                                TriggerHelper.AddEventTrigger(single, new List<EventTrigger.Entry> { TriggerHelper.ScrollDeltaInt(inputField) });

                                if (cmd == "addVariable" || cmd == "subVariable" || cmd == "setVariable" || cmd.Contains("variableOther") || cmd == "setAlphaOther")
                                {
                                    var path = stringInput.Duplicate(layout, "Objects");
                                    path.transform.Find("Text").GetComponent<Text>().text = "Objects Name";

                                    var pathInputField = path.transform.Find("Input").GetComponent<InputField>();
                                    pathInputField.onValueChanged.ClearAll();
                                    pathInputField.textComponent.alignment = TextAnchor.MiddleLeft;
                                    pathInputField.text = modifier.commands[1];
                                    pathInputField.onValueChanged.AddListener(delegate (string _val)
                                    {
                                        modifier.commands[1] = _val;
                                        modifier.active = false;
                                    });

                                    var clickable = path.transform.Find("Input").gameObject.AddComponent<Clickable>();
                                    clickable.onDown = delegate (PointerEventData pointerEventData)
                                    {
                                        if (pointerEventData.button == PointerEventData.InputButton.Right)
                                        {
                                            EditorManager.inst.ShowDialog("Object Search Popup");
                                            RTEditor.inst.RefreshObjectSearch(delegate (BeatmapObject x)
                                            {
                                                pathInputField.text = x.name;
                                                EditorManager.inst.HideDialog("Object Search Popup");
                                            });
                                        }
                                    };
                                }

                                break;
                            }
                        case "keyPressDown":
                        case "keyPress":
                        case "keyPressUp":
                            {
                                var dd = dropdownBar.Duplicate(layout, "Key");
                                dd.transform.Find("Text").GetComponent<Text>().text = "Value";

                                Destroy(dd.transform.Find("Dropdown").GetComponent<HoverTooltip>());

                                var hide = dd.transform.Find("Dropdown").GetComponent<HideDropdownOptions>();
                                hide.DisabledOptions.Clear();
                                var d = dd.transform.Find("Dropdown").GetComponent<Dropdown>();
                                d.onValueChanged.RemoveAllListeners();
                                d.options.Clear();

                                var keyCodes = Enum.GetValues(typeof(KeyCode));

                                for (int i = 0; i < keyCodes.Length; i++)
                                {
                                    var str = Enum.GetName(typeof(KeyCode), i) ?? "Invalid Value";

                                    hide.DisabledOptions.Add(string.IsNullOrEmpty(Enum.GetName(typeof(KeyCode), i)));

                                    d.options.Add(new Dropdown.OptionData(str));
                                }

                                d.value = Parser.TryParse(modifier.value, 0);

                                d.onValueChanged.AddListener(delegate (int _val)
                                {
                                    modifier.value = _val.ToString();
                                });

                                break;
                            }
                        case "loadEquals":
                        case "loadLesserEquals":
                        case "loadGreaterEquals":
                        case "loadLesser":
                        case "loadGreater":
                        case "save":
                        case "saveVariable":
                            {
                                var path = stringInput.Duplicate(layout, "Path");
                                path.transform.Find("Text").GetComponent<Text>().text = "Path";

                                var pathInputField = path.transform.Find("Input").GetComponent<InputField>();
                                pathInputField.onValueChanged.ClearAll();
                                pathInputField.textComponent.alignment = TextAnchor.MiddleLeft;
                                pathInputField.text = modifier.commands[1];
                                pathInputField.onValueChanged.AddListener(delegate (string _val)
                                {
                                    modifier.commands[1] = _val;
                                    modifier.active = false;
                                });
                                
                                var json1 = stringInput.Duplicate(layout, "JN1");
                                json1.transform.Find("Text").GetComponent<Text>().text = "JSON 1";

                                var json1Field = json1.transform.Find("Input").GetComponent<InputField>();
                                json1Field.onValueChanged.ClearAll();
                                json1Field.textComponent.alignment = TextAnchor.MiddleLeft;
                                json1Field.text = modifier.commands[2];
                                json1Field.onValueChanged.AddListener(delegate (string _val)
                                {
                                    modifier.commands[2] = _val;
                                    modifier.active = false;
                                });
                                
                                var json2 = stringInput.Duplicate(layout, "JN2");
                                json2.transform.Find("Text").GetComponent<Text>().text = "JSON 2";

                                var json2Field = json2.transform.Find("Input").GetComponent<InputField>();
                                json2Field.onValueChanged.ClearAll();
                                json2Field.textComponent.alignment = TextAnchor.MiddleLeft;
                                json2Field.text = modifier.commands[3];
                                json2Field.onValueChanged.AddListener(delegate (string _val)
                                {
                                    modifier.commands[3] = _val;
                                    modifier.active = false;
                                });


                                if (cmd != "saveVariable")
                                {
                                    var single = numberInput.Duplicate(layout, "Value");
                                    single.transform.Find("Text").GetComponent<Text>().text = "Value";

                                    var inputField = single.transform.Find("Input").GetComponent<InputField>();
                                    inputField.onValueChanged.ClearAll();
                                    inputField.textComponent.alignment = TextAnchor.MiddleCenter;
                                    inputField.text = Parser.TryParse(modifier.value, 0f).ToString();
                                    inputField.onValueChanged.AddListener(delegate (string _val)
                                    {
                                        if (float.TryParse(_val, out float result))
                                        {
                                            modifier.value = result.ToString();
                                            modifier.active = false;
                                        }
                                    });

                                    TriggerHelper.IncreaseDecreaseButtons(inputField, t: single.transform);
                                    TriggerHelper.AddEventTrigger(single, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(inputField) });
                                }

                                break;
                            }
                        case "reactivePos":
                        case "reactiveSca":
                        case "reactiveRot":
                        case "reactiveCol":
                        case "reactiveColLerp":
                            {
                                var single = numberInput.Duplicate(layout, "Value");
                                single.transform.Find("Text").GetComponent<Text>().text = "Total Multiply";

                                var inputField = single.transform.Find("Input").GetComponent<InputField>();
                                inputField.onValueChanged.ClearAll();
                                inputField.textComponent.alignment = TextAnchor.MiddleCenter;
                                inputField.text = Parser.TryParse(modifier.value, 0f).ToString();
                                inputField.onValueChanged.AddListener(delegate (string _val)
                                {
                                    if (float.TryParse(_val, out float result))
                                    {
                                        modifier.value = result.ToString();
                                        modifier.active = false;
                                    }
                                });

                                TriggerHelper.IncreaseDecreaseButtons(inputField, t: single.transform);
                                TriggerHelper.AddEventTrigger(single, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(inputField) });

                                if (cmd == "reactivePos" || cmd == "reactiveSca")
                                {
                                    var samplesX = numberInput.Duplicate(layout, "Value");
                                    samplesX.transform.Find("Text").GetComponent<Text>().text = "Sample X";

                                    var samplesXIF = samplesX.transform.Find("Input").GetComponent<InputField>();
                                    samplesXIF.onValueChanged.ClearAll();
                                    samplesXIF.textComponent.alignment = TextAnchor.MiddleCenter;
                                    samplesXIF.text = Parser.TryParse(modifier.commands[1], 0).ToString();
                                    samplesXIF.onValueChanged.AddListener(delegate (string _val)
                                    {
                                        if (int.TryParse(_val, out int result))
                                        {
                                            modifier.commands[1] = result.ToString();
                                            modifier.active = false;
                                        }
                                    });

                                    var samplesY = numberInput.Duplicate(layout, "Value");
                                    samplesY.transform.Find("Text").GetComponent<Text>().text = "Sample Y";

                                    var samplesYIF = samplesY.transform.Find("Input").GetComponent<InputField>();
                                    samplesYIF.onValueChanged.ClearAll();
                                    samplesYIF.textComponent.alignment = TextAnchor.MiddleCenter;
                                    samplesYIF.text = Parser.TryParse(modifier.commands[2], 0).ToString();
                                    samplesYIF.onValueChanged.AddListener(delegate (string _val)
                                    {
                                        if (int.TryParse(_val, out int result))
                                        {
                                            modifier.commands[2] = result.ToString();
                                            modifier.active = false;
                                        }
                                    });

                                    TriggerHelper.IncreaseDecreaseButtonsInt(samplesXIF, t: samplesX.transform);
                                    TriggerHelper.IncreaseDecreaseButtonsInt(samplesYIF, t: samplesY.transform);
                                    TriggerHelper.AddEventTriggerParams(samplesX,
                                        TriggerHelper.ScrollDeltaInt(samplesXIF, multi: true),
                                        TriggerHelper.ScrollDeltaVector2Int(samplesXIF, samplesYIF, 1, new List<int> { 0, 255 }));
                                    TriggerHelper.AddEventTriggerParams(samplesY,
                                        TriggerHelper.ScrollDeltaInt(samplesYIF, multi: true),
                                        TriggerHelper.ScrollDeltaVector2Int(samplesXIF, samplesYIF, 1, new List<int> { 0, 255 }));

                                    var multiplyX = numberInput.Duplicate(layout, "Value");
                                    multiplyX.transform.Find("Text").GetComponent<Text>().text = "Multiply X";

                                    var multiplyXIF = multiplyX.transform.Find("Input").GetComponent<InputField>();
                                    multiplyXIF.onValueChanged.ClearAll();
                                    multiplyXIF.textComponent.alignment = TextAnchor.MiddleCenter;
                                    multiplyXIF.text = Parser.TryParse(modifier.commands[3], 0f).ToString();
                                    multiplyXIF.onValueChanged.AddListener(delegate (string _val)
                                    {
                                        if (float.TryParse(_val, out float result))
                                        {
                                            modifier.commands[3] = result.ToString();
                                            modifier.active = false;
                                        }
                                    });

                                    var multiplyY = numberInput.Duplicate(layout, "Value");
                                    multiplyY.transform.Find("Text").GetComponent<Text>().text = "Multiply Y";

                                    var multiplyYIF = multiplyY.transform.Find("Input").GetComponent<InputField>();
                                    multiplyYIF.onValueChanged.ClearAll();
                                    multiplyYIF.textComponent.alignment = TextAnchor.MiddleCenter;
                                    multiplyYIF.text = Parser.TryParse(modifier.commands[4], 0f).ToString();
                                    multiplyYIF.onValueChanged.AddListener(delegate (string _val)
                                    {
                                        if (float.TryParse(_val, out float result))
                                        {
                                            modifier.commands[4] = result.ToString();
                                            modifier.active = false;
                                        }
                                    });

                                    TriggerHelper.IncreaseDecreaseButtons(multiplyXIF, t: multiplyX.transform);
                                    TriggerHelper.IncreaseDecreaseButtons(multiplyYIF, t: multiplyY.transform);
                                    TriggerHelper.AddEventTriggerParams(multiplyX,
                                        TriggerHelper.ScrollDelta(multiplyXIF, multi: true),
                                        TriggerHelper.ScrollDeltaVector2(multiplyXIF, multiplyYIF, 0.1f, 10f));
                                    TriggerHelper.AddEventTriggerParams(multiplyY,
                                        TriggerHelper.ScrollDelta(multiplyYIF, multi: true),
                                        TriggerHelper.ScrollDeltaVector2(multiplyXIF, multiplyYIF, 0.1f, 10f));
                                }
                                else
                                {
                                    var samplesX = numberInput.Duplicate(layout, "Value");
                                    samplesX.transform.Find("Text").GetComponent<Text>().text = "Sample";

                                    var samplesXIF = samplesX.transform.Find("Input").GetComponent<InputField>();
                                    samplesXIF.onValueChanged.ClearAll();
                                    samplesXIF.textComponent.alignment = TextAnchor.MiddleCenter;
                                    samplesXIF.text = Parser.TryParse(modifier.commands[1], 0).ToString();
                                    samplesXIF.onValueChanged.AddListener(delegate (string _val)
                                    {
                                        if (int.TryParse(_val, out int result))
                                        {
                                            modifier.commands[1] = result.ToString();
                                            modifier.active = false;
                                        }
                                    });

                                    TriggerHelper.IncreaseDecreaseButtonsInt(samplesXIF, t: samplesX.transform);
                                    TriggerHelper.AddEventTriggerParams(samplesX, TriggerHelper.ScrollDeltaInt(samplesXIF));

                                    if (cmd == "reactiveCol" || cmd == "reactiveColLerp")
                                    {
                                        var w = numberInput.Duplicate(layout, "Color");

                                        w.transform.Find("Text").GetComponent<Text>().text = "Color";

                                        Destroy(w.transform.Find("Input").gameObject);
                                        Destroy(w.transform.Find(">").gameObject);
                                        Destroy(w.transform.Find("<").gameObject);

                                        var color = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/color/color"));
                                        color.transform.SetParent(w.transform);
                                        color.transform.localScale = Vector3.one;
                                        color.name = "color";

                                        if (color.TryGetComponent(out GridLayoutGroup glg))
                                        {
                                            glg.cellSize = new Vector2(16f, 16f);
                                            glg.spacing = new Vector2(4.66f, 2.5f);
                                        }
                                        if (color.TryGetComponent(out RectTransform rt))
                                        {
                                            rt.sizeDelta = new Vector2(183f, 32f);
                                        }

                                        SetObjectColors(color.GetComponentsInChildren<Toggle>(), 2, Parser.TryParse(modifier.commands[2], 0), modifier);
                                    }
                                }

                                break;
                            }
                        case "setPlayerModel":
                            {
                                var single = numberInput.Duplicate(layout, "Value");
                                single.transform.Find("Text").GetComponent<Text>().text = "Index";

                                var inputField = single.transform.Find("Input").GetComponent<InputField>();
                                inputField.onValueChanged.ClearAll();
                                inputField.textComponent.alignment = TextAnchor.MiddleCenter;
                                inputField.text = Parser.TryParse(modifier.commands[1], 0).ToString();
                                inputField.onValueChanged.AddListener(delegate (string _val)
                                {
                                    if (int.TryParse(_val, out int result))
                                    {
                                        modifier.commands[1] = Mathf.Clamp(result, 0, 3).ToString();
                                        modifier.active = false;
                                    }
                                });

                                TriggerHelper.IncreaseDecreaseButtonsInt(inputField, 1, 0, 3, single.transform);
                                TriggerHelper.AddEventTrigger(single, new List<EventTrigger.Entry> { TriggerHelper.ScrollDeltaInt(inputField, 1, 0, 3) });

                                var path = stringInput.Duplicate(layout, "Model");
                                path.transform.Find("Text").GetComponent<Text>().text = "Model ID";

                                var pathInputField = path.transform.Find("Input").GetComponent<InputField>();
                                pathInputField.onValueChanged.ClearAll();
                                pathInputField.textComponent.alignment = TextAnchor.MiddleLeft;
                                pathInputField.text = modifier.value;
                                pathInputField.onValueChanged.AddListener(delegate (string _val)
                                {
                                    modifier.value = _val;
                                    modifier.active = false;
                                });

                                break;
                            }
                        case "eventOffset":
                            {
                                var type = numberInput.Duplicate(layout, "Value");
                                type.transform.Find("Text").GetComponent<Text>().text = "Type";

                                var typeIF = type.transform.Find("Input").GetComponent<InputField>();
                                typeIF.onValueChanged.ClearAll();
                                typeIF.textComponent.alignment = TextAnchor.MiddleCenter;
                                typeIF.text = Parser.TryParse(modifier.commands[1], 0).ToString();
                                typeIF.onValueChanged.AddListener(delegate (string _val)
                                {
                                    if (int.TryParse(_val, out int result))
                                    {
                                        modifier.commands[1] = Mathf.Clamp(result, 0, GameData.DefaultKeyframes.Count - 1).ToString();
                                        modifier.active = false;
                                    }
                                });

                                TriggerHelper.IncreaseDecreaseButtonsInt(typeIF, 1, 0, GameData.DefaultKeyframes.Count - 1, type.transform);
                                TriggerHelper.AddEventTrigger(type, new List<EventTrigger.Entry> { TriggerHelper.ScrollDeltaInt(typeIF, 1, 0, GameData.DefaultKeyframes.Count - 1) });

                                var vindex = numberInput.Duplicate(layout, "Value");
                                vindex.transform.Find("Text").GetComponent<Text>().text = "Val Index";

                                var vindexIF = vindex.transform.Find("Input").GetComponent<InputField>();
                                vindexIF.onValueChanged.ClearAll();
                                vindexIF.textComponent.alignment = TextAnchor.MiddleCenter;
                                vindexIF.text = Parser.TryParse(modifier.commands[2], 0).ToString();
                                vindexIF.onValueChanged.AddListener(delegate (string _val)
                                {
                                    if (int.TryParse(_val, out int result))
                                    {
                                        modifier.commands[2] = Mathf.Clamp(result, 0, GameData.DefaultKeyframes[Parser.TryParse(modifier.commands[1], 0)].eventValues.Length - 1).ToString();
                                        modifier.active = false;
                                    }
                                });

                                TriggerHelper.IncreaseDecreaseButtonsInt(vindexIF, 1, 0, GameData.DefaultKeyframes[Parser.TryParse(modifier.commands[1], 0)].eventValues.Length - 1, vindex.transform);
                                TriggerHelper.AddEventTriggerParams(vindex, TriggerHelper.ScrollDeltaInt(vindexIF, 1, 0, GameData.DefaultKeyframes[Parser.TryParse(modifier.commands[1], 0)].eventValues.Length - 1));

                                var single = numberInput.Duplicate(layout, "Value");
                                single.transform.Find("Text").GetComponent<Text>().text = "Value";

                                var inputField = single.transform.Find("Input").GetComponent<InputField>();
                                inputField.onValueChanged.ClearAll();
                                inputField.textComponent.alignment = TextAnchor.MiddleCenter;
                                inputField.text = modifier.value;
                                inputField.onValueChanged.AddListener(delegate (string _val)
                                {
                                    modifier.value = _val;
                                    modifier.active = false;
                                });

                                TriggerHelper.IncreaseDecreaseButtons(inputField, t: single.transform);
                                TriggerHelper.AddEventTrigger(single, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(inputField) });

                                break;
                            }
                        case "addColor":
                        case "addColorOther":
                        case "lerpColor":
                        case "lerpColorOther":
                            {
                                if (cmd == "addColorOther" || cmd == "lerpColorOther")
                                {
                                    var path = stringInput.Duplicate(layout, "Objects");
                                    path.transform.Find("Text").GetComponent<Text>().text = "Objects Name";

                                    var pathInputField = path.transform.Find("Input").GetComponent<InputField>();
                                    pathInputField.onValueChanged.ClearAll();
                                    pathInputField.textComponent.alignment = TextAnchor.MiddleLeft;
                                    pathInputField.text = modifier.commands[1];
                                    pathInputField.onValueChanged.AddListener(delegate (string _val)
                                    {
                                        modifier.commands[1] = _val;
                                        modifier.active = false;
                                    });

                                    var clickable = path.transform.Find("Input").gameObject.AddComponent<Clickable>();
                                    clickable.onDown = delegate (PointerEventData pointerEventData)
                                    {
                                        if (pointerEventData.button == PointerEventData.InputButton.Right)
                                        {
                                            EditorManager.inst.ShowDialog("Object Search Popup");
                                            RTEditor.inst.RefreshObjectSearch(delegate (BeatmapObject x)
                                            {
                                                pathInputField.text = x.name;
                                                EditorManager.inst.HideDialog("Object Search Popup");
                                            });
                                        }
                                    };
                                }

                                var w = numberInput.Duplicate(layout, "Color");

                                w.transform.Find("Text").GetComponent<Text>().text = "Color";

                                Destroy(w.transform.Find("Input").gameObject);
                                Destroy(w.transform.Find(">").gameObject);
                                Destroy(w.transform.Find("<").gameObject);

                                var color = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/color/color"));
                                color.transform.SetParent(w.transform);
                                color.transform.localScale = Vector3.one;
                                color.name = "color";

                                if (color.TryGetComponent(out GridLayoutGroup glg))
                                {
                                    glg.cellSize = new Vector2(16f, 16f);
                                    glg.spacing = new Vector2(4.66f, 2.5f);
                                }
                                if (color.TryGetComponent(out RectTransform rt))
                                {
                                    rt.sizeDelta = new Vector2(183f, 32f);
                                }

                                SetObjectColors(color.GetComponentsInChildren<Toggle>(), !cmd.Contains("Other") ? 1 : 2, Parser.TryParse(modifier.commands[!cmd.Contains("Other") ? 1 : 2], 0), modifier);

                                //var type = numberInput.Duplicate(layout, "Value");
                                //type.transform.Find("Text").GetComponent<Text>().text = "Index";

                                //var typeIF = type.transform.Find("Input").GetComponent<InputField>();
                                //typeIF.onValueChanged.ClearAll();
                                //typeIF.textComponent.alignment = TextAnchor.MiddleCenter;
                                //typeIF.text = Parser.TryParse(modifier.commands[cmd != "addColorOther" ? 1 : 2], 0).ToString();
                                //typeIF.onValueChanged.AddListener(delegate (string _val)
                                //{
                                //    if (int.TryParse(_val, out int result))
                                //    {
                                //        modifier.commands[cmd != "addColorOther" ? 1 : 2] = Mathf.Clamp(result, 0, GameData.DefaultKeyframes.Count - 1).ToString();
                                //        modifier.active = false;
                                //    }
                                //});

                                //TriggerHelper.IncreaseDecreaseButtonsInt(typeIF, t: type.transform);
                                //TriggerHelper.AddEventTrigger(type, new List<EventTrigger.Entry> { TriggerHelper.ScrollDeltaInt(typeIF) });

                                var single = numberInput.Duplicate(layout, "Value");
                                single.transform.Find("Text").GetComponent<Text>().text = "Multiply";

                                var inputField = single.transform.Find("Input").GetComponent<InputField>();
                                inputField.onValueChanged.ClearAll();
                                inputField.textComponent.alignment = TextAnchor.MiddleCenter;
                                inputField.text = modifier.value;
                                inputField.onValueChanged.AddListener(delegate (string _val)
                                {
                                    modifier.value = _val;
                                    modifier.active = false;
                                });

                                TriggerHelper.IncreaseDecreaseButtons(inputField, t: single.transform);
                                TriggerHelper.AddEventTrigger(single, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(inputField) });

                                break;
                            }
                        case "signalModifier":
                            {
                                var path = stringInput.Duplicate(layout, "Objects");
                                path.transform.Find("Text").GetComponent<Text>().text = "Objects Name";

                                var pathInputField = path.transform.Find("Input").GetComponent<InputField>();
                                pathInputField.onValueChanged.ClearAll();
                                pathInputField.textComponent.alignment = TextAnchor.MiddleLeft;
                                pathInputField.text = modifier.commands[1];
                                pathInputField.onValueChanged.AddListener(delegate (string _val)
                                {
                                    modifier.commands[1] = _val;
                                    modifier.active = false;
                                });

                                var clickable = path.transform.Find("Input").gameObject.AddComponent<Clickable>();
                                clickable.onDown = delegate (PointerEventData pointerEventData)
                                {
                                    if (pointerEventData.button == PointerEventData.InputButton.Right)
                                    {
                                        EditorManager.inst.ShowDialog("Object Search Popup");
                                        RTEditor.inst.RefreshObjectSearch(delegate (BeatmapObject x)
                                        {
                                            pathInputField.text = x.name;
                                            EditorManager.inst.HideDialog("Object Search Popup");
                                        });
                                    }
                                };

                                var single = numberInput.Duplicate(layout, "Value");
                                single.transform.Find("Text").GetComponent<Text>().text = "Delay";

                                var inputField = single.transform.Find("Input").GetComponent<InputField>();
                                inputField.onValueChanged.ClearAll();
                                inputField.textComponent.alignment = TextAnchor.MiddleCenter;
                                inputField.text = modifier.value;
                                inputField.onValueChanged.AddListener(delegate (string _val)
                                {
                                    if (float.TryParse(_val, out float delay))
                                    {
                                        modifier.value = Mathf.Clamp(delay, 0f, float.PositiveInfinity).ToString();
                                        modifier.active = false;
                                    }
                                });

                                TriggerHelper.IncreaseDecreaseButtons(inputField, max: float.PositiveInfinity, t: single.transform);
                                TriggerHelper.AddEventTrigger(single, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(inputField, max: float.PositiveInfinity) });

                                break;
                            }
                        case "randomGreater":
                        case "randomLesser":
                        case "randomEquals":
                            {
                                var type = numberInput.Duplicate(layout, "Value");
                                type.transform.Find("Text").GetComponent<Text>().text = "Minimum";

                                var typeIF = type.transform.Find("Input").GetComponent<InputField>();
                                typeIF.onValueChanged.ClearAll();
                                typeIF.textComponent.alignment = TextAnchor.MiddleCenter;
                                typeIF.text = Parser.TryParse(modifier.commands[1], 0).ToString();
                                typeIF.onValueChanged.AddListener(delegate (string _val)
                                {
                                    if (int.TryParse(_val, out int result))
                                    {
                                        modifier.commands[1] = result.ToString();
                                        modifier.active = false;
                                    }
                                });

                                TriggerHelper.IncreaseDecreaseButtonsInt(typeIF, t: type.transform);
                                TriggerHelper.AddEventTrigger(type, new List<EventTrigger.Entry> { TriggerHelper.ScrollDeltaInt(typeIF) });

                                var vindex = numberInput.Duplicate(layout, "Value");
                                vindex.transform.Find("Text").GetComponent<Text>().text = "Maximum";

                                var vindexIF = vindex.transform.Find("Input").GetComponent<InputField>();
                                vindexIF.onValueChanged.ClearAll();
                                vindexIF.textComponent.alignment = TextAnchor.MiddleCenter;
                                vindexIF.text = Parser.TryParse(modifier.commands[2], 0).ToString();
                                vindexIF.onValueChanged.AddListener(delegate (string _val)
                                {
                                    if (int.TryParse(_val, out int result))
                                    {
                                        modifier.commands[2] = result.ToString();
                                        modifier.active = false;
                                    }
                                });

                                TriggerHelper.IncreaseDecreaseButtonsInt(vindexIF, t: vindex.transform);
                                TriggerHelper.AddEventTriggerParams(vindex, TriggerHelper.ScrollDeltaInt(vindexIF));

                                var single = numberInput.Duplicate(layout, "Value");
                                single.transform.Find("Text").GetComponent<Text>().text = "Value";

                                var inputField = single.transform.Find("Input").GetComponent<InputField>();
                                inputField.onValueChanged.ClearAll();
                                inputField.textComponent.alignment = TextAnchor.MiddleCenter;
                                inputField.text = modifier.value;
                                inputField.onValueChanged.AddListener(delegate (string _val)
                                {
                                    if (int.TryParse(_val, out int result))
                                    {
                                        modifier.value = result.ToString();
                                        modifier.active = false;
                                    }
                                });

                                TriggerHelper.IncreaseDecreaseButtonsInt(inputField, t: single.transform);
                                TriggerHelper.AddEventTriggerParams(single, TriggerHelper.ScrollDeltaInt(inputField));

                                break;
                            }
                        case "editorNotify":
                            {
                                var path = stringInput.Duplicate(layout, "Path");
                                path.transform.Find("Text").GetComponent<Text>().text = "Text";

                                var pathInputField = path.transform.Find("Input").GetComponent<InputField>();
                                pathInputField.onValueChanged.ClearAll();
                                pathInputField.textComponent.alignment = TextAnchor.MiddleLeft;
                                pathInputField.text = modifier.value;
                                pathInputField.onValueChanged.AddListener(delegate (string _val)
                                {
                                    modifier.value = _val;
                                    modifier.active = false;
                                });

                                var single = numberInput.Duplicate(layout, "Value");
                                single.transform.Find("Text").GetComponent<Text>().text = "Value";

                                var inputField = single.transform.Find("Input").GetComponent<InputField>();
                                inputField.onValueChanged.ClearAll();
                                inputField.textComponent.alignment = TextAnchor.MiddleCenter;
                                inputField.text = Parser.TryParse(modifier.commands[1], 0.5f).ToString();
                                inputField.onValueChanged.AddListener(delegate (string _val)
                                {
                                    if (float.TryParse(_val, out float result))
                                    {
                                        modifier.commands[1] = result.ToString();
                                        modifier.active = false;
                                    }
                                });

                                TriggerHelper.IncreaseDecreaseButtons(inputField, t: single.transform);
                                TriggerHelper.AddEventTrigger(single, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(inputField) });

                                var dd = dropdownBar.Duplicate(layout, "Key");
                                dd.transform.Find("Text").GetComponent<Text>().text = "Value";

                                Destroy(dd.transform.Find("Dropdown").GetComponent<HoverTooltip>());
                                Destroy(dd.transform.Find("Dropdown").GetComponent<HideDropdownOptions>());

                                var d = dd.transform.Find("Dropdown").GetComponent<Dropdown>();
                                d.onValueChanged.RemoveAllListeners();
                                d.options.Clear();

                                d.options = new List<Dropdown.OptionData>
                                {
                                    new Dropdown.OptionData("Info"),
                                    new Dropdown.OptionData("Success"),
                                    new Dropdown.OptionData("Error"),
                                    new Dropdown.OptionData("Warning")
                                };

                                d.value = Parser.TryParse(modifier.commands[2], 0);

                                d.onValueChanged.AddListener(delegate (int _val)
                                {
                                    modifier.commands[2] = Mathf.Clamp(_val, 0, 3).ToString();
                                });

                                break;
                            }
                        case "playerMove":
                        case "playerMoveAll":
                        case "playerMoveX":
                        case "playerMoveXAll":
                        case "playerMoveY":
                        case "playerMoveYAll":
                        case "playerRotate":
                        case "playerRotateAll":
                            {
                                string[] vector = new string[2];

                                bool isBothAxis = cmd == "playerMove" || cmd == "playerMoveAll";
                                if (isBothAxis)
                                {
                                    vector = modifier.value.Split(new char[] { ',' });
                                }

                                var xPosition = numberInput.Duplicate(layout, "X");
                                xPosition.transform.Find("Text").GetComponent<Text>().text = cmd.Contains("X") || isBothAxis || cmd.Contains("Rotate") ? "X" : "Y";

                                var xPositionIF = xPosition.transform.Find("Input").GetComponent<InputField>();
                                xPositionIF.onValueChanged.ClearAll();
                                xPositionIF.textComponent.alignment = TextAnchor.MiddleCenter;
                                xPositionIF.text = Parser.TryParse(isBothAxis ? vector[0] : modifier.value, 0.5f).ToString();
                                xPositionIF.onValueChanged.AddListener(delegate (string _val)
                                {
                                    if (float.TryParse(_val, out float result))
                                    {
                                        modifier.value = isBothAxis ? $"{result},{layout.transform.Find("Y/Input").GetComponent<InputField>().text}" : result.ToString();
                                        modifier.active = false;
                                    }
                                });

                                if (isBothAxis)
                                {
                                    var yPosition = numberInput.Duplicate(layout, "Y");
                                    yPosition.transform.Find("Text").GetComponent<Text>().text = "Y";

                                    var yPositionIF = yPosition.transform.Find("Input").GetComponent<InputField>();
                                    yPositionIF.onValueChanged.ClearAll();
                                    yPositionIF.textComponent.alignment = TextAnchor.MiddleCenter;
                                    yPositionIF.text = Parser.TryParse(isBothAxis ? vector[0] : modifier.value, 0.5f).ToString();
                                    yPositionIF.onValueChanged.AddListener(delegate (string _val)
                                    {
                                        if (float.TryParse(_val, out float result))
                                        {
                                            modifier.value = $"{layout.transform.Find("X/Input").GetComponent<InputField>().text},{result}";
                                            modifier.active = false;
                                        }
                                    });

                                    TriggerHelper.IncreaseDecreaseButtons(yPositionIF, t: yPosition.transform);
                                    TriggerHelper.AddEventTriggerParams(yPosition,
                                        TriggerHelper.ScrollDelta(yPositionIF),
                                        TriggerHelper.ScrollDeltaVector2(xPositionIF, yPositionIF, 0.1f, 10f));

                                }
                                else
                                {
                                    TriggerHelper.IncreaseDecreaseButtons(xPositionIF, t: xPosition.transform);
                                    TriggerHelper.AddEventTrigger(xPosition, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(xPositionIF) });
                                }

                                var single = numberInput.Duplicate(layout, "Duration");
                                single.transform.Find("Text").GetComponent<Text>().text = "Duration";

                                var inputField = single.transform.Find("Input").GetComponent<InputField>();
                                inputField.onValueChanged.ClearAll();
                                inputField.textComponent.alignment = TextAnchor.MiddleCenter;
                                inputField.text = Parser.TryParse(modifier.commands[1], 1f).ToString();
                                inputField.onValueChanged.AddListener(delegate (string _val)
                                {
                                    if (float.TryParse(_val, out float result))
                                    {
                                        modifier.commands[1] = Mathf.Clamp(result, 0f, 9999f).ToString();
                                        modifier.active = false;
                                    }
                                });

                                TriggerHelper.IncreaseDecreaseButtons(inputField, t: single.transform);
                                TriggerHelper.AddEventTrigger(single, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(inputField) });

                                var dd = dropdownBar.Duplicate(layout, "Key");
                                dd.transform.Find("Text").GetComponent<Text>().text = "Value";

                                Destroy(dd.transform.Find("Dropdown").GetComponent<HoverTooltip>());
                                Destroy(dd.transform.Find("Dropdown").GetComponent<HideDropdownOptions>());

                                var d = dd.transform.Find("Dropdown").GetComponent<Dropdown>();
                                d.onValueChanged.RemoveAllListeners();
                                d.options.Clear();

                                foreach (var curveOption in EditorManager.inst.CurveOptions)
                                {
                                    d.options.Add(new Dropdown.OptionData(curveOption.name, curveOption.icon));
                                }

                                d.value = Parser.TryParse(modifier.commands[2], 0);

                                d.onValueChanged.AddListener(delegate (int _val)
                                {
                                    modifier.commands[2] = Mathf.Clamp(_val, 0, EditorManager.inst.CurveOptions.Count - 1).ToString();
                                });

                                if (modifier.commands.Count < 4)
                                    modifier.commands.Add("False");

                                var global = booleanBar.Duplicate(layout, "Relative");
                                global.transform.Find("Text").GetComponent<Text>().text = "Relative";

                                var globalToggle = global.transform.Find("Toggle").GetComponent<Toggle>();
                                globalToggle.onValueChanged.ClearAll();
                                globalToggle.isOn = Parser.TryParse(modifier.commands[3], false);
                                globalToggle.onValueChanged.AddListener(delegate (bool _val)
                                {
                                    modifier.commands[3] = _val.ToString();
                                    modifier.active = false;
                                });

                                break;
                            }
                    }

                    /* List of modifiers that have no values:
                     * - playerKill
                     * - playerKillAll
                     * - playerCollide
                     * - playerMoving
                     * - playerBoosting
                     * - playerAlive
                     * - playerBoost
                     * - playerBoostAll
                     * - playerDisableBoost
                     * - onPlayerHit
                     * - inZenMode
                     * - inNormal
                     * - in1Life
                     * - inNoHit
                     * - inEditor
                     * - showMouse
                     * - hideMouse
                     * - mouseOver
                     * - disableObject
                     * - disableObjectTree
                     * - bulletCollide
                     * - updateObjects
                     * - requireSignal
                     */

                    num++;
                }

                //Add Modifier
                {
                    var button = modifierAddPrefab.Duplicate(content, "add modifier");

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

        public static void SetObjectColors(Toggle[] toggles, int index, int i, BeatmapObject.Modifier modifier)
        {
            modifier.commands[index] = i.ToString();

            int num = 0;
            foreach (var toggle in toggles)
            {
                toggle.onValueChanged.RemoveAllListeners();
                int tmpIndex = num;

                toggle.isOn = num == i;

                toggle.onValueChanged.AddListener(delegate (bool _value)
                {
                    SetObjectColors(toggles, index, tmpIndex, modifier);
                });

                toggle.GetComponent<Image>().color = GameManager.inst.LiveTheme.GetObjColor(tmpIndex);

                if (!toggle.GetComponent<HoverUI>())
                {
                    var hoverUI = toggle.gameObject.AddComponent<HoverUI>();
                    hoverUI.animatePos = false;
                    hoverUI.animateSca = true;
                    hoverUI.size = 1.1f;
                }
                num++;
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

        #region Default Modifiers

        public void CreateDefaultModifiersList()
        {
            var qap = EditorManager.inst.GetDialog("Quick Actions Popup").Dialog;
            var dialog = qap.gameObject.Duplicate(qap.parent, "Default Modifiers Popup");

            EditorHelper.AddEditorDialog("Default Modifiers Popup", dialog);

            // Since ObjectTags has been long since deprecated, this is no longer needed.
            //if (dialog.transform.Find("Panel/mod-helper"))
            //{
            //    Destroy(dialog.transform.Find("Panel/mod-helper").gameObject);
            //}
            //if (dialog.transform.Find("command-input"))
            //{
            //    Destroy(dialog.transform.Find("command-input").gameObject);
            //}

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
        public void RefreshDefaultModifiersList(BeatmapObject beatmapObject)
        {
            if (ModCompatibility.sharedFunctions.ContainsKey("DefaultModifierList"))
                defaultModifiers = (List<BeatmapObject.Modifier>)ModCompatibility.sharedFunctions["DefaultModifierList"];

            var dialog = EditorManager.inst.GetDialog("Default Modifiers Popup").Dialog.gameObject;

            var contentM = dialog.transform.Find("mask/content");
            LSHelpers.DeleteChildren(contentM);

            for (int i = 0; i < defaultModifiers.Count; i++)
            {
                if (string.IsNullOrEmpty(searchTerm) || defaultModifiers[i].commands[0].ToLower().Contains(searchTerm.ToLower()))
                {
                    int tmpIndex = i;

                    var name = defaultModifiers[i].commands[0] + " (" + defaultModifiers[i].type.ToString() + ")";

                    var button = EditorManager.inst.folderButtonPrefab.Duplicate(contentM, name);

                    button.transform.GetChild(0).GetComponent<Text>().text = name;

                    var butt = button.GetComponent<Button>();
                    butt.onClick.RemoveAllListeners();
                    butt.onClick.AddListener(delegate ()
                    {
                        if (beatmapObject.shape == 4 && defaultModifiers[tmpIndex].commands[0] == "setText" || defaultModifiers[tmpIndex].commands[0] != "setText")
                        {
                            var modifier = BeatmapObject.Modifier.DeepCopy(defaultModifiers[tmpIndex]);
                            modifier.modifierObject = beatmapObject;
                            beatmapObject.modifiers.Add(modifier);
                            RTEditor.inst.StartCoroutine(ObjectEditor.RefreshObjectGUI(beatmapObject));
                            EditorManager.inst.HideDialog("Default Modifiers Popup");
                        }
                        else
                            EditorManager.inst.DisplayNotification("Cannot add modifier to object because the object needs to be a text object.", 2f, EditorManager.NotificationType.Error);
                    });
                }
            }
        }

        public List<BeatmapObject.Modifier> defaultModifiers = new List<BeatmapObject.Modifier>();

        #endregion

        #region UI Part Handlers

        GameObject booleanBar;

        GameObject numberInput;

        GameObject stringInput;

        GameObject dropdownBar;

        GameObject Base(string name)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(null);
            gameObject.transform.localScale = Vector3.one;

            var rectTransform = gameObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(0f, 32f);

            var horizontalLayoutGroup = gameObject.AddComponent<HorizontalLayoutGroup>();
            horizontalLayoutGroup.childControlWidth = false;
            horizontalLayoutGroup.childForceExpandWidth = false;
            horizontalLayoutGroup.spacing = 8f;

            var text = new GameObject("Text");
            text.transform.SetParent(rectTransform);
            text.transform.localScale = Vector3.one;
            var textRT = text.AddComponent<RectTransform>();
            textRT.anchoredPosition = new Vector2(10f, -5f);
            textRT.anchorMax = Vector2.one;
            textRT.anchorMin = Vector2.zero;
            textRT.pivot = new Vector2(0f, 1f);
            textRT.sizeDelta = new Vector2(247f, 32f);

            var textText = text.AddComponent<Text>();
            textText.alignment = TextAnchor.MiddleLeft;
            textText.font = FontManager.inst.Inconsolata;
            textText.fontSize = 19;
            textText.color = new Color(0.9373f, 0.9216f, 0.9373f);

            return gameObject;
        }

        GameObject Boolean()
        {
            var gameObject = Base("Bool");
            var rectTransform = (RectTransform)gameObject.transform;

            ((RectTransform)rectTransform.Find("Text")).sizeDelta = new Vector2(266f, 32f);

            var toggleBase = new GameObject("Toggle");
            toggleBase.transform.SetParent(rectTransform);
            toggleBase.transform.localScale = Vector3.one;

            var toggleBaseRT = toggleBase.AddComponent<RectTransform>();

            toggleBaseRT.anchorMax = Vector2.one;
            toggleBaseRT.anchorMin = Vector2.zero;
            toggleBaseRT.sizeDelta = new Vector2(32f, 32f);

            var toggle = toggleBase.AddComponent<Toggle>();

            var background = new GameObject("Background");
            background.transform.SetParent(toggleBaseRT);
            background.transform.localScale = Vector3.one;

            var backgroundRT = background.AddComponent<RectTransform>();
            backgroundRT.anchoredPosition = Vector3.zero;
            backgroundRT.anchorMax = new Vector2(0f, 1f);
            backgroundRT.anchorMin = new Vector2(0f, 1f);
            backgroundRT.pivot = new Vector2(0f, 1f);
            backgroundRT.sizeDelta = new Vector2(32f, 32f);
            var backgroundImage = background.AddComponent<Image>();

            var checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(backgroundRT);
            checkmark.transform.localScale = Vector3.one;

            var checkmarkRT = checkmark.AddComponent<RectTransform>();
            checkmarkRT.anchoredPosition = Vector3.zero;
            checkmarkRT.anchorMax = new Vector2(0.5f, 0.5f);
            checkmarkRT.anchorMin = new Vector2(0.5f, 0.5f);
            checkmarkRT.pivot = new Vector2(0.5f, 0.5f);
            checkmarkRT.sizeDelta = new Vector2(20f, 20f);
            var checkmarkImage = checkmark.AddComponent<Image>();
            checkmarkImage.sprite = SpriteManager.LoadSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_checkmark.png");
            checkmarkImage.color = new Color(0.1294f, 0.1294f, 0.1294f);

            toggle.image = backgroundImage;
            toggle.targetGraphic = backgroundImage;
            toggle.graphic = checkmarkImage;

            return gameObject;
        }

        GameObject NumberInput()
        {
            var gameObject = Base("Number");
            var rectTransform = (RectTransform)gameObject.transform;

            ((RectTransform)rectTransform.Find("Text")).sizeDelta = new Vector2(146f, 32f);

            var input = RTEditor.inst.defaultIF.Duplicate(rectTransform, "Input");
            ((RectTransform)input.transform.Find("Text")).sizeDelta = Vector2.zero;

            var buttonL = Button("<", SpriteManager.LoadSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_left_small.png"));
            buttonL.transform.SetParent(rectTransform);
            buttonL.transform.localScale = Vector3.one;

            ((RectTransform)buttonL.transform).sizeDelta = new Vector2(16f, 32f);

            var buttonR = Button(">", SpriteManager.LoadSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_right_small.png"));
            buttonR.transform.SetParent(rectTransform);
            buttonR.transform.localScale = Vector3.one;

            ((RectTransform)buttonR.transform).sizeDelta = new Vector2(16f, 32f);

            return gameObject;
        }
        
        GameObject StringInput()
        {
            var gameObject = Base("String");
            var rectTransform = (RectTransform)gameObject.transform;

            ((RectTransform)rectTransform.Find("Text")).sizeDelta = new Vector2(146f, 32f);

            var input = RTEditor.inst.defaultIF.Duplicate(rectTransform, "Input");
            ((RectTransform)input.transform).sizeDelta = new Vector2(152f, 32f);
            //((RectTransform)input.transform.Find("Text")).sizeDelta = new Vector2(142f, 100f);
            ((RectTransform)input.transform.Find("Text")).sizeDelta = Vector2.zero;

            return gameObject;
        }

        GameObject Dropdown()
        {
            var gameObject = Base("Dropdown");
            var rectTransform = (RectTransform)gameObject.transform;

            ((RectTransform)rectTransform.Find("Text")).sizeDelta = new Vector2(146f, 32f);

            var dropdownInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/autokill/tod-dropdown")
                .Duplicate(rectTransform, "Dropdown");

            return gameObject;
        }

        GameObject Button(string name, Sprite sprite)
        {
            var gameObject = new GameObject(name);
            var rectTransform = gameObject.AddComponent<RectTransform>();
            var image = gameObject.AddComponent<Image>();
            image.color = new Color(0.8784f, 0.8784f, 0.8784f);
            image.sprite = sprite;

            var button = gameObject.AddComponent<Button>();
            button.colors = UIManager.SetColorBlock(button.colors, Color.white, new Color(0.898f, 0.451f, 0.451f, 1f), Color.white, Color.white, Color.red);

            return gameObject;
        }

        #endregion
    }
}
