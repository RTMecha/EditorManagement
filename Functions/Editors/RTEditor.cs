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

using EditorManagement.Functions.Components;
using EditorManagement.Functions.Tools;
using EditorManagement.Patchers;

using MP3Sharp;
using Crosstales.FB;
using CielaSpike;

using RTFunctions.Functions;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;
using RTFunctions.Functions.Optimization;

using BeatmapObject = DataManager.GameData.BeatmapObject;
using ObjectType = DataManager.GameData.BeatmapObject.ObjectType;

namespace EditorManagement.Functions.Editors
{
    public class RTEditor : MonoBehaviour
    {
        #region Variables

        public static RTEditor inst;

		public static bool ienumRunning;

		public static List<string> notifications = new List<string>();

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

		public static BeatmapObject mirrorObject;
		public static Vector2 preMouse = new Vector2(Input.mousePosition.x, Input.mousePosition.y);

		public static string searchterm = "";

		public static bool loadingThemes = false;

		public static Image timelineSliderHandle;

		public static Image timelineSliderRuler;

		public static Image keyframeTimelineSliderHandle;
		public static Image keyframeTimelineSliderRuler;
		public static GameObject timelineBar;

		public static InputField layersIF;
		public static InputField pitchIF;
		public static InputField timeIF;

		public static GameObject defaultIF;

		public bool parentPickerEnabled = false;

		public GameObject mousePicker;
		RectTransform mousePickerRT;

		public bool selectingKey = false;
		public object keyToSet;

		#endregion

		void Awake()
        {
            if (inst == null)
            {
                inst = this;
            }
            if (inst != this)
            {
                Destroy(gameObject);
            }

			mousePicker = new GameObject("picker");
			mousePicker.transform.SetParent(EditorManager.inst.GetDialog("Parent Selector").Dialog.parent.parent);
			mousePicker.transform.localScale = Vector3.one;
			mousePicker.layer = 5;
			mousePickerRT = mousePicker.AddComponent<RectTransform>();

			var img = new GameObject("image");
			img.transform.SetParent(mousePicker.transform);
			img.transform.localScale = Vector3.one;
			img.layer = 5;

			var imgRT = img.AddComponent<RectTransform>();
			imgRT.anchoredPosition = new Vector2(-930f, -520f);
			imgRT.sizeDelta = new Vector2(32f, 32f);

			var image = img.AddComponent<Image>();

			UIManager.GetImage(image, RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_dropper.png");
        }

		void Update()
		{
			if (EditorManager.inst.isEditing)
			{
				if (Input.GetKeyDown(ConfigEntries.EditorPropertiesKey.Value))
				{
					OpenPropertiesWindow(true);
				}
			}

			if (ObjEditor.inst.currentObjectSelection != null && ObjEditor.inst.currentObjectSelection.Index != -1 && ObjEditor.inst.currentObjectSelection.IsObject() && ObjEditor.inst.currentObjectSelection.GetObjectData() != null && !string.IsNullOrEmpty(ObjEditor.inst.currentObjectSelection.GetObjectData().text))
			{
				var currentObjectSelection = ObjEditor.inst.currentObjectSelection.GetObjectData();
				if (Input.GetKeyDown(KeyCode.Alpha1))
				{
					Debug.LogFormat("{0}Text Object A1Z26 Encrypt: {1}", EditorPlugin.className, RTHelpers.AlphabetA1Z26Encrypt(currentObjectSelection.text));
				}
				if (Input.GetKeyDown(KeyCode.Alpha2))
				{
					Debug.LogFormat("{0}Text Object Caesar Encrypt: {1}", EditorPlugin.className, RTHelpers.AlphabetCaesarEncrypt(currentObjectSelection.text));
				}
				if (Input.GetKeyDown(KeyCode.Alpha3))
				{
					Debug.LogFormat("{0}Text Object Atbash Encrypt: {1}", EditorPlugin.className, RTHelpers.AlphabetAtbashEncrypt(currentObjectSelection.text));
				}
				if (Input.GetKeyDown(KeyCode.Alpha4))
				{
					Debug.LogFormat("{0}Text Object Kevin Encrypt: {1}", EditorPlugin.className, RTHelpers.AlphabetKevinEncrypt(currentObjectSelection.text));
				}
			}

			if (Input.GetMouseButtonDown(1))
				parentPickerEnabled = false;

			if (mousePicker != null)
				mousePicker.SetActive(parentPickerEnabled);

			if (mousePicker != null && mousePickerRT != null && parentPickerEnabled)
            {
				float num = (float)Screen.width / 1920f;
				num = 1f / num;
				float x = mousePickerRT.sizeDelta.x;
				float y = mousePickerRT.sizeDelta.y;
				Vector3 zero = Vector3.zero;
				//if ((Input.mousePosition.x + x + 32f) * num >= 1920f)
				//{
				//	zero.x -= x;
				//}
				//if ((Input.mousePosition.y + y + 32f) * num >= 1080f)
				//{
				//	zero.y -= y;
				//}
				mousePickerRT.anchoredPosition = (Input.mousePosition + zero) * num;
			}

			if (selectingKey && keyToSet != null && keyToSet.GetType() == typeof(ConfigEntry<KeyCode>))
            {
				var configEntry = (ConfigEntry<KeyCode>)keyToSet;
				
				for (int i = 0; i < 345; i++)
                {
					if (Input.GetKeyDown((KeyCode)i))
                    {
						configEntry.Value = (KeyCode)i;
						selectingKey = false;
                    }
                }
            }
		}

        #region Notifications
		
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

        #endregion

        #region Objects

        public static void CreateNewNormalObject(bool _select = true, bool setHistory = true)
		{
			ObjEditor.ObjectSelection tmpSelection = CreateNewDefaultObject(_select);

			ObjectManager.inst.updateObjects(tmpSelection);
			ObjEditor.inst.RenderTimelineObject(tmpSelection);

			if (setHistory)
			{
				EditorManager.inst.history.Add(new History.Command("Create New Normal Object", delegate ()
				{
					CreateNewNormalObject(_select, false);
				}, delegate ()
				{
					inst.StartCoroutine(DeleteObject(tmpSelection));
				}), false);
			}
		}

		public static void CreateNewCircleObject(bool _select = true, bool setHistory = true)
		{
			ObjEditor.ObjectSelection tmpSelection = CreateNewDefaultObject(_select);

			if (tmpSelection.GetObjectData() != null)
			{
				var beatmapObject = tmpSelection.GetObjectData();
				beatmapObject.shape = 1;
				beatmapObject.shapeOption = 0;
				beatmapObject.name = "circle";
			}

			ObjectManager.inst.updateObjects(tmpSelection);
			ObjEditor.inst.RenderTimelineObject(tmpSelection);

			if (setHistory)
			{
				EditorManager.inst.history.Add(new History.Command("Create New Normal Circle Object", delegate ()
				{
					CreateNewCircleObject(_select, false);
				}, delegate ()
				{
					inst.StartCoroutine(DeleteObject(tmpSelection));
				}), false);
			}
		}

		public static void CreateNewTriangleObject(bool _select = true, bool setHistory = true)
		{
			ObjEditor.ObjectSelection tmpSelection = CreateNewDefaultObject(_select);

			if (tmpSelection.GetObjectData() != null)
			{
				var beatmapObject = tmpSelection.GetObjectData();
				beatmapObject.shape = 2;
				beatmapObject.shapeOption = 0;
				beatmapObject.name = "triangle";
			}

			ObjectManager.inst.updateObjects(tmpSelection);
			ObjEditor.inst.RenderTimelineObject(tmpSelection);

			if (setHistory)
			{
				EditorManager.inst.history.Add(new History.Command("Create New Normal Triangle Object", delegate ()
				{
					CreateNewTriangleObject(_select, false);
				}, delegate ()
				{
					inst.StartCoroutine(DeleteObject(tmpSelection));
				}), false);
			}
		}

		public static void CreateNewTextObject(bool _select = true, bool setHistory = true)
		{
			var tmpSelection = CreateNewDefaultObject(_select);

			if (tmpSelection.GetObjectData() != null)
			{
				var beatmapObject = tmpSelection.GetObjectData();
				beatmapObject.shape = 4;
				beatmapObject.shapeOption = 0;
				beatmapObject.text = "text";
				beatmapObject.name = "text";
				beatmapObject.objectType = ObjectType.Decoration;
			}

			ObjectManager.inst.updateObjects(tmpSelection);
			ObjEditor.inst.RenderTimelineObject(tmpSelection);

			if (setHistory)
            {
				EditorManager.inst.history.Add(new History.Command("Create New Normal Text Object", delegate ()
				{
					CreateNewTextObject(_select, false);
				}, delegate ()
				{
					inst.StartCoroutine(DeleteObject(tmpSelection));
				}), false);
			}
		}

		public static void CreateNewHexagonObject(bool _select = true, bool setHistory = true)
		{
			ObjEditor.ObjectSelection tmpSelection = CreateNewDefaultObject(_select);

			if (tmpSelection.GetObjectData() != null)
			{
				var beatmapObject = tmpSelection.GetObjectData();
				beatmapObject.shape = 5;
				beatmapObject.shapeOption = 0;
				beatmapObject.name = "hexagon";
			}

			ObjectManager.inst.updateObjects(tmpSelection);
			ObjEditor.inst.RenderTimelineObject(tmpSelection);

			if (setHistory)
			{
				EditorManager.inst.history.Add(new History.Command("Create New Normal Hexagon Object", delegate ()
				{
					CreateNewHexagonObject(_select, false);
				}, delegate ()
				{
					inst.StartCoroutine(DeleteObject(tmpSelection));
				}), false);
			}
		}

		public static void CreateNewHelperObject(bool _select = true, bool setHistory = true)
		{
			ObjEditor.ObjectSelection tmpSelection = CreateNewDefaultObject(_select);

			if (tmpSelection.GetObjectData() != null)
			{
				var beatmapObject = tmpSelection.GetObjectData();
				beatmapObject.name = "helper";
				beatmapObject.objectType = ObjectType.Helper;
			}

			ObjectManager.inst.updateObjects(tmpSelection);
			ObjEditor.inst.RenderTimelineObject(tmpSelection);

			if (setHistory)
			{
				EditorManager.inst.history.Add(new History.Command("Create New Helper Object", delegate ()
				{
					CreateNewHelperObject(_select, false);
				}, delegate ()
				{
					inst.StartCoroutine(DeleteObject(tmpSelection));
				}), false);
			}
		}

		public static void CreateNewDecorationObject(bool _select = true, bool setHistory = true)
		{
			ObjEditor.ObjectSelection tmpSelection = CreateNewDefaultObject(_select);

			if (tmpSelection.GetObjectData() != null)
			{
				var beatmapObject = tmpSelection.GetObjectData();
				beatmapObject.name = "decoration";
				beatmapObject.objectType = ObjectType.Decoration;
			}

			ObjectManager.inst.updateObjects(tmpSelection);
			ObjEditor.inst.RenderTimelineObject(tmpSelection);

			if (setHistory)
			{
				EditorManager.inst.history.Add(new History.Command("Create New Decoration Object", delegate ()
				{
					CreateNewDecorationObject(_select, false);
				}, delegate ()
				{
					inst.StartCoroutine(DeleteObject(tmpSelection));
				}), false);
			}
		}

		public static void CreateNewEmptyObject(bool _select = true, bool setHistory = true)
		{
			ObjEditor.ObjectSelection tmpSelection = CreateNewDefaultObject(_select);

			if (tmpSelection.GetObjectData() != null)
			{
				var beatmapObject = tmpSelection.GetObjectData();
				beatmapObject.name = "empty";
				beatmapObject.objectType = ObjectType.Empty;
			}

			ObjectManager.inst.updateObjects(tmpSelection);
			ObjEditor.inst.RenderTimelineObject(tmpSelection);

			if (setHistory)
			{
				EditorManager.inst.history.Add(new History.Command("Create New Empty Object", delegate ()
				{
					CreateNewEmptyObject(_select, false);
				}, delegate ()
				{
					inst.StartCoroutine(DeleteObject(tmpSelection));
				}), false);
			}
		}

		public static void CreateNewNoAutokillObject(bool _select = true, bool setHistory = true)
		{
			ObjEditor.ObjectSelection tmpSelection = CreateNewDefaultObject(_select);

			if (tmpSelection.GetObjectData() != null)
			{
				var beatmapObject = tmpSelection.GetObjectData();
				beatmapObject.name = "no autokill";
				beatmapObject.autoKillType = BeatmapObject.AutoKillType.OldStyleNoAutokill;
			}

			ObjectManager.inst.updateObjects(tmpSelection);
			ObjEditor.inst.RenderTimelineObject(tmpSelection);

			if (setHistory)
			{
				EditorManager.inst.history.Add(new History.Command("Create New No Autokill Object", delegate ()
				{
					CreateNewNoAutokillObject(_select, false);
				}, delegate ()
				{
					inst.StartCoroutine(DeleteObject(tmpSelection));
				}), false);
			}
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
			list[3].Add(new DataManager.GameData.EventKeyframe(0f, new float[5], new float[0], 0));
            BeatmapObject beatmapObject = new BeatmapObject(true, AudioManager.inst.CurrentAudioSource.time, "", 0, "", list);
			beatmapObject.id = LSText.randomString(16);
			beatmapObject.autoKillType = BeatmapObject.AutoKillType.LastKeyframeOffset;
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
			int num = DataManager.inst.gameData.beatmapObjects.FindIndex((BeatmapObject x) => x.fromPrefab);
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
			ObjectManager.inst.updateObjects(objectSelection);
			AudioManager.inst.SetMusicTime(AudioManager.inst.CurrentAudioSource.time + 0.001f);
			if (_select)
			{
				ObjEditor.inst.SetCurrentObj(objectSelection);
			}

			if (ObjectModifiersEditor.inst != null)
            {
				ObjectModifiersEditor.AddModifierObject(beatmapObject);
            }

			return objectSelection;
		}

		public static BeatmapObject CreateNewBeatmapObject(float _time, bool _add = true)
		{
            BeatmapObject beatmapObject = new BeatmapObject();
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
				0f,
				0f,
				0f,
				0f,
				0f
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

				if (ObjectModifiersEditor.inst != null)
				{
					ObjectModifiersEditor.AddModifierObject(beatmapObject);
				}
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
                    BeatmapObject objData = _obj.GetObjectData();
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
					if (objData.objectType == ObjectType.Helper)
					{
						gameObject.GetComponent<Image>().sprite = ObjEditor.inst.HelperSprite;
						gameObject.GetComponent<Image>().type = Image.Type.Tiled;
					}
					else if (objData.objectType == ObjectType.Decoration)
					{
						gameObject.GetComponent<Image>().sprite = ObjEditor.inst.DecorationSprite;
						gameObject.GetComponent<Image>().type = Image.Type.Tiled;
					}
					else if (objData.objectType == ObjectType.Empty)
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

		public static GameObject CreateTimelineObject(ObjEditor __instance, ObjEditor.ObjectSelection _selection)
		{
			GameObject gameObject = null;
			if (_selection.IsObject() && !string.IsNullOrEmpty(_selection.ID) && _selection.Index != -1)
			{
				if (__instance.beatmapObjects.ContainsKey(_selection.ID))
				{
					Destroy(__instance.beatmapObjects[_selection.ID]);
				}
				var beatmapObject = _selection.GetObjectData();
				float startTime = beatmapObject.StartTime;

				gameObject = Instantiate(__instance.timelineObjectPrefab);
				gameObject.name = beatmapObject.name;
				gameObject.transform.SetParent(EditorManager.inst.timeline.transform);
				gameObject.transform.localScale = Vector3.one;
				gameObject.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "";

				if (__instance.beatmapObjects.ContainsKey(_selection.ID))
				{
					__instance.beatmapObjects[_selection.ID] = gameObject;
				}
				else
				{
					__instance.beatmapObjects.Add(_selection.ID, gameObject);
				}

				var objectSelection = new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Object, _selection.ID);
				//objectSelection.DebugLog();

				var createBeatmapObjectStartDragTrigger = (EventTrigger.Entry)__instance.GetType().GetMethod("CreateBeatmapObjectStartDragTrigger", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { EventTriggerType.BeginDrag, objectSelection });
				var createBeatmapObjectEndDragTrigger = (EventTrigger.Entry)__instance.GetType().GetMethod("CreateBeatmapObjectEndDragTrigger", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { EventTriggerType.EndDrag, objectSelection });

				Triggers.AddEventTrigger(gameObject, new List<EventTrigger.Entry> { Triggers.CreateBeatmapObjectTrigger(__instance, objectSelection), createBeatmapObjectStartDragTrigger, createBeatmapObjectEndDragTrigger });

				//gameObject.GetComponent<EventTrigger>().triggers.Add(Triggers.CreateBeatmapObjectTrigger(__instance, objectSelection));
				//gameObject.GetComponent<EventTrigger>().triggers.Add(createBeatmapObjectStartDragTrigger);
				//gameObject.GetComponent<EventTrigger>().triggers.Add(createBeatmapObjectEndDragTrigger);
			}
			else if (_selection.IsPrefab() && !string.IsNullOrEmpty(_selection.ID) && _selection.GetPrefabData() != null && _selection.Index != -1)
			{
				if (__instance.prefabObjects.ContainsKey(_selection.ID))
				{
					Destroy(__instance.prefabObjects[_selection.ID]);
				}
				float startTime2 = _selection.GetPrefabObjectData().StartTime;

				gameObject = Instantiate(__instance.timelineObjectPrefab);
				gameObject.name = _selection.GetPrefabData().Name;
				gameObject.transform.SetParent(EditorManager.inst.timeline.transform);
				gameObject.transform.localScale = Vector3.one;
				gameObject.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "";

				if (__instance.prefabObjects.ContainsKey(_selection.ID))
				{
					__instance.prefabObjects[_selection.ID] = gameObject;
				}
				else
				{
					__instance.prefabObjects.Add(_selection.ID, gameObject);
				}

				var obj = new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Prefab, _selection.ID);
				//_selection.DebugLog();

				var createBeatmapObjectStartDragTrigger = (EventTrigger.Entry)__instance.GetType().GetMethod("CreateBeatmapObjectStartDragTrigger", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { EventTriggerType.BeginDrag, obj });
				var createBeatmapObjectEndDragTrigger = (EventTrigger.Entry)__instance.GetType().GetMethod("CreateBeatmapObjectEndDragTrigger", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { EventTriggerType.EndDrag, obj });

				Triggers.AddEventTrigger(gameObject, new List<EventTrigger.Entry> { Triggers.CreateBeatmapObjectTrigger(__instance, obj), createBeatmapObjectStartDragTrigger, createBeatmapObjectEndDragTrigger });

				//gameObject.GetComponent<EventTrigger>().triggers.Add(Triggers.CreateBeatmapObjectTrigger(__instance, obj));
				//gameObject.GetComponent<EventTrigger>().triggers.Add(createBeatmapObjectStartDragTrigger);
				//gameObject.GetComponent<EventTrigger>().triggers.Add(createBeatmapObjectEndDragTrigger);
			}

			if (gameObject != null)
            {
				var hoverUI = gameObject.AddComponent<HoverUI>();
				hoverUI.animatePos = false;
				hoverUI.animateSca = true;
				hoverUI.size = ConfigEntries.TimelineObjectHoverSize.Value;
			}

			return gameObject;
		}

		public static void Duplicate(bool _regen = true) => Copy(false, true, _regen);

		public static void Cut() => Copy(true, false);

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
			Dictionary<string, Dictionary<string, object>> modifiers = null;

			if (RTFile.FileExists(Application.persistentDataPath + "/copied_objects.lsp"))
			{
				JSONNode jn = JSON.Parse(FileManager.inst.LoadJSONFileRaw(Application.persistentDataPath + "/copied_objects.lsp"));

				List<BeatmapObject> _objects = new List<BeatmapObject>();
				modifiers = new Dictionary<string, Dictionary<string, object>>();
				for (int aIndex = 0; aIndex < jn["objects"].Count; ++aIndex)
				{
					_objects.Add(BeatmapObject.ParseGameObject(jn["objects"][aIndex]));
					modifiers.Add(jn["objects"][aIndex]["id"], Parser.ParseModifier(jn["objects"][aIndex]));
				}

				List<DataManager.GameData.PrefabObject> _prefabObjects = new List<DataManager.GameData.PrefabObject>();
				for (int aIndex = 0; aIndex < jn["prefab_objects"].Count; ++aIndex)
					_prefabObjects.Add(ParsePrefabObject(jn["prefab_objects"][aIndex]));

				pr = new DataManager.GameData.Prefab(jn["name"], jn["type"].AsInt, jn["offset"].AsFloat, _objects, _prefabObjects);

				ObjEditor.inst.hasCopiedObject = true;
			}

			if (RTFile.FileExists(Application.persistentDataPath + "/copied_objects.lsp"))
			{
				JSONNode jn = JSON.Parse(FileManager.inst.LoadJSONFileRaw(Application.persistentDataPath + "/copied_objects.lsp"));
			}

			if (pr == null)
			{
				inst.StartCoroutine(AddPrefabExpandedToLevel(ObjEditor.inst.beatmapObjCopy, true, _offsetTime, false, _regen));
			}
			else
			{
				inst.StartCoroutine(AddPrefabExpandedToLevel(pr, true, _offsetTime, false, _regen, modifiers));
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

		public static DataManager.GameData.PrefabObject ParsePrefabObject(JSONNode jn)
		{
			var prefabObject = new DataManager.GameData.PrefabObject();
			prefabObject.ID = jn["id"];
			prefabObject.prefabID = jn["pid"];
			prefabObject.StartTime = jn["st"].AsFloat;

			if (!string.IsNullOrEmpty(jn["rc"]))
				prefabObject.RepeatCount = int.Parse(jn["rc"]);

			if (!string.IsNullOrEmpty(jn["ro"]))
				prefabObject.RepeatOffsetTime = float.Parse(jn["ro"]);

			if (jn["id"] != null)
			{
				prefabObject.ID = jn["id"];
			}
			else
			{
				prefabObject.ID = LSText.randomString(16);
			}
			if (jn["ed"]["locked"] != null)
			{
				prefabObject.editorData.locked = jn["ed"]["locked"].AsBool;
			}
			if (jn["ed"]["shrink"] != null)
			{
				prefabObject.editorData.collapse = jn["ed"]["shrink"].AsBool;
			}
			if (jn["ed"]["bin"] != null)
			{
				prefabObject.editorData.Bin = jn["ed"]["bin"].AsInt;
			}
			if (jn["ed"]["layer"] != null)
			{
				prefabObject.editorData.Layer = jn["ed"]["layer"].AsInt;
			}

			prefabObject.events.Clear();

			if (jn["e"][0]["pos"] != null)
			{
				DataManager.GameData.EventKeyframe kf = new DataManager.GameData.EventKeyframe();
				JSONNode jnpos = jn["e"][0]["pos"];

				var x = float.Parse(jnpos["x"]);
				var y = float.Parse(jnpos["y"]);

				Debug.LogFormat("{0}Pos Offset (X: {1}, Y: {2})", EditorPlugin.className, x, y);

				kf.SetEventValues(new float[]
				{
					x,
					y
				});
				kf.random = jnpos["r"].AsInt;
				kf.SetEventRandomValues(new float[]
				{
					jnpos["rx"].AsFloat,
					jnpos["ry"].AsFloat,
					jnpos["rz"].AsFloat
				});
				kf.active = false;
				prefabObject.events.Add(kf);
			}
			else
			{
				Debug.LogErrorFormat("{0}Failed to parse Prefab Pos Offset due to JSONNode being null: {1}", EditorPlugin.className, jn["e"][0]["pos"] == null);

				prefabObject.events.Add(new DataManager.GameData.EventKeyframe(new float[2] { 0f, 0f }, new float[2] { 0f, 0f }));
            }
			if (jn["e"][1]["sca"] != null)
			{
				DataManager.GameData.EventKeyframe kf = new DataManager.GameData.EventKeyframe();
				JSONNode jnsca = jn["e"][1]["sca"];
				kf.SetEventValues(new float[]
				{
					float.Parse(jnsca["x"]),
					float.Parse(jnsca["y"])
				});
				kf.random = jnsca["r"].AsInt;
				kf.SetEventRandomValues(new float[]
				{
					jnsca["rx"].AsFloat,
					jnsca["ry"].AsFloat,
					jnsca["rz"].AsFloat
				});
				kf.active = false;
				prefabObject.events.Add(kf);
			}
			else
			{
				prefabObject.events.Add(new DataManager.GameData.EventKeyframe(new float[2] { 1f, 1f }, new float[2] { 1f, 1f }));
			}
			if (jn["e"][2]["rot"] != null)
			{
				DataManager.GameData.EventKeyframe kf = new DataManager.GameData.EventKeyframe();
				JSONNode jnrot = jn["e"][2]["rot"];
				kf.SetEventValues(new float[]
				{
					float.Parse(jnrot["x"])
				});
				kf.random = jnrot["r"].AsInt;
				kf.SetEventRandomValues(new float[]
				{
					jnrot["rx"].AsFloat,
					0f,
					jnrot["rz"].AsFloat
				});
				kf.active = false;
				prefabObject.events.Add(kf);
			}
			else
			{
				prefabObject.events.Add(new DataManager.GameData.EventKeyframe(new float[1] { 0f }, new float[1] { 0f }));
			}
			return prefabObject;
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

		public static IEnumerator GroupSelectKeyframes(bool _add = true)
        {
			ienumRunning = true;

			float delay = 0f;
			bool flag = false;
			int num = 0;
			foreach (var list in ObjEditor.inst.timelineKeyframes)
			{
				int num2 = 0;
				foreach (var gameObject2 in list)
				{
					if (RTMath.RectTransformToScreenSpace(ObjEditor.inst.SelectionBoxImage.rectTransform).Overlaps(RTMath.RectTransformToScreenSpace(gameObject2.transform.GetChild(0).GetComponent<Image>().rectTransform)) && gameObject2.activeSelf)
					{
						yield return new WaitForSeconds(delay);
						if (!flag && !_add)
						{
							ObjEditor.inst.SetCurrentKeyframe(num, num2, false, false);
							flag = true;
						}
						else
						{
							ObjEditor.inst.SetCurrentKeyframe(num, num2, false, true);
						}
						delay += 0.0001f;
					}
					num2++;
				}
				num++;
			}

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
				return;
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
						Debug.LogFormat("{0}Deleting single object...", EditorPlugin.className);
						float startTime = 0f;
						if (ObjEditor.inst.currentObjectSelection.IsObject())
						{
							startTime = ObjEditor.inst.currentObjectSelection.GetObjectData().StartTime;
						}
						else
						{
							startTime = ObjEditor.inst.currentObjectSelection.GetPrefabObjectData().StartTime;
						}

						Debug.LogFormat("{0}Assigning prefab for undo...", EditorPlugin.className);
						DataManager.GameData.Prefab prefab = new DataManager.GameData.Prefab("deleted object", 0, startTime, ObjEditor.inst.selectedObjects);

						Debug.LogFormat("{0}Setting history...", EditorPlugin.className);
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

						var currentObjectSelection = ObjEditor.inst.currentObjectSelection;
						bool isObject = currentObjectSelection.IsObject();
						string objectName = "Object";
						if (isObject)
                        {
							objectName = currentObjectSelection.GetObjectData().name;
						}

						Debug.LogFormat("{0}Finally deleting object...", EditorPlugin.className);
						inst.StartCoroutine(DeleteObject(ObjEditor.inst.currentObjectSelection, true));

						if (isObject && !string.IsNullOrEmpty(objectName))
						{
							EditorManager.inst.DisplayNotification("Deleted Beatmap Object\n[ " + objectName + " ].", 1f, EditorManager.NotificationType.Success, false);
						}
						else
						{
							EditorManager.inst.DisplayNotification("Deleted Beatmap Object\n[ Object ].", 1f, EditorManager.NotificationType.Success, false);
						}

						Debug.LogFormat("{0}Done!", EditorPlugin.className);
					}
				}
				else
				{
					EditorManager.inst.DisplayNotification("Can't Delete Only Beatmap Object", 1f, EditorManager.NotificationType.Error, false);
				}
				return;
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

					var dictionary = new Dictionary<EventEditor.KeyframeSelection, DataManager.GameData.EventKeyframe>();

					float num = AudioManager.inst.CurrentAudioSource.time;
					foreach (var l in list2)
                    {
						if (DataManager.inst.gameData.eventObjects.allEvents[l.Type][l.Index].eventTime < num)
                        {
							num = DataManager.inst.gameData.eventObjects.allEvents[l.Type][l.Index].eventTime;
						}
						dictionary.Add(l, DataManager.inst.gameData.eventObjects.allEvents[l.Type][l.Index]);
                    }

					//EditorManager.inst.history.Add(new History.Command("Delete Event Keyframes", delegate ()
					//{
					//	List<EventEditor.KeyframeSelection> list3 = new List<EventEditor.KeyframeSelection>();
					//	foreach (var keyframeSelection in EventEditor.inst.keyframeSelections)
					//	{
					//		if (keyframeSelection.Index != 0)
					//		{
					//			list3.Add(keyframeSelection);
					//		}
					//		else
					//		{
					//			EditorManager.inst.DisplayNotification("Can't Delete First Event Keyframe.", 1f, EditorManager.NotificationType.Error, false);
					//		}
					//	}
					//	EditorManager.inst.ClearDialogs(new EditorManager.EditorDialog.DialogType[]
					//	{
					//	EditorManager.EditorDialog.DialogType.Event
					//	});
					//	list3 = (from x in list3
					//			 orderby x.Index descending
					//			 select x).ToList();
					//	inst.StartCoroutine(DeleteEvent(list3));
					//}, delegate ()
					//{
					//	PasteEventKeyframes(dictionary);
					//}));

					inst.StartCoroutine(DeleteEvent(list2));
					EventEditor.inst.SetCurrentEvent(0, 0);
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
				return;
			}
			if (EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Background))
			{
				BackgroundEditor.inst.DeleteBackground(BackgroundEditor.inst.currentObj);
				return;
			}
			if (EditorManager.inst.IsDialogActive(EditorManager.EditorDialog.DialogType.Checkpoint))
			{
				if (CheckpointEditor.inst.currentObj != 0)
				{
					CheckpointEditor.inst.DeleteCheckpoint(CheckpointEditor.inst.currentObj);
					EditorManager.inst.DisplayNotification("Deleted Checkpoint.", 1f, EditorManager.NotificationType.Success, false);
				}
				EditorManager.inst.DisplayNotification("Can't Delete First Checkpoint.", 1f, EditorManager.NotificationType.Error, false);
				return;
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
				{
					Debug.LogFormat("{0}Selected Objects Count: {1}", EditorPlugin.className, __instance.selectedObjects.Count);
					EditorManager.inst.ShowDialog("Multi Object Editor", false);
				}

				if (__0.IsObject())
				{
					__instance.SetCurrentKeyframe(0, 0, false, false);
				}
				__instance.RenderTimelineObject(__instance.currentObjectSelection);
				return;
			}
			__instance.SetCurrentObj(__0);
		}

		public static void RenderTimelineObjects()
		{
			foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
			{
				var beatmap = ObjEditor.inst.beatmapObjects[beatmapObject.id];
				if (EditorManager.inst.layer == beatmapObject.editorData.Layer && EditorManager.RectTransformToScreenSpace(beatmap.GetComponent<RectTransform>()).Overlaps(EditorManager.RectTransformToScreenSpace(EditorManager.inst.timeline.transform.parent.parent.GetComponent<RectTransform>())))
				{
					beatmap.SetActive(true);
				}
				else if (EditorManager.inst.layer == beatmapObject.editorData.Layer)
				{
					beatmap.SetActive(false);
				}
			}
		}

		public static IEnumerator DeleteObjects(List<ObjEditor.ObjectSelection> _objs, bool _set = true)
		{
			ienumRunning = true;

			float delay = 0f;
			var list = ObjEditor.inst.selectedObjects;
			int count = ObjEditor.inst.selectedObjects.Count;

			int num = DataManager.inst.gameData.beatmapObjects.Count;
			foreach (var obj in _objs)
            {
				if (obj.Index < num)
                {
					num = obj.Index;
                }
            }

			EditorManager.inst.DisplayNotification("Deleting Beatmap Objects [ " + count + " ]", 1f, EditorManager.NotificationType.Success);

			foreach (ObjEditor.ObjectSelection obj in _objs)
			{
				yield return new WaitForSeconds(delay);
				inst.StartCoroutine(DeleteObject(obj, false));
				delay += 0.0001f;
			}

			ObjEditor.inst.SetCurrentObj(new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Object, Mathf.Clamp(num - 1, 0, DataManager.inst.gameData.beatmapObjects.Count)));
			EditorManager.inst.DisplayNotification("Deleted Beatmap Objects [ " + count + " ]", 1f, EditorManager.NotificationType.Success);

			ienumRunning = false;
			yield break;
		}

		public static IEnumerator DeleteObject(ObjEditor.ObjectSelection _obj, bool _set = true)
		{
			int index = _obj.Index;

			//if (ModCompatibility.inst != null && ModCompatibility.catalyst != null && ModCompatibility.catalystType == ModCompatibility.CatalystType.Editor)
   //         {
			//	ModCompatibility.catalyst.GetMethod("updateProcessor", types: new[] { typeof(ObjEditor.ObjectSelection), typeof(bool) }).Invoke(ModCompatibility.catalyst, new object[] { _obj, false });
   //         }

			Updater.updateProcessor(_obj, false);

			if (_obj.IsObject())
			{
				if (DataManager.inst.gameData.beatmapObjects.Count > 1)
				{
					if (ObjectModifiersEditor.inst != null && _obj.GetObjectData() != null)
                    {
						ObjectModifiersEditor.RemoveModifierObject(_obj.GetObjectData());
                    }

                    string id = _obj.GetObjectData().id;
					ObjEditor.inst.selectedObjects.Remove(_obj);
					Destroy(ObjEditor.inst.beatmapObjects[_obj.ID]);
					ObjEditor.inst.beatmapObjects.Remove(_obj.ID);
					DataManager.inst.gameData.beatmapObjects.RemoveAt(index);
					if (ObjectManager.inst.beatmapGameObjects.ContainsKey(id))
					{
						Destroy(ObjectManager.inst.beatmapGameObjects[id].obj);
					}

					if (_set)
					{
						if (DataManager.inst.gameData.beatmapObjects.Count > 0)
						{
							ObjEditor.inst.SetCurrentObj(new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Object, Mathf.Clamp(index - 1, 0, DataManager.inst.gameData.beatmapObjects.Count - 1)));
						}
					}
					ObjectManager.inst.terminateObject(_obj);

					foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
                    {
						if (beatmapObject.parent == id)
                        {
							beatmapObject.parent = "";

							var objectSelection = new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Object, beatmapObject.id);
							objectSelection.Index = DataManager.inst.gameData.beatmapObjects.IndexOf(beatmapObject);

							Updater.updateProcessor(objectSelection, true);
						}
                    }
				}
				else
				{
					EditorManager.inst.DisplayNotification("Can't delete only object", 2f, EditorManager.NotificationType.Error, false);
				}
			}
			else if (_obj.IsPrefab())
			{
				foreach (var bm in DataManager.inst.gameData.beatmapObjects.FindAll(x => x.prefabInstanceID == _obj.ID))
                {
					var objectSelection = new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Object, bm.id);
					objectSelection.Index = DataManager.inst.gameData.beatmapObjects.IndexOf(bm);

					Updater.updateProcessor(objectSelection, false);
					//ModCompatibility.catalyst.GetMethod("updateProcessor", types: new[] { typeof(ObjEditor.ObjectSelection), typeof(bool) }).Invoke(ModCompatibility.catalyst, new object[] { objectSelection, false });
				}

				ObjEditor.inst.selectedObjects.Remove(_obj);
				Destroy(ObjEditor.inst.prefabObjects[_obj.ID]);
				ObjEditor.inst.prefabObjects.Remove(_obj.ID);
				DataManager.inst.gameData.prefabObjects.RemoveAt(_obj.Index);
				if (_set)
				{
					if (DataManager.inst.gameData.beatmapObjects.Count > 0)
					{
						ObjEditor.inst.SetCurrentObj(new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Object, Mathf.Clamp(index - 1, 0, DataManager.inst.gameData.beatmapObjects.Count - 1)));
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

		public static IEnumerator ExpandCurrentPrefab()
		{
			if (ObjEditor.inst.currentObjectSelection.IsPrefab())
			{
				Debug.LogFormat("{0}Attempting to expand prefab!", EditorPlugin.className);
				string id = ObjEditor.inst.currentObjectSelection.GetPrefabObjectData().ID;

				foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
				{
					if (beatmapObject.prefabInstanceID == id && beatmapObject.fromPrefab)
					{
						var objectSelection = new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Object, beatmapObject.id);
						objectSelection.Index = DataManager.inst.gameData.beatmapObjects.IndexOf(beatmapObject);

						Updater.updateProcessor(objectSelection, false);
					}
				}

				inst.StartCoroutine(AddExpandedPrefabToLevel(ObjEditor.inst.currentObjectSelection.GetPrefabObjectData()));

				ObjectManager.inst.terminateObject(ObjEditor.inst.currentObjectSelection);
				Destroy(ObjEditor.inst.prefabObjects[id]);
				ObjEditor.inst.prefabObjects.Remove(id);

				DataManager.inst.gameData.prefabObjects.RemoveAll(x => x.ID == id);
				DataManager.inst.gameData.beatmapObjects.RemoveAll(x => x.prefabInstanceID == id && x.fromPrefab);
				ObjEditor.inst.selectedObjects.Clear();
			}
			else
			{
				EditorManager.inst.DisplayNotification("Can't expand non-prefab!", 2f, EditorManager.NotificationType.Error);
			}
			yield break;
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

			var selection = ObjEditor.inst.currentObjectSelection.GetObjectData();

			foreach (var keyframeSelection2 in list)
			{
				if (keyframeSelection2.Index != 0)
				{
					yield return new WaitForSeconds(delay);

					selection.events[keyframeSelection2.Type].RemoveAt(keyframeSelection2.Index);

					delay += 0.0001f;
				}
				else
				{
					EditorManager.inst.DisplayNotification("Can't delete first Keyframe", 2f, EditorManager.NotificationType.Error, false);
				}
			}
			ObjEditor.inst.SetCurrentKeyframe(0);
			ObjEditor.inst.RenderTimelineObject(ObjEditor.inst.currentObjectSelection);
			ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);

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
				delay += 0.0001f;
			}
			EventManager.inst.updateEvents();

			ienumRunning = false;
		}

		public static IEnumerator AddExpandedPrefabToLevel(DataManager.GameData.PrefabObject _obj)
		{
			ienumRunning = true;

			float delay = 0f;

			string id = _obj.ID;
			DataManager.GameData.Prefab prefab = DataManager.inst.gameData.prefabs.Find(x => x.ID == _obj.prefabID);
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			foreach (BeatmapObject beatmapObject in prefab.objects)
			{
				string str = LSText.randomString(16);
				dictionary.Add(beatmapObject.id, str);
			}
			foreach (BeatmapObject beatmapObject1 in prefab.objects)
			{
				yield return new WaitForSeconds(delay);
                BeatmapObject beatmapObj = beatmapObject1;
                BeatmapObject beatmapObject2 = BeatmapObject.DeepCopy(beatmapObj, false);
				if (dictionary.ContainsKey(beatmapObj.id))
				{
					beatmapObject2.id = dictionary[beatmapObj.id];
				}
				if (dictionary.ContainsKey(beatmapObj.parent))
				{
					beatmapObject2.parent = dictionary[beatmapObj.parent];
				}
				else if (DataManager.inst.gameData.beatmapObjects.FindIndex(x => x.id == beatmapObj.parent) == -1)
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

				if (ObjectModifiersEditor.inst != null)
				{
					ObjectModifiersEditor.AddModifierObject(beatmapObject2);
				}

				delay += 0.0001f;
			}

			EditorManager.inst.DisplayNotification("Expanded Prefab Object [" + _obj + "].", 1f, EditorManager.NotificationType.Success, false);
			ienumRunning = false;
			yield break;
		}

		public static IEnumerator AddPrefabExpandedToLevel(DataManager.GameData.Prefab _obj, bool _select = false, float _offsetTime = 0f, bool _undone = false, bool _regen = false, Dictionary<string, Dictionary<string, object>> _dictionary = null)
		{
			ienumRunning = true;
			float delay = 0f;
			float audioTime = EditorManager.inst.CurrentAudioPos;
			var objEditor = ObjEditor.inst;
			Debug.LogFormat("{0}Placing prefab with {1} objects and {2} prefabs", EditorPlugin.className, _obj.objects.Count, _obj.prefabObjects.Count);

			//Objects
			{
				Dictionary<string, string> dictionary1 = new Dictionary<string, string>();
				foreach (BeatmapObject beatmapObject in _obj.objects)
				{
					string str = LSText.randomString(16);
					dictionary1.Add(beatmapObject.id, str);
				}
				Dictionary<string, string> prefabInstances = new Dictionary<string, string>();
				foreach (BeatmapObject beatmapObject in _obj.objects)
				{
					if (!string.IsNullOrEmpty(beatmapObject.prefabInstanceID) && !prefabInstances.ContainsKey(beatmapObject.prefabInstanceID))
					{
						string str = LSText.randomString(16);
						prefabInstances.Add(beatmapObject.prefabInstanceID, str);
					}
				}
				foreach (BeatmapObject beatmapObject1 in _obj.objects)
				{
					yield return new WaitForSeconds(delay);
                    BeatmapObject orig = beatmapObject1;
                    BeatmapObject beatmapObject2 = BeatmapObject.DeepCopy(orig, false);
					if (dictionary1.ContainsKey(orig.id))
						beatmapObject2.id = dictionary1[orig.id];
					if (dictionary1.ContainsKey(orig.parent))
						beatmapObject2.parent = dictionary1[orig.parent];
					else if (DataManager.inst.gameData.beatmapObjects.FindIndex(x => x.id == orig.parent) == -1)
						beatmapObject2.parent = "";

					beatmapObject2.prefabID = orig.prefabID;
					if (_regen)
					{
						//beatmapObject2.prefabInstanceID = dictionary2[orig.prefabInstanceID];
						beatmapObject2.prefabID = "";
						beatmapObject2.prefabInstanceID = "";
					}
					else
					{
						beatmapObject2.prefabInstanceID = orig.prefabInstanceID;
					}

					beatmapObject2.fromPrefab = orig.fromPrefab;
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

					if (ObjectModifiersEditor.inst != null)
					{
						ObjectModifiersEditor.AddModifierObject(beatmapObject2);
					}

					if (GameObject.Find("BepInEx_Manager").GetComponentByName("ObjectModifiersPlugin") && _dictionary != null)
					{
						var objectModifiersPlugin = GameObject.Find("BepInEx_Manager").GetComponentByName("ObjectModifiersPlugin").GetType();
						objectModifiersPlugin.GetMethod("AddModifierObjectWithValues").Invoke(objectModifiersPlugin, new object[] { beatmapObject2, _dictionary[orig.id] });
					}

					delay += 0.0001f;
				}

			}

			//Prefabs
			{
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

					foreach (var kf in prefabObject1.events)
                    {
						if (kf.eventValues.Length == 1)
							Debug.LogFormat("{0}KF (X: {1})", EditorPlugin.className, kf.eventValues[0]);
						if (kf.eventValues.Length == 2)
							Debug.LogFormat("{0}KF (X: {1}, Y: {2})", EditorPlugin.className, kf.eventValues[0], kf.eventValues[1]);
                    }

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
			}

			string stri = "object";
			if (_obj.objects.Count == 1)
			{
				stri = _obj.objects[0].name;
			}
			if (_obj.objects.Count > 1)
			{
				stri = _obj.Name;
			}
			if (_regen == false)
			{
				EditorManager.inst.DisplayNotification("Pasted Beatmap Object [ " + stri + " ] and kept Prefab Instance ID [ " + _obj.ID + " ]!", 1f, EditorManager.NotificationType.Success, false);
			}
			else
			{
				EditorManager.inst.DisplayNotification("Pasted Beatmap Object [ " + stri + " ]!", 1f, EditorManager.NotificationType.Success, false);
			}
			if (_select && (_obj.objects.Count > 1 || _obj.prefabObjects.Count > 1))
			{
				EditorManager.inst.ShowDialog("Multi Object Editor", false);
			}

			ienumRunning = false;
			yield break;
		}

		public static void AddPrefabObjectToLevel(DataManager.GameData.Prefab _prefab)
		{
			DataManager.GameData.PrefabObject prefabObject = new DataManager.GameData.PrefabObject();
			prefabObject.ID = LSText.randomString(16);
			prefabObject.prefabID = _prefab.ID;
			prefabObject.StartTime = EditorManager.inst.CurrentAudioPos;
			prefabObject.editorData.Layer = EditorManager.inst.layer;

			prefabObject.events[1].eventValues = new float[2]
			{
				1f,
				1f
			};

			DataManager.inst.gameData.prefabObjects.Add(prefabObject);
			ObjectManager.inst.updateObjects();
			if (prefabObject.editorData.Layer != EditorManager.inst.layer)
			{
				EditorManager.inst.SetLayer(prefabObject.editorData.Layer);
			}

			try
			{
				if (DataManager.inst.gameData.prefabs.IndexOf(_prefab) > -1)
				{
					var modPrefab = Objects.prefabs[DataManager.inst.gameData.prefabs.IndexOf(_prefab)];

					var modPrefabObject = new Objects.PrefabObject(prefabObject);

					modPrefabObject.modifiers = modPrefab.modifiers;

					if (!Objects.prefabObjects.ContainsKey(prefabObject.ID))
						Objects.prefabObjects.Add(prefabObject.ID, modPrefabObject);
				}
			}
			catch (Exception ex)
			{
				UnityEngine.Debug.Log($"{EditorPlugin.className}Failed to set prefab object.\nEXCEPTION: {ex.Message}\nSTACKTRACE: {ex.StackTrace}");
			}


			ObjEditor.inst.RenderTimelineObject(new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Prefab, prefabObject.ID));
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
				DataManager.GameData.EventKeyframe eventKeyframe = DataManager.GameData.EventKeyframe.DeepCopy(_keyframes[keyframeSelection]);
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

		#endregion

		#region Timeline

		public static void SetNewTime(string _value)
		{
			AudioManager.inst.CurrentAudioSource.time = float.Parse(_value);
        }

        public static int GetLayer(int _layer)
        {
            if (_layer > 0)
            {
                if (_layer < 5)
                {
                    int l = _layer;
                    return l;
                }
                else
                {
                    int l = _layer + 1;
                    return l;
                }
            }
            return 0;
        }

		public static string GetLayerString(int _layer)
        {
			if (_layer >= 0)
            {
				if (_layer > 4)
                {
					return (GetLayer(_layer) - 1).ToString();
                }
				if (_layer < 5)
                {
					return (GetLayer(_layer) + 1).ToString();
                }
            }
			return "";
        }

        public static Color GetLayerColor(int _layer)
		{
			if (_layer < EditorManager.inst.layerColors.Count)
			{
				return EditorManager.inst.layerColors[_layer];
			}
			return Color.white;
		}

		public static void SetLayer(int _layer, bool setHistory = true)
		{
			Image layerImage = layersIF.gameObject.GetComponent<Image>();
			DataManager.inst.UpdateSettingInt("EditorLayer", _layer);
			int oldLayer = EditorManager.inst.layer;

			EditorManager.inst.layer = _layer;
			EditorManager.inst.timelineWaveformOverlay.GetComponent<Image>().color = GetLayerColor(_layer);
			layerImage.color = GetLayerColor(_layer);

			layersIF.onValueChanged.RemoveAllListeners();

			if (_layer != 5)
			{
				layersIF.text = GetLayerString(_layer);
			}
			else
            {
				layersIF.text = "E";
			}

			layersIF.onValueChanged.AddListener(delegate (string _value)
			{
				if (int.TryParse(_value, out int num))
				{
					if (num > 0)
					{
						if (num < 6)
						{
							SetLayer(num - 1);
						}
						else
						{
							SetLayer(num);
						}
					}
					else
					{
						SetLayer(0);
						layersIF.text = "1";
					}

					layerImage.color = GetLayerColor(GetLayer(num - 1));
				}
			});

			if (EditorManager.inst.layer == 5 && EditorManager.inst.lastLayer != 5)
			{
				EventEditor.inst.EventLabels.SetActive(true);
				EventEditor.inst.EventHolders.SetActive(true);
				EventEditor.inst.CreateEventObjects();
				CheckpointEditor.inst.CreateCheckpoints();
				ObjEditor.inst.RenderTimelineObjects("");
				GameObject.Find("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject/6").GetComponent<Toggle>().isOn = true;
				if (EventEditorPatch.loggle != null)
                {
					EventEditorPatch.loggle.isOn = false;
                }
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
				if (EventEditorPatch.loggle != null)
				{
					EventEditorPatch.loggle.isOn = false;
				}
			}
			else
			{
				ObjEditor.inst.RenderTimelineObjects("");
				GameObject.Find("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject/6").GetComponent<Toggle>().isOn = false;
				if (EventEditorPatch.loggle != null)
				{
					EventEditorPatch.loggle.isOn = false;
				}
			}

			if (_layer < EditorManager.inst.layerSelectors.Count)
			{
				EditorManager.inst.layerSelectors[_layer].GetComponent<Toggle>().isOn = true;
			}

			int tmpLayer = EditorManager.inst.layer;
			if (setHistory)
			{
				EditorManager.inst.history.Add(new History.Command("Change Layer", delegate ()
				{
					Debug.LogFormat("{0}Redone layer: {1}", EditorPlugin.className, tmpLayer);
					SetLayer(tmpLayer, false);
				}, delegate ()
				{
					Debug.LogFormat("{0}Undone layer: {1}", EditorPlugin.className, oldLayer);
					SetLayer(oldLayer, false);
				}), false);
			}
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

        #endregion

        #region UI

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
			uiDictionary.Add("Autokill - Time Of Death DD", tfv.Find("autokill/tod-dropdown").GetComponent<Dropdown>());
			uiDictionary.Add("Autokill - Time Of Death IF", tfv.Find("autokill/tod-value").GetComponent<Dropdown>());
			uiDictionary.Add("Autokill - Time Of Death Set", tfv.Find("autokill/|"));
			uiDictionary.Add("Autokill - Time Of Death Set B", tfv.Find("autokill/|").GetComponent<Button>());
			uiDictionary.Add("Autokill - Collapse T", tfv.Find("autokill/collapse").GetComponent<Toggle>());
			uiDictionary.Add("Start Time - Time ET", tfv.Find("time").GetComponent<EventTrigger>());
			uiDictionary.Add("Start Time - Time IF", tfv.Find("time/time").GetComponent<InputField>());
			uiDictionary.Add("Start Time - Lock T", tfv.Find("time/lock").GetComponent<Toggle>());
			uiDictionary.Add("Start Time - <<", tfv.Find("time/<<").GetComponent<Button>());
			uiDictionary.Add("Start Time - <", tfv.Find("time/<").GetComponent<Button>());
			uiDictionary.Add("Start Time - |", tfv.Find("time/|").GetComponent<Button>());
			uiDictionary.Add("Start Time - >", tfv.Find("time/>").GetComponent<Button>());
			uiDictionary.Add("Start Time - >>", tfv.Find("time/>>").GetComponent<Button>());
			uiDictionary.Add("Depth - Slider", tfv.Find("depth/depth").GetComponent<Slider>());
			uiDictionary.Add("Depth - IF", tfv.Find("spacer/depth").GetComponent<InputField>());
			uiDictionary.Add("Depth - <", tfv.Find("spacer/depth/<").GetComponent<Button>());
			uiDictionary.Add("Depth - >", tfv.Find("spacer/depth/>").GetComponent<Button>());
			uiDictionary.Add("Shape - Settings", tfv.Find("shapesettings"));

			yield break;
        }

		public static void SetDepthSlider(BeatmapObject beatmapObject, float _value, InputField inputField, Slider slider)
        {
			var num = (int)_value;

			beatmapObject.Depth = num;

			slider.onValueChanged.RemoveAllListeners();
			slider.value = num;
			slider.onValueChanged.AddListener(delegate (float _val)
			{
				SetDepthInputField(beatmapObject, ((int)_val).ToString(), inputField, slider);
			});

			ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
		}

		public static void SetDepthInputField(BeatmapObject beatmapObject, string _value, InputField inputField, Slider slider)
		{
			var num = int.Parse(_value);

			beatmapObject.Depth = num;

			inputField.onValueChanged.RemoveAllListeners();
			inputField.text = num.ToString();
			inputField.onValueChanged.AddListener(delegate (string _val)
			{
				if (int.TryParse(_val, out int numb))
					SetDepthSlider(beatmapObject, numb, inputField, slider);
			});

			ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
		}

		public static IEnumerator RefreshObjectGUI()
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

					if (!oxIF.gameObject.GetComponent<InputFieldHelper>())
                    {
						oxIF.gameObject.AddComponent<InputFieldHelper>();
                    }

					oxIF.onValueChanged.RemoveAllListeners();
					oxIF.text = beatmapObject.origin.x.ToString();
					oxIF.onValueChanged.AddListener(delegate (string _value)
					{
						beatmapObject.origin.x = float.Parse(_value);
						objectManager.updateObjects(currentObjectSelection);
						objEditor.RenderTimelineObject(currentObjectSelection);
					});

					var oyIF = tfv.Find("origin/y").GetComponent<InputField>();

					if (!oyIF.gameObject.GetComponent<InputFieldHelper>())
					{
						oyIF.gameObject.AddComponent<InputFieldHelper>();
					}

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

					if (GameObject.Find("BepInEx_Manager").GetComponentByName("ObjectModifiersPlugin"))
                    {
						objType.options = new List<Dropdown.OptionData>
						{
							new Dropdown.OptionData("Normal"),
							new Dropdown.OptionData("Helper"),
							new Dropdown.OptionData("Decoration"),
							new Dropdown.OptionData("Empty"),
							new Dropdown.OptionData("Solid")
						};
                    }

					Triggers.ObjEditorDropdownValues(objType, "objectType", beatmapObject.objectType, true, true);
				}

				Debug.LogFormat("{0}Refresh Object GUI: Layers", EditorPlugin.className);
				//Layers
				{
					var editorLayers = tfv.Find("editor/layers");
					var editorLayersIF = editorLayers.GetComponent<InputField>();
					var editorLayersImage = editorLayers.GetComponent<Image>();

					editorLayersIF.onValueChanged.RemoveAllListeners();
					
					editorLayersIF.text = GetLayerString(beatmapObject.editorData.Layer);

					editorLayersImage.color = GetLayerColor(beatmapObject.editorData.Layer);

					editorLayersIF.onValueChanged.AddListener(delegate (string _value)
					{
						if (int.Parse(_value) > 0)
						{
							if (int.Parse(_value) < 6)
							{
								beatmapObject.editorData.Layer = int.Parse(_value) - 1;
								objEditor.RenderTimelineObject(currentObjectSelection);
							}
							else
							{
								beatmapObject.editorData.Layer = int.Parse(_value);
								objEditor.RenderTimelineObject(currentObjectSelection);
							}
						}
						else
						{
							beatmapObject.editorData.Layer = 0;
							objEditor.RenderTimelineObject(currentObjectSelection);
							editorLayersIF.text = "1";
						}

						editorLayersImage.color = GetLayerColor(beatmapObject.editorData.Layer);
					});

					Triggers.AddEventTrigger(editorLayers.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDeltaInt(editorLayersIF, 1, false, new List<int> { 1, int.MaxValue }) });
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

					if (beatmapObject.autoKillType == BeatmapObject.AutoKillType.FixedTime || beatmapObject.autoKillType == BeatmapObject.AutoKillType.SongTime || beatmapObject.autoKillType == BeatmapObject.AutoKillType.LastKeyframeOffset)
					{
						todValue.gameObject.SetActive(true);

						akOffset.onValueChanged.RemoveAllListeners();
						akOffset.text = beatmapObject.autoKillOffset.ToString();
						akOffset.onValueChanged.AddListener(delegate (string _value)
						{
							float num = float.Parse(_value);
							if (beatmapObject.autoKillType == BeatmapObject.AutoKillType.SongTime)
							{
								float startTime = beatmapObject.StartTime;
								if (num < startTime)
									num = startTime + 0.1f;
							}

							if (num < 0f)
								num = 0f;

							beatmapObject.autoKillOffset = num;
							objectManager.updateObjects(currentObjectSelection);
							objEditor.RenderTimelineObject(currentObjectSelection);
						});

						akset.gameObject.SetActive(true);
						aksetButt.onClick.RemoveAllListeners();
						aksetButt.onClick.AddListener(delegate ()
						{
							float num = 0f;

							if (beatmapObject.autoKillType == BeatmapObject.AutoKillType.SongTime)
								num = AudioManager.inst.CurrentAudioSource.time;
							else num = AudioManager.inst.CurrentAudioSource.time - beatmapObject.StartTime;

							if (num < 0f)
								num = 0f;

							beatmapObject.autoKillOffset = num;
							objectManager.updateObjects(currentObjectSelection);
							objEditor.RenderTimelineObject(currentObjectSelection);
						});

						Triggers.AddEventTrigger(todValue.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(akOffset, 0.1f, 10f, false, new List<float> { 0f, float.PositiveInfinity }) });
					}
					else
					{
						todValue.gameObject.SetActive(false);
						akOffset.onValueChanged.RemoveAllListeners();
						akset.gameObject.SetActive(false);
						aksetButt.onClick.RemoveAllListeners();
					}

					var collapse = tfv.Find("autokill/collapse").GetComponent<Toggle>();

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
					time.triggers.Add(Triggers.ScrollDelta(timeIF, 0.1f, 10f));

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

					depthText.onValueChanged.RemoveAllListeners();
					depthText.text = beatmapObject.Depth.ToString();

					depthText.onValueChanged.AddListener(delegate (string _val)
					{
						if (int.TryParse(_val, out int num))
							SetDepthSlider(beatmapObject, num, depthText, depthSlider);
					});

					bool showAcceptableRange = true;
					if (showAcceptableRange)
                    {
						depthSlider.maxValue = 219;
						depthSlider.minValue = -98;
                    }
					else
					{
						depthSlider.maxValue = 30;
						depthSlider.minValue = 0;
					}

					depthSlider.onValueChanged.RemoveAllListeners();
					depthSlider.value = beatmapObject.Depth;
					depthSlider.onValueChanged.AddListener(delegate (float _val)
					{
						SetDepthInputField(beatmapObject, _val.ToString(), depthText, depthSlider);
					});

					Triggers.IncreaseDecreaseButtonsInt(depthText, -1);
					Triggers.AddEventTrigger(depthText.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDeltaInt(depthText, 1) });
				}

				Debug.LogFormat("{0}Refresh Object GUI: Shape", EditorPlugin.className);
				//Shape Settings
				{
					var shapeSettings = tfv.Find("shapesettings");
					foreach (object obj3 in shapeSettings)
					{
						var ch = (Transform)obj3;
						if (ch.name != "5" && ch.name != "7")
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
						ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
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
					shapeSettings.GetChild(beatmapObject.shape).gameObject.SetActive(true);
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
						textIF.onValueChanged.ClearAll();
						textIF.text = beatmapObject.text;
						textIF.onValueChanged.AddListener(delegate (string _value)
						{
							beatmapObject.text = _value;
							ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
						});
					}
					else if (beatmapObject.shape == 6)
					{
						shapeSettings.Find("7/select").GetComponent<Button>().onClick.RemoveAllListeners();
						shapeSettings.Find("7/select").GetComponent<Button>().onClick.AddListener(delegate ()
						{
							OpenImageSelector();
						});
						shapeSettings.Find("7/text").GetComponent<Text>().text = (string.IsNullOrEmpty(beatmapObject.text) ? "No Object Selected" : beatmapObject.text);
					}
					//else if (beatmapObject.shape == 6)
					//{
					//	var textIF = shapeSettings.Find("5").GetComponent<InputField>();
					//	textIF.onValueChanged.RemoveAllListeners();
					//	textIF.text = beatmapObject.text;
					//	textIF.onValueChanged.AddListener(delegate (string _value)
					//	{
					//		beatmapObject.text = _value;
					//		ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection, false);
					//	});
					//}
				}

				string parent = beatmapObject.parent;
				Debug.LogFormat("{0}Refresh Object GUI: Parent {1}", EditorPlugin.className, parent);
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

                        BeatmapObject beatmapObjectParent = null;
						ObjEditor.ObjectSelection tmp = new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Object, parent);
						if (DataManager.inst.gameData.beatmapObjects.Find(x => x.id == parent) != null)
						{
							beatmapObjectParent = DataManager.inst.gameData.beatmapObjects.Find(x => x.id == parent);
							parentTextText.text = beatmapObjectParent.name;
							tfv.Find("parent/text").GetComponent<HoverTooltip>().tooltipLangauges[0].hint = string.Format("Parent chain count: [{0}]\n(Inclusive)", beatmapObject.GetParentChain().Count);
						}
						else if (parent == "CAMERA_PARENT" && ObjectModifiersEditor.inst != null)
						{
							parentTextText.text = "[CAMERA]";
						}
						else if (parent == "PLAYER_PARENT" && ObjectModifiersEditor.inst != null)
                        {
							parentTextText.text = "[NEAREST PLAYER]";
                        }

						parentText.interactable = true;
						parentText.onClick.RemoveAllListeners();
						parentText.onClick.AddListener(delegate ()
						{
							if (DataManager.inst.gameData.beatmapObjects.Find((BeatmapObject x) => x.id == parent) != null && (parent != "CAMERA_PARENT" && parent != "PLAYER_PARENT" && ObjectModifiersEditor.inst != null && ObjectModifiersEditor.objectModifiersPlugin != null || ObjectModifiersEditor.objectModifiersPlugin == null))
								ObjEditor.inst.SetCurrentObj(tmp);
							else
							{
								SetLayer(5);
								EventEditor.inst.SetCurrentEvent(0, RTExtensions.ClosestEventKeyframe(0));
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
						Debug.LogFormat("{0}Refresh Object GUI: Object Doesn't Have Parent", EditorPlugin.className);

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

				var inspector = AccessTools.TypeByName("UnityExplorer.InspectorManager");
				if (inspector != null && !tfv.Find("inspect"))
                {
                    var inspect = Instantiate(tfv.Find("applyprefab").gameObject);
					inspect.SetActive(true);
                    inspect.transform.SetParent(tfv);
					inspect.transform.SetSiblingIndex(19);
					inspect.transform.localScale = Vector3.one;
					inspect.name = "inspect";

					inspect.transform.GetChild(0).GetComponent<Text>().text = "Inspect";
				}

				if (tfv.Find("inspect"))
				{
					var deleteButton = tfv.Find("inspect").GetComponent<Button>();
					deleteButton.onClick.ClearAll();
					deleteButton.onClick.AddListener(delegate ()
					{
						if (beatmapObject.GetGameObject() != null)
							inspector.GetMethod("Inspect", new[] { typeof(object), AccessTools.TypeByName("UnityExplorer.CacheObject.CacheObjectBase") }).Invoke(inspector, new object[] { beatmapObject.GetGameObject(), null });
					});
				}

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

				if (ObjectModifiersEditor.inst != null && ObjectModifiersEditor.showModifiers)
                {
					yield return new WaitForSeconds(0.1f);
					inst.StartCoroutine(ObjectModifiersEditor.RenderModifiers());
                }
			}
			yield break;
		}

		public static IEnumerator SetupTooltips()
		{
			yield return new WaitForSeconds(2f);

			if (RTExtensions.TryFind("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/depth/depth", out GameObject depth) && depth.TryGetComponent(out HoverTooltip depthTip))
			{
				depthTip.tooltipLangauges.Add(Triggers.NewTooltip("Set the depth layer of the object.", "Depth is if an object shows above or below another object. However, higher number does not equal higher depth here since it's reversed.<br>Higher number = lower depth<br>Lower number = higher depth."));
			}
			else
				Debug.LogErrorFormat("{0}Could not set depth tooltip!", EditorPlugin.className);

			if (RTExtensions.TryFind("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/timeline", out GameObject timeline) && timeline.TryGetComponent(out HoverTooltip timelineTip))
			{
				timelineTip.tooltipLangauges.Add(Triggers.NewTooltip("Create a keyframe in one of the four keyframe bins by right clicking.", "Each keyframe that controls the objects' base properties like position, scale, rotation and color are located here."));
			}
			else
				Debug.LogErrorFormat("{0}Could not set timeline tooltip!", EditorPlugin.className);

			if (RTExtensions.TryFind("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/shapesettings/5", out GameObject textShape) && textShape.TryGetComponent(out HoverTooltip textShapeTip))
			{
				textShapeTip.tooltipLangauges.Add(Triggers.NewTooltip("Write your custom text here.", "Anything you write here will show up as a text object. There are a lot of formatting options, such as < b >, < i >, < br >, < color = #FFFFFF > < alpha = #FF > and more. (without the spaces between)"));
			}
			else
				Debug.LogErrorFormat("{0}Could not set text shape tooltip!", EditorPlugin.className);

			if (RTExtensions.TryFind("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject/prefab", out GameObject prefab) && prefab.TryGetComponent(out HoverTooltip prefabTip))
			{
				prefabTip.tooltipLangauges.Add(Triggers.NewTooltip("Save groups of objects across levels.", "Prefabs act as a collection of objects that you can easily transfer from one level to the next, or even share online."));
			}
			else
				Debug.LogErrorFormat("{0}Could not set prefab tooltip!", EditorPlugin.className);

			if (RTExtensions.TryFind("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject/object", out GameObject @object) && @object.TryGetComponent(out HoverTooltip objectTip))
			{
				objectTip.tooltipLangauges.Add(Triggers.NewTooltip("Beatmap Objects.", "The very thing levels are made of!"));
			}
			else
				Debug.LogErrorFormat("{0}Could not set object tooltip!", EditorPlugin.className);

			if (RTExtensions.TryFind("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject/event", out GameObject @event) && @event.TryGetComponent(out HoverTooltip eventTip))
			{
				eventTip.tooltipLangauges.Add(Triggers.NewTooltip("Use Markers to time and separate segments of a level.", "Markers can be helpful towards organizing the level into segments or remembering specific timings. You can also use markers to loop specific parts of the song if you enable it through the EditorManagement Config."));
			}
			else
				Debug.LogErrorFormat("{0}Could not set event tooltip!", EditorPlugin.className);

			if (RTExtensions.TryFind("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject/background", out GameObject bg) && bg.TryGetComponent(out HoverTooltip bgTip))
			{
				bgTip.tooltipLangauges.Add(Triggers.NewTooltip("Create or look at the list of 3D backgrounds here.", "3D backgrounds are completely static, but they can scale up and down to the reactive channels of the music."));
			}
			else
				Debug.LogErrorFormat("{0}Could not set bg tooltip!", EditorPlugin.className);

			if (EventEditor.inst.EventHolders.TryGetComponent(out HoverTooltip eventLabelTip))
			{
				eventLabelTip.tooltipLangauges.Add(Triggers.NewTooltip("Create an event keyframe to spice up your level!", "Each event keyframe type has its own properties that you can utilize."));
			}
			else
				Debug.LogErrorFormat("{0}Could not set event holders tooltip!", EditorPlugin.className);

			//File Dropdown
			{
				try
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

					try
					{
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
					catch (Exception ex)
					{
						LogException(ex, "Failed to GetKeyCodes for File Dropdown.");
					}
				}
				catch (Exception ex)
				{
					LogException(ex, "There was an error in setting File Dropdown tooltips.");
				}
            }

			//Edit Dropdown
			{
				try
				{
					HoverTooltip undoDDTip = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/Edit/Edit Dropdown/Undo").GetComponent<HoverTooltip>();
					HoverTooltip redoDDTip = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/Edit/Edit Dropdown/Redo").GetComponent<HoverTooltip>();

					undoDDTip.tooltipLangauges.Add(Triggers.NewTooltip("[WIP] Undoes the last action.", "", new List<string> { "Ctrl + Z" }));
					redoDDTip.tooltipLangauges.Add(Triggers.NewTooltip("[WIP] Redoes the last undone action.", "", new List<string> { "Ctrl + Shift + Z" }));
				}
				catch (Exception ex)
				{
					LogException(ex, "There was an error in setting Edit Dropdown tooltips.");
				}
			}

			//View Dropdown
			{
				try
				{
					var objTag = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/View/View Dropdown/Timeline Zoom");
					var plaEdit = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/View/View Dropdown/Grid View");
					var shoHelp = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar/View/View Dropdown/Show Help");

					Triggers.AddTooltip(objTag, "Modify objects to do anything you want!", "", new List<string> { "F3" });
					Triggers.AddTooltip(plaEdit, "Create your own player models to use in stories / gameplay.", "", new List<string> { "F6" });
					Triggers.AddTooltip(shoHelp, "Toggles the Info box.", "", new List<string> { "Ctrl + H" });
				}
				catch (Exception ex)
				{
					LogException(ex, "There was an error in setting View Dropdown tooltips.");
				}
			}

			//var list = Resources.FindObjectsOfTypeAll<HoverTooltip>().ToList();

			yield break;
		}

		public static void LogException(Exception ex, string str) => Debug.LogErrorFormat("{0}{1}\nEXCEPTION: {2}\nSTACKTRACE: {3}", EditorPlugin.className, str, ex.Message, ex.StackTrace);

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
			searchBar.onValueChanged.ClearAll();
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
			propWinButton.onClick.ClearAll();
			propWinButton.onClick.AddListener(delegate ()
			{
				EditorManager.inst.ShowDialog("Object Search Popup");
				ReSync();
			});

			propWin.SetActive(true);

			propWin.transform.Find("Image").GetComponent<Image>().sprite = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/parent/parent/image").GetComponent<Image>().sprite;

			//Add Search Object Popup to EditorDialogsDictionary
			{
				Triggers.AddEditorDialog("Object Search Popup", objectSearch);
			}
		}

		static void ROSTest()
        {
			EditorManager.inst.ShowDialog("Object Search Popup");

			RefreshObjectSearch2(delegate ()
			{
				foreach (var objectSelection in ObjEditor.inst.selectedObjects)
				{
					if (objectSelection.IsObject())
					{
						objectSelection.GetObjectData().parent = "";
						ObjectManager.inst.updateObjects(objectSelection);
					}
				}
			}, true, "");
		}

		public static void RefreshObjectSearch2(Action action, bool multi = false, string multiValue = "")
		{
			Debug.LogFormat("{0}Mutli: {1}\nMultiValue: {2}", EditorPlugin.className, multi.ToString(), multiValue.ToString());
			var content = EditorManager.inst.GetDialog("Object Search Popup").Dialog.Find("mask/content");

			if (multi && multiValue == "parent")
			{
				var buttonPrefab = Instantiate(EditorManager.inst.spriteFolderButtonPrefab);
				buttonPrefab.transform.SetParent(content);
				buttonPrefab.transform.localScale = Vector3.one;
				buttonPrefab.name = "Clear Parents";
				buttonPrefab.transform.GetChild(0).GetComponent<Text>().text = "Clear Parents";

				var b = buttonPrefab.GetComponent<Button>();
				b.onClick.RemoveAllListeners();
				b.onClick.AddListener(delegate ()
				{
					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						if (objectSelection.IsObject())
						{
							objectSelection.GetObjectData().parent = "";
							ObjectManager.inst.updateObjects(objectSelection);
						}
					}
				});

				var x = EditorManager.inst.GetDialog("Object Search Popup").Dialog.Find("Panel/x/Image").GetComponent<Image>().sprite;
				var image = buttonPrefab.transform.Find("Image").GetComponent<Image>();
				image.color = Color.red;
				image.sprite = x;
			}

			LSHelpers.DeleteChildren(content);

			foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
			{
				var regex = new Regex(@"\[([0-9])\]");
				var match = regex.Match(searchterm);

				if (string.IsNullOrEmpty(searchterm) || beatmapObject.name.ToLower().Contains(searchterm.ToLower()) || match.Success && int.Parse(match.Groups[1].ToString()) < DataManager.inst.gameData.beatmapObjects.Count && DataManager.inst.gameData.beatmapObjects.IndexOf(beatmapObject) == int.Parse(match.Groups[1].ToString()))
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

							Debug.LogFormat("{0}Attempting to sync {1} to selection!", EditorPlugin.className, id);

							action();

							EditorManager.inst.HideDialog("Object Search Popup");
						}
					});
					var image = buttonPrefab.transform.Find("Image").GetComponent<Image>();
					image.color = GetObjectColor(beatmapObject, false);

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

					if (beatmapObject.TryGetGameObject(out GameObject gameObjectRef))
					{
						Transform transform = gameObjectRef.transform;

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
							"<br>COL: " + "<#" + ColorToHex(GetObjectColor(beatmapObject, false)) + ">" + "█ <b>#" + ColorToHex(GetObjectColor(beatmapObject, true)) + "</b></color>" +
							ptr;

						Triggers.AddTooltip(buttonPrefab, desc, hint);
					}
				}
			}
		}

		public static void RefreshObjectSearch(bool multi = false, string multiValue = "", bool _objEditor = false, bool _objectManager = false)
		{
			Debug.LogFormat("{0}Mutli: {1}\nMultiValue: {2}", EditorPlugin.className, multi.ToString(), multiValue.ToString());
			var content = EditorManager.inst.GetDialog("Object Search Popup").Dialog.Find("mask/content");

			if (multi && multiValue == "parent")
			{
				var buttonPrefab = Instantiate(EditorManager.inst.spriteFolderButtonPrefab);
				buttonPrefab.transform.SetParent(content);
				buttonPrefab.transform.localScale = Vector3.one;
				buttonPrefab.name = "Clear Parents";
				buttonPrefab.transform.GetChild(0).GetComponent<Text>().text = "Clear Parents";

				var b = buttonPrefab.GetComponent<Button>();
				b.onClick.RemoveAllListeners();
				b.onClick.AddListener(delegate ()
				{
					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						if (objectSelection.IsObject())
						{
							objectSelection.GetObjectData().parent = "";
							ObjectManager.inst.updateObjects(objectSelection);
						}
					}
				});

				var x = EditorManager.inst.GetDialog("Object Search Popup").Dialog.Find("Panel/x/Image").GetComponent<Image>().sprite;
				var image = buttonPrefab.transform.Find("Image").GetComponent<Image>();
				image.color = Color.red;
				image.sprite = x;
			}

			LSHelpers.DeleteChildren(content);

			foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
			{
				var regex = new Regex(@"\[([0-9])\]");
				var match = regex.Match(searchterm);

				if (string.IsNullOrEmpty(searchterm) || beatmapObject.name.ToLower().Contains(searchterm.ToLower()) || match.Success && int.Parse(match.Groups[1].ToString()) < DataManager.inst.gameData.beatmapObjects.Count && DataManager.inst.gameData.beatmapObjects.IndexOf(beatmapObject) == int.Parse(match.Groups[1].ToString()))
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
					image.color = GetObjectColor(beatmapObject, false);

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

					if (beatmapObject.TryGetGameObject(out GameObject gameObjectRef))
					{
						Transform transform = gameObjectRef.transform;

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
							"<br>COL: " + "<#" + ColorToHex(GetObjectColor(beatmapObject, false)) + ">" + "█ <b>#" + ColorToHex(GetObjectColor(beatmapObject, true)) + "</b></color>" +
							ptr;

						Triggers.AddTooltip(buttonPrefab, desc, hint);
					}
				}
			}
		}

		public static Color GetObjectColor(BeatmapObject _beatmapObject, bool _ignoreTransparency)
		{
			if (_beatmapObject.objectType == ObjectType.Empty)
			{
				return Color.white;
			}

			if (_beatmapObject.TryGetGameObject(out GameObject gameObject) && gameObject.TryGetComponent(out Renderer renderer))
			{
				Color color = Color.white;
				if (AudioManager.inst.CurrentAudioSource.time < _beatmapObject.StartTime)
				{
					color = GameManager.inst.LiveTheme.objectColors[(int)_beatmapObject.events[3][0].eventValues[0]];
				}
				else if (AudioManager.inst.CurrentAudioSource.time > _beatmapObject.StartTime + _beatmapObject.GetObjectLifeLength() && _beatmapObject.autoKillType != BeatmapObject.AutoKillType.OldStyleNoAutokill)
				{
					color = GameManager.inst.LiveTheme.objectColors[(int)_beatmapObject.events[3][_beatmapObject.events[3].Count - 1].eventValues[0]];
				}
				else if (renderer.material.HasProperty("_Color"))
				{
					color = renderer.material.color;
				}
				if (_ignoreTransparency)
				{
					color.a = 1f;
				}
				return color;
			}

			return Color.white;
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

		public static IEnumerator InternalPrefabs(bool _toggle = false)
		{
			if (!DataManager.inst.gameData.prefabs.Exists(x => x.ID == "toYoutoYoutoYou") && ConfigEntries.PrefabExampleTemplate.Value)
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
			hover.size = ConfigEntries.PrefabButtonHoverSize.Value;

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
			hover.size = ConfigEntries.PrefabButtonHoverSize.Value;

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
			hover.size = ConfigEntries.PrefabButtonHoverSize.Value;

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
				//typeName.text = "invalid";
				//color.color = Color.red;
				_p.Type = DataManager.inst.PrefabTypes.Count - 1;
			}
			//else
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
						AddPrefabObjectToLevel(_p);
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

		public Dictionary<string, GameObject> themeBars = new Dictionary<string, GameObject>();

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
					EventEditorPatch.RenderThemeEditor(eventEditor, -1);
				});

				foreach (var themeTmp in DataManager.inst.AllThemes)
				{
					if (themeTmp.name.ToLower().Contains(__1.ToLower()))
					{
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
							EventEditorPatch.RenderThemeEditor(eventEditor, int.Parse(tmpThemeID));
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

		public static IEnumerator RenderThemeListNew(Transform __0, string __1)
		{
			var eventEditor = EventEditor.inst;
			var themeEditor = ThemeEditor.inst;

			if (!loadingThemes && !eventEditor.eventDrag)
			{
				loadingThemes = true;
				Debug.LogFormat("{0}Rendering theme list...", EditorPlugin.className);

				var sw = new System.Diagnostics.Stopwatch();
				sw.Start();

				var parent = __0.Find("themes/viewport/content");
				//LSHelpers.DeleteChildren(parent);
				int num = 0;

				if (!inst.themeBars.ContainsKey("NEWTHEME"))
				{
					var cr = Instantiate(eventEditor.ThemeAdd);
					var tf = cr.transform;
					tf.SetParent(parent);
					cr.SetActive(true);
					tf.localScale = Vector2.one;
					cr.GetComponent<Button>().onClick.AddListener(delegate ()
					{
						EventEditorPatch.RenderThemeEditor(eventEditor, -1);
					});
					inst.themeBars.Add("NEWTHEME", cr);
				}

				foreach (var theme in inst.themeBars)
                {
					if (DataManager.inst.AllThemes.Find(x => x.id == theme.Key) == null && theme.Key != "NEWTHEME")
                    {
						Destroy(inst.themeBars[theme.Key]);
						inst.themeBars.Remove(theme.Key);
					}
					else if (DataManager.inst.AllThemes.Find(x => x.id == theme.Key) != null && theme.Key != "NEWTHEME")
                    {
						var ttf = theme.Value.transform;

						inst.StartCoroutine(GetThemeSprite(DataManager.inst.AllThemes.Find(x => x.id == theme.Key), delegate (Sprite _sprite)
						{
							ttf.Find("image").GetComponent<Image>().sprite = _sprite;
						}));
					}
                }

				foreach (var themeTmp in DataManager.inst.AllThemes)
				{
					var tmpThemeID = themeTmp.id;
					if (themeTmp.name.ToLower().Contains(__1.ToLower()) && !inst.themeBars.ContainsKey(tmpThemeID))
					{
						int tmpVal = num;

						var tobj = Instantiate(eventEditor.ThemePanel);
						var ttf = tobj.transform;
						ttf.SetParent(parent);
						ttf.localScale = Vector2.one;
						tobj.name = tmpThemeID;

						var image = ttf.Find("image");

						inst.StartCoroutine(GetThemeSprite(themeTmp, delegate (Sprite _sprite)
						{
							image.GetComponent<Image>().sprite = _sprite;
						}));

						image.GetComponent<Button>().onClick.AddListener(delegate ()
						{
							int n = 0;
							if (DataManager.inst.AllThemes.Find(x => x.id == tmpThemeID) != null)
                            {
								n = int.Parse(tmpThemeID);
                            }

							DataManager.inst.gameData.eventObjects.allEvents[eventEditor.currentEventType][eventEditor.currentEvent].eventValues[0] = n;
							//DataManager.inst.gameData.eventObjects.allEvents[eventEditor.currentEventType][eventEditor.currentEvent].eventValues[0] = DataManager.inst.GetThemeIndexToID(tmpVal);
							EventManager.inst.updateEvents();
							eventEditor.RenderThemePreview(__0);
						});
						ttf.Find("edit").GetComponent<Button>().onClick.AddListener(delegate ()
						{
							EventEditorPatch.RenderThemeEditor(eventEditor, int.Parse(tmpThemeID));
						});

						var delete = ttf.Find("delete").GetComponent<Button>();

						delete.interactable = tmpVal >= DataManager.inst.BeatmapThemes.Count;
						delete.onClick.AddListener(delegate ()
						{
							EditorManager.inst.ShowDialog("Warning Popup");
							RefreshWarningPopup("Are you sure you want to delete this theme?", delegate ()
							{
								themeEditor.DeleteTheme(themeTmp);
								eventEditor.previewTheme.id = null;
								eventEditor.StartCoroutine(themeEditor.LoadThemes());
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

						inst.themeBars.Add(tmpThemeID, tobj);
					}
					num++;
				}

				Debug.LogFormat("{0}Finished rendering theme list and took {1} to complete!", EditorPlugin.className, sw.Elapsed);
				sw.Stop();
				sw = null;

				loadingThemes = false;
			}

			yield break;
		}

		public static IEnumerator SetupTimelineTriggers()
		{
			yield return new WaitForSeconds(1f);

			EventTrigger.Entry entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.PointerEnter;
			entry.callback.AddListener(delegate (BaseEventData eventData)
			{
				EditorPatch.IsOverMainTimeline = true;
			});

			EventTrigger.Entry entry2 = new EventTrigger.Entry();
			entry2.eventID = EventTriggerType.PointerExit;
			entry2.callback.AddListener(delegate (BaseEventData eventData)
			{
				EditorPatch.IsOverMainTimeline = false;
			});

			EventTrigger tltrig = EditorManager.inst.timeline.GetComponent<EventTrigger>();

			tltrig.triggers.Add(entry);
			tltrig.triggers.Add(entry2);
			tltrig.triggers.Add(Triggers.EndDragTrigger());

			if (DataManager.inst != null)
			{
				for (int i = 0; i < EventEditor.inst.EventHolders.transform.childCount - 1; i++)
				{
					var et = EventEditor.inst.EventHolders.transform.GetChild(i).GetComponent<EventTrigger>();
					et.triggers.Clear();
					et.triggers.Add(entry);
					et.triggers.Add(entry2);
					et.triggers.Add(Triggers.StartDragTrigger());
					et.triggers.Add(Triggers.DragTrigger());
					et.triggers.Add(Triggers.EndDragTrigger());

					//if (et.triggers.Count > 3)
					//{
					//	et.triggers.RemoveAt(3);
					//}

					int typeTmp = i;
					EventTrigger.Entry entry3 = new EventTrigger.Entry();
					entry3.eventID = EventTriggerType.PointerDown;
					entry3.callback.AddListener(delegate (BaseEventData eventData)
					{
						Debug.LogFormat("{0}EventHolder: {1}\nActual Event: {2}", EditorPlugin.className, typeTmp, typeTmp + 14);
						if (((PointerEventData)eventData).button == PointerEventData.InputButton.Right)
						{
							if (EventEditorPatch.eventLayer == 0)
							{
								if (DataManager.inst.gameData.eventObjects.allEvents.Count > typeTmp)
								{
									EventEditor.inst.NewKeyframeFromTimeline(typeTmp);
								}
							}
							if (EventEditorPatch.eventLayer == 1)
							{
								if (DataManager.inst.gameData.eventObjects.allEvents.Count > typeTmp + 14)
								{
									EventEditor.inst.NewKeyframeFromTimeline(typeTmp + 14);
								}
							}
						}
					});
					et.triggers.Add(entry3);
				}
			}

			yield break;
		}

		public static IEnumerator StartEditorGUI()
		{
			yield return new WaitForSeconds(0.2f);
			//EditorGUI.CreateEditorGUI();
			//EditorGUI.UpdateEditorGUI();
		}

		public static IEnumerator CreatePropertiesWindow()
		{
			yield return new WaitForSeconds(2f);
			GameObject editorProperties = Instantiate(EditorManager.inst.GetDialog("Object Selector").Dialog.gameObject);
			editorProperties.name = "Editor Properties Popup";
			editorProperties.layer = 5;
			editorProperties.transform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/Popups").transform);
			editorProperties.transform.localScale = Vector3.one;
			editorProperties.transform.localPosition = Vector3.zero;

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
					//categoryColors.Add(LSColors.HexToColor("FFE7E7"));

					ColorBlock cb2 = button.colors;
					cb2.normalColor = new Color(1f, 1f, 1f, 1f);
					cb2.pressedColor = new Color(1.5f, 1.5f, 1.5f, 1f);
					cb2.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
					cb2.selectedColor = new Color(1f, 1f, 1f, 1f);
					button.colors = cb2;

					HoverTooltip hoverTooltip = gameObject.GetComponent<HoverTooltip>();

					hoverTooltip.tooltipLangauges.Clear();
					hoverTooltip.tooltipLangauges.Add(Triggers.NewTooltip("General Editor Settings", ""));

					button.onClick.ClearAll();
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
					//categoryColors.Add(LSColors.HexToColor("C0ACE1"));

					ColorBlock cb2 = button.colors;
					cb2.normalColor = new Color(1f, 1f, 1f, 1f);
					cb2.pressedColor = new Color(1.5f, 1.5f, 1.5f, 1f);
					cb2.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
					cb2.selectedColor = new Color(1f, 1f, 1f, 1f);
					button.colors = cb2;

					HoverTooltip hoverTooltip = gameObject.GetComponent<HoverTooltip>();

					hoverTooltip.tooltipLangauges.Clear();
					hoverTooltip.tooltipLangauges.Add(Triggers.NewTooltip("Timeline Settings", ""));

					button.onClick.ClearAll();
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
					//categoryColors.Add(LSColors.HexToColor("F17BB8"));

					ColorBlock cb2 = button.colors;
					cb2.normalColor = new Color(1f, 1f, 1f, 1f);
					cb2.pressedColor = new Color(1.5f, 1.5f, 1.5f, 1f);
					cb2.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
					cb2.selectedColor = new Color(1f, 1f, 1f, 1f);
					button.colors = cb2;

					HoverTooltip hoverTooltip = gameObject.GetComponent<HoverTooltip>();

					hoverTooltip.tooltipLangauges.Clear();
					hoverTooltip.tooltipLangauges.Add(Triggers.NewTooltip("Data Settings", ""));

					button.onClick.ClearAll();
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
					//categoryColors.Add(LSColors.HexToColor("2F426D"));

					ColorBlock cb2 = button.colors;
					cb2.normalColor = new Color(1f, 1f, 1f, 1f);
					cb2.pressedColor = new Color(1.5f, 1.5f, 1.5f, 1f);
					cb2.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
					cb2.selectedColor = new Color(1f, 1f, 1f, 1f);
					button.colors = cb2;

					HoverTooltip hoverTooltip = gameObject.GetComponent<HoverTooltip>();

					hoverTooltip.tooltipLangauges.Clear();
					hoverTooltip.tooltipLangauges.Add(Triggers.NewTooltip("GUI Settings", ""));

					button.onClick.ClearAll();
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

				//Functions
				{
					GameObject gameObject = Instantiate(prefabTMP);
					gameObject.name = "functions";
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
					//categoryColors.Add(LSColors.HexToColor("4076DF"));

					ColorBlock cb2 = button.colors;
					cb2.normalColor = new Color(1f, 1f, 1f, 1f);
					cb2.pressedColor = new Color(1.5f, 1.5f, 1.5f, 1f);
					cb2.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
					cb2.selectedColor = new Color(1f, 1f, 1f, 1f);
					button.colors = cb2;

					HoverTooltip hoverTooltip = gameObject.GetComponent<HoverTooltip>();

					hoverTooltip.tooltipLangauges.Clear();
					hoverTooltip.tooltipLangauges.Add(Triggers.NewTooltip("Functions Settings", ""));

					button.onClick.ClearAll();
					button.onClick.AddListener(delegate ()
					{
						currentCategory = EditorProperty.EditorPropCategory.Functions;
						RenderPropertiesWindow();
					});

					GameObject textGameObject = gameObject.transform.GetChild(0).gameObject;
					textGameObject.transform.SetParent(gameObject.transform);
					textGameObject.layer = 5;
					RectTransform textRectTransform = textGameObject.GetComponent<RectTransform>();
					Text textText = textGameObject.GetComponent<Text>();

					textRectTransform.anchoredPosition = Vector2.zero;
					textText.text = "Functions";
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
					//categoryColors.Add(LSColors.HexToColor("6CCBCF"));

					ColorBlock cb2 = button.colors;
					cb2.normalColor = new Color(1f, 1f, 1f, 1f);
					cb2.pressedColor = new Color(1.5f, 1.5f, 1.5f, 1f);
					cb2.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
					cb2.selectedColor = new Color(1f, 1f, 1f, 1f);
					button.colors = cb2;

					HoverTooltip hoverTooltip = gameObject.GetComponent<HoverTooltip>();

					hoverTooltip.tooltipLangauges.Clear();
					hoverTooltip.tooltipLangauges.Add(Triggers.NewTooltip("Fields Settings", ""));

					button.onClick.ClearAll();
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
					//categoryColors.Add(LSColors.HexToColor("1B1B1C"));

					ColorBlock cb2 = button.colors;
					cb2.normalColor = new Color(1f, 1f, 1f, 1f);
					cb2.pressedColor = new Color(1.5f, 1.5f, 1.5f, 1f);
					cb2.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
					cb2.selectedColor = new Color(1f, 1f, 1f, 1f);
					button.colors = cb2;

					HoverTooltip hoverTooltip = gameObject.GetComponent<HoverTooltip>();

					hoverTooltip.tooltipLangauges.Clear();
					hoverTooltip.tooltipLangauges.Add(Triggers.NewTooltip("Preview Settings", ""));

					button.onClick.ClearAll();
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

			var propWinButton = propWin.GetComponent<Button>();
			propWinButton.onClick.ClearAll();
			propWinButton.onClick.AddListener(delegate ()
			{
				OpenPropertiesWindow();
			});

			propWin.SetActive(true);


			string jpgFileLocation = "BepInEx/plugins/Assets/editor_gui_preferences-white.png";

			if (RTFile.FileExists(jpgFileLocation))
			{
				Image spriteReloader = propWin.transform.Find("Image").GetComponent<Image>();

				EditorManager.inst.StartCoroutine(EditorManager.inst.GetSprite(RTFile.ApplicationDirectory + jpgFileLocation, new EditorManager.SpriteLimits(), delegate (Sprite cover)
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
				Triggers.AddEditorDialog("Editor Properties Popup", editorProperties);
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
				EditorManager.inst.HideDialog("Editor Properties Popup");
			}
		}

		public static void ScaleTabs(int _tab)
		{
			var editorDialog = EditorManager.inst.GetDialog("Editor Properties Popup").Dialog;
			for (int i = 0; i < editorDialog.Find("crumbs").childCount; i++)
			{
				//var col = editorDialog.Find("crumbs").GetChild(i).GetComponent<Image>().color;
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

		public static List<Color> categoryColors = new List<Color>
		{
			LSColors.HexToColor("FFE7E7"),
			LSColors.HexToColor("C0ACE1"),
			LSColors.HexToColor("F17BB8"),
			LSColors.HexToColor("2F426D"),
			LSColors.HexToColor("4076DF"),
			LSColors.HexToColor("6CCBCF"),
			LSColors.HexToColor("1B1B1C")

		};

		public static float SnapToBPM(float _time)
		{
			return Mathf.RoundToInt(_time / (SettingEditor.inst.BPMMulti / ConfigEntries.BPMSnapDivisions.Value)) * (SettingEditor.inst.BPMMulti / ConfigEntries.BPMSnapDivisions.Value);
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

			ScaleTabs((int)currentCategory);

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
								Destroy(bar.GetComponent<EventTrigger>());

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
								Destroy(x.GetComponent<EventTrigger>());
								Destroy(x.GetComponent<InputField>());

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

								var input = x.transform.Find("input");

								var xif = input.gameObject.AddComponent<InputField>();
                                xif.onValueChanged.RemoveAllListeners();
								xif.textComponent = input.Find("Text").GetComponent<Text>();
								xif.placeholder = input.Find("Placeholder").GetComponent<Text>();
                                xif.characterValidation = InputField.CharacterValidation.Integer;
								xif.text = prop.configEntry.BoxedValue.ToString();
								xif.onValueChanged.AddListener(delegate (string _val)
								{
									prop.configEntry.BoxedValue = int.Parse(_val);
								});

								if (prop.configEntry.Description.AcceptableValues != null)
								{
									int min = int.MinValue;
									int max = int.MaxValue;
									min = (int)prop.configEntry.Description.AcceptableValues.Clamp(min);
									max = (int)prop.configEntry.Description.AcceptableValues.Clamp(max);

									List<int> clamp = new List<int> { min, max };

									Triggers.AddEventTrigger(xif.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDeltaInt(xif, 1, false, clamp) });

									Triggers.IncreaseDecreaseButtonsInt(xif, 1, x.transform, clamp);
								}
								else
								{
									Triggers.AddEventTrigger(xif.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDeltaInt(xif, 1) });

									Triggers.IncreaseDecreaseButtonsInt(xif, 1, x.transform);
								}

								break;
							}
						case EditorProperty.ValueType.Float:
							{
								GameObject x = Instantiate(singleInput);
								x.transform.SetParent(editorDialog.Find("mask/content"));
								x.name = "input [FLOAT]";

								Destroy(x.GetComponent<EventInfo>());
								Destroy(x.GetComponent<EventTrigger>());
								Destroy(x.GetComponent<InputField>());

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

								var input = x.transform.Find("input");

								var xif = input.gameObject.AddComponent<InputField>();
								xif.onValueChanged.RemoveAllListeners();
								xif.textComponent = input.Find("Text").GetComponent<Text>();
								xif.placeholder = input.Find("Placeholder").GetComponent<Text>();
								xif.characterValidation = InputField.CharacterValidation.None;
								xif.text = prop.configEntry.BoxedValue.ToString();
								xif.onValueChanged.AddListener(delegate (string _val)
								{
									prop.configEntry.BoxedValue = float.Parse(_val);
								});

								if (prop.configEntry.Description.AcceptableValues != null)
								{
									float min = float.MinValue;
									float max = float.MaxValue;
									min = (float)prop.configEntry.Description.AcceptableValues.Clamp(min);
									max = (float)prop.configEntry.Description.AcceptableValues.Clamp(max);

									List<float> clamp = new List<float> { min, max };

									Triggers.AddEventTrigger(xif.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(xif, 0.1f, 10f, false, clamp) });

									Triggers.IncreaseDecreaseButtons(xif, 1f, 10f, x.transform, clamp);
								}
								else
								{
									Triggers.AddEventTrigger(xif.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(xif, 0.1f, 10f) });

									Triggers.IncreaseDecreaseButtons(xif, 1f, 10f, x.transform);
								}

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

								if (prop.configEntry.Description.AcceptableValues != null)
								{
									int min = int.MinValue;
									int max = int.MaxValue;
									min = (int)prop.configEntry.Description.AcceptableValues.Clamp(min);
									max = (int)prop.configEntry.Description.AcceptableValues.Clamp(max);

									List<int> clamp = new List<int> { min, max };

									Triggers.AddEventTrigger(xif.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDeltaInt(xif, 1, false, clamp) });

									Triggers.IncreaseDecreaseButtonsInt(xif, 1, x.transform, clamp);
								}
								else
								{
									Triggers.AddEventTrigger(xif.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDeltaInt(xif, 1) });

									Triggers.IncreaseDecreaseButtonsInt(xif, 1, x.transform);
								}

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

								if (prop.configEntry.Description.AcceptableValues != null)
								{
									float min = float.MinValue;
									float max = float.MaxValue;
									min = (float)prop.configEntry.Description.AcceptableValues.Clamp(min);
									max = (float)prop.configEntry.Description.AcceptableValues.Clamp(max);

									List<float> clamp = new List<float> { min, max };

									Triggers.AddEventTrigger(xif.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(xif, 0.1f, 10f, false, clamp) });

									Triggers.IncreaseDecreaseButtons(xif, 1f, 10f, x.transform, clamp);
								}
								else
								{
									Triggers.AddEventTrigger(xif.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(xif, 0.1f, 10f) });

									Triggers.IncreaseDecreaseButtons(xif, 1f, 10f, x.transform);
								}

								break;
							}
						case EditorProperty.ValueType.String:
							{
								var bar = Instantiate(singleInput);

								Destroy(bar.GetComponent<EventInfo>());
								Destroy(bar.GetComponent<EventTrigger>());
								Destroy(bar.GetComponent<InputField>());
								Destroy(bar.GetComponent<InputFieldHelper>());

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

								Destroy(bar.GetComponent<EventInfo>());
								Destroy(bar.GetComponent<EventTrigger>());
								Destroy(bar.GetComponent<InputField>());
								Destroy(bar.GetComponent<InputFieldHelper>());

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

								Triggers.AddEventTrigger(vxif.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(vxif, 0.1f, 10f) });
								Triggers.AddEventTrigger(vyif.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(vyif, 0.1f, 10f) });

								Triggers.IncreaseDecreaseButtons(vxif, 1f, 10f);
								Triggers.IncreaseDecreaseButtons(vyif, 1f, 10f);

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

								Destroy(bar.GetComponent<EventInfo>());
								Destroy(bar.GetComponent<EventTrigger>());
								Destroy(bar.GetComponent<InputField>());
								Destroy(bar.GetComponent<InputFieldHelper>());

								LSHelpers.DeleteChildren(bar.transform);
								bar.transform.SetParent(editorDialog.Find("mask/content"));
								bar.transform.localScale = Vector3.one;
								bar.name = "input [ENUM]";

								Triggers.AddTooltip(bar, prop.name, prop.description + " (You may see some Invalid Values, don't worry nothing's wrong.)", new List<string> { prop.configEntry.BoxedValue.GetType().ToString() });

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
								xRT.anchoredPosition = ConfigEntries.OpenFileDropdownPosition.Value;

								Destroy(x.GetComponent<HoverTooltip>());
								Destroy(x.GetComponent<HideDropdownOptions>());

								var hide = x.AddComponent<HideDropdownOptions>();

								Dropdown dropdown = x.GetComponent<Dropdown>();
								dropdown.options.Clear();
								dropdown.onValueChanged.RemoveAllListeners();
								Type type = prop.configEntry.SettingType;

								var enums = Enum.GetValues(prop.configEntry.SettingType);
								for (int i = 0; i < enums.Length; i++)
								{
									var str = "Invalid Value";
									if (Enum.GetName(prop.configEntry.SettingType, i) != null)
									{
										hide.DisabledOptions.Add(false);
										str = Enum.GetName(prop.configEntry.SettingType, i);
									}
									else
                                    {
										hide.DisabledOptions.Add(true);
                                    }

									dropdown.options.Add(new Dropdown.OptionData(str));
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

								Destroy(bar.GetComponent<EventInfo>());
								Destroy(bar.GetComponent<EventTrigger>());
								Destroy(bar.GetComponent<InputField>());
								Destroy(bar.GetComponent<InputFieldHelper>());

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

								Image image2 = null;

								if (EventEditor.inst.dialogLeft.TryFind("theme/theme/viewport/content/gui/preview/dropper", out Transform dropper))
                                {
									var drop = Instantiate(dropper.gameObject);
									drop.transform.SetParent(bar2.transform);
									drop.transform.localScale = Vector3.one;
									drop.name = "dropper";

									var dropRT = drop.GetComponent<RectTransform>();
									dropRT.sizeDelta = new Vector2(32f, 32f);
									dropRT.anchoredPosition = Vector2.zero;

									if (drop.TryGetComponent(out Image image))
                                    {
										image2 = image;
										image.color = Triggers.InvertColorHue(Triggers.InvertColorValue((Color)prop.configEntry.BoxedValue));
                                    }
                                }

								GameObject x = Instantiate(stringInput);
								x.transform.SetParent(bar.transform);
								x.transform.localScale = Vector3.one;
								Destroy(x.GetComponent<HoverTooltip>());

								x.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(366f, 32f);

								var xif = x.GetComponent<InputField>();
								xif.onValueChanged.RemoveAllListeners();
								xif.characterValidation = InputField.CharacterValidation.None;
								xif.characterLimit = 8;
								xif.text = ColorToHex((Color)prop.configEntry.BoxedValue);
								xif.textComponent.fontSize = 18;
								xif.onValueChanged.AddListener(delegate (string _val)
								{
									if (xif.text.Length == 8)
									{
										prop.configEntry.BoxedValue = LSColors.HexToColorAlpha(_val);
										bar2Color.color = (Color)prop.configEntry.BoxedValue;
										if (image2 != null)
                                        {
											image2.color = Triggers.InvertColorHue(Triggers.InvertColorValue((Color)prop.configEntry.BoxedValue));
										}

										Triggers.AddEventTrigger(bar2, new List<EventTrigger.Entry> { Triggers.CreatePreviewClickTrigger(bar2Color, image2, xif, (Color)prop.configEntry.BoxedValue, "Editor Properties Popup") });
									}
								});

								Triggers.AddEventTrigger(bar2, new List<EventTrigger.Entry> { Triggers.CreatePreviewClickTrigger(bar2Color, image2, xif, (Color)prop.configEntry.BoxedValue, "Editor Properties Popup") });

								break;
							}
						case EditorProperty.ValueType.Function:
							{
								GameObject x = Instantiate(singleInput);
								x.transform.SetParent(editorDialog.Find("mask/content"));
								x.name = "input [FUNCTION]";

								Destroy(x.GetComponent<EventInfo>());
								Destroy(x.GetComponent<EventTrigger>());
								Destroy(x.GetComponent<InputField>());

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

								Destroy(x.transform.Find("input").gameObject);

								var button = x.AddComponent<Button>();
								button.onClick.AddListener(delegate ()
								{
									prop.action();
								});

								break;
							}
					}
				}
			}
		}

		public static void ModifyTimelineBar(EditorManager __instance)
		{
			timelineBar = GameObject.Find("TimelineBar/GameObject");

			timelineBar.transform.GetChild(0).gameObject.name = "Time Default";

			Debug.LogFormat("{0}Removing unused object", EditorPlugin.className);
			var t = timelineBar.transform.Find("Time");
			defaultIF = t.gameObject;
			defaultIF.SetActive(true);
			t.SetParent(null);
			__instance.speedText.transform.parent.SetParent(null);

			if (defaultIF.TryGetComponent(out InputField frick))
            {
				frick.textComponent.fontSize = 19;
            }

			Debug.LogFormat("{0}Instantiating new time object", EditorPlugin.className);
			var timeObj = Instantiate(t.gameObject);
			{
				timeObj.transform.SetParent(timelineBar.transform);
				timeObj.transform.localScale = Vector3.one;
				timeObj.name = "Time Input";

				//timelineBar.transform.GetChild(0).gameObject.SetActive(true);
				timeIF = timeObj.GetComponent<InputField>();

				Triggers.AddTooltip(timeObj, "Shows the exact current time of song.", "Type in the input field to go to a precise time in the level.");

				timeObj.transform.SetAsFirstSibling();
				timeObj.SetActive(true);
				timeIF.text = AudioManager.inst.CurrentAudioSource.time.ToString();
				timeIF.characterValidation = InputField.CharacterValidation.Decimal;

				timeIF.onValueChanged.AddListener(delegate (string _value)
				{
					SetNewTime(_value);
				});
			}

			Debug.LogFormat("{0}Instantiating new layer object", EditorPlugin.className);
			var layersObj = Instantiate(timeObj);
			{
				layersObj.transform.SetParent(timelineBar.transform);
				layersObj.name = "layers";
				layersObj.transform.SetSiblingIndex(8);
				layersObj.transform.localScale = Vector3.one;

				for (int i = 0; i < layersObj.transform.childCount; i++)
				{
					layersObj.transform.GetChild(i).localScale = Vector3.one;
				}
				layersObj.GetComponent<HoverTooltip>().tooltipLangauges.Add(Triggers.NewTooltip("Input any positive number to go to that editor layer.", "Layers will only show specific objects that are on that layer. Can be good to use for organizing levels.", new List<string> { "Middle Mouse Button" }));

				layersIF = layersObj.GetComponent<InputField>();
				layersObj.transform.Find("Text").gameObject.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;

				layersIF.text = GetLayerString(EditorManager.inst.layer);

				var layerImage = layersObj.GetComponent<Image>();

				layersIF.characterValidation = InputField.CharacterValidation.None;
				layersIF.contentType = InputField.ContentType.Standard;
				layersIF.onValueChanged.RemoveAllListeners();
				layersIF.onValueChanged.AddListener(delegate (string _value)
				{
					if (int.TryParse(_value, out int num))
					{
						if (num > 0)
						{
							if (num < 6)
							{
								SetLayer(num - 1);
							}
							else
							{
								SetLayer(num);
							}
						}
						else
						{
							SetLayer(0);
							layersIF.text = "1";
						}

						layerImage.color = GetLayerColor(num);
					}
				});

				layerImage.color = GetLayerColor(EditorManager.inst.layer);

				Triggers.AddEventTrigger(layersObj, new List<EventTrigger.Entry> { Triggers.ScrollDeltaInt(layersIF, 1, false, new List<int> { 1, int.MaxValue }) });

				var entry = new EventTrigger.Entry();
				entry.eventID = EventTriggerType.Scroll;
				entry.callback.AddListener(delegate (BaseEventData eventData)
				{
					PointerEventData pointerEventData = (PointerEventData)eventData;

					if (layersIF.text == "E")
                    {
						layersIF.text = "1";
                    }
				});

				Triggers.AddEventTrigger(layersObj, new List<EventTrigger.Entry> { entry }, false);
			}

			Debug.LogFormat("{0}Instantiating new pitch object", EditorPlugin.className);
			var pitchObj = Instantiate(timeObj);
			{
				pitchObj.transform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject").transform);
				pitchObj.transform.SetSiblingIndex(5);
				pitchObj.name = "pitch";
				pitchObj.transform.localScale = Vector3.one;

				pitchIF = pitchObj.GetComponent<InputField>();
				pitchIF.onValueChanged.RemoveAllListeners();
				pitchIF.onValueChanged.AddListener(delegate (string _val)
				{
					if (float.TryParse(_val, out float num))
					{
						AudioManager.inst.SetPitch(num);
					}
					else
					{
						EditorManager.inst.DisplayNotification("Input is not correct format!", 1f, EditorManager.NotificationType.Error);
					}
				});

				Triggers.AddEventTrigger(pitchObj, new List<EventTrigger.Entry> { Triggers.ScrollDelta(pitchIF, 0.1f, 10f) });

				Triggers.AddTooltip(pitchObj, "Change the pitch of the song", "", new List<string> { "Up / Down Arrow" }, clear: true);

				pitchObj.GetComponent<LayoutElement>().minWidth = 64f;
				pitchObj.transform.Find("Text").GetComponent<Text>().alignment = TextAnchor.MiddleCenter;

				pitchObj.AddComponent<InputFieldHelper>();
			}

			timelineBar.transform.Find("checkpoint").gameObject.SetActive(false);
		}

		public static void CreateMultiObjectEditor()
		{
			GameObject barButton = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/time").transform.GetChild(4).gameObject;

			GameObject eventButton = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject/event");

			Color bcol = new Color(0.3922f, 0.7098f, 0.9647f, 1f);

			var dataLeft = EditorManager.inst.GetDialog("Multi Object Editor").Dialog.Find("data/left");

			dataLeft.gameObject.SetActive(true);

			GameObject scrollView = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View"));
			scrollView.transform.SetParent(dataLeft);
			scrollView.transform.localScale = Vector3.one;

			LSHelpers.DeleteChildren(scrollView.transform.Find("Viewport/Content"), false);

			scrollView.GetComponent<RectTransform>().sizeDelta = new Vector2(383f, 690f);

			dataLeft.GetChild(1).gameObject.SetActive(true);

			dataLeft.GetChild(1).gameObject.name = "label layer";

			dataLeft.GetChild(1).GetChild(0).gameObject.GetComponent<Text>().text = "Set Group Layer";

			dataLeft.GetChild(3).gameObject.SetActive(true);

			dataLeft.GetChild(3).gameObject.name = "label depth";

			dataLeft.GetChild(3).GetChild(0).gameObject.GetComponent<Text>().text = "Set Group Depth";

			dataLeft.GetChild(1).SetParent(scrollView.transform.Find("Viewport/Content"));

			dataLeft.GetChild(2).SetParent(scrollView.transform.Find("Viewport/Content"));

			var textHolder = EditorManager.inst.GetDialog("Multi Object Editor").Dialog.Find("data/right/text holder/Text");
			textHolder.GetComponent<Text>().text = EditorManager.inst.GetDialog("Multi Object Editor").Dialog.Find("data/right/text holder/Text").GetComponent<Text>().text.Replace("The current version of the editor doesn't support any editing functionality.", "On the left you'll see all the multi object editor tools you'll need.");

			textHolder.GetComponent<Text>().fontSize = 22;

			textHolder.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -125f);

			textHolder.GetComponent<RectTransform>().sizeDelta = new Vector2(-68f, 0f);

			var zoom = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/zoom/zoom");

			//Layers
			{
				GameObject gameObject = Instantiate(zoom);
				gameObject.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				gameObject.name = "layer";
				gameObject.transform.SetSiblingIndex(1);
				gameObject.transform.localScale = Vector3.one;
				gameObject.transform.GetChild(0).Find("input/Placeholder").GetComponent<Text>().text = "Enter layer...";

				gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(428f, 32f);

				if (gameObject.transform.GetChild(0).gameObject.TryGetComponent(out InputField inputField))
				{
					inputField.text = "1";
					Triggers.AddEventTrigger(gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDeltaInt(inputField, 1) });
                }

				GameObject multiLB = Instantiate(gameObject.transform.GetChild(0).Find("<").gameObject);
				multiLB.transform.SetParent(gameObject.transform.GetChild(0));
				multiLB.transform.SetSiblingIndex(2);
				multiLB.name = "|";
				multiLB.transform.localScale = Vector3.one;
				multiLB.GetComponent<Image>().sprite = barButton.GetComponent<Image>().sprite;

				var multiLBB = multiLB.GetComponent<Button>();

				multiLBB.onClick.RemoveAllListeners();
				multiLBB.onClick.AddListener(delegate ()
				{
					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						if (int.Parse(gameObject.transform.GetChild(0).gameObject.GetComponent<InputField>().text) > 0)
						{
							objectSelection.GetObjectData().editorData.Layer = int.Parse(gameObject.transform.GetChild(0).gameObject.GetComponent<InputField>().text) - 1;
						}
						else
						{
							objectSelection.GetObjectData().editorData.Layer = 0;
						}
						ObjEditor.inst.RenderTimelineObject(objectSelection);
					}
				});

				var mlsLeft = gameObject.transform.GetChild(0).Find("<").GetComponent<Button>();
				mlsLeft.onClick.RemoveAllListeners();
				mlsLeft.onClick.AddListener(delegate ()
				{
					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						if (int.Parse(gameObject.transform.GetChild(0).gameObject.GetComponent<InputField>().text) > 0 && objectSelection.GetObjectData().editorData.Layer > 0)
						{
							if (objectSelection.GetObjectData().editorData.Layer != 6)
							{
								objectSelection.GetObjectData().editorData.Layer -= int.Parse(gameObject.transform.GetChild(0).gameObject.GetComponent<InputField>().text);
							}
							else
							{
								objectSelection.GetObjectData().editorData.Layer -= int.Parse(gameObject.transform.GetChild(0).gameObject.GetComponent<InputField>().text) + 1;
							}
						}
						else
						{
							objectSelection.GetObjectData().editorData.Layer = 0;
						}
						ObjEditor.inst.RenderTimelineObject(objectSelection);
					}
				});

				var mlsRight = gameObject.transform.GetChild(0).Find(">").GetComponent<Button>();
				mlsRight.onClick.RemoveAllListeners();
				mlsRight.onClick.AddListener(delegate ()
				{
					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						if (objectSelection.GetObjectData().editorData.Layer != 4)
						{
							objectSelection.GetObjectData().editorData.Layer += int.Parse(gameObject.transform.GetChild(0).gameObject.GetComponent<InputField>().text);
						}
						else
						{
							objectSelection.GetObjectData().editorData.Layer += int.Parse(gameObject.transform.GetChild(0).gameObject.GetComponent<InputField>().text) + 1;
						}
						ObjEditor.inst.RenderTimelineObject(objectSelection);
					}
				});
			}

			//Depth
			{
				GameObject gameObject = Instantiate(zoom);
				gameObject.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				gameObject.name = "depth";
				gameObject.transform.GetChild(0).Find("input/Placeholder").GetComponent<Text>().text = "Enter depth...";
				gameObject.transform.localScale = Vector3.one;

				gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(428f, 32f);

				if (gameObject.transform.GetChild(0).gameObject.TryGetComponent(out InputField inputField))
				{
					inputField.text = "15";
					Triggers.AddEventTrigger(gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDeltaInt(inputField, 1) });
				}

				GameObject multiDB = Instantiate(gameObject.transform.GetChild(0).Find("<").gameObject);
				multiDB.transform.SetParent(gameObject.transform.GetChild(0));
				multiDB.transform.SetSiblingIndex(2);
				multiDB.name = "|";
				multiDB.transform.localScale = Vector3.one;
				multiDB.GetComponent<Image>().sprite = barButton.GetComponent<Image>().sprite;

				var multiDBB = multiDB.GetComponent<Button>();
				multiDBB.onClick.RemoveAllListeners();
				multiDBB.onClick.AddListener(delegate ()
				{
					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						if (objectSelection.IsObject())
						{
							objectSelection.GetObjectData().Depth = objectSelection.GetObjectData().Depth + int.Parse(gameObject.transform.GetChild(0).GetComponent<InputField>().text);
							ObjectManager.inst.updateObjects(objectSelection);
						}
						if (objectSelection.IsPrefab())
						{
							DisplayNotification("MSDP", "Cannot modify the depth of a prefab!", 1f, EditorManager.NotificationType.Error);
						}
					}
				});

				var mdsLeft = gameObject.transform.GetChild(0).Find("<").GetComponent<Button>();
				mdsLeft.onClick.RemoveAllListeners();
				mdsLeft.onClick.AddListener(delegate ()
				{
					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						if (objectSelection.IsObject())
						{
							objectSelection.GetObjectData().Depth -= int.Parse(gameObject.transform.GetChild(0).gameObject.GetComponent<InputField>().text);
							ObjectManager.inst.updateObjects(objectSelection);
						}
						if (objectSelection.IsPrefab())
						{
							DisplayNotification("MSDP", "Cannot modify the depth of a prefab!", 1f, EditorManager.NotificationType.Error);
						}
					}
				});

				var mdsRight = gameObject.transform.GetChild(0).Find(">").GetComponent<Button>();
				mdsRight.onClick.RemoveAllListeners();
				mdsRight.onClick.AddListener(delegate ()
				{
					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						if (objectSelection.IsObject())
						{
							objectSelection.GetObjectData().Depth += int.Parse(gameObject.transform.GetChild(0).gameObject.GetComponent<InputField>().text);
							ObjectManager.inst.updateObjects(objectSelection);
						}
						if (objectSelection.IsPrefab())
						{
							DisplayNotification("MSDP", "Cannot modify the depth of a prefab!", 1f, EditorManager.NotificationType.Error);
						}
					}
				});

				scrollView.transform.Find("Viewport/Content/label layer").SetSiblingIndex(0);
			}

			//Song Time
			{
				GameObject label = Instantiate(scrollView.transform.Find("Viewport/Content/label layer").gameObject);
				label.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				label.transform.GetChild(0).gameObject.GetComponent<Text>().text = "Set Song Time";
				label.name = "label";
				label.transform.localScale = Vector3.one;

				GameObject gameObject = Instantiate(zoom);
				gameObject.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				gameObject.name = "time";
				gameObject.transform.GetChild(0).Find("input/Placeholder").GetComponent<Text>().text = "Enter time...";
				gameObject.transform.localScale = Vector3.one;

				gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(428f, 32f);

				if (gameObject.transform.GetChild(0).gameObject.TryGetComponent(out InputField inputField))
				{
					inputField.text = "0";
					Triggers.AddEventTrigger(gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(inputField, 0.1f, 10f) });
				}

				GameObject multiTB = Instantiate(gameObject.transform.GetChild(0).Find("<").gameObject);
				multiTB.transform.SetParent(gameObject.transform.GetChild(0));
				multiTB.transform.SetSiblingIndex(2);
				multiTB.name = "|";
				multiTB.transform.localScale = Vector3.one;
				multiTB.GetComponent<Image>().sprite = barButton.GetComponent<Image>().sprite;

				var multiTBB = multiTB.GetComponent<Button>();
				multiTBB.onClick.RemoveAllListeners();
				multiTBB.onClick.AddListener(delegate ()
				{
					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						if (objectSelection.IsObject())
						{
							objectSelection.GetObjectData().StartTime = AudioManager.inst.CurrentAudioSource.time;
						}
						if (objectSelection.IsPrefab())
						{
							objectSelection.GetPrefabObjectData().StartTime = AudioManager.inst.CurrentAudioSource.time;
						}

						ObjectManager.inst.updateObjects(objectSelection);
						ObjEditor.inst.RenderTimelineObject(objectSelection);
					}
				});

				var mtsLeft = gameObject.transform.GetChild(0).Find("<").GetComponent<Button>();
				mtsLeft.onClick.RemoveAllListeners();
				mtsLeft.onClick.AddListener(delegate ()
				{
					var list = new List<ObjEditor.ObjectSelection>();
					list = (from x in ObjEditor.inst.selectedObjects
							orderby x.StartTime()
							select x).ToList();

					var st1 = list[0].StartTime();

					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						var st = objectSelection.StartTime();
						if (objectSelection.IsObject())
						{
							objectSelection.GetObjectData().StartTime = AudioManager.inst.CurrentAudioSource.time - st1 + st + float.Parse(gameObject.transform.GetChild(0).gameObject.GetComponent<InputField>().text);
						}
						if (objectSelection.IsPrefab())
						{
							objectSelection.GetPrefabObjectData().StartTime = AudioManager.inst.CurrentAudioSource.time - st1 + st + float.Parse(gameObject.transform.GetChild(0).gameObject.GetComponent<InputField>().text);
						}

						ObjectManager.inst.updateObjects(objectSelection);
						ObjEditor.inst.RenderTimelineObject(objectSelection);
					}
				});

				var mtsRight = gameObject.transform.GetChild(0).Find(">").GetComponent<Button>();
				mtsRight.onClick.RemoveAllListeners();
				mtsRight.onClick.AddListener(delegate ()
				{
					var list = new List<ObjEditor.ObjectSelection>();
					list = (from x in ObjEditor.inst.selectedObjects
							orderby x.StartTime()
							select x).ToList();

					var st1 = list[0].StartTime();

					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						var st = objectSelection.StartTime();
						if (objectSelection.IsObject())
						{
							objectSelection.GetObjectData().StartTime = AudioManager.inst.CurrentAudioSource.time - st1 + st - float.Parse(gameObject.transform.GetChild(0).gameObject.GetComponent<InputField>().text);
						}
						if (objectSelection.IsPrefab())
						{
							objectSelection.GetPrefabObjectData().StartTime = AudioManager.inst.CurrentAudioSource.time - st1 + st - float.Parse(gameObject.transform.GetChild(0).gameObject.GetComponent<InputField>().text);
						}

						ObjectManager.inst.updateObjects(objectSelection);
						ObjEditor.inst.RenderTimelineObject(objectSelection);
					}
				});
			}

			//Name
			{
				GameObject multiTextName = Instantiate(scrollView.transform.Find("Viewport/Content/label layer").gameObject);
				multiTextName.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				multiTextName.transform.GetChild(0).gameObject.GetComponent<Text>().text = "Set Name";
				multiTextName.name = "label";
				multiTextName.transform.localScale = Vector3.one;

				GameObject multiNameSet = Instantiate(zoom);
				multiNameSet.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				multiNameSet.name = "name";
				multiNameSet.transform.GetChild(0).gameObject.GetComponent<InputField>().characterValidation = InputField.CharacterValidation.None;
				multiNameSet.transform.GetChild(0).gameObject.GetComponent<InputField>().characterLimit = 0;
				multiNameSet.transform.GetChild(0).gameObject.GetComponent<InputField>().text = "name";
				multiNameSet.transform.GetChild(0).Find("input/Placeholder").GetComponent<Text>().text = "Enter name...";
				multiNameSet.transform.localScale = Vector3.one;

				multiNameSet.GetComponent<RectTransform>().sizeDelta = new Vector2(428f, 32f);

				GameObject multiNB = Instantiate(multiNameSet.transform.GetChild(0).Find("<").gameObject);
				multiNB.transform.SetParent(multiNameSet.transform.GetChild(0));
				multiNB.transform.SetSiblingIndex(2);
				multiNB.name = "|";
				multiNB.transform.localScale = Vector3.one;
				multiNB.GetComponent<Image>().sprite = barButton.GetComponent<Image>().sprite;

				multiNB.GetComponent<Button>().onClick.RemoveAllListeners();
				multiNB.GetComponent<Button>().onClick.AddListener(delegate ()
				{
					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						if (objectSelection.IsObject())
						{
							objectSelection.GetObjectData().name = multiNameSet.transform.GetChild(0).gameObject.GetComponent<InputField>().text;
							ObjEditor.inst.RenderTimelineObject(objectSelection);
						}
						if (objectSelection.IsPrefab())
						{
							DisplayNotification("MSNP", "Cannot modify the name of a prefab!", 1f, EditorManager.NotificationType.Error);
						}
					}
				});

				Destroy(multiNameSet.transform.GetChild(0).Find("<").gameObject);

				var mtnRight = multiNameSet.transform.GetChild(0).Find(">").GetComponent<Button>();

				string jpgFileLocation = RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/add.png";

				if (RTFile.FileExists("BepInEx/plugins/Assets/add.png"))
				{
					Image spriteReloader = multiNameSet.transform.GetChild(0).Find(">").GetComponent<Image>();

					EditorManager.inst.StartCoroutine(EditorManager.inst.GetSprite(jpgFileLocation, new EditorManager.SpriteLimits(), delegate (Sprite cover)
					{
						spriteReloader.sprite = cover;
					}, delegate (string errorFile)
					{
						spriteReloader.sprite = ArcadeManager.inst.defaultImage;
					}));
				}

				var mtnLeftLE = multiNameSet.transform.GetChild(0).Find(">").gameObject.AddComponent<LayoutElement>();
				mtnLeftLE.ignoreLayout = true;

				var mtnLeftRT = multiNameSet.transform.GetChild(0).Find(">").GetComponent<RectTransform>();
				mtnLeftRT.anchoredPosition = new Vector2(339f, 0f);
				mtnLeftRT.sizeDelta = new Vector2(32f, 32f);

				var mtnRightB = mtnRight.GetComponent<Button>();
				mtnRightB.onClick.RemoveAllListeners();
				mtnRightB.onClick.AddListener(delegate ()
				{
					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						if (objectSelection.IsObject())
						{
							objectSelection.GetObjectData().name += multiNameSet.transform.GetChild(0).gameObject.GetComponent<InputField>().text;
							ObjEditor.inst.RenderTimelineObject(objectSelection);
						}
						if (objectSelection.IsPrefab())
						{
							DisplayNotification("MSNP", "Cannot modify the name of a prefab!", 1f, EditorManager.NotificationType.Error);
						}
					}
				});
			}

			//Song Time Autokill
			{
				var label = Instantiate(scrollView.transform.Find("Viewport/Content/label layer").gameObject);
				label.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				label.transform.GetChild(0).gameObject.GetComponent<Text>().text = "Set Song Time Autokill to Current";
				label.name = "label";
				label.transform.localScale = Vector3.one;

				var gameObject = Instantiate(eventButton);
				gameObject.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				gameObject.name = "set autokill";
				gameObject.transform.localScale = Vector3.one;

				gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(404f, 32f);

				gameObject.transform.GetChild(0).GetComponent<Text>().text = "Set";
				gameObject.GetComponent<Image>().color = bcol;

				var button = gameObject.GetComponent<Button>();
				button.onClick.ClearAll();
				button.onClick.AddListener(delegate ()
				{
					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						if (objectSelection.IsObject())
						{
							objectSelection.GetObjectData().autoKillType = BeatmapObject.AutoKillType.SongTime;
							objectSelection.GetObjectData().autoKillOffset = AudioManager.inst.CurrentAudioSource.time;
							ObjectManager.inst.updateObjects(objectSelection, false);
							ObjEditor.inst.RenderTimelineObject(objectSelection);
						}
						if (objectSelection.IsPrefab())
						{
							DisplayNotification("MSAKP", "Cannot set autokill of a prefab!", 1f, EditorManager.NotificationType.Error);
						}
					}
				});
			}

			//Cycle Object Type
			{
				var label = Instantiate(scrollView.transform.Find("Viewport/Content/label layer").gameObject);
				label.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				label.transform.GetChild(0).gameObject.GetComponent<Text>().text = "Cycle object type";
				label.name = "label";
				label.transform.localScale = Vector3.one;

				var gameObject = Instantiate(eventButton);
				gameObject.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				gameObject.name = "cycle obj type";
				gameObject.transform.localScale = Vector3.one;

				gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(404f, 32f);

				gameObject.transform.GetChild(0).GetComponent<Text>().text = "Cycle";
				gameObject.GetComponent<Image>().color = bcol;

				var button = gameObject.GetComponent<Button>();
				button.onClick.ClearAll();
				button.onClick.AddListener(delegate ()
				{
					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						if (objectSelection.IsObject())
						{
							objectSelection.GetObjectData().objectType += 1;
							if ((int)objectSelection.GetObjectData().objectType > 3)
							{
								objectSelection.GetObjectData().objectType = 0;
							}
							ObjectManager.inst.updateObjects(objectSelection, false);
							ObjEditor.inst.RenderTimelineObject(objectSelection);
						}
						if (objectSelection.IsPrefab())
						{
							DisplayNotification("MSOTP", "Cannot set object type of a prefab!", 1f, EditorManager.NotificationType.Error);
						}
					}
				});
			}

			//Lock Swap
			{
				var label = Instantiate(scrollView.transform.Find("Viewport/Content/label layer").gameObject);
				label.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				label.transform.GetChild(0).gameObject.GetComponent<Text>().text = "Swap each object's lock state";
				label.name = "label";
				label.transform.localScale = Vector3.one;

				var gameObject = Instantiate(eventButton);
				gameObject.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				gameObject.name = "lock swap";
				gameObject.transform.localScale = Vector3.one;

				gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(404f, 32f);

				gameObject.transform.GetChild(0).GetComponent<Text>().text = "Swap Lock";
				gameObject.GetComponent<Image>().color = bcol;

				var button = gameObject.GetComponent<Button>();
				button.onClick.ClearAll();
				button.onClick.AddListener(delegate ()
				{
					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						if (objectSelection.IsObject())
						{
							objectSelection.GetObjectData().editorData.locked = !objectSelection.GetObjectData().editorData.locked;
						}
						if (objectSelection.IsPrefab())
						{
							objectSelection.GetPrefabObjectData().editorData.locked = !objectSelection.GetPrefabObjectData().editorData.locked;
						}

						ObjEditor.inst.RenderTimelineObject(objectSelection);
					}
				});
			}

			//Lock Toggle
			{
				GameObject label = Instantiate(scrollView.transform.Find("Viewport/Content/label layer").gameObject);
				label.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				label.transform.GetChild(0).gameObject.GetComponent<Text>().text = "Toggle all object's lock state";
				label.name = "label";
				label.transform.localScale = Vector3.one;

				GameObject gameObject = Instantiate(eventButton);
				gameObject.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				gameObject.name = "lock toggle";
				gameObject.transform.localScale = Vector3.one;

				gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(404f, 32f);

				gameObject.transform.GetChild(0).GetComponent<Text>().text = "Toggle Lock";
				gameObject.GetComponent<Image>().color = bcol;

				bool loggle = false;

				var button = gameObject.GetComponent<Button>();
				button.onClick.ClearAll();
				button.onClick.AddListener(delegate ()
				{
					loggle = !loggle;
					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						if (objectSelection.IsObject())
						{
							if (loggle == false)
							{
								objectSelection.GetObjectData().editorData.locked = false;
							}
							if (loggle == true)
							{
								objectSelection.GetObjectData().editorData.locked = true;
							}
						}
						if (objectSelection.IsPrefab())
						{
							if (loggle == false)
							{
								objectSelection.GetPrefabObjectData().editorData.locked = false;
							}
							if (loggle == true)
							{
								objectSelection.GetPrefabObjectData().editorData.locked = true;
							}
						}

						ObjEditor.inst.RenderTimelineObject(objectSelection);
					}
				});
			}

			//Collapse Swap
			{
				GameObject label = Instantiate(scrollView.transform.Find("Viewport/Content/label layer").gameObject);
				label.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				label.transform.GetChild(0).gameObject.GetComponent<Text>().text = "Swap each object's collapse state";
				label.name = "label";
				label.transform.localScale = Vector3.one;

				GameObject gameObject = Instantiate(eventButton);
				gameObject.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				gameObject.name = "collapse swap";
				gameObject.transform.localScale = Vector3.one;

				gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(404f, 32f);

				gameObject.transform.GetChild(0).GetComponent<Text>().text = "Swap Collapse";
				gameObject.GetComponent<Image>().color = bcol;

				var button = gameObject.GetComponent<Button>();
				button.onClick.ClearAll();
				button.onClick.AddListener(delegate ()
				{
					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						if (objectSelection.IsObject())
						{
							objectSelection.GetObjectData().editorData.collapse = !objectSelection.GetObjectData().editorData.collapse;
						}
						if (objectSelection.IsPrefab())
						{
							objectSelection.GetPrefabObjectData().editorData.collapse = !objectSelection.GetPrefabObjectData().editorData.collapse;
						}

						ObjEditor.inst.RenderTimelineObject(objectSelection);
					}
				});
			}

			//Collapse Toggle
			{
				GameObject label = Instantiate(scrollView.transform.Find("Viewport/Content/label layer").gameObject);
				label.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				label.transform.GetChild(0).gameObject.GetComponent<Text>().text = "Toggle all object's collapse state";
				label.name = "label";
				label.transform.localScale = Vector3.one;

				GameObject gameObject = Instantiate(eventButton);
				gameObject.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				gameObject.name = "collapse toggle";
				gameObject.transform.localScale = Vector3.one;

				gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(404f, 32f);

				gameObject.transform.GetChild(0).GetComponent<Text>().text = "Toggle Collapse";
				gameObject.GetComponent<Image>().color = bcol;

				bool coggle = false;

				var button = gameObject.GetComponent<Button>();
				button.onClick.ClearAll();
				button.onClick.AddListener(delegate ()
				{
					coggle = !coggle;
					foreach (var objectSelection in ObjEditor.inst.selectedObjects)
					{
						if (objectSelection.IsObject())
						{
							if (coggle == false)
							{
								objectSelection.GetObjectData().editorData.collapse = false;
							}
							if (coggle == true)
							{
								objectSelection.GetObjectData().editorData.collapse = true;
							}
						}
						if (objectSelection.IsPrefab())
						{
							if (coggle == false)
							{
								objectSelection.GetPrefabObjectData().editorData.collapse = false;
							}
							if (coggle == true)
							{
								objectSelection.GetPrefabObjectData().editorData.collapse = true;
							}
						}

						ObjEditor.inst.RenderTimelineObject(objectSelection);
					}
				});
			}

			//Sync object selection
			{
				GameObject multiTextSync = Instantiate(scrollView.transform.Find("Viewport/Content/label layer").gameObject);
				multiTextSync.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				multiTextSync.transform.GetChild(0).gameObject.GetComponent<Text>().text = "Sync to specific object";
				multiTextSync.name = "label";
				multiTextSync.transform.localScale = Vector3.one;

				GameObject multiSync = new GameObject("sync layout");
				multiSync.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
				multiSync.transform.localScale = Vector3.one;

				RectTransform multiSyncRT = multiSync.AddComponent<RectTransform>();
				GridLayoutGroup multiSyncGLG = multiSync.AddComponent<GridLayoutGroup>();
				multiSyncGLG.spacing = new Vector2(4f, 4f);
				multiSyncGLG.cellSize = new Vector2(61.6f, 49f);

				//Start Time
				{
					GameObject gameObject = Instantiate(eventButton);
					gameObject.transform.SetParent(scrollView.transform.Find("Viewport/Content/sync layout"));
					gameObject.name = "start time";
					gameObject.transform.localScale = Vector3.one;

					gameObject.transform.GetChild(0).GetComponent<Text>().text = "ST";
					gameObject.GetComponent<Image>().color = bcol;

					var b = gameObject.GetComponent<Button>();
					b.onClick.ClearAll();
					b.onClick.AddListener(delegate ()
					{
						EditorManager.inst.ShowDialog("Object Search Popup");
						ReSync(true, "startTime", true, true);
					});
				}

				//Name
				{
					GameObject gameObject = Instantiate(eventButton);
					gameObject.transform.SetParent(scrollView.transform.Find("Viewport/Content/sync layout"));
					gameObject.name = "name";
					gameObject.transform.localScale = Vector3.one;

					gameObject.transform.GetChild(0).GetComponent<Text>().text = "N";
					gameObject.GetComponent<Image>().color = bcol;

					var b = gameObject.GetComponent<Button>();
					b.onClick.ClearAll();
					b.onClick.AddListener(delegate ()
					{
						objectData = ObjectData.N;
						EditorManager.inst.ShowDialog("Object Search Popup");
						ReSync(true, "name", true, false);
					});
				}

				//Object Type
				{
					GameObject gameObject = Instantiate(eventButton);
					gameObject.transform.SetParent(scrollView.transform.Find("Viewport/Content/sync layout"));
					gameObject.name = "object type";
					gameObject.transform.localScale = Vector3.one;

					gameObject.transform.GetChild(0).GetComponent<Text>().text = "OT";
					gameObject.GetComponent<Image>().color = bcol;

					var b = gameObject.GetComponent<Button>();
					b.onClick.ClearAll();
					b.onClick.AddListener(delegate ()
					{
						objectData = ObjectData.OT;
						EditorManager.inst.ShowDialog("Object Search Popup");
						ReSync(true, "objectType", true, true);
					});
				}

				//Autokill Type
				{
					GameObject gameObject = Instantiate(eventButton);
					gameObject.transform.SetParent(scrollView.transform.Find("Viewport/Content/sync layout"));
					gameObject.name = "autokill type";
					gameObject.transform.localScale = Vector3.one;

					gameObject.transform.GetChild(0).GetComponent<Text>().text = "AKT";
					gameObject.GetComponent<Image>().color = bcol;

					var b = gameObject.GetComponent<Button>();
					b.onClick.ClearAll();
					b.onClick.AddListener(delegate ()
					{
						objectData = ObjectData.AKT;
						EditorManager.inst.ShowDialog("Object Search Popup");
						ReSync(true, "autoKillType", true, true);
					});
				}

				//Autokill Offset
				{
					GameObject gameObject = Instantiate(eventButton);
					gameObject.transform.SetParent(scrollView.transform.Find("Viewport/Content/sync layout"));
					gameObject.name = "autokill offset";
					gameObject.transform.localScale = Vector3.one;

					gameObject.transform.GetChild(0).GetComponent<Text>().text = "AKO";
					gameObject.GetComponent<Image>().color = bcol;

					var b = gameObject.GetComponent<Button>();
					b.onClick.ClearAll();
					b.onClick.AddListener(delegate ()
					{
						objectData = ObjectData.AKO;
						EditorManager.inst.ShowDialog("Object Search Popup");
						ReSync(true, "autoKillOffset", true, true);
					});
				}

				//Parent
				{
					GameObject gameObject = Instantiate(eventButton);
					gameObject.transform.SetParent(scrollView.transform.Find("Viewport/Content/sync layout"));
					gameObject.name = "parent";
					gameObject.transform.localScale = Vector3.one;

					gameObject.transform.GetChild(0).GetComponent<Text>().text = "P";
					gameObject.GetComponent<Image>().color = bcol;

					var b = gameObject.GetComponent<Button>();
					b.onClick.ClearAll();
					b.onClick.AddListener(delegate ()
					{
						objectData = ObjectData.P;
						EditorManager.inst.ShowDialog("Object Search Popup");
						ReSync(true, "parent", false, true);
					});
				}

				//Parent Type
				{
					GameObject gameObject = Instantiate(eventButton);
					gameObject.transform.SetParent(scrollView.transform.Find("Viewport/Content/sync layout"));
					gameObject.name = "parent type";
					gameObject.transform.localScale = Vector3.one;

					gameObject.transform.GetChild(0).GetComponent<Text>().text = "PT";
					gameObject.GetComponent<Image>().color = bcol;

					var b = gameObject.GetComponent<Button>();
					b.onClick.ClearAll();
					b.onClick.AddListener(delegate ()
					{
						objectData = ObjectData.PT;
						EditorManager.inst.ShowDialog("Object Search Popup");
						ReSync(true, "parentType", false, true);
					});
				}

				//Parent Offset
				{
					GameObject gameObject = Instantiate(eventButton);
					gameObject.transform.SetParent(scrollView.transform.Find("Viewport/Content/sync layout"));
					gameObject.name = "parent offset";
					gameObject.transform.localScale = Vector3.one;

					gameObject.transform.GetChild(0).GetComponent<Text>().text = "PO";
					gameObject.GetComponent<Image>().color = bcol;

					var b = gameObject.GetComponent<Button>();
					b.onClick.ClearAll();
					b.onClick.AddListener(delegate ()
					{
						objectData = ObjectData.PO;
						EditorManager.inst.ShowDialog("Object Search Popup");
						ReSync(true, "parentOffset", false, true);
					});
				}

				//Origin
				{
					GameObject gameObject = Instantiate(eventButton);
					gameObject.transform.SetParent(scrollView.transform.Find("Viewport/Content/sync layout"));
					gameObject.name = "origin";
					gameObject.transform.localScale = Vector3.one;

					gameObject.transform.GetChild(0).GetComponent<Text>().text = "O";
					gameObject.GetComponent<Image>().color = bcol;

					var b = gameObject.GetComponent<Button>();
					b.onClick.ClearAll();
					b.onClick.AddListener(delegate ()
					{
						objectData = ObjectData.O;
						EditorManager.inst.ShowDialog("Object Search Popup");
						ReSync(true, "origin", false, true);
					});
				}

				//Shape
				{
					GameObject gameObject = Instantiate(eventButton);
					gameObject.transform.SetParent(scrollView.transform.Find("Viewport/Content/sync layout"));
					gameObject.name = "shape";
					gameObject.transform.localScale = Vector3.one;

					gameObject.transform.GetChild(0).GetComponent<Text>().text = "S";
					gameObject.GetComponent<Image>().color = bcol;

					var b = gameObject.GetComponent<Button>();
					b.onClick.ClearAll();
					b.onClick.AddListener(delegate ()
					{
						objectData = ObjectData.S;
						EditorManager.inst.ShowDialog("Object Search Popup");
						ReSync(true, "shape", false, true);
					});
				}

				//Text
				{
					GameObject gameObject = Instantiate(eventButton);
					gameObject.transform.SetParent(scrollView.transform.Find("Viewport/Content/sync layout"));
					gameObject.name = "text";
					gameObject.transform.localScale = Vector3.one;

					gameObject.transform.GetChild(0).GetComponent<Text>().text = "T";
					gameObject.GetComponent<Image>().color = bcol;

					var b = gameObject.GetComponent<Button>();
					b.onClick.ClearAll();
					b.onClick.AddListener(delegate ()
					{
						objectData = ObjectData.T;
						EditorManager.inst.ShowDialog("Object Search Popup");
						ReSync(true, "text", false, true);
					});
				}

				//Depth
				{
					GameObject gameObject = Instantiate(eventButton);
					gameObject.transform.SetParent(scrollView.transform.Find("Viewport/Content/sync layout"));
					gameObject.name = "depth";
					gameObject.transform.localScale = Vector3.one;

					gameObject.transform.GetChild(0).GetComponent<Text>().text = "D";
					gameObject.GetComponent<Image>().color = bcol;

					var b = gameObject.GetComponent<Button>();
					b.onClick.ClearAll();
					b.onClick.AddListener(delegate ()
					{
						objectData = ObjectData.D;
						EditorManager.inst.ShowDialog("Object Search Popup");
						ReSync(true, "depth", false, true);
					});
				}

				//ISSUE: Causes newly selected objects to retain the values of the previous object for some reason
				//GameObject syncKeyframes = Instantiate(eventButton);
				//syncKeyframes.transform.SetParent(scrollView.transform.Find("Viewport/Content/sync layout"));

				//syncKeyframes.transform.GetChild(0).GetComponent<Text>().text = "KF";
				//syncKeyframes.GetComponent<Image>().color = bcol;

				//syncKeyframes.GetComponent<Button>().onClick.m_Calls.m_ExecutingCalls.Clear();
				//syncKeyframes.GetComponent<Button>().onClick.m_Calls.m_PersistentCalls.Clear();
				//syncKeyframes.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls.Clear();
				//syncKeyframes.GetComponent<Button>().onClick.RemoveAllListeners();
				//syncKeyframes.GetComponent<Button>().onClick.AddListener(delegate ()
				//{
				//	for (int i = 1; i < ObjEditor.inst.selectedObjects.Count; i++)
				//    {
				//		for (int j = 1; j < ObjEditor.inst.selectedObjects[i].GetObjectData().events[0].Count; j++)
				//        {
				//			ObjEditor.inst.selectedObjects[i].GetObjectData().events[0].Clear();
				//			ObjEditor.inst.selectedObjects[i].GetObjectData().events[0].Add(ObjEditor.inst.selectedObjects[0].GetObjectData().events[0][j]);
				//		}
				//		for (int j = 1; j < ObjEditor.inst.selectedObjects[i].GetObjectData().events[1].Count; j++)
				//        {
				//			ObjEditor.inst.selectedObjects[i].GetObjectData().events[1].Clear();
				//			ObjEditor.inst.selectedObjects[i].GetObjectData().events[1].Add(ObjEditor.inst.selectedObjects[0].GetObjectData().events[1][j]);
				//		}
				//		for (int j = 1; j < ObjEditor.inst.selectedObjects[i].GetObjectData().events[2].Count; j++)
				//        {
				//			ObjEditor.inst.selectedObjects[i].GetObjectData().events[2].Clear();
				//			ObjEditor.inst.selectedObjects[i].GetObjectData().events[2].Add(ObjEditor.inst.selectedObjects[0].GetObjectData().events[2][j]);
				//		}
				//		for (int j = 1; j < ObjEditor.inst.selectedObjects[i].GetObjectData().events[3].Count; j++)
				//        {
				//			ObjEditor.inst.selectedObjects[i].GetObjectData().events[3].Clear();
				//			ObjEditor.inst.selectedObjects[i].GetObjectData().events[3].Add(ObjEditor.inst.selectedObjects[0].GetObjectData().events[3][j]);
				//		}
				//
				//		ObjectManager.inst.updateObjects(ObjEditor.inst.selectedObjects[i], false);
				//		ObjEditor.inst.RenderTimelineObject(ObjEditor.inst.selectedObjects[i]);
				//	}
				//});
			}

			EditorManager.inst.GetDialog("Multi Object Editor").Dialog.Find("data").GetComponent<RectTransform>().sizeDelta = new Vector2(810f, 730.11f);
			EditorManager.inst.GetDialog("Multi Object Editor").Dialog.Find("data/left").GetComponent<RectTransform>().sizeDelta = new Vector2(355f, 730f);
		}

		public static void ReSync(bool _multi = false, string _multiValue = "", bool objEditor = false, bool objectManager = false)
		{
			EditorManager.inst.ShowDialog("Object Search Popup");
			var searchBar = EditorManager.inst.GetDialog("Object Search Popup").Dialog.Find("search-box/search").GetComponent<InputField>();
			searchBar.onValueChanged.RemoveAllListeners();
			searchBar.text = searchterm;
			searchBar.onValueChanged.AddListener(delegate (string _value)
			{
				searchterm = _value;
				RefreshObjectSearch(_multi, _multiValue, objEditor, objectManager);
			});
			RefreshObjectSearch(_multi, _multiValue, objEditor, objectManager);
		}

		public static List<LevelFolder<EditorManager.MetadataWrapper>> levelItems = new List<LevelFolder<EditorManager.MetadataWrapper>>();
		
		public static void RenderBeatmapSet()
		{
			levelItems.Clear();

			int foldClamp = ConfigEntries.OpenFileFolderNameMax.Value;
			int songClamp = ConfigEntries.OpenFileSongNameMax.Value;
			int artiClamp = ConfigEntries.OpenFileArtistNameMax.Value;
			int creaClamp = ConfigEntries.OpenFileCreatorNameMax.Value;
			int descClamp = ConfigEntries.OpenFileDescriptionMax.Value;
			int dateClamp = ConfigEntries.OpenFileDateMax.Value;

			if (ConfigEntries.OpenFileFolderNameMax.Value < 3)
			{
				foldClamp = 14;
			}

			if (ConfigEntries.OpenFileSongNameMax.Value < 3)
			{
				songClamp = 22;
			}

			if (ConfigEntries.OpenFileArtistNameMax.Value < 3)
			{
				artiClamp = 16;
			}

			if (ConfigEntries.OpenFileCreatorNameMax.Value < 3)
			{
				creaClamp = 16;
			}

			if (ConfigEntries.OpenFileDescriptionMax.Value < 3)
			{
				descClamp = 16;
			}

			if (ConfigEntries.OpenFileDateMax.Value < 3)
			{
				dateClamp = 16;
			}

            #region Sorting

            //Cover
            if (EditorPlugin.levelFilter == 0 && EditorPlugin.levelAscend == false)
			{
				var result = new List<EditorManager.MetadataWrapper>();
				result = (from x in EditorManager.inst.loadedLevels
						  orderby x.albumArt != EditorManager.inst.AlbumArt descending
						  select x).ToList();

				EditorManager.inst.loadedLevels = result;
			}
			if (EditorPlugin.levelFilter == 0 && EditorPlugin.levelAscend == true)
			{
				var result = new List<EditorManager.MetadataWrapper>();
				result = (from x in EditorManager.inst.loadedLevels
						  orderby x.albumArt != EditorManager.inst.AlbumArt ascending
						  select x).ToList();

				EditorManager.inst.loadedLevels = result;
			}

			//Artist
			if (EditorPlugin.levelFilter == 1 && EditorPlugin.levelAscend == false)
			{
				var result = new List<EditorManager.MetadataWrapper>();
				result = (from x in EditorManager.inst.loadedLevels
						  orderby x.metadata.artist.Name descending
						  select x).ToList();

				EditorManager.inst.loadedLevels = result;
			}
			if (EditorPlugin.levelFilter == 1 && EditorPlugin.levelAscend == true)
			{
				var result = new List<EditorManager.MetadataWrapper>();
				result = (from x in EditorManager.inst.loadedLevels
						  orderby x.metadata.artist.Name ascending
						  select x).ToList();

				EditorManager.inst.loadedLevels = result;
			}

			//Creator
			if (EditorPlugin.levelFilter == 2 && EditorPlugin.levelAscend == false)
			{
				var result = new List<EditorManager.MetadataWrapper>();
				result = (from x in EditorManager.inst.loadedLevels
						  orderby x.metadata.creator.steam_name descending
						  select x).ToList();

				EditorManager.inst.loadedLevels = result;
			}
			if (EditorPlugin.levelFilter == 2 && EditorPlugin.levelAscend == true)
			{
				var result = new List<EditorManager.MetadataWrapper>();
				result = (from x in EditorManager.inst.loadedLevels
						  orderby x.metadata.creator.steam_name ascending
						  select x).ToList();

				EditorManager.inst.loadedLevels = result;
			}

			//Folder
			if (EditorPlugin.levelFilter == 3 && EditorPlugin.levelAscend == false)
			{
				var result = new List<EditorManager.MetadataWrapper>();
				result = (from x in EditorManager.inst.loadedLevels
						  orderby x.folder descending
						  select x).ToList();

				EditorManager.inst.loadedLevels = result;
			}
			if (EditorPlugin.levelFilter == 3 && EditorPlugin.levelAscend == true)
			{
				var result = new List<EditorManager.MetadataWrapper>();
				result = (from x in EditorManager.inst.loadedLevels
						  orderby x.folder ascending
						  select x).ToList();

				EditorManager.inst.loadedLevels = result;
			}

			//Title
			if (EditorPlugin.levelFilter == 4 && EditorPlugin.levelAscend == false)
			{
				var result = new List<EditorManager.MetadataWrapper>();
				result = (from x in EditorManager.inst.loadedLevels
						  orderby x.metadata.song.title descending
						  select x).ToList();

				EditorManager.inst.loadedLevels = result;
			}
			if (EditorPlugin.levelFilter == 4 && EditorPlugin.levelAscend == true)
			{
				var result = new List<EditorManager.MetadataWrapper>();
				result = (from x in EditorManager.inst.loadedLevels
						  orderby x.metadata.song.title ascending
						  select x).ToList();

				EditorManager.inst.loadedLevels = result;
			}

			//Difficulty
			if (EditorPlugin.levelFilter == 5 && EditorPlugin.levelAscend == false)
			{
				var result = new List<EditorManager.MetadataWrapper>();
				result = (from x in EditorManager.inst.loadedLevels
						  orderby x.metadata.song.difficulty descending
						  select x).ToList();

				EditorManager.inst.loadedLevels = result;
			}
			if (EditorPlugin.levelFilter == 5 && EditorPlugin.levelAscend == true)
			{
				var result = new List<EditorManager.MetadataWrapper>();
				result = (from x in EditorManager.inst.loadedLevels
						  orderby x.metadata.song.difficulty ascending
						  select x).ToList();

				EditorManager.inst.loadedLevels = result;
			}

			//Date Edited
			if (EditorPlugin.levelFilter == 6 && EditorPlugin.levelAscend == false)
			{
				var result = new List<EditorManager.MetadataWrapper>();
				result = (from x in EditorManager.inst.loadedLevels
						  orderby x.metadata.beatmap.date_edited descending
						  select x).ToList();

				EditorManager.inst.loadedLevels = result;
			}
			if (EditorPlugin.levelFilter == 6 && EditorPlugin.levelAscend == true)
			{
				var result = new List<EditorManager.MetadataWrapper>();
				result = (from x in EditorManager.inst.loadedLevels
						  orderby x.metadata.beatmap.date_edited ascending
						  select x).ToList();

				EditorManager.inst.loadedLevels = result;
			}

            #endregion

            Transform transform = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("mask").Find("content");
			var close = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("Panel/x");
			foreach (object obj in transform)
			{
				Destroy(((Transform)obj).gameObject);
			}
			foreach (var metadataWrapper in EditorManager.inst.loadedLevels)
			{
				var metadata = metadataWrapper.metadata;
				string name = metadataWrapper.folder;

				string difficultyName = "None";
				if (metadata.song.difficulty == 0)
				{
					difficultyName = "easy";
				}
				if (metadata.song.difficulty == 1)
				{
					difficultyName = "normal";
				}
				if (metadata.song.difficulty == 2)
				{
					difficultyName = "hard";
				}
				if (metadata.song.difficulty == 3)
				{
					difficultyName = "expert";
				}
				if (metadata.song.difficulty == 4)
				{
					difficultyName = "expert+";
				}
				if (metadata.song.difficulty == 5)
				{
					difficultyName = "master";
				}
				if (metadata.song.difficulty == 6)
				{
					difficultyName = "animation";
				}

				if (RTFile.FileExists(EditorPlugin.levelListSlash + metadataWrapper.folder + "/level.ogg"))
				{
					if (EditorManager.inst.openFileSearch == null || !(EditorManager.inst.openFileSearch != "") || name.ToLower().Contains(EditorManager.inst.openFileSearch.ToLower()) || metadata.song.title.ToLower().Contains(EditorManager.inst.openFileSearch.ToLower()) || metadata.artist.Name.ToLower().Contains(EditorManager.inst.openFileSearch.ToLower()) || metadata.creator.steam_name.ToLower().Contains(EditorManager.inst.openFileSearch.ToLower()) || metadata.song.description.ToLower().Contains(EditorManager.inst.openFileSearch.ToLower()) || difficultyName.Contains(EditorManager.inst.openFileSearch.ToLower()))
					{
						GameObject gameObject = Instantiate(EditorManager.inst.folderButtonPrefab);
						gameObject.name = "Folder [" + metadataWrapper.folder + "]";
						gameObject.transform.SetParent(transform);
						gameObject.transform.localScale = Vector3.one;
						var hoverUI = gameObject.AddComponent<HoverUI>();
						hoverUI.size = ConfigEntries.OpenFileButtonHoverSize.Value;
						hoverUI.animatePos = false;
						hoverUI.animateSca = true;
						HoverTooltip htt = gameObject.AddComponent<HoverTooltip>();

						HoverTooltip.Tooltip levelTip = new HoverTooltip.Tooltip();

						if (metadata != null)
						{
							gameObject.transform.GetChild(0).GetComponent<Text>().text = string.Format(ConfigEntries.OpenFileTextFormatting.Value, LSText.ClampString(metadataWrapper.folder, foldClamp), LSText.ClampString(metadata.song.title, songClamp), LSText.ClampString(metadata.artist.Name, artiClamp), LSText.ClampString(metadata.creator.steam_name, creaClamp), metadata.song.difficulty, LSText.ClampString(metadata.song.description, descClamp), LSText.ClampString(metadata.beatmap.date_edited, dateClamp));

							if (metadata.song.difficulty == 4 && ConfigEntries.OpenFileTextInvert.Value == true && ConfigEntries.OpenFileButtonDifficultyColor.Value == true || metadata.song.difficulty == 5 && ConfigEntries.OpenFileTextInvert.Value == true && ConfigEntries.OpenFileButtonDifficultyColor.Value == true)
							{
								gameObject.transform.GetChild(0).GetComponent<Text>().color = LSColors.ChangeColorBrightness(ConfigEntries.OpenFileTextColor.Value, 0.7f);
							}

							Color difficultyColor = Color.white;

							for (int i = 0; i < DataManager.inst.difficulties.Count; i++)
							{
								if (metadata.song.difficulty == i)
								{
									difficultyColor = DataManager.inst.difficulties[i].color;
								}
								if (ConfigEntries.OpenFileButtonDifficultyColor.Value == true)
								{
									gameObject.GetComponent<Image>().color = difficultyColor * ConfigEntries.OpenFileButtonDifficultyMultiply.Value;
								}
							}
							levelTip.desc = "<#" + LSColors.ColorToHex(difficultyColor) + ">" + metadata.artist.Name + " - " + metadata.song.title;
							levelTip.hint = "</color>" + metadata.song.description;
							htt.tooltipLangauges.Add(levelTip);
						}
						else
						{
							gameObject.transform.GetChild(0).GetComponent<Text>().text = string.Format("/{0} : {1}", LSText.ClampString(metadataWrapper.folder, foldClamp), LSText.ClampString("No MetaData File", songClamp));
						}

						gameObject.GetComponent<Button>().onClick.AddListener(delegate ()
						{
							inst.StartCoroutine(LoadLevel(EditorManager.inst, name));
							EditorManager.inst.HideDialog("Open File Popup");

							//if (RTEditor.CompareLastSaved())
							//{
							//	EditorManager.inst.ShowDialog("Warning Popup");
							//	RTEditor.RefreshWarningPopup("You haven't saved! Are you sure you want to exit the level before saving?", delegate ()
							//	{
							//		RTEditor.inst.StartCoroutine(RTEditor.LoadLevel(EditorManager.inst, name));
							//		EditorManager.inst.HideDialog("Open File Popup");
							//		EditorManager.inst.HideDialog("Warning Popup");
							//	}, delegate ()
							//	{
							//		EditorManager.inst.HideDialog("Warning Popup");
							//	});
							//}
							//else
							//{
							//	RTEditor.inst.StartCoroutine(RTEditor.LoadLevel(EditorManager.inst, name));
							//	EditorManager.inst.HideDialog("Open File Popup");
							//}
						});

						GameObject icon = new GameObject("icon");
						icon.transform.SetParent(gameObject.transform);
						icon.transform.localScale = Vector3.one;
						icon.layer = 5;
						RectTransform iconRT = icon.AddComponent<RectTransform>();
						icon.AddComponent<CanvasRenderer>();
						Image iconImage = icon.AddComponent<Image>();

						iconRT.anchoredPosition = ConfigEntries.OpenFileCoverPosition.Value;
						iconRT.sizeDelta = ConfigEntries.OpenFileCoverScale.Value;

						iconImage.sprite = metadataWrapper.albumArt;

						//Close
						if (ConfigEntries.ShowLevelDeleteButton.Value)
						{
							var delete = Instantiate(close.gameObject);
							var deleteTF = delete.transform;
							deleteTF.SetParent(gameObject.transform);
							deleteTF.localScale = Vector3.one;

							delete.GetComponent<RectTransform>().anchoredPosition = new Vector2(-5f, 0f);

							string levelName = metadataWrapper.folder;
							var deleteButton = delete.GetComponent<Button>();
							deleteButton.onClick.ClearAll();
							deleteButton.onClick.AddListener(delegate ()
							{
								EditorManager.inst.ShowDialog("Warning Popup");
								RefreshWarningPopup("Are you sure you want to delete this level? (It will be moved to a recycling folder)", delegate ()
								{
									DeleteLevelFunction(levelName);
									EditorManager.inst.DisplayNotification("Deleted level!", 2f, EditorManager.NotificationType.Success);
									EditorManager.inst.GetLevelList();
									EditorManager.inst.HideDialog("Warning Popup");
								}, delegate ()
								{
									EditorManager.inst.HideDialog("Warning Popup");
								});
							});
						}

						levelItems.Add(new LevelFolder<EditorManager.MetadataWrapper>(metadataWrapper, gameObject, gameObject.GetComponent<RectTransform>(), iconImage));
					}
				}
			}

			if (ModCompatibility.sharedFunctions.ContainsKey("EditorLevelFolders"))
				ModCompatibility.sharedFunctions["EditorLevelFolders"] = levelItems;
			else ModCompatibility.sharedFunctions.Add("EditorLevelFolders", levelItems);
		}

		public static IEnumerator RenderTimeline(float wait = 0.0001f)
        {
			var bpm = SettingEditor.inst.SnapBPM;

			Texture2D texture = null;

			for (int i = 0; i < AudioManager.inst.CurrentAudioSource.clip.length * 100; i++)
            {
				for (int j = 0; j < 300; j++)
                {
					if (i % bpm > bpm - 0.5f && i % bpm < bpm)
						yield return SetPixel(texture, i, j, new Color(1f, 1f, 1f, 0.3f));
					//yield return new WaitForSeconds(wait);
                }
            }

			yield break;
        }

		public static object SetPixel(Texture2D texture, int x, int y, Color color)
		{
			texture.SetPixel(x, y, color);
			return null;
        }

		#endregion

		#region Data

		public static IEnumerator SaveData(string _path, DataManager.GameData _data)
        {
			Debug.Log("Saving Beatmap");
			JSONNode jn = JSON.Parse("{}");
			Debug.Log("Saving Editor Data");
			jn["ed"]["timeline_pos"] = AudioManager.inst.CurrentAudioSource.time.ToString();
			Debug.Log("Saving Markers");
			for (int i = 0; i < _data.beatmapData.markers.Count; i++)
			{
				jn["ed"]["markers"][i]["active"] = "True";
				jn["ed"]["markers"][i]["name"] = _data.beatmapData.markers[i].name.ToString();
				jn["ed"]["markers"][i]["desc"] = _data.beatmapData.markers[i].desc.ToString();
				jn["ed"]["markers"][i]["col"] = _data.beatmapData.markers[i].color.ToString();
				jn["ed"]["markers"][i]["t"] = _data.beatmapData.markers[i].time.ToString();
			}
			Debug.Log("Saving Object Prefabs");
			for (int i = 0; i < _data.prefabObjects.Count; i++)
			{
				jn["prefab_objects"][i]["id"] = _data.prefabObjects[i].ID.ToString();
				jn["prefab_objects"][i]["pid"] = _data.prefabObjects[i].prefabID.ToString();
				jn["prefab_objects"][i]["st"] = _data.prefabObjects[i].StartTime.ToString();

				try
				{
					if (_data.prefabObjects[i].RepeatCount > 0)
						jn["prefab_objects"][i]["rc"] = _data.prefabObjects[i].RepeatCount.ToString();
					if (_data.prefabObjects[i].RepeatOffsetTime > 0f)
						jn["prefab_objects"][i]["ro"] = _data.prefabObjects[i].RepeatOffsetTime.ToString();
				}
				catch (Exception ex)
				{
					Debug.Log($"{EditorPlugin.className}Prefab Object Editor!\nMESSAGE: {ex.Message}\nSTACKTRACE: {ex.StackTrace}");
				}

				if (_data.prefabObjects[i].editorData.locked)
				{
					jn["prefab_objects"][i]["ed"]["locked"] = _data.prefabObjects[i].editorData.locked.ToString();
				}
				if (_data.prefabObjects[i].editorData.collapse)
				{
					jn["prefab_objects"][i]["ed"]["shrink"] = _data.prefabObjects[i].editorData.collapse.ToString();
				}
				jn["prefab_objects"][i]["ed"]["layer"] = _data.prefabObjects[i].editorData.Layer.ToString();
				jn["prefab_objects"][i]["ed"]["bin"] = _data.prefabObjects[i].editorData.Bin.ToString();
				jn["prefab_objects"][i]["e"]["pos"]["x"] = _data.prefabObjects[i].events[0].eventValues[0].ToString();
				jn["prefab_objects"][i]["e"]["pos"]["y"] = _data.prefabObjects[i].events[0].eventValues[1].ToString();
				if (_data.prefabObjects[i].events[0].random != 0)
				{
					jn["prefab_objects"][i]["e"]["pos"]["r"] = _data.prefabObjects[i].events[0].random.ToString();
					jn["prefab_objects"][i]["e"]["pos"]["rx"] = _data.prefabObjects[i].events[0].eventRandomValues[0].ToString();
					jn["prefab_objects"][i]["e"]["pos"]["ry"] = _data.prefabObjects[i].events[0].eventRandomValues[1].ToString();
					jn["prefab_objects"][i]["e"]["pos"]["rz"] = _data.prefabObjects[i].events[0].eventRandomValues[2].ToString();
				}
				jn["prefab_objects"][i]["e"]["sca"]["x"] = _data.prefabObjects[i].events[1].eventValues[0].ToString();
				jn["prefab_objects"][i]["e"]["sca"]["y"] = _data.prefabObjects[i].events[1].eventValues[1].ToString();
				if (_data.prefabObjects[i].events[1].random != 0)
				{
					jn["prefab_objects"][i]["e"]["sca"]["r"] = _data.prefabObjects[i].events[1].random.ToString();
					jn["prefab_objects"][i]["e"]["sca"]["rx"] = _data.prefabObjects[i].events[1].eventRandomValues[0].ToString();
					jn["prefab_objects"][i]["e"]["sca"]["ry"] = _data.prefabObjects[i].events[1].eventRandomValues[1].ToString();
					jn["prefab_objects"][i]["e"]["sca"]["rz"] = _data.prefabObjects[i].events[1].eventRandomValues[2].ToString();
				}
				jn["prefab_objects"][i]["e"]["rot"]["x"] = _data.prefabObjects[i].events[2].eventValues[0].ToString();
				if (_data.prefabObjects[i].events[1].random != 0)
				{
					jn["prefab_objects"][i]["e"]["rot"]["r"] = _data.prefabObjects[i].events[2].random.ToString();
					jn["prefab_objects"][i]["e"]["rot"]["rx"] = _data.prefabObjects[i].events[2].eventRandomValues[0].ToString();
					jn["prefab_objects"][i]["e"]["rot"]["rz"] = _data.prefabObjects[i].events[2].eventRandomValues[2].ToString();
				}
			}
			Debug.Log("Saving Level Data");
			{
				jn["level_data"]["level_version"] = _data.beatmapData.levelData.levelVersion.ToString();
				jn["level_data"]["background_color"] = _data.beatmapData.levelData.backgroundColor.ToString();
				jn["level_data"]["follow_player"] = _data.beatmapData.levelData.followPlayer.ToString();
				jn["level_data"]["show_intro"] = _data.beatmapData.levelData.showIntro.ToString();
				jn["level_data"]["bg_zoom"] = RTHelpers.perspectiveZoom.ToString();
			}
			Debug.Log("Saving prefabs");
			if (DataManager.inst.gameData.prefabs != null)
			{
				for (int i = 0; i < DataManager.inst.gameData.prefabs.Count; i++)
				{
					jn["prefabs"][i] = DataManager.inst.GeneratePrefabJSON(DataManager.inst.gameData.prefabs[i]);
				}
			}
			Debug.Log("Saving themes");
			if (DataManager.inst.CustomBeatmapThemes != null)
            {
                List<DataManager.BeatmapTheme> levelThemes = new List<DataManager.BeatmapTheme>();
				savedBeatmapThemes.Clear();

                for (int i = 0; i < DataManager.inst.CustomBeatmapThemes.Count; i++)
				{
					var beatmapTheme = DataManager.inst.CustomBeatmapThemes[i];

					string id = beatmapTheme.id;

					foreach (var keyframe in DataManager.inst.gameData.eventObjects.allEvents[4])
					{
						var eventValue = keyframe.eventValues[0].ToString();
						if (eventValue.Length == 4 && id.Length == 6)
                        {
							eventValue = "00" + eventValue;
                        }
						if (eventValue.Length == 5 && id.Length == 6)
                        {
							eventValue = "0" + eventValue;
                        }
						if (beatmapTheme.id == eventValue && levelThemes.Find(x => x.id == eventValue) == null)
						{
							levelThemes.Add(beatmapTheme);
						}

						if (beatmapTheme.id == eventValue && !savedBeatmapThemes.ContainsKey(id))
							savedBeatmapThemes.Add(id, beatmapTheme);
					}
				}

				for (int i = 0; i < levelThemes.Count; i++)
				{
					Debug.LogFormat("{0}Saving " + levelThemes[i].id + " - " + levelThemes[i].name + " to level!", EditorPlugin.className);
					jn["themes"][i]["id"] = levelThemes[i].id;
					jn["themes"][i]["name"] = levelThemes[i].name;
					jn["themes"][i]["gui"] = ColorToHex(levelThemes[i].guiColor);
					jn["themes"][i]["bg"] = LSColors.ColorToHex(levelThemes[i].backgroundColor);
					for (int j = 0; j < levelThemes[i].playerColors.Count; j++)
					{
						if (ConfigEntries.SaveOpacityToThemes.Value)
							jn["themes"][i]["players"][j] = ColorToHex(levelThemes[i].playerColors[j]);
						else
							jn["themes"][i]["players"][j] = LSColors.ColorToHex(levelThemes[i].playerColors[j]);
					}
					for (int j = 0; j < levelThemes[i].objectColors.Count; j++)
					{
						if (ConfigEntries.SaveOpacityToThemes.Value)
							jn["themes"][i]["objs"][j] = ColorToHex(levelThemes[i].objectColors[j]);
						else
							jn["themes"][i]["objs"][j] = LSColors.ColorToHex(levelThemes[i].objectColors[j]);
					}
					for (int j = 0; j < levelThemes[i].backgroundColors.Count; j++)
					{
						jn["themes"][i]["bgs"][j] = LSColors.ColorToHex(levelThemes[i].backgroundColors[j]);
					}
				}
			}
			Debug.Log("Saving Checkpoints");
			for (int i = 0; i < _data.beatmapData.checkpoints.Count; i++)
			{
				jn["checkpoints"][i]["active"] = "False";
				jn["checkpoints"][i]["name"] = _data.beatmapData.checkpoints[i].name;
				jn["checkpoints"][i]["t"] = _data.beatmapData.checkpoints[i].time.ToString();
				jn["checkpoints"][i]["pos"]["x"] = _data.beatmapData.checkpoints[i].pos.x.ToString();
				jn["checkpoints"][i]["pos"]["y"] = _data.beatmapData.checkpoints[i].pos.y.ToString();
			}
			Debug.Log("Saving Beatmap Objects");
			if (_data.beatmapObjects != null)
			{
				List<BeatmapObject> list = _data.beatmapObjects.FindAll((BeatmapObject x) => !x.fromPrefab);
				jn["beatmap_objects"] = new JSONArray();
				for (int i = 0; i < list.Count; i++)
				{
					if (list[i] != null && list[i].events != null && !list[i].fromPrefab)
					{
						jn["beatmap_objects"][i]["id"] = list[i].id;
						if (!string.IsNullOrEmpty(list[i].prefabID))
						{
							jn["beatmap_objects"][i]["pid"] = list[i].prefabID;
						}
						if (!string.IsNullOrEmpty(list[i].prefabInstanceID))
						{
							jn["beatmap_objects"][i]["piid"] = list[i].prefabInstanceID;
						}
						if (list[i].GetParentType().ToString() != "101")
						{
							jn["beatmap_objects"][i]["pt"] = list[i].GetParentType().ToString();
						}
						if (list[i].getParentOffsets().FindIndex((float x) => x != 0f) != -1)
						{
							int num4 = 0;
							foreach (float num5 in list[i].getParentOffsets())
							{
								jn["beatmap_objects"][i]["po"][num4] = num5.ToString();
								num4++;
							}
						}
						jn["beatmap_objects"][i]["p"] = list[i].parent.ToString();
						jn["beatmap_objects"][i]["d"] = list[i].Depth.ToString();
						jn["beatmap_objects"][i]["st"] = list[i].StartTime.ToString();
						if (!string.IsNullOrEmpty(list[i].name))
						{
							jn["beatmap_objects"][i]["name"] = list[i].name;
						}
						jn["beatmap_objects"][i]["ot"] = (int)list[i].objectType;
						jn["beatmap_objects"][i]["akt"] = (int)list[i].autoKillType;
						jn["beatmap_objects"][i]["ako"] = list[i].autoKillOffset;
						if (list[i].shape != 0)
						{
							jn["beatmap_objects"][i]["shape"] = list[i].shape.ToString();
						}
						if (list[i].shapeOption != 0)
						{
							jn["beatmap_objects"][i]["so"] = list[i].shapeOption.ToString();
						}
						if (!string.IsNullOrEmpty(list[i].text))
						{
							jn["beatmap_objects"][i]["text"] = list[i].text;
						}
						jn["beatmap_objects"][i]["o"]["x"] = list[i].origin.x.ToString();
						jn["beatmap_objects"][i]["o"]["y"] = list[i].origin.y.ToString();
						if (list[i].editorData.locked)
						{
							jn["beatmap_objects"][i]["ed"]["locked"] = list[i].editorData.locked.ToString();
						}
						if (list[i].editorData.collapse)
						{
							jn["beatmap_objects"][i]["ed"]["shrink"] = list[i].editorData.collapse.ToString();
						}
						jn["beatmap_objects"][i]["ed"]["bin"] = list[i].editorData.Bin.ToString();
						jn["beatmap_objects"][i]["ed"]["layer"] = list[i].editorData.Layer.ToString();
						jn["beatmap_objects"][i]["events"]["pos"] = new JSONArray();
						for (int j = 0; j < list[i].events[0].Count; j++)
						{
							jn["beatmap_objects"][i]["events"]["pos"][j]["t"] = list[i].events[0][j].eventTime.ToString();
							jn["beatmap_objects"][i]["events"]["pos"][j]["x"] = list[i].events[0][j].eventValues[0].ToString();
							jn["beatmap_objects"][i]["events"]["pos"][j]["y"] = list[i].events[0][j].eventValues[1].ToString();

							//Position Z
							if (list[i].events[0][j].eventValues.Length > 2)
							{
								jn["beatmap_objects"][i]["events"]["pos"][j]["z"] = list[i].events[0][j].eventValues[2].ToString();
							}

							if (list[i].events[0][j].curveType.Name != "Linear")
							{
								jn["beatmap_objects"][i]["events"]["pos"][j]["ct"] = list[i].events[0][j].curveType.Name.ToString();
							}
							if (list[i].events[0][j].random != 0)
							{
								jn["beatmap_objects"][i]["events"]["pos"][j]["r"] = list[i].events[0][j].random.ToString();
								jn["beatmap_objects"][i]["events"]["pos"][j]["rx"] = list[i].events[0][j].eventRandomValues[0].ToString();
								jn["beatmap_objects"][i]["events"]["pos"][j]["ry"] = list[i].events[0][j].eventRandomValues[1].ToString();
								jn["beatmap_objects"][i]["events"]["pos"][j]["rz"] = list[i].events[0][j].eventRandomValues[2].ToString();
							}
						}
						jn["beatmap_objects"][i]["events"]["sca"] = new JSONArray();
						for (int j = 0; j < list[i].events[1].Count; j++)
						{
							jn["beatmap_objects"][i]["events"]["sca"][j]["t"] = list[i].events[1][j].eventTime.ToString();
							jn["beatmap_objects"][i]["events"]["sca"][j]["x"] = list[i].events[1][j].eventValues[0].ToString();
							jn["beatmap_objects"][i]["events"]["sca"][j]["y"] = list[i].events[1][j].eventValues[1].ToString();
							if (list[i].events[1][j].curveType.Name != "Linear")
							{
								jn["beatmap_objects"][i]["events"]["sca"][j]["ct"] = list[i].events[1][j].curveType.Name.ToString();
							}
							if (list[i].events[1][j].random != 0)
							{
								jn["beatmap_objects"][i]["events"]["sca"][j]["r"] = list[i].events[1][j].random.ToString();
								jn["beatmap_objects"][i]["events"]["sca"][j]["rx"] = list[i].events[1][j].eventRandomValues[0].ToString();
								jn["beatmap_objects"][i]["events"]["sca"][j]["ry"] = list[i].events[1][j].eventRandomValues[1].ToString();
								jn["beatmap_objects"][i]["events"]["sca"][j]["rz"] = list[i].events[1][j].eventRandomValues[2].ToString();
							}
						}
						jn["beatmap_objects"][i]["events"]["rot"] = new JSONArray();
						for (int j = 0; j < list[i].events[2].Count; j++)
						{
							jn["beatmap_objects"][i]["events"]["rot"][j]["t"] = list[i].events[2][j].eventTime.ToString();
							jn["beatmap_objects"][i]["events"]["rot"][j]["x"] = list[i].events[2][j].eventValues[0].ToString();
							if (list[i].events[2][j].curveType.Name != "Linear")
							{
								jn["beatmap_objects"][i]["events"]["rot"][j]["ct"] = list[i].events[2][j].curveType.Name.ToString();
							}
							if (list[i].events[2][j].random != 0)
							{
								jn["beatmap_objects"][i]["events"]["rot"][j]["r"] = list[i].events[2][j].random.ToString();
								jn["beatmap_objects"][i]["events"]["rot"][j]["rx"] = list[i].events[2][j].eventRandomValues[0].ToString();
								jn["beatmap_objects"][i]["events"]["rot"][j]["rz"] = list[i].events[2][j].eventRandomValues[2].ToString();
							}
						}
						jn["beatmap_objects"][i]["events"]["col"] = new JSONArray();
						for (int j = 0; j < list[i].events[3].Count; j++)
						{
							jn["beatmap_objects"][i]["events"]["col"][j]["t"] = list[i].events[3][j].eventTime.ToString();
							jn["beatmap_objects"][i]["events"]["col"][j]["x"] = list[i].events[3][j].eventValues[0].ToString();
							jn["beatmap_objects"][i]["events"]["col"][j]["y"] = list[i].events[3][j].eventValues[1].ToString();
							jn["beatmap_objects"][i]["events"]["col"][j]["z"] = list[i].events[3][j].eventValues[2].ToString();
							jn["beatmap_objects"][i]["events"]["col"][j]["x2"] = list[i].events[3][j].eventValues[3].ToString();
							jn["beatmap_objects"][i]["events"]["col"][j]["y2"] = list[i].events[3][j].eventValues[4].ToString();
							
							if (list[i].events[3][j].curveType.Name != "Linear")
							{
								jn["beatmap_objects"][i]["events"]["col"][j]["ct"] = list[i].events[3][j].curveType.Name.ToString();
							}
							if (list[i].events[3][j].random != 0)
							{
								jn["beatmap_objects"][i]["events"]["col"][j]["r"] = list[i].events[3][j].random.ToString();
								jn["beatmap_objects"][i]["events"]["col"][j]["rx"] = list[i].events[3][j].eventRandomValues[0].ToString();
							}
						}

                        if (ObjectModifiersEditor.inst != null)
                        {
                            var modifierObject = ObjectModifiersEditor.GetModifierObject(list[i]);

                            if (modifierObject != null)
                            {
                                for (int j = 0; j < ObjectModifiersEditor.GetModifierCount(list[i]); j++)
                                {
                                    var modifier = ObjectModifiersEditor.GetModifierIndex(list[i], j);

                                    var type = (int)modifier.GetType().GetField("type", BindingFlags.Public | BindingFlags.Instance).GetValue(modifier);

									List<string> commands = (List<string>)modifier.GetType().GetField("command", BindingFlags.Public | BindingFlags.Instance).GetValue(modifier);

									var value = (string)modifier.GetType().GetField("value", BindingFlags.Public | BindingFlags.Instance).GetValue(modifier);

									var constant = ((bool)modifier.GetType().GetField("constant", BindingFlags.Public | BindingFlags.Instance).GetValue(modifier)).ToString();

									if (commands.Count > 0 && !string.IsNullOrEmpty(commands[0]))
									{
										//jn["beatmap_objects"][i]["modifiers"][j] = new JSONArray();

										jn["beatmap_objects"][i]["modifiers"][j]["type"] = type;
										if (type == 0)
										{
											jn["beatmap_objects"][i]["modifiers"][j]["not"] = ((bool)modifier.GetType().GetField("not", BindingFlags.Public | BindingFlags.Instance).GetValue(modifier)).ToString();
										}

										for (int k = 0; k < commands.Count; k++)
										{
											if (!string.IsNullOrEmpty(commands[k]))
												jn["beatmap_objects"][i]["modifiers"][j]["commands"][k] = commands[k];
										}

										jn["beatmap_objects"][i]["modifiers"][j]["value"] = value;

										jn["beatmap_objects"][i]["modifiers"][j]["const"] = constant;
									}
								}
                            }
                        }
                    }
				}
			}
			else
			{
				Debug.Log("skipping objects");
				jn["beatmap_objects"] = new JSONArray();
			}
			Debug.Log("Saving Background Objects");
			for (int i = 0; i < _data.backgroundObjects.Count; i++)
			{
				jn["bg_objects"][i]["active"] = _data.backgroundObjects[i].active.ToString();
				jn["bg_objects"][i]["name"] = _data.backgroundObjects[i].name.ToString();
				jn["bg_objects"][i]["kind"] = _data.backgroundObjects[i].kind.ToString();
				jn["bg_objects"][i]["pos"]["x"] = _data.backgroundObjects[i].pos.x.ToString();
				jn["bg_objects"][i]["pos"]["y"] = _data.backgroundObjects[i].pos.y.ToString();
				jn["bg_objects"][i]["size"]["x"] = _data.backgroundObjects[i].scale.x.ToString();
				jn["bg_objects"][i]["size"]["y"] = _data.backgroundObjects[i].scale.y.ToString();
				jn["bg_objects"][i]["rot"] = _data.backgroundObjects[i].rot.ToString();
				jn["bg_objects"][i]["color"] = _data.backgroundObjects[i].color.ToString();
				jn["bg_objects"][i]["layer"] = _data.backgroundObjects[i].layer.ToString();
				jn["bg_objects"][i]["fade"] = _data.backgroundObjects[i].drawFade.ToString();

                try
				{
					var bg = Objects.backgroundObjects[i];
					jn["bg_objects"][i]["zscale"] = bg.zscale.ToString();
					jn["bg_objects"][i]["depth"] = bg.depth.ToString();
					jn["bg_objects"][i]["s"] = bg.shape.Type.ToString();
					jn["bg_objects"][i]["so"] = bg.shape.Option.ToString();

                    {
						jn["bg_objects"][i]["rc"]["pos"]["i"]["x"] = bg.reactivePosIntensity.x.ToString();
						jn["bg_objects"][i]["rc"]["pos"]["i"]["y"] = bg.reactivePosIntensity.y.ToString();
						jn["bg_objects"][i]["rc"]["pos"]["s"]["x"] = bg.reactivePosSamples.x.ToString();
						jn["bg_objects"][i]["rc"]["pos"]["s"]["y"] = bg.reactivePosSamples.y.ToString();

						jn["bg_objects"][i]["rc"]["z"]["active"] = bg.reactiveIncludesZ.ToString();
						jn["bg_objects"][i]["rc"]["z"]["i"] = bg.reactiveZIntensity.ToString();
						jn["bg_objects"][i]["rc"]["z"]["s"] = bg.reactiveZSample.ToString();

						jn["bg_objects"][i]["rc"]["sca"]["i"]["x"] = bg.reactiveScaIntensity.x.ToString();
						jn["bg_objects"][i]["rc"]["sca"]["i"]["y"] = bg.reactiveScaIntensity.y.ToString();
						jn["bg_objects"][i]["rc"]["sca"]["s"]["x"] = bg.reactiveScaSamples.x.ToString();
						jn["bg_objects"][i]["rc"]["sca"]["s"]["y"] = bg.reactiveScaSamples.y.ToString();

						jn["bg_objects"][i]["rc"]["rot"]["i"] = bg.reactiveRotIntensity.ToString();
						jn["bg_objects"][i]["rc"]["rot"]["s"] = bg.reactiveRotSample.ToString();
						
						jn["bg_objects"][i]["rc"]["col"]["i"] = bg.reactiveColIntensity.ToString();
						jn["bg_objects"][i]["rc"]["col"]["s"] = bg.reactiveColSample.ToString();
						jn["bg_objects"][i]["rc"]["col"]["c"] = bg.reactiveCol.ToString();
					}
				}
                catch (Exception ex)
				{
					Debug.Log($"{EditorPlugin.className}BG Mod error!\nMESSAGE: {ex.Message}\nSTACKTRACE: {ex.StackTrace}");
				}

				if (_data.backgroundObjects[i].reactive)
				{
					jn["bg_objects"][i]["r_set"]["type"] = _data.backgroundObjects[i].reactiveType.ToString();
					jn["bg_objects"][i]["r_set"]["scale"] = _data.backgroundObjects[i].reactiveScale.ToString();
				}
			}
			Debug.Log("Saving Event Objects");
			{
				var allEvents = _data.eventObjects.allEvents;

				for (int i = 0; i < _data.eventObjects.allEvents[0].Count(); i++)
				{
					jn["events"]["pos"][i]["t"] = _data.eventObjects.allEvents[0][i].eventTime.ToString();
					jn["events"]["pos"][i]["x"] = _data.eventObjects.allEvents[0][i].eventValues[0].ToString();
					jn["events"]["pos"][i]["y"] = _data.eventObjects.allEvents[0][i].eventValues[1].ToString();
					if (_data.eventObjects.allEvents[0][i].curveType.Name != "Linear")
					{
						jn["events"]["pos"][i]["ct"] = _data.eventObjects.allEvents[0][i].curveType.Name.ToString();
					}
					if (_data.eventObjects.allEvents[0][i].random != 0)
					{
						jn["events"]["pos"][i]["r"] = _data.eventObjects.allEvents[0][i].random.ToString();
						jn["events"]["pos"][i]["rx"] = _data.eventObjects.allEvents[0][i].eventRandomValues[0].ToString();
						jn["events"]["pos"][i]["ry"] = _data.eventObjects.allEvents[0][i].eventRandomValues[1].ToString();
					}
				}
				for (int i = 0; i < _data.eventObjects.allEvents[1].Count(); i++)
				{
					jn["events"]["zoom"][i]["t"] = _data.eventObjects.allEvents[1][i].eventTime.ToString();
					jn["events"]["zoom"][i]["x"] = _data.eventObjects.allEvents[1][i].eventValues[0].ToString();
					if (_data.eventObjects.allEvents[1][i].curveType.Name != "Linear")
					{
						jn["events"]["zoom"][i]["ct"] = _data.eventObjects.allEvents[1][i].curveType.Name.ToString();
					}
					if (_data.eventObjects.allEvents[1][i].random != 0)
					{
						jn["events"]["zoom"][i]["r"] = _data.eventObjects.allEvents[1][i].random.ToString();
						jn["events"]["zoom"][i]["rx"] = _data.eventObjects.allEvents[1][i].eventRandomValues[0].ToString();
					}
				}
				for (int i = 0; i < _data.eventObjects.allEvents[2].Count(); i++)
				{
					jn["events"]["rot"][i]["t"] = _data.eventObjects.allEvents[2][i].eventTime.ToString();
					jn["events"]["rot"][i]["x"] = _data.eventObjects.allEvents[2][i].eventValues[0].ToString();
					if (_data.eventObjects.allEvents[2][i].curveType.Name != "Linear")
					{
						jn["events"]["rot"][i]["ct"] = _data.eventObjects.allEvents[2][i].curveType.Name.ToString();
					}
					if (_data.eventObjects.allEvents[2][i].random != 0)
					{
						jn["events"]["rot"][i]["r"] = _data.eventObjects.allEvents[2][i].random.ToString();
						jn["events"]["rot"][i]["rx"] = _data.eventObjects.allEvents[2][i].eventRandomValues[0].ToString();
					}
				}
				for (int i = 0; i < _data.eventObjects.allEvents[3].Count(); i++)
				{
					jn["events"]["shake"][i]["t"] = _data.eventObjects.allEvents[3][i].eventTime.ToString();
					jn["events"]["shake"][i]["x"] = _data.eventObjects.allEvents[3][i].eventValues[0].ToString();
					if (_data.eventObjects.allEvents[3][i].eventValues.Length > 1)
						jn["events"]["shake"][i]["y"] = _data.eventObjects.allEvents[3][i].eventValues[1].ToString();
					if (_data.eventObjects.allEvents[3][i].eventValues.Length > 2)
						jn["events"]["shake"][i]["z"] = _data.eventObjects.allEvents[3][i].eventValues[2].ToString();

					if (_data.eventObjects.allEvents[3][i].curveType.Name != "Linear")
					{
						jn["events"]["shake"][i]["ct"] = _data.eventObjects.allEvents[3][i].curveType.Name.ToString();
					}
					if (_data.eventObjects.allEvents[3][i].random != 0)
					{
						jn["events"]["shake"][i]["r"] = _data.eventObjects.allEvents[3][i].random.ToString();
						jn["events"]["shake"][i]["rx"] = _data.eventObjects.allEvents[3][i].eventRandomValues[0].ToString();
						jn["events"]["shake"][i]["ry"] = _data.eventObjects.allEvents[3][i].eventRandomValues[1].ToString();
					}
				}
				for (int i = 0; i < _data.eventObjects.allEvents[4].Count(); i++)
				{
					jn["events"]["theme"][i]["t"] = _data.eventObjects.allEvents[4][i].eventTime.ToString();
					jn["events"]["theme"][i]["x"] = _data.eventObjects.allEvents[4][i].eventValues[0].ToString();
					if (_data.eventObjects.allEvents[4][i].curveType.Name != "Linear")
					{
						jn["events"]["theme"][i]["ct"] = _data.eventObjects.allEvents[4][i].curveType.Name.ToString();
					}
					if (_data.eventObjects.allEvents[4][i].random != 0)
					{
						jn["events"]["theme"][i]["r"] = _data.eventObjects.allEvents[4][i].random.ToString();
						jn["events"]["theme"][i]["rx"] = _data.eventObjects.allEvents[4][i].eventRandomValues[0].ToString();
					}
				}
				for (int i = 0; i < _data.eventObjects.allEvents[5].Count(); i++)
				{
					jn["events"]["chroma"][i]["t"] = _data.eventObjects.allEvents[5][i].eventTime.ToString();
					jn["events"]["chroma"][i]["x"] = _data.eventObjects.allEvents[5][i].eventValues[0].ToString();
					if (_data.eventObjects.allEvents[5][i].curveType.Name != "Linear")
					{
						jn["events"]["chroma"][i]["ct"] = _data.eventObjects.allEvents[5][i].curveType.Name.ToString();
					}
					if (_data.eventObjects.allEvents[5][i].random != 0)
					{
						jn["events"]["chroma"][i]["r"] = _data.eventObjects.allEvents[5][i].random.ToString();
						jn["events"]["chroma"][i]["rx"] = _data.eventObjects.allEvents[5][i].eventRandomValues[0].ToString();
					}
				}
				for (int i = 0; i < _data.eventObjects.allEvents[6].Count(); i++)
				{
					jn["events"]["bloom"][i]["t"] = _data.eventObjects.allEvents[6][i].eventTime.ToString();
					jn["events"]["bloom"][i]["x"] = _data.eventObjects.allEvents[6][i].eventValues[0].ToString();
					if (_data.eventObjects.allEvents[6][i].eventValues.Length > 1)
						jn["events"]["bloom"][i]["y"] = _data.eventObjects.allEvents[6][i].eventValues[1].ToString();
					if (_data.eventObjects.allEvents[6][i].eventValues.Length > 2)
						jn["events"]["bloom"][i]["z"] = _data.eventObjects.allEvents[6][i].eventValues[2].ToString();
					if (_data.eventObjects.allEvents[6][i].eventValues.Length > 3)
						jn["events"]["bloom"][i]["x2"] = _data.eventObjects.allEvents[6][i].eventValues[3].ToString();
					if (_data.eventObjects.allEvents[6][i].eventValues.Length > 4)
						jn["events"]["bloom"][i]["y2"] = _data.eventObjects.allEvents[6][i].eventValues[4].ToString();

					if (_data.eventObjects.allEvents[6][i].curveType.Name != "Linear")
					{
						jn["events"]["bloom"][i]["ct"] = _data.eventObjects.allEvents[6][i].curveType.Name.ToString();
					}
					if (_data.eventObjects.allEvents[6][i].random != 0)
					{
						jn["events"]["bloom"][i]["r"] = _data.eventObjects.allEvents[6][i].random.ToString();
						jn["events"]["bloom"][i]["rx"] = _data.eventObjects.allEvents[6][i].eventRandomValues[0].ToString();
					}
				}
				for (int i = 0; i < _data.eventObjects.allEvents[7].Count(); i++)
				{
					jn["events"]["vignette"][i]["t"] = _data.eventObjects.allEvents[7][i].eventTime.ToString();
					jn["events"]["vignette"][i]["x"] = _data.eventObjects.allEvents[7][i].eventValues[0].ToString();
					jn["events"]["vignette"][i]["y"] = _data.eventObjects.allEvents[7][i].eventValues[1].ToString();
					jn["events"]["vignette"][i]["z"] = _data.eventObjects.allEvents[7][i].eventValues[2].ToString();
					jn["events"]["vignette"][i]["x2"] = _data.eventObjects.allEvents[7][i].eventValues[3].ToString();
					jn["events"]["vignette"][i]["y2"] = _data.eventObjects.allEvents[7][i].eventValues[4].ToString();
					jn["events"]["vignette"][i]["z2"] = _data.eventObjects.allEvents[7][i].eventValues[5].ToString();
					if (_data.eventObjects.allEvents[7][i].eventValues.Length > 6)
						jn["events"]["vignette"][i]["x3"] = _data.eventObjects.allEvents[7][i].eventValues[6].ToString();

					if (_data.eventObjects.allEvents[7][i].curveType.Name != "Linear")
					{
						jn["events"]["vignette"][i]["ct"] = _data.eventObjects.allEvents[7][i].curveType.Name.ToString();
					}
					if (_data.eventObjects.allEvents[7][i].random != 0)
					{
						jn["events"]["vignette"][i]["r"] = _data.eventObjects.allEvents[7][i].random.ToString();
						jn["events"]["vignette"][i]["rx"] = _data.eventObjects.allEvents[7][i].eventRandomValues[0].ToString();
						jn["events"]["vignette"][i]["ry"] = _data.eventObjects.allEvents[7][i].eventRandomValues[1].ToString();
						jn["events"]["vignette"][i]["value_random_z"] = _data.eventObjects.allEvents[7][i].eventRandomValues[2].ToString();
						jn["events"]["vignette"][i]["value_random_x2"] = _data.eventObjects.allEvents[7][i].eventRandomValues[3].ToString();
						jn["events"]["vignette"][i]["value_random_y2"] = _data.eventObjects.allEvents[7][i].eventRandomValues[4].ToString();
						jn["events"]["vignette"][i]["value_random_z2"] = _data.eventObjects.allEvents[7][i].eventRandomValues[5].ToString();
					}
				}
				for (int i = 0; i < _data.eventObjects.allEvents[8].Count(); i++)
				{
					jn["events"]["lens"][i]["t"] = _data.eventObjects.allEvents[8][i].eventTime.ToString();
					jn["events"]["lens"][i]["x"] = _data.eventObjects.allEvents[8][i].eventValues[0].ToString();
					if (_data.eventObjects.allEvents[8][i].eventValues.Length > 1)
						jn["events"]["lens"][i]["y"] = _data.eventObjects.allEvents[8][i].eventValues[1].ToString();
					if (_data.eventObjects.allEvents[8][i].eventValues.Length > 2)
						jn["events"]["lens"][i]["z"] = _data.eventObjects.allEvents[8][i].eventValues[2].ToString();
					if (_data.eventObjects.allEvents[8][i].eventValues.Length > 3)
						jn["events"]["lens"][i]["x2"] = _data.eventObjects.allEvents[8][i].eventValues[3].ToString();
					if (_data.eventObjects.allEvents[8][i].eventValues.Length > 4)
						jn["events"]["lens"][i]["y2"] = _data.eventObjects.allEvents[8][i].eventValues[4].ToString();
					if (_data.eventObjects.allEvents[8][i].eventValues.Length > 5)
						jn["events"]["lens"][i]["z2"] = _data.eventObjects.allEvents[8][i].eventValues[5].ToString();

					if (_data.eventObjects.allEvents[8][i].curveType.Name != "Linear")
					{
						jn["events"]["lens"][i]["ct"] = _data.eventObjects.allEvents[8][i].curveType.Name.ToString();
					}
					if (_data.eventObjects.allEvents[8][i].random != 0)
					{
						jn["events"]["lens"][i]["r"] = _data.eventObjects.allEvents[8][i].random.ToString();
						jn["events"]["lens"][i]["rx"] = _data.eventObjects.allEvents[8][i].eventRandomValues[0].ToString();
					}
				}
				for (int i = 0; i < _data.eventObjects.allEvents[9].Count(); i++)
				{
					jn["events"]["grain"][i]["t"] = _data.eventObjects.allEvents[9][i].eventTime.ToString();
					jn["events"]["grain"][i]["x"] = _data.eventObjects.allEvents[9][i].eventValues[0].ToString();
					jn["events"]["grain"][i]["y"] = _data.eventObjects.allEvents[9][i].eventValues[1].ToString();
					jn["events"]["grain"][i]["z"] = _data.eventObjects.allEvents[9][i].eventValues[2].ToString();
					if (_data.eventObjects.allEvents[9][i].curveType.Name != "Linear")
					{
						jn["events"]["grain"][i]["ct"] = _data.eventObjects.allEvents[9][i].curveType.Name.ToString();
					}
					if (_data.eventObjects.allEvents[9][i].random != 0)
					{
						jn["events"]["grain"][i]["r"] = _data.eventObjects.allEvents[9][i].random.ToString();
						jn["events"]["grain"][i]["rx"] = _data.eventObjects.allEvents[9][i].eventRandomValues[0].ToString();
						jn["events"]["grain"][i]["ry"] = _data.eventObjects.allEvents[9][i].eventRandomValues[1].ToString();
						jn["events"]["grain"][i]["value_random_z"] = _data.eventObjects.allEvents[9][i].eventRandomValues[2].ToString();
					}
				}
				if (_data.eventObjects.allEvents.Count > 10)
				{
					for (int i = 0; i < _data.eventObjects.allEvents[10].Count(); i++)
					{
						jn["events"]["cg"][i]["t"] = _data.eventObjects.allEvents[10][i].eventTime.ToString();
						jn["events"]["cg"][i]["x"] = _data.eventObjects.allEvents[10][i].eventValues[0].ToString();
						jn["events"]["cg"][i]["y"] = _data.eventObjects.allEvents[10][i].eventValues[1].ToString();
						jn["events"]["cg"][i]["z"] = _data.eventObjects.allEvents[10][i].eventValues[2].ToString();
						jn["events"]["cg"][i]["x2"] = _data.eventObjects.allEvents[10][i].eventValues[3].ToString();
						jn["events"]["cg"][i]["y2"] = _data.eventObjects.allEvents[10][i].eventValues[4].ToString();
						jn["events"]["cg"][i]["z2"] = _data.eventObjects.allEvents[10][i].eventValues[5].ToString();
						jn["events"]["cg"][i]["x3"] = _data.eventObjects.allEvents[10][i].eventValues[6].ToString();
						jn["events"]["cg"][i]["y3"] = _data.eventObjects.allEvents[10][i].eventValues[7].ToString();
						jn["events"]["cg"][i]["z3"] = _data.eventObjects.allEvents[10][i].eventValues[8].ToString();
						if (_data.eventObjects.allEvents[10][i].curveType.Name != "Linear")
						{
							jn["events"]["cg"][i]["ct"] = _data.eventObjects.allEvents[10][i].curveType.Name.ToString();
						}
						if (_data.eventObjects.allEvents[10][i].random != 0)
						{
							jn["events"]["cg"][i]["r"] = _data.eventObjects.allEvents[10][i].random.ToString();
							jn["events"]["cg"][i]["rx"] = _data.eventObjects.allEvents[10][i].eventRandomValues[0].ToString();
							jn["events"]["cg"][i]["ry"] = _data.eventObjects.allEvents[10][i].eventRandomValues[1].ToString();
						}
					}
				}
				if (_data.eventObjects.allEvents.Count > 11)
				{
					for (int i = 0; i < _data.eventObjects.allEvents[11].Count(); i++)
					{
						jn["events"]["rip"][i]["t"] = _data.eventObjects.allEvents[11][i].eventTime.ToString();
						jn["events"]["rip"][i]["x"] = _data.eventObjects.allEvents[11][i].eventValues[0].ToString();
						jn["events"]["rip"][i]["y"] = _data.eventObjects.allEvents[11][i].eventValues[1].ToString();
						jn["events"]["rip"][i]["z"] = _data.eventObjects.allEvents[11][i].eventValues[2].ToString();
						jn["events"]["rip"][i]["x2"] = _data.eventObjects.allEvents[11][i].eventValues[3].ToString();
						jn["events"]["rip"][i]["y2"] = _data.eventObjects.allEvents[11][i].eventValues[4].ToString();
						if (_data.eventObjects.allEvents[11][i].curveType.Name != "Linear")
						{
							jn["events"]["rip"][i]["ct"] = _data.eventObjects.allEvents[11][i].curveType.Name.ToString();
						}
						if (_data.eventObjects.allEvents[11][i].random != 0)
						{
							jn["events"]["rip"][i]["r"] = _data.eventObjects.allEvents[11][i].random.ToString();
							jn["events"]["rip"][i]["rx"] = _data.eventObjects.allEvents[11][i].eventRandomValues[0].ToString();
							jn["events"]["rip"][i]["ry"] = _data.eventObjects.allEvents[11][i].eventRandomValues[1].ToString();
						}
					}
				}
				if (_data.eventObjects.allEvents.Count > 12)
				{
					for (int i = 0; i < _data.eventObjects.allEvents[12].Count(); i++)
					{
						jn["events"]["rb"][i]["t"] = _data.eventObjects.allEvents[12][i].eventTime.ToString();
						jn["events"]["rb"][i]["x"] = _data.eventObjects.allEvents[12][i].eventValues[0].ToString();
						jn["events"]["rb"][i]["y"] = _data.eventObjects.allEvents[12][i].eventValues[1].ToString();
						if (_data.eventObjects.allEvents[12][i].curveType.Name != "Linear")
						{
							jn["events"]["rb"][i]["ct"] = _data.eventObjects.allEvents[12][i].curveType.Name.ToString();
						}
						if (_data.eventObjects.allEvents[12][i].random != 0)
						{
							jn["events"]["rb"][i]["r"] = _data.eventObjects.allEvents[12][i].random.ToString();
							jn["events"]["rb"][i]["rx"] = _data.eventObjects.allEvents[12][i].eventRandomValues[0].ToString();
							jn["events"]["rb"][i]["ry"] = _data.eventObjects.allEvents[12][i].eventRandomValues[1].ToString();
						}
					}
				}
				if (_data.eventObjects.allEvents.Count > 13)
				{
					for (int i = 0; i < _data.eventObjects.allEvents[13].Count(); i++)
					{
						jn["events"]["cs"][i]["t"] = _data.eventObjects.allEvents[13][i].eventTime.ToString();
						jn["events"]["cs"][i]["x"] = _data.eventObjects.allEvents[13][i].eventValues[0].ToString();
						jn["events"]["cs"][i]["y"] = _data.eventObjects.allEvents[13][i].eventValues[1].ToString();
						if (_data.eventObjects.allEvents[13][i].curveType.Name != "Linear")
						{
							jn["events"]["cs"][i]["ct"] = _data.eventObjects.allEvents[13][i].curveType.Name.ToString();
						}
						if (_data.eventObjects.allEvents[13][i].random != 0)
						{
							jn["events"]["cs"][i]["r"] = _data.eventObjects.allEvents[13][i].random.ToString();
							jn["events"]["cs"][i]["rx"] = _data.eventObjects.allEvents[13][i].eventRandomValues[0].ToString();
							jn["events"]["cs"][i]["ry"] = _data.eventObjects.allEvents[13][i].eventRandomValues[1].ToString();
						}
					}
				}
				if (_data.eventObjects.allEvents.Count > 14)
				{
					for (int i = 0; i < _data.eventObjects.allEvents[14].Count(); i++)
					{
						jn["events"]["offset"][i]["t"] = _data.eventObjects.allEvents[14][i].eventTime.ToString();
						jn["events"]["offset"][i]["x"] = _data.eventObjects.allEvents[14][i].eventValues[0].ToString();
						jn["events"]["offset"][i]["y"] = _data.eventObjects.allEvents[14][i].eventValues[1].ToString();
						if (_data.eventObjects.allEvents[14][i].curveType.Name != "Linear")
						{
							jn["events"]["offset"][i]["ct"] = _data.eventObjects.allEvents[14][i].curveType.Name.ToString();
						}
						if (_data.eventObjects.allEvents[14][i].random != 0)
						{
							jn["events"]["offset"][i]["r"] = _data.eventObjects.allEvents[14][i].random.ToString();
							jn["events"]["offset"][i]["rx"] = _data.eventObjects.allEvents[14][i].eventRandomValues[0].ToString();
							jn["events"]["offset"][i]["ry"] = _data.eventObjects.allEvents[14][i].eventRandomValues[1].ToString();
						}
					}
				}
				if (_data.eventObjects.allEvents.Count > 15)
				{
					for (int i = 0; i < _data.eventObjects.allEvents[15].Count(); i++)
					{
						jn["events"]["grd"][i]["t"] = _data.eventObjects.allEvents[15][i].eventTime.ToString();
						jn["events"]["grd"][i]["x"] = _data.eventObjects.allEvents[15][i].eventValues[0].ToString();
						jn["events"]["grd"][i]["y"] = _data.eventObjects.allEvents[15][i].eventValues[1].ToString();
						jn["events"]["grd"][i]["z"] = _data.eventObjects.allEvents[15][i].eventValues[2].ToString();
						jn["events"]["grd"][i]["x2"] = _data.eventObjects.allEvents[15][i].eventValues[3].ToString();
						jn["events"]["grd"][i]["y2"] = _data.eventObjects.allEvents[15][i].eventValues[4].ToString();
						if (_data.eventObjects.allEvents[15][i].curveType.Name != "Linear")
						{
							jn["events"]["grd"][i]["ct"] = _data.eventObjects.allEvents[15][i].curveType.Name.ToString();
						}
						if (_data.eventObjects.allEvents[15][i].random != 0)
						{
							jn["events"]["grd"][i]["r"] = _data.eventObjects.allEvents[15][i].random.ToString();
							jn["events"]["grd"][i]["rx"] = _data.eventObjects.allEvents[15][i].eventRandomValues[0].ToString();
							jn["events"]["grd"][i]["ry"] = _data.eventObjects.allEvents[15][i].eventRandomValues[1].ToString();
						}
					}
				}
				if (_data.eventObjects.allEvents.Count > 16)
				{
					for (int i = 0; i < _data.eventObjects.allEvents[16].Count(); i++)
					{
						jn["events"]["dbv"][i]["t"] = _data.eventObjects.allEvents[16][i].eventTime.ToString();
						jn["events"]["dbv"][i]["x"] = _data.eventObjects.allEvents[16][i].eventValues[0].ToString();
						if (_data.eventObjects.allEvents[16][i].curveType.Name != "Linear")
						{
							jn["events"]["dbv"][i]["ct"] = _data.eventObjects.allEvents[16][i].curveType.Name.ToString();
						}
						if (_data.eventObjects.allEvents[16][i].random != 0)
						{
							jn["events"]["dbv"][i]["r"] = _data.eventObjects.allEvents[16][i].random.ToString();
							jn["events"]["dbv"][i]["rx"] = _data.eventObjects.allEvents[16][i].eventRandomValues[0].ToString();
							jn["events"]["dbv"][i]["ry"] = _data.eventObjects.allEvents[16][i].eventRandomValues[1].ToString();
						}
					}
				}
				if (_data.eventObjects.allEvents.Count > 17)
				{
					for (int i = 0; i < _data.eventObjects.allEvents[17].Count(); i++)
					{
						jn["events"]["scan"][i]["t"] = _data.eventObjects.allEvents[17][i].eventTime.ToString();
						jn["events"]["scan"][i]["x"] = _data.eventObjects.allEvents[17][i].eventValues[0].ToString();
						jn["events"]["scan"][i]["y"] = _data.eventObjects.allEvents[17][i].eventValues[1].ToString();
						jn["events"]["scan"][i]["z"] = _data.eventObjects.allEvents[17][i].eventValues[2].ToString();
						if (_data.eventObjects.allEvents[17][i].curveType.Name != "Linear")
						{
							jn["events"]["scan"][i]["ct"] = _data.eventObjects.allEvents[17][i].curveType.Name.ToString();
						}
						if (_data.eventObjects.allEvents[17][i].random != 0)
						{
							jn["events"]["scan"][i]["r"] = _data.eventObjects.allEvents[17][i].random.ToString();
							jn["events"]["scan"][i]["rx"] = _data.eventObjects.allEvents[17][i].eventRandomValues[0].ToString();
							jn["events"]["scan"][i]["ry"] = _data.eventObjects.allEvents[17][i].eventRandomValues[1].ToString();
						}
					}
				}
				if (_data.eventObjects.allEvents.Count > 18)
				{
					for (int i = 0; i < _data.eventObjects.allEvents[18].Count(); i++)
					{
						jn["events"]["blur"][i]["t"] = _data.eventObjects.allEvents[18][i].eventTime.ToString();
						jn["events"]["blur"][i]["x"] = _data.eventObjects.allEvents[18][i].eventValues[0].ToString();
						jn["events"]["blur"][i]["y"] = _data.eventObjects.allEvents[18][i].eventValues[1].ToString();
						if (_data.eventObjects.allEvents[18][i].curveType.Name != "Linear")
						{
							jn["events"]["blur"][i]["ct"] = _data.eventObjects.allEvents[18][i].curveType.Name.ToString();
						}
						if (_data.eventObjects.allEvents[18][i].random != 0)
						{
							jn["events"]["blur"][i]["r"] = _data.eventObjects.allEvents[18][i].random.ToString();
							jn["events"]["blur"][i]["rx"] = _data.eventObjects.allEvents[18][i].eventRandomValues[0].ToString();
							jn["events"]["blur"][i]["ry"] = _data.eventObjects.allEvents[18][i].eventRandomValues[1].ToString();
						}
					}
				}
				if (_data.eventObjects.allEvents.Count > 19)
				{
					for (int i = 0; i < _data.eventObjects.allEvents[19].Count(); i++)
					{
						var eventKeyframe = _data.eventObjects.allEvents[19][i];
						jn["events"]["pixel"][i]["t"] = eventKeyframe.eventTime.ToString();
						jn["events"]["pixel"][i]["x"] = eventKeyframe.eventValues[0].ToString();
						if (eventKeyframe.curveType.Name != "Linear")
						{
							jn["events"]["pixel"][i]["ct"] = eventKeyframe.curveType.Name.ToString();
						}
						if (eventKeyframe.random != 0)
						{
							jn["events"]["pixel"][i]["r"] = eventKeyframe.random.ToString();
							jn["events"]["pixel"][i]["rx"] = eventKeyframe.eventRandomValues[0].ToString();
							jn["events"]["pixel"][i]["ry"] = eventKeyframe.eventRandomValues[1].ToString();
						}
					}
				}
				if (_data.eventObjects.allEvents.Count > 20)
				{
					for (int i = 0; i < _data.eventObjects.allEvents[20].Count(); i++)
					{
						var eventKeyframe = _data.eventObjects.allEvents[20][i];
						jn["events"]["bg"][i]["t"] = eventKeyframe.eventTime.ToString();
						jn["events"]["bg"][i]["x"] = eventKeyframe.eventValues[0].ToString();
						if (eventKeyframe.curveType.Name != "Linear")
						{
							jn["events"]["bg"][i]["ct"] = eventKeyframe.curveType.Name.ToString();
						}
						if (eventKeyframe.random != 0)
						{
							jn["events"]["bg"][i]["r"] = eventKeyframe.random.ToString();
							jn["events"]["bg"][i]["rx"] = eventKeyframe.eventRandomValues[0].ToString();
							jn["events"]["bg"][i]["ry"] = eventKeyframe.eventRandomValues[1].ToString();
						}
					}
				}
				if (_data.eventObjects.allEvents.Count > 21)
				{
					for (int i = 0; i < _data.eventObjects.allEvents[21].Count(); i++)
					{
						var eventKeyframe = _data.eventObjects.allEvents[21][i];
						jn["events"]["invert"][i]["t"] = eventKeyframe.eventTime.ToString();
						jn["events"]["invert"][i]["x"] = eventKeyframe.eventValues[0].ToString();
						jn["events"]["invert"][i]["y"] = eventKeyframe.eventValues[1].ToString();
						if (eventKeyframe.curveType.Name != "Linear")
						{
							jn["events"]["invert"][i]["ct"] = eventKeyframe.curveType.Name.ToString();
						}
						if (eventKeyframe.random != 0)
						{
							jn["events"]["invert"][i]["r"] = eventKeyframe.random.ToString();
							jn["events"]["invert"][i]["rx"] = eventKeyframe.eventRandomValues[0].ToString();
							jn["events"]["invert"][i]["ry"] = eventKeyframe.eventRandomValues[1].ToString();
						}
					}
				}
				if (_data.eventObjects.allEvents.Count > 22)
				{
					for (int i = 0; i < _data.eventObjects.allEvents[22].Count(); i++)
					{
						var eventKeyframe = _data.eventObjects.allEvents[22][i];
						jn["events"]["timeline"][i]["t"] = eventKeyframe.eventTime.ToString();
						jn["events"]["timeline"][i]["x"] = eventKeyframe.eventValues[0].ToString();
						jn["events"]["timeline"][i]["y"] = eventKeyframe.eventValues[1].ToString();
						jn["events"]["timeline"][i]["z"] = eventKeyframe.eventValues[2].ToString();
						jn["events"]["timeline"][i]["x2"] = eventKeyframe.eventValues[3].ToString();
						jn["events"]["timeline"][i]["y2"] = eventKeyframe.eventValues[4].ToString();
						jn["events"]["timeline"][i]["z2"] = eventKeyframe.eventValues[5].ToString();
						jn["events"]["timeline"][i]["x3"] = eventKeyframe.eventValues[6].ToString();
						if (eventKeyframe.curveType.Name != "Linear")
						{
							jn["events"]["timeline"][i]["ct"] = eventKeyframe.curveType.Name.ToString();
						}
						if (eventKeyframe.random != 0)
						{
							jn["events"]["timeline"][i]["r"] = eventKeyframe.random.ToString();
							jn["events"]["timeline"][i]["rx"] = eventKeyframe.eventRandomValues[0].ToString();
							jn["events"]["timeline"][i]["ry"] = eventKeyframe.eventRandomValues[1].ToString();
						}
					}
				}
				if (_data.eventObjects.allEvents.Count > 23)
				{
					for (int i = 0; i < _data.eventObjects.allEvents[23].Count(); i++)
					{
						var eventKeyframe = _data.eventObjects.allEvents[23][i];
						jn["events"]["player"][i]["t"] = eventKeyframe.eventTime.ToString();
						jn["events"]["player"][i]["x"] = eventKeyframe.eventValues[0].ToString();
						jn["events"]["player"][i]["y"] = eventKeyframe.eventValues[1].ToString();
						jn["events"]["player"][i]["z"] = eventKeyframe.eventValues[2].ToString();
						jn["events"]["player"][i]["x2"] = eventKeyframe.eventValues[3].ToString();
						if (eventKeyframe.curveType.Name != "Linear")
						{
							jn["events"]["player"][i]["ct"] = eventKeyframe.curveType.Name.ToString();
						}
						if (eventKeyframe.random != 0)
						{
							jn["events"]["player"][i]["r"] = eventKeyframe.random.ToString();
							jn["events"]["player"][i]["rx"] = eventKeyframe.eventRandomValues[0].ToString();
							jn["events"]["player"][i]["ry"] = eventKeyframe.eventRandomValues[1].ToString();
						}
					}
				}
				if (_data.eventObjects.allEvents.Count > 24)
				{
					for (int i = 0; i < _data.eventObjects.allEvents[24].Count(); i++)
					{
						var eventKeyframe = _data.eventObjects.allEvents[24][i];
						jn["events"]["follow_player"][i]["t"] = eventKeyframe.eventTime.ToString();
						jn["events"]["follow_player"][i]["x"] = eventKeyframe.eventValues[0].ToString();
						jn["events"]["follow_player"][i]["y"] = eventKeyframe.eventValues[1].ToString();
						jn["events"]["follow_player"][i]["z"] = eventKeyframe.eventValues[2].ToString();
						jn["events"]["follow_player"][i]["x2"] = eventKeyframe.eventValues[3].ToString();
						jn["events"]["follow_player"][i]["y2"] = eventKeyframe.eventValues[4].ToString();
						jn["events"]["follow_player"][i]["z2"] = eventKeyframe.eventValues[5].ToString();
						jn["events"]["follow_player"][i]["x3"] = eventKeyframe.eventValues[6].ToString();
						jn["events"]["follow_player"][i]["y3"] = eventKeyframe.eventValues[7].ToString();
						jn["events"]["follow_player"][i]["z3"] = eventKeyframe.eventValues[8].ToString();
						jn["events"]["follow_player"][i]["x4"] = eventKeyframe.eventValues[9].ToString();
						if (eventKeyframe.curveType.Name != "Linear")
						{
							jn["events"]["follow_player"][i]["ct"] = eventKeyframe.curveType.Name.ToString();
						}
						if (eventKeyframe.random != 0)
						{
							jn["events"]["follow_player"][i]["r"] = eventKeyframe.random.ToString();
							jn["events"]["follow_player"][i]["rx"] = eventKeyframe.eventRandomValues[0].ToString();
							jn["events"]["follow_player"][i]["ry"] = eventKeyframe.eventRandomValues[1].ToString();
						}
					}
				}
				if (_data.eventObjects.allEvents.Count > 25)
				{
					for (int i = 0; i < _data.eventObjects.allEvents[25].Count(); i++)
					{
						var eventKeyframe = _data.eventObjects.allEvents[25][i];
						jn["events"]["audio"][i]["t"] = eventKeyframe.eventTime.ToString();
						jn["events"]["audio"][i]["x"] = eventKeyframe.eventValues[0].ToString();
						jn["events"]["audio"][i]["y"] = eventKeyframe.eventValues[1].ToString();
						if (eventKeyframe.curveType.Name != "Linear")
						{
							jn["events"]["audio"][i]["ct"] = eventKeyframe.curveType.Name.ToString();
						}
						if (eventKeyframe.random != 0)
						{
							jn["events"]["audio"][i]["r"] = eventKeyframe.random.ToString();
							jn["events"]["audio"][i]["rx"] = eventKeyframe.eventRandomValues[0].ToString();
							jn["events"]["audio"][i]["ry"] = eventKeyframe.eventRandomValues[1].ToString();
						}
					}
				}
			}

			Debug.LogFormat("{0}Saving Entire Beatmap", EditorPlugin.className);
			Debug.LogFormat("{0}Path: {1}", EditorPlugin.className, _path);
			RTFile.WriteToFile(_path, jn.ToString());

			yield return new WaitForSeconds(0.5f);

			if (GameObject.Find("BepInEx_Manager").GetComponentByName("PlayerPlugin"))
			{
				var playerPlugin = GameObject.Find("BepInEx_Manager").GetComponentByName("PlayerPlugin");
				var c = playerPlugin.GetType().GetField("className").GetValue(playerPlugin);

				if (c != null)
				{
					playerPlugin.GetType().GetMethod("SaveLocalModels").Invoke(playerPlugin, new object[] { });
				}
			}

			autoSaving = false;

			yield break;
        }

		public static Dictionary<string, DataManager.BeatmapTheme> savedBeatmapThemes = new Dictionary<string, DataManager.BeatmapTheme>();

		public static bool themesLoading = false;

		public static bool autoSaving = false;

		public static void SetAutosave()
		{
			EditorManager.inst.CancelInvoke("AutoSaveLevel");
			inst.CancelInvoke("AutoSaveLevel");
			inst.InvokeRepeating("AutoSaveLevel", ConfigEntries.AutoSaveLoopTime.Value, ConfigEntries.AutoSaveLoopTime.Value);
		}

		public void AutoSaveLevel()
		{
			if (Time.time == 0f || EditorManager.inst.loading)
				return;

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
			string autosavePath = string.Concat(new string[]
			{
				FileManager.GetAppPath(),
				"/",
				GameManager.inst.basePath,
				"autosaves/autosave_",
				DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss"),
				".lsb"
			});
			if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + GameManager.inst.basePath + "autosaves"))
			{
				Directory.CreateDirectory(RTFile.ApplicationDirectory + GameManager.inst.basePath + "autosaves");
			}
			EditorManager.inst.DisplayNotification("Autosaving backup!", 2f, EditorManager.NotificationType.Warning, false);
			EditorManager.inst.autosaves.Add(autosavePath);
			while (EditorManager.inst.autosaves.Count > ConfigEntries.AutoSaveLimit.Value)
			{
				File.Delete(EditorManager.inst.autosaves.First());
				EditorManager.inst.autosaves.RemoveAt(0);
			}
			EditorManager.inst.StartCoroutine(DataManager.inst.SaveData(autosavePath));

			autoSaving = true;
		}

		public static IEnumerator LoadThemes()
		{
			themesLoading = true;
			//float delay = 0f;
			var dataManager = DataManager.inst;
			var fileManager = FileManager.inst;
			dataManager.CustomBeatmapThemes.Clear();
			dataManager.BeatmapThemeIDToIndex.Clear();
			dataManager.BeatmapThemeIndexToID.Clear();
			int num = 0;
			foreach (DataManager.BeatmapTheme beatmapTheme in DataManager.inst.BeatmapThemes)
			{
				//yield return new WaitForSeconds(delay);
				dataManager.BeatmapThemeIDToIndex.Add(num, num);
				dataManager.BeatmapThemeIndexToID.Add(num, num);
				//delay += 0.0001f;
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
				//yield return new WaitForSeconds(delay);
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
				//delay += 0.0001f;
			}
			themesLoading = false;
			yield break;
		}

		public static IEnumerator UpdatePrefabs()
		{
			PrefabEditor.inst.LoadedPrefabs.Clear();
			PrefabEditor.inst.LoadedPrefabsFiles.Clear();
			Objects.externalPrefabs.Clear();
			yield return inst.StartCoroutine(LoadExternalPrefabs(PrefabEditor.inst));
			PrefabEditor.inst.ReloadExternalPrefabsInPopup();
			EditorManager.inst.DisplayNotification("Updated external prefabs!", 2f, EditorManager.NotificationType.Success, false);
			yield break;
		}

		public static IEnumerator LoadExternalPrefabs(PrefabEditor __instance)
		{
			var folders = FileManager.inst.GetFileList(EditorPlugin.prefabListPath, "lsp");
			while (folders.Count <= 0)
				yield return null;
			Objects.externalPrefabs.Clear();
			foreach (var lsFile in folders)
			{
				var jn = JSON.Parse(FileManager.inst.LoadJSONFileRaw(lsFile.FullPath));
				var _objects = new List<BeatmapObject>();
				for (int aIndex = 0; aIndex < jn["objects"].Count; ++aIndex)
				{
					inst.StartCoroutine(Parser.ParseObject(jn["objects"][aIndex], delegate (BeatmapObject _beatmapObject)
					{
						_objects.Add(_beatmapObject);
					}));
					//_objects.Add(DataManager.GameData.BeatmapObject.ParseGameObject(jsonNode["objects"][aIndex]));
				}
				var _prefabObjects = new List<DataManager.GameData.PrefabObject>();
				for (int aIndex = 0; aIndex < jn["prefab_objects"].Count; ++aIndex)
				{
					_prefabObjects.Add(DataManager.inst.gameData.ParsePrefabObject(jn["prefab_objects"][aIndex]));
				}

				var prefab = new DataManager.GameData.Prefab(jn["name"], jn["type"].AsInt, jn["offset"].AsFloat, _objects, _prefabObjects);

				if (jn["id"] != null)
					prefab.ID = jn["id"];
				else
					prefab.ID = LSText.randomString(16);

				var modPrefab = new Objects.Prefab(prefab);

				Objects.externalPrefabs.Add(modPrefab);
				for (int i = 0; i < jn["objects"].Count; i++)
					__instance.StartCoroutine(Parser.ParsePrefabModifiers(jn["objects"][i], modPrefab));

				__instance.LoadedPrefabs.Add(prefab);
				__instance.LoadedPrefabsFiles.Add(lsFile.FullPath);
			}
		}

		public static IEnumerator LoadLevel(EditorManager __instance, string _levelName)
		{
			var objectManager = ObjectManager.inst;
			var objEditor = ObjEditor.inst;
			var gameManager = GameManager.inst;
			var dataManager = DataManager.inst;

			__instance.loading = true;

			SetLayer(0);

			objectManager.PurgeObjects();
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
			if (RTFile.FileExists(text + "level.ogg"))
			{
				yield return inst.StartCoroutine(FileManager.inst.LoadMusicFile(text + "level.ogg", delegate (AudioClip _song)
				{
					_song.name = _levelName;
					if (_song)
					{
						song = _song;
					}
				}));
			}
			else if (RTFile.FileExists(text + "level.wav"))
			{
				yield return inst.StartCoroutine(FileManager.inst.LoadMusicFile(text + "level.wav", delegate (AudioClip _song)
				{
					_song.name = _levelName;
					if (_song)
					{
						song = _song;
					}
				}));
			}

			Debug.LogFormat("{0}Parsing level data for {1}...", EditorPlugin.className, _levelName);
			gameManager.gameState = GameManager.State.Parsing;
			fileInfo.text = "Parsing Level Data for [" + _levelName + "]";
			if (!string.IsNullOrEmpty(rawJSON) && !string.IsNullOrEmpty(rawMetadataJSON))
			{
				dataManager.ParseMetadata(rawMetadataJSON, true);
				rawJSON = dataManager.gameData.UpdateBeatmap(rawJSON, DataManager.inst.metaData.beatmap.game_version);
				dataManager.gameData.eventObjects = new DataManager.GameData.EventObjects();
				inst.StartCoroutine(Parser.ParseBeatmap(rawJSON, true));

				if (dataManager.metaData.beatmap.workshop_id == -1)
					dataManager.metaData.beatmap.workshop_id = UnityEngine.Random.Range(0, int.MaxValue);
			}

			if (GameObject.Find("BepInEx_Manager").GetComponentByName("PlayerPlugin"))
            {
				var playerPlugin = GameObject.Find("BepInEx_Manager").GetComponentByName("PlayerPlugin");
				var c = playerPlugin.GetType().GetField("className").GetValue(playerPlugin);

				if (c != null)
                {
					playerPlugin.GetType().GetMethod("LoadIndexes").Invoke(playerPlugin, new object[] { });
					playerPlugin.GetType().GetMethod("StartRespawnPlayers").Invoke(playerPlugin, new object[] { });
                }
			}

			fileInfo.text = "Loading Themes for [" + _levelName + "]";
			Debug.LogFormat("{0}Loading themes for {1}...", EditorPlugin.className, _levelName);
			yield return inst.StartCoroutine(LoadThemes());
			float delayTheme = 0f;
			while (themesLoading)
			{
				yield return new WaitForSeconds(delayTheme);
				delayTheme += 0.0001f;
			}

			Debug.LogFormat("{0}Music is null: ", EditorPlugin.className, song == null);

			fileInfo.text = "Playing Music for [" + _levelName + "]\n\nIf it doesn't, then something went wrong!";
			AudioManager.inst.PlayMusic(null, song, true, 0f, true);
			inst.StartCoroutine((IEnumerator)AccessTools.Method(typeof(EditorManager), "SpawnPlayersWithDelay").Invoke(EditorManager.inst, new object[] { 0.2f }));
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
			if (__instance.layer == 5)
			{
				CheckpointEditor.inst.CreateCheckpoints();
			}
			else
			{
				CheckpointEditor.inst.CreateGhostCheckpoints();
			}
			fileInfo.text = "Updating states for [" + _levelName + "]";
			DiscordController.inst.OnStateChange("Editing: " + DataManager.inst.metaData.song.title);
			objEditor.CreateTimelineObjects();
			objectManager.updateObjects();
			EventEditor.inst.CreateEventObjects();
			BackgroundManager.inst.UpdateBackgrounds();
			gameManager.UpdateTheme();
			MarkerEditor.inst.CreateMarkers();
			if (GameObject.Find("BepInEx_Manager").GetComponentByName("EventsCorePlugin"))
            {
				inst.StartCoroutine(FixEvents());
            }
			else
            {
				EventManager.inst.updateEvents();
			}

			//SetLastSaved();

			objEditor.CreateTimelineObjects();
			objEditor.RenderTimelineObjects();
			objEditor.SetCurrentObj(new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Object, 0));

			__instance.HideDialog("File Info Popup");
			__instance.CancelInvoke("LoadingIconUpdate");

			gameManager.ResetCheckpoints(true);
			gameManager.gameState = GameManager.State.Playing;
			__instance.DisplayNotification(_levelName + " Level Loaded", 2f, EditorManager.NotificationType.Success, false);
			__instance.UpdatePlayButton();
			__instance.hasLoadedLevel = true;

			if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + GameManager.inst.basePath + "autosaves"))
			{
				Directory.CreateDirectory(RTFile.ApplicationDirectory + GameManager.inst.basePath + "autosaves");
			}

			// Change this to instead add the files to EditorManager.inst.autosaves
			{
				string[] files = Directory.GetFiles(FileManager.GetAppPath() + "/" + GameManager.inst.basePath, "autosaves/autosave_*.lsb", SearchOption.TopDirectoryOnly);
				files.ToList().Sort();
				//int num = 0;
				//foreach (string text2 in files)
				//{
				//	if (num != files.Count() - 1)
				//	{
				//		File.Delete(text2);
				//	}
				//	num++;
				//}

				foreach (var file in files)
                {
					__instance.autosaves.Add(file);
                }

				SetAutosave();
			}


			Triggers.AddEventTrigger(timeIF.gameObject, new List<EventTrigger.Entry> { Triggers.ScrollDelta(timeIF, 0.1f, 10f, false, new List<float> { 0f, AudioManager.inst.CurrentAudioSource.clip.length }) });

			{
				if (ConfigEntries.LevelLoadsSavedTime.Value == true)
				{
					AudioManager.inst.CurrentAudioSource.time = DataManager.inst.gameData.beatmapData.editorData.timelinePos;
				}
				if (ConfigEntries.LevelPausesOnStart.Value == true)
				{
					AudioManager.inst.CurrentAudioSource.Pause();
					__instance.UpdatePlayButton();
				}

				if (RTFile.FileExists(RTFile.ApplicationDirectory + text + "/editor.lse"))
				{
					string rawProfileJSON = FileManager.inst.LoadJSONFile(text + "/editor.lse");

					JSONNode jsonnode = JSON.Parse(rawProfileJSON);

					if (float.TryParse(jsonnode["timeline"]["z"], out float z))
					{
						EditorManager.inst.zoomSlider.value = z;
					}
					else if (jsonnode["timeline"]["z"] != null)
					{
						EditorManager.inst.zoomSlider.value = jsonnode["timeline"]["z"];
					}

					if (float.TryParse(jsonnode["timeline"]["tsc"], out float tsc))
					{
						GameObject.Find("Editor Systems/Editor GUI/sizer/main/whole-timeline/Scrollbar").GetComponent<Scrollbar>().value = tsc;
					}
					else if (jsonnode["timeline"]["tsc"] != null)
					{
						GameObject.Find("Editor Systems/Editor GUI/sizer/main/whole-timeline/Scrollbar").GetComponent<Scrollbar>().value = jsonnode["timeline"]["tsc"];
					}

					if (int.TryParse(jsonnode["timeline"]["l"], out int l))
					{
						if (l != 5)
						{
							SetLayer(l);
						}
						else
						{
							SetLayer(0);
						}
					}
					else if (jsonnode["timeline"]["l"] != null)
					{
						if ((int)jsonnode["timeline"]["l"] != 5)
						{
							SetLayer(jsonnode["timeline"]["l"]);
						}
						else
						{
							SetLayer(0);
						}
					}

					if (float.TryParse(jsonnode["editor"]["t"], out float t))
					{
						EditorPlugin.timeEdit = t;
					}
					else if (jsonnode["editor"]["t"] != null)
					{
						EditorPlugin.timeEdit = jsonnode["editor"]["t"];
					}

					if (int.TryParse(jsonnode["editor"]["a"], out int a))
					{
						EditorPlugin.openAmount = a;
					}
					else if (jsonnode["editor"]["a"] != null)
					{
						EditorPlugin.openAmount = jsonnode["editor"]["a"];
					}

					EditorPlugin.openAmount += 1;

					if (bool.TryParse(jsonnode["misc"]["sn"], out bool sn))
					{
						SettingEditor.inst.SnapActive = sn;
					}
					else if (jsonnode["misc"]["sn"] != null)
					{
						SettingEditor.inst.SnapActive = jsonnode["misc"]["sn"];
					}

					SettingEditor.inst.SnapBPM = DataManager.inst.metaData.song.BPM;
				}
				else
				{
					EditorPlugin.timeEdit = 0;
				}
			}

			if (ModCompatibility.sharedFunctions.ContainsKey("EditorOnLoadLevel"))
				((Action)ModCompatibility.sharedFunctions["EditorOnLoadLevel"])();

			__instance.loading = false;

			yield break;
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

			bool setNew = false;
			int num = 0;
			string p = RTFile.ApplicationDirectory + EditorPlugin.levelListSlash + __instance.newLevelName;
			while (RTFile.DirectoryExists(p))
			{
				p = RTFile.ApplicationDirectory + EditorPlugin.levelListSlash + __instance.newLevelName + " - " + num.ToString();
				num += 1;
				setNew = true;

			}
			if (setNew)
				__instance.newLevelName += " - " + num.ToString();

			if (RTFile.DirectoryExists(RTFile.ApplicationDirectory + EditorPlugin.levelListSlash + __instance.newLevelName))
			{
				__instance.DisplayNotification("The level you are trying to create already exists.", 2f, EditorManager.NotificationType.Error, false);
				return;
			}
			Directory.CreateDirectory(RTFile.ApplicationDirectory + EditorPlugin.levelListSlash + __instance.newLevelName);
			if (__instance.newAudioFile.ToLower().Contains(".ogg"))
			{
				string destFileName = RTFile.ApplicationDirectory + EditorPlugin.levelListSlash + __instance.newLevelName + "/level.ogg";
				File.Copy(__instance.newAudioFile, destFileName, true);
			}
			inst.StartCoroutine(SaveData(RTFile.ApplicationDirectory + EditorPlugin.levelListSlash + __instance.newLevelName + "/level.lsb", CreateBaseBeatmap()));
			var dataManager = DataManager.inst;
			dataManager.metaData = new DataManager.MetaData();
			dataManager.metaData.beatmap.game_version = "4.1.16";
			dataManager.metaData.song.title = __instance.newLevelName;
			dataManager.metaData.creator.steam_name = SteamWrapper.inst.user.displayName;
			dataManager.metaData.creator.steam_id = SteamWrapper.inst.user.id;
			dataManager.metaData.beatmap.workshop_id = UnityEngine.Random.Range(0, int.MaxValue);

			dataManager.SaveMetadata(RTFile.ApplicationDirectory + EditorPlugin.levelListSlash + __instance.newLevelName + "/metadata.lsb");
			inst.StartCoroutine(LoadLevel(__instance, __instance.newLevelName));
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

            #region Events
            //Move
            {
                List<DataManager.GameData.EventKeyframe> list = new List<DataManager.GameData.EventKeyframe>();
				DataManager.GameData.EventKeyframe eventKeyframe = new DataManager.GameData.EventKeyframe();
				eventKeyframe.eventTime = 0f;
				eventKeyframe.SetEventValues(new float[2]);
				list.Add(eventKeyframe);

				gameData.eventObjects.allEvents[0] = list;
			}

			//Zoom
			{
				List<DataManager.GameData.EventKeyframe> list2 = new List<DataManager.GameData.EventKeyframe>();
				DataManager.GameData.EventKeyframe eventKeyframe2 = new DataManager.GameData.EventKeyframe();
				eventKeyframe2.eventTime = 0f;
				DataManager.GameData.EventKeyframe eventKeyframe3 = eventKeyframe2;
				float[] array = new float[2];
				array[0] = 20f;
				eventKeyframe3.SetEventValues(array);
				list2.Add(eventKeyframe2);

				gameData.eventObjects.allEvents[1] = list2;
			}

			//Rotate
			{
				List<DataManager.GameData.EventKeyframe> list3 = new List<DataManager.GameData.EventKeyframe>();
				DataManager.GameData.EventKeyframe eventKeyframe4 = new DataManager.GameData.EventKeyframe();
				eventKeyframe4.eventTime = 0f;
				eventKeyframe4.SetEventValues(new float[2]);
				list3.Add(eventKeyframe4);

				gameData.eventObjects.allEvents[2] = list3;
			}

			//Shake
			{
				List<DataManager.GameData.EventKeyframe> list4 = new List<DataManager.GameData.EventKeyframe>();
				DataManager.GameData.EventKeyframe eventKeyframe5 = new DataManager.GameData.EventKeyframe();
				eventKeyframe5.eventTime = 0f;
				eventKeyframe5.SetEventValues(new float[3]
					{
						0f,
						1f,
						1f
					});
				list4.Add(eventKeyframe5);

				gameData.eventObjects.allEvents[3] = list4;
			}

			//Theme
			{
				List<DataManager.GameData.EventKeyframe> list5 = new List<DataManager.GameData.EventKeyframe>();
				DataManager.GameData.EventKeyframe eventKeyframe6 = new DataManager.GameData.EventKeyframe();
				eventKeyframe6.eventTime = 0f;
				eventKeyframe6.SetEventValues(new float[2]);
				list5.Add(eventKeyframe6);

				gameData.eventObjects.allEvents[4] = list5;
			}

			//Chromatic
			{
				List<DataManager.GameData.EventKeyframe> list6 = new List<DataManager.GameData.EventKeyframe>();
				DataManager.GameData.EventKeyframe eventKeyframe7 = new DataManager.GameData.EventKeyframe();
				eventKeyframe7.eventTime = 0f;
				eventKeyframe7.SetEventValues(new float[2]);
				list6.Add(eventKeyframe7);

				gameData.eventObjects.allEvents[5] = list6;
			}

			//Bloom
			{
				List<DataManager.GameData.EventKeyframe> list7 = new List<DataManager.GameData.EventKeyframe>();
				DataManager.GameData.EventKeyframe eventKeyframe8 = new DataManager.GameData.EventKeyframe();
				eventKeyframe8.eventTime = 0f;
				eventKeyframe8.SetEventValues(new float[5]
					{
						0f,
						7f,
						1f,
						0f,
						18f
					});
				list7.Add(eventKeyframe8);

				gameData.eventObjects.allEvents[6] = list7;
			}

			//Vignette
			{
				List<DataManager.GameData.EventKeyframe> list8 = new List<DataManager.GameData.EventKeyframe>();
				DataManager.GameData.EventKeyframe eventKeyframe9 = new DataManager.GameData.EventKeyframe();
				eventKeyframe9.eventTime = 0f;
				eventKeyframe9.SetEventValues(new float[7]
					{
						0f,
						0f,
						0f,
						0f,
						0f,
						0f,
						18f
					});
				list8.Add(eventKeyframe9);

				gameData.eventObjects.allEvents[7] = list8;
			}

			//Lens
			{
				List<DataManager.GameData.EventKeyframe> list9 = new List<DataManager.GameData.EventKeyframe>();
				DataManager.GameData.EventKeyframe eventKeyframe10 = new DataManager.GameData.EventKeyframe();
				eventKeyframe10.eventTime = 0f;
				eventKeyframe10.SetEventValues(new float[6]
					{
						0f,
						0f,
						0f,
						1f,
						1f,
						1f
					});
				list9.Add(eventKeyframe10);

				gameData.eventObjects.allEvents[8] = list9;
			}

			//Grain
			{
				List<DataManager.GameData.EventKeyframe> list10 = new List<DataManager.GameData.EventKeyframe>();
				DataManager.GameData.EventKeyframe eventKeyframe11 = new DataManager.GameData.EventKeyframe();
				eventKeyframe11.eventTime = 0f;
				eventKeyframe11.SetEventValues(new float[3]);
				list10.Add(eventKeyframe11);

				gameData.eventObjects.allEvents[9] = list10;
			}

			//ColorGrading
			if (gameData.eventObjects.allEvents.Count > 10)
			{
				List<DataManager.GameData.EventKeyframe> list11 = new List<DataManager.GameData.EventKeyframe>();
				DataManager.GameData.EventKeyframe eventKeyframe12 = new DataManager.GameData.EventKeyframe();
				eventKeyframe12.eventTime = 0f;
				eventKeyframe12.SetEventValues(new float[9]);
				list11.Add(eventKeyframe12);

				gameData.eventObjects.allEvents[10] = list11;
			}

			//Ripples
			if (gameData.eventObjects.allEvents.Count > 11)
			{
				List<DataManager.GameData.EventKeyframe> list12 = new List<DataManager.GameData.EventKeyframe>();
				DataManager.GameData.EventKeyframe eventKeyframe13 = new DataManager.GameData.EventKeyframe();
				eventKeyframe13.eventTime = 0f;
				eventKeyframe13.SetEventValues(new float[5]
					{
						0f,
						0f,
						1f,
						0f,
						0f
					});
				list12.Add(eventKeyframe13);

				gameData.eventObjects.allEvents[11] = list12;
			}

			//RadialBlur
			if (gameData.eventObjects.allEvents.Count > 12)
			{
				List<DataManager.GameData.EventKeyframe> list13 = new List<DataManager.GameData.EventKeyframe>();
				DataManager.GameData.EventKeyframe eventKeyframe14 = new DataManager.GameData.EventKeyframe();
				eventKeyframe14.eventTime = 0f;
				eventKeyframe14.SetEventValues(new float[2]
					{
						0f,
						6f
					});
				list13.Add(eventKeyframe14);

				gameData.eventObjects.allEvents[12] = list13;
			}

			//ColorSplit
			if (gameData.eventObjects.allEvents.Count > 13)
			{
				List<DataManager.GameData.EventKeyframe> list14 = new List<DataManager.GameData.EventKeyframe>();
				DataManager.GameData.EventKeyframe eventKeyframe15 = new DataManager.GameData.EventKeyframe();
				eventKeyframe15.eventTime = 0f;
				eventKeyframe15.SetEventValues(new float[2]);
				list14.Add(eventKeyframe15);

				gameData.eventObjects.allEvents[13] = list14;
			}

			//Camera Offset
			if (gameData.eventObjects.allEvents.Count > 14)
			{
				List<DataManager.GameData.EventKeyframe> list14 = new List<DataManager.GameData.EventKeyframe>();
				DataManager.GameData.EventKeyframe eventKeyframe15 = new DataManager.GameData.EventKeyframe();
				eventKeyframe15.eventTime = 0f;
				eventKeyframe15.SetEventValues(new float[2]);
				list14.Add(eventKeyframe15);

				gameData.eventObjects.allEvents[14] = list14;
			}

			//Gradient
			if (gameData.eventObjects.allEvents.Count > 15)
			{
				List<DataManager.GameData.EventKeyframe> list14 = new List<DataManager.GameData.EventKeyframe>();
				DataManager.GameData.EventKeyframe eventKeyframe15 = new DataManager.GameData.EventKeyframe();
				eventKeyframe15.eventTime = 0f;
				eventKeyframe15.SetEventValues(new float[5]
					{
						0f,
						0f,
						18f,
						18f,
						0f
					});
				list14.Add(eventKeyframe15);

				gameData.eventObjects.allEvents[15] = list14;
			}

			//DoubleVision
			if (gameData.eventObjects.allEvents.Count > 16)
			{
				List<DataManager.GameData.EventKeyframe> list14 = new List<DataManager.GameData.EventKeyframe>();
				DataManager.GameData.EventKeyframe eventKeyframe15 = new DataManager.GameData.EventKeyframe();
				eventKeyframe15.eventTime = 0f;
				eventKeyframe15.SetEventValues(new float[2]);
				list14.Add(eventKeyframe15);

				gameData.eventObjects.allEvents[16] = list14;
			}

			//ScanLines
			if (gameData.eventObjects.allEvents.Count > 17)
			{
				List<DataManager.GameData.EventKeyframe> list14 = new List<DataManager.GameData.EventKeyframe>();
				DataManager.GameData.EventKeyframe eventKeyframe15 = new DataManager.GameData.EventKeyframe();
				eventKeyframe15.eventTime = 0f;
				eventKeyframe15.SetEventValues(new float[3]);
				list14.Add(eventKeyframe15);

				gameData.eventObjects.allEvents[17] = list14;
			}

			//Blur
			if (gameData.eventObjects.allEvents.Count > 18)
			{
				List<DataManager.GameData.EventKeyframe> list14 = new List<DataManager.GameData.EventKeyframe>();
				DataManager.GameData.EventKeyframe eventKeyframe15 = new DataManager.GameData.EventKeyframe();
				eventKeyframe15.eventTime = 0f;
				eventKeyframe15.SetEventValues(new float[2]
					{
						0f,
						6f
					});
				list14.Add(eventKeyframe15);

				gameData.eventObjects.allEvents[18] = list14;
			}

			//Pixelize
			if (gameData.eventObjects.allEvents.Count > 19)
			{
				List<DataManager.GameData.EventKeyframe> list14 = new List<DataManager.GameData.EventKeyframe>();
				DataManager.GameData.EventKeyframe eventKeyframe15 = new DataManager.GameData.EventKeyframe();
				eventKeyframe15.eventTime = 0f;
				eventKeyframe15.SetEventValues(new float[2]);
				list14.Add(eventKeyframe15);

				gameData.eventObjects.allEvents[19] = list14;
			}

			//BG
			if (gameData.eventObjects.allEvents.Count > 20)
			{
				List<DataManager.GameData.EventKeyframe> list14 = new List<DataManager.GameData.EventKeyframe>();
				DataManager.GameData.EventKeyframe eventKeyframe15 = new DataManager.GameData.EventKeyframe();
				eventKeyframe15.eventTime = 0f;
				eventKeyframe15.SetEventValues(new float[2]
					{
						18f,
						0f
					});
				list14.Add(eventKeyframe15);

				gameData.eventObjects.allEvents[20] = list14;
			}

			//Invert
			if (gameData.eventObjects.allEvents.Count > 21)
			{
				List<DataManager.GameData.EventKeyframe> list14 = new List<DataManager.GameData.EventKeyframe>();
				DataManager.GameData.EventKeyframe eventKeyframe15 = new DataManager.GameData.EventKeyframe();
				eventKeyframe15.eventTime = 0f;
				eventKeyframe15.SetEventValues(new float[2]
				{
					0f,
					0f
				});
				list14.Add(eventKeyframe15);

				gameData.eventObjects.allEvents[21] = list14;
			}

			//Timeline
			if (gameData.eventObjects.allEvents.Count > 22)
			{
				List<DataManager.GameData.EventKeyframe> list14 = new List<DataManager.GameData.EventKeyframe>();
				DataManager.GameData.EventKeyframe eventKeyframe15 = new DataManager.GameData.EventKeyframe();
				eventKeyframe15.eventTime = 0f;
				eventKeyframe15.SetEventValues(new float[7]
				{
					0f,
					0f,
					-342f,
					1f,
					1f,
					0f,
					18f
				});
				list14.Add(eventKeyframe15);

				gameData.eventObjects.allEvents[22] = list14;
			}

			//Player
			if (gameData.eventObjects.allEvents.Count > 23)
			{
				List<DataManager.GameData.EventKeyframe> list14 = new List<DataManager.GameData.EventKeyframe>();
				DataManager.GameData.EventKeyframe eventKeyframe15 = new DataManager.GameData.EventKeyframe();
				eventKeyframe15.eventTime = 0f;
				eventKeyframe15.SetEventValues(new float[4]
					{
						0f,
						0f,
						0f,
						0f
					});
				list14.Add(eventKeyframe15);

				gameData.eventObjects.allEvents[23] = list14;
			}

			//Follow Player
			if (gameData.eventObjects.allEvents.Count > 24)
			{
				List<DataManager.GameData.EventKeyframe> list14 = new List<DataManager.GameData.EventKeyframe>();
				DataManager.GameData.EventKeyframe eventKeyframe15 = new DataManager.GameData.EventKeyframe();
				eventKeyframe15.eventTime = 0f;
				eventKeyframe15.SetEventValues(new float[10]
				{
					0f,
					0f,
					0f,
					0.5f,
					0f,
					9999f,
					-9999f,
					9999f,
					-9999f,
					1f
				});
				list14.Add(eventKeyframe15);

				gameData.eventObjects.allEvents[24] = list14;
			}

			//Audio
			if (gameData.eventObjects.allEvents.Count > 25)
			{
				List<DataManager.GameData.EventKeyframe> list14 = new List<DataManager.GameData.EventKeyframe>();
				DataManager.GameData.EventKeyframe eventKeyframe15 = new DataManager.GameData.EventKeyframe();
				eventKeyframe15.eventTime = 0f;
				eventKeyframe15.SetEventValues(new float[2]
				{
					1f,
					1f
				});
				list14.Add(eventKeyframe15);

				gameData.eventObjects.allEvents[25] = list14;
			}

            #endregion

            for (int i = 0; i < 25; i++)
			{
				DataManager.GameData.BackgroundObject backgroundObject = new DataManager.GameData.BackgroundObject();
				backgroundObject.name = "bg - " + i;
				if (UnityEngine.Random.value > 0.5f)
				{
					backgroundObject.scale = new Vector2(UnityEngine.Random.Range(2, 8), UnityEngine.Random.Range(2, 8));
				}
				else
				{
					float num = UnityEngine.Random.Range(2, 6);
					backgroundObject.scale = new Vector2(num, num);
				}
				backgroundObject.pos = new Vector2(UnityEngine.Random.Range(-48, 48), UnityEngine.Random.Range(-32, 32));
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

				var bg = new Objects.BackgroundObject(backgroundObject);
				bg.shape = Objects.Shapes3D[UnityEngine.Random.Range(0, 27)];

				Objects.backgroundObjects.Add(bg);
			}

            BeatmapObject beatmapObject = CreateNewBeatmapObject(0.5f, false);
			List<DataManager.GameData.EventKeyframe> objectEvents = beatmapObject.events[0];
			float time = 4f;
			float[] array2 = new float[3];
			array2[0] = 10f;
			objectEvents.Add(new DataManager.GameData.EventKeyframe(time, array2, new float[2], 0));
			beatmapObject.name = "\"Default object cameo\" -Viral Mecha";
			beatmapObject.autoKillType = BeatmapObject.AutoKillType.LastKeyframeOffset;
			beatmapObject.autoKillOffset = 4f;
			beatmapObject.editorData.Layer = 0;
			gameData.beatmapObjects.Add(beatmapObject);
			return gameData;
		}

		#endregion

        #region Timeline Textures

        public static IEnumerator AssignTimelineTexture()
		{
			//int num = Mathf.Clamp((int)AudioManager.inst.CurrentAudioSource.clip.length * 48, 100, 15000);
			int num = Mathf.Clamp((int)AudioManager.inst.CurrentAudioSource.clip.length * 48, 100, 15000);
			Texture2D waveform = null;

			if (ConfigEntries.WaveformMode.Value == WaveformType.Legacy)
				yield return inst.StartCoroutine(GetWaveformTextureAdvanced(AudioManager.inst.CurrentAudioSource.clip, num, 300, ConfigEntries.WaveformBGColor.Value, ConfigEntries.WaveformTopColor.Value, ConfigEntries.WaveformBottomColor.Value, delegate (Texture2D _tex) { waveform = _tex; }));
			if (ConfigEntries.WaveformMode.Value == WaveformType.Beta)
				yield return inst.StartCoroutine(GetWaveformTexture(AudioManager.inst.CurrentAudioSource.clip, num, 300, ConfigEntries.WaveformBGColor.Value, ConfigEntries.WaveformTopColor.Value, delegate (Texture2D _tex) { waveform = _tex; }));
			if (ConfigEntries.WaveformMode.Value == WaveformType.BetaFast)
				yield return inst.StartCoroutine(BetaFast(AudioManager.inst.CurrentAudioSource.clip, 1f, num, 300, ConfigEntries.WaveformBGColor.Value, ConfigEntries.WaveformTopColor.Value, delegate (Texture2D _tex) { waveform = _tex; }));
			if (ConfigEntries.WaveformMode.Value == WaveformType.LegacyFast)
				yield return inst.StartCoroutine(LegacyFast(AudioManager.inst.CurrentAudioSource.clip, 1f, num, 300, ConfigEntries.WaveformBGColor.Value, ConfigEntries.WaveformTopColor.Value, ConfigEntries.WaveformBottomColor.Value, delegate (Texture2D _tex) { waveform = _tex; }));

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

		#endregion

		#region Misc Functions

		public static void SetKey(ConfigEntry<KeyCode> configEntry)
        {
			inst.selectingKey = true;
			inst.keyToSet = configEntry;
        }

		public static IEnumerator RespawnPlayersDefault()
		{
			if (InputDataManager.inst.players.Count > 0)
            {
				foreach (var player in InputDataManager.inst.players)
				{
					if (player.player != null)
						Destroy(player.player.gameObject);
				}
				yield return new WaitForSeconds(0.1f);
				GameManager.inst.SpawnPlayers(DataManager.inst.gameData.beatmapData.checkpoints[0].pos);
			}
			yield break;
        }

		public static IEnumerator FixEvents()
		{
			yield return new WaitForSeconds(0.4f);
			EventManager.inst.updateEvents();
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

		public static byte[] password = LSEncryption.AES_Encrypt(new byte[] { 9, 5, 7, 6, 4, 38, 6, 4, 3, 66, 43, 6, 47, 8, 54, 6 }, new byte[] { 99, 53, 43, 36, 43, 65, 43, 45 });

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

			string path = RTFile.ApplicationDirectory + EditorPlugin.levelListSlash + EditorManager.inst.currentLoadedLevel + "/level.ogg";
			var songBytes = File.ReadAllBytes(path);
			var encryptedSong = LSEncryption.AES_Encrypt(songBytes, password);
			File.WriteAllBytes(RTFile.ApplicationDirectory + EditorPlugin.levelListSlash + EditorManager.inst.currentLoadedLevel + "/song.lsen", encryptedSong);
			yield break;
		}

		public static void BringToObject()
		{
			AudioManager.inst.SetMusicTime(ObjEditor.inst.currentObjectSelection.GetObjectData().StartTime);
			SetLayer(ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer);

			AudioManager.inst.CurrentAudioSource.Pause();
			EditorManager.inst.UpdatePlayButton();
			EditorManager.inst.StartCoroutine(EditorManager.inst.UpdateTimelineScrollRect(0f, AudioManager.inst.CurrentAudioSource.time / AudioManager.inst.CurrentAudioSource.clip.length));
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
					backgroundObject.reactiveType = (DataManager.GameData.BackgroundObject.ReactiveType)UnityEngine.Random.Range(0, 4);

					backgroundObject.reactiveScale = UnityEngine.Random.Range(0.01f, 0.04f);
				}
				DataManager.inst.gameData.backgroundObjects.Add(backgroundObject);

				var bg = new Objects.BackgroundObject(backgroundObject);

				bg.reactivePosIntensity = new Vector2(UnityEngine.Random.Range(0, 100) > 65 ? UnityEngine.Random.Range(0f, 1f) : 0f, UnityEngine.Random.Range(0, 100) > 65 ? UnityEngine.Random.Range(0f, 1f) : 0f);
				bg.reactiveScaIntensity = new Vector2(UnityEngine.Random.Range(0, 100) > 45 ? UnityEngine.Random.Range(0f, 1f) : 0f, UnityEngine.Random.Range(0, 100) > 45 ? UnityEngine.Random.Range(0f, 1f) : 0f);
				bg.reactiveRotIntensity = UnityEngine.Random.Range(0, 100) > 45 ? UnityEngine.Random.Range(0f, 1f) : 0f;
				bg.reactiveCol = UnityEngine.Random.Range(1, 6);
				bg.shape = Objects.Shapes[UnityEngine.Random.Range(0, Objects.Shapes.Count - 1)];

				Objects.backgroundObjects.Add(bg);
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

				Objects.backgroundObjects.RemoveAt(nooo);
			}

			BackgroundEditor.inst.SetCurrentBackground(0);
			BackgroundManager.inst.UpdateBackgrounds();
			BackgroundEditor.inst.UpdateBackgroundList();

			EditorManager.inst.DisplayNotification("Deleted " + (num - 1).ToString() + " backgrounds!", 2f, EditorManager.NotificationType.Success);
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

		public static void DeleteLevelFunction(string _levelName)
		{
			if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + "recycling"))
			{
				Directory.CreateDirectory(RTFile.ApplicationDirectory + "recycling");
			}
			Directory.Move(RTFile.ApplicationDirectory + EditorPlugin.levelListSlash + _levelName, RTFile.ApplicationDirectory + "recycling/" + _levelName);

			string[] directories = Directory.GetDirectories(RTFile.ApplicationDirectory + "recycling", "*", SearchOption.AllDirectories);
			directories.ToList().Sort();
			foreach (var directory in directories)
			{
				string[] filesDir = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);
				filesDir.ToList().Sort();
			}
		}

		public static void MirrorWatcher()
		{
			var objEditor = ObjEditor.inst;
			var currentObjectSelection = objEditor.currentObjectSelection;
			if (currentObjectSelection.IsObject())
			{
				var beatmapObject = currentObjectSelection.GetObjectData();
				mirrorObject = BeatmapObject.DeepCopy(beatmapObject);
				mirrorObject.name = mirrorObject.name.Replace("left", "26435riht");
				mirrorObject.name = mirrorObject.name.Replace("right", "26435let");
				mirrorObject.name = mirrorObject.name.Replace("Left", "26435Riht");
				mirrorObject.name = mirrorObject.name.Replace("Right", "26435Let");
				mirrorObject.name = mirrorObject.name.Replace("26435riht", "right");
				mirrorObject.name = mirrorObject.name.Replace("26435let", "left");
				mirrorObject.name = mirrorObject.name.Replace("26435Riht", "Right");
				mirrorObject.name = mirrorObject.name.Replace("26435Let", "Left");

				for (int i = 0; i < 3; i++)
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

		//Try to work on sub-folders
		public static void OpenImageSelector()
		{
			if (ObjEditor.inst.currentObjectSelection.IsPrefab() && ObjEditor.inst.currentObjectSelection.GetObjectData().shape != 6)
			{
				return;
			}
			var editorPath = RTFile.ApplicationDirectory + EditorPlugin.levelListSlash + EditorManager.inst.currentLoadedLevel;
			string jpgFile = FileBrowser.OpenSingleFile("Select an image!", editorPath, new string[] { "png", "jpg" });
			Debug.LogFormat("{0}Selected file: {1}", EditorPlugin.className, jpgFile);
			if (!string.IsNullOrEmpty(jpgFile))
			{
				string jpgFileLocation = editorPath + "/" + Path.GetFileName(jpgFile);
				Debug.LogFormat("{0}jpgFileLocation: {1}", EditorPlugin.className, jpgFileLocation);

				var levelPath = jpgFile.Replace("\\", "/").Replace(editorPath + "/", "");
				Debug.LogFormat("{0}levelPath: {1}", EditorPlugin.className, levelPath);

				if (!RTFile.FileExists(jpgFileLocation) && !jpgFile.Replace("\\", "/").Contains(editorPath))
				{
					File.Copy(jpgFile, jpgFileLocation);
					Debug.LogFormat("{0}Copied file to : {1}", EditorPlugin.className, jpgFileLocation);
				}
				else
                {
					jpgFileLocation = editorPath + "/" + levelPath;
				}
				Debug.LogFormat("{0}jpgFileLocation: {1}", EditorPlugin.className, jpgFileLocation);
				ObjEditor.inst.currentObjectSelection.GetObjectData().text = jpgFileLocation.Replace(jpgFileLocation.Substring(0, jpgFileLocation.LastIndexOf(EditorManager.inst.currentLoadedLevel) + EditorManager.inst.currentLoadedLevel.Length + 1), "");
				inst.StartCoroutine(RefreshObjectGUI());
			}
		}

		public static void KFTest()
        {
			inst.StartCoroutine(KeyframesLoop(type: 3, action: delegate (DataManager.GameData.EventKeyframe eventKeyframe)
			{
				if (eventKeyframe.eventValues[0] == 7f)
					eventKeyframe.eventValues[0] = 8f;
				if (eventKeyframe.eventValues[0] == 6f)
					eventKeyframe.eventValues[0] = 7f;
			}));

			var array = new float[2]
			{
				0f,
				9f
			};

			array.AddItem(0f);

			array.Add(0f);
		}

		//foreach (var beatmapObject in RTEditor.SelectedBeatmapObjects)
		//{
		//	beatmapObject.autoKillOffset = AudioManager.inst.CurrentAudioSource.time - beatmapObject.StartTime;
		//	beatmapObject.autoKillType = DataManager.GameData.BeatmapObject.AutoKillType.LastKeyframeOffset;
		//}

		public static List<BeatmapObject> SelectedBeatmapObjects
        {
			get
            {
				return (from x in ObjEditor.inst.selectedObjects
						where x.IsObject()
						select x.GetObjectData()).ToList();
			}
        }

		public static List<DataManager.GameData.PrefabObject> SelectedPrefabObjects
        {
			get
            {
				return (from x in ObjEditor.inst.selectedObjects
						where x.IsPrefab()
						select x.GetPrefabObjectData()).ToList();
            }
        }

		public static IEnumerator KeyframeSelectionLoop(bool all = false, int type = 0, Action<DataManager.GameData.EventKeyframe> action = null)
        {
			foreach (var bm in SelectedBeatmapObjects)
			{
				if (all)
				{
					for (int i = 0; i < bm.events.Count; i++)
					{
						foreach (var kf in bm.events[i])
						{
							if (action != null)
								action(kf);
						}
					}
				}
				else
				{
					foreach (var kf in bm.events[type])
					{
						if (action != null)
							action(kf);
					}
				}
			}
			yield break;
        }

		public static IEnumerator KeyframesLoop(bool all = false, int type = 0, Action<DataManager.GameData.EventKeyframe> action = null)
        {
			foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
			{
				if (all)
				{
					for (int i = 0; i < beatmapObject.events.Count; i++)
					{
						foreach (var kf in beatmapObject.events[i])
						{
							if (action != null)
								action(kf);
						}
					}
				}
				else
                {
					foreach (var kf in beatmapObject.events[type])
					{
						if (action != null)
							action(kf);
                    }
                }
            }
			ObjectManager.inst.updateObjects();
			yield break;
		}

		public static IEnumerator GenerateDuplicationWave(int amount = 14, int reset = 0, float offset = 0.02f, float position = 1.5f, float wait = 0.2f)
		{
			var bm = ObjEditor.inst.currentObjectSelection.GetObjectData();
			float sca = 0f;
			int bin = bm.editorData.Bin;
			for (int i = 0; i < amount; i++)
			{
				Duplicate();
				yield return new WaitForSeconds(wait);

				bm = ObjEditor.inst.currentObjectSelection.GetObjectData();
				bin++;

				if (bin > 14)
				{
					bin = reset;
					bm.editorData.Bin = bin;
					ObjEditor.inst.RenderTimelineObject(ObjEditor.inst.currentObjectSelection);
				}

				bm.StartTime += offset;
				sca += offset;
				bm.SetParentOffset(1, sca);
				bm.events[0][0].eventValues[0] += position;
			}
		}

		public static IEnumerator GenerateNewWave(BeatmapObject parent, int amount = 14, int reset = 0, float offset = 0.02f, float position = 1.5f, float wait = 0.2f)
        {
			float poskf = 0f;
			float sca = 0f;
			int bin = reset;
			for (int i = 0; i < amount; i++)
			{
				var objectSelection = CreateNewDefaultObject();
				var bm = objectSelection.GetObjectData();
				yield return new WaitForSeconds(wait);

				bin++;
				bm.editorData.Bin = bin;

				if (bin > 14)
                {
					bin = reset;
					bm.editorData.Bin = bin;
					ObjEditor.inst.RenderTimelineObject(ObjEditor.inst.currentObjectSelection);
				}

				bm.StartTime += offset;
				sca += offset;
				bm.SetParentOffset(1, sca);
				poskf += position;
				bm.events[0][0].eventValues[0] = poskf;
			}
		}

		#endregion

		#region EditorProperties

		public static List<EditorProperty> editorProperties = new List<EditorProperty>
		{
			//General
			new EditorProperty("Debug", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.General, ConfigEntries.EditorDebug, "Adds some debug functionality, such as showing the Unity debug logs through the in-game notifications (sometimes not the best idea to have on for this reason) and some object debugging."),
			new EditorProperty("Reminder Active", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.General, ConfigEntries.ReminderActive, "Will enable the reminder to tell you to have a break."),
			new EditorProperty("Reminder Loop Time", EditorProperty.ValueType.Float, EditorProperty.EditorPropCategory.General, ConfigEntries.ReminderLoopTime, "The time between each reminder."),
			new EditorProperty("BPM Snaps Keyframes", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.General, ConfigEntries.BPMSnapsKeyframes, "Makes object's keyframes snap if Snap BPM is enabled."),
			new EditorProperty("BPM Snap Divisions", EditorProperty.ValueType.Float, EditorProperty.EditorPropCategory.General, ConfigEntries.BPMSnapDivisions, "How many times the snap is divided into. Can be good for songs that don't do 4 divisions."),
			new EditorProperty("Preferences Open Key", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.General, ConfigEntries.EditorPropertiesKey, "The key to press to open the Editor Properties / Preferences window."),
			new EditorProperty("Prefab Example Template", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.General, ConfigEntries.PrefabExampleTemplate, "Example Template prefab will always be generated into the internal prefabs for you to use."),
			new EditorProperty("Paste Offset", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.General, ConfigEntries.PasteOffset, "When enabled objects that are pasted will be pasted at an offset based on the distance between the audio time and the copied object. Otherwise, the objects will be pasted at the earliest objects start time."),

			//Timeline
			new EditorProperty("Dragging Main Cursor Pauses Level", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Timeline, ConfigEntries.DraggingMainCursorPausesLevel, "If dragging the cursor pauses the level."),
			new EditorProperty("Main Cursor Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Timeline, ConfigEntries.MainTimelineSliderColor, "Color of the main timeline cursor."),
			new EditorProperty("Keyframe Cursor Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Timeline, ConfigEntries.KeyframeTimelineSliderColor, "Color of the object timeline cursor."),
			new EditorProperty("Object Selection Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Timeline, ConfigEntries.ObjectSelectionColor, "Color of selected objects."),
			new EditorProperty("Main Zoom Bounds", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.Timeline, ConfigEntries.MainZoomBounds, "The limits of the main timeline zoom."),
			new EditorProperty("Keyframe Zoom Bounds", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.Timeline, ConfigEntries.KeyframeZoomBounds, "The limits of the keyframe timeline zoom."),
			new EditorProperty("Main Zoom Amount", EditorProperty.ValueType.FloatSlider, EditorProperty.EditorPropCategory.Timeline, ConfigEntries.MainZoomAmount, "Sets the zoom in & out amount for the main timeline."),
			new EditorProperty("Keyframe Zoom Amount", EditorProperty.ValueType.FloatSlider, EditorProperty.EditorPropCategory.Timeline, ConfigEntries.KeyframeZoomAmount, "Sets the zoom in & out amount for the keyframe timeline."),

			new EditorProperty("Waveform Generate", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Timeline, ConfigEntries.GenerateWaveform, "Allows the timeline waveform to generate. (Waveform might not show on some devices and will increase level load times)"),
			new EditorProperty("Waveform Re-render", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Timeline, ConfigEntries.RenderTimeline, "If the timeline waveform should update when a value is changed."),
			new EditorProperty("Waveform Mode", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.Timeline, ConfigEntries.WaveformMode, "Whether the waveform should be Legacy style or old style. Old style was originally used but at some point was replaced with the Legacy version."),
			new EditorProperty("Waveform BG Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Timeline, ConfigEntries.WaveformBGColor, "Color of the background for the waveform."),
			new EditorProperty("Waveform Top Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Timeline, ConfigEntries.WaveformTopColor, "If waveform mode is Legacy, this will be the top color. Otherwise, it will be the base color."),
			new EditorProperty("Waveform Bottom Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Timeline, ConfigEntries.WaveformBottomColor, "If waveform is Legacy, this will be the bottom color. Otherwise, it will be unused."),

			new EditorProperty("Marker Looping Active", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Timeline, ConfigEntries.MarkerLoop, "If markers should loop from set end marker to set start marker."),
			new EditorProperty("Marker Looping Start", EditorProperty.ValueType.Int, EditorProperty.EditorPropCategory.Timeline, ConfigEntries.MarkerStartIndex, ""),
			new EditorProperty("Marker Looping End", EditorProperty.ValueType.Int, EditorProperty.EditorPropCategory.Timeline, ConfigEntries.MarkerEndIndex, ""),

			//Data
			new EditorProperty("Autosave Limit", EditorProperty.ValueType.Int, EditorProperty.EditorPropCategory.Data, ConfigEntries.AutoSaveLimit, "How many autosave files you want to have until it starts removing older autosaves."),
			new EditorProperty("Autosave Loop Time", EditorProperty.ValueType.Float, EditorProperty.EditorPropCategory.Data, ConfigEntries.AutoSaveLoopTime, "The time between each autosave."),
			new EditorProperty("Saving Updates Edited Date", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Data, ConfigEntries.SavingUpdatesTime, "If you want the date edited to be the current date you save on, enable this. Can be good for keeping organized with what levels you did recently."),
			new EditorProperty("Level Loads Last Time", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Data, ConfigEntries.LevelLoadsSavedTime, "Sets the editor position (audio time, layer, etc) to the last saved editor position on level load."),
			new EditorProperty("Level Pauses on Start", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Data, ConfigEntries.LevelPausesOnStart, "Editor pauses on level load."),
			new EditorProperty("Saving Saves Beatmap Opacity", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Data, ConfigEntries.SaveOpacityToThemes, "Turn this off if you don't want themes to break in unmodded PA."),
			//new EditorProperty("Manage Prefab Types", EditorProperty.ValueType.Function, EditorProperty.EditorPropCategory.Data, delegate () { }, "Opens the Prefab Type editor."),

			//Editor GUI
			new EditorProperty("Drag UI", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.DragUI, "Specific UI elements can now be dragged around."),
			new EditorProperty("Hover UI Sound", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.HoverSoundsEnabled, "If the sound when hovering over a UI element should play."),
			new EditorProperty("Notification Width", EditorProperty.ValueType.Float, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.NotificationWidth, "Width of the notifications."),
			new EditorProperty("Notification Size", EditorProperty.ValueType.Float, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.NotificationSize, "Total size of the notifications."),
			new EditorProperty("Notification Direction", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.NotificationDirection, "Direction the notifications popup from."),
			new EditorProperty("Notification Display", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.DisplayNotifications, "If the notifications should display. Does not include the help box."),

			new EditorProperty("Open Level Position", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OpenFilePosition, "Starting position of the Open Level Popup."),
			new EditorProperty("Open Level Scale", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OpenFileScale, ""),
			new EditorProperty("Open Level Path Pos", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OpenFilePathPos, ""),
			new EditorProperty("Open Level Path Length", EditorProperty.ValueType.Float, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OpenFilePathLength, ""),
			new EditorProperty("Open Level Refresh Pos", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OpenFileRefreshPosition, ""),
			new EditorProperty("Open Level Toggle Pos", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OpenFileTogglePosition, ""),
			new EditorProperty("Open Level Dropdown Pos", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OpenFileDropdownPosition, ""),

			new EditorProperty("Open Level Cell Size", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OpenFileCellSize, ""),
			new EditorProperty("Open Level Constraint", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OpenFileCellConstraintType, ""),
			new EditorProperty("Open Level Constraint Count", EditorProperty.ValueType.Int, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OpenFileCellConstraintCount, ""),
			new EditorProperty("Open Level Spacing", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OpenFileCellSpacing, ""),

			new EditorProperty("Open Level HWrap", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OpenFileTextHorizontalWrap, ""),
			new EditorProperty("Open Level VWrap", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OpenFileTextVerticalWrap, ""),
			new EditorProperty("Open Level Text Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OpenFileTextColor, ""),
			new EditorProperty("Open Level Text Invert", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OpenFileTextInvert, ""),
			new EditorProperty("Open Level Font Size", EditorProperty.ValueType.Int, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OpenFileTextFontSize, ""),
			new EditorProperty("Open Level Folder Clamp", EditorProperty.ValueType.Int, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OpenFileFolderNameMax, ""),
			new EditorProperty("Open Level Song Clamp", EditorProperty.ValueType.Int, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OpenFileSongNameMax, ""),
			new EditorProperty("Open Level Artist Clamp", EditorProperty.ValueType.Int, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OpenFileArtistNameMax, ""),
			new EditorProperty("Open Level Creator Clamp", EditorProperty.ValueType.Int, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OpenFileCreatorNameMax, ""),
			new EditorProperty("Open Level Desc Clamp", EditorProperty.ValueType.Int, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OpenFileDescriptionMax, ""),
			new EditorProperty("Open Level Date Clamp", EditorProperty.ValueType.Int, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OpenFileDateMax, ""),
			new EditorProperty("Open Level Format", EditorProperty.ValueType.String, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OpenFileTextFormatting, ""),

			new EditorProperty("Open Level Difficulty Color", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OpenFileButtonDifficultyColor, ""),
			new EditorProperty("Open Level Difficulty Color X", EditorProperty.ValueType.Float, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OpenFileButtonDifficultyMultiply, ""),
			new EditorProperty("Open Level Normal Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OpenFileButtonNormalColor, ""),
			new EditorProperty("Open Level Highlighted Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OpenFileButtonHighlightedColor, ""),
			new EditorProperty("Open Level Pressed Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OpenFileButtonPressedColor, ""),
			new EditorProperty("Open Level Selected Color", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OpenFileButtonSelectedColor, ""),
			new EditorProperty("Open Level Color Fade Time", EditorProperty.ValueType.Float, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OpenFileButtonFadeDuration, ""),

			new EditorProperty("Open Level Button Hover Size", EditorProperty.ValueType.FloatSlider, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OpenFileButtonHoverSize, ""),

			new EditorProperty("Open Level Cover Art Position", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OpenFileCoverPosition, ""),
			new EditorProperty("Open Level Cover Art Scale", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.OpenFileCoverScale, ""),

			new EditorProperty("Open Level Show Delete Button", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.ShowLevelDeleteButton, "Shows a delete button that can be used to move levels to a recycling folder."),

			new EditorProperty("Timeline Object Hover Size", EditorProperty.ValueType.FloatSlider, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.TimelineBarButtonsHoverSize, ""),
			new EditorProperty("Timeline Object Hover Size", EditorProperty.ValueType.FloatSlider, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.TimelineObjectHoverSize, ""),
			new EditorProperty("Object Keyframe Hover Size", EditorProperty.ValueType.FloatSlider, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.KeyframeHoverSize, ""),

			new EditorProperty("Marker Color 1", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.MarkerColN0, ""),
			new EditorProperty("Marker Color 2", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.MarkerColN1, ""),
			new EditorProperty("Marker Color 3", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.MarkerColN2, ""),
			new EditorProperty("Marker Color 4", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.MarkerColN3, ""),
			new EditorProperty("Marker Color 5", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.MarkerColN4, ""),
			new EditorProperty("Marker Color 6", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.MarkerColN5, ""),
			new EditorProperty("Marker Color 7", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.MarkerColN6, ""),
			new EditorProperty("Marker Color 8", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.MarkerColN7, ""),
			new EditorProperty("Marker Color 9", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.MarkerColN8, ""),

			new EditorProperty("IN-Prefab Horizontal Scroll", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.PrefabINHScroll, "Allows scrolling left to right."),
			new EditorProperty("IN-Prefab Cell Size", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.PrefabINCellSize, "Size of each Prefab Cell."),
			new EditorProperty("IN-Prefab Constraint Mode", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.PrefabINConstraint, "Which direction the prefab list goes."),
			new EditorProperty("IN-Prefab Constraint Count", EditorProperty.ValueType.Int, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.PrefabINConstraintColumns, "How many columns the prefabs are divided into."),
			new EditorProperty("IN-Prefab Spacing", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.PrefabINCellSpacing, "Distance between each Prefab Cell."),
			new EditorProperty("IN-Prefab Start Axis", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.PrefabINAxis, "Start axis of the prefab list."),
			new EditorProperty("IN-Prefab Delete Button Pos", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.PrefabINLDeletePos, "Position of the Delete Button."),
			new EditorProperty("IN-Prefab Name HOverflow", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.PrefabINNameHOverflow, "If the text overflows into another line or keeps going."),
			new EditorProperty("IN-Prefab Name VOverflow", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.PrefabINNameVOverflow, "If the text overflows into another line or keeps going."),
			new EditorProperty("IN-Prefab Name Font Size", EditorProperty.ValueType.Int, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.PrefabINNameFontSize, "Size of the text font."),
			new EditorProperty("IN-Prefab Type HOverflow", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.PrefabINTypeHOverflow, "If the text overflows into another line or keeps going."),
			new EditorProperty("IN-Prefab Type VOverflow", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.PrefabINTypeVOverflow, "If the text overflows into another line or keeps going."),
			new EditorProperty("IN-Prefab Type Font Size", EditorProperty.ValueType.Int, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.PrefabINTypeFontSize, "Size of the text font."),
			new EditorProperty("IN-Prefab Popup Position", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.PrefabINANCH, "Position of the internal prefabs popup."),
			new EditorProperty("IN-Prefab Popup Scale", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.PrefabINSD, "Scale of the internal prefabs popup."),

			new EditorProperty("EX-Prefab Horizontal Scroll", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.PrefabEXHScroll, "Allows scrolling left to right."),
			new EditorProperty("EX-Prefab Cell Size", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.PrefabEXCellSize, "Size of each Prefab Cell."),
			new EditorProperty("EX-Prefab Constraint Mode", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.PrefabEXConstraint, "Which direction the prefab list goes."),
			new EditorProperty("EX-Prefab Constraint Count", EditorProperty.ValueType.Int, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.PrefabEXConstraintColumns, "How many columns the prefabs are divided into."),
			new EditorProperty("EX-Prefab Spacing", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.PrefabEXCellSpacing, "Distance between each Prefab Cell."),
			new EditorProperty("EX-Prefab Start Axis", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.PrefabEXAxis, "Start axis of the prefab list."),
			new EditorProperty("EX-Prefab Delete Button Pos", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.PrefabEXLDeletePos, "Position of the Delete Button."),
			new EditorProperty("EX-Prefab Name HOverflow", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.PrefabEXNameHOverflow, "If the text overflows into another line or keeps going."),
			new EditorProperty("EX-Prefab Name VOverflow", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.PrefabEXNameVOverflow, "If the text overflows into another line or keeps going."),
			new EditorProperty("EX-Prefab Name Font Size", EditorProperty.ValueType.Int, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.PrefabEXNameFontSize, "Size of the text font."),
			new EditorProperty("EX-Prefab Type HOverflow", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.PrefabEXTypeHOverflow, "If the text overflows into another line or keeps going."),
			new EditorProperty("EX-Prefab Type VOverflow", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.PrefabEXTypeVOverflow, "If the text overflows into another line or keeps going."),
			new EditorProperty("EX-Prefab Type Font Size", EditorProperty.ValueType.Int, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.PrefabEXTypeFontSize, "Size of the text font."),
			new EditorProperty("EX-Prefab Popup Position", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.PrefabEXANCH, "Position of the external prefabs popup."),
			new EditorProperty("EX-Prefab Popup Scale", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.PrefabEXSD, "Scale of the external prefabs popup."),
			new EditorProperty("EX-Prefab Prefab Path Pos", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.PrefabEXPathPos, "Position of the prefab path input field."),
			new EditorProperty("EX-Prefab Prefab Path Length", EditorProperty.ValueType.Float, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.PrefabEXPathSca, "Length of the prefab path input field."),
			new EditorProperty("EX-Prefab Prefab Refresh Pos", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, ConfigEntries.PrefabEXRefreshPos, "Position of the prefab refresh button."),

			//Functions
			new EditorProperty("Display Random Notification", EditorProperty.ValueType.Function, EditorProperty.EditorPropCategory.Functions, delegate () { EditorManager.inst.DisplayNotification("Test", 2f); }, "Button."),

			//Fields
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

			//Preview
			new EditorProperty("Show Object Dragger", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Preview, ConfigEntries.ShowSelector, "Allows objects's position and scale to directly be modified within the editor preview window."),
			new EditorProperty("Show Only On Layer", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Preview, ConfigEntries.ShowObjectsOnLayer, "Will make any objects not on the current editor layer transparent."),
			new EditorProperty("Not On Layer Opacity", EditorProperty.ValueType.FloatSlider, EditorProperty.EditorPropCategory.Preview, ConfigEntries.ShowObjectsAlpha, "How transparent the objects not on the current editor layer should be."),
			new EditorProperty("Show Empties (Does not work)", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Preview, ConfigEntries.ShowEmpties, "If empties should be shown. Good for understanding character rigs."),
			new EditorProperty("Show Only Damagable (Does not work)", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Preview, ConfigEntries.ShowDamagable, "Will only show objects that can hit you."),
			new EditorProperty("Empties Preview Fix (Deprecated)", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Preview, ConfigEntries.PreviewSelectFix, "Empties will become unselectable in the editor preview window."),
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

			public EditorProperty(string _name, ValueType _valueType, EditorPropCategory _editorProp, Action action, string _description)
            {
				name = _name;
				valueType = _valueType;
				propCategory = _editorProp;
				description = _description;
				this.action = action;
            }

			public string name;
			public ValueType valueType;
			public EditorPropCategory propCategory;
			public ConfigEntryBase configEntry;
			public string description;
			public Action action;

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
				Color,
				Function
			}

			public enum EditorPropCategory
			{
				General,
				Timeline,
				Data,
				EditorGUI,
				Functions,
				Fields,
				Preview
			}
		}

        #endregion
    }
}
