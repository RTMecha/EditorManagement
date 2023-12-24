using System;
using System.IO;
using LSFunctions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using RTFunctions.Functions;

using TMPro;

using RTFunctions.Functions.Components;

namespace EditorManagement.Functions.Editors
{
	/// <summary>
	/// Class used to browse files while still in the editor, allowing users to select files outside of the game directory.
	/// </summary>
    public class RTFileBrowser : MonoBehaviour
    {
		public static RTFileBrowser inst;

        void Awake()
        {
			inst = this;
			title = transform.Find("Panel/Text").GetComponent<TextMeshProUGUI>();
		}

		public void UpdateBrowser(string _folder, string fileExtension, string specificName = "", Action<string> onSelectFile = null)
		{
			if (Directory.Exists(_folder))
			{
				title.text = $"<b>File Browser</b> ({fileExtension.ToLower()})";

				var dir = transform.Find("folder-bar").GetComponent<InputField>();
				dir.onValueChanged.ClearAll();
				dir.onValueChanged.AddListener(delegate (string _val)
				{
					UpdateBrowser(_val, fileExtension, specificName, onSelectFile);
				});

				LSHelpers.DeleteChildren(viewport);
				Debug.LogFormat("Update Browser: [{0}]", new object[]
				{
					_folder
				});
				var directoryInfo = new DirectoryInfo(_folder);
				defaultDir = _folder;
				string[] directories = Directory.GetDirectories(defaultDir);
				string[] files = Directory.GetFiles(defaultDir);
				if (directoryInfo.Parent != null)
				{
					string backStr = directoryInfo.Parent.FullName;
					var gameObject = backPrefab.Duplicate(viewport, backStr);
					gameObject.GetComponent<Button>().onClick.AddListener(delegate ()
					{
						UpdateBrowser(backStr, fileExtension, specificName, onSelectFile);
					});
				}
				string[] array = directories;
				for (int i = 0; i < array.Length; i++)
				{
					string folder = array[i];
					string name = new DirectoryInfo(folder).Name;
					var gameObject = folderPrefab.Duplicate(viewport, name);
					gameObject.transform.Find("Text").GetComponent<Text>().text = name;
					gameObject.GetComponent<Button>().onClick.AddListener(delegate ()
					{
						UpdateBrowser(folder, fileExtension, specificName, onSelectFile);
					});
				}
				array = files;
				for (int i = 0; i < array.Length; i++)
				{
					string fileName = array[i];
					var fileInfoFolder = new FileInfo(fileName);
					string name = fileInfoFolder.Name;
					if (fileInfoFolder.Extension.ToLower() == fileExtension.ToLower() && (specificName == "" || specificName.ToLower() + fileExtension.ToLower() == name.ToLower()))
					{
						var gameObject = filePrefab.Duplicate(viewport, name);
						gameObject.transform.Find("Text").GetComponent<Text>().text = name;
						var button = gameObject.GetComponent<Button>();
						button.onClick.ClearAll();
						button.onClick.AddListener(delegate ()
						{
							onSelectFile?.Invoke(fileInfoFolder.FullName);
						});
					}
				}
				folderBar.text = defaultDir;
				return;
			}
			EditorManager.inst.DisplayNotification("Folder doesn't exist.", 2f, EditorManager.NotificationType.Error);
		}

		public void UpdateBrowser(string _folder, string specificName = "", Action<string> onSelectFolder = null)
		{
			if (Directory.Exists(_folder))
			{
				title.text = $"<b>File Browser</b> (Right click on a folder to use)";

				var dir = transform.Find("folder-bar").GetComponent<InputField>();
				dir.onValueChanged.ClearAll();
				dir.onValueChanged.AddListener(delegate (string _val)
				{
					UpdateBrowser(_val, specificName, onSelectFolder);
				});

				LSHelpers.DeleteChildren(viewport);
				Debug.LogFormat("Update Browser: [{0}]", new object[]
				{
					_folder
				});
				var directoryInfo = new DirectoryInfo(_folder);
				defaultDir = _folder;
				string[] directories = Directory.GetDirectories(defaultDir);
				if (directoryInfo.Parent != null)
				{
					string backStr = directoryInfo.Parent.FullName;
					var gameObject = backPrefab.Duplicate(viewport, backStr);
					gameObject.GetComponent<Button>().onClick.AddListener(delegate ()
					{
						UpdateBrowser(backStr, specificName, onSelectFolder);
					});
				}
				string[] array = directories;
				for (int i = 0; i < array.Length; i++)
				{
					string folder = array[i];
					string name = new DirectoryInfo(folder).Name;
					var gameObject = folderPrefab.Duplicate(viewport, name);
					gameObject.transform.Find("Text").GetComponent<Text>().text = name;
					gameObject.GetComponent<Button>().onClick.AddListener(delegate ()
					{
						UpdateBrowser(folder, specificName, onSelectFolder);
					});

					var clickable = gameObject.AddComponent<Clickable>();
					clickable.onDown = delegate (PointerEventData pointerEventData)
					{
						if (pointerEventData.button == PointerEventData.InputButton.Right)
						{
							onSelectFolder?.Invoke(folder);
						}
					};

				}
				folderBar.text = defaultDir;
				return;
			}
			EditorManager.inst.DisplayNotification("Folder doesn't exist.", 2f, EditorManager.NotificationType.Error);
		}

		public Transform viewport;

		public InputField folderBar;

		public GameObject filePrefab;

		public GameObject backPrefab;

		public GameObject folderPrefab;

		public InputField oggFileInput;

		public string defaultDir = "";

		public TextMeshProUGUI title;
	}
}
