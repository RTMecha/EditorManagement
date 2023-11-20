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

using RTFunctions.Functions;

namespace EditorManagement.Functions.Components
{
    public class InputFieldHelper : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public InputField inputField;
        bool hovered;
        public Type type = Type.Num;
        public enum Type
        {
            Num,
            String
        }

        public void Init(InputField inputField, Type type)
        {
            this.inputField = inputField;
            this.type = type;
            if (inputField)
            {
                inputField.onEndEdit.ClearAll();
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

        public void OnPointerEnter(PointerEventData pointerEventData) => hovered = true;

        public void OnPointerExit(PointerEventData pointerEventData) => hovered = false;

        void Update()
        {
            if (hovered && inputField)
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
                            if (EditorManager.inst)
                                EditorManager.inst.DisplayNotification("Could not invert number!", 1f, EditorManager.NotificationType.Error);
                    }
                    if (type == Type.String)
                        inputField.text = Flip(inputField.text);
                }
            }
        }

        string Flip(string str)
        {
            string s;
            s = str.Replace("Left", "LSLeft87344874").Replace("Right", "LSRight87344874").Replace("left", "LSleft87344874").Replace("right", "LSright87344874").Replace("LEFT", "LSLEFT87344874").Replace("RIGHT", "LSRIGHT87344874");

            return s.Replace("LSLeft87344874", "Right").Replace("LSRight87344874", "Left").Replace("LSleft87344874", "right").Replace("LSright87344874", "left").Replace("LSLEFT87344874", "RIGHT").Replace("LSRIGHT87344874", "LEFT");
        }
    }
}
