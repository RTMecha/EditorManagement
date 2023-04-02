using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EditorManagement.Functions.Tools
{
    public class Triggers : MonoBehaviour
    {
		public static EventTrigger.Entry ScrollDelta(InputField _if, float _amount, float _divide, bool _multi = false)
		{
			EventTrigger.Entry entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.Scroll;
			entry.callback.AddListener(delegate (BaseEventData eventData)
			{
				PointerEventData pointerEventData = (PointerEventData)eventData;
				if (!_multi)
				{
					//Small
					if (Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.LeftControl))
					{
						if (pointerEventData.scrollDelta.y < 0f)
						{
							float x = float.Parse(_if.text);
							x -= _amount / _divide;
							_if.text = x.ToString();
							return;
						}
						if (pointerEventData.scrollDelta.y > 0f)
						{
							float x = float.Parse(_if.text);
							x += _amount / _divide;
							_if.text = x.ToString();
						}
					}

					//Big
					if (!Input.GetKey(KeyCode.LeftAlt) && Input.GetKey(KeyCode.LeftControl))
					{
						if (pointerEventData.scrollDelta.y < 0f)
						{
							float x = float.Parse(_if.text);
							x -= _amount * _divide;
							_if.text = x.ToString();
							return;
						}
						if (pointerEventData.scrollDelta.y > 0f)
						{
							float x = float.Parse(_if.text);
							x += _amount * _divide;
							_if.text = x.ToString();
						}
					}

					//Normal
					if (!Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.LeftControl))
					{
						if (pointerEventData.scrollDelta.y < 0f)
						{
							float x = float.Parse(_if.text);
							x -= _amount;
							_if.text = x.ToString();
							return;
						}
						if (pointerEventData.scrollDelta.y > 0f)
						{
							float x = float.Parse(_if.text);
							x += _amount;
							_if.text = x.ToString();
						}
					}
				}
				else if (!Input.GetKey(KeyCode.LeftShift))
				{
					//Small
					if (Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.LeftControl))
					{
						if (pointerEventData.scrollDelta.y < 0f)
						{
							float x = float.Parse(_if.text);
							x -= _amount / _divide;
							_if.text = x.ToString();
							return;
						}
						if (pointerEventData.scrollDelta.y > 0f)
						{
							float x = float.Parse(_if.text);
							x += _amount / _divide;
							_if.text = x.ToString();
						}
					}

					//Big
					if (!Input.GetKey(KeyCode.LeftAlt) && Input.GetKey(KeyCode.LeftControl))
					{
						if (pointerEventData.scrollDelta.y < 0f)
						{
							float x = float.Parse(_if.text);
							x -= _amount * _divide;
							_if.text = x.ToString();
							return;
						}
						if (pointerEventData.scrollDelta.y > 0f)
						{
							float x = float.Parse(_if.text);
							x += _amount * _divide;
							_if.text = x.ToString();
						}
					}

					//Normal
					if (!Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.LeftControl))
					{
						if (pointerEventData.scrollDelta.y < 0f)
						{
							float x = float.Parse(_if.text);
							x -= _amount;
							_if.text = x.ToString();
							return;
						}
						if (pointerEventData.scrollDelta.y > 0f)
						{
							float x = float.Parse(_if.text);
							x += _amount;
							_if.text = x.ToString();
						}
					}
				}
			});
			return entry;
		}

		public static EventTrigger.Entry ScrollDeltaVector2(InputField _ifX, InputField _ifY, float _amount, float _divide)
		{
			EventTrigger.Entry entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.Scroll;
			entry.callback.AddListener(delegate (BaseEventData eventData)
			{
				PointerEventData pointerEventData = (PointerEventData)eventData;
				if (Input.GetKey(KeyCode.LeftShift))
				{
					//Small
					if (Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.LeftControl))
					{
						if (pointerEventData.scrollDelta.y < 0f)
						{
							float x = float.Parse(_ifX.text);
							float y = float.Parse(_ifY.text);
							x -= _amount / _divide;
							y -= _amount / _divide;
							_ifX.text = x.ToString();
							_ifY.text = y.ToString();
							return;
						}
						if (pointerEventData.scrollDelta.y > 0f)
						{
							float x = float.Parse(_ifX.text);
							float y = float.Parse(_ifY.text);
							x += _amount / _divide;
							y += _amount / _divide;
							_ifX.text = x.ToString();
							_ifY.text = y.ToString();
						}
					}

					//Big
					if (!Input.GetKey(KeyCode.LeftAlt) && Input.GetKey(KeyCode.LeftControl))
					{
						if (pointerEventData.scrollDelta.y < 0f)
						{
							float x = float.Parse(_ifX.text);
							float y = float.Parse(_ifY.text);
							x -= _amount * _divide;
							y -= _amount * _divide;
							_ifX.text = x.ToString();
							_ifY.text = y.ToString();
							return;
						}
						if (pointerEventData.scrollDelta.y > 0f)
						{
							float x = float.Parse(_ifX.text);
							float y = float.Parse(_ifY.text);
							x += _amount * _divide;
							y += _amount * _divide;
							_ifX.text = x.ToString();
							_ifY.text = y.ToString();
						}
					}

					//Normal
					if (!Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.LeftControl))
					{
						if (pointerEventData.scrollDelta.y < 0f)
						{
							float x = float.Parse(_ifX.text);
							float y = float.Parse(_ifY.text);
							x -= _amount;
							y -= _amount;
							_ifX.text = x.ToString();
							_ifY.text = y.ToString();
							return;
						}
						if (pointerEventData.scrollDelta.y > 0f)
						{
							float x = float.Parse(_ifX.text);
							float y = float.Parse(_ifY.text);
							x += _amount;
							y += _amount;
							_ifX.text = x.ToString();
							_ifY.text = y.ToString();
						}
					}
				}
			});
			return entry;
		}

		public static void AddTooltip(GameObject _gameObject, string _desc, string _hint, List<string> _keys = null, DataManager.Language _language = DataManager.Language.english)
        {
			if (!_gameObject.GetComponent<HoverTooltip>())
			{
				_gameObject.AddComponent<HoverTooltip>();
			}
			HoverTooltip hoverTooltip = _gameObject.GetComponent<HoverTooltip>();
			hoverTooltip.tooltipLangauges.Add(NewTooltip(_desc, _hint, _keys, _language));
		}

		public static HoverTooltip.Tooltip NewTooltip(string _desc, string _hint, List<string> _keys = null, DataManager.Language _language = DataManager.Language.english)
        {
			HoverTooltip.Tooltip tooltip = new HoverTooltip.Tooltip();
			tooltip.desc = _desc;
			tooltip.hint = _hint;

			if (_keys == null)
            {
				_keys = new List<string>();
            }

			tooltip.keys = _keys;
			tooltip.language = _language;
			return tooltip;
		}

		public static void SetTooltip(HoverTooltip.Tooltip _tooltip, string _desc, string _hint, List<string> _keys = null, DataManager.Language _language = DataManager.Language.english)
		{
			_tooltip.desc = _desc;
			_tooltip.hint = _hint;
			_tooltip.keys = _keys;
			_tooltip.language = _language;
		}
	}
}
