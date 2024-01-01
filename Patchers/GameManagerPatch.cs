using HarmonyLib;

using EditorManagement.Functions.Editors;

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
