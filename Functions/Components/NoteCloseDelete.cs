using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace EditorManagement.Functions.Components
{
    public class NoteCloseDelete : MonoBehaviour
    {
        public GameObject delete;
        public GameObject close;

        public void Init(GameObject delete, GameObject close)
        {
            this.delete = delete;
            this.close = close;
        }

        void Update()
        {
            delete?.SetActive(EditorManager.inst.editorState == EditorManager.EditorState.Intro && ProjectPlannerManager.inst.CurrentTab == 5);
            close?.SetActive(EditorManager.inst.editorState == EditorManager.EditorState.Main || ProjectPlannerManager.inst.CurrentTab != 5);
        }
    }
}
