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

using EditorManagement.Functions.Tools;

namespace EditorManagement.Functions
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

        public static List<GameObject> playerObjects = new List<GameObject>();

        public static Type playerPlugin;
        public static CreativePlayersEditor inst;

        public static Font editorFont;

        private void Awake()
        {
            if (!GameObject.Find("BepInEx_Manager").GetComponentByName("PlayerPlugin"))
            {
                Destroy(gameObject);
            }
            else
            {
                playerPlugin = GameObject.Find("BepInEx_Manager").GetComponentByName("PlayerPlugin").GetType();
            }

            inst = this;

            editorFont = GameObject.Find("TitleBar/File/Text").GetComponent<Text>().font;

            editorDialogObject = Instantiate(EditorManager.inst.GetDialog("Multi Keyframe Editor (Object)").Dialog.gameObject);
            editorDialogTransform = editorDialogObject.transform;
            editorDialogObject.name = "PlayerEditorDialog";
            editorDialogObject.layer = 5;
            editorDialogTransform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs").transform);
            editorDialogTransform.position = new Vector3(1537.5f, 714.945f, 0f) * EditorManager.inst.ScreenScale;
            editorDialogObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 32f);

            editorDialogTitle = editorDialogTransform.GetChild(0);
            editorDialogTitle.GetComponent<Image>().color = LSColors.HexToColor("E57373");
            editorDialogTitle.GetChild(0).GetComponent<Text>().text = "- Player Editor -";

            editorDialogSpacer = editorDialogTransform.GetChild(1);
            GridLayoutGroup playerFunctionsGroup = editorDialogSpacer.gameObject.AddComponent<GridLayoutGroup>();
            playerFunctionsGroup.cellSize = new Vector2(248f, 50f);
            playerFunctionsGroup.spacing = new Vector2(8f, 8f);

            var dropdownInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/autokill/tod-dropdown");

            var rt = AccessTools.TypeByName("CreativePlayers.Functions.RTExtensions");

            //Dropdown
            {
                GameObject x = Instantiate(dropdownInput);
                x.transform.SetParent(editorDialogSpacer);
                x.transform.localScale = Vector3.one;

                Destroy(x.GetComponent<HoverTooltip>());
                Destroy(x.GetComponent<HideDropdownOptions>());

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

                b1.transform.GetChild(0).GetComponent<Text>().text = "Save Player Models";
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

                b1.transform.GetChild(0).GetComponent<Text>().text = "Create New Player Model";
                var butt = b1.GetComponent<Button>();
                butt.onClick.RemoveAllListeners();
                butt.onClick.AddListener(delegate ()
                {
                    var num = playerModelDropdown.options.Count;
                    playerPlugin.GetMethod("CreateNewPlayerModel").Invoke(playerPlugin, new object[] { });

                    rt.GetMethod("SetPlayerModelIndex").Invoke(rt, new object[] { 0, num });

                    playerPlugin.GetMethod("StartRespawnPlayers").Invoke(playerPlugin, new object[] { });

                    inst.StartCoroutine(RenderDialog());
                });
            }

            editorDialogText = editorDialogTransform.GetChild(2);

            editorDialogText.SetSiblingIndex(1);

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

            objectDialog = Instantiate(EditorManager.inst.GetDialog("Multi Keyframe Editor (Object)").Dialog.gameObject);
            objectDialogTF = objectDialog.transform;
            objectDialog.name = "PlayerObjectEditorDialog";
            objectDialog.layer = 5;
            objectDialogTF.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs").transform);
            objectDialogTF.position = new Vector3(1537.5f, 714.945f, 0f) * EditorManager.inst.ScreenScale;
            objectDialog.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 32f);

            objectDialogTF.GetChild(0).GetComponent<Image>().color = LSColors.HexToColor("E57373");
            objectTitle = objectDialogTF.GetChild(0).GetChild(0).GetComponent<Text>();
            objectTitle.text = "- Base Name -";

            objectText = objectDialogTF.Find("Text").GetComponent<Text>();

            var scrollView2 = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View"));
            objectDialogContent = scrollView2.transform;
            objectDialogContent.SetParent(objectDialogTF);
            objectDialogContent.localScale = Vector3.one;
            scrollView.name = "Scroll View";

            //var glg2 = scrollView2.AddComponent<GridLayoutGroup>();
            //glg2.cellSize = new Vector2(124f, 64f);
            //glg2.spacing = new Vector2(4f, 4f);

            LSHelpers.DeleteChildren(scrollView2.transform.Find("Viewport/Content"), false);

            scrollView2.GetComponent<RectTransform>().sizeDelta = new Vector2(765f, 512f);

            Triggers.AddEditorDialog("Player Object Editor", objectDialog);
        }

        //Put this into RenderDialog() and change this method to be for CustomObjects instead. CustomObjects UI should be similar to BG / Checkpoint editors
        public static void GenerateObjectDialog(string _type, Dictionary<string, object> _dictionary)
        {
            EditorManager.inst.ShowDialog("Player Object Editor");
            var label = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content").transform.GetChild(2).gameObject;
            var singleInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move/position/x");
            var vector2Input = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move/position");
            var boolInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/toggle/toggle");
            var dropdownInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/autokill/tod-dropdown");
            var sliderFullInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/bpm");
            var stringInput = GameObject.Find("TimelineBar/GameObject/Time Input");

            objectTitle.text = "- " + _type + " -";
            objectText.text = "Now editing " + _type + ".";
            var n = _dictionary[_type];
            Debug.LogFormat("{0}Type: {1}\nValue: {2}", EditorPlugin.className, _type, _dictionary[_type]);

            var valueType = 0;
            if (_type == "Base Name")
            {
                valueType = 0;
            }
            if (_type == "Base Health")
            {
                valueType = 1;
            }
            if (_type == "Head Shape")
            {
                valueType = 2;
            }

            switch (valueType)
            {
                case 0:
                    {
                        var s = Instantiate(stringInput);
                        s.transform.SetParent(objectDialogContent.Find("Viewport/Content"));
                        s.transform.localScale = Vector3.one;

                        Destroy(s.GetComponent<UnityEngine.EventSystems.EventTrigger>());

                        var sif = s.GetComponent<InputField>();
                        sif.onValueChanged.RemoveAllListeners();
                        sif.characterValidation = InputField.CharacterValidation.None;
                        sif.contentType = InputField.ContentType.Standard;
                        sif.characterLimit = 0;
                        sif.text = (string)n;
                        sif.onValueChanged.AddListener(delegate (string _val)
                        {
                            Debug.LogFormat("{0}Changing {1} to {2}", EditorPlugin.className, _dictionary[_type], _val);
                            _dictionary[_type] = _val;
                        });

                        break;
                    }
                case 1:
                    {
                        var s = Instantiate(stringInput);
                        s.transform.SetParent(objectDialogContent.Find("Viewport/Content"));
                        s.transform.localScale = Vector3.one;

                        Destroy(s.GetComponent<UnityEngine.EventSystems.EventTrigger>());

                        var sif = s.GetComponent<InputField>();
                        sif.onValueChanged.RemoveAllListeners();
                        sif.characterValidation = InputField.CharacterValidation.None;
                        sif.contentType = InputField.ContentType.Standard;
                        sif.characterLimit = 0;
                        sif.text = (string)n;
                        sif.onValueChanged.AddListener(delegate (string _val)
                        {
                            Debug.LogFormat("{0}Changing {1} to {2}", EditorPlugin.className, _dictionary[_type], _val);
                            _dictionary[_type] = _val;
                        });

                        break;
                    }
                case 2:
                    {
                        break;
                    }
                case 3:
                    {
                        break;
                    }
            }
            var button = Instantiate(EditorManager.inst.folderButtonPrefab);
            button.transform.localScale = Vector3.one;
            button.transform.SetParent(objectDialogContent.Find("Viewport/Content"));
            button.transform.GetChild(0).GetComponent<Text>().text = "Back";

            var b = button.GetComponent<Button>();
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(delegate ()
            {
                EditorManager.inst.HideDialog("Player Object Editor");
                RenderDialog();
            });
        }

        public static void RenderCustomObjects()
        {

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
            Debug.LogFormat("{0}Clearing options", EditorPlugin.className);
            playerModelDropdown.options.Clear();

            Debug.LogFormat("{0}Getting RTExtensions", EditorPlugin.className);
            var rt = AccessTools.TypeByName("CreativePlayers.Functions.RTExtensions");

            if (rt != null)
            {
                Debug.LogFormat("{0}RTExtensions is not null", EditorPlugin.className);
                var obj = rt.GetMethod("GetPlayerModels").Invoke(rt, new object[] { });
                var currentIndex = (string)rt.GetMethod("GetPlayerModelIndex").Invoke(rt, new object[] { 0 });
                object currentModel = null;

                playerModelDropdown.onValueChanged.RemoveAllListeners();
                Debug.LogFormat("{0}Getting PlayerModel List", EditorPlugin.className);
                for (int i = 0; i < obj.GetCount(); i++)
                {
                    var values = (Dictionary<string, object>)obj.GetItem(i).GetType().GetField("values").GetValue(obj.GetItem(i));
                    playerModelDropdown.options.Add(new Dropdown.OptionData((string)values["Base Name"]));

                    if ((string)values["Base ID"] == currentIndex)
                    {
                        currentModel = obj.GetItem(i);
                    }
                }

                int c = (int)rt.GetMethod("GetPlayerModelInt").Invoke(rt, new object[] { currentModel });
                playerModelDropdown.value = c;
                playerModelDropdown.onValueChanged.AddListener(delegate (int _val)
                {
                    Debug.LogFormat("{0}Setting Player 1's model index to {1}", EditorPlugin.className, _val);
                    rt.GetMethod("SetPlayerModelIndex").Invoke(rt, new object[] { 0, _val });
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

                Debug.LogFormat("{0}Generating buttons", EditorPlugin.className);
                foreach (var objects in (Dictionary<string, object>)currentModel.GetType().GetField("values").GetValue(currentModel))
                {
                    var key = objects.Key;
                    if (key != "Base ID")
                    {
                        if (key == "Base Name")
                        {
                            var bar = Instantiate(singleInput);
                            Destroy(bar.GetComponent<InputField>());
                            Destroy(bar.GetComponent<EventInfo>());

                            if (bar.GetComponent<UnityEngine.EventSystems.EventTrigger>())
                                Destroy(bar.GetComponent<UnityEngine.EventSystems.EventTrigger>());

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
                            xif.text = (string)objects.Value;
                            xif.textComponent.fontSize = 18;
                            xif.onValueChanged.AddListener(delegate (string _val)
                            {
                                ((Dictionary<string, object>)currentModel.GetType().GetField("values").GetValue(currentModel))[key] = _val;
                            });
                            xif.onEndEdit.RemoveAllListeners();
                            xif.onEndEdit.AddListener(delegate (string _val)
                            {
                                inst.StartCoroutine(RenderDialog());
                            });
                        }

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

                            Triggers.IncreaseDecreaseButtons(vxif, 1);

                            Destroy(vector2.transform.Find("y").gameObject);
                        }

                        if (key.Contains("Shape"))
                        {
                            //Shape
                            {
                                var bar = Instantiate(singleInput);
                                if (bar.GetComponent<InputField>())
                                    Destroy(bar.GetComponent<InputField>());
                                if (bar.GetComponent<EventInfo>())
                                    Destroy(bar.GetComponent<EventInfo>());
                                if (bar.GetComponent<UnityEngine.EventSystems.EventTrigger>())
                                    Destroy(bar.GetComponent<UnityEngine.EventSystems.EventTrigger>());

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
                                if (bar.GetComponent<UnityEngine.EventSystems.EventTrigger>())
                                    Destroy(bar.GetComponent<UnityEngine.EventSystems.EventTrigger>());

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

                        if (key.Contains("Position") || key.Contains("Scale") && !key.Contains("Start") && !key.Contains("End") || key.Contains("Force"))
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

                            Triggers.IncreaseDecreaseButtons(vxif, 1f);
                            Triggers.IncreaseDecreaseButtons(vyif, 1f);
                        }

                        if (key.Contains("Scale") &&
                            (key.Contains("Start") || key.Contains("End")) ||
                            key.Contains("Rotation") ||
                            key == "Tail Base Distance" ||
                            key.Contains("Opacity") ||
                            key.Contains("Lifetime") ||
                            key.Contains("Start Width") ||
                            key.Contains("End Width") ||
                            key.Contains("Trail Time") ||
                            key == "Boost Particles Duration" ||
                            key.Contains("Amount") && !key.Contains("Boost") ||
                            key.Contains("Speed"))
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

                            Triggers.IncreaseDecreaseButtons(vxif, 1f);

                            Destroy(vector2.transform.Find("y").gameObject);
                        }

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

                        if (key.Contains("Color"))
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

                            for (int i = 19; i < 24; i++)
                            {
                                var colorButton = Instantiate(x.transform.GetChild(0).gameObject);
                                colorButton.name = i.ToString();
                                colorButton.transform.SetParent(x.transform);
                                colorButton.transform.localScale = Vector3.one;
                            }

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
                                if (i > 4)
                                {
                                    int num = i - 5;
                                    x.transform.Find(strr).GetComponent<Image>().color = GameManager.inst.LiveTheme.objectColors[num];
                                }
                            }

                            UpdateColorButtons(x.transform, key, (Dictionary<string, object>)currentModel.GetType().GetField("values").GetValue(currentModel));
                        }

                        if (key.Contains("Active") || key.Contains("Emitting"))
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
                    }
                }
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
