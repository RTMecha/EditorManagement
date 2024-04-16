using EditorManagement.Functions.Editors;
using HarmonyLib;

namespace EditorManagement.Patchers
{
    [HarmonyPatch(typeof(GameManager))]
    public class GameManagerPatch
    {
        [HarmonyPatch("UpdateTimeline")]
        [HarmonyPostfix]
        static void UpdateTimelinePostfix()
        {
            RTEditor.inst?.UpdateTimeline();
        }
    }
}
