using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using HarmonyLib;
using LSFunctions;

using EditorManagement.Functions.Components;
using EditorManagement.Functions.Tools;

using RTFunctions.Functions;

namespace EditorManagement.Functions.Editors
{
    public class CreativePlayersEditor : MonoBehaviour
    {
        public static GameObject editorDialogObject;
        public static Transform editorDialogTransform;
        public static Transform editorDialogTitle;
        public static Transform editorDialogSpacer;
        public static Transform editorDialogContent;
        public static Transform editorDialogText;

        public static Dropdown playerModelDropdown;

        public static GameObject objectDialog;
        public static Transform objectDialogTF;
        public static Transform objectDialogContent;
        public static Text objectTitle;
        public static Text objectText;

        public static GameObject customObjectButtonPrefab;

        public static InputField searchField;
        public static string searchTerm;

        public static string customSearchTerm;

        public static List<GameObject> playerObjects = new List<GameObject>();

        public static Type playerPlugin;
        public static CreativePlayersEditor inst;
        public static Type playerExtensions;

        public static Font editorFont;

        public static string currentID;

        private void Awake()
        {
            if (!GameObject.Find("BepInEx_Manager").GetComponentByName("PlayerPlugin"))
            {
                Destroy(gameObject);
            }
            else
            {
                playerPlugin = GameObject.Find("BepInEx_Manager").GetComponentByName("PlayerPlugin").GetType();
                inst = this;

                editorFont = GameObject.Find("TitleBar/File/Text").GetComponent<Text>().font;

                editorDialogObject = Instantiate(EditorManager.inst.GetDialog("Multi Keyframe Editor (Object)").Dialog.gameObject);
                editorDialogTransform = editorDialogObject.transform;
                editorDialogObject.name = "PlayerEditorDialog";
                editorDialogObject.layer = 5;
                editorDialogTransform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs").transform);
                editorDialogTransform.localScale = Vector3.one;
                editorDialogTransform.position = new Vector3(1537.5f, 714.945f, 0f) * EditorManager.inst.ScreenScale;
                editorDialogObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 32f);

                editorDialogTitle = editorDialogTransform.GetChild(0);
                editorDialogTitle.GetComponent<Image>().color = LSColors.HexToColor("E57373");
                editorDialogTitle.GetChild(0).GetComponent<Text>().text = "- Player Editor -";

                editorDialogSpacer = editorDialogTransform.GetChild(1);
                editorDialogSpacer.GetComponent<RectTransform>().sizeDelta = new Vector2(765f, 54f);
                //GridLayoutGroup playerFunctionsGroup = editorDialogSpacer.gameObject.AddComponent<GridLayoutGroup>();
                //playerFunctionsGroup.cellSize = new Vector2(248f, 50f);
                //playerFunctionsGroup.spacing = new Vector2(8f, 8f);

                var label = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content").transform.GetChild(2).gameObject;
                var singleInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move/position/x");
                var vector2Input = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move/position");
                var boolInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/toggle/toggle");
                var sliderFullInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/bpm");
                var stringInput = GameObject.Find("TimelineBar/GameObject/Time Input");
                var dropdownInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/autokill/tod-dropdown");
                var colorsInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/color/color");

                playerExtensions = AccessTools.TypeByName("CreativePlayers.Functions.PlayerExtensions");

                //Dropdown
                {
                    GameObject x = Instantiate(dropdownInput);
                    x.transform.SetParent(editorDialogSpacer);
                    x.transform.localScale = Vector3.one;
                    x.name = "models";

                    Destroy(x.GetComponent<HoverTooltip>());
                    Destroy(x.GetComponent<HideDropdownOptions>());

                    var xrt = x.GetComponent<RectTransform>();
                    xrt.anchoredPosition = new Vector2(124f, 30f);
                    xrt.sizeDelta = new Vector2(248f, 50f);

                    playerModelDropdown = x.GetComponent<Dropdown>();
                    playerModelDropdown.onValueChanged.RemoveAllListeners();
                    playerModelDropdown.options.Clear();
                    playerModelDropdown.options = new List<Dropdown.OptionData>();
                }

                //Button 1
                {
                    var b1 = Instantiate(EditorManager.inst.folderButtonPrefab);
                    b1.transform.SetParent(editorDialogSpacer);
                    b1.transform.localScale = Vector3.one;
                    b1.name = "save";

                    var b1RT = b1.GetComponent<RectTransform>();
                    b1RT.anchoredPosition = new Vector2(256f, 55f);
                    b1RT.sizeDelta = new Vector2(100f, 50f);

                    b1.transform.GetChild(0).GetComponent<Text>().text = "Save All";
                    var butt = b1.GetComponent<Button>();
                    butt.onClick.RemoveAllListeners();
                    butt.onClick.AddListener(delegate ()
                    {
                        playerPlugin.GetMethod("SavePlayerModels").Invoke(playerPlugin, new object[] { });
                    });
                }

                //Button 2
                {
                    var b1 = Instantiate(EditorManager.inst.folderButtonPrefab);
                    b1.transform.SetParent(editorDialogSpacer);
                    b1.transform.localScale = Vector3.one;
                    b1.name = "create";

                    var b1RT = b1.GetComponent<RectTransform>();
                    b1RT.anchoredPosition = new Vector2(366f, 55f);
                    b1RT.sizeDelta = new Vector2(120f, 50f);

                    b1.transform.GetChild(0).GetComponent<Text>().text = "Create New";
                    var butt = b1.GetComponent<Button>();
                    butt.onClick.RemoveAllListeners();
                    butt.onClick.AddListener(delegate ()
                    {
                        var num = playerModelDropdown.options.Count;
                        playerPlugin.GetMethod("CreateNewPlayerModel").Invoke(playerPlugin, new object[] { });

                        playerExtensions.GetMethod("SetPlayerModelIndex").Invoke(playerExtensions, new object[] { 0, num });

                        playerPlugin.GetMethod("StartRespawnPlayers").Invoke(playerPlugin, new object[] { });

                        inst.StartCoroutine(RenderDialog());
                    });
                }

                //Button 3
                {
                    var b1 = Instantiate(EditorManager.inst.folderButtonPrefab);
                    b1.transform.SetParent(editorDialogSpacer);
                    b1.transform.localScale = Vector3.one;
                    b1.name = "reload";

                    var b1RT = b1.GetComponent<RectTransform>();
                    b1RT.anchoredPosition = new Vector2(495f, 55f);
                    b1RT.sizeDelta = new Vector2(80f, 50f);

                    b1.transform.GetChild(0).GetComponent<Text>().text = "Reload";
                    var butt = b1.GetComponent<Button>();
                    butt.onClick.RemoveAllListeners();
                    butt.onClick.AddListener(delegate ()
                    {
                        ModCompatibility.ClearPlayerModels();

                        inst.StartCoroutine(RenderDialog());
                    });
                }

                editorDialogText = editorDialogTransform.GetChild(2);

                editorDialogText.SetSiblingIndex(1);

                editorDialogText.GetComponent<Text>().text = "You are currently editing the current player model. Have fun messing around with what you can do here!";

                var search = Instantiate(EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("search-box").gameObject);
                search.transform.SetParent(editorDialogTransform);
                search.transform.localScale = Vector3.one;
                search.name = "search";

                searchField = search.transform.GetChild(0).GetComponent<InputField>();

                searchField.onValueChanged.ClearAll();
                searchField.text = "";
                searchField.onValueChanged.AddListener(delegate (string _val)
                {
                    searchTerm = _val;
                    inst.StartCoroutine(RenderDialog());
                });

                search.transform.GetChild(0).Find("Placeholder").GetComponent<Text>().text = "Search for value...";

                var scrollView = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View"));
                editorDialogContent = scrollView.transform;
                editorDialogContent.SetParent(editorDialogTransform);
                editorDialogContent.localScale = Vector3.one;
                scrollView.name = "Scroll View";

                //var glg = scrollView.AddComponent<GridLayoutGroup>();
                //glg.cellSize = new Vector2(124f, 64f);
                //glg.spacing = new Vector2(4f, 4f);

                LSHelpers.DeleteChildren(scrollView.transform.Find("Viewport/Content"), false);

                scrollView.GetComponent<RectTransform>().sizeDelta = new Vector2(765f, 512f);

                Triggers.AddEditorDialog("Player Editor", editorDialogObject);

                objectDialog = Instantiate(EditorManager.inst.GetDialog("Background Editor").Dialog.gameObject);
                objectDialogTF = objectDialog.transform;
                objectDialog.name = "PlayerObjectEditorDialog";
                objectDialog.layer = 5;
                objectDialogTF.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs").transform);
                objectDialogTF.transform.localScale = Vector3.one;
                objectDialogTF.position = new Vector3(1537.5f, 714.945f, 0f) * EditorManager.inst.ScreenScale;

                //Modify
                {
                    var rt = objectDialog.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(0f, 32f);
                    rt.sizeDelta = new Vector2(0f, 32f);

                    var data = objectDialogTF.Find("data");
                    var left = data.Find("left");
                    var right = data.Find("right");
                    var titlebar = data.Find("titlebar");
                    objectDialogContent = right.Find("backgrounds/viewport/content");

                    customObjectButtonPrefab = objectDialogContent.GetChild(0).gameObject;
                    customObjectButtonPrefab.transform.SetParent(null);

                    right.Find("backgrounds").gameObject.name = "objects";

                    right.Find("search/Placeholder").GetComponent<Text>().text = "Search for object...";
                    right.Find("create/Text").GetComponent<Text>().text = "Create New Custom Object";

                    titlebar.Find("left/title").GetComponent<Text>().text = "- Current Object Props -";
                    titlebar.Find("right/title").GetComponent<Text>().text = "- Custom Object List -";

                    var toggle = left.GetChild(1).GetChild(0).gameObject;
                    toggle.transform.SetParent(null);

                    var objtodel = new List<GameObject>();

                    objtodel.Add(left.GetChild(2).gameObject);
                    objtodel.Add(left.GetChild(3).gameObject);
                    objtodel.Add(left.GetChild(8).gameObject);
                    objtodel.Add(left.GetChild(9).gameObject);
                    objtodel.Add(left.GetChild(12).gameObject);
                    objtodel.Add(left.GetChild(13).gameObject);
                    objtodel.Add(left.GetChild(14).gameObject);
                    objtodel.Add(left.GetChild(15).gameObject);
                    objtodel.Add(left.GetChild(16).gameObject);

                    foreach (var del in objtodel)
                        DestroyImmediate(del);

                    left.GetChild(0).GetChild(0).GetComponent<Text>().text = "Name";

                    left.Find("name/name").GetComponent<RectTransform>().sizeDelta = new Vector2(366f, 32f);

                    var scaLabel = left.GetChild(4).gameObject;
                    var scaField = left.GetChild(5).gameObject;

                    left.Find("position/x/text-field").GetComponent<RectTransform>().sizeDelta = new Vector2(111f, 32f);
                    left.Find("position/y/text-field").GetComponent<RectTransform>().sizeDelta = new Vector2(111f, 32f);
                    left.Find("position").GetComponent<HorizontalLayoutGroup>().childControlWidth = false;

                    left.Find("scale/x/text-field").GetComponent<RectTransform>().sizeDelta = new Vector2(111f, 32f);
                    left.Find("scale/y/text-field").GetComponent<RectTransform>().sizeDelta = new Vector2(111f, 32f);
                    left.Find("scale").GetComponent<HorizontalLayoutGroup>().childControlWidth = false;

                    var rotLabel = Instantiate(scaLabel);
                    rotLabel.transform.SetParent(left);
                    rotLabel.transform.localScale = Vector3.one;
                    rotLabel.transform.SetSiblingIndex(6);
                    rotLabel.name = "label";

                    DestroyImmediate(rotLabel.transform.GetChild(1).gameObject);
                    rotLabel.transform.GetChild(0).GetComponent<Text>().text = "Rotation";

                    var rotField = Instantiate(scaField);
                    rotField.transform.SetParent(left);
                    rotField.transform.localScale = Vector3.one;
                    rotField.transform.SetSiblingIndex(7);
                    rotField.name = "rotation";

                    DestroyImmediate(rotField.transform.GetChild(1).gameObject);

                    //Custom Color
                    {
                        var rotOffsetLabel = Instantiate(rotLabel);
                        rotOffsetLabel.transform.SetParent(left);
                        rotOffsetLabel.transform.localScale = Vector3.one;
                        rotOffsetLabel.name = "label";

                        rotOffsetLabel.transform.GetChild(0).GetComponent<Text>().text = "Custom Color";

                        var custom = Instantiate(left.Find("name").gameObject);
                        custom.transform.SetParent(left);
                        custom.transform.localScale = Vector3.one;
                        custom.name = "custom color";
                    }

                    //Opacity
                    {
                        var opacityLabel = Instantiate(rotLabel);
                        opacityLabel.transform.SetParent(left);
                        opacityLabel.transform.localScale = Vector3.one;
                        opacityLabel.name = "label";

                        opacityLabel.transform.GetChild(0).GetComponent<Text>().text = "Opacity";

                        var opacity = Instantiate(rotField);
                        opacity.transform.SetParent(left);
                        opacity.transform.localScale = Vector3.one;
                        opacity.name = "opacity";
                    }

                    //Depth
                    {
                        var depthLabel = Instantiate(rotLabel);
                        depthLabel.transform.SetParent(left);
                        depthLabel.transform.localScale = Vector3.one;
                        depthLabel.name = "label";

                        depthLabel.transform.GetChild(0).GetComponent<Text>().text = "Depth";

                        var depth = Instantiate(rotField);
                        depth.transform.SetParent(left);
                        depth.transform.localScale = Vector3.one;
                        depth.name = "depth";
                    }

                    //Parent Dropdown
                    {
                        var posOffsetLabel = Instantiate(rotLabel);
                        posOffsetLabel.transform.SetParent(left);
                        posOffsetLabel.transform.localScale = Vector3.one;
                        posOffsetLabel.name = "label";

                        posOffsetLabel.transform.GetChild(0).GetComponent<Text>().text = "Parent";

                        GameObject x = Instantiate(dropdownInput);
                        x.transform.SetParent(left);
                        x.transform.localScale = Vector3.one;
                        x.name = "parent";

                        Destroy(x.GetComponent<HoverTooltip>());
                        Destroy(x.GetComponent<HideDropdownOptions>());

                        var xrt = x.GetComponent<RectTransform>();
                        xrt.anchoredPosition = new Vector2(124f, 30f);
                        xrt.sizeDelta = new Vector2(248f, 32f);

                        var parentDropdown = x.GetComponent<Dropdown>();
                        parentDropdown.onValueChanged.RemoveAllListeners();
                        parentDropdown.options.Clear();
                        parentDropdown.options = new List<Dropdown.OptionData>()
                        {
                            new Dropdown.OptionData("Head"),
                            new Dropdown.OptionData("Boost"),
                            new Dropdown.OptionData("Boost Tail"),
                            new Dropdown.OptionData("Tail 1"),
                            new Dropdown.OptionData("Tail 2"),
                            new Dropdown.OptionData("Tail 3"),
                            new Dropdown.OptionData("Face"),
                        };
                    }

                    //Parent Offset
                    {
                        var posOffsetLabel = Instantiate(rotLabel);
                        posOffsetLabel.transform.SetParent(left);
                        posOffsetLabel.transform.localScale = Vector3.one;
                        posOffsetLabel.name = "label";

                        posOffsetLabel.transform.GetChild(0).GetComponent<Text>().text = "Parent Pos Offset";

                        var posOffset = Instantiate(rotField);
                        posOffset.transform.SetParent(left);
                        posOffset.transform.localScale = Vector3.one;
                        posOffset.name = "parent pos offset";

                        var scaOffsetLabel = Instantiate(rotLabel);
                        scaOffsetLabel.transform.SetParent(left);
                        scaOffsetLabel.transform.localScale = Vector3.one;
                        scaOffsetLabel.name = "label";

                        scaOffsetLabel.transform.GetChild(0).GetComponent<Text>().text = "Parent Sca Offset";

                        var scaOffset = Instantiate(rotField);
                        scaOffset.transform.SetParent(left);
                        scaOffset.transform.localScale = Vector3.one;
                        scaOffset.name = "parent sca offset";

                        var scaActiveParent = new GameObject("p");
                        scaActiveParent.transform.SetParent(scaOffset.transform);
                        scaActiveParent.transform.localScale = Vector3.one;
                        scaActiveParent.transform.SetAsFirstSibling();
                        scaActiveParent.AddComponent<RectTransform>();

                        var scaActive = Instantiate(toggle);
                        scaActive.transform.SetParent(scaActiveParent.transform);
                        scaActive.transform.localScale = Vector3.one;
                        scaActive.name = "parent sca active";
                        scaActive.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                        var rotOffsetLabel = Instantiate(rotLabel);
                        rotOffsetLabel.transform.SetParent(left);
                        rotOffsetLabel.transform.localScale = Vector3.one;
                        rotOffsetLabel.name = "label";

                        rotOffsetLabel.transform.GetChild(0).GetComponent<Text>().text = "Parent Rot Offset";

                        var rotOffset = Instantiate(rotField);
                        rotOffset.transform.SetParent(left);
                        rotOffset.transform.localScale = Vector3.one;
                        rotOffset.name = "parent rot offset";

                        var rotActiveParent = new GameObject("p");
                        rotActiveParent.transform.SetParent(rotOffset.transform);
                        rotActiveParent.transform.localScale = Vector3.one;
                        rotActiveParent.transform.SetAsFirstSibling();
                        rotActiveParent.AddComponent<RectTransform>();

                        var rotActive = Instantiate(toggle);
                        rotActive.transform.SetParent(rotActiveParent.transform);
                        rotActive.transform.localScale = Vector3.one;
                        rotActive.name = "parent rot active";
                        rotActive.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                    }

                    //Shape
                    {
                        var depthLabel = Instantiate(rotLabel);
                        depthLabel.transform.SetParent(left);
                        depthLabel.transform.localScale = Vector3.one;
                        depthLabel.name = "label";

                        depthLabel.transform.GetChild(0).GetComponent<Text>().text = "Shape";

                        //Shape
                        {
                            var bar = Instantiate(singleInput);
                            if (bar.GetComponent<InputField>())
                                Destroy(bar.GetComponent<InputField>());
                            if (bar.GetComponent<EventInfo>())
                                Destroy(bar.GetComponent<EventInfo>());
                            if (bar.GetComponent<EventTrigger>())
                                Destroy(bar.GetComponent<EventTrigger>());

                            LSHelpers.DeleteChildren(bar.transform);
                            bar.transform.SetParent(left);
                            bar.transform.localScale = Vector3.one;
                            bar.name = "shape";

                            //var l = Instantiate(label);
                            //l.transform.SetParent(bar.transform);
                            //l.transform.SetAsFirstSibling();
                            //l.transform.localScale = Vector3.one;
                            //l.transform.GetChild(0).GetComponent<Text>().text = "Shape";
                            //l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(134f, 20f);

                            //var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
                            //{
                            //    ltextrt.anchoredPosition = new Vector2(10f, -5f);
                            //}

                            bar.GetComponent<Image>().enabled = true;
                            bar.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);

                            GameObject x = Instantiate(dropdownInput);
                            x.transform.SetParent(bar.transform);
                            x.transform.localScale = Vector3.one;
                            x.name = "x";

                            RectTransform xRT = x.GetComponent<RectTransform>();
                            xRT.anchoredPosition = new Vector2(624f, -16f);
                            xRT.sizeDelta = new Vector2(248f, 32f);

                            Destroy(x.GetComponent<HoverTooltip>());

                            Dropdown dropdown = x.GetComponent<Dropdown>();
                            dropdown.options.Clear();
                            dropdown.onValueChanged.RemoveAllListeners();

                            dropdown.options = GetShapes();

                            var hide = x.AddComponent<HideDropdownOptions>();
                            hide.DisabledOptions = new List<bool>
                            {
                                false,
                                false,
                                false,
                                false,
                                true,
                                false,
                                true,
                                false,
                                false,
                            };
                        }

                        //Shape Option
                        {
                            var bar = Instantiate(singleInput);
                            if (bar.GetComponent<InputField>())
                                Destroy(bar.GetComponent<InputField>());
                            if (bar.GetComponent<EventInfo>())
                                Destroy(bar.GetComponent<EventInfo>());
                            if (bar.GetComponent<EventTrigger>())
                                Destroy(bar.GetComponent<EventTrigger>());

                            LSHelpers.DeleteChildren(bar.transform);
                            bar.transform.SetParent(left);
                            bar.transform.localScale = Vector3.one;
                            bar.name = "shape option";

                            //var l = Instantiate(label);
                            //l.transform.SetParent(bar.transform);
                            //l.transform.SetAsFirstSibling();
                            //l.transform.localScale = Vector3.one;
                            //l.transform.GetChild(0).GetComponent<Text>().text = "Shape Option";
                            //l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(134f, 20f);

                            //var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
                            //{
                            //    ltextrt.anchoredPosition = new Vector2(10f, -5f);
                            //}

                            bar.GetComponent<Image>().enabled = true;
                            bar.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);

                            GameObject x = Instantiate(dropdownInput);
                            x.transform.SetParent(bar.transform);
                            x.transform.localScale = Vector3.one;
                            x.name = "x";

                            RectTransform xRT = x.GetComponent<RectTransform>();
                            xRT.anchoredPosition = new Vector2(624f, -16f);
                            xRT.sizeDelta = new Vector2(248f, 32f);

                            Destroy(x.GetComponent<HoverTooltip>());
                            Destroy(x.GetComponent<HideDropdownOptions>());

                            Dropdown dropdown = x.GetComponent<Dropdown>();
                            dropdown.options.Clear();
                            dropdown.onValueChanged.RemoveAllListeners();

                            dropdown.options = GetShapeOptions(0);
                        }
                    }
                    
                    //Visibility
                    {
                        var posOffsetLabel = Instantiate(rotLabel);
                        posOffsetLabel.transform.SetParent(left);
                        posOffsetLabel.transform.localScale = Vector3.one;
                        posOffsetLabel.name = "label";

                        posOffsetLabel.transform.GetChild(0).GetComponent<Text>().text = "Visibility";

                        GameObject x = Instantiate(dropdownInput);
                        x.transform.SetParent(left);
                        x.transform.localScale = Vector3.one;
                        x.name = "visibility";

                        Destroy(x.GetComponent<HoverTooltip>());
                        Destroy(x.GetComponent<HideDropdownOptions>());

                        var xrt = x.GetComponent<RectTransform>();
                        xrt.anchoredPosition = new Vector2(124f, 30f);
                        xrt.sizeDelta = new Vector2(248f, 32f);

                        var parentDropdown = x.GetComponent<Dropdown>();
                        parentDropdown.onValueChanged.RemoveAllListeners();
                        parentDropdown.options.Clear();
                        parentDropdown.options = new List<Dropdown.OptionData>()
                        {
                            new Dropdown.OptionData("Always"),
                            new Dropdown.OptionData("Boosting"),
                            new Dropdown.OptionData("Being Hit"),
                            new Dropdown.OptionData("In Zen Mode"),
                            new Dropdown.OptionData("Health Percentage"),
                            new Dropdown.OptionData("Health Equals Greater"),
                            new Dropdown.OptionData("Health Equals"),
                            new Dropdown.OptionData("Health Greater"),
                            new Dropdown.OptionData("Key Held"),
                        };
                    }

                    //Visibility Health Percentage
                    {
                        var rotOffsetLabel = Instantiate(rotLabel);
                        rotOffsetLabel.transform.SetParent(left);
                        rotOffsetLabel.transform.localScale = Vector3.one;
                        rotOffsetLabel.name = "label";

                        rotOffsetLabel.transform.GetChild(0).GetComponent<Text>().text = "Visibility Properties";

                        var rotOffset = Instantiate(rotField);
                        rotOffset.transform.SetParent(left);
                        rotOffset.transform.localScale = Vector3.one;
                        rotOffset.name = "visibility health percentage";

                        var rotActiveParent = new GameObject("p");
                        rotActiveParent.transform.SetParent(rotOffset.transform);
                        rotActiveParent.transform.localScale = Vector3.one;
                        rotActiveParent.transform.SetAsFirstSibling();
                        rotActiveParent.AddComponent<RectTransform>();

                        var rotActive = Instantiate(toggle);
                        rotActive.transform.SetParent(rotActiveParent.transform);
                        rotActive.transform.localScale = Vector3.one;
                        rotActive.name = "visibility not";
                        rotActive.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                    }

                    //Color
                    {
                        var colorParent = left.Find("color");
                        LSHelpers.DeleteChildren(colorParent);

                        Destroy(colorParent.GetComponent<GridLayoutGroup>());

                        GameObject x = Instantiate(colorsInput);
                        x.transform.SetParent(colorParent);
                        x.transform.localScale = Vector3.one;
                        x.name = "color";
                        if (x.GetComponent<HoverTooltip>())
                            Destroy(x.GetComponent<HoverTooltip>());

                        for (int i = 19; i < 26; i++)
                        {
                            var colorButton = Instantiate(x.transform.GetChild(0).gameObject);
                            colorButton.name = i.ToString();
                            colorButton.transform.SetParent(x.transform);
                            colorButton.transform.localScale = Vector3.one;
                        }

                        x.GetComponent<RectTransform>().sizeDelta = new Vector2(351f, 32f);
                        x.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                        colorParent.GetComponent<RectTransform>().sizeDelta = new Vector2(351f, 152f);
                    }

                    //Set UI Parents
                    {
                        var listtoadd = new List<Transform>();
                        for (int i = 0; i < left.transform.childCount; i++)
                            listtoadd.Add(left.transform.GetChild(i));

                        var bmb = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View");

                        var e = Instantiate(bmb);

                        var scrollView2 = e.transform;

                        scrollView2.SetParent(left);
                        scrollView2.localScale = Vector3.one;
                        scrollView2.name = "Object Scroll View";

                        var scrollViewRT = scrollView2.GetComponent<RectTransform>();
                        scrollViewRT.sizeDelta = new Vector2(366f, 690f);

                        var content = scrollView2.Find("Viewport/Content");
                        LSHelpers.DeleteChildren(content);

                        foreach (var l in listtoadd)
                        {
                            l.SetParent(content);
                        }
                    }

                    //Close
                    {
                        var l = titlebar.Find("left");

                        var close = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("Panel/x");

                        var exit = Instantiate(close.gameObject);
                        exit.transform.SetParent(l.transform);
                        exit.transform.localScale = Vector3.one;
                        exit.name = "close";

                        exit.GetComponent<RectTransform>().anchoredPosition = new Vector2(-350f, 0f);

                        var exitButton = exit.GetComponent<Button>();
                        exitButton.onClick.ClearAll();
                        exitButton.onClick.AddListener(delegate ()
                        {
                            EditorManager.inst.HideDialog("Player Object Editor");
                        });
                    }
                }

                //objectDialogTF.GetChild(0).GetComponent<Image>().color = LSColors.HexToColor("E57373");
                //objectTitle = objectDialogTF.GetChild(0).GetChild(0).GetComponent<Text>();
                //objectTitle.text = "- Base Name -";

                //objectText = objectDialogTF.Find("Text").GetComponent<Text>();

                //var scrollView2 = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View"));
                //objectDialogContent = scrollView2.transform;
                //objectDialogContent.SetParent(objectDialogTF);
                //objectDialogContent.localScale = Vector3.one;
                //scrollView.name = "Scroll View";

                //var glg2 = scrollView2.AddComponent<GridLayoutGroup>();
                //glg2.cellSize = new Vector2(124f, 64f);
                //glg2.spacing = new Vector2(4f, 4f);

                //LSHelpers.DeleteChildren(scrollView2.transform.Find("Viewport/Content"), false);

                //scrollView2.GetComponent<RectTransform>().sizeDelta = new Vector2(765f, 512f);

                #region Dropdown

                GameObject timelineZoom = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/View/View Dropdown/Grid View");
                timelineZoom.SetActive(true);
                RectTransform timelineZoomRT = timelineZoom.transform.GetChild(2).gameObject.GetComponent<RectTransform>();

                Text timelineZoomText1 = timelineZoom.transform.GetChild(0).gameObject.GetComponent<Text>();
                RectTransform timelineZoomTextRT1 = timelineZoom.transform.GetChild(0).gameObject.GetComponent<RectTransform>();

                timelineZoomText1.text = "Player Editor";
                timelineZoomText1.fontSize = 20;
                timelineZoomTextRT1.sizeDelta = new Vector2(170, 0);

                Text timelineZoomText2 = timelineZoom.transform.GetChild(1).gameObject.GetComponent<Text>();

                timelineZoomText2.text = ConfigEntries.OpenPlayerEditor.Value.ToString();
                timelineZoomText2.fontSize = 20;

                timelineZoomRT.anchoredPosition = new Vector2(-26f, 0f);
                timelineZoomRT.sizeDelta = new Vector2(22, 0);
                timelineZoom.gameObject.GetComponent<Button>().onClick.AddListener(delegate ()
                {
                    OpenDialog();
                });

                string jpgFileLocation = RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_player.png";

                if (RTFile.FileExists("BepInEx/plugins/Assets/editor_gui_player.png"))
                {
                    Image spriteReloader = timelineZoom.transform.Find("Image").GetComponent<Image>();

                    EditorManager.inst.StartCoroutine(EditorManager.inst.GetSprite(jpgFileLocation, new EditorManager.SpriteLimits(), delegate (Sprite cover)
                    {
                        spriteReloader.sprite = cover;
                    }, delegate (string errorFile)
                    {
                        spriteReloader.sprite = ArcadeManager.inst.defaultImage;
                    }));
                }
                else
                {
                    Debug.LogErrorFormat("{0}Missing editor_gui_player.png!", EditorPlugin.className);
                }

                #endregion

                Triggers.AddEditorDialog("Player Object Editor", objectDialog);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(ConfigEntries.OpenPlayerEditor.Value))
            {
                OpenDialog();
            }

            if (Input.GetKeyDown(KeyCode.F5))
            {

                var inspector = AccessTools.TypeByName("UnityExplorer.InspectorManager");
                if (inspector != null)
                {
                    inspector.GetMethod("Inspect", new[] { typeof(object), AccessTools.TypeByName("UnityExplorer.CacheObject.CacheObjectBase") }).Invoke(inspector, new object[] { objectDialog, null });
                }
            }
        }

        //Put this into RenderDialog() and change this method to be for CustomObjects instead. CustomObjects UI should be similar to BG / Checkpoint editors
        public static void RenderCustomDialog(Dictionary<string, object> _dictionary)
        {
            EditorManager.inst.ShowDialog("Player Object Editor");
            var content = objectDialogTF.Find("data/left/Object Scroll View/Viewport/Content");

            var customObjects = (Dictionary<string, object>)_dictionary["Custom Objects"];

            if (customObjects.Count > 0 && !string.IsNullOrEmpty(currentID) && customObjects.ContainsKey(currentID))
            {
                content.gameObject.SetActive(true);
                var currentObject = (Dictionary<string, object>)customObjects[currentID];

                var nameIF = content.Find("name/name").GetComponent<InputField>();
                nameIF.onValueChanged.ClearAll();
                nameIF.onEndEdit.RemoveAllListeners();
                nameIF.text = (string)currentObject["Name"];
                nameIF.onValueChanged.AddListener(delegate (string _val)
                {
                    currentObject["Name"] = _val;
                });
                nameIF.onEndEdit.AddListener(delegate (string _val)
                {
                    RenderCustomObjects(_dictionary);
                });

                //Position
                {
                    var val = (Vector2)currentObject["Position"];
                    var x = content.Find("position/x").GetComponent<InputField>();
                    x.onValueChanged.ClearAll();
                    x.text = val.x.ToString();

                    var y = content.Find("position/y").GetComponent<InputField>();
                    y.onValueChanged.ClearAll();
                    y.text = val.y.ToString();

                    x.onValueChanged.AddListener(delegate (string _val)
                    {
                        currentObject["Position"] = new Vector2(float.Parse(_val), float.Parse(y.text));

                        updatePlayers();
                    });

                    y.onValueChanged.AddListener(delegate (string _val)
                    {
                        currentObject["Position"] = new Vector2(float.Parse(x.text), float.Parse(_val));

                        updatePlayers();
                    });

                    Triggers.AddEventTrigger(x.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(x, 0.1f, 10f, true), Triggers.ScrollDeltaVector2(x, y, 0.1f, 10f) });
                    Triggers.AddEventTrigger(y.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(y, 0.1f, 10f, true), Triggers.ScrollDeltaVector2(x, y, 0.1f, 10f) });

                    Triggers.IncreaseDecreaseButtons(x, 1f, 10f);
                    Triggers.IncreaseDecreaseButtons(y, 1f, 10f);
                }

                //Scale
                {
                    var val = (Vector2)currentObject["Scale"];
                    var x = content.Find("scale/x").GetComponent<InputField>();
                    x.onValueChanged.ClearAll();
                    x.text = val.x.ToString();

                    var y = content.Find("scale/y").GetComponent<InputField>();
                    y.onValueChanged.ClearAll();
                    y.text = val.y.ToString();

                    x.onValueChanged.AddListener(delegate (string _val)
                    {
                        currentObject["Scale"] = new Vector2(float.Parse(_val), float.Parse(y.text));

                        updatePlayers();
                    });

                    y.onValueChanged.AddListener(delegate (string _val)
                    {
                        currentObject["Scale"] = new Vector2(float.Parse(x.text), float.Parse(_val));

                        updatePlayers();
                    });

                    Triggers.AddEventTrigger(x.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(x, 0.1f, 10f, true), Triggers.ScrollDeltaVector2(x, y, 0.1f, 10f) });
                    Triggers.AddEventTrigger(y.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(y, 0.1f, 10f, true), Triggers.ScrollDeltaVector2(x, y, 0.1f, 10f) });

                    Triggers.IncreaseDecreaseButtons(x, 1f, 10f);
                    Triggers.IncreaseDecreaseButtons(y, 1f, 10f);
                }

                //Rotation
                {
                    var val = (float)currentObject["Rotation"];
                    var x = content.Find("rotation/x").GetComponent<InputField>();
                    x.onValueChanged.ClearAll();
                    x.text = val.ToString();

                    x.onValueChanged.AddListener(delegate (string _val)
                    {
                        currentObject["Rotation"] = float.Parse(_val);

                        updatePlayers();
                    });

                    Triggers.AddEventTrigger(x.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(x, 15f, 3f) });

                    Triggers.IncreaseDecreaseButtons(x, 15f, 3f);
                }

                //Depth
                {
                    var val = (float)currentObject["Depth"];
                    var x = content.Find("depth/x").GetComponent<InputField>();
                    x.onValueChanged.ClearAll();
                    x.text = val.ToString();

                    x.onValueChanged.AddListener(delegate (string _val)
                    {
                        currentObject["Depth"] = float.Parse(_val);

                        updatePlayers();
                    });

                    Triggers.AddEventTrigger(x.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(x, 0.1f, 10f) });

                    Triggers.IncreaseDecreaseButtons(x, 1f, 10f);
                }

                //Color
                {
                    var x = content.Find("color/color");

                    for (int i = 0; i < 23; i++)
                    {
                        var strr = (i + 1).ToString();
                        if (i < 4)
                        {
                            x.transform.Find(strr).GetComponent<Image>().color = GameManager.inst.LiveTheme.playerColors[i];
                        }
                        if (i == 4)
                        {
                            x.transform.Find(strr).GetComponent<Image>().color = GameManager.inst.LiveTheme.guiColor;
                        }
                        if (i > 4 && i < 23)
                        {
                            int num = i - 5;
                            x.transform.Find(strr).GetComponent<Image>().color = GameManager.inst.LiveTheme.objectColors[num];
                        }
                        if (i == 23)
                        {
                            x.transform.Find(strr).GetComponent<Image>().color = GameManager.inst.LiveTheme.playerColors[0];
                        }
                        if (i == 24)
                        {
                            x.transform.Find(strr).GetComponent<Image>().color = Color.white;
                        }
                    }

                    UpdateCustomColorButtons(x, "Color", currentObject);
                }

                //Custom Color
                {
                    var x = content.Find("custom color/name").GetComponent<InputField>();
                    x.onValueChanged.ClearAll();
                    x.text = (string)currentObject["Custom Color"];
                    x.onValueChanged.AddListener(delegate (string _val)
                    {
                        if (_val.Length == 6)
                        {
                            currentObject["Custom Color"] = _val;

                            updatePlayers();
                        }
                    });
                }

                //Opacity
                {
                    var val = (float)currentObject["Opacity"];
                    var x = content.Find("opacity/x").GetComponent<InputField>();
                    x.onValueChanged.ClearAll();
                    x.text = val.ToString();

                    x.onValueChanged.AddListener(delegate (string _val)
                    {
                        var num = float.Parse(_val);
                        num = Mathf.Clamp(num, 0f, 1F);

                        currentObject["Opacity"] = num;

                        updatePlayers();
                    });

                    Triggers.AddEventTrigger(x.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(x, 0.1f, 10f, false, new List<float> { 0f, 1f }) });

                    Triggers.IncreaseDecreaseButtons(x, 1f, 10f, null, new List<float> { 0f, 1f });
                }

                //Parent
                {
                    var val = (int)currentObject["Parent"];
                    var x = content.Find("parent").GetComponent<Dropdown>();
                    x.onValueChanged.RemoveAllListeners();
                    x.value = val;
                    x.onValueChanged.AddListener(delegate (int _val)
                    {
                        currentObject["Parent"] = _val;

                        updatePlayers();
                    });
                }

                //Parent Pos Offset
                {
                    var val = (float)currentObject["Parent Position Offset"];
                    var x = content.Find("parent pos offset/x").GetComponent<InputField>();
                    x.onValueChanged.ClearAll();
                    x.text = val.ToString();

                    x.onValueChanged.AddListener(delegate (string _val)
                    {
                        var num = float.Parse(_val);
                        num = Mathf.Clamp(num, 0.001f, 1F);

                        currentObject["Parent Position Offset"] = num;

                        updatePlayers();
                    });

                    Triggers.AddEventTrigger(x.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(x, 0.1f, 10f, false, new List<float> { 0.001f, 1f }) });

                    Triggers.IncreaseDecreaseButtons(x, 1f, 10f, null, new List<float> { 0.001f, 1f });
                }

                //Parent Sca Offset
                {
                    var val = (float)currentObject["Parent Scale Offset"];
                    var x = content.Find("parent sca offset/x").GetComponent<InputField>();
                    x.onValueChanged.ClearAll();
                    x.text = val.ToString();

                    x.onValueChanged.AddListener(delegate (string _val)
                    {
                        var num = float.Parse(_val);
                        num = Mathf.Clamp(num, 0.001f, 1F);

                        currentObject["Parent Scale Offset"] = num;

                        updatePlayers();
                    });

                    Triggers.AddEventTrigger(x.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(x, 0.1f, 10f, false, new List<float> { 0.001f, 1f }) });

                    Triggers.IncreaseDecreaseButtons(x, 1f, 10f, null, new List<float> { 0.001f, 1f });
                }

                //Parent Sca Active
                {
                    var val = (bool)currentObject["Parent Scale Active"];
                    var x = content.Find("parent sca offset/p/parent sca active").GetComponent<Toggle>();
                    x.onValueChanged.RemoveAllListeners();
                    x.isOn = val;
                    x.onValueChanged.AddListener(delegate (bool _val)
                    {
                        currentObject["Parent Scale Active"] = _val;

                        updatePlayers();
                    });
                }

                //Parent Rot Offset
                {
                    var val = (float)currentObject["Parent Rotation Offset"];
                    var x = content.Find("parent rot offset/x").GetComponent<InputField>();
                    x.onValueChanged.ClearAll();
                    x.text = val.ToString();

                    x.onValueChanged.AddListener(delegate (string _val)
                    {
                        var num = float.Parse(_val);
                        num = Mathf.Clamp(num, 0.001f, 1F);

                        currentObject["Parent Rotation Offset"] = num;

                        updatePlayers();
                    });

                    Triggers.AddEventTrigger(x.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(x, 0.1f, 10f, false, new List<float> { 0.001f, 1f }) });

                    Triggers.IncreaseDecreaseButtons(x, 1f, 10f, null, new List<float> { 0.001f, 1f });
                }

                //Parent Rot Active
                {
                    var val = (bool)currentObject["Parent Rotation Active"];
                    var x = content.Find("parent rot offset/p/parent rot active").GetComponent<Toggle>();
                    x.onValueChanged.RemoveAllListeners();
                    x.isOn = val;
                    x.onValueChanged.AddListener(delegate (bool _val)
                    {
                        currentObject["Parent Rotation Active"] = _val;

                        updatePlayers();
                    });
                }

                //Shape
                {
                    var val = (Vector2Int)currentObject["Shape"];
                    //Shape
                    {
                        var shape = content.Find("shape/x").GetComponent<Dropdown>();
                        shape.options.Clear();
                        shape.onValueChanged.RemoveAllListeners();

                        shape.options = GetShapes();

                        shape.value = val.x;

                        var shapeOption = content.Find("shape option/x").GetComponent<Dropdown>();
                        shapeOption.options.Clear();
                        shapeOption.onValueChanged.RemoveAllListeners();

                        shapeOption.options = GetShapeOptions(val.x);

                        shapeOption.value = val.y;

                        shape.onValueChanged.AddListener(delegate (int _val)
                        {
                            currentObject["Shape"] = new Vector2Int(_val, 0);

                            RenderCustomDialog(_dictionary);

                            updatePlayers();
                        });

                        shapeOption.onValueChanged.AddListener(delegate (int _val)
                        {
                            currentObject["Shape"] = new Vector2Int(shape.value, _val);

                            RenderCustomDialog(_dictionary);

                            updatePlayers();
                        });
                    }
                }

                //Visibility
                {
                    var val = (int)currentObject["Visibility"];
                    var x = content.Find("visibility").GetComponent<Dropdown>();
                    x.onValueChanged.RemoveAllListeners();
                    x.value = val;
                    x.onValueChanged.AddListener(delegate (int _val)
                    {
                        currentObject["Visibility"] = _val;
                        currentObject["Visibility Value"] = 0f;

                        RenderCustomDialog(_dictionary);
                        updatePlayers();
                    });
                }

                //Visibility Health Percentage
                {
                    var val = (float)currentObject["Visibility Value"];
                    var x = content.Find("visibility health percentage/x").GetComponent<InputField>();
                    x.onValueChanged.ClearAll();
                    x.text = val.ToString();

                    x.onValueChanged.AddListener(delegate (string _val)
                    {
                        var num = float.Parse(_val);

                        currentObject["Visibility Value"] = num;

                        updatePlayers();
                    });

                    Triggers.AddEventTrigger(x.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(x, 0.1f, 10f, false, new List<float> { 0f, 100f }) });

                    Triggers.IncreaseDecreaseButtons(x, 1f, 10f, null, new List<float> { 0f, 100f });
                }

                //Visibility Not
                {
                    var val = (bool)currentObject["Visibility Not"];
                    var x = content.Find("visibility health percentage/p/visibility not").GetComponent<Toggle>();
                    x.onValueChanged.RemoveAllListeners();
                    x.isOn = val;
                    x.onValueChanged.AddListener(delegate (bool _val)
                    {
                        currentObject["Visibility Not"] = _val;

                        updatePlayers();
                    });
                }
            }
            else
                content.gameObject.SetActive(false);
        }

        public static Dictionary<string, object> dictionaryTest;

        public static void RenderCustomObjects(Dictionary<string, object> _dictionary)
        {
            var create = objectDialogTF.Find("data/right/create").GetComponent<Button>();
            create.onClick.ClearAll();
            create.onClick.AddListener(delegate ()
            {
                Debug.LogFormat("{0}Trying to create new object", EditorPlugin.className);

                playerExtensions.GetMethod("AddCustomObject").Invoke(playerExtensions, new object[] { });

                var obj = playerExtensions.GetMethod("GetPlayerModels").Invoke(playerExtensions, new object[] { });
                var currentIndex = (string)playerExtensions.GetMethod("GetPlayerModelIndex").Invoke(playerExtensions, new object[] { 0 });
                object currentModel = null;

                for (int i = 0; i < obj.GetCount(); i++)
                {
                    var values = (Dictionary<string, object>)obj.GetItem(i).GetType().GetField("values").GetValue(obj.GetItem(i));

                    if ((string)values["Base ID"] == currentIndex)
                    {
                        currentModel = obj.GetItem(i);
                    }
                }

                var dict = (Dictionary<string, object>)currentModel.GetType().GetField("values").GetValue(currentModel);
                var customObjects = (Dictionary<string, object>)dict["Custom Objects"];

                currentID = customObjects.ElementAt(customObjects.Count - 1).Key;

                updatePlayers();
                RenderCustomObjects(dict);
                RenderCustomDialog(dict);
            });

            var search = objectDialogTF.Find("data/right/search").GetComponent<InputField>();
            search.onValueChanged.ClearAll();
            search.text = customSearchTerm;
            search.onValueChanged.AddListener(delegate (string _val)
            {
                customSearchTerm = _val;
                RenderCustomObjects(_dictionary);
            });

            LSHelpers.DeleteChildren(objectDialogContent);

            var close = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("Panel/x");

            var customObjects = (Dictionary<string, object>)_dictionary["Custom Objects"];
            foreach (var custom in customObjects)
            {
                var name = (string)((Dictionary<string, object>)custom.Value)["Name"];
                if (string.IsNullOrEmpty(customSearchTerm) || name.ToLower().Contains(customSearchTerm.ToLower()))
                {
                    string tmpID = custom.Key;

                    var gameObject = Instantiate(customObjectButtonPrefab);
                    gameObject.transform.SetParent(objectDialogContent);
                    gameObject.transform.localScale = Vector3.one;
                    gameObject.name = custom.Key;

                    gameObject.transform.Find("name").GetComponent<Text>().text = name;

                    var button = gameObject.GetComponent<Button>();
                    button.onClick.ClearAll();
                    button.onClick.AddListener(delegate ()
                    {
                        currentID = tmpID;
                        RenderCustomDialog(_dictionary);
                        RenderCustomObjects(_dictionary);
                    });

                    var dot = gameObject.transform.Find("dot").GetComponent<Image>();
                    if (tmpID == currentID)
                        dot.enabled = true;
                    else
                        dot.enabled = false;

                    Destroy(gameObject.transform.Find("time").gameObject);

                    var delete = Instantiate(close.gameObject);
                    var deleteTF = delete.transform;
                    deleteTF.SetParent(gameObject.transform);
                    deleteTF.localScale = Vector3.one;
                    delete.name = "delete";

                    delete.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 0f);

                    var deleteButton = delete.GetComponent<Button>();
                    deleteButton.onClick.ClearAll();
                    deleteButton.onClick.AddListener(delegate ()
                    {
                        playerExtensions.GetMethod("RemoveCustomObject").Invoke(playerExtensions, new object[] { tmpID });

                        var obj = playerExtensions.GetMethod("GetPlayerModels").Invoke(playerExtensions, new object[] { });
                        var currentIndex = (string)playerExtensions.GetMethod("GetPlayerModelIndex").Invoke(playerExtensions, new object[] { 0 });
                        object currentModel = null;

                        for (int i = 0; i < obj.GetCount(); i++)
                        {
                            var values = (Dictionary<string, object>)obj.GetItem(i).GetType().GetField("values").GetValue(obj.GetItem(i));

                            if ((string)values["Base ID"] == currentIndex)
                            {
                                currentModel = obj.GetItem(i);
                            }
                        }

                        var dict = (Dictionary<string, object>)currentModel.GetType().GetField("values").GetValue(currentModel);

                        updatePlayers();
                        RenderCustomObjects(dict);
                        RenderCustomDialog(dict);
                    });

                    var le = delete.AddComponent<LayoutElement>();
                    le.preferredWidth = 28f;
                }
            }
        }

        public static void OpenDialog()
        {
            if (inst == null)
                return;

            Debug.LogFormat("{0}Attempting to open Player Editor!", EditorPlugin.className);
            EditorManager.inst.ShowDialog("Player Editor");
            inst.StartCoroutine(RenderDialog());
        }

        public static IEnumerator RenderDialog()
        {
            playerModelDropdown.options.Clear();

            if (playerExtensions != null)
            {
                var obj = playerExtensions.GetMethod("GetPlayerModels").Invoke(playerExtensions, new object[] { });
                var currentIndex = (string)playerExtensions.GetMethod("GetPlayerModelIndex").Invoke(playerExtensions, new object[] { 0 });
                object currentModel = null;

                playerModelDropdown.onValueChanged.RemoveAllListeners();

                for (int i = 0; i < obj.GetCount(); i++)
                {
                    var values = (Dictionary<string, object>)obj.GetItem(i).GetType().GetField("values").GetValue(obj.GetItem(i));
                    playerModelDropdown.options.Add(new Dropdown.OptionData((string)values["Base Name"]));

                    if ((string)values["Base ID"] == currentIndex)
                    {
                        currentModel = obj.GetItem(i);
                    }
                }

                int c = (int)playerExtensions.GetMethod("GetPlayerModelInt").Invoke(playerExtensions, new object[] { currentModel });
                playerModelDropdown.value = c;
                playerModelDropdown.onValueChanged.AddListener(delegate (int _val)
                {
                    Debug.LogFormat("{0}Setting Player 1's model index to {1}", EditorPlugin.className, _val);
                    playerExtensions.GetMethod("SetPlayerModelIndex").Invoke(playerExtensions, new object[] { 0, _val });
                    inst.StartCoroutine(RenderDialog());

                    playerPlugin.GetMethod("StartRespawnPlayers").Invoke(playerPlugin, new object[] { });
                });

                #region UI Elements

                var label = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content").transform.GetChild(2).gameObject;
                var singleInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move/position/x");
                var vector2Input = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move/position");
                var boolInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/toggle/toggle");
                var dropdownInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/autokill/tod-dropdown");
                var sliderFullInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/bpm");
                var stringInput = GameObject.Find("TimelineBar/GameObject/Time Input");
                var colorsInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/color/color");

                #endregion

                LSHelpers.DeleteChildren(editorDialogContent.Find("Viewport/Content"));

                var dict = (Dictionary<string, object>)currentModel.GetType().GetField("values").GetValue(currentModel);
                var id = (string)dict["Base ID"];

                if (id != "0" && id != "1")
                {
                    foreach (var objects in (Dictionary<string, object>)currentModel.GetType().GetField("values").GetValue(currentModel))
                    {
                        var key = objects.Key;

                        if (key != "Base ID" && (string.IsNullOrEmpty(searchTerm) || key.ToLower().Contains(searchTerm.ToLower())))
                        {
                            //String
                            if (key == "Base Name" || key.Contains("Color") && key.Contains("Custom"))
                            {
                                var bar = Instantiate(singleInput);
                                Destroy(bar.GetComponent<InputField>());
                                Destroy(bar.GetComponent<EventInfo>());

                                if (bar.GetComponent<EventTrigger>())
                                    Destroy(bar.GetComponent<EventTrigger>());

                                LSHelpers.DeleteChildren(bar.transform);
                                bar.transform.SetParent(editorDialogContent.Find("Viewport/Content"));
                                bar.transform.localScale = Vector3.one;
                                bar.name = "input [STRING]";

                                Triggers.AddTooltip(bar, key, "");

                                var l = Instantiate(label);
                                l.transform.SetParent(bar.transform);
                                l.transform.SetAsFirstSibling();
                                l.transform.localScale = Vector3.one;
                                l.transform.GetChild(0).GetComponent<Text>().text = key;
                                l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(354f, 20f);

                                var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
                                {
                                    ltextrt.anchoredPosition = new Vector2(10f, -5f);
                                }

                                bar.GetComponent<Image>().enabled = true;
                                bar.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);

                                GameObject x = Instantiate(stringInput);
                                x.transform.SetParent(bar.transform);
                                x.transform.localScale = Vector3.one;
                                Destroy(x.GetComponent<HoverTooltip>());

                                x.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(366f, 32f);

                                var xif = x.GetComponent<InputField>();
                                xif.onValueChanged.RemoveAllListeners();
                                xif.characterValidation = InputField.CharacterValidation.None;
                                xif.characterLimit = 0;
                                if (key.Contains("Color") && key.Contains("Custom"))
                                    xif.characterLimit = 8;
                                xif.text = (string)objects.Value;
                                xif.textComponent.fontSize = 18;
                                xif.onValueChanged.AddListener(delegate (string _val)
                                {
                                    if (!(key.Contains("Color") && key.Contains("Custom")))
                                        ((Dictionary<string, object>)currentModel.GetType().GetField("values").GetValue(currentModel))[key] = _val;
                                    else
                                    {
                                        if (_val.Length == 6 || _val.Length == 8)
                                        {
                                            ((Dictionary<string, object>)currentModel.GetType().GetField("values").GetValue(currentModel))[key] = _val;
                                        }
                                        else
                                        {
                                            ((Dictionary<string, object>)currentModel.GetType().GetField("values").GetValue(currentModel))[key] = LSColors.ColorToHex(LSColors.pink500);
                                        }
                                    }
                                });
                                xif.onEndEdit.RemoveAllListeners();
                                xif.onEndEdit.AddListener(delegate (string _val)
                                {
                                    inst.StartCoroutine(RenderDialog());
                                });
                            }

                            //Integer
                            if (key == "Base Health" || key == "Boost Particles Amount")
                            {
                                var bar = Instantiate(singleInput);
                                Destroy(bar.GetComponent<InputField>());
                                Destroy(bar.GetComponent<EventInfo>());
                                if (bar.GetComponent<EventTrigger>())
                                    Destroy(bar.GetComponent<EventTrigger>());

                                LSHelpers.DeleteChildren(bar.transform);
                                bar.transform.SetParent(editorDialogContent.Find("Viewport/Content"));
                                bar.transform.localScale = Vector3.one;
                                bar.name = "input [INT]";

                                Triggers.AddTooltip(bar, key, "");

                                var l = Instantiate(label);
                                l.transform.SetParent(bar.transform);
                                l.transform.SetAsFirstSibling();
                                l.transform.localScale = Vector3.one;
                                l.transform.GetChild(0).GetComponent<Text>().text = key;
                                l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(354f, 20f);

                                var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
                                {
                                    ltextrt.anchoredPosition = new Vector2(10f, -5f);
                                }

                                bar.GetComponent<Image>().enabled = true;
                                bar.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);

                                GameObject vector2 = Instantiate(vector2Input);
                                vector2.transform.SetParent(bar.transform);
                                vector2.transform.localScale = Vector3.one;

                                var vtmp = (int)objects.Value;

                                Destroy(vector2.transform.Find("x").GetComponent<EventInfo>());
                                vector2.transform.Find("x").localScale = Vector3.one;
                                vector2.transform.Find("x").GetChild(0).localScale = Vector3.one;
                                var vxif = vector2.transform.Find("x").GetComponent<InputField>();
                                {
                                    vxif.onValueChanged.RemoveAllListeners();
                                    vxif.characterValidation = InputField.CharacterValidation.Integer;
                                    vxif.text = vtmp.ToString();

                                    vxif.onValueChanged.AddListener(delegate (string _val)
                                    {
                                        vtmp = int.Parse(_val);
                                        ((Dictionary<string, object>)currentModel.GetType().GetField("values").GetValue(currentModel))[key] = vtmp;
                                        updatePlayers();
                                    });
                                }

                                var et = vector2.transform.Find("x").GetComponent<EventTrigger>();
                                et.triggers.Clear();
                                et.triggers.Add(Triggers.ScrollDeltaInt(vxif, 1, false, new List<int> { 1, 100 }));

                                Triggers.IncreaseDecreaseButtonsInt(vxif, 1);

                                Destroy(vector2.transform.Find("y").gameObject);
                            }

                            //Shape Dropdowns
                            if (key.Contains("Shape"))
                            {
                                //Shape
                                {
                                    var bar = Instantiate(singleInput);
                                    if (bar.GetComponent<InputField>())
                                        Destroy(bar.GetComponent<InputField>());
                                    if (bar.GetComponent<EventInfo>())
                                        Destroy(bar.GetComponent<EventInfo>());
                                    if (bar.GetComponent<EventTrigger>())
                                        Destroy(bar.GetComponent<EventTrigger>());

                                    LSHelpers.DeleteChildren(bar.transform);
                                    bar.transform.SetParent(editorDialogContent.Find("Viewport/Content"));
                                    bar.transform.localScale = Vector3.one;
                                    bar.name = "input [ENUM]";

                                    Triggers.AddTooltip(bar, key, "");

                                    var l = Instantiate(label);
                                    l.transform.SetParent(bar.transform);
                                    l.transform.SetAsFirstSibling();
                                    l.transform.localScale = Vector3.one;
                                    l.transform.GetChild(0).GetComponent<Text>().text = key;
                                    l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(354f, 20f);

                                    var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
                                    {
                                        ltextrt.anchoredPosition = new Vector2(10f, -5f);
                                    }

                                    bar.GetComponent<Image>().enabled = true;
                                    bar.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);

                                    GameObject x = Instantiate(dropdownInput);
                                    x.transform.SetParent(bar.transform);
                                    x.transform.localScale = Vector3.one;

                                    RectTransform xRT = x.GetComponent<RectTransform>();
                                    xRT.anchoredPosition = new Vector2(624f, -16f);
                                    xRT.sizeDelta = new Vector2(366f, 32f);

                                    Destroy(x.GetComponent<HoverTooltip>());

                                    Dropdown dropdown = x.GetComponent<Dropdown>();
                                    dropdown.options.Clear();
                                    dropdown.onValueChanged.RemoveAllListeners();

                                    dropdown.options = GetShapes();

                                    var hide = x.AddComponent<HideDropdownOptions>();
                                    hide.DisabledOptions = new List<bool>
                                {
                                    false,
                                    false,
                                    false,
                                    false,
                                    true,
                                    false,
                                    true,
                                    false,
                                    false,
                                };

                                    dropdown.value = ((Vector2Int)objects.Value).x;

                                    var v = (Vector2Int)objects.Value;

                                    dropdown.onValueChanged.AddListener(delegate (int _val)
                                    {
                                        ((Dictionary<string, object>)currentModel.GetType().GetField("values").GetValue(currentModel))[key] = new Vector2Int(_val, 0);
                                        inst.StartCoroutine(RenderDialog());
                                        updatePlayers();
                                    });
                                }

                                //Shape Option
                                {
                                    var bar = Instantiate(singleInput);
                                    if (bar.GetComponent<InputField>())
                                        Destroy(bar.GetComponent<InputField>());
                                    if (bar.GetComponent<EventInfo>())
                                        Destroy(bar.GetComponent<EventInfo>());
                                    if (bar.GetComponent<EventTrigger>())
                                        Destroy(bar.GetComponent<EventTrigger>());

                                    LSHelpers.DeleteChildren(bar.transform);
                                    bar.transform.SetParent(editorDialogContent.Find("Viewport/Content"));
                                    bar.transform.localScale = Vector3.one;
                                    bar.name = "input [ENUM]";

                                    Triggers.AddTooltip(bar, key, "");

                                    var l = Instantiate(label);
                                    l.transform.SetParent(bar.transform);
                                    l.transform.SetAsFirstSibling();
                                    l.transform.localScale = Vector3.one;
                                    l.transform.GetChild(0).GetComponent<Text>().text = key + " Option";
                                    l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(354f, 20f);

                                    var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
                                    {
                                        ltextrt.anchoredPosition = new Vector2(10f, -5f);
                                    }

                                    bar.GetComponent<Image>().enabled = true;
                                    bar.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);

                                    GameObject x = Instantiate(dropdownInput);
                                    x.transform.SetParent(bar.transform);
                                    x.transform.localScale = Vector3.one;

                                    RectTransform xRT = x.GetComponent<RectTransform>();
                                    xRT.anchoredPosition = new Vector2(624f, -16f);
                                    xRT.sizeDelta = new Vector2(366f, 32f);

                                    Destroy(x.GetComponent<HoverTooltip>());
                                    Destroy(x.GetComponent<HideDropdownOptions>());

                                    Dropdown dropdown = x.GetComponent<Dropdown>();
                                    dropdown.options.Clear();
                                    dropdown.onValueChanged.RemoveAllListeners();

                                    dropdown.options = GetShapeOptions(((Vector2Int)objects.Value).x);

                                    dropdown.value = ((Vector2Int)objects.Value).y;

                                    var v = (Vector2Int)objects.Value;

                                    dropdown.onValueChanged.AddListener(delegate (int _val)
                                    {
                                        ((Dictionary<string, object>)currentModel.GetType().GetField("values").GetValue(currentModel))[key] = new Vector2Int(v.x, _val);
                                        updatePlayers();
                                    });
                                }
                            }

                            //Vector2
                            if (key.Contains("Position") && !key.Contains("Easing") || key.Contains("Scale") && !key.Contains("Particles") && !key.Contains("Easing") || key.Contains("Force"))
                            {
                                var bar = Instantiate(singleInput);
                                Destroy(bar.GetComponent<InputField>());
                                Destroy(bar.GetComponent<EventInfo>());
                                if (bar.GetComponent<EventTrigger>())
                                    Destroy(bar.GetComponent<EventTrigger>());

                                LSHelpers.DeleteChildren(bar.transform);
                                bar.transform.SetParent(editorDialogContent.Find("Viewport/Content"));
                                bar.transform.localScale = Vector3.one;
                                bar.name = "input [VECTOR2]";

                                Triggers.AddTooltip(bar, key, "");

                                var l = Instantiate(label);
                                l.transform.SetParent(bar.transform);
                                l.transform.SetAsFirstSibling();
                                l.transform.localScale = Vector3.one;
                                l.transform.GetChild(0).GetComponent<Text>().text = key;
                                l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(354f, 20f);

                                var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
                                {
                                    ltextrt.anchoredPosition = new Vector2(10f, -5f);
                                }

                                bar.GetComponent<Image>().enabled = true;
                                bar.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);

                                GameObject vector2 = Instantiate(vector2Input);
                                vector2.transform.SetParent(bar.transform);
                                vector2.transform.localScale = Vector3.one;

                                Vector2 vtmp = (Vector2)objects.Value;

                                Destroy(vector2.transform.Find("x").GetComponent<EventInfo>());
                                vector2.transform.Find("x").localScale = Vector3.one;
                                vector2.transform.Find("x").GetChild(0).localScale = Vector3.one;
                                var vxif = vector2.transform.Find("x").GetComponent<InputField>();
                                {
                                    vxif.onValueChanged.RemoveAllListeners();

                                    vxif.text = vtmp.x.ToString();

                                    vxif.onValueChanged.AddListener(delegate (string _val)
                                    {
                                        vtmp = new Vector2(float.Parse(_val), vtmp.y);
                                        ((Dictionary<string, object>)currentModel.GetType().GetField("values").GetValue(currentModel))[key] = vtmp;
                                        updatePlayers();
                                    });
                                }

                                Destroy(vector2.transform.Find("y").GetComponent<EventInfo>());
                                vector2.transform.Find("y").localScale = Vector3.one;
                                vector2.transform.Find("y").GetChild(0).localScale = Vector3.one;
                                var vyif = vector2.transform.Find("y").GetComponent<InputField>();
                                {
                                    vyif.onValueChanged.RemoveAllListeners();

                                    vyif.text = vtmp.y.ToString();

                                    vyif.onValueChanged.AddListener(delegate (string _val)
                                    {
                                        vtmp = new Vector2(vtmp.x, float.Parse(_val));
                                        ((Dictionary<string, object>)currentModel.GetType().GetField("values").GetValue(currentModel))[key] = vtmp;
                                        updatePlayers();
                                    });
                                }

                                var etX = vector2.transform.Find("x").GetComponent<EventTrigger>();
                                etX.triggers.Clear();
                                etX.triggers.Add(Triggers.ScrollDelta(vxif, 0.1f, 10f, true));
                                etX.triggers.Add(Triggers.ScrollDeltaVector2(vxif, vyif, 0.1f, 10f));

                                var etY = vector2.transform.Find("y").GetComponent<EventTrigger>();
                                etY.triggers.Clear();
                                etY.triggers.Add(Triggers.ScrollDelta(vyif, 0.1f, 10f, true));
                                etY.triggers.Add(Triggers.ScrollDeltaVector2(vxif, vyif, 0.1f, 10f));

                                Triggers.IncreaseDecreaseButtons(vxif, 1f, 10f);
                                Triggers.IncreaseDecreaseButtons(vyif, 1f, 10f);
                            }

                            //Single
                            if (key.Contains("Scale") && (key.Contains("Start") || key.Contains("End")) && !key.Contains("Pulse") ||
                                key.Contains("Rotation") && !key.Contains("Easing") ||
                                key == "Tail Base Distance" ||
                                key.Contains("Opacity") && !key.Contains("Easing") ||
                                key.Contains("Lifetime") ||
                                key.Contains("Start Width") ||
                                key.Contains("End Width") ||
                                key.Contains("Trail Time") ||
                                key == "Boost Particles Duration" ||
                                key.Contains("Amount") && !key.Contains("Boost") ||
                                key.Contains("Speed") || key.Contains("Depth") || key == "Pulse Duration" ||
                                key.Contains("Cooldown") || key.Contains("Boost Time"))
                            {
                                var bar = Instantiate(singleInput);
                                Destroy(bar.GetComponent<InputField>());
                                Destroy(bar.GetComponent<EventInfo>());
                                if (bar.GetComponent<EventTrigger>())
                                    Destroy(bar.GetComponent<EventTrigger>());

                                LSHelpers.DeleteChildren(bar.transform);
                                bar.transform.SetParent(editorDialogContent.Find("Viewport/Content"));
                                bar.transform.localScale = Vector3.one;
                                bar.name = "input [FLOAT]";

                                Triggers.AddTooltip(bar, key, "");

                                var l = Instantiate(label);
                                l.transform.SetParent(bar.transform);
                                l.transform.SetAsFirstSibling();
                                l.transform.localScale = Vector3.one;
                                l.transform.GetChild(0).GetComponent<Text>().text = key;
                                l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(354f, 20f);

                                var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
                                {
                                    ltextrt.anchoredPosition = new Vector2(10f, -5f);
                                }

                                bar.GetComponent<Image>().enabled = true;
                                bar.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);

                                GameObject vector2 = Instantiate(vector2Input);
                                vector2.transform.SetParent(bar.transform);
                                vector2.transform.localScale = Vector3.one;

                                var vtmp = (float)objects.Value;

                                Destroy(vector2.transform.Find("x").GetComponent<EventInfo>());
                                vector2.transform.Find("x").localScale = Vector3.one;
                                vector2.transform.Find("x").GetChild(0).localScale = Vector3.one;
                                var vxif = vector2.transform.Find("x").GetComponent<InputField>();
                                {
                                    vxif.onValueChanged.RemoveAllListeners();

                                    vxif.text = vtmp.ToString();

                                    vxif.onValueChanged.AddListener(delegate (string _val)
                                    {
                                        vtmp = float.Parse(_val);
                                        ((Dictionary<string, object>)currentModel.GetType().GetField("values").GetValue(currentModel))[key] = vtmp;
                                        updatePlayers();
                                    });
                                }

                                var etX = vector2.transform.Find("x").GetComponent<EventTrigger>();
                                etX.triggers.Clear();
                                etX.triggers.Add(Triggers.ScrollDelta(vxif, 0.1f, 10f, false));

                                Triggers.IncreaseDecreaseButtons(vxif, 1f, 10f);

                                Destroy(vector2.transform.Find("y").gameObject);
                            }

                            //Tail Base Mode Dropdown
                            if (key == "Tail Base Mode")
                            {
                                var bar = Instantiate(singleInput);
                                if (bar.GetComponent<InputField>())
                                    Destroy(bar.GetComponent<InputField>());
                                if (bar.GetComponent<EventInfo>())
                                    Destroy(bar.GetComponent<EventInfo>());
                                if (bar.GetComponent<EventTrigger>())
                                    Destroy(bar.GetComponent<EventTrigger>());

                                LSHelpers.DeleteChildren(bar.transform);
                                bar.transform.SetParent(editorDialogContent.Find("Viewport/Content"));
                                bar.transform.localScale = Vector3.one;
                                bar.name = "input [ENUM]";

                                Triggers.AddTooltip(bar, key, "");

                                var l = Instantiate(label);
                                l.transform.SetParent(bar.transform);
                                l.transform.SetAsFirstSibling();
                                l.transform.localScale = Vector3.one;
                                l.transform.GetChild(0).GetComponent<Text>().text = key;
                                l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(354f, 20f);

                                var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
                                {
                                    ltextrt.anchoredPosition = new Vector2(10f, -5f);
                                }

                                bar.GetComponent<Image>().enabled = true;
                                bar.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);

                                GameObject x = Instantiate(dropdownInput);
                                x.transform.SetParent(bar.transform);
                                x.transform.localScale = Vector3.one;

                                RectTransform xRT = x.GetComponent<RectTransform>();
                                xRT.anchoredPosition = new Vector2(624f, -16f);
                                xRT.sizeDelta = new Vector2(366f, 32f);

                                Destroy(x.GetComponent<HoverTooltip>());

                                Dropdown dropdown = x.GetComponent<Dropdown>();
                                dropdown.options.Clear();
                                dropdown.onValueChanged.RemoveAllListeners();

                                dropdown.options = new List<Dropdown.OptionData>
                            {
                                new Dropdown.OptionData("Legacy"),
                                new Dropdown.OptionData("Dev+")
                            };

                                dropdown.value = (int)objects.Value;

                                dropdown.onValueChanged.AddListener(delegate (int _val)
                                {
                                    ((Dictionary<string, object>)currentModel.GetType().GetField("values").GetValue(currentModel))[key] = _val;
                                    updatePlayers();
                                });
                            }

                            //Base Rotate Mode
                            if (key == "Base Rotate Mode")
                            {
                                var bar = Instantiate(singleInput);
                                if (bar.GetComponent<InputField>())
                                    Destroy(bar.GetComponent<InputField>());
                                if (bar.GetComponent<EventInfo>())
                                    Destroy(bar.GetComponent<EventInfo>());
                                if (bar.GetComponent<EventTrigger>())
                                    Destroy(bar.GetComponent<EventTrigger>());

                                LSHelpers.DeleteChildren(bar.transform);
                                bar.transform.SetParent(editorDialogContent.Find("Viewport/Content"));
                                bar.transform.localScale = Vector3.one;
                                bar.name = "input [ENUM]";

                                Triggers.AddTooltip(bar, key, "");

                                var l = Instantiate(label);
                                l.transform.SetParent(bar.transform);
                                l.transform.SetAsFirstSibling();
                                l.transform.localScale = Vector3.one;
                                l.transform.GetChild(0).GetComponent<Text>().text = key;
                                l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(354f, 20f);

                                var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
                                {
                                    ltextrt.anchoredPosition = new Vector2(10f, -5f);
                                }

                                bar.GetComponent<Image>().enabled = true;
                                bar.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);

                                GameObject x = Instantiate(dropdownInput);
                                x.transform.SetParent(bar.transform);
                                x.transform.localScale = Vector3.one;

                                RectTransform xRT = x.GetComponent<RectTransform>();
                                xRT.anchoredPosition = new Vector2(624f, -16f);
                                xRT.sizeDelta = new Vector2(366f, 32f);

                                Destroy(x.GetComponent<HoverTooltip>());

                                Dropdown dropdown = x.GetComponent<Dropdown>();
                                dropdown.options.Clear();
                                dropdown.onValueChanged.RemoveAllListeners();

                                dropdown.options = new List<Dropdown.OptionData>
                            {
                                new Dropdown.OptionData("Face Direction"),
                                new Dropdown.OptionData("None"),
                                new Dropdown.OptionData("Flip X"),
                                new Dropdown.OptionData("Flip Y")
                            };

                                dropdown.value = (int)objects.Value;

                                dropdown.onValueChanged.AddListener(delegate (int _val)
                                {
                                    ((Dictionary<string, object>)currentModel.GetType().GetField("values").GetValue(currentModel))[key] = _val;
                                    updatePlayers();
                                });
                            }
                            
                            //GUI Health Mode
                            if (key == "GUI Health Mode")
                            {
                                var bar = Instantiate(singleInput);
                                if (bar.GetComponent<InputField>())
                                    Destroy(bar.GetComponent<InputField>());
                                if (bar.GetComponent<EventInfo>())
                                    Destroy(bar.GetComponent<EventInfo>());
                                if (bar.GetComponent<EventTrigger>())
                                    Destroy(bar.GetComponent<EventTrigger>());

                                LSHelpers.DeleteChildren(bar.transform);
                                bar.transform.SetParent(editorDialogContent.Find("Viewport/Content"));
                                bar.transform.localScale = Vector3.one;
                                bar.name = "input [ENUM]";

                                Triggers.AddTooltip(bar, key, "");

                                var l = Instantiate(label);
                                l.transform.SetParent(bar.transform);
                                l.transform.SetAsFirstSibling();
                                l.transform.localScale = Vector3.one;
                                l.transform.GetChild(0).GetComponent<Text>().text = key;
                                l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(354f, 20f);

                                var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
                                {
                                    ltextrt.anchoredPosition = new Vector2(10f, -5f);
                                }

                                bar.GetComponent<Image>().enabled = true;
                                bar.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);

                                GameObject x = Instantiate(dropdownInput);
                                x.transform.SetParent(bar.transform);
                                x.transform.localScale = Vector3.one;

                                RectTransform xRT = x.GetComponent<RectTransform>();
                                xRT.anchoredPosition = new Vector2(624f, -16f);
                                xRT.sizeDelta = new Vector2(366f, 32f);

                                Destroy(x.GetComponent<HoverTooltip>());

                                Dropdown dropdown = x.GetComponent<Dropdown>();
                                dropdown.options.Clear();
                                dropdown.onValueChanged.RemoveAllListeners();

                                dropdown.options = new List<Dropdown.OptionData>
                                {
                                    new Dropdown.OptionData("Images"),
                                    new Dropdown.OptionData("Text"),
                                    new Dropdown.OptionData("Equals Bar"),
                                    new Dropdown.OptionData("Bar"),
                                };

                                dropdown.value = (int)objects.Value;

                                dropdown.onValueChanged.AddListener(delegate (int _val)
                                {
                                    ((Dictionary<string, object>)currentModel.GetType().GetField("values").GetValue(currentModel))[key] = _val;
                                    updatePlayers();
                                });
                            }

                            //Easing Dropdown
                            if (key.Contains("Easing"))
                            {
                                var bar = Instantiate(singleInput);
                                if (bar.GetComponent<InputField>())
                                    Destroy(bar.GetComponent<InputField>());
                                if (bar.GetComponent<EventInfo>())
                                    Destroy(bar.GetComponent<EventInfo>());
                                if (bar.GetComponent<EventTrigger>())
                                    Destroy(bar.GetComponent<EventTrigger>());

                                LSHelpers.DeleteChildren(bar.transform);
                                bar.transform.SetParent(editorDialogContent.Find("Viewport/Content"));
                                bar.transform.localScale = Vector3.one;
                                bar.name = "input [ENUM]";

                                Triggers.AddTooltip(bar, key, "");

                                var l = Instantiate(label);
                                l.transform.SetParent(bar.transform);
                                l.transform.SetAsFirstSibling();
                                l.transform.localScale = Vector3.one;
                                l.transform.GetChild(0).GetComponent<Text>().text = key;
                                l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(354f, 20f);

                                var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
                                {
                                    ltextrt.anchoredPosition = new Vector2(10f, -5f);
                                }

                                bar.GetComponent<Image>().enabled = true;
                                bar.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);

                                GameObject x = Instantiate(dropdownInput);
                                x.transform.SetParent(bar.transform);
                                x.transform.localScale = Vector3.one;

                                RectTransform xRT = x.GetComponent<RectTransform>();
                                xRT.anchoredPosition = new Vector2(624f, -16f);
                                xRT.sizeDelta = new Vector2(366f, 32f);

                                Destroy(x.GetComponent<HoverTooltip>());

                                Dropdown dropdown = x.GetComponent<Dropdown>();
                                dropdown.options.Clear();
                                dropdown.onValueChanged.RemoveAllListeners();

                                foreach (var anim in DataManager.inst.AnimationList)
                                {
                                    dropdown.options.Add(new Dropdown.OptionData(anim.Name));
                                }

                                dropdown.value = (int)objects.Value;

                                dropdown.onValueChanged.AddListener(delegate (int _val)
                                {
                                    ((Dictionary<string, object>)currentModel.GetType().GetField("values").GetValue(currentModel))[key] = _val;
                                    updatePlayers();
                                });
                            }

                            //Color
                            if (key.Contains("Color") && !key.Contains("Easing") && !key.Contains("Custom"))
                            {
                                var bar = Instantiate(singleInput);
                                Destroy(bar.GetComponent<InputField>());
                                Destroy(bar.GetComponent<EventInfo>());

                                if (bar.GetComponent<EventTrigger>())
                                    Destroy(bar.GetComponent<EventTrigger>());

                                LSHelpers.DeleteChildren(bar.transform);
                                bar.transform.SetParent(editorDialogContent.Find("Viewport/Content"));
                                bar.transform.localScale = Vector3.one;
                                bar.name = "input [COLOR]";
                                bar.GetComponent<RectTransform>().sizeDelta = new Vector2(750f, 116f);

                                Triggers.AddTooltip(bar, key, "");

                                var l = Instantiate(label);
                                l.transform.SetParent(bar.transform);
                                l.transform.SetAsFirstSibling();
                                l.transform.localScale = Vector3.one;
                                l.transform.GetChild(0).GetComponent<Text>().text = key;
                                l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(354f, 20f);

                                var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
                                {
                                    ltextrt.anchoredPosition = new Vector2(10f, -5f);
                                }

                                bar.GetComponent<Image>().enabled = true;
                                bar.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);

                                GameObject x = Instantiate(colorsInput);
                                x.transform.SetParent(bar.transform);
                                x.transform.localScale = Vector3.one;
                                if (x.GetComponent<HoverTooltip>())
                                    Destroy(x.GetComponent<HoverTooltip>());

                                for (int i = 19; i < 26; i++)
                                {
                                    var colorButton = Instantiate(x.transform.GetChild(0).gameObject);
                                    colorButton.name = i.ToString();
                                    colorButton.transform.SetParent(x.transform);
                                    colorButton.transform.localScale = Vector3.one;
                                }

                                for (int i = 0; i < 25; i++)
                                {
                                    var strr = (i + 1).ToString();
                                    if (i < 4)
                                    {
                                        x.transform.Find(strr).GetComponent<Image>().color = GameManager.inst.LiveTheme.playerColors[i];
                                    }
                                    if (i == 4)
                                    {
                                        x.transform.Find(strr).GetComponent<Image>().color = GameManager.inst.LiveTheme.guiColor;
                                    }
                                    if (i > 4 && i < 23)
                                    {
                                        int num = i - 5;
                                        x.transform.Find(strr).GetComponent<Image>().color = GameManager.inst.LiveTheme.objectColors[num];
                                    }
                                    if (i == 23)
                                    {
                                        x.transform.Find(strr).GetComponent<Image>().color = GameManager.inst.LiveTheme.playerColors[0];
                                        Triggers.AddTooltip(x.transform.Find(strr).gameObject, "Current Player Color", "This represents the color the player would normally always use. For example: Player One uses color 1, Player Two uses color 2, etc.");
                                    }
                                    if (i == 24)
                                    {
                                        Color col = Color.white;

                                        if (dict.ContainsKey(key.Replace("Color", "Custom Color")))
                                        {
                                            col = LSColors.HexToColor((string)((Dictionary<string, object>)currentModel.GetType().GetField("values").GetValue(currentModel))[key.Replace("Color", "Custom Color")]);
                                        }

                                        x.transform.Find(strr).GetComponent<Image>().color = col;
                                        Triggers.AddTooltip(x.transform.Find(strr).gameObject, "Custom Color", "Uses a custom hex code set by you. Do remember this will break levels that use themes heavily such as fade black/white themes.");
                                    }
                                }

                                UpdateColorButtons(x.transform, key, (Dictionary<string, object>)currentModel.GetType().GetField("values").GetValue(currentModel));
                            }

                            //Bool
                            if (key.Contains("Active") || key.Contains("Emitting") || key == "Pulse Rotate to Head" || key == "Tail Base Grows" || key == "Base Collision Accurate")
                            {
                                var bar = Instantiate(singleInput);
                                Destroy(bar.GetComponent<InputField>());
                                Destroy(bar.GetComponent<EventInfo>());
                                if (bar.GetComponent<EventTrigger>())
                                    Destroy(bar.GetComponent<EventTrigger>());

                                LSHelpers.DeleteChildren(bar.transform);
                                bar.transform.SetParent(editorDialogContent.Find("Viewport/Content"));
                                bar.transform.localScale = Vector3.one;
                                bar.name = "input [BOOL]";

                                Triggers.AddTooltip(bar, key, "");

                                var l = Instantiate(label);
                                l.transform.SetParent(bar.transform);
                                l.transform.SetAsFirstSibling();
                                l.transform.localScale = Vector3.one;
                                l.transform.GetChild(0).GetComponent<Text>().text = key;
                                l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(688f, 20f);

                                var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
                                {
                                    ltextrt.anchoredPosition = new Vector2(10f, -5f);
                                }

                                bar.GetComponent<Image>().enabled = true;
                                bar.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);

                                GameObject x = Instantiate(boolInput);
                                x.transform.SetParent(bar.transform);
                                x.transform.localScale = Vector3.one;

                                Toggle xt = x.GetComponent<Toggle>();
                                xt.onValueChanged.RemoveAllListeners();
                                xt.isOn = (bool)objects.Value;
                                xt.onValueChanged.AddListener(delegate (bool _val)
                                {
                                    ((Dictionary<string, object>)currentModel.GetType().GetField("values").GetValue(currentModel))[key] = _val;
                                    updatePlayers();
                                });
                            }

                            //Button
                            if (key == "Custom Objects")
                            {
                                var bar = Instantiate(singleInput);
                                DestroyImmediate(bar.GetComponent<InputField>());
                                DestroyImmediate(bar.GetComponent<InputFieldHelper>());
                                Destroy(bar.GetComponent<EventInfo>());
                                if (bar.GetComponent<EventTrigger>())
                                    Destroy(bar.GetComponent<EventTrigger>());

                                LSHelpers.DeleteChildren(bar.transform);
                                bar.transform.SetParent(editorDialogContent.Find("Viewport/Content"));
                                bar.transform.localScale = Vector3.one;
                                bar.name = "input [BUTTON]";

                                Triggers.AddTooltip(bar, key, "");

                                var l = Instantiate(label);
                                l.transform.SetParent(bar.transform);
                                l.transform.SetAsFirstSibling();
                                l.transform.localScale = Vector3.one;
                                l.transform.GetChild(0).GetComponent<Text>().text = key;
                                l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(354f, 20f);

                                var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
                                {
                                    ltextrt.anchoredPosition = new Vector2(10f, -5f);
                                }

                                bar.GetComponent<Image>().enabled = true;
                                bar.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);

                                var btt = bar.AddComponent<Button>();
                                btt.onClick.AddListener(delegate ()
                                {
                                    if (((Dictionary<string, object>)dict["Custom Objects"]).Count > 0)
                                        currentID = ((Dictionary<string, object>)dict["Custom Objects"]).ElementAt(0).Key;

                                    RenderCustomDialog(dict);
                                    RenderCustomObjects(dict);
                                });
                            }
                        }
                    }
                }

                if (id == "0" || id == "1")
                {
                    var bar = Instantiate(singleInput);
                    Destroy(bar.GetComponent<InputField>());
                    Destroy(bar.GetComponent<EventInfo>());

                    if (bar.GetComponent<EventTrigger>())
                        Destroy(bar.GetComponent<EventTrigger>());

                    LSHelpers.DeleteChildren(bar.transform);
                    bar.transform.SetParent(editorDialogContent.Find("Viewport/Content"));
                    bar.transform.localScale = Vector3.one;
                    bar.name = "input [STRING]";

                    Triggers.AddTooltip(bar, "Default player models cannot be edited.", "");

                    var l = Instantiate(label);
                    l.transform.SetParent(bar.transform);
                    l.transform.SetAsFirstSibling();
                    l.transform.localScale = Vector3.one;
                    l.transform.GetChild(0).GetComponent<Text>().text = "Cannot edit default Player Models!";
                    l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(354f, 20f);

                    var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
                    {
                        ltextrt.anchoredPosition = new Vector2(10f, -5f);
                    }

                    bar.GetComponent<Image>().enabled = true;
                    bar.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);
                }

                dictionaryTest = dict;
            }

            yield break;
        }

        public static void updatePlayers()
        {
            for (int i = 0; i < InputDataManager.inst.players.Count; i++)
            {
                var rtPlayer = GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)).gameObject.GetComponentByName("RTPlayer");
                rtPlayer.GetType().GetMethod("updatePlayer", BindingFlags.Public | BindingFlags.Instance).Invoke(rtPlayer, new object[] { });
            }
        }

        public static void UpdateColorButtons(Transform _tf, string _type, Dictionary<string, object> _dictionary)
        {
            for (int i = 0; i < _tf.childCount; i++)
            {
                int n = (int)_dictionary[_type];

                var toggle = _tf.GetChild(i).GetComponent<Toggle>();

                toggle.onValueChanged.RemoveAllListeners();
                if (i == n)
                {
                    toggle.isOn = true;
                }
                else
                {
                    toggle.isOn = false;
                }
                int tmpIndex = i;
                toggle.onValueChanged.AddListener(delegate (bool _val)
                {
                    _dictionary[_type] = tmpIndex;
                    inst.StartCoroutine(RenderDialog());
                });
            }
        }

        public static void UpdateCustomColorButtons(Transform _tf, string _type, Dictionary<string, object> _dictionary)
        {
            for (int i = 0; i < _tf.childCount; i++)
            {
                int n = (int)_dictionary[_type];

                var toggle = _tf.GetChild(i).GetComponent<Toggle>();

                toggle.onValueChanged.RemoveAllListeners();
                if (i == n)
                {
                    toggle.isOn = true;
                }
                else
                {
                    toggle.isOn = false;
                }
                int tmpIndex = i;
                toggle.onValueChanged.AddListener(delegate (bool _val)
                {
                    _dictionary[_type] = tmpIndex;
                    updatePlayers();

                    UpdateCustomColorButtons(_tf, _type, _dictionary);
                });
            }
        }

        public static List<Dropdown.OptionData> GetShapes()
        {
            var list = new List<Dropdown.OptionData>();

            bool customShapes = ObjectManager.inst.objectPrefabs.Count > 7;

            list.Add(new Dropdown.OptionData("Square"));
            list.Add(new Dropdown.OptionData("Circle"));
            list.Add(new Dropdown.OptionData("Triangle"));
            list.Add(new Dropdown.OptionData("Arrow"));
            list.Add(new Dropdown.OptionData("Text (Cannot use)"));
            list.Add(new Dropdown.OptionData("Hexagon"));
            list.Add(new Dropdown.OptionData("Image (Cannot use)"));
            list.Add(new Dropdown.OptionData("Pentagon"));
            list.Add(new Dropdown.OptionData("Misc"));

            return list;
        }

        public static List<Dropdown.OptionData> GetShapeOptions(int _shape)
        {
            var list = new List<Dropdown.OptionData>();

            bool customShapes = ObjectManager.inst.objectPrefabs.Count > 7;

            switch (_shape)
            {
                case 0:
                    {
                        list.Add(new Dropdown.OptionData("Square"));
                        list.Add(new Dropdown.OptionData("Square Outline"));
                        list.Add(new Dropdown.OptionData("Square Outline Thin"));
                        if (customShapes)
                        {
                            list.Add(new Dropdown.OptionData("Diamond"));
                            list.Add(new Dropdown.OptionData("Diamond Outline"));
                            list.Add(new Dropdown.OptionData("Diamond Outline Thin"));
                        }
                        break;
                    }
                case 1:
                    {
                        list.Add(new Dropdown.OptionData("Circle"));
                        list.Add(new Dropdown.OptionData("Circle Outline"));
                        list.Add(new Dropdown.OptionData("Semi-Circle"));
                        list.Add(new Dropdown.OptionData("Semi-Circle Outline"));
                        list.Add(new Dropdown.OptionData("Circle Outline Thin"));
                        list.Add(new Dropdown.OptionData("Quarter Circle"));
                        list.Add(new Dropdown.OptionData("Quarter Circle Outline"));
                        list.Add(new Dropdown.OptionData("Eighth Circle"));
                        list.Add(new Dropdown.OptionData("Eighth Circle Outline"));
                        if (customShapes)
                        {
                            list.Add(new Dropdown.OptionData("Circle Outline Thinner"));
                            list.Add(new Dropdown.OptionData("Semi-Circle Outline Thin"));
                            list.Add(new Dropdown.OptionData("Semi-Circle Outline Thinner"));
                            list.Add(new Dropdown.OptionData("Quarter Circle Outline Thin"));
                            list.Add(new Dropdown.OptionData("Quarter Circle Outline Thinner"));
                            list.Add(new Dropdown.OptionData("Eighth Circle Outline Thin"));
                            list.Add(new Dropdown.OptionData("Eighth Circle Outline Thinner"));
                        }
                        break;
                    }
                case 2:
                    {
                        list.Add(new Dropdown.OptionData("Triangle"));
                        list.Add(new Dropdown.OptionData("Triangle Outline"));
                        list.Add(new Dropdown.OptionData("Right Triangle"));
                        list.Add(new Dropdown.OptionData("Right Triangle Outline"));
                        if (customShapes)
                            list.Add(new Dropdown.OptionData("Triangle Outline Thin"));
                        break;
                    }
                case 3:
                    {
                        list.Add(new Dropdown.OptionData("Full Arrow"));
                        list.Add(new Dropdown.OptionData("Top Arrow"));
                        if (customShapes)
                            list.Add(new Dropdown.OptionData("Chevron Arrow"));
                        break;
                    }
                case 4:
                    {
                        list.Add(new Dropdown.OptionData("Text (Not usable)"));
                        break;
                    }
                case 5:
                    {
                        list.Add(new Dropdown.OptionData("Hexagon"));
                        list.Add(new Dropdown.OptionData("Hexagon Outline"));
                        list.Add(new Dropdown.OptionData("Hexagon Outline Thin"));
                        list.Add(new Dropdown.OptionData("Half Hexagon"));
                        list.Add(new Dropdown.OptionData("Half Hexagon Outline"));
                        list.Add(new Dropdown.OptionData("Half Hexagon Outline Thin"));
                        break;
                    }
                case 6:
                    {
                        list.Add(new Dropdown.OptionData("Image (Not usable)"));
                        break;
                    }
                case 7:
                    {
                        list.Add(new Dropdown.OptionData("Pentagon"));
                        list.Add(new Dropdown.OptionData("Pentagon Outline"));
                        list.Add(new Dropdown.OptionData("Pentagon Outline Thin"));
                        list.Add(new Dropdown.OptionData("Half Pentagon"));
                        list.Add(new Dropdown.OptionData("Half Pentagon Outline"));
                        list.Add(new Dropdown.OptionData("Half Pentagon Outline Thin"));
                        break;
                    }
                case 8:
                    {
                        list.Add(new Dropdown.OptionData("PA Logo Top"));
                        list.Add(new Dropdown.OptionData("PA Logo Bottom"));
                        list.Add(new Dropdown.OptionData("PA Logo"));
                        list.Add(new Dropdown.OptionData("Star"));
                        list.Add(new Dropdown.OptionData("Moon"));
                        list.Add(new Dropdown.OptionData("Moon Thin"));
                        list.Add(new Dropdown.OptionData("Half Moon"));
                        list.Add(new Dropdown.OptionData("Half Moon Thin"));
                        list.Add(new Dropdown.OptionData("Quarter Moon"));
                        list.Add(new Dropdown.OptionData("Quarter Moon Thin"));
                        list.Add(new Dropdown.OptionData("System Error Wing"));
                        list.Add(new Dropdown.OptionData("Heart"));
                        list.Add(new Dropdown.OptionData("Heart Outline"));
                        list.Add(new Dropdown.OptionData("Heart Outline Thin"));
                        list.Add(new Dropdown.OptionData("Half Heart"));
                        list.Add(new Dropdown.OptionData("Half Heart Outline"));
                        list.Add(new Dropdown.OptionData("Half Heart Outline Thin"));
                        break;
                    }
            }

            return list;
        }
    }
}
