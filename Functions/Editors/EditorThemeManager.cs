using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using RTFunctions.Functions;

using EditorManagement.Functions;
using RTFunctions.Functions.Managers;

using ThemeSetting = EditorManagement.EditorTheme;
using TMPro;
using System.Collections;

namespace EditorManagement.Functions.Editors
{
    /// <summary>
    /// Class that applies Editor Themes and Rounded setting onto every UI element in the editor.
    /// </summary>
    public class EditorThemeManager
    {
        public static bool DebugMode { get; set; } = true;

        public static void Update()
        {
            if (EditorManager.inst == null && EditorGUIElements.Count > 0)
                Clear();

            if (EditorManager.inst && DebugMode && !LSHelpers.IsUsingInputField())
            {
                if (Input.GetKeyDown(KeyCode.G))
                    EditorConfig.Instance.EditorTheme.Value = EditorConfig.Instance.EditorTheme.Value == ThemeSetting.Legacy ? ThemeSetting.Dark : ThemeSetting.Legacy;
            }
        }

        public static void Clear() => EditorGUIElements.Clear();

        public static IEnumerator RenderElements()
        {
            var theme = CurrentTheme;

            for (int i = 0; i < EditorGUIElements.Count; i++)
                EditorGUIElements[i].ApplyTheme(theme);

            for (int i = 0; i < EditorManager.inst.loadedLevels.Count; i++)
            {
                var level = EditorManager.inst.loadedLevels.Select(x => x as EditorWrapper).ElementAt(i);

                if (level.GameObject)
                {
                    ApplyElement(new Element($"Level Button {i}", "List Button 1", level.GameObject, new List<Component>
                    {
                        level.GameObject.GetComponent<Image>(),
                        level.GameObject.GetComponent<Button>(),
                    }, true, 1, SpriteManager.RoundedSide.W, true));

                    var text = level.GameObject.transform.GetChild(0).gameObject;
                    ApplyElement(new Element($"Level Button {i} Text", "Light Text", text, new List<Component>
                    {
                        text.GetComponent<Text>(),
                    }));
                }

                if (level.CombinerGameObject)
                {
                    ApplyElement(new Element($"Level Combiner Button {i}", "List Button 1", level.CombinerGameObject, new List<Component>
                    {
                        level.CombinerGameObject.GetComponent<Image>(),
                        level.CombinerGameObject.GetComponent<Button>(),
                    }, true, 1, SpriteManager.RoundedSide.W, true));

                    var text = level.CombinerGameObject.transform.GetChild(0).gameObject;
                    ApplyElement(new Element($"Level Button {i} Text", "Light Text", text, new List<Component>
                    {
                        text.GetComponent<Text>(),
                    }));
                }
            }

            if (EditorManager.inst.GetDialog("Editor Properties Popup").Dialog.gameObject.activeInHierarchy)
            {
                RTEditor.inst.RenderPropertiesWindow();
            }

            yield break;
        }

        public static EditorTheme CurrentTheme => EditorThemes[Mathf.Clamp(currentTheme, 0, EditorThemes.Count - 1)];
        public static int currentTheme = 0;

        public static void AddElement(Element element)
        {
            EditorGUIElements.Add(element);
            element.ApplyTheme(CurrentTheme);
        }

        public static void ApplyElement(Element element) => element.ApplyTheme(CurrentTheme);

        public static List<Element> EditorGUIElements { get; set; } = new List<Element>();

        public static List<EditorTheme> EditorThemes { get; set; } = new List<EditorTheme>
        {
            new EditorTheme($"{nameof(ThemeSetting.Legacy)}", new Dictionary<string, Color>
            {
                { "Background", LSColors.HexToColorAlpha("212121FF") },
                { "Preview Cover", LSColors.HexToColorAlpha("191919FF") },
                { "Scrollbar Handle", LSColors.HexToColorAlpha("C8C8C8FF") },
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
                { "Close X", LSColors.HexToColorAlpha("EFEBEFFF") },
                { "Light Text", LSColors.HexToColorAlpha("E5E1E5FF") },
                { "Function 1", LSColors.HexToColorAlpha("4E99F4FF") }, // 0F7BF8FF
                { "Function 1 Text", LSColors.HexToColorAlpha("1B1B1CFF") },
                { "Function 2", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Function 2 Normal", LSColors.HexToColorAlpha("E0E0E0FF") },
                { "Function 2 Highlight", LSColors.HexToColorAlpha("C86F6FFF") },
                { "Function 2 Selected", LSColors.HexToColorAlpha("C86F6FFF") },
                { "Function 2 Pressed", LSColors.HexToColorAlpha("E0E0E0FF") },
                { "Function 2 Disabled", LSColors.HexToColorAlpha("C7C7C780") },
                { "Function 2 Text", LSColors.HexToColorAlpha("323232FF") },
                { "List Button 1", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "List Button 1 Normal", LSColors.HexToColorAlpha("2A2A2AFF") },
                { "List Button 1 Highlight", LSColors.HexToColorAlpha("424242FF") },
                { "List Button 1 Selected", LSColors.HexToColorAlpha("424242FF") },
                { "List Button 1 Pressed", LSColors.HexToColorAlpha("424242FF") },
                { "List Button 1 Disabled", LSColors.HexToColorAlpha("424242FF") },
                { "Search Field 1", LSColors.HexToColorAlpha("2F2F2FFF") },
                { "Add", LSColors.HexToColorAlpha("4DB6ACFF") },
                { "Add Text", LSColors.HexToColorAlpha("202020FF") },
                { "Prefab", LSColors.HexToColorAlpha("383838FF") },
                { "Prefab Text", LSColors.HexToColorAlpha("EFEBEFFF") },
                { "Object", LSColors.HexToColorAlpha("EFEBEFFF") },
                { "Object Text", LSColors.HexToColorAlpha("212121FF") },
                { "Marker", LSColors.HexToColorAlpha("FFAF38FF") },
                { "Marker Text", LSColors.HexToColorAlpha("1C1C1DFF") },
                { "Checkpoint", LSColors.HexToColorAlpha("64B5F6FF") },
                { "Checkpoint Text", LSColors.HexToColorAlpha("EFEBEFFF") },
                { "Background Object", LSColors.HexToColorAlpha("E57373FF") },
                { "Background Object Text", LSColors.HexToColorAlpha("EFEBEFFF") },
                { "Timeline Bar", LSColors.HexToColorAlpha("1B1B1CFF") },
                { "Event/Check", LSColors.HexToColorAlpha("6CCBCFFF") },
                { "Event/Check Text", LSColors.HexToColorAlpha("EFEBEFFF") },
                { "Dropdown 1", LSColors.HexToColorAlpha("EDE9EDFF") },
                { "Dropdown 1 Overlay", LSColors.HexToColorAlpha("373737FF") },
                { "Dropdown 1 Item", LSColors.HexToColorAlpha("F5F5F5FF") },
                { "Toggle 1", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Toggle 1 Check", LSColors.HexToColorAlpha("212121FF") },
                { "Input Field", LSColors.HexToColorAlpha("EFEBEFFF") },
                { "Input Field Text", LSColors.HexToColorAlpha("252525FF") },
                { "Slider 1", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Slider 1 Normal", LSColors.HexToColorAlpha("EFEBEFFF") },
                { "Slider 1 Highlight", LSColors.HexToColorAlpha("F5F5F5FF") },
                { "Slider 1 Selected", LSColors.HexToColorAlpha("F5F5F5FF") },
                { "Slider 1 Pressed", LSColors.HexToColorAlpha("C8C8C8FF") },
                { "Slider 1 Disabled", LSColors.HexToColorAlpha("C8C8C880") },
                { "Slider 1 Handle", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Slider", LSColors.HexToColorAlpha("EEEAEEFF") },
                { "Slider Handle", LSColors.HexToColorAlpha("424242FF") },
                { "Documentation", LSColors.HexToColorAlpha("D89356FF") },
                { "Timeline Background", LSColors.HexToColorAlpha("1B1B1BFF") },
                { "Timeline Scrollbar", LSColors.HexToColorAlpha("686868FF") },
                { "Timeline Scrollbar Normal", LSColors.HexToColorAlpha("676767FF") },
                { "Timeline Scrollbar Highlight", LSColors.HexToColorAlpha("9E9E9EFF") },
                { "Timeline Scrollbar Selected", LSColors.HexToColorAlpha("9D9D9DFF") },
                { "Timeline Scrollbar Pressed", LSColors.HexToColorAlpha("EFEBEFFF") },
                { "Timeline Scrollbar Disabled", LSColors.HexToColorAlpha("676767FF") },
                { "Timeline Scrollbar Base", LSColors.HexToColorAlpha("3E3E42FF") },
                { "Timeline Time Scrollbar", LSColors.HexToColorAlpha("3E3E40FF") },
                { "Title Bar Text", LSColors.HexToColorAlpha("FFFFFFFF") },
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
                { "Warning Confirm", LSColors.HexToColorAlpha("FF3645FF") },
                { "Warning Cancel", LSColors.HexToColorAlpha("4DB5ABFF") },
                { "Notification Background", LSColors.HexToColorAlpha("212121FF") },
                { "Notification Info", LSColors.HexToColorAlpha("404040FF") },
                { "Notification Success", LSColors.HexToColorAlpha("4DB6ACFF") },
                { "Notification Error", LSColors.HexToColorAlpha("E57373FF") },
                { "Notification Warning", LSColors.HexToColorAlpha("FFAF38FF") },
                { "Paste", LSColors.HexToColorAlpha("FFAF38FF") },
                { "Paste Text", LSColors.HexToColorAlpha("1C1C1DFF") },
                { "Tab Color 1", LSColors.HexToColorAlpha("FFE7E7FF") },
                { "Tab Color 1 Normal", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 1 Highlight", new Color(2f, 2f, 2f, 1f) },
                { "Tab Color 1 Selected", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 1 Pressed", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 1 Disabled", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 2", LSColors.HexToColorAlpha("C0ACE1FF") },
                { "Tab Color 2 Normal", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 2 Highlight", new Color(2f, 2f, 2f, 1f) },
                { "Tab Color 2 Selected", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 2 Pressed", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 2 Disabled", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 3", LSColors.HexToColorAlpha("F17BB8FF") },
                { "Tab Color 3 Normal", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 3 Highlight", new Color(2f, 2f, 2f, 1f) },
                { "Tab Color 3 Selected", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 3 Pressed", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 3 Disabled", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 4", LSColors.HexToColorAlpha("2F426DFF") },
                { "Tab Color 4 Normal", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 4 Highlight", new Color(2f, 2f, 2f, 1f) },
                { "Tab Color 4 Selected", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 4 Pressed", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 4 Disabled", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 5", LSColors.HexToColorAlpha("4076DFFF") },
                { "Tab Color 5 Normal", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 5 Highlight", new Color(2f, 2f, 2f, 1f) },
                { "Tab Color 5 Selected", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 5 Pressed", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 5 Disabled", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 6", LSColors.HexToColorAlpha("6CCBCFFF") },
                { "Tab Color 6 Normal", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 6 Highlight", new Color(2f, 2f, 2f, 1f) },
                { "Tab Color 6 Selected", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 6 Pressed", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 6 Disabled", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 7", LSColors.HexToColorAlpha("1B1B1CFF") },
                { "Tab Color 7 Normal", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 7 Highlight", new Color(2f, 2f, 2f, 1f) },
                { "Tab Color 7 Selected", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 7 Pressed", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 7 Disabled", LSColors.HexToColorAlpha("FFFFFFFF") },
            }),
            new EditorTheme($"{nameof(ThemeSetting.Dark)}", new Dictionary<string, Color>
            {
                { "Background", LSColors.HexToColorAlpha("0A0A0AFF") },
                { "Preview Cover", LSColors.HexToColorAlpha("060606FF") },
                { "Scrollbar Handle", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Scrollbar Handle Normal", LSColors.HexToColorAlpha("4C4C4CFF") },
                { "Scrollbar Handle Highlight", LSColors.HexToColorAlpha("606060FF") },
                { "Scrollbar Handle Selected", LSColors.HexToColorAlpha("606060FF") },
                { "Scrollbar Handle Pressed", LSColors.HexToColorAlpha("606060FF") },
                { "Scrollbar Handle Disabled", LSColors.HexToColorAlpha("606060FF") },
                { "Close", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Close Normal", LSColors.HexToColorAlpha("F44336FF") },
                { "Close Highlight", LSColors.HexToColorAlpha("292929FF") },
                { "Close Selected", LSColors.HexToColorAlpha("292929FF") },
                { "Close Pressed", LSColors.HexToColorAlpha("292929FF") },
                { "Close Disabled", LSColors.HexToColorAlpha("292929FF") },
                { "Close X", LSColors.HexToColorAlpha("EFEBEFFF") },
                { "Light Text", LSColors.HexToColorAlpha("E5E1E5FF") },
                { "Function 1", LSColors.HexToColorAlpha("1D4675FF") },
                { "Function 1 Text", LSColors.HexToColorAlpha("E5E1E5FF") },
                { "Function 2", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Function 2 Normal", LSColors.HexToColorAlpha("3D3D3DFF") },
                { "Function 2 Highlight", LSColors.HexToColorAlpha("AF5D73FF") },
                { "Function 2 Selected", LSColors.HexToColorAlpha("3D3D3DFF") },
                { "Function 2 Pressed", LSColors.HexToColorAlpha("AF5D73FF") },
                { "Function 2 Disabled", LSColors.HexToColorAlpha("C7C7C780") },
                { "Function 2 Text", LSColors.HexToColorAlpha("C6C6C6FF") },
                { "List Button 1", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "List Button 1 Normal", LSColors.HexToColorAlpha("111111FF") },
                { "List Button 1 Highlight", LSColors.HexToColorAlpha("282828FF") },
                { "List Button 1 Selected", LSColors.HexToColorAlpha("282828FF") },
                { "List Button 1 Pressed", LSColors.HexToColorAlpha("282828FF") },
                { "List Button 1 Disabled", LSColors.HexToColorAlpha("282828FF") },
                { "Search Field 1", LSColors.HexToColorAlpha("111111FF") },
                { "Add", LSColors.HexToColorAlpha("06ADADFF") },
                { "Add Text", LSColors.HexToColorAlpha("011E1EFF") },
                { "Prefab", LSColors.HexToColorAlpha("1B1B1CFF") },
                { "Prefab Text", LSColors.HexToColorAlpha("EFEBEFFF") },
                { "Object", LSColors.HexToColorAlpha("918B91FF") },
                { "Object Text", LSColors.HexToColorAlpha("EFEBEFFF") },
                { "Marker", LSColors.HexToColorAlpha("C97D14FF") },
                { "Marker Text", LSColors.HexToColorAlpha("EFEBEFFF") },
                { "Checkpoint", LSColors.HexToColorAlpha("2781C6FF") },
                { "Checkpoint Text", LSColors.HexToColorAlpha("EFEBEFFF") },
                { "Background Object", LSColors.HexToColorAlpha("AD4848FF") },
                { "Background Object Text", LSColors.HexToColorAlpha("EFEBEFFF") },
                { "Timeline Bar", LSColors.HexToColorAlpha("030305FF") },
                { "Event/Check", LSColors.HexToColorAlpha("2196A8FF") },
                { "Event/Check Text", LSColors.HexToColorAlpha("EFEBEFFF") },
                { "Dropdown 1", LSColors.HexToColorAlpha("373737FF") },
                { "Dropdown 1 Overlay", LSColors.HexToColorAlpha("EDE9EDFF") },
                { "Dropdown 1 Item", LSColors.HexToColorAlpha("373737FF") },
                { "Toggle 1", LSColors.HexToColorAlpha("212121FF") },
                { "Toggle 1 Check", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Input Field", LSColors.HexToColorAlpha("252525FF") },
                { "Input Field Text", LSColors.HexToColorAlpha("EFEBEFFF") },
                { "Slider 1", LSColors.HexToColorAlpha("565656FF") },
                { "Slider 1 Normal", LSColors.HexToColorAlpha("EFEBEFFF") },
                { "Slider 1 Highlight", LSColors.HexToColorAlpha("F5F5F5FF") },
                { "Slider 1 Selected", LSColors.HexToColorAlpha("F5F5F5FF") },
                { "Slider 1 Pressed", LSColors.HexToColorAlpha("C8C8C8FF") },
                { "Slider 1 Disabled", LSColors.HexToColorAlpha("C8C8C880") },
                { "Slider 1 Handle", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Slider", LSColors.HexToColorAlpha("EEEAEEFF") },
                { "Slider Handle", LSColors.HexToColorAlpha("424242FF") },
                { "Documentation", LSColors.HexToColorAlpha("D89356FF") },
                { "Timeline Background", LSColors.HexToColorAlpha("080808FF") },
                { "Timeline Scrollbar", LSColors.HexToColorAlpha("686868FF") },
                { "Timeline Scrollbar Normal", LSColors.HexToColorAlpha("676767FF") },
                { "Timeline Scrollbar Highlight", LSColors.HexToColorAlpha("9E9E9EFF") },
                { "Timeline Scrollbar Selected", LSColors.HexToColorAlpha("9D9D9DFF") },
                { "Timeline Scrollbar Pressed", LSColors.HexToColorAlpha("EFEBEFFF") },
                { "Timeline Scrollbar Disabled", LSColors.HexToColorAlpha("676767FF") },
                { "Timeline Scrollbar Base", LSColors.HexToColorAlpha("3E3E42FF") },
                { "Timeline Time Scrollbar", LSColors.HexToColorAlpha("3E3E40FF") },
                { "Title Bar Text", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Title Bar Button", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Title Bar Button Normal", LSColors.HexToColorAlpha("1E1E1EFF") },
                { "Title Bar Button Highlight", LSColors.HexToColorAlpha("121212FF") },
                { "Title Bar Button Selected", LSColors.HexToColorAlpha("121212FF") },
                { "Title Bar Button Pressed", LSColors.HexToColorAlpha("121212FF") },
                { "Title Bar Dropdown", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Title Bar Dropdown Normal", LSColors.HexToColorAlpha("121212FF") },
                { "Title Bar Dropdown Highlight", LSColors.HexToColorAlpha("1E1E1EFF") },
                { "Title Bar Dropdown Selected", LSColors.HexToColorAlpha("1E1E1EFF") },
                { "Title Bar Dropdown Pressed", LSColors.HexToColorAlpha("1E1E1EFF") },
                { "Title Bar Dropdown Disabled", LSColors.HexToColorAlpha("1E1E1EFF") },
                { "Warning Confirm", LSColors.HexToColorAlpha("FF3645FF") },
                { "Warning Cancel", LSColors.HexToColorAlpha("4DB5ABFF") },
                { "Notification Background", LSColors.HexToColorAlpha("0A0A0AFF") },
                { "Notification Info", LSColors.HexToColorAlpha("191919FF") },
                { "Notification Success", LSColors.HexToColorAlpha("6EB5D3FF") },
                { "Notification Error", LSColors.HexToColorAlpha("AA3D3DFF") },
                { "Notification Warning", LSColors.HexToColorAlpha("CE8633FF") },
                { "Paste", LSColors.HexToColorAlpha("C18736FF") },
                { "Paste Text", LSColors.HexToColorAlpha("E5E1E5FF") },
                { "Tab Color 1", LSColors.HexToColorAlpha("1395BAFF") },
                { "Tab Color 1 Normal", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 1 Highlight", new Color(2f, 2f, 2f, 1f) },
                { "Tab Color 1 Selected", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 1 Pressed", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 1 Disabled", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 2", LSColors.HexToColorAlpha("0D3C55FF") },
                { "Tab Color 2 Normal", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 2 Highlight", new Color(2f, 2f, 2f, 1f) },
                { "Tab Color 2 Selected", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 2 Pressed", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 2 Disabled", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 3", LSColors.HexToColorAlpha("C02E1DFF") },
                { "Tab Color 3 Normal", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 3 Highlight", new Color(2f, 2f, 2f, 1f) },
                { "Tab Color 3 Selected", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 3 Pressed", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 3 Disabled", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 4", LSColors.HexToColorAlpha("F16C20FF") },
                { "Tab Color 4 Normal", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 4 Highlight", new Color(2f, 2f, 2f, 1f) },
                { "Tab Color 4 Selected", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 4 Pressed", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 4 Disabled", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 5", LSColors.HexToColorAlpha("EBC844FF") },
                { "Tab Color 5 Normal", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 5 Highlight", new Color(2f, 2f, 2f, 1f) },
                { "Tab Color 5 Selected", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 5 Pressed", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 5 Disabled", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 6", LSColors.HexToColorAlpha("A2B86CFF") },
                { "Tab Color 6 Normal", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 6 Highlight", new Color(2f, 2f, 2f, 1f) },
                { "Tab Color 6 Selected", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 6 Pressed", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 6 Disabled", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 7", LSColors.HexToColorAlpha("1B1B1CFF") },
                { "Tab Color 7 Normal", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 7 Highlight", new Color(2f, 2f, 2f, 1f) },
                { "Tab Color 7 Selected", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 7 Pressed", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Tab Color 7 Disabled", LSColors.HexToColorAlpha("FFFFFFFF") },
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

            public Element(string name, string group, GameObject gameObject, List<Component> components, bool canSetRounded = false, int rounded = 0, SpriteManager.RoundedSide roundedSide = SpriteManager.RoundedSide.W, bool isSelectable = false)
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
                try
                {
                    SetRounded();

                    if (string.IsNullOrEmpty(group))
                        return;

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
                    else
                    {
                        Debug.LogError($"{EditorPlugin.className}Failed to assign theme color to {name}.");
                    }
                }
                catch
                {

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
                    if (component is TextMeshProUGUI textMeshPro)
                        textMeshPro.color = color;
                }
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
            }

            public void SetRounded()
            {
                if (!canSetRounded)
                    return;

                var canSet = EditorConfig.Instance.RoundedUI.Value;

                foreach (var component in Components)
                {
                    if (component is Image image)
                    {
                        if (Rounded != 0 && canSet)
                            SpriteManager.SetRoundedSprite(image, Rounded, RoundedSide);
                        else
                            image.sprite = null;
                    }
                }
            }

            public override string ToString() => name;
        }
    }
}
