﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

using RTFunctions.Functions;
using RTFunctions;

using Language = RTFunctions.ModLanguage;

namespace EditorManagement.Functions.Helpers
{
    public static class TooltipHelper
	{
		public static Dictionary<string, List<Tooltip>> Tooltips => new Dictionary<string, List<Tooltip>>
		{
			{ "Editor Layer", new List<Tooltip>
			{
				NewTooltip("Which layer of the editor timeline you're viewing.",
					"Scroll on the input field to quickly change between layers or type a number in it to go to that layer. " +
					"You can also click on the scrollwheel while hovering over it to show a list of layers that have objects.", lanuage: Language.English),
				NewTooltip("<font=Angsana>hdsajfakdfsanbdk'<pos=12>.",
					"Roll down ye sea of treasure", lanuage: Language.Thai),
				NewTooltip("<font=Angsana>Which layarrrggg of me treasure ye be lookin' at.",
					"Roll down ye barrel of rum for embarking down the many realms of the 7 seas. Shoot ye firearm at ye barrel of rum to unleash the booty.", lanuage: Language.Pirate),
			} },
		};

		public static void AssignTooltip(HoverTooltip hoverTooltip, string name)
		{
			if (!Tooltips.ContainsKey(name))
				return;

			hoverTooltip.tooltipLangauges.Clear();
			hoverTooltip.tooltipLangauges.AddRange(Tooltips[name]);
		}

		public static void AddTooltip(GameObject gameObject, string desc, string hint, List<string> keys = null, Language language = Language.English, bool clear = false)
		{
			var hoverTooltip = gameObject.GetComponent<HoverTooltip>() ?? gameObject.AddComponent<HoverTooltip>();

			if (clear)
				hoverTooltip.tooltipLangauges.Clear();
			hoverTooltip.tooltipLangauges.Add(NewTooltip(desc, hint, keys, language));
		}

		public static void AddTooltip(GameObject gameObject, List<HoverTooltip.Tooltip> tooltips, bool clear = true)
		{
			var hoverTooltip = gameObject.GetComponent<HoverTooltip>() ?? gameObject.AddComponent<HoverTooltip>();

			if (clear)
				hoverTooltip.tooltipLangauges = tooltips;
			else
				hoverTooltip.tooltipLangauges.AddRange(tooltips);
		}

		public static Tooltip NewTooltip(string desc, string hint, List<string> keys = null, Language lanuage = Language.English) => new Tooltip
		{
			desc = desc,
			hint = hint,
			keys = keys ?? new List<string>(),
			language = lanuage
		};

		public static HoverTooltip.Tooltip DeepCopy(HoverTooltip.Tooltip tooltip) => new HoverTooltip.Tooltip
		{
			desc = tooltip.desc,
			hint = tooltip.hint,
			keys = tooltip.keys.Clone(),
			language = tooltip.language
		};
	}

	public class Tooltip : HoverTooltip.Tooltip
    {
		public Tooltip()
        {

        }

		public Tooltip(HoverTooltip.Tooltip tooltip)
		{
			desc = tooltip.desc;
			hint = tooltip.hint;
			keys = tooltip.keys.Clone();
			language = (Language)tooltip.language;
		}

		public new Language language;

		public static Tooltip DeepCopy(Tooltip tooltip) => new Tooltip
		{
			desc = tooltip.desc,
			hint = tooltip.hint,
			keys = tooltip.keys.Clone(),
			language = tooltip.language
		};
	}
}
