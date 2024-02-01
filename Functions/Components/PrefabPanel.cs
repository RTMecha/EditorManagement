using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

using RTFunctions.Functions.Data;

namespace EditorManagement.Functions.Components
{
    public class PrefabPanel : MonoBehaviour
    {
        public Button Button { get; set; }

        public Button DeleteButton { get; set; }

        public Text Name { get; set; }

        public Text TypeText { get; set; }

        public Image TypeImage { get; set; }

        public PrefabDialog Dialog { get; set; }

        public Prefab Prefab { get; set; }
    }
}
