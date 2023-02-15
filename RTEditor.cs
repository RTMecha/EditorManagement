using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace EditorManagement
{
    public class RTEditor : MonoBehaviour
    {
        public static RTEditor inst;
        private void Awake()
        {
            if (RTEditor.inst == null)
            {
                RTEditor.inst = this;
                return;
            }
            if (RTEditor.inst != this)
            {
                UnityEngine.Object.Destroy(base.gameObject);
            }
        }
        public void AutoSaveLevel()
        {
        }
    }
}
