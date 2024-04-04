using EditorManagement.Functions.Editors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace EditorManagement.Functions.Components
{
    /// <summary>
    /// Fallback component in case the current method is too slow.
    /// </summary>
    public class EditorThemeElement : MonoBehaviour
    {
        bool init = false;
        int lastTheme = 0;

        public EditorThemeManager.Element Element { get; set; }

        public void Init(EditorThemeManager.Element element)
        {
            Element = element;
            Element.ApplyTheme(EditorThemeManager.CurrentTheme);
            lastTheme = EditorThemeManager.currentTheme;
            init = true;
        }

        void FixedUpdate()
        {
            if (init && lastTheme != EditorThemeManager.currentTheme)
            {
                Element.ApplyTheme(EditorThemeManager.CurrentTheme);
                lastTheme = EditorThemeManager.currentTheme;
            }
        }
    }
}
