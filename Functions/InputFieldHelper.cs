using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using LSFunctions;

namespace EditorManagement.Functions
{
    public class InputFieldHelper : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private InputField inputField;
        private bool hovered;
        private void Start()
        {
            inputField = GetComponent<InputField>();
        }

        public void OnPointerEnter(PointerEventData pointerEventData)
        {
            hovered = true;
            RTEditor.hoveringOIF = true;
        }

        public void OnPointerExit(PointerEventData pointerEventData)
        {
            hovered = false;
            RTEditor.hoveringOIF = false;
        }

        private void Update()
        {
            if (hovered)
            {
                if (Input.GetMouseButtonDown(2))
                {
                    if (float.TryParse(inputField.text, out float num))
                    {
                        num = -num;
                        inputField.text = num.ToString();
                    }
                    else
                    {
                        if (EditorManager.inst != null)
                        {
                            EditorManager.inst.DisplayNotification("Could not invert number!", 1f, EditorManager.NotificationType.Error);
                        }
                    }
                }

                if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.V) && !LSHelpers.IsUsingInputField())
                {
                    inputField.text = Clipboard.GetText();
                }
            }
        }
    }
}
