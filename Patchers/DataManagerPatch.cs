using System;
using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using RTFunctions.Patchers;

using EditorManagement.Functions;
using EditorManagement.Functions.Editors;

namespace EditorManagement.Patchers
{
    [HarmonyPatch(typeof(DataManager))]
    public class DataManagerPatch : MonoBehaviour
    {
        public static DataManager Instance { get => DataManager.inst; set => DataManager.inst = value; }

        public static void Init()
        {
            //Patcher.CreatePatch(Instance.Start, PatchType.Postfix, (Action)StartPostfix);
            //Patcher.CreatePatch(AccessTools.Method(typeof(DataManager), "CreateBaseBeatmap"), PatchType.Prefix, AccessTools.Method(typeof(DataManagerPatch), "CreateBaseBeatmapPrefix"));
        }

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void StartPostfix()
        {
            try
            {
                int num = 0;
                while (Instance.PrefabTypes.Count < 20)
                {
                    var prefabType = new DataManager.PrefabType();
                    prefabType.Color = Color.white;
                    prefabType.Name = "NewType " + num.ToString();

                    Instance.PrefabTypes.Add(prefabType);
                    num++;
                }

                for (int i = 0; i < Instance.PrefabTypes.Count; i++)
                {
                    var p = Instance.PrefabTypes[i];
                    var prefabType = new RTFunctions.Functions.Data.PrefabType(p.Name, p.Color);
                    Instance.PrefabTypes[i] = prefabType;
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex.ToString());
            }
        }
    }
}
