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

using EditorManagement.Functions.Editors;

namespace EditorManagement.Functions.Components
{
    public class InputFieldHelper : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public InputField inputField;
        private bool hovered;
        public Type type = Type.Num;
        public enum Type
        {
            Num,
            String
        }

        private void Start()
        {
            if (GetComponent<InputField>())
            {
                inputField = GetComponent<InputField>();
                inputField.onEndEdit.AddListener(delegate (string _val)
                {
                    var regexPlus = new System.Text.RegularExpressions.Regex(@"(.*?)\s\+\s(.*?)");
                    var matchPlus = regexPlus.Match(_val);
                    if (matchPlus.Success && float.TryParse(matchPlus.Groups[1].ToString(), out float startPlus) && float.TryParse(matchPlus.Groups[2].ToString(), out float endPlus))
                    {
                        if (_val.Contains(matchPlus.Groups[1].ToString() + "+" + matchPlus.Groups[2].ToString()))
                            inputField.text = _val.Replace(matchPlus.Groups[1].ToString() + "+" + matchPlus.Groups[2].ToString(), (startPlus + endPlus).ToString());
                        else if (_val.Contains(matchPlus.Groups[1].ToString() + " + " + matchPlus.Groups[2].ToString()))
                            inputField.text = _val.Replace(matchPlus.Groups[1].ToString() + " + " + matchPlus.Groups[2].ToString(), (startPlus + endPlus).ToString());
                    }
                    var regexMinus = new System.Text.RegularExpressions.Regex(@"(.*?)\s-\s(.*?)");
                    var matchMinus = regexMinus.Match(_val);
                    if (matchMinus.Success && float.TryParse(matchMinus.Groups[1].ToString(), out float startMinus) && float.TryParse(matchMinus.Groups[2].ToString(), out float endMinus))
                    {
                        if (_val.Contains(matchMinus.Groups[1].ToString() + "+" + matchMinus.Groups[2].ToString()))
                            inputField.text = _val.Replace(matchMinus.Groups[1].ToString() + "-" + matchMinus.Groups[2].ToString(), (startMinus + endMinus).ToString());
                        else if (_val.Contains(matchMinus.Groups[1].ToString() + " + " + matchMinus.Groups[2].ToString()))
                            inputField.text = _val.Replace(matchMinus.Groups[1].ToString() + " - " + matchMinus.Groups[2].ToString(), (startMinus + endMinus).ToString());
                    }
                });
            }
        }

        public void OnPointerEnter(PointerEventData pointerEventData)
        {
            hovered = true;
            //RTEditor.hoveringOIF = true;
        }

        public void OnPointerExit(PointerEventData pointerEventData)
        {
            hovered = false;
            RTEditor.hoveringOIF = false;
        }

        private void Update()
        {
            if (hovered && inputField != null)
            {
                if (Input.GetMouseButtonDown(2))
                {
                    if (type == Type.Num)
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
                    if (type == Type.String)
                    {
                        inputField.text = Flip(inputField.text);
                    }
                }
            }
        }

        string Flip(string str)
        {
            switch (str)
            {
                case "Left":
                    {
                        return str.Replace("Left", "Right");
                    }
                case "Right":
                    {
                        return str.Replace("Right", "Left");
                    }
                case "left":
                    {
                        return str.Replace("left", "right");
                    }
                case "right":
                    {
                        return str.Replace("right", "left");
                    }
            }
            return str;
        }
    }
}
