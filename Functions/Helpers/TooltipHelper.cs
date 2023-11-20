using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace EditorManagement.Functions.Helpers
{
    public static class TooltipHelper
	{
		public static void AddTooltip(GameObject gameObject, string desc, string hint, List<string> keys = null, DataManager.Language language = DataManager.Language.english, bool clear = false)
		{
			var hoverTooltip = gameObject.GetComponent<HoverTooltip>();

			if (!hoverTooltip)
				hoverTooltip = gameObject.AddComponent<HoverTooltip>();

			if (clear)
				hoverTooltip.tooltipLangauges.Clear();
			hoverTooltip.tooltipLangauges.Add(NewTooltip(desc, hint, keys, language));
		}

		public static void AddTooltip(GameObject gameObject, List<HoverTooltip.Tooltip> tooltips, bool clear = true)
		{
			var hoverTooltip = gameObject.GetComponent<HoverTooltip>();

			if (!hoverTooltip)
				hoverTooltip = gameObject.AddComponent<HoverTooltip>();

			if (clear)
				hoverTooltip.tooltipLangauges = tooltips;
			else
				hoverTooltip.tooltipLangauges.AddRange(tooltips);
		}

		public static HoverTooltip.Tooltip NewTooltip(string desc, string hint, List<string> keys = null, DataManager.Language lanuage = DataManager.Language.english) => new HoverTooltip.Tooltip()
		{
			desc = desc,
			hint = hint,
			keys = keys == null ? new List<string>() : keys,
			language = lanuage
		};

		public static void SetTooltip(HoverTooltip.Tooltip _tooltip, string _desc, string _hint, List<string> _keys = null, DataManager.Language _language = DataManager.Language.english)
		{
			_tooltip.desc = _desc;
			_tooltip.hint = _hint;
			_tooltip.keys = _keys;
			_tooltip.language = _language;
		}

	}
}
