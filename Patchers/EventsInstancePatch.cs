using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

namespace EditorManagement.Patchers
{
    [HarmonyPatch(typeof(DataManager.GameData.EventObjects), MethodType.Constructor)]
    public class EventsInstance
    {
        public static void Postfix(DataManager.GameData.EventObjects __instance)
        {
            if (!EventEditorPatch.eventsCore && EditorManager.inst != null && __instance.allEvents.Count > 10)
            {
                __instance.allEvents.RemoveAt(10);
            }
        }
    }
}
