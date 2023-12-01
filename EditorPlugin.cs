using System;
using System.Collections.Generic;
using System.Linq;

using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

using UnityEngine;

using EditorManagement.Patchers;
using EditorManagement.Functions;

using RTFunctions.Functions;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;

namespace EditorManagement
{
    [BepInPlugin("com.mecha.editormanagement", "EditorManagement", "2.0.0")]
    public class EditorPlugin : BaseUnityPlugin
    {
        // TO DO BEFORE RELEASE
        // Replace "settings" with a duplicate of the other dropdowns and put the "Level Settings" as one of the tabs.

        public static EditorPlugin inst;
        public static string className = $"[<color=#F6AC1A>Editor</color><color=#2FCBD6>Management</color>] {PluginInfo.PLUGIN_VERSION}\n";
        readonly Harmony harmony = new Harmony("EditorManagement");

		public static List<int> allLayers = new List<int>();

		public static float timeEdit;
        public static float itsTheTime;

        public static int levelFilter = 0;
        public static bool levelAscend = true;

        void Awake()
        {
            inst = this;

            Config.SettingChanged += new EventHandler<SettingChangedEventArgs>(UpdateEditorManagementConfigs);

            harmony.PatchAll();

            if (!ModCompatibility.mods.ContainsKey("EditorManagement"))
            {
                var mod = new ModCompatibility.Mod(this, GetType());
                ModCompatibility.mods.Add("EditorManagement", mod);
            }

            ModCompatibility.sharedFunctions.Add("HighlightColor", ConfigEntries.HighlightColor.Value);
            ModCompatibility.sharedFunctions.Add("HighlightDoubleColor", ConfigEntries.HighlightDoubleColor.Value);

            ModCompatibility.sharedFunctions.Add("CanHightlightObjects", ConfigEntries.HighlightObjects.Value);
            ModCompatibility.sharedFunctions.Add("ShowObjectsAlpha", ConfigEntries.ShowObjectsAlpha.Value);
            ModCompatibility.sharedFunctions.Add("ShowObjectsOnLayer", ConfigEntries.ShowObjectsOnLayer.Value);
            ModCompatibility.sharedFunctions.Add("ShowEmpties", ConfigEntries.ShowEmpties.Value);
            ModCompatibility.sharedFunctions.Add("ShowDamagable", ConfigEntries.ShowDamagable.Value);

            // Plugin startup logic
            Logger.LogInfo($"Plugin EditorManagement is loaded!");
        }

        void UpdateEditorManagementConfigs(object sender, SettingChangedEventArgs e)
        {

        }

		public static void ListObjectLayers()
		{
			allLayers.Clear();
			foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
			{
				if (!allLayers.Contains(beatmapObject.editorData.Layer))
				{
					allLayers.Add(beatmapObject.editorData.Layer);
				}
			}

			allLayers = (from x in allLayers
						 orderby x ascending
						 select x).ToList();

			string lister = "";

			int i = 0;
			foreach (var l in allLayers)
			{
				int num = l + 1;
				if (num > 5)
				{
					num -= 1;
				}
				if (!lister.Contains(num.ToString()))
				{
					lister += num.ToString();
					if (i != allLayers.Count - 1)
						lister += ", ";
				}
				i++;
			}

			EditorManager.inst.DisplayNotification("Objects on Layers:<br>[ " + lister + " ]", 2f, EditorManager.NotificationType.Info);
		}

		public static void SetNewMarkerColors()
		{
			MarkerEditor.inst.markerColors = new List<Color>
			{
				new Color(0.8745f, 0.4745f, 0.4392f, 1f),
				new Color(1f, 0.502f, 0.6706f, 1f),
				new Color(0.9176f, 0.502f, 0.9882f, 1f),
				new Color(0.702f, 0.5333f, 1f, 1f),
				new Color(0.549f, 0.6196f, 1f, 1f),
				new Color(0.502f, 0.8471f, 1f, 1f),
				new Color(0.6549f, 1f, 0.9216f, 1f),
				new Color(0.9569f, 1f, 0.5059f, 1f),
				new Color(1f, 0.8196f, 0.502f, 1f),
				ConfigEntries.MarkerColN0.Value,
				ConfigEntries.MarkerColN1.Value,
				ConfigEntries.MarkerColN2.Value,
				ConfigEntries.MarkerColN3.Value,
				ConfigEntries.MarkerColN4.Value,
				ConfigEntries.MarkerColN5.Value,
				ConfigEntries.MarkerColN6.Value,
				ConfigEntries.MarkerColN7.Value,
				ConfigEntries.MarkerColN8.Value,
			};
		}

		public static bool DontRun() => false;
    }
}
