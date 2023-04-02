using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using UnityEngine;
using UnityEngine.UI;

using HarmonyLib;
using BepInEx;
using BepInEx.Configuration;

using DG.Tweening;
using SimpleJSON;
using TMPro;

using LSFunctions;

using EditorManagement.Functions.Tools;

namespace EditorManagement.Functions
{
    public class RTEditor : MonoBehaviour
    {
        public static RTEditor inst;

		public static bool ienumRunning;

		public static List<string> notifications = new List<string>();
		public static List<HoverTooltip> tooltips = new List<HoverTooltip>();

		public static string propertiesSearch;

		private void Awake()
        {
            if (inst == null)
            {
                inst = this;
                return;
            }
            if (inst != this)
            {
                Destroy(gameObject);
            }
        }

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.F10))
			{
				OpenPropertiesWindow(true);
			}
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
			switch (_type)
			{
				case EditorManager.NotificationType.Info:
					{
						GameObject gameObject = Instantiate(EditorManager.inst.notificationPrefabs[0], Vector3.zero, Quaternion.identity);
						Destroy(gameObject, _time);
						gameObject.transform.Find("text").GetComponent<TextMeshProUGUI>().text = _text;
						gameObject.transform.SetParent(EditorManager.inst.notification.transform);
						if (EditorPlugin.NotificationDirection.Value == EditorPlugin.Direction.Down)
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
						if (EditorPlugin.NotificationDirection.Value == EditorPlugin.Direction.Down)
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
						if (EditorPlugin.NotificationDirection.Value == EditorPlugin.Direction.Down)
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
						if (EditorPlugin.NotificationDirection.Value == EditorPlugin.Direction.Down)
						{
							gameObject3.transform.SetAsFirstSibling();
						}
						gameObject3.transform.localScale = Vector3.one;
						break;
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
			if (!notifications.Contains(_name))
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
							if (EditorPlugin.NotificationDirection.Value == EditorPlugin.Direction.Down)
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
							if (EditorPlugin.NotificationDirection.Value == EditorPlugin.Direction.Down)
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
							if (EditorPlugin.NotificationDirection.Value == EditorPlugin.Direction.Down)
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
							if (EditorPlugin.NotificationDirection.Value == EditorPlugin.Direction.Down)
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
			if (!notifications.Contains(_name))
            {
				notifications.Add(_name);

				GameObject gameObject = Instantiate(EditorManager.inst.notificationPrefabs[0], Vector3.zero, Quaternion.identity);
				Destroy(gameObject, _time);
				gameObject.transform.Find("text").GetComponent<TextMeshProUGUI>().text = _text;
				gameObject.transform.SetParent(EditorManager.inst.notification.transform);
				if (EditorPlugin.NotificationDirection.Value == EditorPlugin.Direction.Down)
				{
					gameObject.transform.SetAsFirstSibling();
				}
				gameObject.transform.localScale = Vector3.one;

				gameObject.GetComponent<Image>().color = _base;
				gameObject.transform.Find("bg/bg").GetComponent<Image>().color = _top;
				if (_icon != null)
				{
					gameObject.transform.Find("bg/Image").GetComponent<Image>().sprite = _icon;
				}

				gameObject.transform.Find("bg/Image").GetComponent<Image>().color = _icCol;
				gameObject.transform.Find("bg/title").GetComponent<Text>().text = _title;

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
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].autoKillType = DataManager.GameData.BeatmapObject.AutoKillType.OldStyleNoAutokill;
			DataManager.inst.gameData.beatmapObjects[tmpSelection.Index].name = "no autokill";
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
			list[0].Add(new DataManager.GameData.EventKeyframe(0f, new float[2], new float[0], 0));
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
				if (_add == false)
				{
					objectSelection.GetObjectData().editorData.Layer = _layer;
					ObjEditor.inst.RenderTimelineObject(objectSelection);
				}
				else
				{
					objectSelection.GetObjectData().editorData.Layer += _layer;
					ObjEditor.inst.RenderTimelineObject(objectSelection);
				}
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
			while (EditorManager.inst.autosaves.Count > EditorPlugin.AutoSaveLimit.Value)
			{
				File.Delete(EditorManager.inst.autosaves.First<string>());
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
			inst.StartCoroutine(AddPrefabExpandedToLevel(ObjEditor.inst.beatmapObjCopy, true, _offsetTime, false, _regen));

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

			if (_add == false)
            {
				ObjEditor.inst.selectedObjects.Clear();
				ObjEditor.inst.RenderTimelineObjects();
			}

			foreach (KeyValuePair<string, GameObject> keyValuePair in ObjEditor.inst.beatmapObjects)
			{
				if (EditorManager.RectTransformToScreenSpace(EditorManager.inst.SelectionBoxImage.rectTransform).Overlaps(EditorManager.RectTransformToScreenSpace(keyValuePair.Value.GetComponent<Image>().rectTransform)) && keyValuePair.Value.activeSelf)
				{
					yield return new WaitForSeconds(delay);
					ObjEditor.ObjectSelection objectSelection = new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Object, keyValuePair.Key);
					ObjEditor.inst.AddSelectedObject(objectSelection);
					ObjEditor.inst.RenderTimelineObject(objectSelection);
					delay += 0.0001f;
				}
			}
			foreach (KeyValuePair<string, GameObject> keyValuePair2 in ObjEditor.inst.prefabObjects)
			{
				if (EditorManager.RectTransformToScreenSpace(EditorManager.inst.SelectionBoxImage.rectTransform).Overlaps(EditorManager.RectTransformToScreenSpace(keyValuePair2.Value.GetComponent<Image>().rectTransform)) && keyValuePair2.Value.activeSelf)
				{
					yield return new WaitForSeconds(delay);
					ObjEditor.ObjectSelection prefabSelection = new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Prefab, keyValuePair2.Key);
					ObjEditor.inst.AddSelectedObject(prefabSelection);
					ObjEditor.inst.RenderTimelineObject(prefabSelection);
					delay += 0.0001f;
				}
			}

			if (ObjEditor.inst.selectedObjects.Count() > 0)
			{
				ObjEditor.inst.selectedObjects = (from x in ObjEditor.inst.selectedObjects
												  orderby x.Index ascending
												  select x).ToList();
			}

			if (ObjEditor.inst.selectedObjects.Count() <= 0)
			{
				CheckpointEditor.inst.SetCurrentCheckpoint(0);
			}
			EditorManager.inst.DisplayNotification("Selection includes " + ObjEditor.inst.selectedObjects.Count + " objects!", 1f, EditorManager.NotificationType.Success);
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

		public static IEnumerator DeleteObjects(List<ObjEditor.ObjectSelection> _objs, bool _set = true)
		{
			ienumRunning = true;

			float delay = 0f;
			var list = ObjEditor.inst.selectedObjects;
			int count = ObjEditor.inst.selectedObjects.Count;

			EditorManager.inst.DisplayNotification("Deleting Beatmap Objects [ " + count + " ]", 1f, EditorManager.NotificationType.Success);

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

		//Add Prefabs
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
					beatmapObject2.editorData.Layer = EditorManager.inst.layer;
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
						beatmapObject2.StartTime += EditorManager.inst.CurrentAudioPos;
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
				beatmapObject2.editorData.Layer = EditorManager.inst.layer;
				beatmapObject2.fromPrefab = false;
				DataManager.inst.gameData.beatmapObjects.Add(beatmapObject2);
				ObjEditor.ObjectSelection _selection = new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Object, beatmapObject2.id);
				ObjEditor.inst.CreateTimelineObject(_selection);
				ObjEditor.inst.RenderTimelineObject(_selection);
				ObjectManager.inst.updateObjects(_selection);
				if (_select)
				{
					ObjEditor.inst.AddSelectedObject(_selection);
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
						prefabObject2.StartTime += EditorManager.inst.CurrentAudioPos;
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
				prefabObject2.editorData.Layer = EditorManager.inst.layer;
				DataManager.inst.gameData.prefabObjects.Add(prefabObject2);
				ObjEditor.ObjectSelection _selection = new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Prefab, prefabObject2.ID);
				ObjEditor.inst.CreateTimelineObject(_selection);
				ObjEditor.inst.RenderTimelineObject(_selection);
				ObjectManager.inst.updateObjects(_selection);
				if (_select)
				{
					ObjEditor.inst.AddSelectedObject(_selection);
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
		
		public static IEnumerator ParseBeatmap(string _json)
		{
			JSONNode jsonnode = JSON.Parse(_json);
			DataManager.inst.gameData.ParseThemeData(jsonnode["themes"]);
			DataManager.inst.gameData.ParseEditorData(jsonnode["ed"]);
			DataManager.inst.gameData.ParseLevelData(jsonnode["level_data"]);
			DataManager.inst.gameData.ParseCheckpointData(jsonnode["checkpoints"]);
			ParsePrefabs(jsonnode["prefabs"]);
			ParsePrefabObjects(jsonnode["prefab_objects"]);
			inst.StartCoroutine(ParseGameObjects(jsonnode["beatmap_objects"]));
			DataManager.inst.gameData.ParseBackgroundObjects(jsonnode["bg_objects"]);
			DataManager.inst.gameData.ParseEventObjects(jsonnode["events"]);
			yield break;
        }

		public static IEnumerator ParseThemeData(JSONNode _themeData)
		{
			float delay = 0f;
			DataManager.inst.CustomBeatmapThemes.Clear();
			DataManager.inst.BeatmapThemeIDToIndex.Clear();
			DataManager.inst.BeatmapThemeIndexToID.Clear();
			int num = 0;
			foreach (DataManager.BeatmapTheme beatmapTheme in DataManager.inst.BeatmapThemes)
			{
				yield return new WaitForSeconds(delay);
				DataManager.inst.BeatmapThemeIDToIndex.Add(num, num);
				DataManager.inst.BeatmapThemeIndexToID.Add(num, num);
				delay += 0.0001f;
				num++;
			}
			if (DataManager.inst.gameData.beatmapData == null)
			{
				DataManager.inst.gameData.beatmapData = new DataManager.GameData.BeatmapData();
			}
			if (_themeData != null)
			{
				DataManager.BeatmapTheme.ParseMulti(_themeData, true);
			}
			yield break;
		}

		public static IEnumerator ParseEditorData(JSONNode _editorData)
		{
			float delay = 0f;
			if (DataManager.inst.gameData.beatmapData == null)
			{
				DataManager.inst.gameData.beatmapData = new DataManager.GameData.BeatmapData();
			}
			DataManager.inst.gameData.beatmapData.editorData = new DataManager.GameData.BeatmapData.EditorData();
			if (!string.IsNullOrEmpty(_editorData["timeline_pos"]))
			{
				DataManager.inst.gameData.beatmapData.editorData.timelinePos = _editorData["timeline_pos"].AsFloat;
			}
			else
			{
				DataManager.inst.gameData.beatmapData.editorData.timelinePos = 0f;
			}
			DataManager.inst.gameData.beatmapData.markers.Clear();
			for (int i = 0; i < _editorData["markers"].Count; i++)
			{
				yield return new WaitForSeconds(delay);
				bool asBool = _editorData["markers"][i]["active"].AsBool;
				string name = "Marker";
				if (_editorData["markers"][i]["name"] != null)
				{
					name = _editorData["markers"][i]["name"];
				}
				string desc = "";
				if (_editorData["markers"][i]["desc"] != null)
				{
					desc = _editorData["markers"][i]["desc"];
				}
				float asFloat = _editorData["markers"][i]["t"].AsFloat;
				int color = 0;
				if (_editorData["markers"][i]["col"] != null)
				{
					color = _editorData["markers"][i]["col"].AsInt;
				}
				DataManager.inst.gameData.beatmapData.markers.Add(new DataManager.GameData.BeatmapData.Marker(asBool, name, desc, color, asFloat));
				delay += 0.0001f;
			}
			yield break;
		}

		public static IEnumerator ParseCheckpointData(JSONNode _checkpointData)
		{
			float delay = 0f;
			if (DataManager.inst.gameData.beatmapData == null)
			{
				DataManager.inst.gameData.beatmapData = new DataManager.GameData.BeatmapData();
			}
			if (DataManager.inst.gameData.beatmapData.checkpoints == null)
			{
				DataManager.inst.gameData.beatmapData.checkpoints = new List<DataManager.GameData.BeatmapData.Checkpoint>();
			}
			DataManager.inst.gameData.beatmapData.checkpoints.Clear();
			for (int i = 0; i < _checkpointData.Count; i++)
			{
				yield return new WaitForSeconds(delay);
				bool asBool = _checkpointData[i]["active"].AsBool;
				string name = _checkpointData[i]["name"];
				Vector2 pos = new Vector2(_checkpointData[i]["pos"]["x"].AsFloat, _checkpointData[i]["pos"]["y"].AsFloat);
				float time = _checkpointData[i]["t"].AsFloat;
				if (DataManager.inst.gameData.beatmapData.checkpoints.FindIndex((DataManager.GameData.BeatmapData.Checkpoint x) => x.time == time) == -1)
				{
					DataManager.inst.gameData.beatmapData.checkpoints.Add(new DataManager.GameData.BeatmapData.Checkpoint(asBool, name, time, pos));
				}
				delay += 0.0001f;
			}
			DataManager.inst.gameData.beatmapData.checkpoints = (from x in DataManager.inst.gameData.beatmapData.checkpoints
											orderby x.time
											select x).ToList();
			yield break;
		}

		public static void ParsePrefabs(JSONNode _prefabs)
		{
			if (DataManager.inst.gameData.prefabs == null)
			{
				DataManager.inst.gameData.prefabs = new List<DataManager.GameData.Prefab>();
			}
			DataManager.inst.gameData.prefabs.Clear();
			for (int i = 0; i < _prefabs.Count; i++)
			{
				List<DataManager.GameData.BeatmapObject> list = new List<DataManager.GameData.BeatmapObject>();
				for (int j = 0; j < _prefabs[i]["objects"].Count; j++)
				{
					DataManager.GameData.BeatmapObject beatmapObject = DataManager.GameData.BeatmapObject.ParseGameObject(_prefabs[i]["objects"][j]);
					if (beatmapObject != null)
					{
						list.Add(beatmapObject);
					}
				}
				List<DataManager.GameData.PrefabObject> list2 = new List<DataManager.GameData.PrefabObject>();
				for (int k = 0; k < _prefabs[i]["prefab_objects"].Count; k++)
				{
					list2.Add(DataManager.inst.gameData.ParsePrefabObject(_prefabs[i]["prefab_objects"][k]));
				}
				DataManager.GameData.Prefab prefab = new DataManager.GameData.Prefab(_prefabs[i]["name"], _prefabs[i]["type"].AsInt, _prefabs[i]["offset"].AsFloat, list, list2);
				prefab.ID = _prefabs[i]["id"];
				prefab.MainObjectID = _prefabs[i]["main_obj_id"];
				DataManager.inst.gameData.prefabs.Add(prefab);
			}
		}

		public static void ParsePrefabObjects(JSONNode _prefabObjects)
		{
			if (DataManager.inst.gameData.prefabObjects == null)
			{
				DataManager.inst.gameData.prefabObjects = new List<DataManager.GameData.PrefabObject>();
			}
			DataManager.inst.gameData.prefabObjects.Clear();
			for (int i = 0; i < _prefabObjects.Count; i++)
			{
				DataManager.inst.gameData.prefabObjects.Add(DataManager.inst.gameData.ParsePrefabObject(_prefabObjects[i]));
			}
		}

		public static IEnumerator ParseGameObjects(JSONNode _objects)
		{
			float delay = 0f;
			if (DataManager.inst.gameData.beatmapObjects == null)
			{
				DataManager.inst.gameData.beatmapObjects = new List<DataManager.GameData.BeatmapObject>();
			}
			DataManager.inst.gameData.beatmapObjects.Clear();
			int num = 0;
			for (int i = 0; i < _objects.Count; i++)
			{
				yield return new WaitForSeconds(delay);
				DataManager.GameData.BeatmapObject beatmapObject = DataManager.GameData.BeatmapObject.ParseGameObject(_objects[i]);
				if (beatmapObject != null)
				{
					DataManager.inst.gameData.beatmapObjects.Add(beatmapObject);
					ObjEditor.ObjectSelection objectSelection = new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Object, i);
					updateObjects(beatmapObject);
					ObjEditor.inst.RenderTimelineObject(objectSelection);
				}
				else
				{
					num++;
				}
				delay += 0.0001f;
			}
			ObjectManager.inst.updateObjects();
			yield break;
		}

		public static IEnumerator ParseBackgroundObjects(JSONNode _backgroundObjects)
		{
			float delay = 0f;
			if (DataManager.inst.gameData.backgroundObjects == null)
			{
				DataManager.inst.gameData.backgroundObjects = new List<DataManager.GameData.BackgroundObject>();
			}
			DataManager.inst.gameData.backgroundObjects.Clear();
			for (int i = 0; i < _backgroundObjects.Count; i++)
			{
				yield return new WaitForSeconds(delay);
				bool active = true;
				if (_backgroundObjects[i]["active"] != null)
				{
					active = _backgroundObjects[i]["active"].AsBool;
				}
				string name;
				if (_backgroundObjects[i]["name"] != null)
				{
					name = _backgroundObjects[i]["name"];
				}
				else
				{
					name = "Background";
				}
				int kind;
				if (_backgroundObjects[i]["kind"] != null)
				{
					kind = _backgroundObjects[i]["kind"].AsInt;
				}
				else
				{
					kind = 1;
				}
				string text;
				if (_backgroundObjects[i]["text"] != null)
				{
					text = _backgroundObjects[i]["text"];
				}
				else
				{
					text = "";
				}
				Vector2[] array = new Vector2[4];
				for (int j = 0; j < array.Length; j++)
				{
					if (_backgroundObjects[i]["points"][j]["x"] != null)
					{
						array[j] = new Vector2(_backgroundObjects[i]["points"][j]["x"].AsFloat, _backgroundObjects[i]["points"][j]["y"].AsFloat);
					}
				}
				Vector2 pos = new Vector2(_backgroundObjects[i]["pos"]["x"].AsFloat, _backgroundObjects[i]["pos"]["y"].AsFloat);
				Vector2 scale = new Vector2(_backgroundObjects[i]["size"]["x"].AsFloat, _backgroundObjects[i]["size"]["y"].AsFloat);
				float asFloat = _backgroundObjects[i]["rot"].AsFloat;
				int asInt = _backgroundObjects[i]["color"].AsInt;
				int asInt2 = _backgroundObjects[i]["layer"].AsInt;
				bool reactive = false;
				if (_backgroundObjects[i]["r_set"] != null)
				{
					reactive = true;
				}
				if (_backgroundObjects[i]["r_set"]["active"] != null)
				{
					reactive = _backgroundObjects[i]["r_set"]["active"].AsBool;
				}
				DataManager.GameData.BackgroundObject.ReactiveType reactiveType = DataManager.GameData.BackgroundObject.ReactiveType.LOW;
				if (_backgroundObjects[i]["r_set"]["type"] != null)
				{
					reactiveType = (DataManager.GameData.BackgroundObject.ReactiveType)Enum.Parse(typeof(DataManager.GameData.BackgroundObject.ReactiveType), _backgroundObjects[i]["r_set"]["type"]);
				}
				float reactiveScale = 1f;
				if (_backgroundObjects[i]["r_set"]["scale"] != null)
				{
					reactiveScale = _backgroundObjects[i]["r_set"]["scale"].AsFloat;
				}
				bool drawFade = true;
				if (_backgroundObjects[i]["fade"] != null)
				{
					drawFade = _backgroundObjects[i]["fade"].AsBool;
				}
				DataManager.GameData.BackgroundObject item = new DataManager.GameData.BackgroundObject(active, name, kind, text, pos, scale, asFloat, asInt, asInt2, reactive, reactiveType, reactiveScale, drawFade);
				DataManager.inst.gameData.backgroundObjects.Add(item);
				delay += 0.0001f;
			}
			yield break;
		}

		public static IEnumerator ParseEventObjects(JSONNode _events)
		{
			float delay = 0f;
			if (DataManager.inst.gameData.eventObjects == null)
			{
				DataManager.inst.gameData.eventObjects = new DataManager.GameData.EventObjects();
			}
			for (int i = 0; i < _events["pos"].Count; i++)
			{
				yield return new WaitForSeconds(delay);
				DataManager.GameData.EventKeyframe eventKeyframe = new DataManager.GameData.EventKeyframe();
				JSONNode jsonnode = _events["pos"][i];
				eventKeyframe.eventTime = jsonnode["t"].AsFloat;
				eventKeyframe.SetEventValues(new float[]
				{
					jsonnode["x"].AsFloat,
					jsonnode["y"].AsFloat
				});
				eventKeyframe.random = jsonnode["r"].AsInt;
				DataManager.LSAnimation curveType = DataManager.inst.AnimationList[0];
				if (jsonnode["ct"] != null)
				{
					curveType = DataManager.inst.AnimationListDictionaryStr[jsonnode["ct"]];
					eventKeyframe.curveType = curveType;
				}
				eventKeyframe.SetEventRandomValues(new float[]
				{
					jsonnode["rx"].AsFloat,
					jsonnode["ry"].AsFloat
				});
				eventKeyframe.active = false;
				DataManager.inst.gameData.eventObjects.allEvents[0].Add(eventKeyframe);
				delay += 0.0001f;
			}
			for (int j = 0; j < _events["zoom"].Count; j++)
			{
				yield return new WaitForSeconds(delay);
				DataManager.GameData.EventKeyframe eventKeyframe2 = new DataManager.GameData.EventKeyframe();
				JSONNode jsonnode2 = _events["zoom"][j];
				eventKeyframe2.eventTime = jsonnode2["t"].AsFloat;
				eventKeyframe2.SetEventValues(new float[]
				{
					jsonnode2["x"].AsFloat
				});
				eventKeyframe2.random = jsonnode2["r"].AsInt;
				DataManager.LSAnimation curveType2 = DataManager.inst.AnimationList[0];
				if (jsonnode2["ct"] != null)
				{
					curveType2 = DataManager.inst.AnimationListDictionaryStr[jsonnode2["ct"]];
					eventKeyframe2.curveType = curveType2;
				}
				eventKeyframe2.SetEventRandomValues(new float[]
				{
					jsonnode2["rx"].AsFloat
				});
				eventKeyframe2.active = false;
				DataManager.inst.gameData.eventObjects.allEvents[1].Add(eventKeyframe2);
				delay += 0.0001f;
			}
			for (int k = 0; k < _events["rot"].Count; k++)
			{
				yield return new WaitForSeconds(delay);
				DataManager.GameData.EventKeyframe eventKeyframe3 = new DataManager.GameData.EventKeyframe();
				JSONNode jsonnode3 = _events["rot"][k];
				eventKeyframe3.eventTime = jsonnode3["t"].AsFloat;
				eventKeyframe3.SetEventValues(new float[]
				{
					jsonnode3["x"].AsFloat
				});
				eventKeyframe3.random = jsonnode3["r"].AsInt;
				DataManager.LSAnimation curveType3 = DataManager.inst.AnimationList[0];
				if (jsonnode3["ct"] != null)
				{
					curveType3 = DataManager.inst.AnimationListDictionaryStr[jsonnode3["ct"]];
					eventKeyframe3.curveType = curveType3;
				}
				eventKeyframe3.SetEventRandomValues(new float[]
				{
					jsonnode3["rx"].AsFloat
				});
				eventKeyframe3.active = false;
				DataManager.inst.gameData.eventObjects.allEvents[2].Add(eventKeyframe3);
				delay += 0.0001f;
			}
			for (int l = 0; l < _events["shake"].Count; l++)
			{
				yield return new WaitForSeconds(delay);
				DataManager.GameData.EventKeyframe eventKeyframe4 = new DataManager.GameData.EventKeyframe();
				JSONNode jsonnode4 = _events["shake"][l];
				eventKeyframe4.eventTime = jsonnode4["t"].AsFloat;
				eventKeyframe4.SetEventValues(new float[]
				{
					jsonnode4["x"].AsFloat
				});
				eventKeyframe4.random = jsonnode4["r"].AsInt;
				DataManager.LSAnimation curveType4 = DataManager.inst.AnimationList[0];
				if (jsonnode4["ct"] != null)
				{
					curveType4 = DataManager.inst.AnimationListDictionaryStr[jsonnode4["ct"]];
					eventKeyframe4.curveType = curveType4;
				}
				eventKeyframe4.SetEventRandomValues(new float[]
				{
					jsonnode4["rx"].AsFloat
				});
				eventKeyframe4.active = false;
				DataManager.inst.gameData.eventObjects.allEvents[3].Add(eventKeyframe4);
				delay += 0.0001f;
			}
			for (int m = 0; m < _events["theme"].Count; m++)
			{
				yield return new WaitForSeconds(delay);
				DataManager.GameData.EventKeyframe eventKeyframe5 = new DataManager.GameData.EventKeyframe();
				JSONNode jsonnode5 = _events["theme"][m];
				eventKeyframe5.eventTime = jsonnode5["t"].AsFloat;
				eventKeyframe5.SetEventValues(new float[]
				{
					jsonnode5["x"].AsFloat
				});
				eventKeyframe5.random = jsonnode5["r"].AsInt;
				DataManager.LSAnimation curveType5 = DataManager.inst.AnimationList[0];
				if (jsonnode5["ct"] != null)
				{
					curveType5 = DataManager.inst.AnimationListDictionaryStr[jsonnode5["ct"]];
					eventKeyframe5.curveType = curveType5;
				}
				eventKeyframe5.SetEventRandomValues(new float[]
				{
					jsonnode5["rx"].AsFloat
				});
				eventKeyframe5.active = false;
				DataManager.inst.gameData.eventObjects.allEvents[4].Add(eventKeyframe5);
				delay += 0.0001f;
			}
			for (int n = 0; n < _events["chroma"].Count; n++)
			{
				yield return new WaitForSeconds(delay);
				DataManager.GameData.EventKeyframe eventKeyframe6 = new DataManager.GameData.EventKeyframe();
				JSONNode jsonnode6 = _events["chroma"][n];
				eventKeyframe6.eventTime = jsonnode6["t"].AsFloat;
				eventKeyframe6.SetEventValues(new float[]
				{
					jsonnode6["x"].AsFloat
				});
				eventKeyframe6.random = jsonnode6["r"].AsInt;
				DataManager.LSAnimation curveType6 = DataManager.inst.AnimationList[0];
				if (jsonnode6["ct"] != null)
				{
					curveType6 = DataManager.inst.AnimationListDictionaryStr[jsonnode6["ct"]];
					eventKeyframe6.curveType = curveType6;
				}
				eventKeyframe6.SetEventRandomValues(new float[]
				{
					jsonnode6["rx"].AsFloat
				});
				eventKeyframe6.active = false;
				DataManager.inst.gameData.eventObjects.allEvents[5].Add(eventKeyframe6);
				delay += 0.0001f;
			}
			for (int num = 0; num < _events["bloom"].Count; num++)
			{
				yield return new WaitForSeconds(delay);
				DataManager.GameData.EventKeyframe eventKeyframe7 = new DataManager.GameData.EventKeyframe();
				JSONNode jsonnode7 = _events["bloom"][num];
				eventKeyframe7.eventTime = jsonnode7["t"].AsFloat;
				eventKeyframe7.SetEventValues(new float[]
				{
					jsonnode7["x"].AsFloat
				});
				eventKeyframe7.random = jsonnode7["r"].AsInt;
				DataManager.LSAnimation curveType7 = DataManager.inst.AnimationList[0];
				if (jsonnode7["ct"] != null)
				{
					curveType7 = DataManager.inst.AnimationListDictionaryStr[jsonnode7["ct"]];
					eventKeyframe7.curveType = curveType7;
				}
				eventKeyframe7.SetEventRandomValues(new float[]
				{
					jsonnode7["rx"].AsFloat
				});
				eventKeyframe7.active = false;
				DataManager.inst.gameData.eventObjects.allEvents[6].Add(eventKeyframe7);
				delay += 0.0001f;
			}
			for (int num2 = 0; num2 < _events["vignette"].Count; num2++)
			{
				yield return new WaitForSeconds(delay);
				DataManager.GameData.EventKeyframe eventKeyframe8 = new DataManager.GameData.EventKeyframe();
				JSONNode jsonnode8 = _events["vignette"][num2];
				eventKeyframe8.eventTime = jsonnode8["t"].AsFloat;
				eventKeyframe8.SetEventValues(new float[]
				{
					jsonnode8["x"].AsFloat,
					jsonnode8["y"].AsFloat,
					jsonnode8["z"].AsFloat,
					jsonnode8["x2"].AsFloat,
					jsonnode8["y2"].AsFloat,
					jsonnode8["z2"].AsFloat
				});
				eventKeyframe8.random = jsonnode8["r"].AsInt;
				DataManager.LSAnimation curveType8 = DataManager.inst.AnimationList[0];
				if (jsonnode8["ct"] != null)
				{
					curveType8 = DataManager.inst.AnimationListDictionaryStr[jsonnode8["ct"]];
					eventKeyframe8.curveType = curveType8;
				}
				eventKeyframe8.SetEventRandomValues(new float[]
				{
					jsonnode8["rx"].AsFloat,
					jsonnode8["ry"].AsFloat,
					jsonnode8["value_random_z"].AsFloat,
					jsonnode8["value_random_x2"].AsFloat,
					jsonnode8["value_random_y2"].AsFloat,
					jsonnode8["value_random_z2"].AsFloat
				});
				eventKeyframe8.active = false;
				DataManager.inst.gameData.eventObjects.allEvents[7].Add(eventKeyframe8);
				delay += 0.0001f;
			}
			for (int num3 = 0; num3 < _events["lens"].Count; num3++)
			{
				yield return new WaitForSeconds(delay);
				DataManager.GameData.EventKeyframe eventKeyframe9 = new DataManager.GameData.EventKeyframe();
				JSONNode jsonnode9 = _events["lens"][num3];
				eventKeyframe9.eventTime = jsonnode9["t"].AsFloat;
				eventKeyframe9.SetEventValues(new float[]
				{
					jsonnode9["x"].AsFloat,
					jsonnode9["y"].AsFloat,
					jsonnode9["z"].AsFloat
				});
				eventKeyframe9.random = jsonnode9["r"].AsInt;
				DataManager.LSAnimation curveType9 = DataManager.inst.AnimationList[0];
				if (jsonnode9["ct"] != null)
				{
					curveType9 = DataManager.inst.AnimationListDictionaryStr[jsonnode9["ct"]];
					eventKeyframe9.curveType = curveType9;
				}
				eventKeyframe9.SetEventRandomValues(new float[]
				{
					jsonnode9["rx"].AsFloat,
					jsonnode9["ry"].AsFloat,
					jsonnode9["value_random_z"].AsFloat
				});
				eventKeyframe9.active = false;
				DataManager.inst.gameData.eventObjects.allEvents[8].Add(eventKeyframe9);
				delay += 0.0001f;
			}
			for (int num4 = 0; num4 < _events["grain"].Count; num4++)
			{
				yield return new WaitForSeconds(delay);
				DataManager.GameData.EventKeyframe eventKeyframe10 = new DataManager.GameData.EventKeyframe();
				JSONNode jsonnode10 = _events["grain"][num4];
				eventKeyframe10.eventTime = jsonnode10["t"].AsFloat;
				eventKeyframe10.SetEventValues(new float[]
				{
					jsonnode10["x"].AsFloat,
					jsonnode10["y"].AsFloat,
					jsonnode10["z"].AsFloat
				});
				eventKeyframe10.random = jsonnode10["r"].AsInt;
				DataManager.LSAnimation curveType10 = DataManager.inst.AnimationList[0];
				if (jsonnode10["ct"] != null)
				{
					curveType10 = DataManager.inst.AnimationListDictionaryStr[jsonnode10["ct"]];
					eventKeyframe10.curveType = curveType10;
				}
				eventKeyframe10.SetEventRandomValues(new float[]
				{
					jsonnode10["rx"].AsFloat,
					jsonnode10["ry"].AsFloat,
					jsonnode10["value_random_z"].AsFloat
				});
				eventKeyframe10.active = false;
				DataManager.inst.gameData.eventObjects.allEvents[9].Add(eventKeyframe10);
				delay += 0.0001f;
			}
			if (DataManager.inst.gameData.eventObjects.allEvents.Count > 10)
            {
				for (int num4 = 0; num4 < _events["cg"].Count; num4++)
				{
					yield return new WaitForSeconds(delay);
					DataManager.GameData.EventKeyframe eventKeyframe11 = new DataManager.GameData.EventKeyframe();
					JSONNode jsonnode11 = _events["cg"][num4];
					eventKeyframe11.eventTime = jsonnode11["t"].AsFloat;
					eventKeyframe11.SetEventValues(new float[]
					{
					jsonnode11["x"].AsFloat,
					jsonnode11["y"].AsFloat,
					jsonnode11["z"].AsFloat,
					jsonnode11["x2"].AsFloat,
					jsonnode11["y2"].AsFloat,
					jsonnode11["z2"].AsFloat,
					jsonnode11["x3"].AsFloat,
					jsonnode11["y3"].AsFloat,
					jsonnode11["z3"].AsFloat
					});
					eventKeyframe11.random = jsonnode11["r"].AsInt;
					DataManager.LSAnimation curveType11 = DataManager.inst.AnimationList[0];
					if (jsonnode11["ct"] != null)
					{
						curveType11 = DataManager.inst.AnimationListDictionaryStr[jsonnode11["ct"]];
						eventKeyframe11.curveType = curveType11;
					}
					eventKeyframe11.SetEventRandomValues(new float[]
					{
					jsonnode11["rx"].AsFloat,
					jsonnode11["ry"].AsFloat,
					jsonnode11["value_random_z"].AsFloat
					});
					eventKeyframe11.active = false;
					DataManager.inst.gameData.eventObjects.allEvents[10].Add(eventKeyframe11);
					delay += 0.0001f;
				}
			}			
			EventManager.inst.updateEvents();
			yield break;
		}

		public static void CloseOpenFilePopup()
        {
			var aiGUI = EditorManager.inst.GetDialog("Open File Popup").Dialog.gameObject.GetComponent<AnimateInGUI>();
			aiGUI.OnDisableManual();
		}

		public static EditorManager.EditorDialog GetEditorDialog(string _dialog)
		{
			foreach (var dia in EditorManager.inst.EditorDialogs)
			{
				if (dia.Name == _dialog)
				{
					return dia;
				}
			}
			return EditorManager.inst.EditorDialogs[0];
		}

		public static IEnumerator SaveData(string _path, DataManager.GameData _data)
        {
			Debug.Log("Saving Beatmap");
			JSONNode jsonnode = JSON.Parse("{}");
			Debug.Log("Saving Editor Data");
			jsonnode["ed"]["timeline_pos"] = _data.beatmapData.editorData.timelinePos.ToString();
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
				for (int l = 0; l < DataManager.inst.CustomBeatmapThemes.Count; l++)
				{
					bool cont = false;

					foreach (var keyframe in DataManager.inst.gameData.eventObjects.allEvents[4])
                    {
						if (DataManager.inst.CustomBeatmapThemes[l].id == keyframe.eventValues[0].ToString())
						{
						}
					}

					if (cont)
					{
						jsonnode["themes"][l]["id"] = DataManager.inst.CustomBeatmapThemes[l].id;
						jsonnode["themes"][l]["name"] = DataManager.inst.CustomBeatmapThemes[l].name;
						jsonnode["themes"][l]["gui"] = LSColors.ColorToHex(DataManager.inst.CustomBeatmapThemes[l].guiColor);
						jsonnode["themes"][l]["bg"] = LSColors.ColorToHex(DataManager.inst.CustomBeatmapThemes[l].backgroundColor);
						for (int m = 0; m < DataManager.inst.CustomBeatmapThemes[l].playerColors.Count; m++)
						{
							jsonnode["themes"][l]["players"][m] = LSColors.ColorToHex(DataManager.inst.CustomBeatmapThemes[l].playerColors[m]);
						}
						for (int n = 0; n < DataManager.inst.CustomBeatmapThemes[l].objectColors.Count; n++)
						{
							jsonnode["themes"][l]["objs"][n] = LSColors.ColorToHex(DataManager.inst.CustomBeatmapThemes[l].objectColors[n]);
						}
						for (int num = 0; num < DataManager.inst.CustomBeatmapThemes[l].backgroundColors.Count; num++)
						{
							jsonnode["themes"][l]["bgs"][num] = LSColors.ColorToHex(DataManager.inst.CustomBeatmapThemes[l].backgroundColors[num]);
						}
					}
				}

				for (int index1 = 0; index1 < DataManager.inst.CustomBeatmapThemes.Count; ++index1)
				{
					bool cont = false;

					foreach (var keyframe in DataManager.inst.gameData.eventObjects.allEvents[4])
					{
						if (DataManager.inst.CustomBeatmapThemes[index1].id == keyframe.eventValues[0].ToString())
						{
							cont = true;
						}
					}

					if (cont)
					{
						jsonnode["themes"][index1]["id"] = DataManager.inst.CustomBeatmapThemes[index1].id;
						jsonnode["themes"][index1]["name"] = DataManager.inst.CustomBeatmapThemes[index1].name;
						jsonnode["themes"][index1]["gui"] = LSColors.ColorToHex(DataManager.inst.CustomBeatmapThemes[index1].guiColor);
						jsonnode["themes"][index1]["bg"] = LSColors.ColorToHex(DataManager.inst.CustomBeatmapThemes[index1].backgroundColor);
						for (int index2 = 0; index2 < DataManager.inst.CustomBeatmapThemes[index1].playerColors.Count; ++index2)
							jsonnode["themes"][index1]["players"][index2] = LSColors.ColorToHex(DataManager.inst.CustomBeatmapThemes[index1].playerColors[index2]);
						for (int index3 = 0; index3 < DataManager.inst.CustomBeatmapThemes[index1].objectColors.Count; ++index3)
							jsonnode["themes"][index1]["objs"][index3] = LSColors.ColorToHex(DataManager.inst.CustomBeatmapThemes[index1].objectColors[index3]);
						for (int index4 = 0; index4 < DataManager.inst.CustomBeatmapThemes[index1].backgroundColors.Count; ++index4)
							jsonnode["themes"][index1]["bgs"][index4] = LSColors.ColorToHex(DataManager.inst.CustomBeatmapThemes[index1].backgroundColors[index4]);
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
						jsonnode["events"]["cg"][num21]["value_random_z"] = _data.eventObjects.allEvents[10][num21].eventRandomValues[2].ToString();
					}
				}
			}
			Debug.Log("Saving Entire Beatmap");
			Debug.Log("Path: " + _path);
			RTFile.WriteToFile(_path, jsonnode.ToString());
			yield break;
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
		}

		public static void DeleteAllBackgrounds()
        {
			int num = DataManager.inst.gameData.backgroundObjects.Count;

			for (int i = 1; i < num; i++)
			{
				int nooo = Mathf.Clamp(i, 1, DataManager.inst.gameData.backgroundObjects.Count - 1);
				DataManager.inst.gameData.backgroundObjects.RemoveAt(nooo);
			}

			BackgroundManager.inst.UpdateBackgrounds();
			BackgroundEditor.inst.UpdateBackgroundList();
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

                //Saving
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
					tooltip.desc = "Saving Settings";
					tooltip.hint = "";

					hoverTooltip.tooltipLangauges.Clear();
					hoverTooltip.tooltipLangauges.Add(tooltip);

					button.onClick.m_Calls.m_ExecutingCalls.Clear();
					button.onClick.m_Calls.m_PersistentCalls.Clear();
					button.onClick.m_PersistentCalls.m_Calls.Clear();
					button.onClick.RemoveAllListeners();
					button.onClick.AddListener(delegate ()
					{
						currentCategory = EditorProperty.EditorPropCategory.Saving;
						RenderPropertiesWindow();
					});

					GameObject textGameObject = gameObject.transform.GetChild(0).gameObject;
					textGameObject.transform.SetParent(gameObject.transform);
					textGameObject.layer = 5;
					RectTransform textRectTransform = textGameObject.GetComponent<RectTransform>();
					textGameObject.GetComponent<CanvasRenderer>();
					Text textText = textGameObject.GetComponent<Text>();

					textRectTransform.anchoredPosition = Vector2.zero;
					textText.text = "Saving";
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

		public static void ScaleTabs(int _tab)
		{
			var editorDialog = EditorManager.inst.GetDialog("Editor Properties Popup").Dialog;
			for (int i = 0; i < editorDialog.Find("crumbs").childCount; i++)
			{
				var col = editorDialog.Find("crumbs").GetChild(i).GetComponent<Image>().color;
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
				case EditorProperty.EditorPropCategory.Saving:
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
				if (currentCategory == prop.propCategory && (string.IsNullOrEmpty(propertiesSearch) || prop.name.ToLower().Contains(propertiesSearch)))
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

								var l = x.transform.Find("title").GetComponent<Text>();
								l.font = textFont.font;
								l.text = prop.name;

								x.transform.Find("title").GetComponent<RectTransform>().sizeDelta = new Vector2(220f, 32f);

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
								x.name = "input [FLOATSLIDER]";

								var l = x.transform.Find("title").GetComponent<Text>();
								l.font = textFont.font;
								l.text = prop.name;

								x.transform.Find("title").GetComponent<RectTransform>().sizeDelta = new Vector2(220f, 32f);

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

								RectTransform xRT = x.GetComponent<RectTransform>();
								xRT.anchoredPosition = EditorPlugin.ORLDropdownPos.Value;

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

								//Inplement sliders / color picker / image thing

								break;
                            }
                    }
				}
            }
        }

		public static List<EditorProperty> editorProperties = new List<EditorProperty>
		{
			//General
			new EditorProperty("Debug", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.General, EditorPlugin.EditorDebug, ""),

			//Timeline

			//Saving
			new EditorProperty("Autosave Limit", EditorProperty.ValueType.Int, EditorProperty.EditorPropCategory.Saving, EditorPlugin.AutoSaveLimit, ""),
			new EditorProperty("Autosave Repeat", EditorProperty.ValueType.Float, EditorProperty.EditorPropCategory.Saving, EditorPlugin.AutoSaveRepeat, ""),
			new EditorProperty("Saving Updates Edited Date", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Saving, EditorPlugin.SavingUpdatesTime, ""),
			new EditorProperty("Level Loads Last Time", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Saving, EditorPlugin.IfEditorStartTime, ""),
			new EditorProperty("Level Pauses on Start", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Saving, EditorPlugin.IfEditorPauses, ""),
			new EditorProperty("Level Loads Individual", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Saving, EditorPlugin.IfEditorSlowLoads, ""),

			//Editor GUI
			new EditorProperty("Drag UI", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.EditorGUI, EditorPlugin.DragUI, ""),
			new EditorProperty("Notification Width", EditorProperty.ValueType.Float, EditorProperty.EditorPropCategory.EditorGUI, EditorPlugin.NotificationWidth, ""),
			new EditorProperty("Notification Size", EditorProperty.ValueType.Float, EditorProperty.EditorPropCategory.EditorGUI, EditorPlugin.NotificationSize, ""),
			new EditorProperty("Notification Direction", EditorProperty.ValueType.Enum, EditorProperty.EditorPropCategory.EditorGUI, EditorPlugin.NotificationDirection, ""),

			new EditorProperty("Open File Popup Scale", EditorProperty.ValueType.Vector2, EditorProperty.EditorPropCategory.EditorGUI, EditorPlugin.ORLSizeDelta, ""),
			new EditorProperty("Open File Popup Format", EditorProperty.ValueType.String, EditorProperty.EditorPropCategory.EditorGUI, EditorPlugin.FButtonFormat, ""),

			//Markers
			new EditorProperty("Marker Looping Active", EditorProperty.ValueType.Bool, EditorProperty.EditorPropCategory.Markers, EditorPlugin.MarkerLoop, ""),
			new EditorProperty("Marker Looping Start", EditorProperty.ValueType.Int, EditorProperty.EditorPropCategory.Markers, EditorPlugin.MarkerStartIndex, ""),
			new EditorProperty("Marker Looping End", EditorProperty.ValueType.Int, EditorProperty.EditorPropCategory.Markers, EditorPlugin.MarkerEndIndex, ""),
			new EditorProperty("Markers Color 1", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Markers, EditorPlugin.MarkerColN0, ""),
			new EditorProperty("Markers Color 2", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Markers, EditorPlugin.MarkerColN1, ""),
			new EditorProperty("Markers Color 3", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Markers, EditorPlugin.MarkerColN2, ""),
			new EditorProperty("Markers Color 4", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Markers, EditorPlugin.MarkerColN3, ""),
			new EditorProperty("Markers Color 5", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Markers, EditorPlugin.MarkerColN4, ""),
			new EditorProperty("Markers Color 6", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Markers, EditorPlugin.MarkerColN5, ""),
			new EditorProperty("Markers Color 7", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Markers, EditorPlugin.MarkerColN6, ""),
			new EditorProperty("Markers Color 8", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Markers, EditorPlugin.MarkerColN7, ""),
			new EditorProperty("Markers Color 9", EditorProperty.ValueType.Color, EditorProperty.EditorPropCategory.Markers, EditorPlugin.MarkerColN8, ""),

			//Fields
			new EditorProperty("Time Modify", EditorProperty.ValueType.FloatSlider, EditorProperty.EditorPropCategory.Fields, EditorPlugin.TimeModify, ""),

			//Preview

		};

		public static EditorProperty.EditorPropCategory currentCategory = EditorProperty.EditorPropCategory.Saving;

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
				Saving, //Includes AutoSaving
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
