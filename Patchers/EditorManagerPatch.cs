using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using UnityEngine;

using LSFunctions;

using RTFunctions.Patchers;

using EditorManagement.Functions;
using EditorManagement.Functions.Editors;

using BeatmapObject = DataManager.GameData.BeatmapObject;
using EventKeyframe = DataManager.GameData.EventKeyframe;
using Prefab = DataManager.GameData.Prefab;
using PrefabObject = DataManager.GameData.PrefabObject;
using BackgroundObject = DataManager.GameData.BackgroundObject;

using ObjectType = DataManager.GameData.BeatmapObject.ObjectType;
using AutoKillType = DataManager.GameData.BeatmapObject.AutoKillType;

using ObjectSelection = ObjEditor.ObjectSelection;
using ObjectKeyframeSelection = ObjEditor.KeyframeSelection;
using EventKeyframeSelection = EventEditor.KeyframeSelection;

namespace EditorManagement.Patchers
{
    public class EditorManagerPatch : MonoBehaviour
    {
        static EditorManager Instance { get => EditorManager.inst; set => EditorManager.inst = value; }
        static Type Type { get; set; }

        //public static MethodBase Awake
        //{
        //    get
        //    {
        //        if (EditorManager)
        //            return AccessTools.Method(typeof(EditorManager), "Awake");

        //        return null;
        //    }
        //}

        /// <summary>
        /// Inits patches for EditorManager
        /// </summary>
        public static void Init()
        {
            Type = typeof(EditorManager);
            Patcher.CreatePatch((Action)Instance.Awake, PatchType.Prefix, (PrefixMethod<EditorManager>)AwakePrefix);
            //Patcher.CreatePatch(AccessTools.Method(Type, "Awake"), PatchType.Prefix, (PrefixMethod)AwakePrefix);

            Patcher.CreatePatch((Action)Instance.Update, PatchType.Prefix, (PrefixMethod)UpdatePrefix);
            //Patcher.CreatePatch(AccessTools.Method(Type, "Update"), PatchType.Prefix, (PrefixMethod)UpdatePrefix);

            Patcher.CreatePatch((Action)Instance.AutoSaveLevel, PatchType.Prefix, (PrefixMethod)AutoSaveLevelPrefix);

            Patcher.CreatePatch(AccessTools.Method(Type, "LoadLevel"), PatchType.Prefix, AccessTools.Method(typeof(EditorManagerPatch), "LoadLevelPrefix"));
        }

        static bool AwakePrefix(EditorManager __instance)
        {
			if (!Instance)
				Instance = __instance;
			else if (Instance != Instance)
				Destroy(__instance.gameObject);


			EditorThemeManager.Init();

            return false;
        }

        static bool UpdatePrefix()
        {
            return false;
        }

        static bool AutoSaveLevelPrefix()
        {
            return false;
        }

        static bool LoadLevelPrefix(EditorManager __instance, ref IEnumerator __result, string __0)
        {
            return false;
        }

		static bool GetLevelListPrefix()
		{
            Instance.StartCoroutine(RTEditor.inst.LoadLevels());
			return false;
		}
	}
}
