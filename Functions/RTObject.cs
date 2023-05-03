using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EditorManagement.Functions
{
    public class RTObject : MonoBehaviour
    {
        public bool selected;
		public bool tipEnabled;
		public string id;

        public void OnMouseEnter()
        {
            selected = true;
			if (tipEnabled)
			{
				DataManager.Language enumTmp = DataManager.inst.GetCurrentLanguageEnum();
				int num = tooltipLanguages.FindIndex((HoverTooltip.Tooltip x) => x.language == enumTmp);
				if (num != -1)
				{
					HoverTooltip.Tooltip tooltip = tooltipLanguages[num];
					EditorManager.inst.SetTooltip(tooltip.keys, tooltip.desc, tooltip.hint);
					return;
				}
				EditorManager.inst.SetTooltip(new List<string>(), "No tooltip added yet!", gameObject.name);
			}
		}

        public void OnMouseExit()
        {
            selected = false;
			if (tipEnabled)
			{
				EditorManager.inst.SetTooltipDisappear(0.5f);
			}
		}

		public List<HoverTooltip.Tooltip> tooltipLanguages = new List<HoverTooltip.Tooltip>();

		[Serializable]
		public class Tooltip
		{
			public DataManager.Language language;

			public List<string> keys = new List<string>();

			[TextArea(2, 10)]
			public string desc;

			[TextArea(2, 10)]
			public string hint;
		}
	}
}
