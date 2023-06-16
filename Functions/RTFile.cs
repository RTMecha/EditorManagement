using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.IO;

using HarmonyLib;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

using SimpleJSON;
using DG.Tweening;

namespace EditorManagement.Functions
{
	public class RTFile : MonoBehaviour
	{
		public static IEnumerator ParseBeatmap(string _json, bool editor = false)
		{
			JSONNode jsonnode = JSON.Parse(_json);
			if (!editor)
			{
				DataManager.inst.gameData.ParseThemeData(jsonnode["themes"]);
			}
			DataManager.inst.gameData.ParseEditorData(jsonnode["ed"]);
			DataManager.inst.gameData.ParseLevelData(jsonnode["level_data"]);
			DataManager.inst.gameData.ParseCheckpointData(jsonnode["checkpoints"]);
			ParsePrefabs(jsonnode["prefabs"]);
			ParsePrefabObjects(jsonnode["prefab_objects"]);
			DataManager.inst.StartCoroutine(ParseGameObjects(jsonnode["beatmap_objects"]));
			DataManager.inst.gameData.ParseBackgroundObjects(jsonnode["bg_objects"]);
			DataManager.inst.StartCoroutine(ParseEventObjects(jsonnode["events"]));
			yield break;
		}

		public static IEnumerator ParseThemeData(JSONNode _themeData)
		{
			UnityEngine.Debug.LogFormat("{0}Parse Theme Data", EditorPlugin.className);
			float delay = 0f;
			DataManager.inst.CustomBeatmapThemes.Clear();
			DataManager.inst.BeatmapThemeIDToIndex.Clear();
			DataManager.inst.BeatmapThemeIndexToID.Clear();
			int num = 0;
			foreach (DataManager.BeatmapTheme beatmapTheme in DataManager.inst.BeatmapThemes)
			{
				if (ConfigEntries.IfEditorSlowLoads.Value)
				{
					yield return new WaitForSeconds(delay);
				}
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
				if (ConfigEntries.IfEditorSlowLoads.Value)
				{
					yield return new WaitForSeconds(delay);
				}
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
				if (ConfigEntries.IfEditorSlowLoads.Value)
				{
					yield return new WaitForSeconds(delay);
				}
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

		public static AssetBundle GetAssetBundle(string _filepath, string _bundle)
		{
			return AssetBundle.LoadFromFile(Path.Combine(_filepath, _bundle));
		}

		public static void GetAsset(string _filepath, string _bundle, string _filename, Action<GameObject> callback)
        {
			var assetBundle = GetAssetBundle(_filepath, _bundle);
			var prefab = assetBundle.LoadAsset<GameObject>(_filename);
			callback(Instantiate(prefab));

			assetBundle.Unload(false);
		}

		public static void GetAsset(string _filepath, string _bundle, string _filename, Action<Font> callback)
        {
			var assetBundle = GetAssetBundle(_filepath, _bundle);
			var prefab = assetBundle.LoadAsset<Font>(_filename);
			callback(Instantiate(prefab));

			assetBundle.Unload(false);
		}

		public static IEnumerator ParseObject(JSONNode _object, Action<DataManager.GameData.BeatmapObject> action)
		{
			float delay = 0f;
			int num = 0;
			List<List<DataManager.GameData.EventKeyframe>> list = new List<List<DataManager.GameData.EventKeyframe>>();
			list.Add(new List<DataManager.GameData.EventKeyframe>());
			list.Add(new List<DataManager.GameData.EventKeyframe>());
			list.Add(new List<DataManager.GameData.EventKeyframe>());
			list.Add(new List<DataManager.GameData.EventKeyframe>());
			if (_object["events"] != null)
			{
				for (int i = 0; i < _object["events"]["pos"].Count; i++)
				{
					if (ConfigEntries.IfEditorSlowLoads.Value)
					{
						yield return new WaitForSeconds(delay);
					}
					DataManager.GameData.EventKeyframe eventKeyframe = new DataManager.GameData.EventKeyframe();
					JSONNode jsonnode = _object["events"]["pos"][i];
					eventKeyframe.eventTime = jsonnode["t"].AsFloat;
					if (!string.IsNullOrEmpty(jsonnode["z"]))
					{
						eventKeyframe.SetEventValues(new float[]
						{
							jsonnode["x"].AsFloat,
							jsonnode["y"].AsFloat,
							jsonnode["z"].AsFloat
						});
					}
					else
                    {
						eventKeyframe.SetEventValues(new float[]
						{
							jsonnode["x"].AsFloat,
							jsonnode["y"].AsFloat,
							0f
						});
					}
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
							jsonnode["ry"].AsFloat,
							jsonnode["rz"].AsFloat
					});
					eventKeyframe.active = false;
					list[0].Add(eventKeyframe);
					delay += 0.0001f;
				}
				for (int j = 0; j < _object["events"]["sca"].Count; j++)
				{
					if (ConfigEntries.IfEditorSlowLoads.Value)
					{
						yield return new WaitForSeconds(delay);
					}
					DataManager.GameData.EventKeyframe eventKeyframe2 = new DataManager.GameData.EventKeyframe();
					JSONNode jsonnode2 = _object["events"]["sca"][j];
					eventKeyframe2.eventTime = jsonnode2["t"].AsFloat;
					eventKeyframe2.SetEventValues(new float[]
					{
							jsonnode2["x"].AsFloat,
							jsonnode2["y"].AsFloat
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
							jsonnode2["rx"].AsFloat,
							jsonnode2["ry"].AsFloat,
							jsonnode2["rz"].AsFloat
					});
					list[1].Add(eventKeyframe2);
					delay += 0.0001f;
				}
				for (int k = 0; k < _object["events"]["rot"].Count; k++)
				{
					if (ConfigEntries.IfEditorSlowLoads.Value)
					{
						yield return new WaitForSeconds(delay);
					}
					DataManager.GameData.EventKeyframe eventKeyframe3 = new DataManager.GameData.EventKeyframe();
					JSONNode jsonnode3 = _object["events"]["rot"][k];
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
							jsonnode3["rx"].AsFloat,
							0f,
							jsonnode3["rz"].AsFloat
					});
					list[2].Add(eventKeyframe3);
					delay += 0.0001f;
				}
				for (int l = 0; l < _object["events"]["col"].Count; l++)
				{
					if (ConfigEntries.IfEditorSlowLoads.Value)
					{
						yield return new WaitForSeconds(delay);
					}
					DataManager.GameData.EventKeyframe eventKeyframe4 = new DataManager.GameData.EventKeyframe();
					JSONNode jsonnode4 = _object["events"]["col"][l];
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
					list[3].Add(eventKeyframe4);
					delay += 0.0001f;
				}
			}
			DataManager.GameData.BeatmapObject beatmapObject = new DataManager.GameData.BeatmapObject();
			if (_object["id"] != null)
			{
				beatmapObject.id = _object["id"];
			}
			else
			{
				num++;
			}
			if (_object["piid"] != null)
			{
				beatmapObject.prefabInstanceID = _object["piid"];
			}
			if (_object["pid"] != null)
			{
				beatmapObject.prefabID = _object["pid"];
			}
			if (_object["p"] != null)
			{
				beatmapObject.parent = _object["p"];
			}
			if (_object["pt"] != null)
			{
				string pt = _object["pt"];
				AccessTools.Field(typeof(DataManager.GameData.BeatmapObject), "parentType").SetValue(beatmapObject, pt);
			}
			if (_object["po"] != null)
			{
				AccessTools.Field(typeof(DataManager.GameData.BeatmapObject), "parentOffsets").SetValue(beatmapObject, new List<float>(from n in _object["po"].AsArray.Children
																																	   select n.AsFloat).ToList());
			}
			if (_object["d"] != null)
			{
				AccessTools.Field(typeof(DataManager.GameData.BeatmapObject), "depth").SetValue(beatmapObject, _object["d"].AsInt);
			}
			else
			{
				num++;
			}
			if (_object["empty"] != null)
			{
				beatmapObject.objectType = (_object["empty"].AsBool ? DataManager.GameData.BeatmapObject.ObjectType.Empty : DataManager.GameData.BeatmapObject.ObjectType.Normal);
			}
			else if (_object["h"] != null)
			{
				beatmapObject.objectType = (_object["h"].AsBool ? DataManager.GameData.BeatmapObject.ObjectType.Helper : DataManager.GameData.BeatmapObject.ObjectType.Normal);
			}
			else if (_object["ot"] != null)
			{
				beatmapObject.objectType = (DataManager.GameData.BeatmapObject.ObjectType)_object["ot"].AsInt;
			}
			if (_object["st"] != null)
			{
				beatmapObject.StartTime = _object["st"].AsFloat;
			}
			else
			{
				num++;
			}
			if (_object["name"] != null)
			{
				beatmapObject.name = _object["name"];
			}
			if (_object["shape"] != null)
			{
				beatmapObject.shape = _object["shape"].AsInt;
			}
			if (_object["so"] != null)
			{
				beatmapObject.shapeOption = _object["so"].AsInt;
			}
			if (_object["text"] != null)
			{
				beatmapObject.text = _object["text"];
			}
			if (_object["ak"] != null)
			{
				beatmapObject.autoKillType = (_object["ak"].AsBool ? DataManager.GameData.BeatmapObject.AutoKillType.LastKeyframe : DataManager.GameData.BeatmapObject.AutoKillType.OldStyleNoAutokill);
			}
			else if (_object["akt"] != null)
			{
				beatmapObject.autoKillType = (DataManager.GameData.BeatmapObject.AutoKillType)_object["akt"].AsInt;
			}
			if (_object["ako"] != null)
			{
				beatmapObject.autoKillOffset = _object["ako"].AsFloat;
			}
			if (_object["o"] != null)
			{
				beatmapObject.origin = new Vector2(_object["o"]["x"].AsFloat, _object["o"]["y"].AsFloat);
			}
			else
			{
				num++;
			}
			if (_object["ed"]["bin"] != null)
			{
				beatmapObject.editorData.locked = _object["ed"]["locked"].AsBool;
			}
			if (_object["ed"]["bin"] != null)
			{
				beatmapObject.editorData.collapse = _object["ed"]["shrink"].AsBool;
			}
			if (_object["ed"]["bin"] != null)
			{
				beatmapObject.editorData.Bin = _object["ed"]["bin"].AsInt;
			}
			if (_object["ed"]["layer"] != null)
			{
				beatmapObject.editorData.Layer = _object["ed"]["layer"].AsInt;
			}
			beatmapObject.events = list;
			action(beatmapObject);
			yield break;
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
				if (ConfigEntries.IfEditorSlowLoads.Value)
				{
					yield return new WaitForSeconds(delay);
				}

				DataManager.GameData.BeatmapObject beatmapObject = null;
				DataManager.inst.StartCoroutine(ParseObject(_objects[i], delegate (DataManager.GameData.BeatmapObject beatmapObject1)
				{
					beatmapObject = beatmapObject1;
				}));

				if (ConfigEntries.IfEditorSlowLoads.Value)
				{
					yield return new WaitForSeconds(0.2f);
				}

				if (beatmapObject != null)
				{
					DataManager.inst.gameData.beatmapObjects.Add(beatmapObject);
					//updateObjects(beatmapObject);
					if (EditorManager.inst != null)
					{
						ObjEditor.ObjectSelection objectSelection = new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Object, i);
						ObjEditor.inst.RenderTimelineObject(objectSelection);
					}
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

			var allEvents = DataManager.inst.gameData.eventObjects.allEvents;

			for (int i = 0; i < _events["pos"].Count; i++)
			{
				if (ConfigEntries.IfEditorSlowLoads.Value)
				{
					yield return new WaitForSeconds(delay);
				}
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
				if (ConfigEntries.IfEditorSlowLoads.Value)
				{
					yield return new WaitForSeconds(delay);
				}
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
				if (ConfigEntries.IfEditorSlowLoads.Value)
				{
					yield return new WaitForSeconds(delay);
				}
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
				if (ConfigEntries.IfEditorSlowLoads.Value)
				{
					yield return new WaitForSeconds(delay);
				}
				DataManager.GameData.EventKeyframe eventKeyframe4 = new DataManager.GameData.EventKeyframe();
				JSONNode jsonnode4 = _events["shake"][l];
				eventKeyframe4.eventTime = jsonnode4["t"].AsFloat;
				if (!string.IsNullOrEmpty(jsonnode4["z"]))
				{
					eventKeyframe4.SetEventValues(new float[]
					{
						jsonnode4["x"].AsFloat,
						jsonnode4["y"].AsFloat,
						jsonnode4["z"].AsFloat
					});
				}
				else
				{
					eventKeyframe4.SetEventValues(new float[]
					{
						jsonnode4["x"].AsFloat,
						1f,
						1f
					});
				}
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
				if (ConfigEntries.IfEditorSlowLoads.Value)
				{
					yield return new WaitForSeconds(delay);
				}
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
				if (ConfigEntries.IfEditorSlowLoads.Value)
				{
					yield return new WaitForSeconds(delay);
				}
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
				if (ConfigEntries.IfEditorSlowLoads.Value)
				{
					yield return new WaitForSeconds(delay);
				}
				DataManager.GameData.EventKeyframe eventKeyframe7 = new DataManager.GameData.EventKeyframe();
				JSONNode jsonnode7 = _events["bloom"][num];
				eventKeyframe7.eventTime = jsonnode7["t"].AsFloat;
				if (!string.IsNullOrEmpty(jsonnode7["y"]))
                {
					eventKeyframe7.SetEventValues(new float[]
					{
						jsonnode7["x"].AsFloat,
						jsonnode7["y"].AsFloat,
						jsonnode7["z"].AsFloat,
						jsonnode7["x2"].AsFloat,
						jsonnode7["y2"].AsFloat,
					});
				}
				else
				{
					eventKeyframe7.SetEventValues(new float[]
					{
						jsonnode7["x"].AsFloat,
						7f,
						1f,
						0f,
						18f
					});
				}
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
				if (ConfigEntries.IfEditorSlowLoads.Value)
				{
					yield return new WaitForSeconds(delay);
				}
				DataManager.GameData.EventKeyframe eventKeyframe8 = new DataManager.GameData.EventKeyframe();
				JSONNode jsonnode8 = _events["vignette"][num2];
				eventKeyframe8.eventTime = jsonnode8["t"].AsFloat;
				if (!string.IsNullOrEmpty(jsonnode8["x3"]))
				{
					eventKeyframe8.SetEventValues(new float[]
					{
						jsonnode8["x"].AsFloat,
						jsonnode8["y"].AsFloat,
						jsonnode8["z"].AsFloat,
						jsonnode8["x2"].AsFloat,
						jsonnode8["y2"].AsFloat,
						jsonnode8["z2"].AsFloat,
						jsonnode8["x3"].AsFloat
					});
				}
				else
				{
					eventKeyframe8.SetEventValues(new float[]
					{
						jsonnode8["x"].AsFloat,
						jsonnode8["y"].AsFloat,
						jsonnode8["z"].AsFloat,
						jsonnode8["x2"].AsFloat,
						jsonnode8["y2"].AsFloat,
						jsonnode8["z2"].AsFloat,
						18f
					});
				}
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
				if (ConfigEntries.IfEditorSlowLoads.Value)
				{
					yield return new WaitForSeconds(delay);
				}
				DataManager.GameData.EventKeyframe eventKeyframe9 = new DataManager.GameData.EventKeyframe();
				JSONNode jsonnode9 = _events["lens"][num3];
				eventKeyframe9.eventTime = jsonnode9["t"].AsFloat;
				if (!string.IsNullOrEmpty(jsonnode9["y"]))
				{
					eventKeyframe9.SetEventValues(new float[]
					{
						jsonnode9["x"].AsFloat,
						jsonnode9["y"].AsFloat,
						jsonnode9["z"].AsFloat,
						jsonnode9["x2"].AsFloat,
						jsonnode9["y2"].AsFloat,
						jsonnode9["z2"].AsFloat,
					});
				}
				else
				{
					eventKeyframe9.SetEventValues(new float[]
					{
						jsonnode9["x"].AsFloat,
						0f,
						0f,
						1f,
						1f,
						1f,
					});
				}
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
				if (ConfigEntries.IfEditorSlowLoads.Value)
				{
					yield return new WaitForSeconds(delay);
				}
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
			if (allEvents.Count > 10)
			{
				for (int num4 = 0; num4 < _events["cg"].Count; num4++)
				{
					if (ConfigEntries.IfEditorSlowLoads.Value)
					{
						yield return new WaitForSeconds(delay);
					}
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

				for (int num4 = 0; num4 < _events["rip"].Count; num4++)
				{
					if (ConfigEntries.IfEditorSlowLoads.Value)
					{
						yield return new WaitForSeconds(delay);
					}
					DataManager.GameData.EventKeyframe eventKeyframe11 = new DataManager.GameData.EventKeyframe();
					JSONNode jsonnode11 = _events["rip"][num4];
					eventKeyframe11.eventTime = jsonnode11["t"].AsFloat;
					eventKeyframe11.SetEventValues(new float[]
					{
						jsonnode11["x"].AsFloat,
						jsonnode11["y"].AsFloat,
						jsonnode11["z"].AsFloat,
						jsonnode11["x2"].AsFloat,
						jsonnode11["y2"].AsFloat
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
						jsonnode11["ry"].AsFloat
					});
					eventKeyframe11.active = false;
					DataManager.inst.gameData.eventObjects.allEvents[11].Add(eventKeyframe11);
					delay += 0.0001f;
				}

				for (int num4 = 0; num4 < _events["rb"].Count; num4++)
				{
					if (ConfigEntries.IfEditorSlowLoads.Value)
					{
						yield return new WaitForSeconds(delay);
					}
					DataManager.GameData.EventKeyframe eventKeyframe11 = new DataManager.GameData.EventKeyframe();
					JSONNode jsonnode11 = _events["rb"][num4];
					eventKeyframe11.eventTime = jsonnode11["t"].AsFloat;
					eventKeyframe11.SetEventValues(new float[]
					{
						jsonnode11["x"].AsFloat,
						jsonnode11["y"].AsFloat
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
						jsonnode11["ry"].AsFloat
					});
					eventKeyframe11.active = false;
					DataManager.inst.gameData.eventObjects.allEvents[12].Add(eventKeyframe11);
					delay += 0.0001f;
				}

				for (int num4 = 0; num4 < _events["cs"].Count; num4++)
				{
					if (ConfigEntries.IfEditorSlowLoads.Value)
					{
						yield return new WaitForSeconds(delay);
					}
					DataManager.GameData.EventKeyframe eventKeyframe11 = new DataManager.GameData.EventKeyframe();
					JSONNode jsonnode11 = _events["cs"][num4];
					eventKeyframe11.eventTime = jsonnode11["t"].AsFloat;
					eventKeyframe11.SetEventValues(new float[]
					{
						jsonnode11["x"].AsFloat,
						jsonnode11["y"].AsFloat
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
						jsonnode11["ry"].AsFloat
					});
					eventKeyframe11.active = false;
					DataManager.inst.gameData.eventObjects.allEvents[13].Add(eventKeyframe11);
					delay += 0.0001f;
				}

				for (int num4 = 0; num4 < _events["offset"].Count; num4++)
				{
					if (ConfigEntries.IfEditorSlowLoads.Value)
					{
						yield return new WaitForSeconds(delay);
					}
					DataManager.GameData.EventKeyframe eventKeyframe11 = new DataManager.GameData.EventKeyframe();
					JSONNode jsonnode11 = _events["offset"][num4];
					eventKeyframe11.eventTime = jsonnode11["t"].AsFloat;
					eventKeyframe11.SetEventValues(new float[]
					{
						jsonnode11["x"].AsFloat,
						jsonnode11["y"].AsFloat
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
						jsonnode11["ry"].AsFloat
					});
					eventKeyframe11.active = false;
					DataManager.inst.gameData.eventObjects.allEvents[14].Add(eventKeyframe11);
					delay += 0.0001f;
				}

				for (int num4 = 0; num4 < _events["grd"].Count; num4++)
				{
					if (ConfigEntries.IfEditorSlowLoads.Value)
					{
						yield return new WaitForSeconds(delay);
					}
					DataManager.GameData.EventKeyframe eventKeyframe11 = new DataManager.GameData.EventKeyframe();
					JSONNode jsonnode11 = _events["grd"][num4];
					eventKeyframe11.eventTime = jsonnode11["t"].AsFloat;
					eventKeyframe11.SetEventValues(new float[]
					{
						jsonnode11["x"].AsFloat,
						jsonnode11["y"].AsFloat,
						jsonnode11["z"].AsFloat,
						jsonnode11["x2"].AsFloat,
						jsonnode11["y2"].AsFloat,
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
						jsonnode11["ry"].AsFloat
					});
					eventKeyframe11.active = false;
					DataManager.inst.gameData.eventObjects.allEvents[15].Add(eventKeyframe11);
					delay += 0.0001f;
				}
				
				for (int num4 = 0; num4 < _events["dbv"].Count; num4++)
				{
					if (ConfigEntries.IfEditorSlowLoads.Value)
					{
						yield return new WaitForSeconds(delay);
					}
					DataManager.GameData.EventKeyframe eventKeyframe11 = new DataManager.GameData.EventKeyframe();
					JSONNode jsonnode11 = _events["dbv"][num4];
					eventKeyframe11.eventTime = jsonnode11["t"].AsFloat;
					eventKeyframe11.SetEventValues(new float[]
					{
						jsonnode11["x"].AsFloat,
						jsonnode11["y"].AsFloat
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
						jsonnode11["ry"].AsFloat
					});
					eventKeyframe11.active = false;
					DataManager.inst.gameData.eventObjects.allEvents[16].Add(eventKeyframe11);
					delay += 0.0001f;
				}

				for (int num4 = 0; num4 < _events["scan"].Count; num4++)
				{
					if (ConfigEntries.IfEditorSlowLoads.Value)
					{
						yield return new WaitForSeconds(delay);
					}
					DataManager.GameData.EventKeyframe eventKeyframe11 = new DataManager.GameData.EventKeyframe();
					JSONNode jsonnode11 = _events["scan"][num4];
					eventKeyframe11.eventTime = jsonnode11["t"].AsFloat;
					eventKeyframe11.SetEventValues(new float[]
					{
						jsonnode11["x"].AsFloat,
						jsonnode11["y"].AsFloat,
						jsonnode11["z"].AsFloat,
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
						jsonnode11["ry"].AsFloat
					});
					eventKeyframe11.active = false;
					DataManager.inst.gameData.eventObjects.allEvents[17].Add(eventKeyframe11);
					delay += 0.0001f;
				}

				for (int num4 = 0; num4 < _events["blur"].Count; num4++)
				{
					if (ConfigEntries.IfEditorSlowLoads.Value)
					{
						yield return new WaitForSeconds(delay);
					}
					DataManager.GameData.EventKeyframe eventKeyframe11 = new DataManager.GameData.EventKeyframe();
					JSONNode jsonnode11 = _events["blur"][num4];
					eventKeyframe11.eventTime = jsonnode11["t"].AsFloat;
					eventKeyframe11.SetEventValues(new float[]
					{
						jsonnode11["x"].AsFloat,
						jsonnode11["y"].AsFloat
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
						jsonnode11["ry"].AsFloat
					});
					eventKeyframe11.active = false;
					DataManager.inst.gameData.eventObjects.allEvents[18].Add(eventKeyframe11);
					delay += 0.0001f;
				}

				for (int num4 = 0; num4 < _events["pixel"].Count; num4++)
				{
					if (ConfigEntries.IfEditorSlowLoads.Value)
					{
						yield return new WaitForSeconds(delay);
					}
					DataManager.GameData.EventKeyframe eventKeyframe11 = new DataManager.GameData.EventKeyframe();
					JSONNode jsonnode11 = _events["pixel"][num4];
					eventKeyframe11.eventTime = jsonnode11["t"].AsFloat;
					eventKeyframe11.SetEventValues(new float[]
					{
						jsonnode11["x"].AsFloat,
						jsonnode11["y"].AsFloat
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
						jsonnode11["ry"].AsFloat
					});
					eventKeyframe11.active = false;
					DataManager.inst.gameData.eventObjects.allEvents[19].Add(eventKeyframe11);
					delay += 0.0001f;
				}

				for (int num4 = 0; num4 < _events["bg"].Count; num4++)
				{
					if (ConfigEntries.IfEditorSlowLoads.Value)
					{
						yield return new WaitForSeconds(delay);
					}
					DataManager.GameData.EventKeyframe eventKeyframe11 = new DataManager.GameData.EventKeyframe();
					JSONNode jsonnode11 = _events["bg"][num4];
					eventKeyframe11.eventTime = jsonnode11["t"].AsFloat;
					eventKeyframe11.SetEventValues(new float[]
					{
						jsonnode11["x"].AsFloat,
						jsonnode11["y"].AsFloat
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
						jsonnode11["ry"].AsFloat
					});
					eventKeyframe11.active = false;
					DataManager.inst.gameData.eventObjects.allEvents[20].Add(eventKeyframe11);
					delay += 0.0001f;
				}

				for (int num4 = 0; num4 < _events["overlay"].Count; num4++)
				{
					if (ConfigEntries.IfEditorSlowLoads.Value)
					{
						yield return new WaitForSeconds(delay);
					}
					DataManager.GameData.EventKeyframe eventKeyframe11 = new DataManager.GameData.EventKeyframe();
					JSONNode jsonnode11 = _events["overlay"][num4];
					eventKeyframe11.eventTime = jsonnode11["t"].AsFloat;
					eventKeyframe11.SetEventValues(new float[]
					{
						jsonnode11["x"].AsFloat,
						jsonnode11["y"].AsFloat
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
						jsonnode11["ry"].AsFloat
					});
					eventKeyframe11.active = false;
					DataManager.inst.gameData.eventObjects.allEvents[21].Add(eventKeyframe11);
					delay += 0.0001f;
				}

				for (int num4 = 0; num4 < _events["timeline"].Count; num4++)
				{
					if (ConfigEntries.IfEditorSlowLoads.Value)
					{
						yield return new WaitForSeconds(delay);
					}
					DataManager.GameData.EventKeyframe eventKeyframe11 = new DataManager.GameData.EventKeyframe();
					JSONNode jsonnode11 = _events["timeline"][num4];
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
						jsonnode11["ry"].AsFloat
					});
					eventKeyframe11.active = false;
					DataManager.inst.gameData.eventObjects.allEvents[22].Add(eventKeyframe11);
					delay += 0.0001f;
				}
			}

			for (int type = 0; type < allEvents.Count; type++)
			{
				if (allEvents[type].Count < 1)
				{
					allEvents[type].Add(new DataManager.GameData.EventKeyframe
					{
						eventValues = new float[9],
						eventTime = 0f
					});
					if (type == 11)
					{
						allEvents[type][0].eventValues[2] = 1f;
					}
					if (type == 12)
					{
						allEvents[type][0].eventValues[1] = 6f;
					}
					if (type == 15)
					{
						allEvents[type][0].eventValues[2] = 18f;
						allEvents[type][0].eventValues[3] = 18f;
					}
					if (type == 18)
					{
						allEvents[type][0].eventValues[1] = 6f;
					}
					if (type == 20)
					{
						allEvents[type][0].eventValues[0] = 18f;
					}
					if (type == 21)
					{
						allEvents[type][0].eventValues[0] = 18f;
					}
					if (type == 22)
					{
						//-532?
						allEvents[type][0].eventValues[2] = -342f;
						allEvents[type][0].eventValues[3] = 1f;
						allEvents[type][0].eventValues[4] = 1f;
						allEvents[type][0].eventValues[6] = 18f;
					}
				}
			}
			EventManager.inst.updateEvents();
			yield break;
		}

		public static ObjectManager updateObjects(DataManager.GameData.BeatmapObject _beatmapObject, bool setInactive = true)
		{
			if (ObjectManager.inst != null)
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
						if (setInactive)
							_beatmapObject.active = false;
						for (int j = 0; j < _beatmapObject.events.Count; j++)
						{
							for (int k = 0; k < _beatmapObject.events[j].Count; k++)
							{
								if (setInactive)
									_beatmapObject.events[j][k].active = false;
							}
						}
					}
				}
				ObjectManager.inst.updateObjects(_beatmapObject.id);
				return ObjectManager.inst;
			}
			return null;
		}

		public static IEnumerator IupdateObjects(DataManager.GameData.BeatmapObject _beatmapObject, bool _setInactive = true)
		{
			if (!_beatmapObject.fromPrefab)
			{
				var id = _beatmapObject.id;
				if (ObjectManager.inst.beatmapGameObjects.ContainsKey(id))
				{
					var gameObjectRef = ObjectManager.inst.beatmapGameObjects[id];
					Destroy(gameObjectRef.obj);
					gameObjectRef.sequence.all.Kill();
					gameObjectRef.sequence.col.Kill();
					ObjectManager.inst.beatmapGameObjects.Remove(id);
				}
				if (_setInactive)
					_beatmapObject.active = false;
				for (int i = 0; i < _beatmapObject.events.Count; i++)
				{
					for (int j = 0; j < _beatmapObject.events[i].Count; j++)
                    {
						if (_setInactive)
							_beatmapObject.events[i][j].active = false;
                    }
				}

				foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
				{
					if (beatmapObject.prefabInstanceID == _beatmapObject.id && ObjectManager.inst.beatmapGameObjects.ContainsKey(beatmapObject.id))
					{
						Destroy(ObjectManager.inst.beatmapGameObjects[beatmapObject.id].obj);
						ObjectManager.inst.beatmapGameObjects[beatmapObject.id].sequence.all.Kill(false);
						ObjectManager.inst.beatmapGameObjects[beatmapObject.id].sequence.col.Kill(false);
						ObjectManager.inst.beatmapGameObjects.Remove(beatmapObject.id);
					}
				}
				for (int i = 0; i < DataManager.inst.gameData.prefabObjects.Count; i++)
				{
					if (DataManager.inst.gameData.prefabObjects[i].ID == _beatmapObject.id)
					{
						ObjectManager.inst.AddPrefabToLevel(DataManager.inst.gameData.prefabObjects[i]);
					}
				}
			}
			yield break;
		}

		public static IEnumerator IupdateObjects()
		{
			var objectManager = ObjectManager.inst;
			var dataManager = DataManager.inst;
			foreach (var beatmapObject in dataManager.gameData.beatmapObjects)
			{
				if (objectManager.beatmapGameObjects.ContainsKey(beatmapObject.id))
				{
					objectManager.beatmapGameObjects[beatmapObject.id].sequence.all.Kill(false);
					objectManager.beatmapGameObjects[beatmapObject.id].sequence.col.Kill(false);
					Destroy(objectManager.beatmapGameObjects[beatmapObject.id].obj);
				}
			}
			objectManager.beatmapGameObjects = new Dictionary<string, ObjectManager.GameObjectRef>();
			dataManager.gameData.beatmapObjects.RemoveAll((DataManager.GameData.BeatmapObject x) => x.fromPrefab);
			for (int i = 0; i < dataManager.gameData.prefabObjects.Count; i++)
			{
				objectManager.AddPrefabToLevel(dataManager.gameData.prefabObjects[i]);
			}
			for (int j = 0; j < dataManager.gameData.beatmapObjects.Count; j++)
			{
				if (!dataManager.gameData.beatmapObjects[j].fromPrefab)
				{
					dataManager.gameData.beatmapObjects[j].active = false;
					for (int k = 0; k < dataManager.gameData.beatmapObjects[j].events.Count; k++)
					{
						for (int l = 0; l < dataManager.gameData.beatmapObjects[j].events[k].Count; l++)
						{
							dataManager.gameData.beatmapObjects[j].events[k][l].active = false;
						}
					}
				}
			}
			objectManager.ReGenSeed();
			yield break;
		}

		public static string GetApplicationDirectory()
		{
			return Application.dataPath.Substring(0, Application.dataPath.LastIndexOf("/")) + "/";
		}

		public static string GetPersistentApplicationDirectory()
        {
			return Application.persistentDataPath;
        }

		public static IEnumerator LoadImage(string _filepath, Action<Sprite> callback)
		{
			Texture2D tex = new Texture2D(256, 256, TextureFormat.RGBA32, true);
			tex.requestedMipmapLevel = 3;
			Sprite obj;
			using (UnityWebRequest unityWebRequest = new UnityWebRequest("file://" + _filepath))
			{
				while (!unityWebRequest.isDone)
				{
					yield return null;
				}
			}
        }

		public static bool FileExists(string _filePath)
		{
			return !string.IsNullOrEmpty(_filePath) && File.Exists(_filePath);
		}

		public static bool DirectoryExists(string _directoryPath)
		{
			return !string.IsNullOrEmpty(_directoryPath) && Directory.Exists(_directoryPath);
		}

		public static void WriteToFile(string path, string json)
		{
			StreamWriter streamWriter = new StreamWriter(path);
			streamWriter.Write(json);
			streamWriter.Flush();
			streamWriter.Close();
		}

		public static class OpenInFileBrowser
		{
			public static bool IsInMacOS
			{
				get
				{
					return SystemInfo.operatingSystem.IndexOf("Mac OS") != -1;
				}
			}

			public static bool IsInWinOS
			{
				get
				{
					return SystemInfo.operatingSystem.IndexOf("Windows") != -1;
				}
			}

			public static void OpenInMac(string path)
			{
				bool flag = false;
				string text = path.Replace("\\", "/");
				if (Directory.Exists(text))
				{
					flag = true;
				}
				if (!text.StartsWith("\""))
				{
					text = "\"" + text;
				}
				if (!text.EndsWith("\""))
				{
					text += "\"";
				}
				string arguments = (flag ? "" : "-R ") + text;
				try
				{
					Process.Start("open", arguments);
				}
				catch (Win32Exception ex)
				{
					ex.HelpLink = "";
				}
			}

			public static void OpenInWin(string path)
			{
				bool flag = false;
				string text = path.Replace("/", "\\");
				if (Directory.Exists(text))
				{
					flag = true;
				}
				try
				{
					Process.Start("explorer.exe", (flag ? "/root," : "/select,") + text);
				}
				catch (Win32Exception ex)
				{
					ex.HelpLink = "";
				}
			}

			public static void Open(string path)
			{
				if (IsInWinOS)
				{
					OpenInWin(path);
					return;
				}
				if (IsInMacOS)
				{
					OpenInMac(path);
					return;
				}
			}
		}
	}
}
