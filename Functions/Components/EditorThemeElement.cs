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
        public string id;

        public EditorThemeManager.Element Element { get; set; }

        public void Init(EditorThemeManager.Element element, string id)
        {
            Element = element;
            Element.ApplyTheme(EditorThemeManager.CurrentTheme);
            lastTheme = EditorThemeManager.currentTheme;
            this.id = id;
            init = true;
        }

        void OnDestroy()
        {
            if (!string.IsNullOrEmpty(id) && EditorThemeManager.TemporaryEditorGUIElements.ContainsKey(id))
                EditorThemeManager.TemporaryEditorGUIElements.Remove(id);
        }
    }
}
