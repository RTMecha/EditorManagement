using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BepInEx.Configuration;

using UnityEngine;
using UnityEngine.UI;

using EditorManagement.Functions.Editors;
using EditorManagement.Functions.Components;
using EditorManagement.Functions;
using EditorManagement.Functions.Tools;

namespace EditorManagement
{
    public class EditorGUI : MonoBehaviour
    {
        public static Dictionary<string, EditorElement> objects = new Dictionary<string, EditorElement>();

        public static Sprite roundedSquare;

        public static EditorGUI inst;

        public static EditorManager editor
        {
            get
            {
                if (EditorManager.inst == null)
                {
                    Destroy(inst);
                }

                return EditorManager.inst;
            }
        }

        private void Awake()
        {
            inst = this;

            inst.StartCoroutine(RefreshEditorGUI());
        }

        public static IEnumerator RefreshEditorGUI()
        {
            var guiMain = EditorManager.inst.GUIMain;
            objects.Add("TitleBar", new EditorElement(guiMain.transform.Find("TitleBar").gameObject, new Dictionary<string, object>()));
            yield break;
        }

        public static List<List<GameObject>> editorGUI = new List<List<GameObject>>();
        public static List<ConfigEntry<Color>> configColors = new List<ConfigEntry<Color>>
        {
            //ConfigEntries.EditorGUIColor1,
            //ConfigEntries.EditorGUIColor2,
            //ConfigEntries.EditorGUIColor3,
            //ConfigEntries.EditorGUIColor4,
            //ConfigEntries.EditorGUIColor5,
            //ConfigEntries.EditorGUIColor6,
            //ConfigEntries.EditorGUIColor7,
            //ConfigEntries.EditorGUIColor8,
            //ConfigEntries.EditorGUIColor9,
        };

        public static void CreateEditorGUI()
        {
            if (editorGUI.Count > 0)
            {
                editorGUI.Clear();
            }

            //Search for objects with color
            var list1 = EditorExtensions.ColorRange(new Color(0.1294118f, 0.1294118f, 0.1294118f, 1f), 0.01f);
            var list2 = EditorExtensions.ColorRange(new Color(0.1058824f, 0.1058824f, 0.1098039f, 1f), 0.01f);
            var list3 = EditorExtensions.ColorRange(new Color(0.937255f, 0.9215687f, 0.937255f, 1f), 0.01f);
            var list4 = EditorExtensions.ColorRange(new Color(0.1882353f, 0.1882353f, 0.1882353f, 1f), 0.01f);
            var list5 = EditorExtensions.ColorRange(new Color(0.2431373f, 0.2431373f, 0.2588235f, 1f), 0.01f);
            var list6 = EditorExtensions.ColorRange(new Color(0.9333334f, 0.9176471f, 0.9333334f, 1f), 0.01f);
            var list7 = EditorExtensions.ColorRange(new Color(0.2156863f, 0.2156863f, 0.2196079f, 1f), 0.01f);
            var list8 = EditorExtensions.ColorRange(new Color(0.1960784f, 0.1960784f, 0.1960784f, 1f), 0.01f);
            var list9 = EditorExtensions.ColorRange(new Color(0.1215686f, 0.1215686f, 0.1215686f, 1f), 0.01f);

            //Add GameObject Lists to editorGUI list
            editorGUI.Add(list1);
            editorGUI.Add(list2);
            editorGUI.Add(list3);
            editorGUI.Add(list4);
            editorGUI.Add(list5);
            editorGUI.Add(list6);
            editorGUI.Add(list7);
            editorGUI.Add(list8);
            editorGUI.Add(list9);
        }

        public static void UpdateEditorGUI()
        {
            if (configColors.Count < 1)
                return;

            for (int i = 0; i < configColors.Count; i++)
            {
                foreach (var guiPart in editorGUI[i])
                {
                    if (guiPart && guiPart.GetComponent<Image>() && guiPart.GetComponent<Image>().color != configColors[i].Value)
                    {
                        guiPart.GetComponent<Image>().color = configColors[i].Value;
                    }
                    if (guiPart && guiPart.GetComponent<Text>() && guiPart.GetComponent<Text>().color != configColors[i].Value)
                    {
                        guiPart.GetComponent<Text>().color = configColors[i].Value;
                    }
                }
            }
        }

        public class EditorElement
        {
            public EditorElement(GameObject gameObject, Dictionary<string, object> components, string id = "")
            {
                this.gameObject = gameObject;
                transform = gameObject.transform;
                this.components = components;
            }

            public void Assign()
            {
                if (components.ContainsKey("Image"))
                {
                    ((Image)components["Image"]).color = currentTheme.colorsDictionary[id].color;
                }
                if (components.ContainsKey("Text"))
                {
                    ((Text)components["Text"]).color = currentTheme.colorsDictionary[id].color;
                }
            }

            public GameObject gameObject;
            public Transform transform;
            public Dictionary<string, object> components;
            public string id;
        }

        /* - 0.1216 0.1216 0.1216 1 (TitleBar, EditorDialogs)
         * - 0.1059 0.1059 0.1098 1 (whole-timeline/Timeline/Panel 2, TimelineBar)
         * - 
         * - 
         * - 
         * - 
         * - 
         * - 
         */

        public static int editorThemeIndex = 0;

        public static EditorTheme currentTheme
        {
            get
            {
                return editorThemes[editorThemeIndex];
            }
        }

        public static Dictionary<string, EditorTheme> editorThemesDictionary
        {
            get
            {
                var dictionary = new Dictionary<string, EditorTheme>();
                foreach (var theme in editorThemes)
                {
                    dictionary.Add(theme.name, theme);
                }
                return dictionary;
            }
        }

        public static List<EditorTheme> editorThemes = new List<EditorTheme>
        {
            new EditorTheme("Legacy", new List<EditorTheme.GUIColor>
            {
                new EditorTheme.GUIColor("back", new Color(0.1216f, 0.1216f, 0.1216f, 1f)),
                new EditorTheme.GUIColor("back.timeline", new Color(0.1059f, 0.1059f, 0.1098f, 1f)),
            }),
            new EditorTheme("New", new List<EditorTheme.GUIColor>
            {
                new EditorTheme.GUIColor("back", new Color(0.1216f, 0.1216f, 0.1216f, 1f)),
                new EditorTheme.GUIColor("back.timeline", new Color(0.1059f, 0.1059f, 0.1098f, 1f)),
            }),
            new EditorTheme("Beta", new List<EditorTheme.GUIColor>
            {
                new EditorTheme.GUIColor("back", new Color(0.1216f, 0.1216f, 0.1216f, 1f)),
                new EditorTheme.GUIColor("back.timeline", new Color(0.1059f, 0.1059f, 0.1098f, 1f)),
            }),
            new EditorTheme("Alpha", new List<EditorTheme.GUIColor>
            {
                new EditorTheme.GUIColor("back", new Color(0.1216f, 0.1216f, 0.1216f, 1f)),
                new EditorTheme.GUIColor("back.timeline", new Color(0.1059f, 0.1059f, 0.1098f, 1f)),
            }),
        };

        public class EditorTheme
        {
            public EditorTheme(string name, List<GUIColor> colors)
            {
                this.name = name;
                this.colors = colors;
            }

            public string name;
            public List<GUIColor> colors;

            public Dictionary<string, GUIColor> colorsDictionary
            {
                get
                {
                    var dictionary = new Dictionary<string, GUIColor>();
                    foreach (var color in colors)
                    {
                        dictionary.Add(color.id, color);
                    }
                    return dictionary;
                }
            }

            public class GUIColor
            {
                public GUIColor(string id, Color color)
                {
                    this.id = id;
                    this.color = color;
                }

                public string id;
                public Color color;
            }
        }
    }
}
