using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using UnityEngine;
using UnityEngine.UI;

using DG.Tweening;

using LSFunctions;

namespace EditorManagement.Functions
{
    public class RTEditor : MonoBehaviour
    {
        public static RTEditor inst;

		public static bool ienumRunning;

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

						List<ObjEditor.ObjectSelection> listTime = new List<ObjEditor.ObjectSelection>();
						foreach (ObjEditor.ObjectSelection item in ObjEditor.inst.selectedObjects)
						{
							listTime.Add(item);
						}

						listTime = (from x in listTime
									orderby x.GetObjectData().StartTime ascending
									select x).ToList();

						DataManager.GameData.Prefab prefab = new DataManager.GameData.Prefab("deleted objects", 0, listTime[0].GetObjectData().StartTime, list);

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
						if (!string.IsNullOrEmpty(ObjEditor.inst.currentObjectSelection.GetObjectData().name))
						{
							EditorManager.inst.DisplayNotification("Deleted Beatmap Object\n[ " + ObjEditor.inst.currentObjectSelection.GetObjectData().name + " ].", 1f, EditorManager.NotificationType.Success, false);
						}
						else
                        {
							EditorManager.inst.DisplayNotification("Deleted Beatmap Object\n[ Object ].", 1f, EditorManager.NotificationType.Success, false);
						}

						DataManager.GameData.Prefab prefab = new DataManager.GameData.Prefab("deleted object", 0, ObjEditor.inst.currentObjectSelection.GetObjectData().StartTime, ObjEditor.inst.selectedObjects);

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
				if ((double)_offsetTime == 0.0)
				{
					prefabObject2.StartTime += EditorManager.inst.CurrentAudioPos;
					prefabObject2.StartTime += _obj.Offset;
				}
				else
				{
					prefabObject2.StartTime += _offsetTime;
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

		public static void WaveformTest()
		{
			inst.StartCoroutine(CreateWaveformAdvanced());
		}

		public static IEnumerator CreateWaveformAdvanced()
		{
			Debug.Log("Started Creating Waveform...");
			int audioTime = Mathf.Clamp((int)AudioManager.inst.CurrentAudioSource.clip.length * 48, 100, 15000);

			AudioClip clip = AudioManager.inst.CurrentAudioSource.clip;
			int textureWidth = audioTime;
			int textureHeight = 300;
			Color background = EditorPlugin.TimelineBGColor.Value;
			Color _top = EditorPlugin.TimelineTopColor.Value;
			Color _bottom = EditorPlugin.TimelineBottomColor.Value;

			float delay = 0f;
			int num = 40;
			num = clip.frequency / num;
			Texture2D texture2D = new Texture2D(textureWidth, textureHeight, TextureFormat.ARGB32, false);
			float[] array3;
			float[] array4;
			if (clip.channels > 1)
			{
				float[] array2 = new float[clip.samples * clip.channels];
				array3 = new float[clip.samples];
				array4 = new float[clip.samples];
				clip.GetData(array2, 0);
				array3 = array2.Where((float value, int index) => index % 2 != 0).ToArray<float>();
				array4 = array2.Where((float value, int index) => index % 2 == 0).ToArray<float>();
			}
			else
			{
				float[] array5 = new float[clip.samples * clip.channels];
				array3 = new float[clip.samples];
				array4 = new float[clip.samples];
				clip.GetData(array5, 0);
				array3 = array5;
				array4 = array5;
			}
			float[] array6 = new float[array3.Length / num];
			for (int j = 0; j < array6.Length; j++)
			{
				yield return new WaitForSeconds(delay);
				array6[j] = 0f;
				for (int k = 0; k < num; k++)
				{
					array6[j] += Mathf.Abs(array3[j * num + k]);
				}
				array6[j] /= (float)num;
				array6[j] *= 0.85f;
				delay += 0.0001f;
			}
			Debug.Log("Waveform: [ " + delay + " ]");
			for (int l = 0; l < array6.Length - 1; l++)
			{
				yield return new WaitForSeconds(delay);
				int num2 = 0;
				while ((float)num2 < (float)textureHeight * array6[l])
				{
					texture2D.SetPixel(textureWidth * l / array6.Length, (int)((float)textureHeight * array6[l]) - num2, _top);
					num2++;
				}
				delay += 0.0001f;
			}
			Debug.Log("Waveform: [ " + delay + " ]");
			array6 = new float[array4.Length / num];
			for (int m = 0; m < array6.Length; m++)
			{
				yield return new WaitForSeconds(delay);
				array6[m] = 0f;
				for (int n = 0; n < num; n++)
				{
					array6[m] += Mathf.Abs(array4[m * num + n]);
				}
				array6[m] /= (float)num;
				array6[m] *= 0.85f;
				delay += 0.0001f;
			}
			Debug.Log("Waveform: [ " + delay + " ]");
			for (int num3 = 0; num3 < array6.Length - 1; num3++)
			{
				yield return new WaitForSeconds(delay);
				int num4 = 0;
				while ((float)num4 < (float)textureHeight * array6[num3])
				{
					int x = textureWidth * num3 / array6.Length;
					int y = (int)array4[num3 * num + num4] - num4;
					if (texture2D.GetPixel(x, y) == _top)
					{
						texture2D.SetPixel(x, y, _top + _bottom);
					}
					else
					{
						texture2D.SetPixel(x, y, _bottom);
					}
					num4++;
				}
				delay += 0.0001f;
			}
			Debug.Log("Waveform: [ " + delay + " ]");
			texture2D.wrapMode = TextureWrapMode.Clamp;
			texture2D.filterMode = FilterMode.Point;
			texture2D.Apply();

			Sprite waveform = Sprite.Create(texture2D, new Rect(0f, 0f, (float)audioTime, 300f), new Vector2(0.5f, 0.5f), 100f);

			EditorManager.inst.timeline.GetComponent<Image>().sprite = waveform;
			EditorManager.inst.timelineWaveformOverlay.GetComponent<Image>().sprite = EditorManager.inst.timeline.GetComponent<Image>().sprite;

			Debug.Log("Finished Creating Waveform!");

			yield break;
		}

		public enum DialogType
		{
			Popup,
			Tooltip,
			Event,
			Checkpoint,
			Object,
			Background,
			Metadata,
			Timeline,
			Prefab,
			Settings,
			Marker,
			EditorBar,
			Preview
		}
	}
}
