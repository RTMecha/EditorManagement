using EditorManagement.Functions.Helpers;
using LSFunctions;
using RTFunctions.Functions;
using RTFunctions.Functions.Components;
using RTFunctions.Functions.Data;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;
using RTFunctions.Functions.Optimization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EditorManagement.Functions.Editors
{
    public class ObjectModifiersEditor : MonoBehaviour
    {
        public static ObjectModifiersEditor inst;

        public static bool installed = false;

        public Transform content;
        public Transform scrollView;
        public RectTransform scrollViewRT;

        public bool showModifiers;

        public InputField replEditor;
        public GameObject replBase;
        public Text replText;

        public GameObject modifierCardPrefab;
        public GameObject modifierAddPrefab;

        void Awake()
        {
            if (!ModCompatibility.ObjectModifiersInstalled)
            {
                Destroy(gameObject);
            }
            else
            {
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

            try
            {
                if (ObjectEditor.inst.SelectedObjectCount == 1 && ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
                    intVariable.text = $"Integer Variable: [ {ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>().integerVariable} ]";
            }
            catch
            {
                
            }
        }

        public Text intVariable;

        public Toggle ignoreToggle;

        public void CreateModifiersOnAwake()
        {
            var bmb = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View");

            {
                var label = ObjEditor.inst.ObjectView.transform.ChildList().First(x => x.name == "label").gameObject.Duplicate(ObjEditor.inst.ObjectView.transform);

                Destroy(label.transform.GetChild(1).gameObject);
                intVariable = label.transform.GetChild(0).GetComponent<Text>();
                intVariable.text = "Integer Variable: [ null ]";
                intVariable.fontSize = 18;
            }

            {
                var ignoreGameObject = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/grain/colored"));
                ignoreGameObject.transform.SetParent(bmb.transform.Find("Viewport/Content"));
                ignoreGameObject.transform.localScale = Vector3.one;
                ignoreGameObject.name = "ignore life";
                ignoreGameObject.transform.Find("Text").GetComponent<Text>().text = "Ignore Lifespan";

                ignoreToggle = ignoreGameObject.GetComponent<Toggle>();
            }

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

            var mcpSpacerTop = new GameObject("Spacer Top");
            mcpSpacerTop.transform.SetParent(mcpRT);
            mcpSpacerTop.transform.localScale = Vector3.one;
            var mcpSpacerTopRT = mcpSpacerTop.AddComponent<RectTransform>();
            mcpSpacerTopRT.sizeDelta = new Vector2(350f, 8f);

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
            mcpTextRT.sizeDelta = new Vector2(300f, 32f);

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

            var mcpSpacerMid = new GameObject("Spacer Middle");
            mcpSpacerMid.transform.SetParent(mcpRT);
            mcpSpacerMid.transform.localScale = Vector3.one;
            var mcpSpacerMidRT = mcpSpacerMid.AddComponent<RectTransform>();
            mcpSpacerMidRT.sizeDelta = new Vector2(350f, 8f);

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

            var mcpSpacerBot = new GameObject("Spacer Botom");
            mcpSpacerBot.transform.SetParent(mcpRT);
            mcpSpacerBot.transform.localScale = Vector3.one;
            var mcpSpacerBotRT = mcpSpacerBot.AddComponent<RectTransform>();
            mcpSpacerBotRT.sizeDelta = new Vector2(350f, 8f);

            modifierAddPrefab = EditorManager.inst.folderButtonPrefab.Duplicate(null, "add modifier");

            var text = modifierAddPrefab.transform.GetChild(0).GetComponent<Text>();
            text.text = "+";
            text.alignment = TextAnchor.MiddleCenter;

            booleanBar = Boolean();

            numberInput = NumberInput();

            stringInput = StringInput();

            dropdownBar = Dropdown();
        }

        public IEnumerator RenderModifiers(BeatmapObject beatmapObject)
        {
            if (showModifiers)
            {
                LSHelpers.DeleteChildren(content);

                ((RectTransform)content.parent.parent).sizeDelta = new Vector2(351f, 300f * Mathf.Clamp(beatmapObject.modifiers.Count, 1, 5));

                ignoreToggle.onValueChanged.ClearAll();
                ignoreToggle.isOn = beatmapObject.ignoreLifespan;
                ignoreToggle.onValueChanged.AddListener(delegate (bool _val)
                {
                    beatmapObject.ignoreLifespan = _val;
                });

                int num = 0;
                foreach (var modifier in beatmapObject.modifiers)
                {
                    int index = num;
                    var gameObject = modifierCardPrefab.Duplicate(content, modifier.commands[0]);
                    gameObject.transform.localScale = Vector3.one;
                    gameObject.transform.Find("Label/Text").GetComponent<Text>().text = modifier.commands[0];

                    var delete = gameObject.transform.Find("Label/Delete").GetComponent<Button>();
                    delete.onClick.ClearAll();
                    delete.onClick.AddListener(delegate ()
                    {
                        beatmapObject.modifiers.RemoveAt(index);
                        beatmapObject.reactivePositionOffset = Vector3.zero;
                        beatmapObject.reactiveScaleOffset = Vector3.zero;
                        beatmapObject.reactiveRotationOffset = 0f;
                        Updater.UpdateProcessor(beatmapObject);
                        StartCoroutine(RenderModifiers(beatmapObject));
                    });

                    var layout = gameObject.transform.Find("Layout");

                    var constant = booleanBar.Duplicate(layout, "Constant");
                    constant.transform.localScale = Vector3.one;

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
                        not.transform.localScale = Vector3.one;
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

                    Action<string, int, float> singleGenerator = delegate (string label, int type, float defaultValue)
                    {
                        var single = numberInput.Duplicate(layout, label);
                        single.transform.localScale = Vector3.one;
                        single.transform.Find("Text").GetComponent<Text>().text = label;

                        var inputField = single.transform.Find("Input").GetComponent<InputField>();
                        inputField.onValueChanged.ClearAll();
                        inputField.textComponent.alignment = TextAnchor.MiddleCenter;
                        inputField.text = Parser.TryParse(type == 0 ? modifier.value : modifier.commands[type], defaultValue).ToString();
                        inputField.onValueChanged.AddListener(delegate (string _val)
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                if (type == 0)
                                    modifier.value = num.ToString();
                                else
                                    modifier.commands[type] = num.ToString();
                            }

                            modifier.active = false;
                        });

                        TriggerHelper.IncreaseDecreaseButtons(inputField, t: single.transform);
                        TriggerHelper.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(inputField) });
                    };
                    
                    Action<string, int, int> integerGenerator = delegate (string label, int type, int defaultValue)
                    {
                        var single = numberInput.Duplicate(layout, label);
                        single.transform.localScale = Vector3.one;
                        single.transform.Find("Text").GetComponent<Text>().text = label;

                        var inputField = single.transform.Find("Input").GetComponent<InputField>();
                        inputField.onValueChanged.ClearAll();
                        inputField.textComponent.alignment = TextAnchor.MiddleCenter;
                        inputField.text = Parser.TryParse(type == 0 ? modifier.value : modifier.commands[type], defaultValue).ToString();
                        inputField.onValueChanged.AddListener(delegate (string _val)
                        {
                            if (int.TryParse(_val, out int num))
                            {
                                if (type == 0)
                                    modifier.value = num.ToString();
                                else
                                    modifier.commands[type] = num.ToString();
                            }

                            modifier.active = false;
                        });

                        TriggerHelper.IncreaseDecreaseButtonsInt(inputField, t: single.transform);
                        TriggerHelper.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDeltaInt(inputField) });
                    };
                    
                    Action<string, int, bool> boolGenerator = delegate (string label, int type, bool defaultValue)
                    {
                        var global = booleanBar.Duplicate(layout, label);
                        global.transform.localScale = Vector3.one;
                        global.transform.Find("Text").GetComponent<Text>().text = label;

                        var globalToggle = global.transform.Find("Toggle").GetComponent<Toggle>();
                        globalToggle.onValueChanged.ClearAll();
                        globalToggle.isOn = Parser.TryParse(type == 0 ? modifier.value : modifier.commands[type], defaultValue);
                        globalToggle.onValueChanged.AddListener(delegate (bool _val)
                        {
                            if (type == 0)
                                modifier.value = _val.ToString();
                            else
                                modifier.commands[type] = _val.ToString();
                            modifier.active = false;
                        });
                    };

                    Action<string, int> stringGenerator = delegate (string label, int type)
                    {
                        var path = stringInput.Duplicate(layout, label);
                        path.transform.localScale = Vector3.one;
                        path.transform.Find("Text").GetComponent<Text>().text = label;

                        var pathInputField = path.transform.Find("Input").GetComponent<InputField>();
                        pathInputField.onValueChanged.ClearAll();
                        pathInputField.textComponent.alignment = TextAnchor.MiddleLeft;
                        pathInputField.text = type == 0 ? modifier.value : modifier.commands[type];
                        pathInputField.onValueChanged.AddListener(delegate (string _val)
                        {
                            if (type == 0)
                                modifier.value = _val;
                            else
                                modifier.commands[type] = _val;
                            modifier.active = false;
                        });
                    };

                    Action<string, int> colorGenerator = delegate (string label, int type)
                    {
                        var startColorBase = numberInput.Duplicate(layout, label);
                        startColorBase.transform.localScale = Vector3.one;

                        startColorBase.transform.Find("Text").GetComponent<Text>().text = label;

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

                        startColors.transform.AsRT().sizeDelta = new Vector2(183f, 32f);

                        SetObjectColors(startColors.GetComponentsInChildren<Toggle>(), type, Parser.TryParse(modifier.commands[type], 0), modifier);
                    };

                    Action<string, int, List<string>> dropdownGenerator = delegate (string label, int type, List<string> options)
                    {
                        var dd = dropdownBar.Duplicate(layout, label);
                        dd.transform.localScale = Vector3.one;
                        dd.transform.Find("Text").GetComponent<Text>().text = label;

                        Destroy(dd.transform.Find("Dropdown").GetComponent<HoverTooltip>());
                        Destroy(dd.transform.Find("Dropdown").GetComponent<HideDropdownOptions>());

                        var d = dd.transform.Find("Dropdown").GetComponent<Dropdown>();
                        d.onValueChanged.RemoveAllListeners();
                        d.options.Clear();

                        d.options = options.Select(x => new Dropdown.OptionData(x)).ToList();

                        d.value = Parser.TryParse(modifier.commands[type], 0);

                        d.onValueChanged.AddListener(delegate (int _val)
                        {
                            modifier.commands[type] = _val.ToString();
                            modifier.active = false;
                        });
                    };

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
                        case "playerSpeed":
                            {
                                singleGenerator("Value", 0, 1f);

                                if (cmd == "setAlphaOther")
                                    stringGenerator("Object Group", 1);

                                if (cmd == "blackHole")
                                {
                                    if (modifier.commands.Count < 2)
                                    {
                                        modifier.commands.Add("False");
                                    }

                                    boolGenerator("Use Opacity", 1, false);
                                }

                                break;
                            }
                        case "playSoundOnline":
                        case "playSound":
                            {
                                stringGenerator("Path", 0);
                                {
                                    var search = layout.Find("Path/Input").gameObject.AddComponent<Clickable>();
                                    search.onClick = delegate (PointerEventData pointerEventData)
                                    {
                                        if (pointerEventData.button == PointerEventData.InputButton.Right)
                                        {
                                            EditorManager.inst.ShowDialog("Browser Popup");
                                            RTFileBrowser.inst.UpdateBrowser(System.IO.Directory.GetCurrentDirectory(), new string[] { ".wav", ".ogg" }, onSelectFile: delegate (string _val)
                                            {
                                                var global = Parser.TryParse(modifier.commands[1], false);

                                                if (_val.Replace("\\", "/").Contains(global ? RTFile.ApplicationDirectory.Replace("\\", "/") + "beatmaps/soundlibrary/" : GameManager.inst.basePath.Replace("\\", "/")))
                                                {
                                                    layout.Find("Path/Input").GetComponent<InputField>().text = _val.Replace("\\", "/").Replace(global ? RTFile.ApplicationDirectory.Replace("\\", "/") + "beatmaps/soundlibrary/" : GameManager.inst.basePath.Replace("\\", "/"), "");
                                                    EditorManager.inst.HideDialog("Browser Popup");
                                                }
                                                else
                                                {
                                                    EditorManager.inst.DisplayNotification($"Path does not contain the proper directory.", 2f, EditorManager.NotificationType.Warning);
                                                }
                                            });
                                        }
                                    };
                                }
                                boolGenerator("Global", 1, false);
                                singleGenerator("Pitch", 2, 1f);
                                singleGenerator("Volume", 3, 1f);
                                boolGenerator("Loop", 4, false);

                                break;
                            }
                        case "updateObject":
                        case "copyColor":
                        case "copyColorOther":
                        case "loadLevel":
                        case "loadLevelInternal":
                        case "loadLevelID":
                        case "setText":
                        case "setTextOther":
                        case "addText":
                        case "addTextOther":
                        case "objectCollide":
                        case "setImage":
                        case "setImageOther":
                        case "code":
                        case "setWindowTitle":
                            {
                                if (cmd == "setTextOther" || cmd == "addTextOther" || cmd == "setImageOther")
                                {
                                    stringGenerator("Object Group", 1);
                                    stringGenerator(cmd == "setImageOther" ? "Path" : "Text", 0);
                                }

                                if (cmd == "updateObject" || cmd == "copyColor" || cmd == "copyColorOther" || cmd == "objectCollide")
                                    stringGenerator("Object Group", 0);
                                else if (cmd != "setTextOther" && cmd != "addTextOther" && cmd != "setImageOther")
                                    stringGenerator(cmd == "setText" || cmd == "addText" ? "Text" : cmd == "code" ? "Code" : cmd == "setWindowTitle" ? "Title" : "Path", 0);

                                if (cmd == "code")
                                {
                                    var clickable = layout.Find("Code").gameObject.GetComponent<Clickable>() ?? layout.Find("Code").gameObject.AddComponent<Clickable>();

                                    clickable.onDown = delegate (PointerEventData pointerEventData)
                                    {
                                        RTEditor.inst.RefreshREPLEditor(modifier.value, delegate (string _val)
                                        {
                                            modifier.value = _val;
                                        });
                                    };
                                }

                                break;
                            }
                        case "blur":
                        case "blurOther":
                        case "blurVariable":
                        case "blurVariableOther":
                            {
                                singleGenerator("Amount", 0, 0.5f);

                                if (cmd == "blur")
                                {
                                    boolGenerator("Use Opacity", 1, false);

                                    if (modifier.commands.Count < 3)
                                    {
                                        modifier.commands.Add("False");
                                    }
                                }

                                if (cmd == "blurVariableOther" || cmd == "blurOther")
                                    stringGenerator("Object Group", 1);

                                boolGenerator("Set Back to Normal", cmd != "blurVariable" ? 2 : 1, false);

                                break;
                            }
                        case "particleSystem":
                            {
                                singleGenerator("LifeTime", 0, 5f);
                                colorGenerator("Color", 3);
                                singleGenerator("StartOpacity", 4, 1f);
                                singleGenerator("EndOpacity", 5, 0f);
                                singleGenerator("StartScale", 6, 1f);
                                singleGenerator("EndScale", 7, 0f);
                                singleGenerator("Rotation", 8, 0f);
                                singleGenerator("Speed", 9, 5f);
                                singleGenerator("Amount", 10, 1f);
                                singleGenerator("Duration", 11, 1f);
                                singleGenerator("Force X", 12, 0f);
                                singleGenerator("Force Y", 13, 0f);
                                boolGenerator("Trail Emit", 14, false);

                                break;
                            }
                        case "trailRenderer":
                            {
                                singleGenerator("Time", 0, 1f);
                                singleGenerator("StartWidth", 1, 1f);
                                singleGenerator("EndWidth", 2, 0f);
                                colorGenerator("StartColor", 3);
                                colorGenerator("EndColor", 5);
                                singleGenerator("StartOpacity", 4, 1f);
                                singleGenerator("EndOpacity", 6, 0f);

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
                        case "removeText":
                        case "removeTextAt":
                        case "removeTextOther":
                        case "removeTextOtherAt":
                            {
                                integerGenerator("Value", 0, 0);

                                if (cmd == "addVariable" || cmd == "subVariable" || cmd == "setVariable" || cmd.Contains("variableOther") || cmd == "setAlphaOther" || cmd == "removeTextOther" || cmd == "removeTextOtherAt")
                                {
                                    stringGenerator("Object Group", 1);
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
                        case "loadExists":
                        case "saveFloat":
                        case "saveString":
                        case "saveText":
                        case "saveVariable":
                            {
                                if (cmd == "loadEquals" && modifier.commands.Count < 5)
                                    modifier.commands.Add("0");

                                if (cmd == "loadEquals" && Parser.TryParse(modifier.commands[4], 0) == 0 && !float.TryParse(modifier.value, out float abcdef))
                                    modifier.value = "0";

                                stringGenerator("Path", 1);
                                stringGenerator("JSON 1", 2);
                                stringGenerator("JSON 2", 3);

                                if (cmd != "saveVariable" && cmd != "saveText" && cmd != "loadExists" && cmd != "saveString" && (cmd != "loadEquals" || Parser.TryParse(modifier.commands[4], 0) == 0))
                                    singleGenerator("Value", 0, 0f);

                                if (cmd == "saveString" || cmd == "loadEquals" && Parser.TryParse(modifier.commands[4], 0) == 1)
                                    stringGenerator("Value", 0);

                                if (cmd == "loadEquals")
                                {
                                    var dd = dropdownBar.Duplicate(layout, "Type");
                                    dd.transform.localScale = Vector3.one;
                                    dd.transform.Find("Text").GetComponent<Text>().text = "Type";

                                    Destroy(dd.transform.Find("Dropdown").GetComponent<HoverTooltip>());
                                    Destroy(dd.transform.Find("Dropdown").GetComponent<HideDropdownOptions>());

                                    var d = dd.transform.Find("Dropdown").GetComponent<Dropdown>();
                                    d.onValueChanged.RemoveAllListeners();
                                    d.options.Clear();

                                    d.options = new List<Dropdown.OptionData>
                                    {
                                        new Dropdown.OptionData("Number"),
                                        new Dropdown.OptionData("Text"),
                                    };

                                    d.value = Parser.TryParse(modifier.commands[4], 0);

                                    d.onValueChanged.AddListener(delegate (int _val)
                                    {
                                        modifier.commands[4] = _val.ToString();
                                        modifier.active = false;
                                        StartCoroutine(RenderModifiers(beatmapObject));
                                    });
                                }

                                break;
                            }
                        case "reactivePos":
                        case "reactiveSca":
                        case "reactiveRot":
                        case "reactiveCol":
                        case "reactiveColLerp":
                        case "reactivePosChain":
                        case "reactiveScaChain":
                        case "reactiveRotChain":
                            {
                                singleGenerator("Total Multiply", 0, 0f);

                                if (cmd == "reactivePos" || cmd == "reactiveSca" || cmd == "reactivePosChain" || cmd == "reactiveScaChain")
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
                                    TriggerHelper.AddEventTriggerParams(samplesXIF.gameObject,
                                        TriggerHelper.ScrollDeltaInt(samplesXIF, multi: true),
                                        TriggerHelper.ScrollDeltaVector2Int(samplesXIF, samplesYIF, 1, new List<int> { 0, 255 }));
                                    TriggerHelper.AddEventTriggerParams(samplesYIF.gameObject,
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
                                    TriggerHelper.AddEventTriggerParams(multiplyXIF.gameObject,
                                        TriggerHelper.ScrollDelta(multiplyXIF, multi: true),
                                        TriggerHelper.ScrollDeltaVector2(multiplyXIF, multiplyYIF, 0.1f, 10f));
                                    TriggerHelper.AddEventTriggerParams(multiplyYIF.gameObject,
                                        TriggerHelper.ScrollDelta(multiplyYIF, multi: true),
                                        TriggerHelper.ScrollDeltaVector2(multiplyXIF, multiplyYIF, 0.1f, 10f));
                                }
                                else
                                {
                                    integerGenerator("Sample", 1, 0);

                                    if (cmd == "reactiveCol" || cmd == "reactiveColLerp")
                                    {
                                        colorGenerator("Color", 2);
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
                                TriggerHelper.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDeltaInt(inputField, 1, 0, 3) });

                                stringGenerator("Model ID", 0);

                                break;
                            }
                        case "eventOffset":
                        case "eventOffsetVariable":
                        case "eventOffsetAnimate":
                            {
                                // Event Keyframe Type
                                {
                                    var dd = dropdownBar.Duplicate(layout, "Event Type");
                                    dd.transform.Find("Text").GetComponent<Text>().text = "Event Type";

                                    Destroy(dd.transform.Find("Dropdown").GetComponent<HoverTooltip>());
                                    Destroy(dd.transform.Find("Dropdown").GetComponent<HideDropdownOptions>());

                                    var d = dd.transform.Find("Dropdown").GetComponent<Dropdown>();
                                    d.onValueChanged.RemoveAllListeners();
                                    d.options.Clear();

                                    d.options = RTEventEditor.EventTypes.Select(x => new Dropdown.OptionData(x)).ToList();

                                    d.value = Parser.TryParse(modifier.commands[1], 0);

                                    d.onValueChanged.AddListener(delegate (int _val)
                                    {
                                        modifier.commands[1] = Mathf.Clamp(_val, 0, GameData.DefaultKeyframes.Count - 1).ToString();
                                        modifier.active = false;
                                    });
                                }

                                //var type = numberInput.Duplicate(layout, "Value");
                                //type.transform.Find("Text").GetComponent<Text>().text = "Type";

                                //var typeIF = type.transform.Find("Input").GetComponent<InputField>();
                                //typeIF.onValueChanged.ClearAll();
                                //typeIF.textComponent.alignment = TextAnchor.MiddleCenter;
                                //typeIF.text = Parser.TryParse(modifier.commands[1], 0).ToString();
                                //typeIF.onValueChanged.AddListener(delegate (string _val)
                                //{
                                //    if (int.TryParse(_val, out int result))
                                //    {
                                //        modifier.commands[1] = Mathf.Clamp(result, 0, GameData.DefaultKeyframes.Count - 1).ToString();
                                //        modifier.active = false;
                                //    }
                                //});

                                //TriggerHelper.IncreaseDecreaseButtonsInt(typeIF, 1, 0, GameData.DefaultKeyframes.Count - 1, type.transform);
                                //TriggerHelper.AddEventTrigger(typeIF.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDeltaInt(typeIF, 1, 0, GameData.DefaultKeyframes.Count - 1) });

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
                                TriggerHelper.AddEventTriggerParams(vindexIF.gameObject, TriggerHelper.ScrollDeltaInt(vindexIF, 1, 0, GameData.DefaultKeyframes[Parser.TryParse(modifier.commands[1], 0)].eventValues.Length - 1));

                                singleGenerator(cmd == "eventOffsetVariable" ? "Multiply Var" : "Value", 0, 0f);

                                if (cmd == "eventOffsetAnimate")
                                {
                                    if (modifier.commands.Count < 6)
                                        modifier.commands.Add("False");

                                    singleGenerator("Time", 3, 1f);

                                    var dd = dropdownBar.Duplicate(layout, "Easing");
                                    dd.transform.Find("Text").GetComponent<Text>().text = "Easing";

                                    Destroy(dd.transform.Find("Dropdown").GetComponent<HoverTooltip>());
                                    Destroy(dd.transform.Find("Dropdown").GetComponent<HideDropdownOptions>());

                                    var d = dd.transform.Find("Dropdown").GetComponent<Dropdown>();
                                    d.onValueChanged.RemoveAllListeners();
                                    d.options.Clear();

                                    d.options = EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList();

                                    var curveIndex = EditorManager.inst.CurveOptions.FindIndex(x => modifier.commands[4] == x.name);
                                    d.value = curveIndex < 0 ? 0 : curveIndex;

                                    d.onValueChanged.AddListener(delegate (int _val)
                                    {
                                        modifier.commands[4] = EditorManager.inst.CurveOptions[EditorManager.inst.CurveOptions.Count > _val ? _val : 0].name;
                                    });

                                    boolGenerator("Relative", 5, false);
                                }

                                break;
                            }
                        case "addColor":
                        case "addColorOther":
                        case "addColorPlayerDistance":
                        case "lerpColor":
                        case "lerpColorOther":
                            {
                                if (cmd.Contains("Other"))
                                {
                                    stringGenerator("Object Group", 1);
                                }

                                colorGenerator("Color", !cmd.Contains("Other") ? 1 : 2);

                                singleGenerator("Multiply", 0, 1f);

                                break;
                            }
                        case "signalModifier":
                        case "mouseOverSignalModifier":
                            {
                                stringGenerator("Object Group", 1);

                                var single = numberInput.Duplicate(layout, "Delay");
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
                                TriggerHelper.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(inputField, max: float.PositiveInfinity) });

                                break;
                            }
                        case "randomGreater":
                        case "randomLesser":
                        case "randomEquals":
                            {
                                integerGenerator("Minimum", 1, 0);
                                integerGenerator("Maximum", 2, 0);
                                integerGenerator("Value", 0, 0);

                                break;
                            }
                        case "editorNotify":
                            {
                                stringGenerator("Text", 0);
                                singleGenerator("Time", 1, 0.5f);

                                var dd = dropdownBar.Duplicate(layout, "Notify Type");
                                dd.transform.Find("Text").GetComponent<Text>().text = "Notify Type";

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
                                    modifier.active = false;
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
                                    TriggerHelper.AddEventTriggerParams(yPositionIF.gameObject,
                                        TriggerHelper.ScrollDelta(yPositionIF),
                                        TriggerHelper.ScrollDeltaVector2(xPositionIF, yPositionIF, 0.1f, 10f));

                                }
                                else
                                {
                                    TriggerHelper.IncreaseDecreaseButtons(xPositionIF, t: xPosition.transform);
                                    TriggerHelper.AddEventTrigger(xPositionIF.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(xPositionIF) });
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
                                TriggerHelper.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(inputField) });

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
                        case "spawnPrefab":
                            {
                                var prefabIndex = numberInput.Duplicate(layout, "Index");
                                prefabIndex.transform.Find("Text").GetComponent<Text>().text = "Prefab Index";

                                var prefabIndexIF = prefabIndex.transform.Find("Input").GetComponent<InputField>();
                                prefabIndexIF.onValueChanged.ClearAll();
                                prefabIndexIF.textComponent.alignment = TextAnchor.MiddleCenter;
                                prefabIndexIF.text = Parser.TryParse(modifier.value, 0).ToString();
                                prefabIndexIF.onValueChanged.AddListener(delegate (string _val)
                                {
                                    if (int.TryParse(_val, out int result))
                                    {
                                        modifier.value = Mathf.Clamp(result, 0, DataManager.inst.gameData.prefabObjects.Count - 1).ToString();
                                        modifier.active = false;
                                    }
                                });

                                TriggerHelper.IncreaseDecreaseButtonsInt(prefabIndexIF, 1, 0, DataManager.inst.gameData.prefabObjects.Count - 1, prefabIndex.transform);
                                TriggerHelper.AddEventTrigger(prefabIndexIF.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDeltaInt(prefabIndexIF, 1, 0, DataManager.inst.gameData.prefabObjects.Count - 1) });

                                singleGenerator("Position X", 1, 0f);
                                singleGenerator("Position Y", 2, 0f);
                                singleGenerator("Scale X", 3, 0f);
                                singleGenerator("Scale Y", 4, 0f);
                                singleGenerator("Rotation", 5, 0f);

                                if (modifier.commands.Count < 8)
                                {
                                    modifier.commands.Add("0");
                                    modifier.commands.Add("0");
                                    modifier.commands.Add("1");
                                }

                                var repeatCount = numberInput.Duplicate(layout, "RepeatCount");
                                repeatCount.transform.Find("Text").GetComponent<Text>().text = "Repeat Count";

                                var repeatCountIF = repeatCount.transform.Find("Input").GetComponent<InputField>();
                                repeatCountIF.onValueChanged.ClearAll();
                                repeatCountIF.textComponent.alignment = TextAnchor.MiddleCenter;
                                repeatCountIF.text = Parser.TryParse(modifier.commands[6], 0).ToString();
                                repeatCountIF.onValueChanged.AddListener(delegate (string _val)
                                {
                                    if (int.TryParse(_val, out int num))
                                    {
                                        modifier.commands[6] = Mathf.Clamp(num, 0, 1000).ToString();
                                        modifier.active = false;
                                    }
                                });

                                TriggerHelper.IncreaseDecreaseButtons(repeatCountIF, t: repeatCount.transform);
                                TriggerHelper.AddEventTrigger(repeatCountIF.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(repeatCountIF) });

                                var repeatOffsetTime = numberInput.Duplicate(layout, "RepeatOffsetTime");
                                repeatOffsetTime.transform.Find("Text").GetComponent<Text>().text = "Repeat Offset Time";

                                var repeatOffsetTimeIF = repeatOffsetTime.transform.Find("Input").GetComponent<InputField>();
                                repeatOffsetTimeIF.onValueChanged.ClearAll();
                                repeatOffsetTimeIF.textComponent.alignment = TextAnchor.MiddleCenter;
                                repeatOffsetTimeIF.text = Parser.TryParse(modifier.commands[7], 0f).ToString();
                                repeatOffsetTimeIF.onValueChanged.AddListener(delegate (string _val)
                                {
                                    if (float.TryParse(_val, out float num))
                                    {
                                        modifier.commands[7] = Mathf.Clamp(num, 0f, 60f).ToString();
                                        modifier.active = false;
                                    }
                                });

                                TriggerHelper.IncreaseDecreaseButtons(repeatOffsetTimeIF, t: repeatOffsetTime.transform);
                                TriggerHelper.AddEventTrigger(repeatOffsetTimeIF.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(repeatOffsetTimeIF) });

                                var speed = numberInput.Duplicate(layout, "Speed");
                                speed.transform.Find("Text").GetComponent<Text>().text = "Speed";

                                var speedIF = speed.transform.Find("Input").GetComponent<InputField>();
                                speedIF.onValueChanged.ClearAll();
                                speedIF.textComponent.alignment = TextAnchor.MiddleCenter;
                                speedIF.text = Parser.TryParse(modifier.commands[8], 1f).ToString();
                                speedIF.onValueChanged.AddListener(delegate (string _val)
                                {
                                    if (float.TryParse(_val, out float num))
                                    {
                                        modifier.commands[8] = Mathf.Clamp(num, 0.01f, Updater.MaxFastSpeed).ToString();
                                        modifier.active = false;
                                    }
                                });

                                TriggerHelper.IncreaseDecreaseButtons(speedIF, min: 0.01f, max: Updater.MaxFastSpeed, t: speed.transform);
                                TriggerHelper.AddEventTrigger(speedIF.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(speedIF, min: 0.01f, max: Updater.MaxFastSpeed) });

                                break;
                            }
                        case "clampVariable":
                        case "clampVariableOther":
                            {
                                if (cmd == "clampVariableOther")
                                    stringGenerator("Object Group", 0);

                                integerGenerator("Minimum", 1, 0);
                                integerGenerator("Maximum", 2, 0);

                                break;
                            }
                        case "animateObject":
                        case "animateObjectOther":
                            {
                                singleGenerator("Time", 0, 1f);
                                dropdownGenerator("Type", 1, new List<string> { "Position", "Scale", "Rotation" });
                                singleGenerator("X", 2, 0f);
                                singleGenerator("Y", 3, 0f);
                                singleGenerator("Z", 4, 0f);
                                boolGenerator("Relative", 5, true);

                                dropdownGenerator("Easing", 6, EditorManager.inst.CurveOptions.Select(x => x.name).ToList());

                                //var dd = dropdownBar.Duplicate(layout, "Easing");
                                //dd.transform.Find("Text").GetComponent<Text>().text = "Easing";

                                //Destroy(dd.transform.Find("Dropdown").GetComponent<HoverTooltip>());
                                //Destroy(dd.transform.Find("Dropdown").GetComponent<HideDropdownOptions>());

                                //var d = dd.transform.Find("Dropdown").GetComponent<Dropdown>();
                                //d.onValueChanged.RemoveAllListeners();
                                //d.options.Clear();

                                //d.options = EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList();

                                //var curveIndex = EditorManager.inst.CurveOptions.FindIndex(x => modifier.commands[6] == x.name);
                                //d.value = curveIndex < 0 ? 0 : curveIndex;

                                //d.onValueChanged.AddListener(delegate (int _val)
                                //{
                                //    modifier.commands[6] = EditorManager.inst.CurveOptions[EditorManager.inst.CurveOptions.Count > _val ? _val : 0].name;
                                //});

                                if (cmd == "animateObjectOther")
                                {
                                    stringGenerator("Object Group", 7);
                                }

                                break;
                            }
                        case "animateVariableOther":
                            {
                                stringGenerator("Object Group", 0);

                                dropdownGenerator("From Type", 1, new List<string> { "Position", "Scale", "Rotation" });
                                dropdownGenerator("From Axis", 2, new List<string> { "X", "Y", "Z" });

                                singleGenerator("Delay", 3, 0f);

                                singleGenerator("Multiply", 4, 1f);
                                singleGenerator("Offset", 5, 0f);
                                singleGenerator("Min", 6, -99999f);
                                singleGenerator("Max", 7, 99999f);

                                singleGenerator("Loop", 8, 99999f);

                                break;
                            }
                        case "copyAxis":
                        case "copyPlayerAxis":
                            {
                                if (modifier.commands.Count < 6)
                                    modifier.commands.Add("0");
                                
                                if (modifier.commands.Count < 7)
                                    modifier.commands.Add("1");
                                
                                if (modifier.commands.Count < 8)
                                    modifier.commands.Add("0");

                                if (modifier.commands.Count < 9)
                                    modifier.commands.Add("-99999");

                                if (modifier.commands.Count < 10)
                                    modifier.commands.Add("99999");

                                if (cmd == "copyAxis")
                                {
                                    if (modifier.commands.Count < 11)
                                        modifier.commands.Add("9999");

                                    stringGenerator("Object Group", 0);
                                }

                                dropdownGenerator("From Type", 1, new List<string> { "Position", "Scale", "Rotation", "Color" });
                                dropdownGenerator("From Axis", 2, new List<string> { "X", "Y", "Z" });

                                dropdownGenerator("To Type", 3, new List<string> { "Position", "Scale", "Rotation", "Color" });
                                dropdownGenerator("To Axis (3D)", 4, new List<string> { "X", "Y", "Z" });

                                if (cmd == "copyAxis")
                                    singleGenerator("Delay", 5, 0f);

                                singleGenerator("Multiply", 6, 1f);
                                singleGenerator("Offset", 7, 0f);
                                singleGenerator("Min", 8, -99999f);
                                singleGenerator("Max", 9, 99999f);

                                if (cmd == "copyAxis")
                                    singleGenerator("Loop", 10, 99999f);

                                break;
                            }
                        case "axisEquals":
                        case "axisLesserEquals":
                        case "axisGreaterEquals":
                        case "axisLesser":
                        case "axisGreater":
                            {
                                if (modifier.commands.Count < 11)
                                {
                                    modifier.commands.Add("9999");
                                }

                                stringGenerator("Object Group", 0);

                                dropdownGenerator("Type", 1, new List<string> { "Position", "Scale", "Rotation" });
                                dropdownGenerator("Axis", 2, new List<string> { "X", "Y", "Z" });

                                singleGenerator("Delay", 3, 0f);
                                singleGenerator("Multiply", 4, 1f);
                                singleGenerator("Offset", 5, 0f);
                                singleGenerator("Min", 6, -99999f);
                                singleGenerator("Max", 7, 99999f);
                                singleGenerator("Equals", 8, 1f);
                                boolGenerator("Use Visual", 9, false);
                                singleGenerator("Loop", 10, 99999f);

                                break;
                            }
                        case "setVariableRandom":
                            {
                                stringGenerator("Object Group", 0);
                                integerGenerator("Minimum Range", 1, 0);
                                integerGenerator("Maximum Range", 2, 0);

                                break;
                            }
                        case "rigidbody":
                        case "rigidbodyOther":
                            {
                                if (cmd == "rigidbodyOther")
                                    stringGenerator("Object Group", 0);

                                singleGenerator("Gravity", 1, 0f);

                                {
                                    var dd = dropdownBar.Duplicate(layout, "Collision Mode");
                                    dd.transform.Find("Text").GetComponent<Text>().text = "Collision Mode";

                                    Destroy(dd.transform.Find("Dropdown").GetComponent<HoverTooltip>());
                                    Destroy(dd.transform.Find("Dropdown").GetComponent<HideDropdownOptions>());

                                    var d = dd.transform.Find("Dropdown").GetComponent<Dropdown>();
                                    d.onValueChanged.RemoveAllListeners();
                                    d.options.Clear();

                                    d.options = new List<Dropdown.OptionData>
                                {
                                    new Dropdown.OptionData("Discrete"),
                                    new Dropdown.OptionData("Continuous")
                                };

                                    d.value = Parser.TryParse(modifier.commands[2], 0);

                                    d.onValueChanged.AddListener(delegate (int _val)
                                    {
                                        modifier.commands[2] = Mathf.Clamp(_val, 0, 1).ToString();
                                    });
                                }

                                singleGenerator("Drag", 3, 0f);
                                singleGenerator("Velocity X", 4, 0f);
                                singleGenerator("Velocity Y", 5, 0f);

                                {
                                    var dd = dropdownBar.Duplicate(layout, "Body Type");
                                    dd.transform.Find("Text").GetComponent<Text>().text = "Body Type";

                                    Destroy(dd.transform.Find("Dropdown").GetComponent<HoverTooltip>());
                                    Destroy(dd.transform.Find("Dropdown").GetComponent<HideDropdownOptions>());

                                    var d = dd.transform.Find("Dropdown").GetComponent<Dropdown>();
                                    d.onValueChanged.RemoveAllListeners();
                                    d.options.Clear();

                                    d.options = new List<Dropdown.OptionData>
                                    {
                                        new Dropdown.OptionData("Dynamic"),
                                        new Dropdown.OptionData("Kinematic"),
                                        new Dropdown.OptionData("Static"),
                                    };

                                    d.value = Parser.TryParse(modifier.commands[6], 0);

                                    d.onValueChanged.AddListener(delegate (int _val)
                                    {
                                        modifier.commands[6] = Mathf.Clamp(_val, 0, 2).ToString();
                                    });
                                }

                                break;
                            }
                        case "gravity":
                        case "gravityOther":
                            {
                                if (cmd == "gravityOther")
                                    stringGenerator("Object Group", 0);

                                singleGenerator("X", 1, -1f);
                                singleGenerator("Y", 2, 0f);

                                break;
                            }
                        case "enableObjectTree":
                        case "disableObjectTree":
                            {
                                if (modifier.value == "0")
                                    modifier.value = "False";

                                boolGenerator("Use Self", 0, true);

                                break;
                            }
                        case "levelRankEquals":
                        case "levelRankLesserEquals":
                        case "levelRankGreaterEquals":
                        case "levelRankLesser":
                        case "levelRankGreater":
                            {
                                dropdownGenerator("Rank", 0, DataManager.inst.levelRanks.Select(x => x.name).ToList());

                                break;
                            }
                        case "setDiscordStatus":
                            {
                                stringGenerator("State", 0);
                                stringGenerator("Details", 1);
                                dropdownGenerator("Sub Icon", 2, new List<string> { "Arcade", "Editor", "Play" });
                                dropdownGenerator("Icon", 3, new List<string> { "PA Logo White", "PA Logo Black" });

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

        public void SetObjectColors(Toggle[] toggles, int index, int i, BeatmapObject.Modifier modifier)
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

        public void OpenREPLEditor(BeatmapObject.Modifier modifier, string value)
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

            EditorHelper.AddEditorPopup("Default Modifiers Popup", dialog);

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

        public string searchTerm;
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
                        var cmd = defaultModifiers[tmpIndex].commands[0];
                        if (cmd.Contains("Text") && !cmd.Contains("Other") && beatmapObject.shape != 4)
                        {
                            EditorManager.inst.DisplayNotification("Cannot add modifier to object because the object needs to be a Text Object.", 2f, EditorManager.NotificationType.Error);
                            return;
                        }

                        if (cmd.Contains("Image") && !cmd.Contains("Other") && beatmapObject.shape != 6)
                        {
                            EditorManager.inst.DisplayNotification("Cannot add modifier to object because the object needs to be an Image Object.", 2f, EditorManager.NotificationType.Error);
                            return;
                        }

                        var modifier = BeatmapObject.Modifier.DeepCopy(defaultModifiers[tmpIndex]);
                        modifier.modifierObject = beatmapObject;
                        beatmapObject.modifiers.Add(modifier);
                        RTEditor.inst.StartCoroutine(ObjectEditor.RefreshObjectGUI(beatmapObject));
                        EditorManager.inst.HideDialog("Default Modifiers Popup");
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
            gameObject.transform.SetParent(transform);
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
            rectTransform.localScale = Vector2.one;

            ((RectTransform)rectTransform.Find("Text")).sizeDelta = new Vector2(146f, 32f);

            var input = RTEditor.inst.defaultIF.Duplicate(rectTransform, "Input");
            input.transform.localScale = Vector2.one;
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
            rectTransform.localScale = Vector2.one;

            ((RectTransform)rectTransform.Find("Text")).sizeDelta = new Vector2(146f, 32f);

            var input = RTEditor.inst.defaultIF.Duplicate(rectTransform, "Input");
            input.transform.localScale = Vector2.one;
            ((RectTransform)input.transform).sizeDelta = new Vector2(152f, 32f);
            ((RectTransform)input.transform.Find("Text")).sizeDelta = Vector2.zero;

            return gameObject;
        }

        GameObject Dropdown()
        {
            var gameObject = Base("Dropdown");
            var rectTransform = (RectTransform)gameObject.transform;
            rectTransform.localScale = Vector2.one;

            ((RectTransform)rectTransform.Find("Text")).sizeDelta = new Vector2(146f, 32f);

            var dropdownInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/autokill/tod-dropdown")
                .Duplicate(rectTransform, "Dropdown");
            dropdownInput.transform.localScale = Vector2.one;

            return gameObject;
        }

        GameObject Button(string name, Sprite sprite)
        {
            var gameObject = new GameObject(name);
            var rectTransform = gameObject.AddComponent<RectTransform>();
            rectTransform.localScale = Vector2.one;

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
