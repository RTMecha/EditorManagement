using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

namespace EditorManagement.Functions
{
    public class ObjectModifiersEditor : MonoBehaviour
    {
        public static ObjectModifiersEditor inst;

        public static Type objectModifiersPlugin;

        public static bool installed = false;

        private void Awake()
        {
            if (!GameObject.Find("BepInEx_Manager").GetComponentByName("ObjectModifiersPlugin"))
            {
                Destroy(gameObject);
            }
            else
            {
                objectModifiersPlugin = GameObject.Find("BepInEx_Manager").GetComponentByName("ObjectModifiersPlugin").GetType();
            }

            inst = this;
        }

        public static void CreateHomingTimeline()
        {

        }

        public static void CreateHomingDialog()
        {

        }

        public static GameObject Keyframe()
        {
            return null;
        }
    }
}
