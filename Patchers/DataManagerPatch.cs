using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

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
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        private static void DataLists()
        {
            if (DataManager.inst.difficulties.Count != 7)
            {
                DataManager.inst.difficulties = new List<DataManager.Difficulty>
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

            if (DataManager.inst.linkTypes[3].name != "YouTube")
            {
                DataManager.inst.linkTypes = new List<DataManager.LinkType>
                {
                    new DataManager.LinkType("Spotify", "https://open.spotify.com/artist/{0}"),
                    new DataManager.LinkType("SoundCloud", "https://soundcloud.com/{0}"),
                    new DataManager.LinkType("Bandcamp", "https://{0}.bandcamp.com"),
                    new DataManager.LinkType("Youtube", "https://www.youtube.com/user/{0}"),
                    new DataManager.LinkType("Newgrounds", "https://{0}.newgrounds.com/")
                };
            }

            if (DataManager.inst.AnimationList[1].Animation.keys[1].m_Time != 0.9999f)
            {
                DataManager.inst.AnimationList[1].Animation.keys[1].m_Time = 0.9999f;
                DataManager.inst.AnimationList[1].Animation.keys[1].m_Value = 0f;
            }

            //Themes
			DataManager.inst.BeatmapThemes[0].name = "PA Machine";
			DataManager.inst.BeatmapThemes[1].name = "PA Anarchy";
			DataManager.inst.BeatmapThemes[2].name = "PA Day Night";
			DataManager.inst.BeatmapThemes[3].name = "PA Donuts";
			DataManager.inst.BeatmapThemes[4].name = "PA Classic";
			DataManager.inst.BeatmapThemes[5].name = "PA New";
			DataManager.inst.BeatmapThemes[6].name = "PA Dark";
			DataManager.inst.BeatmapThemes[7].name = "PA White On Black";
			DataManager.inst.BeatmapThemes[8].name = "PA Black On White";
            DataManager.inst.BeatmapThemes.Add(Triggers.CreateTheme("PA Example Theme", "9",
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
            DataManager.inst.PrefabTypes = new List<DataManager.PrefabType>
            {
                new DataManager.PrefabType(),
                new DataManager.PrefabType(),
                new DataManager.PrefabType(),
                new DataManager.PrefabType(),
                new DataManager.PrefabType(),
                new DataManager.PrefabType(),
                new DataManager.PrefabType(),
                new DataManager.PrefabType(),
                new DataManager.PrefabType(),
                new DataManager.PrefabType(),
                new DataManager.PrefabType(),
                new DataManager.PrefabType(),
                new DataManager.PrefabType(),
                new DataManager.PrefabType(),
                new DataManager.PrefabType(),
                new DataManager.PrefabType(),
                new DataManager.PrefabType(),
                new DataManager.PrefabType(),
                new DataManager.PrefabType(),
                new DataManager.PrefabType()
            };

            //Set Name
            DataManager.inst.PrefabTypes[0].Name = ConfigEntries.PT0N.Value;
            DataManager.inst.PrefabTypes[1].Name = ConfigEntries.PT1N.Value;
            DataManager.inst.PrefabTypes[2].Name = ConfigEntries.PT2N.Value;
            DataManager.inst.PrefabTypes[3].Name = ConfigEntries.PT3N.Value;
            DataManager.inst.PrefabTypes[4].Name = ConfigEntries.PT4N.Value;
            DataManager.inst.PrefabTypes[5].Name = ConfigEntries.PT5N.Value;
            DataManager.inst.PrefabTypes[6].Name = ConfigEntries.PT6N.Value;
            DataManager.inst.PrefabTypes[7].Name = ConfigEntries.PT7N.Value;
            DataManager.inst.PrefabTypes[8].Name = ConfigEntries.PT8N.Value;
            DataManager.inst.PrefabTypes[9].Name = ConfigEntries.PT9N.Value;

            //Set New Name
            DataManager.inst.PrefabTypes[10].Name = ConfigEntries.PT10N.Value;
            DataManager.inst.PrefabTypes[11].Name = ConfigEntries.PT11N.Value;
            DataManager.inst.PrefabTypes[12].Name = ConfigEntries.PT12N.Value;
            DataManager.inst.PrefabTypes[13].Name = ConfigEntries.PT13N.Value;
            DataManager.inst.PrefabTypes[14].Name = ConfigEntries.PT14N.Value;
            DataManager.inst.PrefabTypes[15].Name = ConfigEntries.PT15N.Value;
            DataManager.inst.PrefabTypes[16].Name = ConfigEntries.PT16N.Value;
            DataManager.inst.PrefabTypes[17].Name = ConfigEntries.PT17N.Value;
            DataManager.inst.PrefabTypes[18].Name = ConfigEntries.PT18N.Value;
            DataManager.inst.PrefabTypes[19].Name = ConfigEntries.PT19N.Value;

            //Set Color
            DataManager.inst.PrefabTypes[0].Color = ConfigEntries.PT0C.Value;
            DataManager.inst.PrefabTypes[1].Color = ConfigEntries.PT1C.Value;
            DataManager.inst.PrefabTypes[2].Color = ConfigEntries.PT2C.Value;
            DataManager.inst.PrefabTypes[3].Color = ConfigEntries.PT3C.Value;
            DataManager.inst.PrefabTypes[4].Color = ConfigEntries.PT4C.Value;
            DataManager.inst.PrefabTypes[5].Color = ConfigEntries.PT5C.Value;
            DataManager.inst.PrefabTypes[6].Color = ConfigEntries.PT6C.Value;
            DataManager.inst.PrefabTypes[7].Color = ConfigEntries.PT7C.Value;
            DataManager.inst.PrefabTypes[8].Color = ConfigEntries.PT8C.Value;
            DataManager.inst.PrefabTypes[9].Color = ConfigEntries.PT9C.Value;

            //Set New Color
            DataManager.inst.PrefabTypes[10].Color = ConfigEntries.PT10C.Value;
            DataManager.inst.PrefabTypes[11].Color = ConfigEntries.PT11C.Value;
            DataManager.inst.PrefabTypes[12].Color = ConfigEntries.PT12C.Value;
            DataManager.inst.PrefabTypes[13].Color = ConfigEntries.PT13C.Value;
            DataManager.inst.PrefabTypes[14].Color = ConfigEntries.PT14C.Value;
            DataManager.inst.PrefabTypes[15].Color = ConfigEntries.PT15C.Value;
            DataManager.inst.PrefabTypes[16].Color = ConfigEntries.PT16C.Value;
            DataManager.inst.PrefabTypes[17].Color = ConfigEntries.PT17C.Value;
            DataManager.inst.PrefabTypes[18].Color = ConfigEntries.PT18C.Value;
            DataManager.inst.PrefabTypes[19].Color = ConfigEntries.PT19C.Value;
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
