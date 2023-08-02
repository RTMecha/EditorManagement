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
				if (!transform.Find("song/difficulty/toggles").GetChild(i).GetComponent<HoverUI>())
                {
					var hoverUI = transform.Find("song/difficulty/toggles").GetChild(i).gameObject.AddComponent<HoverUI>();
					hoverUI.animatePos = false;
					hoverUI.animateSca = true;
					hoverUI.size = 1.1f;
				}
				transform.Find("song/difficulty/toggles").GetChild(i).gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(69f, 32f);
				transform.Find("song/difficulty/toggles").GetChild(i).Find("Background").gameObject.GetComponent<Image>().color = DataManager.inst.difficulties[i].color;
				transform.Find("song/difficulty/toggles").GetChild(i).Find("Background/Text").GetComponent<Text>().fontSize = 19;

				int tmpIndex = i;
				transform.Find("song/difficulty/toggles").GetChild(i).GetComponent<Toggle>().onValueChanged.RemoveAllListeners();
				transform.Find("song/difficulty/toggles").GetChild(i).GetComponent<Toggle>().isOn = (DataManager.inst.metaData.song.difficulty == i);
				transform.Find("song/difficulty/toggles").GetChild(i).GetComponent<Toggle>().onValueChanged.AddListener(delegate (bool _val)
				{
					DataManager.inst.metaData.song.difficulty = tmpIndex;
				});
			}

			transform.Find("song/difficulty/toggles").gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(468f, -16f);

			Button uploadButton = EditorManager.inst.GetDialog("Metadata Editor").Dialog.Find("Scroll View/Viewport/Content/submit/submit").gameObject.GetComponent<Button>();
			uploadButton.onClick.ClearAll();
			uploadButton.onClick.AddListener(delegate ()
			{
				string rawProfileJSON = null;
				rawProfileJSON = FileManager.inst.LoadJSONFile(EditorPlugin.levelListSlash + EditorManager.inst.currentLoadedLevel + "/level.lsb");

				JSONNode jsonnode = JSON.Parse(rawProfileJSON);

				RTEditor.inst.StartCoroutine(RTEditor.EncryptLevel());

				//RTFile.WriteToFile(EditorPlugin.levelListSlash + EditorManager.inst.currentLoadedLevel + "/encryptedlevel.lsb", LSEncryption.EncryptText(rawProfileJSON, "5erewtdvtedsfdSFCDS"));
				EditorManager.inst.DisplayNotification("Encrypted song file to " + EditorPlugin.levelListSlash + EditorManager.inst.currentLoadedLevel + "/song.lsen", 2f, EditorManager.NotificationType.Success, false);
			});
		}
	}
}
