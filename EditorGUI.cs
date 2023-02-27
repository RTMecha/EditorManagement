using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

namespace EditorManagement
{
    public class EditorGUI : MonoBehaviour
    {
        public static List<List<GameObject>> editorGUI = new List<List<GameObject>>();

        public static void CreateEditorGUI()
        {
            //Search for objects with color
            List<GameObject> list1 = (from obj in Resources.FindObjectsOfTypeAll<GameObject>()
                                      where obj.GetComponent<Image>() && obj.GetComponent<Image>().color == new Color(0.1294118f, 0.1294118f, 0.1294118f, 1f) || obj.GetComponent<Text>() && obj.GetComponent<Text>().color == new Color(0.1294118f, 0.1294118f, 0.1294118f, 1f) || obj.GetComponent<Text>() && obj.GetComponent<Text>().color == new Color(0.1294f, 0.1294f, 0.1294f, 1f) || obj.GetComponent<Image>() && obj.GetComponent<Image>().color == new Color(0.1333333f, 0.1333333f, 0.1333333f, 1f) || obj.GetComponent<Text>() && obj.GetComponent<Text>().color == new Color(0.1333333f, 0.1333333f, 0.1333333f, 1f)
                                      select obj).ToList();

            List<GameObject> list2 = (from obj in Resources.FindObjectsOfTypeAll<GameObject>()
                                      where obj.GetComponent<Image>() && obj.GetComponent<Image>().color == new Color(0.1058824f, 0.1058824f, 0.1098039f, 1f) || obj.GetComponent<Text>() && obj.GetComponent<Text>().color == new Color(0.1058824f, 0.1058824f, 0.1098039f, 1f)
                                      select obj).ToList();

            List<GameObject> list3 = (from obj in Resources.FindObjectsOfTypeAll<GameObject>()
                                      where obj.GetComponent<Image>() && obj.GetComponent<Image>().color == new Color(0.937255f, 0.9215687f, 0.937255f, 1f) || obj.GetComponent<Text>() && obj.GetComponent<Text>().color == new Color(0.937255f, 0.9215687f, 0.937255f, 1f) || obj.GetComponent<Image>() && obj.GetComponent<Image>().color == new Color(0.9294118f, 0.9137256f, 0.9294118f, 1f) || obj.GetComponent<Text>() && obj.GetComponent<Text>().color == new Color(0.9294118f, 0.9137256f, 0.9294118f, 1f)
                                      select obj).ToList();

            List<GameObject> list4 = (from obj in Resources.FindObjectsOfTypeAll<GameObject>()
                                      where obj.GetComponent<Image>() && obj.GetComponent<Image>().color == new Color(0.1882353f, 0.1882353f, 0.1882353f, 1f) || obj.GetComponent<Text>() && obj.GetComponent<Text>().color == new Color(0.1882353f, 0.1882353f, 0.1882353f, 1f)
                                      select obj).ToList();

            List<GameObject> list5 = (from obj in Resources.FindObjectsOfTypeAll<GameObject>()
                                      where obj.GetComponent<Image>() && obj.GetComponent<Image>().color == new Color(0.2431373f, 0.2431373f, 0.2588235f, 1f) || obj.GetComponent<Image>() && obj.GetComponent<Image>().color == new Color(0.2431373f, 0.2431373f, 0.2509804f, 1f)
                                      select obj).ToList();

            List<GameObject> list6 = (from obj in Resources.FindObjectsOfTypeAll<GameObject>()
                                      where obj.GetComponent<Image>() && obj.GetComponent<Image>().color == new Color(0.9333334f, 0.9176471f, 0.9333334f, 1f) || obj.GetComponent<Text>() && obj.GetComponent<Text>().color == new Color(0.9333334f, 0.9176471f, 0.9333334f, 1f) || obj.GetComponent<Image>() && obj.GetComponent<Image>().color == new Color(0.9333f, 0.9176f, 0.9333f, 1f) || obj.GetComponent<Text>() && obj.GetComponent<Text>().color == new Color(0.9333f, 0.9176f, 0.9333f, 1f) || obj.GetComponent<Image>() && obj.GetComponent<Image>().color == new Color(0.9372549f, 0.9215686f, 0.9372549f, 1f) || obj.GetComponent<Text>() && obj.GetComponent<Text>().color == new Color(0.9372549f, 0.9215686f, 0.9372549f, 1f)
                                      select obj).ToList();

            List<GameObject> list7 = (from obj in Resources.FindObjectsOfTypeAll<GameObject>()
                                      where obj.GetComponent<Image>() && obj.GetComponent<Image>().color == new Color(0.2156863f, 0.2156863f, 0.2196079f, 1f) || obj.GetComponent<Text>() && obj.GetComponent<Text>().color == new Color(0.2156863f, 0.2156863f, 0.2196079f, 1f)
                                      select obj).ToList();

            List<GameObject> list8 = (from obj in Resources.FindObjectsOfTypeAll<GameObject>()
                                       where obj.GetComponent<Image>() && obj.GetComponent<Image>().color == new Color(0.1960784f, 0.1960784f, 0.1960784f, 1f) || obj.GetComponent<Text>() && obj.GetComponent<Text>().color == new Color(0.1960784f, 0.1960784f, 0.1960784f, 1f) || obj.GetComponent<Image>() && obj.GetComponent<Image>().color == new Color(0.2196079f, 0.2196079f, 0.2235294f, 1f) || obj.GetComponent<Text>() && obj.GetComponent<Text>().color == new Color(0.2196079f, 0.2196079f, 0.2235294f, 1f)
                                      select obj).ToList();

            List<GameObject> list9 = (from obj in Resources.FindObjectsOfTypeAll<GameObject>()
                                       where obj.GetComponent<Image>() && obj.GetComponent<Image>().color == new Color(0.1215686f, 0.1215686f, 0.1215686f, 1f) || obj.GetComponent<Text>() && obj.GetComponent<Text>().color == new Color(0.1215686f, 0.1215686f, 0.1215686f, 1f)
                                       select obj).ToList();

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
                if (guiPart && guiPart.GetComponent<Image>() && guiPart.GetComponent<Image>().color != EditorPlugin.EditorGUIColor1.Value)
                {
                    guiPart.GetComponent<Image>().color = EditorPlugin.EditorGUIColor1.Value;
                }
                if (guiPart && guiPart.GetComponent<Text>() && guiPart.GetComponent<Text>().color != EditorPlugin.EditorGUIColor1.Value)
                {
                    guiPart.GetComponent<Text>().color = EditorPlugin.EditorGUIColor1.Value;
                }
            }

            foreach (var guiPart in editorGUI[1])
            {
                if (guiPart && guiPart.GetComponent<Image>() && guiPart.GetComponent<Image>().color != EditorPlugin.EditorGUIColor2.Value)
                {
                    guiPart.GetComponent<Image>().color = EditorPlugin.EditorGUIColor2.Value;
                }
                if (guiPart && guiPart.GetComponent<Text>() && guiPart.GetComponent<Text>().color != EditorPlugin.EditorGUIColor2.Value)
                {
                    guiPart.GetComponent<Text>().color = EditorPlugin.EditorGUIColor2.Value;
                }
            }

            foreach (var guiPart in editorGUI[2])
            {
                if (guiPart && guiPart.GetComponent<Image>() && guiPart.GetComponent<Image>().color != EditorPlugin.EditorGUIColor3.Value)
                {
                    guiPart.GetComponent<Image>().color = EditorPlugin.EditorGUIColor3.Value;
                }
                if (guiPart && guiPart.GetComponent<Text>() && guiPart.GetComponent<Text>().color != EditorPlugin.EditorGUIColor3.Value)
                {
                    guiPart.GetComponent<Text>().color = EditorPlugin.EditorGUIColor3.Value;
                }
            }

            foreach (var guiPart in editorGUI[3])
            {
                if (guiPart && guiPart.GetComponent<Image>() && guiPart.GetComponent<Image>().color != EditorPlugin.EditorGUIColor4.Value)
                {
                    guiPart.GetComponent<Image>().color = EditorPlugin.EditorGUIColor4.Value;
                }
                if (guiPart && guiPart.GetComponent<Text>() && guiPart.GetComponent<Text>().color != EditorPlugin.EditorGUIColor4.Value)
                {
                    guiPart.GetComponent<Text>().color = EditorPlugin.EditorGUIColor4.Value;
                }
            }

            foreach (var guiPart in editorGUI[4])
            {
                if (guiPart && guiPart.GetComponent<Image>() && guiPart.GetComponent<Image>().color != EditorPlugin.EditorGUIColor5.Value)
                {
                    guiPart.GetComponent<Image>().color = EditorPlugin.EditorGUIColor5.Value;
                }
                if (guiPart && guiPart.GetComponent<Text>() && guiPart.GetComponent<Text>().color != EditorPlugin.EditorGUIColor5.Value)
                {
                    guiPart.GetComponent<Text>().color = EditorPlugin.EditorGUIColor5.Value;
                }
            }

            foreach (var guiPart in editorGUI[5])
            {
                if (guiPart && guiPart.GetComponent<Image>() && guiPart.GetComponent<Image>().color != EditorPlugin.EditorGUIColor6.Value)
                {
                    guiPart.GetComponent<Image>().color = EditorPlugin.EditorGUIColor6.Value;
                }
                if (guiPart && guiPart.GetComponent<Text>() && guiPart.GetComponent<Text>().color != EditorPlugin.EditorGUIColor6.Value)
                {
                    guiPart.GetComponent<Text>().color = EditorPlugin.EditorGUIColor6.Value;
                }
            }

            foreach (var guiPart in editorGUI[6])
            {
                if (guiPart && guiPart.GetComponent<Image>() && guiPart.GetComponent<Image>().color != EditorPlugin.EditorGUIColor7.Value)
                {
                    guiPart.GetComponent<Image>().color = EditorPlugin.EditorGUIColor7.Value;
                }
                if (guiPart && guiPart.GetComponent<Text>() && guiPart.GetComponent<Text>().color != EditorPlugin.EditorGUIColor7.Value)
                {
                    guiPart.GetComponent<Text>().color = EditorPlugin.EditorGUIColor7.Value;
                }
            }

            foreach (var guiPart in editorGUI[7])
            {
                if (guiPart && guiPart.GetComponent<Image>() && guiPart.GetComponent<Image>().color != EditorPlugin.EditorGUIColor8.Value)
                {
                    guiPart.GetComponent<Image>().color = EditorPlugin.EditorGUIColor8.Value;
                }
                if (guiPart && guiPart.GetComponent<Text>() && guiPart.GetComponent<Text>().color != EditorPlugin.EditorGUIColor8.Value)
                {
                    guiPart.GetComponent<Text>().color = EditorPlugin.EditorGUIColor8.Value;
                }
            }

            foreach (var guiPart in editorGUI[8])
            {
                if (guiPart && guiPart.GetComponent<Image>() && guiPart.GetComponent<Image>().color != EditorPlugin.EditorGUIColor9.Value)
                {
                    guiPart.GetComponent<Image>().color = EditorPlugin.EditorGUIColor9.Value;
                }
                if (guiPart && guiPart.GetComponent<Text>() && guiPart.GetComponent<Text>().color != EditorPlugin.EditorGUIColor9.Value)
                {
                    guiPart.GetComponent<Text>().color = EditorPlugin.EditorGUIColor9.Value;
                }
            }
        }
    }
}
