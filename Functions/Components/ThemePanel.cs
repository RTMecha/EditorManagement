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
    public class ThemePanel : MonoBehaviour
    {
        public List<Image> Colors { get; set; } = new List<Image>();

        public Button UseButton { get; set; }
        public Button EditButton { get; set; }
        public Button DeleteButton { get; set; }

        public Text Name { get; set; }

        public void SetActive(bool active) => gameObject.SetActive(active);

        public BeatmapTheme Theme { get; set; }
    }
}
