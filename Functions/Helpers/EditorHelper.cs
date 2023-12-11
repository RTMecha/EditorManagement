using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using UnityEngine.UI;

using RTFunctions.Functions;
using RTFunctions.Functions.Data;

namespace EditorManagement.Functions.Helpers
{
    public static class EditorHelper
    {
        public static void AddEditorPopup(string _name, GameObject _go)
        {
            var editorPropertiesDialog = new EditorManager.EditorDialog
            {
                Dialog = _go.transform,
                Name = _name,
                Type = EditorManager.EditorDialog.DialogType.Popup
            };

            EditorManager.inst.EditorDialogs.Add(editorPropertiesDialog);
            EditorManager.inst.EditorDialogsDictionary.Add(_name, editorPropertiesDialog);
        }

        public static void AddEditorDialog(string _name, GameObject _go)
        {
            var editorPropertiesDialog = new EditorManager.EditorDialog
            {
                Dialog = _go.transform,
                Name = _name,
                Type = EditorManager.EditorDialog.DialogType.Object
            };

            EditorManager.inst.EditorDialogs.Add(editorPropertiesDialog);
            EditorManager.inst.EditorDialogsDictionary.Add(_name, editorPropertiesDialog);
        }

        public static void AddEditorDropdown(string name, string key, string dropdown, Sprite sprite, UnityEngine.Events.UnityAction unityAction)
        {
            if (!GameObject.Find($"Editor Systems/Editor GUI/sizer/main/TitleBar/{dropdown}"))
                return;

            var propWin = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/Edit/Edit Dropdown/Cut").Duplicate(GameObject.Find($"Editor Systems/Editor GUI/sizer/main/TitleBar/{dropdown}/{dropdown} Dropdown").transform, name);
            propWin.transform.Find("Text").GetComponent<Text>().text = name;
            propWin.transform.Find("Text").GetComponent<RectTransform>().sizeDelta = new Vector2(224f, 0f);
            propWin.transform.Find("Text 1").GetComponent<Text>().text = key;

            var propWinButton = propWin.GetComponent<Button>();
            propWinButton.onClick.ClearAll();
            propWinButton.onClick.AddListener(unityAction);

            propWin.SetActive(true);

            propWin.transform.Find("Image").GetComponent<Image>().sprite = sprite;

        }
    }
}
