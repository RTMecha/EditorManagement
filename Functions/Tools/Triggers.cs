using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using EditorManagement.Patchers;

using LSFunctions;

using DG.Tweening;
using SimpleJSON;

using HarmonyLib;

using BeatmapObject = DataManager.GameData.BeatmapObject;
using EventKeyframe = DataManager.GameData.EventKeyframe;

namespace EditorManagement.Functions.Tools
{
    public class Triggers : MonoBehaviour
    {
		public static EventTrigger.Entry ScrollDelta(InputField _if, float _amount, float _divide, bool _multi = false, List<float> clamp = null)
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

							if (clamp != null)
                            {
								x = Mathf.Clamp(x, clamp[0], clamp[1]);
                            }

							_if.text = x.ToString("f2");
							return;
						}
						if (pointerEventData.scrollDelta.y > 0f)
						{
							float x = float.Parse(_if.text);
							x += _amount / _divide;

							if (clamp != null)
							{
								x = Mathf.Clamp(x, clamp[0], clamp[1]);
							}

							_if.text = x.ToString("f2");
						}
					}

					//Big
					if (!Input.GetKey(KeyCode.LeftAlt) && Input.GetKey(KeyCode.LeftControl))
					{
						if (pointerEventData.scrollDelta.y < 0f)
						{
							float x = float.Parse(_if.text);
							x -= _amount * _divide;

							if (clamp != null)
							{
								x = Mathf.Clamp(x, clamp[0], clamp[1]);
							}

							_if.text = x.ToString("f2");
							return;
						}
						if (pointerEventData.scrollDelta.y > 0f)
						{
							float x = float.Parse(_if.text);
							x += _amount * _divide;

							if (clamp != null)
							{
								x = Mathf.Clamp(x, clamp[0], clamp[1]);
							}

							_if.text = x.ToString("f2");
						}
					}

					//Normal
					if (!Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.LeftControl))
					{
						if (pointerEventData.scrollDelta.y < 0f)
						{
							float x = float.Parse(_if.text);
							x -= _amount;

							if (clamp != null)
							{
								x = Mathf.Clamp(x, clamp[0], clamp[1]);
							}

							_if.text = x.ToString("f2");
							return;
						}
						if (pointerEventData.scrollDelta.y > 0f)
						{
							float x = float.Parse(_if.text);
							x += _amount;

							if (clamp != null)
							{
								x = Mathf.Clamp(x, clamp[0], clamp[1]);
							}

							_if.text = x.ToString("f2");
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

							if (clamp != null)
							{
								x = Mathf.Clamp(x, clamp[0], clamp[1]);
							}

							_if.text = x.ToString("f2");
							return;
						}
						if (pointerEventData.scrollDelta.y > 0f)
						{
							float x = float.Parse(_if.text);
							x += _amount / _divide;
							_if.text = x.ToString("f2");
						}
					}

					//Big
					if (!Input.GetKey(KeyCode.LeftAlt) && Input.GetKey(KeyCode.LeftControl))
					{
						if (pointerEventData.scrollDelta.y < 0f)
						{
							float x = float.Parse(_if.text);
							x -= _amount * _divide;

							if (clamp != null)
							{
								x = Mathf.Clamp(x, clamp[0], clamp[1]);
							}

							_if.text = x.ToString("f2");
							return;
						}
						if (pointerEventData.scrollDelta.y > 0f)
						{
							float x = float.Parse(_if.text);
							x += _amount * _divide;

							if (clamp != null)
							{
								x = Mathf.Clamp(x, clamp[0], clamp[1]);
							}

							_if.text = x.ToString("f2");
						}
					}

					//Normal
					if (!Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.LeftControl))
					{
						if (pointerEventData.scrollDelta.y < 0f)
						{
							float x = float.Parse(_if.text);
							x -= _amount;

							if (clamp != null)
							{
								x = Mathf.Clamp(x, clamp[0], clamp[1]);
							}

							_if.text = x.ToString("f2");
							return;
						}
						if (pointerEventData.scrollDelta.y > 0f)
						{
							float x = float.Parse(_if.text);
							x += _amount;

							if (clamp != null)
							{
								x = Mathf.Clamp(x, clamp[0], clamp[1]);
							}

							_if.text = x.ToString("f2");
						}
					}
				}
			});
			return entry;
		}
		
		public static EventTrigger.Entry ScrollDeltaInt(InputField _if, int _amount, bool _multi = false, List<int> clamp = null)
		{
			EventTrigger.Entry entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.Scroll;
			entry.callback.AddListener(delegate (BaseEventData eventData)
			{
				PointerEventData pointerEventData = (PointerEventData)eventData;
				if (!_multi)
				{
					if (pointerEventData.scrollDelta.y < 0f)
					{
						int x = int.Parse(_if.text);
						x -= _amount;

						if (clamp != null)
						{
							x = Mathf.Clamp(x, clamp[0], clamp[1]);
						}

						_if.text = x.ToString();
						return;
					}
					if (pointerEventData.scrollDelta.y > 0f)
					{
						int x = int.Parse(_if.text);
						x += _amount;

						if (clamp != null)
						{
							x = Mathf.Clamp(x, clamp[0], clamp[1]);
						}

						_if.text = x.ToString();
					}
				}
				else if (!Input.GetKey(KeyCode.LeftShift))
				{
					if (!Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.LeftControl))
					{
						if (pointerEventData.scrollDelta.y < 0f)
						{
							int x = int.Parse(_if.text);
							x -= _amount;

							if (clamp != null)
							{
								x = Mathf.Clamp(x, clamp[0], clamp[1]);
							}

							_if.text = x.ToString();
							return;
						}
						if (pointerEventData.scrollDelta.y > 0f)
						{
							int x = int.Parse(_if.text);
							x += _amount;

							if (clamp != null)
							{
								x = Mathf.Clamp(x, clamp[0], clamp[1]);
							}

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
							_ifX.text = x.ToString("f2");
							_ifY.text = y.ToString("f2");
							return;
						}
						if (pointerEventData.scrollDelta.y > 0f)
						{
							float x = float.Parse(_ifX.text);
							float y = float.Parse(_ifY.text);
							x += _amount / _divide;
							y += _amount / _divide;
							_ifX.text = x.ToString("f2");
							_ifY.text = y.ToString("f2");
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
							_ifX.text = x.ToString("f2");
							_ifY.text = y.ToString("f2");
							return;
						}
						if (pointerEventData.scrollDelta.y > 0f)
						{
							float x = float.Parse(_ifX.text);
							float y = float.Parse(_ifY.text);
							x += _amount * _divide;
							y += _amount * _divide;
							_ifX.text = x.ToString("f2");
							_ifY.text = y.ToString("f2");
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
							_ifX.text = x.ToString("f2");
							_ifY.text = y.ToString("f2");
							return;
						}
						if (pointerEventData.scrollDelta.y > 0f)
						{
							float x = float.Parse(_ifX.text);
							float y = float.Parse(_ifY.text);
							x += _amount;
							y += _amount;
							_ifX.text = x.ToString("f2");
							_ifY.text = y.ToString("f2");
						}
					}
				}
			});
			return entry;
		}

		public static void IncreaseDecreaseButtons(InputField _if, float _amount, List<float> clamp = null)
        {
			var tf = _if.transform;

			var btR = tf.Find("<").GetComponent<Button>();
			var btL = tf.Find(">").GetComponent<Button>();

			btR.onClick.RemoveAllListeners();
			btR.onClick.AddListener(delegate ()
			{
				if (clamp == null)
					_if.text = (float.Parse(_if.text) - _amount).ToString();
				else
					_if.text = Mathf.Clamp(float.Parse(_if.text) - _amount, clamp[0], clamp[1]).ToString();
			});

			btL.onClick.RemoveAllListeners();
			btL.onClick.AddListener(delegate ()
			{
				if (clamp == null)
					_if.text = (float.Parse(_if.text) + _amount).ToString();
				else
					_if.text = Mathf.Clamp(float.Parse(_if.text) + _amount, clamp[0], clamp[1]).ToString();
			});
        }

		public static void IncreaseDecreaseButtons(InputField _if, int _amount, List<float> clamp = null)
        {
			var tf = _if.transform;

			var btR = tf.Find("<").GetComponent<Button>();
			var btL = tf.Find(">").GetComponent<Button>();

			btR.onClick.RemoveAllListeners();
			btR.onClick.AddListener(delegate ()
			{
				if (clamp == null)
					_if.text = (int.Parse(_if.text) - _amount).ToString();
				else
					_if.text = Mathf.Clamp(int.Parse(_if.text) - _amount, clamp[0], clamp[1]).ToString();
			});

			btL.onClick.RemoveAllListeners();
			btL.onClick.AddListener(delegate ()
			{
				if (clamp == null)
					_if.text = (int.Parse(_if.text) + _amount).ToString();
				else
					_if.text = Mathf.Clamp(int.Parse(_if.text) + _amount, clamp[0], clamp[1]).ToString();
			});
        }

		public static EventTrigger.Entry CreatePreviewClickTrigger(Transform _preview, Transform _hex, Color _col, string popupName = "")
		{
			EventTrigger.Entry previewClickTrigger = new EventTrigger.Entry();
			previewClickTrigger.eventID = EventTriggerType.PointerClick;
			previewClickTrigger.callback.AddListener(delegate (BaseEventData eventData)
			{
				EditorManager.inst.ShowDialog("Color Picker");
				if (!string.IsNullOrEmpty(popupName))
				{
					EditorManager.inst.HideDialog(popupName);
				}
				EditorManager.inst.GetDialog("Color Picker").Dialog.Find("content/Color Picker").GetComponent<ColorPicker>().SwitchCurrentColor(_col);
				EditorManager.inst.GetDialog("Color Picker").Dialog.Find("content/Color Picker/info/hex/save").GetComponent<Button>().onClick.RemoveAllListeners();
				EditorManager.inst.GetDialog("Color Picker").Dialog.Find("content/Color Picker/info/hex/save").GetComponent<Button>().onClick.AddListener(delegate ()
				{
					EditorManager.inst.ClearPopups();
					if (!string.IsNullOrEmpty(popupName))
					{
						EditorManager.inst.ShowDialog(popupName);
					}
					double saturation;
					double num;
					LSColors.ColorToHSV(EditorManager.inst.GetDialog("Color Picker").Dialog.Find("content/Color Picker").GetComponent<ColorPicker>().currentColor, out double _, out saturation, out num);
					_hex.GetComponent<InputField>().text = EditorManager.inst.GetDialog("Color Picker").Dialog.Find("content/Color Picker").GetComponent<ColorPicker>().currentHex;
					_preview.GetChild(0).GetComponent<Image>().color = saturation >= 0.5 || num <= 0.5 ? LSColors.white : LSColors.black;
				});
			});
			return previewClickTrigger;
		}

		public static EventTrigger.Entry StartDragTrigger()
        {
			var editorManager = EditorManager.inst;
			EventTrigger.Entry entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.BeginDrag;
			entry.callback.AddListener(delegate (BaseEventData eventData)
			{
				PointerEventData pointerEventData = (PointerEventData)eventData;
				editorManager.SelectionBoxImage.gameObject.SetActive(true);
				editorManager.DragStartPos = pointerEventData.position * editorManager.ScreenScaleInverse;
				editorManager.SelectionRect = default(Rect);
			});
			return entry;
        }

        public static EventTrigger.Entry DragTrigger()
        {
            var editorManager = EditorManager.inst;
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.Drag;
            entry.callback.AddListener(delegate (BaseEventData eventData)
            {
                Vector3 vector = ((PointerEventData)eventData).position * editorManager.ScreenScaleInverse;
                if (vector.x < editorManager.DragStartPos.x)
                {
                    editorManager.SelectionRect.xMin = vector.x;
                    editorManager.SelectionRect.xMax = editorManager.DragStartPos.x;
                }
                else
                {
                    editorManager.SelectionRect.xMin = editorManager.DragStartPos.x;
                    editorManager.SelectionRect.xMax = vector.x;
                }
                if (vector.y < editorManager.DragStartPos.y)
                {
                    editorManager.SelectionRect.yMin = vector.y;
                    editorManager.SelectionRect.yMax = editorManager.DragStartPos.y;
                }
                else
                {
                    editorManager.SelectionRect.yMin = editorManager.DragStartPos.y;
                    editorManager.SelectionRect.yMax = vector.y;
                }
                editorManager.SelectionBoxImage.rectTransform.offsetMin = editorManager.SelectionRect.min;
                editorManager.SelectionBoxImage.rectTransform.offsetMax = editorManager.SelectionRect.max;
            });
            return entry;
        }

		public static EventTrigger.Entry EndDragTrigger()
        {
			var editorManager = EditorManager.inst;

			EventTrigger.Entry entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.EndDrag;
			entry.callback.AddListener(delegate (BaseEventData eventData)
			{
				PointerEventData pointerEventData = (PointerEventData)eventData;
				EditorManager.inst.DragEndPos = pointerEventData.position;
				EditorManager.inst.SelectionBoxImage.gameObject.SetActive(false);
				if (EditorManager.inst.layer != 5)
				{
					if (Input.GetKey(KeyCode.LeftShift))
					{
						RTEditor.inst.StartCoroutine(RTEditor.GroupSelectObjects());
					}
					else
					{
						RTEditor.inst.StartCoroutine(RTEditor.GroupSelectObjects(false));
					}
				}
				else
				{
					bool flag = false;
					int type = 0;
					foreach (List<GameObject> list5 in EventEditor.inst.eventObjects)
					{
						int index = 0;
						foreach (GameObject gameObject2 in list5)
						{
							if (EditorManager.RectTransformToScreenSpace(editorManager.SelectionBoxImage.rectTransform).Overlaps(EditorManager.RectTransformToScreenSpace(gameObject2.transform.GetChild(0).GetComponent<Image>().rectTransform)) && gameObject2.activeSelf)
							{
								if (EventEditorPatch.eventLayer == 0)
								{
									if (!flag)
									{
										EventEditor.inst.SetCurrentEvent(type, index);
										flag = true;
									}
									else
									{
										EventEditor.inst.AddedSelectedEvent(type, index);
									}
								}
								else if (EventEditorPatch.eventLayer == 1)
								{
									if (!flag)
									{
										EventEditor.inst.SetCurrentEvent(type + 14, index);
										flag = true;
									}
									else
									{
										EventEditor.inst.AddedSelectedEvent(type + 14, index);
									}
								}
							}
							index++;
						}
						type++;
					}
				}
			});
			return entry;
		}

        public static void SyncObjects(string _id, string _field, bool _objEditor, bool _objectManager)
		{
			Debug.LogFormat("{0}2Attempting to sync {1} to selection!", EditorPlugin.className, _id);
			foreach (var objectSelection in ObjEditor.inst.selectedObjects)
			{
				if (objectSelection.IsObject())
				{
					var _beatmapObject = DataManager.inst.gameData.beatmapObjects.Find((BeatmapObject x) => x.id == _id);

					switch (_field)
                    {
						case "startTime":
                            {
								objectSelection.GetObjectData().StartTime = _beatmapObject.StartTime;
								break;
                            }
						case "name":
                            {
								objectSelection.GetObjectData().name = _beatmapObject.name;
								break;
                            }
						case "objectType":
                            {
								objectSelection.GetObjectData().objectType = _beatmapObject.objectType;
								break;
                            }
						case "autoKillType":
                            {
								objectSelection.GetObjectData().autoKillType = _beatmapObject.autoKillType;
								break;
                            }
						case "autoKillOffset":
                            {
								objectSelection.GetObjectData().autoKillOffset = _beatmapObject.autoKillOffset;
								break;
                            }
						case "parent":
                            {
								objectSelection.GetObjectData().parent = _beatmapObject.parent;
								break;
                            }
						case "parentType":
							{
								for (int i = 0; i < 3; i++)
								{
									objectSelection.GetObjectData().SetParentType(i, _beatmapObject.GetParentType(i));
								}
								break;
                            }
						case "parentOffset":
							{
								for (int i = 0; i < 3; i++)
								{
									objectSelection.GetObjectData().SetParentOffset(i, _beatmapObject.getParentOffset(i));
								}
								break;
                            }
						case "origin":
							{
								objectSelection.GetObjectData().origin = _beatmapObject.origin;
								break;
							}
						case "shape":
							{
								objectSelection.GetObjectData().shape = _beatmapObject.shape;
								objectSelection.GetObjectData().shapeOption = _beatmapObject.shapeOption;
								break;
							}
						case "text":
							{
								objectSelection.GetObjectData().text = _beatmapObject.text;
								break;
							}
						case "depth":
							{
								objectSelection.GetObjectData().Depth = _beatmapObject.Depth;
								break;
							}
                    }
					if (_objEditor)
                    {
						ObjEditor.inst.RenderTimelineObject(objectSelection);
                    }
					if (_objectManager)
                    {
						ObjectManager.inst.updateObjects(objectSelection);
                    }
				}
				if (objectSelection.IsPrefab())
				{
					RTEditor.DisplayNotification("MSS" + RTEditor.objectData.ToString() + "P", "Cannot sync prefab to object!", 1f, EditorManager.NotificationType.Error);
				}
			}
			Debug.LogFormat("{0}Synced!", EditorPlugin.className);
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

		public static void ObjEditorInputFieldValues(InputField _if, string _field, object _val, RTEditor.EditorProperty.ValueType _valueType, bool _updateObject = true, bool _updateTimeline = false, bool property = false)
        {
			_if.onValueChanged.RemoveAllListeners();
			_if.text = _val.ToString();
			_if.onValueChanged.AddListener(delegate (string _value)
			{
				if (!property)
				{
					switch (_valueType)
                    {
						case RTEditor.EditorProperty.ValueType.Bool:
							{
								ObjEditor.inst.currentObjectSelection.GetObjectData().GetType().GetField(_field).SetValue(ObjEditor.inst.currentObjectSelection.GetObjectData(), bool.Parse(_value));
								break;
                            }
						case RTEditor.EditorProperty.ValueType.Int:
							{
								ObjEditor.inst.currentObjectSelection.GetObjectData().GetType().GetField(_field).SetValue(ObjEditor.inst.currentObjectSelection.GetObjectData(), int.Parse(_value));
								break;
                            }
						case RTEditor.EditorProperty.ValueType.Float:
							{
								ObjEditor.inst.currentObjectSelection.GetObjectData().GetType().GetField(_field).SetValue(ObjEditor.inst.currentObjectSelection.GetObjectData(), float.Parse(_value));
								break;
                            }
						case RTEditor.EditorProperty.ValueType.String:
							{
								ObjEditor.inst.currentObjectSelection.GetObjectData().GetType().GetField(_field).SetValue(ObjEditor.inst.currentObjectSelection.GetObjectData(), _value);
								break;
                            }
						case RTEditor.EditorProperty.ValueType.Vector2:
							{
								ObjEditor.inst.currentObjectSelection.GetObjectData().GetType().GetField(_field).SetValue(ObjEditor.inst.currentObjectSelection.GetObjectData(), new Vector2(float.Parse(_value), float.Parse(_value)));
								break;
                            }
						case RTEditor.EditorProperty.ValueType.Enum:
							{
								ObjEditor.inst.currentObjectSelection.GetObjectData().GetType().GetField(_field).SetValue(ObjEditor.inst.currentObjectSelection.GetObjectData(), int.Parse(_value));
								break;
                            }
                    }
				}
				else
				{
					switch (_valueType)
					{
						case RTEditor.EditorProperty.ValueType.Bool:
							{
								ObjEditor.inst.currentObjectSelection.GetObjectData().GetType().GetProperty(_field).SetValue(ObjEditor.inst.currentObjectSelection.GetObjectData(), bool.Parse(_value));
								break;
							}
						case RTEditor.EditorProperty.ValueType.Int:
							{
								ObjEditor.inst.currentObjectSelection.GetObjectData().GetType().GetProperty(_field).SetValue(ObjEditor.inst.currentObjectSelection.GetObjectData(), int.Parse(_value));
								break;
							}
						case RTEditor.EditorProperty.ValueType.Float:
							{
								ObjEditor.inst.currentObjectSelection.GetObjectData().GetType().GetProperty(_field).SetValue(ObjEditor.inst.currentObjectSelection.GetObjectData(), float.Parse(_value));
								break;
							}
						case RTEditor.EditorProperty.ValueType.String:
							{
								ObjEditor.inst.currentObjectSelection.GetObjectData().GetType().GetProperty(_field).SetValue(ObjEditor.inst.currentObjectSelection.GetObjectData(), _value);
								break;
							}
						case RTEditor.EditorProperty.ValueType.Vector2:
							{
								ObjEditor.inst.currentObjectSelection.GetObjectData().GetType().GetProperty(_field).SetValue(ObjEditor.inst.currentObjectSelection.GetObjectData(), new Vector2(float.Parse(_value), float.Parse(_value)));
								break;
							}
						case RTEditor.EditorProperty.ValueType.Enum:
							{
								ObjEditor.inst.currentObjectSelection.GetObjectData().GetType().GetProperty(_field).SetValue(ObjEditor.inst.currentObjectSelection.GetObjectData(), int.Parse(_value));
								break;
							}
					}
				}
				if (_updateTimeline)
				{
					ObjEditor.inst.RenderTimelineObject(ObjEditor.inst.currentObjectSelection);
				}
				if (_updateObject)
                {
					ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                }
			});
		}

		public static void ObjEditorDropdownValues(Dropdown _dd, string _field, object _val, bool _updateObject = true, bool _updateTimeline = false)
        {
			_dd.onValueChanged.RemoveAllListeners();
			_dd.value = (int)_val;
			_dd.onValueChanged.AddListener(delegate (int _value)
			{
				ObjEditor.inst.currentObjectSelection.GetObjectData().GetType().GetField(_field).SetValue(ObjEditor.inst.currentObjectSelection.GetObjectData(), _value);
				if (_updateTimeline)
				{
					RTEditor.inst.StartCoroutine(RTEditor.RefreshObjectGUI());
					ObjEditor.inst.RenderTimelineObject(ObjEditor.inst.currentObjectSelection);
				}
				if (_updateObject)
				{
					ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
				}
			});
		}

		public static void ObjEditorParentOffset(ObjEditor.ObjectSelection _objectSelection, BeatmapObject _beatmapObject, Transform _p, int _t)
		{
			Debug.LogFormat("{0}Refresh Object GUI: Parent Offset [" + _t + "]", EditorPlugin.className);

			var parentOffset = _beatmapObject.getParentOffset(_t);

			var tog = _p.GetChild(2).GetComponent<Toggle>();
			tog.onValueChanged.RemoveAllListeners();
			tog.isOn = _beatmapObject.GetParentType(_t);
			tog.onValueChanged.AddListener(delegate (bool _value)
			{
				_beatmapObject.SetParentType(_t, _value);
				ObjectManager.inst.updateObjects(_objectSelection, false);
			});

			var pif = _p.GetChild(3).GetComponent<InputField>();
			pif.onValueChanged.RemoveAllListeners();
			pif.text = parentOffset.ToString();
			pif.onValueChanged.AddListener(delegate (string _value)
			{
				float @new;
				if (float.TryParse(_value, out @new))
				{
					_beatmapObject.SetParentOffset(_t, @new);
					ObjectManager.inst.updateObjects(_objectSelection, false);
				}
			});

			if (!_p.GetComponent<EventTrigger>())
            {
				_p.gameObject.AddComponent<EventTrigger>();
            }

			var pet = _p.GetComponent<EventTrigger>();
			pet.triggers.Clear();
			pet.triggers.Add(ScrollDelta(pif, 0.1f, 10f));

			var largeLeft = _p.Find("<<").GetComponent<Button>();
			var smallLeft = _p.Find("<").GetComponent<Button>();
			var smallRight = _p.Find(">").GetComponent<Button>();
			var largeRight = _p.Find(">>").GetComponent<Button>();

			largeLeft.onClick.RemoveAllListeners();
			smallLeft.onClick.RemoveAllListeners();
			smallRight.onClick.RemoveAllListeners();
			largeRight.onClick.RemoveAllListeners();

			largeLeft.onClick.AddListener(delegate ()
			{
				pif.text = (parentOffset - 1f).ToString();
			});

			smallLeft.onClick.AddListener(delegate ()
			{
				pif.text = (parentOffset - 0.1f).ToString();
			});
			smallRight.onClick.AddListener(delegate ()
			{
				pif.text = (parentOffset + 0.1f).ToString();
			});
			largeRight.onClick.AddListener(delegate ()
			{
				pif.text = (parentOffset + 1f).ToString();
			});
		}

		public static void ObjEditorKeyframeDialog(Transform _p, int _t, BeatmapObject _beatmapObject)
        {
			if (ObjEditor.inst.currentObjectSelection.GetObjectData().events[_t].Count > 0)
			{
				float eventTime = _beatmapObject.events[_t][ObjEditor.inst.currentKeyframe].eventTime;

				var tet = _p.Find("time").GetComponent<EventTrigger>();
				var tif = _p.Find("time/time").GetComponent<InputField>();

				tet.triggers.Clear();
				if (ObjEditor.inst.currentKeyframe != 0)
				{
					tet.triggers.Add(ScrollDelta(tif, 0.1f, 10f));
				}
				tif.onValueChanged.RemoveAllListeners();
				tif.text = _beatmapObject.events[_t][ObjEditor.inst.currentKeyframe].eventTime.ToString();
				tif.onValueChanged.AddListener(delegate (string _value)
				{
					ObjEditorPatch.SetKeyframeTime(float.Parse(_value), false);
				});
				_p.Find("time/<<").GetComponent<Button>().onClick.RemoveAllListeners();
				_p.Find("time/<").GetComponent<Button>().onClick.RemoveAllListeners();
				_p.Find("time/>").GetComponent<Button>().onClick.RemoveAllListeners();
				_p.Find("time/>>").GetComponent<Button>().onClick.RemoveAllListeners();
				_p.Find("time/<<").GetComponent<Button>().onClick.AddListener(delegate ()
				{
					float t = _beatmapObject.events[_t][ObjEditor.inst.currentKeyframe].eventTime;
					t -= 1f;
					tif.text = t.ToString();
				});
				_p.Find("time/<").GetComponent<Button>().onClick.AddListener(delegate ()
				{
					float t = _beatmapObject.events[_t][ObjEditor.inst.currentKeyframe].eventTime;
					t -= 0.1f;
					tif.text = t.ToString();
				});
				_p.Find("time/>").GetComponent<Button>().onClick.AddListener(delegate ()
				{
					float t = _beatmapObject.events[_t][ObjEditor.inst.currentKeyframe].eventTime;
					t += 0.1f;
					tif.text = t.ToString();
				});
				_p.Find("time/>>").GetComponent<Button>().onClick.AddListener(delegate ()
				{
					float t = _beatmapObject.events[_t][ObjEditor.inst.currentKeyframe].eventTime;
					t += 1f;
					tif.text = t.ToString();
				});

				_p.Find("curves_label").gameObject.SetActive(ObjEditor.inst.currentKeyframe != 0);
				_p.Find("curves").gameObject.SetActive(ObjEditor.inst.currentKeyframe != 0);
				_p.Find("curves").GetComponent<Dropdown>().onValueChanged.RemoveAllListeners();
				if (DataManager.inst.AnimationListDictionaryBack.ContainsKey(ObjEditor.inst.currentObjectSelection.GetObjectData().events[_t][ObjEditor.inst.currentKeyframe].curveType))
				{
					_p.Find("curves").GetComponent<Dropdown>().value = DataManager.inst.AnimationListDictionaryBack[ObjEditor.inst.currentObjectSelection.GetObjectData().events[_t][ObjEditor.inst.currentKeyframe].curveType];
				}
				_p.Find("curves").GetComponent<Dropdown>().onValueChanged.AddListener(delegate (int _value)
				{
					_beatmapObject.events[_t][ObjEditor.inst.currentKeyframe].curveType = DataManager.inst.AnimationListDictionary[_value];
					ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection, false);
					ObjEditorPatch.CreateKeyframes(-1);
				});

				if (_t != 3)
				{
					int limt = 1;
					if (_t != 2)
                    {
						limt = _beatmapObject.events[_t][ObjEditor.inst.currentKeyframe].eventValues.Count();
					}
					else
                    {
						limt = 1;
                    }
					for (int i = 0; i < limt; i++)
					{
						if (_p.GetChild(9).childCount > i && _p.GetChild(9).GetChild(i) != null)
						{
							var pos = _p.GetChild(9).GetChild(i);
							EventTrigger posET;
							if (_t != 2)
							{
								posET = pos.GetComponent<EventTrigger>();
							}
							else
							{
								posET = _p.GetChild(9).GetComponent<EventTrigger>();
							}
							var posIF = pos.GetComponent<InputField>();
							var posLeft = pos.Find("<").GetComponent<Button>();
							var posRight = pos.Find(">").GetComponent<Button>();

							if (!pos.GetComponent<InputFieldHelper>())
							{
								pos.gameObject.AddComponent<InputFieldHelper>();
							}

							posET.triggers.Clear();
							if (_t != 2)
							{
								Debug.LogFormat("{0}Refresh Object GUI: Keyframe " + _t + " [" + (i + 1) + "/" + limt + "]", EditorPlugin.className);
								posET.triggers.Add(ScrollDelta(posIF, 0.1f, 10f, true));
								posET.triggers.Add(ScrollDeltaVector2(_p.GetChild(9).GetChild(0).GetComponent<InputField>(), _p.GetChild(9).GetChild(1).GetComponent<InputField>(), 0.1f, 10f));
							}
							else
							{
								Debug.LogFormat("{0}Refresh Object GUI: Keyframe " + _t + " [" + (i + 1) + "/" + limt + "]", EditorPlugin.className);
								posET.triggers.Add(ScrollDelta(posIF, 15f, 3f, false));
							}

							int current = i;

							posIF.onValueChanged.RemoveAllListeners();
							posIF.text = _beatmapObject.events[_t][ObjEditor.inst.currentKeyframe].eventValues[i].ToString();
							posIF.onValueChanged.AddListener(delegate (string _value)
							{
								_beatmapObject.events[_t][ObjEditor.inst.currentKeyframe].eventValues[current] = float.Parse(_value);
								ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection, false);
							});

							posLeft.onClick.RemoveAllListeners();
							posLeft.onClick.AddListener(delegate ()
							{
								float x = _beatmapObject.events[_t][ObjEditor.inst.currentKeyframe].eventValues[current];
								if (_t != 2)
								{
									x -= 1f;
								}
								else
								{
									x -= 15f;
								}
								posIF.text = x.ToString();
							});

							posRight.onClick.RemoveAllListeners();
							posRight.onClick.AddListener(delegate ()
							{
								float x = _beatmapObject.events[_t][ObjEditor.inst.currentKeyframe].eventValues[current];
								if (_t != 2)
								{
									x += 1f;
								}
								else
								{
									x += 15f;
								}
								posIF.text = x.ToString();
							});
						}
					}

					Debug.LogFormat("{0}Refresh Object GUI: Keyframe Random Base", EditorPlugin.className);
					var randomValue = _p.GetChild(11);

					int random = _beatmapObject.events[_t][ObjEditor.inst.currentKeyframe].random;

					if (_t != 2)
					{
						for (int n = 0; n <= 3; n++)
						{
							Debug.LogFormat("{0}Refresh Object GUI: Keyframe Random Toggle [" + n + "]", EditorPlugin.className);
							int buttonTmp = (n >= 2) ? (n + 1) : n;
							var child = _p.GetChild(13).GetChild(n).GetComponent<Toggle>();
							child.onValueChanged.RemoveAllListeners();
							child.isOn = (_beatmapObject.events[_t][ObjEditor.inst.currentKeyframe].random == buttonTmp);
							child.onValueChanged.AddListener(delegate (bool _value)
							{
								if (_value)
								{
									_beatmapObject.events[_t][ObjEditor.inst.currentKeyframe].random = buttonTmp;
									_p.GetChild(10).gameObject.SetActive(buttonTmp != 0);
									_p.GetChild(11).gameObject.SetActive(buttonTmp != 0);
									_p.GetChild(10).GetChild(0).GetComponent<Text>().text = ((buttonTmp == 4) ? "Random Scale Min" : "Random X");
									if (_p.GetChild(10).childCount > 1)
									{
										_p.GetChild(10).GetChild(1).GetComponent<Text>().text = ((buttonTmp == 4) ? "Random Scale Max" : "Random Y");
									}
									_p.Find("random/interval-input").gameObject.SetActive(buttonTmp != 0 && buttonTmp != 3);
									_p.Find("r_label/interval").gameObject.SetActive(buttonTmp != 0 && buttonTmp != 3);
									ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection, false);
								}
							});
							if (!child.GetComponent<HoverUI>())
							{
								var hoverUI = child.gameObject.AddComponent<HoverUI>();
								hoverUI.animatePos = false;
								hoverUI.animateSca = true;
								hoverUI.size = 1.1f;
							}
						}
					}
					else
					{
						for (int n = 0; n <= 2; n++)
						{
							Debug.LogFormat("{0}Refresh Object GUI: Keyframe Random Toggle [" + n + "]", EditorPlugin.className);
							int buttonTmp = (n >= 2) ? (n + 1) : n;
							var child = _p.GetChild(13).GetChild(n).GetComponent<Toggle>();
							child.onValueChanged.RemoveAllListeners();
							child.isOn = (_beatmapObject.events[_t][ObjEditor.inst.currentKeyframe].random == buttonTmp);
							child.onValueChanged.AddListener(delegate (bool _value)
							{
								if (_value)
								{
									_beatmapObject.events[_t][ObjEditor.inst.currentKeyframe].random = buttonTmp;
									_p.GetChild(10).gameObject.SetActive(buttonTmp != 0);
									_p.GetChild(11).gameObject.SetActive(buttonTmp != 0);
									_p.GetChild(10).GetChild(0).GetComponent<Text>().text = ((buttonTmp == 4) ? "Random Scale Min" : "Random X");
									if (_p.GetChild(10).childCount > 1)
									{
										_p.GetChild(10).GetChild(1).GetComponent<Text>().text = ((buttonTmp == 4) ? "Random Scale Max" : "Random Y");
									}
									_p.Find("random/interval-input").gameObject.SetActive(buttonTmp != 0 && buttonTmp != 3);
									_p.Find("r_label/interval").gameObject.SetActive(buttonTmp != 0 && buttonTmp != 3);
									ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection, false);
								}
							});
							if (!child.GetComponent<HoverUI>())
							{
								var hoverUI = child.gameObject.AddComponent<HoverUI>();
								hoverUI.animatePos = false;
								hoverUI.animateSca = true;
								hoverUI.size = 1.1f;
							}
						}
					}

					Debug.LogFormat("{0}Refresh Object GUI: Keyframe Random Value [" + random + "]", EditorPlugin.className);
					float num = 0f;
					if (_beatmapObject.events[_t][ObjEditor.inst.currentKeyframe].eventRandomValues.Length > 2)
					{
						num = _beatmapObject.events[_t][ObjEditor.inst.currentKeyframe].eventRandomValues[2];
					}

					_p.GetChild(10).gameObject.SetActive(random != 0);
					_p.GetChild(10).GetChild(0).GetComponent<Text>().text = ((random == 4) ? "Random Scale Min" : "Random X");
					if (_p.GetChild(10).childCount > 1 && _p.GetChild(10).GetChild(1) != null && _p.GetChild(10).GetChild(1).GetComponent<Text>())
					{
						_p.GetChild(10).GetChild(1).GetComponent<Text>().text = ((random == 4) ? "Random Scale Max" : "Random Y");
					}
					randomValue.gameObject.SetActive(random != 0);
					bool active = random != 0 && random != 3;
					_p.Find("r_label/interval").gameObject.SetActive(active);
					_p.Find("random/interval-input").gameObject.SetActive(active);
					_p.Find("random/interval-input/x").GetComponent<Button>().onClick.RemoveAllListeners();
					_p.Find("random/interval-input/x").GetComponent<Button>().onClick.AddListener(delegate ()
					{
						_p.Find("random/interval-input").GetComponent<InputField>().text = "0";
					});
					_p.Find("random/interval-input").GetComponent<InputField>().onValueChanged.RemoveAllListeners();
					_p.Find("random/interval-input").GetComponent<InputField>().text = num.ToString();
					_p.Find("random/interval-input").GetComponent<InputField>().onValueChanged.AddListener(delegate (string _val)
					{
						_beatmapObject.events[_t][ObjEditor.inst.currentKeyframe].eventRandomValues[2] = float.Parse(_val);
						ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection, false);
					});

					for (int kf = 0; kf < _beatmapObject.events[_t][ObjEditor.inst.currentKeyframe].eventRandomValues.Count() - 1; kf++)
					{
						if (kf < randomValue.childCount && randomValue.GetChild(kf))
						{
							int index = kf;
							Debug.LogFormat("{0}Refresh Object GUI: Keyframe Random KF [" + (kf + 1) + "/" + _beatmapObject.events[_t][ObjEditor.inst.currentKeyframe].eventRandomValues.Count() + "]", EditorPlugin.className);
							var randomValueX = randomValue.GetChild(index).GetComponent<InputField>();
							randomValueX.onValueChanged.RemoveAllListeners();
							randomValueX.text = _beatmapObject.events[_t][ObjEditor.inst.currentKeyframe].eventRandomValues[index].ToString();
							randomValueX.onValueChanged.AddListener(delegate (string _value)
							{
								_beatmapObject.events[_t][ObjEditor.inst.currentKeyframe].eventRandomValues[index] = float.Parse(_value);
								ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection, false);
							});

							randomValueX.transform.Find("<").GetComponent<Button>().onClick.RemoveAllListeners();
							randomValueX.transform.Find(">").GetComponent<Button>().onClick.RemoveAllListeners();
							randomValueX.transform.Find("<").GetComponent<Button>().onClick.AddListener(delegate ()
							{
								float x = _beatmapObject.events[_t][ObjEditor.inst.currentKeyframe].eventRandomValues[index];
								x -= 1f;
								randomValueX.text = x.ToString();
							});
							randomValueX.transform.Find(">").GetComponent<Button>().onClick.AddListener(delegate ()
							{
								float x = _beatmapObject.events[_t][ObjEditor.inst.currentKeyframe].eventRandomValues[index];
								x += 1f;
								randomValueX.text = x.ToString();
							});

							if (_t != 2)
							{
								var randET = randomValue.GetChild(index).GetComponent<EventTrigger>();
								randET.triggers.Clear();
								randET.triggers.Add(ScrollDelta(randomValueX, 0.1f, 10f, true));
								randET.triggers.Add(ScrollDeltaVector2(randomValue.GetChild(0).GetComponent<InputField>(), randomValue.GetChild(1).GetComponent<InputField>(), 0.1f, 10f));
							}
							else
							{
								var randET = randomValue.GetComponent<EventTrigger>();
								randET.triggers.Clear();
								randET.triggers.Add(ScrollDelta(randomValueX, 15f, 3f));
							}
						}
					}
				}
				else
                {
					int num6 = 0;
					foreach (Toggle toggle in ObjEditor.inst.colorButtons)
					{
						toggle.onValueChanged.RemoveAllListeners();
						int tmpIndex = num6;
						if (num6 == _beatmapObject.events[_t][ObjEditor.inst.currentKeyframe].eventValues[0])
						{
							toggle.isOn = true;
						}
						else
						{
							toggle.isOn = false;
						}
						toggle.onValueChanged.AddListener(delegate (bool _value)
						{
							ObjEditorPatch.SetKeyframeColor(0, tmpIndex);
						});
						toggle.GetComponent<Image>().color = GameManager.inst.LiveTheme.GetObjColor(tmpIndex);
						if (!toggle.GetComponent<HoverUI>())
                        {
							var hoverUI = toggle.gameObject.AddComponent<HoverUI>();
							hoverUI.animatePos = false;
							hoverUI.animateSca = true;
							hoverUI.size = 1.1f;
                        }
						num6++;

						if (_p.Find("opacity"))
						{
							_p.Find("color").GetComponent<RectTransform>().sizeDelta = new Vector2(366f, 78f);

							var opacity = _p.Find("opacity/x").GetComponent<InputField>();

							opacity.onValueChanged.RemoveAllListeners();
							opacity.text = Mathf.Clamp(-_beatmapObject.events[_t][ObjEditor.inst.currentKeyframe].eventValues[1] + 1, 0f, 1f).ToString();
							opacity.onValueChanged.AddListener(delegate (string _val)
							{
								if (float.TryParse(_val, out float n))
								{
									_beatmapObject.events[_t][ObjEditor.inst.currentKeyframe].eventValues[1] = Mathf.Clamp(-n + 1, 0f, 1f);
									ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
								}
							});

							var et = _p.Find("opacity").GetComponent<EventTrigger>();
							et.triggers.Clear();
							et.triggers.Add(ScrollDelta(opacity, 0.1f, 10f, false, new List<float> { 0f, 1f }));
						}
					}
				}
				Debug.LogFormat("{0}Refresh Object GUI: Keyframes done", EditorPlugin.className);
			}
		}

		public static DataManager.BeatmapTheme CreateTheme(string _name, string _id, Color _bg, Color _gui, List<Color> _players, List<Color> _objects, List<Color> _bgs)
        {
			DataManager.BeatmapTheme beatmapTheme = new DataManager.BeatmapTheme();

			beatmapTheme.name = _name;
			beatmapTheme.id = _id;
			beatmapTheme.backgroundColor = _bg;
			beatmapTheme.guiColor = _gui;
			beatmapTheme.playerColors = _players;
			beatmapTheme.objectColors = _objects;
			beatmapTheme.backgroundColors = _bgs;

			return beatmapTheme;
        }

		public static Color InvertColorHue(Color color)
		{
			double num;
			double saturation;
			double value;
			LSColors.ColorToHSV(color, out num, out saturation, out value);
			return LSColors.ColorFromHSV(num - 180.0, saturation, value);
		}

		public static Color InvertColorValue(Color color)
		{
			double num;
			double saturation;
			double value;
			LSColors.ColorToHSV(color, out num, out saturation, out value);
			value = -value + 255;
			return LSColors.ColorFromHSV(num, saturation, value - 255);
		}

		public static float EventValuesZ1(EventKeyframe _posEvent)
		{
            BeatmapObject bo = null;
			if (DataManager.inst.gameData.beatmapObjects.Find((BeatmapObject x) => x.events[0].Contains(_posEvent)) != null)
			{
				bo = DataManager.inst.gameData.beatmapObjects.Find((BeatmapObject x) => x.events[0].Contains(_posEvent));
			}
			float z = 0.0005f * bo.Depth;
			if (_posEvent.eventValues.Length > 2 && bo != null)
			{
				float calc = _posEvent.eventValues[2] / 10f;
				z = z + calc;
			}
			return z;
		}

		public static float EventValuesZ2(EventKeyframe _posEvent)
		{
            BeatmapObject bo = null;
			if (DataManager.inst.gameData.beatmapObjects.Find((BeatmapObject x) => x.events[0].Contains(_posEvent)) != null)
			{
				bo = DataManager.inst.gameData.beatmapObjects.Find((BeatmapObject x) => x.events[0].Contains(_posEvent));
			}
			float z = 0.1f * bo.Depth;
			if (_posEvent.eventValues.Length > 2 && bo != null)
			{
				float calc = _posEvent.eventValues[2] / 10f;
				z = z + calc;
			}
			return z;
		}

		public static float DummyNumber(EventKeyframe _posEvent)
		{
			float z = 0.0005f;
			return z;
		}

		public static RotateMode EventValuesRMode(EventKeyframe _rotEvent)
        {
			if (_rotEvent.eventValues[1] == 0f)
            {
				return RotateMode.LocalAxisAdd;
            }
			if (_rotEvent.eventValues[1] == 1f)
            {
				return RotateMode.WorldAxisAdd;
            }
			if (_rotEvent.eventValues[1] == 2f)
            {
				return RotateMode.FastBeyond360;
            }
			if (_rotEvent.eventValues[1] == 3f)
            {
				return RotateMode.Fast;
            }
			return RotateMode.LocalAxisAdd;
        }

		public static void SetCollision(BeatmapObject _beatmapObject, GameObject gameObject1, Transform child)
        {
			if (_beatmapObject.objectType == (BeatmapObject.ObjectType)4)
			{
				gameObject1.tag = "Helper";
				child.tag = "Helper";
				child.GetComponent<Collider2D>().isTrigger = false;
			}
		}

		public static EventTrigger.Entry CreateEventObjectTrigger(EventEditor instance, int _type, int _event)
		{
			var entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.PointerClick;
			entry.callback.AddListener(delegate (BaseEventData eventData)
			{
				if (!instance.eventDrag)
				{
					if (InputDataManager.inst.editorActions.MultiSelect.IsPressed)
					{
						RTEditor.AddEvent(instance, _type, _event, true);
						return;
					}
					instance.SetCurrentEvent(_type, _event);
				}
			});
			return entry;
		}

		public static EventTrigger.Entry CreateEventEndDragTrigger()
		{
			var eventEndDragTrigger = new EventTrigger.Entry();
			eventEndDragTrigger.eventID = EventTriggerType.EndDrag;
			eventEndDragTrigger.callback.AddListener(eventData =>
			{
				EventEditor.inst.eventDrag = false;
				EventEditor.inst.UpdateEventOrder();
				EventManager.inst.updateEvents();
			});
			return eventEndDragTrigger;
		}

		public static EventTrigger.Entry CreateEventStartDragTrigger(EventEditor instance, int _type, int _event)
		{
			var startDragTrigger = new EventTrigger.Entry();
			startDragTrigger.eventID = EventTriggerType.BeginDrag;
			startDragTrigger.callback.AddListener(eventData =>
			{
				if (_event != 0)
				{
					if (instance.keyframeSelections.FindIndex(x => x.Type == _type && x.Index == _event) != -1)
					{
						instance.selectedKeyframeOffsets.Clear();
						foreach (var keyframeSelection in instance.keyframeSelections)
						{
							if (keyframeSelection.Index == instance.currentEvent && keyframeSelection.Type == instance.currentEventType)
								instance.selectedKeyframeOffsets.Add(0.0f);
							else
								instance.selectedKeyframeOffsets.Add(DataManager.inst.gameData.eventObjects.allEvents[keyframeSelection.Type][keyframeSelection.Index].eventTime - DataManager.inst.gameData.eventObjects.allEvents[instance.currentEventType][instance.currentEvent].eventTime);
						}
					}
					else
						instance.SetCurrentEvent(_type, _event);
					float timelineTime = EditorManager.inst.GetTimelineTime();
					instance.mouseOffsetXForDrag = DataManager.inst.gameData.eventObjects.allEvents[_type][_event].eventTime - timelineTime;
					instance.eventDrag = true;
				}
				else
					EditorManager.inst.DisplayNotification("Can't change time of first Event", 2f, EditorManager.NotificationType.Warning);
			});
			return startDragTrigger;
		}

		public static void AddEditorDialog(string _name, GameObject _go)
        {
            var editorPropertiesDialog = new EditorManager.EditorDialog
            {
                Dialog = _go.transform,
                Name = _name,
                Type = EditorManager.EditorDialog.DialogType.Popup
            };

            EditorManager.inst.EditorDialogs.Add(editorPropertiesDialog);

			var editorDialogsDictionary = AccessTools.Field(typeof(EditorManager), "EditorDialogsDictionary");

			var editorDialogsDictionaryInst = AccessTools.Field(typeof(EditorManager), "EditorDialogsDictionary").GetValue(EditorManager.inst);

			editorDialogsDictionary.GetValue(EditorManager.inst).GetType().GetMethod("Add").Invoke(editorDialogsDictionaryInst, new object[] { _name, editorPropertiesDialog });
		}

		public static KeyCode KeyCodeDownWatcher()
		{
			for (int i = 0; i < typeof(KeyCode).GetEnumNames().Length; i++)
			{
				if (Input.GetKeyDown((KeyCode)i))
				{
					return (KeyCode)i;
				}
			}
			return KeyCode.None;
		}

		public static KeyCode KeyCodeWatcher()
		{
			for (int i = 0; i < typeof(KeyCode).GetEnumNames().Length; i++)
			{
				if (Input.GetKey((KeyCode)i))
				{
					return (KeyCode)i;
				}
			}
			return KeyCode.None;
		}

		public static void Test()
		{
			foreach (var qp in quickPrefabs)
			{
				if (qp.Value[0] == KeyCode.None && Input.GetKeyDown(qp.Value[1]))
				{
					PrefabEditor.inst.AddPrefabObjectToLevel(DataManager.inst.gameData.prefabs[qp.Key]);
				}
				else if (Input.GetKey(qp.Value[0]) && Input.GetKeyDown(qp.Value[1]))
				{
					PrefabEditor.inst.AddPrefabObjectToLevel(DataManager.inst.gameData.prefabs[qp.Key]);
				}
			}
		}

		public static void AssignKeyToPrefab(int index)
		{
			if (quickPrefabs.ContainsKey(index))
			{
				quickPrefabs[index] = new List<KeyCode>
				{ KeyCode.None, KeyCode.None };

				if (Input.GetKey(KeyCodeWatcher()))
				{
					quickPrefabs[index][0] = KeyCodeWatcher();
					quickPrefabs[index][1] = KeyCodeDownWatcher();
				}
			}
		}

		public static Dictionary<int, List<KeyCode>> quickPrefabs = new Dictionary<int, List<KeyCode>>();

		public static void UpgradeSave()
        {
			string rawProfileJSON = null;
			rawProfileJSON = FileManager.inst.LoadJSONFile("beatmaps/editor/demo/level.bytes");

			JSONNode jn = JSON.Parse("{}");
			JSONNode jnOld = JSON.Parse(rawProfileJSON);

			jn["ed"]["timeline_pos"] = 0f;
			jn["level_data"]["level_version"] = jnOld["levelData"]["levelVersion"];
			jn["level_data"]["background_color"] = jnOld["levelData"]["backgroundData"]["color"];
			jn["level_data"]["follow_player"] = jnOld["levelData"]["followPlayer"];
			jn["level_data"]["show_intro"] = jnOld["levelData"]["showIntro"];

			for (int i = 0; i < jnOld["levelData"]["checkpoints"].Count; i++)
            {
				jn["checkpoints"][i]["active"] = jnOld["levelData"]["checkpoints"][i]["active"];
				jn["checkpoints"][i]["name"] = jnOld["levelData"]["checkpoints"][i]["name"];
				jn["checkpoints"][i]["t"] = jnOld["levelData"]["checkpoints"][i]["time"];
				jn["checkpoints"][i]["pos"]["x"] = jnOld["levelData"]["checkpoints"][i]["pos"]["x"];
				jn["checkpoints"][i]["pos"]["y"] = jnOld["levelData"]["checkpoints"][i]["pos"]["y"];
            }

			for (int i = 0; i < jnOld["beatmapObjects"].Count; i++)
			{
				jn["beatmap_objects"][i]["id"] = LSText.randomString(16);
				jn["beatmap_objects"][i]["p"] = "";
				jn["beatmap_objects"][i]["d"] = jnOld["beatmapObjects"][i]["layer"];
				jn["beatmap_objects"][i]["st"] = jnOld["beatmapObjects"][i]["startTime"];
				jn["beatmap_objects"][i]["name"] = jnOld["beatmapObjects"][i]["name"];
				if (jnOld["beatmapObjects"][i]["helper"] == "False")
				{
					jn["beatmap_objects"][i]["ot"] = 0;
				}
				if (jnOld["beatmapObjects"][i]["helper"] == "True")
				{
					jn["beatmap_objects"][i]["ot"] = 1;
				}
				jn["beatmap_objects"][i]["akt"] = 0;
				jn["beatmap_objects"][i]["ako"] = 0f;
				jn["beatmap_objects"][i]["o"]["x"] = jnOld["beatmapObjects"][i]["origin"]["x"];
				jn["beatmap_objects"][i]["ed"]["bin"] = jnOld["beatmapObjects"][i]["editorData"]["bin"];
				jn["beatmap_objects"][i]["ed"]["layer"] = jnOld["beatmapObjects"][i]["editorData"]["layer"];

				float eventTime = 0f;

				for (int j = 0; j < jnOld["beatmapObjects"][i]["events"].Count; j++)
				{
					eventTime += float.Parse(jnOld["beatmapObjects"][i]["events"][j]["eventTime"]);

					jn["beatmap_objects"][i]["events"]["pos"][j]["t"] = eventTime.ToString();
					jn["beatmap_objects"][i]["events"]["sca"][j]["t"] = eventTime.ToString();
					jn["beatmap_objects"][i]["events"]["rot"][j]["t"] = eventTime.ToString();
					jn["beatmap_objects"][i]["events"]["col"][j]["t"] = eventTime.ToString();

					if (j == 0)
					{
						jn["beatmap_objects"][i]["events"]["pos"][j]["x"] = "0";
						jn["beatmap_objects"][i]["events"]["pos"][j]["y"] = "0";
						jn["beatmap_objects"][i]["events"]["sca"][j]["x"] = "0";
						jn["beatmap_objects"][i]["events"]["sca"][j]["y"] = "0";
						jn["beatmap_objects"][i]["events"]["rot"][j]["x"] = "0";
						jn["beatmap_objects"][i]["events"]["col"][j]["x"] = "0";
					}
					else
					{
						jn["beatmap_objects"][i]["events"]["pos"][j]["x"] = jn["beatmap_objects"][i]["events"]["pos"][j - 1]["x"];
						jn["beatmap_objects"][i]["events"]["pos"][j]["y"] = jn["beatmap_objects"][i]["events"]["pos"][j - 1]["y"];
						jn["beatmap_objects"][i]["events"]["sca"][j]["x"] = jn["beatmap_objects"][i]["events"]["sca"][j - 1]["x"];
						jn["beatmap_objects"][i]["events"]["sca"][j]["y"] = jn["beatmap_objects"][i]["events"]["sca"][j - 1]["y"];
						jn["beatmap_objects"][i]["events"]["rot"][j]["x"] = "0";
						jn["beatmap_objects"][i]["events"]["col"][j]["x"] = jn["beatmap_objects"][i]["events"]["col"][j - 1]["x"];
					}

					for (int k = 0; k < jnOld["beatmapObjects"][i]["events"][j]["eventParts"].Count; k++)
					{
						if (jnOld["beatmapObjects"][i]["events"][j]["eventParts"][k]["kind"] == "0")
						{
							if (jnOld["beatmapObjects"][i]["events"][j]["eventParts"][k]["value0"] != null)
								jn["beatmap_objects"][i]["events"]["pos"][j]["x"] = jnOld["beatmapObjects"][i]["events"][j]["eventParts"][k]["value0"];
							else
								jn["beatmap_objects"][i]["events"]["pos"][j]["x"] = "0";
							if (jnOld["beatmapObjects"][i]["events"][j]["eventParts"][k]["value1"] != null)
								jn["beatmap_objects"][i]["events"]["pos"][j]["y"] = jnOld["beatmapObjects"][i]["events"][j]["eventParts"][k]["value1"];
							else
								jn["beatmap_objects"][i]["events"]["pos"][j]["y"] = "0";
						}

						if (jnOld["beatmapObjects"][i]["events"][j]["eventParts"][k]["kind"] == "1")
						{
							if (jnOld["beatmapObjects"][i]["events"][j]["eventParts"][k]["value0"] != null)
							{
								jn["beatmap_objects"][i]["events"]["sca"][j]["x"] = jnOld["beatmapObjects"][i]["events"][j]["eventParts"][k]["value0"];
							}
							else
							{
								jn["beatmap_objects"][i]["events"]["sca"][j]["x"] = "0";
							}

							if (jnOld["beatmapObjects"][i]["events"][j]["eventParts"][k]["value1"] != null)
							{
								jn["beatmap_objects"][i]["events"]["sca"][j]["y"] = jnOld["beatmapObjects"][i]["events"][j]["eventParts"][k]["value1"];
							}
							else
							{
								jn["beatmap_objects"][i]["events"]["sca"][j]["y"] = "0";
							}
						}

						if (jnOld["beatmapObjects"][i]["events"][j]["eventParts"][k]["kind"] == "2")
						{
							if (jnOld["beatmapObjects"][i]["events"][j]["eventParts"][k]["value0"] != null)
							{
								jn["beatmap_objects"][i]["events"]["rot"][j]["x"] = jnOld["beatmapObjects"][i]["events"][j]["eventParts"][k]["value0"];
							}
							else
							{
								jn["beatmap_objects"][i]["events"]["rot"][j]["x"] = "0";
							}
						}

						if (jnOld["beatmapObjects"][i]["events"][j]["eventParts"][k]["kind"] == "3")
						{
							if (jnOld["beatmapObjects"][i]["events"][j]["eventParts"][k]["value0"] != null)
							{
								jn["beatmap_objects"][i]["events"]["col"][j]["x"] = jnOld["beatmapObjects"][i]["events"][j]["eventParts"][k]["value0"];
							}
							else
							{
								jn["beatmap_objects"][i]["events"]["col"][j]["x"] = "0";
							}
						}
					}
				}

				jn["beatmap_objects"][i]["events"]["pos"][0]["t"] = "0";
				jn["beatmap_objects"][i]["events"]["sca"][0]["t"] = "0";
				jn["beatmap_objects"][i]["events"]["rot"][0]["t"] = "0";
				jn["beatmap_objects"][i]["events"]["col"][0]["t"] = "0";
			}

			for (int i = 0; i < jnOld["backgroundObjects"].Count; i++)
			{
				jn["bg_objects"][i]["active"] = "True";
				jn["bg_objects"][i]["name"] = jnOld["backgroundObjects"][i]["name"];
				jn["bg_objects"][i]["kind"] = jnOld["backgroundObjects"][i]["kind"];
				jn["bg_objects"][i]["pos"]["x"] = jnOld["backgroundObjects"][i]["pos"]["x"];
				jn["bg_objects"][i]["pos"]["y"] = jnOld["backgroundObjects"][i]["pos"]["y"];
				jn["bg_objects"][i]["size"]["x"] = jnOld["backgroundObjects"][i]["size"]["x"];
				jn["bg_objects"][i]["size"]["y"] = jnOld["backgroundObjects"][i]["size"]["y"];
				jn["bg_objects"][i]["rot"] = jnOld["backgroundObjects"][i]["rot"];
				jn["bg_objects"][i]["color"] = jnOld["backgroundObjects"][i]["color"];
				jn["bg_objects"][i]["layer"] = jnOld["backgroundObjects"][i]["layer"];
				jn["bg_objects"][i]["fade"] = jnOld["backgroundObjects"][i]["fade"];
				if (jnOld["backgroundObjects"][i]["reactiveSettings"]["active"] == "True")
				{
					jn["bg_objects"][i]["r_set"]["type"] = "1";
					jn["bg_objects"][i]["r_set"]["scale"] = "1";
				}
			}

			for (int i = 0; i < jnOld["eventObjects"].Count; i++)
            {
				for (int j = 0; j < jnOld["eventObjects"][i]["events"].Count; j++)
                {
					jn["events"]["pos"][i]["t"] = jnOld["eventObjects"][i]["startTime"];
					jn["events"]["zoom"][i]["t"] = jnOld["eventObjects"][i]["startTime"];
					jn["events"]["shake"][i]["t"] = jnOld["eventObjects"][i]["startTime"];

					jn["events"]["pos"][i]["x"] = "0";
					jn["events"]["pos"][i]["y"] = "0";

					jn["events"]["zoom"][i]["x"] = "20";

					jn["events"]["shake"][i]["x"] = "0";

					if (jnOld["eventObjects"][i]["events"][j]["kind"] == "0")
					{
						jn["events"]["pos"][i]["x"] = jnOld["eventObjects"][i]["events"][j]["value0"];
						jn["events"]["pos"][i]["y"] = jnOld["eventObjects"][i]["events"][j]["value1"];
					}
					if (jnOld["eventObjects"][i]["events"][j]["kind"] == "1")
					{
						jn["events"]["zoom"][i]["x"] = jnOld["eventObjects"][i]["events"][j]["value0"];
					}
					if (jnOld["eventObjects"][i]["events"][j]["kind"] == "2")
					{
						jn["events"]["rot"][i]["x"] = jnOld["eventObjects"][i]["events"][j]["value0"];
					}
					if (jnOld["eventObjects"][i]["events"][j]["kind"] == "3")
					{
						jn["events"]["shake"][i]["x"] = jnOld["eventObjects"][i]["events"][j]["value0"];
					}
				}
            }

			jn["events"]["pos"][0]["t"] = "0";
			jn["events"]["pos"][0]["x"] = "0";
			jn["events"]["pos"][0]["y"] = "0";
			jn["events"]["zoom"][0]["t"] = "0";
			jn["events"]["zoom"][0]["x"] = "20";
			jn["events"]["rot"][0]["t"] = "0";
			jn["events"]["rot"][0]["x"] = "0";
			jn["events"]["shake"][0]["t"] = "0";
			jn["events"]["shake"][0]["x"] = "0";
			jn["events"]["shake"][0]["y"] = "0";
			jn["events"]["theme"][0]["t"] = "0";
			jn["events"]["theme"][0]["x"] = "1";
			jn["events"]["chroma"][0]["t"] = "0";
			jn["events"]["chroma"][0]["x"] = "0";
			jn["events"]["bloom"][0]["t"] = "0";
			jn["events"]["bloom"][0]["x"] = "0";
			jn["events"]["vignette"][0]["t"] = "0";
			jn["events"]["vignette"][0]["x"] = "0";
			jn["events"]["vignette"][0]["y"] = "0";
			jn["events"]["vignette"][0]["z"] = "0";
			jn["events"]["vignette"][0]["x2"] = "0";
			jn["events"]["vignette"][0]["y2"] = "0";
			jn["events"]["vignette"][0]["z2"] = "0";
			jn["events"]["lens"][0]["t"] = "0";
			jn["events"]["lens"][0]["x"] = "0";
			jn["events"]["grain"][0]["t"] = "0";
			jn["events"]["grain"][0]["x"] = "0";
			jn["events"]["grain"][0]["y"] = "0";
			jn["events"]["grain"][0]["z"] = "0";

			RTFile.WriteToFile(RTFile.GetApplicationDirectory() + "beatmaps/editor/demo/level.lsb", jn.ToString());
		}
	}
}
