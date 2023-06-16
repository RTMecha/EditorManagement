﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

using HarmonyLib;

using EditorManagement.Functions;
using LSFunctions;
using DG.Tweening;

namespace EditorManagement.Patchers
{
    [HarmonyPatch(typeof(GameManager))]
    public class GameManagerPatch : MonoBehaviour
    {
        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        private static void SetCameraClipPlanes()
        {
            EditorPlugin.SetCameraRenderDistance();
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        private static void UpdatePatch()
        {
            if (GameManager.inst.LiveTheme.objectColors.Count == 9)
            {
                for (int i = 0; i < 9; i++)
                {
                    GameManager.inst.LiveTheme.objectColors.Add(LSColors.pink900);
                }
            }
        }

        //[HarmonyPatch("LoadLevelCurrent")]
        //[HarmonyPrefix]
        //private static bool LevelDecrypter()
        //{
        //    string path = SaveManager.inst.ArcadeQueue.AudioFileStr.Replace("\\level.ogg", "/");
        //    Debug.LogFormat("{0}Trying to load song.lsen from (" + path + ")", EditorPlugin.className);
        //    if (DataManager.inst.GetSettingBool("IsArcade", false) && RTFile.FileExists(path + "song.lsen"))
        //    {
        //        Debug.LogFormat("{0}Loaded song.lsen from (" + path + ")", EditorPlugin.className);
        //        DiscordController.inst.OnIconChange("arcade");
        //        DiscordController.inst.OnDetailsChange("Playing Arcade");
        //        EditorPlugin.inst.StartCoroutine(EditorPlugin.PlayDecryptedLevel(path));
        //        return false;
        //    }
        //    return true;
        //}
    }
}
