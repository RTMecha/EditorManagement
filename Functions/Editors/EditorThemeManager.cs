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

        public static void Update()
        {
            if (EditorManager.inst == null && EditorGUIElements.Count > 0)
                Clear();
        }

        public static void Clear() => EditorGUIElements.Clear();

        public static void RenderElements()
        {
            var theme = CurrentTheme;

            for (int i = 0; i < EditorGUIElements.Count; i++)
                EditorGUIElements[i].ApplyTheme(theme);
        }

        public static EditorTheme CurrentTheme => EditorThemes[Mathf.Clamp((int)EditorConfig.Instance.EditorTheme.Value, 0, EditorThemes.Count - 1)];

        public static void AddElement(Element element)
        {
            EditorGUIElements.Add(element);
            element.ApplyTheme(CurrentTheme);
        }

        public static void ApplyElement(Element element) => element.ApplyTheme(CurrentTheme);

        public static List<Element> EditorGUIElements { get; set; } = new List<Element>();

        public static List<EditorTheme> EditorThemes { get; set; } = new List<EditorTheme>
        {
            new EditorTheme("Legacy", new Dictionary<string, Color>
            {
                { "Background", LSColors.HexToColorAlpha("212121FF") },
                { "Scrollbar Handle", LSColors.HexToColorAlpha("C8C8C8FF") },
                { "Close", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Close Normal", LSColors.HexToColorAlpha("F44336FF") },
                { "Close Highlight", LSColors.HexToColorAlpha("292929FF") },
                { "Close Selected", LSColors.HexToColorAlpha("292929FF") },
                { "Close Pressed", LSColors.HexToColorAlpha("292929FF") },
                { "Close Disabled", LSColors.HexToColorAlpha("292929FF") },
                { "Light Text", LSColors.HexToColorAlpha("E5E1E5FF") },
                { "Function 1", LSColors.HexToColorAlpha("0F7BF8FF") },
                { "Function 2", LSColors.HexToColorAlpha("0F7BF8FF") },
                { "Function 2 Normal", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Function 2 Highlight", LSColors.HexToColorAlpha("E47E7EFF") },
                { "Function 2 Selected", LSColors.HexToColorAlpha("F5F5F5FF") },
                { "Function 2 Pressed", LSColors.HexToColorAlpha("C7C7C7FF") },
                { "Function 2 Disabled", LSColors.HexToColorAlpha("C7C7C780") },
                { "Add", LSColors.HexToColorAlpha("4DB6ACFF") },
                { "Prefab", LSColors.HexToColorAlpha("383838FF") },
                { "Object", LSColors.HexToColorAlpha("EFEBEFFF") },
                { "Marker", LSColors.HexToColorAlpha("FFAF38FF") },
                { "Paste", LSColors.HexToColorAlpha("FFAF38FF") },
                { "Background Object", LSColors.HexToColorAlpha("E57373FF") },
                { "Timeline Bar", LSColors.HexToColorAlpha("1B1B1CFF") },
                { "Event/Check", LSColors.HexToColorAlpha("6CCBCFFF") },
                { "Input Field", LSColors.HexToColorAlpha("EFEBEFFF") },
                { "Input Field Text", LSColors.HexToColorAlpha("252525FF") },
                { "Slider", LSColors.HexToColorAlpha("EEEAEEFF") },
                { "Slider Handle", LSColors.HexToColorAlpha("424242FF") },
                { "Documentation", LSColors.HexToColorAlpha("D89356FF") },
                { "Timeline Scrollbar", LSColors.HexToColorAlpha("686868FF") },
                { "Timeline Scrollbar Normal", LSColors.HexToColorAlpha("676767FF") },
                { "Timeline Scrollbar Highlight", LSColors.HexToColorAlpha("9E9E9EFF") },
                { "Timeline Scrollbar Selected", LSColors.HexToColorAlpha("9D9D9DFF") },
                { "Timeline Scrollbar Pressed", LSColors.HexToColorAlpha("EFEBEFFF") },
                { "Timeline Scrollbar Disabled", LSColors.HexToColorAlpha("676767FF") },
                { "Timeline Scrollbar Base", LSColors.HexToColorAlpha("3E3E42FF") },
                { "Timeline Time Scrollbar", LSColors.HexToColorAlpha("3E3E40FF") },
                { "Title Bar Button", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Title Bar Button Normal", LSColors.HexToColorAlpha("303030FF") },
                { "Title Bar Button Highlight", LSColors.HexToColorAlpha("1B1B1CFF") },
                { "Title Bar Button Selected", LSColors.HexToColorAlpha("1B1B1CFF") },
                { "Title Bar Button Pressed", LSColors.HexToColorAlpha("1B1B1CFF") },
                { "Title Bar Dropdown", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Title Bar Dropdown Normal", LSColors.HexToColorAlpha("1B1B1CFF") },
                { "Title Bar Dropdown Highlight", LSColors.HexToColorAlpha("303030FF") },
                { "Title Bar Dropdown Selected", LSColors.HexToColorAlpha("303030FF") },
                { "Title Bar Dropdown Pressed", LSColors.HexToColorAlpha("303030FF") },
                { "Title Bar Dropdown Disabled", LSColors.HexToColorAlpha("303030FF") },
            }),
            new EditorTheme("Dark", new Dictionary<string, Color>
            {
                { "Background", LSColors.HexToColorAlpha("0A0A0AFF") },
                { "Scrollbar Handle", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Scrollbar Handle Normal", LSColors.HexToColorAlpha("C7C7C7FF") },
                { "Scrollbar Handle Highlight", LSColors.HexToColorAlpha("414141FF") },
                { "Scrollbar Handle Selected", LSColors.HexToColorAlpha("414141FF") },
                { "Scrollbar Handle Pressed", LSColors.HexToColorAlpha("414141FF") },
                { "Scrollbar Handle Disabled", LSColors.HexToColorAlpha("414141FF") },
                { "Close", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Close Normal", LSColors.HexToColorAlpha("F44336FF") },
                { "Close Highlight", LSColors.HexToColorAlpha("292929FF") },
                { "Close Selected", LSColors.HexToColorAlpha("292929FF") },
                { "Close Pressed", LSColors.HexToColorAlpha("292929FF") },
                { "Close Disabled", LSColors.HexToColorAlpha("292929FF") },
                { "Light Text", LSColors.HexToColorAlpha("E5E1E5FF") },
                { "Function 1", LSColors.HexToColorAlpha("0F7BF8FF") },
                { "Function 2", LSColors.HexToColorAlpha("0F7BF8FF") },
                { "Function 2 Normal", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Function 2 Highlight", LSColors.HexToColorAlpha("E47E7EFF") },
                { "Function 2 Selected", LSColors.HexToColorAlpha("F5F5F5FF") },
                { "Function 2 Pressed", LSColors.HexToColorAlpha("C7C7C7FF") },
                { "Function 2 Disabled", LSColors.HexToColorAlpha("C7C7C780") },
                { "Add", LSColors.HexToColorAlpha("4DB6ACFF") },
                { "Prefab", LSColors.HexToColorAlpha("383838FF") },
                { "Object", LSColors.HexToColorAlpha("EFEBEFFF") },
                { "Marker", LSColors.HexToColorAlpha("FFAF38FF") },
                { "Paste", LSColors.HexToColorAlpha("FFAF38FF") },
                { "Background Object", LSColors.HexToColorAlpha("E57373FF") },
                { "Timeline Bar", LSColors.HexToColorAlpha("1B1B1CFF") },
                { "Event/Check", LSColors.HexToColorAlpha("6CCBCFFF") },
                { "Input Field", LSColors.HexToColorAlpha("EFEBEFFF") },
                { "Input Field Text", LSColors.HexToColorAlpha("252525FF") },
                { "Slider", LSColors.HexToColorAlpha("EEEAEEFF") },
                { "Slider Handle", LSColors.HexToColorAlpha("424242FF") },
                { "Documentation", LSColors.HexToColorAlpha("D89356FF") },
                { "Timeline Scrollbar", LSColors.HexToColorAlpha("686868FF") },
                { "Timeline Scrollbar Normal", LSColors.HexToColorAlpha("676767FF") },
                { "Timeline Scrollbar Highlight", LSColors.HexToColorAlpha("9E9E9EFF") },
                { "Timeline Scrollbar Selected", LSColors.HexToColorAlpha("9D9D9DFF") },
                { "Timeline Scrollbar Pressed", LSColors.HexToColorAlpha("EFEBEFFF") },
                { "Timeline Scrollbar Disabled", LSColors.HexToColorAlpha("676767FF") },
                { "Timeline Scrollbar Base", LSColors.HexToColorAlpha("3E3E42FF") },
                { "Timeline Time Scrollbar", LSColors.HexToColorAlpha("3E3E40FF") },
                { "Title Bar Button", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Title Bar Button Normal", LSColors.HexToColorAlpha("303030FF") },
                { "Title Bar Button Highlight", LSColors.HexToColorAlpha("1B1B1CFF") },
                { "Title Bar Button Selected", LSColors.HexToColorAlpha("1B1B1CFF") },
                { "Title Bar Button Pressed", LSColors.HexToColorAlpha("1B1B1CFF") },
                { "Title Bar Dropdown", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Title Bar Dropdown Normal", LSColors.HexToColorAlpha("1B1B1CFF") },
                { "Title Bar Dropdown Highlight", LSColors.HexToColorAlpha("303030FF") },
                { "Title Bar Dropdown Selected", LSColors.HexToColorAlpha("303030FF") },
                { "Title Bar Dropdown Pressed", LSColors.HexToColorAlpha("303030FF") },
                { "Title Bar Dropdown Disabled", LSColors.HexToColorAlpha("303030FF") },
            }),
        };

        public static Dictionary<string, EditorTheme> EditorThemesDictionary => EditorThemes.ToDictionary(x => x.name, x => x);

        public class EditorTheme
        {
            public EditorTheme(string name, Dictionary<string, Color> colorGroups)
            {
                this.name = name;
                ColorGroups = colorGroups;
            }

            public string name;
            public Dictionary<string, Color> ColorGroups { get; set; }
        }

        public class Element
        {
            public Element()
            {

            }

            public Element(string name, string group, GameObject gameObject, List<Component> components, bool canSetRounded, int rounded, SpriteManager.RoundedSide roundedSide, bool isSelectable = false)
            {
                this.name = name;
                this.group = group;
                GameObject = gameObject;
                Components = components;
                this.canSetRounded = canSetRounded;
                Rounded = rounded;
                RoundedSide = roundedSide;
                this.isSelectable = isSelectable;
            }

            public string name;
            public string group;

            public GameObject GameObject { get; set; }

            public List<Component> Components { get; set; }

            public Action<Element, Color> onSetColor;

            public bool isSelectable = false;

            public bool canSetRounded = false;

            int rounded = 0;
            public int Rounded
            {
                get => rounded;
                set => rounded = value;
            }

            public SpriteManager.RoundedSide RoundedSide { get; set; } = SpriteManager.RoundedSide.W;

            public void ApplyTheme(EditorTheme theme)
            {
                SetRounded();

                if (theme.ColorGroups.ContainsKey(group))
                {
                    if (!isSelectable)
                        SetColor(theme.ColorGroups[group]);
                    else
                    {
                        var colorBlock = new ColorBlock();

                        colorBlock.colorMultiplier = 1f;
                        colorBlock.fadeDuration = 0.1f;

                        if (theme.ColorGroups.ContainsKey(group + " Normal"))
                            colorBlock.normalColor = theme.ColorGroups[group + " Normal"];
                        
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
                    if (component is Selectable button)
                        button.colors = colorBlock;
                }

                onSetColor?.Invoke(this, color);
            }

            public void SetRounded()
            {
                if (!canSetRounded || !EditorConfig.Instance.RoundedUI.Value)
                    return;

                foreach (var component in Components)
                {
                    if (component is Image image)
                    {
                        if (Rounded != 0)
                            SpriteManager.SetRoundedSprite(image, Rounded, RoundedSide);
                        else
                            image.sprite = null;
                    }
                }
            }
        }
    }
}
