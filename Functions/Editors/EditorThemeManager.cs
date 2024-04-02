using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using RTFunctions.Functions;

using EditorManagement.Functions;
using RTFunctions.Functions.Managers;

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
                element.ApplyTheme(theme);
            }
        }

        public EditorTheme CurrentTheme => EditorThemes[Mathf.Clamp(currentTheme, 0, EditorThemes.Count - 1)];

        public int currentTheme;

        public void AddElement(Element element)
        {
            element.ApplyTheme(CurrentTheme);
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

        public Dictionary<string, EditorTheme> EditorThemesDictionary => EditorThemes.ToDictionary(x => x.name, x => x);

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

            public Element(string name, string group, GameObject gameObject, List<Component> components, SpriteManager.RoundedSide roundedSide)
            {
                this.name = name;
                this.group = group;
                GameObject = gameObject;
                Components = components;
                RoundedSide = roundedSide;
            }

            public string name;
            public string group;

            public GameObject GameObject { get; set; }

            public List<Component> Components { get; set; } = new List<Component>();

            public Action<Element, Color> onSetColor;

            public bool isButton = false;

            int rounded = 0;
            public int Rounded
            {
                get => rounded;
                set
                {
                    if (canSetRounded)
                        rounded = value;
                }
            }

            public SpriteManager.RoundedSide RoundedSide { get; set; } = SpriteManager.RoundedSide.W;

            public bool canSetRounded = false;

            public void ApplyTheme(EditorTheme theme)
            {
                SetRounded();

                if (theme.ColorGroups.ContainsKey(group))
                {
                    if (!isButton)
                        SetColor(theme.ColorGroups[group]);
                    else
                    {
                        var colorBlock = new ColorBlock();

                        if (theme.ColorGroups.ContainsKey(group + " Highlight"))
                            colorBlock.highlightedColor = theme.ColorGroups[group + " Highlight"];

                        if (theme.ColorGroups.ContainsKey(group + " Selected"))
                            colorBlock.selectedColor = theme.ColorGroups[group + " Selected"];

                        if (theme.ColorGroups.ContainsKey(group + " Pressed"))
                            colorBlock.pressedColor = theme.ColorGroups[group + " Pressed"];

                        if (theme.ColorGroups.ContainsKey(group + " Disabled"))
                            colorBlock.disabledColor = theme.ColorGroups[group + " Disabled"];

                        SetColor(theme.ColorGroups[group], colorBlock);
                    }
                }
            }

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

            public void SetRounded()
            {
                if (!canSetRounded)
                    return;

                foreach (var component in Components)
                {
                    if (component is Image image)
                    {
                        SpriteManager.SetRoundedSprite(image, Rounded, RoundedSide);
                    }
                }
            }
        }
    }
}
