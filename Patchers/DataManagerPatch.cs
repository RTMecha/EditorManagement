using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using HarmonyLib;
using BepInEx.Configuration;

using UnityEngine;

using LSFunctions;
using SimpleJSON;

using EditorManagement.Functions;
using EditorManagement.Functions.Tools;

namespace EditorManagement.Patchers
{
    [HarmonyPatch(typeof(DataManager))]
    public class DataManagerPatch : MonoBehaviour
    {
        public static List<ConfigEntryBase> prefabNames = new List<ConfigEntryBase>
        {
            ConfigEntries.PT0N,
            ConfigEntries.PT1N,
            ConfigEntries.PT2N,
            ConfigEntries.PT3N,
            ConfigEntries.PT4N,
            ConfigEntries.PT5N,
            ConfigEntries.PT6N,
            ConfigEntries.PT7N,
            ConfigEntries.PT8N,
            ConfigEntries.PT9N,
            ConfigEntries.PT10N,
            ConfigEntries.PT11N,
            ConfigEntries.PT12N,
            ConfigEntries.PT13N,
            ConfigEntries.PT14N,
            ConfigEntries.PT15N,
            ConfigEntries.PT16N,
            ConfigEntries.PT17N,
            ConfigEntries.PT18N,
            ConfigEntries.PT19N,
        };
        public static List<ConfigEntryBase> prefabColors = new List<ConfigEntryBase>
        {
            ConfigEntries.PT0C,
            ConfigEntries.PT1C,
            ConfigEntries.PT2C,
            ConfigEntries.PT3C,
            ConfigEntries.PT4C,
            ConfigEntries.PT5C,
            ConfigEntries.PT6C,
            ConfigEntries.PT7C,
            ConfigEntries.PT8C,
            ConfigEntries.PT9C,
            ConfigEntries.PT10C,
            ConfigEntries.PT11C,
            ConfigEntries.PT12C,
            ConfigEntries.PT13C,
            ConfigEntries.PT14C,
            ConfigEntries.PT15C,
            ConfigEntries.PT16C,
            ConfigEntries.PT17C,
            ConfigEntries.PT18C,
            ConfigEntries.PT19C,
        };

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        private static void DataLists(DataManager __instance)
        {
            if (__instance.difficulties.Count != 7)
            {
                __instance.difficulties = new List<DataManager.Difficulty>
                {
                    new DataManager.Difficulty("Easy", LSColors.GetThemeColor("easy")),
                    new DataManager.Difficulty("Normal", LSColors.GetThemeColor("normal")),
                    new DataManager.Difficulty("Hard", LSColors.GetThemeColor("hard")),
                    new DataManager.Difficulty("Expert", LSColors.GetThemeColor("expert")),
                    new DataManager.Difficulty("Expert+", LSColors.GetThemeColor("expert+")),
                    new DataManager.Difficulty("Master", new Color(0.25f, 0.01f, 0.01f)),
                    new DataManager.Difficulty("Animation", LSColors.GetThemeColor("none"))
                };
            }

            if (__instance.linkTypes[3].name != "YouTube")
            {
                __instance.linkTypes = new List<DataManager.LinkType>
                {
                    new DataManager.LinkType("Spotify", "https://open.spotify.com/artist/{0}"),
                    new DataManager.LinkType("SoundCloud", "https://soundcloud.com/{0}"),
                    new DataManager.LinkType("Bandcamp", "https://{0}.bandcamp.com"),
                    new DataManager.LinkType("Youtube", "https://www.youtube.com/user/{0}"),
                    new DataManager.LinkType("Newgrounds", "https://{0}.newgrounds.com/")
                };
            }

            var anim = __instance.AnimationList[1].Animation;

            if (anim.keys[1].m_Time != 0.9999f)
            {
                anim.keys[1].m_Time = 0.9999f;
                anim.keys[1].m_Value = 0f;
            }

            //Themes
            __instance.BeatmapThemes[0].name = "PA Machine";
            __instance.BeatmapThemes[1].name = "PA Anarchy";
            __instance.BeatmapThemes[2].name = "PA Day Night";
            __instance.BeatmapThemes[3].name = "PA Donuts";
            __instance.BeatmapThemes[4].name = "PA Classic";
            __instance.BeatmapThemes[5].name = "PA New";
            __instance.BeatmapThemes[6].name = "PA Dark";
            __instance.BeatmapThemes[7].name = "PA White On Black";
            __instance.BeatmapThemes[8].name = "PA Black On White";

            __instance.BeatmapThemes.Add(Triggers.CreateTheme("PA Example Theme", "9",
                LSColors.HexToColor("212121"),
                LSColors.HexToColor("FFFFFF"),
                new List<Color>
                {
                    LSColors.HexToColor("E57373"),
                    LSColors.HexToColor("64B5F6"),
                    LSColors.HexToColor("81C784"),
                    LSColors.HexToColor("FFB74D")
                }, new List<Color>
                {
                    LSColors.HexToColor("3F59FC"),
                    LSColors.HexToColor("3AD4F5"),
                    LSColors.HexToColor("E91E63"),
                    LSColors.HexToColor("E91E63"),
                    LSColors.HexToColor("E91E63"),
                    LSColors.HexToColor("E91E63"),
                    LSColors.HexToColorAlpha("E91E6345"),
                    LSColors.HexToColor("FFFFFF"),
                    LSColors.HexToColor("000000")
                }, new List<Color>
                {
                    LSColors.HexToColor("212121"),
                    LSColors.HexToColor("E91E63"),
                    LSColors.HexToColor("E91E63"),
                    LSColors.HexToColor("E91E63"),
                    LSColors.HexToColor("E91E63"),
                    LSColors.HexToColor("E91E63"),
                    LSColors.HexToColor("E91E63"),
                    LSColors.HexToColor("E91E63"),
                    LSColors.HexToColor("E91E63")
                }));

            //Creates modded list
            __instance.PrefabTypes.Clear();
            for (int i = 0; i < prefabNames.Count; i++)
            {
                var nTT = new DataManager.PrefabType();
                nTT.Name = (string)prefabNames[i].BoxedValue;
                nTT.Color = (Color)prefabColors[i].BoxedValue;
                __instance.PrefabTypes.Add(nTT);
            }
        }

        [HarmonyPatch("SaveData", typeof(string), typeof(DataManager.GameData))]
        [HarmonyPrefix]
        private static bool DataSaver(DataManager __instance, IEnumerator __result, string __0, DataManager.GameData __1)
        {
            RTEditor.inst.StartCoroutine(RTEditor.SaveData(__0, __1));
            return false;
        }

        [HarmonyPatch("CreateBaseBeatmap")]
        [HarmonyPrefix]
        private static bool CreateBaseBeatmapPatch(ref DataManager.GameData __result)
        {
            __result = RTEditor.CreateBaseBeatmap();
            return false;
        }

        [HarmonyPatch("GeneratePrefabJSON")]
        [HarmonyPrefix]
        public static bool GeneratePrefabJSON(ref JSONNode __result, DataManager.GameData.Prefab __0)
        {
            JSONNode jsonnode = JSON.Parse("{}");
            jsonnode["name"] = __0.Name;
            jsonnode["type"] = __0.Type.ToString();
            bool flag = __0.ID != null;
            bool flag2 = flag;
            if (flag2)
            {
                jsonnode["id"] = __0.ID.ToString();
            }
            bool flag3 = __0.MainObjectID != null;
            bool flag4 = flag3;
            if (flag4)
            {
                jsonnode["main_obj_id"] = __0.MainObjectID.ToString();
            }
            jsonnode["offset"] = __0.Offset.ToString();
            for (int i = 0; i < __0.objects.Count; i++)
            {
                bool flag5 = __0.objects[i] != null;
                bool flag6 = flag5;
                if (flag6)
                {
                    jsonnode["objects"][i]["id"] = __0.objects[i].id;
                    bool flag7 = __0.objects[i].GetParentType().ToString() != "101";
                    bool flag8 = flag7;
                    if (flag8)
                    {
                        jsonnode["objects"][i]["pt"] = __0.objects[i].GetParentType().ToString();
                    }
                    bool flag9 = __0.objects[i].getParentOffsets().FindIndex((float x) => x != 0f) != -1;
                    bool flag10 = flag9;
                    if (flag10)
                    {
                        int num = 0;
                        foreach (float num2 in __0.objects[i].getParentOffsets())
                        {
                            jsonnode["objects"][i]["po"][num] = num2.ToString();
                            num++;
                        }
                    }
                    jsonnode["objects"][i]["p"] = __0.objects[i].parent.ToString();
                    jsonnode["objects"][i]["d"] = __0.objects[i].Depth.ToString();
                    jsonnode["objects"][i]["ot"] = (int)__0.objects[i].objectType;
                    jsonnode["objects"][i]["st"] = __0.objects[i].StartTime.ToString();
                    bool flag11 = !string.IsNullOrEmpty(__0.objects[i].text);
                    bool flag12 = flag11;
                    if (flag12)
                    {
                        jsonnode["objects"][i]["text"] = __0.objects[i].text;
                    }
                    jsonnode["objects"][i]["name"] = __0.objects[i].name;
                    bool flag13 = __0.objects[i].shape != 0;
                    bool flag14 = flag13;
                    if (flag14)
                    {
                        jsonnode["objects"][i]["shape"] = __0.objects[i].shape.ToString();
                    }
                    jsonnode["objects"][i]["akt"] = (int)__0.objects[i].autoKillType;
                    jsonnode["objects"][i]["ako"] = __0.objects[i].autoKillOffset;
                    bool flag15 = __0.objects[i].shapeOption != 0;
                    bool flag16 = flag15;
                    if (flag16)
                    {
                        jsonnode["objects"][i]["so"] = __0.objects[i].shapeOption.ToString();
                    }
                    jsonnode["objects"][i]["o"]["x"] = __0.objects[i].origin.x.ToString();
                    bool locked = __0.objects[i].editorData.locked;
                    bool flag17 = locked;
                    if (flag17)
                    {
                        jsonnode["objects"][i]["ed"]["locked"] = __0.objects[i].editorData.locked.ToString();
                    }
                    bool collapse = __0.objects[i].editorData.collapse;
                    bool flag18 = collapse;
                    if (flag18)
                    {
                        jsonnode["objects"][i]["ed"]["shrink"] = __0.objects[i].editorData.collapse.ToString();
                    }
                    jsonnode["objects"][i]["o"]["y"] = __0.objects[i].origin.y.ToString();
                    jsonnode["objects"][i]["ed"]["bin"] = __0.objects[i].editorData.Bin.ToString();
                    jsonnode["objects"][i]["ed"]["layer"] = __0.objects[i].editorData.Layer.ToString();
                    for (int j = 0; j < __0.objects[i].events[0].Count; j++)
                    {
                        jsonnode["objects"][i]["events"]["pos"][j]["t"] = __0.objects[i].events[0][j].eventTime.ToString();
                        jsonnode["objects"][i]["events"]["pos"][j]["x"] = __0.objects[i].events[0][j].eventValues[0].ToString();
                        jsonnode["objects"][i]["events"]["pos"][j]["y"] = __0.objects[i].events[0][j].eventValues[1].ToString();

                        if (__0.objects[i].events[0][j].eventValues.Length > 2)
                        {
                            jsonnode["objects"][i]["events"]["pos"][j]["z"] = __0.objects[i].events[0][j].eventValues[2].ToString();
                        }

                        bool flag19 = __0.objects[i].events[0][j].curveType.Name != DataManager.inst.AnimationList[0].Name;
                        bool flag20 = flag19;
                        if (flag20)
                        {
                            jsonnode["objects"][i]["events"]["pos"][j]["ct"] = __0.objects[i].events[0][j].curveType.Name.ToString();
                        }
                        bool flag21 = __0.objects[i].events[0][j].random != 0;
                        bool flag22 = flag21;
                        if (flag22)
                        {
                            jsonnode["objects"][i]["events"]["pos"][j]["r"] = __0.objects[i].events[0][j].random.ToString();
                            jsonnode["objects"][i]["events"]["pos"][j]["rx"] = __0.objects[i].events[0][j].eventRandomValues[0].ToString();
                            jsonnode["objects"][i]["events"]["pos"][j]["ry"] = __0.objects[i].events[0][j].eventRandomValues[1].ToString();
                            jsonnode["objects"][i]["events"]["pos"][j]["rz"] = __0.objects[i].events[0][j].eventRandomValues[2].ToString();
                        }
                    }
                    for (int k = 0; k < __0.objects[i].events[1].Count; k++)
                    {
                        jsonnode["objects"][i]["events"]["sca"][k]["t"] = __0.objects[i].events[1][k].eventTime.ToString();
                        jsonnode["objects"][i]["events"]["sca"][k]["x"] = __0.objects[i].events[1][k].eventValues[0].ToString();
                        jsonnode["objects"][i]["events"]["sca"][k]["y"] = __0.objects[i].events[1][k].eventValues[1].ToString();
                        bool flag23 = __0.objects[i].events[1][k].curveType.Name != DataManager.inst.AnimationList[0].Name;
                        bool flag24 = flag23;
                        if (flag24)
                        {
                            jsonnode["objects"][i]["events"]["sca"][k]["ct"] = __0.objects[i].events[1][k].curveType.Name.ToString();
                        }
                        bool flag25 = __0.objects[i].events[1][k].random != 0;
                        bool flag26 = flag25;
                        if (flag26)
                        {
                            jsonnode["objects"][i]["events"]["sca"][k]["r"] = __0.objects[i].events[1][k].random.ToString();
                            jsonnode["objects"][i]["events"]["sca"][k]["rx"] = __0.objects[i].events[1][k].eventRandomValues[0].ToString();
                            jsonnode["objects"][i]["events"]["sca"][k]["ry"] = __0.objects[i].events[1][k].eventRandomValues[1].ToString();
                            jsonnode["objects"][i]["events"]["sca"][k]["rz"] = __0.objects[i].events[1][k].eventRandomValues[2].ToString();
                        }
                    }
                    for (int l = 0; l < __0.objects[i].events[2].Count; l++)
                    {
                        jsonnode["objects"][i]["events"]["rot"][l]["t"] = __0.objects[i].events[2][l].eventTime.ToString();
                        jsonnode["objects"][i]["events"]["rot"][l]["x"] = __0.objects[i].events[2][l].eventValues[0].ToString();
                        bool flag27 = __0.objects[i].events[2][l].curveType.Name != DataManager.inst.AnimationList[0].Name;
                        bool flag28 = flag27;
                        if (flag28)
                        {
                            jsonnode["objects"][i]["events"]["rot"][l]["ct"] = __0.objects[i].events[2][l].curveType.Name.ToString();
                        }
                        bool flag29 = __0.objects[i].events[2][l].random != 0;
                        bool flag30 = flag29;
                        if (flag30)
                        {
                            jsonnode["objects"][i]["events"]["rot"][l]["r"] = __0.objects[i].events[2][l].random.ToString();
                            jsonnode["objects"][i]["events"]["rot"][l]["rx"] = __0.objects[i].events[2][l].eventRandomValues[0].ToString();
                            jsonnode["objects"][i]["events"]["rot"][l]["rz"] = __0.objects[i].events[2][l].eventRandomValues[2].ToString();
                        }
                    }
                    for (int m = 0; m < __0.objects[i].events[3].Count; m++)
                    {
                        jsonnode["objects"][i]["events"]["col"][m]["t"] = __0.objects[i].events[3][m].eventTime.ToString();
                        jsonnode["objects"][i]["events"]["col"][m]["x"] = __0.objects[i].events[3][m].eventValues[0].ToString();
                        bool flag31 = __0.objects[i].events[3][m].curveType.Name != DataManager.inst.AnimationList[0].Name;
                        bool flag32 = flag31;
                        if (flag32)
                        {
                            jsonnode["objects"][i]["events"]["col"][m]["ct"] = __0.objects[i].events[3][m].curveType.Name.ToString();
                        }
                        bool flag33 = __0.objects[i].events[3][m].random != 0;
                        bool flag34 = flag33;
                        if (flag34)
                        {
                            jsonnode["objects"][i]["events"]["col"][m]["r"] = __0.objects[i].events[3][m].random.ToString();
                            jsonnode["objects"][i]["events"]["col"][m]["rx"] = __0.objects[i].events[3][m].eventRandomValues[0].ToString();
                        }
                    }
                }
            }
            __result = jsonnode;
            return false;
        }
    }

    [HarmonyPatch(typeof(DataManager.BeatmapTheme))]
    public class DataManagerBeatmapThemePatch : MonoBehaviour
    {
        [HarmonyPatch("ClearBeatmap")]
        [HarmonyPrefix]
        private static bool ClearBeatmapPrefix(DataManager.BeatmapTheme __instance)
        {
            __instance.playerColors.Clear();
            __instance.objectColors.Clear();
            __instance.backgroundColors.Clear();
            __instance.id = LSText.randomNumString(6);
            __instance.name = ConfigEntries.TemplateThemeName.Value;
            __instance.guiColor = ConfigEntries.TemplateThemeGUIColor.Value;
            __instance.backgroundColor = ConfigEntries.TemplateThemeBGColor.Value;
            __instance.playerColors.Add(ConfigEntries.TemplateThemePlayerColor1.Value);
            __instance.playerColors.Add(ConfigEntries.TemplateThemePlayerColor2.Value);
            __instance.playerColors.Add(ConfigEntries.TemplateThemePlayerColor3.Value);
            __instance.playerColors.Add(ConfigEntries.TemplateThemePlayerColor4.Value);
            __instance.objectColors.Add(ConfigEntries.TemplateThemeOBJColor1.Value);
            __instance.objectColors.Add(ConfigEntries.TemplateThemeOBJColor2.Value);
            __instance.objectColors.Add(ConfigEntries.TemplateThemeOBJColor3.Value);
            __instance.objectColors.Add(ConfigEntries.TemplateThemeOBJColor4.Value);
            __instance.objectColors.Add(ConfigEntries.TemplateThemeOBJColor5.Value);
            __instance.objectColors.Add(ConfigEntries.TemplateThemeOBJColor6.Value);
            __instance.objectColors.Add(ConfigEntries.TemplateThemeOBJColor7.Value);
            __instance.objectColors.Add(ConfigEntries.TemplateThemeOBJColor8.Value);
            __instance.objectColors.Add(ConfigEntries.TemplateThemeOBJColor9.Value);
            __instance.objectColors.Add(ConfigEntries.TemplateThemeOBJColor1.Value);
            __instance.objectColors.Add(ConfigEntries.TemplateThemeOBJColor2.Value);
            __instance.objectColors.Add(ConfigEntries.TemplateThemeOBJColor3.Value);
            __instance.objectColors.Add(ConfigEntries.TemplateThemeOBJColor4.Value);
            __instance.objectColors.Add(ConfigEntries.TemplateThemeOBJColor5.Value);
            __instance.objectColors.Add(ConfigEntries.TemplateThemeOBJColor6.Value);
            __instance.objectColors.Add(ConfigEntries.TemplateThemeOBJColor7.Value);
            __instance.objectColors.Add(ConfigEntries.TemplateThemeOBJColor8.Value);
            __instance.objectColors.Add(ConfigEntries.TemplateThemeOBJColor9.Value);
            __instance.backgroundColors.Add(ConfigEntries.TemplateThemeBGColor1.Value);
            __instance.backgroundColors.Add(ConfigEntries.TemplateThemeBGColor2.Value);
            __instance.backgroundColors.Add(ConfigEntries.TemplateThemeBGColor3.Value);
            __instance.backgroundColors.Add(ConfigEntries.TemplateThemeBGColor4.Value);
            __instance.backgroundColors.Add(ConfigEntries.TemplateThemeBGColor5.Value);
            __instance.backgroundColors.Add(ConfigEntries.TemplateThemeBGColor6.Value);
            __instance.backgroundColors.Add(ConfigEntries.TemplateThemeBGColor7.Value);
            __instance.backgroundColors.Add(ConfigEntries.TemplateThemeBGColor8.Value);
            __instance.backgroundColors.Add(ConfigEntries.TemplateThemeBGColor9.Value);
            return false;
        }

        [HarmonyPatch("Lerp")]
        [HarmonyPrefix]
        private static bool Lerp(DataManager.BeatmapTheme __instance, ref DataManager.BeatmapTheme _start, ref DataManager.BeatmapTheme _end, float _val)
        {
            __instance.guiColor = Color.Lerp(_start.guiColor, _end.guiColor, _val);
            __instance.backgroundColor = Color.Lerp(_start.backgroundColor, _end.backgroundColor, _val);
            for (int i = 0; i < 4; i++)
            {
                if (_start.playerColors[i] != null && _end.playerColors[i] != null)
                {
                    __instance.playerColors[i] = Color.Lerp(_start.GetPlayerColor(i), _end.GetPlayerColor(i), _val);
                }
            }

            int maxObj = 9;
            if (_start.objectColors.Count > 9 || _end.objectColors.Count > 9)
            {
                maxObj = 18;
            }

            for (int j = 0; j < maxObj; j++)
            {
                if (_start.objectColors[j] != null && _end.objectColors[j] != null)
                {
                    __instance.objectColors[j] = Color.Lerp(_start.GetObjColor(j), _end.GetObjColor(j), _val);
                }
            }
            for (int k = 0; k < 9; k++)
            {
                if (_start.backgroundColors[k] != null && _end.backgroundColors[k] != null)
                {
                    __instance.backgroundColors[k] = Color.Lerp(_start.GetBGColor(k), _end.GetBGColor(k), _val);
                }
            }
            return false;
        }

        [HarmonyPatch("Parse")]
        [HarmonyPrefix]
        private static bool ParsePrefix(DataManager.BeatmapTheme __instance, ref DataManager.BeatmapTheme __result, JSONNode __0, bool __1)
        {
            DataManager.BeatmapTheme beatmapTheme = new DataManager.BeatmapTheme();
            beatmapTheme.id = DataManager.inst.AllThemes.Count().ToString();
            if (__0["id"] != null)
                beatmapTheme.id = __0["id"];
            beatmapTheme.name = "name your themes!";
            if (__0["name"] != null)
                beatmapTheme.name = __0["name"];
            beatmapTheme.guiColor = LSColors.gray800;
            if (__0["gui"] != null)
                beatmapTheme.guiColor = LSColors.HexToColorAlpha(__0["gui"]);
            beatmapTheme.backgroundColor = LSColors.gray100;
            if (__0["bg"] != null)
                beatmapTheme.backgroundColor = LSColors.HexToColor(__0["bg"]);
            if (__0["players"] == null)
            {
                beatmapTheme.playerColors.Add(LSColors.HexToColorAlpha("E57373FF"));
                beatmapTheme.playerColors.Add(LSColors.HexToColorAlpha("64B5F6FF"));
                beatmapTheme.playerColors.Add(LSColors.HexToColorAlpha("81C784FF"));
                beatmapTheme.playerColors.Add(LSColors.HexToColorAlpha("FFB74DFF"));
            }
            else
            {
                int num = 0;
                foreach (KeyValuePair<string, JSONNode> keyValuePair in __0["players"].AsArray)
                {
                    JSONNode hex = keyValuePair;
                    if (num <= 3)
                    {
                        if (hex != null)
                        {
                            beatmapTheme.playerColors.Add(LSColors.HexToColorAlpha(hex));
                        }
                        else
                            beatmapTheme.playerColors.Add(LSColors.pink500);
                        ++num;
                    }
                    else
                        break;
                }
                while (beatmapTheme.playerColors.Count <= 3)
                    beatmapTheme.playerColors.Add(LSColors.pink500);
            }
            if (__0["objs"] == null)
            {
                beatmapTheme.objectColors.Add(LSColors.pink100);
                beatmapTheme.objectColors.Add(LSColors.pink200);
                beatmapTheme.objectColors.Add(LSColors.pink300);
                beatmapTheme.objectColors.Add(LSColors.pink400);
                beatmapTheme.objectColors.Add(LSColors.pink500);
                beatmapTheme.objectColors.Add(LSColors.pink600);
                beatmapTheme.objectColors.Add(LSColors.pink700);
                beatmapTheme.objectColors.Add(LSColors.pink800);
                beatmapTheme.objectColors.Add(LSColors.pink900);
            }
            else
            {
                int num = 0;
                Color color = LSColors.pink500;
                foreach (KeyValuePair<string, JSONNode> keyValuePair in __0["objs"].AsArray)
                {
                    JSONNode hex = keyValuePair;
                    if (num <= 17)
                    {
                        if (hex != null)
                        {
                            beatmapTheme.objectColors.Add(LSColors.HexToColorAlpha(hex));
                            color = LSColors.HexToColorAlpha(hex);
                        }
                        else
                            beatmapTheme.objectColors.Add(LSColors.pink500);
                        ++num;
                    }
                    else
                        break;
                }
                while (beatmapTheme.objectColors.Count <= 17)
                    beatmapTheme.objectColors.Add(color);
            }
            if (__0["bgs"] == null)
            {
                beatmapTheme.backgroundColors.Add(LSColors.gray100);
                beatmapTheme.backgroundColors.Add(LSColors.gray200);
                beatmapTheme.backgroundColors.Add(LSColors.gray300);
                beatmapTheme.backgroundColors.Add(LSColors.gray400);
                beatmapTheme.backgroundColors.Add(LSColors.gray500);
            }
            else
            {
                int num = 0;
                Color color = LSColors.pink500;
                foreach (KeyValuePair<string, JSONNode> keyValuePair in __0["bgs"].AsArray)
                {
                    JSONNode hex = keyValuePair;
                    if (num <= 8)
                    {
                        if (hex != null)
                        {
                            beatmapTheme.backgroundColors.Add(LSColors.HexToColor(hex));
                            color = LSColors.HexToColor(hex);
                        }
                        else
                            beatmapTheme.backgroundColors.Add(LSColors.pink500);
                        ++num;
                    }
                    else
                        break;
                }
                while (beatmapTheme.backgroundColors.Count <= 8)
                    beatmapTheme.backgroundColors.Add(color);
            }
            if (__1)
            {
                DataManager.inst.CustomBeatmapThemes.Add(beatmapTheme);
                if (DataManager.inst.BeatmapThemeIDToIndex.ContainsKey(int.Parse(beatmapTheme.id)))
                {
                    if (EditorManager.inst != null)
                        EditorManager.inst.DisplayNotification("Unable to Load theme [" + beatmapTheme.name + "]", 2f, EditorManager.NotificationType.Error);
                }
                else
                {
                    DataManager.inst.BeatmapThemeIndexToID.Add(DataManager.inst.AllThemes.Count() - 1, int.Parse(beatmapTheme.id));
                    DataManager.inst.BeatmapThemeIDToIndex.Add(int.Parse(beatmapTheme.id), DataManager.inst.AllThemes.Count() - 1);
                }
            }
            __result = beatmapTheme;
            return false;
        }

        [HarmonyPatch("DeepCopy")]
        [HarmonyPrefix]
        private static bool DeepCopyPatch(ref DataManager.BeatmapTheme __result, DataManager.BeatmapTheme __0, bool __1 = false)
        {
            DataManager.BeatmapTheme beatmapTheme = new DataManager.BeatmapTheme();
            beatmapTheme.name = __0.name;
            beatmapTheme.playerColors = new List<Color>((from cols in __0.playerColors
                                                         select new Color(cols.r, cols.g, cols.b, cols.a)).ToList());
            beatmapTheme.objectColors = new List<Color>((from cols in __0.objectColors
                                                         select new Color(cols.r, cols.g, cols.b, cols.a)).ToList());
            beatmapTheme.guiColor = __0.guiColor;
            beatmapTheme.backgroundColor = __0.backgroundColor;
            beatmapTheme.backgroundColors = new List<Color>((from cols in __0.backgroundColors
                                                             select new Color(cols.r, cols.g, cols.b, cols.a)).ToList());
            AccessTools.Field(typeof(DataManager.BeatmapTheme), "expanded").SetValue(beatmapTheme, AccessTools.Field(typeof(DataManager.BeatmapTheme), "expanded").GetValue(__0));
            DataManager.BeatmapTheme beatmapTheme2 = beatmapTheme;
            if (__1)
            {
                beatmapTheme2.id = __0.id;
            }
            if (beatmapTheme2.objectColors.Count < __0.objectColors.Count)
            {
                Color item = beatmapTheme2.objectColors.Last();
                while (beatmapTheme2.objectColors.Count < __0.objectColors.Count)
                {
                    beatmapTheme2.objectColors.Add(item);
                }
            }
            if (beatmapTheme2.backgroundColors.Count < 9)
            {
                Color item2 = beatmapTheme2.backgroundColors.Last();
                while (beatmapTheme2.backgroundColors.Count < 9)
                {
                    beatmapTheme2.backgroundColors.Add(item2);
                }
            }
            __result = beatmapTheme2;
            return false;
        }
    }

    [HarmonyPatch(typeof(DataManager.GameData))]
    public class DataManagerGameDataPatch : MonoBehaviour
    {
        [HarmonyPatch("ParseBeatmap")]
        [HarmonyPrefix]
        private static bool ParseBeatmapPatch(string _json)
        {
            Debug.LogFormat("{0} Parse Beatmap", EditorPlugin.className);
            DataManager.inst.StartCoroutine(RTFile.ParseBeatmap(_json));
            return false;
        }
    }
}
