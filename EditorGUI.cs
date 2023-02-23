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

        public static void SetEditorGUI()
        {
            //Search for objects with color
            List<GameObject> list1 = (from obj in Resources.FindObjectsOfTypeAll<GameObject>()
                                      where obj.GetComponent<Image>() && obj.GetComponent<Image>().color == new Color(0.1294118f, 0.1294118f, 0.1294118f, 1f)
                                      select obj).ToList();

            List<GameObject> list2 = (from obj in Resources.FindObjectsOfTypeAll<GameObject>()
                                      where obj.GetComponent<Image>() && obj.GetComponent<Image>().color == new Color(0.1058824f, 0.1058824f, 0.1098039f, 1f)
                                      select obj).ToList();

            List<GameObject> list3 = (from obj in Resources.FindObjectsOfTypeAll<GameObject>()
                                      where obj.GetComponent<Image>() && obj.GetComponent<Image>().color == new Color(0.937255f, 0.9215687f, 0.937255f, 1f)
                                      select obj).ToList();

            List<GameObject> list4 = (from obj in Resources.FindObjectsOfTypeAll<GameObject>()
                                      where obj.GetComponent<Image>() && obj.GetComponent<Image>().color == new Color(0.1882353f, 0.1882353f, 0.1882353f, 1f)
                                      select obj).ToList();

            List<GameObject> list5 = (from obj in Resources.FindObjectsOfTypeAll<GameObject>()
                                      where obj.GetComponent<Image>() && obj.GetComponent<Image>().color == new Color(0.2431373f, 0.2431373f, 0.2588235f, 1f)
                                      select obj).ToList();

            List<GameObject> list6 = (from obj in Resources.FindObjectsOfTypeAll<GameObject>()
                                      where obj.GetComponent<Image>() && obj.GetComponent<Image>().color == new Color(0.2431373f, 0.2431373f, 0.2509804f, 1f)
                                      select obj).ToList();

            List<GameObject> list7 = (from obj in Resources.FindObjectsOfTypeAll<GameObject>()
                                      where obj.GetComponent<Image>() && obj.GetComponent<Image>().color == new Color(0.9333334f, 0.9176471f, 0.9333334f, 1f)
                                      select obj).ToList();

            List<GameObject> list8 = (from obj in Resources.FindObjectsOfTypeAll<GameObject>()
                                      where obj.GetComponent<Text>() && obj.GetComponent<Text>().color == new Color(0.937255f, 0.9215687f, 0.937255f, 1f)
                                      select obj).ToList();

            List<GameObject> list9 = (from obj in Resources.FindObjectsOfTypeAll<GameObject>()
                                      where obj.GetComponent<Image>() && obj.GetComponent<Image>().color == new Color(0.2156863f, 0.2156863f, 0.2196079f, 1f)
                                      select obj).ToList();

            List<GameObject> list10 = (from obj in Resources.FindObjectsOfTypeAll<GameObject>()
                                       where obj.GetComponent<Text>() && obj.GetComponent<Text>().color == new Color(0.1960784f, 0.1960784f, 0.1960784f, 1f)
                                       select obj).ToList<GameObject>();



            //Add GameObject Lists to editorGUI list
            editorGUI.Add(list1);
            editorGUI.Add(list2);

            //Set new GUI theme
            foreach (var gui1 in editorGUI[0])
            {
                gui1.GetComponent<Image>().color = new Color(0.1294118f, 0.1294118f, 0.1294118f, 1f);
            }

            foreach (var gui2 in editorGUI[1])
            {
                gui2.GetComponent<Image>().color = new Color(0.1058824f, 0.1058824f, 0.1098039f, 1f);
            }

            foreach (var gui2 in editorGUI[1])
            {
                gui2.GetComponent<Image>().color = new Color(0.937255f, 0.9215687f, 0.937255f, 1f);
            }
        }
    }
}
