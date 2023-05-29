using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.Networking;

using HarmonyLib;
using BepInEx;
using BepInEx.Configuration;

using DG.Tweening;
using SimpleJSON;
using TMPro;

using LSFunctions;

using EditorManagement.Functions.Tools;
using EditorManagement.Patchers;

using MP3Sharp;

namespace EditorManagement.Functions
{
    public class RTEditor : MonoBehaviour
    {
        public static RTEditor inst;

		public static bool ienumRunning;

		public static List<string> notifications = new List<string>();
		public static Dictionary<string, HoverTooltip> tooltips = new Dictionary<string, HoverTooltip>();

		public static string propertiesSearch;

		public static bool hoveringOIF;
		public bool allowQuit = false;

		public static bool multiObjectSearch = false;
		public static ObjectData objectData = ObjectData.ST;
		public enum ObjectData
        {
			ST,
			N,
			OT,
			AKT,
			AKO,
			P,
			PT,
			PO,
			O,
			S,
			T,
			D
        }

		public static Type objectModifiersPlugin;

		private void Awake()
        {
            if (inst == null)
            {
                inst = this;
            }
            if (inst != this)
            {
                Destroy(gameObject);
            }

			if (GameObject.Find("BepInEx_Manager").GetComponentByName("ObjectModifiersPlugin"))
            {
				objectModifiersPlugin = GameObject.Find("BepInEx_Manager").GetComponentByName("ObjectModifiersPlugin").GetType();
			}
        }

		private void Update()
		{
			if (Input.GetKeyDown(ConfigEntries.EditorPropertiesKey.Value))
			{
				OpenPropertiesWindow(true);
			}
		}

		public static void Log(object _log)
        {
			Console.WriteLine(_log);
        }

		public static void LogFormat(object _log, params object[] _formats)
        {
			string logger = _log.ToString();

			foreach (var obj in _formats)
			{
				var regex = new Regex(@"{([0-9]+)}");
				var match = regex.Match(_log.ToString());
				if (match.Success)
                {
					logger = _log.ToString().Replace("{" + match.Groups[1].ToString() + "}", obj.ToString());
                }
			}

			Console.WriteLine(logger);
        }

		public static void SetAutosave()
		{
			EditorManager.inst.CancelInvoke("AutoSaveLevel");
			inst.CancelInvoke("AutoSaveLevel");
			inst.InvokeRepeating("AutoSaveLevel", ConfigEntries.AutoSaveRepeat.Value, ConfigEntries.AutoSaveRepeat.Value);
		}

		public static IEnumerator FixHelp(string _text, float _time)
		{
			EditorManager.inst.notification.transform.Find("info").gameObject.SetActive(true);
			EditorManager.inst.notification.transform.Find("info/text").GetComponent<TextMeshProUGUI>().text = _text;
			LayoutRebuilder.ForceRebuildLayoutImmediate(EditorManager.inst.notification.transform.Find("info/text").GetComponent<RectTransform>());
			LayoutRebuilder.ForceRebuildLayoutImmediate(EditorManager.inst.notification.transform.Find("info").GetComponent<RectTransform>());
			LayoutRebuilder.ForceRebuildLayoutImmediate(EditorManager.inst.notification.GetComponent<RectTransform>());
			yield return new WaitForSeconds(_time);
			EditorManager.inst.notification.transform.Find("info").gameObject.SetActive(EditorManager.inst.showHelp);
			if (EditorManager.inst.showHelp)
			{
				LayoutRebuilder.ForceRebuildLayoutImmediate(EditorManager.inst.notification.transform.Find("info/text").GetComponent<RectTransform>());
				LayoutRebuilder.ForceRebuildLayoutImmediate(EditorManager.inst.notification.transform.Find("info").GetComponent<RectTransform>());
			}
			LayoutRebuilder.ForceRebuildLayoutImmediate(EditorManager.inst.notification.GetComponent<RectTransform>());
		}

		public static IEnumerator DisplayDefaultNotification(string _text, float _time, EditorManager.NotificationType _type)
		{
			if (!ConfigEntries.EditorDebug.Value)
			{
				Debug.LogFormat("{0}Notification:\nText: " + _text + "\nTime: " + _time + "\nType: " + _type, EditorPlugin.className);
			}
			if (notifications.Count < 20)
			{
				switch (_type)
				{
					case EditorManager.NotificationType.Info:
						{
							GameObject gameObject = Instantiate(EditorManager.inst.notificationPrefabs[0], Vector3.zero, Quaternion.identity);
							Destroy(gameObject, _time);
							gameObject.transform.Find("text").GetComponent<TextMeshProUGUI>().text = _text;
							gameObject.transform.SetParent(EditorManager.inst.notification.transform);
							if (ConfigEntries.NotificationDirection.Value == Direction.Down)
							{
								gameObject.transform.SetAsFirstSibling();
							}
							gameObject.transform.localScale = Vector3.one;
							break;
						}
					case EditorManager.NotificationType.Success:
						{
							GameObject gameObject1 = Instantiate(EditorManager.inst.notificationPrefabs[1], Vector3.zero, Quaternion.identity);
							Destroy(gameObject1, _time);
							gameObject1.transform.Find("text").GetComponent<Text>().text = _text;
							gameObject1.transform.SetParent(EditorManager.inst.notification.transform);
							if (ConfigEntries.NotificationDirection.Value == Direction.Down)
							{
								gameObject1.transform.SetAsFirstSibling();
							}
							gameObject1.transform.localScale = Vector3.one;
							break;
						}
					case EditorManager.NotificationType.Error:
						{
							GameObject gameObject2 = Instantiate(EditorManager.inst.notificationPrefabs[2], Vector3.zero, Quaternion.identity);
							Destroy(gameObject2, _time);
							gameObject2.transform.Find("text").GetComponent<Text>().text = _text;
							gameObject2.transform.SetParent(EditorManager.inst.notification.transform);
							if (ConfigEntries.NotificationDirection.Value == Direction.Down)
							{
								gameObject2.transform.SetAsFirstSibling();
							}
							gameObject2.transform.localScale = Vector3.one;
							break;
						}
					case EditorManager.NotificationType.Warning:
						{
							GameObject gameObject3 = Instantiate(EditorManager.inst.notificationPrefabs[3], Vector3.zero, Quaternion.identity);
							Destroy(gameObject3, _time);
							gameObject3.transform.Find("text").GetComponent<Text>().text = _text;
							gameObject3.transform.SetParent(EditorManager.inst.notification.transform);
							if (ConfigEntries.NotificationDirection.Value == Direction.Down)
							{
								gameObject3.transform.SetAsFirstSibling();
							}
							gameObject3.transform.localScale = Vector3.one;
							break;
						}
				}
			}

			yield break;
		}

		public static void DisplayNotification(string _name, string _text, float _time, EditorManager.NotificationType _type)
        {
			inst.StartCoroutine(DisplayNotificationLoop(_name, _text, _time, _type));
        }
		//RTEditor.DisplayCustomNotification("h", 1f, LSColors.HexToColor("202020"), LSColors.HexToColor("FF0000"), Color.white, "Title!");
		public static void DisplayCustomNotification(string _name, string _text, float _time, Color _base, Color _top, Color _icCol, string _title, Sprite _icon = null)
        {
			inst.StartCoroutine(DisplayCustomNotificationLoop(_name, _text, _time, _base, _top, _icCol, _title, _icon));
        }

		public static IEnumerator DisplayNotificationLoop(string _name, string _text, float _time, EditorManager.NotificationType _type)
		{
			if (!ConfigEntries.EditorDebug.Value)
			{
				Debug.Log("<color=#F6AC1A>Editor</color><color=#2FCBD6>Management</color>\nNotification: " + _name + "\nText: " + _text + "\nTime: " + _time + "\nType: " + _type);
			}
			if (!notifications.Contains(_name) && notifications.Count < 20 && ConfigEntries.DisplayNotifications.Value)
			{
				notifications.Add(_name);
				switch (_type)
				{
					case EditorManager.NotificationType.Info:
						{
							GameObject gameObject = Instantiate(EditorManager.inst.notificationPrefabs[0], Vector3.zero, Quaternion.identity);
							Destroy(gameObject, _time);
							gameObject.transform.Find("text").GetComponent<TextMeshProUGUI>().text = _text;
							gameObject.transform.SetParent(EditorManager.inst.notification.transform);
							if (ConfigEntries.NotificationDirection.Value == Direction.Down)
							{
								gameObject.transform.SetAsFirstSibling();
							}
							gameObject.transform.localScale = Vector3.one;
							break;
						}
					case EditorManager.NotificationType.Success:
						{
							GameObject gameObject1 = Instantiate(EditorManager.inst.notificationPrefabs[1], Vector3.zero, Quaternion.identity);
							Destroy(gameObject1, _time);
							gameObject1.transform.Find("text").GetComponent<Text>().text = _text;
							gameObject1.transform.SetParent(EditorManager.inst.notification.transform);
							if (ConfigEntries.NotificationDirection.Value == Direction.Down)
							{
								gameObject1.transform.SetAsFirstSibling();
							}
							gameObject1.transform.localScale = Vector3.one;
							break;
						}
					case EditorManager.NotificationType.Error:
						{
							GameObject gameObject2 = Instantiate(EditorManager.inst.notificationPrefabs[2], Vector3.zero, Quaternion.identity);
							Destroy(gameObject2, _time);
							gameObject2.transform.Find("text").GetComponent<Text>().text = _text;
							gameObject2.transform.SetParent(EditorManager.inst.notification.transform);
							if (ConfigEntries.NotificationDirection.Value == Direction.Down)
							{
								gameObject2.transform.SetAsFirstSibling();
							}
							gameObject2.transform.localScale = Vector3.one;
							break;
						}
					case EditorManager.NotificationType.Warning:
						{
							GameObject gameObject3 = Instantiate(EditorManager.inst.notificationPrefabs[3], Vector3.zero, Quaternion.identity);
							Destroy(gameObject3, _time);
							gameObject3.transform.Find("text").GetComponent<Text>().text = _text;
							gameObject3.transform.SetParent(EditorManager.inst.notification.transform);
							if (ConfigEntries.NotificationDirection.Value == Direction.Down)
							{
								gameObject3.transform.SetAsFirstSibling();
							}
							gameObject3.transform.localScale = Vector3.one;
							break;
						}
				}

				yield return new WaitForSeconds(_time);
				notifications.Remove(_name);
			}
			yield break;
		}

		public static IEnumerator DisplayCustomNotificationLoop(string _name, string _text, float _time, Color _base, Color _top, Color _icCol, string _title, Sprite _icon = null)
		{
			if (!ConfigEntries.EditorDebug.Value)
			{
				Debug.Log("<color=#F6AC1A>Editor</color><color=#2FCBD6>Management</color>\nNotification: " + _name + "\nText: " + _text + "\nTime: " + _time + "\nBase Color: " + ColorToHex(_base) + "\nTop Color: " + ColorToHex(_top) + "\nIcon Color: " + ColorToHex(_icCol) + "\nTitle: " + _title);
			}
			if (!notifications.Contains(_name) && notifications.Count < 20 && ConfigEntries.DisplayNotifications.Value)
            {
				notifications.Add(_name);
				GameObject gameObject = Instantiate(EditorManager.inst.notificationPrefabs[0], Vector3.zero, Quaternion.identity);
				Destroy(gameObject, _time);
				gameObject.transform.Find("text").GetComponent<TextMeshProUGUI>().text = _text;
				gameObject.transform.SetParent(EditorManager.inst.notification.transform);
				if (ConfigEntries.NotificationDirection.Value == Direction.Down)
				{
					gameObject.transform.SetAsFirstSibling();
				}
				gameObject.transform.localScale = Vector3.one;

				gameObject.GetComponent<Image>().color = _base;
				var bg = gameObject.transform.Find("bg");
				var img = bg.Find("Image").GetComponent<Image>();
				bg.Find("bg").GetComponent<Image>().color = _top;
				if (_icon != null)
				{
					img.sprite = _icon;
				}

				img.color = _icCol;
				bg.Find("title").GetComponent<Text>().text = _title;

				yield return new WaitForSeconds(_time);
				notifications.Remove(_name);
			}

			yield break;
		}

		public static string GetShape(int _shape, int _shapeOption)
		{
			if (ObjectManager.inst != null && ObjectManager.inst.objectPrefabs.Count > 0 && ObjectManager.inst.objectPrefabs[_shape].options[_shapeOption])
            {
				int s = Mathf.Clamp(_shape, 0, ObjectManager.inst.objectPrefabs.Count - 1);
				int so = Mathf.Clamp(_shapeOption, 0, ObjectManager.inst.objectPrefabs[s].options.Count - 1);
				return ObjectManager.inst.objectPrefabs[s].options[so].name;
			}
			return "no shape";
        }

		public static string ColorToHex(Color32 color)
		{
			return color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2") + color.a.ToString("X2");
		}

		public static string secondsToTime(float _seconds)
		{
			TimeSpan timeSpan = TimeSpan.FromSeconds((double)_seconds);
			return string.Format("{0:D0}:{1:D1}:{2:D2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
		}

		public static void SetNewTime(string _value)
		{
			AudioManager.inst.CurrentAudioSource.time = float.Parse(_value);
		}

		public static void CreateNewNormalObject(bool _select = true)
		{
			ObjEditor.ObjectSelection tmpSelection = CreateNewDefaultObject(_select);
			EditorManager.inst.history.Add(new History.Command("Create New Normal Object", delegate ()
			{
				CreateNewDefaultObject(_select);
			}, delegate ()
			{
				ObjEditor.inst.DeleteObject(tmpSelection, true);
			}), false);
		}

		public static void CreateNewCircleObject(bool _select = true)
		{
			ObjEditor.ObjectSelection tmpSelection = CreateNewDefaultObject(_select);
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].shape = 1;
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].shapeOption = 0;
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].name = "circle";
			ObjectManager.inst.updateObjects(tmpSelection, false);
			ObjEditor.inst.RenderTimelineObject(tmpSelection);
			EditorManager.inst.history.Add(new History.Command("Create New Normal Circle Object", delegate ()
			{
				CreateNewCircleObject(_select);
			}, delegate ()
			{
				ObjEditor.inst.DeleteObject(tmpSelection, true);
			}), false);
		}

		public static void CreateNewTriangleObject(bool _select = true)
		{
			ObjEditor.ObjectSelection tmpSelection = CreateNewDefaultObject(_select);
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].shape = 2;
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].shapeOption = 0;
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].name = "triangle";
			ObjectManager.inst.updateObjects(tmpSelection, false);
			ObjEditor.inst.RenderTimelineObject(tmpSelection);
			EditorManager.inst.history.Add(new History.Command("Create New Normal Triangle Object", delegate ()
			{
				CreateNewTriangleObject(_select);
			}, delegate ()
			{
				ObjEditor.inst.DeleteObject(tmpSelection, true);
			}), false);
		}

		public static void CreateNewTextObject(bool _select = true)
		{
			ObjEditor.ObjectSelection tmpSelection = CreateNewDefaultObject(_select);
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].shape = 4;
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].shapeOption = 0;
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].text = "text";
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].name = "text";
			ObjectManager.inst.updateObjects(tmpSelection, false);
			ObjEditor.inst.RenderTimelineObject(tmpSelection);
			EditorManager.inst.history.Add(new History.Command("Create New Normal Text Object", delegate ()
			{
				CreateNewTextObject(_select);
			}, delegate ()
			{
				ObjEditor.inst.DeleteObject(tmpSelection, true);
			}), false);
		}

		public static void CreateNewHexagonObject(bool _select = true)
		{
			ObjEditor.ObjectSelection tmpSelection = CreateNewDefaultObject(_select);
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].shape = 5;
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].shapeOption = 0;
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].name = "hexagon";
			ObjectManager.inst.updateObjects(tmpSelection, false);
			ObjEditor.inst.RenderTimelineObject(tmpSelection);
			EditorManager.inst.history.Add(new History.Command("Create New Normal Hexagon Object", delegate ()
			{
				CreateNewHexagonObject(_select);
			}, delegate ()
			{
				ObjEditor.inst.DeleteObject(tmpSelection, true);
			}), false);
		}

		public static void CreateNewHelperObject(bool _select = true)
		{
			ObjEditor.ObjectSelection tmpSelection = CreateNewDefaultObject(_select);
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].objectType = DataManager.GameData.BeatmapObject.ObjectType.Helper;
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].name = "helper";
			ObjectManager.inst.updateObjects(tmpSelection, false);
			ObjEditor.inst.RenderTimelineObject(tmpSelection);
			EditorManager.inst.history.Add(new History.Command("Create New Helper Object", delegate ()
			{
				CreateNewHelperObject(_select);
			}, delegate ()
			{
				ObjEditor.inst.DeleteObject(tmpSelection, true);
			}), false);
		}

		public static void CreateNewDecorationObject(bool _select = true)
		{
			ObjEditor.ObjectSelection tmpSelection = CreateNewDefaultObject(_select);
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].objectType = DataManager.GameData.BeatmapObject.ObjectType.Decoration;
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].name = "decoration";
			ObjectManager.inst.updateObjects(tmpSelection, false);
			ObjEditor.inst.RenderTimelineObject(tmpSelection);
			EditorManager.inst.history.Add(new History.Command("Create New Decoration Object", delegate ()
			{
				CreateNewDecorationObject(_select);
			}, delegate ()
			{
				ObjEditor.inst.DeleteObject(tmpSelection, true);
			}), false);
		}

		public static void CreateNewEmptyObject(bool _select = true)
		{
			ObjEditor.ObjectSelection tmpSelection = CreateNewDefaultObject(_select);
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].objectType = DataManager.GameData.BeatmapObject.ObjectType.Empty;
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].name = "empty";
			ObjectManager.inst.updateObjects(tmpSelection, false);
			ObjEditor.inst.RenderTimelineObject(tmpSelection);
			EditorManager.inst.history.Add(new History.Command("Create New Empty Object", delegate ()
			{
				CreateNewEmptyObject(_select);
			}, delegate ()
			{
				ObjEditor.inst.DeleteObject(tmpSelection, true);
			}), false);
		}

		public static void CreateNewNoAutokillObject(bool _select = true)
		{
			ObjEditor.ObjectSelection tmpSelection = CreateNewDefaultObject(_select);
			var obj = tmpSelection.GetObjectData();
			obj.autoKillType = DataManager.GameData.BeatmapObject.AutoKillType.OldStyleNoAutokill;
			obj.name = "no autokill";

			ObjectManager.inst.updateObjects(tmpSelection, false);
			ObjEditor.inst.RenderTimelineObject(tmpSelection);
			EditorManager.inst.history.Add(new History.Command("Create New No Autokill Object", delegate ()
			{
				CreateNewNoAutokillObject(_select);
			}, delegate ()
			{
				ObjEditor.inst.DeleteObject(tmpSelection, true);
			}), false);
		}

		public static ObjEditor.ObjectSelection CreateNewDefaultObject(bool _select = true)
		{
			if (!EditorManager.inst.hasLoadedLevel)
			{
				EditorManager.inst.DisplayNotification("Can't add objects to level until a level has been loaded!", 2f, EditorManager.NotificationType.Error, false);
				return null;
			}
			List<List<DataManager.GameData.EventKeyframe>> list = new List<List<DataManager.GameData.EventKeyframe>>();
			list.Add(new List<DataManager.GameData.EventKeyframe>());
			list.Add(new List<DataManager.GameData.EventKeyframe>());
			list.Add(new List<DataManager.GameData.EventKeyframe>());
			list.Add(new List<DataManager.GameData.EventKeyframe>());
			list[0].Add(new DataManager.GameData.EventKeyframe(0f, new float[3], new float[0], 0));
			list[1].Add(new DataManager.GameData.EventKeyframe(0f, new float[]
			{
				1f,
				1f
			}, new float[0], 0));
			list[2].Add(new DataManager.GameData.EventKeyframe(0f, new float[1], new float[0], 0));
			list[3].Add(new DataManager.GameData.EventKeyframe(0f, new float[1], new float[0], 0));
			DataManager.GameData.BeatmapObject beatmapObject = new DataManager.GameData.BeatmapObject(true, AudioManager.inst.CurrentAudioSource.time, "", 0, "", list);
			beatmapObject.id = LSText.randomString(16);
			beatmapObject.autoKillType = DataManager.GameData.BeatmapObject.AutoKillType.LastKeyframeOffset;
			beatmapObject.autoKillOffset = 5f;
			if (EditorManager.inst.layer == 5)
			{
				beatmapObject.editorData.Layer = EditorManager.inst.lastLayer;
				SetLayer(EditorManager.inst.lastLayer);
			}
			else
			{
				beatmapObject.editorData.Layer = EditorManager.inst.layer;
			}
			int num = DataManager.inst.gameData.beatmapObjects.FindIndex((DataManager.GameData.BeatmapObject x) => x.fromPrefab);
			if (num == -1)
			{
				DataManager.inst.gameData.beatmapObjects.Add(beatmapObject);
			}
			else
			{
				DataManager.inst.gameData.beatmapObjects.Insert(num, beatmapObject);
			}
			ObjEditor.ObjectSelection objectSelection = new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Object, beatmapObject.id);
			ObjEditor.inst.CreateTimelineObject(objectSelection);
			ObjEditor.inst.RenderTimelineObject(objectSelection);
			ObjectManager.inst.updateObjects(objectSelection, false);
			AudioManager.inst.SetMusicTime(AudioManager.inst.CurrentAudioSource.time + 0.001f);
			if (_select)
			{
				ObjEditor.inst.SetCurrentObj(objectSelection);
			}
			return objectSelection;
		}

		public static DataManager.GameData.BeatmapObject CreateNewBeatmapObject(float _time, bool _add = true)
		{
			DataManager.GameData.BeatmapObject beatmapObject = new DataManager.GameData.BeatmapObject();
			beatmapObject.id = LSText.randomString(16);
			beatmapObject.StartTime = _time;
			if (EditorManager.inst.layer != 5)
			{
				beatmapObject.editorData.Layer = EditorManager.inst.layer;
			}
			else
			{
				beatmapObject.editorData.Layer = EditorManager.inst.lastLayer;
			}
			DataManager.GameData.EventKeyframe eventKeyframe = new DataManager.GameData.EventKeyframe();
			eventKeyframe.eventTime = 0f;
			eventKeyframe.SetEventValues(new float[3]);
			DataManager.GameData.EventKeyframe eventKeyframe2 = new DataManager.GameData.EventKeyframe();
			eventKeyframe2.eventTime = 0f;
			eventKeyframe2.SetEventValues(new float[]
			{
				1f,
				1f
			});
			DataManager.GameData.EventKeyframe eventKeyframe3 = new DataManager.GameData.EventKeyframe();
			eventKeyframe3.eventTime = 0f;
			eventKeyframe3.SetEventValues(new float[1]);
			DataManager.GameData.EventKeyframe eventKeyframe4 = new DataManager.GameData.EventKeyframe();
			eventKeyframe4.eventTime = 0f;
			eventKeyframe4.SetEventValues(new float[]
			{
				1f
			});
			beatmapObject.events[0].Add(eventKeyframe);
			beatmapObject.events[1].Add(eventKeyframe2);
			beatmapObject.events[2].Add(eventKeyframe3);
			beatmapObject.events[3].Add(eventKeyframe4);
			if (_add)
			{
				DataManager.inst.gameData.beatmapObjects.Add(beatmapObject);
				ObjEditor.ObjectSelection objectSelection = new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Object, beatmapObject.id);
				ObjEditor.inst.CreateTimelineObject(objectSelection);
				ObjEditor.inst.RenderTimelineObject(objectSelection);
				ObjEditor.inst.SetCurrentObj(objectSelection);
			}
			return beatmapObject;
		}

		public static GameObject RenderTimelineObject(ObjEditor.ObjectSelection _obj)
		{
			if (_obj.IsObject() && !string.IsNullOrEmpty(_obj.ID) && _obj.GetObjectData() != null && !_obj.GetObjectData().fromPrefab)
			{
				if (_obj == ObjEditor.inst.currentObjectSelection)
				{
					AccessTools.Method(typeof(ObjEditor), "ResizeKeyframeTimeline").Invoke(ObjEditor.inst, new object[] { });
				}
				GameObject gameObject;
				if (!_obj.HasTimelineObject())
				{
					gameObject = ObjEditor.inst.CreateTimelineObject(_obj);
				}
				else
				{
					gameObject = _obj.GetTimelineObject();
				}
				if (EditorManager.inst.layer != _obj.GetObjectData().editorData.Layer)
				{
					gameObject.SetActive(false);
				}
				else
				{
					DataManager.GameData.BeatmapObject objData = _obj.GetObjectData();
					if (objData.editorData.locked && gameObject.transform.Find("icons/lock") == null)
					{
						GameObject gameObject2 = Instantiate(ObjEditor.inst.timelineObjectPrefabLock);
						gameObject2.name = "lock";
						gameObject2.transform.SetParent(gameObject.transform.Find("icons"));
						gameObject2.GetComponent<RectTransform>().localScale = Vector2.one;
						gameObject2.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
					}
					else if (objData.editorData.locked && gameObject.transform.Find("icons/lock") != null)
					{
						gameObject.transform.Find("icons/lock").GetComponent<RectTransform>().localScale = Vector2.one;
						gameObject.transform.Find("icons/lock").GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
					}
					else if (!objData.editorData.locked && gameObject.transform.Find("icons/lock") != null)
					{
						Destroy(gameObject.transform.Find("icons/lock").gameObject);
					}
					if (objData.editorData.collapse && gameObject.transform.Find("icons/dots") == null)
					{
						GameObject gameObject3 = Instantiate(ObjEditor.inst.timelineObjectPrefabDots);
						gameObject3.name = "dots";
						gameObject3.transform.SetParent(gameObject.transform.Find("icons"));
						gameObject3.GetComponent<RectTransform>().localScale = Vector2.one;
						gameObject3.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
					}
					else if (objData.editorData.collapse && gameObject.transform.Find("icons/dots") != null)
					{
						gameObject.transform.Find("icons/dots").GetComponent<RectTransform>().localScale = Vector2.one;
						gameObject.transform.Find("icons/dots").GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
					}
					else if (!objData.editorData.collapse && gameObject.transform.Find("icons/dots") != null)
					{
						Destroy(gameObject.transform.Find("icons/dots").gameObject);
					}
					float startTime = objData.StartTime;
					float num = objData.GetObjectLifeLength(0f, false, true);
					if (num <= 0.4f)
					{
						num = 0.4f * EditorManager.inst.Zoom;
					}
					else
					{
						num *= EditorManager.inst.Zoom;
					}
					gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(num, 20f);
					gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(startTime * EditorManager.inst.Zoom, (float)(-20 * Mathf.Clamp(objData.editorData.Bin, 0, 14)));
					if (objData.objectType == DataManager.GameData.BeatmapObject.ObjectType.Helper)
					{
						gameObject.GetComponent<Image>().sprite = ObjEditor.inst.HelperSprite;
						gameObject.GetComponent<Image>().type = Image.Type.Tiled;
					}
					else if (objData.objectType == DataManager.GameData.BeatmapObject.ObjectType.Decoration)
					{
						gameObject.GetComponent<Image>().sprite = ObjEditor.inst.DecorationSprite;
						gameObject.GetComponent<Image>().type = Image.Type.Tiled;
					}
					else if (objData.objectType == DataManager.GameData.BeatmapObject.ObjectType.Empty)
					{
						gameObject.GetComponent<Image>().sprite = ObjEditor.inst.EmptySprite;
						gameObject.GetComponent<Image>().type = Image.Type.Tiled;
					}
					else
					{
						gameObject.GetComponent<Image>().sprite = null;
						gameObject.GetComponent<Image>().type = Image.Type.Simple;
					}
					gameObject.GetComponentInChildren<TextMeshProUGUI>().text = ((!string.IsNullOrEmpty(objData.name)) ? string.Format("<mark=#000000aa>{0}</mark>", objData.name) : "");
					Color color = ObjEditor.inst.NormalColor;
					if (objData.prefabID != "")
					{
						if (DataManager.inst.gameData.prefabs.FindIndex((DataManager.GameData.Prefab x) => x.ID == objData.prefabID) != -1)
						{
							color = DataManager.inst.PrefabTypes[DataManager.inst.gameData.prefabs.Find((DataManager.GameData.Prefab x) => x.ID == objData.prefabID).Type].Color;
						}
						else
						{
							DataManager.inst.gameData.beatmapObjects[_obj.Index].prefabID = null;
							DataManager.inst.gameData.beatmapObjects[_obj.Index].prefabInstanceID = null;
						}
					}
					if (ObjEditor.inst.ContainedInSelectedObjects(_obj))
					{
						gameObject.GetComponent<Image>().color = ObjEditor.inst.SelectedColor;
					}
					else
					{
						gameObject.GetComponent<Image>().color = color;
					}
					gameObject.GetComponentInChildren<TextMeshProUGUI>().color = LSColors.white;
					gameObject.SetActive(true);
				}
				return gameObject;
			}
			if (_obj.IsPrefab() && !string.IsNullOrEmpty(_obj.ID) && _obj.GetPrefabObjectData() != null)
			{
				GameObject gameObject4;
				if (!_obj.HasTimelineObject())
				{
					gameObject4 = ObjEditor.inst.CreateTimelineObject(_obj);
				}
				else
				{
					gameObject4 = _obj.GetTimelineObject();
				}
				if (EditorManager.inst.layer != _obj.GetPrefabObjectData().editorData.Layer)
				{
					gameObject4.SetActive(false);
				}
				else
				{
					DataManager.GameData.PrefabObject prefabObjectData = _obj.GetPrefabObjectData();
					DataManager.GameData.Prefab prefabData = _obj.GetPrefabData();
					_obj.DebugLog();
					if (prefabObjectData.editorData.locked && gameObject4.transform.Find("icons/lock") == null)
					{
						GameObject gameObject5 = Instantiate(ObjEditor.inst.timelineObjectPrefabLock);
						gameObject5.name = "lock";
						gameObject5.transform.SetParent(gameObject4.transform.Find("icons"));
						gameObject5.GetComponent<RectTransform>().localScale = Vector2.one;
						gameObject5.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
					}
					else if (prefabObjectData.editorData.locked && gameObject4.transform.Find("icons/lock") != null)
					{
						gameObject4.transform.Find("icons/lock").GetComponent<RectTransform>().localScale = Vector2.one;
						gameObject4.transform.Find("icons/lock").GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
					}
					else if (!prefabObjectData.editorData.locked && gameObject4.transform.Find("icons/lock") != null)
					{
						Destroy(gameObject4.transform.Find("icons/lock").gameObject);
					}
					if (prefabObjectData.editorData.collapse && gameObject4.transform.Find("icons/dots") == null)
					{
						GameObject gameObject6 = Instantiate(ObjEditor.inst.timelineObjectPrefabDots);
						gameObject6.name = "dots";
						gameObject6.transform.SetParent(gameObject4.transform.Find("icons"));
						gameObject6.GetComponent<RectTransform>().localScale = Vector2.one;
						gameObject6.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
					}
					else if (prefabObjectData.editorData.collapse && gameObject4.transform.Find("icons/dots") != null)
					{
						gameObject4.transform.Find("icons/dots").GetComponent<RectTransform>().localScale = Vector2.one;
						gameObject4.transform.Find("icons/dots").GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
					}
					else if (!prefabObjectData.editorData.collapse && gameObject4.transform.Find("icons/dots") != null)
					{
						Destroy(gameObject4.transform.Find("icons/dots").gameObject);
					}
					float num2 = prefabObjectData.StartTime + prefabData.Offset;
					float num3 = prefabData.GetPrefabLifeLength(prefabObjectData, true);
					if (num3 <= 0.2f)
					{
						num3 = 0.2f * EditorManager.inst.Zoom;
					}
					else
					{
						num3 *= EditorManager.inst.Zoom;
					}
					gameObject4.GetComponent<RectTransform>().sizeDelta = new Vector2(num3, 20f);
					gameObject4.GetComponent<RectTransform>().anchoredPosition = new Vector2(num2 * EditorManager.inst.Zoom, (float)(-20 * Mathf.Clamp(prefabObjectData.editorData.Bin, 0, 14)));
					Color color2 = DataManager.inst.PrefabTypes[prefabData.Type].Color;
					gameObject4.GetComponent<Image>().sprite = null;
					gameObject4.GetComponent<Image>().type = Image.Type.Simple;
					gameObject4.GetComponentInChildren<TextMeshProUGUI>().text = ((!string.IsNullOrEmpty(prefabData.Name)) ? string.Format("<mark=#000000aa>{0}</mark>", prefabData.Name) : DataManager.inst.PrefabTypes[prefabData.Type].Name);
					if (ObjEditor.inst.ContainedInSelectedObjects(_obj))
					{
						gameObject4.GetComponent<Image>().color = ObjEditor.inst.SelectedColor;
					}
					else
					{
						gameObject4.GetComponent<Image>().color = color2;
					}
					gameObject4.GetComponentInChildren<TextMeshProUGUI>().color = LSColors.white;
					gameObject4.SetActive(true);
				}
				return gameObject4;
			}
			return null;
		}

		public static void SetLayer(int _layer)
		{
			Image layerImage = GameObject.Find("TimelineBar/GameObject/layers").GetComponent<Image>();
			DataManager.inst.UpdateSettingInt("EditorLayer", _layer);
			int oldLayer = EditorManager.inst.layer;
			EditorManager.inst.layer = _layer;
			if (_layer < EditorManager.inst.layerColors.Count)
			{
				EditorManager.inst.timelineWaveformOverlay.GetComponent<Image>().color = EditorManager.inst.layerColors[_layer];
				layerImage.color = EditorManager.inst.layerColors[_layer];
			}
			if (_layer > 6)
			{
				layerImage.color = Color.white;
			}
			if (EditorManager.inst.layer == 5 && EditorManager.inst.lastLayer != 5)
			{
				EventEditor.inst.EventLabels.SetActive(true);
				EventEditor.inst.EventHolders.SetActive(true);
				EventEditor.inst.CreateEventObjects();
				CheckpointEditor.inst.CreateCheckpoints();
				ObjEditor.inst.RenderTimelineObjects("");
				GameObject.Find("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject/6").GetComponent<Toggle>().isOn = true;
			}
			else if (EditorManager.inst.layer != 5 && EditorManager.inst.lastLayer == 5)
			{
				EventEditor.inst.EventLabels.SetActive(false);
				EventEditor.inst.EventHolders.SetActive(false);
				if (EventEditor.inst.eventObjects.Count > 0)
				{
					foreach (List<GameObject> list in EventEditor.inst.eventObjects)
					{
						foreach (GameObject obj in list)
						{
							Destroy(obj);
						}
					}
					foreach (List<GameObject> list2 in EventEditor.inst.eventObjects)
					{
						list2.Clear();
					}
				}
				ObjEditor.inst.RenderTimelineObjects("");
				if (CheckpointEditor.inst.checkpoints.Count > 0)
				{
					foreach (GameObject obj2 in CheckpointEditor.inst.checkpoints)
					{
						Destroy(obj2);
					}
					CheckpointEditor.inst.checkpoints.Clear();
				}
				CheckpointEditor.inst.CreateGhostCheckpoints();
				GameObject.Find("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject/6").GetComponent<Toggle>().isOn = false;
			}
			else
			{
				ObjEditor.inst.RenderTimelineObjects("");
				GameObject.Find("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject/6").GetComponent<Toggle>().isOn = false;
			}

			if (_layer < EditorManager.inst.layerSelectors.Count)
			{
				EditorManager.inst.layerSelectors[_layer].GetComponent<Toggle>().isOn = true;
			}

			EditorManager.inst.history.Add(new History.Command("Change Layer", delegate ()
			{
				SetLayer(_layer);
			}, delegate ()
			{
				SetLayer(oldLayer);
			}), false);
		}

		public static void SetMultiObjectLayer(int _layer, bool _add = false)
		{
			foreach (var objectSelection in ObjEditor.inst.selectedObjects)
			{
				if (objectSelection.IsObject())
				{
					if (_add == false)
					{
						objectSelection.GetObjectData().editorData.Layer = _layer;
					}
					else
					{
						objectSelection.GetObjectData().editorData.Layer += _layer;
					}
				}
				if (objectSelection.IsPrefab())
				{
					if (_add == false)
					{
						objectSelection.GetPrefabObjectData().editorData.Layer = _layer;
					}
					else
					{
						objectSelection.GetPrefabObjectData().editorData.Layer += _layer;
					}
				}
				ObjEditor.inst.RenderTimelineObject(objectSelection);
			}
		}

		public void AutoSaveLevel()
		{
			if (!EditorManager.inst.hasLoadedLevel)
			{
				EditorManager.inst.DisplayNotification("Beatmap can't autosave until you load a level.", 3f, EditorManager.NotificationType.Error, false);
				return;
			}
			if (EditorManager.inst.savingBeatmap)
			{
				EditorManager.inst.DisplayNotification("Already attempting to save the beatmap!", 2f, EditorManager.NotificationType.Error, false);
				return;
			}
			string text = string.Concat(new string[]
			{
			FileManager.GetAppPath(),
			"/",
			GameManager.inst.basePath,
			"autosaves/autosave_",
			DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss"),
			".lsb"
			});
			if (!RTFile.DirectoryExists(RTFile.GetApplicationDirectory() + GameManager.inst.basePath + "autosaves"))
			{
				Directory.CreateDirectory(RTFile.GetApplicationDirectory() + GameManager.inst.basePath + "autosaves");
			}
			EditorManager.inst.DisplayNotification("Autosaving backup!", 2f, EditorManager.NotificationType.Warning, false);
			EditorManager.inst.autosaves.Add(text);
			while (EditorManager.inst.autosaves.Count > ConfigEntries.AutoSaveLimit.Value)
			{
				File.Delete(EditorManager.inst.autosaves.First());
				EditorManager.inst.autosaves.RemoveAt(0);
			}
			EditorManager.inst.StartCoroutine(DataManager.inst.SaveData(text));
		}

		public static void Duplicate(bool _regen = true)
		{
			Copy(false, true, _regen);
		}

		public static void Cut()
		{
			Copy(true, false);
		}

		public static void Copy(bool _cut = false, bool _dup = false, bool _regen = true)
		{
			if ((EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Timeline) && EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Background)) || EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Background))
			{
				BackgroundEditor.inst.CopyBackground();
				if (!_cut)
				{
					EditorManager.inst.DisplayNotification("Copied Background Object", 1f, EditorManager.NotificationType.Success, false);
				}
				else
				{
					BackgroundEditor.inst.DeleteBackground(BackgroundEditor.inst.currentObj);
					EditorManager.inst.DisplayNotification("Cut Background Object", 1f, EditorManager.NotificationType.Success, false);
				}
				if (_dup)
				{
					EditorManager.inst.Paste(0f);
				}
			}
			if ((EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Timeline) && EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Checkpoint)) || EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Checkpoint))
			{
				if (!_dup)
				{
					CheckpointEditor.inst.CopyCheckpoint();
					if (!_cut)
					{
						EditorManager.inst.DisplayNotification("Copied Checkpoint", 1f, EditorManager.NotificationType.Success, false);
					}
					else
					{
						BackgroundEditor.inst.DeleteBackground(BackgroundEditor.inst.currentObj);
						EditorManager.inst.DisplayNotification("Cut Checkpoint", 1f, EditorManager.NotificationType.Success, false);
					}
				}
				else
				{
					EditorManager.inst.DisplayNotification("Can't Duplicate Checkpoint", 1f, EditorManager.NotificationType.Error, false);
				}
			}
			if (EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Object))
			{
				if (!_dup)
				{
					ObjEditor.inst.CopyAllSelectedEvents();
					if (!_cut)
					{
						EditorManager.inst.DisplayNotification("Copied Object Keyframe", 1f, EditorManager.NotificationType.Success, false);
					}
					else
					{
						foreach (ObjEditor.KeyframeSelection keyframeSelection in ObjEditor.inst.copiedObjectKeyframes.Keys)
						{
							ObjEditor.inst.DeleteKeyframe(keyframeSelection.Type, keyframeSelection.Index);
						}
						EditorManager.inst.DisplayNotification("Cut Object Keyframe", 1f, EditorManager.NotificationType.Success, false);
					}
				}
				else
				{
					EditorManager.inst.DisplayNotification("Can't Duplicate Keyframe", 1f, EditorManager.NotificationType.Error, false);
				}
			}
			if ((EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Timeline) && EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Event)) || EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Event))
			{
				if (!_dup)
				{
					EventEditor.inst.CopyAllSelectedEvents();
					if (!_cut)
					{
						EditorManager.inst.DisplayNotification("Copied Event Keyframe", 1f, EditorManager.NotificationType.Success, false);
					}
					else
					{
						foreach (EventEditor.KeyframeSelection keyframeSelection2 in EventEditor.inst.copiedEventKeyframes.Keys)
						{
							EventEditor.inst.DeleteEvent(keyframeSelection2.Type, keyframeSelection2.Index);
						}
						EventEditor.inst.copiedEventKeyframes.Clear();
						EditorManager.inst.DisplayNotification("Cut Event Keyframe", 1f, EditorManager.NotificationType.Success, false);
					}
				}
				else
				{
					EditorManager.inst.DisplayNotification("Can't Duplicate Keyframe", 1f, EditorManager.NotificationType.Error, false);
				}
			}
			if ((EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Timeline) && EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Object)) || EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Prefab))
			{
				float offsetTime = ObjEditor.inst.selectedObjects.Min(delegate (ObjEditor.ObjectSelection x)
				{
					if (x.IsObject())
					{
						return x.GetObjectData().StartTime;
					}
					return x.GetPrefabObjectData().StartTime;
				});
				ObjEditor.inst.CopyObject();
				if (!_cut)
				{
					EditorManager.inst.DisplayNotification("Copied Beatmap Object", 1f, EditorManager.NotificationType.Success, false);
				}
				else
				{
					ObjEditor.inst.DeleteObject(ObjEditor.inst.currentObjectSelection, true);
					EditorManager.inst.DisplayNotification("Cut Beatmap Object", 1f, EditorManager.NotificationType.Success, false);
				}
				if (_dup)
				{
					Paste(offsetTime, _regen);
				}
			}
		}

		public static void Paste(float _offsetTime = 0f, bool _regen = true)
		{
			if (!hoveringOIF)
			{
				if ((EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Timeline) && EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Object)) || (EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Timeline) && EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Prefab)) || (EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Timeline) && EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Prefab)))
				{
					PasteObject(_offsetTime, _regen);
				}
				if ((EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Timeline) && EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Event)) || EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Event))
				{
					EventEditor.inst.PasteEvents();
					EditorManager.inst.DisplayNotification("Pasted Event Object", 1f, EditorManager.NotificationType.Success, false);
				}
				if (EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Object))
				{
					ObjEditor.inst.PasteKeyframes();
					EditorManager.inst.DisplayNotification("Pasted Object Keyframe", 1f, EditorManager.NotificationType.Success, false);
				}
				if ((EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Timeline) && EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Checkpoint)) || EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Checkpoint))
				{
					CheckpointEditor.inst.PasteCheckpoint();
					EditorManager.inst.DisplayNotification("Pasted Checkpoint Object", 1f, EditorManager.NotificationType.Success, false);
				}
				if (EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Background))
				{
					BackgroundEditor.inst.PasteBackground();
					EditorManager.inst.DisplayNotification("Pasted Background Object", 1f, EditorManager.NotificationType.Success, false);
				}
			}
		}

		public static void PasteObject(float _offsetTime = 0f, bool _regen = true)
		{
			if (!ObjEditor.inst.hasCopiedObject || ObjEditor.inst.beatmapObjCopy == null || (ObjEditor.inst.beatmapObjCopy.prefabObjects.Count <= 0 && ObjEditor.inst.beatmapObjCopy.objects.Count <= 0))
			{
				EditorManager.inst.DisplayNotification("No copied object yet!", 1f, EditorManager.NotificationType.Error, false);
				return;
			}

			ObjEditor.inst.DeRenderSelectedObjects();
			ObjEditor.inst.selectedObjects.Clear();
			EditorManager.inst.DisplayNotification("Pasting objects, please wait.", 1f, EditorManager.NotificationType.Success);

			DataManager.GameData.Prefab pr = null;
			if (RTFile.FileExists(Application.persistentDataPath + "/copied_objects.lsp"))
			{
				JSONNode jn = JSON.Parse(FileManager.inst.LoadJSONFileRaw(Application.persistentDataPath + "/copied_objects.lsp"));

				List<DataManager.GameData.BeatmapObject> _objects = new List<DataManager.GameData.BeatmapObject>();
				for (int aIndex = 0; aIndex < jn["objects"].Count; ++aIndex)
					_objects.Add(DataManager.GameData.BeatmapObject.ParseGameObject(jn["objects"][aIndex]));

				List<DataManager.GameData.PrefabObject> _prefabObjects = new List<DataManager.GameData.PrefabObject>();
				for (int aIndex = 0; aIndex < jn["prefab_objects"].Count; ++aIndex)
					_prefabObjects.Add(DataManager.inst.gameData.ParsePrefabObject(jn["prefab_objects"][aIndex]));

				pr = new DataManager.GameData.Prefab(jn["name"], jn["type"].AsInt, jn["offset"].AsFloat, _objects, _prefabObjects);
				ObjEditor.inst.hasCopiedObject = true;
			}
			if (pr == null)
			{
				inst.StartCoroutine(AddPrefabExpandedToLevel(ObjEditor.inst.beatmapObjCopy, true, _offsetTime, false, _regen));
			}
			else
            {
				inst.StartCoroutine(AddPrefabExpandedToLevel(pr, true, _offsetTime, false, _regen));
            }

			//Keyframe bug testing
			foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
			{
				if (ObjEditor.inst.keyframeTimelineSelections.ContainsKey(beatmapObject.id))
				{
					if (ObjEditor.inst.keyframeTimelineSelections[beatmapObject.id].Count == 0)
					{
						ObjEditor.KeyframeSelection item = new ObjEditor.KeyframeSelection(0, 0);
						ObjEditor.inst.keyframeTimelineSelections[beatmapObject.id].Add(item);
					}
				}
			}
		}

		public static IEnumerator GroupSelectObjects(bool _add = true)
        {
			ienumRunning = true;
			EditorManager.inst.DisplayNotification("Selecting objects, please wait.", 1f, EditorManager.NotificationType.Success);
			float delay = 0f;
			var objEditor = ObjEditor.inst;

			if (_add == false)
            {
				objEditor.selectedObjects.Clear();
				objEditor.RenderTimelineObjects();
			}

			foreach (KeyValuePair<string, GameObject> keyValuePair in objEditor.beatmapObjects)
			{
				if (EditorManager.RectTransformToScreenSpace(EditorManager.inst.SelectionBoxImage.rectTransform).Overlaps(EditorManager.RectTransformToScreenSpace(keyValuePair.Value.GetComponent<Image>().rectTransform)) && keyValuePair.Value.activeSelf)
				{
					yield return new WaitForSeconds(delay);
					ObjEditor.ObjectSelection objectSelection = new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Object, keyValuePair.Key);
					AddSelectedObject(objEditor, objectSelection);
					delay += 0.0001f;
				}
			}
			foreach (KeyValuePair<string, GameObject> keyValuePair2 in objEditor.prefabObjects)
			{
				if (EditorManager.RectTransformToScreenSpace(EditorManager.inst.SelectionBoxImage.rectTransform).Overlaps(EditorManager.RectTransformToScreenSpace(keyValuePair2.Value.GetComponent<Image>().rectTransform)) && keyValuePair2.Value.activeSelf)
				{
					yield return new WaitForSeconds(delay);
					ObjEditor.ObjectSelection prefabSelection = new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Prefab, keyValuePair2.Key);
					AddSelectedObject(objEditor, prefabSelection);
					delay += 0.0001f;
				}
			}

			if (objEditor.selectedObjects.Count() > 0)
			{
				objEditor.selectedObjects = (from x in objEditor.selectedObjects
											orderby x.Index ascending
											select x).ToList();
				EditorManager.inst.ShowDialog("Multi Object Editor", false);
			}

			if (objEditor.selectedObjects.Count() <= 0)
			{
				CheckpointEditor.inst.SetCurrentCheckpoint(0);
			}
			EditorManager.inst.DisplayNotification("Selection includes " + objEditor.selectedObjects.Count + " objects!", 1f, EditorManager.NotificationType.Success);
			ienumRunning = false;
			yield break;
        }

		public static void Delete()
        {
			if (EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Object))
			{
				if (ObjEditor.inst.currentKeyframe != 0)
				{
					inst.StartCoroutine(DeleteKeyframes());
					EditorManager.inst.DisplayNotification("Deleted Beatmap Object Keyframe.", 1f, EditorManager.NotificationType.Success, false);
				}
				else
				{
					EditorManager.inst.DisplayNotification("Can't Delete First Keyframe.", 1f, EditorManager.NotificationType.Error, false);
				}
			}
			if ((EditorManager.inst.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Timeline) && EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Object)) || EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Prefab))
			{
				if (DataManager.inst.gameData.beatmapObjects.Count > 1)
				{
					if (ObjEditor.inst.selectedObjects.Count > 1)
					{
						List<ObjEditor.ObjectSelection> list = new List<ObjEditor.ObjectSelection>();
						foreach (ObjEditor.ObjectSelection item in ObjEditor.inst.selectedObjects)
						{
							list.Add(item);
						}
						EditorManager.inst.ClearDialogs(new EditorManager.EditorDialog.DialogType[]
						{
						EditorManager.EditorDialog.DialogType.Object
						});
						EditorManager.inst.ClearDialogs(new EditorManager.EditorDialog.DialogType[]
						{
						EditorManager.EditorDialog.DialogType.Prefab
						});
						list = (from x in list
								orderby x.Index ascending
								select x).ToList();


						//Figure out how to get this to work
						float startTime = 0f;

						List<float> startTimeList = new List<float>();
						foreach (var item in list)
                        {
							if (item.IsObject())
							{
								startTimeList.Add(item.GetObjectData().StartTime);
							}
							if (item.IsPrefab())
							{
								startTimeList.Add(item.GetPrefabObjectData().StartTime);
							}
                        }

						startTimeList = (from x in startTimeList
										 orderby x ascending
										 select x).ToList();

						startTime = startTimeList[0];

						DataManager.GameData.Prefab prefab = new DataManager.GameData.Prefab("deleted objects", 0, startTime, list);

						EditorManager.inst.history.Add(new History.Command("Delete Objects", delegate ()
						{
							List<ObjEditor.ObjectSelection> redone = new List<ObjEditor.ObjectSelection>();
							foreach (ObjEditor.ObjectSelection item in ObjEditor.inst.selectedObjects)
							{
								redone.Add(item);
							}
							inst.StartCoroutine(DeleteObjects(redone, true));
						}, delegate ()
						{
							ObjEditor.inst.selectedObjects.Clear();
							inst.StartCoroutine(AddPrefabExpandedToLevel(prefab, true, 0f, true));
						}), false);

						inst.StartCoroutine(DeleteObjects(list, true));
					}
					else
					{
						float startTime = 0f;
						if (ObjEditor.inst.currentObjectSelection.IsObject())
                        {
							startTime = ObjEditor.inst.currentObjectSelection.GetObjectData().StartTime;
						}
						else
                        {
							startTime = ObjEditor.inst.currentObjectSelection.GetPrefabObjectData().StartTime;
                        }

						DataManager.GameData.Prefab prefab = new DataManager.GameData.Prefab("deleted object", 0, startTime, ObjEditor.inst.selectedObjects);

						EditorManager.inst.history.Add(new History.Command("Delete Object", delegate ()
						{
							List<ObjEditor.ObjectSelection> redone = new List<ObjEditor.ObjectSelection>();
							foreach (ObjEditor.ObjectSelection item in ObjEditor.inst.selectedObjects)
							{
								redone.Add(item);
							}
							inst.StartCoroutine(DeleteObject(redone[0], true));
						}, delegate ()
						{
							ObjEditor.inst.selectedObjects.Clear();
							inst.StartCoroutine(AddPrefabExpandedToLevel(prefab, true, 0f, true));
						}), false);

						inst.StartCoroutine(DeleteObject(ObjEditor.inst.currentObjectSelection, true));

						if (ObjEditor.inst.currentObjectSelection.IsObject() && !string.IsNullOrEmpty(ObjEditor.inst.currentObjectSelection.GetObjectData().name))
						{
							EditorManager.inst.DisplayNotification("Deleted Beatmap Object\n[ " + ObjEditor.inst.currentObjectSelection.GetObjectData().name + " ].", 1f, EditorManager.NotificationType.Success, false);
						}
						else
						{
							EditorManager.inst.DisplayNotification("Deleted Beatmap Object\n[ Object ].", 1f, EditorManager.NotificationType.Success, false);
						}
					}
				}
				else
				{
					EditorManager.inst.DisplayNotification("Can't Delete Only Beatmap Object", 1f, EditorManager.NotificationType.Error, false);
				}
			}
			if (EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Event))
			{
				if (EventEditor.inst.keyframeSelections.Count > 1)
				{
					List<EventEditor.KeyframeSelection> list2 = new List<EventEditor.KeyframeSelection>();
					foreach (var keyframeSelection in EventEditor.inst.keyframeSelections)
					{
						if (keyframeSelection.Index != 0)
						{
							list2.Add(keyframeSelection);
						}
						else
						{
							EditorManager.inst.DisplayNotification("Can't Delete First Event Keyframe.", 1f, EditorManager.NotificationType.Error, false);
						}
					}
					EditorManager.inst.ClearDialogs(new EditorManager.EditorDialog.DialogType[]
					{
					EditorManager.EditorDialog.DialogType.Event
					});
					list2 = (from x in list2
							 orderby x.Index descending
							 select x).ToList();

					inst.StartCoroutine(DeleteEvent(list2));
					EditorManager.inst.DisplayNotification("Deleted Event Keyframes.", 1f, EditorManager.NotificationType.Success, false);
				}
				else if (EventEditor.inst.currentEvent != 0)
				{
					EventEditor.inst.DeleteEvent(EventEditor.inst.currentEventType, EventEditor.inst.currentEvent);
					EditorManager.inst.DisplayNotification("Deleted Event Keyframe.", 1f, EditorManager.NotificationType.Success, false);
				}
				else
				{
					EditorManager.inst.DisplayNotification("Can't Delete First Event Keyframe.", 1f, EditorManager.NotificationType.Error, false);
				}
			}
			if (EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Background))
			{
				BackgroundEditor.inst.DeleteBackground(BackgroundEditor.inst.currentObj);
			}
			if (EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Checkpoint))
			{
				if (CheckpointEditor.inst.currentObj != 0)
				{
					CheckpointEditor.inst.DeleteCheckpoint(CheckpointEditor.inst.currentObj);
					EditorManager.inst.DisplayNotification("Deleted Checkpoint.", 1f, EditorManager.NotificationType.Success, false);
					return;
				}
				EditorManager.inst.DisplayNotification("Can't Delete First Checkpoint.", 1f, EditorManager.NotificationType.Error, false);
			}
		}

		public static void AddSelectedObject(ObjEditor __instance, ObjEditor.ObjectSelection __0, bool _showMulti = false)
		{
			if (__instance.ContainedInSelectedObjects(__0))
			{
				__instance.selectedObjects.Remove(__0);
			}
			else
			{
				__instance.selectedObjects.Add(__0);
				__instance.currentObjectSelection = __0;
			}
			if (__instance.selectedObjects.Count > 1)
			{
				EditorManager.inst.ClearDialogs(Array.Empty<EditorManager.EditorDialog.DialogType>());
				if (_showMulti)
					EditorManager.inst.ShowDialog("Multi Object Editor", false);

				if (__0.IsObject())
				{
					__instance.SetCurrentKeyframe(0, 0, false, false);
				}
				__instance.RenderTimelineObject(__instance.currentObjectSelection);
				return;
			}
			__instance.SetCurrentObj(__0);
		}

		public static ObjectManager updateObjects(DataManager.GameData.BeatmapObject _beatmapObject)
		{
			string id = _beatmapObject.id;
			if (!_beatmapObject.fromPrefab)
			{
				if (ObjectManager.inst.beatmapGameObjects.ContainsKey(id))
				{
					Destroy(ObjectManager.inst.beatmapGameObjects[id].obj);
					ObjectManager.inst.beatmapGameObjects[id].sequence.all.Kill(false);
					ObjectManager.inst.beatmapGameObjects[id].sequence.col.Kill(false);
					ObjectManager.inst.beatmapGameObjects.Remove(id);
				}
				if (!_beatmapObject.fromPrefab)
				{
					_beatmapObject.active = false;
					for (int j = 0; j < _beatmapObject.events.Count; j++)
					{
						for (int k = 0; k < _beatmapObject.events[j].Count; k++)
						{
							_beatmapObject.events[j][k].active = false;
						}
					}
				}
			}
			ObjectManager.inst.updateObjects(_beatmapObject.id);
			return ObjectManager.inst;
		}

		public static IEnumerator IupdateObjects(DataManager.GameData.BeatmapObject _beatmapObject)
        {
			string id = _beatmapObject.id;
			if (!_beatmapObject.fromPrefab)
            {
				if (ObjectManager.inst.beatmapGameObjects.ContainsKey(id))
                {
					Destroy(ObjectManager.inst.beatmapGameObjects[id].obj);
					ObjectManager.inst.beatmapGameObjects[id].sequence.all.Kill();
					ObjectManager.inst.beatmapGameObjects[id].sequence.col.Kill();
					ObjectManager.inst.beatmapGameObjects.Remove(id);
				}
				if (!_beatmapObject.fromPrefab)
                {
					_beatmapObject.active = false;
					for (int i = 0; i < _beatmapObject.events.Count; i++)
                    {
						for (int j = 0; j < _beatmapObject.events[i].Count; j++)
                        {
							_beatmapObject.events[i][j].active = false;
                        }
                    }
                }
            }
			yield break;
        }

		public static IEnumerator DeleteObjects(List<ObjEditor.ObjectSelection> _objs, bool _set = true)
		{
			ienumRunning = true;

			float delay = 0f;
			var list = ObjEditor.inst.selectedObjects;
			int count = ObjEditor.inst.selectedObjects.Count;

			EditorManager.inst.DisplayNotification("Deleting Beatmap Objects [ " + count + " ]", 1f, EditorManager.NotificationType.Success);

			ObjEditor.inst.SetCurrentObj(new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Object, 0));
			foreach (ObjEditor.ObjectSelection obj in _objs)
			{
				yield return new WaitForSeconds(delay);
				inst.StartCoroutine(DeleteObject(obj, _set));
				delay += 0.0001f;
			}

			EditorManager.inst.DisplayNotification("Deleted Beatmap Objects [ " + count + " ]", 1f, EditorManager.NotificationType.Success);

			ienumRunning = false;
			yield break;
		}

		public static IEnumerator DeleteObject(ObjEditor.ObjectSelection _obj, bool _set = true)
		{
			if (_obj.IsObject())
			{
				if (DataManager.inst.gameData.beatmapObjects.Count > 1)
				{
					string name = _obj.GetObjectData().name;
					string id = _obj.GetObjectData().id;
					for (int i = 0; i < DataManager.inst.gameData.beatmapObjects.Count; i++)
					{
						if (DataManager.inst.gameData.beatmapObjects[i].parent == id)
						{
							DataManager.inst.gameData.beatmapObjects[i].parent = "";
							updateObjects(DataManager.inst.gameData.beatmapObjects[i]);
						}
					}
					ObjEditor.inst.selectedObjects.Remove(_obj);
					Destroy(ObjEditor.inst.beatmapObjects[_obj.ID]);
					ObjEditor.inst.beatmapObjects.Remove(_obj.ID);
					DataManager.inst.gameData.beatmapObjects.RemoveAt(_obj.Index);
					if (ObjectManager.inst.beatmapGameObjects.ContainsKey(_obj.ID))
					{
						Destroy(ObjectManager.inst.beatmapGameObjects[_obj.ID].obj);
					}
					//Maybe consider moving this.
					if (_set)
					{
						if (DataManager.inst.gameData.beatmapObjects.Count > 0)
						{
							//ObjEditor.inst.SetCurrentObj(new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Object, Mathf.Clamp(_obj.Index - 1, 0, DataManager.inst.gameData.beatmapObjects.Count - 1)));
						}
						else if (DataManager.inst.gameData.beatmapData.checkpoints.Count > 0)
						{
							CheckpointEditor.inst.SetCurrentCheckpoint(0);
						}
					}
					ObjectManager.inst.terminateObject(_obj);
				}
				else
				{
					EditorManager.inst.DisplayNotification("Can't delete only object", 2f, EditorManager.NotificationType.Error, false);
				}
			}
			else if (_obj.IsPrefab())
			{
				string name2 = _obj.GetPrefabData().Name;
				string id2 = _obj.GetPrefabData().ID;
				ObjEditor.inst.selectedObjects.Remove(_obj);
				Destroy(ObjEditor.inst.prefabObjects[_obj.ID]);
				ObjEditor.inst.prefabObjects.Remove(_obj.ID);
				DataManager.inst.gameData.prefabObjects.RemoveAt(_obj.Index);
				if (_set)
				{
					if (DataManager.inst.gameData.beatmapObjects.Count > 0)
					{
						ObjEditor.inst.SetCurrentObj(new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Object, Mathf.Clamp(_obj.Index - 1, 0, DataManager.inst.gameData.beatmapObjects.Count - 1)));
					}
					else if (DataManager.inst.gameData.beatmapData.checkpoints.Count > 0)
					{
						CheckpointEditor.inst.SetCurrentCheckpoint(0);
					}
				}

				ObjectManager.inst.terminateObject(_obj);
			}
			yield break;
		}

		public static Dictionary<string, object> uiDictionary = new Dictionary<string, object>();

		public static IEnumerator AddUIDictionary()
		{
			var objEditor = ObjEditor.inst;
			var tfv = objEditor.ObjectView.transform;
			uiDictionary.Add("Object - Origin X IF", tfv.Find("origin/x").GetComponent<InputField>());
			uiDictionary.Add("Object - Origin X >", tfv.Find("origin/x/>").GetComponent<InputField>());
			uiDictionary.Add("Object - Origin X <", tfv.Find("origin/x/<").GetComponent<InputField>());
			uiDictionary.Add("Object - Origin Y IF", tfv.Find("origin/y").GetComponent<InputField>());
			uiDictionary.Add("Object - Bin Slider", tfv.Find("editor/bin").GetComponent<Slider>());
			uiDictionary.Add("Object - Name IF", tfv.Find("name/name").GetComponent<InputField>());
			uiDictionary.Add("Object - Object Type DD", tfv.Find("name/object-type").GetComponent<Dropdown>());

			yield break;
        }

		public static IEnumerator RefreshObjectGUI()
        {
			if (!ienumRunning)
			{
				if (DataManager.inst.gameData.beatmapObjects.Count > 0 && ObjEditor.inst.currentObjectSelection != null && !string.IsNullOrEmpty(ObjEditor.inst.currentObjectSelection.ID) && ObjEditor.inst.currentObjectSelection.IsObject() && ObjEditor.inst.selectedObjects.Count < 2)
				{
					var objEditor = ObjEditor.inst;
					var dataManager = DataManager.inst;
					var objectManager = ObjectManager.inst;

					var currentObjectSelection = objEditor.currentObjectSelection;
					var beatmapObject = currentObjectSelection.GetObjectData();

					var tfv = objEditor.ObjectView.transform;

					Debug.LogFormat("{0}Refresh Object GUI: Origin", EditorPlugin.className);
					//Origin
					{
						var oxIF = tfv.Find("origin/x").GetComponent<InputField>();

						oxIF.onValueChanged.RemoveAllListeners();
						oxIF.text = beatmapObject.origin.x.ToString();
						oxIF.onValueChanged.AddListener(delegate (string _value)
						{
							beatmapObject.origin.x = float.Parse(_value);
							objectManager.updateObjects(currentObjectSelection);
							objEditor.RenderTimelineObject(currentObjectSelection);
						});

						var oyIF = tfv.Find("origin/y").GetComponent<InputField>();

						oyIF.onValueChanged.RemoveAllListeners();
						oyIF.text = beatmapObject.origin.y.ToString();
						oyIF.onValueChanged.AddListener(delegate (string _value)
						{
							beatmapObject.origin.y = float.Parse(_value);
							objectManager.updateObjects(currentObjectSelection);
							objEditor.RenderTimelineObject(currentObjectSelection);
						});

						var oxleft = oxIF.transform.Find("<").GetComponent<Button>();
						var oxright = oxIF.transform.Find(">").GetComponent<Button>();

						var oyleft = oyIF.transform.Find("<").GetComponent<Button>();
						var oyright = oyIF.transform.Find(">").GetComponent<Button>();

						oxleft.onClick.RemoveAllListeners();
						oxleft.onClick.AddListener(delegate ()
						{
							float a = float.Parse(oxIF.text);
							a -= 1f;
							oxIF.text = a.ToString();
						});

						oxright.onClick.RemoveAllListeners();
						oxright.onClick.AddListener(delegate ()
						{
							float a = float.Parse(oxIF.text);
							a += 1f;
							oxIF.text = a.ToString();
						});

						oyleft.onClick.RemoveAllListeners();
						oyleft.onClick.AddListener(delegate ()
						{
							float a = float.Parse(oyIF.text);
							a -= 1f;
							oyIF.text = a.ToString();
						});

						oyright.onClick.RemoveAllListeners();
						oyright.onClick.AddListener(delegate ()
						{
							float a = float.Parse(oyIF.text);
							a += 1f;
							oyIF.text = a.ToString();
						});

						if (!tfv.Find("origin/x").gameObject.GetComponent<EventTrigger>())
						{
							var oxtrig = tfv.Find("origin/x").gameObject.AddComponent<EventTrigger>();
							var oytrig = tfv.Find("origin/y").gameObject.AddComponent<EventTrigger>();

							oxtrig.triggers.Add(Triggers.ScrollDelta(oxIF, 0.1f, 10f, true));
							oytrig.triggers.Add(Triggers.ScrollDelta(oyIF, 0.1f, 10f, true));
							oxtrig.triggers.Add(Triggers.ScrollDeltaVector2(oxIF, oyIF, 0.1f, 10f));
							oytrig.triggers.Add(Triggers.ScrollDeltaVector2(oxIF, oyIF, 0.1f, 10f));
						}
					}

					Debug.LogFormat("{0}Refresh Object GUI: General", EditorPlugin.className);
					//General
					{
						var editorBin = tfv.Find("editor/bin").GetComponent<Slider>();
						editorBin.onValueChanged.RemoveAllListeners();
						editorBin.value = beatmapObject.editorData.Bin;
						editorBin.onValueChanged.AddListener(delegate (float _value)
						{
							beatmapObject.editorData.Bin = (int)Mathf.Clamp(_value, 0f, 14f);
							objEditor.RenderTimelineObject(currentObjectSelection);
						});

						if (!tfv.Find("name/name").GetComponent<InputFieldHelper>())
						{
							var t = tfv.Find("name/name").gameObject.AddComponent<InputFieldHelper>();
							t.type = InputFieldHelper.Type.String;
						}
						var nameName = tfv.Find("name/name").GetComponent<InputField>();
						Triggers.ObjEditorInputFieldValues(nameName, "name", beatmapObject.name, EditorProperty.ValueType.String, false, true);

						var objType = tfv.Find("name/object-type").GetComponent<Dropdown>();
						Triggers.ObjEditorDropdownValues(objType, "objectType", beatmapObject.objectType, true, true);
					}

					Debug.LogFormat("{0}Refresh Object GUI: Autokill", EditorPlugin.className);
					//Autokill
					{
						var akType = tfv.Find("autokill/tod-dropdown").GetComponent<Dropdown>();
						Triggers.ObjEditorDropdownValues(akType, "autoKillType", beatmapObject.autoKillType, true, true);

						var todValue = tfv.Find("autokill/tod-value");
						var akOffset = todValue.GetComponent<InputField>();
						var akset = tfv.Find("autokill/|");
						var aksetButt = akset.GetComponent<Button>();

						if (beatmapObject.autoKillType == DataManager.GameData.BeatmapObject.AutoKillType.FixedTime || beatmapObject.autoKillType == DataManager.GameData.BeatmapObject.AutoKillType.SongTime || beatmapObject.autoKillType == DataManager.GameData.BeatmapObject.AutoKillType.LastKeyframeOffset)
						{
							todValue.gameObject.SetActive(true);

							akOffset.onValueChanged.RemoveAllListeners();
							akOffset.text = beatmapObject.autoKillOffset.ToString();
							akOffset.onValueChanged.AddListener(delegate (string _value)
							{
								float num = float.Parse(_value);
								if (beatmapObject.autoKillType == DataManager.GameData.BeatmapObject.AutoKillType.SongTime)
								{
									float startTime = beatmapObject.StartTime;
									if (num < startTime)
									{
										num = startTime + 0.1f;
									}
								}
								if (num < 0f)
								{
									num = 0f;
								}
								beatmapObject.autoKillOffset = num;
								objectManager.updateObjects(currentObjectSelection);
								objEditor.RenderTimelineObject(currentObjectSelection);
							});

							akset.gameObject.SetActive(true);
							aksetButt.onClick.RemoveAllListeners();
							aksetButt.onClick.AddListener(delegate ()
							{
								beatmapObject.autoKillOffset = AudioManager.inst.CurrentAudioSource.time;
								objectManager.updateObjects(currentObjectSelection);
								objEditor.RenderTimelineObject(currentObjectSelection);
							});
						}
						else
						{
							todValue.gameObject.SetActive(false);
							akOffset.onValueChanged.RemoveAllListeners();
							akset.gameObject.SetActive(false);
							aksetButt.onClick.RemoveAllListeners();
						}

						var collapse = tfv.Find("autokill/collapse").GetComponent<Toggle>();

						if (!collapse.GetComponent<HoverUI>())
						{
							var collapseHover = collapse.gameObject.AddComponent<HoverUI>();
							collapseHover.animatePos = true;
							collapseHover.animateSca = true;
							collapseHover.size = 1.1f;
							collapseHover.animPos = new Vector3(-3f, 2f, 0f);
						}

						collapse.onValueChanged.RemoveAllListeners();
						collapse.isOn = beatmapObject.editorData.collapse;
						collapse.onValueChanged.AddListener(delegate (bool _value)
						{
							beatmapObject.editorData.collapse = _value;
							objectManager.updateObjects(currentObjectSelection);
							objEditor.RenderTimelineObject(currentObjectSelection);
						});
					}

					Debug.LogFormat("{0}Refresh Object GUI: Start Time", EditorPlugin.className);
					//Start Time
					{
						var time = tfv.Find("time").GetComponent<EventTrigger>();
						var timeIF = tfv.Find("time/time").GetComponent<InputField>();

						var locker = tfv.Find("time/lock").GetComponent<Toggle>();
						locker.onValueChanged.RemoveAllListeners();
						locker.isOn = beatmapObject.editorData.locked;
						locker.onValueChanged.AddListener(delegate (bool _val)
						{
							beatmapObject.editorData.locked = _val;
							objEditor.RenderTimelineObject(currentObjectSelection);
						});

						time.triggers.Clear();
						time.triggers.Add(Triggers.ScrollDelta(timeIF, 1f, 2f));

						Triggers.ObjEditorInputFieldValues(timeIF, "StartTime", beatmapObject.StartTime, EditorProperty.ValueType.Float, true, true, true);

						var timeJumpLargeLeft = tfv.Find("time/<<").GetComponent<Button>();

						timeJumpLargeLeft.onClick.RemoveAllListeners();
						timeJumpLargeLeft.interactable = (beatmapObject.StartTime > 0f);
						timeJumpLargeLeft.onClick.AddListener(delegate ()
						{
							float moveTime = beatmapObject.StartTime - 1f;
							moveTime = Mathf.Clamp(moveTime, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
							timeIF.text = moveTime.ToString();

							objectManager.updateObjects(currentObjectSelection);
							objEditor.RenderTimelineObject(currentObjectSelection);
							ObjEditorPatch.ResizeKeyframeTimeline();
						});

						var timeJumpLeft = tfv.Find("time/<").GetComponent<Button>();

						timeJumpLeft.onClick.RemoveAllListeners();
						timeJumpLeft.interactable = (beatmapObject.StartTime > 0f);
						timeJumpLeft.onClick.AddListener(delegate ()
						{
							float moveTime = beatmapObject.StartTime - 0.1f;
							moveTime = Mathf.Clamp(moveTime, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
							timeIF.text = moveTime.ToString();

							objectManager.updateObjects(currentObjectSelection);
							objEditor.RenderTimelineObject(currentObjectSelection);
							ObjEditorPatch.ResizeKeyframeTimeline();
						});

						var setStartToTime = tfv.Find("time/|").GetComponent<Button>();

						setStartToTime.onClick.RemoveAllListeners();
						setStartToTime.onClick.AddListener(delegate ()
						{
							timeIF.text = EditorManager.inst.CurrentAudioPos.ToString();

							objectManager.updateObjects(currentObjectSelection);
							objEditor.RenderTimelineObject(currentObjectSelection);
							ObjEditorPatch.ResizeKeyframeTimeline();
						});

						var timeJumpRight = tfv.Find("time/>").GetComponent<Button>();

						timeJumpRight.onClick.RemoveAllListeners();
						timeJumpRight.onClick.AddListener(delegate ()
						{
							float moveTime = beatmapObject.StartTime + 0.1f;
							moveTime = Mathf.Clamp(moveTime, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
							timeIF.text = moveTime.ToString();

							objectManager.updateObjects(currentObjectSelection, false);
							objEditor.RenderTimelineObject(currentObjectSelection);
							ObjEditorPatch.ResizeKeyframeTimeline();
						});

						var timeJumpLargeRight = tfv.Find("time/>>").GetComponent<Button>();

						timeJumpLargeRight.onClick.RemoveAllListeners();
						timeJumpLargeRight.onClick.AddListener(delegate ()
						{
							float moveTime = beatmapObject.StartTime + 1f;
							moveTime = Mathf.Clamp(moveTime, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
							timeIF.text = moveTime.ToString();

							objectManager.updateObjects(currentObjectSelection);
							objEditor.RenderTimelineObject(currentObjectSelection);
							ObjEditorPatch.ResizeKeyframeTimeline();
						});
					}

					Debug.LogFormat("{0}Refresh Object GUI: Depth", EditorPlugin.className);
					//Depth
					{
						var depthSlider = tfv.Find("depth/depth").GetComponent<Slider>();
						var depthText = tfv.Find("spacer/depth").GetComponent<InputField>();

						if (!depthText.GetComponent<InputFieldHelper>())
						{
							depthText.gameObject.AddComponent<InputFieldHelper>();
						}

						if (!tfv.Find("depth/depth/Handle Slide Area/Handle").GetComponent<HoverUI>())
						{
							var depthHover = tfv.Find("depth/depth/Handle Slide Area/Handle").gameObject.AddComponent<HoverUI>();
							depthHover.animatePos = false;
							depthHover.animateSca = true;
							depthHover.size = 1.05f;
						}

						depthText.onValueChanged.RemoveAllListeners();
						depthText.text = beatmapObject.Depth.ToString();

						depthText.onValueChanged.AddListener(delegate (string _value)
						{
							beatmapObject.Depth = int.Parse(_value);
							if (ConfigEntries.DepthUpdate.Value)
							{
								depthSlider.value = beatmapObject.Depth;
							}
							else
							{
								objectManager.updateObjects(currentObjectSelection);
							}
						});

						depthSlider.onValueChanged.RemoveAllListeners();
						depthSlider.value = (float)beatmapObject.Depth;
						depthSlider.onValueChanged.AddListener(delegate (float _value)
						{
							depthText.text = ((int)_value).ToString();
							objectManager.updateObjects(currentObjectSelection);
						});

						var depthLeft = depthText.transform.Find("<").GetComponent<Button>();
						var depthRight = depthText.transform.Find(">").GetComponent<Button>();

						depthLeft.onClick.RemoveAllListeners();
						depthRight.onClick.RemoveAllListeners();
						depthLeft.onClick.AddListener(delegate ()
						{
							depthText.text = (int.Parse(depthText.text) + 1).ToString();
						});
						depthRight.onClick.AddListener(delegate ()
						{
							depthText.text = (int.Parse(depthText.text) - 1).ToString();
						});

						if (!depthText.GetComponent<EventTrigger>())
						{
							var depthTrigger = depthText.gameObject.AddComponent<EventTrigger>();

							EventTrigger.Entry entryDepth = new EventTrigger.Entry();
							entryDepth.eventID = EventTriggerType.Scroll;
							entryDepth.callback.AddListener(delegate (BaseEventData eventData)
							{
								PointerEventData pointerEventData = (PointerEventData)eventData;
								if (pointerEventData.scrollDelta.y < 0f)
								{
									depthText.text = (int.Parse(depthText.text) - ConfigEntries.DepthAmount.Value).ToString();
									return;
								}
								if (pointerEventData.scrollDelta.y > 0f)
								{
									depthText.text = (int.Parse(depthText.text) + ConfigEntries.DepthAmount.Value).ToString();
								}
							});
							depthTrigger.triggers.Clear();
							depthTrigger.triggers.Add(entryDepth);
						}
					}

					Debug.LogFormat("{0}Refresh Object GUI: Shape", EditorPlugin.className);
					//Shape Settings
					{
						var shapeSettings = tfv.Find("shapesettings");
						foreach (object obj3 in shapeSettings)
						{
							var ch = (Transform)obj3;
							if (ch.name != "5")
							{
								foreach (var c in ch)
								{
									var e = (Transform)c;
									if (!e.GetComponent<HoverUI>())
									{
										var he = e.gameObject.AddComponent<HoverUI>();
										he.animatePos = false;
										he.animateSca = true;
										he.size = 1.1f;
									}
								}
							}
							ch.gameObject.SetActive(false);
						}
						if (beatmapObject.shape >= shapeSettings.childCount)
						{
							beatmapObject.shape = shapeSettings.childCount - 1;
							ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection, false);
						}
						if (beatmapObject.shape == 4)
						{
							shapeSettings.GetComponent<RectTransform>().sizeDelta = new Vector2(351f, 74f);
							var child = shapeSettings.GetChild(4);
							child.GetComponent<RectTransform>().sizeDelta = new Vector2(351f, 74f);
							child.Find("Text").GetComponent<Text>().alignment = TextAnchor.UpperLeft;
							child.Find("Placeholder").GetComponent<Text>().alignment = TextAnchor.UpperLeft;
							child.GetComponent<InputField>().lineType = InputField.LineType.MultiLineNewline;
						}
						else
						{
							shapeSettings.GetComponent<RectTransform>().sizeDelta = new Vector2(351f, 32f);
							shapeSettings.GetChild(4).GetComponent<RectTransform>().sizeDelta = new Vector2(351f, 32f);
						}
						if (beatmapObject.shape != 6)
						{
							shapeSettings.GetChild(beatmapObject.shape).gameObject.SetActive(true);
						}
						else
						{
							shapeSettings.GetChild(4).gameObject.SetActive(true);
						}
						for (int j = 1; j <= ObjectManager.inst.objectPrefabs.Count; j++)
						{
							int buttonTmp = j;
							var shoggle = tfv.Find("shape/" + j).GetComponent<Toggle>();
							shoggle.onValueChanged.RemoveAllListeners();
							if (beatmapObject.shape == buttonTmp - 1)
							{
								tfv.Find("shape/" + j).GetComponent<Toggle>().isOn = true;
							}
							else
							{
								tfv.Find("shape/" + j).GetComponent<Toggle>().isOn = false;
							}
							shoggle.onValueChanged.AddListener(delegate (bool _value)
							{
								if (_value)
								{
									ObjEditor.inst.SetShape(buttonTmp - 1, 0);
								}
							});

							if (!tfv.Find("shape/" + j).GetComponent<HoverUI>())
							{
								var hoverUI = tfv.Find("shape/" + j).gameObject.AddComponent<HoverUI>();
								hoverUI.animatePos = false;
								hoverUI.animateSca = true;
								hoverUI.size = 1.1f;
							}
						}
						if (beatmapObject.shape != 4 && beatmapObject.shape != 6)
						{
							for (int k = 0; k < shapeSettings.GetChild(beatmapObject.shape).childCount - 1; k++)
							{
								int buttonTmp = k;
								shapeSettings.GetChild(beatmapObject.shape).GetChild(k).GetComponent<Toggle>().onValueChanged.RemoveAllListeners();
								if (beatmapObject.shapeOption == k)
								{
									shapeSettings.GetChild(beatmapObject.shape).GetChild(k).GetComponent<Toggle>().isOn = true;
								}
								else
								{
									shapeSettings.GetChild(beatmapObject.shape).GetChild(k).GetComponent<Toggle>().isOn = false;
								}
								shapeSettings.GetChild(beatmapObject.shape).GetChild(k).GetComponent<Toggle>().onValueChanged.AddListener(delegate (bool _value)
								{
									if (_value)
									{
										ObjEditor.inst.SetShape(beatmapObject.shape, buttonTmp);
									}
								});
							}
						}
						else if (beatmapObject.shape == 4)
						{
							var textIF = shapeSettings.Find("5").GetComponent<InputField>();
							textIF.onValueChanged.RemoveAllListeners();
							textIF.text = beatmapObject.text;
							textIF.onValueChanged.AddListener(delegate (string _value)
							{
								beatmapObject.text = _value;
								ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection, false);
							});
						}
						else if (beatmapObject.shape == 6)
						{
							var textIF = shapeSettings.Find("5").GetComponent<InputField>();
							textIF.onValueChanged.RemoveAllListeners();
							textIF.text = beatmapObject.text;
							textIF.onValueChanged.AddListener(delegate (string _value)
							{
								beatmapObject.text = _value;
								ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection, false);
							});
						}
					}

					Debug.LogFormat("{0}Refresh Object GUI: Parent", EditorPlugin.className);
					string parent = beatmapObject.parent;
					//RefreshParentGUI
					{
						//Make this more optimized.
						var parentTextText = tfv.Find("parent/text/text").GetComponent<Text>();
						var parentText = tfv.Find("parent/text").GetComponent<Button>();
						var parentMore = tfv.Find("parent/more").GetComponent<Button>();
						var parent_more = tfv.Find("parent_more");

						if (!string.IsNullOrEmpty(parent))
						{
							Debug.LogFormat("{0}Refresh Object GUI: Object Has Parent", EditorPlugin.className);

							DataManager.GameData.BeatmapObject beatmapObjectParent = null;
							ObjEditor.ObjectSelection tmp = new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Object, parent);
							if (DataManager.inst.gameData.beatmapObjects.Find((DataManager.GameData.BeatmapObject x) => x.id == parent) != null && (parent != "CAMERA_PARENT" && objectModifiersPlugin != null || objectModifiersPlugin == null))
							{
								beatmapObjectParent = DataManager.inst.gameData.beatmapObjects.Find((DataManager.GameData.BeatmapObject x) => x.id == parent);
								parentTextText.text = beatmapObjectParent.name;
							}
							else
							{
								parentTextText.text = "[CAMERA]";
							}

							parentText.interactable = true;
							parentText.onClick.RemoveAllListeners();
							parentText.onClick.AddListener(delegate ()
							{
								if (DataManager.inst.gameData.beatmapObjects.Find((DataManager.GameData.BeatmapObject x) => x.id == parent) != null && (parent != "CAMERA_PARENT" && objectModifiersPlugin != null || objectModifiersPlugin == null))
									ObjEditor.inst.SetCurrentObj(tmp);
								else
								{
									SetLayer(5);
									EventEditor.inst.SetCurrentEvent(0, EditorExtensions.ClosestEventKeyframe(0));
								}
							});
							parentMore.onClick.RemoveAllListeners();
							parentMore.interactable = true;
							parentMore.onClick.AddListener(delegate ()
							{
								ObjEditor.inst.advancedParent = !ObjEditor.inst.advancedParent;
								parent_more.gameObject.SetActive(ObjEditor.inst.advancedParent);
							});
							parent_more.gameObject.SetActive(ObjEditor.inst.advancedParent);

							for (int i = 0; i < 3; i++)
							{
								Triggers.ObjEditorParentOffset(currentObjectSelection, beatmapObject, tfv.Find("parent_more").GetChild(i + 1), i);
							}
						}
						else
						{
							parentTextText.text = "No Parent Object";
							parentText.interactable = false;
							parentText.onClick.RemoveAllListeners();
							parentMore.onClick.RemoveAllListeners();
							parentMore.interactable = false;
							parent_more.gameObject.SetActive(false);
						}

						Debug.LogFormat("{0}Refresh Object GUI: Parent Popup", EditorPlugin.className);
						var parentParent = tfv.Find("parent/parent").GetComponent<Button>();
						parentParent.onClick.RemoveAllListeners();
						parentParent.onClick.AddListener(delegate ()
						{
							EditorManager.inst.OpenParentPopup();
						});
					}

					Debug.LogFormat("{0}Refresh Object GUI: Prefab stuff", EditorPlugin.className);
					tfv.Find("collapselabel").gameObject.SetActive(beatmapObject.prefabID != "");
					tfv.Find("applyprefab").gameObject.SetActive(beatmapObject.prefabID != "");

					if (ObjEditor.inst.keyframeSelections.Count <= 1)
					{
						for (int i = 0; i < ObjEditor.inst.KeyframeDialogs.Count(); i++)
						{
							ObjEditor.inst.KeyframeDialogs[i].SetActive(i == ObjEditor.inst.currentKeyframeKind);
						}

						Debug.LogFormat("{0}Refresh Object GUI: Keyframes", EditorPlugin.className);
						//Keyframes
						{
							var kfdialog = ObjEditor.inst.KeyframeDialogs[ObjEditor.inst.currentKeyframeKind].transform;
							if (beatmapObject.events[ObjEditor.inst.currentKeyframeKind].Count > 0)
							{
								Triggers.ObjEditorKeyframeDialog(kfdialog, ObjEditor.inst.currentKeyframeKind, beatmapObject);
							}
							if (ObjEditor.inst.currentKeyframe == 0)
							{
								kfdialog.Find("time/<<").GetComponent<Button>().interactable = false;
								kfdialog.Find("time/<").GetComponent<Button>().interactable = false;
								kfdialog.Find("time/>").GetComponent<Button>().interactable = false;
								kfdialog.Find("time/>>").GetComponent<Button>().interactable = false;
								kfdialog.Find("time/time").GetComponent<InputField>().interactable = false;
							}
							else
							{
								kfdialog.Find("time/<<").GetComponent<Button>().interactable = true;
								kfdialog.Find("time/<").GetComponent<Button>().interactable = true;
								kfdialog.Find("time/>").GetComponent<Button>().interactable = true;
								kfdialog.Find("time/>>").GetComponent<Button>().interactable = true;
								kfdialog.Find("time/time").GetComponent<InputField>().interactable = true;
							}

							var superLeft = kfdialog.Find("edit/<<").GetComponent<Button>();

							superLeft.onClick.RemoveAllListeners();
							superLeft.interactable = (ObjEditor.inst.currentKeyframe != 0);
							superLeft.onClick.AddListener(delegate ()
							{
								ObjEditor.inst.SetCurrentKeyframe(0, true);
							});

							var left = kfdialog.Find("edit/<").GetComponent<Button>();

							left.onClick.RemoveAllListeners();
							left.interactable = (ObjEditor.inst.currentKeyframe != 0);
							left.onClick.AddListener(delegate ()
							{
								ObjEditor.inst.AddCurrentKeyframe(-1, true);
							});

							string text = ObjEditor.inst.currentKeyframe.ToString();
							if (ObjEditor.inst.currentKeyframe == 0)
							{
								text = "S";
							}
							else if (ObjEditor.inst.currentKeyframe == beatmapObject.events[ObjEditor.inst.currentKeyframeKind].Count - 1)
							{
								text = "E";
							}

							kfdialog.Find("edit/|").GetComponentInChildren<Text>().text = text;

							var right = kfdialog.Find("edit/>").GetComponent<Button>();

							right.onClick.RemoveAllListeners();
							right.interactable = (ObjEditor.inst.currentKeyframe < beatmapObject.events[ObjEditor.inst.currentKeyframeKind].Count - 1);
							right.onClick.AddListener(delegate ()
							{
								ObjEditor.inst.AddCurrentKeyframe(1, true);
							});

							var superRight = kfdialog.Find("edit/>>").GetComponent<Button>();

							superRight.onClick.RemoveAllListeners();
							superRight.interactable = (ObjEditor.inst.currentKeyframe < beatmapObject.events[ObjEditor.inst.currentKeyframeKind].Count - 1);
							superRight.onClick.AddListener(delegate ()
							{
								ObjEditor.inst.AddCurrentKeyframe(int.MaxValue, true);
							});

							var deleteKey = kfdialog.Find("edit/del").GetComponent<Button>();

							deleteKey.onClick.RemoveAllListeners();
							deleteKey.interactable = (ObjEditor.inst.currentKeyframe != 0);
							deleteKey.onClick.AddListener(delegate ()
							{
								ObjEditor.inst.DeleteKeyframe();
							});
						}
					}
					else
					{
						for (int num7 = 0; num7 < ObjEditor.inst.KeyframeDialogs.Count(); num7++)
						{
							ObjEditor.inst.KeyframeDialogs[num7].SetActive(false);
						}
						ObjEditor.inst.KeyframeDialogs[4].SetActive(true);
					}
				}
			}
			yield break;
        }

		public static void ExpandCurrentPrefab()
		{
			if (ObjEditor.inst.currentObjectSelection.IsPrefab())
			{
				string id = ObjEditor.inst.currentObjectSelection.GetPrefabObjectData().ID;
				inst.StartCoroutine(AddExpandedPrefabToLevel(ObjEditor.inst.currentObjectSelection.GetPrefabObjectData()));
				ObjectManager.inst.terminateObject(ObjEditor.inst.currentObjectSelection);
				Destroy(ObjEditor.inst.prefabObjects[id]);
				ObjEditor.inst.prefabObjects.Remove(id);
				DataManager.inst.gameData.prefabObjects.RemoveAll((Predicate<DataManager.GameData.PrefabObject>)(x => x.ID == id));
				DataManager.inst.gameData.beatmapObjects.RemoveAll((Predicate<DataManager.GameData.BeatmapObject>)(x => x.prefabInstanceID == id && x.fromPrefab));
				ObjEditor.inst.selectedObjects.Clear();
			}
			else
			{
				EditorManager.inst.DisplayNotification("Can't expand non-prefab!", 2f, EditorManager.NotificationType.Error);
			}
		}

		public static IEnumerator DeleteKeyframes()
		{
			ienumRunning = true;

			float delay = 0f;
			List<ObjEditor.KeyframeSelection> list = new List<ObjEditor.KeyframeSelection>();
			foreach (ObjEditor.KeyframeSelection keyframeSelection in ObjEditor.inst.keyframeSelections)
			{
				list.Add(new ObjEditor.KeyframeSelection(keyframeSelection.Type, keyframeSelection.Index));
			}
			list = (from x in list
					orderby x.Index descending
					select x).ToList();

			int count = list.Count;

			EditorManager.inst.DisplayNotification("Deleting Object Keyframes [ " + count + " ]", 2f, EditorManager.NotificationType.Success);

			foreach (ObjEditor.KeyframeSelection keyframeSelection2 in list)
			{
				if (keyframeSelection2.Index != 0)
				{
					yield return new WaitForSeconds(delay);
					DataManager.inst.gameData.beatmapObjects[ObjEditor.inst.currentObjectSelection.Index].events[keyframeSelection2.Type].RemoveAt(keyframeSelection2.Index);
					delay += 0.0001f;
				}
				else
				{
					EditorManager.inst.DisplayNotification("Can't delete first Keyframe", 2f, EditorManager.NotificationType.Error, false);
				}
			}
			ObjEditor.inst.SetCurrentKeyframe(0, false);
			ObjEditor.inst.RenderTimelineObject(ObjEditor.inst.currentObjectSelection);
			ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection, false);

			var editor = ObjEditor.inst;
			MethodInfo createKeyframes = editor.GetType().GetMethod("CreateKeyframes", BindingFlags.NonPublic | BindingFlags.Instance);
			createKeyframes.Invoke(editor, new object[] { -1 });

			MethodInfo refreshKeyframeGUI = editor.GetType().GetMethod("RefreshKeyframeGUI", BindingFlags.NonPublic | BindingFlags.Instance);
			refreshKeyframeGUI.Invoke(editor, new object[] { });

			MethodInfo resizeKeyframeTimeline = editor.GetType().GetMethod("ResizeKeyframeTimeline", BindingFlags.NonPublic | BindingFlags.Instance);
			resizeKeyframeTimeline.Invoke(editor, new object[] { });

			EditorManager.inst.DisplayNotification("Deleted Object Keyframes [ " + count + " ]", 1f, EditorManager.NotificationType.Success);

			ienumRunning = false;

			yield break;
		}

		public static IEnumerator DeleteEvent(List<EventEditor.KeyframeSelection> _keyframes)
		{
			ienumRunning = true;

			float delay = 0f;
			foreach (EventEditor.KeyframeSelection selection in _keyframes)
			{
				yield return new WaitForSeconds(delay);
				DataManager.inst.gameData.eventObjects.allEvents[selection.Type].RemoveAt(selection.Index);
				EventEditor.inst.CreateEventObjects();
				EventManager.inst.updateEvents();
				delay += 0.0001f;
			}

			ienumRunning = false;
		}

		public static IEnumerator AddExpandedPrefabToLevel(DataManager.GameData.PrefabObject _obj)
		{
			ienumRunning = true;

			float delay = 0f;

			string id = _obj.ID;
			DataManager.GameData.Prefab prefab = DataManager.inst.gameData.prefabs.Find((Predicate<DataManager.GameData.Prefab>)(x => x.ID == _obj.prefabID));
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			foreach (DataManager.GameData.BeatmapObject beatmapObject in prefab.objects)
			{
				string str = LSText.randomString(16);
				dictionary.Add(beatmapObject.id, str);
			}
			foreach (DataManager.GameData.BeatmapObject beatmapObject1 in prefab.objects)
			{
				yield return new WaitForSeconds(delay);
				DataManager.GameData.BeatmapObject beatmapObj = beatmapObject1;
				DataManager.GameData.BeatmapObject beatmapObject2 = DataManager.GameData.BeatmapObject.DeepCopy(beatmapObj, false);
				if (dictionary.ContainsKey(beatmapObj.id))
				{
					beatmapObject2.id = dictionary[beatmapObj.id];
				}
				if (dictionary.ContainsKey(beatmapObj.parent))
				{
					beatmapObject2.parent = dictionary[beatmapObj.parent];
				}
				else if (DataManager.inst.gameData.beatmapObjects.FindIndex((Predicate<DataManager.GameData.BeatmapObject>)(x => x.id == beatmapObj.parent)) == -1)
				{
					beatmapObject2.parent = "";
				}
				beatmapObject2.active = false;
				beatmapObject2.fromPrefab = false;
				beatmapObject2.prefabID = prefab.ID;
				beatmapObject2.StartTime += _obj.StartTime;
				beatmapObject2.StartTime += prefab.Offset;
				if (EditorManager.inst != null)
				{
					if (EditorManager.inst.layer == 5)
                    {
						beatmapObject2.editorData.Layer = EditorManager.inst.lastLayer;
					}
					else
					{
						beatmapObject2.editorData.Layer = EditorManager.inst.layer;
					}
					beatmapObject2.editorData.Bin = Mathf.Clamp(beatmapObject2.editorData.Bin, 0, 14);
				}

				beatmapObject2.prefabInstanceID = id;
				DataManager.inst.gameData.beatmapObjects.Add(beatmapObject2);

				if (ObjEditor.inst != null)
				{
					ObjEditor.ObjectSelection objectSelection = new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Object, beatmapObject2.id);
					ObjEditor.inst.AddSelectedObject(objectSelection);

					ObjEditor.inst.RenderTimelineObject(objectSelection);
					ObjectManager.inst.updateObjects(objectSelection);
				}

				delay += 0.0001f;
			}

			EditorManager.inst.DisplayNotification("Expanded Prefab Object [" + _obj + "].", 1f, EditorManager.NotificationType.Success, false);
			ienumRunning = false;
			yield break;
		}
 
		public static IEnumerator AddPrefabExpandedToLevel(DataManager.GameData.Prefab _obj, bool _select = false, float _offsetTime = 0f, bool _undone = false, bool _regen = false)
		{
			ienumRunning = true;
			float delay = 0f;
			float audioTime = EditorManager.inst.CurrentAudioPos;
			var objEditor = ObjEditor.inst;

			Dictionary<string, string> dictionary1 = new Dictionary<string, string>();
			foreach (DataManager.GameData.BeatmapObject beatmapObject in _obj.objects)
			{
				string str = LSText.randomString(16);
				dictionary1.Add(beatmapObject.id, str);
			}
			Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
			foreach (DataManager.GameData.BeatmapObject beatmapObject in _obj.objects)
			{
				if (!string.IsNullOrEmpty(beatmapObject.prefabInstanceID) && !dictionary2.ContainsKey(beatmapObject.prefabInstanceID))
				{
					string str = LSText.randomString(16);
					dictionary2.Add(beatmapObject.prefabInstanceID, str);
				}
			}
			foreach (DataManager.GameData.BeatmapObject beatmapObject1 in _obj.objects)
			{
				yield return new WaitForSeconds(delay);
				DataManager.GameData.BeatmapObject beatmapObj = beatmapObject1;
				DataManager.GameData.BeatmapObject beatmapObject2 = DataManager.GameData.BeatmapObject.DeepCopy(beatmapObj, false);
				if (dictionary1.ContainsKey(beatmapObj.id))
					beatmapObject2.id = dictionary1[beatmapObj.id];
				if (dictionary1.ContainsKey(beatmapObj.parent))
					beatmapObject2.parent = dictionary1[beatmapObj.parent];
				else if (DataManager.inst.gameData.beatmapObjects.FindIndex((Predicate<DataManager.GameData.BeatmapObject>)(x => x.id == beatmapObj.parent)) == -1)
					beatmapObject2.parent = "";
				beatmapObject2.prefabID = beatmapObj.prefabID;
				if (_regen && !string.IsNullOrEmpty(beatmapObj.prefabInstanceID))
				{
					beatmapObject2.prefabInstanceID = dictionary2[beatmapObj.prefabInstanceID];
				}
				else
					beatmapObject2.prefabInstanceID = beatmapObj.prefabInstanceID;
				beatmapObject2.fromPrefab = beatmapObj.fromPrefab;
				if (_undone == false)
				{
					if (_offsetTime == 0.0)
					{
						beatmapObject2.StartTime += audioTime;
						beatmapObject2.StartTime += _obj.Offset;
					}
					else
					{
						beatmapObject2.StartTime += _offsetTime;
						++beatmapObject2.editorData.Bin;
					}
				}
				else
				{
					if (_offsetTime == 0.0)
					{
						beatmapObject2.StartTime += _obj.Offset;
					}
					else
					{
						beatmapObject2.StartTime += _offsetTime;
						++beatmapObject2.editorData.Bin;
					}
				}
				if (EditorManager.inst.layer == 5)
				{
					beatmapObject2.editorData.Layer = EditorManager.inst.layer;
				}
				else
				{
					beatmapObject2.editorData.Layer = EditorManager.inst.layer;
				}
				beatmapObject2.fromPrefab = false;
				DataManager.inst.gameData.beatmapObjects.Add(beatmapObject2);
				ObjEditor.ObjectSelection _selection = new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Object, beatmapObject2.id);
				ObjEditor.inst.CreateTimelineObject(_selection);
				ObjEditor.inst.RenderTimelineObject(_selection);
				ObjectManager.inst.updateObjects(_selection);
				if (_select)
				{
					AddSelectedObject(objEditor, _selection);
				}
				delay += 0.0001f;
			}
			Dictionary<string, string> dictionary3 = new Dictionary<string, string>();
			foreach (DataManager.GameData.PrefabObject prefabObject in _obj.prefabObjects)
			{
				string str = LSText.randomString(16);
				dictionary3.Add(prefabObject.ID, str);
			}
			foreach (DataManager.GameData.PrefabObject prefabObject1 in _obj.prefabObjects)
			{
				yield return new WaitForSeconds(delay);
				DataManager.GameData.PrefabObject prefabObject2 = DataManager.GameData.PrefabObject.DeepCopy(prefabObject1, false);
				if (dictionary3.ContainsKey(prefabObject1.ID))
					prefabObject2.ID = dictionary3[prefabObject1.ID];
				prefabObject2.prefabID = prefabObject1.prefabID;
				if (_undone == false)
				{
					if (_offsetTime == 0.0)
					{
						prefabObject2.StartTime += audioTime;
						prefabObject2.StartTime += _obj.Offset;
					}
					else
					{
						prefabObject2.StartTime += _offsetTime;
						++prefabObject2.editorData.Bin;
					}
				}
				else
				{
					if (_offsetTime == 0.0)
					{
						prefabObject2.StartTime += _obj.Offset;
					}
					else
					{
						prefabObject2.StartTime += _offsetTime;
						++prefabObject2.editorData.Bin;
					}
				}
				if (EditorManager.inst.layer == 5)
				{
					prefabObject2.editorData.Layer = EditorManager.inst.lastLayer;
				}
				else
				{
					prefabObject2.editorData.Layer = EditorManager.inst.layer;
				}
				DataManager.inst.gameData.prefabObjects.Add(prefabObject2);
				ObjEditor.ObjectSelection _selection = new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Prefab, prefabObject2.ID);
				ObjEditor.inst.CreateTimelineObject(_selection);
				ObjEditor.inst.RenderTimelineObject(_selection);
				ObjectManager.inst.updateObjects(_selection);
				if (_select)
				{
					AddSelectedObject(objEditor, _selection);
				}
				delay += 0.0001f;
			}

			if (_regen == false)
			{
				EditorManager.inst.DisplayNotification("Pasted Beatmap Object [ " + _obj.Name + " ] and kept Prefab Instance ID [ " + _obj.ID + " ]!", 1f, EditorManager.NotificationType.Success, false);
			}
			else
			{
				EditorManager.inst.DisplayNotification("Pasted Beatmap Object [ " + _obj.Name + " ]!", 1f, EditorManager.NotificationType.Success, false);
			}
			if (_select)
			{
				EditorManager.inst.ShowDialog("Multi Object Editor", false);
			}
			ienumRunning = false;
			yield break;
		}

		public static void CopyEventKeyframes(Dictionary<EventEditor.KeyframeSelection, DataManager.GameData.EventKeyframe> _keyframes)
		{
			_keyframes.Clear();
			float num = float.PositiveInfinity;
			foreach (EventEditor.KeyframeSelection keyframeSelection in EventEditor.inst.keyframeSelections)
			{
				if (DataManager.inst.gameData.eventObjects.allEvents[keyframeSelection.Type][keyframeSelection.Index].eventTime < num)
				{
					num = DataManager.inst.gameData.eventObjects.allEvents[keyframeSelection.Type][keyframeSelection.Index].eventTime;
				}
			}
			foreach (EventEditor.KeyframeSelection keyframeSelection2 in EventEditor.inst.keyframeSelections)
			{
				int type = keyframeSelection2.Type;
				int index = keyframeSelection2.Index;
				DataManager.GameData.EventKeyframe eventKeyframe = DataManager.GameData.EventKeyframe.DeepCopy(DataManager.inst.gameData.eventObjects.allEvents[type][index], true);
				eventKeyframe.eventTime -= num;
				_keyframes.Add(new EventEditor.KeyframeSelection(type, index), eventKeyframe);
			}
		}

		public static IEnumerator PasteEventKeyframes(Dictionary<EventEditor.KeyframeSelection, DataManager.GameData.EventKeyframe> _keyframes)
        {
			float delay = 0f;
			foreach (var keyframeSelection in _keyframes.Keys)
            {
				yield return new WaitForSeconds(delay);
				DataManager.GameData.EventKeyframe eventKeyframe = DataManager.GameData.EventKeyframe.DeepCopy(_keyframes[keyframeSelection], true);
				DataManager.inst.gameData.eventObjects.allEvents[keyframeSelection.Type].Add(eventKeyframe);
				DataManager.inst.gameData.eventObjects.allEvents[keyframeSelection.Type] = (from x in DataManager.inst.gameData.eventObjects.allEvents[keyframeSelection.Type]
																							orderby x.eventTime
																							select x).ToList();
				delay += 0.0001f;
			}
			EventEditor.inst.CreateEventObjects();
			EventManager.inst.updateEvents();
			yield break;
        }
		
		public static void CloseOpenFilePopup()
        {
			var aiGUI = EditorManager.inst.GetDialog("Open File Popup").Dialog.gameObject.GetComponent<AnimateInGUI>();
			aiGUI.OnDisableManual();
		}

		public static IEnumerator SetupTooltips()
        {
			yield return new WaitForSeconds(2f);
			HoverTooltip depthTip = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/depth/depth").GetComponent<HoverTooltip>();
			HoverTooltip timelineTip = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/timeline").GetComponent<HoverTooltip>();
			HoverTooltip textShapeTip = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/shapesettings/5").GetComponent<HoverTooltip>();
			HoverTooltip prefabTip = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject/prefab").GetComponent<HoverTooltip>();
			HoverTooltip objectTip = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject/object").GetComponent<HoverTooltip>();
			HoverTooltip eventTip = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject/event").GetComponent<HoverTooltip>();
			HoverTooltip bgTip = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject/background").GetComponent<HoverTooltip>();

			depthTip.tooltipLangauges.Add(Triggers.NewTooltip("Set the depth layer of the object.", "Depth is if an object shows above or below another object. However, higher number does not equal higher depth here since it's reversed.<br>Higher number = lower depth<br>Lower number = higher depth."));
			timelineTip.tooltipLangauges.Add(Triggers.NewTooltip("Create a keyframe in one of the four keyframe bins by right clicking.", "Each keyframe that controls the objects' base properties like position, scale, rotation and color are located here."));
			textShapeTip.tooltipLangauges.Add(Triggers.NewTooltip("Write your custom text here.", "Anything you write here will show up as a text object. There are a lot of formatting options, such as < b >, < i >, < br >, < color = #FFFFFF > < alpha = #FF > and more. (without the spaces between)"));
			prefabTip.tooltipLangauges.Add(Triggers.NewTooltip("Save groups of objects across levels.", "Prefabs act as a collection of objects that you can easily transfer from one level to the next, or even share online."));
			objectTip.tooltipLangauges.Add(Triggers.NewTooltip("Beatmap Objects.", "The very thing levels are made of!"));
			eventTip.tooltipLangauges.Add(Triggers.NewTooltip("Use Markers to time and separate segments of a level.", "Markers can be helpful towards organizing the level into segments or remembering specific timings. You can also use markers to loop specific parts of the song if you enable it through the EditorManagement Config."));
			bgTip.tooltipLangauges.Add(Triggers.NewTooltip("Create or look at the list of 3D backgrounds here.", "3D backgrounds are completely static, but they can scale up and down to the reactive channels of the music."));

			var eventLabelTip = EventEditor.inst.EventHolders.GetComponent<HoverTooltip>();
			eventLabelTip.tooltipLangauges.Add(Triggers.NewTooltip("Create an event keyframe to spice up your level!", "Each event keyframe type has its own properties that you can utilize."));

			//File Dropdown
			{
				HoverTooltip fileDDTip = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/File").GetComponent<HoverTooltip>();
				HoverTooltip newFileDDTip = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/File/File Dropdown/New Level").GetComponent<HoverTooltip>();
				HoverTooltip openFileDDTip = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/File/File Dropdown/Open").GetComponent<HoverTooltip>();
				HoverTooltip openFolderFileDDTip = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/File/File Dropdown/Open Level Folder").GetComponent<HoverTooltip>();
				HoverTooltip saveFileDDTip = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/File/File Dropdown/Save").GetComponent<HoverTooltip>();
				HoverTooltip saveAsFileDDTip = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/File/File Dropdown/Save As").GetComponent<HoverTooltip>();
				HoverTooltip toggleFileDDTip = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/File/File Dropdown/Toggle Play Mode").GetComponent<HoverTooltip>();
				HoverTooltip quitMenuFileDDTip = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/File/File Dropdown/Quit to Main Menu").GetComponent<HoverTooltip>();
				HoverTooltip quitFileDDTip = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/File/File Dropdown/Quit Game").GetComponent<HoverTooltip>();

				fileDDTip.tooltipLangauges.Add(Triggers.NewTooltip("Show the general options for the editor.", ""));

				newFileDDTip.tooltipLangauges.Add(Triggers.NewTooltip("Create a new level.", "", new List<string>()));
				openFileDDTip.tooltipLangauges.Add(Triggers.NewTooltip("Opens the level list popup, where you can choose a level to open.", "", new List<string> { "Ctrl + O" }));
				openFolderFileDDTip.tooltipLangauges.Add(Triggers.NewTooltip("Opens the folder the current level is located at.", ""));
				saveFileDDTip.tooltipLangauges.Add(Triggers.NewTooltip("Saves the current level and metadata.", "", new List<string> { "Ctrl + S" }));
				saveAsFileDDTip.tooltipLangauges.Add(Triggers.NewTooltip("Saves a copy of the current level.", "", new List<string> { "Alt + S" }));
				toggleFileDDTip.tooltipLangauges.Add(Triggers.NewTooltip("Toggles preview mode.", "", new List<string> { "~" }));
				quitMenuFileDDTip.tooltipLangauges.Add(Triggers.NewTooltip("Quits to main menu.", ""));
				quitFileDDTip.tooltipLangauges.Add(Triggers.NewTooltip("Quits the game entirely.", ""));

				if (EditorPatch.keybindsType != null)
				{
					string newLevelMod = EditorPatch.GetKeyCodeName.Invoke(EditorPatch.keybindsType, new object[] { "New Level", false }).ToString().Replace("Left", "Left ").Replace("Right", "Right ");
					string newLevelMai = EditorPatch.GetKeyCodeName.Invoke(EditorPatch.keybindsType, new object[] { "New Level", true }).ToString().Replace("Left", "Left ").Replace("Right", "Right ");
					newFileDDTip.tooltipLangauges.Clear();
					newFileDDTip.tooltipLangauges.Add(Triggers.NewTooltip("Create a new level.", "", new List<string> { newLevelMod + " + " + newLevelMai }));

					string saveMod = EditorPatch.GetKeyCodeName.Invoke(EditorPatch.keybindsType, new object[] { "New Level", false }).ToString().Replace("Left", "Left ").Replace("Right", "Right ");
					string saveMai = EditorPatch.GetKeyCodeName.Invoke(EditorPatch.keybindsType, new object[] { "New Level", true }).ToString().Replace("Left", "Left ").Replace("Right", "Right ");
					saveFileDDTip.tooltipLangauges.Clear();
					saveFileDDTip.tooltipLangauges.Add(Triggers.NewTooltip("Create a new level.", "", new List<string> { saveMod + " + " + saveMai }));
				}
			}

			//Edit Dropdown
			{
				HoverTooltip undoDDTip = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/Edit/Edit Dropdown/Undo").GetComponent<HoverTooltip>();
				HoverTooltip redoDDTip = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/Edit/Edit Dropdown/Redo").GetComponent<HoverTooltip>();

				undoDDTip.tooltipLangauges.Add(Triggers.NewTooltip("[WIP] Undoes the last action.", "", new List<string> { "Ctrl + Z" }));
				redoDDTip.tooltipLangauges.Add(Triggers.NewTooltip("[WIP] Redoes the last undone action.", "", new List<string> { "Ctrl + Shift + Z" }));
			}

			//View Dropdown
			{
				var objTag = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/View/View Dropdown/Timeline Zoom");
				var plaEdit = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/View/View Dropdown/Grid View");
				var shoHelp = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/View/View Dropdown/Show Help");

				Triggers.AddTooltip(objTag, "Modify objects to do anything you want!", "", new List<string> { "F3" });
				Triggers.AddTooltip(plaEdit, "Create your own player models to use in stories / gameplay.", "", new List<string> { "F6" });
				Triggers.AddTooltip(shoHelp, "Toggles the Info box.", "", new List<string> { "Ctrl + H" });
			}

			List<GameObject> list = (from obj in Resources.FindObjectsOfTypeAll<GameObject>()
									 where obj.GetComponent<HoverTooltip>() != null
									 select obj).ToList();

			foreach (var l in list)
			{
				string n = l.name;
				if (!tooltips.ContainsKey(n))
				{
					tooltips.Add(n, l.GetComponent<HoverTooltip>());
				}
			}
			yield break;
		}

		public static IEnumerator SaveData(string _path, DataManager.GameData _data)
        {
			Debug.Log("Saving Beatmap");
			JSONNode jsonnode = JSON.Parse("{}");
			Debug.Log("Saving Editor Data");
			jsonnode["ed"]["timeline_pos"] = AudioManager.inst.CurrentAudioSource.time.ToString();
			Debug.Log("Saving Markers");
			for (int i = 0; i < _data.beatmapData.markers.Count; i++)
			{
				jsonnode["ed"]["markers"][i]["active"] = "True";
				jsonnode["ed"]["markers"][i]["name"] = _data.beatmapData.markers[i].name.ToString();
				jsonnode["ed"]["markers"][i]["desc"] = _data.beatmapData.markers[i].desc.ToString();
				jsonnode["ed"]["markers"][i]["col"] = _data.beatmapData.markers[i].color.ToString();
				jsonnode["ed"]["markers"][i]["t"] = _data.beatmapData.markers[i].time.ToString();
			}
			Debug.Log("Saving Object Prefabs");
			for (int j = 0; j < _data.prefabObjects.Count; j++)
			{
				jsonnode["prefab_objects"][j]["id"] = _data.prefabObjects[j].ID.ToString();
				jsonnode["prefab_objects"][j]["pid"] = _data.prefabObjects[j].prefabID.ToString();
				jsonnode["prefab_objects"][j]["st"] = _data.prefabObjects[j].StartTime.ToString();
				if (_data.prefabObjects[j].editorData.locked)
				{
					jsonnode["prefab_objects"][j]["ed"]["locked"] = _data.prefabObjects[j].editorData.locked.ToString();
				}
				if (_data.prefabObjects[j].editorData.collapse)
				{
					jsonnode["prefab_objects"][j]["ed"]["shrink"] = _data.prefabObjects[j].editorData.collapse.ToString();
				}
				jsonnode["prefab_objects"][j]["ed"]["layer"] = _data.prefabObjects[j].editorData.Layer.ToString();
				jsonnode["prefab_objects"][j]["ed"]["bin"] = _data.prefabObjects[j].editorData.Bin.ToString();
				jsonnode["prefab_objects"][j]["e"]["pos"]["x"] = _data.prefabObjects[j].events[0].eventValues[0].ToString();
				jsonnode["prefab_objects"][j]["e"]["pos"]["y"] = _data.prefabObjects[j].events[0].eventValues[1].ToString();
				if (_data.prefabObjects[j].events[0].random != 0)
				{
					jsonnode["prefab_objects"][j]["e"]["pos"]["r"] = _data.prefabObjects[j].events[0].random.ToString();
					jsonnode["prefab_objects"][j]["e"]["pos"]["rx"] = _data.prefabObjects[j].events[0].eventRandomValues[0].ToString();
					jsonnode["prefab_objects"][j]["e"]["pos"]["ry"] = _data.prefabObjects[j].events[0].eventRandomValues[1].ToString();
					jsonnode["prefab_objects"][j]["e"]["pos"]["rz"] = _data.prefabObjects[j].events[0].eventRandomValues[2].ToString();
				}
				jsonnode["prefab_objects"][j]["e"]["sca"]["x"] = _data.prefabObjects[j].events[1].eventValues[0].ToString();
				jsonnode["prefab_objects"][j]["e"]["sca"]["y"] = _data.prefabObjects[j].events[1].eventValues[1].ToString();
				if (_data.prefabObjects[j].events[1].random != 0)
				{
					jsonnode["prefab_objects"][j]["e"]["sca"]["r"] = _data.prefabObjects[j].events[1].random.ToString();
					jsonnode["prefab_objects"][j]["e"]["sca"]["rx"] = _data.prefabObjects[j].events[1].eventRandomValues[0].ToString();
					jsonnode["prefab_objects"][j]["e"]["sca"]["ry"] = _data.prefabObjects[j].events[1].eventRandomValues[1].ToString();
					jsonnode["prefab_objects"][j]["e"]["sca"]["rz"] = _data.prefabObjects[j].events[1].eventRandomValues[2].ToString();
				}
				jsonnode["prefab_objects"][j]["e"]["rot"]["x"] = _data.prefabObjects[j].events[2].eventValues[0].ToString();
				if (_data.prefabObjects[j].events[1].random != 0)
				{
					jsonnode["prefab_objects"][j]["e"]["rot"]["r"] = _data.prefabObjects[j].events[2].random.ToString();
					jsonnode["prefab_objects"][j]["e"]["rot"]["rx"] = _data.prefabObjects[j].events[2].eventRandomValues[0].ToString();
					jsonnode["prefab_objects"][j]["e"]["rot"]["rz"] = _data.prefabObjects[j].events[2].eventRandomValues[2].ToString();
				}
			}
			Debug.Log("Saving Level Data");
			jsonnode["level_data"]["level_version"] = _data.beatmapData.levelData.levelVersion.ToString();
			jsonnode["level_data"]["background_color"] = _data.beatmapData.levelData.backgroundColor.ToString();
			jsonnode["level_data"]["follow_player"] = _data.beatmapData.levelData.followPlayer.ToString();
			jsonnode["level_data"]["show_intro"] = _data.beatmapData.levelData.showIntro.ToString();
			Debug.Log("Saving prefabs");
			if (DataManager.inst.gameData.prefabs != null)
			{
				for (int k = 0; k < DataManager.inst.gameData.prefabs.Count; k++)
				{
					jsonnode["prefabs"][k] = DataManager.inst.GeneratePrefabJSON(DataManager.inst.gameData.prefabs[k]);
				}
			}
			Debug.Log("Saving themes");
			if (DataManager.inst.CustomBeatmapThemes != null)
			{
				List<DataManager.BeatmapTheme> levelThemes = new List<DataManager.BeatmapTheme>();

				for (int e = 0; e < DataManager.inst.CustomBeatmapThemes.Count; e++)
				{
					foreach (var keyframe in DataManager.inst.gameData.eventObjects.allEvents[4])
					{
						if (DataManager.inst.CustomBeatmapThemes[e].id == keyframe.eventValues[0].ToString())
						{
							levelThemes.Add(DataManager.inst.CustomBeatmapThemes[e]);
						}
					}
				}

				for (int l = 0; l < levelThemes.Count; l++)
				{
					Debug.LogFormat("{0}Saving " + levelThemes[l].id + " - " + levelThemes[l].name + " to level!", EditorPlugin.className);
					jsonnode["themes"][l]["id"] = levelThemes[l].id;
					jsonnode["themes"][l]["name"] = levelThemes[l].name;
					jsonnode["themes"][l]["gui"] = ColorToHex(levelThemes[l].guiColor);
					jsonnode["themes"][l]["bg"] = LSColors.ColorToHex(levelThemes[l].backgroundColor);
					for (int m = 0; m < levelThemes[l].playerColors.Count; m++)
					{
						jsonnode["themes"][l]["players"][m] = ColorToHex(levelThemes[l].playerColors[m]);
					}
					for (int n = 0; n < levelThemes[l].objectColors.Count; n++)
					{
						jsonnode["themes"][l]["objs"][n] = ColorToHex(levelThemes[l].objectColors[n]);
					}
					for (int num = 0; num < levelThemes[l].backgroundColors.Count; num++)
					{
						jsonnode["themes"][l]["bgs"][num] = ColorToHex(levelThemes[l].backgroundColors[num]);
					}
				}
			}
			Debug.Log("Saving Checkpoints");
			for (int num2 = 0; num2 < _data.beatmapData.checkpoints.Count; num2++)
			{
				jsonnode["checkpoints"][num2]["active"] = "False";
				jsonnode["checkpoints"][num2]["name"] = _data.beatmapData.checkpoints[num2].name;
				jsonnode["checkpoints"][num2]["t"] = _data.beatmapData.checkpoints[num2].time.ToString();
				jsonnode["checkpoints"][num2]["pos"]["x"] = _data.beatmapData.checkpoints[num2].pos.x.ToString();
				jsonnode["checkpoints"][num2]["pos"]["y"] = _data.beatmapData.checkpoints[num2].pos.y.ToString();
			}
			Debug.Log("Saving Beatmap Objects");
			if (_data.beatmapObjects != null)
			{
				List<DataManager.GameData.BeatmapObject> list = _data.beatmapObjects.FindAll((DataManager.GameData.BeatmapObject x) => !x.fromPrefab);
				jsonnode["beatmap_objects"] = new JSONArray();
				for (int num3 = 0; num3 < list.Count; num3++)
				{
					if (list[num3] != null && list[num3].events != null && !list[num3].fromPrefab)
					{
						jsonnode["beatmap_objects"][num3]["id"] = list[num3].id;
						if (!string.IsNullOrEmpty(list[num3].prefabID))
						{
							jsonnode["beatmap_objects"][num3]["pid"] = list[num3].prefabID;
						}
						if (!string.IsNullOrEmpty(list[num3].prefabInstanceID))
						{
							jsonnode["beatmap_objects"][num3]["piid"] = list[num3].prefabInstanceID;
						}
						if (list[num3].GetParentType().ToString() != "101")
						{
							jsonnode["beatmap_objects"][num3]["pt"] = list[num3].GetParentType().ToString();
						}
						if (list[num3].getParentOffsets().FindIndex((float x) => x != 0f) != -1)
						{
							int num4 = 0;
							foreach (float num5 in list[num3].getParentOffsets())
							{
								jsonnode["beatmap_objects"][num3]["po"][num4] = num5.ToString();
								num4++;
							}
						}
						jsonnode["beatmap_objects"][num3]["p"] = list[num3].parent.ToString();
						jsonnode["beatmap_objects"][num3]["d"] = list[num3].Depth.ToString();
						jsonnode["beatmap_objects"][num3]["st"] = list[num3].StartTime.ToString();
						if (!string.IsNullOrEmpty(list[num3].name))
						{
							jsonnode["beatmap_objects"][num3]["name"] = list[num3].name;
						}
						jsonnode["beatmap_objects"][num3]["ot"] = (int)list[num3].objectType;
						jsonnode["beatmap_objects"][num3]["akt"] = (int)list[num3].autoKillType;
						jsonnode["beatmap_objects"][num3]["ako"] = list[num3].autoKillOffset;
						if (list[num3].shape != 0)
						{
							jsonnode["beatmap_objects"][num3]["shape"] = list[num3].shape.ToString();
						}
						if (list[num3].shapeOption != 0)
						{
							jsonnode["beatmap_objects"][num3]["so"] = list[num3].shapeOption.ToString();
						}
						if (!string.IsNullOrEmpty(list[num3].text))
						{
							jsonnode["beatmap_objects"][num3]["text"] = list[num3].text;
						}
						jsonnode["beatmap_objects"][num3]["o"]["x"] = list[num3].origin.x.ToString();
						jsonnode["beatmap_objects"][num3]["o"]["y"] = list[num3].origin.y.ToString();
						if (list[num3].editorData.locked)
						{
							jsonnode["beatmap_objects"][num3]["ed"]["locked"] = list[num3].editorData.locked.ToString();
						}
						if (list[num3].editorData.collapse)
						{
							jsonnode["beatmap_objects"][num3]["ed"]["shrink"] = list[num3].editorData.collapse.ToString();
						}
						jsonnode["beatmap_objects"][num3]["ed"]["bin"] = list[num3].editorData.Bin.ToString();
						jsonnode["beatmap_objects"][num3]["ed"]["layer"] = list[num3].editorData.Layer.ToString();
						jsonnode["beatmap_objects"][num3]["events"]["pos"] = new JSONArray();
						for (int num6 = 0; num6 < list[num3].events[0].Count; num6++)
						{
							jsonnode["beatmap_objects"][num3]["events"]["pos"][num6]["t"] = list[num3].events[0][num6].eventTime.ToString();
							jsonnode["beatmap_objects"][num3]["events"]["pos"][num6]["x"] = list[num3].events[0][num6].eventValues[0].ToString();
							jsonnode["beatmap_objects"][num3]["events"]["pos"][num6]["y"] = list[num3].events[0][num6].eventValues[1].ToString();

							//Position Z
							if (list[num3].events[0][num6].eventValues.Length > 2)
							{
								jsonnode["beatmap_objects"][num3]["events"]["pos"][num6]["z"] = list[num3].events[0][num6].eventValues[2].ToString();
							}

							if (list[num3].events[0][num6].curveType.Name != "Linear")
							{
								jsonnode["beatmap_objects"][num3]["events"]["pos"][num6]["ct"] = list[num3].events[0][num6].curveType.Name.ToString();
							}
							if (list[num3].events[0][num6].random != 0)
							{
								jsonnode["beatmap_objects"][num3]["events"]["pos"][num6]["r"] = list[num3].events[0][num6].random.ToString();
								jsonnode["beatmap_objects"][num3]["events"]["pos"][num6]["rx"] = list[num3].events[0][num6].eventRandomValues[0].ToString();
								jsonnode["beatmap_objects"][num3]["events"]["pos"][num6]["ry"] = list[num3].events[0][num6].eventRandomValues[1].ToString();
								jsonnode["beatmap_objects"][num3]["events"]["pos"][num6]["rz"] = list[num3].events[0][num6].eventRandomValues[2].ToString();
							}
						}
						jsonnode["beatmap_objects"][num3]["events"]["sca"] = new JSONArray();
						for (int num7 = 0; num7 < list[num3].events[1].Count; num7++)
						{
							jsonnode["beatmap_objects"][num3]["events"]["sca"][num7]["t"] = list[num3].events[1][num7].eventTime.ToString();
							jsonnode["beatmap_objects"][num3]["events"]["sca"][num7]["x"] = list[num3].events[1][num7].eventValues[0].ToString();
							jsonnode["beatmap_objects"][num3]["events"]["sca"][num7]["y"] = list[num3].events[1][num7].eventValues[1].ToString();
							if (list[num3].events[1][num7].curveType.Name != "Linear")
							{
								jsonnode["beatmap_objects"][num3]["events"]["sca"][num7]["ct"] = list[num3].events[1][num7].curveType.Name.ToString();
							}
							if (list[num3].events[1][num7].random != 0)
							{
								jsonnode["beatmap_objects"][num3]["events"]["sca"][num7]["r"] = list[num3].events[1][num7].random.ToString();
								jsonnode["beatmap_objects"][num3]["events"]["sca"][num7]["rx"] = list[num3].events[1][num7].eventRandomValues[0].ToString();
								jsonnode["beatmap_objects"][num3]["events"]["sca"][num7]["ry"] = list[num3].events[1][num7].eventRandomValues[1].ToString();
								jsonnode["beatmap_objects"][num3]["events"]["sca"][num7]["rz"] = list[num3].events[1][num7].eventRandomValues[2].ToString();
							}
						}
						jsonnode["beatmap_objects"][num3]["events"]["rot"] = new JSONArray();
						for (int num8 = 0; num8 < list[num3].events[2].Count; num8++)
						{
							jsonnode["beatmap_objects"][num3]["events"]["rot"][num8]["t"] = list[num3].events[2][num8].eventTime.ToString();
							jsonnode["beatmap_objects"][num3]["events"]["rot"][num8]["x"] = list[num3].events[2][num8].eventValues[0].ToString();
							if (list[num3].events[2][num8].curveType.Name != "Linear")
							{
								jsonnode["beatmap_objects"][num3]["events"]["rot"][num8]["ct"] = list[num3].events[2][num8].curveType.Name.ToString();
							}
							if (list[num3].events[2][num8].random != 0)
							{
								jsonnode["beatmap_objects"][num3]["events"]["rot"][num8]["r"] = list[num3].events[2][num8].random.ToString();
								jsonnode["beatmap_objects"][num3]["events"]["rot"][num8]["rx"] = list[num3].events[2][num8].eventRandomValues[0].ToString();
								jsonnode["beatmap_objects"][num3]["events"]["rot"][num8]["rz"] = list[num3].events[2][num8].eventRandomValues[2].ToString();
							}
						}
						jsonnode["beatmap_objects"][num3]["events"]["col"] = new JSONArray();
						for (int num9 = 0; num9 < list[num3].events[3].Count; num9++)
						{
							jsonnode["beatmap_objects"][num3]["events"]["col"][num9]["t"] = list[num3].events[3][num9].eventTime.ToString();
							jsonnode["beatmap_objects"][num3]["events"]["col"][num9]["x"] = list[num3].events[3][num9].eventValues[0].ToString();
							if (list[num3].events[3][num9].curveType.Name != "Linear")
							{
								jsonnode["beatmap_objects"][num3]["events"]["col"][num9]["ct"] = list[num3].events[3][num9].curveType.Name.ToString();
							}
							if (list[num3].events[3][num9].random != 0)
							{
								jsonnode["beatmap_objects"][num3]["events"]["col"][num9]["r"] = list[num3].events[3][num9].random.ToString();
								jsonnode["beatmap_objects"][num3]["events"]["col"][num9]["rx"] = list[num3].events[3][num9].eventRandomValues[0].ToString();
							}
						}
					}
				}
			}
			else
			{
				Debug.Log("skipping objects");
				jsonnode["beatmap_objects"] = new JSONArray();
			}
			Debug.Log("Saving Background Objects");
			for (int num10 = 0; num10 < _data.backgroundObjects.Count; num10++)
			{
				jsonnode["bg_objects"][num10]["active"] = _data.backgroundObjects[num10].active.ToString();
				jsonnode["bg_objects"][num10]["name"] = _data.backgroundObjects[num10].name.ToString();
				jsonnode["bg_objects"][num10]["kind"] = _data.backgroundObjects[num10].kind.ToString();
				jsonnode["bg_objects"][num10]["pos"]["x"] = _data.backgroundObjects[num10].pos.x.ToString();
				jsonnode["bg_objects"][num10]["pos"]["y"] = _data.backgroundObjects[num10].pos.y.ToString();
				jsonnode["bg_objects"][num10]["size"]["x"] = _data.backgroundObjects[num10].scale.x.ToString();
				jsonnode["bg_objects"][num10]["size"]["y"] = _data.backgroundObjects[num10].scale.y.ToString();
				jsonnode["bg_objects"][num10]["rot"] = _data.backgroundObjects[num10].rot.ToString();
				jsonnode["bg_objects"][num10]["color"] = _data.backgroundObjects[num10].color.ToString();
				jsonnode["bg_objects"][num10]["layer"] = _data.backgroundObjects[num10].layer.ToString();
				jsonnode["bg_objects"][num10]["fade"] = _data.backgroundObjects[num10].drawFade.ToString();
				if (_data.backgroundObjects[num10].reactive)
				{
					jsonnode["bg_objects"][num10]["r_set"]["type"] = _data.backgroundObjects[num10].reactiveType.ToString();
					jsonnode["bg_objects"][num10]["r_set"]["scale"] = _data.backgroundObjects[num10].reactiveScale.ToString();
				}
			}
			Debug.Log("Saving Event Objects");
			for (int num11 = 0; num11 < _data.eventObjects.allEvents[0].Count(); num11++)
			{
				jsonnode["events"]["pos"][num11]["t"] = _data.eventObjects.allEvents[0][num11].eventTime.ToString();
				jsonnode["events"]["pos"][num11]["x"] = _data.eventObjects.allEvents[0][num11].eventValues[0].ToString();
				jsonnode["events"]["pos"][num11]["y"] = _data.eventObjects.allEvents[0][num11].eventValues[1].ToString();
				if (_data.eventObjects.allEvents[0][num11].curveType.Name != "Linear")
				{
					jsonnode["events"]["pos"][num11]["ct"] = _data.eventObjects.allEvents[0][num11].curveType.Name.ToString();
				}
				if (_data.eventObjects.allEvents[0][num11].random != 0)
				{
					jsonnode["events"]["pos"][num11]["r"] = _data.eventObjects.allEvents[0][num11].random.ToString();
					jsonnode["events"]["pos"][num11]["rx"] = _data.eventObjects.allEvents[0][num11].eventRandomValues[0].ToString();
					jsonnode["events"]["pos"][num11]["ry"] = _data.eventObjects.allEvents[0][num11].eventRandomValues[1].ToString();
				}
			}
			for (int num12 = 0; num12 < _data.eventObjects.allEvents[1].Count(); num12++)
			{
				jsonnode["events"]["zoom"][num12]["t"] = _data.eventObjects.allEvents[1][num12].eventTime.ToString();
				jsonnode["events"]["zoom"][num12]["x"] = _data.eventObjects.allEvents[1][num12].eventValues[0].ToString();
				if (_data.eventObjects.allEvents[1][num12].curveType.Name != "Linear")
				{
					jsonnode["events"]["zoom"][num12]["ct"] = _data.eventObjects.allEvents[1][num12].curveType.Name.ToString();
				}
				if (_data.eventObjects.allEvents[1][num12].random != 0)
				{
					jsonnode["events"]["zoom"][num12]["r"] = _data.eventObjects.allEvents[1][num12].random.ToString();
					jsonnode["events"]["zoom"][num12]["rx"] = _data.eventObjects.allEvents[1][num12].eventRandomValues[0].ToString();
				}
			}
			for (int num13 = 0; num13 < _data.eventObjects.allEvents[2].Count(); num13++)
			{
				jsonnode["events"]["rot"][num13]["t"] = _data.eventObjects.allEvents[2][num13].eventTime.ToString();
				jsonnode["events"]["rot"][num13]["x"] = _data.eventObjects.allEvents[2][num13].eventValues[0].ToString();
				if (_data.eventObjects.allEvents[2][num13].curveType.Name != "Linear")
				{
					jsonnode["events"]["rot"][num13]["ct"] = _data.eventObjects.allEvents[2][num13].curveType.Name.ToString();
				}
				if (_data.eventObjects.allEvents[2][num13].random != 0)
				{
					jsonnode["events"]["rot"][num13]["r"] = _data.eventObjects.allEvents[2][num13].random.ToString();
					jsonnode["events"]["rot"][num13]["rx"] = _data.eventObjects.allEvents[2][num13].eventRandomValues[0].ToString();
				}
			}
			for (int num14 = 0; num14 < _data.eventObjects.allEvents[3].Count(); num14++)
			{
				jsonnode["events"]["shake"][num14]["t"] = _data.eventObjects.allEvents[3][num14].eventTime.ToString();
				jsonnode["events"]["shake"][num14]["x"] = _data.eventObjects.allEvents[3][num14].eventValues[0].ToString();
				jsonnode["events"]["shake"][num14]["y"] = _data.eventObjects.allEvents[3][num14].eventValues[1].ToString();
				if (_data.eventObjects.allEvents[3][num14].curveType.Name != "Linear")
				{
					jsonnode["events"]["shake"][num14]["ct"] = _data.eventObjects.allEvents[3][num14].curveType.Name.ToString();
				}
				if (_data.eventObjects.allEvents[3][num14].random != 0)
				{
					jsonnode["events"]["shake"][num14]["r"] = _data.eventObjects.allEvents[3][num14].random.ToString();
					jsonnode["events"]["shake"][num14]["rx"] = _data.eventObjects.allEvents[3][num14].eventRandomValues[0].ToString();
					jsonnode["events"]["shake"][num14]["ry"] = _data.eventObjects.allEvents[3][num14].eventRandomValues[1].ToString();
				}
			}
			for (int num15 = 0; num15 < _data.eventObjects.allEvents[4].Count(); num15++)
			{
				jsonnode["events"]["theme"][num15]["t"] = _data.eventObjects.allEvents[4][num15].eventTime.ToString();
				jsonnode["events"]["theme"][num15]["x"] = _data.eventObjects.allEvents[4][num15].eventValues[0].ToString();
				if (_data.eventObjects.allEvents[4][num15].curveType.Name != "Linear")
				{
					jsonnode["events"]["theme"][num15]["ct"] = _data.eventObjects.allEvents[4][num15].curveType.Name.ToString();
				}
				if (_data.eventObjects.allEvents[4][num15].random != 0)
				{
					jsonnode["events"]["theme"][num15]["r"] = _data.eventObjects.allEvents[4][num15].random.ToString();
					jsonnode["events"]["theme"][num15]["rx"] = _data.eventObjects.allEvents[4][num15].eventRandomValues[0].ToString();
				}
			}
			for (int num16 = 0; num16 < _data.eventObjects.allEvents[5].Count(); num16++)
			{
				jsonnode["events"]["chroma"][num16]["t"] = _data.eventObjects.allEvents[5][num16].eventTime.ToString();
				jsonnode["events"]["chroma"][num16]["x"] = _data.eventObjects.allEvents[5][num16].eventValues[0].ToString();
				if (_data.eventObjects.allEvents[5][num16].curveType.Name != "Linear")
				{
					jsonnode["events"]["chroma"][num16]["ct"] = _data.eventObjects.allEvents[5][num16].curveType.Name.ToString();
				}
				if (_data.eventObjects.allEvents[5][num16].random != 0)
				{
					jsonnode["events"]["chroma"][num16]["r"] = _data.eventObjects.allEvents[5][num16].random.ToString();
					jsonnode["events"]["chroma"][num16]["rx"] = _data.eventObjects.allEvents[5][num16].eventRandomValues[0].ToString();
				}
			}
			for (int num17 = 0; num17 < _data.eventObjects.allEvents[6].Count(); num17++)
			{
				jsonnode["events"]["bloom"][num17]["t"] = _data.eventObjects.allEvents[6][num17].eventTime.ToString();
				jsonnode["events"]["bloom"][num17]["x"] = _data.eventObjects.allEvents[6][num17].eventValues[0].ToString();
				if (_data.eventObjects.allEvents[6][num17].curveType.Name != "Linear")
				{
					jsonnode["events"]["bloom"][num17]["ct"] = _data.eventObjects.allEvents[6][num17].curveType.Name.ToString();
				}
				if (_data.eventObjects.allEvents[6][num17].random != 0)
				{
					jsonnode["events"]["bloom"][num17]["r"] = _data.eventObjects.allEvents[6][num17].random.ToString();
					jsonnode["events"]["bloom"][num17]["rx"] = _data.eventObjects.allEvents[6][num17].eventRandomValues[0].ToString();
				}
			}
			for (int num18 = 0; num18 < _data.eventObjects.allEvents[7].Count(); num18++)
			{
				jsonnode["events"]["vignette"][num18]["t"] = _data.eventObjects.allEvents[7][num18].eventTime.ToString();
				jsonnode["events"]["vignette"][num18]["x"] = _data.eventObjects.allEvents[7][num18].eventValues[0].ToString();
				jsonnode["events"]["vignette"][num18]["y"] = _data.eventObjects.allEvents[7][num18].eventValues[1].ToString();
				jsonnode["events"]["vignette"][num18]["z"] = _data.eventObjects.allEvents[7][num18].eventValues[2].ToString();
				jsonnode["events"]["vignette"][num18]["x2"] = _data.eventObjects.allEvents[7][num18].eventValues[3].ToString();
				jsonnode["events"]["vignette"][num18]["y2"] = _data.eventObjects.allEvents[7][num18].eventValues[4].ToString();
				jsonnode["events"]["vignette"][num18]["z2"] = _data.eventObjects.allEvents[7][num18].eventValues[5].ToString();
				if (_data.eventObjects.allEvents[7][num18].curveType.Name != "Linear")
				{
					jsonnode["events"]["vignette"][num18]["ct"] = _data.eventObjects.allEvents[7][num18].curveType.Name.ToString();
				}
				if (_data.eventObjects.allEvents[7][num18].random != 0)
				{
					jsonnode["events"]["vignette"][num18]["r"] = _data.eventObjects.allEvents[7][num18].random.ToString();
					jsonnode["events"]["vignette"][num18]["rx"] = _data.eventObjects.allEvents[7][num18].eventRandomValues[0].ToString();
					jsonnode["events"]["vignette"][num18]["ry"] = _data.eventObjects.allEvents[7][num18].eventRandomValues[1].ToString();
					jsonnode["events"]["vignette"][num18]["value_random_z"] = _data.eventObjects.allEvents[7][num18].eventRandomValues[2].ToString();
					jsonnode["events"]["vignette"][num18]["value_random_x2"] = _data.eventObjects.allEvents[7][num18].eventRandomValues[3].ToString();
					jsonnode["events"]["vignette"][num18]["value_random_y2"] = _data.eventObjects.allEvents[7][num18].eventRandomValues[4].ToString();
					jsonnode["events"]["vignette"][num18]["value_random_z2"] = _data.eventObjects.allEvents[7][num18].eventRandomValues[5].ToString();
				}
			}
			for (int num19 = 0; num19 < _data.eventObjects.allEvents[8].Count(); num19++)
			{
				jsonnode["events"]["lens"][num19]["t"] = _data.eventObjects.allEvents[8][num19].eventTime.ToString();
				jsonnode["events"]["lens"][num19]["x"] = _data.eventObjects.allEvents[8][num19].eventValues[0].ToString();
				if (_data.eventObjects.allEvents[8][num19].curveType.Name != "Linear")
				{
					jsonnode["events"]["lens"][num19]["ct"] = _data.eventObjects.allEvents[8][num19].curveType.Name.ToString();
				}
				if (_data.eventObjects.allEvents[8][num19].random != 0)
				{
					jsonnode["events"]["lens"][num19]["r"] = _data.eventObjects.allEvents[8][num19].random.ToString();
					jsonnode["events"]["lens"][num19]["rx"] = _data.eventObjects.allEvents[8][num19].eventRandomValues[0].ToString();
				}
			}
			for (int num20 = 0; num20 < _data.eventObjects.allEvents[9].Count(); num20++)
			{
				jsonnode["events"]["grain"][num20]["t"] = _data.eventObjects.allEvents[9][num20].eventTime.ToString();
				jsonnode["events"]["grain"][num20]["x"] = _data.eventObjects.allEvents[9][num20].eventValues[0].ToString();
				jsonnode["events"]["grain"][num20]["y"] = _data.eventObjects.allEvents[9][num20].eventValues[1].ToString();
				jsonnode["events"]["grain"][num20]["z"] = _data.eventObjects.allEvents[9][num20].eventValues[2].ToString();
				if (_data.eventObjects.allEvents[9][num20].curveType.Name != "Linear")
				{
					jsonnode["events"]["grain"][num20]["ct"] = _data.eventObjects.allEvents[9][num20].curveType.Name.ToString();
				}
				if (_data.eventObjects.allEvents[9][num20].random != 0)
				{
					jsonnode["events"]["grain"][num20]["r"] = _data.eventObjects.allEvents[9][num20].random.ToString();
					jsonnode["events"]["grain"][num20]["rx"] = _data.eventObjects.allEvents[9][num20].eventRandomValues[0].ToString();
					jsonnode["events"]["grain"][num20]["ry"] = _data.eventObjects.allEvents[9][num20].eventRandomValues[1].ToString();
					jsonnode["events"]["grain"][num20]["value_random_z"] = _data.eventObjects.allEvents[9][num20].eventRandomValues[2].ToString();
				}
			}
			if (_data.eventObjects.allEvents.Count > 10)
			{
				for (int num21 = 0; num21 < _data.eventObjects.allEvents[10].Count(); num21++)
				{
					jsonnode["events"]["cg"][num21]["t"] = _data.eventObjects.allEvents[10][num21].eventTime.ToString();
					jsonnode["events"]["cg"][num21]["x"] = _data.eventObjects.allEvents[10][num21].eventValues[0].ToString();
					jsonnode["events"]["cg"][num21]["y"] = _data.eventObjects.allEvents[10][num21].eventValues[1].ToString();
					jsonnode["events"]["cg"][num21]["z"] = _data.eventObjects.allEvents[10][num21].eventValues[2].ToString();
					jsonnode["events"]["cg"][num21]["x2"] = _data.eventObjects.allEvents[10][num21].eventValues[3].ToString();
					jsonnode["events"]["cg"][num21]["y2"] = _data.eventObjects.allEvents[10][num21].eventValues[4].ToString();
					jsonnode["events"]["cg"][num21]["z2"] = _data.eventObjects.allEvents[10][num21].eventValues[5].ToString();
					jsonnode["events"]["cg"][num21]["x3"] = _data.eventObjects.allEvents[10][num21].eventValues[6].ToString();
					jsonnode["events"]["cg"][num21]["y3"] = _data.eventObjects.allEvents[10][num21].eventValues[7].ToString();
					jsonnode["events"]["cg"][num21]["z3"] = _data.eventObjects.allEvents[10][num21].eventValues[8].ToString();
					if (_data.eventObjects.allEvents[10][num21].curveType.Name != "Linear")
					{
						jsonnode["events"]["cg"][num21]["ct"] = _data.eventObjects.allEvents[10][num21].curveType.Name.ToString();
					}
					if (_data.eventObjects.allEvents[10][num21].random != 0)
					{
						jsonnode["events"]["cg"][num21]["r"] = _data.eventObjects.allEvents[10][num21].random.ToString();
						jsonnode["events"]["cg"][num21]["rx"] = _data.eventObjects.allEvents[10][num21].eventRandomValues[0].ToString();
						jsonnode["events"]["cg"][num21]["ry"] = _data.eventObjects.allEvents[10][num21].eventRandomValues[1].ToString();
					}
				}
				for (int num21 = 0; num21 < _data.eventObjects.allEvents[11].Count(); num21++)
				{
					jsonnode["events"]["rip"][num21]["t"] = _data.eventObjects.allEvents[11][num21].eventTime.ToString();
					jsonnode["events"]["rip"][num21]["x"] = _data.eventObjects.allEvents[11][num21].eventValues[0].ToString();
					jsonnode["events"]["rip"][num21]["y"] = _data.eventObjects.allEvents[11][num21].eventValues[1].ToString();
					jsonnode["events"]["rip"][num21]["z"] = _data.eventObjects.allEvents[11][num21].eventValues[2].ToString();
					jsonnode["events"]["rip"][num21]["x2"] = _data.eventObjects.allEvents[11][num21].eventValues[3].ToString();
					jsonnode["events"]["rip"][num21]["y2"] = _data.eventObjects.allEvents[11][num21].eventValues[4].ToString();
					if (_data.eventObjects.allEvents[11][num21].curveType.Name != "Linear")
					{
						jsonnode["events"]["rip"][num21]["ct"] = _data.eventObjects.allEvents[11][num21].curveType.Name.ToString();
					}
					if (_data.eventObjects.allEvents[11][num21].random != 0)
					{
						jsonnode["events"]["rip"][num21]["r"] = _data.eventObjects.allEvents[11][num21].random.ToString();
						jsonnode["events"]["rip"][num21]["rx"] = _data.eventObjects.allEvents[11][num21].eventRandomValues[0].ToString();
						jsonnode["events"]["rip"][num21]["ry"] = _data.eventObjects.allEvents[11][num21].eventRandomValues[1].ToString();
					}
				}
				for (int num21 = 0; num21 < _data.eventObjects.allEvents[12].Count(); num21++)
				{
					jsonnode["events"]["rb"][num21]["t"] = _data.eventObjects.allEvents[12][num21].eventTime.ToString();
					jsonnode["events"]["rb"][num21]["x"] = _data.eventObjects.allEvents[12][num21].eventValues[0].ToString();
					jsonnode["events"]["rb"][num21]["y"] = _data.eventObjects.allEvents[12][num21].eventValues[1].ToString();
					if (_data.eventObjects.allEvents[12][num21].curveType.Name != "Linear")
					{
						jsonnode["events"]["rb"][num21]["ct"] = _data.eventObjects.allEvents[12][num21].curveType.Name.ToString();
					}
					if (_data.eventObjects.allEvents[12][num21].random != 0)
					{
						jsonnode["events"]["rb"][num21]["r"] = _data.eventObjects.allEvents[12][num21].random.ToString();
						jsonnode["events"]["rb"][num21]["rx"] = _data.eventObjects.allEvents[12][num21].eventRandomValues[0].ToString();
						jsonnode["events"]["rb"][num21]["ry"] = _data.eventObjects.allEvents[12][num21].eventRandomValues[1].ToString();
					}
				}
				for (int num21 = 0; num21 < _data.eventObjects.allEvents[13].Count(); num21++)
				{
					jsonnode["events"]["cs"][num21]["t"] = _data.eventObjects.allEvents[13][num21].eventTime.ToString();
					jsonnode["events"]["cs"][num21]["x"] = _data.eventObjects.allEvents[13][num21].eventValues[0].ToString();
					jsonnode["events"]["cs"][num21]["y"] = _data.eventObjects.allEvents[13][num21].eventValues[1].ToString();
					if (_data.eventObjects.allEvents[13][num21].curveType.Name != "Linear")
					{
						jsonnode["events"]["cs"][num21]["ct"] = _data.eventObjects.allEvents[13][num21].curveType.Name.ToString();
					}
					if (_data.eventObjects.allEvents[13][num21].random != 0)
					{
						jsonnode["events"]["cs"][num21]["r"] = _data.eventObjects.allEvents[13][num21].random.ToString();
						jsonnode["events"]["cs"][num21]["rx"] = _data.eventObjects.allEvents[13][num21].eventRandomValues[0].ToString();
						jsonnode["events"]["cs"][num21]["ry"] = _data.eventObjects.allEvents[13][num21].eventRandomValues[1].ToString();
					}
				}
			}
			Debug.Log("Saving Entire Beatmap");
			Debug.Log("Path: " + _path);
			RTFile.WriteToFile(_path, jsonnode.ToString());
			yield break;
        }

		public static bool themesLoading = false;

		public static IEnumerator LoadThemes()
		{
			themesLoading = true;
			float delay = 0f;
			var dataManager = DataManager.inst;
			var fileManager = FileManager.inst;
			dataManager.CustomBeatmapThemes.Clear();
			dataManager.BeatmapThemeIDToIndex.Clear();
			dataManager.BeatmapThemeIndexToID.Clear();
			int num = 0;
			foreach (DataManager.BeatmapTheme beatmapTheme in DataManager.inst.BeatmapThemes)
			{
				yield return new WaitForSeconds(delay);
				dataManager.BeatmapThemeIDToIndex.Add(num, num);
				dataManager.BeatmapThemeIndexToID.Add(num, num);
				delay += 0.0001f;
				num++;
			}
			var folders = fileManager.GetFileList(EditorPlugin.themeListPath, "lst");
			folders = (from x in folders
					orderby x.Name.ToLower()
					select x).ToList();

			while (folders.Count <= 0)
			{
				yield return null;
			}

			foreach (var folder in folders)
			{
				yield return new WaitForSeconds(delay);
				var lsfile = folder;
				var jn = JSON.Parse(FileManager.inst.LoadJSONFileRaw(lsfile.FullPath));
				var orig = DataManager.BeatmapTheme.Parse(jn, true);
				if (jn["id"] == null)
				{
					var beatmapTheme2 = DataManager.BeatmapTheme.DeepCopy(orig, false);
					beatmapTheme2.id = LSText.randomNumString(6);
					dataManager.BeatmapThemes.Remove(orig);
					fileManager.DeleteFileRaw(lsfile.FullPath);
					ThemeEditor.inst.SaveTheme(beatmapTheme2);
					dataManager.BeatmapThemes.Add(beatmapTheme2);
				}
				delay += 0.0001f;
			}
			themesLoading = false;
			yield break;
		}

		public static IEnumerator LoadLevel(EditorManager __instance, string _levelName)
		{
			var objectManager = ObjectManager.inst;
			var objEditor = ObjEditor.inst;
			var gameManager = GameManager.inst;
			var dataManager = DataManager.inst;

			objectManager.PurgeObjects();
			SetAutosave();
			__instance.InvokeRepeating("LoadingIconUpdate", 0f, 0.05f);
			__instance.currentLoadedLevel = _levelName;
			__instance.SetPitch(1f);
			__instance.timelineScrollbar.GetComponent<Scrollbar>().value = 0f;
			gameManager.gameState = GameManager.State.Loading;
			string rawJSON = null;
			string rawMetadataJSON = null;
			AudioClip song = null;
			string text = EditorPlugin.levelListSlash + _levelName + "/";
			__instance.ClearDialogs(new EditorManager.EditorDialog.DialogType[1]);
			__instance.ShowDialog("File Info Popup");

			var fileInfo = __instance.GetDialog("File Info Popup").Dialog.transform.Find("text").GetComponent<Text>();

			fileInfo.text = "Loading Level Data for [" + _levelName + "]";

			Debug.LogFormat("{0}Loading {1}...", EditorPlugin.className, text);
			rawJSON = FileManager.inst.LoadJSONFile(text + "level.lsb");
			rawMetadataJSON = FileManager.inst.LoadJSONFile(text + "metadata.lsb");

			if (string.IsNullOrEmpty(rawMetadataJSON))
			{
				dataManager.SaveMetadata(text + "metadata.lsb");
			}

			gameManager.path = text + "level.lsb";
			gameManager.basePath = text;
			gameManager.levelName = _levelName;
			fileInfo.text = "Loading Level Music for [" + _levelName + "]\n\nIf this is taking more than a minute or two check if the .ogg file is corrupt.";

			Debug.LogFormat("{0}Loading audio for {1}...", EditorPlugin.className, _levelName);
			bool loadedAudio = false;
			if (RTFile.FileExists(text + "level.ogg") && !RTFile.FileExists(text + "level.wav") && !loadedAudio)
			{
				yield return inst.StartCoroutine(FileManager.inst.LoadMusicFile(text + "level.ogg", delegate (AudioClip _song)
				{
					_song.name = _levelName;
					if (_song)
					{
						song = _song;
					}
				}));
				loadedAudio = true;
			}

			if (!RTFile.FileExists(text + "level.ogg") && RTFile.FileExists(text + "level.wav") && !loadedAudio)
			{
				yield return inst.StartCoroutine(FileManager.inst.LoadMusicFile(text + "level.wav", delegate (AudioClip _song)
				{
					_song.name = _levelName;
					if (_song)
					{
						song = _song;
					}
				}));
				loadedAudio = true;
			}

			Debug.LogFormat("{0}Parsing level data for {1}...", EditorPlugin.className, _levelName);
			gameManager.gameState = GameManager.State.Parsing;
			fileInfo.text = "Parsing Level Data for [" + _levelName + "]";
			if (!string.IsNullOrEmpty(rawJSON) && !string.IsNullOrEmpty(rawMetadataJSON))
			{
				dataManager.ParseMetadata(rawMetadataJSON, true);
				rawJSON = dataManager.gameData.UpdateBeatmap(rawJSON, DataManager.inst.metaData.beatmap.game_version);
				dataManager.gameData.eventObjects = new DataManager.GameData.EventObjects();
				inst.StartCoroutine(RTFile.ParseBeatmap(rawJSON, true));
			}

			//fileInfo.text = "Loading Themes for [" + _levelName + "]";
			//Debug.LogFormat("{0}Loading themes for {1}...", EditorPlugin.className, _levelName);
			//yield return inst.StartCoroutine(LoadThemes());
			//float delayTheme = 0f;
			//while (themesLoading)
			//{
			//	yield return new WaitForSeconds(delayTheme);
			//	delayTheme += 0.0001f;
			//}

			Debug.LogFormat("{0}Music is null: ", EditorPlugin.className, song == null);

			fileInfo.text = "Playing Music for [" + _levelName + "]\n\nIf it doesn't, then something went wrong!";
			AudioManager.inst.PlayMusic(null, song, true, 0f, true);
			inst.StartCoroutine((IEnumerator)AccessTools.Method(typeof(EditorManager), "SpawnPlayersWithDelay").Invoke(EditorManager.inst, new object[] { 0.2f }));
			fileInfo.text = "Updating states for [" + _levelName + "]";
			DiscordController.inst.OnStateChange("Editing: " + DataManager.inst.metaData.song.title);
			objEditor.CreateTimelineObjects();
			objectManager.updateObjects();
			EventEditor.inst.CreateEventObjects();
			EventManager.inst.updateEvents();
			BackgroundManager.inst.UpdateBackgrounds();
			gameManager.UpdateTheme();
			MarkerEditor.inst.CreateMarkers();

			EditorPlugin.CreateMultiObjectEditor();
			if (ConfigEntries.GenerateWaveform.Value == true)
			{
				fileInfo.text = "Assigning Waveform Textures for [" + _levelName + "]";
				Debug.LogFormat("{0}Assigning timeline textures for {1}...", EditorPlugin.className, _levelName);
				var image = EditorManager.inst.timeline.GetComponent<Image>();
				yield return AssignTimelineTexture();
				float delay = 0f;
				while (image.sprite == null)
				{
					yield return new WaitForSeconds(delay);
					delay += 0.0001f;
				}
			}
			else
			{
				fileInfo.text = "Skipping Waveform Textures for [" + _levelName + "]";
				Debug.LogFormat("{0}Skipping Waveform Textures for {1}...", EditorPlugin.className, _levelName);
				EditorManager.inst.timeline.GetComponent<Image>().sprite = null;
				EditorManager.inst.timelineWaveformOverlay.GetComponent<Image>().sprite = null;
			}

			fileInfo.text = "Updating Timeline for [" + _levelName + "]";
			Debug.LogFormat("{0}Updating editor for {1}...", EditorPlugin.className, _levelName);
			AccessTools.Method(typeof(EditorManager), "UpdateTimelineSizes").Invoke(EditorManager.inst, new object[] { });
			gameManager.UpdateTimeline();
			__instance.ClearDialogs(Array.Empty<EditorManager.EditorDialog.DialogType>());
			EventEditor.inst.SetCurrentEvent(0, 0);
			CheckpointEditor.inst.SetCurrentCheckpoint(0);
			MetadataEditor.inst.Render();
			objEditor.CreateTimelineObjects();
			objEditor.RenderTimelineObjects();
			objEditor.SetCurrentObj(new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Object, 0));
			if (__instance.layer == 5)
			{
				CheckpointEditor.inst.CreateCheckpoints();
			}
			else
			{
				CheckpointEditor.inst.CreateGhostCheckpoints();
			}
			__instance.HideDialog("File Info Popup");
			__instance.CancelInvoke("LoadingIconUpdate");

			gameManager.ResetCheckpoints(true);
			gameManager.gameState = GameManager.State.Playing;
			__instance.DisplayNotification(_levelName + " Level Loaded", 2f, EditorManager.NotificationType.Success, false);
			__instance.UpdatePlayButton();
			__instance.hasLoadedLevel = true;

			if (!RTFile.DirectoryExists(RTFile.GetApplicationDirectory() + GameManager.inst.basePath + "autosaves"))
			{
				Directory.CreateDirectory(RTFile.GetApplicationDirectory() + GameManager.inst.basePath + "autosaves");
			}
			string[] files = Directory.GetFiles(FileManager.GetAppPath() + "/" + GameManager.inst.basePath, "autosaves/autosave_*.lsb", SearchOption.TopDirectoryOnly);
			files.ToList().Sort();
			int num = 0;
			foreach (string text2 in files)
			{
				if (num != files.Count() - 1)
				{
					File.Delete(text2);
				}
				num++;
			}
			yield break;
		}

		public static IEnumerator AssignTimelineTexture()
		{
			//int num = Mathf.Clamp((int)AudioManager.inst.CurrentAudioSource.clip.length * 48, 100, 15000);
			int num = Mathf.Clamp((int)AudioManager.inst.CurrentAudioSource.clip.length * 48, 100, 15000);
			Texture2D waveform = null;

			if (ConfigEntries.WaveformMode.Value == WaveformType.Legacy)
				yield return inst.StartCoroutine(GetWaveformTextureAdvanced(AudioManager.inst.CurrentAudioSource.clip, num, 300, ConfigEntries.TimelineBGColor.Value, ConfigEntries.TimelineTopColor.Value, ConfigEntries.TimelineBottomColor.Value, delegate (Texture2D _tex) { waveform = _tex; }));
			if (ConfigEntries.WaveformMode.Value == WaveformType.Beta)
				yield return inst.StartCoroutine(GetWaveformTexture(AudioManager.inst.CurrentAudioSource.clip, num, 300, ConfigEntries.TimelineBGColor.Value, ConfigEntries.TimelineTopColor.Value, delegate (Texture2D _tex) { waveform = _tex; }));
			if (ConfigEntries.WaveformMode.Value == WaveformType.BetaFast)
				yield return inst.StartCoroutine(BetaFast(AudioManager.inst.CurrentAudioSource.clip, 1f, num, 300, ConfigEntries.TimelineBGColor.Value, ConfigEntries.TimelineTopColor.Value, delegate (Texture2D _tex) { waveform = _tex; }));
			if (ConfigEntries.WaveformMode.Value == WaveformType.LegacyFast)
				yield return inst.StartCoroutine(LegacyFast(AudioManager.inst.CurrentAudioSource.clip, 1f, num, 300, ConfigEntries.TimelineBGColor.Value, ConfigEntries.TimelineTopColor.Value, ConfigEntries.TimelineBottomColor.Value, delegate (Texture2D _tex) { waveform = _tex; }));

			var waveSprite = Sprite.Create(waveform, new Rect(0f, 0f, (float)num, 300f), new Vector2(0.5f, 0.5f), 100f);
			EditorManager.inst.timeline.GetComponent<Image>().sprite = waveSprite;
			EditorManager.inst.timelineWaveformOverlay.GetComponent<Image>().sprite = EditorManager.inst.timeline.GetComponent<Image>().sprite;
			yield break;
		}

		public static IEnumerator GetWaveformTexture(AudioClip clip, int textureWidth, int textureHeight, Color background, Color waveform, Action<Texture2D> action)
		{
			Debug.LogFormat("{0}Generating Beta Waveform", EditorPlugin.className);
			var sw = new System.Diagnostics.Stopwatch();
			sw.Start();
			int num = 100;
			Texture2D texture2D = new Texture2D(textureWidth, textureHeight, TextureFormat.Alpha8, false);
			Color[] array = new Color[texture2D.width * texture2D.height];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = background;
			}
			Debug.LogFormat("{0}Generating Beta Waveform at {1}", EditorPlugin.className, sw.Elapsed);
			texture2D.SetPixels(array);
			num = clip.frequency / num;
			float[] array2 = new float[clip.samples * clip.channels];
			clip.GetData(array2, 0);
			float[] array3 = new float[array2.Length / num];
			for (int j = 0; j < array3.Length; j++)
			{
				array3[j] = 0f;
				for (int k = 0; k < num; k++)
				{
					array3[j] += Mathf.Abs(array2[j * num + k]);
				}
				array3[j] /= (float)num;
			}
			Debug.LogFormat("{0}Generating Beta Waveform at {1}", EditorPlugin.className, sw.Elapsed);
			for (int l = 0; l < array3.Length - 1; l++)
			{
				int num2 = 0;
				while ((float)num2 < (float)textureHeight * array3[l] + 1f)
				{
					texture2D.SetPixel(textureWidth * l / array3.Length, (int)((float)textureHeight * (array3[l] + 1f) / 2f) - num2, waveform);
					num2++;
				}
			}
			Debug.LogFormat("{0}Generating Beta Waveform at {1}", EditorPlugin.className, sw.Elapsed);
			texture2D.wrapMode = TextureWrapMode.Clamp;
			texture2D.filterMode = FilterMode.Point;
			texture2D.Apply();
			action(texture2D);
			Debug.LogFormat("{0}Generating Beta Waveform at {1}", EditorPlugin.className, sw.Elapsed);
			sw.Stop();
			yield break;
		}

		public static IEnumerator GetWaveformTextureAdvanced(AudioClip clip, int textureWidth, int textureHeight, Color background, Color _top, Color _bottom, Action<Texture2D> action)
		{
			Debug.LogFormat("{0}Generating Legacy Waveform", EditorPlugin.className);
			var sw = new System.Diagnostics.Stopwatch();
			sw.Start();
			int num = 160;
			num = clip.frequency / num;
			Texture2D texture2D = new Texture2D(textureWidth, textureHeight, TextureFormat.ARGB32, false);
			Color[] array = new Color[texture2D.width * texture2D.height];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = background;
			}
			Debug.LogFormat("{0}Generating Legacy Waveform at {1}", EditorPlugin.className, sw.Elapsed);
			texture2D.SetPixels(array);
			float[] array3 = new float[clip.samples];
			float[] array4 = new float[clip.samples];
			float[] array5 = new float[clip.samples * clip.channels];
			clip.GetData(array5, 0);
			if (clip.channels > 1)
			{
				array3 = array5.Where((float value, int index) => index % 2 != 0).ToArray();
				array4 = array5.Where((float value, int index) => index % 2 == 0).ToArray();
			}
			else
			{
				array3 = array5;
				array4 = array5;
			}
			Debug.LogFormat("{0}Generating Legacy Waveform at {1}", EditorPlugin.className, sw.Elapsed);
			float[] array6 = new float[array3.Length / num];
			for (int j = 0; j < array6.Length; j++)
			{
				array6[j] = 0f;
				for (int k = 0; k < num; k++)
				{
					array6[j] += Mathf.Abs(array3[j * num + k]);
				}
				array6[j] /= (float)num;
				array6[j] *= 0.85f;
			}
			Debug.LogFormat("{0}Generating Legacy Waveform at {1}", EditorPlugin.className, sw.Elapsed);
			for (int l = 0; l < array6.Length - 1; l++)
			{
				int num2 = 0;
				while ((float)num2 < (float)textureHeight * array6[l])
				{
					texture2D.SetPixel(textureWidth * l / array6.Length, (int)((float)textureHeight * array6[l]) - num2, _top);
					num2++;
				}
			}
			Debug.LogFormat("{0}Generating Legacy Waveform at {1}", EditorPlugin.className, sw.Elapsed);
			array6 = new float[array4.Length / num];
			for (int m = 0; m < array6.Length; m++)
			{
				array6[m] = 0f;
				for (int n = 0; n < num; n++)
				{
					array6[m] += Mathf.Abs(array4[m * num + n]);
				}
				array6[m] /= (float)num;
				array6[m] *= 0.85f;
			}
			Debug.LogFormat("{0}Generating Legacy Waveform at {1}", EditorPlugin.className, sw.Elapsed);
			for (int num3 = 0; num3 < array6.Length - 1; num3++)
			{
				int num4 = 0;
				while ((float)num4 < (float)textureHeight * array6[num3])
				{
					int x = textureWidth * num3 / array6.Length;
					int y = (int)array4[num3 * num + num4] - num4;
					if (texture2D.GetPixel(x, y) == _top)
					{
						texture2D.SetPixel(x, y, MixColors(new List<Color> { _top, _bottom }));
					}
					else
					{
						texture2D.SetPixel(x, y, _bottom);
					}
					num4++;
				}
			}
			Debug.LogFormat("{0}Generating Legacy Waveform at {1}", EditorPlugin.className, sw.Elapsed);
			texture2D.wrapMode = TextureWrapMode.Clamp;
			texture2D.filterMode = FilterMode.Point;
			texture2D.Apply();
			action(texture2D);
			Debug.LogFormat("{0}Generating Legacy Waveform at {1}", EditorPlugin.className, sw.Elapsed);
			sw.Stop();
			yield break;
		}

		public static IEnumerator BetaFast(AudioClip audio, float saturation, int width, int height, Color background, Color col, Action<Texture2D> action)
		{
			Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
			float[] samples = new float[audio.samples * audio.channels];
			float[] waveform = new float[width];
			audio.GetData(samples, 0);
			float packSize = ((float)samples.Length / (float)width);
			int s = 0;
			for (float i = 0; Mathf.RoundToInt(i) < samples.Length && s < waveform.Length; i += packSize)
			{
				waveform[s] = Mathf.Abs(samples[Mathf.RoundToInt(i)]);
				s++;
			}

			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					tex.SetPixel(x, y, background);
				}
			}

			for (int x = 0; x < waveform.Length; x++)
			{
				for (int y = 0; y <= waveform[x] * ((float)height * .75f); y++)
				{
					tex.SetPixel(x, (height / 2) + y, col);
					tex.SetPixel(x, (height / 2) - y, col);
				}
			}
			tex.Apply();

			action(tex);
			yield break;
		}

		public static IEnumerator LegacyFast(AudioClip audio, float saturation, int width, int height, Color background, Color colTop, Color colBot, Action<Texture2D> action)
		{
			Debug.LogFormat("{0}Generating Legacy Waveform (Fast)", EditorPlugin.className);
			var sw = new System.Diagnostics.Stopwatch();
			sw.Start();
			Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
			float[] samples = new float[audio.samples * audio.channels];
			float[] waveform = new float[width];
			audio.GetData(samples, 0);
			float packSize = ((float)samples.Length / (float)width);
			int s = 0;
			for (float i = 0; Mathf.RoundToInt(i) < samples.Length && s < waveform.Length; i += packSize)
			{
				waveform[s] = Mathf.Abs(samples[Mathf.RoundToInt(i)]);
				s++;
			}

			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					tex.SetPixel(x, y, background);
				}
			}

			for (int x = 0; x < waveform.Length; x++)
			{
				for (int y = 0; y <= waveform[x] * ((float)height * .75f); y++)
				{
					tex.SetPixel(x, height - y, colTop);
					if (tex.GetPixel(x, y) == colTop)
					{
						tex.SetPixel(x, y, MixColors(new List<Color> { colTop, colBot }));
					}
					else
					{
						tex.SetPixel(x, y, colBot);
					}
				}
			}
			tex.Apply();

			action(tex);
			Debug.LogFormat("{0}Generated Legacy Waveform at {1}", EditorPlugin.className, sw.Elapsed);
			sw.Stop();
			yield break;
		}

		public static Color MixColors(List<Color> colors)
		{
			var invertedColorSum = Color.black;
			foreach (var color in colors)
			{
				invertedColorSum += Color.white - color;
			}

			return Color.white - invertedColorSum / colors.Count;
		}

		public static DataManager.GameData.BeatmapObject mirrorObject;
		public static void MirrorWatcher()
        {
			var objEditor = ObjEditor.inst;
			var currentObjectSelection = objEditor.currentObjectSelection;
			if (currentObjectSelection.IsObject())
			{
				var beatmapObject = currentObjectSelection.GetObjectData();
				mirrorObject = DataManager.GameData.BeatmapObject.DeepCopy(beatmapObject);
				mirrorObject.name = mirrorObject.name.Replace("left", "26435riht");
				mirrorObject.name = mirrorObject.name.Replace("right", "26435let");
				mirrorObject.name = mirrorObject.name.Replace("Left", "26435Riht");
				mirrorObject.name = mirrorObject.name.Replace("Right", "26435Let");
				mirrorObject.name = mirrorObject.name.Replace("26435riht", "right");
				mirrorObject.name = mirrorObject.name.Replace("26435let", "left");
				mirrorObject.name = mirrorObject.name.Replace("26435Riht", "Right");
				mirrorObject.name = mirrorObject.name.Replace("26435Let", "Left");

				for (int i = 0; i < 3; i ++)
				{
					foreach (var keyframe in mirrorObject.events[i])
					{
						keyframe.eventValues[0] = -keyframe.eventValues[0];
					}
				}

				mirrorObject.editorData.Bin = Mathf.Clamp(mirrorObject.editorData.Bin + 1, 0, 14);
				if (!DataManager.inst.gameData.beatmapObjects.Contains(mirrorObject))
                {
					DataManager.inst.gameData.beatmapObjects.Add(mirrorObject);
				}
			}
        }

		public static void CreateNewLevel(EditorManager __instance)
		{
			if (!__instance.newAudioFile.ToLower().Contains(".ogg"))
			{
				__instance.DisplayNotification("The file you are trying to load doesn't appear to be a .ogg file.", 2f, EditorManager.NotificationType.Error, false);
				return;
			}
			if (!RTFile.FileExists(__instance.newAudioFile))
			{
				__instance.DisplayNotification("The file you are trying to load doesn't appear to exist.", 2f, EditorManager.NotificationType.Error, false);
				return;
			}
			if (RTFile.DirectoryExists(RTFile.GetApplicationDirectory() + EditorPlugin.levelListSlash + __instance.newLevelName))
			{
				__instance.DisplayNotification("The level you are trying to create already exists.", 2f, EditorManager.NotificationType.Error, false);
				return;
			}
			Directory.CreateDirectory(RTFile.GetApplicationDirectory() + EditorPlugin.levelListSlash + __instance.newLevelName);
			if (__instance.newAudioFile.ToLower().Contains(".ogg"))
			{
				string destFileName = RTFile.GetApplicationDirectory() + EditorPlugin.levelListSlash + __instance.newLevelName + "/level.ogg";
				File.Copy(__instance.newAudioFile, destFileName, true);
			}
			inst.StartCoroutine(SaveData(RTFile.GetApplicationDirectory() + EditorPlugin.levelListSlash + __instance.newLevelName + "/level.lsb", CreateBaseBeatmap()));
			var dataManager = DataManager.inst;
			dataManager.metaData = new DataManager.MetaData();
			dataManager.metaData.beatmap.game_version = "4.1.16";
			dataManager.metaData.song.title = __instance.newLevelName;
			dataManager.metaData.creator.steam_name = SteamWrapper.inst.user.displayName;
			dataManager.metaData.creator.steam_id = SteamWrapper.inst.user.id;
			dataManager.SaveMetadata(RTFile.GetApplicationDirectory() + EditorPlugin.levelListSlash + __instance.newLevelName + "/metadata.lsb");
			if (EditorPlugin.tester == true)
			{
				inst.StartCoroutine(EditorManager.inst.LoadLevel(__instance.newLevelName));
			}
			else
			{
				inst.StartCoroutine(LoadLevel(__instance, __instance.newLevelName));
			}
			__instance.HideDialog("New File Popup");
		}

		public static DataManager.GameData CreateBaseBeatmap()
		{
			DataManager.GameData gameData = new DataManager.GameData();
			gameData.beatmapData = new DataManager.GameData.BeatmapData();
			gameData.beatmapData.levelData = new DataManager.GameData.BeatmapData.LevelData();
			gameData.beatmapData.levelData.backgroundColor = 0;
			gameData.beatmapData.checkpoints.Add(new DataManager.GameData.BeatmapData.Checkpoint(false, "Base Checkpoint", 0f, Vector2.zero));
			DataManager.GameData.BeatmapData.EditorData editorData = new DataManager.GameData.BeatmapData.EditorData();
			gameData.beatmapData.editorData = editorData;
			List<DataManager.GameData.EventKeyframe> list = new List<DataManager.GameData.EventKeyframe>();
			DataManager.GameData.EventKeyframe eventKeyframe = new DataManager.GameData.EventKeyframe();
			eventKeyframe.eventTime = 0f;
			eventKeyframe.SetEventValues(new float[2]);
			list.Add(eventKeyframe);
			List<DataManager.GameData.EventKeyframe> list2 = new List<DataManager.GameData.EventKeyframe>();
			DataManager.GameData.EventKeyframe eventKeyframe2 = new DataManager.GameData.EventKeyframe();
			eventKeyframe2.eventTime = 0f;
			DataManager.GameData.EventKeyframe eventKeyframe3 = eventKeyframe2;
			float[] array = new float[2];
			array[0] = 20f;
			eventKeyframe3.SetEventValues(array);
			list2.Add(eventKeyframe2);

			List<DataManager.GameData.EventKeyframe> list3 = new List<DataManager.GameData.EventKeyframe>();
			DataManager.GameData.EventKeyframe eventKeyframe4 = new DataManager.GameData.EventKeyframe();
			eventKeyframe4.eventTime = 0f;
			eventKeyframe4.SetEventValues(new float[2]);
			list3.Add(eventKeyframe4);

			List<DataManager.GameData.EventKeyframe> list4 = new List<DataManager.GameData.EventKeyframe>();
			DataManager.GameData.EventKeyframe eventKeyframe5 = new DataManager.GameData.EventKeyframe();
			eventKeyframe5.eventTime = 0f;
			eventKeyframe5.SetEventValues(new float[2]);
			list4.Add(eventKeyframe5);

			List<DataManager.GameData.EventKeyframe> list5 = new List<DataManager.GameData.EventKeyframe>();
			DataManager.GameData.EventKeyframe eventKeyframe6 = new DataManager.GameData.EventKeyframe();
			eventKeyframe6.eventTime = 0f;
			eventKeyframe6.SetEventValues(new float[2]);
			list5.Add(eventKeyframe6);

			List<DataManager.GameData.EventKeyframe> list6 = new List<DataManager.GameData.EventKeyframe>();
			DataManager.GameData.EventKeyframe eventKeyframe7 = new DataManager.GameData.EventKeyframe();
			eventKeyframe7.eventTime = 0f;
			eventKeyframe7.SetEventValues(new float[2]);
			list6.Add(eventKeyframe7);

			List<DataManager.GameData.EventKeyframe> list7 = new List<DataManager.GameData.EventKeyframe>();
			DataManager.GameData.EventKeyframe eventKeyframe8 = new DataManager.GameData.EventKeyframe();
			eventKeyframe8.eventTime = 0f;
			eventKeyframe8.SetEventValues(new float[2]);
			list7.Add(eventKeyframe8);

			List<DataManager.GameData.EventKeyframe> list8 = new List<DataManager.GameData.EventKeyframe>();
			DataManager.GameData.EventKeyframe eventKeyframe9 = new DataManager.GameData.EventKeyframe();
			eventKeyframe9.eventTime = 0f;
			eventKeyframe9.SetEventValues(new float[6]);
			list8.Add(eventKeyframe9);

			List<DataManager.GameData.EventKeyframe> list9 = new List<DataManager.GameData.EventKeyframe>();
			DataManager.GameData.EventKeyframe eventKeyframe10 = new DataManager.GameData.EventKeyframe();
			eventKeyframe10.eventTime = 0f;
			eventKeyframe10.SetEventValues(new float[2]);
			list9.Add(eventKeyframe10);

			List<DataManager.GameData.EventKeyframe> list10 = new List<DataManager.GameData.EventKeyframe>();
			DataManager.GameData.EventKeyframe eventKeyframe11 = new DataManager.GameData.EventKeyframe();
			eventKeyframe11.eventTime = 0f;
			eventKeyframe11.SetEventValues(new float[3]);
			list10.Add(eventKeyframe11);

			List<DataManager.GameData.EventKeyframe> list11 = new List<DataManager.GameData.EventKeyframe>();
			DataManager.GameData.EventKeyframe eventKeyframe12 = new DataManager.GameData.EventKeyframe();
			eventKeyframe12.eventTime = 0f;
			eventKeyframe12.SetEventValues(new float[9]);
			list11.Add(eventKeyframe12);

			List<DataManager.GameData.EventKeyframe> list12 = new List<DataManager.GameData.EventKeyframe>();
			DataManager.GameData.EventKeyframe eventKeyframe13 = new DataManager.GameData.EventKeyframe();
			eventKeyframe13.eventTime = 0f;
			eventKeyframe13.SetEventValues(new float[5]);
			list12.Add(eventKeyframe13);

			List<DataManager.GameData.EventKeyframe> list13 = new List<DataManager.GameData.EventKeyframe>();
			DataManager.GameData.EventKeyframe eventKeyframe14 = new DataManager.GameData.EventKeyframe();
			eventKeyframe14.eventTime = 0f;
			eventKeyframe14.SetEventValues(new float[2]);
			list13.Add(eventKeyframe14);

			List<DataManager.GameData.EventKeyframe> list14 = new List<DataManager.GameData.EventKeyframe>();
			DataManager.GameData.EventKeyframe eventKeyframe15 = new DataManager.GameData.EventKeyframe();
			eventKeyframe15.eventTime = 0f;
			eventKeyframe15.SetEventValues(new float[2]);
			list14.Add(eventKeyframe15);

			gameData.eventObjects.allEvents[0] = list;
			gameData.eventObjects.allEvents[1] = list2;
			gameData.eventObjects.allEvents[2] = list3;
			gameData.eventObjects.allEvents[3] = list4;
			gameData.eventObjects.allEvents[4] = list5;
			gameData.eventObjects.allEvents[5] = list6;
			gameData.eventObjects.allEvents[6] = list7;
			gameData.eventObjects.allEvents[7] = list8;
			gameData.eventObjects.allEvents[8] = list9;
			gameData.eventObjects.allEvents[9] = list10;
			gameData.eventObjects.allEvents.Add(new List<DataManager.GameData.EventKeyframe>());
			gameData.eventObjects.allEvents[10] = list11;
			gameData.eventObjects.allEvents.Add(new List<DataManager.GameData.EventKeyframe>());
			gameData.eventObjects.allEvents[11] = list12;
			gameData.eventObjects.allEvents.Add(new List<DataManager.GameData.EventKeyframe>());
			gameData.eventObjects.allEvents[12] = list13;
			gameData.eventObjects.allEvents.Add(new List<DataManager.GameData.EventKeyframe>());
			gameData.eventObjects.allEvents[13] = list14;
			for (int i = 0; i < 25; i++)
			{
				DataManager.GameData.BackgroundObject backgroundObject = new DataManager.GameData.BackgroundObject();
				backgroundObject.name = "bg - " + i;
				if (UnityEngine.Random.value > 0.5f)
				{
					backgroundObject.scale = new Vector2((float)UnityEngine.Random.Range(2, 8), (float)UnityEngine.Random.Range(2, 8));
				}
				else
				{
					float num = (float)UnityEngine.Random.Range(2, 6);
					backgroundObject.scale = new Vector2(num, num);
				}
				backgroundObject.pos = new Vector2((float)UnityEngine.Random.Range(-48, 48), (float)UnityEngine.Random.Range(-32, 32));
				backgroundObject.color = UnityEngine.Random.Range(1, 6);
				backgroundObject.layer = UnityEngine.Random.Range(0, 6);
				backgroundObject.reactive = (UnityEngine.Random.value > 0.5f);
				if (backgroundObject.reactive)
				{
					switch (UnityEngine.Random.Range(0, 4))
					{
						case 0:
							backgroundObject.reactiveType = DataManager.GameData.BackgroundObject.ReactiveType.LOW;
							break;
						case 1:
							backgroundObject.reactiveType = DataManager.GameData.BackgroundObject.ReactiveType.MID;
							break;
						case 2:
							backgroundObject.reactiveType = DataManager.GameData.BackgroundObject.ReactiveType.HIGH;
							break;
					}
					backgroundObject.reactiveScale = UnityEngine.Random.Range(0.01f, 0.04f);
				}
				gameData.backgroundObjects.Add(backgroundObject);
			}
			DataManager.GameData.BeatmapObject beatmapObject = CreateNewBeatmapObject(0.5f, false);
			List<DataManager.GameData.EventKeyframe> objectEvents = beatmapObject.events[0];
			float time = 4f;
			float[] array2 = new float[3];
			array2[0] = 10f;
			objectEvents.Add(new DataManager.GameData.EventKeyframe(time, array2, new float[2], 0));
			beatmapObject.name = "\"Default object cameo\" -Viral Mecha";
			beatmapObject.autoKillType = DataManager.GameData.BeatmapObject.AutoKillType.LastKeyframeOffset;
			beatmapObject.autoKillOffset = 4f;
			gameData.beatmapObjects.Add(beatmapObject);
			return gameData;
		}

		public static Vector2 preMouse = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
		public static bool IsPressingAnyKey()
		{
			string[] PieceTypeNames = Enum.GetNames(typeof(KeyCode));
			for (int i = 0; i < PieceTypeNames.Length; i++)
			{
				if (Input.GetKey((KeyCode)i))
				{
					return true;
				}
			}
			if (preMouse != new Vector2(Input.mousePosition.x, Input.mousePosition.y))
            {
				preMouse = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
				return true;
            }				
			return false;
		}

		public static IEnumerator InitiateSecret()
		{
			if (!IsPressingAnyKey())
			{
			}
			if (!IsPressingAnyKey())
			{
				yield return new WaitForSeconds(10f);
			}
			if (!IsPressingAnyKey())
			{
				DisplayNotification("e", "Because if nobody's there, I don't know what I'll do.", 2f, EditorManager.NotificationType.Info);
			}
			if (!IsPressingAnyKey())
			{
				yield return new WaitForSeconds(10f);
			}
			if (!IsPressingAnyKey())
			{
				DisplayNotification("e", "Probably go to the circus...", 2f, EditorManager.NotificationType.Info);
			}
			if (!IsPressingAnyKey())
			{
				yield return new WaitForSeconds(10f);
			}
			if (!IsPressingAnyKey())
			{
				DisplayNotification("e", "Find a strangely verbose clown...", 2f, EditorManager.NotificationType.Info);
			}
			if (!IsPressingAnyKey())
			{
				yield return new WaitForSeconds(10f);
			}
			if (!IsPressingAnyKey())
			{
				DisplayNotification("e", "<br>sigh</br>", 2f, EditorManager.NotificationType.Info);
			}

			yield break;
        }

		public static void SearchObjectsCreator()
        {
			var objectSearch = Instantiate(EditorManager.inst.GetDialog("Parent Selector").Dialog.gameObject);
			objectSearch.transform.SetParent(EditorManager.inst.GetDialog("Parent Selector").Dialog.GetParent());
			objectSearch.transform.localScale = Vector3.one;
			objectSearch.transform.localPosition = Vector3.zero;
			objectSearch.name = "Object Search";

			var objectSearchRT = objectSearch.GetComponent<RectTransform>();
			objectSearchRT.sizeDelta = new Vector2(600f, 450f);
			var objectSearchPanel = objectSearch.transform.Find("Panel").GetComponent<RectTransform>();
			objectSearchPanel.sizeDelta = new Vector2(632f, 32f);
			objectSearchPanel.transform.Find("Text").GetComponent<Text>().text = "Object Search";
			objectSearch.transform.Find("search-box").GetComponent<RectTransform>().sizeDelta = new Vector2(600f, 32f);
			objectSearch.transform.Find("mask/content").GetComponent<GridLayoutGroup>().cellSize = new Vector2(600f, 32f);

			var x = objectSearchPanel.transform.Find("x").GetComponent<Button>();
			x.onClick.RemoveAllListeners();
			x.onClick.AddListener(delegate ()
			{
				EditorManager.inst.HideDialog("Object Search Popup");
			});

			var searchBar = objectSearch.transform.Find("search-box/search").GetComponent<InputField>();
			searchBar.onValueChanged.RemoveAllListeners();
			searchBar.onValueChanged.AddListener(delegate (string _value)
			{
				searchterm = _value;
				RefreshObjectSearch();
			});
			searchBar.transform.Find("Placeholder").GetComponent<Text>().text = "Search for object...";

			var propWin = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/Edit/Edit Dropdown/Cut"));
			propWin.transform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/Edit/Edit Dropdown").transform);
			propWin.transform.localScale = Vector3.one;
			propWin.name = "Search Objects";
			propWin.transform.Find("Text").GetComponent<Text>().text = "Search Objects";
			propWin.transform.Find("Text").GetComponent<RectTransform>().sizeDelta = new Vector2(224f, 0f);
			propWin.transform.Find("Text 1").GetComponent<Text>().text = "";

			var propWinButton = propWin.GetComponent<Button>();
			propWinButton.onClick.m_Calls.m_ExecutingCalls.Clear();
			propWinButton.onClick.m_Calls.m_PersistentCalls.Clear();
			propWinButton.onClick.m_PersistentCalls.m_Calls.Clear();
			propWinButton.onClick.RemoveAllListeners();
			propWinButton.onClick.AddListener(delegate ()
			{
				EditorManager.inst.ShowDialog("Object Search Popup");
				RefreshObjectSearch();
			});

			propWin.SetActive(true);

			propWin.transform.Find("Image").GetComponent<Image>().sprite = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/parent/parent/image").GetComponent<Image>().sprite;

			//Add Search Object Popup to EditorDialogsDictionary
			{
				EditorManager.EditorDialog editorPropertiesDialog = new EditorManager.EditorDialog();

				editorPropertiesDialog.Dialog = objectSearch.transform;
				editorPropertiesDialog.Name = "Object Search Popup";
				editorPropertiesDialog.Type = EditorManager.EditorDialog.DialogType.Popup;

				EditorManager.inst.EditorDialogs.Add(editorPropertiesDialog);

				var editorDialogsDictionary = AccessTools.Field(typeof(EditorManager), "EditorDialogsDictionary");

				var editorDialogsDictionaryInst = AccessTools.Field(typeof(EditorManager), "EditorDialogsDictionary").GetValue(EditorManager.inst);

				editorDialogsDictionary.GetValue(EditorManager.inst).GetType().GetMethod("Add").Invoke(editorDialogsDictionaryInst, new object[] { "Object Search Popup", editorPropertiesDialog });
			}
		}

		public static void WarningPopupCreator()
        {
			var warningPopup = Instantiate(EditorManager.inst.GetDialog("Save As Popup").Dialog.gameObject);
			var warningPopupTF = warningPopup.transform;
			warningPopupTF.SetParent(EditorManager.inst.GetDialog("Save As Popup").Dialog.GetParent());
			warningPopupTF.localScale = Vector3.one;
			warningPopupTF.localPosition = Vector3.zero;
			warningPopup.name = "Warning Popup";

			var main = warningPopupTF.GetChild(0);

			var spacer1 = new GameObject
			{
				name = "spacerL",
				transform =
				{
					parent = main,
					localScale = Vector3.one
                }
			};
			var spacer1RT = spacer1.AddComponent<RectTransform>();
			spacer1.AddComponent<LayoutElement>();
			var horiz = spacer1.AddComponent<HorizontalLayoutGroup>();
			horiz.spacing = 22f;

			spacer1RT.sizeDelta = new Vector2(292f, 40f);

			var submit1 = main.Find("submit");
			submit1.SetParent(spacer1.transform);

			var submit2 = Instantiate(submit1);
			var submit2TF = submit2.transform;

			submit2TF.SetParent(spacer1.transform);
			submit2TF.localScale = Vector3.one;

			submit1.name = "submit1";
			submit2.name = "submit2";

			submit1.GetComponent<Image>().color = new Color(1f, 0.2137f, 0.2745f, 1f);
			submit2.GetComponent<Image>().color = new Color(0.302f, 0.7137f, 0.6745f, 1f);

			var submit1Button = submit1.GetComponent<Button>();
			var submit2Button = submit2.GetComponent<Button>();

			submit1Button.onClick.m_Calls.m_ExecutingCalls.Clear();
			submit1Button.onClick.m_Calls.m_PersistentCalls.Clear();
			submit1Button.onClick.m_PersistentCalls.m_Calls.Clear();
			submit1Button.onClick.RemoveAllListeners();

			submit2Button.onClick.m_Calls.m_ExecutingCalls.Clear();
			submit2Button.onClick.m_Calls.m_PersistentCalls.Clear();
			submit2Button.onClick.m_PersistentCalls.m_Calls.Clear();
			submit2Button.onClick.RemoveAllListeners();

			Destroy(main.Find("level-name").gameObject);

			var sizeFitter = main.GetComponent<ContentSizeFitter>();
			sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
			sizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

			var mainRT = main.GetComponent<RectTransform>();
			mainRT.sizeDelta = new Vector2(400f, 160f);

			main.Find("Level Name").GetComponent<RectTransform>().sizeDelta = new Vector2(292f, 64f);

			var close = main.Find("Panel/x").GetComponent<Button>();
			close.onClick.m_Calls.m_ExecutingCalls.Clear();
			close.onClick.m_Calls.m_PersistentCalls.Clear();
			close.onClick.m_PersistentCalls.m_Calls.Clear();
			close.onClick.RemoveAllListeners();
			close.onClick.AddListener(delegate ()
			{
				EditorManager.inst.HideDialog("Warning Popup");
			});

			main.Find("Panel/Text").GetComponent<Text>().text = "Warning!";

			Triggers.AddEditorDialog("Warning Popup", warningPopup);
		}

		public static void RefreshWarningPopup(string warning, UnityAction c1, UnityAction c2, string confirm = "Yes", string cancel = "No")
        {
			var warningPopup = EditorManager.inst.GetDialog("Warning Popup").Dialog.GetChild(0);

			warningPopup.Find("Level Name").GetComponent<Text>().text = warning;

			var submit1 = warningPopup.Find("spacerL/submit1");
			var submit2 = warningPopup.Find("spacerL/submit2");

			var submit1Button = submit1.GetComponent<Button>();
			var submit2Button = submit2.GetComponent<Button>();

			submit1.Find("text").GetComponent<Text>().text = confirm;
			submit2.Find("text").GetComponent<Text>().text = cancel;

			submit1Button.onClick.RemoveAllListeners();
			submit2Button.onClick.RemoveAllListeners();

			submit1Button.onClick.AddListener(c1);
			submit2Button.onClick.AddListener(c2);
		}

		public static void DeleteLevelFunction(string _levelName)
        {
			if (!RTFile.DirectoryExists(RTFile.GetApplicationDirectory() + "recycling"))
            {
				Directory.CreateDirectory(RTFile.GetApplicationDirectory() + "recycling");
            }
			Directory.Move(RTFile.GetApplicationDirectory() + EditorPlugin.levelListSlash + _levelName, RTFile.GetApplicationDirectory() + "recycling/" + _levelName);

			string[] directories = Directory.GetDirectories(RTFile.GetApplicationDirectory() + "recycling", "*", SearchOption.AllDirectories);
			directories.ToList().Sort();
			foreach (var directory in directories)
            {
				string[] filesDir = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);
				filesDir.ToList().Sort();
            }
		}

		public static string searchterm = "";

		public static void BringToObject()
		{
			AudioManager.inst.SetMusicTime(ObjEditor.inst.currentObjectSelection.GetObjectData().StartTime);
			SetLayer(ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer);

			AudioManager.inst.CurrentAudioSource.Pause();
			EditorManager.inst.UpdatePlayButton();
			EditorManager.inst.StartCoroutine(EditorManager.inst.UpdateTimelineScrollRect(0f, AudioManager.inst.CurrentAudioSource.time / AudioManager.inst.CurrentAudioSource.clip.length));
		}

		public static void RefreshObjectSearch(bool multi = false, string multiValue = "", bool _objEditor = false, bool _objectManager = false)
        {
			Debug.LogFormat("{0}Mutli: {1}\nMultiValue: {2}", EditorPlugin.className, multi.ToString(), multiValue.ToString());
			var content = EditorManager.inst.GetDialog("Object Search Popup").Dialog.Find("mask/content");
			LSHelpers.DeleteChildren(content);

			foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
			{
				var regex = new Regex(@"\[([0-9])\]");
				var match = regex.Match(searchterm);

				if (beatmapObject.name.ToLower().Contains(searchterm.ToLower()) || match.Success && int.Parse(match.Groups[1].ToString()) < DataManager.inst.gameData.beatmapObjects.Count && DataManager.inst.gameData.beatmapObjects.IndexOf(beatmapObject) == int.Parse(match.Groups[1].ToString()))
				{
					var buttonPrefab = Instantiate(EditorManager.inst.spriteFolderButtonPrefab);
					buttonPrefab.transform.SetParent(content.transform);
					buttonPrefab.transform.localScale = Vector3.one;
					string nm = "[" + DataManager.inst.gameData.beatmapObjects.IndexOf(beatmapObject).ToString("0000") + "/" + (DataManager.inst.gameData.beatmapObjects.Count - 1).ToString("0000") + " - " + beatmapObject.id + "] : " + beatmapObject.name;
					buttonPrefab.name = nm;
					buttonPrefab.transform.GetChild(0).GetComponent<Text>().text = nm;

					var b = buttonPrefab.GetComponent<Button>();
					b.onClick.RemoveAllListeners();
					b.onClick.AddListener(delegate ()
					{
						if (!multi)
						{
							ObjEditor.inst.SetCurrentObj(new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Object, DataManager.inst.gameData.beatmapObjects.IndexOf(beatmapObject)));
							BringToObject();
						}
						else
						{
							string id = beatmapObject.id;

							Debug.LogFormat("{0}1Attempting to sync {1} to selection!", EditorPlugin.className, id);

							Triggers.SyncObjects(id, multiValue, _objEditor, _objectManager);

							EditorManager.inst.HideDialog("Object Search Popup");
						}
					});
					var image = buttonPrefab.transform.Find("Image").GetComponent<Image>();
					image.color = beatmapObject.GetObjectColor(false);

					int n = beatmapObject.shape + 1;

					if (beatmapObject.shape == 4 || beatmapObject.shape == 6)
                    {
						image.sprite = ObjEditor.inst.ObjectView.transform.Find("shape/" + n.ToString() + "/Image").GetComponent<Image>().sprite;
					}
					else
					{
						image.sprite = ObjEditor.inst.ObjectView.transform.Find("shapesettings").GetChild(beatmapObject.shape).GetChild(beatmapObject.shapeOption).Find("Image").GetComponent<Image>().sprite;
					}

					string desc = "";
					string hint = "";
					var gameObjectRef = ObjectManager.inst.beatmapGameObjects[beatmapObject.id];

					Transform transform = beatmapObject.GetTransformChain()[beatmapObject.GetTransformChain().Count - 1];

					string parent = "";
					if (!string.IsNullOrEmpty(beatmapObject.parent))
					{
						parent = "<br>P: " + beatmapObject.parent + " (" + beatmapObject.GetParentType() + ")";
					}
					else
					{
						parent = "<br>P: No Parent" + " (" + beatmapObject.GetParentType() + ")";
					}

					string text = "";
					if (beatmapObject.shape != 4 || beatmapObject.shape != 6)
					{
						text = "<br>S: " + GetShape(beatmapObject.shape, beatmapObject.shapeOption) +
							"<br>T: " + beatmapObject.text;
					}
					if (beatmapObject.shape == 4)
					{
						text = "<br>S: Text" +
							"<br>T: " + beatmapObject.text;
					}
					if (beatmapObject.shape == 6)
					{
						text = "<br>S: Image" +
							"<br>T: " + beatmapObject.text;
					}

					string ptr = "";
					if (!string.IsNullOrEmpty(beatmapObject.prefabID) && !string.IsNullOrEmpty(beatmapObject.prefabInstanceID))
					{
						ptr = "<br><#" + ColorToHex(beatmapObject.GetPrefabTypeColor()) + ">PID: " + beatmapObject.prefabID + " | PIID: " + beatmapObject.prefabInstanceID + "</color>";
					}
					else
					{
						ptr = "<br>Not from prefab";
					}

					desc = "N/ST: " + beatmapObject.name + " [ " + beatmapObject.StartTime + " ]";
					hint = "ID: {" + beatmapObject.id + "}" +
						parent +
						"<br>A: " + beatmapObject.TimeWithinLifespan().ToString() +
						"<br>O: {X: " + beatmapObject.origin.x + ", Y: " + beatmapObject.origin.y + "}" +
						text +
						"<br>D: " + beatmapObject.Depth +
						"<br>ED: {L: " + beatmapObject.editorData.Layer + ", B: " + beatmapObject.editorData.Bin + "}" +
						"<br>POS: {X: " + transform.position.x + ", Y: " + transform.position.y + "}" +
						"<br>SCA: {X: " + transform.localScale.x + ", Y: " + transform.localScale.y + "}" +
						"<br>ROT: " + transform.eulerAngles.z +
						"<br>COL: " + "<#" + ColorToHex(beatmapObject.GetObjectColor(false)) + ">" + "█ <b>#" + ColorToHex(beatmapObject.GetObjectColor(true)) + "</b></color>" +
						ptr;

					Triggers.AddTooltip(buttonPrefab, desc, hint);
				}
			}
		}

		public static void CreateBackgrounds(int _amount)
        {
			int number = Mathf.Clamp(_amount, 0, 100);

			for (int i = 0; i < number; i++)
			{
				DataManager.GameData.BackgroundObject backgroundObject = new DataManager.GameData.BackgroundObject();
				backgroundObject.name = "bg - " + i;
				if (UnityEngine.Random.value > 0.5f)
				{
					backgroundObject.scale = new Vector2((float)UnityEngine.Random.Range(2, 8), (float)UnityEngine.Random.Range(2, 8));
				}
				else
				{
					float num = (float)UnityEngine.Random.Range(2, 6);
					backgroundObject.scale = new Vector2(num, num);
				}
				backgroundObject.pos = new Vector2((float)UnityEngine.Random.Range(-48, 48), (float)UnityEngine.Random.Range(-32, 32));
				backgroundObject.color = UnityEngine.Random.Range(1, 6);
				backgroundObject.layer = UnityEngine.Random.Range(0, 6);
				backgroundObject.reactive = (UnityEngine.Random.value > 0.5f);
				if (backgroundObject.reactive)
				{
					switch (UnityEngine.Random.Range(0, 4))
					{
						case 0:
							backgroundObject.reactiveType = DataManager.GameData.BackgroundObject.ReactiveType.LOW;
							break;
						case 1:
							backgroundObject.reactiveType = DataManager.GameData.BackgroundObject.ReactiveType.MID;
							break;
						case 2:
							backgroundObject.reactiveType = DataManager.GameData.BackgroundObject.ReactiveType.HIGH;
							break;
					}
					backgroundObject.reactiveScale = UnityEngine.Random.Range(0.01f, 0.04f);
				}
				DataManager.inst.gameData.backgroundObjects.Add(backgroundObject);
			}

			BackgroundManager.inst.UpdateBackgrounds();
			BackgroundEditor.inst.UpdateBackgroundList();
		}

		public static void DeleteAllBackgrounds()
        {
			int num = DataManager.inst.gameData.backgroundObjects.Count;

			for (int i = 1; i < num; i++)
			{
				int nooo = Mathf.Clamp(i, 1, DataManager.inst.gameData.backgroundObjects.Count - 1);
				DataManager.inst.gameData.backgroundObjects.RemoveAt(nooo);
			}

			BackgroundEditor.inst.SetCurrentBackground(0);
			BackgroundManager.inst.UpdateBackgrounds();
			BackgroundEditor.inst.UpdateBackgroundList();

			EditorManager.inst.DisplayNotification("Deleted " + (num - 1).ToString() + " backgrounds!", 2f, EditorManager.NotificationType.Success);
		}

		public static IEnumerator InternalPrefabs(bool _toggle = false)
		{
			if (!DataManager.inst.gameData.prefabs.Exists((DataManager.GameData.Prefab x) => x.ID == "toYoutoYoutoYou") && ConfigEntries.EXPrefab.Value)
			{
				DataManager.inst.gameData.prefabs.Add(ExamplePrefab.examplePrefab);
			}

			yield return new WaitForSeconds(0.03f);

			LSHelpers.DeleteChildren(PrefabEditorPatch.internalContent, false);
			GameObject gameObject = Instantiate(PrefabEditor.inst.CreatePrefab, Vector3.zero, Quaternion.identity);
			gameObject.name = "add new prefab";
			gameObject.GetComponentInChildren<Text>().text = "New Internal Prefab";
			gameObject.transform.SetParent(PrefabEditorPatch.internalContent);
			gameObject.transform.localScale = Vector3.one;

			var hover = gameObject.AddComponent<HoverUI>();
			hover.animateSca = true;
			hover.animatePos = false;
			hover.size = ConfigEntries.SizeUpper.Value;

			var createInternalB = gameObject.GetComponent<Button>();
			createInternalB.onClick.RemoveAllListeners();
			createInternalB.onClick.AddListener(delegate ()
			{
				PrefabEditor.inst.OpenDialog();
				EditorPlugin.createInternal = true;
			});

			int num = 0;
			foreach (DataManager.GameData.Prefab prefab in DataManager.inst.gameData.prefabs)
			{
				if (ContainsName(prefab, PrefabDialog.Internal))
				{
					inst.StartCoroutine(CreatePrefabButton(prefab, num, PrefabDialog.Internal, _toggle));
				}
				num++;
			}

			foreach (object obj in PrefabEditorPatch.internalContent)
			{
				((Transform)obj).localScale = Vector3.one;
			}

			yield break;
		}

		public static IEnumerator ExternalPrefabFiles(bool _toggle = false)
		{
			yield return new WaitForSeconds(0.03f);

			LSHelpers.DeleteChildren(PrefabEditorPatch.externalContent, false);
			GameObject gameObject = Instantiate(PrefabEditor.inst.CreatePrefab, Vector3.zero, Quaternion.identity);
			gameObject.name = "add new prefab";
			gameObject.GetComponentInChildren<Text>().text = "New External Prefab";
			gameObject.transform.SetParent(PrefabEditorPatch.externalContent);
			gameObject.transform.localScale = Vector3.one;

			var hover = gameObject.AddComponent<HoverUI>();
			hover.animateSca = true;
			hover.animatePos = false;
			hover.size = ConfigEntries.SizeUpper.Value;

			var createExternal = gameObject.GetComponent<Button>();
			createExternal.onClick.RemoveAllListeners();
			createExternal.onClick.AddListener(delegate ()
			{
				PrefabEditor.inst.OpenDialog();
				EditorPlugin.createInternal = false;
			});

			int num = 0;
			foreach (DataManager.GameData.Prefab prefab in PrefabEditor.inst.LoadedPrefabs)
			{
				if (ContainsName(prefab, PrefabDialog.External))
				{
					inst.StartCoroutine(CreatePrefabButton(prefab, num, PrefabDialog.External, _toggle));
				}
				num++;
			}

			foreach (object obj in PrefabEditorPatch.externalContent)
			{
				((Transform)obj).localScale = Vector3.one;
			}

			yield break;
		}

		public static IEnumerator CreatePrefabButton(DataManager.GameData.Prefab _p, int _num, PrefabDialog _d, bool _toggle = false)
		{
			GameObject gameObject = Instantiate(PrefabEditor.inst.AddPrefab, Vector3.zero, Quaternion.identity);
			var tf = gameObject.transform;

			var hover = gameObject.AddComponent<HoverUI>();
			hover.animateSca = true;
			hover.animatePos = false;
			hover.size = ConfigEntries.SizeUpper.Value;

			tf.localScale = Vector3.one;

			var name = tf.Find("name").GetComponent<Text>();
			var typeName = tf.Find("type-name").GetComponent<Text>();
			var color = tf.Find("category").GetComponent<Image>();
			var deleteRT = tf.Find("delete").GetComponent<RectTransform>();
			var addPrefabObject = gameObject.GetComponent<Button>();
			var delete = tf.Find("delete").GetComponent<Button>();

			tf.localScale = Vector3.one;

			name.text = _p.Name;
			if (_p.Type < 0 || _p.Type > DataManager.inst.PrefabTypes.Count - 1)
			{
				typeName.text = "invalid";
				color.color = Color.red;
			}
			else
			{
				typeName.text = DataManager.inst.PrefabTypes[_p.Type].Name;
				color.color = DataManager.inst.PrefabTypes[_p.Type].Color;
			}

			Triggers.AddTooltip(gameObject, "<#" + LSColors.ColorToHex(color.color) + ">" + _p.Name + "</color>", "O: " + _p.Offset + "<br>T: " + typeName.text + "<br>Count: " + _p.objects.Count);

			addPrefabObject.onClick.RemoveAllListeners();
			delete.onClick.RemoveAllListeners();

			if (_d == PrefabDialog.Internal)
			{
				//Name Text
				{
					name.horizontalOverflow = ConfigEntries.PrefabINNameHOverflow.Value;
					name.verticalOverflow = ConfigEntries.PrefabINNameVOverflow.Value;
					name.fontSize = ConfigEntries.PrefabINNameFontSize.Value;
				}
				//Type Text
				{
					typeName.horizontalOverflow = ConfigEntries.PrefabINTypeHOverflow.Value;
					typeName.verticalOverflow = ConfigEntries.PrefabINTypeVOverflow.Value;
					typeName.fontSize = ConfigEntries.PrefabINTypeFontSize.Value;
				}

				deleteRT.anchoredPosition = ConfigEntries.PrefabINLDeletePos.Value;
				deleteRT.sizeDelta = ConfigEntries.PrefabINLDeleteSca.Value;
				gameObject.transform.SetParent(PrefabEditorPatch.internalContent);
				delete.onClick.AddListener(delegate ()
				{
					EditorManager.inst.ShowDialog("Warning Popup");
					RefreshWarningPopup("Are you sure you want to delete this prefab? (This is permanent!)", delegate ()
					{
						PrefabEditor.inst.DeleteInternalPrefab(_num);
						EditorManager.inst.HideDialog("Warning Popup");
					}, delegate ()
					{
						EditorManager.inst.HideDialog("Warning Popup");
					});
				});
				addPrefabObject.onClick.AddListener(delegate ()
				{
					if (!_toggle)
					{
						PrefabEditor.inst.AddPrefabObjectToLevel(_p);
						EditorManager.inst.ClearDialogs(new EditorManager.EditorDialog.DialogType[1]);
						return;
					}
					PrefabEditor.inst.UpdateCurrentPrefab(_p);
					PrefabEditor.inst.ReloadInternalPrefabsInPopup(false);
				});
			}
			if (_d == PrefabDialog.External)
			{
				//Name Text
				{
					name.horizontalOverflow = ConfigEntries.PrefabEXNameHOverflow.Value;
					name.verticalOverflow = ConfigEntries.PrefabEXNameVOverflow.Value;
					name.fontSize = ConfigEntries.PrefabEXNameFontSize.Value;
				}
				//Type Text
				{
					typeName.horizontalOverflow = ConfigEntries.PrefabEXTypeHOverflow.Value;
					typeName.verticalOverflow = ConfigEntries.PrefabEXTypeVOverflow.Value;
					typeName.fontSize = ConfigEntries.PrefabEXTypeFontSize.Value;
				}

				deleteRT.anchoredPosition = ConfigEntries.PrefabEXLDeletePos.Value;
				deleteRT.sizeDelta = ConfigEntries.PrefabEXLDeleteSca.Value;
				gameObject.transform.SetParent(PrefabEditorPatch.externalContent);
				delete.onClick.AddListener(delegate ()
				{
					EditorManager.inst.ShowDialog("Warning Popup");
					RefreshWarningPopup("Are you sure you want to delete this prefab? (This is permanent!)", delegate ()
					{
						PrefabEditor.inst.DeleteExternalPrefab(_num);
						EditorManager.inst.HideDialog("Warning Popup");
					}, delegate ()
					{
						EditorManager.inst.HideDialog("Warning Popup");
					});
				});
				addPrefabObject.onClick.AddListener(delegate ()
				{
					PrefabEditor.inst.ImportPrefabIntoLevel(_p);
				});
			}

			tf.localScale = Vector3.one;
			yield break;
		}

		public static bool ContainsName(DataManager.GameData.Prefab _p, PrefabDialog _d)
		{
			if (_d == PrefabDialog.External)
			{
				if (_p.Name.ToLower().Contains(PrefabEditorPatch.externalSearchStr.ToLower()) || DataManager.inst.PrefabTypes[_p.Type].Name.ToLower().Contains(PrefabEditorPatch.externalSearchStr.ToLower()) || string.IsNullOrEmpty(PrefabEditorPatch.externalSearchStr))
				{
					return true;
				}
			}
			if (_d == PrefabDialog.Internal)
			{
				if (_p.Name.ToLower().Contains(PrefabEditorPatch.internalSearchStr.ToLower()) || DataManager.inst.PrefabTypes[_p.Type].Name.ToLower().Contains(PrefabEditorPatch.internalSearchStr.ToLower()) || string.IsNullOrEmpty(PrefabEditorPatch.internalSearchStr))
				{
					return true;
				}
			}
			return false;
		}

		public static IEnumerator GetThemeSprite(DataManager.BeatmapTheme themeTmp, Action<Sprite> _sprite)
		{
			Texture2D texture2D = new Texture2D(16, 16, TextureFormat.ARGB32, false);
			int num2 = 0;
			for (int i = 0; i < 16; i++)
			{
				if (i % 4 == 0)
				{
					num2++;
				}
				for (int j = 0; j < 16; j++)
				{
					texture2D.SetPixel(i, j, themeTmp.GetObjColor(num2 - 1));
				}
			}
			texture2D.filterMode = FilterMode.Point;
			texture2D.Apply();
			_sprite(Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f));
			yield break;
		}

		public static bool loadingThemes = false;

		public static IEnumerator RenderThemeList(Transform __0, string __1)
		{
			var eventEditor = EventEditor.inst;
			var themeEditor = ThemeEditor.inst;

			if (loadingThemes == false && !eventEditor.eventDrag)
            {
				loadingThemes = true;
				Debug.LogFormat("{0}Rendering theme list...", EditorPlugin.className);
				var sw = new System.Diagnostics.Stopwatch();
				sw.Start();

				float delay = 0f;
				Transform parent = __0.Find("themes/viewport/content");
				LSHelpers.DeleteChildren(parent, false);
				int num = 0;

				var cr = Instantiate(eventEditor.ThemeAdd);
				var tf = cr.transform;
				tf.SetParent(parent);
				cr.SetActive(true);
				tf.localScale = Vector2.one;
				cr.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					EventEditorPatch.RenderThemeEditor(-1);
				});

				foreach (var themeTmp in DataManager.inst.AllThemes)
				{
					if (themeTmp.name.ToLower().Contains(__1.ToLower()))
					{
						if (ConfigEntries.IfEditorSlowLoads.Value)
						{
							yield return new WaitForSeconds(delay);
							delay += 0.0001f;
						}
						var tobj = Instantiate(eventEditor.ThemePanel);
						var ttf = tobj.transform;
						ttf.SetParent(parent);
						ttf.localScale = Vector2.one;
						tobj.name = themeTmp.id;

						var image = ttf.Find("image");

						inst.StartCoroutine(GetThemeSprite(themeTmp, delegate (Sprite _sprite)
						{
							image.GetComponent<Image>().sprite = _sprite;
						}));

						int tmpVal = num;
						string tmpThemeID = themeTmp.id;
						image.GetComponent<Button>().onClick.AddListener(delegate ()
						{
							DataManager.inst.gameData.eventObjects.allEvents[eventEditor.currentEventType][eventEditor.currentEvent].eventValues[0] = DataManager.inst.GetThemeIndexToID(tmpVal);
							EventManager.inst.updateEvents();
							eventEditor.RenderThemePreview(__0);
						});
						ttf.Find("edit").GetComponent<Button>().onClick.AddListener(delegate ()
						{
							EventEditorPatch.RenderThemeEditor(int.Parse(tmpThemeID));
						});

						var delete = ttf.Find("delete").GetComponent<Button>();

						delete.interactable = (tmpVal >= DataManager.inst.BeatmapThemes.Count());
						delete.onClick.AddListener(delegate ()
						{
							EditorManager.inst.ShowDialog("Warning Popup");
							RefreshWarningPopup("Are you sure you want to delete this theme?", delegate ()
							{
								themeEditor.DeleteTheme(themeTmp);
								eventEditor.previewTheme.id = null;
								eventEditor.StartCoroutine(ThemeEditor.inst.LoadThemes());
								Transform child = eventEditor.dialogRight.GetChild(eventEditor.currentEventType);
								eventEditor.RenderThemeContent(child, child.Find("theme-search").GetComponent<InputField>().text);
								eventEditor.RenderThemePreview(child);
								eventEditor.showTheme = false;
								eventEditor.dialogLeft.Find("theme").gameObject.SetActive(false);
								EditorManager.inst.HideDialog("Warning Popup");
							}, delegate ()
							{
								EditorManager.inst.HideDialog("Warning Popup");
							});
						});
						ttf.Find("text").GetComponent<Text>().text = themeTmp.name;
					}
					num++;
				}

				Debug.LogFormat("{0}Finished rendering theme list and took {1} to complete!", EditorPlugin.className, sw.Elapsed);
				loadingThemes = false;
			}

			yield break;
		}

		public static void AddEvent(EventEditor __instance, int __0, int __1, bool openDialog = false)
		{
			var item = new EventEditor.KeyframeSelection(__0, __1);
			if (__instance.keyframeSelections.Contains(item))
			{
				__instance.keyframeSelections.Remove(item);
			}
			else
			{
				__instance.keyframeSelections.Add(item);
				__instance.currentEventType = __0;
				__instance.currentEvent = __1;
			}
			if (__instance.keyframeSelections.Count > 1)
			{
				EditorManager.inst.ClearDialogs(Array.Empty<EditorManager.EditorDialog.DialogType>());
				EditorManager.inst.ShowDialog("Multi Keyframe Editor", false);
				__instance.RenderEventObjects();
				Debug.LogFormat("{0}Add keyframe to selection -> [{1}] - [{2}]", new object[]
				{
					"[<color=#e65100>EventEditor</color>]\n",
					__0,
					__1
				});
				return;
			}
			if (openDialog)
			{
				__instance.OpenDialog();
			}
		}

		public static byte[] password = LSEncryption.AES_Encrypt(new byte[] { 9,5,7,6,4,38,6,4,3,66,43,6,47,8,54,6 }, new byte[] { 99, 53, 43 ,36 ,43, 65, 43,45 });

		public static IEnumerator EncryptLevel()
        {
			//var llsb = File.ReadAllBytes(RTFile.GetApplicationDirectory() + GameManager.inst.basePath + "level.lsb");
			//var sogg = File.ReadAllBytes(RTFile.GetApplicationDirectory() + GameManager.inst.basePath + "level.ogg");
			//var ljpg = File.ReadAllBytes(RTFile.GetApplicationDirectory() + GameManager.inst.basePath + "level.jpg");
			//var mlsb = File.ReadAllBytes(RTFile.GetApplicationDirectory() + GameManager.inst.basePath + "metadata.lsb");
			//JSONNode jn = JSON.Parse("{}");
			//
			//for (int i = 0; i < llsb.Length; i++)
			//{
			//	jn["level.lsb"][i] = llsb[i];
			//}
			//
			//for (int i = 0; i < sogg.Length; i++)
			//{
			//	jn["level.ogg"][i] = sogg[i];
			//}
			//
			//for (int i = 0; i < ljpg.Length; i++)
			//{
			//	jn["level.jpg"][i] = ljpg[i];
			//}
			//
			//for (int i = 0; i < mlsb.Length; i++)
			//{
			//	jn["metadata.lsb"][i] = mlsb[i];
			//}

			//RTFile.WriteToFile(GameManager.inst.basePath + "level.lsen", jn.ToString());

			string path = RTFile.GetApplicationDirectory() + EditorPlugin.levelListSlash + EditorManager.inst.currentLoadedLevel + "/level.ogg";
			var songBytes = File.ReadAllBytes(path);
			var encryptedSong = LSEncryption.AES_Encrypt(songBytes, password);
			File.WriteAllBytes(RTFile.GetApplicationDirectory() + EditorPlugin.levelListSlash + EditorManager.inst.currentLoadedLevel + "/song.lsen", encryptedSong);
			yield break;
		}

		public static IEnumerator StartEditorGUI()
        {
			yield return new WaitForSeconds(0.2f);
			EditorGUI.CreateEditorGUI();
			EditorGUI.UpdateEditorGUI();
		}

		public static IEnumerator CreatePropertiesWindow()
        {
			yield return new WaitForSeconds(2f);
			GameObject editorProperties = Instantiate(EditorManager.inst.GetDialog("Object Selector").Dialog.gameObject);
			editorProperties.name = "Editor Properties Popup";
			editorProperties.layer = 5;
			editorProperties.transform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/Popups").transform);
			editorProperties.transform.localScale = Vector3.one * EditorManager.inst.ScreenScale;
			editorProperties.transform.localPosition = Vector3.zero;

			var animUI = editorProperties.AddComponent<AnimateInGUI>();
			animUI.SetEasing((int)ConfigEntries.EPPAnimateEaseIn.Value, (int)ConfigEntries.EPPAnimateEaseOut.Value);
			animUI.animateX = ConfigEntries.EPPAnimateX.Value;
			animUI.animateY = ConfigEntries.EPPAnimateY.Value;
			animUI.animateInTime = ConfigEntries.EPPAnimateInOutSpeeds.Value.x;
			animUI.animateOutTime = ConfigEntries.EPPAnimateInOutSpeeds.Value.y;

			var eSelect = editorProperties.AddComponent<SelectUI>();
			eSelect.target = editorProperties.transform;
			eSelect.ogPos = editorProperties.transform.position;

			Text textFont = GameObject.Find("TitleBar/File/Text").GetComponent<Text>();
			var prefabTMP = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject/prefab");

			//Set Text and stuff
			{
				var searchField = editorProperties.transform.Find("search-box").GetChild(0).GetComponent<InputField>();
				searchField.onValueChanged.RemoveAllListeners();
				searchField.onValueChanged.AddListener(delegate (string _val)
				{
					propertiesSearch = _val;
					RenderPropertiesWindow();
				});
				searchField.placeholder.GetComponent<Text>().text = "Search for property...";
				editorProperties.transform.Find("Panel/Text").GetComponent<Text>().text = "Editor Properties";
            }

			//Sort Layout
			{
				editorProperties.transform.Find("mask/content").GetComponent<GridLayoutGroup>().cellSize = new Vector2(750f, 32f);
				editorProperties.GetComponent<RectTransform>().sizeDelta = new Vector2(750f, 450f);
				editorProperties.transform.Find("Panel").GetComponent<RectTransform>().sizeDelta = new Vector2(782f, 32f);
				editorProperties.transform.Find("search-box").GetComponent<RectTransform>().sizeDelta = new Vector2(750f, 32f);
				editorProperties.transform.Find("search-box").localPosition = new Vector3(0f, 195f, 0f);
				editorProperties.transform.Find("crumbs").GetComponent<RectTransform>().sizeDelta = new Vector2(750f, 32f);
				editorProperties.transform.Find("crumbs").localPosition = new Vector3(0f, 225f, 0f);
				editorProperties.transform.Find("crumbs").GetComponent<HorizontalLayoutGroup>().spacing = 5.5f;
			}


            //Categories
            {
                //General
                {
					GameObject gameObject = Instantiate(prefabTMP);
					gameObject.name = "general";
					gameObject.transform.SetParent(editorProperties.transform.Find("crumbs"));
					gameObject.layer = 5;
					RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
					Image image = gameObject.GetComponent<Image>();
					Button button = gameObject.GetComponent<Button>();

					var hoverUI = gameObject.AddComponent<HoverUI>();
					hoverUI.ogPos = gameObject.transform.localPosition;
					hoverUI.animPos = new Vector3(0f, 6f, 0f);
					hoverUI.size = 1f;
					hoverUI.animatePos = true;
					hoverUI.animateSca = false;

					rectTransform.sizeDelta = new Vector2(100f, 32f);
					rectTransform.anchorMin = new Vector2(-0.1f, -0.1f);

					image.color = LSColors.HexToColor("FFE7E7");
					categoryColors.Add(LSColors.HexToColor("FFE7E7"));

					ColorBlock cb2 = button.colors;
					cb2.normalColor = new Color(1f, 1f, 1f, 1f);
					cb2.pressedColor = new Color(1.5f, 1.5f, 1.5f, 1f);
					cb2.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
					cb2.selectedColor = new Color(1f, 1f, 1f, 1f);
					button.colors = cb2;

					HoverTooltip hoverTooltip = gameObject.GetComponent<HoverTooltip>();

					HoverTooltip.Tooltip tooltip = new HoverTooltip.Tooltip();
					tooltip.desc = "General Editor Settings";
					tooltip.hint = "";

					hoverTooltip.tooltipLangauges.Clear();
					hoverTooltip.tooltipLangauges.Add(tooltip);

					button.onClick.m_Calls.m_ExecutingCalls.Clear();
					button.onClick.m_Calls.m_PersistentCalls.Clear();
					button.onClick.m_PersistentCalls.m_Calls.Clear();
					button.onClick.RemoveAllListeners();
					button.onClick.AddListener(delegate ()
					{
						currentCategory = EditorProperty.EditorPropCategory.General;
						RenderPropertiesWindow();
					});

					GameObject textGameObject = gameObject.transform.GetChild(0).gameObject;
					textGameObject.transform.SetParent(gameObject.transform);
					textGameObject.layer = 5;
					RectTransform textRectTransform = textGameObject.GetComponent<RectTransform>();
					Text textText = textGameObject.GetComponent<Text>();

					textRectTransform.anchoredPosition = Vector2.zero;
					textText.text = "General";
					textText.alignment = TextAnchor.MiddleCenter;
					textText.color = new Color(0.1294f, 0.1294f, 0.1294f, 1f);
					textText.font = textFont.font;
					textText.fontSize = 20;
				}

                //Timeline
                {
					GameObject gameObject = Instantiate(prefabTMP);
					gameObject.name = "timeline";
					gameObject.transform.SetParent(editorProperties.transform.Find("crumbs"));
					gameObject.layer = 5;
					RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
					Image image = gameObject.GetComponent<Image>();
					Button button = gameObject.GetComponent<Button>();

					var hoverUI = gameObject.AddComponent<HoverUI>();
					hoverUI.ogPos = gameObject.transform.localPosition;
					hoverUI.animPos = new Vector3(0f, 6f, 0f);
					hoverUI.size = 1f;
					hoverUI.animatePos = true;
					hoverUI.animateSca = false;

					rectTransform.sizeDelta = new Vector2(100f, 32f);
					rectTransform.anchorMin = new Vector2(-0.1f, -0.1f);

					image.color = LSColors.HexToColor("C0ACE1");
					categoryColors.Add(LSColors.HexToColor("C0ACE1"));

					ColorBlock cb2 = button.colors;
					cb2.normalColor = new Color(1f, 1f, 1f, 1f);
					cb2.pressedColor = new Color(1.5f, 1.5f, 1.5f, 1f);
					cb2.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
					cb2.selectedColor = new Color(1f, 1f, 1f, 1f);
					button.colors = cb2;

					HoverTooltip hoverTooltip = gameObject.GetComponent<HoverTooltip>();

					HoverTooltip.Tooltip tooltip = new HoverTooltip.Tooltip();
					tooltip.desc = "Timeline Settings";
					tooltip.hint = "";

					hoverTooltip.tooltipLangauges.Clear();
					hoverTooltip.tooltipLangauges.Add(tooltip);

					button.onClick.m_Calls.m_ExecutingCalls.Clear();
					button.onClick.m_Calls.m_PersistentCalls.Clear();
					button.onClick.m_PersistentCalls.m_Calls.Clear();
					button.onClick.RemoveAllListeners();
					button.onClick.AddListener(delegate ()
					{
						currentCategory = EditorProperty.EditorPropCategory.Timeline;
						RenderPropertiesWindow();
					});

					GameObject textGameObject = gameObject.transform.GetChild(0).gameObject;
					textGameObject.transform.SetParent(gameObject.transform);
					textGameObject.layer = 5;
					RectTransform textRectTransform = textGameObject.GetComponent<RectTransform>();
					Text textText = textGameObject.GetComponent<Text>();

					textRectTransform.anchoredPosition = Vector2.zero;
					textText.text = "Timeline";
					textText.alignment = TextAnchor.MiddleCenter;
					textText.color = new Color(0.1294f, 0.1294f, 0.1294f, 1f);
					textText.font = textFont.font;
					textText.fontSize = 20;
				}

                //Data
                {
					GameObject gameObject = Instantiate(prefabTMP);
					gameObject.name = "saving";
					gameObject.transform.SetParent(editorProperties.transform.Find("crumbs"));
					gameObject.layer = 5;
					RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
					Image image = gameObject.GetComponent<Image>();
					Button button = gameObject.GetComponent<Button>();

					var hoverUI = gameObject.AddComponent<HoverUI>();
					hoverUI.ogPos = gameObject.transform.localPosition;
					hoverUI.animPos = new Vector3(0f, 6f, 0f);
					hoverUI.size = 1f;
					hoverUI.animatePos = true;
					hoverUI.animateSca = false;

					rectTransform.sizeDelta = new Vector2(100f, 32f);
					rectTransform.anchorMin = new Vector2(-0.1f, -0.1f);

					image.color = LSColors.HexToColor("F17BB8");
					categoryColors.Add(LSColors.HexToColor("F17BB8"));

					ColorBlock cb2 = button.colors;
					cb2.normalColor = new Color(1f, 1f, 1f, 1f);
					cb2.pressedColor = new Color(1.5f, 1.5f, 1.5f, 1f);
					cb2.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
					cb2.selectedColor = new Color(1f, 1f, 1f, 1f);
					button.colors = cb2;

					HoverTooltip hoverTooltip = gameObject.GetComponent<HoverTooltip>();

					HoverTooltip.Tooltip tooltip = new HoverTooltip.Tooltip();
					tooltip.desc = "Data Settings";
					tooltip.hint = "";

					hoverTooltip.tooltipLangauges.Clear();
					hoverTooltip.tooltipLangauges.Add(tooltip);

					button.onClick.m_Calls.m_ExecutingCalls.Clear();
					button.onClick.m_Calls.m_PersistentCalls.Clear();
					button.onClick.m_PersistentCalls.m_Calls.Clear();
					button.onClick.RemoveAllListeners();
					button.onClick.AddListener(delegate ()
					{
						currentCategory = EditorProperty.EditorPropCategory.Data;
						RenderPropertiesWindow();
					});

					GameObject textGameObject = gameObject.transform.GetChild(0).gameObject;
					textGameObject.transform.SetParent(gameObject.transform);
					textGameObject.layer = 5;
					RectTransform textRectTransform = textGameObject.GetComponent<RectTransform>();
					textGameObject.GetComponent<CanvasRenderer>();
					Text textText = textGameObject.GetComponent<Text>();

					textRectTransform.anchoredPosition = Vector2.zero;
					textText.text = "Data";
					textText.alignment = TextAnchor.MiddleCenter;
					textText.color = new Color(0.1294f, 0.1294f, 0.1294f, 1f);
					textText.font = textFont.font;
					textText.fontSize = 20;
				}

                //Editor GUI
                {
					GameObject gameObject = Instantiate(prefabTMP);
					gameObject.name = "editorgui";
					gameObject.transform.SetParent(editorProperties.transform.Find("crumbs"));
					gameObject.layer = 5;
					RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
					Image image = gameObject.GetComponent<Image>();
					Button button = gameObject.GetComponent<Button>();

					var hoverUI = gameObject.AddComponent<HoverUI>();
					hoverUI.ogPos = gameObject.transform.localPosition;
					hoverUI.animPos = new Vector3(0f, 6f, 0f);
					hoverUI.size = 1f;
					hoverUI.animatePos = true;
					hoverUI.animateSca = false;

					rectTransform.sizeDelta = new Vector2(100f, 32f);
					rectTransform.anchorMin = new Vector2(-0.1f, -0.1f);

					image.color = LSColors.HexToColor("2F426D");
					categoryColors.Add(LSColors.HexToColor("2F426D"));

					ColorBlock cb2 = button.colors;
					cb2.normalColor = new Color(1f, 1f, 1f, 1f);
					cb2.pressedColor = new Color(1.5f, 1.5f, 1.5f, 1f);
					cb2.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
					cb2.selectedColor = new Color(1f, 1f, 1f, 1f);
					button.colors = cb2;

					HoverTooltip hoverTooltip = gameObject.GetComponent<HoverTooltip>();

					HoverTooltip.Tooltip tooltip = new HoverTooltip.Tooltip();
					tooltip.desc = "GUI Settings";
					tooltip.hint = "";

					hoverTooltip.tooltipLangauges.Clear();
					hoverTooltip.tooltipLangauges.Add(tooltip);

					button.onClick.m_Calls.m_ExecutingCalls.Clear();
					button.onClick.m_Calls.m_PersistentCalls.Clear();
					button.onClick.m_PersistentCalls.m_Calls.Clear();
					button.onClick.RemoveAllListeners();
					button.onClick.AddListener(delegate ()
					{
						currentCategory = EditorProperty.EditorPropCategory.EditorGUI;
						RenderPropertiesWindow();
					});

					GameObject textGameObject = gameObject.transform.GetChild(0).gameObject;
					textGameObject.transform.SetParent(gameObject.transform);
					textGameObject.layer = 5;
					RectTransform textRectTransform = textGameObject.GetComponent<RectTransform>();
					Text textText = textGameObject.GetComponent<Text>();

					textRectTransform.anchoredPosition = Vector2.zero;
					textText.text = "Editor GUI";
					textText.alignment = TextAnchor.MiddleCenter;
					textText.color = new Color(0.1294f, 0.1294f, 0.1294f, 1f);
					textText.font = textFont.font;
					textText.fontSize = 20;
				}

                //Markers
                {
					GameObject gameObject = Instantiate(prefabTMP);
					gameObject.name = "markers";
					gameObject.transform.SetParent(editorProperties.transform.Find("crumbs"));
					gameObject.layer = 5;
					RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
					Image image = gameObject.GetComponent<Image>();
					Button button = gameObject.GetComponent<Button>();

					var hoverUI = gameObject.AddComponent<HoverUI>();
					hoverUI.ogPos = gameObject.transform.localPosition;
					hoverUI.animPos = new Vector3(0f, 6f, 0f);
					hoverUI.size = 1f;
					hoverUI.animatePos = true;
					hoverUI.animateSca = false;

					rectTransform.sizeDelta = new Vector2(100f, 32f);
					rectTransform.anchorMin = new Vector2(-0.1f, -0.1f);

					image.color = LSColors.HexToColor("4076DF");
					categoryColors.Add(LSColors.HexToColor("4076DF"));

					ColorBlock cb2 = button.colors;
					cb2.normalColor = new Color(1f, 1f, 1f, 1f);
					cb2.pressedColor = new Color(1.5f, 1.5f, 1.5f, 1f);
					cb2.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
					cb2.selectedColor = new Color(1f, 1f, 1f, 1f);
					button.colors = cb2;

					HoverTooltip hoverTooltip = gameObject.GetComponent<HoverTooltip>();

					HoverTooltip.Tooltip tooltip = new HoverTooltip.Tooltip();
					tooltip.desc = "Markers Settings";
					tooltip.hint = "";

					hoverTooltip.tooltipLangauges.Clear();
					hoverTooltip.tooltipLangauges.Add(tooltip);

					button.onClick.m_Calls.m_ExecutingCalls.Clear();
					button.onClick.m_Calls.m_PersistentCalls.Clear();
					button.onClick.m_PersistentCalls.m_Calls.Clear();
					button.onClick.RemoveAllListeners();
					button.onClick.AddListener(delegate ()
					{
						currentCategory = EditorProperty.EditorPropCategory.Markers;
						RenderPropertiesWindow();
					});

					GameObject textGameObject = gameObject.transform.GetChild(0).gameObject;
					textGameObject.transform.SetParent(gameObject.transform);
					textGameObject.layer = 5;
					RectTransform textRectTransform = textGameObject.GetComponent<RectTransform>();
					Text textText = textGameObject.GetComponent<Text>();

					textRectTransform.anchoredPosition = Vector2.zero;
					textText.text = "Markers";
					textText.alignment = TextAnchor.MiddleCenter;
					textText.color = new Color(0.1294f, 0.1294f, 0.1294f, 1f);
					textText.font = textFont.font;
					textText.fontSize = 20;
				}

                //Fields
                {
					GameObject gameObject = Instantiate(prefabTMP);
					gameObject.name = "fields";
					gameObject.transform.SetParent(editorProperties.transform.Find("crumbs"));
					gameObject.layer = 5;
					RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
					Image image = gameObject.GetComponent<Image>();
					Button button = gameObject.GetComponent<Button>();

					var hoverUI = gameObject.AddComponent<HoverUI>();
					hoverUI.ogPos = gameObject.transform.localPosition;
					hoverUI.animPos = new Vector3(0f, 6f, 0f);
					hoverUI.size = 1f;
					hoverUI.animatePos = true;
					hoverUI.animateSca = false;

					rectTransform.sizeDelta = new Vector2(100f, 32f);
					rectTransform.anchorMin = new Vector2(-0.1f, -0.1f);

					image.color = LSColors.HexToColor("6CCBCF");
					categoryColors.Add(LSColors.HexToColor("6CCBCF"));

					ColorBlock cb2 = button.colors;
					cb2.normalColor = new Color(1f, 1f, 1f, 1f);
					cb2.pressedColor = new Color(1.5f, 1.5f, 1.5f, 1f);
					cb2.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
					cb2.selectedColor = new Color(1f, 1f, 1f, 1f);
					button.colors = cb2;

					HoverTooltip hoverTooltip = gameObject.GetComponent<HoverTooltip>();

					HoverTooltip.Tooltip tooltip = new HoverTooltip.Tooltip();
					tooltip.desc = "Fields Settings";
					tooltip.hint = "";

					hoverTooltip.tooltipLangauges.Clear();
					hoverTooltip.tooltipLangauges.Add(tooltip);

					button.onClick.m_Calls.m_ExecutingCalls.Clear();
					button.onClick.m_Calls.m_PersistentCalls.Clear();
					button.onClick.m_PersistentCalls.m_Calls.Clear();
					button.onClick.RemoveAllListeners();
					button.onClick.AddListener(delegate ()
					{
						currentCategory = EditorProperty.EditorPropCategory.Fields;
						RenderPropertiesWindow();
					});

					GameObject textGameObject = gameObject.transform.GetChild(0).gameObject;
					textGameObject.transform.SetParent(gameObject.transform);
					textGameObject.layer = 5;
					RectTransform textRectTransform = textGameObject.GetComponent<RectTransform>();
					Text textText = textGameObject.GetComponent<Text>();

					textRectTransform.anchoredPosition = Vector2.zero;
					textText.text = "Fields";
					textText.alignment = TextAnchor.MiddleCenter;
					textText.color = new Color(0.1294f, 0.1294f, 0.1294f, 1f);
					textText.font = textFont.font;
					textText.fontSize = 20;
				}

                //Preview
                {
					GameObject gameObject = Instantiate(prefabTMP);
					gameObject.name = "preview";
					gameObject.transform.SetParent(editorProperties.transform.Find("crumbs"));
					gameObject.layer = 5;
					RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
					Image image = gameObject.GetComponent<Image>();
					Button button = gameObject.GetComponent<Button>();

					var hoverUI = gameObject.AddComponent<HoverUI>();
					hoverUI.ogPos = gameObject.transform.localPosition;
					hoverUI.animPos = new Vector3(0f, 6f, 0f);
					hoverUI.size = 1f;
					hoverUI.animatePos = true;
					hoverUI.animateSca = false;

					rectTransform.sizeDelta = new Vector2(100f, 32f);
					rectTransform.anchorMin = new Vector2(-0.1f, -0.1f);

					image.color = LSColors.HexToColor("1B1B1C");
					categoryColors.Add(LSColors.HexToColor("1B1B1C"));

					ColorBlock cb2 = button.colors;
					cb2.normalColor = new Color(1f, 1f, 1f, 1f);
					cb2.pressedColor = new Color(1.5f, 1.5f, 1.5f, 1f);
					cb2.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
					cb2.selectedColor = new Color(1f, 1f, 1f, 1f);
					button.colors = cb2;

					HoverTooltip hoverTooltip = gameObject.GetComponent<HoverTooltip>();

					HoverTooltip.Tooltip tooltip = new HoverTooltip.Tooltip();
					tooltip.desc = "Preview Settings";
					tooltip.hint = "";

					hoverTooltip.tooltipLangauges.Clear();
					hoverTooltip.tooltipLangauges.Add(tooltip);

					button.onClick.m_Calls.m_ExecutingCalls.Clear();
					button.onClick.m_Calls.m_PersistentCalls.Clear();
					button.onClick.m_PersistentCalls.m_Calls.Clear();
					button.onClick.RemoveAllListeners();
					button.onClick.AddListener(delegate ()
					{
						currentCategory = EditorProperty.EditorPropCategory.Preview;
						RenderPropertiesWindow();
					});

					GameObject textGameObject = gameObject.transform.GetChild(0).gameObject;
					textGameObject.transform.SetParent(gameObject.transform);
					textGameObject.layer = 5;
					RectTransform textRectTransform = textGameObject.GetComponent<RectTransform>();
					Text textText = textGameObject.GetComponent<Text>();

					textRectTransform.anchoredPosition = Vector2.zero;
					textText.text = "Preview";
					textText.alignment = TextAnchor.MiddleCenter;
					textText.color = new Color(0.95f, 0.95f, 0.95f, 1f);
					textText.font = textFont.font;
					textText.fontSize = 20;
				}
            }

			var propWin = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/Edit/Edit Dropdown/Cut"));
			propWin.transform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/Edit/Edit Dropdown").transform);
			propWin.transform.localScale = Vector3.one;
			propWin.name = "Preferences";
			propWin.transform.Find("Text").GetComponent<Text>().text = "Preferences";
			propWin.transform.Find("Text 1").GetComponent<Text>().text = "F10";

			propWin.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
			propWin.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
			propWin.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
			propWin.GetComponent<Button>().onClick.RemoveAllListeners();
			propWin.GetComponent<Button>().onClick.AddListener(delegate ()
			{
				OpenPropertiesWindow();
			});

			propWin.SetActive(true);


			string jpgFileLocation = "BepInEx/plugins/Assets/editor_gui_preferences-white.png";

			if (RTFile.FileExists(jpgFileLocation))
			{
				Image spriteReloader = propWin.transform.Find("Image").GetComponent<Image>();

				EditorManager.inst.StartCoroutine(EditorManager.inst.GetSprite(RTFile.GetApplicationDirectory() + jpgFileLocation, new EditorManager.SpriteLimits(), delegate (Sprite cover)
				{
					spriteReloader.sprite = cover;
				}, delegate (string errorFile)
				{
					spriteReloader.sprite = ArcadeManager.inst.defaultImage;
				}));
			}

			editorProperties.transform.Find("Panel/x").GetComponent<Button>().onClick.RemoveAllListeners();
			editorProperties.transform.Find("Panel/x").GetComponent<Button>().onClick.AddListener(delegate ()
			{
				ClosePropertiesWindow();
			});

			//Add Editor Properties Popup to EditorDialogsDictionary
			{
				EditorManager.EditorDialog editorPropertiesDialog = new EditorManager.EditorDialog();

				editorPropertiesDialog.Dialog = editorProperties.transform;
				editorPropertiesDialog.Name = "Editor Properties Popup";
				editorPropertiesDialog.Type = EditorManager.EditorDialog.DialogType.Object;

				EditorManager.inst.EditorDialogs.Add(editorPropertiesDialog);

				var editorDialogsDictionary = AccessTools.Field(typeof(EditorManager), "EditorDialogsDictionary");

				var editorDialogsDictionaryInst = AccessTools.Field(typeof(EditorManager), "EditorDialogsDictionary").GetValue(EditorManager.inst);

				editorDialogsDictionary.GetValue(EditorManager.inst).GetType().GetMethod("Add").Invoke(editorDialogsDictionaryInst, new object[] { "Editor Properties Popup", editorPropertiesDialog });
			}
			yield break;
        }

		public static void OpenPropertiesWindow(bool _toggle = false)
		{
			if (EditorManager.inst != null)
			{
				if (EditorManager.inst.GetDialog("Editor Properties Popup").Dialog.gameObject.activeSelf == false)
				{
					EditorManager.inst.ShowDialog("Editor Properties Popup");
					RenderPropertiesWindow();
				}
				else if (_toggle == true)
				{
					EditorManager.inst.HideDialog("Editor Properties Popup");
				}
			}
		}

		public static void ClosePropertiesWindow()
		{
			if (EditorManager.inst != null)
			{
				EditorManager.inst.GetDialog("Editor Properties Popup").Dialog.GetComponent<AnimateInGUI>().OnDisableManual();
			}
		}

		public static void ScaleTabs(int _tab)
		{
			var editorDialog = EditorManager.inst.GetDialog("Editor Properties Popup").Dialog;
			for (int i = 0; i < editorDialog.Find("crumbs").childCount; i++)
			{
				var col = editorDialog.Find("crumbs").GetChild(i).GetComponent<Image>().color;
				editorDialog.Find("crumbs").GetChild(i).localScale = Vector3.one;
				if (i == _tab)
                {
					editorDialog.Find("crumbs").GetChild(i).GetComponent<Image>().DOColor(LSColors.HexToColor("E57373"), 0.3f).SetEase(DataManager.inst.AnimationList[3].Animation).Play();
				}
				else
				{
					editorDialog.Find("crumbs").GetChild(i).GetComponent<Image>().DOColor(categoryColors[i], 0.3f).SetEase(DataManager.inst.AnimationList[3].Animation).Play();
				}
            }
		}

		public static List<Color> categoryColors = new List<Color>();

		public static float SnapToBPM(float _time)
		{
			return (float)Mathf.RoundToInt(_time / (SettingEditor.inst.BPMMulti / ConfigEntries.SnapAmount.Value)) * (SettingEditor.inst.BPMMulti / ConfigEntries.SnapAmount.Value);
		}

		public static void RenderPropertiesWindow()
        {
			var editorDialog = EditorManager.inst.GetDialog("Editor Properties Popup").Dialog;
			var label = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content").transform.GetChild(2).gameObject;
			var singleInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move/position/x");
			var vector2Input = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move/position");
			var boolInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/toggle/toggle");
			var dropdownInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/autokill/tod-dropdown");
			var sliderFullInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/bpm");
			var stringInput = GameObject.Find("TimelineBar/GameObject/Time Input");

			Text textFont = GameObject.Find("TitleBar/File/Text").GetComponent<Text>();

			LSHelpers.DeleteChildren(editorDialog.Find("mask/content"));

			switch (currentCategory)
            {
				case EditorProperty.EditorPropCategory.General:
                    {
						ScaleTabs(0);
						break;
                    }
				case EditorProperty.EditorPropCategory.Timeline:
                    {
						ScaleTabs(1);
						break;
                    }
				case EditorProperty.EditorPropCategory.Data:
                    {
						ScaleTabs(2);
						break;
                    }
				case EditorProperty.EditorPropCategory.EditorGUI:
                    {
						ScaleTabs(3);
						break;
                    }
				case EditorProperty.EditorPropCategory.Markers:
                    {
						ScaleTabs(4);
						break;
                    }
				case EditorProperty.EditorPropCategory.Fields:
                    {
						ScaleTabs(5);
						break;
                    }
				case EditorProperty.EditorPropCategory.Preview:
                    {
						ScaleTabs(6);
						break;
                    }
            }

			foreach (var prop in editorProperties)
            {
				if (currentCategory == prop.propCategory && (string.IsNullOrEmpty(propertiesSearch) || prop.name.ToLower().Contains(propertiesSearch.ToLower())))
                {
					switch (prop.valueType)
                    {
						case EditorProperty.ValueType.Bool:
                            {
								var bar = Instantiate(singleInput);
								Destroy(bar.GetComponent<InputField>());
								Destroy(bar.GetComponent<EventInfo>());
								LSHelpers.DeleteChildren(bar.transform);
								bar.transform.SetParent(editorDialog.Find("mask/content"));
								bar.transform.localScale = Vector3.one;
								bar.name = "input [BOOL]";

								Triggers.AddTooltip(bar, prop.name, prop.description, new List<string> { prop.configEntry.BoxedValue.GetType().ToString() });

								var l = Instantiate(label);
								l.transform.SetParent(bar.transform);
								l.transform.SetAsFirstSibling();
								l.transform.localScale = Vector3.one;
								l.transform.GetChild(0).GetComponent<Text>().text = prop.name;
								l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(688f, 20f);

								var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
                                {
									ltextrt.anchoredPosition = new Vector2(10f, -5f);
                                }

								bar.GetComponent<Image>().enabled = true;
								bar.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);

								GameObject x = Instantiate(boolInput);
								x.transform.SetParent(bar.transform);
								x.transform.localScale = Vector3.one;

								Toggle xt = x.GetComponent<Toggle>();
								xt.onValueChanged.RemoveAllListeners();
								xt.isOn = (bool)prop.configEntry.BoxedValue;
								xt.onValueChanged.AddListener(delegate (bool _val)
								{
									prop.configEntry.BoxedValue = _val;
								});
								break;
                            }
						case EditorProperty.ValueType.Int:
							{
								GameObject x = Instantiate(singleInput);
								x.transform.SetParent(editorDialog.Find("mask/content"));
								x.name = "input [INT]";

								Destroy(x.GetComponent<EventInfo>());
								x.transform.localScale = Vector3.one;
								x.transform.GetChild(0).localScale = Vector3.one;

								var l = Instantiate(label);
								l.transform.SetParent(x.transform);
								l.transform.SetAsFirstSibling();
								l.transform.GetChild(0).GetComponent<Text>().text = prop.name;
								l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(541f, 20f);

								var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
								{
									ltextrt.anchoredPosition = new Vector2(10f, -5f);
								}

								x.GetComponent<Image>().enabled = true;
								x.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);

								Triggers.AddTooltip(x, prop.name, prop.description, new List<string> { prop.configEntry.BoxedValue.GetType().ToString() });

								var xif = x.GetComponent<InputField>();
								xif.onValueChanged.RemoveAllListeners();
								xif.characterValidation = InputField.CharacterValidation.Integer;
								xif.text = prop.configEntry.BoxedValue.ToString();
								xif.onValueChanged.AddListener(delegate (string _val)
								{
									prop.configEntry.BoxedValue = int.Parse(_val);
								});
								break;
                            }
						case EditorProperty.ValueType.Float:
							{
								GameObject x = Instantiate(singleInput);
								x.transform.SetParent(editorDialog.Find("mask/content"));
								x.name = "input [FLOAT]";

								Destroy(x.GetComponent<EventInfo>());

								var l = Instantiate(label);
								l.transform.SetParent(x.transform);
								l.transform.SetAsFirstSibling();
								l.transform.GetChild(0).GetComponent<Text>().text = prop.name;
								l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(541f, 20f);

								var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
								{
									ltextrt.anchoredPosition = new Vector2(10f, -5f);
								}

								x.transform.localScale = Vector3.one;
								x.transform.GetChild(0).localScale = Vector3.one;

								x.GetComponent<Image>().enabled = true;
								x.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);

								Triggers.AddTooltip(x, prop.name, prop.description, new List<string> { prop.configEntry.BoxedValue.GetType().ToString() });

								var xif = x.GetComponent<InputField>();
								xif.onValueChanged.RemoveAllListeners();
								xif.characterValidation = InputField.CharacterValidation.None;
								xif.text = prop.configEntry.BoxedValue.ToString();
								xif.onValueChanged.AddListener(delegate (string _val)
								{
									prop.configEntry.BoxedValue = float.Parse(_val);
								});
								break;
                            }
						case EditorProperty.ValueType.IntSlider:
							{
								GameObject x = Instantiate(sliderFullInput);
								x.transform.SetParent(editorDialog.Find("mask/content"));
								x.name = "input [INTSLIDER]";

								var title = x.transform.Find("title");

								var l = title.GetComponent<Text>();
								l.font = textFont.font;
								l.text = prop.name;

								var titleRT = title.GetComponent<RectTransform>();
								titleRT.sizeDelta = new Vector2(220f, 32f);
								titleRT.anchoredPosition = new Vector2(122f, -16f);

								var image = x.AddComponent<Image>();
								image.color = new Color(1f, 1f, 1f, 0.03f);

								Triggers.AddTooltip(x, prop.name, prop.description, new List<string> { prop.configEntry.BoxedValue.GetType().ToString() });

								x.transform.Find("slider").GetComponent<RectTransform>().sizeDelta = new Vector2(295f, 32f);
								var xsli = x.transform.Find("slider").GetComponent<Slider>();
								xsli.onValueChanged.RemoveAllListeners();

								xsli.value = (int)prop.configEntry.BoxedValue * 10;

								xsli.maxValue = 100f;
								xsli.minValue = -100f;

								var xif = x.transform.Find("input").GetComponent<InputField>();
								xif.onValueChanged.RemoveAllListeners();
								xif.characterValidation = InputField.CharacterValidation.Integer;
								xif.text = prop.configEntry.BoxedValue.ToString();

								xif.onValueChanged.AddListener(delegate (string _val)
								{
									if (LSHelpers.IsUsingInputField())
									{
										prop.configEntry.BoxedValue = float.Parse(_val);
										xsli.value = int.Parse(_val);
									}
								});

								xsli.onValueChanged.AddListener(delegate (float _val)
								{
									if (!LSHelpers.IsUsingInputField())
									{
										prop.configEntry.BoxedValue = _val;
										xif.text = _val.ToString();
									}
								});
								break;
                            }
						case EditorProperty.ValueType.FloatSlider:
							{
								GameObject x = Instantiate(sliderFullInput);
								x.transform.SetParent(editorDialog.Find("mask/content"));
								x.transform.localScale = Vector3.one;
								x.name = "input [FLOATSLIDER]";

								var title = x.transform.Find("title");

								var l = title.GetComponent<Text>();
								l.font = textFont.font;
								l.text = prop.name;

								x.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;

								var titleRT = title.GetComponent<RectTransform>();
								titleRT.sizeDelta = new Vector2(220f, 32f);
								titleRT.anchoredPosition = new Vector2(122f, -16f);

								var image = x.AddComponent<Image>();
								image.color = new Color(1f, 1f, 1f, 0.03f);

								Triggers.AddTooltip(x, prop.name, prop.description, new List<string> { prop.configEntry.BoxedValue.GetType().ToString() });

								x.transform.Find("slider").GetComponent<RectTransform>().sizeDelta = new Vector2(295f, 32f);
								var xsli = x.transform.Find("slider").GetComponent<Slider>();
								xsli.onValueChanged.RemoveAllListeners();

								xsli.value = (float)prop.configEntry.BoxedValue * 10;

								xsli.maxValue = 100f;
								xsli.minValue = -100f;

								var xif = x.transform.Find("input").GetComponent<InputField>();
								xif.onValueChanged.RemoveAllListeners();
								xif.characterValidation = InputField.CharacterValidation.None;
								xif.text = prop.configEntry.BoxedValue.ToString();

								xif.onValueChanged.AddListener(delegate (string _val)
								{
									if (LSHelpers.IsUsingInputField())
									{
										prop.configEntry.BoxedValue = float.Parse(_val);
										int v = int.Parse(_val) * 10;
										xsli.value = v;
									}
								});

								xsli.onValueChanged.AddListener(delegate (float _val)
								{
									if (!LSHelpers.IsUsingInputField())
									{
										int v = (int)_val * 10;
										float v2 = v / 100f;
										prop.configEntry.BoxedValue = v2;
										xif.text = v2.ToString();
									}
								});

								break;
                            }
						case EditorProperty.ValueType.String:
                            {
								var bar = Instantiate(singleInput);
								Destroy(bar.GetComponent<InputField>());
								Destroy(bar.GetComponent<EventInfo>());
								LSHelpers.DeleteChildren(bar.transform);
								bar.transform.SetParent(editorDialog.Find("mask/content"));
								bar.transform.localScale = Vector3.one;
								bar.name = "input [STRING]";

								Triggers.AddTooltip(bar, prop.name, prop.description, new List<string> { prop.configEntry.BoxedValue.GetType().ToString() });

								var l = Instantiate(label);
								l.transform.SetParent(bar.transform);
								l.transform.SetAsFirstSibling();
								l.transform.localScale = Vector3.one;
								l.transform.GetChild(0).GetComponent<Text>().text = prop.name;
								l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(354f, 20f);

								var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
								{
									ltextrt.anchoredPosition = new Vector2(10f, -5f);
								}

								bar.GetComponent<Image>().enabled = true;
								bar.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);

								GameObject x = Instantiate(stringInput);
								x.transform.SetParent(bar.transform);
								x.transform.localScale = Vector3.one;
								Destroy(x.GetComponent<HoverTooltip>());

								x.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(366f, 32f);

								var xif = x.GetComponent<InputField>();
								xif.onValueChanged.RemoveAllListeners();
								xif.characterValidation = InputField.CharacterValidation.None;
								xif.characterLimit = 0;
								xif.text = prop.configEntry.BoxedValue.ToString();
								xif.textComponent.fontSize = 18;
								xif.onValueChanged.AddListener(delegate (string _val)
								{
									prop.configEntry.BoxedValue = _val;
								});

								break;
                            }
						case EditorProperty.ValueType.Vector2:
							{
								var bar = Instantiate(singleInput);
								Destroy(bar.GetComponent<InputField>());
								Destroy(bar.GetComponent<EventInfo>());
								LSHelpers.DeleteChildren(bar.transform);
								bar.transform.SetParent(editorDialog.Find("mask/content"));
								bar.transform.localScale = Vector3.one;
								bar.name = "input [VECTOR2]";

								Triggers.AddTooltip(bar, prop.name, prop.description, new List<string> { prop.configEntry.BoxedValue.GetType().ToString() });

								var l = Instantiate(label);
								l.transform.SetParent(bar.transform);
								l.transform.SetAsFirstSibling();
								l.transform.localScale = Vector3.one;
								l.transform.GetChild(0).GetComponent<Text>().text = prop.name;
								l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(354f, 20f);

								var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
								{
									ltextrt.anchoredPosition = new Vector2(10f, -5f);
								}

								bar.GetComponent<Image>().enabled = true;
								bar.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);

								GameObject vector2 = Instantiate(vector2Input);
								vector2.transform.SetParent(bar.transform);
								vector2.transform.localScale = Vector3.one;

								Vector2 vtmp = (Vector2)prop.configEntry.BoxedValue;

								Destroy(vector2.transform.Find("x").GetComponent<EventInfo>());
								vector2.transform.Find("x").localScale = Vector3.one;
								vector2.transform.Find("x").GetChild(0).localScale = Vector3.one;
								var vxif = vector2.transform.Find("x").GetComponent<InputField>();
                                {
                                    vxif.onValueChanged.RemoveAllListeners();

									vxif.text = vtmp.x.ToString();

									vxif.onValueChanged.AddListener(delegate (string _val)
									{
										vtmp = new Vector2(float.Parse(_val), vtmp.y);
										prop.configEntry.BoxedValue = vtmp;
									});
								}

								Destroy(vector2.transform.Find("y").GetComponent<EventInfo>());
								vector2.transform.Find("y").localScale = Vector3.one;
								vector2.transform.Find("x").GetChild(0).localScale = Vector3.one;
								var vyif = vector2.transform.Find("y").GetComponent<InputField>();
								{
									vyif.onValueChanged.RemoveAllListeners();

									vyif.text = vtmp.y.ToString();

									vyif.onValueChanged.AddListener(delegate (string _val)
									{
										vtmp = new Vector2(vtmp.x, float.Parse(_val));
										prop.configEntry.BoxedValue = vtmp;
									});
								}
								break;
                            }
						case EditorProperty.ValueType.Vector3:
                            {
								Debug.Log("lol");
								break;
                            }
						case EditorProperty.ValueType.Enum:
                            {
								var bar = Instantiate(singleInput);
								Destroy(bar.GetComponent<InputField>());
								Destroy(bar.GetComponent<EventInfo>());
								LSHelpers.DeleteChildren(bar.transform);
								bar.transform.SetParent(editorDialog.Find("mask/content"));
								bar.transform.localScale = Vector3.one;
								bar.name = "input [ENUM]";

								Triggers.AddTooltip(bar, prop.name, prop.description, new List<string> { prop.configEntry.BoxedValue.GetType().ToString() });

								var l = Instantiate(label);
								l.transform.SetParent(bar.transform);
								l.transform.SetAsFirstSibling();
								l.transform.localScale = Vector3.one;
								l.transform.GetChild(0).GetComponent<Text>().text = prop.name;
								l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(522f, 20f);

								var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
								{
									ltextrt.anchoredPosition = new Vector2(10f, -5f);
								}

								bar.GetComponent<Image>().enabled = true;
								bar.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);

								GameObject x = Instantiate(dropdownInput);
								x.transform.SetParent(bar.transform);
								x.transform.localScale = Vector3.one;

								RectTransform xRT = x.GetComponent<RectTransform>();
								xRT.anchoredPosition = ConfigEntries.ORLDropdownPos.Value;

								Destroy(x.GetComponent<HoverTooltip>());
								Destroy(x.GetComponent<HideDropdownOptions>());

								Dropdown dropdown = x.GetComponent<Dropdown>();
								dropdown.options.Clear();
								dropdown.onValueChanged.RemoveAllListeners();
								Type type = prop.configEntry.SettingType;

								string[] PieceTypeNames = Enum.GetNames(prop.configEntry.SettingType);
								for (int i = 0; i < PieceTypeNames.Length; i++)
								{
									dropdown.options.Add(new Dropdown.OptionData(PieceTypeNames[i]));
								}

								dropdown.onValueChanged.AddListener(delegate (int _val)
								{
									prop.configEntry.BoxedValue = _val;
								});

								dropdown.value = (int)prop.configEntry.BoxedValue;

								break;
                            }
						case EditorProperty.ValueType.Color:
							{
								var bar = Instantiate(singleInput);
								Destroy(bar.GetComponent<InputField>());
								Destroy(bar.GetComponent<EventInfo>());
								LSHelpers.DeleteChildren(bar.transform);
								bar.transform.SetParent(editorDialog.Find("mask/content"));
								bar.transform.localScale = Vector3.one;
								bar.name = "input [COLOR]";

								Triggers.AddTooltip(bar, prop.name, prop.description, new List<string> { prop.configEntry.BoxedValue.GetType().ToString() });

								var l = Instantiate(label);
								l.transform.SetParent(bar.transform);
								l.transform.SetAsFirstSibling();
								l.transform.localScale = Vector3.one;
								l.transform.GetChild(0).GetComponent<Text>().text = prop.name;
								l.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(314f, 20f);

								var ltextrt = l.transform.GetChild(0).GetComponent<RectTransform>();
								{
									ltextrt.anchoredPosition = new Vector2(10f, -5f);
								}

								bar.GetComponent<Image>().enabled = true;
								bar.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);

								var bar2 = Instantiate(singleInput);
								Destroy(bar2.GetComponent<InputField>());
								Destroy(bar2.GetComponent<EventInfo>());
								LSHelpers.DeleteChildren(bar2.transform);
								bar2.transform.SetParent(bar.transform);
								bar2.transform.localScale = Vector3.one;
								bar2.name = "color";
								bar2.GetComponent<RectTransform>().sizeDelta = new Vector2(32f, 32f);

								var bar2Color = bar2.GetComponent<Image>();
								bar2Color.enabled = true;
								bar2Color.color = (Color)prop.configEntry.BoxedValue;

								if (!bar2.GetComponent<EventTrigger>())
                                {
									bar2.AddComponent<EventTrigger>();
                                }

								GameObject x = Instantiate(stringInput);
								x.transform.SetParent(bar.transform);
								x.transform.localScale = Vector3.one;
								Destroy(x.GetComponent<HoverTooltip>());

								x.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(366f, 32f);

								var xif = x.GetComponent<InputField>();
								xif.onValueChanged.RemoveAllListeners();
								xif.characterValidation = InputField.CharacterValidation.None;
								xif.characterLimit = 0;
								xif.text = ColorToHex((Color)prop.configEntry.BoxedValue);
								xif.textComponent.fontSize = 18;
								xif.onValueChanged.AddListener(delegate (string _val)
								{
									if (xif.text.Length == 8)
									{
										prop.configEntry.BoxedValue = LSColors.HexToColorAlpha(_val);
										bar2Color.color = (Color)prop.configEntry.BoxedValue;
									}
								});

								var trigger = bar2.GetComponent<EventTrigger>();
								trigger.triggers.Add(Triggers.CreatePreviewClickTrigger(bar2.transform, x.transform, (Color)prop.configEntry.BoxedValue, "Editor Properties Popup"));
								break;
                            }
                    }
				}
            }
        }

		public static List<EditorProperty> editorProperties = new List<EditorProperty>
		{
			//General
			new EditorProperty("Debug", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.General, ConfigEntries.EditorDebug, "Adds some debug functionality, such as showing the Unity debug logs through the in-game notifications (sometimes no the best idea to have on for this reason) and some object debugging."),
			new EditorProperty("Reminder Active", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.General, ConfigEntries.ReminderActive, "Will enable the reminder to tell you to have a break."),
			new EditorProperty("Reminder Loop Time", EditorProperty.ValueType.Float, EditorProperty.EditorPropCategory.General, ConfigEntries.ReminderRepeat, "The time between each reminder."),
			new EditorProperty("BPM Snaps Keyframes", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.General, ConfigEntries.KeyframeSnap, "Makes object's keyframes snap if Snap BPM is enabled."),
			new EditorProperty("BPM Snap Divisions", EditorProperty.ValueType.Float, EditorProperty.EditorPropCategory.General, ConfigEntries.SnapAmount, "How many times the snap is divided into. Can be good for songs that don't do 4 time signatures."),
			new EditorProperty("Preferences Open Key", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.General, ConfigEntries.EditorPropertiesKey, "What key should be pressed to open the Editor Preferences window."),
			new EditorProperty("Prefab Example Template", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.General, ConfigEntries.EXPrefab, "If an Example prefab template should generate and be added to your internal prefabs."),
			new EditorProperty("Paste Offset", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.General, ConfigEntries.PrefabOffset, "If enabled, when objects are pasted they will be pasted at an offset. Otherwise, the objects will be pasted at the earliest objects start time."),

			//Timeline
			new EditorProperty("Dragging Timeline Cursor Pauses Level", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Timeline, ConfigEntries.DraggingTimelineSliderPauses, "Allows the timeline cursor to scrub across the timeline without pausing."),
			new EditorProperty("Timeline Cursor Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Timeline, ConfigEntries.MTSliderCol, "Color of the main timeline cursor."),
			new EditorProperty("Object Cursor Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Timeline, ConfigEntries.KTSliderCol, "Color of the object timeline cursor."),
			new EditorProperty("Object Selection Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Timeline, ConfigEntries.ObjSelCol, "Color of selected objects."),
			new EditorProperty("Timeline Zoom Bounds", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.Timeline, ConfigEntries.ETLZoomBounds, "The limits of the main timeline zoom."),
			new EditorProperty("Object Zoom Bounds", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.Timeline, ConfigEntries.ObjZoomBounds, "The limits of the object timeline zoom."),
			new EditorProperty("Zoom Amount", EditorProperty.ValueType.FloatSlider, EditorProperty.EditorPropCategory.Timeline, ConfigEntries.ZoomAmount, "Sets the zoom amount."),
			new EditorProperty("Waveform Generate", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Timeline, ConfigEntries.GenerateWaveform, "If the waveform on the main timeline should generate at all. If disabled, level loading times will be decreased by a bit."),
			new EditorProperty("Waveform Re-render", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Timeline, ConfigEntries.RenderTimeline, "If making any changes to the waveform should re-render it."),
			new EditorProperty("Waveform Mode", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.Timeline, ConfigEntries.WaveformMode, "Whether the waveform should be Legacy style or old style. Old style was originally used but at some point was replaced with the Legacy version."),
			new EditorProperty("Waveform BG Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Timeline, ConfigEntries.TimelineBGColor, "Color of the background for the waveform."),
			new EditorProperty("Waveform Top Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Timeline, ConfigEntries.TimelineTopColor, "If waveform mode is Legacy, this will be the top color. Otherwise, it will be the base color."),
			new EditorProperty("Waveform Bottom Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Timeline, ConfigEntries.TimelineBottomColor, "If waveform is Legacy, this will be the bottom color. Otherwise, it will be unused."),

			//Data
			new EditorProperty("Autosave Limit", EditorProperty.ValueType.Int, EditorProperty.EditorPropCategory.Data, ConfigEntries.AutoSaveLimit, "How many autosave files you want to have until it starts removing older autosaves."),
			new EditorProperty("Autosave Repeat", EditorProperty.ValueType.Float, EditorProperty.EditorPropCategory.Data, ConfigEntries.AutoSaveRepeat, "The time between each autosave."),
			new EditorProperty("Saving Updates Edited Date", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Data, ConfigEntries.SavingUpdatesTime, "If you want the date edited to be the current date you save on, enable this. Can be good for keeping organized with what levels you did recently."),
			new EditorProperty("Level Loads Last Time", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Data, ConfigEntries.IfEditorStartTime, "When loading into a level, it will open at the last place you left it at."),
			new EditorProperty("Level Pauses on Start", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Data, ConfigEntries.IfEditorPauses, "Goes with the last setting, if you want the editor to just not play when you load into a level you can enable this."),
			new EditorProperty("Level Loads Individual", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Data, ConfigEntries.IfEditorSlowLoads, "Objects will load individually, potentially allowing for better load times but some objects might still be loading in hours later. ONLY USE THIS IF YOU'RE SURE!"),

			//Editor GUI
			new EditorProperty("Drag UI", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.DragUI, "Specific UI elements can now be dragged around."),
			new EditorProperty("Hover UI Sound", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.HoverSoundsEnabled, "If the sound when hovering over a UI element should play."),
			new EditorProperty("Notification Width", EditorProperty.ValueType.Float, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.NotificationWidth, "Controls the width of the notifications"),
			new EditorProperty("Notification Size", EditorProperty.ValueType.Float, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.NotificationSize, "How big the notifications are."),
			new EditorProperty("Notification Direction", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.NotificationDirection, "Which direction the notifications should appear from."),

			new EditorProperty("Editor Color 1", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.EditorGUIColor1, "Editor GUI color 1"),
			new EditorProperty("Editor Color 2", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.EditorGUIColor2, "Editor GUI color 2"),
			new EditorProperty("Editor Color 3", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.EditorGUIColor3, "Editor GUI color 3"),
			new EditorProperty("Editor Color 4", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.EditorGUIColor4, "Editor GUI color 4"),
			new EditorProperty("Editor Color 5", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.EditorGUIColor5, "Editor GUI color 5"),
			new EditorProperty("Editor Color 6", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.EditorGUIColor6, "Editor GUI color 6"),
			new EditorProperty("Editor Color 7", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.EditorGUIColor7, "Editor GUI color 7"),
			new EditorProperty("Editor Color 8", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.EditorGUIColor8, "Editor GUI color 8"),
			new EditorProperty("Editor Color 9", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.EditorGUIColor9, "Editor GUI color 9"),

			new EditorProperty("Open File Origin", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.ORLAnchoredPos, ""),
			new EditorProperty("Open File Scale", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.ORLSizeDelta, ""),
			new EditorProperty("Open File Path Pos", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.ORLPathPos, ""),
			new EditorProperty("Open File Path Length", EditorProperty.ValueType.Float, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.ORLPathLength, ""),
			new EditorProperty("Open File Refresh Pos", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.ORLRefreshPos, ""),
			new EditorProperty("Open File Toggle Pos", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.ORLTogglePos, ""),
			new EditorProperty("Open File Dropdown Pos", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.ORLDropdownPos, ""),

			new EditorProperty("Open File Cell Size", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OGLVLCellSize, ""),
			new EditorProperty("Open File Constraint", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OGLVLConstraint, ""),
			new EditorProperty("Open File Const. Count", EditorProperty.ValueType.Int, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OGLVLConstraintCount, ""),
			new EditorProperty("Open File Spacing", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OGLVLSpacing, ""),

			new EditorProperty("Open File HWrap", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.FButtonHWrap, ""),
			new EditorProperty("Open File VWrap", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.FButtonVWrap, ""),
			new EditorProperty("Open File Text Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.FButtonTextColor, ""),
			new EditorProperty("Open File Text Invert", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.FButtonTextInvert, ""),
			new EditorProperty("Open File Font Size", EditorProperty.ValueType.Int, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.FButtonFontSize, ""),
			new EditorProperty("Open File Folder Clamp", EditorProperty.ValueType.Int, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.FButtonFoldClamp, ""),
			new EditorProperty("Open File Song Clamp", EditorProperty.ValueType.Int, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.FButtonSongClamp, ""),
			new EditorProperty("Open File Artist Clamp", EditorProperty.ValueType.Int, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.FButtonArtiClamp, ""),
			new EditorProperty("Open File Creator Clamp", EditorProperty.ValueType.Int, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.FButtonCreaClamp, ""),
			new EditorProperty("Open File Desc Clamp", EditorProperty.ValueType.Int, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.FButtonDescClamp, ""),
			new EditorProperty("Open File Date Clamp", EditorProperty.ValueType.Int, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.FButtonDateClamp, ""),
			new EditorProperty("Open File Format", EditorProperty.ValueType.String, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.FButtonFormat, ""),

			new EditorProperty("Open File Difficulty Color", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.FButtonDifColor, ""),
			new EditorProperty("Open File Difficulty Color X", EditorProperty.ValueType.Float, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.FButtonDifColorMult, ""),
			new EditorProperty("Open File Normal Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.FButtonNColor, ""),
			new EditorProperty("Open File Highlighted Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.FButtonHColor, ""),
			new EditorProperty("Open File Pressed Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.FButtonPColor, ""),
			new EditorProperty("Open File Selected Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.FButtonSColor, ""),
			new EditorProperty("Open File Color Fade Time", EditorProperty.ValueType.Float, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.FButtonFadeDColor, ""),

			new EditorProperty("Open File Cover Art Pos", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.FBIconPos, ""),
			new EditorProperty("Open File Cover Art Sca", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.FBIconSca, ""),

			new EditorProperty("Open File Hover Size", EditorProperty.ValueType.FloatSlider, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.HoverUIOFPSize, ""),
			new EditorProperty("Timeline Object Hover Size", EditorProperty.ValueType.FloatSlider, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.HoverUIETLSize, ""),
			new EditorProperty("Object Keyframe Hover Size", EditorProperty.ValueType.FloatSlider, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.HoverUIKFSize, ""),

			new EditorProperty("Anim Edit Prop Easing Open", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.EPPAnimateEaseIn, ""),
			new EditorProperty("Anim Edit Prop Easing Close", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.EPPAnimateEaseOut, ""),
			new EditorProperty("Anim Edit Prop Animate X", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.EPPAnimateX, ""),
			new EditorProperty("Anim Edit Prop Animate Y", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.EPPAnimateY, ""),
			new EditorProperty("Anim Edit Prop Speeds", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.EPPAnimateInOutSpeeds, ""),

			new EditorProperty("Anim Open File Easing Open", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OFPAnimateEaseIn, ""),
			new EditorProperty("Anim Open File Easing Close", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OFPAnimateEaseOut, ""),
			new EditorProperty("Anim Open File Animate X", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OFPAnimateX, ""),
			new EditorProperty("Anim Open File Animate Y", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OFPAnimateY, ""),
			new EditorProperty("Anim Open File Speeds", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OFPAnimateInOutSpeeds, ""),

			new EditorProperty("Anim New Level Easing Open", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.NFPAnimateEaseIn, ""),
			new EditorProperty("Anim New Level Easing Close", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.NFPAnimateEaseOut, ""),
			new EditorProperty("Anim New Level Animate X", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.NFPAnimateX, ""),
			new EditorProperty("Anim New Level Animate Y", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.NFPAnimateY, ""),
			new EditorProperty("Anim New Level Speeds", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.NFPAnimateInOutSpeeds, ""),

			new EditorProperty("Anim Prefabs Easing Open", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.PPAnimateEaseIn, ""),
			new EditorProperty("Anim Prefabs Easing Close", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.PPAnimateEaseOut, ""),
			new EditorProperty("Anim Prefabs Animate X", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.PPAnimateX, ""),
			new EditorProperty("Anim Prefabs Animate Y", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.PPAnimateY, ""),
			new EditorProperty("Anim Prefabs Speeds", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.PPAnimateInOutSpeeds, ""),

			new EditorProperty("Anim Object Tags Easing Open", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.QAPAnimateEaseIn, ""),
			new EditorProperty("Anim Object Tags Easing Close", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.QAPAnimateEaseOut, ""),
			new EditorProperty("Anim Object Tags Animate X", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.QAPAnimateX, ""),
			new EditorProperty("Anim Object Tags Animate Y", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.QAPAnimateY, ""),
			new EditorProperty("Anim Object Tags Speeds", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.QAPAnimateInOutSpeeds, ""),

			new EditorProperty("Anim Create Object Easing Open", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OBJPAnimateEaseIn, ""),
			new EditorProperty("Anim Create Object Easing Close", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OBJPAnimateEaseOut, ""),
			new EditorProperty("Anim Create Object Animate X", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OBJPAnimateX, ""),
			new EditorProperty("Anim Create Object Animate Y", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OBJPAnimateY, ""),
			new EditorProperty("Anim Create Object Speeds", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OBJPAnimateInOutSpeeds, ""),

			new EditorProperty("Anim Create BG Easing Open", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.BGPAnimateEaseIn, ""),
			new EditorProperty("Anim Create BG Easing Close", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.BGPAnimateEaseOut, ""),
			new EditorProperty("Anim Create BG Animate X", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.BGPAnimateX, ""),
			new EditorProperty("Anim Create BG Animate Y", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.BGPAnimateY, ""),
			new EditorProperty("Anim Create BG Speeds", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.BGPAnimateInOutSpeeds, ""),

			new EditorProperty("Anim Object Edit Easing Open", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.GODAnimateEaseIn, ""),
			new EditorProperty("Anim Object Edit Easing Close", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.GODAnimateEaseOut, ""),
			new EditorProperty("Anim Object Edit Animate X", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.GODAnimateX, ""),
			new EditorProperty("Anim Object Edit Animate Y", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.GODAnimateY, ""),
			new EditorProperty("Anim Object Edit Speeds", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.GODAnimateInOutSpeeds, ""),

			//Markers
			new EditorProperty("Marker Looping Active", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Markers, ConfigEntries.MarkerLoop, "If markers should loop from set end marker to set start marker."),
			new EditorProperty("Marker Looping Start", EditorProperty.ValueType.Int, EditorProperty.EditorPropCategory.Markers, ConfigEntries.MarkerStartIndex, ""),
			new EditorProperty("Marker Looping End", EditorProperty.ValueType.Int, EditorProperty.EditorPropCategory.Markers, ConfigEntries.MarkerEndIndex, ""),
			new EditorProperty("Markers Color 1", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Markers, ConfigEntries.MarkerColN0, ""),
			new EditorProperty("Markers Color 2", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Markers, ConfigEntries.MarkerColN1, ""),
			new EditorProperty("Markers Color 3", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Markers, ConfigEntries.MarkerColN2, ""),
			new EditorProperty("Markers Color 4", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Markers, ConfigEntries.MarkerColN3, ""),
			new EditorProperty("Markers Color 5", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Markers, ConfigEntries.MarkerColN4, ""),
			new EditorProperty("Markers Color 6", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Markers, ConfigEntries.MarkerColN5, ""),
			new EditorProperty("Markers Color 7", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Markers, ConfigEntries.MarkerColN6, ""),
			new EditorProperty("Markers Color 8", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Markers, ConfigEntries.MarkerColN7, ""),
			new EditorProperty("Markers Color 9", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Markers, ConfigEntries.MarkerColN8, ""),

			//Fields
			new EditorProperty("Time Modify", EditorProperty.ValueType.FloatSlider, EditorProperty.EditorPropCategory.Fields, ConfigEntries.TimeModify, "The amount scrolling on the time input field increases by."),
			new EditorProperty("Origin Offset X Modify", EditorProperty.ValueType.FloatSlider, EditorProperty.EditorPropCategory.Fields, ConfigEntries.OriginXAmount, "The amount scrolling on the Origin X Offset input field increases by."),
			new EditorProperty("Origin Offset Y Modify", EditorProperty.ValueType.FloatSlider, EditorProperty.EditorPropCategory.Fields, ConfigEntries.OriginYAmount, "The amount scrolling on the Origin Y Offset input field increases by."),
			new EditorProperty("Depth Slider Normal Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.DepthNormalColor, ""),
			new EditorProperty("Depth Slider Pressed Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.DepthPressedColor, ""),
			new EditorProperty("Depth Slider Highlighted Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.DepthHighlightedColor, ""),
			new EditorProperty("Depth Slider Disabled Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.DepthDisabledColor, ""),
			new EditorProperty("Depth Slider Fade Duration", EditorProperty.ValueType.Float, EditorProperty.EditorPropCategory.Fields, ConfigEntries.DepthFadeDuration, ""),
			new EditorProperty("Depth Slider Interactable", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Fields, ConfigEntries.DepthInteractable, ""),
			new EditorProperty("Depth Slider Updates", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Fields, ConfigEntries.DepthUpdate, ""),
			new EditorProperty("Depth Slider Max", EditorProperty.ValueType.Int, EditorProperty.EditorPropCategory.Fields, ConfigEntries.SliderRMax, ""),
			new EditorProperty("Depth Slider Min", EditorProperty.ValueType.Int, EditorProperty.EditorPropCategory.Fields, ConfigEntries.SliderRMin, ""),
			new EditorProperty("Depth Slider Direction", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.Fields, ConfigEntries.SliderDDirection, ""),
			new EditorProperty("Depth Modify", EditorProperty.ValueType.IntSlider, EditorProperty.EditorPropCategory.Fields, ConfigEntries.DepthAmount, "The amount scrolling on the Depth input field increases by."),

			new EditorProperty("Quick Prefab Create Active 1", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PQCActive0, ""),
			new EditorProperty("Quick Prefab Create Index 1", EditorProperty.ValueType.Int, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PQCIndex0, ""),
			new EditorProperty("Quick Prefab Create Key 1", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PQCKey0, ""),
			new EditorProperty("Quick Prefab Create Active 2", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PQCActive1, ""),
			new EditorProperty("Quick Prefab Create Index 2", EditorProperty.ValueType.Int, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PQCIndex1, ""),
			new EditorProperty("Quick Prefab Create Key 2", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PQCKey1, ""),
			new EditorProperty("Quick Prefab Create Active 3", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PQCActive2, ""),
			new EditorProperty("Quick Prefab Create Index 3", EditorProperty.ValueType.Int, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PQCIndex2, ""),
			new EditorProperty("Quick Prefab Create Key 3", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PQCKey2, ""),
			new EditorProperty("Quick Prefab Create Active 4", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PQCActive3, ""),
			new EditorProperty("Quick Prefab Create Index 4", EditorProperty.ValueType.Int, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PQCIndex3, ""),
			new EditorProperty("Quick Prefab Create Key 4", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PQCKey3, ""),
			new EditorProperty("Quick Prefab Create Active 5", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PQCActive4, ""),
			new EditorProperty("Quick Prefab Create Index 5", EditorProperty.ValueType.Int, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PQCIndex4, ""),
			new EditorProperty("Quick Prefab Create Key 5", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PQCKey4, ""),

			new EditorProperty("Theme Reloads After Drag", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Fields, ConfigEntries.ReloadThemesAfterDrag, ""),

			new EditorProperty("Theme Template Name", EditorProperty.ValueType.String, EditorProperty.EditorPropCategory.Fields, ConfigEntries.TemplateThemeName, ""),
			new EditorProperty("Theme Template GUI Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.TemplateThemeGUIColor, ""),
			new EditorProperty("Theme Template BG Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.TemplateThemeBGColor, ""),
			new EditorProperty("Theme Template P1 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.TemplateThemePlayerColor1, ""),
			new EditorProperty("Theme Template P2 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.TemplateThemePlayerColor2, ""),
			new EditorProperty("Theme Template P3 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.TemplateThemePlayerColor3, ""),
			new EditorProperty("Theme Template P4 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.TemplateThemePlayerColor4, ""),
			new EditorProperty("Theme Template OBJ1 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.TemplateThemeOBJColor1, ""),
			new EditorProperty("Theme Template OBJ2 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.TemplateThemeOBJColor2, ""),
			new EditorProperty("Theme Template OBJ3 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.TemplateThemeOBJColor3, ""),
			new EditorProperty("Theme Template OBJ4 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.TemplateThemeOBJColor4, ""),
			new EditorProperty("Theme Template OBJ5 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.TemplateThemeOBJColor5, ""),
			new EditorProperty("Theme Template OBJ6 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.TemplateThemeOBJColor6, ""),
			new EditorProperty("Theme Template OBJ7 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.TemplateThemeOBJColor7, ""),
			new EditorProperty("Theme Template OBJ8 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.TemplateThemeOBJColor8, ""),
			new EditorProperty("Theme Template OBJ9 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.TemplateThemeOBJColor9, ""),
			new EditorProperty("Theme Template BG1 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.TemplateThemeBGColor1, ""),
			new EditorProperty("Theme Template BG2 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.TemplateThemeBGColor2, ""),
			new EditorProperty("Theme Template BG3 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.TemplateThemeBGColor3, ""),
			new EditorProperty("Theme Template BG4 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.TemplateThemeBGColor4, ""),
			new EditorProperty("Theme Template BG5 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.TemplateThemeBGColor5, ""),
			new EditorProperty("Theme Template BG6 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.TemplateThemeBGColor6, ""),
			new EditorProperty("Theme Template BG7 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.TemplateThemeBGColor7, ""),
			new EditorProperty("Theme Template BG8 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.TemplateThemeBGColor8, ""),
			new EditorProperty("Theme Template BG9 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.TemplateThemeBGColor9, ""),

			new EditorProperty("Prefab Type 1 Name", EditorProperty.ValueType.String, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT0N, ""),
			new EditorProperty("Prefab Type 1 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT0C, ""),
			new EditorProperty("Prefab Type 2 Name", EditorProperty.ValueType.String, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT1N, ""),
			new EditorProperty("Prefab Type 2 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT1C, ""),
			new EditorProperty("Prefab Type 3 Name", EditorProperty.ValueType.String, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT2N, ""),
			new EditorProperty("Prefab Type 3 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT2C, ""),
			new EditorProperty("Prefab Type 4 Name", EditorProperty.ValueType.String, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT3N, ""),
			new EditorProperty("Prefab Type 4 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT3C, ""),
			new EditorProperty("Prefab Type 5 Name", EditorProperty.ValueType.String, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT4N, ""),
			new EditorProperty("Prefab Type 5 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT4C, ""),
			new EditorProperty("Prefab Type 6 Name", EditorProperty.ValueType.String, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT5N, ""),
			new EditorProperty("Prefab Type 6 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT5C, ""),
			new EditorProperty("Prefab Type 7 Name", EditorProperty.ValueType.String, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT6N, ""),
			new EditorProperty("Prefab Type 7 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT6C, ""),
			new EditorProperty("Prefab Type 8 Name", EditorProperty.ValueType.String, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT7N, ""),
			new EditorProperty("Prefab Type 8 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT7C, ""),
			new EditorProperty("Prefab Type 9 Name", EditorProperty.ValueType.String, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT8N, ""),
			new EditorProperty("Prefab Type 9 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT8C, ""),
			new EditorProperty("Prefab Type 10 Name", EditorProperty.ValueType.String, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT9N, ""),
			new EditorProperty("Prefab Type 10 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT9C, ""),
			new EditorProperty("Prefab Type 11 Name", EditorProperty.ValueType.String, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT10N, ""),
			new EditorProperty("Prefab Type 11 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT10C, ""),
			new EditorProperty("Prefab Type 12 Name", EditorProperty.ValueType.String, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT11N, ""),
			new EditorProperty("Prefab Type 12 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT11C, ""),
			new EditorProperty("Prefab Type 13 Name", EditorProperty.ValueType.String, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT12N, ""),
			new EditorProperty("Prefab Type 13 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT12C, ""),
			new EditorProperty("Prefab Type 14 Name", EditorProperty.ValueType.String, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT13N, ""),
			new EditorProperty("Prefab Type 14 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT13C, ""),
			new EditorProperty("Prefab Type 15 Name", EditorProperty.ValueType.String, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT14N, ""),
			new EditorProperty("Prefab Type 15 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT14C, ""),
			new EditorProperty("Prefab Type 16 Name", EditorProperty.ValueType.String, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT15N, ""),
			new EditorProperty("Prefab Type 16 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT15C, ""),
			new EditorProperty("Prefab Type 17 Name", EditorProperty.ValueType.String, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT16N, ""),
			new EditorProperty("Prefab Type 17 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT16C, ""),
			new EditorProperty("Prefab Type 18 Name", EditorProperty.ValueType.String, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT17N, ""),
			new EditorProperty("Prefab Type 18 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT17C, ""),
			new EditorProperty("Prefab Type 19 Name", EditorProperty.ValueType.String, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT18N, ""),
			new EditorProperty("Prefab Type 19 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT18C, ""),
			new EditorProperty("Prefab Type 20 Name", EditorProperty.ValueType.String, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT19N, ""),
			new EditorProperty("Prefab Type 20 Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Fields, ConfigEntries.PT19C, ""),

			//Preview
			new EditorProperty("Show Object Dragger", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Preview, ConfigEntries.ShowSelector, "Allows objects's position and scale to directly be modified within the editor preview window."),
			new EditorProperty("Show Only On Layer", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Preview, ConfigEntries.ShowObjectsOnLayer, "Will make any objects not on the current editor layer transparent."),
			new EditorProperty("Not On Layer Opacity", EditorProperty.ValueType.FloatSlider, EditorProperty.EditorPropCategory.Preview, ConfigEntries.ShowObjectsAlpha, "How transparent the objects not on the current editor layer should be."),
			new EditorProperty("Show Empties", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Preview, ConfigEntries.ShowEmpties, "If empties should be shown. Good for understanding character rigs."),
			new EditorProperty("Show Only Damagable", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Preview, ConfigEntries.ShowDamagable, "Will only show objects that can hit you."),
			new EditorProperty("Empties Preview Fix", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Preview, ConfigEntries.PreviewSelectFix, "Empties will become unselectable in the editor preview window."),
			new EditorProperty("Highlight Objects", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Preview, ConfigEntries.HighlightObjects, "If hovering over an object should highlight it."),
			new EditorProperty("Highlight Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Preview, ConfigEntries.HighlightColor, "The color will set to this amount."),
			new EditorProperty("Highlight Color Shift", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Preview, ConfigEntries.HighlightDoubleColor, "When holding shift, the color will set to this amount."),
		};

		public static EditorProperty.EditorPropCategory currentCategory = EditorProperty.EditorPropCategory.General;

		public class EditorProperty
        {
			public EditorProperty()
            {
            }

			public EditorProperty(string _name, ValueType _valueType, EditorPropCategory _editorProp, ConfigEntryBase _configEntry, string _description)
            {
				name = _name;
				valueType = _valueType;
				propCategory = _editorProp;
				configEntry = _configEntry;
				description = _description;
			}

			public string name;
			public ValueType valueType;
			public EditorPropCategory propCategory;
			public ConfigEntryBase configEntry;
			public string description;

			public enum ValueType
            {
				Bool,
				Int,
				Float,
				IntSlider,
				FloatSlider,
				String,
				Vector2,
				Vector3,
				Enum,
				Color
            }

			public enum EditorPropCategory
			{
				//New
				General, //Includes Remidner
				Timeline, //Includes ZoomBounds
				Data, //Includes AutoSaving
				EditorGUI, //Includes EditorNotifications, OpenFilePopup settings, AnimateGUI
				Markers,
				Fields, //Includes TimelineBar, RenderDepthUnlimited scroll amount, EventsPlus increase / decrease amount, etc
				Preview

				//-----Old-----
				//AnimateGUI,
				//AutoSave,
				//EditorGUI,
				//EditorNotifications,
				//GeneralEditor,
				//Markers,
				//OpenFilePopupBase,
				//OpenFilePopupButtons,
				//OpenFilePopupCells,
				//Preview,
				//Reminder,
				//Saving,
				//Timeline,
				//TimelineBar,
				//ZoomBounds
			}
		}
	}
}
