using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using RTFunctions.Functions;

using EditorManagement.Functions;

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

        public void Clear()
        {
            EditorGUIElements.Clear();
        }

        public void RenderElements()
        {
            var theme = CurrentTheme;

            foreach (var element in EditorGUIElements)
            {
                if (theme.ColorGroups.ContainsKey(element.group))
                {
                    element.SetColor(theme.ColorGroups[element.group]);
                }
            }
        }

        public EditorTheme CurrentTheme => EditorThemes[Mathf.Clamp(currentTheme, 0, EditorThemes.Count - 1)];

        public int currentTheme;

        public void AddElement(Element element)
        {
            var theme = CurrentTheme;

            if (theme.ColorGroups.ContainsKey(element.group))
            {
                if (!element.isButton)
                    element.SetColor(theme.ColorGroups[element.group]);
                else
                {
                    var colorBlock = new ColorBlock();

                    if (theme.ColorGroups.ContainsKey(element.group + " Highlight"))
                    {
                        colorBlock.highlightedColor = theme.ColorGroups[element.group + " Highlight"];
                    }

                    if (theme.ColorGroups.ContainsKey(element.group + " Selected"))
                    {
                        colorBlock.selectedColor = theme.ColorGroups[element.group + " Selected"];
                    }

                    if (theme.ColorGroups.ContainsKey(element.group + " Pressed"))
                    {
                        colorBlock.pressedColor = theme.ColorGroups[element.group + " Pressed"];
                    }

                    if (theme.ColorGroups.ContainsKey(element.group + " Disabled"))
                    {
                        colorBlock.disabledColor = theme.ColorGroups[element.group + " Disabled"];
                    }

                    element.SetColor(theme.ColorGroups[element.group], colorBlock);
                }
            }
        }

        public List<Element> EditorGUIElements { get; set; } = new List<Element>();

        public List<EditorTheme> EditorThemes { get; set; } = new List<EditorTheme>
        {
            new EditorTheme("Legacy", new Dictionary<string, Color>
            {
                { "Background", LSColors.HexToColorAlpha("212121FF") },
                { "Scrollbar Handle", LSColors.HexToColorAlpha("C8C8C8FF") },
            }),
        };

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

            public bool isButton = false;

            bool rounded = false;
            public bool Rounded { get; set; }
            public bool canSetRounded = false;

            public void SetColor(Color color)
            {
                foreach (var component in Components)
                {
                    if (component is Image image)
                        image.color = color;
                    if (component is Text text)
                        text.color = color;
                }

                onSetColor?.Invoke(this, color);
            }

            public void SetColor(Color color, ColorBlock colorBlock)
            {
                foreach (var component in Components)
                {
                    if (component is Image image)
                        image.color = color;
                    if (component is Button button)
                        button.colors = colorBlock;
                }
            }
        }
    }
}
