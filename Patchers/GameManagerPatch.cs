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
            if (RTEditor.inst)
                RTEditor.inst.UpdateTimeline();
        }
    }
}
