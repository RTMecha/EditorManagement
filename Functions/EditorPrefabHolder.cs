using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

namespace EditorManagement.Functions
{
    public class EditorPrefabHolder
    {
        public static EditorPrefabHolder Instance { get; set; }
        public EditorPrefabHolder()
        {
            Instance = this;
        }

        public Transform PrefabParent { get; set; }
        public GameObject StringInputField { get; set; }
        public GameObject FloatInputField { get; set; }
        public GameObject DeleteButton { get; set; }
        public GameObject FunctionButton { get; set; }
    }

    public class FloatInputFieldStorage : MonoBehaviour
    {
        [SerializeField]
        public Button leftGreaterButton;
        [SerializeField]
        public Button leftButton;
        [SerializeField]
        public Button middleButton;
        [SerializeField]
        public Button rightButton;
        [SerializeField]
        public Button rightGreaterButton;
        [SerializeField]
        public InputField inputField;
    }

    public class PrefabPanelStorage : MonoBehaviour
    {
        [SerializeField]
        public Button button;

        [SerializeField]
        public Text nameText;

        [SerializeField]
        public Text typeNameText;

        [SerializeField]
        public Image typeImage;

        [SerializeField]
        public Image typeImageShade;

        [SerializeField]
        public Image typeIconImage;

        [SerializeField]
        public Button deleteButton;
    }

    public class ThemePanelStorage : MonoBehaviour
    {
        [SerializeField]
        public Image baseImage;

        [SerializeField]
        public Button button;

        [SerializeField]
        public Image color1;
        [SerializeField]
        public Image color2;
        [SerializeField]
        public Image color3;
        [SerializeField]
        public Image color4;

        [SerializeField]
        public Text text;

        [SerializeField]
        public Button edit;
        [SerializeField]
        public Button delete;
    }
}
