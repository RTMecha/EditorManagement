using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using LSFunctions;

using RTFunctions.Functions;
using RTFunctions.Functions.Data;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;
using RTFunctions.Functions.Optimization;

using EditorManagement.Functions.Editors;
using EditorManagement.Patchers;

using BaseBeatmapObject = DataManager.GameData.BeatmapObject;
using BaseEventKeyframe = DataManager.GameData.EventKeyframe;

using ObjectSelection = ObjEditor.ObjectSelection;

namespace EditorManagement.Functions.Helpers
{
    public static class TriggerHelper
    {

		public static void AddEventTrigger(GameObject _if, List<EventTrigger.Entry> entries, bool clear = true)
		{
			if (!_if.GetComponent<EventTrigger>())
			{
				_if.AddComponent<EventTrigger>();
			}
			var et = _if.GetComponent<EventTrigger>();
			if (clear)
				et.triggers.Clear();
			foreach (var entry in entries)
				et.triggers.Add(entry);
		}


		public static EventTrigger.Entry ScrollDelta(InputField _if, float amount = 0.1f, float mutliply = 10f, float min = 0f, float max = 0f, bool multi = false)
		{
			var entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.Scroll;
			entry.callback.AddListener(delegate (BaseEventData eventData)
			{
				var pointerEventData = (PointerEventData)eventData;

				if (float.TryParse(_if.text, out float result))
				{
					if (!multi || !Input.GetKey(KeyCode.LeftShift))
					{
						// Small Amount
						bool holdingAlt = Input.GetKey(KeyCode.LeftAlt);

						// Large Amount
						bool holdingCtrl = Input.GetKey(KeyCode.LeftControl);

						if (pointerEventData.scrollDelta.y < 0f)
							result -= holdingAlt ? amount / mutliply : holdingCtrl ? amount * mutliply : amount;
						if (pointerEventData.scrollDelta.y > 0f)
							result += holdingAlt ? amount / mutliply : holdingCtrl ? amount * mutliply : amount;

						if (min != 0f && max != 0f)
							result = Mathf.Clamp(result, min, max);

						_if.text = result.ToString("f2");
					}
				}
			});
			return entry;
		}

		public static EventTrigger.Entry ScrollDeltaInt(InputField _if, int _amount = 1, int min = 0, int max = 0, bool multi = false)
		{
			var entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.Scroll;
			entry.callback.AddListener(delegate (BaseEventData eventData)
			{
				var pointerEventData = (PointerEventData)eventData;

				if (int.TryParse(_if.text, out int result))
				{
					if (!multi || !Input.GetKey(KeyCode.LeftShift))
					{
						if (pointerEventData.scrollDelta.y < 0f)
							result -= _amount * (Input.GetKey(KeyCode.LeftControl) ? 10 : 1);
						if (pointerEventData.scrollDelta.y > 0f)
							result += _amount * (Input.GetKey(KeyCode.LeftControl) ? 10 : 1);

						if (min != 0f && max != 0f)
							result = Mathf.Clamp(result, min, max);

						_if.text = result.ToString();
					}
				}
			});
			return entry;
		}

		public static EventTrigger.Entry ScrollDeltaVector2(InputField _ifX, InputField _ifY, float _amount, float _divide, List<float> clamp = null)
		{
			var entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.Scroll;
			entry.callback.AddListener(delegate (BaseEventData eventData)
			{
				var pointerEventData = (PointerEventData)eventData;
				if (Input.GetKey(KeyCode.LeftShift) && float.TryParse(_ifX.text, out float x) && float.TryParse(_ifY.text, out float y))
				{
					// Small Amount
					bool holdingAlt = Input.GetKey(KeyCode.LeftAlt);

					// Large Amount
					bool holdingCtrl = Input.GetKey(KeyCode.LeftControl);

					if (pointerEventData.scrollDelta.y < 0f)
					{
						x -= holdingAlt && !holdingCtrl ? _amount / _divide : !holdingAlt && holdingCtrl ? _amount * _divide : _amount;
						y -= holdingAlt && !holdingCtrl ? _amount / _divide : !holdingAlt && holdingCtrl ? _amount * _divide : _amount;
					}
					if (pointerEventData.scrollDelta.y > 0f)
                    {
						x += holdingAlt && !holdingCtrl ? _amount / _divide : !holdingAlt && holdingCtrl ? _amount * _divide : _amount;
						y += holdingAlt && !holdingCtrl ? _amount / _divide : !holdingAlt && holdingCtrl ? _amount * _divide : _amount;
					}

					if (clamp != null && clamp.Count > 1)
					{
						x = Mathf.Clamp(x, clamp[0], clamp[1]);
						if (clamp.Count == 2)
							y = Mathf.Clamp(y, clamp[0], clamp[1]);
						else
							y = Mathf.Clamp(y, clamp[2], clamp[3]);
					}

					_ifX.text = x.ToString("f2");
					_ifY.text = y.ToString("f2");
				}
			});
			return entry;
		}

		public static EventTrigger.Entry ScrollDeltaVector2Int(InputField _ifX, InputField _ifY, int _amount, List<int> clamp = null)
		{
			EventTrigger.Entry entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.Scroll;
			entry.callback.AddListener(delegate (BaseEventData eventData)
			{
				PointerEventData pointerEventData = (PointerEventData)eventData;
				if (Input.GetKey(KeyCode.LeftShift))
				{
					//Big
					if (Input.GetKey(KeyCode.LeftControl))
					{
						if (pointerEventData.scrollDelta.y < 0f)
						{
							int x = int.Parse(_ifX.text);
							int y = int.Parse(_ifY.text);

							x -= _amount * 10;
							y -= _amount * 10;


							if (clamp != null)
							{
								x = Mathf.Clamp(x, clamp[0], clamp[1]);
								if (clamp.Count == 2)
									y = Mathf.Clamp(y, clamp[0], clamp[1]);
								else
									y = Mathf.Clamp(y, clamp[2], clamp[3]);
							}

							_ifX.text = x.ToString();
							_ifY.text = y.ToString();
							return;
						}
						if (pointerEventData.scrollDelta.y > 0f)
						{
							int x = int.Parse(_ifX.text);
							int y = int.Parse(_ifY.text);

							x += _amount * 10;
							y += _amount * 10;

							if (clamp != null)
							{
								x = Mathf.Clamp(x, clamp[0], clamp[1]);
								if (clamp.Count == 2)
									y = Mathf.Clamp(y, clamp[0], clamp[1]);
								else
									y = Mathf.Clamp(y, clamp[2], clamp[3]);
							}

							_ifX.text = x.ToString();
							_ifY.text = y.ToString();
						}
					}

					//Normal
					if (!Input.GetKey(KeyCode.LeftControl))
					{
						if (pointerEventData.scrollDelta.y < 0f)
						{
							int x = int.Parse(_ifX.text);
							int y = int.Parse(_ifY.text);

							x -= _amount;
							y -= _amount;

							if (clamp != null)
							{
								x = Mathf.Clamp(x, clamp[0], clamp[1]);
								if (clamp.Count == 2)
									y = Mathf.Clamp(y, clamp[0], clamp[1]);
								else
									y = Mathf.Clamp(y, clamp[2], clamp[3]);
							}

							_ifX.text = x.ToString();
							_ifY.text = y.ToString();
							return;
						}
						if (pointerEventData.scrollDelta.y > 0f)
						{
							int x = int.Parse(_ifX.text);
							int y = int.Parse(_ifY.text);

							x += _amount;
							y += _amount;

							if (clamp != null)
							{
								x = Mathf.Clamp(x, clamp[0], clamp[1]);
								if (clamp.Count == 2)
									y = Mathf.Clamp(y, clamp[0], clamp[1]);
								else
									y = Mathf.Clamp(y, clamp[2], clamp[3]);
							}

							_ifX.text = x.ToString();
							_ifY.text = y.ToString();
						}
					}
				}
			});
			return entry;
		}

		public static void IncreaseDecreaseButtons(InputField _if, float _amount = 0.1f, float _divide = 10f, float min = 0f, float max = 0f, Transform t = null)
		{
			var tf = !t ? _if.transform : t;

			float num = _amount;

			var btR = tf.Find("<").GetComponent<Button>();
			var btL = tf.Find(">").GetComponent<Button>();

			btR.onClick.ClearAll();
			btR.onClick.AddListener(delegate ()
			{
				if (float.TryParse(_if.text, out float result))
				{
					result -= Input.GetKey(KeyCode.LeftAlt) ? _amount / _divide : Input.GetKey(KeyCode.LeftControl) ? _amount * _divide : _amount;

					if (min != 0f && max != 0f)
						result = Mathf.Clamp(result, min, max);

					_if.text = result.ToString();
				}
			});

			btL.onClick.ClearAll();
			btL.onClick.AddListener(delegate ()
			{
				if (float.TryParse(_if.text, out float result))
				{
					result -= Input.GetKey(KeyCode.LeftAlt) ? _amount / _divide : Input.GetKey(KeyCode.LeftControl) ? _amount * _divide : _amount;

					if (min != 0f && max != 0f)
						result = Mathf.Clamp(result, min, max);

					_if.text = result.ToString();
				}
			});

			if (tf.TryFind("<<", out Transform btLargeRTF) && btLargeRTF.gameObject.TryGetComponent(out Button btLargeR))
			{
				btLargeR.onClick.ClearAll();
				btLargeR.onClick.AddListener(delegate ()
				{
					if (float.TryParse(_if.text, out float result))
					{
						result -= (Input.GetKey(KeyCode.LeftAlt) ? _amount / _divide : Input.GetKey(KeyCode.LeftControl) ? _amount * _divide : _amount) * 10f;

						if (min != 0f && max != 0f)
							result = Mathf.Clamp(result, min, max);

						_if.text = result.ToString();
					}
				});
			}

			if (tf.TryFind(">>", out Transform btLargeLTF) && btLargeLTF.gameObject.TryGetComponent(out Button btLargeL))
			{
				btLargeL.onClick.ClearAll();
				btLargeL.onClick.AddListener(delegate ()
				{
					if (float.TryParse(_if.text, out float result))
					{
						result += (Input.GetKey(KeyCode.LeftAlt) ? _amount / _divide : Input.GetKey(KeyCode.LeftControl) ? _amount * _divide : _amount) * 10f;

						if (min != 0f && max != 0f)
							result = Mathf.Clamp(result, min, max);

						_if.text = result.ToString();
					}
				});
			}
		}
		
		public static void IncreaseDecreaseButtonsInt(InputField _if, int _amount = 1, int min = 0, int max = 0, Transform t = null)
		{
			var tf = !t ? _if.transform : t;

			float num = _amount;

			var btR = tf.Find("<").GetComponent<Button>();
			var btL = tf.Find(">").GetComponent<Button>();

			btR.onClick.RemoveAllListeners();
			btR.onClick.AddListener(delegate ()
			{
				if (float.TryParse(_if.text, out float result))
				{
					result -= Input.GetKey(KeyCode.LeftControl) ? _amount * 10 : _amount;

					if (min != 0f && max != 0f)
						result = Mathf.Clamp(result, min, max);

					_if.text = result.ToString();
				}
			});

			btL.onClick.RemoveAllListeners();
			btL.onClick.AddListener(delegate ()
			{
				if (float.TryParse(_if.text, out float result))
				{
					result += Input.GetKey(KeyCode.LeftControl) ? _amount * 10 : _amount;

					if (min != 0f && max != 0f)
						result = Mathf.Clamp(result, min, max);

					_if.text = result.ToString();
				}
			});
		}

		public static void SetInteractable(bool interactable, params Selectable[] buttons)
        {
			foreach (var button in buttons)
				button.interactable = interactable;
        }

		#region Keyframes

		public static EventTrigger.Entry CreateKeyframeStartDragTrigger(BeatmapObject beatmapObject, int type, int index)
		{
			var entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.BeginDrag;
			entry.callback.AddListener(delegate (BaseEventData eventData)
			{
				if (index != 0)
				{
					//var keyframes = ObjEditor.inst.keyframeSelections;
					//if (keyframes.FindIndex(x => x.Type == _kind && x.Index == _keyframe) != -1)
					//{
					//	ObjEditor.inst.selectedKeyframeOffsets.Clear();
					//	foreach (var keyframeSelection in keyframes)
					//	{
					//		if (keyframeSelection.Index == ObjEditor.inst.currentKeyframe && keyframeSelection.Type == ObjEditor.inst.currentKeyframeKind)
					//			ObjEditor.inst.selectedKeyframeOffsets.Add(0f);
					//		else
					//			ObjEditor.inst.selectedKeyframeOffsets.Add(beatmapObject.events[keyframeSelection.Type][keyframeSelection.Index].eventTime - beatmapObject.events[ObjEditor.inst.currentKeyframeKind][ObjEditor.inst.currentKeyframe].eventTime);
					//	}
					//}

					// Used for dragging from a singular keyframe's time.
					foreach (var timelineObject in RTEditor.inst.timelineBeatmapObjectKeyframes)
                    {
						ObjEditor.inst.selectedKeyframeOffsets.Clear();
						if (timelineObject.Type == type && timelineObject.Index == index)
							ObjEditor.inst.selectedKeyframeOffsets.Add(0f);
						else
							ObjEditor.inst.selectedKeyframeOffsets.Add(beatmapObject.events[timelineObject.Type][timelineObject.Index].eventTime - beatmapObject.events[type][index].eventTime);
					}

					ObjEditor.inst.mouseOffsetXForKeyframeDrag = beatmapObject.events[type][index].eventTime - ObjEditorPatch.timeCalc();
					ObjEditor.inst.timelineKeyframesDrag = true;
				}
				else
					EditorManager.inst.DisplayNotification("Can't change time of first Keyframe", 2f, EditorManager.NotificationType.Warning, false);

				ObjectEditor.inst.SetCurrentKeyframe(beatmapObject, type, index);
			});
			return entry;
		}

		public static EventTrigger.Entry CreateKeyframeEndDragTrigger(BeatmapObject beatmapObject, int _kind, int _keyframe)
		{
			var entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.EndDrag;
			entry.callback.AddListener(delegate (BaseEventData eventData)
			{
				ObjEditorPatch.timeCalc();
				var tmp = beatmapObject.events[_kind][_keyframe];
				ObjectEditor.inst.UpdateKeyframeOrder(beatmapObject);
				ObjectEditor.inst.CreateKeyframes(beatmapObject);
				int keyframe = beatmapObject.events[_kind].FindIndex(x => x == tmp);

				ObjEditor.inst.SetCurrentKeyframe(_kind, keyframe, false, InputDataManager.inst.editorActions.MultiSelect.IsPressed);

				// Keyframes affect both physical object and timeline object.

				if (RTEditor.inst.timelineBeatmapObjects.ContainsKey(beatmapObject.id))
                {
					ObjectEditor.RenderTimelineObject(RTEditor.inst.timelineBeatmapObjects[beatmapObject.id]);
                }

				RTEditor.inst.StartCoroutine(ObjectEditor.RefreshObjectGUI(beatmapObject));
				ObjEditor.inst.timelineKeyframesDrag = false;
			});
			return entry;
		}

		#endregion

		#region Objects

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
				if (RTEditor.inst.layerType == RTEditor.LayerType.Objects)
				{
					if (Input.GetKey(KeyCode.LeftShift))
						RTEditor.inst.StartCoroutine(ObjectEditor.inst.GroupSelectObjects());
					else
						RTEditor.inst.StartCoroutine(ObjectEditor.inst.GroupSelectObjects(false));
				}
				else
				{
					bool flag = false;
					//int type = 0;
					//foreach (var list5 in EventEditor.inst.eventObjects)
					//{
					//	int index = 0;
					//	foreach (var gameObject2 in list5)
					//	{
					//		if (EditorManager.RectTransformToScreenSpace(editorManager.SelectionBoxImage.rectTransform).Overlaps(EditorManager.RectTransformToScreenSpace(gameObject2.transform.GetChild(0).GetComponent<Image>().rectTransform)) && gameObject2.activeSelf)
					//		{
					//			if (!flag)
					//			{
					//				EventEditor.inst.SetCurrentEvent(type % RTEventEditor.EventLimit, index);
					//				flag = true;
					//			}
					//			else
					//			{
					//				EventEditor.inst.AddedSelectedEvent(type % RTEventEditor.EventLimit, index);
					//			}
					//		}
					//		index++;
					//	}
					//	type++;
					//}

					foreach (var timelineObject in RTEditor.inst.timelineKeyframes)
                    {
						if (timelineObject.Image && RTFunctions.Functions.IO.RTMath.RectTransformToScreenSpace(editorManager.SelectionBoxImage.rectTransform).Overlaps(RTFunctions.Functions.IO.RTMath.RectTransformToScreenSpace(timelineObject.Image.rectTransform)))
                        {
							if (!flag)
                            {
								EventEditor.inst.SetCurrentEvent(timelineObject.Type, timelineObject.Index);
								flag = true;
                            }
							else
								EventEditor.inst.AddedSelectedEvent(timelineObject.Type, timelineObject.Index);
						}
                    }
				}
			});
			return entry;
		}

		//public static EventTrigger.Entry CreateBeatmapObjectStartDragTrigger(ObjectSelection _obj)
		//{
		//	var entry = new EventTrigger.Entry();
		//	entry.eventID = EventTriggerType.BeginDrag;
		//	entry.callback.AddListener(delegate (BaseEventData eventData)
		//	{
		//		if (ObjEditor.inst.ContainedInSelectedObjects(_obj))
		//		{
		//			foreach (ObjectSelection objectSelection in ObjEditor.inst.selectedObjects)
		//			{
		//				if (objectSelection.IsObject())
		//				{
		//					if (ObjEditor.inst.currentObjectSelection.IsObject())
		//					{
		//						objectSelection.TimeOffset = objectSelection.GetObjectData().StartTime - ObjEditor.inst.currentObjectSelection.GetObjectData().StartTime;
		//						objectSelection.BinOffset = objectSelection.GetObjectData().editorData.Bin - ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Bin;
		//					}
		//					else if (ObjEditor.inst.currentObjectSelection.IsPrefab())
		//					{
		//						objectSelection.TimeOffset = objectSelection.GetObjectData().StartTime - ObjEditor.inst.currentObjectSelection.GetPrefabObjectData().StartTime;
		//						objectSelection.BinOffset = objectSelection.GetObjectData().editorData.Bin - ObjEditor.inst.currentObjectSelection.GetPrefabObjectData().editorData.Bin;
		//					}
		//				}
		//				else if (objectSelection.IsPrefab())
		//				{
		//					if (ObjEditor.inst.currentObjectSelection.IsObject())
		//					{
		//						objectSelection.TimeOffset = objectSelection.GetPrefabObjectData().StartTime - ObjEditor.inst.currentObjectSelection.GetObjectData().StartTime;
		//						objectSelection.BinOffset = objectSelection.GetPrefabObjectData().editorData.Bin - ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Bin;
		//					}
		//					else if (ObjEditor.inst.currentObjectSelection.IsPrefab())
		//					{
		//						objectSelection.TimeOffset = objectSelection.GetPrefabObjectData().StartTime - ObjEditor.inst.currentObjectSelection.GetPrefabObjectData().StartTime;
		//						objectSelection.BinOffset = objectSelection.GetPrefabObjectData().editorData.Bin - ObjEditor.inst.currentObjectSelection.GetPrefabObjectData().editorData.Bin;
		//					}
		//				}
		//			}
		//		}
		//		ObjEditor.inst.currentObjectSelection.TimeOffset = 0f;
		//		ObjEditor.inst.currentObjectSelection.BinOffset = 0;
		//		float timelineTime = EditorManager.inst.GetTimelineTime(0f);
		//		int num = 14 - Mathf.RoundToInt((Input.mousePosition.y - 25f) * EditorManager.inst.ScreenScaleInverse / 20f);
		//		if (ObjEditor.inst.currentObjectSelection.IsObject())
		//		{
		//			ObjEditor.inst.mouseOffsetXForDrag = ObjEditor.inst.currentObjectSelection.GetObjectData().StartTime - timelineTime;
		//			ObjEditor.inst.mouseOffsetYForDrag = ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Bin - num;
		//		}
		//		else if (ObjEditor.inst.currentObjectSelection.IsPrefab())
		//		{
		//			ObjEditor.inst.mouseOffsetXForDrag = ObjEditor.inst.currentObjectSelection.GetPrefabObjectData().StartTime - timelineTime;
		//			ObjEditor.inst.mouseOffsetYForDrag = ObjEditor.inst.currentObjectSelection.GetPrefabObjectData().editorData.Bin - num;
		//		}
		//		ObjEditor.inst.beatmapObjectsDrag = true;
		//	});
		//	return entry;
		//}

		//public static EventTrigger.Entry CreateBeatmapObjectEndDragTrigger(ObjectSelection _obj)
		//{
		//	var entry = new EventTrigger.Entry();
		//	entry.eventID = EventTriggerType.EndDrag;
		//	entry.callback.AddListener(delegate (BaseEventData eventData)
		//	{
		//		ObjEditor.inst.beatmapObjectsDrag = false;
		//		foreach (ObjectSelection objectSelection in ObjEditor.inst.selectedObjects)
		//		{
		//			ObjEditor.inst.RenderTimelineObject(objectSelection);
		//			ObjectManager.inst.updateObjects(objectSelection, false);
		//		}
		//		if (ObjEditor.inst.selectedObjects.Count <= 1)
		//			RTEditor.inst.StartCoroutine(ObjectEditor.RefreshObjectGUI(ObjEditor.inst.currentObjectSelection.GetObjectData()));
		//	});
		//	return entry;
		//}

		//public static EventTrigger.Entry CreateBeatmapObjectTrigger(ObjectSelection _obj)
		//{
		//	var entry = new EventTrigger.Entry();
		//	entry.eventID = EventTriggerType.PointerUp;
		//	entry.callback.AddListener(delegate (BaseEventData eventData)
		//	{
		//		if (!ObjEditor.inst.beatmapObjectsDrag)
		//		{
		//			if (InputDataManager.inst.editorActions.MultiSelect.IsPressed)
		//			{
		//				ObjEditor.inst.AddSelectedObject(_obj);
		//			}
		//			else
		//			{
		//				ObjEditor.inst.SetCurrentObj(_obj);
		//			}
		//			foreach (ObjectSelection obj in ObjEditor.inst.selectedObjects)
		//			{
		//				ObjEditor.inst.RenderTimelineObject(obj);
		//			}
		//			float timelineTime = EditorManager.inst.GetTimelineTime(0f);
		//			if (ObjEditor.inst.currentObjectSelection.IsObject())
		//			{
		//				ObjEditor.inst.mouseOffsetXForDrag = ObjEditor.inst.currentObjectSelection.GetObjectData().StartTime - timelineTime;
		//				return;
		//			}
		//			if (ObjEditor.inst.currentObjectSelection.IsPrefab())
		//			{
		//				ObjEditor.inst.mouseOffsetXForDrag = ObjEditor.inst.currentObjectSelection.GetPrefabObjectData().StartTime - timelineTime;
		//			}
		//		}
		//	});
		//	return entry;
		//}

		public static EventTrigger.Entry CreateBeatmapObjectStartDragTrigger<T>(TimelineObject<T> timelineObject)
		{
			var entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.BeginDrag;
			entry.callback.AddListener(delegate (BaseEventData eventData)
			{
				if (timelineObject != null && RTEditor.inst.timelineBeatmapObjects.ContainsKey(timelineObject.ID))
				{
					int bin = 0;
					if (timelineObject.IsBeatmapObject)
						bin = (timelineObject.Data as BaseBeatmapObject).editorData.Bin;

					foreach (var otherTLO in RTEditor.inst.TimelineBeatmapObjects)
					{
						otherTLO.timeOffset = otherTLO.Data.StartTime - timelineObject.Time;
						otherTLO.binOffset = otherTLO.Data.editorData.Bin - bin;
					}

					foreach (var otherTLO in RTEditor.inst.TimelinePrefabObjects)
					{
						otherTLO.timeOffset = otherTLO.Data.StartTime - timelineObject.Time;
						otherTLO.binOffset = otherTLO.Data.editorData.Bin - bin;
					}

					timelineObject.timeOffset = 0f;
					timelineObject.binOffset = 0;

					float timelineTime = EditorManager.inst.GetTimelineTime(0f);
					int num = 14 - Mathf.RoundToInt((Input.mousePosition.y - 25f) * EditorManager.inst.ScreenScaleInverse / 20f);
					ObjEditor.inst.mouseOffsetXForDrag = timelineObject.Time - timelineTime;
					ObjEditor.inst.mouseOffsetYForDrag = bin - num;
					ObjEditor.inst.beatmapObjectsDrag = true;
				}
			});
			return entry;
		}

		public static EventTrigger.Entry CreateBeatmapObjectEndDragTrigger<T>(TimelineObject<T> timelineObject)
		{
			var entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.EndDrag;
			entry.callback.AddListener(delegate (BaseEventData eventData)
			{
				ObjEditor.inst.beatmapObjectsDrag = false;

				foreach (var timelineObject in RTEditor.inst.TimelineBeatmapObjects.FindAll(x => x.selected))
                {
					ObjectEditor.RenderTimelineObject(timelineObject);
					if (ObjectEditor.UpdateObjects)
						Updater.UpdateProcessor(timelineObject.Data, "Start Time");
                }
				
				foreach (var timelineObject in RTEditor.inst.TimelinePrefabObjects.FindAll(x => x.selected))
                {
					ObjectEditor.RenderTimelineObject(timelineObject);
					foreach (var bm in DataManager.inst.gameData.beatmapObjects.FindAll(x => x.prefabInstanceID == timelineObject.Data.ID))
					{
						if (ObjectEditor.UpdateObjects)
							Updater.UpdateProcessor(bm, "Start Time");
					}
                }

				if (RTEditor.inst.TimelineBeatmapObjects.Count == 1 && timelineObject.IsBeatmapObject)
					RTEditor.inst.StartCoroutine(ObjectEditor.RefreshObjectGUI(timelineObject.Data as BeatmapObject));
			});
			return entry;
		}

		public static EventTrigger.Entry CreateBeatmapObjectTrigger<T>(TimelineObject<T> timelineObject)
		{
			var entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.PointerUp;
			entry.callback.AddListener(delegate (BaseEventData eventData)
			{
				if (!ObjEditor.inst.beatmapObjectsDrag)
				{
					//if (InputDataManager.inst.editorActions.MultiSelect.IsPressed)
					//{
					//	ObjEditor.inst.AddSelectedObject(_obj);
					//}
					//else
					//{
					//	ObjEditor.inst.SetCurrentObj(_obj);
					//}

					if (InputDataManager.inst.editorActions.MultiSelect.IsPressed)
						ObjectEditor.inst.AddSelectedObject(timelineObject);
					else
						ObjectEditor.inst.SetCurrentObject(timelineObject);

					//foreach (ObjectSelection obj in ObjEditor.inst.selectedObjects)
					//{
					//	ObjEditor.inst.RenderTimelineObject(obj);
					//}

					float timelineTime = EditorManager.inst.GetTimelineTime(0f);
					ObjEditor.inst.mouseOffsetXForDrag = timelineObject.Time - timelineTime;
				}
			});
			return entry;
		}

		public static EventTrigger.Entry CreateKeyframeStartDragTrigger(BeatmapObject beatmapObject, TimelineObject<EventKeyframe> timelineObject)
		{
			EventTrigger.Entry entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.BeginDrag;
			entry.callback.AddListener(delegate (BaseEventData eventData)
			{
				if (timelineObject.Index != 0)
				{
					var list = ObjEditor.inst.keyframeSelections;
					if (list.FindIndex(x => x.Type == timelineObject.Type && x.Index == timelineObject.Index) != -1)
					{
						ObjEditor.inst.selectedKeyframeOffsets.Clear();

						foreach (var otherTLO in ObjectEditor.inst.SelectedBeatmapObjectKeyframes)
                        {
							if (otherTLO.Type == ObjEditor.inst.currentKeyframeKind && otherTLO.Index == ObjEditor.inst.currentKeyframe)
								ObjEditor.inst.selectedKeyframeOffsets.Add(0f);
							else
								ObjEditor.inst.selectedKeyframeOffsets.Add(otherTLO.Data.eventTime - timelineObject.Data.eventTime);
                        }
					}
					ObjEditor.inst.mouseOffsetXForKeyframeDrag = timelineObject.Data.eventTime - ObjEditorPatch.timeCalc();
					ObjEditor.inst.timelineKeyframesDrag = true;
				}
				else
				{
					EditorManager.inst.DisplayNotification("Can't change time of first Keyframe", 2f, EditorManager.NotificationType.Warning, false);
				}
				ObjEditor.inst.SetCurrentKeyframe(timelineObject.Type, timelineObject.Index, false, false);
			});
			return entry;
		}

		public static EventTrigger.Entry CreateKeyframeEndDragTrigger(BeatmapObject beatmapObject, TimelineObject<EventKeyframe> timelineObject)
		{
			EventTrigger.Entry entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.EndDrag;
			entry.callback.AddListener(delegate (BaseEventData eventData)
			{
				ObjectEditor.inst.UpdateKeyframeOrder(beatmapObject);
				ObjectEditor.inst.CreateKeyframes(beatmapObject);

				ObjectEditor.inst.SetCurrentKeyframe(beatmapObject, timelineObject.Type, timelineObject.Index, false, InputDataManager.inst.editorActions.MultiSelect.IsPressed);

				if (RTEditor.inst.timelineBeatmapObjects.ContainsKey(beatmapObject.id))
					ObjectEditor.RenderTimelineObject(RTEditor.inst.timelineBeatmapObjects[beatmapObject.id]);
				ObjectEditor.inst.StartCoroutine(ObjectEditor.RefreshObjectGUI(beatmapObject));
				ObjEditor.inst.timelineKeyframesDrag = false;
			});
			return entry;
		}

		#endregion

		#region Events

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
						// RTEditor.AddEvent(instance, _type, _event, true);
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


		#endregion

		#region Themes

		public static EventTrigger.Entry CreatePreviewClickTrigger(Image _preview, Image _dropper, InputField _hex, Color _col, string popupName = "")
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

				var colorPickerTF = EditorManager.inst.GetDialog("Color Picker").Dialog.Find("content/Color Picker");
				var colorPicker = colorPickerTF.GetComponent<ColorPicker>();

				colorPicker.SwitchCurrentColor(_col);

				var save = colorPickerTF.Find("info/hex/save").GetComponent<Button>();

				save.onClick.RemoveAllListeners();
				save.onClick.AddListener(delegate ()
				{
					EditorManager.inst.ClearPopups();
					if (!string.IsNullOrEmpty(popupName))
					{
						EditorManager.inst.ShowDialog(popupName);
					}
					double saturation;
					double num;
					LSColors.ColorToHSV(colorPicker.currentColor, out double _, out saturation, out num);
					_hex.text = colorPicker.currentHex;
					_preview.color = colorPicker.currentColor;

					if (_dropper != null)
					{
						_dropper.color = RTHelpers.InvertColorHue(RTHelpers.InvertColorValue(colorPicker.currentColor));
					}
				});
			});
			return previewClickTrigger;
		}

		#endregion
	}
}
