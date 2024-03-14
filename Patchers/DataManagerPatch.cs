using HarmonyLib;
using RTFunctions.Functions.Data;
using System;
using UnityEngine;

namespace EditorManagement.Patchers
{
    [HarmonyPatch(typeof(DataManager))]
    public class DataManagerPatch : MonoBehaviour
    {
        public static DataManager Instance { get => DataManager.inst; set => DataManager.inst = value; }

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void StartPostfix()
        {
            try
            {
                int num = 0;
                while (Instance.PrefabTypes.Count < 20)
                {
                    var prefabType = new DataManager.PrefabType
                    {
                        Color = Color.white,
                        Name = "NewType " + num.ToString()
                    };

                    Instance.PrefabTypes.Add(prefabType);
                    num++;
                }

                for (int i = 0; i < Instance.PrefabTypes.Count; i++)
                {
                    var p = Instance.PrefabTypes[i];
                    var prefabType = new PrefabType(p.Name, p.Color);
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
