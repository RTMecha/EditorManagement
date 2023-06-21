using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

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

            //Dropdown
            {
                GameObject x = Instantiate(dropdownInput);
                x.transform.SetParent(editorDialogSpacer);
                x.transform.localScale = Vector3.one;

                //RectTransform xRT = x.GetComponent<RectTransform>();
                //xRT.anchoredPosition = ConfigEntries.ORLDropdownPos.Value;

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
                b1.name = "something 1";
            }

            //Button 2
            {
                var b1 = Instantiate(EditorManager.inst.folderButtonPrefab);
                b1.transform.SetParent(editorDialogSpacer);
                b1.transform.localScale = Vector3.one;
                b1.name = "something 2";
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
            RenderDialog();
        }

        public static void RenderDialog()
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
                    RenderDialog();
                });

                #region UI Elements

                var label = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content").transform.GetChild(2).gameObject;
                var singleInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move/position/x");
                var vector2Input = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move/position");
                var boolInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/toggle/toggle");
                var dropdownInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/autokill/tod-dropdown");
                var sliderFullInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/bpm");
                var stringInput = GameObject.Find("TimelineBar/GameObject/Time Input");

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
                            LSHelpers.DeleteChildren(bar.transform);
                            bar.transform.SetParent(objectDialogContent.Find("Viewport/Content"));
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
                        }

                        if (key == "Base Health")
                        {
                            GameObject x = Instantiate(singleInput);
                            x.transform.SetParent(objectDialogContent.Find("Viewport/Content"));
                            x.name = "input [INT]";

                            Destroy(x.GetComponent<EventInfo>());
                            x.transform.localScale = Vector3.one;
                            x.transform.GetChild(0).localScale = Vector3.one;

                            var l = Instantiate(label);
                            l.transform.SetParent(x.transform);
                            l.transform.SetAsFirstSibling();
                            l.transform.GetChild(0).GetComponent<Text>().text = key;
                            l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(541f, 20f);

                            var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
                            {
                                ltextrt.anchoredPosition = new Vector2(10f, -5f);
                            }

                            x.GetComponent<Image>().enabled = true;
                            x.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);

                            Triggers.AddTooltip(x, key, "");

                            var xif = x.GetComponent<InputField>();
                            xif.onValueChanged.RemoveAllListeners();
                            xif.characterValidation = InputField.CharacterValidation.Integer;
                            xif.text = objects.Value.ToString();
                            xif.onValueChanged.AddListener(delegate (string _val)
                            {
                                if (int.TryParse(_val, out int e))
                                {
                                    ((Dictionary<string, object>)currentModel.GetType().GetField("values").GetValue(currentModel))[key] = e;
                                }
                            });
                        }
                    }
                }
            }
        }
    }
}
