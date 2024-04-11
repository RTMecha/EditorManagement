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
using EditorManagement.Functions.Components;

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

            try
            {
                for (int i = 0; i < TemporaryEditorGUIElements.Count; i++)
                {
                    var element = TemporaryEditorGUIElements.ElementAt(i).Value;

                    element.ApplyTheme(theme);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            if (RTEditor.inst && RTEditor.inst.layerType == RTEditor.LayerType.Events)
            {
                RTEventEditor.inst.RenderLayerBins();
                if (EventEditor.inst.dialogRight.gameObject.activeInHierarchy)
                    RTEventEditor.inst.RenderEventsDialog();
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

        public static void ApplyElement(Element element)
        {
            element.ApplyTheme(CurrentTheme);

            if (element.GameObject != null)
            {
                var id = LSText.randomNumString(16);
                element.GameObject.AddComponent<EditorThemeElement>().Init(element, id);
                if (!TemporaryEditorGUIElements.ContainsKey(id))
                    TemporaryEditorGUIElements.Add(id, element);
            }
        }

        public static List<Element> EditorGUIElements { get; set; } = new List<Element>();
        public static Dictionary<string, Element> TemporaryEditorGUIElements { get; set; } = new Dictionary<string, Element>();

        public static List<EditorTheme> EditorThemes { get; set; } = new List<EditorTheme>
        {
            new EditorTheme($"{nameof(ThemeSetting.Legacy)}", new Dictionary<string, Color>
            {
                { "Background", LSColors.HexToColorAlpha("212121FF") },
                { "Background 2", LSColors.HexToColorAlpha("1B1B1CFF") },
                { "Background 3", LSColors.HexToColorAlpha("373738FF") },
                { "Preview Cover", LSColors.HexToColorAlpha("191919FF") },

                { "Scrollbar Handle", LSColors.HexToColorAlpha("C8C8C8FF") },
                { "Scrollbar Handle Normal", LSColors.HexToColorAlpha("C7C7C7FF") },
                { "Scrollbar Handle Highlight", LSColors.HexToColorAlpha("414141FF") },
                { "Scrollbar Handle Selected", LSColors.HexToColorAlpha("414141FF") },
                { "Scrollbar Handle Pressed", LSColors.HexToColorAlpha("414141FF") },
                { "Scrollbar Handle Disabled", LSColors.HexToColorAlpha("414141FF") },

                { "Scrollbar 2", LSColors.HexToColorAlpha("EEEAEEFF") },
                { "Scrollbar Handle 2", LSColors.HexToColorAlpha("424242FF") },
                { "Scrollbar Handle 2 Normal", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Scrollbar Handle 2 Highlight", LSColors.HexToColorAlpha("F5F5F5FF") },
                { "Scrollbar Handle 2 Selected", LSColors.HexToColorAlpha("414141FF") },
                { "Scrollbar Handle 2 Pressed", LSColors.HexToColorAlpha("F5F5F5FF") },
                { "Scrollbar Handle 2 Disabled", LSColors.HexToColorAlpha("C8C8C880") },

                { "Close", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Close Normal", LSColors.HexToColorAlpha("F44336FF") },
                { "Close Highlight", LSColors.HexToColorAlpha("292929FF") },
                { "Close Selected", LSColors.HexToColorAlpha("292929FF") },
                { "Close Pressed", LSColors.HexToColorAlpha("292929FF") },
                { "Close Disabled", LSColors.HexToColorAlpha("292929FF") },
                { "Close X", LSColors.HexToColorAlpha("EFEBEFFF") },

                { "Light Text", LSColors.HexToColorAlpha("E5E1E5FF") },
                { "Dark Text", LSColors.HexToColorAlpha("323232FF") },

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

                { "List Button 2", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "List Button 2 Normal", LSColors.HexToColorAlpha("EDE9EDFF") },
                { "List Button 2 Highlight", LSColors.HexToColorAlpha("C8C8C8FF") },
                { "List Button 2 Selected", LSColors.HexToColorAlpha("C8C8C8FF") },
                { "List Button 2 Pressed", LSColors.HexToColorAlpha("C8C8C8FF") },
                { "List Button 2 Disabled", LSColors.HexToColorAlpha("96969680") },

                { "Search Field 1", LSColors.HexToColorAlpha("2F2F2FFF") },
                { "Search Field 1 Text", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Search Field 2", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Search Field 2 Text", LSColors.HexToColorAlpha("2F2F2FFF") },

                { "Add", LSColors.HexToColorAlpha("4DB6ACFF") },
                { "Add Text", LSColors.HexToColorAlpha("202020FF") },
                { "Delete", LSColors.HexToColorAlpha("E67474FF") },
                { "Delete Text", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Delete Keyframe BG", LSColors.HexToColorAlpha("EEEAEEFF") },
                { "Delete Keyframe Button", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Delete Keyframe Button Normal", LSColors.HexToColorAlpha("F34235FF") },
                { "Delete Keyframe Button Highlight", LSColors.HexToColorAlpha("212121FF") },
                { "Delete Keyframe Button Selected", LSColors.HexToColorAlpha("212121FF") },
                { "Delete Keyframe Button Pressed", LSColors.HexToColorAlpha("222222FF") },
                { "Delete Keyframe Button Disabled", LSColors.HexToColorAlpha("37373880") },

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

                { "Copy", LSColors.HexToColorAlpha("3DADFFFF") },
                { "Copy Text", LSColors.HexToColorAlpha("1C1C1DFF") },
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

                { "Event Color 1", LSColors.HexToColorAlpha("673AB7FF") }, // 1
                { "Event Color 2", LSColors.HexToColorAlpha("3E51B4FF") }, // 2
                { "Event Color 3", LSColors.HexToColorAlpha("2196F3FF") }, // 3
                { "Event Color 4", LSColors.HexToColorAlpha("03A9F4FF") }, // 4
                { "Event Color 5", LSColors.HexToColorAlpha("00BCD4FF") }, // 5
                { "Event Color 6", LSColors.HexToColorAlpha("009688FF") }, // 6
                { "Event Color 7", LSColors.HexToColorAlpha("4BAF50FF") }, // 7
                { "Event Color 8", LSColors.HexToColorAlpha("7CB341FF") }, // 8
                { "Event Color 9", LSColors.HexToColorAlpha("AFB42BFF") }, // 9
                { "Event Color 10", LSColors.HexToColorAlpha("FFC107FF") }, // 10
                { "Event Color 11", LSColors.HexToColorAlpha("FF9800FF") }, // 11
                { "Event Color 12", LSColors.HexToColorAlpha("B96000FF") }, // 12
                { "Event Color 13", LSColors.HexToColorAlpha("B12411FF") }, // 13
                { "Event Color 14", LSColors.HexToColorAlpha("B12424FF") }, // 14
                { "Event Color 15", LSColors.HexToColorAlpha("64B4F6FF") }, // 15

                { "Event Color 1 Keyframe", LSColors.HexToColorAlpha("673AB7FF") }, // 1
                { "Event Color 2 Keyframe", LSColors.HexToColorAlpha("3E51B4FF") }, // 2
                { "Event Color 3 Keyframe", LSColors.HexToColorAlpha("2196F3FF") }, // 3
                { "Event Color 4 Keyframe", LSColors.HexToColorAlpha("03A9F4FF") }, // 4
                { "Event Color 5 Keyframe", LSColors.HexToColorAlpha("00BCD4FF") }, // 5
                { "Event Color 6 Keyframe", LSColors.HexToColorAlpha("009688FF") }, // 6
                { "Event Color 7 Keyframe", LSColors.HexToColorAlpha("4BAF50FF") }, // 7
                { "Event Color 8 Keyframe", LSColors.HexToColorAlpha("7CB341FF") }, // 8
                { "Event Color 9 Keyframe", LSColors.HexToColorAlpha("AFB42BFF") }, // 9
                { "Event Color 10 Keyframe", LSColors.HexToColorAlpha("FFC107FF") }, // 10
                { "Event Color 11 Keyframe", LSColors.HexToColorAlpha("FF9800FF") }, // 11
                { "Event Color 12 Keyframe", LSColors.HexToColorAlpha("B96000FF") }, // 12
                { "Event Color 13 Keyframe", LSColors.HexToColorAlpha("B12411FF") }, // 13
                { "Event Color 14 Keyframe", LSColors.HexToColorAlpha("B12424FF") }, // 14
                { "Event Color 15 Keyframe", LSColors.HexToColorAlpha("64B4F6FF") }, // 15
                { "Event Color 1 Editor", LSColors.HexToColorAlpha("564B6AFF") }, // 1
                { "Event Color 2 Editor", LSColors.HexToColorAlpha("41445EFF") }, // 2
                { "Event Color 3 Editor", LSColors.HexToColorAlpha("44627AFF") }, // 3
                { "Event Color 4 Editor", LSColors.HexToColorAlpha("315B6EFF") }, // 4
                { "Event Color 5 Editor", LSColors.HexToColorAlpha("3E6D73FF") }, // 5
                { "Event Color 6 Editor", LSColors.HexToColorAlpha("305653FF") }, // 6
                { "Event Color 7 Editor", LSColors.HexToColorAlpha("506951FF") }, // 7
                { "Event Color 8 Editor", LSColors.HexToColorAlpha("515E41FF") }, // 8
                { "Event Color 9 Editor", LSColors.HexToColorAlpha("676945FF") }, // 9
                { "Event Color 10 Editor", LSColors.HexToColorAlpha("726335FF") }, // 10
                { "Event Color 11 Editor", LSColors.HexToColorAlpha("FF9800FF") }, // 11
                { "Event Color 12 Editor", LSColors.HexToColorAlpha("FF5800FF") }, // 12
                { "Event Color 13 Editor", LSColors.HexToColorAlpha("FF2509FF") }, // 13
                { "Event Color 14 Editor", LSColors.HexToColorAlpha("FF0F0FFF") }, // 14

                { "Object Keyframe Color 1", LSColors.HexToColorAlpha("F44336FF") }, // 1
                { "Object Keyframe Color 2", LSColors.HexToColorAlpha("4CAF50FF") }, // 2
                { "Object Keyframe Color 3", LSColors.HexToColorAlpha("2196F3FF") }, // 3
                { "Object Keyframe Color 4", LSColors.HexToColorAlpha("FFC107FF") }, // 4
            }),
            new EditorTheme($"{nameof(ThemeSetting.Dark)}", new Dictionary<string, Color>
            {
                { "Background", LSColors.HexToColorAlpha("0A0A0AFF") },
                { "Background 2", LSColors.HexToColorAlpha("060606FF") },
                { "Background 3", LSColors.HexToColorAlpha("0C0C0CFF") },
                { "Preview Cover", LSColors.HexToColorAlpha("060606FF") },

                { "Scrollbar Handle", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Scrollbar Handle Normal", LSColors.HexToColorAlpha("4C4C4CFF") },
                { "Scrollbar Handle Highlight", LSColors.HexToColorAlpha("606060FF") },
                { "Scrollbar Handle Selected", LSColors.HexToColorAlpha("606060FF") },
                { "Scrollbar Handle Pressed", LSColors.HexToColorAlpha("606060FF") },
                { "Scrollbar Handle Disabled", LSColors.HexToColorAlpha("606060FF") },

                { "Scrollbar 2", LSColors.HexToColorAlpha("262526FF") },
                { "Scrollbar Handle 2", LSColors.HexToColorAlpha("424242FF") },
                { "Scrollbar Handle 2 Normal", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Scrollbar Handle 2 Highlight", LSColors.HexToColorAlpha("F5F5F5FF") },
                { "Scrollbar Handle 2 Selected", LSColors.HexToColorAlpha("414141FF") },
                { "Scrollbar Handle 2 Pressed", LSColors.HexToColorAlpha("F5F5F5FF") },
                { "Scrollbar Handle 2 Disabled", LSColors.HexToColorAlpha("C8C8C880") },

                { "Close", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Close Normal", LSColors.HexToColorAlpha("F44336FF") },
                { "Close Highlight", LSColors.HexToColorAlpha("292929FF") },
                { "Close Selected", LSColors.HexToColorAlpha("292929FF") },
                { "Close Pressed", LSColors.HexToColorAlpha("292929FF") },
                { "Close Disabled", LSColors.HexToColorAlpha("292929FF") },
                { "Close X", LSColors.HexToColorAlpha("EFEBEFFF") },

                { "Light Text", LSColors.HexToColorAlpha("E5E1E5FF") },
                { "Dark Text", LSColors.HexToColorAlpha("E5E1E5FF") },

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

                { "List Button 2", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "List Button 2 Normal", LSColors.HexToColorAlpha("111111FF") },
                { "List Button 2 Highlight", LSColors.HexToColorAlpha("282828FF") },
                { "List Button 2 Selected", LSColors.HexToColorAlpha("282828FF") },
                { "List Button 2 Pressed", LSColors.HexToColorAlpha("282828FF") },
                { "List Button 2 Disabled", LSColors.HexToColorAlpha("282828FF") },

                { "Search Field 1", LSColors.HexToColorAlpha("111111FF") },
                { "Search Field 1 Text", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Search Field 2", LSColors.HexToColorAlpha("111111FF") },
                { "Search Field 2 Text", LSColors.HexToColorAlpha("FFFFFFFF") },

                { "Add", LSColors.HexToColorAlpha("06ADADFF") },
                { "Add Text", LSColors.HexToColorAlpha("011E1EFF") },
                { "Delete", LSColors.HexToColorAlpha("7F2626FF") },
                { "Delete Text", LSColors.HexToColorAlpha("EFE1E1FF") },
                { "Delete Keyframe BG", LSColors.HexToColorAlpha("EEEAEEFF") },
                { "Delete Keyframe Button", LSColors.HexToColorAlpha("FFFFFFFF") },
                { "Delete Keyframe Button Normal", LSColors.HexToColorAlpha("F34235FF") },
                { "Delete Keyframe Button Highlight", LSColors.HexToColorAlpha("212121FF") },
                { "Delete Keyframe Button Selected", LSColors.HexToColorAlpha("212121FF") },
                { "Delete Keyframe Button Pressed", LSColors.HexToColorAlpha("222222FF") },
                { "Delete Keyframe Button Disabled", LSColors.HexToColorAlpha("37373880") },

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

                { "Copy", LSColors.HexToColorAlpha("1D4675FF") },
                { "Copy Text", LSColors.HexToColorAlpha("E5E1E5FF") },
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

                { "Event Color 1", LSColors.HexToColorAlpha("241A35FF") }, // 1
			    { "Event Color 2", LSColors.HexToColorAlpha("1F2444FF") }, // 2
			    { "Event Color 3", LSColors.HexToColorAlpha("193042FF") }, // 3
			    { "Event Color 4", LSColors.HexToColorAlpha("102C38FF") }, // 4
			    { "Event Color 5", LSColors.HexToColorAlpha("14393DFF") }, // 5
			    { "Event Color 6", LSColors.HexToColorAlpha("164440FF") }, // 6
			    { "Event Color 7", LSColors.HexToColorAlpha("274928FF") }, // 7
			    { "Event Color 8", LSColors.HexToColorAlpha("2E3D1AFF") }, // 8
			    { "Event Color 9", LSColors.HexToColorAlpha("404225FF") }, // 9
			    { "Event Color 10", LSColors.HexToColorAlpha("594711FF") }, // 10
			    { "Event Color 11", LSColors.HexToColorAlpha("562E13FF") }, // 11
			    { "Event Color 12", LSColors.HexToColorAlpha("66280EFF") }, // 12
			    { "Event Color 13", LSColors.HexToColorAlpha("561106FF") }, // 13
			    { "Event Color 14", LSColors.HexToColorAlpha("3D0000FF") }, // 14
			    { "Event Color 15", LSColors.HexToColorAlpha("1168AFFF") }, // 15

                { "Event Color 1 Keyframe", LSColors.HexToColorAlpha("673AB7FF") }, // 1
                { "Event Color 2 Keyframe", LSColors.HexToColorAlpha("3E51B4FF") }, // 2
                { "Event Color 3 Keyframe", LSColors.HexToColorAlpha("2196F3FF") }, // 3
                { "Event Color 4 Keyframe", LSColors.HexToColorAlpha("03A9F4FF") }, // 4
                { "Event Color 5 Keyframe", LSColors.HexToColorAlpha("00BCD4FF") }, // 5
                { "Event Color 6 Keyframe", LSColors.HexToColorAlpha("009688FF") }, // 6
                { "Event Color 7 Keyframe", LSColors.HexToColorAlpha("4BAF50FF") }, // 7
                { "Event Color 8 Keyframe", LSColors.HexToColorAlpha("7CB341FF") }, // 8
                { "Event Color 9 Keyframe", LSColors.HexToColorAlpha("AFB42BFF") }, // 9
                { "Event Color 10 Keyframe", LSColors.HexToColorAlpha("FFC107FF") }, // 10
                { "Event Color 11 Keyframe", LSColors.HexToColorAlpha("FF9800FF") }, // 11
                { "Event Color 12 Keyframe", LSColors.HexToColorAlpha("B96000FF") }, // 12
                { "Event Color 13 Keyframe", LSColors.HexToColorAlpha("B12411FF") }, // 13
                { "Event Color 14 Keyframe", LSColors.HexToColorAlpha("B12424FF") }, // 14
                { "Event Color 15 Keyframe", LSColors.HexToColorAlpha("64B4F6FF") }, // 15

                { "Event Color 1 Editor", LSColors.HexToColorAlpha("564B6AFF") }, // 1
                { "Event Color 2 Editor", LSColors.HexToColorAlpha("41445EFF") }, // 2
                { "Event Color 3 Editor", LSColors.HexToColorAlpha("44627AFF") }, // 3
                { "Event Color 4 Editor", LSColors.HexToColorAlpha("315B6EFF") }, // 4
                { "Event Color 5 Editor", LSColors.HexToColorAlpha("3E6D73FF") }, // 5
                { "Event Color 6 Editor", LSColors.HexToColorAlpha("305653FF") }, // 6
                { "Event Color 7 Editor", LSColors.HexToColorAlpha("506951FF") }, // 7
                { "Event Color 8 Editor", LSColors.HexToColorAlpha("515E41FF") }, // 8
                { "Event Color 9 Editor", LSColors.HexToColorAlpha("676945FF") }, // 9
                { "Event Color 10 Editor", LSColors.HexToColorAlpha("726335FF") }, // 10
                { "Event Color 11 Editor", LSColors.HexToColorAlpha("FF9800FF") }, // 11
                { "Event Color 12 Editor", LSColors.HexToColorAlpha("FF5800FF") }, // 12
                { "Event Color 13 Editor", LSColors.HexToColorAlpha("FF2509FF") }, // 13
                { "Event Color 14 Editor", LSColors.HexToColorAlpha("FF0F0FFF") }, // 14

                { "Object Keyframe Color 1", LSColors.HexToColorAlpha("E57373FF") }, // 1
                { "Object Keyframe Color 2", LSColors.HexToColorAlpha("81C784FF") }, // 2
                { "Object Keyframe Color 3", LSColors.HexToColorAlpha("64B5F6FF") }, // 3
                { "Object Keyframe Color 4", LSColors.HexToColorAlpha("FFB74DFF") }, // 4
            }),
        };

        public static Dictionary<string, EditorTheme> EditorThemesDictionary => EditorThemes.ToDictionary(x => x.name, x => x);

        public static void AddDropdown(Dropdown dropdown, string name)
        {
            AddElement(new Element(name, "Dropdown 1", dropdown.gameObject, new List<Component>
            {
                dropdown.image,
            }, true, 1, SpriteManager.RoundedSide.W));

            AddElement(new Element($"{name} Text", "Dropdown 1 Overlay", dropdown.captionText.gameObject, new List<Component>
            {
                dropdown.captionText,
            }));

            AddElement(new Element($"{name} Arrow", "Dropdown 1 Overlay", dropdown.transform.Find("Arrow").gameObject, new List<Component>
            {
                dropdown.transform.Find("Arrow").gameObject.GetComponent<Image>(),
            }));

            if (dropdown.captionImage)
                AddElement(new Element($"{name} Preview", "Dropdown 1 Overlay", dropdown.captionImage.gameObject, new List<Component>
                {
                    dropdown.captionImage,
                }));

            var template = dropdown.transform.Find("Template").gameObject;
            AddElement(new Element($"{name} Template", "Dropdown 1", template, new List<Component>
            {
                template.GetComponent<Image>(),
            }, true, 1, SpriteManager.RoundedSide.Bottom));

            var templateItem = template.transform.Find("Viewport/Content/Item");
            var templateItemBG = templateItem.Find("Item Background").gameObject;
            AddElement(new Element($"{name} Template", "Dropdown 1 Item", templateItemBG, new List<Component>
            {
                templateItemBG.GetComponent<Image>(),
            }, true, 1, SpriteManager.RoundedSide.W));

            var templateItemCheckmark = templateItem.Find("Item Checkmark").gameObject;
            AddElement(new Element($"{name} Template Checkmark", "Dropdown 1 Overlay", templateItemCheckmark, new List<Component>
            {
                templateItemCheckmark.GetComponent<Image>(),
            }));

            var templateItemLabel = templateItem.Find("Item Label").gameObject;
            AddElement(new Element($"{name} Template Label", "Dropdown 1 Overlay", templateItemLabel, new List<Component>
            {
                templateItemLabel.GetComponent<Text>(),
            }));

        }

        public static void AddInputField(InputField inputField, string name, string group)
        {
            inputField.image.fillCenter = true;
            AddElement(new Element(name, group, inputField.gameObject, new List<Component>
            {
                inputField.image,
            }, true, 1, SpriteManager.RoundedSide.W));

            AddElement(new Element($"{name} Text", $"{group} Text", inputField.textComponent.gameObject, new List<Component>
            {
                inputField.textComponent,
            }));
        }
        
        public static void ApplyInputField(InputField inputField, string name, string group)
        {
            inputField.image.fillCenter = true;
            ApplyElement(new Element(name, group, inputField.gameObject, new List<Component>
            {
                inputField.image,
            }, true, 1, SpriteManager.RoundedSide.W));

            ApplyElement(new Element($"{name} Text", $"{group} Text", inputField.textComponent.gameObject, new List<Component>
            {
                inputField.textComponent,
            }));
        }

        public static void AddInputFields(GameObject gameObject, bool self, string name, bool selfInput = false, bool searchChildren = true)
        {
            if (!searchChildren)
            {
                var inputField = gameObject.GetComponent<InputField>();

                if (!inputField)
                    return;

                var input = selfInput ? inputField.transform : gameObject.transform.Find("input") ?? gameObject.transform.Find("Input");

                AddElement(new Element($"{name} Value", "Input Field", input.gameObject, new List<Component>
                {
                    selfInput ? inputField.image : input.GetComponent<Image>(),
                }, true, 1, SpriteManager.RoundedSide.W));

                AddElement(new Element($"{name} Value Text", "Input Field Text", inputField.textComponent.gameObject, new List<Component>
                {
                    inputField.textComponent,
                }));

                var buttonLeft = self ? gameObject.transform.Find("<") : gameObject.transform.parent.Find("<");
                var buttonRight = self ? gameObject.transform.Find(">") : gameObject.transform.parent.Find(">");

                if (!buttonLeft || !buttonRight)
                    return;

                var buttonLeftComponent = buttonLeft.GetComponent<Button>();
                var buttonRightComponent = buttonRight.GetComponent<Button>();

                UnityEngine.Object.Destroy(buttonLeftComponent.GetComponent<Animator>());
                buttonLeftComponent.transition = Selectable.Transition.ColorTint;

                UnityEngine.Object.Destroy(buttonRightComponent.GetComponent<Animator>());
                buttonRightComponent.transition = Selectable.Transition.ColorTint;

                AddElement(new Element($"{name} Button", "Function 2", buttonLeft.gameObject, new List<Component>
                {
                    buttonLeftComponent,
                    buttonLeftComponent.image
                }, isSelectable: true));

                AddElement(new Element($"{name} Button", "Function 2", buttonRight.gameObject, new List<Component>
                {
                    buttonRightComponent,
                    buttonRightComponent.image
                }, isSelectable: true));

                return;
            }
            
            for (int j = 0; j < gameObject.transform.childCount; j++)
            {
                var child = gameObject.transform.GetChild(j);

                var inputField = child.GetComponent<InputField>();
                var input = selfInput ? inputField.transform : child.Find("input") ?? child.Find("Input");

                if (!inputField)
                    continue;

                AddElement(new Element($"{name} Value", "Input Field", input.gameObject, new List<Component>
                {
                    selfInput ? inputField.image : input.GetComponent<Image>(),
                }, true, 1, SpriteManager.RoundedSide.W));

                AddElement(new Element($"{name} Value Text", "Input Field Text", inputField.textComponent.gameObject, new List<Component>
                {
                    inputField.textComponent,
                }));

                var buttonLeft = self ? child.Find("<") : child.parent.Find("<");
                var buttonRight = self ? child.Find(">") : child.parent.Find(">");

                if (!buttonLeft || !buttonRight)
                    continue;

                var buttonLeftComponent = buttonLeft.GetComponent<Button>();
                var buttonRightComponent = buttonRight.GetComponent<Button>();

                UnityEngine.Object.Destroy(buttonLeftComponent.GetComponent<Animator>());
                buttonLeftComponent.transition = Selectable.Transition.ColorTint;

                UnityEngine.Object.Destroy(buttonRightComponent.GetComponent<Animator>());
                buttonRightComponent.transition = Selectable.Transition.ColorTint;

                AddElement(new Element($"{name} Button", "Function 2", buttonLeft.gameObject, new List<Component>
                {
                    buttonLeftComponent,
                    buttonLeftComponent.image
                }, isSelectable: true));

                AddElement(new Element($"{name} Button", "Function 2", buttonRight.gameObject, new List<Component>
                {
                    buttonRightComponent,
                    buttonRightComponent.image
                }, isSelectable: true));
            }
        }

        public static void AddToggle(Toggle toggle, string name, Text text = null)
        {
            AddElement(new Element(name, "Toggle 1", toggle.gameObject, new List<Component>
            {
                toggle.image,
            }, true, 1, SpriteManager.RoundedSide.W));

            AddElement(new Element($"{name} Checkmark", "Toggle 1 Check", toggle.graphic.gameObject, new List<Component>
            {
                toggle.graphic,
            }));

            if (text)
            {
                AddElement(new Element($"{name} Text", "Toggle 1 Check", text.gameObject, new List<Component>
                {
                    text,
                }));
                return;
            }

            if (toggle.transform.Find("Text"))
                AddElement(new Element($"{name} Text", "Toggle 1 Check", toggle.transform.Find("Text").gameObject, new List<Component>
                {
                    toggle.transform.Find("Text").GetComponent<Text>(),
                }));

            if (toggle.transform.Find("text"))
                AddElement(new EditorThemeManager.Element($"{name} Text", "Toggle 1 Check", toggle.transform.Find("text").gameObject, new List<Component>
                {
                    toggle.transform.Find("text").GetComponent<Text>(),
                }));
        }

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
                        Debug.LogError($"{EditorPlugin.className}Failed to assign theme color ({group}) to {name}.");
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
