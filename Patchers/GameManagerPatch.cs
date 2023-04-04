using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

using HarmonyLib;

namespace EditorManagement.Patchers
{
    [HarmonyPatch(typeof(GameManager))]
    public class GameManagerPatch : MonoBehaviour
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        private static void SetCameraClipPlanes()
        {
            Camera camera = GameObject.Find("Main Camera").GetComponent<Camera>();
            camera.farClipPlane = 100000;
            camera.nearClipPlane = -100000;
        }
    }
}
