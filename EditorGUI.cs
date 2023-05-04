using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

using EditorManagement.Functions;
using EditorManagement.Functions.Tools;

namespace EditorManagement
{
    public class EditorGUI : MonoBehaviour
    {
        public static List<List<GameObject>> editorGUI = new List<List<GameObject>>();

        public static void CreateEditorGUI()
        {
            //Search for objects with color
            var list1 = EditorExtensions.ColorRange(new Color(0.1294118f, 0.1294118f, 0.1294118f, 1f), 0.01f);
            var list2 = EditorExtensions.ColorRange(new Color(0.1058824f, 0.1058824f, 0.1098039f, 1f), 0.01f);
            var list3 = EditorExtensions.ColorRange(new Color(0.937255f, 0.9215687f, 0.937255f, 1f), 0.01f);
            var list4 = EditorExtensions.ColorRange(new Color(0.1882353f, 0.1882353f, 0.1882353f, 1f), 0.01f);
            var list5 = EditorExtensions.ColorRange(new Color(0.2431373f, 0.2431373f, 0.2588235f, 1f), 0.01f);
            var list6 = EditorExtensions.ColorRange(new Color(0.9333334f, 0.9176471f, 0.9333334f, 1f), 0.01f);
            var list7 = EditorExtensions.ColorRange(new Color(0.2156863f, 0.2156863f, 0.2196079f, 1f), 0.01f);
            var list8 = EditorExtensions.ColorRange(new Color(0.1960784f, 0.1960784f, 0.1960784f, 1f), 0.01f);
            var list9 = EditorExtensions.ColorRange(new Color(0.1215686f, 0.1215686f, 0.1215686f, 1f), 0.01f);

            //List<GameObject> list1 = (from obj in Resources.FindObjectsOfTypeAll<GameObject>()
            //                          where obj.GetComponent<Image>() && obj.GetComponent<Image>().color == new Color(0.1294118f, 0.1294118f, 0.1294118f, 1f) || obj.GetComponent<Text>() && obj.GetComponent<Text>().color == new Color(0.1294118f, 0.1294118f, 0.1294118f, 1f) || obj.GetComponent<Text>() && obj.GetComponent<Text>().color == new Color(0.1294f, 0.1294f, 0.1294f, 1f) || obj.GetComponent<Image>() && obj.GetComponent<Image>().color == new Color(0.1333333f, 0.1333333f, 0.1333333f, 1f) || obj.GetComponent<Text>() && obj.GetComponent<Text>().color == new Color(0.1333333f, 0.1333333f, 0.1333333f, 1f)
            //                          select obj).ToList();

            //List<GameObject> list2 = (from obj in Resources.FindObjectsOfTypeAll<GameObject>()
            //                          where obj.GetComponent<Image>() && obj.GetComponent<Image>().color == new Color(0.1058824f, 0.1058824f, 0.1098039f, 1f) || obj.GetComponent<Text>() && obj.GetComponent<Text>().color == new Color(0.1058824f, 0.1058824f, 0.1098039f, 1f)
            //                          select obj).ToList();

            //List<GameObject> list3 = (from obj in Resources.FindObjectsOfTypeAll<GameObject>()
            //                          where obj.GetComponent<Image>() && obj.GetComponent<Image>().color == new Color(0.937255f, 0.9215687f, 0.937255f, 1f) || obj.GetComponent<Text>() && obj.GetComponent<Text>().color == new Color(0.937255f, 0.9215687f, 0.937255f, 1f) || obj.GetComponent<Image>() && obj.GetComponent<Image>().color == new Color(0.9294118f, 0.9137256f, 0.9294118f, 1f) || obj.GetComponent<Text>() && obj.GetComponent<Text>().color == new Color(0.9294118f, 0.9137256f, 0.9294118f, 1f)
            //                          select obj).ToList();

            //List<GameObject> list4 = (from obj in Resources.FindObjectsOfTypeAll<GameObject>()
            //                          where obj.GetComponent<Image>() && obj.GetComponent<Image>().color == new Color(0.1882353f, 0.1882353f, 0.1882353f, 1f) || obj.GetComponent<Text>() && obj.GetComponent<Text>().color == new Color(0.1882353f, 0.1882353f, 0.1882353f, 1f)
            //                          select obj).ToList();

            //List<GameObject> list5 = (from obj in Resources.FindObjectsOfTypeAll<GameObject>()
            //                          where obj.GetComponent<Image>() && obj.GetComponent<Image>().color == new Color(0.2431373f, 0.2431373f, 0.2588235f, 1f) || obj.GetComponent<Image>() && obj.GetComponent<Image>().color == new Color(0.2431373f, 0.2431373f, 0.2509804f, 1f)
            //                          select obj).ToList();

            //List<GameObject> list6 = (from obj in Resources.FindObjectsOfTypeAll<GameObject>()
            //                          where obj.GetComponent<Image>() && obj.GetComponent<Image>().color == new Color(0.9333334f, 0.9176471f, 0.9333334f, 1f) || obj.GetComponent<Text>() && obj.GetComponent<Text>().color == new Color(0.9333334f, 0.9176471f, 0.9333334f, 1f) || obj.GetComponent<Image>() && obj.GetComponent<Image>().color == new Color(0.9333f, 0.9176f, 0.9333f, 1f) || obj.GetComponent<Text>() && obj.GetComponent<Text>().color == new Color(0.9333f, 0.9176f, 0.9333f, 1f) || obj.GetComponent<Image>() && obj.GetComponent<Image>().color == new Color(0.9372549f, 0.9215686f, 0.9372549f, 1f) || obj.GetComponent<Text>() && obj.GetComponent<Text>().color == new Color(0.9372549f, 0.9215686f, 0.9372549f, 1f)
            //                          select obj).ToList();

            //List<GameObject> list7 = (from obj in Resources.FindObjectsOfTypeAll<GameObject>()
            //                          where obj.GetComponent<Image>() && obj.GetComponent<Image>().color == new Color(0.2156863f, 0.2156863f, 0.2196079f, 1f) || obj.GetComponent<Text>() && obj.GetComponent<Text>().color == new Color(0.2156863f, 0.2156863f, 0.2196079f, 1f)
            //                          select obj).ToList();

            //List<GameObject> list8 = (from obj in Resources.FindObjectsOfTypeAll<GameObject>()
            //                           where obj.GetComponent<Image>() && obj.GetComponent<Image>().color == new Color(0.1960784f, 0.1960784f, 0.1960784f, 1f) || obj.GetComponent<Text>() && obj.GetComponent<Text>().color == new Color(0.1960784f, 0.1960784f, 0.1960784f, 1f) || obj.GetComponent<Image>() && obj.GetComponent<Image>().color == new Color(0.2196079f, 0.2196079f, 0.2235294f, 1f) || obj.GetComponent<Text>() && obj.GetComponent<Text>().color == new Color(0.2196079f, 0.2196079f, 0.2235294f, 1f)
            //                          select obj).ToList();

            //List<GameObject> list9 = (from obj in Resources.FindObjectsOfTypeAll<GameObject>()
            //                           where obj.GetComponent<Image>() && obj.GetComponent<Image>().color == new Color(0.1215686f, 0.1215686f, 0.1215686f, 1f) || obj.GetComponent<Text>() && obj.GetComponent<Text>().color == new Color(0.1215686f, 0.1215686f, 0.1215686f, 1f)
            //                           select obj).ToList();

            //Add GameObject Lists to editorGUI list
            editorGUI.Add(list1);
            editorGUI.Add(list2);
            editorGUI.Add(list3);
            editorGUI.Add(list4);
            editorGUI.Add(list5);
            editorGUI.Add(list6);
            editorGUI.Add(list7);
            editorGUI.Add(list8);
            editorGUI.Add(list9);
        }

        public static void UpdateEditorGUI()
        {
            foreach (var guiPart in editorGUI[0])
            {
                if (guiPart && guiPart.GetComponent<Image>() && guiPart.GetComponent<Image>().color != ConfigEntries.EditorGUIColor1.Value)
                {
                    guiPart.GetComponent<Image>().color = ConfigEntries.EditorGUIColor1.Value;
                }
                if (guiPart && guiPart.GetComponent<Text>() && guiPart.GetComponent<Text>().color != ConfigEntries.EditorGUIColor1.Value)
                {
                    guiPart.GetComponent<Text>().color = ConfigEntries.EditorGUIColor1.Value;
                }
            }

            foreach (var guiPart in editorGUI[1])
            {
                if (guiPart && guiPart.GetComponent<Image>() && guiPart.GetComponent<Image>().color != ConfigEntries.EditorGUIColor2.Value)
                {
                    guiPart.GetComponent<Image>().color = ConfigEntries.EditorGUIColor2.Value;
                }
                if (guiPart && guiPart.GetComponent<Text>() && guiPart.GetComponent<Text>().color != ConfigEntries.EditorGUIColor2.Value)
                {
                    guiPart.GetComponent<Text>().color = ConfigEntries.EditorGUIColor2.Value;
                }
            }

            foreach (var guiPart in editorGUI[2])
            {
                if (guiPart && guiPart.GetComponent<Image>() && guiPart.GetComponent<Image>().color != ConfigEntries.EditorGUIColor3.Value)
                {
                    guiPart.GetComponent<Image>().color = ConfigEntries.EditorGUIColor3.Value;
                }
                if (guiPart && guiPart.GetComponent<Text>() && guiPart.GetComponent<Text>().color != ConfigEntries.EditorGUIColor3.Value)
                {
                    guiPart.GetComponent<Text>().color = ConfigEntries.EditorGUIColor3.Value;
                }
            }

            foreach (var guiPart in editorGUI[3])
            {
                if (guiPart && guiPart.GetComponent<Image>() && guiPart.GetComponent<Image>().color != ConfigEntries.EditorGUIColor4.Value)
                {
                    guiPart.GetComponent<Image>().color = ConfigEntries.EditorGUIColor4.Value;
                }
                if (guiPart && guiPart.GetComponent<Text>() && guiPart.GetComponent<Text>().color != ConfigEntries.EditorGUIColor4.Value)
                {
                    guiPart.GetComponent<Text>().color = ConfigEntries.EditorGUIColor4.Value;
                }
            }

            foreach (var guiPart in editorGUI[4])
            {
                if (guiPart && guiPart.GetComponent<Image>() && guiPart.GetComponent<Image>().color != ConfigEntries.EditorGUIColor5.Value)
                {
                    guiPart.GetComponent<Image>().color = ConfigEntries.EditorGUIColor5.Value;
                }
                if (guiPart && guiPart.GetComponent<Text>() && guiPart.GetComponent<Text>().color != ConfigEntries.EditorGUIColor5.Value)
                {
                    guiPart.GetComponent<Text>().color = ConfigEntries.EditorGUIColor5.Value;
                }
            }

            foreach (var guiPart in editorGUI[5])
            {
                if (guiPart && guiPart.GetComponent<Image>() && guiPart.GetComponent<Image>().color != ConfigEntries.EditorGUIColor6.Value)
                {
                    guiPart.GetComponent<Image>().color = ConfigEntries.EditorGUIColor6.Value;
                }
                if (guiPart && guiPart.GetComponent<Text>() && guiPart.GetComponent<Text>().color != ConfigEntries.EditorGUIColor6.Value)
                {
                    guiPart.GetComponent<Text>().color = ConfigEntries.EditorGUIColor6.Value;
                }
            }

            foreach (var guiPart in editorGUI[6])
            {
                if (guiPart && guiPart.GetComponent<Image>() && guiPart.GetComponent<Image>().color != ConfigEntries.EditorGUIColor7.Value)
                {
                    guiPart.GetComponent<Image>().color = ConfigEntries.EditorGUIColor7.Value;
                }
                if (guiPart && guiPart.GetComponent<Text>() && guiPart.GetComponent<Text>().color != ConfigEntries.EditorGUIColor7.Value)
                {
                    guiPart.GetComponent<Text>().color = ConfigEntries.EditorGUIColor7.Value;
                }
            }

            foreach (var guiPart in editorGUI[7])
            {
                if (guiPart && guiPart.GetComponent<Image>() && guiPart.GetComponent<Image>().color != ConfigEntries.EditorGUIColor8.Value)
                {
                    guiPart.GetComponent<Image>().color = ConfigEntries.EditorGUIColor8.Value;
                }
                if (guiPart && guiPart.GetComponent<Text>() && guiPart.GetComponent<Text>().color != ConfigEntries.EditorGUIColor8.Value)
                {
                    guiPart.GetComponent<Text>().color = ConfigEntries.EditorGUIColor8.Value;
                }
            }

            foreach (var guiPart in editorGUI[8])
            {
                if (guiPart && guiPart.GetComponent<Image>() && guiPart.GetComponent<Image>().color != ConfigEntries.EditorGUIColor9.Value)
                {
                    guiPart.GetComponent<Image>().color = ConfigEntries.EditorGUIColor9.Value;
                }
                if (guiPart && guiPart.GetComponent<Text>() && guiPart.GetComponent<Text>().color != ConfigEntries.EditorGUIColor9.Value)
                {
                    guiPart.GetComponent<Text>().color = ConfigEntries.EditorGUIColor9.Value;
                }
            }
        }
    }
}
