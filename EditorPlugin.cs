using BepInEx;
using HarmonyLib;

using EditorManagement.Patchers;
using EditorManagement.Functions;

namespace EditorManagement
{
    [BepInPlugin("com.mecha.editormanagement", "EditorManagement", "2.0.0")]
    public class EditorPlugin : BaseUnityPlugin
    {
        public static EditorPlugin inst;
        public static string className = $"[<color=#F6AC1A>Editor</color><color=#2FCBD6>Management</color>] {PluginInfo.PLUGIN_VERSION}\n";
        readonly Harmony harmony = new Harmony("EditorManagement");

        void Awake()
        {
            inst = this;

            EditorManagerPatch.Init();
            ObjEditorPatch.Init();

            // Plugin startup logic
            Logger.LogInfo($"Plugin EditorManagement is loaded!");
        }

        public static bool DontRun() => false;
    }
}
