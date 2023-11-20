using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

namespace EditorManagement.Functions.Editors
{
    public class EditorThemeManager
    {
        // When working on this, try rounding up the colors. If one color is really similar to another, then include them in the same color group.

        public static EditorThemeManager inst;

        public static void Init()
        {
            inst = new EditorThemeManager();
        }

        public void Update()
        {
            if (EditorManager.inst == null)
                Clear();
        }

        public static void Clear()
        {
            inst = null;
        }

        public void RenderElements()
        {
            var theme = EditorThemes[currentTheme];

            foreach (var element in EditorGUIElements)
            {
                if (theme.ColorGroups.ContainsKey(element.group))
                {
                    element.SetColor(theme.ColorGroups[element.group]);
                }
            }
        }

        public int currentTheme;

        public List<Element> EditorGUIElements { get; set; } = new List<Element>();

        public List<EditorTheme> EditorThemes { get; set; } = new List<EditorTheme>();

        public class EditorTheme
        {
            public EditorTheme(string name, Dictionary<string, Color> colorGroups)
            {
                this.name = name;
                ColorGroups = colorGroups;
            }

            public string name;
            public Dictionary<string, Color> ColorGroups { get; set; } = new Dictionary<string, Color>();
        }

        public class Element
        {
            public Element()
            {

            }

            public Element(string name, string group, GameObject gameObject, List<Component> components)
            {
                this.name = name;
                this.group = group;
                GameObject = gameObject;
                Components = components;
            }

            public string name;
            public string group;

            public GameObject GameObject { get; set; }

            public List<Component> Components { get; set; } = new List<Component>();

            public Action<Element, Color> onSetColor;

            public void SetColor(Color color)
            {
                foreach (var component in Components)
                {
                    if (component is Image)
                        ((Image)component).color = color;
                    if (component is Text)
                        ((Text)component).color = color;
                }

                onSetColor?.Invoke(this, color);
            }
        }
    }
}
