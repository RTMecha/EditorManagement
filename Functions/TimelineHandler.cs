using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace EditorManagement.Functions
{
    public class TimelineHandler : MonoBehaviour
    {
        private void OnMouseEnter()
        {
            EditorPatch.IsOverMainTimeline = true;
        }

        private void OnMouseExit()
        {
            EditorPatch.IsOverMainTimeline = false;
        }
    }
}
