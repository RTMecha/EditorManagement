using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

using SimpleJSON;

using HarmonyLib;

using EditorManagement.Functions.Editors;
using EditorManagement.Functions.Components;
using EditorManagement.Functions;
using EditorManagement.Functions.Tools;

using LSFunctions;
using RTFunctions.Functions;

namespace EditorManagement.Patchers
{
	[HarmonyPatch(typeof(MetadataEditor))]
	public class MetadataPatch : MonoBehaviour
    {
		[HarmonyPatch("Render")]
		[HarmonyPostfix]
		private static void MetadataRender()
		{
			if (!EditorManager.inst.GetDialog("Metadata Editor").Dialog.Find("Scroll View").Find("Viewport").Find("Content").Find("artist").Find("x(Clone)"))
			{
				GameObject openLink = Instantiate(EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("Panel").Find("x").gameObject);

				openLink.transform.SetParent(EditorManager.inst.GetDialog("Metadata Editor").Dialog.Find("Scroll View").Find("Viewport").Find("Content").Find("artist"));
				openLink.transform.localScale = Vector3.one;
				openLink.transform.Find("Image").gameObject.GetComponent<Image>().sprite = EditorManager.inst.DropdownMenus[3].transform.Find("Open Workshop").Find("Image").gameObject.GetComponent<Image>().sprite;

				RectTransform openLinkRT = openLink.GetComponent<RectTransform>();
				LayoutElement openLinkLE = openLink.AddComponent<LayoutElement>();
				Button openLinkButton = openLink.GetComponent<Button>();

				openLinkRT.anchoredPosition = new Vector2(-520f, -72f);
				openLinkLE.ignoreLayout = true;
				openLinkButton.onClick.RemoveAllListeners();
				openLinkButton.onClick.AddListener(delegate ()
				{
					Application.OpenURL(string.Format(DataManager.inst.linkTypes[DataManager.inst.metaData.artist.LinkType].linkFormat, DataManager.inst.metaData.artist.Link));
				});

				ColorBlock cb = openLinkButton.colors;
				cb.normalColor = new Color(0f, 0.5f, 1f, 1f);
				cb.pressedColor = new Color(0.6f, 0.9f, 1f, 1f);
				cb.highlightedColor = new Color(0.3f, 0.6f, 1f, 1f);
				cb.selectedColor = new Color(0f, 0.5f, 1f, 1f);
				openLinkButton.colors = cb;
			}

			if (!EditorManager.inst.GetDialog("Metadata Editor").Dialog.Find("Scroll View/Viewport/Content/song/difficulty/toggles/master"))
			{
				GameObject master = Instantiate(EditorManager.inst.GetDialog("Metadata Editor").Dialog.Find("Scroll View/Viewport/Content/song/difficulty/toggles/expert +").gameObject);
				master.transform.SetParent(EditorManager.inst.GetDialog("Metadata Editor").Dialog.Find("Scroll View/Viewport/Content/song/difficulty/toggles"));
				master.name = "master";
				master.transform.Find("Background/Text").GetComponent<Text>().text = "Master";
				master.transform.localScale = Vector3.one;
			}
			if (!EditorManager.inst.GetDialog("Metadata Editor").Dialog.Find("Scroll View/Viewport/Content/song/difficulty/toggles/none"))
			{
				GameObject animation = Instantiate(EditorManager.inst.GetDialog("Metadata Editor").Dialog.Find("Scroll View/Viewport/Content/song/difficulty/toggles/expert +").gameObject);
				animation.transform.SetParent(EditorManager.inst.GetDialog("Metadata Editor").Dialog.Find("Scroll View/Viewport/Content/song/difficulty/toggles"));
				animation.name = "none";
				animation.transform.Find("Background/Text").GetComponent<Text>().text = "None";
				animation.transform.localScale = Vector3.one;
			}

			Transform transform = EditorManager.inst.GetDialog("Metadata Editor").Dialog.Find("Scroll View/Viewport/Content");

			Triggers.AddTooltip(transform.Find("song/difficulty").gameObject, "Shows players how difficult a level is.", "");

			for (int i = 0; i < DataManager.inst.difficulties.Count; i++)
			{
				var difficulty = transform.Find("song/difficulty/toggles").GetChild(i);

				if (!difficulty.GetComponent<HoverUI>())
                {
					var hoverUI = difficulty.gameObject.AddComponent<HoverUI>();
					hoverUI.animatePos = false;
					hoverUI.animateSca = true;
					hoverUI.size = 1.1f;
				}
				difficulty.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(69f, 32f);
				difficulty.Find("Background").gameObject.GetComponent<Image>().color = DataManager.inst.difficulties[i].color;
				difficulty.Find("Background/Text").GetComponent<Text>().fontSize = 19;

				int tmpIndex = i;
				var difficultyToggle = difficulty.GetComponent<Toggle>();
				difficultyToggle.onValueChanged.RemoveAllListeners();
				difficultyToggle.isOn = (DataManager.inst.metaData.song.difficulty == i);
				difficultyToggle.onValueChanged.AddListener(delegate (bool _val)
				{
					DataManager.inst.metaData.song.difficulty = tmpIndex;
				});
			}

			transform.Find("song/difficulty/toggles").GetComponent<RectTransform>().anchoredPosition = new Vector2(468f, -16f);

			Button uploadButton = EditorManager.inst.GetDialog("Metadata Editor").Dialog.Find("Scroll View/Viewport/Content/submit/submit").gameObject.GetComponent<Button>();
			uploadButton.onClick.ClearAll();
			uploadButton.onClick.AddListener(delegate ()
			{
				RTEditor.RefreshWarningPopup("This will create an encrypted song.lsen file to use instead of level.ogg. Are you sure you want to do this?", delegate ()
				{
					RTEditor.inst.StartCoroutine(RTEditor.EncryptLevel());

					EditorManager.inst.DisplayNotification("Encrypted song file to " + EditorPlugin.levelListSlash + EditorManager.inst.currentLoadedLevel + "/song.lsen", 2f, EditorManager.NotificationType.Success, false);
					EditorManager.inst.HideDialog("Warning Popup");
				}, delegate ()
				{
					EditorManager.inst.HideDialog("Warning Popup");
				});
			});
		}
	}
}
