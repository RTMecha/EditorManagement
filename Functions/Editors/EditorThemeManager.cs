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
        public static void Update()
        {
            if (EditorManager.inst == null && EditorGUIElements.Count > 0)
                Clear();
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

            if (element.GameObject == null)
                return;

            var id = LSText.randomNumString(16);
            element.GameObject.AddComponent<EditorThemeElement>().Init(element, id);

            if (!TemporaryEditorGUIElements.ContainsKey(id))
                TemporaryEditorGUIElements.Add(id, element);
        }

        public static List<Element> EditorGUIElements { get; set; } = new List<Element>();
        public static Dictionary<string, Element> TemporaryEditorGUIElements { get; set; } = new Dictionary<string, Element>();

        public static List<EditorTheme> EditorThemes { get; set; } = new List<EditorTheme>
        {
            new EditorTheme($"{nameof(ThemeSetting.Legacy)}", new Dictionary<ThemeGroup, Color>
            {
                { ThemeGroup.Background_1, LSColors.HexToColorAlpha("212121FF") },
                { ThemeGroup.Background_2, LSColors.HexToColorAlpha("1B1B1CFF") },
                { ThemeGroup.Background_3, LSColors.HexToColorAlpha("373738FF") },
                { ThemeGroup.Preview_Cover, LSColors.HexToColorAlpha("191919FF") },

                { ThemeGroup.Scrollbar_1_Handle, LSColors.HexToColorAlpha("C8C8C8FF") },
                { ThemeGroup.Scrollbar_1_Handle_Normal, LSColors.HexToColorAlpha("C7C7C7FF") },
                { ThemeGroup.Scrollbar_1_Handle_Highlighted, LSColors.HexToColorAlpha("414141FF") },
                { ThemeGroup.Scrollbar_1_Handle_Selected, LSColors.HexToColorAlpha("414141FF") },
                { ThemeGroup.Scrollbar_1_Handle_Pressed, LSColors.HexToColorAlpha("414141FF") },
                { ThemeGroup.Scrollbar_1_Handle_Disabled, LSColors.HexToColorAlpha("414141FF") },

                { ThemeGroup.Scrollbar_2, LSColors.HexToColorAlpha("EEEAEEFF") },
                { ThemeGroup.Scrollbar_2_Handle, LSColors.HexToColorAlpha("424242FF") },
                { ThemeGroup.Scrollbar_2_Handle_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Scrollbar_2_Handle_Hightlighted, LSColors.HexToColorAlpha("F5F5F5FF") },
                { ThemeGroup.Scrollbar_2_Handle_Selected, LSColors.HexToColorAlpha("414141FF") },
                { ThemeGroup.Scrollbar_2_Handle_Pressed, LSColors.HexToColorAlpha("F5F5F5FF") },
                { ThemeGroup.Scrollbar_2_Handle_Disabled, LSColors.HexToColorAlpha("C8C8C880") },

                { ThemeGroup.Close, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Close_Normal, LSColors.HexToColorAlpha("F44336FF") },
                { ThemeGroup.Close_Highlighted, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Close_Selected, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Close_Pressed, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Close_Disabled, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Close_X, LSColors.HexToColorAlpha("EFEBEFFF") },

                { ThemeGroup.Picker, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Picker_Normal, LSColors.HexToColorAlpha("0E7BF7FF") },
                { ThemeGroup.Picker_Highlighted, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Picker_Selected, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Picker_Pressed, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Picker_Disabled, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Picker_Icon, LSColors.HexToColorAlpha("EFEBEFFF") },

                { ThemeGroup.Light_Text, LSColors.HexToColorAlpha("E5E1E5FF") },
                { ThemeGroup.Dark_Text, LSColors.HexToColorAlpha("323232FF") },

                { ThemeGroup.Function_1, LSColors.HexToColorAlpha("4E99F4FF") }, // 0F7BF8FF
                { ThemeGroup.Function_1_Text, LSColors.HexToColorAlpha("1B1B1CFF") },
                { ThemeGroup.Function_2, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Function_2_Normal, LSColors.HexToColorAlpha("E0E0E0FF") },
                { ThemeGroup.Function_2_Highlighted, LSColors.HexToColorAlpha("C86F6FFF") },
                { ThemeGroup.Function_2_Selected, LSColors.HexToColorAlpha("C86F6FFF") },
                { ThemeGroup.Function_2_Pressed, LSColors.HexToColorAlpha("E0E0E0FF") },
                { ThemeGroup.Function_2_Disabled, LSColors.HexToColorAlpha("C7C7C780") },
                { ThemeGroup.Function_2_Text, LSColors.HexToColorAlpha("323232FF") },
                { ThemeGroup.Function_3, LSColors.HexToColorAlpha("EDE9EDFF") },
                { ThemeGroup.Function_3_Text, LSColors.HexToColorAlpha("0A0A0AFF") },

                { ThemeGroup.List_Button_1, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.List_Button_1_Normal, LSColors.HexToColorAlpha("2A2A2AFF") },
                { ThemeGroup.List_Button_1_Highlighted, LSColors.HexToColorAlpha("424242FF") },
                { ThemeGroup.List_Button_1_Selected, LSColors.HexToColorAlpha("424242FF") },
                { ThemeGroup.List_Button_1_Pressed, LSColors.HexToColorAlpha("424242FF") },
                { ThemeGroup.List_Button_1_Disabled, LSColors.HexToColorAlpha("424242FF") },

                { ThemeGroup.List_Button_2, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.List_Button_2_Normal, LSColors.HexToColorAlpha("EDE9EDFF") },
                { ThemeGroup.List_Button_2_Highlighted, LSColors.HexToColorAlpha("C8C8C8FF") },
                { ThemeGroup.List_Button_2_Selected, LSColors.HexToColorAlpha("C8C8C8FF") },
                { ThemeGroup.List_Button_2_Pressed, LSColors.HexToColorAlpha("C8C8C8FF") },
                { ThemeGroup.List_Button_2_Disabled, LSColors.HexToColorAlpha("96969680") },
                { ThemeGroup.List_Button_2_Text, LSColors.HexToColorAlpha("323232FF") },

                { ThemeGroup.Back_Button, LSColors.HexToColorAlpha("90A4AEFF") },
                { ThemeGroup.Back_Button_Text, LSColors.HexToColorAlpha("222222FF") },
                { ThemeGroup.Folder_Button, LSColors.HexToColorAlpha("BCAAA4FF") },
                { ThemeGroup.Folder_Button_Text, LSColors.HexToColorAlpha("323232FF") },
                { ThemeGroup.File_Button, LSColors.HexToColorAlpha("4076DFFF") },
                { ThemeGroup.File_Button_Text, LSColors.HexToColorAlpha("F5F5F5FF") },

                { ThemeGroup.Search_Field_1, LSColors.HexToColorAlpha("2F2F2FFF") },
                { ThemeGroup.Search_Field_1_Text, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Search_Field_2, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Search_Field_2_Text, LSColors.HexToColorAlpha("2F2F2FFF") },

                { ThemeGroup.Add, LSColors.HexToColorAlpha("4DB6ACFF") },
                { ThemeGroup.Add_Text, LSColors.HexToColorAlpha("202020FF") },
                { ThemeGroup.Delete, LSColors.HexToColorAlpha("E54444FF") },
                { ThemeGroup.Delete_Text, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Delete_Keyframe_BG, LSColors.HexToColorAlpha("EEEAEEFF") },
                { ThemeGroup.Delete_Keyframe_Button, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Delete_Keyframe_Button_Normal, LSColors.HexToColorAlpha("F34235FF") },
                { ThemeGroup.Delete_Keyframe_Button_Highlighted, LSColors.HexToColorAlpha("212121FF") },
                { ThemeGroup.Delete_Keyframe_Button_Selected, LSColors.HexToColorAlpha("212121FF") },
                { ThemeGroup.Delete_Keyframe_Button_Pressed, LSColors.HexToColorAlpha("222222FF") },
                { ThemeGroup.Delete_Keyframe_Button_Disabled, LSColors.HexToColorAlpha("37373880") },

                { ThemeGroup.Prefab, LSColors.HexToColorAlpha("383838FF") },
                { ThemeGroup.Prefab_Text, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Object, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Object_Text, LSColors.HexToColorAlpha("212121FF") },
                { ThemeGroup.Marker, LSColors.HexToColorAlpha("FFAF38FF") },
                { ThemeGroup.Marker_Text, LSColors.HexToColorAlpha("1C1C1DFF") },
                { ThemeGroup.Checkpoint, LSColors.HexToColorAlpha("64B5F6FF") },
                { ThemeGroup.Checkpoint_Text, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Background_Object, LSColors.HexToColorAlpha("E57373FF") },
                { ThemeGroup.Background_Object_Text, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Timeline_Bar, LSColors.HexToColorAlpha("1B1B1CFF") },
                { ThemeGroup.Event_Check, LSColors.HexToColorAlpha("6CCBCFFF") },
                { ThemeGroup.Event_Check_Text, LSColors.HexToColorAlpha("EFEBEFFF") },

                { ThemeGroup.Dropdown_1, LSColors.HexToColorAlpha("EDE9EDFF") },
                { ThemeGroup.Dropdown_1_Overlay, LSColors.HexToColorAlpha("373737FF") },
                { ThemeGroup.Dropdown_1_Item, LSColors.HexToColorAlpha("F5F5F5FF") },
                { ThemeGroup.Toggle_1, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Toggle_1_Check, LSColors.HexToColorAlpha("212121FF") },
                { ThemeGroup.Input_Field, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Input_Field_Text, LSColors.HexToColorAlpha("252525FF") },
                { ThemeGroup.Slider_1, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Slider_1_Normal, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Slider_1_Highlighted, LSColors.HexToColorAlpha("F5F5F5FF") },
                { ThemeGroup.Slider_1_Selected, LSColors.HexToColorAlpha("F5F5F5FF") },
                { ThemeGroup.Slider_1_Pressed, LSColors.HexToColorAlpha("C8C8C8FF") },
                { ThemeGroup.Slider_1_Disabled, LSColors.HexToColorAlpha("C8C8C880") },
                { ThemeGroup.Slider_1_Handle, LSColors.HexToColorAlpha("FFFFFFFF") },

                { ThemeGroup.Slider_2, LSColors.HexToColorAlpha("EEEAEEFF") },
                { ThemeGroup.Slider_2_Handle, LSColors.HexToColorAlpha("424242FF") },

                { ThemeGroup.Documentation, LSColors.HexToColorAlpha("D89356FF") },

                { ThemeGroup.Timeline_Background, LSColors.HexToColorAlpha("1B1B1BFF") },
                { ThemeGroup.Timeline_Scrollbar, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Timeline_Scrollbar_Normal, LSColors.HexToColorAlpha("676767FF") },
                { ThemeGroup.Timeline_Scrollbar_Highlighted, LSColors.HexToColorAlpha("9E9E9EFF") },
                { ThemeGroup.Timeline_Scrollbar_Selected, LSColors.HexToColorAlpha("9D9D9DFF") },
                { ThemeGroup.Timeline_Scrollbar_Pressed, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Timeline_Scrollbar_Disabled, LSColors.HexToColorAlpha("676767FF") },
                { ThemeGroup.Timeline_Scrollbar_Base, LSColors.HexToColorAlpha("3E3E42FF") },
                { ThemeGroup.Timeline_Time_Scrollbar, LSColors.HexToColorAlpha("3E3E40FF") },

                { ThemeGroup.Title_Bar_Text, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Title_Bar_Button, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Title_Bar_Button_Normal, LSColors.HexToColorAlpha("303030FF") },
                { ThemeGroup.Title_Bar_Button_Highlighted, LSColors.HexToColorAlpha("1B1B1CFF") },
                { ThemeGroup.Title_Bar_Button_Selected, LSColors.HexToColorAlpha("1B1B1CFF") },
                { ThemeGroup.Title_Bar_Button_Pressed, LSColors.HexToColorAlpha("1B1B1CFF") },
                { ThemeGroup.Title_Bar_Dropdown, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Title_Bar_Dropdown_Normal, LSColors.HexToColorAlpha("1B1B1CFF") },
                { ThemeGroup.Title_Bar_Dropdown_Highlighted, LSColors.HexToColorAlpha("303030FF") },
                { ThemeGroup.Title_Bar_Dropdown_Selected, LSColors.HexToColorAlpha("303030FF") },
                { ThemeGroup.Title_Bar_Dropdown_Pressed, LSColors.HexToColorAlpha("303030FF") },
                { ThemeGroup.Title_Bar_Dropdown_Disabled, LSColors.HexToColorAlpha("303030FF") },

                { ThemeGroup.Warning_Confirm, LSColors.HexToColorAlpha("FF3645FF") },
                { ThemeGroup.Warning_Cancel, LSColors.HexToColorAlpha("4DB5ABFF") },

                { ThemeGroup.Notification_Background, LSColors.HexToColorAlpha("212121FF") },
                { ThemeGroup.Notification_Info, LSColors.HexToColorAlpha("404040FF") },
                { ThemeGroup.Notification_Success, LSColors.HexToColorAlpha("4DB6ACFF") },
                { ThemeGroup.Notification_Error, LSColors.HexToColorAlpha("E57373FF") },
                { ThemeGroup.Notification_Warning, LSColors.HexToColorAlpha("FFAF38FF") },

                { ThemeGroup.Copy, LSColors.HexToColorAlpha("3DADFFFF") },
                { ThemeGroup.Copy_Text, LSColors.HexToColorAlpha("1C1C1DFF") },
                { ThemeGroup.Paste, LSColors.HexToColorAlpha("FFAF38FF") },
                { ThemeGroup.Paste_Text, LSColors.HexToColorAlpha("1C1C1DFF") },

                { ThemeGroup.Tab_Color_1, LSColors.HexToColorAlpha("FFE7E7FF") },
                { ThemeGroup.Tab_Color_1_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_1_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_1_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_1_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_1_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_2, LSColors.HexToColorAlpha("C0ACE1FF") },
                { ThemeGroup.Tab_Color_2_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_2_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_2_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_2_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_2_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_3, LSColors.HexToColorAlpha("F17BB8FF") },
                { ThemeGroup.Tab_Color_3_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_3_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_3_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_3_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_3_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_4, LSColors.HexToColorAlpha("2F426DFF") },
                { ThemeGroup.Tab_Color_4_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_4_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_4_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_4_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_4_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_5, LSColors.HexToColorAlpha("4076DFFF") },
                { ThemeGroup.Tab_Color_5_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_5_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_5_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_5_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_5_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_6, LSColors.HexToColorAlpha("6CCBCFFF") },
                { ThemeGroup.Tab_Color_6_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_6_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_6_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_6_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_6_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_7, LSColors.HexToColorAlpha("1B1B1CFF") },
                { ThemeGroup.Tab_Color_7_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_7_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_7_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_7_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_7_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },

                { ThemeGroup.Event_Color_1, LSColors.HexToColorAlpha("673AB77F") }, // 1
                { ThemeGroup.Event_Color_2, LSColors.HexToColorAlpha("3E51B47F") }, // 2
                { ThemeGroup.Event_Color_3, LSColors.HexToColorAlpha("2196F37F") }, // 3
                { ThemeGroup.Event_Color_4, LSColors.HexToColorAlpha("03A9F47F") }, // 4
                { ThemeGroup.Event_Color_5, LSColors.HexToColorAlpha("00BCD47F") }, // 5
                { ThemeGroup.Event_Color_6, LSColors.HexToColorAlpha("0096887F") }, // 6
                { ThemeGroup.Event_Color_7, LSColors.HexToColorAlpha("4BAF507F") }, // 7
                { ThemeGroup.Event_Color_8, LSColors.HexToColorAlpha("7CB3417F") }, // 8
                { ThemeGroup.Event_Color_9, LSColors.HexToColorAlpha("AFB42B7F") }, // 9
                { ThemeGroup.Event_Color_10, LSColors.HexToColorAlpha("FFC1077F") }, // 10
                { ThemeGroup.Event_Color_11, LSColors.HexToColorAlpha("FF98007F") }, // 11
                { ThemeGroup.Event_Color_12, LSColors.HexToColorAlpha("B960007F") }, // 12
                { ThemeGroup.Event_Color_13, LSColors.HexToColorAlpha("B124117F") }, // 13
                { ThemeGroup.Event_Color_14, LSColors.HexToColorAlpha("B124247F") }, // 14
                { ThemeGroup.Event_Color_15, LSColors.HexToColorAlpha("64B4F67F") }, // 15

                { ThemeGroup.Event_Color_1_Keyframe, LSColors.HexToColorAlpha("673AB7FF") }, // 1
                { ThemeGroup.Event_Color_2_Keyframe, LSColors.HexToColorAlpha("3E51B4FF") }, // 2
                { ThemeGroup.Event_Color_3_Keyframe, LSColors.HexToColorAlpha("2196F3FF") }, // 3
                { ThemeGroup.Event_Color_4_Keyframe, LSColors.HexToColorAlpha("03A9F4FF") }, // 4
                { ThemeGroup.Event_Color_5_Keyframe, LSColors.HexToColorAlpha("00BCD4FF") }, // 5
                { ThemeGroup.Event_Color_6_Keyframe, LSColors.HexToColorAlpha("009688FF") }, // 6
                { ThemeGroup.Event_Color_7_Keyframe, LSColors.HexToColorAlpha("4BAF50FF") }, // 7
                { ThemeGroup.Event_Color_8_Keyframe, LSColors.HexToColorAlpha("7CB341FF") }, // 8
                { ThemeGroup.Event_Color_9_Keyframe, LSColors.HexToColorAlpha("AFB42BFF") }, // 9
                { ThemeGroup.Event_Color_10_Keyframe, LSColors.HexToColorAlpha("FFC107FF") }, // 10
                { ThemeGroup.Event_Color_11_Keyframe, LSColors.HexToColorAlpha("FF9800FF") }, // 11
                { ThemeGroup.Event_Color_12_Keyframe, LSColors.HexToColorAlpha("B96000FF") }, // 12
                { ThemeGroup.Event_Color_13_Keyframe, LSColors.HexToColorAlpha("B12411FF") }, // 13
                { ThemeGroup.Event_Color_14_Keyframe, LSColors.HexToColorAlpha("B12424FF") }, // 14
                { ThemeGroup.Event_Color_15_Keyframe, LSColors.HexToColorAlpha("64B4F6FF") }, // 15

                { ThemeGroup.Event_Color_1_Editor, LSColors.HexToColorAlpha("564B6AFF") }, // 1
                { ThemeGroup.Event_Color_2_Editor, LSColors.HexToColorAlpha("41445EFF") }, // 2
                { ThemeGroup.Event_Color_3_Editor, LSColors.HexToColorAlpha("44627AFF") }, // 3
                { ThemeGroup.Event_Color_4_Editor, LSColors.HexToColorAlpha("315B6EFF") }, // 4
                { ThemeGroup.Event_Color_5_Editor, LSColors.HexToColorAlpha("3E6D73FF") }, // 5
                { ThemeGroup.Event_Color_6_Editor, LSColors.HexToColorAlpha("305653FF") }, // 6
                { ThemeGroup.Event_Color_7_Editor, LSColors.HexToColorAlpha("506951FF") }, // 7
                { ThemeGroup.Event_Color_8_Editor, LSColors.HexToColorAlpha("515E41FF") }, // 8
                { ThemeGroup.Event_Color_9_Editor, LSColors.HexToColorAlpha("676945FF") }, // 9
                { ThemeGroup.Event_Color_10_Editor, LSColors.HexToColorAlpha("726335FF") }, // 10
                { ThemeGroup.Event_Color_11_Editor, LSColors.HexToColorAlpha("FF9800FF") }, // 11
                { ThemeGroup.Event_Color_12_Editor, LSColors.HexToColorAlpha("FF5800FF") }, // 12
                { ThemeGroup.Event_Color_13_Editor, LSColors.HexToColorAlpha("FF2509FF") }, // 13
                { ThemeGroup.Event_Color_14_Editor, LSColors.HexToColorAlpha("FF0F0FFF") }, // 14

                { ThemeGroup.Object_Keyframe_Color_1, LSColors.HexToColorAlpha("F44336FF") }, // 1
                { ThemeGroup.Object_Keyframe_Color_2, LSColors.HexToColorAlpha("4CAF50FF") }, // 2
                { ThemeGroup.Object_Keyframe_Color_3, LSColors.HexToColorAlpha("2196F3FF") }, // 3
                { ThemeGroup.Object_Keyframe_Color_4, LSColors.HexToColorAlpha("FFC107FF") }, // 4
            }),
            new EditorTheme($"{nameof(ThemeSetting.Dark)}", new Dictionary<ThemeGroup, Color>
            {
                { ThemeGroup.Background_1, LSColors.HexToColorAlpha("0A0A0AFF") },
                { ThemeGroup.Background_2, LSColors.HexToColorAlpha("060606FF") },
                { ThemeGroup.Background_3, LSColors.HexToColorAlpha("0C0C0CFF") },
                { ThemeGroup.Preview_Cover, LSColors.HexToColorAlpha("060606FF") },

                { ThemeGroup.Scrollbar_1_Handle, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Scrollbar_1_Handle_Normal, LSColors.HexToColorAlpha("4C4C4CFF") },
                { ThemeGroup.Scrollbar_1_Handle_Highlighted, LSColors.HexToColorAlpha("606060FF") },
                { ThemeGroup.Scrollbar_1_Handle_Selected, LSColors.HexToColorAlpha("606060FF") },
                { ThemeGroup.Scrollbar_1_Handle_Pressed, LSColors.HexToColorAlpha("606060FF") },
                { ThemeGroup.Scrollbar_1_Handle_Disabled, LSColors.HexToColorAlpha("606060FF") },

                { ThemeGroup.Scrollbar_2, LSColors.HexToColorAlpha("262526FF") },
                { ThemeGroup.Scrollbar_2_Handle, LSColors.HexToColorAlpha("424242FF") },
                { ThemeGroup.Scrollbar_2_Handle_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Scrollbar_2_Handle_Hightlighted, LSColors.HexToColorAlpha("F5F5F5FF") },
                { ThemeGroup.Scrollbar_2_Handle_Selected, LSColors.HexToColorAlpha("414141FF") },
                { ThemeGroup.Scrollbar_2_Handle_Pressed, LSColors.HexToColorAlpha("F5F5F5FF") },
                { ThemeGroup.Scrollbar_2_Handle_Disabled, LSColors.HexToColorAlpha("C8C8C880") },

                { ThemeGroup.Close, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Close_Normal, LSColors.HexToColorAlpha("C43C33FF") },
                { ThemeGroup.Close_Highlighted, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Close_Selected, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Close_Pressed, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Close_Disabled, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Close_X, LSColors.HexToColorAlpha("EFEBEFFF") },

                { ThemeGroup.Picker, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Picker_Normal, LSColors.HexToColorAlpha("3A73AFFF") },
                { ThemeGroup.Picker_Highlighted, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Picker_Selected, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Picker_Pressed, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Picker_Disabled, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Picker_Icon, LSColors.HexToColorAlpha("EFEBEFFF") },

                { ThemeGroup.Light_Text, LSColors.HexToColorAlpha("E5E1E5FF") },
                { ThemeGroup.Dark_Text, LSColors.HexToColorAlpha("E5E1E5FF") },

                { ThemeGroup.Function_1, LSColors.HexToColorAlpha("1D4675FF") },
                { ThemeGroup.Function_1_Text, LSColors.HexToColorAlpha("E5E1E5FF") },
                { ThemeGroup.Function_2, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Function_2_Normal, LSColors.HexToColorAlpha("3D3D3DFF") },
                { ThemeGroup.Function_2_Highlighted, LSColors.HexToColorAlpha("AF5D73FF") },
                { ThemeGroup.Function_2_Selected, LSColors.HexToColorAlpha("3D3D3DFF") },
                { ThemeGroup.Function_2_Pressed, LSColors.HexToColorAlpha("AF5D73FF") },
                { ThemeGroup.Function_2_Disabled, LSColors.HexToColorAlpha("C7C7C780") },
                { ThemeGroup.Function_2_Text, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Function_3, LSColors.HexToColorAlpha("3D3D3DFF") },
                { ThemeGroup.Function_3_Text, LSColors.HexToColorAlpha("C6C6C6FF") },

                { ThemeGroup.List_Button_1, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.List_Button_1_Normal, LSColors.HexToColorAlpha("111111FF") },
                { ThemeGroup.List_Button_1_Highlighted, LSColors.HexToColorAlpha("282828FF") },
                { ThemeGroup.List_Button_1_Selected, LSColors.HexToColorAlpha("282828FF") },
                { ThemeGroup.List_Button_1_Pressed, LSColors.HexToColorAlpha("282828FF") },
                { ThemeGroup.List_Button_1_Disabled, LSColors.HexToColorAlpha("282828FF") },

                { ThemeGroup.List_Button_2, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.List_Button_2_Normal, LSColors.HexToColorAlpha("111111FF") },
                { ThemeGroup.List_Button_2_Highlighted, LSColors.HexToColorAlpha("282828FF") },
                { ThemeGroup.List_Button_2_Selected, LSColors.HexToColorAlpha("282828FF") },
                { ThemeGroup.List_Button_2_Pressed, LSColors.HexToColorAlpha("282828FF") },
                { ThemeGroup.List_Button_2_Disabled, LSColors.HexToColorAlpha("282828FF") },
                { ThemeGroup.List_Button_2_Text, LSColors.HexToColorAlpha("EFEBEFFF") },

                { ThemeGroup.Back_Button, LSColors.HexToColorAlpha("264756FF") },
                { ThemeGroup.Back_Button_Text, LSColors.HexToColorAlpha("F5F5F5FF") },
                { ThemeGroup.Folder_Button, LSColors.HexToColorAlpha("514A47FF") },
                { ThemeGroup.Folder_Button_Text, LSColors.HexToColorAlpha("F5F5F5FF") },
                { ThemeGroup.File_Button, LSColors.HexToColorAlpha("204C8EFF") },
                { ThemeGroup.File_Button_Text, LSColors.HexToColorAlpha("F5F5F5FF") },

                { ThemeGroup.Search_Field_1, LSColors.HexToColorAlpha("111111FF") },
                { ThemeGroup.Search_Field_1_Text, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Search_Field_2, LSColors.HexToColorAlpha("111111FF") },
                { ThemeGroup.Search_Field_2_Text, LSColors.HexToColorAlpha("FFFFFFFF") },

                { ThemeGroup.Add, LSColors.HexToColorAlpha("06ADADFF") },
                { ThemeGroup.Add_Text, LSColors.HexToColorAlpha("011E1EFF") },
                { ThemeGroup.Delete, LSColors.HexToColorAlpha("7F2626FF") },
                { ThemeGroup.Delete_Text, LSColors.HexToColorAlpha("EFE1E1FF") },
                { ThemeGroup.Delete_Keyframe_BG, LSColors.HexToColorAlpha("EEEAEEFF") },
                { ThemeGroup.Delete_Keyframe_Button, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Delete_Keyframe_Button_Normal, LSColors.HexToColorAlpha("F34235FF") },
                { ThemeGroup.Delete_Keyframe_Button_Highlighted, LSColors.HexToColorAlpha("212121FF") },
                { ThemeGroup.Delete_Keyframe_Button_Selected, LSColors.HexToColorAlpha("212121FF") },
                { ThemeGroup.Delete_Keyframe_Button_Pressed, LSColors.HexToColorAlpha("222222FF") },
                { ThemeGroup.Delete_Keyframe_Button_Disabled, LSColors.HexToColorAlpha("37373880") },

                { ThemeGroup.Prefab, LSColors.HexToColorAlpha("1B1B1CFF") },
                { ThemeGroup.Prefab_Text, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Object, LSColors.HexToColorAlpha("918B91FF") },
                { ThemeGroup.Object_Text, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Marker, LSColors.HexToColorAlpha("C97D14FF") },
                { ThemeGroup.Marker_Text, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Checkpoint, LSColors.HexToColorAlpha("2781C6FF") },
                { ThemeGroup.Checkpoint_Text, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Background_Object, LSColors.HexToColorAlpha("AD4848FF") },
                { ThemeGroup.Background_Object_Text, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Timeline_Bar, LSColors.HexToColorAlpha("030305FF") },
                { ThemeGroup.Event_Check, LSColors.HexToColorAlpha("2196A8FF") },
                { ThemeGroup.Event_Check_Text, LSColors.HexToColorAlpha("EFEBEFFF") },

                { ThemeGroup.Dropdown_1, LSColors.HexToColorAlpha("373737FF") },
                { ThemeGroup.Dropdown_1_Overlay, LSColors.HexToColorAlpha("EDE9EDFF") },
                { ThemeGroup.Dropdown_1_Item, LSColors.HexToColorAlpha("373737FF") },
                { ThemeGroup.Toggle_1, LSColors.HexToColorAlpha("212121FF") },
                { ThemeGroup.Toggle_1_Check, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Input_Field, LSColors.HexToColorAlpha("252525FF") },
                { ThemeGroup.Input_Field_Text, LSColors.HexToColorAlpha("EFEBEFFF") },

                { ThemeGroup.Slider_1, LSColors.HexToColorAlpha("565656FF") },
                { ThemeGroup.Slider_1_Normal, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Slider_1_Highlighted, LSColors.HexToColorAlpha("F5F5F5FF") },
                { ThemeGroup.Slider_1_Selected, LSColors.HexToColorAlpha("F5F5F5FF") },
                { ThemeGroup.Slider_1_Pressed, LSColors.HexToColorAlpha("C8C8C8FF") },
                { ThemeGroup.Slider_1_Disabled, LSColors.HexToColorAlpha("C8C8C880") },
                { ThemeGroup.Slider_1_Handle, LSColors.HexToColorAlpha("FFFFFFFF") },

                { ThemeGroup.Slider_2, LSColors.HexToColorAlpha("2D2D2DFF") },
                { ThemeGroup.Slider_2_Handle, LSColors.HexToColorAlpha("424242FF") },

                { ThemeGroup.Documentation, LSColors.HexToColorAlpha("D89356FF") },

                { ThemeGroup.Timeline_Background, LSColors.HexToColorAlpha("080808FF") },
                { ThemeGroup.Timeline_Scrollbar, LSColors.HexToColorAlpha("686868FF") },
                { ThemeGroup.Timeline_Scrollbar_Normal, LSColors.HexToColorAlpha("676767FF") },
                { ThemeGroup.Timeline_Scrollbar_Highlighted, LSColors.HexToColorAlpha("9E9E9EFF") },
                { ThemeGroup.Timeline_Scrollbar_Selected, LSColors.HexToColorAlpha("9D9D9DFF") },
                { ThemeGroup.Timeline_Scrollbar_Pressed, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Timeline_Scrollbar_Disabled, LSColors.HexToColorAlpha("676767FF") },
                { ThemeGroup.Timeline_Scrollbar_Base, LSColors.HexToColorAlpha("1F1F21FF") },
                { ThemeGroup.Timeline_Time_Scrollbar, LSColors.HexToColorAlpha("1F1F21FF") },

                { ThemeGroup.Title_Bar_Text, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Title_Bar_Button, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Title_Bar_Button_Normal, LSColors.HexToColorAlpha("1E1E1EFF") },
                { ThemeGroup.Title_Bar_Button_Highlighted, LSColors.HexToColorAlpha("121212FF") },
                { ThemeGroup.Title_Bar_Button_Selected, LSColors.HexToColorAlpha("121212FF") },
                { ThemeGroup.Title_Bar_Button_Pressed, LSColors.HexToColorAlpha("121212FF") },
                { ThemeGroup.Title_Bar_Dropdown, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Title_Bar_Dropdown_Normal, LSColors.HexToColorAlpha("121212FF") },
                { ThemeGroup.Title_Bar_Dropdown_Highlighted, LSColors.HexToColorAlpha("1E1E1EFF") },
                { ThemeGroup.Title_Bar_Dropdown_Selected, LSColors.HexToColorAlpha("1E1E1EFF") },
                { ThemeGroup.Title_Bar_Dropdown_Pressed, LSColors.HexToColorAlpha("1E1E1EFF") },
                { ThemeGroup.Title_Bar_Dropdown_Disabled, LSColors.HexToColorAlpha("1E1E1EFF") },

                { ThemeGroup.Warning_Confirm, LSColors.HexToColorAlpha("FF3645FF") },
                { ThemeGroup.Warning_Cancel, LSColors.HexToColorAlpha("4DB5ABFF") },

                { ThemeGroup.Notification_Background, LSColors.HexToColorAlpha("0A0A0AFF") },
                { ThemeGroup.Notification_Info, LSColors.HexToColorAlpha("191919FF") },
                { ThemeGroup.Notification_Success, LSColors.HexToColorAlpha("6EB5D3FF") },
                { ThemeGroup.Notification_Error, LSColors.HexToColorAlpha("AA3D3DFF") },
                { ThemeGroup.Notification_Warning, LSColors.HexToColorAlpha("CE8633FF") },

                { ThemeGroup.Copy, LSColors.HexToColorAlpha("1D4675FF") },
                { ThemeGroup.Copy_Text, LSColors.HexToColorAlpha("E5E1E5FF") },
                { ThemeGroup.Paste, LSColors.HexToColorAlpha("C18736FF") },
                { ThemeGroup.Paste_Text, LSColors.HexToColorAlpha("E5E1E5FF") },

                { ThemeGroup.Tab_Color_1, LSColors.HexToColorAlpha("1395BAFF") },
                { ThemeGroup.Tab_Color_1_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_1_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_1_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_1_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_1_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_2, LSColors.HexToColorAlpha("0D3C55FF") },
                { ThemeGroup.Tab_Color_2_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_2_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_2_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_2_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_2_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_3, LSColors.HexToColorAlpha("C02E1DFF") },
                { ThemeGroup.Tab_Color_3_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_3_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_3_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_3_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_3_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_4, LSColors.HexToColorAlpha("F16C20FF") },
                { ThemeGroup.Tab_Color_4_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_4_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_4_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_4_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_4_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_5, LSColors.HexToColorAlpha("EBC844FF") },
                { ThemeGroup.Tab_Color_5_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_5_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_5_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_5_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_5_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_6, LSColors.HexToColorAlpha("A2B86CFF") },
                { ThemeGroup.Tab_Color_6_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_6_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_6_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_6_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_6_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_7, LSColors.HexToColorAlpha("1B1B1CFF") },
                { ThemeGroup.Tab_Color_7_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_7_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_7_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_7_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_7_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },

                { ThemeGroup.Event_Color_1, LSColors.HexToColorAlpha("241A357F") }, // 1
			    { ThemeGroup.Event_Color_2, LSColors.HexToColorAlpha("1F24447F") }, // 2
			    { ThemeGroup.Event_Color_3, LSColors.HexToColorAlpha("1930427F") }, // 3
			    { ThemeGroup.Event_Color_4, LSColors.HexToColorAlpha("102C387F") }, // 4
			    { ThemeGroup.Event_Color_5, LSColors.HexToColorAlpha("14393D7F") }, // 5
			    { ThemeGroup.Event_Color_6, LSColors.HexToColorAlpha("1644407F") }, // 6
			    { ThemeGroup.Event_Color_7, LSColors.HexToColorAlpha("2749287F") }, // 7
			    { ThemeGroup.Event_Color_8, LSColors.HexToColorAlpha("2E3D1A7F") }, // 8
			    { ThemeGroup.Event_Color_9, LSColors.HexToColorAlpha("4042257F") }, // 9
			    { ThemeGroup.Event_Color_10, LSColors.HexToColorAlpha("5947117F") }, // 10
			    { ThemeGroup.Event_Color_11, LSColors.HexToColorAlpha("562E137F") }, // 11
			    { ThemeGroup.Event_Color_12, LSColors.HexToColorAlpha("66280E7F") }, // 12
			    { ThemeGroup.Event_Color_13, LSColors.HexToColorAlpha("5611067F") }, // 13
			    { ThemeGroup.Event_Color_14, LSColors.HexToColorAlpha("3D00007F") }, // 14
			    { ThemeGroup.Event_Color_15, LSColors.HexToColorAlpha("1168AF7F") }, // 15

                { ThemeGroup.Event_Color_1_Keyframe, LSColors.HexToColorAlpha("673AB7FF") }, // 1
                { ThemeGroup.Event_Color_2_Keyframe, LSColors.HexToColorAlpha("3E51B4FF") }, // 2
                { ThemeGroup.Event_Color_3_Keyframe, LSColors.HexToColorAlpha("2196F3FF") }, // 3
                { ThemeGroup.Event_Color_4_Keyframe, LSColors.HexToColorAlpha("03A9F4FF") }, // 4
                { ThemeGroup.Event_Color_5_Keyframe, LSColors.HexToColorAlpha("00BCD4FF") }, // 5
                { ThemeGroup.Event_Color_6_Keyframe, LSColors.HexToColorAlpha("009688FF") }, // 6
                { ThemeGroup.Event_Color_7_Keyframe, LSColors.HexToColorAlpha("4BAF50FF") }, // 7
                { ThemeGroup.Event_Color_8_Keyframe, LSColors.HexToColorAlpha("7CB341FF") }, // 8
                { ThemeGroup.Event_Color_9_Keyframe, LSColors.HexToColorAlpha("AFB42BFF") }, // 9
                { ThemeGroup.Event_Color_10_Keyframe, LSColors.HexToColorAlpha("FFC107FF") }, // 10
                { ThemeGroup.Event_Color_11_Keyframe, LSColors.HexToColorAlpha("FF9800FF") }, // 11
                { ThemeGroup.Event_Color_12_Keyframe, LSColors.HexToColorAlpha("B96000FF") }, // 12
                { ThemeGroup.Event_Color_13_Keyframe, LSColors.HexToColorAlpha("B12411FF") }, // 13
                { ThemeGroup.Event_Color_14_Keyframe, LSColors.HexToColorAlpha("B12424FF") }, // 14
                { ThemeGroup.Event_Color_15_Keyframe, LSColors.HexToColorAlpha("64B4F6FF") }, // 15

                { ThemeGroup.Event_Color_1_Editor, LSColors.HexToColorAlpha("564B6AFF") }, // 1
                { ThemeGroup.Event_Color_2_Editor, LSColors.HexToColorAlpha("41445EFF") }, // 2
                { ThemeGroup.Event_Color_3_Editor, LSColors.HexToColorAlpha("44627AFF") }, // 3
                { ThemeGroup.Event_Color_4_Editor, LSColors.HexToColorAlpha("315B6EFF") }, // 4
                { ThemeGroup.Event_Color_5_Editor, LSColors.HexToColorAlpha("3E6D73FF") }, // 5
                { ThemeGroup.Event_Color_6_Editor, LSColors.HexToColorAlpha("305653FF") }, // 6
                { ThemeGroup.Event_Color_7_Editor, LSColors.HexToColorAlpha("506951FF") }, // 7
                { ThemeGroup.Event_Color_8_Editor, LSColors.HexToColorAlpha("515E41FF") }, // 8
                { ThemeGroup.Event_Color_9_Editor, LSColors.HexToColorAlpha("676945FF") }, // 9
                { ThemeGroup.Event_Color_10_Editor, LSColors.HexToColorAlpha("726335FF") }, // 10
                { ThemeGroup.Event_Color_11_Editor, LSColors.HexToColorAlpha("FF9800FF") }, // 11
                { ThemeGroup.Event_Color_12_Editor, LSColors.HexToColorAlpha("FF5800FF") }, // 12
                { ThemeGroup.Event_Color_13_Editor, LSColors.HexToColorAlpha("FF2509FF") }, // 13
                { ThemeGroup.Event_Color_14_Editor, LSColors.HexToColorAlpha("FF0F0FFF") }, // 14

                { ThemeGroup.Object_Keyframe_Color_1, LSColors.HexToColorAlpha("E57373FF") }, // 1
                { ThemeGroup.Object_Keyframe_Color_2, LSColors.HexToColorAlpha("81C784FF") }, // 2
                { ThemeGroup.Object_Keyframe_Color_3, LSColors.HexToColorAlpha("64B5F6FF") }, // 3
                { ThemeGroup.Object_Keyframe_Color_4, LSColors.HexToColorAlpha("FFB74DFF") }, // 4
            }),
            new EditorTheme($"{nameof(ThemeSetting.Light)}", new Dictionary<ThemeGroup, Color>
            {
                { ThemeGroup.Background_1, LSColors.HexToColorAlpha("E2E2E2FF") },
                { ThemeGroup.Background_2, LSColors.HexToColorAlpha("D9D9E0FF") },
                { ThemeGroup.Background_3, LSColors.HexToColorAlpha("B3B3B5FF") },
                { ThemeGroup.Preview_Cover, LSColors.HexToColorAlpha("999999FF") },

                { ThemeGroup.Scrollbar_1_Handle, LSColors.HexToColorAlpha("C8C8C8FF") },
                { ThemeGroup.Scrollbar_1_Handle_Normal, LSColors.HexToColorAlpha("C7C7C7FF") },
                { ThemeGroup.Scrollbar_1_Handle_Highlighted, LSColors.HexToColorAlpha("414141FF") },
                { ThemeGroup.Scrollbar_1_Handle_Selected, LSColors.HexToColorAlpha("414141FF") },
                { ThemeGroup.Scrollbar_1_Handle_Pressed, LSColors.HexToColorAlpha("414141FF") },
                { ThemeGroup.Scrollbar_1_Handle_Disabled, LSColors.HexToColorAlpha("414141FF") },

                { ThemeGroup.Scrollbar_2, LSColors.HexToColorAlpha("EEEAEEFF") },
                { ThemeGroup.Scrollbar_2_Handle, LSColors.HexToColorAlpha("424242FF") },
                { ThemeGroup.Scrollbar_2_Handle_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Scrollbar_2_Handle_Hightlighted, LSColors.HexToColorAlpha("F5F5F5FF") },
                { ThemeGroup.Scrollbar_2_Handle_Selected, LSColors.HexToColorAlpha("414141FF") },
                { ThemeGroup.Scrollbar_2_Handle_Pressed, LSColors.HexToColorAlpha("F5F5F5FF") },
                { ThemeGroup.Scrollbar_2_Handle_Disabled, LSColors.HexToColorAlpha("C8C8C880") },

                { ThemeGroup.Close, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Close_Normal, LSColors.HexToColorAlpha("F44336FF") },
                { ThemeGroup.Close_Highlighted, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Close_Selected, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Close_Pressed, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Close_Disabled, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Close_X, LSColors.HexToColorAlpha("EFEBEFFF") },

                { ThemeGroup.Picker, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Picker_Normal, LSColors.HexToColorAlpha("0E7BF7FF") },
                { ThemeGroup.Picker_Highlighted, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Picker_Selected, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Picker_Pressed, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Picker_Disabled, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Picker_Icon, LSColors.HexToColorAlpha("EFEBEFFF") },

                { ThemeGroup.Light_Text, LSColors.HexToColorAlpha("323232FF") },
                { ThemeGroup.Dark_Text, LSColors.HexToColorAlpha("E5E1E5FF") },

                { ThemeGroup.Function_1, LSColors.HexToColorAlpha("134EF2FF") }, // 0F7BF8FF
                { ThemeGroup.Function_1_Text, LSColors.HexToColorAlpha("1B1B1CFF") },
                { ThemeGroup.Function_2, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Function_2_Normal, LSColors.HexToColorAlpha("F4F4F4FF") },
                { ThemeGroup.Function_2_Highlighted, LSColors.HexToColorAlpha("FF6090FF") },
                { ThemeGroup.Function_2_Selected, LSColors.HexToColorAlpha("FF6090FF") },
                { ThemeGroup.Function_2_Pressed, LSColors.HexToColorAlpha("FF6090FF") },
                { ThemeGroup.Function_2_Disabled, LSColors.HexToColorAlpha("C7C7C780") },
                { ThemeGroup.Function_2_Text, LSColors.HexToColorAlpha("323232FF") },
                { ThemeGroup.Function_3, LSColors.HexToColorAlpha("EDE9EDFF") },
                { ThemeGroup.Function_3_Text, LSColors.HexToColorAlpha("0A0A0AFF") },

                { ThemeGroup.List_Button_1, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.List_Button_1_Normal, LSColors.HexToColorAlpha("F7F7F7FF") },
                { ThemeGroup.List_Button_1_Highlighted, LSColors.HexToColorAlpha("C4C4C4FF") },
                { ThemeGroup.List_Button_1_Selected, LSColors.HexToColorAlpha("C4C4C4FF") },
                { ThemeGroup.List_Button_1_Pressed, LSColors.HexToColorAlpha("C4C4C4FF") },
                { ThemeGroup.List_Button_1_Disabled, LSColors.HexToColorAlpha("C4C4C4FF") },

                { ThemeGroup.List_Button_2, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.List_Button_2_Normal, LSColors.HexToColorAlpha("EDE9EDFF") },
                { ThemeGroup.List_Button_2_Highlighted, LSColors.HexToColorAlpha("C8C8C8FF") },
                { ThemeGroup.List_Button_2_Selected, LSColors.HexToColorAlpha("C8C8C8FF") },
                { ThemeGroup.List_Button_2_Pressed, LSColors.HexToColorAlpha("C8C8C8FF") },
                { ThemeGroup.List_Button_2_Disabled, LSColors.HexToColorAlpha("96969680") },
                { ThemeGroup.List_Button_2_Text, LSColors.HexToColorAlpha("323232FF") },

                { ThemeGroup.Back_Button, LSColors.HexToColorAlpha("90A4AEFF") },
                { ThemeGroup.Back_Button_Text, LSColors.HexToColorAlpha("222222FF") },
                { ThemeGroup.Folder_Button, LSColors.HexToColorAlpha("BCAAA4FF") },
                { ThemeGroup.Folder_Button_Text, LSColors.HexToColorAlpha("323232FF") },
                { ThemeGroup.File_Button, LSColors.HexToColorAlpha("4076DFFF") },
                { ThemeGroup.File_Button_Text, LSColors.HexToColorAlpha("F5F5F5FF") },

                { ThemeGroup.Search_Field_1, LSColors.HexToColorAlpha("FCFCFCFF") },
                { ThemeGroup.Search_Field_1_Text, LSColors.HexToColorAlpha("2F2F2FFF") },
                { ThemeGroup.Search_Field_2, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Search_Field_2_Text, LSColors.HexToColorAlpha("2F2F2FFF") },

                { ThemeGroup.Add, LSColors.HexToColorAlpha("0EC9EAFF") },
                { ThemeGroup.Add_Text, LSColors.HexToColorAlpha("202020FF") },
                { ThemeGroup.Delete, LSColors.HexToColorAlpha("E2284EFF") },
                { ThemeGroup.Delete_Text, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Delete_Keyframe_BG, LSColors.HexToColorAlpha("EEEAEEFF") },
                { ThemeGroup.Delete_Keyframe_Button, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Delete_Keyframe_Button_Normal, LSColors.HexToColorAlpha("F34235FF") },
                { ThemeGroup.Delete_Keyframe_Button_Highlighted, LSColors.HexToColorAlpha("212121FF") },
                { ThemeGroup.Delete_Keyframe_Button_Selected, LSColors.HexToColorAlpha("212121FF") },
                { ThemeGroup.Delete_Keyframe_Button_Pressed, LSColors.HexToColorAlpha("222222FF") },
                { ThemeGroup.Delete_Keyframe_Button_Disabled, LSColors.HexToColorAlpha("37373880") },

                { ThemeGroup.Prefab, LSColors.HexToColorAlpha("383838FF") },
                { ThemeGroup.Prefab_Text, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Object, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Object_Text, LSColors.HexToColorAlpha("212121FF") },
                { ThemeGroup.Marker, LSColors.HexToColorAlpha("FFAF38FF") },
                { ThemeGroup.Marker_Text, LSColors.HexToColorAlpha("1C1C1DFF") },
                { ThemeGroup.Checkpoint, LSColors.HexToColorAlpha("64B5F6FF") },
                { ThemeGroup.Checkpoint_Text, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Background_Object, LSColors.HexToColorAlpha("E57373FF") },
                { ThemeGroup.Background_Object_Text, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Timeline_Bar, LSColors.HexToColorAlpha("D4C9D6FF") },
                { ThemeGroup.Event_Check, LSColors.HexToColorAlpha("6CCBCFFF") },
                { ThemeGroup.Event_Check_Text, LSColors.HexToColorAlpha("EFEBEFFF") },

                { ThemeGroup.Dropdown_1, LSColors.HexToColorAlpha("EDE9EDFF") },
                { ThemeGroup.Dropdown_1_Overlay, LSColors.HexToColorAlpha("373737FF") },
                { ThemeGroup.Dropdown_1_Item, LSColors.HexToColorAlpha("F5F5F5FF") },
                { ThemeGroup.Toggle_1, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Toggle_1_Check, LSColors.HexToColorAlpha("252525FF") },
                { ThemeGroup.Input_Field, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Input_Field_Text, LSColors.HexToColorAlpha("252525FF") },
                { ThemeGroup.Slider_1, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Slider_1_Normal, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Slider_1_Highlighted, LSColors.HexToColorAlpha("F5F5F5FF") },
                { ThemeGroup.Slider_1_Selected, LSColors.HexToColorAlpha("F5F5F5FF") },
                { ThemeGroup.Slider_1_Pressed, LSColors.HexToColorAlpha("C8C8C8FF") },
                { ThemeGroup.Slider_1_Disabled, LSColors.HexToColorAlpha("C8C8C880") },
                { ThemeGroup.Slider_1_Handle, LSColors.HexToColorAlpha("FFFFFFFF") },

                { ThemeGroup.Slider_2, LSColors.HexToColorAlpha("EEEAEEFF") },
                { ThemeGroup.Slider_2_Handle, LSColors.HexToColorAlpha("9B9B9BFF") },

                { ThemeGroup.Documentation, LSColors.HexToColorAlpha("D89356FF") },

                { ThemeGroup.Timeline_Background, LSColors.HexToColorAlpha("878787FF") },
                { ThemeGroup.Timeline_Scrollbar, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Timeline_Scrollbar_Normal, LSColors.HexToColorAlpha("E2E2E2FF") },
                { ThemeGroup.Timeline_Scrollbar_Highlighted, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Timeline_Scrollbar_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Timeline_Scrollbar_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Timeline_Scrollbar_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Timeline_Scrollbar_Base, LSColors.HexToColorAlpha("AAAAB5FF") },
                { ThemeGroup.Timeline_Time_Scrollbar, LSColors.HexToColorAlpha("AAAAB5FF") },

                { ThemeGroup.Title_Bar_Text, LSColors.HexToColorAlpha("303030FF") }, //303030FF
                { ThemeGroup.Title_Bar_Button, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Title_Bar_Button_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Title_Bar_Button_Highlighted, LSColors.HexToColorAlpha("B3B3B5FF") }, //1B1B1CFF
                { ThemeGroup.Title_Bar_Button_Selected, LSColors.HexToColorAlpha("B3B3B5FF") },
                { ThemeGroup.Title_Bar_Button_Pressed, LSColors.HexToColorAlpha("B3B3B5FF") },
                { ThemeGroup.Title_Bar_Dropdown, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Title_Bar_Dropdown_Normal, LSColors.HexToColorAlpha("B3B3B5FF") },
                { ThemeGroup.Title_Bar_Dropdown_Highlighted, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Title_Bar_Dropdown_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Title_Bar_Dropdown_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Title_Bar_Dropdown_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },

                { ThemeGroup.Warning_Confirm, LSColors.HexToColorAlpha("FF3645FF") },
                { ThemeGroup.Warning_Cancel, LSColors.HexToColorAlpha("4DB5ABFF") },

                { ThemeGroup.Notification_Background, LSColors.HexToColorAlpha("E2E2E2FF") },
                { ThemeGroup.Notification_Info, LSColors.HexToColorAlpha("D9D9E0FF") },
                { ThemeGroup.Notification_Success, LSColors.HexToColorAlpha("4DB6ACFF") },
                { ThemeGroup.Notification_Error, LSColors.HexToColorAlpha("E57373FF") },
                { ThemeGroup.Notification_Warning, LSColors.HexToColorAlpha("FFAF38FF") },

                { ThemeGroup.Copy, LSColors.HexToColorAlpha("007BFFFF") },
                { ThemeGroup.Copy_Text, LSColors.HexToColorAlpha("1C1C1DFF") },
                { ThemeGroup.Paste, LSColors.HexToColorAlpha("FF800AFF") },
                { ThemeGroup.Paste_Text, LSColors.HexToColorAlpha("1C1C1DFF") },

                { ThemeGroup.Tab_Color_1, LSColors.HexToColorAlpha("346188FF") },
                { ThemeGroup.Tab_Color_1_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_1_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_1_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_1_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_1_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_2, LSColors.HexToColorAlpha("FF6A86FF") },
                { ThemeGroup.Tab_Color_2_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_2_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_2_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_2_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_2_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_3, LSColors.HexToColorAlpha("20E4EDFF") },
                { ThemeGroup.Tab_Color_3_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_3_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_3_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_3_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_3_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_4, LSColors.HexToColorAlpha("71DF4FFF") },
                { ThemeGroup.Tab_Color_4_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_4_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_4_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_4_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_4_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_5, LSColors.HexToColorAlpha("EF8E46FF") },
                { ThemeGroup.Tab_Color_5_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_5_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_5_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_5_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_5_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_6, LSColors.HexToColorAlpha("FFCD86FF") },
                { ThemeGroup.Tab_Color_6_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_6_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_6_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_6_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_6_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_7, LSColors.HexToColorAlpha("333333FF") },
                { ThemeGroup.Tab_Color_7_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_7_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_7_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_7_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_7_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },

                { ThemeGroup.Event_Color_1, LSColors.HexToColorAlpha("673AB77F") }, // 1
                { ThemeGroup.Event_Color_2, LSColors.HexToColorAlpha("3E51B47F") }, // 2
                { ThemeGroup.Event_Color_3, LSColors.HexToColorAlpha("2196F37F") }, // 3
                { ThemeGroup.Event_Color_4, LSColors.HexToColorAlpha("03A9F47F") }, // 4
                { ThemeGroup.Event_Color_5, LSColors.HexToColorAlpha("00BCD47F") }, // 5
                { ThemeGroup.Event_Color_6, LSColors.HexToColorAlpha("0096887F") }, // 6
                { ThemeGroup.Event_Color_7, LSColors.HexToColorAlpha("4BAF507F") }, // 7
                { ThemeGroup.Event_Color_8, LSColors.HexToColorAlpha("7CB3417F") }, // 8
                { ThemeGroup.Event_Color_9, LSColors.HexToColorAlpha("AFB42B7F") }, // 9
                { ThemeGroup.Event_Color_10, LSColors.HexToColorAlpha("FFC1077F") }, // 10
                { ThemeGroup.Event_Color_11, LSColors.HexToColorAlpha("FF98007F") }, // 11
                { ThemeGroup.Event_Color_12, LSColors.HexToColorAlpha("B960007F") }, // 12
                { ThemeGroup.Event_Color_13, LSColors.HexToColorAlpha("B124117F") }, // 13
                { ThemeGroup.Event_Color_14, LSColors.HexToColorAlpha("B124247F") }, // 14
                { ThemeGroup.Event_Color_15, LSColors.HexToColorAlpha("64B4F67F") }, // 15

                { ThemeGroup.Event_Color_1_Keyframe, LSColors.HexToColorAlpha("673AB7FF") }, // 1
                { ThemeGroup.Event_Color_2_Keyframe, LSColors.HexToColorAlpha("3E51B4FF") }, // 2
                { ThemeGroup.Event_Color_3_Keyframe, LSColors.HexToColorAlpha("2196F3FF") }, // 3
                { ThemeGroup.Event_Color_4_Keyframe, LSColors.HexToColorAlpha("03A9F4FF") }, // 4
                { ThemeGroup.Event_Color_5_Keyframe, LSColors.HexToColorAlpha("00BCD4FF") }, // 5
                { ThemeGroup.Event_Color_6_Keyframe, LSColors.HexToColorAlpha("009688FF") }, // 6
                { ThemeGroup.Event_Color_7_Keyframe, LSColors.HexToColorAlpha("4BAF50FF") }, // 7
                { ThemeGroup.Event_Color_8_Keyframe, LSColors.HexToColorAlpha("7CB341FF") }, // 8
                { ThemeGroup.Event_Color_9_Keyframe, LSColors.HexToColorAlpha("AFB42BFF") }, // 9
                { ThemeGroup.Event_Color_10_Keyframe, LSColors.HexToColorAlpha("FFC107FF") }, // 10
                { ThemeGroup.Event_Color_11_Keyframe, LSColors.HexToColorAlpha("FF9800FF") }, // 11
                { ThemeGroup.Event_Color_12_Keyframe, LSColors.HexToColorAlpha("B96000FF") }, // 12
                { ThemeGroup.Event_Color_13_Keyframe, LSColors.HexToColorAlpha("B12411FF") }, // 13
                { ThemeGroup.Event_Color_14_Keyframe, LSColors.HexToColorAlpha("B12424FF") }, // 14
                { ThemeGroup.Event_Color_15_Keyframe, LSColors.HexToColorAlpha("64B4F6FF") }, // 15

                { ThemeGroup.Event_Color_1_Editor, LSColors.HexToColorAlpha("564B6AFF") }, // 1
                { ThemeGroup.Event_Color_2_Editor, LSColors.HexToColorAlpha("41445EFF") }, // 2
                { ThemeGroup.Event_Color_3_Editor, LSColors.HexToColorAlpha("44627AFF") }, // 3
                { ThemeGroup.Event_Color_4_Editor, LSColors.HexToColorAlpha("315B6EFF") }, // 4
                { ThemeGroup.Event_Color_5_Editor, LSColors.HexToColorAlpha("3E6D73FF") }, // 5
                { ThemeGroup.Event_Color_6_Editor, LSColors.HexToColorAlpha("305653FF") }, // 6
                { ThemeGroup.Event_Color_7_Editor, LSColors.HexToColorAlpha("506951FF") }, // 7
                { ThemeGroup.Event_Color_8_Editor, LSColors.HexToColorAlpha("515E41FF") }, // 8
                { ThemeGroup.Event_Color_9_Editor, LSColors.HexToColorAlpha("676945FF") }, // 9
                { ThemeGroup.Event_Color_10_Editor, LSColors.HexToColorAlpha("726335FF") }, // 10
                { ThemeGroup.Event_Color_11_Editor, LSColors.HexToColorAlpha("FF9800FF") }, // 11
                { ThemeGroup.Event_Color_12_Editor, LSColors.HexToColorAlpha("FF5800FF") }, // 12
                { ThemeGroup.Event_Color_13_Editor, LSColors.HexToColorAlpha("FF2509FF") }, // 13
                { ThemeGroup.Event_Color_14_Editor, LSColors.HexToColorAlpha("FF0F0FFF") }, // 14

                { ThemeGroup.Object_Keyframe_Color_1, LSColors.HexToColorAlpha("F21D0EFF") }, // 1
                { ThemeGroup.Object_Keyframe_Color_2, LSColors.HexToColorAlpha("1BE522FF") }, // 2
                { ThemeGroup.Object_Keyframe_Color_3, LSColors.HexToColorAlpha("0E56F2FF") }, // 3
                { ThemeGroup.Object_Keyframe_Color_4, LSColors.HexToColorAlpha("FF8307FF") }, // 4
            }),
            new EditorTheme($"{nameof(ThemeSetting.Vision)}", new Dictionary<ThemeGroup, Color>
            {
                { ThemeGroup.Background_1, LSColors.HexToColorAlpha("160505FF") },
                { ThemeGroup.Background_2, LSColors.HexToColorAlpha("2B0B0BFF") },
                { ThemeGroup.Background_3, LSColors.HexToColorAlpha("421515FF") },
                { ThemeGroup.Preview_Cover, LSColors.HexToColorAlpha("331717FF") },

                { ThemeGroup.Scrollbar_1_Handle, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Scrollbar_1_Handle_Normal, LSColors.HexToColorAlpha("492626FF") },
                { ThemeGroup.Scrollbar_1_Handle_Highlighted, LSColors.HexToColorAlpha("A34B4BFF") },
                { ThemeGroup.Scrollbar_1_Handle_Selected, LSColors.HexToColorAlpha("A34B4BFF") },
                { ThemeGroup.Scrollbar_1_Handle_Pressed, LSColors.HexToColorAlpha("A34B4BFF") },
                { ThemeGroup.Scrollbar_1_Handle_Disabled, LSColors.HexToColorAlpha("A34B4BFF") },

                { ThemeGroup.Scrollbar_2, LSColors.HexToColorAlpha("3A1820FF") },
                { ThemeGroup.Scrollbar_2_Handle, LSColors.HexToColorAlpha("472727FF") },
                { ThemeGroup.Scrollbar_2_Handle_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Scrollbar_2_Handle_Hightlighted, LSColors.HexToColorAlpha("F5F5F5FF") },
                { ThemeGroup.Scrollbar_2_Handle_Selected, LSColors.HexToColorAlpha("414141FF") },
                { ThemeGroup.Scrollbar_2_Handle_Pressed, LSColors.HexToColorAlpha("F5F5F5FF") },
                { ThemeGroup.Scrollbar_2_Handle_Disabled, LSColors.HexToColorAlpha("C8C8C880") },

                { ThemeGroup.Close, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Close_Normal, LSColors.HexToColorAlpha("C43C33FF") },
                { ThemeGroup.Close_Highlighted, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Close_Selected, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Close_Pressed, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Close_Disabled, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Close_X, LSColors.HexToColorAlpha("EFEBEFFF") },

                { ThemeGroup.Picker, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Picker_Normal, LSColors.HexToColorAlpha("CC2C4CFF") },
                { ThemeGroup.Picker_Highlighted, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Picker_Selected, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Picker_Pressed, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Picker_Disabled, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Picker_Icon, LSColors.HexToColorAlpha("EFEBEFFF") },

                { ThemeGroup.Light_Text, LSColors.HexToColorAlpha("C41532FF") },
                { ThemeGroup.Dark_Text, LSColors.HexToColorAlpha("B51321FF") },

                { ThemeGroup.Function_1, LSColors.HexToColorAlpha("891A2CFF") },
                { ThemeGroup.Function_1_Text, LSColors.HexToColorAlpha("E5E1E5FF") },
                { ThemeGroup.Function_2, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Function_2_Normal, LSColors.HexToColorAlpha("3A2727FF") },
                { ThemeGroup.Function_2_Highlighted, LSColors.HexToColorAlpha("AD2227FF") },
                { ThemeGroup.Function_2_Selected, LSColors.HexToColorAlpha("3D3D3DFF") },
                { ThemeGroup.Function_2_Pressed, LSColors.HexToColorAlpha("AD2227FF") },
                { ThemeGroup.Function_2_Disabled, LSColors.HexToColorAlpha("C7C7C780") },
                { ThemeGroup.Function_2_Text, LSColors.HexToColorAlpha("DB9292FF") },
                { ThemeGroup.Function_3, LSColors.HexToColorAlpha("3A2A2AFF") },
                { ThemeGroup.Function_3_Text, LSColors.HexToColorAlpha("C4A1A1FF") },

                { ThemeGroup.List_Button_1, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.List_Button_1_Normal, LSColors.HexToColorAlpha("260A0AFF") },
                { ThemeGroup.List_Button_1_Highlighted, LSColors.HexToColorAlpha("632727FF") },
                { ThemeGroup.List_Button_1_Selected, LSColors.HexToColorAlpha("632727FF") },
                { ThemeGroup.List_Button_1_Pressed, LSColors.HexToColorAlpha("632727FF") },
                { ThemeGroup.List_Button_1_Disabled, LSColors.HexToColorAlpha("632727FF") },

                { ThemeGroup.List_Button_2, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.List_Button_2_Normal, LSColors.HexToColorAlpha("260A0AFF") },
                { ThemeGroup.List_Button_2_Highlighted, LSColors.HexToColorAlpha("632727FF") },
                { ThemeGroup.List_Button_2_Selected, LSColors.HexToColorAlpha("632727FF") },
                { ThemeGroup.List_Button_2_Pressed, LSColors.HexToColorAlpha("632727FF") },
                { ThemeGroup.List_Button_2_Disabled, LSColors.HexToColorAlpha("632727FF") },
                { ThemeGroup.List_Button_2_Text, LSColors.HexToColorAlpha("C41532FF") },

                { ThemeGroup.Back_Button, LSColors.HexToColorAlpha("54254FFF") },
                { ThemeGroup.Back_Button_Text, LSColors.HexToColorAlpha("F4C1C1FF") },
                { ThemeGroup.Folder_Button, LSColors.HexToColorAlpha("4F3026FF") },
                { ThemeGroup.Folder_Button_Text, LSColors.HexToColorAlpha("F4ABABFF") },
                { ThemeGroup.File_Button, LSColors.HexToColorAlpha("D64D4AFF") },
                { ThemeGroup.File_Button_Text, LSColors.HexToColorAlpha("F5F5F5FF") },

                { ThemeGroup.Search_Field_1, LSColors.HexToColorAlpha("1E1312FF") },
                { ThemeGroup.Search_Field_1_Text, LSColors.HexToColorAlpha("AD1313FF") },
                { ThemeGroup.Search_Field_2, LSColors.HexToColorAlpha("1E1312FF") },
                { ThemeGroup.Search_Field_2_Text, LSColors.HexToColorAlpha("AD1313FF") },

                { ThemeGroup.Add, LSColors.HexToColorAlpha("D30845FF") },
                { ThemeGroup.Add_Text, LSColors.HexToColorAlpha("1C010FFF") },
                { ThemeGroup.Delete, LSColors.HexToColorAlpha("BA1C0BFF") },
                { ThemeGroup.Delete_Text, LSColors.HexToColorAlpha("EFE1E1FF") },
                { ThemeGroup.Delete_Keyframe_BG, LSColors.HexToColorAlpha("600E12FF") },
                { ThemeGroup.Delete_Keyframe_Button, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Delete_Keyframe_Button_Normal, LSColors.HexToColorAlpha("F34235FF") },
                { ThemeGroup.Delete_Keyframe_Button_Highlighted, LSColors.HexToColorAlpha("212121FF") },
                { ThemeGroup.Delete_Keyframe_Button_Selected, LSColors.HexToColorAlpha("212121FF") },
                { ThemeGroup.Delete_Keyframe_Button_Pressed, LSColors.HexToColorAlpha("222222FF") },
                { ThemeGroup.Delete_Keyframe_Button_Disabled, LSColors.HexToColorAlpha("37373880") },

                { ThemeGroup.Prefab, LSColors.HexToColorAlpha("191414FF") },
                { ThemeGroup.Prefab_Text, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Object, LSColors.HexToColorAlpha("8E6264FF") },
                { ThemeGroup.Object_Text, LSColors.HexToColorAlpha("EDB8CDFF") },
                { ThemeGroup.Marker, LSColors.HexToColorAlpha("B72D12FF") },
                { ThemeGroup.Marker_Text, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Checkpoint, LSColors.HexToColorAlpha("BA66C4FF") },
                { ThemeGroup.Checkpoint_Text, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Background_Object, LSColors.HexToColorAlpha("D83A3AFF") },
                { ThemeGroup.Background_Object_Text, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Timeline_Bar, LSColors.HexToColorAlpha("281512FF") },
                { ThemeGroup.Event_Check, LSColors.HexToColorAlpha("BF5E56FF") },
                { ThemeGroup.Event_Check_Text, LSColors.HexToColorAlpha("EFEBEFFF") },

                { ThemeGroup.Dropdown_1, LSColors.HexToColorAlpha("512020FF") },
                { ThemeGroup.Dropdown_1_Overlay, LSColors.HexToColorAlpha("EA5B62FF") },
                { ThemeGroup.Dropdown_1_Item, LSColors.HexToColorAlpha("350909FF") },
                { ThemeGroup.Toggle_1, LSColors.HexToColorAlpha("482121FF") },
                { ThemeGroup.Toggle_1_Check, LSColors.HexToColorAlpha("CC1818FF") },
                { ThemeGroup.Input_Field, LSColors.HexToColorAlpha("2B1515FF") },
                { ThemeGroup.Input_Field_Text, LSColors.HexToColorAlpha("ED1C1CFF") },

                { ThemeGroup.Slider_1, LSColors.HexToColorAlpha("543535FF") },
                { ThemeGroup.Slider_1_Normal, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Slider_1_Highlighted, LSColors.HexToColorAlpha("F5F5F5FF") },
                { ThemeGroup.Slider_1_Selected, LSColors.HexToColorAlpha("F5F5F5FF") },
                { ThemeGroup.Slider_1_Pressed, LSColors.HexToColorAlpha("C8C8C8FF") },
                { ThemeGroup.Slider_1_Disabled, LSColors.HexToColorAlpha("C8C8C880") },
                { ThemeGroup.Slider_1_Handle, LSColors.HexToColorAlpha("FFFFFFFF") },

                { ThemeGroup.Slider_2, LSColors.HexToColorAlpha("2B1A1AFF") },
                { ThemeGroup.Slider_2_Handle, LSColors.HexToColorAlpha("A51212FF") },

                { ThemeGroup.Documentation, LSColors.HexToColorAlpha("D89356FF") },

                { ThemeGroup.Timeline_Background, LSColors.HexToColorAlpha("230303FF") },
                { ThemeGroup.Timeline_Scrollbar, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Timeline_Scrollbar_Normal, LSColors.HexToColorAlpha("752222FF") },
                { ThemeGroup.Timeline_Scrollbar_Highlighted, LSColors.HexToColorAlpha("DB2B2BFF") },
                { ThemeGroup.Timeline_Scrollbar_Selected, LSColors.HexToColorAlpha("DB2B2BFF") },
                { ThemeGroup.Timeline_Scrollbar_Pressed, LSColors.HexToColorAlpha("DB2B2BFF") },
                { ThemeGroup.Timeline_Scrollbar_Disabled, LSColors.HexToColorAlpha("676767FF") },
                { ThemeGroup.Timeline_Scrollbar_Base, LSColors.HexToColorAlpha("3D2323FF") },
                { ThemeGroup.Timeline_Time_Scrollbar, LSColors.HexToColorAlpha("3D2323FF") },

                { ThemeGroup.Title_Bar_Text, LSColors.HexToColorAlpha("E23434FF") },
                { ThemeGroup.Title_Bar_Button, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Title_Bar_Button_Normal, LSColors.HexToColorAlpha("230808FF") },
                { ThemeGroup.Title_Bar_Button_Highlighted, LSColors.HexToColorAlpha("190606FF") },
                { ThemeGroup.Title_Bar_Button_Selected, LSColors.HexToColorAlpha("190606FF") },
                { ThemeGroup.Title_Bar_Button_Pressed, LSColors.HexToColorAlpha("190606FF") },
                { ThemeGroup.Title_Bar_Dropdown, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Title_Bar_Dropdown_Normal, LSColors.HexToColorAlpha("190606FF") },
                { ThemeGroup.Title_Bar_Dropdown_Highlighted, LSColors.HexToColorAlpha("230808FF") },
                { ThemeGroup.Title_Bar_Dropdown_Selected, LSColors.HexToColorAlpha("230808FF") },
                { ThemeGroup.Title_Bar_Dropdown_Pressed, LSColors.HexToColorAlpha("230808FF") },
                { ThemeGroup.Title_Bar_Dropdown_Disabled, LSColors.HexToColorAlpha("230808FF") },

                { ThemeGroup.Warning_Confirm, LSColors.HexToColorAlpha("FF192CFF") },
                { ThemeGroup.Warning_Cancel, LSColors.HexToColorAlpha("CC7B35FF") },

                { ThemeGroup.Notification_Background, LSColors.HexToColorAlpha("1E0808FF") },
                { ThemeGroup.Notification_Info, LSColors.HexToColorAlpha("2D0B0BFF") },
                { ThemeGroup.Notification_Success, LSColors.HexToColorAlpha("D1368EFF") },
                { ThemeGroup.Notification_Error, LSColors.HexToColorAlpha("A81919FF") },
                { ThemeGroup.Notification_Warning, LSColors.HexToColorAlpha("CC431AFF") },

                { ThemeGroup.Copy, LSColors.HexToColorAlpha("BF185EFF") },
                { ThemeGroup.Copy_Text, LSColors.HexToColorAlpha("E5E1E5FF") },
                { ThemeGroup.Paste, LSColors.HexToColorAlpha("BF311EFF") },
                { ThemeGroup.Paste_Text, LSColors.HexToColorAlpha("E5E1E5FF") },

                { ThemeGroup.Tab_Color_1, LSColors.HexToColorAlpha("FF0000FF") },
                { ThemeGroup.Tab_Color_1_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_1_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_1_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_1_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_1_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_2, LSColors.HexToColorAlpha("BA0000FF") },
                { ThemeGroup.Tab_Color_2_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_2_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_2_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_2_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_2_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_3, LSColors.HexToColorAlpha("8C0000FF") },
                { ThemeGroup.Tab_Color_3_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_3_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_3_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_3_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_3_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_4, LSColors.HexToColorAlpha("6B0000FF") },
                { ThemeGroup.Tab_Color_4_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_4_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_4_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_4_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_4_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_5, LSColors.HexToColorAlpha("440000FF") },
                { ThemeGroup.Tab_Color_5_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_5_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_5_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_5_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_5_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_6, LSColors.HexToColorAlpha("230000FF") },
                { ThemeGroup.Tab_Color_6_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_6_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_6_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_6_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_6_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_7, LSColors.HexToColorAlpha("0A0000FF") },
                { ThemeGroup.Tab_Color_7_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_7_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_7_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_7_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_7_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },

                { ThemeGroup.Event_Color_1, LSColors.HexToColorAlpha("FF00007F") }, // 1
			    { ThemeGroup.Event_Color_2, LSColors.HexToColorAlpha("EF00007F") }, // 2
			    { ThemeGroup.Event_Color_3, LSColors.HexToColorAlpha("DD00007F") }, // 3
			    { ThemeGroup.Event_Color_4, LSColors.HexToColorAlpha("CE00007F") }, // 4
			    { ThemeGroup.Event_Color_5, LSColors.HexToColorAlpha("BC00007F") }, // 5
			    { ThemeGroup.Event_Color_6, LSColors.HexToColorAlpha("AA00007F") }, // 6
			    { ThemeGroup.Event_Color_7, LSColors.HexToColorAlpha("9900007F") }, // 7
			    { ThemeGroup.Event_Color_8, LSColors.HexToColorAlpha("8900007F") }, // 8
			    { ThemeGroup.Event_Color_9, LSColors.HexToColorAlpha("7700007F") }, // 9
			    { ThemeGroup.Event_Color_10, LSColors.HexToColorAlpha("6600007F") }, // 10
			    { ThemeGroup.Event_Color_11, LSColors.HexToColorAlpha("5600007F") }, // 11
			    { ThemeGroup.Event_Color_12, LSColors.HexToColorAlpha("4700007F") }, // 12
			    { ThemeGroup.Event_Color_13, LSColors.HexToColorAlpha("3500007F") }, // 13
			    { ThemeGroup.Event_Color_14, LSColors.HexToColorAlpha("2300007F") }, // 14
			    { ThemeGroup.Event_Color_15, LSColors.HexToColorAlpha("1100007F") }, // 15

                { ThemeGroup.Event_Color_1_Keyframe, LSColors.HexToColorAlpha("FF0000FF") }, // 1
                { ThemeGroup.Event_Color_2_Keyframe, LSColors.HexToColorAlpha("EF0000FF") }, // 2
                { ThemeGroup.Event_Color_3_Keyframe, LSColors.HexToColorAlpha("DD0000FF") }, // 3
                { ThemeGroup.Event_Color_4_Keyframe, LSColors.HexToColorAlpha("CE0000FF") }, // 4
                { ThemeGroup.Event_Color_5_Keyframe, LSColors.HexToColorAlpha("BC0000FF") }, // 5
                { ThemeGroup.Event_Color_6_Keyframe, LSColors.HexToColorAlpha("AA0000FF") }, // 6
                { ThemeGroup.Event_Color_7_Keyframe, LSColors.HexToColorAlpha("990000FF") }, // 7
                { ThemeGroup.Event_Color_8_Keyframe, LSColors.HexToColorAlpha("890000FF") }, // 8
                { ThemeGroup.Event_Color_9_Keyframe, LSColors.HexToColorAlpha("770000FF") }, // 9
                { ThemeGroup.Event_Color_10_Keyframe, LSColors.HexToColorAlpha("660000FF") }, // 10
                { ThemeGroup.Event_Color_11_Keyframe, LSColors.HexToColorAlpha("560000FF") }, // 11
                { ThemeGroup.Event_Color_12_Keyframe, LSColors.HexToColorAlpha("470000FF") }, // 12
                { ThemeGroup.Event_Color_13_Keyframe, LSColors.HexToColorAlpha("350000FF") }, // 13
                { ThemeGroup.Event_Color_14_Keyframe, LSColors.HexToColorAlpha("230000FF") }, // 14
                { ThemeGroup.Event_Color_15_Keyframe, LSColors.HexToColorAlpha("110000FF") }, // 15

                { ThemeGroup.Event_Color_1_Editor, LSColors.HexToColorAlpha("FF0000FF") }, // 1
                { ThemeGroup.Event_Color_2_Editor, LSColors.HexToColorAlpha("EF0000FF") }, // 2
                { ThemeGroup.Event_Color_3_Editor, LSColors.HexToColorAlpha("DD0000FF") }, // 3
                { ThemeGroup.Event_Color_4_Editor, LSColors.HexToColorAlpha("CE0000FF") }, // 4
                { ThemeGroup.Event_Color_5_Editor, LSColors.HexToColorAlpha("BC0000FF") }, // 5
                { ThemeGroup.Event_Color_6_Editor, LSColors.HexToColorAlpha("AA0000FF") }, // 6
                { ThemeGroup.Event_Color_7_Editor, LSColors.HexToColorAlpha("990000FF") }, // 7
                { ThemeGroup.Event_Color_8_Editor, LSColors.HexToColorAlpha("890000FF") }, // 8
                { ThemeGroup.Event_Color_9_Editor, LSColors.HexToColorAlpha("770000FF") }, // 9
                { ThemeGroup.Event_Color_10_Editor, LSColors.HexToColorAlpha("660000FF") }, // 10
                { ThemeGroup.Event_Color_11_Editor, LSColors.HexToColorAlpha("560000FF") }, // 11
                { ThemeGroup.Event_Color_12_Editor, LSColors.HexToColorAlpha("470000FF") }, // 12
                { ThemeGroup.Event_Color_13_Editor, LSColors.HexToColorAlpha("350000FF") }, // 13
                { ThemeGroup.Event_Color_14_Editor, LSColors.HexToColorAlpha("230000FF") }, // 14

                { ThemeGroup.Object_Keyframe_Color_1, LSColors.HexToColorAlpha("E21414FF") }, // 1
                { ThemeGroup.Object_Keyframe_Color_2, LSColors.HexToColorAlpha("E25B44FF") }, // 2
                { ThemeGroup.Object_Keyframe_Color_3, LSColors.HexToColorAlpha("9B1438FF") }, // 3
                { ThemeGroup.Object_Keyframe_Color_4, LSColors.HexToColorAlpha("BA1279FF") }, // 4
            }),
            new EditorTheme($"{nameof(ThemeSetting.Butter)}", new Dictionary<ThemeGroup, Color>
            {
                { ThemeGroup.Background_1, LSColors.HexToColorAlpha("1E140BFF") },
                { ThemeGroup.Background_2, LSColors.HexToColorAlpha("2B2017FF") },
                { ThemeGroup.Background_3, LSColors.HexToColorAlpha("231809FF") },
                { ThemeGroup.Preview_Cover, LSColors.HexToColorAlpha("191004FF") },

                { ThemeGroup.Scrollbar_1_Handle, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Scrollbar_1_Handle_Normal, LSColors.HexToColorAlpha("49433EFF") },
                { ThemeGroup.Scrollbar_1_Handle_Highlighted, LSColors.HexToColorAlpha("5E5044FF") },
                { ThemeGroup.Scrollbar_1_Handle_Selected, LSColors.HexToColorAlpha("5E5044FF") },
                { ThemeGroup.Scrollbar_1_Handle_Pressed, LSColors.HexToColorAlpha("5E5044FF") },
                { ThemeGroup.Scrollbar_1_Handle_Disabled, LSColors.HexToColorAlpha("5E5044FF") },

                { ThemeGroup.Scrollbar_2, LSColors.HexToColorAlpha("231F15FF") },
                { ThemeGroup.Scrollbar_2_Handle, LSColors.HexToColorAlpha("3F3328FF") },
                { ThemeGroup.Scrollbar_2_Handle_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Scrollbar_2_Handle_Hightlighted, LSColors.HexToColorAlpha("F5F5F5FF") },
                { ThemeGroup.Scrollbar_2_Handle_Selected, LSColors.HexToColorAlpha("414141FF") },
                { ThemeGroup.Scrollbar_2_Handle_Pressed, LSColors.HexToColorAlpha("F5F5F5FF") },
                { ThemeGroup.Scrollbar_2_Handle_Disabled, LSColors.HexToColorAlpha("C8C8C880") },

                { ThemeGroup.Close, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Close_Normal, LSColors.HexToColorAlpha("AA4D2EFF") },
                { ThemeGroup.Close_Highlighted, LSColors.HexToColorAlpha("382716FF") },
                { ThemeGroup.Close_Selected, LSColors.HexToColorAlpha("382716FF") },
                { ThemeGroup.Close_Pressed, LSColors.HexToColorAlpha("382716FF") },
                { ThemeGroup.Close_Disabled, LSColors.HexToColorAlpha("382716FF") },
                { ThemeGroup.Close_X, LSColors.HexToColorAlpha("EFEBEFFF") },

                { ThemeGroup.Picker, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Picker_Normal, LSColors.HexToColorAlpha("C4A148FF") },
                { ThemeGroup.Picker_Highlighted, LSColors.HexToColorAlpha("382716FF") },
                { ThemeGroup.Picker_Selected, LSColors.HexToColorAlpha("382716FF") },
                { ThemeGroup.Picker_Pressed, LSColors.HexToColorAlpha("382716FF") },
                { ThemeGroup.Picker_Disabled, LSColors.HexToColorAlpha("382716FF") },
                { ThemeGroup.Picker_Icon, LSColors.HexToColorAlpha("EFEBEFFF") },

                { ThemeGroup.Light_Text, LSColors.HexToColorAlpha("CCC39FFF") },
                { ThemeGroup.Dark_Text, LSColors.HexToColorAlpha("CCC39FFF") },

                { ThemeGroup.Function_1, LSColors.HexToColorAlpha("288220FF") },
                { ThemeGroup.Function_1_Text, LSColors.HexToColorAlpha("E5E1E5FF") },
                { ThemeGroup.Function_2, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Function_2_Normal, LSColors.HexToColorAlpha("3A3229FF") },
                { ThemeGroup.Function_2_Highlighted, LSColors.HexToColorAlpha("BC9C49FF") },
                { ThemeGroup.Function_2_Selected, LSColors.HexToColorAlpha("BC9C49FF") },
                { ThemeGroup.Function_2_Pressed, LSColors.HexToColorAlpha("BC9C49FF") },
                { ThemeGroup.Function_2_Disabled, LSColors.HexToColorAlpha("C7C7C780") },
                { ThemeGroup.Function_2_Text, LSColors.HexToColorAlpha("C6C6C6FF") },
                { ThemeGroup.Function_3, LSColors.HexToColorAlpha("3A352AFF") },
                { ThemeGroup.Function_3_Text, LSColors.HexToColorAlpha("C6C6C6FF") },

                { ThemeGroup.List_Button_1, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.List_Button_1_Normal, LSColors.HexToColorAlpha("261A11FF") },
                { ThemeGroup.List_Button_1_Highlighted, LSColors.HexToColorAlpha("4C331EFF") },
                { ThemeGroup.List_Button_1_Selected, LSColors.HexToColorAlpha("4C331EFF") },
                { ThemeGroup.List_Button_1_Pressed, LSColors.HexToColorAlpha("4C331EFF") },
                { ThemeGroup.List_Button_1_Disabled, LSColors.HexToColorAlpha("4C331EFF") },

                { ThemeGroup.List_Button_2, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.List_Button_2_Normal, LSColors.HexToColorAlpha("261A11FF") },
                { ThemeGroup.List_Button_2_Highlighted, LSColors.HexToColorAlpha("4C331EFF") },
                { ThemeGroup.List_Button_2_Selected, LSColors.HexToColorAlpha("4C331EFF") },
                { ThemeGroup.List_Button_2_Pressed, LSColors.HexToColorAlpha("4C331EFF") },
                { ThemeGroup.List_Button_2_Disabled, LSColors.HexToColorAlpha("4C331EFF") },
                { ThemeGroup.List_Button_2_Text, LSColors.HexToColorAlpha("4C331EFF") },

                { ThemeGroup.Back_Button, LSColors.HexToColorAlpha("255429FF") },
                { ThemeGroup.Back_Button_Text, LSColors.HexToColorAlpha("F4C095FF") },
                { ThemeGroup.Folder_Button, LSColors.HexToColorAlpha("4F402DFF") },
                { ThemeGroup.Folder_Button_Text, LSColors.HexToColorAlpha("F4C095FF") },
                { ThemeGroup.File_Button, LSColors.HexToColorAlpha("B28F4EFF") },
                { ThemeGroup.File_Button_Text, LSColors.HexToColorAlpha("F4C095FF") },

                { ThemeGroup.Search_Field_1, LSColors.HexToColorAlpha("353123FF") },
                { ThemeGroup.Search_Field_1_Text, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Search_Field_2, LSColors.HexToColorAlpha("353123FF") },
                { ThemeGroup.Search_Field_2_Text, LSColors.HexToColorAlpha("FFFFFFFF") },

                { ThemeGroup.Add, LSColors.HexToColorAlpha("DBBF46FF") },
                { ThemeGroup.Add_Text, LSColors.HexToColorAlpha("191C01FF") },
                { ThemeGroup.Delete, LSColors.HexToColorAlpha("F34235") },
                { ThemeGroup.Delete_Text, LSColors.HexToColorAlpha("CEB698FF") },
                { ThemeGroup.Delete_Keyframe_BG, LSColors.HexToColorAlpha("EDD5C0FF") },
                { ThemeGroup.Delete_Keyframe_Button, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Delete_Keyframe_Button_Normal, LSColors.HexToColorAlpha("F34235") },
                { ThemeGroup.Delete_Keyframe_Button_Highlighted, LSColors.HexToColorAlpha("2D210EFF") },
                { ThemeGroup.Delete_Keyframe_Button_Selected, LSColors.HexToColorAlpha("2D210EFF") },
                { ThemeGroup.Delete_Keyframe_Button_Pressed, LSColors.HexToColorAlpha("2D210EFF") },
                { ThemeGroup.Delete_Keyframe_Button_Disabled, LSColors.HexToColorAlpha("37373880") },

                { ThemeGroup.Prefab, LSColors.HexToColorAlpha("19130AFF") },
                { ThemeGroup.Prefab_Text, LSColors.HexToColorAlpha("EDDD95FF") },
                { ThemeGroup.Object, LSColors.HexToColorAlpha("8E856FFF") },
                { ThemeGroup.Object_Text, LSColors.HexToColorAlpha("EDDD95FF") },
                { ThemeGroup.Marker, LSColors.HexToColorAlpha("C6861FFF") },
                { ThemeGroup.Marker_Text, LSColors.HexToColorAlpha("EDDD95FF") },
                { ThemeGroup.Checkpoint, LSColors.HexToColorAlpha("BCB247FF") },
                { ThemeGroup.Checkpoint_Text, LSColors.HexToColorAlpha("EDDD95FF") },
                { ThemeGroup.Background_Object, LSColors.HexToColorAlpha("AA6F47FF") },
                { ThemeGroup.Background_Object_Text, LSColors.HexToColorAlpha("EDDD95FF") },
                { ThemeGroup.Timeline_Bar, LSColors.HexToColorAlpha("161201FF") },
                { ThemeGroup.Event_Check, LSColors.HexToColorAlpha("80A521FF") },
                { ThemeGroup.Event_Check_Text, LSColors.HexToColorAlpha("EDDD95FF") },

                { ThemeGroup.Dropdown_1, LSColors.HexToColorAlpha("352F25FF") },
                { ThemeGroup.Dropdown_1_Overlay, LSColors.HexToColorAlpha("EAD2A1FF") },
                { ThemeGroup.Dropdown_1_Item, LSColors.HexToColorAlpha("544529FF") },
                { ThemeGroup.Toggle_1, LSColors.HexToColorAlpha("2D2515FF") },
                { ThemeGroup.Toggle_1_Check, LSColors.HexToColorAlpha("EAD2A1FF") },
                { ThemeGroup.Input_Field, LSColors.HexToColorAlpha("3A2B15FF") },
                { ThemeGroup.Input_Field_Text, LSColors.HexToColorAlpha("BFB174FF") },

                { ThemeGroup.Slider_1, LSColors.HexToColorAlpha("544C3DFF") },
                { ThemeGroup.Slider_1_Normal, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Slider_1_Highlighted, LSColors.HexToColorAlpha("F5F5F5FF") },
                { ThemeGroup.Slider_1_Selected, LSColors.HexToColorAlpha("F5F5F5FF") },
                { ThemeGroup.Slider_1_Pressed, LSColors.HexToColorAlpha("C8C8C8FF") },
                { ThemeGroup.Slider_1_Disabled, LSColors.HexToColorAlpha("C8C8C880") },
                { ThemeGroup.Slider_1_Handle, LSColors.HexToColorAlpha("FFFFFFFF") },

                { ThemeGroup.Slider_2, LSColors.HexToColorAlpha("3A2D1EFF") },
                { ThemeGroup.Slider_2_Handle, LSColors.HexToColorAlpha("564534FF") },

                { ThemeGroup.Documentation, LSColors.HexToColorAlpha("D89356FF") },

                { ThemeGroup.Timeline_Background, LSColors.HexToColorAlpha("1E1308FF") },
                { ThemeGroup.Timeline_Scrollbar, LSColors.HexToColorAlpha("665F53FF") },
                { ThemeGroup.Timeline_Scrollbar_Normal, LSColors.HexToColorAlpha("676767FF") },
                { ThemeGroup.Timeline_Scrollbar_Highlighted, LSColors.HexToColorAlpha("9E9E9EFF") },
                { ThemeGroup.Timeline_Scrollbar_Selected, LSColors.HexToColorAlpha("9D9D9DFF") },
                { ThemeGroup.Timeline_Scrollbar_Pressed, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Timeline_Scrollbar_Disabled, LSColors.HexToColorAlpha("676767FF") },
                { ThemeGroup.Timeline_Scrollbar_Base, LSColors.HexToColorAlpha("352B1FFF") },
                { ThemeGroup.Timeline_Time_Scrollbar, LSColors.HexToColorAlpha("352B1FFF") },

                { ThemeGroup.Title_Bar_Text, LSColors.HexToColorAlpha("FFC493FF") },
                { ThemeGroup.Title_Bar_Button, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Title_Bar_Button_Normal, LSColors.HexToColorAlpha("422D1CFF") },
                { ThemeGroup.Title_Bar_Button_Highlighted, LSColors.HexToColorAlpha("28190DFF") },
                { ThemeGroup.Title_Bar_Button_Selected, LSColors.HexToColorAlpha("28190DFF") },
                { ThemeGroup.Title_Bar_Button_Pressed, LSColors.HexToColorAlpha("28190DFF") },
                { ThemeGroup.Title_Bar_Dropdown, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Title_Bar_Dropdown_Normal, LSColors.HexToColorAlpha("28190DFF") },
                { ThemeGroup.Title_Bar_Dropdown_Highlighted, LSColors.HexToColorAlpha("422D1CFF") },
                { ThemeGroup.Title_Bar_Dropdown_Selected, LSColors.HexToColorAlpha("422D1CFF") },
                { ThemeGroup.Title_Bar_Dropdown_Pressed, LSColors.HexToColorAlpha("422D1CFF") },
                { ThemeGroup.Title_Bar_Dropdown_Disabled, LSColors.HexToColorAlpha("422D1CFF") },

                { ThemeGroup.Warning_Confirm, LSColors.HexToColorAlpha("FF3E30FF") },
                { ThemeGroup.Warning_Cancel, LSColors.HexToColorAlpha("BFB241FF") },

                { ThemeGroup.Notification_Background, LSColors.HexToColorAlpha("26160EFF") },
                { ThemeGroup.Notification_Info, LSColors.HexToColorAlpha("161109FF") },
                { ThemeGroup.Notification_Success, LSColors.HexToColorAlpha("D1D16EFF") },
                { ThemeGroup.Notification_Error, LSColors.HexToColorAlpha("AA3D3DFF") },
                { ThemeGroup.Notification_Warning, LSColors.HexToColorAlpha("CE8633FF") },

                { ThemeGroup.Copy, LSColors.HexToColorAlpha("897F11FF") },
                { ThemeGroup.Copy_Text, LSColors.HexToColorAlpha("E5E1E5FF") },
                { ThemeGroup.Paste, LSColors.HexToColorAlpha("C18736FF") },
                { ThemeGroup.Paste_Text, LSColors.HexToColorAlpha("E5E1E5FF") },

                { ThemeGroup.Tab_Color_1, LSColors.HexToColorAlpha("1395BAFF") },
                { ThemeGroup.Tab_Color_1_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_1_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_1_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_1_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_1_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_2, LSColors.HexToColorAlpha("0D3C55FF") },
                { ThemeGroup.Tab_Color_2_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_2_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_2_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_2_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_2_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_3, LSColors.HexToColorAlpha("C02E1DFF") },
                { ThemeGroup.Tab_Color_3_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_3_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_3_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_3_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_3_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_4, LSColors.HexToColorAlpha("F16C20FF") },
                { ThemeGroup.Tab_Color_4_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_4_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_4_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_4_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_4_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_5, LSColors.HexToColorAlpha("EBC844FF") },
                { ThemeGroup.Tab_Color_5_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_5_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_5_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_5_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_5_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_6, LSColors.HexToColorAlpha("A2B86CFF") },
                { ThemeGroup.Tab_Color_6_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_6_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_6_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_6_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_6_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_7, LSColors.HexToColorAlpha("1B1B1CFF") },
                { ThemeGroup.Tab_Color_7_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_7_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_7_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_7_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_7_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },

                { ThemeGroup.Event_Color_1, LSColors.HexToColorAlpha("241A357F") }, // 1
			    { ThemeGroup.Event_Color_2, LSColors.HexToColorAlpha("1F24447F") }, // 2
			    { ThemeGroup.Event_Color_3, LSColors.HexToColorAlpha("1930427F") }, // 3
			    { ThemeGroup.Event_Color_4, LSColors.HexToColorAlpha("102C387F") }, // 4
			    { ThemeGroup.Event_Color_5, LSColors.HexToColorAlpha("14393D7F") }, // 5
			    { ThemeGroup.Event_Color_6, LSColors.HexToColorAlpha("1644407F") }, // 6
			    { ThemeGroup.Event_Color_7, LSColors.HexToColorAlpha("2749287F") }, // 7
			    { ThemeGroup.Event_Color_8, LSColors.HexToColorAlpha("2E3D1A7F") }, // 8
			    { ThemeGroup.Event_Color_9, LSColors.HexToColorAlpha("4042257F") }, // 9
			    { ThemeGroup.Event_Color_10, LSColors.HexToColorAlpha("5947117F") }, // 10
			    { ThemeGroup.Event_Color_11, LSColors.HexToColorAlpha("562E137F") }, // 11
			    { ThemeGroup.Event_Color_12, LSColors.HexToColorAlpha("66280E7F") }, // 12
			    { ThemeGroup.Event_Color_13, LSColors.HexToColorAlpha("5611067F") }, // 13
			    { ThemeGroup.Event_Color_14, LSColors.HexToColorAlpha("3D00007F") }, // 14
			    { ThemeGroup.Event_Color_15, LSColors.HexToColorAlpha("1168AF7F") }, // 15

                { ThemeGroup.Event_Color_1_Keyframe, LSColors.HexToColorAlpha("673AB7FF") }, // 1
                { ThemeGroup.Event_Color_2_Keyframe, LSColors.HexToColorAlpha("3E51B4FF") }, // 2
                { ThemeGroup.Event_Color_3_Keyframe, LSColors.HexToColorAlpha("2196F3FF") }, // 3
                { ThemeGroup.Event_Color_4_Keyframe, LSColors.HexToColorAlpha("03A9F4FF") }, // 4
                { ThemeGroup.Event_Color_5_Keyframe, LSColors.HexToColorAlpha("00BCD4FF") }, // 5
                { ThemeGroup.Event_Color_6_Keyframe, LSColors.HexToColorAlpha("009688FF") }, // 6
                { ThemeGroup.Event_Color_7_Keyframe, LSColors.HexToColorAlpha("4BAF50FF") }, // 7
                { ThemeGroup.Event_Color_8_Keyframe, LSColors.HexToColorAlpha("7CB341FF") }, // 8
                { ThemeGroup.Event_Color_9_Keyframe, LSColors.HexToColorAlpha("AFB42BFF") }, // 9
                { ThemeGroup.Event_Color_10_Keyframe, LSColors.HexToColorAlpha("FFC107FF") }, // 10
                { ThemeGroup.Event_Color_11_Keyframe, LSColors.HexToColorAlpha("FF9800FF") }, // 11
                { ThemeGroup.Event_Color_12_Keyframe, LSColors.HexToColorAlpha("B96000FF") }, // 12
                { ThemeGroup.Event_Color_13_Keyframe, LSColors.HexToColorAlpha("B12411FF") }, // 13
                { ThemeGroup.Event_Color_14_Keyframe, LSColors.HexToColorAlpha("B12424FF") }, // 14
                { ThemeGroup.Event_Color_15_Keyframe, LSColors.HexToColorAlpha("64B4F6FF") }, // 15

                { ThemeGroup.Event_Color_1_Editor, LSColors.HexToColorAlpha("564B6AFF") }, // 1
                { ThemeGroup.Event_Color_2_Editor, LSColors.HexToColorAlpha("41445EFF") }, // 2
                { ThemeGroup.Event_Color_3_Editor, LSColors.HexToColorAlpha("44627AFF") }, // 3
                { ThemeGroup.Event_Color_4_Editor, LSColors.HexToColorAlpha("315B6EFF") }, // 4
                { ThemeGroup.Event_Color_5_Editor, LSColors.HexToColorAlpha("3E6D73FF") }, // 5
                { ThemeGroup.Event_Color_6_Editor, LSColors.HexToColorAlpha("305653FF") }, // 6
                { ThemeGroup.Event_Color_7_Editor, LSColors.HexToColorAlpha("506951FF") }, // 7
                { ThemeGroup.Event_Color_8_Editor, LSColors.HexToColorAlpha("515E41FF") }, // 8
                { ThemeGroup.Event_Color_9_Editor, LSColors.HexToColorAlpha("676945FF") }, // 9
                { ThemeGroup.Event_Color_10_Editor, LSColors.HexToColorAlpha("726335FF") }, // 10
                { ThemeGroup.Event_Color_11_Editor, LSColors.HexToColorAlpha("FF9800FF") }, // 11
                { ThemeGroup.Event_Color_12_Editor, LSColors.HexToColorAlpha("FF5800FF") }, // 12
                { ThemeGroup.Event_Color_13_Editor, LSColors.HexToColorAlpha("FF2509FF") }, // 13
                { ThemeGroup.Event_Color_14_Editor, LSColors.HexToColorAlpha("FF0F0FFF") }, // 14

                { ThemeGroup.Object_Keyframe_Color_1, LSColors.HexToColorAlpha("E57373FF") }, // 1
                { ThemeGroup.Object_Keyframe_Color_2, LSColors.HexToColorAlpha("81C784FF") }, // 2
                { ThemeGroup.Object_Keyframe_Color_3, LSColors.HexToColorAlpha("64B5F6FF") }, // 3
                { ThemeGroup.Object_Keyframe_Color_4, LSColors.HexToColorAlpha("FFB74DFF") }, // 4
            }),
            new EditorTheme($"{nameof(ThemeSetting.Arrhythmia)}", new Dictionary<ThemeGroup, Color>
            {
                { ThemeGroup.Background_1, LSColors.HexToColorAlpha("1C140FFF") },
                { ThemeGroup.Background_2, LSColors.HexToColorAlpha("19110AFF") },
                { ThemeGroup.Background_3, LSColors.HexToColorAlpha("352A26FF") },
                { ThemeGroup.Preview_Cover, LSColors.HexToColorAlpha("19110AFF") },

                { ThemeGroup.Scrollbar_1_Handle, LSColors.HexToColorAlpha("C8C8C8FF") },
                { ThemeGroup.Scrollbar_1_Handle_Normal, LSColors.HexToColorAlpha("C7C7C7FF") },
                { ThemeGroup.Scrollbar_1_Handle_Highlighted, LSColors.HexToColorAlpha("414141FF") },
                { ThemeGroup.Scrollbar_1_Handle_Selected, LSColors.HexToColorAlpha("414141FF") },
                { ThemeGroup.Scrollbar_1_Handle_Pressed, LSColors.HexToColorAlpha("414141FF") },
                { ThemeGroup.Scrollbar_1_Handle_Disabled, LSColors.HexToColorAlpha("414141FF") },

                { ThemeGroup.Scrollbar_2, LSColors.HexToColorAlpha("EEEAEEFF") },
                { ThemeGroup.Scrollbar_2_Handle, LSColors.HexToColorAlpha("424242FF") },
                { ThemeGroup.Scrollbar_2_Handle_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Scrollbar_2_Handle_Hightlighted, LSColors.HexToColorAlpha("F5F5F5FF") },
                { ThemeGroup.Scrollbar_2_Handle_Selected, LSColors.HexToColorAlpha("414141FF") },
                { ThemeGroup.Scrollbar_2_Handle_Pressed, LSColors.HexToColorAlpha("F5F5F5FF") },
                { ThemeGroup.Scrollbar_2_Handle_Disabled, LSColors.HexToColorAlpha("C8C8C880") },

                { ThemeGroup.Close, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Close_Normal, LSColors.HexToColorAlpha("F44336FF") },
                { ThemeGroup.Close_Highlighted, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Close_Selected, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Close_Pressed, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Close_Disabled, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Close_X, LSColors.HexToColorAlpha("EFEBEFFF") },

                { ThemeGroup.Picker, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Picker_Normal, LSColors.HexToColorAlpha("0E7BF7FF") },
                { ThemeGroup.Picker_Highlighted, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Picker_Selected, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Picker_Pressed, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Picker_Disabled, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Picker_Icon, LSColors.HexToColorAlpha("EFEBEFFF") },

                { ThemeGroup.Light_Text, LSColors.HexToColorAlpha("E5E1E5FF") },
                { ThemeGroup.Dark_Text, LSColors.HexToColorAlpha("323232FF") },

                { ThemeGroup.Function_1, LSColors.HexToColorAlpha("4E99F4FF") }, // 0F7BF8FF
                { ThemeGroup.Function_1_Text, LSColors.HexToColorAlpha("1B1B1CFF") },
                { ThemeGroup.Function_2, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Function_2_Normal, LSColors.HexToColorAlpha("F2762AFF") },
                { ThemeGroup.Function_2_Highlighted, LSColors.HexToColorAlpha("C86F6FFF") },
                { ThemeGroup.Function_2_Selected, LSColors.HexToColorAlpha("C86F6FFF") },
                { ThemeGroup.Function_2_Pressed, LSColors.HexToColorAlpha("E0E0E0FF") },
                { ThemeGroup.Function_2_Disabled, LSColors.HexToColorAlpha("C7C7C780") },
                { ThemeGroup.Function_2_Text, LSColors.HexToColorAlpha("323232FF") },
                { ThemeGroup.Function_3, LSColors.HexToColorAlpha("EDE9EDFF") },
                { ThemeGroup.Function_3_Text, LSColors.HexToColorAlpha("0A0A0AFF") },

                { ThemeGroup.List_Button_1, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.List_Button_1_Normal, LSColors.HexToColorAlpha("2A2A2AFF") },
                { ThemeGroup.List_Button_1_Highlighted, LSColors.HexToColorAlpha("424242FF") },
                { ThemeGroup.List_Button_1_Selected, LSColors.HexToColorAlpha("424242FF") },
                { ThemeGroup.List_Button_1_Pressed, LSColors.HexToColorAlpha("424242FF") },
                { ThemeGroup.List_Button_1_Disabled, LSColors.HexToColorAlpha("424242FF") },

                { ThemeGroup.List_Button_2, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.List_Button_2_Normal, LSColors.HexToColorAlpha("EDE9EDFF") },
                { ThemeGroup.List_Button_2_Highlighted, LSColors.HexToColorAlpha("C8C8C8FF") },
                { ThemeGroup.List_Button_2_Selected, LSColors.HexToColorAlpha("C8C8C8FF") },
                { ThemeGroup.List_Button_2_Pressed, LSColors.HexToColorAlpha("C8C8C8FF") },
                { ThemeGroup.List_Button_2_Disabled, LSColors.HexToColorAlpha("96969680") },
                { ThemeGroup.List_Button_2_Text, LSColors.HexToColorAlpha("323232FF") },

                { ThemeGroup.Back_Button, LSColors.HexToColorAlpha("90A4AEFF") },
                { ThemeGroup.Back_Button_Text, LSColors.HexToColorAlpha("222222FF") },
                { ThemeGroup.Folder_Button, LSColors.HexToColorAlpha("BCAAA4FF") },
                { ThemeGroup.Folder_Button_Text, LSColors.HexToColorAlpha("323232FF") },
                { ThemeGroup.File_Button, LSColors.HexToColorAlpha("4076DFFF") },
                { ThemeGroup.File_Button_Text, LSColors.HexToColorAlpha("F5F5F5FF") },

                { ThemeGroup.Search_Field_1, LSColors.HexToColorAlpha("2F2F2FFF") },
                { ThemeGroup.Search_Field_1_Text, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Search_Field_2, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Search_Field_2_Text, LSColors.HexToColorAlpha("2F2F2FFF") },

                { ThemeGroup.Add, LSColors.HexToColorAlpha("4DB6ACFF") },
                { ThemeGroup.Add_Text, LSColors.HexToColorAlpha("202020FF") },
                { ThemeGroup.Delete, LSColors.HexToColorAlpha("E54444FF") },
                { ThemeGroup.Delete_Text, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Delete_Keyframe_BG, LSColors.HexToColorAlpha("EEEAEEFF") },
                { ThemeGroup.Delete_Keyframe_Button, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Delete_Keyframe_Button_Normal, LSColors.HexToColorAlpha("F34235FF") },
                { ThemeGroup.Delete_Keyframe_Button_Highlighted, LSColors.HexToColorAlpha("212121FF") },
                { ThemeGroup.Delete_Keyframe_Button_Selected, LSColors.HexToColorAlpha("212121FF") },
                { ThemeGroup.Delete_Keyframe_Button_Pressed, LSColors.HexToColorAlpha("222222FF") },
                { ThemeGroup.Delete_Keyframe_Button_Disabled, LSColors.HexToColorAlpha("37373880") },

                { ThemeGroup.Prefab, LSColors.HexToColorAlpha("383838FF") },
                { ThemeGroup.Prefab_Text, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Object, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Object_Text, LSColors.HexToColorAlpha("212121FF") },
                { ThemeGroup.Marker, LSColors.HexToColorAlpha("FFAF38FF") },
                { ThemeGroup.Marker_Text, LSColors.HexToColorAlpha("1C1C1DFF") },
                { ThemeGroup.Checkpoint, LSColors.HexToColorAlpha("64B5F6FF") },
                { ThemeGroup.Checkpoint_Text, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Background_Object, LSColors.HexToColorAlpha("E57373FF") },
                { ThemeGroup.Background_Object_Text, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Timeline_Bar, LSColors.HexToColorAlpha("1B1B1CFF") },
                { ThemeGroup.Event_Check, LSColors.HexToColorAlpha("6CCBCFFF") },
                { ThemeGroup.Event_Check_Text, LSColors.HexToColorAlpha("EFEBEFFF") },

                { ThemeGroup.Dropdown_1, LSColors.HexToColorAlpha("EDE9EDFF") },
                { ThemeGroup.Dropdown_1_Overlay, LSColors.HexToColorAlpha("373737FF") },
                { ThemeGroup.Dropdown_1_Item, LSColors.HexToColorAlpha("F5F5F5FF") },
                { ThemeGroup.Toggle_1, LSColors.HexToColorAlpha("F2762AFF") },
                { ThemeGroup.Toggle_1_Check, LSColors.HexToColorAlpha("212121FF") },
                { ThemeGroup.Input_Field, LSColors.HexToColorAlpha("F2762AFF") },
                { ThemeGroup.Input_Field_Text, LSColors.HexToColorAlpha("261100FF") },
                { ThemeGroup.Slider_1, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Slider_1_Normal, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Slider_1_Highlighted, LSColors.HexToColorAlpha("F5F5F5FF") },
                { ThemeGroup.Slider_1_Selected, LSColors.HexToColorAlpha("F5F5F5FF") },
                { ThemeGroup.Slider_1_Pressed, LSColors.HexToColorAlpha("C8C8C8FF") },
                { ThemeGroup.Slider_1_Disabled, LSColors.HexToColorAlpha("C8C8C880") },
                { ThemeGroup.Slider_1_Handle, LSColors.HexToColorAlpha("FFFFFFFF") },

                { ThemeGroup.Slider_2, LSColors.HexToColorAlpha("EEEAEEFF") },
                { ThemeGroup.Slider_2_Handle, LSColors.HexToColorAlpha("424242FF") },

                { ThemeGroup.Documentation, LSColors.HexToColorAlpha("D89356FF") },

                { ThemeGroup.Timeline_Background, LSColors.HexToColorAlpha("1B1B1BFF") },
                { ThemeGroup.Timeline_Scrollbar, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Timeline_Scrollbar_Normal, LSColors.HexToColorAlpha("676767FF") },
                { ThemeGroup.Timeline_Scrollbar_Highlighted, LSColors.HexToColorAlpha("9E9E9EFF") },
                { ThemeGroup.Timeline_Scrollbar_Selected, LSColors.HexToColorAlpha("9D9D9DFF") },
                { ThemeGroup.Timeline_Scrollbar_Pressed, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Timeline_Scrollbar_Disabled, LSColors.HexToColorAlpha("676767FF") },
                { ThemeGroup.Timeline_Scrollbar_Base, LSColors.HexToColorAlpha("3E3E42FF") },
                { ThemeGroup.Timeline_Time_Scrollbar, LSColors.HexToColorAlpha("3E3E40FF") },

                { ThemeGroup.Title_Bar_Text, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Title_Bar_Button, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Title_Bar_Button_Normal, LSColors.HexToColorAlpha("303030FF") },
                { ThemeGroup.Title_Bar_Button_Highlighted, LSColors.HexToColorAlpha("1B1B1CFF") },
                { ThemeGroup.Title_Bar_Button_Selected, LSColors.HexToColorAlpha("1B1B1CFF") },
                { ThemeGroup.Title_Bar_Button_Pressed, LSColors.HexToColorAlpha("1B1B1CFF") },
                { ThemeGroup.Title_Bar_Dropdown, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Title_Bar_Dropdown_Normal, LSColors.HexToColorAlpha("1B1B1CFF") },
                { ThemeGroup.Title_Bar_Dropdown_Highlighted, LSColors.HexToColorAlpha("303030FF") },
                { ThemeGroup.Title_Bar_Dropdown_Selected, LSColors.HexToColorAlpha("303030FF") },
                { ThemeGroup.Title_Bar_Dropdown_Pressed, LSColors.HexToColorAlpha("303030FF") },
                { ThemeGroup.Title_Bar_Dropdown_Disabled, LSColors.HexToColorAlpha("303030FF") },

                { ThemeGroup.Warning_Confirm, LSColors.HexToColorAlpha("FF3645FF") },
                { ThemeGroup.Warning_Cancel, LSColors.HexToColorAlpha("4DB5ABFF") },

                { ThemeGroup.Notification_Background, LSColors.HexToColorAlpha("212121FF") },
                { ThemeGroup.Notification_Info, LSColors.HexToColorAlpha("404040FF") },
                { ThemeGroup.Notification_Success, LSColors.HexToColorAlpha("4DB6ACFF") },
                { ThemeGroup.Notification_Error, LSColors.HexToColorAlpha("E57373FF") },
                { ThemeGroup.Notification_Warning, LSColors.HexToColorAlpha("FFAF38FF") },

                { ThemeGroup.Copy, LSColors.HexToColorAlpha("3DADFFFF") },
                { ThemeGroup.Copy_Text, LSColors.HexToColorAlpha("1C1C1DFF") },
                { ThemeGroup.Paste, LSColors.HexToColorAlpha("FFAF38FF") },
                { ThemeGroup.Paste_Text, LSColors.HexToColorAlpha("1C1C1DFF") },

                { ThemeGroup.Tab_Color_1, LSColors.HexToColorAlpha("FFE7E7FF") },
                { ThemeGroup.Tab_Color_1_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_1_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_1_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_1_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_1_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_2, LSColors.HexToColorAlpha("C0ACE1FF") },
                { ThemeGroup.Tab_Color_2_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_2_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_2_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_2_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_2_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_3, LSColors.HexToColorAlpha("F17BB8FF") },
                { ThemeGroup.Tab_Color_3_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_3_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_3_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_3_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_3_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_4, LSColors.HexToColorAlpha("2F426DFF") },
                { ThemeGroup.Tab_Color_4_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_4_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_4_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_4_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_4_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_5, LSColors.HexToColorAlpha("4076DFFF") },
                { ThemeGroup.Tab_Color_5_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_5_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_5_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_5_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_5_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_6, LSColors.HexToColorAlpha("6CCBCFFF") },
                { ThemeGroup.Tab_Color_6_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_6_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_6_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_6_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_6_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_7, LSColors.HexToColorAlpha("1B1B1CFF") },
                { ThemeGroup.Tab_Color_7_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_7_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_7_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_7_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_7_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },

                { ThemeGroup.Event_Color_1, LSColors.HexToColorAlpha("673AB77F") }, // 1
                { ThemeGroup.Event_Color_2, LSColors.HexToColorAlpha("3E51B47F") }, // 2
                { ThemeGroup.Event_Color_3, LSColors.HexToColorAlpha("2196F37F") }, // 3
                { ThemeGroup.Event_Color_4, LSColors.HexToColorAlpha("03A9F47F") }, // 4
                { ThemeGroup.Event_Color_5, LSColors.HexToColorAlpha("00BCD47F") }, // 5
                { ThemeGroup.Event_Color_6, LSColors.HexToColorAlpha("0096887F") }, // 6
                { ThemeGroup.Event_Color_7, LSColors.HexToColorAlpha("4BAF507F") }, // 7
                { ThemeGroup.Event_Color_8, LSColors.HexToColorAlpha("7CB3417F") }, // 8
                { ThemeGroup.Event_Color_9, LSColors.HexToColorAlpha("AFB42B7F") }, // 9
                { ThemeGroup.Event_Color_10, LSColors.HexToColorAlpha("FFC1077F") }, // 10
                { ThemeGroup.Event_Color_11, LSColors.HexToColorAlpha("FF98007F") }, // 11
                { ThemeGroup.Event_Color_12, LSColors.HexToColorAlpha("B960007F") }, // 12
                { ThemeGroup.Event_Color_13, LSColors.HexToColorAlpha("B124117F") }, // 13
                { ThemeGroup.Event_Color_14, LSColors.HexToColorAlpha("B124247F") }, // 14
                { ThemeGroup.Event_Color_15, LSColors.HexToColorAlpha("64B4F67F") }, // 15

                { ThemeGroup.Event_Color_1_Keyframe, LSColors.HexToColorAlpha("673AB7FF") }, // 1
                { ThemeGroup.Event_Color_2_Keyframe, LSColors.HexToColorAlpha("3E51B4FF") }, // 2
                { ThemeGroup.Event_Color_3_Keyframe, LSColors.HexToColorAlpha("2196F3FF") }, // 3
                { ThemeGroup.Event_Color_4_Keyframe, LSColors.HexToColorAlpha("03A9F4FF") }, // 4
                { ThemeGroup.Event_Color_5_Keyframe, LSColors.HexToColorAlpha("00BCD4FF") }, // 5
                { ThemeGroup.Event_Color_6_Keyframe, LSColors.HexToColorAlpha("009688FF") }, // 6
                { ThemeGroup.Event_Color_7_Keyframe, LSColors.HexToColorAlpha("4BAF50FF") }, // 7
                { ThemeGroup.Event_Color_8_Keyframe, LSColors.HexToColorAlpha("7CB341FF") }, // 8
                { ThemeGroup.Event_Color_9_Keyframe, LSColors.HexToColorAlpha("AFB42BFF") }, // 9
                { ThemeGroup.Event_Color_10_Keyframe, LSColors.HexToColorAlpha("FFC107FF") }, // 10
                { ThemeGroup.Event_Color_11_Keyframe, LSColors.HexToColorAlpha("FF9800FF") }, // 11
                { ThemeGroup.Event_Color_12_Keyframe, LSColors.HexToColorAlpha("B96000FF") }, // 12
                { ThemeGroup.Event_Color_13_Keyframe, LSColors.HexToColorAlpha("B12411FF") }, // 13
                { ThemeGroup.Event_Color_14_Keyframe, LSColors.HexToColorAlpha("B12424FF") }, // 14
                { ThemeGroup.Event_Color_15_Keyframe, LSColors.HexToColorAlpha("64B4F6FF") }, // 15

                { ThemeGroup.Event_Color_1_Editor, LSColors.HexToColorAlpha("564B6AFF") }, // 1
                { ThemeGroup.Event_Color_2_Editor, LSColors.HexToColorAlpha("41445EFF") }, // 2
                { ThemeGroup.Event_Color_3_Editor, LSColors.HexToColorAlpha("44627AFF") }, // 3
                { ThemeGroup.Event_Color_4_Editor, LSColors.HexToColorAlpha("315B6EFF") }, // 4
                { ThemeGroup.Event_Color_5_Editor, LSColors.HexToColorAlpha("3E6D73FF") }, // 5
                { ThemeGroup.Event_Color_6_Editor, LSColors.HexToColorAlpha("305653FF") }, // 6
                { ThemeGroup.Event_Color_7_Editor, LSColors.HexToColorAlpha("506951FF") }, // 7
                { ThemeGroup.Event_Color_8_Editor, LSColors.HexToColorAlpha("515E41FF") }, // 8
                { ThemeGroup.Event_Color_9_Editor, LSColors.HexToColorAlpha("676945FF") }, // 9
                { ThemeGroup.Event_Color_10_Editor, LSColors.HexToColorAlpha("726335FF") }, // 10
                { ThemeGroup.Event_Color_11_Editor, LSColors.HexToColorAlpha("FF9800FF") }, // 11
                { ThemeGroup.Event_Color_12_Editor, LSColors.HexToColorAlpha("FF5800FF") }, // 12
                { ThemeGroup.Event_Color_13_Editor, LSColors.HexToColorAlpha("FF2509FF") }, // 13
                { ThemeGroup.Event_Color_14_Editor, LSColors.HexToColorAlpha("FF0F0FFF") }, // 14

                { ThemeGroup.Object_Keyframe_Color_1, LSColors.HexToColorAlpha("F44336FF") }, // 1
                { ThemeGroup.Object_Keyframe_Color_2, LSColors.HexToColorAlpha("4CAF50FF") }, // 2
                { ThemeGroup.Object_Keyframe_Color_3, LSColors.HexToColorAlpha("2196F3FF") }, // 3
                { ThemeGroup.Object_Keyframe_Color_4, LSColors.HexToColorAlpha("FFC107FF") }, // 4
            }),
            new EditorTheme($"{nameof(ThemeSetting.Modern)}", new Dictionary<ThemeGroup, Color>
            {
                { ThemeGroup.Background_1, LSColors.HexToColorAlpha("111111FF") },
                { ThemeGroup.Background_2, LSColors.HexToColorAlpha("1B1B1CFF") },
                { ThemeGroup.Background_3, LSColors.HexToColorAlpha("3B3B3BFF") },
                { ThemeGroup.Preview_Cover, LSColors.HexToColorAlpha("111111FF") },

                { ThemeGroup.Scrollbar_1_Handle, LSColors.HexToColorAlpha("C8C8C8FF") },
                { ThemeGroup.Scrollbar_1_Handle_Normal, LSColors.HexToColorAlpha("C7C7C7FF") },
                { ThemeGroup.Scrollbar_1_Handle_Highlighted, LSColors.HexToColorAlpha("414141FF") },
                { ThemeGroup.Scrollbar_1_Handle_Selected, LSColors.HexToColorAlpha("414141FF") },
                { ThemeGroup.Scrollbar_1_Handle_Pressed, LSColors.HexToColorAlpha("414141FF") },
                { ThemeGroup.Scrollbar_1_Handle_Disabled, LSColors.HexToColorAlpha("414141FF") },

                { ThemeGroup.Scrollbar_2, LSColors.HexToColorAlpha("EEEAEEFF") },
                { ThemeGroup.Scrollbar_2_Handle, LSColors.HexToColorAlpha("424242FF") },
                { ThemeGroup.Scrollbar_2_Handle_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Scrollbar_2_Handle_Hightlighted, LSColors.HexToColorAlpha("F5F5F5FF") },
                { ThemeGroup.Scrollbar_2_Handle_Selected, LSColors.HexToColorAlpha("414141FF") },
                { ThemeGroup.Scrollbar_2_Handle_Pressed, LSColors.HexToColorAlpha("F5F5F5FF") },
                { ThemeGroup.Scrollbar_2_Handle_Disabled, LSColors.HexToColorAlpha("C8C8C880") },

                { ThemeGroup.Close, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Close_Normal, LSColors.HexToColorAlpha("491F27FF") },
                { ThemeGroup.Close_Highlighted, LSColors.HexToColorAlpha("491F27FF") },
                { ThemeGroup.Close_Selected, LSColors.HexToColorAlpha("491F27FF") },
                { ThemeGroup.Close_Pressed, LSColors.HexToColorAlpha("491F27FF") },
                { ThemeGroup.Close_Disabled, LSColors.HexToColorAlpha("491F27FF") },
                { ThemeGroup.Close_X, LSColors.HexToColorAlpha("FFFFFFFF") },

                { ThemeGroup.Picker, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Picker_Normal, LSColors.HexToColorAlpha("373637FF") },
                { ThemeGroup.Picker_Highlighted, LSColors.HexToColorAlpha("373637FF") },
                { ThemeGroup.Picker_Selected, LSColors.HexToColorAlpha("373637FF") },
                { ThemeGroup.Picker_Pressed, LSColors.HexToColorAlpha("373637FF") },
                { ThemeGroup.Picker_Disabled, LSColors.HexToColorAlpha("373637FF") },
                { ThemeGroup.Picker_Icon, LSColors.HexToColorAlpha("E1E1E1FF") },

                { ThemeGroup.Light_Text, LSColors.HexToColorAlpha("E5E1E5FF") },
                { ThemeGroup.Dark_Text, LSColors.HexToColorAlpha("323232FF") },

                { ThemeGroup.Function_1, LSColors.HexToColorAlpha("00AEEFFF") },
                { ThemeGroup.Function_1_Text, LSColors.HexToColorAlpha("E1E1E1FF") },
                { ThemeGroup.Function_2, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Function_2_Normal, LSColors.HexToColorAlpha("E0E0E0FF") },
                { ThemeGroup.Function_2_Highlighted, LSColors.HexToColorAlpha("C86F6FFF") },
                { ThemeGroup.Function_2_Selected, LSColors.HexToColorAlpha("C86F6FFF") },
                { ThemeGroup.Function_2_Pressed, LSColors.HexToColorAlpha("E0E0E0FF") },
                { ThemeGroup.Function_2_Disabled, LSColors.HexToColorAlpha("C7C7C780") },
                { ThemeGroup.Function_2_Text, LSColors.HexToColorAlpha("323232FF") },
                { ThemeGroup.Function_3, LSColors.HexToColorAlpha("373637FF") },
                { ThemeGroup.Function_3_Text, LSColors.HexToColorAlpha("E1E1E1FF") },

                { ThemeGroup.List_Button_1, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.List_Button_1_Normal, LSColors.HexToColorAlpha("2A2A2AFF") },
                { ThemeGroup.List_Button_1_Highlighted, LSColors.HexToColorAlpha("424242FF") },
                { ThemeGroup.List_Button_1_Selected, LSColors.HexToColorAlpha("424242FF") },
                { ThemeGroup.List_Button_1_Pressed, LSColors.HexToColorAlpha("424242FF") },
                { ThemeGroup.List_Button_1_Disabled, LSColors.HexToColorAlpha("424242FF") },

                { ThemeGroup.List_Button_2, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.List_Button_2_Normal, LSColors.HexToColorAlpha("EDE9EDFF") },
                { ThemeGroup.List_Button_2_Highlighted, LSColors.HexToColorAlpha("C8C8C8FF") },
                { ThemeGroup.List_Button_2_Selected, LSColors.HexToColorAlpha("C8C8C8FF") },
                { ThemeGroup.List_Button_2_Pressed, LSColors.HexToColorAlpha("C8C8C8FF") },
                { ThemeGroup.List_Button_2_Disabled, LSColors.HexToColorAlpha("96969680") },
                { ThemeGroup.List_Button_2_Text, LSColors.HexToColorAlpha("323232FF") },

                { ThemeGroup.Back_Button, LSColors.HexToColorAlpha("90A4AEFF") },
                { ThemeGroup.Back_Button_Text, LSColors.HexToColorAlpha("222222FF") },
                { ThemeGroup.Folder_Button, LSColors.HexToColorAlpha("BCAAA4FF") },
                { ThemeGroup.Folder_Button_Text, LSColors.HexToColorAlpha("323232FF") },
                { ThemeGroup.File_Button, LSColors.HexToColorAlpha("4076DFFF") },
                { ThemeGroup.File_Button_Text, LSColors.HexToColorAlpha("F5F5F5FF") },

                { ThemeGroup.Search_Field_1, LSColors.HexToColorAlpha("2F2F2FFF") },
                { ThemeGroup.Search_Field_1_Text, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Search_Field_2, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Search_Field_2_Text, LSColors.HexToColorAlpha("2F2F2FFF") },

                { ThemeGroup.Add, LSColors.HexToColorAlpha("00AEEFFF") },
                { ThemeGroup.Add_Text, LSColors.HexToColorAlpha("E1E1E1FF") },
                { ThemeGroup.Delete, LSColors.HexToColorAlpha("EF4768FF") },
                { ThemeGroup.Delete_Text, LSColors.HexToColorAlpha("E1E1E1FF") },
                { ThemeGroup.Delete_Keyframe_BG, LSColors.HexToColorAlpha("65373FFF") },
                { ThemeGroup.Delete_Keyframe_Button, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Delete_Keyframe_Button_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Delete_Keyframe_Button_Highlighted, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Delete_Keyframe_Button_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Delete_Keyframe_Button_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Delete_Keyframe_Button_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },

                { ThemeGroup.Prefab, LSColors.HexToColorAlpha("626262FF") },
                { ThemeGroup.Prefab_Text, LSColors.HexToColorAlpha("F7F7F7FF") },
                { ThemeGroup.Object, LSColors.HexToColorAlpha("E1E1E1FF") },
                { ThemeGroup.Object_Text, LSColors.HexToColorAlpha("222222FF") },
                { ThemeGroup.Marker, LSColors.HexToColorAlpha("00AEEFFF") },
                { ThemeGroup.Marker_Text, LSColors.HexToColorAlpha("E1E1E1FF") },
                { ThemeGroup.Checkpoint, LSColors.HexToColorAlpha("41436BFF") },
                { ThemeGroup.Checkpoint_Text, LSColors.HexToColorAlpha("E1E1E1FF") },
                { ThemeGroup.Background_Object, LSColors.HexToColorAlpha("EF4768FF") },
                { ThemeGroup.Background_Object_Text, LSColors.HexToColorAlpha("E1E1E1FF") },
                { ThemeGroup.Timeline_Bar, LSColors.HexToColorAlpha("111111FF") },
                { ThemeGroup.Event_Check, LSColors.HexToColorAlpha("3B3B3BFF") },
                { ThemeGroup.Event_Check_Text, LSColors.HexToColorAlpha("FFFFFFFF") },

                { ThemeGroup.Dropdown_1, LSColors.HexToColorAlpha("EDE9EDFF") },
                { ThemeGroup.Dropdown_1_Overlay, LSColors.HexToColorAlpha("373737FF") },
                { ThemeGroup.Dropdown_1_Item, LSColors.HexToColorAlpha("F5F5F5FF") },
                { ThemeGroup.Toggle_1, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Toggle_1_Check, LSColors.HexToColorAlpha("212121FF") },
                { ThemeGroup.Input_Field, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Input_Field_Text, LSColors.HexToColorAlpha("252525FF") },
                { ThemeGroup.Slider_1, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Slider_1_Normal, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Slider_1_Highlighted, LSColors.HexToColorAlpha("F5F5F5FF") },
                { ThemeGroup.Slider_1_Selected, LSColors.HexToColorAlpha("F5F5F5FF") },
                { ThemeGroup.Slider_1_Pressed, LSColors.HexToColorAlpha("C8C8C8FF") },
                { ThemeGroup.Slider_1_Disabled, LSColors.HexToColorAlpha("C8C8C880") },
                { ThemeGroup.Slider_1_Handle, LSColors.HexToColorAlpha("FFFFFFFF") },

                { ThemeGroup.Slider_2, LSColors.HexToColorAlpha("EEEAEEFF") },
                { ThemeGroup.Slider_2_Handle, LSColors.HexToColorAlpha("424242FF") },

                { ThemeGroup.Documentation, LSColors.HexToColorAlpha("D89356FF") },

                { ThemeGroup.Timeline_Background, LSColors.HexToColorAlpha("161616FF") },
                { ThemeGroup.Timeline_Scrollbar, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Timeline_Scrollbar_Normal, LSColors.HexToColorAlpha("676767FF") },
                { ThemeGroup.Timeline_Scrollbar_Highlighted, LSColors.HexToColorAlpha("9E9E9EFF") },
                { ThemeGroup.Timeline_Scrollbar_Selected, LSColors.HexToColorAlpha("9D9D9DFF") },
                { ThemeGroup.Timeline_Scrollbar_Pressed, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Timeline_Scrollbar_Disabled, LSColors.HexToColorAlpha("676767FF") },
                { ThemeGroup.Timeline_Scrollbar_Base, LSColors.HexToColorAlpha("3C3C3CFF") },
                { ThemeGroup.Timeline_Time_Scrollbar, LSColors.HexToColorAlpha("222222FF") },

                { ThemeGroup.Title_Bar_Text, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Title_Bar_Button, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Title_Bar_Button_Normal, LSColors.HexToColorAlpha("222222FF") },
                { ThemeGroup.Title_Bar_Button_Highlighted, LSColors.HexToColorAlpha("111111FF") },
                { ThemeGroup.Title_Bar_Button_Selected, LSColors.HexToColorAlpha("111111FF") },
                { ThemeGroup.Title_Bar_Button_Pressed, LSColors.HexToColorAlpha("111111FF") },
                { ThemeGroup.Title_Bar_Dropdown, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Title_Bar_Dropdown_Normal, LSColors.HexToColorAlpha("111111FF") },
                { ThemeGroup.Title_Bar_Dropdown_Highlighted, LSColors.HexToColorAlpha("222222FF") },
                { ThemeGroup.Title_Bar_Dropdown_Selected, LSColors.HexToColorAlpha("222222FF") },
                { ThemeGroup.Title_Bar_Dropdown_Pressed, LSColors.HexToColorAlpha("222222FF") },
                { ThemeGroup.Title_Bar_Dropdown_Disabled, LSColors.HexToColorAlpha("222222FF") },

                { ThemeGroup.Warning_Confirm, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Warning_Cancel, LSColors.HexToColorAlpha("FFFFFFFF") },

                { ThemeGroup.Notification_Background, LSColors.HexToColorAlpha("212121FF") },
                { ThemeGroup.Notification_Info, LSColors.HexToColorAlpha("404040FF") },
                { ThemeGroup.Notification_Success, LSColors.HexToColorAlpha("4DB6ACFF") },
                { ThemeGroup.Notification_Error, LSColors.HexToColorAlpha("E57373FF") },
                { ThemeGroup.Notification_Warning, LSColors.HexToColorAlpha("FFAF38FF") },

                { ThemeGroup.Copy, LSColors.HexToColorAlpha("00AEEFFF") },
                { ThemeGroup.Copy_Text, LSColors.HexToColorAlpha("E1E1E1FF") },
                { ThemeGroup.Paste, LSColors.HexToColorAlpha("EF4768FF") },
                { ThemeGroup.Paste_Text, LSColors.HexToColorAlpha("E1E1E1FF") },

                { ThemeGroup.Tab_Color_1, LSColors.HexToColorAlpha("B84255FF") },
                { ThemeGroup.Tab_Color_1_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_1_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_1_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_1_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_1_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_2, LSColors.HexToColorAlpha("2D88B6FF") },
                { ThemeGroup.Tab_Color_2_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_2_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_2_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_2_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_2_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_3, LSColors.HexToColorAlpha("3A3A58FF") },
                { ThemeGroup.Tab_Color_3_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_3_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_3_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_3_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_3_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_4, LSColors.HexToColorAlpha("222222FF") },
                { ThemeGroup.Tab_Color_4_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_4_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_4_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_4_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_4_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_5, LSColors.HexToColorAlpha("41436BFF") },
                { ThemeGroup.Tab_Color_5_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_5_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_5_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_5_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_5_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_6, LSColors.HexToColorAlpha("FCE8C7FF") },
                { ThemeGroup.Tab_Color_6_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_6_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_6_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_6_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_6_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_7, LSColors.HexToColorAlpha("F7B846FF") },
                { ThemeGroup.Tab_Color_7_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_7_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_7_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_7_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_7_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },

                { ThemeGroup.Event_Color_1, new Color(0.9373f, 0.2784f, 0.4118f, 0.149f) }, // 1
                { ThemeGroup.Event_Color_2, new Color(0.0078f, 0.6824f, 0.9412f, 0.149f) }, // 2
                { ThemeGroup.Event_Color_3, new Color(0.1333f, 0.1333f, 0.1333f, 0.149f) }, // 3
                { ThemeGroup.Event_Color_4, new Color(0.4902f, 0.5882f, 0.5647f, 0.149f) }, // 4
                { ThemeGroup.Event_Color_5, new Color(0.7608f, 0.9137f, 0.9608f, 0.149f) }, // 5
                { ThemeGroup.Event_Color_6, new Color(1f, 0.7059f, 0.6353f, 0.149f) }, // 6
                { ThemeGroup.Event_Color_7, new Color(0.7216f, 0.4471f, 0.3529f, 0.149f) }, // 7
                { ThemeGroup.Event_Color_8, new Color(0.9843f, 0.4706f, 0.0627f, 0.149f) }, // 8
                { ThemeGroup.Event_Color_9, new Color(1f, 0.1647f, 0.051f, 0.149f) }, // 9
                { ThemeGroup.Event_Color_10, new Color(0.9373f, 0.298f, 0.8157f, 0.149f) }, // 10
                { ThemeGroup.Event_Color_11, new Color(0.9608f, 0.6039f, 0.9059f, 0.149f) }, // 11
                { ThemeGroup.Event_Color_12, new Color(0.4f, 1f, 0.5333f, 0.149f) }, // 12
                { ThemeGroup.Event_Color_13, new Color(0.2588f, 0.6392f, 0.349f, 0.149f) }, // 13
                { ThemeGroup.Event_Color_14, new Color(0.298f, 0.9882f, 0.8824f, 0.149f) }, // 14
                { ThemeGroup.Event_Color_15, new Color(0.2667f, 0.4863f, 0.9961f, 0.149f) }, // 15

                { ThemeGroup.Event_Color_1_Keyframe, new Color(0.9373f, 0.2784f, 0.4118f, 1f) }, // 1
                { ThemeGroup.Event_Color_2_Keyframe, new Color(0.0078f, 0.6824f, 0.9412f, 1f) }, // 2
                { ThemeGroup.Event_Color_3_Keyframe, new Color(0.1333f, 0.1333f, 0.1333f, 1f) }, // 3
                { ThemeGroup.Event_Color_4_Keyframe, new Color(0.4902f, 0.5882f, 0.5647f, 1f) }, // 4
                { ThemeGroup.Event_Color_5_Keyframe, new Color(0.7608f, 0.9137f, 0.9608f, 1f) }, // 5
                { ThemeGroup.Event_Color_6_Keyframe, new Color(1f, 0.7059f, 0.6353f, 1f) }, // 6
                { ThemeGroup.Event_Color_7_Keyframe, new Color(0.7216f, 0.4471f, 0.3529f, 1f) }, // 7
                { ThemeGroup.Event_Color_8_Keyframe, new Color(0.9843f, 0.4706f, 0.0627f, 1f) }, // 8
                { ThemeGroup.Event_Color_9_Keyframe, new Color(1f, 0.1647f, 0.051f, 1f) }, // 9
                { ThemeGroup.Event_Color_10_Keyframe, new Color(0.9373f, 0.298f, 0.8157f, 1f) }, // 10
                { ThemeGroup.Event_Color_11_Keyframe, new Color(0.9608f, 0.6039f, 0.9059f, 1f) }, // 11
                { ThemeGroup.Event_Color_12_Keyframe, new Color(0.4f, 1f, 0.5333f, 1f) }, // 12
                { ThemeGroup.Event_Color_13_Keyframe, new Color(0.2588f, 0.6392f, 0.349f, 1f) }, // 13
                { ThemeGroup.Event_Color_14_Keyframe, new Color(0.298f, 0.9882f, 0.8824f, 1f) }, // 14
                { ThemeGroup.Event_Color_15_Keyframe, new Color(0.2667f, 0.4863f, 0.9961f, 1f) }, // 15

                { ThemeGroup.Event_Color_1_Editor, LSColors.HexToColorAlpha("41436BFF") }, // 1
                { ThemeGroup.Event_Color_2_Editor, LSColors.HexToColorAlpha("41436BFF") }, // 2
                { ThemeGroup.Event_Color_3_Editor, LSColors.HexToColorAlpha("41436BFF") }, // 3
                { ThemeGroup.Event_Color_4_Editor, LSColors.HexToColorAlpha("41436BFF") }, // 4
                { ThemeGroup.Event_Color_5_Editor, LSColors.HexToColorAlpha("41436BFF") }, // 5
                { ThemeGroup.Event_Color_6_Editor, LSColors.HexToColorAlpha("41436BFF") }, // 6
                { ThemeGroup.Event_Color_7_Editor, LSColors.HexToColorAlpha("41436BFF") }, // 7
                { ThemeGroup.Event_Color_8_Editor, LSColors.HexToColorAlpha("41436BFF") }, // 8
                { ThemeGroup.Event_Color_9_Editor, LSColors.HexToColorAlpha("41436BFF") }, // 9
                { ThemeGroup.Event_Color_10_Editor, LSColors.HexToColorAlpha("41436BFF") }, // 10
                { ThemeGroup.Event_Color_11_Editor, LSColors.HexToColorAlpha("41436BFF") }, // 11
                { ThemeGroup.Event_Color_12_Editor, LSColors.HexToColorAlpha("41436BFF") }, // 12
                { ThemeGroup.Event_Color_13_Editor, LSColors.HexToColorAlpha("41436BFF") }, // 13
                { ThemeGroup.Event_Color_14_Editor, LSColors.HexToColorAlpha("41436BFF") }, // 14

                { ThemeGroup.Object_Keyframe_Color_1, LSColors.HexToColorAlpha("B84255FF") }, // 1
                { ThemeGroup.Object_Keyframe_Color_2, LSColors.HexToColorAlpha("2D88B6FF") }, // 2
                { ThemeGroup.Object_Keyframe_Color_3, LSColors.HexToColorAlpha("3A3A58FF") }, // 3
                { ThemeGroup.Object_Keyframe_Color_4, LSColors.HexToColorAlpha("222222FF") }, // 4
            }),
            new EditorTheme($"{nameof(ThemeSetting.Beats)}", new Dictionary<ThemeGroup, Color>
            {
                { ThemeGroup.Background_1, LSColors.HexToColorAlpha("101D2DFF") },
                { ThemeGroup.Background_2, LSColors.HexToColorAlpha("3F1631FF") },
                { ThemeGroup.Background_3, LSColors.HexToColorAlpha("2B0F21FF") },
                { ThemeGroup.Preview_Cover, LSColors.HexToColorAlpha("0A0A0AFF") },

                { ThemeGroup.Scrollbar_1_Handle, LSColors.HexToColorAlpha("C8C8C8FF") },
                { ThemeGroup.Scrollbar_1_Handle_Normal, LSColors.HexToColorAlpha("C7C7C7FF") },
                { ThemeGroup.Scrollbar_1_Handle_Highlighted, LSColors.HexToColorAlpha("414141FF") },
                { ThemeGroup.Scrollbar_1_Handle_Selected, LSColors.HexToColorAlpha("414141FF") },
                { ThemeGroup.Scrollbar_1_Handle_Pressed, LSColors.HexToColorAlpha("414141FF") },
                { ThemeGroup.Scrollbar_1_Handle_Disabled, LSColors.HexToColorAlpha("414141FF") },

                { ThemeGroup.Scrollbar_2, LSColors.HexToColorAlpha("EEEAEEFF") },
                { ThemeGroup.Scrollbar_2_Handle, LSColors.HexToColorAlpha("424242FF") },
                { ThemeGroup.Scrollbar_2_Handle_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Scrollbar_2_Handle_Hightlighted, LSColors.HexToColorAlpha("F5F5F5FF") },
                { ThemeGroup.Scrollbar_2_Handle_Selected, LSColors.HexToColorAlpha("414141FF") },
                { ThemeGroup.Scrollbar_2_Handle_Pressed, LSColors.HexToColorAlpha("F5F5F5FF") },
                { ThemeGroup.Scrollbar_2_Handle_Disabled, LSColors.HexToColorAlpha("C8C8C880") },

                { ThemeGroup.Close, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Close_Normal, LSColors.HexToColorAlpha("E0007BFF") },
                { ThemeGroup.Close_Highlighted, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Close_Selected, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Close_Pressed, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Close_Disabled, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Close_X, LSColors.HexToColorAlpha("EFEBEFFF") },

                { ThemeGroup.Picker, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Picker_Normal, LSColors.HexToColorAlpha("56CDEAFF") },
                { ThemeGroup.Picker_Highlighted, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Picker_Selected, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Picker_Pressed, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Picker_Disabled, LSColors.HexToColorAlpha("292929FF") },
                { ThemeGroup.Picker_Icon, LSColors.HexToColorAlpha("EFEBEFFF") },

                { ThemeGroup.Light_Text, LSColors.HexToColorAlpha("56CDEAFF") },
                { ThemeGroup.Dark_Text, LSColors.HexToColorAlpha("B7226DFF") },

                { ThemeGroup.Function_1, LSColors.HexToColorAlpha("4E99F4FF") }, // 0F7BF8FF
                { ThemeGroup.Function_1_Text, LSColors.HexToColorAlpha("1B1B1CFF") },
                { ThemeGroup.Function_2, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Function_2_Normal, LSColors.HexToColorAlpha("56CDEAFF") },
                { ThemeGroup.Function_2_Highlighted, LSColors.HexToColorAlpha("E01D75FF") },
                { ThemeGroup.Function_2_Selected, LSColors.HexToColorAlpha("E01D75FF") },
                { ThemeGroup.Function_2_Pressed, LSColors.HexToColorAlpha("E01D75FF") },
                { ThemeGroup.Function_2_Disabled, LSColors.HexToColorAlpha("54063A80") },
                { ThemeGroup.Function_2_Text, LSColors.HexToColorAlpha("323232FF") },
                { ThemeGroup.Function_3, LSColors.HexToColorAlpha("56CDEAFF") },
                { ThemeGroup.Function_3_Text, LSColors.HexToColorAlpha("E0007BFF") },

                { ThemeGroup.List_Button_1, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.List_Button_1_Normal, LSColors.HexToColorAlpha("254368FF") },
                { ThemeGroup.List_Button_1_Highlighted, LSColors.HexToColorAlpha("C14770FF") },
                { ThemeGroup.List_Button_1_Selected, LSColors.HexToColorAlpha("C14770FF") },
                { ThemeGroup.List_Button_1_Pressed, LSColors.HexToColorAlpha("C14770FF") },
                { ThemeGroup.List_Button_1_Disabled, LSColors.HexToColorAlpha("C14770FF") },

                { ThemeGroup.List_Button_2, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.List_Button_2_Normal, LSColors.HexToColorAlpha("C14770FF") },
                { ThemeGroup.List_Button_2_Highlighted, LSColors.HexToColorAlpha("254368FF") },
                { ThemeGroup.List_Button_2_Selected, LSColors.HexToColorAlpha("254368FF") },
                { ThemeGroup.List_Button_2_Pressed, LSColors.HexToColorAlpha("254368FF") },
                { ThemeGroup.List_Button_2_Disabled, LSColors.HexToColorAlpha("254368FF") },
                { ThemeGroup.List_Button_2_Text, LSColors.HexToColorAlpha("56CDEAFF") },

                { ThemeGroup.Back_Button, LSColors.HexToColorAlpha("90A4AEFF") },
                { ThemeGroup.Back_Button_Text, LSColors.HexToColorAlpha("222222FF") },
                { ThemeGroup.Folder_Button, LSColors.HexToColorAlpha("BCAAA4FF") },
                { ThemeGroup.Folder_Button_Text, LSColors.HexToColorAlpha("323232FF") },
                { ThemeGroup.File_Button, LSColors.HexToColorAlpha("4076DFFF") },
                { ThemeGroup.File_Button_Text, LSColors.HexToColorAlpha("F5F5F5FF") },

                { ThemeGroup.Search_Field_1, LSColors.HexToColorAlpha("2A3849FF") },
                { ThemeGroup.Search_Field_1_Text, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Search_Field_2, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Search_Field_2_Text, LSColors.HexToColorAlpha("2F2F2FFF") },

                { ThemeGroup.Add, LSColors.HexToColorAlpha("56CDEAFF") },
                { ThemeGroup.Add_Text, LSColors.HexToColorAlpha("202020FF") },
                { ThemeGroup.Delete, LSColors.HexToColorAlpha("E0007BFF") },
                { ThemeGroup.Delete_Text, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Delete_Keyframe_BG, LSColors.HexToColorAlpha("EEEAEEFF") },
                { ThemeGroup.Delete_Keyframe_Button, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Delete_Keyframe_Button_Normal, LSColors.HexToColorAlpha("E0007BFF") },
                { ThemeGroup.Delete_Keyframe_Button_Highlighted, LSColors.HexToColorAlpha("212121FF") },
                { ThemeGroup.Delete_Keyframe_Button_Selected, LSColors.HexToColorAlpha("212121FF") },
                { ThemeGroup.Delete_Keyframe_Button_Pressed, LSColors.HexToColorAlpha("222222FF") },
                { ThemeGroup.Delete_Keyframe_Button_Disabled, LSColors.HexToColorAlpha("37373880") },

                { ThemeGroup.Prefab, LSColors.HexToColorAlpha("383838FF") },
                { ThemeGroup.Prefab_Text, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Object, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Object_Text, LSColors.HexToColorAlpha("212121FF") },
                { ThemeGroup.Marker, LSColors.HexToColorAlpha("FFF056FF") },
                { ThemeGroup.Marker_Text, LSColors.HexToColorAlpha("1C1C1DFF") },
                { ThemeGroup.Checkpoint, LSColors.HexToColorAlpha("64B5F6FF") },
                { ThemeGroup.Checkpoint_Text, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Background_Object, LSColors.HexToColorAlpha("E57373FF") },
                { ThemeGroup.Background_Object_Text, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Timeline_Bar, LSColors.HexToColorAlpha("380927FF") },
                { ThemeGroup.Event_Check, LSColors.HexToColorAlpha("56CDEAFF") },
                { ThemeGroup.Event_Check_Text, LSColors.HexToColorAlpha("101D2DFF") },

                { ThemeGroup.Dropdown_1, LSColors.HexToColorAlpha("385375FF") },
                { ThemeGroup.Dropdown_1_Overlay, LSColors.HexToColorAlpha("56CDEAFF") },
                { ThemeGroup.Dropdown_1_Item, LSColors.HexToColorAlpha("385375FF") },
                { ThemeGroup.Toggle_1, LSColors.HexToColorAlpha("385375FF") },
                { ThemeGroup.Toggle_1_Check, LSColors.HexToColorAlpha("56CDEAFF") },
                { ThemeGroup.Input_Field, LSColors.HexToColorAlpha("355F93FF") },
                { ThemeGroup.Input_Field_Text, LSColors.HexToColorAlpha("56CDEAFF") },
                { ThemeGroup.Slider_1, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Slider_1_Normal, LSColors.HexToColorAlpha("EFEBEFFF") },
                { ThemeGroup.Slider_1_Highlighted, LSColors.HexToColorAlpha("F5F5F5FF") },
                { ThemeGroup.Slider_1_Selected, LSColors.HexToColorAlpha("F5F5F5FF") },
                { ThemeGroup.Slider_1_Pressed, LSColors.HexToColorAlpha("C8C8C8FF") },
                { ThemeGroup.Slider_1_Disabled, LSColors.HexToColorAlpha("C8C8C880") },
                { ThemeGroup.Slider_1_Handle, LSColors.HexToColorAlpha("FFFFFFFF") },

                { ThemeGroup.Slider_2, LSColors.HexToColorAlpha("385375FF") },
                { ThemeGroup.Slider_2_Handle, LSColors.HexToColorAlpha("56CDEAFF") },

                { ThemeGroup.Documentation, LSColors.HexToColorAlpha("D89356FF") },

                { ThemeGroup.Timeline_Background, LSColors.HexToColorAlpha("380621FF") },
                { ThemeGroup.Timeline_Scrollbar, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Timeline_Scrollbar_Normal, LSColors.HexToColorAlpha("7F185BFF") },
                { ThemeGroup.Timeline_Scrollbar_Highlighted, LSColors.HexToColorAlpha("AA3A85FF") },
                { ThemeGroup.Timeline_Scrollbar_Selected, LSColors.HexToColorAlpha("AA3A85FF") },
                { ThemeGroup.Timeline_Scrollbar_Pressed, LSColors.HexToColorAlpha("AA3A85FF") },
                { ThemeGroup.Timeline_Scrollbar_Disabled, LSColors.HexToColorAlpha("676767FF") },
                { ThemeGroup.Timeline_Scrollbar_Base, LSColors.HexToColorAlpha("23233FFF") },
                { ThemeGroup.Timeline_Time_Scrollbar, LSColors.HexToColorAlpha("3F2337FF") },

                { ThemeGroup.Title_Bar_Text, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Title_Bar_Button, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Title_Bar_Button_Normal, LSColors.HexToColorAlpha("0B1623FF") }, // 0B1623FF
                { ThemeGroup.Title_Bar_Button_Highlighted, LSColors.HexToColorAlpha("101D2DFF") }, // 101D2DFF
                { ThemeGroup.Title_Bar_Button_Selected, LSColors.HexToColorAlpha("101D2DFF") },
                { ThemeGroup.Title_Bar_Button_Pressed, LSColors.HexToColorAlpha("101D2DFF") },
                { ThemeGroup.Title_Bar_Dropdown, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Title_Bar_Dropdown_Normal, LSColors.HexToColorAlpha("101D2DFF") },
                { ThemeGroup.Title_Bar_Dropdown_Highlighted, LSColors.HexToColorAlpha("0B1623FF") },
                { ThemeGroup.Title_Bar_Dropdown_Selected, LSColors.HexToColorAlpha("0B1623FF") },
                { ThemeGroup.Title_Bar_Dropdown_Pressed, LSColors.HexToColorAlpha("0B1623FF") },
                { ThemeGroup.Title_Bar_Dropdown_Disabled, LSColors.HexToColorAlpha("0B1623FF") },

                { ThemeGroup.Warning_Confirm, LSColors.HexToColorAlpha("56CDEAFF") },
                { ThemeGroup.Warning_Cancel, LSColors.HexToColorAlpha("E01671FF") },

                { ThemeGroup.Notification_Background, LSColors.HexToColorAlpha("212121FF") },
                { ThemeGroup.Notification_Info, LSColors.HexToColorAlpha("404040FF") },
                { ThemeGroup.Notification_Success, LSColors.HexToColorAlpha("4DB6ACFF") },
                { ThemeGroup.Notification_Error, LSColors.HexToColorAlpha("E57373FF") },
                { ThemeGroup.Notification_Warning, LSColors.HexToColorAlpha("FFAF38FF") },

                { ThemeGroup.Copy, LSColors.HexToColorAlpha("56CDEAFF") },
                { ThemeGroup.Copy_Text, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Paste, LSColors.HexToColorAlpha("E01671FF") },
                { ThemeGroup.Paste_Text, LSColors.HexToColorAlpha("FFFFFFFF") },

                { ThemeGroup.Tab_Color_1, LSColors.HexToColorAlpha("FF2071FF") },
                { ThemeGroup.Tab_Color_1_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_1_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_1_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_1_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_1_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_2, LSColors.HexToColorAlpha("D5F5FCFF") },
                { ThemeGroup.Tab_Color_2_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_2_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_2_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_2_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_2_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_3, LSColors.HexToColorAlpha("000000FF") },
                { ThemeGroup.Tab_Color_3_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_3_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_3_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_3_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_3_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_4, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_4_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_4_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_4_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_4_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_4_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_5, LSColors.HexToColorAlpha("00F2FCFF") },
                { ThemeGroup.Tab_Color_5_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_5_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_5_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_5_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_5_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_6, LSColors.HexToColorAlpha("85CAFEFF") },
                { ThemeGroup.Tab_Color_6_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_6_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_6_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_6_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_6_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_7, LSColors.HexToColorAlpha("7DF2CAFF") },
                { ThemeGroup.Tab_Color_7_Normal, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_7_Highlighted, new Color(2f, 2f, 2f, 1f) },
                { ThemeGroup.Tab_Color_7_Selected, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_7_Pressed, LSColors.HexToColorAlpha("FFFFFFFF") },
                { ThemeGroup.Tab_Color_7_Disabled, LSColors.HexToColorAlpha("FFFFFFFF") },

                { ThemeGroup.Event_Color_1, LSColors.HexToColorAlpha("673AB77F") }, // 1
                { ThemeGroup.Event_Color_2, LSColors.HexToColorAlpha("3E51B47F") }, // 2
                { ThemeGroup.Event_Color_3, LSColors.HexToColorAlpha("2196F37F") }, // 3
                { ThemeGroup.Event_Color_4, LSColors.HexToColorAlpha("03A9F47F") }, // 4
                { ThemeGroup.Event_Color_5, LSColors.HexToColorAlpha("00BCD47F") }, // 5
                { ThemeGroup.Event_Color_6, LSColors.HexToColorAlpha("0096887F") }, // 6
                { ThemeGroup.Event_Color_7, LSColors.HexToColorAlpha("4BAF507F") }, // 7
                { ThemeGroup.Event_Color_8, LSColors.HexToColorAlpha("7CB3417F") }, // 8
                { ThemeGroup.Event_Color_9, LSColors.HexToColorAlpha("AFB42B7F") }, // 9
                { ThemeGroup.Event_Color_10, LSColors.HexToColorAlpha("FFC1077F") }, // 10
                { ThemeGroup.Event_Color_11, LSColors.HexToColorAlpha("FF98007F") }, // 11
                { ThemeGroup.Event_Color_12, LSColors.HexToColorAlpha("B960007F") }, // 12
                { ThemeGroup.Event_Color_13, LSColors.HexToColorAlpha("B124117F") }, // 13
                { ThemeGroup.Event_Color_14, LSColors.HexToColorAlpha("B124247F") }, // 14
                { ThemeGroup.Event_Color_15, LSColors.HexToColorAlpha("64B4F67F") }, // 15

                { ThemeGroup.Event_Color_1_Keyframe, LSColors.HexToColorAlpha("673AB7FF") }, // 1
                { ThemeGroup.Event_Color_2_Keyframe, LSColors.HexToColorAlpha("3E51B4FF") }, // 2
                { ThemeGroup.Event_Color_3_Keyframe, LSColors.HexToColorAlpha("2196F3FF") }, // 3
                { ThemeGroup.Event_Color_4_Keyframe, LSColors.HexToColorAlpha("03A9F4FF") }, // 4
                { ThemeGroup.Event_Color_5_Keyframe, LSColors.HexToColorAlpha("00BCD4FF") }, // 5
                { ThemeGroup.Event_Color_6_Keyframe, LSColors.HexToColorAlpha("009688FF") }, // 6
                { ThemeGroup.Event_Color_7_Keyframe, LSColors.HexToColorAlpha("4BAF50FF") }, // 7
                { ThemeGroup.Event_Color_8_Keyframe, LSColors.HexToColorAlpha("7CB341FF") }, // 8
                { ThemeGroup.Event_Color_9_Keyframe, LSColors.HexToColorAlpha("AFB42BFF") }, // 9
                { ThemeGroup.Event_Color_10_Keyframe, LSColors.HexToColorAlpha("FFC107FF") }, // 10
                { ThemeGroup.Event_Color_11_Keyframe, LSColors.HexToColorAlpha("FF9800FF") }, // 11
                { ThemeGroup.Event_Color_12_Keyframe, LSColors.HexToColorAlpha("B96000FF") }, // 12
                { ThemeGroup.Event_Color_13_Keyframe, LSColors.HexToColorAlpha("B12411FF") }, // 13
                { ThemeGroup.Event_Color_14_Keyframe, LSColors.HexToColorAlpha("B12424FF") }, // 14
                { ThemeGroup.Event_Color_15_Keyframe, LSColors.HexToColorAlpha("64B4F6FF") }, // 15

                { ThemeGroup.Event_Color_1_Editor, LSColors.HexToColorAlpha("564B6AFF") }, // 1
                { ThemeGroup.Event_Color_2_Editor, LSColors.HexToColorAlpha("41445EFF") }, // 2
                { ThemeGroup.Event_Color_3_Editor, LSColors.HexToColorAlpha("44627AFF") }, // 3
                { ThemeGroup.Event_Color_4_Editor, LSColors.HexToColorAlpha("315B6EFF") }, // 4
                { ThemeGroup.Event_Color_5_Editor, LSColors.HexToColorAlpha("3E6D73FF") }, // 5
                { ThemeGroup.Event_Color_6_Editor, LSColors.HexToColorAlpha("305653FF") }, // 6
                { ThemeGroup.Event_Color_7_Editor, LSColors.HexToColorAlpha("506951FF") }, // 7
                { ThemeGroup.Event_Color_8_Editor, LSColors.HexToColorAlpha("515E41FF") }, // 8
                { ThemeGroup.Event_Color_9_Editor, LSColors.HexToColorAlpha("676945FF") }, // 9
                { ThemeGroup.Event_Color_10_Editor, LSColors.HexToColorAlpha("726335FF") }, // 10
                { ThemeGroup.Event_Color_11_Editor, LSColors.HexToColorAlpha("FF9800FF") }, // 11
                { ThemeGroup.Event_Color_12_Editor, LSColors.HexToColorAlpha("FF5800FF") }, // 12
                { ThemeGroup.Event_Color_13_Editor, LSColors.HexToColorAlpha("FF2509FF") }, // 13
                { ThemeGroup.Event_Color_14_Editor, LSColors.HexToColorAlpha("FF0F0FFF") }, // 14

                { ThemeGroup.Object_Keyframe_Color_1, LSColors.HexToColorAlpha("00F2FCFF") }, // 1
                { ThemeGroup.Object_Keyframe_Color_2, LSColors.HexToColorAlpha("FEFE00FF") }, // 2
                { ThemeGroup.Object_Keyframe_Color_3, LSColors.HexToColorAlpha("64FE00FF") }, // 3
                { ThemeGroup.Object_Keyframe_Color_4, LSColors.HexToColorAlpha("FE9800FF") }, // 4
            }),
        };

        public static Dictionary<string, EditorTheme> EditorThemesDictionary => EditorThemes.ToDictionary(x => x.name, x => x);

        public static void AddDropdown(Dropdown dropdown, string name = "")
        {
            AddGraphic(dropdown.image, ThemeGroup.Dropdown_1, true);
            AddGraphic(dropdown.captionText, ThemeGroup.Dropdown_1_Overlay);
            AddGraphic(dropdown.transform.Find("Arrow").GetComponent<Image>(), ThemeGroup.Dropdown_1_Overlay);
            if (dropdown.captionImage)
                AddGraphic(dropdown.captionImage, ThemeGroup.Dropdown_1_Overlay);

            var template = dropdown.template.gameObject;
            AddGraphic(template.GetComponent<Image>(), ThemeGroup.Dropdown_1, true, roundedSide: SpriteManager.RoundedSide.Bottom);

            var templateItem = template.transform.Find("Viewport/Content/Item");
            AddGraphic(templateItem.Find("Item Background").GetComponent<Image>(), ThemeGroup.Dropdown_1_Item, true);
            AddGraphic(templateItem.Find("Item Checkmark").GetComponent<Image>(), ThemeGroup.Dropdown_1_Overlay);
            AddGraphic(dropdown.itemText, ThemeGroup.Dropdown_1_Overlay);
            if (dropdown.itemImage)
                AddGraphic(dropdown.itemImage, ThemeGroup.Dropdown_1_Overlay);
        }

        public static void ApplyDropdown(Dropdown dropdown)
        {
            ApplyGraphic(dropdown.image, ThemeGroup.Dropdown_1, true);
            ApplyGraphic(dropdown.captionText, ThemeGroup.Dropdown_1_Overlay);
            ApplyGraphic(dropdown.transform.Find("Arrow").GetComponent<Image>(), ThemeGroup.Dropdown_1_Overlay);
            if (dropdown.captionImage)
                ApplyGraphic(dropdown.captionImage, ThemeGroup.Dropdown_1_Overlay);

            var template = dropdown.template.gameObject;
            ApplyGraphic(template.GetComponent<Image>(), ThemeGroup.Dropdown_1, true, roundedSide: SpriteManager.RoundedSide.Bottom);

            var templateItem = template.transform.Find("Viewport/Content/Item");
            ApplyGraphic(templateItem.Find("Item Background").GetComponent<Image>(), ThemeGroup.Dropdown_1_Item, true);
            ApplyGraphic(templateItem.Find("Item Checkmark").GetComponent<Image>(), ThemeGroup.Dropdown_1_Overlay);
            ApplyGraphic(dropdown.itemText, ThemeGroup.Dropdown_1_Overlay);
            if (dropdown.itemImage)
                ApplyGraphic(dropdown.itemImage, ThemeGroup.Dropdown_1_Overlay);
        }

        public static void AddInputField(InputField inputField, string name, string group, int rounded = 1, SpriteManager.RoundedSide roundedSide = SpriteManager.RoundedSide.W)
        {
            inputField.image.fillCenter = true;
            AddElement(new Element(name, group, inputField.gameObject, new List<Component>
            {
                inputField.image,
            }, true, rounded, roundedSide));

            AddElement(new Element($"{name} Text", $"{group} Text", inputField.textComponent.gameObject, new List<Component>
            {
                inputField.textComponent,
            }));
        }
        
        public static void AddInputField(InputField inputField, ThemeGroup group = ThemeGroup.Input_Field, int rounded = 1, SpriteManager.RoundedSide roundedSide = SpriteManager.RoundedSide.W)
        {
            inputField.image.fillCenter = true;
            AddElement(new Element(group, inputField.gameObject, new List<Component>
            {
                inputField.image,
            }, true, rounded, roundedSide));

            AddElement(new Element(EditorTheme.GetGroup($"{EditorTheme.GetString(group)} Text"), inputField.textComponent.gameObject, new List<Component>
            {
                inputField.textComponent,
            }));
        }
        
        public static void AddInputField(TMP_InputField inputField, string name, string group, int rounded = 1, SpriteManager.RoundedSide roundedSide = SpriteManager.RoundedSide.W)
        {
            inputField.image.fillCenter = true;
            AddElement(new Element(name, group, inputField.gameObject, new List<Component>
            {
                inputField.image,
            }, true, rounded, roundedSide));

            AddElement(new Element($"{name} Text", $"{group} Text", inputField.textComponent.gameObject, new List<Component>
            {
                inputField.textComponent,
            }));
        }
        
        public static void AddInputField(TMP_InputField inputField, ThemeGroup group = ThemeGroup.Input_Field, int rounded = 1, SpriteManager.RoundedSide roundedSide = SpriteManager.RoundedSide.W)
        {
            inputField.image.fillCenter = true;
            AddElement(new Element(group, inputField.gameObject, new List<Component>
            {
                inputField.image,
            }, true, rounded, roundedSide));

            AddElement(new Element(EditorTheme.GetGroup($"{EditorTheme.GetString(group)} Text"), inputField.textComponent.gameObject, new List<Component>
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
        
        public static void ApplyInputField(InputField inputField, ThemeGroup group = ThemeGroup.Input_Field)
        {
            inputField.image.fillCenter = true;
            ApplyElement(new Element(group, inputField.gameObject, new List<Component>
            {
                inputField.image,
            }, true, 1, SpriteManager.RoundedSide.W));

            ApplyElement(new Element(EditorTheme.GetGroup($"{EditorTheme.GetString(group)} Text"), inputField.textComponent.gameObject, new List<Component>
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

                var input = selfInput ? inputField.transform : gameObject.transform.Find("input") ?? gameObject.transform.Find("Input") ?? gameObject.transform.Find("text-field");

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

                if (!inputField)
                    continue;

                var input = selfInput ? inputField.transform : child.Find("input") ?? child.Find("Input") ?? child.Find("text-field");

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
            toggle.image.fillCenter = true;
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
                AddElement(new Element($"{name} Text", "Toggle 1 Check", toggle.transform.Find("text").gameObject, new List<Component>
                {
                    toggle.transform.Find("text").GetComponent<Text>(),
                }));
        }
        
        public static void AddToggle(Toggle toggle, ThemeGroup checkGroup = ThemeGroup.Null, Graphic graphic = null)
        {
            toggle.image.fillCenter = true;
            AddElement(new Element(ThemeGroup.Toggle_1, toggle.gameObject, new List<Component>
            {
                toggle.image,
            }, true, 1, SpriteManager.RoundedSide.W));

            var checkMarkGroup = checkGroup != ThemeGroup.Null ? checkGroup : ThemeGroup.Toggle_1_Check;
            AddElement(new Element(checkMarkGroup, toggle.graphic.gameObject, new List<Component>
            {
                toggle.graphic,
            }));

            if (graphic)
            {
                AddElement(new Element(checkMarkGroup, graphic.gameObject, new List<Component>
                {
                    graphic,
                }));
                return;
            }

            if (toggle.transform.Find("Text"))
                AddElement(new Element(checkMarkGroup, toggle.transform.Find("Text").gameObject, new List<Component>
                {
                    toggle.transform.Find("Text").GetComponent<Text>(),
                }));

            if (toggle.transform.Find("text"))
                AddElement(new Element(checkMarkGroup, toggle.transform.Find("text").gameObject, new List<Component>
                {
                    toggle.transform.Find("text").GetComponent<Text>(),
                }));
        }

        public static void ApplyToggle(Toggle toggle, string name, Text text = null, string checkmark = "")
        {
            toggle.image.fillCenter = true;
            ApplyElement(new Element(name, "Toggle 1", toggle.gameObject, new List<Component>
            {
                toggle.image,
            }, true, 1, SpriteManager.RoundedSide.W));

            ApplyElement(new Element($"{name} Checkmark", string.IsNullOrEmpty(checkmark) ? "Toggle 1 Check" : checkmark, toggle.graphic.gameObject, new List<Component>
            {
                toggle.graphic,
            }));

            if (text)
            {
                ApplyElement(new Element($"{name} Text", "Toggle 1 Check", text.gameObject, new List<Component>
                {
                    text,
                }));
                return;
            }

            if (toggle.transform.Find("Text"))
                ApplyElement(new Element($"{name} Text", "Toggle 1 Check", toggle.transform.Find("Text").gameObject, new List<Component>
                {
                    toggle.transform.Find("Text").GetComponent<Text>(),
                }));

            if (toggle.transform.Find("text"))
                ApplyElement(new Element($"{name} Text", "Toggle 1 Check", toggle.transform.Find("text").gameObject, new List<Component>
                {
                    toggle.transform.Find("text").GetComponent<Text>(),
                }));
        }

        public static void ApplyToggle(Toggle toggle, ThemeGroup checkGroup = ThemeGroup.Null, Text text = null)
        {
            toggle.image.fillCenter = true;
            ApplyElement(new Element(ThemeGroup.Toggle_1, toggle.gameObject, new List<Component>
            {
                toggle.image,
            }, true, 1, SpriteManager.RoundedSide.W));

            var checkMarkGroup = checkGroup != ThemeGroup.Null ? checkGroup : ThemeGroup.Toggle_1_Check;
            ApplyElement(new Element(checkMarkGroup, toggle.graphic.gameObject, new List<Component>
            {
                toggle.graphic,
            }));

            if (text)
            {
                ApplyElement(new Element(checkMarkGroup, text.gameObject, new List<Component>
                {
                    text,
                }));
                return;
            }

            if (toggle.transform.Find("Text"))
                ApplyElement(new Element(checkMarkGroup, toggle.transform.Find("Text").gameObject, new List<Component>
                {
                    toggle.transform.Find("Text").GetComponent<Text>(),
                }));

            if (toggle.transform.Find("text"))
                ApplyElement(new Element(checkMarkGroup, toggle.transform.Find("text").gameObject, new List<Component>
                {
                    toggle.transform.Find("text").GetComponent<Text>(),
                }));
        }

        public static void AddLightText(Text text)
        {
            AddElement(new Element(ThemeGroup.Light_Text, text.gameObject, new List<Component>
            {
                text,
            }));
        }
        
        public static void ApplyLightText(Text text)
        {
            ApplyElement(new Element(ThemeGroup.Light_Text, text.gameObject, new List<Component>
            {
                text,
            }));
        }

        public static void AddLightText(TextMeshProUGUI text)
        {
            AddElement(new Element(ThemeGroup.Light_Text, text.gameObject, new List<Component>
            {
                text,
            }));
        }

        public static void ApplyLightText(TextMeshProUGUI text)
        {
            ApplyElement(new Element(ThemeGroup.Light_Text, text.gameObject, new List<Component>
            {
                text,
            }));
        }

        public static void AddSelectable(Selectable selectable, ThemeGroup group, bool canSetRounded = true, int rounded = 1, SpriteManager.RoundedSide roundedSide = SpriteManager.RoundedSide.W)
        {
            AddElement(new Element(group, selectable.gameObject, new List<Component>
            {
                selectable.image,
                selectable,
            }, canSetRounded, rounded, roundedSide, true));
        }

        public static void ApplySelectable(Selectable selectable, ThemeGroup group, bool canSetRounded = true, int rounded = 1, SpriteManager.RoundedSide roundedSide = SpriteManager.RoundedSide.W)
        {
            ApplyElement(new Element(group, selectable.gameObject, new List<Component>
            {
                selectable.image,
                selectable,
            }, canSetRounded, rounded, roundedSide, true));
        }

        public static void AddGraphic(Graphic graphic, ThemeGroup group, bool canSetRounded = false, int rounded = 1, SpriteManager.RoundedSide roundedSide = SpriteManager.RoundedSide.W)
        {
            AddElement(new Element(group, graphic.gameObject, new List<Component>
            {
                graphic,
            }, canSetRounded, rounded, roundedSide));
        }
        
        public static void ApplyGraphic(Graphic graphic, ThemeGroup group, bool canSetRounded = false, int rounded = 1, SpriteManager.RoundedSide roundedSide = SpriteManager.RoundedSide.W)
        {
            ApplyElement(new Element(group, graphic.gameObject, new List<Component>
            {
                graphic,
            }, canSetRounded, rounded, roundedSide));
        }

        public static void AddScrollbar(Scrollbar scrollbar, Image backgroundImage = null, ThemeGroup scrollbarGroup = ThemeGroup.Background_1, ThemeGroup handleGroup = ThemeGroup.Scrollbar_1_Handle,
            bool canSetScrollbarRounded = true, bool canSetHandleRounded = true, int scrollbarRounded = 1, int handleRounded = 1,
            SpriteManager.RoundedSide scrollbarRoundedSide = SpriteManager.RoundedSide.W, SpriteManager.RoundedSide handleRoundedSide = SpriteManager.RoundedSide.W)
        {
            AddGraphic(backgroundImage ?? scrollbar.GetComponent<Image>(), scrollbarGroup, canSetScrollbarRounded, scrollbarRounded, scrollbarRoundedSide);

            AddElement(new Element(handleGroup, scrollbar.image.gameObject, new List<Component>
            {
                scrollbar.image,
                scrollbar
            }, canSetHandleRounded, handleRounded, handleRoundedSide, true));
        }
        
        public static void ApplyScrollbar(Scrollbar scrollbar, Image backgroundImage = null, ThemeGroup scrollbarGroup = ThemeGroup.Background_1, ThemeGroup handleGroup = ThemeGroup.Scrollbar_1_Handle,
            bool canSetScrollbarRounded = true, bool canSetHandleRounded = true, int scrollbarRounded = 1, int handleRounded = 1,
            SpriteManager.RoundedSide scrollbarRoundedSide = SpriteManager.RoundedSide.W, SpriteManager.RoundedSide handleRoundedSide = SpriteManager.RoundedSide.W)
        {
            ApplyGraphic(backgroundImage ?? scrollbar.GetComponent<Image>(), scrollbarGroup, canSetScrollbarRounded, scrollbarRounded, scrollbarRoundedSide);

            ApplyElement(new Element(handleGroup, scrollbar.image.gameObject, new List<Component>
            {
                scrollbar.image,
                scrollbar
            }, canSetHandleRounded, handleRounded, handleRoundedSide, true));
        }
        
        public static void AddSlider(Slider slider, Image backgroundImage = null, ThemeGroup sliderGroup = ThemeGroup.Slider_2, ThemeGroup handleGroup = ThemeGroup.Slider_2_Handle,
            bool canSetSliderRounded = true, bool canSetHandleRounded = true, int sliderRounded = 1, int handleRounded = 1,
            SpriteManager.RoundedSide sliderRoundedSide = SpriteManager.RoundedSide.W, SpriteManager.RoundedSide handleRoundedSide = SpriteManager.RoundedSide.W, bool selectable = false)
        {
            AddGraphic(backgroundImage ?? slider.GetComponent<Image>(), sliderGroup, canSetSliderRounded, sliderRounded, sliderRoundedSide);

            AddElement(new Element(handleGroup, slider.image.gameObject, new List<Component>
            {
                slider.image,
                slider
            }, canSetHandleRounded, handleRounded, handleRoundedSide, selectable));
        }

        public static void ApplySlider(Slider slider, Image backgroundImage = null, ThemeGroup sliderGroup = ThemeGroup.Slider_2, ThemeGroup handleGroup = ThemeGroup.Slider_2_Handle,
            bool canSetSliderRounded = true, bool canSetHandleRounded = true, int sliderRounded = 1, int handleRounded = 1,
            SpriteManager.RoundedSide sliderRoundedSide = SpriteManager.RoundedSide.W, SpriteManager.RoundedSide handleRoundedSide = SpriteManager.RoundedSide.W, bool selectable = false)
        {
            ApplyGraphic(backgroundImage ?? slider.GetComponent<Image>(), sliderGroup, canSetSliderRounded, sliderRounded, sliderRoundedSide);

            ApplyElement(new Element(handleGroup, slider.image.gameObject, new List<Component>
            {
                slider.image,
                slider
            }, canSetHandleRounded, handleRounded, handleRoundedSide, selectable));
        }

        public class EditorTheme
        {
            public EditorTheme(string name, Dictionary<ThemeGroup, Color> colorGroups)
            {
                this.name = name;
                ColorGroups = colorGroups;
            }

            public string name;
            public Dictionary<ThemeGroup, Color> ColorGroups { get; set; }

            public static ThemeGroup GetGroup(string group)
            {
                switch (group)
                {
                    case "Background": return ThemeGroup.Background_1;
                    case "Background 2": return ThemeGroup.Background_2;
                    case "Background 3": return ThemeGroup.Background_3;
                    case "Preview Cover": return ThemeGroup.Preview_Cover;

                    case "Scrollbar Handle": return ThemeGroup.Scrollbar_1_Handle;
                    case "Scrollbar Handle Normal": return ThemeGroup.Scrollbar_1_Handle_Normal;
                    case "Scrollbar Handle Highlight": return ThemeGroup.Scrollbar_1_Handle_Highlighted;
                    case "Scrollbar Handle Selected": return ThemeGroup.Scrollbar_1_Handle_Selected;
                    case "Scrollbar Handle Pressed": return ThemeGroup.Scrollbar_1_Handle_Pressed;
                    case "Scrollbar Handle Disabled": return ThemeGroup.Scrollbar_1_Handle_Disabled;

                    case "Scrollbar 2": return ThemeGroup.Scrollbar_2;
                    case "Scrollbar Handle 2": return ThemeGroup.Scrollbar_2_Handle;
                    case "Scrollbar Handle 2 Normal": return ThemeGroup.Scrollbar_2_Handle_Normal;
                    case "Scrollbar Handle 2 Highlight": return ThemeGroup.Scrollbar_2_Handle_Hightlighted;
                    case "Scrollbar Handle 2 Selected": return ThemeGroup.Scrollbar_2_Handle_Selected;
                    case "Scrollbar Handle 2 Pressed": return ThemeGroup.Scrollbar_2_Handle_Pressed;
                    case "Scrollbar Handle 2 Disabled": return ThemeGroup.Scrollbar_2_Handle_Disabled;

                    case "Close": return ThemeGroup.Close;
                    case "Close Normal": return ThemeGroup.Close_Normal;
                    case "Close Highlight": return ThemeGroup.Close_Highlighted;
                    case "Close Selected": return ThemeGroup.Close_Selected;
                    case "Close Pressed": return ThemeGroup.Close_Pressed;
                    case "Close Disabled": return ThemeGroup.Close_Disabled;
                    case "Close X": return ThemeGroup.Close_X;

                    case "Picker": return ThemeGroup.Picker;
                    case "Picker Normal": return ThemeGroup.Picker_Normal;
                    case "Picker Highlight": return ThemeGroup.Picker_Highlighted;
                    case "Picker Selected": return ThemeGroup.Picker_Selected;
                    case "Picker Pressed": return ThemeGroup.Picker_Pressed;
                    case "Picker Disabled": return ThemeGroup.Picker_Disabled;
                    case "Picker Icon": return ThemeGroup.Picker_Icon;

                    case "Light Text": return ThemeGroup.Light_Text;
                    case "Dark Text": return ThemeGroup.Dark_Text;

                    case "Function 1": return ThemeGroup.Function_1; // 0F7BF8FF
                    case "Function 1 Text": return ThemeGroup.Function_1_Text;
                    case "Function 2": return ThemeGroup.Function_2;
                    case "Function 2 Normal": return ThemeGroup.Function_2_Normal;
                    case "Function 2 Highlight": return ThemeGroup.Function_2_Highlighted;
                    case "Function 2 Selected": return ThemeGroup.Function_2_Selected;
                    case "Function 2 Pressed": return ThemeGroup.Function_2_Pressed;
                    case "Function 2 Disabled": return ThemeGroup.Function_2_Disabled;
                    case "Function 2 Text": return ThemeGroup.Function_2_Text;
                    case "Function 3": return ThemeGroup.Function_3;
                    case "Function 3 Text": return ThemeGroup.Function_3_Text;

                    case "List Button 1": return ThemeGroup.List_Button_1;
                    case "List Button 1 Normal": return ThemeGroup.List_Button_1_Normal;
                    case "List Button 1 Highlight": return ThemeGroup.List_Button_1_Highlighted;
                    case "List Button 1 Selected": return ThemeGroup.List_Button_1_Selected;
                    case "List Button 1 Pressed": return ThemeGroup.List_Button_1_Pressed;
                    case "List Button 1 Disabled": return ThemeGroup.List_Button_1_Disabled;

                    case "List Button 2": return ThemeGroup.List_Button_2;
                    case "List Button 2 Normal": return ThemeGroup.List_Button_2_Normal;
                    case "List Button 2 Highlight": return ThemeGroup.List_Button_2_Highlighted;
                    case "List Button 2 Selected": return ThemeGroup.List_Button_2_Selected;
                    case "List Button 2 Pressed": return ThemeGroup.List_Button_2_Pressed;
                    case "List Button 2 Disabled": return ThemeGroup.List_Button_2_Disabled;
                    case "List Button 2 Text": return ThemeGroup.List_Button_2_Text;

                    case "Back Button": return ThemeGroup.Back_Button;
                    case "Back Button Text": return ThemeGroup.Back_Button_Text;
                    case "Folder Button": return ThemeGroup.Folder_Button;
                    case "Folder Button Text": return ThemeGroup.Folder_Button_Text;

                    case "Search Field 1": return ThemeGroup.Search_Field_1;
                    case "Search Field 1 Text": return ThemeGroup.Search_Field_1_Text;
                    case "Search Field 2": return ThemeGroup.Search_Field_2;
                    case "Search Field 2 Text": return ThemeGroup.Search_Field_2_Text;

                    case "Add": return ThemeGroup.Add;
                    case "Add Text": return ThemeGroup.Add_Text;
                    case "Delete": return ThemeGroup.Delete;
                    case "Delete Text": return ThemeGroup.Delete_Text;
                    case "Delete Keyframe BG": return ThemeGroup.Delete_Keyframe_BG;
                    case "Delete Keyframe Button": return ThemeGroup.Delete_Keyframe_Button;
                    case "Delete Keyframe Button Normal": return ThemeGroup.Delete_Keyframe_Button_Normal;
                    case "Delete Keyframe Button Highlight": return ThemeGroup.Delete_Keyframe_Button_Highlighted;
                    case "Delete Keyframe Button Selected": return ThemeGroup.Delete_Keyframe_Button_Selected;
                    case "Delete Keyframe Button Pressed": return ThemeGroup.Delete_Keyframe_Button_Pressed;
                    case "Delete Keyframe Button Disabled": return ThemeGroup.Delete_Keyframe_Button_Disabled;

                    case "Prefab": return ThemeGroup.Prefab;
                    case "Prefab Text": return ThemeGroup.Prefab_Text;
                    case "Object": return ThemeGroup.Object;
                    case "Object Text": return ThemeGroup.Object_Text;
                    case "Marker": return ThemeGroup.Marker;
                    case "Marker Text": return ThemeGroup.Marker_Text;
                    case "Checkpoint": return ThemeGroup.Checkpoint;
                    case "Checkpoint Text": return ThemeGroup.Checkpoint_Text;
                    case "Background Object": return ThemeGroup.Background_Object;
                    case "Background Object Text": return ThemeGroup.Background_Object_Text;
                    case "Timeline Bar": return ThemeGroup.Timeline_Bar;
                    case "Event/Check": return ThemeGroup.Event_Check;
                    case "Event/Check Text": return ThemeGroup.Event_Check_Text;

                    case "Dropdown 1": return ThemeGroup.Dropdown_1;
                    case "Dropdown 1 Overlay": return ThemeGroup.Dropdown_1_Overlay;
                    case "Dropdown 1 Item": return ThemeGroup.Dropdown_1_Item;
                    case "Toggle 1": return ThemeGroup.Toggle_1;
                    case "Toggle 1 Check": return ThemeGroup.Toggle_1_Check;
                    case "Input Field": return ThemeGroup.Input_Field;
                    case "Input Field Text": return ThemeGroup.Input_Field_Text;
                    case "Slider 1": return ThemeGroup.Slider_1;
                    case "Slider 1 Normal": return ThemeGroup.Slider_1_Normal;
                    case "Slider 1 Highlight": return ThemeGroup.Slider_1_Highlighted;
                    case "Slider 1 Selected": return ThemeGroup.Slider_1_Selected;
                    case "Slider 1 Pressed": return ThemeGroup.Slider_1_Pressed;
                    case "Slider 1 Disabled": return ThemeGroup.Slider_1_Disabled;
                    case "Slider 1 Handle": return ThemeGroup.Slider_1_Handle;

                    case "Slider": return ThemeGroup.Slider_2;
                    case "Slider Handle": return ThemeGroup.Slider_2_Handle;

                    case "Documentation": return ThemeGroup.Documentation;

                    case "Timeline Background": return ThemeGroup.Timeline_Background;
                    case "Timeline Scrollbar": return ThemeGroup.Timeline_Scrollbar;
                    case "Timeline Scrollbar Normal": return ThemeGroup.Timeline_Scrollbar_Normal;
                    case "Timeline Scrollbar Highlight": return ThemeGroup.Timeline_Scrollbar_Highlighted;
                    case "Timeline Scrollbar Selected": return ThemeGroup.Timeline_Scrollbar_Selected;
                    case "Timeline Scrollbar Pressed": return ThemeGroup.Timeline_Scrollbar_Pressed;
                    case "Timeline Scrollbar Disabled": return ThemeGroup.Timeline_Scrollbar_Disabled;
                    case "Timeline Scrollbar Base": return ThemeGroup.Timeline_Scrollbar_Base;
                    case "Timeline Time Scrollbar": return ThemeGroup.Timeline_Time_Scrollbar;

                    case "Title Bar Text": return ThemeGroup.Title_Bar_Text;
                    case "Title Bar Button": return ThemeGroup.Title_Bar_Button;
                    case "Title Bar Button Normal": return ThemeGroup.Title_Bar_Button_Normal;
                    case "Title Bar Button Highlight": return ThemeGroup.Title_Bar_Button_Highlighted;
                    case "Title Bar Button Selected": return ThemeGroup.Title_Bar_Button_Selected;
                    case "Title Bar Button Pressed": return ThemeGroup.Title_Bar_Button_Pressed;
                    case "Title Bar Dropdown": return ThemeGroup.Title_Bar_Dropdown;
                    case "Title Bar Dropdown Normal": return ThemeGroup.Title_Bar_Dropdown_Normal;
                    case "Title Bar Dropdown Highlight": return ThemeGroup.Title_Bar_Dropdown_Highlighted;
                    case "Title Bar Dropdown Selected": return ThemeGroup.Title_Bar_Dropdown_Selected;
                    case "Title Bar Dropdown Pressed": return ThemeGroup.Title_Bar_Dropdown_Pressed;
                    case "Title Bar Dropdown Disabled": return ThemeGroup.Title_Bar_Dropdown_Disabled;

                    case "Warning Confirm": return ThemeGroup.Warning_Confirm;
                    case "Warning Cancel": return ThemeGroup.Warning_Cancel;

                    case "Notification Background": return ThemeGroup.Notification_Background;
                    case "Notification Info": return ThemeGroup.Notification_Info;
                    case "Notification Success": return ThemeGroup.Notification_Success;
                    case "Notification Error": return ThemeGroup.Notification_Error;
                    case "Notification Warning": return ThemeGroup.Notification_Warning;

                    case "Copy": return ThemeGroup.Copy;
                    case "Copy Text": return ThemeGroup.Copy_Text;
                    case "Paste": return ThemeGroup.Paste;
                    case "Paste Text": return ThemeGroup.Paste_Text;

                    case "Tab Color 1": return ThemeGroup.Tab_Color_1;
                    case "Tab Color 1 Normal": return ThemeGroup.Tab_Color_1_Normal;
                    case "Tab Color 1 Highlight": return ThemeGroup.Tab_Color_1_Highlighted;
                    case "Tab Color 1 Selected": return ThemeGroup.Tab_Color_1_Selected;
                    case "Tab Color 1 Pressed": return ThemeGroup.Tab_Color_1_Pressed;
                    case "Tab Color 1 Disabled": return ThemeGroup.Tab_Color_1_Disabled;
                    case "Tab Color 2": return ThemeGroup.Tab_Color_2;
                    case "Tab Color 2 Normal": return ThemeGroup.Tab_Color_2_Normal;
                    case "Tab Color 2 Highlight": return ThemeGroup.Tab_Color_2_Highlighted;
                    case "Tab Color 2 Selected": return ThemeGroup.Tab_Color_2_Selected;
                    case "Tab Color 2 Pressed": return ThemeGroup.Tab_Color_2_Pressed;
                    case "Tab Color 2 Disabled": return ThemeGroup.Tab_Color_2_Disabled;
                    case "Tab Color 3": return ThemeGroup.Tab_Color_3;
                    case "Tab Color 3 Normal": return ThemeGroup.Tab_Color_3_Normal;
                    case "Tab Color 3 Highlight": return ThemeGroup.Tab_Color_3_Highlighted;
                    case "Tab Color 3 Selected": return ThemeGroup.Tab_Color_3_Selected;
                    case "Tab Color 3 Pressed": return ThemeGroup.Tab_Color_3_Pressed;
                    case "Tab Color 3 Disabled": return ThemeGroup.Tab_Color_3_Disabled;
                    case "Tab Color 4": return ThemeGroup.Tab_Color_4;
                    case "Tab Color 4 Normal": return ThemeGroup.Tab_Color_4_Normal;
                    case "Tab Color 4 Highlight": return ThemeGroup.Tab_Color_4_Highlighted;
                    case "Tab Color 4 Selected": return ThemeGroup.Tab_Color_4_Selected;
                    case "Tab Color 4 Pressed": return ThemeGroup.Tab_Color_4_Pressed;
                    case "Tab Color 4 Disabled": return ThemeGroup.Tab_Color_4_Disabled;
                    case "Tab Color 5": return ThemeGroup.Tab_Color_5;
                    case "Tab Color 5 Normal": return ThemeGroup.Tab_Color_5_Normal;
                    case "Tab Color 5 Highlight": return ThemeGroup.Tab_Color_5_Highlighted;
                    case "Tab Color 5 Selected": return ThemeGroup.Tab_Color_5_Selected;
                    case "Tab Color 5 Pressed": return ThemeGroup.Tab_Color_5_Pressed;
                    case "Tab Color 5 Disabled": return ThemeGroup.Tab_Color_5_Disabled;
                    case "Tab Color 6": return ThemeGroup.Tab_Color_6;
                    case "Tab Color 6 Normal": return ThemeGroup.Tab_Color_6_Normal;
                    case "Tab Color 6 Highlight": return ThemeGroup.Tab_Color_6_Highlighted;
                    case "Tab Color 6 Selected": return ThemeGroup.Tab_Color_6_Selected;
                    case "Tab Color 6 Pressed": return ThemeGroup.Tab_Color_6_Pressed;
                    case "Tab Color 6 Disabled": return ThemeGroup.Tab_Color_6_Disabled;
                    case "Tab Color 7": return ThemeGroup.Tab_Color_7;
                    case "Tab Color 7 Normal": return ThemeGroup.Tab_Color_7_Normal;
                    case "Tab Color 7 Highlight": return ThemeGroup.Tab_Color_7_Highlighted;
                    case "Tab Color 7 Selected": return ThemeGroup.Tab_Color_7_Selected;
                    case "Tab Color 7 Pressed": return ThemeGroup.Tab_Color_7_Pressed;
                    case "Tab Color 7 Disabled": return ThemeGroup.Tab_Color_7_Disabled;

                    case "Event Color 1": return ThemeGroup.Event_Color_1; // 1
                    case "Event Color 2": return ThemeGroup.Event_Color_2; // 2
                    case "Event Color 3": return ThemeGroup.Event_Color_3; // 3
                    case "Event Color 4": return ThemeGroup.Event_Color_4; // 4
                    case "Event Color 5": return ThemeGroup.Event_Color_5; // 5
                    case "Event Color 6": return ThemeGroup.Event_Color_6; // 6
                    case "Event Color 7": return ThemeGroup.Event_Color_7; // 7
                    case "Event Color 8": return ThemeGroup.Event_Color_8; // 8
                    case "Event Color 9": return ThemeGroup.Event_Color_9; // 9
                    case "Event Color 10": return ThemeGroup.Event_Color_10; // 10
                    case "Event Color 11": return ThemeGroup.Event_Color_11; // 11
                    case "Event Color 12": return ThemeGroup.Event_Color_12; // 12
                    case "Event Color 13": return ThemeGroup.Event_Color_13; // 13
                    case "Event Color 14": return ThemeGroup.Event_Color_14; // 14
                    case "Event Color 15": return ThemeGroup.Event_Color_15; // 15

                    case "Event Color 1 Keyframe": return ThemeGroup.Event_Color_1_Keyframe; // 1
                    case "Event Color 2 Keyframe": return ThemeGroup.Event_Color_2_Keyframe; // 2
                    case "Event Color 3 Keyframe": return ThemeGroup.Event_Color_3_Keyframe; // 3
                    case "Event Color 4 Keyframe": return ThemeGroup.Event_Color_4_Keyframe; // 4
                    case "Event Color 5 Keyframe": return ThemeGroup.Event_Color_5_Keyframe; // 5
                    case "Event Color 6 Keyframe": return ThemeGroup.Event_Color_6_Keyframe; // 6
                    case "Event Color 7 Keyframe": return ThemeGroup.Event_Color_7_Keyframe; // 7
                    case "Event Color 8 Keyframe": return ThemeGroup.Event_Color_8_Keyframe; // 8
                    case "Event Color 9 Keyframe": return ThemeGroup.Event_Color_9_Keyframe; // 9
                    case "Event Color 10 Keyframe": return ThemeGroup.Event_Color_10_Keyframe; // 10
                    case "Event Color 11 Keyframe": return ThemeGroup.Event_Color_11_Keyframe; // 11
                    case "Event Color 12 Keyframe": return ThemeGroup.Event_Color_12_Keyframe; // 12
                    case "Event Color 13 Keyframe": return ThemeGroup.Event_Color_13_Keyframe; // 13
                    case "Event Color 14 Keyframe": return ThemeGroup.Event_Color_14_Keyframe; // 14
                    case "Event Color 15 Keyframe": return ThemeGroup.Event_Color_15_Keyframe; // 15

                    case "Event Color 1 Editor": return ThemeGroup.Event_Color_1_Editor; // 1
                    case "Event Color 2 Editor": return ThemeGroup.Event_Color_2_Editor; // 2
                    case "Event Color 3 Editor": return ThemeGroup.Event_Color_3_Editor; // 3
                    case "Event Color 4 Editor": return ThemeGroup.Event_Color_4_Editor; // 4
                    case "Event Color 5 Editor": return ThemeGroup.Event_Color_5_Editor; // 5
                    case "Event Color 6 Editor": return ThemeGroup.Event_Color_6_Editor; // 6
                    case "Event Color 7 Editor": return ThemeGroup.Event_Color_7_Editor; // 7
                    case "Event Color 8 Editor": return ThemeGroup.Event_Color_8_Editor; // 8
                    case "Event Color 9 Editor": return ThemeGroup.Event_Color_9_Editor; // 9
                    case "Event Color 10 Editor": return ThemeGroup.Event_Color_10_Editor; // 10
                    case "Event Color 11 Editor": return ThemeGroup.Event_Color_11_Editor; // 11
                    case "Event Color 12 Editor": return ThemeGroup.Event_Color_12_Editor; // 12
                    case "Event Color 13 Editor": return ThemeGroup.Event_Color_13_Editor; // 13
                    case "Event Color 14 Editor": return ThemeGroup.Event_Color_14_Editor; // 14

                    case "Object Keyframe Color 1": return ThemeGroup.Object_Keyframe_Color_1; // 1
                    case "Object Keyframe Color 2": return ThemeGroup.Object_Keyframe_Color_2; // 2
                    case "Object Keyframe Color 3": return ThemeGroup.Object_Keyframe_Color_3; // 3
                    case "Object Keyframe Color 4": return ThemeGroup.Object_Keyframe_Color_4; // 4
                }

                return ThemeGroup.Null;
            }

            public static string GetString(ThemeGroup group)
            {
                switch (group)
                {
                    case ThemeGroup.Background_1: return "Background";

                    case ThemeGroup.Scrollbar_1_Handle: return "Scrollbar Handle";
                    case ThemeGroup.Scrollbar_1_Handle_Normal: return "Scrollbar Handle Normal";
                    case ThemeGroup.Scrollbar_1_Handle_Highlighted: return "Scrollbar Handle Highlight";
                    case ThemeGroup.Scrollbar_1_Handle_Selected: return "Scrollbar Handle Selected";
                    case ThemeGroup.Scrollbar_1_Handle_Pressed: return "Scrollbar Handle Pressed";
                    case ThemeGroup.Scrollbar_1_Handle_Disabled: return "Scrollbar Handle Disabled";

                    case ThemeGroup.Scrollbar_2: return "Scrollbar 2";
                    case ThemeGroup.Scrollbar_2_Handle: return "Scrollbar Handle 2";
                    case ThemeGroup.Scrollbar_2_Handle_Normal: return "Scrollbar Handle 2 Normal";
                    case ThemeGroup.Scrollbar_2_Handle_Hightlighted: return "Scrollbar Handle 2 Highlight";
                    case ThemeGroup.Scrollbar_2_Handle_Selected: return "Scrollbar Handle 2 Selected";
                    case ThemeGroup.Scrollbar_2_Handle_Pressed: return "Scrollbar Handle 2 Pressed";
                    case ThemeGroup.Scrollbar_2_Handle_Disabled: return "Scrollbar Handle 2 Disabled";

                    case ThemeGroup.Close_Highlighted: return "Close Highlight";

                    case ThemeGroup.Function_2_Highlighted: return "Function 2 Highlight";

                    case ThemeGroup.List_Button_1_Highlighted: return "List Button 1 Highlight";

                    case ThemeGroup.List_Button_2_Highlighted: return "List Button 2 Highlight";

                    case ThemeGroup.Delete_Keyframe_Button_Highlighted: return "Delete Keyframe Button Highlight";

                    case ThemeGroup.Event_Check: return "Event/Check";
                    case ThemeGroup.Event_Check_Text: return "Event/Check Text";

                    case ThemeGroup.Slider_2: return "Slider";
                    case ThemeGroup.Slider_2_Handle: return "Slider Handle";

                    case ThemeGroup.Timeline_Scrollbar_Highlighted: return "Timeline Scrollbar Highlight";

                    case ThemeGroup.Title_Bar_Button_Highlighted: return "Title Bar Button Highlight";
                    case ThemeGroup.Title_Bar_Dropdown_Highlighted: return "Title Bar Dropdown Highlight";

                    case ThemeGroup.Tab_Color_1_Highlighted: return "Tab Color 1 Highlight";
                    case ThemeGroup.Tab_Color_2_Highlighted: return "Tab Color 2 Highlight";
                    case ThemeGroup.Tab_Color_3_Highlighted: return "Tab Color 3 Highlight";
                    case ThemeGroup.Tab_Color_4_Highlighted: return "Tab Color 4 Highlight";
                    case ThemeGroup.Tab_Color_5_Highlighted: return "Tab Color 5 Highlight";
                    case ThemeGroup.Tab_Color_6_Highlighted: return "Tab Color 6 Highlight";
                    case ThemeGroup.Tab_Color_7_Highlighted: return "Tab Color 7 Highlight";
                }

                return group.ToString().Replace("_", " ");
            }

            public Color GetColor(string group) => ColorGroups[GetGroup(group)];
            public bool ContainsGroup(string group) => GetGroup(group) != ThemeGroup.Null;
        }

        public class Element
        {
            public Element(ThemeGroup group, GameObject gameObject, List<Component> components, bool canSetRounded = false, int rounded = 0, SpriteManager.RoundedSide roundedSide = SpriteManager.RoundedSide.W, bool isSelectable = false)
            {
                themeGroup = group;
                GameObject = gameObject;
                Components = components;
                this.canSetRounded = canSetRounded;
                Rounded = rounded;
                RoundedSide = roundedSide;
                this.isSelectable = isSelectable;
            }

            public Element(string name, string group, GameObject gameObject, List<Component> components, bool canSetRounded = false, int rounded = 0, SpriteManager.RoundedSide roundedSide = SpriteManager.RoundedSide.W, bool isSelectable = false)
            {
                //this.name = name;
                this.group = group;
                GameObject = gameObject;
                Components = components;
                this.canSetRounded = canSetRounded;
                Rounded = rounded;
                RoundedSide = roundedSide;
                this.isSelectable = isSelectable;
            }

            /// <summary>
            /// Debug purposes only
            /// </summary>
            //public string name;
            public string group;
            public ThemeGroup themeGroup = ThemeGroup.Null;

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

                    if (themeGroup != ThemeGroup.Null)
                    {
                        if (theme.ColorGroups.ContainsKey(themeGroup))
                        {
                            if (!isSelectable)
                                SetColor(theme.ColorGroups[themeGroup]);
                            else
                            {
                                var colorBlock = new ColorBlock();

                                colorBlock.colorMultiplier = 1f;
                                colorBlock.fadeDuration = 0.1f;

                                var space = EditorTheme.GetString(themeGroup);
                                var normalGroup = EditorTheme.GetGroup(space + " Normal");
                                var highlightGroup = EditorTheme.GetGroup(space + " Highlight");
                                var selectedGroup = EditorTheme.GetGroup(space + " Selected");
                                var pressedGroup = EditorTheme.GetGroup(space + " Pressed");
                                var disabledGroup = EditorTheme.GetGroup(space + " Disabled");

                                if (theme.ColorGroups.ContainsKey(normalGroup))
                                    colorBlock.normalColor = theme.ColorGroups[normalGroup];

                                if (theme.ColorGroups.ContainsKey(highlightGroup))
                                    colorBlock.highlightedColor = theme.ColorGroups[highlightGroup];

                                if (theme.ColorGroups.ContainsKey(selectedGroup))
                                    colorBlock.selectedColor = theme.ColorGroups[selectedGroup];

                                if (theme.ColorGroups.ContainsKey(pressedGroup))
                                    colorBlock.pressedColor = theme.ColorGroups[pressedGroup];

                                if (theme.ColorGroups.ContainsKey(disabledGroup))
                                    colorBlock.disabledColor = theme.ColorGroups[disabledGroup];

                                SetColor(theme.ColorGroups[themeGroup], colorBlock);
                            }
                        }

                        return;
                    }

                    if (string.IsNullOrEmpty(group))
                        return;

                    var mainGroup = EditorTheme.GetGroup(group);
                    if (theme.ColorGroups.ContainsKey(mainGroup))
                    {
                        if (!isSelectable)
                            SetColor(theme.ColorGroups[mainGroup]);
                        else
                        {
                            var colorBlock = new ColorBlock();

                            colorBlock.colorMultiplier = 1f;
                            colorBlock.fadeDuration = 0.1f;

                            var normalGroup = EditorTheme.GetGroup(group + " Normal");
                            if (theme.ColorGroups.ContainsKey(normalGroup))
                                colorBlock.normalColor = theme.ColorGroups[normalGroup];

                            var highlightGroup = EditorTheme.GetGroup(group + " Highlight");
                            if (theme.ColorGroups.ContainsKey(highlightGroup))
                                colorBlock.highlightedColor = theme.ColorGroups[highlightGroup];

                            var selectedGroup = EditorTheme.GetGroup(group + " Highlight");
                            if (theme.ColorGroups.ContainsKey(selectedGroup))
                                colorBlock.selectedColor = theme.ColorGroups[selectedGroup];

                            var pressedGroup = EditorTheme.GetGroup(group + " Pressed");
                            if (theme.ColorGroups.ContainsKey(pressedGroup))
                                colorBlock.pressedColor = theme.ColorGroups[pressedGroup];

                            var disabledGroup = EditorTheme.GetGroup(group + " Disabled");
                            if (theme.ColorGroups.ContainsKey(disabledGroup))
                                colorBlock.disabledColor = theme.ColorGroups[disabledGroup];

                            SetColor(theme.ColorGroups[mainGroup], colorBlock);
                        }
                    }
                    else
                    {
                        Debug.LogError($"{EditorPlugin.className}Failed to assign theme color ({group} / {themeGroup}) to {GameObject.name}.");
                    }
                }
                catch
                {

                }
            }

            public void SetColor(Color color)
            {
                try
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
                catch
                {
                    foreach (var component in Components)
                    {
                        if (component is Text text)
                        {
                            var str = text.text;
                            text.text = "";
                            text.text = str;
                        }
                    }
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

            public override string ToString() => GameObject.name;
        }
    }
}
